#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using UnityEngine.SceneManagement;
using RoyalThrone.Campaign;
using RoyalThrone.Testing;

namespace RoyalThrone.ReferenceGameplay.Editor
{
    /// <summary>
    /// Small editor-only inbox for this import's explicit checks. Accepts only the
    /// named commands below; no arbitrary methods, code, scene loads, or player builds.
    /// </summary>
    [InitializeOnLoad]
    public static class ReferenceGameplayChecks
    {
        const string Key = "Royal.ReferenceGameplayChecks.";
        static readonly string Folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../Design/ReferenceGameplayImport"));
        static readonly string Request = Path.Combine(Folder, "editor-request.txt");
        static double nextPoll;
        static bool processing, captureOwnsSession;
        static string ioError = "";

        [Serializable] sealed class ErrorEntry
        {
            public string utc, kind, message, stack;
        }
        [Serializable] sealed class ErrorList
        {
            public List<ErrorEntry> entries = new List<ErrorEntry>();
            public int total;
        }
        [Serializable] sealed class SceneState
        {
            public string path;
            public bool loaded, dirty;
        }
        [Serializable] sealed class SceneList { public SceneState[] scenes; }
        [Serializable] sealed class Status
        {
            public string utc, trackedSinceUtc, lastCommand, commandUtc, result, campaignStatus, captureStatus, ioError;
            public bool playing, changingPlayMode, compiling, updating, paused, qaReady, testProfile, referenceCampaign;
            public string[] acceptedCommands;
            public SceneState[] scenes, scenesBeforePlay;
            public int errorCount;
            public ErrorEntry[] recentErrors;
        }

        static ReferenceGameplayChecks()
        {
            if (SessionState.GetString(Key + "since", "") == "")
                SessionState.SetString(Key + "since", DateTime.UtcNow.ToString("o"));
            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged += PlayStateChanged;
            Application.logMessageReceived += LogReceived;
            CompilationPipeline.assemblyCompilationFinished += CompilationFinished;
        }

        static SceneState[] Scenes()
        {
            var scenes = new List<SceneState>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                scenes.Add(new SceneState { path = scene.path, loaded = scene.isLoaded, dirty = scene.isDirty });
            }
            return scenes.ToArray();
        }

        static void Tick()
        {
            if (EditorApplication.timeSinceStartup < nextPoll || processing) return;
            nextPoll = EditorApplication.timeSinceStartup + .5;
            try
            {
#if !ROYAL_DISABLE_QA
                if (captureOwnsSession && RoyalThrone.Editor.RoyalHandoffCapture.Finished)
                {
                    captureOwnsSession = false;
                    RoyalThrone.Editor.RoyalRegressionRunner.Run(FinishCapture());
                }
#endif
                if (!EditorApplication.isCompiling && !EditorApplication.isUpdating && File.Exists(Request))
                {
                    var command = File.ReadAllText(Request).Trim();
                    // Consume once, before a command can trigger domain reload or Play Mode.
                    File.Delete(Request);
                    Execute(command);
                }
                WriteStatus();
            }
            catch (IOException ex) { ioError = ex.Message; }
            catch (UnauthorizedAccessException ex) { ioError = ex.Message; }
        }

