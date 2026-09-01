// Round-trip checks for the story save file. Editor-only; runs headless via
// -executeMethod JellyGarden.Scripts.Story.Editor.StoryProgressTests.RunHeadless

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace JellyGarden.Scripts.Story.Editor
{
    public static class StoryProgressTests
    {
        [MenuItem("JellyGarden/Story/Run Save System Tests")]
        public static void Run()
        {
            List<string> failures = Check();
            if (failures.Count == 0)
            {
                Debug.Log("[Story] Save system tests passed.");
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
            Debug.Log("[Story] Save system tests passed.");
            EditorApplication.Exit(0);
        }

        private static List<string> Check()
        {
            var failures = new List<string>();

            // Work on a copy so a developer's real progress is never destroyed.
            string path = StoryProgress.SavePath;
            string backup = path + ".testbackup";
            bool hadSave = File.Exists(path);
            if (hadSave) File.Copy(path, backup, true);

            try
            {
                StoryProgress.ResetAll();

                StoryArea area1 = StoryDatabase.GetArea(1);
                if (area1 == null)
                {
                    failures.Add("area 1 missing from the database");
                    return failures;
                }

                Expect(failures, StoryProgress.IsAreaUnlocked(1), "area 1 unlocked on a fresh save");
                Expect(failures, !StoryProgress.IsAreaUnlocked(2), "area 2 locked on a fresh save");
                Expect(failures, StoryProgress.StarsIn(1) == 0, "fresh save has no stars");

                int firstTask = area1.Tasks[0].Index;
                Expect(failures, !StoryProgress.CanCompleteTask(1, firstTask),
                    "a task cannot be paid for with zero stars");
                Expect(failures, !StoryProgress.CompleteTask(1, firstTask),
                    "CompleteTask refuses when unaffordable");

                // Earn exactly enough to clear the whole area.
                StoryProgress.AddStar(1, area1.StarCost);
                Expect(failures, StoryProgress.StarsIn(1) == area1.StarCost, "stars accumulate");

                for (int i = 0; i < area1.Tasks.Count; i++)
                    Expect(failures, StoryProgress.CompleteTask(1, area1.Tasks[i].Index),
                        "task " + area1.Tasks[i].Index + " completes");

                Expect(failures, StoryProgress.StarsIn(1) == 0, "stars are spent, not free");
                Expect(failures, StoryProgress.IsAreaComplete(1), "area completes on its last task");
                Expect(failures, StoryProgress.IsAreaUnlocked(2), "finishing area 1 unlocks area 2");

                StoryProgress.MarkClueSeen(1);
                StoryProgress.MarkLevelBeaten(3);
                StoryProgress.MarkOpeningSeen();
                StoryProgress.CurrentArea = 2;
                StoryProgress.Flush();

                Expect(failures, File.Exists(path), "save file was written");
                Expect(failures, !File.Exists(path + ".tmp"), "temp file was renamed away, not left behind");

                // Reload from disk and confirm everything survived.
                ReloadFromDisk();
                Expect(failures, StoryProgress.IsAreaComplete(1), "area completion survives a reload");
                Expect(failures, StoryProgress.IsClueSeen(1), "seen clues survive a reload");
                Expect(failures, StoryProgress.IsLevelBeaten(3), "beaten levels survive a reload");
                Expect(failures, StoryProgress.OpeningSeen, "the opening flag survives a reload");
                Expect(failures, StoryProgress.CurrentArea == 2, "current area survives a reload");
                Expect(failures, StoryProgress.TasksCompletedIn(1) == area1.Tasks.Count,
                    "completed tasks survive a reload");

                // A corrupt file must not throw.
                File.WriteAllText(path, "{ this is not json");
                ReloadFromDisk();
                Expect(failures, StoryProgress.CurrentArea >= 1, "a corrupt save falls back to a fresh one");

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
                ReloadFromDisk();
            }

            return failures;
        }

        /// <summary>Drops the in-memory state so the next access re-reads the file.</summary>
        private static void ReloadFromDisk()
        {
            StoryProgress.ReloadFromDisk();
        }

        private static void Expect(List<string> failures, bool condition, string what)
        {
            if (!condition) failures.Add("FAILED: " + what);
        }
    }
}
