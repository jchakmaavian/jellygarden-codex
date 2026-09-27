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
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RoyalThrone.Editor
{
    // The Areas loop in the QA profile: stars run short, tasks spend stars and show their objects, the restored area pays its
    // chest and moves on, the list shows every area and opens a restored one full screen. The player's real save is compared
    // before and after.
    public static class RoyalAreaChecks
    {
        const string Folder = "Design/Areas/Unity/";
        static readonly List<string> lines = new List<string>();
        static readonly List<string> errors = new List<string>();
        static readonly string[] RealKeys = { RoyalAreas.CurrentKey, RoyalAreas.DoneKey, RoyalAreas.SpentKey, "Gems", "Stripes", "Packages", "Colorful_bomb", "OpenLevel", "Lifes" };

        [MenuItem("Tools/Royal Throne/Areas/Check areas (Play Mode, QA profile)")]
        public static void Run()
        {
            if (!EditorApplication.isPlaying) throw new InvalidOperationException("Run in Play Mode with Home visible.");
            RoyalRegressionRunner.Run(Drive());
        }

        static void Check(bool ok, string text)
        {
            lines.Add((ok ? "PASS " : "FAIL ") + text);
            File.WriteAllLines(Folder + "checks.txt", lines);
            if (!ok) throw new InvalidOperationException(text);
        }
        // Only ever inside the QA session: outside it these keys are the player's real areas.
        static readonly List<string> addedStars = new List<string>();
        static void ResetQaAreas()
        {
            if (!RoyalSave.Testing) return;
            foreach (var key in new[] { RoyalAreas.CurrentKey, RoyalAreas.DoneKey, RoyalAreas.SpentKey }) RoyalSave.DeleteKey(key);
            foreach (var key in addedStars) RoyalSave.DeleteKey(key);
            addedStars.Clear();
        }
        // The QA profile needs at least one area's worth of level stars; any it lacks are added and removed again afterwards.
        static void EnsureQaStars(int wanted)
        {
            if (!RoyalSave.Testing) return;
            int have = 0;
            for (int level = 1; level <= 100; level++) have += Mathf.Clamp(RoyalSave.GetInt(string.Format("Level.{0:000}.StarsCount", level)), 0, 3);
            for (int level = 1; level <= 100 && have < wanted; level++)
            {
                string key = string.Format("Level.{0:000}.StarsCount", level);
                int stars = Mathf.Clamp(RoyalSave.GetInt(key), 0, 3);
                if (stars >= 3) continue;
                if (!RoyalSave.HasKey(key)) addedStars.Add(key);
                RoyalSave.SetInt(key, 3); have += 3 - stars;
            }
        }
        static void Log(string message, string trace, LogType type) { if (type == LogType.Error || type == LogType.Exception) errors.Add(message); }
        static IEnumerator Shot(string name) { yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(Folder + name + "-" + Screen.width + "x" + Screen.height + ".png"); yield return null; }
        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }
        static IEnumerator Seconds(float s) { float end = Time.unscaledTime + s; while (Time.unscaledTime < end) yield return null; }

        static void CheckHit(Button button)
        {
            var corners = new Vector3[4]; ((RectTransform)button.transform).GetWorldCorners(corners);
            var centre = (corners[0] + corners[2]) * .5f;
            var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = centre }, hits);
            Check(hits.Count > 0 && hits[0].gameObject.GetComponentInParent<Button>() == button, button.name + " receives its tap");
        }

        static IEnumerator Drive()
        {
            Directory.CreateDirectory(Folder);
            lines.Clear(); errors.Clear(); lines.Add("Royal Throne area checks " + DateTime.UtcNow.ToString("O"));
            var real = RealKeys.ToDictionary(k => k, k => UnityEngine.PlayerPrefs.HasKey(k) ? UnityEngine.PlayerPrefs.GetInt(k).ToString() : "-");
            var qa = RoyalQaService.Instance;
            var home = UnityEngine.Object.FindAnyObjectByType<RoyalHomeScreen>();
            Application.logMessageReceived += Log;
            yield return null;
            try
            {
                Check(home && home.IsHomeVisible && home.AreaView != null, "Home is visible with its area view");
                qa.StartSession(); qa.Visible = false;
                Check(RoyalSave.Testing, "QA profile active");
                ResetQaAreas(); EnsureQaStars(12);
                home.ClosePanel(); home.RefreshProgress();
                yield return Frames(3);
                var view = home.AreaView; var scene = view.Scene; var areas = RoyalAreas.All;
                Check(areas.Length == 10 && areas.Sum(a => a.tasks.Length) == 87 && areas.Sum(a => a.TotalCost) == 208, "Ten areas, 87 tasks, 208 stars in the catalogue");
                Check(areas.All(a => a.tasks.All(t => t.cost >= 1 && t.cost <= 3)), "Every task costs one to three stars");
                Check(areas[0].Ready && areas[0].tasks.Length == 6 && areas[0].TotalCost == 10, "Area 1 has its art and six tasks for ten stars");
                Check(scene.Shown == areas[0] && !scene.ShowingRestored && home.transform.Find("Castle framing/Royal blue courtyard").GetComponent<Image>().sprite == RoyalAreas.Art(areas[0].before),
                    "A fresh profile's Home shows Area 1 unrestored");
                Check(home.areaLabel.text == "Area 1" && home.areaProgress.text == "0/6", "Area card counts tasks (0/6), not levels");
                int earned = home.Progress.stars;

                // Too few stars: nothing is built, and the next level is offered.
                RoyalSave.SetInt(RoyalAreas.SpentKey, earned); home.RefreshProgress();
                Check(home.starLabel.text == "0", "Star counter shows stars left to spend");
                home.areaButton.onClick.Invoke(); yield return null;
                Check(view.TasksVisible && view.VisibleTiles == 6 && home.OpenPanelName == RoyalAreaView.Tasks, "Area dialog lists the six tasks");
                CheckHit(view.TaskButton(0)); CheckHit(view.SecondaryButton); CheckHit(home.dialogButton);
                yield return Shot("tasks-no-stars");
                view.TaskButton(0).onClick.Invoke(); yield return null;
                Check(home.OpenPanelName == RoyalAreaView.Short && home.dialogButtonLabel.text.StartsWith("Play level"), "Too few stars explains and offers the next level");
                Check(!RoyalAreas.IsDone(0) && RoyalAreas.Spent == earned, "No task is built without enough stars");
                home.ClosePanel(); yield return null;

                // Ten stars to spend: build all six tasks.
                RoyalSave.SetInt(RoyalAreas.SpentKey, earned - 10); home.RefreshProgress();
                home.areaButton.onClick.Invoke(); yield return null;
                yield return Shot("tasks");
                view.TaskButton(0).onClick.Invoke(); yield return null;
                Check(RoyalAreas.IsDone(0) && RoyalAreas.Available(earned) == 9, "Building a one-star task spends one star");
                Check(!home.dialog.activeSelf && scene.Patch(0).enabled && scene.Patch(0).sprite, "The dialog closes and the task's object appears");
                yield return Seconds(.5f);
                Check(scene.Patch(0).color.a > .99f, "The object finishes fading in");
                Check(home.starLabel.text == "9" && home.areaProgress.text == "1/6", "Star counter and Area card update");
                yield return Shot("one-task");
                for (int i = 1; i < 6; i++)
                {
                    home.areaButton.onClick.Invoke(); yield return null;
                    Check(!view.TaskButton(i - 1).interactable, "A built task cannot be bought again (" + (i) + ")");
                    view.TaskButton(i).onClick.Invoke(); yield return Frames(2);
                    if (i == 3) yield return Shot("four-tasks");
                }
                Check(RoyalAreas.Complete && RoyalAreas.Available(earned) == 0 && RoyalAreas.Spent == earned, "Six tasks spend exactly the area's ten stars");
                float deadline = Time.unscaledTime + 4;
                while (home.OpenPanelName != RoyalAreaView.Chest && Time.unscaledTime < deadline) yield return null;
                Check(home.OpenPanelName == RoyalAreaView.Chest && home.dialogButtonLabel.text == "Collect", "A restored area offers its chest");
                Check(scene.ShowingRestored && home.transform.Find("Castle framing/Royal blue courtyard").GetComponent<Image>().sprite == RoyalAreas.Art(areas[0].after), "The restored scene replaces the objects");
                yield return Shot("chest");
                int coins = InitScript.Gems, rockets = RoyalSave.GetInt("Stripes");
                home.dialogButton.onClick.Invoke(); yield return Frames(2);
                Check(InitScript.Gems == coins + areas[0].rewardCoins && RoyalSave.GetInt("Stripes") == rockets + 1, "The chest pays " + areas[0].rewardCoins + " coins and a rocket");
                Check(RoyalAreas.Current == 2 && RoyalAreas.DoneCount == 0, "Area 2 becomes current with no tasks built");
                bool secondReady = areas[1].Ready;
                Check(home.OpenPanelName == (secondReady ? RoyalAreaView.Unlocked : RoyalAreaView.Soon), secondReady ? "Area 2 is welcomed" : "Without Area 2's art the game says more areas are coming");
                if (!secondReady)
                {
                    Check(scene.Shown == areas[0] && scene.ShowingRestored, "Home keeps restored Area 1 until Area 2's art exists");
                    Check(home.areaLabel.text == "Area 2" && home.areaProgress.text == "Soon", "Area card shows Area 2 as coming soon");
                }
                home.ClosePanel(); yield return null;

                // Every area in the list; a restored one opens full screen and Back returns.
                home.OpenPanel(RoyalAreaView.List); yield return null;
                Check(view.ListVisible && Enumerable.Range(1, 10).All(n => view.AreaButton(n) && view.AreaButton(n).gameObject.activeSelf), "The areas list shows all ten areas");
                Check(view.AreaButton(1).interactable && view.AreaButton(3).interactable == false, "Restored areas can be visited; later ones are locked");
                yield return Shot("areas");
                CheckHit(view.AreaButton(1));
                view.AreaButton(1).onClick.Invoke(); yield return Frames(2);
                Check(view.Viewing && home.SceneViewOpen && home.controls.alpha == 0 && !home.controls.interactable, "A restored area opens full screen with the Home controls put away");
                CheckHit(view.ViewBackButton);
                yield return Shot("visit");
                view.ViewBackButton.onClick.Invoke(); yield return Frames(2);
                Check(!view.Viewing && home.OpenPanelName == RoyalAreaView.List && home.controls.alpha == 1, "Back returns to the areas list with the controls restored");
                home.ClosePanel(); yield return null;
                Check(RoyalSave.GetInt(RoyalAreas.CurrentKey) == 2 && RoyalAreas.Spent == earned, "Area progress is saved in the profile");
                Check(errors.Count == 0, "No runtime errors" + (errors.Count > 0 ? ": " + errors[0] : ""));
                // Leave the QA profile's areas fresh for other suites, then return to the player's profile.
                ResetQaAreas();
                yield return qa.EndSession();
                home.RefreshProgress();
            }
            finally
            {
                Application.logMessageReceived -= Log;
                if (RoyalSave.Testing) { ResetQaAreas(); RoyalRegressionRunner.Run(qa.EndSession()); }
            }
            yield return Frames(3);
            var after = RealKeys.ToDictionary(k => k, k => UnityEngine.PlayerPrefs.HasKey(k) ? UnityEngine.PlayerPrefs.GetInt(k).ToString() : "-");
            Check(!RoyalSave.Testing && RealKeys.All(k => real[k] == after[k]), "The player's real save is untouched");
            lines.Add("ALL AREA CHECKS PASSED");
            File.WriteAllLines(Folder + "checks.txt", lines);
        }
    }
}
