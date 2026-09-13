using System;
using System.Collections.Generic;
using JellyGarden.Scripts.GUI;
using JellyGarden.Scripts.Targets;
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
                var pieces = new HashSet<Sprite>();
                for (int colour = 0; colour < 6; colour++)
                {
                    Sprite sprite = StorybookBoardArt.Piece(colour);
                    Require(sprite != null && pieces.Add(sprite), "Missing or duplicate colour " + colour);
                    var goal = new TargetObject { type = Target.COLLECT, color = colour };
                    Require(goal.DeepCopy().icon == sprite && goal.icon == null, "Goal mapping mutated source or changed colour " + colour);
                }
                foreach (BoostType boost in Enum.GetValues(typeof(BoostType)))
                    if (boost != BoostType.None) Require(StoryBoosters.IconOf(boost) != null, "Missing booster " + boost);
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
                    foreach (Rect safe in new[] { new Rect(0, 0, 393, 852), new Rect(0, 0, 320, 568), new Rect(0, 34, 393, 759) })
                    {
                        Rect field = StorybookBoardLayout.FieldRect(safe);
                        float pitch = StorybookBoardLayout.FitPitch(safe, cols, rows);
                        Require((cols + .52f) * pitch <= field.width + .01f && (rows + .52f) * pitch <= field.height + .01f,
                            "Board overlaps fixed controls: " + level + " at " + safe.size);
                        layouts++;
                    }
                }
                Debug.Log("[Storybook board] PASS: six distinct colours, immutable goal mappings, all eight powers, " + layouts + " level/safe-area fits.");
                EditorApplication.Exit(0);
            }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }
    }
}
