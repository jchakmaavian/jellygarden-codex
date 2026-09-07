// Builds a real player so the game can be run and looked at, rather than only
// compiled and asserted about. macOS to look at it on this machine, iOS to put
// it in a hand.
//
// Unity -batchmode -quit -nographics -projectPath . \
//   -executeMethod JellyGarden.Scripts.Story.Editor.StoryBuild.BuildMac -logFile -
//   -executeMethod JellyGarden.Scripts.Story.Editor.StoryBuild.BuildIos -logFile -

using System.Linq;
using UnityEditor;
using UnityEditor.Build;
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

        private const string IosPath = "Builds/iOS";

        /// <summary>
        /// Writes the Xcode project rather than an .ipa: Unity's iOS target is a
        /// generated Xcode workspace, and xcodebuild is what turns it into
        /// something a phone will run. Signing is set here rather than left in
        /// ProjectSettings.asset by hand, so Unity writes the file and there is
        /// no chance of a YAML edit disagreeing with what the Editor believes.
        /// </summary>
        [MenuItem("JellyGarden/Story/Build iOS Xcode Project")]
        public static void BuildIos()
        {
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "com.vmp.kingleo");
            PlayerSettings.iOS.appleEnableAutomaticSigning = true;
            PlayerSettings.iOS.appleDeveloperTeamID = "Z62T7FR68P";

            // The kit ships portrait-only art and the story shell is built for
            // it, so the player is pinned rather than left to autorotate.
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;

            string[] scenes = EnabledScenes();
            if (scenes == null) return;

            Debug.Log("[Story] Building iOS from " + scenes.Length + " scene(s): " +
                      string.Join(", ", scenes));

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = IosPath,
                target = BuildTarget.iOS,
                targetGroup = BuildTargetGroup.iOS,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            Debug.Log(string.Format(
                "[Story] iOS build {0}: {1} errors, {2} warnings, {3:0}s -> {4}",
                summary.result, summary.totalErrors, summary.totalWarnings,
                summary.totalTime.TotalSeconds, IosPath));

            EditorApplication.Exit(summary.result == BuildResult.Succeeded ? 0 : 1);
        }

        /// <summary>The scenes the build settings actually ship, or null after logging why not.</summary>
        private static string[] EnabledScenes()
        {
            string[] scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            if (scenes.Length != 0) return scenes;

            Debug.LogError("[Story] No enabled scenes in build settings.");
            EditorApplication.Exit(1);
            return null;
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
