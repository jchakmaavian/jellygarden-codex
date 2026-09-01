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
        private const float PanelWidth = 940f;
        private const float RowHeight = 132f;
        private const float RowGap = 12f;

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
        private Text _title;
        private Text _chapter;
        private Text _goal;
        private Text _starCount;
        private Button _playButton;
        private Text _playLabel;
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

            // The room itself, behind everything: the player watches it change as
            // they pay for repairs, which is the whole point of the screen.
            _scene = StoryUi.Stretch(StoryUi.CreateRect("Scene", _canvas.transform));

            StoryUi.CreateFullScreenButton("Scrim", _canvas.transform, StoryUi.Dim, null);

            RectTransform panel = StoryUi.CreateRect("Panel", _canvas.transform);
            panel.anchorMin = new Vector2(0.5f, 0.5f);
            panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(PanelWidth, 1560f);

            var panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.sprite = StoryUi.PanelSprite;
            panelImage.color = panelImage.sprite != null ? Color.white : StoryUi.Parchment;
            panelImage.type = Image.Type.Simple;

            _chapter = StoryUi.CreateText("Chapter", panel, "", 34, StoryUi.Muted,
                TextAnchor.UpperCenter);
            StoryUi.TopAnchored(_chapter.rectTransform, 46f, 46f, 60f);

            _title = StoryUi.CreateText("Title", panel, "", 56, StoryUi.Ink,
                TextAnchor.UpperCenter);
            StoryUi.TopAnchored(_title.rectTransform, 96f, 76f, 60f);

            _goal = StoryUi.CreateText("Goal", panel, "", 34, StoryUi.Ink,
                TextAnchor.UpperCenter);
            StoryUi.TopAnchored(_goal.rectTransform, 180f, 130f, 70f);

            BuildStarCounter(panel);
            BuildTaskList(panel);
            BuildFooter(panel);

            _toast = StoryUi.CreateText("Toast", _canvas.transform, "", 36, Color.white,
                TextAnchor.MiddleCenter);
            StoryUi.BottomAnchored(_toast.rectTransform, 260f, 90f, 60f);
            _toast.gameObject.SetActive(false);
        }

        private void BuildStarCounter(RectTransform panel)
        {
            RectTransform holder = StoryUi.CreateRect("Stars", panel);
            StoryUi.TopAnchored(holder, 320f, 76f, 70f);

            Image star = StoryUi.CreateImage("Star", holder, StoryUi.StarSprite,
                StoryUi.StarSprite != null ? Color.white : StoryUi.Gold);
            star.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            star.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            star.rectTransform.pivot = new Vector2(0f, 0.5f);
            star.rectTransform.sizeDelta = new Vector2(64f, 64f);

            _starCount = StoryUi.CreateText("Count", holder, "", 40, StoryUi.Ink);
            _starCount.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            _starCount.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            _starCount.rectTransform.pivot = new Vector2(0f, 0.5f);
            _starCount.rectTransform.offsetMin = new Vector2(78f, -38f);
            _starCount.rectTransform.offsetMax = new Vector2(0f, 38f);
        }

        private void BuildTaskList(RectTransform panel)
        {
            RectTransform viewport = StoryUi.CreateRect("TaskViewport", panel);
            viewport.anchorMin = new Vector2(0f, 0f);
            viewport.anchorMax = new Vector2(1f, 1f);
            viewport.offsetMin = new Vector2(60f, 260f);
            viewport.offsetMax = new Vector2(-60f, -410f);

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

        private void BuildFooter(RectTransform panel)
        {
            _playButton = StoryUi.CreateButton("Play", panel, "Play", 46, OnPlay);
            StoryUi.BottomAnchored(_playButton.GetComponent<RectTransform>(), 70f, 150f, 200f);
            _playLabel = _playButton.GetComponentInChildren<Text>();

            Button back = StoryUi.CreateButton("Back", panel, "Map", 34, OnBack);
            RectTransform backRect = back.GetComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0f, 0f);
            backRect.anchorMax = new Vector2(0f, 0f);
            backRect.pivot = new Vector2(0f, 0f);
            backRect.anchoredPosition = new Vector2(40f, 80f);
            backRect.sizeDelta = new Vector2(150f, 110f);
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

            StoryArcInfo arc = StoryDatabase.ArcOf(_areaId);
            _chapter.text = arc != null
                ? string.Format("Arc {0} — {1}", arc.Arc, arc.Title)
                : string.Empty;
            _title.text = string.Format("Area {0:000} — {1}", area.Id, area.Name);
            _goal.text = area.Goal;

            int stars = StoryProgress.StarsIn(_areaId);
            _starCount.text = stars == 1 ? "1 star" : stars + " stars";

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

                row.Label = StoryUi.CreateText("Label", rowRect, task.Label, 34, StoryUi.Ink);
                row.Label.rectTransform.anchorMin = new Vector2(0f, 0f);
                row.Label.rectTransform.anchorMax = new Vector2(1f, 1f);
                row.Label.rectTransform.offsetMin = new Vector2(16f, 0f);
                row.Label.rectTransform.offsetMax = new Vector2(-320f, 0f);

                row.Cost = StoryUi.CreateText("Cost", rowRect,
                    task.Stars == 1 ? "1★" : task.Stars + "★", 36, StoryUi.Gold,
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

                row.Label.color = done ? StoryUi.Muted : StoryUi.Ink;
                row.Label.text = done ? "<s>" + task.Label + "</s>" : task.Label;

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

            StoryUi.SetInteractable(_playButton, !complete && next > 0);
            if (_playLabel != null)
                _playLabel.text = complete ? "Restored" : "Play level " + next;
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

            Close();
            StoryFlow.LaunchLevel(levelNumber, _areaId);
        }

        private void OnRepair(int taskIndex)
        {
            StoryArea area = StoryDatabase.GetArea(_areaId);
            StoryTask task = StoryProgress.FindTask(area, taskIndex);
            if (task == null) return;

            if (!StoryProgress.CanCompleteTask(_areaId, taskIndex))
            {
                int short_ = task.Stars - StoryProgress.StarsIn(_areaId);
                Toast(short_ == 1
                    ? "One more star needed."
                    : short_ + " more stars needed.");
                return;
            }

            StoryRepair.Perform(_areaId, taskIndex, Refresh);
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
