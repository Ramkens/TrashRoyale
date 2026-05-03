using System.Collections.Generic;
using UnityEngine;

namespace TrashRoyale.Bootstrap
{
    public static class BattleLauncher
    {
        public class Request
        {
            public bool isPvE = true;
            public List<string> playerDeck;
            public List<string> enemyDeck;
            public string botName = "Bot";
            public float botDifficulty = 0.5f;
            public int trophyDelta = 30;
            public string netRoomCode;
            public bool netHost;
            // Visual identity for the opponent shown in the match-start
            // banner reveal. Defaults are sane fallbacks when the bot
            // ladder doesn't supply them (e.g. legacy callers).
            public Color botBannerColor = new Color(0.6f, 0.35f, 0.85f);
            public string botIconKey = "fist";
            // Up to 3 secondary badges shown on the bot's intro banner
            // (mirrors the player's equippedBadges list). Filled by
            // BotLadder.PickFor; empty = no extra badges.
            public string[] botBadgeKeys = new string[0];
        }
        public static Request Pending;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            Pending = null;
        }
    }
}
