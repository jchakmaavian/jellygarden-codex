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
                    {
                    image.preserveAspect = true;
                    if (StorybookUi.Enabled && image.transform.Find("AdventureTitle") == null)
                    {
                        image.enabled = false;
                        Text title = StoryUi.CreateText("AdventureTitle", image.transform, "Adventure of\nKing Leo", 72, StorybookUi.Ink, TextAnchor.MiddleCenter);
                        title.font = StorybookArt.Font;
                        StoryUi.Stretch(title.rectTransform);
                        title.resizeTextForBestFit = true;
                        title.resizeTextMinSize = 24;
                        title.resizeTextMaxSize = 72;
                    }
                }

                swapped.Add(story);
            }

            if (DressPlayButton()) swapped.Add("play button");
            if (DressLoadingScreen()) swapped.Add("loading screen");

            if (swapped.Count > 0 && !_reported)
            {
                _reported = true;
                Debug.Log("[Story] splash dressed: " + string.Join(", ", swapped.ToArray()) +
                          " — the game opens on King Leo.");
            }
        }

        /// <summary>The kit sprite the splash's Play button wears.</summary>
        private const string KitPlaySprite = "play_main_menu_button";

        /// <summary>The marker child, so a second Install does not stack a second mark.</summary>
        private const string DressedMarker = "StoryPlayMark";

        /// <summary>Where the button's centre sits, as a fraction up the screen.</summary>
        private const float PlayHeightFrac = 0.245f;

        /// <summary>The painted button in Resources/Story/UI, drawn in place of the kit's.</summary>
        private const string PlayButtonSprite = "play_button";

        /// <summary>Its height, as a fraction of the height the kit gave its own button.</summary>
        private const float ButtonHeightFrac = 1.15f;

        /// <summary>The bare gold mark, drawn only if the painted button is missing.</summary>
        private const string PlayMarkSprite = "play_icon";

        /// <summary>The mark's side, as a fraction of the button's height.</summary>
        private const float MarkHeightFrac = 1.15f;


        /// <summary>
        /// Replaces the kit's blue candy Play button with the story's gold mark.
        ///
        /// It is redressed in place rather than replaced, because the button's
        /// own onClick is what starts the game and rebuilding it would mean
        /// re-wiring that. The kit's word "Play" is painted INTO
        /// play_main_menu_button.png, so the sprite is made transparent and the
        /// mark laid on top. Both are additions; the sprite is the only thing
        /// overwritten, and only on the instance.
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

                // The button is one painted sprite now.
                //
                // Every version before this was assembled out of primitives —
                // fills, bevels, a border of four bars — and each one read as UI
                // drawn over a painting rather than as something painted. This
                // is one piece of art in the same hand as the castle behind it:
                // cream rim, orange gloss, the word cut into it in white.
                //
                // preserveAspect, because the kit's rect is not this sprite's
                // shape and a stretched button is the one thing that would give
                // the whole screen away. The rect is scaled off its own height so
                // the sprite lands at a thumb's size whatever the kit set.
                Sprite art = StoryUi.LoadSprite(PlayButtonSprite);
                if (art != null)
                {
                    image.sprite = art;
                    image.type = Image.Type.Simple;
                    image.color = Color.white;
                    image.preserveAspect = true;
                    image.raycastTarget = true;

                    float height = rect.sizeDelta.y * ButtonHeightFrac;
                    rect.sizeDelta = new Vector2(
                        height * art.rect.width / art.rect.height, height);
                }
                else
                {
                    // The sprite is missing, so the kit's own candy button is
                    // still there and would ship. Hide it and fall back to the
                    // gold mark, which is a second file and may well be present.
                    // LoadSprite has already warned.
                    image.sprite = StoryUi.RoundedSprite(30);
                    image.type = Image.Type.Sliced;
                    image.color = new Color(0f, 0f, 0f, 0f);
                    image.raycastTarget = true;

                    Sprite mark = StoryUi.LoadSprite(PlayMarkSprite);
                    if (mark != null)
                    {
                        Image icon = StoryUi.CreateImage(DressedMarker, rect, mark, Color.white);
                        icon.preserveAspect = true;
                        icon.raycastTarget = false;
                        RectTransform iconRect = icon.rectTransform;
                        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
                        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
                        iconRect.pivot = new Vector2(0.5f, 0.5f);
                        float side = rect.sizeDelta.y * MarkHeightFrac;
                        iconRect.sizeDelta = new Vector2(side, side);
                        iconRect.anchoredPosition = new Vector2(2f, 0f);
                    }
                }

                return true;
            }

            return false;
        }



        /// <summary>The kit sprite its loading screen wears.</summary>
        private const string KitLoadingSprite = "loading";

        /// <summary>The marker child, so a second Install does not stack a second word.</summary>
        private const string LoadingMarker = "StoryLoadingLabel";

        /// <summary>
        /// The wait between the splash and the map was still the kit's game.
        ///
        /// Pressing Play unloads `main` and loads `game`, and for about half a
        /// second the kit's loading art covers the screen: a cyan field with the
        /// Jelly Garden wordmark on it and "Loading..." in a cloud. It is a
        /// single baked texture, so the wordmark cannot be swapped out of it —
        /// the whole picture has to go.
        ///
        /// It is replaced by the splash's own castle, which makes the wait read
        /// as the title screen holding rather than as a second game's screen
        /// interrupting. The word is a real Text because it was painted into the
        /// texture that just went.
        /// </summary>
        private static bool DressLoadingScreen()
        {
            foreach (Image image in
                     Object.FindObjectsByType<Image>(FindObjectsInactive.Include))
            {
                if (image == null || image.sprite == null) continue;
                if (image.sprite.name != KitLoadingSprite) continue;
                if (image.transform.Find(LoadingMarker) != null) return false;

                Sprite art = StoryArt.Load("splash_background");
                if (art == null) return false;
                image.sprite = art;

                Text word = StoryUi.CreateText(LoadingMarker, image.rectTransform,
                    "Loading...", 40, StoryUi.RoyalTitle, TextAnchor.MiddleCenter);
                word.font = StorybookUi.Enabled ? StoryUi.NameFont : StoryUi.DisplayFont;
                word.raycastTarget = false;

                // Centred, not sat on the bottom edge. On a phone the word was
                // down by the home indicator, far from the castle it belongs to;
                // the middle is where the eye already is while the wait passes.
                RectTransform rect = word.rectTransform;
                rect.anchorMin = new Vector2(0f, 0.5f);
                rect.anchorMax = new Vector2(1f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(0f, 120f);
                rect.anchoredPosition = Vector2.zero;
                var outline = word.gameObject.AddComponent<Outline>();
                outline.effectColor = StorybookUi.Enabled ? StorybookUi.Ink : StoryUi.IconWood;
                outline.effectDistance = new Vector2(3f, -3f);

                return true;
            }

            return false;
        }

    }
}
