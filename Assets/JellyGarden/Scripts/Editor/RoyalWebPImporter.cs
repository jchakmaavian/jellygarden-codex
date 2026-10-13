using System;
using System.IO;
using System.Diagnostics;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace RoyalThrone.Editor
{
    // WebP remains the source format; PNG is an editor-only cache, never a player asset.
    [ScriptedImporter(2, "webp")]
    public sealed class RoyalWebPImporter : ScriptedImporter
    {
        public float pixelsPerUnit = 160;
        [Tooltip("Copy visible colour under fully transparent pixels, as Alpha Is Transparency does, so bilinear edges and block compression do not pull in black. Alpha and visible pixels are unchanged.")]
        public bool bleedTransparentEdges;
        [Tooltip("Android/iOS: ASTC 6x6 instead of ASTC 4x4. Used by frame sequences.")]
        public bool astcOnMobile;
        public override void OnImportAsset(AssetImportContext ctx)
        {
            string executable = FindFFmpeg();
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
            if (bleedTransparentEdges) BleedTransparentEdges(texture);
            // Phones always get ASTC: phone GPUs have no BC3, so a DXT5 texture was expanded to RGBA32 at load (and one
            // whose size is not a multiple of 4 shipped as RGBA32). Opted-in sequences use 6x6, everything else 4x4.
            bool mobile = ctx.selectedBuildTarget == BuildTarget.Android || ctx.selectedBuildTarget == BuildTarget.iOS;
            var format = !mobile ? TextureFormat.DXT5 : astcOnMobile ? TextureFormat.ASTC_6x6 : TextureFormat.ASTC_4x4;
            EditorUtility.CompressTexture(texture, format, TextureCompressionQuality.Best);
            texture.Apply(false, true);
            var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), pixelsPerUnit, 0, SpriteMeshType.FullRect);
            sprite.name = texture.name;
            ctx.AddObjectToAsset("texture", texture); ctx.AddObjectToAsset("sprite", sprite); ctx.SetMainObject(sprite);
        }

        // Explicit setting first, then known installs, then PATH (winget and package managers put FFmpeg there).
        static string FindFFmpeg()
        {
            string executable = Environment.GetEnvironmentVariable("ROYAL_THRONE_FFMPEG");
            if (string.IsNullOrEmpty(executable)) executable = EditorPrefs.GetString("RoyalThrone.FFmpeg", "");
            if (File.Exists(executable)) return executable;
            foreach (string candidate in new[] { @"C:\Program Files\Softdeluxe\Free Download Manager\ffmpeg.exe", @"C:\ffmpeg\bin\ffmpeg.exe", "/usr/local/bin/ffmpeg", "/opt/homebrew/bin/ffmpeg", "/usr/bin/ffmpeg" })
                if (File.Exists(candidate)) return candidate;
            string name = Application.platform == RuntimePlatform.WindowsEditor ? "ffmpeg.exe" : "ffmpeg";
            // The editor's own PATH can predate an install, so the user's stored PATH is searched as well.
            string path = Environment.GetEnvironmentVariable("PATH") + Path.PathSeparator
                + Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User) + Path.PathSeparator
                + Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine);
            foreach (string dir in path.Split(Path.PathSeparator))
            {
                if (string.IsNullOrWhiteSpace(dir)) continue;
                try { string candidate = Path.Combine(dir.Trim().Trim('"'), name); if (File.Exists(candidate)) return candidate; }
                catch (ArgumentException) { }
            }
            // winget installs Gyan.FFmpeg under its package folder.
            string winget = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Microsoft\WinGet\Packages");
            if (Directory.Exists(winget))
                foreach (string package in Directory.GetDirectories(winget, "*FFmpeg*"))
                    foreach (string candidate in Directory.GetFiles(package, "ffmpeg.exe", SearchOption.AllDirectories))
                        return candidate;
            return null;
        }

        // Grows visible colour outward one ring per pass (alpha-weighted), leaving alpha at zero.
        static void BleedTransparentEdges(Texture2D texture, int passes = 8)
        {
            int w = texture.width, h = texture.height;
            var px = texture.GetPixels32();
            var weight = new int[px.Length];
            for (int i = 0; i < px.Length; i++) weight[i] = px[i].a;
            var ring = new System.Collections.Generic.List<int>();
            for (int pass = 0; pass < passes; pass++)
            {
                ring.Clear();
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        int i = y * w + x;
                        if (weight[i] > 0) continue;
                        int r = 0, g = 0, b = 0, n = 0;
                        for (int yy = Math.Max(0, y - 1); yy <= Math.Min(h - 1, y + 1); yy++)
                            for (int xx = Math.Max(0, x - 1); xx <= Math.Min(w - 1, x + 1); xx++)
                            {
                                int k = yy * w + xx, wk = weight[k];
                                if (wk <= 0) continue;
                                r += px[k].r * wk; g += px[k].g * wk; b += px[k].b * wk; n += wk;
                            }
                        if (n > 0) { px[i] = new Color32((byte)(r / n), (byte)(g / n), (byte)(b / n), 0); ring.Add(i); }
                    }
                if (ring.Count == 0) break;
                // Filled texels seed the next ring with a small weight so visible colour always dominates.
                foreach (int i in ring) weight[i] = 1;
            }
            texture.SetPixels32(px);
            texture.Apply(false);
        }
    }
}
