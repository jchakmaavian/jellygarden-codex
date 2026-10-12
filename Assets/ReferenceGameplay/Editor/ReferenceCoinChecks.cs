#if UNITY_EDITOR && !ROYAL_DISABLE_QA
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JellyGarden.Scripts;
using JellyGarden.Scripts.GUI;
using JellyGarden.Scripts.Juice;
using RoyalThrone.Campaign;
using RoyalThrone.Presentation;
using RoyalThrone.Testing;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalThrone.ReferenceGameplay.Editor
{
    [InitializeOnLoad]
    public static class ReferenceCoinChecks
    {
        static readonly string Folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../Design/Coin-Reference"));
        static readonly string Inbox = Path.Combine(Folder, "editor-request.txt");
        static readonly string Validation = Path.Combine(Folder, "Validation");
        static double nextPoll;
        static bool running;
        static RoyalQaService qa;
        static Report report;
        static int oldTarget, oldVsync;
        static bool oldVisible;
        static Coroutine preview;
        static Coroutine homeCapture;
        static bool homeCaptureRunning;
        static Dictionary<string, string> normalProfile;

        [Serializable] sealed class Flow
        {
            public int coinsBefore, reward, coinsAfterWin, coinsAfterVisuals, finaleBursts;
            public float starStarted = -1, coinsVisible = -1, starArrived = -1, firstCoinArrived = -1, finished;
            public bool overlap;
            public string captureDirectory;
            public int captureFrames;
            public float captureSeconds, captureEffectiveFps;
            public List<int> displayedArrivals = new List<int>();
            public List<string> timeline = new List<string>();
        }

        [Serializable] sealed class Case
        {
            public string name;
            public int requestedFps, callbacks, distinctOrdinals, frames, peakFlights, peakParticleLeases, peakParticles, peakBakedVertices;
            public float seconds, effectiveFps, meanDelta, maxDelta;
            public int endingFlights, endingLeases, endingParticles;
            public bool soundEnabled;
            public int peakAppearVoices, peakCounterVoices, peakCollectVoices, endingCoinVoices;
        }
        [Serializable] sealed class Report
        {
            public string utc, command, status = "running";
            public bool passed, profileRestored;
            public int normalRuntimeCoinsBefore, normalRuntimeCoinsAfter, normalSavedCoinsBefore, normalSavedCoinsAfter;
            public int normalSavedLivesBefore, normalSavedLivesAfter, captureFrames;
            public float initialTimeScale, captureSeconds;
            public List<string> checks = new List<string>();
            public List<string> errors = new List<string>();
            public List<string> frameTrace = new List<string>();
            public List<string> captureTimes = new List<string>();
            public List<Case> cases = new List<Case>();
            public Flow flow;
        }

        static ReferenceCoinChecks() { EditorApplication.update += Poll; }
        static void Poll()
        {
            if (running || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.timeSinceStartup < nextPoll) return;
            nextPoll = EditorApplication.timeSinceStartup + .4;
            if (!File.Exists(Inbox)) return;
            string command = File.ReadAllText(Inbox).Trim();
            File.Delete(Inbox);
            try
            {
                if (command == "import")
                {
                    if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Coin import requires Edit Mode.");
                    ReferenceCoinImporter.Build();
                    File.WriteAllText(Path.Combine(Folder, "editor-result.txt"), "PASS import\n");
                }
                else if (command == "checks" || command == "capture" || command == "capture-only" || command == "board-checks" || command == "flow-checks" || command == "flow-capture")
                    Run(command == "capture", command == "board-checks", command == "capture-only", command.StartsWith("flow-"), command == "flow-capture");
                else throw new InvalidOperationException("Accepted coin commands: import, checks, capture, capture-only, board-checks, flow-checks, flow-capture.");
            }
            catch (Exception ex)
            {
                File.WriteAllText(Path.Combine(Folder, "editor-result.txt"), "FAIL " + command + "\n" + ex);
                Debug.LogException(ex);
            }
        }

        [MenuItem("Tools/Royal Throne/QA/Verify recovered coins")]
        public static void RunChecks() => Run(false);
        static void Run(bool capture, bool boardFixture = false, bool captureOnly = false, bool flowChecks = false, bool flowCapture = false)
        {
            if (running) throw new InvalidOperationException("Coin checks are already running.");
            qa = RoyalQaService.Instance;
            if (!EditorApplication.isPlaying || !qa || !qa.Ready || qa.Busy) throw new InvalidOperationException("Open Home in Play Mode before coin checks.");
            if (RoyalSave.Testing || LevelManager.THIS.gameStatus != GameState.Map) throw new InvalidOperationException("Coin checks start from Home with the normal profile active.");
            if (!Mathf.Approximately(Time.timeScale, 1)) throw new InvalidOperationException("Coin checks require the existing normal 1x clock.");
            Directory.CreateDirectory(Validation);
            running = true;
            oldTarget = Application.targetFrameRate; oldVsync = QualitySettings.vSyncCount; oldVisible = qa.Visible;
            normalProfile = flowChecks ? ProfileSnapshot() : null;
            report = new Report
            {
                utc = DateTime.UtcNow.ToString("o"), command = flowChecks ? (flowCapture ? "flow-capture" : "flow-checks") : boardFixture ? "board-checks" : captureOnly ? "capture-only" : capture ? "capture" : "checks",
                normalRuntimeCoinsBefore = InitScript.Gems,
                normalSavedCoinsBefore = PlayerPrefs.GetInt("Gems"), normalSavedLivesBefore = PlayerPrefs.GetInt("Lifes"),
                initialTimeScale = Time.timeScale
            };
            Application.logMessageReceived += Log;
            Write();
            RoyalThrone.Editor.RoyalRegressionRunner.Run(Suite(capture, boardFixture, captureOnly, flowChecks, flowCapture));
        }

        static void Log(string message, string stack, LogType type)
        {
            if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert)
                report.errors.Add(type + ": " + message + "\n" + stack);
        }
        static void Check(bool condition, string message)
        {
            report.checks.Add((condition ? "PASS " : "FAIL ") + message);
            Write();
            if (!condition) throw new InvalidOperationException(message);
        }
        static void Write()
        {
            File.WriteAllText(Path.Combine(Validation, "checks.json"), JsonUtility.ToJson(report, true));
            File.WriteAllLines(Path.Combine(Validation, "checks.txt"), new[] { report.status, "UTC " + report.utc }.Concat(report.checks).Concat(report.errors));
            if (report.command.StartsWith("flow-"))
            {
                File.WriteAllText(Path.Combine(Validation, "flow-checks.json"), JsonUtility.ToJson(report, true));
                File.WriteAllLines(Path.Combine(Validation, "flow-checks.txt"), new[] { report.status, "UTC " + report.utc }.Concat(report.checks).Concat(report.errors));
            }
            File.WriteAllText(Path.Combine(Folder, "editor-result.txt"), report.status + " " + report.command + "\n");
        }

        static IEnumerator Suite(bool capture, bool boardFixture, bool captureOnly, bool flowChecks, bool flowCapture)
        {
            bool completed = false;
            try
            {
                var gameView = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
                if (gameView != null) EditorWindow.GetWindow(gameView).Focus();
                RoyalWinSequence.CancelCoinPreview();
                qa.StartSession();
                Check(RoyalSave.Testing, "Entered isolated QA save routing");
                qa.Visible = false;
                QualitySettings.vSyncCount = 0;
                int money = InitScript.Gems, saved = RoyalSave.GetInt("Gems");
                if (flowChecks) yield return RealRewardFlow(flowCapture);
                else if (boardFixture) yield return BoardCollection();
                else if (captureOnly) yield return Capture();
                else
                {
                    CheckFrames();
                    foreach (int fps in new[] { 30, 60 })
                    {
                        Application.targetFrameRate = fps;
                        yield return new WaitForSecondsRealtime(.2f);
                        yield return FlightCase("home-" + fps, fps, false, false, fps == 30);
                        yield return FlightCase("board-" + fps, fps, true, false, false);
                    }
                    yield return FlightCase("cancel-before-arrival", 60, true, true, false);
                    int images = FlightImages();
                    yield return FlightCase("reuse", 60, false, false, false);
                    Check(FlightImages() <= images, "Repeated reward reuses the existing coin Image pool");
                    if (capture) yield return Capture();
                }
                if (!boardFixture && !flowChecks) Check(InitScript.Gems == money && RoyalSave.GetInt("Gems") == saved, "Previews and cancellation grant no currency");
                Check(Mathf.Approximately(Time.timeScale, report.initialTimeScale), "Checks retain the normal simulation clock");
                Check(report.errors.Count == 0, "No new runtime error or exception during coin checks");
                completed = true;
            }
            finally
            {
                try
                {
                    Cleanup(RoyalWinSequence.CancelCoinPreview, "cancel coin preview");
                    Cleanup(() => { if (preview != null && qa) qa.StopCoroutine(preview); }, "stop preview coroutine");
                    Cleanup(() => { if (homeCapture != null && qa) qa.StopCoroutine(homeCapture); }, "stop Home capture coroutine");
                    preview = null;
                    homeCapture = null; homeCaptureRunning = false;
                    Application.targetFrameRate = oldTarget; QualitySettings.vSyncCount = oldVsync;
                    report.passed = completed;
                    report.status = "restoring";
                    Write();
                }
                finally
                {
                    // Profile restoration must still run if retiring a broken presentation throws.
                    RoyalThrone.Editor.RoyalRegressionRunner.Run(Restore());
                }
            }
        }

        static void Cleanup(Action action, string label)
        {
            try { action(); }
            catch (Exception ex) { report.errors.Add("Cleanup " + label + ": " + ex); }
        }

        static Dictionary<string, string> ProfileSnapshot()
        {
            var keys = new HashSet<string> { "Gems", "Lifes", "OpenLevel", "OpenLevelTest", "Music", "Sound", "RoyalThrone.HomeMotion", "RoyalThrone.ShowHints" };
            foreach (var level in Resources.LoadAll<TextAsset>("Levels"))
                if (int.TryParse(level.name, out var number)) { keys.Add("Score" + number); keys.Add(string.Format("Level.{0:000}.StarsCount", number)); }
            foreach (BoostType boost in Enum.GetValues(typeof(BoostType))) keys.Add(boost.ToString());
            return keys.ToDictionary(k => k, k => PlayerPrefs.HasKey(k) + "/" + PlayerPrefs.GetInt(k) + "/" + PlayerPrefs.GetFloat(k) + "/" + PlayerPrefs.GetString(k));
        }

        static IEnumerator RealRewardFlow(bool capture)
        {
            Application.targetFrameRate = 60;
            foreach (var sample in new[] { new[] { 64, 10 }, new[] { 15, 10 }, new[] { 25, 10 }, new[] { 15, 6 }, new[] { 3, 10 }, new[] { 0, 10 } })
                for (int arrived = 0; arrived <= sample[1]; arrived++)
                {
                    int expected = arrived == sample[1] ? sample[0] : (sample[0] / sample[1]) * arrived;
                    Check(RoyalWinSequence.CoinAmountAtArrival(sample[0], arrived, sample[1]) == expected,
                        "Arrival amount " + sample[0] + "/" + sample[1] + " at " + arrived + " = " + expected + " (floor increment, final remainder)");
                }

            // Only the disposable QA profile is reset. Load the authored opening board,
            // then use its actual completion, remaining-move, Win and Continue code paths.
            RoyalSave.SetInt("Level.001.StarsCount", 0);
            RoyalSave.SetInt("RoyalThrone.HomeMotion", 1);
            qa.Level = 1;
            yield return qa.OpenLevel();
            qa.Visible = false;
            var lm = LevelManager.THIS;
            Check(RoyalReferenceCampaign.Opening && lm.currentLevel == 1, "Real flow uses authored opening level 1 in isolated saves");
            var flow = report.flow = new Flow { coinsBefore = InitScript.Gems };
            int savedBefore = RoyalSave.GetInt("Gems");
            lm.Limit = 3;
            lm.stars = 1; LevelManager.Score = Mathf.Max(1, lm.star1);
            foreach (var goal in lm.targetObject) goal.countCollected = goal.targetCount;
            lm.CheckWinLose();
            Check(lm.gameStatus == GameState.PreWinAnimations, "Completing goals enters the real pre-win sequence");
            float until = Time.realtimeSinceStartup + 30;
            while (lm.gameStatus != GameState.Win && Time.realtimeSinceStartup < until) yield return null;
            Check(lm.gameStatus == GameState.Win, "Remaining-move sequence completes to Win within 30 seconds");
            flow.reward = RoyalWinSequence.Coins(lm);
            flow.coinsAfterWin = InitScript.Gems;
            flow.finaleBursts = RoyalWinSequence.FinaleBurstCount;
            Check(flow.finaleBursts >= 3 && RoyalWinSequence.FinalePendingCoinValue == 0, "Multiple accepted booster events emit finale coins and drain every assigned value");
            Check(flow.coinsAfterWin == flow.coinsBefore + flow.reward && RoyalSave.GetInt("Gems") == savedBefore + flow.reward,
                "Actual Win credits and persists exactly the existing payout before presentation");
            int lifeAfterWin = InitScript.lifes;
            lm.gameStatus = GameState.Win;
            yield return null;
            Check(InitScript.Gems == flow.coinsAfterWin && RoyalSave.GetInt("Gems") == savedBefore + flow.reward && InitScript.lifes == lifeAfterWin,
                "Duplicate Win assignment cannot repeat coin or life credit");
            Check(RoyalWinSequence.HomeRewardsPending, "Opening win queues the actual Home reward presentation");
            var card = MenuManager.Instance.GetCanvasGlobal().Find("MenuComplete");
            Check(card && card.gameObject.activeInHierarchy, "Actual win card is visible before Continue");
            if (capture) Application.targetFrameRate = 30;
            card.GetComponent<AnimationManager>().CloseMenu();
            Check(lm.gameStatus == GameState.Map, "Actual Continue returns to Home");

            float start = Time.realtimeSinceStartup;
            if (capture) homeCapture = qa.StartCoroutine(CaptureHomeFlow(start));
            int previousShown = flow.coinsBefore;
            bool starWasActive = false, shotOverlap = false, shotArrival = false;
            while (RoyalWinSequence.HomeRewardsPending && Time.realtimeSinceStartup - start < 12)
            {
                yield return null;
                float age = Time.realtimeSinceStartup - start;
                int stars = RoyalWinSequence.HomeRewardStarsInFlight, coins = RoyalWinSequence.HomeRewardCoinsInFlight;
                if (stars > 0 && !starWasActive)
                {
                    starWasActive = true; flow.starStarted = age;
                    flow.timeline.Add(age.ToString("F3") + " star-start");
                }
                bool visibleCoin = coins > 0 && UnityEngine.Object.FindObjectsByType<Image>(FindObjectsSortMode.None)
                    .Any(i => i.name == "Flying reward" && i.gameObject.activeInHierarchy && i.color.a > .01f && i.sprite && i.sprite.name.StartsWith("Items-CoinItem-coin", StringComparison.Ordinal));
                if (visibleCoin && flow.coinsVisible < 0)
                {
                    flow.coinsVisible = age; flow.timeline.Add(age.ToString("F3") + " coin-pile-visible");
                }
                if (stars > 0 && visibleCoin) flow.overlap = true;
                if (starWasActive && stars == 0 && flow.starArrived < 0)
                {
                    flow.starArrived = age; flow.timeline.Add(age.ToString("F3") + " star-arrived");
                }
                int shown = RoyalWinSequence.HomeCoinsShown;
                if (starWasActive && shown != previousShown)
                {
                    if (flow.firstCoinArrived < 0) flow.firstCoinArrived = age;
                    previousShown = shown; flow.displayedArrivals.Add(shown - flow.coinsBefore);
                    flow.timeline.Add(age.ToString("F3") + " coin-arrival shown=" + shown);
                }
                if (capture && !shotOverlap && flow.overlap) { shotOverlap = true; qa.StartCoroutine(FlowShot("flow-star-and-coins")); }
                if (capture && !shotArrival && flow.firstCoinArrived >= 0) { shotArrival = true; qa.StartCoroutine(FlowShot("flow-first-arrival")); }
            }
            flow.finished = Time.realtimeSinceStartup - start;
            Check(!RoyalWinSequence.HomeRewardsPending, "Actual Home reward flow finishes within its bounded timeout");
            Check(flow.starStarted >= 0 && flow.coinsVisible > flow.starStarted && flow.overlap,
                "Home star begins first and remains in flight when the coin pile becomes visible");
            Check(flow.starArrived >= 0 && flow.firstCoinArrived > flow.starArrived,
                "Home star reaches its counter before the first coin arrival");
            Check(flow.displayedArrivals.Count == 10, "Actual Home counter records ten arrival updates");
            for (int i = 0; i < flow.displayedArrivals.Count; i++)
                Check(flow.displayedArrivals[i] == RoyalWinSequence.CoinAmountAtArrival(flow.reward, i + 1, 10), "Actual Home arrival " + (i + 1) + " applies its floor increment or final remainder");
            flow.coinsAfterVisuals = InitScript.Gems;
            Check(flow.coinsAfterVisuals == flow.coinsAfterWin && RoyalSave.GetInt("Gems") == savedBefore + flow.reward,
                "Actual Home visuals do not duplicate the persisted Win reward");
            Check(RoyalWinSequence.HomeCoinsShown == flow.coinsAfterWin && RoyalWinSequence.ActiveCoinFlights == 0,
                "Home display settles at the live saved balance with no coin flight remaining");
            while (homeCaptureRunning) yield return null;
            homeCapture = null;
            if (capture)
            {
                Check(flow.captureFrames > 0 && flow.captureSeconds >= 5.4f, "Captured the actual Continue/Home sequence for 5.5 seconds with realtime timestamps");
                Application.targetFrameRate = 60;
            }

            // Exercise the same integer mapping on genuine renderer callbacks without another Win.
            int callbacks = 0; var mapped = new List<int>();
            yield return RoyalWinSequence.PreviewCoins(new Vector2(0, -190), HomeCoinTarget(), false, n =>
            { callbacks++; mapped.Add(RoyalWinSequence.CoinAmountAtArrival(64, n, 10)); });
            Check(callbacks == 10 && mapped.SequenceEqual(new[] { 6, 12, 18, 24, 30, 36, 42, 48, 54, 64 }),
                "Ten live preview callbacks map 64 to nine increments of six and the final remainder");
            Check(InitScript.Gems == flow.coinsAfterWin && RoyalSave.GetInt("Gems") == savedBefore + flow.reward,
                "Renderer amount preview changes neither runtime currency nor its saved value");
            yield return PartialArrivalCancellation();
        }

        static IEnumerator PartialArrivalCancellation()
        {
            int callbacks = 0, shown = 0, money = InitScript.Gems, saved = RoyalSave.GetInt("Gems");
            var item = new Case { name = "cancel-after-three-arrivals", requestedFps = Application.targetFrameRate, soundEnabled = RoyalSave.GetInt("Sound", 1) != 0 };
            report.cases.Add(item);
            preview = qa.StartCoroutine(RoyalWinSequence.PreviewCoins(new Vector2(0, -190), HomeCoinTarget(), false,
                n => { callbacks++; shown = RoyalWinSequence.CoinAmountAtArrival(64, n, 10); }));
            float deadline = Time.realtimeSinceStartup + 5;
            while (callbacks < 3 && Time.realtimeSinceStartup < deadline) yield return null;
            RoyalWinSequence.CancelCoinPreview();
            Check(callbacks == 3 && shown == 18, "Partial cancellation occurs after exactly three arrivals and 18 displayed coins");
            Check(SampleAudio(item) == 0, "Partial cancellation immediately retires active coin SFX");
            yield return new WaitForSecondsRealtime(2);
            item.callbacks = callbacks;
            item.endingFlights = RoyalWinSequence.ActiveCoinFlights;
            item.endingLeases = ReferenceCoinParticles.ActiveCount; item.endingParticles = ReferenceCoinParticles.ParticleCount;
            item.endingCoinVoices = SampleAudio(item);
            Check(callbacks == 3 && shown == 18, "No callback or remainder adjustment arrives after partial cancellation");
            Check(item.endingFlights == 0 && item.endingLeases == 0 && item.endingParticles == 0 && item.endingCoinVoices == 0,
                "Partial cancellation leaves no coin flights, particle leases or voices");
            Check(InitScript.Gems == money && RoyalSave.GetInt("Gems") == saved, "Partial cancellation never changes runtime or saved currency");
            preview = null;
        }

        static IEnumerator CaptureHomeFlow(float start)
        {
            homeCaptureRunning = true;
            string frames = Path.Combine(Validation, "HomeFlow-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));
            Directory.CreateDirectory(frames);
            int width = 540, height = Mathf.Max(1, Mathf.RoundToInt(540f * Screen.height / Mathf.Max(1, Screen.width)));
            var target = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
            var pixels = new Texture2D(width, height, TextureFormat.RGB24, false);
            var times = new List<string>();
            int frame = 0; float next = start;
            try
            {
                while (frame < 165 && Time.realtimeSinceStartup - start < 5.5f)
                {
                    yield return new WaitForEndOfFrame();
                    float now = Time.realtimeSinceStartup;
                    if (now < next) continue;
                    var screenshot = ScreenCapture.CaptureScreenshotAsTexture();
                    var previous = RenderTexture.active;
                    try
                    {
                        Graphics.Blit(screenshot, target); RenderTexture.active = target;
                        pixels.ReadPixels(new Rect(0, 0, width, height), 0, 0); pixels.Apply(false, false);
                        File.WriteAllBytes(Path.Combine(frames, "coin-" + frame.ToString("D3") + ".jpg"), pixels.EncodeToJPG(85));
                    }
                    finally { RenderTexture.active = previous; UnityEngine.Object.Destroy(screenshot); }
                    times.Add(frame + "," + (now - start).ToString("F6", System.Globalization.CultureInfo.InvariantCulture));
                    frame++; next = start + (Mathf.Floor((now - start) * 30) + 1) / 30f;
                }
            }
            finally
            {
                RenderTexture.ReleaseTemporary(target); UnityEngine.Object.Destroy(pixels);
                report.flow.captureDirectory = frames; report.flow.captureFrames = frame;
                report.flow.captureSeconds = Time.realtimeSinceStartup - start;
                report.flow.captureEffectiveFps = frame / Mathf.Max(.001f, report.flow.captureSeconds);
                File.WriteAllLines(Path.Combine(frames, "timestamps.csv"), new[] { "frame,realtime_seconds" }.Concat(times));
                File.WriteAllText(Path.Combine(frames, "README.txt"), "Silent capture of actual Continue/Home reward flow. Requested 30 FPS; encode using actual timestamps.csv durations. JPEG " + width + "x" + height + ". Sound and music settings preserved.\n");
                homeCaptureRunning = false;
            }
        }

        static IEnumerator FlowShot(string name)
        {
            yield return new WaitForEndOfFrame();
            int width = 540, height = Mathf.Max(1, Mathf.RoundToInt(540f * Screen.height / Mathf.Max(1, Screen.width)));
            var screenshot = ScreenCapture.CaptureScreenshotAsTexture();
            var target = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
            var pixels = new Texture2D(width, height, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            try
            {
                Graphics.Blit(screenshot, target); RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, width, height), 0, 0); pixels.Apply(false, false);
                File.WriteAllBytes(Path.Combine(Validation, name + ".jpg"), pixels.EncodeToJPG(85));
            }
            finally { RenderTexture.active = previous; RenderTexture.ReleaseTemporary(target); UnityEngine.Object.Destroy(pixels); UnityEngine.Object.Destroy(screenshot); }
        }

        static Vector2 HomeCoinTarget()
        {
            Canvas.ForceUpdateCanvases();
            var home = UnityEngine.Object.FindAnyObjectByType<RoyalHomeScreen>();
            if (!home || home.resourceButtons == null || home.resourceButtons.Length < 2 || !home.resourceButtons[1])
                throw new InvalidOperationException("Home coin resource button is unavailable.");
            var button = home.resourceButtons[1].transform;
            var icon = button.Find("Imported 22081") ?? button.Find("Coins icon");
            if (!icon) icon = button.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Imported 22081" || t.name == "Coins icon");
            if (!icon) throw new InvalidOperationException("Home coin icon is unavailable.");
            var canvas = icon.GetComponentInParent<Canvas>();
            canvas = canvas ? canvas.rootCanvas : null;
            Camera camera = canvas && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? (canvas.worldCamera ? canvas.worldCamera : Camera.main) : null;
            var screen = RectTransformUtility.WorldToScreenPoint(camera, icon.position);
            float scale = 800f / Mathf.Max(1, Screen.width);
            return screen * scale - new Vector2(400, Screen.height * scale * .5f);
        }

        static void CheckFrames()
        {
            var node = new GameObject("Coin frame audit", typeof(RectTransform), typeof(Image));
            try
            {
                var image = node.GetComponent<Image>();
                string[] expected = { "coin", "coin.02", "coin.03", "coin.04", "coin.05", "coin.04", "coin.03", "coin.02", "coin", "coin.11", "coin.10", "coin.11" };
                for (int i = 0; i < expected.Length; i++)
                {
                    image.rectTransform.localScale = new Vector3(1.3f, 1.3f, 1);
                    ReferenceCoinAnimation.Apply(image, (i + .25f) / 60f, 70);
                    bool flipped = i >= 5 && i < 10;
                    Check(image.sprite && image.sprite.name.StartsWith("Items-CoinItem-" + expected[i] + "__", StringComparison.Ordinal), "Native sprite frame " + i);
                    Check((image.rectTransform.localScale.x < 0) == flipped && Mathf.Approximately(Mathf.Abs(image.rectTransform.localScale.x), 1.3f), "Native mirror without squash at frame " + i);
                    Check(Mathf.Approximately(image.rectTransform.sizeDelta.y, 70) && Mathf.Approximately(image.rectTransform.sizeDelta.x / 70, image.sprite.rect.width / image.sprite.rect.height), "Authored slice aspect at frame " + i);
                    report.frameTrace.Add((i / 60f).ToString("F6") + " " + image.sprite.name + " flipX=" + flipped + " size=" + image.rectTransform.sizeDelta);
                }
            }
            finally { UnityEngine.Object.Destroy(node); }
        }

        static int FlightImages() => UnityEngine.Object.FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None).Count(i => i.name == "Flying reward");
        static int SampleAudio(Case item)
        {
            int appear = 0, counter = 0, collect = 0;
            foreach (var voice in UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
            {
                if (!voice.isPlaying || !voice.clip) continue;
                switch (voice.clip.name)
                {
                    case "coin_appear__rfv387_17675": appear++; break;
                    case "coin_counter__rfv387_17638": counter++; break;
                    case "coin_collect__15714": collect++; break;
                }
            }
            item.peakAppearVoices = Mathf.Max(item.peakAppearVoices, appear);
            item.peakCounterVoices = Mathf.Max(item.peakCounterVoices, counter);
            item.peakCollectVoices = Mathf.Max(item.peakCollectVoices, collect);
            return appear + counter + collect;
        }
        static IEnumerator FlightCase(string name, int fps, bool board, bool cancel, bool shots)
        {
            RoyalWinSequence.CancelCoinPreview();
            var item = new Case { name = name, requestedFps = fps, soundEnabled = RoyalSave.GetInt("Sound", 1) != 0 };
            report.cases.Add(item);
            var ordinals = new HashSet<int>();
            int calls = 0;
            preview = qa.StartCoroutine(RoyalWinSequence.PreviewCoins(new Vector2(0, -190), HomeCoinTarget(), board, n => { calls++; ordinals.Add(n); }));
            float start = Time.realtimeSinceStartup, last = start, sum = 0, tailStart = -1;
            bool cancelled = false; int shot = 0;
            float[] shotTimes = { .3f, .9f, 1.5f };
            while (Time.realtimeSinceStartup - start < 8)
            {
                yield return null;
                float now = Time.realtimeSinceStartup, age = now - start, dt = now - last; last = now;
                item.frames++; sum += dt; item.maxDelta = Mathf.Max(item.maxDelta, dt);
                item.peakFlights = Mathf.Max(item.peakFlights, RoyalWinSequence.ActiveCoinFlights);
                item.peakParticleLeases = Mathf.Max(item.peakParticleLeases, ReferenceCoinParticles.ActiveCount);
                item.peakParticles = Mathf.Max(item.peakParticles, ReferenceCoinParticles.ParticleCount);
                item.peakBakedVertices = Mathf.Max(item.peakBakedVertices, ReferenceCoinParticles.BakedVertexCount);
                SampleAudio(item);
                if (cancel && !cancelled && age >= .3f)
                {
                    RoyalWinSequence.CancelCoinPreview(); cancelled = true; tailStart = now;
                    Check(SampleAudio(item) == 0, name + ": cancellation immediately stops coin SFX voices");
                }
                if (shots && shot < shotTimes.Length && age >= shotTimes[shot])
                {
                    ScreenCapture.CaptureScreenshot(Path.Combine(Validation, "coin-" + shotTimes[shot].ToString("F1", System.Globalization.CultureInfo.InvariantCulture) + "s.png"));
                    shot++;
                }
                if (!cancel && calls == 10 && tailStart < 0) tailStart = now;
                if (tailStart >= 0 && now - tailStart >= 2) break;
            }
            item.seconds = Time.realtimeSinceStartup - start;
            item.meanDelta = item.frames > 0 ? sum / item.frames : 0;
            item.effectiveFps = item.seconds > 0 ? item.frames / item.seconds : 0;
            item.callbacks = calls; item.distinctOrdinals = ordinals.Count;
            item.endingFlights = RoyalWinSequence.ActiveCoinFlights;
            item.endingLeases = ReferenceCoinParticles.ActiveCount; item.endingParticles = ReferenceCoinParticles.ParticleCount;
            item.endingCoinVoices = SampleAudio(item);
            Check(calls == (cancel ? 0 : 10), name + ": " + calls + " arrival callbacks, expected " + (cancel ? 0 : 10));
            if (!cancel) Check(ordinals.Count == 10, name + ": each arrival ordinal occurs exactly once");
            Check(item.endingFlights == 0 && item.endingLeases == 0 && item.endingParticles == 0, name + ": flights and particle tail retire within two seconds");
            if (!cancel) Check(item.peakParticles > 0 && item.peakBakedVertices > 0, name + ": native particles produced visible UI mesh vertices");
            if (!cancel && item.soundEnabled) Check(item.peakAppearVoices > 0 && item.peakCounterVoices > 0 && item.peakCollectVoices > 0, name + ": exact appear, counter and collect clips played through live AudioSources");
            if (!item.soundEnabled) report.checks.Add("SKIP " + name + ": live SFX playback check; player's Sound setting is off and was preserved");
            Check(item.endingCoinVoices == 0, name + ": no coin SFX voices remain after cleanup");
            Check(Mathf.Approximately(Time.timeScale, report.initialTimeScale), name + ": simulation clock unchanged");
            preview = null;
        }

        static IEnumerator BoardCollection()
        {
            RoyalReferenceCampaign.Select(true);
            qa.Level = 20;
            yield return qa.OpenLevel();
            qa.Visible = false;
            var lm = LevelManager.THIS;
            Check(RoyalReferenceCampaign.IsBonus && lm.currentLevel == 20, "Opened only the isolated authored coin level 20");
            var coin = lm.squaresArray.Where(s => s && s.item).Select(s => s.item)
                .FirstOrDefault(i => i.GetComponent<RoyalCampaignItem>() && i.GetComponent<RoyalCampaignItem>().Visual == RoyalCampaignBlockers.Art("coin"));
            Check(coin, "Authored bonus board contains a collectable coin");
            var goal = lm.targetObject.FirstOrDefault(t => t.referenceGoal == "Coin");
            Check(goal != null && !goal.Done(), "Coin goal is available before the accepted hit");
            int before = goal.countCollected, money = InitScript.Gems, saved = RoyalSave.GetInt("Gems"), moves = lm.Limit;
            var square = coin.square;
            var item = new Case { name = "actual-board-coin", requestedFps = Application.targetFrameRate, soundEnabled = RoyalSave.GetInt("Sound", 1) != 0 };
            report.cases.Add(item);
            ReferenceVfxPool.Clear();
            coin.DestroyItem(true); coin.DestroyItem(true);
            Check(goal.countCollected == before + 1, "Repeated contact credits the board coin goal exactly once");
            Check(RoyalCampaignBlockers.HasCoinPresentation(coin), "Accepted coin hit claims native particles instead of generic fruit feedback");
            Check(square.item != coin, "Accepted coin hit releases its square through the existing destruction path");
            float start = Time.realtimeSinceStartup, last = start, sum = 0;
            while (Time.realtimeSinceStartup - start < 2)
            {
                yield return null;
                float now = Time.realtimeSinceStartup, dt = now - last; last = now;
                item.frames++; sum += dt; item.maxDelta = Mathf.Max(item.maxDelta, dt);
                item.peakParticleLeases = Mathf.Max(item.peakParticleLeases, ReferenceVfxPool.Active);
                item.peakParticles = Mathf.Max(item.peakParticles, ReferenceVfxPool.ParticleCount);
                SampleAudio(item);
            }
            item.seconds = Time.realtimeSinceStartup - start; item.meanDelta = sum / Mathf.Max(1, item.frames);
            item.effectiveFps = item.frames / Mathf.Max(.001f, item.seconds);
            item.endingLeases = ReferenceVfxPool.Active; item.endingParticles = ReferenceVfxPool.ParticleCount; item.endingCoinVoices = SampleAudio(item);
            Check(item.peakParticles > 0, "Actual accepted board hit emitted native coin particles");
            Check(item.endingLeases == 0 && item.endingParticles == 0, "Board coin particles retire within two seconds");
            Check(!coin, "Original coin object completes its existing destruction lifecycle");
            Check(InitScript.Gems == money && RoyalSave.GetInt("Gems") == saved && lm.Limit == moves, "One board coin hit changes no currency or move budget");
            if (item.soundEnabled) Check(item.peakCollectVoices > 0, "Board coin plays the exact collect clip through a live AudioSource");
            else report.checks.Add("SKIP actual-board-coin: live SFX playback check; player's Sound setting is off and was preserved");
            Check(item.endingCoinVoices == 0, "Board coin leaves no active coin SFX voice");
        }

        static IEnumerator Capture()
        {
            Application.targetFrameRate = 30;
            string frames = Path.Combine(Validation, "Frames-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));
            Directory.CreateDirectory(frames);
            preview = qa.StartCoroutine(RoyalWinSequence.PreviewCoins(new Vector2(0, -190), HomeCoinTarget(), true));
            float start = Time.realtimeSinceStartup, next = start;
            int frame = 0;
            int width = 540, height = Mathf.Max(1, Mathf.RoundToInt(540f * Screen.height / Mathf.Max(1, Screen.width)));
            var target = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
            var pixels = new Texture2D(width, height, TextureFormat.RGB24, false);
            try
            {
                while (frame < 90 && Time.realtimeSinceStartup - start < 3)
                {
                    yield return new WaitForEndOfFrame();
                    float now = Time.realtimeSinceStartup;
                    if (now < next) continue;
                    var screenshot = ScreenCapture.CaptureScreenshotAsTexture();
                    var previous = RenderTexture.active;
                    try
                    {
                        Graphics.Blit(screenshot, target);
                        RenderTexture.active = target;
                        pixels.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                        pixels.Apply(false, false);
                        File.WriteAllBytes(Path.Combine(frames, "coin-" + frame.ToString("D3") + ".jpg"), pixels.EncodeToJPG(85));
                    }
                    finally { RenderTexture.active = previous; UnityEngine.Object.Destroy(screenshot); }
                    report.captureTimes.Add(frame + "," + (now - start).ToString("F6", System.Globalization.CultureInfo.InvariantCulture));
                    frame++;
                    // Late rendered frames keep their actual times; capture never invents 30 FPS.
                    next = start + (Mathf.Floor((now - start) * 30) + 1) / 30f;
                }
            }
            finally { RenderTexture.ReleaseTemporary(target); UnityEngine.Object.Destroy(pixels); }
            report.captureFrames = frame; report.captureSeconds = Time.realtimeSinceStartup - start;
            File.WriteAllLines(Path.Combine(frames, "timestamps.csv"), new[] { "frame,realtime_seconds" }.Concat(report.captureTimes));
            File.WriteAllText(Path.Combine(frames, "README.txt"), "Silent visual capture. JPEG frames are " + width + "x" + height + "; timestamps.csv records actual realtime frame times. Sound and music settings were preserved.\n");
            yield return new WaitForSecondsRealtime(2);
            RoyalWinSequence.CancelCoinPreview(); preview = null;
            Check(frame > 0, "Captured up to 90 rendered JPEG frames with measured realtime timestamps: " + frames);
        }

        static IEnumerator Restore()
        {
            try
            {
                if (qa && RoyalSave.Testing) yield return qa.EndSession();
                yield return null;
                if (qa) qa.Visible = oldVisible;
                report.normalRuntimeCoinsAfter = InitScript.Gems;
                report.normalSavedCoinsAfter = PlayerPrefs.GetInt("Gems"); report.normalSavedLivesAfter = PlayerPrefs.GetInt("Lifes");
                report.profileRestored = !RoyalSave.Testing && report.normalRuntimeCoinsAfter == report.normalRuntimeCoinsBefore && report.normalSavedCoinsAfter == report.normalSavedCoinsBefore;
                report.checks.Add((report.profileRestored ? "PASS " : "FAIL ") + "Normal runtime and saved coin balance restored after isolated checks");
                if (normalProfile != null)
                {
                    var after = ProfileSnapshot();
                    var changed = normalProfile.Where(pair => !after.TryGetValue(pair.Key, out var value) || value != pair.Value).Select(pair => pair.Key).ToArray();
                    report.profileRestored &= changed.Length == 0;
                    report.checks.Add((changed.Length == 0 ? "PASS " : "FAIL ") + "Normal economy, level, score, booster and preference keys preserved" + (changed.Length == 0 ? "" : ": " + string.Join(", ", changed)));
                }
                report.passed &= report.profileRestored && report.errors.Count == 0;
            }
            finally
            {
                Application.logMessageReceived -= Log;
                report.status = report.passed ? "PASS" : "FAIL";
                running = false; Write();
            }
        }
    }
}
#endif
