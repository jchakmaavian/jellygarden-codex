// Proves the universal-powerup rules on a live board, from the log.
//
// Two rules were added to the kit and both are invisible to any headless test,
// because they only exist once a board is running:
//
//   tapping a rocket or a bomb fires it where it sits, no match required
//   a powerup is colour independent — it is never absorbed into a colour match
//
// Driving the Game view with synthetic clicks to check that turned out to be
// unreliable, so this presses the same buttons the player's finger would, in
// code, and prints what happened. It runs itself once per play session:
// this project has the Input System package as its only active input
// handler, so a legacy UnityEngine.Input keypress throws rather than
// reads, and a hotkey is not available here.
//
// Editor and development builds only; it never installs itself in a release
// player.

using System.Collections;
using System.Collections.Generic;
using JellyGarden.Scripts;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JellyGarden.Scripts.Story
{
    [DisallowMultipleComponent]
    public sealed class StoryPowerupTest : MonoBehaviour
    {
        private static StoryPowerupTest _instance;

        public static void Install()
        {
            if (!Application.isEditor && !Debug.isDebugBuild) return;
            if (_instance != null) return;
            // No LevelManager check: this host survives scene loads and only acts
            // on F9, so it must install from the menu scene too — that is where
            // the tester is standing when they press it.

            var host = new GameObject("StoryPowerupTest");
            Object.DontDestroyOnLoad(host);
            _instance = host.AddComponent<StoryPowerupTest>();
            Debug.Log("[Story] Powerup self-test armed; it runs itself once.");
        }

        private bool _running;
        private bool _ranOnce;

        /// <summary>
        /// Whether this has had its one run and handed the level back. The shell
        /// self-test waits on it: this one loads the game scene and plays a level
        /// in it, and an area screen opened over that measures the collision
        /// rather than the shell. True when nothing is installed, so a build with
        /// no board test to wait for is not waited on.
        /// </summary>
        public static bool IsFinished
        {
            get { return _instance == null || (_instance._ranOnce && !_instance._running); }
        }

        private void Start()
        {
            // Run itself once per play session. Sending a keystroke to the Game
            // view needs that view focused, which is not reliable to arrange from
            // outside the Editor, so the test does not depend on input at all.
            StartCoroutine(AutoRun());
        }

        private IEnumerator AutoRun()
        {
            yield return new WaitForSeconds(2f);
            if (_ranOnce || _running) yield break;
            _ranOnce = true;
            _running = true;
            yield return Drive();
        }



        /// <summary>
        /// Get to a playable board without anyone having to click a map node,
        /// then run the checks. Driving the Game view with synthetic clicks was
        /// unreliable enough to be worth removing from the test entirely.
        /// </summary>
        private IEnumerator Drive()
        {
            if (LevelManager.THIS == null ||
                LevelManager.THIS.gameStatus != GameState.Playing)
            {
                // The menu scene has no LevelManager, so load the board scene
                // first and wait for it rather than asking the tester to click
                // Play. StorySceneHooks reinstalls this host on the way in.
                if (LevelManager.THIS == null)
                {
                    Debug.Log("[Story] Powerup test: loading the game scene.");
                    SceneManager.LoadScene("game");
                    float sceneWait = 0f;
                    while (sceneWait < 20f && LevelManager.THIS == null)
                    {
                        yield return new WaitForSeconds(0.25f);
                        sceneWait += 0.25f;
                    }
                    yield return new WaitForSeconds(1.5f);
                }

                if (LevelManager.THIS == null)
                {
                    Debug.LogWarning("[Story] Powerup test: game scene never produced a LevelManager.");
                    _running = false;
                    yield break;
                }

                Debug.Log("[Story] Powerup test: launching level 1 of area 1.");
                StoryFlow.LaunchLevel(1, 1);

                float waited = 0f;
                while (waited < 25f &&
                       (LevelManager.THIS == null ||
                        LevelManager.THIS.gameStatus != GameState.Playing))
                {
                    yield return new WaitForSeconds(0.25f);
                    waited += 0.25f;
                }
                yield return new WaitForSeconds(1.5f);
            }

            if (LevelManager.THIS == null ||
                LevelManager.THIS.gameStatus != GameState.Playing)
            {
                Debug.LogWarning("[Story] Powerup test: could not reach a board.");
                _running = false;
                yield break;
            }

            yield return RunAll();

            // Give the level back. This test launches level 1 on every play
            // session, and before it handed the level back it left the shell "in
            // a level" for the rest of the session: the nav bar stayed hidden and
            // home never came back. HandleLevelComplete is the game's own way out
            // of a level, so this is not a second path to keep in step.
            StoryFlow.HandleLevelComplete();

            _running = false;
        }

        private IEnumerator RunAll()
        {
            Debug.Log("[Story] ---- universal powerup self-test ----");
            yield return ColourIndependence();
            yield return TapFires(ItemsTypes.HORIZONTAL_STRIPPED, "rocket across");
            yield return TapFires(ItemsTypes.VERTICAL_STRIPPED, "rocket up");
            yield return TapFires(ItemsTypes.PACKAGE, "bomb");
            Debug.Log("[Story] ---- self-test done ----");
        }

        /// <summary>A powerup must never be counted into a colour match.</summary>
        private IEnumerator ColourIndependence()
        {
            Made made = new Made();
            yield return Make(ItemsTypes.HORIZONTAL_STRIPPED, made);
            if (made.item == null)
            {
                Debug.LogWarning("[Story] Powerup test: conversion did not take (" + made.why + ").");
                yield break;
            }

            Item now = made.item;
            int colour = made.colour;

            // Ask the board for every match it can see, and check the powerup is
            // in none of them — including runs of its own former colour.
            int appearances = 0;
            List<List<Item>> all = LevelManager.THIS.GetMatches();
            foreach (List<Item> combo in all)
                foreach (Item item in combo)
                    if (item == now) appearances++;

            Debug.Log(string.Format(
                "[Story] colour independence: powerup still carries colour {0}; " +
                "board reports {1} match group(s); powerup appears in {2}. {3}",
                colour, all.Count, appearances,
                appearances == 0 ? "PASS" : "FAIL"));
        }

        /// <summary>A tap must fire the powerup and charge one move.</summary>
        private IEnumerator TapFires(ItemsTypes type, string label)
        {
            Made made = new Made();
            yield return Make(type, made);
            if (made.item == null)
            {
                Debug.LogWarning("[Story] Powerup test: could not make a " + label +
                                 " (" + made.why + ").");
                yield break;
            }

            Item powerup = made.item;

            // Snapshot exactly what this powerup should hit, so the measurement
            // is of the powerup's effect and not of how fast the board refills.
            // Counting the whole board after a second measures the refill, which
            // is why the first run of this test read "cleared 0" for a rocket
            // that had plainly fired.
            List<Item> targets = ExpectedTargets(powerup, type);
            int aimedAt = targets.Count;
            int movesBefore = LevelManager.THIS.Limit;
            bool canTap = powerup.CanTapActivate();
            string blockedBy = WhyNotTappable(powerup);

            bool fired = powerup.TapActivate();
            yield return new WaitForSeconds(0.25f);

            int hit = 0;
            foreach (Item item in targets)
                if (item == null || item.destroying) hit++;

            int movesAfter = LevelManager.THIS.Limit;
            bool pass = fired && hit > 0 && movesAfter == movesBefore - 1;

            Debug.Log(string.Format(
                "[Story] tap {0}: canTap={1}{2} fired={3} hit {4} of the {5} it aimed at, " +
                "moves {6}->{7}. {8}",
                label, canTap, canTap ? "" : " (blocked by " + blockedBy + ")",
                fired, hit, aimedAt, movesBefore, movesAfter,
                pass ? "PASS" : "FAIL"));
        }

        /// <summary>
        /// Which guard in CanTapActivate said no. A bare "canTap=False" cannot
        /// tell a real rule failure apart from a board that had not finished
        /// settling when the test reached for it, and those want opposite fixes.
        /// </summary>
        private static string WhyNotTappable(Item item)
        {
            if (item == null) return "the item is gone";
            if (item.destroying) return "it is already destroying";
            if (item.falling) return "it is still falling";
            if (item.square == null) return "it has no square";
            if (LevelManager.THIS == null) return "there is no LevelManager";
            if (LevelManager.THIS.DragBlocked) return "the board is busy (DragBlocked)";
            if (LevelManager.THIS.gameStatus != GameState.Playing)
                return "gameStatus is " + LevelManager.THIS.gameStatus;
            if (!Item.IsUniversalPowerup(item)) return "it is not a powerup: " + item.currentType;
            return "the type is not tap-activated: " + item.currentType;
        }

        /// <summary>The items this powerup is supposed to take out.</summary>
        private static List<Item> ExpectedTargets(Item powerup, ItemsTypes type)
        {
            List<Item> targets = new List<Item>();
            if (type == ItemsTypes.HORIZONTAL_STRIPPED)
                targets.AddRange(LevelManager.THIS.GetRow(powerup.square.row));
            else if (type == ItemsTypes.VERTICAL_STRIPPED)
                targets.AddRange(LevelManager.THIS.GetColumn(powerup.square.col));
            else
                targets.AddRange(LevelManager.THIS.GetItemsAround(powerup.square));

            targets.RemoveAll(item => item == null || item == powerup);
            return targets;
        }

        /// <summary>What Make produced, and why it gave up if it did not.</summary>
        private sealed class Made
        {
            public Item item;
            public int colour;
            public string why = "no attempt was made";
        }

        /// <summary>
        /// Turn a plain item into a powerup and hand it back only once the board
        /// has actually settled on it.
        ///
        /// Setting debugType is a request, not a result: the board applies it on
        /// its own schedule, and an item picked while a cascade is still running
        /// can be matched away or refilled over before the conversion lands. A
        /// fixed WaitForSeconds after the assignment therefore passed or failed
        /// by luck — across three consecutive runs of this test each of the four
        /// checks failed at least once and passed at least once, always with the
        /// board busy rather than the rule broken.
        ///
        /// So: wait for an idle board, convert, then poll for the conversion,
        /// and if the item was taken from under us, pick another and try again.
        /// </summary>
        private IEnumerator Make(ItemsTypes type, Made made)
        {
            for (int attempt = 1; attempt <= 4; attempt++)
            {
                yield return WaitForIdleBoard();

                Item subject = PickPlainItem();
                if (subject == null)
                {
                    made.why = "no plain item on the board";
                    yield return new WaitForSeconds(0.4f);
                    continue;
                }

                Square square = subject.square;
                int colour = subject.color;
                subject.debugType = type;

                // Poll rather than sleep a fixed time: the conversion normally
                // lands within a frame or two, and waiting the full second when
                // it already has is what let the next cascade catch up with us.
                float waited = 0f;
                while (waited < 2f)
                {
                    yield return new WaitForSeconds(0.1f);
                    waited += 0.1f;

                    Item now = square.item;
                    if (now == null || now.destroying || now.falling) continue;
                    if (now.currentType != type) continue;
                    if (LevelManager.THIS.DragBlocked) continue;

                    made.item = now;
                    made.colour = colour;
                    made.why = "";
                    yield break;
                }

                made.why = "the conversion did not land in 2s (attempt " + attempt + " of 4)";
            }
        }

        /// <summary>
        /// Nothing falling, nothing mid-destroy. Converting an item that is still
        /// in flight silently does nothing, which is what made the bomb case fail
        /// the first time this ran.
        /// </summary>
        private IEnumerator WaitForIdleBoard()
        {
            float waited = 0f;
            while (waited < 6f)
            {
                bool busy = LevelManager.THIS.DragBlocked;
                if (!busy)
                {
                    for (int col = 0; col < LevelManager.THIS.maxCols && !busy; col++)
                        for (int row = 0; row < LevelManager.THIS.maxRows && !busy; row++)
                        {
                            Square square = LevelManager.THIS.GetSquare(col, row);
                            if (square != null && square.item != null &&
                                (square.item.falling || square.item.destroying))
                                busy = true;
                        }
                }
                if (!busy) yield break;
                yield return new WaitForSeconds(0.25f);
                waited += 0.25f;
            }
        }

        private static Item PickPlainItem()
        {
            List<Item> plain = new List<Item>();
            for (int col = 0; col < LevelManager.THIS.maxCols; col++)
            {
                for (int row = 0; row < LevelManager.THIS.maxRows; row++)
                {
                    Square square = LevelManager.THIS.GetSquare(col, row);
                    if (square == null || square.item == null) continue;
                    Item item = square.item;
                    if (item.destroying || item.falling) continue;
                    if (item.currentType != ItemsTypes.NONE) continue;
                    plain.Add(item);
                }
            }
            if (plain.Count == 0) return null;
            return plain[Random.Range(0, plain.Count)];
        }

        private static int CountItems()
        {
            int total = 0;
            for (int col = 0; col < LevelManager.THIS.maxCols; col++)
                for (int row = 0; row < LevelManager.THIS.maxRows; row++)
                {
                    Square square = LevelManager.THIS.GetSquare(col, row);
                    if (square != null && square.item != null && !square.item.destroying)
                        total++;
                }
            return total;
        }
    }
}
