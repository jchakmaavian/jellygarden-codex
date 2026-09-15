// How big a cell is, and everything that has to follow from it.
//
// The kit lays its board out at a square pitch of 0.82 world units — the value
// on the LevelManager in game.unity, not the 1.2 in the field's declaration —
// and its cell tile, Textures/Blocks/square.png, is 82 pixels at 100 pixels to
// the unit, so tiles meet exactly at that pitch and at no other.
//
// The cell can be made bigger, and the board then really does grow on screen,
// because AspectCamera frames the board with a padding written in world units
// rather than as a fraction:
//
//     ortho = fieldWidth * (height / width) / 2 + 1.5
//
// At nine columns the board spans 9 * pitch and the view spans 8 * pitch plus a
// fixed 1.384, so the padding stays the same size while the board grows into
// it: 93% of the screen's width at the kit's 0.82, 95% at 0.95, 98% at 1.17.
// The board is exactly the screen at 1.384 and wider than it past that.
//
// Four things are pinned to the pitch and are moved with it here, because a
// change to one alone is a visible fault rather than a smaller one:
//
//   - the cell tile, which gaps if it is not scaled with the pitch;
//   - the blocks, which are children of the square and so ride its scale;
//   - the gems, which are NOT children of the square — Square.GenItem parents
//     them to the field — and so take ItemScale;
//   - StoryBoardTheme's drawn ground, whose sprite is built at 8 pixels to the
//     pitch so one texel-block is one cell.
//
// Nothing here touches an animation, a particle or a clip: the kit's own
// prefabs are instantiated unchanged and scaled after the fact, which is what
// lets a bigger cell carry the kit's effects at the same size relative to it.

using UnityEngine;

namespace JellyGarden.Scripts.Story
{
    public static class StoryBoardScale
    {
        /// <summary>The pitch the kit ships, from the LevelManager in game.unity.</summary>
        public const float KitPitch = 0.82f;

        /// <summary>
        /// The pitch the story plays at. 1.17 is a 43% wider cell and just over
        /// twice the area, and leaves the nine-column board — every board but 68
        /// of the 1208 — covering 98% of a 393-point screen: about four points
        /// of margin a side, which is as close to the edge as a board can be
        /// asked to sit. The board IS the screen at 1.384.
        ///
        /// The view grows with it — AspectCamera's ortho goes 8.61 to 11.65 —
        /// so the board is 12.9 units tall in a 23.3-unit view and the space
        /// above and below it grows too. That space is the level HUD's, and
        /// nothing here can spend it on cell size: the width is what binds.
        /// </summary>
        public const float Pitch = 1.17f;

        /// <summary>
        /// The gem's share of its cell, unchanged by the pitch: an item sprite
        /// paints 114 of its 128 pixels, so this * Factor draws
        /// 1.14 * ItemShare / KitPitch of a cell whatever the pitch is.
        ///
        /// 0.57 is 79% of a cell. It was 0.66 — 92% — which was right while a
        /// cell was a bare patch of ground, and is too big now that the ground
        /// is a rounded tile inset by StoryBoardTheme.CellGap: at 92% the gem
        /// ran into the tile's rounded corners and the tile read as a ring
        /// around the gem rather than as the thing the gem sits on. At 79% the
        /// blue shows on every side of the gem.
        /// </summary>
        public const float ItemShare = 0.57f;

        /// <summary>Everything pinned to the pitch is multiplied by this.</summary>
        public static float Factor
        {
            get { return Pitch / KitPitch; }
        }

        /// <summary>The gem's localScale, in the kit's own units.</summary>
        /// <remarks>
        /// The Storybook share is .60, not .64. It is the gem's width as a
        /// fraction of its CELL, so the remainder is the tile showing around it:
        /// at a 52px cell .64 drew a 33.3px gem with 9.0px of tile a side, and
        /// .60 draws 31.2px with 10.0px. The tile is what separates one gem from
        /// the next now that the seam between tiles is down to .78px, so the
        /// padding inside the cell is doing the work the gap used to.
        /// </remarks>
        public static float ItemScale
        {
            get { return (StorybookBoardArt.Enabled ? .60f : ItemShare) * Factor; }
        }

        /// <summary>
        /// Makes a block, an obstacle or anything else parented to a Square
        /// fill its cell.
        ///
        /// The kit instantiates these at a WORLD position and then calls
        /// SetParent(square.transform) — the one-argument overload, which is
        /// worldPositionStays: true. Unity keeps the object's world scale by
        /// dividing the parent's out of the child's localScale, so a prefab
        /// authored at scale 1 lands under a Square scaled by Factor with a
        /// localScale of 1 / Factor. The block's art is 82px at 100 to the
        /// unit — KitPitch exactly, one whole cell at the kit's pitch — so it
        /// came out 0.82 of a world unit inside a 1.17 cell: 70% of the cell,
        /// an ice patch floating in the middle of the gem it covers rather
        /// than the cell's own lid.
        ///
        /// Setting localScale back to one puts the art at KitPitch * Factor,
        /// which is Pitch: the block is exactly its cell, whatever the pitch
        /// is. Position, sprite, sorting order, material and every animation
        /// on the prefab are untouched — DestroyBlock's BrickRotate plays on
        /// the same transform and simply plays bigger.
        /// </summary>
        public static void FitToCell(Transform t)
        {
            if (t == null) return;
            t.localScale = Vector3.one;
        }

        /// <summary>
        /// Puts the pitch on the LevelManager before it lays a board out.
        /// Called from GenerateLevel, so a level loaded by any path — play, the
        /// playtest driver, a screen test — gets the same board.
        /// </summary>
        public static void Apply(LevelManager manager)
        {
            if (manager == null) return;

            manager.squareWidth = Pitch;
            manager.squareHeight = Pitch;
        }
    }
}
