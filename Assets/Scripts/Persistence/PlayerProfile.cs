using System;
using System.Collections.Generic;
using UnityEngine;
using TrashRoyale.Core;

namespace TrashRoyale.Persistence
{
    [Serializable]
    public class PlayerProfile
    {
        public int trophies = 0;
        public int wins = 0;
        public int losses = 0;
        public string playerName = "Хрюнделик";
        public List<string> deck = new List<string> { "knight", "pig", "skibidi", "pocoyo", "amongus", "cheems", "nyancat", "fireball" };

        public static PlayerProfile Load()
        {
            var json = PlayerPrefs.GetString("trashroyale.profile", "");
            if (string.IsNullOrEmpty(json)) return EnsureDefault(new PlayerProfile());
            try { return EnsureDefault(JsonUtility.FromJson<PlayerProfile>(json) ?? new PlayerProfile()); }
            catch { return EnsureDefault(new PlayerProfile()); }
        }

        static PlayerProfile EnsureDefault(PlayerProfile p)
        {
            CardDatabase.EnsureLoaded();
            if (p.deck == null || p.deck.Count != 8)
            {
                p.deck = new List<string> { "knight", "pig", "skibidi", "pocoyo", "amongus", "cheems", "nyancat", "fireball" };
            }
            // sanitize
            for (int i = p.deck.Count - 1; i >= 0; i--)
            {
                if (CardDatabase.Get(p.deck[i]) == null) p.deck.RemoveAt(i);
            }
            int idx = 0;
            while (p.deck.Count < 8 && idx < CardDatabase.All.Count)
            {
                var c = CardDatabase.All[idx++];
                if (!p.deck.Contains(c.id)) p.deck.Add(c.id);
            }
            return p;
        }

        public void Save()
        {
            var json = JsonUtility.ToJson(this);
            PlayerPrefs.SetString("trashroyale.profile", json);
            PlayerPrefs.Save();
        }

        public void RecordWin(int trophyDelta)
        {
            wins++;
            trophies = Mathf.Max(0, trophies + trophyDelta);
            Save();
        }

        public void RecordLoss(int trophyDelta)
        {
            losses++;
            trophies = Mathf.Max(0, trophies - trophyDelta);
            Save();
        }
    }
}
