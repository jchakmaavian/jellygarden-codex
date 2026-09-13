// Makes every seam between two cells the same width.
//
// The gap between two cells is drawn by shrinking each tile inside its own
// cell (StoryBoardTheme.CellGap), so in WORLD units it is exactly the same on
// all four sides of every cell on the board. On SCREEN it was not, and the
// reason is that a cell was never a whole number of pixels:
//
//     AspectCamera sizes the view from the board's own width, so at nine
//     columns and a pitch of 1.17 it sets ortho to 9.36 * 852/393 / 2 + 1.5 =
//     11.65. An orthographic camera's size is half the view in world units, so
//     that is 426 / 11.65 = 36.58 pixels to the unit, and a cell is
//     1.17 * 36.58 = 42.79 pixels.
//
// Four fifths of a pixel per cell, accumulating: column 0 starts on a pixel
// boundary, column 1 starts a fifth past one, column 5 starts halfway between
// two. A 1.7-pixel seam therefore rasterises as one pixel under some columns
// and as three soft ones under others, and the rows drift on a phase of their
// own, so the horizontal seams do not match the vertical ones either. It is a
// sampling fault, not a geometry one: nothing about the board is off-centre.
//
// Two things fix it, and both are needed:
//
//   1. Round the cell to a whole number of pixels, by nudging the camera's
//      ortho. 42.79 -> 43 costs half a percent of the view — 11.65 to 11.59 —
//      which is under a pixel of framing and cannot be seen. Every cell now
//      steps a whole number of pixels from the last, so every cell in a row
//      shares one subpixel phase.
//
//   2. Put that shared phase ON a pixel boundary, by shifting the camera by
//      the fraction of a pixel the board's first cell edge misses by. The
//      shift is always less than one pixel, so it does not move the board and
//      does not fight StoryBoardCentre, which is what decides where the board
//      sits; this only tidies the last fraction of a pixel of that decision.
//
// The correction is made on the camera for the same reason StoryBoardCentre
// makes its own there: the field's x is driven by the kit's slide-in clip and
// three other things hang off the field's parent.
//
// Idempotent, and so safe to run every frame: once the ortho is snapped it
// snaps to itself, and once the edge is on a boundary the fraction it misses
// by is zero. It runs in LateUpdate so it lands after AspectCamera's coroutine,
// after LevelManager's own two ortho writes, and after StoryBoardCentre.
//
// The map is left alone — MapCamera drives that camera itself, and there is no
// grid of cells there to align.

using UnityEngine;

namespace JellyGarden.Scripts.Story
{
    [DisallowMultipleComponent]
    public sealed class StoryPixelSnap : MonoBehaviour
    {
        private static StoryPixelSnap _instance;

        /// <summary>How many screen pixels a cell came out. A test reads it.</summary>
        public static float LastCellPixels { get; private set; }

        /// <summary>The ortho this last set, or 0 if it has not run.</summary>
        public static float LastOrtho { get; private set; }

        /// <summary>
        /// The fraction of a pixel the board's first cell edge missed by, before
        /// the camera was shifted. A test reads it: it is what tells the second
        /// run that the first one worked.
        /// </summary>
        public static float LastShiftX { get; private set; }

        /// <summary>The same, down the screen.</summary>
        public static float LastShiftY { get; private set; }

        /// <summary>Called by StorySceneHooks on every scene load.</summary>
        public static void Install()
        {
            if (_instance != null) return;

            var go = new GameObject("~StoryPixelSnap");
            go.hideFlags = HideFlags.HideAndDontSave;
            StoryObjects.Persist(go);
            _instance = go.AddComponent<StoryPixelSnap>();
        }

        private void LateUpdate()
        {
            Apply();
        }

