// The first three minutes, as specified in 00-OPENING.md.
//
// Rule zero from that document is gameplay before story: no logo hold, no menu,
// no cinematic to sit through. The app opens on a board, and the story arrives
// after the first win — never before it. So this plays once, when the player
// comes back from level 1, and not a moment earlier.
//
// The beat sheet it follows:
//   the board dissolves into the real gate
//   the portcullis grinds up and Leo rides through, alone
//   CASTLE REVEAL, with Alfred's four lines over it
//   Leo, quiet: "...Freya?"  — no answer
//   Mimi steps out of the dark. The only welcome he gets.
//   title card, then the map
//
// The hook is one unanswered word. The spec is explicit that there is no second
// line and nobody explains who she is, so there is deliberately no room here to
// add one.

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace JellyGarden.Scripts.Story
{
    public sealed class OpeningSequence : MonoBehaviour
    {
        /// <summary>The area whose first win triggers the opening.</summary>
        private const int OpeningArea = 1;

        private static OpeningSequence _instance;

        private Canvas _canvas;
        private CanvasGroup _group;
        private Image _backdrop;
        private Image _mimi;
        private Text _title;
        private bool _playing;

        public static bool IsPlaying
        {
            get { return _instance != null && _instance._playing; }
        }

        /// <summary>
        /// Plays the opening if it is owed. Returns true when it took over — the
        /// caller's continuation then runs after it, not alongside it.
        /// </summary>
        public static bool PlayIfDue(int areaId, Action onDone)
        {
            if (areaId != OpeningArea) return false;
            if (StoryProgress.OpeningSeen) return false;

            // Gameplay before story: the opening is owed only once a level has
            // actually been won, never on the way into one.
            if (StoryProgress.LevelsBeatenCount < 1) return false;

            Ensure();
            _instance.Begin(onDone);
            return true;
        }

        private static void Ensure()
        {
            if (_instance != null) return;

            var go = new GameObject("~StoryOpening");
            StoryObjects.Persist(go);
            _instance = go.AddComponent<OpeningSequence>();
            _instance.Build();
        }

        private void Build()
        {
            _canvas = StoryUi.CreateOverlayCanvas("OpeningCanvas", 880);
            _canvas.transform.SetParent(transform, false);

            RectTransform root = StoryUi.Stretch(StoryUi.CreateRect("Root", _canvas.transform));
            _group = root.gameObject.AddComponent<CanvasGroup>();

            Image black = StoryUi.CreateImage("Black", root, null, Color.black);
            StoryUi.Stretch(black.rectTransform);

            // The castle Leo rides into. Desaturated hard, per the direction note
            // — greys, dead green, rust — which is what the generated art already
            // is, so it is shown as it came.
            _backdrop = StoryUi.CreateImage("CastleReveal", root,
                StoryArt.Load("backdrop_castle"), Color.white);
            StoryUi.Stretch(_backdrop.rectTransform);
            _backdrop.preserveAspect = true;

            // Mimi arrives late and stays. The only welcome he gets.
            _mimi = StoryUi.CreateImage("Mimi", root, StoryArt.Load("portrait_mimi"), Color.white);
            _mimi.rectTransform.anchorMin = new Vector2(1f, 0f);
            _mimi.rectTransform.anchorMax = new Vector2(1f, 0f);
            _mimi.rectTransform.pivot = new Vector2(1f, 0f);
            _mimi.rectTransform.anchoredPosition = new Vector2(-40f, 620f);
            _mimi.rectTransform.sizeDelta = new Vector2(360f, 360f);
            _mimi.preserveAspect = true;
            _mimi.gameObject.SetActive(false);

            _title = StoryUi.CreateText("TitleCard", root, "King Leo", 110, Color.white,
                TextAnchor.MiddleCenter);
            StoryUi.Stretch(_title.rectTransform);
            _title.fontStyle = FontStyle.Bold;
            _title.gameObject.SetActive(false);

            gameObject.SetActive(false);
        }

        private void Begin(Action onDone)
        {
            _playing = true;
            gameObject.SetActive(true);
            _group.alpha = 1f;
            _group.blocksRaycasts = true;
            _mimi.gameObject.SetActive(false);
            _title.gameObject.SetActive(false);

            DialogueSystem.Play(BuildLines(), () => ShowTitleThen(onDone));
        }

        /// <summary>
        /// The opening's lines, in the order of the beat sheet. Alfred is
        /// voice-over only for the whole of Area 001 — he does not appear in
        /// person until Area 005 — so his lines carry no portrait, and the
        /// emptiness is the point.
        /// </summary>
        private List<DialogueLine> BuildLines()
        {
            var lines = new List<DialogueLine>
            {
                new DialogueLine("Alfred", "The King had been gone eleven weeks.", "alfred_open_01"),
                new DialogueLine("Alfred", "He had left the kingdom in good hands. So he was told.", "alfred_open_02"),
                new DialogueLine("Alfred", "He expected trumpets. He got weeds.", "alfred_open_03"),

                // 1:25. The hook. One word, into an empty courtyard, no reply.
                new DialogueLine("Leo", "...Freya?"),

                new DialogueLine("Alfred", "No one had thought to tell him. That, in itself, was the answer.", "alfred_open_04")
            };

            return lines;
        }

        /// <summary>
        /// Mimi appears on Leo's unanswered line, and the title card follows the
        /// last of Alfred's. Both are driven off the dialogue's own progress
        /// rather than a timer, so they cannot drift out of sync with a reader
        /// who taps quickly.
        /// </summary>
        private void ShowTitleThen(Action onDone)
        {
            _mimi.gameObject.SetActive(true);
            _title.gameObject.SetActive(true);

            DialogueSystem.Play(new List<DialogueLine>
            {
                new DialogueLine("Leo", "Where is everyone, cat?")
            }, () => Finish(onDone));
        }

        private void Finish(Action onDone)
        {
            _playing = false;
            StoryProgress.MarkOpeningSeen();

            _group.blocksRaycasts = false;
            gameObject.SetActive(false);

            if (onDone != null) onDone();
        }
    }
}
