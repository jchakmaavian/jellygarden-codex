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
        private const float NavHeightFrac = 150f / 1472f;
        private const float ButtonsBottomFrac = 200f / 1472f;
        private const float ButtonsHeightFrac = 164f / 1472f;

        // The two buttons across that band, as fractions of its width. In the
        // reference they run x 31..370 and 446..797 of a 31..797 band, so the
        // gold card is the wider of the two and the gutter between them is real.
        private const float LevelRightFrac = 339f / 766f;
        private const float AreaLeftFrac = 415f / 766f;

        // Inside the gold card: the name across the whole top, the bar and the
        // chest sharing the bottom. Fractions of the card's content rect. The
        // reference centres its name over the bar alone and gives the chest the
        // full height of the card, which works for "Area 2" and does not work
        // for "The Hall of Broken Mirrors" — a name narrowed to the bar's width
        // wraps to two lines and spills out through the top of the card.
        private const float AreaBarTopFrac = 0.62f;
        private const float AreaBarBottomFrac = 0.16f;
        private const float AreaBarRightFrac = 0.66f;

        // The chest is nearly square on purpose. It is a stack of rounded rects
        // — a lid over a body — and squeezed into a box half as tall as it is
        // wide those two read as a pair of purple ovals rather than as a chest.
        private const float ChestLeftFrac = 0.70f;
        private const float ChestRightFrac = 0.98f;
        private const float ChestBottomFrac = 0f;
        private const float ChestTopFrac = 0.66f;

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
                       && _topBar.Coins != null && _topBar.Lives != null
                       && _topBar.Stars != null && _topBar.Settings != null;
            }
        }

        /// <summary>What the top bar reads right now, as "coins/lives/stars".</summary>
        public string TopBarText
        {
            get
            {
                if (_topBar == null) return null;
                return string.Format("{0}/{1}/{2}",
                    _topBar.Coins.text, _topBar.Lives.text, _topBar.Stars.text);
            }
        }

        /// <summary>The band the Level and Area buttons sit in.</summary>
        public RectTransform ButtonBand
        {
            get { return _buttonBand; }
        }

        /// <summary>The band the five-tab nav goes in (US-010), empty until then.</summary>
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
            _topBar = StoryUi.TopBar(_canvas.transform, null);

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
            _levelButton = StoryUi.GreenButton("Level", _buttonBand, "Level", 66, OnLevel);
            RectTransform levelRect = _levelButton.GetComponent<RectTransform>();
            levelRect.anchorMin = new Vector2(0f, 0f);
            levelRect.anchorMax = new Vector2(LevelRightFrac, 1f);
            levelRect.offsetMin = Vector2.zero;
            levelRect.offsetMax = Vector2.zero;

            // Level numbers reach four digits by area 150, so the label fits
            // itself to the button rather than running off the end of it.
            _levelLabel = _levelButton.GetComponentInChildren<Text>();
            _levelLabel.resizeTextForBestFit = true;
            _levelLabel.resizeTextMinSize = 24;
            _levelLabel.resizeTextMaxSize = 66;

            _areaButton = StoryUi.GoldButton("Area", _buttonBand, OnArea);
            RectTransform areaRect = _areaButton.GetComponent<RectTransform>();
            areaRect.anchorMin = new Vector2(AreaLeftFrac, 0f);
            areaRect.anchorMax = new Vector2(1f, 1f);
            areaRect.offsetMin = Vector2.zero;
            areaRect.offsetMax = Vector2.zero;

            RectTransform content = StoryUi.ContentOf(_areaButton);

            // 150 area names, and they are not "Area 2" — best fit, or "The
            // Hall of Broken Mirrors" runs out through the chest.
            _areaLabel = StoryUi.CreateText("Name", content, string.Empty, 52,
                StoryUi.RoyalCream, TextAnchor.MiddleCenter);
            RectTransform labelRect = _areaLabel.rectTransform;
            labelRect.anchorMin = new Vector2(0f, AreaBarTopFrac);
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            _areaLabel.fontStyle = FontStyle.Bold;
            _areaLabel.resizeTextForBestFit = true;
            _areaLabel.resizeTextMinSize = 18;
            _areaLabel.resizeTextMaxSize = 52;
            var nameOutline = _areaLabel.gameObject.AddComponent<Outline>();
            nameOutline.effectColor = StoryUi.RoyalGoldDeep;
            nameOutline.effectDistance = new Vector2(2f, -2f);

            _areaBar = StoryUi.ProgressBar("Progress", content);
            _areaBar.Root.anchorMin = new Vector2(0f, AreaBarBottomFrac);
            _areaBar.Root.anchorMax = new Vector2(AreaBarRightFrac, AreaBarTopFrac);
            _areaBar.Root.offsetMin = Vector2.zero;
            _areaBar.Root.offsetMax = Vector2.zero;

            // The chest sits at the end of the bar: the reward the bar is filling
            // towards. Its panels are cream rather than the reference's gold,
            // because this card is already gold and a gold-panelled chest on it
            // is two purple rings with the card showing through them.
            _areaChest = StoryUi.ChestIcon("Chest", content, StoryUi.RoyalCream);
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

            if (_areaLabel != null) _areaLabel.text = area.Name;
            StoryUi.SetProgress(_areaBar,
                StoryProgress.TasksCompletedIn(_areaId), area.Tasks.Count);
            StoryUi.SetCountBadge(_areaBadge, AffordableRepairs(_areaId));
        }

        #endregion
    }
}
