// Builds a real macOS player so the game can be run and looked at, rather than
// only compiled and asserted about.
//
// Unity -batchmode -quit -nographics -projectPath . \
//   -executeMethod JellyGarden.Scripts.Story.Editor.StoryBuild.BuildMac -logFile -

using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace JellyGarden.Scripts.Story.Editor
{
    public static class StoryBuild
    {
        private const string OutputPath = "Builds/Mac/KingLeo.app";

        [MenuItem("JellyGarden/Story/Build macOS Player")]
        public static void BuildMac()
        {
            Build(development: true);
        }

        /// <summary>
        /// A clean player to actually play: no development watermark and no
        /// on-screen console. Debug.Log still reaches Player.log either way.
        /// </summary>
        [MenuItem("JellyGarden/Story/Build macOS Player (Release)")]
        public static void BuildMacRelease()
        {
            Build(development: false);
        }

        private static void Build(bool development)
        {
            string[] scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                Debug.LogError("[Story] No enabled scenes in build settings.");
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log("[Story] Building " + scenes.Length + " scenes: " +
                      string.Join(", ", scenes));

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = OutputPath,
                target = BuildTarget.StandaloneOSX,
                targetGroup = BuildTargetGroup.Standalone,
                options = development
                    ? BuildOptions.Development | BuildOptions.AllowDebugging
                    : BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            Debug.Log(string.Format(
                "[Story] Build {0} ({1}): {2} errors, {3} warnings, {4:0.0} MB, {5:0}s -> {6}",
                summary.result, development ? "development" : "release",
                summary.totalErrors, summary.totalWarnings,
                summary.totalSize / 1024f / 1024f, summary.totalTime.TotalSeconds,
                OutputPath));

            EditorApplication.Exit(summary.result == BuildResult.Succeeded ? 0 : 1);
        }
    }
}
