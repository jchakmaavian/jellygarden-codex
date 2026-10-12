using System;
using System.Collections.Generic;
using JellyGarden.Scripts;
using JellyGarden.Scripts.Juice;
using UnityEngine;

namespace RoyalThrone.ReferenceGameplay
{
    // Board-only native asset adapter. Home, branding and menu catalogs are deliberately
    // independent. A missing mapping is a reported import gap, never a generated asset.
    public static class ReferenceGameplayCatalog
    {
        [Serializable] public sealed class Manifest { public Entry[] entries; }
        [Serializable] public sealed class Entry
        {
            public string key, resourcesPath, sourceName;
            public long sourcePathId;
        }
        static Dictionary<string, Entry> entries;
        static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
        static readonly HashSet<Sprite> nativeSprites = new HashSet<Sprite>();
        static readonly HashSet<string> reported = new HashSet<string>();
        static readonly string[] colours = { "ruby", "shield", "crown", "leaf", "lily", "vase" };
        public static bool Available { get { Load(); return entries.Count > 0; } }
        public static IEnumerable<Entry> Entries { get { Load(); return entries.Values; } }
        static void Load()
        {
            if (entries != null) return;
            entries = new Dictionary<string, Entry>(StringComparer.Ordinal);
            var text = Resources.Load<TextAsset>("ReferenceGameplay/runtime-catalog");
            if (!text) return;
            var manifest = JsonUtility.FromJson<Manifest>(text.text);
            if (manifest?.entries == null) return;
            foreach (var entry in manifest.entries)
                if (!string.IsNullOrEmpty(entry.key) && !string.IsNullOrEmpty(entry.resourcesPath)) entries.Add(entry.key, entry);
        }
        public static Sprite Art(string key)
        {
            Load();
            if (key == null || !entries.TryGetValue(key, out var entry)) return null;
            if (sprites.TryGetValue(key, out var cached)) return cached;
            var sprite = Resources.Load<Sprite>(entry.resourcesPath);
            sprites[key] = sprite;
            if (sprite) nativeSprites.Add(sprite);
            else if (reported.Add(key)) Debug.LogError("[Reference Gameplay] Missing imported Sprite: " + entry.resourcesPath);
            return sprite;
        }
        public static Sprite For(ItemsTypes type, int color)
        {
            switch (type)
            {
                case ItemsTypes.INGREDIENT: return null;
                case ItemsTypes.HORIZONTAL_STRIPPED: return Art("rocket");
                case ItemsTypes.VERTICAL_STRIPPED: return Art("rocket-vertical");
                case ItemsTypes.PACKAGE: return Art("tnt");
                case ItemsTypes.BOMB: return Art("orb");
                case ItemsTypes.HELICOPTER: return Art("helicopter");
                default: return Art(colours[Mathf.Clamp(color, 0, colours.Length - 1)]);
            }
        }
        public static bool IsNative(Sprite sprite) => sprite && nativeSprites.Contains(sprite);
        // Reference sprites use their recovered PPU and pivot. One reference board cell
        // is one source unit; do not fit every individual silhouette to the same bounds.
        public static float CellScale(LevelManager level, Transform parent)
        {
            if (!level) return 1;
            float field = level.GameField ? Mathf.Abs(level.GameField.lossyScale.x) : 1;
            return level.squareWidth * field / Mathf.Max(.0001f, parent ? Mathf.Abs(parent.lossyScale.x) : 1);
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { entries = null; sprites.Clear(); nativeSprites.Clear(); reported.Clear(); }
    }
}
