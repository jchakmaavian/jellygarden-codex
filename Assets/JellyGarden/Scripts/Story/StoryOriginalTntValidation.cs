#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using RoyalThrone.ReferenceGameplay.Preview;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JellyGarden.Scripts.Story.Editor
{
    /// <summary>
    /// Plays the original Royal Throne TNT on a real King Leo level through the
    /// shipping Item paths: a TNT made by a match (DestroyCor → GenItem → Start)
    /// and one made by ChangeType, its reveal and idle, a tap, the blast, pause
    /// and cleanup. Frames land in Temp/original-tnt-validation.
    /// </summary>
    public sealed class StoryOriginalTntValidation : MonoBehaviour
    {
        const string Output = "Temp/original-tnt-validation";
        int checks, frame;
        float savedTimeScale;
        bool savedOriginal, savedBomb;

        [MenuItem("JellyGarden/Story/Preview/Validate Original TNT")]
        public static void Run()
        {
            if (!Application.isPlaying) throw new InvalidOperationException("Enter Play Mode before validating the TNT.");
            if (FindAnyObjectByType<StoryOriginalTntValidation>() != null) return;
            Directory.CreateDirectory(Output);
            foreach (var old in Directory.GetFiles(Output, "*.png")) File.Delete(old);
            var go = new GameObject("Original TNT validation");
            DontDestroyOnLoad(go);
            go.AddComponent<StoryOriginalTntValidation>().Begin();
        }

        void Begin()
        {
            savedTimeScale = Time.timeScale;
            savedOriginal = StoryOriginalTnt.Enabled;
            savedBomb = StoryBombVfx.Enabled;
            File.WriteAllText(Output + "/progress.txt", "Starting original TNT validation\n");
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
            File.WriteAllText(Output + "/result.txt", "PASS " + checks + " checks: match-made and ChangeType TNT pieces, reveal, idle, tap, 0.12s ignition, blast wave, TNT+TNT anticipation, pause and cleanup.");
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

        IEnumerator Validate()
        {
            StoryOriginalTnt.Enabled = true;
            StoryBombVfx.Enabled = true;
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

            // 1. A TNT made by a match: the matched gem carries NextType, DestroyCor
            //    hands it to a fresh item through GenItem, and Start consumes it.
            Item matched = Centre();
            Square cell = matched.square;
            matched.NextType = ItemsTypes.PACKAGE;
            Time.timeScale = .25f;
            matched.DestroyItem(true);
            Item made = null;
            yield return Until(() => (made = cell.item) != null && made != matched && made.currentType == ItemsTypes.PACKAGE,
                5f, "Match-made TNT reached its square");
            yield return Until(() => Piece(made) != null && Piece(made).Showing, 3f, "Match-made TNT shows the original piece");
            Require(!made.sprRenderer.enabled, "Match-made TNT hides the kit sprite");
            Require(Piece(made).Revealing, "Match-made TNT plays the creation clip");
            for (int i = 0; i < 4; i++) { yield return new WaitForSeconds(.08f); yield return Shot("match-reveal"); }
            Time.timeScale = 1f;
            yield return Until(() => !Piece(made).Revealing, 3f, "Match-made TNT settles to idle");
            yield return Shot("match-idle");
            yield return Settled();

            // 2. That TNT tapped: original blast, its pause, and cleanup.
            yield return Until(() => made != null && made.CanTapActivate(), 5f, "TNT can activate");
            int moves = manager.Limit;
            Time.timeScale = .25f;
            var ring = LevelManager.THIS.GetItemsAround(made.square)
                .Where(i => i != null && i != made && i.currentType == ItemsTypes.NONE && !i.destroying).ToList();
            int ignitions = StoryOriginalTnt.Ignitions;
            Require(made.TapActivate(), "TNT tap accepted");
            float tapped = Time.time;
            Require(manager.Limit == moves - 1, "TNT charged one move");
            // RoyalPowerupDirector.Explosion: the fuse burns 0.12s, piece still up, before the blast.
            Require(StoryOriginalTnt.Ignitions == ignitions + 1, "Tap lit the original fuse");
            Require(StoryOriginalTnt.ActiveBlasts == 0, "No blast while the fuse burns");
            Require(Piece(made) != null && Piece(made).Showing && Piece(made).Igniting, "The piece stays up while its fuse burns");
            yield return Shot("ignition");
            yield return Until(() => StoryOriginalTnt.ActiveBlasts == 1, 3f, "The original blast follows the fuse");
            float fused = Time.time - tapped;
            Require(fused >= ReferenceTnt.IgnitionDuration - .02f && fused <= ReferenceTnt.IgnitionDuration + .1f,
                "The fuse burned 0.12s before the blast (" + fused.ToString("0.000") + "s)");
            if (StoryOriginalMotion.Ready && ring.Count > 0)
            {
                // The wave: the cells next to the TNT shatter 0.012 + 0.025 + 0.018s after the blast.
                Require(ring.All(i => i != null && i.sprRenderer.enabled), "The next ring is still whole at the blast");
                float blasted = Time.time;
                yield return Until(() => ring.All(i => i == null || !i.sprRenderer.enabled), 3f, "The next ring shattered");
                float reached = Time.time - blasted;
                Require(reached >= .05f, "The wave reached the next ring after 0.055s (" + reached.ToString("0.000") + "s)");
            }
            for (int i = 0; i < 4; i++) { yield return new WaitForSeconds(.06f); yield return Shot("blast"); }
            var blast = FindObjectsByType<ParticleSystem>().First(p => p.transform.root.name == "~OriginalTnt");
            manager.gameStatus = GameState.Pause;
            float age = blast.time;
            yield return new WaitForSecondsRealtime(.25f);
            Require(Mathf.Approximately(age, blast.time), "Blast holds while the game is paused");
            manager.gameStatus = GameState.Playing;
            Time.timeScale = 1f;
            yield return Settled();
            yield return Until(() => StoryOriginalTnt.ActiveBlasts == 0, 6f, "Blast retired");

            // 3. A TNT made by ChangeType (debug and pre-level boosters take this path).
            Item changed = Centre();
            changed.NextType = ItemsTypes.PACKAGE;
            changed.ChangeType();
            yield return Until(() => Piece(changed) != null && Piece(changed).Showing, 5f, "ChangeType TNT shows the original piece");
            yield return Until(() => !Piece(changed).Revealing, 3f, "ChangeType TNT settles to idle");
            yield return Shot("changetype-idle");
            yield return Until(() => changed.CanTapActivate(), 5f, "Second TNT can activate");
            Require(changed.TapActivate(), "Second TNT tap accepted");
            yield return Settled();
            yield return Until(() => StoryOriginalTnt.ActiveBlasts == 0, 6f, "Second blast retired");
            Require(!FindObjectsByType<StoryOriginalTntPiece>().Any(p => p.Showing && (p.GetComponent<Item>().currentType != ItemsTypes.PACKAGE)),
                "No original piece left on a non-TNT item");

            // 4. Two TNTs swapped together: the dedicated body/roof/fuse clip first, the board
            //    held, then the big blast (NativeTntComboAnticipation).
            Item first = Centre();
            Item second = PlainNeighbour(first);
            Require(second != null, "TNT+TNT has a neighbour for the second TNT");
            foreach (var tnt in new[] { first, second })
            {
                tnt.NextType = ItemsTypes.PACKAGE;
                tnt.ChangeType();
            }
            Func<bool> made2 = () => first != null && second != null &&
                first.currentType == ItemsTypes.PACKAGE && second.currentType == ItemsTypes.PACKAGE &&
                Piece(first) != null && !Piece(first).Revealing && Piece(second) != null && !Piece(second).Revealing;
            float makeUntil = Time.realtimeSinceStartup + 6f;
            while (!made2() && Time.realtimeSinceStartup < makeUntil) yield return null;
            Require(made2(), "Two TNTs made (" + Making(first) + " / " + Making(second) + ")");
            Require(first.currentType == ItemsTypes.PACKAGE && second.currentType == ItemsTypes.PACKAGE, "Both TNTs are on the board (" + ComboState(first, second) + ")");
            yield return Settled();
            int anticipations = StoryOriginalTnt.ComboAnticipations, combos = StoryOriginalBoosters.TntTntPlays;
            ignitions = StoryOriginalTnt.Ignitions;
            Swap(first, second);
            yield return Until(() => StoryOriginalTnt.ComboAnticipations == anticipations + 1, 3f, "TNT+TNT plays the original anticipation");
            float began = Time.time;
            Require(StoryOriginalTnt.Anticipating, "The board is held during the anticipation");
            Require(StoryOriginalTnt.LastComboLength > 1.5f, "The anticipation clip runs its full 1.58s");
            Require(FindObjectsByType<Transform>().Any(t => t.name.StartsWith(ReferenceTnt.ComboKey)), "The combo body stands in for the two TNTs");
            yield return null;
            Require(!Piece(first).Showing && !Piece(second).Showing, "The two TNT pieces are hidden under the combo body");
            Require(StoryOriginalBoosters.TntTntPlays == combos, "No blast during the anticipation");
            for (int i = 0; i < 4; i++) { yield return new WaitForSeconds(.35f); yield return Shot("tnt-tnt-anticipation"); }
            float waitUntil = Time.realtimeSinceStartup + 4f;
            while (StoryOriginalBoosters.TntTntPlays != combos + 1 && Time.realtimeSinceStartup < waitUntil) yield return null;
            Require(StoryOriginalBoosters.TntTntPlays == combos + 1, "The big blast follows the anticipation (" + ComboState(first, second) + ")");
            float held = Time.time - began;
            Require(held >= StoryOriginalTnt.LastComboLength - .05f, "The blast waited for the whole clip (" + held.ToString("0.00") + "s)");
            float clip = StoryOriginalTnt.LastComboResolved - StoryOriginalTnt.LastComboStarted;
            Require(Mathf.Abs(clip - StoryOriginalTnt.LastComboLength) < .1f,
                "The clear ran when the clip ended (" + clip.ToString("0.000") + "s of " + StoryOriginalTnt.LastComboLength.ToString("0.000") + "s)");
            Require(!StoryOriginalTnt.Anticipating, "The board is released after the clear starts");
            Require(StoryOriginalTnt.Ignitions == ignitions,
                "The combo's own TNTs light no fuse");
            for (int i = 0; i < 3; i++) { yield return new WaitForSeconds(.08f); yield return Shot("tnt-tnt-blast"); }
            yield return Settled();
            yield return Until(() => StoryOriginalTnt.ActiveBlasts == 0, 6f, "TNT+TNT blast retired");

            StoryRoyalVfx.StateChanged(GameState.PrepareGame);
            Require(StoryOriginalTnt.ActiveBlasts == 0, "Level reset leaves no blast");
        }

        static StoryOriginalTntPiece Piece(Item item) => item != null ? item.GetComponent<StoryOriginalTntPiece>() : null;

        static string ComboState(Item a, Item b) =>
            "anticipating=" + StoryOriginalTnt.Anticipating + " started=" + StoryOriginalTnt.LastComboStarted.ToString("0.00") +
            " resolved=" + StoryOriginalTnt.LastComboResolved.ToString("0.00") + " now=" + Time.time.ToString("0.00") +
            " a=" + Describe(a) + " b=" + Describe(b) + " state=" + LevelManager.THIS.gameStatus;

        // Where a ChangeType stands: type, next type, piece, and the kit Animator's state (ChangeTypeCor waits on "appear").
        static string Making(Item item)
        {
            if (item == null) return "gone";
            var piece = Piece(item);
            var anim = item.GetComponentInChildren<Animator>();
            string state = anim == null ? "no animator" : !anim.enabled ? "animator off" :
                "anim " + anim.GetCurrentAnimatorStateInfo(0).shortNameHash + " speed " + anim.speed;
            return Describe(item) + " next=" + item.NextType + " piece=" + (piece == null ? "none" : piece.Showing + "/" + piece.Revealing) + " " + state;
        }

        static string Describe(Item item) => item == null ? "gone" :
            item.currentType + (item.destroying ? " destroying" : "") + (item.falling ? " falling" : "") +
            (item.square != null ? " at " + item.square.col + "," + item.square.row : "");

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
                    !i.destroying && !i.falling)
                .OrderBy(i => Mathf.Abs(i.square.row - manager.maxRows / 2) + Mathf.Abs(i.square.col - manager.maxCols / 2))
                .First();
        }

        // Still, refilled, and still again a moment later: gems picked from a board that is
        // still dropping refills in can be taken by the cascade.
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

        void OnDestroy()
        {
            Time.timeScale = savedTimeScale;
            StoryOriginalTnt.Enabled = savedOriginal;
            StoryBombVfx.Enabled = savedBomb;
        }
    }
}
#endif
