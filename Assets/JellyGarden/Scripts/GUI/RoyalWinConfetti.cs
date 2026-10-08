using UnityEngine;
using UnityEngine.UI;

namespace RoyalThrone.Presentation
{
    // Gold and ivory star particles in bounded side lanes, using one reusable UI mesh.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RoyalWinConfetti : MaskableGraphic
    {
        public Vector2 origin;
        float age;
        bool calm;
        public int VisibleSparkles { get; private set; }

        public void Pose(float seconds, bool reduced)
        {
            if (age == seconds && calm == reduced) return;
            age = seconds;
            calm = reduced;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            VisibleSparkles = 0;
            float reveal = Mathf.SmoothStep(0, 1, Mathf.Clamp01(age / .3f));
            if (reveal <= 0) return;
            float clock = calm ? 0 : Mathf.Max(0, age - .22f);

            // Sparse, staggered glints remain after the initial celebration.
            for (int i = 0; i < 10; i++)
            {
                float side = i % 2 == 0 ? -1 : 1;
                float phase = i * 2.39996f;
                float pulse = calm ? .65f : Mathf.Pow(.5f + .5f * Mathf.Sin(clock * 2.1f + phase), 3);
                float size = (i % 3 == 0 ? 10 : 6) * (.65f + .35f * pulse);
                Vector2 centre = origin + new Vector2(side * (216 + (i % 3) * 18),
                    118 - (i / 2) * 65 + (calm ? 0 : 5 * Mathf.Sin(clock * .8f + phase)));
                Star(vh, centre, size, i * 17, reveal * (.18f + .82f * pulse));
            }
            if (calm) return;

            // Short outward burst; no paper shapes or colored ribbons.
            for (int i = 0; i < 22; i++)
            {
                float u = (clock - i * .018f) / (1.45f + .15f * (i % 4));
                if (u <= 0 || u >= 1) continue;
                float side = i % 2 == 0 ? -1 : 1;
                float x = side * (178 + (i % 4) * 13 + 34 * u);
                float y = -155 + (i % 7) * 43 + 56 * u - 20 * u * u;
                float alpha = reveal * Mathf.Sin(Mathf.PI * u);
                Star(vh, origin + new Vector2(x, y), (4 + i % 4) * Mathf.Sin(Mathf.PI * u),
                    i * 31 + u * 35, alpha);
            }
        }

        void Star(VertexHelper vh, Vector2 centre, float radius, float degrees, float alpha)
        {
            if (alpha <= .01f || radius <= .01f) return;
            VisibleSparkles++;
            float angle = degrees * Mathf.Deg2Rad;
            // A soft golden halo sits below a crisp four-point ivory star.
            Fan(vh, centre, radius * 2.1f, angle, new Color(1, .73f, .22f, alpha * .19f), true);
            Fan(vh, centre, radius, angle, new Color(1, .94f, .65f, alpha), false);
            Fan(vh, centre, radius * .52f, angle, new Color(1, 1, .96f, alpha), false);
        }

        void Fan(VertexHelper vh, Vector2 centre, float radius, float angle, Color tint, bool halo)
        {
            int first = vh.currentVertCount;
            vh.AddVert(centre, tint * color, Vector2.zero);
            const int points = 8;
            for (int i = 0; i < points; i++)
            {
                float a = angle + i * Mathf.PI / 4;
                float r = radius * (halo ? 1 : (i % 2 == 0 ? 1 : .23f));
                Color edge = tint;
                if (halo) edge.a = 0;
                vh.AddVert(centre + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r, edge * color, Vector2.zero);
            }
            for (int i = 0; i < points; i++)
                vh.AddTriangle(first, first + 1 + i, first + 1 + (i + 1) % points);
        }
    }
}
