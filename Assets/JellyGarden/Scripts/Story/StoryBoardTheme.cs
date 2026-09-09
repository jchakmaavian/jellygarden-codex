// Dresses the board for the area the player is restoring.
//
// Two things change, both of them safe:
//
//   the backdrop behind the board — Background picks one of five shipped
//   pictures by currentLevel / 20; this shows the area's saved repair state,
//   just like the room on the home screen
//
//   the tint on the board's own cells — a colour on the existing sprites, so
//   every animation, particle and material stays exactly as it was
//
// The gems are deliberately left alone; see the note in StoryTheme.

using System.Collections.Generic;
using JellyGarden.Scripts.GUI;
using UnityEngine;
using UnityEngine.UI;

namespace JellyGarden.Scripts.Story
{
    [DisallowMultipleComponent]
    public sealed class StoryBoardTheme : MonoBehaviour
    {
        /// <summary>
        /// Whether the kit draws its outline around the board's silhouette.
        ///
        /// `LevelManager.GenerateOutline` rings every playable cell that has a
        /// hole beside it with border_01..03 — a hard bevelled edge that was
        /// drawn for a board sitting on a flat colour. The story's board is a
        /// shade over the arc's room, and a bevel around it makes it a cut-out
        /// panel again rather than a dimmed pane. The outline is not deleted,
        /// only not built: the sprites, the code that draws them and their
        /// sorting orders are untouched, so turning this back on restores the
        /// kit's look exactly.
        /// </summary>
        public const bool DrawKitOutline = false;

        /// <summary>
        /// Whether the story puts its own picture behind the level.
        ///
        /// It does. This is the whole page — the room the player is restoring,
        /// at its current repair state, behind everything the level draws. It
        /// is the same picture the home screen and the area page show, and it
        /// is what makes a level look like the place it is in rather than like
        /// one of five kit pictures indexed by level number.
        ///
        /// Kept as a switch because it was briefly turned off, and because the
        /// kit's path is still the fallback when an area has no art.
        /// </summary>
        public const bool UseStoryBackdrop = true;

        /// <summary>
        /// Whether the story gives the CELLS a ground of its own.
        ///
        /// This is the board itself, not the page behind it. The story swapped
        /// each cell's sprite for a white square and drew it in the arc's
        /// colour; that is off, so a cell keeps exactly what the kit gives it —
        /// Textures/Blocks/square.png and square_2.png, flat black at alpha
        /// 0.50 and 0.65, alternated by CreateSquare into the kit's checker.
        /// The board is the kit's dark scrim over the story's room again.
        ///
        /// Nothing is deleted: ApplyCellFill, the generated fill sprite and the
        /// arc's boardFill in themes.json are all still here and still built,
        /// so turning this on restores the story's ground exactly.
        /// </summary>
        public const bool UseStoryCellFill = false;

        /// <summary>
        /// The two alphas the board's checker is drawn at.
        ///
        /// The kit's cell is pure black at 0.50 and its alternate at 0.65, and a
        /// tint cannot change either — a colour multiplies a sprite, so it can
        /// only take alpha down, never up. The cell's sprite is therefore
        /// swapped for the flat white square and drawn in black at these two
        /// values instead: same two-tone, same parity, same neutral black, at
        /// the depth chosen here rather than the depth the tiles were painted.
        ///
        /// 0.55 / 0.65 is a shallower step than the kit's own — the light
        /// square comes down toward the dark one, so the checker reads as one
        /// dark pane with a grain rather than as two alternating tiles. Setting
        /// both to the kit's own 0.50 / 0.65 leaves the board exactly as the kit
        /// draws it.
        /// </summary>
        public const float CellAlpha = 0.55f;
        public const float CellAlphaAlt = 0.65f;

        /// <summary>The kit's own two cell alphas, from square.png and square_2.png.</summary>
        private const float KitCellAlpha = 128f / 255f;
        private const float KitCellAlphaAlt = 166f / 255f;

