using System;
using System.Collections.Generic;
using UnityEngine;
using TrashRoyale.Core;
using TrashRoyale.Match;
using TrashRoyale.Bootstrap;

namespace TrashRoyale.Net
{
    public class NetMatchSync : MonoBehaviour
    {
        WebSocketClient _ws;
        MatchManager _match;
        bool _ready;
        public bool IsHost { get; private set; }

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
        }

        void OnDestroy()
        {
            _ws?.Close();
        }

        public void SendCardPlay(int handSlot, string cardId, Vector3 pos)
        {
            if (_ws == null || !_ready) return;
            var msg = $"{{\"type\":\"play\",\"slot\":{handSlot},\"card\":\"{cardId}\",\"x\":{pos.x:F2},\"z\":{pos.z:F2}}}";
            _ws.Send(msg);
        }

        void OnNetMessage(string json)
        {
            if (json.IndexOf("\"type\":\"play\"", StringComparison.Ordinal) < 0) return;
            string card = ExtractStr(json, "card");
            float x = ExtractFloat(json, "x");
            float z = ExtractFloat(json, "z");
            var data = CardDatabase.Get(card);
            if (data == null) return;
            // remote player is on the opposite side, mirror Z
            Vector3 worldPos = new Vector3(-x, 0, -z);
            UnitFactory.SpawnCard(data, Team.Enemy, worldPos);
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
