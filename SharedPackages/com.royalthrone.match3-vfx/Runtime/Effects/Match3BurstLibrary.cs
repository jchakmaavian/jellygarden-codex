using System;
using UnityEngine;

namespace RoyalThrone.Match3Vfx
{
    /// <summary>
    /// Painted debris chips per colour index. Index 0 is used for pieces with colour index 0,
    /// and so on. Build one from burst sheets with
    /// Assets > Create > Royal Throne > Match-3 Burst Library From Selected Textures.
    /// </summary>
    [CreateAssetMenu(fileName = "Match3BurstLibrary", menuName = "Royal Throne/Match-3 Burst Library (empty)", order = 1)]
    public sealed class Match3BurstLibrary : ScriptableObject
    {
        [Serializable]
        public sealed class Piece
        {
            public Sprite sprite;
            [Tooltip("Chip centre relative to the sheet centre, in sheet widths (-0.5..0.5).")]
            public Vector2 offset;
        }

        [Serializable]
        public sealed class Burst
        {
            public string label;
            public Piece[] pieces = new Piece[0];
        }

        public Burst[] bursts = new Burst[0];

        public bool Has(int colorIndex)
        {
            return bursts != null && colorIndex >= 0 && colorIndex < bursts.Length &&
                   bursts[colorIndex] != null && bursts[colorIndex].pieces != null &&
                   bursts[colorIndex].pieces.Length > 0 && bursts[colorIndex].pieces[0].sprite;
        }
    }
}
