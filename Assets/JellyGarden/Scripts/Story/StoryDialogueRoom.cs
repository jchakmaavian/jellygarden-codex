// The room a dialogue is spoken over.
//
// `DialogueSystem` draws a card and nothing else, which is right whenever there
// is already a screen underneath it — a cutscene stage, the home room, a live
// board. Twice now a beat has fired with nothing behind it and the player has
// been handed a cream card floating on black:
//
//   - the opening, which grew its own backdrop for exactly this reason and
//     says so in its header;
//   - the sabotage line at level 157, which `StoryFlow.LaunchLevel` plays after
//     the shell has come down for the level and before the board has been
//     loaded, so the one moment the game names Grimm out loud was spoken into a
//     void.
//
// This is that backdrop, written once, so the third caller does not write it a
// third time. It owns only the room; `DialogueSystem` still owns the card, the
// portrait and the tap-to-advance.

using UnityEngine;
using UnityEngine.UI;

namespace JellyGarden.Scripts.Story
{
    public sealed class StoryDialogueRoom : MonoBehaviour
    {
        /// <summary>
        /// Below `DialogueSystem`'s 850 and below `CutsceneRunner`'s 820 stage,
        /// so a beat that already has a scene behind it is never covered by this.
        /// </summary>
        private const int SortingOrder = 810;

        private static StoryDialogueRoom _instance;

        private RectTransform _room;
        private int _areaShowing;

        /// <summary>The area whose room is on screen, or 0 when nothing is.</summary>
        public static int AreaShowing
        {
            get { return _instance == null ? 0 : _instance._areaShowing; }
        }

        /// <summary>
        /// Puts an area's room behind the dialogue card. Does nothing for an area
        /// the story does not carry — a missing room is not a reason to refuse to
        /// speak the line.
        /// </summary>
        public static void Show(int areaId)
        {
            if (areaId <= 0 || StoryDatabase.GetArea(areaId) == null) return;

            Ensure();
            _instance.Display(areaId);
        }

        public static void Hide()
        {
            if (_instance == null) return;

            _instance._areaShowing = 0;
            _instance.gameObject.SetActive(false);
        }

        private static void Ensure()
        {
            if (_instance != null) return;

            var go = new GameObject("~StoryDialogueRoom");
            StoryObjects.Persist(go);
            _instance = go.AddComponent<StoryDialogueRoom>();
            _instance.Build();
        }

        private void Build()
        {
            Canvas canvas = StoryUi.CreateOverlayCanvas("DialogueRoomCanvas", SortingOrder);
            canvas.transform.SetParent(transform, false);

            RectTransform root = StoryUi.Stretch(StoryUi.CreateRect("Root", canvas.transform));

            // A covering view overflows its parent by design, so the parent has to
            // mask or the painting draws outside the screen it belongs to.
            _room = StoryUi.Stretch(StoryUi.CreateRect("Room", root));
            _room.gameObject.AddComponent<RectMask2D>();

            // Enough to hold the card's cream and the white text against a bright
            // room, not enough to lose the room behind it. The same value the
            // opening measured.
            //
            // Built as a LATER SIBLING of the room rather than as its child:
            // AreaSceneView.Attach adds itself at the end of its parent, so a dim
            // inside the room would be drawn under the artwork it is meant to
            // hold down.
            Image dim = StoryUi.CreateImage("Dim", root, null, new Color(0.03f, 0.05f, 0.11f, 0.32f));
            StoryUi.Stretch(dim.rectTransform);

            gameObject.SetActive(false);
        }

        private void Display(int areaId)
        {
            gameObject.SetActive(true);

            if (_areaShowing == areaId) return;
            _areaShowing = areaId;

            foreach (Transform child in _room) Destroy(child.gameObject);
            AreaSceneView.Attach(_room, areaId, true);
        }
    }
}
