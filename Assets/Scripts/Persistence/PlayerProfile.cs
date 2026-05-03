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
        public string playerName = "Треш Рояль";
        public List<string> deck = new List<string> { "knight", "pig", "skibidi", "pocoyo", "amongus", "cheems", "nyancat", "fireball" };

        // PR4: persisted achievement progress.
        public List<string> unlockedAchievements = new List<string>();
        // Hex color (#RRGGBB) for the player banner. Awarded by certain
        // tier achievements; empty falls back to the default blue.
        public string bannerColorHex = "";
        // Display preference: which medal/icon to show next to the
        // player name in the menu banner. Defaults to highest-tier
        // unlocked medal — but the user can pin a specific one via the
        // achievements popup.
        public string pinnedMedalKind = "";
        // Up to 3 medals equipped on the banner. Order = display order.
        // Falls back to highest-tier unlocked when empty.
        public List<string> equippedBadges = new List<string>();
        // Persistent anonymous identifier so the cloud-sync layer can
        // attribute progress before the player binds an email.
        public string playerId = "";
        // Lifetime totals — useful for additional achievements down the
        // road (e.g. crowns scored across all matches).
        public int totalCrownsScored = 0;

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
            if (p.equippedBadges == null) p.equippedBadges = new List<string>();
            if (p.unlockedAchievements == null) p.unlockedAchievements = new List<string>();
            if (string.IsNullOrEmpty(p.playerId))
            {
                p.playerId = "guest_" + Guid.NewGuid().ToString("N").Substring(0, 12);
            }
            return p;
        }

        public void Save()
        {
            var json = JsonUtility.ToJson(this);
            PlayerPrefs.SetString("trashroyale.profile", json);
            PlayerPrefs.Save();
            // PR5: best-effort push to the cloud profile if the player
            // is logged in. Failure is silent — local save is canon.
            CloudProfileSync.QueuePush(json);
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
