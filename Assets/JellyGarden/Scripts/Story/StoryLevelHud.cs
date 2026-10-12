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
//   This HUD is not a second header. It is built INSIDE the kit's own strip,
//   which is stretched over the panel the metrics below were measured against.
//   The strip is the object the kit shows and hides when a level opens and
//   closes, so hosting the HUD there means it is shown and hidden with it, and
//   inherits its scaling and sort order, for free.
//
//   The kit's header CONTENT is DESTROYED, not hidden under this one. The score
//   bar, its star markers and the static target slots are chrome nothing else
//   references, and they go. What survives is the strip itself, now carrying
//   King Leo's design instead of Jelly Garden's.
//
//   One object survives and is adopted into this HUD instead: the group owning
//   the kit's TargetIcons. LevelManager's collect animation flies a matched
//   piece to `TargetObject.guiObj`, one of those icons, and TargetsGroup
//   destroys the icons it made in OnDisable — so destroying the group, or even
//   disabling it, leaves the board animating to a dead transform. It is
//   reparented in and kept transparent: an anchor inside the new header rather
//   than a second header behind it.
//
// RETIRED FROM THE LEVEL. Royal Throne's HUD (RoyalGameplayPresentation) is the
// in-level HUD now. Nothing installs this one on a level any more. The copy
// baked into game.unity only drives itself while the Royal layer is off (see
// Enabled); with it on, the strip is inert under Royal Throne's hidden
// Canvas/Panel. What stays is the goal model (HudModel, HudTarget, ReadLevel,
// ReadLevelManager) that StoryLevelPopup reads, and the strip builder that
// StoryHudBaker and the screen tests drive.

using System;
using System.Collections.Generic;
using JellyGarden.Scripts.GUI;
using JellyGarden.Scripts.Targets;
using UnityEngine;
using UnityEngine.UI;

namespace JellyGarden.Scripts.Story
{
    [DisallowMultipleComponent]
    public sealed partial class StoryLevelHud : MonoBehaviour
    {
        /// <summary>
        /// Whether the baked strip drives itself. Only while the Royal layer is
        /// off: LevelManager adds RoyalGameplayPresentation when the effects
        /// profile loads, and that HUD then replaces this one.
        /// </summary>
        public static bool Enabled
        {
            get { return !JellyGarden.Scripts.Juice.RoyalEffectsProfile.Get; }
        }

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
            /// Moves, or seconds, still left — the same number
            /// <see cref="LimitValue"/> spells out, kept as an int because the
            /// portrait reacts to how much is left and a clock string cannot be
            /// compared. -1 means nothing said.
            /// </summary>
            public int LimitLeft = -1;

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

        #region Reference HUD geometry

        // Measured on tools/art/hud/reference/frame-compact-source.png (2172 x 724).
        // All heights use the existing canvas's width scale. Keep the frame's
        // aspect ratio, with the live portrait inside its gold opening.
        private const float DesignWidth = 1080f;
        private const float StripHeight = 360f;
        private const float PanelLeftMin = 0.060f;
        private const float PanelLeftMax = 0.335f;
        private const float PanelTop = 80f;
        private const float PanelHeight = 192f;
        private const float TitleFrac = 0.32f;
        private const float TitleSize = 38f;
        private const float WindowMin = 0.378f;
        private const float WindowMax = 0.622f;
        private const float WindowTop = 53f;
        private const float WindowHeight = 253f;

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

        #region The Royal Court palette

        // Taken from the reference art: a wooden frame, two deep-blue panels,
        // cream chips, and gold on every rim. The strip used to be flat
        // RoyalBlue with gold trim; this is the same family with the wood frame
        // and the panel/chip separation the reference actually has.
        //
        // Held HERE rather than in StoryUi.Royal: those colours are shared by
        // every other screen in the game, and this is the strip's own scheme.

        private static readonly Color WoodFrame = Rgb(138, 90, 51);      // outer frame
        private static readonly Color WoodShadow = Rgb(101, 63, 33);     // its lower edge
        private static readonly Color PanelBlue = Rgb(30, 78, 156);      // Targets / Moves panels
        private static readonly Color PanelBlueDeep = Rgb(20, 55, 115);  // their rim
        private static readonly Color PanelRule = Rgb(240, 169, 59);     // the rule under a title
        private static readonly Color PanelTitle = Rgb(255, 241, 197);   // TARGETS / MOVES
        private static readonly Color PillFill = Rgb(246, 238, 220);     // a target chip
        private static readonly Color PillEdge = Rgb(214, 194, 160);
        private static readonly Color BadgeRim = Rgb(232, 180, 74);      // the icon's gold ring
        private static readonly Color BadgeFace = Rgb(255, 250, 238);
        private static readonly Color PanelNumber = Rgb(17, 39, 98);     // every number

