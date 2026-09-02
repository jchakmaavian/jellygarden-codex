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
        private const float RowHeight = 132f;
        private const float RowGap = 12f;

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
            public Text Label;
            public Text Cost;
            public Button Repair;
            public Image Tick;
        }

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
            RectTransform band = StoryUi.CreateRect("Panel", _canvas.transform);
            band.anchorMin = new Vector2(0f, 0f);
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
            if (_rows.Count == area.Tasks.Count) return;

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

                var row = new TaskRow { TaskIndex = task.Index };

                Image card = StoryUi.CreamCard("Card", rowRect, 26);
                StoryUi.Stretch(card.rectTransform);

                row.Label = StoryUi.CreateText("Label", rowRect, task.Label, 34,
                    StoryUi.RoyalNavy);
                row.Label.rectTransform.anchorMin = new Vector2(0f, 0f);
                row.Label.rectTransform.anchorMax = new Vector2(1f, 1f);
                row.Label.rectTransform.offsetMin = new Vector2(16f, 0f);
                row.Label.rectTransform.offsetMax = new Vector2(-320f, 0f);

                row.Cost = StoryUi.CreateText("Cost", rowRect,
                    task.Stars == 1 ? "1★" : task.Stars + "★", 36, StoryUi.RoyalGoldDeep,
                    TextAnchor.MiddleRight);
                row.Cost.rectTransform.anchorMin = new Vector2(1f, 0f);
                row.Cost.rectTransform.anchorMax = new Vector2(1f, 1f);
                row.Cost.rectTransform.pivot = new Vector2(1f, 0.5f);
                row.Cost.rectTransform.offsetMin = new Vector2(-300f, 0f);
                row.Cost.rectTransform.offsetMax = new Vector2(-190f, 0f);

                int captured = task.Index;
                row.Repair = StoryUi.CreateButton("Repair", rowRect, "Repair", 30,
                    () => OnRepair(captured));
                RectTransform repairRect = row.Repair.GetComponent<RectTransform>();
                repairRect.anchorMin = new Vector2(1f, 0.5f);
                repairRect.anchorMax = new Vector2(1f, 0.5f);
                repairRect.pivot = new Vector2(1f, 0.5f);
                repairRect.anchoredPosition = new Vector2(-16f, 0f);
                repairRect.sizeDelta = new Vector2(170f, 96f);

                row.Tick = StoryUi.CreateImage("Done", rowRect, StoryUi.StarSprite, StoryUi.Gold);
                row.Tick.rectTransform.anchorMin = new Vector2(1f, 0.5f);
                row.Tick.rectTransform.anchorMax = new Vector2(1f, 0.5f);
                row.Tick.rectTransform.pivot = new Vector2(1f, 0.5f);
                row.Tick.rectTransform.anchoredPosition = new Vector2(-60f, 0f);
                row.Tick.rectTransform.sizeDelta = new Vector2(64f, 64f);

                _rows.Add(row);
            }

            _taskList.sizeDelta = new Vector2(0f,
                area.Tasks.Count * (RowHeight + RowGap) + RowGap);
        }

        private void RefreshRows(StoryArea area)
        {
            // Only the next unfinished task offers its button, so the player is
            // repairing the room in the order the story wrote it.
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
                row.Label.fontStyle = done ? FontStyle.Italic : FontStyle.Normal;
                row.Label.text = task.Label;

                row.Cost.gameObject.SetActive(!done);
                row.Tick.gameObject.SetActive(done);

                row.Repair.gameObject.SetActive(!done && isNext);
                StoryUi.SetInteractable(row.Repair, affordable);
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
