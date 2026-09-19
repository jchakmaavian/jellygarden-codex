using System.Collections.Generic;
using UnityEngine;

namespace JellyGarden.Scripts.Story
{
    // A rounded, bevelled border follows the actual level silhouette, including
    // holes and stepped edges. One mesh also fills the tiny seams between cells.
    public sealed class StoryRoyalBoardFrame : MonoBehaviour
    {
        struct Edge { public Vector2Int a, b; public bool used; }
        Mesh mesh;
        Material material;
        public int ContourCount { get; private set; }

        public static void Draw(LevelManager manager, Transform field)
        {
            var child = field.Find("RoyalBoardFrame");
            if (child == null)
            {
                child = new GameObject("RoyalBoardFrame").transform;
                child.SetParent(field, false);
            }
            var frame = child.GetComponent<StoryRoyalBoardFrame>();
            if (frame == null) frame = child.gameObject.AddComponent<StoryRoyalBoardFrame>();
            frame.Rebuild(manager);
        }

        void Rebuild(LevelManager manager)
        {
            int rows = manager.maxRows, cols = manager.maxCols;
            var live = new bool[rows, cols];
            Square first = null;
            foreach (var square in manager.squaresArray)
            {
                if (square == null) continue;
                if (first == null) first = square;
                var renderer = square.GetComponent<SpriteRenderer>();
                live[square.row, square.col] = renderer != null && renderer.enabled;
            }
            if (first == null) return;
            bool Has(int r, int c) => r >= 0 && c >= 0 && r < rows && c < cols && live[r, c];
            float pitch = manager.squareWidth;
            Vector3 origin = first.transform.localPosition -
                new Vector3(first.col * pitch, -first.row * pitch, 0) + new Vector3(-pitch * .5f, pitch * .5f, .02f);
            Vector2 Point(Vector2Int p) => new Vector2(p.x * pitch, -p.y * pitch);
            var vertices = new List<Vector3>();
            var colors = new List<Color32>();
            var triangles = new List<int>();
            var edges = new List<Edge>();
            var outgoing = new Dictionary<Vector2Int, List<int>>();
            void AddEdge(Vector2Int a, Vector2Int b)
            {
                if (!outgoing.TryGetValue(a, out var candidates)) outgoing[a] = candidates = new List<int>();
                candidates.Add(edges.Count);
                edges.Add(new Edge { a = a, b = b });
            }
            void Vertex(Vector2 p, Color color)
            {
                vertices.Add(origin + new Vector3(p.x, p.y, 0));
                colors.Add(color);
            }
            var ground = new Color32(26, 49, 76, 255);
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    if (!live[r, c]) continue;
                    if (!Has(r - 1, c)) AddEdge(new Vector2Int(c, r), new Vector2Int(c + 1, r));
                    if (!Has(r, c + 1)) AddEdge(new Vector2Int(c + 1, r), new Vector2Int(c + 1, r + 1));
                    if (!Has(r + 1, c)) AddEdge(new Vector2Int(c + 1, r + 1), new Vector2Int(c, r + 1));
                    if (!Has(r, c - 1)) AddEdge(new Vector2Int(c, r + 1), new Vector2Int(c, r));
                    int start = vertices.Count;
                    Vertex(Point(new Vector2Int(c, r)), ground);
                    Vertex(Point(new Vector2Int(c + 1, r)), ground);
                    Vertex(Point(new Vector2Int(c + 1, r + 1)), ground);
                    Vertex(Point(new Vector2Int(c, r + 1)), ground);
                    triangles.AddRange(new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
                }

            ContourCount = 0;
            for (int seed = 0; seed < edges.Count; seed++)
            {
                if (edges[seed].used) continue;
                var polygon = new List<Vector2>();
                int index = seed;
                for (int guard = 0; guard <= edges.Count; guard++)
                {
                    Edge edge = edges[index];
                    if (edge.used) break;
                    edge.used = true;
                    edges[index] = edge;
                    polygon.Add(Point(edge.a));
                    if (edge.b == edges[seed].a) break;
                    if (!outgoing.TryGetValue(edge.b, out var candidates)) break;
                    int next = -1;
                    float best = float.MaxValue;
                    foreach (int candidate in candidates)
                    {
                        if (edges[candidate].used) continue;
                        float turn = Vector2.SignedAngle(Point(edge.b - edge.a), Point(edges[candidate].b - edges[candidate].a));
                        // At diagonally touching islands, take the right turn
                        // to retain two contours instead of crossing the border.
                        if (turn < best) { best = turn; next = candidate; }
                    }
                    if (next < 0) break;
                    index = next;
                }
                if (polygon.Count < 3) continue;
                for (int i = polygon.Count - 1; i >= 0 && polygon.Count > 3; i--)
                {
                    Vector2 a = polygon[(i + polygon.Count - 1) % polygon.Count], b = polygon[i], d = polygon[(i + 1) % polygon.Count];
                    if (Mathf.Abs(Vector2.SignedAngle(b - a, d - b)) < .1f) polygon.RemoveAt(i);
                }
                var path = new List<Vector2>();
                var normals = new List<Vector2>();
                for (int i = 0; i < polygon.Count; i++)
                {
                    Vector2 p = polygon[i], prev = polygon[(i + polygon.Count - 1) % polygon.Count], next = polygon[(i + 1) % polygon.Count];
                    Vector2 incoming = (p - prev).normalized, direction = (next - p).normalized;
                    float radius = Mathf.Min(pitch * .17f, Vector2.Distance(prev, p) * .25f, Vector2.Distance(p, next) * .25f);
                    Vector2 begin = p - incoming * radius, end = p + direction * radius;
                    for (int step = 0; step <= 6; step++)
                    {
                        float t = step / 6f;
                        Vector2 at = (1 - t) * (1 - t) * begin + 2 * (1 - t) * t * p + t * t * end;
                        Vector2 tangent = ((1 - t) * (p - begin) + t * (end - p)).normalized;
                        Vector2 normal = new Vector2(-tangent.y, tangent.x);
                        path.Add(at + normal * pitch * .018f);
                        normals.Add(normal);
                    }
                }
                float[] band = { -.5f, -.37f, -.18f, .08f, .31f, .46f, .5f };
                Color32[] gold = {
                    new Color32(95, 51, 21, 255), new Color32(237, 156, 32, 255),
                    new Color32(255, 221, 104, 255), new Color32(255, 248, 192, 255),
                    new Color32(255, 196, 61, 255), new Color32(168, 93, 23, 255), new Color32(80, 45, 27, 255)
                };
                int baseIndex = vertices.Count;
                for (int i = 0; i < path.Count; i++)
                    for (int b = 0; b < band.Length; b++)
                    {
                        Color color = gold[b];
                        float light = .88f + .12f * Mathf.Clamp01(Vector2.Dot(normals[i], new Vector2(-.5f, .85f)));
                        color.r *= light; color.g *= light; color.b *= light;
                        Vertex(path[i] + normals[i] * pitch * .13f * band[b], color);
                    }
                for (int i = 0; i < path.Count; i++)
                    for (int b = 0; b < band.Length - 1; b++)
                    {
                        int a = baseIndex + i * band.Length + b;
                        int next = baseIndex + (i + 1) % path.Count * band.Length + b;
                        triangles.AddRange(new[] { a, next, a + 1, a + 1, next, next + 1 });
                    }
                ContourCount++;
            }
            if (mesh != null) Destroy(mesh);
            mesh = new Mesh { name = "Royal level silhouette" };
            mesh.SetVertices(vertices);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            var filter = GetComponent<MeshFilter>();
            if (filter == null) filter = gameObject.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            var rendererComponent = GetComponent<MeshRenderer>();
            if (rendererComponent == null) rendererComponent = gameObject.AddComponent<MeshRenderer>();
            if (material == null) material = new Material(Shader.Find("Sprites/Default")) { name = "Royal board frame" };
            rendererComponent.sharedMaterial = material;
            rendererComponent.sortingOrder = -1;
        }
        void OnDestroy()
        {
            if (mesh != null) Destroy(mesh);
            if (material != null) Destroy(material);
        }
    }
}
