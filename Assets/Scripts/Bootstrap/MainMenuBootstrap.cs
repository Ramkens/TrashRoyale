using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TrashRoyale.Audio;
using TrashRoyale.Persistence;
using TrashRoyale.UI;
using TrashRoyale.Core;

namespace TrashRoyale.Bootstrap
{
    public class MainMenuBootstrap : MonoBehaviour
    {
        Canvas _canvas;
        Text _trophiesText;
        Text _playerNameText;
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

            // Background art
            var bg = UIFactory.MakePanel(_canvas.transform, "BG", new Color(0.13f, 0.18f, 0.32f));
            var bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;
            var bgTex = Resources.Load<Texture2D>("UI/menu_bg");
            if (bgTex != null)
            {
                bg.sprite = Sprite.Create(bgTex, new Rect(0, 0, bgTex.width, bgTex.height), new Vector2(0.5f, 0.5f));
                bg.color = Color.white;
                bg.preserveAspect = false;
                bg.type = Image.Type.Simple;
            }

            // Top player banner: avatar circle + name + trophy
            BuildPlayerBanner();

            // Title
            var title = UIFactory.MakeText(_canvas.transform, "Title", "TRASH ROYALE", 150, TextAnchor.MiddleCenter);
            var trt = title.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0, 0.66f);
            trt.anchorMax = new Vector2(1, 0.78f);
            trt.offsetMin = trt.offsetMax = Vector2.zero;
            title.color = new Color(1f, 0.92f, 0.3f);
            title.fontStyle = FontStyle.BoldAndItalic;
            var titleOutline = title.GetComponent<Outline>();
            if (titleOutline != null) { titleOutline.effectColor = new Color(0.4f, 0.15f, 0f, 1f); titleOutline.effectDistance = new Vector2(5, -5); }

            var subtitle = UIFactory.MakeText(_canvas.transform, "Sub", "POMOIKA EDITION  ·  by Kuniman", 38, TextAnchor.MiddleCenter);
            var srt = subtitle.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0, 0.62f);
            srt.anchorMax = new Vector2(1, 0.66f);
            srt.offsetMin = srt.offsetMax = Vector2.zero;
            subtitle.color = new Color(1f, 0.85f, 0.4f);

            // Big PvE button (red/gold)
            var btnPvE = UIFactory.MakeButton(_canvas.transform, "PvE", "БОЙ ЗА КУБКИ", () =>
            {
                AudioManager.PlaySfx("card_play");
                StartPvE();
            }, new Color(0.95f, 0.35f, 0.25f));
            var prt1 = btnPvE.GetComponent<RectTransform>();
            prt1.anchorMin = new Vector2(0.1f, 0.42f);
            prt1.anchorMax = new Vector2(0.9f, 0.55f);
            prt1.offsetMin = prt1.offsetMax = Vector2.zero;
            

            // Friendly button (blue)
            var btnPvP = UIFactory.MakeButton(_canvas.transform, "Friendly", "ДРУЖЕСКИЙ БОЙ", () =>
            {
                AudioManager.PlaySfx("card_play");
                FriendlyBattlePopup.Open(_canvas.transform);
            }, new Color(0.25f, 0.55f, 0.95f));
            var prt2 = btnPvP.GetComponent<RectTransform>();
            prt2.anchorMin = new Vector2(0.1f, 0.3f);
            prt2.anchorMax = new Vector2(0.9f, 0.4f);
            prt2.offsetMin = prt2.offsetMax = Vector2.zero;
            

            // Deck button (gold)
            var btnDeck = UIFactory.MakeButton(_canvas.transform, "Deck", "КОЛОДА", () =>
            {
                AudioManager.PlaySfx("card_play");
                DeckEditor.Open(_canvas.transform, _profile);
            }, new Color(0.95f, 0.75f, 0.2f));
            var prt3 = btnDeck.GetComponent<RectTransform>();
            prt3.anchorMin = new Vector2(0.1f, 0.18f);
            prt3.anchorMax = new Vector2(0.9f, 0.28f);
            prt3.offsetMin = prt3.offsetMax = Vector2.zero;
            

            // Footer
            var footer = UIFactory.MakeText(_canvas.transform, "Footer", "v0.1.0  ·  Kuniman Studios", 28, TextAnchor.MiddleCenter);
            var frt = footer.GetComponent<RectTransform>();
            frt.anchorMin = new Vector2(0, 0.01f);
            frt.anchorMax = new Vector2(1, 0.05f);
            frt.offsetMin = frt.offsetMax = Vector2.zero;
            footer.color = new Color(1f, 1f, 1f, 0.45f);
        }

        void BuildPlayerBanner()
        {
            // Banner background panel
            var banner = UIFactory.MakePanel(_canvas.transform, "Banner", new Color(0.05f, 0.07f, 0.18f, 0.85f));
            var brt = banner.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0.04f, 0.86f);
            brt.anchorMax = new Vector2(0.96f, 0.97f);
            brt.offsetMin = brt.offsetMax = Vector2.zero;
            var bannerSprite = UIFactory.LoadSprite("UI/banner_blue");
            if (bannerSprite != null) { banner.sprite = bannerSprite; banner.type = Image.Type.Sliced; banner.color = Color.white; }

            // Player name (no avatar - icon only on Android launcher)
            _playerNameText = UIFactory.MakeText(banner.transform, "Name", _profile.playerName, 56, TextAnchor.MiddleLeft);
            var pnrt = _playerNameText.GetComponent<RectTransform>();
            pnrt.anchorMin = new Vector2(0.04f, 0.5f);
            pnrt.anchorMax = new Vector2(0.6f, 1f);
            pnrt.offsetMin = pnrt.offsetMax = Vector2.zero;
            _playerNameText.color = Color.white;
            _playerNameText.fontStyle = FontStyle.Bold;

            // Trophies (with crown)
            _trophiesText = UIFactory.MakeText(banner.transform, "Trophies", _profile.trophies + " К", 60, TextAnchor.MiddleRight);
            var ttrt = _trophiesText.GetComponent<RectTransform>();
            ttrt.anchorMin = new Vector2(0.55f, 0.2f);
            ttrt.anchorMax = new Vector2(0.97f, 0.95f);
            ttrt.offsetMin = ttrt.offsetMax = Vector2.zero;
            _trophiesText.color = new Color(1f, 0.85f, 0.3f);
            _trophiesText.fontStyle = FontStyle.Bold;

            // Tier label below name
            var tier = UIFactory.MakeText(banner.transform, "Tier", BotLadder.TierName(_profile.trophies), 32, TextAnchor.MiddleLeft);
            var trtt = tier.GetComponent<RectTransform>();
            trtt.anchorMin = new Vector2(0.04f, 0.05f);
            trtt.anchorMax = new Vector2(0.6f, 0.5f);
            trtt.offsetMin = trtt.offsetMax = Vector2.zero;
            tier.color = new Color(0.85f, 0.85f, 0.95f);
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
