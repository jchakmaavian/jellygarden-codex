// The area screen: what King Leo is trying to fix here, what it costs, and the
// button that sends the player to a level to pay for it.
//
// This is the screen the map opens instead of the kit's pre-play popup. It reads
// everything from StoryDatabase and StoryProgress; it knows no area ids.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace JellyGarden.Scripts.Story
{
    public sealed class AreaScreen : MonoBehaviour
    {
        // Measured off docs/royal-match/02-area-tasks.jpg: the cream row card is
        // 154 reference points tall with 50 between cards. The gap is pulled in,
        // because the reference only ever shows two rows and this story writes
        // areas of seven.
        private static float RowHeight => StorybookUi.Enabled ? 196f : 150f;
        private const float RowGap = 24f;

        // The row's three columns, in reference points from the card's edges:
        // the icon, then the label, then the Do It button on the right.
        private const float RowIconSize = 102f;
        private const float RowSidePad = 16f;
        private const float DoItWidth = 260f;
        private const float DoItHeight = 112f;

        // Measured off docs/royal-match/02-area-tasks.jpg (828x1472) and scaled to
        // the 1080 reference canvas by 1080/828 = 1.304. Every VERTICAL metric is
        // a fraction of the panel's content height rather than a count of pixels:
        // the reference is a 9:19.5 phone and the Editor's Game view is 4:3, and a
        // column of fixed heights that fits one overlaps on the other while every
        // editor test still passes. DesignHeight is what the panel's content rect
        // measures on the reference device.
        private const float DesignHeight = 1073f;
        private const float CardTopFrac = 65f / DesignHeight;
        private const float CardHeightFrac = 228f / DesignHeight;
        private const float CardGapFrac = 34f / DesignHeight;
        private const float PlayBottomFrac = 34f / DesignHeight;
        private const float PlayHeightFrac = 150f / DesignHeight;
        private const float PlayGapFrac = 30f / DesignHeight;

        // Horizontal metrics stay in reference points: the canvas scaler keeps the
        // width honest whatever the aspect.
        private const float CardSide = 64f;
        private const float ChestWidth = 265f;

        // How much of the card's right end the chest owns. Both the heading and
        // the track stop here rather than running the full width.
        //
        // Measured off docs/royal-match/02-area-tasks.jpg: their card runs x=85
        // to x=730 and the track stops at x=530, where the chest starts — so the
        // chest owns the right 31% of the card and "Area Progress" is centred
        // over the TRACK, not over the card. Centred over the card, which is
        // what this did, the heading runs straight under the chest and the two
        // read as one smudge.
        private const float ChestClearance = ChestWidth * 0.80f;
        private const float PlayWidth = 460f;

        // The room's band while the task list is up, and how far the panel dims
        // it. Both are released for the length of a repair: the room takes the
        // whole screen and the dimming comes off, which is the closest this
        // screen gets to Royal Match's camera staying on the room.
        private const float SceneBottomFrac = 0.66f;
        private const float ScrimAlpha = 0.35f;

        private static AreaScreen _instance;

        public static AreaScreen Instance
        {
            get { return _instance; }
        }

        public static bool IsOpen
        {
            get { return _instance != null && _instance.gameObject.activeSelf; }
        }

        private int _areaId;
        private Canvas _canvas;
        private RectTransform _scene;
        private RectTransform _band;
        private Image _scrim;
        private Image _backdrop;
        private bool _repairing;
        private CanvasGroup _repairPanelGroup;
        private Coroutine _repairPanelFade;
        private AreaSceneView _sceneView;
        private RectTransform _taskList;
        private Text _chapter;
        private Text _goal;
        private StoryUi.RoyalPanelParts _panel;
        private StoryUi.TopBarParts _topBar;
        private StoryUi.ProgressBarParts _progress;
        private RectTransform _chest;
        private Button _playButton;
        private Text _playLabel;
        private Button _replayButton;
        private Text _toast;
        private float _toastUntil;

        private readonly List<TaskRow> _rows = new List<TaskRow>();

        private sealed class TaskRow
        {
            public int TaskIndex;
            public int Cost;
            public RectTransform Icon;
            public Text Label;
            public Button Repair;
            public Image Tick;
        }

        // The rows are torn down and rebuilt when the area changes, not only when
        // the task count does: two areas of the same length would otherwise keep
        // each other's icons and star costs, which are baked in at build time.
        private int _rowsBuiltFor = -1;

        public int AreaId
        {
            get { return _areaId; }
        }

        /// <summary>What the title plate reads — the area's name.</summary>
        public string TitleText
        {
            get { return _panel == null || _panel.Title == null ? null : _panel.Title.text; }
        }

        /// <summary>What the Area Progress bar reads, as "done/total".</summary>
        public string ProgressText
        {
            get { return _progress == null || _progress.Label == null ? null : _progress.Label.text; }
        }

        /// <summary>How far along the Area Progress bar is drawn, 0..1.</summary>
        public float ProgressFraction
        {
            get { return _progress == null ? 0f : _progress.Fraction; }
        }

        /// <summary>Whether the chest at the end of the bar is on screen.</summary>
        public bool HasChest
        {
            get { return _chest != null && _chest.gameObject.activeInHierarchy; }
        }

        /// <summary>
        /// The panel's red X — the only way off this screen, and the one a test
        /// checking for a dead end has to press rather than call Close() behind.
        /// </summary>
        public Button CloseButton
        {
            get { return _panel == null ? null : _panel.Close; }
        }

        /// <summary>
        /// The rect the room is painted in. A repair plays over THIS — the room
        /// the player has been looking at while deciding to buy it — rather than
        /// on a second screen of its own.
        /// </summary>
        public RectTransform RoomRect
        {
            get { return _scene; }
        }

        /// <summary>The live view of the room, or null before the screen is opened.</summary>
        public AreaSceneView RoomView
        {
            get { return _sceneView; }
        }

        /// <summary>Whether the task list is up. False for the length of a repair.</summary>
        public bool PanelVisible
        {
            get { return _band != null && _band.gameObject.activeSelf; }
        }

        /// <summary>Whether the room currently has the whole screen.</summary>
        public bool RoomIsFullScreen
        {
            get { return _scene != null && _scene.anchorMin.y <= 0.001f; }
        }

        /// <summary>How many task rows are drawn.</summary>
        public int RowCount
        {
            get { return _rows.Count; }
        }

        /// <summary>The label the nth row reads, or null.</summary>
        public string RowLabel(int i)
        {
            return InRange(i) && _rows[i].Label != null ? _rows[i].Label.text : null;
        }

        /// <summary>The star price baked into the nth row's Do It button.</summary>
        public int RowCost(int i)
        {
            return InRange(i) ? _rows[i].Cost : 0;
        }

        /// <summary>Whether the nth row drew an icon for its repair.</summary>
        public bool RowHasIcon(int i)
        {
            return InRange(i) && _rows[i].Icon != null
                   && _rows[i].Icon.gameObject.activeInHierarchy
                   && _rows[i].Icon.GetComponentsInChildren<Image>(true).Length > 0;
        }

        /// <summary>Whether the nth row's Do It button is on screen at all.</summary>
        public bool RowOffersRepair(int i)
        {
            return InRange(i) && _rows[i].Repair != null
                   && _rows[i].Repair.gameObject.activeInHierarchy;
        }

        /// <summary>Whether the nth row's Do It button can actually be pressed.</summary>
        public bool RowCanRepair(int i)
        {
            return RowOffersRepair(i) && _rows[i].Repair.interactable;
        }

        /// <summary>Which task the nth row is for, in the story's own numbering.</summary>
        public int RowTaskIndex(int i)
        {
            return InRange(i) ? _rows[i].TaskIndex : -1;
        }

        /// <summary>
        /// The nth row's Do It button. Anything driving a repair from outside
        /// presses this rather than calling the repair directly — pressing it is
        /// the path the player has, and the guards live behind it.
        /// </summary>
        public Button RepairButton(int i)
        {
            return InRange(i) ? _rows[i].Repair : null;
        }

        /// <summary>Whether the nth row is drawn as a finished repair.</summary>
        public bool RowIsDone(int i)
        {
            return InRange(i) && _rows[i].Tick != null
                   && _rows[i].Tick.gameObject.activeInHierarchy;
        }

        private bool InRange(int i)
        {
            return i >= 0 && i < _rows.Count;
        }

        /// <summary>Opens the screen for an area, building it on first use.</summary>
        public static void Open(int areaId)
        {
            if (!StoryCampaign.CanEnter(areaId))
            {
                StoryToast.Show(StoryCampaign.ComingSoon);
                return;
            }
            if (StoryDatabase.GetArea(areaId) == null)
            {
                Debug.LogWarning("[Story] No area " + areaId + " to open.");
                return;
            }

            if (_instance == null)
            {
                var go = new GameObject("StoryAreaScreen");
                StoryObjects.Persist(go);
                _instance = go.AddComponent<AreaScreen>();
                _instance.Build();
            }

            _instance._areaId = areaId;
            StoryProgress.CurrentArea = areaId;
            _instance.gameObject.SetActive(true);

            // A repair that finished an area closes this screen with the panel
            // still put away. Opening it again has to find the task list, not the
            // bare room it was hidden behind.
            _instance.ShowAfterRepair();
            _instance.Refresh();
            if (StoryCampaign.IsTreasureHunt)
            {
                // Open on the next repair, including the last row on short
                // phones. Players can still scroll back to completed tasks.
                Canvas.ForceUpdateCanvases();
                ScrollRect scroll = _instance._taskList.GetComponentInParent<ScrollRect>();
                if (scroll != null)
                {
                    scroll.StopMovement();
                    int count = StoryDatabase.GetArea(areaId).Tasks.Count;
                    scroll.verticalNormalizedPosition = 1f - Mathf.Clamp01(
                        StoryProgress.TasksCompletedIn(areaId) / (float)Mathf.Max(1, count - 1));
                }
            }

            // One line per open rather than per refresh: Refresh runs on every
            // repair, and the screen tests open all 150 areas in a row.
            if (Application.isPlaying)
                Debug.Log(string.Format("[Story] area screen {0} '{1}': progress {2}, chest {3}",
                    areaId, _instance.TitleText, _instance.ProgressText,
                    _instance.HasChest ? "present" : "missing"));
        }

        public static void Close()
        {
            if (_instance != null) _instance.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        #region Build

        private void Build()
        {
            _canvas = StoryUi.CreateOverlayCanvas("AreaCanvas", 400);
            _canvas.transform.SetParent(transform, false);

            // Behind everything, and invisible until a repair plays. When the
            // panel goes away the room grows into the space it left, and a 4:3
            // painting in a tall screen leaves bars — this is what is in them,
            // instead of whichever screen happens to be open underneath.
            _backdrop = StoryUi.CreateImage("Backdrop", _canvas.transform, null,
                new Color(0f, 0f, 0f, 0f));
            StoryUi.Stretch(_backdrop.rectTransform);
            _backdrop.raycastTarget = false;

            // The room itself, in the band above the panel. Not stretched to the
            // whole canvas: the paintings are 4:3 and preserveAspect letterboxes
            // them into a tall screen, which put the room entirely behind the
            // panel and left the map showing through the top.
            _scene = StoryUi.CreateRect("Scene", _canvas.transform);
            _scene.anchorMin = new Vector2(0f, SceneBottomFrac);
            _scene.anchorMax = new Vector2(1f, 1f);
            _scene.offsetMin = new Vector2(0f, 0f);
            _scene.offsetMax = new Vector2(0f, 0f);

            _scrim = StoryUi.CreateFullScreenButton("Scrim", _canvas.transform,
                new Color(0f, 0f, 0f, ScrimAlpha), null).targetGraphic as Image;

            BuildCaption();

            // Royal Match's own area page carries the top bar, and this screen no
            // longer draws a star counter of its own: the stars on the bar are
            // this area's, because Open sets StoryProgress.CurrentArea first.
            _topBar = StoryUi.TopBar(_canvas.transform, OpenSettings);

            // The panel sits in the lower two thirds so the room's artwork stays
            // visible above it. Watching the gate change as repairs land is the
            // whole point of the screen — a panel over the top of it would have
            // hidden the one thing the player is paying stars for.
            // The panel stops above the bottom nav, which draws over this screen
            // rather than inside it (StoryNavBar sorts at 500 against this canvas's
            // 400). Anchored to the bar's own height constant, not to a guess: the
            // Play button sits at the bottom of this panel, and the reserve being
            // a few points short puts the nav's raised castle through it.
            RectTransform band = StoryUi.CreateRect("Panel", _canvas.transform);
            _band = band;
            // The 70 points that were the gap to the screen edge are kept as the
            // gap to the bar, because the raised tab's glyph stands half a bar
            // height PROUD of the bar and would otherwise reach the Play button.
            band.anchorMin = new Vector2(0f, StoryUi.NavBarHeightFrac);
            band.anchorMax = new Vector2(1f, 0.74f);
            band.offsetMin = new Vector2(0f, 70f);
            band.offsetMax = new Vector2(0f, 0f);

            // The red X is the way back to the map. There is no second Back
            // button, because the reference has none.
            _panel = StoryUi.RoyalPanel("Card", band, string.Empty, OnBack);

            // 150 area names, and some of them are long: the plate's 76pt fits
            // "Throne Room" and not "The Cartographer's Long Gallery".
            _panel.Title.resizeTextForBestFit = true;
            _panel.Title.resizeTextMinSize = 28;
            _panel.Title.resizeTextMaxSize = 76;

            BuildProgressCard(_panel.Content);
            BuildTaskList(_panel.Content);
            BuildFooter(_panel.Content);
            if (StorybookUi.Enabled) LayoutStorybookSheet();

            _toast = StoryUi.CreateText("Toast", _canvas.transform, "", 36, Color.white,
                TextAnchor.MiddleCenter);
            StoryUi.BottomAnchored(_toast.rectTransform, 260f, 90f, 60f);
            _toast.gameObject.SetActive(false);
        }

        private Text _bookProgress, _bookSummary;
        private RectTransform _bookCompletion;
        private float _bookLayoutHeight = -1;
        private void LayoutStorybookSheet()
        {
            _band.anchorMin = new Vector2(0, .004f);
            _band.anchorMax = new Vector2(1, .565f);
            _band.offsetMin = _band.offsetMax = Vector2.zero;
            _panel.Root.offsetMin = new Vector2(12, 0);
            _panel.Root.offsetMax = new Vector2(-12, 0);
            _panel.Content.offsetMin = new Vector2(55, 45);
            _panel.Content.offsetMax = new Vector2(-55, -408);
            Transform ribbon = _panel.Title.transform.parent;
            _panel.Title.transform.SetParent(_panel.Root, false);
            ribbon.gameObject.SetActive(false);
            StoryUi.TopAnchored(_panel.Title.rectTransform, 64, 105, 160);
            _panel.Title.font = StorybookArt.Font;
            _panel.Title.color = StorybookUi.Ink;
            var titleShadow = _panel.Title.GetComponent<Shadow>();
            if (titleShadow != null) titleShadow.enabled = false;
            _panel.Close.GetComponent<RectTransform>().anchoredPosition = new Vector2(-18, -18);
            _bookProgress = StoryUi.CreateText("CompletedTasks", _panel.Root, "", 42,
                StorybookUi.Muted, TextAnchor.MiddleCenter);
            StoryUi.TopAnchored(_bookProgress.rectTransform, 156, 65, 60);
            _bookProgress.fontStyle = FontStyle.Normal;
            Image rule = StoryUi.CreateImage("SummaryRule", _panel.Root, null, StorybookUi.GoldLight);
            StoryUi.TopAnchored(rule.rectTransform, 251, 2, 66);
            _bookSummary = StoryUi.CreateText("TaskSummary", _panel.Root, "", 49,
                StorybookUi.Ink, TextAnchor.MiddleLeft);
            StoryUi.TopAnchored(_bookSummary.rectTransform, 280, 75, 205);
            _bookSummary.fontStyle = FontStyle.Normal;
            Image tick = StorybookArt.Image("SummaryCheck", _panel.Root,
                StorybookArt.Crop("summary-check",472,569,31,30,15,0,24,3,29,11,29,20,22,28,12,29,4,24,0,16,3,7));
            StoryCastleUi.Place(tick.rectTransform, 91, -289, 76, 74);
            _panel.Content.Find("Progress").gameObject.SetActive(false);
            RectTransform viewport = (RectTransform)_taskList.parent;
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = new Vector2(0, 255);
            viewport.offsetMax = Vector2.zero;
            RectTransform play = _playButton.GetComponent<RectTransform>();
            play.anchorMin = new Vector2(0, 0);
            play.anchorMax = new Vector2(1, 0);
            play.pivot = new Vector2(.5f, 0);
            play.offsetMin = new Vector2(32, 42);
            play.offsetMax = new Vector2(-32, 241);
            _playLabel.resizeTextMinSize = 55;
            _playLabel.resizeTextMaxSize = 86;
            _scrim.color = Color.clear;
            _chapter.gameObject.SetActive(false);
            _goal.gameObject.SetActive(false);
            _bookCompletion = StoryUi.CreateRect("ChapterComplete", _panel.Content);
            StoryUi.Stretch(_bookCompletion);
            _bookCompletion.offsetMin = new Vector2(0, 205);
            Image clue = StoryUi.CreateImage("SunClue", _bookCompletion, StoryArt.ClueImage(1), Color.white);
            clue.preserveAspect = true;
            clue.rectTransform.anchorMin = new Vector2(.28f, .35f);
            clue.rectTransform.anchorMax = new Vector2(.72f, .98f);
            clue.rectTransform.offsetMin = clue.rectTransform.offsetMax = Vector2.zero;
            Text next = StoryUi.CreateText("NextChapter", _bookCompletion,
                "Next: Outer Courtyard\nComing soon", 42, StorybookUi.Ink, TextAnchor.MiddleCenter);
            next.rectTransform.anchorMin = Vector2.zero;
            next.rectTransform.anchorMax = new Vector2(1, .34f);
            next.rectTransform.offsetMin = next.rectTransform.offsetMax = Vector2.zero;
            _bookCompletion.gameObject.SetActive(false);
        }

        /// <summary>
        /// The arc and the area's goal, captioned on the artwork rather than set
        /// into the panel. Royal Match's card carries the room's name and nothing
        /// else, and the story is the one thing this game has that Royal Match
        /// does not — so it reads over the room instead of crowding the panel.
        /// </summary>
        private void BuildCaption()
        {
            // _scene spans the canvas from 0.66 up, and the panel's top edge is at
            // 0.74 of the canvas — 0.235 in this rect's own fractions. A caption
            // below that line is drawn behind the panel and reads as clipped text,
            // which is exactly what it did at 0.20. Both captions sit clear of the
            // panel below and of the top bar above.
            _chapter = StoryUi.CreateText("Chapter", _scene, "", 34, StoryUi.RoyalGold,
                TextAnchor.LowerCenter);
            Caption(_chapter, 0.50f, 0.62f);

            _goal = StoryUi.CreateText("Goal", _scene, "", 38, StoryUi.RoyalTitle,
                TextAnchor.UpperCenter);
            Caption(_goal, 0.27f, 0.49f);
        }

        private static void Caption(Text text, float minY, float maxY)
        {
            RectTransform rect = text.rectTransform;
            rect.anchorMin = new Vector2(0.06f, minY);
            rect.anchorMax = new Vector2(0.94f, maxY);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 16;
            text.resizeTextMaxSize = text.fontSize;

            // The artwork underneath is of any brightness, so the letters carry
            // their own contrast rather than hoping the room is dark.
            var outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            outline.effectDistance = new Vector2(2.5f, -2.5f);
        }

        /// <summary>
        /// "Area Progress": a cream card holding a sunken track that reads
        /// completed/total repairs, with the area's chest on its right-hand end.
        /// </summary>
        private void BuildProgressCard(RectTransform content)
        {
            Image card = StoryUi.CreamCard("Progress", content, 30);
            RectTransform rect = card.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f - CardTopFrac - CardHeightFrac);
            rect.anchorMax = new Vector2(1f, 1f - CardTopFrac);
            rect.offsetMin = new Vector2(CardSide, 0f);
            rect.offsetMax = new Vector2(-CardSide, 0f);

            Text heading = StoryUi.CreateText("Heading", rect, "Area Progress", 58,
                StoryUi.RoyalTitle, TextAnchor.MiddleCenter);
            RectTransform headingRect = heading.rectTransform;
            headingRect.anchorMin = new Vector2(0f, 0.62f);
            headingRect.anchorMax = new Vector2(1f, 0.97f);
            headingRect.offsetMin = new Vector2(24f, 0f);
            headingRect.offsetMax = new Vector2(-ChestClearance, 0f);
            heading.fontStyle = FontStyle.Bold;
            heading.resizeTextForBestFit = true;
            heading.resizeTextMinSize = 18;
            heading.resizeTextMaxSize = 58;

            var headingOutline = heading.gameObject.AddComponent<Outline>();
            headingOutline.effectColor = StoryUi.RoyalTealInk;
            headingOutline.effectDistance = new Vector2(2.5f, -2.5f);

            _progress = StoryUi.ProgressBar("Track", rect);
            RectTransform track = _progress.Root;
            track.anchorMin = new Vector2(0f, 0.20f);
            track.anchorMax = new Vector2(1f, 0.59f);
            track.offsetMin = new Vector2(34f, 0f);
            track.offsetMax = new Vector2(-ChestClearance, 0f);

            // The chest sits on the right-hand end of the track and overhangs it,
            // as it does in the reference. Its height is a fraction of the card,
            // so a short screen shrinks it instead of pushing it out of the card.
            _chest = StoryUi.ChestIcon("Chest", rect);
            _chest.anchorMin = new Vector2(1f, 0.06f);
            _chest.anchorMax = new Vector2(1f, 0.94f);
            _chest.pivot = new Vector2(1f, 0.5f);
            _chest.sizeDelta = new Vector2(ChestWidth, 0f);
            _chest.anchoredPosition = new Vector2(-8f, 0f);
        }

        private void BuildTaskList(RectTransform content)
        {
            RectTransform viewport = StoryUi.CreateRect("TaskViewport", content);
            viewport.anchorMin = new Vector2(0f, PlayBottomFrac + PlayHeightFrac + PlayGapFrac);
            viewport.anchorMax = new Vector2(1f, 1f - CardTopFrac - CardHeightFrac - CardGapFrac);
            viewport.offsetMin = new Vector2(CardSide, 0f);
            viewport.offsetMax = new Vector2(-CardSide, 0f);

            var mask = viewport.gameObject.AddComponent<Image>();
            mask.color = StorybookUi.Enabled ? Color.clear : new Color(0f, 0f, 0f, 0.02f);
            viewport.gameObject.AddComponent<RectMask2D>();

            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 30f;

            _taskList = StoryUi.CreateRect("TaskList", viewport);
            _taskList.anchorMin = new Vector2(0f, 1f);
            _taskList.anchorMax = new Vector2(1f, 1f);
            _taskList.pivot = new Vector2(0.5f, 1f);
            _taskList.sizeDelta = new Vector2(0f, 0f);

            scroll.content = _taskList;
            scroll.viewport = viewport;
        }

        private void BuildFooter(RectTransform content)
        {
            _playButton = StoryUi.GreenButton("Play", content, "Play", 52, OnPlay);
            RectTransform playRect = _playButton.GetComponent<RectTransform>();
            playRect.anchorMin = new Vector2(0.5f, PlayBottomFrac);
            playRect.anchorMax = new Vector2(0.5f, PlayBottomFrac + PlayHeightFrac);
            playRect.pivot = new Vector2(0.5f, 0.5f);
            playRect.sizeDelta = new Vector2(PlayWidth, 0f);
            playRect.anchoredPosition = Vector2.zero;

            _playLabel = _playButton.GetComponentInChildren<Text>();
            _playLabel.resizeTextForBestFit = true;
            _playLabel.resizeTextMinSize = 18;
            _playLabel.resizeTextMaxSize = 52;

            _replayButton = StoryUi.CreateButton("Replay", content, "Scene", 30, OnReplayScene);
            RectTransform replayRect = _replayButton.GetComponent<RectTransform>();
            replayRect.anchorMin = new Vector2(1f, PlayBottomFrac);
            replayRect.anchorMax = new Vector2(1f, PlayBottomFrac + PlayHeightFrac);
            replayRect.pivot = new Vector2(1f, 0.5f);
            replayRect.sizeDelta = new Vector2(170f, 0f);
            replayRect.anchoredPosition = new Vector2(-8f, 0f);
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

        #region Refresh

        /// <summary>Rebuilds the screen from current progress. Cheap enough to call on every change.</summary>
        public void Refresh()
        {
            StoryArea area = StoryDatabase.GetArea(_areaId);
            if (area == null) return;

            if (_sceneView == null)
                _sceneView = AreaSceneView.Attach(_scene, _areaId);
            else if (_sceneView.AreaId != _areaId)
                _sceneView.Show(_areaId);
            else
                // Same room, but the repair count may have moved while this
                // screen was not looking: the view rebuilds on an area change and
                // on nothing else, so its neglect wash has to be re-read here.
                _sceneView.SyncDamage();

            LayoutRoom();

            // The artwork is attached after the captions were built, so it is a
            // later sibling and draws over them. Push them back to the front.
            _goal.transform.SetAsLastSibling();
            _chapter.transform.SetAsLastSibling();

            StoryArcInfo arc = StoryDatabase.ArcOf(_areaId);
            _chapter.text = arc != null
                ? string.Format("Area {0:000} · Arc {1} — {2}", area.Id, arc.Arc, arc.Title)
                : string.Format("Area {0:000}", area.Id);
            _goal.text = area.Goal;
            _panel.Title.text = area.Name;
            if (_bookProgress != null)
                _bookProgress.text = StoryProgress.TasksCompletedIn(_areaId) + "/" + area.Tasks.Count + " complete";

            // Both numbers are read, never stored: a repair anywhere else in the
            // game moves this bar the next time the screen is looked at.
            StoryUi.SetProgress(_progress, StoryProgress.TasksCompletedIn(_areaId),
                area.Tasks.Count);
            StoryUi.RefreshTopBar(_topBar);

            BuildRows(area);
            RefreshRows(area);
            RefreshPlayButton(area);
        }

        private void LayoutRoom()
        {
            // Portrait paintings fill the screen behind the task card. Fitting
            // one into the old landscape band creates a second miniature gate
            // on top of the home screen's full-sized version.
            bool portrait = _sceneView != null && _sceneView.Aspect > 0f && _sceneView.Aspect < 1f;
            _scene.anchorMin = new Vector2(0f, portrait || _repairing ? 0f : SceneBottomFrac);
            _scene.anchorMax = Vector2.one;
            if (_sceneView != null) _sceneView.SetCovering(portrait || _repairing);

            // Keep captions in the same screen band when the picture grows.
            // On the portrait layout they also sit above the dimming scrim,
            // which otherwise darkens the light letters along with the gate.
            Transform captionParent = portrait ? _canvas.transform : _scene;
            if (_chapter.transform.parent != captionParent) _chapter.transform.SetParent(captionParent, false);
            if (_goal.transform.parent != captionParent) _goal.transform.SetParent(captionParent, false);
            float bottom = portrait ? SceneBottomFrac : 0f;
            float span = portrait ? 1f - SceneBottomFrac : 1f;
            CaptionBounds(_chapter, bottom + span * 0.50f, bottom + span * 0.62f);
            CaptionBounds(_goal, bottom + span * 0.27f, bottom + span * 0.49f);
            _chapter.color = portrait ? new Color(1f, 0.9f, 0.65f) : StoryUi.RoyalGold;
            _goal.color = portrait ? new Color(1f, 0.96f, 0.85f) : StoryUi.RoyalTitle;
        }

        private static void CaptionBounds(Text text, float minY, float maxY)
        {
            text.rectTransform.anchorMin = new Vector2(0.06f, minY);
            text.rectTransform.anchorMax = new Vector2(0.94f, maxY);
        }

        private void BuildRows(StoryArea area)
        {
            if (_rowsBuiltFor == area.Id && _rows.Count == area.Tasks.Count) return;
            _rowsBuiltFor = area.Id;

            foreach (TaskRow row in _rows)
                if (row.Label != null) Destroy(row.Label.transform.parent.gameObject);
            _rows.Clear();

            for (int i = 0; i < area.Tasks.Count; i++)
            {
                StoryTask task = area.Tasks[i];

                RectTransform rowRect = StoryUi.CreateRect("Task" + task.Index, _taskList);
                rowRect.anchorMin = new Vector2(0f, 1f);
                rowRect.anchorMax = new Vector2(1f, 1f);
                rowRect.pivot = new Vector2(0.5f, 1f);
                rowRect.offsetMin = new Vector2(0f, 0f);
                rowRect.offsetMax = new Vector2(0f, 0f);
                rowRect.anchoredPosition = new Vector2(0f, -i * (RowHeight + RowGap));
                rowRect.sizeDelta = new Vector2(0f, RowHeight);

                var row = new TaskRow { TaskIndex = task.Index, Cost = task.Stars };

                Image card = StoryUi.CreamCard("Card", rowRect, 26);
                StoryUi.Stretch(card.rectTransform);

                // The object being repaired, on the left of the row as in the
                // reference. Drawn from the kind of work unless someone has
                // painted this exact task.
                row.Icon = StorybookUi.Enabled ? StorybookUi.TaskIcon("Icon", rowRect, area.Id, task) : StoryUi.TaskIcon("Icon", rowRect, area.Id, task);
                row.Icon.anchorMin = new Vector2(0f, 0.5f);
                row.Icon.anchorMax = new Vector2(0f, 0.5f);
                row.Icon.pivot = new Vector2(0f, 0.5f);
                row.Icon.sizeDelta = new Vector2(RowIconSize, RowIconSize);
                row.Icon.anchoredPosition = new Vector2(RowSidePad, 0f);

                row.Label = StoryUi.CreateText("Label", rowRect, task.Label, 34,
                    StoryUi.RoyalNavy);
                row.Label.fontStyle = FontStyle.Bold;
                row.Label.rectTransform.anchorMin = new Vector2(0f, 0f);
                row.Label.rectTransform.anchorMax = new Vector2(1f, 1f);
                row.Label.rectTransform.offsetMin =
                    new Vector2(RowSidePad + RowIconSize + 14f, 0f);
                row.Label.rectTransform.offsetMax =
                    new Vector2(-(RowSidePad + DoItWidth + 14f), 0f);

                // 807 labels, and "Clear the collapsed scaffold from the nave" is
                // as real a task as "Lay the carpet".
                row.Label.resizeTextForBestFit = true;
                row.Label.resizeTextMinSize = 18;
                row.Label.resizeTextMaxSize = 34;

                // The price rides on the button, the way "Do It ★1" does in
                // docs/royal-match/02-area-tasks.jpg — there is no second place
                // on the row where the cost is written.
                int captured = task.Index;
                row.Repair = StoryUi.DoItButton("Repair", rowRect, "Do It", task.Stars,
                    () => OnRepair(captured));
                RectTransform repairRect = row.Repair.GetComponent<RectTransform>();
                repairRect.anchorMin = new Vector2(1f, 0.5f);
                repairRect.anchorMax = new Vector2(1f, 0.5f);
                repairRect.pivot = new Vector2(1f, 0.5f);
                repairRect.anchoredPosition = new Vector2(-RowSidePad, 0f);
                repairRect.sizeDelta = new Vector2(DoItWidth, DoItHeight);

                // A finished repair swaps the button for its own star back.
                row.Tick = StoryUi.CreateImage("Done", rowRect, StoryUi.StarSprite, StoryUi.Gold);
                row.Tick.rectTransform.anchorMin = new Vector2(1f, 0.5f);
                row.Tick.rectTransform.anchorMax = new Vector2(1f, 0.5f);
                row.Tick.rectTransform.pivot = new Vector2(1f, 0.5f);
                row.Tick.rectTransform.anchoredPosition = new Vector2(-RowSidePad - 60f, 0f);
                row.Tick.rectTransform.sizeDelta = new Vector2(72f, 72f);

                if (StorybookUi.Enabled)
                {
                    // The whole task row is the purchase target in the HTML
                    // design. Keep its existing guarded OnRepair callback.
                    if (Application.isPlaying) Destroy(row.Repair.gameObject);
                    else DestroyImmediate(row.Repair.gameObject);
                    Image hit = StoryUi.CreateImage("Repair", rowRect, null, Color.clear);
                    StoryUi.Stretch(hit.rectTransform);
                    row.Repair = StoryCastleUi.Clickable(hit, () => OnRepair(captured));
                    Image price = StorybookArt.Image("CostStar", rowRect, StorybookArt.SmallStar);
                    price.rectTransform.anchorMin = price.rectTransform.anchorMax = new Vector2(1,.5f);
                    price.rectTransform.sizeDelta = new Vector2(93,98);
                    price.rectTransform.anchoredPosition = new Vector2(-123,0);
                    Text amount = StoryUi.CreateText("Cost",rowRect,task.Stars.ToString(),65,StorybookUi.Ink,TextAnchor.MiddleCenter);
                    amount.font = StorybookArt.Font;
                    amount.rectTransform.anchorMin = amount.rectTransform.anchorMax = new Vector2(1,.5f);
                    amount.rectTransform.sizeDelta = new Vector2(71,110);
                    amount.rectTransform.anchoredPosition = new Vector2(-47,0);
                    row.Icon.sizeDelta = new Vector2(156,156);
                    row.Icon.anchoredPosition = new Vector2(38,0);
                    row.Label.rectTransform.offsetMin = new Vector2(232,9);
                    row.Label.rectTransform.offsetMax = new Vector2(-211,-9);
                }
                _rows.Add(row);
            }

            _taskList.sizeDelta = new Vector2(0f,
                area.Tasks.Count * (RowHeight + RowGap) + RowGap);
        }

        private void RefreshRows(StoryArea area)
        {
            // Repairs are still bought in the order the story wrote them, but a
            // row that is not yet the player's to buy is greyed rather than
            // stripped of its button: the star price is the reason to go and win
            // a level, so it has to stay on screen and stay legible. Royal Match
            // does the same with a task it will not let you afford.
            StoryTask next = StoryProgress.NextTask(_areaId);

            int visibleRow = 0;
            for (int i = 0; i < _rows.Count && i < area.Tasks.Count; i++)
            {
                TaskRow row = _rows[i];
                StoryTask task = area.Tasks[i];
                bool done = StoryProgress.IsTaskComplete(_areaId, task.Index);
                bool isNext = next != null && next.Index == task.Index;
                bool affordable = StoryProgress.CanCompleteTask(_areaId, task.Index);

                // Legacy UI Text supports <b>, <i>, <size>, <color> and nothing
                // else — an <s> for strikethrough renders as literal "<s>" on
                // screen. A completed repair is marked by its colour and its
                // star instead.
                row.Label.color = done ? StoryUi.Muted : StoryUi.RoyalNavy;
                row.Label.fontStyle = done ? FontStyle.Italic : FontStyle.Bold;
                row.Label.text = task.Label;

                if (row.Icon != null)
                {
                    var icon = row.Icon.GetComponent<CanvasGroup>();
                    if (icon == null) icon = row.Icon.gameObject.AddComponent<CanvasGroup>();
                    icon.alpha = done ? 0.45f : 1f;
                }

                row.Tick.gameObject.SetActive(done);

                // Done is the one state with no button at all: there is nothing
                // left to buy, and a greyed price on a finished repair reads as
                // something still owed.
                row.Repair.gameObject.SetActive(!done);
                StoryUi.SetGreenInteractable(row.Repair, isNext && affordable);
                if (StorybookUi.Enabled)
                {
                    var rect = (RectTransform)row.Label.transform.parent;
                    rect.gameObject.SetActive(!done);
                    if (!done)
                    {
                        rect.anchoredPosition = new Vector2(0, -visibleRow++ * (RowHeight + RowGap));
                        row.Label.fontStyle = FontStyle.Normal;
                        row.Label.font = StoryUi.BodyFont;
                        row.Label.fontStyle = FontStyle.Normal;
                        row.Label.resizeTextMinSize = 30;
                        row.Label.resizeTextMaxSize = 48;
                    }
                }
            }
            if (StorybookUi.Enabled)
            {
                _bookSummary.text = StoryProgress.TasksCompletedIn(_areaId) + " tasks complete";
                _taskList.sizeDelta = new Vector2(0, visibleRow * (RowHeight + RowGap));
                _bookCompletion.gameObject.SetActive(visibleRow == 0);
                if (visibleRow == 0) _bookProgress.text = area.Tasks.Count + "/" + area.Tasks.Count + " complete · First clue found!";
            }
        }

        private void RefreshPlayButton(StoryArea area)
        {
            bool complete = StoryProgress.IsAreaComplete(_areaId);
            int next = StoryLevelPlan.NextLevelFor(_areaId);

            StoryUi.SetGreenInteractable(_playButton, !complete && next > 0);
            if (_playLabel != null)
                _playLabel.text = complete ? (StorybookUi.Enabled ? "Area complete" : "Restored") : "Play Level " + next;

            // A beat's scene can be watched again once it has been earned.
            bool canReplay = complete && (StoryCampaign.IsTreasureHunt
                ? TreasureHuntPresentation.Has(area.PayoffScene) : CutsceneRunner.Has(_areaId));
            if (_replayButton != null)
            {
                _replayButton.gameObject.SetActive(canReplay);
                if (StorybookUi.Enabled && canReplay)
                {
                    _playButton.gameObject.SetActive(false);
                    RectTransform replay = _replayButton.GetComponent<RectTransform>();
                    replay.anchorMin = new Vector2(0, 0);
                    replay.anchorMax = new Vector2(1, 0);
                    replay.offsetMin = new Vector2(32, 42);
                    replay.offsetMax = new Vector2(-32, 241);
                    Text replayLabel = _replayButton.GetComponentInChildren<Text>();
                    replayLabel.text = "View the sun clue";
                    replayLabel.fontSize = 60;
                    StoryUi.FitOneLine(replayLabel, 40, 60);
                }
                else _playButton.gameObject.SetActive(true);
            }
        }

        #endregion

        #region Actions

        private void OnPlay()
        {
            int levelNumber = StoryLevelPlan.NextLevelFor(_areaId);
            if (levelNumber <= 0)
            {
                Toast("No level to play here.");
                return;
            }

            // The popup states the level's goal before the board opens, the way
            // Royal Match does; the X on it comes back here rather than to the
            // map, so no screen is reachable without a way out of it.
            int areaId = _areaId;
            Close();
            StoryLevelPopup.Open(levelNumber, areaId, () => Open(areaId));
        }

        /// <summary>
        /// Puts the task list away for the length of a repair and gives the whole
        /// screen to the room: the card goes, the dimming comes off and the
        /// painting grows out of its band. Royal Match never cuts to a second
        /// screen for a repair, and neither does this — what animates is the room
        /// that was already on screen.
        ///
        /// Called by StoryRepair rather than by the button, because a repair can
        /// be bought from more than one place and all of them should look the same.
        /// </summary>
        public void HideForRepair(bool fadePanel = false)
        {
            if (_repairing) return;
            _repairing = true;

            SetRepairPanelVisible(false, fadePanel);

            // The top bar goes with it, as it does in Royal Match: the reference
            // capture of a repair mid-play has the room running to the top edge
            // of the screen with no counters over it.
            if (_topBar != null && _topBar.Root != null)
                _topBar.Root.gameObject.SetActive(false);

            if (_chapter != null) _chapter.gameObject.SetActive(false);
            if (_goal != null) _goal.gameObject.SetActive(false);
            if (_toast != null) _toast.gameObject.SetActive(false);
            if (_scrim != null) _scrim.color = new Color(0f, 0f, 0f, 0f);

            // The backdrop stays clear. It used to go black here, and that black
            // was the whole problem: the room is letterboxed on this page, so a
            // black backdrop behind it put bars top and bottom and the repair
            // read as a cutscene on its own screen. The room is switched to
            // enveloping instead, so it fills what the task list gave back and
            // there is nothing behind it left to see.
            if (_backdrop != null) _backdrop.color = new Color(0f, 0f, 0f, 0f);

            if (_scene != null)
            {
                _scene.anchorMin = new Vector2(0f, 0f);
                _scene.anchorMax = new Vector2(1f, 1f);
            }

            if (_sceneView != null) _sceneView.SetCovering(true);
        }

        /// <summary>
        /// Brings the task list back once the repair is over. Always paired with
        /// <see cref="HideForRepair"/> by the caller that hid it: a screen left
        /// hidden is a dead end with no X on it.
        /// </summary>
        public void ShowAfterRepair(bool fadePanel = false)
        {
            if (!_repairing) return;
            _repairing = false;

            LayoutRoom();

            if (_backdrop != null) _backdrop.color = new Color(0f, 0f, 0f, 0f);
            if (_scrim != null) _scrim.color = new Color(0f, 0f, 0f, StorybookUi.Enabled ? 0f : ScrimAlpha);
            if (_chapter != null) _chapter.gameObject.SetActive(!StorybookUi.Enabled);
            if (_goal != null) _goal.gameObject.SetActive(!StorybookUi.Enabled);
            if (_topBar != null && _topBar.Root != null)
                _topBar.Root.gameObject.SetActive(true);
            SetRepairPanelVisible(true, fadePanel);
        }

        private void SetRepairPanelVisible(bool visible, bool animated)
        {
            if (_band == null) return;
            if (_repairPanelFade != null) StopCoroutine(_repairPanelFade);
            _repairPanelFade = null;
            if (_repairPanelGroup == null)
                _repairPanelGroup = _band.gameObject.AddComponent<CanvasGroup>();
            _repairPanelGroup.alpha = 1f;
            _repairPanelGroup.blocksRaycasts = visible;
            _repairPanelGroup.interactable = visible;
            _band.gameObject.SetActive(visible || animated);
            if (animated && Application.isPlaying && isActiveAndEnabled)
                _repairPanelFade = StartCoroutine(FadeRepairPanel(visible));
            else _band.gameObject.SetActive(visible);
        }

        private IEnumerator FadeRepairPanel(bool visible)
        {
            float started = Time.realtimeSinceStartup;
            float duration = visible ? 0.42f : 0.36f;
            while (Time.realtimeSinceStartup - started < duration)
            {
                float p = Mathf.SmoothStep(0f, 1f, (Time.realtimeSinceStartup - started) / duration);
                _repairPanelGroup.alpha = visible ? p : 1f - p;
                yield return null;
            }
            _repairPanelGroup.alpha = 1f;
            _band.gameObject.SetActive(visible);
            _repairPanelFade = null;
        }

        private void OnDisable()
        {
            if (_repairPanelFade != null) StopCoroutine(_repairPanelFade);
            _repairPanelFade = null;
            if (_repairPanelGroup != null) _repairPanelGroup.alpha = 1f;
        }

        private void OnRepair(int taskIndex)
        {
            StoryArea area = StoryDatabase.GetArea(_areaId);
            StoryTask task = StoryProgress.FindTask(area, taskIndex);
            if (task == null) return;

            // Every unfinished row now carries a button, so the story's order is
            // enforced here rather than by which buttons exist. onClick fires
            // whether or not a button is interactable.
            StoryTask next = StoryProgress.NextTask(_areaId);
            if (next == null || next.Index != taskIndex)
            {
                StorySfx.PlayInvalid();
                Toast("Finish the repair above this one first.");
                return;
            }

            if (!StoryProgress.CanCompleteTask(_areaId, taskIndex))
            {
                StorySfx.PlayInvalid();
                int short_ = task.Stars - StoryProgress.StarsIn(_areaId);
                Toast(short_ == 1
                    ? "One more star needed."
                    : short_ + " more stars needed.");
                return;
            }

            // The star flies to the counter, and the repair starts anyway.
            //
            // It was the other way round for one build — fly first, buy on
            // arrival — and it broke the shell self-test outright: that test
            // presses Do It and immediately asks whether the repair was bought,
            // and for 0.38 seconds it had not been. A press that does not do the
            // thing until an animation finishes is a press with lag in it, which
            // is the same complaint from the player's side. The flight is
            // feedback about a purchase that already happened.
            FlySpentStar(taskIndex);
            StoryRepair.Perform(_areaId, taskIndex, Refresh);
        }

        // How long the star takes to reach the counter, and how high it arcs on
        // the way as a fraction of the distance it covers. A straight line reads
        // as a sprite being dragged; the arc is what makes it read as thrown.
        private const float StarFlightSeconds = 0.38f;
        private const float StarFlightArc = 0.22f;

        /// <summary>
        /// Sends the spent star from the row that was pressed up to the counter
        /// it came out of. Purely feedback: the repair is bought by the caller
        /// whether or not this draws anything.
        ///
        /// It flies on a canvas of its own rather than on this screen's, because
        /// the repair takes this screen down — counter included — within the
        /// same frame. A star parented here would vanish on the first frame of
        /// its own flight, and one aimed at the counter's rect would be aiming
        /// at something already deactivated. Both ends are read as screen
        /// positions BEFORE any of that happens.
        /// </summary>
        private void FlySpentStar(int taskIndex)
        {
            if (!Application.isPlaying) return;

            RectTransform from = null;
            for (int i = 0; i < _rows.Count && from == null; i++)
                if (_rows[i].TaskIndex == taskIndex && _rows[i].Repair != null)
                    from = _rows[i].Repair.GetComponent<RectTransform>();

            RectTransform to = _topBar != null && _topBar.Stars != null
                ? _topBar.Stars.rectTransform.parent as RectTransform
                : null;

            if (from == null || to == null) return;

            StoryStarFlight.Play(
                RectTransformUtility.WorldToScreenPoint(null, from.position),
                RectTransformUtility.WorldToScreenPoint(null, to.position),
                StarFlightSeconds, StarFlightArc);
        }

        private void OnReplayScene()
        {
            int areaId = _areaId;
            Close();
            if (StoryCampaign.IsTreasureHunt)
                TreasureHuntPresentation.Play(StoryDatabase.GetArea(areaId).PayoffScene, areaId, () => Open(areaId), true);
            else
                CutsceneRunner.Play(areaId, () => Open(areaId));
        }

        private void OnBack()
        {
            Close();

            // Normally this closes onto whatever opened it — home, or the areas
            // list, both of which sort below. But a level ending, the preview menu
            // and a map node can all open this screen with nothing underneath it,
            // and a red X over an empty canvas is a dead end. EnsureFloor is a
            // no-op when there was somewhere to go back to.
            StoryShell.EnsureFloor();
        }

        public void Toast(string message)
        {
            if (_toast == null) return;
            _toast.text = message;
            _toast.gameObject.SetActive(true);
            _toastUntil = StoryClock.Now + 2.4f;
        }

        private void Update()
        {
            if (StorybookUi.Enabled && _panel != null && _bookSummary != null &&
                Mathf.Abs(_panel.Root.rect.height - _bookLayoutHeight) > .5f)
            {
                _bookLayoutHeight = _panel.Root.rect.height;
                float compact = Mathf.InverseLerp(1230, 1080, _bookLayoutHeight);
                _panel.Content.offsetMax = new Vector2(-55, -408 + 58 * compact);
                StoryUi.TopAnchored(_bookSummary.rectTransform, 280 - 40 * compact, 75, 205);
                StoryUi.TopAnchored((RectTransform)_panel.Root.Find("SummaryRule"),251 - 31 * compact,2,66);
                StoryCastleUi.Place((RectTransform)_panel.Root.Find("SummaryCheck"),91,-289 + 40 * compact,76,74);
            }
            if (_toast != null && _toast.gameObject.activeSelf && StoryClock.Now > _toastUntil)
                _toast.gameObject.SetActive(false);
        }

        #endregion
    }
}