        static void Execute(string command)
        {
            if (processing) return;
            processing = true;
            SessionState.SetString(Key + "command", command);
            SessionState.SetString(Key + "commandUtc", DateTime.UtcNow.ToString("o"));
            SetResult("Running " + command);
            try
            {
                switch (command)
                {
                    case "refresh":
                        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode before refresh.");
                        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                        SetResult("Refreshed edited scripts without rebuilding imported assets.");
                        break;
                    case "input-checks":
                    case "input-event-checks":
#if !ROYAL_DISABLE_QA
                        if (!EditorApplication.isPlaying || !RoyalQaService.Instance || !RoyalQaService.Instance.Ready || RoyalSave.Testing)
                            throw new InvalidOperationException("Input checks need gameplay ready without an existing QA session.");
                        RoyalThrone.Editor.RoyalRegressionRunner.Run(command == "input-checks" ? ResponsiveInputChecks.Run() : ResponsiveInputChecks.InputEvents());
#endif
                        break;
                    case "import":
                        if (EditorApplication.isPlayingOrWillChangePlaymode)
                            throw new InvalidOperationException("Exit Play Mode before importing.");
                        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                        ReferenceGameplayMediaImporter.Build();
                        ReferenceGameplayVfxImporter.Build();
                        ReferenceGameplayVfxImporter.DumpImportedModules();
                        ReferenceBlockerImporter.Import();
                        ReferenceLevelChecks.VerifyData();
                        SetResult("Imported changed assets and verified level data.");
                        break;
                    case "enter-play":
                        if (EditorApplication.isPlayingOrWillChangePlaymode)
                        {
                            SetResult("Already playing or entering Play Mode.");
                            break;
                        }
                        var setup = Scenes();
                        if (!setup.Any(s => s.loaded && (s.path == "Assets/JellyGarden/Scenes/game.unity" || s.path == "Assets/JellyGarden/Scenes/main.unity")))
                            throw new InvalidOperationException("The game or startup scene is not open. Current scenes and unsaved changes were left untouched.");
                        SessionState.SetString(Key + "scenesBeforePlay", JsonUtility.ToJson(new SceneList { scenes = setup }));
                        SetResult("Entering Play Mode with current scenes; no scene saved, unloaded or replaced.");
                        WriteStatus();
                        EditorApplication.isPlaying = true;
                        break;
                    case "campaign-checks":
                    case "campaign-31-40":
                    case "campaign-41-50":
                    case "campaign-40":
#if !ROYAL_DISABLE_QA
                        if (!EditorApplication.isPlaying || !RoyalQaService.Instance || !RoyalQaService.Instance.Ready)
                            throw new InvalidOperationException("Game scene is not ready in Play Mode.");
                        if (RoyalSave.Testing)
                            throw new InvalidOperationException("An existing QA session is active. End it before starting the isolated campaign checks.");
                        ReferenceLevelChecks.VerifyData();
                        if (command == "campaign-40") RoyalThrone.Editor.RoyalCampaignChecks.RunLevel40Outcome();
                        else if (command == "campaign-31-40") RoyalThrone.Editor.RoyalCampaignChecks.Batch31();
                        else if (command == "campaign-41-50") RoyalThrone.Editor.RoyalCampaignChecks.Batch41();
                        else RoyalThrone.Editor.RoyalCampaignChecks.RunAll();
                        SetResult("Started existing isolated 50-level campaign checks. Read campaignStatus and its validation reports.");
#else
                        throw new InvalidOperationException("QA is disabled by ROYAL_DISABLE_QA.");
#endif
                        break;
                    case "open-game":
                        if (!EditorApplication.isPlaying) throw new InvalidOperationException("Enter Play Mode first.");
                        var title = UnityEngine.Object.FindAnyObjectByType<RoyalThrone.Presentation.RoyalTitleScreen>();
                        if (title) title.BeginPlay();
                        else if (!SceneManager.GetSceneByPath("Assets/JellyGarden/Scenes/game.unity").isLoaded)
                            throw new InvalidOperationException("Neither the existing title entry point nor game scene is available.");
                        SetResult("Used existing title Play action to open the gameplay scene.");
                        break;
                    case "capture-gameplay":
#if !ROYAL_DISABLE_QA
                        if (!EditorApplication.isPlaying || !RoyalQaService.Instance || !RoyalQaService.Instance.Ready || RoyalSave.Testing)
                            throw new InvalidOperationException("Capture requires Play Mode ready with no existing QA session.");
                        var captureFolder = Path.Combine(Folder, "Gameplay-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"));
                        var gameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
                        if (gameViewType != null) { var gameView = EditorWindow.GetWindow(gameViewType); gameView.Show(); gameView.Focus(); }
                        RoyalThrone.Editor.RoyalHandoffCapture.Run(captureFolder,
                            "Match-Match3,Match-Match4,Match-Match5,Match-Cascade,Rocket,Rocket-Vertical,TNT,Propeller,ColorBall,Combo-ColorBall-Rocket,Combo-Rocket-TNT,Combo-TNT-TNT,Combo-Propeller-Propeller");
                        captureOwnsSession = true;
                        SetResult("Recording actual runtime in isolated QA profile: " + captureFolder);
#else
                        throw new InvalidOperationException("QA is disabled by ROYAL_DISABLE_QA.");
#endif
                        break;
                    case "palette-checks":
#if !ROYAL_DISABLE_QA
                        if (!EditorApplication.isPlaying || !RoyalQaService.Instance || !RoyalQaService.Instance.Ready || RoyalSave.Testing)
                            throw new InvalidOperationException("Palette check requires gameplay ready without an active test session.");
                        RoyalThrone.Editor.RoyalRegressionRunner.Run(CheckBoardPalette());
#endif
                        break;
                    case "exit-play":
                        SetResult("Exiting Play Mode; current edit-time scenes remain intact.");
                        WriteStatus();
                        EditorApplication.isPlaying = false;
                        break;
                    case "status":
                        SetResult("Status captured; no test or scene change requested.");
                        break;
                    case "reset-tracking":
                        File.WriteAllText(Path.Combine(Folder, "errors-before-checks.json"), SessionState.GetString(Key + "errors", "{}"));
                        SessionState.SetString(Key + "errors", "{\"entries\":[],\"total\":0}");
                        SessionState.SetString(Key + "since", DateTime.UtcNow.ToString("o"));
                        SetResult("Archived prior errors and started fresh validation tracking.");
                        break;
                    default:
                        throw new InvalidOperationException("Unknown command. Allowed: import, enter-play, open-game, campaign-checks, capture-gameplay, exit-play, status, reset-tracking.");
                }
            }
            catch (Exception ex)
            {
                SetResult("FAILED " + command + ": " + ex.Message);
                Record("Command", ex.Message, ex.StackTrace);
                Debug.LogError("[Reference Gameplay checks] " + ex.Message);
            }
            finally { processing = false; }
        }

        static void SetResult(string value) { SessionState.SetString(Key + "result", value); }

