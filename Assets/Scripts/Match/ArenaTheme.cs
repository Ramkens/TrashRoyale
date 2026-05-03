using UnityEngine;

namespace TrashRoyale.Match
{
    /// <summary>
    /// Visual recolouring of the arena based on the player's current
    /// trophy count. Roughly 250 trophies between tiers, 10 themes total.
    ///
    /// Themes only swap colour tints + the sky / camera clear colour;
    /// they keep the same layout (towers, river, bridge positions) so
    /// the gameplay feel stays consistent between arenas.
    /// </summary>
    public class ArenaTheme
    {
        public string DisplayName;
        // Resources path key (without extension) for the preview thumbnail
        // shown in Road of Glory and the main-menu arena banner. Lookup
        // happens via Resources.Load<Texture2D>("ArenaThumbs/" + ThumbnailKey).
        // Empty / missing = falls back to a flat color tile.
        public string ThumbnailKey;
        public Color SkyTop;            // camera clear colour (top of viewport)
        public Color PlayerSideTint;    // tint applied to grass on the local side
        public Color EnemySideTint;     // tint applied to grass on the remote side
        public Color RiverTint;
        public Color BridgeTint;
        public Color PlayerTowerTint;
        public Color EnemyTowerTint;
        public Color PlayerKingTint;
        public Color EnemyKingTint;
        public Color FogColor;
        public float FogDensity;        // 0 = no fog

