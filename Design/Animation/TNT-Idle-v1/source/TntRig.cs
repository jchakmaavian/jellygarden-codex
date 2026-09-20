// TNT Reveal rig renderer. Layers are cut from the original TNT artwork so the rest pose
// reproduces the source exactly; motion comes from transforms, never from redrawing the design.
// C# 5 (compiled by Windows PowerShell Add-Type).
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

public sealed class Img
{
    public int W, H;
    public float[] R, G, B, A; // premultiplied, 0..1

    public Img(int w, int h) { W = w; H = h; R = new float[w * h]; G = new float[w * h]; B = new float[w * h]; A = new float[w * h]; }

    public static Img Load(string path)
    {
        using (var bmp = new Bitmap(path))
        {
            int w = bmp.Width, h = bmp.Height;
            var img = new Img(w, h);
            var data = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            var px = new byte[w * h * 4];
            Marshal.Copy(data.Scan0, px, 0, px.Length);
            bmp.UnlockBits(data);
            for (int i = 0; i < w * h; i++)
            {
                float a = px[i * 4 + 3] / 255f;
                img.A[i] = a;
                img.R[i] = px[i * 4 + 2] / 255f * a;
                img.G[i] = px[i * 4 + 1] / 255f * a;
                img.B[i] = px[i * 4] / 255f * a;
            }
            return img;
        }
    }

    public void Save(string path)
    {
        using (var bmp = new Bitmap(W, H, PixelFormat.Format32bppArgb))
        {
            var data = bmp.LockBits(new Rectangle(0, 0, W, H), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            var px = new byte[W * H * 4];
            for (int i = 0; i < W * H; i++)
            {
                float a = Math.Max(0f, Math.Min(1f, A[i]));
                float r = 0, g = 0, b = 0;
                if (a > 1e-5f) { r = R[i] / a; g = G[i] / a; b = B[i] / a; }
                px[i * 4] = ToByte(b); px[i * 4 + 1] = ToByte(g); px[i * 4 + 2] = ToByte(r); px[i * 4 + 3] = ToByte(a);
            }
            Marshal.Copy(px, 0, data.Scan0, px.Length);
            bmp.UnlockBits(data);
            bmp.Save(path, ImageFormat.Png);
        }
    }

    static byte ToByte(float v) { int x = (int)Math.Round(v * 255f); return (byte)(x < 0 ? 0 : x > 255 ? 255 : x); }

    public void Sample(double x, double y, out float r, out float g, out float b, out float a)
    {
        // Pixel centres at integer + 0.5.
        x -= 0.5; y -= 0.5;
        int x0 = (int)Math.Floor(x), y0 = (int)Math.Floor(y);
        float fx = (float)(x - x0), fy = (float)(y - y0);
        r = g = b = a = 0;
        for (int j = 0; j < 2; j++)
            for (int i = 0; i < 2; i++)
            {
                int xi = x0 + i, yi = y0 + j;
                if (xi < 0 || yi < 0 || xi >= W || yi >= H) continue;
                float wgt = (i == 0 ? 1 - fx : fx) * (j == 0 ? 1 - fy : fy);
                int k = yi * W + xi;
                r += R[k] * wgt; g += G[k] * wgt; b += B[k] * wgt; a += A[k] * wgt;
            }
    }
}

public static class Curve
{
    // Monotone cubic (Fritsch-Carlson) through keys: no overshoot between keys.
    public static double Eval(double[] keys, double t)
    {
        int n = keys.Length / 2;
        if (t <= keys[0]) return keys[1];
        if (t >= keys[(n - 1) * 2]) return keys[(n - 1) * 2 + 1];
        var xs = new double[n]; var ys = new double[n];
        for (int i = 0; i < n; i++) { xs[i] = keys[i * 2]; ys[i] = keys[i * 2 + 1]; }
        var d = new double[n - 1]; var m = new double[n];
        for (int i = 0; i < n - 1; i++) d[i] = (ys[i + 1] - ys[i]) / (xs[i + 1] - xs[i]);
        m[0] = d[0]; m[n - 1] = d[n - 2];
        for (int i = 1; i < n - 1; i++) m[i] = (d[i - 1] * d[i] <= 0) ? 0 : (d[i - 1] + d[i]) / 2;
        for (int i = 0; i < n - 1; i++)
        {
            if (d[i] == 0) { m[i] = 0; m[i + 1] = 0; continue; }
            double a = m[i] / d[i], b = m[i + 1] / d[i], s = a * a + b * b;
            if (s > 9) { double tau = 3 / Math.Sqrt(s); m[i] = tau * a * d[i]; m[i + 1] = tau * b * d[i]; }
        }
        int k = 0;
        while (k < n - 2 && t > xs[k + 1]) k++;
        double h = xs[k + 1] - xs[k], u = (t - xs[k]) / h;
        double h00 = 2 * u * u * u - 3 * u * u + 1, h10 = u * u * u - 2 * u * u + u, h01 = -2 * u * u * u + 3 * u * u, h11 = u * u * u - u * u;
        return h00 * ys[k] + h10 * h * m[k] + h01 * ys[k + 1] + h11 * h * m[k + 1];
    }
}

public static class TntRig
{
    const int N = 512;
    static Img src, head, rim, rope, spark, body;
    static int[] beltTop = new int[N];
    static double[] frontEdge = new double[N];
    static double[] beltSmooth = new double[N];
    // Rope centreline (source px), densely resampled.
    static List<double> cx = new List<double>(), cy = new List<double>(), cs = new List<double>();
    static double ropeLen;
    static double[] projS, projN; // nearest centreline arc/normal for every source pixel
    static readonly double[] RootPt = { 208, 204 };
    static readonly double[] SparkC = { 368, 104 };

    // ------------------------------------------------------------------ colour helpers
    static void Hsv(float r, float g, float b, out double h, out double s, out double v)
    {
        double mx = Math.Max(r, Math.Max(g, b)), mn = Math.Min(r, Math.Min(g, b)), d = mx - mn;
        v = mx; s = mx <= 0 ? 0 : d / mx; h = 0;
        if (d > 0)
        {
            if (mx == r) h = 60 * (((g - b) / d) % 6);
            else if (mx == g) h = 60 * ((b - r) / d + 2);
            else h = 60 * ((r - g) / d + 4);
        }
        if (h < 0) h += 360;
    }
    static double Goldness(int x, int y)
    {
        int i = y * N + x; if (src.A[i] < 0.75f) return 0;
        float r, g, b; Straight(src, i, out r, out g, out b); double h, s, v; Hsv(r, g, b, out h, out s, out v);
        return (h >= 30 && h <= 62 && s > 0.55 && v > 0.45) ? 1 : 0;
    }
    static void Straight(Img im, int i, out float r, out float g, out float b)
    {
        float a = im.A[i];
        if (a < 1e-4f) { r = g = b = 0; return; }
        r = im.R[i] / a; g = im.G[i] / a; b = im.B[i] / a;
    }

    // ------------------------------------------------------------------ setup
    static void BuildCentreline()
    {
        double[] pts = { 217, 206, 222, 180, 232, 150, 246, 125, 263, 106, 285, 96, 310, 97, 333, 101, 353, 108 };
        int n = pts.Length / 2;
        cx.Clear(); cy.Clear(); cs.Clear();
        double acc = 0, px0 = pts[0], py0 = pts[1];
        for (int i = 0; i < n - 1; i++)
        {
            double p0x = pts[Math.Max(0, i - 1) * 2], p0y = pts[Math.Max(0, i - 1) * 2 + 1];
            double p1x = pts[i * 2], p1y = pts[i * 2 + 1], p2x = pts[(i + 1) * 2], p2y = pts[(i + 1) * 2 + 1];
            double p3x = pts[Math.Min(n - 1, i + 2) * 2], p3y = pts[Math.Min(n - 1, i + 2) * 2 + 1];
            for (int k = 0; k < 40; k++)
            {
                double t = k / 40.0, t2 = t * t, t3 = t2 * t;
                double x = 0.5 * (2 * p1x + (-p0x + p2x) * t + (2 * p0x - 5 * p1x + 4 * p2x - p3x) * t2 + (-p0x + 3 * p1x - 3 * p2x + p3x) * t3);
                double y = 0.5 * (2 * p1y + (-p0y + p2y) * t + (2 * p0y - 5 * p1y + 4 * p2y - p3y) * t2 + (-p0y + 3 * p1y - 3 * p2y + p3y) * t3);
                if (cx.Count > 0) acc += Math.Sqrt((x - px0) * (x - px0) + (y - py0) * (y - py0));
                cx.Add(x); cy.Add(y); cs.Add(acc); px0 = x; py0 = y;
            }
        }
        double lx = pts[(n - 1) * 2], ly = pts[(n - 1) * 2 + 1];
        acc += Math.Sqrt((lx - px0) * (lx - px0) + (ly - py0) * (ly - py0));
        cx.Add(lx); cy.Add(ly); cs.Add(acc);
        ropeLen = acc;
    }

    // Point and unit tangent on the centreline at arc s (extrapolated linearly past the ends).
    static void At(double s, out double x, out double y, out double tx, out double ty)
    {
        int n = cx.Count;
        if (s <= 0)
        {
            tx = cx[1] - cx[0]; ty = cy[1] - cy[0]; double l = Math.Sqrt(tx * tx + ty * ty); tx /= l; ty /= l;
            x = cx[0] + tx * s; y = cy[0] + ty * s; return;
        }
        if (s >= ropeLen)
        {
            tx = cx[n - 1] - cx[n - 2]; ty = cy[n - 1] - cy[n - 2]; double l = Math.Sqrt(tx * tx + ty * ty); tx /= l; ty /= l;
            x = cx[n - 1] + tx * (s - ropeLen); y = cy[n - 1] + ty * (s - ropeLen); return;
        }
        int lo = 0, hi = n - 1;
        while (hi - lo > 1) { int mid = (lo + hi) / 2; if (cs[mid] <= s) lo = mid; else hi = mid; }
        double seg = cs[hi] - cs[lo], u = seg > 1e-9 ? (s - cs[lo]) / seg : 0;
        x = cx[lo] + (cx[hi] - cx[lo]) * u; y = cy[lo] + (cy[hi] - cy[lo]) * u;
        tx = cx[hi] - cx[lo]; ty = cy[hi] - cy[lo]; double len = Math.Sqrt(tx * tx + ty * ty); tx /= len; ty /= len;
    }

