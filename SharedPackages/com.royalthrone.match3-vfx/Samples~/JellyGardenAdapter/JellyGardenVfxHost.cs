using JellyGarden.Scripts;
using UnityEngine;

namespace RoyalThrone.Match3Vfx.Adapters
{
    /// <summary>Reads cell size, pause state and preferences from a JellyGarden LevelManager.</summary>
    public sealed class JellyGardenVfxHost : IMatch3VfxHost
    {
        /// <summary>PlayerPrefs int; 0 means reduced motion. Empty = never reduced.</summary>
        public string reducedMotionKey = "RoyalThrone.HomeMotion";
        /// <summary>PlayerPrefs int used by JellyGarden's sound toggle; 0 means muted.</summary>
        public string soundKey = "Sound";

        int bottomFrame = -1;
        float bottom = float.NegativeInfinity;

        public float CellSize
        {
            get
            {
                var lm = LevelManager.THIS;
                return lm && lm.GameField ? Mathf.Abs(lm.squareWidth * lm.GameField.lossyScale.x) : 1f;
            }
        }

        public bool IsPaused => LevelManager.THIS && LevelManager.THIS.gameStatus == GameState.Pause;

        public bool ReducedMotion => !string.IsNullOrEmpty(reducedMotionKey) && PlayerPrefs.GetInt(reducedMotionKey, 1) == 0;

        public bool SoundEnabled => string.IsNullOrEmpty(soundKey) || PlayerPrefs.GetInt(soundKey, 1) != 0;

        public float BoardBottomY
        {
            get
            {
                // Many chips can ask in one frame; scan the squares once per frame.
                if (bottomFrame == Time.frameCount) return bottom;
                bottomFrame = Time.frameCount;
                bottom = float.NegativeInfinity;
                var lm = LevelManager.THIS;
                if (!lm || lm.squaresArray == null) return bottom;
                float lowest = float.PositiveInfinity;
                foreach (var square in lm.squaresArray)
                    if (square && !square.IsNone()) lowest = Mathf.Min(lowest, square.transform.position.y);
                if (!float.IsPositiveInfinity(lowest)) bottom = lowest;
                return bottom;
            }
        }

        public Camera ShakeCamera => Camera.main;
    }
}