        private static StoryBoardTheme _instance;

        private int _themedLevel = -1;
        private int _themedArea = -1;
        private int _themedRepairs = -1;

        private static int AreaForLevel(int levelNumber)
        {
            // The launch owns the area, including replays. A level opened by
            // the kit directly still has an area in the story's level plan.
            return StoryFlow.PlayingLevel == levelNumber && StoryFlow.PlayingArea > 0
                ? StoryFlow.PlayingArea
                : StoryLevelPlan.AreaOf(levelNumber);
        }

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

        /// <summary>Applies the area's current room art and the arc's cell theme.</summary>
        public void Apply()
        {
            if (LevelManager.THIS == null) return;

            int levelNumber = LevelManager.THIS.currentLevel;
            int areaId = AreaForLevel(levelNumber);
            ArcTheme theme = StoryTheme.ForArea(areaId) ?? StoryTheme.ForLevel(levelNumber);
            if (theme == null) return;

            if (UseStoryBackdrop) ApplyBackdrop(theme, areaId);
            else RestoreKitBackdrop();

            if (UseStoryCellFill)
            {
                ApplyCellFill(theme);
            }
            else
            {
                // The cells wear the kit's own checker, one step darker. Said
                // out loud in the bookkeeping the theme test reads, so "no arc
                // ground" reports as a choice rather than as a board that
                // missed its theme.
                DarkenKitChecker();
                LastFill = Color.clear;
            }
            _themedLevel = levelNumber;
            _themedArea = areaId;
            _themedRepairs = StoryProgress.TasksCompletedIn(areaId);
        }

        /// <summary>
        /// What the last <see cref="Apply"/> managed to put behind the board, as
        /// one line. Empty until a board has been themed.
        ///
        /// This exists because the fallback below is silent by design — a missing
        /// picture must never stop a level opening — and silence is exactly what
        /// makes "arc 6 is wearing the kit's default" impossible to see in a
        /// screenshot of a board that looks fine. A test reads this and says so.
        /// </summary>
        public static string LastBackdrop { get; private set; }

        /// <summary>
        /// True when the last board fell back to one of the kit's own pictures
        /// because neither the area's room nor the arc's backdrop was in Resources.
        /// </summary>
        public static bool LastBackdropWasKitDefault { get; private set; }

        /// <summary>The arc whose theme <see cref="LastBackdrop"/> describes.</summary>
        public static int LastBackdropArc { get; private set; }

        /// <summary>The keys already warned about, so a sweep logs each one once.</summary>
        private static readonly HashSet<string> _warnedBackdrops = new HashSet<string>();

        /// <summary>
        /// Puts the kit's own picture back behind the board, by the kit's own
        /// rule — Background.cs indexes its five sprites by
        /// currentLevel / changeBackgoundEveryLevels and clamps.
        ///
        /// Written out here rather than left to Background.OnEnable, because
        /// that fires once when the object is enabled and the board is rebuilt
        /// on every retry and every next level without it; a session that had
        /// already been given a story picture would otherwise keep it. The
        /// AspectRatioFitter the story added is removed for the same reason:
        /// the kit's picture is drawn to the scene's own rect, which is what
        /// it was authored for.
        /// </summary>
        private static void RestoreKitBackdrop()
        {
            var background = Object.FindFirstObjectByType<Background>(FindObjectsInactive.Include);
            if (background == null)
            {
                LastBackdrop = "no Background object in the scene";
                LastBackdropWasKitDefault = true;
                return;
            }

            var image = background.GetComponent<Image>();
            Sprite[] pictures = background.pictures;
            if (image == null || pictures == null || pictures.Length == 0)
            {
                LastBackdrop = "the kit's Background has no pictures";
                LastBackdropWasKitDefault = true;
                return;
            }

            var fitter = image.GetComponent<AspectRatioFitter>();
            if (fitter != null) fitter.enabled = false;

            int every = Mathf.Max(1, background.changeBackgoundEveryLevels);
            int index = (int)((float)LevelManager.THIS.currentLevel / every - 0.01f);
            index = Mathf.Clamp(index, 0, pictures.Length - 1);

            image.sprite = pictures[index];
            image.raycastTarget = false;

            LastBackdrop = "the kit's own picture " + index;
            LastBackdropWasKitDefault = false;
        }

