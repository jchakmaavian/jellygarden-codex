// The in-level HUD, the way Royal Match draws it: Target, a portrait, Moves.
//
// The kit's HUD is a score bar with three star markers along it, a score
// counter and a move counter tucked underneath. Royal Match has none of that —
// see docs/royal-match/11-level-hud.jpg. A level states what you must collect
// and how many moves you have left, and the score is not shown at all, because
// there is no score. Three panels across one blue strip:
//
//     [ Target ]        (portrait)        [ Moves ]
//       icons+counts       King Leo          35
//
// Every metric below was measured off that capture (828x1472) and scaled onto
// the canvas the strip is built in. Horizontal positions are normalised anchors
// so they hold at any aspect; vertical ones are pixels scaled by the host
// canvas's own reference width, since the kit's canvas matches width.
//
// What is NOT done here, and why:
//
//   The kit's target icons are not destroyed or deactivated. LevelManager's
//   collect animation flies a matched piece to `TargetObject.guiObj`, the icon
//   TargetsGroup made — and TargetsGroup destroys those icons in OnDisable, so
//   deactivating the group would leave the board animating to a dead transform.
//   They are made transparent instead and moved onto this HUD's own slots, so
//   the kit's animation still lands on the icon the player is reading.
//
//   The score bar and its star markers are the chrome nothing else references,
//   so those objects are deactivated — hidden, never deleted. Turning
//   StoryLevelHud.Enabled off restores the kit's HUD exactly.

using System;
using System.Collections.Generic;
using JellyGarden.Scripts.GUI;
using JellyGarden.Scripts.Targets;
using UnityEngine;
using UnityEngine.UI;

namespace JellyGarden.Scripts.Story
{
    [DisallowMultipleComponent]
    public sealed class StoryLevelHud : MonoBehaviour
    {
        /// <summary>The whole feature, in one switch.</summary>
        public static bool Enabled = true;

        public const string HostName = "StoryLevelHud";

        public static StoryLevelHud Instance { get; private set; }

        #region What the HUD shows

        /// <summary>One target: its icon and how many are still owed.</summary>
        public sealed class HudTarget
        {
            public Sprite Icon;
            public int Remaining;
            public bool Done;
        }

        /// <summary>
        /// Everything on the strip, in the form the HUD draws it. The board fills
        /// this in through <see cref="ReadLevelManager"/>; a test fills it in by
        /// hand through <see cref="Source"/>, which is the only way to drive the
        /// HUD with no board in the scene.
        /// </summary>
        public sealed class HudModel
        {
            public string LimitTitle = "Moves";
            public string LimitValue = "0";
            public readonly List<HudTarget> Targets = new List<HudTarget>();
        }

        /// <summary>Test seam. Null means "read the live board".</summary>
        public static Func<HudModel> Source;

        #endregion

        #region Metrics, measured off docs/royal-match/11-level-hud.jpg

        // The capture is 828x1472 and the reference canvas is 1080x1920, so every
        // pixel measured off the JPG is multiplied by 1080/828 = 1.304 here.
        private const float DesignWidth = 1080f;

        private const float StripHeight = 360f;   // blue body
        private const float StripBand = 15f;      // the gold band under it
        private const float PlateTop = 29f;
        private const float PlateHeight = 66f;
        private const float CardTop = 110f;
        private const float CardHeight = 226f;

        // x in fractions of the screen width: left panel, mirrored on the right.
        private const float TargetPlateMin = 0.0205f;
        private const float TargetPlateMax = 0.3213f;
        private const float TargetCardMin = 0.0362f;
        private const float TargetCardMax = 0.3043f;

        private const float PortraitMin = 0.3442f;
        private const float PortraitMax = 0.6594f;
        private const float PortraitTop = 21f;
        private const float PortraitHeight = 330f;

        private const int MaxTargets = 4;

        /// <summary>The portrait in the middle of the strip. King Leo's game.</summary>
        public const string PortraitCharacter = "leo";

        #endregion

        #region Parts

        private RectTransform _root;
        private RectTransform _targetRow;
        private Text _targetTitle;
        private Text _limitTitle;
        private Text _limitValue;
        private Image _portrait;

        private sealed class Slot
        {
            public RectTransform Root;
            public Image Icon;
            public Text Count;
        }

        private readonly List<Slot> _slots = new List<Slot>();

        /// <summary>The kit's HUD strip, kept so nothing here has to find it twice.</summary>
        private Transform _kitStrip;

        private float _scale = 1f;
        private float _nextRefresh;
        private int _visibleSlots;

        /// <summary>How many target slots are currently drawn. Read by tests.</summary>
        public int VisibleTargetCount { get { return _visibleSlots; } }

