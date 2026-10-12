using JellyGarden.Scripts;
using UnityEngine;

namespace JellyGarden.Scripts.Juice
{
    /// <summary>
    /// What the level tells the HUD's king portrait. Royal Throne raised these on its
    /// KingVanguard character directly; the port routes them through here so the
    /// board, the director and the tools stay free of any character type, and King
    /// Leo's own StoryAvatar is the one listening (plan D2). Raise only after the
    /// game has committed the action.
    /// </summary>
    public static class AvatarEvents
    {
        public static event global::System.Action<LevelManager> ExtraMovesGranted;
        public static event global::System.Action<LevelManager> GoalsCompleted;
        public static event global::System.Action<LevelManager, int> MatchResolved;
        public static event global::System.Action<LevelManager, Item> PowerupCreated;
        public static event global::System.Action<LevelManager, bool> WorriedChanged;
        public static event global::System.Action<LevelManager> Lost;
        public static event global::System.Action<float> Reaction;
        public static event global::System.Action<int> ToolUsed;
        public static event global::System.Action<int> ToolStruck;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            ExtraMovesGranted = null; GoalsCompleted = null; MatchResolved = null; PowerupCreated = null;
            WorriedChanged = null; Lost = null; Reaction = null; ToolUsed = null; ToolStruck = null;
        }

        public static void NotifyMatchResolved(LevelManager level, int matchedPieces)
        {
            if (level && level.gameStatus == GameState.Playing && matchedPieces >= 3)
                MatchResolved?.Invoke(level, matchedPieces);
        }

        public static void NotifyPowerupCreated(LevelManager level, Item item)
        {
            // A committed piece on the current board, whatever drew its reveal.
            // Previews, retired boards and non-power type changes give no feedback.
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

        /// <summary>Raised when the moves (or time) left cross the "few left" line, either way.</summary>
        public static void NotifyWorried(LevelManager level, bool worried)
        {
            if (level) WorriedChanged?.Invoke(level, worried);
        }

        public static void NotifyLost(LevelManager level)
        {
            if (level) Lost?.Invoke(level);
        }

        /// <summary>How hard an effect hit, 0..1. Royal Throne: 0.5-0.75 happy, 0.75+ laugh.</summary>
        public static void NotifyReaction(float strength)
        {
            Reaction?.Invoke(Mathf.Clamp01(strength));
        }

        public static void NotifyToolUsed(int tool) { ToolUsed?.Invoke(tool); }

        public static void NotifyToolStruck(int tool) { ToolStruck?.Invoke(tool); }
    }
}
