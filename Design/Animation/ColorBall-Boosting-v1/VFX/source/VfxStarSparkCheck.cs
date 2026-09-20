// Automated frame checks for ColorBall_StarSpark clips (works for any folder of <Asset>_NNN.png frames).
// C# 5, standalone (System.Drawing only).
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

public static class VfxStarSparkCheck
{
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

    // Premultiplied RGBA floats (0..1) from straight BGRA bytes.
    static float[] Premul(byte[] px)
    {
        int n = px.Length / 4; var o = new float[n * 4];
        for (int i = 0; i < n; i++)
        {
            float a = px[i * 4 + 3] / 255f;
            o[i * 4] = px[i * 4 + 2] / 255f * a; o[i * 4 + 1] = px[i * 4 + 1] / 255f * a; o[i * 4 + 2] = px[i * 4] / 255f * a; o[i * 4 + 3] = a;
        }
        return o;
    }

    static double Diff(float[] a, float[] b)
    {
        double d = 0; for (int i = 0; i < a.Length; i++) d += Math.Abs(a[i] - b[i]);
        return d / a.Length;
    }

    /// <summary>Check one clip folder. border = transparent margin in px that must be alpha 0. cx, cy = expected pivot.</summary>
    public static string Run(string framesDir, string clipName, bool isLoop, int border, double cx, double cy)
    {
        var files = Directory.GetFiles(framesDir, "*.png");
        Array.Sort(files, StringComparer.Ordinal);
        var sb = new StringBuilder();
        sb.AppendLine("== " + clipName + "  (" + files.Length + " frames, " + framesDir + ")");
        if (files.Length == 0) { sb.AppendLine("  NO FRAMES"); return sb.ToString(); }

        int maxBorderA = 0, maxBorderFrame = -1;
        long premulGt = 0, dirty = 0, faint = 0, straightGt = 0;
        double maxRadius = 0, maxCentroidDev = 0;
        int w = 0, h = 0;
        float[] prev = null, first = null; double stepSum = 0; int steps = 0;
        var perFrame = new StringBuilder();
        bool naming = true;
        for (int f = 0; f < files.Length; f++)
        {
            string expect = string.Format("_{0:000}.png", f + 1);
            if (!files[f].EndsWith(expect, StringComparison.Ordinal)) naming = false;
            var px = LoadBgra(files[f], out w, out h);
            int frameMaxA = 0, frameBorderA = 0; double sa = 0, sx = 0, sy = 0, frameR = 0;
            long frameFaint = 0;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int k = (y * w + x) * 4;
                    int b = px[k], g = px[k + 1], r = px[k + 2], a = px[k + 3];
                    if (a > frameMaxA) frameMaxA = a;
                    bool edge = x < border || y < border || x >= w - border || y >= h - border;
                    if (edge && a > frameBorderA) frameBorderA = a;
                    if (a == 0 && (r | g | b) != 0) dirty++;
                    if (a > 0 && a < 2) { faint++; frameFaint++; }
                    // Premultiplied texel r*a/255 (rounded) must not exceed a.
                    if ((r * a + 127) / 255 > a || (g * a + 127) / 255 > a || (b * a + 127) / 255 > a) premulGt++;
                    if (a > 0 && (r > a || g > a || b > a)) straightGt++;
                    if (a > 0)
                    {
                        double dx = x + 0.5 - cx, dy = y + 0.5 - cy, rr = Math.Sqrt(dx * dx + dy * dy);
                        if (rr > frameR) frameR = rr;
                        sa += a; sx += a * (x + 0.5); sy += a * (y + 0.5);
                    }
                }
            double dev = sa > 0 ? Math.Sqrt((sx / sa - cx) * (sx / sa - cx) + (sy / sa - cy) * (sy / sa - cy)) : 0;
            if (dev > maxCentroidDev) maxCentroidDev = dev;
            if (frameR > maxRadius) maxRadius = frameR;
            if (frameBorderA > maxBorderA) { maxBorderA = frameBorderA; maxBorderFrame = f + 1; }
            var cur = Premul(px);
            if (prev != null) { stepSum += Diff(prev, cur); steps++; }
            if (first == null) first = cur;
            perFrame.AppendFormat("  f{0:000} maxA={1,3} borderA={2} outerR={3,5:0.0} centroidDev={4:0.000} faint={5}{6}",
                f + 1, frameMaxA, frameBorderA, frameR, dev, frameFaint, Environment.NewLine);
            prev = cur;
        }
        double lastToFirst = Diff(prev, first), avgStep = steps > 0 ? stepSum / steps : 0;
        sb.Append(perFrame.ToString());
        sb.AppendLine("  size                      : " + w + "x" + h);
        sb.AppendLine("  naming 1-based sequential : " + (naming ? "OK" : "FAIL"));
        sb.AppendLine("  max alpha within " + border + " px of border: " + maxBorderA + (maxBorderA == 0 ? "  OK" : "  FAIL (frame " + maxBorderFrame + ")"));
        sb.AppendLine("  premultiplied RGB > alpha : " + premulGt + (premulGt == 0 ? "  OK" : "  FAIL"));
        sb.AppendLine("  dirty transparent texels  : " + dirty + (dirty == 0 ? "  OK" : "  FAIL") + "   (alpha 0 with RGB != 0)");
        sb.AppendLine("  faint texels (alpha 1/255): " + faint + (faint == 0 ? "  OK" : "  WARN"));
        sb.AppendLine("  straight RGB byte > alpha : " + straightGt + "   (info only: PNG is straight alpha, bright light at partial alpha is expected)");
        sb.AppendLine("  outermost visible radius  : " + maxRadius.ToString("0.0") + " px from (" + cx + "," + cy + ")");
        sb.AppendLine("  max centroid drift        : " + maxCentroidDev.ToString("0.000") + " px");
        sb.AppendLine(string.Format("  last->first diff {0:0.00000} vs avg step {1:0.00000} (ratio {2:0.00}){3}",
            lastToFirst, avgStep, avgStep > 0 ? lastToFirst / avgStep : 0, isLoop ? (avgStep > 0 && lastToFirst / avgStep <= 1.5 ? "  OK loop" : "  FAIL loop") : "  (not a loop: info only)"));
        return sb.ToString();
    }

    static void Hsv(double r, double g, double b, out double s, out double v)
    {
        double mx = Math.Max(r, Math.Max(g, b)), mn = Math.Min(r, Math.Min(g, b)); v = mx; s = mx > 0 ? (mx - mn) / mx : 0;
    }

    /// <summary>
    /// Colour acceptance metrics for one frame, composited on the navy board #20242e:
    /// solid = pixels with alpha >= 60: count, mean value, saturation of the mean colour, mean per-pixel saturation;
    /// edge = alpha 60..200 band: mean composite colour, saturation, value; peak alpha; total premultiplied luminance.
    /// </summary>
    public static string Colour(string path, string label)
    {
        int w, h; var px = LoadBgra(path, out w, out h);
        double[] bg = { 0x20 / 255.0, 0x24 / 255.0, 0x2e / 255.0 };
        int nS = 0, nE = 0, peak = 0; double vS = 0, psS = 0, rS = 0, gS = 0, bS = 0, rE = 0, gE = 0, bE = 0, light = 0;
        for (int i = 0; i < w * h; i++)
        {
            int a = px[i * 4 + 3]; if (a == 0) continue;
            if (a > peak) peak = a;
            double al = a / 255.0, r = px[i * 4 + 2] / 255.0, g = px[i * 4 + 1] / 255.0, b = px[i * 4] / 255.0;
            light += (0.2126 * r + 0.7152 * g + 0.0722 * b) * al;
            if (a < 60) continue;
            double cr = r * al + bg[0] * (1 - al), cg = g * al + bg[1] * (1 - al), cb = b * al + bg[2] * (1 - al);
            double s, v; Hsv(cr, cg, cb, out s, out v);
            nS++; vS += v; psS += s; rS += cr; gS += cg; bS += cb;
            if (a <= 200) { nE++; rE += cr; gE += cg; bE += cb; }
        }
        var sb = new StringBuilder(label.PadRight(12));
        if (nS > 0)
        {
            double s, v; Hsv(rS / nS, gS / nS, bS / nS, out s, out v);
            sb.AppendFormat("a>=60 n={0,4} meanV={1:0.00} satMean={2:0.00} pxSat={3:0.00}", nS, vS / nS, s, psS / nS);
        }
        else sb.Append("a>=60 n=   0                                      ");
        if (nE > 0)
        {
            double s, v; Hsv(rE / nE, gE / nE, bE / nE, out s, out v);
            sb.AppendFormat(" | edge60-200 n={0,4} #{1:X2}{2:X2}{3:X2} sat={4:0.00} v={5:0.00}", nE, (int)(rE / nE * 255), (int)(gE / nE * 255), (int)(bE / nE * 255), s, v);
        }
        sb.AppendFormat(" | peakA={0,3} light={1,6:0.0}", peak, light);
        return sb.ToString();
    }

    /// <summary>Straight alpha along +x from the pivot, every 2 px (pixel containing the point just below the axis).</summary>
    public static string Reach(string path, double cx, double cy)
    {
        int w, h; var px = LoadBgra(path, out w, out h);
        var sb = new StringBuilder(Path.GetFileName(path) + " +x alpha:");
        for (int r = 30; r <= 62; r += 2)
        {
            int x = (int)Math.Floor(cx + r), y = (int)Math.Floor(cy);
            if (x >= w) break;
            int a = Math.Max(px[(y * w + x) * 4 + 3], px[((y - 1) * w + x) * 4 + 3]);
            sb.AppendFormat(" r{0}:{1}", r, a);
        }
        double m32 = 0, m64 = 0, m128 = 0;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int a = px[(y * w + x) * 4 + 3]; if (a < 32) continue;
                double dx = x + 0.5 - cx, dy = y + 0.5 - cy, rr = Math.Sqrt(dx * dx + dy * dy);
                if (rr > m32) m32 = rr; if (a >= 64 && rr > m64) m64 = rr; if (a >= 128 && rr > m128) m128 = rr;
            }
        sb.AppendFormat("  | reach a>=32 {0:0.0}  a>=64 {1:0.0}  a>=128 {2:0.0}", m32, m64, m128);
        return sb.ToString();
    }
}
