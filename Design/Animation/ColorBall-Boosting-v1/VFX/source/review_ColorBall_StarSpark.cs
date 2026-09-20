// Independent reviewer checker for ColorBall_StarSpark frames. C# 5.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

public static class ReviewStarSpark
{
    public class Frame
    {
        public int W, H;
        public byte[] R, G, B, A; // straight
    }

    public static Frame Load(string path)
    {
        using (var bmp = new Bitmap(path))
        {
            var f = new Frame();
            f.W = bmp.Width; f.H = bmp.Height;
            int n = f.W * f.H;
            f.R = new byte[n]; f.G = new byte[n]; f.B = new byte[n]; f.A = new byte[n];
            var data = bmp.LockBits(new Rectangle(0, 0, f.W, f.H), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            var px = new byte[data.Stride * f.H];
            Marshal.Copy(data.Scan0, px, 0, px.Length);
            bmp.UnlockBits(data);
            for (int y = 0; y < f.H; y++)
                for (int x = 0; x < f.W; x++)
                {
                    int o = y * data.Stride + x * 4, i = y * f.W + x;
                    f.B[i] = px[o]; f.G[i] = px[o + 1]; f.R[i] = px[o + 2]; f.A[i] = px[o + 3];
                }
            return f;
        }
    }

    static double Lum(double r, double g, double b) { return 0.2126 * r + 0.7152 * g + 0.0722 * b; }

    // Mean abs diff of premultiplied RGBA (0..1) between two frames.
    static double Diff(Frame a, Frame b)
    {
        double s = 0; int n = a.W * a.H;
        for (int i = 0; i < n; i++)
        {
            double aa = a.A[i] / 255.0, ba = b.A[i] / 255.0;
            s += Math.Abs(a.R[i] / 255.0 * aa - b.R[i] / 255.0 * ba);
            s += Math.Abs(a.G[i] / 255.0 * aa - b.G[i] / 255.0 * ba);
            s += Math.Abs(a.B[i] / 255.0 * aa - b.B[i] / 255.0 * ba);
            s += Math.Abs(aa - ba);
        }
        return s / (n * 4.0);
    }

    public static string Run(string dir, string clip, int expectCount, bool lastMustBeEmpty, double pcx, double pcy)
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== " + clip + " : " + dir);
        var files = Directory.GetFiles(dir, "*.png");
        Array.Sort(files, StringComparer.Ordinal);
        sb.AppendLine("frame files: " + files.Length + " (expected " + expectCount + ")" + (files.Length == expectCount ? " OK" : " FAIL"));
        for (int k = 0; k < files.Length; k++)
        {
            string want = string.Format("ColorBall_StarSpark_{0:000}.png", k + 1);
            if (Path.GetFileName(files[k]) != want) sb.AppendLine("  NAME FAIL: " + Path.GetFileName(files[k]) + " expected " + want);
        }
        var frames = new List<Frame>();
        foreach (var p in files) frames.Add(Load(p));
        int fails = 0;
        sb.AppendLine(" f | W x H | maxA | bord2 bord3 | a0rgb | a1 a<2 | isol | rMax(a>0) rMax(a>=32) rMax(a>=128) | cenX cenY | sumA light | Rg | rot4 | lum>bg(dark%) ");
        double[] light = new double[frames.Count];
        for (int k = 0; k < frames.Count; k++)
        {
            var f = frames[k];
            int W = f.W, H = f.H;
            int maxA = 0, bord2 = 0, bord3 = 0, a0rgb = 0, a1 = 0, isol = 0;
            double rMax0 = 0, rMax32 = 0, rMax128 = 0, sx = 0, sy = 0, sa = 0, sl = 0, sr2 = 0;
            double hr = 0, hi = 0;
            int visible = 0, darker = 0;
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    int i = y * W + x; int a = f.A[i];
                    if (a > maxA) maxA = a;
                    bool b2 = x < 2 || y < 2 || x >= W - 2 || y >= H - 2;
                    bool b3 = x < 3 || y < 3 || x >= W - 3 || y >= H - 3;
                    if (b2 && a > bord2) bord2 = a;
                    if (b3 && a > bord3) bord3 = a;
                    if (a == 0 && (f.R[i] | f.G[i] | f.B[i]) != 0) a0rgb++;
                    if (a == 1) a1++;
                    if (a == 0) continue;
                    // isolated: all 8 neighbours alpha 0
                    bool anyN = false;
                    for (int dy = -1; dy <= 1 && !anyN; dy++)
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            if (dx == 0 && dy == 0) continue;
                            int xx = x + dx, yy = y + dy;
                            if (xx < 0 || yy < 0 || xx >= W || yy >= H) continue;
                            if (f.A[yy * W + xx] > 0) { anyN = true; break; }
                        }
                    if (!anyN) isol++;
                    double px = x + 0.5 - pcx, py = y + 0.5 - pcy, r = Math.Sqrt(px * px + py * py);
                    if (r > rMax0) rMax0 = r;
                    if (a >= 32 && r > rMax32) rMax32 = r;
                    if (a >= 128 && r > rMax128) rMax128 = r;
                    double al = a / 255.0;
                    sx += al * (x + 0.5); sy += al * (y + 0.5); sa += al;
                    double L = Lum(f.R[i] / 255.0, f.G[i] / 255.0, f.B[i] / 255.0);
                    sl += L * al;
                    sr2 += al * r * r;
                    double th = Math.Atan2(py, px);
                    hr += al * r * r * Math.Cos(4 * th); hi += al * r * r * Math.Sin(4 * th);
                    // composite over dark navy: is the result lighter than bg for every channel (light-like)?
                    if (a >= 13)
                    {
                        visible++;
                        double cr = f.R[i] / 255.0 * al + (0x20 / 255.0) * (1 - al);
                        double cg = f.G[i] / 255.0 * al + (0x24 / 255.0) * (1 - al);
                        double cb = f.B[i] / 255.0 * al + (0x2e / 255.0) * (1 - al);
                        if (Lum(cr, cg, cb) <= Lum(0x20 / 255.0, 0x24 / 255.0, 0x2e / 255.0)) darker++;
                    }
                }
            light[k] = sl;
            double cenX = sa > 0 ? sx / sa : double.NaN, cenY = sa > 0 ? sy / sa : double.NaN;
            double rg = sa > 0 ? Math.Sqrt(sr2 / sa) : 0;
            double rot = (hr != 0 || hi != 0) ? Math.Atan2(hi, hr) / 4 * 180 / Math.PI : double.NaN;
            if (bord2 != 0) fails++;
            if (a0rgb != 0) fails++;
            if (W != 128 || H != 128) fails++;
            sb.AppendLine(string.Format("{0,2} | {1}x{2} | {3,3} | {4,3} {5,3} | {6} | {7} | {8} | {9,5:0.0} {10,5:0.0} {11,5:0.0} | {12,7:0.000} {13,7:0.000} | {14,7:0.0} {15,7:0.0} | {16,5:0.00} | {17,6:0.00} | {18}/{19}",
                k + 1, W, H, maxA, bord2, bord3, a0rgb, a1, isol, rMax0, rMax32, rMax128, cenX, cenY, sa, sl, rg, rot, darker, visible));
        }
        if (lastMustBeEmpty && frames.Count > 0)
        {
            var l = frames[frames.Count - 1]; int m = 0;
            for (int i = 0; i < l.A.Length; i++) if (l.A[i] > m) m = l.A[i];
            sb.AppendLine("last frame max alpha: " + m + (m == 0 ? " OK" : " FAIL"));
            if (m != 0) fails++;
        }
        if (frames.Count > 1)
        {
            double tot = 0; var steps = new StringBuilder();
            for (int k = 1; k < frames.Count; k++) { double d = Diff(frames[k - 1], frames[k]); tot += d; steps.Append(string.Format(" {0:0.0000}", d)); }
            double mean = tot / (frames.Count - 1);
            double seam = Diff(frames[frames.Count - 1], frames[0]);
            sb.AppendLine("steps:" + steps);
            sb.AppendLine(string.Format("mean step {0:0.0000}, last->first {1:0.0000} (ratio {2:0.00}) [info only for one-shots]", mean, seam, mean > 0 ? seam / mean : 0));
            var ls = new StringBuilder();
            foreach (var v in light) ls.Append(string.Format(" {0:0}", v));
            sb.AppendLine("light per frame:" + ls);
        }
        sb.AppendLine("hard FAIL count: " + fails);
        return sb.ToString();
    }

    // Alpha / colour profile along a ray from the pivot at angle deg (clockwise, y down), sampled every px (bilinear).
    public static string Profile(string path, double deg, double pcx, double pcy)
    {
        var f = Load(path);
        var sb = new StringBuilder();
        sb.AppendLine("profile " + Path.GetFileName(path) + " @ " + deg + " deg: r : A  R G B (straight)");
        double rad = deg * Math.PI / 180;
        for (int r = 0; r <= 62; r += 2)
        {
            double x = pcx + Math.Cos(rad) * r - 0.5, y = pcy + Math.Sin(rad) * r - 0.5;
            int xi = (int)Math.Round(x), yi = (int)Math.Round(y);
            if (xi < 0 || yi < 0 || xi >= f.W || yi >= f.H) break;
            int i = yi * f.W + xi;
            sb.AppendLine(string.Format("  r{0,2}: A{1,3}  #{2:X2}{3:X2}{4:X2}", r, f.A[i], f.R[i], f.G[i], f.B[i]));
        }
        return sb.ToString();
    }

    // Width profile of an arm: for radius r along 0 deg, count pixels across with alpha >= thr.
    public static string ArmWidth(string path, double pcx, double pcy, int thr)
    {
        var f = Load(path);
        var sb = new StringBuilder("arm width (a>=" + thr + ") along +x:");
        for (int r = 4; r <= 56; r += 4)
        {
            int x = (int)(pcx + r); int cnt = 0;
            for (int y = 0; y < f.H; y++) if (f.A[y * f.W + x] >= thr) cnt++;
            sb.Append(string.Format(" r{0}:{1}", r, cnt));
        }
        return sb.ToString();
    }

    // Compare two frame dirs byte-for-byte.
    public static string Compare(string dirA, string dirB)
    {
        var fa = Directory.GetFiles(dirA, "*.png"); Array.Sort(fa, StringComparer.Ordinal);
        int diffFrames = 0; int maxd = 0;
        foreach (var p in fa)
        {
            string q = Path.Combine(dirB, Path.GetFileName(p));
            if (!File.Exists(q)) { diffFrames++; continue; }
            var a = Load(p); var b = Load(q);
            bool d = false;
            for (int i = 0; i < a.A.Length; i++)
            {
                int m = Math.Max(Math.Abs(a.A[i] - b.A[i]), Math.Max(Math.Abs(a.R[i] - b.R[i]), Math.Max(Math.Abs(a.G[i] - b.G[i]), Math.Abs(a.B[i] - b.B[i]))));
                if (m > 0) { d = true; if (m > maxd) maxd = m; }
            }
            if (d) diffFrames++;
        }
        return "compare " + dirA + " vs rerender: frames differing " + diffFrames + " / " + fa.Length + ", max byte diff " + maxd;
    }
}

