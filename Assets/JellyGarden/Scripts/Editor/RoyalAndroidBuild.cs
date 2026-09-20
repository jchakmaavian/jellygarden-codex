using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
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
        public static string LogPath => Path.Combine(OutputDir, "last-build.log");

        /// <summary>Applies the QA build configuration without building.</summary>
        public static string Configure()
        {
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
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

            EditorUserBuildSettings.development = true;
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
            var started = DateTime.Now;
            Directory.CreateDirectory(OutputDir);
            File.WriteAllText(LogPath, "STATUS running\nstarted " + started.ToString("u") + "\n");

            string config = Configure();
            string apk = Path.Combine(OutputDir, ApkName);

            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = apk,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.Development | BuildOptions.CompressWithLz4
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
            log.Append("finished ").Append(DateTime.Now.ToString("u")).Append("\n");
            File.WriteAllText(LogPath, log.ToString());
        }
    }
}
