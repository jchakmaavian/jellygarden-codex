// Where in a room a repair happens.
//
// Royal Match puts its repair effect on the thing being repaired, not over the
// whole picture. Our rooms are painted a state at a time rather than an object
// at a time, so nothing in the story data says where the change is — but the
// pixels do: state 3 differs from state 2 exactly where repair 3 landed.
//
// That diff is taken offline by tools/repair_regions_build.py and shipped as a
// small table, rather than measured here. Reading a sprite's pixels at runtime
// needs `isReadable` on the importer, which keeps an uncompressed second copy
// of every room in memory; 150 areas of 1500px artwork is not a cost worth
// paying on a phone to learn a handful of rectangles.
//
// Most areas are NOT in the table, and that is the design rather than a gap:
// an area with one restored picture has no diff to take, and an area whose art
// repaints wholesale between states has a "region" that is the whole room with
// extra steps. Ask for one of those and you get Unknown, and the caller falls
// back to something that covers the room — which is the honest answer, because
// nobody has said where that repair happens.

using System.Collections.Generic;
using UnityEngine;

namespace JellyGarden.Scripts.Story
{
    public static class StoryRepairRegions
    {
        private const string Path = "Story/repair_regions";

        /// <summary>
        /// What a caller gets when the region is not known. Deliberately the
        /// whole room rather than a guessed corner: a sparkle burst centred on a
        /// place nothing was repaired is worse than one that covers everything.
        /// </summary>
        public static readonly Rect Unknown = new Rect(0f, 0f, 1f, 1f);

        private static Dictionary<int, Rect> _regions;

        /// <summary>
        /// The rect repair <paramref name="state"/> of an area changed, in the
        /// room's own space: origin bottom-left, 0..1 on both axes. State 1 is
        /// the first repair.
        ///
        /// Returns <see cref="Unknown"/> when nobody has measured this one.
        /// </summary>
        public static Rect For(int areaId, int state)
        {
            Load();

            Rect rect;
            return _regions.TryGetValue(Key(areaId, state), out rect) ? rect : Unknown;
        }

        /// <summary>Whether this repair has a measured region of its own.</summary>
        public static bool IsKnown(int areaId, int state)
        {
            Load();
            return _regions.ContainsKey(Key(areaId, state));
        }

        /// <summary>How many regions the table carries. For tests and the log.</summary>
        public static int Count
        {
            get { Load(); return _regions.Count; }
        }

        // Areas run to 150 and an area has at most a dozen repairs, so one int
        // keys the pair with room to spare and without allocating a tuple per
        // lookup.
        private static int Key(int areaId, int state)
        {
            return areaId * 100 + state;
        }

        private static void Load()
        {
            if (_regions != null) return;
            _regions = new Dictionary<int, Rect>();

            var text = Resources.Load<TextAsset>(Path);
            if (text == null)
            {
                Debug.Log("[Story] No repair regions table; every repair sparkles room-wide.");
                return;
            }

            JsonValue root = JsonValue.Parse(text.text);

            for (int areaId = 1; areaId <= 150; areaId++)
            {
                JsonValue area = root[areaId.ToString()];
                if (area.IsNull) continue;

                for (int state = 1; state <= 32; state++)
                {
                    JsonValue rect = area[state.ToString()];
                    if (rect.IsNull || rect.Count < 4) continue;

                    _regions[Key(areaId, state)] = new Rect(
                        rect[0].AsFloat(), rect[1].AsFloat(),
                        rect[2].AsFloat(), rect[3].AsFloat());
                }
            }

            Debug.Log(string.Format(
                "[Story] {0} repair region(s) loaded; the rest sparkle room-wide.",
                _regions.Count));
        }

        /// <summary>Drops the cache. Editor tests re-read the table with this.</summary>
        public static void Forget()
        {
            _regions = null;
        }
    }
}
