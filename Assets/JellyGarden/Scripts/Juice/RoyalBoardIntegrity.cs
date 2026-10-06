using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace JellyGarden.Scripts.Juice
{
    /// <summary>
    /// The board's safety net (2026-10-06, the user: two gems overlapped in one cell; "make sure it never happens again").
    /// Every piece must be owned by exactly the cell it names (item.square.item == item) and rest on that cell. The cause
    /// found then (gravity moving a piece on paper while a swap held it) is fixed at the source in LevelManager.FallingDown,
    /// Square.FallOut and Item.SwitchingCore; this catches any other cause, now or later.
    /// It only looks while the board is calm in play (nothing resolving, swapping, falling, breaking, no power-up or tool at
    /// work) for half a second, and then repairs:
    /// - a cell whose record points at a piece that names another cell: the record is cleared (gravity refills it);
    /// - a piece no cell owns (an orphan, drawn over another piece): it takes its own cell back if that is empty, else the
    ///   empty cell it is drawn on, else it is removed (it is a stray copy over a piece that owns the cell);
    /// - a piece more than a third of a cell away from its own cell: it glides back.
    /// Each repair is logged as a warning naming what was found, so a new cause shows up in the console.
    /// </summary>
    public sealed class RoyalBoardIntegrity : MonoBehaviour
    {
        const float Interval = .25f, CalmFor = .5f, DriftCells = .34f;
        float nextCheck, calmSince = -1;
        /// <summary>Repairs made since the game started (tests read it).</summary>
        public static int Repairs { get; private set; }
        public static string LastReport { get; private set; } = "";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (FindAnyObjectByType<RoyalBoardIntegrity>()) return;
            var go = new GameObject("Royal board integrity");
            DontDestroyOnLoad(go);
            go.AddComponent<RoyalBoardIntegrity>();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Repairs = 0; LastReport = ""; }

        void Update()
        {
            if (Time.unscaledTime < nextCheck) return;
            nextCheck = Time.unscaledTime + Interval;
            var lm = LevelManager.THIS;
            if (!Calm(lm, out var pieces)) { calmSince = -1; return; }
            if (calmSince < 0) { calmSince = Time.unscaledTime; return; }
            if (Time.unscaledTime - calmSince < CalmFor) return;
            Repair(lm, pieces);
        }

        static bool Calm(LevelManager lm, out List<Item> pieces)
        {
            pieces = null;
            if (!lm || lm.gameStatus != GameState.Playing || lm.squaresArray == null || !lm.GameField || !lm.GameField.gameObject.activeInHierarchy) return false;
            if (lm.IsResolvingMatches || lm.InputSwaps > 0 || RoyalPowerupDirector.Busy) return false;
            var tools = RoyalThrone.ReferenceGameplay.ReferenceBoosterBar.Instance;
            if (tools && tools.Busy) return false;
            pieces = new List<Item>();
            foreach (var go in GameObject.FindGameObjectsWithTag("Item"))
            {
                var item = go ? go.GetComponent<Item>() : null;
                // Only the level's own pieces: lesson boards and previews keep theirs elsewhere.
                if (!item || !item.isActiveAndEnabled || !item.transform.IsChildOf(lm.GameField)) continue;
                if (item.destroying || item.falling || item.inputSwapLocked || item.inputConversionLocks > 0 || item.royalConsumed) return false;
                pieces.Add(item);
            }
            return true;
        }

        void Repair(LevelManager lm, List<Item> pieces)
        {
            float cell = Mathf.Abs(lm.squareWidth * lm.GameField.lossyScale.x);
            var report = new StringBuilder();
            bool refill = false;
            // Cells whose record points at a piece that names another cell (or at nothing alive).
            foreach (var sq in lm.squaresArray)
            {
                if (!sq || !sq.item) continue;
                if (sq.item.square != sq)
                {
                    report.Append("cell " + sq.col + "," + sq.row + " pointed at a piece of cell " + (sq.item.square ? sq.item.square.col + "," + sq.item.square.row : "none") + "; ");
                    sq.item = null; refill = true;
                }
            }
            foreach (var item in pieces)
            {
                if (!item) continue;
                var own = item.square;
                if (own && own.item == item)
                {
                    float off = Vector2.Distance(item.transform.position, own.transform.position);
                    if (off > cell * DriftCells)
                    {
                        report.Append("piece of cell " + own.col + "," + own.row + " was " + (off / cell).ToString("0.00") + " cells away; ");
                        item.falling = false; item.StartFalling();
                    }
                    continue;
                }
                // An orphan: no cell owns it.
                Square here = null; float best = cell * .5f;
                foreach (var sq in lm.squaresArray)
                {
                    if (!sq || sq.IsNone()) continue;
                    float d = Vector2.Distance(sq.transform.position, item.transform.position);
                    if (d < best) { best = d; here = sq; }
                }
                if (own && !own.item && !own.IsNone() && own.CanGoInto())
                {
                    report.Append("orphan took its cell " + own.col + "," + own.row + " back; ");
                    own.item = item; item.falling = false; item.StartFalling();
                }
                else if (here && !here.item && here.CanGoInto())
                {
                    report.Append("orphan took the empty cell " + here.col + "," + here.row + " it was on; ");
                    here.item = item; item.square = here; item.falling = false; item.StartFalling();
                }
                else
                {
                    report.Append("orphan colour " + item.color + " drawn over cell " + (here ? here.col + "," + here.row : "?") + " (named " + (own ? own.col + "," + own.row : "none") + ") was removed; ");
                    item.destroying = true;
                    Destroy(item.gameObject);
                    refill = true;
                }
            }
            if (report.Length == 0) return;
            Repairs++;
            LastReport = report.ToString();
            Debug.LogWarning("[Royal board] Repaired the board: " + LastReport);
            calmSince = -1;
            // Cleared cells and removed strays leave holes: one resolver pass lets gravity and the refill close them.
            if (refill) lm.FindMatches();
        }
    }
}
