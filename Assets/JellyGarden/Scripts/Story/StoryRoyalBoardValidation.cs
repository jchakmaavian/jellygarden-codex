#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JellyGarden.Scripts.GUI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace JellyGarden.Scripts.Story.Editor
{
    // Explicit editor review. Inventory is restored even when a check fails.
    public sealed class StoryRoyalBoardValidation : MonoBehaviour
    {
        const string Output = "art-review/gameplay-redesign-v1/implementation";
        readonly Dictionary<string, int?> saved = new Dictionary<string, int?>();
        Action<int, int> resize;
        int checks;
        string savedDirectory;
        public static void Run(Action<int, int> resize)
        {
            if (!Application.isPlaying) throw new InvalidOperationException("Enter Play Mode first.");
            if (FindAnyObjectByType<StoryRoyalBoardValidation>() != null) return;
            var host = new GameObject("Royal board validation");
            DontDestroyOnLoad(host);
            var review = host.AddComponent<StoryRoyalBoardValidation>();
            review.resize = resize;
            review.StartCoroutine(review.Guard(review.Validate()));
        }
        IEnumerator Guard(IEnumerator routine)
        {
            Directory.CreateDirectory(Output);
            File.Delete(Output + "/result.txt");
            File.WriteAllText(Output + "/progress.txt", "Starting royal board review\n");
            savedDirectory = StoryProgress.SaveDirectoryOverride;
            StoryProgress.SaveDirectoryOverride = "Temp/royal-board-isolated-save";
            foreach (string key in new[] { "OpenLevel", "OpenLevelTest", "ExtraMoves", "ExtraTime", "Hand", "Bomb", "Random_color" })
                saved[key] = PlayerPrefs.HasKey(key) ? PlayerPrefs.GetInt(key) : (int?)null;
            var stack = new Stack<IEnumerator>();
            stack.Push(routine);
            while (stack.Count > 0)
            {
                object value = null;
                Exception error = null;
                try { if (!stack.Peek().MoveNext()) stack.Pop(); else value = stack.Peek().Current; }
                catch (Exception e) { error = e; }
                if (error != null)
                {
                    File.WriteAllText(Output + "/result.txt", "FAIL after " + checks + " checks\n" + error);
                    Debug.LogException(error);
                    Destroy(gameObject);
                    yield break;
                }
                if (value is IEnumerator nested) stack.Push(nested); else yield return value;
            }
            File.WriteAllText(Output + "/result.txt", "PASS " + checks + " checks: live sprite mapping, shaped frame, targets, moves, booster callbacks, pause and three viewport sizes.\n");
            Destroy(gameObject);
        }
        void OnDestroy()
        {
            Time.timeScale = 1;
            StoryProgress.SaveDirectoryOverride = savedDirectory;
            foreach (var pair in saved)
                if (pair.Value.HasValue) PlayerPrefs.SetInt(pair.Key, pair.Value.Value); else PlayerPrefs.DeleteKey(pair.Key);
            PlayerPrefs.Save();
            resize?.Invoke(393, 852);
        }
        void Check(bool condition, string description)
        {
            if (!condition) throw new Exception(description);
            checks++;
            File.AppendAllText(Output + "/progress.txt", description + "\n");
        }
        IEnumerator Settled()
        {
            float deadline = Time.realtimeSinceStartup + 25;
            while (Time.realtimeSinceStartup < deadline && (LevelManager.THIS == null ||
                LevelManager.THIS.gameStatus != GameState.Playing || LevelManager.THIS.DragBlocked ||
                FindObjectsByType<Item>(FindObjectsSortMode.None).Any(i => i.falling || i.destroying))) yield return null;
            Check(LevelManager.THIS != null && LevelManager.THIS.gameStatus == GameState.Playing && !LevelManager.THIS.DragBlocked, "Board settled");
            yield return new WaitForSecondsRealtime(1);
        }
        IEnumerator Shot(string name)
        {
            StoryLevelHud.Instance.Refresh();
            yield return new WaitForSecondsRealtime(.4f);
            yield return new WaitForEndOfFrame();
            StoryWindow.Capture(Output + "/" + name + ".png");
        }
        Button Button(string name) => GameObject.Find("RoyalBoardHud").GetComponentsInChildren<Button>().First(b => b.name == name);
        IEnumerator Validate()
        {
            Time.timeScale = 1;
            SceneManager.LoadScene("game");
            yield return new WaitForSecondsRealtime(2);
            foreach (string key in new[] { "ExtraMoves", "ExtraTime", "Hand", "Bomb", "Random_color" }) PlayerPrefs.SetInt(key, 3);
            foreach (int level in new[] { 3, 4, 17, 12 })
            {
                StoryFlow.LaunchLevel(level, 1);
                yield return Settled();
                var manager = LevelManager.THIS;
                Check(StoryLevelHud.Instance.VisibleTargetCount == manager.targetObject.Length, "All targets visible on level " + level);
                Check(FindAnyObjectByType<StoryRoyalBoardFrame>().ContourCount > 0, "Rounded shaped frame on level " + level);
                Check(FindObjectsByType<Item>(FindObjectsSortMode.None).Where(i => i.currentType == ItemsTypes.NONE)
                    .All(i => i.sprRenderer.sprite == StorybookBoardArt.Piece(i.color)), "Colour mapping on level " + level);
                yield return Shot("level-" + level + "-393x852");
                if (level == 17)
                {
                    var pieces = FindObjectsByType<Item>(FindObjectsSortMode.None).Where(i => i.currentType == ItemsTypes.NONE).Take(4).ToArray();
                    var kinds = new[] { ItemsTypes.HORIZONTAL_STRIPPED, ItemsTypes.VERTICAL_STRIPPED, ItemsTypes.PACKAGE, ItemsTypes.BOMB };
                    for (int i = 0; i < pieces.Length; i++) { pieces[i].NextType = kinds[i]; pieces[i].ChangeType(); }
                    yield return new WaitForSecondsRealtime(1);
                    yield return Shot("powerups-393x852");
                    foreach (var size in new[] { new Vector2Int(320, 568), new Vector2Int(768, 1024) })
                    {
                        resize(size.x, size.y);
                        yield return new WaitForSecondsRealtime(1);
                        Check(Screen.width == size.x && Screen.height == size.y, "Viewport " + size);
                        yield return Shot("powerups-" + size.x + "x" + size.y);
                    }
                    resize(393, 852);
                }
                if (level != 12) continue;
                int moves = manager.Limit;
                Button("Booster0").onClick.Invoke();
                Check(manager.Limit == moves + 5 && PlayerPrefs.GetInt("ExtraMoves") == 2, "Extra moves adds five and spends one");
                var types = new[] { BoostType.Hand, BoostType.Bomb, BoostType.Random_color };
                for (int i = 1; i < 4; i++)
                {
                    Button("Booster" + i).onClick.Invoke();
                    Check(manager.activatedBoost != null && manager.activatedBoost.type == types[i - 1], "Booster selects " + types[i - 1]);
                    Button("Booster" + i).onClick.Invoke();
                    Check(manager.activatedBoost == null && PlayerPrefs.GetInt(types[i - 1].ToString()) == 3, "Booster cancels without spending");
                }
                Button("Pause").onClick.Invoke();
                float pauseDeadline = Time.realtimeSinceStartup + 3;
                while (manager.gameStatus != GameState.Pause && Time.realtimeSinceStartup < pauseDeadline) yield return null;
                Check(manager.gameStatus == GameState.Pause, "Pause callback opens pause");
                yield return Shot("pause-393x852");
                manager.gameStatus = GameState.Playing;
                Time.timeScale = 1;
            }
        }
    }
}
#endif
