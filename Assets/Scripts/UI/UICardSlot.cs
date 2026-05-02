using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using TrashRoyale.Core;

namespace TrashRoyale.UI
{
    public class UICardSlot : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public int slotIndex;
        Image _bg;
        Image _art;
        TMP_Text _name;
        TMP_Text _cost;
        Image _costBubble;
        CardData _card;
        bool _affordable;
        public Action<int> OnDragStart;
        public Action<int, Vector2> OnDragEnd;

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

            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.85f, 0.7f, 0.4f);

            var art = new GameObject("Art");
            art.transform.SetParent(go.transform, false);
            var artRt = art.AddComponent<RectTransform>();
            artRt.anchorMin = new Vector2(0.05f, 0.18f);
            artRt.anchorMax = new Vector2(0.95f, 0.92f);
            artRt.offsetMin = artRt.offsetMax = Vector2.zero;
            var artImg = art.AddComponent<Image>();
            artImg.color = new Color(0.15f, 0.18f, 0.3f);
            artImg.preserveAspect = true;

            var costBubble = new GameObject("CostBubble");
            costBubble.transform.SetParent(go.transform, false);
            var cbRt = costBubble.AddComponent<RectTransform>();
            cbRt.anchorMin = new Vector2(0, 1);
            cbRt.anchorMax = new Vector2(0, 1);
            cbRt.pivot = new Vector2(0, 1);
            cbRt.sizeDelta = new Vector2(64, 64);
            cbRt.anchoredPosition = new Vector2(-12, 12);
            var cbImg = costBubble.AddComponent<Image>();
            cbImg.color = new Color(0.85f, 0.3f, 0.95f);

            var costText = new GameObject("CostText");
            costText.transform.SetParent(costBubble.transform, false);
            var ctRt = costText.AddComponent<RectTransform>();
            ctRt.anchorMin = Vector2.zero;
            ctRt.anchorMax = Vector2.one;
            ctRt.offsetMin = ctRt.offsetMax = Vector2.zero;
            var costTxt = costText.AddComponent<TextMeshProUGUI>();
            costTxt.text = "?";
            costTxt.alignment = TextAlignmentOptions.Center;
            costTxt.fontSize = 44;

            var name = new GameObject("Name");
            name.transform.SetParent(go.transform, false);
            var nrt = name.AddComponent<RectTransform>();
            nrt.anchorMin = new Vector2(0, 0);
            nrt.anchorMax = new Vector2(1, 0.18f);
            nrt.offsetMin = nrt.offsetMax = Vector2.zero;
            var nameTxt = name.AddComponent<TextMeshProUGUI>();
            nameTxt.alignment = TextAlignmentOptions.Center;
            nameTxt.fontSize = 28;
            nameTxt.color = Color.white;

            var slot = go.AddComponent<UICardSlot>();
            slot.slotIndex = idx;
            slot._bg = bg;
            slot._art = artImg;
            slot._name = nameTxt;
            slot._cost = costTxt;
            slot._costBubble = cbImg;
            return slot;
        }

        public void SetCard(CardData card, bool affordable)
        {
            _card = card;
            _affordable = affordable;
            if (card == null)
            {
                _name.text = "—";
                _cost.text = "";
                _costBubble.color = new Color(0.4f, 0.4f, 0.4f);
                _bg.color = new Color(0.4f, 0.4f, 0.4f);
                _art.sprite = null;
                _art.color = new Color(0.1f, 0.1f, 0.15f);
                return;
            }
            _name.text = card.displayName;
            _cost.text = card.elixirCost.ToString();
            _costBubble.color = new Color(0.85f, 0.3f, 0.95f);
            _bg.color = affordable ? new Color(0.85f, 0.7f, 0.4f) : new Color(0.5f, 0.5f, 0.55f);
            var sprite = CardArtCache.Get(card.id);
            if (sprite != null) { _art.sprite = sprite; _art.color = Color.white; }
            else { _art.color = card.Kind == CardKind.Spell ? new Color(1f, 0.5f, 0.2f) : new Color(0.4f, 0.55f, 0.85f); }
        }

        public void OnBeginDrag(PointerEventData e)
        {
            if (_card == null || !_affordable) return;
            OnDragStart?.Invoke(slotIndex);
        }

        public void OnDrag(PointerEventData e) { }

        public void OnEndDrag(PointerEventData e)
        {
            if (_card == null) return;
            OnDragEnd?.Invoke(slotIndex, e.position);
        }
    }
}
