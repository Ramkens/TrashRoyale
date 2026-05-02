using System;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace TrashRoyale.Net
{
    /// <summary>
    /// Thin HTTP wrapper around the relay-server's auth + profile API.
    /// Endpoints (see relay-server/auth.js):
    ///   POST /auth/register   { email, password } -> { token, email, profileJson }
    ///   POST /auth/login      { email, password } -> { token, email, profileJson }
    ///   GET  /profile         (Authorization: Bearer token) -> { profileJson }
    ///   POST /profile         { profileJson }                -> { ok: true }
    ///   DELETE /auth/session                                  -> { ok: true }
    ///
    /// Auth state is cached in PlayerPrefs so a logged-in user stays
    /// logged in across launches. Set <see cref="OfflineMode"/> to skip
    /// remote sync entirely (used when the user picks "Skip" on the
    /// login screen).
    /// </summary>
    public static class AuthClient
    {
        const string PrefsToken = "trashroyale.auth.token";
        const string PrefsEmail = "trashroyale.auth.email";
        const string PrefsOffline = "trashroyale.auth.offline";

        public static string Token => PlayerPrefs.GetString(PrefsToken, "");
        public static string Email => PlayerPrefs.GetString(PrefsEmail, "");
        public static bool IsLoggedIn => !string.IsNullOrEmpty(Token);
        public static bool OfflineMode
        {
            get => PlayerPrefs.GetInt(PrefsOffline, 0) == 1;
            set { PlayerPrefs.SetInt(PrefsOffline, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        public static void Logout()
        {
            PlayerPrefs.DeleteKey(PrefsToken);
            PlayerPrefs.DeleteKey(PrefsEmail);
            PlayerPrefs.Save();
        }

        public class Result
        {
            public bool ok;
            public string error;        // server-side error code or local exception
            public string token;
            public string email;
            public string profileJson;
        }

        public static System.Collections.IEnumerator Register(string email, string password, Action<Result> done)
        {
            yield return DoAuth("/auth/register", email, password, done);
        }

        public static System.Collections.IEnumerator Login(string email, string password, Action<Result> done)
        {
            yield return DoAuth("/auth/login", email, password, done);
        }

        static System.Collections.IEnumerator DoAuth(string path, string email, string password, Action<Result> done)
        {
            var url = NetConfig.ApiBaseUrl + path;
            var body = $"{{\"email\":\"{Escape(email)}\",\"password\":\"{Escape(password)}\"}}";
            using var req = new UnityWebRequest(url, "POST");
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.timeout = 15;
            yield return req.SendWebRequest();
            var r = new Result();
            if (req.result != UnityWebRequest.Result.Success)
            {
                r.error = ParseErrorOrNet(req);
                done?.Invoke(r);
                yield break;
            }
            try
            {
                var json = req.downloadHandler.text;
                r.token = ExtractField(json, "token");
                r.email = ExtractField(json, "email");
                r.profileJson = ExtractField(json, "profileJson");
                if (string.IsNullOrEmpty(r.token))
                {
                    r.error = ExtractField(json, "error");
                }
                else
                {
                    r.ok = true;
                    PlayerPrefs.SetString(PrefsToken, r.token);
                    PlayerPrefs.SetString(PrefsEmail, r.email);
                    PlayerPrefs.SetInt(PrefsOffline, 0);
                    PlayerPrefs.Save();
                }
            }
            catch (Exception ex)
            {
                r.error = "parse_error:" + ex.Message;
            }
            done?.Invoke(r);
        }

        public static System.Collections.IEnumerator FetchProfile(Action<Result> done)
        {
            var r = new Result();
            if (!IsLoggedIn) { r.error = "not_logged_in"; done?.Invoke(r); yield break; }
            var url = NetConfig.ApiBaseUrl + "/profile";
            using var req = UnityWebRequest.Get(url);
            req.SetRequestHeader("Authorization", "Bearer " + Token);
            req.timeout = 15;
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success)
            {
                r.error = ParseErrorOrNet(req);
                done?.Invoke(r);
                yield break;
            }
            r.ok = true;
            r.profileJson = ExtractField(req.downloadHandler.text, "profileJson");
            done?.Invoke(r);
        }

        public static System.Collections.IEnumerator PushProfile(string profileJson, Action<Result> done)
        {
            var r = new Result();
            if (!IsLoggedIn) { r.error = "not_logged_in"; done?.Invoke(r); yield break; }
            var url = NetConfig.ApiBaseUrl + "/profile";
            // Wrap in a JSON envelope; profileJson is a string-encoded JSON
            // payload (i.e. the entire serialized PlayerProfile).
            var body = $"{{\"profileJson\":\"{Escape(profileJson)}\"}}";
            using var req = new UnityWebRequest(url, "POST");
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("Authorization", "Bearer " + Token);
            req.timeout = 15;
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success)
            {
                r.error = ParseErrorOrNet(req);
                done?.Invoke(r);
                yield break;
            }
            r.ok = true;
            done?.Invoke(r);
        }

        // ---- Tiny JSON helpers (we only ever read flat string/number
        // fields, so a regex-style extractor is safer than dragging in
        // JsonUtility's strict typing for one-off responses). ----

        static string Escape(string s)
        {
            if (s == null) return "";
            var sb = new StringBuilder(s.Length + 8);
            foreach (var c in s)
            {
                switch (c)
                {
                    case '\\': sb.Append("\\\\"); break;
                    case '"':  sb.Append("\\\""); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20) sb.AppendFormat("\\u{0:x4}", (int)c);
                        else sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
        }

        static string ExtractField(string json, string key)
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
                var sb = new StringBuilder();
                while (i < json.Length && json[i] != '"')
                {
                    if (json[i] == '\\' && i + 1 < json.Length)
                    {
                        char esc = json[i + 1];
                        if (esc == 'n') sb.Append('\n');
                        else if (esc == 'r') sb.Append('\r');
                        else if (esc == 't') sb.Append('\t');
                        else if (esc == '\\') sb.Append('\\');
                        else if (esc == '"') sb.Append('"');
                        else if (esc == '/') sb.Append('/');
                        else sb.Append(esc);
                        i += 2;
                    }
                    else
                    {
                        sb.Append(json[i++]);
                    }
                }
                return sb.ToString();
            }
            int e = i;
            while (e < json.Length && ",}]\n\r\t ".IndexOf(json[e]) < 0) e++;
            return json.Substring(i, e - i);
        }

        static string ParseErrorOrNet(UnityWebRequest req)
        {
            try
            {
                var msg = ExtractField(req.downloadHandler != null ? req.downloadHandler.text : "", "error");
                if (!string.IsNullOrEmpty(msg)) return msg;
            }
            catch { }
            if (req.responseCode == 0) return "network_error";
            return "http_" + req.responseCode;
        }
    }
}
