using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JellyGarden.Scripts.Juice
{
    // The board's shuffle (2026-10-04, the user: "if there is no match, shuffle automatically; keep the player satisfied").
    // When no swap makes a match, the plain gems already on the board are rearranged instead of the old rebuild (which
    // destroyed every gem, generated new ones and did not guarantee a move, so "No more matches" could repeat). Power-ups,
    // obstacles, chained gems and campaign pieces stay where they are. The new arrangement has no ready-made matches and
    // at least one swap that matches; among a few hundred tries the one with the most moves wins (enough at 5). The gems
    // then glide to their new cells together, under a "SHUFFLE!" banner.
    public static class RoyalShuffle
    {
        const int Tries = 400, Enough = 5;
        const float Glide = .55f;

        public static IEnumerator Run(LevelManager lm, global::System.Action<bool> done)
        {
            var squares = new List<Square>(); var items = new List<Item>();
            foreach (var sq in lm.squaresArray)
            {
                if (!sq || sq.IsNone() || !sq.item) continue;
                var it = sq.item;
                if (it.currentType != ItemsTypes.NONE || it.destroying || it.falling || RoyalThrone.Campaign.RoyalCampaignBlockers.IsSpecialItem(it)) continue;
                if (sq.type == SquareTypes.WIREBLOCK || !sq.CanGoInto() || !sq.CanGoOut()) continue;
                squares.Add(sq); items.Add(it);
            }
            if (items.Count < 3) { done(false); yield break; }
            var colours = new int[items.Count];
            for (int i = 0; i < items.Count; i++) colours[i] = items[i].color;

            int[] best = null; int bestMoves = 0;
            var order = new int[items.Count];
            for (int attempt = 0; attempt < Tries && bestMoves < Enough; attempt++)
            {
                for (int i = 0; i < order.Length; i++) order[i] = i;
                for (int i = order.Length - 1; i > 0; i--) { int j = Random.Range(0, i + 1); int t = order[i]; order[i] = order[j]; order[j] = t; }
                // Try it in place: each gem takes the colour of the gem that would move onto its cell (restored below,
                // before this frame ends, so nothing is drawn in between).
                for (int i = 0; i < items.Count; i++) items[i].color = colours[order[i]];
                if (!ReadyMatch(lm))
                {
                    int moves = Moves(lm, Enough);
                    if (moves > bestMoves) { bestMoves = moves; best = (int[])order.Clone(); }
                }
                for (int i = 0; i < items.Count; i++) items[i].color = colours[i];
                if (attempt % 40 == 39) yield return null;   // spread the search over a few frames
            }
            if (best == null) { done(false); yield break; }

            // Apply: the gem that had colour `best[i]` moves onto squares[i].
            var moving = new Item[squares.Count];
            for (int i = 0; i < squares.Count; i++) moving[i] = items[best[i]];
            var from = new Vector3[squares.Count]; var to = new Vector3[squares.Count];
            for (int i = 0; i < squares.Count; i++)
            {
                squares[i].item = moving[i]; moving[i].square = squares[i];
                from[i] = moving[i].transform.position; to[i] = squares[i].transform.position + Vector3.back * .2f;
            }
            RoyalCelebration.PlayWord("SHUFFLE!");
            float cell = lm.squareHeight * Mathf.Abs(lm.GameField.lossyScale.y);
            float t0 = 0;
            while (t0 < Glide)
            {
                if (lm.gameStatus == GameState.Pause) { yield return null; continue; }
                t0 += Time.deltaTime;
                float k = Mathf.Clamp01(t0 / Glide), e = k * k * (3 - 2 * k);
                for (int i = 0; i < moving.Length; i++)
                {
                    if (!moving[i]) continue;
                    // A soft arc: each gem lifts a little off the board as it travels, more for a longer trip.
                    float lift = Mathf.Sin(k * Mathf.PI) * Mathf.Min(.35f, Vector3.Distance(from[i], to[i]) / Mathf.Max(.01f, cell) * .08f) * cell;
                    moving[i].transform.position = Vector3.Lerp(from[i], to[i], e) + Vector3.up * lift;
                }
                yield return null;
            }
            for (int i = 0; i < moving.Length; i++) if (moving[i]) moving[i].transform.position = to[i];
            done(true);
        }

        static int ColourAt(LevelManager lm, int col, int row)
        {
            var sq = lm.GetSquare(col, row);
            if (!sq || sq.IsNone() || !sq.item || sq.item.currentType != ItemsTypes.NONE || RoyalThrone.Campaign.RoyalCampaignBlockers.IsSpecialItem(sq.item)) return int.MinValue;
            return sq.item.color;
        }

        // A line of three or (with the royal effects) a 2x2 square already on the board.
        static bool ReadyMatch(LevelManager lm)
        {
            bool squaresMatch = RoyalEffectsProfile.Get;
            for (int row = 0; row < lm.maxRows; row++)
                for (int col = 0; col < lm.maxCols; col++)
                {
                    int c = ColourAt(lm, col, row); if (c == int.MinValue) continue;
                    if (ColourAt(lm, col + 1, row) == c && ColourAt(lm, col + 2, row) == c) return true;
                    if (ColourAt(lm, col, row + 1) == c && ColourAt(lm, col, row + 2) == c) return true;
                    if (squaresMatch && ColourAt(lm, col + 1, row) == c && ColourAt(lm, col, row + 1) == c && ColourAt(lm, col + 1, row + 1) == c) return true;
                }
            return false;
        }

        // How many swaps make a match (counted up to `cap`).
        static int Moves(LevelManager lm, int cap)
        {
            int n = 0;
            for (int row = 0; row < lm.maxRows; row++)
                for (int col = 0; col < lm.maxCols; col++)
                {
                    var sq = lm.GetSquare(col, row); if (!sq || !sq.item) continue;
                    foreach (var d in new[] { Vector2Int.right, Vector2Int.up })
                    {
                        var next = lm.GetSquare(col + d.x, row + d.y); if (!next || !next.item) continue;
                        if (RoyalHintSolver.EvaluateSwap(lm, sq.item, next.item) != null && ++n >= cap) return n;
                    }
                }
            return n;
        }
    }
}
