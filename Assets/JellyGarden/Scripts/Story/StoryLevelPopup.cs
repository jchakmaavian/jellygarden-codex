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
using JellyGarden.Scripts.GUI;
using UnityEngine;
using UnityEngine.UI;

namespace JellyGarden.Scripts.Story
{
    public sealed class StoryLevelPopup : MonoBehaviour
    {
        public const string HostName = "StoryLevelPopup";

        #region Metrics, measured off docs/royal-match/10-level-start-popup.jpg

        // The reference is a 9:16-and-then-some phone. A 4:3 view — an iPad, or
        // the Editor's default Game view — hands this canvas far fewer reference
        // units of height than of width, so every VERTICAL measurement here is a
        // fraction of the panel's own content height rather than a count of
        // pixels. Fixed heights fit the phone and silently overlap everywhere
        // else: that is exactly how the booster row first landed under the Play
        // button. Horizontal measurements stay in reference points, because the
        // canvas scaler keeps the width honest.
        //
        // The fractions are the measured pixel heights over DesignHeight, which
        // is what the panel's content rect measures on the reference device.
        private const float DesignHeight = 1146f;

        // The panel band. The popup does not fill the screen: the room and the
        // top bar stay visible above it and the nav bar below it.
        private const float PopupTop = 274f;
        private const float PopupBottom = 238f;

        // The cream Goal card, inset inside the panel's content rect.
        private const float GoalCardSide = 109f;
        private const float GoalCardTopFrac = 73f / DesignHeight;
        private const float GoalTitleFrac = 120f / 690f;   // of the card
        private const float GoalRowSide = 44f;

        // The Play button, and the gap the card keeps above it.
        private const float PlayWidth = 574f;
        private const float PlayHeightFrac = 209f / DesignHeight;
        private const float PlayBottomFrac = 122f / DesignHeight;
        private const float PlayGapFrac = 40f / DesignHeight;

        /// <summary>Four is what the Target panel holds; the popup matches it.</summary>
        private const int MaxGoals = 4;

        private const float SlotMaxWidth = 200f;
        private const float SlotIcon = 130f;

        // The "Select Boosters:" row, under the goal well and inside the same
        // cream card: three tiles at a fixed pitch, centred. Both fractions are
        // of the card's height, so the goal well takes whatever is left.
        private const float BoosterTitleFrac = 110f / 690f;
        private const float BoosterRowFrac = 210f / 690f;
        private const float BoosterTileWidth = 150f;
        private const float BoosterPitch = 238f;

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
        private readonly List<BoosterSlot> _boosters = new List<BoosterSlot>();

        private sealed class BoosterSlot
        {
            public BoostType Boost;
            public StoryUi.BoosterSlotParts Parts;
        }

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

        /// <summary>The owned count each booster tile is showing, in row order.</summary>
        public List<string> BoosterCounts()
        {
            var counts = new List<string>();
            for (int i = 0; i < _boosters.Count; i++) counts.Add(_boosters[i].Parts.Count.text);
            return counts;
        }

        public int BoosterCount
        {
            get { return _boosters.Count; }
        }

        public BoostType BoosterAt(int index)
        {
            return _boosters[index].Boost;
        }

        public Button BoosterButton(int index)
        {
            return _boosters[index].Parts.Button;
        }

        /// <summary>Whether the tile is drawn locked — no booster of that kind owned.</summary>
        public bool BoosterIsLocked(int index)
        {
            return _boosters[index].Parts.Locked.gameObject.activeSelf;
        }

        /// <summary>Whether the tile is drawn as chosen.</summary>
        public bool BoosterIsSelected(int index)
        {
            return _boosters[index].Parts.Ring.gameObject.activeSelf;
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

            // Each visit is a fresh choice: a booster picked and then backed out
            // of must not follow the player into the next level they open.
            StoryBoosters.ClearSelection();

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
            playRect.anchorMin = new Vector2(0.5f, PlayBottomFrac);
            playRect.anchorMax = new Vector2(0.5f, PlayBottomFrac + PlayHeightFrac);
            playRect.pivot = new Vector2(0.5f, 0.5f);
            playRect.sizeDelta = new Vector2(PlayWidth, 0f);
            playRect.anchoredPosition = Vector2.zero;

            Text playLabel = _play.GetComponentInChildren<Text>();
            playLabel.resizeTextForBestFit = true;
            playLabel.resizeTextMinSize = 30;
            playLabel.resizeTextMaxSize = 84;
        }

