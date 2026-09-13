// Characters talking: a portrait, a name, and a line that types itself out.
//
// One tap completes the line, another advances. The story is read, not spoken:
// there is no narration track and no voice key, by design — it is a storybook,
// and the words on the card are the whole of it. A line with no portrait still
// reads correctly.

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace JellyGarden.Scripts.Story
{
    public sealed class DialogueLine
    {
        public string StageCue = string.Empty;
        public string Speaker = string.Empty;
        public string Text = string.Empty;

        public DialogueLine() { }

        public DialogueLine(string speaker, string text)
        {
            Speaker = speaker ?? string.Empty;
            Text = text ?? string.Empty;
        }
    }

    public sealed class DialogueSystem : MonoBehaviour
    {
        public static event Action<DialogueLine> LineStarted;
        /// <summary>Characters per second while a line types itself out.</summary>
        private const float TypeSpeed = 45f;

        #region The card's measurements

        // The card was a fixed 400 units tall with a body inset 100 from its top
        // and 70 from its bottom, which is room for three lines of type at size
        // 54 — and the median line in this game's dialogue is 79 characters,
        // which wraps to five. Level 157's sabotage beat put "Not broken." under
        // the tap hint and "Opened." on the black BELOW the card. 185 of the 354
        // lines the game speaks are long enough to do it.
        //
        // So the card is sized to its line: it grows upward from the same bottom
        // edge, and only when even the tallest card cannot hold the line does the
        // type shrink. Every number below is what the fixed card already used, so
        // a short line draws exactly as it did.

        private const float BoxSidePadding = 50f;
        private const float BoxBottom = 90f;
        private const float BoxMinHeight = 400f;

        private const float BodyTopInset = 100f;
        private const float BodyBottomInset = 70f;
        private const float BodyRightInset = 50f;
        private const float BodyLeftWithPortrait = 350f;
        private const float BodyLeftAlone = 60f;

        /// <summary>
        /// How far the portrait's centre sits below the card's top edge. It is
        /// where the middle-anchored bust landed on the old 400-unit card
        /// (200 + 34), kept exactly so nothing moves for a short line — but
        /// measured from the TOP now, so the face stays beside the name plate
        /// and the first line as the card grows rather than drifting to the
        /// middle of a tall one.
        /// </summary>
        private const float BustBelowCardTop = 166f;

        /// <summary>
        /// How much room above the card belongs to the Skip button. Skip is
        /// anchored 60 below the top and is 96 tall, so it ends at 156; the rest
        /// is the gap that keeps the card from touching it.
        /// </summary>
        private const float SkipClearance = 196f;

        private const int BodySizeMax = 54;

        /// <summary>
        /// The floor the type may shrink to. Only the longest handful of lines
        /// reach it — the game's longest is 345 characters — and 34 against the
        /// speaker plate's own 26 is still the larger of the two sizes on the
        /// card.
        /// </summary>
        private const int BodySizeMin = 34;

        #endregion

        private static DialogueSystem _instance;

        private Canvas _canvas;
        private CanvasGroup _group;
        private RectTransform _root;
        private RectTransform _box;
        private Image _portrait;
        private RectTransform _bust;
        private RectTransform _plate;
        private Text _speaker;
        private Text _body;
        private Text _tapHint;
        private Button _skip;

        private readonly List<DialogueLine> _queue = new List<DialogueLine>();

        /// <summary>
        /// The line the card is currently sized for, and where its text starts.
        /// Kept so the fit can be taken again once the canvas has a width: on
        /// the very first frame of a beat the root rect is still zero, and a
        /// measurement against a zero-width box is not a measurement.
        /// </summary>
        private string _fitText = string.Empty;
        private float _fitLeft = BodyLeftWithPortrait;
        private bool _refitPending;

        private float _lineOverflow;
        private float _cardHeight = BoxMinHeight;

        private int _index = -1;
        private float _lineStartedAt;
        private bool _lineComplete;
        private Action _onFinished;
        private bool _playing;

        public static bool IsPlaying
        {
            get { return _instance != null && _instance._playing; }
        }

        public static bool IsTyping => IsPlaying && !_instance._lineComplete;

        #region What the fit came out at

        // Three read-only numbers so a test can ask whether the line on screen
        // actually fits the card it is on, rather than asking whether the card
        // was built. Same shape as StoryBoardTheme.LastBackdrop.

        /// <summary>
        /// How far the line spills past its box, in card units. Zero or less when
        /// it fits — which it must, for every line the game speaks.
        /// </summary>
        public static float LineOverflow
        {
            get { return _instance == null ? 0f : _instance._lineOverflow; }
        }

        /// <summary>The card's height in card units, for the line on screen.</summary>
        public static float CardHeight
        {
            get { return _instance == null ? 0f : _instance._cardHeight; }
        }

        /// <summary>The size the line is drawn at, after any shrink to fit.</summary>
        public static int LineFontSize
        {
            get { return _instance == null ? 0 : _instance._body.fontSize; }
        }

        /// <summary>The floor the type may shrink to, for a test to compare against.</summary>
        public static int SmallestLineFontSize
        {
            get { return BodySizeMin; }
        }

        /// <summary>The height a card holding a short line stays at.</summary>
        public static float ShortestCardHeight
        {
            get { return BoxMinHeight; }
        }

        #endregion

        /// <summary>Plays a sequence of lines, then calls back. Skippable.</summary>
        public static void Play(IList<DialogueLine> lines, Action onFinished, bool showSkip = true)
        {
            if (lines == null || lines.Count == 0)
            {
                if (onFinished != null) onFinished();
                return;
            }

            Ensure();
            _instance.Begin(lines, onFinished, showSkip);
        }

        private static void Ensure()
        {
            if (_instance != null) return;

            var go = new GameObject("~StoryDialogue");
            StoryObjects.Persist(go);
            _instance = go.AddComponent<DialogueSystem>();
            _instance.Build();
        }

        #region Build

        private void Build()
        {
            _canvas = StoryUi.CreateOverlayCanvas("DialogueCanvas", 850);
            _canvas.transform.SetParent(transform, false);

            RectTransform root = StoryUi.Stretch(StoryUi.CreateRect("Root", _canvas.transform));
            _group = root.gameObject.AddComponent<CanvasGroup>();
            _root = root;

            StoryUi.CreateFullScreenButton("Advance", root, new Color(0f, 0f, 0f, 0.001f), Advance);

            RectTransform box = StoryUi.CreateRect("Box", root);
            StoryUi.BottomAnchored(box, BoxBottom, BoxMinHeight, BoxSidePadding);
            _box = box;

            // A rim is two nested rounded rects, never an Outline: Outline offsets
            // the whole quad and leaves the corners square. Gold outside, cream
            // face inside, the same pair every Royal panel in this game is made of.
            Image rim = StoryUi.CreateImage("Rim", box, StoryUi.RoundedSprite(34),
                StorybookUi.Enabled ? StorybookUi.Gold : StoryUi.RoyalGoldDeep);
            rim.type = Image.Type.Sliced;
            StoryUi.Stretch(rim.rectTransform);

            Image face = StoryUi.CreateImage("Face", box, StoryUi.RoundedSprite(28),
                StorybookUi.Enabled ? StorybookUi.Paper : StoryUi.RoyalCream);
            face.type = Image.Type.Sliced;
            StoryUi.Stretch(face.rectTransform, 9f);

            // The portrait is round, ringed in gold, and rides ABOVE the card's
            // top edge. That overlap is what stops the card reading as a form:
            // the speaker breaks the box rather than sitting inside it.
            //
            // Anchored to the card's TOP, not its middle: the card grows upward
            // to hold a long line, and a middle-anchored face drifts away from
            // the name plate it belongs to. BustBelowCardTop is where the middle
            // anchor put it on the old fixed card.
            _bust = StoryUi.CreateRect("Bust", box);
            _bust.anchorMin = new Vector2(0f, 1f);
            _bust.anchorMax = new Vector2(0f, 1f);
            _bust.pivot = new Vector2(0f, 0.5f);
            _bust.anchoredPosition = new Vector2(30f, -BustBelowCardTop);
            _bust.sizeDelta = new Vector2(260f, 260f);

            Image ring = StoryUi.CreateImage("Ring", _bust, StoryUi.CircleSprite,
                StorybookUi.Enabled ? StorybookUi.Gold : StoryUi.RoyalGoldDeep);
            StoryUi.Stretch(ring.rectTransform);

            // A Mask clips its CHILDREN, not itself — the first version put one on
            // the portrait, which has no children, so nothing was clipped and the
            // square painted background showed. The circle is the parent now and
            // the portrait is what it cuts.
            Image hole = StoryUi.CreateImage("Hole", _bust, StoryUi.CircleSprite, Color.white);
            StoryUi.Stretch(hole.rectTransform, 12f);
            var mask = hole.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            _portrait = StoryUi.CreateImage("Portrait", hole.rectTransform, null, Color.white);
            StoryUi.Stretch(_portrait.rectTransform);
            _portrait.preserveAspect = true;

            // The name sits on a gold plate rather than floating on the cream,
            // which is what separates a speaker from the line they are saying.
            _plate = StoryUi.CreateRect("NamePlate", box);
            RectTransform plate = _plate;
            plate.anchorMin = new Vector2(0f, 1f);
            plate.anchorMax = new Vector2(0f, 1f);
            plate.pivot = new Vector2(0f, 1f);
            plate.anchoredPosition = new Vector2(350f, -22f);
            plate.sizeDelta = new Vector2(280f, 64f);

            Image plateFill = StoryUi.CreateImage("PlateFill", plate,
                StoryUi.RoundedSprite(20), StorybookUi.Enabled ? StorybookUi.Blue : StoryUi.RoyalGoldDeep);
            plateFill.type = Image.Type.Sliced;
            StoryUi.Stretch(plateFill.rectTransform);

            _speaker = StoryUi.CreateText("Speaker", plate, "", 48, StoryUi.RoyalTitle,
                TextAnchor.MiddleCenter);
            StoryUi.Stretch(_speaker.rectTransform, 12f);
            _speaker.font = StoryUi.NameFont;
            _speaker.resizeTextForBestFit = true;
            _speaker.resizeTextMinSize = 26;
            _speaker.resizeTextMaxSize = 48;

            _body = StoryUi.CreateText("Body", box, "", BodySizeMax, StorybookUi.Enabled ? StorybookUi.Ink : StoryUi.Ink, TextAnchor.UpperLeft);
            _body.font = StoryUi.BodyFont;
            _body.lineSpacing = 1.08f;
            _body.rectTransform.anchorMin = new Vector2(0f, 0f);
            _body.rectTransform.anchorMax = new Vector2(1f, 1f);
            _body.rectTransform.offsetMin = new Vector2(BodyLeftWithPortrait, BodyBottomInset);
            _body.rectTransform.offsetMax = new Vector2(-BodyRightInset, -BodyTopInset);

            _tapHint = StoryUi.CreateText("TapHint", box, "tap to continue", 26, StoryUi.Muted,
                TextAnchor.LowerRight);
            _tapHint.rectTransform.anchorMin = new Vector2(0f, 0f);
            _tapHint.rectTransform.anchorMax = new Vector2(1f, 0f);
            _tapHint.rectTransform.pivot = new Vector2(0.5f, 0f);
            _tapHint.rectTransform.offsetMin = new Vector2(0f, 24f);
            _tapHint.rectTransform.offsetMax = new Vector2(-BodyRightInset, BodyBottomInset);

            _skip = StoryUi.CreateButton("Skip", root, "Skip", 30, Finish);
            RectTransform skipRect = _skip.GetComponent<RectTransform>();
            skipRect.anchorMin = new Vector2(1f, 1f);
            skipRect.anchorMax = new Vector2(1f, 1f);
            skipRect.pivot = new Vector2(1f, 1f);
            skipRect.anchoredPosition = new Vector2(-40f, -60f);
            skipRect.sizeDelta = new Vector2(180f, 96f);

            gameObject.SetActive(false);
        }

        #endregion

        #region Playback

        private void Begin(IList<DialogueLine> lines, Action onFinished, bool showSkip)
        {
            _queue.Clear();
            _queue.AddRange(lines);
            _onFinished = onFinished;
            _index = -1;
            _playing = true;

            gameObject.SetActive(true);
            _group.alpha = 1f;
            _group.blocksRaycasts = true;
            _skip.gameObject.SetActive(showSkip);

            Next();
        }

        private void Next()
        {
            _index++;
            if (_index >= _queue.Count)
            {
                Finish();
                return;
            }

            DialogueLine line = _queue[_index];
            _speaker.text = line.Speaker;
            _body.text = string.Empty;
            _lineStartedAt = StoryClock.Now;
            _lineComplete = false;

            Sprite portrait = StoryArt.Portrait(line.Speaker);
            _portrait.sprite = portrait;
            _portrait.color = portrait != null ? Color.white : new Color(1f, 1f, 1f, 0f);

            // A line with no portrait should not leave a hole where one would be.
            // The NAME PLATE moves, never the name's own rect: the text is
            // stretched inside the plate, and writing an offset onto it gave it a
            // negative height — which draws nothing at all, so the plate came up
            // empty. Same trap as the panel title that was inset 110 on a 210
            // plate and was invisible for weeks.
            float textLeft = portrait != null ? BodyLeftWithPortrait : BodyLeftAlone;
            _body.rectTransform.offsetMin = new Vector2(textLeft, BodyBottomInset);
            _plate.anchoredPosition = new Vector2(textLeft, -22f);
            _bust.gameObject.SetActive(portrait != null);

            // Sized from the WHOLE line, never from what the typewriter has put
            // on screen so far: fitting the partial text would grow the card a
            // row at a time under the reader's eyes.
            FitCardTo(line.Text, textLeft);
            LineStarted?.Invoke(line);
        }

        /// <summary>One tap finishes the typing; the next advances.</summary>
        private void Advance()
        {
            if (!_playing) return;

            if (!_lineComplete)
            {
                _body.text = _queue[_index].Text;
                _lineComplete = true;
                return;
            }

            Next();
        }

        private void Finish()
        {
            if (!_playing) return;
            _playing = false;

            _group.blocksRaycasts = false;
            gameObject.SetActive(false);

            Action done = _onFinished;
            _onFinished = null;
            if (done != null) done();
        }

        #region Fitting the card to the line

        /// <summary>
        /// Grows the card until the line fits inside it, and shrinks the type
        /// only when even the tallest card cannot hold it.
        ///
        /// The height is measured, not counted: `Text.cachedTextGeneratorForLayout`
        /// wraps the real string in the real font at the real width, which is the
        /// only thing that knows how many rows "The sluice gates above royal pool
        /// had been opened. Not broken. Opened." becomes.
        /// </summary>
        private void FitCardTo(string text, float textLeft)
        {
            _fitText = text ?? string.Empty;
            _fitLeft = textLeft;

            float rootWidth = _root.rect.width;
            float rootHeight = _root.rect.height;

            // First frame of a beat: the canvas has no size yet, and a card fitted
            // to a zero-width box is a card fitted to nothing. Update takes it
            // again as soon as there is something to measure against.
            if (rootWidth < 1f || rootHeight < 1f)
            {
                _refitPending = true;
                return;
            }

            _refitPending = false;

            float bodyWidth = rootWidth - 2f * BoxSidePadding - textLeft - BodyRightInset;
            float chrome = BodyTopInset + BodyBottomInset;
            float maxHeight = Mathf.Max(BoxMinHeight, rootHeight - BoxBottom - SkipClearance);

            int size = BodySizeMax;
            float bodyHeight = MeasuredBodyHeight(_fitText, bodyWidth, size);

            while (size > BodySizeMin && bodyHeight + chrome > maxHeight)
            {
                size -= 2;
                bodyHeight = MeasuredBodyHeight(_fitText, bodyWidth, size);
            }

            _body.fontSize = size;

            float height = Mathf.Clamp(bodyHeight + chrome, BoxMinHeight, maxHeight);
            _box.offsetMax = new Vector2(-BoxSidePadding, BoxBottom + height);

            _cardHeight = height;
            _lineOverflow = bodyHeight - (height - chrome);
        }

        /// <summary>How tall the line wraps to at one size, in the card's units.</summary>
        private float MeasuredBodyHeight(string text, float width, int size)
        {
            int was = _body.fontSize;
            _body.fontSize = size;

            TextGenerationSettings settings =
                _body.GetGenerationSettings(new Vector2(Mathf.Max(1f, width), 0f));
            settings.verticalOverflow = VerticalWrapMode.Overflow;

            float height =
                _body.cachedTextGeneratorForLayout.GetPreferredHeight(text, settings) /
                _body.pixelsPerUnit;

            _body.fontSize = was;
            return height;
        }

        #endregion

        private void Update()
        {
            Tick(StoryClock.Now);
        }

        /// <summary>
        /// The typewriter, driven by an explicit clock so tests can advance it
        /// rather than waiting on real seconds.
        /// </summary>
        public void Tick(float now)
        {
            // Taken here rather than in Update so an editor test — which has no
            // frame loop — can force the canvas, tick, and read the fit back.
            if (_refitPending && _playing) FitCardTo(_fitText, _fitLeft);

            if (!_playing || _lineComplete || _index < 0 || _index >= _queue.Count) return;

            string full = _queue[_index].Text;
            int shown = Mathf.FloorToInt((now - _lineStartedAt) * TypeSpeed);

            if (shown >= full.Length)
            {
                _body.text = full;
                _lineComplete = true;
                return;
            }

            _body.text = full.Substring(0, Mathf.Max(0, shown));
        }

        #endregion
    }
}
