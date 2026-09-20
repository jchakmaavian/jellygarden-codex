using System;
using System.IO;
using JellyGarden.Scripts.Juice;
using UnityEditor;
using UnityEngine;

namespace RoyalThrone.Editor
{
    /// <summary>Import settings are restricted to the new gameplay pack.</summary>
    public sealed class RoyalGameplayArtImporter : AssetPostprocessor
    {
        const string Root = "Assets/JellyGarden/Resources/RoyalGameplayV4/";

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Root, StringComparison.Ordinal) ||
                !assetPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) return;
            Configure((TextureImporter)assetImporter);
        }

        static void Configure(TextureImporter importer)
        {
            // The runtime catalog supplies exact sprite rectangles and pivots; no
            // Sprite Editor package, CPU texture copy or runtime pixel scan is needed.
            importer.textureType = TextureImporterType.Default;
            // A new minimal .meta may start with the cubemap shape. Default texture
            // type alone does not change it, making typed Texture2D loads return null.
            importer.textureShape = TextureImporterShape.Texture2D;
            importer.sRGBTexture = true;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.isReadable = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 512;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
        }

        [MenuItem("Tools/Royal Throne/Gameplay/Import and verify gameplay art v4")]
        public static void ImportAndVerify()
        {
            RoyalHomeScreenBuilder.RequireCopy();
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before importing gameplay art.");
            AssetDatabase.Refresh();
            int checkedCount = 0;
            foreach (string key in RoyalGameplayArt.Keys)
            {
                string path = "Assets/JellyGarden/Resources/" + RoyalGameplayArt.ResourcePath(key) + ".png";
                if (!File.Exists(path)) throw new FileNotFoundException("Missing gameplay source PNG", path);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (!importer) throw new InvalidOperationException("No texture importer for " + path);
                Configure(importer);
                importer.SaveAndReimport();
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                var sprite = RoyalGameplayArt.Art(key);
                if (!texture || !sprite || sprite.texture != texture)
                    throw new InvalidOperationException("Gameplay sprite could not be loaded: " + key +
                        "; path=" + path + "; main asset type=" + AssetDatabase.GetMainAssetTypeAtPath(path) +
                        "; importer shape=" + importer.textureShape + "; texture=" + Describe(texture) +
                        "; sprite=" + Describe(sprite) + "; sprite texture=" + Describe(sprite ? sprite.texture : null));
                if (sprite.rect.xMin < 0 || sprite.rect.yMin < 0 ||
                    sprite.rect.xMax > texture.width || sprite.rect.yMax > texture.height)
                    throw new InvalidOperationException("Sprite bounds exceed original canvas: " + key);
                var originalCenter = sprite.rect.position + sprite.pivot;
                if (Vector2.Distance(originalCenter, new Vector2(texture.width * .5f, texture.height * .5f)) > .01f)
                    throw new InvalidOperationException("Sprite source center changed: " + key);
                if (importer.textureShape != TextureImporterShape.Texture2D || texture.isReadable ||
                    importer.mipmapEnabled || importer.npotScale != TextureImporterNPOTScale.None)
                    throw new InvalidOperationException("Unexpected memory or resizing settings: " + key);
                checkedCount++;
            }
            if (checkedCount != RoyalGameplayArt.AssetCount)
                throw new InvalidOperationException("Gameplay catalog asset count changed.");
            Debug.Log("[Royal Throne Gameplay Art] " + checkedCount + " imported PNGs, sprite bounds, source-centered pivots and GPU-only textures verified.");
        }

        static string Describe(UnityEngine.Object value)
        {
            return value ? value.GetType().Name + " '" + value.name + "' (" + value.GetEntityId() + ")" : "null";
        }
    }
}