        /// <summary>The chips' corner radius. A pill: half the row's height.</summary>
        private const int PillCorner = 40;

        /// <summary>The panels' corner radius, in texture pixels.</summary>
        private const int PanelCorner = 12;

        /// <summary>
        /// How wide the round icon badge is, in 1080-wide design units. Matched
        /// to the content band's height on the half-height bar, so the badge
        /// reads as a circle rather than as a squashed oval.
        /// </summary>
        private const float BadgeSize = 96f;

        /// <summary>The clear space between two stacked chips, in design units.</summary>
        private const float RowGap = 8f;


        private static Color Rgb(int r, int g, int b)
        {
            return new Color(r / 255f, g / 255f, b / 255f, 1f);
        }

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

        #region Binding

        private void Awake()
        {
            EnsureBound();
        }

        /// <summary>
        /// Points this component at the parts the SCENE already holds.
        ///
        /// The strip is baked into game.unity, so on a scene load these objects
        /// exist before anything runs — but the fields below are private and not
        /// serialised, so they come back null and have to be found again. They
        /// are found BY NAME, the names StoryHudBaker wrote, rather than by
        /// serialised references: a name survives someone re-baking, re-parenting
        /// or hand-editing the strip in the Editor, and a broken one is a null
        /// this method reports rather than a silently dead HUD.
        ///
        /// Cheap and idempotent: it returns immediately once bound.
        /// </summary>
        public void EnsureBound()
        {
            if (_root != null) return;

            _root = transform as RectTransform;
            if (_root == null) return;

            _scale = ScaleOf(_root.parent);
            // These paths and the ones BuildStrip creates are the same fact
            // written twice, and they have already drifted apart once — the HUD
            // came up with an empty Targets panel and a Moves number frozen on
            // its placeholder, because the binder was still looking for a
            // hierarchy the builder had stopped making. If you rename anything
            // in Panel(), BuildPortrait() or BuildLimitPanel(), rename it here.
            _targetTitle = TextAt("TargetPanel/Title");
            _limitTitle = TextAt("MovesPanel/Title");
            _limitValue = TextAt("MovesPanel/Content/MovesCard/MovesValue");
            _targetRow = _root.Find("TargetPanel/Content") as RectTransform;

            Transform face = _root.Find("Portrait/Window/Face");
            _portrait = face == null ? null : face.GetComponent<Image>();

            Instance = this;

            if (_targetRow == null || _limitValue == null || _portrait == null)
                Debug.LogError("[Story] Level HUD: the scene's strip is missing parts — " +
                               "targets row " + (_targetRow == null ? "MISSING" : "ok") +
                               ", moves value " + (_limitValue == null ? "MISSING" : "ok") +
                               ", portrait " + (_portrait == null ? "MISSING" : "ok") +
                               ". Re-run JellyGarden/Story/Bake Level HUD into the scene.");
        }

        private Text TextAt(string path)
        {
            Transform found = _root.Find(path);
            return found == null ? null : found.GetComponent<Text>();
        }

        /// <summary>
        /// Builds the strip and nothing else — no board, no kit HUD, no scene
        /// assumptions.
        ///
        /// This is no longer what runs when a level opens: StoryHudBaker runs it
        /// ONCE, in the Editor, and saves the result into game.unity. It stays
        /// the single definition of the strip's shape, metrics and colours, and
        /// it is what an editor test drives.
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
            // The level is inactive while its HUD is baked. Excluding inactive
            // ancestors silently uses scale 1 on the kit's 720-wide canvas,
            // stretching the frame vertically by 50%.
            var scaler = parent.GetComponentInParent<CanvasScaler>(true);
            if (scaler == null || scaler.referenceResolution.x <= 0f) return 1f;
            return scaler.referenceResolution.x / DesignWidth;
        }

        #endregion

        #region Building the strip

