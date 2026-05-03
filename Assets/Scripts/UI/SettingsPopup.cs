using UnityEngine;
using UnityEngine.UI;
using TrashRoyale.Audio;
using TrashRoyale.Net;
using TrashRoyale.Persistence;

namespace TrashRoyale.UI
{
    // Settings hub reachable from the gear icon in the main menu.
    // Currently exposes account-binding (so the player can attach an
    // email + password to a profile that started out anonymous) and
    // logout. New settings (audio sliders, language, etc.) can hang
    // off the same panel later — anchors are already broken into rows.
    public class SettingsPopup : MonoBehaviour
    {
        Text _statusText;
        Text _accountSummaryText;
        GameObject _accountBlock;

        public static SettingsPopup Open(Transform canvas, System.Action onClosed = null)
        {
            var go = new GameObject("SettingsPopup");
            go.transform.SetParent(canvas, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var dim = go.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.85f);
            dim.raycastTarget = true;
            var p = go.AddComponent<SettingsPopup>();
            p._onClosed = onClosed;
            p.Build();
            return p;
        }

        System.Action _onClosed;

        void Build()
        {
            var panel = UIFactory.MakePanel(transform, "Panel", new Color(0.07f, 0.13f, 0.28f, 1f));
            var prt = panel.GetComponent<RectTransform>();
            prt.anchorMin = new Vector2(0.05f, 0.10f);
            prt.anchorMax = new Vector2(0.95f, 0.92f);
            prt.offsetMin = prt.offsetMax = Vector2.zero;
            var bg = UIFactory.LoadSprite("UI/menu_bg");
            if (bg != null) { panel.sprite = bg; panel.color = new Color(1f, 1f, 1f, 0.55f); }

            var title = UIFactory.MakeText(panel.transform, "Title", "НАСТРОЙКИ", 60, TextAnchor.MiddleCenter);
            var trt = title.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0, 0.90f);
            trt.anchorMax = new Vector2(1, 0.98f);
            trt.offsetMin = trt.offsetMax = Vector2.zero;
            title.color = new Color(1f, 0.93f, 0.4f);

            BuildAccountBlock(panel.transform);

            var profile = PlayerProfile.Load();
            var idLabel = UIFactory.MakeText(panel.transform, "Id", "ID игрока: " + profile.playerId, 22, TextAnchor.MiddleCenter);
            var ilrt = idLabel.GetComponent<RectTransform>();
            ilrt.anchorMin = new Vector2(0.04f, 0.18f);
            ilrt.anchorMax = new Vector2(0.96f, 0.23f);
            ilrt.offsetMin = ilrt.offsetMax = Vector2.zero;
            idLabel.color = new Color(0.7f, 0.78f, 0.95f);

            var hint = UIFactory.MakeText(panel.transform, "Hint", "Привяжи почту и пароль чтобы потом войти в этот аккаунт с другого устройства.", 20, TextAnchor.MiddleCenter);
            var hrt = hint.GetComponent<RectTransform>();
            hrt.anchorMin = new Vector2(0.04f, 0.12f);
            hrt.anchorMax = new Vector2(0.96f, 0.18f);
            hrt.offsetMin = hrt.offsetMax = Vector2.zero;
            hint.color = new Color(0.85f, 0.85f, 1f);

            _statusText = UIFactory.MakeText(panel.transform, "Status", "", 22, TextAnchor.MiddleCenter);
            var srt = _statusText.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0.04f, 0.06f);
            srt.anchorMax = new Vector2(0.96f, 0.12f);
            srt.offsetMin = srt.offsetMax = Vector2.zero;

