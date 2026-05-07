using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TrashRoyale.Audio;
using TrashRoyale.Persistence;
using TrashRoyale.UI;
using TrashRoyale.Core;
using TrashRoyale.Match;
using TrashRoyale.Net;

namespace TrashRoyale.Bootstrap
{
    public class MainMenuBootstrap : MonoBehaviour
    {
        Canvas _canvas;
        Text _trophiesText;
        Text _playerNameText;
        Text _tierText;
        Transform _deckPreview;
        PlayerProfile _profile;

        void Start()
        {
            CardDatabase.EnsureLoaded();
            AudioManager.Boot();
            _profile = PlayerProfile.Load();

            EnsureCamera();
            EnsureEventSystem();
            BuildUI();
            AudioManager.PlayMusic("menu_music");
            // PR5: kick off the cloud-sync singleton so saves get
            // pushed even if the user never opens the login popup
            // (no-ops while logged out).
            CloudProfileSync.EnsureBooted();

            // If app was launched via deep link (trashroyale://join/CODE or
            // https://trashroyale-relay.onrender.com/join/CODE), auto-open the
            // friendly battle popup with the code prefilled.
            var pendingCode = TrashRoyale.Net.DeepLinkHandler.ConsumeJoinCode();
            if (!string.IsNullOrEmpty(pendingCode))
            {
                FriendlyBattlePopup.Open(_canvas.transform, pendingCode);
            }
            // Login is no longer surfaced at startup — players should be
            // able to play immediately. Account-linking (so progress can
            // sync across devices) lives in Settings → "Привязать
            // аккаунт", reached from the gear icon in the main menu.
        }

        void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }

        void EnsureCamera()
        {
            if (Camera.main != null) return;
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            var cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.07f, 0.13f, 0.32f);
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