        private void BuildStrip()
        {
            // ONE painted plate. hud_frame carries the wood frame, both blue
            // panels, their gold rules and the gold medallion with its sunburst
            // — everything in the reference that never changes. Nothing below
            // draws a rim, a rule or a ring by hand any more; the code only
            // places the things that DO change over the art.
            Image body = StoryUi.CreateImage("Strip", _root,
                StoryUi.LoadSprite("hud_reference_frame"), Color.white);
            Slab(body.rectTransform, 0f, 1f, 0f, StripHeight);
            body.raycastTarget = true;   // the strip eats taps meant for the board
            if (StorybookUi.Enabled) body.color = Color.clear;

            BuildTargetPanel();
            BuildPortrait();
            BuildLimitPanel();
        }

        /// <summary>
        /// Lays out one of the two panels PAINTED INTO hud_frame: its name in the
        /// band above the gold rule, and a content rect below it.
        ///
        /// Draws no background of its own. Every number here — where the panel
        /// sits, how tall it is, where the rule crosses it — was measured off the
        /// art rather than chosen, so the text lands on the panel the painting
        /// actually has. Re-measure if the art is regenerated.
        /// </summary>
        private RectTransform Panel(string name, float xMin, float xMax, string label)
        {
            RectTransform panel = StoryUi.CreateRect(name, _root);
            Slab(panel, xMin, xMax, PanelTop, PanelHeight);
            if (StorybookUi.Enabled)
            {
                Image paper = StorybookUi.PaperCard("PaperPanel", panel, 3f * _scale, false);
                StoryUi.Stretch(paper.rectTransform);
            }

            Text title = StoryUi.CreateText("Title", panel, label,
                Mathf.RoundToInt(TitleSize * _scale), StorybookUi.Enabled ? StorybookUi.Ink : PanelTitle, TextAnchor.MiddleCenter);
            title.font = StorybookUi.Enabled ? StoryUi.NameFont : StoryUi.DisplayFont;
            title.fontStyle = FontStyle.Normal;
            var titleShadow = title.gameObject.AddComponent<Shadow>();
            titleShadow.effectColor = StorybookUi.Enabled ? Color.clear : Rgb(71, 35, 28);
            titleShadow.effectDistance = new Vector2(0f, -1f * _scale);
            RectTransform titleRect = title.rectTransform;
            titleRect.anchorMin = new Vector2(0f, 1f - TitleFrac);
            titleRect.anchorMax = Vector2.one;
            titleRect.offsetMin = new Vector2(10f * _scale, 2f * _scale);
            titleRect.offsetMax = new Vector2(-10f * _scale, -2f * _scale);
            // TARGETS fits within the padded band at this fixed size. Keep all
            // headings consistent and centre their visible glyphs above the rule.
            title.resizeTextForBestFit = false;
            title.alignByGeometry = true;
            title.horizontalOverflow = HorizontalWrapMode.Overflow;
            title.verticalOverflow = VerticalWrapMode.Truncate;

            RectTransform content = StoryUi.CreateRect("Content", panel);
            content.anchorMin = Vector2.zero;
            content.anchorMax = new Vector2(1f, 1f - TitleFrac);
            content.offsetMin = new Vector2(5f * _scale, 6f * _scale);
            content.offsetMax = new Vector2(-5f * _scale, -8f * _scale);
            return content;
        }

        private void BuildTargetPanel()
        {
            _targetRow = Panel("TargetPanel", PanelLeftMin, PanelLeftMax, "TARGETS");
            Transform title = _root.Find("TargetPanel/Title");
            _targetTitle = title == null ? null : title.GetComponent<Text>();
        }

