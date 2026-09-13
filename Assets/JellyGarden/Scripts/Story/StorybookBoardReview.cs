using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JellyGarden.Scripts.GUI.TutorialStuff;
using UnityEngine;
using UnityEngine.UI;

namespace JellyGarden.Scripts.Story
{
    // Explicit command-line review only. Uses isolated story progress, and restores
    // the inventory/open-level preferences on exit. Never runs in normal play.
    public sealed class StorybookBoardReview : MonoBehaviour
    {
        static string _output;
        static readonly Dictionary<string, int> Prefs = new Dictionary<string, int>();
        readonly List<string> _errors = new List<string>();
        bool _finished;
        float _deadline;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        static void Install()
        {
            string arg = Environment.GetCommandLineArgs().FirstOrDefault(a => a.StartsWith("-storybook-board-review="));
            if (arg == null) return;
            _output = arg.Substring("-storybook-board-review=".Length);
            Directory.CreateDirectory(_output);
            StoryProgress.SaveDirectoryOverride = Path.Combine(_output, "isolated-save-" + DateTime.UtcNow.Ticks);
            foreach (string key in new[] { "OpenLevel", "OpenLevelTest", "ExtraMoves", "ExtraTime", "Hand", "Bomb", "Random_color" })
                Prefs[key] = PlayerPrefs.HasKey(key) ? PlayerPrefs.GetInt(key) : int.MinValue;
            var host = new GameObject("~StorybookBoardReview");
            DontDestroyOnLoad(host);
            host.AddComponent<StorybookBoardReview>();
        }
        void OnEnable() { Application.logMessageReceived += Log; }
        void OnDisable() { Application.logMessageReceived -= Log; }
        void Log(string message, string stack, LogType type)
        {
            if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert) _errors.Add(message + "\n" + stack);
        }
        void Update() { if (!_finished && _deadline > 0 && Time.realtimeSinceStartup > _deadline) { _errors.Add("Review timed out"); Finish(); } }
        IEnumerator Start()
        {
            _deadline = Time.realtimeSinceStartup + 240;
            yield return new WaitForSecondsRealtime(6);
            foreach (string key in new[] { "ExtraMoves", "ExtraTime", "Hand", "Bomb", "Random_color" }) PlayerPrefs.SetInt(key, 3);
            foreach (int level in new[] { 1, 3, 4, 17, 12 })
            {
                Time.timeScale = 1;
                StoryShell.CloseForLevel();
                PlayerPrefs.SetInt("OpenLevel", level);
                InitScript.openLevel = level;
                LevelManager manager = LevelManager.THIS;
                manager.LoadLevel();
                manager.gameStatus = GameState.PrepareGame;
                yield return new WaitForSecondsRealtime(7);
                StoryPrePlay.Close();
                foreach (TutorialManager tutorial in FindObjectsByType<TutorialManager>(FindObjectsInactive.Include))
                    if (tutorial.tutorial != null) tutorial.tutorial.SetActive(false);
                if (manager.gameStatus != GameState.Playing) _errors.Add("Level " + level + " is " + manager.gameStatus);
                foreach (Item item in FindObjectsByType<Item>(FindObjectsSortMode.None))
                    if (item.currentType == ItemsTypes.NONE && (item.color < 0 || item.color > 5 || item.sprRenderer.sprite != StorybookBoardArt.Piece(item.color)))
                        _errors.Add("Sprite/colour mismatch at level " + level);
                StoryLevelHud hud = StoryLevelHud.Instance;
                hud.Refresh();
                if (hud.VisibleTargetCount != manager.targetObject.Length) _errors.Add("Missing goals on level " + level);
                yield return Shots("level-" + level);
                if (level == 17)
                {
                    var live = FindObjectsByType<Item>(FindObjectsSortMode.None).Where(i => i.currentType == ItemsTypes.NONE && i.square != null).Take(4).ToArray();
                    var kinds = new[] { ItemsTypes.HORIZONTAL_STRIPPED, ItemsTypes.VERTICAL_STRIPPED, ItemsTypes.PACKAGE, ItemsTypes.BOMB };
                    for (int i = 0; i < live.Length; i++) { live[i].NextType = kinds[i]; live[i].ChangeType(); }
                    yield return new WaitForSecondsRealtime(1);
                    yield return Shots("board-specials");
                    manager.limitType = LIMIT.TIME;
                    manager.Limit = 45;
                    manager.InGameBoosts[0].gameObject.SetActive(false);
                    manager.InGameBoosts[1].gameObject.SetActive(true);
                    hud.Refresh();
                    yield return Shots("timed");
                    manager.limitType = LIMIT.MOVES;
                }
                if (level == 12)
                {
                    int before = manager.Limit;
                    Button extra = FindObjectsByType<Button>(FindObjectsSortMode.None).FirstOrDefault(b => b.name == "Booster0");
                    extra.onClick.Invoke();
                    if (manager.Limit != before + 5 || PlayerPrefs.GetInt("ExtraMoves") != 2) _errors.Add("Extra moves callback/count did not work");
                    for (int i = 1; i < 4; i++)
                    {
                        Button button = FindObjectsByType<Button>(FindObjectsSortMode.None).First(b => b.name == "Booster" + i);
                        button.onClick.Invoke();
                        if (manager.activatedBoost == null) _errors.Add("Booster " + i + " did not select");
                        button.onClick.Invoke();
                        if (manager.activatedBoost != null) _errors.Add("Booster " + i + " did not deselect");
                    }
                }
            }
            // One normal legal-swap playthrough verifies matches/cascades/ice and win delivery.
            yield return StoryPlaytest.PlayForCampaignReview(1, 1);
            if (!StoryProgress.IsLevelBeaten(1)) _errors.Add("Legal board-1 playthrough did not win");
            yield return Shots("won-level-1");
            Finish();
        }
        IEnumerator Shots(string name)
        {
            foreach (Vector2Int size in new[] { new Vector2Int(393, 852), new Vector2Int(320, 568) })
            {
                Screen.SetResolution(size.x, size.y, FullScreenMode.Windowed);
                yield return new WaitForSecondsRealtime(.7f);
                yield return new WaitForEndOfFrame();
                StoryWindow.Capture(Path.Combine(_output, name + "-" + size.x + ".png"));
                if (LevelManager.THIS.gameStatus == GameState.Playing)
                {
                    Rect field = StorybookBoardLayout.FieldRect(StorybookBoardLayout.Safe);
                    foreach (Square square in LevelManager.THIS.squaresArray)
                    {
                        if (square == null || square.type == SquareTypes.NONE) continue;
                        Vector3 point = Camera.main.WorldToScreenPoint(square.transform.position);
                        if (!field.Contains(point)) { _errors.Add("Board outside reserved space: " + name + " at " + size); break; }
                    }
                }
            }
            Screen.SetResolution(StoryWindow.Width, StoryWindow.Height, FullScreenMode.Windowed);
            yield return new WaitForSecondsRealtime(.3f);
        }
        void Restore()
        {
            foreach (var pair in Prefs)
                if (pair.Value == int.MinValue) PlayerPrefs.DeleteKey(pair.Key); else PlayerPrefs.SetInt(pair.Key, pair.Value);
            PlayerPrefs.Save();
        }
        void OnApplicationQuit() { Restore(); }
        void Finish()
        {
            if (_finished) return;
            _finished = true;
            Restore();
            File.WriteAllText(Path.Combine(_output, "results.txt"), _errors.Count == 0 ? "PASS: colour identity, five real board layouts, targets, timed HUD, four booster callbacks, legal level-1 win, both phone sizes.\n" : string.Join("\n\n", _errors));
            Application.Quit(_errors.Count == 0 ? 0 : 1);
        }
    }
}