        // Themes ordered by unlock trophy count. Index 0 = beginner, last = legendary.
        // Spacing matches the user's design (~250 trophies between arenas) and a
        // bonus 500 step on the final endgame arena.
        public static readonly ArenaTheme[] All = new[]
        {
            // 0 — 0+ kубков. Старая помойка, зелень с фиолетовым отливом.
            new ArenaTheme {
                DisplayName = "Помойка",
                ThumbnailKey = "arena_0",
                SkyTop = new Color(0.18f, 0.22f, 0.45f),
                PlayerSideTint = new Color(0.85f, 1.0f, 0.85f),
                EnemySideTint = new Color(0.95f, 0.85f, 1.0f),
                RiverTint = new Color(1f, 1f, 1f),
                BridgeTint = new Color(1f, 1f, 1f),
                PlayerTowerTint = new Color(0.55f, 0.75f, 1f),
                EnemyTowerTint = new Color(1f, 0.55f, 0.55f),
                PlayerKingTint = new Color(0.4f, 0.55f, 1f),
                EnemyKingTint = new Color(0.95f, 0.45f, 0.45f),
                FogColor = new Color(0.5f, 0.55f, 0.7f), FogDensity = 0f,
            },
            // 1 — 250 kубков. Стоновая крепость в песках.
            new ArenaTheme {
                DisplayName = "Каменный Двор",
                ThumbnailKey = "arena_1",
                SkyTop = new Color(0.32f, 0.40f, 0.22f),
                PlayerSideTint = new Color(0.55f, 0.78f, 0.45f),
                EnemySideTint = new Color(0.40f, 0.55f, 0.30f),
                RiverTint = new Color(0.5f, 0.7f, 0.5f),
                BridgeTint = new Color(0.65f, 0.55f, 0.4f),
                PlayerTowerTint = new Color(0.75f, 0.85f, 0.6f),
                EnemyTowerTint = new Color(0.8f, 0.55f, 0.4f),
                PlayerKingTint = new Color(0.55f, 0.7f, 0.4f),
                EnemyKingTint = new Color(0.7f, 0.4f, 0.3f),
                FogColor = new Color(0.45f, 0.55f, 0.45f), FogDensity = 0.012f,
            },
            // 2 — 500 kубков. Колизей с баннерами.
            new ArenaTheme {
                DisplayName = "Колизей",
                ThumbnailKey = "arena_2",
                SkyTop = new Color(0.28f, 0.30f, 0.34f),
                PlayerSideTint = new Color(0.60f, 0.62f, 0.66f),
                EnemySideTint = new Color(0.55f, 0.55f, 0.60f),
                RiverTint = new Color(0.7f, 0.78f, 0.85f),
                BridgeTint = new Color(0.55f, 0.55f, 0.55f),
                PlayerTowerTint = new Color(0.7f, 0.78f, 0.95f),
                EnemyTowerTint = new Color(0.95f, 0.7f, 0.6f),
                PlayerKingTint = new Color(0.5f, 0.65f, 0.95f),
                EnemyKingTint = new Color(0.85f, 0.45f, 0.4f),
                FogColor = new Color(0.5f, 0.55f, 0.6f), FogDensity = 0.014f,
            },
            // 3 — 750 kубков. Тренировочная башня с красными флагами.
            new ArenaTheme {
                DisplayName = "Боевая Башня",
                ThumbnailKey = "arena_3",
                SkyTop = new Color(0.35f, 0.10f, 0.10f),
                PlayerSideTint = new Color(0.95f, 0.55f, 0.40f),
                EnemySideTint = new Color(1.0f, 0.45f, 0.25f),
                RiverTint = new Color(1f, 0.6f, 0.2f),
                BridgeTint = new Color(0.45f, 0.20f, 0.15f),
                PlayerTowerTint = new Color(0.95f, 0.85f, 0.45f),
                EnemyTowerTint = new Color(1.0f, 0.55f, 0.40f),
                PlayerKingTint = new Color(0.95f, 0.70f, 0.30f),
                EnemyKingTint = new Color(0.95f, 0.30f, 0.10f),
                FogColor = new Color(0.7f, 0.3f, 0.15f), FogDensity = 0.020f,
            },
            // 4 — 1000 kубков. Магическая шахта с кристаллами.
            new ArenaTheme {
                DisplayName = "Кристалльная Шахта",
                ThumbnailKey = "arena_4",
                SkyTop = new Color(0.42f, 0.34f, 0.18f),
                PlayerSideTint = new Color(1.0f, 0.95f, 0.65f),
                EnemySideTint = new Color(1.0f, 0.85f, 0.50f),
                RiverTint = new Color(1f, 0.95f, 0.5f),
                BridgeTint = new Color(0.9f, 0.75f, 0.35f),
                PlayerTowerTint = new Color(1.0f, 0.95f, 0.5f),
                EnemyTowerTint = new Color(0.95f, 0.65f, 0.35f),
                PlayerKingTint = new Color(1f, 0.85f, 0.30f),
                EnemyKingTint = new Color(0.95f, 0.45f, 0.30f),
                FogColor = new Color(0.85f, 0.75f, 0.45f), FogDensity = 0.008f,
            },
            // 5 — 1250 kубков. Гоблинская пилорама.
            new ArenaTheme {
                DisplayName = "Пилорама",
                ThumbnailKey = "arena_5",
                SkyTop = new Color(0.65f, 0.30f, 0.40f),
                PlayerSideTint = new Color(1.0f, 0.78f, 0.65f),
                EnemySideTint = new Color(0.95f, 0.55f, 0.65f),
                RiverTint = new Color(1f, 0.65f, 0.7f),
                BridgeTint = new Color(0.65f, 0.40f, 0.45f),
                PlayerTowerTint = new Color(1.0f, 0.85f, 0.85f),
                EnemyTowerTint = new Color(0.95f, 0.55f, 0.65f),
                PlayerKingTint = new Color(1f, 0.55f, 0.55f),
                EnemyKingTint = new Color(0.85f, 0.30f, 0.45f),
                FogColor = new Color(0.85f, 0.55f, 0.55f), FogDensity = 0.010f,
            },
            // 6 — 1500 kубков. Кузница варваров.
            new ArenaTheme {
                DisplayName = "Кузница",
                ThumbnailKey = "arena_6",
                SkyTop = new Color(0.55f, 0.78f, 0.95f),
                PlayerSideTint = new Color(0.85f, 0.95f, 1.0f),
                EnemySideTint = new Color(0.75f, 0.90f, 1.0f),
                RiverTint = new Color(0.4f, 0.7f, 1f),
                BridgeTint = new Color(0.85f, 0.95f, 1.0f),
                PlayerTowerTint = new Color(0.7f, 0.92f, 1.0f),
                EnemyTowerTint = new Color(0.85f, 0.65f, 1.0f),
                PlayerKingTint = new Color(0.4f, 0.75f, 1f),
                EnemyKingTint = new Color(0.7f, 0.5f, 1f),
                FogColor = new Color(0.85f, 0.92f, 1f), FogDensity = 0.015f,
            },
            // 7 — 1750 kубков. Королевский двор с флагами.
            new ArenaTheme {
                DisplayName = "Королевский Двор",
                ThumbnailKey = "arena_7",
                SkyTop = new Color(0.05f, 0.04f, 0.20f),
                PlayerSideTint = new Color(0.30f, 0.25f, 0.55f),
                EnemySideTint = new Color(0.20f, 0.15f, 0.45f),
                RiverTint = new Color(0.4f, 0.3f, 0.85f),
                BridgeTint = new Color(0.25f, 0.20f, 0.40f),
                PlayerTowerTint = new Color(0.55f, 0.85f, 1.0f),
                EnemyTowerTint = new Color(0.95f, 0.55f, 1.0f),
                PlayerKingTint = new Color(0.35f, 0.65f, 1f),
                EnemyKingTint = new Color(0.95f, 0.35f, 0.85f),
                FogColor = new Color(0.10f, 0.10f, 0.30f), FogDensity = 0.022f,
            },
            // 8 — 2000 kубков. Ледяная арена.
            new ArenaTheme {
                DisplayName = "Ледяная Арена",
                ThumbnailKey = "arena_8",
                SkyTop = new Color(0.05f, 0.10f, 0.20f),
                PlayerSideTint = new Color(0.30f, 0.85f, 0.95f),
                EnemySideTint = new Color(0.95f, 0.30f, 0.85f),
                RiverTint = new Color(0.40f, 0.95f, 0.90f),
                BridgeTint = new Color(0.20f, 0.20f, 0.30f),
                PlayerTowerTint = new Color(0.30f, 1.00f, 0.95f),
                EnemyTowerTint = new Color(1.00f, 0.30f, 0.85f),
                PlayerKingTint = new Color(0.20f, 0.95f, 0.85f),
                EnemyKingTint = new Color(1f, 0.20f, 0.65f),
                FogColor = new Color(0.10f, 0.20f, 0.30f), FogDensity = 0.018f,
            },
            // 9 — 2500 kубков. Сад голема / серпентин.
            new ArenaTheme {
                DisplayName = "Сад Голема",
                ThumbnailKey = "arena_9",
                SkyTop = new Color(0.20f, 0.05f, 0.12f),
                PlayerSideTint = new Color(0.95f, 0.85f, 0.45f),
                EnemySideTint = new Color(0.65f, 0.20f, 0.30f),
                RiverTint = new Color(1f, 0.85f, 0.30f),
                BridgeTint = new Color(0.55f, 0.45f, 0.20f),
                PlayerTowerTint = new Color(1.0f, 0.95f, 0.45f),
                EnemyTowerTint = new Color(1.0f, 0.30f, 0.30f),
                PlayerKingTint = new Color(1.0f, 0.85f, 0.20f),
                EnemyKingTint = new Color(0.85f, 0.10f, 0.20f),
                FogColor = new Color(0.40f, 0.10f, 0.15f), FogDensity = 0.012f,
            },
            // 10 — 2500 kубков. Неон-мегаполис (cyber endgame).
            new ArenaTheme {
                DisplayName = "Мегаполис",
                ThumbnailKey = "arena_10",
                SkyTop = new Color(0.05f, 0.10f, 0.20f),
                PlayerSideTint = new Color(0.30f, 0.85f, 0.95f),
                EnemySideTint = new Color(0.95f, 0.30f, 0.85f),
                RiverTint = new Color(0.40f, 0.95f, 0.90f),
                BridgeTint = new Color(0.20f, 0.20f, 0.30f),
                PlayerTowerTint = new Color(0.30f, 1.00f, 0.95f),
                EnemyTowerTint = new Color(1.00f, 0.30f, 0.85f),
                PlayerKingTint = new Color(0.20f, 0.95f, 0.85f),
                EnemyKingTint = new Color(1f, 0.20f, 0.65f),
                FogColor = new Color(0.10f, 0.20f, 0.30f), FogDensity = 0.018f,
            },
        };

        public static int IndexFor(int trophies)
        {
            // 11 tiers, 250-trophy steps. The user supplied 11 arena
            // preview thumbnails so we run from 0 — "Помойка" all
            // the way to the legendary 10 — "Мегаполис".
            if (trophies < 250)  return 0;
            if (trophies < 500)  return 1;
            if (trophies < 750)  return 2;
            if (trophies < 1000) return 3;
            if (trophies < 1250) return 4;
            if (trophies < 1500) return 5;
            if (trophies < 1750) return 6;
            if (trophies < 2000) return 7;
            if (trophies < 2250) return 8;
            if (trophies < 2500) return 9;
            return 10;
        }

        public static int TrophyFloor(int idx)
        {
            switch (idx)
            {
                case 0: return 0;
                case 1: return 250;
                case 2: return 500;
                case 3: return 750;
                case 4: return 1000;
                case 5: return 1250;
                case 6: return 1500;
                case 7: return 1750;
                case 8: return 2000;
                case 9: return 2250;
                default: return 2500;
            }
        }

        public static ArenaTheme Current(int trophies) => All[IndexFor(trophies)];
    }
}
