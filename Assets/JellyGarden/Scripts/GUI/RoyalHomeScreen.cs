using PlayerPrefs = RoyalThrone.Testing.RoyalSave;
using System;
using JellyGarden.Scripts;
using JellyGarden.Scripts.GUI;
using JellyGarden.Scripts.Juice;
using JellyGarden.Scripts.MapScripts;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RoyalThrone.Presentation
{
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class RoyalHomeScreen : MonoBehaviour
    {
        public RectTransform safeArea, layout, hud, logo, actions, navigation, navigationBackground;
        public RectTransform levelRect, areaRect, dialogRect, gridRect;
        public CanvasGroup presentation, controls;
        public Button levelButton, areaButton, closeButton, dialogButton, previousPage, nextPage;
        public Button[] resourceButtons, tabButtons, levelChoices, settingButtons;
        public Text coinLabel, lifeLabel, lifeStatus, starLabel, levelLabel, areaLabel, areaProgress;
        public RectTransform areaFill;
        public GameObject dialog, levelGrid, settingsGroup;
        public Image dialogIcon;
        public RoyalWaterGlow waterGlow;
        public Text dialogTitle, dialogCopy, dialogDetail, dialogButtonLabel, pageLabel;
        public Sprite[] icons;
        public int[] levelNumbers;
        public CanvasGroup legacyMapUI;
        public Transform globalMenus;
        public MapCamera mapCamera;
        public RoyalPanelGraphic[] motes;
        public bool IsHomeVisible { get; private set; }
        public int OpenRequestCount { get; private set; }
        public RoyalHomeProgress Progress { get; private set; }
        public int SelectedTab { get; private set; } = 2;
        public bool MotionEnabled => PlayerPrefs.GetInt("RoyalThrone.HomeMotion", 1) != 0;
        Rect lastSafe;
        Vector2 lastSize;
        bool lastMap, waitingForLevel, runtimeInitialized;
        bool runtimeQueued;   // InitializeRuntime handed to the startup loading screen, which runs it in a frame of its own
        float nextRefresh;
        int page;
        string openPanel;
        GameObject returnFocus;
        Canvas legacyCanvas;
        CanvasGroup navigationInput;
        Image[] tabIcons;
        Text[] tabLabels;
        RoyalPanelGraphic[] tabSurfaces;
        float[] tabEmphasis;
        bool presentationPolished;
        Image dogPortrait, stewardPortrait;
        RoyalPanelGraphic throneSeal;
        RoyalAreaScene areaScene;
        RoyalAreaView areaView;
        public RoyalAreaView AreaView => areaView;
        public string OpenPanelName => openPanel;
        /// <summary>A restored area is shown full screen: the Home controls and tabs are put away until its Back.</summary>
        public bool SceneViewOpen { get; set; }
        // Existing game-scene canvas settings, restored whenever the board owns presentation.
        const RenderMode LegacyRenderMode = RenderMode.ScreenSpaceCamera;
        const int LegacySortingOrder = 2;

        void InitializeRuntime()
        {
            if (runtimeInitialized) return;
            runtimeInitialized = true;
            ApplyPresentationPolish();
            RoyalThrone.ReferenceGameplay.ReferenceHomeLayout.Attach(this);
            // King Vanguard (APK Spine) takes the profile portrait. Mounted after the reference layout has placed and re-enabled
            // the portrait image, which stays assigned underneath as the recoverable previous king.
            var kingPortrait = resourceButtons != null && resourceButtons.Length > 0 && resourceButtons[0] ? resourceButtons[0].transform.Find("King portrait") : null;
            if (kingPortrait) RoyalThrone.Characters.KingVanguardSlot.Mount(kingPortrait.GetComponent<Image>(), RoyalThrone.Characters.KingVanguardCharacter.Framing.Avatar);
            legacyCanvas = globalMenus.GetComponent<Canvas>();
            levelButton.onClick.AddListener(OpenNextLevel);
            areaButton.onClick.AddListener(() => OpenPanel("area", areaButton.gameObject));
            string[] resources = { "profile", "coins", "lives", "stars", "settings" };
            // The first tab lists the areas, as the reference's does (the user's decision, 2026-09-28).
            string[] tabs = { RoyalAreaView.List, "leagues", "home", "team", "collection" };
            for (int i = 0; i < resources.Length; i++)
            { int index = i; resourceButtons[i].onClick.AddListener(() => OpenPanel(resources[index], resourceButtons[index].gameObject)); }
            for (int i = 0; i < tabs.Length; i++)
            { int index = i; tabButtons[i].onClick.AddListener(() => OpenPanel(tabs[index], tabButtons[index].gameObject)); }
            closeButton.onClick.AddListener(ClosePanel);
            dialogButton.onClick.AddListener(ClosePanel);
            previousPage.onClick.AddListener(() => { page--; PopulateLevels(); Click(); });
            nextPage.onClick.AddListener(() => { page++; PopulateLevels(); Click(); });
            for (int i = 0; i < levelChoices.Length; i++)
            { int index = i; levelChoices[i].onClick.AddListener(() => OpenLevel(levelNumbers[page * levelChoices.Length + index])); }
            for (int i = 0; i < settingButtons.Length; i++)
            { int index = i; settingButtons[i].onClick.AddListener(() => ToggleSetting(index)); }
            dialog.SetActive(false);
            presentation.alpha = 0;
            // Areas: the Home background is the current area, restored with stars through the Area card's dialogs.
            areaScene = RoyalAreaScene.Attach(this);
            if (areaScene) { areaScene.ShowCurrent(); areaView = RoyalAreaView.Attach(this, areaScene); }
        }

        void OnEnable()
        {
            RefreshLayout();
        }
        void OnDisable()
        {
            if (!legacyCanvas) return;
            legacyCanvas.renderMode = LegacyRenderMode;
            legacyCanvas.sortingOrder = LegacySortingOrder;
        }

        // Back closes an open dialog here, before LevelManager (execution order 1000) handles the release: on Android 13+
        // Unity sends the press and the release in the same frame, and its quit check must already see this Back consumed.
        void Update()
        {
            if (!Application.isPlaying || !dialog || !dialog.activeSelf || !LevelManager.THIS || LevelManager.THIS.gameStatus != GameState.Map || InputManager.Instance == null) return;
            if (InputManager.Instance.Gameplay.Cancel.WasPressedThisFrame()) { ClosePanel(); RoyalBackButton.Consume(); }
        }

        void LateUpdate()
        {
            if (lastSafe != Screen.safeArea || lastSize != ((RectTransform)transform).rect.size) RefreshLayout();
            if (!Application.isPlaying) return;
            // ExecuteAlways Awake can run during editor scene authoring, before Play Mode.
            // Initialize on the first actual game frame so both direct scene play and title entry work. Under the startup loading
            // screen it runs in a frame of its own (RoyalTitleScreen.QueueStartupWork) and Home waits for it; otherwise here.
            if (!runtimeInitialized && (runtimeQueued || (runtimeQueued = RoyalTitleScreen.QueueStartupWork(this, InitializeRuntime)))) return;
            InitializeRuntime();
            bool map = LevelManager.THIS && LevelManager.THIS.gameStatus == GameState.Map;
            if (map != lastMap)
            {
                lastMap = map;
                if (map) { waitingForLevel = false; SelectedTab = 2; RefreshProgress(); }
                else { dialog.SetActive(false); openPanel = null; }
            }
            IsHomeVisible = map;
            // Overlay home art would otherwise cover the original camera-space pre-level panel.
            // Return its canvas to the original mode on the board for the existing UI/VFX layers.
            if (legacyCanvas)
            {
                RenderMode mode = map ? RenderMode.ScreenSpaceOverlay : LegacyRenderMode;
                int order = map ? 30 : LegacySortingOrder;
                if (legacyCanvas.renderMode != mode) legacyCanvas.renderMode = mode;
                if (legacyCanvas.sortingOrder != order) legacyCanvas.sortingOrder = order;
            }
            presentation.alpha = map ? 1 : 0;
            presentation.blocksRaycasts = presentation.interactable = map;
            // Hide only the old map graphics. Its active life counter continues to own regeneration.
            if (legacyMapUI)
            { legacyMapUI.alpha = 0; legacyMapUI.interactable = legacyMapUI.blocksRaycasts = false; }
            if (map && mapCamera) mapCamera.enabled = false;
            bool legacyPopup = false;
            if (map && globalMenus)
                foreach (Transform child in globalMenus)
                    if (child.gameObject.activeInHierarchy) { legacyPopup = true; break; }
            // The dialog veil catches the castle controls; navigation remains available
            // above it so a tab can be changed directly without a second close tap.
            controls.interactable = controls.blocksRaycasts = map && !legacyPopup && !dialog.activeSelf && !SceneViewOpen;
            if (navigationInput) navigationInput.interactable = navigationInput.blocksRaycasts = map && !legacyPopup && !SceneViewOpen;
            controls.alpha = SceneViewOpen ? 0 : 1;
            if (navigationInput) navigationInput.alpha = SceneViewOpen ? 0 : 1;
            if (!legacyPopup) waitingForLevel = false;
            if (map && Time.unscaledTime >= nextRefresh)
            {
                nextRefresh = Time.unscaledTime + .25f;
                RefreshResources();
            }
            float time = Time.unscaledTime;
            UpdateNavigation(false);
            if (waterGlow) waterGlow.color = new Color(.8f, .95f, 1, map && MotionEnabled ? .08f + .1f * (1 + Mathf.Sin(time * 1.8f)) : 0);
            for (int i = 0; i < motes.Length; i++)
            {
                motes[i].color = new Color(1, 1, 1, map && MotionEnabled ? Mathf.Max(0, Mathf.Sin(time * .9f + i * 2)) * .65f : 0);
                motes[i].rectTransform.anchoredPosition = new Vector2(0, MotionEnabled ? Mathf.Repeat(time * 9 + i * 25, 70) : 0);
            }
        }

        public void RefreshProgress()
        {
            var progress = RoyalHomeProgress.Read(levelNumbers, ReadStars);
            RoyalAreas.Fill(ref progress);
            Progress = progress;
            if (levelLabel) levelLabel.text = "Level " + Progress.nextLevel;
            var area = RoyalAreas.CurrentArea;
            if (areaLabel) areaLabel.text = RoyalAreas.AllRestored ? "Areas" : "Area " + Progress.area;
            if (areaProgress) areaProgress.text = RoyalAreas.AllRestored ? "Restored" : area == null || !area.Ready ? "Soon" : Progress.areaCompleted + "/" + Progress.areaTotal;
            if (areaFill) areaFill.anchorMax = new Vector2(Progress.areaTotal > 0 ? (float)Progress.areaCompleted / Progress.areaTotal : 0, 1);
            if (areaView) areaView.DecorateCard();
            RefreshResources();
        }

        public static int ReadStars(int level) => PlayerPrefs.GetInt(string.Format("Level.{0:000}.StarsCount", level), 0);
        void RefreshResources()
        {
            if (!InitScript.Instance || !coinLabel) return;
            // Keeps the home HUD correct even when the legacy map counter is not running.
            InitScript.Instance.UpdateLives();
            coinLabel.text = InitScript.Gems.ToString("N0");
            lifeLabel.text = InitScript.lifes.ToString();
            int seconds = Mathf.Max(0, Mathf.CeilToInt(InitScript.RestLifeTimer));
            lifeStatus.text = InitScript.lifes >= InitScript.Instance.CapOfLife ? "Full" : (seconds / 60) + ":" + (seconds % 60).ToString("00");
            // Stars left to spend on area tasks (earned in levels, less what tasks cost).
            starLabel.text = Progress.available.ToString();
            if (openPanel == "lives") dialogDetail.text = "Lives: " + InitScript.lifes + "/" + InitScript.Instance.CapOfLife +
                (InitScript.lifes < InitScript.Instance.CapOfLife ? "\nNext life in " + lifeStatus.text : "\nYour next level is ready.");
        }

        public void OpenNextLevel() { RefreshProgress(); OpenLevel(Progress.nextLevel); }
        public void OpenLevel(int number)
        {
            if (!Application.isPlaying || !LevelManager.THIS || LevelManager.THIS.gameStatus != GameState.Map || waitingForLevel) return;
            if (Array.IndexOf(levelNumbers, number) < 0 || LevelsMap.IsLevelLocked(number)) return;
            // Use the same pre-level/boost/life flow as the map. A UI click must not be sent
            // through OnLevelClicked, which deliberately rejects pointers over UI.
            if (InitScript.Instance.TryOpenLevel(number))
            {
                waitingForLevel = true;
                SelectedTab = 2;
                OpenRequestCount++;
                dialog.SetActive(false);
                openPanel = null;
                controls.interactable = controls.blocksRaycasts = false;
                Click();
            }
        }

        // "1 star", "2 stars": the panels showed "1 coins" / "1 levels completed" to every new player.
        static string Count(int n, string one, string many) => n.ToString("N0") + " " + (n == 1 ? one : many);

        public void OpenPanel(string panel, GameObject focus = null)
        {
            if (!IsHomeVisible || waitingForLevel) return;
            if (panel == "home") { ClosePanel(); return; }
            // The coin counter opens the shop (2026-10-05): coins and stars are bought there. The star counter opens the area's
            // task list instead, since the Home's Area card is hidden (the user's decision, 2026-10-05); stars stay on sale in
            // the shop's Stars tab.
            if (panel == "coins") { RoyalShopPage.Open(RoyalShopPage.Tab.Coins); return; }
            if (panel == "stars") panel = "area";
            if (areaView) panel = areaView.Route(panel);
            string[] tabs = { RoyalAreaView.List, "leagues", "home", "team", "collection" };
            int selected = Array.IndexOf(tabs, panel);
            SelectedTab = selected < 0 ? 2 : selected;
            RefreshProgress();
            openPanel = panel;
            returnFocus = focus ? focus : levelButton.gameObject;
            levelGrid.SetActive(false); settingsGroup.SetActive(false);
            if (areaView) areaView.Hide();
            previousPage.gameObject.SetActive(false); nextPage.gameObject.SetActive(false); pageLabel.gameObject.SetActive(false);
            dialogCopy.gameObject.SetActive(true); dialogDetail.gameObject.SetActive(true);
            dialogButtonLabel.text = "Back to home";
            string title = "", copy = "", detail = ""; int icon = 0;
            switch (panel)
            {
                case "profile": title = RoyalOfflineMode.KingName; copy = "Welcome to Royal Throne"; detail = Count(Progress.completed, "level", "levels") + " completed\n" + Count(Progress.stars, "star", "stars") + " earned"; icon = 0; break;
                case "coins": title = Count(InitScript.Gems, "coin", "coins"); copy = "A little help for your next challenge"; detail = "Use your balance for boosts and extra moves during a level."; icon = 1; break;
                case "lives": title = "Royal lives"; copy = "One heart, one new adventure"; icon = 2; break;
                case "stars": title = Count(Progress.available, "star", "stars") + " to spend"; copy = "Restore your kingdom"; detail = "Win levels to earn up to three stars each, then spend them on area tasks. " + Count(Progress.stars, "star", "stars") + " earned so far."; icon = 3; break;
                case "settings": title = "Settings"; copy = "Make yourself at home"; icon = 4; settingsGroup.SetActive(true); dialogDetail.gameObject.SetActive(false); RefreshSettings(); break;
                case RoyalAreaView.Tasks: case RoyalAreaView.List: case RoyalAreaView.Chest: case RoyalAreaView.Unlocked: case RoyalAreaView.Short: case RoyalAreaView.Soon:
                    if (areaView) areaView.Fill(panel, ref title, ref copy, ref detail, ref icon); break;
                case "story": title = "Royal Journey"; copy = "Choose your next chapter"; icon = 6; page = Array.IndexOf(levelNumbers, Progress.nextLevel) / levelChoices.Length; levelGrid.SetActive(true); dialogDetail.gameObject.SetActive(false); PopulateLevels(); break;
                case "leagues": title = "Royal Leagues"; copy = "Rise through the royal ranks"; detail = "Competitions are coming soon.\nKeep sharpening your match skills!"; icon = 7; break;
                case "team": title = "Your Royal Team"; copy = "A kingdom is better together"; detail = "Teams and helping friends are coming soon."; icon = 9; break;
                case "collection": title = "Royal Collection"; copy = "Treasures of the kingdom"; detail = "Collectible cards and sets are coming soon."; icon = 10; break;
            }
            dialogTitle.text = title; dialogCopy.text = copy; dialogDetail.text = detail; dialogIcon.sprite = icons[icon];
            ShowRoyalCompanions(panel == "profile" || panel == "story");
            dialog.SetActive(true); controls.interactable = controls.blocksRaycasts = false;
            RefreshResources(); RefreshLayout(); Click();
            if (EventSystem.current) EventSystem.current.SetSelectedGameObject(closeButton.gameObject);
        }

        public void ClosePanel()
        {
            bool wasOpen = dialog.activeSelf;
            dialog.SetActive(false); openPanel = null;
            SelectedTab = 2;
            controls.interactable = controls.blocksRaycasts = IsHomeVisible;
            if (wasOpen) Click();
            if (EventSystem.current) EventSystem.current.SetSelectedGameObject(returnFocus ? returnFocus : levelButton.gameObject);
        }

        void PopulateLevels()
        {
            int pages = Mathf.Max(1, Mathf.CeilToInt((float)levelNumbers.Length / levelChoices.Length));
            page = Mathf.Clamp(page, 0, pages - 1);
            for (int i = 0; i < levelChoices.Length; i++)
            {
                int index = page * levelChoices.Length + i;
                var button = levelChoices[i]; button.gameObject.SetActive(index < levelNumbers.Length);
                if (index >= levelNumbers.Length) continue;
                int number = levelNumbers[index]; bool unlocked = !LevelsMap.IsLevelLocked(number);
                button.interactable = unlocked;
                button.GetComponentInChildren<Text>().text = number + "\n" + (unlocked ? ReadStars(number) + "/3" : "Locked");
            }
            previousPage.gameObject.SetActive(true); nextPage.gameObject.SetActive(true); pageLabel.gameObject.SetActive(true);
            previousPage.interactable = page > 0; nextPage.interactable = page < pages - 1;
            pageLabel.text = (page + 1) + " / " + pages;
        }
        void ToggleSetting(int index)
        {
            if (index == 3)
            {
                RoyalHintSettings.Enabled = !RoyalHintSettings.Enabled;
                RefreshSettings(); Click(); return;
            }
            string key = index == 0 ? "Music" : index == 1 ? "Sound" : "RoyalThrone.HomeMotion";
            int value = PlayerPrefs.GetInt(key, 1) == 0 ? 1 : 0;
            PlayerPrefs.SetInt(key, value); PlayerPrefs.Save();
            if (index == 0 && MusicBase.Instance) MusicBase.Instance.SetVolume(value);
            if (index == 1 && SoundBase.Instance) SoundBase.Instance.SetVolume(value);
            RefreshSettings(); Click();
        }
        void RefreshSettings()
        {
            string[] labels = { "Music", "Sound", "Motion", "Hints" }; string[] keys = { "Music", "Sound", "RoyalThrone.HomeMotion", "RoyalThrone.ShowHints" };
            for (int i = 0; i < settingButtons.Length; i++)
                settingButtons[i].GetComponentInChildren<Text>().text = labels[i] + ": " + (PlayerPrefs.GetInt(keys[i], 1) != 0 ? "On" : "Off");
        }
        static void Click() { if (SoundBase.Instance) SoundBase.Instance.PlaySound(SoundBase.Instance.click); }

        // Runtime migration keeps existing scene references and button callbacks intact.
        // The authoring builder also invokes this after all bindings have been assigned.
        public void ApplyPresentationPolish()
        {
            if (presentationPolished || !navigation || tabButtons == null || tabButtons.Length != 5) return;
            presentationPolished = true;
            // Keep keyboard navigation as well as pointer input on the visible tabs;
            // the castle buttons remain disabled while a dialog is open.
            navigation.SetParent(layout, false);
            navigationInput = navigation.GetComponent<CanvasGroup>();
            if (!navigationInput) navigationInput = navigation.gameObject.AddComponent<CanvasGroup>();
            var navCanvas = navigation.GetComponent<Canvas>();
            if (!navCanvas) navCanvas = navigation.gameObject.AddComponent<Canvas>();
            navCanvas.overrideSorting = true; navCanvas.sortingOrder = 20;
            if (!navigation.GetComponent<GraphicRaycaster>()) navigation.gameObject.AddComponent<GraphicRaycaster>();
            var navFace = CreateSurface("Navigation clean blue", navigation,
                new Color(.06f, .29f, .64f), new Color(.025f, .14f, .38f), 0);
            navFace.transform.SetAsFirstSibling();
            var navTrim = CreateSurface("Navigation fine gold edge", navFace.transform,
                new Color(1, .92f, .48f), new Color(.87f, .57f, .12f), 0);
            navTrim.rectTransform.anchorMin = new Vector2(0, 1); navTrim.rectTransform.anchorMax = Vector2.one;
            navTrim.rectTransform.sizeDelta = new Vector2(0, 4);
            tabIcons = new Image[5]; tabLabels = new Text[5];
            tabSurfaces = new RoyalPanelGraphic[5]; tabEmphasis = new float[5];
            for (int i = 0; i < 5; i++)
            {
                var button = tabButtons[i];
                button.GetComponent<Image>().color = Color.clear;
                tabIcons[i] = button.transform.Find(button.name + " icon").GetComponent<Image>();
                tabIcons[i].raycastTarget = true; // The raised icon is part of the same button hit target.
                tabLabels[i] = button.GetComponentInChildren<Text>(true);
                tabLabels[i].fontSize = 27; tabLabels[i].resizeTextMaxSize = 27;
                tabLabels[i].resizeTextMinSize = 18;
                var panel = CreateSurface("Selected tab surface", button.transform,
                    new Color(.16f, .49f, .91f), new Color(.055f, .26f, .64f), 0);
                panel.transform.SetAsFirstSibling(); tabSurfaces[i] = panel;
                var line = CreateSurface("Tab divider", button.transform,
                    new Color(.035f, .17f, .44f, .55f), new Color(.035f, .17f, .44f, .12f), 0);
                line.rectTransform.anchorMin = new Vector2(0, 0); line.rectTransform.anchorMax = new Vector2(0, 1);
                line.rectTransform.sizeDelta = new Vector2(i == 0 ? 0 : 2, 0);
                line.transform.SetSiblingIndex(1);
            }
            // A single strong action replaces the narrow crowned plaque on Home.
            var artwork = levelButton.transform.Find("Artwork");
            if (artwork) artwork.gameObject.SetActive(false);
            var depth = CreateSurface("Play gold frame", levelButton.transform,
                new Color(1, .89f, .42f), new Color(.71f, .36f, .035f), 26);
            depth.transform.SetAsFirstSibling();
            var face = CreateSurface("Play green face", depth.transform,
                new Color(.055f, .65f, .56f), new Color(.025f, .32f, .34f), 22);
            face.rectTransform.offsetMin = new Vector2(5, 8); face.rectTransform.offsetMax = new Vector2(-5, -5);
            var highlight = CreateSurface("Play upper highlight", face.transform,
                new Color(.73f, 1, .9f, .24f), Color.clear, 18);
            highlight.rectTransform.anchorMin = new Vector2(0, .52f);
            levelLabel.rectTransform.anchorMin = new Vector2(.07f, .09f);
            levelLabel.rectTransform.anchorMax = new Vector2(.93f, .91f);
            levelLabel.rectTransform.offsetMin = levelLabel.rectTransform.offsetMax = Vector2.zero;
            levelLabel.fontSize = levelLabel.resizeTextMaxSize = 43;
            if (settingButtons.Length == 3)
            {
                var hint = Instantiate(settingButtons[2], settingsGroup.transform);
                hint.name = "Setting 3 Hints";
                Array.Resize(ref settingButtons, 4); settingButtons[3] = hint;
            }
            for (int i = 0; i < settingButtons.Length; i++)
            {
                var rect = (RectTransform)settingButtons[i].transform;
                rect.anchorMin = new Vector2(0, 1 - (i + 1) * .25f + .015f);
                rect.anchorMax = new Vector2(1, 1 - i * .25f - .015f);
                rect.offsetMin = rect.offsetMax = Vector2.zero;
            }
            // Calm press feedback on every Home control: down to 0.975, settling in about 0.15 s, and no growth on hover or focus
            // (a tapped button keeps the focus, so a focus scale stayed enlarged after every tap).
            foreach (var motion in GetComponentsInChildren<RoyalButtonMotion>(true))
            { motion.pressScale = .975f; motion.hoverScale = 1; motion.response = 20; }
            ApplyCampaignArtwork();
            UpdateNavigation(true);
        }

        public void ApplyCampaignArtwork()
        {
            // The courtyard's palace picture was removed from the project (2026-10-04); the courtyard shows the sky and the
            // current area (ReferenceHomeSky, RoyalAreaScene). The inherited glow belonged to another castle's fountain.
            if (waterGlow) waterGlow.enabled = false;
            var coin = RoyalPieceSkin.CampaignArt("coin");
            if (coin && resourceButtons != null && resourceButtons.Length > 1)
            {
                if (icons != null && icons.Length > 1) icons[1] = coin;
                var picture = resourceButtons[1].transform.Find("Coins icon");
                if (picture) picture.GetComponent<Image>().sprite = coin;
            }
            var crown = RoyalPieceSkin.CampaignArt("crown");
            if (crown && tabIcons != null && tabIcons.Length > 2) tabIcons[2].sprite = crown;
            if (!dialogIcon) return;
            dogPortrait = CompanionPortrait("Royal dog portrait", "dog", -.70f, .15f);
            stewardPortrait = CompanionPortrait("Royal steward portrait", "steward", .85f, 1.70f);
            throneSeal = CreateSurface("Royal sun rosette", dialogRect,
                new Color(1, .87f, .43f), new Color(.79f, .48f, .13f), 0);
            throneSeal.sunRosette = true; throneSeal.color = new Color(1, 1, 1, .27f);
            throneSeal.transform.SetSiblingIndex(dialogIcon.transform.GetSiblingIndex());
            ShowRoyalCompanions(openPanel == "profile" || openPanel == "story");
        }

        Image CompanionPortrait(string name, string art, float left, float right)
        {
            var sprite = RoyalPieceSkin.CampaignArt(art);
            if (!sprite) return null;
            var existing = dialogIcon.transform.Find(name);
            var go = existing ? existing.gameObject : new GameObject(name, typeof(RectTransform));
            go.layer = 5; go.transform.SetParent(dialogIcon.transform, false);
            var image = go.GetComponent<Image>(); if (!image) image = go.AddComponent<Image>();
            image.sprite = sprite; image.preserveAspect = true; image.raycastTarget = false;
            image.rectTransform.anchorMin = new Vector2(left, .025f); image.rectTransform.anchorMax = new Vector2(right, .93f);
            image.rectTransform.offsetMin = image.rectTransform.offsetMax = Vector2.zero;
            return image;
        }

        void ShowRoyalCompanions(bool visible)
        {
            // King Vanguard (APK Spine) stands in the dialog whenever the king is its subject; other panels keep their own icons.
            if (Application.isPlaying && dialogIcon)
            {
                var vanguard = visible ? RoyalThrone.Characters.KingVanguardSlot.Mount(dialogIcon, RoyalThrone.Characters.KingVanguardCharacter.Framing.Bust)
                                       : dialogIcon.GetComponent<RoyalThrone.Characters.KingVanguardSlot>();
                if (vanguard) vanguard.SetShown(visible, !visible);
            }
            if (dogPortrait) dogPortrait.gameObject.SetActive(visible);
            if (stewardPortrait) stewardPortrait.gameObject.SetActive(visible);
            if (throneSeal) throneSeal.gameObject.SetActive(visible);
        }

        void UpdateNavigation(bool immediate)
        {
            if (tabButtons == null || tabIcons == null || tabLabels == null || tabSurfaces == null || tabEmphasis == null || tabIcons.Length != tabButtons.Length || tabLabels.Length != tabButtons.Length || tabSurfaces.Length != tabButtons.Length || tabEmphasis.Length != tabButtons.Length) return;
            float step = immediate || !MotionEnabled ? 1 : 1 - Mathf.Exp(-19 * Time.unscaledDeltaTime);
            for (int i = 0; i < tabButtons.Length; i++)
            {
                bool selected = i == SelectedTab;
                tabEmphasis[i] = Mathf.Lerp(tabEmphasis[i], selected ? 1 : 0, step);
                float t = tabEmphasis[i];
                var rect = (RectTransform)tabButtons[i].transform;
                rect.anchorMin = new Vector2(i / 5f, 0); rect.anchorMax = new Vector2((i + 1) / 5f, 1);
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                var icon = tabIcons[i].rectTransform;
                icon.anchorMin = icon.anchorMax = new Vector2(.5f, .58f + .24f * t);
                icon.sizeDelta = Vector2.one * navigation.rect.width * Mathf.Lerp(.17f, .22f, t);
                icon.anchoredPosition = Vector2.zero;
                // Every tab is named under its icon; the selected one is emphasised by its size and the raised art.
                tabLabels[i].gameObject.SetActive(true);
                tabLabels[i].rectTransform.anchorMin = new Vector2(.01f, .04f);
                tabLabels[i].rectTransform.anchorMax = new Vector2(.99f, .29f);
                tabLabels[i].rectTransform.offsetMin = tabLabels[i].rectTransform.offsetMax = Vector2.zero;
                tabSurfaces[i].color = new Color(1, 1, 1, t);
            }
        }

        static RoyalPanelGraphic CreateSurface(string name, Transform parent, Color top, Color bottom, float radius)
        {
            var existing = parent.Find(name);
            var go = existing ? existing.gameObject : new GameObject(name, typeof(RectTransform)); go.layer = 5;
            var rect = (RectTransform)go.transform; rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var panel = go.GetComponent<RoyalPanelGraphic>(); if (!panel) panel = go.AddComponent<RoyalPanelGraphic>(); panel.raycastTarget = false;
            panel.topColor = top; panel.bottomColor = bottom; panel.cornerRadius = radius;
            return panel;
        }

        public void RefreshLayout()
        {
            if (!safeArea || !layout) return;
            var root = (RectTransform)transform; lastSize = root.rect.size; lastSafe = Screen.safeArea;
            Vector2 min = Vector2.zero, max = Vector2.one;
            if (Application.isPlaying && Screen.width > 0 && lastSafe.width > 0)
            { min = lastSafe.min / new Vector2(Screen.width, Screen.height); max = lastSafe.max / new Vector2(Screen.width, Screen.height); }
            safeArea.anchorMin = min; safeArea.anchorMax = max; safeArea.offsetMin = safeArea.offsetMax = Vector2.zero;
            float height = root.rect.height * (max.y - min.y);
            float width = Mathf.Min(root.rect.width * (max.x - min.x), height * .68f);
            layout.sizeDelta = new Vector2(width, 0);
            bool shortPhone = height / Mathf.Max(width, 1) < 2;
            Place(hud, width * .95f, width * .16f, .5f, 1, 0, -height * .04f - width * .08f);
            float logoSize = width * (shortPhone ? .25f : .29f);
            Place(logo, logoSize, logoSize, .5f, 1, 0, -height * .117f - logoSize * .5f);
            float actionWidth = width * .94f, leftWidth = actionWidth * .59f, rightWidth = actionWidth * .38f;
            float actionHeight = Mathf.Max(96, width * .16f);
            float navHeight = Mathf.Max(width * .21f, height * (shortPhone ? .108f : .093f));
            float raisedIconClearance = width * .122f - navHeight * .18f;
            float actionGap = Mathf.Max(height * .045f, raisedIconClearance);
            Place(actions, actionWidth, actionHeight, .5f, 0, 0, navHeight + actionGap + actionHeight * .5f);
            Place(levelRect, leftWidth, actionHeight, 0, .5f, leftWidth * .5f, 0);
            Place(areaRect, rightWidth, actionHeight, 1, .5f, -rightWidth * .5f, 0);
            Place(navigation, width, navHeight, .5f, 0, 0, navHeight * .5f);
            navigationBackground.sizeDelta = new Vector2(0, navHeight + root.rect.height * min.y);
            UpdateNavigation(!Application.isPlaying);
            bool tallDialog = openPanel == "story" || openPanel == "settings" || RoyalAreaView.Tall(openPanel);
            float dialogWidth = Mathf.Min(width * .88f, 670), dialogHeight = Mathf.Min(height * .84f, dialogWidth * (tallDialog ? 1.48f : 1.08f));
            Place(dialogRect, dialogWidth, dialogHeight, .5f, .5f, 0, 0);
            if (dialogIcon)
            {
                SetVertical(dialogIcon.rectTransform, tallDialog ? .79f : .72f, tallDialog ? .97f : .96f);
                SetVertical(dialogTitle.rectTransform, tallDialog ? .68f : .58f, tallDialog ? .80f : .74f);
                SetVertical(dialogCopy.rectTransform, tallDialog ? .62f : .48f, tallDialog ? .68f : .58f);
                SetVertical(dialogButton.GetComponent<RectTransform>(), tallDialog ? .035f : .07f, tallDialog ? .14f : .21f);
                if (throneSeal)
                {
                    throneSeal.rectTransform.anchorMin = new Vector2(.35f, dialogIcon.rectTransform.anchorMin.y);
                    throneSeal.rectTransform.anchorMax = new Vector2(.65f, dialogIcon.rectTransform.anchorMax.y);
                    throneSeal.rectTransform.offsetMin = throneSeal.rectTransform.offsetMax = Vector2.zero;
                }
            }
            // In Play Mode the imported layout replaces this placement every frame; apply it now so callers read what is drawn.
            var reference = GetComponent<RoyalThrone.ReferenceGameplay.ReferenceHomeLayout>();
            if (reference) reference.Relayout();
        }
        static void SetVertical(RectTransform rect, float bottom, float top)
        { rect.anchorMin = new Vector2(rect.anchorMin.x, bottom); rect.anchorMax = new Vector2(rect.anchorMax.x, top); }
        static void Place(RectTransform rect, float width, float height, float x, float y, float offsetX, float offsetY)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(x, y); rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = new Vector2(width, height); rect.anchoredPosition = new Vector2(offsetX, offsetY);
        }
    }
}
