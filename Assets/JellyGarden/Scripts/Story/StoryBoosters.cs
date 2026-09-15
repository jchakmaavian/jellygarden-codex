// The three boosters a player may take into a level, and how a choice made on
// the level popup reaches the board.
//
// Royal Match offers exactly three pre-level boosters and the kit already has
// them: LevelManager.SetPreBoosts reads BoostStriped, BoostPackage and
// BoostColorfullBomb after the board is generated and turns that many random
// items into stripes, packages or colour bombs. Those three are therefore the
// row — not a story invention, and not the in-level boosters (hammer, bomb,
// hand), which are used on a board rather than before one.
//
// The popup runs in the map scene, where LevelManager.THIS does not exist yet,
// so the choice is held here and applied on LevelManager.OnLevelLoaded — which
// fires from ReGenLevel before the RegenMatches coroutine reaches SetPreBoosts.
// The kit then spends the booster itself, through its own InitScript.SpendBoost.
// Nothing in LevelManager is edited: three public fields are written from
// outside, exactly as the kit's own BoostIcon writes them.

using System;
using System.Collections.Generic;
using JellyGarden.Scripts.GUI;
using UnityEngine;

namespace JellyGarden.Scripts.Story
{
    public static class StoryBoosters
    {
        /// <summary>
        /// The row, in the order docs/royal-match/10-level-start-popup.jpg shows
        /// it: the rocket, the TNT and the ball of colour.
        /// </summary>
        public static readonly BoostType[] PreLevel =
        {
            BoostType.Stripes,
            BoostType.Packages,
            BoostType.Colorful_bomb
        };

        /// <summary>
        /// How many items each booster converts, matching the kit's own
        /// BoostIcon.ActivateBoost so a booster does the same thing whether it
        /// was picked here or in the kit's booster row.
        /// </summary>
        public static int AmountOf(BoostType boost)
        {
            return boost == BoostType.Colorful_bomb ? 1 : 5;
        }

        /// <summary>
        /// How many of a booster the player owns. Null is PlayerPrefs, which is
        /// where the kit keeps them; a test replaces it so the row can be driven
        /// at chosen counts without writing the player's save.
        /// </summary>
        public static Func<BoostType, int> OwnedSource;

        public static int Owned(BoostType boost)
        {
            if (OwnedSource != null) return OwnedSource(boost);
            return PlayerPrefs.GetInt("" + boost, 0);
        }

        #region The selection

        private static readonly List<BoostType> _selected = new List<BoostType>();

        public static bool IsSelected(BoostType boost)
        {
            return _selected.Contains(boost);
        }

        /// <summary>
        /// Toggles a booster and reports whether it is now selected. A booster
        /// the player does not own cannot be selected: the slot is locked, not
        /// a prompt to buy one, because nothing in this game is for sale.
        /// </summary>
        public static bool Toggle(BoostType boost)
        {
            if (_selected.Remove(boost)) return false;
            if (Owned(boost) <= 0) return false;

            _selected.Add(boost);
            return true;
        }

        /// <summary>What the next level will start with, in row order.</summary>
        public static List<BoostType> Selection()
        {
            var chosen = new List<BoostType>();
            for (int i = 0; i < PreLevel.Length; i++)
                if (_selected.Contains(PreLevel[i])) chosen.Add(PreLevel[i]);
            return chosen;
        }

        public static void ClearSelection()
        {
            _selected.Clear();
        }

        #endregion

        #region Icons

        public static Sprite IconOf(BoostType boost)
        {
            // The three PLAYABLE boosters keep the Royal plates in every style:
            // they were picked on the level popup and the Storybook board art
            // reads as the gem itself rather than as a button. The rest of the
            // boosters have no Royal plate at all, so they still come from the
            // board set when Storybook is on.
            switch (boost)
            {
                case BoostType.Stripes: return StoryUi.LoadSprite("booster_stripes");
                case BoostType.Packages: return StoryUi.LoadSprite("booster_packages");
                case BoostType.Colorful_bomb: return StoryUi.LoadSprite("booster_colorbomb");
            }

            if (StorybookBoardArt.Enabled) return StorybookBoardArt.Booster(boost);
            return null;
        }

        /// <summary>The kit's own locked-slot plate, so a locked slot is art
        /// rather than a blank tile.</summary>
        public static Sprite LockedSprite
        {
            get { return StoryUi.LoadSprite("booster_locked"); }
        }

        #endregion

        #region Handing the selection to the board

        private static bool _hooked;

        public static void Install()
        {
            if (_hooked) return;
            _hooked = true;

            LevelManager.OnLevelLoaded += Apply;
        }

        /// <summary>
        /// Writes the selection onto the board that has just been generated.
        /// Clears it afterwards, so the "no more matches" regeneration — which
        /// fires OnLevelLoaded again — cannot hand out a second set.
        /// </summary>
        private static void Apply()
        {
            if (_selected.Count == 0) return;

            LevelManager level = LevelManager.THIS;
            if (level == null) return;

            List<BoostType> chosen = Selection();
            for (int i = 0; i < chosen.Count; i++)
            {
                BoostType boost = chosen[i];
                int amount = AmountOf(boost);

                if (boost == BoostType.Stripes) level.BoostStriped = amount;
                else if (boost == BoostType.Packages) level.BoostPackage = amount;
                else if (boost == BoostType.Colorful_bomb) level.BoostColorfullBomb = amount;
            }

            ClearSelection();

            var names = new List<string>();
            for (int i = 0; i < chosen.Count; i++) names.Add(chosen[i].ToString());
            Debug.Log("[Story] Boosters into the level: " + string.Join(", ", names.ToArray()) + ".");
        }

        #endregion
    }
}