    static void Project(double px, double py, out double s, out double nrm)
    {
        double best = 1e18; s = 0; nrm = 0;
        for (int i = 0; i < cx.Count - 1; i++)
        {
            double ax = cx[i], ay = cy[i], bx = cx[i + 1], by = cy[i + 1];
            double dx = bx - ax, dy = by - ay, l2 = dx * dx + dy * dy;
            double u = l2 > 1e-9 ? ((px - ax) * dx + (py - ay) * dy) / l2 : 0;
            if (i == 0 && u < 0) { } else if (u < 0) u = 0;
            if (i == cx.Count - 2 && u > 1) { } else if (u > 1) u = 1;
            double qx = ax + dx * u, qy = ay + dy * u, dd = (px - qx) * (px - qx) + (py - qy) * (py - qy);
            if (dd < best)
            {
                best = dd;
                double l = Math.Sqrt(l2);
                s = cs[i] + u * l;
                nrm = ((px - ax) * dy - (py - ay) * dx) / l; // signed distance, right-hand normal
            }
        }
    }

    static readonly double[] RadiusKeys = { 0, 33, 0.15, 37, 0.35, 37, 0.5, 34, 0.65, 32, 0.8, 26, 1.0, 19 };
    static double RopeRadius(double s) { double u = Math.Max(0, Math.Min(1, s / ropeLen)); return Curve.Eval(RadiusKeys, u) + 1; }

    // Least-squares quadratic y = a u^2 + b u + c, u = (x-256)/100, with two rounds of outlier rejection.
    static double[] RobustQuad(double[] ys, bool[] have, int x0, int x1)
    {
        var use = new bool[ys.Length]; for (int x = x0; x < x1; x++) use[x] = have[x];
        double qa = 0, qb = 0, qc = 0;
        for (int round = 0; round < 3; round++)
        {
            double s0 = 0, s1 = 0, s2 = 0, s3 = 0, s4 = 0, t0 = 0, t1 = 0, t2 = 0;
            for (int x = x0; x < x1; x++) if (use[x]) { double u = (x - 256) / 100.0; s0++; s1 += u; s2 += u * u; s3 += u * u * u; s4 += u * u * u * u; t0 += ys[x]; t1 += ys[x] * u; t2 += ys[x] * u * u; }
            double[,] M = { { s4, s3, s2, t2 }, { s3, s2, s1, t1 }, { s2, s1, s0, t0 } };
            for (int c = 0; c < 3; c++) for (int r = c + 1; r < 3; r++) { double f = M[r, c] / M[c, c]; for (int k = c; k < 4; k++) M[r, k] -= f * M[c, k]; }
            qc = M[2, 3] / M[2, 2]; qb = (M[1, 3] - M[1, 2] * qc) / M[1, 1]; qa = (M[0, 3] - M[0, 2] * qc - M[0, 1] * qb) / M[0, 0];
            for (int x = x0; x < x1; x++) if (use[x]) { double u = (x - 256) / 100.0; if (Math.Abs(ys[x] - (qa * u * u + qb * u + qc)) > (round == 0 ? 10 : 5)) use[x] = false; }
        }
        return new double[] { qa, qb, qc };
    }

    public static string Setup(string sourcePath, string debugDir, string patchPath)
    {
        var log = new StringBuilder();
        src = Img.Load(sourcePath);
        BuildCentreline();
        log.AppendLine("rope length " + ropeLen.ToString("0.0"));

        // Belt top: hand-measured curve (the light stave carries a gold stripe that colour alone cannot
        // separate from the belt), refined per column only where a clear red->gold transition exists.
        double[] beltKeys = { 82, 200, 89, 202, 104, 207, 114, 229, 131, 249, 147, 260, 164, 269, 184, 264, 204, 259, 224, 256, 251, 255, 277, 256, 297, 260, 317, 268, 331, 272, 347, 266, 364, 259, 384, 249, 401, 235, 411, 219, 417, 205, 430, 200 };
        for (int x = 0; x < N; x++)
        {
            int guess = (int)Math.Round(Curve.Eval(beltKeys, x));
            int best = guess; double bestScore = 1.5;
            for (int y = guess - 5; y <= guess + 5; y++)
            {
                if (y < 4 || y > N - 5) continue;
                double below = 0, above = 0;
                for (int k = 0; k < 3; k++) { below += Goldness(x, y + k); above += Goldness(x, y - 1 - k); }
                double score = below - above;
                if (score > bestScore) { bestScore = score; best = y; }
            }
            beltTop[x] = best;
        }
        for (int x = 0; x < N; x++) { double acc = 0; int c = 0; for (int k = -8; k <= 8; k++) { int xx = Math.Max(0, Math.Min(N - 1, x + k)); acc += beltTop[xx]; c++; } beltSmooth[x] = acc / c; }

        // Rope projection field for the rope's neighbourhood.
        projS = new double[N * N]; projN = new double[N * N];
        for (int y = 40; y < 240; y++)
            for (int x = 150; x < 430; x++) { double s, n; Project(x + 0.5, y + 0.5, out s, out n); projS[y * N + x] = s; projN[y * N + x] = n; }

        // Masks.
        var ropeM = new bool[N * N]; var sparkM = new bool[N * N];
        for (int y = 40; y < 240; y++)
            for (int x = 150; x < 430; x++)
            {
                int i = y * N + x; if (src.A[i] < 0.02f) continue;
                double s = projS[i], n = projN[i];
                float r, g, b; Straight(src, i, out r, out g, out b); double h, sat, v; Hsv(r, g, b, out h, out sat, out v);
                bool redish = (h < 12 || h > 345) && sat > 0.75;
                bool inTube = s > -4 && s < ropeLen + 3 && Math.Abs(n) <= RopeRadius(s) + 1.5;
                if (inTube && !redish && y < 205) ropeM[i] = true;
                double dx = x + 0.5 - SparkC[0], dy = y + 0.5 - SparkC[1];
                if (dx * dx + dy * dy < 60 * 60 && !ropeM[i] && !redish && y < 160) sparkM[i] = true;
            }
        // Dilate the fuse by one pixel (inside the image) so anti-aliased edges travel with it.
        var fuseD = new bool[N * N];
        for (int y = 1; y < N - 1; y++)
            for (int x = 1; x < N - 1; x++)
            {
                int i = y * N + x; if (src.A[i] < 0.001f || y >= 206) continue;
                bool any = false;
                for (int j = -2; j <= 2 && !any; j++) for (int k = -2; k <= 2 && !any; k++) { int yy = y + j, xx = x + k; if (yy < 0 || xx < 0 || yy >= N || xx >= N) continue; int q = yy * N + xx; if (ropeM[q] || sparkM[q]) any = true; }
                fuseD[i] = any;
            }
        for (int i = 0; i < N * N; i++) if (fuseD[i] && !ropeM[i] && !sparkM[i]) { double dx = i % N + 0.5 - SparkC[0], dy = i / N + 0.5 - SparkC[1]; if (dx * dx + dy * dy < 60 * 60 && i / N < 160) sparkM[i] = true; else ropeM[i] = true; }

        // Opening front edge and outer rim top: hand-measured from 5x / 3x zooms of the source.
        double[] frontKeys = { 150, 160, 164, 189, 172, 193, 180, 195, 200, 199, 220, 201, 256, 201, 272, 200, 288, 198, 304, 196, 316, 194, 330, 189, 340, 182, 348, 172, 360, 150 };
        for (int x = 0; x < N; x++) frontEdge[x] = Curve.Eval(frontKeys, x);
        double[] topKeys = { 100, 200, 116, 176, 129, 155, 146, 138, 163, 130, 179, 125, 196, 121, 226, 118, 256, 117, 286, 119, 310, 124, 326, 131, 349, 138, 369, 146, 386, 158, 396, 171, 408, 200 };
        Func<int, double> headTop = delegate (int x) { return Curve.Eval(topKeys, x); };
        log.AppendLine("belt top: x90=" + beltTop[90] + " x150=" + beltTop[150] + " x180=" + beltTop[180] + " x195=" + beltTop[195] + " x256=" + beltTop[256] + " x331=" + beltTop[331] + " x360=" + beltTop[360] + " x420=" + beltTop[420]);
        log.AppendLine("front edge: x180=" + frontEdge[180].ToString("0") + " x256=" + frontEdge[256].ToString("0") + " x330=" + frontEdge[330].ToString("0") + "; head top x256=" + headTop(256).ToString("0"));

        // Build layers.
        head = new Img(N, N); rim = new Img(N, N); rope = new Img(N, N); spark = new Img(N, N); body = new Img(N, N);
        var fill = new bool[N * N];
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                int i = y * N + x;
                bool isBody = y >= beltTop[x];
                Img dst;
                if (isBody) dst = body;
                else if (ropeM[i]) dst = rope;
                else if (sparkM[i]) dst = spark;
                else dst = head;
                dst.R[i] = src.R[i]; dst.G[i] = src.G[i]; dst.B[i] = src.B[i]; dst.A[i] = src.A[i];
                if (!isBody && (ropeM[i] || sparkM[i]) && y >= headTop(x) - 0.5 && x > 100 && x < 420) fill[i] = true;
                if (!isBody && !ropeM[i] && y >= frontEdge[x] && x >= 140 && x <= 372) { rim.R[i] = src.R[i]; rim.G[i] = src.G[i]; rim.B[i] = src.B[i]; rim.A[i] = src.A[i]; }
            }
        // Diffusion pre-fill (fallback), then elliptical texture fill: the rim and opening are
        // near-concentric ellipses about (256,170) with aspect 0.34, so a missing pixel copies real
        // rim/interior texture from the same ring further round, feathered at both seams.
        var orig = new Img(N, N);
        Array.Copy(head.R, orig.R, N * N); Array.Copy(head.G, orig.G, N * N); Array.Copy(head.B, orig.B, N * N); Array.Copy(head.A, orig.A, N * N);
        for (int i = 0; i < N * N; i++) if (fill[i]) { orig.A[i] = 0; orig.R[i] = orig.G[i] = orig.B[i] = 0; }
        for (int it = 0; it < 600; it++)
            for (int y = 1; y < N - 1; y++)
                for (int x = 1; x < N - 1; x++)
                {
                    int i = y * N + x; if (!fill[i]) continue;
                    float r = 0, g = 0, b = 0, a = 0, w = 0;
                    int[] nb = { i - 1, i + 1, i - N, i + N };
                    foreach (int q in nb)
                    {
                        bool usable = fill[q] || (head.A[q] > 0.5f && (q / N) < beltTop[q % N]);
                        if (!usable) continue;
                        r += head.R[q]; g += head.G[q]; b += head.B[q]; a += head.A[q]; w += 1;
                    }
                    if (w > 0) { head.R[i] = r / w; head.G[i] = g / w; head.B[i] = b / w; head.A[i] = a / w; }
                }
        const double ECX = 256, ECY = 170, EK = 0.34;
        var tMin = new double[400]; var tMax = new double[400];
        for (int k = 0; k < 400; k++) { tMin[k] = 1e9; tMax[k] = -1e9; }
        for (int i = 0; i < N * N; i++)
        {
            if (!fill[i]) continue;
            double dx = i % N + 0.5 - ECX, dy = (i / N + 0.5 - ECY) / EK;
            int rb = (int)Math.Sqrt(dx * dx + dy * dy); if (rb >= 400) continue;
            double th = Math.Atan2(dy, dx) * 180 / Math.PI; if (th < 0) th += 360;
            for (int k = Math.Max(0, rb - 2); k <= Math.Min(399, rb + 2); k++) { tMin[k] = Math.Min(tMin[k], th); tMax[k] = Math.Max(tMax[k], th); }
        }
        double gMin = 1e9, gMax = -1e9;
        for (int k = 60; k < 170; k++) if (tMax[k] >= tMin[k]) { gMin = Math.Min(gMin, tMin[k]); gMax = Math.Max(gMax, tMax[k]); }
        double globalShift = (gMax - gMin) + 6;
        log.AppendLine("fill angular span " + gMin.ToString("0") + ".." + gMax.ToString("0") + " deg, rigid shift " + globalShift.ToString("0"));
        int texFilled = 0;
        for (int i = 0; i < N * N; i++)
        {
            if (!fill[i]) continue;
            double px = i % N + 0.5, py = i / N + 0.5;
            double dx = px - ECX, dy = (py - ECY) / EK, rr = Math.Sqrt(dx * dx + dy * dy);
            int rb = (int)rr; if (rb >= 400 || tMax[rb] < tMin[rb]) continue;
            double th = Math.Atan2(dy, dx) * 180 / Math.PI; if (th < 0) th += 360;
            double shift = globalShift;
            Func<double, float[]> sampleAt = delegate (double ang)
            {
                double a = ang * Math.PI / 180, sx = ECX + rr * Math.Cos(a), sy = ECY + rr * Math.Sin(a) * EK;
                float r, g, b, al; orig.Sample(sx, sy, out r, out g, out b, out al);
                return new float[] { r, g, b, al };
            };
            var copy = sampleAt(th + shift);
            if (copy[3] < 0.85f) continue; // landed off the rim: keep the diffusion fallback
            // Feather each seam with a reflection of the untouched neighbour texture.
            const double F = 11;
            double eLo = th - tMin[rb], eHi = tMax[rb] - th;
            float[] res = copy;
            if (eLo < F) { var refl = sampleAt(tMin[rb] - eLo - 1); if (refl[3] > 0.85f) { float t = (float)(eLo / F); t = t * t * (3 - 2 * t); res = new float[] { refl[0] + (res[0] - refl[0]) * t, refl[1] + (res[1] - refl[1]) * t, refl[2] + (res[2] - refl[2]) * t, refl[3] + (res[3] - refl[3]) * t }; } }
            if (eHi < F) { var refl = sampleAt(tMax[rb] + eHi + 1); if (refl[3] > 0.85f) { float t = (float)(eHi / F); t = t * t * (3 - 2 * t); res = new float[] { refl[0] + (res[0] - refl[0]) * t, refl[1] + (res[1] - refl[1]) * t, refl[2] + (res[2] - refl[2]) * t, refl[3] + (res[3] - refl[3]) * t }; } }
            head.R[i] = res[0]; head.G[i] = res[1]; head.B[i] = res[2]; head.A[i] = res[3];
            texFilled++;
        }
        int fillCount = 0; for (int i = 0; i < N * N; i++) if (fill[i]) fillCount++;
        log.AppendLine("inpaint: " + fillCount + " px, texture-filled " + texFilled);

