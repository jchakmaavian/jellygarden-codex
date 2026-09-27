using UnityEngine;
using UnityEngine.UI;

namespace RoyalThrone.Presentation
{
    // The Level Start card's drawn surfaces: a rounded rectangle (circular or superelliptic corners) with a two-colour
    // gradient and anti-aliased edges, either filled or as a band along its edge, with an optional soft falloff for glows
    // and shadows. RoyalPanelGraphic stays as it is for the other popups; this one adds the one-pixel edge fringe that keeps
    // thin gold rims smooth.
    [AddComponentMenu("Royal Throne/Card Shape")]
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RoyalCardShape : MaskableGraphic
    {
        // Top and bottom colours, or left and right when horizontal.
        public Color from = Color.white, to = Color.white;
        public bool horizontal;
        [Min(0)] public float radius = 20;
        // 2 gives circular corners; larger values square the corners off into a squircle.
        [Min(2)] public float exponent = 2;
        // 0 fills the shape; otherwise only a band this wide along the edge is drawn.
        [Min(0)] public float band;
        // 0 anti-aliases the outer edge over one pixel; otherwise the edge fades over this width (glows, shadows).
        [Min(0)] public float softness;
        // The same for a band's inner edge.
        [Min(0)] public float innerSoftness;

        const int MaxCornerSteps = 24, FalloffRings = 4;
        static readonly float[] ringInset = new float[2 * FalloffRings + 2], ringAlpha = new float[2 * FalloffRings + 2];

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect rect = GetPixelAdjustedRect();
            if (rect.width <= 0 || rect.height <= 0) return;
            float pixel = canvas ? 1f / Mathf.Max(.01f, canvas.scaleFactor) : 1f;
            float r = Mathf.Min(radius, Mathf.Min(rect.width, rect.height) * .5f);
            int steps = Mathf.Clamp(Mathf.CeilToInt(r / pixel / 3f), 2, MaxCornerSteps);

            // Rings from the outside in, each an inset from the shape's edge with its alpha. The outer edge fades across the
            // shape's boundary; a band's soft inner edge starts at the band and fades inwards (an inner glow).
            int rings = 0;
            if (softness > 0) AddFalloff(ref rings, -softness * .5f, softness * .5f, true, true);
            else AddFalloff(ref rings, -pixel * .5f, pixel * .5f, false, true);
            if (band > 0)
            {
                if (innerSoftness > 0) AddFalloff(ref rings, band, band + innerSoftness, true, false);
                else AddFalloff(ref rings, band - pixel * .5f, band + pixel * .5f, false, false);
            }

            int perRing = 4 * (steps + 1);
            for (int k = 0; k < rings; k++)
            {
                for (int corner = 0; corner < 4; corner++)
                    for (int s = 0; s <= steps; s++)
                        AddVertex(vh, rect, CornerPoint(rect, r, ringInset[k], corner, s / (float)steps), ringAlpha[k]);
                if (k == 0) continue;
                int a = (k - 1) * perRing, b = k * perRing;
                for (int i = 0; i < perRing; i++)
                {
                    int j = (i + 1) % perRing;
                    vh.AddTriangle(a + i, a + j, b + j);
                    vh.AddTriangle(a + i, b + j, b + i);
                }
            }
            if (band > 0) return;
            // Filled: a fan from the centre to the innermost ring.
            int centre = rings * perRing, last = (rings - 1) * perRing;
            AddVertex(vh, rect, rect.center, 1);
            for (int i = 0; i < perRing; i++) vh.AddTriangle(centre, last + i, last + (i + 1) % perRing);
        }

        // An edge between two insets: alpha rises from 0 to 1 inwards (fadeIn) or falls from 1 to 0, smoothly or linearly.
        static void AddFalloff(ref int rings, float start, float end, bool smooth, bool fadeIn)
        {
            int count = smooth ? FalloffRings : 1;
            for (int i = 0; i <= count; i++)
            {
                float t = i / (float)count;
                ringInset[rings] = Mathf.Lerp(start, end, t);
                float a = smooth ? t * t * (3 - 2 * t) : t;
                ringAlpha[rings] = fadeIn ? a : 1 - a;
                rings++;
            }
        }

        // A point on the corner arc of the shape inset by `inset` (negative: outset). Corners run counter-clockwise from
        // the top right; `t` walks each arc from its first side to its second.
        Vector2 CornerPoint(Rect rect, float r, float inset, int corner, float t)
        {
            float ri = Mathf.Max(0, r - inset);
            float half = Mathf.Max(0, Mathf.Min(rect.width, rect.height) * .5f - inset);
            ri = Mathf.Min(ri, half);
            float left = rect.xMin + inset + ri, right = rect.xMax - inset - ri;
            float bottom = rect.yMin + inset + ri, top = rect.yMax - inset - ri;
            if (left > right) left = right = rect.center.x;
            if (bottom > top) bottom = top = rect.center.y;
            float angle = (corner * 90f + t * 90f) * Mathf.Deg2Rad;
            float c = Mathf.Cos(angle), s = Mathf.Sin(angle);
            float power = 2f / exponent;
            float x = Mathf.Sign(c) * Mathf.Pow(Mathf.Abs(c), power), y = Mathf.Sign(s) * Mathf.Pow(Mathf.Abs(s), power);
            var centre = new Vector2(corner == 0 || corner == 3 ? right : left, corner < 2 ? top : bottom);
            return centre + new Vector2(x, y) * ri;
        }

        void AddVertex(VertexHelper vh, Rect rect, Vector2 point, float alpha)
        {
            Color shade = horizontal
                ? Color.Lerp(from, to, Mathf.InverseLerp(rect.xMin, rect.xMax, point.x))
                : Color.Lerp(to, from, Mathf.InverseLerp(rect.yMin, rect.yMax, point.y));
            shade *= color;
            shade.a *= alpha;
            vh.AddVert(point, shade, Vector2.zero);
        }
    }
}
