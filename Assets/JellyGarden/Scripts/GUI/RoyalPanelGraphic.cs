using UnityEngine;
using UnityEngine.UI;

namespace RoyalThrone.Presentation
{
    // Resolution-independent UI surfaces: the text, trim and fill remain editable.
    [AddComponentMenu("Royal Throne/Gradient Panel")]
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RoyalPanelGraphic : MaskableGraphic
    {
        public Color topColor = Color.white;
        public Color bottomColor = Color.white;
        [Min(0)] public float cornerRadius = 24;
        public bool diamond;

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect rect = GetPixelAdjustedRect();
            AddVertex(mesh, rect.center, rect);
            if (diamond)
            {
                AddVertex(mesh, new Vector2(rect.center.x, rect.yMax), rect);
                AddVertex(mesh, new Vector2(rect.xMax, rect.center.y), rect);
                AddVertex(mesh, new Vector2(rect.center.x, rect.yMin), rect);
                AddVertex(mesh, new Vector2(rect.xMin, rect.center.y), rect);
                for (int i = 0; i < 4; i++) mesh.AddTriangle(0, i + 1, (i + 1) % 4 + 1);
                return;
            }
            float radius = Mathf.Min(cornerRadius, Mathf.Min(rect.width, rect.height) * .5f);
            const int steps = 10;
            for (int corner = 0; corner < 4; corner++)
            {
                Vector2 center = new Vector2(corner == 0 || corner == 3 ? rect.xMax - radius : rect.xMin + radius,
                    corner < 2 ? rect.yMax - radius : rect.yMin + radius);
                for (int step = 0; step <= steps; step++)
                {
                    float angle = (corner * 90f + step * 90f / steps) * Mathf.Deg2Rad;
                    AddVertex(mesh, center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius, rect);
                }
            }
            int count = 4 * (steps + 1);
            for (int i = 0; i < count; i++) mesh.AddTriangle(0, i + 1, (i + 1) % count + 1);
        }

        void AddVertex(VertexHelper mesh, Vector2 point, Rect rect)
        {
            float t = Mathf.InverseLerp(rect.yMin, rect.yMax, point.y);
            Color shade = Color.Lerp(bottomColor, topColor, t) * color;
            mesh.AddVert(point, shade, Vector2.zero);
        }
    }
}
