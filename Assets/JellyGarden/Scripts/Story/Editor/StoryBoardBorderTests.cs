using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace JellyGarden.Scripts.Story.Editor
{
    /// <summary>Samples the actual generated sprites at seams and at both kinds of turn.</summary>
    public static class StoryBoardBorderTests
    {
        private const BindingFlags PrivateStatic = BindingFlags.NonPublic | BindingFlags.Static;
        private const float Gap = .045f;
        private const float Thickness = .055f;
        private const float Radius = .10f;
        private static int _samples;

        [MenuItem("JellyGarden/Story/Run Board Border Tests")]
        public static void Run()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Run border tests outside Play mode.");
            LevelManager previous = LevelManager.THIS;
            var host = new GameObject("BorderTest");
            try
            {
                LevelManager manager = host.AddComponent<LevelManager>();
                LevelManager.THIS = manager;
                _samples = 0;
                foreach (string[] shape in new[] {
                    new[]{"#"}, new[]{"####", "####", "####"},
                    new[]{".##", "###", "###"}, new[]{"###", "#.#", "###"},
                    new[]{"..##", ".###", "###.", "##.."},
                    new[]{"##.##", "#####", "##.##"}, new[]{"#.", ".#"}
                }) CheckShape(manager, host.transform, shape);
                Debug.Log("[Story] Board border checks passed: tile clearance, straight seams, convex corners, " +
                    "concave steps, holes, narrow necks, diagonal contacts and repeat builds; " + _samples + " samples.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
                LevelManager.THIS = previous;
            }
        }

        private static void CheckShape(LevelManager manager, Transform parent, string[] shape)
        {
            var root = new GameObject("Shape");
            root.transform.SetParent(parent, false);
            try
            {
                manager.maxRows = shape.Length;
                manager.maxCols = shape[0].Length;
                manager.squaresArray = new Square[manager.maxRows * manager.maxCols];
                Sprite tile = (Sprite)typeof(StoryBoardTheme).GetMethod("FillSprite", PrivateStatic).Invoke(null, null);
                for (int y = 0; y < manager.maxRows; y++)
                for (int x = 0; x < manager.maxCols; x++)
                {
                    var go = new GameObject("Cell " + x + "," + y, typeof(SpriteRenderer), typeof(Square));
                    go.transform.SetParent(root.transform, false);
                    go.transform.localPosition = new Vector3(x, -y) * StoryBoardScale.Pitch;
                    go.transform.localScale = Vector3.one * StoryBoardScale.Factor;
                    SpriteRenderer renderer = go.GetComponent<SpriteRenderer>();
                    renderer.sprite = tile;
                    renderer.enabled = Filled(shape, x, y);
                    manager.squaresArray[y * manager.maxCols + x] = go.GetComponent<Square>();
                }
                MethodInfo build = typeof(StoryBoardTheme).GetMethod("ApplyBoardOutline", PrivateStatic);
                build.Invoke(null, null);
                int firstCount = GoldRenderers(root).Length;
                build.Invoke(null, null);
                SpriteRenderer[] gold = GoldRenderers(root);
                Require(gold.Length == firstCount, "re-theming does not accumulate border pieces");
                float pitch = StoryBoardScale.Pitch;
                float clearance = (Gap - .02f) * pitch;
                float middle = clearance + Thickness * pitch * .5f;
                float outer = (.5f + Gap - .02f + Thickness) * pitch;
                float radius = Radius * pitch;
                for (int y = 0; y < manager.maxRows; y++)
                for (int x = 0; x < manager.maxCols; x++)
                {
                    if (!Filled(shape, x, y)) continue;
                    Vector2 center = new Vector2(x, -y) * pitch;
                    foreach (Vector2 direction in new[]{Vector2.up, Vector2.down, Vector2.left, Vector2.right})
                    {
                        int nx = x + (int)direction.x, ny = y - (int)direction.y;
                        if (Filled(shape, nx, ny)) continue;
                        // The middle of an exposed side has empty space after
                        // the tile, followed by an opaque strip of gold.
                        Vector2 empty = center + direction * ((.48f + Gap * .5f) * pitch);
                        Require(!Painted(gold, empty), "space between tile and border at " + x + "," + y);
                        for (int t = -8; t <= 8; t++)
                        {
                            Vector2 tangent = new Vector2(-direction.y, direction.x);
                            Vector2 p = center + direction * (.5f * pitch + middle) + tangent * (t * .05f * pitch);
                            Require(Painted(gold, p), "unbroken exposed side at " + p);
                        }
                        Vector2 along = new Vector2(-direction.y, direction.x);
                        int ax = x + (int)along.x, ay = y - (int)along.y;
                        if (Filled(shape, ax, ay) && !Filled(shape, ax + (int)direction.x, ay - (int)direction.y))
                        {
                            Vector2 seam = center + direction * (.5f * pitch + middle) + along * (.5f * pitch);
                            for (int side = -1; side <= 1; side++)
                                Require(Painted(gold, seam + along * (side * .002f * pitch)), "joined straight seam");
                        }
                    }
                    for (int sx = -1; sx <= 1; sx += 2)
                    for (int sy = -1; sy <= 1; sy += 2)
                    {
                        if (Filled(shape, x + sx, y) || Filled(shape, x, y - sy)) continue;
                        Vector2 arc = center + new Vector2(sx, sy) * (outer - radius);
                        for (int i = 0; i <= 16; i++)
                        {
                            float angle = i * Mathf.PI / 32f;
                            Vector2 p = arc + new Vector2(sx * Mathf.Cos(angle), sy * Mathf.Sin(angle)) *
                                (radius - Thickness * pitch * .5f);
                            Require(Painted(gold, p), "joined outer corner at " + p);
                        }
                    }
                }
                // At every grid vertex, a single missing quadrant is an inward
                // turn. Follow its two arms through their common corner.
                for (int y = 0; y <= manager.maxRows; y++)
                for (int x = 0; x <= manager.maxCols; x++)
                {
                    bool[] around = {Filled(shape,x-1,y-1), Filled(shape,x,y-1), Filled(shape,x-1,y), Filled(shape,x,y)};
                    if (around.Count(v => v) != 3) continue;
                    int missing = Array.FindIndex(around, v => !v);
                    int sx = (missing % 2 == 0) ? -1 : 1, sy = missing < 2 ? 1 : -1;
                    Vector2 vertex = new Vector2(x-.5f, .5f-y) * pitch;
                    Vector2 corner = vertex + new Vector2(sx,sy) * middle;
                    for (int i = 0; i <= 12; i++)
                    {
                        Require(Painted(gold,corner+new Vector2(sx,0)*(.025f*i*pitch)), "connected inward horizontal turn");
                        Require(Painted(gold,corner+new Vector2(0,sy)*(.025f*i*pitch)), "connected inward vertical turn");
                    }
                }
                foreach (SpriteRenderer renderer in gold.Where(r => r.name == "edge"))
                {
                    Bounds bounds = renderer.bounds;
                    Require(bounds.size.x > 0 && bounds.size.y > 0, "positive border bounds");
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        private static bool Filled(string[] shape, int x, int y) =>
            y >= 0 && y < shape.Length && x >= 0 && x < shape[y].Length && shape[y][x] == '#';
        private static SpriteRenderer[] GoldRenderers(GameObject root) => root.GetComponentsInChildren<SpriteRenderer>()
            .Where(r => r.name == "edge" || r.name == "corner").ToArray();
        private static bool Painted(IEnumerable<SpriteRenderer> renderers, Vector2 world)
        {
            foreach (SpriteRenderer renderer in renderers)
            {
                Sprite sprite = renderer.sprite;
                Vector2 local = renderer.transform.InverseTransformPoint(world);
                Vector2 pixel = local * sprite.pixelsPerUnit + sprite.pivot;
                if (pixel.x < -.0001f || pixel.y < -.0001f || pixel.x > sprite.rect.width+.0001f || pixel.y > sprite.rect.height+.0001f) continue;
                float u = (sprite.rect.x + pixel.x) / sprite.texture.width;
                float v = (sprite.rect.y + pixel.y) / sprite.texture.height;
                if (sprite.texture.GetPixelBilinear(u,v).a > .75f) return true;
            }
            return false;
        }
        private static void Require(bool value, string message)
        {
            _samples++;
            if (!value) throw new InvalidOperationException("[Story] Board border: " + message);
        }
        public static void RunHeadless()
        {
            try { Run(); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }
    }
}
