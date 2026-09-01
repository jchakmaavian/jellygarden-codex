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
                // Development so the player writes a log we can read, and so a
                // runtime exception is visible rather than swallowed.
                options = BuildOptions.Development | BuildOptions.AllowDebugging
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            Debug.Log(string.Format(
                "[Story] Build {0}: {1} errors, {2} warnings, {3:0.0} MB, {4:0}s -> {5}",
                summary.result, summary.totalErrors, summary.totalWarnings,
                summary.totalSize / 1024f / 1024f, summary.totalTime.TotalSeconds,
                OutputPath));

            EditorApplication.Exit(summary.result == BuildResult.Succeeded ? 0 : 1);
        }
    }
}
