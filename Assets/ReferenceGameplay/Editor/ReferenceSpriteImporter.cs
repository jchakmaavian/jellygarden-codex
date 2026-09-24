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
                importer.maxTextureSize = assetPath.EndsWith("/mailbox.png", StringComparison.Ordinal) ? 512 : 4096;
                foreach (var platform in new[] { "Standalone", "Android", "iPhone" })
                {
                    var p = importer.GetPlatformTextureSettings(platform);
                    p.overridden = true;
                    p.maxTextureSize = importer.maxTextureSize;
                    p.format = TextureImporterFormat.RGBA32;
                    p.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.SetPlatformTextureSettings(p);
                }
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
            {
                var p = importer.GetPlatformTextureSettings(platform);
                p.overridden = true;
                p.maxTextureSize = importer.maxTextureSize;
                p.format = TextureImporterFormat.RGBA32;
                p.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SetPlatformTextureSettings(p);
            }
        }
    }
}
#endif