        private void BuildLimitPanel()
        {
            // Mirrored: the right panel is the left one reflected about the
            // centre line, which is how the art is painted.
            RectTransform content = Panel("MovesPanel",
                1f - PanelLeftMax, 1f - PanelLeftMin, "MOVES");
            Transform title = _root.Find("MovesPanel/Title");
            _limitTitle = title == null ? null : title.GetComponent<Text>();

            Image card = StoryUi.CreateImage("MovesCard", content,
                StoryUi.LoadSprite("hud_reference_moves"), Color.white);
            StoryUi.Stretch(card.rectTransform);
            card.type = Image.Type.Sliced;
            if (StorybookUi.Enabled)
            {
                card.sprite = StoryUi.RoundedSprite(20);
                card.color = StorybookUi.Paper;
            }

            _limitValue = StoryUi.CreateText("MovesValue", card.rectTransform, "0",
                Mathf.RoundToInt(178f * _scale), PanelNumber, TextAnchor.MiddleCenter);
            _limitValue.font = StorybookUi.Enabled ? StoryUi.NameFont : StoryUi.DisplayFont;
            StoryUi.Stretch(_limitValue.rectTransform, 14f * _scale);
            _limitValue.fontStyle = FontStyle.Normal;
            var numberEdge = _limitValue.gameObject.AddComponent<Outline>();
            numberEdge.effectColor = new Color(0.60f, 0.91f, 1f, 0.75f);
            numberEdge.effectDistance = new Vector2(1f * _scale, -1f * _scale);

            // A moves value is one or two characters and a TIME value is four —
            // "0:59" — and the card is sized for the short one. FitOneLine turns
            // wrapping off, which is what makes best-fit solve for WIDTH; left
            // wrapping on, Unity broke "0:59" into "0:5" over "9".
            StoryUi.FitOneLine(_limitValue,
                Mathf.Max(8, Mathf.RoundToInt(48f * _scale)),
                Mathf.Max(9, Mathf.RoundToInt(178f * _scale)));
        }

