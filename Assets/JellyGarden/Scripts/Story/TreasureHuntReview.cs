// Opt-in, isolated real-player review. Uses actual legal swaps and UI handlers.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace JellyGarden.Scripts.Story
{
    public sealed class TreasureHuntReview : MonoBehaviour
    {
        private static string _directory;
        private static bool _vinesOnly, _skipVines, _smallVines;
        private static int _onlyTask;
        private static bool _smallTransitions, _skipTransitions;
        private static bool _replayOpening, _bookReview, _fidelityReview;
        private bool _screensReviewed;
        private static TreasureHuntReview _active;
        private readonly List<string> _errors = new List<string>();
        private float _deadline;
        private bool _finished;
        private int _startingTasks;

        // Select the review save before StoryBoot loads progress at BeforeSceneLoad.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void Install()
        {
            string[] args = Environment.GetCommandLineArgs();
            if (Array.IndexOf(args, "-treasure-review") < 0) return;
            _vinesOnly = Array.IndexOf(args, "-treasure-review-vines") >= 0;
            _skipVines = Array.IndexOf(args, "-treasure-review-skip-vines") >= 0;
            _smallVines = Array.IndexOf(args, "-treasure-review-small-vines") >= 0;
            _smallTransitions = Array.IndexOf(args, "-treasure-review-small-transitions") >= 0;
            _skipTransitions = Array.IndexOf(args, "-treasure-review-skip-transitions") >= 0;
            _replayOpening = Array.IndexOf(args, "-treasure-review-opening") >= 0;
            _bookReview = Array.IndexOf(args, "-treasure-review-ui") >= 0;
            _fidelityReview = Array.IndexOf(args, "-treasure-review-fidelity") >= 0;
            foreach (string arg in args)
                if (arg.StartsWith("-treasure-review-task="))
                    _onlyTask = int.Parse(arg.Substring("-treasure-review-task=".Length));
            if (_onlyTask < 0 || _onlyTask > 4) throw new ArgumentException("Review task must be 1–4");
            foreach (string arg in args)
                if (arg.StartsWith("-treasure-review-dir=")) _directory = arg.Substring(21);
            if (string.IsNullOrEmpty(_directory)) throw new ArgumentException("Review output directory required");
            Directory.CreateDirectory(_directory);
            // Each review starts with a fresh isolated save, without deleting any
            // prior review or touching the player's campaign save.
            StoryProgress.SaveDirectoryOverride = Path.Combine(_directory, "save-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));
            Directory.CreateDirectory(StoryProgress.SaveDirectoryOverride);
            foreach (string arg in args)
                if (arg.StartsWith("-treasure-review-save="))
                    File.Copy(arg.Substring("-treasure-review-save=".Length),
                        Path.Combine(StoryProgress.SaveDirectoryOverride, StoryCampaign.SaveFileName));
            var host = new GameObject("~TreasureHuntReview");
            DontDestroyOnLoad(host);
            host.AddComponent<TreasureHuntReview>();
        }

        private void OnEnable() { _active = this; Application.logMessageReceived += Caught; }
        private void OnDisable() { Application.logMessageReceived -= Caught; }
        private void Caught(string message, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                _errors.Add(message + "\n" + stack);
        }
        private void Update()
        {
            if (!_finished && _deadline > 0f && Time.realtimeSinceStartup > _deadline)
            { _errors.Add("Review timed out"); Finish(); }
        }

        private IEnumerator Start()
        {
            _deadline = Time.realtimeSinceStartup + 900f;
            yield return new WaitForSecondsRealtime(4f);
            _startingTasks = StoryProgress.TasksCompletedIn(1);
            if (_fidelityReview)
            {
                yield return FidelityScreens();
                Finish();
                yield break;
            }
            if (_replayOpening)
            {
                if (!TreasureHuntPresentation.Play("OPEN-01", 1, null, true))
                    _errors.Add("Opening replay did not start");
                yield return Dialogue("opening");
            }
            for (int level = _startingTasks + 1; level <= (_vinesOnly ? 1 : _onlyTask > 0 ? _onlyTask : 4); level++)
            {
                // A focused replay may load the checkpoint earned by the first
                // review. It never grants stars or changes completed task flags.
                if ((!_vinesOnly && _onlyTask == 0) || !StoryProgress.IsLevelBeaten(level))
                {
                    yield return StoryPlaytest.PlayForCampaignReview(level, 1);
                    if (!StoryProgress.IsLevelBeaten(level))
                    { _errors.Add("Board " + level + " did not end in a real first win"); Finish(); yield break; }
                    if (_bookReview) yield return BothPhoneSizes("level-" + level + "-win");
                    else yield return Shot("level-" + level + "-win");
                    Tap(StoryWinPopup.HostName, "Continue");
                    yield return new WaitForSecondsRealtime(1f);
                }
                if (level == 1) yield return Dialogue("opening");
                yield return Settle();
                StoryHomeScreen.Open(1);
                if (_bookReview && !_screensReviewed) yield return BookScreens();
                yield return BothPhoneSizes("state-" + (level - 1));
                if (level == 1)
                {
                    StoryProgress.Flush();
                    StoryProgress.ReloadFromDisk();
                    if (StoryProgress.StarsIn(1) != 1) _errors.Add("First star lost after reload");
                }
                AreaScreen.Open(1);
                yield return new WaitForSecondsRealtime(0.7f);
                yield return BothPhoneSizes("task-" + level);
                StoryProgress.Flush();
                File.Copy(StoryProgress.SavePath, Path.Combine(_directory, "earned-before-task-" + level + ".json"), true);
                if (_smallTransitions)
                {
                    Screen.SetResolution(320, 568, false);
                    yield return new WaitForSecondsRealtime(.6f);
                }
                if (level == 1 && _vinesOnly)
                {
                    StoryProgress.Flush();
                    File.Copy(StoryProgress.SavePath, Path.Combine(_directory, "earned-before-vines.json"), true);
                    if (_smallVines)
                    {
                        Screen.SetResolution(320, 568, false);
                        yield return new WaitForSecondsRealtime(.6f);
                    }
                }
                // A person may purchase the task while its review screenshot is
                // being taken. That is still a normal UI purchase; do not buy it twice.
                if (!StoryProgress.IsTaskComplete(1, level))
                {
                    Button repair = AreaScreen.Instance.RepairButton(level - 1);
                    if (repair == null || !repair.interactable)
                    { _errors.Add("Repair " + level + " unavailable after win"); Finish(); yield break; }
                    repair.onClick.Invoke();
                }
                if (level == 1)
                {
                    if (!RepairCutscene.IsPlaying || DialogueSystem.IsPlaying ||
                        !AreaScreen.IsOpen || StoryProgress.TasksCompletedIn(1) != 1 ||
                        StoryProgress.StarsIn(1) != 0)
                        _errors.Add("Clear the vines did not start its transition after spending one star");
                    if (StoryRepair.Perform(1, 2, null))
                        _errors.Add("A second repair was accepted during the vine transition");
                    if (GameObject.Find("VineTransition") == null)
                        _errors.Add("The native vine effect did not attach to the room");
                    if (_skipVines)
                    {
                        yield return new WaitForSecondsRealtime(.15f);
                        Tap("~StoryRepairCutscene", "Skip");
                    }
                    else
                    {
                        yield return new WaitForSecondsRealtime(1.2f);
                        yield return Shot("vines-clearing-lower");
                        yield return new WaitForSecondsRealtime(.45f);
                        yield return Shot("vines-clearing-arch");
                    }
                }
                else
                {
                    var host = GameObject.Find("GateTransition");
                    var transition = host == null ? null : host.GetComponent<StoryGateTransition>();
                    string expected = StoryProgress.FindTask(StoryDatabase.GetArea(1), level).RepairTransition;
                    if (!RepairCutscene.IsPlaying || transition == null || transition.Kind != expected ||
                        DialogueSystem.IsPlaying || StoryProgress.StarsIn(1) != 0 ||
                        StoryProgress.TasksCompletedIn(1) != level)
                        _errors.Add("Native transition did not start after purchase: " + expected);
                    if (StoryRepair.Perform(1, level, null) || (level < 4 && StoryRepair.Perform(1, level + 1, null)))
                        _errors.Add("A second repair was accepted during " + expected);
                    if (_skipTransitions)
                    {
                        yield return new WaitForSecondsRealtime(.15f);
                        Tap("~StoryRepairCutscene", "Skip");
                        yield return new WaitForSecondsRealtime(.5f);
                        if (RepairCutscene.IsPlaying || GameObject.Find("GateTransition") != null)
                            _errors.Add("Native transition did not finish promptly after skip: " + expected);
                    }
                    else
                    {
                        yield return new WaitForSecondsRealtime(.7f);
                        yield return Shot(expected + "-working");
                        yield return new WaitForSecondsRealtime(.55f);
                        yield return Shot(expected + "-settling");
                    }
                }
                yield return Shot("repair-" + level);
                yield return Settle();
                if (GameObject.Find("GateTransition") != null)
                    _errors.Add("Native transition left its layers attached after completion");
                if (level == 4 && !DialogueSystem.IsPlaying)
                    _errors.Add("Final discovery missing after repair " + level);
                if (level < 4 && (!AreaScreen.IsOpen || !AreaScreen.Instance.PanelVisible ||
                    AreaScreen.Instance.RowOffersRepair(level - 1) || DialogueSystem.IsPlaying))
                    _errors.Add("Silent repair did not return to the completed task panel: " + level);
                yield return Dialogue(level == 4 ? "discovery" : "task-" + level + "-dialogue");
                yield return Settle();
                StoryProgress.Flush();
                StoryProgress.ReloadFromDisk();
                if (StoryProgress.TasksCompletedIn(1) != level || StoryProgress.StarsIn(1) != 0)
                    _errors.Add("Repair/save mismatch after task " + level);
            }
            if (_onlyTask > 0)
            {
                if (StoryProgress.TasksCompletedIn(1) != _onlyTask || StoryProgress.StarsIn(1) != 0 ||
                    StoryProgress.IsAreaUnlocked(2) || (_onlyTask == 4 && !StoryProgress.IsSceneSeen("A001-DISCOVERY")))
                    _errors.Add("Focused repair/save/discovery boundary mismatch");
                AreaScreen.Close();
                StoryHomeScreen.Open(1);
                yield return BothPhoneSizes("saved-state-" + _onlyTask);
                AreaScreen.Open(1);
                yield return BothPhoneSizes("completed-panel-" + _onlyTask);
                if (StorybookUi.Enabled && _onlyTask == 4)
                {
                    AreaScreen.Close();
                    StorybookCollection.Open();
                    yield return BothPhoneSizes("ui-earned-collection");
                    StorybookCollection.Close();
                }
                Finish(); yield break;
            }
            if (_vinesOnly)
            {
                if (StoryProgress.TasksCompletedIn(1) != 1 || StoryProgress.StarsIn(1) != 0 ||
                    StoryProgress.IsAreaUnlocked(2) || GameObject.Find("VineTransition") != null)
                    _errors.Add("Vine transition did not finish cleanly at one saved task and zero stars");
                AreaScreen.Close();
                StoryHomeScreen.Open(1);
                yield return BothPhoneSizes("vines-saved-state");
                AreaScreen.Open(1);
                yield return BothPhoneSizes("vines-completed-panel");
                Finish();
                yield break;
            }
            StoryHomeScreen.Open(1);
            yield return BothPhoneSizes("state-4-complete");
            // Exercise the small phone layout with actual rendering, then restore
            // the standard review window through its shared dimensions.
            Screen.SetResolution(320, 568, false);
            yield return new WaitForSecondsRealtime(1f);
            AreaScreen.Open(1);
            yield return Shot("small-phone-complete");
            AreaScreen.Close();
            TreasureHuntPresentation.Play("A001-DISCOVERY", 1, null, true);
            yield return Dialogue("small-phone-discovery");
            Screen.SetResolution(StoryWindow.Width, StoryWindow.Height, false);
            if (!StoryProgress.IsAreaComplete(1) || StoryProgress.IsAreaUnlocked(2))
                _errors.Add("Completed-area boundary incorrect");
            if (!StoryProgress.IsSceneSeen("OPEN-01") || !StoryProgress.IsSceneSeen("A001-DISCOVERY"))
                _errors.Add("Missing seen scene flags");
            if (_bookReview)
            {
                AreaScreen.Close();
                StorybookCollection.Open();
                yield return BothPhoneSizes("ui-earned-collection");
                StorybookCollection.Close();
            }
            Finish();
        }

        // Visual comparison from the checkpoint earned before repairing the
        // door. Buy it through the real task row, then win the actual third board.
        private IEnumerator FidelityScreens()
        {
            if (StoryProgress.TasksCompletedIn(1) == 1 && StoryProgress.StarsIn(1) == 1)
            {
                AreaScreen.Open(1);
                yield return new WaitForSecondsRealtime(.7f);
                AreaScreen.Instance.RepairButton(1).onClick.Invoke();
                yield return Settle();
                if (StoryProgress.TasksCompletedIn(1) != 2 || StoryProgress.StarsIn(1) != 0)
                    _errors.Add("Full-width task row failed its normal purchase");
                AreaScreen.Close();
            }
            int stars = StoryProgress.StarsIn(1), tasks = StoryProgress.TasksCompletedIn(1);
            StoryHomeScreen.Open(1);
            yield return BothPhoneSizes("home");
            AreaScreen.Open(1);
            yield return BothPhoneSizes("tasks");
            AreaScreen.Close();
            StoryFlow.LaunchLevel(3, 1);
            float deadline = Time.realtimeSinceStartup + 35;
            while (Time.realtimeSinceStartup < deadline &&
                (LevelManager.THIS == null || LevelManager.THIS.gameStatus != GameState.Playing))
                yield return new WaitForSecondsRealtime(.2f);
            if (LevelManager.THIS == null || LevelManager.THIS.gameStatus != GameState.Playing)
            { _errors.Add("Fidelity board did not load"); yield break; }
            yield return new WaitForSecondsRealtime(3.5f);
            yield return BothPhoneSizes("board");
            StoryLevelPopup.Open(3, 1, null);
            yield return BothPhoneSizes("before");
            StoryLevelPopup.Close();
            bool previouslyBeaten = StoryProgress.IsLevelBeaten(3);
            yield return StoryPlaytest.PlayForCampaignReview(3, 1);
            if (!StoryProgress.IsLevelBeaten(3)) _errors.Add("Fidelity review did not win board three");
            yield return new WaitForSecondsRealtime(1.3f);
            yield return BothPhoneSizes("victory");
            StoryWinPopup.Close();
            if (stars + (previouslyBeaten ? 0 : 1) != StoryProgress.StarsIn(1) || tasks != StoryProgress.TasksCompletedIn(1))
                _errors.Add("Fidelity review reward mismatch");
        }

        public static IEnumerator ReviewBoard(int level)
        {
            if (!_bookReview || _active == null) yield break;
            yield return _active.BothPhoneSizes("board-" + level);
            JellyGarden.Scripts.GUI.MenuManager.Instance.ShowMenuPause(true);
            yield return new WaitForSecondsRealtime(.9f);
            yield return _active.BothPhoneSizes("pause-" + level);
            Tap("MenuPause", "Continue");
            yield return new WaitForSecondsRealtime(.9f);
            if (LevelManager.THIS.gameStatus != GameState.Playing)
                _active._errors.Add("Pause did not resume board " + level);
        }

        private IEnumerator BookScreens()
        {
            _screensReviewed = true;
            int stars = StoryProgress.StarsIn(1), tasks = StoryProgress.TasksCompletedIn(1);
            AreaScreen.Close();
            StoryHomeScreen.Open(1);
            yield return BothPhoneSizes("ui-home");
            StorySettings.Open(null);
            yield return BothPhoneSizes("ui-settings");
            StorySettings.Close();
            StoryNavBar.Sync();
            StoryNavBar.Instance.ButtonFor(StoryNavBar.Tab.Areas).onClick.Invoke();
            yield return BothPhoneSizes("ui-map");
            StoryNavBar.Instance.ButtonFor(StoryNavBar.Tab.Card).onClick.Invoke();
            yield return BothPhoneSizes("ui-collection");
            StoryNavBar.Instance.ButtonFor(StoryNavBar.Tab.Home).onClick.Invoke();
            for (int level = 1; level <= 4; level++)
            {
                StoryLevelPopup.Open(level, 1, () => StoryHomeScreen.Open(1));
                yield return BothPhoneSizes("ui-before-level-" + level);
                StoryLevelPopup.Instance.CloseButton.onClick.Invoke();
            }
            StoryLosePopup.Open(1, 1, false, null);
            yield return BothPhoneSizes("ui-out-of-moves");
            StoryLosePopup.Close();
            StoryWinPopup.Open(1, 1, false, null);
            yield return new WaitForSecondsRealtime(1);
            yield return BothPhoneSizes("ui-replay-victory");
            StoryWinPopup.Close();
            if (stars != StoryProgress.StarsIn(1) || tasks != StoryProgress.TasksCompletedIn(1))
                _errors.Add("Visiting storybook screens changed progress");
        }

        private static IEnumerator Settle()
        {
            float until = Time.realtimeSinceStartup + 25f;
            while (Time.realtimeSinceStartup < until && RepairCutscene.IsPlaying)
                yield return new WaitForSecondsRealtime(0.2f);
            yield return new WaitForSecondsRealtime(0.5f);
        }
        private IEnumerator Dialogue(string name)
        {
            int line = 0;
            while (DialogueSystem.IsPlaying && line < 35)
            {
                if (DialogueSystem.IsTyping) Tap("~StoryDialogue", "Advance");
                yield return new WaitForSecondsRealtime(0.3f);
                if (name.StartsWith("small-phone-"))
                    yield return Shot(name + "-" + line);
                else
                    yield return BothPhoneSizes(name + "-" + line);
                Tap("~StoryDialogue", "Advance");
                yield return new WaitForSecondsRealtime(0.2f);
                line++;
            }
            if ((name == "opening" || name == "discovery") && line != 2)
                _errors.Add("Expected two dialogue cards in " + name + ", got " + line);
        }
        private static void Tap(string hostName, string buttonName)
        {
            var host = GameObject.Find(hostName);
            if (host == null) return;
            foreach (Button button in host.GetComponentsInChildren<Button>(true))
                if (button.name == buttonName && button.gameObject.activeInHierarchy && button.interactable)
                { button.onClick.Invoke(); return; }
        }
        private IEnumerator Shot(string name)
        {
            yield return new WaitForSecondsRealtime(0.5f);
            yield return new WaitForEndOfFrame();
            if (DialogueSystem.IsPlaying && DialogueSystem.LineOverflow > 1f)
                _errors.Add("Dialogue text overflow at " + Screen.width + "x" + Screen.height + ": " + name);
            StoryWindow.Capture(Path.Combine(_directory, name + ".png"));
        }
        private IEnumerator BothPhoneSizes(string name)
        {
            yield return Shot(name);
            Screen.SetResolution(320, 568, false);
            yield return new WaitForSecondsRealtime(0.5f);
            yield return Shot("small-phone-" + name);
            Screen.SetResolution(StoryWindow.Width, StoryWindow.Height, false);
            yield return new WaitForSecondsRealtime(0.5f);
        }
        private void Finish()
        {
            if (_finished) return;
            _finished = true;
            var report = new Report { passed = _errors.Count == 0, wins = StoryProgress.LevelsBeatenCount,
                tasks = StoryProgress.TasksCompletedIn(1), stars = StoryProgress.StarsIn(1),
                startingTasks = _startingTasks,
                area2Locked = !StoryProgress.IsAreaUnlocked(2), errors = _errors.ToArray() };
            File.WriteAllText(Path.Combine(_directory, "runtime-check.json"), JsonUtility.ToJson(report, true));
            Application.Quit(report.passed ? 0 : 1);
        }
        [Serializable] private sealed class Report
        {
            public bool passed, area2Locked;
            public int wins, tasks, stars, startingTasks;
            public string[] errors;
        }
    }
}
