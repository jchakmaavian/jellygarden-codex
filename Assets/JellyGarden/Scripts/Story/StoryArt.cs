// Where the story's pictures come from.
//
// Art is resolved by convention from Resources/Story/Art, so adding a picture is
// dropping a file with the right name — no code change, no inspector wiring. A
// miss warns once per key and falls back to the arc's backdrop, and then to
// nothing, so a screen never renders blank and never spams the log per frame.

using System.Collections.Generic;
using UnityEngine;

namespace JellyGarden.Scripts.Story
{
    public static class StoryArt
    {
        private const string Root = "Story/Art/";

        private static readonly Dictionary<string, Sprite> _cache =
            new Dictionary<string, Sprite>();
        private static readonly HashSet<string> _warned = new HashSet<string>();

        /// <summary>Resources/Story/Art/&lt;key&gt;, or null.</summary>
        public static Sprite Load(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;

            Sprite sprite;
            if (_cache.TryGetValue(key, out sprite)) return sprite;

            sprite = Resources.Load<Sprite>(Root + key);
            _cache[key] = sprite;
            return sprite;
        }

        /// <summary>
        /// The nth state of an area's scene: state 0 is the ruin, state n is the
        /// scene with n repairs done.
        /// </summary>
        public static Sprite AreaState(int areaId, int state)
        {
            return Load(string.Format("area_{0:000}_state{1}", areaId, state));
        }

        /// <summary>The picture on an area's clue card.</summary>
        public static Sprite ClueImage(int areaId)
        {
            Sprite clue = Load(string.Format("clue_{0:000}", areaId));
            return clue != null ? clue : ArcBackdrop(areaId);
        }

        /// <summary>The arc's backdrop, used wherever an area has no art of its own.</summary>
        public static Sprite ArcBackdrop(int areaId)
        {
            string theme = StoryDatabase.ThemeOf(areaId);
            Sprite backdrop = Load("backdrop_" + theme);
            if (backdrop == null) WarnOnce("backdrop_" + theme);
            return backdrop;
        }

        /// <summary>A speaking character's portrait, keyed by name.</summary>
        public static Sprite Portrait(string character)
        {
            if (string.IsNullOrEmpty(character)) return null;
            string key = "portrait_" + character.Trim().ToLowerInvariant();
            Sprite portrait = Load(key);
            if (portrait == null) WarnOnce(key);
            return portrait;
        }

        private static void WarnOnce(string key)
        {
            if (!_warned.Add(key)) return;
            Debug.LogWarning("[Story] No art for '" + key + "' in Resources/" + Root +
                             "; falling back.");
        }

        /// <summary>Test hook — forces the next lookup to hit Resources again.</summary>
        public static void ClearCache()
        {
            _cache.Clear();
            _warned.Clear();
        }
    }
}
