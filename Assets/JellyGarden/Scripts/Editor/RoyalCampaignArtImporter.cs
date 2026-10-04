using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using JellyGarden.Scripts.Juice;
using UnityEditor;
using UnityEngine;

namespace RoyalThrone.Editor
{
    /// <summary>Original campaign sprites share one import contract and the existing art cache.</summary>
    public sealed class RoyalCampaignArtImporter : AssetPostprocessor
    {
        public const string Root = "Assets/JellyGarden/Resources/RoyalCampaignArt/";
        public const string ManifestPath = "Design/Inspection-Levels01-50/original-art-manifest.json";

        void OnPreprocessTexture()
        {
            if (assetPath.StartsWith(Root, StringComparison.Ordinal) && assetPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                Configure((TextureImporter)assetImporter);
        }

        static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            foreach (var path in imported)
                if (path.StartsWith(Root, StringComparison.Ordinal)) { RoyalPieceSkin.ResetArtCache(); return; }
        }

        static void Configure(TextureImporter importer)
        {
            int maxSize = Path.GetFileNameWithoutExtension(importer.assetPath) == "palace" ? 1024 : 512;
            importer.textureType = TextureImporterType.Sprite;
            importer.textureShape = TextureImporterShape.Texture2D;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100;
            importer.spriteBorder = Vector4.zero;
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            settings.spritePivot = new Vector2(.5f, .5f);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.sRGBTexture = true;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.isReadable = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = maxSize;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            var android = importer.GetPlatformTextureSettings("Android");
            android.overridden = true; android.maxTextureSize = maxSize;
            android.format = TextureImporterFormat.RGBA32;
            android.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SetPlatformTextureSettings(android);
        }

        [MenuItem("Tools/Royal Throne/Art/Import and verify original campaign art")]
        public static void ImportAndVerify()
        {
            RoyalHomeScreenBuilder.RequireCopy();
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode before importing campaign art.");
            AssetDatabase.Refresh();
            foreach (var key in RoyalPieceSkin.CampaignKeys)
            {
                string path = Root + key + ".png";
                if (!File.Exists(path)) throw new FileNotFoundException("Missing original campaign sprite", path);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (!importer) throw new InvalidOperationException("No sprite importer for " + path);
                Configure(importer); importer.SaveAndReimport();
            }
            Verify();
        }

        public static void Verify()
        {
            RoyalHomeScreenBuilder.RequireCopy();
            RoyalPieceSkin.ResetArtCache();
            var records = new List<ArtRecord>();
            foreach (var key in RoyalPieceSkin.CampaignKeys)
            {
                string path = Root + key + ".png";
                if (!File.Exists(path)) throw new FileNotFoundException("Missing original campaign sprite", path);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (!importer || !sprite || RoyalPieceSkin.CampaignArt(key) != sprite)
                    throw new InvalidOperationException("Campaign sprite cannot be loaded through the shared catalog: " + key);
                int maxSize = key == "palace" ? 1024 : 512;
                var android = importer.GetPlatformTextureSettings("Android");
                if (importer.spriteImportMode != SpriteImportMode.Single || importer.textureCompression != TextureImporterCompression.Uncompressed ||
                    importer.maxTextureSize != maxSize || importer.isReadable || importer.mipmapEnabled || !importer.alphaIsTransparency ||
                    !android.overridden || android.format != TextureImporterFormat.RGBA32 || android.maxTextureSize != maxSize)
                    throw new InvalidOperationException("Unexpected campaign texture memory, alpha or compression settings: " + key);
                if (Vector2.Distance(sprite.pivot, sprite.rect.size * .5f) > .01f ||
                    sprite.rect.width != sprite.texture.width || sprite.rect.height != sprite.texture.height)
                    throw new InvalidOperationException("Campaign sprite must retain its full canvas and centered pivot: " + key);
                if (RoyalGameplayArt.Art(key) != sprite || RoyalPieceSkin.Art(key) != sprite)
                    throw new InvalidOperationException("Current game art did not prefer the original campaign sprite: " + key);
                using (var sha = SHA256.Create())
                    records.Add(new ArtRecord { key = key, path = path, guid = AssetDatabase.AssetPathToGUID(path),
                        sha256 = BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant(),
                        width = sprite.texture.width, height = sprite.texture.height });
            }
            if (RoyalGameplayArt.Art("vase") != RoyalPieceSkin.CampaignArt("gem"))
                throw new InvalidOperationException("Campaign piece alias failed.");
            Directory.CreateDirectory(Path.GetDirectoryName(ManifestPath));
            File.WriteAllText(ManifestPath, JsonUtility.ToJson(new ArtManifest { assets = records.ToArray() }, true));
            Debug.Log("[Royal Throne Art] Verified " + records.Count + " original PNGs, catalog preference, centered sprites, alpha and GPU-only import settings. Manifest: " + ManifestPath);
        }

        [Serializable] sealed class ArtManifest
        { public string game = "Royal Throne"; public string king = "King Vanguard"; public string source = "Original generated Royal Throne artwork; see ORIGINAL-ART-INTEGRATION.md for generation provenance."; public ArtRecord[] assets; }
        [Serializable] sealed class ArtRecord
        { public string key, path, guid, sha256; public int width, height; }
    }
}
