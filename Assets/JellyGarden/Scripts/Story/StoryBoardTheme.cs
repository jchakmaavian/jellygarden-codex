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
        /// This is the board itself, not the page behind it. On, every cell's
        /// sprite is swapped for a white square drawn in CellGround — one
        /// opaque royal blue across the whole board — so the ground IS that
        /// colour rather than the kit's black scrim over the room.
        ///
        /// Off, a cell keeps exactly what the kit gives it: square.png and
        /// square_2.png, flat black at alpha 0.50 and 0.65, alternated by
        /// CreateSquare into the kit's checker. That was the state until the
        /// blue ground was asked for, and DarkenKitChecker is still here, so
        /// turning this back off restores it exactly.
        /// </summary>
        public const bool UseStoryCellFill = true;

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
                ApplyBoardOutline();
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

        /// <summary>
        /// How bright the room behind the board is drawn, as a multiplier on the
        /// painting.
        ///
        /// The picture is the level's page, not its subject: at full brightness
        /// it competes with the board and the HUD, both of which the player is
        /// actually reading. Taken down it still reads as the room being
        /// repaired and stops pulling the eye off the pieces. It is a tint on
        /// the Image, so no art is edited and nothing else on the canvas dims
        /// with it.
        /// </summary>
        private const float BackdropDim = 0.5f;

        private static void ShowBackdrop(Image image, Sprite sprite)
        {
            image.sprite = sprite;
            image.raycastTarget = false;
            image.preserveAspect = true;
            image.color = new Color(BackdropDim, BackdropDim, BackdropDim, 1f);
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
        private static Sprite _flatSprite;

        /// <summary>
        /// The gap between two neighbouring cells, as a fraction of the pitch.
        ///
        /// 0.04 — each tile is drawn at 96% of its cell, so a thin line of the
        /// room shows between every pair and the ground reads as a tile per
        /// gem rather than as one unbroken sheet. On a 43-pixel cell that line
        /// is under 2 pixels: a seam, not a grid of separate buttons.
        ///
        /// It has been 0.09 and 0.06, and before those 0 (tile IS the cell,
        /// neighbours touching). The gem is ItemShare = 0.57, 79% of a cell,
        /// which is the ceiling this can climb to before the gem overhangs its
        /// tile.
        /// </summary>
        private const float CellGap = 0.04f;

        /// <summary>
        /// Corner radius of a cell, as a fraction of the tile. Small: softened,
        /// not a circle.
        ///
        /// 0.04 of the tile — an edge softened just off square. The rounding is
        /// cut from the tile's own alpha, so where four cells meet their four
        /// corners leave a hole of the room showing through. A corner falls
        /// short of its square by (sqrt(2) - 1) * radius, so that hole is
        /// 0.83 * CellCorner of a cell along its diagonal: at 0.16 it was 0.13
        /// of a cell — a 5-pixel dark dot at every crossing of a 43-pixel
        /// board, dozens of them. At 0.04 it is under 1.5 pixels, and with
        /// CellGap opening a seam between the tiles anyway, that crossing reads
        /// as part of the seam rather than as a dot on an unbroken sheet.
        /// </summary>
        private const float CellCorner = 0.04f;

        /// <summary>How many texels a tile is drawn into. 64 is enough for a clean curve at phone size.</summary>
        private const int TileTexels = 64;

        /// <summary>
        /// One cell's ground: a white rounded square, one cell across LESS the
        /// gap.
        ///
        /// KitPitch and not Pitch is the size it is built to. LevelManager
        /// scales every Square by StoryBoardScale.Factor, and this renderer is
        /// on the Square, so a sprite built at Pitch comes out Pitch * Factor
        /// wide on screen — 43% too big, which is the ground spilling past the
        /// gold frame and out over the room. The kit's own square.png is 82px
        /// at 100 to the unit, KitPitch exactly, and this matches it.
        ///
        /// The corners are cut in the ALPHA, not in the colour, so what shows
        /// in them is the arc's room rather than a dark notch. Coverage is
        /// sampled from the distance to the rounded rectangle so the curve is
        /// smooth instead of a staircase.
        ///
        /// Built once and shared by every cell on every board.
        /// </summary>
        private static Sprite FillSprite()
        {
            if (_fillSprite != null) return _fillSprite;

            int n = TileTexels;
            var texture = new Texture2D(n, n, TextureFormat.RGBA32, false);
            texture.hideFlags = HideFlags.HideAndDontSave;
            texture.wrapMode = TextureWrapMode.Clamp;

            var pixels = new Color32[n * n];
            float half = n * 0.5f;
            float radius = CellCorner * n;
            // The centre of a corner's arc, in texels from the middle.
            float inner = half - radius;

            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    // Distance outside the rounded rectangle, in texels: zero
                    // anywhere inside, and along a corner it is the distance
                    // past that corner's arc.
                    float dx = Mathf.Abs(x + 0.5f - half) - inner;
                    float dy = Mathf.Abs(y + 0.5f - half) - inner;
                    float outside = Mathf.Sqrt(
                        Mathf.Max(dx, 0f) * Mathf.Max(dx, 0f) +
                        Mathf.Max(dy, 0f) * Mathf.Max(dy, 0f)) - radius;

                    // One texel of falloff across the edge: full inside, none
                    // outside, and the fraction of the texel the shape covers
                    // in between.
                    float coverage = Mathf.Clamp01(0.5f - outside);
                    pixels[y * n + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(coverage * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            float size = StoryBoardScale.KitPitch * (1f - CellGap);
            _fillSprite = Sprite.Create(texture, new Rect(0f, 0f, n, n),
                new Vector2(0.5f, 0.5f), n / size);
            _fillSprite.name = "StoryCellFill";
            _fillSprite.hideFlags = HideFlags.HideAndDontSave;
            return _fillSprite;
        }

        /// <summary>
        /// A plain white square one cell across, for the outline's bars.
        ///
        /// The bars cannot use the ground's sprite: its corners are rounded and
        /// a bar is a sliver of it, so the rounding would eat most of the bar's
        /// length. This one is flat, and the bar's own scale gives it its shape.
        /// </summary>
        private static Sprite FlatSprite()
        {
            if (_flatSprite != null) return _flatSprite;

            var texture = new Texture2D(8, 8, TextureFormat.RGBA32, false);
            texture.hideFlags = HideFlags.HideAndDontSave;
            var pixels = new Color32[8 * 8];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(255, 255, 255, 255);
            texture.SetPixels32(pixels);
            texture.Apply();

            _flatSprite = Sprite.Create(texture, new Rect(0f, 0f, 8f, 8f),
                new Vector2(0.5f, 0.5f), 8f / StoryBoardScale.KitPitch);
            _flatSprite.name = "StoryCellFlat";
            _flatSprite.hideFlags = HideFlags.HideAndDontSave;
            return _flatSprite;
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

        /// <summary>
        /// One shade for every cell of every board: #72A6B7, a pale blue-teal.
        ///
        /// The fill is drawn in FRONT of the kit's black cell, so this colour is
        /// what reaches the screen, composited over the arc's room at
        /// GroundAlpha. The arc's computed BoardFill in themes.json is left as
        /// it is and no longer reaches a cell.
        /// </summary>
        public static readonly Color CellGround = new Color(114f / 255f, 166f / 255f, 183f / 255f, GroundAlpha);

        /// <summary>
        /// How opaque the ground is. Under 1 the arc's room reads faintly
        /// through the blue instead of being sealed off by it; low enough and
        /// the gems stop separating from what is behind them, which is what the
        /// kit's own boards get wrong.
        /// </summary>
        private const float GroundAlpha = 0.82f;

        private static void ApplyCellFill(ArcTheme theme)
        {
            if (LevelManager.THIS == null || LevelManager.THIS.squaresArray == null) return;

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
                cell.color = CellGround;
                filled++;
            }

            LastFill = CellGround;
            LastFilledCells = filled;

            if (made > 0 && !_reportedFill)
            {
                _reportedFill = true;
                Debug.Log(string.Format(
                    "[Story] board ground: {0} cell(s) now {1}.",
                    made, CellGround));
            }
        }

        /// <summary>The child that carries a cell's share of the board outline.</summary>
        private const string EdgeName = "~StoryBoardEdge";

        /// <summary>Bright gold, the colour of the frame around the blue ground.</summary>
        public static readonly Color BoardEdge = new Color(1f, 0.843f, 0f, 1f);

        /// <summary>
        /// How thick the outline is, as a fraction of one cell. Thin: a drawn
        /// line around the silhouette, not a second ring of tiles.
        /// </summary>
        private const float EdgeThickness = 0.055f;

        /// <summary>Clear space between the blue tile and the gold, in cell pitches.</summary>
        private const float EdgeGap = 0.045f;

        /// <summary>Soft outer turns, with enough radius for the full stroke width.</summary>
        private const float EdgeCorner = 0.10f;

        /// <summary>The frame's reach beyond the grid, used when fitting the camera.</summary>
        public const float BoardOuterPadding = EdgeGap + EdgeThickness - CellGap * 0.5f;

        /// <summary>Nearer the camera than the ground, still behind gems and blockers.</summary>
        private const float EdgeDepth = -0.1f;

        /// <summary>
        /// Rings exposed cell sides, joining both outward and inward turns.
        /// Straight neighbours share an endpoint at the grid seam; inward turns
        /// stop at the perpendicular stroke's inner edge. All sides use the
        /// same clearance from the tiles, including the sides around holes.
        /// </summary>
        private static void ApplyBoardOutline()
        {
            LevelManager manager = LevelManager.THIS;
            if (manager == null || manager.squaresArray == null) return;

            int cols = manager.maxCols;
            int rows = manager.maxRows;

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    Square square = At(manager, row, col);
                    if (!IsDrawn(square)) continue;

                    Transform edges = square.transform.Find(EdgeName);
                    if (edges == null)
                    {
                        var holder = new GameObject(EdgeName);
                        edges = holder.transform;
                        edges.SetParent(square.transform, false);
                        edges.localPosition = new Vector3(0f, 0f, EdgeDepth - FillDepth);
                    }

                    // Rebuilt rather than patched: the same board is re-themed
                    // on retry and on repair progress, and a stale bar left
                    // over from a different silhouette would draw a line
                    // through the middle of the board.
                    for (int i = edges.childCount - 1; i >= 0; i--)
                    {
                        GameObject old = edges.GetChild(i).gameObject;
                        old.SetActive(false);
                        if (Application.isPlaying) Object.Destroy(old);
                        else Object.DestroyImmediate(old);
                    }

                    var cell = square.GetComponent<SpriteRenderer>();
                    int order = cell == null ? 0 : cell.sortingOrder + 1;
                    string layer = cell == null ? null : cell.sortingLayerName;

                    // Which sides of this cell are on the boundary. Row 0 is
                    // the TOP of the board, so a smaller row is up.
                    bool up = !IsDrawn(At(manager, row - 1, col));
                    bool down = !IsDrawn(At(manager, row + 1, col));
                    bool left = !IsDrawn(At(manager, row, col - 1));
                    bool right = !IsDrawn(At(manager, row, col + 1));

                    // A diagonal filled neighbour makes an inward turn when
                    // the adjacent side is not exposed. This is the join the
                    // tile-local bars previously missed at every board step.
                    bool upLeft = IsDrawn(At(manager, row - 1, col - 1));
                    bool upRight = IsDrawn(At(manager, row - 1, col + 1));
                    bool downLeft = IsDrawn(At(manager, row + 1, col - 1));
                    bool downRight = IsDrawn(At(manager, row + 1, col + 1));
                    if (up) Bar(edges, 0f, 1f, order, layer, left, right, upLeft, upRight);
                    if (down) Bar(edges, 0f, -1f, order, layer, left, right, downLeft, downRight);
                    if (left) Bar(edges, -1f, 0f, order, layer, down, up, downLeft, upLeft);
                    if (right) Bar(edges, 1f, 0f, order, layer, down, up, downRight, upRight);

                    if (up && left) Elbow(edges, -1f, 1f, order, layer);
                    if (up && right) Elbow(edges, 1f, 1f, order, layer);
                    if (down && left) Elbow(edges, -1f, -1f, order, layer);
                    if (down && right) Elbow(edges, 1f, -1f, order, layer);
                }
            }
        }

        /// <summary>
        /// The square at a grid position, or null when the position is off the
        /// board. Row-major, the same indexing CreateSquare fills.
        /// </summary>
        private static Square At(LevelManager manager, int row, int col)
        {
            if (row < 0 || col < 0 || row >= manager.maxRows || col >= manager.maxCols) return null;
            int index = row * manager.maxCols + col;
            if (index < 0 || index >= manager.squaresArray.Length) return null;
            return manager.squaresArray[index];
        }

        /// <summary>
        /// Whether a cell is part of the board the player sees. A hole is a
        /// square with its renderer switched off, which is the same test
        /// ApplyCellFill uses to decide what gets a ground.
        /// </summary>
        private static bool IsDrawn(Square square)
        {
            if (square == null) return false;
            var cell = square.GetComponent<SpriteRenderer>();
            return cell != null && cell.enabled && cell.sprite != null;
        }

        /// <summary>
        /// A straight run outside a tile. Convex ends leave room for a rounded
        /// elbow; concave ends meet the neighbouring perpendicular stroke.
        /// </summary>
        private static void Bar(Transform parent, float dx, float dy, int order, string layer,
            bool trimLow, bool trimHigh, bool concaveLow, bool concaveHigh)
        {
            var go = new GameObject("edge");
            go.transform.SetParent(parent, false);

            // Squares already carry StoryBoardScale.Factor, so use KitPitch
            // locally. Keep the border wholly outside the tile's painted area.
            float cell = StoryBoardScale.KitPitch;
            float sprite = cell;
            float thick = EdgeThickness * cell;
            float clearance = (EdgeGap - CellGap * 0.5f) * cell;
            float outer = cell * 0.5f + clearance + thick;
            float radius = EdgeCorner * cell;
            float low = trimLow ? -outer + radius
                : -cell * 0.5f + (concaveLow ? clearance : 0f);
            float high = trimHigh ? outer - radius
                : cell * 0.5f - (concaveHigh ? clearance : 0f);

            float length = high - low;
            if (length <= 0f) return;

            float shift = (high + low) * 0.5f;

            go.transform.localPosition = dx != 0f
                ? new Vector3(dx * (cell * 0.5f + clearance + thick * 0.5f), shift, 0f)
                : new Vector3(shift, dy * (cell * 0.5f + clearance + thick * 0.5f), 0f);

            float along = length / sprite;
            float across = thick / sprite;
            go.transform.localScale = dx != 0f
                ? new Vector3(across, along, 1f)
                : new Vector3(along, across, 1f);

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = FlatSprite();
            renderer.color = BoardEdge;
            renderer.sortingOrder = order;
            if (!string.IsNullOrEmpty(layer)) renderer.sortingLayerName = layer;
        }

        /// <summary>
        /// The frame's turn at an outer corner: a quarter of a ring, one
        /// thickness wide, joining the two bars that were cut back for it.
        ///
        /// The arc sprite is drawn for the +x/+y corner with its pivot ON the
        /// centre of the arc, so the other three corners are the same sprite
        /// with a negative scale — no second texture, and the join lands in the
        /// same place on all four.
        /// </summary>
        private static void Elbow(Transform parent, float dx, float dy, int order, string layer)
        {
            var go = new GameObject("corner");
            go.transform.SetParent(parent, false);

            float cell = StoryBoardScale.KitPitch;
            float outer = cell * (0.5f + BoardOuterPadding);
            float radius = EdgeCorner * cell;

            // Match the tangents of the two trimmed bars, outside the tile.
            go.transform.localPosition = new Vector3(
                dx * (outer - radius), dy * (outer - radius), 0f);
            go.transform.localScale = new Vector3(dx, dy, 1f);

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = ArcSprite();
            renderer.color = BoardEdge;
            renderer.sortingOrder = order;
            if (!string.IsNullOrEmpty(layer)) renderer.sortingLayerName = layer;
        }

        private static Sprite _arcSprite;

        /// <summary>
        /// A quarter ring of outer radius EdgeCorner and width EdgeThickness,
        /// occupying the +x/+y quadrant with its pivot at the arc's centre.
        ///
        /// Alpha again, not colour: what is outside the ring has to be the room
        /// showing through, or the rounded corner would be a gold corner with a
        /// square block behind it.
        /// </summary>
        private static Sprite ArcSprite()
        {
            if (_arcSprite != null) return _arcSprite;

            int n = TileTexels;
            var texture = new Texture2D(n, n, TextureFormat.RGBA32, false);
            texture.hideFlags = HideFlags.HideAndDontSave;
            texture.wrapMode = TextureWrapMode.Clamp;

            float cell = StoryBoardScale.KitPitch;
            float radius = EdgeCorner * cell;
            // The texture spans one radius, so a texel is this much of the world
            // and the ring's width can be written in texels.
            float width = EdgeThickness * cell / radius * n;

            var pixels = new Color32[n * n];
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float px = x + 0.5f;
                    float py = y + 0.5f;
                    float d = Mathf.Sqrt(px * px + py * py);

                    // Inside the outer edge AND outside the inner one, with a
                    // texel of falloff on each so the curve is smooth.
                    float coverage = Mathf.Clamp01(Mathf.Min(n - d, d - (n - width)) + 0.5f);
                    pixels[y * n + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(coverage * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            _arcSprite = Sprite.Create(texture, new Rect(0f, 0f, n, n),
                Vector2.zero, n / radius);
            _arcSprite.name = "StoryBoardCorner";
            _arcSprite.hideFlags = HideFlags.HideAndDontSave;
            return _arcSprite;
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
