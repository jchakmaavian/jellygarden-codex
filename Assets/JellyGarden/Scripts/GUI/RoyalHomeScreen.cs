using System;
using JellyGarden.Scripts;
using JellyGarden.Scripts.GUI;
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
        public bool MotionEnabled => PlayerPrefs.GetInt("RoyalThrone.HomeMotion", 1) != 0;
        Rect lastSafe;
        Vector2 lastSize;
        bool lastMap, waitingForLevel, runtimeInitialized;
        float nextRefresh;
        int page;
        string openPanel;
        GameObject returnFocus;
        Canvas legacyCanvas;
        // Existing game-scene canvas settings, restored whenever the board owns presentation.
        const RenderMode LegacyRenderMode = RenderMode.ScreenSpaceCamera;
        const int LegacySortingOrder = 2;

        void InitializeRuntime()
        {
            if (runtimeInitialized) return;
            runtimeInitialized = true;
            legacyCanvas = globalMenus.GetComponent<Canvas>();
            levelButton.onClick.AddListener(OpenNextLevel);
            areaButton.onClick.AddListener(() => OpenPanel("area", areaButton.gameObject));
            string[] resources = { "profile", "coins", "lives", "stars", "settings" };
            string[] tabs = { "story", "leagues", "home", "team", "collection" };
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

        void LateUpdate()
        {
            if (lastSafe != Screen.safeArea || lastSize != ((RectTransform)transform).rect.size) RefreshLayout();
            if (!Application.isPlaying) return;
            // ExecuteAlways Awake can run during editor scene authoring, before Play Mode.
            // Initialize on the first actual game frame so both direct scene play and title entry work.
            InitializeRuntime();
            bool map = LevelManager.THIS && LevelManager.THIS.gameStatus == GameState.Map;
            if (map != lastMap)
            {
                lastMap = map;
                if (map) { waitingForLevel = false; RefreshProgress(); }
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
            controls.interactable = controls.blocksRaycasts = map && !legacyPopup && !dialog.activeSelf;
            if (!legacyPopup) waitingForLevel = false;
            if (map && Time.unscaledTime >= nextRefresh)
            {
                nextRefresh = Time.unscaledTime + .25f;
                RefreshResources();
            }
            if (map && dialog.activeSelf && InputManager.Instance.Gameplay.Cancel.WasPressedThisFrame()) ClosePanel();
            float time = Time.unscaledTime;
            if (waterGlow) waterGlow.color = new Color(.8f, .95f, 1, map && MotionEnabled ? .08f + .1f * (1 + Mathf.Sin(time * 1.8f)) : 0);
            for (int i = 0; i < motes.Length; i++)
            {
                motes[i].color = new Color(1, 1, 1, map && MotionEnabled ? Mathf.Max(0, Mathf.Sin(time * .9f + i * 2)) * .65f : 0);
                motes[i].rectTransform.anchoredPosition = new Vector2(0, MotionEnabled ? Mathf.Repeat(time * 9 + i * 25, 70) : 0);
            }
        }

        public void RefreshProgress()
        {
            Progress = RoyalHomeProgress.Read(levelNumbers, ReadStars);
            if (levelLabel) levelLabel.text = "Level " + Progress.nextLevel;
            if (areaLabel) areaLabel.text = "Area " + Progress.area;
            if (areaProgress) areaProgress.text = Progress.areaCompleted + "/" + Progress.areaTotal;
            if (areaFill) areaFill.anchorMax = new Vector2(Progress.areaTotal > 0 ? (float)Progress.areaCompleted / Progress.areaTotal : 0, 1);
            RefreshResources();
        }

        public static int ReadStars(int level) => PlayerPrefs.GetInt(string.Format("Level.{0:000}.StarsCount", level), 0);
        void RefreshResources()
        {
            if (!InitScript.Instance || !coinLabel) return;
            coinLabel.text = InitScript.Gems.ToString("N0");
            lifeLabel.text = InitScript.lifes.ToString();
            int seconds = Mathf.Max(0, Mathf.CeilToInt(InitScript.RestLifeTimer));
            lifeStatus.text = InitScript.lifes >= InitScript.Instance.CapOfLife ? "Full" : (seconds / 60) + ":" + (seconds % 60).ToString("00");
            starLabel.text = Progress.stars.ToString();
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
                OpenRequestCount++;
                dialog.SetActive(false);
                openPanel = null;
                controls.interactable = controls.blocksRaycasts = false;
                Click();
            }
        }

        public void OpenPanel(string panel, GameObject focus = null)
        {
            if (!IsHomeVisible || waitingForLevel) return;
            if (panel == "home") { ClosePanel(); return; }
            RefreshProgress();
            openPanel = panel;
            returnFocus = focus ? focus : levelButton.gameObject;
            levelGrid.SetActive(false); settingsGroup.SetActive(false);
            previousPage.gameObject.SetActive(false); nextPage.gameObject.SetActive(false); pageLabel.gameObject.SetActive(false);
            dialogCopy.gameObject.SetActive(true); dialogDetail.gameObject.SetActive(true);
            dialogButtonLabel.text = "Back to home";
            string title = "", copy = "", detail = ""; int icon = 0;
            switch (panel)
            {
                case "profile": title = "Your Majesty"; copy = "Welcome to your kingdom"; detail = Progress.completed + " levels completed\n" + Progress.stars + " stars earned"; icon = 0; break;
                case "coins": title = InitScript.Gems.ToString("N0") + " coins"; copy = "A little help for your next challenge"; detail = "Use your balance for boosts and extra moves during a level."; icon = 1; break;
                case "lives": title = "Royal lives"; copy = "One heart, one new adventure"; icon = 2; break;
                case "stars": title = Progress.stars + " stars"; copy = "Your royal achievements"; detail = "Earn up to three stars in each level. Replay a completed level from Story to improve your score."; icon = 3; break;
                case "settings": title = "Settings"; copy = "Make yourself at home"; icon = 4; settingsGroup.SetActive(true); dialogDetail.gameObject.SetActive(false); RefreshSettings(); break;
                case "area": title = "Area " + Progress.area; copy = Progress.area == 1 ? "The Royal Courtyard" : "Your royal journey"; detail = Progress.areaCompleted + " of " + Progress.areaTotal + " levels completed\n\nCastle restoration and chest rewards are coming soon."; icon = 5; break;
                case "story": title = "Royal Journey"; copy = "Choose your next chapter"; icon = 6; page = Array.IndexOf(levelNumbers, Progress.nextLevel) / levelChoices.Length; levelGrid.SetActive(true); dialogDetail.gameObject.SetActive(false); PopulateLevels(); break;
                case "leagues": title = "Royal Leagues"; copy = "Rise through the royal ranks"; detail = "Competitions are coming soon.\nKeep sharpening your match skills!"; icon = 7; break;
                case "team": title = "Your Royal Team"; copy = "A kingdom is better together"; detail = "Teams and helping friends are coming soon."; icon = 9; break;
                case "collection": title = "Royal Collection"; copy = "Treasures of the kingdom"; detail = "Collectible cards and sets are coming soon."; icon = 10; break;
            }
            dialogTitle.text = title; dialogCopy.text = copy; dialogDetail.text = detail; dialogIcon.sprite = icons[icon];
            dialog.SetActive(true); controls.interactable = controls.blocksRaycasts = false;
            RefreshResources(); RefreshLayout(); Click();
            if (EventSystem.current) EventSystem.current.SetSelectedGameObject(closeButton.gameObject);
        }

        public void ClosePanel()
        {
            bool wasOpen = dialog.activeSelf;
            dialog.SetActive(false); openPanel = null;
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
            string key = index == 0 ? "Music" : index == 1 ? "Sound" : "RoyalThrone.HomeMotion";
            int value = PlayerPrefs.GetInt(key, 1) == 0 ? 1 : 0;
            PlayerPrefs.SetInt(key, value); PlayerPrefs.Save();
            if (index == 0 && MusicBase.Instance) MusicBase.Instance.SetVolume(value);
            if (index == 1 && SoundBase.Instance) SoundBase.Instance.SetVolume(value);
            RefreshSettings(); Click();
        }
        void RefreshSettings()
        {
            string[] labels = { "Music", "Sound", "Motion" }; string[] keys = { "Music", "Sound", "RoyalThrone.HomeMotion" };
            for (int i = 0; i < settingButtons.Length; i++)
                settingButtons[i].GetComponentInChildren<Text>().text = labels[i] + ": " + (PlayerPrefs.GetInt(keys[i], 1) != 0 ? "On" : "Off");
        }
        static void Click() { if (SoundBase.Instance) SoundBase.Instance.PlaySound(SoundBase.Instance.click); }

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
            float logoSize = width * (shortPhone ? .30f : .36f);
            Place(logo, logoSize, logoSize, .5f, 1, 0, -height * .117f - logoSize * .5f);
            float actionWidth = width * .94f, leftWidth = actionWidth * .504f, rightWidth = actionWidth * .466f;
            float actionHeight = Mathf.Max(100, leftWidth / 2.35f);
            float navHeight = Mathf.Max(width * .225f, height * (shortPhone ? .125f : .108f));
            Place(actions, actionWidth, actionHeight, .5f, 0, 0, navHeight + height * .018f + actionHeight * .5f);
            Place(levelRect, leftWidth, actionHeight, 0, .5f, leftWidth * .5f, 0);
            Place(areaRect, rightWidth, actionHeight, 1, .5f, -rightWidth * .5f, 0);
            Place(navigation, width, navHeight, .5f, 0, 0, navHeight * .5f);
            navigationBackground.sizeDelta = new Vector2(0, navHeight + root.rect.height * min.y);
            bool tallDialog = openPanel == "story" || openPanel == "settings";
            float dialogWidth = Mathf.Min(width * .88f, 670), dialogHeight = Mathf.Min(height * .84f, dialogWidth * (tallDialog ? 1.48f : 1.08f));
            Place(dialogRect, dialogWidth, dialogHeight, .5f, .5f, 0, 0);
            if (dialogIcon)
            {
                SetVertical(dialogIcon.rectTransform, tallDialog ? .79f : .72f, tallDialog ? .97f : .96f);
                SetVertical(dialogTitle.rectTransform, tallDialog ? .68f : .58f, tallDialog ? .80f : .74f);
                SetVertical(dialogCopy.rectTransform, tallDialog ? .62f : .48f, tallDialog ? .68f : .58f);
                SetVertical(dialogButton.GetComponent<RectTransform>(), tallDialog ? .035f : .07f, tallDialog ? .14f : .21f);
            }
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