        /// <summary>
        /// The king, inside the medallion the frame art already paints.
        ///
        /// No rings are drawn here — the frame has the plain gold circle and
        /// the sunburst behind it. All this does is clip the portrait to its
        /// opening. The clip is not optional: the avatar frames carry a
        /// royal-blue ground in their corners from the clip they were cut out
        /// of, and the mask is what removes it.
        ///
        /// The wrapper spans the whole strip so the window's own numbers are
        /// read against the same origin the generator measured them in.
        /// </summary>
        private void BuildPortrait()
        {
            RectTransform frame = StoryUi.CreateRect("Portrait", _root);
            Slab(frame, 0f, 1f, 0f, StripHeight);

            Image window = StoryUi.CreateImage("Window", frame, StoryUi.CircleSprite,
                Color.white);
            Slab(window.rectTransform, WindowMin, WindowMax, WindowTop, WindowHeight);
            window.gameObject.AddComponent<Mask>().showMaskGraphic = false;

            if (StorybookUi.Enabled)
            {
                Image ring = StoryUi.CreateImage("GoldMedallion", frame, StoryUi.CircleSprite, StorybookUi.Gold);
                Slab(ring.rectTransform, WindowMin, WindowMax, WindowTop, WindowHeight);
                ring.rectTransform.sizeDelta += Vector2.one * (12 * _scale);
                ring.transform.SetAsFirstSibling();
            }
            _portrait = StoryAvatar.Build(PortraitCharacter, window.rectTransform);
        }


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
            if (!Enabled) return;
            if (Time.unscaledTime < _nextRefresh) return;
            _nextRefresh = Time.unscaledTime + RefreshInterval;
            Refresh();
        }

        private void LateUpdate()
        {
            if (!Enabled) return;
            AimKitIconsAtThisHud();
        }

        /// <summary>Pulls the live values and writes only what changed.</summary>
        public void Refresh()
        {
            HudModel model = Source != null ? Source() : ReadLevelManager();
            if (model == null) return;

            SetText(_limitTitle, model.LimitTitle.ToUpperInvariant());
            SetText(_limitValue, model.LimitValue);
            SyncTargets(model.Targets);
            React(model);
        }

        #region The face

        // A cascade is what a reaction is for, not a single match. A match of
        // three takes at most three off the count, so five is the smallest drop
        // that cannot be one — measured against the refresh interval above,
        // which is short enough that two separate matches do not land in one
        // pull.
        private const int PopDrop = 5;

        // Royal Match starts fretting with a handful of moves left. Three is the
        // point the kit's own low-moves warning fires at; a timed level counts
        // seconds instead, where ten is the same fraction of a typical limit.
        private const int LowMoves = 3;
        private const int LowSeconds = 10;

        private int _owed = -1;
        private bool _wasDone;

        /// <summary>
        /// Turns the strip's numbers into an expression on the portrait.
        ///
        /// Read from the model rather than hooked into the board, so nothing in
        /// the kit's match, cascade or win path is touched to get it — the face
        /// reacts to what the HUD already pulls ten times a second, and a level
        /// with no avatar frames authored simply never reacts.
        /// </summary>
        private void React(HudModel model)
        {
            int owed = 0;
            bool allDone = model.Targets.Count > 0;
            for (int i = 0; i < model.Targets.Count; i++)
            {
                HudTarget target = model.Targets[i];
                owed += Mathf.Max(0, target.Remaining);
                if (!target.Done) allDone = false;
            }

            bool timed = model.LimitTitle == "Time";
            int low = timed ? LowSeconds : LowMoves;

            if (allDone && !_wasDone) StoryAvatar.React("cheer");
            else if (_owed >= 0 && _owed - owed >= PopDrop) StoryAvatar.React("pop");
            else if (model.LimitLeft >= 0 && model.LimitLeft <= low && !allDone)
                StoryAvatar.React("worry");

            _wasDone = allDone;
            _owed = owed;
        }

        #endregion

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
            model.LimitLeft = limit;

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
        /// Turns the kit's target list into slots, in the kit's own order
        /// (LevelManager.targetObject), so slot i is always target i.
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
                // Same art as the board's goal icon (TargetIcon): the catalog
                // first, main's storybook art where the catalog has none.
                var art = RoyalThrone.Presentation.RoyalVisualCatalog.Goal(target);
                if (!art) art = StorybookBoardArt.Goal(target);
                return new HudTarget
                {
                    Icon = art,
                    // The KIT's block sprite is 55% opaque frost, so drawn alone
                    // on cream it read as a blank box with a number beside it —
                    // hence the gem behind it, the way `CreateSquare` stacks them
                    // on the board. The storybook `ice` is an opaque cyan frame
                    // with its own navy outline and needs no such help; a gem
                    // behind it also names a COLOUR the goal does not have, since
                    // a BLOCKS target counts a crate clearing whatever sits under
                    // it (`Square.DestroyBlock`). Backing is for the kit art only.
                    Backing = target.type == Target.BLOCKS && !StorybookBoardArt.Enabled ? BlockBacking : null,
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
                Icon = StorybookBoardArt.Enabled ? StorybookBoardArt.Get("score") : (icon != null ? icon : StoryUi.StarSprite),
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
            model.LimitLeft = limit;

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

        /// <summary>
        /// A single horizontal group of objectives, each with its icon above
        /// its count. Shared objectives show the amount still owed.
        /// </summary>
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

                Slot slot = _slots[i];
                HudTarget target = targets[i];
                PlaceTarget(slot.Root, i, count);
                // Keep icons square, with room below for the count. Their size
                // follows the available column width on three/four-goal levels.
                float iconSize = Mathf.Min(68f * _scale,
                    Mathf.Max(1f, _targetRow.rect.width / Mathf.Max(2, count) - 12f * _scale));
                var iconBox = slot.Icon.transform.parent as RectTransform;
                iconBox.sizeDelta = new Vector2(iconSize, iconSize);

                if (slot.Icon.sprite != target.Icon)
                {
                    slot.Icon.sprite = target.Icon;
                    slot.Icon.enabled = target.Icon != null;
                    slot.Shadow.sprite = target.Icon;
                    slot.Shadow.enabled = target.Icon != null;
                }

                if (slot.Backing.sprite != target.Backing)
                {
                    slot.Backing.sprite = target.Backing;
                    slot.Backing.enabled = target.Backing != null;
                }

                // A finished target goes green rather than gaining a tick: the
                // kit's font is not guaranteed to carry a check glyph, and a
                // missing glyph renders as nothing at all.
                SetText(slot.Count, count > 1 ? target.DisplayShared : target.Display);
                slot.Count.color = target.Done ? StoryUi.RoyalGreen : StorybookUi.Enabled ? StorybookUi.Ink : PanelTitle;
            }

            _visibleSlots = count;
        }

        /// <summary>Centre one group of evenly spaced icon/count columns.</summary>
        private void PlaceTarget(RectTransform column, int index, int count)
        {
            if (column == null || count <= 0) return;
            float width = 1f / count;
            float left = (1f - count * width) * 0.5f + index * width;
            column.anchorMin = new Vector2(left, 0f);
            column.anchorMax = new Vector2(left + width, 1f);
            column.offsetMin = new Vector2(3f * _scale, 0f);
            column.offsetMax = new Vector2(-3f * _scale, 0f);
        }

        /// <summary>One bare objective icon with its count underneath.</summary>
        private Slot BuildSlot(int index)
        {
            RectTransform root = StoryUi.CreateRect("Target" + index, _targetRow);
            var slot = new Slot { Root = root };

            RectTransform badge = StoryUi.CreateRect("IconBox", root);
            badge.anchorMin = new Vector2(0.5f, 0.66f);
            badge.anchorMax = badge.anchorMin;
            badge.pivot = new Vector2(0.5f, 0.5f);
            badge.anchoredPosition = Vector2.zero;

            // The icon casts its own shape as a shadow, one step down and right.
            //
            // Not decoration: a target icon is the kit's own artwork and some of
            // it is nearly white. `Textures/Blocks/block.png` — the icon on all
            // 40 shipped BLOCKS levels and every generated one — is a pale ice
            // tile, and on a cream badge it read as an empty box next to a
            // number. A silhouette gives any pale icon an edge without touching
            // the kit's art.
            slot.Shadow = StoryUi.CreateImage("IconShadow", badge, null,
                new Color(0f, 0f, 0f, 0.28f));
            slot.Shadow.preserveAspect = true;
            slot.Shadow.raycastTarget = false;
            StoryUi.Stretch(slot.Shadow.rectTransform);
            slot.Shadow.rectTransform.anchoredPosition =
                new Vector2(IconShadowStep * _scale, -IconShadowStep * _scale);

            // What the icon sits on, between the shadow and the icon. Only a
            // BLOCKS chip fills this in; see HudTarget.Backing. The block sprite
            // is 55% opaque frost, so the chip draws it over a gem the way the
            // board always has.
            slot.Backing = StoryUi.CreateImage("IconBacking", badge, null, Color.white);
            slot.Backing.preserveAspect = true;
            slot.Backing.raycastTarget = false;
            slot.Backing.enabled = false;
            StoryUi.Stretch(slot.Backing.rectTransform);

            slot.Icon = StoryUi.CreateImage("Icon", badge, null, Color.white);
            slot.Icon.preserveAspect = true;
            StoryUi.Stretch(slot.Icon.rectTransform);

            slot.Count = StoryUi.CreateText("Count", root, "0",
                Mathf.RoundToInt(36f * _scale), PanelTitle, TextAnchor.MiddleCenter);
            slot.Count.font = StorybookUi.Enabled ? StoryUi.BodyFont : StoryUi.DisplayFont;
            slot.Count.fontStyle = FontStyle.Normal;
            slot.Count.alignByGeometry = true;
            RectTransform count = slot.Count.rectTransform;
            count.anchorMin = Vector2.zero;
            count.anchorMax = new Vector2(1f, 0.32f);
            count.offsetMin = Vector2.zero;
            count.offsetMax = Vector2.zero;

            // Fit long score counts inside their own column without wrapping
            // or running into the neighbouring target's number.
            slot.Count.resizeTextForBestFit = true;
            slot.Count.resizeTextMinSize = Mathf.Max(8, Mathf.RoundToInt(14f * _scale));
            slot.Count.resizeTextMaxSize = Mathf.Max(9, Mathf.RoundToInt(36f * _scale));
            slot.Count.horizontalOverflow = HorizontalWrapMode.Wrap;
            slot.Count.verticalOverflow = VerticalWrapMode.Truncate;
            if (StorybookUi.Enabled)
            {
                slot.Count.fontSize = Mathf.RoundToInt(34 * _scale);
                slot.Count.resizeTextForBestFit = false;
                slot.Count.horizontalOverflow = HorizontalWrapMode.Overflow;
                slot.Count.verticalOverflow = VerticalWrapMode.Overflow;
            }

            return slot;
        }

        #endregion

        #region Standing in for the kit's HUD

        /// <summary>
        /// The kit's pause button sat inside the strip this HUD now covers, so
        /// it moves to the empty middle slot of the kit's booster row — which is
        /// where Royal Match keeps its settings button, and the one place on the
        /// screen that draws above this HUD and holds nothing a player needs.
        ///
        /// Moved, not replaced: it is still the kit's button running the kit's
        /// action, and if the booster row is not there it stays where it was
        /// rather than being hidden.
        ///
        /// It sits a little ABOVE the boosters rather than level with them,
        /// because the tray it stands on is not flat: `gui_bottom` has a raised
        /// centre boss with a round recess in it, and the pause button is the
        /// thing that recess is for. Level with the boosters it sat on the rail
        /// under the boss, which reads as a button that missed its socket.
        /// </summary>
        public void MovePauseOutOfTheHudsWay(Transform strip)
        {
            RectTransform row = FindBoosterRow(strip);
            if (row == null) return;

            // NOT strip.Find("Pause"). The scene keeps the pause button under
            // BottomPanel/Panel, and FindKitHudStrip answers with the TOP strip
            // — its own walk starts at the progress bar and falls back to
            // `Level/Canvas/Panel/Panel` — so looking for the button on the
            // strip found nothing and this method returned before doing
            // anything at all. It looked right anyway, because the scene had
            // already parked the button in the middle of the tray, which is why
            // it went unnoticed until the tray art changed and the button was
            // asked to move.
            RectTransform pause = FindPause(strip) ?? FindPause(row);
            if (pause == null) return;

            // The row IS the tray — FindBoosterRow returns the child of
            // BottomPanel that holds the Boost objects, which is the same
            // `Panel` that draws `gui_bottom`. So the button is placed against
            // the tray's own rect rather than against a booster's y: the boss
            // is part of the artwork, and only the artwork knows where it is.
            float trayHeight = row.rect.height > 1f ? row.rect.height : KitTrayHeight;

            pause.SetParent(row, false);
            pause.SetAsLastSibling();
            pause.anchorMin = new Vector2(0.5f, 0.5f);
            pause.anchorMax = new Vector2(0.5f, 0.5f);
            pause.pivot = new Vector2(0.5f, 0.5f);
            pause.anchoredPosition = new Vector2(0f, trayHeight * RecessRise);
            if (StorybookUi.Enabled && row.Find("StorybookTray") == null)
            {
                Image original = row.GetComponent<Image>();
                if (original != null) original.color = Color.clear;
                Image paper = StorybookUi.PaperCard("StorybookTray", row, .65f, false);
                StoryUi.Stretch(paper.rectTransform);
                paper.transform.SetAsFirstSibling();
                Image pauseFace = pause.GetComponent<Image>();
                if (pauseFace != null)
                {
                    pauseFace.sprite = StoryUi.CircleSprite;
                    pauseFace.color = StorybookUi.Blue;
                    Image gear = StoryUi.CreateImage("StorybookGear", pause, StoryUi.GearSprite, StorybookUi.Paper);
                    StoryUi.Stretch(gear.rectTransform, 15);
                    gear.preserveAspect = true;
                }
            }
        }

        /// <summary>
        /// Where the recess on the tray's centre boss sits, as a fraction of the
        /// tray's height above its middle.
        ///
        /// Measured off `gui_bottom` itself: sampling the art's centre column,
        /// the dark disc inside the gold boss runs from row 66 to row 200 of
        /// 342, so its middle is at 0.389 of the height from the top — 0.111 of
        /// the height ABOVE the middle. The disc is 0.392 of the height across,
        /// which at 171 units is 67, and the kit's pause button is 69: it is
        /// the plug for that socket, and this is what seats it.
        ///
        /// Placing it against the boosters instead — the first two attempts —
        /// could only ever be a guess, because the boosters know nothing about
        /// where the artwork put its hole.
        ///
        /// 0.16 rather than the disc's own 0.111: the button rides a little
        /// proud of the recess, which is how a raised control reads against a
        /// socket. Past about 0.21 the dark disc starts showing under it and
        /// the button looks like it slipped off the boss.
        ///
        /// 0.14 is the value the owner settled on from a running build, walked
        /// down from 0.45 — the offset that proved the seating works at all —
        /// through 0.36, 0.26 and 0.16. It sits between the art's two readings:
        /// 0.111 seats the button dead centre in the recess, and past about
        /// 0.21 the dark disc shows underneath and the button looks like it
        /// slipped off the boss.
        /// </summary>
        private const float RecessRise = 0.14f;

        /// <summary>The tray's height in canvas units, when its rect cannot be read yet.</summary>
        private const float KitTrayHeight = 171f;

        /// <summary>The pause button anywhere under the given root, active or not.</summary>
        private static RectTransform FindPause(Transform root)
        {
            if (root == null) return null;
            if (root.name == "Pause") return root as RectTransform;
            for (int i = 0; i < root.childCount; i++)
            {
                RectTransform found = FindPause(root.GetChild(i));
                if (found != null) return found;
            }
            return null;
        }

        /// <summary>The kit's booster row, found from the boosters in it.</summary>
        public static RectTransform FindBoosterRow(Transform strip)
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
                icons[i].transform.position = _slots[i].Icon.transform.position;
            }
        }

        #endregion

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
