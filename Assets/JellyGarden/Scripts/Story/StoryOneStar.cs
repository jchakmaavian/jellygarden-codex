// One star a level, the way Royal Match does it.
//
// The story economy has always paid exactly one star per level, once, on the
// first win — StoryFlow.HandleWin does that and a replay pays nothing. What
// contradicted it was the kit's own three-star score system running alongside:
// LevelManager.CheckStars fills `stars` to 1, 2 or 3 as Score crosses star1,
// star2 and star3, the HUD draws three markers along the progress bar, and the
// win panel pops one star per point scored. Two different things called "star",
// and the story only ever used one of them.
//
// This collapses the kit's version to a single star without deleting any of it:
//
//   star3 is set to star1, so the progress bar — which reads Score / star3 —
//   fills exactly when the level is passed rather than at a bonus threshold
//
//   the second and third markers on the bar are hidden and the first is moved
//   to the end of it
//
//   CheckStars is capped at one, through the one marked hook in LevelManager
//
// star1 is deliberately left alone. It is not only a display threshold: for
// SCORE levels it is the pass mark, so removing it would leave those levels
// with no win condition at all.

using JellyGarden.Scripts.GUI;
using UnityEngine;

namespace JellyGarden.Scripts.Story
{
    public static class StoryOneStar
    {
        /// <summary>
        /// The whole feature, in one switch. Turning it off restores the kit's
        /// three-star scoring exactly, because nothing here was removed.
        /// </summary>
        public static bool Enabled = true;

        private static int _preparedLevel = -1;

        /// <summary>
        /// The kit's cap. Called from LevelManager.CheckStars — one star is the
        /// most any level can pay, so the second and third thresholds are never
        /// consulted.
        /// </summary>
        public static int CapStars(int stars)
        {
            return Enabled ? Mathf.Clamp(stars, 0, 1) : stars;
        }

        /// <summary>Called by StorySceneHooks on every scene load.</summary>
        public static void Install()
        {
            if (!Enabled) return;
            if (LevelManager.THIS == null) return;

            // LoadLevel rewrites star1..star3 from the level file, so this has to
            // run per level rather than once per scene.
            if (_preparedLevel == LevelManager.THIS.currentLevel) return;

            Apply();
        }

        /// <summary>
        /// Point the progress bar at the single star. Safe to call repeatedly.
        /// </summary>
        public static void Apply()
        {
            LevelManager manager = LevelManager.THIS;
            if (manager == null || manager.star1 <= 0) return;

            _preparedLevel = manager.currentLevel;

            // The bar's fill is Score / star3 and its markers are placed at
            // star1/star3 and star2/star3. Collapsing all three onto star1 puts
            // the one marker at the end of a bar that fills on the pass mark.
            manager.star2 = manager.star1;
            manager.star3 = manager.star1;

            HideExtraMarkers();

            Debug.Log(string.Format(
                "[Story] Level {0}: one star. Pass mark {1}; the kit's 2nd and 3rd " +
                "thresholds collapsed onto it.", manager.currentLevel, manager.star1));
        }

        private static void HideExtraMarkers()
        {
            ProgressBarScript bar = ProgressBarScript.Instance;
            if (bar == null || bar.stars == null) return;

            for (int index = 0; index < bar.stars.Length; index++)
            {
                if (bar.stars[index] == null) continue;
                // Keep the first marker, drop the two that no longer mean
                // anything. The kit re-lays them out in PrepareStars, which now
                // computes star1/star3 == 1 and parks the survivor at the end.
                bar.stars[index].SetActive(index == 0);
            }
        }

        /// <summary>Test hook — forces the next Install to run again.</summary>
        public static void Forget()
        {
            _preparedLevel = -1;
        }
    }
}