            BuildBackground();
            BuildPlayerBanner();
            BuildArenaBanner();
            // BuildTitle() removed: the giant "TRASH ROYALE" word
            // overlapped the new arena banner. The game already has
            // a splash logo on launch; in-menu we lean on the arena
            // art instead, exactly like Clash Royale does.
            BuildBattleCenter();
            BuildBottomNav();
        }

        void BuildBackground()
        {
            var bg = UIFactory.MakePanel(_canvas.transform, "BG", new Color(0.13f, 0.32f, 0.6f));
            bg.raycastTarget = false;
            var bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;
            var bgSprite = UIFactory.LoadSprite("UI/menu_bg");
            if (bgSprite != null) { bg.sprite = bgSprite; bg.color = Color.white; }
        }

        void BuildPlayerBanner()
        {
            var banner = UIFactory.MakePanel(_canvas.transform, "Banner", new Color(0.05f, 0.07f, 0.18f, 0.9f));
            var brt = banner.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0.04f, 0.88f);
            brt.anchorMax = new Vector2(0.96f, 0.97f);
            brt.offsetMin = brt.offsetMax = Vector2.zero;
            var btnSp = UIFactory.LoadSprite("UI/btn_gold");
            // Banner color = unlocked-banner reward (PR4) or default blue.
            Color bannerColor = new Color(0.25f, 0.45f, 0.85f, 1f);
            if (!string.IsNullOrEmpty(_profile.bannerColorHex) &&
                ColorUtility.TryParseHtmlString(_profile.bannerColorHex, out var c))
            {
                bannerColor = c;
            }
            if (btnSp != null) { banner.sprite = btnSp; banner.type = Image.Type.Sliced; banner.color = bannerColor; }

            // Tap the banner area to open the unified profile screen
            // (nick / banner / achievements / road to glory / account
            // binding all live there). Trophy zone keeps its own hit
            // area for "Дорога Славы".
            var bannerBtn = banner.gameObject.AddComponent<Button>();
            bannerBtn.targetGraphic = banner;
            bannerBtn.onClick.AddListener(() =>
            {
                AudioManager.PlaySfx("click");
                ProfilePopup.Open(_canvas.transform, _profile, RefreshBanner);
            });

            // Medals strip removed per design refresh — the menu now
            // shows only the player name + trophies / tier band, and
            // the arena thumbnail above the BATTLE button doubles as
            // the entry point to Road of Glory.

            // Player name (tap banner to edit)
            _playerNameText = UIFactory.MakeText(banner.transform, "Name", _profile.playerName, 50, TextAnchor.MiddleLeft);
            var pnrt = _playerNameText.GetComponent<RectTransform>();
            pnrt.anchorMin = new Vector2(0.05f, 0.5f);
            pnrt.anchorMax = new Vector2(0.62f, 1f);
            pnrt.offsetMin = pnrt.offsetMax = Vector2.zero;
            _playerNameText.color = Color.white;

            // Tier label below name
            _tierText = UIFactory.MakeText(banner.transform, "Tier", BotLadder.TierName(_profile.trophies), 28, TextAnchor.MiddleLeft);
            var trtt = _tierText.GetComponent<RectTransform>();
            trtt.anchorMin = new Vector2(0.05f, 0.05f);
            trtt.anchorMax = new Vector2(0.62f, 0.5f);
            trtt.offsetMin = trtt.offsetMax = Vector2.zero;
            _tierText.color = Color.white;

            // Trophies count (right side)
            _trophiesText = UIFactory.MakeText(banner.transform, "Trophies", _profile.trophies + " \u00A0\u00A0", 64, TextAnchor.MiddleRight);
            var ttrt = _trophiesText.GetComponent<RectTransform>();
            ttrt.anchorMin = new Vector2(0.65f, 0.15f);
            ttrt.anchorMax = new Vector2(0.97f, 0.85f);
            ttrt.offsetMin = ttrt.offsetMax = Vector2.zero;
            _trophiesText.color = new Color(1f, 0.93f, 0.4f);

            var trLabel = UIFactory.MakeText(banner.transform, "TrophiesLabel", "\u041a\u0423\u0411\u041a\u0418", 18, TextAnchor.MiddleRight);
            var trLR = trLabel.GetComponent<RectTransform>();
            trLR.anchorMin = new Vector2(0.65f, 0f);
            trLR.anchorMax = new Vector2(0.97f, 0.18f);
            trLR.offsetMin = trLR.offsetMax = Vector2.zero;
            trLabel.color = new Color(1f, 1f, 1f, 0.7f);

            // Tap trophies area → "Дорога Славы" popup with all arenas.
            // The trophies count + tier name + label all open the same screen.
            var trophyHit = UIFactory.MakePanel(banner.transform, "TrophiesHit", new Color(0, 0, 0, 0.0001f));
            var thr = trophyHit.GetComponent<RectTransform>();
            thr.anchorMin = new Vector2(0.62f, 0f);
            thr.anchorMax = new Vector2(1f, 1f);
            thr.offsetMin = thr.offsetMax = Vector2.zero;
            trophyHit.raycastTarget = true;
            var thb = trophyHit.gameObject.AddComponent<Button>();
            thb.targetGraphic = trophyHit;
            thb.onClick.AddListener(() =>
            {
                AudioManager.PlaySfx("click");
                RoadToGloryPopup.Open(_canvas.transform, _profile);
            });
        }

        void RefreshBanner()
        {
            if (_playerNameText != null) _playerNameText.text = _profile.playerName;
            if (_trophiesText != null) _trophiesText.text = _profile.trophies.ToString();
            if (_tierText != null) _tierText.text = BotLadder.TierName(_profile.trophies);
        }

        // Show up to 4 medal icons next to the player name. Tap opens
        // the AchievementsPopup with the full grid + lock states.
        void BuildMedalsStrip(Transform parent)
        {
            var strip = UIFactory.MakePanel(parent, "MedalsStrip", new Color(0, 0, 0, 0.0f));
            strip.raycastTarget = true;
            var srt = strip.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0.34f, 0.1f);
            srt.anchorMax = new Vector2(0.62f, 0.95f);
            srt.offsetMin = srt.offsetMax = Vector2.zero;

            // Show equipped badges first; fallback to highest-tier
            // unlocked when nothing is pinned. Capped at 3 like CR.
            var unlocked = _profile.unlockedAchievements ?? new System.Collections.Generic.List<string>();
            var defs = new System.Collections.Generic.List<AchievementDef>();
            if (_profile.equippedBadges != null && _profile.equippedBadges.Count > 0)
            {
                foreach (var key in _profile.equippedBadges)
                {
                    var def = Achievements.Find(key);
                    if (def != null && unlocked.Contains(def.kind.ToString())) defs.Add(def);
                    if (defs.Count >= 3) break;
                }
            }
            if (defs.Count == 0)
            {
                for (int i = Achievements.All.Length - 1; i >= 0 && defs.Count < 3; i--)
                {
                    var def = Achievements.All[i];
                    if (unlocked.Contains(def.kind.ToString())) defs.Add(def);
                }
            }
            int shown = 0;
            int slotCount = Mathf.Max(1, defs.Count);
            foreach (var def in defs)
            {
                var iconImg = UIFactory.MakeIcon(strip.transform, "Medal_" + def.kind,
                    "Icons/" + def.medalIconKey, new Vector2(60, 60));
                iconImg.color = def.medalColor;
                var irt = iconImg.GetComponent<RectTransform>();
                float w = 1f / slotCount;
                irt.anchorMin = new Vector2(shown * w + 0.02f, 0.15f);
                irt.anchorMax = new Vector2((shown + 1) * w - 0.02f, 0.85f);
                irt.offsetMin = irt.offsetMax = Vector2.zero;
                shown++;
            }

            var stripBtn = strip.gameObject.AddComponent<Button>();
            stripBtn.targetGraphic = strip;
            stripBtn.onClick.AddListener(() =>
            {
                AudioManager.PlaySfx("click");
                AchievementsPopup.Open(_canvas.transform, _profile);
            });
        }

        void BuildTitle()
        {
            var title = UIFactory.MakeText(_canvas.transform, "Title", "TRASH ROYALE", 110, TextAnchor.MiddleCenter);
            var trt = title.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0, 0.74f);
            trt.anchorMax = new Vector2(1, 0.86f);
            trt.offsetMin = trt.offsetMax = Vector2.zero;
            title.color = new Color(1f, 0.93f, 0.45f);
            var outline = title.GetComponent<Outline>();
            if (outline != null)
            {
                outline.effectColor = new Color(0f, 0f, 0f, 1f);
                outline.effectDistance = new Vector2(7, -7);
            }
            var shadow = title.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
            shadow.effectDistance = new Vector2(0, -10);
        }

        void BuildBattleCenter()
        {
            // Home screen no longer shows the 8-card deck strip — the
            // user asked to drop it ("не показывай колоду на главное
            // экране") since editing happens through the КАРТЫ tab. The
            // home is just: banner / arena banner / БОЙ button / nav.
            //
            // Big PvE button. Anchored just above the bottom nav so the
            // arena banner above can take the full mid-screen real
            // estate. Tap → StartPvE.
            var btnPvE = UIFactory.MakeButton(_canvas.transform, "PvE", "БОЙ ЗА КУБКИ", () =>
            {
                AudioManager.PlaySfx("card_play");
                StartPvE();
            });
            var prt = btnPvE.GetComponent<RectTransform>();
            prt.anchorMin = new Vector2(0.10f, 0.20f);
            prt.anchorMax = new Vector2(0.90f, 0.36f);
            prt.offsetMin = prt.offsetMax = Vector2.zero;

            _deckPreview = null;
        }

        // Deck preview removed from the home screen. The DeckEditor
        // (opened by the КАРТЫ tab) now owns the full deck UI. We keep
        // the method as a no-op so DeckEditor can still poke us when it
        // saves — wiring stays compatible without re-plumbing callers.
        void RebuildDeckPreview()
        {
            // Intentionally empty.
        }

        void BuildBottomNav()
        {
            // CR-style tab bar: 3 icon buttons inset on a dark plate.
            // Per user spec ("вкладка с картами - колода, мечи -
            // главный, мечи в щите - тренировка") and the reference
            // screenshot, we ship just three tabs and the central
            // "swords" tab is the active highlighted one (we're already
            // on the home screen so its tap is a no-op). Chests,
            // Pass Royale, and the laurel tab are deliberately omitted.
            var nav = UIFactory.MakePanel(_canvas.transform, "BottomNav", new Color(0.04f, 0.06f, 0.14f, 0.92f));
            var nrt = nav.GetComponent<RectTransform>();
            nrt.anchorMin = new Vector2(0, 0);
            nrt.anchorMax = new Vector2(1, 0.13f);
            nrt.offsetMin = nrt.offsetMax = Vector2.zero;

            // Left tab: cards icon → DeckEditor.
            BuildIconTab(
                nav.transform, "CardsTab",
                iconResource: "Icons/card_play",
                label: "КАРТЫ",
                anchorMinX: 0.02f, anchorMaxX: 0.34f,
                active: false,
                onClick: () =>
                {
                    AudioManager.PlaySfx("click");
                    DeckEditor.Open(_canvas.transform, _profile, RebuildDeckPreview);
                });

            // Center tab: crossed swords → main battle screen (active).
            BuildIconTab(
                nav.transform, "BattleTab",
                iconResource: "Icons/crossed_swords",
                label: "БОЙ",
                anchorMinX: 0.34f, anchorMaxX: 0.66f,
                active: true,
                onClick: () =>
                {
                    AudioManager.PlaySfx("click");
                    // Already on home — just bounce focus.
                });

            // Right tab: shield → Training PvE popup.
            BuildIconTab(
                nav.transform, "TrainingTab",
                iconResource: "Icons/shield",
                label: "ТРЕНИРОВКА",
                anchorMinX: 0.66f, anchorMaxX: 0.98f,
                active: false,
                onClick: () =>
                {
                    AudioManager.PlaySfx("click");
                    TrainingPopup.Open(_canvas.transform);
                });

            BuildSettingsButton();
        }

        // Builds one CR-style nav tab: an icon stacked over a small
        // label inside a clickable plate. The active tab gets a yellow
        // raised plate (using the existing btn_gold sprite) so it pops
        // from the dark bar; inactive tabs are flat-transparent.
        void BuildIconTab(Transform parent, string name, string iconResource, string label,
            float anchorMinX, float anchorMaxX, bool active, UnityEngine.Events.UnityAction onClick)
        {
            var plate = UIFactory.MakePanel(parent, name,
                active ? new Color(1f, 0.78f, 0.15f, 1f) : new Color(0f, 0f, 0f, 0.001f));
            if (active)
            {
                var btnSp = UIFactory.LoadSprite("UI/btn_gold");
                if (btnSp != null) { plate.sprite = btnSp; plate.type = Image.Type.Sliced; plate.color = Color.white; }
            }
            var prt = plate.GetComponent<RectTransform>();
            prt.anchorMin = new Vector2(anchorMinX, 0.10f);
            prt.anchorMax = new Vector2(anchorMaxX, 0.92f);
            prt.offsetMin = prt.offsetMax = Vector2.zero;

            var btn = plate.gameObject.AddComponent<Button>();
            btn.targetGraphic = plate;
            btn.onClick.AddListener(onClick);

            var icon = UIFactory.MakeIcon(plate.transform, name + "_Icon", iconResource, new Vector2(96, 96));
            var irt = icon.GetComponent<RectTransform>();
            irt.anchorMin = new Vector2(0.20f, 0.30f);
            irt.anchorMax = new Vector2(0.80f, 0.95f);
            irt.offsetMin = irt.offsetMax = Vector2.zero;
            // Active tab keeps the icon white-on-yellow; inactive uses
            // a soft white tint so the dark bar reads as muted.
            icon.color = active ? new Color(1f, 1f, 1f, 1f) : new Color(0.92f, 0.95f, 1f, 0.85f);

            var txt = UIFactory.MakeText(plate.transform, name + "_Label", label, 24, TextAnchor.MiddleCenter);
            var trt = txt.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0f, 0.02f);
            trt.anchorMax = new Vector2(1f, 0.30f);
            trt.offsetMin = trt.offsetMax = Vector2.zero;
            txt.color = active ? new Color(1f, 0.95f, 0.6f, 1f) : new Color(0.9f, 0.95f, 1f, 0.85f);
        }

        // Big arena thumbnail card sandwiched between the player
        // banner and the БОЙ button. The user explicitly asked for a
        // rounded-corner arena image ("картинки арен с закругленными
        // краями") so we wrap the thumbnail in the gold sliced frame —
        // its 9-slice corners are already rounded so the framed art
        // reads as a card.
        //
        // Layout: outer gold frame (taps into Road of Glory), inner
        // arena art inset by ~6%, and a short translucent strip at the
        // bottom that holds the arena's display name.
        void BuildArenaBanner()
        {
            var theme = ArenaTheme.Current(_profile.trophies);

            // Outer rounded gold frame — also the tap target.
            var frame = UIFactory.MakePanel(_canvas.transform, "ArenaFrame", Color.white);
            var frt = frame.GetComponent<RectTransform>();
            frt.anchorMin = new Vector2(0.06f, 0.40f);
            frt.anchorMax = new Vector2(0.94f, 0.85f);
            frt.offsetMin = frt.offsetMax = Vector2.zero;
            var goldSp = UIFactory.LoadSprite("UI/btn_gold");
            if (goldSp != null) { frame.sprite = goldSp; frame.type = Image.Type.Sliced; frame.color = Color.white; }
            var btn = frame.gameObject.AddComponent<Button>();
            btn.targetGraphic = frame;
            btn.onClick.AddListener(() =>
            {
                AudioManager.PlaySfx("click");
                RoadToGloryPopup.Open(_canvas.transform, _profile);
            });

            // Inner arena thumbnail — inset so the gold border is visible.
            var arena = UIFactory.MakePanel(frame.transform, "ArenaThumb", Color.white);
            var art = arena.GetComponent<RectTransform>();
            art.anchorMin = new Vector2(0.04f, 0.12f);
            art.anchorMax = new Vector2(0.96f, 0.96f);
            art.offsetMin = art.offsetMax = Vector2.zero;
            arena.raycastTarget = false;
            Texture2D thumbTex = !string.IsNullOrEmpty(theme.ThumbnailKey)
                ? Resources.Load<Texture2D>("ArenaThumbs/" + theme.ThumbnailKey)
                : null;
            if (thumbTex != null)
            {
                arena.sprite = Sprite.Create(thumbTex,
                    new Rect(0, 0, thumbTex.width, thumbTex.height),
                    new Vector2(0.5f, 0.5f), 100f);
                arena.preserveAspect = true;
            }
            else
            {
                arena.color = Color.Lerp(theme.PlayerSideTint, theme.EnemySideTint, 0.5f);
            }

            // Bottom-strip arena name (sits inside the gold frame, just
            // below the inner art).
            var name = UIFactory.MakeText(frame.transform, "ArenaName", theme.DisplayName, 36, TextAnchor.MiddleCenter);
            var nrt = name.GetComponent<RectTransform>();
            nrt.anchorMin = new Vector2(0f, 0f);
            nrt.anchorMax = new Vector2(1f, 0.12f);
            nrt.offsetMin = nrt.offsetMax = Vector2.zero;
            name.color = new Color(0.25f, 0.15f, 0.05f);
        }

        void BuildSettingsButton()
        {
            var btn = UIFactory.MakeButton(_canvas.transform, "GearBtn", "", () =>
            {
                AudioManager.PlaySfx("click");
                SettingsPopup.Open(_canvas.transform, RefreshBanner);
            });
            var rt = btn.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.88f, 0.91f);
            rt.anchorMax = new Vector2(0.985f, 0.985f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            // Replace the empty button label with a gear icon — Unity's
            // built-in Arial doesn't include the ⚙ glyph.
            var icon = UIFactory.MakeIcon(btn.transform, "Icons/gear", new Vector2(64, 64));
            if (icon != null)
            {
                var irt = icon.GetComponent<RectTransform>();
                irt.anchorMin = new Vector2(0.15f, 0.15f);
                irt.anchorMax = new Vector2(0.85f, 0.85f);
                irt.offsetMin = irt.offsetMax = Vector2.zero;
            }
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
                botBannerColor = bot.bannerColor,
                botIconKey = bot.iconKey,
                botBadgeKeys = bot.badgeKeys,
                trophyDelta = 30
            };
            SceneManager.LoadScene("Battle");
        }
    }
}
