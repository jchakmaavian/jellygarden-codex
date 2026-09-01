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
            StoryUi.BottomAnchored(box, 90f, 560f, 50f);

            Image backdrop = StoryUi.CreateImage("Backdrop", box, StoryUi.PanelSprite,
                new Color(0.96f, 0.91f, 0.80f, 0.97f));
            backdrop.type = Image.Type.Sliced;
            StoryUi.Stretch(backdrop.rectTransform);

            _portrait = StoryUi.CreateImage("Portrait", box, null, Color.white);
            _portrait.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            _portrait.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            _portrait.rectTransform.pivot = new Vector2(0f, 0.5f);
            _portrait.rectTransform.anchoredPosition = new Vector2(40f, 0f);
            _portrait.rectTransform.sizeDelta = new Vector2(280f, 280f);
            _portrait.preserveAspect = true;

            _speaker = StoryUi.CreateText("Speaker", box, "", 40, StoryUi.Gold);
            _speaker.rectTransform.anchorMin = new Vector2(0f, 1f);
            _speaker.rectTransform.anchorMax = new Vector2(1f, 1f);
            _speaker.rectTransform.pivot = new Vector2(0.5f, 1f);
            _speaker.rectTransform.offsetMin = new Vector2(350f, -110f);
            _speaker.rectTransform.offsetMax = new Vector2(-50f, -46f);
            _speaker.fontStyle = FontStyle.Bold;

            _body = StoryUi.CreateText("Body", box, "", 38, StoryUi.Ink, TextAnchor.UpperLeft);
            _body.rectTransform.anchorMin = new Vector2(0f, 0f);
            _body.rectTransform.anchorMax = new Vector2(1f, 1f);
            _body.rectTransform.offsetMin = new Vector2(350f, 90f);
            _body.rectTransform.offsetMax = new Vector2(-50f, -120f);

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
            _body.rectTransform.offsetMin = new Vector2(portrait != null ? 350f : 60f, 90f);
            _speaker.rectTransform.offsetMin = new Vector2(portrait != null ? 350f : 60f, -110f);

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
