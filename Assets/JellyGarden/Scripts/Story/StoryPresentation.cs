// Decides what the player sees when an area is finished, and remembers it if
// the moment has to wait.
//
// Rule from the story bible: every fifth area is an arc beat and gets a real
// scene; every other area gets a three-second clue card. Neither ever fires on a
// level win — only on the last repair of an area.

using System;
using UnityEngine;

namespace JellyGarden.Scripts.Story
{
    public static class StoryPresentation
    {
        /// <summary>An area whose payoff was owed but could not be played yet.</summary>
        private static int _pendingArea;

        /// <summary>
        /// Plays the payoff for a finished area: the arc beat's cutscene if it has
        /// one, otherwise the clue card.
        /// </summary>
        public static void PlayAreaPayoff(int areaId, Action onDone)
        {
            if (StoryCampaign.IsTreasureHunt)
            {
                StoryArea current = StoryDatabase.GetArea(areaId);
                if (current == null || !TreasureHuntPresentation.Play(current.PayoffScene, areaId, onDone))
                    if (onDone != null) onDone();
                return;
            }
            StoryArea area = StoryDatabase.GetArea(areaId);
            if (area == null)
            {
                if (onDone != null) onDone();
                return;
            }

            Action finish = () =>
            {
                _pendingArea = 0;
                if (onDone != null) onDone();
            };

            // Seen scenes do not replay themselves; the area screen offers a
            // Replay button for anyone who wants them again.
            if (area.IsArcBeat && CutsceneRunner.Has(areaId) &&
                !StoryProgress.IsCutsceneSeen(areaId))
            {
                CutsceneRunner.Play(areaId, () => ClueThenFinish(areaId, finish));
                return;
            }

            ClueThenFinish(areaId, finish);
        }

        /// <summary>
        /// The clue is the area's own line and lands even on a beat area, after
        /// the scene — the beat is what happened, the clue is what Leo is left
        /// holding.
        /// </summary>
        private static void ClueThenFinish(int areaId, Action finish)
        {
            if (StoryProgress.IsClueSeen(areaId))
            {
                finish();
                return;
            }

            ClueCard.Show(areaId, finish);
        }

        /// <summary>
        /// Records that an area's payoff is owed. Used when the area finishes
        /// somewhere the card cannot be shown, so it can play at the next natural
        /// pause instead of being lost.
        /// </summary>
        public static void Defer(int areaId)
        {
            _pendingArea = areaId;
        }

        /// <summary>
        /// Plays a deferred payoff if one is owed for this area. Returns true when
        /// it took over — the caller's own continuation is then the callback.
        /// </summary>
        public static bool PlayPending(int areaId, Action onDone)
        {
            if (TreasureHuntPresentation.PlayPending(areaId, onDone)) return true;
            if (_pendingArea == 0 || _pendingArea != areaId) return false;

            int pending = _pendingArea;
            _pendingArea = 0;
            PlayAreaPayoff(pending, onDone);
            return true;
        }

        public static bool IsBusy
        {
            get { return ClueCard.IsShowing || CutsceneRunner.IsPlaying || TreasureHuntPresentation.IsPlaying; }
        }
    }
}