        /// <summary>
        /// Rounds the cell to whole pixels and puts the grid's edges on pixel
        /// boundaries. Cheap: a handful of floats and, once settled, no writes.
        /// </summary>
        public static void Apply()
        {
            LevelManager manager = LevelManager.THIS;
            if (manager == null) return;
            if (manager.gameStatus == GameState.Map) return;
            if (StorybookBoardArt.Enabled)
            {
                StorybookBoardLayout.FitCamera(manager);
                LastCellPixels = StorybookBoardLayout.CellPixels;
                LastOrtho = Camera.main == null ? 0 : Camera.main.orthographicSize;
                LastShiftX = LastShiftY = 0;
                return;
            }

            Camera camera = Camera.main;
            if (camera == null || !camera.orthographic) return;

            float pitch = manager.squareWidth;
            if (pitch <= 0f) return;
            if (camera.orthographicSize <= 0f) return;
            if (Screen.height <= 0 || Screen.width <= 0) return;

            // An orthographic camera's size is half the view in world units.
            float pixelsPerUnit = Screen.height / (2f * camera.orthographicSize);

            // 1. Prefer the nearest whole pixel to preserve AspectCamera's
            //    framing. Round down only if the outer frame would not fit.
            float cellPixels = Mathf.Round(pitch * pixelsPerUnit);
            // The gold outline now sits outside the grid. Reserve its padding
            // and two screen pixels per side before snapping a wide board.
            float columns = manager.maxCols + (StoryBoardTheme.UseStoryCellFill
                ? 2f * StoryBoardTheme.BoardOuterPadding : 0f);
            if (columns > 0f)
                cellPixels = Mathf.Min(cellPixels, Mathf.Floor((Screen.width - 4f) / columns));
            if (cellPixels < 1f) cellPixels = 1f;
            LastCellPixels = cellPixels;

            float ortho = Screen.height * pitch / (2f * cellPixels);
            if (Mathf.Abs(ortho - camera.orthographicSize) > 0.0001f)
            {
                camera.orthographicSize = ortho;
            }
            LastOrtho = ortho;

            // Recomputed from the snapped ortho rather than reused, so the
            // alignment below is measured against the view the player gets.
            pixelsPerUnit = Screen.height / (2f * camera.orthographicSize);

            // 2. The phase. Any one cell edge stands for all of them now that a
            //    cell is a whole number of pixels, so the first square the board
            //    made is reference enough.
            Square reference = FirstSquare(manager);
            if (reference == null) return;

            Vector3 at = camera.transform.position;
            Vector3 cell = reference.transform.position;

            // The cell's own left and bottom edges, in world units, and where
            // they land on the screen. Screen x grows with world x and shrinks
            // with the camera's, so moving the camera RIGHT by however far the
            // edge sits past a pixel boundary lands it on that boundary.
            float edgeX = cell.x - pitch * 0.5f;
            float edgeY = cell.y - manager.squareHeight * 0.5f;

            float screenX = (edgeX - at.x) * pixelsPerUnit + Screen.width * 0.5f;
            float screenY = (edgeY - at.y) * pixelsPerUnit + Screen.height * 0.5f;

            // The NEAREST boundary, and so a signed residual in [-0.5, 0.5].
            //
            // Floor was wrong, and wrong in the one way that shows: it returns
            // a distance in [0, 1), so an edge already on a boundary but a
            // float hair under it — which is what the frame after a successful
            // snap gives — reads as 0.99999 of a pixel out and the camera moves
            // a whole pixel right AGAIN, every frame, walking the board off to
            // the left for as long as the level is up. Rounding to nearest
            // makes that same hair read as -0.00001, so the snap settles.
            float residualX = (screenX - Mathf.Round(screenX)) / pixelsPerUnit;
            float residualY = (screenY - Mathf.Round(screenY)) / pixelsPerUnit;

            LastShiftX = residualX;
            LastShiftY = residualY;

            // A hundredth of a pixel is not worth a transform write, and the
            // deadband is the second guard against a walk: below it, nothing
            // moves at all.
            float deadband = 0.01f / pixelsPerUnit;
            if (Mathf.Abs(residualX) < deadband && Mathf.Abs(residualY) < deadband) return;

            camera.transform.position = new Vector3(at.x + residualX, at.y + residualY, at.z);
        }

        /// <summary>
        /// The first square the board actually drew. A hole is a square with
        /// its renderer switched off and it sits on the same grid as the rest,
        /// so any entry that exists will do — but the array can hold nulls
        /// while a board is being built.
        /// </summary>
        private static Square FirstSquare(LevelManager manager)
        {
            if (manager.squaresArray == null) return null;

            for (int i = 0; i < manager.squaresArray.Length; i++)
            {
                Square square = manager.squaresArray[i];
                if (square != null) return square;
            }

            return null;
        }
    }
}
