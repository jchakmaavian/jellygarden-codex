// Puts the Editor back where a person expects to find it.
//
// Two things go wrong after a platform build runs in batchmode. The active
// build target is left on the platform that was built, and the scene the
// Editor reopens on is whatever was loaded when the batch process exited --
// which for a build is nothing, so Library/LastSceneManagerSetup.txt is
// written empty and the next Editor launch lands on an untitled scene with
// only a camera in it.
//
// Neither is a fault in the project, and neither can be fixed from outside the
// Editor: the target lives in Library/EditorUserBuildSettings.asset and the
// scene record is written by the scene manager on exit. So this does it from
// inside, in batchmode, with the Editor closed.

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace JellyGarden.Scripts.Story.Editor
{
    public static class StoryEditorState
    {
        private const string MainScene = "Assets/JellyGarden/Scenes/main.unity";

        /// <summary>
        /// Switches the active build target back to macOS and opens the main
        /// scene, so the next Editor launch restores both.
        /// </summary>
        public static void RestoreMac()
        {
            Restore(BuildTarget.StandaloneOSX, BuildTargetGroup.Standalone, "macOS");
        }

        /// <summary>The same, but leaving the target on iOS.</summary>
        public static void RestoreScene()
        {
            OpenMain();
            Finish(0);
        }

        private static void Restore(BuildTarget target, BuildTargetGroup group, string name)
        {
            if (EditorUserBuildSettings.activeBuildTarget != target)
            {
                Debug.Log("[Story] switching active build target to " + name + "...");
                // Synchronous: the reimport happens inside this call, which is
                // why this method is worth running in batchmode rather than
                // making a person wait on it with the Editor open.
                bool ok = EditorUserBuildSettings.SwitchActiveBuildTarget(group, target);
                if (!ok)
                {
                    Debug.LogError("[Story] could not switch to " + name +
                                   "; is the platform module installed?");
                    Finish(1);
                    return;
                }
            }

            Debug.Log("[Story] active build target is " +
                      EditorUserBuildSettings.activeBuildTarget + ".");

            OpenMain();
            Finish(0);
        }

        private static void OpenMain()
        {
            // Opened through EditorSceneManager rather than SceneManager: this
            // is what writes LastSceneManagerSetup.txt, and that file is the
            // whole point -- an empty one is what leaves the Editor on an
            // untitled scene.
            UnityEngine.SceneManagement.Scene scene =
                EditorSceneManager.OpenScene(MainScene, OpenSceneMode.Single);

            Debug.Log(scene.IsValid()
                ? "[Story] opened " + MainScene + " (" + scene.rootCount + " root objects)."
                : "[Story] FAILED to open " + MainScene + ".");
        }

        private static void Finish(int code)
        {
            if (Application.isBatchMode) EditorApplication.Exit(code);
        }
    }
}
