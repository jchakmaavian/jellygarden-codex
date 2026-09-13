using UnityEngine;
using UnityEngine.UI;

namespace JellyGarden.Scripts.Story
{
    /// <summary>A read-only album of discoveries. Opening it never awards a clue or key.</summary>
    public static class StorybookCollection
    {
        private static GameObject _root;
        public static bool IsOpen => _root != null && _root.activeSelf;

        public static void Open()
        {
            Close();
            Canvas canvas = StoryUi.CreateOverlayCanvas("StorybookCollection", 470);
            _root = canvas.gameObject;
            StoryObjects.Persist(_root);
            canvas.GetComponent<CanvasScaler>().referenceResolution = new Vector2(360, 720);
            Image ground = StoryUi.CreateImage("AlbumGround", canvas.transform, null, StorybookUi.PaperShade);
            StoryUi.Stretch(ground.rectTransform);
            ground.raycastTarget = true;
            RectTransform frame = StoryCastleFrame.Create(canvas.transform);
            Text title = StorybookUi.Ribbon("AlbumRibbon", frame, "Collection", 29);
            StoryCastleUi.Place((RectTransform)title.transform.parent, 21, -31, 318, 69);
            Text intro = StoryCastleUi.Label("Introduction", frame, "Every discovery tells a story", 14,
                StorybookUi.Muted, false, TextAnchor.MiddleCenter);
            StoryCastleUi.Place(intro.rectTransform, 25, -111, 310, 26);
            Image card = StorybookUi.PaperCard("FirstClue", frame);
            StoryCastleUi.Place(card.rectTransform, 28, -162, 304, 276);
            bool discovered = StoryProgress.IsSceneSeen(StoryDatabase.GetArea(1).PayoffScene);
            Image clue = StoryUi.CreateImage("SunClue", card.transform,
                discovered ? StoryArt.ClueImage(1) : StoryUi.LockSprite, Color.white);
            clue.preserveAspect = true;
            StoryCastleUi.Place(clue.rectTransform, 64, -23, 176, 145);
            if (!discovered)
            {
                clue.color = Color.clear;
                RectTransform padlock = StorybookUi.Padlock(clue.transform);
                StoryUi.Stretch(padlock, 26);
            }
            Text label = StoryCastleUi.Label("ClueName", card.transform,
                discovered ? "The sun clue" : "Your first clue awaits", 24, StorybookUi.Ink,
                true, TextAnchor.MiddleCenter);
            StoryCastleUi.Place(label.rectTransform, 18, -178, 268, 42);
            Text detail = StoryCastleUi.Label("ClueSource", card.transform,
                discovered ? "Found at Castle Gates" : "Complete all four Castle Gates tasks", 13,
                StorybookUi.Muted, false, TextAnchor.MiddleCenter);
            StoryCastleUi.Place(detail.rectTransform, 14, -225, 276, 28);
            Text next = StoryCastleUi.Label("NextChapter", frame,
                "Six keys. One royal surprise.", 19, StorybookUi.Ink, true, TextAnchor.MiddleCenter);
            StoryCastleUi.Place(next.rectTransform, 25, -464, 310, 35);
            Text soon = StoryCastleUi.Label("ComingSoon", frame,
                "More discoveries in the next chapter", 13, StorybookUi.Muted, false, TextAnchor.MiddleCenter);
            StoryCastleUi.Place(soon.rectTransform, 25, -502, 310, 26);
        }

        public static void Close()
        {
            if (_root == null) return;
            _root.SetActive(false);
            if (Application.isPlaying) Object.Destroy(_root);
            else Object.DestroyImmediate(_root);
            _root = null;
        }
    }
}
