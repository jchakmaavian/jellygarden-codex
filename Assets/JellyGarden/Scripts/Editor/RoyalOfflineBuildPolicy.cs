using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace RoyalThrone.Editor
{
    /// <summary>Keep inherited integrations disabled in every Royal Throne build entry point.</summary>
    public sealed class RoyalOfflineBuildPolicy : IPreprocessBuildWithReport
    {
        public static readonly string[] ServiceDefines =
        {
            "UNITY_ADS", "UNITY_INAPPS", "GOOGLE_MOBILE_ADS", "CHARTBOOST_ADS",
            "FACEBOOK", "PLAYFAB", "GAMESPARKS", "GS_FORCE_NATIVE_PLATFORM",
        };

        public int callbackOrder => -1000;

        public static string FilterDefines(string defines)
        {
            return string.Join(";", defines.Split(';').Select(d => d.Trim())
                .Where(d => d.Length > 0 && !ServiceDefines.Contains(d)).Distinct());
        }

        public static void Configure(NamedBuildTarget target)
        {
            string original = PlayerSettings.GetScriptingDefineSymbols(target);
            string filtered = FilterDefines(original);
            if (original != filtered) PlayerSettings.SetScriptingDefineSymbols(target, filtered);
        }

        public static void ConfigureCommonTargets()
        {
            foreach (var target in new[] { NamedBuildTarget.Android, NamedBuildTarget.Standalone,
                NamedBuildTarget.iOS, NamedBuildTarget.tvOS, NamedBuildTarget.WebGL,
                NamedBuildTarget.FromBuildTargetGroup(BuildTargetGroup.WSA) }) Configure(target);
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            var target = NamedBuildTarget.FromBuildTargetGroup(report.summary.platformGroup);
            var enabled = PlayerSettings.GetScriptingDefineSymbols(target).Split(';')
                .Select(d => d.Trim()).Intersect(ServiceDefines).ToArray();
            // Mutating defines after compilation starts would leave stale compiled integrations.
            if (enabled.Length > 0)
                throw new BuildFailedException("Royal Throne is offline. Remove service defines before building: " + string.Join(", ", enabled));
        }
    }
}
