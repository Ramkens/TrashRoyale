using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TrashRoyale.Audio;
using TrashRoyale.Bootstrap;
using TrashRoyale.Persistence;

namespace TrashRoyale.UI
{
    // Training-mode popup — pick a bot difficulty and jump straight
    // into a non-ranked match. The bot still gets a randomised meme
    // identity (banner colour + name + icon) but its difficulty knob
    // is overridden, and trophy delta is 0 so practice doesn't tank
    // the player's ranked progress.
    public class TrainingPopup : MonoBehaviour
    {
        struct Tier
        {
            public string title;
            public string subtitle;
            public float difficulty;
            public Color color;
        }

        static readonly Tier[] Tiers =
        {
            new Tier { title = "ИЗИ",        subtitle = "Бот тупой как пробка",       difficulty = 0.10f, color = new Color(0.45f, 0.85f, 0.45f) },
            new Tier { title = "НОРМ",       subtitle = "Стандартный соперник",       difficulty = 0.40f, color = new Color(0.30f, 0.78f, 0.95f) },
            new Tier { title = "ХАРД",       subtitle = "Думает быстрее, играет жёстче", difficulty = 0.75f, color = new Color(0.97f, 0.55f, 0.20f) },
            new Tier { title = "КИБЕРСПОРТ", subtitle = "Тащит как Pro Player",       difficulty = 1.00f, color = new Color(0.95f, 0.32f, 0.36f) },
        };

        public static TrainingPopup Open(Transform canvas)
        {
            var go = new GameObject("TrainingPopup");
            go.transform.SetParent(canvas, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var dim = go.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.85f);
            dim.raycastTarget = true;
            var p = go.AddComponent<TrainingPopup>();
            p.Build();
            return p;
        }

        void Build()
        {
            var panel = UIFactory.MakePanel(transform, "Panel", new Color(0.07f, 0.13f, 0.28f, 1f));
            var prt = panel.GetComponent<RectTransform>();
            prt.anchorMin = new Vector2(0.05f, 0.18f);
            prt.anchorMax = new Vector2(0.95f, 0.86f);
            prt.offsetMin = prt.offsetMax = Vector2.zero;
            var bg = UIFactory.LoadSprite("UI/menu_bg");
            if (bg != null) { panel.sprite = bg; panel.color = new Color(1f, 1f, 1f, 0.55f); }

            var title = UIFactory.MakeText(panel.transform, "Title", "ТРЕНИРОВКА", 60, TextAnchor.MiddleCenter);
            var trt = title.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0, 0.88f);
            trt.anchorMax = new Vector2(1, 0.97f);
            trt.offsetMin = trt.offsetMax = Vector2.zero;
            title.color = new Color(1f, 0.93f, 0.4f);

            var sub = UIFactory.MakeText(panel.transform, "Sub", "Кубки не считаются. Выбери уровень бота:", 26, TextAnchor.MiddleCenter);
            var srt = sub.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0, 0.78f);
            srt.anchorMax = new Vector2(1, 0.86f);
            srt.offsetMin = srt.offsetMax = Vector2.zero;
            sub.color = new Color(0.9f, 0.9f, 1f);

            for (int i = 0; i < Tiers.Length; i++)
            {
                var tier = Tiers[i];
                BuildTierButton(panel.transform, i, tier);
            }

            var close = UIFactory.MakeButton(panel.transform, "Close", "ЗАКРЫТЬ", () =>
            {
                AudioManager.PlaySfx("click");
                Destroy(gameObject);
            });
            var crt = close.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.30f, 0.04f);
            crt.anchorMax = new Vector2(0.70f, 0.12f);
            crt.offsetMin = crt.offsetMax = Vector2.zero;
        }

        void BuildTierButton(Transform parent, int idx, Tier tier)
        {
            float top = 0.74f - idx * 0.16f;
            float bot = top - 0.13f;

            var row = new GameObject("Tier_" + tier.title);
            row.transform.SetParent(parent, false);
            var rrt = row.AddComponent<RectTransform>();
            rrt.anchorMin = new Vector2(0.06f, bot);
            rrt.anchorMax = new Vector2(0.94f, top);
            rrt.offsetMin = rrt.offsetMax = Vector2.zero;
            var bgImg = row.AddComponent<Image>();
            bgImg.color = new Color(tier.color.r * 0.6f, tier.color.g * 0.6f, tier.color.b * 0.6f, 0.9f);

            var btn = row.AddComponent<Button>();
            btn.targetGraphic = bgImg;
            int captured = idx;
            btn.onClick.AddListener(() => Launch(captured));

            var label = UIFactory.MakeText(row.transform, "Label", tier.title, 44, TextAnchor.MiddleLeft);
            var lrt = label.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0.04f, 0.45f);
            lrt.anchorMax = new Vector2(0.5f, 0.95f);
            lrt.offsetMin = lrt.offsetMax = Vector2.zero;
            label.color = Color.white;

            var sub = UIFactory.MakeText(row.transform, "Sub", tier.subtitle, 22, TextAnchor.MiddleLeft);
            var srt = sub.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0.04f, 0.05f);
            srt.anchorMax = new Vector2(0.7f, 0.45f);
            srt.offsetMin = srt.offsetMax = Vector2.zero;
            sub.color = new Color(1f, 1f, 1f, 0.9f);

            var pct = UIFactory.MakeText(row.transform, "Pct", Mathf.RoundToInt(tier.difficulty * 100f) + "%", 56, TextAnchor.MiddleRight);
            var prtT = pct.GetComponent<RectTransform>();
            prtT.anchorMin = new Vector2(0.7f, 0.05f);
            prtT.anchorMax = new Vector2(0.96f, 0.95f);
            prtT.offsetMin = prtT.offsetMax = Vector2.zero;
            pct.color = tier.color;
        }

        void Launch(int idx)
        {
            AudioManager.PlaySfx("card_play");
            var tier = Tiers[idx];
            var profile = PlayerProfile.Load();
            // Pull a random meme identity but override the difficulty
            // with the chosen training tier. trophyDelta = 0 so practice
            // never affects ranked trophies.
            var bot = BotLadder.PickFor(profile.trophies);
            BattleLauncher.Pending = new BattleLauncher.Request
            {
                isPvE = true,
                playerDeck = new List<string>(profile.deck),
                enemyDeck = bot.deck,
                botName = bot.name + " [" + tier.title + "]",
                botDifficulty = tier.difficulty,
                botBannerColor = bot.bannerColor,
                botIconKey = bot.iconKey,
                trophyDelta = 0,
            };
            SceneManager.LoadScene("Battle");
        }
    }
}
