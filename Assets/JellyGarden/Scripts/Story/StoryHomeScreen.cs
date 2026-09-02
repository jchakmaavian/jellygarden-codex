// The home screen: the room King Leo is restoring, and what the player owns.
//
// Royal Match opens on the room itself. There is no map, no level list and no
// menu — the artwork is the screen, edge to edge, and the interface is a strip
// of counters over the top of it. See docs/royal-match/01-home-area.jpg: the
// ceiling runs behind the coin and life pills, and the floor runs behind the
// buttons. Nothing is framed, nothing is letterboxed.
//
// That is the whole of this screen for now. The green Level button, the area
// button and the five-tab nav bar arrive in the stories after this one; the
// bands they will occupy are laid out here and left empty, so adding them is
// filling a rect rather than re-cutting the screen.
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

        // The bands US-009 and US-010 fill, measured off
        // docs/royal-match/01-home-area.jpg (828x1472) scaled to the 1080x1920
        // reference canvas by 1080/828 = 1.304. They are fractions of the canvas
        // height, not pixels: the reference is a 9:19.5 phone and the Editor's
        // Game view is 4:3, and a column of fixed heights that fits one overlaps
        // on the other while every editor test still passes.
        private const float NavHeightFrac = 150f / 1472f;
        private const float ButtonsBottomFrac = 190f / 1472f;
        private const float ButtonsHeightFrac = 150f / 1472f;

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

        /// <summary>
        /// The band the Level and Area buttons go in (US-009), empty until then.
        /// </summary>
        public RectTransform ButtonBand
        {
            get { return _buttonBand; }
        }

        /// <summary>The band the five-tab nav goes in (US-010), empty until then.</summary>
        public RectTransform NavBand
        {
            get { return _navBand; }
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
                "of {5}, top bar {6}",
                areaId, area.Name, StoryProgress.TasksCompletedIn(areaId), area.Tasks.Count,
                _instance.VisibleRoomLayers,
                _instance._roomView == null ? 0 : _instance._roomView.LayerCount,
                _instance.TopBarText));
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
            _buttonBand.offsetMin = new Vector2(48f, 0f);
            _buttonBand.offsetMax = new Vector2(-48f, 0f);

            _navBand = StoryUi.CreateRect("Nav", _canvas.transform);
            _navBand.anchorMin = Vector2.zero;
            _navBand.anchorMax = new Vector2(1f, NavHeightFrac);
            _navBand.offsetMin = Vector2.zero;
            _navBand.offsetMax = Vector2.zero;
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
        }

        #endregion
    }
}
