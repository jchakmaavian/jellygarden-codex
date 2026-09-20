# Automated frame check for ColorBall_TargetImpact (usable for any single-clip frame folder).
# Usage: powershell -NoProfile -ExecutionPolicy Bypass -File check_colorball_targetimpact.ps1 -Frames <dir> -Prefix ColorBall_TargetImpact [-Loop] [-LastTransparent] [-Cx 192 -Cy 192]
# Reports: max alpha within 2 px of the border (must be 0), pixels whose premultiplied RGB exceeds alpha, zero-alpha texels carrying RGB,
# faint (alpha byte 1) texels, stray texels (visible but no alpha>=24 texel within 40 px), last-frame transparency, radius/centroid about the pivot, and the loop seam metric
# (mean abs diff last->first vs the average consecutive step).
param([string]$Frames, [string]$Prefix = 'ColorBall_TargetImpact', [switch]$Loop, [switch]$LastTransparent, [int]$Border = 2, [double]$Cx = 192, [double]$Cy = 192)
$ErrorActionPreference = 'Stop'
$src = @'
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

public static class CbtiFrameCheck
{
    const int StrongA = 24, StrayDist = 40;

    static byte[] LoadBgra(string path, out int w, out int h)
    {
        using (var bmp = new Bitmap(path))
        {
            w = bmp.Width; h = bmp.Height;
            var data = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            var px = new byte[w * h * 4];
            for (int y = 0; y < h; y++) Marshal.Copy(IntPtr.Add(data.Scan0, y * data.Stride), px, y * w * 4, w * 4);
            bmp.UnlockBits(data);
            return px;
        }
    }

    // Premultiplied 0..255 value of channel c at alpha a.
    static double Pm(byte c, byte a) { return c * a / 255.0; }

    static double Diff(byte[] a, byte[] b)
    {
        double d = 0; int n = a.Length / 4;
        for (int i = 0; i < n; i++)
        {
            byte aa = a[i * 4 + 3], ba = b[i * 4 + 3];
            for (int c = 0; c < 3; c++) d += Math.Abs(Pm(a[i * 4 + c], aa) - Pm(b[i * 4 + c], ba));
            d += Math.Abs(aa - ba);
        }
        return d / (n * 4.0);
    }

    public static string Run(string dir, string prefix, bool loop, bool lastTransparent, int border, double cx, double cy)
    {
        var files = new List<string>();
        for (int i = 1; ; i++)
        {
            string p = Path.Combine(dir, string.Format("{0}_{1:000}.png", prefix, i));
            if (!File.Exists(p)) break;
            files.Add(p);
        }
        var sb = new StringBuilder();
        if (files.Count == 0) { sb.AppendLine("FAIL: no frames named " + prefix + "_001.png in " + dir); return sb.ToString(); }
        int extra = Directory.GetFiles(dir, "*.png").Length - files.Count;

        int W = 0, H = 0; bool sizeOk = true;
        int borderMax = 0, borderFrame = -1;
        long rgbOverAlpha = 0, dirtyZero = 0, faint = 0, isolated = 0;
        double maxRadius = 0; int maxRadiusFrame = -1;
        double maxCentroidOff = 0; int centroidFrame = -1;
        byte[] prev = null, first = null, last = null;
        double stepSum = 0, stepMax = 0; int steps = 0;
        var perFrame = new StringBuilder();
        for (int f = 0; f < files.Count; f++)
        {
            int w, h; var px = LoadBgra(files[f], out w, out h);
            if (f == 0) { W = w; H = h; } else if (w != W || h != H) sizeOk = false;
            int fb = 0; long fOver = 0, fDirty = 0, fFaint = 0, fIso = 0, fVis = 0; double fr = 0, sa = 0, sx = 0, sy = 0;
            // Prefix sum of "substantial" texels (alpha >= StrongA) for the stray query.
            var ps = new int[(w + 1) * (h + 1)];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    ps[(y + 1) * (w + 1) + x + 1] = (px[(y * w + x) * 4 + 3] >= StrongA ? 1 : 0) + ps[y * (w + 1) + x + 1] + ps[(y + 1) * (w + 1) + x] - ps[y * (w + 1) + x];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int i = (y * w + x) * 4; byte a = px[i + 3];
                    bool edge = x < border || y < border || x >= w - border || y >= h - border;
                    if (edge && a > fb) fb = a;
                    if (a == 0) { if (px[i] != 0 || px[i + 1] != 0 || px[i + 2] != 0) fDirty++; continue; }
                    fVis++;
                    // Premultiplied colour must not exceed alpha (after 8-bit rounding).
                    if (Math.Round(Pm(px[i], a)) > a || Math.Round(Pm(px[i + 1], a)) > a || Math.Round(Pm(px[i + 2], a)) > a) fOver++;
                    if (a < 2) fFaint++;
                    // Stray: a visible texel with no substantial texel (alpha >= StrongA) within StrayDist px, i.e. not a glow tail of the effect.
                    int xa = Math.Max(0, x - StrayDist), xb = Math.Min(w - 1, x + StrayDist), ya = Math.Max(0, y - StrayDist), yb = Math.Min(h - 1, y + StrayDist);
                    int cnt = ps[(yb + 1) * (w + 1) + xb + 1] - ps[ya * (w + 1) + xb + 1] - ps[(yb + 1) * (w + 1) + xa] + ps[ya * (w + 1) + xa];
                    if (cnt == 0) fIso++;
                    double dx = x + 0.5 - cx, dy = y + 0.5 - cy, r = Math.Sqrt(dx * dx + dy * dy);
                    if (r > fr) fr = r;
                    sa += a; sx += a * (x + 0.5); sy += a * (y + 0.5);
                }
            if (fb > borderMax) { borderMax = fb; borderFrame = f + 1; }
            rgbOverAlpha += fOver; dirtyZero += fDirty; faint += fFaint; isolated += fIso;
            if (fr > maxRadius) { maxRadius = fr; maxRadiusFrame = f + 1; }
            double coff = 0;
            if (sa > 0) { coff = Math.Sqrt(Math.Pow(sx / sa - cx, 2) + Math.Pow(sy / sa - cy, 2)); if (coff > maxCentroidOff) { maxCentroidOff = coff; centroidFrame = f + 1; } }
            double step = prev != null ? Diff(prev, px) : 0;
            if (prev != null) { stepSum += step; steps++; if (step > stepMax) stepMax = step; }
            perFrame.AppendLine(string.Format("  {0:000}  visible {1,6}  border {2,3}  radius {3,6:F1}  centroidOff {4,5:F1}  step {5,6:F3}  faint {6}  stray {7}", f + 1, fVis, fb, fr, coff, step, fFaint, fIso));
            if (f == 0) first = px;
            last = px; prev = px;
        }
        int lastMax = 0; for (int i = 3; i < last.Length; i += 4) if (last[i] > lastMax) lastMax = last[i];
        double meanStep = steps > 0 ? stepSum / steps : 0;
        double seam = Diff(last, first);

