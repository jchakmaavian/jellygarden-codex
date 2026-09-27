using UnityEngine;
using UnityEngine.UI;

namespace RoyalThrone.Presentation
{
    // An arched panel for the gameplay HUD: a rectangle whose top edge rises into a shallow arc between its top corners
    // (`bulge`, the Goal and Moves headers), or whose top corners grow into a tall half-ellipse (`topRadius` half the width,
    // `topRise` its height: the king's portrait window). Rounded bottom corners, a two-colour vertical gradient and
    // anti-aliased edges, filled or as a band along the edge, like RoyalCardShape. The rect encloses the whole shape: the
    // arc's peak touches its top edge.
    [AddComponentMenu("Royal Throne/Arch Shape")]
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RoyalArchShape : MaskableGraphic
    {
        // Top and bottom colours.
        public Color from = Color.white, to = Color.white;
        // The top corners' horizontal and vertical radii.
        [Min(0)] public float topRadius = 12, topRise = 12;
        // How far the top edge rises in the middle, between the top corners.
        [Min(0)] public float bulge;
        [Min(0)] public float bottomRadius = 12;
        // 0 fills the shape; otherwise only a band this wide along the edge is drawn.
        [Min(0)] public float band;
        // 0 anti-aliases the outer edge over one pixel; otherwise the edge fades over this width (glows, shadows).
        [Min(0)] public float softness;
        // The same for a band's inner edge.
        [Min(0)] public float innerSoftness;

        const int MaxCornerSteps = 32, FalloffRings = 4;
        static readonly float[] ringInset = new float[2 * FalloffRings + 2], ringAlpha = new float[2 * FalloffRings + 2];
        int topSteps, bottomSteps, arcSteps;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect rect = GetPixelAdjustedRect();
            if (rect.width <= 0 || rect.height <= 0) return;
            float pixel = canvas ? 1f / Mathf.Max(.01f, canvas.scaleFactor) : 1f;
            float half = rect.width * .5f;
            float rx = Mathf.Min(topRadius, half), ry = Mathf.Min(topRise, rect.height - bulge), rb = Mathf.Min(bottomRadius, half);
            topSteps = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(rx, ry) / pixel / 3f), 2, MaxCornerSteps);
            bottomSteps = Mathf.Clamp(Mathf.CeilToInt(rb / pixel / 3f), 1, MaxCornerSteps);
            float span = rect.width - 2 * rx;
            arcSteps = bulge > 0 && span > pixel ? Mathf.Clamp(Mathf.CeilToInt(span / pixel / 6f), 4, 48) : 0;

            int rings = 0;
            if (softness > 0) AddFalloff(ref rings, -softness * .5f, softness * .5f, true, true);
            else AddFalloff(ref rings, -pixel * .5f, pixel * .5f, false, true);
            if (band > 0)
            {
                if (innerSoftness > 0) AddFalloff(ref rings, band, band + innerSoftness, true, false);
                else AddFalloff(ref rings, band - pixel * .5f, band + pixel * .5f, false, false);
            }

            int perRing = 2 * (topSteps + 1) + 2 * (bottomSteps + 1) + Mathf.Max(0, arcSteps - 1);
            for (int k = 0; k < rings; k++)
            {
                Outline(vh, rect, rx, ry, rb, ringInset[k], ringAlpha[k]);
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
            int centre = rings * perRing, last = (rings - 1) * perRing;
            AddVertex(vh, rect, new Vector2(rect.center.x, rect.yMin + (rect.height - bulge) * .5f), 1);
            for (int i = 0; i < perRing; i++) vh.AddTriangle(centre, last + i, last + (i + 1) % perRing);
        }

        // One ring of the outline, `inset` inside the shape's edge (negative: outside), clockwise from the top of the top-right
        // corner: that corner, the bottom-right, the bottom-left, the top-left, then the arc back to the start. Every ring has
        // the same points in the same order, so neighbouring rings join into quads.
        void Outline(VertexHelper vh, Rect rect, float rx0, float ry0, float rb0, float inset, float alpha)
        {
            float left = rect.xMin + inset, right = rect.xMax - inset, bottom = rect.yMin + inset;
            float side = rect.yMax - bulge - inset;   // the top edge at the corners
            float rx = Mathf.Max(0, rx0 - inset), ry = Mathf.Max(0, ry0 - inset), rb = Mathf.Max(0, rb0 - inset);
            float half = Mathf.Max(0, (right - left) * .5f);
            rx = Mathf.Min(rx, half); rb = Mathf.Min(rb, half);
            for (int s = 0; s <= topSteps; s++)
            {
                float a = Mathf.PI * .5f * (1 - s / (float)topSteps);
                AddVertex(vh, rect, new Vector2(right - rx + Mathf.Cos(a) * rx, side - ry + Mathf.Sin(a) * ry), alpha);
            }
            for (int s = 0; s <= bottomSteps; s++)
            {
                float a = -Mathf.PI * .5f * s / bottomSteps;
                AddVertex(vh, rect, new Vector2(right - rb + Mathf.Cos(a) * rb, bottom + rb + Mathf.Sin(a) * rb), alpha);
            }
            for (int s = 0; s <= bottomSteps; s++)
            {
                float a = -Mathf.PI * .5f - Mathf.PI * .5f * s / bottomSteps;
                AddVertex(vh, rect, new Vector2(left + rb + Mathf.Cos(a) * rb, bottom + rb + Mathf.Sin(a) * rb), alpha);
            }
            for (int s = 0; s <= topSteps; s++)
            {
                float a = Mathf.PI - Mathf.PI * .5f * s / topSteps;
                AddVertex(vh, rect, new Vector2(left + rx + Mathf.Cos(a) * rx, side - ry + Mathf.Sin(a) * ry), alpha);
            }
            // The shallow arc between the top corners (a parabola through its ends and its peak).
            float x0 = left + rx, x1 = right - rx;
            for (int s = 1; s < arcSteps; s++)
            {
                float t = s / (float)arcSteps, u = 2 * t - 1;
                AddVertex(vh, rect, new Vector2(Mathf.Lerp(x0, x1, t), side + bulge * (1 - u * u)), alpha);
            }
        }

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

        void AddVertex(VertexHelper vh, Rect rect, Vector2 point, float alpha)
        {
            Color shade = Color.Lerp(to, from, Mathf.InverseLerp(rect.yMin, rect.yMax, point.y));
            shade *= color;
            shade.a *= alpha;
            vh.AddVert(point, shade, Vector2.zero);
        }
    }
}
