#if UNITY_EDITOR && !ROYAL_DISABLE_QA
using System;
using System.Collections;
using System.IO;
using System.Linq;
using JellyGarden.Scripts;
using JellyGarden.Scripts.Juice;
using RoyalThrone.Presentation;
using RoyalThrone.Testing;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace RoyalThrone.Editor
{
    public static class RoyalTalesBranding
    {
        const string Main = "Assets/JellyGarden/Scenes/main.unity";
        const string Logo = "Assets/JellyGarden/Textures/menu/main_menu/logo.png";
        const string Folder = "Design/Branding/RoyalTales-Verification/";
        static bool running;
        public static void PrepareVerification()
        {
            Install(); OpenStartup();
            SessionState.SetBool("RoyalTales.Branding.Armed", true);
            EditorApplication.update -= AwaitPlay; EditorApplication.update += AwaitPlay;
            EditorApplication.isPaused = false; EditorApplication.EnterPlaymode();
        }
        [InitializeOnLoadMethod]
        static void ResumeVerification()
        {
            if (!SessionState.GetBool("RoyalTales.Branding.Armed", false)) return;
            EditorApplication.update -= AwaitPlay;
            EditorApplication.update += AwaitPlay;
        }
        static void AwaitPlay()
        {
            if (!SessionState.GetBool("RoyalTales.Branding.Armed", false)) { EditorApplication.update -= AwaitPlay; return; }
            if (!Application.isPlaying || EditorApplication.isCompiling) return;
            EditorApplication.update -= AwaitPlay;
            SessionState.SetBool("RoyalTales.Branding.Armed", false); Verify();
        }
        [MenuItem("Tools/Royal Tales/Install logo animation")]
        public static void Install()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Install branding in Edit Mode.");
            var scene = SceneManager.GetSceneByPath(Main); bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(Main, OpenSceneMode.Additive);
            try
            {
                var title = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<RoyalTitleScreen>(true)).Single();
                if (title.name == "Royal Tales UI" && title.loadingLogo.name == "Royal Tales logo" && title.loadingLogo.GetComponent<RoyalLogoMotion>()) return;
                title.name = "Royal Tales UI"; title.loadingLogo.name = "Royal Tales logo";
                if (!title.loadingLogo.GetComponent<RoyalLogoMotion>()) title.loadingLogo.gameObject.AddComponent<RoyalLogoMotion>();
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
                Debug.Log("Royal Tales loading logo animation installed; existing scene references preserved.");
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }
        [MenuItem("Tools/Royal Tales/Open startup for verification")]
        public static void OpenStartup()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Open startup in Edit Mode.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save the current scene before opening startup for QA.");
            EditorSceneManager.OpenScene(Main);
        }
        static void Check(bool ok, string message)
        {
            File.AppendAllText(Folder + "checks.txt", (ok ? "PASS " : "FAIL ") + message + "\n");
            if (!ok) throw new InvalidOperationException(message);
        }
        static void Capture(string name)
        {
            var image = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Folder + name + ".png", image.EncodeToPNG()); Object.Destroy(image);
        }
        [MenuItem("Tools/Royal Tales/Verify branding in Play Mode")]
        public static void Verify()
        {
            if (running) return;
            running = true; SessionState.SetBool("RoyalTales.Branding.Armed", false);
            EditorApplication.update -= AwaitPlay;
            RoyalRegressionRunner.Run(GuardedSuite());
        }
        static IEnumerator GuardedSuite()
        {
            try { yield return Suite(); }
            finally { running = false; }
        }
        static IEnumerator Suite()
        {
            Directory.CreateDirectory(Folder); File.WriteAllText(Folder + "checks.txt", "Royal Tales branding and animation checks\n");
            Check(Application.isPlaying, "Unity Play Mode");
            var logo = AssetDatabase.LoadAssetAtPath<Sprite>(Logo);
            Check(logo && AssetDatabase.AssetPathToGUID(Logo) == "6c0b96a44d47a0846b80777f8bb71d51", "Logo sprite GUID preserved");
            Check(File.ReadAllBytes(Logo).SequenceEqual(File.ReadAllBytes("Design/Branding/royal-tales-splash-logo-v1.png")), "Runtime PNG exactly matches the generated Royal Tales master");
            int loadingFrames = 0; bool moved = false, captured = false; Vector3 first = Vector3.zero;
            float deadline = Time.realtimeSinceStartup + 120;
            RoyalHomeScreen home = null;
            while (Time.realtimeSinceStartup < deadline)
            {
                yield return new WaitForEndOfFrame();
                var title = Object.FindAnyObjectByType<RoyalTitleScreen>();
                if (title && title.Phase == RoyalTitleScreen.StartupPhase.Loading)
                {
                    CheckLoadingBindings(title, logo);
                    if (loadingFrames++ == 0) first = title.loadingLogo.localScale;
                    moved |= Vector3.Distance(first, title.loadingLogo.localScale) > .0001f;
                    if (!captured && title.loadingGroup.alpha >= .99f && loadingFrames > 12) { Capture("loading"); captured = true; }
                }
                home = Object.FindAnyObjectByType<RoyalHomeScreen>();
                if (!title && home && home.IsHomeVisible) break;
            }
            Check(home && home.IsHomeVisible, "Actual startup reaches the homepage");
            Check(loadingFrames > 0, "Royal Tales logo appears during actual asynchronous loading");
            Check(RoyalSave.GetInt("RoyalThrone.HomeMotion", 1) == 0 || moved, "Logo entrance and breathing motion advance during loading");
            Capture("home");
            var qa = RoyalQaService.Instance; int coins = InitScript.Gems, lives = InitScript.lifes;
            try
            {
                qa.StartSession(); yield return qa.Practice(); qa.Visible = false;
                var lm = LevelManager.THIS; lm.gameStatus = GameState.PreWinAnimations;
                // The real state transition already starts the level's celebration coroutine.
                bool shown = false, correct = true, winCaptured = false; int frames = 0; float shownAt = 0;
                deadline = Time.realtimeSinceStartup + 10;
                while (Time.realtimeSinceStartup < deadline)
                {
                    yield return new WaitForEndOfFrame();
                    var pieces = Object.FindObjectsByType<Image>().Where(i => i.name == "Logo crown" || i.name == "Logo royal" || i.name == "Logo tales").ToArray();
                    if (pieces.Length == 3)
                    {
                        if (!shown) shownAt = Time.unscaledTime;
                        shown = true; correct &= pieces.All(i => i.sprite && i.sprite.texture == logo.texture);
                        frames++;
                        if (!winCaptured && Time.unscaledTime - shownAt >= 1.4f) { Capture("win-celebration"); winCaptured = true; }
                    }
                    else if (shown) break;
                }
                Check(shown && correct && winCaptured, "Win animation reveals crown, ROYAL and TALES from the new master");
                Check(!Object.FindObjectsByType<Image>().Any(i => i.name == "Logo throne"), "Old THRONE word is absent from the live win animation");
                lm.gameStatus = GameState.Playing;
                // Verify reduced motion using the isolated QA preference namespace.
                var node = new GameObject("QA Royal Tales motion", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                node.GetComponent<Image>().sprite = logo;
                node.GetComponent<RectTransform>().sizeDelta = Vector2.one * 400;
                node.AddComponent<RoyalLogoMotion>(); RoyalSave.SetInt("RoyalThrone.HomeMotion", 0);
                yield return null; yield return new WaitForEndOfFrame();
                Check(node.transform.localScale == Vector3.one && node.GetComponentsInChildren<Image>().Skip(1).All(i => i.color.a == 0 && !i.raycastTarget), "Reduced motion keeps the logo still and hides decorative gleams");
                Object.Destroy(node);
                yield return qa.EndSession();
                Check(!RoyalSave.Testing && InitScript.Gems == coins && InitScript.lifes == lives, "Normal player data restored");
                File.AppendAllText(Folder + "checks.txt", "ALL CHECKS PASSED\n");
            }
            finally { if (RoyalSave.Testing) RoyalRegressionRunner.Run(qa.EndSession()); }
        }
        static void CheckLoadingBindings(RoyalTitleScreen title, Sprite logo)
        {
            if (title.loadingLogo.GetComponent<Image>().sprite != logo || !title.loadingLogo.GetComponent<RoyalLogoMotion>())
                throw new InvalidOperationException("Loading screen has the wrong logo or no motion component.");
        }
    }
}
#endif
