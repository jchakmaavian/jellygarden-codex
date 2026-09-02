// Opens a story screen in Play mode, so it can be looked at.
//
// A screen is not verified until it has been seen in the Game view: an editor
// test checks strings, and a rect of negative height passes every one of them
// while drawing nothing. Getting there by tapping the map in the Game view is
// unreliable — synthetic clicks on a Unity Game view mostly do not land — and
// the alternative that has been reached for before is a temporary early-return
// hook in a runtime script, which then rides along into a commit.
//
// These menu items are that hook, made permanent and harmless: they do nothing
// outside Play mode, they are Editor-only, and they call the same entry points
// the game calls.

using UnityEditor;
using UnityEngine;

namespace JellyGarden.Scripts.Story.Editor
{
    public static class StoryScreenPreview
    {
        private const string Menu = "JellyGarden/Story/Preview/";

        [MenuItem(Menu + "Open Area Screen (area 1)", true)]
        [MenuItem(Menu + "Open Area Screen (area 6)", true)]
        [MenuItem(Menu + "Grant 3 Stars Here", true)]
        [MenuItem(Menu + "Reset Progress", true)]
        private static bool OnlyInPlayMode()
        {
            return Application.isPlaying;
        }

        /// <summary>Area 1 — four tasks, the shortest area the story writes.</summary>
        [MenuItem(Menu + "Open Area Screen (area 1)")]
        private static void OpenAreaOne()
        {
            Open(1);
        }

        /// <summary>Area 6 — seven tasks, enough rows to scroll the list.</summary>
        [MenuItem(Menu + "Open Area Screen (area 6)")]
        private static void OpenAreaSix()
        {
            Open(6);
        }

        /// <summary>
        /// Stars for the area on screen, so the affordable and unaffordable
        /// states of a Do It row can both be looked at without playing levels.
        /// </summary>
        [MenuItem(Menu + "Grant 3 Stars Here")]
        private static void GrantStars()
        {
            int areaId = AreaScreen.IsOpen ? AreaScreen.Instance.AreaId : StoryProgress.CurrentArea;
            StoryProgress.AddStar(areaId, 3);
            if (AreaScreen.IsOpen) AreaScreen.Instance.Refresh();
            Debug.Log("[Story] preview: area " + areaId + " now holds " +
                      StoryProgress.StarsIn(areaId) + " star(s).");
        }

        [MenuItem(Menu + "Reset Progress")]
        private static void Reset()
        {
            StoryProgress.ResetAll();
            if (AreaScreen.IsOpen) AreaScreen.Instance.Refresh();
            Debug.Log("[Story] preview: progress reset.");
        }

        /// <summary>
        /// Straight to AreaScreen.Open rather than through
        /// StoryFlow.HandleMapNodeClicked: the flow refuses a locked area, and
        /// unlocking is derived from the area before it being finished, so
        /// previewing area 6 would otherwise mean playing the first five.
        /// </summary>
        private static void Open(int areaId)
        {
            AreaScreen.Open(areaId);
        }
    }
}
