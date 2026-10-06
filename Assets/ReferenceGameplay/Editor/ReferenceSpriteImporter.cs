#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace RoyalThrone.ReferenceGameplay.Editor
{
    /// <summary>Reimports retain the packaged Sprite's pixel size, pivot, border and alpha.</summary>
    public sealed class ReferenceSpriteImporter : AssetPostprocessor
    {
        const string Root = "Assets/ReferenceGameplay/Art/Resources/ReferenceGameplay/Sprites/";

        [Serializable] sealed class NativeHints
        {
            public float pixelsPerUnit, pivotX, pivotY, borderX, borderY, borderZ, borderW;
        }

        void OnPreprocessTexture()
        {
            if (!assetPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) return;
            var importer = (TextureImporter)assetImporter;
            if (assetPath.StartsWith("Assets/ReferenceGameplay/Art/Atlases/", StringComparison.Ordinal) ||
                assetPath.StartsWith("Assets/ReferenceGameplay/VFX/Textures/", StringComparison.Ordinal))
            {
                // These are native atlas/material dependencies, never automatically sliced Sprites.
                // A project-wide 2D default must not dilate PMA RGB or resize the recovered atlas.
                importer.textureType = TextureImporterType.Default;
                importer.spriteImportMode = SpriteImportMode.None;
                importer.sRGBTexture = true;
                importer.alphaSource = TextureImporterAlphaSource.FromInput;
                importer.alphaIsTransparency = false;
                importer.mipmapEnabled = false;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                bool mailbox = assetPath.EndsWith("/mailbox.png", StringComparison.Ordinal);
                importer.maxTextureSize = mailbox ? 512 : 4096;
                // Desktop keeps the exact RGBA; phones get ASTC (6x6 for the particle atlases), which keeps the alpha and
                // costs a quarter of RGBA32 or less in the APK and in memory.
                foreach (var platform in new[] { "Standalone", "Android", "iPhone" })
                    SetPlatform(importer, platform, mailbox ? TextureImporterFormat.ASTC_4x4 : TextureImporterFormat.ASTC_6x6);
                return;
            }
            if (!assetPath.StartsWith(Root, StringComparison.Ordinal)) return;
            if (string.IsNullOrEmpty(importer.userData))
                throw new InvalidOperationException("Recovered Sprite metadata is missing: " + assetPath);
            var native = JsonUtility.FromJson<NativeHints>(importer.userData);
            if (native == null || native.pixelsPerUnit <= 0)
                throw new InvalidOperationException("Invalid recovered Sprite metadata: " + assetPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = native.pixelsPerUnit;
            importer.spriteBorder = new Vector4(native.borderX, native.borderY, native.borderZ, native.borderW);
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = new Vector2(native.pivotX, native.pivotY);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteExtrude = 0;
            settings.spriteGenerateFallbackPhysicsShape = false;
            importer.SetTextureSettings(settings);
            importer.sRGBTexture = true;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            // No hidden RGB dilation/unpremultiplication: preserve the packaged RGBA.
            importer.alphaIsTransparency = false;
            importer.mipmapEnabled = false;
            importer.isReadable = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            foreach (var platform in new[] { "Standalone", "Android", "iPhone" })
                SetPlatform(importer, platform, TextureImporterFormat.ASTC_4x4);
        }

        static void SetPlatform(TextureImporter importer, string platform, TextureImporterFormat mobile)
        {
            var p = importer.GetPlatformTextureSettings(platform);
            p.overridden = true;
            p.maxTextureSize = importer.maxTextureSize;
            bool phone = platform != "Standalone";
            p.format = phone ? mobile : TextureImporterFormat.RGBA32;
            p.textureCompression = phone ? TextureImporterCompression.Compressed : TextureImporterCompression.Uncompressed;
            importer.SetPlatformTextureSettings(p);
        }
    }
}
#endif
