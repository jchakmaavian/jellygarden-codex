using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace JellyGarden.Scripts.Story.Editor
{
    public static class StoryWinPopupTests
    {
        [MenuItem("JellyGarden/Story/Preview/Open Victory (first win)")]
        private static void PreviewWin() => Preview(true);

        [MenuItem("JellyGarden/Story/Preview/Open Victory (replay)")]
        private static void PreviewReplay() => Preview(false);

        [MenuItem("JellyGarden/Story/Preview/Open Victory (first win)", true)]
        [MenuItem("JellyGarden/Story/Preview/Open Victory (replay)", true)]
        private static bool CanPreview() => Application.isPlaying;

        private static void Preview(bool paid)
        {
            int level = LevelManager.THIS == null ? 3 : Mathf.Max(1, LevelManager.THIS.currentLevel);
            StoryWinPopup.Open(level, StoryLevelPlan.AreaOf(level), paid, null);
        }

        // Exercises the production component without changing any save data.
        [MenuItem("JellyGarden/Story/Run Victory Tests")]
        public static void Run()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Run victory tests outside Play mode.");
            int level = 3, area = StoryLevelPlan.AreaOf(level);
            int starsBefore = StoryProgress.StarsIn(area);
            bool beatenBefore = StoryProgress.IsLevelBeaten(level);
            bool enabledBefore = StoryWinPopup.Enabled;
            float timeScaleBefore = Time.timeScale;
            GameObject host = null;
            try
            {
                StoryWinPopup.Enabled = true;
                int callbacks = 0;
                StoryWinPopup.Open(level, area, true, () => callbacks++);
                host = GameObject.Find(StoryWinPopup.HostName);
                Require(StoryWinPopup.IsOpen && StoryWinPopup.TitleText == "Level 3", "first win opens");
                Require(StoryWinPopup.CaptionText == "+1 Star earned", "first win describes one star");
                Button button = host.GetComponentInChildren<Button>();
                button.onClick.Invoke();
                button.onClick.Invoke();
                Require(callbacks == 1 && !StoryWinPopup.IsOpen, "double Continue invokes callback only once");

                StoryWinPopup.Open(level, area, false, () => callbacks++);
                Require(StoryWinPopup.CaptionText == "Star already earned", "replay has truthful positive wording");
                Image star = host.GetComponentsInChildren<Image>().Single(i => i.name == "Star");
                Require(star.color == Color.white && star.sprite != null, "replay star stays bright gold");
                Image headline = host.GetComponentsInChildren<Image>().Single(i => i.name == "Headline");
                Require(headline.sprite == StoryArt.Load("win_victory_headline") && headline.sprite != null &&
                    headline.preserveAspect && !headline.raycastTarget, "curved headline image is visible and keeps its proportions");
                Require(!host.GetComponentsInChildren<Text>().Any(t => t.text == "Level Complete!"),
                    "straight headline text is replaced");
                StoryWinPopup.Close();
                button.onClick.Invoke();
                Require(callbacks == 1, "external close discards a stale continuation");
                StoryWinPopup.Open(level + 1, StoryLevelPlan.AreaOf(level + 1), true, () => callbacks += 10);
                button.onClick.Invoke();
                Require(callbacks == 11, "reopen uses the new continuation");

                StoryWinPopup.Open(level, area, true, null);
                StoryWinStarVfx vfx = host.GetComponentInChildren<StoryWinStarVfx>();
                StoryClock.Override(100f);
                Time.timeScale = 0f;
                vfx.Play(100f);
                Require(star.transform.localScale.x < 0.01f, "star entrance starts hidden");
                StoryClock.Advance(0.65f);
                vfx.SendMessage("Update");
                Require(star.transform.localScale.x > 1f, "star bounces while time scale is zero");
                StoryClock.Advance(0.8f);
                vfx.SendMessage("Update");
                Vector3 firstPosition = star.transform.localPosition;
                StoryClock.Advance(0.6f);
                vfx.SendMessage("Update");
                Require(Vector3.Distance(firstPosition, star.transform.localPosition) > 0.1f, "idle motion continues while paused");

                foreach (string key in new[] { "win_panel", "win_victory_star", "win_victory_banner", "win_victory_headline", "win_victory_button" })
                    Require(StoryArt.Load(key) != null, "production sprite imported: " + key);
                Require(Resources.Load<Shader>("Story/UI/WinBackdropBlur") != null, "blur shader ships in Resources");
                Require(StoryProgress.StarsIn(area) == starsBefore &&
                    StoryProgress.IsLevelBeaten(level) == beatenBefore, "presentation never changes rewards or progress");
                Debug.Log("[Story] Victory tests passed: rewards, replay, callbacks, reopen, paused animation and assets.");
            }
            finally
            {
                StoryWinPopup.Close();
                if (host != null) UnityEngine.Object.DestroyImmediate(host);
                StoryClock.Release();
                Time.timeScale = timeScaleBefore;
                StoryWinPopup.Enabled = enabledBefore;
            }
        }

        public static void RunHeadless()
        {
            try { Run(); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("[Story] Victory test failed: " + message);
        }
    }
}
