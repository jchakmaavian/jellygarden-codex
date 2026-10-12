using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;

namespace RoyalThrone.Presentation
{
    // Reuses the original light-wheel texture. Vertex alpha softens its outside edge;
    // rotating the RectTransform does not rebuild the mesh each frame.
    [RequireComponent(typeof(CanvasRenderer))]
    [AddComponentMenu("Royal Throne/Logo Halo")]
    public sealed class RoyalLogoHalo : MaskableGraphic
    {
        public Sprite sprite;
        [Min(1)] public float rotationSeconds = 6;
        public override Texture mainTexture => sprite ? sprite.texture : base.mainTexture;

        void Update()
        {
            if (Application.isPlaying)
                rectTransform.localRotation = Quaternion.Euler(0, 0,
                    -Mathf.Repeat(Time.unscaledTime / Mathf.Max(1, rotationSeconds), 1) * 360);
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if (!sprite) return;
            Rect rect = GetPixelAdjustedRect();
            Vector4 uv = DataUtility.GetOuterUV(sprite);
            const int divisions = 24;
            for (int y = 0; y <= divisions; y++)
            for (int x = 0; x <= divisions; x++)
            {
                float u = x / (float)divisions, v = y / (float)divisions;
                float radius = Vector2.Distance(new Vector2(u, v), Vector2.one * .5f);
                Color tint = color;
                tint.a *= 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.18f, .5f, radius));
                mesh.AddVert(new Vector3(Mathf.Lerp(rect.xMin, rect.xMax, u),
                    Mathf.Lerp(rect.yMin, rect.yMax, v)), tint,
                    new Vector2(Mathf.Lerp(uv.x, uv.z, u), Mathf.Lerp(uv.y, uv.w, v)));
            }
            for (int y = 0; y < divisions; y++)
            for (int x = 0; x < divisions; x++)
            {
                int a = y * (divisions + 1) + x;
                mesh.AddTriangle(a, a + divisions + 1, a + 1);
                mesh.AddTriangle(a + 1, a + divisions + 1, a + divisions + 2);
            }
        }
    }
}
