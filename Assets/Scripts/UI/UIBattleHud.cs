using UnityEngine;
using UnityEngine.UI;
using TrashRoyale.Core;
using TrashRoyale.Match;
using TrashRoyale.Combat;

namespace TrashRoyale.UI
{
    public class UIBattleHud : MonoBehaviour
    {
        Slider _elixirBar;
        Text _elixirText;
        Text _timerText;
        Text _crownsText;
        Text _phaseText;
        UICardSlot[] _cardSlots = new UICardSlot[4];
        UINextCard _nextCard;
        Canvas _canvas;
        GameObject _endScreen;
        Text _endText;

        public Camera arenaCamera;
        public bool placementMode { get; private set; }
        int _draggingSlot = -1;
        Vector2 _dragPos;

        public static UIBattleHud Build(Camera cam)
        {
            var go = new GameObject("BattleHUD");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            go.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 1f;
            go.AddComponent<GraphicRaycaster>();
            var hud = go.AddComponent<UIBattleHud>();
            hud._canvas = canvas;
            hud.arenaCamera = cam;
            hud.BuildContent();
            return hud;
        }

        void BuildContent()
        {
            var topBar = UIFactory.MakePanel(transform, "TopBar", new Color(0, 0, 0, 0.4f));
            var trt = topBar.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0, 1);
            trt.anchorMax = new Vector2(1, 1);
            trt.pivot = new Vector2(0.5f, 1f);
            trt.sizeDelta = new Vector2(0, 120);
            trt.anchoredPosition = Vector2.zero;

            _timerText = UIFactory.MakeText(topBar.transform, "Timer", "3:00", 56, TextAnchor.MiddleCenter);
            var rtTimer = _timerText.GetComponent<RectTransform>();
            rtTimer.anchorMin = new Vector2(0.5f, 0.5f);
            rtTimer.anchorMax = new Vector2(0.5f, 0.5f);
            rtTimer.sizeDelta = new Vector2(220, 70);
            rtTimer.anchoredPosition = Vector2.zero;

            _crownsText = UIFactory.MakeText(topBar.transform, "Crowns", "0 - 0", 36, TextAnchor.MiddleCenter);
            var rtCrowns = _crownsText.GetComponent<RectTransform>();
            rtCrowns.anchorMin = new Vector2(0.5f, 0.5f);
            rtCrowns.anchorMax = new Vector2(0.5f, 0.5f);
            rtCrowns.sizeDelta = new Vector2(280, 50);
            rtCrowns.anchoredPosition = new Vector2(0, -50);

            _phaseText = UIFactory.MakeText(transform, "Phase", "", 32, TextAnchor.MiddleCenter);
            var rtPhase = _phaseText.GetComponent<RectTransform>();
            rtPhase.anchorMin = new Vector2(0.5f, 1f);
            rtPhase.anchorMax = new Vector2(0.5f, 1f);
            rtPhase.sizeDelta = new Vector2(600, 60);
            rtPhase.anchoredPosition = new Vector2(0, -140);
            _phaseText.color = new Color(1f, 0.85f, 0.4f);

            var bottom = UIFactory.MakePanel(transform, "BottomBar", new Color(0, 0, 0, 0.55f));
            var brt = bottom.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0, 0);
            brt.anchorMax = new Vector2(1, 0);
            brt.pivot = new Vector2(0.5f, 0f);
            brt.sizeDelta = new Vector2(0, 320);
            brt.anchoredPosition = Vector2.zero;

            var elixirRow = UIFactory.MakePanel(bottom.transform, "ElixirRow", new Color(0.1f, 0.1f, 0.18f, 0.8f));
            var ert = elixirRow.GetComponent<RectTransform>();
            ert.anchorMin = new Vector2(0, 1);
            ert.anchorMax = new Vector2(1, 1);
            ert.pivot = new Vector2(0.5f, 1f);
            ert.sizeDelta = new Vector2(-32, 40);
            ert.anchoredPosition = new Vector2(0, -8);

