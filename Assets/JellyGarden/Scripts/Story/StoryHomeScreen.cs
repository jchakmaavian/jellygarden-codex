// The home screen: the room King Leo is restoring, and what the player owns.
//
// Royal Match opens on the room itself. There is no map, no level list and no
// menu — the artwork is the screen, edge to edge, and the interface is a strip
// of counters over the top of it. See docs/royal-match/01-home-area.jpg: the
// ceiling runs behind the coin and life pills, and the floor runs behind the
// buttons. Nothing is framed, nothing is letterboxed.
//
// Over the floor sit the only two things the player can press from here: a big
// green "Level N" that opens the level, and a gold card for the room itself
// carrying its name, its repairs-done bar and its chest. The five-tab nav bar
// arrives in the story after this one; the band it will occupy is laid out here
// and left empty, so adding it is filling a rect rather than re-cutting the
// screen.
//
// The room is drawn by AreaSceneView in its covering mode, which is the same
// stack of state layers the area screen shows — so a repair bought on the area
// page is visible here, and the neglect wash lifts as the area is paid off,
// with no second art path to keep in step.

using UnityEngine;
using UnityEngine.UI;

namespace JellyGarden.Scripts.Story
{
    public sealed class StoryHomeScreen : MonoBehaviour
    {
        public const string HostName = "StoryHomeScreen";

        // Below the area screen (400) and the level popup (600): home is the
        // screen everything else opens on top of, never the other way round.
        private const int SortingOrder = 300;

        // The bands, measured off docs/royal-match/09-home-level-button.jpg
        // (828x1472) scaled to the 1080x1920 reference canvas by 1080/828 =
        // 1.304. They are fractions of the canvas height, not pixels: the
        // reference is a 9:19.5 phone and the Editor's Game view is 4:3, and a
        // column of fixed heights that fits one overlaps on the other while
        // every editor test still passes.
        // The nav band is a RESERVATION, not the bar. StoryNavBar draws the five
        // tabs on its own persistent canvas, because the area page shows the same
        // strip and there must not be two of them; this rect keeps home from
        // laying anything out under it, and takes its height from the same
        // constant the bar does so the two cannot drift apart.
        private const float NavHeightFrac = StoryUi.NavBarHeightFrac;
        // Measured off the reference at 164 of 1472, and shorter than that on
        // purpose: both cards there carry a name that can run long, and ours
        // carry "Level 1" and "Area 1". Two short words do not need the height,
        // and every point given back is a point more of the room behind them.
        private const float ButtonsBottomFrac = 206f / 1472f;
        private const float ButtonsHeightFrac = 128f / 1472f;

        // The two buttons across that band, as fractions of its width. In the
        // reference they run x 31..370 and 446..797 of a 31..797 band, so the
        // gold card is the wider of the two and the gutter between them is real.
        private const float LevelRightFrac = 339f / 766f;
        private const float AreaLeftFrac = 415f / 766f;

        // Inside the card, as fractions of the card itself. Every number is
        // (pixel - cardOrigin) / cardSize measured on
        // docs/royal-match/09-home-level-button.jpg, where the card is 348x157
        // at x446,y1110. Unity counts y up and the screenshot counts it down,
        // so the vertical pairs are 1 minus the reading.
        //
        // The name and the bar share a left and right edge — in the reference
        // they are one stacked block, and the chest owns everything right of it.
        private const float AreaBarLeftFrac = 0.115f;     // x 486
        private const float AreaBarRightFrac = 0.557f;    // x 640
        private const float AreaBarBottomFrac = 0.261f;   // y 1226
        private const float AreaBarTopFrac = 0.484f;      // y 1191

        private const float AreaTextLeftFrac = AreaBarLeftFrac;
        private const float AreaTextRightFrac = AreaBarRightFrac;
        private const float AreaTextBottomFrac = 0.50f;   // y 1189, on the bar
        private const float AreaTextTopFrac = 0.80f;      // y 1141

