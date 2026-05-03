using System;
using System.Collections.Generic;
using UnityEngine;

namespace TrashRoyale.Persistence
{
    /// <summary>
    /// Stat-snapshot fed to <see cref="Achievements.EvaluateAfterMatch"/>
    /// so the achievement system has all the per-match info (delta crowns,
    /// whether the match ended in a 3-crown sweep, etc.) without poking
    /// into <see cref="MatchManager"/> directly.
    /// </summary>
    public struct MatchResultStats
    {
        public bool wonMatch;
        public int playerCrowns;     // crowns the local player scored
        public int enemyCrowns;      // crowns the bot/opponent scored
        public bool wasPvE;
        public int trophiesAfter;
        public int winsAfter;
        public int lossesAfter;
    }

    public enum AchievementKind
    {
        FirstWin,
        Trophies100,
        Trophies500,
        Trophies1000,
        Trophies2000,
        Trophies3000,
        Wins10,
        Wins50,
        Wins100,
        ThreeCrownSweep,
        UndefeatedOpponent,    // win without losing any tower
        ComebackKing,          // win after enemy scored 2 crowns first
    }

    [Serializable]
    public class AchievementDef
    {
        public AchievementKind kind;
        public string title;
        public string description;
        public string medalIconKey;   // Resources/Icons/<key>.png
        public Color medalColor;
        public string bannerColorHex; // optional banner reward (#RRGGBB or empty)
    }

    /// <summary>
    /// Static achievement catalog + evaluator. Achievements are stored
    /// as a list of unlocked kind names on <see cref="PlayerProfile"/>;
    /// awarding a medal also optionally swaps the player banner color.
    /// </summary>
    public static class Achievements
    {
        public static readonly AchievementDef[] All = new AchievementDef[]
        {
            new AchievementDef {
                kind = AchievementKind.FirstWin,
                title = "Первая победа",
                description = "Выиграй свой первый бой.",
                medalIconKey = "trophy",
                medalColor = new Color(0.55f, 0.78f, 0.4f),
                bannerColorHex = "",
            },
            new AchievementDef {
                kind = AchievementKind.Trophies100,
                title = "100 Кубков",
                description = "Набери 100 кубков.",
                medalIconKey = "trophy",
                medalColor = new Color(0.86f, 0.86f, 0.92f),
                bannerColorHex = "",
            },
            new AchievementDef {
                kind = AchievementKind.Trophies500,
                title = "Помойный Зал",
                description = "Набери 500 кубков.",
                medalIconKey = "trophy",
                medalColor = new Color(1f, 0.78f, 0.32f),
                bannerColorHex = "#5b8cff",
            },
            new AchievementDef {
                kind = AchievementKind.Trophies1000,
                title = "Тысячник",
                description = "Набери 1000 кубков.",
                medalIconKey = "crown",
                medalColor = new Color(0.96f, 0.85f, 0.32f),
                bannerColorHex = "#ffd84a",
            },
            new AchievementDef {
                kind = AchievementKind.Trophies2000,
                title = "Гига-Лига",
                description = "Набери 2000 кубков.",
                medalIconKey = "crown",
                medalColor = new Color(0.95f, 0.55f, 0.95f),
                bannerColorHex = "#c75bff",
            },
            new AchievementDef {
                kind = AchievementKind.Trophies3000,
                title = "Король Помойки",
                description = "Набери 3000 кубков.",
                medalIconKey = "crown",
                medalColor = new Color(1f, 0.4f, 0.4f),
                bannerColorHex = "#ff5b5b",
            },
            new AchievementDef {
                kind = AchievementKind.Wins10,
                title = "Десятка побед",
                description = "Выиграй 10 боёв.",
                medalIconKey = "fist",
                medalColor = new Color(0.8f, 0.8f, 0.85f),
                bannerColorHex = "",
            },
            new AchievementDef {
                kind = AchievementKind.Wins50,
                title = "Полтинник",
                description = "Выиграй 50 боёв.",
                medalIconKey = "fist",
                medalColor = new Color(1f, 0.78f, 0.32f),
                bannerColorHex = "",
            },
            new AchievementDef {
                kind = AchievementKind.Wins100,
                title = "Сотня побед",
                description = "Выиграй 100 боёв.",
                medalIconKey = "crossed_swords",
                medalColor = new Color(0.95f, 0.45f, 0.95f),
                bannerColorHex = "#9b3bff",
            },
            new AchievementDef {
                kind = AchievementKind.ThreeCrownSweep,
                title = "Тройной Краун",
                description = "Выиграй с тремя коронами.",
                medalIconKey = "crown",
                medalColor = new Color(0.96f, 0.85f, 0.32f),
                bannerColorHex = "",
            },
            new AchievementDef {
                kind = AchievementKind.UndefeatedOpponent,
                title = "Без Ран",
                description = "Победи, не потеряв ни одной башни.",
                medalIconKey = "shield",
                medalColor = new Color(0.4f, 0.85f, 0.95f),
                bannerColorHex = "",
            },
            new AchievementDef {
                kind = AchievementKind.ComebackKing,
                title = "Камбэк-Принц",
                description = "Победи, после того как противник снёс две твои башни.",
                medalIconKey = "fist",
                medalColor = new Color(1f, 0.5f, 0.2f),
                bannerColorHex = "#ff8a3d",
            },
        };