        if (!string.IsNullOrEmpty(patchPath) && File.Exists(patchPath))
        {
            // Generated fuse-less TNT on black: black composite == premultiplied colour.
            var gen = Img.Load(patchPath);
            var bg = new bool[N * N]; var stack = new Stack<int>();
            Func<int, bool> nearBlack = delegate (int q) { return Math.Max(gen.R[q], Math.Max(gen.G[q], gen.B[q])) < 0.07f; };
            for (int x = 0; x < N; x++) { stack.Push(x); stack.Push((N - 1) * N + x); }
            for (int y = 0; y < N; y++) { stack.Push(y * N); stack.Push(y * N + N - 1); }
            while (stack.Count > 0)
            {
                int q = stack.Pop(); if (bg[q] || !nearBlack(q)) continue; bg[q] = true;
                int qx = q % N, qy = q / N;
                if (qx > 0) stack.Push(q - 1); if (qx < N - 1) stack.Push(q + 1); if (qy > 0) stack.Push(q - N); if (qy < N - 1) stack.Push(q + N);
            }
            for (int q = 0; q < N * N; q++) gen.A[q] = bg[q] ? 0f : 1f;

            // Registration on untouched rim/head texture well away from the patch.
            var near = new bool[N * N];
            for (int q = 0; q < N * N; q++) if (fill[q]) { int qx = q % N, qy = q / N; for (int j = -10; j <= 10; j++) for (int k = -10; k <= 10; k++) { int yy = qy + j, xx = qx + k; if (yy >= 0 && xx >= 0 && yy < N && xx < N) near[yy * N + xx] = true; } }
            var refPts = new List<int>();
            for (int y = 110; y < 250; y += 2) for (int x = 110; x < 410; x += 2) { int q = y * N + x; if (!near[q] && head.A[q] > 0.97f && y < beltTop[x] - 2) refPts.Add(q); }
            double bestErr = 1e18, bs = 1, btx = 0, bty = 0;
            Func<double, double, double, double> err = delegate (double s, double tx, double ty)
            {
                double e = 0;
                foreach (int q in refPts)
                {
                    double px = q % N + 0.5, py = q / N + 0.5;
                    float r, g, b, a; gen.Sample(256 + s * (px - 256) + tx, 256 + s * (py - 256) + ty, out r, out g, out b, out a);
                    double dr = r - orig.R[q], dg = g - orig.G[q], db = b - orig.B[q];
                    e += dr * dr + dg * dg + db * db;
                }
                return e;
            };
            for (double s = 0.95; s <= 1.05; s += 0.005) for (double tx = -10; tx <= 10; tx += 1) for (double ty = -10; ty <= 10; ty += 1) { double e = err(s, tx, ty); if (e < bestErr) { bestErr = e; bs = s; btx = tx; bty = ty; } }
            double cs0 = bs, ctx0 = btx, cty0 = bty;
            for (double s = cs0 - 0.005; s <= cs0 + 0.005; s += 0.001) for (double tx = ctx0 - 1; tx <= ctx0 + 1; tx += 0.25) for (double ty = cty0 - 1; ty <= cty0 + 1; ty += 0.25) { double e = err(s, tx, ty); if (e < bestErr) { bestErr = e; bs = s; btx = tx; bty = ty; } }
            log.AppendLine("patch registration: scale " + bs.ToString("0.000") + " offset (" + btx.ToString("0.00") + ", " + bty.ToString("0.00") + ") rms " + Math.Sqrt(bestErr / Math.Max(1, refPts.Count * 3)).ToString("0.000") + " over " + refPts.Count + " px");

            // Per-channel gain/offset from a ring around the patch.
            double[] sx = new double[3], sy = new double[3], sxx = new double[3], sxy = new double[3]; double cnt = 0;
            for (int q = 0; q < N * N; q++)
            {
                if (fill[q] || !near[q] || head.A[q] < 0.97f) continue;
                int qx = q % N, qy = q / N; if (qy >= beltTop[qx] - 2) continue;
                float r, g, b, a; gen.Sample(256 + bs * (qx + 0.5 - 256) + btx, 256 + bs * (qy + 0.5 - 256) + bty, out r, out g, out b, out a);
                if (a < 0.99f) continue;
                double[] gv = { r, g, b }, sv = { orig.R[q], orig.G[q], orig.B[q] };
                for (int c = 0; c < 3; c++) { sx[c] += gv[c]; sy[c] += sv[c]; sxx[c] += gv[c] * gv[c]; sxy[c] += gv[c] * sv[c]; }
                cnt++;
            }
            var gain = new double[3]; var off = new double[3];
            for (int c = 0; c < 3; c++) { double d = cnt * sxx[c] - sx[c] * sx[c]; gain[c] = Math.Abs(d) > 1e-9 ? (cnt * sxy[c] - sx[c] * sy[c]) / d : 1; gain[c] = Math.Max(0.7, Math.Min(1.4, gain[c])); off[c] = (sy[c] - gain[c] * sx[c]) / cnt; }
            log.AppendLine("patch colour: gain " + gain[0].ToString("0.00") + "/" + gain[1].ToString("0.00") + "/" + gain[2].ToString("0.00") + " from " + cnt + " px");

            int patched = 0;
            for (int q = 0; q < N * N; q++)
            {
                if (!fill[q]) continue;
                int qx = q % N, qy = q / N;
                float r, g, b, a; gen.Sample(256 + bs * (qx + 0.5 - 256) + btx, 256 + bs * (qy + 0.5 - 256) + bty, out r, out g, out b, out a);
                if (a < 0.02f) { head.R[q] = head.G[q] = head.B[q] = head.A[q] = 0; patched++; continue; }
                float sr = r / a, sg = g / a, sb = b / a;
                sr = (float)Math.Max(0, Math.Min(1, gain[0] * sr + off[0])); sg = (float)Math.Max(0, Math.Min(1, gain[1] * sg + off[1])); sb = (float)Math.Max(0, Math.Min(1, gain[2] * sb + off[2]));
                head.R[q] = sr * a; head.G[q] = sg * a; head.B[q] = sb * a; head.A[q] = a; patched++;
            }
            // Feather INWARD (inside the patch, which the rope always covers at rest): blend from a
            // smooth diffusion fill at the patch boundary to the generated pixels 5 px inside.
            var diff = new Img(N, N);
            for (int q = 0; q < N * N; q++) { diff.R[q] = orig.R[q]; diff.G[q] = orig.G[q]; diff.B[q] = orig.B[q]; diff.A[q] = orig.A[q]; }
            for (int it = 0; it < 400; it++)
                for (int y = 1; y < N - 1; y++)
                    for (int x = 1; x < N - 1; x++)
                    {
                        int q = y * N + x; if (!fill[q]) continue;
                        float sr2 = 0, sg2 = 0, sb2 = 0, sa2 = 0, w2 = 0;
                        int[] nb = { q - 1, q + 1, q - N, q + N };
                        foreach (int t in nb) { if (!fill[t] && orig.A[t] < 0.5f) continue; sr2 += diff.R[t]; sg2 += diff.G[t]; sb2 += diff.B[t]; sa2 += diff.A[t]; w2++; }
                        if (w2 > 0) { diff.R[q] = sr2 / w2; diff.G[q] = sg2 / w2; diff.B[q] = sb2 / w2; diff.A[q] = sa2 / w2; }
                    }
            var dIn = new int[N * N];
            for (int q = 0; q < N * N; q++) dIn[q] = fill[q] ? 99 : 0;
            for (int pass = 0; pass < 5; pass++)
                for (int q = 0; q < N * N; q++)
                {
                    if (dIn[q] != 99) continue; int qx = q % N, qy = q / N; bool hit = false;
                    for (int j = -1; j <= 1 && !hit; j++) for (int k = -1; k <= 1 && !hit; k++) { int yy = qy + j, xx = qx + k; if (yy < 0 || xx < 0 || yy >= N || xx >= N) continue; if (dIn[yy * N + xx] == pass) hit = true; }
                    if (hit) dIn[q] = pass + 1;
                }
            int feathered = 0;
            for (int q = 0; q < N * N; q++)
            {
                if (!fill[q] || dIn[q] > 5 || diff.A[q] < 0.5f || head.A[q] < 0.02f) continue;
                float w = (float)Smooth(0, 6, dIn[q]);
                head.R[q] = diff.R[q] * (1 - w) + head.R[q] * w; head.G[q] = diff.G[q] * (1 - w) + head.G[q] * w; head.B[q] = diff.B[q] * (1 - w) + head.B[q] * w;
                feathered++;
            }
            log.AppendLine("patched " + patched + " px from generated fuse-less TNT, inward-feathered " + feathered);
        }

