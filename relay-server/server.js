const http = require('http');
const fs = require('fs');
const path = require('path');
const { WebSocketServer } = require('ws');
const { makeAuth } = require('./auth');

const PORT = process.env.PORT || 8080;

// Persist room metadata across short Render restarts so an in-flight
// match doesn't lose its pairing during a 5-10 second redeploy. We
// persist ONLY the room codes + creation time, not the WebSocket
// objects (those are re-established by clients on reconnect).
//
// Render's free tier has an ephemeral filesystem at /tmp, so the
// snapshot survives a cold restart but not a full instance hop. That's
// fine — the cron-job.org keepalive (every 60s) keeps the instance
// warm so a hop is rare.
const STATE_DIR = process.env.STATE_DIR || '/tmp';
const STATE_FILE = path.join(STATE_DIR, 'trashroyale-rooms.json');

// roomCode -> { host, guest, createdAt, lastActivity, lingerUntil }
//   host/guest are live ws sockets (or null while waiting for reconnect)
//   lingerUntil > now() means the room is orphaned but still reservable
//   so a freshly-reconnected client can reclaim its slot.
const rooms = new Map();
const ROOM_LINGER_MS = 60_000;
const ROOM_TTL_MS = 30 * 60_000;

function nowMs() { return Date.now(); }

function snapshot() {
  const out = [];
  for (const [code, e] of rooms.entries()) {
    out.push({
      code,
      createdAt: e.createdAt,
      lastActivity: e.lastActivity,
      lingerUntil: e.lingerUntil || 0,
    });
  }
  try {
    fs.writeFileSync(STATE_FILE, JSON.stringify(out), 'utf8');
  } catch (err) {
    console.warn('[snapshot] write failed:', err.message);
  }
}

function restore() {
  try {
    if (!fs.existsSync(STATE_FILE)) return;
    const raw = fs.readFileSync(STATE_FILE, 'utf8');
    const arr = JSON.parse(raw);
    const cutoff = nowMs() - ROOM_TTL_MS;
    let restored = 0;
    for (const r of arr) {
      if (!r.code || (r.lastActivity || 0) < cutoff) continue;
      // Restore as ghost rooms — clients will reclaim host/guest slots
      // on reconnect. Lingering is extended so reconnects within 60s
      // after restart succeed.
      rooms.set(r.code, {
        host: null,
        guest: null,
        createdAt: r.createdAt || nowMs(),
        lastActivity: r.lastActivity || nowMs(),
        lingerUntil: nowMs() + ROOM_LINGER_MS,
      });
      restored++;
    }
    if (restored > 0) console.log(`[restore] ${restored} rooms restored from snapshot`);
  } catch (err) {
    console.warn('[restore] read failed:', err.message);
  }
}

restore();

// Periodic cleanup: drop fully-orphaned rooms past their linger window
// so we don't accumulate dead codes forever.
setInterval(() => {
  const now = nowMs();
  let pruned = 0;
  for (const [code, e] of rooms.entries()) {
    const orphaned = !e.host && !e.guest;
    if (orphaned && (e.lingerUntil || 0) < now) {
      rooms.delete(code);
      pruned++;
    } else if (now - (e.lastActivity || 0) > ROOM_TTL_MS) {
      try { e.host?.close(1001, 'expired'); } catch {}
      try { e.guest?.close(1001, 'expired'); } catch {}
      rooms.delete(code);
      pruned++;
    }
  }
  if (pruned > 0) snapshot();
}, 15_000);

// Periodic snapshot to ride out unscheduled crashes.
setInterval(snapshot, 5_000);

// PR5: email/password auth + cloud-saved profile.
const auth = makeAuth({ stateDir: STATE_DIR, log: console });