#if !ROYAL_DISABLE_QA
        static System.Collections.IEnumerator CheckBoardPalette()
        {
            var qa = RoyalQaService.Instance;
            string report = Path.Combine(Folder, "Validation/Board-Palette.txt");
            File.WriteAllText(report, "RUNNING five-piece palette check\n");
            void Check(bool valid, string label)
            {
                if (!valid) throw new InvalidOperationException(label);
                File.AppendAllText(report, "PASS " + label + "\n");
            }
            try
            {
                qa.StartSession();
                yield return qa.Practice();
                var lm = JellyGarden.Scripts.LevelManager.THIS;
                lm.colorLimit = 6; // Exercise an old six-colour level, without changing its saved asset.
                foreach (var item in lm.GetItems().Where(i => i).ToArray())
                {
                    item.GenColor();
                    Check(item.color >= 0 && item.color < 5, "Refill excludes retired crystal");
                    yield return null;
                }
                yield return qa.Settled();
                var piece = lm.GetSquare(3, 3).item;
                piece.SetColor(5);
                Check(piece.color == 1 && piece.sprRenderer.sprite == ReferenceGameplayCatalog.Art("shield"), "Old crystal ID becomes a matching blue shield");
                var authored = new JellyGarden.Scripts.Targets.TargetObject { type = JellyGarden.Scripts.Target.COLLECT, color = 5, targetCount = 3 };
                var runtime = authored.DeepCopy();
                Check(authored.color == 5 && runtime.color == 1 && runtime.icon == ReferenceGameplayCatalog.Art("shield"), "Old collection goal migrates in runtime copy only");
                Check(runtime.SetOff(piece.gameObject) && runtime.countCollected == 1, "Replacement shield credits the migrated goal once");
                Check(JellyGarden.Scripts.Juice.RoyalGameplayArt.BoardColor(555) == 555 && JellyGarden.Scripts.Juice.RoyalGameplayArt.BoardColor(1000) == 1000, "Booster and ingredient sentinel IDs preserved");
                yield return qa.Execute("Match 3");
                yield return qa.Settled();
                Check(JellyGarden.Scripts.LevelManager.Score > 0 && lm.GetItems().Where(i => i).All(i => i.color != 5), "Live match and refill settle without the crystal");
                yield return qa.EndSession();
                Check(!RoyalSave.Testing, "Normal player profile restored");
                File.AppendAllText(report, "ALL PALETTE CHECKS PASSED\n");
                SetResult("Five-piece palette, legacy goal compatibility and live match checks passed.");
            }
            finally { if (RoyalSave.Testing && qa) qa.StartCoroutine(qa.EndSession()); }
        }

        static System.Collections.IEnumerator FinishCapture()
        {
            if (RoyalSave.Testing && RoyalQaService.Instance) yield return RoyalQaService.Instance.EndSession();
            yield return null;
            yield return null;
            var report = "UTC=" + DateTime.UtcNow.ToString("o") + "\nTestProfile=" + RoyalSave.Testing
                + "\nNativeVfx=" + ReferenceVfxPool.Active + "\nParticles=" + ReferenceVfxPool.ParticleCount
                + "\nActivationViews=" + ReferenceActivationVisual.ActiveCount + "\nVoices=" + ReferenceActivationVisual.PlayingVoices
                + "\nBoardViews=" + ReferencePieceVisual.ActiveViews;
            File.WriteAllText(Path.Combine(Folder, "Validation/Gameplay-Cleanup.txt"), report);
            SetResult("Gameplay capture finished and isolated save session restored; cleanup counts recorded.");
        }
