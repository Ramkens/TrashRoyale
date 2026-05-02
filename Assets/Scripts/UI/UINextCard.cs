using UnityEngine;
using UnityEngine.UI;
using TrashRoyale.Core;

namespace TrashRoyale.UI
{
    public class UINextCard : MonoBehaviour
    {
        Text _name;
        Image _bg;

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
            img.color = new Color(0.5f, 0.4f, 0.3f, 0.8f);

            var t = UIFactory.MakeText(go.transform, "Name", "—", 22, TextAnchor.MiddleCenter);
            var nrt = t.GetComponent<RectTransform>();
            nrt.anchorMin = Vector2.zero;
            nrt.anchorMax = Vector2.one;
            nrt.offsetMin = nrt.offsetMax = Vector2.zero;

            var lblText = UIFactory.MakeText(go.transform, "Label", "NEXT", 18, TextAnchor.MiddleCenter);
            var lrt = lblText.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0, 1);
            lrt.anchorMax = new Vector2(1, 1);
            lrt.pivot = new Vector2(0.5f, 1f);
            lrt.sizeDelta = new Vector2(0, 28);
            lblText.color = new Color(1f, 0.9f, 0.5f);

            var nx = go.AddComponent<UINextCard>();
            nx._name = t;
            nx._bg = img;
            return nx;
        }

        public void SetCard(CardData card)
        {
            _name.text = card != null ? $"{card.displayName}\n{card.elixirCost} эл" : "—";
        }
    }
}
