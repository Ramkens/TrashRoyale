using System;
using UnityEngine;

namespace TrashRoyale.Net
{
    /// <summary>
    /// Captures deep links (trashroyale://join/CODE or https://.../join/CODE) and
    /// extracts the room code so the menu can auto-open the friendly battle popup.
    /// </summary>
    public static class DeepLinkHandler
    {
        public static string PendingJoinCode { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        static void Init()
        {
            // Cold start: app launched via deep link.
            if (!string.IsNullOrEmpty(Application.absoluteURL))
                Capture(Application.absoluteURL);

            Application.deepLinkActivated -= Capture;
            Application.deepLinkActivated += Capture;
        }

        public static event Action<string> OnNewJoinCode;

        public static void Capture(string url)
        {
            if (string.IsNullOrEmpty(url)) return;
            try
            {
                // Strip scheme + host, find /join/{CODE} or trashroyale://join/CODE
                string code = null;
                int joinIdx = url.IndexOf("/join/", StringComparison.OrdinalIgnoreCase);
                if (joinIdx >= 0)
                {
                    code = url.Substring(joinIdx + 6);
                }
                else if (url.StartsWith("trashroyale://join/", StringComparison.OrdinalIgnoreCase))
                {
                    code = url.Substring("trashroyale://join/".Length);
                }
                if (string.IsNullOrEmpty(code)) return;
                // Strip query / fragment / trailing slash
                int q = code.IndexOfAny(new[] { '/', '?', '#' });
                if (q >= 0) code = code.Substring(0, q);
                code = code.ToUpperInvariant().Trim();
                if (code.Length < 4 || code.Length > 8) return;

                PendingJoinCode = code;
                Debug.Log("[DeepLink] Captured join code: " + code);
                OnNewJoinCode?.Invoke(code);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[DeepLink] Parse failed: " + ex.Message);
            }
        }

        public static string ConsumeJoinCode()
        {
            var c = PendingJoinCode;
            PendingJoinCode = null;
            return c;
        }
    }
}
