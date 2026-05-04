using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
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
            try
            {
                CardDatabase.EnsureLoaded();
                AudioManager.Boot();
                EnsureEventSystem();
                EnsureCamera();

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
                BuildBackButton();

                // CR-style banner reveal during the countdown.
                var intro = MatchIntroOverlay.Build();
                intro.StartIntro();

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

                AudioManager.PlayMusic("battle_music", 0.32f);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[BattleBootstrap] Init failed: " + ex);
                BuildErrorScreen(ex);
            }
        }

        void EnsureCamera()
        {
            if (Camera.main != null) return;
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            var cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.18f, 0.22f, 0.45f);
            go.AddComponent<AudioListener>();
        }

        void BuildBackButton()
        {
            var canvasGo = new GameObject("BackButtonCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasGo.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 1f;
            canvasGo.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            var btn = TrashRoyale.UI.UIFactory.MakeButton(canvasGo.transform, "SurrenderBtn", "СДАТЬСЯ", () =>
            {
                ShowSurrenderConfirm(canvasGo.transform);
            });
            var rt = btn.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(20, -20);
            rt.sizeDelta = new Vector2(260, 80);
        }

        void ShowSurrenderConfirm(Transform parent)
        {
            // Modal dim background covering the whole screen.
            var dim = TrashRoyale.UI.UIFactory.MakePanel(parent, "SurrenderDim", new Color(0, 0, 0, 0.78f));
            var drt = dim.GetComponent<RectTransform>();
            drt.anchorMin = Vector2.zero; drt.anchorMax = Vector2.one;
            drt.offsetMin = drt.offsetMax = Vector2.zero;

            var box = TrashRoyale.UI.UIFactory.MakePanel(dim.transform, "SurrenderBox", new Color(0.15f, 0.18f, 0.32f, 1f));
            var brt = box.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0.5f, 0.5f);
            brt.anchorMax = new Vector2(0.5f, 0.5f);
            brt.pivot = new Vector2(0.5f, 0.5f);
            brt.sizeDelta = new Vector2(820, 480);
            brt.anchoredPosition = Vector2.zero;

            var title = TrashRoyale.UI.UIFactory.MakeText(box.transform, "T", "Сдаться?", 70, TextAnchor.MiddleCenter);
            var trt = title.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0, 0.7f); trt.anchorMax = new Vector2(1, 0.95f);
            trt.offsetMin = trt.offsetMax = Vector2.zero;
            title.color = new Color(1f, 0.85f, 0.4f);

            var msg = TrashRoyale.UI.UIFactory.MakeText(box.transform, "M",
                "Противник получит победу и кубки.\nПродолжить?",
                32, TextAnchor.MiddleCenter);
            var mrt = msg.GetComponent<RectTransform>();
            mrt.anchorMin = new Vector2(0.05f, 0.4f); mrt.anchorMax = new Vector2(0.95f, 0.7f);
            mrt.offsetMin = mrt.offsetMax = Vector2.zero;
            msg.color = Color.white;

            var yes = TrashRoyale.UI.UIFactory.MakeButton(box.transform, "Yes", "Да, сдаюсь", () =>
            {
                AudioManager.StopMusic();
                MatchManager.I?.PlayerSurrender();
            });
            var yrt = yes.GetComponent<RectTransform>();
            yrt.anchorMin = new Vector2(0.07f, 0.12f);
            yrt.anchorMax = new Vector2(0.48f, 0.32f);
            yrt.offsetMin = yrt.offsetMax = Vector2.zero;

            var no = TrashRoyale.UI.UIFactory.MakeButton(box.transform, "No", "Отмена", () =>
            {
                Object.Destroy(dim);
            });
            var nrt = no.GetComponent<RectTransform>();
            nrt.anchorMin = new Vector2(0.52f, 0.12f);
            nrt.anchorMax = new Vector2(0.93f, 0.32f);
            nrt.offsetMin = nrt.offsetMax = Vector2.zero;
        }

        void BuildErrorScreen(System.Exception ex)
        {
            var canvasGo = new GameObject("ErrorCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;
            var scaler = canvasGo.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 1f;
            canvasGo.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            var bg = TrashRoyale.UI.UIFactory.MakePanel(canvasGo.transform, "Bg", new Color(0.12f, 0.05f, 0.05f, 0.95f));
            var bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;

            var title = TrashRoyale.UI.UIFactory.MakeText(canvasGo.transform, "T", "БОЙ НЕ ЗАПУСТИЛСЯ", 70, TextAnchor.MiddleCenter);
            var trt = title.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0, 0.7f); trt.anchorMax = new Vector2(1, 0.85f);
            trt.offsetMin = trt.offsetMax = Vector2.zero;
            title.color = new Color(1f, 0.6f, 0.5f);

            var msg = TrashRoyale.UI.UIFactory.MakeText(canvasGo.transform, "M", ex.Message, 26, TextAnchor.MiddleCenter);
            var mrt = msg.GetComponent<RectTransform>();
            mrt.anchorMin = new Vector2(0.05f, 0.4f); mrt.anchorMax = new Vector2(0.95f, 0.7f);
            mrt.offsetMin = mrt.offsetMax = Vector2.zero;
            msg.color = Color.white;
            msg.horizontalOverflow = HorizontalWrapMode.Wrap;
            msg.verticalOverflow = VerticalWrapMode.Truncate;

            var back = TrashRoyale.UI.UIFactory.MakeButton(canvasGo.transform, "Back", "В МЕНЮ", () => SceneManager.LoadScene("Main"));
            var brt = back.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0.2f, 0.18f); brt.anchorMax = new Vector2(0.8f, 0.3f);
            brt.offsetMin = brt.offsetMax = Vector2.zero;
        }

        void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
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
            var match = MatchManager.I;
            int playerCrowns = match != null ? match.PlayerCrowns : 0;
            int enemyCrowns = match != null ? match.EnemyCrowns : 0;
            bool won = winner == Team.Player;
            // Practice (isPvE) is for warm-up against bots - it doesn't move
            // wins/losses or trophies. Online (PvP) updates the win/loss
            // tally for achievements but keeps trophies untouched (the
            // ladder is purely cosmetic in this build).
            if (won)
            {
                if (!isPvE) profile.RecordWin(0);
                AudioManager.PlayOneShot("victory", Vector3.zero);
                _hud.ShowEndScreen("ПОБЕДА!");
            }
            else
            {
                if (!isPvE) profile.RecordLoss(0);
                AudioManager.PlayOneShot("defeat", Vector3.zero);
                _hud.ShowEndScreen("ПОРАЖЕНИЕ");
            }
            profile.totalCrownsScored += playerCrowns;
            var stats = new MatchResultStats
            {
                wonMatch = won,
                playerCrowns = playerCrowns,
                enemyCrowns = enemyCrowns,
                wasPvE = isPvE,
                trophiesAfter = profile.trophies,
                winsAfter = profile.wins,
                lossesAfter = profile.losses,
            };
            var newly = Achievements.EvaluateAfterMatch(profile, stats);
            if (newly.Count > 0)
            {
                AchievementToast.ShowQueue(newly);
            }
        }
    }
}
