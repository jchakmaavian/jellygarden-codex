using System.Collections.Generic;
using JellyGarden.Scripts.GUI;
using UnityEngine;

namespace JellyGarden.Scripts.Juice
{
    /// <summary>
    /// Royal Throne's coin rules in one place (the user's request, 2026-10-05: 50-100 coins for each won level, spending a
    /// player enjoys, and earning and spending that balance). The win reward, the Continue offer, the lives refill, the
    /// booster shop and the in-level tool bar all read their numbers here; the inspector values they used before
    /// (FirstGems 5, FailedCost 12, CostIfRefill 12, packs at 5/6/11) no longer apply.
    ///
    /// The balance, per 10 levels of a typical player:
    ///   earned  10 wins x about 75 (50, plus 5 for each of about 5 moves left)                      about 750
    ///   spent   a 5-pack (225) and a 3-pack (150) of boosters, two first Continues (2 x 150),
    ///           one lives refill (100)                                                               about 775
    /// A player who uses a booster now and then and sometimes buys a Continue spends about what the levels pay;
    /// skilful wins (more moves left) bank the difference, and Area chests add a bonus on top.
    /// </summary>
    public static class RoyalEconomy
    {
        // ------------------------------------------------------------------ earning
        /// <summary>A new player's purse: enough to try a booster pack and a Continue in the first levels.</summary>
        public const int StartingCoins = 300;
        public const int WinBase = 50, WinPerMoveLeft = 5, WinMax = 100;

        /// <summary>A won level pays 50, plus 5 for every move left when its goals are met, up to 100.</summary>
        public static int WinCoins(int movesLeft) =>
            Mathf.Clamp(WinBase + WinPerMoveLeft * Mathf.Max(0, movesLeft), WinBase, WinMax);

        /// <summary>The reward for the level being played: its moves left, or one per 5 seconds left on a timed level.</summary>
        public static int WinCoins(LevelManager level) =>
            WinCoins(!level ? 0 : level.limitType == LIMIT.TIME ? level.Limit / 5 : level.Limit);

        // ------------------------------------------------------------------ spending
        /// <summary>Continue (+5 moves) after running out: 150, then 250, then 400 for each further one in the same attempt.</summary>
        public static int ContinueCost(int boughtThisAttempt) =>
            boughtThisAttempt <= 0 ? 150 : boughtThisAttempt == 1 ? 250 : 400;

        /// <summary>Refills every life.</summary>
        public const int LivesRefill = 100;

        /// <summary>The shop sells boosters in packs of 3, 5 and 10; bigger packs cost less per booster.</summary>
        public static readonly int[] PackCounts = { 3, 5, 10 };

        /// <summary>Pack prices: about 50 a booster, 60 for the Color Ball, 120 for extra moves or time.</summary>
        public static int[] PackPrices(BoostType type)
        {
            switch (type)
            {
                case BoostType.Colorful_bomb: return new[] { 180, 270, 480 };
                case BoostType.ExtraMoves: case BoostType.ExtraTime: return new[] { 360, 540, 960 };
                default: return new[] { 150, 225, 400 };
            }
        }

        /// <summary>The in-level tool bar sells packs of 3: Hammer and Jester for 150, Arrow and Cannon for 180.</summary>
        public static void ToolPack(int toolIndex, out int count, out int cost)
        {
            count = 3;
            cost = toolIndex == 1 || toolIndex == 2 ? 180 : 150;
        }

        /// <summary>Writes the pack table into the shop's products, in BoostType order.</summary>
        public static void Apply(List<BoostProduct> products)
        {
            if (products == null) return;
            for (int i = 0; i < products.Count; i++)
            {
                if (products[i] == null) continue;
                products[i].count = (int[])PackCounts.Clone();
                products[i].GemPrices = PackPrices((BoostType)i);
            }
        }
    }
}
