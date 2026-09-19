// Local editor bridge for repeatable rocket and TNT integration checks.
using System;
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JellyGarden.Scripts.Story.Editor
{
    [InitializeOnLoad]
    static class RocketValidationBridge
    {
        const string Request = "Temp/rocket-validation-command.txt";
        const string Response = "Temp/rocket-validation-response.txt";
        static RocketValidationBridge() { EditorApplication.update += Tick; }
        static EditorWindow captureView;
        static bool previewingPopup;
        static void PortraitGameView(int width = StoryWindow.Width, int height = StoryWindow.Height)
        {
            // The Device Simulator and a second Game view can compete for the
            // render target. Use one ordinary Game view for these captures.
            foreach (var window in Resources.FindObjectsOfTypeAll<EditorWindow>())
                if (window.titleContent.text == "Simulator") window.Close();
            var assembly = typeof(UnityEditor.Editor).Assembly;
            var sizesType = assembly.GetType("UnityEditor.GameViewSizes");
            var singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            var sizes = singleton.GetProperty("instance").GetValue(null);
            var groupType = assembly.GetType("UnityEditor.GameViewSizeGroupType");
            var group = sizesType.GetMethod("GetGroup").Invoke(sizes, new[] { Enum.Parse(groupType, "Standalone") });
            var labels = (string[])group.GetType().GetMethod("GetDisplayTexts").Invoke(group, null);
            string label = "King Leo review " + width + "x" + height;
            int index = Array.FindIndex(labels, text => text.Contains(label));
            if (index < 0)
            {
                var sizeType = assembly.GetType("UnityEditor.GameViewSize");
                var kind = assembly.GetType("UnityEditor.GameViewSizeType");
                var size = Activator.CreateInstance(sizeType, new[] { Enum.Parse(kind, "FixedResolution"),
                    (object)width, height, label });
                group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { size });
                index = labels.Length;
            }
            var viewType = assembly.GetType("UnityEditor.GameView");
            var view = EditorWindow.GetWindow(viewType);
            viewType.GetProperty("selectedSizeIndex").SetValue(view, index);
            captureView = view;
            view.Show();
            view.Focus();
        }

        static void Tick()
        {
            if (EditorApplication.isPlaying && captureView != null &&
                (UnityEngine.Object.FindAnyObjectByType<StoryRocketValidation>() != null ||
                 UnityEngine.Object.FindAnyObjectByType<StoryRoyalBoardValidation>() != null || previewingPopup))
            {
                captureView.Repaint();
                EditorApplication.QueuePlayerLoopUpdate();
            }
            if (EditorApplication.isCompiling || !File.Exists(Request)) return;
            string command = File.ReadAllText(Request).Trim();
            File.Delete(Request);
            try
            {
                if (command == "play") EditorApplication.isPlaying = true;
                else if (command == "stop") EditorApplication.isPlaying = false;
                else if (command == "validate")
                {
                    PortraitGameView();
                    StoryRocketValidation.Run();
                }
                else if (command == "royal-board")
                {
                    PortraitGameView();
                    StoryRoyalBoardValidation.Run(PortraitGameView);
                }
                else if (command == "popup-preview")
                {
                    if (!EditorApplication.isPlaying) throw new InvalidOperationException("Enter Play Mode first.");
                    PortraitGameView();
                    StoryHomeScreen.Open(1);
                    StoryLevelPopup.Open(7, 1, null);
                    StoryLevelPopup.Instance.StartCoroutine(CapturePopups());
                }
                File.WriteAllText(Response, "ok " + command + " playing=" + EditorApplication.isPlaying +
                    " scene=" + SceneManager.GetActiveScene().name +
                    " paused=" + EditorApplication.isPaused + " timeScale=" + Time.timeScale + " screen=" + Screen.width + "x" + Screen.height +
                    " state=" + (LevelManager.THIS != null ? LevelManager.THIS.gameStatus.ToString() : "no manager"));
            }
            catch (Exception e) { File.WriteAllText(Response, e.ToString()); }
        }

        static IEnumerator CapturePopups()
        {
            const string output = "art-review/gameplay-redesign-v1/implementation";
            var owned = StoryBoosters.OwnedSource;
            previewingPopup = true;
            StoryBoosters.OwnedSource = boost => 3;
            try
            {
                Directory.CreateDirectory(output);
                foreach (int level in new[] { 7, 17 })
                {
                    StoryLevelPopup.Open(level, 1, null);
                    yield return new WaitForSecondsRealtime(1);
                    yield return new WaitForEndOfFrame();
                    StoryWindow.Capture(output + "/level-start-" + level + "-393x852.png");
                }
                File.WriteAllText(output + "/popup-preview.txt", "Captured live level-start popups for levels 7 and 17. Inventory was previewed at three per booster without writing player preferences.\n");
            }
            finally
            {
                previewingPopup = false;
                StoryBoosters.OwnedSource = owned;
                StoryBoosters.ClearSelection();
                StoryLevelPopup.Close();
            }
        }
    }
}
