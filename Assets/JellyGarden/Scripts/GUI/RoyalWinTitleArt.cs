using UnityEngine;
using UnityEngine.UI;

namespace RoyalThrone.Presentation
{
    // One continuous surface preserves the approved connected gold lettering.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RoyalWinTitleArt : MaskableGraphic
    {
        public Texture2D artwork;
        public const int Columns = 160, Rows = 8;
        static readonly float[] Centers = { .113f, .259f, .33f, .385f, .535f, .635f, .72f, .812f, .928f };
        static readonly int[] Beats = { 0, 1, 2, 3, 5, 6, 7, 8, 9 };
        float progress;
        public override Texture mainTexture => artwork ? artwork : base.mainTexture;

        public void Write(float letters)
        {
            if (progress == letters) return;
            progress = letters;
            SetVerticesDirty();
        }

        float Pulse(int index)
        {
            float u = (progress - Beats[index]) / 10f;
            if (u <= 0 || u >= 1) return 0;
            float peak = index == Centers.Length - 1 ? .20f : .16f;
            if (u < .25f) return Mathf.Lerp(0, peak, Smooth(u / .25f));
            if (u < .75f) return Mathf.Lerp(peak, -.035f, Smooth((u - .25f) / .5f));
            return Mathf.Lerp(-.035f, 0, Smooth((u - .75f) / .25f));
        }

        static float Smooth(float u) => u * u * (3 - 2 * u);

        float Wave(float x)
        {
            if (x <= Centers[0]) return Pulse(0);
            for (int i = 1; i < Centers.Length; i++)
                if (x <= Centers[i]) return Mathf.Lerp(Pulse(i - 1), Pulse(i), Smooth(Mathf.InverseLerp(Centers[i - 1], Centers[i], x)));
            return Pulse(Centers.Length - 1);
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = rectTransform.rect;
            var tint = color; tint.a *= Mathf.Clamp01(progress / .8f);
            for (int y = 0; y <= Rows; y++)
                for (int x = 0; x <= Columns; x++)
                {
                    float u = x / (float)Columns, v = y / (float)Rows;
                    float warped = v + (v - .13f) * Wave(u);
                    vh.AddVert(new Vector3(r.xMin + u * r.width, r.yMin + warped * r.height), tint, new Vector2(u, v));
                }
            for (int y = 0; y < Rows; y++)
                for (int x = 0; x < Columns; x++)
                {
                    int a = y * (Columns + 1) + x, b = a + Columns + 1;
                    vh.AddTriangle(a, b, b + 1); vh.AddTriangle(a, b + 1, a + 1);
                }
        }
    }
}
