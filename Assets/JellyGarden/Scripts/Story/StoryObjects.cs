// Two small seams that let the story layer be exercised outside play mode.
//
// The story screens are built in code, so an editor test can construct and drive
// them for real — but only if they do not call play-mode-only APIs on the way up.

using UnityEngine;

namespace JellyGarden.Scripts.Story
{
    public static class StoryObjects
    {
        /// <summary>
        /// DontDestroyOnLoad, but a no-op outside play mode where Unity refuses
        /// it. Story screens survive scene changes in the game and are ordinary
        /// objects in a test.
        /// </summary>
        public static void Persist(GameObject go)
        {
            if (go == null) return;
            if (!Application.isPlaying) return;
            Object.DontDestroyOnLoad(go);
        }
    }

    /// <summary>
    /// The clock the story's timed screens read. In the game it is Unity's
    /// unscaled time; a test can take it over and step it, so a three-second
    /// clue card is verified in microseconds and never flakes.
    /// </summary>
    public static class StoryClock
    {
        private static bool _overridden;
        private static float _now;

        public static float Now
        {
            get { return _overridden ? _now : Time.unscaledTime; }
        }

        public static void Override(float now)
        {
            _overridden = true;
            _now = now;
        }

        public static void Advance(float seconds)
        {
            if (!_overridden) Override(Time.unscaledTime);
            _now += seconds;
        }

        public static void Release()
        {
            _overridden = false;
            _now = 0f;
        }

        public static bool IsOverridden
        {
            get { return _overridden; }
        }
    }
}
