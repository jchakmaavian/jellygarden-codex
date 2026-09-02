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
using JellyGarden.Scripts.GUI;
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
                AreaProgressCountsTheRepairs();
                RepairIsOfferedOnlyForTheNextAffordableTask();
                SpendingStarsCompletesTheAreaAndUnlocksTheNext();
                ClueCardShowsTheClueVerbatimAndDismissesItself();
                DialogueTypesOutAndAdvancesOnTap();
                ArcBeatsHaveScenesAndPlayThemOnce();
                OpeningPlaysAfterTheFirstWinAndOnlyOnce();
                GeneratedArtIsWiredUp();
                SabotageStartsWhereTheStorySaysItDoes();
                TheGameIsFree();
                RepairsAreClassifiedByWhatTheyAre();
                EveryArcHasATheme();
                EveryAreaOpens();
                RoyalPrimitivesConstruct();
                LevelHudIsTargetAndMoves();
                ScoreLevelsShowTheirPassMark();
                LevelPopupStatesTheGoal();
                LevelPopupOffersBoosters();
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

        /// <summary>
        /// The Area Progress bar is the area's own arithmetic — repairs done over
        /// repairs the story wrote — and the chest sits on the end of it.
        /// </summary>
        private static void AreaProgressCountsTheRepairs()
        {
            StoryProgress.ResetAll();
            StoryArea area = StoryDatabase.GetArea(1);

            AreaScreen.Open(1);
            AreaScreen screen = AreaScreen.Instance;

            Expect(screen.TitleText == area.Name,
                "the title plate reads the area's name (read '" + screen.TitleText + "')");
            Expect(screen.ProgressText == "0/" + area.Tasks.Count,
                "an untouched area reads 0/" + area.Tasks.Count +
                " (read '" + screen.ProgressText + "')");
            Expect(screen.ProgressFraction == 0f, "an untouched bar is drawn empty");
            Expect(screen.HasChest, "the chest is on the end of the bar");

            StoryTask first = area.Tasks[0];
            StoryProgress.AddStar(1, first.Stars);
            Expect(StoryProgress.CompleteTask(1, first.Index), "the first repair was paid for");
            screen.Refresh();

            Expect(screen.ProgressText == "1/" + area.Tasks.Count,
                "one repair reads 1/" + area.Tasks.Count +
                " (read '" + screen.ProgressText + "')");
            Expect(Mathf.Abs(screen.ProgressFraction - 1f / area.Tasks.Count) < 0.001f,
                "the bar is filled one task's worth");

            Debug.Log(string.Format("[Story] area progress: '{0}' reads {1}, chest {2}",
                screen.TitleText, screen.ProgressText, screen.HasChest ? "present" : "missing"));

            StoryProgress.ResetAll();
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

            // Every arc needs a backdrop and every speaking character a face, or
            // the game degrades to flat colour somewhere in the middle of itself.
            foreach (string theme in new[]
                     { "castle", "village", "mines", "border", "crypts", "siege" })
                Expect(StoryArt.Load("backdrop_" + theme) != null,
                    "arc theme '" + theme + "' has a backdrop");

            foreach (string character in new[]
                     { "Leo", "Freya", "Grimm", "Mimi", "Cook", "Gardener", "Alfred", "Roderick" })
                Expect(StoryArt.Portrait(character) != null,
                    character + " has a portrait");

            // And every one of the 150 areas must resolve to something drawable.
            int blank = 0;
            for (int areaId = 1; areaId <= StoryDatabase.AreaCount; areaId++)
                if (StoryArt.AreaState(areaId, 0) == null &&
                    StoryArt.ArcBackdrop(areaId) == null) blank++;
            Expect(blank == 0, "every area has something to draw (" + blank + " do not)");
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

        private static void TheGameIsFree()
        {
            // TECH.md: no ads, no IAP, no lives gate. These are the four gates
            // every monetisation path in the kit passes through.
            Expect(StoryFreeGame.Enabled, "the free-game rules are on");
            Expect(!StoryFreeGame.ShouldSpendLife, "playing a level costs no life");
            Expect(!StoryFreeGame.AdsAllowed, "no ad may be requested");
            Expect(!StoryFreeGame.ShopsAllowed, "no shop may open");

            // Losing must never take a life, however many times it happens.
            if (InitScript.Instance != null)
            {
                int before = InitScript.lifes;
                InitScript.Instance.SpendLife(1);
                InitScript.Instance.SpendLife(1);
                Expect(InitScript.lifes == before,
                    "lives are untouched by playing (" + before + " -> " +
                    InitScript.lifes + ")");
            }

            // Boosters are earned. The same level always grants the same one, so
            // a replay cannot farm them. Every booster the game can grant is
            // counted, in-level and pre-level alike.
            string[] boosterKeys =
            {
                "Hand", "Stripes", "Bomb", "Random_color", "Packages", "Colorful_bomb"
            };

            foreach (string key in boosterKeys) PlayerPrefs.DeleteKey(key);

            StoryFreeGame.GrantBoosterForWin(1);
            int granted = boosterKeys.Sum(k => PlayerPrefs.GetInt(k, 0));
            Expect(granted == 1, "winning a level grants exactly one booster");

            foreach (string key in boosterKeys) PlayerPrefs.DeleteKey(key);
            PlayerPrefs.DeleteKey("Story.StarterBoostersGranted");
            StoryFreeGame.GrantStarterBoosters();
            Expect(PlayerPrefs.GetInt("Hand", 0) == 2 && PlayerPrefs.GetInt("Random_color", 0) == 1,
                "first run grants two hammers and a shuffle so the mechanic is discoverable");

            StoryFreeGame.GrantStarterBoosters();
            Expect(PlayerPrefs.GetInt("Hand", 0) == 2,
                "the starter grant happens once, not every boot");

            // ...and one of each booster the level popup offers, so its row is
            // never three locked slots on a save that has never seen one.
            foreach (string key in boosterKeys) PlayerPrefs.DeleteKey(key);
            PlayerPrefs.DeleteKey("Story.StarterPreBoostersGranted");
            StoryFreeGame.GrantStarterPreBoosters();
            Expect(StoryBoosters.PreLevel.All(b => PlayerPrefs.GetInt("" + b, 0) == 1),
                "first run grants one of each pre-level booster");

            StoryFreeGame.GrantStarterPreBoosters();
            Expect(StoryBoosters.PreLevel.All(b => PlayerPrefs.GetInt("" + b, 0) == 1),
                "the pre-level starter grant happens once, not every boot");
        }

        private static void RepairsAreClassifiedByWhatTheyAre()
        {
            // The cutscene picks its effect from the task's own words, so a
            // misclassification is a lantern being swept away or rubble catching
            // fire. These are the cases the wording actually turns on.
            var expected = new Dictionary<string, RepairWorkType>
            {
                { "Cut back the ivy choking the portcullis", RepairWorkType.Clear },
                { "Replace the rotted gate timbers", RepairWorkType.Rebuild },
                { "Rehang the royal banner", RepairWorkType.Hang },
                { "Relight the gate lanterns", RepairWorkType.Light },
                { "Repave the cracked stones", RepairWorkType.Rebuild },
                { "Sweep out the debris", RepairWorkType.Clear },
                { "Restock the pantry shelves", RepairWorkType.Restock },
                { "Polish the great doors", RepairWorkType.Polish },
                { "Break the cell door", RepairWorkType.Force },

                // The ones that hinge on reading the whole label: both start with
                // "clear", and one of them is water.
                { "Clear the flooded corridor", RepairWorkType.Water },
                { "Clear the last rubble", RepairWorkType.Clear },
                { "Drain the bath house", RepairWorkType.Water },
            };

            foreach (KeyValuePair<string, RepairWorkType> pair in expected)
            {
                RepairWorkType actual = RepairWork.Classify(pair.Key);
                Expect(actual == pair.Value,
                    "'" + pair.Key + "' classifies as " + pair.Value + " (got " + actual + ")");
            }

            // And no task anywhere in the story may fall through without a line
            // to say, or a repair plays in silence.
            int voiceless = 0;
            foreach (StoryArea area in StoryDatabase.AllAreas)
            {
                foreach (StoryTask task in area.Tasks)
                {
                    RepairWorkType work = RepairWork.Classify(task.Label);
                    if (string.IsNullOrEmpty(RepairWork.WorkingLine(work)) ||
                        string.IsNullOrEmpty(RepairWork.CompletedLine(work)) ||
                        string.IsNullOrEmpty(RepairWork.SoundKey(work)))
                        voiceless++;
                }
            }
            Expect(voiceless == 0,
                "every one of the 807 repairs has a line and a sound (" + voiceless + " do not)");

            // A repair should not be classified General very often; that would
            // mean the classifier is not reading the story's words.
            int general = 0, total = 0;
            foreach (StoryArea area in StoryDatabase.AllAreas)
                foreach (StoryTask task in area.Tasks)
                {
                    total++;
                    if (RepairWork.Classify(task.Label) == RepairWorkType.General) general++;
                }
            int percent = total == 0 ? 0 : general * 100 / total;
            Expect(percent <= 15,
                "at most 15% of repairs fall through to a generic effect (" + percent + "%)");
            Debug.Log(string.Format(
                "[Story] Repair classification: {0} of {1} tasks generic ({2}%).",
                general, total, percent));
        }

        private static void EveryArcHasATheme()
        {
            Expect(StoryTheme.Count == 6, "all six arcs have a theme (" + StoryTheme.Count + ")");

            for (int arc = 1; arc <= 6; arc++)
            {
                ArcTheme theme = StoryTheme.ForArc(arc);
                if (theme == null)
                {
                    Fail("arc " + arc + " has no theme");
                    return;
                }
                Expect(!string.IsNullOrEmpty(theme.ThemeKey), "arc " + arc + " has a theme key");
            }

            // Every level must resolve to a theme, or a board somewhere in the
            // game falls back to whatever the previous one left behind.
            int unthemed = 0;
            for (int levelNumber = 1; levelNumber <= StoryLevelPlan.LevelCount; levelNumber++)
                if (StoryTheme.ForLevel(levelNumber) == null) unthemed++;
            Expect(unthemed == 0, "every level resolves to an arc theme (" + unthemed + " do not)");

            // The six arcs must not all land on the same look. Background picks
            // one of five shipped pictures by currentLevel / 20, which across
            // 1209 levels is one picture for ninety per cent of the game; that is
            // the regression this replaces.
            var keys = new HashSet<string>();
            var tints = new HashSet<string>();
            for (int arc = 1; arc <= 6; arc++)
            {
                ArcTheme theme = StoryTheme.ForArc(arc);
                keys.Add(theme.ThemeKey);
                tints.Add(theme.SquareTint.ToString());
            }
            Expect(keys.Count == 6, "the six arcs are six different themes");
            Expect(tints.Count >= 5, "the arcs do not all tint the board the same");
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

        /// <summary>
        /// Builds every Royal Match primitive once and reports what it produced.
        ///
        /// These are shapes, not behaviour: there is nothing to assert about a
        /// gold rim beyond that it was drawn. What can go wrong is a null sprite,
        /// a missing font or a procedural texture that throws — so each primitive
        /// is constructed for real and one line per primitive says what came out.
        /// </summary>
        private static void RoyalPrimitivesConstruct()
        {
            var host = new GameObject("~StoryRoyalPrimitives");
            try
            {
                Canvas canvas = StoryUi.CreateOverlayCanvas("~RoyalPrimitivesCanvas");
                canvas.transform.SetParent(host.transform, false);
                var root = (RectTransform)canvas.transform;

                bool closed = false;
                StoryUi.RoyalPanelParts panel =
                    StoryUi.RoyalPanel("Panel", root, "Throne Room", () => closed = true);

                Expect(panel != null && panel.Root != null, "RoyalPanel builds a root");
                Expect(panel.Title != null && panel.Title.text == "Throne Room",
                    "RoyalPanel carries its title");
                Expect(panel.Title != null && panel.Title.font != null,
                    "RoyalPanel's title has a font");
                // A rect of negative height draws nothing, which looks exactly
                // like a panel with no title rather than like a bug.
                Canvas.ForceUpdateCanvases();
                Expect(panel.Title != null && panel.Title.rectTransform.rect.height > 1f,
                    "RoyalPanel's title has a rect it can be drawn in (height " +
                    (panel.Title == null ? 0f : panel.Title.rectTransform.rect.height) + ")");
                Expect(panel.Content != null, "RoyalPanel exposes a content rect");
                Expect(panel.Close != null, "RoyalPanel has a close X");

                if (panel.Close != null) panel.Close.onClick.Invoke();
                Expect(closed, "the close X invokes its action");

                Debug.Log("[Story] primitive RoyalPanel: title '" +
                          (panel.Title == null ? "<none>" : panel.Title.text) +
                          "', " + panel.Root.GetComponentsInChildren<Image>(true).Length +
                          " images, close X " + (panel.Close != null ? "present" : "absent"));

                Image card = StoryUi.CreamCard("Card", panel.Content);
                Expect(card != null && card.sprite != null, "CreamCard draws a rounded sprite");
                Debug.Log("[Story] primitive CreamCard: " + card.color + " over " +
                          card.rectTransform.childCount + " child layer(s)");

                int greenClicks = 0;
                Button play = StoryUi.GreenButton("Play", panel.Content, "Play", 72,
                    () => greenClicks++);
                Expect(play != null, "GreenButton builds");
                play.onClick.Invoke();
                Expect(greenClicks == 1, "GreenButton invokes its action");

                StoryUi.SetGreenInteractable(play, false);
                Expect(!play.interactable, "GreenButton greys out rather than disappearing");
                StoryUi.SetGreenInteractable(play, true);

                Text playLabel = play.GetComponentInChildren<Text>();
                Debug.Log("[Story] primitive GreenButton: label '" +
                          (playLabel == null ? "<none>" : playLabel.text) + "', interactable " +
                          play.interactable);

                RectTransform badge = StoryUi.StarBadge("Badge", panel.Content, 2);
                Text badgeCount = badge.GetComponentInChildren<Text>();
                Expect(badgeCount != null && badgeCount.text == "2",
                    "StarBadge shows the cost it was given");
                Debug.Log("[Story] primitive StarBadge: cost '" +
                          (badgeCount == null ? "<none>" : badgeCount.text) + "', star sprite " +
                          (StoryUi.StarSprite != null ? "loaded" : "missing, drawn as gold"));

                Button doIt = StoryUi.DoItButton("Repair", panel.Content, "Do It", 1, null);
                List<string> doItTexts = TextsIn(doIt.gameObject);
                Expect(doItTexts.Contains("Do It"), "DoItButton keeps its label");
                Expect(doItTexts.Contains("1"), "DoItButton carries the task's star cost");
                Debug.Log("[Story] primitive DoItButton: " + string.Join(" ", doItTexts));

                bool settingsOpened = false;
                StoryUi.TopBarParts bar = StoryUi.TopBar(root, () => settingsOpened = true);
                Expect(bar != null && bar.Root != null, "TopBar builds a root");
                Expect(bar.Coins != null && bar.Lives != null && bar.Stars != null,
                    "TopBar carries coins, lives and stars");
                Expect(bar.Settings != null, "TopBar has a settings gear");

                // The values must be read, not invented: stars are the current
                // area's pool, so adding one has to show up on the next refresh.
                int areaId = StoryProgress.CurrentArea;
                int before = StoryProgress.StarsIn(areaId);
                StoryProgress.AddStar(areaId);
                StoryUi.RefreshTopBar(bar);
                Expect(bar.Stars.text == (before + 1).ToString(),
                    "TopBar reads the star pool live (showed '" + bar.Stars.text + "')");

                if (bar.Settings != null) bar.Settings.onClick.Invoke();
                Expect(settingsOpened, "the settings gear invokes its action");

                Debug.Log("[Story] primitive TopBar: coins '" + bar.Coins.text + "', lives '" +
                          bar.Lives.text + "', stars '" + bar.Stars.text + "', gear present");

                Expect(StoryUi.GearSprite != null, "the gear sprite draws");
                Expect(StoryUi.CircleSprite != null, "the circle sprite draws");
                Debug.Log("[Story] primitive drawn sprites: circle " +
                          StoryUi.CircleSprite.texture.width + "px, gear " +
                          StoryUi.GearSprite.texture.width + "px");
            }
            finally
            {
                Object.DestroyImmediate(host);
                StoryProgress.ResetAll();
            }
        }

        /// <summary>
        /// The level HUD reads Target and Moves and nothing else.
        ///
        /// There is no board in the Editor outside play mode, so the strip is
        /// driven through StoryLevelHud.Source — the same seam the board fills in
        /// live. What is checked is what a player would read off it: the target
        /// icons and their counts, the moves left, and the absence of any score.
        /// </summary>
        private static void LevelHudIsTargetAndMoves()
        {
            var host = new GameObject("~StoryLevelHudTest");
            global::System.Func<StoryLevelHud.HudModel> previous = StoryLevelHud.Source;
            try
            {
                Canvas canvas = StoryUi.CreateOverlayCanvas("~LevelHudCanvas");
                canvas.transform.SetParent(host.transform, false);

                var model = new StoryLevelHud.HudModel { LimitTitle = "Moves", LimitValue = "35" };
                model.Targets.Add(new StoryLevelHud.HudTarget { Remaining = 60 });
                model.Targets.Add(new StoryLevelHud.HudTarget { Remaining = 24 });
                StoryLevelHud.Source = () => model;

                StoryLevelHud hud = StoryLevelHud.Build(canvas.transform, 1f);
                hud.Refresh();

                List<string> texts = TextsIn(hud.gameObject);

                Expect(texts.Contains("Target"), "the HUD names its Target panel");
                Expect(texts.Contains("Moves"), "the HUD names its Moves panel");
                Expect(texts.Contains("35"), "the HUD shows the moves left (read '" +
                                             hud.LimitText + "')");
                Expect(hud.VisibleTargetCount == 2,
                    "the HUD draws one slot per target (drew " + hud.VisibleTargetCount + ")");
                Expect(texts.Contains("60") && texts.Contains("24"),
                    "the HUD shows each target's remaining count");
                Expect(!texts.Any(t => t.ToLowerInvariant().Contains("score")),
                    "the HUD has no score anywhere on it");

                // Collecting one has to move the number the player is watching.
                model.Targets[0].Remaining = 59;
                model.LimitValue = "34";
                hud.Refresh();
                texts = TextsIn(hud.gameObject);
                Expect(texts.Contains("59"), "a collected target counts down");
                Expect(texts.Contains("34"), "spending a move counts down");

                Debug.Log("[Story] level HUD: " + hud.LimitTitleText + " " + hud.LimitText +
                          ", " + hud.VisibleTargetCount + " target slot(s) [" +
                          string.Join(" ", texts.ToArray()) + "]");

                // A one-target level draws one slot, not two stale ones.
                model.Targets.RemoveAt(1);
                hud.Refresh();
                Expect(hud.VisibleTargetCount == 1,
                    "a level with one target draws one slot (drew " +
                    hud.VisibleTargetCount + ")");

                // The timed levels the kit ships say Time, not Moves.
                model.LimitTitle = "Time";
                model.LimitValue = "1:30";
                hud.Refresh();
                Expect(hud.LimitTitleText == "Time" && hud.LimitText == "1:30",
                    "a timed level's panel reads Time");
            }
            finally
            {
                StoryLevelHud.Source = previous;
                Object.DestroyImmediate(host);
            }
        }

        /// <summary>
        /// Pulling the score bar left a SCORE-mode level with nothing to read:
        /// its only objective is a number the bar used to carry. The Target
        /// panel now states that number and how close the player is to it.
        ///
        /// Driven from the shipped level data rather than a hand-built model, so
        /// this fails if the levels and the HUD ever come apart.
        /// </summary>
        private static void ScoreLevelsShowTheirPassMark()
        {
            int scoreLevel = FirstLevelOfMode(Target.SCORE);
            int otherLevel = FirstLevelOfMode(Target.BLOCKS);
            Expect(scoreLevel > 0, "the shipped levels include a SCORE level");
            Expect(otherLevel > 0, "the shipped levels include a non-SCORE level");
            if (scoreLevel <= 0 || otherLevel <= 0) return;

            var host = new GameObject("~StoryScoreHudTest");
            global::System.Func<StoryLevelHud.HudModel> previous = StoryLevelHud.Source;
            try
            {
                Canvas canvas = StoryUi.CreateOverlayCanvas("~ScoreHudCanvas");
                canvas.transform.SetParent(host.transform, false);

                StoryLevelHud.HudModel fresh = StoryLevelHud.ReadLevel(scoreLevel, 0);
                Expect(fresh != null, "level " + scoreLevel + " has HUD data");
                if (fresh == null) return;

                StoryLevelHud.Source = () => fresh;
                StoryLevelHud hud = StoryLevelHud.Build(canvas.transform, 1f);
                hud.Refresh();

                List<string> slots = hud.TargetTexts();
                Expect(hud.VisibleTargetCount > 0, "a SCORE level's Target panel is not empty");
                Expect(fresh.PassMark > 0,
                    "a SCORE level has a pass mark (read " + fresh.PassMark + ")");
                Expect(slots.Count > 0 && slots[0] == "0/" + fresh.PassMark,
                    "a SCORE level reads have/need (read '" +
                    string.Join(" ", slots.ToArray()) + "')");

                Debug.Log("[Story] SCORE level " + scoreLevel + ": Target [" +
                          string.Join(" ", slots.ToArray()) + "] " +
                          hud.LimitTitleText + " " + hud.LimitText);

                // Scoring has to move it: the bar was the only thing that did.
                StoryLevelHud.HudModel scored = StoryLevelHud.ReadLevel(scoreLevel, 25);
                StoryLevelHud.Source = () => scored;
                hud.Refresh();
                List<string> after = hud.TargetTexts();
                Expect(after.Count > 0 && after[0] == "25/" + fresh.PassMark,
                    "the panel states the score the player has (read '" +
                    string.Join(" ", after.ToArray()) + "')");

                // Past the mark it reads met, not overshot.
                StoryLevelHud.HudModel passed =
                    StoryLevelHud.ReadLevel(scoreLevel, fresh.PassMark + 500);
                StoryLevelHud.Source = () => passed;
                hud.Refresh();
                Expect(hud.TargetTexts()[0] == fresh.PassMark + "/" + fresh.PassMark,
                    "the panel stops at the pass mark (read '" + hud.TargetTexts()[0] + "')");
                Expect(passed.Targets[0].Done, "the score target reports done at the mark");

                // A non-SCORE level still lists the objectives it is really about.
                StoryLevelHud.HudModel other = StoryLevelHud.ReadLevel(otherLevel, 0);
                StoryLevelHud.Source = () => other;
                hud.Refresh();
                List<string> otherSlots = hud.TargetTexts();
                Expect(hud.VisibleTargetCount > 0, "a non-SCORE level's Target panel is not empty");
                Expect(otherSlots.Any(t => !t.Contains("/")),
                    "a non-SCORE level still shows a plain collect count (read '" +
                    string.Join(" ", otherSlots.ToArray()) + "')");

                Debug.Log("[Story] non-SCORE level " + otherLevel + " (mode " +
                          StoryLevelHud.ModeOf(otherLevel) + "): Target [" +
                          string.Join(" ", otherSlots.ToArray()) + "] " +
                          hud.LimitTitleText + " " + hud.LimitText);

                // The strip still never names the score, on any level.
                Expect(!TextsIn(hud.gameObject).Any(t => t.ToLowerInvariant().Contains("score")),
                    "the strip never says 'score'");

                // And no level anywhere draws an empty Target panel.
                int empty = 0;
                string firstEmpty = "none";
                int levels = 0;
                for (int level = 1; ; level++)
                {
                    StoryLevelHud.HudModel model = StoryLevelHud.ReadLevel(level, 0);
                    if (model == null) break;
                    levels++;
                    if (model.Targets.Count > 0) continue;
                    if (empty == 0) firstEmpty = level.ToString();
                    empty++;
                }
                Expect(levels > 0, "the levels are on disk");
                Expect(empty == 0, empty + " of " + levels +
                                   " levels draw an empty Target panel (first: " + firstEmpty + ")");
                Debug.Log("[Story] Target panel: " + levels +
                          " levels checked, none empty.");
            }
            finally
            {
                StoryLevelHud.Source = previous;
                Object.DestroyImmediate(host);
            }
        }

        /// <summary>
        /// The level start popup says which level it is and what the level
        /// wants, and nothing else. Play launches that level; the X does not.
        /// </summary>
        private static void LevelPopupStatesTheGoal()
        {
            int level = LevelWithMostGoals();
            Expect(level > 0, "a level with a readable goal exists");
            if (level <= 0) return;

            StoryLevelHud.HudModel model = StoryLevelHud.ReadLevel(level, 0);
            if (model == null) { Fail("level " + level + " has goal data"); return; }

            var launched = new List<string>();
            int closes = 0;
            global::System.Action<int, int> previousLauncher = StoryLevelPopup.Launcher;
            StoryLevelPopup.Launcher = (l, a) => launched.Add(l + "/" + a);

            try
            {
                StoryLevelPopup.Open(level, 1, () => closes++);
                StoryLevelPopup popup = StoryLevelPopup.Instance;
                if (popup == null) { Fail("the level popup opens"); return; }

                Expect(popup.TitleText == "Level " + level,
                    "the popup is titled 'Level " + level + "' (read '" + popup.TitleText + "')");

                List<string> goals = popup.GoalTexts();
                var expected = model.Targets
                    .Take(goals.Count)
                    .Select(t => Mathf.Max(0, t.Remaining).ToString())
                    .ToList();
                Expect(goals.Count > 0, "the popup lists at least one goal");
                Expect(goals.Count == Mathf.Min(model.Targets.Count, 4),
                    "the popup lists every one of the level's targets (" + goals.Count +
                    " of " + model.Targets.Count + ")");
                Expect(goals.SequenceEqual(expected),
                    "the goal counts come from the level's Target asset (read '" +
                    string.Join(" ", goals.ToArray()) + "', expected '" +
                    string.Join(" ", expected.ToArray()) + "')");

                // Royal Match does not show a move count before the board opens.
                List<string> texts = TextsIn(popup.gameObject);
                Expect(!texts.Any(t => t.ToLowerInvariant().Contains("move")),
                    "the popup never mentions moves");
                Expect(!texts.Any(t => t.ToLowerInvariant().Contains("time")),
                    "the popup never mentions the time limit");
                Expect(!texts.Any(t => t.ToLowerInvariant().Contains("score")),
                    "the popup never names the score");

                Debug.Log("[Story] Level popup: 'Level " + level + "' goal [" +
                          string.Join(" ", goals.ToArray()) + "], no moves shown.");

                // The X goes back to whatever opened it, without playing.
                Expect(popup.CloseButton != null, "the popup has a close X");
                if (popup.CloseButton != null) popup.CloseButton.onClick.Invoke();
                Expect(!StoryLevelPopup.IsOpen, "the X closes the popup");
                Expect(launched.Count == 0, "the X does not start the level");
                Expect(closes == 1, "the X returns to the screen that opened the popup");

                // Play starts that level, for that area.
                StoryLevelPopup.Open(level, 3, () => closes++);
                popup = StoryLevelPopup.Instance;
                Expect(popup.PlayButton != null, "the popup has a Play button");
                if (popup.PlayButton != null) popup.PlayButton.onClick.Invoke();
                Expect(!StoryLevelPopup.IsOpen, "Play closes the popup");
                Expect(launched.Count == 1 && launched[0] == level + "/3",
                    "Play launches level " + level + " for area 3 (got '" +
                    string.Join(" ", launched.ToArray()) + "')");
                Expect(closes == 1, "Play is not a close: the popup does not go back");
            }
            finally
            {
                StoryLevelPopup.Launcher = previousLauncher;
                StoryLevelPopup.Close();
            }
        }

        /// <summary>
        /// The "Select Boosters:" row: three slots, the owned count on each, a
        /// locked slot where the player owns none, and a choice that reaches the
        /// board rather than stopping at the popup.
        /// </summary>
        private static void LevelPopupOffersBoosters()
        {
            int level = LevelWithMostGoals();
            if (level <= 0) { Fail("a level with a readable goal exists"); return; }

            // Owned counts are read through a seam so the row can be driven at
            // chosen counts without writing the player's own PlayerPrefs.
            var owned = new Dictionary<BoostType, int>
            {
                { BoostType.Stripes, 4 },
                { BoostType.Packages, 0 },
                { BoostType.Colorful_bomb, 2 }
            };

            global::System.Func<BoostType, int> previousOwned = StoryBoosters.OwnedSource;
            global::System.Action<int, int> previousLauncher = StoryLevelPopup.Launcher;
            StoryBoosters.OwnedSource = b => owned.ContainsKey(b) ? owned[b] : 0;
            StoryLevelPopup.Launcher = (l, a) => { };

            try
            {
                StoryLevelPopup.Open(level, 1, () => { });
                StoryLevelPopup popup = StoryLevelPopup.Instance;
                if (popup == null) { Fail("the level popup opens"); return; }

                Expect(popup.BoosterCount == 3,
                    "the popup offers three booster slots (found " + popup.BoosterCount + ")");
                if (popup.BoosterCount != 3) return;

                List<string> texts = TextsIn(popup.gameObject);
                Expect(texts.Any(t => t.Contains("Select Boosters")),
                    "the row is headed 'Select Boosters:'");

                List<string> counts = popup.BoosterCounts();
                var expected = StoryBoosters.PreLevel.Select(b => owned[b].ToString()).ToList();
                Expect(counts.SequenceEqual(expected),
                    "each slot shows its owned count (read '" +
                    string.Join(" ", counts.ToArray()) + "', expected '" +
                    string.Join(" ", expected.ToArray()) + "')");

                for (int i = 0; i < 3; i++)
                {
                    BoostType boost = popup.BoosterAt(i);
                    bool shouldLock = owned[boost] <= 0;
                    Expect(popup.BoosterIsLocked(i) == shouldLock,
                        boost + " renders " + (shouldLock ? "locked" : "unlocked"));
                    Expect(popup.BoosterButton(i).gameObject.activeInHierarchy,
                        boost + " is present rather than hidden");
                }

                // A slot the player owns none of cannot be chosen.
                int lockedIndex = IndexOfBooster(popup, BoostType.Packages);
                popup.BoosterButton(lockedIndex).onClick.Invoke();
                Expect(!popup.BoosterIsSelected(lockedIndex),
                    "a locked slot cannot be selected");
                Expect(!StoryBoosters.IsSelected(BoostType.Packages),
                    "a locked booster never enters the selection");

                // A slot the player owns toggles on, and off again.
                int stripes = IndexOfBooster(popup, BoostType.Stripes);
                popup.BoosterButton(stripes).onClick.Invoke();
                Expect(popup.BoosterIsSelected(stripes), "tapping a slot selects it");
                Expect(StoryBoosters.IsSelected(BoostType.Stripes),
                    "the selection holds the chosen booster");

                popup.BoosterButton(stripes).onClick.Invoke();
                Expect(!popup.BoosterIsSelected(stripes), "tapping it again clears it");
                Expect(!StoryBoosters.IsSelected(BoostType.Stripes),
                    "clearing a slot removes it from the selection");

                // Two chosen boosters survive Play, which is how they reach the
                // board: LevelManager.OnLevelLoaded reads them there.
                popup.BoosterButton(stripes).onClick.Invoke();
                popup.BoosterButton(IndexOfBooster(popup, BoostType.Colorful_bomb)).onClick.Invoke();
                popup.PlayButton.onClick.Invoke();

                List<BoostType> chosen = StoryBoosters.Selection();
                Expect(chosen.Count == 2 &&
                       chosen.Contains(BoostType.Stripes) &&
                       chosen.Contains(BoostType.Colorful_bomb),
                    "Play carries the selection into the level (" + chosen.Count + " chosen)");
                Expect(StoryBoosters.AmountOf(BoostType.Stripes) == 5 &&
                       StoryBoosters.AmountOf(BoostType.Colorful_bomb) == 1,
                    "each booster converts the same number of items as the kit's own row");

                Debug.Log("[Story] Level popup boosters: counts [" +
                          string.Join(" ", counts.ToArray()) + "], " +
                          BoostType.Packages + " locked, chosen [" +
                          string.Join(" ", chosen.Select(b => b.ToString()).ToArray()) + "].");

                // Backing out of the popup chooses nothing.
                StoryLevelPopup.Open(level, 1, () => { });
                Expect(StoryBoosters.Selection().Count == 0,
                    "opening the popup starts from an empty selection");
                popup.BoosterButton(stripes).onClick.Invoke();
                popup.CloseButton.onClick.Invoke();
                Expect(StoryBoosters.Selection().Count == 0,
                    "the X drops the boosters the player had picked");
            }
            finally
            {
                StoryBoosters.OwnedSource = previousOwned;
                StoryBoosters.ClearSelection();
                StoryLevelPopup.Launcher = previousLauncher;
                StoryLevelPopup.Close();
            }
        }

        private static int IndexOfBooster(StoryLevelPopup popup, BoostType boost)
        {
            for (int i = 0; i < popup.BoosterCount; i++)
                if (popup.BoosterAt(i) == boost) return i;
            return 0;
        }

        /// <summary>
        /// The lowest-numbered level carrying more than one target, so the goal
        /// row is proven with more than a single slot in it.
        /// </summary>
        private static int LevelWithMostGoals()
        {
            int best = 0;
            int bestCount = 0;
            for (int level = 1; level <= 40; level++)
            {
                StoryLevelHud.HudModel model = StoryLevelHud.ReadLevel(level, 0);
                if (model == null) break;
                if (model.Targets.Count <= bestCount) continue;
                best = level;
                bestCount = model.Targets.Count;
            }
            return best;
        }

        /// <summary>The lowest-numbered level whose MODE line is this one.</summary>
        private static int FirstLevelOfMode(Target mode)
        {
            for (int level = 1; ; level++)
            {
                if (Resources.Load<TextAsset>("Levels/" + level) == null) return 0;
                if (StoryLevelHud.ModeOf(level) == mode) return level;
            }
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
                         "~StoryToast", "~StoryCutscene", StoryLevelHud.HostName,
                         StoryLevelPopup.HostName
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
