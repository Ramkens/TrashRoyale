using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using TrashRoyale.Core;
using TrashRoyale.Match;
using TrashRoyale.Bootstrap;
using TrashRoyale.Combat;

namespace TrashRoyale.Net
{
    public class NetMatchSync : MonoBehaviour
    {
        WebSocketClient _ws;
        MatchManager _match;
        bool _ready;
        public bool IsHost { get; private set; }
        // Host pushes an authoritative snapshot every quarter-second so
        // the guest's tower HP, elixir, and crown counts stay tied to
        // the host's simulation. Earlier builds shipped only card-play
        // events — that left tower / elixir state diverging between
        // clients, hence the "you see one thing, other sees another"
        // bug report.
        const float SnapshotInterval = 0.25f;
        float _snapTimer;

        public void AttachTo(MatchManager match)
        {
            _match = match;
            _match.IsLocalPvE = false;
            var req = BattleLauncher.Pending;
            if (req == null || string.IsNullOrEmpty(req.netRoomCode)) return;
            IsHost = req.netHost;

            string url = NetConfig.RelayUrl;
            if (string.IsNullOrEmpty(url))
            {
                Debug.LogWarning("[Net] No relay URL configured; running locally");
                _match.IsLocalPvE = true;
                return;
            }
            _ws = new WebSocketClient();
            _ws.OnMessage = OnNetMessage;
            _ws.OnOpen = OnNetOpen;
            _ws.OnClose = (code, msg) => Debug.Log($"[Net] closed {code}/{msg}");
            _ws.OnReconnecting = () => Debug.Log("[Net] reconnecting…");
            _ws.OnReconnected = () => Debug.Log("[Net] reconnected");
            // Free-tier Render redeploys take ~5-15s — keep retrying for a
            // full minute so a brief outage doesn't drop the match.
            _ws.EnableAutoReconnect(60f);
            _ws.Connect($"{url}?room={req.netRoomCode}&role={(IsHost ? "host" : "guest")}");
        }

        void OnNetOpen()
        {
            _ready = true;
            Debug.Log("[Net] connected");
        }

        void Update()
        {
            _ws?.Update();
            if (_match == null || !_ready || !IsHost) return;
            // Host pushes the authoritative state snapshot. Guest never
            // sends snapshots back; it only forwards card-play inputs.
            _snapTimer -= Time.deltaTime;
            if (_snapTimer <= 0f)
            {
                _snapTimer = SnapshotInterval;
                SendSnapshot();
            }
        }

        void SendSnapshot()
        {
            var sb = new StringBuilder(256);
            sb.Append("{\"type\":\"snap\"");
            // Tower HPs — fixed slot order so guest can reconcile by index.
            // PlayerSideTowers[0..1], EnemySideTowers[0..1], PlayerKing, EnemyKing.
            sb.Append(",\"thp\":[");
            AppendTowerHp(sb, _match.PlayerSideTowers, 0); sb.Append(',');
            AppendTowerHp(sb, _match.PlayerSideTowers, 1); sb.Append(',');
            AppendTowerHp(sb, _match.EnemySideTowers, 0); sb.Append(',');
            AppendTowerHp(sb, _match.EnemySideTowers, 1); sb.Append(',');
            AppendOne(sb, _match.PlayerKing); sb.Append(',');
            AppendOne(sb, _match.EnemyKing);
            sb.Append(']');
            sb.Append(",\"ep\":").Append(_match.PlayerElixir.Current.ToString("F2", CultureInfo.InvariantCulture));
            sb.Append(",\"ee\":").Append(_match.EnemyElixir.Current.ToString("F2", CultureInfo.InvariantCulture));
            sb.Append(",\"cp\":").Append(_match.PlayerCrowns);
            sb.Append(",\"ce\":").Append(_match.EnemyCrowns);
            sb.Append(",\"tr\":").Append(_match.TimeRemaining.ToString("F2", CultureInfo.InvariantCulture));
            sb.Append('}');
            _ws.Send(sb.ToString());
        }

        static void AppendTowerHp(StringBuilder sb, List<Tower> list, int idx)
        {
            if (list != null && idx < list.Count && list[idx] != null && !list[idx].isDead)
                sb.Append(list[idx].hp.ToString("F1", CultureInfo.InvariantCulture));
            else
                sb.Append('0');
        }

        static void AppendOne(StringBuilder sb, Tower t)
        {
            if (t != null && !t.isDead)
                sb.Append(t.hp.ToString("F1", CultureInfo.InvariantCulture));
            else
                sb.Append('0');
        }

        void OnDestroy()
        {
            _ws?.Close();
        }

