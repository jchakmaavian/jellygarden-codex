// Grimm's sabotage: the moment the board starts fighting back.
//
// GAMEPLAY.md: areas 001-018 are static neglect. At area 019 Grimm opens the
// sluice gates and obstacles begin acting on their own. Everything after that
// reads as the villain being active rather than the designer being cruel — but
// only if the game says so once, at the moment it changes.
//
// The obstacle itself is the kit's own thriving block, already wired with its
// prefab and its spread animation, so the beat costs no new mechanic. What is
// added here is the line that explains it.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace JellyGarden.Scripts.Story
{
    public static class StorySabotage
    {
        private const string SeenKey = "Story.SabotageIntroSeen";

        /// <summary>
        /// Plays the one-time line that attributes the new obstacle to Grimm, on
        /// the level the plan marks as the beat. Returns true when it took over.
        /// </summary>
        public static bool PlayIntroIfDue(int levelNumber, Action onDone)
        {
            StoryLevelEntry entry = StoryLevelPlan.Get(levelNumber);
            if (entry == null || !entry.SabotageBeat) return false;
            if (PlayerPrefs.GetInt(SeenKey, 0) != 0) return false;

            PlayerPrefs.SetInt(SeenKey, 1);
            PlayerPrefs.Save();

            StoryArea area = StoryDatabase.GetArea(entry.AreaId);
            string room = area != null ? area.Name.ToLowerInvariant() : "the room";

            // The beat fires from StoryFlow.LaunchLevel: the shell has already
            // come down for the level and the board has not been loaded yet, so
            // there is nothing on screen for the card to be spoken over. Without
            // the room behind it the one moment the game names Grimm out loud is
            // a cream card on black — which is what level 157 photographed.
            StoryDialogueRoom.Show(entry.AreaId);

            DialogueSystem.Play(new List<DialogueLine>
            {
                new DialogueLine("Alfred",
                    "The sluice gates above " + room + " had been opened. " +
                    "Not broken. Opened."),
                new DialogueLine("Leo",
                    "Then someone is still here, undoing my work as fast as I do it.")
            }, () =>
            {
                StoryDialogueRoom.Hide();
                if (onDone != null) onDone();
            });

            return true;
        }

        /// <summary>Does this level carry a self-acting obstacle?</summary>
        public static bool IsSabotaged(int levelNumber)
        {
            StoryLevelEntry entry = StoryLevelPlan.Get(levelNumber);
            return entry != null && entry.Sabotage;
        }

        /// <summary>The first area where anything on the board acts on its own.</summary>
        public static int FirstSabotageArea
        {
            get
            {
                StoryLevelPlan.Load();
                return StoryLevelPlan.FirstSabotageArea;
            }
        }

        /// <summary>Test hook — lets the intro be seen again.</summary>
        public static void ResetIntro()
        {
            PlayerPrefs.DeleteKey(SeenKey);
        }
    }
}
