using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TrashRoyale.Audio;
using TrashRoyale.Bootstrap;
using TrashRoyale.Net;
using TrashRoyale.Persistence;

namespace TrashRoyale.UI
{
    /// <summary>
    /// "Ожидание соперника" lobby. Both host and guest pass through it
    /// after creating / joining a room — the popup opens a WebSocket to
    /// the relay, waits for the server's <c>{"type":"peer","status":"ready"}</c>
    /// broadcast (sent when both slots are filled), then closes the
    /// preview connection and hands off to BattleLauncher.
    ///
    /// The relay's "linger" window keeps the host's slot reserved for a
    /// few seconds after we drop the lobby WebSocket, so the Battle
    /// scene's NetMatchSync can reclaim it cleanly.
    /// </summary>
    public class WaitingRoomPopup : MonoBehaviour
    {
        public static WaitingRoomPopup Open(Transform canvas, string roomCode, bool isHost)
        {
            var go = new GameObject("WaitingRoomPopup");
            go.transform.SetParent(canvas, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var dim = go.AddComponent<Image>();
            dim.color = new Color(0, 0, 0, 0.85f);
            dim.raycastTarget = true;

            var pop = go.AddComponent<WaitingRoomPopup>();
            pop._roomCode = roomCode;
            pop._isHost = isHost;
            pop.Build();
            pop.StartCoroutine(pop.WaitForPeer());
            return pop;
        }

        const string ShareBase = "https://trashroyale-relay.onrender.com/play/";

        string _roomCode;
        bool _isHost;
        WebSocketClient _ws;
        bool _peerReady;
        bool _cancelled;
        Text _statusText;
        Text _codeText;

        void Build()
        {
            var panel = UIFactory.MakePanel(transform, "Panel", new Color(0.07f, 0.13f, 0.28f, 1f));
            var prt = panel.GetComponent<RectTransform>();
            prt.anchorMin = new Vector2(0.08f, 0.20f);
            prt.anchorMax = new Vector2(0.92f, 0.80f);
            prt.offsetMin = prt.offsetMax = Vector2.zero;
            var bg = UIFactory.LoadSprite("UI/btn_gold");
            if (bg != null) { panel.sprite = bg; panel.type = Image.Type.Sliced; panel.color = new Color(0.10f, 0.16f, 0.34f); }

            string title = _isHost ? "ОЖИДАНИЕ СОПЕРНИКА" : "ПОДКЛЮЧЕНИЕ";
            var titleT = UIFactory.MakeText(panel.transform, "Title", title, 56, TextAnchor.MiddleCenter);
            var trt = titleT.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0, 0.78f); trt.anchorMax = new Vector2(1, 0.95f);
            trt.offsetMin = trt.offsetMax = Vector2.zero;
            titleT.color = new Color(1f, 0.93f, 0.45f);

            // Room code in big chunky letters so it's easy to read out loud.
            _codeText = UIFactory.MakeText(panel.transform, "Code", _roomCode, 100, TextAnchor.MiddleCenter);
            var crt = _codeText.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.05f, 0.50f);
            crt.anchorMax = new Vector2(0.95f, 0.78f);
            crt.offsetMin = crt.offsetMax = Vector2.zero;
            _codeText.color = Color.white;

            // Status line — animates dots while we wait.
            _statusText = UIFactory.MakeText(panel.transform, "Status",
                _isHost ? "Жду второго игрока..." : "Подключаюсь к комнате...",
                32, TextAnchor.MiddleCenter);
            var srt = _statusText.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0.05f, 0.30f);
            srt.anchorMax = new Vector2(0.95f, 0.50f);
            srt.offsetMin = srt.offsetMax = Vector2.zero;
            _statusText.color = new Color(0.85f, 0.85f, 1f, 1f);

