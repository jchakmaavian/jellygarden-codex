// Independent reviewer checks for ColorBall_TargetGlow (reads PNGs only; no dependency on the builder's code).
// C# 5.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

public sealed class RvFrame
{
    public int W, H;
    public byte[] R, G, B, A;   // straight bytes as stored
    public double[] Pr, Pg, Pb, Pa; // premultiplied 0..1

    public static RvFrame Load(string path)
    {
        using (var bmp = new Bitmap(path))
        {
            var f = new RvFrame();
            f.W = bmp.Width; f.H = bmp.Height;
            int n = f.W * f.H;
            f.R = new byte[n]; f.G = new byte[n]; f.B = new byte[n]; f.A = new byte[n];
            f.Pr = new double[n]; f.Pg = new double[n]; f.Pb = new double[n]; f.Pa = new double[n];
            var data = bmp.LockBits(new Rectangle(0, 0, f.W, f.H), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            var px = new byte[f.W * 4];
            for (int y = 0; y < f.H; y++)
            {
                Marshal.Copy(IntPtr.Add(data.Scan0, y * data.Stride), px, 0, f.W * 4);
                for (int x = 0; x < f.W; x++)
                {
                    int i = y * f.W + x;
                    f.B[i] = px[x * 4]; f.G[i] = px[x * 4 + 1]; f.R[i] = px[x * 4 + 2]; f.A[i] = px[x * 4 + 3];
                    double a = f.A[i] / 255.0;
                    f.Pa[i] = a; f.Pr[i] = f.R[i] / 255.0 * a; f.Pg[i] = f.G[i] / 255.0 * a; f.Pb[i] = f.B[i] / 255.0 * a;
                }
            }
            bmp.UnlockBits(data);
            return f;
        }
    }
}

public static class ReviewColorBallTargetGlow
{
    const double Cx = 128, Cy = 128, Half = 88, Corner = 44;

    static double Sdf(double x, double y)
    {
        double px = Math.Abs(x - Cx), py = Math.Abs(y - Cy);
        double qx = px - (Half - Corner), qy = py - (Half - Corner);
        double ox = Math.Max(qx, 0), oy = Math.Max(qy, 0);
        return Math.Sqrt(ox * ox + oy * oy) + Math.Min(Math.Max(qx, qy), 0) - Corner;
    }

    public static List<RvFrame> LoadClip(string dir, string prefix, StringBuilder sb)
    {
        var list = new List<RvFrame>();
        var files = Directory.GetFiles(dir, "*.png");
        Array.Sort(files, StringComparer.Ordinal);
        sb.AppendLine("dir " + dir + " : " + files.Length + " png files");
        for (int k = 0; k < files.Length; k++)
        {
            string expect = string.Format("{0}_{1:000}.png", prefix, k + 1);
            if (Path.GetFileName(files[k]) != expect) sb.AppendLine("  NAME MISMATCH: " + Path.GetFileName(files[k]) + " expected " + expect);
            list.Add(RvFrame.Load(files[k]));
        }
        return list;
    }

    static double Step(RvFrame a, RvFrame b, Func<int, int, bool> mask)
    {
        double d = 0; int n = 0;
        for (int y = 0; y < a.H; y++)
            for (int x = 0; x < a.W; x++)
            {
                if (mask != null && !mask(x, y)) continue;
                int i = y * a.W + x;
                d += Math.Abs(a.Pr[i] - b.Pr[i]) + Math.Abs(a.Pg[i] - b.Pg[i]) + Math.Abs(a.Pb[i] - b.Pb[i]) + Math.Abs(a.Pa[i] - b.Pa[i]);
                n++;
            }
        return d / (n * 4.0);
    }

    static string Stats(List<double> v)
    {
        var s = new List<double>(v); s.Sort();
        double mean = 0; foreach (var x in s) mean += x; mean /= s.Count;
        double med = s[s.Count / 2];
        return string.Format("mean={0:0.00000} median={1:0.00000} min={2:0.00000} max={3:0.00000}", mean, med, s[0], s[s.Count - 1]);
    }

