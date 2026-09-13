using UnityEngine;
using UnityEngine.UI;

namespace JellyGarden.Scripts.Story
{
    public static class StorybookBoardLayout
    {
        public const float HeaderBottom = 174;
        public const float FooterTop = 128;
        public static float CellPixels { get; private set; }
        public static Rect Safe => Screen.safeArea.width > 0 ? Screen.safeArea : new Rect(0, 0, Screen.width, Screen.height);
        /// <summary>
        /// How much of the screen's width the board may not use, each side.
        ///
        /// 3, not 24. A nine-column board is WIDTH-limited on every phone this
        /// ships to — it already fills the field exactly, with nothing spare —
        /// so this padding is the only thing between the board and a bigger
        /// cell. At 24 a 393-point screen drew a 36.2-point cell; at 3 it draws
        /// 40.6. Not 0: the outline stands Gap clear of the cells and needs a
        /// little room of its own, or it sits on the screen edge.
        /// </summary>
        public const float SidePadding = 3;

        public static Rect FieldRect(Rect safe)
        {
            float unit = safe.width / 393f;
            return new Rect(safe.x + SidePadding * unit, safe.y + FooterTop * unit,
                safe.width - SidePadding * 2f * unit,
                Mathf.Max(1, safe.height - (HeaderBottom + FooterTop) * unit));
        }
        public static float FitPitch(Rect safe, int cols, int rows)
        {
            Rect field = FieldRect(safe);
            // Padding is proportional to a cell, so it also shrinks on short phones.
            return Mathf.Max(1, Mathf.Floor(Mathf.Min(45.7f * safe.width / 393f,
                field.width / (Mathf.Max(1, cols) + .52f), field.height / (Mathf.Max(1, rows) + .52f))));
        }
        public static void FitCamera(LevelManager manager)
        {
            Camera camera = Camera.main;
            if (camera == null || !camera.orthographic || manager.maxCols < 1 || manager.maxRows < 1) return;
            Rect field = FieldRect(Safe);
            CellPixels = FitPitch(Safe, manager.maxCols, manager.maxRows);
            float ppu = CellPixels / manager.squareWidth;
            camera.orthographicSize = Screen.height / (2f * ppu);
            // Field's slide-in animation owns its transform; offset only the camera.
            Vector3 position = camera.transform.position;
            position.x = -manager.maxCols / 2.75f + (manager.maxCols - 1) * manager.squareWidth * .5f
                - (Mathf.Round(field.center.x) - Screen.width * .5f) / ppu;
            position.y = manager.maxRows / 2.75f - (manager.maxRows - 1) * manager.squareHeight * .5f
                - (Mathf.Round(field.center.y) - Screen.height * .5f) / ppu;
            camera.transform.position = position;
        }
        static Sprite _cell;
        static Sprite Cell()
        {
            if (_cell != null) return _cell;
            const int size = 64;
            const float radius = 11;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(radius - x, x - (size - 1 - radius), 0);
                    float dy = Mathf.Max(radius - y, y - (size - 1 - radius), 0);
                    float alpha = Mathf.Clamp01(radius - Mathf.Sqrt(dx * dx + dy * dy));
                    pixels[y * size + x] = new Color(1, 1, 1, alpha);
                }
            texture.SetPixels32(pixels);
            texture.Apply();
            _cell = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f),
                size / (StoryBoardScale.KitPitch * .955f), 0, SpriteMeshType.FullRect);
            _cell.name = "storybook_soft_cell";
            return _cell;
        }
        public static void DressCells(LevelManager manager)
        {
            if (manager == null || manager.squaresArray == null) return;
            Transform field = null;
            foreach (Square square in manager.squaresArray)
            {
                if (square == null) continue;
                var renderer = square.GetComponent<SpriteRenderer>();
                if (renderer == null || !renderer.enabled) continue;
                field = square.transform.parent;
                renderer.sprite = Cell();
                renderer.color = (square.row + square.col) % 2 == 0
                    ? new Color32(220, 232, 235, 255) : new Color32(232, 240, 240, 255);
                Transform oldEdge = square.transform.Find("~StoryBoardEdge");
                if (oldEdge != null) oldEdge.gameObject.SetActive(false);
                // Only the kit's ice layers change; stone, chains and other blockers retain their behaviour/art.
                if (square.type == SquareTypes.BLOCK && square.block != null)
                    foreach (GameObject block in square.block)
                    {
                        if (block == null) continue;
                        var ice = block.GetComponent<SpriteRenderer>();
                        if (ice != null) { ice.sprite = StorybookBoardArt.Get("ice"); ice.color = Color.white; }
                    }
            }
            if (field == null) return;
            Vector3 centre = new Vector3((manager.maxCols - 1) * manager.squareWidth * .5f,
                -(manager.maxRows - 1) * manager.squareHeight * .5f, .1f);
            // Squares are generated at world positions; use their local origin for the moving tray.
            Square first = manager.squaresArray[0];
            if (first != null) centre += first.transform.localPosition;
            Vector2 size = new Vector2((manager.maxCols + .52f) * manager.squareWidth,
                (manager.maxRows + .52f) * manager.squareHeight);
            // No tray behind the cells: the arc's room is what the board sits on.
            // The per-cell tiles still carry the grid, so a hole and a blocked
            // cell read exactly as before. Hidden rather than skipped, because
            // this runs per level and a tray built by an earlier one survives.
            Hide(field, "StorybookBoardShadow");
            Hide(field, "StorybookBoardPaper");

            // A rectangle around the whole grid was drawn here, which squares off
            // every missing corner and hole. The border traces the board's own
            // OUTLINE instead: one bar per cell edge that has no live cell beyond
            // it, so it steps in and out with the shape and sits in the gap the
            // outer cells already leave.
            Hide(field, "StorybookBoardFrameOuter");
            Hide(field, "StorybookBoardFrame");
            Outline(manager, field, centre);
        }

        /// <summary>The board's silhouette, in yellow, one bar per exposed cell edge.</summary>
        static void Outline(LevelManager manager, Transform field, Vector3 centre)
        {
            Transform root = field.Find(OutlineName);
            if (root == null)
            {
                root = new GameObject(OutlineName).transform;
                root.SetParent(field, false);
            }
            root.gameObject.SetActive(true);
            // A previous level's outline is a different shape, so every bar is
            // switched off and only the ones this board needs come back on.
            for (int i = 0; i < root.childCount; i++) root.GetChild(i).gameObject.SetActive(false);

            // Seated against the cells, and the whole outline is pushed out
            // afterwards by scaling its root. Moving each bar out on its own
            // normal instead pulls the steps apart: a bar slides perpendicular
            // to its own length, so the corners separate diagonally and the
            // outline comes out as detached strokes.
            float cell = manager.squareWidth;
            float inset = cell * .5f + Thickness * .5f;
            int bar = 0;
            foreach (Square square in manager.squaresArray)
            {
                if (!Live(square)) continue;
                Vector3 at = square.transform.localPosition;
                // Exactly its own cell, not cell + Thickness. The longer run was
                // there to close an L-join, but at a step the far end has no
                // bar to meet and the overshoot stuck out as a yellow tab. The
                // corner squares below close the joins instead.
                float run = cell;
                if (!Live(At(manager, square.row - 1, square.col)))
                    Bar(root, ref bar, at + Vector3.up * inset, new Vector2(run, Thickness));
                if (!Live(At(manager, square.row + 1, square.col)))
                    Bar(root, ref bar, at + Vector3.down * inset, new Vector2(run, Thickness));
                if (!Live(At(manager, square.row, square.col - 1)))
                    Bar(root, ref bar, at + Vector3.left * inset, new Vector2(Thickness, run));
                if (!Live(At(manager, square.row, square.col + 1)))
                    Bar(root, ref bar, at + Vector3.right * inset, new Vector2(Thickness, run));

                // The four corners of this cell, filled only where two of its
                // own edges meet — an outside corner. A step's open end has no
                // corner here, so nothing juts into the gap beside it.
                Corner(manager, root, ref bar, square, at, inset, -1, -1);
                Corner(manager, root, ref bar, square, at, inset, +1, -1);
                Corner(manager, root, ref bar, square, at, inset, -1, +1);
                Corner(manager, root, ref bar, square, at, inset, +1, +1);
            }

            // The gap, applied to the finished outline rather than to each bar:
            // the whole thing is grown about the board's centre, so every bar
            // moves together and the steps stay joined. Scaling by the board's
            // own span keeps the push a fixed fraction of a cell whether the
            // board is 5 wide or 9.
            // The edge has to move out by one Gap of a cell, and scaling about
            // the centre moves it by HALF the span times (scale - 1) — so the
            // half-span is what the push divides by. Dividing by the whole span
            // halved it again and the gap came out a hairline.
            float span = Mathf.Max(manager.maxCols * cell, manager.maxRows * manager.squareHeight);
            float scale = 1f + cell * Gap / Mathf.Max(span * .5f, .001f);
            root.localScale = Vector3.one * scale;
            root.localPosition = centre * (1f - scale);
        }

        static Sprite _plain;

        /// <summary>
        /// An opaque white square with a border on every side, so a bar drawn
        /// Sliced keeps a straight edge at any length.
        /// </summary>
        static Sprite Plain()
        {
            if (_plain != null) return _plain;
            const int size = 8;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            var pixels = new Color32[size * size];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(255, 255, 255, 255);
            texture.SetPixels32(pixels);
            texture.Apply();
            _plain = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f),
                size / (StoryBoardScale.KitPitch * .955f), 0, SpriteMeshType.FullRect,
                new Vector4(2, 2, 2, 2));
            _plain.name = "storybook_outline_bar";
            return _plain;
        }

        const string OutlineName = "StorybookBoardOutline";

        /// <summary>The bar's width, as a fraction of a cell.</summary>
        const float Thickness = .12f;

        /// <summary>
        /// How far clear of the outer cells the border sits, as a fraction of a
        /// cell. At 0 the bar sits against the tiles and reads as their edging.
        /// </summary>
        const float Gap = .07f;

        static readonly Color32 OutlineYellow = new Color32(255, 208, 0, 255);

        /// <summary>
        /// The little square that closes an L-join, at one corner of a cell.
        ///
        /// Drawn only at an OUTSIDE corner — where both of this cell's edges in
        /// that direction are exposed. At a step the corner belongs to the cell
        /// around the bend, so nothing is drawn here and no tab juts out.
        /// </summary>
        static void Corner(LevelManager manager, Transform root, ref int bar, Square square,
            Vector3 at, float inset, int dx, int dy)
        {
            if (Live(At(manager, square.row + dy, square.col))) return;
            if (Live(At(manager, square.row, square.col + dx))) return;
            Bar(root, ref bar, at + new Vector3(dx * inset, -dy * inset, 0f),
                new Vector2(Thickness, Thickness));
        }

        /// <summary>Part of the board: a square that exists and paints a tile.</summary>
        static bool Live(Square square)
        {
            if (square == null) return false;
            var renderer = square.GetComponent<SpriteRenderer>();
            return renderer != null && renderer.enabled;
        }

        static Square At(LevelManager manager, int row, int col)
        {
            if (row < 0 || col < 0 || row >= manager.maxRows || col >= manager.maxCols) return null;
            return manager.squaresArray[row * manager.maxCols + col];
        }

        /// <summary>One segment of the outline, reusing the bar a previous level built.</summary>
        static void Bar(Transform root, ref int index, Vector3 at, Vector2 size)
        {
            Transform child = index < root.childCount ? root.GetChild(index) : null;
            if (child == null)
            {
                child = new GameObject("Bar").transform;
                child.SetParent(root, false);
            }
            index++;
            child.gameObject.SetActive(true);
            child.localPosition = at;
            var renderer = child.GetComponent<SpriteRenderer>();
            if (renderer == null) renderer = child.gameObject.AddComponent<SpriteRenderer>();
            // A plain square, not Cell(): that sprite is created without a border
            // vector, so Sliced has nothing to slice and its rounded corners
            // stretch down the length of a bar.
            renderer.sprite = Plain();
            renderer.drawMode = SpriteDrawMode.Sliced;
            renderer.size = size;
            renderer.color = OutlineYellow;
            // Behind the cells (0), the pieces (2) and the blockers (4).
            renderer.sortingOrder = -1;
        }

        static Sprite _frame;

        /// <summary>
        /// A rounded rectangle that is only its edge: opaque on the band,
        /// transparent inside, so the arc's room still reads through the board.
        ///
        /// RoundedSprite fills its shape, which would hide the room and the
        /// pieces, so the ring is drawn here the way Cell() draws a tile. It is
        /// sliced, so one texture fits every board from 5x5 to 9x11 without the
        /// corners stretching.
        /// </summary>
        static Sprite Frame()
        {
            if (_frame != null) return _frame;
            // 96, not 64: the corner has to survive the slice. At 64 with a 14px
            // radius the sliced corner is smaller than the band and the ring came
            // out square. The radius is a quarter of the texture, so the corner
            // piece is always bigger than the band that runs into it.
            const int size = 96;
            const float radius = 24;
            // 10, not 5: at this PPU a 5px band on a 96px texture came out a
            // hairline against a nine-column board.
            const float band = 10f;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    // Distance outside the rounded rect, as Cell() measures it.
                    float dx = Mathf.Max(radius - x, x - (size - 1 - radius), 0);
                    float dy = Mathf.Max(radius - y, y - (size - 1 - radius), 0);
                    float outside = Mathf.Sqrt(dx * dx + dy * dy) - radius;
                    // Distance from the edge itself: 0 on the outline, negative
                    // inside. The band is the ring either side of that line.
                    float edge = Mathf.Abs(outside + band * .5f) - band * .5f;
                    pixels[y * size + x] = new Color(1, 1, 1, Mathf.Clamp01(1 - edge));
                }
            texture.SetPixels32(pixels);
            texture.Apply();
            _frame = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f),
                size / (StoryBoardScale.KitPitch * .955f), 0, SpriteMeshType.FullRect,
                new Vector4(radius, radius, radius, radius));
            _frame.name = "storybook_board_frame";
            return _frame;
        }

        static void Frame(Transform parent, string name, Vector3 centre, Vector2 size, Color color, int order)
        {
            Transform child = parent.Find(name);
            if (child == null) { child = new GameObject(name).transform; child.SetParent(parent, false); }
            child.gameObject.SetActive(true);
            child.localPosition = centre;
            var renderer = child.GetComponent<SpriteRenderer>();
            if (renderer == null) renderer = child.gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = Frame();
            renderer.drawMode = SpriteDrawMode.Sliced;
            renderer.size = size;
            renderer.color = color;
            renderer.sortingOrder = order;
        }
        /// <summary>Switch off a tray a previous level left behind, if there is one.</summary>
        static void Hide(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child != null) child.gameObject.SetActive(false);
        }

        static void Shell(Transform parent, string name, Vector3 centre, Vector2 size, Color color, int order)
        {
            Transform child = parent.Find(name);
            if (child == null) { child = new GameObject(name).transform; child.SetParent(parent, false); }
            child.localPosition = centre;
            // Not ??: a missing component is Unity's fake null, which ?? treats as present.
            var renderer = child.GetComponent<SpriteRenderer>();
            if (renderer == null) renderer = child.gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = StoryUi.RoundedSprite(48);
            renderer.drawMode = SpriteDrawMode.Sliced;
            renderer.size = size;
            renderer.color = color;
            renderer.sortingOrder = order;
        }
        public static void DressBackdrop(Image image)
        {
            image.color = Color.white;
            Canvas canvas = image.GetComponentInParent<Canvas>();
            if (canvas != null) { canvas.overrideSorting = true; canvas.sortingOrder = -10; }
            if (image.transform.Find("StorybookWash") != null) return;
            // Keep the painting at its own aspect; the cream wash is anchored to the viewport.
            Image wash = StoryUi.CreateImage("StorybookWash", image.transform, null, Color.white);
            StoryUi.Stretch(wash.rectTransform);
            wash.rectTransform.anchorMin = new Vector2(0, .66f);
            StoryCastleUi.Gradient(wash, new Color32(255, 250, 231, 118), new Color32(255, 245, 221, 240));
            wash.raycastTarget = false;
            Image floor = StoryUi.CreateImage("StorybookFloorWash", image.transform, null, Color.white);
            StoryUi.Stretch(floor.rectTransform);
            floor.rectTransform.anchorMax = new Vector2(1, .66f);
            StoryCastleUi.Gradient(floor, new Color32(255, 245, 221, 240), new Color32(249, 238, 217, 255));
            floor.raycastTarget = false;
        }
    }
}
