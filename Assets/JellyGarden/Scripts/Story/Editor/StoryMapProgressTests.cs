// Proves the map's lock state is driven by story completion rather than by the
// kit's per-level star bookkeeping.

using System.Collections.Generic;
using System.IO;
using JellyGarden.Scripts.MapScripts;
using UnityEditor;
using UnityEngine;

namespace JellyGarden.Scripts.Story.Editor
{
    public static class StoryMapProgressTests
    {
        [MenuItem("JellyGarden/Story/Run Map Progress Tests")]
        public static void Run()
        {
            List<string> failures = Check();
            if (failures.Count == 0)
            {
                Debug.Log("[Story] Map progress tests passed.");
                return;
            }
            for (int i = 0; i < failures.Count; i++) Debug.LogError("[Story] " + failures[i]);
        }

        public static void RunHeadless()
        {
            List<string> failures = Check();
            for (int i = 0; i < failures.Count; i++) Debug.LogError("[Story] " + failures[i]);
            if (failures.Count > 0)
            {
                EditorApplication.Exit(1);
                return;
            }
            Debug.Log("[Story] Map progress tests passed.");
            EditorApplication.Exit(0);
        }

        private static List<string> Check()
        {
            var failures = new List<string>();

            string path = StoryProgress.SavePath;
            string backup = path + ".testbackup";
            bool hadSave = File.Exists(path);
            if (hadSave) File.Copy(path, backup, true);

            try
            {
                StoryMapProgressManager.Install();
                StoryProgress.ResetAll();

                Expect(failures, !LevelsMap.IsLevelLocked(1),
                    "area 1 is unlocked on a fresh save");
                Expect(failures, LevelsMap.IsLevelLocked(2),
                    "area 2 is locked on a fresh save");

                // Winning levels alone must not unlock anything — repairs do.
                StoryArea area1 = StoryDatabase.GetArea(1);
                foreach (int levelNumber in StoryLevelPlan.LevelsFor(1))
                {
                    StoryProgress.MarkLevelBeaten(levelNumber);
                    StoryProgress.AddStar(1);
                }
                Expect(failures, LevelsMap.IsLevelLocked(2),
                    "beating every level in area 1 does not by itself unlock area 2");

                foreach (StoryTask task in area1.Tasks)
                    StoryProgress.CompleteTask(1, task.Index);

                Expect(failures, !LevelsMap.IsLevelLocked(2),
                    "finishing area 1's repairs unlocks area 2");
                Expect(failures, LevelsMap.IsLevelLocked(3),
                    "area 3 stays locked behind area 2");

                // The plan and the map have to agree about how many nodes exist.
                Expect(failures, StoryLevelPlan.LevelCount > 0, "the level plan loaded");
                Expect(failures, StoryLevelPlan.LevelsFor(1).Count == area1.StarCost,
                    "area 1 has one planned level per star it costs");
                Expect(failures, StoryLevelPlan.AreaOf(1) == 1,
                    "level 1 belongs to area 1");

                int lastArea = StoryDatabase.AreaCount;
                Expect(failures, StoryLevelPlan.LevelsFor(lastArea).Count > 0,
                    "the final area has planned levels");

                StoryProgress.ResetAll();
            }
            finally
            {
                if (hadSave)
                {
                    File.Copy(backup, path, true);
                    File.Delete(backup);
                }
                else if (File.Exists(path))
                {
                    File.Delete(path);
                }
                StoryProgress.ReloadFromDisk();
            }

            return failures;
        }

        private static void Expect(List<string> failures, bool condition, string what)
        {
            if (!condition) failures.Add("FAILED: " + what);
        }
    }
}
