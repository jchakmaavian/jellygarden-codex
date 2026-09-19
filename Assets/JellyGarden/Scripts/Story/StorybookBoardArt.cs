using System.Collections.Generic;
using JellyGarden.Scripts.GUI;
using JellyGarden.Scripts.Targets;
using UnityEngine;

namespace JellyGarden.Scripts.Story
{
    // Approved royal mockup sprites, kept in the kit's existing colour order.
    public static class StorybookBoardArt
    {
        public static bool Enabled => StorybookUi.Enabled;
        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();
        static readonly string[] Colours = { "red", "orange", "purple", "blue", "green", "yellow" };
        public static Sprite Get(string key)
        {
            if (!Cache.TryGetValue(key, out Sprite sprite))
            {
                string royal = key;
                switch (key)
                {
                    case "red": royal = "book"; break;
                    case "orange": royal = "coin"; break;
                    case "purple": royal = "gem"; break;
                    case "blue": royal = "shield"; break;
                    case "green": royal = "leaf"; break;
                    case "yellow": royal = "crown"; break;
                    case "area-bomb": royal = "tnt"; break;
                    case "rainbow-orb": royal = "orb"; break;
                    case "recolour": royal = "bomb"; break;
                }
                sprite = Resources.Load<Sprite>("Story/RoyalBoard/" + royal);
                Cache[key] = sprite = sprite != null ? sprite : Resources.Load<Sprite>("Story/TreasureHunt/Board/" + key);
            }
            return sprite;
        }
        public static Sprite Piece(int colour) => Get(Colours[Mathf.Clamp(colour, 0, 5)]);
        public static Sprite Booster(BoostType type)
        {
            switch (type)
            {
                case BoostType.ExtraMoves: return Get("moves");
                case BoostType.ExtraTime: return Get("time");
                case BoostType.Hand: return Get("glove");
                case BoostType.Bomb: return Get("hammer");
                case BoostType.Random_color: return Get("recolour");
                case BoostType.Stripes: return Get("rocket");
                case BoostType.Packages: return Get("area-bomb");
                case BoostType.Colorful_bomb: return Get("rainbow-orb");
                default: return null;
            }
        }
        public static Sprite Goal(TargetObject target)
        {
            if (!Enabled) return target.icon;
            if (target.type == Target.COLLECT) return Piece(target.color);
            if (target.type == Target.SCORE) return Get("score");
            if (target.type == Target.BLOCKS) return Get("ice");
            string name = target.icon == null ? "" : target.icon.name;
            return name == "ingredient_02" || name == "ring" ? Get("ring") : Get("locket");
        }
        public static void Apply(Item item)
        {
            if (!Enabled || Piece(0) == null) return;
            // GenColor also recovers colour with Array.IndexOf(items, sprite).
            // Replace the array before Start, including on every cascade spawn.
            item.items = new Sprite[6];
            item.stripedItems = new List<StripedItem>();
            item.packageItems = new Sprite[6];
            Sprite rocket = Get("rocket");
            Sprite areaBomb = Get("area-bomb");
            Sprite rainbow = Get("rainbow-orb");

            for (int i = 0; i < 6; i++)
            {
                item.items[i] = Piece(i);
                item.stripedItems.Add(new StripedItem { horizontal = rocket, vertical = rocket });
                item.packageItems[i] = areaBomb;
            }
            item.bombItems = new[] { rainbow };
            item.ingredientItems = new[] { Get("locket"), Get("ring") };
            item.gameObject.AddComponent<StorybookRocketFacing>();
        }
    }
}
