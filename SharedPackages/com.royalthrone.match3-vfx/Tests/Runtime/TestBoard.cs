using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RoyalThrone.Match3Vfx.Tests
{
    /// <summary>Grid with row 0 at the bottom that records every hit with its game time.</summary>
    internal sealed class TestBoard : IMatch3Board
    {
        public struct Hit
        {
            public Vector2Int cell;
            public Match3HitSource source;
            public float time;
        }

        public readonly List<Hit> Hits = new List<Hit>();
        public readonly HashSet<Vector2Int> Holes = new HashSet<Vector2Int>();
        readonly float pitch;

        public TestBoard(int columns, int rows, float pitch)
        {
            Columns = columns;
            Rows = rows;
            this.pitch = pitch;
        }

        public int Columns { get; }
        public int Rows { get; }
        public IEnumerable<Vector2Int> UniqueCells => Hits.Select(h => h.cell).Distinct();

        public bool IsCell(Vector2Int c) => c.x >= 0 && c.y >= 0 && c.x < Columns && c.y < Rows && !Holes.Contains(c);

        public Vector3 CellToWorld(Vector2Int c) => new Vector3(c.x * pitch, c.y * pitch, 0f);

        public void HitCell(Vector2Int c, Match3HitSource source) => Hits.Add(new Hit { cell = c, source = source, time = Time.time });

        public bool TryPickHelicopterTarget(Vector2Int origin, ICollection<Vector2Int> reserved, out Vector2Int target)
        {
            target = default;
            int best = -1;
            for (int y = 0; y < Rows; y++)
            {
                for (int x = 0; x < Columns; x++)
                {
                    var c = new Vector2Int(x, y);
                    if (c == origin || !IsCell(c) || reserved.Contains(c)) continue;
                    int d = Mathf.Abs(c.x - origin.x) + Mathf.Abs(c.y - origin.y);
                    if (d > best) { best = d; target = c; }
                }
            }
            return best >= 0;
        }
    }
}
