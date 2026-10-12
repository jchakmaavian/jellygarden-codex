using System;
using JellyGarden.Scripts.GUI;
using UnityEditor;
using UnityEngine;

namespace JellyGarden.Scripts.Story.Editor
{
    public static class StorybookBoardTests
    {
        static void Require(bool condition, string description)
        {
            if (!condition) throw new Exception(description);
        }
        public static void RunHeadless()
        {
            try
            {
                // Pieces and goal art are Royal Throne's now (RoyalPieceSkin,
                // RoyalVisualCatalog), so only the booster icons the popup and
                // the rail draw are checked here.
                foreach (BoostType boost in StoryBoosters.PreLevel)
                    Require(StoryBoosters.IconOf(boost) != null, "Missing pre-level booster " + boost);
                foreach (BoostType boost in global::RoyalThrone.ReferenceGameplay.ReferenceBoosterBar.Types)
                    Require(StoryBoosters.IconOf(boost) != null, "Missing rail tool " + boost);
                int layouts = 0;
                for (int level = 1; level <= 1208; level++)
                {
                    TextAsset asset = Resources.Load<TextAsset>("Levels/" + level);
                    Require(asset != null, "Missing level " + level);
                    string[] lines = asset.text.Replace("\r", "").Split('\n');
                    // Read the existing layout's grid rows, which are space-separated cell pairs.
                    int rows = 0, cols = 0;
                    foreach (string line in lines)
                    {
                        string[] cells = line.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (cells.Length == 0 || cells[0].Length != 2 || !char.IsDigit(cells[0][0]) || !char.IsDigit(cells[0][1])) continue;
                        rows++; cols = Mathf.Max(cols, cells.Length);
                    }
                    Require(rows > 0 && cols > 0, "Unreadable layout " + level);
                    // Royal Throne's camera fits any grid between the HUD and
                    // the rail (RoyalGameplayPresentation.FrameBoard), so a
                    // readable grid is what every level has to provide.
                    layouts++;
                }
                Debug.Log("[Storybook board] PASS: pre-level and rail booster icons, " + layouts + " readable level grids.");
                EditorApplication.Exit(0);
            }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }
    }
}