        /// <summary>
        /// The cream card: a "Goal" heading over a sunken row of icons. The row
        /// is its own rect so the goal slots can be rebuilt per level without
        /// touching the card around them. The booster row lives in the same card
        /// underneath, as it does in the reference.
        /// </summary>
        private void BuildGoalCard(Transform parent)
        {
            // Top of the content down to just above Play: whatever height the
            // screen has left is the card's, and its rows divide that.
            Image card = StoryUi.CreamCard("GoalCard", parent, 32);
            RectTransform rect = card.rectTransform;
            rect.anchorMin = new Vector2(0f, PlayBottomFrac + PlayHeightFrac + PlayGapFrac);
            rect.anchorMax = new Vector2(1f, 1f - GoalCardTopFrac);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(GoalCardSide, 0f);
            rect.offsetMax = new Vector2(-GoalCardSide, 0f);

            Text heading = Heading("Heading", rect, "Goal", 64,
                1f - GoalTitleFrac, 1f);
            heading.fontStyle = FontStyle.Bold;

            // The sunken well the icons sit in: the cream card's own darker
            // shade, which is how the reference separates the two.
            Image well = StoryUi.CreateImage("GoalWell", rect,
                StoryUi.RoundedSprite(24), StoryUi.RoyalCreamDeep);
            well.type = Image.Type.Sliced;
            RectTransform wellRect = well.rectTransform;
            wellRect.anchorMin = new Vector2(0f, BoosterTitleFrac + BoosterRowFrac);
            wellRect.anchorMax = new Vector2(1f, 1f - GoalTitleFrac);
            wellRect.pivot = new Vector2(0.5f, 0.5f);
            wellRect.offsetMin = new Vector2(GoalRowSide, 0f);
            wellRect.offsetMax = new Vector2(-GoalRowSide, 0f);

            _goalRow = StoryUi.Stretch(StoryUi.CreateRect("GoalRow", wellRect), 12f);

            BuildBoosterRow(rect);
        }

        /// <summary>
        /// "Select Boosters:" over three tiles, inside the same cream card as the
        /// goal — which is where the reference puts them, not in a strip of their
        /// own under the card.
        /// </summary>
        private void BuildBoosterRow(RectTransform card)
        {
            Text heading = Heading("BoosterHeading", card, "Select Boosters:", 58,
                BoosterRowFrac, BoosterRowFrac + BoosterTitleFrac);
            heading.fontStyle = FontStyle.Bold;

            RectTransform row = StoryUi.CreateRect("BoosterRow", card);
            row.anchorMin = new Vector2(0f, 0f);
            row.anchorMax = new Vector2(1f, BoosterRowFrac);
            row.offsetMin = Vector2.zero;
            row.offsetMax = Vector2.zero;

            BoostType[] boosts = StoryBoosters.PreLevel;
            for (int i = 0; i < boosts.Length; i++)
            {
                BoostType boost = boosts[i];

                var slot = new BoosterSlot();
                slot.Boost = boost;
                slot.Parts = StoryUi.BoosterSlot("Booster" + boost, row, null);
                slot.Parts.Button.onClick.AddListener(() => OnBooster(boost));

                // Fixed width, height from the row, so the tile is square-ish
                // whatever height the screen left the row.
                RectTransform tile = slot.Parts.Root;
                tile.anchorMin = new Vector2(0.5f, 0.1f);
                tile.anchorMax = new Vector2(0.5f, 0.9f);
                tile.pivot = new Vector2(0.5f, 0.5f);
                tile.sizeDelta = new Vector2(BoosterTileWidth, 0f);
                tile.anchoredPosition =
                    new Vector2((i - (boosts.Length - 1) * 0.5f) * BoosterPitch, 0f);

                slot.Parts.Icon.sprite = StoryBoosters.IconOf(boost);
                slot.Parts.Icon.enabled = slot.Parts.Icon.sprite != null;

                _boosters.Add(slot);
            }
        }

