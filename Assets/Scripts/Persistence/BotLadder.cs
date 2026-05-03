using System.Collections.Generic;
using UnityEngine;
using TrashRoyale.Core;

namespace TrashRoyale.Persistence
{
    public static class BotLadder
    {
        public struct BotEntry
        {
            public string name;
            public int trophies;
            public float difficulty;
            public List<string> deck;
            // Visual identity randomized at PickFor() time so two
            // back-to-back matches against "Бот Васян" actually look
            // different — different banner color, different meme icon.
            public Color bannerColor;
            public string iconKey;
        }

        static readonly List<string> KitDefault = new List<string> { "knight", "pig", "skibidi", "pocoyo", "amongus", "cheems", "nyancat", "fireball" };
        static readonly List<string> KitTank = new List<string> { "shrek", "gigachad", "knight", "pocoyo", "skibidi", "fireball", "amongus", "cheems" };
        static readonly List<string> KitSwarm = new List<string> { "amongus", "cheems", "knight", "fireball", "pig", "pocoyo", "nyancat", "skibidi" };
        static readonly List<string> KitAir = new List<string> { "nyancat", "skibidi", "pocoyo", "knight", "fireball", "amongus", "pig", "cheems" };
        static readonly List<string> KitElite = new List<string> { "gigachad", "shrek", "knight", "fireball", "skibidi", "pocoyo", "nyancat", "pig" };
        static readonly List<string> KitDefenders = new List<string> { "knight", "cannon", "tesla", "fireball", "bomber", "pocoyo", "amongus", "nyancat" };
        static readonly List<string> KitSplash = new List<string> { "doge_mage", "bomber", "fireball", "knight", "pocoyo", "tesla", "skibidi", "cheems" };

        // Static "ladder" — anchors a difficulty/deck profile to a trophy
        // bracket. The displayed name + banner + icon are randomized at
        // PickFor() time from the meme pools below.
        static readonly BotEntry[] Bots =
        {
            new BotEntry { name = "?",  trophies = 50,   difficulty = 0.15f, deck = KitDefault },
            new BotEntry { name = "?",  trophies = 200,  difficulty = 0.25f, deck = KitDefault },
            new BotEntry { name = "?",  trophies = 400,  difficulty = 0.35f, deck = KitSwarm },
            new BotEntry { name = "?",  trophies = 700,  difficulty = 0.45f, deck = KitDefenders },
            new BotEntry { name = "?",  trophies = 1000, difficulty = 0.55f, deck = KitTank },
            new BotEntry { name = "?",  trophies = 1300, difficulty = 0.65f, deck = KitAir },
            new BotEntry { name = "?",  trophies = 1600, difficulty = 0.75f, deck = KitSplash },
            new BotEntry { name = "?",  trophies = 2000, difficulty = 0.85f, deck = KitElite },
            new BotEntry { name = "?",  trophies = 2500, difficulty = 0.95f, deck = KitElite },
            new BotEntry { name = "?",  trophies = 3000, difficulty = 1.0f,  deck = KitElite }
        };

        // Meme bot names. Picked uniformly at random per match.
        static readonly string[] MemeNames =
        {
            "Скибиди-Сигма",   "Хрюшелло-228",       "Покойо-Алкаш",     "Шрек 24/7",
            "Гигачад-Джавы",   "Чимс Безработный",   "Нян-Нармал",       "Амогус Шизик",
            "Король Помойки",  "Босс-Биотуалет",     "Доктор Бим-Бам",   "Анти-Девин",
            "Помоечник",       "Trash Lord",         "Sigma Гонщик",     "Кринж-Мастер",
            "Васян Рагнар",    "Анус-Овермайнд",     "Жмыхный Принц",   "Капитан Кринж",
            "Клоун-Ривайвал",  "Ноумарси",           "АВТОМАТ-9000",     "Босс Ботов",
            "Бот Витёк",       "ВЛАДЕЛЕЦ ЧАТОВ",     "Куздай Эйнштейн",  "Кринж Магистр",
            "Грязнуля",        "Мамин Друг",         "Дядя Стёпа",       "Тиктокер №7",
        };

        // Banner colors — bright meme palette. RGB tuned for the dark
        // banner background in MainMenu/IntroOverlay.
        static readonly Color[] BannerColors =
        {
            new Color(0.96f, 0.32f, 0.36f), // crimson
            new Color(0.95f, 0.55f, 0.2f),  // orange
            new Color(0.97f, 0.85f, 0.30f), // gold
            new Color(0.45f, 0.85f, 0.45f), // green
            new Color(0.30f, 0.78f, 0.95f), // sky
            new Color(0.55f, 0.4f, 0.95f),  // violet
            new Color(0.95f, 0.4f, 0.85f),  // pink
            new Color(0.4f, 0.95f, 0.85f),  // teal
            new Color(0.85f, 0.55f, 0.35f), // bronze
            new Color(0.6f, 0.6f, 0.65f),   // steel
        };

        // Resources/Icons/<key>.png keys we can use as bot avatars.
        // (Existing icon set in the repo — listed under Assets/Resources/Icons.)
        static readonly string[] IconKeys =
        {
            "crown", "trophy", "fist", "shield", "crossed_swords",
            "team", "drop", "play", "card_play", "coin",
        };

        public static BotEntry PickFor(int playerTrophies)
        {
            BotEntry baseBot = Bots[0];
            int bestDiff = int.MaxValue;
            foreach (var b in Bots)
            {
                int d = System.Math.Abs(b.trophies - playerTrophies);
                if (d < bestDiff) { bestDiff = d; baseBot = b; }
            }
            // Roll the meme identity for this match.
            baseBot.name = MemeNames[Random.Range(0, MemeNames.Length)];
            baseBot.bannerColor = BannerColors[Random.Range(0, BannerColors.Length)];
            baseBot.iconKey = IconKeys[Random.Range(0, IconKeys.Length)];
            // Each match the bot uses a fresh random 8-card deck pulled
            // from the live card database, instead of always cycling the
            // same archetype kit (which made every PvE match feel like
            // the same cards in the same order).
            baseBot.deck = RandomDeck(8);
            return baseBot;
        }

        public static List<string> RandomDeck(int size)
        {
            CardDatabase.EnsureLoaded();
            var pool = new List<string>();
            foreach (var c in CardDatabase.All) pool.Add(c.id);
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (pool[i], pool[j]) = (pool[j], pool[i]);
            }
            if (pool.Count > size) pool.RemoveRange(size, pool.Count - size);
            return pool;
        }

        public static string TierName(int trophies)
        {
            if (trophies < 200) return "Свалка";
            if (trophies < 500) return "Помойная Арена";
            if (trophies < 800) return "Арена Свинарника";
            if (trophies < 1200) return "Помойный Зал";
            if (trophies < 1600) return "Скибиди-Арена";
            if (trophies < 2000) return "Гига-Лига";
            if (trophies < 2500) return "Чад Лига";
            if (trophies < 3000) return "Король Помойки";
            return "Легенда Свалки";
        }
    }
}
