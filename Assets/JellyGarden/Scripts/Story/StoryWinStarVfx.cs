// One small UI mesh for the halo, rays, confetti and glints. No particle
// prefabs, runtime textures or per-frame object allocations are needed.
using UnityEngine;
using UnityEngine.UI;

namespace JellyGarden.Scripts.Story
{
    public sealed class StoryWinStarVfx : MaskableGraphic
    {
        private RectTransform _star;
        private Vector2 _rest;
        private float _began, _elapsed = 3f;
        private bool _playing;

        public void SetStar(RectTransform star)
        {
            _star = star;
            _rest = star.anchoredPosition;
        }

        public void Play(float began)
        {
            _began = began;
            _playing = true;
            Tick(0f);
        }

        public void Settle()
        {
            _playing = false;
            Tick(3f);
            if (_star == null) return;
            _star.localScale = Vector3.one;
            _star.localRotation = Quaternion.identity;
            _star.anchoredPosition = _rest;
        }

        private void Update()
        {
            if (_playing) Tick(Mathf.Max(0f, StoryClock.Now - _began));
        }

        private void Tick(float elapsed)
        {
            _elapsed = elapsed;
            if (_star != null)
            {
                float t = Mathf.Clamp01((elapsed - 0.14f) / 0.62f);
                float back = t - 1f;
                float entrance = 1f + 2.45f * back * back * back + 1.45f * back * back;
                float idle = Mathf.Max(0f, elapsed - 0.85f);
                float blend = Mathf.Clamp01(idle / 0.6f);
                float scale = Mathf.Max(0f, entrance) * (1f + Mathf.Sin(idle * 1.8f) * 0.018f * blend);
                _star.localScale = Vector3.one * scale;
                _star.localRotation = Quaternion.Euler(0f, 0f,
                    -18f * (1f - t) * (1f - t) + Mathf.Sin(idle * 1.35f) * 3f * blend);
                _star.anchoredPosition = _rest + new Vector2(0f,
                    -24f * (1f - t) + Mathf.Sin(idle * 1.8f) * 7f * blend);
            }
            SetVerticesDirty();
        }

        protected override void OnDisable()
        {
            _playing = false;
            base.OnDisable();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            float reveal = Mathf.Clamp01((_elapsed - 0.22f) / 0.5f);
            if (reveal <= 0f) return;
            float pulse = 0.92f + Mathf.Sin(_elapsed * 1.6f) * 0.08f;
            float radius = 315f * pulse;
            // Three feathered rings avoid a hard-edged disc under the star.
            Ring(vh, 0f, radius * 0.32f, 0.64f * reveal, 0.5f * reveal);
            Ring(vh, radius * 0.32f, radius * 0.68f, 0.5f * reveal, 0.16f * reveal);
            Ring(vh, radius * 0.68f, radius, 0.16f * reveal, 0f);
            for (int i = 0; i < 12; i++)
            {
                float a = i * Mathf.PI / 6f + _elapsed * 0.045f;
                int first = vh.currentVertCount;
                Add(vh, Polar(a - 0.05f, 72f), Tint(0.24f * reveal));
                Add(vh, Polar(a + 0.05f, 72f), Tint(0.24f * reveal));
                Add(vh, Polar(a + 0.075f, 324f), Tint(0f));
                Add(vh, Polar(a - 0.075f, 324f), Tint(0f));
                Quad(vh, first);
            }
            for (int i = 0; i < 7; i++)
            {
                float phase = Mathf.Repeat(_elapsed * 0.32f + i * 0.173f, 1f);
                float alpha = Mathf.Pow(Mathf.Sin(phase * Mathf.PI), 6f) * reveal;
                float angle = i * 2.39996f + 0.2f;
                Vector2 at = Polar(angle, 226f + (i % 3) * 25f);
                Sparkle(vh, at, (12f + (i % 3) * 5f) * (0.65f + alpha * 0.35f), alpha);
            }
            if (StorybookUi.Enabled)
            {
                // The reference keeps four bright glints around the reward,
                // including after the entrance burst has finished.
                for (int i = 0; i < 4; i++)
                {
                    Vector2 at = new Vector2(i % 2 == 0 ? -337 : 337, i < 2 ? 188 : -180);
                    float light = (.83f + .13f * Mathf.Sin(_elapsed * 1.7f + i)) * reveal;
                    Sparkle(vh, at, 24, light * .22f);
                    Sparkle(vh, at, 17, light);
                }
            }
            // A short local burst; it never drifts over the button or headline.
            float flight = Mathf.Clamp01((_elapsed - 0.3f) / 2.1f);
            if (flight > 0f && flight < 1f)
            {
                float alpha = Mathf.Sin(flight * Mathf.PI) * 0.95f;
                for (int i = 0; i < 12; i++)
                {
                    float angle = i * 2.39996f;
                    Vector2 at = Polar(angle, 120f + Mathf.Sqrt(flight) * (115f + i % 4 * 15f));
                    at.y -= flight * flight * 55f;
                    Confetti(vh, at, angle + flight * (i % 2 == 0 ? 5f : -5f),
                        new Color(1f, 0.72f + i % 3 * 0.09f, 0.16f, alpha));
                }
            }
        }

        private static Color Tint(float alpha) => new Color(1f, 0.81f, 0.22f, alpha);
        private static Vector2 Polar(float angle, float radius) =>
            new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;

        private static void Ring(VertexHelper vh, float inner, float outer, float inside, float outside)
        {
            const int segments = 64;
            for (int i = 0; i < segments; i++)
            {
                float a = i * 2f * Mathf.PI / segments, b = (i + 1) * 2f * Mathf.PI / segments;
                int start = vh.currentVertCount;
                Add(vh, Polar(a, inner), Tint(inside));
                Add(vh, Polar(b, inner), Tint(inside));
                Add(vh, Polar(b, outer), Tint(outside));
                Add(vh, Polar(a, outer), Tint(outside));
                Quad(vh, start);
            }
        }

        private static void Sparkle(VertexHelper vh, Vector2 at, float size, float alpha)
        {
            int start = vh.currentVertCount;
            Color light = new Color(1f, 1f, 0.85f, alpha);
            Add(vh, at, light);
            for (int i = 0; i <= 8; i++)
                Add(vh, at + Polar(i * Mathf.PI / 4f, i % 2 == 0 ? size : size * 0.18f), light);
            for (int i = 0; i < 8; i++) vh.AddTriangle(start, start + i + 1, start + i + 2);
        }

        private static void Confetti(VertexHelper vh, Vector2 at, float angle, Color color)
        {
            Vector2 x = Polar(angle, 7f), y = Polar(angle + Mathf.PI * 0.5f, 14f);
            int start = vh.currentVertCount;
            Add(vh, at - x - y, color);
            Add(vh, at + x - y, color);
            Add(vh, at + x + y, color);
            Add(vh, at - x + y, color);
            Quad(vh, start);
        }

        private static void Add(VertexHelper vh, Vector2 at, Color color)
        {
            vh.AddVert(new Vector3(at.x, at.y, 0f), color, Vector2.zero);
        }

        private static void Quad(VertexHelper vh, int start)
        {
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start, start + 2, start + 3);
        }
    }
}
