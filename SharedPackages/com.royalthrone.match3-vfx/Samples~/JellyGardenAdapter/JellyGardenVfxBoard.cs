using System;
using System.Collections.Generic;
using JellyGarden.Scripts;
using UnityEngine;

namespace RoyalThrone.Match3Vfx.Adapters
{
    /// <summary>
    /// Maps kit cells to JellyGarden squares. Cell (x, y) = (square.col, square.row); row 0 is the top
    /// row, as in LevelManager.GetSquare. HitCell uses JellyGarden's own DestroyItem/DestroyBlock,
    /// so targets, scores and blockers keep their existing rules.
    /// </summary>
    public sealed class JellyGardenVfxBoard : IMatch3Board
    {
        /// <summary>Raised before an item is destroyed by a power-up. Set project-specific flags here.</summary>
        public static event Action<Item, Match3HitSource> BeforeItemHit;
        /// <summary>Raised before a blocker layer is chipped by a power-up.</summary>
        public static event Action<Square, Match3HitSource> BeforeBlockHit;

        static readonly Dictionary<Item, Match3HitSource> pendingSources = new Dictionary<Item, Match3HitSource>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            BeforeItemHit = null;
            BeforeBlockHit = null;
            pendingSources.Clear();
        }

        static LevelManager LM => LevelManager.THIS;

        public int Columns => LM ? LM.maxCols : 0;
        public int Rows => LM ? LM.maxRows : 0;

        public static Vector2Int CellOf(Square square) => new Vector2Int(square.col, square.row);
        public static Vector2Int CellOf(Item item) => item && item.square ? CellOf(item.square) : new Vector2Int(-1, -1);

        public static Square SquareAt(Vector2Int c)
        {
            var lm = LM;
            if (!lm || lm.squaresArray == null || c.x < 0 || c.y < 0 || c.x >= lm.maxCols || c.y >= lm.maxRows) return null;
            return lm.GetSquare(c.x, c.y);
        }

        /// <summary>
        /// Pop style for an item that is being destroyed: Rocket or PowerClear when a kit sequence hit it,
        /// otherwise <paramref name="fallback"/>. Call once from the item's destroy coroutine.
        /// </summary>
        public static Match3PopKind TakePopKind(Item item, Match3PopKind fallback = Match3PopKind.Match)
        {
            if (!item || !pendingSources.TryGetValue(item, out var source)) return fallback;
            pendingSources.Remove(item);
            return source == Match3HitSource.Rocket ? Match3PopKind.Rocket : Match3PopKind.PowerClear;
        }

        public bool IsCell(Vector2Int c)
        {
            var square = SquareAt(c);
            return square && !square.IsNone();
        }

        public Vector3 CellToWorld(Vector2Int c)
        {
            var lm = LM;
            if (!lm || !lm.GameField || lm.maxCols <= 0 || lm.maxRows <= 0) return Vector3.zero;
            var clamped = new Vector2Int(Mathf.Clamp(c.x, 0, lm.maxCols - 1), Mathf.Clamp(c.y, 0, lm.maxRows - 1));
            var square = lm.GetSquare(clamped.x, clamped.y);
            Vector3 basePosition = square ? square.transform.position : lm.GameField.position;
            // Squares are laid out at (col * width, -row * height) inside GameField.
            float width = lm.squareWidth * lm.GameField.lossyScale.x;
            float height = lm.squareHeight * lm.GameField.lossyScale.y;
            return basePosition + new Vector3((c.x - clamped.x) * width, -(c.y - clamped.y) * height, 0f);
        }

        public void HitCell(Vector2Int c, Match3HitSource source)
        {
            var square = SquareAt(c);
            if (!square || square.IsNone() || square.type == SquareTypes.UNDESTROYABLE) return;
            var item = square.item;
            if (item && item.currentType != ItemsTypes.INGREDIENT)
            {
                if (item.destroying) return;
                pendingSources[item] = source;
                BeforeItemHit?.Invoke(item, source);
                item.DestroyItem(true);
            }
            else if (square.block != null && square.block.Count > 0)
            {
                BeforeBlockHit?.Invoke(square, source);
                square.DestroyBlock();
            }
        }

        /// <summary>Blockers first, then distance, with a little randomness so repeats vary.</summary>
        public bool TryPickHelicopterTarget(Vector2Int origin, ICollection<Vector2Int> reserved, out Vector2Int target)
        {
            target = default;
            var lm = LM;
            if (!lm) return false;
            Vector3 from = CellToWorld(origin);
            float best = float.NegativeInfinity;
            for (int row = 0; row < lm.maxRows; row++)
            {
                for (int col = 0; col < lm.maxCols; col++)
                {
                    var cell = new Vector2Int(col, row);
                    var square = lm.GetSquare(col, row);
                    if (!square || cell == origin || square.IsNone() || square.type == SquareTypes.UNDESTROYABLE || reserved.Contains(cell)) continue;
                    bool hasBlock = square.block != null && square.block.Count > 0;
                    if (!square.item && !hasBlock) continue;
                    if (square.item && square.item.currentType == ItemsTypes.INGREDIENT) continue;
                    float value = (hasBlock ? 100f : 0f) + Vector3.Distance(from, square.transform.position) + UnityEngine.Random.value * 3f;
                    if (value > best) { best = value; target = cell; }
                }
            }
            return best > float.NegativeInfinity;
        }
    }
}
