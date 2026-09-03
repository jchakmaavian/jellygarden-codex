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
            ApplySquareTint(theme);
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
        /// Drawn behind the cell, so it must be further from the camera than the
        /// cell (z 0) and nearer than the backdrop, which is UI on a screen-space
        /// camera canvas a hundred units out. A tenth of a unit does both.
        /// </summary>
        private const float FillDepth = 0.1f;

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
        /// Note for anyone reading ApplySquareTint below and wondering why the
        /// board never changed colour with the arc: multiplying a black sprite
        /// by a tint leaves it black. The tint is kept — it is correct the day
        /// the cell art stops being black — but this is what carries the colour.
        /// </summary>
        private static void ApplyCellFill(ArcTheme theme)
        {
            if (LevelManager.THIS == null || LevelManager.THIS.squaresArray == null) return;

            foreach (Square square in LevelManager.THIS.squaresArray)
            {
                if (square == null) continue;

                var cell = square.GetComponent<SpriteRenderer>();
                Transform existing = square.transform.Find(FillName);

                // A hole in the board is a square like any other with its
                // renderer switched off — CreateSquare does that for
                // SquareTypes.NONE, and the level's silhouette is exactly the
                // set of cells left drawing. So the fill follows the cell's own
                // renderer rather than the array, which is every cell in the
                // bounding box including the corners no one plays on.
                bool wanted = theme.HasBoardFill && cell != null && cell.enabled;

                if (!wanted)
                {
                    if (existing != null) existing.gameObject.SetActive(false);
                    continue;
                }

                SpriteRenderer fill;
                if (existing != null)
                {
                    existing.gameObject.SetActive(true);
                    fill = existing.GetComponent<SpriteRenderer>();
                    if (fill == null) continue;
                }
                else
                {
                    var go = new GameObject(FillName);
                    go.transform.SetParent(square.transform, false);
                    go.transform.localPosition = new Vector3(0f, 0f, FillDepth);
                    fill = go.AddComponent<SpriteRenderer>();
                    fill.sprite = FillSprite();

                    // The cell's own layer and order, so the fill lands in the
                    // same sorting group and is separated from it by depth
                    // alone. Anything the kit draws over the cell — outline at
                    // order 1, blockers at z -0.5 — still draws over both.
                    fill.sortingLayerID = cell.sortingLayerID;
                    fill.sortingOrder = cell.sortingOrder;
                }

                fill.color = theme.BoardFill;
            }
        }

        /// <summary>
        /// Tints the board's cells. A tint only — the sprite, the material, the
        /// sorting and every animation on the square are untouched, and the gems
        /// sitting on top keep their own colour.
        /// </summary>
        private static void ApplySquareTint(ArcTheme theme)
        {
            if (LevelManager.THIS == null || LevelManager.THIS.squaresArray == null) return;

            foreach (Square square in LevelManager.THIS.squaresArray)
            {
                if (square == null) continue;

                var renderer = square.GetComponent<SpriteRenderer>();
                if (renderer == null) continue;

                // Only the cell itself. A blocker sitting on it is a story object
                // in its own right and keeps its authored colour.
                renderer.color = theme.SquareTint;
            }
        }

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
