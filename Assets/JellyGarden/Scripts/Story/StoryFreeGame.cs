// The free-game rules from TECH.md, in one switch.
//
// "Completely free — no ads, no IAP, no revenue." Without revenue the five-lives
// system is pure friction: it exists to sell refills, and there is nothing to
// sell. So retry is instant and unlimited, no ad is ever requested, the shops
// are never shown, and boosters are earned rather than bought.
//
// Nothing is deleted to achieve this. The kit's ad controllers, IAP integration
// and shop screens all remain in the repo; they are simply never reached. Four
// marked early-returns in InitScript and MenuManager are the whole of it, and
// flipping Enabled to false restores the kit's behaviour exactly.

using JellyGarden.Scripts.GUI;
using UnityEngine;

namespace JellyGarden.Scripts.Story
{
    public static class StoryFreeGame
    {
        /// <summary>
        /// The single switch. True ships the game TECH.md describes; false hands
        /// the kit its monetisation back without a code change anywhere else.
        /// </summary>
        public const bool Enabled = true;

        /// <summary>
        /// Boosters the story grants. Four, matching TECH.md's list — hammer,
        /// rocket, bomb, shuffle — mapped onto the kit's own boost types.
        /// </summary>
        private static readonly BoostType[] Grantable =
        {
            BoostType.Hand,          // Hammer: removes one tile
            BoostType.Stripes,       // Rocket: clears a line
            BoostType.Bomb,          // Bomb: clears 3x3
            BoostType.Random_color   // Shuffle's nearest equivalent in the kit
        };

        /// <summary>Two hammers and a shuffle on first run, so the mechanic is discoverable.</summary>
        private const string StarterGrantKey = "Story.StarterBoostersGranted";

        /// <summary>
        /// One random booster per level won. The player already paid for it by
        /// winning; there is no second price.
        /// </summary>
        public static void GrantBoosterForWin(int levelNumber)
        {
            if (!Enabled) return;

            // Seeded on the level number so a replay cannot farm boosters, and so
            // what you get is the same on any device.
            // Fully qualified: the kit has a JellyGarden.Scripts.System namespace,
            // so a bare System.Random resolves into it and fails.
            var rng = new global::System.Random(levelNumber * 7919);
            BoostType boost = Grantable[rng.Next(Grantable.Length)];
            Grant(boost, 1);
        }

        public static void GrantStarterBoosters()
        {
            if (!Enabled) return;
            if (PlayerPrefs.GetInt(StarterGrantKey, 0) != 0) return;

            PlayerPrefs.SetInt(StarterGrantKey, 1);
            Grant(BoostType.Hand, 2);
            Grant(BoostType.Random_color, 1);
            PlayerPrefs.Save();
        }

        private static void Grant(BoostType boost, int count)
        {
            int owned = PlayerPrefs.GetInt("" + boost, 0);

            // BuyBoost is the kit's own setter; the price argument is unused by
            // it, which is why granting costs nothing rather than being faked.
            if (InitScript.Instance != null)
                InitScript.Instance.BuyBoost(boost, 0, owned + count);
            else
                PlayerPrefs.SetInt("" + boost, owned + count);
        }

        /// <summary>
        /// Whether a life should be taken for playing. Never, while the game is
        /// free — a lost level costs a retry tap and nothing else.
        /// </summary>
        public static bool ShouldSpendLife
        {
            get { return !Enabled; }
        }

        /// <summary>Whether an ad may be requested. Never; the app makes no network calls.</summary>
        public static bool AdsAllowed
        {
            get { return !Enabled; }
        }

        /// <summary>Whether a shop screen may open. Never; there is nothing for sale.</summary>
        public static bool ShopsAllowed
        {
            get { return !Enabled; }
        }
    }
}
