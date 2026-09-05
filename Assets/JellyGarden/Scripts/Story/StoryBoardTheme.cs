// Dresses the board for the arc the player is in.
//
// Two things change, both of them safe:
//
//   the backdrop behind the board — Background picks one of five shipped
//   pictures by currentLevel / 20, which with 1209 levels means one picture for
//   ninety per cent of the game; this picks by arc instead
//
//   the tint on the board's own cells — a colour on the existing sprites, so
//   every animation, particle and material stays exactly as it was
//
// The gems are deliberately left alone; see the note in StoryTheme.

using JellyGarden.Scripts.GUI;
using UnityEngine;
using UnityEngine.UI;

namespace JellyGarden.Scripts.Story
{
    [DisallowMultipleComponent]
    public sealed class StoryBoardTheme : MonoBehaviour
    {
        private static StoryBoardTheme _instance;

        private int _themedLevel = -1;

        /// <summary>Called by StorySceneHooks on every scene load; a no-op when this scene has no map or board.</summary>
        public static void Install()
        {
            if (_instance != null) return;

            var go = new GameObject("~StoryBoardTheme");
            go.hideFlags = HideFlags.HideAndDontSave;
            StoryObjects.Persist(go);
            _instance = go.AddComponent<StoryBoardTheme>();
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

        /// <summary>Applies the current level's arc theme. Cheap and idempotent.</summary>
        public void Apply()
        {
            if (LevelManager.THIS == null) return;

            int levelNumber = LevelManager.THIS.currentLevel;
            ArcTheme theme = StoryTheme.ForLevel(levelNumber);
            if (theme == null) return;

            ApplyBackdrop(theme);
            ApplyCellFill(theme);
            _themedLevel = levelNumber;
        }

        /// <summary>
        /// Overrides Background's level-bucket choice with the arc's. A story
        /// backdrop is used when one exists; otherwise the kit's own picture that
        /// suits the arc's setting.
        /// </summary>
        private static void ApplyBackdrop(ArcTheme theme)
        {
            var background = Object.FindFirstObjectByType<Background>(FindObjectsInactive.Include);
            if (background == null) return;

            var image = background.GetComponent<Image>();
            if (image == null) return;

            Sprite story = StoryArt.Load(theme.BoardBackdrop);
            if (story != null)
            {
                image.sprite = story;
                return;
            }

            Sprite[] pictures = background.pictures;
            if (pictures == null || pictures.Length == 0) return;

            int index = Mathf.Clamp(theme.KitBackdropIndex, 0, pictures.Length - 1);
            image.sprite = pictures[index];
        }

        /// <summary>The name of the fill object, so it is found again rather than doubled.</summary>
        private const string FillName = "~StoryCellFill";

        /// <summary>
        /// Drawn in FRONT of the cell — nearer the camera than its z of 0.
        ///
        /// It was behind, and that was the whole reason the board ground never
        /// looked like anything. Textures/Blocks/square.png is pure black at
        /// 50% alpha (square_2.png at 65%): the kit's cell is a dark scrim, not
        /// a tile. A colour laid behind a half-opaque black square comes back
        /// halved — the castle's sand at 216,188,140 arrives as 108,94,70 — so
        /// no value put there can ever be a light ground, which is what Royal
        /// Match's boards have.
        ///
        /// In front, the fill IS the ground and the scrim under it contributes
        /// nothing. Everything the kit draws over a cell still draws over both:
        /// the outline is sorting order 1 against this one's 0, and blockers sit
        /// at z -0.5 against this one's -0.05.
        ///
        /// The same fact is why the old ApplySquareTint never did anything
        /// visible, and why it is gone: multiplying a pure black sprite by a
        /// colour leaves it black. Its job is now this one's — the swapped
        /// sprite is white, so the colour on the renderer IS the ground.
        /// </summary>
        private const float FillDepth = -0.05f;

        private static Sprite _fillSprite;

        /// <summary>
        /// A white square the exact size of the kit's cell: 82 pixels at 100 to
        /// the unit, which is what Textures/Blocks/square.png imports as. Built
        /// once and shared by every cell on every board.
        /// </summary>
        private static Sprite FillSprite()
        {
            if (_fillSprite != null) return _fillSprite;

            var texture = new Texture2D(8, 8, TextureFormat.RGBA32, false);
            texture.hideFlags = HideFlags.HideAndDontSave;
            var pixels = new Color32[8 * 8];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(255, 255, 255, 255);
            texture.SetPixels32(pixels);
            texture.Apply();

            // 8 pixels drawn at 9.756 to the unit is 0.82 units — the same square
            // the cell sprite covers. A flat colour needs no more texels than that.
            _fillSprite = Sprite.Create(texture, new Rect(0f, 0f, 8f, 8f),
                new Vector2(0.5f, 0.5f), 8f / 0.82f);
            _fillSprite.name = "StoryCellFill";
            _fillSprite.hideFlags = HideFlags.HideAndDontSave;
            return _fillSprite;
        }

        /// <summary>
        /// Gives the board a ground of its own.
        ///
        /// The kit's cell sprite is flat black at alpha 0.50, and its alternate
        /// at 0.65 — a dark checker with nothing behind it but the backdrop
        /// picture, which is why the board reads as an outline around a hole.
        /// This lays the arc's colour under each cell. The cell, its sprite, its
        /// material, its sorting order and its Animation component are all
        /// untouched; the fill is a child that draws behind it.
        ///
        /// Note for anyone wondering why the
        /// board never changed colour with the arc: multiplying a black sprite
        /// by a tint leaves it black. The tint is kept — it is correct the day
        /// the cell art stops being black — but this is what carries the colour.
        /// </summary>
        private static void ApplyCellFill(ArcTheme theme)
        {
            if (LevelManager.THIS == null || LevelManager.THIS.squaresArray == null) return;
            if (!theme.HasBoardFill) return;

            int made = 0;

            foreach (Square square in LevelManager.THIS.squaresArray)
            {
                if (square == null) continue;

                var cell = square.GetComponent<SpriteRenderer>();

                // A hole in the board is a square like any other with its
                // renderer switched off — CreateSquare does that for
                // SquareTypes.NONE, and the level's silhouette is exactly the
                // set of cells left drawing. Following the renderer rather than
                // the array is what keeps the ground out of the bounding-box
                // corners nobody plays on.
                if (cell == null || !cell.enabled || cell.sprite == null) continue;

                if (cell.sprite != FillSprite())
                {
                    cell.sprite = FillSprite();
                    made++;
                }

                // The kit alternates square.png and square_2.png to make its
                // checker — identical black tiles at 50% and 65% alpha — and
                // that difference cannot survive the sprite being replaced. The
                // checker is rebuilt from the cell's own place on the grid
                // instead of remembered from the sprite it used to wear, which
                // is what a checker actually is and needs nothing kept between
                // one board and the next.
                bool dark = ((square.row + square.col) & 1) == 1;
                cell.color = dark ? theme.BoardFillDark : theme.BoardFill;
            }

            if (made > 0 && !_reportedFill)
            {
                _reportedFill = true;
                Debug.Log(string.Format(
                    "[Story] board ground: {0} cell(s) now {1}.",
                    made, theme.BoardFill));
            }
        }

        private static bool _reportedFill;

        private void LateUpdate()
        {
            // The board is rebuilt on retry and on a new level without always
            // firing OnLevelLoaded, so a cheap check catches the rest.
            if (LevelManager.THIS == null) return;
            if (LevelManager.THIS.currentLevel == _themedLevel) return;
            Apply();
        }
    }
}
