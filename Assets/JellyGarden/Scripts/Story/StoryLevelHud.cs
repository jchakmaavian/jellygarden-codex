// The in-level HUD, the way Royal Match draws it: Target, a portrait, Moves.
//
// The kit's HUD is a score bar with three star markers along it, a score
// counter and a move counter tucked underneath. Royal Match has none of that —
// see docs/royal-match/11-level-hud.jpg. A level states what you must collect
// and how many moves you have left. Three panels across one blue strip:
//
//     [ Target ]        (portrait)        [ Moves ]
//       icons+counts       King Leo          35
//
// The kit still has a score, and every level it ships carries a Target.SCORE
// entry that CheckWinLose insists on, so the score is a real objective and not
// chrome. It is drawn as one more target — icon plus "have/need" against the
// level's first star mark — rather than as the bar that used to carry it. On a
// SCORE-mode level (176 of the 1209) that is the whole objective, which is why
// the panel must state it: with the bar gone there is nothing else to read.
// The strip still never uses the word "score".
//
// Every metric below was measured off that capture (828x1472) and scaled onto
// the canvas the strip is built in. Horizontal positions are normalised anchors
// so they hold at any aspect; vertical ones are pixels scaled by the host
// canvas's own reference width, since the kit's canvas matches width.
//
// What is NOT done here, and why:
//
//   The kit's header is DESTROYED, not hidden under this one. The score bar,
//   its star markers and the static target slots are chrome nothing else
//   references, and they go.
//
//   One object survives and is adopted into this HUD instead: the group owning
//   the kit's TargetIcons. LevelManager's collect animation flies a matched
//   piece to `TargetObject.guiObj`, one of those icons, and TargetsGroup
//   destroys the icons it made in OnDisable — so destroying the group, or even
//   disabling it, leaves the board animating to a dead transform. It is
//   reparented in and kept transparent: an anchor inside the new header rather
//   than a second header behind it.
//
//   That means StoryLevelHud.Enabled = false no longer restores the kit's HUD
//   within a session — the objects are gone until the level is reloaded.

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

            /// <summary>
            /// What the icon is drawn ON TOP OF, or null for an icon that stands
            /// on its own.
            ///
            /// The kit's block sprite is not a picture of a block: it is 55%
            /// opaque frost that `CreateSquare` parents to a square which still
            /// holds an item, so on the board it is always a gem seen through
            /// ice. Alone on the HUD's cream card it composites to a pale empty
            /// square — see <see cref="BlockBacking"/>.
            /// </summary>
            public Sprite Backing;

            public int Remaining;
            public bool Done;

            /// <summary>
            /// What the slot reads when a plain remaining count is not the whole
            /// story. A score target sets this to "have/need"; null means "draw
            /// <see cref="Remaining"/>", which is what a collect target wants.
            /// </summary>
            public string Text;

            /// <summary>True for the level's score pass mark.</summary>
            public bool IsScore;

            public string Display { get { return Text ?? Remaining.ToString(); } }

            /// <summary>
            /// What the slot reads when it SHARES the plate: always the plain
            /// number still owed, on every kind of target.
            ///
            /// "have/need" is nine characters at its longest ("1511/1511"), and
            /// the target card is 100 design units — 118 pixels at 393 — shared
            /// between up to three chips. Best-fit therefore drew the score's
            /// nine characters at font 8 beside a one-character count at font
            /// 19: measured off levels 113, 117, 120, 122, 125 and 128's own end
            /// screenshots, the score's digits were 3 to 5 pixels tall against
            /// 14, and "1002/1002" on level 122 lost its slash and read as one
            /// eight-digit number. It is not a size that can be fixed by sharing
            /// the plate out differently — nine characters and two counts do not
            /// fit at one legible size — so the string is what gives.
            ///
            /// The number owed is what every other chip already says, so a
            /// shared plate now reads one way throughout: how much is left, and
            /// green when it is none. Alone on the plate a score chip has all
            /// 118 pixels to itself, draws "0/970" at the same font 19 as any
            /// count, and keeps saying have/need — the fault is the sharing, so
            /// the seam is `count > 1`, the same one SizeCountBox and
            /// SizeIconBox turn on.
            ///
            /// 1062 of the 1208 levels carry more than one chip.
            /// </summary>
            public string DisplayShared { get { return Remaining.ToString(); } }
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

            /// <summary>
            /// The score the level must reach: LevelManager.star1, which
            /// CheckWinLose tests on every level, not just the SCORE ones.
            /// </summary>
            public int PassMark;

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

        /// <summary>How far a target icon's shadow sits below and right of it,
        /// in the same 1080-wide design units as everything else on the strip.</summary>
        private const float IconShadowStep = 6f;

        /// <summary>
        /// How much of a slot a target's NUMBER gives up on a side that has
        /// another chip beside it, in 1080-wide design units.
        ///
        /// Each number is centred in its own slot, so the only thing keeping two
        /// of them apart is whatever slack each has left over. A two-digit count
        /// has almost none: measured off level 65's own screenshot at 393 points
        /// wide, "25" and "15" sat 6 pixels apart while the digits INSIDE "25"
        /// sat 3 apart, so the plate read "2515". Level 69 ("15 35") was worse at
        /// 5 against 5, and level 78 ("20 25") the same.
        ///
        /// Two of these — one from each side of a boundary — are 26 design units,
        /// which is 9.5 points at 393 wide: about one digit's advance, measured
        /// as 11 points off the same shot. A gap a digit wide is the typographic
        /// minimum for two numbers that are not one number.
        /// </summary>
        private const float CountGutter = 13f;

        /// <summary>
        /// How much of a slot a target's ICON gives up on each side when it
        /// shares the plate, in the same 1080-wide design units.
        ///
        /// One shadow step, and that is the whole reason: the icon casts its own
        /// silhouette one step down and RIGHT (see <see cref="IconShadowStep"/>),
        /// and with the icon box the full width of its slot that silhouette
        /// lands in the only space separating it from the next chip. Measured on
        /// level 81's own screenshot at 393 points wide, its two ingredient
        /// icons sat 3 pixels apart against a shadow step of 2.2 — so the shadow
        /// closed most of the gap and the pair, both gold-rimmed jewels, read as
        /// one clump. A gutter of one step keeps an icon's shadow inside its own
        /// slot, which is the least an icon can be inset by and still be a
        /// separate object.
        ///
        /// It is deliberately NOT <see cref="CountGutter"/>. That one answers a
        /// different question — two numbers butt-joined read as one NUMBER, so
        /// it is measured against a digit's advance — and applying it here
        /// shrinks every icon on a shared plate to about the height of the digit
        /// under it.
        /// </summary>
        private const float IconGutter = IconShadowStep;

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
            public Image Shadow;
            public Image Backing;
            public Image Icon;
            public Text Count;

            /// <summary>
            /// How much of this slot's width the icon box is allowed to use, so
            /// that every chip on the plate draws its icon at one size. -1 means
            /// "not applied yet". See <see cref="SizeIconBox"/>.
            /// </summary>
            public float IconFrac = -1f;

            /// <summary>
            /// The gutter currently held clear on each side of this slot's
            /// number, in canvas units. NaN means "not applied yet".
            /// See <see cref="SizeCountBox"/>.
            /// </summary>
            public float Gutter = float.NaN;

            /// <summary>
            /// The gutter currently held clear on each side of this slot's ICON,
            /// in canvas units. NaN means "not applied yet".
            /// See <see cref="SizeIconBox"/>.
            /// </summary>
            public float IconGutter = float.NaN;
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

        /// <summary>What each drawn target slot reads, in draw order.</summary>
        public List<string> TargetTexts()
        {
            var texts = new List<string>();
            for (int i = 0; i < _visibleSlots && i < _slots.Count; i++)
                texts.Add(_slots[i].Count.text);
            return texts;
        }

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

            hud.ReplaceKitHud(strip);
            hud.MovePauseOutOfTheHudsWay(strip);
            hud.ReplaceKitLevelPlate(strip);
            hud.Refresh();

            Debug.Log("[Story] Level HUD installed: Target + portrait + Moves, no score bar.");
            hud.ReportPlacement(parent);
        }

        /// <summary>
        /// Where the strip actually landed, in screen pixels.
        ///
        /// The strip is anchored to the top of the rect it is built in, so "is
        /// there a band of bare backdrop above the HUD" is a question about that
        /// rect and not about the strip. Answering it by eye off a screenshot
        /// costs a build; this answers it from the log.
        /// </summary>
        private void ReportPlacement(Transform parent)
        {
            var parentRect = parent as RectTransform;
            if (parentRect == null || _root == null) return;

            Canvas.ForceUpdateCanvases();

            var corners = new Vector3[4];
            parentRect.GetWorldCorners(corners);
            Camera camera = CameraFor(parentRect);
            float parentTop = RectTransformUtility.WorldToScreenPoint(camera, corners[1]).y;

            _root.GetWorldCorners(corners);
            float rootTop = RectTransformUtility.WorldToScreenPoint(camera, corners[1]).y;

            Rect safe = Screen.safeArea;

            Debug.Log(string.Format(
                "[Story] HUD placement: screen {0}x{1}, safe area {2}, host '{3}' top at y={4:0}, " +
                "strip root top at y={5:0} — {6:0}px of screen above the strip.",
                Screen.width, Screen.height, safe, parent.name,
                parentTop, rootTop, Screen.height - rootTop));

        }

        private static Camera CameraFor(RectTransform rect)
        {
            Canvas canvas = rect.GetComponentInParent<Canvas>();
            if (canvas == null) return null;
            canvas = canvas.rootCanvas;
            return canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
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

            // A moves value is one or two characters and a TIME value is four —
            // "0:59" — and the card is sized for the short one. Left wrapping,
            // Unity broke the line rather than shrinking it, so every timed
            // level read "0:5" over "9": two lines of a four-character clock,
            // which is not a number a player can read at a glance. FitOneLine
            // turns wrapping off, which is what makes best-fit solve for WIDTH.
            StoryUi.FitOneLine(_limitValue,
                Mathf.Max(8, Mathf.RoundToInt(60f * _scale)),
                Mathf.Max(9, Mathf.RoundToInt(150f * _scale)));
        }

        /// <summary>
        /// A title plate: the same curved banner the popups wear, in the same
        /// three layers — blue shell, gold, dark blue field — so the word over
        /// the board and the word over a panel are the same object at different
        /// sizes. They were flat rounded rects, which was the one place left in
        /// the game still wearing the old shape.
        /// </summary>
        private Text Plate(string name, float xMin, float xMax, string label)
        {
            RectTransform plate = StoryUi.CreateRect(name, _root);
            Slab(plate, xMin, xMax, PlateTop, PlateHeight);

            Image shell = StoryUi.CreateImage("Shell", plate, StoryUi.BannerSprite,
                StoryUi.RoyalBlue);
            StoryUi.Stretch(shell.rectTransform);

            Image rim = StoryUi.CreateImage("Rim", plate, StoryUi.BannerSprite,
                StoryUi.RoyalGoldDeep);
            StoryUi.Stretch(rim.rectTransform, 5f * _scale);

            Image gold = StoryUi.CreateImage("Gold", plate, StoryUi.BannerSprite,
                StoryUi.RoyalGold);
            StoryUi.Stretch(gold.rectTransform, 8f * _scale);

            Image fill = StoryUi.CreateImage("Fill", plate, StoryUi.BannerSprite,
                StoryUi.RoyalBlueDark);
            StoryUi.Stretch(fill.rectTransform, 14f * _scale);

            Text text = StoryUi.CreateText("Label", plate, label,
                Mathf.RoundToInt(56f * _scale), StoryUi.RoyalTitle, TextAnchor.MiddleCenter);

            // The banner's field sits in the TOP of its rect — the band slides
            // down the arc — so text centred in the whole plate sits under the
            // gold. Same correction the panel title needed.
            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = new Vector2(0f, StoryUi.BannerArc);
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(14f * _scale, 2f * _scale);
            textRect.offsetMax = new Vector2(-14f * _scale, -6f * _scale);
            text.fontStyle = FontStyle.Bold;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 12;
            text.resizeTextMaxSize = Mathf.RoundToInt(56f * _scale);
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

            // Head and shoulders, not the whole standing figure: at this size a
            // full-body portrait is a brown smudge with a face two pixels wide.
            // StoryAvatar animates it where frames are authored and crops the
            // portrait where they are not, and the mask above is what makes the
            // crop possible — see the note in that file.
            _portrait = StoryAvatar.Build(PortraitCharacter, window.rectTransform);
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

            AddTargets(model, manager.targetObject, manager.star1, LevelManager.Score);
            return model;
        }

        /// <summary>
        /// A score target ALONE on the plate reads as progress towards the pass
        /// mark rather than as an amount still owed: "120/300" says both what the
        /// player has and what the level wants, in the space one count would
        /// take. The word "score" is deliberately not on the strip — Royal Match
        /// never names it, and the number under the icon says everything the
        /// player has to know.
        ///
        /// Sharing the plate is the case this string cannot serve; the chip
        /// falls back to the plain number owed there. See
        /// <see cref="HudTarget.DisplayShared"/>.
        /// </summary>
        public static string ScoreText(int have, int need)
        {
            return Mathf.Max(0, have) + "/" + Mathf.Max(0, need);
        }

        /// <summary>
        /// Turns the kit's target list into slots, in the kit's own order — the
        /// collect animation is aimed by index in <see cref="AimKitIconsAtThisHud"/>,
        /// so reordering here would fly a matched piece at the wrong icon.
        ///
        /// Every level the kit ships carries a <see cref="Target.SCORE"/> entry,
        /// and LevelManager.CheckWinLose requires it done like any other, so it
        /// is a real objective on every level and is drawn as one. On a
        /// SCORE-mode level it is the only objective there is — which is what
        /// this is for: pulling the score bar left those levels with an icon and
        /// a number that never moved. It now reads have/need and moves with the
        /// score.
        /// </summary>
        private static void AddTargets(HudModel model, TargetObject[] targets, int star1, int score)
        {
            model.PassMark = Mathf.Max(0, star1);

            if (targets != null)
            {
                for (int i = 0; i < targets.Length; i++)
                {
                    TargetObject target = targets[i];
                    if (target == null) continue;
                    model.Targets.Add(Slotify(target, star1, score));
                }
            }

            // A level with no targets at all would draw an empty Target panel,
            // which is the one thing this must never do. The pass mark is always
            // there to fall back on: no level can be won below it.
            if (model.Targets.Count == 0 && model.PassMark > 0)
                model.Targets.Add(ScoreSlot(null, model.PassMark, score));
        }

        /// <summary>
        /// The gem a BLOCKS chip is drawn over: the kit's own first item sprite,
        /// off the kit's own `Prefabs/Item`, so no new art enters the game and
        /// the chip cannot drift from what the board draws.
        ///
        /// Which of the six is measured, not chosen. Composited under
        /// `Textures/Blocks/block.png` on RoyalCream, item_01 separates from the
        /// card by 0.274 of luminance against 0.113 for the bare block and 0.145
        /// to 0.201 for the other five. On screen at 393 points that came out as
        /// 0.288 against 0.175 (level 33's chip, measured off the sweep's PNGs).
        ///
        /// It cannot be mistaken for a collect chip of the same gem: across all
        /// 1208 target assets, not one of the 508 levels carrying a BLOCKS chip
        /// carries a bare-gem chip beside it. Their other chip is always the star.
        /// </summary>
        private static Sprite _blockBacking;
        private static bool _blockBackingLoaded;

        public static Sprite BlockBacking
        {
            get
            {
                if (_blockBackingLoaded) return _blockBacking;
                _blockBackingLoaded = true;
                var prefab = Resources.Load<GameObject>("Prefabs/Item");
                if (prefab == null) return null;
                var item = prefab.GetComponent<Item>();
                if (item == null || item.items == null || item.items.Length == 0) return null;
                _blockBacking = item.items[0];
                return _blockBacking;
            }
        }

        private static HudTarget Slotify(TargetObject target, int star1, int score)
        {
            if (target.type != Target.SCORE)
            {
                return new HudTarget
                {
                    Icon = target.icon,
                    // The board never shows an empty block. `CreateSquare`
                    // instantiates blockPrefab as a child of a square that still
                    // holds an item, at z -0.01 in front of it, and the sprite is
                    // 55% opaque — so what the player breaks is a gem under ice.
                    // The chip drew the ice on its own and read as a blank box
                    // with a number beside it, on 508 of the 1208 levels.
                    Backing = target.type == Target.BLOCKS ? BlockBacking : null,
                    Remaining = Mathf.Max(0, target.GetCount()),
                    Done = target.Done()
                };
            }

            // The asset's own count and the level file's first star mark are the
            // same number throughout the shipped data; star1 wins where they
            // differ, because star1 is what CheckWinLose actually tests.
            int need = star1 > 0 ? star1 : target.targetCount;
            return ScoreSlot(target.icon, need, score);
        }

        private static HudTarget ScoreSlot(Sprite icon, int need, int score)
        {
            int have = Mathf.Clamp(score, 0, Mathf.Max(0, need));
            return new HudTarget
            {
                Icon = icon != null ? icon : StoryUi.StarSprite,
                Remaining = Mathf.Max(0, need - have),
                Done = need > 0 && have >= need,
                Text = ScoreText(have, need),
                IsScore = true
            };
        }

        /// <summary>
        /// What a level's HUD would read, from the shipped data alone — no board
        /// in the scene. The level file carries the mode, the limit and the star
        /// marks; the Targets asset carries the objectives. Both are loaded the
        /// same way LevelManager loads them, so this cannot drift from the level
        /// the player would actually get.
        /// </summary>
        public static HudModel ReadLevel(int level, int score)
        {
            var text = Resources.Load<TextAsset>("Levels/" + level);
            var asset = Resources.Load<TargetLevel>("Targets/Level" + level);
            if (text == null || asset == null) return null;

            int star1 = 0;
            int limit = 0;
            bool timed = false;
            string[] lines = text.text.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.StartsWith("STARS"))
                {
                    string[] marks = line.Substring("STARS".Length).Trim().Split('/');
                    if (marks.Length > 0) int.TryParse(marks[0], out star1);
                }
                else if (line.StartsWith("LIMIT"))
                {
                    string[] parts = line.Substring("LIMIT".Length).Trim().Split('/');
                    if (parts.Length > 1)
                    {
                        int type;
                        timed = int.TryParse(parts[0], out type) && type == (int)LIMIT.TIME;
                        int.TryParse(parts[1], out limit);
                    }
                }
            }

            var model = new HudModel();
            model.LimitTitle = timed ? "Time" : "Moves";
            model.LimitValue = timed
                ? string.Format("{0}:{1:00}", limit / 60, limit % 60)
                : limit.ToString();

            var targets = new TargetObject[asset.targets.Count];
            for (int i = 0; i < asset.targets.Count; i++) targets[i] = asset.targets[i].DeepCopy();
            AddTargets(model, targets, star1, score);
            return model;
        }

        /// <summary>The level's MODE line: which objective the level is about.</summary>
        public static Target ModeOf(int level)
        {
            var text = Resources.Load<TextAsset>("Levels/" + level);
            if (text == null) return Target.SCORE;

            string[] lines = text.text.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (!line.StartsWith("MODE")) continue;
                int mode;
                if (int.TryParse(line.Substring("MODE".Length).Trim(), out mode)) return (Target)mode;
            }
            return Target.SCORE;
        }

        private void SyncTargets(List<HudTarget> targets)
        {
            int count = Mathf.Min(targets == null ? 0 : targets.Count, MaxTargets);

            while (_slots.Count < count) _slots.Add(BuildSlot(_slots.Count));

            // The plate is shared out by how wide each target's number actually
            // is, not evenly. A score target reads up to five digits and a
            // collect one reads "3", so on a three-target level an even split
            // gave the score a fraction of the room it needed and best-fit
            // shrank it to its floor — a chip a player cannot read at 393 points
            // wide, next to two single digits at full size. Levels 12, 13 and 14
            // are where it showed. The weights are the two shapes, not per-level
            // tuning.
            float total = 0f;
            for (int i = 0; i < count; i++) total += SlotWeight(targets[i]);
            float placed = 0f;

            // Widening the score slot widened its ICON too, because the icon box
            // is the whole slot and `preserveAspect` fits the sprite into it.
            // The star came out at the icon band's full height on every plate
            // while a count chip was capped by its own narrower slot: 2.3x the
            // book and shield on a three-chip plate, 1.5x the blocks chip on a
            // two-chip one. Levels 49..64 are where it was measured, and it is
            // on every plate in the game that carries a score chip beside a
            // count. The plate still shares its WIDTH out by number shape; the
            // icon box no longer follows, and is the narrowest slot's width on
            // every chip, so one plate draws one icon size.
            float narrowest = float.MaxValue;
            for (int i = 0; i < count; i++)
                narrowest = Mathf.Min(narrowest, SlotWeight(targets[i]));

            for (int i = 0; i < _slots.Count; i++)
            {
                bool used = i < count;
                if (_slots[i].Root.gameObject.activeSelf != used)
                    _slots[i].Root.gameObject.SetActive(used);
                if (!used) continue;

                float weight = SlotWeight(targets[i]);
                float share = weight / Mathf.Max(0.0001f, total);
                PlaceSlot(_slots[i].Root, placed, placed + share);
                placed += share;

                // An icon box the full width of its slot puts two icons only
                // their own transparent margins apart, and the icon's shadow
                // then falls into that. Measured on level 81's own screenshot
                // at 393 points wide: 3 pixels between two gold-rimmed jewels
                // with 16 between the numbers under them, and the pair read as
                // one clump. 82, 85, 87, 90, 92, 94 and 95 are the rest of this
                // band that showed it. `IconGutter` is one shadow step, so an
                // icon's silhouette can no longer cross into the next slot.
                SizeIconBox(_slots[i], narrowest / Mathf.Max(0.0001f, weight),
                    count > 1 ? IconGutter * _scale : 0f);

                // Both sides or neither, never one. Gutting only the sides that
                // face a neighbour left the first chip a wider box than the
                // second, and best-fit then drew "25" visibly larger than the
                // "15" beside it — the same "one plate, two sizes" fault
                // SizeIconBox exists to prevent. A single-chip plate has nothing
                // to run into and keeps its full width.
                SizeCountBox(_slots[i], count > 1);

                HudTarget target = targets[i];
                if (_slots[i].Icon.sprite != target.Icon)
                {
                    _slots[i].Icon.sprite = target.Icon;
                    _slots[i].Icon.enabled = target.Icon != null;
                    _slots[i].Shadow.sprite = target.Icon;
                    _slots[i].Shadow.enabled = target.Icon != null;
                }

                if (_slots[i].Backing.sprite != target.Backing)
                {
                    _slots[i].Backing.sprite = target.Backing;
                    _slots[i].Backing.enabled = target.Backing != null;
                }

                // A finished target goes green rather than gaining a tick: the
                // kit's font is not guaranteed to carry a check glyph, and a
                // missing glyph renders as nothing at all.
                //
                // `count > 1` is the same seam SizeCountBox and SizeIconBox use,
                // and for the same reason: a shared plate has no room. See
                // HudTarget.DisplayShared.
                SetText(_slots[i].Count, count > 1 ? target.DisplayShared : target.Display);
                _slots[i].Count.color = target.Done ? StoryUi.RoyalGreen : StoryUi.RoyalNavy;
            }

            // Last, because it measures the boxes the loop above has just sized
            // and the strings it has just written.
            MatchNumberSizes(count);

            _visibleSlots = count;
        }

        private Slot BuildSlot(int index)
        {
            RectTransform root = StoryUi.CreateRect("Target" + index, _targetRow);

            var slot = new Slot { Root = root };

            // The icon casts its own shape as a shadow, one step down and right.
            //
            // Not decoration: a target icon is the kit's own artwork and some of
            // it is nearly white. `Textures/Blocks/block.png` — the icon on all
            // 40 shipped BLOCKS levels and every generated one — is a pale ice
            // tile at 0.77 luminance, and it is drawn on RoyalCream at 0.92. On
            // the board it reads because it frames a coloured gem; alone in a
            // chip it separated from the card by 0.15 and looked like an empty
            // box next to a number. Levels 18, 19, 29 and 31 are where it
            // showed. A silhouette of the icon's own sprite gives any pale icon
            // an edge without touching the kit's art, and sits under the darker
            // ones where it reads as the depth the reference panel has.
            //
            // The silhouette gave the block chip an EDGE; it did not give it a
            // subject, and levels 33..48 showed it still reading as a blank box
            // beside a number. `Slot.Backing` is the other half: the block
            // sprite is 55% opaque frost, so the chip now draws it over a gem
            // the way the board always has.
            slot.Shadow = StoryUi.CreateImage("IconShadow", root, null,
                new Color(StoryUi.RoyalNavy.r, StoryUi.RoyalNavy.g, StoryUi.RoyalNavy.b, 0.55f));
            slot.Shadow.preserveAspect = true;
            slot.Shadow.raycastTarget = false;
            RectTransform shadow = slot.Shadow.rectTransform;
            shadow.anchorMin = new Vector2(0f, 0.34f);
            shadow.anchorMax = new Vector2(1f, 1f);
            shadow.offsetMin = new Vector2(IconShadowStep * _scale, -IconShadowStep * _scale);
            shadow.offsetMax = new Vector2(IconShadowStep * _scale, -IconShadowStep * _scale);

            // What the icon sits on, between the shadow and the icon. Only a
            // BLOCKS chip fills this in; see HudTarget.Backing.
            slot.Backing = StoryUi.CreateImage("IconBacking", root, null, Color.white);
            slot.Backing.preserveAspect = true;
            slot.Backing.raycastTarget = false;
            slot.Backing.enabled = false;
            RectTransform backing = slot.Backing.rectTransform;
            backing.anchorMin = new Vector2(0f, 0.34f);
            backing.anchorMax = new Vector2(1f, 1f);
            backing.offsetMin = Vector2.zero;
            backing.offsetMax = Vector2.zero;

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

            // A score target alone on the plate reads "120/300" where a collect
            // target reads "4", and up to three targets share the card. Best fit
            // lets the one number stay big and the longer one still fit its
            // slot.
            //
            // This one keeps WRAPPING ON, unlike the Moves value. A slot is one
            // third of a plate 393 points wide, so with wrapping off best-fit
            // stops shrinking and the number simply runs into its neighbour:
            // level 20's three chips read "25 350/1000" instead of
            // "25 35 0/1000". Wrapping on, the same rect shrinks the number
            // until it fits between the chips either side of it. The slot is
            // wide enough that a wrap has not been reached — `SlotWeight` is
            // what keeps it that way.
            slot.Count.resizeTextForBestFit = true;
            slot.Count.resizeTextMinSize = MinNumberSize();
            slot.Count.resizeTextMaxSize = MaxNumberSize();

            return slot;
        }

        /// <summary>The smallest a chip's number may ever be drawn.</summary>
        private int MinNumberSize()
        {
            return Mathf.Max(8, Mathf.RoundToInt(18f * _scale));
        }

        /// <summary>
        /// The largest a chip's number may be drawn — what a chip with room to
        /// spare settles at, and the probe size <see cref="MatchNumberSizes"/>
        /// measures each string at.
        /// </summary>
        private int MaxNumberSize()
        {
            return Mathf.Max(9, Mathf.RoundToInt(52f * _scale));
        }

        /// <summary>
        /// One plate, one number size: every chip on a shared plate is drawn at
        /// the largest size that fits the tightest of them.
        ///
        /// Best-fit solves each chip on its own, so a chip's size is whatever
        /// its own string needs in its own box, and one card carries several
        /// answers. Level 113 read "20 2 1511" at three sizes — the two-digit
        /// count smallest, the single digit largest, half again as tall as the
        /// count beside it. It is the fourth member of the
        /// <see cref="SlotWeight"/> / <see cref="SizeIconBox"/> /
        /// <see cref="SizeCountBox"/> family: those three own how much ROOM a
        /// chip gets, and nothing owned the relationship between the SIZES the
        /// room produces.
        ///
        /// Nothing shrinks. The size chosen is the one the tightest chip already
        /// draws at, so the only chips that move are the ones with room to
        /// spare, and they move down to meet it. Over all 1208 levels the size
        /// this settles at is the cap on 695 of them, 17 on 229 and 14 on 284 —
        /// it never reaches <see cref="MinNumberSize"/>. That floor of 14 is
        /// what <see cref="HudTarget.DisplayShared"/> bought: while the score
        /// chip still read "have/need" the tightest chip on a plate was a nine-
        /// character string at the floor, and matching to it would have taken
        /// every number down with it.
        ///
        /// A plate with one chip keeps best-fit: there is nothing to match, and
        /// a lone score chip's "0/970" is measured to draw at the cap already.
        /// </summary>
        private void MatchNumberSizes(int count)
        {
            int cap = MaxNumberSize();
            if (count < 2)
            {
                RestoreBestFit(count);
                return;
            }

            float size = cap;
            for (int i = 0; i < count && i < _slots.Count; i++)
            {
                Text number = _slots[i].Count;
                if (number == null) continue;

                number.resizeTextForBestFit = false;
                number.fontSize = cap;

                float box = number.rectTransform.rect.width;
                float wanted = number.preferredWidth;

                // No layout yet — the rect has no width to measure against, and
                // a size picked from it would be the floor. Best-fit is right
                // until there is one, and Update pulls this again a tenth of a
                // second later.
                if (box <= 1f)
                {
                    RestoreBestFit(count);
                    return;
                }

                if (wanted > 0f) size = Mathf.Min(size, cap * box / wanted);
            }

            int chosen = Mathf.Clamp(Mathf.FloorToInt(size), MinNumberSize(), cap);
            for (int i = 0; i < count && i < _slots.Count; i++)
                if (_slots[i].Count != null) _slots[i].Count.fontSize = chosen;
        }

        /// <summary>Hands the first <paramref name="count"/> numbers back to
        /// best-fit, which is what a plate of one chip wants and what a plate
        /// with no layout yet has to fall back on.</summary>
        private void RestoreBestFit(int count)
        {
            for (int i = 0; i < count && i < _slots.Count; i++)
            {
                Text number = _slots[i].Count;
                if (number != null && !number.resizeTextForBestFit)
                    number.resizeTextForBestFit = true;
            }
        }

        /// <summary>
        /// How much of the target plate a slot is worth.
        ///
        /// A score target's number is the pass mark still owed — five digits at
        /// its longest, and four on 986 of the 1208 levels — and every other
        /// target's is a count of at most three. Two and one is the ratio of
        /// those two shapes, measured rather than tuned, and it still holds
        /// after <see cref="HudTarget.DisplayShared"/> shortened the score
        /// string: on a three-chip plate the score's slot is 59 pixels at 393
        /// wide and its two <see cref="CountGutter"/>s leave 49.5 for five
        /// digits — 9.9 each — against 20 pixels for a two-digit count, 10 each.
        /// The two numbers are drawn at the same size because they are given the
        /// same room per character.
        /// </summary>
        private static float SlotWeight(HudTarget target)
        {
            return target != null && target.IsScore ? 2f : 1f;
        }

        /// <summary>
        /// Centres the icon, its shadow and its backing in a box <paramref
        /// name="frac"/> of the slot wide, less a <paramref name="gutter"/> on
        /// each side, leaving the slot itself — and so the number under it — the
        /// width <see cref="SlotWeight"/> gave it.
        ///
        /// `preserveAspect` fits a sprite into whichever of the box's two sides
        /// binds first. Every icon on the plate gets the same box width here, so
        /// the widest slot can no longer draw the biggest icon.
        ///
        /// The gutter is <see cref="IconGutter"/> — one shadow step, so that an
        /// icon's silhouette stays inside its own slot instead of closing the
        /// gap to the chip beside it. Both sides or neither, on every slot: an
        /// icon inset only where it faces a neighbour would draw at two sizes
        /// across one plate, which is the fault this method exists to prevent.
        /// </summary>
        private void SizeIconBox(Slot slot, float frac, float gutter)
        {
            frac = Mathf.Clamp(frac, 0.05f, 1f);
            bool sameFrac = Mathf.Approximately(slot.IconFrac, frac);
            bool sameGutter = Mathf.Approximately(slot.IconGutter, gutter);
            if (sameFrac && sameGutter) return;
            slot.IconFrac = frac;
            slot.IconGutter = gutter;

            float min = 0.5f - frac * 0.5f;
            float max = 0.5f + frac * 0.5f;
            AnchorHorizontally(slot.Icon, min, max);
            AnchorHorizontally(slot.Shadow, min, max);
            AnchorHorizontally(slot.Backing, min, max);

            // The shadow carries its own step; the gutter is added to it rather
            // than replacing it, or the silhouette snaps back under the icon.
            InsetHorizontally(slot.Icon, 0f, gutter);
            InsetHorizontally(slot.Backing, 0f, gutter);
            InsetHorizontally(slot.Shadow, IconShadowStep * _scale, gutter);
        }

        /// <summary>
        /// Keeps a <see cref="CountGutter"/> of the slot clear on each side of a
        /// number when it shares the plate, so two numbers can never meet.
        ///
        /// The number is best-fit and centred, so narrowing its box does one of
        /// two things and both are wanted: a number that had slack simply moves
        /// away from the boundary, and one that filled its slot shrinks until it
        /// fits inside the gutter. Nothing else in the slot moves — the icon is
        /// centred by <see cref="SizeIconBox"/> and the slot itself still holds
        /// the width <see cref="SlotWeight"/> gave it.
        /// </summary>
        private void SizeCountBox(Slot slot, bool shared)
        {
            float gutter = shared ? CountGutter * _scale : 0f;
            if (Mathf.Approximately(slot.Gutter, gutter)) return;
            slot.Gutter = gutter;

            RectTransform rect = slot.Count.rectTransform;
            rect.offsetMin = new Vector2(gutter, rect.offsetMin.y);
            rect.offsetMax = new Vector2(-gutter, rect.offsetMax.y);
        }

        /// <summary>Holds <paramref name="gutter"/> clear on each side of an icon
        /// layer's box, on top of whatever horizontal offset it already wants —
        /// <paramref name="baseX"/>, which is the shadow's step.</summary>
        private static void InsetHorizontally(Image image, float baseX, float gutter)
        {
            if (image == null) return;
            RectTransform rect = image.rectTransform;
            rect.offsetMin = new Vector2(baseX + gutter, rect.offsetMin.y);
            rect.offsetMax = new Vector2(baseX - gutter, rect.offsetMax.y);
        }

        /// <summary>Narrows an icon layer to a horizontal band of its slot, keeping
        /// whatever offsets it carries — the shadow's step, for one.</summary>
        private static void AnchorHorizontally(Image image, float min, float max)
        {
            if (image == null) return;
            RectTransform rect = image.rectTransform;
            rect.anchorMin = new Vector2(min, rect.anchorMin.y);
            rect.anchorMax = new Vector2(max, rect.anchorMax.y);
        }

        /// <summary>Puts a slot in the band from one fraction of the plate to another.</summary>
        private static void PlaceSlot(RectTransform slot, float from, float to)
        {
            slot.anchorMin = new Vector2(from, 0f);
            slot.anchorMax = new Vector2(to, 1f);
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
        /// <summary>
        /// Takes the kit's header off the screen for good, rather than leaving it
        /// alive underneath this one.
        ///
        /// Everything that is chrome — the score bar, its star markers, the
        /// static target slots — is DESTROYED. It used to be deactivated, which
        /// left a second header sitting under the new one for anyone reading the
        /// hierarchy or a screenshot.
        ///
        /// One object cannot be destroyed and is adopted instead: the group that
        /// owns the kit's TargetIcons. `LevelManager.CheckCollectedTarget` flies
        /// every matched piece to `TargetObject.guiObj`, one of those icons, and
        /// `TargetsGroup.OnDisable` destroys the icons it made — so destroying
        /// the group, or merely disabling it, leaves the board animating to a
        /// dead transform. It is reparented INTO this HUD and kept transparent,
        /// so it is an animation anchor inside the new header rather than a
        /// second header behind it. Nothing of the kit's own strip survives as a
        /// layer of its own.
        /// </summary>
        private void ReplaceKitHud(Transform strip)
        {
            var destroyed = new List<string>();
            var adopted = new List<string>();

            // Copied first: destroying children while enumerating the live
            // Transform skips every other one.
            var children = new List<Transform>();
            foreach (Transform child in strip) children.Add(child);

            foreach (Transform child in children)
            {
                if (child == null || child.name == "Pause") continue;

                if (child.GetComponentInChildren<TargetsGroup>(true) != null)
                {
                    child.SetParent(_root, false);
                    Fade(child);
                    adopted.Add(child.name);
                    continue;
                }

                destroyed.Add(child.name);
                Destroy(child.gameObject);
            }

            Debug.Log("[Story] Level HUD: destroyed " + string.Join(", ", destroyed.ToArray()) +
                      "; adopted " + string.Join(", ", adopted.ToArray()) +
                      " as the collect animation's anchor.");
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

        /// <summary>
        /// Takes the kit's "Level" plate and its level number off the booster
        /// row, rather than leaving them under the pause button that just moved
        /// on top of them.
        ///
        /// The row carries three things: the boosters, the pause button this
        /// HUD put there, and — from the kit — a `level_txt` image with the word
        /// "Level" painted into it plus a Text that Counter_ fills with the
        /// level number. The kit drew those because its bottom bar was the only
        /// place a level was named. This game names the level twice already, on
        /// the start popup and on the win panel, and neither of those is the
        /// board; what is left on the bar is a stray word sitting behind the
        /// pause button in all 1208 levels.
        ///
        /// Destroyed for the same reason the kit's header is: a deactivated one
        /// is still a second layer for anyone reading the hierarchy, and the
        /// baked-in label is exactly what StoryUi refuses to borrow.
        /// </summary>
        private void ReplaceKitLevelPlate(Transform strip)
        {
            RectTransform row = FindBoosterRow(strip);
            if (row == null) return;

            var children = new List<Transform>();
            foreach (Transform child in row) children.Add(child);

            // FindBoosterRow falls back to the whole bottom panel when it cannot
            // find the row, and everything on the panel would then look like
            // chrome to the loop below. Only a row that actually holds boosters
            // is one whose other children are the kit's level plate.
            bool isBoosterRow = false;
            foreach (Transform child in children)
                if (child != null && child.name.StartsWith("Boost")) isBoosterRow = true;
            if (!isBoosterRow) return;

            var destroyed = new List<string>();
            foreach (Transform child in children)
            {
                if (child == null) continue;
                if (child.name == "Pause") continue;
                if (child.name.StartsWith("Boost")) continue;

                destroyed.Add(child.name);
                Destroy(child.gameObject);
            }

            if (destroyed.Count == 0) return;

            Debug.Log("[Story] Level HUD: destroyed the kit's level plate on the booster row (" +
                      string.Join(", ", destroyed.ToArray()) + ").");
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
            if (_root == null || _visibleSlots == 0) return;

            // The icons were reparented into this HUD when the kit's header was
            // destroyed, so they are found here rather than on the old strip —
            // which no longer has any children left to search.
            TargetIcon[] icons = _root.GetComponentsInChildren<TargetIcon>(true);
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