        /// <summary>How much gold shows around the Level button, in reference points.</summary>
        private const float LevelRimThickness = 9f;

        // The chest: x 655..790, y 1140..1250 of the same card.
        private const float ChestLeftFrac = 0.60f;
        private const float ChestRightFrac = 0.99f;
        private const float ChestBottomFrac = 0.11f;
        private const float ChestTopFrac = 0.81f;

        private static StoryHomeScreen _instance;

        public static StoryHomeScreen Instance
        {
            get { return _instance; }
        }

        public static bool IsOpen
        {
            get { return _instance != null && _instance.gameObject.activeSelf; }
        }

        private int _areaId;
        private Canvas _canvas;
        private RectTransform _room;
        private RectTransform _buttonBand;
        private RectTransform _navBand;
        private AreaSceneView _roomView;
        private StoryUi.TopBarParts _topBar;
        private Button _levelButton;
        private Text _levelLabel;
        private Button _areaButton;
        private Text _areaLabel;
        private StoryUi.ProgressBarParts _areaBar;
        private RectTransform _areaChest;
        private RectTransform _areaBadge;

        /// <summary>The area whose room is on screen.</summary>
        public int AreaId
        {
            get { return _areaId; }
        }

        /// <summary>The room's layer stack, for a test that wants to look at it.</summary>
        public AreaSceneView Room
        {
            get { return _roomView; }
        }

        /// <summary>
        /// The rect the room is painted in, so a repair bought while the home
        /// screen is up plays on THAT room rather than on a screen of its own.
        /// The area page has the same property for the same reason.
        /// </summary>
        public RectTransform RoomRect
        {
            get { return _room; }
        }

        /// <summary>Whether the room is drawn edge to edge rather than letterboxed.</summary>
        public bool RoomIsFullBleed
        {
            get { return _roomView != null && _roomView.IsCovering && _roomView.Aspect > 0f; }
        }

        /// <summary>How many state layers of the room the player can see.</summary>
        public int VisibleRoomLayers
        {
            get { return _roomView == null ? 0 : _roomView.VisibleLayerCount; }
        }

        /// <summary>Whether the top bar's four slots are all on screen.</summary>
        public bool HasTopBar
        {
            get
            {
                return _topBar != null && _topBar.Root != null
                       && _topBar.Root.gameObject.activeInHierarchy
                       && _topBar.Coins != null
                       && _topBar.Stars != null && _topBar.Settings != null;
            }
        }

        /// <summary>What the top bar reads right now, as "coins/stars".</summary>
        public string TopBarText
        {
            get
            {
                if (_topBar == null) return null;
                return string.Format("{0}/{1}",
                    _topBar.Coins.text, _topBar.Stars.text);
            }
        }

        /// <summary>The band the Level and Area buttons sit in.</summary>
        public RectTransform ButtonBand
        {
            get { return _buttonBand; }
        }

        /// <summary>
        /// The band StoryNavBar's five tabs cover. Kept empty on purpose: the bar
        /// is its own canvas above this one, because it outlives this screen.
        /// </summary>
        public RectTransform NavBand
        {
            get { return _navBand; }
        }

        /// <summary>The green button that opens the next level.</summary>
        public Button LevelButton
        {
            get { return _levelButton; }
        }

        /// <summary>The gold card that opens this area's page.</summary>
        public Button AreaButton
        {
            get { return _areaButton; }
        }

        /// <summary>What the green button reads, e.g. "Level 16".</summary>
        public string LevelButtonText
        {
            get { return _levelLabel == null ? null : _levelLabel.text; }
        }

        /// <summary>What the gold card's title reads — the area's own name.</summary>
        public string AreaButtonText
        {
            get { return _areaLabel == null ? null : _areaLabel.text; }
        }

