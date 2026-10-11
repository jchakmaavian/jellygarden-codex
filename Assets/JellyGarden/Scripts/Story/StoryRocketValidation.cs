#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using RoyalRocketVFX;
using RoyalTNTVFX;

namespace JellyGarden.Scripts.Story.Editor
{
    /// <summary>Exercises the shipping Item destroy path, including refill after chained hits.</summary>
    public sealed class StoryRocketValidation : MonoBehaviour
    {
        const string Output = "Temp/royal-vfx-validation";
        int checks;
        float savedTimeScale;
        RocketVariant savedVariant;
        bool savedEnabled, savedOriginalTnt, savedOriginalBoosters;

        [MenuItem("JellyGarden/Story/Preview/Validate Rocket Animation")]
        public static void Run()
        {
            if (!Application.isPlaying) throw new InvalidOperationException("Enter Play Mode before validating rockets.");
            if (FindAnyObjectByType<StoryRocketValidation>() != null) return;
            Directory.CreateDirectory(Output);
            var go = new GameObject("Rocket validation");
            DontDestroyOnLoad(go);
            go.AddComponent<StoryRocketValidation>().StartCoroutineGuarded();
        }

        void StartCoroutineGuarded()
        {
            savedTimeScale = Time.timeScale;
            savedVariant = StoryRocketVariant.Current;
            savedEnabled = StoryRocketVfx.Enabled;
            savedOriginalTnt = StoryOriginalTnt.Enabled;
            savedOriginalBoosters = StoryOriginalBoosters.Enabled;
            File.WriteAllText(Output + "/progress.txt", "Starting ZIP validation\n");
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
                    Time.timeScale = savedTimeScale;
                    Destroy(gameObject);
                    yield break;
                }
                if (value is IEnumerator nested) stack.Push(nested);
                else yield return value;
            }
            Time.timeScale = savedTimeScale;
            File.WriteAllText(Output + "/result.txt", "PASS " + checks + " checks: ZIP players, contacts, both axes, edge, pause, chain, TNT, cleanup and refill.");
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

        IEnumerator Validate()
        {
            StoryRocketVfx.Enabled = true;
            // This harness asserts on the RoyalTNTVFX player; the original TNT
            // replaces it when on. OnDestroy restores the switch.
            StoryOriginalTnt.Enabled = false;
            // Likewise the RoyalRocketVFX player and its timing, which the original
            // Rocket replaces (StoryOriginalBoosters).
            StoryOriginalBoosters.Enabled = false;
            StoryRocketVariant.Current = RocketVariant.RoyalStreak;
            SceneManager.LoadScene("game");
            yield return Until(() => LevelManager.THIS != null, 20f, "No game board manager");
            yield return new WaitForSecondsRealtime(1f);
            StoryFlow.LaunchLevel(3, 1);
            yield return Settled();
            // Prevent test clears from ending the level or spending player progression.
            LevelManager.THIS.Limit = 100;
            LevelManager.THIS.star1 = 1000000;
            LevelManager.THIS.star2 = 2000000;
            LevelManager.THIS.star3 = 3000000;
            Require(StoryRoyalVfx.RocketReady && StoryRoyalVfx.TntReady, "ZIP players were not prewarmed");
            Require(RocketVFX.Player.transform.childCount == 1, "Rocket prefab has a duplicate runtime pool");
            Require(RocketVFX.Player.Tuning.poolSize == 32, "Rocket pool is too small for chain clears");
            Require(Shader.Find("RoyalRocketVFX/RocketFire").isSupported, "Rocket shader unsupported");
            Require(Shader.Find("RoyalTNTVFX/TNTGlow").isSupported, "TNT shader unsupported");
            yield return new WaitForSeconds(3f);
            foreach (var goal in LevelManager.THIS.targetObject) goal.targetCount = 999;

            yield return Shot(true, false, false, "horizontal");
            yield return Shot(false, false, false, "vertical");
            yield return Shot(true, true, false, "edge");
            yield return Shot(true, false, true, "chain");
            yield return Shot(true, false, true, "rocket-tnt");
            yield return Bomb();
            StoryRoyalVfx.FireRocket(Vector3.zero, true, 4, 4, StoryBoardScale.Pitch);
            StoryRoyalVfx.Blast(Vector3.zero, 1);
            Require(RocketVFX.Player.ActiveRockets == 2 && TNTExplosionVFX.Player.ActiveExplosions == 1,
                "ZIP pools could not be reused");
            StoryRoyalVfx.StateChanged(GameState.PrepareGame);
            Require(!RocketVFX.IsBusy && !TNTExplosionVFX.IsBusy, "Level reset left active effects");
            Require(FindAnyObjectByType<StoryRocketFlight>() == null, "Procedural fallback also ran");
        }