        /// <summary>The Moves (or Time) number as the player reads it.</summary>
        public string LimitText { get { return _limitValue == null ? null : _limitValue.text; } }

        /// <summary>The Moves (or Time) heading.</summary>
        public string LimitTitleText { get { return _limitTitle == null ? null : _limitTitle.text; } }

        #endregion

        #region Install

        private static bool _hooked;

        /// <summary>
        /// Called by StorySceneHooks on every scene load.
        ///
        /// The board is not there yet when a scene finishes loading —
        /// LevelManager sets THIS in Start, and the level UI is inactive until a
        /// level is opened, so nothing to attach to exists at that moment. The
        /// kit already raises an event for exactly this, so the HUD attaches on
        /// the level being loaded and on play starting, both of which are
        /// idempotent.
        /// </summary>
        public static void Install()
        {
            if (!Enabled) return;

            if (!_hooked)
            {
                _hooked = true;
                LevelManager.OnLevelLoaded += Attach;
                LevelManager.OnStartPlay += Attach;
            }

            Attach();
        }

        /// <summary>Builds the HUD over the kit's, once per board. Cheap to repeat.</summary>
        private static void Attach()
        {
            if (!Enabled) return;
            if (Instance != null) return;

            Transform strip = FindKitHudStrip();
            // No strip yet: the level UI is still inactive, and the next
            // OnLevelLoaded will come back through here.
            if (strip == null) return;

            // The HUD lives inside the kit's level canvas rather than on a canvas
            // of its own, so it inherits the kit's scaling, its sort order and —
            // the part that matters — its visibility. It is the sibling before
            // the strip, so anything left visible in the strip stays on top.
            Transform parent = strip.parent;
            StoryLevelHud hud = Build(parent, ScaleOf(parent));
            hud.transform.SetSiblingIndex(strip.GetSiblingIndex());
            hud._kitStrip = strip;

            hud.HideKitHud(strip);
            hud.MovePauseOutOfTheHudsWay(strip);
            hud.Refresh();

            Debug.Log("[Story] Level HUD installed: Target + portrait + Moves, no score bar.");
        }

        /// <summary>
        /// Builds the strip and nothing else — no board, no kit HUD, no scene
        /// assumptions. This is what an editor test drives.
        /// </summary>
        public static StoryLevelHud Build(Transform parent, float scale)
        {
            RectTransform root = StoryUi.CreateRect(HostName, parent);
            StoryUi.Stretch(root);

            var hud = root.gameObject.AddComponent<StoryLevelHud>();
            hud._root = root;
            hud._scale = scale <= 0f ? 1f : scale;
            hud.BuildStrip();

            Instance = hud;
            return hud;
        }

        /// <summary>
        /// The canvas the HUD is being built into may be authored at any
        /// reference width; the metrics above are in 1080. One number reconciles
        /// them, because the kit's canvas scales on width.
        /// </summary>
        private static float ScaleOf(Transform parent)
        {
            if (parent == null) return 1f;
            var scaler = parent.GetComponentInParent<CanvasScaler>();
            if (scaler == null || scaler.referenceResolution.x <= 0f) return 1f;
            return scaler.referenceResolution.x / DesignWidth;
        }

        /// <summary>
        /// The kit's HUD strip, found from the kit's own components rather than a
        /// scene path: the progress bar is inside it, and the pause button is the
        /// one child name the strip is known by.
        /// </summary>
        private static Transform FindKitHudStrip()
        {
            ProgressBarScript bar = ProgressBarScript.Instance;
            for (Transform t = bar == null ? null : bar.transform; t != null; t = t.parent)
            {
                if (t.Find("Pause") != null) return t;
            }

            GameObject byPath = GameObject.Find("Level/Canvas/Panel/Panel");
            return byPath == null ? null : byPath.transform;
        }

        #endregion

        #region Building the strip

        private void BuildStrip()
        {
            Image body = StoryUi.CreateImage("Strip", _root, null, StoryUi.RoyalBlue);
            Slab(body.rectTransform, 0f, 1f, 0f, StripHeight);
            body.raycastTarget = true;   // the strip eats taps meant for the board

            Image bandDeep = StoryUi.CreateImage("StripBand", _root, null, StoryUi.RoyalGoldDeep);
            Slab(bandDeep.rectTransform, 0f, 1f, StripHeight, StripBand);
            Image bandLit = StoryUi.CreateImage("StripBandLit", _root, null, StoryUi.RoyalGold);
            Slab(bandLit.rectTransform, 0f, 1f, StripHeight, StripBand * 0.45f);

            BuildTargetPanel();
            BuildPortrait();
            BuildLimitPanel();
        }

