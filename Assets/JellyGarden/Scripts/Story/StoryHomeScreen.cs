// Castle First: the room fills the screen while the controls float over it.
// All labels, prices and destinations follow the current area's saved progress.
using UnityEngine;
using UnityEngine.UI;

namespace JellyGarden.Scripts.Story
{
    public sealed class StoryHomeScreen : MonoBehaviour
    {
        public const string HostName = "StoryHomeScreen";
        private const int SortingOrder = 300;
        private static StoryHomeScreen _instance;
        private int _areaId;
        private Canvas _canvas;
        private RectTransform _room, _frame, _buttonBand, _navBand, _marker;
        private AreaSceneView _roomView;
        private StoryUi.TopBarParts _topBar;
        private Button _levelButton, _areaButton;
        private Text _levelLabel, _levelCaption, _areaLabel, _areaNumber, _areaTitle, _progressLabel, _costLabel;
        private Image _markerHalo, _costStar;
        private int _repairCost;
        private bool _repairReady;
        private Vector2 _repairPoint = new Vector2(0.5f, 0.4f);

        public static StoryHomeScreen Instance { get { return _instance; } }
        public static bool IsOpen { get { return _instance != null && _instance.gameObject.activeSelf; } }
        public int AreaId { get { return _areaId; } }
        public AreaSceneView Room { get { return _roomView; } }
        public RectTransform RoomRect { get { return _room; } }
        public bool RoomIsFullBleed { get { return _roomView != null && _roomView.IsCovering && _roomView.Aspect > 0f; } }
        public int VisibleRoomLayers { get { return _roomView == null ? 0 : _roomView.VisibleLayerCount; } }
        public bool HasTopBar
        {
            get { return _topBar != null && _topBar.Root.gameObject.activeInHierarchy
                && _topBar.Coins != null && _topBar.Stars != null && _topBar.Settings != null; }
        }
        public string TopBarText { get { return _topBar == null ? null : _topBar.Coins.text + "/" + _topBar.Stars.text; } }
        public RectTransform ButtonBand { get { return _buttonBand; } }
        public RectTransform NavBand { get { return _navBand; } }
        public Button LevelButton { get { return _levelButton; } }
        public Button AreaButton { get { return _areaButton; } }
        public string LevelButtonText { get { return _levelLabel == null ? null : _levelLabel.text; } }
        public string LevelCaptionText { get { return _levelCaption == null ? null : _levelCaption.text; } }
        public string AreaButtonText { get { return _areaLabel == null ? null : _areaLabel.text; } }
        public string AreaTitleText { get { return _areaTitle == null ? null : _areaTitle.text; } }
        public string RestorationText { get { return _progressLabel == null ? null : _progressLabel.text; } }
        public string AreaProgressText
        {
            get
            {
                StoryArea area = StoryDatabase.GetArea(_areaId);
                return area == null ? null : StoryProgress.TasksCompletedIn(_areaId) + "/" + area.Tasks.Count;
            }
        }
        public int RepairCost { get { return _repairCost; } }
        public bool RepairReady { get { return _repairReady; } }
        public int NextLevel { get { return StoryLevelPlan.NextLevelFor(_areaId); } }

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
                "of {5}, wallet {6}, play '{7}', repair '{8}', {9}, {10} ready",
                areaId, area.Name, StoryProgress.TasksCompletedIn(areaId), area.Tasks.Count,
                _instance.VisibleRoomLayers,
                _instance._roomView == null ? 0 : _instance._roomView.LayerCount,
                _instance.TopBarText,
                _instance.LevelButtonText, _instance.AreaButtonText,
                _instance.AreaProgressText,
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

