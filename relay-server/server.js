const http = require('http');
const { WebSocketServer } = require('ws');

const PORT = process.env.PORT || 8080;
const rooms = new Map(); // roomCode -> { host, guest }

const server = http.createServer((req, res) => {
  if (req.url === '/' || req.url === '/health') {
    res.writeHead(200, { 'Content-Type': 'application/json' });
    res.end(JSON.stringify({ status: 'ok', rooms: rooms.size }));
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
  res.writeHead(404);
  res.end('not found');
});

const wss = new WebSocketServer({ server, path: '/ws' });

wss.on('connection', (ws, req) => {
  const url = new URL(req.url, 'http://x');
  const room = (url.searchParams.get('room') || '').toUpperCase();
  const role = (url.searchParams.get('role') || 'host').toLowerCase();
  if (!room) {
    ws.close(1008, 'room required');
    return;
  }
  let entry = rooms.get(room) || { host: null, guest: null };
  if (role === 'host') {
    if (entry.host) { ws.close(1008, 'host taken'); return; }
    entry.host = ws;
  } else {
    if (entry.guest) { ws.close(1008, 'guest taken'); return; }
    entry.guest = ws;
  }
  rooms.set(room, entry);

  console.log(`[+] room=${room} role=${role} now host=${!!entry.host} guest=${!!entry.guest}`);

  // Notify peers when both sides connected
  if (entry.host && entry.guest) {
    try { entry.host.send('{"type":"peer","status":"ready"}'); } catch {}
    try { entry.guest.send('{"type":"peer","status":"ready"}'); } catch {}
  }

  ws.on('message', (data) => {
    const text = data.toString();
    const peer = role === 'host' ? entry.guest : entry.host;
    if (peer && peer.readyState === 1) {
      try { peer.send(text); } catch {}
    }
  });

  ws.on('close', () => {
    if (role === 'host') entry.host = null; else entry.guest = null;
    if (!entry.host && !entry.guest) rooms.delete(room);
    console.log(`[-] room=${room} role=${role} closed`);
    const peer = role === 'host' ? entry.guest : entry.host;
    if (peer && peer.readyState === 1) {
      try { peer.send('{"type":"peer","status":"disconnected"}'); } catch {}
    }
  });

  ws.on('error', () => {});
});

server.listen(PORT, () => {
  console.log(`TrashRoyale relay listening on :${PORT}`);
});