        IEnumerator Settled()
        {
            yield return Until(() => LevelManager.THIS != null && LevelManager.THIS.gameStatus == GameState.Playing &&
                !LevelManager.THIS.DragBlocked && !FindObjectsByType<Item>().Any(i => i.destroying || i.falling),
                20f, "Board did not finish its rocket/cascade");
            yield return new WaitForSeconds(0.15f);
        }

        IEnumerator Shot(bool horizontal, bool edge, bool chain, string label)
        {
            var manager = LevelManager.THIS;
            Item[] available = FindObjectsByType<Item>()
                .Where(i => i.square != null && i.currentType == ItemsTypes.NONE && !i.destroying && !i.falling).ToArray();
            Item origin = available.OrderBy(i => edge ? i.square.col :
                Mathf.Abs(i.square.col - manager.maxCols / 2) + Mathf.Abs(i.square.row - manager.maxRows / 2)).First();
            int col = origin.square.col, row = origin.square.row;
            Item target = available.Where(i => i != origin && (horizontal ? i.square.row == row : i.square.col == col))
                .OrderByDescending(i => horizontal ? Mathf.Abs(i.square.col - col) : Mathf.Abs(i.square.row - row)).First();
            int distance = horizontal ? Mathf.Abs(target.square.col - col) : Mathf.Abs(target.square.row - row);
            origin.NextType = horizontal ? ItemsTypes.HORIZONTAL_STRIPPED : ItemsTypes.VERTICAL_STRIPPED;
            origin.ChangeType();
            if (chain)
            {
                target.NextType = label == "rocket-tnt" ? ItemsTypes.PACKAGE :
                    horizontal ? ItemsTypes.VERTICAL_STRIPPED : ItemsTypes.HORIZONTAL_STRIPPED;
                target.ChangeType();
            }
            yield return Until(() => origin != null && origin.CanTapActivate(), 5f, label + " rocket cannot activate");
            Time.timeScale = 0.25f;
            float start = Time.time;
            float packageContact = -1f, clearedAt = -1f;
            Vector2 targetAt = target.square.transform.position;
            Action<Vector3, int> contact = (position, index) =>
            {
                if (packageContact < 0f && Vector2.Distance(position, targetAt) < 0.1f)
                    packageContact = Time.time - start;
            };
            RocketVFX.Player.CellReached += contact;
            int moves = manager.Limit;
            Require(origin.TapActivate(), label + " tap rejected");
            Require(RocketVFX.Player.ActiveRockets >= 2, label + " did not launch ZIP rocket pair");
            Require(manager.Limit == moves - 1, label + " charged the wrong number of moves");
            Require(target != null && target.destroying && target.sprRenderer.enabled,
                label + " did not reserve the still-visible target");
            Require(!target.animationFinished, label + " released refill before impact");

            if (label == "horizontal")
            {
                manager.gameStatus = GameState.Pause;
                float pausedAt = Time.realtimeSinceStartup;
                while (Time.realtimeSinceStartup - pausedAt < 0.2f) yield return null;
                Require(target != null && target.sprRenderer.enabled, "Rocket cleared a tile while paused");
                Require(RocketVFX.Player.Paused && TNTExplosionVFX.Player.Paused, "ZIP players did not pause");
                manager.gameStatus = GameState.Playing;
                Time.timeScale = 0.25f;
                start = Time.time;
            }

            float[] frames = { 0.04f, 0.13f, 0.21f, 0.30f, 0.46f };
            int frame = 0;
            bool checkedImpact = false;
            float expectedHit = StoryRocketVfx.ReachesAt(distance);
            while (Time.time - start < 0.65f)
            {
                float elapsed = Time.time - start;
                if (!checkedImpact && (target == null || !target.sprRenderer.enabled))
                {
                    Require(elapsed >= expectedHit - 0.04f, label + " target vanished ahead of the head: " + elapsed);
                    Require(elapsed <= expectedHit + 0.12f, label + " target remained after the head: " + elapsed);
                    checkedImpact = true;
                    clearedAt = elapsed;
                }
                if (frame < frames.Length && elapsed >= frames[frame])
                {
                    yield return new WaitForEndOfFrame();
                    StoryWindow.Capture(Output + "/" + label + "-" + frame + ".png");
                    frame++;
                }
                yield return null;
            }
            RocketVFX.Player.CellReached -= contact;
            Require(checkedImpact, label + " target never cleared");
            Require(packageContact >= 0f, label + " ZIP did not reach the target cell");
            Require(Mathf.Abs(clearedAt - packageContact) < 0.05f, label + " clear disagrees with ZIP contact event: " + clearedAt + " / " + packageContact);
            if (label == "rocket-tnt")
                Require(TNTExplosionVFX.Player.ActiveExplosions > 0, "Rocket hit did not trigger ZIP TNT");
            Time.timeScale = 1f;
            yield return Settled();
            Require(manager.gameStatus == GameState.Playing, label + " left the board unplayable");
            File.AppendAllText(Output + "/progress.txt", label + " passed\n");
        }

