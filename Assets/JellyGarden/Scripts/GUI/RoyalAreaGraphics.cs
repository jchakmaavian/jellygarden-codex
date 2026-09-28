using UnityEngine;
using UnityEngine.UI;

namespace RoyalThrone.Presentation
{
    /// <summary>
    /// A horizontal band whose top and bottom edges bow up toward the middle, as the area dialogs' ribbon does. The rect is the
    /// band at its middle; toward the ends both edges drop by <c>drop2 v^2 + drop4 v^4</c> units (v = 0 at the middle, 1 at the
    /// ends), measured on the reference ribbon at 720 wide (38.9 and 23.9 units).
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RoyalArchGraphic : MaskableGraphic
    {
        public Color topColor = Color.white, bottomColor = Color.white;
        public float drop2 = 38.9f, drop4 = 23.9f;
        [Range(4, 96)] public int segments = 48;

        public float Drop(float v) { v = Mathf.Clamp(v, -1, 1); float v2 = v * v; return drop2 * v2 + drop4 * v2 * v2; }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = GetPixelAdjustedRect();
            for (int i = 0; i <= segments; i++)
            {
                float u = (float)i / segments, x = Mathf.Lerp(r.xMin, r.xMax, u), d = Drop(u * 2 - 1);
                vh.AddVert(new Vector3(x, r.yMin - d), bottomColor * color, Vector2.zero);
                vh.AddVert(new Vector3(x, r.yMax - d), topColor * color, Vector2.zero);
                if (i == 0) continue;
                int k = i * 2;
                vh.AddTriangle(k - 2, k - 1, k + 1); vh.AddTriangle(k - 2, k + 1, k);
            }
        }
    }

    /// <summary>A thick tick drawn in the rect: two strokes through three fixed points, coloured top to bottom.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RoyalCheckGraphic : MaskableGraphic
    {
        public Color topColor = Color.white, bottomColor = Color.white;
        [Range(.05f, .5f)] public float thickness = .22f;
        static readonly Vector2 A = new Vector2(.12f, .50f), B = new Vector2(.40f, .20f), C = new Vector2(.90f, .82f);

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = GetPixelAdjustedRect(); float t = thickness * Mathf.Min(r.width, r.height);
            Vector2 P(Vector2 n) => new Vector2(r.xMin + n.x * r.width, r.yMin + n.y * r.height);
            Stroke(vh, r, P(A), P(B), t); Stroke(vh, r, P(B), P(C), t);
        }

        void Stroke(VertexHelper vh, Rect r, Vector2 a, Vector2 b, float t)
        {
            var d = (b - a).normalized; var n = new Vector2(-d.y, d.x) * t * .5f;
            a -= d * t * .5f; b += d * t * .5f;
            int k = vh.currentVertCount;
            foreach (var p in new[] { a - n, a + n, b + n, b - n })
                vh.AddVert(p, Color.Lerp(bottomColor, topColor, Mathf.InverseLerp(r.yMin, r.yMax, p.y)) * color, Vector2.zero);
            vh.AddTriangle(k, k + 1, k + 2); vh.AddTriangle(k, k + 2, k + 3);
        }
    }

    /// <summary>A ring segment centred in the rect (a padlock's shackle): from <c>from</c> to <c>to</c> degrees, counter-clockwise
    /// from the right, <c>thickness</c> as a fraction of the radius.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RoyalArcGraphic : MaskableGraphic
    {
        public Color topColor = Color.white, bottomColor = Color.white;
        public float from = 0, to = 180;
        [Range(.02f, 1)] public float thickness = .3f;
        [Range(4, 96)] public int segments = 32;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = GetPixelAdjustedRect(); float outer = Mathf.Min(r.width, r.height) * .5f, inner = outer * (1 - thickness);
            var c = r.center;
            for (int i = 0; i <= segments; i++)
            {
                float a = Mathf.Lerp(from, to, (float)i / segments) * Mathf.Deg2Rad; var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                Vector2 o = c + dir * outer, n = c + dir * inner;
                vh.AddVert(o, Color.Lerp(bottomColor, topColor, Mathf.InverseLerp(r.yMin, r.yMax, o.y)) * color, Vector2.zero);
                vh.AddVert(n, Color.Lerp(bottomColor, topColor, Mathf.InverseLerp(r.yMin, r.yMax, n.y)) * color, Vector2.zero);
                if (i == 0) continue;
                int k = i * 2;
                vh.AddTriangle(k - 2, k - 1, k + 1); vh.AddTriangle(k - 2, k + 1, k);
            }
        }
    }

    /// <summary>Colours a Text's glyphs from top to bottom (the area titles' cream-to-straw lettering).</summary>
    public sealed class RoyalTextGradient : BaseMeshEffect
    {
        public Color top = Color.white, bottom = Color.white;

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || vh.currentVertCount == 0) return;
            var v = new UIVertex(); float min = float.MaxValue, max = float.MinValue;
            for (int i = 0; i < vh.currentVertCount; i++) { vh.PopulateUIVertex(ref v, i); min = Mathf.Min(min, v.position.y); max = Mathf.Max(max, v.position.y); }
            for (int i = 0; i < vh.currentVertCount; i++)
            {
                vh.PopulateUIVertex(ref v, i);
                v.color = Color.Lerp(bottom, top, Mathf.InverseLerp(min, max, v.position.y)) * (Color)v.color;
                vh.SetUIVertex(v, i);
            }
        }
    }
}
