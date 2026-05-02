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

            // Background must exist as a raycast target (so drag handlers fire),
            // but is fully transparent so the card looks like just art + cost bubble.
            var bg = go.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0);
            bg.raycastTarget = true;

            // Art fills the slot.
            var art = new GameObject("Art");
            art.transform.SetParent(go.transform, false);
            var artRt = art.AddComponent<RectTransform>();
            artRt.anchorMin = new Vector2(0f, 0f);
            artRt.anchorMax = new Vector2(1f, 1f);
            artRt.offsetMin = artRt.offsetMax = Vector2.zero;
            var artImg = art.AddComponent<Image>();
            artImg.color = new Color(0, 0, 0, 0);
            artImg.preserveAspect = true;
            artImg.raycastTarget = false;

            // No cost bubble overlay — the card art itself shows the elixir cost.

            var cg = go.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = true;
            cg.interactable = true;

            var slot = go.AddComponent<UICardSlot>();
            slot.slotIndex = idx;
            slot._bg = bg;
            slot._art = artImg;
            slot._slotCanvas = cg;
            return slot;
        }

        public void SetCard(CardData card, bool affordable)
        {
            _card = card;
            _affordable = affordable;
            if (card == null)
            {
                _art.sprite = null;
                _art.color = new Color(0, 0, 0, 0);
                return;
            }
            var sprite = CardArtCache.Get(card.id);
            if (sprite != null)
            {
                _art.sprite = sprite;
                _art.color = affordable ? Color.white : new Color(0.55f, 0.55f, 0.55f);
            }
            else
            {
                _art.sprite = null;
                _art.color = card.Kind == CardKind.Spell ? new Color(1f, 0.5f, 0.2f) : new Color(0.4f, 0.55f, 0.85f);
            }
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
