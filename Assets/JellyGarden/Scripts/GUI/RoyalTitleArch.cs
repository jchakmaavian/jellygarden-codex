using UnityEngine;
using UnityEngine.UI;

namespace RoyalThrone.Presentation
{
    // Bends one title letter as a slice of a whole-word arc, like the reference lettering: letters stay upright, the top
    // edge curves over the middle, the ends sit a little lower and shorter, and their strokes lean gently outward. The bend
    // uses the letter's rest place in the title, so the reveal's scale and wobble play on top of a settled arc.
    [RequireComponent(typeof(RawImage))]
    public sealed class RoyalTitleArch : BaseMeshEffect
    {
        const int Columns = 8, Rows = 4;
        // center: the letter's rest x in the title. sag: baseline drop at the title ends, in title units.
        // shrink: share of height the ends lose. fan: outward lean of the ends' strokes per unit of height.
        public float center, halfWidth = 1, sag, shrink, fan;

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || vh.currentVertCount < 4 || halfWidth <= 0) return;
            var v = new UIVertex(); vh.PopulateUIVertex(ref v, 0);
            Vector2 min = v.position, max = v.position, uvMin = v.uv0, uvMax = v.uv0; Color32 color = v.color;
            for (int i = 1; i < 4; i++)
            {
                vh.PopulateUIVertex(ref v, i);
                min = Vector2.Min(min, v.position); max = Vector2.Max(max, v.position);
                uvMin = Vector2.Min(uvMin, v.uv0); uvMax = Vector2.Max(uvMax, v.uv0);
            }
            vh.Clear();
            for (int r = 0; r <= Rows; r++)
                for (int c = 0; c <= Columns; c++)
                {
                    float u = (float)c / Columns, w = (float)r / Rows;
                    float x = Mathf.Lerp(min.x, max.x, u), up = (max.y - min.y) * w;
                    float t = (center + x) / halfWidth, bend = t * t;
                    var position = new Vector3(x + fan * t * up, min.y + up * (1 - shrink * bend) - sag * bend);
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
