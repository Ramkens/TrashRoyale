using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TrashRoyale.Bootstrap;
using TrashRoyale.Persistence;

namespace TrashRoyale.UI
{
    public class FriendlyBattlePopup : MonoBehaviour
    {
        // Public web base used for share-link mirror.
        // Replace with your relay site once deployed (e.g. https://trashroyale.example.com/join/CODE).
        // Until then, link is informational; the 6-char code is the source of truth.
        const string ShareBase = "https://trashroyale-relay.onrender.com/join/";

        InputField _input;
        Text _statusText;
        Text _codeText;
        Text _linkText;
        GameObject _hostBlock;
        GameObject _joinBlock;
        Button _copyBtn;
        Text _copyBtnLabel;
        string _roomCode;

        public static FriendlyBattlePopup Open(Transform canvas)
        {
            var go = new GameObject("FriendlyPopup");
            go.transform.SetParent(canvas, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var dim = go.AddComponent<Image>();
            dim.color = new Color(0, 0, 0, 0.85f);
            dim.raycastTarget = true;

            var p = go.AddComponent<FriendlyBattlePopup>();
            p.Build();
            return p;
        }

        void Build()
        {
            // Card panel
            var panel = UIFactory.MakePanel(transform, "Panel", new Color(0.07f, 0.13f, 0.28f, 1f));
            var prt = panel.GetComponent<RectTransform>();
            prt.anchorMin = new Vector2(0.05f, 0.18f);
            prt.anchorMax = new Vector2(0.95f, 0.86f);
            prt.offsetMin = prt.offsetMax = Vector2.zero;
            var bg = UIFactory.LoadSprite("UI/menu_bg");
            if (bg != null) { panel.sprite = bg; panel.color = new Color(1f, 1f, 1f, 0.55f); }

            var title = UIFactory.MakeText(panel.transform, "Title", "ДРУЖЕСКИЙ БОЙ", 60, TextAnchor.MiddleCenter);
            var trt = title.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0, 0.88f); trt.anchorMax = new Vector2(1, 0.97f);
            trt.offsetMin = trt.offsetMax = Vector2.zero;
            title.color = new Color(1f, 0.93f, 0.4f);

            // Host block (visible until "Создать комнату" pressed; then shows code+link)
            _hostBlock = new GameObject("HostBlock");
            _hostBlock.transform.SetParent(panel.transform, false);
            var hbr = _hostBlock.AddComponent<RectTransform>();
            hbr.anchorMin = new Vector2(0.04f, 0.42f);
            hbr.anchorMax = new Vector2(0.96f, 0.86f);
            hbr.offsetMin = hbr.offsetMax = Vector2.zero;

            BuildHostBlock(_hostBlock.transform);

            // Join block (always visible at bottom)
            _joinBlock = new GameObject("JoinBlock");
            _joinBlock.transform.SetParent(panel.transform, false);
            var jbr = _joinBlock.AddComponent<RectTransform>();
            jbr.anchorMin = new Vector2(0.04f, 0.04f);
            jbr.anchorMax = new Vector2(0.96f, 0.4f);
            jbr.offsetMin = jbr.offsetMax = Vector2.zero;

            BuildJoinBlock(_joinBlock.transform);

            // Close button (top-right)
            var closeBtn = UIFactory.MakeButton(transform, "Close", "ЗАКРЫТЬ", () => Destroy(gameObject));
            var crt = closeBtn.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.3f, 0.06f);
            crt.anchorMax = new Vector2(0.7f, 0.13f);
            crt.offsetMin = crt.offsetMax = Vector2.zero;

