// The free-game rules from TECH.md, in one switch.
//
// "Completely free — no ads, no IAP, no revenue." Without revenue the five-lives
// system is pure friction: it exists to sell refills, and there is nothing to
// sell. So retry is instant and unlimited, no ad is ever requested, the shops
// are never shown, and boosters are earned rather than bought.
//
// Nothing is deleted to achieve this. The kit's ad controllers, IAP integration
// and shop screens all remain in the repo; they are simply never reached. Six
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
        /// rocket, bomb, shuffle — mapped onto the kit's own boost types, plus
        /// Royal Throne's four in-level tools on the bottom rail.
        /// </summary>
        private static readonly BoostType[] Grantable =
        {
            BoostType.Hand,          // Hammer: removes one tile
            BoostType.Stripes,       // Rocket: clears a line
            BoostType.Bomb,          // Bomb: clears 3x3
            BoostType.Random_color,  // Shuffle's nearest equivalent in the kit
            BoostType.Packages,      // TNT: a pre-level booster on the level popup
            BoostType.Colorful_bomb, // The ball of colour, likewise
            BoostType.RoyalHammer,   // Rail tool: clears one cell
            BoostType.RoyalArrow,    // Rail tool: clears the tapped row
            BoostType.RoyalCannon,   // Rail tool: clears the tapped column
            BoostType.RoyalJester    // Rail tool: shuffles the board
        };

        /// <summary>Royal Throne's in-level tools, in rail order.</summary>
        private static readonly BoostType[] RailTools =
        {
            BoostType.RoyalHammer,
            BoostType.RoyalArrow,
            BoostType.RoyalCannon,
            BoostType.RoyalJester
        };

        /// <summary>Two hammers and a shuffle on first run, so the mechanic is discoverable.</summary>
        private const string StarterGrantKey = "Story.StarterBoostersGranted";

        /// <summary>
        /// The three pre-level boosters the level popup offers, granted once so
        /// the row is not three locked slots on a fresh save. Its own key, so a
        /// save made before the popup existed still receives them.
        /// </summary>
        private const string StarterPreBoostKey = "Story.StarterPreBoostersGranted";

        /// <summary>
        /// The rail's four tools, granted once. Its own key, so a save that
        /// already had its starter boosters still receives them.
        /// </summary>
        private const string StarterToolsKey = "Story.StarterToolsGranted";

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

            // Checked on its own key first, so a save past its starter grant
            // still receives the rail's tools once.
            GrantStarterTools();

            if (PlayerPrefs.GetInt(StarterGrantKey, 0) != 0) return;

            PlayerPrefs.SetInt(StarterGrantKey, 1);
            Grant(BoostType.Hand, 2);
            Grant(BoostType.Random_color, 1);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Two hammers and one of each other rail tool, once, so every tool on
        /// the rail can be tried when it unlocks; nothing is for sale.
        /// </summary>
        public static void GrantStarterTools()
        {
            if (!Enabled) return;
            if (PlayerPrefs.GetInt(StarterToolsKey, 0) != 0) return;

            PlayerPrefs.SetInt(StarterToolsKey, 1);
            for (int i = 0; i < RailTools.Length; i++)
                Grant(RailTools[i], RailTools[i] == BoostType.RoyalHammer ? 2 : 1);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// One of each pre-level booster, once. Without this the popup's row
        /// would open locked on every slot and the player would never learn what
        /// it is for — and, since nothing is for sale, a locked slot has no way
        /// to become unlocked except by winning.
        /// </summary>
        public static void GrantStarterPreBoosters()
        {
            if (!Enabled) return;
            if (PlayerPrefs.GetInt(StarterPreBoostKey, 0) != 0) return;

            PlayerPrefs.SetInt(StarterPreBoostKey, 1);
            for (int i = 0; i < StoryBoosters.PreLevel.Length; i++)
                Grant(StoryBoosters.PreLevel[i], 1);
            PlayerPrefs.Save();
        }

        private static void Grant(BoostType boost, int count)
        {
            // Read and written under the key StoryBoosters.Owned and the rail use.
            int owned = global::RoyalThrone.Testing.RoyalSave.GetInt(StoryBoosters.KeyOf(boost), 0);

            // BuyBoost is the kit's own setter; the price argument is unused by
            // it, which is why granting costs nothing rather than being faked.
            // A QA session writes through RoyalSave instead, so it never lands
            // in the player's real counts.
            if (InitScript.Instance != null && !global::RoyalThrone.Testing.RoyalSave.Testing)
                InitScript.Instance.BuyBoost(boost, 0, owned + count);
            else
                global::RoyalThrone.Testing.RoyalSave.SetInt(StoryBoosters.KeyOf(boost), owned + count);
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

        /// <summary>
        /// Whether the kit's "rate this game" panel may open. Never.
        ///
        /// Two reasons, and the second is a bug rather than a preference.
        /// LevelManager asks for it on entering GameState.Map, every
        /// ShowRateEvery levels — and this game has no map: StoryShell puts it
        /// away and the room is the entry point. So the panel opens onto a
        /// screen it was not designed for, and what the player gets is its
        /// scrim: a black Image at alpha 0.87 over the whole screen, with
        /// nothing on it to dismiss. It was found by photographing the sixth
        /// level of a session and measuring the frame — every check about the
        /// board passed, and the picture was black.
        /// </summary>
        public static bool RatePromptAllowed
        {
            get { return !Enabled; }
        }

        /// <summary>
        /// Whether the kit's "five more moves for twelve gems" banner may open
        /// when a level runs out of moves. Never: it is a shop in the one place
        /// the kit is surest a player will pay, and there is nothing to pay
        /// with. MenuManager.ShowPreFailed carries the level straight to its end
        /// panel instead — see the marked block there, which is the sixth and
        /// last of these early-returns.
        /// </summary>
        public static bool PreFailOfferAllowed
        {
            get { return !Enabled; }
        }
    }
}