public static class ReviewStarSparkColour
{
    static void Hsv(double r, double g, double b, out double h, out double s, out double v)
    {
        double mx = Math.Max(r, Math.Max(g, b)), mn = Math.Min(r, Math.Min(g, b)); v = mx; s = mx > 0 ? (mx - mn) / mx : 0; double d = mx - mn;
        if (d < 1e-9) { h = 0; return; }
        if (mx == r) h = 60 * (((g - b) / d) % 6); else if (mx == g) h = 60 * ((b - r) / d + 2); else h = 60 * ((r - g) / d + 4);
        if (h < 0) h += 360;
    }
    public static string Stats(string path, string label)
    {
        var f = ReviewStarSpark.Load(path); var sb = new StringBuilder(label + ": ");
        int[] lo = { 250, 60, 8 }; int[] hi = { 255, 200, 60 }; string[] nm = { "body", "edge", "faint" };
        double[][] bgs = { new double[] { 0x20, 0x24, 0x2e }, new double[] { 0x8a, 0x93, 0xc8 } };
        for (int k = 0; k < 3; k++)
        {
            double sr = 0, sg = 0, sbb = 0; int n = 0; double[] cr = new double[2], cg = new double[2], cb = new double[2];
            for (int i = 0; i < f.A.Length; i++)
            {
                int a = f.A[i]; if (a < lo[k] || a > hi[k]) continue; n++; sr += f.R[i]; sg += f.G[i]; sbb += f.B[i]; double al = a / 255.0;
                for (int q = 0; q < 2; q++) { cr[q] += f.R[i] * al + bgs[q][0] * (1 - al); cg[q] += f.G[i] * al + bgs[q][1] * (1 - al); cb[q] += f.B[i] * al + bgs[q][2] * (1 - al); }
            }
            if (n == 0) { sb.Append(nm[k] + ":none "); continue; }
            double h, s, v; Hsv(sr / n / 255, sg / n / 255, sbb / n / 255, out h, out s, out v);
            sb.Append(string.Format("{0}[n={1} straight #{2:X2}{3:X2}{4:X2} sat{5:0.00}", nm[k], n, (int)(sr / n), (int)(sg / n), (int)(sbb / n), s));
            for (int q = 0; q < 2; q++)
            {
                double h2, s2, v2; Hsv(cr[q] / n / 255, cg[q] / n / 255, cb[q] / n / 255, out h2, out s2, out v2);
                sb.Append(string.Format(" {0}:#{1:X2}{2:X2}{3:X2} h{4:0} s{5:0.00} v{6:0.00}", q == 0 ? "navy" : "lav", (int)(cr[q] / n), (int)(cg[q] / n), (int)(cb[q] / n), h2, s2, v2));
            }
            sb.Append("] ");
        }
        int c = 64 * f.W + 64; sb.Append(string.Format(" centre A{0} #{1:X2}{2:X2}{3:X2}", f.A[c], f.R[c], f.G[c], f.B[c]));
        return sb.ToString();
    }
}
