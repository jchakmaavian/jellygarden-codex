// Royal Throne Color Ball rig. The original art is the only source of the design:
//  - Body: the navy sphere with its fixed screen-space lighting (studs and stud shadows removed, smooth fill).
//  - Studs: tangent-plane decals cut from the original face-on studs, placed on a sphere shell at their original
//    3D positions. Edge studs, which the art only shows as slivers, use a colour-matched face-on decal; green and
//    magenta decals are colour-transferred from face-on studs. The unseen back of the ball carries a colour-permuted
//    copy of the front pattern.
//  - The studs spin about the vertical screen axis with rotational motion blur; body lighting never rotates.
// Frames at rest are taken from the untouched original art by the sequence scripts.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

public sealed class BallLayers { public Img Body, Studs; }

public static class BallRig
{
    public const int N = 512;
    public static Img Src, Body, StudLayer;
    public static double Cx, Cy, R, Rs;
    static float[] shadow;
    static bool[] studMask;
    public static bool[] StudMask { get { return studMask; } }
    const int TW = 1440, TH = 720;
    // Studs are small domes: three shells, each carrying the decals at a shrinking angular size.
    // Same decal size on every shell (a short cylinder): from the front the lower shells hide behind the top one,
    // at the limb they read as the stud side wall. Lower shells are darker.
    static readonly double[] ShellRadius = { 1.00, 1.018, 1.036, 1.054, 1.072, 1.09 };
    static readonly double[] ShellScale = { 1.0, 1.0, 1.0, 1.0, 1.0, 1.0 };
    static readonly float[] ShellTint = { 0.46f, 0.55f, 0.64f, 0.74f, 0.86f, 1.0f };
    static Img[] studTex; static float[] shadowTex;
    const int PS = 128; const double PR = 0.30; // decal patch texels and angular half-size (radians)

    sealed class Patch { public Img Col; public float[] Shadow; }
    sealed class Stud { public string Name; public double X, Y; public string Class; public bool Own; public double Lon, Lat; }

    // Image-space centroids from the segmentation pass (components) and visual placement for merged/edge studs.
    static readonly Stud[] Studs =
    {
        new Stud { Name = "orange-centre-top", X = 285.6, Y = 185.4, Class = "orange", Own = true },
        new Stud { Name = "blue-left", X = 156.9, Y = 238.1, Class = "blue", Own = true },
        new Stud { Name = "orange-right", X = 402.7, Y = 248.1, Class = "orange", Own = true },
        new Stud { Name = "red-centre", X = 254.1, Y = 307.8, Class = "red", Own = true },
        new Stud { Name = "yellow-lower-left", X = 135.9, Y = 359.8, Class = "yellow", Own = true },
        new Stud { Name = "blue-lower-right", X = 350.6, Y = 362.4, Class = "blue2", Own = true },
        new Stud { Name = "yellow-top-left", X = 196, Y = 112, Class = "yellow", Own = false },
        new Stud { Name = "blue-top", X = 316, Y = 98, Class = "blue", Own = false },
        new Stud { Name = "orange-bottom", X = 240.5, Y = 435.0, Class = "orange", Own = false },
        new Stud { Name = "orange-top-left-edge", X = 150, Y = 95, Class = "orange", Own = false },
        new Stud { Name = "green-top-edge", X = 272, Y = 60, Class = "green", Own = false },
        new Stud { Name = "magenta-top-right-edge", X = 396.4, Y = 115.2, Class = "magenta", Own = false },
        new Stud { Name = "magenta-left-edge", X = 98.0, Y = 173.9, Class = "magenta", Own = false },
        new Stud { Name = "orange-right-upper-edge", X = 445, Y = 195, Class = "orange", Own = false },
        new Stud { Name = "orange-right-lower-edge", X = 448, Y = 325, Class = "orange", Own = false },
        new Stud { Name = "green-left-edge", X = 67.1, Y = 292.1, Class = "green", Own = false },
        new Stud { Name = "blue-bottom-left-edge", X = 123.9, Y = 412.2, Class = "blue2", Own = false },
        new Stud { Name = "green-bottom-right-edge", X = 379.4, Y = 424.6, Class = "green", Own = false },
    };