            var close = UIFactory.MakeButton(panel.transform, "Close", "ЗАКРЫТЬ", () =>
            {
                AudioManager.PlaySfx("click");
                Destroy(gameObject);
                _onClosed?.Invoke();
            });
            var crt = close.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.30f, 0.01f);
            crt.anchorMax = new Vector2(0.70f, 0.06f);
            crt.offsetMin = crt.offsetMax = Vector2.zero;
        }

        void BuildAccountBlock(Transform parent)
        {
            _accountBlock = new GameObject("AccountBlock");
            _accountBlock.transform.SetParent(parent, false);
            var abr = _accountBlock.AddComponent<RectTransform>();
            abr.anchorMin = new Vector2(0.04f, 0.30f);
            abr.anchorMax = new Vector2(0.96f, 0.88f);
            abr.offsetMin = abr.offsetMax = Vector2.zero;

            var bg = _accountBlock.AddComponent<Image>();
            bg.color = new Color(0.04f, 0.07f, 0.18f, 0.85f);

            var heading = UIFactory.MakeText(_accountBlock.transform, "Heading", "АККАУНТ", 38, TextAnchor.MiddleLeft);
            var hrt = heading.GetComponent<RectTransform>();
            hrt.anchorMin = new Vector2(0.05f, 0.83f);
            hrt.anchorMax = new Vector2(0.95f, 0.96f);
            hrt.offsetMin = hrt.offsetMax = Vector2.zero;
            heading.color = new Color(1f, 0.9f, 0.4f);

            _accountSummaryText = UIFactory.MakeText(_accountBlock.transform, "Summary", "", 24, TextAnchor.UpperLeft);
            var srt = _accountSummaryText.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0.05f, 0.62f);
            srt.anchorMax = new Vector2(0.95f, 0.83f);
            srt.offsetMin = srt.offsetMax = Vector2.zero;
            _accountSummaryText.color = Color.white;

            RefreshSummary();

            if (AuthClient.IsLoggedIn)
            {
                var logoutBtn = UIFactory.MakeButton(_accountBlock.transform, "Logout", "ВЫЙТИ ИЗ АККАУНТА", () =>
                {
                    AudioManager.PlaySfx("click");
                    AuthClient.Logout();
                    SetStatus("Выход выполнен", false);
                    Destroy(_accountBlock);
                    BuildAccountBlock(transform.GetChild(0));
                });
                var lbrt = logoutBtn.GetComponent<RectTransform>();
                lbrt.anchorMin = new Vector2(0.10f, 0.10f);
                lbrt.anchorMax = new Vector2(0.90f, 0.40f);
                lbrt.offsetMin = lbrt.offsetMax = Vector2.zero;
            }
            else
            {
                var bindBtn = UIFactory.MakeButton(_accountBlock.transform, "Bind", "ПРИВЯЗАТЬ ПОЧТУ + ПАРОЛЬ", () =>
                {
                    AudioManager.PlaySfx("click");
                    LoginPopup.Open(transform.parent, () =>
                    {
                        Destroy(_accountBlock);
                        BuildAccountBlock(transform.GetChild(0));
                    });
                });
                var bbrt = bindBtn.GetComponent<RectTransform>();
                bbrt.anchorMin = new Vector2(0.10f, 0.30f);
                bbrt.anchorMax = new Vector2(0.90f, 0.55f);
                bbrt.offsetMin = bbrt.offsetMax = Vector2.zero;

                var why = UIFactory.MakeText(_accountBlock.transform, "Why", "Сейчас прогресс хранится только на этом устройстве. Привяжи аккаунт чтобы зайти с того же ника на телефоне/планшете.", 18, TextAnchor.UpperLeft);
                var wrt = why.GetComponent<RectTransform>();
                wrt.anchorMin = new Vector2(0.05f, 0.05f);
                wrt.anchorMax = new Vector2(0.95f, 0.30f);
                wrt.offsetMin = wrt.offsetMax = Vector2.zero;
                why.color = new Color(0.85f, 0.85f, 1f, 1f);
            }
        }

        void RefreshSummary()
        {
            if (_accountSummaryText == null) return;
            if (AuthClient.IsLoggedIn)
            {
                _accountSummaryText.text = "Привязан: " + AuthClient.Email + "\nПрогресс синкается в облако.";
                _accountSummaryText.color = new Color(0.6f, 1f, 0.6f);
            }
            else
            {
                _accountSummaryText.text = "Аккаунт пока не привязан.\nИграешь как гость.";
                _accountSummaryText.color = new Color(1f, 0.85f, 0.5f);
            }
        }

        void SetStatus(string msg, bool error)
        {
            if (_statusText == null) return;
            _statusText.text = msg;
            _statusText.color = error ? new Color(1f, 0.45f, 0.35f) : new Color(0.6f, 1f, 0.6f);
        }
    }
}
