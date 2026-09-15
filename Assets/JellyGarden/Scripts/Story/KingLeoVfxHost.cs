// Bridges the Royal Throne Match-3 VFX kit to this board.
//
// The kit reads the game's state through IMatch3VfxHost rather than reaching
// into LevelManager itself: cell size for effect scaling, pause so particles
// hold, and the board's bottom Y so shatter debris is recycled when it falls
// off the board instead of falling forever.
//
// Only the match pop goes through the kit. Power-ups, swaps and falls are still
// the game's own code, so IMatch3Board is deliberately not implemented — none
// of the sequences that need a grid (rocket, TNT, helicopter, orb) are used.

using UnityEngine;
using RoyalThrone.Match3Vfx;

namespace JellyGarden.Scripts.Story
{
    public sealed class KingLeoVfxHost : IMatch3VfxHost
    {
        /// <summary>
        /// Points the kit at this board. Safe to call more than once: the kit
        /// keeps one host, and a second call simply replaces it with an
        /// equivalent one after a level reload.
        /// </summary>
        public static void Configure()
        {
            Match3Vfx.Configure(null, new KingLeoVfxHost());
            Match3Vfx.Prewarm();
        }

        private static LevelManager L
        {
            get { return LevelManager.THIS; }
        }

        /// <summary>
        /// World units per cell. Squares are placed at col * squareWidth, so
        /// this is the grid pitch the kit sizes bursts and chips against.
        /// </summary>
        public float CellSize
        {
            get { return L != null ? L.squareWidth : 1.2f; }
        }

        public bool IsPaused
        {
            get { return L != null && L.gameStatus == GameState.Pause; }
        }

        /// <summary>
        /// No accessibility toggle exists in this project yet. Returning false
        /// keeps the full effect; wire a preference here when one is added.
        /// </summary>
        public bool ReducedMotion
        {
            get { return false; }
        }

        /// <summary>
        /// SoundBase has no mute flag of its own — volume is the only control —
        /// so sound is on whenever the mixer is audible.
        /// </summary>
        public bool SoundEnabled
        {
            get { return SoundBase.Instance == null || SoundBase.Instance.GetVolume() > 0f; }
        }

        /// <summary>
        /// World Y of the lowest row. Rows are laid out downward from
        /// firstSquarePosition (-row * squareHeight), so the last row is the
        /// lowest one. Debris is recycled below this instead of falling forever.
        /// </summary>
        public float BoardBottomY
        {
            get
            {
                if (L == null) return float.NegativeInfinity;
                return L.firstSquarePosition.y - (L.maxRows - 1) * L.squareHeight;
            }
        }

        /// <summary>
        /// The match pop never shakes the camera — only the kit's power-up
        /// sequences do, and those are not used here. Null disables shake.
        /// </summary>
        public Camera ShakeCamera
        {
            get { return null; }
        }
    }
}
