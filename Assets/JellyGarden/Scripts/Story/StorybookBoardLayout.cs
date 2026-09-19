using UnityEngine;
using UnityEngine.UI;

namespace JellyGarden.Scripts.Story
{
    public static class StorybookBoardLayout
    {
        // Shared with the responsive royal HUD, in 393-point portrait units.
        public const float HeaderBottom = 160;
        public const float FooterTop = 185;
        public const float SidePadding = 5;
        public const float Allowance = .22f;
        public const float IceFill = 1.1228f;
        public static float CellPixels { get; private set; }
        public static Rect Safe => Screen.safeArea.width > 0 ? Screen.safeArea : new Rect(0, 0, Screen.width, Screen.height);
        public static float DesignUnit(Rect safe) => Mathf.Min(safe.width / 393f, safe.height / 720f);

        public static Rect FieldRect(Rect safe)
        {
            float unit = DesignUnit(safe);
            return new Rect(safe.x + SidePadding * unit, safe.y + FooterTop * unit,
                safe.width - SidePadding * 2f * unit,
                Mathf.Max(1, safe.height - (HeaderBottom + FooterTop) * unit));
        }
        public static float FitPitch(Rect safe, int cols, int rows)
        {
            Rect field = FieldRect(safe);
            return Mathf.Max(1, Mathf.Floor(Mathf.Min(58f * DesignUnit(safe),
                field.width / (Mathf.Max(1, cols) + Allowance),
                field.height / (Mathf.Max(1, rows) + Allowance))));
        }
        public static void FitCamera(LevelManager manager)
        {
            Camera camera = Camera.main;
            if (camera == null || !camera.orthographic || manager.maxCols < 1 || manager.maxRows < 1) return;
            Rect field = FieldRect(Safe);
            CellPixels = FitPitch(Safe, manager.maxCols, manager.maxRows);
            float ppu = CellPixels / manager.squareWidth;
            camera.orthographicSize = Screen.height / (2f * ppu);
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
                    float edge = radius - Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(edge);
                    float distance = Mathf.Min(Mathf.Min(x, y), Mathf.Min(size - 1 - x, size - 1 - y));
                    float top = y / (float)(size - 1);
                    float brightness = Mathf.Lerp(.70f, .92f, top);
                    if (distance < 1.5f) brightness = .55f;
                    else if (distance < 3f) brightness = top > .5f ? 1f : .64f;
                    pixels[y * size + x] = new Color(brightness, brightness, brightness, alpha);
                }
            texture.SetPixels32(pixels);
            texture.Apply();
            // Keep a narrow seam so the recessed cells read as one board.
            _cell = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f),
                size / (StoryBoardScale.KitPitch * .985f), 0, SpriteMeshType.FullRect);
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
                    ? new Color32(54, 92, 129, 255) : new Color32(64, 107, 148, 255);
                Transform oldEdge = square.transform.Find("~StoryBoardEdge");
                if (oldEdge != null) oldEdge.gameObject.SetActive(false);
                // Only the kit's ice layers change; stone, chains and other blockers retain their behaviour/art.
                //
                // square.type, NOT the block list: CreateObstacles adds wire,
                // stone, undestroyable and thriving prefabs to that same list,
                // so walking it would repaint every one of them as ice.
                //
                // The cost is that a cell carrying ice AND an obstacle has its
                // type overwritten by the obstacle branch, so it is skipped --
                // 1422 cells across 213 levels, nearly all "23", ice under a
                // wire. Those keep the ice art's own baked-in margin. Closing
                // that needs a per-block test of which prefab a block came
                // from, which this does not have.
                if (square.type == SquareTypes.BLOCK && square.block != null)
                    foreach (GameObject block in square.block)
                    {
                        if (block == null) continue;
                        var ice = block.GetComponent<SpriteRenderer>();
                        if (ice != null)
                        {
                            ice.sprite = StorybookBoardArt.Get("ice");
                            ice.color = Color.white;
                            block.transform.localScale = Vector3.one * IceFill;
                        }
                    }
            }
            if (field == null) return;
            // Retire surfaces that an earlier level renderer may have created.
            Hide(field, "StorybookBoardShadow");
            Hide(field, "StorybookBoardPaper");

            Hide(field, "StorybookBoardFrameOuter");
            Hide(field, "StorybookBoardFrame");
            Hide(field, "StorybookBoardOutline");
            StoryRoyalBoardFrame.Draw(manager, field);
        }

        /// <summary>Switch off a tray a previous level left behind, if there is one.</summary>
        static void Hide(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child != null) child.gameObject.SetActive(false);
        }

        public static void DressBackdrop(Image image)
        {
            image.color = Color.white;
            Canvas canvas = image.GetComponentInParent<Canvas>();
            if (canvas != null) { canvas.overrideSorting = true; canvas.sortingOrder = -10; }
            // The approved courtyard already has a quiet center and soft focus.
            // Retire the opaque washes that belonged to the previous palette.
            Hide(image.transform, "StorybookWash");
            Hide(image.transform, "StorybookFloorWash");
        }
    }
}
