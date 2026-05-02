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
