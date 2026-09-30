using System;
using JellyGarden.Scripts;
using UnityEngine;

namespace RoyalThrone.Characters
{
    /// <summary>Local game-to-character adapter events. Emit only after the game commits the corresponding action.</summary>
    public static class KingVanguardActivityEvents
    {
        public static event Action<LevelManager> ExtraMovesGranted;
        public static event Action<LevelManager> GoalsCompleted;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { ExtraMovesGranted = null; GoalsCompleted = null; }

        public static void NotifyExtraMovesGranted(LevelManager level)
        {
            if (level && level.gameStatus == GameState.Playing && level.limitType == LIMIT.MOVES)
                ExtraMovesGranted?.Invoke(level);
        }

        public static void NotifyGoalsCompleted(LevelManager level)
        {
            if (level && level.gameStatus == GameState.PreWinAnimations)
                GoalsCompleted?.Invoke(level);
        }
    }
}
