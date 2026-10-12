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
        public static event Action<LevelManager, int> MatchResolved;
        public static event Action<LevelManager, Item> PowerupCreated;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { ExtraMovesGranted = null; GoalsCompleted = null; MatchResolved = null; PowerupCreated = null; }

        public static void NotifyMatchResolved(LevelManager level, int matchedPieces)
        {
            if (level && level.gameStatus == GameState.Playing && matchedPieces >= 3)
                MatchResolved?.Invoke(level, matchedPieces);
        }

        public static void NotifyPowerupCreated(LevelManager level, Item item)
        {
            // A committed piece in the current board, independent of whether its reveal uses native or legacy visuals.
            // Preview objects, retired boards and non-power type changes cannot produce gameplay feedback.
            if (!level || level != LevelManager.THIS || level.gameStatus != GameState.Playing ||
                !level.GameField || !level.GameField.gameObject.activeInHierarchy || !item || !item.isActiveAndEnabled ||
                item.destroying || !item.square || item.square.item != item ||
                !item.transform.IsChildOf(level.GameField) || !item.square.transform.IsChildOf(level.GameField) ||
                level.GetSquare(item.square.col, item.square.row, true) != item.square) return;
            switch (item.currentType)
            {
                case ItemsTypes.HORIZONTAL_STRIPPED:
                case ItemsTypes.VERTICAL_STRIPPED:
                case ItemsTypes.PACKAGE:
                case ItemsTypes.BOMB:
                case ItemsTypes.HELICOPTER:
                    PowerupCreated?.Invoke(level, item);
                    break;
            }
        }

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
