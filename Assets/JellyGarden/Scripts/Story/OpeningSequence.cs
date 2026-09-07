// The opening, as specified in 00-OPENING.md.
//
// Rule zero from that document is gameplay before story: no logo hold, no menu,
// no cinematic to sit through. The app opens on a board, and the story arrives
// after the first win — never before it. So this plays once, when the player
// comes back from level 1, and not a moment earlier.
//
// It is DIALOGUE, not a cutscene. The lines are spoken over whatever the player
// is already looking at, on the same bottom card with a portrait that every
// other conversation in the game uses.
//
// It used to be a full-screen takeover: black behind a letterboxed castle
// plate, a 360px Mimi pasted into the corner and the title in white across the
// middle. On a phone that read as the game going dark and showing a slideshow —
// the castle plate is `preserveAspect` on a 9:19.5 screen, so most of what the
// player saw was the black behind it. Nothing was gained by owning the whole
// screen that the card does not do better, so the screen is left alone.
//
// The beat the lines carry:
//   Alfred, over the room: three lines of what the kingdom has come to
//   Leo, quiet: "...Freya?"  — no answer
//   Alfred: the answer nobody gave him
//   Leo: "Where is everyone, cat?"  — Mimi is there, saying nothing
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

        /// <summary>
        /// The room the lines are spoken over.
        ///
        /// The shell is closed for the level the player just won, so without this
        /// the dialogue card floats on nothing — which is the black screen the
        /// old cutscene was blamed for and which survived deleting it.
        ///
        /// It is the Castle Gates room itself, not a separate cinematic plate:
        /// Leo is standing at his own gate saying her name, and the same picture
        /// visibly repairs as the area is restored. Covering rather than
        /// letterboxed, because bars are what made the old version read as a
        /// slideshow.
        /// </summary>
        private void Build()
        {
            // Below DialogueSystem's 850 so the card always draws on top.
            _canvas = StoryUi.CreateOverlayCanvas("OpeningBackdropCanvas", 840);
            _canvas.transform.SetParent(transform, false);

            RectTransform root = StoryUi.Stretch(
                StoryUi.CreateRect("Room", _canvas.transform));

            // A covering view overflows its parent by design, so the parent has
            // to mask or the room paints outside the screen it belongs to.
            root.gameObject.AddComponent<RectMask2D>();
            AreaSceneView.Attach(root, OpeningArea, true);

            // Enough to hold the card's cream and the white text against a bright
            // midday castle, not enough to lose the gate behind it.
            Image dim = StoryUi.CreateImage("Dim", root, null, new Color(0.03f, 0.05f, 0.11f, 0.32f));
            StoryUi.Stretch(dim.rectTransform);

            gameObject.SetActive(false);
        }

        /// <summary>
        /// `DialogueSystem` owns the card, the portrait and the tap-to-advance;
        /// this owns only the room behind them.
        /// </summary>
        private void Begin(Action onDone)
        {
            _playing = true;
            gameObject.SetActive(true);
            DialogueSystem.Play(BuildLines(), () => Finish(onDone));
        }

        /// <summary>
        /// The opening's lines, in the order of the beat sheet.
        ///
        /// `DialogueSystem` resolves the portrait from the speaker's name, and
        /// `portrait_alfred` exists — so Alfred's face is on the card here even
        /// though 00-OPENING.md has him as voice-over only until Area 005. That
        /// is a deliberate departure: a card with a name and no face reads as
        /// missing art rather than as absence. Delete the portrait, or speak his
        /// lines as an unnamed narrator, if the emptiness is wanted back.
        /// </summary>
        private List<DialogueLine> BuildLines()
        {
            var lines = new List<DialogueLine>
            {
                new DialogueLine("Alfred", "The King had been gone eleven weeks.", "alfred_open_01"),
                new DialogueLine("Alfred", "He had left the kingdom in good hands. So he was told.", "alfred_open_02"),
                new DialogueLine("Alfred", "He expected trumpets. He got weeds.", "alfred_open_03"),

                // The hook. One word, into an empty courtyard, no reply.
                new DialogueLine("Leo", "...Freya?"),

                new DialogueLine("Alfred", "No one had thought to tell him. That, in itself, was the answer.", "alfred_open_04"),

                new DialogueLine("Leo", "Where is everyone, cat?"),

                // Mimi is the only welcome he gets, and she says nothing, being a
                // cat. The beat is hers so that her portrait is what the player
                // is left looking at; the ellipsis is the whole of her answer.
                new DialogueLine("Mimi", "...")
            };

            return lines;
        }

        private void Finish(Action onDone)
        {
            _playing = false;
            StoryProgress.MarkOpeningSeen();
            gameObject.SetActive(false);

            if (onDone != null) onDone();
        }
    }
}
