using PlayerPrefs = RoyalThrone.Testing.RoyalSave;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JellyGarden.Scripts;
using JellyGarden.Scripts.GUI;
using JellyGarden.Scripts.Juice;
using JellyGarden.Scripts.MapScripts;
using RoyalThrone.Presentation;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RoyalThrone.Editor
{
    // Explicit, repeatable smoke checks. Never awards progress or changes the player's economy.
    public static class RoyalHomeScreenChecks
    {
        const string Output = "Design/Branding/Home-Unity/";
        static RoyalHomeScreen home;
        static readonly List<string> checks = new List<string>();
        static readonly List<string> errors = new List<string>();
        static int phase, openingCount, startingCoins, startingLives, startingOpenLevel;
        static double next;
        static bool running;
        static int runId;
        static string size;

        [MenuItem("Tools/Royal Throne/Home/Check and capture homepage (Play Mode) #F8")]
        public static void Run()
        {
            RoyalHomeScreenBuilder.RequireCopy();
            if (!EditorApplication.isPlaying || EditorApplication.isPaused) throw new InvalidOperationException("Run with the homepage visible in unpaused Play Mode.");
            home = UnityEngine.Object.FindAnyObjectByType<RoyalHomeScreen>();
            if (!home || !home.IsHomeVisible) throw new InvalidOperationException("Open the homepage before checking it.");
            EditorApplication.update -= Watch; Application.logMessageReceived -= Log;
            // Every step runs in the player loop. From an editor callback Screen can describe the editor window
            // instead of the Game view, which misplaces these checks and the layout the Home buttons refresh.
            RoyalRegressionRunner.Run(Drive(++runId));
        }
        static IEnumerator Drive(int id)
        {
            yield return null;
            if (id != runId || !Begin()) yield break;
            while (running && id == runId) { Tick(); yield return null; }
        }
        static bool Begin()
        {
            checks.Clear(); errors.Clear(); size = Screen.width + "x" + Screen.height;
            startingCoins = InitScript.Gems; startingLives = InitScript.lifes; startingOpenLevel = PlayerPrefs.GetInt("OpenLevel");
            Directory.CreateDirectory(Output);
            try
            {
                ProgressCases();
                home.ClosePanel(); home.RefreshProgress(); Canvas.ForceUpdateCanvases();
                Check(home.levelNumbers.Length == 100, "All 100 existing levels available");
                Check(!LevelsMap.IsLevelLocked(home.Progress.nextLevel), "Next level obeys the original unlock rule");
                Check(home.coinLabel.text == InitScript.Gems.ToString("N0") && home.lifeLabel.text == InitScript.lifes.ToString() && home.starLabel.text == home.Progress.stars.ToString(), "HUD shows real saved resources");
                Check(home.legacyMapUI.gameObject.activeInHierarchy && home.legacyMapUI.GetComponentsInChildren<LIFESAddCounter>().Any(t => t.isActiveAndEnabled), "Original life-regeneration counter remains active");
                Check(home.legacyMapUI.alpha == 0 && !home.legacyMapUI.blocksRaycasts, "Old map HUD hidden without disabling its timer");
                Check(home.globalMenus.GetComponent<Canvas>().renderMode == RenderMode.ScreenSpaceOverlay && home.globalMenus.GetComponent<Canvas>().sortingOrder > home.GetComponent<Canvas>().sortingOrder, "Legacy level/boost/lives canvas can render above Home");
                Check(!home.mapCamera.enabled, "Old map camera cannot pan behind Home");
                CheckNavigation(2);
                Check(home.settingButtons.Length == 4, "Home exposes Music, Sound, Motion and Hints");
                foreach (var b in home.resourceButtons.Concat(home.tabButtons).Concat(new[] { home.levelButton, home.areaButton })) CheckHit(b);
                Rect left = ScreenRect(home.levelRect), right = ScreenRect(home.areaRect), nav = ScreenRect(home.navigation), hud = ScreenRect(home.hud);
                Check(left.xMax < right.xMin && left.yMin > nav.yMax && hud.yMin > left.yMax, "HUD, level/area actions and navigation do not overlap");
                CheckHomeArt(left, right);
                Capture("home");
                phase = 0; next = EditorApplication.timeSinceStartup + .6;
                Application.logMessageReceived += Log; EditorApplication.update += Watch; running = true; return true;
            }
            catch (Exception e) { Finish(e); return false; }
        }
        // The steps run in the player loop; this editor hook only reports a Play session that ends mid-suite.
        static void Watch() { if (running && !EditorApplication.isPlaying) Finish(new InvalidOperationException("Play session remains available")); }

        static void Tick()
        {
            if (EditorApplication.timeSinceStartup < next) return;
            next = EditorApplication.timeSinceStartup + .7;
            try
            {
                Check(EditorApplication.isPlaying && home, "Play session remains available");
                switch (phase++)
                {
                    case 0:
                        home.areaButton.onClick.Invoke(); Check(home.dialog.activeSelf && !home.controls.interactable, "Area dialog disables castle controls while navigation stays usable");
                        CheckHit(home.tabButtons[0]);
                        Check(home.dialogDetail.text.Contains("coming soon"), "Unbuilt restoration/rewards are disclosed"); Capture("area"); break;
                    case 1:
                        CheckHit(home.closeButton); CheckHit(home.dialogButton);
                        home.ClosePanel(); home.tabButtons[0].onClick.Invoke(); Canvas.ForceUpdateCanvases();
                        Check(home.levelGrid.activeSelf, "Story opens replay and level selection");
                        CheckNavigation(0);
                        Check(!ScreenRect(home.gridRect).Overlaps(ScreenRect(home.dialogCopy.rectTransform)), "Story subtitle does not overlap level grid");
                        for (int i = 0; i < home.levelChoices.Length; i++)
                        {
                            var b = home.levelChoices[i]; if (!b.gameObject.activeSelf) continue;
                            int n = int.Parse(b.GetComponentInChildren<Text>().text.Split('\n')[0]);
                            Check(b.interactable == !LevelsMap.IsLevelLocked(n), "Story lock rule for level " + n);
                        }
                        Capture("story"); break;
                    case 2:
                        // Story opens on the page holding the next level, so pagination is checked relative to that page.
                        int shownPage = int.Parse(home.pageLabel.text.Split('/')[0].Trim()); bool forward = home.nextPage.interactable;
                        (forward ? home.nextPage : home.previousPage).onClick.Invoke(); Check(home.pageLabel.text.StartsWith((forward ? shownPage + 1 : shownPage - 1) + " /"), "Story pagination advances");
                        (forward ? home.previousPage : home.nextPage).onClick.Invoke();
                        home.ClosePanel(); home.resourceButtons[4].onClick.Invoke();
                        Check(home.settingsGroup.activeSelf, "Settings opens real audio and motion controls");
                        int motion = PlayerPrefs.GetInt("RoyalThrone.HomeMotion", 1);
                        bool hadMotion = PlayerPrefs.HasKey("RoyalThrone.HomeMotion");
                        home.settingButtons[2].onClick.Invoke(); Check(home.MotionEnabled == (motion == 0), "Motion toggle updates the saved setting");
                        home.settingButtons[2].onClick.Invoke();
                        if (!hadMotion) { PlayerPrefs.DeleteKey("RoyalThrone.HomeMotion"); PlayerPrefs.Save(); }
                        CheckHints();
                        Capture("settings"); break;
                    case 3:
                        foreach (var setting in home.settingButtons) CheckHit(setting);
                        home.ClosePanel();
                        foreach (int i in new[] { 1, 3, 4 })
                        { home.tabButtons[i].onClick.Invoke(); Check(home.dialogDetail.text.Contains("coming soon"), "Future tab " + i + " has no fabricated feature"); CheckNavigation(i); CheckHit(home.tabButtons[2]); }
                        home.tabButtons[2].onClick.Invoke();
                        Check(!home.dialog.activeSelf, "Home tab returns directly from a feature panel"); CheckNavigation(2);
                        openingCount = home.OpenRequestCount;
                        home.OpenNextLevel(); home.OpenNextLevel();
                        Check(home.OpenRequestCount == openingCount + 1, "Repeated level press creates only one entry request");
                        Check(MenuManager.Instance.menuPlay.activeSelf, "Existing pre-level booster menu opens");
                        Check(PlayerPrefs.GetInt("OpenLevel") == home.Progress.nextLevel, "Pre-level selection receives the real next level");
                        Check(InitScript.Gems == startingCoins && InitScript.lifes >= startingLives, "Opening Home dialogs consumes no coins or lives");
                        break;
                    case 4:
                        CheckHit(MenuManager.Instance.menuPlay.transform.Find("Image/Close").GetComponent<Button>());
                        Capture("pre-level"); break;
                    case 5: MenuManager.Instance.menuPlay.transform.Find("Image/Close").GetComponent<Button>().onClick.Invoke(); break;
                    case 6:
                        Check(home.IsHomeVisible && home.controls.interactable && !home.dialog.activeSelf, "Closing pre-level returns control to Home");
                        Check(errors.Count == 0, "No runtime errors during homepage checks"); Finish(null); break;
                }
            }
            catch (Exception e) { Finish(e); }
        }

        static void ProgressCases()
        {
            var levels = Enumerable.Range(1, 100).ToArray();
            var p = RoyalHomeProgress.Read(levels, n => 0);
            Check(p.nextLevel == 1 && p.stars == 0 && p.area == 1 && p.areaTotal == 6, "Fresh save starts at level 1 / area 1");
            p = RoyalHomeProgress.Read(levels, n => n <= 6 ? 3 : 0);
            Check(p.nextLevel == 7 && p.stars == 18 && p.area == 2 && p.areaCompleted == 0, "Six completed levels advance the area");
            p = RoyalHomeProgress.Read(levels, n => n == 1 ? 2 : n == 8 ? 1 : 0);
            Check(p.nextLevel == 9 && p.completed == 2 && p.stars == 3, "Restored non-contiguous progress matches map unlocks");
            p = RoyalHomeProgress.Read(levels, n => n <= 99 ? 3 : 0);
            Check(p.nextLevel == 100 && p.area == 17 && p.areaTotal == 4 && p.areaCompleted == 3, "Final partial area has four levels");
            p = RoyalHomeProgress.Read(levels, n => 3);
            Check(p.nextLevel == 100 && p.stars == 300 && p.areaCompleted == 4, "Completed campaign never offers missing level 101");
            p = RoyalHomeProgress.Read(levels, n => n == 1 ? 99 : -1);
            Check(p.stars == 3 && p.completed == 1, "Unexpected saved star values are clamped");
            p = RoyalHomeProgress.Read(Array.Empty<int>(), n => 0);
            Check(p.areaTotal == 0 && p.stars == 0, "Empty level list is safe");
        }
        static void CheckHit(Button button)
        {
            Rect rect = ScreenRect(button.GetComponent<RectTransform>());
            Check(rect.xMin >= -.5f && rect.xMax <= Screen.width + .5f && rect.yMin >= -.5f && rect.yMax <= Screen.height + .5f, button.name + " stays on screen");
            var data = new PointerEventData(EventSystem.current) { position = rect.center };
            var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(data, hits);
            Check(hits.Count > 0 && hits[0].gameObject.GetComponentInParent<Button>() == button,
                button.name + " receives its pointer hit (hits: " + string.Join(", ", hits.Take(3).Select(h => h.gameObject.name)) +
                "; interactable: " + button.IsInteractable() + "; center: " + rect.center + ")");
        }
        static void CheckNavigation(int selected)
        {
            home.RefreshLayout(); Canvas.ForceUpdateCanvases();
            Check(home.SelectedTab == selected, "Navigation selects tab " + selected);
            for (int i = 0; i < home.tabButtons.Length; i++)
            {
                var label = home.tabButtons[i].GetComponentInChildren<Text>(true);
                Check(label.gameObject.activeSelf && label.enabled && label.text.Length > 0, "Tab " + i + " shows its name");
                Rect hit = ScreenRect((RectTransform)home.tabButtons[i].transform);
                Check(hit.width >= Screen.width * .13f, "Tab " + i + " retains a generous touch target");
            }
        }
        // The raised Home art must leave the level and area cards clear. The imported castle (drawn when the reference layout is
        // attached) rises through the gap between them: in its sprite only the central spire fills the top quarter, at most 14% of
        // the width in the top 17% and 20% down to 25% (measured from 16525.png). So only rows in that quarter may reach above the
        // cards, and the spire's column there must stay between the cards.
        static void CheckHomeArt(Rect left, Rect right)
        {
            var castle = home.tabButtons[2].GetComponentsInChildren<Image>().FirstOrDefault(i => i.enabled && i.sprite && i.name.StartsWith("Imported"));
            if (!castle)
            {
                var homeIcon = home.tabButtons[2].transform.Find("Home icon") as RectTransform;
                Check(!ScreenRect(homeIcon).Overlaps(left) && !ScreenRect(homeIcon).Overlaps(right), "Lifted Home icon leaves the primary actions clear");
                return;
            }
            Rect box = ScreenRect(castle.rectTransform);
            float aspect = castle.sprite.rect.width / castle.sprite.rect.height;
            // preserveAspect draws the sprite centred in its box.
            Vector2 size = box.width / box.height > aspect ? new Vector2(box.height * aspect, box.height) : new Vector2(box.width, box.width / aspect);
            Rect art = new Rect(box.center - size * .5f, size);
            float cardBottom = Mathf.Min(left.yMin, right.yMin), above = Mathf.Clamp01((art.yMax - cardBottom) / art.height);
            float spire = art.width * (above <= .17f ? .14f : .20f);
            Rect column = Rect.MinMaxRect(art.center.x - spire * .5f, cardBottom, art.center.x + spire * .5f, art.yMax);
            Check(above <= .25f && (above == 0 || !column.Overlaps(left) && !column.Overlaps(right)),
                "Lifted Home castle leaves the primary actions clear (" + Mathf.RoundToInt(above * 100) + "% of it above the cards, spire between them)");
        }
        static void CheckHints()
        {
            bool hints = RoyalHintSettings.Enabled, hadHints = PlayerPrefs.HasKey("RoyalThrone.ShowHints");
            try
            {
                home.settingButtons[3].onClick.Invoke();
                Check(RoyalHintSettings.Enabled != hints && home.settingButtons[3].GetComponentInChildren<Text>().text.EndsWith(hints ? "Off" : "On"), "Hints button immediately updates the shared setting and label");
                home.settingButtons[3].onClick.Invoke();
                Check(RoyalHintSettings.Enabled == hints, "Home Hints check restores the player's preference");
            }
            finally
            {
                RoyalHintSettings.Enabled = hints;
                if (!hadHints) { PlayerPrefs.DeleteKey("RoyalThrone.ShowHints"); PlayerPrefs.Save(); }
            }
        }
        static Rect ScreenRect(RectTransform rect)
        {
            var corners = new Vector3[4]; rect.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }
        static void Check(bool result, string message)
        { if (!result) throw new InvalidOperationException(message); checks.Add("PASS " + message); }
        static void Capture(string name)
        { Canvas.ForceUpdateCanvases(); ScreenCapture.CaptureScreenshot(Output + name + "-" + size + ".png"); }
        static void Log(string message, string trace, LogType type)
        { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors.Add(message); }
        static void Finish(Exception failure)
        {
            running = false; EditorApplication.update -= Watch; Application.logMessageReceived -= Log;
            // Opening the next level's card writes OpenLevel on the active profile: put the player's selection back.
            PlayerPrefs.SetInt("OpenLevel", startingOpenLevel); PlayerPrefs.Save();
            if (failure != null) checks.Add("FAIL " + failure.Message);
            checks.AddRange(errors.Select(e => "ERROR " + e));
            File.WriteAllLines(Output + "checks-" + size + ".txt", new[] { "Royal Throne Unity home checks", DateTime.UtcNow.ToString("O"), "Resolution: " + size }.Concat(checks));
            if (failure != null) Debug.LogError("Royal Home checks failed: " + failure.Message);
            else Debug.Log("Royal Home checks passed: " + size + ". Report and game captures in " + Output);
        }
    }
}
