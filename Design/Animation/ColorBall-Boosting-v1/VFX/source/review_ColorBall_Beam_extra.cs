// Extra reviewer probes for ColorBall_Beam: grey-risk texels, flare radial profile, contact-star jitter.
using System;
using System.IO;
using System.Text;

public static class ReviewColorBallBeamExtra
{
    static double Lum(double r, double g, double b) { return 0.2126 * r + 0.7152 * g + 0.0722 * b; }

    public static string Run(string root)
    {
        var sb = new StringBuilder();
        string dir = Path.Combine(root, "Sequence", "frames");
        var fr = new ReviewColorBallBeam.Fr[34];
        for (int k = 0; k < 34; k++) fr[k] = ReviewColorBallBeam.Load(Path.Combine(dir, string.Format("ColorBall_Beam_{0:000}.png", k + 1)));
        const double br = 32, bgc = 36, bb = 46; double lbg = Lum(br, bgc, bb);
        sb.AppendLine("GREY-RISK on dark board: composite chroma<14 and bg+15 < lum < 170 (visible neutral grey), by region src(x<200) mid(200..519) tgt(x>=520); plus max lum of such texels");
        for (int k = 0; k < 34; k++)
        {
            var f = fr[k]; int s = 0, m = 0, t = 0; double mx = 0; int khaki = 0;
            for (int y = 0; y < f.H; y++)
                for (int x = 0; x < f.W; x++)
                {
                    int i = y * f.W + x; if (f.A[i] == 0) continue; double a = f.A[i] / 255.0;
                    double r = f.R[i] * a + br * (1 - a), g = f.G[i] * a + bgc * (1 - a), b = f.B[i] * a + bb * (1 - a);
                    double chroma = Math.Max(r, Math.Max(g, b)) - Math.Min(r, Math.Min(g, b)); double l = Lum(r, g, b);
                    if (chroma < 14 && l > lbg + 15 && l < 170) { if (x < 200) s++; else if (x < 520) m++; else t++; if (l > mx) mx = l; }
                    if (chroma >= 14 && chroma < 40 && l > lbg + 15 && l < 130) khaki++;
                }
            sb.AppendLine(string.Format("  f{0}: grey src={1} mid={2} tgt={3} maxLum={4:0}; dull khaki/brown(chroma14..40, lum<130)={5}", k + 1, s, m, t, mx, khaki));
        }
        sb.AppendLine("STRAY: alpha>0 texels farther than 10 px (Chebyshev) from any texel with alpha >= max(8, 10% of frame max)");
        for (int k = 0; k < 34; k++)
        {
            var f = fr[k]; int mxA = 0; for (int i = 0; i < f.A.Length; i++) if (f.A[i] > mxA) mxA = f.A[i];
            if (mxA == 0) { sb.AppendLine(string.Format("  f{0}: empty", k + 1)); continue; }
            int thr = Math.Max(8, mxA / 10); int cnt = 0; int minA = 255;
            for (int y = 0; y < f.H; y++)
                for (int x = 0; x < f.W; x++)
                {
                    int a = f.A[y * f.W + x]; if (a == 0) continue; if (a < minA) minA = a;
                    bool near = false;
                    for (int dy = -10; dy <= 10 && !near; dy++) { int yy = y + dy; if (yy < 0 || yy >= f.H) continue; for (int dx = -10; dx <= 10; dx++) { int xx = x + dx; if (xx >= 0 && xx < f.W && f.A[yy * f.W + xx] >= thr) { near = true; break; } } }
                    if (!near) cnt++;
                }
            sb.AppendLine(string.Format("  f{0}: maxA={1} thr={2} stray={3} minNonZeroA={4}", k + 1, mxA, thr, cnt, minA));
        }
        // sample straight colours along the beam centre and 45-degree diagonal of the flare
        foreach (int k in new[] { 10, 11, 12, 20, 29, 30, 31, 32, 33 })
        {
            var f = fr[k - 1]; var line = new StringBuilder();
            for (int d = 0; d <= 40; d += 4) { int x = 560 + (int)(d * 0.7071), y = 80 - (int)(d * 0.7071); int i = y * f.W + x; line.Append(string.Format("r{0}:{1},{2},{3}/{4} ", d, f.R[i], f.G[i], f.B[i], f.A[i])); }
            sb.AppendLine(string.Format("  f{0} flare diagonal: {1}", k, line));
            var l2 = new StringBuilder();
            foreach (int x in new[] { 30, 80, 150, 250, 350, 450, 530 }) { int i = 80 * f.W + x; l2.Append(string.Format("x{0}:{1},{2},{3}/{4} ", x, f.R[i], f.G[i], f.B[i], f.A[i])); }
            sb.AppendLine(string.Format("  f{0} centre row: {1}", k, l2));
            if (k == 10) { var l3 = new StringBuilder(); for (int x = 556; x <= 582; x += 2) { int i = 80 * f.W + x; l3.Append(string.Format("x{0}:{1},{2},{3}/{4} ", x, f.R[i], f.G[i], f.B[i], f.A[i])); } sb.AppendLine("  f10 tip row: " + l3); }
        }
        // contact star jitter: alpha-weighted x centroid of the upper ray rows y 50..58 and lower ray y 102..110 within x 530..600
        sb.AppendLine("CONTACT STAR ray tip x-centroids (upper y50..58 / lower y102..110), and upper-ray top row reached (alpha>=40):");
        for (int k = 11; k <= 33; k++)
        {
            var f = fr[k - 1];
            double su = 0, sxu = 0, sl = 0, sxl = 0; int top = -1;
            for (int y = 30; y <= 110; y++)
                for (int x = 530; x <= 600; x++)
                {
                    int a = f.A[y * f.W + x];
                    if (y >= 50 && y <= 58) { su += a; sxu += a * (x + 0.5); }
                    if (y >= 102 && y <= 110) { sl += a; sxl += a * (x + 0.5); }
                    if (a >= 40 && top < 0 && y < 70) top = y;
                }
            sb.AppendLine(string.Format("  f{0}: upper x={1:0.00} (mass {2:0}) lower x={3:0.00} (mass {4:0}) top={5}", k, su > 0 ? sxu / su : 0, su / 255, sl > 0 ? sxl / sl : 0, sl / 255, top));
        }
        return sb.ToString();
    }
}