        /// <summary>
        /// A heading inside the cream card, placed by the fraction of the card's
        /// height it occupies rather than by a pixel offset from its top.
        /// </summary>
        private static Text Heading(string name, RectTransform card, string label, int size,
            float bottomFrac, float topFrac)
        {
            Text text = StoryUi.CreateText(name, card, label, size,
                StoryUi.RoyalNavy, TextAnchor.MiddleCenter);
            RectTransform rect = text.rectTransform;
            rect.anchorMin = new Vector2(0f, bottomFrac);
            rect.anchorMax = new Vector2(1f, topFrac);
            rect.offsetMin = new Vector2(12f, 4f);
            rect.offsetMax = new Vector2(-12f, -4f);

            // A heading in a card that shrank must shrink with it rather than
            // spilling over the row below.
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 20;
            text.resizeTextMaxSize = size;
            return text;
        }

        private Slot BuildSlot()
        {
            var slot = new Slot();
            slot.Root = StoryUi.CreateRect("Goal" + _slots.Count, _goalRow);
            slot.Root.anchorMin = new Vector2(0.5f, 0f);
            slot.Root.anchorMax = new Vector2(0.5f, 1f);
            slot.Root.pivot = new Vector2(0.5f, 0.5f);
            slot.Root.sizeDelta = new Vector2(SlotMaxWidth, 0f);

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

            RefreshBoosters();

            Debug.Log(string.Format(
                "[Story] Level popup: Level {0} for area {1}, goal [{2}], boosters [{3}].",
                _level, _areaId, string.Join(" ", GoalTexts().ToArray()),
                string.Join(" ", BoosterCounts().ToArray())));
        }

        /// <summary>
        /// Redraws the row from what the player owns and what they have chosen.
        /// Called on open and after every tap, so a locked slot unlocks the
        /// moment a booster is won.
        /// </summary>
        private void RefreshBoosters()
        {
            for (int i = 0; i < _boosters.Count; i++)
            {
                BoosterSlot slot = _boosters[i];
                int owned = StoryBoosters.Owned(slot.Boost);

                slot.Parts.Count.text = owned.ToString();
                StoryUi.SetBoosterLocked(slot.Parts, owned <= 0);
                StoryUi.SetBoosterSelected(slot.Parts, StoryBoosters.IsSelected(slot.Boost));
            }
        }

        private void OnBooster(BoostType boost)
        {
            bool selected = StoryBoosters.Toggle(boost);
            RefreshBoosters();

            Debug.Log(string.Format("[Story] Level popup: booster {0} {1}.",
                boost, selected ? "selected" : "cleared"));
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
            slot.sizeDelta = new Vector2(width, 0f);
            slot.anchoredPosition = new Vector2((index - (count - 1) * 0.5f) * width, 0f);
        }

        #endregion

        #region Actions

        private void OnPlay()
        {
            int level = _level;
            int areaId = _areaId;

            List<BoostType> chosen = StoryBoosters.Selection();
            var names = new List<string>();
            for (int i = 0; i < chosen.Count; i++) names.Add(chosen[i].ToString());

            Debug.Log(string.Format(
                "[Story] Level popup: Play pressed for level {0}, area {1}, boosters [{2}].",
                level, areaId, string.Join(" ", names.ToArray())));

            Close();

            if (Launcher != null) Launcher(level, areaId);
            else StoryFlow.LaunchLevel(level, areaId);
        }

        private void OnClose()
        {
            Action back = _onClose;

            // Backing out spends nothing and chooses nothing.
            StoryBoosters.ClearSelection();

            Debug.Log(string.Format("[Story] Level popup: closed without playing level {0}.",
                _level));

            Close();
            if (back != null) back();
        }

        #endregion
    }
}