const server = http.createServer(async (req, res) => {
  // Permissive CORS for the in-app HTTP client (no browser origin
  // matters here, but kept for parity with the join landing page).
  res.setHeader('Access-Control-Allow-Origin', '*');
  res.setHeader('Access-Control-Allow-Headers', 'Content-Type, Authorization');
  res.setHeader('Access-Control-Allow-Methods', 'GET, POST, DELETE, OPTIONS');
  if (req.method === 'OPTIONS') { res.writeHead(204); res.end(); return; }

  // Auth + profile endpoints first — return early if handled.
  try {
    const handled = await auth.handle(req, res);
    if (handled) return;
  } catch (err) {
    console.warn('[auth] handler error:', err.message);
  }

  if (req.url === '/' || req.url === '/health') {
    res.writeHead(200, { 'Content-Type': 'application/json' });
    res.end(JSON.stringify({
      status: 'ok',
      rooms: rooms.size,
      uptime: process.uptime(),
    }));
    return;
  }
  // Landing page for share links: /join/{CODE}
  // On Android, the App Link will open Trash Royale directly (no browser).
  // If browser falls through (iOS, desktop, app not installed), show a fallback page
  // that auto-attempts the trashroyale://join/CODE custom scheme.
  const m = req.url.match(/^\/join\/([A-Z0-9]{4,8})\/?$/i);
  if (m) {
    const code = m[1].toUpperCase();
    res.writeHead(200, { 'Content-Type': 'text/html; charset=utf-8' });
    res.end(`<!doctype html><html lang="ru"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Trash Royale \u00b7 \u041a\u043e\u043c\u043d\u0430\u0442\u0430 ${code}</title><style>body{margin:0;font-family:system-ui,-apple-system,sans-serif;background:linear-gradient(135deg,#1a3a8e,#0f1f5a);color:#fff;min-height:100vh;display:flex;align-items:center;justify-content:center;text-align:center;padding:20px}.card{background:rgba(0,0,0,.35);border:2px solid #ffd84a;border-radius:18px;padding:32px;max-width:420px;box-shadow:0 8px 32px rgba(0,0,0,.4)}h1{color:#ffd84a;margin:0 0 8px;text-shadow:2px 2px 0 #000;font-size:34px;letter-spacing:1px}.code{font-size:46px;font-weight:900;letter-spacing:6px;color:#ffd84a;background:rgba(0,0,0,.4);padding:14px 20px;border-radius:12px;margin:18px 0;font-family:ui-monospace,SFMono-Regular,Menlo,monospace;cursor:pointer}p{font-size:15px;line-height:1.5;opacity:.9}.btn{display:inline-block;background:#ffd84a;color:#3a2700;padding:12px 22px;border-radius:10px;font-weight:800;text-decoration:none;margin:6px;border:2px solid #000}.row{display:flex;gap:6px;justify-content:center;flex-wrap:wrap;margin-top:8px}.small{font-size:12px;opacity:.7;margin-top:14px}</style></head><body><div class="card"><h1>TRASH ROYALE</h1><p>\u0422\u0435\u0431\u044f \u043f\u0440\u0438\u0433\u043b\u0430\u0441\u0438\u043b\u0438 \u0432 \u043a\u043e\u043c\u043d\u0430\u0442\u0443</p><div class="code" id="code" onclick="copyCode()">${code}</div><div class="row"><a class="btn" href="trashroyale://join/${code}" id="open">\u041e\u0442\u043a\u0440\u044b\u0442\u044c \u0432 \u0438\u0433\u0440\u0435</a><a class="btn" href="javascript:copyCode()">\u0421\u043a\u043e\u043f\u0438\u0440\u043e\u0432\u0430\u0442\u044c \u043a\u043e\u0434</a></div><p class="small">\u0415\u0441\u043b\u0438 \u0438\u0433\u0440\u0430 \u043d\u0435 \u043e\u0442\u043a\u0440\u044b\u043b\u0430\u0441\u044c \u0430\u0432\u0442\u043e\u043c\u0430\u0442\u0438\u0447\u0435\u0441\u043a\u0438, \u0437\u0430\u043f\u0443\u0441\u0442\u0438 Trash Royale, \u043d\u0430\u0436\u043c\u0438 \u0414\u0420\u0423\u0417\u042c\u042f \u0438 \u0432\u0432\u0435\u0434\u0438 \u043a\u043e\u0434.</p></div><script>const C='${code}';function copyCode(){navigator.clipboard&&navigator.clipboard.writeText(C);const e=document.getElementById('code');e.textContent='\u0421\u043a\u043e\u043f\u0438\u0440\u043e\u0432\u0430\u043d\u043e!';setTimeout(()=>e.textContent=C,1200)}setTimeout(()=>{location.href='trashroyale://join/'+C},250)</script></body></html>`);
    return;
  }
  // Random matchmaking queue: GET /queue/join?id=<clientId>
  // Returns a roomCode the client should connect to. The first caller
  // creates a room and waits as host; the second caller is matched into
  // the same room as guest. Clients then connect via /ws as usual.
  if (req.url.startsWith('/queue/join')) {
    handleQueueJoin(req, res);
    return;
  }
  if (req.url.startsWith('/queue/cancel')) {
    handleQueueCancel(req, res);
    return;
  }
  res.writeHead(404);
  res.end('not found');
});

// ---------- Random-opponent matchmaking queue ----------
// Single FIFO queue. Each entry holds { id, roomCode, createdAt }.
// On /queue/join: if a peer is waiting, return their roomCode so
// caller joins as guest. Else create a new room and put caller in
// queue as host.
const queue = []; // { id, roomCode, createdAt }
const QUEUE_TIMEOUT_MS = 90_000;

function genRoomCode() {
  const alphabet = 'ABCDEFGHJKLMNPQRSTUVWXYZ23456789';
  let s = '';
  for (let i = 0; i < 5; i++) s += alphabet[Math.floor(Math.random() * alphabet.length)];
  if (rooms.has(s)) return genRoomCode();
  return s;
}

