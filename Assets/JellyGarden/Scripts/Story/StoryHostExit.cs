// Leaving the game and handing the screen back.
//
// King Leo ships two ways and "exit" means a different thing in each:
//
//   embedded   — the game is a UnityFramework inside the VMP app. Unloading
//                returns the screen to the host, which is listening for
//                unityDidUnload to put its own window back. Quitting here would
//                kill VMP along with the game.
//   standalone — the game IS the app. There is nothing to return to, so
//                unloading would leave the player staring at a blank screen.
//
// Nothing in Unity reports which one is running, so it is inferred from the
// bundle identifier: standalone builds are com.vmp.kingleo, and embedded the
// main bundle belongs to whatever host embedded the framework.
//
// The default when the check cannot decide is Unload, because that is the
// shipping path and its failure mode is survivable — a standalone build shows a
// blank screen, where Quit inside VMP would take the VPN down with it.

using UnityEngine;

namespace JellyGarden.Scripts.Story
{
    public static class StoryHostExit
    {
        /// <summary>
        /// The standalone player's bundle id, as set by StoryBuild.BuildIos.
        /// Any other identifier means some host embedded the framework.
        /// </summary>
        public const string StandaloneBundleId = "com.vmp.kingleo";

        /// <summary>Whether the game is running inside a host app rather than on its own.</summary>
        public static bool IsEmbedded
        {
            get { return Application.identifier != StandaloneBundleId; }
        }

        /// <summary>
        /// Leaves the game. Embedded this unloads the engine and the host takes
        /// the screen back; standalone it quits the app.
        /// </summary>
        public static void Exit()
        {
            if (IsEmbedded)
            {
                Debug.Log("[Story] Exit: unloading Unity, returning to host app.");
                Application.Unload();
                return;
            }

            Debug.Log("[Story] Exit: standalone build, quitting.");
            Application.Quit();
        }
    }
}
