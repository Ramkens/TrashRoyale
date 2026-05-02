using UnityEngine;
using UnityEngine.SceneManagement;

namespace TrashRoyale.Bootstrap
{
    /// <summary>
    /// Boot entry: builds menu/battle GameObjects in code so empty scenes work.
    /// </summary>
    public static class BootEntry
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void OnSceneLoaded()
        {
            var name = SceneManager.GetActiveScene().name;
            if (name == "Main")
            {
                if (Object.FindObjectOfType<MainMenuBootstrap>() == null)
                {
                    var go = new GameObject("MainMenuBootstrap");
                    go.AddComponent<MainMenuBootstrap>();
                }
            }
            else if (name == "Battle")
            {
                if (Object.FindObjectOfType<BattleBootstrap>() == null)
                {
                    var go = new GameObject("BattleBootstrap");
                    var bb = go.AddComponent<BattleBootstrap>();
                    var req = BattleLauncher.Pending;
                    if (req != null)
                    {
                        bb.isPvE = req.isPvE;
                        bb.playerDeck = req.playerDeck;
                        bb.enemyDeck = req.enemyDeck;
                        bb.botName = req.botName;
                        bb.botDifficulty = req.botDifficulty;
                        bb.trophyDelta = req.trophyDelta;
                    }
                }
            }
        }
    }
}
