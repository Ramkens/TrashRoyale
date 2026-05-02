using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TrashRoyale.Core;
using TrashRoyale.Persistence;
using TrashRoyale.Audio;

namespace TrashRoyale.UI
{
    public class DeckEditor : MonoBehaviour
    {
        PlayerProfile _profile;
        System.Action _onClosed;
        Transform _slotsRoot;
        Transform _gridRoot;

        public static DeckEditor Open(Transform canvas, PlayerProfile profile, System.Action onClosed = null)
        {
            var go = new GameObject("DeckEditor");
            go.transform.SetParent(canvas, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0.92f);
            img.raycastTarget = true;

            var ed = go.AddComponent<DeckEditor>();
            ed._profile = profile;
            ed._onClosed = onClosed;
            ed.Build();
            return ed;
        }

        void Build()
        {
            CardDatabase.EnsureLoaded();
            var title = UIFactory.MakeText(transform, "Title", "КОЛОДА", 70, TextAnchor.MiddleCenter);
            var trt = title.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0, 0.92f);
            trt.anchorMax = new Vector2(1, 0.98f);
            trt.offsetMin = trt.offsetMax = Vector2.zero;
            title.color = Color.white;

            var hint = UIFactory.MakeText(transform, "Hint", "Тапни карту для замены  ·  долго держи для информации", 22, TextAnchor.MiddleCenter);
            var hrt = hint.GetComponent<RectTransform>();
            hrt.anchorMin = new Vector2(0, 0.88f);
            hrt.anchorMax = new Vector2(1, 0.92f);
            hrt.offsetMin = hrt.offsetMax = Vector2.zero;
            hint.color = new Color(1f, 1f, 1f, 0.65f);

            // Slots row
            var slotsPanel = UIFactory.MakePanel(transform, "Slots", new Color(0.05f, 0.08f, 0.18f, 0.92f));
            var sprt = slotsPanel.GetComponent<RectTransform>();
            sprt.anchorMin = new Vector2(0.03f, 0.7f);
            sprt.anchorMax = new Vector2(0.97f, 0.87f);
            sprt.offsetMin = sprt.offsetMax = Vector2.zero;
            _slotsRoot = slotsPanel.transform;

            BuildSlotsRow();

            // Library grid
            var libPanel = UIFactory.MakePanel(transform, "Library", new Color(0.04f, 0.05f, 0.12f, 0.92f));
            var lrt = libPanel.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0.03f, 0.13f);
            lrt.anchorMax = new Vector2(0.97f, 0.69f);
            lrt.offsetMin = lrt.offsetMax = Vector2.zero;
            _gridRoot = libPanel.transform;

            BuildLibraryGrid();

            var save = UIFactory.MakeButton(transform, "Save", "СОХРАНИТЬ", () =>
            {
                AudioManager.PlaySfx("click");
                _profile.Save();
                _onClosed?.Invoke();
                Destroy(gameObject);
            });
            var srt2 = save.GetComponent<RectTransform>();
            srt2.anchorMin = new Vector2(0.06f, 0.03f);
            srt2.anchorMax = new Vector2(0.5f, 0.11f);
            srt2.offsetMin = srt2.offsetMax = Vector2.zero;