    public static string Check(string root, string prefix)
    {
        var sb = new StringBuilder();
        var on = LoadClip(Path.Combine(root, "On\\frames"), prefix, sb);
        var loop = LoadClip(Path.Combine(root, "Loop\\frames"), prefix, sb);
        sb.AppendLine(string.Format("On frames={0} (spec 10)  Loop frames={1} (spec 30)", on.Count, loop.Count));
        Basic("On", on, sb);
        Basic("Loop", loop, sb);

        // ---- hand-off
        int maxd = 0; long sum = 0;
        var a10 = on[on.Count - 1]; var l1 = loop[0];
        for (int i = 0; i < a10.A.Length; i++)
        {
            int d = Math.Max(Math.Abs(a10.A[i] - l1.A[i]), Math.Max(Math.Abs(a10.R[i] - l1.R[i]), Math.Max(Math.Abs(a10.G[i] - l1.G[i]), Math.Abs(a10.B[i] - l1.B[i]))));
            if (d > maxd) maxd = d; sum += d;
        }
        sb.AppendLine(string.Format("HANDOFF On f10 vs Loop f1: maxByteDiff={0} meanByteDiff={1:0.0000}", maxd, sum / (double)a10.A.Length));
        sb.AppendLine(string.Format("  step On f9 -> Loop f1 = {0:0.00000}; On f9 -> On f10 = {1:0.00000}; On f10 -> Loop f2 = {2:0.00000}",
            Step(on[8], loop[0], null), Step(on[8], on[9], null), Step(on[9], loop[1], null)));

        // ---- loop seam
        var steps = new List<double>(); var stepsOuter = new List<double>(); var stepsInner = new List<double>();
        Func<int, int, bool> outer = (x, y) => Sdf(x + 0.5, y + 0.5) > -6.5;
        Func<int, int, bool> inner = (x, y) => Sdf(x + 0.5, y + 0.5) <= -6.5;
        var sbs = new StringBuilder();
        for (int k = 0; k < loop.Count - 1; k++)
        {
            steps.Add(Step(loop[k], loop[k + 1], null));
            stepsOuter.Add(Step(loop[k], loop[k + 1], outer));
            stepsInner.Add(Step(loop[k], loop[k + 1], inner));
            sbs.Append(string.Format("{0:0.0000} ", steps[k]));
        }
        double wrap = Step(loop[loop.Count - 1], loop[0], null);
        double wrapO = Step(loop[loop.Count - 1], loop[0], outer);
        double wrapI = Step(loop[loop.Count - 1], loop[0], inner);
        sb.AppendLine("LOOP steps f1->f2 ... f29->f30: " + sbs);
        sb.AppendLine(string.Format("  wrap f30->f1 = {0:0.00000}", wrap));
        sb.AppendLine("  all steps: " + Stats(steps));
        double mean = 0; foreach (var x in steps) mean += x; mean /= steps.Count;
        var even = new List<double>(); var odd = new List<double>();
        for (int k = 0; k < steps.Count; k++) { if (k % 2 == 0) even.Add(steps[k]); else odd.Add(steps[k]); }
        sb.AppendLine("  steps from odd frame (f1->f2, f3->f4 ...; holds?): " + Stats(even));
        sb.AppendLine("  steps from even frame (f2->f3, f4->f5 ...; changes?): " + Stats(odd));
        double oddMean = 0; foreach (var x in odd) oddMean += x; oddMean /= odd.Count;
        sb.AppendLine(string.Format("  wrap/meanAll = {0:0.000}  wrap/meanSameParity = {1:0.000}", wrap / mean, wrap / oddMean));
        sb.AppendLine(string.Format("  OUTER region (rim+glow+glints) wrap={0:0.00000}  steps {1}", wrapO, Stats(stepsOuter)));
        sb.AppendLine(string.Format("  INNER region (fill+crackles) wrap={0:0.00000}  steps {1}", wrapI, Stats(stepsInner)));
        // Two-frame-back comparison: f29->f1 vs typical f(k)->f(k+2)
        var two = new List<double>();
        for (int k = 0; k < loop.Count; k++) two.Add(Step(loop[k], loop[(k + 2) % loop.Count], null));
        sb.AppendLine("  2-frame steps (incl wrap): " + Stats(two) + string.Format("  f29->f1={0:0.00000} f30->f2={1:0.00000}", two[28], two[29]));

        // ---- On clip per-frame beats
        sb.AppendLine("ON per-frame: centreAlpha(9x9 mean) | rimBand premulLum mean | glowBand(d 8..14) alpha mean | starZone(d>12 & near 193,63) max alpha | alphaSum");
        for (int k = 0; k < on.Count; k++) sb.AppendLine("  f" + (k + 1) + ": " + Beats(on[k]));
        sb.AppendLine("LOOP per-frame same:");
        for (int k = 0; k < loop.Count; k++) sb.AppendLine("  f" + (k + 1) + ": " + Beats(loop[k]));

        // ---- rim / glow / fill profile (loop, min and max over frames) along y=128 (right edge) and the diagonal
        sb.AppendLine("PROFILE along row y=128, x=128..255 (outline at x=216): x: minA/maxA straight RGB(f1)");
        for (int x = 128; x < 256; x += (x < 196 ? 8 : 1))
        {
            if (x > 250) break;
            int i = 128 * 256 + x; int mn = 255, mx = 0;
            foreach (var fr in loop) { mn = Math.Min(mn, fr.A[i]); mx = Math.Max(mx, fr.A[i]); }
            sb.AppendLine(string.Format("  x={0} d={1:0.0}: A {2}/{3}  rgb({4},{5},{6})", x, Sdf(x + 0.5, 128.5), mn, mx, loop[0].R[i], loop[0].G[i], loop[0].B[i]));
        }
        sb.AppendLine("PROFILE along diagonal to top-right (outline t~106): t: minA/maxA over loop");
        for (int t = 0; t < 180; t += (t < 90 ? 10 : 2))
        {
            int x = (int)Math.Floor(128 + t / Math.Sqrt(2)), y = (int)Math.Floor(128 - t / Math.Sqrt(2));
            if (x > 255 || y < 0) break;
            int i = y * 256 + x; int mn = 255, mx = 0;
            foreach (var fr in loop) { mn = Math.Min(mn, fr.A[i]); mx = Math.Max(mx, fr.A[i]); }
            sb.AppendLine(string.Format("  t={0} ({1},{2}) d={3:0.0}: A {4}/{5}", t, x, y, Sdf(x + 0.5, y + 0.5), mn, mx));
        }
        // Fill check: min over loop frames inside region d < -32 (away from crackles), max alpha
        int fillMaxAll = 0, fillCentreMax = 0;
        foreach (var fr in loop)
            for (int y = 0; y < 256; y++)
                for (int x = 0; x < 256; x++)
                {
                    int i = y * 256 + x;
                    double d = Sdf(x + 0.5, y + 0.5);
                    if (d < -32) fillMaxAll = Math.Max(fillMaxAll, fr.A[i]);
                    if ((x - 127.5) * (x - 127.5) + (y - 127.5) * (y - 127.5) < 100) fillCentreMax = Math.Max(fillCentreMax, fr.A[i]);
                }
        sb.AppendLine(string.Format("FILL loop: max alpha within r10 of centre = {0} ({1:0.000}); max alpha anywhere with d<-32 = {2} ({3:0.000})  [spec <=0.22 at centre]", fillCentreMax, fillCentreMax / 255.0, fillMaxAll, fillMaxAll / 255.0));

        // Rim breathing: premultiplied luminance in rim band per loop frame
        var rimLum = new List<double>(); var rimSat = new List<double>();
        foreach (var fr in loop)
        {
            double s = 0; int n = 0, sat = 0;
            for (int y = 0; y < 256; y++)
                for (int x = 0; x < 256; x++)
                {
                    double d = Sdf(x + 0.5, y + 0.5);
                    if (Math.Abs(d + 2.5) > 4) continue;
                    // exclude glint zones roughly: skip nothing, report
                    int i = y * 256 + x;
                    s += 0.2126 * fr.Pr[i] + 0.7152 * fr.Pg[i] + 0.0722 * fr.Pb[i]; n++;
                    if (fr.A[i] == 255) sat++;
                }
            rimLum.Add(s / n); rimSat.Add(sat / (double)n);
        }
        double rm = 0; foreach (var v in rimLum) rm += v; rm /= rimLum.Count;
        double rmin = double.MaxValue, rmax = 0; int imin = 0, imax = 0;
        for (int k = 0; k < rimLum.Count; k++) { if (rimLum[k] < rmin) { rmin = rimLum[k]; imin = k; } if (rimLum[k] > rmax) { rmax = rimLum[k]; imax = k; } }
        var rs = new StringBuilder(); foreach (var v in rimLum) rs.Append(string.Format("{0:0.000} ", v));
        sb.AppendLine("RIM band |d+2.5|<=4 premul luminance per loop frame: " + rs);
        sb.AppendLine(string.Format("  mean {0:0.000}, max {1:0.000} (f{2}) {3:+0.0}%, min {4:0.000} (f{5}) {6:+0.0;-0.0}%; fraction of band texels alpha=255 at f1 {7:0.00}, at peak {8:0.00}",
            rm, rmax, imax + 1, (rmax / rm - 1) * 100, rmin, imin + 1, (rmin / rm - 1) * 100, rimSat[0], rimSat[imax]));

        // Straight colour sanity: muddy/dark or cool texels
        ColourSanity("On", on, sb);
        ColourSanity("Loop", loop, sb);
        return sb.ToString();
    }

