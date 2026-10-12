#if UNITY_EDITOR && !ROYAL_DISABLE_QA
// Runs Royal Throne's Play-mode check suites under King Leo (plan Task 4.0).
//
// Every suite gets its own item under King Leo > Port checks, plus Run all.
// Each item opens the menu scene, enters Play mode, loads the game scene and
// runs the suites one after another, each through RoyalRegressionRunner:
//
//   - Board suites (RoyalOverlapChecks and friends) run inside
//     RoyalQaService.StartSession / EndSession on the QA practice board, an 8x8
//     moves-based board built over level 1. EndSession runs in a finally.
//   - The RoyalEffectsChecks drivers run on the same practice board inside a
//     session, with the QA service's Update paused, because that Update forces
//     Time.timeScale every frame and the effects suite tests pausing.
//   - Suites that drive their own QA session (RoyalQaChecks, the booster V4/V5
//     suites, the dual-combo suites) are started from the normal profile, as
//     their menu items were in Royal Throne; their own file gives the verdict.
//
// Results: Logs/port-checks/report.txt (one 'PASS|FAIL|TIMEOUT|ERROR <suite>
// <detail>' line per suite at column 0, the suite's own check lines indented
// under it, and a final 'SUMMARY:' line) and Logs/port-checks/<Suite>.txt.
//
// Headless: PortChecksRunner.RunHeadless (see that file).
// Open Editor without a menu click: write suite names (comma or line
// separated, or 'all') to Temp/port-check-command.txt; the report is copied to
// Temp/port-check-response.txt when the run ends.
//
// The game's story save is copied to Temp/port-checks/save and the run plays
// against the copy (StoryProgress.SaveDirectoryOverride), so a QA 'Win
// instantly' never writes the player's real campaign save.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using RoyalThrone.Editor;
using RoyalThrone.Testing;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace JellyGarden.Scripts.Story.Editor
{
    public static class PortChecks
    {
        #region Suites

        internal enum SuiteKind
        {
            /// <summary>Enumerator suite run on the practice board inside a QA session.</summary>
            Session,
            /// <summary>A RoyalEffectsChecks driver run on the practice board inside a QA session.</summary>
            EffectsBoard,
            /// <summary>A suite that opens and closes its own QA session.</summary>
            SelfSession,
        }

        internal sealed class Suite
        {
            public string Name;
            public SuiteKind Kind;
            public double Timeout = DefaultTimeout;
            /// <summary>False for suites that read Game-view pixels; they run only in an open Editor.</summary>
            public bool Headless = true;
            public string Note;
            public Func<LevelManager, Action<bool, string>, IEnumerator> Body;
            public Action Entry;
            public Func<string> ResultFile;
            public Func<string[], bool> Passed;
        }

        public const double DefaultTimeout = 240;

        private static List<Suite> _suites;

        private static List<Suite> Suites
        {
            get { return _suites ?? (_suites = BuildSuites()); }
        }

        private static List<Suite> BuildSuites()
        {
            var list = new List<Suite>();

            void Board(string name, Action entry, double timeout, bool headless, string note)
            {
                list.Add(new Suite { Name = name, Kind = SuiteKind.EffectsBoard, Entry = entry, Timeout = timeout, Headless = headless, Note = note });
            }

            void InQa(string name, Func<LevelManager, Action<bool, string>, IEnumerator> body, double timeout, string note)
            {
                list.Add(new Suite { Name = name, Kind = SuiteKind.Session, Body = body, Timeout = timeout, Note = note });
            }

            void Own(string name, Action entry, Func<string> file, Func<string[], bool> passed, double timeout, string note)
            {
                list.Add(new Suite { Name = name, Kind = SuiteKind.SelfSession, Entry = entry, ResultFile = file, Passed = passed, Timeout = timeout, Note = note });
            }

            Board("Effects", RoyalEffectsChecks.Run, 600, true,
                "RoyalEffectsChecks board regression on its workshop board (art, surface, presentation, toolkit, input, combos, hint checks)");
            Board("EffectsInput", RoyalEffectsChecks.RunInput, DefaultTimeout, true, "RoyalEffectsChecks: board input only");
            Board("EffectsCellBreaks", RoyalEffectsChecks.RunCellBreaks, 300, true, "RoyalEffectsChecks: restored match breaks (RoyalCellBreakChecks)");
            Board("EffectsColorBall", RoyalEffectsChecks.RunColorBall, 300, true, "RoyalEffectsChecks: live Color Ball + Rocket conversion (RoyalColorBallChecks)");
            Board("EffectsRockets", RoyalEffectsChecks.RunRockets, 600, false,
                "RoyalEffectsChecks: rocket travel (RoyalRocketChecks reads Game-view pixels, so open Editor only)");
            Board("EffectsTnt", RoyalEffectsChecks.RunTnt, 600, false,
                "RoyalEffectsChecks: TNT at quarter and full speed (RoyalTNTChecks is a screen-capture review, so open Editor only)");

            InQa("HintPresentation", HintOnPracticeBoard, DefaultTimeout, "RoyalHintPresentationChecks on the practice board");
            InQa("Overlap", (lm, check) => RoyalOverlapChecks.Run(lm, check), 480, "RoyalOverlapChecks");
            InQa("BlockedRefill", (lm, check) => RoyalBlockedRefillChecks.Run(check), 420, "RoyalBlockedRefillChecks");
            InQa("MatchResponse", (lm, check) => RoyalMatchResponseChecks.Run(lm, check, true), DefaultTimeout, "RoyalMatchResponseChecks (immediate)");
            InQa("FormationReference", (lm, check) => RoyalFormationReferenceChecks.Run(lm, check, true), DefaultTimeout,
                "RoyalFormationReferenceChecks (reference timing)");
            InQa("SpecialFormation", (lm, check) => SpecialFormations(check), 360, "RoyalSpecialFormationChecks: TNT-L, TNT-T, Rocket-H, Rocket-V, ColorBall");
            InQa("PropellerFormation", (lm, check) => RoyalPropellerFormationChecks.Run(lm, check), DefaultTimeout, "RoyalPropellerFormationChecks");
            InQa("SquarePlusOne", (lm, check) => RoyalSquarePlusOneChecks.Run(check), 480, "RoyalSquarePlusOneChecks");
            InQa("SuperBombFuse", (lm, check) => RoyalSuperBombFuseChecks.Run(lm, check), DefaultTimeout, "RoyalSuperBombFuseChecks");

            Own("ColorBallQa", RoyalColorBallQaChecks.Run,
                StaticPath(typeof(RoyalColorBallQaChecks), "Folder", "checks.txt", "Logs/port-checks/Effects/ColorBall-live-v5/checks.txt"),
                Line("ALL COLOR BALL QA PATHS PASSED"), 300, "RoyalColorBallQaChecks");
            Own("DualColorBall", RoyalDualColorBallChecks.Run,
                StaticPath(typeof(RoyalDualColorBallChecks), "Folder", "checks.txt", "Logs/port-checks/DualColorBall-Reference/Verification/checks.txt"),
                Line("ALL DUAL COLORBALL CHECKS PASSED"), 300, "RoyalDualColorBallChecks");
            Own("AllDualCombo", RoyalAllDualComboChecks.Run,
                StaticPath(typeof(RoyalAllDualComboChecks), "Folder", "checks.txt", "Logs/port-checks/DualCombo-Reference/Verification/checks.txt"),
                Line("ALL DUAL COMBO CHECKS PASSED"), 900, "RoyalAllDualComboChecks");
            Own("PropellerV5", Hidden(typeof(global::RoyalThrone.Boosters.Propeller.V5.Editor.PropellerV5Checks), "Run"),
                StaticPath(typeof(global::RoyalThrone.Boosters.Propeller.V5.Editor.PropellerV5Checks), "Root", "checks.txt", "Logs/port-checks/PropellerV5/checks.txt"),
                Line("ALL PROPELLER V5 CHECKS PASSED"), 420, "PropellerV5Checks");
            Own("PropellerReference", global::RoyalThrone.Boosters.Propeller.V5.Editor.PropellerReferenceChecks.Run,
                StaticPath(typeof(global::RoyalThrone.Boosters.Propeller.V5.Editor.PropellerReferenceChecks), "Output", null,
                    "Logs/port-checks/PropellerReference/Runtime-checks.txt"),
                lines => lines.Length > 0 && lines[0].StartsWith("PASS ", StringComparison.Ordinal), 300, "PropellerReferenceChecks");
            Own("ColorBallV4", Hidden(typeof(global::RoyalThrone.Boosters.ColorBall.V4.Editor.ColorBallV4Checks), "Run"),
                StaticPath(typeof(global::RoyalThrone.Boosters.ColorBall.V4.Editor.ColorBallV4Checks), "Root", "checks.txt", "Logs/port-checks/ColorBallV4/checks.txt"),
                Line("ALL COLOR BALL V4 CHECKS PASSED"), 420, "ColorBallV4Checks");
            // 'Full' is the combo and frame-rate run. The proof and single-sequence
            // items leave Play mode when they pass, which would end the whole run.
            Own("ColorBallV5", Hidden(typeof(global::RoyalThrone.Boosters.ColorBall.V5.Editor.ColorBallV5Checks), "Full"),
                StaticPath(typeof(global::RoyalThrone.Boosters.ColorBall.V5.Editor.ColorBallV5Checks), "Root", "checks.txt", null),
                Line("PASS"), 480, "ColorBallV5Checks (combo and actual frame rate)");
            // Last: its 'Win instantly' goes through the real win flow and can
            // leave a story beat on the home screen.
            Own("QaChecks", RoyalQaChecks.Run,
                StaticPath(typeof(RoyalQaChecks), "Folder", "checks.txt", "Logs/port-checks/QA-Mode/PlayMode/checks.txt"),
                Line("ALL QA CONTROLS PASSED"), 600, "RoyalQaChecks (every QA control, isolation audit)");

            return list;
        }

        private static readonly (string shape, ItemsTypes type)[] FormationShapes =
        {
            ("TNT-L", ItemsTypes.PACKAGE),
            ("TNT-T", ItemsTypes.PACKAGE),
            ("Rocket-H", ItemsTypes.HORIZONTAL_STRIPPED),
            ("Rocket-V", ItemsTypes.VERTICAL_STRIPPED),
            ("ColorBall", ItemsTypes.BOMB),
        };

        private static IEnumerator SpecialFormations(Action<bool, string> check)
        {
            var qa = RoyalQaService.Instance;
            foreach (var formation in FormationShapes)
            {
                Stage("special formation " + formation.shape);
                // The suite's 30 FPS pass reuses whatever board it is given.
                yield return qa.Practice();
                yield return RoyalSpecialFormationChecks.Run(LevelManager.THIS, check, formation.type, formation.shape);
            }
        }

        /// <summary>
        /// RoyalHintPresentationChecks refuses any board but RoyalEffectsChecks'
        /// disposable workshop board, by name. The QA practice board is the same
        /// kind of board (8x8, disposable, colours (col + 2 row) % 5), so it is
        /// named as one for the length of the suite.
        /// </summary>
        private static IEnumerator HintOnPracticeBoard(LevelManager lm, Action<bool, string> check)
        {
            lm.StopAllCoroutines();
            AI.THIS.StopAllCoroutines();
            AI.THIS.CancelVisibleHint();
            AI.THIS.allowShowTip = false;
            Transform field = lm.GameField;
            string name = field.name;
            field.name = "Royal workshop test board (port checks: QA practice board)";
            try
            {
                yield return RoyalHintPresentationChecks.Run(lm, check);
            }
            finally
            {
                if (field) field.name = name;
            }
        }

        private static Func<string[], bool> Line(string marker)
        {
            return lines => lines.Any(l => l.Trim() == marker);
        }

        /// <summary>A results path built from a suite's own folder field, read when it is needed.</summary>
        private static Func<string> StaticPath(Type type, string field, string file, string fallback)
        {
            return () =>
            {
                string folder = null;
                try
                {
                    var info = type.GetField(field, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                    folder = info == null ? null : info.GetValue(null) as string;
                }
                catch (Exception)
                {
                    folder = null;
                }
                if (string.IsNullOrEmpty(folder)) return fallback;
                return file == null ? folder : folder + file;
            };
        }

        /// <summary>Several booster suites keep their entry point private behind a menu item.</summary>
        private static Action Hidden(Type type, string method)
        {
            return () =>
            {
                var info = type.GetMethod(method, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public, null, Type.EmptyTypes, null);
                if (info == null) throw new MissingMethodException(type.FullName, method);
                try
                {
                    info.Invoke(null, null);
                }
                catch (TargetInvocationException e)
                {
                    var inner = e.InnerException ?? e;
                    throw new InvalidOperationException(inner.GetType().Name + ": " + inner.Message, inner);
                }
            };
        }

        public static IList<string> AllSuiteNames
        {
            get { return Suites.Select(s => s.Name).ToList(); }
        }

        /// <summary>The suites that can run in batchmode: everything that does not read Game-view pixels.</summary>
        public static IList<string> HeadlessSuiteNames
        {
            get { return Suites.Where(s => s.Headless).Select(s => s.Name).ToList(); }
        }

        private static Suite Find(string name)
        {
            return Suites.FirstOrDefault(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        #endregion

        #region Menu

        private const string Menu = "King Leo/Port checks/";

        [MenuItem(Menu + "Run all", false, 1)]
        private static void MenuRunAll() { FromMenu(AllSuiteNames); }

        [MenuItem(Menu + "Run headless set", false, 2)]
        private static void MenuRunHeadlessSet() { FromMenu(HeadlessSuiteNames); }

        [MenuItem(Menu + "Open report", false, 3)]
        private static void MenuOpenReport()
        {
            if (File.Exists(ReportPath)) EditorUtility.OpenWithDefaultApp(ReportPath);
            else Debug.Log(Tag + "no report yet at " + ReportPath);
        }

        [MenuItem(Menu + "Stop the current run", false, 4)]
        private static void MenuStop()
        {
            if (!Armed)
            {
                Debug.Log(Tag + "no run in progress.");
                return;
            }
            if (_active != null) Timeout(_active, "stopped from the menu");
            Finish("stopped from the menu");
        }

        [MenuItem(Menu + "Effects", false, 20)] private static void MenuEffects() { FromMenu("Effects"); }
        [MenuItem(Menu + "EffectsInput", false, 21)] private static void MenuEffectsInput() { FromMenu("EffectsInput"); }
        [MenuItem(Menu + "EffectsCellBreaks", false, 22)] private static void MenuEffectsCellBreaks() { FromMenu("EffectsCellBreaks"); }
        [MenuItem(Menu + "EffectsColorBall", false, 23)] private static void MenuEffectsColorBall() { FromMenu("EffectsColorBall"); }
        [MenuItem(Menu + "EffectsRockets", false, 24)] private static void MenuEffectsRockets() { FromMenu("EffectsRockets"); }
        [MenuItem(Menu + "EffectsTnt", false, 25)] private static void MenuEffectsTnt() { FromMenu("EffectsTnt"); }
        [MenuItem(Menu + "HintPresentation", false, 40)] private static void MenuHint() { FromMenu("HintPresentation"); }
        [MenuItem(Menu + "Overlap", false, 41)] private static void MenuOverlap() { FromMenu("Overlap"); }
        [MenuItem(Menu + "BlockedRefill", false, 42)] private static void MenuBlockedRefill() { FromMenu("BlockedRefill"); }
        [MenuItem(Menu + "MatchResponse", false, 43)] private static void MenuMatchResponse() { FromMenu("MatchResponse"); }
        [MenuItem(Menu + "FormationReference", false, 44)] private static void MenuFormationReference() { FromMenu("FormationReference"); }
        [MenuItem(Menu + "SpecialFormation", false, 45)] private static void MenuSpecialFormation() { FromMenu("SpecialFormation"); }
        [MenuItem(Menu + "PropellerFormation", false, 46)] private static void MenuPropellerFormation() { FromMenu("PropellerFormation"); }
        [MenuItem(Menu + "SquarePlusOne", false, 47)] private static void MenuSquarePlusOne() { FromMenu("SquarePlusOne"); }
        [MenuItem(Menu + "SuperBombFuse", false, 48)] private static void MenuSuperBombFuse() { FromMenu("SuperBombFuse"); }
        [MenuItem(Menu + "ColorBallQa", false, 60)] private static void MenuColorBallQa() { FromMenu("ColorBallQa"); }
        [MenuItem(Menu + "DualColorBall", false, 61)] private static void MenuDualColorBall() { FromMenu("DualColorBall"); }
        [MenuItem(Menu + "AllDualCombo", false, 62)] private static void MenuAllDualCombo() { FromMenu("AllDualCombo"); }
        [MenuItem(Menu + "PropellerV5", false, 63)] private static void MenuPropellerV5() { FromMenu("PropellerV5"); }
        [MenuItem(Menu + "PropellerReference", false, 64)] private static void MenuPropellerReference() { FromMenu("PropellerReference"); }
        [MenuItem(Menu + "ColorBallV4", false, 65)] private static void MenuColorBallV4() { FromMenu("ColorBallV4"); }
        [MenuItem(Menu + "ColorBallV5", false, 66)] private static void MenuColorBallV5() { FromMenu("ColorBallV5"); }
        [MenuItem(Menu + "QaChecks", false, 67)] private static void MenuQaChecks() { FromMenu("QaChecks"); }

        private static void FromMenu(string name) { FromMenu(new[] { name }); }

        private static void FromMenu(IList<string> names)
        {
            if (Armed)
            {
                Debug.LogWarning(Tag + "a run is already in progress; wait for its SUMMARY line.");
                return;
            }
            Start(names, false, 0, 0, null);
        }

        #endregion

        #region Paths and persisted state

        private const string Tag = "[Port checks] ";
        private const string MainScene = "Assets/JellyGarden/Scenes/main.unity";
        private const string GameScene = "game";
        private const string ResultsFolder = "Logs/port-checks";
        private const string SaveCopyFolder = "Temp/port-checks/save";
        private const string CommandFile = "Temp/port-check-command.txt";
        private const string ResponseFile = "Temp/port-check-response.txt";

        public static string ReportPath
        {
            get { return Path.Combine(Path.GetFullPath(ResultsFolder), "report.txt"); }
        }

        // SessionState survives the domain reload that entering Play mode
        // causes, and, unlike EditorPrefs, never outlives the Editor process:
        // a crashed run cannot re-arm itself the next time the project opens.
        private const string KArmed = "PortChecks.Armed";
        private const string KQueue = "PortChecks.Queue";
        private const string KHeadless = "PortChecks.Headless";
        private const string KDeadline = "PortChecks.Deadline";
        private const string KTimeout = "PortChecks.TimeoutOverride";
        private const string KResponse = "PortChecks.Response";
        private const string KNeedEnter = "PortChecks.NeedEnter";
        private const string KSaveCopied = "PortChecks.SaveCopied";
        private const string KBridge = "PortChecks.Bridge";

        private static bool Armed
        {
            get { return SessionState.GetBool(KArmed, false); }
        }

        private static bool Headless
        {
            get { return SessionState.GetBool(KHeadless, false); }
        }

        internal static DateTime GlobalDeadlineUtc
        {
            get
            {
                long ticks;
                return long.TryParse(SessionState.GetString(KDeadline, ""), out ticks) ? new DateTime(ticks, DateTimeKind.Utc) : DateTime.MaxValue;
            }
        }

        /// <summary>The watchdog thread acts two minutes after the run's own budget.</summary>
        private static DateTime WatchdogDeadlineUtc
        {
            get
            {
                var deadline = GlobalDeadlineUtc;
                return deadline > DateTime.UtcNow.AddYears(1) ? deadline : deadline.AddSeconds(120);
            }
        }

        private static List<string> Queue
        {
            get
            {
                string text = SessionState.GetString(KQueue, "");
                return text.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            }
            set { SessionState.SetString(KQueue, string.Join(",", value.ToArray())); }
        }

        /// <summary>Set by the watchdog thread's reader; true once the SUMMARY line is written.</summary>
        internal static volatile bool Finished;

        #endregion

        #region Starting a run

        /// <summary>
        /// Arms a run and enters Play mode. <paramref name="headless"/> exits the
        /// Editor with the verdict when the run ends; a human's run leaves Play
        /// mode instead.
        /// </summary>
        internal static void Start(IList<string> requested, bool headless, double timeoutOverride, double budgetOverride, string response)
        {
            if (Armed)
            {
                Debug.LogWarning(Tag + "a run is already in progress.");
                return;
            }

            Directory.CreateDirectory(Path.GetFullPath(ResultsFolder));
            Finished = false;

            var names = new List<string>();
            var unknown = new List<string>();
            foreach (string raw in requested)
            {
                string name = (raw ?? "").Trim();
                if (name.Length == 0) continue;
                var suite = Find(name);
                if (suite == null) unknown.Add(name);
                else if (!names.Contains(suite.Name)) names.Add(suite.Name);
            }

            var header = new StringBuilder();
            header.Append("# King Leo port checks (Royal Throne Play-mode suites), ")
                .Append(DateTime.UtcNow.ToString("u")).Append('\n');
            header.Append("# mode: ").Append(headless ? "batchmode" : "editor")
                .Append("; suites: ").Append(names.Count == 0 ? "(none)" : string.Join(", ", names.ToArray())).Append('\n');
            header.Append("# verdict lines start at column 0; each suite's own check lines are indented under its '## <suite>' heading.\n");
            File.WriteAllText(ReportPath, header.ToString());
            foreach (string name in names)
            {
                string own = SuiteFile(name);
                if (File.Exists(own)) File.Delete(own);
            }
            foreach (string name in unknown)
                Report("ERROR " + name + " unknown suite; known: " + string.Join(", ", AllSuiteNames.ToArray()));

            double budget = budgetOverride > 0 ? budgetOverride : 300;
            if (budgetOverride <= 0)
                foreach (string name in names)
                    budget += (timeoutOverride > 0 ? timeoutOverride : Find(name).Timeout) + 90;

            SessionState.SetBool(KArmed, true);
            SessionState.SetBool(KHeadless, headless);
            SessionState.SetString(KDeadline, DateTime.UtcNow.AddSeconds(budget).Ticks.ToString());
            SessionState.SetFloat(KTimeout, (float)timeoutOverride);
            SessionState.SetString(KResponse, response ?? "");
            SessionState.SetBool(KBridge, response != null);
            SessionState.SetBool(KSaveCopied, false);
            Queue = names;

            Debug.Log(Tag + "run armed: " + (names.Count == 0 ? "no suites" : string.Join(", ", names.ToArray())) +
                      "; budget " + budget.ToString("0") + " s; report " + ReportPath);

            if (names.Count == 0)
            {
                Finish(null);
                return;
            }

            if (headless) PortChecksRunner.StartWatchdog(WatchdogDeadlineUtc, ReportPath);
            Hook();
            _phase = Phase.WaitPlay;
            _phaseSince = EditorApplication.timeSinceStartup;

            if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                // Each run starts from a fresh Play mode: the save copy has to be
                // in place before the game's first Awake, and that is only
                // possible on the domain reload that entering Play mode causes.
                SessionState.SetBool(KNeedEnter, true);
                EditorApplication.ExitPlaymode();
            }
            else
            {
                EnterNow();
            }
        }

        private static void EnterNow()
        {
            SessionState.SetBool(KNeedEnter, false);
            if (!Headless)
            {
                if (SessionState.GetBool(KBridge, false))
                {
                    for (int i = 0; i < SceneManager.sceneCount; i++)
                    {
                        if (!SceneManager.GetSceneAt(i).isDirty) continue;
                        Finish("the open scene has unsaved changes; save or discard them before a bridge run");
                        return;
                    }
                }
                else if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    Finish("cancelled: the open scene's changes were not saved");
                    return;
                }
            }

            EditorSceneManager.OpenScene(MainScene, OpenSceneMode.Single);
            IsolateStorySave();
            _phase = Phase.WaitPlay;
            _phaseSince = EditorApplication.timeSinceStartup;
            Debug.Log(Tag + "opening the menu scene and entering Play mode.");
            EditorApplication.EnterPlaymode();
        }

        /// <summary>Picked up again after the domain reload that entering Play mode causes.</summary>
        [InitializeOnLoadMethod]
        private static void Resume()
        {
            if (!Application.isBatchMode)
            {
                EditorApplication.update -= WatchBridge;
                EditorApplication.update += WatchBridge;
            }
            if (!Armed) return;

            IsolateStorySave();
            Hook();
            _phase = Phase.WaitPlay;
            _phaseSince = EditorApplication.timeSinceStartup;
            if (Headless) PortChecksRunner.StartWatchdog(WatchdogDeadlineUtc, ReportPath);
        }

        /// <summary>Plays the run against a copy of the story save; the player's own file is never written.</summary>
        private static void IsolateStorySave()
        {
            try
            {
                string copy = Path.GetFullPath(SaveCopyFolder);
                if (!SessionState.GetBool(KSaveCopied, false))
                {
                    if (Directory.Exists(copy)) Directory.Delete(copy, true);
                    Directory.CreateDirectory(copy);
                    string source = StoryIdentity.SaveDirectory;
                    if (Directory.Exists(source))
                        foreach (string file in Directory.GetFiles(source, "*.json"))
                            File.Copy(file, Path.Combine(copy, Path.GetFileName(file)), true);
                    SessionState.SetBool(KSaveCopied, true);
                }
                Directory.CreateDirectory(copy);
                StoryProgress.SaveDirectoryOverride = copy;
            }
            catch (Exception e)
            {
                Debug.LogWarning(Tag + "could not isolate the story save: " + e.Message);
            }
        }

        private static void Hook()
        {
            EditorApplication.update -= Poll;
            EditorApplication.update += Poll;
            Application.logMessageReceived -= OnLog;
            Application.logMessageReceived += OnLog;
        }

        private static void Unhook()
        {
            EditorApplication.update -= Poll;
            Application.logMessageReceived -= OnLog;
        }

        #endregion

        #region The run

        private enum Phase { WaitPlay, Boot, Between, Running }

        private sealed class Active
        {
            public Suite Suite;
            public double Started;
            public double Deadline;
            public DateTime StartedUtc;
            public string Stage = "starting";
            public bool WrapperDone;
            public bool BodyDone;
            public int Checks;
            public int Failed;
            public string FirstFail;
            public string LastCheck;
            public int ErrorLogs;
            public string FirstError;
            public string LastException;
            public readonly List<string> Lines = new List<string>();
            public readonly List<string> ErrorSamples = new List<string>();
        }

        private static Phase _phase = Phase.WaitPlay;
        private static double _phaseSince;
        private static double _lastTick;
        private static double _lastPress;
        private static bool _gameRequested;
        private static double _readyAt = -1;
        private static bool _recoveryStarted;
        private static Active _active;
        private static int _frameRate;
        private static int _vSync;

        private static void Poll()
        {
            if (!Armed)
            {
                Unhook();
                return;
            }

            PortChecksRunner.Heartbeat();
            double now = EditorApplication.timeSinceStartup;
            if (now - _lastTick < 0.1) return;
            _lastTick = now;

            if (DateTime.UtcNow > GlobalDeadlineUtc)
            {
                if (_active != null) Timeout(_active, "the run's global budget ran out");
                Finish("global budget exhausted before the queue finished");
                return;
            }

            try
            {
                switch (_phase)
                {
                    case Phase.WaitPlay: WaitForPlay(now); break;
                    case Phase.Boot: Boot(now); break;
                    case Phase.Between: Between(now); break;
                    case Phase.Running: Running(now); break;
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                if (_active != null) Verdict(_active, "ERROR", "runner fault: " + OneLine(e.GetType().Name + ": " + e.Message), null);
                _active = null;
                Finish("runner fault: " + OneLine(e.Message));
            }
        }

        private static void Enter(Phase phase)
        {
            _phase = phase;
            _phaseSince = EditorApplication.timeSinceStartup;
        }

        private static void WaitForPlay(double now)
        {
            if (SessionState.GetBool(KNeedEnter, false))
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode) EnterNow();
                return;
            }
            if (EditorApplication.isPlaying)
            {
                _gameRequested = false;
                _readyAt = -1;
                Enter(Phase.Boot);
                return;
            }
            if (now - _phaseSince > 300) Finish("Play mode never started (compile errors, or a modal dialog in the way)");
        }

        private static void Boot(double now)
        {
            if (!EditorApplication.isPlaying)
            {
                Finish("Play mode ended while the game was loading");
                return;
            }
            if (now - _phaseSince < 2) return;

            // The menu scene waits on its Play button (TreasureHuntEntry); load
            // the board's scene the way the button does.
            if (LevelManager.THIS == null && !_gameRequested && SceneManager.GetActiveScene().name != GameScene)
            {
                _gameRequested = true;
                Debug.Log(Tag + "loading the game scene.");
                SceneManager.LoadScene(GameScene);
                return;
            }

            PressThroughDialogue(now);
            var qa = RoyalQaService.Instance;
            if (qa && qa.Ready)
            {
                if (_readyAt < 0) _readyAt = now;
                if (now - _readyAt < 1.5) return;

                // A runner left over from an earlier run in this Editor would be
                // counted as a live suite.
                foreach (var runner in Runners()) Object.DestroyImmediate(runner.gameObject);
                _frameRate = Application.targetFrameRate;
                _vSync = QualitySettings.vSyncCount;
                Time.captureDeltaTime = 0;
                Debug.Log(Tag + "game ready; starting the suites.");
                Enter(Phase.Between);
                return;
            }

            if (now - _phaseSince > 120)
            {
                Finish("the game scene never became ready (LevelManager " + (LevelManager.THIS != null) +
                       ", InitScript " + (InitScript.Instance != null) + ", MenuManager " + (global::JellyGarden.Scripts.GUI.MenuManager.Instance != null) +
                       ", QA service " + (qa != null) + ")");
            }
        }

        private static void Between(double now)
        {
            if (!EditorApplication.isPlaying)
            {
                Finish("Play mode ended between suites");
                return;
            }
            PressThroughDialogue(now);

            string busy = NotCalm();
            if (busy != null)
            {
                // A suite that failed or timed out may still be restoring the
                // player profile. Give it time, then end the session ourselves.
                var qa = RoyalQaService.Instance;
                if (!_recoveryStarted && now - _phaseSince > 30 && qa && Runners().Count == 0 && (RoyalSave.Testing || qa.Busy))
                {
                    _recoveryStarted = true;
                    Debug.LogWarning(Tag + "the last suite left the QA service " + busy + "; restoring it.");
                    if (qa.Busy)
                    {
                        // A command that never returns keeps Busy set, and EndSession waits on Busy.
                        qa.StopAllCoroutines();
                        SetProperty(qa, "Busy", false);
                    }
                    SetField(qa, "endingSession", false);
                    if (RoyalSave.Testing) RoyalRegressionRunner.Run(qa.EndSession());
                }
                if (now - _phaseSince > 90)
                {
                    foreach (string left in Queue)
                        Report("ERROR " + left + " not run: the previous suite left the game unusable (" + busy + ")");
                    Queue = new List<string>();
                    Finish("could not restore a clean QA state between suites (" + busy + ")");
                }
                return;
            }
            _recoveryStarted = false;

            var queue = Queue;
            if (queue.Count == 0)
            {
                Finish(null);
                return;
            }
            string next = queue[0];
            queue.RemoveAt(0);
            Queue = queue;
            Launch(Find(next), now);
        }

        private static string NotCalm()
        {
            var qa = RoyalQaService.Instance;
            if (!qa) return "no QA service";
            if (!qa.Ready) return "QA service not ready";
            int runners = Runners().Count;
            if (runners > 0) return runners + " regression runner(s) still active";
            if (RoyalSave.Testing) return "QA session still open";
            if (qa.Busy) return "QA command still running";
            return null;
        }

        private static void Launch(Suite suite, double now)
        {
            var a = new Active
            {
                Suite = suite,
                Started = now,
                StartedUtc = DateTime.UtcNow,
            };
            float overrideTimeout = SessionState.GetFloat(KTimeout, 0);
            a.Deadline = now + (overrideTimeout > 0 ? overrideTimeout : suite.Timeout);

            Report("## " + suite.Name + " (" + suite.Note + ")");
            Debug.Log(Tag + "---- " + suite.Name + " ----");

            if (Headless && !suite.Headless)
            {
                Verdict(a, "ERROR", "needs the Game view (screen captures / pixel reads); run it from King Leo > Port checks in an open Editor", null);
                Enter(Phase.Between);
                return;
            }

            Time.timeScale = 1;
            Time.captureDeltaTime = 0;
            if (suite.Kind == SuiteKind.SelfSession)
            {
                string file = SafePath(suite.ResultFile);
                if (file != null && File.Exists(file)) File.Delete(file);
            }

            _active = a;
            Enter(Phase.Running);
            RoyalRegressionRunner.Run(suite.Kind == SuiteKind.SelfSession ? SelfDriven(a) : InSession(a));
        }

        private static void Running(double now)
        {
            var a = _active;
            if (a == null)
            {
                Enter(Phase.Between);
                return;
            }
            if (!EditorApplication.isPlaying)
            {
                Verdict(a, "ERROR", "Play mode ended during the suite (stage '" + a.Stage + "')", null);
                _active = null;
                Finish("Play mode ended during " + a.Suite.Name);
                return;
            }
            if (a.WrapperDone)
            {
                Complete(a);
                _active = null;
                RestoreGlobals();
                Enter(Phase.Between);
                return;
            }
            if (now > a.Deadline)
            {
                Timeout(a, "exceeded " + (a.Deadline - a.Started).ToString("0") + " s");
                _active = null;
                StopEverything();
                Enter(Phase.Between);
            }
        }

        #endregion

        #region Suite wrappers (run by RoyalRegressionRunner)

        private static void Stage(string stage)
        {
            if (_active != null) _active.Stage = stage;
        }

        /// <summary>The check callback handed to enumerator suites: records, never throws, so one miss does not hide the rest.</summary>
        private static void Check(bool ok, string message)
        {
            var a = _active;
            if (a == null) return;
            a.Checks++;
            a.LastCheck = message;
            a.Lines.Add((ok ? "PASS " : "FAIL ") + message);
            if (ok) return;
            a.Failed++;
            if (a.FirstFail == null) a.FirstFail = message;
        }

        private static IEnumerator InSession(Active a)
        {
            var qa = RoyalQaService.Instance;
            bool ended = false;
            try
            {
                // Start on the player loop, not inside the Editor update that launched the runner.
                yield return null;
                if (!qa || !qa.Ready) throw new InvalidOperationException("RoyalQaService is not ready (game scene, LevelManager, InitScript, MenuManager)");
                if (RoyalSave.Testing) throw new InvalidOperationException("a QA session is already open");
                a.Stage = "start session";
                qa.Loop = false;
                qa.StartSession();
                if (!RoyalSave.Testing)
                    throw new InvalidOperationException("RoyalQaService.StartSession did not open the isolated profile (busy " + qa.Busy + ")");
                qa.Level = 1;
                qa.Visible = false;
                qa.Loop = false;
                qa.Speed = 1;
                qa.Counters = false;

                // Level 1 is moves-based (LIMIT 0/6); the practice board keeps
                // its limit type and replaces the grid with the QA 8x8 fixture.
                a.Stage = "practice board";
                yield return qa.Practice();
                var lm = LevelManager.THIS;
                if (!lm || lm.gameStatus != GameState.Playing || lm.squaresArray == null)
                    throw new InvalidOperationException("no settled practice board (state " + (lm ? lm.gameStatus.ToString() : "no LevelManager") + ")");

                a.Stage = "suite";
                if (a.Suite.Kind == SuiteKind.EffectsBoard) yield return EffectsBody(a.Suite.Entry);
                else yield return a.Suite.Body(lm, Check);
                a.BodyDone = true;

                a.Stage = "end session";
                yield return qa.EndSession();
                ended = true;
            }
            finally
            {
                if (!ended && qa && RoyalSave.Testing) RoyalRegressionRunner.Run(qa.EndSession());
                a.WrapperDone = true;
            }
        }

        /// <summary>
        /// RoyalQaService.Update sets Time.timeScale to the QA speed every frame
        /// of a session, and the effects suite proves that pausing (timeScale 0)
        /// holds an impact. The service is paused for the suite and resumed
        /// before EndSession.
        /// </summary>
        private static IEnumerator EffectsBody(Action entry)
        {
            var qa = RoyalQaService.Instance;
            ResetEffectsFlags();
            int baseline = Runners().Count;
            qa.enabled = false;
            try
            {
                entry();
                while (Runners().Count > baseline) yield return new WaitForSecondsRealtime(0.1f);
            }
            finally
            {
                if (qa) qa.enabled = true;
            }
        }

        private static IEnumerator SelfDriven(Active a)
        {
            var qa = RoyalQaService.Instance;
            try
            {
                yield return null;
                if (!qa || !qa.Ready) throw new InvalidOperationException("RoyalQaService is not ready (game scene, LevelManager, InitScript, MenuManager)");
                if (RoyalSave.Testing) throw new InvalidOperationException("a QA session is already open; this suite starts from the normal profile");
                a.Stage = "suite";
                int baseline = Runners().Count;
                a.Suite.Entry();
                while (Runners().Count > baseline) yield return new WaitForSecondsRealtime(0.1f);

                // Several suites end their profile on the QA service itself.
                a.Stage = "suite restoring the player profile";
                float until = Time.realtimeSinceStartup + 30;
                while ((RoyalSave.Testing || qa.Busy) && Time.realtimeSinceStartup < until) yield return null;
                if (RoyalSave.Testing) a.Lines.Add("FAIL the suite left the isolated QA profile active after it finished");
                a.BodyDone = true;
            }
            finally
            {
                a.WrapperDone = true;
            }
        }

        private static void ResetEffectsFlags()
        {
            // RunTnt and friends set a flag and call Run; Run clears it only in its
            // finally. A refused precondition would leave the flag for the next run.
            foreach (string flag in new[] { "cellBreakOnly", "rocketOnly", "tntOnly", "colorBallOnly", "inputOnly" })
                SetStatic(typeof(RoyalEffectsChecks), flag, false);
        }

        #endregion

        #region Verdicts

        private static void Complete(Active a)
        {
            switch (a.Suite.Kind)
            {
                case SuiteKind.Session: CompleteSession(a); break;
                case SuiteKind.EffectsBoard: CompleteEffects(a); break;
                case SuiteKind.SelfSession: CompleteSelf(a); break;
            }
        }

        private static void CompleteSession(Active a)
        {
            if (a.ErrorLogs > 0 && a.BodyDone)
                a.Lines.Add("FAIL " + a.ErrorLogs + " runtime error(s) logged during the suite; first: " + a.FirstError);
            if (!a.BodyDone)
            {
                Verdict(a, "ERROR", "aborted at stage '" + a.Stage + "': " + (a.LastException ?? a.FirstError ?? "no exception was logged") + Progress(a), a.Lines);
                return;
            }
            int failed = a.Lines.Count(l => l.StartsWith("FAIL ", StringComparison.Ordinal));
            if (failed > 0)
            {
                string first = a.Lines.First(l => l.StartsWith("FAIL ", StringComparison.Ordinal)).Substring(5);
                Verdict(a, "FAIL", failed + " of " + a.Lines.Count + " checks failed; first: " + first, a.Lines);
                return;
            }
            if (a.Checks == 0)
            {
                Verdict(a, "FAIL", "the suite finished without recording a check", a.Lines);
                return;
            }
            Verdict(a, "PASS", a.Checks + " checks", a.Lines);
        }

        private static void CompleteEffects(Active a)
        {
            var lines = EffectsLines(out int passed, out int failed, out string first);
            if (!a.BodyDone)
            {
                Verdict(a, "ERROR", "aborted at stage '" + a.Stage + "': " + (a.LastException ?? a.FirstError ?? "no exception was logged"), lines);
                return;
            }
            if (failed > 0)
            {
                Verdict(a, "FAIL", failed + " error(s) after " + passed + " passing checks; first: " + first, lines);
                return;
            }
            if (passed == 0)
            {
                Verdict(a, "FAIL", "the suite finished without recording a check", lines);
                return;
            }
            Verdict(a, "PASS", passed + " checks", lines);
        }

        private static List<string> EffectsLines(out int passed, out int failed, out string first)
        {
            var lines = new List<string>();
            var checks = GetStatic(typeof(RoyalEffectsChecks), "checks") as List<string>;
            var errors = GetStatic(typeof(RoyalEffectsChecks), "errors") as List<string>;
            passed = checks == null ? 0 : checks.Count;
            failed = errors == null ? 0 : errors.Count;
            first = null;
            if (checks != null) lines.AddRange(checks.Select(c => "PASS " + c));
            if (errors != null)
            {
                foreach (string error in errors)
                {
                    string text = OneLine(error.StartsWith("FAIL: ", StringComparison.Ordinal) ? error.Substring(6) : error);
                    if (first == null) first = text;
                    lines.Add("FAIL " + text);
                }
            }
            return lines;
        }

        private static void CompleteSelf(Active a)
        {
            string path = SafePath(a.Suite.ResultFile);
            string[] lines = FreshLines(path, a.StartedUtc);
            var own = lines == null ? new List<string>() : lines.Where(l => l.Trim().Length > 0).ToList();
            if (a.ErrorLogs > 0)
                own.Add("NOTE " + a.ErrorLogs + " error log(s) during the suite; first: " + a.FirstError);
            own.AddRange(a.Lines);

            if (!a.BodyDone)
            {
                Verdict(a, "ERROR", "could not run: " + (a.LastException ?? a.FirstError ?? "no exception was logged"), own);
                return;
            }
            if (lines == null)
            {
                Verdict(a, "ERROR", "wrote no result file (" + (path ?? "unknown path") + ")" +
                                    (a.FirstError != null ? "; first error: " + a.FirstError : ""), own);
                return;
            }
            int passing = lines.Count(l => l.StartsWith("PASS ", StringComparison.Ordinal));
            bool left = a.Lines.Any(l => l.StartsWith("FAIL ", StringComparison.Ordinal));
            if (a.Suite.Passed(lines) && !left)
            {
                Verdict(a, "PASS", passing + " checks (" + path + ")", own);
                return;
            }
            string reason = lines.FirstOrDefault(l => l.StartsWith("FAIL ", StringComparison.Ordinal))
                            ?? (left ? a.Lines.First(l => l.StartsWith("FAIL ", StringComparison.Ordinal)) : null)
                            ?? a.LastException
                            ?? lines.LastOrDefault(l => l.Trim().Length > 0)
                            ?? "no success marker";
            Verdict(a, "FAIL", "after " + passing + " passing checks: " + OneLine(reason), own);
        }

        private static void Timeout(Active a, string why)
        {
            List<string> lines;
            switch (a.Suite.Kind)
            {
                case SuiteKind.EffectsBoard:
                    lines = EffectsLines(out _, out _, out _);
                    break;
                case SuiteKind.SelfSession:
                    var file = FreshLines(SafePath(a.Suite.ResultFile), a.StartedUtc);
                    lines = file == null ? new List<string>() : file.Where(l => l.Trim().Length > 0).ToList();
                    break;
                default:
                    lines = a.Lines;
                    break;
            }
            string last = a.LastCheck ?? (lines.Count > 0 ? lines[lines.Count - 1] : null);
            Verdict(a, "TIMEOUT", why + " at stage '" + a.Stage + "'" + (last != null ? "; last check: " + OneLine(last) : "") +
                                  (a.LastException != null ? "; last exception: " + a.LastException : ""), lines);
        }

        private static string Progress(Active a)
        {
            return a.Checks > 0 ? " (" + a.Checks + " checks recorded, " + a.Failed + " failed)" : "";
        }

        private static void Verdict(Active a, string verdict, string detail, IList<string> lines)
        {
            double seconds = EditorApplication.timeSinceStartup - a.Started;
            var text = new StringBuilder();
            int shown = 0;
            if (lines != null)
            {
                foreach (string line in lines)
                {
                    if (shown++ >= 2000)
                    {
                        text.Append("  ... ").Append(lines.Count - 2000).Append(" more lines in ").Append(SuiteFile(a.Suite.Name)).Append('\n');
                        break;
                    }
                    text.Append("  ").Append(OneLine(line, 600)).Append('\n');
                }
            }
            foreach (string sample in a.ErrorSamples) text.Append("  LOG ").Append(sample).Append('\n');
            string verdictLine = verdict + " " + a.Suite.Name + " " + OneLine(detail, 900) + " [" + seconds.ToString("0.0") + " s]";
            text.Append(verdictLine).Append('\n');

            File.AppendAllText(ReportPath, text.ToString());
            try
            {
                var own = new StringBuilder();
                own.Append("# ").Append(a.Suite.Name).Append(": ").Append(a.Suite.Note).Append('\n');
                if (lines != null) foreach (string line in lines) own.Append(line).Append('\n');
                own.Append(verdictLine).Append('\n');
                File.WriteAllText(SuiteFile(a.Suite.Name), own.ToString());
            }
            catch (Exception e)
            {
                Debug.LogWarning(Tag + "could not write " + SuiteFile(a.Suite.Name) + ": " + e.Message);
            }
            Debug.Log(Tag + verdictLine);
        }

        private static string[] FreshLines(string path, DateTime since)
        {
            try
            {
                if (path == null || !File.Exists(path)) return null;
                if (File.GetLastWriteTimeUtc(path) < since.AddSeconds(-2)) return null;
                return File.ReadAllLines(path);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static string SafePath(Func<string> path)
        {
            try
            {
                string value = path == null ? null : path();
                return string.IsNullOrEmpty(value) ? null : Path.GetFullPath(value);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static string SuiteFile(string name)
        {
            return Path.Combine(Path.GetFullPath(ResultsFolder), name + ".txt");
        }

        private static void Report(string line)
        {
            File.AppendAllText(ReportPath, line + "\n");
        }

        private static string OneLine(string text, int max = 400)
        {
            if (text == null) return "";
            string line = text.Replace('\r', ' ').Replace('\n', ' ').Trim();
            return line.Length > max ? line.Substring(0, max) + "..." : line;
        }

        /// <summary>Counts verdict lines (column 0) in a report. Thread-safe: the watchdog calls it too.</summary>
        internal static string SummaryOf(string reportPath, out int exitCode)
        {
            int pass = 0, fail = 0, timeout = 0, error = 0;
            try
            {
                foreach (string line in File.ReadAllLines(reportPath))
                {
                    if (line.StartsWith("PASS ", StringComparison.Ordinal)) pass++;
                    else if (line.StartsWith("FAIL ", StringComparison.Ordinal)) fail++;
                    else if (line.StartsWith("TIMEOUT ", StringComparison.Ordinal)) timeout++;
                    else if (line.StartsWith("ERROR ", StringComparison.Ordinal)) error++;
                }
            }
            catch (Exception)
            {
                error++;
            }
            exitCode = pass > 0 && fail + timeout + error == 0 ? 0 : 1;
            return "SUMMARY: " + pass + " pass, " + fail + " fail, " + timeout + " timeout, " + error + " error";
        }

        #endregion

        #region Ending

        private static void Finish(string fatal)
        {
            if (fatal != null)
            {
                foreach (string left in Queue) Report("ERROR " + left + " not run: " + fatal);
                Report("ERROR PortChecks " + fatal);
            }
            Queue = new List<string>();

            if (_active != null)
            {
                StopEverything();
                _active = null;
            }

            int code;
            string summary = SummaryOf(ReportPath, out code);
            Report(summary);
            Finished = true;

            bool headless = Headless;
            string response = SessionState.GetString(KResponse, "");
            SessionState.SetBool(KArmed, false);
            SessionState.SetBool(KNeedEnter, false);
            SessionState.EraseString(KQueue);
            Unhook();
            RestoreGlobals();

            if (response.Length > 0) WriteResponse(response, File.ReadAllText(ReportPath));
            Debug.Log(Tag + summary + " (exit code " + code + "). Report: " + ReportPath);

            if (headless)
            {
                // The watchdog stays up until the Editor is gone, in case quitting hangs.
                PortChecksRunner.ExpectExit();
                EditorApplication.Exit(code);
                return;
            }
            if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
        }

        /// <summary>Stops a hung suite: its runner's finally blocks run and start restoring the profile.</summary>
        private static void StopEverything()
        {
            var qa = RoyalQaService.Instance;
            if (qa)
            {
                qa.Loop = false;
                qa.enabled = true;
                if (qa.Busy)
                {
                    // A command coroutine that never returns keeps Busy set, and
                    // EndSession waits on Busy. Stop it before the runners' finally
                    // blocks start their EndSession on this service.
                    qa.StopAllCoroutines();
                    SetField(qa, "endingSession", false);
                    SetProperty(qa, "Busy", false);
                }
            }
            foreach (var runner in Runners())
            {
                try
                {
                    if (runner) Object.DestroyImmediate(runner.gameObject);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
            RestoreGlobals();
        }

        private static void RestoreGlobals()
        {
            Time.timeScale = 1;
            Time.captureDeltaTime = 0;
            if (_frameRate != 0 || _vSync != 0)
            {
                Application.targetFrameRate = _frameRate;
                QualitySettings.vSyncCount = _vSync;
            }
        }

        private static void WriteResponse(string path, string text)
        {
            try
            {
                string full = Path.GetFullPath(path);
                string temp = full + ".tmp";
                File.WriteAllText(temp, text);
                if (File.Exists(full)) File.Delete(full);
                File.Move(temp, full);
            }
            catch (Exception e)
            {
                Debug.LogWarning(Tag + "could not write " + path + ": " + e.Message);
            }
        }

        #endregion

        #region File bridge (open Editor)

        private static double _lastBridgeLook;

        /// <summary>
        /// Lets a shell drive an Editor that is already open (batchmode is locked
        /// out while it is): a line in Temp/port-check-command.txt runs those
        /// suites, and the report lands in Temp/port-check-response.txt.
        /// </summary>
        private static void WatchBridge()
        {
            double now = EditorApplication.timeSinceStartup;
            if (now - _lastBridgeLook < 1) return;
            _lastBridgeLook = now;

            string command = Path.GetFullPath(CommandFile);
            if (!File.Exists(command)) return;

            string text;
            try
            {
                text = File.ReadAllText(command);
                File.Delete(command);
            }
            catch (Exception)
            {
                return;
            }

            if (Armed)
            {
                WriteResponse(ResponseFile, "ERROR PortChecks busy: a run is already in progress\n");
                return;
            }
            string response = Path.GetFullPath(ResponseFile);
            if (File.Exists(response)) File.Delete(response);

            var names = text.Split(new[] { ',', '\n', '\r', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            if (names.Count == 0 || names.Any(n => string.Equals(n, "all", StringComparison.OrdinalIgnoreCase))) names = AllSuiteNames.ToList();
            else if (names.Any(n => string.Equals(n, "headless", StringComparison.OrdinalIgnoreCase))) names = HeadlessSuiteNames.ToList();
            Debug.Log(Tag + "bridge command: " + string.Join(", ", names.ToArray()));
            Start(names, false, 0, 0, ResponseFile);
        }

        #endregion

        #region Helpers

        private static List<RoyalRegressionRunner> Runners()
        {
            var list = new List<RoyalRegressionRunner>();
            foreach (var runner in Resources.FindObjectsOfTypeAll<RoyalRegressionRunner>())
                if (runner && runner.gameObject) list.Add(runner);
            return list;
        }

        /// <summary>Taps through a story beat (dialogue waits for a tap forever), as StoryPlaytest does.</summary>
        private static void PressThroughDialogue(double now)
        {
            if (now - _lastPress < 0.3) return;
            _lastPress = now;
            if (!DialogueSystem.IsPlaying) return;
            var host = GameObject.Find("~StoryDialogue");
            if (host == null) return;
            foreach (var button in host.GetComponentsInChildren<UnityEngine.UI.Button>(true))
                if (button.name == "Advance") button.onClick.Invoke();
        }

        private static void OnLog(string message, string stack, LogType type)
        {
            var a = _active;
            if (a == null) return;
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
            if (message != null && message.StartsWith(Tag, StringComparison.Ordinal)) return;
            // Unity Search can throw on its own at startup (RoyalAllDualComboChecks excludes it too).
            if (stack != null && stack.Contains("UnityEditor.Search")) return;

            string line = OneLine(type + ": " + message, 500);
            string at = stack == null ? "" : stack.Split('\n')[0].Trim();
            if (at.Length > 0) line += " (at " + OneLine(at, 200) + ")";
            a.ErrorLogs++;
            if (a.FirstError == null) a.FirstError = line;
            if (type == LogType.Exception) a.LastException = line;
            if (a.ErrorSamples.Count < 5) a.ErrorSamples.Add(line);
        }

        private static object GetStatic(Type type, string field)
        {
            var info = type.GetField(field, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            return info == null ? null : info.GetValue(null);
        }

        private static void SetStatic(Type type, string field, object value)
        {
            var info = type.GetField(field, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            if (info != null && !info.IsLiteral && !info.IsInitOnly) info.SetValue(null, value);
        }

        private static void SetField(object target, string field, object value)
        {
            var info = target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (info != null) info.SetValue(target, value);
        }

        private static void SetProperty(object target, string property, object value)
        {
            var info = target.GetType().GetProperty(property, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            var setter = info == null ? null : info.GetSetMethod(true);
            if (setter != null) setter.Invoke(target, new[] { value });
        }

        #endregion
    }
}
#endif
