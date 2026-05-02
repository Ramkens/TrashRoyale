using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TrashRoyale.Core;
using TrashRoyale.Match;
using TrashRoyale.AI;
using TrashRoyale.Audio;
using TrashRoyale.Persistence;
using TrashRoyale.UI;
using TrashRoyale.Combat;

namespace TrashRoyale.Bootstrap
{
    public class BattleBootstrap : MonoBehaviour
    {
        public bool isPvE = true;
        public List<string> playerDeck;
        public List<string> enemyDeck;
        public string botName = "Bot";
        public float botDifficulty = 0.5f;
        public int trophyDelta = 30;

        UIBattleHud _hud;
        BotController _bot;

        void Start()
        {
            CardDatabase.EnsureLoaded();
            AudioManager.Boot();

            CombatRegistry.Clear();

            var arenaGo = new GameObject("Arena");
            var arena = arenaGo.AddComponent<ArenaController>();

            var matchGo = new GameObject("MatchManager");
            var match = matchGo.AddComponent<MatchManager>();

            ArenaBuilder.Build(match, arena);

            var profile = PlayerProfile.Load();
            if (playerDeck == null || playerDeck.Count != 8) playerDeck = new List<string>(profile.deck);
            if (enemyDeck == null || enemyDeck.Count != 8) enemyDeck = SampleEnemyDeck();

            match.InitMatch(playerDeck, enemyDeck, isPvE);

            _hud = UIBattleHud.Build(Camera.main);
            match.OnMatchEnded += OnMatchEnded;

            if (isPvE)
            {
                _bot = matchGo.AddComponent<BotController>();
                _bot.Difficulty = botDifficulty;
            }
            else
            {
                var net = matchGo.GetComponent<TrashRoyale.Net.NetMatchSync>() ?? matchGo.AddComponent<TrashRoyale.Net.NetMatchSync>();
                net.AttachTo(match);
            }

            AudioManager.PlayMusic("battle_music");
        }

        List<string> SampleEnemyDeck()
        {
            var ids = new List<string>();
            CardDatabase.EnsureLoaded();
            foreach (var c in CardDatabase.All) ids.Add(c.id);
            // shuffle
            for (int i = ids.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (ids[i], ids[j]) = (ids[j], ids[i]);
            }
            if (ids.Count > 8) ids.RemoveRange(8, ids.Count - 8);
            return ids;
        }

        void OnMatchEnded(Team winner)
        {
            AudioManager.StopMusic();
            var profile = PlayerProfile.Load();
            if (winner == Team.Player)
            {
                profile.RecordWin(isPvE ? trophyDelta : 0);
                AudioManager.PlayOneShot("victory", Vector3.zero);
                _hud.ShowEndScreen("ПОБЕДА!");
            }
            else
            {
                profile.RecordLoss(isPvE ? trophyDelta : 0);
                AudioManager.PlayOneShot("defeat", Vector3.zero);
                _hud.ShowEndScreen("ПОРАЖЕНИЕ");
            }
        }
    }
}
