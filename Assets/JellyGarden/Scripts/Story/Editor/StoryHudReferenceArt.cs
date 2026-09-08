using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace JellyGarden.Scripts.Story.Editor
{
    /// <summary>Imports the reference frame and authors its reusable counter sprites.</summary>
    public static class StoryHudReferenceArt
    {
        private const string Output = "Assets/JellyGarden/Resources/Story/UI/";

        [MenuItem("JellyGarden/Story/Import Reference HUD Art")]
        public static void Import()
        {
            var frame = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            frame.LoadImage(File.ReadAllBytes("tools/art/hud/reference/frame-compact-source.png"));
            RemoveOutsideMatte(frame);
            Save(frame, "hud_reference_frame", Vector4.zero);
            Save(Card(256, 256, true), "hud_reference_moves", new Vector4(52, 52, 52, 52));
            Save(Card(256, 112, false), "hud_reference_pill", new Vector4(54, 54, 54, 54));
            Save(Badge(), "hud_reference_badge", Vector4.zero);
            Debug.Log("[Story] Reference HUD art imported.");
        }

        public static void BakeHeadless()
        {
            Import();
            StoryHudBaker.BakeHeadless();
        }

        // The generator returned an RGB preview matte. Convert only the
        // neutral area connected to the outer border into sprite alpha.
        // Interior cream highlights stay opaque, even when nearly white.
        private static void RemoveOutsideMatte(Texture2D texture)
        {
            Color32[] pixels = texture.GetPixels32();
            int width = texture.width, height = texture.height;
            var outside = new bool[pixels.Length];
            var queue = new Queue<int>();
            for (int x = 0; x < width; x++)
            {
                Enqueue(x, pixels, outside, queue);
                Enqueue((height - 1) * width + x, pixels, outside, queue);
            }
            for (int y = 0; y < height; y++)
            {
                Enqueue(y * width, pixels, outside, queue);
                Enqueue(y * width + width - 1, pixels, outside, queue);
            }
            while (queue.Count > 0)
            {
                int i = queue.Dequeue(), x = i % width, y = i / width;
                if (x > 0) Enqueue(i - 1, pixels, outside, queue);
                if (x + 1 < width) Enqueue(i + 1, pixels, outside, queue);
                if (y > 0) Enqueue(i - width, pixels, outside, queue);
                if (y + 1 < height) Enqueue(i + width, pixels, outside, queue);
            }
            for (int i = 0; i < pixels.Length; i++)
                if (outside[i]) pixels[i].a = 0;
            texture.SetPixels32(pixels);
            texture.Apply();
        }

        private static void Enqueue(int i, Color32[] pixels, bool[] outside, Queue<int> queue)
        {
            if (outside[i]) return;
            Color32 c = pixels[i];
            int high = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
            int low = Mathf.Min(c.r, Mathf.Min(c.g, c.b));
            if (c.a > 0 && (high - low > 28 || low < 105)) return;
            outside[i] = true;
            queue.Enqueue(i);
        }

        private static Texture2D Card(int width, int height, bool moves)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float t = y / (float)(height - 1);
                float distance = RoundedDistance(x + 0.5f, y + 0.5f, width, height, moves ? 56 : 54);
                Color color = Color.clear;
                if (moves)
                {
                    // Cyan light surrounds a copper double bevel and cream inset.
                    color = new Color(0.05f, 0.65f, 1f, Mathf.Exp(-Mathf.Abs(distance + 9f) * 0.19f) * 0.65f);
                    Layer(ref color, distance, -9, Hex(0x0876dd));
                    Layer(ref color, distance, -12, Hex(0x74edff));
                    Layer(ref color, distance, -15, Hex(0x182844));
                    Layer(ref color, distance, -18, Color.Lerp(Hex(0x835345), Hex(0xeabc87), t));
                    Layer(ref color, distance, -22, Hex(0xffe8b3));
                    Layer(ref color, distance, -25, Color.Lerp(Hex(0x9e6c55), Hex(0xd6b28a), t));
                    Layer(ref color, distance, -29, Hex(0xfff5d8));
                    Layer(ref color, distance, -33, Color.Lerp(Hex(0xe2be90), Hex(0xfaedcf), t));
                }
                else
                {
                    Layer(ref color, distance, -1, Hex(0x293347));
                    Layer(ref color, distance, -3, Color.Lerp(Hex(0x94604e), Hex(0xe9c394), t));
                    Layer(ref color, distance, -6, Hex(0xffedc9));
                    Layer(ref color, distance, -9, Color.Lerp(Hex(0xe2be94), Hex(0xf9e8c4), t));
                }
                pixels[y * width + x] = color;
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private static Texture2D Badge()
        {
            const int size = 160;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(80, 80)) - 78;
                float t = y / 159f;
                Color color = Color.clear;
                Layer(ref color, distance, 0, Hex(0x20283d));
                Layer(ref color, distance, -3, Color.Lerp(Hex(0x87513c), Hex(0xffe1a2), t));
                Layer(ref color, distance, -8, Hex(0x713e2e));
                Layer(ref color, distance, -11, Color.Lerp(Hex(0x755542), Hex(0xf6d39b), t));
                Layer(ref color, distance, -14, Color.Lerp(Hex(0x142743), Hex(0x315c91), t));
                pixels[y * size + x] = color;
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private static float RoundedDistance(float x, float y, float width, float height, float radius)
        {
            Vector2 q = new Vector2(Mathf.Abs(x - width / 2) - width / 2 + radius,
                Mathf.Abs(y - height / 2) - height / 2 + radius);
            return new Vector2(Mathf.Max(q.x, 0), Mathf.Max(q.y, 0)).magnitude
                + Mathf.Min(Mathf.Max(q.x, q.y), 0) - radius;
        }

        private static void Layer(ref Color current, float distance, float edge, Color fill)
        {
            current = Color.Lerp(current, fill, Mathf.Clamp01(edge - distance + 0.5f));
        }

        private static Color Hex(int rgb)
        {
            return new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, 1);
        }

        private static void Save(Texture2D texture, string name, Vector4 border)
        {
            string path = Output + name + ".png";
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = border;
            importer.spritePixelsPerUnit = name == "hud_reference_frame" ? 100 : 200;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
    }
}
