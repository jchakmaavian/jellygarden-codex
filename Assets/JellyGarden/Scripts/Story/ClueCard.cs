// The clue card.
//
// Three seconds, one image, the clue text, and nothing else. The story bible is
// explicit that the clue is never explained — the player gets a torn sentence
// and draws the line themselves — so there is deliberately no room here for a
// caption, a hint or a "that's suspicious" line.

using System;
using UnityEngine;
using UnityEngine.UI;

namespace JellyGarden.Scripts.Story
{
    public sealed class ClueCard : MonoBehaviour
    {
        private const float HoldSeconds = 3f;
        private const float FadeSeconds = 0.35f;

        /// <summary>Long enough to read the card, short enough not to feel stuck.</summary>
        private const float MinimumBeforeTapDismiss = 0.4f;

        private static ClueCard _instance;

        private CanvasGroup _group;
        private Text _text;
        private Image _art;
        private Action _onDone;
        private float _shownAt;
        private float _dismissAt;
        private bool _showing;

        public static bool IsShowing
        {
            get { return _instance != null && _instance._showing; }
        }

        /// <summary>Shows an area's clue once. Calls back when the card is gone.</summary>
        public static void Show(int areaId, Action onDone)
        {
            StoryArea area = StoryDatabase.GetArea(areaId);
            if (area == null || string.IsNullOrEmpty(area.Clue))
            {
                if (onDone != null) onDone();
                return;
            }

            Ensure();
            StoryProgress.MarkClueSeen(areaId);
            _instance.Display(area, onDone);
        }

        private static void Ensure()
        {
            if (_instance != null) return;

            var go = new GameObject("~StoryClueCard");
            StoryObjects.Persist(go);
            _instance = go.AddComponent<ClueCard>();
            _instance.Build();
        }

        private void Build()
        {
            Canvas canvas = StoryUi.CreateOverlayCanvas("ClueCanvas", 800);
            canvas.transform.SetParent(transform, false);

            RectTransform root = StoryUi.Stretch(StoryUi.CreateRect("Root", canvas.transform));
            _group = root.gameObject.AddComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;

            StoryUi.CreateFullScreenButton("Dismiss", root, new Color(0f, 0f, 0f, 0.88f), Dismiss);

            RectTransform card = StoryUi.CreateRect("Card", root);
            card.anchorMin = new Vector2(0.5f, 0.5f);
            card.anchorMax = new Vector2(0.5f, 0.5f);
            card.sizeDelta = new Vector2(900f, 1100f);

            _art = StoryUi.CreateImage("Art", card, null, Color.white);
            _art.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            _art.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            _art.rectTransform.pivot = new Vector2(0.5f, 1f);
            _art.rectTransform.sizeDelta = new Vector2(760f, 620f);
            _art.preserveAspect = true;

            _text = StoryUi.CreateText("Clue", card, "", 42, Color.white,
                TextAnchor.UpperCenter);
            _text.rectTransform.anchorMin = new Vector2(0f, 0f);
            _text.rectTransform.anchorMax = new Vector2(1f, 1f);
            _text.rectTransform.offsetMin = new Vector2(60f, 40f);
            _text.rectTransform.offsetMax = new Vector2(-60f, -660f);
            _text.fontStyle = FontStyle.Italic;

            gameObject.SetActive(false);
        }

        private void Display(StoryArea area, Action onDone)
        {
            _onDone = onDone;
            _text.text = area.Clue;

            Sprite art = StoryArt.ClueImage(area.Id);
            _art.sprite = art;
            _art.color = art != null ? Color.white : new Color(1f, 1f, 1f, 0f);

            gameObject.SetActive(true);
            _group.alpha = 1f;
            _group.blocksRaycasts = true;
            _showing = true;
            _shownAt = StoryClock.Now;
            _dismissAt = _shownAt + HoldSeconds;

            StoryAudio.PlayClueVoice(area.Id);
        }

        private void Dismiss()
        {
            // A tap in the first fraction of a second is almost always the same
            // tap that opened the card.
            if (StoryClock.Now - _shownAt < MinimumBeforeTapDismiss) return;
            _dismissAt = Mathf.Min(_dismissAt, StoryClock.Now);
        }

        private void Update()
        {
            Tick(StoryClock.Now);
        }

        /// <summary>
        /// The card's whole time behaviour, driven by an explicit clock so tests
        /// can advance it instead of sleeping through it.
        /// </summary>
        public void Tick(float now)
        {
            if (!_showing) return;

            float since = now - _dismissAt;
            if (since < 0f) return;

            if (since < FadeSeconds)
            {
                _group.alpha = 1f - since / FadeSeconds;
                return;
            }

            _showing = false;
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            gameObject.SetActive(false);

            Action done = _onDone;
            _onDone = null;
            if (done != null) done();
        }
    }
}
