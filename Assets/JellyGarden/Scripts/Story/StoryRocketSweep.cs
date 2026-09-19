// Connects the rocket picture to the board clear. Items are reserved synchronously;
// their own destroy coroutines wait for impact before hiding the sprite. Obstacle
// contacts run on LevelManager and participate in its existing refill barrier, so
// destroying the launching rocket cannot cancel a pending hit.

using System.Collections;
using UnityEngine;

namespace JellyGarden.Scripts.Story
{
    public static class StoryRocketSweep
    {
        static LevelManager obstacleOwner;
        static int pendingObstacles;

        public static bool HasPendingHits => obstacleOwner != null &&
            obstacleOwner == LevelManager.THIS && pendingObstacles > 0;

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
                ColourOf(origin, manager),
                origin.sprRenderer != null ? origin.sprRenderer.sprite : null,
                horizontal ? from : cells - 1 - from,
                horizontal ? cells - 1 - from : from);

            // Open the window the per-gem effects read to stagger themselves.
            // See OpenWindow: this is a PICTURE-ONLY stagger, and the kit's
            // clear below still runs whole on this frame.
            OpenWindow(origin.square.row, origin.square.col, horizontal,
                       origin.square.transform.position);

            // The board's recoil, timed to land when the head does rather than
            // when the row clears — the two are the same frame for the kit and
            // a third of a second apart for the eye.
            if (StoryRocketVariant.Current != RocketVariant.RoyalStreak)
                StoryFinishVfx.PunchAfter(StoryRocketVfx.ReachesAt(reach));
        }

        /// <summary>
        /// Reserve the item synchronously, but keep its sprite until this flight reaches it.
        /// Explicit per-item timing also works for intersecting and chained rockets; a global
        /// "last rocket" window cannot describe those. The item's own destroy coroutine owns
        /// the wait, so destroying the launching rocket cannot interrupt the clear.
        /// </summary>
        public static void Hit(Item target, Item origin, bool horizontal)
        {
            if (StoryRocketVfx.Enabled && target != null && target.square != null &&
                origin != null && origin.square != null)
            {
                int distance = horizontal
                    ? target.square.col - origin.square.col
                    : target.square.row - origin.square.row;
                target.DestroyByRocket(StoryRocketVfx.ReachesAt(distance));
            }
            else if (target != null) target.DestroyItem(true);
        }

        public static void HitObstacle(Square target, Item origin, bool horizontal)
        {
            var manager = LevelManager.THIS;
            if (!StoryRocketVfx.Enabled || manager == null || origin == null || origin.square == null)
            {
                if (target != null) target.DestroyBlock();
                return;
            }
            if (obstacleOwner != manager) { obstacleOwner = manager; pendingObstacles = 0; }
            int distance = horizontal ? target.col - origin.square.col : target.row - origin.square.row;
            pendingObstacles++;
            manager.StartCoroutine(ObstacleContact(target, StoryRocketVfx.ReachesAt(distance), manager));
        }

        static IEnumerator ObstacleContact(Square target, float delay, LevelManager manager)
        {
            try
            {
                float elapsed = 0f;
                while (elapsed < delay)
                {
                    yield return null;
                    if (manager == null || manager != LevelManager.THIS) yield break;
                    if (manager.gameStatus != GameState.Pause) elapsed += Time.deltaTime;
                }
                if (target != null)
                {
                    StoryRocketVfx.Contact(target.transform.position, manager.squareWidth);
                    target.DestroyBlock();
                }
            }
            finally { if (obstacleOwner == manager) pendingObstacles = Mathf.Max(0, pendingObstacles - 1); }
        }

        /// <summary>
        /// Where the last rocket fired from, and when.
        ///
        /// WHY A STATIC WINDOW AND NOT A PARAMETER
        ///
        /// The per-gem effects are called from inside Item.DestroyCor — once
        /// per gem, from a coroutine the kit starts, through a signature the
        /// kit owns: DestroyItem(bool, string, bool). There is no argument to
        /// carry "you are the fourth cell of a rocket that fired 90ms ago",
        /// and adding one would change a kit method's shape rather than insert
        /// a line into it, which is the one thing the VFX guard forbids.
        ///
        /// So the rocket leaves its origin here and each gem works out its own
        /// distance from its own square. A gem cleared by an ordinary match
        /// falls outside the window and is unaffected.
        ///
        /// THE WINDOW IS SHORT ON PURPOSE
        ///
        /// It closes after one sweep's worth of travel. A gem cleared by a
        /// cascade a second later must NOT read itself as part of the rocket
        /// and delay its own burst — the cascade is a different event and the
        /// board has already refilled underneath it.
        /// </summary>
        private static int _row, _col;
        private static bool _horizontal;
        private static float _openedAt = float.NegativeInfinity;
        private static float _window;
        private static Vector3 _originAt;

        private static void OpenWindow(int row, int col, bool horizontal, Vector3 at)
        {
            _row = row;
            _col = col;
            _horizontal = horizontal;
            _originAt = at;
            _openedAt = Time.time;

            LevelManager manager = LevelManager.THIS;
            int cells = manager == null
                ? 9
                : (horizontal ? manager.maxCols : manager.maxRows);
            // One sweep, plus a frame's grace so the gem at the far end is
            // still inside the window when its own destroy runs.
            _window = StoryRocketVfx.ReachesAt(cells) + 0.05f;
        }

        /// <summary>
        /// How long this gem's burst should wait so it goes off as the rocket's
        /// head arrives at it. Zero for any gem not cleared by a live rocket,
        /// which is what makes an ordinary match look exactly as it does today.
        /// </summary>
        public static float DelayFor(Item item)
        {
            if (!StoryRocketVfx.Enabled) return 0f;
            if (item == null || item.square == null) return 0f;
            if (Time.time - _openedAt > _window) return 0f;

            // Only the line the rocket actually ran down. A package or a colour
            // bomb clearing cells elsewhere in the same instant is not part of
            // this sweep.
            if (_horizontal)
            {
                if (item.square.row != _row) return 0f;
                return StoryRocketVfx.ReachesAt(item.square.col - _col);
            }

            if (item.square.col != _col) return 0f;
            return StoryRocketVfx.ReachesAt(item.square.row - _row);
        }

        /// <summary>
        /// The same delay, for a caller that has a world position rather than
        /// an Item.
        ///
        /// StoryBlastVfx.Blast is handed `transform.position` by the kit and
        /// never sees the Item, so it cannot read a row and a column. The
        /// origin square's own position and the board's pitch are enough: the
        /// offset along the sweep's axis, divided by the pitch, IS the distance
        /// in cells.
        ///
        /// Rounded rather than truncated, and only accepted within a third of a
        /// cell of the line, so a burst thrown from a gem mid-fall — or from
        /// the half-cell offset a swap animation leaves — is not mistaken for
        /// one a cell further along.
        /// </summary>
        public static float DelayFor(Vector3 at)
        {
            if (!StoryRocketVfx.Enabled) return 0f;
            if (Time.time - _openedAt > _window) return 0f;

            float pitch = StoryBoardScale.Pitch;
            if (pitch <= 0.0001f) return 0f;

            Vector3 offset = at - _originAt;
            float along = _horizontal ? offset.x : offset.y;
            float across = _horizontal ? offset.y : offset.x;

            // Off the rocket's line entirely: a package or a colour bomb
            // clearing cells elsewhere in the same instant is not this sweep.
            if (Mathf.Abs(across) > pitch * 0.34f) return 0f;

            float cells = along / pitch;
            if (Mathf.Abs(cells - Mathf.Round(cells)) > 0.34f) return 0f;

            return StoryRocketVfx.ReachesAt(Mathf.RoundToInt(cells));
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
