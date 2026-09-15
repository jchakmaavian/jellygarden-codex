using UnityEngine;
using UnityEngine.UI;

namespace RoyalThrone.Match3Vfx
{
    /// <summary>Vertical gold gradient on banner lettering. Outline and shadow vertices keep their dark colour.</summary>
    [AddComponentMenu("")]
    public sealed class ComboBannerGradient : BaseMeshEffect
    {
        public Color top = new Color(1f, 1f, 0.9f);
        public Color bottom = new Color(1f, 0.7f, 0.17f);

        public override void ModifyMesh(VertexHelper vertices)
        {
            if (!IsActive() || vertices.currentVertCount == 0) return;
            var vertex = new UIVertex();
            float min = float.PositiveInfinity, max = float.NegativeInfinity;
            for (int i = 0; i < vertices.currentVertCount; i++)
            {
                vertices.PopulateUIVertex(ref vertex, i);
                min = Mathf.Min(min, vertex.position.y);
                max = Mathf.Max(max, vertex.position.y);
            }
            for (int i = 0; i < vertices.currentVertCount; i++)
            {
                vertices.PopulateUIVertex(ref vertex, i);
                if (vertex.color.r > 180 && vertex.color.g > 150)
                {
                    Color32 c = Color.Lerp(bottom, top, Mathf.InverseLerp(min, max, vertex.position.y));
                    c.a = vertex.color.a;
                    vertex.color = c;
                }
                vertices.SetUIVertex(vertex, i);
            }
        }
    }
}
