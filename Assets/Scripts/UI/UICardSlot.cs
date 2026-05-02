using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TrashRoyale.Core;

namespace TrashRoyale.UI
{
    /// <summary>
    /// CR-style hand card. Just card art + elixir cost in a corner bubble. No
    /// description box, no extra text overlay (the art already shows what it is).
    /// During drag the slot fades out and a floating ghost follows the finger so the
    /// player gets visual feedback that the drag is registered.
    /// </summary>
    public class UICardSlot : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public int slotIndex;
        Image _bg;
        Image _art;
        Image _costBubble;
        Text _cost;
        CanvasGroup _slotCanvas;
        CardData _card;
        bool _affordable;
        public Action<int> OnDragStart;
        public Action<int, Vector2> OnDragEnd;
        public Action<int, Vector2> OnDragMove;

        public static UICardSlot Build(Transform parent, int idx)
        {
            var go = new GameObject($"CardSlot_{idx}");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            float w = 220f;
            float h = 280f;
            float gap = 16f;
            float total = w * 4 + gap * 3;
            float startX = -total / 2 + w / 2;
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(startX + idx * (w + gap), h / 2 + 12f);

            // Frame
            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.85f, 0.7f, 0.4f);

            // Art fills frame except for tiny border
            var art = new GameObject("Art");
            art.transform.SetParent(go.transform, false);
            var artRt = art.AddComponent<RectTransform>();
            artRt.anchorMin = new Vector2(0.04f, 0.04f);
            artRt.anchorMax = new Vector2(0.96f, 0.96f);
            artRt.offsetMin = artRt.offsetMax = Vector2.zero;
            var artImg = art.AddComponent<Image>();
            artImg.color = new Color(0.15f, 0.18f, 0.3f);
            artImg.preserveAspect = true;
            artImg.raycastTarget = false;

            // Elixir cost bubble in top-left corner
            var costBubble = new GameObject("CostBubble");
            costBubble.transform.SetParent(go.transform, false);
            var cbRt = costBubble.AddComponent<RectTransform>();
            cbRt.anchorMin = new Vector2(0, 1);
            cbRt.anchorMax = new Vector2(0, 1);
            cbRt.pivot = new Vector2(0.5f, 0.5f);
            cbRt.sizeDelta = new Vector2(72, 72);
            cbRt.anchoredPosition = new Vector2(20, -20);
            var cbImg = costBubble.AddComponent<Image>();
            cbImg.color = new Color(0.85f, 0.3f, 0.95f);

            var costText = new GameObject("CostText");
            costText.transform.SetParent(costBubble.transform, false);
            var ctRt = costText.AddComponent<RectTransform>();
            ctRt.anchorMin = Vector2.zero;
            ctRt.anchorMax = Vector2.one;
            ctRt.offsetMin = ctRt.offsetMax = Vector2.zero;
            var costTxt = costText.AddComponent<Text>();
            costTxt.text = "?";
            costTxt.alignment = TextAnchor.MiddleCenter;
            costTxt.fontSize = 50;
            costTxt.font = UIFactory.DefaultFont;
            costTxt.color = Color.white;
            costTxt.fontStyle = FontStyle.Bold;
            costTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
            costTxt.verticalOverflow = VerticalWrapMode.Overflow;
            costTxt.raycastTarget = false;
            var costOutline = costText.AddComponent<Outline>();
            costOutline.effectColor = new Color(0, 0, 0, 0.9f);
            costOutline.effectDistance = new Vector2(3, -3);

            var cg = go.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = true;
            cg.interactable = true;

            var slot = go.AddComponent<UICardSlot>();
            slot.slotIndex = idx;
            slot._bg = bg;
            slot._art = artImg;
            slot._cost = costTxt;
            slot._costBubble = cbImg;
            slot._slotCanvas = cg;
            return slot;
        }

        public void SetCard(CardData card, bool affordable)
        {
            _card = card;
            _affordable = affordable;
            if (card == null)
            {
                _cost.text = "";
                _costBubble.color = new Color(0.4f, 0.4f, 0.4f);
                _bg.color = new Color(0.4f, 0.4f, 0.4f);
                _art.sprite = null;
                _art.color = new Color(0.1f, 0.1f, 0.15f);
                return;
            }
            _cost.text = card.elixirCost.ToString();
            _costBubble.color = new Color(0.85f, 0.3f, 0.95f);
            _bg.color = affordable ? new Color(0.85f, 0.7f, 0.4f) : new Color(0.5f, 0.5f, 0.55f);
            var sprite = CardArtCache.Get(card.id);
            if (sprite != null) { _art.sprite = sprite; _art.color = affordable ? Color.white : new Color(0.6f, 0.6f, 0.6f); }
            else { _art.color = card.Kind == CardKind.Spell ? new Color(1f, 0.5f, 0.2f) : new Color(0.4f, 0.55f, 0.85f); }
        }

        public Sprite Art => _art != null ? _art.sprite : null;
        public CardData Card => _card;

        public void OnBeginDrag(PointerEventData e)
        {
            if (_card == null || !_affordable) return;
            if (_slotCanvas != null) _slotCanvas.alpha = 0.3f;
            OnDragStart?.Invoke(slotIndex);
        }

        public void OnDrag(PointerEventData e)
        {
            if (_card == null || !_affordable) return;
            OnDragMove?.Invoke(slotIndex, e.position);
        }

        public void OnEndDrag(PointerEventData e)
        {
            if (_slotCanvas != null) _slotCanvas.alpha = 1f;
            if (_card == null) return;
            OnDragEnd?.Invoke(slotIndex, e.position);
        }
    }
}
