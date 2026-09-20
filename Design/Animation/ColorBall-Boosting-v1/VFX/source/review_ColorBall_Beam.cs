// Independent reviewer checks for ColorBall_Beam (reads saved PNGs only). C# 5, System.Drawing.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

public static class ReviewColorBallBeam
{
    public sealed class Fr
    {
        public int W, H;
        public byte[] R, G, B, A; // straight bytes
        public string Name;
    }

    public static Fr Load(string path)
    {
        using (var bmp = new Bitmap(path))
        {
            int w = bmp.Width, h = bmp.Height;
            var f = new Fr { W = w, H = h, R = new byte[w * h], G = new byte[w * h], B = new byte[w * h], A = new byte[w * h], Name = Path.GetFileName(path) };
            var data = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            var px = new byte[w * h * 4];
            for (int y = 0; y < h; y++) Marshal.Copy(IntPtr.Add(data.Scan0, y * data.Stride), px, y * w * 4, w * 4);
            bmp.UnlockBits(data);
            for (int i = 0; i < w * h; i++) { f.B[i] = px[i * 4]; f.G[i] = px[i * 4 + 1]; f.R[i] = px[i * 4 + 2]; f.A[i] = px[i * 4 + 3]; }
            return f;
        }
    }

    static List<Fr> LoadClip(string dir, StringBuilder sb, string prefix)
    {
        var files = new List<string>(Directory.GetFiles(dir, "*.png"));
        files.Sort(StringComparer.Ordinal);
        var list = new List<Fr>();
        bool namesOk = true;
        for (int i = 0; i < files.Count; i++)
        {
            string expect = string.Format("{0}_{1:000}.png", prefix, i + 1);
            if (Path.GetFileName(files[i]) != expect) namesOk = false;
            list.Add(Load(files[i]));
        }
        sb.AppendLine(string.Format("  files={0} names1basedSequential={1}", files.Count, namesOk));
        return list;
    }

    static double Lum(double r, double g, double b) { return 0.2126 * r + 0.7152 * g + 0.0722 * b; }

    // mean abs diff of premultiplied RGBA on a 0..255 scale
    public static double Diff(Fr a, Fr b)
    {
        double s = 0; int n = a.W * a.H;
        for (int i = 0; i < n; i++)
        {
            double aa = a.A[i] / 255.0, ba = b.A[i] / 255.0;
            s += Math.Abs(a.R[i] * aa - b.R[i] * ba) + Math.Abs(a.G[i] * aa - b.G[i] * ba) + Math.Abs(a.B[i] * aa - b.B[i] * ba) + Math.Abs(a.A[i] - b.A[i]);
        }
        return s / (n * 4.0);
    }

    // mean abs diff of composite luminance over a background, only over texels where either frame has alpha
    public static double DiffComp(Fr a, Fr b, int bg, out double maxPx)
    {
        double br = (bg >> 16) & 255, bgc = (bg >> 8) & 255, bb = bg & 255;
        double s = 0; int n = a.W * a.H; maxPx = 0;
        for (int i = 0; i < n; i++)
        {
            double aa = a.A[i] / 255.0, ba = b.A[i] / 255.0;
            double la = Lum(a.R[i] * aa + br * (1 - aa), a.G[i] * aa + bgc * (1 - aa), a.B[i] * aa + bb * (1 - aa));
            double lb = Lum(b.R[i] * ba + br * (1 - ba), b.G[i] * ba + bgc * (1 - ba), b.B[i] * ba + bb * (1 - ba));
            double d = Math.Abs(la - lb); s += d; if (d > maxPx) maxPx = d;
        }
        return s / n;
    }

    static int MaxA(Fr f) { int m = 0; for (int i = 0; i < f.A.Length; i++) if (f.A[i] > m) m = f.A[i]; return m; }

