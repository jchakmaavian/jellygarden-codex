// Characters talking: a portrait, a name, and a line that types itself out.
//
// One tap completes the line, another advances. Narration plays through
// StoryAudio, which ducks the music underneath it. Everything is optional — a
// line with no portrait and no clip still reads correctly.

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace JellyGarden.Scripts.Story
{
    public sealed class DialogueLine
    {
        public string Speaker = string.Empty;
        public string Text = string.Empty;

        /// <summary>Key under Resources/Story/Audio/VO, without extension.</summary>
        public string VoiceKey = string.Empty;

        public DialogueLine() { }

        public DialogueLine(string speaker, string text, string voiceKey = null)
        {
            Speaker = speaker ?? string.Empty;
            Text = text ?? string.Empty;
            VoiceKey = voiceKey ?? string.Empty;
        }
    }

    public sealed class DialogueSystem : MonoBehaviour
    {
        /// <summary>Characters per second while a line types itself out.</summary>
        private const float TypeSpeed = 45f;

        private static DialogueSystem _instance;

        private Canvas _canvas;
        private CanvasGroup _group;
        private Image _portrait;
        private RectTransform _bust;
        private RectTransform _plate;
        private Text _speaker;
        private Text _body;
        private Text _tapHint;
        private Button _skip;

        private readonly List<DialogueLine> _queue = new List<DialogueLine>();
        private int _index = -1;
        private float _lineStartedAt;
        private bool _lineComplete;
        private Action _onFinished;
        private bool _playing;

        public static bool IsPlaying
        {
            get { return _instance != null && _instance._playing; }
        }

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

            StoryUi.CreateFullScreenButton("Advance", root, new Color(0f, 0f, 0f, 0.001f), Advance);

            RectTransform box = StoryUi.CreateRect("Box", root);
            StoryUi.BottomAnchored(box, 90f, 400f, 50f);

            // A rim is two nested rounded rects, never an Outline: Outline offsets
            // the whole quad and leaves the corners square. Gold outside, cream
            // face inside, the same pair every Royal panel in this game is made of.
            Image rim = StoryUi.CreateImage("Rim", box, StoryUi.RoundedSprite(34),
                StoryUi.RoyalGoldDeep);
            rim.type = Image.Type.Sliced;
            StoryUi.Stretch(rim.rectTransform);

            Image face = StoryUi.CreateImage("Face", box, StoryUi.RoundedSprite(28),
                StoryUi.RoyalCream);
            face.type = Image.Type.Sliced;
            StoryUi.Stretch(face.rectTransform, 9f);

            // The portrait is round, ringed in gold, and rides ABOVE the card's
            // top edge. That overlap is what stops the card reading as a form:
            // the speaker breaks the box rather than sitting inside it.
            _bust = StoryUi.CreateRect("Bust", box);
            _bust.anchorMin = new Vector2(0f, 0.5f);
            _bust.anchorMax = new Vector2(0f, 0.5f);
            _bust.pivot = new Vector2(0f, 0.5f);
            _bust.anchoredPosition = new Vector2(30f, 34f);
            _bust.sizeDelta = new Vector2(260f, 260f);

            Image ring = StoryUi.CreateImage("Ring", _bust, StoryUi.CircleSprite,
                StoryUi.RoyalGoldDeep);
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
                StoryUi.RoundedSprite(20), StoryUi.RoyalGoldDeep);
            plateFill.type = Image.Type.Sliced;
            StoryUi.Stretch(plateFill.rectTransform);

            _speaker = StoryUi.CreateText("Speaker", plate, "", 48, StoryUi.RoyalTitle,
                TextAnchor.MiddleCenter);
            StoryUi.Stretch(_speaker.rectTransform, 12f);
            _speaker.font = StoryUi.NameFont;
            _speaker.resizeTextForBestFit = true;
            _speaker.resizeTextMinSize = 26;
            _speaker.resizeTextMaxSize = 48;

            _body = StoryUi.CreateText("Body", box, "", 54, StoryUi.Ink, TextAnchor.UpperLeft);
            _body.font = StoryUi.BodyFont;
            _body.lineSpacing = 1.08f;
            _body.rectTransform.anchorMin = new Vector2(0f, 0f);
            _body.rectTransform.anchorMax = new Vector2(1f, 1f);
            _body.rectTransform.offsetMin = new Vector2(350f, 70f);
            _body.rectTransform.offsetMax = new Vector2(-50f, -100f);

            _tapHint = StoryUi.CreateText("TapHint", box, "tap to continue", 26, StoryUi.Muted,
                TextAnchor.LowerRight);
            _tapHint.rectTransform.anchorMin = new Vector2(0f, 0f);
            _tapHint.rectTransform.anchorMax = new Vector2(1f, 0f);
            _tapHint.rectTransform.pivot = new Vector2(0.5f, 0f);
            _tapHint.rectTransform.offsetMin = new Vector2(0f, 24f);
            _tapHint.rectTransform.offsetMax = new Vector2(-50f, 70f);

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
            float textLeft = portrait != null ? 350f : 60f;
            _body.rectTransform.offsetMin = new Vector2(textLeft, 70f);
            _plate.anchoredPosition = new Vector2(textLeft, -22f);
            _bust.gameObject.SetActive(portrait != null);

            StoryAudio.StopVoice();
            if (!string.IsNullOrEmpty(line.VoiceKey))
                StoryAudio.PlayVoice(line.VoiceKey);
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

            StoryAudio.StopVoice();
            _group.blocksRaycasts = false;
            gameObject.SetActive(false);

            Action done = _onFinished;
            _onFinished = null;
            if (done != null) done();
        }

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
