using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace JellyGarden.Scripts.Story.Editor
{
    public static class TreasureHuntTests
    {
        public static void RunHeadless()
        {
            var failures = Check();
            foreach (string failure in failures) Debug.LogError("[TreasureHunt] " + failure);
            Debug.Log("[TreasureHunt] Campaign checks: " + failures.Count + " failure(s).");
            if (Application.isBatchMode) EditorApplication.Exit(failures.Count == 0 ? 0 : 1);
        }

        public static List<string> Check()
        {
            var failures = new List<string>();
            Action<bool, string> expect = (ok, message) => { if (!ok) failures.Add(message); };
            string previous = StoryProgress.SaveDirectoryOverride;
            string dir = Path.Combine(Path.GetTempPath(), "king-leo-campaign-test-" + Guid.NewGuid());
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "story_save.json"), "legacy-save-sentinel");
            StoryProgress.Flush();
            StoryProgress.SaveDirectoryOverride = dir;
            try
            {
                StoryProgress.ReloadFromDisk();
                expect(StoryCampaign.IsTreasureHunt, "Treasure Hunt must be selected");
                expect(StoryDatabase.AreaCount == 150, "150 new catalogue entries");
                expect(StoryDatabase.GetArea(1).Name == "Castle Gates", "new first area name");
                expect(StoryDatabase.TotalStars == 4 && StoryLevelPlan.LevelCount == 4, "four funded tasks");
                var opening = TreasureHuntPresentation.LinesFor("OPEN-01");
                var ending = TreasureHuntPresentation.LinesFor("A001-DISCOVERY");
                expect(opening.Count == 2 && ending.Count == 2, "Area 1 has only four dialogue cards");
                if (opening.Count == 2 && ending.Count == 2)
                {
                    expect(opening[0].Speaker == "Leo" && opening[0].Text == "Welcome, Puzzle Partner!" &&
                        opening[1].Speaker == "Pip" && opening[1].Text == "I’m Pip! Six keys lead to one royal surprise!" &&
                        opening[1].StageCue == "map", "approved opening retains Pip's map cue");
                    expect(ending[0].Speaker == "Pip" && ending[0].Text == "Look! The sun is our first clue!" &&
                        ending[0].StageCue == "sun" && ending[1].Speaker == "Freya" &&
                        ending[1].Text == "Next, let’s restore the courtyard fountain!", "approved ending retains the sun clue");
                }
                StoryTask vines = StoryProgress.FindTask(StoryDatabase.GetArea(1), 1);
                expect(!vines.InstantRepair && vines.RepairTransition == "vines",
                    "Clear the vines uses the approved native transition");
                expect(Resources.Load<Shader>("Story/UI/VineDissolve") != null &&
                    Resources.Load<TextAsset>(StoryVineTransition.DataPath(1, 1)) != null,
                    "vine transition shader and measured leaf anchors are included");
                string[] transitions = { "door", "flags", "lanterns" };
                for (int task = 2; task <= 4; task++)
                    expect(StoryProgress.FindTask(StoryDatabase.GetArea(1), task).RepairTransition == transitions[task - 2],
                        "Area 1 task " + task + " uses its approved transition");
                Texture2D gateAtlas = Resources.Load<Texture2D>(StoryGateTransition.DataPath(1));
                TextAsset gateLayout = Resources.Load<TextAsset>(StoryGateTransition.DataPath(1) + "_layout");
                expect(gateAtlas != null && gateLayout != null && Resources.Load<Shader>("Story/UI/LanternLight") != null,
                    "gate transition atlas, layout and lighting shader are included");
                if (gateAtlas != null && gateLayout != null)
                {
                    JsonValue layout = JsonValue.Parse(gateLayout.text);
                    expect(gateAtlas.width == layout["width"].AsInt() && gateAtlas.height == layout["height"].AsInt(),
                        "transition atlas is imported at its authored dimensions");
                    expect(!gateAtlas.isReadable && layout["parts"].Count == 15,
                        "transition layers need no runtime texture readback");
                    foreach (JsonValue part in layout["parts"].Items)
                        expect(part["atlasX"].AsInt() >= 0 && part["atlasY"].AsInt() >= 0 &&
                            part["atlasX"].AsInt() + part["width"].AsInt() <= gateAtlas.width &&
                            part["atlasY"].AsInt() + part["height"].AsInt() <= gateAtlas.height,
                            "transition part fits the imported texture: " + part["key"].AsString());
                }
                for (int task = 1; task <= 4; task++)
                    expect(StoryRepairVideo.Load(1, task) == null,
                        "new campaign must not reuse a legacy repair movie: " + task);
                expect(!StoryProgress.OpeningSeen && StoryProgress.LevelsBeatenCount == 0, "fresh campaign ignores old progress");
                for (int area = 2; area <= 150; area++)
                {
                    expect(!StoryCampaign.CanEnter(area), "unfinished area entered: " + area);
                    expect(StoryLevelPlan.LevelsFor(area).Count == 0, "unfinished area assigned boards: " + area);
                }
                expect(!StoryProgress.CompleteTask(1, 1), "no repair without a star");
                expect(!StoryProgress.RecordLevelWin(1, 2), "wrong area cannot receive reward");
                expect(!StoryProgress.RecordLevelWin(5, 1), "unmapped board cannot receive reward");
                expect(StoryCampaign.ProtectFirstPuzzle(1), "first puzzle retry protection");
                expect(StoryProgress.RecordLevelWin(1, 1), "first real win pays");
                expect(!StoryProgress.RecordLevelWin(1, 1), "replay must not pay twice");
                StoryProgress.Flush();
                StoryProgress.ReloadFromDisk();
                expect(StoryProgress.StarsIn(1) == 1 && StoryProgress.IsLevelBeaten(1), "first star survives restart");
                expect(!StoryCampaign.ProtectFirstPuzzle(1), "protection ends after first win");
                expect(!StoryProgress.CompleteTask(1, 2), "tasks cannot be bought out of order");
                expect(TreasureHuntPresentation.PlayPending(1, null), "opening resumes after interrupted first win");
                Tap("Skip");
                expect(StoryProgress.OpeningSeen && StoryProgress.IsSceneSeen("OPEN-01"), "opening skip records completion");
                expect(!TreasureHuntPresentation.PlayPending(1, null), "opening not repeated automatically");
                expect(TreasureHuntPresentation.Play("OPEN-01", 1, null, true), "explicit opening replay");
                Tap("Skip");
                expect(StoryProgress.StarsIn(1) == 1, "replaying opening does not pay");
                for (int task = 1; task <= 4; task++)
                {
                    if (task > 1) expect(StoryProgress.RecordLevelWin(task, 1), "first win " + task);
                    expect(string.IsNullOrEmpty(StoryProgress.FindTask(StoryDatabase.GetArea(1), task).OnComplete),
                        "Area 1 omits ordinary task dialogue " + task);
                    if (task < 4)
                    {
                        AreaScreen.Open(1);
                        expect(StoryRepair.Perform(1, task, AreaScreen.Instance.Refresh), "silent repair " + task);
                        expect(AreaScreen.IsOpen && AreaScreen.Instance.PanelVisible &&
                            !AreaScreen.Instance.RowOffersRepair(task - 1) &&
                            !RepairCutscene.IsPlaying && !DialogueSystem.IsPlaying,
                            "edit-mode repair settles to the visible task panel without a dialogue card");
                        AreaScreen.Close();
                        bool continued = false;
                        TreasureHuntPresentation.AfterTask(1, StoryProgress.FindTask(StoryDatabase.GetArea(1), task),
                            () => continued = true);
                        expect(continued && !DialogueSystem.IsPlaying, "silent task continues without a blank card " + task);
                    }
                    else expect(StoryProgress.CompleteTask(1, task), "repair " + task);
                    expect(!StoryProgress.CompleteTask(1, task), "duplicate repair " + task);
                    StoryProgress.Flush();
                    StoryProgress.ReloadFromDisk();
                    expect(StoryProgress.TasksCompletedIn(1) == task && StoryProgress.StarsIn(1) == 0,
                        "repair state and spend survive restart " + task);
                    expect(StoryArt.AreaAtProgress(1) == StoryArt.AreaState(1, task), "correct saved room state " + task);
                }
                expect(StoryProgress.IsAreaComplete(1), "last task completes area");
                expect(!StoryProgress.IsAreaUnlocked(2), "completion does not unlock unfinished area");
                StoryProgress.CurrentArea = 2;
                expect(StoryProgress.CurrentArea == 1, "current area rejects unavailable content");
                expect(TreasureHuntPresentation.PlayPending(1, null), "interrupted final discovery resumes");
                Tap("Skip");
                expect(StoryProgress.IsSceneSeen("A001-DISCOVERY"), "discovery skip persists");
                expect(!TreasureHuntPresentation.PlayPending(1, null), "discovery delivered once");
                expect(TreasureHuntPresentation.Play("A001-DISCOVERY", 1, null, true), "discovery replay");
                Tap("Skip");
                expect(StoryProgress.StarsIn(1) == 0 && StoryProgress.LevelsBeatenCount == 4, "replay has no rewards");
                failures.AddRange(StoryContentAudit.RunForTheShell(false));
                foreach (string speaker in new[] {"Leo", "Freya", "Alfred", "Mimi", "Pip"})
                    expect(StoryArt.Portrait(speaker) != null, "portrait " + speaker);
                expect(StoryArt.Load("win_victory_button") != null, "victory UI shared art");
                foreach (JsonValue key in JsonValue.Parse(StoryCampaign.LoadText("campaign").text)["sharedArt"].Items)
                    expect(StoryArt.Load(key.AsString()) != null, "shared UI art " + key.AsString());
                foreach (string key in new[] {"icon_heart", "icon_star", "icon_gear", "icon_chest",
                    "nav_areas", "nav_leaderboard", "nav_home", "nav_cards"})
                    expect(StoryArt.Load(key) != null, "required navigation art " + key);
                expect(StoryArt.Load("area_002_state0") == null, "no legacy room fallback");
                expect(File.ReadAllText(Path.Combine(dir, "story_save.json")) == "legacy-save-sentinel", "legacy save preserved");
            }
            catch (Exception e) { failures.Add(e.ToString()); }
            finally
            {
                Tap("Skip");
                StoryProgress.Flush();
                StoryProgress.SaveDirectoryOverride = previous;
                StoryProgress.ReloadFromDisk();
                Directory.Delete(dir, true);
            }
            return failures;
        }

        private static void Tap(string name)
        {
            var host = GameObject.Find("~StoryDialogue");
            if (host == null) return;
            foreach (Button button in host.GetComponentsInChildren<Button>(true))
                if (button.name == name && button.gameObject.activeInHierarchy) button.onClick.Invoke();
        }
    }
}
