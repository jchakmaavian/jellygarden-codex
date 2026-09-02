// Spending stars on a repair task — the moment the story actually advances.
//
// The rule from the story bible: the clue fires on the last repair task of an
// area, never on a level win. That is enforced here, in one place.

using System;
using JellyGarden.Scripts.GUI;
using UnityEngine;

namespace JellyGarden.Scripts.Story
{
    public static class StoryRepair
    {
        /// <summary>
        /// Pays for a task and plays what follows it. Returns false when the
        /// player cannot afford it or it is already done.
        /// </summary>
        public static bool Perform(int areaId, int taskIndex, Action onChanged)
        {
            StoryArea area = StoryDatabase.GetArea(areaId);
            if (area == null) return false;

            StoryTask task = StoryProgress.FindTask(area, taskIndex);
            if (task == null) return false;

            if (!StoryProgress.CompleteTask(areaId, taskIndex))
                return false;

            // How many repairs the room has had once this one lands — the state
            // the cutscene reveals. The reveal itself belongs to the cutscene, on
            // its own beat: firing it here changed the room while the player was
            // still looking at the "before".
            int stateAfter = StoryProgress.TasksCompletedIn(areaId);

            // A repair bought on the area page plays on that page's own room: the
            // task list goes away, the room takes the screen, and the work happens
            // on the picture the star was spent on. Bought anywhere else — the
            // map, a test — there is no room to play over and the cutscene brings
            // its own screen.
            AreaScreen screen = AreaScreen.IsOpen && AreaScreen.Instance != null
                                && AreaScreen.Instance.AreaId == areaId
                ? AreaScreen.Instance
                : null;

            RectTransform room = null;
            if (screen != null)
            {
                room = screen.RoomRect;
                screen.HideForRepair();
            }

            // The repair plays out before anything else happens. Everything that
            // follows — the refreshed screen, the clue, the next area — waits for
            // it, so the player is never reading a task list over a cutscene.
            RepairCutscene.Play(areaId, task, stateAfter, room, () =>
            {
                // The panel comes back before anything else can close it: a screen
                // left hidden is a screen with no X on it.
                if (screen != null) screen.ShowAfterRepair();

                if (onChanged != null) onChanged();

                // The home screen is showing the same room and the same counts.
                // Its art reveals itself (every live AreaSceneView does), but the
                // numbers on its area card are read, not watched.
                if (StoryHomeScreen.IsOpen && StoryHomeScreen.Instance != null)
                    StoryHomeScreen.Instance.Refresh();

                if (StoryProgress.IsAreaComplete(areaId))
                    OnAreaFinished(areaId);
            });

            return true;
        }

        private static void OnAreaFinished(int areaId)
        {
            StoryArea area = StoryDatabase.GetArea(areaId);
            if (area == null) return;

            Debug.Log(string.Format(
                "[Story] Area {0} ({1}) restored. Area {2} unlocks.",
                area.Id, area.Name, area.Id + 1));

            // Close the task list first: the clue and the beat are the payoff, and
            // they should not play behind a screen of struck-through chores.
            AreaScreen.Close();

            StoryPresentation.PlayAreaPayoff(areaId, () =>
            {
                if (MenuManager.Instance != null)
                    MenuManager.Instance.ShowMenuPlay(false);
                StoryMapRefresh.Request();
            });
        }
    }
}
