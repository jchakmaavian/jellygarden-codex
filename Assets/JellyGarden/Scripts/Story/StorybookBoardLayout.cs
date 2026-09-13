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
        public static Rect FieldRect(Rect safe)
        {
            float unit = safe.width / 393f;
            return new Rect(safe.x + 24 * unit, safe.y + FooterTop * unit,
                safe.width - 48 * unit, Mathf.Max(1, safe.height - (HeaderBottom + FooterTop) * unit));
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
            Shell(field, "StorybookBoardShadow", centre + Vector3.down * manager.squareHeight * .09f,
                size, new Color32(191, 202, 192, 255), -3);
            Shell(field, "StorybookBoardPaper", centre, size, new Color32(237, 240, 232, 255), -2);
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
