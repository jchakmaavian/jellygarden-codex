// Plays a level by itself and reports what went wrong.
//
// StoryPowerupTest proves two specific powerup rules on a live board. This is
// the blunter instrument next to it: it opens a level, finds a legal move,
// makes it, waits for the cascade to settle, and repeats until the level ends
// — recording every error Unity raised on the way and every place the board
// stopped answering.
//
// It exists because the bugs that matter here do not show up in a headless
// content test. A destroyed HUD object that the kit still writes to throws
// inside a coroutine, and a coroutine that dies mid-cascade leaves the board
// looking fine and behaving like it is thinking. Only playing a level finds
// that, and playing it by hand finds it once.
//
// Launch a development player, or enter play mode, with:
//
//     open -a KingLeo.app --args -screen-fullscreen 0 -screen-width 393 -screen-height 852 -storyplaytest
//
// The report is written to Application.persistentDataPath/playtest.txt and
// echoed to the log, so a batchmode run can be read from either.

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using JellyGarden.Scripts.Targets;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JellyGarden.Scripts.Story
{
    [DisallowMultipleComponent]
    public sealed class StoryPlaytest : MonoBehaviour
    {
        private const string Flag = "-storyplaytest";

        /// <summary>Set by the Editor runner, which cannot pass a command line.</summary>
        private const string Pref = "StoryPlaytest.Requested";

        /// <summary>Which level to play. The Editor runner overrides it.</summary>
        private const string LevelPref = "StoryPlaytest.Level";

        /// <summary>
        /// A comma-separated run of levels, played back to back without leaving
        /// the scene. One level proves almost nothing here: the faults that
        /// matter come from state the first level leaves behind — a HUD it
        /// destroyed, a flag it never cleared — and only the second level in a
        /// session walks into them.
        /// </summary>
        private const string LevelsPref = "StoryPlaytest.Levels";

        /// <summary>Written when the run is over, so a batchmode host can stop waiting.</summary>
        public const string ReportName = "playtest.txt";

        private static bool _installed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void Install()
        {
            if (_installed) return;
            if (!Requested()) return;

            _installed = true;
            var host = new GameObject("~StoryPlaytest");
            DontDestroyOnLoad(host);
            host.AddComponent<StoryPlaytest>();
        }

        private static bool Requested()
        {
            foreach (string arg in global::System.Environment.GetCommandLineArgs())
                if (arg == Flag) return true;

            if (PlayerPrefs.GetInt(Pref, 0) != 1) return false;

            // One shot. The pref outlives the run that set it, so leaving it up
            // turns every later press of Play — the tester's own, days later —
            // into another playtest. The command line is not cleared: a player
            // launched with the flag is meant to do nothing else.
            PlayerPrefs.SetInt(Pref, 0);
            PlayerPrefs.Save();
            return true;
        }

        #region The report

        private readonly List<string> _lines = new List<string>();
        private readonly List<string> _errors = new List<string>();
        private readonly List<string> _findings = new List<string>();

        /// <summary>
        /// The level being played right now, so a finding can name it without
        /// every call site remembering to. A sweep of 1208 levels is triaged
        /// from the findings alone; one that does not say where it came from
        /// costs a reader the whole log.
        /// </summary>
        private int _level;

        private int _played;
        private int _won;
        private int _lost;

        private void Say(string line)
        {
            _lines.Add(line);
            Debug.Log("[Playtest] " + line);
        }

        /// <summary>Something a player would notice. These are the point of the run.</summary>
        private void Finding(string what)
        {
            string named = (_level > 0 ? "level " + _level + ": " : "") + what;
            _findings.Add(named);
            _lines.Add("FINDING: " + named);
            Debug.LogWarning("[Playtest] FINDING: " + named);
        }

        private void OnEnable() { Application.logMessageReceived += Caught; }
        private void OnDisable() { Application.logMessageReceived -= Caught; }

        /// <summary>
        /// Every error the run raises, kept with its stack. An exception thrown
        /// inside a coroutine does not stop the game — it silently truncates that
        /// coroutine — so the log is the only place it is visible at all.
        /// </summary>
        private void Caught(string message, string stack, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
            if (message.StartsWith("[Playtest]")) return;

            string first = stack == null ? string.Empty : stack.Split('\n')[0];
            _errors.Add(type + ": " + message + (first.Length > 0 ? "\n    at " + first : string.Empty));
        }

        #endregion

        private void Start()
        {
            StartCoroutine(Drive());
        }

        private IEnumerator Drive()
        {
            List<int> levels = Levels();

            Say("starting; levels [" +
                string.Join(", ", levels.ConvertAll(l => l + " (" + Where(l) + ")").ToArray()) +
                "], back to back in one session.");
            yield return new WaitForSecondsRealtime(2f);

            for (int i = 0; i < levels.Count; i++)
            {
                int level = levels[i];
                int area = AreaOf(level);
                _level = level;

                Say("---- level " + level + " (" + (i + 1) + " of " + levels.Count + "), " +
                    Where(level) + " ----");

                if (area <= 0)
                {
                    // Playing it in area 1 anyway would be testing the level
                    // wearing area 1's theme, obstacles and story, and reporting
                    // that as fine. A level nothing owns is the finding.
                    Finding(
                        "belongs to no area — the level plan has no entry for it, so it has " +
                        "no theme, no obstacle set and no repair it pays for. Skipped rather than " +
                        "played in area 1.");
                    continue;
                }

                yield return Reach(level, area);
                _played++;

                if (LevelManager.THIS == null || LevelManager.THIS.gameStatus != GameState.Playing)
                {
                    // This is the black screen, seen from the inside: the board
                    // was asked for and never arrived.
                    Finding(string.Format(
                        "never became playable — state {0}, {1} square(s) on the board. " +
                        "Nothing is drawn, which on screen is a black screen.",
                        LevelManager.THIS == null ? "no LevelManager" : LevelManager.THIS.gameStatus.ToString(),
                        LevelManager.THIS == null || LevelManager.THIS.squaresArray == null
                            ? 0 : LevelManager.THIS.squaresArray.Length));
                    break;
                }

                yield return PlayOut(level);

                if (i + 1 < levels.Count) yield return LeaveLevel();
            }

            _level = 0;
            Finish();
        }

        private List<int> Levels()
        {
            var levels = new List<int>();

            // A player has no EditorPrefs and no one to set them, so the command
            // line is how a built player is told what to play. This is the path
            // that matters: a player never reloads its domain mid-run, and the
            // Editor does, which makes the Editor the less trustworthy of the
            // two for anything that outlives a single frame.
            string raw = string.Empty;
            foreach (string arg in global::System.Environment.GetCommandLineArgs())
                if (arg.StartsWith("-playtestLevels="))
                    raw = arg.Substring("-playtestLevels=".Length);

            if (raw.Length == 0) raw = PlayerPrefs.GetString(LevelsPref, string.Empty);

            foreach (string part in raw.Split(','))
            {
                int level;
                if (int.TryParse(part.Trim(), out level) && level > 0) levels.Add(level);
            }

            if (levels.Count == 0) levels.Add(PlayerPrefs.GetInt(LevelPref, 1));
            return levels;
        }

        /// <summary>
        /// Hands the level back the way the game does, so the next one starts
        /// from the state a player would actually be in.
        /// </summary>
        private IEnumerator LeaveLevel()
        {
            GameState ended = LevelManager.THIS == null ? GameState.Map : LevelManager.THIS.gameStatus;

            if (ended == GameState.Win) StoryFlow.HandleLevelComplete();
            else StoryFlow.HandleLevelFailed();

            yield return new WaitForSecondsRealtime(3f);
            Say("left the level; state " +
                (LevelManager.THIS == null ? "no LevelManager" : LevelManager.THIS.gameStatus.ToString()) + ".");
        }

        /// <summary>
        /// The area a level actually belongs to, or 0 when nothing owns it.
        ///
        /// The level plan is the map from level number to area. An area the plan
        /// names but the story does not carry is no owner either: the theme, the
        /// obstacle set and the repair the level pays for all come off the story
        /// row, so without one there is nothing to launch the level into.
        /// </summary>
        private static int AreaOf(int level)
        {
            int areaId = StoryLevelPlan.AreaOf(level);
            if (areaId <= 0) return 0;
            return StoryDatabase.GetArea(areaId) != null ? areaId : 0;
        }

        /// <summary>How a level's home reads in the report.</summary>
        private static string Where(int level)
        {
            int areaId = AreaOf(level);
            if (areaId <= 0) return "no area";

            StoryArea area = StoryDatabase.GetArea(areaId);
            string name = area == null ? string.Empty : area.Name;
            return name.Length > 0 ? "area " + areaId + " " + name : "area " + areaId;
        }

        /// <summary>Gets to a live board without anyone clicking a map node.</summary>
        private IEnumerator Reach(int level, int areaId)
        {
            if (LevelManager.THIS == null)
            {
                SceneManager.LoadScene("game");
                float wait = 0f;
                while (wait < 25f && LevelManager.THIS == null)
                {
                    yield return new WaitForSecondsRealtime(0.25f);
                    wait += 0.25f;
                }
                yield return new WaitForSecondsRealtime(1.5f);
            }

            if (LevelManager.THIS == null) yield break;

            // Deliberately NOT reloading the scene between levels: a fresh scene
            // rebuilds the kit's HUD and hides exactly the fault being looked
            // for, which only exists because the previous level destroyed it.
            StoryFlow.LaunchLevel(level, areaId);

            float waited = 0f;
            while (waited < 30f && LevelManager.THIS.gameStatus != GameState.Playing)
            {
                yield return new WaitForSecondsRealtime(0.25f);
                waited += 0.25f;
            }
            yield return new WaitForSecondsRealtime(1.5f);
        }

        #region Playing

        /// <summary>How long a cascade may take before it counts as stuck.</summary>
        private const float SettleTimeout = 20f;

        private IEnumerator PlayOut(int level)
        {
            LevelManager manager = LevelManager.THIS;

            Say(string.Format("level {0}: {1}, limit {2}, {3} target(s), pass mark {4}.",
                level, manager.limitType, manager.Limit,
                manager.targetObject == null ? 0 : manager.targetObject.Length, manager.star1));
            Say("targets: " + Targets());

            int move = 0;
            int noMoveRuns = 0;

            while (manager.gameStatus == GameState.Playing)
            {
                bool settled = false;
                yield return Settle(r => settled = r);
                if (!settled)
                {
                    Finding(string.Format(
                        "the board never settled after move {0} — DragBlocked stayed true for {1}s. " +
                        "That is a cascade that stopped halfway; the level cannot end from here.",
                        move, SettleTimeout));
                    yield break;
                }

                if (manager.gameStatus != GameState.Playing) break;

                Move found = FindMove();
                if (found == null)
                {
                    noMoveRuns++;
                    Say("no legal move on the board; waiting for a reshuffle (" + noMoveRuns + ").");
                    yield return new WaitForSecondsRealtime(3f);
                    if (noMoveRuns >= 4)
                    {
                        Finding(string.Format(
                            "no legal move for four checks running and the board never reshuffled. " +
                            "Given up after {0} move(s); the level cannot be finished from here.",
                            move));
                        yield break;
                    }
                    continue;
                }

                noMoveRuns = 0;
                move++;

                int before = manager.Limit;
                int scoreBefore = LevelManager.Score;

                found.Item.switchDirection = Vector3.zero;
                found.Item.SwitchDirection(found.Delta);

                yield return new WaitForSecondsRealtime(0.6f);
                bool moveSettled = false;
                yield return Settle(r => moveSettled = r);

                if (!moveSettled)
                {
                    Finding(string.Format(
                        "move {0} never finished — the board stayed blocked for {1}s after the swap. " +
                        "Score {2}, moves left {3}.",
                        move, SettleTimeout, LevelManager.Score, manager.Limit));
                    yield break;
                }

                Say(string.Format("move {0}: moves {1}->{2}, score {3}->{4}, targets {5}",
                    move, before, manager.Limit, scoreBefore, LevelManager.Score, Targets()));

                if (manager.Limit == before && manager.gameStatus == GameState.Playing)
                    Say("  (that swap was not charged a move)");

                if (move > 200)
                {
                    Finding(string.Format(
                        "played 200 moves ({0} limit, {1} left) without the level ending; giving up.",
                        manager.limitType, manager.Limit));
                    yield break;
                }
            }

            yield return Ending(move);
        }

        /// <summary>Watches the level actually finish, which is where it went wrong before.</summary>
        private IEnumerator Ending(int move)
        {
            LevelManager manager = LevelManager.THIS;
            GameState reached = manager.gameStatus;
            Say("board stopped playing after " + move + " move(s); state " + reached + ".");

            if (reached == GameState.PreWinAnimations)
            {
                float wait = 0f;
                while (wait < 60f && manager.gameStatus == GameState.PreWinAnimations)
                {
                    yield return new WaitForSecondsRealtime(0.5f);
                    wait += 0.5f;
                }

                if (manager.gameStatus == GameState.PreWinAnimations)
                {
                    Finding(
                        "the level was won but never showed its win: it sat in PreWinAnimations " +
                        "for 60s. Targets are complete and moves are spent, and no popup came.");
                    yield break;
                }

                Say("win animation finished after " + wait + "s; state " + manager.gameStatus + ".");
            }

            // A level the bot cannot beat is a result, not a fault: the sweep
            // records it as lost and goes on to the next one. Only a state that
            // is neither won nor lost is worth a finding.
            if (manager.gameStatus == GameState.Win)
            {
                _won++;
                Say("RESULT: won. Score " + LevelManager.Score + ", stars " + manager.stars + ".");
            }
            else if (manager.gameStatus == GameState.GameOver || manager.gameStatus == GameState.PreFailed)
            {
                _lost++;
                Say("RESULT: lost. Score " + LevelManager.Score + ", targets " + Targets() + ".");
            }
            else
            {
                Finding("ended in state " + manager.gameStatus + ", which is neither won nor lost.");
            }
        }

        /// <summary>True when the board is idle: nothing blocked, nothing falling, nothing matched.</summary>
        private IEnumerator Settle(global::System.Action<bool> report)
        {
            LevelManager manager = LevelManager.THIS;
            float wait = 0f;

            while (wait < SettleTimeout)
            {
                if (manager.gameStatus != GameState.Playing &&
                    manager.gameStatus != GameState.PreWinAnimations)
                {
                    report(true);
                    yield break;
                }

                if (!manager.DragBlocked && !AnythingFalling() && manager.GetMatches().Count == 0)
                {
                    report(true);
                    yield break;
                }

                yield return new WaitForSecondsRealtime(0.1f);
                wait += 0.1f;
            }

            report(false);
        }

        private static bool AnythingFalling()
        {
            Square[] squares = LevelManager.THIS.squaresArray;
            if (squares == null) return false;

            foreach (Square square in squares)
            {
                if (square == null || square.item == null) continue;
                if (square.item.falling) return true;
            }
            return false;
        }

        #endregion

        #region Finding a legal move

        private sealed class Move
        {
            public Item Item;
            public Vector3 Delta;
            public string Where;
        }

        /// <summary>
        /// Scans for a swap that makes a match, the same way the board itself
        /// decides one: swap the two squares' item references, ask both squares
        /// what they now match, and put them back. Only the references move, so
        /// nothing on screen is touched by the scan.
        ///
        /// The deltas are the ones Item.SwitchDirection reads, and they are
        /// inverted against the direction of travel because the kit computes its
        /// drag as (where the finger started - where it is now).
        /// </summary>
        private Move FindMove()
        {
            Square[] squares = LevelManager.THIS.squaresArray;
            if (squares == null) return null;

            foreach (Square square in squares)
            {
                if (square == null || square.item == null) continue;

                Move move = TryDirection(square, square.GetNeighborLeft(), new Vector3(1, 0, 0), "left")
                            ?? TryDirection(square, square.GetNeighborRight(), new Vector3(-1, 0, 0), "right")
                            ?? TryDirection(square, square.GetNeighborBottom(), new Vector3(0, 1, 0), "down")
                            ?? TryDirection(square, square.GetNeighborTop(), new Vector3(0, -1, 0), "up");
                if (move != null) return move;
            }

            return null;
        }

        private static Move TryDirection(Square square, Square neighbour, Vector3 delta, string where)
        {
            if (neighbour == null || neighbour.item == null || square.item == null) return null;
            if (neighbour.type == SquareTypes.WIREBLOCK || square.type == SquareTypes.WIREBLOCK) return null;

            Item mine = square.item;
            Item theirs = neighbour.item;
            if (mine.falling || theirs.falling) return null;
            if (mine.currentType == ItemsTypes.INGREDIENT || theirs.currentType == ItemsTypes.INGREDIENT) return null;

            neighbour.item = mine;
            square.item = theirs;
            int matches = neighbour.FindMatchesAround().Count + square.FindMatchesAround().Count;
            neighbour.item = theirs;
            square.item = mine;

            if (matches <= 0) return null;
            return new Move { Item = mine, Delta = delta, Where = where };
        }

        #endregion

        private static string Targets()
        {
            TargetObject[] targets = LevelManager.THIS.targetObject;
            if (targets == null) return "none";

            var text = new StringBuilder();
            for (int i = 0; i < targets.Length; i++)
            {
                if (i > 0) text.Append(", ");
                text.Append(targets[i].type).Append(' ')
                    .Append(targets[i].countCollected).Append('/').Append(targets[i].targetCount);
            }
            return text.ToString();
        }

        private void Finish()
        {
            var report = new StringBuilder();
            report.AppendLine("==== playtest ====");
            foreach (string line in _lines) report.AppendLine(line);

            report.AppendLine();
            report.AppendLine("---- errors raised during play: " + _errors.Count + " ----");
            // Identical exceptions repeat once per match, so they are counted
            // rather than printed hundreds of times.
            var seen = new Dictionary<string, int>();
            var order = new List<string>();
            foreach (string error in _errors)
            {
                if (!seen.ContainsKey(error)) { seen[error] = 0; order.Add(error); }
                seen[error]++;
            }
            foreach (string error in order)
                report.AppendLine("x" + seen[error] + "  " + error);

            report.AppendLine();
            report.AppendLine("---- findings: " + _findings.Count + " ----");
            foreach (string finding in _findings) report.AppendLine("- " + finding);
            if (_findings.Count == 0 && order.Count == 0)
                report.AppendLine("(the level played through with nothing raised)");
            report.AppendLine("==== end ====");

            // One line a batch report can read without parsing the rest.
            report.AppendLine(string.Format(
                "SUMMARY: {0} level(s) played, {1} won, {2} lost, {3} finding(s), {4} error(s).",
                _played, _won, _lost, _findings.Count, _errors.Count));

            string text = report.ToString();
            Debug.Log(text);

            try
            {
                File.WriteAllText(Path.Combine(Application.persistentDataPath, ReportName), text);
            }
            catch (global::System.Exception e)
            {
                Debug.LogWarning("[Playtest] could not write the report: " + e.Message);
            }
        }
    }
}
