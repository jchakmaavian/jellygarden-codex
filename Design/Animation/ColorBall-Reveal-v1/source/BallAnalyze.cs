// Analysis of the Royal Throne color_ball art: body vs stud segmentation, sphere fit, stud components.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

public static class BallAnalyze
{
    public static void Hsv(float r, float g, float b, out double h, out double s, out double v)
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

    public static string Run(string srcPath, string outDir)
    {
        var sb = new StringBuilder();
        var src = Img.Load(srcPath);
        int N = src.W;
        // Classify: body = opaque, value < 0.45 and bluish or dark; stud = opaque and (value >= 0.45 or strongly non-blue hue).
        var cls = new int[N * N]; // 0 transparent, 1 body, 2 stud, 3 edge (partial alpha)
        var body = new Img(N, N); var studs = new Img(N, N);
        for (int i = 0; i < N * N; i++)
        {
            float a = src.A[i];
            if (a < 0.02f) continue;
            float r = src.R[i] / a, g = src.G[i] / a, b = src.B[i] / a;
            double h, s, v; Hsv(r, g, b, out h, out s, out v);
            double chroma = Math.Max(r, Math.Max(g, b)) - Math.Min(r, Math.Min(g, b));
            bool blueHue = h >= 185 && h <= 265;
            bool isBody = blueHue ? chroma <= 0.52 : chroma <= 0.26;
            cls[i] = isBody ? 1 : 2;
            var target = isBody ? body : studs;
            target.R[i] = src.R[i]; target.G[i] = src.G[i]; target.B[i] = src.B[i]; target.A[i] = a;
        }
        body.Save(Path.Combine(outDir, "an_body.png"));
        studs.Save(Path.Combine(outDir, "an_studs.png"));

        // Sphere fit: boundary pixels (opaque with a transparent 4-neighbour) whose colour is body-like.
        var xs = new List<double>(); var ys = new List<double>();
        for (int y = 1; y < N - 1; y++)
            for (int x = 1; x < N - 1; x++)
            {
                int i = y * N + x;
                if (src.A[i] < 0.5f) continue;
                bool edge = src.A[i - 1] < 0.5f || src.A[i + 1] < 0.5f || src.A[i - N] < 0.5f || src.A[i + N] < 0.5f;
                if (!edge || cls[i] != 1) continue;
                xs.Add(x + 0.5); ys.Add(y + 0.5);
            }
        double cx, cy, rad; FitCircle(xs, ys, out cx, out cy, out rad);
        // Reject outliers (points under protruding studs), refit.
        for (int round = 0; round < 3; round++)
        {
            var nx = new List<double>(); var ny = new List<double>();
            for (int k = 0; k < xs.Count; k++) { double d = Math.Sqrt((xs[k] - cx) * (xs[k] - cx) + (ys[k] - cy) * (ys[k] - cy)); if (Math.Abs(d - rad) < 3.5) { nx.Add(xs[k]); ny.Add(ys[k]); } }
            xs = nx; ys = ny; FitCircle(xs, ys, out cx, out cy, out rad);
        }
        sb.AppendLine(string.Format("sphere centre ({0:0.00}, {1:0.00}) radius {2:0.00} from {3} boundary points", cx, cy, rad, xs.Count));

        // Fill holes (specular highlights) enclosed by stud pixels: flood the non-stud area from the canvas border.
        {
            var outside = new bool[N * N]; var fq = new Queue<int>();
            for (int k = 0; k < N; k++) { foreach (int b0 in new[] { k, (N - 1) * N + k, k * N, k * N + N - 1 }) if (cls[b0] != 2 && !outside[b0]) { outside[b0] = true; fq.Enqueue(b0); } }
            while (fq.Count > 0) { int k = fq.Dequeue(); int kx = k % N, ky = k / N; foreach (int j in new[] { kx > 0 ? k - 1 : -1, kx < N - 1 ? k + 1 : -1, ky > 0 ? k - N : -1, ky < N - 1 ? k + N : -1 }) { if (j < 0 || outside[j] || cls[j] == 2) continue; if (cls[j] == 1) { // body pixels only escape through other body pixels, so small enclosed holes stay inside
                    } outside[j] = true; fq.Enqueue(j); } }
            int filled = 0; for (int k = 0; k < N * N; k++) if (!outside[k] && cls[k] != 2 && src.A[k] > 0.02f) { cls[k] = 2; filled++; }
            sb.AppendLine("filled enclosed pixels: " + filled);
        }
        // Stud components: 8-connected regions of class 2 (plus partial-alpha pixels adjacent to them).
        var label = new int[N * N]; int next = 0;
        var comps = new List<int[]>();
        for (int i = 0; i < N * N; i++)
        {
            if (cls[i] != 2 || label[i] != 0) continue;
            next++;
            var q = new Queue<int>(); q.Enqueue(i); label[i] = next;
            int count = 0; double sx = 0, sy = 0, sr = 0, sg = 0, sbb = 0; int minX = N, minY = N, maxX = 0, maxY = 0;
            while (q.Count > 0)
            {
                int k = q.Dequeue(); int kx = k % N, ky = k / N;
                count++; sx += kx; sy += ky;
                float a = src.A[k]; sr += src.R[k] / a; sg += src.G[k] / a; sbb += src.B[k] / a;
                minX = Math.Min(minX, kx); maxX = Math.Max(maxX, kx); minY = Math.Min(minY, ky); maxY = Math.Max(maxY, ky);
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int nx = kx + dx, ny = ky + dy; if (nx < 0 || ny < 0 || nx >= N || ny >= N) continue;
                        int j = ny * N + nx; if (cls[j] != 2 || label[j] != 0) continue;
                        label[j] = next; q.Enqueue(j);
                    }
            }
            if (count < 40) continue;
            double mr = sr / count, mg = sg / count, mb = sbb / count, h, s, v; Hsv((float)mr, (float)mg, (float)mb, out h, out s, out v);
            double px = sx / count + 0.5, py = sy / count + 0.5;
            double ddx = px - cx, ddy = py - cy, dist = Math.Sqrt(ddx * ddx + ddy * ddy);
            comps.Add(new[] { next, count, (int)Math.Round(px), (int)Math.Round(py), minX, minY, maxX, maxY });
            sb.AppendLine(string.Format("stud {0,2}: area {1,5} centre ({2:0.0},{3:0.0}) bbox {4},{5}-{6},{7} rgb ({8:0.00},{9:0.00},{10:0.00}) hue {11:0} sat {12:0.00} val {13:0.00} r/R {14:0.00}",
                next, count, px, py, minX, minY, maxX, maxY, mr, mg, mb, h, s, v, dist / rad));
        }
        // Label visualisation.
        var vis = new Img(N, N);
        for (int i = 0; i < N * N; i++)
        {
            if (src.A[i] < 0.02f) continue;
            if (label[i] > 0) { double u = Hash.U(label[i], 7), w = Hash.U(label[i], 11); vis.R[i] = (float)(0.3 + 0.7 * u); vis.G[i] = (float)(0.3 + 0.7 * w); vis.B[i] = (float)(0.3 + 0.7 * (1 - u)); vis.A[i] = 1; }
            else if (cls[i] == 1) { vis.R[i] = 0.08f; vis.G[i] = 0.1f; vis.B[i] = 0.25f; vis.A[i] = 1; }
            else { vis.R[i] = 0.6f; vis.G[i] = 0.6f; vis.B[i] = 0.6f; vis.A[i] = 1; }
        }
        // Mark fitted circle.
        for (int t = 0; t < 2000; t++)
        {
            double ang = t * 2 * Math.PI / 2000; int x = (int)(cx + rad * Math.Cos(ang)), y = (int)(cy + rad * Math.Sin(ang));
            if (x >= 0 && y >= 0 && x < N && y < N) { int i = y * N + x; vis.R[i] = 1; vis.G[i] = 1; vis.B[i] = 1; vis.A[i] = 1; }
        }
        vis.Save(Path.Combine(outDir, "an_labels.png"));
        sb.AppendLine("components kept: " + comps.Count);
        return sb.ToString();
    }

    static void FitCircle(List<double> xs, List<double> ys, out double cx, out double cy, out double r)
    {
        // Kasa algebraic fit.
        int n = xs.Count; double sx = 0, sy = 0, sxx = 0, syy = 0, sxy = 0, sxz = 0, syz = 0, sz = 0;
        for (int i = 0; i < n; i++)
        {
            double x = xs[i], y = ys[i], z = x * x + y * y;
            sx += x; sy += y; sxx += x * x; syy += y * y; sxy += x * y; sxz += x * z; syz += y * z; sz += z;
        }
        double[,] M = { { sxx, sxy, sx, sxz }, { sxy, syy, sy, syz }, { sx, sy, n, sz } };
        for (int c = 0; c < 3; c++) for (int rr = c + 1; rr < 3; rr++) { double f = M[rr, c] / M[c, c]; for (int k = c; k < 4; k++) M[rr, k] -= f * M[c, k]; }
        double C = M[2, 3] / M[2, 2], B = (M[1, 3] - M[1, 2] * C) / M[1, 1], A = (M[0, 3] - M[0, 2] * C - M[0, 1] * B) / M[0, 0];
        cx = A / 2; cy = B / 2; r = Math.Sqrt(C + cx * cx + cy * cy);
    }
}
