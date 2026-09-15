using UnityEngine;

namespace RoyalThrone.Match3Vfx
{
    /// <summary>
    /// Particle shapes generated from maths at load time. White RGB, shape in alpha, so every
    /// effect can be tinted. No imported art is required for the kit to work.
    /// </summary>
    public static class Match3Shapes
    {
        static Texture2D fourPointSparkle, sixPointStar, diamond, softGlow, ring, smokePuff, gem;
        static Sprite glowSprite, ringSprite, gemSprite, sparkleSprite;

        public static Texture2D FourPointSparkle => fourPointSparkle ? fourPointSparkle : fourPointSparkle = Build(64, "Match3Vfx Sparkle4", FourPointSparkleAlpha);
        public static Texture2D SixPointStar => sixPointStar ? sixPointStar : sixPointStar = Build(64, "Match3Vfx Star6", SixPointStarAlpha);
        public static Texture2D Diamond => diamond ? diamond : diamond = Build(48, "Match3Vfx Diamond", DiamondAlpha);
        public static Texture2D SoftGlow => softGlow ? softGlow : softGlow = Build(64, "Match3Vfx Glow", SoftGlowAlpha);
        public static Texture2D Ring => ring ? ring : ring = Build(128, "Match3Vfx Ring", RingAlpha);
        public static Texture2D SmokePuff => smokePuff ? smokePuff : smokePuff = Build(96, "Match3Vfx Smoke", SmokePuffAlpha);
        /// <summary>Rounded gem silhouette with a highlight, used by the demo board and as projectile fallback.</summary>
        public static Texture2D Gem => gem ? gem : gem = BuildGem(96);

        /// <summary>Sprites whose pixelsPerUnit equals the texture width, so scale 1 = one world unit.</summary>
        public static Sprite GlowSprite => glowSprite ? glowSprite : glowSprite = UnitSprite(SoftGlow);
        public static Sprite RingSprite => ringSprite ? ringSprite : ringSprite = UnitSprite(Ring);
        public static Sprite GemSprite => gemSprite ? gemSprite : gemSprite = UnitSprite(Gem);
        public static Sprite SparkleSprite => sparkleSprite ? sparkleSprite : sparkleSprite = UnitSprite(FourPointSparkle);

        public static Sprite UnitSprite(Texture2D texture)
        {
            var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f), texture.width, 0, SpriteMeshType.FullRect);
            sprite.name = texture.name;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        delegate float AlphaFunc(float u, float v);

        static Texture2D NewTexture(int size, string name)
        {
            return new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = name,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        static Texture2D Build(int size, string name, AlphaFunc f)
        {
            var tex = NewTexture(size, name);
            var pixels = new Color32[size * size];
            float inv = 2f / (size - 1);
            for (int y = 0; y < size; y++)
            {
                float v = y * inv - 1f;
                for (int x = 0; x < size; x++)
                {
                    float u = x * inv - 1f;
                    float a = Mathf.Pow(Mathf.Clamp01(f(u, v)), 0.85f);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply(false, false);
            return tex;
        }

        static Texture2D BuildGem(int size)
        {
            var tex = NewTexture(size, "Match3Vfx Gem");
            var pixels = new Color32[size * size];
            float inv = 2f / (size - 1);
            for (int y = 0; y < size; y++)
            {
                float v = y * inv - 1f;
                for (int x = 0; x < size; x++)
                {
                    float u = x * inv - 1f;
                    // Rounded square body with a soft rim.
                    float d = Mathf.Pow(Mathf.Pow(Mathf.Abs(u), 4f) + Mathf.Pow(Mathf.Abs(v), 4f), 0.25f);
                    float alpha = 1f - Edge(0.84f, 0.9f, d);
                    float shade = Mathf.Lerp(1f, 0.72f, Mathf.Clamp01((d - 0.35f) / 0.55f));
                    float highlight = Mathf.Exp(-((u + 0.3f) * (u + 0.3f) + (v - 0.35f) * (v - 0.35f)) * 14f);
                    byte c = (byte)(Mathf.Clamp01(shade + highlight * 0.6f) * 255f);
                    pixels[y * size + x] = new Color32(c, c, c, (byte)(alpha * 255f));
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply(false, false);
            return tex;
        }

        static float FourPointSparkleAlpha(float u, float v)
        {
            float r2 = u * u + v * v;
            if (r2 > 1f) return 0f;
            float core = Mathf.Exp(-r2 * 14f);
            float taperU = Mathf.Max(0f, 1f - Mathf.Abs(u));
            float taperV = Mathf.Max(0f, 1f - Mathf.Abs(v));
            float hSpike = Mathf.Exp(-(v * v) * 320f) * taperU * taperU * taperU;
            float vSpike = Mathf.Exp(-(u * u) * 320f) * taperV * taperV * taperV;
            float halo = Mathf.Exp(-r2 * 5f) * 0.16f;
            return core + (hSpike + vSpike) * 0.95f + halo;
        }

        static float SixPointStarAlpha(float u, float v)
        {
            float r = Mathf.Sqrt(u * u + v * v);
            if (r > 1f) return 0f;
            float lobe = 0.5f + 0.5f * Mathf.Cos(6f * Mathf.Atan2(v, u));
            lobe *= lobe;
            float reach = Mathf.Lerp(0.26f, 0.98f, lobe);
            float edge = Mathf.SmoothStep(reach, reach * 0.45f, r);
            float core = Mathf.Exp(-(r * r) * 16f) * 0.9f;
            return Mathf.Max(edge * 0.95f, core);
        }

        static float DiamondAlpha(float u, float v)
        {
            float d = Mathf.Abs(u) * 1.55f + Mathf.Abs(v) * 0.85f;
            if (d > 1f) return 0f;
            float body = Mathf.SmoothStep(1f, 0.15f, d);
            float core = Mathf.Exp(-(u * u * 2.4f + v * v * 0.8f) * 9f) * 0.7f;
            return Mathf.Max(body, core);
        }

        static float SoftGlowAlpha(float u, float v)
        {
            float r2 = u * u + v * v;
            if (r2 > 1f) return 0f;
            return Mathf.Exp(-r2 * 6.5f) * Mathf.SmoothStep(1f, 0.72f, Mathf.Sqrt(r2));
        }

        static float RingAlpha(float u, float v)
        {
            float r = Mathf.Sqrt(u * u + v * v);
            if (r > 1f) return 0f;
            float d = r - 0.72f;
            return Mathf.Exp(-(d * d) * 90f) * Mathf.SmoothStep(1f, 0.85f, r);
        }

        static float SmokePuffAlpha(float u, float v)
        {
            float r = Mathf.Sqrt(u * u + v * v);
            if (r > 1f) return 0f;
            // A lumpy edge from three low-frequency lobes reads as a sculpted puff, not a disc.
            float angle = Mathf.Atan2(v, u);
            float lumps = 0.78f + 0.08f * Mathf.Sin(angle * 3f + 0.6f) + 0.06f * Mathf.Sin(angle * 5f + 2.1f);
            float body = 1f - Edge(lumps * 0.55f, lumps, r);
            return body * 0.85f;
        }

        /// <summary>Hermite edge like shader smoothstep: 0 below edge0, 1 above edge1.</summary>
        static float Edge(float edge0, float edge1, float x)
        {
            float t = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
            return t * t * (3f - 2f * t);
        }
    }
}
