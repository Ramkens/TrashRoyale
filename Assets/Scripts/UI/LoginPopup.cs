using UnityEngine;
using UnityEngine.UI;
using TrashRoyale.Net;
using TrashRoyale.Persistence;
using TrashRoyale.Audio;

namespace TrashRoyale.UI
{
    /// <summary>
    /// Email/password login popup for cloud profile sync. Two modes:
    /// register and login. Both rotate to the other via a small toggle
    /// at the bottom. There is also a "Skip" button that drops the
    /// player into offline mode (the local profile is then canon and
    /// won't be synced anywhere).
    ///
    /// Opened from <see cref="MainMenuBootstrap"/> when the user taps
    /// the cloud icon in the player banner.
    /// </summary>
    public class LoginPopup : MonoBehaviour
    {
        public static LoginPopup Open(Transform canvas, System.Action onClosed)
        {
            var go = new GameObject("LoginPopup");
            go.transform.SetParent(canvas, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var dim = go.AddComponent<Image>();
            dim.color = new Color(0, 0, 0, 0.8f);
            dim.raycastTarget = true;
            var pop = go.AddComponent<LoginPopup>();
            pop._onClosed = onClosed;
            pop.Build();
            return pop;
        }

        System.Action _onClosed;
        InputField _emailInput;
        InputField _passwordInput;
        Text _statusText;
        Button _submitBtn;
        Text _submitText;
        Button _toggleBtn;
        Text _toggleText;
        Text _titleText;
        bool _registerMode;
        bool _busy;

        void Build()
        {
            var panel = UIFactory.MakePanel(transform, "Panel", new Color(0.07f, 0.13f, 0.28f, 1f));
            var prt = panel.GetComponent<RectTransform>();
            prt.anchorMin = new Vector2(0.06f, 0.18f);
            prt.anchorMax = new Vector2(0.94f, 0.82f);
            prt.offsetMin = prt.offsetMax = Vector2.zero;
            var btnSp = UIFactory.LoadSprite("UI/btn_gold");
            if (btnSp != null) { panel.sprite = btnSp; panel.type = Image.Type.Sliced; panel.color = new Color(0.08f, 0.12f, 0.28f); }

            _titleText = UIFactory.MakeText(panel.transform, "Title", "ВХОД", 64, TextAnchor.MiddleCenter);
            var trt = _titleText.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0, 0.86f);
            trt.anchorMax = new Vector2(1, 0.97f);
            trt.offsetMin = trt.offsetMax = Vector2.zero;
            _titleText.color = new Color(1f, 0.93f, 0.45f);

            var emailLabel = UIFactory.MakeText(panel.transform, "EmailLabel", "Почта", 32, TextAnchor.MiddleLeft);
            var elrt = emailLabel.GetComponent<RectTransform>();
            elrt.anchorMin = new Vector2(0.08f, 0.74f);
            elrt.anchorMax = new Vector2(0.92f, 0.80f);
            elrt.offsetMin = elrt.offsetMax = Vector2.zero;
            emailLabel.color = Color.white;

            // Smaller font size — at 40 the field could only fit ~15 characters
            // before the text shifted, so a typical email like
            // "predatel586@gmail.com" wouldn't fit. 28 leaves room for ~25.
            _emailInput = UIFactory.MakeInputField(panel.transform, "Email", AuthClient.Email, 28);
            _emailInput.contentType = InputField.ContentType.EmailAddress;
            _emailInput.characterLimit = 64;
            var erit = _emailInput.GetComponent<RectTransform>();
            erit.anchorMin = new Vector2(0.04f, 0.62f);
            erit.anchorMax = new Vector2(0.96f, 0.74f);
            erit.offsetMin = erit.offsetMax = Vector2.zero;

            var pwLabel = UIFactory.MakeText(panel.transform, "PwLabel", "Пароль (≥6 символов)", 32, TextAnchor.MiddleLeft);
            var plrt = pwLabel.GetComponent<RectTransform>();
            plrt.anchorMin = new Vector2(0.08f, 0.54f);
            plrt.anchorMax = new Vector2(0.92f, 0.60f);
            plrt.offsetMin = plrt.offsetMax = Vector2.zero;
            pwLabel.color = Color.white;

            _passwordInput = UIFactory.MakeInputField(panel.transform, "Pw", "", 28);
            _passwordInput.contentType = InputField.ContentType.Password;
            _passwordInput.characterLimit = 64;
            var prrt = _passwordInput.GetComponent<RectTransform>();
            prrt.anchorMin = new Vector2(0.04f, 0.42f);
            prrt.anchorMax = new Vector2(0.96f, 0.54f);
            prrt.offsetMin = prrt.offsetMax = Vector2.zero;

            _statusText = UIFactory.MakeText(panel.transform, "Status", "", 28, TextAnchor.MiddleCenter);
            var srt = _statusText.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0.05f, 0.34f);
            srt.anchorMax = new Vector2(0.95f, 0.42f);
            srt.offsetMin = srt.offsetMax = Vector2.zero;
            _statusText.color = new Color(1f, 0.5f, 0.4f);