        sb.AppendLine("Clip " + prefix + "  (" + dir + ")");
        sb.AppendLine(string.Format("  frames {0}  size {1}x{2}{3}{4}", files.Count, W, H, sizeOk ? "" : "  SIZE MISMATCH", extra > 0 ? "  (+" + extra + " other png files in folder)" : ""));
        sb.AppendLine(string.Format("  max alpha within {0} px of border : {1}{2}", border, borderMax, borderMax > 0 ? "  (frame " + borderFrame + ")" : ""));
        sb.AppendLine(string.Format("  pixels with premult RGB > alpha   : {0}", rgbOverAlpha));
        sb.AppendLine(string.Format("  alpha-0 texels carrying RGB       : {0}", dirtyZero));
        sb.AppendLine(string.Format("  faint texels (alpha byte 1)       : {0}", faint));
        sb.AppendLine(string.Format("  stray texels (no a>=24 within 40px): {0}", isolated));
        sb.AppendLine(string.Format("  last frame max alpha              : {0}{1}", lastMax, lastTransparent ? " (must be 0)" : ""));
        sb.AppendLine(string.Format("  max visible radius from pivot     : {0:F1} px (frame {1})", maxRadius, maxRadiusFrame));
        sb.AppendLine(string.Format("  max alpha-centroid offset         : {0:F1} px (frame {1})", maxCentroidOff, centroidFrame));
        sb.AppendLine(string.Format("  mean consecutive step             : {0:F3}  (max {1:F3}, 0..255 premult RGBA)", meanStep, stepMax));
        sb.AppendLine(string.Format("  last->first step                  : {0:F3}  ratio {1:F2}{2}", seam, meanStep > 0 ? seam / meanStep : 0, loop ? "" : "  (not a loop: informational)"));
        bool pass = sizeOk && borderMax == 0 && rgbOverAlpha == 0 && dirtyZero == 0 && faint == 0 && isolated == 0 && (!lastTransparent || lastMax == 0) && (!loop || meanStep <= 0 || seam <= stepMax * 1.05);
        sb.AppendLine("  RESULT: " + (pass ? "PASS" : "FAIL"));
        sb.AppendLine("  per frame:");
        sb.Append(perFrame.ToString());
        return sb.ToString();
    }
}
'@
Add-Type -TypeDefinition $src -ReferencedAssemblies System.Drawing
[CbtiFrameCheck]::Run($Frames, $Prefix, [bool]$Loop, [bool]$LastTransparent, $Border, $Cx, $Cy)
