// Puts the board in the middle of the screen.
//
// `LevelManager.GenerateLevel` places the field at
//
//     fieldPos = new Vector3(-maxCols / 2.75f, maxRows / 2.75f, -10)
//
// and then lays the squares out from there at `squareWidth` apart. Those two
// numbers only agree for ONE board width: the field's centre comes out at
//
//     -maxCols / 2.75 + (maxCols - 1) * squareWidth / 2
//
// which with the shipped `squareWidth` of 0.82 is zero at nine columns and
// nowhere else. The game's 1140 nine-wide boards are centred to within a third
// of a pixel; the other 68 are not. A five-wide board — level 149 is one — sits
// 0.178 units left of centre, which at 393 points is nine pixels: an 85-point
// margin on its left against 103 on its right.
//
// The kit ships the same fault on eight of its own hundred levels (3, 5, 6, 8,
// 11, 12, 24, 62), so it is not something a generator introduced and not
// something a level file can fix.
//
// The correction is made on the CAMERA, not on the board. Three other things
// hang off the same parent as the field, so moving that moves them too; the
// field's own x is driven every frame of the slide-in by the legacy clip
// `AnimateField` builds, so writing it afterwards would land the board and then
// jerk it sideways. Moving the camera moves nothing but the view: the level's
// UI is on canvases that follow the camera, the backdrop with them, and the
// board's own effects are children of the board.

using UnityEngine;

namespace JellyGarden.Scripts.Story
{
    [DisallowMultipleComponent]
    public sealed class StoryBoardCentre : MonoBehaviour
    {
        /// <summary>The kit's own divisor, from `LevelManager.GenerateLevel`.</summary>
        private const float FieldDivisor = 2.75f;

        private static StoryBoardCentre _instance;

        /// <summary>
        /// Where the last board's middle sat, in world units, before the camera
        /// was moved. Zero means it was already centred. A test reads it.
        /// </summary>
        public static float LastOffset { get; private set; }

        /// <summary>Called by StorySceneHooks on every scene load.</summary>
        public static void Install()
        {
            if (_instance != null) return;

            var go = new GameObject("~StoryBoardCentre");
            go.hideFlags = HideFlags.HideAndDontSave;
            StoryObjects.Persist(go);
            _instance = go.AddComponent<StoryBoardCentre>();
        }

        private void OnEnable()
        {
            LevelManager.OnLevelLoaded += Apply;
            LevelManager.OnStartPlay += Apply;
        }

        private void OnDisable()
        {
            LevelManager.OnLevelLoaded -= Apply;
            LevelManager.OnStartPlay -= Apply;
        }

        /// <summary>
        /// Aims the camera at the middle of the grid. Cheap and idempotent — it
        /// is called again on OnStartPlay because `EnableMap(false)` puts the
        /// camera back on the origin on the way into every level.
        /// </summary>
        public void Apply()
        {
            LevelManager manager = LevelManager.THIS;
            if (manager == null) return;

            Camera camera = Camera.main;
            if (camera == null) return;

            float offset = OffsetFor(manager.maxCols, manager.squareWidth);
            LastOffset = offset;

            Vector3 at = camera.transform.position;
            camera.transform.position = new Vector3(offset, at.y, at.z);
        }

        /// <summary>
        /// How far the middle of a grid of this many columns lands from the
        /// screen's middle, in world units. Written from the kit's own two
        /// numbers rather than from a measured table, so a level with a square
        /// size of its own is centred by the same rule.
        ///
        /// It is the GRID that is centred, not the cells that happen to be
        /// filled: a board with an empty left column is a board the level file
        /// drew that way, and re-centring on what is left would move the 1140
        /// boards that are already right.
        /// </summary>
        public static float OffsetFor(int columns, float squareWidth)
        {
            if (columns <= 0) return 0f;
            return -columns / FieldDivisor + (columns - 1) * squareWidth * 0.5f;
        }
    }
}
