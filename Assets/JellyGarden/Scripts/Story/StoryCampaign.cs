// A campaign selects its content and progress together. Legacy assets remain
// available to the legacy campaign, never as accidental new-story room fallbacks.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace JellyGarden.Scripts.Story
{
    public static class StoryCampaign
    {
        private static bool _loaded;
        private static string _root = "Story";
        private static JsonValue _manifest = JsonValue.Null;
        private static readonly HashSet<int> _ready = new HashSet<int>();
        private static readonly HashSet<string> _sharedArt = new HashSet<string>();

        public static bool IsTreasureHunt { get { Load(); return Id == "treasure-hunt"; } }
        public static string Id { get { Load(); return _manifest["id"].AsString("legacy"); } }
        public static string ResourceRoot { get { Load(); return _root; } }
        public static string SaveFileName { get { return IsTreasureHunt ? "treasure_hunt_save.json" : "story_save.json"; } }
        public static string OpeningScene { get { Load(); return _manifest["openingScene"].AsString(); } }
        public const string ComingSoon = "More adventures are being prepared!";

        private static void Load()
        {
            if (_loaded) return;
            _loaded = true;
            // Explicit developer opt-out, so the original campaign can still be
            // inspected without changing a player's saved progress.
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-legacy-story") >= 0) return;
            var active = Resources.Load<TextAsset>("Story/active_campaign");
            if (active == null) return;
            _root = JsonValue.Parse(active.text)["resourceRoot"].AsString("Story");
            var manifest = Resources.Load<TextAsset>(_root + "/campaign");
            if (manifest == null) throw new InvalidOperationException("Campaign manifest is missing: " + _root);
            _manifest = JsonValue.Parse(manifest.text);
            foreach (JsonValue area in _manifest["readyAreas"].Items) _ready.Add(area.AsInt());
            foreach (JsonValue key in _manifest["sharedArt"].Items) _sharedArt.Add(key.AsString());
        }

        public static TextAsset LoadText(string key)
        {
            return Resources.Load<TextAsset>(ResourceRoot + "/" + key);
        }

        public static bool IsReady(int areaId)
        {
            Load();
            return areaId > 0 && (!IsTreasureHunt || _ready.Contains(areaId));
        }

        public static bool CanEnter(int areaId)
        {
            return !IsTreasureHunt || (IsReady(areaId) && StoryProgress.IsAreaUnlocked(areaId));
        }

        public static Sprite LoadArt(string key)
        {
            Sprite sprite = Resources.Load<Sprite>(ResourceRoot + "/Art/" + key);
            if (sprite != null || !IsTreasureHunt) return sprite;
            foreach (JsonValue alias in _manifest["artAliases"].Items)
            {
                if (alias["key"].AsString() != key) continue;
                sprite = Resources.Load<Sprite>(ResourceRoot + "/Art/" + alias["source"].AsString());
                if (sprite == null) return null;
                JsonValue crop = alias["rect"];
                if (crop.Count != 4) return sprite;
                Rect source = sprite.rect;
                var rect = new Rect(source.x + crop[0].AsFloat() * source.width,
                    source.y + crop[1].AsFloat() * source.height,
                    crop[2].AsFloat() * source.width, crop[3].AsFloat() * source.height);
                Sprite detail = Sprite.Create(sprite.texture, rect, new Vector2(0.5f, 0.5f), sprite.pixelsPerUnit);
                detail.name = key;
                return detail;
            }
            return _sharedArt.Contains(key) ? Resources.Load<Sprite>("Story/Art/" + key) : null;
        }

        public static bool ProtectFirstPuzzle(int level)
        {
            if (!IsTreasureHunt || StoryProgress.IsLevelBeaten(level)) return false;
            IList<int> openingLevels = StoryLevelPlan.LevelsFor(1);
            return openingLevels.Count > 0 && level == openingLevels[0];
        }

        public static void ApplyLevelTuning(LevelManager manager)
        {
            if (!IsTreasureHunt) return;
            StoryLevelEntry entry = StoryLevelPlan.Get(manager.currentLevel);
            if (entry != null && entry.MinimumMoves > 0)
                manager.Limit = Mathf.Max(manager.Limit, entry.MinimumMoves);
        }
    }
}
