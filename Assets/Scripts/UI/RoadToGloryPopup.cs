using UnityEngine;
using UnityEngine.UI;
using TrashRoyale.Audio;
using TrashRoyale.Match;
using TrashRoyale.Persistence;

namespace TrashRoyale.UI
{
    /// <summary>
    /// "Дорога Славы" — preview of every arena tier. Shows the trophy
    /// gate, the colour palette, and which one is currently active.
    /// Locked arenas are dimmed.
    /// </summary>
    public class RoadToGloryPopup : MonoBehaviour
    {
        public static void Open(Transform parent, PlayerProfile profile)
        {
            var go = new GameObject("RoadToGloryPopup");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            var dim = UIFactory.MakePanel(go.transform, "Dim", new Color(0, 0, 0, 0.7f));
            var drt = dim.GetComponent<RectTransform>();
            drt.anchorMin = Vector2.zero; drt.anchorMax = Vector2.one;
            drt.offsetMin = drt.offsetMax = Vector2.zero;
            var dimBtn = dim.gameObject.AddComponent<Button>();
            dimBtn.targetGraphic = dim;
            dimBtn.onClick.AddListener(() => Object.Destroy(go));

            var panel = UIFactory.MakePanel(go.transform, "Panel", new Color(0.07f, 0.10f, 0.22f, 1f));
            var prt = panel.GetComponent<RectTransform>();
            prt.anchorMin = new Vector2(0.05f, 0.08f);
            prt.anchorMax = new Vector2(0.95f, 0.94f);
            prt.offsetMin = prt.offsetMax = Vector2.zero;
            var btnSp = UIFactory.LoadSprite("UI/btn_gold");
            if (btnSp != null) { panel.sprite = btnSp; panel.type = Image.Type.Sliced; panel.color = new Color(0.08f, 0.12f, 0.28f); }

            var title = UIFactory.MakeText(panel.transform, "Title", "ДОРОГА СЛАВЫ", 56, TextAnchor.MiddleCenter);
            var trt = title.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0, 0.92f);
            trt.anchorMax = new Vector2(1, 1f);
            trt.offsetMin = trt.offsetMax = Vector2.zero;
            title.color = new Color(1f, 0.93f, 0.45f);

            // Scrollable list of arena tier cards.
            var scroll = new GameObject("Scroll");
            scroll.transform.SetParent(panel.transform, false);
            var srt = scroll.AddComponent<RectTransform>();
            srt.anchorMin = new Vector2(0.02f, 0.06f);
            srt.anchorMax = new Vector2(0.98f, 0.92f);
            srt.offsetMin = srt.offsetMax = Vector2.zero;
            var sv = scroll.AddComponent<ScrollRect>();
            sv.horizontal = false;

            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scroll.transform, false);
            var vrt = viewport.AddComponent<RectTransform>();
            vrt.anchorMin = Vector2.zero; vrt.anchorMax = Vector2.one;
            vrt.offsetMin = vrt.offsetMax = Vector2.zero;
            var vimg = viewport.AddComponent<Image>();
            vimg.color = new Color(0, 0, 0, 0.0001f);
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            sv.viewport = vrt;

            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            var crt = content.AddComponent<RectTransform>();
            crt.anchorMin = new Vector2(0, 1f);
            crt.anchorMax = new Vector2(1, 1f);
            crt.pivot = new Vector2(0.5f, 1f);
            crt.anchoredPosition = Vector2.zero;
            var grid = content.AddComponent<VerticalLayoutGroup>();
            grid.spacing = 14;
            grid.padding = new RectOffset(20, 20, 20, 20);
            grid.childForceExpandHeight = false;
            grid.childForceExpandWidth = true;
            grid.childControlHeight = false;
            grid.childControlWidth = true;
            var fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sv.content = crt;

