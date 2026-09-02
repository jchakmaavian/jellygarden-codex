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

using JellyGarden.Scripts.GUI;
using UnityEditor;
using UnityEngine;

namespace JellyGarden.Scripts.Story.Editor
{
    public static class StoryScreenPreview
    {
        private const string Menu = "JellyGarden/Story/Preview/";

    [MenuItem(Menu + "Open Home Screen", true)]
        [MenuItem(Menu + "Open Areas List", true)]
        [MenuItem(Menu + "Open Areas List (locked area 100)", true)]
        [MenuItem(Menu + "Open Area Screen (area 1)", true)]
        [MenuItem(Menu + "Open Area Screen (area 3)", true)]
        [MenuItem(Menu + "Open Area Screen (area 6)", true)]
        [MenuItem(Menu + "Close The Level", true)]
        [MenuItem(Menu + "Grant 3 Stars Here", true)]
        [MenuItem(Menu + "Buy The Next Repair", true)]
        [MenuItem(Menu + "Reset Progress", true)]
        private static bool OnlyInPlayMode()
        {
            return Application.isPlaying;
        }

        /// <summary>
        /// Home, on whichever room the player is restoring. Closes the area
        /// screen first: home sorts below it, so leaving it open would put a
        /// panel over the screen being looked at.
        /// </summary>
        [MenuItem(Menu + "Open Home Screen")]
        private static void OpenHome()
        {
            AreaScreen.Close();
            StoryHomeScreen.Open();
        }

        /// <summary>
        /// The list of all 150, scrolled to the room the player is in. Closes the
        /// area screen first, which sorts above it.
        /// </summary>
        [MenuItem(Menu + "Open Areas List")]
        private static void OpenAreasList()
        {
            StoryAreasList.Open();
        }

        /// <summary>
        /// The same list parked on an area the player cannot reach, which is the
        /// only way to look at a locked card: the list opens on the player's own
        /// frontier and 149 of the 150 cards are below it.
        /// </summary>
        [MenuItem(Menu + "Open Areas List (locked area 100)")]
        private static void OpenAreasListLocked()
        {
            StoryAreasList.Open(100);
        }

        /// <summary>Area 1 — four tasks, the shortest area the story writes.</summary>
        [MenuItem(Menu + "Open Area Screen (area 1)")]
        private static void OpenAreaOne()
        {
            Open(1);
        }

        /// <summary>
        /// Area 3 — the first area drawn from a single restored plate rather
        /// than a state per repair, and the only one in the shipping band whose
        /// rows carry a Polish and a Restock glyph.
        /// </summary>
        [MenuItem(Menu + "Open Area Screen (area 3)")]
        private static void OpenAreaThree()
        {
            Open(3);
        }

        /// <summary>Area 6 — seven tasks, enough rows to scroll the list.</summary>
        [MenuItem(Menu + "Open Area Screen (area 6)")]
        private static void OpenAreaSix()
        {
            Open(6);
        }

        /// <summary>
        /// Comes back from a level, the way finishing one does. StoryPowerupTest
        /// launches level 1 on every play session and never finishes it, so a
        /// session that has run it is still "in a level" as far as the story is
        /// concerned — StoryFlow.PlayingArea is set and the board's gameStatus is
        /// not Map. Anything that hides itself during a level, the nav bar first
        /// among them, then stays hidden over a shell screen opened on top of it.
        ///
        /// HandleLevelComplete is the game's own way out, so this is not a second
        /// path to keep in step. It only falls back to the two kit calls when the
        /// story was not the one that launched the level.
        /// </summary>
        [MenuItem(Menu + "Close The Level")]
        private static void CloseLevel()
        {
            if (!StoryFlow.HandleLevelComplete())
            {
                if (LevelManager.THIS != null) LevelManager.THIS.gameStatus = GameState.Map;
                if (MenuManager.Instance != null) MenuManager.Instance.ShowMenuPlay(false);
            }

            Debug.Log("[Story] preview: left the level.");
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
            if (StoryHomeScreen.IsOpen) StoryHomeScreen.Instance.Refresh();
            Debug.Log("[Story] preview: area " + areaId + " now holds " +
                      StoryProgress.StarsIn(areaId) + " star(s).");
        }

        /// <summary>
        /// Presses the Do It button on the next repair of the area on screen, so
        /// the repair — the panel closing, the room taking the screen, the work
        /// playing on it — can be watched in the Game view. The BUTTON is pressed
        /// rather than StoryRepair called: the order guard and the price check
        /// live behind it, and a preview that reaches past them is previewing
        /// something the player cannot do.
        /// </summary>
        [MenuItem(Menu + "Buy The Next Repair")]
        private static void BuyNextRepair()
        {
            if (!AreaScreen.IsOpen)
            {
                Debug.Log("[Story] preview: no area screen open to repair on.");
                return;
            }

            AreaScreen screen = AreaScreen.Instance;
            StoryTask next = StoryProgress.NextTask(screen.AreaId);
            if (next == null)
            {
                Debug.Log("[Story] preview: area " + screen.AreaId + " is fully restored.");
                return;
            }

            for (int i = 0; i < screen.RowCount; i++)
            {
                if (screen.RowTaskIndex(i) != next.Index) continue;

                UnityEngine.UI.Button button = screen.RepairButton(i);
                if (button == null) break;

                Debug.Log(string.Format(
                    "[Story] preview: pressing Do It on '{0}' ({1} star(s), {2} held).",
                    next.Label, next.Stars, StoryProgress.StarsIn(screen.AreaId)));
                button.onClick.Invoke();
                return;
            }

            Debug.Log("[Story] preview: no row found for the next repair.");
        }

        [MenuItem(Menu + "Reset Progress")]
        private static void Reset()
        {
            StoryProgress.ResetAll();
            if (AreaScreen.IsOpen) AreaScreen.Instance.Refresh();
            if (StoryHomeScreen.IsOpen) StoryHomeScreen.Instance.Refresh();
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