        /// <summary>The gold card's repair count, e.g. "1/7".</summary>
        public string AreaProgressText
        {
            get { return _areaBar == null || _areaBar.Label == null ? null : _areaBar.Label.text; }
        }

        /// <summary>Whether the chest is drawn on the end of the card's bar.</summary>
        public bool HasChest
        {
            get { return _areaChest != null && _areaChest.gameObject.activeInHierarchy; }
        }

        /// <summary>The level the green button would open, or 0 if there is none.</summary>
        public int NextLevel
        {
            get { return StoryLevelPlan.NextLevelFor(_areaId); }
        }

        /// <summary>
        /// How many repairs the player could buy in this area right now: the red
        /// badge on the gold card. Walked in the story's order and paid for out
        /// of one pool of stars, because that is how the area page spends them —
        /// a badge counting repairs the player cannot actually afford in
        /// sequence would be pointing at a button that greys out when pressed.
        /// </summary>
        public static int AffordableRepairs(int areaId)
        {
            StoryArea area = StoryDatabase.GetArea(areaId);
            if (area == null) return 0;

            int stars = StoryProgress.StarsIn(areaId);
            int affordable = 0;

            for (int i = 0; i < area.Tasks.Count; i++)
            {
                StoryTask task = area.Tasks[i];
                if (StoryProgress.IsTaskComplete(areaId, task.Index)) continue;

                // The story's order is a queue, so the first repair the player
                // cannot pay for stops the count rather than being skipped past.
                if (task.Stars > stars) break;

                stars -= task.Stars;
                affordable++;
            }

            return affordable;
        }

        /// <summary>
        /// The room the player is actually working on. Normally where they last
        /// were, but an area they have finished is not somewhere to come home to:
        /// once it is restored, home moves on to the first unlocked area that
        /// still has repairs owed.
        /// </summary>
        public static int CurrentRoom
        {
            get
            {
                int where = StoryProgress.CurrentArea;
                if (StoryDatabase.GetArea(where) != null && !StoryProgress.IsAreaComplete(where))
                    return where;

                for (int areaId = 1; areaId <= StoryDatabase.AreaCount; areaId++)
                {
                    if (!StoryProgress.IsAreaUnlocked(areaId)) continue;
                    if (StoryProgress.IsAreaComplete(areaId)) continue;
                    return areaId;
                }

                // Every area restored: stay in the last one rather than fall to 0.
                return StoryProgress.FurthestUnlockedArea;
            }
        }

        #region Open and close

        /// <summary>Opens home on the room the player is restoring.</summary>
        public static void Open()
        {
            Open(CurrentRoom);
        }

        /// <summary>Opens home on a named area.</summary>
        public static void Open(int areaId)
        {
            if (StoryDatabase.GetArea(areaId) == null)
            {
                Debug.LogWarning("[Story] No area " + areaId + " to come home to.");
                return;
            }

            if (_instance == null)
            {
                var go = new GameObject(HostName);
                StoryObjects.Persist(go);
                _instance = go.AddComponent<StoryHomeScreen>();
                _instance.Build();
            }

            _instance._areaId = areaId;
            StoryProgress.CurrentArea = areaId;
            _instance.gameObject.SetActive(true);
            _instance.Refresh();

            StoryArea area = StoryDatabase.GetArea(areaId);
            Debug.Log(string.Format(
                "[Story] home screen: area {0} '{1}', {2}/{3} repairs, room {4} layer(s) " +
                "of {5}, top bar {6}, buttons ['{7}'] ['{8}' {9} chest {10}, {11} ready]",
                areaId, area.Name, StoryProgress.TasksCompletedIn(areaId), area.Tasks.Count,
                _instance.VisibleRoomLayers,
                _instance._roomView == null ? 0 : _instance._roomView.LayerCount,
                _instance.TopBarText,
                _instance.LevelButtonText, _instance.AreaButtonText,
                _instance.AreaProgressText, _instance.HasChest ? "present" : "missing",
                AffordableRepairs(areaId)));
        }

