using UnityEngine;
using UnityEngine.UI;
using TrashRoyale.Persistence;
using TrashRoyale.Audio;

namespace TrashRoyale.UI
{
    /// <summary>
    /// Modal grid of every <see cref="Achievement"/> in the catalog,
    /// rendered as a tile with the medal icon, title, description, and
    /// a lock state for unearned ones. Opened from the medals strip on
    /// the main menu banner.
    /// </summary>
    public class AchievementsPopup : MonoBehaviour
    {
        public static void Open(Transform parent, PlayerProfile profile)
        {
            var go = new GameObject("AchievementsPopup");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            var dim = UIFactory.MakePanel(go.transform, "Dim", new Color(0, 0, 0, 0.7f));
            var drt = dim.GetComponent<RectTransform>();
            drt.anchorMin = Vector2.zero; drt.anchorMax = Vector2.one;
            drt.offsetMin = drt.offsetMax = Vector2.zero;
            dim.raycastTarget = true;
            var dimBtn = dim.gameObject.AddComponent<Button>();
            dimBtn.targetGraphic = dim;
            dimBtn.onClick.AddListener(() => Object.Destroy(go));

            var panel = UIFactory.MakePanel(go.transform, "Panel", new Color(0.07f, 0.10f, 0.22f, 1f));
            var prt = panel.GetComponent<RectTransform>();
            prt.anchorMin = new Vector2(0.05f, 0.1f);
            prt.anchorMax = new Vector2(0.95f, 0.92f);
            prt.offsetMin = prt.offsetMax = Vector2.zero;
            var btnSp = UIFactory.LoadSprite("UI/btn_gold");
            if (btnSp != null) { panel.sprite = btnSp; panel.type = Image.Type.Sliced; panel.color = new Color(0.08f, 0.12f, 0.28f); }

            var title = UIFactory.MakeText(panel.transform, "Title", "ДОСТИЖЕНИЯ", 64, TextAnchor.MiddleCenter);
            var trt = title.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0, 0.92f);
            trt.anchorMax = new Vector2(1, 1f);
            trt.offsetMin = trt.offsetMax = Vector2.zero;
            title.color = new Color(1f, 0.93f, 0.45f);

            // Grid: 2 columns x N rows. Use a simple GridLayoutGroup
            // inside a ScrollView so long catalogs scroll.
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
            var grid = content.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(440, 200);
            grid.spacing = new Vector2(20, 20);
            grid.padding = new RectOffset(20, 20, 20, 20);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            var fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sv.content = crt;

            var unlocked = profile.unlockedAchievements ?? new System.Collections.Generic.List<string>();
            foreach (var def in Achievements.All)
            {
                bool earned = unlocked.Contains(def.kind.ToString());
                BuildTile(content.transform, def, earned);
            }

            // Close button in corner.
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

        static void BuildTile(Transform parent, AchievementDef def, bool earned)
        {
            var tile = UIFactory.MakePanel(parent, "Tile_" + def.kind, new Color(0.05f, 0.07f, 0.18f, 1f));
            var btnSp = UIFactory.LoadSprite("UI/btn_gold");
            if (btnSp != null)
            {
                tile.sprite = btnSp;
                tile.type = Image.Type.Sliced;
                tile.color = earned ? def.medalColor : new Color(0.18f, 0.18f, 0.22f, 1f);
            }
            var icon = UIFactory.MakeIcon(tile.transform, "Icon", "Icons/" + def.medalIconKey, new Vector2(140, 140));
            icon.color = earned ? Color.white : new Color(0.4f, 0.4f, 0.4f, 0.7f);
            var irt = icon.GetComponent<RectTransform>();
            irt.anchorMin = new Vector2(0f, 0.5f);
            irt.anchorMax = new Vector2(0f, 0.5f);
            irt.pivot = new Vector2(0f, 0.5f);
            irt.anchoredPosition = new Vector2(20f, 0f);

            var titleT = UIFactory.MakeText(tile.transform, "T", def.title, 36, TextAnchor.UpperLeft);
            titleT.color = earned ? Color.white : new Color(0.7f, 0.7f, 0.7f);
            var trt = titleT.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0f, 0f);
            trt.anchorMax = new Vector2(1f, 1f);
            trt.offsetMin = new Vector2(170, 70);
            trt.offsetMax = new Vector2(-15, -15);

            var descT = UIFactory.MakeText(tile.transform, "D", def.description, 22, TextAnchor.UpperLeft);
            descT.color = earned ? new Color(1f, 1f, 1f, 0.85f) : new Color(0.5f, 0.5f, 0.5f);
            var drt = descT.GetComponent<RectTransform>();
            drt.anchorMin = new Vector2(0f, 0f);
            drt.anchorMax = new Vector2(1f, 1f);
            drt.offsetMin = new Vector2(170, 15);
            drt.offsetMax = new Vector2(-15, -65);

            if (!earned)
            {
                var lockT = UIFactory.MakeText(tile.transform, "Lock", "🔒", 60, TextAnchor.MiddleCenter);
                lockT.color = new Color(0f, 0f, 0f, 0.5f);
                var lrt = lockT.GetComponent<RectTransform>();
                lrt.anchorMin = new Vector2(0f, 0f);
                lrt.anchorMax = new Vector2(0f, 1f);
                lrt.sizeDelta = new Vector2(160, 0);
                lrt.anchoredPosition = new Vector2(80, 0);
            }
        }
    }
}
