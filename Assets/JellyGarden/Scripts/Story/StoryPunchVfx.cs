// The board's own reaction to a gem being cleared.
//
// Stages 1-3 (StoryPopVfx) happen to the gem that dies. This is what happens to
// everything AROUND it: each surviving neighbour gets a small squash-and-settle
// spring, strongest next to the blast and fading with distance, so a clear reads
// as something that happened TO the board rather than something that happened on
// top of it.
//
// 220ms, a damped sine, amplitude falling off as exp(-0.85 * d) in cell widths,
// cut off past 2.6 cells — beyond that the term is under 11% of its peak and the
// motion is not worth the work of computing it.
//
// WHY THIS CANNOT REUSE StoryPopVfx
//
// They look like the same effect applied to different gems, and they are not.
//
// StoryPopVfx drives a gem that is ABOUT TO BE DESTROYED. It can leave that gem
// at any scale it likes, because the gem is gone a frame later. Item.PopScale
// captures the resting scale once and never puts it back, and for a dying gem
// that is exactly right.
//
// A neighbour SURVIVES. Left at the end of a spring it would keep whatever
// scale the last frame wrote, forever — and because Item.AnimIdleStart is still
// running and still assigning localScale from xScale/yScale every fixed update,
// that wrong value would be actively maintained rather than drifting back. Over
// a cascade the board would visibly deform, one gem at a time, and nothing would
// ever restore it. So every punch MUST end with Item.PopRelease(), including the
// paths where it ends early.
//
// WHY IT WALKS THE GRID ITSELF INSTEAD OF CALLING GetItemsAround
//
// LevelManager.GetItemsAround is the obvious helper and it is wrong for this.
// It reads each neighbour with GetSquare(c, r, safe: true), and `safe` CLAMPS an
// out-of-range index to the board edge rather than returning null:
//
//     row = Mathf.Clamp(row, 0, maxRows - 1);
//     col = Mathf.Clamp(col, 0, maxCols - 1);
//
// So for a gem on the edge it hands back the same square two or three times. The
// kit does not care — it destroys each one and destroying twice is a no-op — but
// a spring is additive, so an edge gem would be punched two or three times as
// hard as one in the middle, which is the opposite of the falloff this exists to
// produce. This walks the square radius with safe: false and null-checks.
//
// It also only ever reaches 3x3 and this needs 2.6 cells in every direction.
//
// COST
//
// One coroutine per punched neighbour, up to 24 per cleared gem, each living
// 220ms. A colour bomb clears ~25 gems at once, so the ceiling is a few hundred
// short-lived coroutines — the same order as the 850 motes StoryShatterVfx sizes
// its pool for, and each of these does three multiplies a frame. The guard that
// actually matters is _busy: a gem already springing is not given a second
// spring, so overlapping clears cannot compound on one piece.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JellyGarden.Scripts.Story
{
    public static class StoryPunchVfx
    {
        /// <summary>
        /// Off until a launch asks for it. Shares the pop's flag: the board
        /// response is part of the same effect and there is no reason to want
        /// one without the other. Flipped by <c>-storypop</c>.
        /// </summary>
        public static bool Enabled
        {
            get { return StoryPopVfx.Enabled; }
        }

        // The whole spring, in seconds — stage 7's 220ms.
        private const float PunchTime = 0.220f;

        // How hard the nearest neighbour is hit, as a fraction of its own scale.
        // 0.17 is a seventh of a cell at the peak of the first swing and settles
        // to nothing; past about 0.25 the gems visibly overlap their neighbours
        // at this board's pitch.
        private const float Amplitude = 0.17f;

        // exp(-Falloff * d), d in cell widths. At one cell the neighbour gets
        // 43% of the peak, at two cells 18%, at 2.6 cells 11% — which is where
        // Radius cuts it off.
        private const float Falloff = 0.85f;
        private const float Radius = 2.6f;

        // How many times the spring crosses zero over its life. 3.2 gives a
        // strong first swing, a clear bounce back and a small third that reads
        // as the piece settling rather than as a wobble.
        private const float Oscillations = 3.2f;

        // How fast the envelope dies. 3.6 leaves the last swing at about 2% of
        // the first, so the gem arrives at rest rather than being cut off
        // mid-motion when the coroutine ends.
        private const float Damping = 3.6f;

        // Gems already springing. A cascade clears several cells within a few
        // frames and the same neighbour is in range of more than one of them;
        // without this the springs would add and a gem next to a big clear
        // would be thrown far harder than Amplitude allows.
        private static readonly HashSet<Item> Busy = new HashSet<Item>();

        /// <summary>
        /// Punch the board around one cleared cell.
        ///
        /// Called at the moment the gem dies, from Item.DestroyCor. Starts one
        /// coroutine per neighbour in range and returns immediately — it must
        /// not be yielded on, because the kit's destroy path is synchronous by
        /// contract below this point. See StoryRocketSweep's header for what
        /// happens when that contract is broken.
        /// </summary>
        public static void Punch(Item origin)
        {
            if (!Enabled) return;
            if (!Application.isPlaying) return;

            // See StoryBlastVfx.Blast: one effect per clear. The neighbour
            // spring is a separate idea from "the gem shatters and falls out of
            // the board", and stacking the two made every variant look the same.
            if (StoryVfxVariant.Current != VfxVariant.Kit) return;
            if (origin == null || origin.square == null) return;

            LevelManager manager = LevelManager.THIS;
            if (manager == null) return;

            int centreCol = origin.square.col;
            int centreRow = origin.square.row;
            int punched = 0;

            // Whole cells out to the radius. 2.6 needs 3 rings, and the corner
            // of ring 3 is 4.24 cells away, which the distance test below drops.
            int ring = Mathf.CeilToInt(Radius);

            for (int r = centreRow - ring; r <= centreRow + ring; r++)
            {
                for (int c = centreCol - ring; c <= centreCol + ring; c++)
                {
                    if (r == centreRow && c == centreCol) continue;
                    if (r < 0 || c < 0 || r >= manager.maxRows || c >= manager.maxCols) continue;

                    // safe: false — see the header. A clamped lookup would hand
                    // back the same edge square several times and punch it once
                    // per duplicate.
                    Square square = manager.GetSquare(c, r);
                    if (square == null) continue;

                    Item neighbour = square.item;
                    if (neighbour == null) continue;

                    // A gem already dying has its own wind-up running on the
                    // same two fields; springing it as well would fight that.
                    if (neighbour.destroying) continue;
                    if (Busy.Contains(neighbour)) continue;

                    float distance = Mathf.Sqrt(
                        (r - centreRow) * (r - centreRow) +
                        (c - centreCol) * (c - centreCol));
                    if (distance > Radius) continue;

                    float amplitude = Amplitude * Mathf.Exp(-Falloff * distance);

                    Busy.Add(neighbour);
                    neighbour.StartCoroutine(Spring(neighbour, amplitude));
                    punched++;
                }
            }

            // How many neighbours a clear actually reached. Zero would mean the
            // radius or the null checks are wrong and the whole stage is inert,
            // which looks exactly like "no effect" from the player's side.
            if (_said < 3)
            {
                _said++;
                Debug.Log(string.Format(
                    "[Punch] #{0} at cell {1},{2} punched {3} neighbour(s)",
                    _said, centreCol, centreRow, punched));
            }
        }

        private static int _said;

        /// <summary>
        /// One neighbour's squash and settle.
        ///
        /// The gem is squashed along one axis and stretched along the other by
        /// the same factor, so its area is roughly preserved and it reads as
        /// being compressed rather than as changing size.
        /// </summary>
        private static IEnumerator Spring(Item item, float amplitude)
        {
            float elapsed = 0f;

            while (elapsed < PunchTime)
            {
                // The neighbour can be cleared mid-spring by the very cascade
                // that started it. Release and stop rather than writing to a
                // dead transform.
                if (item == null) break;
                if (item.destroying) break;

                float t = elapsed / PunchTime;

                // A decaying sine: starts at zero, swings, and arrives back at
                // zero. Starting at zero matters — a spring that starts at full
                // deflection is a jump, not a punch.
                float swing = Mathf.Exp(-Damping * t) *
                              Mathf.Sin(t * Mathf.PI * Oscillations);

                float squash = swing * amplitude;
                item.PopScale(1f - squash, 1f + squash);

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            // ALWAYS, on every path out of the loop. A neighbour left scaled is
            // a neighbour permanently deformed, because the idle loop keeps
            // reasserting whatever xScale/yScale last held. See the header.
            if (item != null) item.PopRelease();
            Busy.Remove(item);
        }

        /// <summary>
        /// Drop every in-flight spring. Called when a level unloads so a gem
        /// destroyed between scenes cannot leave an entry behind in the busy
        /// set, which would stop that Item ever being punched again if Unity
        /// reused the reference.
        /// </summary>
        public static void Reset()
        {
            Busy.Clear();
        }
    }
}
