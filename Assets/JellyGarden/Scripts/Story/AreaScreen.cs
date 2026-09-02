// The area screen: what King Leo is trying to fix here, what it costs, and the
// button that sends the player to a level to pay for it.
//
// This is the screen the map opens instead of the kit's pre-play popup. It reads
// everything from StoryDatabase and StoryProgress; it knows no area ids.

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
        private const float RowHeight = 150f;
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
        private const float PlayWidth = 460f;

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
            _instance.Refresh();

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

            // The room itself, in the band above the panel. Not stretched to the
            // whole canvas: the paintings are 4:3 and preserveAspect letterboxes
            // them into a tall screen, which put the room entirely behind the
            // panel and left the map showing through the top.
            _scene = StoryUi.CreateRect("Scene", _canvas.transform);
            _scene.anchorMin = new Vector2(0f, 0.66f);
            _scene.anchorMax = new Vector2(1f, 1f);
            _scene.offsetMin = new Vector2(0f, 0f);
            _scene.offsetMax = new Vector2(0f, 0f);

            StoryUi.CreateFullScreenButton("Scrim", _canvas.transform,
                new Color(0f, 0f, 0f, 0.35f), null);

            BuildCaption();

            // Royal Match's own area page carries the top bar, and this screen no
            // longer draws a star counter of its own: the stars on the bar are
            // this area's, because Open sets StoryProgress.CurrentArea first.
            _topBar = StoryUi.TopBar(_canvas.transform, null);

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

            _toast = StoryUi.CreateText("Toast", _canvas.transform, "", 36, Color.white,
                TextAnchor.MiddleCenter);
            StoryUi.BottomAnchored(_toast.rectTransform, 260f, 90f, 60f);
            _toast.gameObject.SetActive(false);
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
            headingRect.offsetMax = new Vector2(-24f, 0f);
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
            track.offsetMax = new Vector2(-34f, 0f);

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
            mask.color = new Color(0f, 0f, 0f, 0.02f);
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

            // Both numbers are read, never stored: a repair anywhere else in the
            // game moves this bar the next time the screen is looked at.
            StoryUi.SetProgress(_progress, StoryProgress.TasksCompletedIn(_areaId),
                area.Tasks.Count);
            StoryUi.RefreshTopBar(_topBar);

            BuildRows(area);
            RefreshRows(area);
            RefreshPlayButton(area);
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
                row.Icon = StoryUi.TaskIcon("Icon", rowRect, area.Id, task);
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
            }
        }

        private void RefreshPlayButton(StoryArea area)
        {
            bool complete = StoryProgress.IsAreaComplete(_areaId);
            int next = StoryLevelPlan.NextLevelFor(_areaId);

            StoryUi.SetGreenInteractable(_playButton, !complete && next > 0);
            if (_playLabel != null)
                _playLabel.text = complete ? "Restored" : "Play level " + next;

            // A beat's scene can be watched again once it has been earned.
            bool canReplay = complete && CutsceneRunner.Has(_areaId);
            if (_replayButton != null)
                _replayButton.gameObject.SetActive(canReplay);
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

            StoryRepair.Perform(_areaId, taskIndex, Refresh);
        }

        private void OnReplayScene()
        {
            int areaId = _areaId;
            Close();
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
            if (_toast != null && _toast.gameObject.activeSelf && StoryClock.Now > _toastUntil)
                _toast.gameObject.SetActive(false);
        }

        #endregion
    }
}
