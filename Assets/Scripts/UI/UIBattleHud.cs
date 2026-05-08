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
        GameObject _dragGhost;
        Image _dragGhostArt;
        GameObject _placementHint;
        PlacementOverlay _placement;

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

            // Transparent container for the elixir + cards row, sized so cards have
            // their own slot and elixir bar sits ABOVE them (not behind).
            var bottom = new GameObject("BottomBar");
            bottom.transform.SetParent(transform, false);
            var brt = bottom.AddComponent<RectTransform>();
            brt.anchorMin = new Vector2(0, 0);
            brt.anchorMax = new Vector2(1, 0);
            brt.pivot = new Vector2(0.5f, 0f);
            brt.sizeDelta = new Vector2(0, 380);
            brt.anchoredPosition = Vector2.zero;

            // Elixir bar in CR style: drop+number on the left, segmented horizontal
            // fill bar to the right. Sits above the cards (not behind).
            var elixirRow = UIFactory.MakePanel(bottom.transform, "ElixirRow", new Color(0.05f, 0.05f, 0.1f, 0.9f));
            var ert = elixirRow.GetComponent<RectTransform>();
            ert.anchorMin = new Vector2(0, 1);
            ert.anchorMax = new Vector2(1, 1);
            ert.pivot = new Vector2(0.5f, 1f);
            ert.sizeDelta = new Vector2(-32, 70);
            ert.anchoredPosition = new Vector2(0, -6);

            // Big elixir count (CR-style purple drop with white digit)
            var drop = new GameObject("ElixirDrop");
            drop.transform.SetParent(elixirRow.transform, false);
            var drt = drop.AddComponent<RectTransform>();
            drt.anchorMin = new Vector2(0, 0.5f);
            drt.anchorMax = new Vector2(0, 0.5f);
            drt.pivot = new Vector2(0.5f, 0.5f);
            drt.sizeDelta = new Vector2(76, 76);
            drt.anchoredPosition = new Vector2(38, 0);
            var dropImg = drop.AddComponent<Image>();
            dropImg.color = new Color(0.7f, 0.25f, 0.85f);

            _elixirText = UIFactory.MakeText(drop.transform, "ElixirNum", "5", 50, TextAnchor.MiddleCenter);
            var etrt = _elixirText.GetComponent<RectTransform>();
            etrt.anchorMin = Vector2.zero;
            etrt.anchorMax = Vector2.one;
            etrt.offsetMin = etrt.offsetMax = Vector2.zero;
            _elixirText.fontStyle = FontStyle.Bold;
            _elixirText.color = Color.white;
            var elOutline = _elixirText.gameObject.AddComponent<Outline>();
            elOutline.effectColor = new Color(0, 0, 0, 0.85f);
            elOutline.effectDistance = new Vector2(3, -3);

            _elixirBar = UIFactory.MakeSlider(elixirRow.transform, "Bar", new Color(0.95f, 0.4f, 1f));
            var srt = _elixirBar.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0, 0);
            srt.anchorMax = new Vector2(1, 1);
            srt.offsetMin = new Vector2(86, 8);
            srt.offsetMax = new Vector2(-12, -8);

            // Tick marks at each elixir step (10 total)
            for (int i = 1; i < 10; i++)
            {
                var tick = new GameObject($"Tick_{i}");
                tick.transform.SetParent(_elixirBar.transform, false);
                var tRt = tick.AddComponent<RectTransform>();
                tRt.anchorMin = new Vector2(i / 10f, 0.1f);
                tRt.anchorMax = new Vector2(i / 10f, 0.9f);
                tRt.pivot = new Vector2(0.5f, 0.5f);
                tRt.sizeDelta = new Vector2(2, 0);
                tRt.anchoredPosition = Vector2.zero;
                var tImg = tick.AddComponent<Image>();
                tImg.color = new Color(0, 0, 0, 0.5f);
                tImg.raycastTarget = false;
            }

            for (int i = 0; i < 4; i++)
            {
                var slot = UICardSlot.Build(bottom.transform, i);
                slot.OnDragStart += BeginDrag;
                slot.OnDragMove += DragMove;
                slot.OnDragEnd += EndDrag;
                _cardSlots[i] = slot;
            }
            _nextCard = UINextCard.Build(bottom.transform);
            BuildDragGhost();
        }

        void BuildDragGhost()
        {
            _dragGhost = new GameObject("DragGhost");
            _dragGhost.transform.SetParent(transform, false);
            var rt = _dragGhost.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(220, 220);
            rt.anchorMin = rt.anchorMax = new Vector2(0, 0);
            rt.pivot = new Vector2(0.5f, 0.5f);
            _dragGhostArt = _dragGhost.AddComponent<Image>();
            _dragGhostArt.preserveAspect = true;
            _dragGhostArt.raycastTarget = false;
            var cg = _dragGhost.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            cg.alpha = 0.85f;
            _dragGhost.SetActive(false);
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
                    _timerText.color = Color.white;
                }
                else if (match.Phase == MatchPhase.Overtime)
                {
                    // Overtime counts DOWN from OvertimeMaxSeconds so the
                    // player can see how long they have left to break the
                    // tie before the match resolves as a draw.
                    int otLeft = Mathf.Max(0, Mathf.CeilToInt(
                        MatchManager.OvertimeMaxSeconds - match.OvertimeElapsed));
                    _timerText.text = $"OT {otLeft / 60}:{otLeft % 60:00}";
                    _timerText.color = new Color(1f, 0.55f, 0.25f);
                }
                else
                {
                    int sec = Mathf.Max(0, Mathf.CeilToInt(match.TimeRemaining));
                    _timerText.text = $"{sec / 60}:{sec % 60:00}";
                    _timerText.color = Color.white;
                }
            }
            if (_crownsText != null) _crownsText.text = $"{match.PlayerCrowns} - {match.EnemyCrowns}";
            if (_phaseText != null)
            {
                if (match.Phase == MatchPhase.Overtime)
                {
                    // Show the live ramping multiplier so the player knows
                    // the regen has kicked up to x4 / x5 / x6 / x7.
                    int mult = Mathf.RoundToInt(match.OvertimeElixirMultiplier);
                    _phaseText.text = $"Овертайм! x{mult} эликсир";
                }
                else
                {
                    _phaseText.text = match.Phase switch
                    {
                        MatchPhase.Countdown => "Готовься!",
                        MatchPhase.SingleElixir => "",
                        MatchPhase.DoubleElixir => "x2 Эликсир!",
                        MatchPhase.TripleElixir => "x3 Эликсир!!! Овертайм скоро!",
                        _ => ""
                    };
                }
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
            var match = MatchManager.I;
            var card = match != null && match.PlayerDeck != null
                ? match.PlayerDeck.Hand[slot] : null;
            if (_dragGhost != null && _cardSlots[slot] != null)
            {
                _dragGhostArt.sprite = _cardSlots[slot].Art;
                _dragGhostArt.color = Color.white;
                _dragGhost.SetActive(true);
            }
            if (_placement == null) _placement = PlacementOverlay.Create();
            _placement.Show(card);
        }

        void DragMove(int slot, Vector2 screenPos)
        {
            if (_dragGhost != null && _dragGhost.activeSelf)
            {
                var rt = _dragGhost.GetComponent<RectTransform>();
                var canvasRt = _canvas.transform as RectTransform;
                if (canvasRt != null)
                {
                    Vector2 local;
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        canvasRt, screenPos,
                        _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : arenaCamera,
                        out local);
                    rt.anchoredPosition = local;
                }
            }
            // Move the world-space cursor ring to the touch position so the player
            // sees exactly where the unit will spawn / spell will land.
            if (_placement != null && arenaCamera != null)
            {
                var ray = arenaCamera.ScreenPointToRay(screenPos);
                if (TryRaycastPlane(ray, out var worldPoint))
                {
                    var match = MatchManager.I;
                    var card = match != null && match.PlayerDeck != null
                        ? match.PlayerDeck.Hand[slot] : null;
                    bool valid = ArenaController.I != null && card != null
                        && ArenaController.I.IsValidPlacement(Team.Player, worldPoint, card);
                    _placement.SetCursor(worldPoint, valid);
                }
            }
        }

        void EndDrag(int slot, Vector2 screenPos)
        {
            placementMode = false;
            if (_dragGhost != null) _dragGhost.SetActive(false);
            if (_placement != null) _placement.Hide();
            if (_draggingSlot != slot) { _draggingSlot = -1; return; }
            _draggingSlot = -1;
            var match = MatchManager.I;
            if (match == null || arenaCamera == null) return;
            Vector3 worldPoint;
            var ray = arenaCamera.ScreenPointToRay(screenPos);
            if (!TryRaycastPlane(ray, out worldPoint))
            {
                if (Physics.Raycast(ray, out var hit, 100f, ~0, QueryTriggerInteraction.Ignore))
                    worldPoint = hit.point;
                else return;
            }
            // Don't auto-clamp into the player's own half: with broken enemy towers
            // the deploy zone genuinely extends across the river, and clamping
            // would make those drops fail. Bounds clamping for x is still helpful
            // so the unit doesn't end up sliding off the side.
            worldPoint.x = Mathf.Clamp(worldPoint.x, -ArenaController.HalfWidth + 0.3f, ArenaController.HalfWidth - 0.3f);
            worldPoint.z = Mathf.Clamp(worldPoint.z, -ArenaController.HalfLength + 0.3f, ArenaController.HalfLength - 0.3f);
            match.TryDeployPlayer(slot, worldPoint);
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