    static string Beats(RvFrame f)
    {
        double ca = 0; int cn = 0;
        for (int y = 124; y <= 132; y++) for (int x = 124; x <= 132; x++) { ca += f.Pa[y * 256 + x]; cn++; }
        double rl = 0; int rn = 0, gn = 0; double ga = 0, sz = 0; double asum = 0;
        for (int y = 0; y < 256; y++)
            for (int x = 0; x < 256; x++)
            {
                int i = y * 256 + x; double d = Sdf(x + 0.5, y + 0.5);
                asum += f.Pa[i];
                if (Math.Abs(d + 2.5) <= 3 && !(x > 150 && y < 106)) { rl += 0.2126 * f.Pr[i] + 0.7152 * f.Pg[i] + 0.0722 * f.Pb[i]; rn++; }
                if (d >= 8 && d <= 14 && !(x > 150 && y < 106) && !(x > 150 && y > 150) && !(x < 106 && y < 106)) { ga += f.Pa[i]; gn++; }
                if (d > 12 && (x - 193) * (x - 193) + (y - 63) * (y - 63) < 50 * 50) sz = Math.Max(sz, f.Pa[i]);
            }
        return string.Format("{0:0.000} | {1:0.000} | {2:0.000} | {3:0.000} | {4:0}", ca / cn, rl / rn, ga / gn, sz, asum);
    }

