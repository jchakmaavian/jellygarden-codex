using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace RoyalThrone.Editor
{
    // Builds a sideloadable APK for QA on physical Android devices.
    // Batch mode: -executeMethod RoyalThrone.Editor.RoyalThroneAndroidBuild.BuildQaApk
    public static class RoyalThroneAndroidBuild
    {
        public const string OutputPath = "Builds/Android/RoyalThrone-QA.apk";

        [MenuItem("Tools/Royal Throne/Build/Android QA APK")]
        public static void BuildQaApk()
        {
            RoyalHomeScreenBuilder.RequireCopy();
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode before building.");
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android)) throw new BuildFailedException("Android Build Support is not installed for this Unity version.");

            RoyalOfflineBuildPolicy.Configure(NamedBuildTarget.Android);

            EditorUserBuildSettings.buildAppBundle = false;
            EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
            Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = OutputPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None
            });
            var summary = report.summary;
            if (summary.result != BuildResult.Succeeded) throw new BuildFailedException($"Android QA build {summary.result}: {summary.totalErrors} error(s). See the Console for details.");
            Debug.Log($"[Royal Throne] Android QA APK built at {OutputPath} ({new FileInfo(OutputPath).Length / 1048576f:F1} MB, {summary.totalTime:mm\\:ss}).");
        }
    }
}
