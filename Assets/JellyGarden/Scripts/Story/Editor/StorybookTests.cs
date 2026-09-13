using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace JellyGarden.Scripts.Story.Editor
{
    public static class StorybookTests
    {
        public static void RunHeadless()
        {
            try
            {
                var failures = TreasureHuntTests.Check();
                if (failures.Count > 0) throw new Exception(string.Join("\n", failures));
                CheckNavigationAndActions();
                StoryWinPopupTests.Run();
                Debug.Log("[Storybook] PASS: campaign, navigation, task order, goals, boosters, callbacks and victory.");
                EditorApplication.Exit(0);
            }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        private static void Require(bool condition, string description)
        {
            if (!condition) throw new Exception(description);
        }

        private static void CheckNavigationAndActions()
        {
            string previous = StoryProgress.SaveDirectoryOverride;
            string dir = Path.Combine(Path.GetTempPath(), "king-leo-storybook-" + Guid.NewGuid());
            var launcher = StoryLevelPopup.Launcher;
            var owned = StoryBoosters.OwnedSource;
            var playing = StoryNavBar.LevelIsPlayingSource;
            StoryProgress.Flush();
            Directory.CreateDirectory(dir);
            StoryProgress.SaveDirectoryOverride = dir;
            try
            {
                StoryProgress.ReloadFromDisk();
                StoryNavBar.LevelIsPlayingSource = () => false;
                AreaScreen.Close();
                StoryHomeScreen.Open(1);
                StoryNavBar.Sync();
                Require(StoryNavBar.Instance.Parts.Tabs.Length == 3, "Home / Map / Collection dock");
                Require(StoryHomeScreen.Instance.LevelButtonText == "Play Level 1", "home names next level");
                StoryNavBar.Instance.ButtonFor(StoryNavBar.Tab.Card).onClick.Invoke();
                Require(StorybookCollection.IsOpen && !StoryProgress.IsClueSeen(1), "album cannot grant a clue");
                StoryNavBar.Instance.ButtonFor(StoryNavBar.Tab.Home).onClick.Invoke();
                Require(!StorybookCollection.IsOpen && StoryHomeScreen.IsOpen, "Home closes the album");
                AreaScreen.Open(1);
                StoryNavBar.Sync();
                Require(!StoryNavBar.IsVisible, "task sheet has its own close action");
                Require(!AreaScreen.Instance.RowCanRepair(0), "unfunded task is disabled");
                StoryProgress.RecordLevelWin(1, 1);
                AreaScreen.Instance.Refresh();
                Require(AreaScreen.Instance.RowCanRepair(0) && !AreaScreen.Instance.RowCanRepair(1), "only next funded task is enabled");
                int before = StoryProgress.StarsIn(1);
                AreaScreen.Instance.CloseButton.onClick.Invoke();
                Require(!AreaScreen.IsOpen && StoryProgress.StarsIn(1) == before, "closing the sheet never spends a star");
                int launches = 0, closes = 0;
                StoryLevelPopup.Launcher = (level, area) =>
                { Require(level == 3 && area == 1, "Play uses the requested level and area"); launches++; };
                StoryBoosters.OwnedSource = boost => boost == StoryBoosters.PreLevel[0] ? 2 : 0;
                for (int level = 1; level <= 4; level++)
                {
                    StoryLevelPopup.Open(level, 1, () => closes++);
                    var popup = StoryLevelPopup.Instance;
                    var expected = StoryLevelHud.ReadLevel(level, 0).Targets.Take(4)
                        .Select(t => Mathf.Max(0, t.Remaining).ToString());
                    Require(popup.GoalTexts().SequenceEqual(expected), "actual goals on level " + level);
                    Canvas.ForceUpdateCanvases();
                    foreach (StoryLevelGoalVfx effect in popup.GetComponentsInChildren<StoryLevelGoalVfx>())
                    {
                        var icon = effect.transform.parent.Find("Icon") as RectTransform;
                        if (icon == null) continue;
                        Vector2 rest = icon.anchoredPosition;
                        effect.SendMessage("Update");
                        Require(Vector2.Distance(rest, icon.anchoredPosition) <= 2.1f,
                            "goal animation retains its laid-out position on level " + level);
                    }
                    Require(popup.BoosterButton(0).interactable && !popup.BoosterButton(1).interactable,
                        "booster availability comes from inventory");
                    popup.BoosterButton(0).onClick.Invoke();
                    Require(popup.BoosterIsSelected(0), "owned booster can be selected");
                    popup.CloseButton.onClick.Invoke();
                    popup.CloseButton.onClick.Invoke();
                    Require(!StoryBoosters.IsSelected(StoryBoosters.PreLevel[0]), "close clears selection");
                }
                Require(closes == 4 && launches == 0, "each close returns once without launching");
                StoryLevelPopup.Open(3, 1, null);
                StoryLevelPopup.Instance.PlayButton.onClick.Invoke();
                StoryLevelPopup.Instance.PlayButton.onClick.Invoke();
                Require(launches == 1, "Play is protected against double taps");
                Require(StoryProgress.StarsIn(1) == before && !StoryProgress.IsAreaUnlocked(2),
                    "UI navigation preserves star balance and development gate");
                StoryProgress.MarkSceneSeen(StoryDatabase.GetArea(1).PayoffScene);
                StorybookCollection.Open();
                Require(GameObject.Find("StorybookCollection").GetComponentsInChildren<Text>()
                    .Any(t => t.text == "The sun clue"), "album reads the Treasure Hunt discovery flag");
                Require(StoryProgress.StarsIn(1) == before, "viewing an earned clue never rewards stars");
            }
            finally
            {
                StoryLevelPopup.Close(); StorybookCollection.Close(); AreaScreen.Close(); StoryHomeScreen.Close();
                StoryLevelPopup.Launcher = launcher;
                StoryBoosters.OwnedSource = owned;
                StoryNavBar.LevelIsPlayingSource = playing;
                StoryProgress.Flush();
                StoryProgress.SaveDirectoryOverride = previous;
                StoryProgress.ReloadFromDisk();
                Directory.Delete(dir, true);
            }
        }
    }
}