    static void Basic(string name, List<RvFrame> frames, StringBuilder sb)
    {
        sb.AppendLine("== " + name);
        int badSize = 0, border2 = 0, border3 = 0, rgbNoA = 0, faint = 0, premulBad = 0;
        int minX = 999, minY = 999, maxX = -1, maxY = -1;
        double maxCdrift = 0; int maxCf = 0;
        var cents = new StringBuilder();
        int worstComponents = 0; var smallComp = new StringBuilder();
        for (int k = 0; k < frames.Count; k++)
        {
            var f = frames[k];
            if (f.W != 256 || f.H != 256) badSize++;
            double sa = 0, sx = 0, sy = 0;
            int fb2 = 0, fb3 = 0;
            for (int y = 0; y < f.H; y++)
                for (int x = 0; x < f.W; x++)
                {
                    int i = y * f.W + x; int a = f.A[i];
                    int bd = Math.Min(Math.Min(x, y), Math.Min(f.W - 1 - x, f.H - 1 - y));
                    if (bd < 2) fb2 = Math.Max(fb2, a);
                    if (bd < 3) fb3 = Math.Max(fb3, a);
                    if (a == 0 && (f.R[i] | f.G[i] | f.B[i]) != 0) rgbNoA++;
                    if (a > 0 && a < 2) faint++;
                    if (f.Pr[i] > f.Pa[i] + 1e-9 || f.Pg[i] > f.Pa[i] + 1e-9 || f.Pb[i] > f.Pa[i] + 1e-9) premulBad++;
                    if (a > 0) { minX = Math.Min(minX, x); maxX = Math.Max(maxX, x); minY = Math.Min(minY, y); maxY = Math.Max(maxY, y); }
                    sa += f.Pa[i]; sx += f.Pa[i] * (x + 0.5); sy += f.Pa[i] * (y + 0.5);
                }
            border2 = Math.Max(border2, fb2); border3 = Math.Max(border3, fb3);
            double cx = sx / Math.Max(1e-9, sa), cy = sy / Math.Max(1e-9, sa);
            double dr = Math.Sqrt((cx - 128) * (cx - 128) + (cy - 128) * (cy - 128));
            if (dr > maxCdrift) { maxCdrift = dr; maxCf = k + 1; }
            cents.Append(string.Format("({0:0.00},{1:0.00}) ", cx - 128, cy - 128));
            // connected components of alpha>0 (8-connected)
            var lab = new int[f.W * f.H]; int nc = 0; var stack = new Stack<int>();
            for (int i0 = 0; i0 < lab.Length; i0++)
            {
                if (f.A[i0] == 0 || lab[i0] != 0) continue;
                nc++; int size = 0, maxa = 0; double ccx = 0, ccy = 0;
                stack.Push(i0); lab[i0] = nc;
                while (stack.Count > 0)
                {
                    int p = stack.Pop(); size++; maxa = Math.Max(maxa, f.A[p]);
                    int px = p % f.W, py = p / f.W; ccx += px; ccy += py;
                    for (int dy = -1; dy <= 1; dy++)
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int qx = px + dx, qy = py + dy;
                            if (qx < 0 || qy < 0 || qx >= f.W || qy >= f.H) continue;
                            int q = qy * f.W + qx;
                            if (f.A[q] != 0 && lab[q] == 0) { lab[q] = nc; stack.Push(q); }
                        }
                }
                if (size < 2000) smallComp.Append(string.Format("[f{0} comp size={1} maxA={2} at ({3:0},{4:0})] ", k + 1, size, maxa, ccx / size, ccy / size));
            }
            worstComponents = Math.Max(worstComponents, nc);
        }
        sb.AppendLine(string.Format("  badSize={0} maxAlphaOuter2px={1} maxAlphaOuter3px={2} transparentWithRGB={3} faint(0<a<2)={4} premulRGB>A={5}", badSize, border2, border3, rgbNoA, faint, premulBad));
        sb.AppendLine(string.Format("  union bbox x {0}..{1} y {2}..{3} (margins L{0} T{2} R{4} B{5})", minX, maxX, minY, maxY, 255 - maxX, 255 - maxY));
        sb.AppendLine(string.Format("  max alpha-centroid offset {0:0.00} px at f{1}", maxCdrift, maxCf));
        sb.AppendLine("  centroid offsets: " + cents);
        sb.AppendLine(string.Format("  max connected components (alpha>0) in a frame: {0}; small components: {1}", worstComponents, smallComp.Length == 0 ? "none" : smallComp.ToString()));
    }

