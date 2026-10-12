// The story's board dressing, retired from the level (plan D6).
//
// This used to put the area's repair painting behind the board, a blue ground
// under its cells and a gold outline around it. Royal Throne's board scene
// (RoyalGameplayPresentation, RoyalBoardSurface) now draws the backdrop, the
// cells and the frame of every level, so nothing here touches the board or
// subscribes to LevelManager any more.
//
// What is left is the D6 decision as two switches, both off, and the record a
// board check can read. Nothing in the level reads either any more.

using UnityEngine;

namespace JellyGarden.Scripts.Story
{
    public static class StoryBoardTheme
    {
        /// <summary>Whether the story puts the area's painting behind the level. Off: D6.</summary>
        public const bool UseStoryBackdrop = false;

        /// <summary>Whether the story gives the board's cells a ground of its own. Off: D6.</summary>
        public const bool UseStoryCellFill = false;

        /// <summary>What the story put behind the last board: nothing, since D6.</summary>
        public static string LastBackdrop
        {
            get { return "none: the level's backdrop is Royal Throne's board scene"; }
        }

        /// <summary>Never true: the story no longer falls back to a kit picture.</summary>
        public static bool LastBackdropWasKitDefault
        {
            get { return false; }
        }

        /// <summary>The ground the story drew under the cells: none.</summary>
        public static Color LastFill
        {
            get { return Color.clear; }
        }

        /// <summary>How many cells carry the story's ground: none.</summary>
        public static int LastFilledCells
        {
            get { return 0; }
        }
    }
}