        /// <summary>
        /// Shows the same painted repair state as the home screen. Areas without
        /// state paintings use their restored room, then the arc and kit fallbacks.
        /// Unused while <see cref="UseStoryBackdrop"/> is false, and kept whole so
        /// turning that back on restores the story's board backdrop exactly.
        /// </summary>
        private static void ApplyBackdrop(ArcTheme theme, int areaId)
        {
            LastBackdropArc = theme.Arc;

            var background = Object.FindFirstObjectByType<Background>(FindObjectsInactive.Include);
            if (background == null)
            {
                Record(theme, "no Background object in the scene", true);
                return;
            }

            var image = background.GetComponent<Image>();
            if (image == null)
            {
                Record(theme, "the Background object has no Image", true);
                return;
            }

            Sprite story = StoryArt.AreaAtProgress(areaId) ?? StoryArt.Load(theme.BoardBackdrop);
            if (story != null)
            {
                ShowBackdrop(image, story);
                Record(theme, "Story/Art/" + story.name, false);
                return;
            }

            Sprite[] pictures = background.pictures;
            if (pictures == null || pictures.Length == 0)
            {
                Record(theme, "no story art and the kit has no pictures either", true);
                return;
            }

            int index = Mathf.Clamp(theme.KitBackdropIndex, 0, pictures.Length - 1);
            ShowBackdrop(image, pictures[index]);
            Record(theme, "the kit's own picture " + index + " — " +
                          "Resources/Story/Art/" + theme.BoardBackdrop + " is missing", true);
        }

        private static void ShowBackdrop(Image image, Sprite sprite)
        {
            image.sprite = sprite;
            image.raycastTarget = false;
            image.preserveAspect = true;
            if (sprite == null || sprite.rect.height <= 0f) return;

            // The kit's fixed-size rect stretches portrait gate art. Cover the
            // canvas at the painting's aspect, as the home screen does, so it
            // stays proportional and fills the phone on every screen size.
            var fitter = image.GetComponent<AspectRatioFitter>();
            if (fitter == null) fitter = image.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = sprite.rect.width / sprite.rect.height;
        }

