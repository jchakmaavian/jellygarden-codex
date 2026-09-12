using UnityEngine;
using UnityEngine.UI;

namespace JellyGarden.Scripts.Story
{
    /// <summary>A vertex tint, so soft UI gradients need no extra texture or material.</summary>
    [DisallowMultipleComponent]
    public sealed class StoryCastleGradient : BaseMeshEffect
    {
        public Color Top = Color.white;
        public Color Bottom = Color.white;

        public override void ModifyMesh(VertexHelper mesh)
        {
            if (!IsActive()) return;
            Rect rect = graphic.rectTransform.rect;
            UIVertex vertex = default(UIVertex);
            for (int i = 0; i < mesh.currentVertCount; i++)
            {
                mesh.PopulateUIVertex(ref vertex, i);
                float t = Mathf.InverseLerp(rect.yMin, rect.yMax, vertex.position.y);
                vertex.color = (Color)vertex.color * Color.Lerp(Bottom, Top, t);
                mesh.SetUIVertex(vertex, i);
            }
        }
    }
}
