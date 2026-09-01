// King Leo story layer — the one place story content is read from.
//
// Every screen asks StoryDatabase for its area. No screen parses JSON, and no
// screen hardcodes an area id.

using System.Collections.Generic;
using UnityEngine;

namespace JellyGarden.Scripts.Story
{
    public static class StoryDatabase
    {
        public const int ArcCount = 6;

        private static bool _loaded;
        private static readonly List<StoryArea> _areas = new List<StoryArea>();
        private static readonly Dictionary<int, StoryArea> _byId = new Dictionary<int, StoryArea>();
        private static readonly List<StoryArcInfo> _arcs = new List<StoryArcInfo>();

        public static IList<StoryArea> AllAreas
        {
            get { Load(); return _areas; }
        }

        public static IList<StoryArcInfo> Arcs
        {
            get { Load(); return _arcs; }
        }

        public static int AreaCount
        {
            get { Load(); return _areas.Count; }
        }

        public static int TotalStars
        {
            get
            {
                Load();
                int total = 0;
                for (int i = 0; i < _areas.Count; i++) total += _areas[i].StarCost;
                return total;
            }
        }

        public static int TotalTasks
        {
            get
            {
                Load();
                int total = 0;
                for (int i = 0; i < _areas.Count; i++) total += _areas[i].Tasks.Count;
                return total;
            }
        }

        /// <summary>Idempotent. Safe to call from any Awake or before scene load.</summary>
        public static void Load()
        {
            if (_loaded) return;
            _loaded = true;

            for (int arc = 1; arc <= ArcCount; arc++)
            {
                var text = Resources.Load<TextAsset>("Story/arc" + arc);
                if (text == null)
                {
                    Debug.LogWarning("[Story] Missing Resources/Story/arc" + arc + ".json");
                    continue;
                }

                JsonValue root = JsonValue.Parse(text.text);
                foreach (JsonValue areaJson in root["areas"].Items)
                {
                    StoryArea area = StoryArea.From(areaJson, arc);
                    if (area.Id <= 0 || _byId.ContainsKey(area.Id)) continue;
                    _areas.Add(area);
                    _byId[area.Id] = area;
                }
            }

            _areas.Sort((a, b) => a.Id.CompareTo(b.Id));

            var index = Resources.Load<TextAsset>("Story/index");
            if (index != null)
            {
                JsonValue root = JsonValue.Parse(index.text);
                foreach (JsonValue arcJson in root["arcs"].Items)
                    _arcs.Add(StoryArcInfo.From(arcJson));
            }

            Debug.Log(string.Format("Story loaded: {0} areas, {1} tasks, {2} stars",
                _areas.Count, TotalTasks, TotalStars));
        }

        /// <summary>Null when the id is outside the authored range.</summary>
        public static StoryArea GetArea(int areaId)
        {
            Load();
            StoryArea area;
            return _byId.TryGetValue(areaId, out area) ? area : null;
        }

        public static StoryArcInfo ArcOf(int areaId)
        {
            Load();
            for (int i = 0; i < _arcs.Count; i++)
                if (areaId >= _arcs[i].FirstArea && areaId <= _arcs[i].LastArea)
                    return _arcs[i];
            return null;
        }

        /// <summary>The arc theme key used to dress the board. Falls back to castle.</summary>
        public static string ThemeOf(int areaId)
        {
            StoryArcInfo arc = ArcOf(areaId);
            int n = arc != null ? arc.Arc : 1;
            switch (n)
            {
                case 1: return "castle";
                case 2: return "village";
                case 3: return "mines";
                case 4: return "border";
                case 5: return "crypts";
                case 6: return "siege";
                default: return "castle";
            }
        }

        /// <summary>Test hook — forces the next access to re-read from Resources.</summary>
        public static void Unload()
        {
            _loaded = false;
            _areas.Clear();
            _byId.Clear();
            _arcs.Clear();
        }
    }
}