        for (int i = 0; i < N * N; i++) if (rim.A[i] > 0) { head.R[i] = head.G[i] = head.B[i] = head.A[i] = 0; }

        // Clean spark: the original spark glow overlaps the rim below and the rope tip to its left,
        // so its lower half is cut from rim-coloured pixels. Rebuild the whole starburst from the
        // clean up-right quadrant (over transparent background) by 90-degree rotations.
        spark = new Img(N, N);
        const double SCX = 368, SCY = 104, SR = 60;
        for (int y = (int)(SCY - SR); y <= (int)(SCY + SR); y++)
            for (int x = (int)(SCX - SR); x <= (int)(SCX + SR); x++)
            {
                if (x < 0 || y < 0 || x >= N || y >= N) continue;
                double dx = x + 0.5 - SCX, dy = y + 0.5 - SCY, rr = Math.Sqrt(dx * dx + dy * dy);
                if (rr > SR) continue;
                double th = Math.Atan2(dy, dx); // image coords: up-right quadrant is th in [-pi/2, 0]
                double q = th; while (q < -Math.PI / 2) q += Math.PI / 2; while (q > 0) q -= Math.PI / 2;
                double sx = SCX + rr * Math.Cos(q), sy = SCY + rr * Math.Sin(q);
                float r, g, b, a; src.Sample(sx, sy, out r, out g, out b, out a);
                float fade = (float)Smooth(SR, SR - 10, rr);
                int k = y * N + x;
                spark.R[k] = r * fade; spark.G[k] = g * fade; spark.B[k] = b * fade; spark.A[k] = a * fade;
            }

        if (!string.IsNullOrEmpty(debugDir))
        {
            Directory.CreateDirectory(debugDir);
            var over = new Img(N, N);
            for (int i = 0; i < N * N; i++)
            {
                float lum = (src.R[i] * 0.3f + src.G[i] * 0.6f + src.B[i] * 0.1f);
                float r = lum, g = lum, b = lum;
                int y = i / N, x = i % N;
                if (ropeM[i]) { r = 0.2f + lum * .3f; g = 0.9f; b = 0.3f; }
                else if (sparkM[i]) { r = 1f; g = 0.2f + lum * .3f; b = 1f; }
                else if (rim.A[i] > 0.01f) { r = 0.3f; g = 0.6f + lum * .3f; b = 1f; }
                else if (y >= beltTop[x]) { r = 1f; g = 0.75f + lum * .2f; b = 0.1f; }
                else if (head.A[i] > 0.01f) { r = 0.9f; g = 0.2f + lum * 0.2f; b = 0.25f; }
                float a = src.A[i];
                over.R[i] = r * a; over.G[i] = g * a; over.B[i] = b * a; over.A[i] = a;
            }
            for (int s = 0; s < cx.Count; s++) { int xi = (int)cx[s], yi = (int)cy[s]; if (xi >= 0 && yi >= 0 && xi < N && yi < N) { int i = yi * N + xi; over.R[i] = 0; over.G[i] = 0; over.B[i] = 0; over.A[i] = 1; } }
            for (int x = 140; x <= 372; x++) { int yi = (int)frontEdge[x]; if (yi > 0 && yi < N) { int i = yi * N + x; over.R[i] = 1; over.G[i] = 1; over.B[i] = 1; over.A[i] = 1; } }
            over.Save(Path.Combine(debugDir, "masks.png"));
            head.Save(Path.Combine(debugDir, "layer_head_inpainted.png"));
            rope.Save(Path.Combine(debugDir, "layer_rope.png"));
            spark.Save(Path.Combine(debugDir, "layer_spark.png"));
            rim.Save(Path.Combine(debugDir, "layer_rim.png"));
            body.Save(Path.Combine(debugDir, "layer_body.png"));
        }
        return log.ToString();
    }

    // ================================================================== animation
    // Keys are (frame, value) over frames 0..119 at 60 fps.
    public static double[] K_Scale = { 0, 0.62, 4, 0.80, 10, 0.97, 18, 1.02, 30, 1.00, 48, 1.05, 60, 1.08, 66, 1.05, 72, 0.96, 80, 1.02, 88, 0.99, 96, 1.005, 104, 1.0 };
    public static double[] K_SqX = { 0, 1.12, 4, 1.06, 10, 0.92, 16, 1.03, 22, 0.99, 40, 0.98, 60, 0.96, 66, 0.98, 72, 1.07, 80, 0.97, 88, 1.02, 96, 0.995, 104, 1.0 };
    public static double[] K_SqY = { 0, 0.80, 4, 0.88, 10, 1.10, 16, 0.97, 22, 1.01, 40, 1.02, 60, 1.05, 66, 1.02, 72, 0.92, 80, 1.04, 88, 0.98, 96, 1.005, 104, 1.0 };
    public static double[] K_Lift = { 0, 0, 14, 0, 18, 5, 24, 16, 30, 26, 36, 32, 42, 38, 48, 44, 54, 50, 60, 54, 64, 55, 68, 40, 72, 8, 75, -4, 84, 9, 92, 0, 97, -1, 104, 0 };
    public static double[] K_Tilt = { 0, 0, 18, 0, 26, -3, 40, 2, 54, -2, 62, 1.5, 72, 0, 80, -1.5, 88, 1, 96, 0 };
    public static double[] K_FuseExt = { 0, 0, 20, 0, 26, 0.15, 32, 0.55, 38, 0.95, 44, 1.12, 52, 1.18, 60, 1.20, 68, 1.05, 74, 0.97, 82, 1.03, 90, 0.99, 98, 1.0 };
    public static double[] K_Sway = { 0, 0, 28, 0, 34, 10, 42, -6, 50, 5, 58, -4, 66, 6, 74, -5, 82, 3, 90, -1.5, 98, 0.5, 106, 0 };
    public static double[] K_Glow = { 0, 0, 13, 0, 18, 0.45, 26, 0.65, 32, 0.78, 40, 0.9, 54, 1.0, 62, 1.0, 70, 0.7, 76, 0.35, 84, 0.15, 92, 0.05, 100, 0 };
    public static double[] K_Flame = { 0, 0, 30, 0, 36, 0.18, 42, 0.45, 48, 0.7, 54, 0.9, 60, 1.0, 66, 0.9, 72, 0.65, 80, 0.35, 88, 0.15, 96, 0.04, 102, 0 };

    // Canvas mapping: source base pivot -> output base pivot at content scale Kc.
    public static double Kc = 0.84;
    static readonly double PX = 256, PY = 458, OX = 256, OY = 480, HPX = 256, HPY = 258;

