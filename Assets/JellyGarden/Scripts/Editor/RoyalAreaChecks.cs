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
    // The Areas loop in the QA profile, as the reference stages it (Design/RoyalMatchReference/Areas/GAPS.md): one task at a
    // time and in order, short of stars, the build (stars from the counter to Do It, the list closing, the Home controls away,
    // the object rising, the progress streak, the list back on the next task), the chest paying once, the next area (coming
    // soon, and New Area/Unlock with a borrowed-art fixture), the Areas tab and its replay. The real save is compared at the end.
    public static class RoyalAreaChecks
    {
        const string Folder = "Design/Areas/Unity/";
        static readonly List<string> lines = new List<string>();
        static readonly List<string> errors = new List<string>();
        static readonly string[] RealKeys = { RoyalAreas.CurrentKey, RoyalAreas.DoneKey, RoyalAreas.SpentKey, RoyalAreas.EnteredKey, RoyalAreas.LayoutVersionKey, RoyalAreas.MigrationCreditKey, "Gems", "Stripes", "Packages", "Colorful_bomb", "OpenLevel", "Lifes" };
        // BoughtKey: stars bought in the shop count as spendable (2026-10-05); a QA shop test must not change the counts here.
        static readonly string[] AreaKeys = { RoyalAreas.CurrentKey, RoyalAreas.DoneKey, RoyalAreas.SpentKey, RoyalAreas.EnteredKey, RoyalAreas.LayoutVersionKey, RoyalAreas.MigrationCreditKey, RoyalAreas.BoughtKey };

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
        static void Note(string text) { lines.Add("NOTE " + text); File.WriteAllLines(Folder + "checks.txt", lines); }
        // Only ever inside the QA session: outside it these keys are the player's real areas.
        static readonly List<string> addedStars = new List<string>();
        static void ResetQaAreas()
        {
            if (!RoyalSave.Testing) return;
            foreach (var key in AreaKeys) RoyalSave.DeleteKey(key);
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
        static IEnumerator Until(Func<bool> done, float timeout) { float end = Time.unscaledTime + timeout; while (!done() && Time.unscaledTime < end) yield return null; }

        // A tap lands on the control: the first raycast hit at its centre belongs to it (hit-tested a frame after it appeared).
        static void CheckHit(Button button, string what)
        {
            Check(button && button.gameObject.activeInHierarchy, what + " is on screen");
            var corners = new Vector3[4]; ((RectTransform)button.transform).GetWorldCorners(corners);
            var centre = (corners[0] + corners[2]) * .5f;
            var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = centre }, hits);
            Check(hits.Count > 0 && hits[0].gameObject.GetComponentInParent<Button>() == button, what + " receives its tap (hit: " + (hits.Count > 0 ? hits[0].gameObject.name : "none") + ")");
        }
        static int Available(RoyalHomeScreen home) => RoyalAreas.Available(home.Progress.stars);

        static IEnumerator Drive()
        {
            Directory.CreateDirectory(Folder);
            lines.Clear(); errors.Clear(); lines.Add("Royal Throne area checks " + DateTime.UtcNow.ToString("O") + " at " + Screen.width + "x" + Screen.height);
            var real = RealKeys.ToDictionary(k => k, k => UnityEngine.PlayerPrefs.HasKey(k) ? UnityEngine.PlayerPrefs.GetInt(k).ToString() : "-");
            var qa = RoyalQaService.Instance;
            var home = UnityEngine.Object.FindAnyObjectByType<RoyalHomeScreen>();
            var areas = RoyalAreas.All;
            // The fixture for the New Area path borrows Area 1's art for Area 2 in memory only; this restores it.
            var saved = areas.Length > 1 ? new { before = areas[1].before, after = areas[1].after, tb = areas[1].thumbBefore, ta = areas[1].thumbAfter, tasks = areas[1].tasks } : null;
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
                var view = home.AreaView; var scene = view.Scene;
                Check(areas.Length == 10 && areas.Sum(a => a.tasks.Length) == 90 && areas.Sum(a => a.TotalCost) == 208, "Ten areas, 90 tasks, unchanged 208-star catalogue budget");
                Check(areas.All(a => a.tasks.All(t => t.cost >= 1 && t.cost <= 3)), "Every task costs one to three stars");
                Check(areas[0].Ready && areas[0].tasks.Length == 6 && areas[0].TotalCost == 10, "Area 1 has its art and six tasks for ten stars");
                Check(scene.Shown == areas[0] && !scene.ShowingRestored, "A fresh profile's Home shows Area 1 unrestored");
                Check(home.areaLabel.text == "Area 1" && home.areaProgress.text == "0/6", "Area card counts tasks (0/6), not levels");
                Check(home.tabButtons[0].GetComponentInChildren<Text>(true).text == "Areas", "The first tab is Areas");
                int earned = home.Progress.stars;

                // ---- Too few stars: one row, the first task; Do It explains and offers the next level; nothing is spent.
                RoyalSave.SetInt(RoyalAreas.SpentKey, earned); home.RefreshProgress();
                Check(home.starLabel.text == "0", "Star counter shows stars left to spend");
                home.resourceButtons[3].onClick.Invoke(); yield return Frames(2);
                Check(view.Showing == RoyalAreaView.Tasks && view.VisibleTiles == 1 && view.RowTask == 0, "The list shows one row: the first task");
                Check(view.TabClearance >= 0, "The task list sits above the tab bar (" + view.TabClearance.ToString("0") + " px clear)");
                Check(!home.dialogRect.gameObject.activeSelf, "The Home card gives way to the area panel");
                CheckHit(view.DoItButton, "Do It"); CheckHit(view.CloseButton, "The red close");
                yield return Shot("tasks-no-stars");
                view.DoItButton.onClick.Invoke(); yield return Frames(2);
                Check(view.Showing == RoyalAreaView.Short && home.OpenPanelName == RoyalAreaView.Short, "Short of stars opens Not Enough Stars!");
                Check(view.TabClearance >= 0, "Not Enough Stars! sits above the tab bar (" + view.TabClearance.ToString("0") + " px clear)");
                Check(!RoyalAreas.IsDone(0) && RoyalAreas.Spent == earned && Available(home) == 0, "No task is built without enough stars, and the balance stays at zero");
                CheckHit(view.ShortContinueButton, "Continue (to the next level)");
                yield return Shot("short-of-stars");
                // A tap outside the panel closes it (A-05).
                view.OutsideButton.onClick.Invoke(); yield return Frames(1);
                Check(!home.dialog.activeSelf, "A tap outside closes the panel");

                // ---- Ten stars: the chest's bubble, then the first build, with repeated taps.
                RoyalSave.SetInt(RoyalAreas.SpentKey, earned - 10); home.RefreshProgress();
                home.resourceButtons[3].onClick.Invoke(); yield return Frames(2);
                CheckHit(view.ChestButton, "The progress chest");
                view.ChestButton.onClick.Invoke(); yield return Frames(1);
                Check(view.TooltipVisible, "Tapping the chest shows its rewards");
                yield return Shot("chest-rewards");
                view.ChestButton.onClick.Invoke(); yield return Frames(1);
                Check(!view.TooltipVisible, "Tapping it again puts them away");
                yield return Shot("tasks");
                float tap = Time.unscaledTime;
                view.DoItButton.onClick.Invoke();
                for (int i = 0; i < 4; i++) view.DoItButton.onClick.Invoke();   // repeated taps on the same frame
                Check(RoyalAreas.IsDone(0) && Available(home) == 9 && RoyalAreas.Spent == earned - 9, "Do It spends one star, once, however often it is tapped");
                Check(view.Building && home.dialog.activeSelf && !view.DoItButton.interactable, "The list stays while the star is in flight, and Do It is spent");
                // The reference's counter drops 0.13 s after the tap, as the star leaves it.
                yield return Until(() => home.starLabel.text == "9", .6f);
                float departed = Time.unscaledTime - tap;
                Check(home.starLabel.text == "9" && departed < .45f, "The counter counts down as the star leaves it (" + departed.ToString("0.00") + " s after the tap)");
                Check(home.areaProgress.text == "0/6", "The Area card still shows the old count");
                yield return Until(() => !home.dialog.activeSelf, 3);
                float closed = Time.unscaledTime - tap;
                Check(!home.dialog.activeSelf && closed > .6f && closed < 1.8f, "The list closes after the star lands (" + closed.ToString("0.00") + " s after the tap)");
                var layout = home.GetComponent<RoyalThrone.ReferenceGameplay.ReferenceHomeLayout>();
                yield return Seconds(.45f);
                Check(layout && layout.HudAway > .99f, "The Home controls slide off while the object is built");
                // The reference's empty moment: the controls are away and the scene waits without the object; then it rises
                // with a puff (the reference's puff comes 2.4 s after the tap).
                Check(scene.NativePlayer && !scene.NativePlayer.Revealing && scene.NativePlayer.CompletedMask == 0, "The native scene waits a moment before revealing the paid task");
                yield return Until(() => scene.NativePlayer && scene.NativePlayer.Revealing, 1.5f);
                float rose = Time.unscaledTime - tap;
                Check(scene.NativePlayer && scene.NativePlayer.CurrentTask == 0 && scene.Revealing && rose > 1.6f && rose < 2.8f, "The native task reveal starts (" + rose.ToString("0.00") + " s after the tap)");
                yield return Seconds(.5f);
                yield return Shot("building");
                yield return Until(() => layout.HudAway < .01f, 6);
                Check(layout.HudAway < .01f, "The controls come back");
                yield return Until(() => home.areaProgress.text == "1/6", 3);
                Check(home.areaProgress.text == "1/6" && home.starLabel.text == "9", "The streak lands and the Area card moves on (1/6)");
                Check(scene.NativePlayer && !scene.NativePlayer.Revealing && (scene.NativePlayer.CompletedMask & 1) != 0, "The authored reveal settles into its completed pose");
                yield return Until(() => view.Showing == RoyalAreaView.Tasks, 3);
                Check(view.Showing == RoyalAreaView.Tasks, "The list opens again by itself");
                yield return Until(() => !view.Building, 4);
                Check(!view.Building && view.RowTask == 1 && view.DoItButton.interactable, "It moves on to the next task (" + (Time.unscaledTime - tap).ToString("0.0") + " s in all)");
                yield return Shot("next-task");

                // ---- Order: a later task cannot be bought first.
                int spent = RoyalAreas.Spent;
                Check(RoyalAreas.Build(3, earned) == RoyalAreas.BuildResult.Unavailable && RoyalAreas.Spent == spent && !RoyalAreas.IsDone(3), "A later task cannot be built before the next one");

                // ---- The rest of the area; the last task ends in the chest ceremony.
                for (int task = 1; task < 6; task++)
                {
                    if (view.Showing != RoyalAreaView.Tasks) { home.resourceButtons[3].onClick.Invoke(); yield return Frames(2); }
                    Check(view.RowTask == task, "Task " + (task + 1) + " is the one on offer");
                    view.DoItButton.onClick.Invoke(); yield return Frames(1);
                    if (task < 5) { yield return Until(() => !view.Building, 12); Check(!view.Building && RoyalAreas.IsDone(task), "Task " + (task + 1) + " is built"); }
                }
                Check(RoyalAreas.Complete && Available(home) == 0 && RoyalAreas.Spent == earned, "Six tasks spend exactly the area's ten stars");
                yield return Until(() => view.Showing == RoyalAreaView.Tasks && !home.dialogRect.gameObject.activeSelf, 12);
                yield return Until(() => view.Showing == RoyalAreaView.Chest, 12);
                Check(view.Showing == RoyalAreaView.Chest && view.StageStep == 1, "The last task leads to the chest (Tap to Open)");
                yield return Seconds(1.4f);
                yield return Shot("chest-tap-to-open");
                int coins = InitScript.Gems, rockets = RoyalSave.GetInt("Stripes");
                CheckHit(view.StageButton, "The chest stage");
                view.StageButton.onClick.Invoke(); yield return Until(() => view.StageStep == 2, 2);
                Check(InitScript.Gems == coins + areas[0].rewardCoins && RoyalSave.GetInt("Stripes") == rockets + 1, "Opening the chest pays " + areas[0].rewardCoins + " coins and a rocket");
                Check(!RoyalAreas.CollectChest(out _) && InitScript.Gems == coins + areas[0].rewardCoins, "The chest cannot pay twice");
                view.StageButton.onClick.Invoke(); view.StageButton.onClick.Invoke();
                yield return Seconds(1.2f);
                yield return Shot("chest-rewards-open");
                yield return Until(() => view.StageStep == 3 || view.StageStep >= 4, 3);
                Check(InitScript.Gems == coins + areas[0].rewardCoins, "Repeated taps on the chest pay nothing more");
                yield return Until(() => view.StageStep == 4, 4);
                Check(view.StageStep == 4 && home.SceneViewOpen, "Area Completed! shows with the Home controls away");
                yield return Seconds(1.6f);
                yield return Shot("area-completed");
                yield return Until(() => view.StageStep == 5, 6);
                Check(view.StageStep == 5 && scene.ShowingRestored, "Then the restored area on its own, with Continue");
                yield return Seconds(.8f);
                yield return Shot("restored-showcase");
                view.StageButton.onClick.Invoke(); yield return Frames(1);
                var showcaseButton = view.transform.GetComponentsInChildren<Button>(true).FirstOrDefault(b => b.name == "Area showcase");
                if (showcaseButton) showcaseButton.onClick.Invoke();
                yield return Until(() => !view.Ceremony, 3);
                Check(!view.Ceremony && !home.dialog.activeSelf && !home.SceneViewOpen, "Continue returns to Home");
                Check(RoyalAreas.Current == 2 && RoyalAreas.DoneCount == 0 && RoyalAreas.Entered == 1 && RoyalAreas.AwaitingEntry, "Area 2 is next, waiting to be unlocked");
                // ---- No "Coming Soon": an area without its own scene yet is restored on a placeholder scene (Area 1's art).
                Check(areas.All(a => a.Ready), "Every area can be restored (areas without their own scene use a placeholder)");
                Check(!areas[1].placeholder && areas[1].nativeMotion && areas[1].tasks.Length == 7 && areas[1].tasks[0].name == "Lay the carpet" && areas[1].before != areas[0].before,
                    "Area 2 has its own native throne room and seven restoration tasks");

                // ---- New Area and Unlock.
                home.RefreshProgress(); yield return Frames(2);
                Check(home.areaLabel.text == "New Area" && !home.areaProgress.gameObject.activeSelf, "With art, the Area card reads New Area");
                Check(scene.Shown == areas[0] && scene.ShowingRestored, "Home still shows restored Area 1");
                yield return Shot("home-new-area");
                home.resourceButtons[3].onClick.Invoke(); yield return Frames(2);
                Check(view.Showing == RoyalAreaView.Unlocked, "New Area opens New Area!");
                Check(view.TabClearance >= 0, "New Area! sits above the tab bar (" + view.TabClearance.ToString("0") + " px clear)");
                CheckHit(view.UnlockButton, "Unlock");
                yield return Shot("new-area");
                view.UnlockButton.onClick.Invoke(); view.UnlockButton.onClick.Invoke();
                yield return Until(() => !home.dialog.activeSelf, 3);
                Check(RoyalAreas.Entered == 2 && !RoyalAreas.AwaitingEntry && RoyalAreas.Current == 2, "Unlock enters Area 2, once");
                yield return Frames(2);
                Check(scene.Shown == areas[1] && !scene.ShowingRestored && scene.NativePlayer.Area == 2 && home.areaLabel.text == "Area 2" && home.areaProgress.text == "0/7", "Home shows the native throne room and 0/7");
                yield return Shot("area2-unlocked");

                // ---- Persistence: what the profile holds is what the Home shows after a restart (checked from the save).
                Check(RoyalSave.GetInt(RoyalAreas.CurrentKey) == 2 && RoyalSave.GetInt(RoyalAreas.EnteredKey) == 2 && RoyalSave.GetInt(RoyalAreas.SpentKey) == earned, "Area progress is saved in the profile");

                // ---- The Areas tab: newest first, the restored Area 1 with View, which replays it.
                home.tabButtons[0].onClick.Invoke(); yield return Frames(2);
                Check(view.ListVisible && home.SelectedTab == 0, "The Areas tab opens its list");
                Check(view.ListedAreas == 10 && Enumerable.Range(3, 8).All(n => view.CardState(n) == "locked") && view.CardState(2) == "current" && view.CardState(1) == "done",
                    "It lists the eight areas to come (locked), the current one and the restored one (" + view.ListedAreas + ")");
                yield return Shot("areas-tab");
                // It opens on the current area; the restored one is further down, as a player would scroll to it.
                view.ScrollTo(1); yield return Frames(2);
                CheckHit(view.AreaButton(1), "View on restored Area 1");
                yield return Shot("areas-tab-restored");
                view.AreaButton(1).onClick.Invoke(); yield return Frames(2);
                Check(view.Viewing && home.SceneViewOpen && home.controls.alpha == 0 && view.Replaying, "View shows Area 1 full screen and replays it");
                yield return Seconds(1.5f);
                yield return Shot("view-replay");
                CheckHit(view.ViewBackButton, "The view's red close");
                view.ViewBackButton.onClick.Invoke(); yield return Frames(2);
                Check(!view.Viewing && view.ListVisible && home.controls.alpha == 1, "Its close returns to the Areas tab");
                home.ClosePanel(); yield return Frames(1);
                Check(errors.Count == 0, "No runtime errors" + (errors.Count > 0 ? ": " + errors[0] : ""));
                // Leave the QA profile's areas fresh for other suites, then return to the player's profile.
                ResetQaAreas();
                yield return qa.EndSession();
            }
            finally
            {
                Application.logMessageReceived -= Log;
                if (saved != null) { areas[1].before = saved.before; areas[1].after = saved.after; areas[1].thumbBefore = saved.tb; areas[1].thumbAfter = saved.ta; areas[1].tasks = saved.tasks; }
                // A failed check ends here too: the QA session must never outlive the suite (the player's profile comes back).
                if (RoyalSave.Testing) { ResetQaAreas(); home.ClosePanel(); RoyalRegressionRunner.Run(qa.EndSession()); }
            }
            home.RefreshProgress(); home.AreaView.Scene.ShowCurrent();
            yield return Frames(3);
            var after = RealKeys.ToDictionary(k => k, k => UnityEngine.PlayerPrefs.HasKey(k) ? UnityEngine.PlayerPrefs.GetInt(k).ToString() : "-");
            Check(!RoyalSave.Testing && RealKeys.All(k => real[k] == after[k]), "The player's real save is untouched");
            lines.Add("ALL AREA CHECKS PASSED");
            File.WriteAllLines(Folder + "checks.txt", lines);
        }
    }
}
