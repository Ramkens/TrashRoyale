using UnityEngine;
using UnityEngine.SceneManagement;

namespace TrashRoyale.Bootstrap
{
    /// <summary>
    /// Boot entry: builds menu/battle GameObjects in code so empty scenes work.
    /// Hooks SceneManager.sceneLoaded so EVERY scene load (including Main->Battle)
    /// gets a bootstrap. RuntimeInitializeOnLoadMethod alone only fires once at app start.
    /// </summary>
    public static class BootEntry
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        static void Hook()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void OnFirstSceneLoaded()
        {
            // Cover the very first scene that loads at app launch (sceneLoaded
            // event isn't fired for the initial scene in some Unity versions).
            BootForActiveScene();
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            BootForActiveScene();
        }

        static void BootForActiveScene()
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
