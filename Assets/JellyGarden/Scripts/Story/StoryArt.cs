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
        /// The top painted layer of the room at the player's saved repair count,
        /// using the same state order and base-art fallback as AreaSceneView.
        /// Missing intermediate paintings keep the last available state visible.
        /// </summary>
        public static Sprite AreaAtProgress(int areaId)
        {
            StoryArea area = StoryDatabase.GetArea(areaId);
            if (area == null) return null;

            int done = Mathf.Clamp(StoryProgress.TasksCompletedIn(areaId), 0, area.Tasks.Count);
            for (int state = done; state >= 0; state--)
            {
                Sprite sprite = AreaState(areaId, state);
                if (sprite != null) return sprite;
            }

            return AreaRestored(areaId);
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
        /// A speaking character's portrait in a named mood — `portrait_{name}_{mood}`.
        ///
        /// Every character is drawn in four moods, and one of the four is their
        /// resting face, filed under the plain `portrait_{name}` the rest of the
        /// game asks for. So a miss here is the normal case rather than a fault:
        /// asking Leo for `happy` (his resting face) or for a mood nobody drew
        /// both land on the plain portrait, and neither warns. The mood is a
        /// hint the art can answer, not a contract it must meet.
        /// </summary>
        public static Sprite Portrait(string character, string mood)
        {
            if (string.IsNullOrEmpty(character)) return null;
            if (string.IsNullOrEmpty(mood)) return Portrait(character);

            string key = "portrait_" + character.Trim().ToLowerInvariant() +
                         "_" + mood.Trim().ToLowerInvariant();
            Sprite portrait = Load(key);
            return portrait != null ? portrait : Portrait(character);
        }

        /// <summary>
        /// A character's avatar animation, in frame order, or an empty array
        /// when none is authored.
        ///
        /// Frames are `avatar_{name}_00`, `_01`, … and are read until the first
        /// gap, so adding frames to Resources/Story/Art lengthens the loop with
        /// no code change — the same convention the rest of this file uses. Like
        /// <see cref="TaskIcon"/> it does NOT warn on a miss: a character with
        /// no avatar frames is the normal case, and the caller falls back to a
        /// crop of the portrait.
        /// </summary>
        public static Sprite[] AvatarFrames(string character)
        {
            return AvatarFrames(character, null);
        }

        /// <summary>
        /// The frames of one NAMED clip: avatar_{character}_{clip}_00, _01, …
        ///
        /// A null or empty clip is the idle loop, whose frames carry no clip
        /// segment at all — so the idle keeps the name it already had on disk
        /// and a character with only an idle is unaffected by this overload.
        ///
        /// Like the idle, a missing clip is the normal case and returns empty
        /// rather than warning: every reaction is optional, and a character with
        /// none simply never reacts.
        /// </summary>
        public static Sprite[] AvatarFrames(string character, string clip)
        {
            if (string.IsNullOrEmpty(character)) return EmptyFrames;

            string name = character.Trim().ToLowerInvariant();
            string prefix = string.IsNullOrEmpty(clip)
                ? "avatar_" + name + "_"
                : "avatar_" + name + "_" + clip.Trim().ToLowerInvariant() + "_";
            var frames = new List<Sprite>();

            for (int i = 0; i < MaxAvatarFrames; i++)
            {
                Sprite frame = Load(string.Format("{0}{1:00}", prefix, i));
                if (frame == null) break;
                frames.Add(frame);
            }

            return frames.Count == 0 ? EmptyFrames : frames.ToArray();
        }

        /// <summary>A ceiling, so a naming slip cannot turn into an unbounded loop.</summary>
        private const int MaxAvatarFrames = 64;

        private static readonly Sprite[] EmptyFrames = new Sprite[0];

        /// <summary>
        /// The head-and-shoulders window onto a portrait, in texture coordinates
        /// (origin bottom-left), used when a character has no avatar frames of
        /// their own — which, since the cast was redrawn, is everyone.
        ///
        /// The redrawn portraits are busts rather than standing figures, so the
        /// head fills far more of the frame than it used to and the old window
        /// landed on a forehead. This one was picked by cropping all eight and
        /// looking: 12% in from each side, starting 5% down, 76% of the frame
        /// square. It clears Leo's crown and the Cook's hat at the top and stops
        /// below every chin, so one window still serves them all — including
        /// Mimi, whose whole head is the portrait.
        /// </summary>
        public static readonly Rect AvatarCrop =
            new Rect(0.12f, 1f - 0.81f, 0.76f, 0.76f);

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
