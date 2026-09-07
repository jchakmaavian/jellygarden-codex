// The last resort behind the kit's reshuffle.
//
// When no swap on the board can make a match, the kit is supposed to say "no
// more matches" and regenerate: `AI.CheckPossibleCombines` scans for a hint,
// and the run that finds none calls `LevelManager.NoMatches()`. That scan is a
// heuristic — a fixed list of L, T and line shapes, one colour at a time — and
// the coroutine that runs it is started from six places, never stopped, and
// gates BOTH its work and its own re-arming on `LevelManager.DragBlocked`
// while sharing one `gotTip` field between however many copies are in flight.
// It does not have to be wrong often to matter: when it does not fire, the
// player is left holding a board with no move on it and no way to end the
// level except to quit it.
//
// Level 31 is where it showed. Its map is nine isolated one-wide columns over
// a nine-by-three slab — no swap inside a one-wide column can ever line three
// up, so the whole level is played in the slab — and with three moves left the
// slab ran out of moves. The board sat there for twelve seconds with the kit
// silent, score goal already met and twenty-one ice tiles still to break.
//
// This watchdog asks the question exhaustively instead of heuristically, using
// the kit's OWN matcher (`Square.FindMatchesAround`, which is what decides
// whether a real swap scores), and when the answer is "nothing on this board
// matches" it calls the kit's OWN recovery. Nothing is replaced: the kit's AI
// still runs, still shows hints, and still reshuffles whenever it gets there
// first. This only covers the case where it never does.

using UnityEngine;

namespace JellyGarden.Scripts.Story
{
    [DisallowMultipleComponent]
    public sealed class StoryDeadBoard : MonoBehaviour
    {
        private static StoryDeadBoard _instance;

        /// <summary>How often the board is asked whether anything on it can still match.</summary>
        private const float CheckInterval = 1f;

        /// <summary>
        /// How many checks in a row must agree before the reshuffle is called.
        ///
        /// One is not enough: a board mid-cascade has falling items, and a
        /// falling item is not a legal partner for a swap, so a settling board
        /// reads as a dead one for a frame or two. Three checks a second apart
        /// is longer than any cascade in the kit and still well inside the two
        /// and a half seconds the kit's own path takes when it works.
        /// </summary>
        private const int RunsBeforeReshuffle = 3;

        private float _nextCheck;
        private int _deadRuns;

        /// <summary>Reshuffles this watchdog has had to call. Read by the playtest.</summary>
        public static int Reshuffles { get; private set; }

        /// <summary>Called by StorySceneHooks on every scene load; harmless in a scene with no board.</summary>
        public static void Install()
        {
            if (_instance != null) return;

            var go = new GameObject("~StoryDeadBoard");
            go.hideFlags = HideFlags.HideAndDontSave;
            StoryObjects.Persist(go);
            _instance = go.AddComponent<StoryDeadBoard>();
        }

        private void Update()
        {
            if (StoryClock.Now < _nextCheck) return;
            _nextCheck = StoryClock.Now + CheckInterval;

            if (!BoardIsIdle())
            {
                _deadRuns = 0;
                return;
            }

            if (AnyMoveExists())
            {
                _deadRuns = 0;
                return;
            }

            _deadRuns++;
            if (_deadRuns < RunsBeforeReshuffle) return;

            _deadRuns = 0;
            Reshuffles++;
            Debug.Log("[Story] no swap on the board can match and the kit did not reshuffle; "
                      + "asking it to, on level " + LevelManager.THIS.currentLevel + ".");
            LevelManager.THIS.NoMatches();
        }

        /// <summary>
        /// True when the board is the player's to move: a level is up, it is
        /// being played, and the kit is not in the middle of resolving a swap.
        /// `DragBlocked` is the kit's own word for "not yours yet".
        /// </summary>
        private static bool BoardIsIdle()
        {
            LevelManager manager = LevelManager.THIS;
            if (manager == null) return false;
            if (manager.gameStatus != GameState.Playing) return false;
            if (manager.DragBlocked) return false;
            return manager.squaresArray != null;
        }

        /// <summary>
        /// Whether ANY swap of two neighbouring items would score, asked the
        /// same way a real swap is judged.
        ///
        /// The two items are exchanged, the kit's matcher is run on both ends,
        /// and they are put back — the same shape `StoryPlaytest.TryDirection`
        /// uses, and for the same reason: the kit has no other definition of a
        /// legal move, so anything that invents one can disagree with the game.
        /// Ingredients are skipped because an ingredient has no colour to match
        /// with; that makes this answer conservative, never optimistic.
        /// </summary>
        public static bool AnyMoveExists()
        {
            Square[] squares = LevelManager.THIS == null ? null : LevelManager.THIS.squaresArray;
            if (squares == null) return true;

            for (int i = 0; i < squares.Length; i++)
            {
                Square square = squares[i];
                if (square == null || square.item == null) continue;

                // Right and top only: every pair of neighbours is reachable
                // from one of its two ends, so checking all four directions
                // would test each pair twice.
                if (Scores(square, square.GetNeighborRight())) return true;
                if (Scores(square, square.GetNeighborTop())) return true;
            }

            return false;
        }

        private static bool Scores(Square square, Square neighbour)
        {
            if (neighbour == null || neighbour.item == null || square.item == null) return false;
            if (square.type == SquareTypes.WIREBLOCK || neighbour.type == SquareTypes.WIREBLOCK) return false;

            Item mine = square.item;
            Item theirs = neighbour.item;
            if (mine.falling || theirs.falling) return false;
            if (mine.currentType == ItemsTypes.INGREDIENT || theirs.currentType == ItemsTypes.INGREDIENT) return false;

            neighbour.item = mine;
            square.item = theirs;
            int matches = neighbour.FindMatchesAround().Count + square.FindMatchesAround().Count;
            neighbour.item = theirs;
            square.item = mine;

            return matches > 0;
        }
    }
}
