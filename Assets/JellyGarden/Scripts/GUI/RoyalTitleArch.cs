using UnityEngine;
using UnityEngine.UI;

namespace RoyalThrone.Presentation
{
    // Bends a whole-word title image along a gentle arch, like the reference lettering: the word follows one circle, its
    // ends dip and lean outward while the middle keeps its height. Its outline stays continuous because the image is bent
    // as one piece. Widths are kept along the baseline arc, so the word reads at its original size.
    [RequireComponent(typeof(RawImage))]
    public sealed class RoyalTitleArch : BaseMeshEffect
    {
        const int Columns = 32, Rows = 2;
        // The arc's half-angle at the image's ends, in radians.
        public float angle = .1f;

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || vh.currentVertCount < 4 || angle <= 0) return;
            var v = new UIVertex(); vh.PopulateUIVertex(ref v, 0);
            Vector2 min = v.position, max = v.position, uvMin = v.uv0, uvMax = v.uv0; Color32 color = v.color;
            for (int i = 1; i < 4; i++)
            {
                vh.PopulateUIVertex(ref v, i);
                min = Vector2.Min(min, v.position); max = Vector2.Max(max, v.position);
                uvMin = Vector2.Min(uvMin, v.uv0); uvMax = Vector2.Max(uvMax, v.uv0);
            }
            float mid = (min.x + max.x) * .5f, radius = (max.x - min.x) * .5f / angle;
            vh.Clear();
            for (int r = 0; r <= Rows; r++)
                for (int c = 0; c <= Columns; c++)
                {
                    float u = (float)c / Columns, w = (float)r / Rows;
                    float a = (Mathf.Lerp(min.x, max.x, u) - mid) / radius, reach = radius + (max.y - min.y) * w;
                    var position = new Vector3(mid + reach * Mathf.Sin(a), min.y + reach * Mathf.Cos(a) - radius);
                    vh.AddVert(position, color, new Vector4(Mathf.Lerp(uvMin.x, uvMax.x, u), Mathf.Lerp(uvMin.y, uvMax.y, w)));
                }
            for (int r = 0; r < Rows; r++)
                for (int c = 0; c < Columns; c++)
                {
                    int a = r * (Columns + 1) + c, b = a + Columns + 1;
                    vh.AddTriangle(a, b, b + 1); vh.AddTriangle(b + 1, a + 1, a);
                }
        }
    }
}
