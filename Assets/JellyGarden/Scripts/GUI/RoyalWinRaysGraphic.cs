using UnityEngine;
using UnityEngine.UI;

namespace RoyalThrone.Presentation
{
    // Feathered gold rays behind the reward. All edges fade; the star face stays unobscured.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RoyalWinRaysGraphic : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = GetPixelAdjustedRect();
            float radius = Mathf.Min(r.width, r.height) * .5f;
            const int rays = 18, rings = 14, sides = 4;
            for (int ray = 0; ray < rays; ray++)
            {
                float angle = ray * Mathf.PI * 2 / rays;
                float reach = radius * (ray % 3 == 0 ? 1 : .87f);
                float spread = ray % 2 == 0 ? .13f : .065f;
                int first = vh.currentVertCount;
                for (int ring = 0; ring <= rings; ring++)
                {
                    float u = ring / (float)rings;
                    float envelope = Mathf.Sin(Mathf.PI * u) * Mathf.Pow(1 - u, .55f);
                    for (int side = 0; side <= sides; side++)
                    {
                        float v = side / (float)sides;
                        float a = angle + Mathf.Lerp(-spread, spread, v);
                        var shade = new Color(1, .73f + .16f * (1 - u), .22f, .96f * envelope * Mathf.Sin(Mathf.PI * v)) * color;
                        vh.AddVert(r.center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * reach * u, shade, Vector2.zero);
                    }
                }
                for (int ring = 0; ring < rings; ring++)
                    for (int side = 0; side < sides; side++)
                    {
                        int a = first + ring * (sides + 1) + side, b = a + sides + 1;
                        vh.AddTriangle(a, b, b + 1); vh.AddTriangle(a, b + 1, a + 1);
                    }
            }
        }
    }
}