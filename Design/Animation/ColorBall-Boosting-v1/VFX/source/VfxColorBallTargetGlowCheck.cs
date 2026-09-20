// Automated checks for ColorBall_TargetGlow frame PNGs (straight-alpha PNGs as written by Img.Save). C# 5.
// Per clip: max alpha within 2 px of the border, texels whose premultiplied RGB exceeds alpha, transparent texels carrying RGB,
// faint dust (alpha byte 1 = below 2/255), isolated texels, alpha bounding box, centroid drift; loops: wrap step vs consecutive steps.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

public static class VfxColorBallTargetGlowCheck
{
    // Returns BGRA bytes (straight alpha).
    static byte[] Load(string path, out int w, out int h)
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

    // Premultiplied RGBA in 0..1.
    static float[] Premul(byte[] px)
    {
        int n = px.Length / 4; var o = new float[px.Length];
        for (int i = 0; i < n; i++)
        {
            float a = px[i * 4 + 3] / 255f;
            o[i * 4] = px[i * 4 + 2] / 255f * a; o[i * 4 + 1] = px[i * 4 + 1] / 255f * a; o[i * 4 + 2] = px[i * 4] / 255f * a; o[i * 4 + 3] = a;
        }
        return o;
    }

    static double MeanAbs(float[] a, float[] b)
    {
        double d = 0; for (int i = 0; i < a.Length; i++) d += Math.Abs(a[i] - b[i]);
        return d / a.Length;
    }

    static string[] Frames(string dir, string prefix)
    {
        var f = Directory.GetFiles(dir, prefix + "_*.png"); Array.Sort(f, StringComparer.Ordinal); return f;
    }

