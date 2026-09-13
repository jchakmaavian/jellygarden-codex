// One command that proves the runtime can open every area without throwing.
//
// The content validator (tools/validate_story.py) checks the files. This checks
// what the game actually does with them: for all 150 areas it resolves the art
// layers, the level plan, the clue or the scene, and the repair economy, and
// fails on the first area that cannot answer.
//
// Unity -batchmode -quit -nographics -projectPath . \
//   -executeMethod JellyGarden.Scripts.Story.Editor.StorySmokeTest.RunAll -logFile -

using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace JellyGarden.Scripts.Story.Editor
{
    public static class StorySmokeTest
    {
        /// <summary>
        /// Areas 1-5 on their own, with a line per area in the log. The full
        /// smoke test walks all 150 and says nothing about any of them; this is
        /// the one to run while the shell's five areas are the deliverable.
        /// </summary>
        [MenuItem("JellyGarden/Story/Audit Shell Areas")]
        public static void AuditShellAreas()
        {
            List<string> problems = StoryContentAudit.RunForTheShell();
            for (int i = 0; i < problems.Count; i++) Debug.LogError("[Story] " + problems[i]);
            if (problems.Count == 0) Debug.Log("[Story] Shell content audit passed.");
        }

        [MenuItem("JellyGarden/Story/Run Full Smoke Test")]
        public static void Run()
        {
            List<string> failures = Check();
            if (failures.Count == 0)
            {
                Debug.Log("[Story] Smoke test passed.");
                return;
            }
            for (int i = 0; i < failures.Count && i < 40; i++)
                Debug.LogError("[Story] " + failures[i]);
        }

        /// <summary>Batchmode entry point. Exits non-zero on the first real problem.</summary>
        public static void RunAll()
        {
            List<string> failures = Check();

            for (int i = 0; i < failures.Count && i < 40; i++)
                Debug.LogError("[Story] " + failures[i]);
            if (failures.Count > 40)
                Debug.LogError(string.Format("[Story] ... and {0} more", failures.Count - 40));

            if (failures.Count > 0)
            {
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log("[Story] Smoke test passed.");
            EditorApplication.Exit(0);
        }

        private static List<string> Check()
        {
            if (StoryCampaign.IsTreasureHunt) return TreasureHuntTests.Check();
            var failures = new List<string>();

            // Never run against a developer's real progress.
            string savePath = StoryProgress.SavePath;
            string backup = savePath + ".smokebackup";
            bool hadSave = File.Exists(savePath);
            if (hadSave) File.Copy(savePath, backup, true);

            try
            {
                StoryBoot.BootForTests();
                StoryProgress.ResetAll();

                CheckTotals(failures);
                CheckShellContent(failures);
                CheckEveryArea(failures);
                CheckWholeGameIsReachable(failures);
            }
            finally
            {
                if (hadSave)
                {
                    File.Copy(backup, savePath, true);
                    File.Delete(backup);
                }
                else if (File.Exists(savePath))
                {
                    File.Delete(savePath);
                }
                StoryProgress.ReloadFromDisk();
            }

            return failures;
        }

        private static void CheckTotals(List<string> failures)
        {
            if (StoryDatabase.AreaCount != 150)
                failures.Add("expected 150 areas, loaded " + StoryDatabase.AreaCount);
            if (StoryDatabase.TotalTasks != 807)
                failures.Add("expected 807 tasks, loaded " + StoryDatabase.TotalTasks);
            if (StoryDatabase.TotalStars != 1209)
                failures.Add("expected 1209 stars, loaded " + StoryDatabase.TotalStars);
            if (StoryLevelPlan.LevelCount != StoryDatabase.TotalStars)
                failures.Add(string.Format(
                    "the story costs {0} stars but the plan has {1} levels",
                    StoryDatabase.TotalStars, StoryLevelPlan.LevelCount));
            if (StoryTheme.Count != 6)
                failures.Add("expected 6 arc themes, loaded " + StoryTheme.Count);
        }

        /// <summary>
        /// The areas that ship get a stricter pass than the other 145 and put a
        /// line each in the log, so a batchmode run says what areas 1-5 contain
        /// rather than only that nothing threw.
        /// </summary>
        private static void CheckShellContent(List<string> failures)
        {
            failures.AddRange(StoryContentAudit.RunForTheShell());
            AreaScreen.Close();
        }

        private static void CheckEveryArea(List<string> failures)
        {
            for (int areaId = 1; areaId <= StoryDatabase.AreaCount; areaId++)
            {
                StoryArea area = StoryDatabase.GetArea(areaId);
                if (area == null)
                {
                    failures.Add("area " + areaId + " is missing");
                    continue;
                }

                string where = "area " + areaId;

                if (StoryDatabase.ArcOf(areaId) == null)
                    failures.Add(where + " belongs to no arc");
                if (StoryTheme.ForArea(areaId) == null)
                    failures.Add(where + " has no theme");

                // Something must be drawable, or the screen is a flat rectangle.
                if (StoryArt.AreaState(areaId, 0) == null && StoryArt.ArcBackdrop(areaId) == null)
                    failures.Add(where + " has no artwork and no arc backdrop");
                if (StoryArt.ClueImage(areaId) == null)
                    failures.Add(where + " has no picture for its clue card");

                // Every star the area charges must have a board behind it.
                IList<int> levels = StoryLevelPlan.LevelsFor(areaId);
                if (levels.Count != area.StarCost)
                    failures.Add(string.Format(
                        "{0} costs {1} stars but has {2} levels", where, area.StarCost, levels.Count));

                foreach (int levelNumber in levels)
                {
                    if (Resources.Load<TextAsset>("Levels/" + levelNumber) == null)
                        failures.Add(string.Format("{0}: level {1} has no board", where, levelNumber));
                    if (Resources.Load<ScriptableObject>("Targets/Level" + levelNumber) == null)
                        failures.Add(string.Format("{0}: level {1} has no target asset",
                            where, levelNumber));
                }

                if (StoryLevelPlan.NextLevelFor(areaId) <= 0)
                    failures.Add(where + " has no level to play");

                // A beat needs a scene; everything else needs a clue.
                if (area.IsArcBeat && !CutsceneRunner.Has(areaId))
                    failures.Add(where + " is an arc beat with no scene");
                if (string.IsNullOrEmpty(area.Clue))
                    failures.Add(where + " has no clue");

                // And the screen has to open.
                AreaScreen.Open(areaId);
                if (!AreaScreen.IsOpen || AreaScreen.Instance.AreaId != areaId)
                    failures.Add(where + " failed to open");
            }

            AreaScreen.Close();
        }

        /// <summary>
        /// Walks the whole game the way a player would: earn the stars, spend
        /// them, watch the next area unlock, 150 times. Anything that can dead-end
        /// the player fails here rather than hours into a playthrough.
        /// </summary>
        private static void CheckWholeGameIsReachable(List<string> failures)
        {
            StoryProgress.ResetAll();

            var levelsPlayed = new HashSet<int>();

            for (int areaId = 1; areaId <= StoryDatabase.AreaCount; areaId++)
            {
                if (!StoryProgress.IsAreaUnlocked(areaId))
                {
                    failures.Add(string.Format(
                        "area {0} never unlocked — the game dead-ends at area {1}",
                        areaId, areaId - 1));
                    return;
                }

                StoryArea area = StoryDatabase.GetArea(areaId);

                foreach (int levelNumber in StoryLevelPlan.LevelsFor(areaId))
                {
                    if (!levelsPlayed.Add(levelNumber))
                        failures.Add(string.Format(
                            "level {0} is claimed by two areas", levelNumber));
                    StoryProgress.MarkLevelBeaten(levelNumber);
                    StoryProgress.AddStar(areaId);
                }

                foreach (StoryTask task in area.Tasks)
                {
                    if (!StoryProgress.CompleteTask(areaId, task.Index))
                    {
                        failures.Add(string.Format(
                            "area {0} task {1} could not be paid for with the {2} stars " +
                            "its own levels award", areaId, task.Index, area.StarCost));
                        return;
                    }
                }

                if (StoryProgress.StarsIn(areaId) != 0)
                    failures.Add(string.Format(
                        "area {0} leaves {1} stars unspent; the economy does not balance",
                        areaId, StoryProgress.StarsIn(areaId)));

                if (!StoryProgress.IsAreaComplete(areaId))
                {
                    failures.Add("area " + areaId + " did not complete");
                    return;
                }
            }

            if (levelsPlayed.Count != StoryLevelPlan.LevelCount)
                failures.Add(string.Format(
                    "a full playthrough uses {0} of {1} levels",
                    levelsPlayed.Count, StoryLevelPlan.LevelCount));

            var sb = new StringBuilder();
            sb.AppendFormat("[Story] Full playthrough simulated: {0} areas restored, ",
                StoryDatabase.AreaCount);
            sb.AppendFormat("{0} levels played, {1} stars earned and spent.",
                levelsPlayed.Count, StoryDatabase.TotalStars);
            Debug.Log(sb.ToString());

            StoryProgress.ResetAll();
        }
    }
}
