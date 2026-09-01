// Drives the story screens for real and checks what the player would see.
//
// Not [UnityTest]: this project has no asmdefs, so its scripts live in the
// predefined Assembly-CSharp. Unity gives that assembly NUnit but not
// UnityEngine.TestTools, and an asmdef test assembly cannot reference a
// predefined assembly — so the Test Framework's play-mode runner is not
// available here. Instead the screens are built and driven directly, with
// StoryClock standing in for the frame loop. That makes the three-second clue
// card and the typewriter deterministic rather than something to sleep through.
//
// Unity -batchmode -quit -nographics -projectPath . \
//   -executeMethod JellyGarden.Scripts.Story.Editor.StoryScreenTests.RunHeadless -logFile -

using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace JellyGarden.Scripts.Story.Editor
{
    public static class StoryScreenTests
    {
        private static List<string> _failures;

        [MenuItem("JellyGarden/Story/Run Screen Tests")]
        public static void Run()
        {
            List<string> failures = Check();
            if (failures.Count == 0)
            {
                Debug.Log("[Story] Screen tests passed.");
                return;
            }
            for (int i = 0; i < failures.Count; i++) Debug.LogError("[Story] " + failures[i]);
        }

        public static void RunHeadless()
        {
            List<string> failures = Check();
            for (int i = 0; i < failures.Count; i++) Debug.LogError("[Story] " + failures[i]);
            if (failures.Count > 0)
            {
                EditorApplication.Exit(1);
                return;
            }
            Debug.Log("[Story] Screen tests passed.");
            EditorApplication.Exit(0);
        }

        private static List<string> Check()
        {
            _failures = new List<string>();

            string savePath = StoryProgress.SavePath;
            string backup = savePath + ".screentestbackup";
            bool hadSave = File.Exists(savePath);
            if (hadSave) File.Copy(savePath, backup, true);

            StoryClock.Override(1000f);

            try
            {
                StoryBoot.BootForTests();
                StoryProgress.ResetAll();

                AreaScreenShowsTheStory();
                RepairIsOfferedOnlyForTheNextAffordableTask();
                SpendingStarsCompletesTheAreaAndUnlocksTheNext();
                ClueCardShowsTheClueVerbatimAndDismissesItself();
                DialogueTypesOutAndAdvancesOnTap();
                ArcBeatsHaveScenesAndPlayThemOnce();
                EveryAreaOpens();
            }
            finally
            {
                AreaScreen.Close();
                StoryClock.Release();
                CleanUpStoryObjects();

                if (hadSave)
                {
                    File.Copy(backup, savePath, true);
                    File.Delete(backup);
                }
                else if (File.Exists(savePath))
                {
                    File.Delete(savePath);
                }
                StoryProgress.ReloadFromDisk();
            }

            return _failures;
        }

        #region Cases

        private static void AreaScreenShowsTheStory()
        {
            AreaScreen.Open(1);
            Expect(AreaScreen.IsOpen, "the area screen opens");

            StoryArea area = StoryDatabase.GetArea(1);
            List<string> texts = TextsIn(AreaScreen.Instance.gameObject);

            Expect(texts.Any(t => t.Contains(area.Name)), "the area name is on screen");
            Expect(texts.Any(t => t.Contains(area.Goal)), "the area goal is on screen");

            StoryArcInfo arc = StoryDatabase.ArcOf(1);
            Expect(texts.Any(t => t.Contains(arc.Title)), "the arc title is on screen");

            foreach (StoryTask task in area.Tasks)
                Expect(texts.Any(t => t.Contains(task.Label)),
                    "task '" + task.Label + "' is listed");
        }

        private static void RepairIsOfferedOnlyForTheNextAffordableTask()
        {
            StoryProgress.ResetAll();
            AreaScreen.Open(1);

            Button[] repairs = RepairButtons();
            Expect(repairs.Length == 1,
                "only the next unfinished task offers a Repair button (found " +
                repairs.Length + ")");
            Expect(repairs.Length == 1 && !repairs[0].interactable,
                "with no stars the Repair button is not interactable");

            StoryProgress.AddStar(1);
            AreaScreen.Instance.Refresh();

            repairs = RepairButtons();
            Expect(repairs.Length == 1 && repairs[0].interactable,
                "with a star in hand the Repair button becomes interactable");
        }

        private static void SpendingStarsCompletesTheAreaAndUnlocksTheNext()
        {
            StoryProgress.ResetAll();
            StoryArea area = StoryDatabase.GetArea(1);
            StoryProgress.AddStar(1, area.StarCost);

            AreaScreen.Open(1);

            for (int i = 0; i < area.Tasks.Count; i++)
            {
                Button repair = RepairButtons().FirstOrDefault();
                if (repair == null)
                {
                    Fail("no Repair button offered for task " + (i + 1));
                    return;
                }
                if (!repair.interactable)
                {
                    Fail("task " + (i + 1) + " was not affordable");
                    return;
                }
                repair.onClick.Invoke();

                // The last repair closes the screen and hands over to the clue.
                if (AreaScreen.IsOpen) AreaScreen.Instance.Refresh();
            }

            Expect(StoryProgress.IsAreaComplete(1), "area 1 is complete");
            Expect(StoryProgress.StarsIn(1) == 0, "the stars were spent, not kept");
            Expect(StoryProgress.IsAreaUnlocked(2), "area 2 is unlocked");
            Expect(StoryProgress.IsClueSeen(1), "the clue fired on the last repair");
        }

        private static void ClueCardShowsTheClueVerbatimAndDismissesItself()
        {
            StoryProgress.ResetAll();

            bool done = false;
            ClueCard.Show(1, () => done = true);
            Expect(ClueCard.IsShowing, "the clue card is showing");

            var card = Object.FindFirstObjectByType<ClueCard>();
            if (card == null)
            {
                Fail("no ClueCard object was created");
                return;
            }

            List<string> texts = TextsIn(card.gameObject);
            string clue = StoryDatabase.GetArea(1).Clue;
            Expect(texts.Any(t => t == clue),
                "the clue text is on the card verbatim, with nothing added");

            // Three seconds of hold plus the fade, stepped rather than waited on.
            StoryClock.Advance(3f);
            card.Tick(StoryClock.Now);
            Expect(ClueCard.IsShowing, "the card is still up during its fade");

            StoryClock.Advance(0.5f);
            card.Tick(StoryClock.Now);

            Expect(!ClueCard.IsShowing, "the clue card dismisses itself");
            Expect(done, "the clue card calls back when it is gone");
        }

        private static void DialogueTypesOutAndAdvancesOnTap()
        {
            var lines = new List<DialogueLine>
            {
                new DialogueLine("Leo", "Freya?"),
                new DialogueLine("Alfred", "He never finished the sentence.")
            };

            bool done = false;
            DialogueSystem.Play(lines, () => done = true);
            Expect(DialogueSystem.IsPlaying, "dialogue starts playing");

            var dialogue = Object.FindFirstObjectByType<DialogueSystem>();
            if (dialogue == null)
            {
                Fail("no DialogueSystem object was created");
                return;
            }

            List<string> shown = TextsIn(dialogue.gameObject);
            Expect(shown.Any(t => t == "Leo"), "the speaker's name is shown");
            Expect(!shown.Any(t => t == lines[0].Text),
                "the first line has not typed itself out yet");

            StoryClock.Advance(2f);
            dialogue.Tick(StoryClock.Now);
            shown = TextsIn(dialogue.gameObject);
            Expect(shown.Any(t => t == lines[0].Text), "the line finishes typing");

            Button advance = dialogue.GetComponentsInChildren<Button>(true)
                .FirstOrDefault(b => b.name == "Advance");
            if (advance == null)
            {
                Fail("the dialogue box has no tap-to-advance area");
                return;
            }

            advance.onClick.Invoke();  // -> line 2
            StoryClock.Advance(2f);
            dialogue.Tick(StoryClock.Now);
            advance.onClick.Invoke();  // -> finish

            Expect(!DialogueSystem.IsPlaying, "dialogue finishes");
            Expect(done, "dialogue calls back");
        }

        private static void ArcBeatsHaveScenesAndPlayThemOnce()
        {
            StoryProgress.ResetAll();

            // Every fifth area is a beat and must have a scene; the six arc
            // finales carry the payoff and need more than a card's worth.
            int beats = 0;
            for (int areaId = 5; areaId <= StoryDatabase.AreaCount; areaId += 5)
            {
                if (!CutsceneRunner.Has(areaId))
                {
                    Fail("arc beat area " + areaId + " has no cutscene");
                    return;
                }

                Cutscene scene = CutsceneRunner.Get(areaId);
                int minimum = areaId % 25 == 0 ? 6 : 3;
                if (scene.Lines.Count < minimum)
                {
                    Fail("area " + areaId + " has " + scene.Lines.Count +
                         " lines, needs " + minimum);
                    return;
                }

                foreach (DialogueLine line in scene.Lines)
                {
                    if (string.IsNullOrEmpty(line.Text))
                    {
                        Fail("area " + areaId + " has an empty line");
                        return;
                    }
                    if (line.Text.StartsWith("ARC PAYOFF"))
                    {
                        Fail("area " + areaId + " speaks a production label aloud");
                        return;
                    }
                }
                beats++;
            }

            Expect(beats == StoryDatabase.AreaCount / 5,
                "every fifth area has a scene (" + beats + " found)");

            // A beat plays its scene, then its clue, then hands back.
            bool done = false;
            StoryPresentation.PlayAreaPayoff(5, () => done = true);
            Expect(CutsceneRunner.IsPlaying, "the beat's scene starts playing");

            var dialogue = Object.FindFirstObjectByType<DialogueSystem>();
            Button skip = dialogue.GetComponentsInChildren<Button>(true)
                .FirstOrDefault(b => b.name == "Skip");
            if (skip == null)
            {
                Fail("the cutscene offers no Skip control");
                return;
            }
            skip.onClick.Invoke();

            Expect(!CutsceneRunner.IsPlaying, "the scene is skippable");
            Expect(StoryProgress.IsCutsceneSeen(5), "the scene is marked seen");
            Expect(ClueCard.IsShowing, "the clue follows the scene");

            var card = Object.FindFirstObjectByType<ClueCard>();
            StoryClock.Advance(3.5f);
            card.Tick(StoryClock.Now);
            Expect(done, "the payoff hands back when the clue is gone");

            // Second time round it must not replay itself.
            bool replayed = false;
            StoryPresentation.PlayAreaPayoff(5, () => replayed = true);
            Expect(!CutsceneRunner.IsPlaying, "a seen scene does not play itself again");
            Expect(replayed, "an already-seen payoff hands straight back");
        }

        private static void EveryAreaOpens()
        {
            // 150 areas is the whole product. One that cannot open is a dead end a
            // player walks into hours in, so every one is opened here.
            StoryProgress.ResetAll();

            int opened = 0;
            for (int areaId = 1; areaId <= StoryDatabase.AreaCount; areaId++)
            {
                AreaScreen.Open(areaId);
                if (!AreaScreen.IsOpen || AreaScreen.Instance.AreaId != areaId)
                {
                    Fail("area " + areaId + " did not open");
                    return;
                }

                List<string> texts = TextsIn(AreaScreen.Instance.gameObject);
                StoryArea area = StoryDatabase.GetArea(areaId);
                if (!texts.Any(t => t.Contains(area.Name)))
                {
                    Fail("area " + areaId + " opened without its name");
                    return;
                }
                opened++;
            }

            Expect(opened == StoryDatabase.AreaCount,
                "all " + StoryDatabase.AreaCount + " areas open (" + opened + " did)");
        }

        #endregion

        #region Helpers

        private static List<string> TextsIn(GameObject root)
        {
            return root.GetComponentsInChildren<Text>(true)
                .Select(t => t.text ?? string.Empty)
                .ToList();
        }

        private static Button[] RepairButtons()
        {
            if (!AreaScreen.IsOpen) return new Button[0];
            return AreaScreen.Instance.GetComponentsInChildren<Button>(true)
                .Where(b => b.name == "Repair" && b.gameObject.activeInHierarchy)
                .ToArray();
        }

        private static void CleanUpStoryObjects()
        {
            foreach (string name in new[]
                     {
                         "StoryAreaScreen", "~StoryClueCard", "~StoryDialogue",
                         "~StoryToast", "~StoryCutscene"
                     })
            {
                GameObject go = GameObject.Find(name);
                if (go != null) Object.DestroyImmediate(go);
            }
        }

        private static void Expect(bool condition, string what)
        {
            if (!condition) _failures.Add("FAILED: " + what);
        }

        private static void Fail(string what)
        {
            _failures.Add("FAILED: " + what);
        }

        #endregion
    }
}
