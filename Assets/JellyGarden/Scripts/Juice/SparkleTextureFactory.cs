using UnityEngine;

namespace JellyGarden.Scripts.Juice
{
    /// <summary>
    /// Builds the particle shapes procedurally at load time.
    ///
    /// Everything here is generated from maths - no imported artwork, no sampled
    /// textures from any other game. Generating rather than importing also keeps
    /// the repository free of new binary art assets and lets the shapes be
    /// re-tuned by editing numbers.
    ///
    /// RGB is left white; the shape lives entirely in the alpha channel so the
    /// additive shader can tint each particle with the fruit colour.
    /// </summary>
    public static class SparkleTextureFactory
    {
        private static Texture2D _fourPointSparkle;
        private static Texture2D _sixPointStar;
        private static Texture2D _diamond;
        private static Texture2D _softGlow;
        private static Texture2D _ring;

        /// <summary>Four-point sparkle: bright core with two tapered spikes. The workhorse shape.</summary>
        public static Texture2D FourPointSparkle
        {
            get
            {
                if (_fourPointSparkle == null)
                    _fourPointSparkle = Build(64, "JuiceSparkle4", FourPointSparkleAlpha);
                return _fourPointSparkle;
            }
        }

        /// <summary>Six-point star, used sparsely to break up the repetition of the 4-point shape.</summary>
        public static Texture2D SixPointStar
        {
            get
            {
                if (_sixPointStar == null)
                    _sixPointStar = Build(64, "JuiceStar6", SixPointStarAlpha);
                return _sixPointStar;
            }
        }

        /// <summary>Small elongated diamond/shard for the micro sparkle layer.</summary>
        public static Texture2D Diamond
        {
            get
            {
                if (_diamond == null)
                    _diamond = Build(48, "JuiceDiamond", DiamondAlpha);
                return _diamond;
            }
        }

        /// <summary>Soft radial falloff for the impact flash and coloured fragments.</summary>
        public static Texture2D SoftGlow
        {
            get
            {
                if (_softGlow == null)
                    _softGlow = Build(64, "JuiceGlow", SoftGlowAlpha);
                return _softGlow;
            }
        }

        /// <summary>Thin expanding ring for the shared match-centre emphasis.</summary>
        public static Texture2D Ring
        {
            get
            {
                if (_ring == null)
                    _ring = Build(128, "JuiceRing", RingAlpha);
                return _ring;
            }
        }

        // ------------------------------------------------------------------

        private delegate float AlphaFunc(float u, float v);

        private static Texture2D Build(int size, string name, AlphaFunc f)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = name,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                anisoLevel = 0,
                // Generated at runtime and shared for the session - never written to disk.
                hideFlags = HideFlags.HideAndDontSave
            };

            var pixels = new Color32[size * size];
            float inv = 2f / (size - 1);

            for (int y = 0; y < size; y++)
            {
                float v = y * inv - 1f;
                int rowStart = y * size;
                for (int x = 0; x < size; x++)
                {
                    float u = x * inv - 1f;
                    float a = Mathf.Clamp01(f(u, v));
                    // Slight gamma lift keeps the soft edges from looking muddy when added.
                    a = Mathf.Pow(a, 0.85f);
                    pixels[rowStart + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            return tex;
        }

        private static float FourPointSparkleAlpha(float u, float v)
        {
            float r2 = u * u + v * v;
            if (r2 > 1f) return 0f;

            // Bright, tight core.
            float core = Mathf.Exp(-r2 * 14f);

            // Two spikes that taper to nothing at the texture edge.
            float taperU = Mathf.Max(0f, 1f - Mathf.Abs(u));
            float taperV = Mathf.Max(0f, 1f - Mathf.Abs(v));
            float hSpike = Mathf.Exp(-(v * v) * 320f) * taperU * taperU * taperU;
            float vSpike = Mathf.Exp(-(u * u) * 320f) * taperV * taperV * taperV;

            // A faint halo stops the spikes reading as hard lines.
            float halo = Mathf.Exp(-r2 * 5f) * 0.16f;

            return core + (hSpike + vSpike) * 0.95f + halo;
        }

        private static float SixPointStarAlpha(float u, float v)
        {
            float r = Mathf.Sqrt(u * u + v * v);
            if (r > 1f) return 0f;

            float theta = Mathf.Atan2(v, u);
            // 6 lobes; squared to sharpen the points.
            float lobe = 0.5f + 0.5f * Mathf.Cos(6f * theta);
            lobe *= lobe;

            float reach = Mathf.Lerp(0.26f, 0.98f, lobe);
            float edge = Mathf.SmoothStep(reach, reach * 0.45f, r);

            float core = Mathf.Exp(-(r * r) * 16f) * 0.9f;
            return Mathf.Max(edge * 0.95f, core);
        }

        private static float DiamondAlpha(float u, float v)
        {
            // Elongated on Y so the shard reads as a flake rather than a square.
            float d = Mathf.Abs(u) * 1.55f + Mathf.Abs(v) * 0.85f;
            if (d > 1f) return 0f;

            float body = Mathf.SmoothStep(1f, 0.15f, d);
            float core = Mathf.Exp(-(u * u * 2.4f + v * v * 0.8f) * 9f) * 0.7f;
            return Mathf.Max(body, core);
        }

        private static float SoftGlowAlpha(float u, float v)
        {
            float r2 = u * u + v * v;
            if (r2 > 1f) return 0f;
            float a = Mathf.Exp(-r2 * 6.5f);
            // Fade hard to zero at the rim so no square edge shows when scaled up.
            a *= Mathf.SmoothStep(1f, 0.72f, Mathf.Sqrt(r2));
            return a;
        }

        private static float RingAlpha(float u, float v)
        {
            float r = Mathf.Sqrt(u * u + v * v);
            if (r > 1f) return 0f;
            const float radius = 0.72f;
            float d = r - radius;
            // 260 gave a ~12px hairline that vanished on screen. 90 widens the stroke
            // to roughly 18% of the half-quad so the shared match-centre accent reads.
            float a = Mathf.Exp(-(d * d) * 90f);
            a *= Mathf.SmoothStep(1f, 0.85f, r);
            return a;
        }
    }
}
