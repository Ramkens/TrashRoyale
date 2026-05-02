using UnityEngine;
using UnityEngine.UI;
using TrashRoyale.Core;

namespace TrashRoyale.UI
{
    /// <summary>
    /// Tiny "next card" preview shown bottom-left of the HUD. Just the card art, no
    /// label or cost overlay (per UX request).
    /// </summary>
    public class UINextCard : MonoBehaviour
    {
        Image _art;

        public static UINextCard Build(Transform parent)
        {
            var go = new GameObject("NextCard");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0f, 0f);
            rt.sizeDelta = new Vector2(140, 180);
            rt.anchoredPosition = new Vector2(20, 20);

            var img = go.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0);
            img.preserveAspect = true;
            img.raycastTarget = false;

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
