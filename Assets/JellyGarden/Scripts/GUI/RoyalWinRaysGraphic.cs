using UnityEngine;
using UnityEngine.UI;

namespace RoyalThrone.Presentation
{
    // A soft radial fan behind the reward star. No texture, material instance or per-frame allocation.
    public sealed class RoyalWinRaysGraphic : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect r = GetPixelAdjustedRect();
            float radius = Mathf.Min(r.width, r.height) * .5f;
            const int rays = 10, rings = 12;
            for (int ray = 0; ray < rays; ray++)
            {
                float angle = ray * Mathf.PI * 2 / rays;
                int first = vh.currentVertCount;
                for (int ring = 0; ring <= rings; ring++)
                {
                    float u = ring / (float)rings;
                    float distance = radius * u;
                    Color shade = new Color(1, .88f, .52f, .32f * Mathf.Sin(Mathf.PI * u) * (1 - u)) * color;
                    for (int side = 0; side < 2; side++)
                    {
                        float a = angle + (side == 0 ? -.095f : .095f);
                        vh.AddVert(r.center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * distance, shade, Vector2.zero);
                    }
                    if (ring == 0) continue;
                    int i = first + ring * 2;
                    vh.AddTriangle(i - 2, i - 1, i + 1);
                    vh.AddTriangle(i - 2, i + 1, i);
                }
            }
        }
    }
}