            _elixirBar = UIFactory.MakeSlider(elixirRow.transform, "Bar", new Color(0.95f, 0.4f, 1f));
            var srt = _elixirBar.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0, 0);
            srt.anchorMax = new Vector2(1, 1);
            srt.offsetMin = new Vector2(8, 6);
            srt.offsetMax = new Vector2(-100, -6);

            _elixirText = UIFactory.MakeText(elixirRow.transform, "ElixirNum", "5", 36, TextAnchor.MiddleCenter);
            var etrt = _elixirText.GetComponent<RectTransform>();
            etrt.anchorMin = new Vector2(1, 0);
            etrt.anchorMax = new Vector2(1, 1);
            etrt.pivot = new Vector2(1, 0.5f);
            etrt.sizeDelta = new Vector2(96, 0);
            etrt.anchoredPosition = new Vector2(-6, 0);

            for (int i = 0; i < 4; i++)
            {
                var slot = UICardSlot.Build(bottom.transform, i);
                slot.OnDragStart += BeginDrag;
                slot.OnDragEnd += EndDrag;
                _cardSlots[i] = slot;
            }
            _nextCard = UINextCard.Build(bottom.transform);
        }

        void Update()
        {
            var match = MatchManager.I;
            if (match == null) return;

            if (_elixirBar != null) _elixirBar.value = match.PlayerElixir.Current / ElixirManager.Max;
            if (_elixirText != null) _elixirText.text = Mathf.FloorToInt(match.PlayerElixir.Current).ToString();

            if (_timerText != null)
            {
                if (match.Phase == MatchPhase.Countdown)
                {
                    _timerText.text = Mathf.CeilToInt(match.CountdownRemaining).ToString();
                }
                else
                {
                    int sec = Mathf.Max(0, Mathf.CeilToInt(match.TimeRemaining));
                    _timerText.text = $"{sec / 60}:{sec % 60:00}";
                }
            }
            if (_crownsText != null) _crownsText.text = $"{match.PlayerCrowns} - {match.EnemyCrowns}";
            if (_phaseText != null)
            {
                _phaseText.text = match.Phase switch
                {
                    MatchPhase.Countdown => "Готовься!",
                    MatchPhase.SingleElixir => "",
                    MatchPhase.DoubleElixir => "x2 Эликсир!",
                    MatchPhase.TripleElixir => "x3 Эликсир!!! Овертайм скоро!",
                    MatchPhase.Overtime => "Овертайм!",
                    _ => ""
                };
            }

            for (int i = 0; i < 4; i++)
            {
                var c = match.PlayerDeck != null ? match.PlayerDeck.Hand[i] : null;
                _cardSlots[i].SetCard(c, match.PlayerElixir.Current >= (c?.elixirCost ?? 0));
            }
            _nextCard.SetCard(match.PlayerDeck?.NextCard);
        }

        void BeginDrag(int slot)
        {
            placementMode = true;
            _draggingSlot = slot;
        }

        void EndDrag(int slot, Vector2 screenPos)
        {
            placementMode = false;
            if (_draggingSlot != slot) { _draggingSlot = -1; return; }
            _draggingSlot = -1;
            var match = MatchManager.I;
            if (match == null || arenaCamera == null) return;
            var ray = arenaCamera.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out var hit, 100f, ~0, QueryTriggerInteraction.Ignore))
            {
                match.TryDeployPlayer(slot, hit.point);
            }
            else if (TryRaycastPlane(ray, out var pt))
            {
                match.TryDeployPlayer(slot, pt);
            }
        }

        bool TryRaycastPlane(Ray ray, out Vector3 point)
        {
            var plane = new Plane(Vector3.up, Vector3.zero);
            if (plane.Raycast(ray, out var enter)) { point = ray.GetPoint(enter); return true; }
            point = Vector3.zero; return false;
        }

        public void ShowEndScreen(string msg)
        {
            if (_endScreen != null) Destroy(_endScreen);
            _endScreen = UIFactory.MakePanel(transform, "EndScreen", new Color(0, 0, 0, 0.85f)).gameObject;
            var rt = _endScreen.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            _endText = UIFactory.MakeText(_endScreen.transform, "End", msg, 80, TextAnchor.MiddleCenter);
            var trt = _endText.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0.1f, 0.55f);
            trt.anchorMax = new Vector2(0.9f, 0.85f);
            trt.offsetMin = trt.offsetMax = Vector2.zero;
            _endText.color = new Color(1f, 0.9f, 0.3f);

            var btn = UIFactory.MakeButton(_endScreen.transform, "Continue", "В меню", () =>
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("Main");
            });
            var brt = btn.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0.25f, 0.25f);
            brt.anchorMax = new Vector2(0.75f, 0.4f);
            brt.offsetMin = brt.offsetMax = Vector2.zero;
        }
    }
}
