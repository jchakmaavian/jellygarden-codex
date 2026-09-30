using UnityEngine;
using UnityEngine.UI;

namespace RoyalThrone.Presentation
{
    // A soft, slanted band of light that a caller slides across a rounded shape (PLAY's gloss, the booster cards' glint).
    // The band is clipped to the same rounded outline as RoyalCardShape, so no mask is needed.
    [AddComponentMenu("Royal Throne/Sweep Highlight")]
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RoyalSweepGraphic : MaskableGraphic
    {
        [Min(0)] public float radius = 20;
        [Min(2)] public float exponent = 2;
        // Band centre across the shape, 0 at its left edge and 1 at its right; outside the shape nothing is drawn.
        public float position = -1;
        // Half the band's width, as a fraction of the shape's width.
        [Min(.01f)] public float width = .16f;
        // Horizontal lean of the band per unit of height, as a fraction of the shape's width.
        public float slant = .35f;
        [Range(0, 1)] public float strength;

        const int Columns = 40, Rows = 6;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect rect = GetPixelAdjustedRect();
            float reach = width + Mathf.Abs(slant) * rect.height / Mathf.Max(1, rect.width);
            if (strength <= 0 || rect.width <= 0 || rect.height <= 0 || position < -reach || position > 1 + reach) return;
            float r = Mathf.Min(radius, Mathf.Min(rect.width, rect.height) * .5f);
            for (int c = 0; c <= Columns; c++)
            {
                float x = rect.xMin + rect.width * c / Columns;
                float inset = CornerInset(rect, r, x);
                float y0 = rect.yMin + inset, y1 = rect.yMax - inset;
                for (int k = 0; k <= Rows; k++)
                {
                    float v = k / (float)Rows, y = Mathf.Lerp(y0, y1, v);
                    float u = (x - rect.xMin) / rect.width + slant * (y - rect.center.y) / rect.width;
                    float s = (u - position) / width, a = 0;
                    if (Mathf.Abs(s) < 1) { a = Mathf.Cos(s * Mathf.PI * .5f); a *= a; }
                    var shade = color;
                    shade.a *= strength * a * Mathf.Lerp(.55f, 1, v);   // a little brighter towards the top
                    vh.AddVert(new Vector3(x, y), shade, Vector2.zero);
                }
            }
            for (int c = 0; c < Columns; c++)
                for (int k = 0; k < Rows; k++)
                {
                    int a = c * (Rows + 1) + k, b = a + Rows + 1;
                    vh.AddTriangle(a, b, b + 1);
                    vh.AddTriangle(a, b + 1, a + 1);
                }
        }

        // How far the rounded outline sits in from the top and bottom edges at x.
        float CornerInset(Rect rect, float r, float x)
        {
            if (r <= 0) return 0;
            float dx = x < rect.xMin + r ? rect.xMin + r - x : x > rect.xMax - r ? x - (rect.xMax - r) : 0;
            if (dx <= 0) return 0;
            float q = Mathf.Clamp01(dx / r);
            return r - r * Mathf.Pow(Mathf.Max(0, 1 - Mathf.Pow(q, exponent)), 1f / exponent);
        }
    }
}