        public static AchievementDef Find(AchievementKind k)
        {
            foreach (var a in All) if (a.kind == k) return a;
            return null;
        }

        // String overload — handy for the equippedBadges list which
        // stores medal kinds as strings (since profiles persist via
        // JsonUtility and AchievementKind enum values can be renumbered
        // between releases).
        public static AchievementDef Find(string kindString)
        {
            if (string.IsNullOrEmpty(kindString)) return null;
            foreach (var a in All)
                if (a.kind.ToString() == kindString) return a;
            return null;
        }

        /// <summary>
        /// Evaluate post-match stats and unlock any newly-earned medals.
        /// Returns the list of NEWLY unlocked achievements (so the
        /// battle bootstrap can show toasts). Mutates and saves the
        /// profile.
        /// </summary>
        public static List<AchievementDef> EvaluateAfterMatch(PlayerProfile profile, MatchResultStats stats)
        {
            var newly = new List<AchievementDef>();
            void Try(AchievementKind k, bool cond)
            {
                if (!cond) return;
                if (profile.unlockedAchievements == null)
                    profile.unlockedAchievements = new List<string>();
                if (profile.unlockedAchievements.Contains(k.ToString())) return;
                profile.unlockedAchievements.Add(k.ToString());
                var def = Find(k);
                if (def != null) newly.Add(def);
            }

            Try(AchievementKind.FirstWin, stats.wonMatch && stats.winsAfter >= 1);
            Try(AchievementKind.Trophies100, stats.trophiesAfter >= 100);
            Try(AchievementKind.Trophies500, stats.trophiesAfter >= 500);
            Try(AchievementKind.Trophies1000, stats.trophiesAfter >= 1000);
            Try(AchievementKind.Trophies2000, stats.trophiesAfter >= 2000);
            Try(AchievementKind.Trophies3000, stats.trophiesAfter >= 3000);
            Try(AchievementKind.Wins10, stats.winsAfter >= 10);
            Try(AchievementKind.Wins50, stats.winsAfter >= 50);
            Try(AchievementKind.Wins100, stats.winsAfter >= 100);
            Try(AchievementKind.ThreeCrownSweep, stats.wonMatch && stats.playerCrowns >= 3);
            Try(AchievementKind.UndefeatedOpponent, stats.wonMatch && stats.enemyCrowns == 0);
            Try(AchievementKind.ComebackKing, stats.wonMatch && stats.enemyCrowns >= 2 && stats.playerCrowns > stats.enemyCrowns);

            // Apply banner reward of the most-recent unlock (highest tier
            // wins), if any provides a hex.
            for (int i = newly.Count - 1; i >= 0; i--)
            {
                var def = newly[i];
                if (!string.IsNullOrEmpty(def.bannerColorHex))
                {
                    profile.bannerColorHex = def.bannerColorHex;
                    break;
                }
            }
            profile.Save();
            return newly;
        }
    }
}
