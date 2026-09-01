// Makes the map's lock state follow the story instead of the kit's per-level
// star bookkeeping.
//
// LevelsMap.IsLevelLocked asks this for the previous node's star count and locks
// anything that returns zero. In the story that question means "did you finish
// the area before this one" — a node is an area, and an area opens when the one
// before it is fully repaired, not when a level happened to score a star.
//
// Installed with LevelsMap.OverrideMapProgressManager. The kit's
// PlayerPrefsMapProgressManager is left in place and untouched.

using JellyGarden.Scripts.MapScripts;
using UnityEngine;

namespace JellyGarden.Scripts.Story
{
    public sealed class StoryMapProgressManager : IMapProgressManager
    {
        /// <summary>
        /// A finished area reports three stars so the map draws it as complete.
        /// The number is cosmetic — LevelsMap only ever compares it against zero.
        /// </summary>
        private const int CompleteStars = 3;

        public int LoadLevelStarsCount(int level)
        {
            return StoryProgress.IsAreaComplete(level) ? CompleteStars : 0;
        }

        /// <summary>
        /// The kit calls this when a level is won. In the story a level win pays a
        /// star into the area's pool and nothing else — completion is decided by
        /// spending those stars on repairs, so there is nothing to write here.
        /// </summary>
        public void SaveLevelStarsCount(int level, int starsCount)
        {
        }

        public void ClearLevelProgress(int level)
        {
            // Progress is one file, not a key per level; clearing a single area
            // in isolation is not something the story exposes.
            Debug.Log(string.Format(
                "[Story] ClearLevelProgress({0}) ignored — story progress is cleared " +
                "as a whole via StoryProgress.ResetAll().", level));
        }

        /// <summary>
        /// Installs this manager. Safe to call more than once; the last call wins
        /// and they are all equivalent.
        /// </summary>
        public static void Install()
        {
            LevelsMap.OverrideMapProgressManager(new StoryMapProgressManager());
        }
    }
}
