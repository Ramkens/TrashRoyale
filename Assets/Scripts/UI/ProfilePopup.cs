using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TrashRoyale.Audio;
using TrashRoyale.Persistence;
using TrashRoyale.Match;

namespace TrashRoyale.UI
{
    /// <summary>
    /// Unified profile screen — replaces the standalone "tap banner =
    /// edit nick" flow with a single overview that shows everything
    /// the player has earned so far. Buttons drill into the
    /// per-feature popups (nickname / achievements / road to glory /
    /// settings + account binding).
    /// </summary>
    public class ProfilePopup : MonoBehaviour
    {
        public static void Open(Transform canvas, PlayerProfile profile, System.Action onChanged)
        {
            var go = new GameObject("ProfilePopup");
            go.transform.SetParent(canvas, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var dim = go.AddComponent<Image>();
            dim.color = new Color(0, 0, 0, 0.82f);
            dim.raycastTarget = true;
            var dimBtn = go.AddComponent<Button>();
            dimBtn.targetGraphic = dim;
            dimBtn.onClick.AddListener(() => Object.Destroy(go));

            var pop = go.AddComponent<ProfilePopup>();
            pop._profile = profile;
            pop._onChanged = onChanged;
            pop._canvas = canvas;
            pop.Build();
        }

        PlayerProfile _profile;
        System.Action _onChanged;
        Transform _canvas;

        void Build()
        {
            var panel = UIFactory.MakePanel(transform, "Panel", new Color(0.07f, 0.10f, 0.22f, 1f));
            var prt = panel.GetComponent<RectTransform>();
            prt.anchorMin = new Vector2(0.04f, 0.06f);
            prt.anchorMax = new Vector2(0.96f, 0.96f);
            prt.offsetMin = prt.offsetMax = Vector2.zero;
            var btnSp = UIFactory.LoadSprite("UI/btn_gold");
            if (btnSp != null) { panel.sprite = btnSp; panel.type = Image.Type.Sliced; panel.color = new Color(0.09f, 0.13f, 0.30f); }
            // Tap inside panel is fine — only the dim background closes
            // the popup. We still need a graphic that swallows raycasts
            // so taps on the panel don't fall through to the dimmer.
            var swallow = panel.gameObject.AddComponent<Button>();
            swallow.targetGraphic = panel;
            swallow.transition = Selectable.Transition.None;

            BuildHeader(panel.transform);
            BuildBanner(panel.transform);
            BuildStats(panel.transform);
            BuildActions(panel.transform);
            BuildClose(panel.transform);
        }

        void BuildHeader(Transform parent)
        {
            var title = UIFactory.MakeText(parent, "Title", "ПРОФИЛЬ", 56, TextAnchor.MiddleCenter);
            var trt = title.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0, 0.92f);
            trt.anchorMax = new Vector2(1, 0.99f);
            trt.offsetMin = trt.offsetMax = Vector2.zero;
            title.color = new Color(1f, 0.93f, 0.45f);
        }

        void BuildBanner(Transform parent)
        {
            // Big banner preview — same colour the player will display in
            // the main menu. Tap to edit nick.
            Color bannerColor = new Color(0.25f, 0.45f, 0.85f, 1f);
            if (!string.IsNullOrEmpty(_profile.bannerColorHex) &&
                ColorUtility.TryParseHtmlString(_profile.bannerColorHex, out var c))
            {
                bannerColor = c;
            }
            var banner = UIFactory.MakePanel(parent, "Banner", bannerColor);
            var brt = banner.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0.05f, 0.74f);
            brt.anchorMax = new Vector2(0.95f, 0.91f);
            brt.offsetMin = brt.offsetMax = Vector2.zero;
            var btnSp = UIFactory.LoadSprite("UI/btn_gold");
            if (btnSp != null) { banner.sprite = btnSp; banner.type = Image.Type.Sliced; banner.color = bannerColor; }

            // Player nickname inline.
            var nick = UIFactory.MakeText(banner.transform, "Nick", _profile.playerName, 70, TextAnchor.MiddleLeft);
            var nrt = nick.GetComponent<RectTransform>();
            nrt.anchorMin = new Vector2(0.04f, 0.40f);
            nrt.anchorMax = new Vector2(0.66f, 1.0f);
            nrt.offsetMin = nrt.offsetMax = Vector2.zero;
            nick.color = Color.white;

            // Trophy count, large.
            var tro = UIFactory.MakeText(banner.transform, "Trophies", _profile.trophies + " 🏆", 70, TextAnchor.MiddleRight);
            var trrt = tro.GetComponent<RectTransform>();
            trrt.anchorMin = new Vector2(0.66f, 0.40f);
            trrt.anchorMax = new Vector2(0.96f, 1.0f);
            trrt.offsetMin = trrt.offsetMax = Vector2.zero;
            tro.color = new Color(1f, 0.93f, 0.45f);

