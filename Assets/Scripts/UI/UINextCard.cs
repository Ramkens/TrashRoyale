using UnityEngine;
using UnityEngine.UI;
using TrashRoyale.Core;

namespace TrashRoyale.UI
{
    /// <summary>
    /// Tiny "next card" preview shown above the leftmost hand card. Just the card art
    /// plus a small "далее" label so the player knows what's coming. Sits ABOVE the
    /// card row (not on top of slot 0) so it never overlaps the active hand.
    /// </summary>
    public class UINextCard : MonoBehaviour
    {
        Image _art;

        public static UINextCard Build(Transform parent)
        {
            var go = new GameObject("NextCard");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            // Bottom-left anchored, sitting ABOVE the 4-card hand row.
            // Card slots are h=280 with anchoredY=152 (h/2+12), so their
            // top edge sits at y=292. We park "next card" at y=320 so
            // there's a small gap and the preview never covers slot 0.
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0f, 0f);
            rt.sizeDelta = new Vector2(110, 140);
            rt.anchoredPosition = new Vector2(20, 320);

            var img = go.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0);
            img.preserveAspect = true;
            img.raycastTarget = false;

            // Small "далее" label above the preview so it's clear what
            // this thumbnail is for (matches CR's "next" tag).
            var lbl = UIFactory.MakeText(go.transform, "Label", "далее", 24, TextAnchor.MiddleCenter);
            var lrt = lbl.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0f, 1f);
            lrt.anchorMax = new Vector2(1f, 1f);
            lrt.pivot = new Vector2(0.5f, 0f);
            lrt.sizeDelta = new Vector2(0, 28);
            lrt.anchoredPosition = new Vector2(0, 2);
            lbl.color = new Color(1f, 0.93f, 0.45f, 0.95f);
            lbl.raycastTarget = false;
            var lblOutline = lbl.gameObject.AddComponent<Outline>();
            lblOutline.effectColor = new Color(0, 0, 0, 0.8f);
            lblOutline.effectDistance = new Vector2(2, -2);

            var nx = go.AddComponent<UINextCard>();
            nx._art = img;
            return nx;
        }

        public void SetCard(CardData card)
        {
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
                _art.color = new Color(1f, 1f, 1f, 0.85f);
            }
            else
            {
                _art.sprite = null;
                _art.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            }
        }
    }
}
