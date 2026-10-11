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
            var stack = new System.Collections.Generic.Stack<IEnumerator>();
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
            File.WriteAllText(Output + "/result.txt", "PASS " + checks + " checks: rockets, color ball, five combos, match break, pause, moves-left flight and win stars.");
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
            foreach (var goal in manager.targetObject) goal.targetCount = 999;
            yield return Settled();
            Require(StoryOriginalBoosters.OwnsRocket, "The board's rockets fly as the original");

            // 1. Horizontal Rocket: original piece, creation clip, idle, tap, flight and timing.
            yield return RocketTapped(ItemsTypes.HORIZONTAL_STRIPPED, "rocket-h");
            // 2. Vertical Rocket, paused mid-flight.
            yield return RocketTapped(ItemsTypes.VERTICAL_STRIPPED, "rocket-v");

            // 3. Color Ball swapped with a plain gem: charge, beams, final pulse.
            Item ball = Centre();
            yield return Make(ball, ItemsTypes.BOMB, "color-ball");
            Item gem = PlainNeighbour(ball);
            Require(gem != null, "Color Ball has a plain neighbour");
            int balls = StoryOriginalBoosters.ColorBallPlays, rays = StoryOriginalBoosters.Rays;
            Time.timeScale = Slow;
            Swap(ball, gem);
            yield return Until(() => StoryOriginalBoosters.ColorBallPlays == balls + 1, 5f, "Swap fired the original Color Ball");
            yield return Shots("color-ball-fire", 6, .12f);
            Require(StoryOriginalBoosters.Rays > rays, "Color Ball fired a beam per target");
            Time.timeScale = 1f;
            yield return Settled();
            yield return Until(() => StoryOriginalBoosters.ActiveEffects == 0, 8f, "Color Ball effects retired");

            // 4-8. Combos: two specials side by side, swapped together.
            yield return Combo(ItemsTypes.HORIZONTAL_STRIPPED, ItemsTypes.VERTICAL_STRIPPED, "rocket-rocket",
                () => StoryOriginalBoosters.RocketFlights, 2);
            yield return Combo(ItemsTypes.HORIZONTAL_STRIPPED, ItemsTypes.PACKAGE, "rocket-tnt",
                () => StoryOriginalBoosters.RocketTntPlays, 1);
            yield return Combo(ItemsTypes.PACKAGE, ItemsTypes.PACKAGE, "tnt-tnt",
                () => StoryOriginalBoosters.TntTntPlays, 1);
            yield return Combo(ItemsTypes.BOMB, ItemsTypes.HORIZONTAL_STRIPPED, "ball-rocket",
                () => StoryOriginalBoosters.ColorBallConverts, 1);
            yield return FireConverted("ball-rocket-fire");
            yield return Combo(ItemsTypes.BOMB, ItemsTypes.PACKAGE, "ball-tnt",
                () => StoryOriginalBoosters.ColorBallConverts, 1);
            yield return FireConverted("ball-tnt-fire");
            yield return Combo(ItemsTypes.BOMB, ItemsTypes.BOMB, "ball-ball",
                () => StoryOriginalBoosters.DualColorBallPlays, 1);

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
            Require(Math.Abs(StoryRoyalVfx.RocketReachesAt(3) - StoryOriginalBoosters.RocketReachesAt(3)) < 1e-5f,
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
            yield return Until(() => counter() >= before + expected, 5f, label + " played the original combo");
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

        IEnumerator Settled()
        {
            yield return Until(() => LevelManager.THIS != null && LevelManager.THIS.gameStatus == GameState.Playing &&
                !LevelManager.THIS.DragBlocked && !FindObjectsByType<Item>().Any(i => i.destroying || i.falling),
                20f, "Board settled");
            yield return new WaitForSeconds(0.15f);
        }

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