    static void ColourSanity(string name, List<RvFrame> frames, StringBuilder sb)
    {
        int dark = 0, cool = 0, n = 0; int minMax = 255;
        for (int k = 0; k < frames.Count; k++)
        {
            var f = frames[k];
            for (int i = 0; i < f.A.Length; i++)
            {
                if (f.A[i] < 16) continue;
                n++;
                int mx = Math.Max(f.R[i], Math.Max(f.G[i], f.B[i]));
                if (mx < minMax) minMax = mx;
                if (mx < 217) dark++;
                if (f.B[i] > f.R[i] + 4) cool++;
            }
        }
        sb.AppendLine(string.Format("COLOUR {0}: texels a>=16: {1}; min straight max-channel {2}; texels with max-channel < 0.85: {3}; cool (B>R): {4}", name, n, minMax, dark, cool));
    }

    // Mock pieces for readability review: rounded-square gem with vertical gradient, highlight and dark outline.
    public static void WritePiece(string path, int r, int g, int b)
    {
        using (var bmp = new Bitmap(256, 256, PixelFormat.Format32bppArgb))
        {
            for (int y = 0; y < 256; y++)
                for (int x = 0; x < 256; x++)
                {
                    double acc = 0;
                    for (int j = 0; j < 3; j++) for (int i = 0; i < 3; i++)
                        {
                            double d = Sdf(x + (i + 0.5) / 3, y + (j + 0.5) / 3) + 6; // piece art slightly smaller than footprint
                            acc += d < 0 ? 1 : 0;
                        }
                    acc /= 9;
                    if (acc <= 0) { bmp.SetPixel(x, y, Color.FromArgb(0, 0, 0, 0)); continue; }
                    double dd = Sdf(x + 0.5, y + 0.5) + 6;
                    double shade = 0.62 + 0.55 * (1 - (y - 46) / 164.0);
                    if (dd > -6) shade *= 0.55;           // dark outline
                    double hx = (x - 100) / 38.0, hy = (y - 80) / 22.0; // specular
                    double spec = Math.Exp(-(hx * hx + hy * hy) * 1.2) * 0.55;
                    // a simple symbol (dark diamond) so readability can be judged
                    double sym = Math.Abs(x + 0.5 - 128) + Math.Abs(y + 0.5 - 140) < 34 ? 0.55 : 1.0;
                    int rr = Clamp((r * shade * sym) + 255 * spec), gg = Clamp((g * shade * sym) + 255 * spec), bb = Clamp((b * shade * sym) + 255 * spec);
                    bmp.SetPixel(x, y, Color.FromArgb(Clamp(acc * 255), rr, gg, bb));
                }
            bmp.Save(path, ImageFormat.Png);
        }
    }

