using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JellyGarden.Scripts;
using RoyalThrone.Presentation;
using RoyalThrone.Testing;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalThrone.Editor
{
    // Explicit checks for the five replacement environments. Never runs automatically on import.
    public static class RoyalAreaRedesignChecks
    {
        const string Output = "Design/Areas/RedesignV1/Unity/";
        const string ArtRoot = "RedesignV1/";
        static readonly int[][] Costs = {
            new[] { 1, 1, 2, 2, 2, 2 }, new[] { 1, 2, 2, 2, 2, 3 },
            new[] { 2, 2, 2, 2, 2, 2, 3 }, new[] { 2, 2, 2, 2, 2, 2, 3, 3 },
            new[] { 2, 2, 2, 2, 2, 3, 3, 3 }
        };
        static readonly string[] SaveKeys = { RoyalAreas.CurrentKey, RoyalAreas.DoneKey, RoyalAreas.SpentKey,
            RoyalAreas.EnteredKey, "Gems", "Stripes", "Packages", "Colorful_bomb", "OpenLevelTest", "Lifes" };
        static readonly string[] FloatSaveKeys = { "RestLifeTimer" };
        static readonly string[] StringSaveKeys = { "DateOfExit", InitScript.NextLifeKey };
        static readonly List<string> lines = new List<string>();
        static string report;
        static bool running;
        [Serializable] sealed class Catalog { public RoyalAreaInfo[] areas; }

        static void Begin(string file)
        {
            RoyalHomeScreenBuilder.RequireCopy();
            Directory.CreateDirectory(Output); report = Output + file;
            lines.Clear(); lines.Add("Royal Throne area redesign checks " + DateTime.UtcNow.ToString("O"));
            File.WriteAllLines(report, lines);
        }
        static void Check(bool condition, string message)
        {
            lines.Add((condition ? "PASS " : "FAIL ") + message);
            File.WriteAllLines(report, lines);
            if (!condition) throw new InvalidOperationException(message);
        }
        static void Note(string message) { lines.Add(message); File.WriteAllLines(report, lines); }

        [MenuItem("Tools/Royal Throne/Areas/Verify redesigned area assets")]
        public static void RunAssets()
        {
            if (running) throw new InvalidOperationException("Area redesign checks are already running.");
            Begin("asset-checks.txt");
            var json = Resources.Load<TextAsset>("RoyalAreas/areas");
            Check(json, "The runtime area catalogue loads");
            var areas = JsonUtility.FromJson<Catalog>(json.text).areas;
            Check(areas != null && areas.Length == 10, "The ten-area campaign remains present");
            Check(areas.Sum(a => a.tasks.Length) == 87 && areas.Sum(a => a.TotalCost) == 208,
                "Campaign task count and star economy remain 87 tasks / 208 stars");
            var keys = new HashSet<string>();
            for (int n = 0; n < 5; n++)
            {
                var area = areas[n]; string prefix = ArtRoot + "Area" + (n + 1).ToString("00") + "/";
                Check(area.number == n + 1 && area.Ready && !area.placeholder, "Area " + (n + 1) + " has its own completed art contract");
                Check(area.tasks.Select(t => t.cost).SequenceEqual(Costs[n]), "Area " + area.number + " preserves task indices and star costs");
                Check(area.tasks.All(t => !string.IsNullOrWhiteSpace(t.name)) && area.tasks.Select(t => t.name).Distinct().Count() == area.tasks.Length,
                    "Area " + area.number + " has clear, distinct names for each restoration object");
                Check(new[] { area.before, area.after, area.thumbBefore, area.thumbAfter }.All(k => k != null && k.StartsWith(prefix, StringComparison.Ordinal) && keys.Add(k)),
                    "Area " + area.number + " uses unique redesign scenes and thumbnails");
                var before = RequireSprite(area.before); var after = RequireSprite(area.after);
                Check(before.rect.width == after.rect.width && before.rect.height == after.rect.height,
                    "Area " + area.number + " before and after have identical dimensions");
                int w = (int)before.rect.width, h = (int)before.rect.height;
                Check(w == 940 && h == 1672, "Area " + area.number + " has a 940x1672 scene");
                foreach (string key in new[] { area.thumbBefore, area.thumbAfter })
                {
                    var thumbnail = RequireSprite(key);
                    Check(thumbnail.rect.width == 240 && thumbnail.rect.height == 428, "Thumbnail is 240x428: " + key);
                }
                for (int i = 0; i < area.tasks.Length; i++)
                {
                    var task = area.tasks[i]; string label = "Area " + area.number + " task " + (i + 1);
                    Check(task.patch != null && task.patch.StartsWith(prefix, StringComparison.Ordinal) && keys.Add(task.patch), label + " has a unique object sprite");
                    Check(task.x0 >= 0 && task.y0 >= 0 && task.x1 <= 1 && task.y1 <= 1 && task.x0 < task.x1 && task.y0 < task.y1,
                        label + " has valid top-left scene bounds");
                    var sprite = RequireSprite(task.patch); int pw = (int)sprite.rect.width, ph = (int)sprite.rect.height;
                    Check(Mathf.Abs(pw - (task.x1 - task.x0) * w) < 1.1f && Mathf.Abs(ph - (task.y1 - task.y0) * h) < 1.1f,
                        label + " fits its scene bounds without scaling");
                    var pixels = Pixels(sprite.texture);
                    Check(pixels.Any(p => p.a <= 4) && pixels.Any(p => p.a >= 250), label + " contains transparent space and solid object pixels");
                    if (!string.IsNullOrEmpty(task.icon))
                    {
                        Check(task.icon.StartsWith(prefix, StringComparison.Ordinal) && keys.Add(task.icon), label + " has a unique redesign task icon");
                        var icon = RequireSprite(task.icon);
                        Check(icon.rect.width == 256 && icon.rect.height == 256, label + " icon is 256x256");
                    }
                }
            }
            Note("Exact base + layer composition is checked on lossless design sources by the asset pipeline, before WebP and GPU compression.");
            Note("ALL REDESIGN ASSET CHECKS PASSED");
        }

        static Sprite RequireSprite(string key)
        {
            var sprite = RoyalAreas.Art(key); Check(sprite, "Sprite loads: " + key);
            var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(sprite)) as RoyalWebPImporter;
            Check(importer && importer.astcOnMobile && importer.bleedTransparentEdges == Path.GetFileName(key).StartsWith("task-", StringComparison.Ordinal),
                "Mobile compression and alpha-edge import contract: " + key);
            return sprite;
        }
        static Color32[] Pixels(Texture2D source)
        {
            var previous = RenderTexture.active; bool previousSrgb = GL.sRGBWrite;
            var target = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            Texture2D copy = null;
            try
            {
                GL.sRGBWrite = QualitySettings.activeColorSpace == ColorSpace.Linear;
                Graphics.Blit(source, target); RenderTexture.active = target;
                copy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
                copy.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0); copy.Apply(); return copy.GetPixels32();
            }
            finally { RenderTexture.active = previous; GL.sRGBWrite = previousSrgb; RenderTexture.ReleaseTemporary(target); if (copy) UnityEngine.Object.DestroyImmediate(copy); }
        }

        static double startupDeadline;
        [MenuItem("Tools/Royal Throne/Areas/Prepare Home from startup (Play Mode)")]
        public static void PrepareHome()
        {
            RoyalHomeScreenBuilder.RequireCopy();
            if (running || !EditorApplication.isPlaying || EditorApplication.isPaused)
                throw new InvalidOperationException("Enter unpaused Play Mode in main.unity, with no area check running.");
            var home = UnityEngine.Object.FindAnyObjectByType<RoyalHomeScreen>();
            var title = UnityEngine.Object.FindAnyObjectByType<RoyalTitleScreen>();
            if (!title && (!home || !home.IsHomeVisible))
                throw new InvalidOperationException("The normal startup screen or Home must be present; this helper does not load scenes directly.");
            Begin("startup-checks.txt");
            // Focus the existing Game view so the startup's WaitForEndOfFrame can complete.
            EditorApplication.ExecuteMenuItem("Window/General/Game");
            if (title) title.BeginPlay();
            running = true; startupDeadline = EditorApplication.timeSinceStartup + 120;
            EditorApplication.update -= WatchStartup; EditorApplication.update += WatchStartup;
        }
        static void WatchStartup()
        {
            var title = UnityEngine.Object.FindAnyObjectByType<RoyalTitleScreen>();
            var home = UnityEngine.Object.FindAnyObjectByType<RoyalHomeScreen>();
            bool ready = EditorApplication.isPlaying && !title && home && home.IsHomeVisible && home.AreaView && home.AreaView.Scene &&
                RoyalQaService.Instance && RoyalQaService.Instance.Ready;
            if (!ready && EditorApplication.isPlaying && EditorApplication.timeSinceStartup < startupDeadline) return;
            EditorApplication.update -= WatchStartup; running = false;
            Check(ready, "Normal startup reached Home with its area view and QA service ready");
            Note("HOME READY; no scene, area, currency or level keys were set by the preparation helper.");
        }

        [MenuItem("Tools/Royal Throne/Areas/Verify redesigned progression and replay (Play Mode)")]
        public static void RunPlayMode()
        {
            RoyalHomeScreenBuilder.RequireCopy();
            var home = UnityEngine.Object.FindAnyObjectByType<RoyalHomeScreen>(); var qa = RoyalQaService.Instance;
            if (running || !EditorApplication.isPlaying || EditorApplication.isPaused || !home || !home.IsHomeVisible || !home.AreaView || !home.AreaView.Scene ||
                home.AreaView.Building || home.AreaView.Ceremony || home.SceneViewOpen || !qa || !qa.Ready)
                throw new InvalidOperationException("Run with Home visible in unpaused Play Mode and no area checks running.");
            if (RoyalSave.Testing) throw new InvalidOperationException("End the manual QA session before running this isolated check.");
            Begin("play-checks-" + Screen.width + "x" + Screen.height + ".txt"); running = true;
            RoyalRegressionRunner.Run(Play(home, qa));
        }

        static IEnumerator Play(RoyalHomeScreen home, RoyalQaService qa)
        {
            var realKeys = SaveKeys.Concat(new[] { "OpenLevel" })
                .Concat(Enumerable.Range(1, 100).Select(level => string.Format("Level.{0:000}.StarsCount", level)));
            var real = realKeys.ToDictionary(k => k, k => new SavedValue(k, PrefKind.Int));
            foreach (string key in FloatSaveKeys) real[key] = new SavedValue(key, PrefKind.Float);
            foreach (string key in StringSaveKeys) real[key] = new SavedValue(key, PrefKind.String);
            var snapshot = new QaSnapshot(); var scene = home.AreaView.Scene;
            var errors = new List<string>(); Application.LogCallback log = (message, trace, type) => { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors.Add(message); };
            bool restored = false; Application.logMessageReceived += log;
            try
            {
                qa.StartSession(); qa.Visible = false; Check(RoyalSave.Testing, "QA profile isolates progression and reward writes");
                RoyalSave.SetInt(RoyalAreas.CurrentKey, 1); RoyalSave.SetInt(RoyalAreas.EnteredKey, 1);
                RoyalSave.SetInt(RoyalAreas.DoneKey, 0); RoyalSave.SetInt(RoyalAreas.SpentKey, 0);
                home.ClosePanel(); scene.Hold = true;
                int spent = 0, earned = Costs.Sum(c => c.Sum());
                for (int n = 0; n < 5; n++)
                {
                    var area = RoyalAreas.All[n]; Check(area.Ready && !area.placeholder, "Live area " + (n + 1) + " uses redesigned art");
                    if (n > 0)
                    {
                        Check(RoyalAreas.AwaitingEntry && RoyalAreas.Build(0, earned) == RoyalAreas.BuildResult.Unavailable,
                            "Area " + area.number + " waits for Unlock before spending stars");
                        Check(RoyalAreas.Enter() && !RoyalAreas.Enter(), "Area " + area.number + " unlocks once");
                    }
                    scene.ShowCurrent(); home.RefreshProgress(); yield return null;
                    Check(scene.Shown == area && !scene.ShowingRestored, "Area " + area.number + " starts on its empty scene");
                    yield return Shot("area-" + area.number + "-before");
                    Check(RoyalAreas.Build(1, earned) == RoyalAreas.BuildResult.Unavailable && RoyalAreas.Spent == spent, "Area " + area.number + " rejects out-of-order task");
                    Check(RoyalAreas.Build(0, spent) == RoyalAreas.BuildResult.NotEnoughStars, "Area " + area.number + " rejects insufficient stars");
                    for (int task = 0; task < area.tasks.Length; task++)
                    {
                        home.OpenPanel(RoyalAreaView.Tasks); yield return null;
                        var rowIcon = home.GetComponentsInChildren<Image>().FirstOrDefault(image => image.name == "Task object");
                        string iconKey = string.IsNullOrEmpty(area.tasks[task].icon) ? area.tasks[task].patch : area.tasks[task].icon;
                        Check(home.AreaView.RowTask == task && rowIcon && rowIcon.sprite == RoyalAreas.Art(iconKey) &&
                            home.GetComponentsInChildren<Text>().Any(text => text.text == area.tasks[task].name),
                            "Area " + area.number + " task " + (task + 1) + " displays its own name and task icon");
                        if (task == 0) yield return Shot("area-" + area.number + "-task-panel");
                        home.ClosePanel();
                        Check(RoyalAreas.Build(task, earned) == RoyalAreas.BuildResult.Built, "Area " + area.number + " builds task " + (task + 1));
                        spent += area.tasks[task].cost;
                        Check(RoyalAreas.Spent == spent && RoyalAreas.Build(task, earned) == RoyalAreas.BuildResult.AlreadyDone,
                            "Area " + area.number + " task " + (task + 1) + " spends once");
                        yield return scene.Rise(task, .12f);
                        if (task < area.tasks.Length - 1) Check(scene.Patch(task) && scene.Patch(task).enabled && scene.Patch(task).sprite == RoyalAreas.Art(area.tasks[task].patch),
                            "Area " + area.number + " task " + (task + 1) + " (" + area.tasks[task].name + ") is visible as its own correct layer");
                    }
                    Check(RoyalAreas.Complete && scene.ShowingRestored, "Area " + area.number + " settles on the restored scene");
                    // This accelerated fixture emits several build puffs in quick succession. Let their normal lifetime
                    // finish before recording the environment and its Home UI.
                    yield return new WaitForSecondsRealtime(2.2f);
                    home.RefreshProgress(); yield return null;
                    yield return Shot("area-" + area.number + "-after");
                    int coins = InitScript.Gems;
                    Check(RoyalAreas.CollectChest(out var reward) && reward.area == area.number && InitScript.Gems == coins + area.rewardCoins,
                        "Area " + area.number + " chest pays once and advances");
                    Check(!RoyalAreas.CollectChest(out _) && RoyalAreas.Current == area.number + 1, "Repeated chest collection does not pay again");
                    home.AreaView.View(area); scene.Replay(area, true); yield return null;
                    Check(home.AreaView.Viewing && home.SceneViewOpen && home.controls.alpha == 0, "Area " + area.number + " opens its full scene viewer");
                    var seen = new bool[area.tasks.Length];
                    float deadline = Time.unscaledTime + 12;
                    while (scene.Replaying && Time.unscaledTime < deadline)
                    {
                        for (int i = 0; i < seen.Length; i++)
                            if (scene.Patch(i) && scene.Patch(i).enabled && scene.Patch(i).sprite == RoyalAreas.Art(area.tasks[i].patch)) seen[i] = true;
                        yield return null;
                    }
                    Check(!scene.Replaying && scene.Shown == area && scene.ShowingRestored,
                        "Area " + area.number + " replay finishes on its own restored scene");
                    Check(seen.All(value => value), "Area " + area.number + " replay displayed every task's separate object layer");
                    Check(RoyalAreas.Current == area.number + 1 && RoyalAreas.Spent == spent, "Replay leaves saved progress unchanged");
                    yield return Shot("area-" + area.number + "-fullscene");
                    home.AreaView.EndView(); home.ClosePanel(); scene.Hold = true; yield return null;
                    Check(!home.SceneViewOpen && !home.AreaView.Viewing, "Area " + area.number + " viewer returns to Home");
                }
                Check(spent == 74 && RoyalAreas.Available(earned) == 0 && RoyalAreas.Current == 6, "Five areas spend 74 stars and reach Area 6");
                scene.StopReplay(); scene.Hold = false;
                yield return qa.EndSession(); snapshot.Restore(); restored = true;
                Check(!RoyalSave.Testing && real.All(kv => kv.Value.Matches(kv.Key)), "Player save is unchanged after five-area progression and replay");
                Check(snapshot.Matches(), "Original QA progression, currency and life-schedule preferences are restored");
                home.RefreshProgress(); scene.ShowCurrent(); yield return null;
                yield return Shot("player-home-restored");
                Check(errors.Count == 0, "No runtime errors" + (errors.Count == 0 ? "" : ": " + errors[0]));
                Note("ALL REDESIGN PLAY CHECKS PASSED");
            }
            finally
            {
                Application.logMessageReceived -= log; scene.StopReplay(); scene.Hold = false;
                if (!restored) RoyalRegressionRunner.Run(Cleanup(home, qa, snapshot));
                else running = false;
            }
        }
        static IEnumerator Cleanup(RoyalHomeScreen home, RoyalQaService qa, QaSnapshot snapshot)
        {
            try
            {
                if (home && home.AreaView.Viewing) home.AreaView.EndView();
                if (home) home.ClosePanel();
                if (RoyalSave.Testing) yield return qa.EndSession();
                snapshot.Restore();
                if (home) { home.RefreshProgress(); home.AreaView.Scene.ShowCurrent(); }
            }
            finally { running = false; }
        }
        static IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Output + name + "-" + Screen.width + "x" + Screen.height + ".png"); yield return null;
        }
        enum PrefKind { Int, Float, String }
        sealed class SavedValue
        {
            readonly bool present; readonly PrefKind kind; readonly int integer; readonly float number; readonly string text;
            public SavedValue(string key, PrefKind type)
            {
                kind = type; present = UnityEngine.PlayerPrefs.HasKey(key);
                if (kind == PrefKind.Int) integer = UnityEngine.PlayerPrefs.GetInt(key);
                else if (kind == PrefKind.Float) number = UnityEngine.PlayerPrefs.GetFloat(key);
                else text = UnityEngine.PlayerPrefs.GetString(key);
            }
            public bool Matches(string key) => present == UnityEngine.PlayerPrefs.HasKey(key) && (!present ||
                (kind == PrefKind.Int ? integer == UnityEngine.PlayerPrefs.GetInt(key) : kind == PrefKind.Float ? number == UnityEngine.PlayerPrefs.GetFloat(key) : text == UnityEngine.PlayerPrefs.GetString(key)));
            public void Restore(string key)
            {
                if (!present) UnityEngine.PlayerPrefs.DeleteKey(key);
                else if (kind == PrefKind.Int) UnityEngine.PlayerPrefs.SetInt(key, integer);
                else if (kind == PrefKind.Float) UnityEngine.PlayerPrefs.SetFloat(key, number);
                else UnityEngine.PlayerPrefs.SetString(key, text);
            }
        }
        sealed class QaSnapshot
        {
            readonly Dictionary<string, SavedValue> values = new Dictionary<string, SavedValue>();
            public QaSnapshot()
            {
                foreach (string prefix in new[] { "RoyalThrone.QA.v1/", "RoyalThrone.QA.Reference50.v1/" })
                {
                    Remember(prefix + "keys", PrefKind.String);
                    foreach (string key in SaveKeys) { Remember(prefix + "seen/" + key, PrefKind.Int); Remember(prefix + "data/" + key, PrefKind.Int); }
                    foreach (string key in FloatSaveKeys) { Remember(prefix + "seen/" + key, PrefKind.Int); Remember(prefix + "data/" + key, PrefKind.Float); }
                    foreach (string key in StringSaveKeys) { Remember(prefix + "seen/" + key, PrefKind.Int); Remember(prefix + "data/" + key, PrefKind.String); }
                }
            }
            void Remember(string key, PrefKind kind) { values[key] = new SavedValue(key, kind); }
            public bool Matches() => values.All(kv => kv.Value.Matches(kv.Key));
            public void Restore()
            {
                foreach (var item in values) item.Value.Restore(item.Key);
                UnityEngine.PlayerPrefs.Save();
            }
        }
    }
}
