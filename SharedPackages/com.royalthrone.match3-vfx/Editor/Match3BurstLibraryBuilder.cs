using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RoyalThrone.Match3Vfx.Editor
{
    /// <summary>
    /// Cuts burst sheets (a painted explosion of separated chips on transparency) into a
    /// <see cref="Match3BurstLibrary"/>. Each alpha-connected, coloured island becomes one chip;
    /// the white centre glow and compression specks are skipped. Source PNGs are not modified.
    /// </summary>
    public static class Match3BurstLibraryBuilder
    {
        public struct Island
        {
            public RectInt rect;
            public int area;
        }

        const string MenuPath = "Assets/Create/Royal Throne/Match-3 Burst Library From Selected Textures";

        [MenuItem(MenuPath, true)]
        static bool ValidateFromSelection() => Selection.GetFiltered<Texture2D>(SelectionMode.Assets).Length > 0;

        [MenuItem(MenuPath, false, 2)]
        static void FromSelection()
        {
            var sheets = Selection.GetFiltered<Texture2D>(SelectionMode.Assets).OrderBy(t => t.name).ToList();
            string folder = Path.GetDirectoryName(AssetDatabase.GetAssetPath(sheets[0])).Replace('\\', '/');
            string path = AssetDatabase.GenerateUniqueAssetPath(folder + "/Match3BurstLibrary.asset");
            var library = Build(sheets, path);
            Selection.activeObject = library;
            Debug.Log("[Match3Vfx] Burst library " + path + ": " + string.Join(", ", library.bursts.Select(b => b.label + " " + b.pieces.Length)) +
                      ". Colour index follows this order; reorder the bursts array to match your pieces.");
        }

        /// <summary>Creates or replaces a library asset. Bursts follow the order of <paramref name="sheets"/>.</summary>
        public static Match3BurstLibrary Build(IList<Texture2D> sheets, string assetPath, int minimumPieces = 4, int maximumPieces = 24)
        {
            var library = AssetDatabase.LoadAssetAtPath<Match3BurstLibrary>(assetPath);
            if (!library)
            {
                library = ScriptableObject.CreateInstance<Match3BurstLibrary>();
                AssetDatabase.CreateAsset(library, assetPath);
            }
            foreach (var old in AssetDatabase.LoadAllAssetsAtPath(assetPath))
                if (old != library) Object.DestroyImmediate(old, true);

            var bursts = new List<Match3BurstLibrary.Burst>();
            foreach (var source in sheets)
            {
                string sourcePath = AssetDatabase.GetAssetPath(source);
                var readable = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                try
                {
                    if (!readable.LoadImage(File.ReadAllBytes(Path.GetFullPath(sourcePath))))
                        throw new InvalidDataException("Cannot decode " + sourcePath + ". Use PNG or JPG sheets.");
                    int width = readable.width, height = readable.height;
                    var islands = FindIslands(readable.GetPixels32(), width, height)
                        .OrderByDescending(i => i.area).Take(maximumPieces)
                        .OrderBy(i => Mathf.Atan2(i.rect.center.y - height * 0.5f, i.rect.center.x - width * 0.5f)).ToList();
                    if (islands.Count < minimumPieces)
                    {
                        Debug.LogWarning("[Match3Vfx] " + sourcePath + " has only " + islands.Count + " separated coloured chips; skipped.");
                        continue;
                    }

                    var pieces = new Match3BurstLibrary.Piece[islands.Count];
                    for (int i = 0; i < pieces.Length; i++)
                    {
                        var r = islands[i].rect;
                        // The imported texture may be downscaled by max-size settings.
                        var importedRect = new Rect(r.x * source.width / (float)width, r.y * source.height / (float)height,
                            r.width * source.width / (float)width, r.height * source.height / (float)height);
                        var sprite = Sprite.Create(source, importedRect, new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
                        sprite.name = source.name + " chip " + i;
                        AssetDatabase.AddObjectToAsset(sprite, library);
                        pieces[i] = new Match3BurstLibrary.Piece
                        {
                            sprite = sprite,
                            offset = new Vector2(r.center.x / width - 0.5f, r.center.y / height - 0.5f)
                        };
                    }
                    bursts.Add(new Match3BurstLibrary.Burst { label = source.name, pieces = pieces });
                }
                finally
                {
                    Object.DestroyImmediate(readable);
                }
            }

            library.bursts = bursts.ToArray();
            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(assetPath);
            return library;
        }

        /// <summary>Alpha-connected coloured islands, padded by two pixels.</summary>
        public static List<Island> FindIslands(Color32[] pixels, int width, int height, byte alphaThreshold = 40, int minimumArea = 80, bool requireColour = true)
        {
            var found = new List<Island>();
            var visited = new bool[pixels.Length];
            var queue = new Queue<int>();
            for (int start = 0; start < pixels.Length; start++)
            {
                if (visited[start] || pixels[start].a < alphaThreshold) continue;
                visited[start] = true;
                queue.Enqueue(start);
                int minX = width, minY = height, maxX = 0, maxY = 0, area = 0, coloured = 0;
                while (queue.Count > 0)
                {
                    int p = queue.Dequeue(), x = p % width, y = p / width;
                    var c = pixels[p];
                    minX = Math.Min(minX, x); minY = Math.Min(minY, y);
                    maxX = Math.Max(maxX, x); maxY = Math.Max(maxY, y);
                    area++;
                    if (Math.Max(c.r, Math.Max(c.g, c.b)) - Math.Min(c.r, Math.Min(c.g, c.b)) > 35) coloured++;
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int nx = x + dx, ny = y + dy;
                            if (nx < 0 || ny < 0 || nx >= width || ny >= height) continue;
                            int n = ny * width + nx;
                            if (visited[n] || pixels[n].a < alphaThreshold) continue;
                            visited[n] = true;
                            queue.Enqueue(n);
                        }
                    }
                }
                // Skip the white centre flash, tiny specks and anything spanning most of the sheet.
                if (area < minimumArea || (requireColour && coloured < area * 0.28f) ||
                    maxX - minX > width * 0.45f || maxY - minY > height * 0.45f) continue;
                int x0 = Math.Max(0, minX - 2), y0 = Math.Max(0, minY - 2);
                int x1 = Math.Min(width, maxX + 3), y1 = Math.Min(height, maxY + 3);
                found.Add(new Island { rect = new RectInt(x0, y0, x1 - x0, y1 - y0), area = area });
            }
            return found;
        }
    }
}