    public static Img FlameSprite; // optional generated flame (null = procedural placeholder)

    sealed class Spark { public double x, y, vx, vy, size, life, born, phase; }
    static List<Spark> sparks;

    static double Rand(int seed) { double v = Math.Sin(seed * 12.9898 + 78.233) * 43758.5453; return v - Math.Floor(v); }

    static void BuildSparks()
    {
        sparks = new List<Spark>();
        int[] bursts = { 57, 71 }; int[] counts = { 22, 14 };
        for (int bI = 0; bI < bursts.Length; bI++)
            for (int i = 0; i < counts[bI]; i++)
            {
                int sd = bI * 100 + i;
                double th = (i + 0.5 + (Rand(sd) - 0.5) * 0.8) / counts[bI] * Math.PI * 2;
                var p = new Spark();
                p.born = bursts[bI] + Rand(sd + 7) * 4;
                p.x = 256 + Math.Cos(th) * 150; p.y = 200 + Math.Sin(th) * 38; // seam ellipse, body space (approx)
                double sp = 2.2 + Rand(sd + 3) * 3.0;
                p.vx = Math.Cos(th) * sp * 1.2; p.vy = Math.Sin(th) * sp * 0.45 - (1.2 + Rand(sd + 5) * 2.2);
                p.size = 16 + Rand(sd + 9) * 16; p.life = 26 + Rand(sd + 11) * 16; p.phase = Rand(sd + 13) * 6.28;
                sparks.Add(p);
            }
    }

    static void Ramp(double I, out float r, out float g, out float b)
    {
        // white-hot -> saturated yellow -> warm gold
        I = Math.Max(0, Math.Min(1, I));
        if (I > 0.72) { double t = (I - 0.72) / 0.28; r = 1; g = (float)(0.92 + 0.08 * t); b = (float)(0.45 + 0.50 * t); }
        else if (I > 0.38) { double t = (I - 0.38) / 0.34; r = 1; g = (float)(0.70 + 0.22 * t); b = (float)(0.14 + 0.31 * t); }
        else { double t = I / 0.38; r = 1; g = (float)(0.52 + 0.18 * t); b = (float)(0.06 + 0.08 * t); }
    }

    static double Smooth(double e0, double e1, double x) { double t = Math.Max(0, Math.Min(1, (x - e0) / (e1 - e0))); return t * t * (3 - 2 * t); }

    public static string Render(string outDir, int fStart, int fEnd, bool restTest)
    {
        Directory.CreateDirectory(outDir);
        if (sparks == null) BuildSparks();
        var sb = new StringBuilder();
        double tipX, tipY, tipTX, tipTY; At(ropeLen, out tipX, out tipY, out tipTX, out tipTY);
        double tipAngle0 = Math.Atan2(tipTY, tipTX);
        double beltC = beltTop[256];

        for (int fr = fStart; fr <= fEnd; fr++)
        {
            double S = Curve.Eval(K_Scale, fr), sqx = Curve.Eval(K_SqX, fr), sqy = Curve.Eval(K_SqY, fr);
            double lift = Curve.Eval(K_Lift, fr), tilt = Curve.Eval(K_Tilt, fr) * Math.PI / 180;
            double ext = Curve.Eval(K_FuseExt, fr), sway = Curve.Eval(K_Sway, fr) * Math.PI / 180;
            double glow = Curve.Eval(K_Glow, fr), flame = Curve.Eval(K_Flame, fr);
            double kc = Kc, ox = OX, oy = OY;
            if (restTest) { S = sqx = sqy = 1; lift = tilt = sway = 0; ext = 1; glow = flame = 0; kc = 1; ox = PX; oy = PY; }

            double gx = kc * S * sqx, gy = kc * S * sqy;
            double ct = Math.Cos(tilt), st = Math.Sin(tilt);
            double emerge = Math.Min(1, ext), stretch = Math.Max(1, ext);
            double cw = Math.Cos(sway), sw = Math.Sin(sway);
            double tipS = emerge * ropeLen;
            double tx2, ty2, ttx, tty; At(tipS, out tx2, out ty2, out ttx, out tty);
            double dAng = Math.Atan2(tty, ttx) - tipAngle0;
            double cd = Math.Cos(dAng), sd = Math.Sin(dAng);
            double sparkScale = Smooth(0.06, 0.35, emerge);
            double shiftS = (1 - emerge) * ropeLen;
            double time = fr / 60.0;

            var outImg = new Img(N, N);
            var flameBack = new Img(N, N); var flameFront = new Img(N, N);
            if (!restTest) DrawTongues(flameBack, flameFront, flame, lift, gx, gy, ox, oy, kc, time);
            double[] offs = restTest ? new double[] { 0.5 } : new double[] { 0.25, 0.75 };
            float invCount = 1f / (offs.Length * offs.Length);
            for (int oyI = 0; oyI < N; oyI++)
                for (int oxI = 0; oxI < N; oxI++)
                {
                    float aR = 0, aG = 0, aB = 0, aA = 0;
                    foreach (double sy2 in offs)
                        foreach (double sx2 in offs)
                        {
                            double oX = oxI + sx2, oY = oyI + sy2;
                            // body space
                            double bx = PX + (oX - ox) / gx, by = PY + (oY - oy) / gy;
                            // head space (inverse of: rotate about head pivot, then lift)
                            double hx0 = bx - HPX, hy0 = by - HPY + lift;
                            double hx = HPX + ct * hx0 + st * hy0, hy = HPY - st * hx0 + ct * hy0;
                            // rest-rope space (inverse of sway+stretch about the rope root)
                            double rx0 = (hx - RootPt0X) / stretch, ry0 = (hy - RootPt0Y) / stretch;
                            double rx = RootPt0X + cw * rx0 + sw * ry0, ry = RootPt0Y - sw * rx0 + cw * ry0;

                            float r = 0, g = 0, b = 0, a = 0;
                            float lr, lg, lb, la;

                            // 1. back flame tongues (behind head and body)
                            if (!restTest) { flameBack.Sample(oX, oY, out lr, out lg, out lb, out la); r = lr; g = lg; b = lb; a = la; }
                            // 2. gap light (only shows where head/body leave a gap)
                            if (glow > 0.001 && lift > 0.2 && !restTest)
                            {
                                int bxi = (int)Math.Floor(bx);
                                if (bxi >= 96 && bxi <= 416)
                                {
                                    double bt = beltSmooth[bxi];
                                    double top = bt - lift - 12, bot = bt + 2;
                                    if (by >= top && by <= bot)
                                    {
                                        double vv = (by - (bt - lift)) / Math.Max(2, lift + 2);
                                        double xf = Smooth(100, 132, bx) * Smooth(412, 380, bx);
                                        double flick = 0.88 + 0.12 * Math.Sin(bx * 0.09 + time * 26) * Math.Sin(bx * 0.031 - time * 17);
                                        double core = Math.Exp(-Math.Pow((vv - 0.5) / 0.32, 2));
                                        double I = glow * (0.30 + 0.70 * core) * flick * (0.55 + 0.45 * xf);
                                        Ramp(I, out lr, out lg, out lb); la = (float)(xf * Smooth(0.0, 0.25, glow));
                                        lr *= la; lg *= la; lb *= la;
                                        r = lr + r * (1 - la); g = lg + g * (1 - la); b = lb + b * (1 - la); a = la + a * (1 - la);
                                    }
                                }
                            }
                            // 3. head (+ dark underside lip, visible only when lifted)
                            head.Sample(hx, hy, out lr, out lg, out lb, out la);
                            if (!restTest && lift > 0.2)
                            {
                                int hxi = (int)Math.Floor(hx);
                                if (hxi >= 96 && hxi <= 416 && hy >= beltTop[hxi] - 0.5 && hy < beltTop[hxi] + 3.5)
                                {
                                    float lipA = (float)(Smooth(96, 112, hx) * Smooth(416, 400, hx));
                                    float lr2 = 0.33f, lg2 = 0.05f, lb2 = 0.03f;
                                    lr = lr2 * lipA + lr * (1 - lipA); lg = lg2 * lipA + lg * (1 - lipA); lb = lb2 * lipA + lb * (1 - lipA); la = lipA + la * (1 - lipA);
                                }
                            }
                            r = lr + r * (1 - la); g = lg + g * (1 - la); b = lb + b * (1 - la); a = la + a * (1 - la);
                            // 4. rope (emerges tip-first along its own curve)
                            if (emerge > 0.001)
                            {
                                int rxi = (int)Math.Floor(rx), ryi = (int)Math.Floor(ry);
                                if (rxi >= 150 && rxi < 430 && ryi >= 40 && ryi < 240)
                                {
                                    double s0 = projS[ryi * N + rxi], n0 = projN[ryi * N + rxi];
                                    if (shiftS < 0.01 || (s0 <= tipS + 4.5 && s0 > -8))
                                    {
                                        if (shiftS < 0.01) rope.Sample(rx, ry, out lr, out lg, out lb, out la);
                                        else
                                        {
                                            double cx2, cy2, tx3, ty3; At(s0 + shiftS, out cx2, out cy2, out tx3, out ty3);
                                            double nx = ty3, ny = -tx3; // right-hand normal matching Project()
                                            rope.Sample(cx2 + nx * n0, cy2 + ny * n0, out lr, out lg, out lb, out la);
                                        }
                                        r = lr + r * (1 - la); g = lg + g * (1 - la); b = lb + b * (1 - la); a = la + a * (1 - la);
                                    }
                                }
                                // 5. spark at the rope tip
                                if (sparkScale > 0.001)
                                {
                                    double qx0 = (rx - tx2) / sparkScale, qy0 = (ry - ty2) / sparkScale;
                                    double qx = tipX + cd * qx0 + sd * qy0, qy = tipY - sd * qx0 + cd * qy0;
                                    if (qx > 290 && qx < 440 && qy > 30 && qy < 175)
                                    {
                                        spark.Sample(qx, qy, out lr, out lg, out lb, out la);
                                        r = lr + r * (1 - la); g = lg + g * (1 - la); b = lb + b * (1 - la); a = la + a * (1 - la);
                                    }
                                }
                            }
                            // 6. front rim occluder
                            rim.Sample(hx, hy, out lr, out lg, out lb, out la);
                            r = lr + r * (1 - la); g = lg + g * (1 - la); b = lb + b * (1 - la); a = la + a * (1 - la);
                            // 7. body
                            body.Sample(bx, by, out lr, out lg, out lb, out la);
                            r = lr + r * (1 - la); g = lg + g * (1 - la); b = lb + b * (1 - la); a = la + a * (1 - la);
                            // 7b. front flame tongues (over the seam and past the silhouette)
                            if (!restTest) { flameFront.Sample(oX, oY, out lr, out lg, out lb, out la); r = lr + r * (1 - la); g = lg + g * (1 - la); b = lb + b * (1 - la); a = la + a * (1 - la); }
                            // 8. soft bloom over the seam
                            if (glow > 0.001 && !restTest)
                            {
                                double cyB = beltC - lift * 0.5;
                                double u = (bx - 256) / 185.0, v = (by - cyB) / (24 + lift * 0.9);
                                double d2 = u * u + v * v;
                                if (d2 < 2.6)
                                {
                                    double peak = Smooth(50, 58, fr) * Smooth(70, 62, fr);
                                    double I = glow * (0.62 + 0.38 * peak) * Math.Exp(-2.4 * d2) * (0.9 + 0.1 * Math.Sin(time * 31));
                                    Ramp(0.45 + 0.55 * I / 0.62, out lr, out lg, out lb);
                                    float add = (float)I;
                                    r = Math.Min(1, r + lr * add); g = Math.Min(1, g + lg * add); b = Math.Min(1, b + lb * add * 0.8f); a = Math.Max(a, Math.Min(1, add * 1.1f));
                                }
                            }
                            aR += r; aG += g; aB += b; aA += a;
                        }
                    int oi = oyI * N + oxI;
                    outImg.R[oi] = aR * invCount; outImg.G[oi] = aG * invCount; outImg.B[oi] = aB * invCount; outImg.A[oi] = aA * invCount;
                }

            // 9. sparkles in output space (they must not squash with the barrel)
            if (!restTest)
                foreach (var p in sparks)
                {
                    double age = fr - p.born; if (age < 0 || age > p.life) continue;
                    double px = p.x, py = p.y, vx = p.vx, vy = p.vy;
                    for (int st2 = 0; st2 < (int)age; st2++) { px += vx; py += vy; vx *= 0.93; vy = vy * 0.93 + 0.06; }
                    double lifeK = Math.Sin(Math.PI * age / p.life);
                    double tw = 0.72 + 0.28 * Math.Sin(age * 0.55 + p.phase);
                    double size = p.size * kc * tw * (0.55 + 0.45 * lifeK);
                    double cxo = ox + (px - PX) * gx, cyo = oy + (py - PY - lift * 0.3) * gy;
                    int x0 = (int)(cxo - size * 1.2), x1 = (int)(cxo + size * 1.2), y0 = (int)(cyo - size * 1.2), y1 = (int)(cyo + size * 1.2);
                    for (int yy = Math.Max(0, y0); yy <= Math.Min(N - 1, y1); yy++)
                        for (int xx = Math.Max(0, x0); xx <= Math.Min(N - 1, x1); xx++)
                        {
                            double dx = (xx + 0.5 - cxo) / size, dy = (yy + 0.5 - cyo) / size;
                            double core = Math.Exp(-(dx * dx + dy * dy) / 0.02);
                            double rays = Math.Exp(-Math.Abs(dx) / 0.05) * Math.Exp(-dy * dy / 0.30) + Math.Exp(-Math.Abs(dy) / 0.05) * Math.Exp(-dx * dx / 0.30);
                            double I = Math.Min(1, core + rays) * Math.Pow(lifeK, 0.7);
                            if (I < 0.01) continue;
                            float cr, cg, cb; Ramp(0.4 + 0.6 * Math.Min(1, core * 1.4 + rays * 0.5), out cr, out cg, out cb);
                            int oi = yy * N + xx; float la2 = (float)I;
                            outImg.R[oi] = cr * la2 + outImg.R[oi] * (1 - la2); outImg.G[oi] = cg * la2 + outImg.G[oi] * (1 - la2); outImg.B[oi] = cb * la2 + outImg.B[oi] * (1 - la2); outImg.A[oi] = la2 + outImg.A[oi] * (1 - la2);
                        }
                }

            string name = restTest ? "rest_test.png" : string.Format("TNT_Reveal_{0:000}.png", fr + 1);
            outImg.Save(Path.Combine(outDir, name));
            if (restTest)
            {
                double se = 0; int cnt = 0, big = 0;
                for (int i = 0; i < N * N; i++)
                {
                    double d = Math.Abs(outImg.R[i] - src.R[i]) + Math.Abs(outImg.G[i] - src.G[i]) + Math.Abs(outImg.B[i] - src.B[i]) + Math.Abs(outImg.A[i] - src.A[i]);
                    se += d * d; cnt++; if (d > 0.08) big++;
                }
                var heat = new Img(N, N);
                for (int i = 0; i < N * N; i++) { double d = Math.Abs(outImg.R[i] - src.R[i]) + Math.Abs(outImg.G[i] - src.G[i]) + Math.Abs(outImg.B[i] - src.B[i]) + Math.Abs(outImg.A[i] - src.A[i]); float v = (float)Math.Min(1, d * 4); heat.R[i] = v; heat.G[i] = src.A[i] * 0.15f; heat.B[i] = src.A[i] * 0.15f; heat.A[i] = 1; }
                heat.Save(Path.Combine(outDir, "rest_diff_heat.png"));
                sb.AppendLine("rest reconstruction RMSE " + Math.Sqrt(se / cnt).ToString("0.00000") + ", pixels off by >0.08: " + big);
            }
        }
        return sb.ToString();
    }

