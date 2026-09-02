// The level start popup: "Level 16", what the level wants, and Play.
//
// Royal Match states a level's goal before the board opens and says nothing
// else about it — no moves count, no star rating, no difficulty. The number of
// moves is the level's problem, not the popup's, and putting it here would give
// the player a number to fear before they have seen the board.
//
// Everything on it comes from StoryLevelHud.ReadLevel: the same model the
// in-level HUD draws, built from Resources/Levels/N.txt plus
// Resources/Targets/LevelN.asset with no board in the scene. The popup and the
// HUD therefore cannot disagree about what a level is asking for.
//
// Metrics and colours are measured off docs/royal-match/10-level-start-popup.jpg
// (828x1472) and scaled to the 1080x1920 reference canvas by 1080/828 = 1.304.

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace JellyGarden.Scripts.Story
{
    public sealed class StoryLevelPopup : MonoBehaviour
    {
        public const string HostName = "StoryLevelPopup";

        #region Metrics, measured off docs/royal-match/10-level-start-popup.jpg

        // The panel band. The popup does not fill the screen: the room and the
        // top bar stay visible above it and the nav bar below it.
        private const float PopupTop = 274f;
        private const float PopupBottom = 238f;

        // The cream Goal card, inset inside the panel's content rect.
        private const float GoalCardTop = 73f;
        private const float GoalCardSide = 109f;
        private const float GoalTitleHeight = 120f;
        private const float GoalRowHeight = 220f;
        private const float GoalRowSide = 44f;
        private const float GoalCardPad = 30f;

        // The Play button.
        private const float PlayWidth = 574f;
        private const float PlayHeight = 209f;
        private const float PlayBottom = 122f;

        /// <summary>Four is what the Target panel holds; the popup matches it.</summary>
        private const int MaxGoals = 4;

        private const float SlotMaxWidth = 200f;
        private const float SlotIcon = 130f;

        #endregion

        #region State

        private static StoryLevelPopup _instance;

        public static StoryLevelPopup Instance
        {
            get { return _instance; }
        }

        public static bool IsOpen
        {
            get { return _instance != null && _instance.gameObject.activeSelf; }
        }

        /// <summary>
        /// What Play does. Null means StoryFlow.LaunchLevel, which is the only
        /// thing it ever is in the game; a test replaces it so the button can be
        /// proven without a board in the scene.
        /// </summary>
        public static Action<int, int> Launcher;

        private int _level;
        private int _areaId;
        private Action _onClose;

        private Canvas _canvas;
        private StoryUi.RoyalPanelParts _panel;
        private RectTransform _goalRow;
        private Button _play;

        private readonly List<Slot> _slots = new List<Slot>();

        private sealed class Slot
        {
            public RectTransform Root;
            public Image Icon;
            public Text Count;
        }

        /// <summary>The level this popup is currently offering.</summary>
        public int Level
        {
            get { return _level; }
        }

        /// <summary>The area the level will be played for.</summary>
        public int AreaId
        {
            get { return _areaId; }
        }

        public Button PlayButton
        {
            get { return _play; }
        }

        public Button CloseButton
        {
            get { return _panel == null ? null : _panel.Close; }
        }

        public string TitleText
        {
            get { return _panel == null || _panel.Title == null ? null : _panel.Title.text; }
        }

        /// <summary>What each drawn goal slot reads, in the level's own target order.</summary>
        public List<string> GoalTexts()
        {
            var texts = new List<string>();
            for (int i = 0; i < _slots.Count; i++)
            {
                if (!_slots[i].Root.gameObject.activeSelf) continue;
                texts.Add(_slots[i].Count.text);
            }
            return texts;
        }

        #endregion

        #region Open and close

        /// <summary>
        /// Opens the popup for a level. <paramref name="onClose"/> is what the
        /// red X goes back to — the screen the popup was opened from. It is not
        /// called when Play is pressed: the level is where the player went.
        /// </summary>
        public static void Open(int levelNumber, int areaId, Action onClose)
        {
            if (levelNumber <= 0)
            {
                Debug.LogWarning("[Story] No level to offer.");
                return;
            }

            if (_instance == null)
            {
                var go = new GameObject(HostName);
                StoryObjects.Persist(go);
                _instance = go.AddComponent<StoryLevelPopup>();
                _instance.Build();
            }

            _instance._level = levelNumber;
            _instance._areaId = areaId;
            _instance._onClose = onClose;
            _instance.gameObject.SetActive(true);
            _instance.Refresh();
        }

        public static void Close()
        {
            if (_instance != null) _instance.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        #endregion

        #region Build

        private void Build()
        {
            _canvas = StoryUi.CreateOverlayCanvas("LevelPopupCanvas", 600);
            _canvas.transform.SetParent(transform, false);

            // The scrim dims what is behind and eats the taps that miss the
            // panel. It does not close: the X is the way out, so a fat-fingered
            // tap beside the Play button cannot lose the player their place.
            StoryUi.CreateFullScreenButton("Scrim", _canvas.transform,
                new Color(0f, 0f, 0f, 0.45f), null);

            RectTransform band = StoryUi.CreateRect("Band", _canvas.transform);
            band.anchorMin = Vector2.zero;
            band.anchorMax = Vector2.one;
            band.offsetMin = new Vector2(0f, PopupBottom);
            band.offsetMax = new Vector2(0f, -PopupTop);

            _panel = StoryUi.RoyalPanel("Panel", band, "Level", OnClose);

            BuildGoalCard(_panel.Content);

            _play = StoryUi.GreenButton("Play", _panel.Content, "Play", 84, OnPlay);
            RectTransform playRect = _play.GetComponent<RectTransform>();
            playRect.anchorMin = new Vector2(0.5f, 0f);
            playRect.anchorMax = new Vector2(0.5f, 0f);
            playRect.pivot = new Vector2(0.5f, 0f);
            playRect.sizeDelta = new Vector2(PlayWidth, PlayHeight);
            playRect.anchoredPosition = new Vector2(0f, PlayBottom);
        }

        /// <summary>
        /// The cream card: a "Goal" heading over a sunken row of icons. The row
        /// is its own rect so the goal slots can be rebuilt per level without
        /// touching the card around them — and so US-005's booster row has an
        /// obvious place to be added underneath.
        /// </summary>
        private void BuildGoalCard(Transform parent)
        {
            float height = GoalTitleHeight + GoalRowHeight + GoalCardPad;

            Image card = StoryUi.CreamCard("GoalCard", parent, 32);
            RectTransform rect = card.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(GoalCardSide, -GoalCardTop - height);
            rect.offsetMax = new Vector2(-GoalCardSide, -GoalCardTop);

            Text heading = StoryUi.CreateText("Heading", rect, "Goal", 64,
                StoryUi.RoyalNavy, TextAnchor.MiddleCenter);
            StoryUi.TopAnchored(heading.rectTransform, 8f, GoalTitleHeight - 16f);
            heading.fontStyle = FontStyle.Bold;

            // The sunken well the icons sit in: the cream card's own darker
            // shade, which is how the reference separates the two.
            Image well = StoryUi.CreateImage("GoalWell", rect,
                StoryUi.RoundedSprite(24), StoryUi.RoyalCreamDeep);
            well.type = Image.Type.Sliced;
            RectTransform wellRect = well.rectTransform;
            wellRect.anchorMin = new Vector2(0f, 1f);
            wellRect.anchorMax = new Vector2(1f, 1f);
            wellRect.pivot = new Vector2(0.5f, 1f);
            wellRect.offsetMin = new Vector2(GoalRowSide, -GoalTitleHeight - GoalRowHeight);
            wellRect.offsetMax = new Vector2(-GoalRowSide, -GoalTitleHeight);

            _goalRow = StoryUi.Stretch(StoryUi.CreateRect("GoalRow", wellRect), 12f);
        }

        private Slot BuildSlot()
        {
            var slot = new Slot();
            slot.Root = StoryUi.CreateRect("Goal" + _slots.Count, _goalRow);
            slot.Root.anchorMin = new Vector2(0.5f, 0.5f);
            slot.Root.anchorMax = new Vector2(0.5f, 0.5f);
            slot.Root.pivot = new Vector2(0.5f, 0.5f);
            slot.Root.sizeDelta = new Vector2(SlotMaxWidth, GoalRowHeight - 24f);

            slot.Icon = StoryUi.CreateImage("Icon", slot.Root, null, Color.white);
            slot.Icon.preserveAspect = true;
            RectTransform icon = slot.Icon.rectTransform;
            icon.anchorMin = new Vector2(0.5f, 0.5f);
            icon.anchorMax = new Vector2(0.5f, 0.5f);
            icon.pivot = new Vector2(0.5f, 0.5f);
            icon.sizeDelta = new Vector2(SlotIcon, SlotIcon);
            icon.anchoredPosition = new Vector2(-12f, 8f);

            // The count overlaps the icon's lower-right corner, as in the
            // reference, rather than sitting under it: it reads as a label on
            // the thing rather than as a second row of content.
            slot.Count = StoryUi.CreateText("Count", slot.Root, "0", 58,
                StoryUi.RoyalTitle, TextAnchor.MiddleCenter);
            RectTransform count = slot.Count.rectTransform;
            count.anchorMin = new Vector2(0.5f, 0.5f);
            count.anchorMax = new Vector2(0.5f, 0.5f);
            count.pivot = new Vector2(0.5f, 0.5f);
            count.sizeDelta = new Vector2(SlotMaxWidth - 20f, 74f);
            count.anchoredPosition = new Vector2(18f, -58f);
            slot.Count.fontStyle = FontStyle.Bold;
            // A pass mark is five digits wide and a collect count is one, in the
            // same slot; best fit is the only thing that holds both.
            slot.Count.resizeTextForBestFit = true;
            slot.Count.resizeTextMinSize = 24;
            slot.Count.resizeTextMaxSize = 58;
            var outline = slot.Count.gameObject.AddComponent<Outline>();
            outline.effectColor = StoryUi.RoyalNavy;
            outline.effectDistance = new Vector2(3f, -3f);

            _slots.Add(slot);
            return slot;
        }

        #endregion

        #region Refresh

        private void Refresh()
        {
            if (_panel != null && _panel.Title != null)
                _panel.Title.text = "Level " + _level;

            StoryLevelHud.HudModel model = StoryLevelHud.ReadLevel(_level, 0);
            List<StoryLevelHud.HudTarget> targets = model == null
                ? new List<StoryLevelHud.HudTarget>()
                : model.Targets;

            int count = Mathf.Min(targets.Count, MaxGoals);
            while (_slots.Count < count) BuildSlot();

            for (int i = 0; i < _slots.Count; i++)
            {
                Slot slot = _slots[i];
                bool used = i < count;
                slot.Root.gameObject.SetActive(used);
                if (!used) continue;

                StoryLevelHud.HudTarget target = targets[i];
                slot.Icon.sprite = target.Icon != null ? target.Icon : StoryUi.StarSprite;
                slot.Icon.enabled = slot.Icon.sprite != null;

                // Read at score zero, so Remaining is the whole of what the
                // level asks for — the pass mark on a score target, the count on
                // a collect one. The popup states the goal, not the progress.
                slot.Count.text = Mathf.Max(0, target.Remaining).ToString();

                Place(slot.Root, i, count);
            }

            Debug.Log(string.Format("[Story] Level popup: Level {0} for area {1}, goal [{2}].",
                _level, _areaId, string.Join(" ", GoalTexts().ToArray())));
        }

        /// <summary>
        /// Lays the used slots out evenly across the well, centred. The pitch is
        /// fixed per count rather than divided out of the well's measured width:
        /// the well's rect is not resolved until a layout pass has run, and this
        /// screen is built and read back inside a single editor test.
        /// </summary>
        private static void Place(RectTransform slot, int index, int count)
        {
            float width = count <= 2 ? SlotMaxWidth : (count == 3 ? 190f : 150f);
            slot.sizeDelta = new Vector2(width, slot.sizeDelta.y);
            slot.anchoredPosition = new Vector2((index - (count - 1) * 0.5f) * width, 0f);
        }

        #endregion

        #region Actions

        private void OnPlay()
        {
            int level = _level;
            int areaId = _areaId;

            Debug.Log(string.Format("[Story] Level popup: Play pressed for level {0}, area {1}.",
                level, areaId));

            Close();

            if (Launcher != null) Launcher(level, areaId);
            else StoryFlow.LaunchLevel(level, areaId);
        }

        private void OnClose()
        {
            Action back = _onClose;

            Debug.Log(string.Format("[Story] Level popup: closed without playing level {0}.",
                _level));

            Close();
            if (back != null) back();
        }

        #endregion
    }
}
