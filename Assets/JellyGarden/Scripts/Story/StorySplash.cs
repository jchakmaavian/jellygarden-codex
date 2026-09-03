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
                // The splash has a picture of its own rather than borrowing the
                // arc's board backdrop, which was only ever standing in: that one
                // is the ground a board is played on and is painted dark to suit
                // it, which is the opposite of what a title screen wants.
                { "main_menu_banner", "splash_background" },
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

        // The inlay, in reference points, outside in: a dark edge, the gold
        // sitting inside it, then a bronze line where the gold meets the field.
        private const float EdgeThickness = 3f;
        private const float InlayThickness = 5f;
        private const float BronzeThickness = 3f;

        /// <summary>Where the button's centre sits, as a fraction up the screen.</summary>
        private const float PlayHeightFrac = 0.245f;

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

                // Down onto the path.
                //
                // The kit put this button half way up a flat candy banner, where
                // there was nothing behind it to collide with. The splash now has
                // a castle, and that is exactly where its gate and towers are —
                // the one part of the painting with detail in it. A quarter of the
                // way up puts the button on the open lawn instead.
                //
                // A fraction of the canvas rather than a pixel offset, because the
                // reference is a 9:19.5 phone and a 4:3 view hands this canvas far
                // less height; a fixed offset that clears the castle on one lands
                // back on it on the other.
                rect.anchorMin = new Vector2(0.5f, PlayHeightFrac);
                rect.anchorMax = new Vector2(0.5f, PlayHeightFrac);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = Vector2.zero;

                // The gold inlay, outside in.
                //
                // The button this replaces was three flat fills stacked on each
                // other — dark base, lighter face, lighter gloss across the top
                // half — and every boundary between them was a hard edge between
                // two solid colours, so it read as three rectangles rather than
                // one lit object. The gloss stopped dead across the middle,
                // exactly where the eye lands to read the word.
                //
                // Here the gold runs INSIDE a dark edge rather than wrapping the
                // button, so it reads as inlaid into the green instead of as a
                // second rectangle around it, and the only light on the field is
                // a ramp that fades out rather than stopping. No hard edge
                // anywhere on the face.
                image.sprite = StoryUi.RoundedSprite(30);
                image.type = Image.Type.Sliced;
                image.color = StoryUi.RoyalTealInk;

                Image goldRing = StoryUi.CreateImage("StoryInlayGold", rect,
                    StoryUi.RoundedSprite(28), StoryUi.RoyalGold);
                StoryUi.Stretch(goldRing.rectTransform, EdgeThickness);
                goldRing.type = Image.Type.Sliced;
                goldRing.raycastTarget = false;

                Image bronzeRing = StoryUi.CreateImage("StoryInlayBronze", rect,
                    StoryUi.RoundedSprite(26), StoryUi.RoyalGoldDeep);
                StoryUi.Stretch(bronzeRing.rectTransform, EdgeThickness + InlayThickness);
                bronzeRing.type = Image.Type.Sliced;
                bronzeRing.raycastTarget = false;

                Image field = StoryUi.CreateImage(DressedMarker, rect,
                    StoryUi.RoundedSprite(24), StoryUi.RoyalGreen);
                StoryUi.Stretch(field.rectTransform,
                    EdgeThickness + InlayThickness + BronzeThickness);
                field.type = Image.Type.Sliced;
                field.raycastTarget = false;

                // The ramp is a rectangle and the field is rounded, so the field
                // clips it — otherwise its square top corners show past the
                // curve. showMaskGraphic keeps the green itself drawn.
                field.gameObject.AddComponent<Mask>().showMaskGraphic = true;

                Image sheen = StoryUi.CreateImage("StorySheen", field.rectTransform,
                    StoryUi.SheenSprite, new Color(1f, 1f, 1f, 0.30f));
                StoryUi.Stretch(sheen.rectTransform);
                sheen.raycastTarget = false;

                // The kit's word "Play" is painted into the sprite that was just
                // replaced, so the label is a real Text — in the display face,
                // because Arial under a painted gold wordmark is what made this
                // button read as placeholder UI.
                Text label = StoryUi.CreateText("StoryLabel", rect, "Play", 52,
                    StoryUi.RoyalTitle, TextAnchor.MiddleCenter);
                label.font = StoryUi.DisplayFont;
                StoryUi.Stretch(label.rectTransform, 10f);
                label.raycastTarget = false;
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = 18;
                label.resizeTextMaxSize = 52;
                var outline = label.gameObject.AddComponent<Outline>();
                outline.effectColor = StoryUi.RoyalGreenDark;
                outline.effectDistance = new Vector2(3f, -3f);

                return true;
            }

            return false;
        }

    }
}