        /// <summary>
        /// Remembers what the backdrop resolved to, and says so once per key when
        /// neither the room nor its arc supplied a picture. A sweep of 1208 levels
        /// re-themes on every one of them and a per-level warning would bury the
        /// report it belongs in.
        /// </summary>
        private static void Record(ArcTheme theme, string what, bool kitDefault)
        {
            LastBackdrop = what;
            LastBackdropWasKitDefault = kitDefault;

            if (!kitDefault) return;
            if (!_warnedBackdrops.Add(theme.ThemeKey)) return;

            Debug.LogWarning(string.Format(
                "[Story] arc {0} ({1}) board backdrop fell back to {2}.",
                theme.Arc, theme.ThemeKey, what));
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

            // 8 pixels drawn at 8 / pitch to the unit is one cell across — the
            // same square the cell sprite covers, at whatever pitch the board is
            // playing at. A flat colour needs no more texels than that.
            _fillSprite = Sprite.Create(texture, new Rect(0f, 0f, 8f, 8f),
                new Vector2(0.5f, 0.5f), 8f / StoryBoardScale.Pitch);
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
        /// This lays the arc's own shade over each cell instead, at one value
        /// across the whole board. The cell, its sprite, its material, its
        /// sorting order and its Animation component are all untouched.
        ///
        /// Note for anyone wondering why the
        /// board never changed colour with the arc: multiplying a black sprite
        /// by a tint leaves it black. The tint is kept — it is correct the day
        /// the cell art stops being black — but this is what carries the colour.
        /// </summary>
        /// <summary>
        /// Redraws the kit's checker at the story's two alphas, without
        /// changing what it is.
        ///
        /// Same walk as ApplyCellFill — the renderer, not the array, so a hole
        /// stays a hole — but the colour is neutral black at CellAlpha or
        /// CellAlphaAlt, and the parity is the kit's: CreateSquare alternates
        /// square.png and square_2.png down the grid, and which of the two a
        /// cell wore is read off its own sprite before the swap, never guessed.
        /// </summary>
        private static void DarkenKitChecker()
        {
            if (LevelManager.THIS == null || LevelManager.THIS.squaresArray == null) return;

            int filled = 0;

            foreach (Square square in LevelManager.THIS.squaresArray)
            {
                if (square == null) continue;

                var cell = square.GetComponent<SpriteRenderer>();
                if (cell == null || !cell.enabled || cell.sprite == null) continue;

                // Which of the kit's two tiles this cell was given, read off
                // the sprite rather than recomputed: CreateSquare's chessColor
                // flips per cell AND again per row when the column count is
                // even, so a parity worked out from row and column would
                // disagree with the board on every even-width level.
                //
                // On a second pass the sprite is already the story's flat
                // square, and then the alpha it is carrying is the record of
                // which half of the checker it belongs to.
                float alpha;
                if (cell.sprite == FillSprite())
                {
                    alpha = cell.color.a > (CellAlpha + CellAlphaAlt) * 0.5f
                        ? CellAlphaAlt
                        : CellAlpha;
                }
                else
                {
                    alpha = cell.sprite == LevelManager.THIS.squareSprite1
                        ? CellAlphaAlt
                        : CellAlpha;
                    cell.sprite = FillSprite();
                }

                cell.color = new Color(0f, 0f, 0f, alpha);
                filled++;
            }

            LastFilledCells = filled;
        }

        private static void ApplyCellFill(ArcTheme theme)
        {
            if (LevelManager.THIS == null || LevelManager.THIS.squaresArray == null) return;
            if (!theme.HasBoardFill) return;

            int made = 0;
            int filled = 0;

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

                // No checker. The kit alternated square.png and square_2.png —
                // identical black tiles at 50% and 65% alpha — and the story
                // rebuilt that from the cell's own place on the grid once the
                // sprite was replaced. Both are gone: the ground is one shade
                // over the whole board, so what the player sees inside the
                // frame is the room dimmed, not a grid drawn on it. The cells
                // are still separate objects and a hole is still a hole; it is
                // only the two-tone that went.
                cell.color = theme.BoardFill;
                filled++;
            }

            LastFill = theme.HasBoardFill ? theme.BoardFill : Color.clear;
            LastFilledCells = filled;

            if (made > 0 && !_reportedFill)
            {
                _reportedFill = true;
                Debug.Log(string.Format(
                    "[Story] board ground: {0} cell(s) now {1}.",
                    made, theme.BoardFill));
            }
        }

        private static bool _reportedFill;

        /// <summary>
        /// The ground the last board was drawn on, and how many cells carry it.
        /// Read by StoryThemeTest, which is checking that the arc's colour
        /// actually reached the board rather than that a theme was found.
        /// </summary>
        public static Color LastFill { get; private set; }

        public static int LastFilledCells { get; private set; }

        private void LateUpdate()
        {
            // The board is rebuilt on retry and on a new level without always
            // firing OnLevelLoaded, so a cheap check catches the rest.
            if (LevelManager.THIS == null) return;
            int level = LevelManager.THIS.currentLevel;
            int areaId = AreaForLevel(level);
            if (level == _themedLevel && areaId == _themedArea &&
                StoryProgress.TasksCompletedIn(areaId) == _themedRepairs) return;
            Apply();
        }
    }
}
