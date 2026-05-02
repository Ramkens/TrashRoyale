using System;
using UnityEngine;
using UnityEngine.Networking;

namespace TrashRoyale.Net
{
    /// <summary>
    /// HTTP client for the relay's random-opponent queue. Endpoint:
    ///   GET /queue/join?id=&lt;clientId&gt; -> { status, roomCode, role }
    ///
    /// status="waiting" means the caller is the first in queue and was
    /// given a fresh roomCode; status="matched" means a peer was waiting
    /// and the caller was paired into their existing room.
    ///
    /// Either way, the client should connect to the relay's <c>/ws</c>
    /// endpoint with the returned roomCode + role to actually start the
    /// match.
    /// </summary>
    public static class MatchmakingClient
    {
        public class Result
        {
            public bool ok;
            public string error;
            public string roomCode;
            public string role;
            public bool matched; // true when status == "matched"
        }

        static string ClientId()
        {
            var id = PlayerPrefs.GetString("trashroyale.client.id", "");
            if (string.IsNullOrEmpty(id))
            {
                id = Guid.NewGuid().ToString("N").Substring(0, 16);
                PlayerPrefs.SetString("trashroyale.client.id", id);
                PlayerPrefs.Save();
            }
            return id;
        }

        public static System.Collections.IEnumerator JoinQueue(Action<Result> done)
        {
            var url = NetConfig.ApiBaseUrl + "/queue/join?id=" + UnityWebRequest.EscapeURL(ClientId());
            using var req = UnityWebRequest.Get(url);
            req.timeout = 30;
            yield return req.SendWebRequest();
            var r = new Result();
            if (req.result != UnityWebRequest.Result.Success)
            {
                r.error = req.responseCode == 0 ? "network_error" : ("http_" + req.responseCode);
                done?.Invoke(r);
                yield break;
            }
            try
            {
                var json = req.downloadHandler.text;
                r.roomCode = AuthClient_ExtractField(json, "roomCode");
                r.role = AuthClient_ExtractField(json, "role");
                var status = AuthClient_ExtractField(json, "status");
                r.matched = status == "matched";
                if (string.IsNullOrEmpty(r.roomCode))
                {
                    r.error = AuthClient_ExtractField(json, "error");
                }
                else
                {
                    r.ok = true;
                }
            }
            catch (Exception ex)
            {
                r.error = "parse_error:" + ex.Message;
            }
            done?.Invoke(r);
        }

        // Tiny stand-alone JSON extractor (mirrors AuthClient.ExtractField
        // — duplicated here so MatchmakingClient stays self-contained).
        static string AuthClient_ExtractField(string json, string key)
        {
            if (string.IsNullOrEmpty(json)) return "";
            var needle = "\"" + key + "\"";
            int k = json.IndexOf(needle, StringComparison.Ordinal);
            if (k < 0) return "";
            int colon = json.IndexOf(':', k + needle.Length);
            if (colon < 0) return "";
            int i = colon + 1;
            while (i < json.Length && (json[i] == ' ' || json[i] == '\t')) i++;
            if (i >= json.Length) return "";
            if (json[i] == '"')
            {
                int start = ++i;
                var sb = new System.Text.StringBuilder();
                while (i < json.Length && json[i] != '"')
                {
                    if (json[i] == '\\' && i + 1 < json.Length) { sb.Append(json[i + 1]); i += 2; }
                    else sb.Append(json[i++]);
                }
                return sb.ToString();
            }
            int e = i;
            while (e < json.Length && ",}]\n\r\t ".IndexOf(json[e]) < 0) e++;
            return json.Substring(i, e - i);
        }
    }
}
