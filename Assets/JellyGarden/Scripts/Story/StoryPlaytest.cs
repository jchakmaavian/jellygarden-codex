// Plays a level by itself and reports what went wrong.
//
// Royal Throne's Port checks prove specific board rules. This is the blunter
// instrument next to them: it opens a level, finds a legal move, makes it,
// waits for the cascade to settle, and repeats until the level ends —
// recording every error Unity raised on the way and every place the board
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
//     tools/run_player.sh -storyplaytest
//
// The report is written to Application.persistentDataPath/playtest.txt and
// echoed to the log, so a batchmode run can be read from either. Two PNGs per
// level land next to it in Application.persistentDataPath/playtest/, except in
// batchmode, where no shots are taken.

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using JellyGarden.Scripts.GUI.TutorialStuff;
using JellyGarden.Scripts.Targets;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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

        /// <summary>Where the PNGs go, or empty when the directory could not be made.</summary>
        private string _shotDir = string.Empty;

        /// <summary>Numbers the shots in playing order, across the whole run.</summary>
        private int _shot;

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
            if (!_campaignReview) StartCoroutine(Drive());
        }

        private bool _campaignReview;

        // The area review uses the same legal-move player, with normal win and
        // repair handlers between boards. No forced wins or injected stars.
        public static IEnumerator PlayForCampaignReview(int level, int area)
        {
            var host = new GameObject("~CampaignBoardReview");
            var player = host.AddComponent<StoryPlaytest>();
            player._campaignReview = true;
            player._level = level;
            // Fresh campaign entry already launched board 1. Loading it again
            // mid-tutorial would destroy the pieces its hint coroutine owns.
            if (LevelManager.THIS == null || StoryFlow.PlayingLevel != level ||
                LevelManager.THIS.gameStatus != GameState.Playing)
                yield return player.Reach(level, area);
            player.HideTutorial();
            yield return TreasureHuntReview.ReviewBoard(level);
            yield return player.PlayOut(level);
            Destroy(host);
        }

        private IEnumerator Drive()
        {
            List<int> levels = Levels();

            OpenShotDir();

            Say("starting; levels [" +
                string.Join(", ", levels.ConvertAll(l => l + " (" + Where(l) + ")").ToArray()) +
                "], back to back in one session.");
            Say(_shotDir.Length > 0
                ? "screenshots in " + _shotDir
                : "screenshots: none (headless, or the directory could not be made).");

            // The shape a fault will be judged in. A sweep run full screen is
            // looking at the wrong aspect and its pictures cannot be trusted.
            Say(string.Format("window {0}x{1}{2}; shots at {3}x.",
                Screen.width, Screen.height,
                Screen.fullScreen ? " FULL SCREEN" : " windowed",
                StoryWindow.ShotSuperSize));
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

                // Taken before the playable check rather than after it, so that
                // the one level in the sweep that never draws a board is the one
                // level with a picture of what the player saw instead.
                yield return Shot(level, "start");

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
                yield return Shot(level, "end");

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
            bool shotTheBeat = false;

            while (waited < 30f && LevelManager.THIS.gameStatus != GameState.Playing)
            {
                // A level can be gated on a story beat: level 157 opens with the
                // sabotage line, and StoryFlow.LaunchLevel holds the board back
                // until it is done. A beat waits for a tap forever, so a bot that
                // only sleeps reports the beat as "never became playable — black
                // screen" and, because that finding breaks the run, every level
                // behind it goes unplayed. Tap through it the way a player would.
                if (DialogueSystem.IsPlaying && !shotTheBeat)
                {
                    // The card IS what the player is looking at here, so it gets
                    // its own picture before it is dismissed. Without one the only
                    // evidence a beat ever played is that the level took longer to
                    // open — and the fault this shot exists to catch is a card
                    // whose text falls off the bottom of it.
                    //
                    // Shot after ONE press, not before it: the first press is what
                    // finishes the typewriter, and a picture taken on the frame the
                    // card opens is a picture of an empty card.
                    shotTheBeat = true;
                    Say("story beat before the board; photographing it, then tapping through.");
                    PressOnThroughDialogue();
                    yield return new WaitForSecondsRealtime(0.4f);
                    yield return Shot(level, "beat");
                }

                PressOnThroughDialogue();

                yield return new WaitForSecondsRealtime(0.25f);
                waited += 0.25f;
            }
            yield return new WaitForSecondsRealtime(1.5f);
        }

        /// <summary>
        /// Presses the dialogue's own full-screen Advance button — the handler a
        /// tap would reach — rather than synthesising a click. Same shape as
        /// StoryThemeTest.PressOnThroughDialogue and StoryShellTest's.
        ///
        /// One press finishes the line being typed, the next moves on, so this is
        /// called once per poll rather than looped: the card is on screen long
        /// enough to be worth a picture if a later shot wants one.
        /// </summary>
        private static void PressOnThroughDialogue()
        {
            if (!DialogueSystem.IsPlaying) return;

            GameObject host = GameObject.Find("~StoryDialogue");
            if (host == null) return;

            Button[] buttons = host.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
                if (buttons[i].name == "Advance") buttons[i].onClick.Invoke();
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

            int repairsBefore = JellyGarden.Scripts.Juice.RoyalBoardIntegrity.Repairs;
            yield return PlayMoves(manager);
            ReportIntegrity(repairsBefore);
        }

        /// <summary>
        /// Royal Throne's board guard repairs a cell that two pieces claimed, or
        /// a piece no cell owns. Each repair is an ownership fault the player
        /// may have seen, so any on a level is a finding.
        /// </summary>
        private void ReportIntegrity(int repairsBefore)
        {
            int repairs = JellyGarden.Scripts.Juice.RoyalBoardIntegrity.Repairs - repairsBefore;
            Say(string.Format("board integrity repairs this level: {0} (session {1}).",
                repairs, JellyGarden.Scripts.Juice.RoyalBoardIntegrity.Repairs));
            if (repairs > 0)
                Finding(string.Format("RoyalBoardIntegrity repaired the board {0} time(s); last: {1}",
                    repairs, JellyGarden.Scripts.Juice.RoyalBoardIntegrity.LastReport));
        }

        private IEnumerator PlayMoves(LevelManager manager)
        {
            int move = 0;
            int noMoveRuns = 0;

            while (manager.gameStatus == GameState.Playing
                   || manager.gameStatus == GameState.RegenLevel)
            {
                // A reshuffle is not the end of a level, it is the middle of
                // one. `LevelManager.NoMatchesCor` puts the board into
                // RegenLevel, shows "no more matches", rebuilds the items and
                // comes back to Playing — so a run that treated any state but
                // Playing as an ending reported a reshuffled level as "ended in
                // state Playing, which is neither won nor lost" and stopped
                // playing it. Wait it out instead; a reshuffle that never ends
                // is what a finding is for.
                yield return WaitOutReshuffle();

                bool settled = false;
                yield return Settle(r => settled = r);
                if (!settled)
                {
                    Finding(string.Format(
                        "the board never settled after move {0} — it was still resolving after {1}s. " +
                        "That is a cascade that stopped halfway; the level cannot end from here.",
                        move, SettleTimeout));
                    yield break;
                }

                if (manager.gameStatus == GameState.RegenLevel) continue;
                if (manager.gameStatus != GameState.Playing) break;

                Move found = FindMove();
                if (found == null)
                {
                    noMoveRuns++;
                    Say("no legal move on the board; waiting for a reshuffle (" + noMoveRuns
                        + "); watchdog reshuffles so far " + StoryDeadBoard.Reshuffles + ".");
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

                // The same entry a finger drag reaches; it re-checks CanAcceptInput
                // on both pieces and drops the swap if either is no longer free.
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
                yield return WaitOut(GameState.PreWinAnimations, "win animation");
                if (manager.gameStatus == GameState.PreWinAnimations)
                {
                    Finding(
                        "the level was won but never showed its win: it stopped moving in " +
                        "PreWinAnimations. Targets are complete and moves are spent, and no " +
                        "popup came.");
                    yield break;
                }
            }

            // The same wait on the losing side, and for the same reason the win
            // side has one.
            //
            // PreFailed is the kit's "buy five more moves" banner sliding in; the
            // level is not over until its animation event sets GameOver, which is
            // what raises OnLose and puts the fail panel up. Recording the result
            // at PreFailed and shooting straight away photographed the banner
            // mid-slide and never the panel, so every lost level in the band came
            // back with a picture of a grey stripe. A level that never leaves
            // PreFailed is a finding of its own: it is a level that cannot end.
            if (manager.gameStatus == GameState.PreFailed)
            {
                yield return WaitOut(GameState.PreFailed, "fail banner");
                if (manager.gameStatus == GameState.PreFailed)
                {
                    Finding(
                        "the level was lost but never finished losing: it stopped moving in " +
                        "PreFailed. Moves are spent and no end panel came.");
                    yield break;
                }
            }

            // Give the end panel a moment to build, so the shot that follows is
            // of the panel rather than of the frame it opens on.
            yield return new WaitForSecondsRealtime(1f);

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

        /// <summary>
        /// The longest an end-of-level animation may go with NOTHING happening
        /// before it is called stuck: no bonus spent, no point scored.
        ///
        /// Measured, not chosen. Across the end animations of levels 113-128 the
        /// longest quiet gap inside a win that DID finish was 5.5 seconds, and
        /// every fail banner was quiet for at most 0.5 before it moved the state
        /// itself. Fifteen is 2.7x the worst real gap — far enough that a slow
        /// animation is never called stuck, and short enough that a stuck one is
        /// reported four times sooner than the 60-second clock it replaced.
        /// </summary>
        private const float StallWindow = 15f;

        /// <summary>
        /// An absolute stop, so an animation that twitches forever still ends the
        /// level. Two and a half times the longest end animation on record, and
        /// under `PER_LEVEL_BUDGET` in tools/playtest_sweep.py, so a level that
        /// hits it is reported as a finding rather than killing its whole batch.
        /// </summary>
        private const float EndingCeiling = 120f;

        /// <summary>
        /// Waits for the board to leave an end-of-level state, and gives up on
        /// PROGRESS rather than on the clock.
        ///
        /// A fixed clock cannot tell a slow win animation from a stopped one, and
        /// the two are far apart: over the 71 wins in the sweep reports on this
        /// machine the animation ran anywhere from half a second to 48.5, and it
        /// does not scale with what it has to spend — level 111 took 33.5s with no
        /// moves left to convert and level 80 took 30s with 57. The old 60-second
        /// bound was 1.24x the worst real win, and level 118 duly crossed it and
        /// was reported as a level that never showed its win, having shown it
        /// moments later. That is a fault in the thing watching, not in the game.
        ///
        /// The score and the move counter are what the win animation moves as it
        /// spends leftover moves, and the fail banner ends by moving the state
        /// itself. So: wait while either is still changing, and call it stuck only
        /// after <see cref="StallWindow"/> of complete quiet. A genuinely stuck
        /// level is now reported four times sooner than before, and a slow one is
        /// no longer reported at all.
        /// </summary>
        private IEnumerator WaitOut(GameState state, string what)
        {
            LevelManager manager = LevelManager.THIS;
            float waited = 0f;
            float quiet = 0f;
            float longestQuiet = 0f;
            int score = LevelManager.Score;
            int limit = manager.Limit;

            while (manager.gameStatus == state && waited < EndingCeiling && quiet < StallWindow)
            {
                yield return new WaitForSecondsRealtime(0.5f);
                waited += 0.5f;

                if (LevelManager.Score != score || manager.Limit != limit)
                {
                    score = LevelManager.Score;
                    limit = manager.Limit;
                    quiet = 0f;
                }
                else
                {
                    quiet += 0.5f;
                    if (quiet > longestQuiet) longestQuiet = quiet;
                }
            }

            Say(what + " finished after " + waited + "s (longest quiet " + longestQuiet +
                "s); state " + manager.gameStatus + ".");
        }

        /// <summary>
        /// Waits out a reshuffle, and reports one that never ends.
        ///
        /// `LevelManager.NoMatchesCor` sets RegenLevel, waits a second, destroys
        /// the items and generates new ones, and `RegenMatches` returns the board
        /// to Playing. Two and a half seconds is the whole of it when it works,
        /// so a board still in RegenLevel after thirty is stuck in the one state
        /// the player has no button for.
        /// </summary>
        private IEnumerator WaitOutReshuffle()
        {
            LevelManager manager = LevelManager.THIS;
            if (manager.gameStatus != GameState.RegenLevel) yield break;

            Say("the board is reshuffling.");

            float wait = 0f;
            while (wait < 30f && manager.gameStatus == GameState.RegenLevel)
            {
                yield return new WaitForSecondsRealtime(0.25f);
                wait += 0.25f;
            }

            if (manager.gameStatus == GameState.RegenLevel)
            {
                Finding("the board went into a reshuffle and never came out of it: " +
                        "30s in RegenLevel. The level cannot be played on from here.");
                yield break;
            }

            Say("reshuffled after " + wait + "s; state " + manager.gameStatus + ".");
        }

        /// <summary>
        /// True when the board is idle in Royal Throne's terms: the resolver is
        /// not running, no power-up is in flight, no swap is held and nothing
        /// is falling.
        /// </summary>
        private static bool Settled(LevelManager manager)
        {
            return !manager.IsResolvingMatches && !JellyGarden.Scripts.Juice.RoyalPowerupDirector.Busy &&
                   manager.InputSwaps == 0 && !AnythingFalling();
        }

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

                if (Settled(manager))
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

        #region Screenshots

        /// <summary>
        /// Makes the directory the PNGs go in. A failure here is not worth
        /// stopping the run for — the log and the report are still the point —
        /// so it is reported once and every later shot is skipped.
        /// </summary>
        private void OpenShotDir()
        {
            // -nographics has no frame to wait for the end of, and a batchmode
            // Editor hung on WaitForEndOfFrame in the baseline run, so a shot
            // would hang the whole sweep. Those runs get the text report only.
            if (Application.isBatchMode ||
                SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                _shotDir = string.Empty;
                return;
            }

            string dir = Path.Combine(Application.persistentDataPath, "playtest");
            try
            {
                Directory.CreateDirectory(dir);
                _shotDir = dir;
            }
            catch (global::System.Exception e)
            {
                _shotDir = string.Empty;
                Debug.LogWarning("[Playtest] could not make " + dir + ": " + e.Message);
            }
        }

        /// <summary>
        /// A PNG of the screen as it is right now.
        ///
        /// A visual fault raises no error and ends no level: a board off centre,
        /// a HUD sitting over it, an unreadable target chip and a tile with no
        /// sprite all play through perfectly. The log cannot see any of them, so
        /// a sweep that only writes text finds only the faults that throw.
        ///
        /// Named 0001_level_1_start.png, so the directory sorts into playing
        /// order and a level's two shots sit next to each other.
        ///
        /// This costs a normal player nothing: the component that calls it is
        /// only ever built when the playtest was asked for.
        /// </summary>
        private IEnumerator Shot(int level, string moment)
        {
            if (_shotDir.Length == 0) yield break;

            HideTutorial();

            // End of frame, or the capture catches the UI mid-build.
            yield return new WaitForEndOfFrame();

            string name = string.Format("{0:0000}_level_{1}_{2}.png", ++_shot, level, moment);
            StoryWindow.Capture(Path.Combine(_shotDir, name));

            // CaptureScreenshot is asynchronous; give it a moment to land.
            yield return new WaitForSecondsRealtime(0.6f);
            Say("shot -> " + name);
        }

        /// <summary>
        /// Takes the kit's tutorial overlay off the screen before a shot.
        ///
        /// On level 1 — and on any level the kit decides to teach — TutorialManager
        /// dims the whole screen and puts a pointing hand over the board. The
        /// start shot landed under it, so the one picture of the level a reviewer
        /// has shows the overlay and not the board. That is ralph/findings.md
        /// F-004, and StoryThemeTest already answers it the same way.
        ///
        /// Switched off rather than destroyed: this is a run that exists to take
        /// pictures, and the bot does not need to be taught how to swap two gems.
        /// </summary>
        private void HideTutorial()
        {
            int hidden = 0;

            foreach (TutorialManager manager in
                     FindObjectsByType<TutorialManager>(FindObjectsInactive.Include))
            {
                if (manager == null || manager.tutorial == null) continue;
                if (!manager.tutorial.activeSelf) continue;
                manager.tutorial.SetActive(false);
                hidden++;
            }

            if (hidden > 0) Say("kit tutorial overlay hidden for the shot.");
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
        ///
        /// Only pieces whose CanAcceptInput is true are tried: a piece that is
        /// reserved, leased to a swap or mid-flight would refuse the swap.
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
            if (!mine.CanAcceptInput || !theirs.CanAcceptInput) return null;
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
