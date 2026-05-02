using UnityEngine;
using UnityEngine.UI;
using TrashRoyale.Core;

namespace TrashRoyale.UI
{
    public class CardInfoPopup : MonoBehaviour
    {
        public static CardInfoPopup Open(Transform canvas, CardData card)
        {
            var go = new GameObject("CardInfoPopup");
            go.transform.SetParent(canvas, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var dim = go.AddComponent<Image>();
            dim.color = new Color(0, 0, 0, 0.85f);
            dim.raycastTarget = true;

            // Tap outside to close
            var tapToClose = go.AddComponent<Button>();
            tapToClose.targetGraphic = dim;

            var pop = go.AddComponent<CardInfoPopup>();
            tapToClose.onClick.AddListener(() => Destroy(go));
            pop.Build(card);
            return pop;
        }

        void Build(CardData c)
        {
            var panel = UIFactory.MakePanel(transform, "Panel", new Color(0.06f, 0.12f, 0.25f, 1f));
            var prt = panel.GetComponent<RectTransform>();
            prt.anchorMin = new Vector2(0.08f, 0.18f);
            prt.anchorMax = new Vector2(0.92f, 0.82f);
            prt.offsetMin = prt.offsetMax = Vector2.zero;
            var bgSp = UIFactory.LoadSprite("UI/menu_bg");
            if (bgSp != null) { panel.sprite = bgSp; panel.color = new Color(1f, 1f, 1f, 0.55f); }

            // Card art (top half)
            var art = UIFactory.MakeCardArt(panel.transform, c.id);
            var art_rt = art.GetComponent<RectTransform>();
            art_rt.anchorMin = new Vector2(0.1f, 0.5f);
            art_rt.anchorMax = new Vector2(0.9f, 0.95f);
            art_rt.offsetMin = art_rt.offsetMax = Vector2.zero;

            // Name
            var name = UIFactory.MakeText(panel.transform, "Name", c.displayName, 56, TextAnchor.MiddleCenter);
            var nrt = name.GetComponent<RectTransform>();
            nrt.anchorMin = new Vector2(0.05f, 0.42f);
            nrt.anchorMax = new Vector2(0.95f, 0.5f);
            nrt.offsetMin = nrt.offsetMax = Vector2.zero;
            name.color = Color.white;

            // Elixir cost (with droplet icon)
            var elixir = UIFactory.MakeText(panel.transform, "Elixir", c.elixirCost.ToString(), 70, TextAnchor.MiddleCenter);
            var ert = elixir.GetComponent<RectTransform>();
            ert.anchorMin = new Vector2(0.06f, 0.83f);
            ert.anchorMax = new Vector2(0.22f, 0.98f);
            ert.offsetMin = ert.offsetMax = Vector2.zero;
            elixir.color = new Color(1f, 0.55f, 0.95f, 1f);

            // Description
            var desc = UIFactory.MakeText(panel.transform, "Desc", c.description, 30, TextAnchor.UpperCenter);
            var drt = desc.GetComponent<RectTransform>();
            drt.anchorMin = new Vector2(0.05f, 0.27f);
            drt.anchorMax = new Vector2(0.95f, 0.42f);
            drt.offsetMin = drt.offsetMax = Vector2.zero;
            desc.color = new Color(1f, 1f, 1f, 0.9f);
            desc.horizontalOverflow = HorizontalWrapMode.Wrap;
            desc.verticalOverflow = VerticalWrapMode.Overflow;

            // Stats grid
            var statsTxt = UIFactory.MakeText(panel.transform, "Stats", BuildStats(c), 28, TextAnchor.UpperLeft);
            var srt = statsTxt.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0.08f, 0.07f);
            srt.anchorMax = new Vector2(0.92f, 0.27f);
            srt.offsetMin = srt.offsetMax = Vector2.zero;
            statsTxt.color = Color.white;

            // Close
            var btn = UIFactory.MakeButton(transform, "Close", "ОК", () => Destroy(gameObject));
            var brt = btn.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0.3f, 0.06f);
            brt.anchorMax = new Vector2(0.7f, 0.13f);
            brt.offsetMin = brt.offsetMax = Vector2.zero;
        }

        string BuildStats(CardData c)
        {
            if (c.Kind == CardKind.Spell)
            {
                return $"Урон: {(int)c.damage}    Радиус: {c.splashRadius:0.0}\nЗаклинание";
            }
            string targets = c.Targets == TargetMode.BuildingsOnly ? "только здания" : (c.targetsAir ? "наземные+воздух" : "наземные");
            string airTag = c.isAir ? "  ВОЗДУХ" : "";
            return
                $"HP: {(int)c.hp}    Урон: {(int)c.damage}\n" +
                $"Скорость атаки: {c.attackInterval:0.0}с    Дальность: {c.range:0.0}\n" +
                $"Скорость: {c.moveSpeed:0.0}    Спавн: {c.spawnCount}{airTag}\n" +
                $"Цели: {targets}";
        }
    }
}
