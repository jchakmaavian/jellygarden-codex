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

        // Ads, purchases, social and backend integrations are not configured for this copy.
        // Their inherited IDs belong to the original game, so QA builds must not enable them.
        static readonly string[] ServiceDefines = { "UNITY_ADS", "UNITY_INAPPS", "GOOGLE_MOBILE_ADS", "CHARTBOOST_ADS", "FACEBOOK", "PLAYFAB", "GAMESPARKS" };

        [MenuItem("Tools/Royal Throne/Build/Android QA APK")]
        public static void BuildQaApk()
        {
            RoyalHomeScreenBuilder.RequireCopy();
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode before building.");
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android)) throw new BuildFailedException("Android Build Support is not installed for this Unity version.");

            // The inherited PostImporting script used to force UNITY_ADS on after imports, so strip service defines here rather than trusting settings.
            var defines = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Android).Split(';').Select(d => d.Trim()).Where(d => d.Length > 0).ToArray();
            var enabledServices = defines.Intersect(ServiceDefines).ToArray();
            if (enabledServices.Length > 0)
            {
                PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Android, string.Join(";", defines.Except(ServiceDefines)));
                Debug.LogWarning("[Royal Throne] Removed Android scripting defines for the QA build: " + string.Join(", ", enabledServices));
            }

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
