// Does the story actually have everything a finished area needs?
//
// tools/validate_story.py checks the markdown and the JSON agree. StorySmokeTest
// checks all 150 areas can be opened without throwing. Neither answers the
// question this asks: for the areas that are shipping, does every single thing
// the player will look at resolve — the name, the goal line, every task with a
// price and an icon, the room's picture, the clue's picture — and does anything
// warn about missing art on the way past.
//
// It logs one line per area, so the answer is in the log rather than in an
// assertion someone has to go and read. Nothing here hardcodes an area id; the
// range is a parameter and ShellFirstArea..ShellLastArea is only today's
// default.

using System.Collections.Generic;
using UnityEngine;

namespace JellyGarden.Scripts.Story
{
    /// <summary>What the audit found in one area. Ok is Problems being empty.</summary>
    public sealed class StoryAreaReport
    {
        public int AreaId;
        public string Name = string.Empty;
        public int TaskCount;
        public int StarCost;
        public int LevelCount;

        /// <summary>Where the room's picture came from — a state set, a restored plate, or the arc.</summary>
        public string ArtSource = string.Empty;

        /// <summary>One entry per task: the authored art key, or the glyph its label classified to.</summary>
        public readonly List<string> IconSources = new List<string>();

        public readonly List<string> Problems = new List<string>();

        public bool Ok
        {
            get { return Problems.Count == 0; }
        }

        /// <summary>The single line this area contributes to the log.</summary>
        public string Line
        {
            get
            {
                return string.Format(
                    "[Story] area {0} '{1}': {2} tasks, {3} stars, {4} levels, art {5}, icons {6}. {7}",
                    AreaId, Name, TaskCount, StarCost, LevelCount, ArtSource,
                    string.Join("/", IconSources.ToArray()),
                    Ok ? "OK" : Problems.Count + " problem(s)");
            }
        }
    }

    public static class StoryContentAudit
    {
        /// <summary>The band of areas the Royal Match shell ships with.</summary>
        public const int ShellFirstArea = 1;

        public const int ShellLastArea = 5;

        /// <summary>Audits areas 1-5 and logs a line each. Returns every problem found.</summary>
        public static List<string> RunForTheShell(bool openScreens = true)
        {
            return Run(ShellFirstArea, ShellLastArea, openScreens);
        }

        /// <summary>
        /// Audits a band of areas, logs one line per area plus a summary, and
        /// returns the problems. openScreens builds each area's screen as well,
        /// which is what catches a UI sprite that never made it into Resources —
        /// pass false to leave whatever is on screen alone.
        /// </summary>
        public static List<string> Run(int firstArea, int lastArea, bool openScreens = true)
        {
            var problems = new List<string>();
            List<StoryAreaReport> reports = Audit(firstArea, lastArea, openScreens);

            int tasks = 0, stars = 0;
            for (int i = 0; i < reports.Count; i++)
            {
                StoryAreaReport report = reports[i];
                tasks += report.TaskCount;
                stars += report.StarCost;

                Debug.Log(report.Line);
                for (int p = 0; p < report.Problems.Count; p++)
                    problems.Add(report.Problems[p]);
            }

            // A warning is a problem the log would otherwise swallow: art falls
            // back rather than failing, so the screen still draws and nobody
            // notices the arc backdrop standing in for a room.
            foreach (string key in StoryArt.WarnedKeys)
                problems.Add("missing art '" + key + "' warned during the audit");
            foreach (string key in StoryUi.WarnedSprites)
                problems.Add("missing UI sprite '" + key + "' warned during the audit");

            Debug.Log(string.Format(
                "[Story] Content audit, areas {0}-{1}: {2} areas, {3} tasks, {4} stars, {5} problem(s).",
                firstArea, lastArea, reports.Count, tasks, stars, problems.Count));

            return problems;
        }

        /// <summary>
        /// The audit itself. Clears both warning sets first so what it reports is
        /// what this run provoked, not what some earlier screen left behind.
        /// </summary>
        public static List<StoryAreaReport> Audit(int firstArea, int lastArea,
            bool openScreens = true)
        {
            StoryDatabase.Load();
            StoryLevelPlan.Load();
            StoryArt.ClearCache();
            StoryUi.ClearSpriteWarnings();

            var reports = new List<StoryAreaReport>();
            for (int areaId = firstArea; areaId <= lastArea; areaId++)
            {
                if (!StoryCampaign.IsReady(areaId)) continue;
                reports.Add(AuditArea(areaId, openScreens));
            }

            if (openScreens) AreaScreen.Close();
            return reports;
        }

