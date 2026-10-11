#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JellyGarden.Scripts.Story.Editor
{
    /// <summary>
    /// Plays the original Royal Throne Rocket, Color Ball, combos, match break and
    /// win finish on a real King Leo level, through the shipping Item paths:
    /// pieces made by ChangeType, rockets tapped, swaps made with SwitchDirection,
    /// the moves-left flight the pre-win sequence uses, and the win card. Frames
    /// land in Temp/original-boosters-validation. It never wins the level, so no
    /// progress or stars are written.
    /// </summary>
    public sealed class StoryOriginalBoostersValidation : MonoBehaviour
    {
        const string Output = "Temp/original-boosters-validation";
        const float Slow = .35f;
        int checks, frame;
        float savedTimeScale;
        bool savedBoosters, savedTnt, savedBomb;

        [MenuItem("JellyGarden/Story/Preview/Validate Original Boosters")]
        public static void Run()
        {
            if (!Application.isPlaying) throw new InvalidOperationException("Enter Play Mode before validating the boosters.");
            if (FindAnyObjectByType<StoryOriginalBoostersValidation>() != null) return;
            Directory.CreateDirectory(Output);
            foreach (var old in Directory.GetFiles(Output, "*.png")) File.Delete(old);
            var go = new GameObject("Original boosters validation");
            DontDestroyOnLoad(go);
            go.AddComponent<StoryOriginalBoostersValidation>().Begin();
        }

        void Begin()
        {
            savedTimeScale = Time.timeScale;
            savedBoosters = StoryOriginalBoosters.Enabled;
            savedTnt = StoryOriginalTnt.Enabled;
            savedBomb = StoryBombVfx.Enabled;
            File.WriteAllText(Output + "/progress.txt", "Starting original boosters validation\n");
            File.Delete(Output + "/result.txt");
            Time.timeScale = 1f;
            StartCoroutine(Guard(Validate()));
        }

        IEnumerator Guard(IEnumerator routine)
        {
            // Flatten nested coroutines so any assertion writes a result and restores the clock.
            var stack = new global::System.Collections.Generic.Stack<IEnumerator>();
            stack.Push(routine);
            while (stack.Count > 0)
            {
                object value = null;
                Exception error = null;
                try
                {
                    if (!stack.Peek().MoveNext()) stack.Pop();
                    else value = stack.Peek().Current;
                }
                catch (Exception e) { error = e; }
                if (error != null)
                {
                    File.WriteAllText(Output + "/result.txt", "FAIL after " + checks + " checks\n" + error);
                    Debug.LogException(error);
                    Destroy(gameObject);
                    yield break;
                }
                if (value is IEnumerator nested) stack.Push(nested);
                else yield return value;
            }
            File.WriteAllText(Output + "/result.txt", "PASS " + checks + " checks: gem break, fall, landing and refill, rockets, color ball, five combos, match break, pause, moves-left flight and win stars.");
            Destroy(gameObject);
        }

        void Require(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
            checks++;
            File.AppendAllText(Output + "/progress.txt", "check " + checks + " passed: " + message + "\n");
        }

        IEnumerator Until(Func<bool> condition, float seconds, string message)
        {
            float end = Time.realtimeSinceStartup + seconds;
            while (!condition() && Time.realtimeSinceStartup < end) yield return null;
            Require(condition(), message);
        }

        IEnumerator Shot(string label)
        {
            yield return new WaitForEndOfFrame();
            StoryWindow.Capture(Output + "/" + (frame++).ToString("00") + "-" + label + ".png");
        }

        IEnumerator Shots(string label, int count, float every)
        {
            for (int i = 0; i < count; i++) { yield return new WaitForSeconds(every); yield return Shot(label); }
        }

        IEnumerator Validate()
        {
            StoryOriginalBoosters.Enabled = true;
            StoryOriginalTnt.Enabled = true;
            StoryBombVfx.Enabled = true;
            Require(StoryOriginalBoosters.Ready, "Imported booster assets load");
            Require(StoryOriginalTnt.Ready, "Imported TNT assets load");
            SceneManager.LoadScene("game");
            yield return Until(() => LevelManager.THIS != null, 20f, "No game board manager");
            yield return new WaitForSecondsRealtime(1f);
            StoryFlow.LaunchLevel(3, 1);
            yield return Settled();
            // Keep test clears from ending the level or spending progression.
            var manager = LevelManager.THIS;
            manager.Limit = 100;
            manager.star1 = 1000000; manager.star2 = 2000000; manager.star3 = 3000000;
            yield return new WaitForSeconds(3f);
            // Far past any score: a score goal counts the score itself, and the combos clear a lot.
            foreach (var goal in manager.targetObject) goal.targetCount = int.MaxValue / 2;
            yield return Settled();
            Require(StoryOriginalBoosters.OwnsRocket, "The board's rockets fly as the original");

            // 0. Gem break, fall, landing and refill (StoryOriginalMotion).
            yield return Motion();

            // 1. Horizontal Rocket: original piece, creation clip, idle, tap, flight and timing.
            yield return RocketTapped(ItemsTypes.HORIZONTAL_STRIPPED, "rocket-h");
            // 2. Vertical Rocket, paused mid-flight.
            yield return RocketTapped(ItemsTypes.VERTICAL_STRIPPED, "rocket-v");

            // 3. The Color Ball by royal-throne's rules: swap, tap and its three combos.
            yield return ColorBallChecks();

            // 4-8. Combos: two specials side by side, swapped together.
            yield return Combo(ItemsTypes.HORIZONTAL_STRIPPED, ItemsTypes.VERTICAL_STRIPPED, "rocket-rocket",
                () => StoryOriginalBoosters.RocketFlights, 2);
            yield return Combo(ItemsTypes.HORIZONTAL_STRIPPED, ItemsTypes.PACKAGE, "rocket-tnt",
                () => StoryOriginalBoosters.RocketTntPlays, 1);
            yield return Combo(ItemsTypes.PACKAGE, ItemsTypes.PACKAGE, "tnt-tnt",
                () => StoryOriginalBoosters.TntTntPlays, 1);

            // 9. Plain gems broke with the original explosion along the way.
            Require(StoryOriginalBoosters.MatchBreaks > 0, "Plain gems broke with the original explosion");
            Require(!FindObjectsByType<StoryOriginalPiece>().Any(p => p.Showing &&
                    !IsPiece(p.GetComponent<Item>().currentType)), "No original piece left on a plain item");

            // 10. Moves left at a win: the same call PreWinAnimationsCor makes for each move.
            int sent = StoryOriginalBoosters.MovesSent;
            var picks = manager.GetRandomItems(3);
            Time.timeScale = Slow;
            foreach (var item in picks)
            {
                item.NextType = ItemsTypes.HORIZONTAL_STRIPPED;
                Require(StoryOriginalBoosters.SendMove(item), "A move left flies in");
                yield return new WaitForSeconds(.2f * Slow);
            }
            Require(StoryOriginalBoosters.MovesInFlight > 0, "Moves are in flight");
            yield return Shots("moves-fly", 4, .1f);
            yield return Until(() => StoryOriginalBoosters.MovesInFlight == 0, 6f, "Every move landed");
            Require(StoryOriginalBoosters.MovesSent == sent + picks.Count, "One flight per move");
            yield return Until(() => picks.All(i => i != null && i.currentType == ItemsTypes.HORIZONTAL_STRIPPED &&
                    i.GetComponent<StoryOriginalPiece>() != null && i.GetComponent<StoryOriginalPiece>().Showing),
                5f, "Each landed move became an original Rocket");
            yield return Shots("moves-landed", 2, .15f);
            Time.timeScale = 1f;
            foreach (var item in picks) if (item != null && item.CanTapActivate()) item.TapActivate();
            yield return Shots("moves-fire", 2, .12f);
            yield return Settled();

            // 11. The win card with the original stars (opened directly; the level is not won).
            StoryWinPopup.Open(manager.currentLevel, 1, false, null);
            StoryOriginalWinStars stars = null;
            yield return Until(() => (stars = FindAnyObjectByType<StoryOriginalWinStars>()) != null && stars.Playing,
                5f, "Win card plays the original stars");
            yield return new WaitForSecondsRealtime(1.2f);
            yield return Shot("win-stars");
            Require(stars.VisibleSparkles > 0, "Win stars emit sparkles");
            yield return new WaitForSecondsRealtime(1.5f);
            yield return Shot("win-stars-idle");
            StoryWinPopup.Close();

            StoryRoyalVfx.StateChanged(GameState.PrepareGame);
            Require(StoryOriginalBoosters.ActiveEffects == 0, "Level reset leaves no original effect");
        }

        // The match loop's own calls: MarkMatch with the group, then DestroyItem, for a row of gems.
        IEnumerator Motion()
        {
            Require(StoryOriginalMotion.Ready, "The Royal Throne gem motion is on");
            var manager = LevelManager.THIS;

            // A plain three breaks at once: no pop, the explosion on the first frame.
            int immediate = StoryOriginalMotion.ImmediateBreaks, breaks = StoryOriginalBoosters.MatchBreaks;
            var three = Row(3);
            Require(three != null, "A row of three plain gems");
            Time.timeScale = Slow;
            for (int i = 0; i < three.Length; i++) StoryOriginalMotion.MarkMatch(three[i], 3, i);
            foreach (var gem in three) gem.DestroyItem(true);
            manager.FindMatches();   // as a match or tap does: the cascade then falls and refills
            Require(StoryOriginalMotion.ImmediateBreaks == immediate + 3, "A three breaks at once");
            Require(StoryOriginalBoosters.MatchBreaks == breaks + 3, "Each gem of the three explodes on the first frame");
            Require(three.All(g => !g.sprRenderer.enabled), "The explosion replaces each gem's sprite");
            // Twice the original's dots and falling shards (StoryOriginalBoosters.BreakParticleScale), one ring.
            var burst = FindObjectsByType<Transform>().Where(t => t.name == "MatchItemExplodeParticles(Clone)").ToList();
            Require(burst.Count >= 3, "Each break plays the imported gem explosion");
            ParticleSystem Part(Transform root, Func<string, bool> name) =>
                root.GetComponentsInChildren<ParticleSystem>(true).First(p => name(p.name));
            float Burst(ParticleSystem ps) => ps.emission.GetBurst(0).count.constant;
            Require(burst.All(b => Mathf.Approximately(Burst(Part(b, n => n == "add_blast_dots")), 20f)), "Each break bursts 20 sparkle dots (2x)");
            // The shard layer in play is the one left active for the gem's colour.
            Require(burst.All(b => Mathf.Approximately(Burst(b.GetComponentsInChildren<ParticleSystem>(false)
                .First(p => p.name.StartsWith("blast_part_"))), 8f)), "Each break throws 8 coloured shards (2x)");
            Require(burst.All(b => Mathf.Approximately(Burst(Part(b, n => n == "add_light_ring")), 1f)), "Each break keeps one flash ring");
            yield return new WaitForSeconds(.05f);
            yield return Shot("break-2x");
            Require(burst.Where(b => b != null).Any(b => b.GetComponentsInChildren<ParticleSystem>().Sum(p => p.particleCount) >= 28),
                "A break shows its doubled dots and shards");

            // The refill: queued above the column's top cell, fading in, then falling and landing.
            int entries = StoryOriginalMotion.Entries, falls = StoryOriginalMotion.Falls, landings = StoryOriginalMotion.Landings;
            bool fading = false, squashed = false;
            float end = Time.realtimeSinceStartup + 8f;
            int shots = 0;
            while (Time.realtimeSinceStartup < end && (!fading || !squashed || StoryOriginalMotion.Landings < landings + 3))
            {
                foreach (var state in FindObjectsByType<StoryOriginalFall>())
                {
                    var item = state.GetComponent<Item>();
                    if (item == null || item.sprRenderer == null) continue;
                    if (!float.IsNaN(state.EntryEdge) && item.sprRenderer.color.a < .99f) fading = true;
                    if (state.Landing != null && item.currentType == ItemsTypes.NONE &&
                        Mathf.Abs(item.transform.localScale.y - item.transform.localScale.x) > .001f) squashed = true;
                }
                if (shots < 6 && StoryOriginalMotion.Falls > falls) { yield return Shot("refill"); shots++; yield return new WaitForSeconds(.04f); }
                else yield return null;
            }
            Require(StoryOriginalMotion.Entries >= entries + 3, "Three refill gems queued above the board");
            Require(fading, "Refill gems fade in across the board's edge");
            Require(StoryOriginalMotion.Falls >= falls + 3, "Gems fall with the Royal Throne gravity");
            Require(StoryOriginalMotion.Landings >= landings + 3, "Gems land with the Royal Throne bounce");
            Require(squashed, "A landing gem squashes");
            Time.timeScale = 1f;
            yield return Settled();
            Require(!FindObjectsByType<Item>().Any(i => i.sprRenderer != null && i.sprRenderer.enabled && i.sprRenderer.color.a < .99f && !i.falling),
                "Every settled gem is fully visible again");

            // A bigger group pulls in, squashes and rebounds before it breaks.
            int popped = StoryOriginalMotion.PoppedBreaks;
            var four = Row(4);
            Require(four != null, "A row of four plain gems");
            Time.timeScale = .2f;
            Vector3 rest = four[1].transform.localScale;
            for (int i = 0; i < four.Length; i++) StoryOriginalMotion.MarkMatch(four[i], 4, i);
            foreach (var gem in four) gem.DestroyItem(true);
            manager.FindMatches();
            Require(StoryOriginalMotion.PoppedBreaks == popped + 4, "A four pops before it breaks");
            bool deformed = false;
            for (int f = 0; f < 40 && !deformed; f++)
            {
                if (four[1] != null && four[1].sprRenderer.enabled && Mathf.Abs(four[1].transform.localScale.x - rest.x) > .002f) deformed = true;
                if (f % 8 == 0) yield return Shot("pop-four");
                yield return null;
            }
            Require(deformed, "The gem squashes and rebounds before breaking");
            Time.timeScale = 1f;
            yield return Settled();
            Require(StoryOriginalMotion.Breaking == 0, "Every break finished and released the cascade");
        }

        // A horizontal run of plain, settled gems with plain gems on the board's interior.
        static Item[] Row(int length)
        {
            var manager = LevelManager.THIS;
            for (int r = 1; r < manager.maxRows - 1; r++)
                for (int c = 0; c + length <= manager.maxCols; c++)
                {
                    var run = new Item[length];
                    bool ok = true;
                    for (int k = 0; k < length && ok; k++)
                    {
                        var sq = manager.GetSquare(c + k, r);
                        var item = sq != null ? sq.item : null;
                        ok = item != null && item.currentType == ItemsTypes.NONE && !item.destroying && !item.falling &&
                            sq.type == SquareTypes.EMPTY;
                        run[k] = item;
                    }
                    if (ok) return run;
                }
            return null;
        }

        IEnumerator RocketTapped(ItemsTypes type, string label)
        {
            var manager = LevelManager.THIS;
            Item rocket = Centre();
            Time.timeScale = Slow;
            yield return Make(rocket, type, label, true);
            Time.timeScale = 1f;
            int flights = StoryOriginalBoosters.RocketFlights;
            yield return Until(() => rocket != null && rocket.CanTapActivate(), 5f, label + " can activate");
            int moves = manager.Limit;
            Time.timeScale = Slow;
            Require(rocket.TapActivate(), label + " tap accepted");
            Require(manager.Limit == moves - 1, label + " charged one move");
            yield return Until(() => StoryOriginalBoosters.RocketFlights == flights + 1, 3f, label + " flew as the original");
            yield return Shots(label + "-flight", 3, .06f);
            Require(Mathf.Abs(StoryRoyalVfx.RocketReachesAt(3) - StoryOriginalBoosters.RocketReachesAt(3)) < 1e-5f,
                label + " clears cells on the original timing");

            if (type == ItemsTypes.VERTICAL_STRIPPED)
            {
                var half = FindObjectsByType<SpriteRenderer>().FirstOrDefault(r => r.name == "Original rocket half");
                Require(half != null, "A rocket half is in flight");
                manager.gameStatus = GameState.Pause;
                Vector3 held = half.transform.position;
                yield return new WaitForSecondsRealtime(.25f);
                Require(half == null || half.transform.position == held, "Rocket holds while the game is paused");
                manager.gameStatus = GameState.Playing;
            }
            Time.timeScale = 1f;
            yield return Settled();
            yield return Until(() => StoryOriginalBoosters.ActiveEffects == 0, 6f, label + " effects retired");
        }

        // RoyalPowerupDirector.Orb and its combos, through the board's own swap and tap paths.
        IEnumerator ColorBallChecks()
        {
            Require(StoryOriginalColorBall.Ready, "The Royal Throne Color Ball is on");

            // a. Swapped with a gem: that gem's colour, plain gems only; each held charged until the
            //    ball finishes, then all clear on that frame. A Rocket of the same colour is no target.
            Item ball = Centre();
            yield return Make(ball, ItemsTypes.BOMB, "color-ball");
            Item gem = PlainNeighbour(ball);
            Require(gem != null, "Color Ball has a plain neighbour");
            int colour = gem.color;
            Item special = FindObjectsByType<Item>().Where(i => StoryOriginalColorBall.Eligible(i) && i.color == colour &&
                    i != gem && i.square != null && Mathf.Abs(i.square.col - ball.square.col) + Mathf.Abs(i.square.row - ball.square.row) > 2)
                .FirstOrDefault();
            if (special != null) yield return Make(special, ItemsTypes.HORIZONTAL_STRIPPED, "same-colour-rocket");
            yield return Settled();
            int expected = FindObjectsByType<Item>().Count(i => StoryOriginalColorBall.Eligible(i) && i.color == colour);
            int plays = StoryOriginalColorBall.Plays, beams = StoryOriginalColorBall.Beams;
            int held = StoryOriginalColorBall.Held, cleared = StoryOriginalColorBall.Cleared;
            float finish = StoryOriginalColorBall.LastFinishAt;
            Time.timeScale = Slow;
            Swap(ball, gem);
            yield return Until(() => StoryOriginalColorBall.Plays == plays + 1, 5f, "Swap fired the Royal Throne Color Ball");
            Require(StoryOriginalColorBall.LastColour == colour, "It takes the swapped gem's colour");
            Require(StoryOriginalColorBall.LastTargets == expected,
                "It targets every plain gem of that colour, and nothing else (" + StoryOriginalColorBall.LastTargets + " of " + expected + ")");
            Require(StoryOriginalColorBall.Busy, "The board is held for the sequence");
            var targets = FindObjectsByType<Item>().Where(i => StoryOriginalColorBall.Eligible(i) && i.color == colour).ToList();
            bool sawHeld = false;
            int shots = 0;
            while (StoryOriginalColorBall.LastFinishAt == finish && StoryOriginalColorBall.Busy)
            {
                if (StoryOriginalColorBall.Held >= held + 2 && !sawHeld)
                {
                    sawHeld = true;
                    Require(targets.All(i => i != null && !i.destroying), "Reached gems stay on the board, charged, until the finish");
                    Require(FindObjectsByType<StoryOriginalChargedTarget>().Count(c => c.Held) >= 2, "Reached gems glow and tremble");
                }
                if (shots < 6) { yield return Shot("color-ball-beams"); shots++; yield return new WaitForSeconds(.08f); }
                else yield return null;
            }
            Require(sawHeld || expected < 2, "The beams held their gems");
            Require(StoryOriginalColorBall.Beams - beams == StoryOriginalColorBall.Held - held, "One beam reached, and held, each gem");
            Require(targets.All(i => i == null || i.destroying), "Every held gem cleared on the finish frame");
            Require(StoryOriginalColorBall.Cleared - cleared == StoryOriginalColorBall.Held - held, "They clear together, once each");
            if (special != null)
                Require(special != null && !special.destroying && special.currentType == ItemsTypes.HORIZONTAL_STRIPPED,
                    "A Rocket of that colour is not a target");
            yield return Shots("color-ball-finish", 3, .08f);
            Time.timeScale = 1f;
            yield return Settled();
            if (special != null && special.CanTapActivate()) { special.TapActivate(); yield return Settled(); }

            // b. Tapped: the colour most plain gems share.
            Item tapped = Centre();
            yield return Make(tapped, ItemsTypes.BOMB, "color-ball-tap");
            yield return Settled();
            Require(tapped.CanTapActivate(), "A Color Ball can be tapped");
            int common = StoryOriginalColorBall.MostCommonColour();
            plays = StoryOriginalColorBall.Plays;
            Require(tapped.TapActivate(), "Color Ball tap accepted");
            Require(StoryOriginalColorBall.Plays == plays + 1, "A tap fires the Color Ball");
            Require(StoryOriginalColorBall.LastColour == common, "A tap takes the most common colour");
            yield return Shots("color-ball-tap", 3, .2f);
            yield return Settled();

            // c, d. With a Rocket or TNT: the most common colour turns into that exact piece as each
            //       beam lands, and they all fire together once the ball is gone. The partner is spent.
            foreach (var payload in new[] { ItemsTypes.VERTICAL_STRIPPED, ItemsTypes.PACKAGE })
            {
                string label = payload == ItemsTypes.PACKAGE ? "ball-tnt" : "ball-rocket";
                Item a = Centre();
                Item b = PlainNeighbour(a);
                Require(b != null, label + " has a neighbour for the partner");
                yield return Make(a, ItemsTypes.BOMB, label + "-ball");
                yield return Make(b, payload, label + "-partner");
                yield return Settled();
                common = StoryOriginalColorBall.MostCommonColour();
                int combos = StoryOriginalColorBall.Combos, converted = StoryOriginalColorBall.Converted, fired = StoryOriginalColorBall.Fired;
                Time.timeScale = Slow;
                Swap(a, b);
                yield return Until(() => StoryOriginalColorBall.Combos == combos + 1, 5f, label + " fired the Royal Throne combo");
                Require(StoryOriginalColorBall.LastColour == common, label + " takes the most common colour");
                Require(b == null || b.destroying, label + ": the partner is spent");
                yield return Until(() => StoryOriginalColorBall.Converted > converted, 10f, label + ": beams convert their gems");
                yield return Until(() => FindObjectsByType<Item>().Any(i => !i.destroying && i.currentType == payload), 5f,
                    label + ": converted gems become the partner's exact type");
                if (payload == ItemsTypes.PACKAGE)
                    yield return Until(() => FindObjectsByType<StoryOriginalTntPiece>().Any(p => p.ColorBallReveal), 5f,
                        label + ": converted TNTs reveal with the dedicated clip");
                Require(StoryOriginalColorBall.Fired == fired, label + ": nothing fires before the ball is gone");
                yield return Shots(label, 4, .15f);
                yield return Until(() => !StoryOriginalColorBall.Busy, 20f, label + " finished");
                // They go off together; one caught in another's blast first goes off from that blast.
                Require(StoryOriginalColorBall.Fired > fired, label + ": the converted pieces fired after the ball");
                yield return Until(() => !FindObjectsByType<Item>().Any(i => !i.destroying && i.currentType == payload), 10f,
                    label + ": every converted piece went off (" + (StoryOriginalColorBall.Converted - converted) + " made)");
                yield return Shots(label + "-fire", 2, .15f);
                Time.timeScale = 1f;
                yield return Settled();
            }

            // e. Two Color Balls: the two-ball clip, then every cell on the board at once.
            {
                Item a = Centre();
                Item b = PlainNeighbour(a);
                Require(b != null, "ball-ball has a neighbour for the second ball");
                yield return Make(a, ItemsTypes.BOMB, "ball-ball-a");
                yield return Make(b, ItemsTypes.BOMB, "ball-ball-b");
                yield return Settled();
                int duals = StoryOriginalColorBall.Duals, cleared2 = StoryOriginalColorBall.Cleared;
                Time.timeScale = Slow;
                Swap(a, b);
                yield return Until(() => StoryOriginalColorBall.Duals == duals + 1, 5f, "ball-ball fired the two-ball clip");
                int cells = StoryOriginalColorBall.LastTargets;
                yield return Shots("ball-ball", 4, .25f);
                yield return Until(() => StoryOriginalColorBall.Cleared - cleared2 == cells, 20f, "ball-ball hit every cell at once");
                yield return Shots("ball-ball-blast", 3, .1f);
                Time.timeScale = 1f;
                yield return Settled();
            }
            yield return Until(() => !StoryOriginalColorBall.Busy && StoryOriginalChargedTarget.Active == 0, 10f, "Color Ball sequences finished");
        }

        IEnumerator Combo(ItemsTypes a, ItemsTypes b, string label, Func<int> counter, int expected)
        {
            Item first = Centre();
            Item second = PlainNeighbour(first);
            Require(second != null, label + " has a neighbour for the second piece");
            yield return Make(first, a, label + "-a");
            yield return Make(second, b, label + "-b");
            yield return Shot(label + "-pair");
            int before = counter();
            Time.timeScale = Slow;
            Swap(first, second);
            float waitUntil = Time.realtimeSinceStartup + 12f;
            while (counter() < before + expected && Time.realtimeSinceStartup < waitUntil) yield return null;
            Require(counter() >= before + expected, label + " played the original combo (anticipating=" + StoryOriginalTnt.Anticipating +
                " started=" + StoryOriginalTnt.LastComboStarted.ToString("0.00") + " resolved=" + StoryOriginalTnt.LastComboResolved.ToString("0.00") +
                " now=" + Time.time.ToString("0.00") + " first=" + Describe(first) + " second=" + Describe(second) + ")");
            yield return Shots(label, 5, .1f);
            Time.timeScale = 1f;
            yield return Settled();
            yield return Until(() => StoryOriginalBoosters.ActiveEffects == 0, 10f, label + " effects retired");
        }

        // A Color Ball's conversions stay on the board: fire them so the board is plain again.
        IEnumerator FireConverted(string label)
        {
            var converted = FindObjectsByType<Item>().Where(i => i.square != null && !i.destroying &&
                (i.currentType == ItemsTypes.HORIZONTAL_STRIPPED || i.currentType == ItemsTypes.VERTICAL_STRIPPED ||
                 i.currentType == ItemsTypes.PACKAGE)).ToList();
            Require(converted.Count > 0, label + ": the Color Ball converted gems");
            Require(converted.All(i => i.currentType == ItemsTypes.PACKAGE
                    ? i.GetComponent<StoryOriginalTntPiece>() != null && i.GetComponent<StoryOriginalTntPiece>().Showing
                    : i.GetComponent<StoryOriginalPiece>() != null && i.GetComponent<StoryOriginalPiece>().Showing),
                label + ": every converted gem shows its original piece");
            Require(converted.All(i => i.currentType != ItemsTypes.PACKAGE || i.GetComponent<StoryOriginalTntPiece>().ColorBallReveal),
                label + ": Color Ball TNTs used the dedicated reveal clip");
            yield return Shot(label + "-converted");
            foreach (var item in converted) if (item != null && !item.destroying && item.CanTapActivate()) { item.TapActivate(); break; }
            yield return Shots(label, 2, .12f);
            yield return Settled();
            // Anything still special is cleared through the same tap path.
            for (int pass = 0; pass < 6; pass++)
            {
                var left = FindObjectsByType<Item>().FirstOrDefault(i => i.square != null && !i.destroying &&
                    i.currentType != ItemsTypes.NONE && i.currentType != ItemsTypes.INGREDIENT && i.CanTapActivate());
                if (left == null) break;
                left.TapActivate();
                yield return Settled();
            }
            yield return Until(() => StoryOriginalBoosters.ActiveEffects == 0, 10f, label + " effects retired");
        }

        IEnumerator Make(Item item, ItemsTypes type, string label, bool watchReveal = false)
        {
            item.NextType = type;
            item.ChangeType();
            yield return Until(() => item != null && item.currentType == type, 5f, label + " made");
            if (type == ItemsTypes.PACKAGE)
            {
                yield return Until(() => item.GetComponent<StoryOriginalTntPiece>() != null &&
                    item.GetComponent<StoryOriginalTntPiece>().Showing, 3f, label + " shows the original TNT");
                yield break;
            }
            var piece = item.GetComponent<StoryOriginalPiece>();
            Require(piece != null && piece.Showing, label + " shows the original piece");
            Require(!item.sprRenderer.enabled, label + " hides the kit sprite");
            Require(piece.Key == (type == ItemsTypes.BOMB ? "ColorBall" : "Rocket"), label + " uses the " + piece.Key + " prefab");
            if (watchReveal)
            {
                Require(piece.Revealing, label + " plays the creation clip");
                yield return Shots(label + "-reveal", 3, .08f);
            }
            yield return Until(() => !piece.Revealing, 4f, label + " settles to idle");
            yield return Shot(label + "-idle");
        }

        static bool IsPiece(ItemsTypes t) => t == ItemsTypes.HORIZONTAL_STRIPPED || t == ItemsTypes.VERTICAL_STRIPPED || t == ItemsTypes.BOMB;

        // Item.SwitchDirection reads (where the finger started - where it is now).
        static void Swap(Item from, Item to)
        {
            Square s = from.square;
            Vector3 delta = to.square == s.GetNeighborLeft() ? new Vector3(1, 0, 0) :
                to.square == s.GetNeighborRight() ? new Vector3(-1, 0, 0) :
                to.square == s.GetNeighborBottom() ? new Vector3(0, 1, 0) : new Vector3(0, -1, 0);
            from.switchDirection = Vector3.zero;
            from.SwitchDirection(delta);
        }

        static Item PlainNeighbour(Item item)
        {
            Square s = item.square;
            foreach (var n in new[] { s.GetNeighborRight(), s.GetNeighborLeft(), s.GetNeighborTop(), s.GetNeighborBottom() })
                if (n != null && n.item != null && n.type != SquareTypes.WIREBLOCK && n.item.currentType == ItemsTypes.NONE &&
                    !n.item.destroying && !n.item.falling)
                    return n.item;
            return null;
        }

        static Item Centre()
        {
            var manager = LevelManager.THIS;
            return FindObjectsByType<Item>().Where(i => i.square != null && i.currentType == ItemsTypes.NONE &&
                    !i.destroying && !i.falling && PlainNeighbour(i) != null)
                .OrderBy(i => Mathf.Abs(i.square.row - manager.maxRows / 2) + Mathf.Abs(i.square.col - manager.maxCols / 2))
                .First();
        }

        // Still, refilled, and still again a moment later: a swap on a board that is
        // dropping new gems in can be taken by the cascade instead.
        IEnumerator Settled()
        {
            for (int pass = 0; pass < 2; pass++)
            {
                yield return Until(() => Calm(), 20f, "Board settled");
                yield return new WaitForSeconds(0.5f);
            }
            yield return Until(() => Calm(), 20f, "Board settled");
        }

        static bool Calm()
        {
            var manager = LevelManager.THIS;
            if (manager == null || manager.gameStatus != GameState.Playing || manager.DragBlocked) return false;
            if (FindObjectsByType<Item>().Any(i => i.destroying || i.falling)) return false;
            for (int r = 0; r < manager.maxRows; r++)
                for (int c = 0; c < manager.maxCols; c++)
                {
                    var sq = manager.GetSquare(c, r);
                    if (sq != null && !sq.IsNone() && sq.type == SquareTypes.EMPTY && sq.item == null) return false;
                }
            return true;
        }

        static string Describe(Item item) => item == null ? "gone" :
            item.currentType + (item.destroying ? " destroying" : "") + (item.falling ? " falling" : "") +
            (item.square != null ? " at " + item.square.col + "," + item.square.row : "");

        void OnDestroy()
        {
            Time.timeScale = savedTimeScale;
            StoryOriginalBoosters.Enabled = savedBoosters;
            StoryOriginalTnt.Enabled = savedTnt;
            StoryBombVfx.Enabled = savedBomb;
        }
    }
}
#endif
