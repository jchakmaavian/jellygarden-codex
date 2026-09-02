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

        /// <summary>
        /// The area fully restored. Most areas have one of these rather than a
        /// picture per repair — the repair cutscene wipes it in over the ruined
        /// version, and the neglect wash carries the steps in between.
        /// </summary>
        public static Sprite AreaRestored(int areaId)
        {
            Sprite restored = Load(string.Format("area_{0:000}_restored", areaId));
            if (restored != null) return restored;

            // An area with per-state art: the last state IS the restored one.
            StoryArea area = StoryDatabase.GetArea(areaId);
            if (area != null)
            {
                for (int state = area.Tasks.Count; state >= 0; state--)
                {
                    Sprite exact = AreaState(areaId, state);
                    if (exact != null) return exact;
                }
            }

            return ArcBackdrop(areaId);
        }

        /// <summary>Whether this area has artwork of its own at all.</summary>
        public static bool HasAreaArt(int areaId)
        {
            return Load(string.Format("area_{0:000}_restored", areaId)) != null
                   || AreaState(areaId, 0) != null;
        }

        /// <summary>
        /// The object drawn on a task's row, if anyone has painted one.
        /// Resources/Story/Art/task_012_3.png is task 3 of area 12.
        ///
        /// A miss is silent, unlike the rest of this class: there are 807 tasks
        /// and no art for any of them yet, so warning per key would be 807 lines
        /// of log for a case StoryUi.TaskIcon already handles by drawing a glyph
        /// for the kind of work the label describes.
        /// </summary>
        public static Sprite TaskIcon(int areaId, int taskIndex)
        {
            return Load(string.Format("task_{0:000}_{1}", areaId, taskIndex));
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

        /// <summary>
        /// The keys that have fallen back so far. A miss warns instead of
        /// failing, so a screen still draws — which means the only way to prove
        /// an area's art is complete is to read what warned.
        /// </summary>
        public static IEnumerable<string> WarnedKeys
        {
            get { return _warned; }
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
