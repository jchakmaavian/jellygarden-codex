// King Leo's monetisation switches, in one place.
//
// TECH.md shipped the game "completely free — no ads, no IAP, no revenue", with
// lives, shops and the fail offer all switched off. Royal Throne's coin economy
// (Juice/RoyalEconomy) replaces that: a win pays coins, and coins buy boosters,
// tools, a lives refill and the "So Close!" Continue. Lives are spent again and
// refill on Royal Throne's schedule (InitScript.UpdateLives).
//
// What stays off: ads, the rate prompt and real money. Enabled is the offline
// switch RoyalOfflineMode reads, so the kit's ad controllers, Facebook and IAP
// never start, and RoyalStore's coin packs stay unavailable.
//
// The gates themselves are unchanged: the marked early-returns in InitScript and
// MenuManager still ask these properties.

using JellyGarden.Scripts.GUI;
using UnityEngine;

namespace JellyGarden.Scripts.Story
{
    public static class StoryFreeGame
    {
        /// <summary>
        /// The offline switch. True keeps ads, real-money purchases and every
        /// network service off (RoyalOfflineMode.ServicesEnabled is its inverse);
        /// false hands the kit its ads and IAP back.
        /// </summary>
        public const bool Enabled = true;

        /// <summary>Royal Throne's in-level tools, in rail order.</summary>
        private static readonly BoostType[] RailTools =
        {
            BoostType.RoyalHammer,
            BoostType.RoyalArrow,
            BoostType.RoyalCannon,
            BoostType.RoyalJester
        };

        /// <summary>
        /// The pre-level boosters the level popup offers, granted once so the
        /// row is not three empty slots on a fresh save. Its own key, so a save
        /// made before the popup existed still receives them.
        /// </summary>
        private const string StarterPreBoostKey = "Story.StarterPreBoostersGranted";

        /// <summary>
        /// The rail's four tools, granted once. Its own key, so a save that
        /// already had its starter boosters still receives them.
        /// </summary>
        private const string StarterToolsKey = "Story.StarterToolsGranted";

        /// <summary>
        /// A won level grants no booster any more: under the coin economy the win
        /// pays coins (LevelManager.CommitWin, RoyalEconomy.WinCoins), and boosters
        /// are bought with them. Kept so StoryFlow's win path still calls one place.
        /// </summary>
        public static void GrantBoosterForWin(int levelNumber)
        {
        }

        /// <summary>
        /// The one-time starter grants at boot. The kit's in-level boosters
        /// (Hand, Bomb, Random_color) are no longer granted: nothing in the
        /// level can use them, since the rail carries Royal Throne's tools.
        /// </summary>
        public static void GrantStarterBoosters()
        {
            GrantStarterTools();
        }

        /// <summary>
        /// Two hammers and one of each other rail tool, once, so every tool on
        /// the rail can be tried when it unlocks. More are bought with coins
        /// (ReferenceBoosterBar's purchase panel, RoyalEconomy.ToolPack).
        /// </summary>
        public static void GrantStarterTools()
        {
            if (PlayerPrefs.GetInt(StarterToolsKey, 0) != 0) return;

            PlayerPrefs.SetInt(StarterToolsKey, 1);
            for (int i = 0; i < RailTools.Length; i++)
                Grant(RailTools[i], RailTools[i] == BoostType.RoyalHammer ? 2 : 1);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// One of each pre-level booster, once, so the player learns what the
        /// popup's row is for. More are bought with coins (StoryBoosters.Buy).
        /// </summary>
        public static void GrantStarterPreBoosters()
        {
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
        /// Whether a life is taken for playing. Yes: lives are on, as in Royal
        /// Throne, and refill over time or for coins (RoyalEconomy.LivesRefill).
        /// </summary>
        public static bool ShouldSpendLife
        {
            get { return true; }
        }

        /// <summary>Whether an ad may be requested. Never while offline; the app makes no network calls.</summary>
        public static bool AdsAllowed
        {
            get { return !Enabled; }
        }

        /// <summary>
        /// Whether a shop screen may open: the booster, lives and coin shops.
        /// Yes; they sell for coins. Real-money packs stay unavailable
        /// (RoyalStore) while Enabled.
        /// </summary>
        public static bool ShopsAllowed
        {
            get { return true; }
        }

        /// <summary>
        /// Whether the kit's "rate this game" panel may open. Never while offline.
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
        /// Whether the "So Close!" banner may offer five more moves when a level
        /// runs out of moves. Yes: the Continue costs coins
        /// (RoyalEconomy.ContinueCost, 150 then 250 then 400 in one attempt).
        /// </summary>
        public static bool PreFailOfferAllowed
        {
            get { return true; }
        }
    }
}