        public static void Close()
        {
            if (_instance != null) _instance.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        /// <summary>
        /// The gear. Opens the settings panel and refreshes on the way back, so
        /// a reset taken from here leaves the screen showing the wiped save
        /// rather than the numbers it had before.
        /// </summary>
        private void OpenSettings()
        {
            StorySettings.Open(Refresh);
        }

        #endregion

        #region Build

        private void Build()
        {
            _canvas = StoryUi.CreateOverlayCanvas("HomeCanvas", SortingOrder);
            _canvas.transform.SetParent(transform, false);

            // The room fills the canvas and is masked by it. The paintings are
            // 4:3 and the phone is 9:19.5, so covering crops their sides — which
            // is the right loss to take: these rooms are composed around their
            // centre, and the alternative is bars down the top and bottom of a
            // screen whose whole subject is the room.
            _room = StoryUi.CreateRect("Room", _canvas.transform);
            StoryUi.Stretch(_room);
            _room.gameObject.AddComponent<RectMask2D>();

            // The counters sit straight on the artwork, as in the reference —
            // no bar, no scrim. Their own rims carry them against a bright
            // ceiling or a dark one.
            _topBar = StoryUi.TopBar(_canvas.transform, OpenSettings);

            _buttonBand = StoryUi.CreateRect("Buttons", _canvas.transform);
            _buttonBand.anchorMin = new Vector2(0f, ButtonsBottomFrac);
            _buttonBand.anchorMax = new Vector2(1f, ButtonsBottomFrac + ButtonsHeightFrac);
            _buttonBand.offsetMin = new Vector2(40f, 0f);
            _buttonBand.offsetMax = new Vector2(-40f, 0f);
            BuildButtons();

            _navBand = StoryUi.CreateRect("Nav", _canvas.transform);
            _navBand.anchorMin = Vector2.zero;
            _navBand.anchorMax = new Vector2(1f, NavHeightFrac);
            _navBand.offsetMin = Vector2.zero;
            _navBand.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// The two buttons across the band: "Level N" and the area card. Their
        /// text and numbers are written by Refresh, not here — the player can
        /// beat a level or buy a repair and come straight back to this screen.
        /// </summary>
        private void BuildButtons()
        {
            // The Level button is the splash's PLAY button with another word on
            // it.
            //
            // It was a green plate inside a gold rim, assembled out of
            // primitives, and it was the last screen element still built that
            // way — beside a painted castle, painted ribbons and a painted PLAY
            // it read as the one piece of UI in the room. The same sprite for
            // both also says something true: this is the button that starts a
            // game, and it is the same button on both screens.
            _levelButton = StoryUi.PaintedButton("Level", _buttonBand, "Level", 66, OnLevel);

            if (_levelButton != null)
            {
                RectTransform paintedRect = _levelButton.GetComponent<RectTransform>();
                paintedRect.anchorMin = new Vector2(0f, 0f);
                paintedRect.anchorMax = new Vector2(LevelRightFrac, 1f);
                paintedRect.offsetMin = Vector2.zero;
                paintedRect.offsetMax = Vector2.zero;
            }
            else
            {
                // No painted button on disk. The drawn one it replaces, rim and
                // all: a gold plate with the green button inset in it, which is
                // how docs/royal-match/01-home-area.jpg pairs it with the area
                // card beside it. StoryUi has already warned about the sprite.
                Image levelRim = StoryUi.CreateImage("LevelRim", _buttonBand,
                    StoryUi.RoundedSprite(28), StoryUi.RoyalGoldDeep);
                levelRim.type = Image.Type.Sliced;
                RectTransform rimRect = levelRim.rectTransform;
                rimRect.anchorMin = new Vector2(0f, 0f);
                rimRect.anchorMax = new Vector2(LevelRightFrac, 1f);
                rimRect.offsetMin = Vector2.zero;
                rimRect.offsetMax = Vector2.zero;

                Image levelRimLit = StoryUi.CreateImage("LevelRimLit", rimRect,
                    StoryUi.RoundedSprite(28), StoryUi.RoyalGold);
                StoryUi.Stretch(levelRimLit.rectTransform, LevelRimThickness * 0.45f);
                levelRimLit.type = Image.Type.Sliced;

                _levelButton = StoryUi.GreenButton("Level", rimRect, "Level", 66, OnLevel);
                RectTransform levelRect = _levelButton.GetComponent<RectTransform>();
                StoryUi.Stretch(levelRect, LevelRimThickness);
            }

            // Level numbers reach four digits by area 150, so the label fits
            // itself to the button rather than running off the end of it.
            _levelLabel = _levelButton.GetComponentInChildren<Text>();
            _levelLabel.resizeTextForBestFit = true;
            _levelLabel.resizeTextMinSize = 24;
            _levelLabel.resizeTextMaxSize = 66;

            // The card, rebuilt off the reference pixel by pixel.
            //
            // docs/royal-match/09-home-level-button.jpg is this exact screen.
            // The card there is 348x157 at x446,y1110 of an 828x1472 shot, and
            // everything below — three bands, their colours, and the fractions
            // the name, the bar and the chest sit at — is measured off it rather
            // than judged by eye. Drawn rather than painted, because a generated
            // plate can be close to the reference but cannot be the reference.
            _areaButton = StoryUi.CardPlate("Area", _buttonBand, OnArea);

            RectTransform areaRect = _areaButton.GetComponent<RectTransform>();
            areaRect.anchorMin = new Vector2(AreaLeftFrac, 0f);
            areaRect.anchorMax = new Vector2(1f, 1f);
            areaRect.offsetMin = Vector2.zero;
            areaRect.offsetMax = Vector2.zero;

            // The parts sit on the card itself, at fractions measured off the
            // reference — no content rect, because every rect below is already
            // expressed as a fraction of the whole card.
            RectTransform content = areaRect;

            // "Area 1", not "Castle Gates" — the word the reference card uses.
            //
            // It also holds one type size for all 150 areas: the names had to
            // shrink to 18pt for the longer ones, so the card's type changed
            // from area to area. The name still has the whole area screen to be
            // said on. Centred over the bar, as in the reference, not over the
            // whole card — the chest owns the right-hand third.
            _areaLabel = StoryUi.CreateText("Name", content, string.Empty, 52,
                StoryUi.CardInk, TextAnchor.MiddleCenter);
            RectTransform labelRect = _areaLabel.rectTransform;
            labelRect.anchorMin = new Vector2(AreaTextLeftFrac, AreaTextBottomFrac);
            labelRect.anchorMax = new Vector2(AreaTextRightFrac, AreaTextTopFrac);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            _areaLabel.fontStyle = FontStyle.Bold;

            StoryUi.FitOneLine(_areaLabel, 20, 52);
            var nameOutline = _areaLabel.gameObject.AddComponent<Outline>();
            nameOutline.effectColor = StoryUi.CardInkEdge;
            nameOutline.effectDistance = new Vector2(2f, -2f);

            _areaBar = StoryUi.ProgressBar("Progress", content);
            _areaBar.Root.anchorMin = new Vector2(AreaBarLeftFrac, AreaBarBottomFrac);
            _areaBar.Root.anchorMax = new Vector2(AreaBarRightFrac, AreaBarTopFrac);
            _areaBar.Root.offsetMin = Vector2.zero;
            _areaBar.Root.offsetMax = Vector2.zero;

            // The chest sits at the end of the bar: the reward the bar is
            // filling towards, straight on the gold.
            //
            // It had a sunken blue plate under it, added when the chest was six
            // drawn rectangles and vanished into the card. The painted chest
            // does not need one, and the reference has no plate: the chest sits
            // on the gold face with nothing behind it.
            _areaChest = StoryUi.ChestIcon("Chest", content, StoryUi.RoyalChestGold);
            _areaChest.anchorMin = new Vector2(ChestLeftFrac, ChestBottomFrac);
            _areaChest.anchorMax = new Vector2(ChestRightFrac, ChestTopFrac);
            _areaChest.offsetMin = Vector2.zero;
            _areaChest.offsetMax = Vector2.zero;

            // On the button's own rect, not its content: the badge hangs off the
            // top-right corner and is meant to break the rim.
            _areaBadge = StoryUi.CountBadge("Ready", _areaButton.transform, 0);
            _areaBadge.anchorMin = Vector2.one;
            _areaBadge.anchorMax = Vector2.one;
            _areaBadge.pivot = new Vector2(0.5f, 0.5f);
            _areaBadge.anchoredPosition = new Vector2(-18f, 4f);
        }

        #endregion

        #region Actions

        /// <summary>
        /// The green button: the level popup over the room. Home stays open
        /// underneath it — the popup's canvas sorts above this one — so backing
        /// out of the popup only has to bring the counters up to date.
        /// </summary>
        private void OnLevel()
        {
            int areaId = _areaId;
            int levelNumber = StoryLevelPlan.NextLevelFor(areaId);
            if (levelNumber <= 0)
            {
                Debug.LogWarning("[Story] No level to play in area " + areaId + ".");
                return;
            }

            Debug.Log(string.Format("[Story] home: Level {0} pressed in area {1}.",
                levelNumber, areaId));
            StoryLevelPopup.Open(levelNumber, areaId, () => Open(areaId));
        }

        /// <summary>The gold card: the area's own page, which sorts above home.</summary>
        private void OnArea()
        {
            Debug.Log("[Story] home: area card pressed for area " + _areaId + ".");
            AreaScreen.Open(_areaId);
        }

        #endregion

        #region Refresh

        /// <summary>
        /// Rebuilds the room from current progress and re-reads the counters.
        /// The layer stack is rebuilt rather than nudged: the player may have
        /// bought repairs on the area page since home was last looked at, and
        /// the reveal animation only knows about the one that just landed.
        /// </summary>
        public void Refresh()
        {
            if (StoryDatabase.GetArea(_areaId) == null) return;

            if (_roomView == null)
                _roomView = AreaSceneView.Attach(_room, _areaId, true);
            else
                _roomView.Show(_areaId);

            // The artwork is attached under _room, which was built before the
            // top bar, so the bar stays above it — but the bar is rebuilt by
            // nobody and the room by every refresh, so make the order explicit
            // rather than depend on when each was created.
            if (_topBar != null && _topBar.Root != null)
                _topBar.Root.SetAsLastSibling();
            if (_buttonBand != null) _buttonBand.SetAsLastSibling();
            if (_navBand != null) _navBand.SetAsLastSibling();

            StoryUi.RefreshTopBar(_topBar);
            RefreshButtons();
        }

        /// <summary>
        /// Points both buttons at the current area: the next level the player has
        /// not beaten, and the room's own name and repair count.
        /// </summary>
        private void RefreshButtons()
        {
            StoryArea area = StoryDatabase.GetArea(_areaId);
            if (area == null) return;

            int next = StoryLevelPlan.NextLevelFor(_areaId);
            if (_levelLabel != null)
                _levelLabel.text = next > 0 ? "Level " + next : "No level";

            // Greyed rather than hidden, and the handler guards itself: a
            // non-interactable button still fires onClick when invoked directly.
            StoryUi.SetGreenInteractable(_levelButton, next > 0);

            if (_areaLabel != null) _areaLabel.text = "Area " + _areaId;
            StoryUi.SetProgress(_areaBar,
                StoryProgress.TasksCompletedIn(_areaId), area.Tasks.Count);
            StoryUi.SetCountBadge(_areaBadge, AffordableRepairs(_areaId));
        }

        #endregion
    }
}
