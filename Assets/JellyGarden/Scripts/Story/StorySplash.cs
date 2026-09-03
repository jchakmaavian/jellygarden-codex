// The game opens on King Leo, not on Jelly Garden.
//
// The kit's first scene is `main`: a candy banner with the Jelly Garden wordmark
// over it and a Play button. That is the first thing anyone sees, and until now
// it was still selling somebody else's game.
//
// Nothing in that scene is edited and no kit texture is overwritten. The two
// Images are found by the name of the sprite they are already wearing and their
// sprites are swapped — exactly the move StoryBoardTheme makes on the board's
// backdrop. Delete this file, or set Enabled = false, and the kit's splash comes
// back untouched.
//
// Finding them by sprite name rather than by object name is deliberate: the
// banner's GameObject is called "Image", which is worth nothing to a reader and
// would match any stray Image somebody adds to the scene later. "The Image
// currently wearing main_menu_banner" is unambiguous and says what it means.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace JellyGarden.Scripts.Story
{
    public static class StorySplash
    {
        /// <summary>The whole feature, in one switch.</summary>
        public static bool Enabled = true;

        /// <summary>
        /// The kit sprite each slot wears, and the story art that replaces it.
        /// A story key with no art on disk leaves that slot alone.
        /// </summary>
        private static readonly Dictionary<string, string> Swaps =
            new Dictionary<string, string>
            {
                { "logo", "splash_logo" },
                { "main_menu_banner", "backdrop_castle" },
            };

        private static bool _reported;

        /// <summary>Called by StorySceneHooks on every scene load.</summary>
        public static void Install()
        {
            if (!Enabled) return;

            var swapped = new List<string>();

            foreach (Image image in
                     Object.FindObjectsByType<Image>(FindObjectsInactive.Include))
            {
                if (image == null || image.sprite == null) continue;

                string story;
                if (!Swaps.TryGetValue(image.sprite.name, out story)) continue;

                Sprite art = StoryArt.Load(story);
                if (art == null) continue;

                image.sprite = art;

                // The wordmark is a cutout with its own proportions and must not
                // be stretched to whatever rect the kit's logo happened to use.
                // The banner behind it is a backdrop and is meant to fill.
                if (image.sprite.name == "splash_logo" || story == "splash_logo")
                    image.preserveAspect = true;

                swapped.Add(story);
            }

            if (DressPlayButton()) swapped.Add("play button");

            if (swapped.Count > 0 && !_reported)
            {
                _reported = true;
                Debug.Log("[Story] splash dressed: " + string.Join(", ", swapped.ToArray()) +
                          " — the game opens on King Leo.");
            }
        }

        /// <summary>The kit sprite the splash's Play button wears.</summary>
        private const string KitPlaySprite = "play_main_menu_button";

        /// <summary>The marker child, so a second Install does not stack a second face.</summary>
        private const string DressedMarker = "StoryFace";

        /// <summary>
        /// Turns the kit's blue candy Play button into the story's green one.
        ///
        /// It is redressed in place rather than replaced, because the button's
        /// own onClick is what starts the game and rebuilding it would mean
        /// re-wiring that. The kit's word "Play" is painted INTO
        /// play_main_menu_button.png, so swapping the sprite loses the label —
        /// which is why a real Text is added on top. Both are additions; the
        /// sprite is the only thing overwritten, and only on the instance.
        /// </summary>
        private static bool DressPlayButton()
        {
            foreach (Image image in
                     Object.FindObjectsByType<Image>(FindObjectsInactive.Include))
            {
                if (image == null || image.sprite == null) continue;
                if (image.sprite.name != KitPlaySprite) continue;
                if (image.transform.Find(DressedMarker) != null) return false;

                var rect = image.rectTransform;

                // Base, face and gloss, the three layers that make GreenButton
                // read as pressable. Built here rather than by calling
                // GreenButton, which would make a whole new button.
                image.sprite = StoryUi.RoundedSprite(28);
                image.type = Image.Type.Sliced;
                image.color = StoryUi.RoyalGreenDark;

                Image face = StoryUi.CreateImage(DressedMarker, rect,
                    StoryUi.RoundedSprite(28), StoryUi.RoyalGreen);
                face.type = Image.Type.Sliced;
                face.raycastTarget = false;
                RectTransform faceRect = face.rectTransform;
                faceRect.anchorMin = Vector2.zero;
                faceRect.anchorMax = Vector2.one;
                faceRect.offsetMin = new Vector2(5f, 9f);
                faceRect.offsetMax = new Vector2(-5f, -5f);

                Image gloss = StoryUi.CreateImage("StoryGloss", rect,
                    StoryUi.RoundedSprite(28), StoryUi.RoyalGreenLight);
                gloss.type = Image.Type.Sliced;
                gloss.raycastTarget = false;
                RectTransform glossRect = gloss.rectTransform;
                glossRect.anchorMin = new Vector2(0f, 0.55f);
                glossRect.anchorMax = new Vector2(1f, 1f);
                glossRect.offsetMin = new Vector2(13f, 0f);
                glossRect.offsetMax = new Vector2(-13f, -10f);

                Text label = StoryUi.CreateText("StoryLabel", rect, "Play", 44,
                    StoryUi.RoyalTitle, TextAnchor.MiddleCenter);
                StoryUi.Stretch(label.rectTransform, 8f);
                label.fontStyle = FontStyle.Bold;
                label.raycastTarget = false;
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = 16;
                label.resizeTextMaxSize = 44;
                var outline = label.gameObject.AddComponent<Outline>();
                outline.effectColor = StoryUi.RoyalGreenDark;
                outline.effectDistance = new Vector2(2f, -2f);

                return true;
            }

            return false;
        }

    }
}