            if (_isHost)
            {
                var copyBtn = UIFactory.MakeButton(panel.transform, "Copy", "СКОПИРОВАТЬ ССЫЛКУ", () =>
                {
                    AudioManager.PlaySfx("click");
                    GUIUtility.systemCopyBuffer = ShareBase + _roomCode;
                    if (_statusText != null) _statusText.text = "Ссылка скопирована! Кинь её другу.";
                });
                var copyrt = copyBtn.GetComponent<RectTransform>();
                copyrt.anchorMin = new Vector2(0.10f, 0.16f);
                copyrt.anchorMax = new Vector2(0.90f, 0.27f);
                copyrt.offsetMin = copyrt.offsetMax = Vector2.zero;
            }

            var cancelBtn = UIFactory.MakeButton(panel.transform, "Cancel", "ОТМЕНА", () =>
            {
                AudioManager.PlaySfx("click");
                _cancelled = true;
                if (_ws != null) try { _ws.Close(); } catch { }
                Destroy(gameObject);
            });
            var canRT = cancelBtn.GetComponent<RectTransform>();
            canRT.anchorMin = new Vector2(0.30f, 0.04f);
            canRT.anchorMax = new Vector2(0.70f, 0.14f);
            canRT.offsetMin = canRT.offsetMax = Vector2.zero;
        }

        IEnumerator WaitForPeer()
        {
            string url = NetConfig.RelayUrl;
            if (string.IsNullOrEmpty(url))
            {
                if (_statusText != null) _statusText.text = "Сервер не настроен. Нажми ОТМЕНА.";
                yield break;
            }

            _ws = new WebSocketClient();
            _ws.OnMessage = OnNetMessage;
            _ws.OnOpen = () =>
            {
                if (_statusText != null)
                    _statusText.text = _isHost ? "Подключено. Жду соперника..." : "Подключено. Готовлю бой...";
            };
            _ws.OnClose = (code, msg) =>
            {
                if (!_peerReady && !_cancelled && _statusText != null)
                    _statusText.text = "Связь оборвалась. Жми ОТМЕНА и попробуй ещё раз.";
            };
            string role = _isHost ? "host" : "guest";
            _ws.Connect($"{url}?room={_roomCode}&role={role}");

            // Animate dots in the status text while waiting.
            float lastTick = Time.unscaledTime;
            int dots = 0;
            string baseStatus = _isHost ? "Жду соперника" : "Подключаюсь";
            while (!_peerReady && !_cancelled)
            {
                _ws.Update();
                if (Time.unscaledTime - lastTick > 0.5f)
                {
                    lastTick = Time.unscaledTime;
                    dots = (dots + 1) % 4;
                    if (_statusText != null && _statusText.text.StartsWith(baseStatus))
                    {
                        _statusText.text = baseStatus + new string('.', dots);
                    }
                }
                yield return null;
            }
            if (_cancelled) yield break;

            // Both peers connected — show countdown then close lobby WS
            // and hand off to Battle scene. NetMatchSync will reclaim
            // the room slot using the relay's linger window.
            if (_statusText != null) _statusText.text = "Соперник найден! Старт через 3...";
            for (int i = 3; i >= 1; i--)
            {
                if (_statusText != null) _statusText.text = "Старт через " + i + "...";
                AudioManager.PlaySfx("click");
                yield return new WaitForSecondsRealtime(0.7f);
            }
            try { _ws.Close(); } catch { }
            StartBattle();
        }

        void OnNetMessage(string json)
        {
            // Server broadcasts {"type":"peer","status":"ready"} once both
            // host and guest are connected. That's our trigger to start.
            if (json.IndexOf("\"status\":\"ready\"") >= 0)
            {
                _peerReady = true;
            }
        }

        void StartBattle()
        {
            var profile = PlayerProfile.Load();
            BattleLauncher.Pending = new BattleLauncher.Request
            {
                isPvE = false,
                playerDeck = new System.Collections.Generic.List<string>(profile.deck),
                enemyDeck = new System.Collections.Generic.List<string>(profile.deck),
                netRoomCode = _roomCode,
                netHost = _isHost,
                trophyDelta = 0,
            };
            SceneManager.LoadScene("Battle");
        }

        void OnDestroy()
        {
            if (_ws != null) try { _ws.Close(); } catch { }
        }
    }
}