    public static string CheckClip(string framesDir, string prefix, bool loop)
    {
        var files = Frames(framesDir, prefix);
        var sb = new StringBuilder();
        sb.AppendLine(string.Format("clip {0}: {1} frames", framesDir, files.Length));
        if (files.Length == 0) return sb.ToString();
        int maxBorder = 0, maxBorderFrame = 0, premulBad = 0, transparentRgb = 0, faint = 0, isolated = 0;
        int bx0 = int.MaxValue, by0 = int.MaxValue, bx1 = -1, by1 = -1;
        double maxDrift = 0; int W = 0, H = 0;
        var prem = new List<float[]>();
        var alphaSum = new List<double>();
        var rimBand = new List<double>();
        int centreMax = 0;
        for (int fi = 0; fi < files.Length; fi++)
        {
            int w, h; var px = Load(files[fi], out w, out h); W = w; H = h;
            double s = 0, sx = 0, sy = 0, rb = 0; int rbn = 0;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int i = y * w + x; byte a = px[i * 4 + 3], r = px[i * 4 + 2], g = px[i * 4 + 1], b = px[i * 4];
                    // Asset-specific probes: fill alpha near the pivot, and mean alpha of the unsaturated rim shoulder (1..5 px outside the 176/44 outline).
                    double cdx = x + 0.5 - w / 2.0, cdy = y + 0.5 - h / 2.0;
                    if (cdx * cdx + cdy * cdy <= 100 && a > centreMax) centreMax = a;
                    double sd = RoundedSquareSdf(x + 0.5 - w / 2.0, y + 0.5 - h / 2.0, 88, 44);
                    if (sd >= 1 && sd <= 5) { rb += a; rbn++; }
                    bool border = x < 2 || y < 2 || x >= w - 2 || y >= h - 2;
                    if (border && a > maxBorder) { maxBorder = a; maxBorderFrame = fi + 1; }
                    if (a == 0) { if (r != 0 || g != 0 || b != 0) transparentRgb++; continue; }
                    // Premultiplied colour must not exceed alpha (after 8-bit rounding).
                    int pr = (int)Math.Round(r * a / 255.0), pg = (int)Math.Round(g * a / 255.0), pb = (int)Math.Round(b * a / 255.0);
                    if (pr > a || pg > a || pb > a) premulBad++;
                    if (a == 1) faint++;
                    bool lonely = true;
                    for (int dy = -1; dy <= 1 && lonely; dy++)
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            if (dx == 0 && dy == 0) continue;
                            int xx = x + dx, yy = y + dy; if (xx < 0 || yy < 0 || xx >= w || yy >= h) continue;
                            if (px[(yy * w + xx) * 4 + 3] != 0) { lonely = false; break; }
                        }
                    if (lonely) isolated++;
                    if (x < bx0) bx0 = x; if (y < by0) by0 = y; if (x > bx1) bx1 = x; if (y > by1) by1 = y;
                    s += a; sx += a * (x + 0.5); sy += a * (y + 0.5);
                }
            if (s > 0) { double cx = sx / s - w / 2.0, cy = sy / s - h / 2.0; maxDrift = Math.Max(maxDrift, Math.Sqrt(cx * cx + cy * cy)); }
            alphaSum.Add(s / 255.0);
            rimBand.Add(rbn > 0 ? rb / rbn / 255.0 : 0);
            prem.Add(Premul(px));
        }
        sb.AppendLine(string.Format("  size {0}x{1}", W, H));
        sb.AppendLine(string.Format("  maxAlphaWithin2pxOfBorder = {0} (frame {1})  [must be 0]", maxBorder, maxBorderFrame));
        sb.AppendLine(string.Format("  premultipliedRgbAboveAlpha = {0}  [must be 0]", premulBad));
        sb.AppendLine(string.Format("  transparentTexelsWithRgb = {0}  [must be 0]", transparentRgb));
        sb.AppendLine(string.Format("  faintTexels(alpha<2/255) = {0}  isolatedTexels = {1}", faint, isolated));
        sb.AppendLine(string.Format("  alphaBBox = x {0}..{1}, y {2}..{3}  (margin L{0} T{2} R{4} B{5})", bx0, bx1, by0, by1, W - 1 - bx1, H - 1 - by1));
        sb.AppendLine(string.Format("  maxCentroidOffsetFromPivot = {0:0.00} px", maxDrift));
        var cov = new StringBuilder();
        for (int i = 0; i < alphaSum.Count; i++) cov.Append(string.Format("{0}{1:0}", i == 0 ? "" : " ", alphaSum[i]));
        sb.AppendLine("  alphaSum per frame: " + cov);
        sb.AppendLine(string.Format("  maxAlphaWithin10pxOfPivot = {0} ({1:0.000})", centreMax, centreMax / 255.0));
        var rbs = new StringBuilder(); double rbMin = double.MaxValue, rbMax = 0, rbMean = 0;
        for (int i = 0; i < rimBand.Count; i++) { rbs.Append(string.Format("{0}{1:0.000}", i == 0 ? "" : " ", rimBand[i])); rbMin = Math.Min(rbMin, rimBand[i]); rbMax = Math.Max(rbMax, rimBand[i]); rbMean += rimBand[i]; }
        rbMean /= Math.Max(1, rimBand.Count);
        sb.AppendLine("  rim shoulder mean alpha per frame: " + rbs);
        sb.AppendLine(string.Format("  rim shoulder min/mean/max = {0:0.000}/{1:0.000}/{2:0.000} (+{3:0.0}% / -{4:0.0}% about mean)", rbMin, rbMean, rbMax, (rbMax / rbMean - 1) * 100, (1 - rbMin / rbMean) * 100));
        var steps = new StringBuilder(); double sum = 0, pairSum = 0, holdSum = 0; int pairN = 0, holdN = 0;
        for (int i = 1; i < prem.Count; i++)
        {
            double d = MeanAbs(prem[i - 1], prem[i]); sum += d;
            if (i % 2 == 0) { pairSum += d; pairN++; } else { holdSum += d; holdN++; }
            steps.Append(string.Format("{0}{1:0.00000}", i == 1 ? "" : " ", d));
        }
        sb.AppendLine("  consecutive steps: " + steps);
        double avg = prem.Count > 1 ? sum / (prem.Count - 1) : 0;
        sb.AppendLine(string.Format("  avgConsecutiveStep = {0:0.00000}", avg));
        if (loop && prem.Count > 1)
        {
            double wrap = MeanAbs(prem[prem.Count - 1], prem[0]);
            double pairAvg = pairN > 0 ? pairSum / pairN : 0, holdAvg = holdN > 0 ? holdSum / holdN : 0;
            sb.AppendLine(string.Format("  wrapStep(last->first) = {0:0.00000}  ratio to avg = {1:0.000}", wrap, avg > 0 ? wrap / avg : 0));
            sb.AppendLine(string.Format("  avg seed-change step (odd->even frame index) = {0:0.00000}  wrap/seed-change = {1:0.000};  avg hold step = {2:0.00000}", pairAvg, pairAvg > 0 ? wrap / pairAvg : 0, holdAvg));
        }
        return sb.ToString();
    }

    static double RoundedSquareSdf(double x, double y, double half, double corner)
    {
        double qx = Math.Abs(x) - (half - corner), qy = Math.Abs(y) - (half - corner);
        return Math.Sqrt(Math.Max(qx, 0) * Math.Max(qx, 0) + Math.Max(qy, 0) * Math.Max(qy, 0)) + Math.Min(Math.Max(qx, qy), 0) - corner;
    }

    // Max and mean byte difference between two PNGs (identical => 0 / 0).
    public static string Compare(string a, string b)
    {
        int w1, h1, w2, h2; var p = Load(a, out w1, out h1); var q = Load(b, out w2, out h2);
        if (w1 != w2 || h1 != h2) return "size mismatch";
        int mx = 0; double mean = 0;
        for (int i = 0; i < p.Length; i++) { int d = Math.Abs(p[i] - q[i]); if (d > mx) mx = d; mean += d; }
        return string.Format("maxByteDiff = {0}, meanByteDiff = {1:0.0000}", mx, mean / p.Length);
    }

    public static double Step(string a, string b)
    {
        int w, h; return MeanAbs(Premul(Load(a, out w, out h)), Premul(Load(b, out w, out h)));
    }

    // Review-only stand-in piece: a glossy orange-red rounded square (~168 px) so readability under the fill can be judged.
    public static void WriteMockPiece(string path)
    {
        const int N = 256; const double c = 128, half = 84, rad = 40;
        using (var bmp = new Bitmap(N, N, PixelFormat.Format32bppArgb))
        {
            var data = bmp.LockBits(new Rectangle(0, 0, N, N), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            var px = new byte[N * N * 4];
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    double acc = 0;
                    for (int j = 0; j < 3; j++) for (int i = 0; i < 3; i++)
                        {
                            double qx = Math.Abs(x + (i + 0.5) / 3 - c) - (half - rad), qy = Math.Abs(y + (j + 0.5) / 3 - c) - (half - rad);
                            double d = Math.Sqrt(Math.Max(qx, 0) * Math.Max(qx, 0) + Math.Max(qy, 0) * Math.Max(qy, 0)) + Math.Min(Math.Max(qx, qy), 0) - rad;
                            if (d < 0) acc += 1;
                        }
                    double a = acc / 9; if (a <= 0) continue;
                    double v = (y - (c - half)) / (2 * half);
                    double r = 0.93 - 0.25 * v, g = 0.28 - 0.14 * v, b = 0.18 - 0.08 * v;
                    double hx = (x - 104) / 46.0, hy = (y - 78) / 22.0, hl = Math.Exp(-(hx * hx + hy * hy)) * 0.55;
                    r += (1 - r) * hl; g += (1 - g) * hl; b += (1 - b) * hl;
                    int k = (y * N + x) * 4;
                    px[k] = (byte)(Math.Max(0, Math.Min(1, b)) * 255); px[k + 1] = (byte)(Math.Max(0, Math.Min(1, g)) * 255);
                    px[k + 2] = (byte)(Math.Max(0, Math.Min(1, r)) * 255); px[k + 3] = (byte)(a * 255);
                }
            for (int y = 0; y < N; y++) Marshal.Copy(px, y * N * 4, IntPtr.Add(data.Scan0, y * data.Stride), N * 4);
            bmp.UnlockBits(data);
            bmp.Save(path, ImageFormat.Png);
        }
    }
}
