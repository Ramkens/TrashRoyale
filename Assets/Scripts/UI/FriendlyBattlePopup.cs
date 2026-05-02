using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using TrashRoyale.Bootstrap;
using TrashRoyale.Net;
using TrashRoyale.Persistence;

namespace TrashRoyale.UI
{
    public class FriendlyBattlePopup : MonoBehaviour
    {
        TMP_InputField _input;
        TMP_Text _status;
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

            var p = go.AddComponent<FriendlyBattlePopup>();
            p.Build();
            return p;
        }

        void Build()
        {
            var title = UIFactory.MakeText(transform, "Title", "Дружеский бой", 60, TextAlignmentOptions.Center);
            var trt = title.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0, 0.78f); trt.anchorMax = new Vector2(1, 0.88f);
            trt.offsetMin = trt.offsetMax = Vector2.zero;
            title.color = new Color(1f, 0.9f, 0.3f);

            _status = UIFactory.MakeText(transform, "Status", "Создай комнату или введи код друга", 32, TextAlignmentOptions.Center);
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
            _input = inputGo.AddComponent<TMP_InputField>();
            var textArea = new GameObject("TextArea");
            textArea.transform.SetParent(inputGo.transform, false);
            var tart = textArea.AddComponent<RectTransform>();
            tart.anchorMin = Vector2.zero; tart.anchorMax = Vector2.one;
            tart.offsetMin = new Vector2(20, 8); tart.offsetMax = new Vector2(-20, -8);
            var text = UIFactory.MakeText(textArea.transform, "Text", "", 50, TextAlignmentOptions.Left);
            text.color = Color.black;
            var rt2 = text.GetComponent<RectTransform>();
            rt2.anchorMin = Vector2.zero; rt2.anchorMax = Vector2.one;
            rt2.offsetMin = rt2.offsetMax = Vector2.zero;
            _input.targetGraphic = inputBg;
            _input.textViewport = tart;
            _input.textComponent = text;
            _input.text = "";
            _input.characterLimit = 6;
            _input.contentType = TMP_InputField.ContentType.Alphanumeric;
            ((TextMeshProUGUI)text).richText = false;

            var hostBtn = UIFactory.MakeButton(transform, "Host", "Создать комнату", () => HostRoom());
            var hrt = hostBtn.GetComponent<RectTransform>();
            hrt.anchorMin = new Vector2(0.18f, 0.4f); hrt.anchorMax = new Vector2(0.82f, 0.48f);
            hrt.offsetMin = hrt.offsetMax = Vector2.zero;
            hostBtn.image.color = new Color(0.3f, 0.7f, 1f);

            var joinBtn = UIFactory.MakeButton(transform, "Join", "Присоединиться", () => JoinRoom());
            var jrt = joinBtn.GetComponent<RectTransform>();
            jrt.anchorMin = new Vector2(0.18f, 0.3f); jrt.anchorMax = new Vector2(0.82f, 0.38f);
            jrt.offsetMin = jrt.offsetMax = Vector2.zero;
            joinBtn.image.color = new Color(0.3f, 0.85f, 0.3f);

            var closeBtn = UIFactory.MakeButton(transform, "Close", "Закрыть", () => Destroy(gameObject));
            var crt = closeBtn.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.18f, 0.18f); crt.anchorMax = new Vector2(0.82f, 0.26f);
            crt.offsetMin = crt.offsetMax = Vector2.zero;
            closeBtn.image.color = new Color(0.6f, 0.3f, 0.3f);
        }

        void HostRoom()
        {
            _roomCode = MakeCode();
            _status.text = $"Код твоей комнаты: <b>{_roomCode}</b>\nПередай другу. Жду подключения...";
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
            const string A = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
            var s = new System.Text.StringBuilder(6);
            for (int i = 0; i < 6; i++) s.Append(A[Random.Range(0, A.Length)]);
            return s.ToString();
        }

        void StartBattle(string code, bool host)
        {
            var profile = PlayerProfile.Load();
            BattleLauncher.Pending = new BattleLauncher.Request
            {
                isPvE = false,
                playerDeck = new List<string>(profile.deck),
                netRoomCode = code,
                netHost = host,
                trophyDelta = 0
            };
            SceneManager.LoadScene("Battle");
        }
    }
}