        private void BuildTargetPanel()
        {
            _targetTitle = Plate("TargetPlate", TargetPlateMin, TargetPlateMax, "Target");

            Image card = StoryUi.CreamCard("TargetCard", _root, 26);
            Slab(card.rectTransform, TargetCardMin, TargetCardMax, CardTop, CardHeight);

            _targetRow = StoryUi.CreateRect("Targets", card.rectTransform);
            StoryUi.Stretch(_targetRow, 12f * _scale);
        }

        private void BuildLimitPanel()
        {
            // Mirrored: the right panel is the left one reflected about the
            // centre line, which is how the capture measures.
            _limitTitle = Plate("MovesPlate", 1f - TargetPlateMax, 1f - TargetPlateMin, "Moves");

            Image card = StoryUi.CreamCard("MovesCard", _root, 26);
            Slab(card.rectTransform, 1f - TargetCardMax, 1f - TargetCardMin, CardTop, CardHeight);

            _limitValue = StoryUi.CreateText("MovesValue", card.rectTransform, "0",
                Mathf.RoundToInt(150f * _scale), StoryUi.RoyalNavy, TextAnchor.MiddleCenter);
            StoryUi.Stretch(_limitValue.rectTransform, 8f * _scale);
            _limitValue.fontStyle = FontStyle.Bold;
        }

        /// <summary>A title plate: gold rim, dark blue slab, cream text.</summary>
        private Text Plate(string name, float xMin, float xMax, string label)
        {
            RectTransform plate = StoryUi.CreateRect(name, _root);
            Slab(plate, xMin, xMax, PlateTop, PlateHeight);

            Image rim = StoryUi.CreateImage("Rim", plate, StoryUi.RoundedSprite(30),
                StoryUi.RoyalGoldDeep);
            StoryUi.Stretch(rim.rectTransform);
            rim.type = Image.Type.Sliced;

            Image fill = StoryUi.CreateImage("Fill", plate, StoryUi.RoundedSprite(30),
                StoryUi.RoyalBlueDark);
            StoryUi.Stretch(fill.rectTransform, 7f * _scale);
            fill.type = Image.Type.Sliced;

            Text text = StoryUi.CreateText("Label", plate, label,
                Mathf.RoundToInt(56f * _scale), StoryUi.RoyalTitle, TextAnchor.MiddleCenter);
            StoryUi.Stretch(text.rectTransform, 10f * _scale);
            text.fontStyle = FontStyle.Bold;
            return text;
        }

        private void BuildPortrait()
        {
            RectTransform frame = StoryUi.CreateRect("Portrait", _root);
            Slab(frame, PortraitMin, PortraitMax, PortraitTop, PortraitHeight);

            Image rim = StoryUi.CreateImage("Rim", frame, StoryUi.RoundedSprite(40),
                StoryUi.RoyalGoldDeep);
            StoryUi.Stretch(rim.rectTransform);
            rim.type = Image.Type.Sliced;

            Image lit = StoryUi.CreateImage("RimLit", frame, StoryUi.RoundedSprite(40),
                StoryUi.RoyalGold);
            StoryUi.Stretch(lit.rectTransform, 6f * _scale);
            lit.type = Image.Type.Sliced;

            // The mask carries the same rounded corners, so a rectangular
            // portrait sits inside the frame rather than over its rim.
            Image window = StoryUi.CreateImage("Window", frame, StoryUi.RoundedSprite(40),
                StoryUi.RoyalBlueDark);
            StoryUi.Stretch(window.rectTransform, 16f * _scale);
            window.type = Image.Type.Sliced;
            window.gameObject.AddComponent<Mask>().showMaskGraphic = true;

            _portrait = StoryUi.CreateImage("Face", window.rectTransform,
                StoryArt.Portrait(PortraitCharacter), Color.white);
            StoryUi.Stretch(_portrait.rectTransform);
            _portrait.preserveAspect = true;
            // No portrait art is not a reason to draw a blue hole with nothing
            // in it, so the slot simply stays empty and the frame still reads.
            _portrait.enabled = _portrait.sprite != null;
        }

