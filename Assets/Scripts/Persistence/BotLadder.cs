using System.Collections.Generic;

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
        }

        static readonly List<string> KitDefault = new List<string> { "knight", "pig", "skibidi", "pocoyo", "amongus", "cheems", "nyancat", "fireball" };
        static readonly List<string> KitTank = new List<string> { "shrek", "gigachad", "knight", "pocoyo", "skibidi", "fireball", "amongus", "cheems" };
        static readonly List<string> KitSwarm = new List<string> { "amongus", "cheems", "knight", "fireball", "pig", "pocoyo", "nyancat", "skibidi" };
        static readonly List<string> KitAir = new List<string> { "nyancat", "skibidi", "pocoyo", "knight", "fireball", "amongus", "pig", "cheems" };
        static readonly List<string> KitElite = new List<string> { "gigachad", "shrek", "knight", "fireball", "skibidi", "pocoyo", "nyancat", "pig" };

        static readonly BotEntry[] Bots =
        {
            new BotEntry { name = "Бот Васян",       trophies = 50,   difficulty = 0.15f, deck = KitDefault },
            new BotEntry { name = "Хрюшелло",         trophies = 200,  difficulty = 0.25f, deck = KitDefault },
            new BotEntry { name = "Скибиди-демон",    trophies = 400,  difficulty = 0.35f, deck = KitSwarm },
            new BotEntry { name = "Ноглы Амогус",    trophies = 700,  difficulty = 0.45f, deck = KitSwarm },
            new BotEntry { name = "Шрек 24/7",        trophies = 1000, difficulty = 0.55f, deck = KitTank },
            new BotEntry { name = "Покойо Тильт",     trophies = 1300, difficulty = 0.65f, deck = KitAir },
            new BotEntry { name = "Чимс Сигма",        trophies = 1600, difficulty = 0.75f, deck = KitElite },
            new BotEntry { name = "Гигачадоносец",    trophies = 2000, difficulty = 0.85f, deck = KitElite },
            new BotEntry { name = "Нян-Босс",          trophies = 2500, difficulty = 0.95f, deck = KitElite },
            new BotEntry { name = "Король Помойки",   trophies = 3000, difficulty = 1.0f,  deck = KitElite }
        };

        public static BotEntry PickFor(int playerTrophies)
        {
            BotEntry best = Bots[0];
            int bestDiff = int.MaxValue;
            foreach (var b in Bots)
            {
                int d = System.Math.Abs(b.trophies - playerTrophies);
                if (d < bestDiff) { bestDiff = d; best = b; }
            }
            return best;
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