            int currentIdx = ArenaTheme.IndexFor(profile.trophies);
            for (int i = 0; i < ArenaTheme.All.Length; i++)
            {
                var theme = ArenaTheme.All[i];
                int floor = ArenaTheme.TrophyFloor(i);
                bool unlocked = profile.trophies >= floor;
                bool current = i == currentIdx;
                BuildRow(content.transform, theme, floor, unlocked, current);
            }

            var close = UIFactory.MakeButton(panel.transform, "Close", "X", () =>
            {
                AudioManager.PlaySfx("click");
                Object.Destroy(go);
            });
            var crrt = close.GetComponent<RectTransform>();
            crrt.anchorMin = new Vector2(0.92f, 0.93f);
            crrt.anchorMax = new Vector2(0.99f, 0.995f);
            crrt.offsetMin = crrt.offsetMax = Vector2.zero;
        }

        static void BuildRow(Transform parent, ArenaTheme theme, int floor, bool unlocked, bool current)
        {
            var row = UIFactory.MakePanel(parent, "Row_" + theme.DisplayName, new Color(0.05f, 0.07f, 0.18f, 1f));
            var rl = row.gameObject.AddComponent<LayoutElement>();
            rl.minHeight = 130;
            rl.preferredHeight = 130;
            var btnSp = UIFactory.LoadSprite("UI/btn_gold");
            if (btnSp != null)
            {
                row.sprite = btnSp;
                row.type = Image.Type.Sliced;
                row.color = unlocked
                    ? Color.Lerp(theme.PlayerSideTint, theme.EnemySideTint, 0.5f)
                    : new Color(0.20f, 0.20f, 0.24f, 1f);
            }

            // Twin colour swatches showing the per-side tint.
            var swatchL = UIFactory.MakePanel(row.transform, "Left", theme.PlayerSideTint);
            var slr = swatchL.GetComponent<RectTransform>();
            slr.anchorMin = new Vector2(0.02f, 0.15f);
            slr.anchorMax = new Vector2(0.13f, 0.85f);
            slr.offsetMin = slr.offsetMax = Vector2.zero;

            var swatchR = UIFactory.MakePanel(row.transform, "Right", theme.EnemySideTint);
            var srr = swatchR.GetComponent<RectTransform>();
            srr.anchorMin = new Vector2(0.13f, 0.15f);
            srr.anchorMax = new Vector2(0.24f, 0.85f);
            srr.offsetMin = srr.offsetMax = Vector2.zero;

            var sky = UIFactory.MakePanel(row.transform, "Sky", theme.SkyTop);
            var skr = sky.GetComponent<RectTransform>();
            skr.anchorMin = new Vector2(0.24f, 0.15f);
            skr.anchorMax = new Vector2(0.30f, 0.85f);
            skr.offsetMin = skr.offsetMax = Vector2.zero;

            string suffix = current ? "  (СЕЙЧАС)" : (unlocked ? "" : "  ЗАБЛОКИРОВАНО");
            var name = UIFactory.MakeText(row.transform, "Name", theme.DisplayName + suffix,
                32, TextAnchor.UpperLeft);
            var nrt = name.GetComponent<RectTransform>();
            nrt.anchorMin = new Vector2(0.32f, 0.45f);
            nrt.anchorMax = new Vector2(0.95f, 0.95f);
            nrt.offsetMin = nrt.offsetMax = Vector2.zero;
            name.color = unlocked ? Color.white : new Color(0.7f, 0.7f, 0.7f);

            string sub = unlocked
                ? "От " + floor + " кубков"
                : "Откроется на " + floor + " кубках";
            var subT = UIFactory.MakeText(row.transform, "Sub", sub, 24, TextAnchor.UpperLeft);
            var srt2 = subT.GetComponent<RectTransform>();
            srt2.anchorMin = new Vector2(0.32f, 0.05f);
            srt2.anchorMax = new Vector2(0.95f, 0.45f);
            srt2.offsetMin = srt2.offsetMax = Vector2.zero;
            subT.color = current
                ? new Color(1f, 0.93f, 0.45f)
                : new Color(0.85f, 0.85f, 1f, 0.85f);
        }
    }
}
