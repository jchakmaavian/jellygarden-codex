using UnityEngine;
using UnityEngine.UI;

namespace RoyalThrone.Presentation
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RoyalWaterGlow : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect rect = GetPixelAdjustedRect();
            mesh.AddVert(rect.center, color, Vector2.zero);
            Color transparent = color; transparent.a = 0;
            const int steps = 48;
            for (int i = 0; i < steps; i++)
            {
                float angle = i * 2 * Mathf.PI / steps;
                mesh.AddVert(rect.center + new Vector2(Mathf.Cos(angle) * rect.width * .5f, Mathf.Sin(angle) * rect.height * .5f), transparent, Vector2.zero);
            }
            for (int i = 0; i < steps; i++) mesh.AddTriangle(0, i + 1, (i + 1) % steps + 1);
        }
    }
}
