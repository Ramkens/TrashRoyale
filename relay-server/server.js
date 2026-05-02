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
  const m = req.url.match(/^\/join\/([A-Z0-9]{4,8})\/?$/i);
  if (m) {
    const code = m[1].toUpperCase();
    res.writeHead(200, { 'Content-Type': 'text/html; charset=utf-8' });
    res.end(`<!doctype html><html lang="ru"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>TrashRoyale \u00b7 \u041a\u043e\u043c\u043d\u0430\u0442\u0430 ${code}</title><style>body{margin:0;font-family:system-ui,-apple-system,sans-serif;background:linear-gradient(135deg,#1a3a8e,#0f1f5a);color:#fff;min-height:100vh;display:flex;align-items:center;justify-content:center;text-align:center;padding:20px}.card{background:rgba(0,0,0,.35);border:2px solid #ffd84a;border-radius:18px;padding:32px;max-width:400px;box-shadow:0 8px 32px rgba(0,0,0,.4)}h1{color:#ffd84a;margin:0 0 8px;text-shadow:2px 2px 0 #000}.code{font-size:46px;font-weight:900;letter-spacing:6px;color:#ffd84a;background:rgba(0,0,0,.4);padding:14px 20px;border-radius:12px;margin:18px 0;font-family:ui-monospace,SFMono-Regular,Menlo,monospace}p{font-size:15px;line-height:1.5;opacity:.9}.btn{display:inline-block;background:#ffd84a;color:#3a2700;padding:12px 22px;border-radius:10px;font-weight:800;text-decoration:none;margin-top:12px;border:2px solid #000}</style></head><body><div class="card"><h1>TRASH ROYALE</h1><p>\u0422\u0435\u0431\u044f \u043f\u0440\u0438\u0433\u043b\u0430\u0441\u0438\u043b\u0438 \u0432 \u043a\u043e\u043c\u043d\u0430\u0442\u0443</p><div class="code">${code}</div><p>\u041e\u0442\u043a\u0440\u043e\u0439 \u0438\u0433\u0440\u0443 TrashRoyale, \u043d\u0430\u0436\u043c\u0438 <b>\u0414\u0420\u0423\u0417\u042c\u042f</b> \u0438 \u0432\u0432\u0435\u0434\u0438 \u044d\u0442\u043e\u0442 \u043a\u043e\u0434.</p></div></body></html>`);
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
