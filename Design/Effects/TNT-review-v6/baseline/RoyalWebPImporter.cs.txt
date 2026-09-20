using System;
using System.IO;
using System.Diagnostics;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace RoyalThrone.Editor
{
    // WebP remains the source format; PNG is an editor-only cache, never a player asset.
    [ScriptedImporter(1, "webp")]
    public sealed class RoyalWebPImporter : ScriptedImporter
    {
        public float pixelsPerUnit = 160;
        public override void OnImportAsset(AssetImportContext ctx)
        {
            string executable = Environment.GetEnvironmentVariable("ROYAL_THRONE_FFMPEG");
            if (string.IsNullOrEmpty(executable)) executable = EditorPrefs.GetString("RoyalThrone.FFmpeg", "");
            if (!File.Exists(executable))
                foreach (string candidate in new[] { @"C:\Program Files\Softdeluxe\Free Download Manager\ffmpeg.exe", "/usr/local/bin/ffmpeg", "/opt/homebrew/bin/ffmpeg", "/usr/bin/ffmpeg" })
                    if (File.Exists(candidate)) { executable = candidate; break; }
            if (!File.Exists(executable)) throw new FileNotFoundException("WebP import requires FFmpeg in the editor. Set ROYAL_THRONE_FFMPEG to its executable path. No decoder ships in the game.");
            Directory.CreateDirectory("Library/RoyalWebPCache");
            string cache = Path.GetFullPath("Library/RoyalWebPCache/" + AssetDatabase.AssetPathToGUID(ctx.assetPath) + ".png");
            var info = new ProcessStartInfo(executable, "-hide_banner -loglevel error -y -i \"" + Path.GetFullPath(ctx.assetPath) + "\" -frames:v 1 \"" + cache + "\"")
                { UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true };
            using (var process = Process.Start(info))
            {
                string error = process.StandardError.ReadToEnd();
                if (!process.WaitForExit(15000)) { process.Kill(); throw new TimeoutException("WebP decoder timed out."); }
                if (process.ExitCode != 0) throw new InvalidDataException(error);
            }
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(cache))) throw new InvalidDataException("WebP cache decode failed.");
            texture.name = Path.GetFileNameWithoutExtension(ctx.assetPath);
            texture.filterMode = FilterMode.Bilinear; texture.wrapMode = TextureWrapMode.Clamp;
            // Desktop BC3; platform exports are deferred. Import settings can be extended for ASTC then.
            EditorUtility.CompressTexture(texture, TextureFormat.DXT5, TextureCompressionQuality.Best);
            texture.Apply(false, true);
            var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), pixelsPerUnit, 0, SpriteMeshType.FullRect);
            sprite.name = texture.name;
            ctx.AddObjectToAsset("texture", texture); ctx.AddObjectToAsset("sprite", sprite); ctx.SetMainObject(sprite);
        }
    }
}