    static double RootPt0X { get { return 217; } }
    static double RootPt0Y { get { return 206; } }

    // ================================================================== generated flame tongues
    public static Img[] Flames;            // 9 sprites, 256x256, base anchor (128, 236), flame height 200
    const double FlameAnchorX = 128, FlameAnchorY = 236, FlameHeight = 200;

    public static string LoadFlames(string sheetPath)
    {
        var sheet = Img.Load(sheetPath);
        int cell = sheet.W / 3;
        Flames = new Img[9];
        var log = new StringBuilder();
        for (int cy = 0; cy < 3; cy++)
            for (int cxI = 0; cxI < 3; cxI++)
            {
                int x0 = cxI * cell, y0 = cy * cell;
                int top = int.MaxValue, bottom = -1; double wx = 0, ww = 0;
                for (int y = y0 + 4; y < y0 + cell - 4; y++)
                    for (int x = x0 + 4; x < x0 + cell - 4; x++)
                    {
                        int i = y * sheet.W + x; float m = Math.Max(sheet.R[i], Math.Max(sheet.G[i], sheet.B[i]));
                        if (m > 0.10f) { if (y < top) top = y; if (y > bottom) bottom = y; }
                    }
                for (int y = bottom - 30; y <= bottom; y++)
                    for (int x = x0 + 4; x < x0 + cell - 4; x++)
                    { int i = y * sheet.W + x; float m = Math.Max(sheet.R[i], Math.Max(sheet.G[i], sheet.B[i])); if (m > 0.5f) { wx += x * m; ww += m; } }
                double baseX = ww > 0 ? wx / ww : x0 + cell / 2.0, baseY = bottom;
                double s = FlameHeight / Math.Max(10, bottom - top);
                var spr = new Img(256, 256);
                for (int y = 0; y < 256; y++)
                    for (int x = 0; x < 256; x++)
                    {
                        double sx = baseX + (x + 0.5 - FlameAnchorX) / s, sy = baseY + (y + 0.5 - FlameAnchorY) / s;
                        if (sx < x0 + 2 || sx > x0 + cell - 2 || sy < y0 + 2 || sy > y0 + cell - 2) continue;
                        float r, g, b, a; sheet.Sample(sx, sy, out r, out g, out b, out a);
                        float mx = Math.Max(r, Math.Max(g, b));
                        if (mx < 0.015f) continue;
                        float al = (float)Math.Min(1, Math.Pow(mx, 0.85) * 1.05);
                        int k = y * 256 + x;
                        spr.R[k] = r / mx * al; spr.G[k] = g / mx * al; spr.B[k] = b / mx * al; spr.A[k] = al;
                    }
                Flames[cy * 3 + cxI] = spr;
                log.Append("flame " + (cy * 3 + cxI) + " h=" + (bottom - top) + " base(" + (baseX - x0).ToString("0") + "," + (baseY - y0).ToString("0") + ")  ");
            }
        return log.ToString();
    }

