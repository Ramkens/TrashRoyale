using UnityEngine;
using UnityEngine.UI;
using TrashRoyale.Match;
using TrashRoyale.Persistence;
using TrashRoyale.Bootstrap;
using TrashRoyale.Audio;

namespace TrashRoyale.UI
{
    /// <summary>
    /// Clash Royale-style match-start banner reveal. Activates during
    /// <see cref="MatchPhase.Countdown"/>:
    ///   * 0.0–1.0s: dark dimmer fades in over the arena, "VS" pops
    ///     in the middle.
    ///   * 0.4–1.6s: opponent banner slides in from the top (name +
    ///     icon + colored bar), player banner slides in from the
    ///     bottom.
    ///   * 1.6–2.6s: banners hold.
    ///   * 2.6–4.0s: banners + dimmer ease out.
    ///   * 4.0–5.0s: huge "3..2..1.. GO!" digit pulses in middle.
    /// Pure procedural UI — no prefab dependency. Lives on its own
    /// canvas at sortingOrder=20 so it draws above the regular HUD.
    /// </summary>
    public class MatchIntroOverlay : MonoBehaviour
    {
        Canvas _canvas;
        Image _dimmer;
        RectTransform _opponentBanner;
        RectTransform _playerBanner;
        Text _vsText;
        Text _countdownText;
        bool _started;
        float _t;
        bool _goSoundPlayed;
        int _lastCountdownDigit = -1;

        public static MatchIntroOverlay Build()
        {
            var go = new GameObject("MatchIntroOverlay");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            go.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 1f;
            go.AddComponent<GraphicRaycaster>();
            var ov = go.AddComponent<MatchIntroOverlay>();
            ov._canvas = canvas;
            ov.BuildContent();
            return ov;
        }

        void BuildContent()
        {
            // Dimmer covers the whole screen.
            _dimmer = UIFactory.MakePanel(transform, "Dimmer", new Color(0, 0, 0, 0.0f));
            var drt = _dimmer.GetComponent<RectTransform>();
            drt.anchorMin = Vector2.zero;
            drt.anchorMax = Vector2.one;
            drt.offsetMin = drt.offsetMax = Vector2.zero;
            _dimmer.raycastTarget = true; // block input during intro

            // VS text in center.
            _vsText = UIFactory.MakeText(transform, "VS", "VS", 220, TextAnchor.MiddleCenter);
            _vsText.color = new Color(1f, 0.95f, 0.4f, 0f);
            var vsrt = _vsText.GetComponent<RectTransform>();
            vsrt.anchorMin = new Vector2(0.5f, 0.5f);
            vsrt.anchorMax = new Vector2(0.5f, 0.5f);
            vsrt.sizeDelta = new Vector2(400, 250);
            vsrt.anchoredPosition = Vector2.zero;
            var vsOutline = _vsText.gameObject.AddComponent<Outline>();
            vsOutline.effectColor = Color.black;
            vsOutline.effectDistance = new Vector2(8, -8);

            // Opponent banner (top — slides down from off-screen top).
            var profile = PlayerProfile.Load();
            var req = BattleLauncher.Pending;
            string oppName = req != null ? req.botName : "Соперник";
            Color oppColor = req != null ? req.botBannerColor : new Color(0.6f, 0.35f, 0.85f);
            string oppIcon = req != null ? req.botIconKey : "fist";
            _opponentBanner = BuildBanner("OpponentBanner", oppName, oppColor, oppIcon, top: true);

            // Player banner (bottom — slides up).
            Color plColor = new Color(0.25f, 0.55f, 0.95f);
            string plIcon = "crown";
            _playerBanner = BuildBanner("PlayerBanner", profile.playerName, plColor, plIcon, top: false);

            // Big countdown digits in center (after banners exit).
            _countdownText = UIFactory.MakeText(transform, "Countdown", "", 380, TextAnchor.MiddleCenter);
            _countdownText.color = new Color(1f, 0.93f, 0.45f, 0f);
            var crt = _countdownText.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.5f, 0.5f);
            crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.sizeDelta = new Vector2(600, 460);
            crt.anchoredPosition = Vector2.zero;
            var cOutline = _countdownText.gameObject.AddComponent<Outline>();
            cOutline.effectColor = Color.black;
            cOutline.effectDistance = new Vector2(10, -10);
        }

        RectTransform BuildBanner(string name, string nick, Color color, string iconKey, bool top)
        {
            var panel = UIFactory.MakePanel(transform, name, new Color(0.05f, 0.07f, 0.18f, 0.95f));
            var btnSp = UIFactory.LoadSprite("UI/btn_gold");
            if (btnSp != null)
            {
                panel.sprite = btnSp;
                panel.type = Image.Type.Sliced;
                panel.color = color;
            }
            var rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, top ? 1f : 0f);
            rt.anchorMax = new Vector2(1f, top ? 1f : 0f);
            rt.pivot = new Vector2(0.5f, top ? 0f : 1f);
            rt.sizeDelta = new Vector2(-80, 200);
            // Start off-screen — slide in via Update().
            float offY = top ? 260f : -260f;
            rt.anchoredPosition = new Vector2(0, offY);