        /// <summary>
        /// Places a rect by fractions of the width and pixels from the top. The
        /// kit's canvas scales on width, so a width fraction is exact at any
        /// aspect and a vertical pixel only needs the one scale factor.
        /// </summary>
        private void Slab(RectTransform rect, float xMin, float xMax, float top, float height)
        {
            rect.anchorMin = new Vector2(xMin, 1f);
            rect.anchorMax = new Vector2(xMax, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(0f, -(top + height) * _scale);
            rect.offsetMax = new Vector2(0f, -top * _scale);
        }

        #endregion

        #region Filling it in

        // Reading the board allocates a model, so the strip is pulled ten times a
        // second rather than every frame. A move takes far longer than that to
        // play out, so the counter still changes on the move that spends it.
        private const float RefreshInterval = 0.1f;

        private void Update()
        {
            if (Time.unscaledTime < _nextRefresh) return;
            _nextRefresh = Time.unscaledTime + RefreshInterval;
            Refresh();
        }

        private void LateUpdate()
        {
            MirrorKitVisibility();
            AimKitIconsAtThisHud();
        }

        /// <summary>Pulls the live values and writes only what changed.</summary>
        public void Refresh()
        {
            HudModel model = Source != null ? Source() : ReadLevelManager();
            if (model == null) return;

            SetText(_limitTitle, model.LimitTitle);
            SetText(_limitValue, model.LimitValue);
            SyncTargets(model.Targets);
        }

        private static void SetText(Text text, string value)
        {
            if (text == null || value == null) return;
            if (text.text != value) text.text = value;
        }

        /// <summary>What the board says, in the form the strip draws.</summary>
        public static HudModel ReadLevelManager()
        {
            LevelManager manager = LevelManager.THIS;
            if (manager == null) return null;

            var model = new HudModel();
            bool timed = manager.limitType == LIMIT.TIME;
            int limit = Mathf.Max(0, manager.Limit);
            model.LimitTitle = timed ? "Time" : "Moves";
            model.LimitValue = timed
                ? string.Format("{0}:{1:00}", limit / 60, limit % 60)
                : limit.ToString();

            if (manager.targetObject != null)
            {
                for (int i = 0; i < manager.targetObject.Length; i++)
                {
                    TargetObject target = manager.targetObject[i];
                    if (target == null) continue;
                    model.Targets.Add(new HudTarget
                    {
                        Icon = target.icon,
                        Remaining = Mathf.Max(0, target.GetCount()),
                        Done = target.Done()
                    });
                }
            }

            return model;
        }

        private void SyncTargets(List<HudTarget> targets)
        {
            int count = Mathf.Min(targets == null ? 0 : targets.Count, MaxTargets);

            while (_slots.Count < count) _slots.Add(BuildSlot(_slots.Count));

            for (int i = 0; i < _slots.Count; i++)
            {
                bool used = i < count;
                if (_slots[i].Root.gameObject.activeSelf != used)
                    _slots[i].Root.gameObject.SetActive(used);
                if (!used) continue;

                PlaceSlot(_slots[i].Root, i, count);

                HudTarget target = targets[i];
                if (_slots[i].Icon.sprite != target.Icon)
                {
                    _slots[i].Icon.sprite = target.Icon;
                    _slots[i].Icon.enabled = target.Icon != null;
                }

                // A finished target goes green rather than gaining a tick: the
                // kit's font is not guaranteed to carry a check glyph, and a
                // missing glyph renders as nothing at all.
                SetText(_slots[i].Count, target.Remaining.ToString());
                _slots[i].Count.color = target.Done ? StoryUi.RoyalGreen : StoryUi.RoyalNavy;
            }

            _visibleSlots = count;
        }

        private Slot BuildSlot(int index)
        {
            RectTransform root = StoryUi.CreateRect("Target" + index, _targetRow);

            var slot = new Slot { Root = root };

            slot.Icon = StoryUi.CreateImage("Icon", root, null, Color.white);
            slot.Icon.preserveAspect = true;
            RectTransform icon = slot.Icon.rectTransform;
            icon.anchorMin = new Vector2(0f, 0.34f);
            icon.anchorMax = new Vector2(1f, 1f);
            icon.offsetMin = Vector2.zero;
            icon.offsetMax = Vector2.zero;

            slot.Count = StoryUi.CreateText("Count", root, "0",
                Mathf.RoundToInt(52f * _scale), StoryUi.RoyalNavy, TextAnchor.UpperCenter);
            RectTransform count = slot.Count.rectTransform;
            count.anchorMin = new Vector2(0f, 0f);
            count.anchorMax = new Vector2(1f, 0.34f);
            count.offsetMin = Vector2.zero;
            count.offsetMax = Vector2.zero;
            slot.Count.fontStyle = FontStyle.Bold;

            return slot;
        }

        private static void PlaceSlot(RectTransform slot, int index, int count)
        {
            float share = 1f / Mathf.Max(1, count);
            slot.anchorMin = new Vector2(index * share, 0f);
            slot.anchorMax = new Vector2((index + 1) * share, 1f);
            slot.offsetMin = Vector2.zero;
            slot.offsetMax = Vector2.zero;
        }

        #endregion

        #region Standing in for the kit's HUD

        /// <summary>
        /// Hides the kit's score bar, its star markers and its counters, and
        /// makes its target icons transparent without deactivating them.
        ///
        /// Nothing is destroyed: every object here is still in the scene, and
        /// StoryLevelHud.Enabled = false leaves all of it alone.
        /// </summary>
        private void HideKitHud(Transform strip)
        {
            var hidden = new List<string>();
            var faded = new List<string>();

            foreach (Transform child in strip)
            {
                if (child.name == "Pause") continue;

                // TargetsGroup destroys the icons it made in OnDisable, and
                // LevelManager flies collected pieces to those icons — so the
                // group has to stay enabled. Alpha 0 hides it just as well.
                if (child.GetComponentInChildren<TargetsGroup>(true) != null)
                {
                    Fade(child);
                    faded.Add(child.name);
                    continue;
                }

                child.gameObject.SetActive(false);
                hidden.Add(child.name);
            }

            // TargetIngr and TargetScore are the kit's static target slots; they
            // are chrome, so they went out with the rest above.
            Debug.Log("[Story] Level HUD: hid " + string.Join(", ", hidden.ToArray()) +
                      "; faded " + string.Join(", ", faded.ToArray()) +
                      " (kept alive for the collect animation).");
        }

        private static void Fade(Transform target)
        {
            var group = target.GetComponent<CanvasGroup>();
            if (group == null) group = target.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
        }

        /// <summary>
        /// The kit's pause button sat inside the strip this HUD now covers, so
        /// it moves to the empty middle slot of the kit's booster row — which is
        /// where Royal Match keeps its settings button, and the one place on the
        /// screen that draws above this HUD and holds nothing a player needs.
        ///
        /// Moved, not replaced: it is still the kit's button running the kit's
        /// action, and if the booster row is not there it stays where it was
        /// rather than being hidden.
        /// </summary>
        private void MovePauseOutOfTheHudsWay(Transform strip)
        {
            var pause = strip.Find("Pause") as RectTransform;
            if (pause == null) return;

            RectTransform row = FindBoosterRow(strip);
            if (row == null) return;

            // The row's own boosters say what height the row sits at, so nothing
            // here has to guess at the kit's layout.
            float y = 0f;
            for (int i = 0; i < row.childCount; i++)
            {
                var child = row.GetChild(i) as RectTransform;
                if (child == null || !child.name.StartsWith("Boost")) continue;
                y = child.anchoredPosition.y;
                break;
            }

            pause.SetParent(row, false);
            pause.SetAsLastSibling();
            pause.anchorMin = new Vector2(0.5f, 0.5f);
            pause.anchorMax = new Vector2(0.5f, 0.5f);
            pause.pivot = new Vector2(0.5f, 0.5f);
            pause.anchoredPosition = new Vector2(0f, y);
        }

        /// <summary>The kit's booster row, found from the boosters in it.</summary>
        private static RectTransform FindBoosterRow(Transform strip)
        {
            Transform canvas = strip.parent == null ? null : strip.parent.parent;
            if (canvas == null) return null;

            Transform bottom = canvas.Find("BottomPanel");
            if (bottom == null) return null;

            foreach (Transform child in bottom)
            {
                for (int i = 0; i < child.childCount; i++)
                {
                    if (child.GetChild(i).name.StartsWith("Boost")) return child as RectTransform;
                }
            }
            return bottom as RectTransform;
        }

        /// <summary>
        /// The HUD is only ever as visible as the kit's own HUD was, so the map,
        /// the menus and the level-end popups all behave exactly as before.
        /// </summary>
        private void MirrorKitVisibility()
        {
            if (_kitStrip == null) return;
            bool shouldShow = _kitStrip.gameObject.activeInHierarchy;
            if (_root.gameObject.activeSelf != shouldShow)
                _root.gameObject.SetActive(shouldShow);
        }

        /// <summary>
        /// Keeps the kit's invisible target icons sitting on this HUD's slots, so
        /// a collected piece flies to the icon the player is actually reading.
        /// </summary>
        private void AimKitIconsAtThisHud()
        {
            if (_kitStrip == null || _visibleSlots == 0) return;

            TargetIcon[] icons = _kitStrip.GetComponentsInChildren<TargetIcon>(true);
            for (int i = 0; i < icons.Length && i < _slots.Count; i++)
            {
                if (icons[i] == null || !_slots[i].Root.gameObject.activeInHierarchy) continue;
                icons[i].transform.position = _slots[i].Root.position;
            }
        }

        #endregion

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