            // Status (single line, fixed position - never overlaps)
            _statusText = UIFactory.MakeText(transform, "Status", "", 28, TextAnchor.MiddleCenter);
            var srt = _statusText.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0.05f, 0.14f);
            srt.anchorMax = new Vector2(0.95f, 0.18f);
            srt.offsetMin = srt.offsetMax = Vector2.zero;
            _statusText.color = new Color(1f, 1f, 1f, 0.9f);
        }

        void BuildHostBlock(Transform parent)
        {
            var bg = UIFactory.MakePanel(parent, "BgHost", new Color(0, 0, 0, 0.35f));
            var bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;
            bg.raycastTarget = false;

            var label = UIFactory.MakeText(parent, "HostLabel", "СОЗДАЙ КОМНАТУ ДЛЯ БОЯ С ДРУГОМ", 28, TextAnchor.MiddleCenter);
            var lrt = label.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0, 0.84f); lrt.anchorMax = new Vector2(1, 0.97f);
            lrt.offsetMin = lrt.offsetMax = Vector2.zero;
            label.color = new Color(1f, 1f, 1f, 0.85f);

            // Code display (single text element, big)
            _codeText = UIFactory.MakeText(parent, "CodeBig", "------", 96, TextAnchor.MiddleCenter);
            var ctrt = _codeText.GetComponent<RectTransform>();
            ctrt.anchorMin = new Vector2(0, 0.46f); ctrt.anchorMax = new Vector2(1, 0.84f);
            ctrt.offsetMin = ctrt.offsetMax = Vector2.zero;
            _codeText.color = new Color(1f, 0.95f, 0.45f);

            // Link display (single text element)
            _linkText = UIFactory.MakeText(parent, "LinkText", "(ссылка появится после создания)", 22, TextAnchor.MiddleCenter);
            var ltr = _linkText.GetComponent<RectTransform>();
            ltr.anchorMin = new Vector2(0, 0.32f); ltr.anchorMax = new Vector2(1, 0.46f);
            ltr.offsetMin = ltr.offsetMax = Vector2.zero;
            _linkText.color = new Color(0.85f, 0.92f, 1f, 0.95f);

            // Two buttons in a row: Создать / Копировать (use smaller font + constrained overflow)
            var hostBtn = UIFactory.MakeButton(parent, "Host", "СОЗДАТЬ", () => HostRoom());
            ShrinkLabel(hostBtn, 38);
            var hbr = hostBtn.GetComponent<RectTransform>();
            hbr.anchorMin = new Vector2(0.02f, 0.04f); hbr.anchorMax = new Vector2(0.49f, 0.3f);
            hbr.offsetMin = hbr.offsetMax = Vector2.zero;

            _copyBtn = UIFactory.MakeButton(parent, "Copy", "КОПИЯ", () => CopyLink());
            ShrinkLabel(_copyBtn, 38);
            var cbr = _copyBtn.GetComponent<RectTransform>();
            cbr.anchorMin = new Vector2(0.51f, 0.04f); cbr.anchorMax = new Vector2(0.98f, 0.3f);
            cbr.offsetMin = cbr.offsetMax = Vector2.zero;
            _copyBtnLabel = _copyBtn.GetComponentInChildren<Text>();
            SetCopyEnabled(false);
        }

        void BuildJoinBlock(Transform parent)
        {
            var bg = UIFactory.MakePanel(parent, "BgJoin", new Color(0, 0, 0, 0.35f));
            var bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;
            bg.raycastTarget = false;

            var label = UIFactory.MakeText(parent, "JoinLabel", "ИЛИ ВВЕДИ КОД ДРУГА", 28, TextAnchor.MiddleCenter);
            var lrt = label.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0, 0.78f); lrt.anchorMax = new Vector2(1, 0.97f);
            lrt.offsetMin = lrt.offsetMax = Vector2.zero;
            label.color = new Color(1f, 1f, 1f, 0.85f);

            _input = UIFactory.MakeInputField(parent, "JoinCode", "", 60);
            var irt = _input.GetComponent<RectTransform>();
            irt.anchorMin = new Vector2(0.05f, 0.42f); irt.anchorMax = new Vector2(0.95f, 0.74f);
            irt.offsetMin = irt.offsetMax = Vector2.zero;
            _input.characterLimit = 6;
            _input.contentType = InputField.ContentType.Alphanumeric;
            if (_input.placeholder is Text plc) plc.text = "ABC123";

            var joinBtn = UIFactory.MakeButton(parent, "Join", "ПРИСОЕДИНИТЬСЯ", () => JoinRoom());
            ShrinkLabel(joinBtn, 42);
            var jbr = joinBtn.GetComponent<RectTransform>();
            jbr.anchorMin = new Vector2(0.05f, 0.06f); jbr.anchorMax = new Vector2(0.95f, 0.38f);
            jbr.offsetMin = jbr.offsetMax = Vector2.zero;
        }

        static void ShrinkLabel(Button btn, int fontSize)
        {
            var t = btn.GetComponentInChildren<Text>();
            if (t == null) return;
            t.fontSize = fontSize;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            t.resizeTextForBestFit = false;
            // Tight to button rect with small inner padding so text never spills outside
            var rt = t.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(8, 6);
            rt.offsetMax = new Vector2(-8, 0);
        }

        void SetCopyEnabled(bool on)
        {
            if (_copyBtn == null) return;
            _copyBtn.interactable = on;
            if (_copyBtnLabel != null)
            {
                _copyBtnLabel.color = on ? Color.white : new Color(1, 1, 1, 0.45f);
            }
        }

        void HostRoom()
        {
            _roomCode = MakeCode();
            string link = ShareBase + _roomCode;
            if (_codeText != null) _codeText.text = _roomCode;
            if (_linkText != null) _linkText.text = link;
            SetCopyEnabled(true);
            // Auto-copy on first generation
            GUIUtility.systemCopyBuffer = link;
            if (_statusText != null) _statusText.text = "Ссылка скопирована! Кинь её другу. Жду подключения...";
        }

        void CopyLink()
        {
            if (string.IsNullOrEmpty(_roomCode)) return;
            string link = ShareBase + _roomCode;
            GUIUtility.systemCopyBuffer = link;
            if (_statusText != null) _statusText.text = "Ссылка снова скопирована: " + link;
        }

        void JoinRoom()
        {
            var code = (_input.text ?? "").Trim().ToUpperInvariant();
            if (code.Length < 4)
            {
                if (_statusText != null) _statusText.text = "Слишком короткий код";
                return;
            }
            if (_statusText != null) _statusText.text = "Подключаюсь к комнате " + code + "...";
            StartBattle(code, false);
        }

        string MakeCode()
        {
            const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var c = new char[6];
            var rnd = new System.Random();
            for (int i = 0; i < 6; i++) c[i] = alphabet[rnd.Next(alphabet.Length)];
            return new string(c);
        }

        void StartBattle(string code, bool isHost)
        {
            var profile = PlayerProfile.Load();
            BattleLauncher.Pending = new BattleLauncher.Request
            {
                isPvE = false,
                playerDeck = new List<string>(profile.deck),
                enemyDeck = new List<string>(profile.deck),
                netRoomCode = code,
                netHost = isHost,
                trophyDelta = 0
            };
            SceneManager.LoadScene("Battle");
        }
    }
}