        private void Build()
        {
            _canvas = StoryUi.CreateOverlayCanvas("HomeCanvas", SortingOrder);
            _canvas.transform.SetParent(transform, false);
            _canvas.GetComponent<CanvasScaler>().referenceResolution = new Vector2(360, 720);
            _room = StoryUi.CreateRect("Room", _canvas.transform);
            StoryUi.Stretch(_room);
            _room.gameObject.AddComponent<RectMask2D>();

            // Just enough tint at the edges for the small floating controls.
            EdgeShade("SkyShade", 0.76f, 1f, new Color(0.08f, 0.2f, 0.2f, 0.12f), Color.clear);
            EdgeShade("FloorShade", 0f, 0.28f, Color.clear, new Color(0.06f, 0.16f, 0.16f, 0.48f));
            _frame = StoryCastleFrame.Create(_canvas.transform);
            _topBar = StoryCastleUi.TopBar(_frame, OpenSettings);
            BuildTitle();
            BuildRepairMarker();
            BuildLevelButton();

            // StoryNavBar draws the actual dock on its persistent canvas.
            _navBand = StoryUi.CreateRect("NavReservation", _frame);
            StoryCastleUi.Place(_navBand, 16, 88, 328, 70, 0, 0);
        }

        private void EdgeShade(string name, float bottom, float top, Color topColor, Color bottomColor)
        {
            Image shade = StoryUi.CreateImage(name, _canvas.transform, null, Color.white);
            shade.rectTransform.anchorMin = new Vector2(0, bottom);
            shade.rectTransform.anchorMax = new Vector2(1, top);
            shade.rectTransform.offsetMin = shade.rectTransform.offsetMax = Vector2.zero;
            StoryCastleUi.Gradient(shade, topColor, bottomColor);
        }

        private void BuildTitle()
        {
            _areaNumber = StoryCastleUi.Label("AreaNumber", _frame, "", 9, StoryCastleUi.Cream);
            StoryCastleUi.Place(_areaNumber.rectTransform, 22, -98, 190, 15);
            _areaTitle = StoryCastleUi.Label("AreaTitle", _frame, "", 27, StoryCastleUi.Cream, true);
            StoryCastleUi.Place(_areaTitle.rectTransform, 22, -114, 224, 37);
            StoryUi.FitOneLine(_areaTitle, 15, 27);
            var shadow = _areaTitle.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.17f, 0.23f, 0.12f, 0.64f);
            shadow.effectDistance = new Vector2(0, -2);

            Image progressRim = StoryCastleUi.Plate("Restoration", _frame, 15,
                new Color(1f, 1f, 0.92f, 0.45f));
            StoryCastleUi.Place(progressRim.rectTransform, 251, -121, 87, 27);
            Image progress = StoryCastleUi.Plate("Glass", progressRim.transform, 14,
                new Color(0.12f, 0.22f, 0.22f, 0.65f));
            StoryUi.Stretch(progress.rectTransform, 0.7f);
            _progressLabel = StoryCastleUi.Label("Count", progress.transform, "", 10,
                StoryCastleUi.Cream, false, TextAnchor.MiddleCenter);
            StoryUi.Stretch(_progressLabel.rectTransform, 3);
            StoryUi.FitOneLine(_progressLabel, 8, 10);
        }

