using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using TrashRoyale.Audio;
using TrashRoyale.Persistence;
using TrashRoyale.UI;
using TrashRoyale.Core;

namespace TrashRoyale.Bootstrap
{
    public class MainMenuBootstrap : MonoBehaviour
    {
        Canvas _canvas;
        TMP_Text _trophiesText;
        PlayerProfile _profile;

        void Start()
        {
            CardDatabase.EnsureLoaded();
            AudioManager.Boot();
            _profile = PlayerProfile.Load();

            EnsureCamera();
            BuildUI();
            AudioManager.PlayMusic("menu_music");
        }

        void EnsureCamera()
        {
            if (Camera.main != null) return;
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            var cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.07f, 0.08f, 0.15f);
            go.AddComponent<AudioListener>();
        }

        void BuildUI()
        {
            var canvasGo = new GameObject("MainMenuCanvas");
            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 1f;
            canvasGo.AddComponent<GraphicRaycaster>();

            var bg = UIFactory.MakePanel(_canvas.transform, "BG", new Color(0.07f, 0.08f, 0.15f));
            var bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;

            var title = UIFactory.MakeText(_canvas.transform, "Title", "TrashRoyale", 130, TextAlignmentOptions.Center);
            var trt = title.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0, 0.78f);
            trt.anchorMax = new Vector2(1, 0.92f);
            trt.offsetMin = trt.offsetMax = Vector2.zero;
            title.color = new Color(1f, 0.85f, 0.2f);

            var subtitle = UIFactory.MakeText(_canvas.transform, "Sub", "by Kuniman", 38, TextAlignmentOptions.Center);
            var srt = subtitle.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0, 0.74f);
            srt.anchorMax = new Vector2(1, 0.78f);
            srt.offsetMin = srt.offsetMax = Vector2.zero;
            subtitle.color = new Color(0.85f, 0.85f, 0.95f);

            var avatar = UIFactory.MakePanel(_canvas.transform, "Avatar", Color.white);
            var art = avatar.GetComponent<RectTransform>();
            art.anchorMin = new Vector2(0.5f, 0.5f);
            art.anchorMax = new Vector2(0.5f, 0.5f);
            art.sizeDelta = new Vector2(360, 360);
            art.anchoredPosition = new Vector2(0, 230);
            var avatarTex = Resources.Load<Texture2D>("Branding/avatar");
            if (avatarTex != null)
            {
                avatar.sprite = Sprite.Create(avatarTex, new Rect(0, 0, avatarTex.width, avatarTex.height), new Vector2(0.5f, 0.5f));
                avatar.color = Color.white;
                avatar.preserveAspect = true;
            }
            else avatar.color = new Color(0.2f, 0.2f, 0.3f);

            _trophiesText = UIFactory.MakeText(_canvas.transform, "Trophies", $"🏆 {_profile.trophies}", 60, TextAlignmentOptions.Center);
            var trophyRt = _trophiesText.GetComponent<RectTransform>();
            trophyRt.anchorMin = new Vector2(0, 0.32f);
            trophyRt.anchorMax = new Vector2(1, 0.4f);
            trophyRt.offsetMin = trophyRt.offsetMax = Vector2.zero;
            _trophiesText.color = new Color(1f, 0.85f, 0.4f);

            var btnPvE = UIFactory.MakeButton(_canvas.transform, "PvE", "Бой за кубки", () =>
            {
                StartPvE();
            });
            var prt1 = btnPvE.GetComponent<RectTransform>();
            prt1.anchorMin = new Vector2(0.18f, 0.22f);
            prt1.anchorMax = new Vector2(0.82f, 0.3f);
            prt1.offsetMin = prt1.offsetMax = Vector2.zero;
            btnPvE.image.color = new Color(0.85f, 0.4f, 0.95f);

            var btnPvP = UIFactory.MakeButton(_canvas.transform, "Friendly", "Дружеский бой (онлайн)", () =>
            {
                FriendlyBattlePopup.Open(_canvas.transform);
            });
            var prt2 = btnPvP.GetComponent<RectTransform>();
            prt2.anchorMin = new Vector2(0.18f, 0.13f);
            prt2.anchorMax = new Vector2(0.82f, 0.21f);
            prt2.offsetMin = prt2.offsetMax = Vector2.zero;
            btnPvP.image.color = new Color(0.3f, 0.7f, 1f);

            var btnDeck = UIFactory.MakeButton(_canvas.transform, "Deck", "Колода", () =>
            {
                DeckEditor.Open(_canvas.transform, _profile);
            });
            var prt3 = btnDeck.GetComponent<RectTransform>();
            prt3.anchorMin = new Vector2(0.18f, 0.04f);
            prt3.anchorMax = new Vector2(0.82f, 0.12f);
            prt3.offsetMin = prt3.offsetMax = Vector2.zero;
            btnDeck.image.color = new Color(0.95f, 0.7f, 0.3f);
        }

        void StartPvE()
        {
            var bot = BotLadder.PickFor(_profile.trophies);
            BattleLauncher.Pending = new BattleLauncher.Request
            {
                isPvE = true,
                playerDeck = new List<string>(_profile.deck),
                enemyDeck = bot.deck,
                botName = bot.name,
                botDifficulty = bot.difficulty,
                trophyDelta = 30
            };
            SceneManager.LoadScene("Battle");
        }
    }
}
