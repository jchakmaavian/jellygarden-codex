using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace RoyalThrone.Editor
{
    /// <summary>
    /// Builds the Android QA APK. The build runs for minutes and blocks the main thread, so the
    /// result is written to a log file rather than returned: the caller polls for that file.
    /// </summary>
    public static class RoyalAndroidBuild
    {
        public const string OutputDir = "Builds/Android";
        public const string ApkName = "RoyalThrone-Test.apk";
        public const string LevelsTestApk = "Design/Backup/apk/RoyalThrone-Levels01-50-Test.apk";
        public const string LevelsTestLog = "Design/Backup/apk/RoyalThrone-Levels01-50-Test-build.log";
        public const string LevelsTestApplicationId = "com.royalthrone.levelsqa";
        public static string LogPath => Path.Combine(OutputDir, "last-build.log");
        public const string PlayerApkName = "RoyalThrone.apk";
        public static string PlayerLogPath => Path.Combine(OutputDir, "last-player-build.log");

        /// <summary>Applies the QA build configuration without building.</summary>
        public static string Configure(bool development = true)
        {
            RoyalHomeScreenBuilder.RequireCopy();
            RoyalOfflineBuildPolicy.Configure(NamedBuildTarget.Android);
            var log = new StringBuilder();

            // Portrait only. The project shipped as AutoRotation, which let the board letterbox
            // into landscape on a phone.
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;

            // APK, ARM64, IL2CPP: all already the project's settings, restated so a QA build is
            // reproducible from a clean checkout.
            EditorUserBuildSettings.buildAppBundle = false;
            EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

            EditorUserBuildSettings.development = development;
            EditorUserBuildSettings.allowDebugging = false;
            EditorUserBuildSettings.connectProfiler = false;
            EditorUserBuildSettings.buildWithDeepProfilingSupport = false;
            EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.Generic;

            // A stale macOS keystore path was left in the project. Custom signing is off, so it is
            // inert, but clearing it removes an obvious way for a later build to fail.
            PlayerSettings.Android.useCustomKeystore = false;
            PlayerSettings.Android.keystoreName = string.Empty;
            PlayerSettings.Android.keyaliasName = string.Empty;

            log.Append("orientation=").Append(PlayerSettings.defaultInterfaceOrientation)
               .Append(" backend=").Append(PlayerSettings.GetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android))
               .Append(" archs=").Append(PlayerSettings.Android.targetArchitectures)
               .Append(" appBundle=").Append(EditorUserBuildSettings.buildAppBundle)
               .Append(" development=").Append(EditorUserBuildSettings.development)
               .Append(" debugging=").Append(EditorUserBuildSettings.allowDebugging)
               .Append(" profiler=").Append(EditorUserBuildSettings.connectProfiler)
               .Append(" id=").Append(PlayerSettings.GetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android));
            return log.ToString();
        }

        [MenuItem("Royal Throne/Build Android QA APK")]
        public static void BuildAndroid()
        {
            BuildAndroidTo(Path.Combine(OutputDir, ApkName), LogPath);
        }

        /// <summary>
        /// The player APK: a release build with ROYAL_DISABLE_QA, so the Mod Menu, the QA service and
        /// the QA save sandbox are not compiled in. Everything else matches the QA APK.
        /// </summary>
        [MenuItem("Royal Throne/Build Android Player APK (no Mod Menu)")]
        public static void BuildPlayer()
        {
            BuildAndroidTo(Path.Combine(OutputDir, PlayerApkName), PlayerLogPath, player: true);
        }

        [MenuItem("Royal Throne/Build Levels 01-50 Test APK")]
        public static void BuildLevels01To50Test()
        {
            RoyalHomeScreenBuilder.RequireCopy();
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before building.");
            var state = new AndroidBuildState();
            try
            {
                // A distinct package gives QA its own Android data directory and PlayerPrefs.
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, LevelsTestApplicationId);
                PlayerSettings.productName = "Royal Throne Levels 01-50 Test";
                BuildAndroidTo(LevelsTestApk, LevelsTestLog, true);
            }
            finally
            {
                state.Restore();
                AssetDatabase.SaveAssets();
            }
        }

        static void BuildAndroidTo(string apk, string logPath, bool referenceCampaign = false, bool player = false)
        {
            var started = DateTime.Now;
            Directory.CreateDirectory(Path.GetDirectoryName(apk));
            File.WriteAllText(logPath, "STATUS running\nstarted " + started.ToString("u") + "\n");

            // Configure persists the development flag; a player build restores it afterwards.
            bool wasDevelopment = EditorUserBuildSettings.development;
            string config = Configure(!player);

            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = apk,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                extraScriptingDefines = player ? new[]{"ROYAL_DISABLE_QA"}
                    : referenceCampaign ? new[]{"ROYAL_REFERENCE_CAMPAIGN"} : Array.Empty<string>(),
                options = player ? BuildOptions.CompressWithLz4HC : BuildOptions.Development | BuildOptions.CompressWithLz4
            };

            var log = new StringBuilder();
            try
            {
                BuildReport report = BuildPipeline.BuildPlayer(options);
                var summary = report.summary;
                log.Append("STATUS ").Append(summary.result == BuildResult.Succeeded ? "succeeded" : "failed").Append("\n");
                log.Append("result=").Append(summary.result).Append("\n");
                log.Append("errors=").Append(summary.totalErrors).Append(" warnings=").Append(summary.totalWarnings).Append("\n");
                log.Append("time=").Append(summary.totalTime).Append("\n");
                log.Append("outputPath=").Append(summary.outputPath).Append("\n");
                log.Append("sizeBytes=").Append(summary.totalSize).Append("\n");
                log.Append("config=").Append(config).Append("\n");
                log.Append("scenes=").Append(string.Join(";", scenes)).Append("\n");

                foreach (var step in report.steps)
                    foreach (var message in step.messages)
                        if (message.type == LogType.Error || message.type == LogType.Exception)
                            log.Append("ERROR [").Append(step.name).Append("] ").Append(message.content.Replace("\n", " ")).Append("\n");
            }
            catch (Exception e)
            {
                log.Append("STATUS failed\nEXCEPTION ").Append(e.GetType().Name).Append(": ").Append(e.Message.Replace("\n", " ")).Append("\n");
            }
            if (player) EditorUserBuildSettings.development = wasDevelopment;
            log.Append("finished ").Append(DateTime.Now.ToString("u")).Append("\n");
            File.WriteAllText(logPath, log.ToString());
        }

        sealed class AndroidBuildState
        {
            readonly string applicationId = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android);
            readonly string productName = PlayerSettings.productName;
            readonly UIOrientation orientation = PlayerSettings.defaultInterfaceOrientation;
            readonly bool portrait = PlayerSettings.allowedAutorotateToPortrait;
            readonly bool upsideDown = PlayerSettings.allowedAutorotateToPortraitUpsideDown;
            readonly bool landscapeLeft = PlayerSettings.allowedAutorotateToLandscapeLeft;
            readonly bool landscapeRight = PlayerSettings.allowedAutorotateToLandscapeRight;
            readonly ScriptingImplementation backend = PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android);
            readonly AndroidArchitecture architectures = PlayerSettings.Android.targetArchitectures;
            readonly bool appBundle = EditorUserBuildSettings.buildAppBundle;
            readonly bool exportProject = EditorUserBuildSettings.exportAsGoogleAndroidProject;
            readonly bool development = EditorUserBuildSettings.development;
            readonly bool debugging = EditorUserBuildSettings.allowDebugging;
            readonly bool profiler = EditorUserBuildSettings.connectProfiler;
            readonly bool deepProfiling = EditorUserBuildSettings.buildWithDeepProfilingSupport;
            readonly MobileTextureSubtarget subtarget = EditorUserBuildSettings.androidBuildSubtarget;
            readonly bool customKeystore = PlayerSettings.Android.useCustomKeystore;
            readonly string keystoreName = PlayerSettings.Android.keystoreName;
            readonly string keyaliasName = PlayerSettings.Android.keyaliasName;

            public void Restore()
            {
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, applicationId);
                PlayerSettings.productName = productName;
                PlayerSettings.defaultInterfaceOrientation = orientation;
                PlayerSettings.allowedAutorotateToPortrait = portrait;
                PlayerSettings.allowedAutorotateToPortraitUpsideDown = upsideDown;
                PlayerSettings.allowedAutorotateToLandscapeLeft = landscapeLeft;
                PlayerSettings.allowedAutorotateToLandscapeRight = landscapeRight;
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, backend);
                PlayerSettings.Android.targetArchitectures = architectures;
                EditorUserBuildSettings.buildAppBundle = appBundle;
                EditorUserBuildSettings.exportAsGoogleAndroidProject = exportProject;
                EditorUserBuildSettings.development = development;
                EditorUserBuildSettings.allowDebugging = debugging;
                EditorUserBuildSettings.connectProfiler = profiler;
                EditorUserBuildSettings.buildWithDeepProfilingSupport = deepProfiling;
                EditorUserBuildSettings.androidBuildSubtarget = subtarget;
                PlayerSettings.Android.useCustomKeystore = customKeystore;
                PlayerSettings.Android.keystoreName = keystoreName;
                PlayerSettings.Android.keyaliasName = keyaliasName;
            }
        }
    }
}