        public void SendCardPlay(int handSlot, string cardId, Vector3 pos)
        {
            if (_ws == null || !_ready) return;
            // "from" tags the sender role so receivers can drop their
            // own echoes (the relay broadcasts to the whole room, sender
            // included) without losing legitimate plays from the other
            // side. Without this, the guest used to drop ALL play
            // messages with `if (!IsHost) return` — that nuked echoes
            // AND the host's plays, so the guest could not see any
            // enemy units (only tower HP via snapshots).
            string fromRole = IsHost ? "host" : "guest";
            var msg = $"{{\"type\":\"play\",\"from\":\"{fromRole}\",\"slot\":{handSlot},\"card\":\"{cardId}\",\"x\":{pos.x:F2},\"z\":{pos.z:F2}}}";
            _ws.Send(msg);
        }

        void OnNetMessage(string json)
        {
            if (json.IndexOf("\"type\":\"play\"", StringComparison.Ordinal) >= 0)
            {
                string from = ExtractStr(json, "from");
                string ownRole = IsHost ? "host" : "guest";
                // Drop our own echo. Older builds without "from" come
                // back as null — fall back to the previous behaviour
                // (host runs full path, guest ignores).
                if (from != null && from == ownRole) return;
                if (from == null && !IsHost) return;

                string card = ExtractStr(json, "card");
                float x = ExtractFloat(json, "x");
                float z = ExtractFloat(json, "z");
                var data = CardDatabase.Get(card);
                if (data == null) return;
                // remote player is on the opposite side, mirror X/Z.
                Vector3 worldPos = new Vector3(-x, 0, -z);

                if (IsHost)
                {
                    // Host-authoritative validation. Without this the
                    // guest could spam the same play in the 0.25s gap
                    // between snapshots: the host wouldn't deduct
                    // enemy elixir, the next snapshot would push the
                    // un-deducted elixir back to the guest, and the
                    // guest could keep spending the same pool over and
                    // over -> infinite-elixir exploit.
                    if (ArenaController.I != null &&
                        !ArenaController.I.IsValidPlacement(Team.Enemy, worldPos, data))
                    {
                        return;
                    }
                    if (!_match.EnemyElixir.TrySpend(data.elixirCost))
                    {
                        // Guest tried to spend more elixir than the host
                        // thinks they have. Drop the play silently — the
                        // next snapshot will correct the guest's UI.
                        return;
                    }
                }
                // Both host and guest spawn the enemy unit locally so
                // each player can see the other side's deployments.
                // Host's elixir/HP snapshots reconcile any drift later.
                UnitFactory.SpawnCard(data, Team.Enemy, worldPos);
                return;
            }
            if (json.IndexOf("\"type\":\"snap\"", StringComparison.Ordinal) >= 0 && !IsHost)
            {
                // Host's authoritative state. Reconcile tower HP, elixir, crowns.
                var thps = ExtractFloatArray(json, "thp");
                if (thps == null || thps.Length < 6) return;
                float ep = ExtractFloat(json, "ep");
                float ee = ExtractFloat(json, "ee");
                int cp = (int)ExtractFloat(json, "cp");
                int ce = (int)ExtractFloat(json, "ce");
                float tr = ExtractFloat(json, "tr");
                _match.ApplyAuthoritativeSnapshot(thps, ep, ee, cp, ce, tr);
            }
        }

        static float[] ExtractFloatArray(string json, string key)
        {
            int i = json.IndexOf("\"" + key + "\":[", StringComparison.Ordinal);
            if (i < 0) return null;
            int s = i + key.Length + 4; // past "key":[
            int e = json.IndexOf(']', s);
            if (e < 0) return null;
            var parts = json.Substring(s, e - s).Split(',');
            var result = new float[parts.Length];
            for (int p = 0; p < parts.Length; p++)
            {
                float.TryParse(parts[p].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out result[p]);
            }
            return result;
        }

        static string ExtractStr(string json, string key)
        {
            int i = json.IndexOf("\"" + key + "\":");
            if (i < 0) return null;
            int s = json.IndexOf('"', i + key.Length + 3) + 1;
            int e = json.IndexOf('"', s);
            return s > 0 && e > s ? json.Substring(s, e - s) : null;
        }

        static float ExtractFloat(string json, string key)
        {
            int i = json.IndexOf("\"" + key + "\":");
            if (i < 0) return 0;
            int s = i + key.Length + 3;
            int e = s;
            while (e < json.Length && (char.IsDigit(json[e]) || json[e] == '.' || json[e] == '-')) e++;
            float.TryParse(json.Substring(s, e - s), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var f);
            return f;
        }
    }

    public static class NetConfig
    {
        public static string RelayUrl = "wss://trashroyale-relay.onrender.com/ws";
        // PR5: HTTP base for /auth, /profile, /queue endpoints. Same
        // host as the relay; the WebSocket scheme is swapped out in
        // AuthClient when building the URL.
        public static string ApiBaseUrl = "https://trashroyale-relay.onrender.com";
    }
}