#endif

        static void PlayStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetString(Key + "command", "") == "enter-play")
                SetResult("Entered Play Mode. Wait for qaReady before campaign-checks.");
            if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetString(Key + "command", "") == "exit-play")
                SetResult("Returned to Edit Mode; no scenes saved or replaced by the check runner.");
            nextPoll = 0;
        }

        static ErrorList Errors()
        {
            return JsonUtility.FromJson<ErrorList>(SessionState.GetString(Key + "errors", "{\"entries\":[],\"total\":0}")) ?? new ErrorList();
        }
        static void Record(string kind, string message, string stack)
        {
            var errors = Errors();
            errors.total++;
            errors.entries.Add(new ErrorEntry { utc = DateTime.UtcNow.ToString("o"), kind = kind, message = message, stack = stack });
            if (errors.entries.Count > 32) errors.entries.RemoveAt(0);
            SessionState.SetString(Key + "errors", JsonUtility.ToJson(errors));
        }
        static void LogReceived(string message, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                Record(type.ToString(), message, stack);
        }
        static void CompilationFinished(string assembly, CompilerMessage[] messages)
        {
            foreach (var message in messages)
                if (message.type == CompilerMessageType.Error)
                    Record("Compile", message.message, message.file + ":" + message.line + " in " + assembly);
        }

        static void WriteStatus()
        {
            Directory.CreateDirectory(Folder);
            var errors = Errors();
            var before = JsonUtility.FromJson<SceneList>(SessionState.GetString(Key + "scenesBeforePlay", "{\"scenes\":[]}"));
            var status = new Status
            {
                utc = DateTime.UtcNow.ToString("o"), trackedSinceUtc = SessionState.GetString(Key + "since", ""),
                lastCommand = SessionState.GetString(Key + "command", ""), commandUtc = SessionState.GetString(Key + "commandUtc", ""),
                result = SessionState.GetString(Key + "result", "Ready; no request received."),
                playing = EditorApplication.isPlaying, changingPlayMode = EditorApplication.isPlayingOrWillChangePlaymode != EditorApplication.isPlaying,
                compiling = EditorApplication.isCompiling, updating = EditorApplication.isUpdating, paused = EditorApplication.isPaused,
                testProfile = RoyalSave.Testing, referenceCampaign = RoyalReferenceCampaign.Enabled,
                acceptedCommands = new[] { "import", "refresh", "enter-play", "open-game", "input-checks", "input-event-checks", "palette-checks", "campaign-checks", "campaign-31-40", "campaign-41-50", "capture-gameplay", "exit-play", "status", "reset-tracking" },
                scenes = Scenes(), scenesBeforePlay = before?.scenes ?? Array.Empty<SceneState>(),
                errorCount = errors.total, recentErrors = errors.entries.ToArray(), ioError = ioError
            };
#if !ROYAL_DISABLE_QA
            status.qaReady = EditorApplication.isPlaying && RoyalQaService.Instance && RoyalQaService.Instance.Ready;
            status.campaignStatus = RoyalThrone.Editor.RoyalCampaignChecks.Status;
            status.captureStatus = RoyalThrone.Editor.RoyalHandoffCapture.Status;
#else
            status.campaignStatus = "QA disabled";
#endif
            string temporary = Path.Combine(Folder, "status.json.tmp");
            File.WriteAllText(temporary, JsonUtility.ToJson(status, true));
            var destination = Path.Combine(Folder, "status.json");
            if (File.Exists(destination)) File.Replace(temporary, destination, null);
            else File.Move(temporary, destination);
        }

        [MenuItem("Tools/Royal Throne/Reference Import/Refresh and verify data")]
        public static void Import() { Execute("import"); }
        [MenuItem("Tools/Royal Throne/Reference Import/Run isolated campaign checks")]
        public static void CampaignChecks() { Execute("campaign-checks"); }
    }
}
#endif
