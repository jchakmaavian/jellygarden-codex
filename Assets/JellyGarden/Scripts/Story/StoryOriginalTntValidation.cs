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
            File.WriteAllText(Output + "/result.txt", "PASS " + checks + " checks: match-made and ChangeType TNT pieces, reveal, idle, tap, blast, pause and cleanup.");
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
            Require(made.TapActivate(), "TNT tap accepted");
            Require(manager.Limit == moves - 1, "TNT charged one move");
            Require(StoryOriginalTnt.ActiveBlasts == 1, "Tap played the original blast");
            for (int i = 0; i < 5; i++) { yield return new WaitForSeconds(.06f); yield return Shot("blast"); }
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

            StoryRoyalVfx.StateChanged(GameState.PrepareGame);
            Require(StoryOriginalTnt.ActiveBlasts == 0, "Level reset leaves no blast");
        }

        static StoryOriginalTntPiece Piece(Item item) => item != null ? item.GetComponent<StoryOriginalTntPiece>() : null;

        static Item Centre()
        {
            var manager = LevelManager.THIS;
            return FindObjectsByType<Item>().Where(i => i.square != null && i.currentType == ItemsTypes.NONE &&
                    !i.destroying && !i.falling)
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
            StoryOriginalTnt.Enabled = savedOriginal;
            StoryBombVfx.Enabled = savedBomb;
        }
    }
}
#endif
