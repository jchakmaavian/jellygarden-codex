using System.Collections.Generic;
using UnityEngine;

namespace JellyGarden.Scripts.Juice
{
    /// <summary>
    /// Prefer original Royal Throne campaign art, then the existing gameplay pack.
    /// Cached legacy sprite views preserve their audited bounds and centered pivots.
    /// </summary>
    public static class RoyalGameplayArt
    {
        public const string ResourceRoot = "RoyalGameplayV4/";
        public const int AssetCount = 77;

        sealed class Entry
        {
            public string Path;
            public Rect Rect;
            public Vector2 Pivot;
            public Vector4 Border;
            public Sprite Sprite;
        }

        static readonly Dictionary<string, Entry> entries = Build();
        static readonly string[] colours = { "ruby", "shield", "crown", "leaf", "lily", "vase" };

        /// <summary>Load a gameplay sprite once. Unknown keys return null for legacy fallback.</summary>
        public static Sprite Art(string key)
        {
            var campaign = RoyalPieceSkin.CampaignArt(key);
            if (campaign) return campaign;
            return LegacyArt(key);
        }

        /// <summary>Retain the previous pack for fallback and its own authoring checks.</summary>
        public static Sprite LegacyArt(string key)
        {
            if (string.IsNullOrEmpty(key) || !entries.TryGetValue(key, out var entry)) return null;
            if (entry.Sprite) return entry.Sprite;
            var texture = Resources.Load<Texture2D>(ResourceRoot + entry.Path);
            if (!texture) return null;
            // FullRect avoids read/write texture copies and works for UI and SpriteRenderer.
            entry.Sprite = Sprite.Create(texture, entry.Rect, entry.Pivot, 100f, 0, SpriteMeshType.FullRect, entry.Border);
            entry.Sprite.name = "RoyalGameplayV4_" + key;
            return entry.Sprite;
        }