        private static StoryAreaReport AuditArea(int areaId, bool openScreens)
        {
            var report = new StoryAreaReport { AreaId = areaId };
            string where = "area " + areaId;

            StoryArea area = StoryDatabase.GetArea(areaId);
            if (area == null)
            {
                report.Problems.Add(where + " is not in the database");
                report.ArtSource = "none";
                return report;
            }

            report.Name = area.Name;
            report.TaskCount = area.Tasks.Count;
            report.StarCost = area.StarCost;
            report.LevelCount = StoryLevelPlan.LevelsFor(areaId).Count;

            if (string.IsNullOrEmpty(area.Name)) report.Problems.Add(where + " has no name");
            if (string.IsNullOrEmpty(area.Goal)) report.Problems.Add(where + " has no goal line");
            if (string.IsNullOrEmpty(area.Clue)) report.Problems.Add(where + " has no clue");
            if (StoryDatabase.ArcOf(areaId) == null) report.Problems.Add(where + " belongs to no arc");
            if (area.Tasks.Count == 0) report.Problems.Add(where + " has no tasks");

            AuditTasks(area, report, where);
            AuditArt(area, report, where);

            // The area's own levels have to fund the area's own repairs; stars do
            // not travel between areas.
            if (report.LevelCount != report.StarCost)
                report.Problems.Add(string.Format(
                    "{0} costs {1} stars but has {2} levels", where, report.StarCost,
                    report.LevelCount));

            if (openScreens) AuditScreen(area, report, where);

            return report;
        }

        private static void AuditTasks(StoryArea area, StoryAreaReport report, string where)
        {
            for (int i = 0; i < area.Tasks.Count; i++)
            {
                StoryTask task = area.Tasks[i];
                string which = where + " task " + (i + 1);

                if (string.IsNullOrEmpty(task.Label)) report.Problems.Add(which + " has no label");
                if (task.Stars < 1)
                    report.Problems.Add(which + " costs " + task.Stars + " stars");

                // The repair order is the list order, and StoryProgress pays for
                // tasks by index, so a gap would strand the rest of the area.
                if (task.Index != i + 1)
                    report.Problems.Add(string.Format(
                        "{0} is indexed {1}, breaking the repair order", which, task.Index));

                report.IconSources.Add(IconSource(area.Id, task, report, which));
            }
        }

        /// <summary>
        /// What the task row will actually draw. Authored art wins; otherwise the
        /// glyph comes from the kind of work the label describes, and a task that
        /// falls through to the generic cog is a content gap in a shipping area
        /// even though it does draw something.
        /// </summary>
        private static string IconSource(int areaId, StoryTask task, StoryAreaReport report,
            string which)
        {
            if (StoryArt.TaskIcon(areaId, task.Index) != null)
                return string.Format("task_{0:000}_{1}", areaId, task.Index);

            RepairWorkType work = RepairWork.Classify(task.Label);
            if (work == RepairWorkType.General)
                report.Problems.Add(string.Format(
                    "{0} ('{1}') has no icon but the generic one", which, task.Label));

            return work.ToString();
        }

        private static void AuditArt(StoryArea area, StoryAreaReport report, string where)
        {
            // An area is drawn either as a set of states or as one restored plate
            // the repair wipes in over the ruin. Both are fine; neither is not.
            bool hasStates = StoryArt.AreaState(area.Id, 0) != null;
            bool hasRestored = StoryArt.Load(string.Format("area_{0:000}_restored", area.Id)) != null;

            if (hasStates)
            {
                int states = 0;
                for (int state = 0; state <= area.Tasks.Count; state++)
                    if (StoryArt.AreaState(area.Id, state) != null) states++;
                report.ArtSource = string.Format("state x{0}", states);
                if (StoryCampaign.IsTreasureHunt && states != area.Tasks.Count + 1)
                    report.Problems.Add(where + " is missing a repair state");
            }
            else if (hasRestored)
            {
                report.ArtSource = "restored";
            }
            else
            {
                report.ArtSource = "arc backdrop";
                report.Problems.Add(where + " has no artwork of its own");
            }

            if (StoryArt.AreaRestored(area.Id) == null)
                report.Problems.Add(where + " has no picture of itself restored");
            if (StoryArt.ClueImage(area.Id) == null)
                report.Problems.Add(where + " has no picture for its clue card");
            if (StoryArt.ArcBackdrop(area.Id) == null)
                report.Problems.Add(where + " has no arc backdrop to fall back to");
        }

        private static void AuditScreen(StoryArea area, StoryAreaReport report, string where)
        {
            AreaScreen.Open(area.Id);
            if (!AreaScreen.IsOpen || AreaScreen.Instance.AreaId != area.Id)
            {
                report.Problems.Add(where + " did not open");
                return;
            }

            if (AreaScreen.Instance.RowCount != area.Tasks.Count)
                report.Problems.Add(string.Format(
                    "{0} lists {1} of its {2} tasks", where, AreaScreen.Instance.RowCount,
                    area.Tasks.Count));
        }
    }
}
