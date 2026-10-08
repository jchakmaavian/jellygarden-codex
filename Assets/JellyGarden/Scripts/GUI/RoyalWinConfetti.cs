using UnityEngine;
using UnityEngine.UI;

namespace RoyalThrone.Presentation
{
    // Curled blue/gold ribbon mesh. One renderer, bounded side lanes, no textures or spawned particles.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RoyalWinConfetti : MaskableGraphic
    {
        public Vector2 origin;
        float age;
        bool calm;
        public int VisibleRibbons { get; private set; }

        public void Pose(float seconds, bool reduced)
        {
            if (age == seconds && calm == reduced) return;
            age = seconds; calm = reduced; SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); VisibleRibbons = 0;
            float reveal = Mathf.SmoothStep(0, 1, Mathf.Clamp01((age - .22f) / .30f));
            if (calm) reveal = Mathf.Clamp01(age / .20f);
            if (reveal <= 0) return;
            float clock = calm ? 0 : Mathf.Max(0, age - .22f);
            for (int i = 0; i < 4; i++)
            {
                float side = i % 2 == 0 ? -1 : 1;
                float phase = i * 1.7f;
                Vector2 centre = origin + new Vector2(side * (235 + 9 * Mathf.Sin(clock * 1.1f + phase)),
                    (i < 2 ? 78 : -132) + 15 * Mathf.Sin(clock * .95f + phase));
                Ribbon(vh, centre, 48, 18, side * 30 + 22 * Mathf.Sin(clock * 1.5f + phase), clock * 2 + phase, i == 0 || i == 3, reveal);
            }
            if (calm) return;
            for (int i = 0; i < 18; i++)
            {
                float u = (clock - i * .025f) / (1.8f + .3f * (i % 3));
                if (u <= 0 || u >= 1) continue;
                float side = i % 2 == 0 ? -1 : 1;
                float x = side * (205 + (i % 4) * 17 + 7 * Mathf.Sin(u * 5 + i));
                float y = 60 + (i % 5) * 10 + 65 * Mathf.Sin(Mathf.PI * u) - 300 * u;
                float alpha = reveal * Mathf.Clamp01(u * 12) * Mathf.Clamp01((1 - u) * 4);
                Ribbon(vh, origin + new Vector2(x, y), 12 + i % 3 * 4, 5, i * 41 + u * 250, i + u * 9, i % 3 != 0, alpha);
            }
        }

        void Ribbon(VertexHelper vh, Vector2 centre, float length, float width, float degrees, float turn, bool gold, float alpha)
        {
            const int steps = 16;
            float radians = degrees * Mathf.Deg2Rad, cs = Mathf.Cos(radians), sn = Mathf.Sin(radians);
            float fold = .82f + .18f * Mathf.Cos(turn);
            int first = vh.currentVertCount; VisibleRibbons++;
            for (int i = 0; i <= steps; i++)
            {
                float u = i / (float)steps, curve = (u - .5f) * 2f;
                Vector2 mid = new Vector2(((1 - Mathf.Cos(curve)) * .72f - .16f) * length, Mathf.Sin(curve) * length * .62f);
                Vector2 normal = new Vector2(.62f * Mathf.Cos(curve), -.72f * Mathf.Sin(curve)).normalized;
                float light = .5f + .5f * Mathf.Cos(curve * 1.6f + turn * .3f);
                Color dark = gold ? new Color(.78f, .30f, .015f) : new Color(.015f, .18f, .80f);
                Color bright = gold ? new Color(1, .94f, .48f) : new Color(.30f, .92f, 1);
                Color shade = Color.Lerp(dark, bright, light);
                if (light > .93f) shade = Color.Lerp(shade, new Color(1, 1, .9f), (light - .93f) * 9);
                shade.a = alpha;
                for (int edge = 0; edge < 2; edge++)
                {
                    Vector2 p = mid + normal * ((edge == 0 ? -1 : 1) * width * .5f);
                    p.x *= fold;
                    p = centre + new Vector2(p.x * cs - p.y * sn, p.x * sn + p.y * cs);
                    vh.AddVert(p, shade * color, Vector2.zero);
                }
                if (i == 0) continue;
                int v = first + i * 2;
                vh.AddTriangle(v - 2, v - 1, v + 1); vh.AddTriangle(v - 2, v + 1, v);
            }
        }
    }
}
