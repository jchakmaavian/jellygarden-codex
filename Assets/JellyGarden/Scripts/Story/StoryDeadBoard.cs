// Retired with the Royal Throne level port (plan Task 4.5).
//
// This was the last resort behind the kit's reshuffle: once a second it asked
// the kit's matcher whether any swap on an idle board could score, and after
// three "no" answers it called `LevelManager.NoMatches()` itself. The kit's AI
// scan that it covered for is gone from the level now. Royal Throne's board
// owns a dead board end to end (`Juice/RoyalShuffle`, guarded by
// `Juice/RoyalBoardIntegrity`): it rearranges the plain gems into a board with
// at least one move. A second watchdog calling `NoMatches()` on top of that
// would start a second shuffle over the first.
//
// The class stays only so its two callers keep compiling until they drop it:
// `StorySceneHooks` calls Install, which installs nothing, and `StoryPlaytest`
// reads Reshuffles, which stays zero.

namespace JellyGarden.Scripts.Story
{
    public static class StoryDeadBoard
    {
        /// <summary>Reshuffles this watchdog called. Always zero: Royal Throne's RoyalShuffle owns dead boards.</summary>
        public static int Reshuffles { get { return 0; } }

        /// <summary>Installs nothing; kept for StorySceneHooks until it drops the call.</summary>
        public static void Install()
        {
        }
    }
}