            // Icon on left.
            var iconImg = UIFactory.MakeIcon(panel.transform, "Icon", "Icons/" + iconKey, new Vector2(140, 140));
            var irt = iconImg.GetComponent<RectTransform>();
            irt.anchorMin = new Vector2(0f, 0.5f);
            irt.anchorMax = new Vector2(0f, 0.5f);
            irt.pivot = new Vector2(0f, 0.5f);
            irt.anchoredPosition = new Vector2(20, 0);

            // Name center-left.
            var nameTxt = UIFactory.MakeText(panel.transform, "Name", nick, 64, TextAnchor.MiddleLeft);
            nameTxt.color = Color.white;
            var nrt = nameTxt.GetComponent<RectTransform>();
            nrt.anchorMin = new Vector2(0f, 0f);
            nrt.anchorMax = new Vector2(1f, 1f);
            nrt.offsetMin = new Vector2(180, 10);
            nrt.offsetMax = new Vector2(-30, -10);
            var nameOutline = nameTxt.gameObject.AddComponent<Outline>();
            nameOutline.effectColor = Color.black;
            nameOutline.effectDistance = new Vector2(3, -3);
            return rt;
        }

        public void StartIntro()
        {
            _started = true;
            _t = 0f;
            AudioManager.PlayOneShot("match_start", Vector3.zero);
        }

        void Update()
        {
            if (!_started) return;
            _t += Time.deltaTime;
            float t = _t;

            // Dimmer fade — up to 0.6 over the first 1s, hold, fade out
            // during 2.6-4.0s.
            float dim = 0f;
            if (t < 1.0f) dim = Mathf.SmoothStep(0f, 0.6f, t);
            else if (t < 2.6f) dim = 0.6f;
            else if (t < 4.0f) dim = Mathf.SmoothStep(0.6f, 0f, (t - 2.6f) / 1.4f);
            _dimmer.color = new Color(0, 0, 0, dim);

            // VS pop (0.7-2.4s).
            float vsAlpha = 0f;
            if (t >= 0.6f && t < 2.4f) vsAlpha = Mathf.SmoothStep(0f, 1f, (t - 0.6f) / 0.4f);
            else if (t >= 2.4f && t < 3.0f) vsAlpha = 1f - Mathf.SmoothStep(0f, 1f, (t - 2.4f) / 0.6f);
            _vsText.color = new Color(_vsText.color.r, _vsText.color.g, _vsText.color.b, vsAlpha);

            // Opponent banner slides down 0.4-1.6, holds, slides up 2.6-3.8.
            float oppY = 260f;
            if (t >= 0.4f && t < 1.6f) oppY = Mathf.Lerp(260f, -10f, EaseOutBack((t - 0.4f) / 1.2f));
            else if (t >= 1.6f && t < 2.6f) oppY = -10f;
            else if (t >= 2.6f && t < 3.8f) oppY = Mathf.Lerp(-10f, 260f, (t - 2.6f) / 1.2f);
            _opponentBanner.anchoredPosition = new Vector2(0, oppY);

            // Player banner slides up 0.4-1.6, holds, slides down 2.6-3.8.
            float plY = -260f;
            if (t >= 0.4f && t < 1.6f) plY = Mathf.Lerp(-260f, 10f, EaseOutBack((t - 0.4f) / 1.2f));
            else if (t >= 1.6f && t < 2.6f) plY = 10f;
            else if (t >= 2.6f && t < 3.8f) plY = Mathf.Lerp(10f, -260f, (t - 2.6f) / 1.2f);
            _playerBanner.anchoredPosition = new Vector2(0, plY);

            // Big countdown digits 4.0-5.0 (1 second per digit-ish; we
            // squeeze 3-2-1-GO into the last second of the 5s window).
            // Actually use the full last second pulsing 1 large "GO!"
            // when match starts; before that show whatever digit
            // MatchManager is on.
            var match = MatchManager.I;
            if (match != null && match.Phase == MatchPhase.Countdown)
            {
                float remaining = match.CountdownRemaining;
                int digit = Mathf.CeilToInt(remaining);
                if (digit <= 3 && digit >= 1)
                {
                    _countdownText.text = digit.ToString();
                    float pulse = 1f - (remaining - (digit - 1));
                    float a = Mathf.SmoothStep(0f, 1f, pulse) * (1f - pulse * 0.3f);
                    _countdownText.color = new Color(1f, 0.93f, 0.45f, a);
                    if (digit != _lastCountdownDigit)
                    {
                        _lastCountdownDigit = digit;
                        AudioManager.PlayOneShot("click", Vector3.zero);
                    }
                }
                else
                {
                    _countdownText.color = new Color(1f, 0.93f, 0.45f, 0f);
                }
            }
            else
            {
                // Just dropped into SingleElixir → show GO! once, then fade.
                if (!_goSoundPlayed)
                {
                    _goSoundPlayed = true;
                    AudioManager.PlayOneShot("victory", Vector3.zero);
                }
                _countdownText.text = "GO!";
                float since = Mathf.Max(0f, t - 5f);
                float a = since < 0.6f ? 1f : Mathf.Lerp(1f, 0f, (since - 0.6f) / 0.4f);
                _countdownText.color = new Color(1f, 0.4f, 0.3f, a);
                if (since > 1.2f)
                {
                    Destroy(gameObject);
                }
            }
        }

        // Lightweight overshoot ease for a snappy CR-style banner pop-in.
        static float EaseOutBack(float x)
        {
            x = Mathf.Clamp01(x);
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
        }
    }
}
