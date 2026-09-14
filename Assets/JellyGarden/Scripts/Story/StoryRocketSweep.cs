// Where a rocket's travelling head is fired from, and how far it has to go.
//
// This is the seam between the kit's rocket and the picture drawn over it. It
// works out the axis, the reach and the colour from the gem being destroyed,
// and hands those to StoryRocketVfx. It destroys nothing.
//
// WHY IT DESTROYS NOTHING — A REGRESSION WORTH RECORDING
//
// The first version of this staggered the kit's own clear so gems died as the
// head reached them, which is what actually makes a rocket read as a shot
// rather than a flash. It hung the board.
//
// The mechanism is in Item.DestroyCor:
//
//     square.DestroyBlock();
//     if (currentType == HORIZONTAL_STRIPPED) DestroyHorizontal();
//     ...
//     if (destroying) Destroy(gameObject);
//
// DestroyHorizontal returns as soon as it has started a coroutine, so the kit
// destroyed the rocket gem and carried on while the sweep was still walking the
// row. The cascade then waited on a clear that never completed and DragBlocked
// stayed true. StoryPlaytest caught it on the first move of level 1: "move 1
// never finished — the board stayed blocked for 20s".
//
// The lesson generalises past this one method. The kit's destroy path is
// SYNCHRONOUS by contract: everything downstream of it — the cascade, the
// refill, the win check — assumes the line is clear by the time the call
// returns. Anything additive here has to leave that contract alone and spend
// its time on the picture instead.
//
// So the head travels and the row still clears on one frame. The head crosses
// a nine-wide board in about 120ms, which is short enough that the eye reads
// the direction without the board appearing to lag behind it.

using UnityEngine;

namespace JellyGarden.Scripts.Story
{
    public static class StoryRocketSweep
    {
        /// <summary>
        /// Fire the visible head for a rocket clearing a row or column.
        ///
        /// Called from Item.DestroyHorizontal / DestroyVertical, before the
        /// kit's own clear. A no-op unless -storyrocket asked for it.
        /// </summary>
        public static void Show(Item origin, bool horizontal)
        {
            if (!StoryRocketVfx.Enabled) return;
            if (origin == null || origin.square == null) return;

            LevelManager manager = LevelManager.THIS;
            if (manager == null) return;

            // How many cells the head has to cross to leave the board. Measured
            // from the rocket's own cell to whichever end is further, so the
            // head always makes it off the edge rather than stopping short on
            // an off-centre rocket.
            int from = horizontal ? origin.square.col : origin.square.row;
            int cells = horizontal ? manager.maxCols : manager.maxRows;
            int reach = Mathf.Max(from, cells - 1 - from);

            // The SQUARE's position, not the gem's.
            //
            // Item.Switching lerps transform.position between two cells while
            // the swap plays, and DestroyHorizontal is called from inside that
            // same coroutine on the rocket-plus-rocket path — so reading the
            // gem's transform there launches the head from wherever the piece
            // happened to be mid-slide, which is between two cells and, on a
            // vertical swap, in the wrong row entirely. The square never moves.
            StoryRocketVfx.Fire(
                origin.square.transform.position,
                horizontal,
                reach,
                StoryBoardScale.Pitch,
                ColourOf(origin, manager));
        }

        /// <summary>
        /// The gem's own colour, from the board's table — the same list the
        /// score popup reads, so a rocket cannot disagree with the gem that
        /// fired it.
        /// </summary>
        private static Color ColourOf(Item item, LevelManager manager)
        {
            if (manager.scoresColors != null &&
                item.color >= 0 && item.color < manager.scoresColors.Length)
                return manager.scoresColors[item.color];
            return Color.white;
        }
    }
}
