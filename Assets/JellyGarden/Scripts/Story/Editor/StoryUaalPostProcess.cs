// Makes the exported Xcode project produce a UnityFramework that can be dropped
// into another app — Unity as a Library — rather than only a standalone player.
//
// Unity already writes a UnityFramework target into every iOS export, so the
// engine and the IL2CPP game code are embeddable as they stand. What is not is
// the Data folder: Unity puts it in the Unity-iPhone app target's Resources
// phase, so it lands next to the app binary. A host app that embeds only the
// .framework therefore gets an engine with no scenes, no assets and no managed
// assemblies — it boots to a black screen and logs nothing useful, which is the
// single most common way a first UaaL integration fails.
//
// Moving Data into the UnityFramework target makes the framework self-contained:
// one artifact to hand to the host, and the host's only obligation is to tell
// Unity where to look for it (setDataBundleId with the framework's bundle id,
// com.unity3d.framework, rather than the host's own).
//
// This runs on every iOS export including the standalone player build. That is
// safe: Unity-iPhone links UnityFramework and loads Data through it either way,
// so the standalone player keeps working with the data one level in.

using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Callbacks;
using UnityEngine;
#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif

namespace JellyGarden.Scripts.Story.Editor
{
    public static class StoryUaalPostProcess
    {
        /// <summary>
        /// The bundle id Unity gives the framework, and so the id a host app has
        /// to pass to setDataBundleId before calling runEmbedded. Named here
        /// because the host side has no way to discover it.
        /// </summary>
        public const string FrameworkBundleId = "com.unity3d.framework";

        /// <summary>
        /// Runs late so Unity has finished writing the project. A higher callback
        /// order than any of Unity's own post-processors, which sit below 100.
        /// </summary>
        [PostProcessBuild(999)]
        public static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject)
        {
#if UNITY_IOS
            if (target != BuildTarget.iOS)
                return;

            string projectPath = PBXProject.GetPBXProjectPath(pathToBuiltProject);
            PBXProject project = new PBXProject();
            project.ReadFromFile(projectPath);

            string appTarget = project.GetUnityMainTargetGuid();
            string frameworkTarget = project.GetUnityFrameworkTargetGuid();

            // Data is a folder reference rather than a group, so it is one file
            // guid covering the whole tree — moving it moves everything under it.
            string dataGuid = project.FindFileGuidByProjectPath("Data");
            if (string.IsNullOrEmpty(dataGuid))
            {
                Debug.LogWarning("[Story] UaaL: no Data folder in the exported project; " +
                                 "leaving target membership alone.");
                return;
            }

            project.RemoveFileFromBuild(appTarget, dataGuid);
            project.AddFileToBuild(frameworkTarget, dataGuid);

            project.WriteToFile(projectPath);

            Debug.Log("[Story] UaaL: Data moved from Unity-iPhone to UnityFramework. " +
                      "The framework is now self-contained; a host app must call " +
                      "setDataBundleId(\"" + FrameworkBundleId + "\") before runEmbedded.");
#endif
        }
    }
}