        IEnumerator Bomb()
        {
            // Refill can finish while a prior TNT's final sparks are still alive.
            // Isolate this single-bomb assertion and its preview from that tail.
            yield return Until(() => !TNTExplosionVFX.IsBusy && !RocketVFX.IsBusy, 5f, "Previous effects did not retire");
            var manager = LevelManager.THIS;
            Item bomb = FindObjectsByType<Item>().Where(i => i.square != null && i.currentType == ItemsTypes.NONE &&
                !i.destroying && !i.falling).OrderBy(i => Mathf.Abs(i.square.row - manager.maxRows / 2) +
                    Mathf.Abs(i.square.col - manager.maxCols / 2)).First();
            bomb.NextType = ItemsTypes.PACKAGE;
            bomb.ChangeType();
            yield return Until(() => bomb != null && bomb.CanTapActivate(), 5f, "Bomb cannot activate");
            int moves = manager.Limit;
            Time.timeScale = 0.25f;
            Require(bomb.TapActivate(), "Bomb tap rejected");
            Require(manager.Limit == moves - 1, "Bomb charged the wrong number of moves");
            Require(TNTExplosionVFX.Player.ActiveExplosions == 1, "Bomb did not launch ZIP TNT");
            yield return new WaitForSeconds(0.05f);
            var actor = TNTExplosionVFX.Player.GetComponentsInChildren<TNTExplosionActor>()
                .First(a => a.State != TNTExplosionActor.VisualState.Idle);
            manager.gameStatus = GameState.Pause;
            float age = actor.VisualAge;
            yield return new WaitForSecondsRealtime(0.2f);
            Require(TNTExplosionVFX.Player.Paused && Mathf.Approximately(age, actor.VisualAge), "TNT advanced while paused");
            manager.gameStatus = GameState.Playing;
            Time.timeScale = 0.25f;
            for (int frame = 0; frame < 5; frame++)
            {
                yield return new WaitForSeconds(0.08f);
                yield return new WaitForEndOfFrame();
                StoryWindow.Capture(Output + "/tnt-" + frame + ".png");
            }
            Require(TNTExplosionVFX.Player.VisibleRenderers > 0, "TNT has no visible renderers");
            Time.timeScale = 1f;
            yield return Settled();
            yield return Until(() => !TNTExplosionVFX.IsBusy && !RocketVFX.IsBusy, 5f, "ZIP pools never retired");
            File.AppendAllText(Output + "/progress.txt", "tnt passed\n");
        }

        void OnDestroy()
        {
            Time.timeScale = savedTimeScale;
            StoryRocketVariant.Current = savedVariant;
            StoryRocketVfx.Enabled = savedEnabled;
            StoryOriginalTnt.Enabled = savedOriginalTnt;
            StoryOriginalBoosters.Enabled = savedOriginalBoosters;
        }
    }
}
#endif