        private void BuildLevelButton()
        {
            _buttonBand = StoryUi.CreateRect("Play", _frame);
            StoryCastleUi.Place(_buttonBand, 48, 174, 264, 67, 0, 0);
            Image depth = StoryCastleUi.Plate("Shadow", _buttonBand, 34, new Color32(40, 73, 44, 220));
            StoryUi.Stretch(depth.rectTransform);
            depth.rectTransform.anchoredPosition = new Vector2(0, -4);
            Image rim = StoryCastleUi.Plate("GoldEdge", _buttonBand, 34, new Color32(255, 237, 181, 255));
            StoryUi.Stretch(rim.rectTransform);
            Image face = StoryCastleUi.Plate("LevelButton", rim.transform, 32, Color.white);
            StoryUi.Stretch(face.rectTransform, 2);
            StoryCastleUi.Gradient(face, new Color32(95, 168, 91, 255), new Color32(50, 120, 71, 255));
            _levelButton = StoryCastleUi.Clickable(face, OnLevel);
            Image highlight = StoryCastleUi.Plate("Highlight", face.transform, 8, Color.white);
            StoryCastleUi.Place(highlight.rectTransform, 15, -4, 112, 10);
            StoryCastleUi.Gradient(highlight, new Color(1f, 1f, 0.8f, 0.34f), Color.clear);
            _levelCaption = StoryCastleUi.Label("Caption", face.transform, "N E X T  P U Z Z L E", 8,
                Color.white, false, TextAnchor.MiddleCenter);
            StoryCastleUi.Place(_levelCaption.rectTransform, 28, -7, 194, 15);
            _levelLabel = StoryCastleUi.Label("Level", face.transform, "", 29,
                Color.white, true, TextAnchor.MiddleCenter);
            StoryCastleUi.Place(_levelLabel.rectTransform, 23, -20, 204, 38);
            StoryUi.FitOneLine(_levelLabel, 21, 29);
            var shadow = _levelLabel.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.1f, 0.3f, 0.17f, 0.5f);
            shadow.effectDistance = new Vector2(0, -1);
            Chevron(face.transform);
        }

        private static void Chevron(Transform parent)
        {
            RectTransform arrow = StoryUi.CreateRect("Chevron", parent);
            StoryCastleUi.Place(arrow, 232, -24, 12, 16);
            for (int i = 0; i < 2; i++)
            {
                Image stroke = StoryUi.CreateImage("Stroke", arrow, null, Color.white);
                StoryCastleUi.Place(stroke.rectTransform, 4, i == 0 ? -3 : -9, 2.5f, 10);
                stroke.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                stroke.rectTransform.localRotation = Quaternion.Euler(0, 0, i == 0 ? 42 : -42);
            }
        }

        private void BuildRepairMarker()
        {
            _marker = StoryUi.CreateRect("RepairMarker", _frame);
            _marker.anchorMin = _marker.anchorMax = _marker.pivot = new Vector2(0.5f, 0.5f);
            _marker.sizeDelta = new Vector2(190, 44);
            _markerHalo = StoryCastleUi.Plate("Halo", _marker, 28, new Color(1f, 0.97f, 0.65f, 0.19f));
            StoryUi.Stretch(_markerHalo.rectTransform, -6);
            Image rim = StoryCastleUi.Plate("RepairButton", _marker, 22, StoryCastleUi.Gold);
            StoryUi.Stretch(rim.rectTransform);
            var shadow = rim.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.24f, 0.19f, 0.08f, 0.3f);
            shadow.effectDistance = new Vector2(0, -3);
            _areaButton = StoryCastleUi.Clickable(rim, OnArea);
            Image face = StoryCastleUi.Plate("Cream", rim.transform, 20, StoryCastleUi.Cream);
            StoryUi.Stretch(face.rectTransform, 2);
            StoryCastleUi.Hammer(rim.transform);
            _areaLabel = StoryCastleUi.Label("NextRepair", rim.transform, "", 12, StoryCastleUi.Ink, true);
            _costStar = StoryCastleUi.Icon("Star", rim.transform, "icon_star");
            _costLabel = StoryCastleUi.Label("Cost", rim.transform, "", 13, StoryCastleUi.Ink, true);
        }

        private void OnLevel()
        {
            int areaId = _areaId;
            int levelNumber = StoryLevelPlan.NextLevelFor(areaId);
            if (levelNumber <= 0) return;
            StoryLevelPopup.Open(levelNumber, areaId, () => Open(areaId));
        }

        private void OnArea()
        {
            // Opening the repair sheet never spends a star. AreaScreen owns the
            // affordable, ordered purchase and its existing reveal animation.
            AreaScreen.Open(_areaId);
        }

        public void Refresh()
        {
            StoryArea area = StoryDatabase.GetArea(_areaId);
            if (area == null) return;
            if (_roomView == null) _roomView = AreaSceneView.Attach(_room, _areaId, true);
            else _roomView.Show(_areaId);
            _roomView.SetNeglectStrength(0.3f);
            StoryUi.RefreshTopBar(_topBar);
            // Home can remain underneath an area page that changes CurrentArea.
            _topBar.Stars.text = StoryProgress.StarsIn(_areaId).ToString();
            _areaNumber.text = "A R E A  " + _areaId.ToString("00");
            _areaTitle.text = area.Name;
            _progressLabel.text = StoryProgress.TasksCompletedIn(_areaId) + " / " + area.Tasks.Count + " restored";
            int next = NextLevel;
            _levelLabel.text = next > 0 ? "Level " + next : "All played";
            _levelCaption.text = next <= 0 ? "A R E A  L E V E L S"
                : StoryProgress.IsLevelBeaten(next) ? "P L A Y  A G A I N" : "N E X T  P U Z Z L E";
            _levelButton.interactable = next > 0;
            RefreshRepair();
            _frame.GetComponent<StoryCastleFrame>().Fit();
            PositionMarker();
        }

        private void RefreshRepair()
        {
            StoryTask task = StoryProgress.NextTask(_areaId);
            _repairCost = task == null ? 0 : task.Stars;
            _repairReady = task != null && StoryProgress.StarsIn(_areaId) >= task.Stars;
            _areaLabel.text = task == null ? "Area restored" : task.Label;
            _costLabel.text = task == null ? "" : task.Stars.ToString();
            _costStar.gameObject.SetActive(task != null);
            _costLabel.gameObject.SetActive(task != null);

            // Keep authored task names intact, growing to two lines only when
            // the name will not fit a compact pill at a readable size.
            _areaLabel.resizeTextForBestFit = false;
            _areaLabel.fontSize = 12;
            float textWidth = _areaLabel.preferredWidth;
            float trailing = task == null ? 14 : 50;
            float width = Mathf.Clamp(40 + textWidth + trailing, 166, 298);
            float labelWidth = width - 40 - trailing;
            bool wraps = textWidth > labelWidth + 0.5f;
            float height = wraps ? 56 : 44;
            _marker.sizeDelta = new Vector2(width, height);
            _areaLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            _areaLabel.verticalOverflow = VerticalWrapMode.Truncate;
            _areaLabel.resizeTextForBestFit = true;
            _areaLabel.resizeTextMinSize = 10;
            _areaLabel.resizeTextMaxSize = 12;
            StoryCastleUi.Place(_areaLabel.rectTransform, 40, -5, labelWidth, height - 10);
            StoryCastleUi.Place(_costStar.rectTransform, width - 45, -(height - 22) / 2, 22, 22);
            StoryCastleUi.Place(_costLabel.rectTransform, width - 22, -(height - 24) / 2, 19, 24);
            StoryUi.FitOneLine(_costLabel, 9, 13);
            _repairPoint = new Vector2(0.5f, 0.4f);
            if (task != null && StoryRepairRegions.IsKnown(_areaId, task.Index))
                _repairPoint = StoryRepairRegions.For(_areaId, task.Index).center;
        }

        private void LateUpdate() { PositionMarker(); }

        private void PositionMarker()
        {
            if (_marker == null || _roomView == null || _roomView.Root == null) return;
            Rect room = _roomView.Root.rect;
            Vector3 world = _roomView.Root.TransformPoint(new Vector3(
                room.xMin + room.width * _repairPoint.x, room.yMin + room.height * _repairPoint.y, 0));
            Vector3 point = _frame.InverseTransformPoint(world);
            Rect bounds = _frame.rect;
            float half = _marker.rect.width / 2;
            point.x = Mathf.Clamp(point.x, bounds.xMin + half + 20, bounds.xMax - half - 20);
            point.y = Mathf.Clamp(point.y, bounds.yMin + 235, bounds.yMax - 205);
            // Unscaled time keeps the small idle animation alive in the menu.
            float wave = Mathf.Sin(StoryClock.Now * Mathf.PI * 2f / 3f);
            point.y += _repairReady ? 2.5f * wave : 0f;
            _marker.anchoredPosition = new Vector2(point.x, point.y);
            _markerHalo.color = new Color(1f, 0.97f, 0.65f, _repairReady ? 0.18f + 0.05f * wave : 0.06f);
            _costStar.rectTransform.localScale = Vector3.one * (_repairReady ? 1f + 0.045f * wave : 1f);
        }
    }
}