    static int Clamp(double v) { int x = (int)Math.Round(v); return x < 0 ? 0 : x > 255 ? 255 : x; }
}

public static class ReviewColorBallTargetGlowExtra
{
    static double Sdf(double x, double y)
    {
        double px = Math.Abs(x - 128), py = Math.Abs(y - 128);
        double qx = px - 44, qy = py - 44;
        double ox = Math.Max(qx, 0), oy = Math.Max(qy, 0);
        return Math.Sqrt(ox * ox + oy * oy) + Math.Min(Math.Max(qx, qy), 0) - 44;
    }
    static double Lin(double c) { return c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4); }

    public static string Run(string root, string prefix)
    {
        var sb = new StringBuilder();
        var on = new List<RvFrame>(); var loop = new List<RvFrame>();
        for (int k = 1; k <= 10; k++) on.Add(RvFrame.Load(Path.Combine(root, string.Format("On/frames/{0}_{1:000}.png", prefix, k))));
        for (int k = 1; k <= 30; k++) loop.Add(RvFrame.Load(Path.Combine(root, string.Format("Loop/frames/{0}_{1:000}.png", prefix, k))));
        // 1. breathing: composited (sRGB, normal blend) relative luminance in rim band d in [-9,3] and glow band [3,20], excluding glint and crackle-prone zones
        double[][] bgs = { new double[] { 0x20 / 255.0, 0x24 / 255.0, 0x2e / 255.0 }, new double[] { 0x8a / 255.0, 0x93 / 255.0, 0xc8 / 255.0 } };
        string[] bgn = { "dark", "lavender" };
        for (int b = 0; b < 2; b++)
        {
            var rimL = new double[30]; var glowL = new double[30]; var coreL = new double[30];
            for (int f = 0; f < 30; f++)
            {
                double sr = 0, sg = 0, sc = 0; int nr = 0, ng = 0, nc = 0;
                var fr = loop[f];
                for (int y = 0; y < 256; y++)
                    for (int x = 0; x < 256; x++)
                    {
                        double d = Sdf(x + 0.5, y + 0.5);
                        if (d < -9 || d > 20) continue;
                        if ((x - 191) * (x - 191) + (y - 214) * (y - 214) < 30 * 30) continue;
                        if ((x - 39) * (x - 39) + (y - 74) * (y - 74) < 30 * 30) continue;
                        int i = y * 256 + x; double a = fr.Pa[i];
                        double r = fr.Pr[i] + bgs[b][0] * (1 - a), g = fr.Pg[i] + bgs[b][1] * (1 - a), bl = fr.Pb[i] + bgs[b][2] * (1 - a);
                        double L = 0.2126 * Lin(r) + 0.7152 * Lin(g) + 0.0722 * Lin(bl);
                        if (d >= -9 && d <= 3) { sr += L; nr++; }
                        if (d >= -5 && d <= 0) { sc += L; nc++; }
                        if (d > 3) { sg += L; ng++; }
                    }
                rimL[f] = sr / nr; glowL[f] = sg / ng; coreL[f] = sc / nc;
            }
            sb.AppendLine("BREATH over " + bgn[b] + " (linear luminance, glints masked): " + Amp("rimBand[-9,3]", rimL) + " | " + Amp("core[-5,0]", coreL) + " | " + Amp("glow(3,20]", glowL));
        }
        // 2. leakage of per-frame elements outside the outline (d in [2,26]) excluding glint zones: max over frames of A - minA
        int leak = 0; int lx = 0, ly = 0, lf = 0;
        for (int y = 0; y < 256; y++)
            for (int x = 0; x < 256; x++)
            {
                double d = Sdf(x + 0.5, y + 0.5);
                if (d < 2 || d > 26) continue;
                if ((x - 191) * (x - 191) + (y - 214) * (y - 214) < 22 * 22) continue;
                if ((x - 39) * (x - 39) + (y - 74) * (y - 74) < 22 * 22) continue;
                int i = y * 256 + x, mn = 255;
                foreach (var fr in loop) mn = Math.Min(mn, fr.A[i]);
                for (int f = 0; f < 30; f++) { int v = loop[f].A[i] - mn; if (v > leak) { leak = v; lx = x; ly = y; lf = f + 1; } }
            }
        sb.AppendLine(string.Format("LEAK outside outline (d 2..26, glints masked): max alpha above per-pixel loop minimum = {0} at ({1},{2}) f{3}", leak, lx, ly, lf));
        // 3. star arms at On f2..f5 along axes through (193,63)
        for (int f = 1; f <= 5; f++)
        {
            double deg = -6 + 4 * f, rad = deg * Math.PI / 180, cs = Math.Cos(rad), sn = Math.Sin(rad);
            var parts = new StringBuilder();
            string[] names = { "+u(right)", "-u(left)", "+v(down)", "-v(up)" };
            double[][] dirs = { new[] { cs, sn }, new[] { -cs, -sn }, new[] { -sn, cs }, new[] { sn, -cs } };
            for (int a = 0; a < 4; a++)
            {
                int far50 = 0, far20 = 0;
                for (int r = 0; r < 60; r++)
                {
                    int x = (int)Math.Floor(193 + dirs[a][0] * r), y = (int)Math.Floor(63 + dirs[a][1] * r);
                    if (x < 0 || y < 0 || x > 255 || y > 255) break;
                    int i = y * 256 + x;
                    double dA = on[f].Pa[i] - on[9].Pa[i];
                    double lum = 0.2126 * on[f].Pr[i] + 0.7152 * on[f].Pg[i] + 0.0722 * on[f].Pb[i];
                    if (on[f].A[i] >= 250 && lum > 0.97) far50 = r;
                    if (dA > 0.2) far20 = r;
                }
                parts.Append(string.Format("{0}: opaque-white to r={1}, alpha+0.2 over f10 to r={2}; ", names[a], far50, far20));
            }
            sb.AppendLine("STAR On f" + (f + 1) + " rot " + deg + ": " + parts);
        }
        return sb.ToString();
    }

    static string Amp(string name, double[] v)
    {
        double mn = double.MaxValue, mx = 0, m = 0; int imn = 0, imx = 0;
        for (int k = 0; k < v.Length; k++) { m += v[k]; if (v[k] < mn) { mn = v[k]; imn = k; } if (v[k] > mx) { mx = v[k]; imx = k; } }
        m /= v.Length;
        return string.Format("{0} mean {1:0.0000} max f{2} {3:+0.0}% min f{4} {5:+0.0;-0.0}%", name, m, imx + 1, (mx / m - 1) * 100, imn + 1, (mn / m - 1) * 100);
    }
}
