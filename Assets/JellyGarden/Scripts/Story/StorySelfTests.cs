// Whether a launch wants the self-tests to run themselves.
//
// StoryPowerupTest opens level 1 and plays it, and StoryShellTest buys 25
// repairs across areas 1-5 — 25 repair animations and the arc beats between
// them. Both were armed by Debug.isDebugBuild alone, so EVERY development
// build did all of that on launch, before the player could touch anything.
// That is right for a verification run and wrong for opening the game, and
// the shell walk also writes to the save on its way past.
//
// So they are opt-in now. A plain launch opens the game; a launch that asks
// for them gets them:
//
//     tools/run_player.sh -storyselftest
//
// The Editor's Preview menu calls RunNow() directly and is unaffected, which
// is the normal way to run either of them while working.

namespace JellyGarden.Scripts.Story
{
    public static class StorySelfTests
    {
        private const string Flag = "-storyselftest";

        private static bool _asked;
        private static bool _read;

        /// <summary>Whether this launch asked for the self-tests to run.</summary>
        public static bool Requested
        {
            get
            {
                if (_read) return _asked;
                _read = true;

                foreach (string arg in global::System.Environment.GetCommandLineArgs())
                {
                    if (arg != Flag) continue;
                    _asked = true;
                    break;
                }

                return _asked;
            }
        }
    }
}