            _submitBtn = UIFactory.MakeButton(panel.transform, "Submit", "ВОЙТИ", OnSubmit);
            _submitText = _submitBtn.GetComponentInChildren<Text>();
            var sbrt = _submitBtn.GetComponent<RectTransform>();
            sbrt.anchorMin = new Vector2(0.08f, 0.20f);
            sbrt.anchorMax = new Vector2(0.92f, 0.32f);
            sbrt.offsetMin = sbrt.offsetMax = Vector2.zero;

            _toggleBtn = UIFactory.MakeButton(panel.transform, "Toggle", "Нет аккаунта? Зарегистрироваться", () =>
            {
                _registerMode = !_registerMode;
                RefreshMode();
            });
            _toggleText = _toggleBtn.GetComponentInChildren<Text>();
            _toggleText.fontSize = 26;
            var tbrt = _toggleBtn.GetComponent<RectTransform>();
            tbrt.anchorMin = new Vector2(0.08f, 0.10f);
            tbrt.anchorMax = new Vector2(0.92f, 0.18f);
            tbrt.offsetMin = tbrt.offsetMax = Vector2.zero;

            var skipBtn = UIFactory.MakeButton(panel.transform, "Skip", "ИГРАТЬ БЕЗ ВХОДА", () =>
            {
                AuthClient.OfflineMode = true;
                Destroy(gameObject);
                _onClosed?.Invoke();
            });
            var skbrt = skipBtn.GetComponent<RectTransform>();
            skbrt.anchorMin = new Vector2(0.08f, 0.02f);
            skbrt.anchorMax = new Vector2(0.92f, 0.09f);
            skbrt.offsetMin = skbrt.offsetMax = Vector2.zero;

            RefreshMode();
        }

        void RefreshMode()
        {
            if (_registerMode)
            {
                _titleText.text = "РЕГИСТРАЦИЯ";
                _submitText.text = "СОЗДАТЬ АККАУНТ";
                _toggleText.text = "Уже есть аккаунт? Войти";
            }
            else
            {
                _titleText.text = "ВХОД";
                _submitText.text = "ВОЙТИ";
                _toggleText.text = "Нет аккаунта? Зарегистрироваться";
            }
            _statusText.text = "";
        }

        void OnSubmit()
        {
            if (_busy) return;
            var email = (_emailInput.text ?? "").Trim();
            var password = _passwordInput.text ?? "";
            if (string.IsNullOrEmpty(email) || !email.Contains("@"))
            {
                SetStatus("Некорректная почта", true);
                return;
            }
            if (password.Length < 6)
            {
                SetStatus("Пароль ≥ 6 символов", true);
                return;
            }
            _busy = true;
            SetStatus("...", false);
            AudioManager.PlaySfx("click");
            var coro = _registerMode
                ? AuthClient.Register(email, password, OnAuthDone)
                : AuthClient.Login(email, password, OnAuthDone);
            StartCoroutine(coro);
        }

        void OnAuthDone(AuthClient.Result r)
        {
            _busy = false;
            if (!r.ok)
            {
                SetStatus(TranslateError(r.error), true);
                return;
            }
            SetStatus("Готово!", false);
            // After login: pull cloud profile (overwrites local), then close.
            CloudProfileSync.PullAndOverwriteLocal((pulled) =>
            {
                Destroy(gameObject);
                _onClosed?.Invoke();
            });
        }

        void SetStatus(string s, bool error)
        {
            _statusText.text = s;
            _statusText.color = error ? new Color(1f, 0.45f, 0.35f) : new Color(0.45f, 0.95f, 0.55f);
        }

        static string TranslateError(string code)
        {
            switch (code)
            {
                case "bad_email": return "Некорректная почта";
                case "bad_password": return "Слабый пароль";
                case "email_taken": return "Почта уже занята";
                case "bad_credentials": return "Неверная почта или пароль";
                case "network_error": return "Сервер недоступен";
                default: return string.IsNullOrEmpty(code) ? "Ошибка" : code;
            }
        }
    }
}
