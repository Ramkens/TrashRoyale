// Email/password auth + cloud profile sync for Trash Royale.
//
// Pure-Node, zero new deps — uses the built-in `crypto` module.
//
// Storage model:
//   * users.json — { [email]: { email, salt, hash, token, createdAt,
//     updatedAt, profileJson } }
//   * Render's free tier filesystem is ephemeral; you should attach a
//     1GB persistent disk and point STATE_DIR at it for production.
//     Without it, every redeploy wipes accounts.
//   * users.json is rewritten atomically on every mutation (write to
//     `${path}.tmp` then rename) to survive a kill mid-write.
//
// Token model:
//   * Random 32-byte hex on register/login. Stored alongside the user
//     row. Client passes it as `Authorization: Bearer <token>`.
//   * No expiry by default — meme game, single device, simpler UX.
//     Logout (DELETE /auth/session) clears the token.

const crypto = require('crypto');
const fs = require('fs');
const path = require('path');

function readJsonBody(req, max = 256 * 1024) {
  return new Promise((resolve, reject) => {
    let data = '';
    let bytes = 0;
    req.on('data', (chunk) => {
      bytes += chunk.length;
      if (bytes > max) { reject(new Error('payload too large')); req.destroy(); return; }
      data += chunk;
    });
    req.on('end', () => {
      try { resolve(data ? JSON.parse(data) : {}); }
      catch (e) { reject(e); }
    });
    req.on('error', reject);
  });
}

function jsonResponse(res, code, obj) {
  res.writeHead(code, { 'Content-Type': 'application/json' });
  res.end(JSON.stringify(obj));
  return true;
}

function emailValid(s) {
  return typeof s === 'string' && /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(s) && s.length < 200;
}
function passwordValid(s) {
  return typeof s === 'string' && s.length >= 6 && s.length < 200;
}

function makeAuth({ stateDir, log = console }) {
  if (!fs.existsSync(stateDir)) {
    try { fs.mkdirSync(stateDir, { recursive: true }); } catch {}
  }
  const usersFile = path.join(stateDir, 'trashroyale-users.json');

  // In-memory map keyed by lowercased email.
  /** @type {Map<string, any>} */
  const users = new Map();
  // Token -> email for O(1) auth lookups.
  const tokenIndex = new Map();

  function loadFromDisk() {
    try {
      if (!fs.existsSync(usersFile)) return;
      const raw = fs.readFileSync(usersFile, 'utf8');
      const arr = JSON.parse(raw);
      if (!Array.isArray(arr)) return;
      for (const u of arr) {
        if (!u || !u.email) continue;
        users.set(u.email.toLowerCase(), u);
        if (u.token) tokenIndex.set(u.token, u.email.toLowerCase());
      }
      log.log(`[auth] loaded ${users.size} users from disk`);
    } catch (err) {
      log.warn('[auth] load failed:', err.message);
    }
  }

  let writeQueued = false;
  function persist() {
    if (writeQueued) return;
    writeQueued = true;
    setImmediate(() => {
      writeQueued = false;
      try {
        const arr = Array.from(users.values());
        const tmp = usersFile + '.tmp';
        fs.writeFileSync(tmp, JSON.stringify(arr), 'utf8');
        fs.renameSync(tmp, usersFile);
      } catch (err) {
        log.warn('[auth] persist failed:', err.message);
      }
    });
  }

  loadFromDisk();

  function hashPassword(password, salt) {
    return crypto.scryptSync(password, salt, 64).toString('hex');
  }
  function genToken() {
    return crypto.randomBytes(32).toString('hex');
  }

  function findByToken(token) {
    if (!token) return null;
    const email = tokenIndex.get(token);
    if (!email) return null;
    return users.get(email) || null;
  }

  function authFromHeaders(req) {
    const h = req.headers['authorization'] || '';
    if (!h.startsWith('Bearer ')) return null;
    return findByToken(h.slice(7).trim());
  }

  // ---- HTTP handlers (return true if handled) ----

  async function handle(req, res) {
    const url = req.url;

    if (url === '/auth/register' && req.method === 'POST') {
      try {
        const body = await readJsonBody(req);
        const email = (body.email || '').toLowerCase().trim();
        const password = body.password || '';
        if (!emailValid(email)) return jsonResponse(res, 400, { error: 'bad_email' });
        if (!passwordValid(password)) return jsonResponse(res, 400, { error: 'bad_password' });
        if (users.has(email)) return jsonResponse(res, 409, { error: 'email_taken' });
        const salt = crypto.randomBytes(16).toString('hex');
        const hash = hashPassword(password, salt);
        const token = genToken();
        const u = {
          email,
          salt,
          hash,
          token,
          createdAt: Date.now(),
          updatedAt: Date.now(),
          profileJson: body.profileJson || '',
        };
        users.set(email, u);
        tokenIndex.set(token, email);
        persist();
        return jsonResponse(res, 200, { token, email, profileJson: u.profileJson });
      } catch (err) {
        return jsonResponse(res, 400, { error: 'bad_request', detail: err.message });
      }
    }

    if (url === '/auth/login' && req.method === 'POST') {
      try {
        const body = await readJsonBody(req);
        const email = (body.email || '').toLowerCase().trim();
        const password = body.password || '';
        const u = users.get(email);
        if (!u) return jsonResponse(res, 401, { error: 'bad_credentials' });
        const candidate = hashPassword(password, u.salt);
        // Use timingSafeEqual to defeat timing attacks.
        const a = Buffer.from(candidate, 'hex');
        const b = Buffer.from(u.hash, 'hex');
        if (a.length !== b.length || !crypto.timingSafeEqual(a, b)) {
          return jsonResponse(res, 401, { error: 'bad_credentials' });
        }
        // Rotate token on every login so old sessions can't replay.
        if (u.token) tokenIndex.delete(u.token);
        u.token = genToken();
        u.updatedAt = Date.now();
        tokenIndex.set(u.token, email);
        persist();
        return jsonResponse(res, 200, { token: u.token, email, profileJson: u.profileJson || '' });
      } catch (err) {
        return jsonResponse(res, 400, { error: 'bad_request', detail: err.message });
      }
    }

    if (url === '/profile' && req.method === 'GET') {
      const u = authFromHeaders(req);
      if (!u) return jsonResponse(res, 401, { error: 'unauthorized' });
      return jsonResponse(res, 200, { email: u.email, profileJson: u.profileJson || '' });
    }

    if (url === '/profile' && req.method === 'POST') {
      const u = authFromHeaders(req);
      if (!u) return jsonResponse(res, 401, { error: 'unauthorized' });
      try {
        const body = await readJsonBody(req);
        if (typeof body.profileJson !== 'string') {
          return jsonResponse(res, 400, { error: 'missing_profileJson' });
        }
        if (body.profileJson.length > 32 * 1024) {
          return jsonResponse(res, 413, { error: 'profile_too_large' });
        }
        u.profileJson = body.profileJson;
        u.updatedAt = Date.now();
        persist();
        return jsonResponse(res, 200, { ok: true });
      } catch (err) {
        return jsonResponse(res, 400, { error: 'bad_request', detail: err.message });
      }
    }

    if (url === '/auth/session' && req.method === 'DELETE') {
      const u = authFromHeaders(req);
      if (!u) return jsonResponse(res, 401, { error: 'unauthorized' });
      tokenIndex.delete(u.token);
      u.token = null;
      persist();
      return jsonResponse(res, 200, { ok: true });
    }

    return false;
  }

  return { handle, _users: users, _tokenIndex: tokenIndex };
}

module.exports = { makeAuth };
