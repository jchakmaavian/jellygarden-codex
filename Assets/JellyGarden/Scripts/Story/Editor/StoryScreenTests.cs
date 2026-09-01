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
                OpeningPlaysAfterTheFirstWinAndOnlyOnce();
                GeneratedArtIsWiredUp();
                SabotageStartsWhereTheStorySaysItDoes();
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

        private static void OpeningPlaysAfterTheFirstWinAndOnlyOnce()
        {
            StoryProgress.ResetAll();

            // Rule zero: gameplay before story. Nothing plays until a level has
            // actually been won.
            Expect(!OpeningSequence.PlayIfDue(1, null),
                "the opening does not play before the first level is won");

            StoryProgress.MarkLevelBeaten(1);
            bool done = false;
            Expect(OpeningSequence.PlayIfDue(1, () => done = true),
                "the opening plays after the first win");
            Expect(OpeningSequence.IsPlaying, "the opening is on screen");

            var dialogue = Object.FindFirstObjectByType<DialogueSystem>();
            Button advance = dialogue.GetComponentsInChildren<Button>(true)
                .FirstOrDefault(b => b.name == "Advance");
            if (advance == null)
            {
                Fail("the opening has no way to advance");
                return;
            }

            // The hook is one unanswered word; check it is actually spoken, and
            // that nobody follows it with an explanation.
            var spoken = new List<string>();
            for (int i = 0; i < 40 && OpeningSequence.IsPlaying; i++)
            {
                // Let the line finish typing before reading it, or the assertion
                // races the typewriter.
                StoryClock.Advance(3f);
                dialogue.Tick(StoryClock.Now);
                spoken.AddRange(TextsIn(dialogue.gameObject));
                advance.onClick.Invoke();
            }

            Expect(spoken.Any(t => t.Contains("Freya")),
                "Leo says her name in the opening");
            Expect(spoken.Any(t => t.Contains("eleven weeks")),
                "Alfred's first line is in the opening");
            Expect(done, "the opening hands back when it is over");
            Expect(StoryProgress.OpeningSeen, "the opening is marked seen");
            Expect(!OpeningSequence.PlayIfDue(1, null),
                "the opening never plays a second time");
        }

        private static void GeneratedArtIsWiredUp()
        {
            // Naming is the whole of the art wiring, so a rename that breaks it
            // has to fail here rather than show up as a blank screen.
            StoryArt.ClearCache();

            Expect(StoryArt.ArcBackdrop(1) != null, "arc 1 has a backdrop");
            Expect(StoryArt.ClueImage(1) != null, "area 1's clue has a picture");
            Expect(StoryArt.Portrait("Leo") != null, "Leo has a portrait");
            Expect(StoryArt.Portrait("Mimi") != null, "Mimi has a portrait");

            StoryArea area1 = StoryDatabase.GetArea(1);
            for (int state = 0; state <= area1.Tasks.Count; state++)
                Expect(StoryArt.AreaState(1, state) != null,
                    "area 1 has artwork for state " + state);

            Expect(StoryAudio.LoadVoice("alfred_open_01") != null,
                "Alfred's first opening line is present");
            Expect(StoryAudio.LoadVoice("alfred_clue_001") != null,
                "Alfred's area 1 clue line is present");
        }

        private static void SabotageStartsWhereTheStorySaysItDoes()
        {
            // GAMEPLAY.md: areas 001-018 are static neglect. Nothing on the board
            // may act on its own before Grimm opens the sluice gates at area 019.
            Expect(StorySabotage.FirstSabotageArea == 19,
                "sabotage begins at area 19 (plan says " +
                StorySabotage.FirstSabotageArea + ")");

            int earlySabotaged = 0;
            for (int areaId = 1; areaId < 19; areaId++)
                foreach (int levelNumber in StoryLevelPlan.LevelsFor(areaId))
                    if (StorySabotage.IsSabotaged(levelNumber)) earlySabotaged++;

            Expect(earlySabotaged == 0,
                "no level before area 19 is sabotaged (" + earlySabotaged + " are)");

            // And it must actually ramp rather than latch on: a spreading blocker
            // on every level from area 19 would be a tax, not a villain.
            int earlyBand = CountSabotaged(19, 40);
            int lateBand = CountSabotaged(130, 150);
            Expect(earlyBand > 0, "sabotage appears soon after area 19");
            Expect(lateBand > earlyBand,
                "sabotage becomes more common by the siege (" + earlyBand +
                " then " + lateBand + " per cent)");

            // The beat itself is marked exactly once.
            int beats = 0;
            for (int levelNumber = 1; levelNumber <= StoryLevelPlan.LevelCount; levelNumber++)
            {
                StoryLevelEntry entry = StoryLevelPlan.Get(levelNumber);
                if (entry != null && entry.SabotageBeat) beats++;
            }
            Expect(beats == 1, "exactly one level carries the sabotage beat (" + beats + ")");
        }

        private static int CountSabotaged(int fromArea, int toArea)
        {
            int total = 0, sabotaged = 0;
            for (int areaId = fromArea; areaId <= toArea; areaId++)
                foreach (int levelNumber in StoryLevelPlan.LevelsFor(areaId))
                {
                    total++;
                    if (StorySabotage.IsSabotaged(levelNumber)) sabotaged++;
                }
            return total == 0 ? 0 : sabotaged * 100 / total;
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