    static void DrawTongues(Img back, Img front, double flame, double lift, double gx, double gy, double ox, double oy, double kc, double time)
    {
        if (Flames == null || flame < 0.003) return;
        const int count = 16;
        double cyE = beltSmooth[256] - lift * 0.5 - 30, ea = 158, eb = 30;
        for (int i = 0; i < count; i++)
        {
            double th = 2 * Math.PI * (i + 0.5) / count + (Rand(i * 31 + 3) - 0.5) * 0.25;
            double sn = Math.Sin(th), cs2 = Math.Cos(th);
            double bxA = 256 + ea * cs2, byA = cyE + eb * sn + 30;
            double oxA = ox + (bxA - PX) * gx, oyA = oy + (byA - PY) * gy;
            double side = Math.Abs(cs2);
            double pulse = 0.82 + 0.18 * Math.Sin(time * 7.3 + i * 2.1);
            double heightPx = Math.Pow(flame, 0.8) * 185 * kc * pulse * (0.70 + 0.42 * side) * (sn > 0 ? 0.90 : 1.0);
            double sc = heightPx / FlameHeight;
            if (sc < 0.01) continue;
            double rot = (cs2 * 16 + 5 * Math.Sin(time * 9 + i * 1.3)) * Math.PI / 180;
            double cr = Math.Cos(rot), sr = Math.Sin(rot);
            double t = Rand(i * 17 + 11) * 9 + time * 13.5;
            int f0 = ((int)Math.Floor(t)) % 9, f1 = (f0 + 1) % 9; float mix = (float)(t - Math.Floor(t));
            mix = mix * mix * (3 - 2 * mix);
            var dst = sn < -0.15 ? back : front;
            float bright = (float)Math.Min(1, flame * 1.25);
            double ext = 256 * sc;
            int x0 = Math.Max(0, (int)(oxA - ext)), x1 = Math.Min(N - 1, (int)(oxA + ext)), y0 = Math.Max(0, (int)(oyA - ext)), y1 = Math.Min(N - 1, (int)(oyA + ext));
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    double dx = (x + 0.5 - oxA) / sc, dy = (y + 0.5 - oyA) / sc;
                    double lx = FlameAnchorX + cr * dx + sr * dy, ly = FlameAnchorY - sr * dx + cr * dy;
                    if (lx < 0 || ly < 0 || lx >= 256 || ly >= 256) continue;
                    float r0, g0, b0, a0, r1, g1, b1, a1;
                    Flames[f0].Sample(lx, ly, out r0, out g0, out b0, out a0);
                    Flames[f1].Sample(lx, ly, out r1, out g1, out b1, out a1);
                    float la = (a0 + (a1 - a0) * mix) * bright; if (la < 0.004f) continue;
                    float lr = (r0 + (r1 - r0) * mix) * bright, lg = (g0 + (g1 - g0) * mix) * bright, lb = (b0 + (b1 - b0) * mix) * bright;
                    int k = y * N + x;
                    dst.R[k] = lr + dst.R[k] * (1 - la); dst.G[k] = lg + dst.G[k] * (1 - la); dst.B[k] = lb + dst.B[k] * (1 - la); dst.A[k] = la + dst.A[k] * (1 - la);
                }
        }
    }


    // ================================================================== idle loop
    // All idle curves are periodic over LoopFrames with matching slope at the wrap, so frame 120
    // flows into frame 1 with no jump and no kink. Keys are (frame, value) with a key at frame 0;
    // the value at LoopFrames is implicitly the frame-0 value.
    public const int LoopFrames = 120;

    // Body breath: slight inhale at 0.4 s, return 0.8 s, tiny echo 1.2 s, settle 1.6 s.
    public static double[] I_Scale = { 0, 1.0, 24, 1.012, 48, 1.0, 72, 1.003, 96, 0.9985 };
    public static double[] I_SqY   = { 0, 1.0, 24, 1.004, 48, 1.0, 72, 1.001, 96, 1.0 };
    // Head/rim: delayed secondary vertical flex about its base (no lift, so no gap ever opens).
    public static double[] I_HeadSY = { 0, 1.0, 32, 1.010, 56, 0.994, 80, 1.006, 100, 0.999 };
    // Fuse bend at the tip, degrees: gentle wobble with a small flick at ~1.2 s.
    public static double[] I_Bend = { 0, 0, 18, 2.4, 40, -1.8, 62, 0.4, 74, 3.4, 86, -2.2, 100, 1.0, 112, -0.4 };
    // Spark envelope: small -> brighter -> small -> brief sparkle -> small.
    public static double[] I_SparkScale  = { 0, 1.0, 12, 0.94, 30, 1.08, 48, 0.95, 70, 1.0, 76, 1.12, 82, 0.96, 100, 0.98 };
    public static double[] I_SparkBright = { 0, 1.0, 12, 0.90, 30, 1.25, 48, 0.92, 70, 1.0, 76, 1.40, 82, 0.95, 100, 0.97 };
    // Warm glow around the fuse/head.
    public static double[] I_Glow = { 0, 0, 20, 0.20, 30, 0.40, 48, 0.18, 66, 0.10, 76, 0.40, 86, 0.20, 104, 0.08 };

    // Fixed spark shape variants, cycled with crossfades: ray modulation amount/phase, spin (deg), scale, brightness.
    static readonly double[,] SparkVariants = {
        { 0.00, 0.0,  0, 1.00, 1.00 },
        { 0.10, 0.6,  3, 0.97, 0.95 },
        { 0.12, 2.1, -4, 1.03, 1.08 },
        { 0.08, 3.9,  6, 0.99, 1.00 },
        { 0.11, 5.0, -2, 1.02, 1.05 },
        { 0.06, 1.4, -6, 0.98, 0.97 } };
    const int FramesPerVariant = 5; // 6 variants x 5 frames = 30 frames: 4 whole cycles per loop

    // Periodic monotone cubic: Fritsch-Carlson tangents computed with wrap-around.
    public static double CycEval(double[] keys, double f)
    {
        int n = keys.Length / 2;
        double period = LoopFrames;
        f = ((f % period) + period) % period;
        var xs = new double[n + 1]; var ys = new double[n + 1];
        for (int i = 0; i < n; i++) { xs[i] = keys[i * 2]; ys[i] = keys[i * 2 + 1]; }
        xs[n] = period; ys[n] = ys[0];
        var d = new double[n];
        for (int i = 0; i < n; i++) d[i] = (ys[i + 1] - ys[i]) / (xs[i + 1] - xs[i]);
        var m = new double[n + 1];
        for (int i = 0; i < n; i++)
        {
            double dPrev = d[(i - 1 + n) % n], dNext = d[i];
            m[i] = (dPrev * dNext <= 0) ? 0 : (dPrev + dNext) / 2;
        }
        m[n] = m[0];
        for (int i = 0; i < n; i++)
        {
            if (d[i] == 0) continue;
            double a = m[i] / d[i], b = m[i + 1] / d[i], s = a * a + b * b;
            if (s > 9) { double tau = 3 / Math.Sqrt(s); m[i] = tau * a * d[i]; m[i + 1] = tau * b * d[i]; if (i + 1 == n) m[0] = m[n]; }
        }
        int k = 0;
        while (k < n - 1 && f >= xs[k + 1]) k++;
        double h = xs[k + 1] - xs[k], u = (f - xs[k]) / h;
        double h00 = 2 * u * u * u - 3 * u * u + 1, h10 = u * u * u - 2 * u * u + u, h01 = -2 * u * u * u + 3 * u * u, h11 = u * u * u - u * u;
        return h00 * ys[k] + h10 * h * m[k] + h01 * ys[k + 1] + h11 * h * m[k + 1];
    }

    sealed class IdleSparkle { public int born, life; public double dx, dy, vx, vy, size; }
    static readonly IdleSparkle[] IdleSparkles = {
        new IdleSparkle { born = 18, life = 22, dx = 26, dy = -30, vx = 0.25, vy = -0.35, size = 14 },
        new IdleSparkle { born = 74, life = 20, dx = -34, dy = -22, vx = -0.22, vy = -0.30, size = 18 },
        new IdleSparkle { born = 96, life = 18, dx = 38, dy = 6, vx = 0.30, vy = -0.15, size = 11 } };

    static void StarAt(Img dst, double cx, double cy, double size, double intensity, double rayAngle)
    {
        if (intensity < 0.005 || size < 0.5) return;
        double cr = Math.Cos(rayAngle), sr = Math.Sin(rayAngle);
        int x0 = (int)(cx - size * 1.3), x1 = (int)(cx + size * 1.3), y0 = (int)(cy - size * 1.3), y1 = (int)(cy + size * 1.3);
        for (int yy = Math.Max(0, y0); yy <= Math.Min(N - 1, y1); yy++)
            for (int xx = Math.Max(0, x0); xx <= Math.Min(N - 1, x1); xx++)
            {
                double px = (xx + 0.5 - cx) / size, py = (yy + 0.5 - cy) / size;
                double dx = cr * px + sr * py, dy = -sr * px + cr * py;
                // Round core plus the MAX of two thin ray ridges: summing the ridges saturates into a
                // visible diamond where they cross.
                double core = 0.9 * Math.Exp(-(dx * dx + dy * dy) / 0.006);
                double rayX = Math.Exp(-dy * dy / 0.0010) * Math.Exp(-dx * dx / 0.30), rayY = Math.Exp(-dx * dx / 0.0010) * Math.Exp(-dy * dy / 0.30);
                double rays = Math.Max(rayX, rayY);
                double I = Math.Min(1, core + rays * (1 - core)) * intensity;
                if (I < 0.004) continue;
                float cr2, cg, cb; Ramp(0.45 + 0.55 * Math.Min(1, core * 1.4 + rays * 0.5), out cr2, out cg, out cb);
                int oi = yy * N + xx; float la = (float)Math.Min(1, I);
                dst.R[oi] = cr2 * la + dst.R[oi] * (1 - la); dst.G[oi] = cg * la + dst.G[oi] * (1 - la); dst.B[oi] = cb * la + dst.B[oi] * (1 - la); dst.A[oi] = la + dst.A[oi] * (1 - la);
            }
    }

    public static string RenderIdle(string outDir, int fStart, int fEnd)
    {
        Directory.CreateDirectory(outDir);
        var log = new StringBuilder();

        // The head flexes vertically about its own cut line in every column, so its bottom edge
        // never moves and no gap can open against the belt.
        var headX = head;

        const double C0X = 368, C0Y = 104;
        double R0X = RootPt0X, R0Y = RootPt0Y;

        for (int fr = fStart; fr <= fEnd; fr++)
        {
            double S = CycEval(I_Scale, fr), sqy = CycEval(I_SqY, fr), hsy = CycEval(I_HeadSY, fr);
            double bendTip = CycEval(I_Bend, fr) * Math.PI / 180;
            double scEnv = CycEval(I_SparkScale, fr), brEnv = CycEval(I_SparkBright, fr), glow = Math.Max(0, CycEval(I_Glow, fr));
            double kc = Kc, ox = OX, oy = OY;
            double gx = kc * S, gy = kc * S * sqy;

            // spark variant crossfade
            double u = (double)fr / FramesPerVariant;
            int vi = ((int)Math.Floor(u)) % 6, vj = (vi + 1) % 6;
            double vmix = u - Math.Floor(u); vmix = vmix * vmix * (3 - 2 * vmix);

            // spark centre in head space follows the bent tip
            double cB = Math.Cos(bendTip), sB = Math.Sin(bendTip);
            double chx = R0X + cB * (C0X - R0X) - sB * (C0Y - R0Y), chy = R0Y + sB * (C0X - R0X) + cB * (C0Y - R0Y);

            var outImg = new Img(N, N);
            double[] offs = { 0.25, 0.75 };
            for (int oyI = 0; oyI < N; oyI++)
                for (int oxI = 0; oxI < N; oxI++)
                {
                    float aR = 0, aG = 0, aB = 0, aA = 0;
                    foreach (double sy2 in offs)
                        foreach (double sx2 in offs)
                        {
                            double oX = oxI + sx2, oY = oyI + sy2;
                            double bx = PX + (oX - ox) / gx, by = PY + (oY - oy) / gy;
                            double hx = bx; int hcol = Math.Max(0, Math.Min(N - 1, (int)Math.Floor(hx))); double pv = beltTop[hcol]; double hy = pv + (by - pv) / hsy;
                            float r = 0, g = 0, b = 0, a = 0, lr, lg, lb, la;

                            // head (extruded copy)
                            headX.Sample(hx, hy, out lr, out lg, out lb, out la);
                            r = lr; g = lg; b = lb; a = la;

                            // rope, bent progressively toward the tip (fixed-point inverse)
                            double rx = hx, ry = hy;
                            bool ropeOk = true;
                            if (Math.Abs(bendTip) > 1e-6)
                            {
                                double th0 = -bendTip * 0.6, c0 = Math.Cos(th0), s0r = Math.Sin(th0);
                                rx = R0X + c0 * (hx - R0X) - s0r * (hy - R0Y); ry = R0Y + s0r * (hx - R0X) + c0 * (hy - R0Y);
                                for (int it = 0; it < 3; it++)
                                {
                                    int xi = (int)Math.Floor(rx), yi = (int)Math.Floor(ry);
                                    if (xi < 150 || xi >= 430 || yi < 40 || yi >= 240) { ropeOk = false; break; }
                                    double sArc = projS[yi * N + xi];
                                    double w = Math.Pow(Smooth(0.12 * ropeLen, ropeLen, sArc), 1.2);
                                    double th = -bendTip * w, ct2 = Math.Cos(th), st3 = Math.Sin(th);
                                    rx = R0X + ct2 * (hx - R0X) - st3 * (hy - R0Y); ry = R0Y + st3 * (hx - R0X) + ct2 * (hy - R0Y);
                                }
                            }
                            if (ropeOk)
                            {
                                rope.Sample(rx, ry, out lr, out lg, out lb, out la);
                                r = lr + r * (1 - la); g = lg + g * (1 - la); b = lb + b * (1 - la); a = la + a * (1 - la);
                            }

                            // spark: two fixed variants crossfaded, under the brightness/scale envelope
                            double dxs = hx - chx, dys = hy - chy;
                            if (dxs * dxs + dys * dys < 80 * 80)
                            {
                                float sr0 = 0, sg0 = 0, sb0 = 0, sa0 = 0;
                                for (int pass = 0; pass < 2; pass++)
                                {
                                    int v = pass == 0 ? vi : vj; double wv = pass == 0 ? 1 - vmix : vmix;
                                    if (wv < 1e-4) continue;
                                    double amp = SparkVariants[v, 0], ph = SparkVariants[v, 1], spin = SparkVariants[v, 2] * Math.PI / 180;
                                    double vsc = SparkVariants[v, 3] * scEnv, vbr = SparkVariants[v, 4];
                                    double ang = -(bendTip + spin), ca = Math.Cos(ang), sa = Math.Sin(ang);
                                    double lx = (ca * dxs - sa * dys) / vsc, ly = (sa * dxs + ca * dys) / vsc;
                                    double rho = Math.Sqrt(lx * lx + ly * ly), phi = Math.Atan2(ly, lx);
                                    // Vary the rays only; the soft outer glow stays round.
                                    double ampR = amp * (1 - Smooth(26 * vsc, 48 * vsc, rho));
                                    double mod = 1 + ampR * Math.Cos(4 * phi + ph) + ampR * 0.35 * Math.Cos(8 * phi - ph);
                                    double rs = rho / Math.Max(0.5, mod);
                                    float pr, pg, pb, pa; spark.Sample(C0X + rs * Math.Cos(phi), C0Y + rs * Math.Sin(phi), out pr, out pg, out pb, out pa);
                                    sr0 += (float)(pr * vbr * wv); sg0 += (float)(pg * vbr * wv); sb0 += (float)(pb * vbr * wv); sa0 += (float)(pa * wv);
                                }
                                // Brighter means hotter: blend the core toward white instead of scaling channels
                                // (scaling saturates red and drifts the spark toward lime). Dimmer darkens.
                                double hot = Math.Max(0, Math.Min(0.32, (brEnv - 1) * 0.9)), dim = Math.Min(1, brEnv);
                                sr0 = (float)((sr0 + (sa0 - sr0) * hot) * dim);
                                sg0 = (float)((sg0 + (sa0 * 0.97 - sg0) * hot) * dim);
                                sb0 = (float)((sb0 + (sa0 * 0.88 - sb0) * hot * 0.85) * dim);
                                sa0 = (float)Math.Min(1, sa0 * (0.85 + 0.15 * Math.Min(1.3, brEnv)));
                                r = sr0 + r * (1 - sa0); g = sg0 + g * (1 - sa0); b = sb0 + b * (1 - sa0); a = sa0 + a * (1 - sa0);
                            }

                            // front rim and body
                            rim.Sample(hx, hy, out lr, out lg, out lb, out la);
                            r = lr + r * (1 - la); g = lg + g * (1 - la); b = lb + b * (1 - la); a = la + a * (1 - la);
                            body.Sample(bx, by, out lr, out lg, out lb, out la);
                            r = lr + r * (1 - la); g = lg + g * (1 - la); b = lb + b * (1 - la); a = la + a * (1 - la);

                            aR += r; aG += g; aB += b; aA += a;
                        }
                    int oi = oyI * N + oxI;
                    outImg.R[oi] = aR / 4; outImg.G[oi] = aG / 4; outImg.B[oi] = aB / 4; outImg.A[oi] = aA / 4;
                }

            // spark centre in output space
            double pvS = beltTop[Math.Max(0, Math.Min(N - 1, (int)Math.Floor(chx)))]; double cbx = chx, cby = pvS + (chy - pvS) * hsy;
            double cox = ox + (cbx - PX) * gx, coy = oy + (cby - PY) * gy;

            // warm glow pulse around the fuse spark and the nearby rim (additive light)
            if (glow > 0.002)
            {
                double s1 = 30 * kc, s2 = 66 * kc;
                double hrx = ox + (236 - PX) * gx, hry = oy + (beltTop[236] + (150 - beltTop[236]) * hsy - PY) * gy, s3 = 72 * kc;
                for (int y = 0; y < N; y++)
                    for (int x = 0; x < N; x++)
                    {
                        int oi = y * N + x;
                        double dx = x + 0.5 - cox, dy = y + 0.5 - coy, d2 = dx * dx + dy * dy;
                        double I = glow * (0.50 * Math.Exp(-d2 / (2 * s1 * s1)) + 0.30 * Math.Exp(-d2 / (2 * s2 * s2)));
                        double hdx = x + 0.5 - hrx, hdy = y + 0.5 - hry;
                        double Ih = glow * 0.16 * Math.Exp(-(hdx * hdx + hdy * hdy) / (2 * s3 * s3)) * outImg.A[oi];
                        I *= 1 - 0.75 * outImg.A[oi]; // a halo around the spark, not paint over its core
                        double tot = I + Ih; if (tot < 0.003) continue;
                        float cr2, cg, cb; Ramp(0.18 + 0.40 * Math.Min(1, tot * 1.6), out cr2, out cg, out cb);
                        float add = (float)tot;
                        outImg.R[oi] = Math.Min(1, outImg.R[oi] + cr2 * add); outImg.G[oi] = Math.Min(1, outImg.G[oi] + cg * add); outImg.B[oi] = Math.Min(1, outImg.B[oi] + cb * add * 0.6f);
                        outImg.A[oi] = Math.Max(outImg.A[oi], Math.Min(1, (float)I * 0.9f));
                        outImg.R[oi] = Math.Min(outImg.R[oi], outImg.A[oi]); outImg.G[oi] = Math.Min(outImg.G[oi], outImg.A[oi]); outImg.B[oi] = Math.Min(outImg.B[oi], outImg.A[oi]);
                    }
            }

            // occasional tiny sparkles near the fuse (all born and gone within the loop)
            foreach (var p in IdleSparkles)
            {
                int age = fr - p.born; if (age < 0 || age > p.life) continue;
                double lifeK = Math.Sin(Math.PI * age / p.life);
                double sxp = cox + (p.dx + p.vx * age) * kc, syp = coy + (p.dy + p.vy * age) * kc;
                double tw = 0.75 + 0.25 * Math.Sin(age * 0.7 + p.dx);
                StarAt(outImg, sxp, syp, p.size * kc * tw * (0.6 + 0.4 * lifeK), Math.Pow(lifeK, 0.7), 0);
            }
            // brief glint across the spark at the sparkle beat
            if (fr >= 70 && fr <= 84)
            {
                double gk = Math.Sin(Math.PI * (fr - 70) / 14.0);
                StarAt(outImg, cox, coy, 52 * kc * (0.7 + 0.3 * gk), Math.Pow(gk, 1.5), Math.PI / 8);
            }

            outImg.Save(Path.Combine(outDir, string.Format("TNT_Idle_{0:000}.png", fr + 1)));
        }
        log.AppendLine("rendered idle frames " + (fStart + 1) + ".." + (fEnd + 1));
        return log.ToString();
    }

}
