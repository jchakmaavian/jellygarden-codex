using UnityEditor;

namespace RoyalThrone.Editor
{
    /// <summary>
    /// Holds script reloads while the editor is in Play Mode and lets them run as soon as Play stops. A reload in the
    /// middle of Play resets every static: RoyalSave.Testing falls back to the player's own profile, InitScript's lives
    /// read 0 and are saved on exit, and the Input System and uGUI tables break until the next reload. Builds never
    /// reload, so this only protects editor sessions. Scripts edited during Play still compile; the new code loads
    /// when Play ends.
    /// </summary>
    [InitializeOnLoad]
    static class RoyalPlayModeReloadGuard
    {
        static bool locked;

        static RoyalPlayModeReloadGuard()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        static void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredPlayMode && !locked)
            {
                EditorApplication.LockReloadAssemblies();
                locked = true;
            }
            else if (change == PlayModeStateChange.ExitingPlayMode && locked)
            {
                EditorApplication.UnlockReloadAssemblies();
                locked = false;
            }
        }
    }
}
