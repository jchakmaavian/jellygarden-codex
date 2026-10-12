using JellyGarden.Scripts;
using UnityEditor;
using UnityEngine;

/// <summary>Developer-assisted gameplay: exercises the existing swap/resolution path, not pointer input.</summary>
public static class RoyalThroneBoardPlaytest
{
    [MenuItem("Tools/Royal Throne/Playtest/Play suggested swap (bypasses pointer) _F9")]
    public static void PlaySuggestedSwap()
    {
        LevelManager level = LevelManager.THIS;
        AI hints = AI.THIS;
        if (!EditorApplication.isPlaying || level == null ||
            level.gameStatus != GameState.Playing || level.DragBlocked ||
            hints == null || !hints.gotTip || hints.tipItem == null ||
            hints.tipItem.falling || hints.tipItem.destroying || hints.vDirection == Vector3.zero)
        {
            Debug.Log("Royal Throne playtest: wait for a settled board and its suggested move.");
            return;
        }

        Debug.Log($"Royal Throne developer-assisted swap: level {level.currentLevel}, " +
            $"moves {level.Limit}, score {LevelManager.Score}, direction {hints.vDirection}. Pointer input bypassed.");
        // Item expects press position minus current position, the inverse of travel.
        hints.tipItem.SwitchDirection(-hints.vDirection);
    }
}
