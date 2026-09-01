// Asks the map to redraw its lock icons after progress changes.
//
// LevelsMap.Reset() also re-places the character and the camera, so it is only
// worth calling when something actually changed — hence a request rather than a
// per-frame poll.

using JellyGarden.Scripts.MapScripts;
using UnityEngine;

namespace JellyGarden.Scripts.Story
{
    public static class StoryMapRefresh
    {
        public static void Request()
        {
            if (LevelsMap._instance == null) return;

            // Reset() reads the progress manager, which is already the story's,
            // so this is enough to unlock the next node on screen.
            LevelsMap._instance.Reset();
        }
    }
}
