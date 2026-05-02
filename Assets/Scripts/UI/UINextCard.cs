using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TrashRoyale.Core;

namespace TrashRoyale.UI
{
    public class UINextCard : MonoBehaviour
    {
        TMP_Text _name;
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

            var name = new GameObject("Name");
            name.transform.SetParent(go.transform, false);
            var nrt = name.AddComponent<RectTransform>();
            nrt.anchorMin = Vector2.zero;
            nrt.anchorMax = Vector2.one;
            nrt.offsetMin = nrt.offsetMax = Vector2.zero;
            var t = name.AddComponent<TextMeshProUGUI>();
            t.alignment = TextAlignmentOptions.Center;
            t.fontSize = 22;
            t.color = Color.white;

            var lbl = new GameObject("Label");
            lbl.transform.SetParent(go.transform, false);
            var lrt = lbl.AddComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0, 1);
            lrt.anchorMax = new Vector2(1, 1);
            lrt.pivot = new Vector2(0.5f, 1f);
            lrt.sizeDelta = new Vector2(0, 28);
            var lblText = lbl.AddComponent<TextMeshProUGUI>();
            lblText.text = "NEXT";
            lblText.alignment = TextAlignmentOptions.Center;
            lblText.fontSize = 18;
            lblText.color = new Color(1f, 0.9f, 0.5f);

            var nx = go.AddComponent<UINextCard>();
            nx._name = t;
            nx._bg = img;
            return nx;
        }

        public void SetCard(CardData card)
        {
            _name.text = card != null ? $"{card.displayName}\n{card.elixirCost}⚡" : "—";
        }
    }
}
