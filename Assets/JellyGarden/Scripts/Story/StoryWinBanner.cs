// The "Level Completed" ribbon, in the story's own colours.
//
// The kit shows it for three seconds between the last firework and the win
// panel — LevelManager.PreWinAnimationsCor, through
// MenuManager.ShowPreCompleteBanner. The ribbon under the lettering is
// `Textures/menu/play_menu/game_play_01_banner.png`: a light blue candy stripe,
// which is the kit's world and not this one. It is the last kit artwork on the
// winning path, arriving one beat before a royal win panel.
//
// So the striped art is switched off and `Story/UI/win_banner` is drawn in its
// place: a royal blue plaque in a carved gold frame with a crown at the top and
// bottom.
//
// The lettering on top of it needs nothing from here — `level_completed_text`
// in the kit's own texture folder now carries the royal wordmark, painted into
// the file the kit already points at. On the Storybook board that wordmark is
// destroyed instead: the ribbon there carries its own "Wonderful!".
//
// Nothing is deleted, renamed or moved:
//
//   - the kit Image component is DISABLED, not removed, so its sprite,
//     material and sizing are all still there;
//   - the new ribbon is one child added under the same object, at sibling index
//     0 so it draws behind the lettering;
//   - the object's own Animation — `bannerComplete` — drives the parent
//     transform, so the slide and the bounce play over the new art unchanged.
//
// StoryWinBanner.Enabled = false, or deleting this file, gives the stripes
// back.
//
// It has to find the banner while the object is INACTIVE — the kit only
// switches it on for its three seconds — so the search walks the scene's roots
// rather than asking for an active object.

using UnityEngine;
using UnityEngine.UI;

namespace JellyGarden.Scripts.Story
{
    public static class StoryWinBanner
    {
        /// <summary>The whole feature, in one switch.</summary>
        public static bool Enabled = true;

        /// <summary>The kit's object, and the child carrying its striped art.</summary>
        private const string KitBannerName = "PreCompleteBanner";
        private const string KitRibbonName = "Banner";
        private const string KitWordmarkName = "Image";

        /// <summary>The name this adds, so it is found rather than doubled.</summary>
        private const string RibbonName = "~StoryWinRibbon";

        /// <summary>
        /// How wide the ribbon is drawn, in the kit canvas's own units.
        ///
        /// **690, not the object's own 800.** That canvas is authored at 720
        /// wide and its scaler matches WIDTH, so one reference unit is one 720th
        /// of the screen whatever the screen is, and 800 units is 1.11 screens.
        /// The kit's stripe can run off both edges and lose nothing — it is a
        /// stripe — but this one has a carved frame whose corners are the whole
        /// look of it. 690 leaves 15 units of margin a side, about 8 points
        /// at 393.
        /// </summary>
        private const float RibbonWidth = 690f;

        /// <summary>
        /// The art's own ratio, and the plaque is deliberately deep: 1024x492
        /// is 2.08:1, so at 690 wide it stands 331 units — 181 points at 393,
        /// against 98 for the long thin ribbon it replaces. A deep plaque is
        /// what carries a wordmark; the shallow one read as a strip of trim
        /// with type on it.
        /// </summary>
        private const float RibbonAspect = 1024f / 492f;

        /// <summary>Lifts the ribbon so the lettering sits on its blue, not on its rim.</summary>
        private const float RibbonRise = 6f;

        public static void Install()
        {
            if (!Enabled) return;

            Transform banner = Find(KitBannerName);
            if (banner == null) return;

            // Already dressed. The scene hook runs on every load and the object
            // outlives a level, so this is the common case.
            if (banner.Find(RibbonName) != null) return;

            Sprite art = StoryUi.LoadSprite("win_banner");
            if (art == null) return;

            Transform ribbon = banner.Find(KitRibbonName);
            if (ribbon != null)
            {
                var kitImage = ribbon.GetComponent<Image>();
                if (kitImage != null) kitImage.enabled = false;
            }

            if (StorybookUi.Enabled)
            {
                // The kit's "LEVEL COMPLETED" wordmark drew over "Wonderful!".
                // Nothing references it; bannerComplete skips a missing path.
                Transform wordmark = banner.Find(KitWordmarkName);
                if (wordmark != null) Object.Destroy(wordmark.gameObject);
                Text title = StorybookUi.Ribbon(RibbonName, banner, "Wonderful!", 80, 2);
                var root = (RectTransform)title.transform.parent;
                root.anchorMin = root.anchorMax = root.pivot = new Vector2(.5f, .5f);
                root.sizeDelta = new Vector2(RibbonWidth, 180);
                root.anchoredPosition = new Vector2(0, RibbonRise);
                root.SetSiblingIndex(0);
                return;
            }
            Image royal = StoryUi.CreateImage(RibbonName, banner, art, Color.white);
            royal.preserveAspect = true;

            RectTransform rect = royal.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = rect.anchorMin;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(RibbonWidth, RibbonWidth / RibbonAspect);
            rect.anchoredPosition = new Vector2(0f, RibbonRise);
            rect.SetSiblingIndex(0);

            Debug.Log("[Story] win banner dressed in the royal ribbon.");
        }

        /// <summary>
        /// The named object anywhere in the active scene, active or not.
        ///
        /// GameObject.Find skips inactive objects and this one is inactive
        /// except for the three seconds it is on screen, so the walk starts at
        /// the scene's roots.
        /// </summary>
        private static Transform Find(string name)
        {
            GameObject[] roots = UnityEngine.SceneManagement.SceneManager
                .GetActiveScene().GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Transform found = FindDeep(roots[i].transform, name);
                if (found != null) return found;
            }
            return null;
        }

        private static Transform FindDeep(Transform parent, string name)
        {
            if (parent == null) return null;
            if (parent.name == name) return parent;
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform found = FindDeep(parent.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
