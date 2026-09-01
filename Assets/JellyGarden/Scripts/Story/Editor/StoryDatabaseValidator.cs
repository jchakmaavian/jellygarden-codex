// Editor-side validation of the story content. Run from the menu, or headless
// via -executeMethod JellyGarden.Scripts.Story.Editor.StoryDatabaseValidator.RunHeadless

using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace JellyGarden.Scripts.Story.Editor
{
    public static class StoryDatabaseValidator
    {
        public const int ExpectedAreas = 150;
        public const int ExpectedTasks = 807;
        public const int ExpectedStars = 1209;

        [MenuItem("JellyGarden/Story/Validate Story Data")]
        public static void Validate()
        {
            List<string> failures = Check();
            if (failures.Count == 0)
            {
                Debug.Log(string.Format(
                    "[Story] Validation passed: {0} areas, {1} tasks, {2} stars.",
                    StoryDatabase.AreaCount, StoryDatabase.TotalTasks, StoryDatabase.TotalStars));
                return;
            }

            var sb = new StringBuilder("[Story] Validation FAILED:\n");
            for (int i = 0; i < failures.Count; i++) sb.AppendLine("  - " + failures[i]);
            Debug.LogError(sb.ToString());
        }

        /// <summary>Batchmode entry point. Exits non-zero when the content is broken.</summary>
        public static void RunHeadless()
        {
            List<string> failures = Check();
            for (int i = 0; i < failures.Count; i++)
                Debug.LogError("[Story] " + failures[i]);

            if (failures.Count > 0)
            {
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log(string.Format(
                "[Story] Validation passed: {0} areas, {1} tasks, {2} stars.",
                StoryDatabase.AreaCount, StoryDatabase.TotalTasks, StoryDatabase.TotalStars));
            EditorApplication.Exit(0);
        }

        private static List<string> Check()
        {
            StoryDatabase.Unload();
            var failures = new List<string>();
            var areas = StoryDatabase.AllAreas;

            if (areas.Count != ExpectedAreas)
                failures.Add(string.Format("expected {0} areas, got {1}", ExpectedAreas, areas.Count));

            int tasks = 0, stars = 0;
            for (int i = 0; i < areas.Count; i++)
            {
                StoryArea area = areas[i];

                if (area.Id != i + 1)
                    failures.Add(string.Format("area ids are not contiguous at index {0} (id {1})", i, area.Id));
                if (area.Tasks.Count < 1)
                    failures.Add(string.Format("area {0} has no repair tasks", area.Id));
                if (string.IsNullOrEmpty(area.Clue))
                    failures.Add(string.Format("area {0} has no clue", area.Id));
                if (string.IsNullOrEmpty(area.Name))
                    failures.Add(string.Format("area {0} has no name", area.Id));
                if (string.IsNullOrEmpty(area.Goal))
                    failures.Add(string.Format("area {0} has no goal", area.Id));

                for (int t = 0; t < area.Tasks.Count; t++)
                {
                    if (area.Tasks[t].Stars < 1)
                        failures.Add(string.Format("area {0} task {1} costs no stars", area.Id, t + 1));
                    if (string.IsNullOrEmpty(area.Tasks[t].Label))
                        failures.Add(string.Format("area {0} task {1} has no label", area.Id, t + 1));
                }

                tasks += area.Tasks.Count;
                stars += area.StarCost;
            }

            if (tasks != ExpectedTasks)
                failures.Add(string.Format("expected {0} tasks, got {1}", ExpectedTasks, tasks));
            if (stars != ExpectedStars)
                failures.Add(string.Format("expected {0} stars, got {1}", ExpectedStars, stars));
            if (StoryDatabase.Arcs.Count != StoryDatabase.ArcCount)
                failures.Add(string.Format("expected {0} arcs in index.json, got {1}",
                    StoryDatabase.ArcCount, StoryDatabase.Arcs.Count));

            return failures;
        }
    }
}