            var close = UIFactory.MakeButton(transform, "Close", "ЗАКРЫТЬ", () =>
            {
                AudioManager.PlaySfx("click");
                _onClosed?.Invoke();
                Destroy(gameObject);
            });
            var crt2 = close.GetComponent<RectTransform>();
            crt2.anchorMin = new Vector2(0.52f, 0.03f);
            crt2.anchorMax = new Vector2(0.94f, 0.11f);
            crt2.offsetMin = crt2.offsetMax = Vector2.zero;
        }

        void BuildSlotsRow()
        {
            for (int i = _slotsRoot.childCount - 1; i >= 0; i--) Destroy(_slotsRoot.GetChild(i).gameObject);
            float pad = 0.012f;
            float cellW = (1f - pad * 9f) / 8f;
            for (int i = 0; i < 8; i++)
            {
                int idx = i;
                var card = i < _profile.deck.Count ? CardDatabase.Get(_profile.deck[i]) : null;
                var cell = MakeCardCell(_slotsRoot, "Slot_" + i, card, true,
                    () => SwapSlotWithLibrary(idx),
                    () => { if (card != null) CardInfoPopup.Open(transform, card); });
                var crt = cell.GetComponent<RectTransform>();
                crt.anchorMin = new Vector2(pad + idx * (cellW + pad), 0.05f);
                crt.anchorMax = new Vector2(pad + idx * (cellW + pad) + cellW, 0.95f);
                crt.offsetMin = crt.offsetMax = Vector2.zero;
            }
        }

        void BuildLibraryGrid()
        {
            for (int i = _gridRoot.childCount - 1; i >= 0; i--) Destroy(_gridRoot.GetChild(i).gameObject);
            int columns = 4;
            float padX = 0.015f, padY = 0.018f;
            float cellW = (1f - padX * (columns + 1)) / columns;
            int rows = (CardDatabase.All.Count + columns - 1) / columns;
            float cellH = (1f - padY * (rows + 1)) / Mathf.Max(rows, 1);

            for (int i = 0; i < CardDatabase.All.Count; i++)
            {
                var c = CardDatabase.All[i];
                CardData captured = c;
                int row = i / columns;
                int col = i % columns;
                bool inDeck = _profile.deck.Contains(c.id);
                var cell = MakeCardCell(_gridRoot, "Lib_" + c.id, c, !inDeck,
                    () =>
                    {
                        if (_profile.deck.Contains(captured.id))
                        {
                            CardInfoPopup.Open(transform, captured);
                        }
                        else if (_profile.deck.Count < 8)
                        {
                            _profile.deck.Add(captured.id);
                            BuildSlotsRow();
                            BuildLibraryGrid();
                            AudioManager.PlaySfx("card_play");
                        }
                    },
                    () => CardInfoPopup.Open(transform, captured));
                var crt = cell.GetComponent<RectTransform>();
                crt.anchorMin = new Vector2(padX + col * (cellW + padX), 1f - padY - (row + 1) * cellH - row * padY);
                crt.anchorMax = new Vector2(padX + col * (cellW + padX) + cellW, 1f - padY - row * (cellH + padY));
                crt.offsetMin = crt.offsetMax = Vector2.zero;
            }
        }

        GameObject MakeCardCell(Transform parent, string name, CardData c, bool active, System.Action onTap, System.Action onLongPress)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            var img = go.AddComponent<Image>();
            img.color = active ? new Color(0.13f, 0.18f, 0.32f, 1f) : new Color(0.06f, 0.08f, 0.16f, 1f);
            img.raycastTarget = true;

            if (c != null)
            {
                var art = UIFactory.MakeCardArt(go.transform, c.id);
                var artRt = art.GetComponent<RectTransform>();
                artRt.anchorMin = new Vector2(0.04f, 0.22f);
                artRt.anchorMax = new Vector2(0.96f, 0.96f);
                artRt.offsetMin = artRt.offsetMax = Vector2.zero;
                if (!active) art.color = new Color(0.5f, 0.5f, 0.5f, 1f);

                var costTxt = UIFactory.MakeText(go.transform, "Cost", c.elixirCost.ToString(), 32, TextAnchor.MiddleCenter);
                var crrt = costTxt.GetComponent<RectTransform>();
                crrt.anchorMin = new Vector2(0, 0);
                crrt.anchorMax = new Vector2(1, 0.22f);
                crrt.offsetMin = crrt.offsetMax = Vector2.zero;
                costTxt.color = new Color(1f, 0.55f, 0.95f, 1f);
            }
            else
            {
                var emptyTxt = UIFactory.MakeText(go.transform, "Empty", "+", 80, TextAnchor.MiddleCenter);
                var ert = emptyTxt.GetComponent<RectTransform>();
                ert.anchorMin = Vector2.zero; ert.anchorMax = Vector2.one;
                ert.offsetMin = ert.offsetMax = Vector2.zero;
                emptyTxt.color = new Color(1f, 1f, 1f, 0.4f);
            }

            var press = go.AddComponent<PressDispatcher>();
            press.OnTap = onTap;
            press.OnLongPress = onLongPress;
            return go;
        }

        // When user taps a slot, find first not-in-deck card from library and offer swap; for now we just remove it
        void SwapSlotWithLibrary(int slot)
        {
            if (slot < 0 || slot >= _profile.deck.Count) return;
            _profile.deck.RemoveAt(slot);
            AudioManager.PlaySfx("click");
            BuildSlotsRow();
            BuildLibraryGrid();
        }
    }

    /// <summary>Generic press-dispatcher: distinguishes tap vs long press without any input lib.</summary>
    public class PressDispatcher : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public System.Action OnTap;
        public System.Action OnLongPress;
        public float LongPressTime = 0.45f;
        float _downTime;
        bool _isDown;

        public void OnPointerDown(PointerEventData eventData) { _isDown = true; _downTime = Time.unscaledTime; }
        public void OnPointerExit(PointerEventData eventData) { _isDown = false; }
        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_isDown) return;
            float dt = Time.unscaledTime - _downTime;
            _isDown = false;
            if (dt >= LongPressTime) OnLongPress?.Invoke();
            else OnTap?.Invoke();
        }
    }
}
