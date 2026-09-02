// Proves the shipping band of areas by playing it, not by asserting about it.
//
// The editor suites build the screens outside Play mode, where the repair
// cutscene calls back synchronously and every timed beat is a clock the test
// steps itself. That is what makes them fast, and it is also what they cannot
// answer: in Play mode a repair is four seconds of coroutine, the payoff of a
// finished area is a clue card or an arc scene, and the area screen closes out
// from under whoever bought the last repair. Nothing before this had walked
// areas 1-5 with the real clock running.
//
// So this does. Once per play session, with no input at all — this project has
// the Input System package as its only active input handler, so a legacy
// UnityEngine.Input read throws rather than reads, and synthetic clicks on the
// Game view do not land. It presses the same buttons the player's finger would,
// in code, and prints one PASS/FAIL line per area.
//
// It walks over the player's save, so it takes a copy first and puts it back
// afterwards — including the clue and cutscene flags the walk sets on its way
// through.
//
// Editor and development builds only; it never installs itself in a release
// player. Re-run it from JellyGarden > Story > Preview > Run Shell Self-Test.

using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace JellyGarden.Scripts.Story
{
    /// <summary>What the walk found in one area. Ok is Problems being empty.</summary>
    public sealed class StoryShellAreaResult
    {
        public int AreaId;
        public string Name = string.Empty;

        /// <summary>Task rows the area screen actually drew.</summary>
        public int Rows;

        /// <summary>Repairs bought by pressing a Do It button.</summary>
        public int Repairs;

        /// <summary>Stars the screen charged for them.</summary>
        public int StarsSpent;

        public bool Restored;
        public bool NextUnlocked;

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
                    "[Story] shell test area {0} '{1}': {2} rows, {3} repairs, {4} stars spent, " +
                    "{5}, area {6} {7}. {8}",
                    AreaId, Name, Rows, Repairs, StarsSpent,
                    Restored ? "restored" : "NOT restored",
                    AreaId + 1, NextUnlocked ? "unlocked" : "still locked",
                    Ok ? "PASS" : "FAIL");
            }
        }
    }

    [DisallowMultipleComponent]
    public sealed class StoryShellTest : MonoBehaviour
    {
        /// <summary>The band the shell ships with — the same one the audit reads.</summary>
        public const int FirstArea = StoryContentAudit.ShellFirstArea;

        public const int LastArea = StoryContentAudit.ShellLastArea;

        /// <summary>How long to give one repair's cutscene and payoff before calling it stuck.</summary>
        private const float BeatTimeout = 45f;

        /// <summary>How long to wait for the board self-test to have had its run.</summary>
        private const float BoardTestTimeout = 180f;

        private const float Poll = 0.2f;

        private static StoryShellTest _instance;

        private bool _running;
        private bool _ranOnce;

        /// <summary>Whether the walk is in progress right now.</summary>
        public static bool IsRunning
        {
            get { return _instance != null && _instance._running; }
        }

        public static void Install()
        {
            if (!Application.isEditor && !Debug.isDebugBuild) return;
            if (_instance != null) return;

            // Installed from whichever scene is up, like the board self-test: this
            // host survives scene loads, and the walk waits for the game scene
            // itself rather than for the scene it was installed in.
            var host = new GameObject("StoryShellTest");
            Object.DontDestroyOnLoad(host);
            _instance = host.AddComponent<StoryShellTest>();
            Debug.Log(string.Format(
                "[Story] Shell self-test armed for areas {0}-{1}; it runs itself once.",
                FirstArea, LastArea));
        }

        /// <summary>
        /// Runs the walk now, whatever it has done already. The editor's Preview
        /// menu uses this; the session's own run goes through AutoRun.
        /// </summary>
        public static void RunNow()
        {
            if (_instance == null)
            {
                Debug.LogWarning("[Story] Shell self-test is not installed.");
                return;
            }
            if (_instance._running)
            {
                Debug.Log("[Story] Shell self-test is already running.");
                return;
            }
            _instance.StartCoroutine(_instance.Walk());
        }

        private void Start()
        {
            StartCoroutine(AutoRun());
        }

        private IEnumerator AutoRun()
        {
            if (_ranOnce || _running) yield break;
            _ranOnce = true;
            yield return Walk();
        }

        #region The walk

        private IEnumerator Walk()
        {
            _running = true;

            // The board self-test loads the game scene and plays level 1 on every
            // session. Opening an area screen over that would be measuring the
            // collision rather than the shell.
            yield return WaitForTheBoardTest();
            yield return WaitForCalm(BeatTimeout);

            string savePath = StoryProgress.SavePath;
            string backup = savePath + ".shelltestbackup";
            bool hadSave = File.Exists(savePath);
            if (hadSave) File.Copy(savePath, backup, true);

            var results = new List<StoryShellAreaResult>();

            try
            {
                // A part-finished save cannot prove "buys every repair", so the
                // walk starts from nothing and the player's own save is put back
                // at the end.
                StoryProgress.ResetAll();

                Debug.Log(string.Format(
                    "[Story] ---- shell self-test, areas {0}-{1} ----", FirstArea, LastArea));

                for (int areaId = FirstArea; areaId <= LastArea; areaId++)
                {
                    var result = new StoryShellAreaResult { AreaId = areaId };
                    results.Add(result);
                    yield return WalkArea(result);
                    Debug.Log(result.Line);
                }

                int passed = 0, repairs = 0, stars = 0;
                for (int i = 0; i < results.Count; i++)
                {
                    if (results[i].Ok) passed++;
                    repairs += results[i].Repairs;
                    stars += results[i].StarsSpent;
                }

                Debug.Log(string.Format(
                    "[Story] Shell self-test, areas {0}-{1}: {2} area(s), {3} repairs, " +
                    "{4} stars. {5} PASS, {6} FAIL.",
                    FirstArea, LastArea, results.Count, repairs, stars,
                    passed, results.Count - passed));

                for (int i = 0; i < results.Count; i++)
                    for (int p = 0; p < results[i].Problems.Count; p++)
                        Debug.LogWarning("[Story] shell test: " + results[i].Problems[p]);
            }
            finally
            {
                AreaScreen.Close();

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

                // The walk left the shell standing in area 5. Put the player back
                // in their own room — the one the restored save names, not the one
                // StoryHomeScreen.CurrentRoom would pick. Home writes whatever
                // area it opens on back to CurrentArea, so coming home to anything
                // else would move the bookmark this block just put back.
                if (StoryShell.InGameScene && !StoryNavBar.LevelIsPlaying)
                    StoryShell.GoHome(StoryProgress.CurrentArea);

                _running = false;
            }
        }

        private IEnumerator WalkArea(StoryShellAreaResult result)
        {
            int areaId = result.AreaId;
            StoryArea area = StoryDatabase.GetArea(areaId);
            if (area == null)
            {
                result.Problems.Add("area " + areaId + " is not in the database");
                yield break;
            }

            result.Name = area.Name;

            AreaScreen.Open(areaId);
            yield return null;

            if (!AreaScreen.IsOpen || AreaScreen.Instance == null ||
                AreaScreen.Instance.AreaId != areaId)
            {
                result.Problems.Add("area " + areaId + " did not open");
                yield break;
            }

            result.Rows = AreaScreen.Instance.RowCount;
            if (result.Rows != area.Tasks.Count)
                result.Problems.Add(string.Format(
                    "area {0} lists {1} of its {2} tasks", areaId, result.Rows, area.Tasks.Count));

            // Exactly what the area costs, so the wallet ending anywhere but zero
            // means the screen charged a price the story did not write.
            int needed = area.StarCost - StoryProgress.StarsIn(areaId);
            if (needed > 0) StoryProgress.AddStar(areaId, needed);
            AreaScreen.Instance.Refresh();
            yield return null;

            for (int bought = 0; bought < area.Tasks.Count; bought++)
            {
                yield return BuyNextRepair(result, areaId);
                if (!result.Ok) break;
            }

            // The last repair of an area closes the screen and plays the payoff:
            // a clue card, or an arc beat's scene on every fifth area.
            yield return WaitForCalm(BeatTimeout);

            result.Restored = StoryProgress.IsAreaComplete(areaId);
            result.NextUnlocked = StoryProgress.IsAreaUnlocked(areaId + 1);

            if (!result.Restored)
                result.Problems.Add(string.Format(
                    "area {0} is not restored after {1} repair(s)", areaId, result.Repairs));

            if (StoryProgress.TasksCompletedIn(areaId) != area.Tasks.Count)
                result.Problems.Add(string.Format(
                    "area {0} records {1} of its {2} repairs", areaId,
                    StoryProgress.TasksCompletedIn(areaId), area.Tasks.Count));

            if (StoryProgress.StarsIn(areaId) != 0)
                result.Problems.Add(string.Format(
                    "area {0} was granted its {1} stars and has {2} left over", areaId,
                    area.StarCost, StoryProgress.StarsIn(areaId)));

            if (!result.NextUnlocked)
                result.Problems.Add(string.Format(
                    "restoring area {0} did not unlock area {1}", areaId, areaId + 1));
        }

        /// <summary>
        /// Presses the Do It button on the area's next repair and waits the whole
        /// thing out. The BUTTON is pressed rather than StoryRepair called: the
        /// repair order guard and the price check live behind it, and a test that
        /// reaches past them is testing something the player cannot do.
        /// </summary>
        private IEnumerator BuyNextRepair(StoryShellAreaResult result, int areaId)
        {
            StoryTask next = StoryProgress.NextTask(areaId);
            if (next == null)
            {
                result.Problems.Add("area " + areaId + " ran out of tasks to buy");
                yield break;
            }

            // A repair that finished an area closes the screen; one that did not
            // leaves it up. Either way the next press needs it open on this area.
            if (!AreaScreen.IsOpen || AreaScreen.Instance == null ||
                AreaScreen.Instance.AreaId != areaId)
            {
                AreaScreen.Open(areaId);
                yield return null;
            }

            AreaScreen screen = AreaScreen.Instance;
            int row = RowFor(screen, next.Index);
            if (row < 0)
            {
                result.Problems.Add(string.Format(
                    "area {0} has no row for task {1} ('{2}')", areaId, next.Index, next.Label));
                yield break;
            }

            if (!screen.RowCanRepair(row))
            {
                result.Problems.Add(string.Format(
                    "area {0} row {1} ('{2}') is not pressable with {3} star(s) held for a " +
                    "{4} star repair", areaId, row, next.Label,
                    StoryProgress.StarsIn(areaId), next.Stars));
                yield break;
            }

            Button button = screen.RepairButton(row);
            if (button == null)
            {
                result.Problems.Add(string.Format(
                    "area {0} row {1} ('{2}') has no Do It button", areaId, row, next.Label));
                yield break;
            }

            int starsBefore = StoryProgress.StarsIn(areaId);
            button.onClick.Invoke();

            yield return WaitForCalm(BeatTimeout);

            if (!StoryProgress.IsTaskComplete(areaId, next.Index))
            {
                result.Problems.Add(string.Format(
                    "pressing Do It on area {0} task {1} ('{2}') did not buy it",
                    areaId, next.Index, next.Label));
                yield break;
            }

            int charged = starsBefore - StoryProgress.StarsIn(areaId);
            if (charged != next.Stars)
                result.Problems.Add(string.Format(
                    "area {0} task {1} ('{2}') costs {3} star(s) and charged {4}",
                    areaId, next.Index, next.Label, next.Stars, charged));

            result.Repairs++;
            result.StarsSpent += charged;
        }

        private static int RowFor(AreaScreen screen, int taskIndex)
        {
            for (int i = 0; i < screen.RowCount; i++)
                if (screen.RowTaskIndex(i) == taskIndex) return i;
            return -1;
        }

        #endregion

        #region Waiting

        /// <summary>
        /// Waits until nothing of the story's is playing over the top. Anything
        /// that waits on a tap is given one, so the walk is not blocked by a beat
        /// it cannot dismiss — the arc scene on every fifth area is exactly that.
        /// </summary>
        private IEnumerator WaitForCalm(float timeout)
        {
            float waited = 0f;
            while (waited < timeout)
            {
                if (!StoryShell.StoryIsPlaying) yield break;
                PressOnThroughDialogue();
                yield return new WaitForSeconds(Poll);
                waited += Poll;
            }

            Debug.LogWarning(string.Format(
                "[Story] shell test: a beat was still playing after {0}s.", timeout));
        }

        /// <summary>
        /// Dialogue never advances itself: one tap finishes the line, the next
        /// moves on. This presses the screen's own full-screen Advance button,
        /// which is the handler the tap would reach — not a synthetic click.
        /// </summary>
        private static void PressOnThroughDialogue()
        {
            if (!DialogueSystem.IsPlaying) return;

            GameObject host = GameObject.Find("~StoryDialogue");
            if (host == null) return;

            Button[] buttons = host.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
                if (buttons[i].name == "Advance")
                {
                    buttons[i].onClick.Invoke();
                    return;
                }
        }

        /// <summary>
        /// The board self-test owns the first stretch of every play session: it
        /// loads the game scene and plays a level in it. This gets out of its way
        /// rather than racing it.
        /// </summary>
        private IEnumerator WaitForTheBoardTest()
        {
            float waited = 0f;
            while (waited < BoardTestTimeout)
            {
                if (StoryPowerupTest.IsFinished && !StoryNavBar.LevelIsPlaying &&
                    StoryShell.InGameScene)
                    yield break;

                yield return new WaitForSeconds(Poll);
                waited += Poll;
            }

            Debug.LogWarning(
                "[Story] shell test: gave up waiting for the board self-test; running anyway.");
        }

        #endregion
    }
}