    static readonly Dictionary<string, string> BackClass = new Dictionary<string, string>
    { { "orange", "orange" }, { "blue", "blue2" }, { "blue2", "blue" }, { "red", "red" }, { "yellow", "yellow" }, { "green", "green" }, { "magenta", "magenta" } };

    public static string Setup(string srcPath, string debugDir)
    {
        var log = new StringBuilder();
        Src = Img.Load(srcPath);
        int n = N * N;
        studMask = new bool[n];
        for (int i = 0; i < n; i++)
        {
            float a = Src.A[i]; if (a < 0.02f) continue;
            float r = Src.R[i] / a, g = Src.G[i] / a, b = Src.B[i] / a;
            double h, s, v; BallAnalyze.Hsv(r, g, b, out h, out s, out v);
            double chroma = Math.Max(r, Math.Max(g, b)) - Math.Min(r, Math.Min(g, b));
            studMask[i] = (h >= 185 && h <= 265) ? chroma > 0.52 : chroma > 0.26;
        }
        FillHoles(studMask);
        FitSphere(log);
        Rs = R * 1.09;

        // Body: smooth shading from body pixels away from studs, plus the painted texture where it is far from any stud.
        var hole = Dilate(studMask, 9);
        var bodyCol = new float[n * 3]; var known = new bool[n];
        var distToStud = DistanceTo(studMask, 40);
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                int i = y * N + x;
                if (Dist(x, y) > R - 2) continue;
                float a = Src.A[i];
                if (!hole[i] && a > 0.98f) { bodyCol[i * 3] = Src.R[i] / a; bodyCol[i * 3 + 1] = Src.G[i] / a; bodyCol[i * 3 + 2] = Src.B[i] / a; known[i] = true; }
            }
        var smooth = NormBlur(bodyCol, known, 20);
        shadow = new float[n];
        for (int i = 0; i < n; i++)
        {
            shadow[i] = 1;
            if (studMask[i] || Src.A[i] < 0.98f || distToStud[i] > 34) continue;
            double lum = Lum(Src.R[i] / Src.A[i], Src.G[i] / Src.A[i], Src.B[i] / Src.A[i]), ref0 = Lum(smooth[i * 3], smooth[i * 3 + 1], smooth[i * 3 + 2]);
            if (ref0 > 1e-3 && lum < ref0 * 0.95) shadow[i] = (float)Math.Max(0.30, lum / ref0);
        }
        Body = new Img(N, N);
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                int i = y * N + x; double d = Dist(x, y);
                float cov = (float)Math.Max(0, Math.Min(1, R + 0.5 - d));
                if (cov <= 0) continue;
                float r = smooth[i * 3], g = smooth[i * 3 + 1], b = smooth[i * 3 + 2];
                if (known[i])
                {
                    float w = (float)Curve.Smooth(12, 30, distToStud[i]);
                    r += (bodyCol[i * 3] - smooth[i * 3]) * w; g += (bodyCol[i * 3 + 1] - smooth[i * 3 + 1]) * w; b += (bodyCol[i * 3 + 2] - smooth[i * 3 + 2]) * w;
                }
                // Keep the original rim tint where the source body reaches the silhouette.
                if (d > R - 2.5 && Src.A[i] > 0.05f && !hole[i]) { float a = Src.A[i]; r = Src.R[i] / a; g = Src.G[i] / a; b = Src.B[i] / a; }
                Body.R[i] = r * cov; Body.G[i] = g * cov; Body.B[i] = b * cov; Body.A[i] = cov;
            }

        // Stud layer (original stud pixels, feathered) used for decal extraction.
        StudLayer = new Img(N, N);
        var studSoft = Feather(Dilate(studMask, 1), 1.2);
        for (int i = 0; i < n; i++)
        {
            float w = studSoft[i]; if (w <= 0) continue;
            int x = i % N, y = i / N; float k = Dist(x, y) > R ? 1 : w;
            StudLayer.R[i] = Src.R[i] * k; StudLayer.G[i] = Src.G[i] * k; StudLayer.B[i] = Src.B[i] * k; StudLayer.A[i] = Src.A[i] * k;
        }

        // Decal patches.
        foreach (var s in Studs) Place(s);
        var own = new Dictionary<string, Patch>();
        foreach (var s in Studs) if (s.Own) own[s.Name] = Extract(s.Lon, s.Lat);
        var templates = new Dictionary<string, Patch>();
        templates["orange"] = own["orange-centre-top"];
        templates["blue"] = own["blue-left"];
        templates["blue2"] = own["blue-lower-right"];
        templates["red"] = own["red-centre"];
        templates["yellow"] = own["yellow-lower-left"];
        templates["green"] = Recolour(own["blue-left"], 107, 0.86, 0.66);
        templates["magenta"] = Recolour(own["red-centre"], 304, 0.80, 0.84);

        studTex = new Img[ShellRadius.Length]; for (int L = 0; L < studTex.Length; L++) studTex[L] = new Img(TW, TH);
        shadowTex = new float[TW * TH];
        for (int i = 0; i < TW * TH; i++) shadowTex[i] = 1;
        foreach (var s in Studs)
        {
            Paint(s.Own ? own[s.Name] : templates[s.Class], s.Lon, s.Lat);
            Paint(templates[BackClass[s.Class]], s.Lon + Math.PI, s.Lat);
        }

        if (!string.IsNullOrEmpty(debugDir))
        {
            Body.Save(Path.Combine(debugDir, "rig_body.png"));
            studTex[0].Save(Path.Combine(debugDir, "rig_studtex.png"));
            var sh = new Img(TW, TH); for (int i = 0; i < TW * TH; i++) { float v = 1 - shadowTex[i]; sh.R[i] = sh.G[i] = sh.B[i] = v; sh.A[i] = 1; }
            sh.Save(Path.Combine(debugDir, "rig_shadowtex.png"));
        }
        log.AppendLine(string.Format("sphere ({0:0.00},{1:0.00}) R {2:0.00} shell {3:0.00}", Cx, Cy, R, Rs));
        foreach (var s in Studs) log.AppendLine(string.Format("  {0,-26} lon {1,7:0.0} lat {2,6:0.0} {3}", s.Name, s.Lon * 180 / Math.PI, s.Lat * 180 / Math.PI, s.Own ? "own" : s.Class));
        return log.ToString();
    }

    static double Dist(double x, double y) { return Math.Sqrt((x + 0.5 - Cx) * (x + 0.5 - Cx) + (y + 0.5 - Cy) * (y + 0.5 - Cy)); }
    static double Lum(double r, double g, double b) { return 0.3 * r + 0.59 * g + 0.11 * b; }

    static void Place(Stud s)
    {
        double X = (s.X - Cx) / Rs, Y = -(s.Y - Cy) / Rs, d = Math.Sqrt(X * X + Y * Y), Z;
        if (d > 0.93)
        {
            // Edge stud: on the limb, slightly behind it along its image-plane direction.
            double psi = 86 * Math.PI / 180;
            X = Math.Sin(psi) * X / d; Y = Math.Sin(psi) * Y / d; Z = Math.Cos(psi);
        }
        else Z = Math.Sqrt(1 - d * d);
        s.Lon = Math.Atan2(X, Z); s.Lat = Math.Asin(Math.Max(-1, Math.Min(1, Y)));
    }

    static void Frame(double lon, double lat, out double[] c, out double[] e, out double[] nn)
    {
        c = new[] { Math.Cos(lat) * Math.Sin(lon), Math.Sin(lat), Math.Cos(lat) * Math.Cos(lon) };
        e = new[] { Math.Cos(lon), 0.0, -Math.Sin(lon) };
        nn = new[] { -Math.Sin(lat) * Math.Sin(lon), Math.Cos(lat), -Math.Sin(lat) * Math.Cos(lon) };
    }

    static Patch Extract(double lon, double lat)
    {
        double[] c, e, nn; Frame(lon, lat, out c, out e, out nn);
        var p = new Patch { Col = new Img(PS, PS), Shadow = new float[PS * PS] };
        for (int j = 0; j < PS; j++)
            for (int i = 0; i < PS; i++)
            {
                double u = ((i + 0.5) / PS * 2 - 1) * PR, v = (1 - (j + 0.5) / PS * 2) * PR;
                double dx = c[0] + u * e[0] + v * nn[0], dy = c[1] + u * e[1] + v * nn[1], dz = c[2] + u * e[2] + v * nn[2];
                double l = Math.Sqrt(dx * dx + dy * dy + dz * dz); dx /= l; dy /= l; dz /= l;
                int k = j * PS + i;
                p.Shadow[k] = 1;
                if (dz <= 0.02) continue;
                float r, g, b, a; StudLayer.Sample(Cx + Rs * dx, Cy - Rs * dy, out r, out g, out b, out a);
                // Keep only the stud centred in this patch: fade out beyond ~0.62 of the patch radius.
                float fade = (float)Curve.Smooth(0.86, 0.62, Math.Sqrt(u * u + v * v) / PR);
                p.Col.R[k] = r * fade; p.Col.G[k] = g * fade; p.Col.B[k] = b * fade; p.Col.A[k] = a * fade;
                int sx = (int)Math.Floor(Cx + R * dx), sy = (int)Math.Floor(Cy - R * dy);
                if (sx >= 0 && sy >= 0 && sx < N && sy < N) p.Shadow[k] = 1 - (1 - shadow[sy * N + sx]) * (float)Curve.Smooth(1.0, 0.75, Math.Sqrt(u * u + v * v) / PR);
            }
        return p;
    }

    static Patch Recolour(Patch src, double hue, double sat, double val)
    {
        var p = new Patch { Col = new Img(PS, PS), Shadow = (float[])src.Shadow.Clone() };
        double ms = 0, mv = 0, wsum = 0;
        for (int k = 0; k < PS * PS; k++)
        {
            float a = src.Col.A[k]; if (a < 0.5f) continue;
            double h, s, v; BallAnalyze.Hsv(src.Col.R[k] / a, src.Col.G[k] / a, src.Col.B[k] / a, out h, out s, out v);
            ms += s * a; mv += v * a; wsum += a;
        }
        ms /= wsum; mv /= wsum;
        for (int k = 0; k < PS * PS; k++)
        {
            float a = src.Col.A[k]; if (a <= 0) continue;
            double h, s, v; BallAnalyze.Hsv(src.Col.R[k] / a, src.Col.G[k] / a, src.Col.B[k] / a, out h, out s, out v);
            double ns = Math.Min(1, s * sat / ms), nv = Math.Min(1, v * val / mv);
            double r, g, b; HsvToRgb(hue, ns, nv, out r, out g, out b);
            p.Col.R[k] = (float)r * a; p.Col.G[k] = (float)g * a; p.Col.B[k] = (float)b * a; p.Col.A[k] = a;
        }
        return p;
    }

    static void HsvToRgb(double h, double s, double v, out double r, out double g, out double b)
    {
        double c = v * s, hh = (h % 360) / 60, x = c * (1 - Math.Abs(hh % 2 - 1)), m = v - c;
        r = g = b = 0;
        if (hh < 1) { r = c; g = x; } else if (hh < 2) { r = x; g = c; } else if (hh < 3) { g = c; b = x; }
        else if (hh < 4) { g = x; b = c; } else if (hh < 5) { r = x; b = c; } else { r = c; b = x; }
        r += m; g += m; b += m;
    }

    // Paint a decal patch into the spherical textures, centred at (lon, lat), composited over what is there.
    static void Paint(Patch p, double lon, double lat)
    {
        for (int L = 0; L < ShellRadius.Length; L++) PaintLayer(p, lon, lat, L);
    }

    static void PaintLayer(Patch p, double lon, double lat, int L)
    {
        double[] c, e, nn; Frame(lon, lat, out c, out e, out nn);
        double pr = PR * ShellScale[L];
        double cosMax = Math.Cos(pr * 1.45);
        for (int ty = 0; ty < TH; ty++)
        {
            double tlat = (0.5 - (ty + 0.5) / TH) * Math.PI, cl = Math.Cos(tlat), sl = Math.Sin(tlat);
            for (int tx = 0; tx < TW; tx++)
            {
                double tlon = ((tx + 0.5) / TW - 0.5) * 2 * Math.PI;
                double dx = cl * Math.Sin(tlon), dy = sl, dz = cl * Math.Cos(tlon);
                double dot = dx * c[0] + dy * c[1] + dz * c[2];
                if (dot < cosMax) continue;
                double u = (dx * e[0] + dy * e[1] + dz * e[2]) / dot, v = (dx * nn[0] + dy * nn[1] + dz * nn[2]) / dot;
                double pi = (u / pr + 1) / 2 * PS, pj = (1 - v / pr) / 2 * PS;
                if (pi < 0 || pj < 0 || pi >= PS || pj >= PS) continue;
                float r, g, b, a; p.Col.Sample(pi, pj, out r, out g, out b, out a);
                int k = ty * TW + tx;
                if (a > 0) studTex[L].Over(k, r * ShellTint[L], g * ShellTint[L], b * ShellTint[L], a);
                if (L != 0) continue;
                int si = Math.Min(PS - 1, (int)pi), sj = Math.Min(PS - 1, (int)pj);
                shadowTex[k] *= p.Shadow[sj * PS + si];
            }
        }
    }

    static void FitSphere(StringBuilder log)
    {
        var xs = new List<double>(); var ys = new List<double>();
        for (int y = 1; y < N - 1; y++)
            for (int x = 1; x < N - 1; x++)
            {
                int i = y * N + x; if (Src.A[i] < 0.5f || studMask[i]) continue;
                bool edge = Src.A[i - 1] < 0.5f || Src.A[i + 1] < 0.5f || Src.A[i - N] < 0.5f || Src.A[i + N] < 0.5f;
                if (edge) { xs.Add(x + 0.5); ys.Add(y + 0.5); }
            }
        double cx, cy, r; Circle(xs, ys, out cx, out cy, out r);
        for (int round = 0; round < 4; round++)
        {
            var nx = new List<double>(); var ny = new List<double>();
            for (int k = 0; k < xs.Count; k++) { double d = Math.Sqrt((xs[k] - cx) * (xs[k] - cx) + (ys[k] - cy) * (ys[k] - cy)); if (Math.Abs(d - r) < 3) { nx.Add(xs[k]); ny.Add(ys[k]); } }
            xs = nx; ys = ny; Circle(xs, ys, out cx, out cy, out r);
        }
        Cx = cx; Cy = cy; R = r;
        log.AppendLine("sphere fit points " + xs.Count);
    }

    static void Circle(List<double> xs, List<double> ys, out double cx, out double cy, out double r)
    {
        int n = xs.Count; double sx = 0, sy = 0, sxx = 0, syy = 0, sxy = 0, sxz = 0, syz = 0, sz = 0;
        for (int i = 0; i < n; i++) { double x = xs[i], y = ys[i], z = x * x + y * y; sx += x; sy += y; sxx += x * x; syy += y * y; sxy += x * y; sxz += x * z; syz += y * z; sz += z; }
        double[,] M = { { sxx, sxy, sx, sxz }, { sxy, syy, sy, syz }, { sx, sy, n, sz } };
        for (int c = 0; c < 3; c++) for (int rr = c + 1; rr < 3; rr++) { double f = M[rr, c] / M[c, c]; for (int k = c; k < 4; k++) M[rr, k] -= f * M[c, k]; }
        double C = M[2, 3] / M[2, 2], B = (M[1, 3] - M[1, 2] * C) / M[1, 1], A = (M[0, 3] - M[0, 2] * C - M[0, 1] * B) / M[0, 0];
        cx = A / 2; cy = B / 2; r = Math.Sqrt(C + cx * cx + cy * cy);
    }

    static void FillHoles(bool[] mask)
    {
        var outside = new bool[N * N]; var q = new Queue<int>();
        Action<int> push = j => { if (!mask[j] && !outside[j]) { outside[j] = true; q.Enqueue(j); } };
        for (int k = 0; k < N; k++) { push(k); push((N - 1) * N + k); push(k * N); push(k * N + N - 1); }
        while (q.Count > 0)
        {
            int k = q.Dequeue(), kx = k % N, ky = k / N;
            if (kx > 0) push(k - 1); if (kx < N - 1) push(k + 1); if (ky > 0) push(k - N); if (ky < N - 1) push(k + N);
        }
        for (int k = 0; k < N * N; k++) if (!outside[k]) mask[k] = true;
    }

    static bool[] Dilate(bool[] m, int radius)
    {
        var o = new bool[N * N];
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                if (!m[y * N + x]) continue;
                for (int dy = -radius; dy <= radius; dy++)
                {
                    int yy = y + dy; if (yy < 0 || yy >= N) continue;
                    int span = (int)Math.Sqrt(radius * radius - dy * dy);
                    for (int dx = -span; dx <= span; dx++) { int xx = x + dx; if (xx >= 0 && xx < N) o[yy * N + xx] = true; }
                }
            }
        return o;
    }

    // Chamfer-ish distance (px) to the nearest mask pixel, capped.
    static float[] DistanceTo(bool[] m, int cap)
    {
        var d = new float[N * N];
        for (int i = 0; i < N * N; i++) d[i] = m[i] ? 0 : cap;
        for (int pass = 0; pass < 2; pass++)
        {
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    int i = y * N + x; float v = d[i];
                    if (x > 0) v = Math.Min(v, d[i - 1] + 1); if (y > 0) v = Math.Min(v, d[i - N] + 1);
                    if (x > 0 && y > 0) v = Math.Min(v, d[i - N - 1] + 1.414f); if (x < N - 1 && y > 0) v = Math.Min(v, d[i - N + 1] + 1.414f);
                    d[i] = v;
                }
            for (int y = N - 1; y >= 0; y--)
                for (int x = N - 1; x >= 0; x--)
                {
                    int i = y * N + x; float v = d[i];
                    if (x < N - 1) v = Math.Min(v, d[i + 1] + 1); if (y < N - 1) v = Math.Min(v, d[i + N] + 1);
                    if (x < N - 1 && y < N - 1) v = Math.Min(v, d[i + N + 1] + 1.414f); if (x > 0 && y < N - 1) v = Math.Min(v, d[i + N - 1] + 1.414f);
                    d[i] = v;
                }
        }
        return d;
    }

    static float[] Feather(bool[] m, double radius)
    {
        var o = new float[N * N]; int r = (int)Math.Ceiling(radius * 2);
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                double s = 0, w = 0;
                for (int dy = -r; dy <= r; dy++)
                    for (int dx = -r; dx <= r; dx++)
                    {
                        int xx = x + dx, yy = y + dy; if (xx < 0 || yy < 0 || xx >= N || yy >= N) continue;
                        double k = Math.Exp(-(dx * dx + dy * dy) / (2 * radius * radius)); w += k; if (m[yy * N + xx]) s += k;
                    }
                o[y * N + x] = (float)(s / w);
            }
        return o;
    }

    static float[] NormBlur(float[] col, bool[] known, double sigma)
    {
        int n = N * N, rad = (int)Math.Ceiling(sigma * 3);
        var kern = new double[rad * 2 + 1]; for (int i = -rad; i <= rad; i++) kern[i + rad] = Math.Exp(-(i * i) / (2 * sigma * sigma));
        var c1 = new double[n * 3]; var w1 = new double[n];
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                double s0 = 0, s1 = 0, s2 = 0, ww = 0;
                for (int k = -rad; k <= rad; k++) { int xx = x + k; if (xx < 0 || xx >= N) continue; int j = y * N + xx; if (!known[j]) continue; double q = kern[k + rad]; s0 += col[j * 3] * q; s1 += col[j * 3 + 1] * q; s2 += col[j * 3 + 2] * q; ww += q; }
                int i = y * N + x; c1[i * 3] = s0; c1[i * 3 + 1] = s1; c1[i * 3 + 2] = s2; w1[i] = ww;
            }
        var o = new float[n * 3];
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                double s0 = 0, s1 = 0, s2 = 0, ww = 0;
                for (int k = -rad; k <= rad; k++) { int yy = y + k; if (yy < 0 || yy >= N) continue; int j = yy * N + x; double q = kern[k + rad]; s0 += c1[j * 3] * q; s1 += c1[j * 3 + 1] * q; s2 += c1[j * 3 + 2] * q; ww += w1[j] * q; }
                int i = y * N + x; if (ww > 1e-6) { o[i * 3] = (float)(s0 / ww); o[i * 3 + 1] = (float)(s1 / ww); o[i * 3 + 2] = (float)(s2 / ww); }
            }
        return o;
    }

    static void TexSample(int L, double lon, double lat, out float r, out float g, out float b, out float a, out float sh)
    {
        double u = (lon / (2 * Math.PI) + 0.5) * TW - 0.5, v = (0.5 - lat / Math.PI) * TH - 0.5;
        int x0 = (int)Math.Floor(u), y0 = (int)Math.Floor(v); float fx = (float)(u - x0), fy = (float)(v - y0);
        r = g = b = a = 0; sh = 0;
        var t = studTex[L];
        for (int j = 0; j < 2; j++)
            for (int i = 0; i < 2; i++)
            {
                int xi = ((x0 + i) % TW + TW) % TW, yi = Math.Max(0, Math.Min(TH - 1, y0 + j));
                float w = (i == 0 ? 1 - fx : fx) * (j == 0 ? 1 - fy : fy); int k = yi * TW + xi;
                r += t.R[k] * w; g += t.G[k] * w; b += t.B[k] * w; a += t.A[k] * w; sh += shadowTex[k] * w;
            }
    }

    /// <summary>
    /// Render body (with moving stud shadows) and studs as separate premultiplied layers. The studs are spun by angleDeg about
    /// the vertical axis (positive = front surface moves right), averaging blurDeg of rotation over the given samples.
    /// squashX/squashY deform the ball about its centre (a spinning disc is wider and flatter).
    /// </summary>
    public static BallLayers Render(double angleDeg, double blurDeg, int samples, double squashX, double squashY)
    {
        return Render(angleDeg, blurDeg, samples, squashX, squashY, 1.0);
    }

    // protrude 0..1 scales how far the button domes stand off the sphere. While the spin is blurred the buttons lie flat on the
    // surface (protrude 0: one shell, no side walls, no slab above the pole); they grow back out as the spin slows.
    public static BallLayers Render(double angleDeg, double blurDeg, int samples, double squashX, double squashY, double protrude)
    {
        var body = new Img(N, N); var studs = new Img(N, N);
        double th0 = angleDeg * Math.PI / 180, bl = blurDeg * Math.PI / 180;
        if (samples < 1) samples = 1;
        var ths = new double[samples];
        for (int k = 0; k < samples; k++) ths[k] = th0 + (samples == 1 ? 0 : bl * ((k + 0.5) / samples - 0.5));
        double pr = Math.Max(0, Math.Min(1, protrude)); int L0 = pr < 0.02 ? ShellRadius.Length - 1 : 0;
        double rmax = R * (1 + (ShellRadius[ShellRadius.Length - 1] - 1) * pr);
        int x0 = Math.Max(0, (int)(Cx - rmax * squashX) - 2), x1 = Math.Min(N - 1, (int)(Cx + rmax * squashX) + 2);
        int y0 = Math.Max(0, (int)(Cy - rmax * squashY) - 2), y1 = Math.Min(N - 1, (int)(Cy + rmax * squashY) + 2);
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float br = 0, bg = 0, bb = 0, ba = 0, tr = 0, tg = 0, tb = 0, ta = 0;
                for (int ssy = 0; ssy < 2; ssy++)
                    for (int ssx = 0; ssx < 2; ssx++)
                    {
                        double px = (x + (ssx + 0.5) / 2 - Cx) / squashX, py = (y + (ssy + 0.5) / 2 - Cy) / squashY;
                        double d2 = px * px + py * py;
                        float lr = 0, lg = 0, lb = 0, la = 0;
                        if (d2 < (R + 1) * (R + 1))
                        {
                            float r0, g0, b0, a0; Body.Sample(Cx + px, Cy + py, out r0, out g0, out b0, out a0);
                            double lat = Math.Asin(Math.Max(-1, Math.Min(1, -py / R))), cl = Math.Cos(lat);
                            double lon = cl > 1e-6 ? Math.Asin(Math.Max(-1, Math.Min(1, px / (R * cl)))) : 0;
                            double shs = 0;
                            for (int k = 0; k < samples; k++) { float q0, q1, q2, q3, s0; TexSample(0, Wrap(lon - ths[k]), lat, out q0, out q1, out q2, out q3, out s0); shs += s0; }
                            float shade = (float)(shs / samples);
                            br += r0 * shade; bg += g0 * shade; bb += b0 * shade; ba += a0;
                        }
                        for (int L = L0; L < ShellRadius.Length; L++)
                        {
                            double rl = pr < 0.02 ? R : R * (1 + (ShellRadius[L] - 1) * pr);
                            if (d2 >= rl * rl) continue;
                            double lat = Math.Asin(Math.Max(-1, Math.Min(1, -py / rl))), cl = Math.Cos(lat);
                            double lon = cl > 1e-6 ? Math.Asin(Math.Max(-1, Math.Min(1, px / (rl * cl)))) : 0;
                            float ar = 0, ag = 0, ab = 0, aa = 0;
                            for (int k = 0; k < samples; k++) { float r0, g0, b0, a0, s0; TexSample(L, Wrap(lon - ths[k]), lat, out r0, out g0, out b0, out a0, out s0); ar += r0; ag += g0; ab += b0; aa += a0; }
                            float inv = 1f / samples;
                            // Soft limb on each shell silhouette.
                            float edge = (float)Math.Max(0, Math.Min(1, (rl - Math.Sqrt(d2)) / 3.0)); // soft shell edges: stacked shells read as one dome, no 1-px slices
                            // Lower shells fade in with the protrusion, so switching from one flat shell to stacked domes is continuous.
                            float shellW = L == ShellRadius.Length - 1 ? 1f : (float)Math.Min(1.0, pr / 0.35);
                            ar *= inv * edge * shellW; ag *= inv * edge * shellW; ab *= inv * edge * shellW; aa *= inv * edge * shellW;
                            // Upper shells cover lower ones.
                            lr = ar + lr * (1 - aa); lg = ag + lg * (1 - aa); lb = ab + lb * (1 - aa); la = aa + la * (1 - aa);
                        }
                        tr += lr; tg += lg; tb += lb; ta += la;
                    }
                int i = y * N + x;
                body.R[i] = br / 4; body.G[i] = bg / 4; body.B[i] = bb / 4; body.A[i] = ba / 4;
                studs.R[i] = tr / 4; studs.G[i] = tg / 4; studs.B[i] = tb / 4; studs.A[i] = ta / 4;
            }
        return new BallLayers { Body = body, Studs = studs };
    }

    public static Img Composite(BallLayers l)
    {
        var o = l.Body.Clone();
        for (int i = 0; i < N * N; i++) { float a = l.Studs.A[i]; if (a > 0) o.Over(i, l.Studs.R[i], l.Studs.G[i], l.Studs.B[i], a); }
        return o;
    }

    static double Wrap(double a) { while (a < -Math.PI) a += 2 * Math.PI; while (a >= Math.PI) a -= 2 * Math.PI; return a; }
}
