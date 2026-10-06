using UnityEngine;

namespace RoyalThrone.Match3Vfx
{
    /// <summary>
    /// Loads the kit's base materials from Resources. A shader that no asset references is
    /// stripped from player builds, so materials under Resources keep the shaders alive;
    /// Shader.Find is only an editor fallback.
    /// </summary>
    internal static class Match3VfxMaterials
    {
        public const string AdditivePath = "Match3Vfx/Additive";
        public const string FragmentPath = "Match3Vfx/Fragment";
        public const string ParticleAlphaPath = "Match3Vfx/ParticleAlpha";

        public const string AdditiveShader = "RoyalThrone/Match3Vfx/Additive";
        public const string FragmentShader = "RoyalThrone/Match3Vfx/Fragment";
        public const string ParticleAlphaShader = "RoyalThrone/Match3Vfx/ParticleAlpha";

        /// <summary>Additive material for particle systems (vertex colour only).</summary>
        public static Material CreateAdditiveParticle(Texture texture, float boost)
        {
            var m = Instance(AdditivePath, AdditiveShader);
            m.DisableKeyword("SPRITE_RENDERER_COLOR");
            m.SetFloat("_UseRendererColor", 0f);
            m.SetFloat("_Boost", boost);
            if (texture) m.mainTexture = texture;
            return m;
        }

        /// <summary>Additive material for SpriteRenderers (multiplies SpriteRenderer.color).</summary>
        public static Material CreateAdditiveSprite(float boost)
        {
            var m = Instance(AdditivePath, AdditiveShader);
            m.EnableKeyword("SPRITE_RENDERER_COLOR");
            m.SetFloat("_UseRendererColor", 1f);
            m.SetFloat("_Boost", boost);
            return m;
        }

        public static Material CreateFragment() => Instance(FragmentPath, FragmentShader);

        public static Material CreateParticleAlpha(Texture texture)
        {
            var m = Instance(ParticleAlphaPath, ParticleAlphaShader);
            if (texture) m.mainTexture = texture;
            return m;
        }

        static Material Instance(string resourcePath, string shaderName)
        {
            var baseMaterial = Resources.Load<Material>(resourcePath);
            Material m;
            if (baseMaterial && baseMaterial.shader && baseMaterial.shader.isSupported)
            {
                m = new Material(baseMaterial);
            }
            else
            {
                var shader = Shader.Find(shaderName);
                if (!shader)
                {
                    shader = Shader.Find("Sprites/Default");
                    Debug.LogWarning("[Match3Vfx] Resources/" + resourcePath + ".mat and shader " + shaderName +
                                     " are missing. Effects fall back to Sprites/Default. Reimport the package.");
                }
                m = new Material(shader);
            }
            m.hideFlags = HideFlags.HideAndDontSave;
            return m;
        }
    }
}
