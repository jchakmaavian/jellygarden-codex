// Independent reviewer checks for ColorBall_TargetImpact frames (C# 5, loads PNGs directly, no dependency on the builder code).
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

public static class ReviewTargetImpact
{
    public static byte[] Load(string path, out int w, out int h)
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

    // Raw PNG bytes for IDAT-independent colour check: GDI+ may alter RGB of alpha-0 texels, so also check file-level via PngRaw when needed.
    static double StepDiff(byte[] a, byte[] b)
    {
        double d = 0; int n = a.Length / 4;
        for (int i = 0; i < n; i++)
        {
            double aa = a[i * 4 + 3] / 255.0, ba = b[i * 4 + 3] / 255.0;
            for (int c = 0; c < 3; c++) d += Math.Abs(a[i * 4 + c] / 255.0 * aa - b[i * 4 + c] / 255.0 * ba);
            d += Math.Abs(aa - ba);
        }
        return d / (n * 4.0);
    }

    public static string Run(string dir, string prefix, int expected, double cx, double cy, double cell)
    {
        var sb = new StringBuilder();
        var all = Directory.GetFiles(dir, "*.png");
        sb.AppendLine("pngs in dir: " + all.Length + " expected " + expected);
        var frames = new List<byte[]>();
        int W = 0, H = 0;
        for (int i = 1; i <= expected; i++)
        {
            string p = Path.Combine(dir, string.Format("{0}_{1:000}.png", prefix, i));
            if (!File.Exists(p)) { sb.AppendLine("MISSING " + p); frames.Add(null); continue; }
            int w, h; frames.Add(Load(p, out w, out h));
            if (i == 1) { W = w; H = h; }
            if (w != 384 || h != 384) sb.AppendLine("BAD SIZE f" + i + " " + w + "x" + h);
        }
        sb.AppendLine("size " + W + "x" + H);
        sb.AppendLine("f  | nz     asum     maxA | bord2 bord4 minEdge | a0rgb aByte1 strayA<8comp strayFar | cenX   cenY   | Rout(cov>=.5) Rin  rim  | far128 | darkPx khakiPx greyPx blueish | step");
        byte[] prev = null;
        var steps = new List<double>();
        for (int f = 0; f < frames.Count; f++)
        {
            var px = frames[f]; if (px == null) continue;
            int nz = 0, maxA = 0, bord2 = 0, bord4 = 0, minEdge = 9999, a0rgb = 0, ab1 = 0;
            double asum = 0, sx = 0, sy = 0;
            int dark = 0, khaki = 0, grey = 0, blueish = 0;
            int nb = 400;
            var covCnt = new double[nb]; var tot = new double[nb];
            double far128 = 0;
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    int i = (y * W + x) * 4; int a = px[i + 3]; int b = px[i], g = px[i + 1], r = px[i + 2];
                    double dx = x + 0.5 - cx, dy = y + 0.5 - cy, rr = Math.Sqrt(dx * dx + dy * dy);
                    int bin = (int)rr; if (bin < nb) { tot[bin] += 1; if (a >= 128) covCnt[bin] += 1; }
                    if (a == 0) { if (r != 0 || g != 0 || b != 0) a0rgb++; continue; }
                    nz++; asum += a / 255.0; sx += a * (x + 0.5); sy += a * (y + 0.5); if (a > maxA) maxA = a;
                    if (a == 1) ab1++;
                    int e = Math.Min(Math.Min(x, y), Math.Min(W - 1 - x, H - 1 - y));
                    if (e < minEdge) minEdge = e;
                    if (e < 2 && a > bord2) bord2 = a;
                    if (e < 4 && a > bord4) bord4 = a;
                    if (a >= 128 && rr > far128) far128 = rr;
                    if (a >= 40)
                    {
                        // Straight colour quality. Palette is R=255 warm; anything with low R / low value is muddy.
                        int mx = Math.Max(r, Math.Max(g, b));
                        if (mx < 200) dark++;
                        if (r < 235) grey++;
                        if (b > g + 8) blueish++;
                    }
                    // khaki test: composite over navy #20242e and check for low-saturation mid-value tan
                    if (a >= 60 && a <= 235)
                    {
                        double al = a / 255.0;
                        double cr = r * al + 32 * (1 - al), cg = g * al + 36 * (1 - al), cbb = b * al + 46 * (1 - al);
                        double mx2 = Math.Max(cr, Math.Max(cg, cbb)), mn2 = Math.Min(cr, Math.Min(cg, cbb));
                        double sat = mx2 > 0 ? (mx2 - mn2) / mx2 : 0;
                        if (mx2 < 215 && mx2 > 120 && sat > 0.2 && sat < 0.55 && cr > cbb) khaki++;
                    }
                }
            // stray: components (8-conn) of a>0; report components whose max alpha < 8, and pixels a>0 with no a>=24 within 40 px
            int strayComp = 0, strayCompPx = 0;
            var lab = new int[W * H];
            var stack = new Stack<int>();
            int compId = 0;
            for (int s = 0; s < W * H; s++)
            {
                if (px[s * 4 + 3] == 0 || lab[s] != 0) continue;
                compId++; int cnt = 0, cmax = 0; stack.Push(s); lab[s] = compId;
                while (stack.Count > 0)
                {
                    int q = stack.Pop(); cnt++; int qa = px[q * 4 + 3]; if (qa > cmax) cmax = qa;
                    int qx = q % W, qy = q / W;
                    for (int oy = -1; oy <= 1; oy++) for (int ox = -1; ox <= 1; ox++)
                        {
                            int nx = qx + ox, ny = qy + oy; if (nx < 0 || ny < 0 || nx >= W || ny >= H) continue;
                            int n = ny * W + nx; if (lab[n] != 0 || px[n * 4 + 3] == 0) continue; lab[n] = compId; stack.Push(n);
                        }
                }
                if (cmax < 8) { strayComp++; strayCompPx += cnt; }
            }
            var ps = new int[(W + 1) * (H + 1)];
            for (int y = 0; y < H; y++) for (int x = 0; x < W; x++)
                    ps[(y + 1) * (W + 1) + x + 1] = (px[(y * W + x) * 4 + 3] >= 24 ? 1 : 0) + ps[y * (W + 1) + x + 1] + ps[(y + 1) * (W + 1) + x] - ps[y * (W + 1) + x];
            int strayFar = 0;
            for (int y = 0; y < H; y++) for (int x = 0; x < W; x++)
                {
                    if (px[(y * W + x) * 4 + 3] == 0) continue;
                    int x0 = Math.Max(0, x - 40), x1 = Math.Min(W, x + 41), y0 = Math.Max(0, y - 40), y1 = Math.Min(H, y + 41);
                    int c = ps[y1 * (W + 1) + x1] - ps[y0 * (W + 1) + x1] - ps[y1 * (W + 1) + x0] + ps[y0 * (W + 1) + x0];
                    if (c == 0) strayFar++;
                }
            double rout = 0, rin = -1;
            for (int k = 0; k < nb; k++) { if (tot[k] > 0 && covCnt[k] / tot[k] >= 0.5) { rout = k + 1; if (rin < 0) rin = k; } }
            double step = 0;
            if (prev != null) { step = StepDiff(prev, px) * 1000; steps.Add(step); }
            prev = px;
            double cenX = asum > 0 ? sx / (asum * 255) : 0, cenY = asum > 0 ? sy / (asum * 255) : 0;
            sb.AppendLine(string.Format("{0,2} | {1,6} {2,8:0.0} {3,3} | {4,3} {5,3} {6,4} | {7,4} {8,4} {9,3}/{10,-4} {11,4} | {12,6:0.0} {13,6:0.0} | {14,5:0.0}({15:0.000}c) {16,5:0.0} {17,5:0.0}({18:0.000}c) | {19,5:0.0} | {20,5} {21,5} {22,5} {23,5} | {24,6:0.00}",
                f + 1, nz, asum, maxA, bord2, bord4, minEdge == 9999 ? -1 : minEdge, a0rgb, ab1, strayComp, strayCompPx, strayFar, cenX, cenY,
                rout, 2 * rout / cell, rin, rout - Math.Max(0, rin), (rout - Math.Max(0, rin)) / cell, far128, dark, khaki, grey, blueish, step));
        }
        steps.Sort();
        sb.AppendLine("median step " + steps[steps.Count / 2].ToString("0.00"));
        return sb.ToString();
    }

    // Radial + angular analysis of one frame: angles of alpha peaks on a circle, and FWHM across a ray at radius r.
    public static string Needles(string path, double cx, double cy, double r, double thr)
    {
        int w, h; var px = Load(path, out w, out h);
        var sb = new StringBuilder();
        int N = 1440; var prof = new double[N];
        for (int k = 0; k < N; k++)
        {
            double a = k * 2 * Math.PI / N; int x = (int)(cx + Math.Cos(a) * r), y = (int)(cy + Math.Sin(a) * r);
            prof[k] = px[(y * w + x) * 4 + 3];
        }
        double bg = 1e9; for (int k = 0; k < N; k++) if (prof[k] < bg) bg = prof[k];
        sb.AppendLine("r=" + r + " min alpha on circle " + bg);
        for (int k = 0; k < N; k++)
        {
            double p = prof[k], pl = prof[(k + N - 1) % N], pr = prof[(k + 1) % N];
            if (p >= thr && p >= pl && p > pr)
            {
                double deg = k * 360.0 / N;
                // FWHM across direction (perpendicular) at this radius, measured in px along the tangent
                double half = bg + (p - bg) / 2; int wpx = 0;
                double a = deg * Math.PI / 180, tx = -Math.Sin(a), ty = Math.Cos(a);
                double ex = cx + Math.Cos(a) * r, ey = cy + Math.Sin(a) * r;
                var cs = new StringBuilder();
                for (double t = -8; t <= 8; t += 1)
                {
                    int x = (int)Math.Round(ex + tx * t - 0.5), y = (int)Math.Round(ey + ty * t - 0.5);
                    int av = px[(y * w + x) * 4 + 3]; if (av >= half) wpx++;
                    cs.Append(av).Append(' ');
                }
                sb.AppendLine(string.Format("peak at {0:0.0} deg alpha {1} widthAboveHalf {2}px  cross: {3}", deg, p, wpx, cs));
                // reach along the ray
                double reach = 0;
                for (double rr = 0; rr < 190; rr += 0.5)
                {
                    int x = (int)(cx + Math.Cos(a) * rr), y = (int)(cy + Math.Sin(a) * rr);
                    if (x < 0 || y < 0 || x >= w || y >= h) break;
                    if (px[(y * w + x) * 4 + 3] >= 64) reach = rr;
                }
                sb.AppendLine("   reach(a>=64) " + reach.ToString("0.0") + " px = " + (reach / 192).ToString("0.000") + " cell");
            }
        }
        return sb.ToString();
    }

    // Reach along a given angle (deg) for alpha >= thr (searching a +-2 deg fan), per frame.
    public static string Reach(string dir, string prefix, int f0, int f1, double cx, double cy, double[] angs, int thr)
    {
        var sb = new StringBuilder();
        for (int f = f0; f <= f1; f++)
        {
            int w, h; var px = Load(Path.Combine(dir, string.Format("{0}_{1:000}.png", prefix, f)), out w, out h);
            sb.Append("f" + f + ":");
            foreach (double ad in angs)
            {
                double best = 0;
                for (double da = -2; da <= 2; da += 0.25)
                {
                    double a = (ad + da) * Math.PI / 180;
                    double reach = 0;
                    for (double rr = 0; rr < 190; rr += 0.5)
                    {
                        int x = (int)(cx + Math.Cos(a) * rr), y = (int)(cy + Math.Sin(a) * rr);
                        if (px[(y * w + x) * 4 + 3] >= thr) reach = rr;
                    }
                    if (reach > best) best = reach;
                }
                sb.Append(string.Format("  {0:0}deg {1:0.0}px({2:0.00}c)", ad, best, best / 192));
            }
            sb.AppendLine();
        }
        return sb.ToString();
    }

    // Blob list: connected components of alpha >= thr, with centre, radius from pivot, bbox size, peak alpha, mean straight colour.
    public static string Blobs(string path, int thr, double cx, double cy, double minR)
    {
        int w, h; var px = Load(path, out w, out h);
        var lab = new int[w * h]; var sb = new StringBuilder(); int id = 0; var st = new Stack<int>();
        for (int s = 0; s < w * h; s++)
        {
            if (px[s * 4 + 3] < thr || lab[s] != 0) continue;
            id++; st.Push(s); lab[s] = id; int n = 0, mnx = 9999, mny = 9999, mxx = -1, mxy = -1, pk = 0; double ssx = 0, ssy = 0, ssa = 0, sr = 0, sg = 0, sbb = 0;
            while (st.Count > 0)
            {
                int q = st.Pop(); n++; int qx = q % w, qy = q / w; int a = px[q * 4 + 3];
                mnx = Math.Min(mnx, qx); mxx = Math.Max(mxx, qx); mny = Math.Min(mny, qy); mxy = Math.Max(mxy, qy); if (a > pk) pk = a;
                ssx += a * (qx + 0.5); ssy += a * (qy + 0.5); ssa += a; sr += px[q * 4 + 2] * a; sg += px[q * 4 + 1] * a; sbb += px[q * 4] * a;
                for (int oy = -1; oy <= 1; oy++) for (int ox = -1; ox <= 1; ox++)
                    {
                        int nx = qx + ox, ny = qy + oy; if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                        int m = ny * w + nx; if (lab[m] != 0 || px[m * 4 + 3] < thr) continue; lab[m] = id; st.Push(m);
                    }
            }
            double bx = ssx / ssa, by = ssy / ssa, rr = Math.Sqrt((bx - cx) * (bx - cx) + (by - cy) * (by - cy));
            if (rr < minR) continue;
            sb.AppendLine(string.Format("blob n={0,5} centre=({1:0.0},{2:0.0}) r={3:0.0}px ({4:0.00}c) bbox={5}x{6} peakA={7} meanRGB=({8:0},{9:0},{10:0})",
                n, bx, by, rr, rr / 192, mxx - mnx + 1, mxy - mny + 1, pk, sr / ssa, sg / ssa, sbb / ssa));
        }
        return sb.ToString();
    }

    // Edge-change energy: mean abs alpha diff restricted to the annulus [r0,r1] for consecutive frames.
    public static string Annulus(string dir, string prefix, int f0, int f1, double cx, double cy, double r0, double r1)
    {
        var sb = new StringBuilder(); byte[] prev = null;
        for (int f = f0; f <= f1; f++)
        {
            int w, h; var px = Load(Path.Combine(dir, string.Format("{0}_{1:000}.png", prefix, f)), out w, out h);
            if (prev != null)
            {
                double d = 0; int n = 0;
                for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
                    {
                        double dx = x + 0.5 - cx, dy = y + 0.5 - cy, rr = Math.Sqrt(dx * dx + dy * dy);
                        if (rr < r0 || rr > r1) continue; int i = (y * w + x) * 4; d += Math.Abs(px[i + 3] - prev[i + 3]); n++;
                    }
                sb.AppendLine(string.Format("{0}->{1}: {2:0.00}", f - 1, f, d / n));
            }
            prev = px;
        }
        return sb.ToString();
    }

    public static string Compare(string a, string b, string prefix, int count)
    {
        var sb = new StringBuilder(); int same = 0, missing = 0;
        for (int i = 1; i <= count; i++)
        {
            string pa = Path.Combine(a, string.Format("{0}_{1:000}.png", prefix, i)), pb = Path.Combine(b, string.Format("{0}_{1:000}.png", prefix, i));
            if (!File.Exists(pa) || !File.Exists(pb)) { missing++; continue; }
            var ba = File.ReadAllBytes(pa); var bb = File.ReadAllBytes(pb);
            bool eq = ba.Length == bb.Length; if (eq) for (int k = 0; k < ba.Length; k++) if (ba[k] != bb[k]) { eq = false; break; }
            if (eq) same++; else sb.AppendLine("DIFF frame " + i);
        }
        sb.AppendLine("byte-identical " + same + "/" + count + " missing " + missing);
        return sb.ToString();
    }
}