        public static Sprite For(ItemsTypes type, int color)
        {
            var imported = RoyalThrone.ReferenceGameplay.ReferenceGameplayCatalog.For(type, color);
            if (imported) return imported;
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

        /// <summary>Editor tools can read the legacy pack PNG without enabling texture read/write.</summary>
        public static string ResourcePath(string key)
        {
            return key != null && entries.TryGetValue(key, out var entry) ? ResourceRoot + entry.Path : null;
        }

        public static IEnumerable<string> Keys => entries.Keys;

        static Dictionary<string, Entry> Build()
        {
            var result = new Dictionary<string, Entry>(AssetCount);
            void Add(string key, string path, int canvasWidth, int canvasHeight, int x, int y, int width, int height, int border = 0)
            {
                result.Add(key, new Entry
                {
                    Path = path,
                    Rect = new Rect(x, y, width, height),
                    Pivot = new Vector2((canvasWidth * .5f - x) / width, (canvasHeight * .5f - y) / height),
                    Border = new Vector4(border, border, border, border)
                });
            }
            // Rectangles are generated from audited alpha > 0 bounds. Coordinates are
            // bottom-left, in original pixels. Tiles and all frame parts keep full bounds.
            Add("ruby", "01_match_pieces/block_red", 512, 512, 45, 41, 421, 430);
            Add("crown", "01_match_pieces/crown_yellow", 512, 512, 41, 61, 430, 390);
            Add("lily", "01_match_pieces/crystal_magenta", 512, 512, 41, 62, 430, 388);
            Add("leaf", "01_match_pieces/leaf_green", 512, 512, 53, 41, 405, 430);
            Add("shield", "01_match_pieces/shield_blue", 512, 512, 50, 41, 411, 430);
            Add("tnt", "02_powerups/bomb_barrel", 512, 512, 76, 41, 360, 430);
            Add("orb", "02_powerups/color_ball", 512, 512, 42, 41, 428, 430);
            Add("helicopter", "02_powerups/propeller", 512, 512, 41, 63, 430, 386);
            Add("rocket", "02_powerups/rocket_horizontal", 512, 512, 41, 95, 430, 323);
            Add("rocket-vertical", "02_powerups/rocket_vertical", 512, 512, 116, 41, 280, 430);
            Add("grass-border", "03_grass/grass_border", 512, 512, 0, 0, 512, 512);
            Add("grass-full", "03_grass/grass_full", 512, 512, 0, 0, 512, 512);
            Add("grass-partial", "03_grass/grass_partial", 512, 512, 0, 0, 512, 512);
            Add("cell-dark", "04_board/cell_dark", 512, 512, 0, 0, 512, 512);
            Add("cell-light", "04_board/cell_light", 512, 512, 0, 0, 512, 512);
            Add("frame-gold-9slice", "04_board/frame_gold_9slice", 512, 512, 0, 0, 512, 512, 40);
            Add("frame-corner-bottom-left", "04_board/frame_parts/frame_corner_bottom_left", 40, 40, 0, 0, 40, 40);
            Add("frame-corner-bottom-right", "04_board/frame_parts/frame_corner_bottom_right", 40, 40, 0, 0, 40, 40);
            Add("frame-corner-top-left", "04_board/frame_parts/frame_corner_top_left", 40, 40, 0, 0, 40, 40);
            Add("frame-corner-top-right", "04_board/frame_parts/frame_corner_top_right", 40, 40, 0, 0, 40, 40);
            Add("frame-edge-bottom", "04_board/frame_parts/frame_edge_bottom", 432, 24, 0, 0, 432, 24);
            Add("frame-edge-left", "04_board/frame_parts/frame_edge_left", 24, 432, 0, 0, 24, 432);
            Add("frame-edge-right", "04_board/frame_parts/frame_edge_right", 24, 432, 0, 0, 24, 432);
            Add("frame-edge-top", "04_board/frame_parts/frame_edge_top", 432, 24, 0, 0, 432, 24);
            Add("burst-blue", "05_effects/bursts/burst_blue", 512, 512, 49, 41, 413, 430);
            Add("burst-gold", "05_effects/bursts/burst_gold", 512, 512, 41, 50, 430, 413);
            Add("burst-leaf", "05_effects/bursts/burst_leaf", 512, 512, 41, 117, 430, 279);
            Add("burst-magenta", "05_effects/bursts/burst_magenta", 512, 512, 44, 41, 423, 430);
            Add("burst-red", "05_effects/bursts/burst_red", 512, 512, 49, 41, 413, 430);
            Add("clear-ring", "05_effects/clear_ring", 512, 512, 41, 43, 430, 426);
            Add("hit-flash", "05_effects/hit_flash", 512, 512, 63, 41, 385, 430);
            Add("particle-blue-01", "05_effects/particles/blue_01", 128, 128, 12, 17, 104, 94);
            Add("particle-blue-02", "05_effects/particles/blue_02", 128, 128, 16, 12, 96, 104);
            Add("particle-blue-03", "05_effects/particles/blue_03", 128, 128, 23, 12, 81, 104);
            Add("particle-blue-04", "05_effects/particles/blue_04", 128, 128, 12, 18, 104, 92);
            Add("particle-gold-01", "05_effects/particles/gold_01", 128, 128, 13, 12, 101, 104);
            Add("particle-gold-02", "05_effects/particles/gold_02", 128, 128, 23, 12, 82, 104);
            Add("particle-gold-03", "05_effects/particles/gold_03", 128, 128, 22, 12, 84, 104);
            Add("particle-gold-04", "05_effects/particles/gold_04", 128, 128, 26, 12, 76, 104);
            Add("particle-leaf-01", "05_effects/particles/leaf_01", 128, 128, 18, 12, 92, 104);
            Add("particle-leaf-02", "05_effects/particles/leaf_02", 128, 128, 12, 15, 104, 99);
            Add("particle-leaf-03", "05_effects/particles/leaf_03", 128, 128, 32, 12, 63, 104);
            Add("particle-leaf-04", "05_effects/particles/leaf_04", 128, 128, 12, 25, 104, 79);
            Add("particle-leaf-05", "05_effects/particles/leaf_05", 128, 128, 22, 12, 84, 104);
            Add("particle-leaf-06", "05_effects/particles/leaf_06", 128, 128, 17, 12, 94, 104);
            Add("particle-leaf-07", "05_effects/particles/leaf_07", 128, 128, 33, 12, 61, 104);
            Add("particle-leaf-08", "05_effects/particles/leaf_08", 128, 128, 12, 30, 104, 69);
            Add("particle-magenta-01", "05_effects/particles/magenta_01", 128, 128, 12, 12, 103, 104);
            Add("particle-magenta-02", "05_effects/particles/magenta_02", 128, 128, 29, 12, 70, 104);
            Add("particle-magenta-03", "05_effects/particles/magenta_03", 128, 128, 18, 12, 91, 104);
            Add("particle-magenta-04", "05_effects/particles/magenta_04", 128, 128, 17, 12, 94, 104);
            Add("particle-magenta-05", "05_effects/particles/magenta_05", 128, 128, 12, 16, 104, 96);
            Add("particle-red-01", "05_effects/particles/red_01", 128, 128, 12, 12, 104, 104);
            Add("particle-red-02", "05_effects/particles/red_02", 128, 128, 25, 12, 78, 104);
            Add("particle-red-03", "05_effects/particles/red_03", 128, 128, 12, 15, 104, 99);
            Add("particle-red-04", "05_effects/particles/red_04", 128, 128, 20, 12, 87, 104);
            Add("particle-red-05", "05_effects/particles/red_05", 128, 128, 21, 12, 86, 104);
            Add("particle-red-06", "05_effects/particles/red_06", 128, 128, 25, 12, 78, 104);
            Add("particle-red-07", "05_effects/particles/red_07", 128, 128, 12, 14, 104, 101);
            Add("sparkle-01", "05_effects/particles/sparkle_01", 256, 256, 27, 26, 201, 204);
            Add("sparkle-02", "05_effects/particles/sparkle_02", 256, 256, 34, 26, 188, 204);
            Add("sparkle-03", "05_effects/particles/sparkle_03", 256, 256, 36, 26, 183, 204);
            Add("sparkle-04", "05_effects/particles/sparkle_04", 256, 256, 34, 26, 187, 204);
            Add("rocket-trail-horizontal", "05_effects/rocket_trail_horizontal", 512, 192, 16, 24, 480, 144);
            Add("rocket-trail-vertical", "05_effects/rocket_trail_vertical", 192, 512, 24, 16, 144, 480);
            Add("sparkle-cluster", "05_effects/sparkle_cluster", 512, 512, 41, 62, 430, 389);
            Add("bomb-round", "06_optional_extras/bomb_round_alternative", 512, 512, 78, 41, 355, 430);
            Add("bubble", "06_optional_extras/bubble", 512, 512, 47, 41, 418, 430);
            Add("color-pinwheel", "06_optional_extras/color_pinwheel_alternative", 512, 512, 41, 47, 430, 418);
            Add("ice", "06_optional_extras/ice_block", 512, 512, 49, 41, 414, 430);
            Add("ivy-stone", "06_optional_extras/ivy_stone_block", 512, 512, 41, 41, 430, 430);
            Add("vase", "06_optional_extras/porcelain_vase", 512, 512, 78, 41, 356, 430);
            Add("coin", "06_optional_extras/reward_coin", 512, 512, 45, 41, 422, 430);
            Add("key", "06_optional_extras/royal_key", 512, 512, 79, 41, 353, 430);
            Add("stone", "06_optional_extras/stone_block", 512, 512, 42, 41, 428, 430);
            Add("chest", "06_optional_extras/treasure_chest", 512, 512, 41, 42, 430, 428);
            Add("crate", "06_optional_extras/wooden_crate", 512, 512, 42, 41, 428, 430);
            return result;
        }
    }
}
