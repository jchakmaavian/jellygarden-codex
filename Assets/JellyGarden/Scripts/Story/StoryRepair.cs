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

            StorySfx.PlayRepair(StoryProgress.IsAreaComplete(areaId));
            AreaSceneView.NotifyTaskCompleted(areaId, taskIndex);

            // The line the story wrote for this repair. Arc 1 authors one per
            // task; later arcs do not, and a toast with nothing in it is worse
            // than no toast.
            if (!string.IsNullOrEmpty(task.OnComplete))
                StoryToast.Show(task.OnComplete);

            if (onChanged != null) onChanged();

            bool areaFinished = StoryProgress.IsAreaComplete(areaId);
            if (areaFinished)
                OnAreaFinished(areaId);

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