function handleQueueJoin(req, res) {
  const url = new URL(req.url, 'http://x');
  const id = url.searchParams.get('id') || ('anon-' + Math.random().toString(36).slice(2, 8));
  // Drop stale waiters.
  const now = nowMs();
  while (queue.length > 0 && (now - queue[0].createdAt > QUEUE_TIMEOUT_MS)) queue.shift();
  // Don't allow same id to queue twice.
  for (let i = queue.length - 1; i >= 0; i--) {
    if (queue[i].id === id) queue.splice(i, 1);
  }
  if (queue.length > 0) {
    const partner = queue.shift();
    res.writeHead(200, { 'Content-Type': 'application/json' });
    res.end(JSON.stringify({ status: 'matched', roomCode: partner.roomCode, role: 'guest' }));
    return;
  }
  const code = genRoomCode();
  // Reserve the room with linger so the host has time to open ws.
  rooms.set(code, {
    host: null,
    guest: null,
    createdAt: now,
    lastActivity: now,
    lingerUntil: now + ROOM_LINGER_MS,
  });
  queue.push({ id, roomCode: code, createdAt: now });
  snapshot();
  res.writeHead(200, { 'Content-Type': 'application/json' });
  res.end(JSON.stringify({ status: 'waiting', roomCode: code, role: 'host' }));
}

function handleQueueCancel(req, res) {
  const url = new URL(req.url, 'http://x');
  const id = url.searchParams.get('id') || '';
  const code = (url.searchParams.get('room') || '').toUpperCase();
  for (let i = queue.length - 1; i >= 0; i--) {
    if (queue[i].id === id || queue[i].roomCode === code) queue.splice(i, 1);
  }
  if (code && rooms.has(code)) {
    const e = rooms.get(code);
    if (!e.host && !e.guest) rooms.delete(code);
  }
  res.writeHead(200, { 'Content-Type': 'application/json' });
  res.end(JSON.stringify({ status: 'cancelled' }));
}

const wss = new WebSocketServer({ server, path: '/ws' });

wss.on('connection', (ws, req) => {
  const url = new URL(req.url, 'http://x');
  const room = (url.searchParams.get('room') || '').toUpperCase();
  const role = (url.searchParams.get('role') || 'host').toLowerCase();
  if (!room) {
    ws.close(1008, 'room required');
    return;
  }
  const now = nowMs();
  let entry = rooms.get(room) || {
    host: null,
    guest: null,
    createdAt: now,
    lastActivity: now,
    lingerUntil: 0,
  };

  // Reclaim slots even if a previous (zombie) socket is still set —
  // it might be a half-closed socket from a previous instance. We
  // close it explicitly so the new client can take over.
  function closeSocket(s) {
    if (!s) return;
    try { s.close(1000, 'replaced'); } catch {}
  }

  if (role === 'host') {
    if (entry.host && entry.host.readyState === 1 && entry.host !== ws) {
      // A live host already owns this slot — reject.
      ws.close(1008, 'host taken');
      return;
    }
    closeSocket(entry.host);
    entry.host = ws;
  } else {
    if (entry.guest && entry.guest.readyState === 1 && entry.guest !== ws) {
      ws.close(1008, 'guest taken');
      return;
    }
    closeSocket(entry.guest);
    entry.guest = ws;
  }
  entry.lastActivity = now;
  entry.lingerUntil = 0; // not lingering anymore
  rooms.set(room, entry);
  snapshot();

  console.log(`[+] room=${room} role=${role} host=${!!entry.host} guest=${!!entry.guest}`);

  // Notify peers when both sides connected
  if (entry.host && entry.guest) {
    try { entry.host.send('{"type":"peer","status":"ready"}'); } catch {}
    try { entry.guest.send('{"type":"peer","status":"ready"}'); } catch {}
  } else {
    try { ws.send(JSON.stringify({ type: 'peer', status: 'waiting' })); } catch {}
  }

  ws.on('message', (data) => {
    const text = data.toString();
    entry.lastActivity = nowMs();
    const peer = role === 'host' ? entry.guest : entry.host;
    if (peer && peer.readyState === 1) {
      try { peer.send(text); } catch {}
    }
  });

  ws.on('close', () => {
    if (role === 'host' && entry.host === ws) entry.host = null;
    if (role === 'guest' && entry.guest === ws) entry.guest = null;
    if (!entry.host && !entry.guest) {
      // Don't immediately delete — keep ghost room around so a quickly
      // reconnecting client can reclaim it (Render restart, mobile
      // network blip).
      entry.lingerUntil = nowMs() + ROOM_LINGER_MS;
    }
    rooms.set(room, entry);
    console.log(`[-] room=${room} role=${role} closed`);
    const peer = role === 'host' ? entry.guest : entry.host;
    if (peer && peer.readyState === 1) {
      try { peer.send('{"type":"peer","status":"disconnected"}'); } catch {}
    }
    snapshot();
  });

  ws.on('error', () => {});
});

server.listen(PORT, () => {
  console.log(`TrashRoyale relay listening on :${PORT}`);
});

function shutdown(sig) {
  console.log(`[shutdown] ${sig} — snapshotting state`);
  snapshot();
  process.exit(0);
}
process.on('SIGTERM', () => shutdown('SIGTERM'));
process.on('SIGINT', () => shutdown('SIGINT'));
