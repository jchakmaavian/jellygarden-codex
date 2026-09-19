// Local editor bridge for repeatable rocket and TNT integration checks.
using System;
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
        static void PortraitGameView()
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
            int index = Array.FindIndex(labels, text => text.Contains("King Leo VFX portrait"));
            if (index < 0)
            {
                var sizeType = assembly.GetType("UnityEditor.GameViewSize");
                var kind = assembly.GetType("UnityEditor.GameViewSizeType");
                var size = Activator.CreateInstance(sizeType, new[] { Enum.Parse(kind, "FixedResolution"),
                    (object)StoryWindow.Width, StoryWindow.Height, "King Leo VFX portrait" });
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
                UnityEngine.Object.FindAnyObjectByType<StoryRocketValidation>() != null)
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
                File.WriteAllText(Response, "ok " + command + " playing=" + EditorApplication.isPlaying +
                    " scene=" + SceneManager.GetActiveScene().name +
                    " paused=" + EditorApplication.isPaused + " timeScale=" + Time.timeScale + " screen=" + Screen.width + "x" + Screen.height +
                    " state=" + (LevelManager.THIS != null ? LevelManager.THIS.gameStatus.ToString() : "no manager"));
            }
            catch (Exception e) { File.WriteAllText(Response, e.ToString()); }
        }
    }
}
