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
        }
        public static Request Pending;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            Pending = null;
        }
    }
}