            // Equipped medals strip — exactly the same logic as
            // MainMenuBootstrap so the preview matches the in-game banner.
            var unlocked = _profile.unlockedAchievements ?? new List<string>();
            var defs = new List<AchievementDef>();
            if (_profile.equippedBadges != null)
            {
                foreach (var key in _profile.equippedBadges)
                {
                    var def = Achievements.Find(key);
                    if (def != null && unlocked.Contains(def.kind.ToString())) defs.Add(def);
                    if (defs.Count >= 3) break;
                }
            }
            if (defs.Count == 0)
            {
                for (int i = Achievements.All.Length - 1; i >= 0 && defs.Count < 3; i--)
                {
                    var def = Achievements.All[i];
                    if (unlocked.Contains(def.kind.ToString())) defs.Add(def);
                }
            }
            int slot = Mathf.Max(1, defs.Count);
            for (int i = 0; i < defs.Count; i++)
            {
                var def = defs[i];
                var icon = UIFactory.MakeIcon(banner.transform, "Medal_" + def.kind,
                    "Icons/" + def.medalIconKey, new Vector2(70, 70));
                icon.color = def.medalColor;
                var irt = icon.GetComponent<RectTransform>();
                float w = 1f / slot;
                irt.anchorMin = new Vector2(0.04f + i * 0.18f, 0.05f);
                irt.anchorMax = new Vector2(0.04f + i * 0.18f + 0.16f, 0.40f);
                irt.offsetMin = irt.offsetMax = Vector2.zero;
            }
            if (defs.Count == 0)
            {
                var hint = UIFactory.MakeText(banner.transform, "MedalHint",
                    "Получай медали в боях, потом цепляй их сюда в попапе достижений.",
                    22, TextAnchor.MiddleLeft);
                var hrt = hint.GetComponent<RectTransform>();
                hrt.anchorMin = new Vector2(0.04f, 0.05f);
                hrt.anchorMax = new Vector2(0.96f, 0.40f);
                hrt.offsetMin = hrt.offsetMax = Vector2.zero;
                hint.color = new Color(1f, 1f, 1f, 0.75f);
            }
        }

        void BuildStats(Transform parent)
        {
            int total = Achievements.All.Length;
            int unlocked = _profile.unlockedAchievements?.Count ?? 0;
            int played = _profile.wins + _profile.losses;
            float winRate = played > 0 ? (_profile.wins * 100f / played) : 0f;
            string arena = ArenaTheme.Current(_profile.trophies).DisplayName;

            var lines = new[]
            {
                ("Арена", arena),
                ("Кубков", _profile.trophies.ToString()),
                ("Побед / Поражений", _profile.wins + " / " + _profile.losses),
                ("Винрейт", played > 0 ? winRate.ToString("F0") + " %" : "—"),
                ("Достижения", unlocked + " / " + total),
            };

            var box = UIFactory.MakePanel(parent, "Stats", new Color(0.05f, 0.07f, 0.18f, 1f));
            var brt = box.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0.05f, 0.42f);
            brt.anchorMax = new Vector2(0.95f, 0.72f);
            brt.offsetMin = brt.offsetMax = Vector2.zero;
            var btnSp = UIFactory.LoadSprite("UI/btn_gold");
            if (btnSp != null) { box.sprite = btnSp; box.type = Image.Type.Sliced; box.color = new Color(0.05f, 0.07f, 0.18f, 1f); }

            for (int i = 0; i < lines.Length; i++)
            {
                float top = 1f - i / (float)lines.Length;
                float bot = 1f - (i + 1) / (float)lines.Length;
                var label = UIFactory.MakeText(box.transform, "L" + i, lines[i].Item1, 28, TextAnchor.MiddleLeft);
                var lrt = label.GetComponent<RectTransform>();
                lrt.anchorMin = new Vector2(0.05f, bot + 0.02f);
                lrt.anchorMax = new Vector2(0.55f, top - 0.02f);
                lrt.offsetMin = lrt.offsetMax = Vector2.zero;
                label.color = new Color(0.85f, 0.85f, 1f, 0.85f);

                var val = UIFactory.MakeText(box.transform, "V" + i, lines[i].Item2, 32, TextAnchor.MiddleRight);
                var vrt = val.GetComponent<RectTransform>();
                vrt.anchorMin = new Vector2(0.55f, bot + 0.02f);
                vrt.anchorMax = new Vector2(0.95f, top - 0.02f);
                vrt.offsetMin = vrt.offsetMax = Vector2.zero;
                val.color = Color.white;
            }
        }

        void BuildActions(Transform parent)
        {
            var actions = new (string label, System.Action onClick)[]
            {
                ("Сменить ник", () =>
                {
                    NicknamePopup.Open(_canvas, _profile, () => { _onChanged?.Invoke(); RefreshSelf(); });
                }),
                ("Достижения", () => AchievementsPopup.Open(_canvas, _profile)),
                ("Дорога Славы", () => RoadToGloryPopup.Open(_canvas, _profile)),
                ("Привязать почту / настройки", () => SettingsPopup.Open(_canvas, () => { _onChanged?.Invoke(); RefreshSelf(); })),
            };
            for (int i = 0; i < actions.Length; i++)
            {
                int idx = i;
                var btn = UIFactory.MakeButton(parent, "Btn" + i, actions[i].label, () =>
                {
                    AudioManager.PlaySfx("click");
                    actions[idx].onClick();
                });
                var brt = btn.GetComponent<RectTransform>();
                float top = 0.40f - i * 0.085f;
                brt.anchorMin = new Vector2(0.10f, top - 0.075f);
                brt.anchorMax = new Vector2(0.90f, top);
                brt.offsetMin = brt.offsetMax = Vector2.zero;
            }
        }

        void BuildClose(Transform parent)
        {
            var close = UIFactory.MakeButton(parent, "Close", "X", () =>
            {
                AudioManager.PlaySfx("click");
                Destroy(gameObject);
            });
            var crt = close.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.92f, 0.93f);
            crt.anchorMax = new Vector2(0.99f, 0.995f);
            crt.offsetMin = crt.offsetMax = Vector2.zero;
        }

        // After a sub-popup mutated the profile, rebuild the banner /
        // stats sections in place so the player sees changes without
        // closing the profile screen.
        void RefreshSelf()
        {
            var canvas = _canvas;
            var onChanged = _onChanged;
            Destroy(gameObject);
            Open(canvas, PlayerProfile.Load(), onChanged);
        }
    }
}
