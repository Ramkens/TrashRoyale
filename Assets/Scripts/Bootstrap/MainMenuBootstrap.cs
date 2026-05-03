using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TrashRoyale.Audio;
using TrashRoyale.Persistence;
using TrashRoyale.UI;
using TrashRoyale.Core;
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
            // Big PvE button
            var btnPvE = UIFactory.MakeButton(_canvas.transform, "PvE", "БОЙ ЗА КУБКИ", () =>
            {
                AudioManager.PlaySfx("card_play");
                StartPvE();
            });
            var prt = btnPvE.GetComponent<RectTransform>();
            prt.anchorMin = new Vector2(0.12f, 0.42f);
            prt.anchorMax = new Vector2(0.88f, 0.62f);
            prt.offsetMin = prt.offsetMax = Vector2.zero;

            // Deck preview
            var deckLabel = UIFactory.MakeText(_canvas.transform, "DeckLabel", "ТЕКУЩАЯ КОЛОДА", 28, TextAnchor.MiddleCenter);
            var dlrt = deckLabel.GetComponent<RectTransform>();
            dlrt.anchorMin = new Vector2(0, 0.34f);
            dlrt.anchorMax = new Vector2(1, 0.39f);
            dlrt.offsetMin = dlrt.offsetMax = Vector2.zero;
            deckLabel.color = Color.white;

            var preview = UIFactory.MakePanel(_canvas.transform, "DeckPreview", new Color(0, 0, 0, 0.35f));
            var pvr = preview.GetComponent<RectTransform>();
            pvr.anchorMin = new Vector2(0.04f, 0.16f);
            pvr.anchorMax = new Vector2(0.96f, 0.34f);
            pvr.offsetMin = pvr.offsetMax = Vector2.zero;
            preview.raycastTarget = false;
            _deckPreview = preview.transform;
            RebuildDeckPreview();
        }

        void RebuildDeckPreview()
        {
            if (_deckPreview == null) return;
            for (int i = _deckPreview.childCount - 1; i >= 0; i--) Destroy(_deckPreview.GetChild(i).gameObject);
            for (int i = 0; i < 8; i++)
            {
                int idx = i;
                string id = i < _profile.deck.Count ? _profile.deck[i] : null;
                var card = id != null ? CardDatabase.Get(id) : null;

                var cell = new GameObject("Cell_" + i);
                cell.transform.SetParent(_deckPreview, false);
                var cr = cell.AddComponent<RectTransform>();
                float pad = 0.012f;
                float cellW = (1f - pad * 9f) / 8f;
                cr.anchorMin = new Vector2(pad + idx * (cellW + pad), 0.05f);
                cr.anchorMax = new Vector2(pad + idx * (cellW + pad) + cellW, 0.95f);
                cr.offsetMin = cr.offsetMax = Vector2.zero;
                var cellImg = cell.AddComponent<Image>();
                cellImg.color = new Color(0.05f, 0.08f, 0.18f, 0.85f);

                if (card != null)
                {
                    var art = UIFactory.MakeCardArt(cell.transform, card.id);
                    var artRt = art.GetComponent<RectTransform>();
                    artRt.anchorMin = new Vector2(0.05f, 0.18f);
                    artRt.anchorMax = new Vector2(0.95f, 0.95f);
                    artRt.offsetMin = artRt.offsetMax = Vector2.zero;

                    var costTxt = UIFactory.MakeText(cell.transform, "Cost", card.elixirCost.ToString(), 26, TextAnchor.MiddleCenter);
                    var crrt = costTxt.GetComponent<RectTransform>();
                    crrt.anchorMin = new Vector2(0, 0);
                    crrt.anchorMax = new Vector2(1, 0.18f);
                    crrt.offsetMin = crrt.offsetMax = Vector2.zero;
                    costTxt.color = new Color(1f, 0.55f, 0.95f, 1f);
                }
            }
        }

        void BuildBottomNav()
        {
            var nav = UIFactory.MakePanel(_canvas.transform, "BottomNav", new Color(0.04f, 0.06f, 0.14f, 0.92f));
            var nrt = nav.GetComponent<RectTransform>();
            nrt.anchorMin = new Vector2(0, 0);
            nrt.anchorMax = new Vector2(1, 0.13f);
            nrt.offsetMin = nrt.offsetMax = Vector2.zero;

            // Three-tab layout: КОЛОДА | ТРЕНИРОВКА | ДРУЗЬЯ
            var deckBtn = UIFactory.MakeButton(nav.transform, "DeckTab", "КОЛОДА", () =>
            {
                AudioManager.PlaySfx("click");
                DeckEditor.Open(_canvas.transform, _profile, RebuildDeckPreview);
            });
            var drt = deckBtn.GetComponent<RectTransform>();
            drt.anchorMin = new Vector2(0.02f, 0.15f);
            drt.anchorMax = new Vector2(0.33f, 0.85f);
            drt.offsetMin = drt.offsetMax = Vector2.zero;
            var deckLabel = deckBtn.GetComponentInChildren<Text>();
            if (deckLabel != null) deckLabel.fontSize = 36;

            var trainBtn = UIFactory.MakeButton(nav.transform, "TrainingTab", "ТРЕНИРОВКА", () =>
            {
                AudioManager.PlaySfx("click");
                TrainingPopup.Open(_canvas.transform);
            });
            var trrt = trainBtn.GetComponent<RectTransform>();
            trrt.anchorMin = new Vector2(0.34f, 0.15f);
            trrt.anchorMax = new Vector2(0.66f, 0.85f);
            trrt.offsetMin = trrt.offsetMax = Vector2.zero;
            var trainLabel = trainBtn.GetComponentInChildren<Text>();
            if (trainLabel != null) trainLabel.fontSize = 32;

            // "ДРУЗЬЯ" tab removed: online sync is unstable, the
            // user asked us to either nail it or hide it, and we're
            // shipping a single-player-first refresh. Friendly battle
            // can come back later behind a settings toggle. The two
            // remaining tabs grow to fill the row evenly.
            drt.anchorMax = new Vector2(0.49f, 0.85f);
            trrt.anchorMin = new Vector2(0.51f, 0.15f);
            trrt.anchorMax = new Vector2(0.98f, 0.85f);

            BuildSettingsButton();
        }

        // Big arena thumbnail panel sandwiched between the player
        // banner and the BATTLE button. It mirrors how Clash Royale
        // shows the current arena art and acts as a tap-target into
        // Road of Glory.
        void BuildArenaBanner()
        {
            var theme = ArenaTheme.Current(_profile.trophies);
            var arena = UIFactory.MakePanel(_canvas.transform, "ArenaBanner", Color.white);
            var art = arena.GetComponent<RectTransform>();
            art.anchorMin = new Vector2(0.10f, 0.63f);
            art.anchorMax = new Vector2(0.90f, 0.86f);
            art.offsetMin = art.offsetMax = Vector2.zero;
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
            // Translucent dark scrim along the bottom for the title.
            var scrim = UIFactory.MakePanel(arena.transform, "Scrim", new Color(0, 0, 0, 0.55f));
            var sr = scrim.GetComponent<RectTransform>();
            sr.anchorMin = new Vector2(0f, 0f);
            sr.anchorMax = new Vector2(1f, 0.30f);
            sr.offsetMin = sr.offsetMax = Vector2.zero;
            var name = UIFactory.MakeText(arena.transform, "ArenaName", theme.DisplayName, 38, TextAnchor.MiddleCenter);
            var nrt = name.GetComponent<RectTransform>();
            nrt.anchorMin = new Vector2(0f, 0f);
            nrt.anchorMax = new Vector2(1f, 0.30f);
            nrt.offsetMin = nrt.offsetMax = Vector2.zero;
            name.color = new Color(1f, 0.93f, 0.45f);
            // Tap anywhere on the banner → Road of Glory popup.
            var btn = arena.gameObject.AddComponent<Button>();
            btn.targetGraphic = arena;
            btn.onClick.AddListener(() =>
            {
                AudioManager.PlaySfx("click");
                RoadToGloryPopup.Open(_canvas.transform, _profile);
            });
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
