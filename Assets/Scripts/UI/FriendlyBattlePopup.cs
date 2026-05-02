using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TrashRoyale.Bootstrap;
using TrashRoyale.Net;
using TrashRoyale.Persistence;

namespace TrashRoyale.UI
{
    public class FriendlyBattlePopup : MonoBehaviour
    {
        InputField _input;
        Text _status;
        string _roomCode;

        public static FriendlyBattlePopup Open(Transform canvas)
        {
            var go = new GameObject("FriendlyPopup");
            go.transform.SetParent(canvas, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0.88f);
            // Block clicks behind
            go.AddComponent<Button>();

            var p = go.AddComponent<FriendlyBattlePopup>();
            p.Build();
            return p;
        }

        void Build()
        {
            var title = UIFactory.MakeText(transform, "Title", "Дружеский бой", 60, TextAnchor.MiddleCenter);
            var trt = title.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0, 0.78f); trt.anchorMax = new Vector2(1, 0.88f);
            trt.offsetMin = trt.offsetMax = Vector2.zero;
            title.color = new Color(1f, 0.9f, 0.3f);
            title.fontStyle = FontStyle.Bold;

            _status = UIFactory.MakeText(transform, "Status", "Создай комнату или введи код друга", 32, TextAnchor.MiddleCenter);
            var srt = _status.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0.05f, 0.66f); srt.anchorMax = new Vector2(0.95f, 0.74f);
            srt.offsetMin = srt.offsetMax = Vector2.zero;

            var inputGo = new GameObject("Input");
            inputGo.transform.SetParent(transform, false);
            var irt = inputGo.AddComponent<RectTransform>();
            irt.anchorMin = new Vector2(0.15f, 0.54f); irt.anchorMax = new Vector2(0.85f, 0.62f);
            irt.offsetMin = irt.offsetMax = Vector2.zero;
            var inputBg = inputGo.AddComponent<Image>();
            inputBg.color = new Color(1, 1, 1, 0.95f);
            _input = inputGo.AddComponent<InputField>();

            var textArea = new GameObject("TextArea");
            textArea.transform.SetParent(inputGo.transform, false);
            var tart = textArea.AddComponent<RectTransform>();
            tart.anchorMin = Vector2.zero; tart.anchorMax = Vector2.one;
            tart.offsetMin = new Vector2(20, 8); tart.offsetMax = new Vector2(-20, -8);

            var text = UIFactory.MakeText(textArea.transform, "Text", "", 50, TextAnchor.MiddleLeft);
            text.color = Color.black;
            text.supportRichText = false;
            var rt2 = text.GetComponent<RectTransform>();
            rt2.anchorMin = Vector2.zero; rt2.anchorMax = Vector2.one;
            rt2.offsetMin = rt2.offsetMax = Vector2.zero;

            var placeholder = UIFactory.MakeText(textArea.transform, "Placeholder", "ABC123", 50, TextAnchor.MiddleLeft);
            placeholder.color = new Color(0, 0, 0, 0.3f);
            placeholder.fontStyle = FontStyle.Italic;
            var prt = placeholder.GetComponent<RectTransform>();
            prt.anchorMin = Vector2.zero; prt.anchorMax = Vector2.one;
            prt.offsetMin = prt.offsetMax = Vector2.zero;

            _input.targetGraphic = inputBg;
            _input.textComponent = text;
            _input.placeholder = placeholder;
            _input.text = "";
            _input.characterLimit = 6;
            _input.contentType = InputField.ContentType.Alphanumeric;

            var hostBtn = UIFactory.MakeButton(transform, "Host", "Создать комнату", () => HostRoom(), new Color(0.3f, 0.7f, 1f));
            var hrt = hostBtn.GetComponent<RectTransform>();
            hrt.anchorMin = new Vector2(0.18f, 0.4f); hrt.anchorMax = new Vector2(0.82f, 0.48f);
            hrt.offsetMin = hrt.offsetMax = Vector2.zero;

            var joinBtn = UIFactory.MakeButton(transform, "Join", "Присоединиться", () => JoinRoom(), new Color(0.3f, 0.85f, 0.3f));
            var jrt = joinBtn.GetComponent<RectTransform>();
            jrt.anchorMin = new Vector2(0.18f, 0.3f); jrt.anchorMax = new Vector2(0.82f, 0.38f);
            jrt.offsetMin = jrt.offsetMax = Vector2.zero;

            var closeBtn = UIFactory.MakeButton(transform, "Close", "Закрыть", () => Destroy(gameObject), new Color(0.6f, 0.3f, 0.3f));
            var crt = closeBtn.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.18f, 0.18f); crt.anchorMax = new Vector2(0.82f, 0.26f);
            crt.offsetMin = crt.offsetMax = Vector2.zero;
        }

        void HostRoom()
        {
            _roomCode = MakeCode();
            _status.text = $"Код твоей комнаты: {_roomCode}\nПередай другу. Жду подключения...";
            StartBattle(_roomCode, true);
        }

        void JoinRoom()
        {
            var code = (_input.text ?? "").Trim().ToUpperInvariant();
            if (code.Length < 4)
            {
                _status.text = "Слишком короткий код";
                return;
            }
            _status.text = $"Подключаюсь к комнате {code}...";
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