    static void Hygiene(Fr f, StringBuilder sb, ref int worstBorder, ref int dirty, ref int faint, ref int stray, ref int offHue, ref int grey, ref int border3, string tag)
    {
        int w = f.W, h = f.H;
        int bmax = 0, b3 = 0;
        // dilated mask of alpha >= 24 by 6 px (Chebyshev)
        var strong = new bool[w * h];
        for (int i = 0; i < w * h; i++) strong[i] = f.A[i] >= 24;
        var rowD = new bool[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                bool any = false;
                for (int dx = -6; dx <= 6 && !any; dx++) { int xx = x + dx; if (xx >= 0 && xx < w && strong[y * w + xx]) any = true; }
                rowD[y * w + x] = any;
            }
        int localStray = 0, localOff = 0, localGrey = 0; string offEx = "", greyEx = "";
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int i = y * w + x; int a = f.A[i];
                if ((x < 2 || y < 2 || x >= w - 2 || y >= h - 2) && a > bmax) bmax = a;
                if ((x < 3 || y < 3 || x >= w - 3 || y >= h - 3) && a > b3) b3 = a;
                if (a == 0 && (f.R[i] | f.G[i] | f.B[i]) != 0) dirty++;
                if (a == 1) faint++;
                if (a > 0)
                {
                    bool near = false;
                    for (int dy = -6; dy <= 6 && !near; dy++) { int yy = y + dy; if (yy >= 0 && yy < h && rowD[yy * w + x]) near = true; }
                    if (!near) localStray++;
                }
                if (a >= 8)
                {
                    // light family: red dominant, G >= B (white..gold). Off-hue = blue/green cast.
                    if (f.R[i] + 2 < f.G[i] || f.G[i] + 2 < f.B[i]) { localOff++; if (offEx.Length < 80) offEx += string.Format("({0},{1})={2},{3},{4}/{5} ", x, y, f.R[i], f.G[i], f.B[i], a); }
                    if (Math.Max(f.R[i], Math.Max(f.G[i], f.B[i])) < 240) { localGrey++; if (greyEx.Length < 80) greyEx += string.Format("({0},{1})={2},{3},{4}/{5} ", x, y, f.R[i], f.G[i], f.B[i], a); }
                }
            }
        stray += localStray; offHue += localOff; grey += localGrey;
        if (bmax > worstBorder) worstBorder = bmax;
        if (b3 > border3) border3 = b3;
        if (bmax > 0 || localStray > 0 || localOff > 0 || localGrey > 0)
            sb.AppendLine(string.Format("    {0}: border2={1} stray={2} offHue={3} {4} dimStraight={5} {6}", tag, bmax, localStray, localOff, offEx, localGrey, greyEx));
    }

    static void Centroid(Fr f, out double cx, out double cy, out double sum, out int x0, out int x1, out int y0, out int y1)
    {
        double s = 0, sx = 0, sy = 0; x0 = f.W; x1 = -1; y0 = f.H; y1 = -1;
        for (int y = 0; y < f.H; y++)
            for (int x = 0; x < f.W; x++)
            {
                int a = f.A[y * f.W + x]; if (a == 0) continue;
                s += a; sx += a * (x + 0.5); sy += a * (y + 0.5);
                if (x < x0) x0 = x; if (x > x1) x1 = x; if (y < y0) y0 = y; if (y > y1) y1 = y;
            }
        sum = s / 255.0; cx = s > 0 ? sx / s : 0; cy = s > 0 ? sy / s : 0;
    }

    // vertical extent at column x with alpha >= thr, and "white core" count (A>=250, B>=235)
    static string ColProfile(Fr f, int x)
    {
        int a10 = 0, a20 = 0, a50 = 0, core = 0, any = 0; int top = -1, bot = -1;
        double wsum = 0;
        for (int y = 0; y < f.H; y++)
        {
            int i = y * f.W + x; int a = f.A[i];
            if (a > 0) { any++; if (top < 0) top = y; bot = y; }
            if (a >= 26) a10++; if (a >= 51) a20++; if (a >= 128) a50++;
            if (a >= 250 && f.B[i] >= 235) core++;
            wsum += a / 255.0;
        }
        return string.Format("x={0}: core={1} a50%={2} a20%={3} a10%={4} any={5} ({6}..{7}) alphaSum={8:0.0}", x, core, a50, a20, a10, any, top, bot, wsum);
    }

    static string RowProfile(Fr f, int y)
    {
        int first = -1, last = -1, cf = -1, cl = -1, h50f = -1, h50l = -1;
        for (int x = 0; x < f.W; x++)
        {
            int i = y * f.W + x; int a = f.A[i];
            if (a > 0) { if (first < 0) first = x; last = x; }
            if (a >= 128) { if (h50f < 0) h50f = x; h50l = x; }
            if (a >= 250 && f.B[i] >= 235) { if (cf < 0) cf = x; cl = x; }
        }
        return string.Format("row y={0}: alpha>0 x {1}..{2}; a>=50% x {3}..{4}; whiteCore x {5}..{6}", y, first, last, h50f, h50l, cf, cl);
    }

    static double VSym(Fr f, double cy)
    {
        // mean abs alpha difference between row cy-0.5-k and cy-0.5+k+... mirrored about the line y=cy (cy integer boundary)
        int c = (int)Math.Round(cy); double s = 0; int n = 0;
        for (int k = 0; k < c && c + k < f.H; k++)
            for (int x = 0; x < f.W; x++) { s += Math.Abs(f.A[(c - 1 - k) * f.W + x] - f.A[(c + k) * f.W + x]); n++; }
        return s / n;
    }

    static double RegionAlpha(Fr f, int xa, int xb)
    {
        double s = 0;
        for (int y = 0; y < f.H; y++) for (int x = Math.Max(0, xa); x <= Math.Min(f.W - 1, xb); x++) s += f.A[y * f.W + x] / 255.0;
        return s;
    }

    static double MaxAlphaRegion(Fr f, int xa, int xb)
    {
        int m = 0;
        for (int y = 0; y < f.H; y++) for (int x = Math.Max(0, xa); x <= Math.Min(f.W - 1, xb); x++) m = Math.Max(m, f.A[y * f.W + x]);
        return m;
    }

    // min luminance change of composite vs background (negative = darkens board), plus count of darkened texels
    static void DarkCheck(Fr f, int bg, out double minDelta, out int darkCount, out double minSatDrop)
    {
        double br = (bg >> 16) & 255, bgc = (bg >> 8) & 255, bb = bg & 255, lb = Lum(br, bgc, bb);
        minDelta = 0; darkCount = 0; minSatDrop = 0;
        for (int i = 0; i < f.A.Length; i++)
        {
            if (f.A[i] == 0) continue;
            double a = f.A[i] / 255.0;
            double r = f.R[i] * a + br * (1 - a), g = f.G[i] * a + bgc * (1 - a), b = f.B[i] * a + bb * (1 - a);
            double d = Lum(r, g, b) - lb;
            if (d < minDelta) minDelta = d;
            if (d < -1.0) darkCount++;
        }
    }

    public static string Run(string root)
    {
        var sb = new StringBuilder();
        const int DARK = 0x20242e, LAV = 0x8a93c8;
        string prefix = "ColorBall_Beam";
        foreach (var clip in new[] { "Strip", "Head", "Sequence" })
        {
            sb.AppendLine("== " + clip);
            var frames = LoadClip(Path.Combine(root, clip, "frames"), sb, prefix);
            if (frames.Count == 0) continue;
            int ew = clip == "Strip" ? 512 : clip == "Head" ? 160 : 640, eh = clip == "Strip" ? 96 : clip == "Head" ? 64 : 160, en = clip == "Sequence" ? 34 : 1;
            bool sizeOk = true; foreach (var f in frames) if (f.W != ew || f.H != eh) sizeOk = false;
            sb.AppendLine(string.Format("  expected {0}x{1} x{2}: size ok={3}, count ok={4}", ew, eh, en, sizeOk, frames.Count == en));
            int worstBorder = 0, dirty = 0, faint = 0, stray = 0, off = 0, grey = 0, border3 = 0;
            for (int k = 0; k < frames.Count; k++) Hygiene(frames[k], sb, ref worstBorder, ref dirty, ref faint, ref stray, ref off, ref grey, ref border3, "f" + (k + 1));
            sb.AppendLine(string.Format("  border2 maxA={0} [{1}] border3 maxA={2}; alpha0-with-RGB={3}; alpha==1 texels={4}; stray(a>0, >6px from a>=24)={5}; offHue(a>=8)={6}; dimStraight(max<240,a>=8)={7}",
                worstBorder, worstBorder == 0 ? "PASS" : "FAIL", border3, dirty, faint, stray, off, grey));
            // premultiplied RGB > alpha, computed on premultiplied reconstruction (always valid for straight PNG) - reported for completeness
            // darkening vs board
            double worstDarkD = 0, worstDarkL = 0; int dcD = 0, dcL = 0;
            foreach (var f in frames)
            {
                double md, sd; int dc;
                DarkCheck(f, DARK, out md, out dc, out sd); if (md < worstDarkD) worstDarkD = md; dcD += dc;
                DarkCheck(f, LAV, out md, out dc, out sd); if (md < worstDarkL) worstDarkL = md; dcL += dc;
            }
            sb.AppendLine(string.Format("  composite luminance vs board: dark min delta={0:0.00} (texels darker>1: {1}); lavender min delta={2:0.00} (texels darker>1: {3})", worstDarkD, dcD, worstDarkL, dcL));
            // centroid / bbox per frame
            var cen = new StringBuilder();
            for (int k = 0; k < frames.Count; k++)
            {
                double cx, cy, s; int x0, x1, y0, y1; Centroid(frames[k], out cx, out cy, out s, out x0, out x1, out y0, out y1);
                cen.AppendLine(string.Format("    f{0}: centroid=({1:0.00},{2:0.00}) alphaSum={3:0} bbox x{4}..{5} y{6}..{7} maxA={8} vSym={9:0.000}", k + 1, cx, cy, s, x0, x1, y0, y1, MaxA(frames[k]), VSym(frames[k], eh / 2.0)));
            }
            sb.Append(cen.ToString());

            if (clip == "Strip")
            {
                var f = frames[0];
                sb.AppendLine("  " + RowProfile(f, 48)); sb.AppendLine("  " + RowProfile(f, 47));
                foreach (int x in new[] { 6, 12, 14, 20, 40, 100, 256, 400, 470, 490, 496, 499, 502, 505 }) sb.AppendLine("  " + ColProfile(f, x));
                // along-X smoothness of column alpha sums between x=30 and x=480
                double maxD2 = 0; int at = 0;
                double[] cs = new double[f.W];
                for (int x = 0; x < f.W; x++) { double s = 0; for (int y = 0; y < f.H; y++) s += f.A[y * f.W + x] / 255.0; cs[x] = s; }
                for (int x = 31; x < 480; x++) { double d2 = Math.Abs(cs[x + 1] - 2 * cs[x] + cs[x - 1]); if (d2 > maxD2) { maxD2 = d2; at = x; } }
                sb.AppendLine(string.Format("  column alphaSum x30={0:0.00} x255={1:0.00} x480={2:0.00}; max |2nd diff| 31..479 = {3:0.0000} at x={4}", cs[30], cs[255], cs[480], maxD2, at));
                // column centre-row colour
                foreach (int x in new[] { 20, 256, 490 })
                {
                    var line = new StringBuilder();
                    for (int y = 48; y < 48 + 22; y++) { int i = y * f.W + x; line.Append(string.Format("{0}:{1},{2},{3}/{4} ", y - 48, f.R[i], f.G[i], f.B[i], f.A[i])); }
                    sb.AppendLine(string.Format("  x={0} below centre: {1}", x, line));
                }
            }
            if (clip == "Head")
            {
                var f = frames[0];
                sb.AppendLine("  " + RowProfile(f, 32)); sb.AppendLine("  " + RowProfile(f, 31));
                int bestCore = 0, bestX = 0, best50 = 0, best50X = 0;
                for (int x = 0; x < f.W; x++)
                {
                    int core = 0, a50 = 0;
                    for (int y = 0; y < f.H; y++) { int i = y * f.W + x; if (f.A[i] >= 250 && f.B[i] >= 235) core++; if (f.A[i] >= 128) a50++; }
                    if (core > bestCore) { bestCore = core; bestX = x; }
                    if (a50 > best50) { best50 = a50; best50X = x; }
                }
                sb.AppendLine(string.Format("  max white core thickness={0} at x={1}; max a>=50% thickness={2} at x={3}", bestCore, bestX, best50, best50X));
                foreach (int x in new[] { 30, 40, 50, 57, 70, 80, 90, 100, 111, 120, 128, 131, 134, 138, 142, 146, 150 }) sb.AppendLine("  " + ColProfile(f, x));
                var line = new StringBuilder();
                for (int x = 20; x < 156; x += 4) { int i = 32 * f.W + x; line.Append(string.Format("{0}:{1}/{2} ", x, f.A[i], f.B[i])); }
                sb.AppendLine("  row32 alpha/blue: " + line);
            }
            if (clip == "Sequence")
            {
                sb.AppendLine(string.Format("  last frame f{0} maxA={1} [{2}]", frames.Count, MaxA(frames[frames.Count - 1]), MaxA(frames[frames.Count - 1]) == 0 ? "PASS" : "FAIL"));
                var steps = new StringBuilder(); var stepsL = new StringBuilder(); var stepsD = new StringBuilder();
                double[] st = new double[frames.Count];
                for (int k = 0; k + 1 < frames.Count; k++)
                {
                    st[k] = Diff(frames[k], frames[k + 1]);
                    double mx; double dl = DiffComp(frames[k], frames[k + 1], LAV, out mx); double mxd; double dd = DiffComp(frames[k], frames[k + 1], DARK, out mxd);
                    steps.Append(string.Format("{0}->{1}:{2:0.00} ", k + 1, k + 2, st[k]));
                    stepsL.Append(string.Format("{0}->{1}:{2:0.00}/{3:0} ", k + 1, k + 2, dl, mx));
                    stepsD.Append(string.Format("{0}->{1}:{2:0.00}/{3:0} ", k + 1, k + 2, dd, mxd));
                }
                sb.AppendLine("  steps premul: " + steps);
                sb.AppendLine("  steps lavender lum mean/max: " + stepsL);
                sb.AppendLine("  steps dark lum mean/max: " + stepsD);
                int L0 = 13, L1 = 26;
                double sum = 0, mxs = 0, mn = 1e9; int n = 0; var inner = new List<double>();
                for (int k = L0; k < L1; k++) { double d = Diff(frames[k - 1], frames[k]); inner.Add(d); sum += d; n++; mxs = Math.Max(mxs, d); mn = Math.Min(mn, d); }
                inner.Sort(); double median = inner[inner.Count / 2];
                double wrap = Diff(frames[L1 - 1], frames[L0 - 1]);
                double evenSum = 0, oddSum = 0; int ne = 0, no = 0;
                for (int k = L0; k < L1; k++) { double d = Diff(frames[k - 1], frames[k]); if ((k - L0) % 2 == 0) { evenSum += d; ne++; } else { oddSum += d; no++; } }
                sb.AppendLine(string.Format("  LOOP f{0}-f{1}: wrap f{1}->f{0}={2:0.000}; mean step={3:0.000} median={4:0.000} min={5:0.000} max={6:0.000}; wrap/mean={7:0.00} wrap/median={8:0.00} wrap/max={9:0.00} [{10}]",
                    L0, L1, wrap, sum / n, median, mn, mxs, wrap / (sum / n), wrap / median, wrap / mxs, wrap / (sum / n) <= 1.5 ? "PASS" : "FAIL"));
                sb.AppendLine(string.Format("  within-seed steps (f13->14, f15->16..) mean={0:0.000}; re-seed steps (f14->15..) mean={1:0.000}; wrap vs re-seed mean={2:0.00}", evenSum / ne, oddSum / no, wrap / (oddSum / no)));
                double mxw; double wl = DiffComp(frames[L1 - 1], frames[L0 - 1], LAV, out mxw); double mxwd; double wd = DiffComp(frames[L1 - 1], frames[L0 - 1], DARK, out mxwd);
                double sL = 0, sD = 0, mxL = 0, mxD = 0;
                for (int k = L0; k < L1; k++) { double m1, m2; sL += DiffComp(frames[k - 1], frames[k], LAV, out m1); sD += DiffComp(frames[k - 1], frames[k], DARK, out m2); mxL = Math.Max(mxL, m1); mxD = Math.Max(mxD, m2); }
                sb.AppendLine(string.Format("  LOOP composite lum: lavender wrap={0:0.000} (max px {1:0}) vs mean {2:0.000} (max px {3:0}) ratio {4:0.00}; dark wrap={5:0.000} (max px {6:0}) vs mean {7:0.000} (max px {8:0}) ratio {9:0.00}",
                    wl, mxw, sL / n, mxL, wl / (sL / n), wd, mxwd, sD / n, mxD, wd / (sD / n)));
                // f13 vs f27 closeness (f27 is first fade frame)
                sb.AppendLine(string.Format("  entry f12->f13={0:0.000}; exit f26->f27={1:0.000}; f13 vs f27={2:0.000}", Diff(frames[11], frames[12]), Diff(frames[25], frames[26]), Diff(frames[12], frames[26])));
                // travel tip
                var tip = new StringBuilder();
                for (int k = 0; k < 12; k++)
                {
                    var f = frames[k]; int last50 = -1, lastAny = -1, lastCore = -1;
                    for (int x = 0; x < f.W; x++) { int i = 80 * f.W + x; if (f.A[i] >= 128) last50 = x; if (f.A[i] > 0) lastAny = x; if (f.A[i] >= 250 && f.B[i] >= 235) lastCore = x; }
                    tip.Append(string.Format("f{0}: core->{1} a50->{2} any->{3}; ", k + 1, lastCore, last50, lastAny));
                }
                sb.AppendLine("  travel tip row80: " + tip);
                // line thickness at x=100 and x=300 across frames
                foreach (int k in new[] { 3, 5, 8, 10, 11, 12, 13, 16, 20, 24, 26, 27, 28, 29, 30, 31, 32, 33 })
                    sb.AppendLine(string.Format("  f{0} {1} | {2} | {3} | {4}", k, ColProfile(frames[k - 1], 40), ColProfile(frames[k - 1], 150), ColProfile(frames[k - 1], 300), ColProfile(frames[k - 1], 500)));
                foreach (int k in new[] { 10, 11, 12, 13, 17, 20, 26, 27, 29, 31, 33 })
                    sb.AppendLine(string.Format("  f{0} flare: {1} | {2} | {3}", k, ColProfile(frames[k - 1], 560), RowProfile(frames[k - 1], 80), ColProfile(frames[k - 1], 590)));
                // fade: source half vs target half alpha sums
                var fade = new StringBuilder();
                for (int k = 24; k <= 34; k++)
                    fade.Append(string.Format("f{0}: src(16..150)={1:0} mid(150..420)={2:0} tgt(420..640)={3:0} srcMax={4} ; ", k, RegionAlpha(frames[k - 1], 16, 150), RegionAlpha(frames[k - 1], 150, 420), RegionAlpha(frames[k - 1], 420, 639), MaxAlphaRegion(frames[k - 1], 0, 150)));
                sb.AppendLine("  fade: " + fade);
            }
        }
        return sb.ToString();
    }

    // -------------------------------------------------------------- composition previews (review only)
    static void Blit(double[] cr, double[] cg, double[] cb, int W, int H, Fr src, double pivX, double pivY, double sx, double sy, double deg, double tx, double ty, int ss)
    {
        double rad = deg * Math.PI / 180, cs = Math.Cos(rad), sn = Math.Sin(rad);
        double r = Math.Sqrt(src.W * src.W * sx * sx + src.H * src.H * sy * sy) + 4;
        int xa = Math.Max(0, (int)(tx - r)), xb = Math.Min(W - 1, (int)(tx + r)), ya = Math.Max(0, (int)(ty - r)), yb = Math.Min(H - 1, (int)(ty + r));
        for (int y = ya; y <= yb; y++)
            for (int x = xa; x <= xb; x++)
            {
                double pr = 0, pg = 0, pb = 0, pa = 0;
                for (int j = 0; j < ss; j++)
                    for (int i = 0; i < ss; i++)
                    {
                        double X = x + (i + 0.5) / ss - tx, Y = y + (j + 0.5) / ss - ty;
                        double lx = (X * cs + Y * sn) / sx + pivX, ly = (-X * sn + Y * cs) / sy + pivY;
                        // bilinear on premultiplied
                        double fx = lx - 0.5, fy = ly - 0.5; int x0 = (int)Math.Floor(fx), y0 = (int)Math.Floor(fy); double ux = fx - x0, uy = fy - y0;
                        for (int q = 0; q < 2; q++)
                            for (int p = 0; p < 2; p++)
                            {
                                int xi = x0 + p, yi = y0 + q; if (xi < 0 || yi < 0 || xi >= src.W || yi >= src.H) continue;
                                double wgt = (p == 0 ? 1 - ux : ux) * (q == 0 ? 1 - uy : uy); int k = yi * src.W + xi; double a = src.A[k] / 255.0;
                                pr += src.R[k] / 255.0 * a * wgt; pg += src.G[k] / 255.0 * a * wgt; pb += src.B[k] / 255.0 * a * wgt; pa += a * wgt;
                            }
                    }
                double inv = 1.0 / (ss * ss); pr *= inv; pg *= inv; pb *= inv; pa *= inv;
                if (pa <= 0) continue;
                int o = y * W + x;
                cr[o] = pr + cr[o] * (1 - pa); cg[o] = pg + cg[o] * (1 - pa); cb[o] = pb + cb[o] * (1 - pa);
            }
    }

    static void SaveRgb(double[] cr, double[] cg, double[] cb, int W, int H, string path)
    {
        using (var bmp = new Bitmap(W, H, PixelFormat.Format24bppRgb))
        {
            var data = bmp.LockBits(new Rectangle(0, 0, W, H), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
            var row = new byte[data.Stride];
            for (int y = 0; y < H; y++)
            {
                for (int x = 0; x < W; x++)
                {
                    int o = y * W + x;
                    row[x * 3] = (byte)Math.Max(0, Math.Min(255, Math.Round(cb[o] * 255)));
                    row[x * 3 + 1] = (byte)Math.Max(0, Math.Min(255, Math.Round(cg[o] * 255)));
                    row[x * 3 + 2] = (byte)Math.Max(0, Math.Min(255, Math.Round(cr[o] * 255)));
                }
                Marshal.Copy(row, 0, IntPtr.Add(data.Scan0, y * data.Stride), data.Stride);
            }
            bmp.UnlockBits(data);
            bmp.Save(path, ImageFormat.Png);
        }
    }

    static void Fill(double[] cr, double[] cg, double[] cb, int bg)
    {
        for (int i = 0; i < cr.Length; i++) { cr[i] = ((bg >> 16) & 255) / 255.0; cg[i] = ((bg >> 8) & 255) / 255.0; cb[i] = (bg & 255) / 255.0; }
    }

    static double LumAt(double[] cr, double[] cg, double[] cb, int i) { return 255 * Lum(cr[i], cg[i], cb[i]); }

    public static string Compose(string root, string outDir)
    {
        var sb = new StringBuilder();
        var strip = Load(Path.Combine(root, "Strip", "frames", "ColorBall_Beam_001.png"));
        var head = Load(Path.Combine(root, "Head", "frames", "ColorBall_Beam_001.png"));
        var seq20 = Load(Path.Combine(root, "Sequence", "frames", "ColorBall_Beam_020.png"));
        var seq6 = Load(Path.Combine(root, "Sequence", "frames", "ColorBall_Beam_006.png"));
        var seq11 = Load(Path.Combine(root, "Sequence", "frames", "ColorBall_Beam_011.png"));
        foreach (var bg in new[] { 0x20242e, 0x8a93c8 })
        {
            string tag = bg == 0x20242e ? "dark" : "lavender";
            // 1) Strip + Head composition (Option B) at 3 travel lengths, 1:1, plus 3x zoom crop around the join
            int W = 700, H = 300;
            var cr = new double[W * H]; var cg = new double[W * H]; var cb = new double[W * H];
            Fill(cr, cg, cb, bg);
            double[] lens = { 200, 380, 600 };
            for (int k = 0; k < 3; k++)
            {
                double yy = 50 + k * 100, len = lens[k];
                Blit(cr, cg, cb, W, H, strip, 12, 48, len / 488.0, 0.7, 0, 40, yy, 2);
                Blit(cr, cg, cb, W, H, head, 134, 32, 1, 1, 0, 40 + len, yy, 2);
            }
            SaveRgb(cr, cg, cb, W, H, Path.Combine(outDir, "review_option_b_" + tag + ".png"));
            // centre-row luminance along the join for the 380 px beam
            var line = new StringBuilder();
            int ry = 150;
            for (int x = 300; x < 440; x += 3) line.Append(string.Format("{0}:{1:0} ", x, LumAt(cr, cg, cb, ry * W + x)));
            sb.AppendLine("  optionB " + tag + " row150 lum x300..440: " + line);
            // thickness (lum > bg+40) per column
            double lbg = 255 * Lum(((bg >> 16) & 255) / 255.0, ((bg >> 8) & 255) / 255.0, (bg & 255) / 255.0);
            var th = new StringBuilder();
            for (int x = 280; x < 440; x += 5)
            {
                int c = 0; for (int y = 100; y < 200; y++) if (LumAt(cr, cg, cb, y * W + x) > lbg + 40) c++;
                th.Append(string.Format("{0}:{1} ", x, c));
            }
            sb.AppendLine("  optionB " + tag + " thickness(lum>bg+40) x280..440: " + th);

            // 2) Stacking: 4 sequence hold beams from one source fanning out + a crossing pair
            W = 900; H = 700;
            cr = new double[W * H]; cg = new double[W * H]; cb = new double[W * H];
            Fill(cr, cg, cb, bg);
            double sx = 120, sy = 350;
            double[][] tg = { new[] { 760.0, 330.0 }, new[] { 740.0, 400.0 }, new[] { 520.0, 80.0 }, new[] { 420.0, 640.0 } };
            foreach (var t in tg)
            {
                double dx = t[0] - sx, dy = t[1] - sy, dist = Math.Sqrt(dx * dx + dy * dy), deg = Math.Atan2(dy, dx) * 180 / Math.PI;
                Blit(cr, cg, cb, W, H, seq20, 16, 80, dist / 544.0, 1, deg, sx, sy, 2);
            }
            // crossing beam from a second ball
            {
                double bx = 820, by = 120, tx = 300, ty = 600, dx = tx - bx, dy = ty - by, dist = Math.Sqrt(dx * dx + dy * dy), deg = Math.Atan2(dy, dx) * 180 / Math.PI;
                Blit(cr, cg, cb, W, H, seq20, 16, 80, dist / 544.0, 1, deg, bx, by, 2);
            }
            SaveRgb(cr, cg, cb, W, H, Path.Combine(outDir, "review_stack_" + tag + ".png"));

            // 3) Scale test: Sequence f6, f11, f20 at X scale 0.55, 1.0, 1.8
            W = 1100; H = 3 * 3 * 170;
            cr = new double[W * H]; cg = new double[W * H]; cb = new double[W * H];
            Fill(cr, cg, cb, bg);
            Fr[] fs = { seq6, seq11, seq20 }; double[] scl = { 0.55, 1.0, 1.8 };
            for (int a = 0; a < 3; a++)
                for (int b = 0; b < 3; b++)
                    Blit(cr, cg, cb, W, H, fs[a], 16, 80, scl[b], 1, 0, 20, 85 + (a * 3 + b) * 170, 2);
            SaveRgb(cr, cg, cb, W, H, Path.Combine(outDir, "review_scale_" + tag + ".png"));
        }
        return sb.ToString();
    }
}
