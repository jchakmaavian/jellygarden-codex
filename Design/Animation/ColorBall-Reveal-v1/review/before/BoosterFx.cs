// Shared rendering core for the Royal Throne booster animation sequences.
// C# 5 (compiled by Windows PowerShell Add-Type with System.Drawing).
// Images are premultiplied RGBA floats in 0..1. Canvas coordinates: pixel (x, y) covers [x, x+1) x [y, y+1), y down.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

public sealed class Img
{
    public int W, H;
    public float[] R, G, B, A;

    public Img(int w, int h) { W = w; H = h; R = new float[w * h]; G = new float[w * h]; B = new float[w * h]; A = new float[w * h]; }

    public Img Clone()
    {
        var c = new Img(W, H);
        Array.Copy(R, c.R, R.Length); Array.Copy(G, c.G, G.Length); Array.Copy(B, c.B, B.Length); Array.Copy(A, c.A, A.Length);
        return c;
    }

    public static Img Load(string path)
    {
        using (var bmp = new Bitmap(path))
        {
            int w = bmp.Width, h = bmp.Height;
            var img = new Img(w, h);
            var data = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            var px = new byte[w * h * 4];
            for (int y = 0; y < h; y++) Marshal.Copy(IntPtr.Add(data.Scan0, y * data.Stride), px, y * w * 4, w * 4);
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
                float a = Clamp01(A[i]);
                float r = 0, g = 0, b = 0;
                if (a > 1e-5f) { r = Math.Min(1f, R[i] / a); g = Math.Min(1f, G[i] / a); b = Math.Min(1f, B[i] / a); }
                // Fully transparent texels carry no colour, so encoders cannot leak stale RGB.
                if (ToByte(a) == 0) { r = g = b = 0; }
                px[i * 4] = ToByte(b); px[i * 4 + 1] = ToByte(g); px[i * 4 + 2] = ToByte(r); px[i * 4 + 3] = ToByte(a);
            }
            for (int y = 0; y < H; y++) Marshal.Copy(px, y * W * 4, IntPtr.Add(data.Scan0, y * data.Stride), W * 4);
            bmp.UnlockBits(data);
            bmp.Save(path, ImageFormat.Png);
        }
    }

    public static float Clamp01(float v) { return v < 0 ? 0 : v > 1 ? 1 : v; }
    static byte ToByte(float v) { int x = (int)Math.Round(v * 255f); return (byte)(x < 0 ? 0 : x > 255 ? 255 : x); }

    // Bilinear sample, pixel centres at integer + 0.5, transparent outside.
    public void Sample(double x, double y, out float r, out float g, out float b, out float a)
    {
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

    // Composite a premultiplied colour over pixel i.
    public void Over(int i, float r, float g, float b, float a)
    {
        float k = 1 - a;
        R[i] = r + R[i] * k; G[i] = g + G[i] * k; B[i] = b + B[i] * k; A[i] = a + A[i] * k;
    }

    // Additive light: adds premultiplied colour; alpha grows toward the light's own coverage.
    public void Add(int i, float r, float g, float b, float coverage)
    {
        R[i] += r; G[i] += g; B[i] += b;
        A[i] = A[i] + coverage * (1 - A[i]);
        // Keep premultiplied colour representable: light brighter than its alpha raises alpha.
        float m = Math.Max(R[i], Math.Max(G[i], B[i]));
        if (m > A[i]) A[i] = Math.Min(1f, m);
        if (R[i] > 1) R[i] = 1; if (G[i] > 1) G[i] = 1; if (B[i] > 1) B[i] = 1;
    }
}

public struct Affine
{
    // Maps layer (source) coordinates to canvas coordinates: X = a*x + b*y + c, Y = d*x + e*y + f.
    public double a, b, c, d, e, f;

    public static Affine Identity { get { return new Affine { a = 1, e = 1 }; } }

    // Scale (sx, sy) about a pivot in layer space, rotate by deg (clockwise on screen, y down), then place the pivot at (tx, ty).
    public static Affine Pose(double pivotX, double pivotY, double sx, double sy, double deg, double tx, double ty)
    {
        double rad = deg * Math.PI / 180, cs = Math.Cos(rad), sn = Math.Sin(rad);
        var m = new Affine();
        m.a = cs * sx; m.b = -sn * sy; m.d = sn * sx; m.e = cs * sy;
        m.c = tx - (m.a * pivotX + m.b * pivotY);
        m.f = ty - (m.d * pivotX + m.e * pivotY);
        return m;
    }

    public Affine Then(Affine o)
    {
        // Apply this, then o.
        return new Affine
        {
            a = o.a * a + o.b * d, b = o.a * b + o.b * e, c = o.a * c + o.b * f + o.c,
            d = o.d * a + o.e * d, e = o.d * b + o.e * e, f = o.d * c + o.e * f + o.f
        };
    }

    public bool Invert(out Affine inv)
    {
        double det = a * e - b * d; inv = new Affine();
        if (Math.Abs(det) < 1e-12) return false;
        inv.a = e / det; inv.b = -b / det; inv.d = -d / det; inv.e = a / det;
        inv.c = -(inv.a * c + inv.b * f); inv.f = -(inv.d * c + inv.e * f);
        return true;
    }

    public void Apply(double x, double y, out double X, out double Y) { X = a * x + b * y + c; Y = d * x + e * y + f; }
}

public static class Curve
{
    // Monotone cubic (Fritsch-Carlson) through (t, v) keys: no overshoot between keys.
    public static double Eval(double[] keys, double t)
    {
        int n = keys.Length / 2;
        if (t <= keys[0]) return keys[1];
        if (t >= keys[(n - 1) * 2]) return keys[(n - 1) * 2 + 1];
        var xs = new double[n]; var ys = new double[n];
        for (int i = 0; i < n; i++) { xs[i] = keys[i * 2]; ys[i] = keys[i * 2 + 1]; }
        var m = Slopes(xs, ys, false, 0);
        int k = 0;
        while (k < n - 2 && t > xs[k + 1]) k++;
        return Hermite(xs[k], xs[k + 1], ys[k], ys[k + 1], m[k], m[k + 1], t);
    }

    // Periodic monotone cubic: keys span [0, period); value and slope wrap seamlessly.
    public static double CycEval(double[] keys, double t, double period)
    {
        int n = keys.Length / 2;
        t = ((t % period) + period) % period;
        var xs = new double[n + 2]; var ys = new double[n + 2];
        xs[0] = keys[(n - 1) * 2] - period; ys[0] = keys[(n - 1) * 2 + 1];
        for (int i = 0; i < n; i++) { xs[i + 1] = keys[i * 2]; ys[i + 1] = keys[i * 2 + 1]; }
        xs[n + 1] = keys[0] + period; ys[n + 1] = keys[1];
        var m = Slopes(xs, ys, false, 0);
        int k = 0;
        while (k < n && t > xs[k + 1]) k++;
        return Hermite(xs[k], xs[k + 1], ys[k], ys[k + 1], m[k], m[k + 1], t);
    }

    static double[] Slopes(double[] xs, double[] ys, bool unused, int unused2)
    {
        int n = xs.Length;
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
        return m;
    }

    static double Hermite(double x0, double x1, double y0, double y1, double m0, double m1, double t)
    {
        double h = x1 - x0, u = (t - x0) / h;
        double h00 = 2 * u * u * u - 3 * u * u + 1, h10 = u * u * u - 2 * u * u + u, h01 = -2 * u * u * u + 3 * u * u, h11 = u * u * u - u * u;
        return h00 * y0 + h10 * h * m0 + h01 * y1 + h11 * h * m1;
    }

    public static double Smooth(double e0, double e1, double x)
    {
        double t = (x - e0) / (e1 - e0); t = t < 0 ? 0 : t > 1 ? 1 : t;
        return t * t * (3 - 2 * t);
    }
}

// Deterministic hash noise so every render of a frame is identical.
public static class Hash
{
    public static double U(int seed, int k)
    {
        unchecked
        {
            uint h = (uint)seed * 374761393u + (uint)k * 668265263u;
            h = (h ^ (h >> 13)) * 1274126177u;
            h ^= h >> 16;
            return (h & 0xFFFFFF) / 16777216.0;
        }
    }
}

public static class Fx
{
    // Draw a layer through an affine pose, supersampled ss x ss per canvas pixel, clipped to its transformed bounds.
    public static void DrawLayer(Img dst, Img layer, Affine m, float opacity, int ss, float tintR = 1, float tintG = 1, float tintB = 1)
    {
        Affine inv; if (!m.Invert(out inv) || opacity <= 0) return;
        double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
        foreach (var p in new[] { new[] { 0.0, 0.0 }, new[] { (double)layer.W, 0.0 }, new[] { 0.0, (double)layer.H }, new[] { (double)layer.W, (double)layer.H } })
        {
            double X, Y; m.Apply(p[0], p[1], out X, out Y);
            minX = Math.Min(minX, X); minY = Math.Min(minY, Y); maxX = Math.Max(maxX, X); maxY = Math.Max(maxY, Y);
        }
        int x0 = Math.Max(0, (int)Math.Floor(minX) - 1), x1 = Math.Min(dst.W - 1, (int)Math.Ceiling(maxX) + 1);
        int y0 = Math.Max(0, (int)Math.Floor(minY) - 1), y1 = Math.Min(dst.H - 1, (int)Math.Ceiling(maxY) + 1);
        float inv2 = 1f / (ss * ss);
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float sr = 0, sg = 0, sb = 0, sa = 0;
                for (int j = 0; j < ss; j++)
                    for (int i = 0; i < ss; i++)
                    {
                        double cx = x + (i + 0.5) / ss, cy = y + (j + 0.5) / ss, lx, ly;
                        inv.Apply(cx, cy, out lx, out ly);
                        float r, g, b, a; layer.Sample(lx, ly, out r, out g, out b, out a);
                        sr += r; sg += g; sb += b; sa += a;
                    }
                if (sa <= 0) continue;
                int k = y * dst.W + x;
                dst.Over(k, sr * inv2 * opacity * tintR, sg * inv2 * opacity * tintG, sb * inv2 * opacity * tintB, sa * inv2 * opacity);
            }
    }

    // White -> yellow -> gold ramp by heat (1 = white core, 0 = deep gold edge).
    public static void Heat(double t, out float r, out float g, out float b)
    {
        t = t < 0 ? 0 : t > 1 ? 1 : t;
        if (t > 0.6) { double u = (t - 0.6) / 0.4; r = 1; g = (float)(0.93 + 0.07 * u); b = (float)(0.55 + 0.45 * u); }
        else if (t > 0.25) { double u = (t - 0.25) / 0.35; r = 1; g = (float)(0.74 + 0.19 * u); b = (float)(0.16 + 0.39 * u); }
        else { double u = t / 0.25; r = (float)(0.96 + 0.04 * u); g = (float)(0.56 + 0.18 * u); b = (float)(0.05 + 0.11 * u); }
    }

    // Soft additive glow: gaussian falloff with a heat ramp from the white core outward.
    public static void Glow(Img dst, double cx, double cy, double radius, double intensity, double coreHeat = 1)
    {
        if (intensity <= 0 || radius <= 0) return;
        int R = (int)Math.Ceiling(radius * 2.2);
        for (int y = Math.Max(0, (int)cy - R); y < Math.Min(dst.H, (int)cy + R + 1); y++)
            for (int x = Math.Max(0, (int)cx - R); x < Math.Min(dst.W, (int)cx + R + 1); x++)
            {
                double dx = x + 0.5 - cx, dy = y + 0.5 - cy, q = (dx * dx + dy * dy) / (radius * radius);
                double w = Math.Exp(-q * 2.2) * intensity;
                if (w < 0.002) continue;
                float r, g, b; Heat(coreHeat * Math.Exp(-q * 1.3), out r, out g, out b);
                float cov = (float)Math.Min(1, w);
                dst.Add(y * dst.W + x, (float)(r * w) * 0.9f, (float)(g * w) * 0.9f, (float)(b * w) * 0.9f, cov * 0.9f);
            }
    }

    // Crisp four-point star with a soft luminous centre, rotated by deg; size is the tip-to-centre radius.
    public static void Star(Img dst, double cx, double cy, double size, double deg, double intensity, double heat = 0.85)
    {
        if (intensity <= 0 || size <= 0.3) return;
        double rad = deg * Math.PI / 180, cs = Math.Cos(rad), sn = Math.Sin(rad);
        int R = (int)Math.Ceiling(size * 1.25) + 1;
        float cr, cg, cb; Heat(heat, out cr, out cg, out cb);
        for (int y = Math.Max(0, (int)cy - R); y < Math.Min(dst.H, (int)cy + R + 1); y++)
            for (int x = Math.Max(0, (int)cx - R); x < Math.Min(dst.W, (int)cx + R + 1); x++)
            {
                double acc = 0;
                for (int j = 0; j < 2; j++)
                    for (int i = 0; i < 2; i++)
                    {
                        double dx = x + (i + 0.5) / 2 - cx, dy = y + (j + 0.5) / 2 - cy;
                        double u = (dx * cs + dy * sn) / size, v = (-dx * sn + dy * cs) / size;
                        double diamond = Math.Pow(Math.Abs(u), 0.45) + Math.Pow(Math.Abs(v), 0.45);
                        double a = Curve.Smooth(1.15, 0.70, diamond) + Math.Exp(-(u * u + v * v) * 22) * 0.45;
                        acc += Math.Min(1, a);
                    }
                double w = acc / 4 * intensity;
                if (w < 0.003) continue;
                // White-hot centre, warm tips.
                double dxc = x + 0.5 - cx, dyc = y + 0.5 - cy, rr = Math.Sqrt(dxc * dxc + dyc * dyc) / size;
                float r, g, b; Heat(Math.Max(heat, 1 - rr * 0.8), out r, out g, out b);
                dst.Add(y * dst.W + x, (float)(r * w), (float)(g * w), (float)(b * w), (float)Math.Min(1, w));
            }
    }

    // Radial light rays: count rays, gaussian angular width, fading with radius.
    public static void Rays(Img dst, double cx, double cy, int count, double length, double phaseDeg, double widthDeg, double intensity, double inner = 0)
    {
        if (intensity <= 0 || length <= 0) return;
        int R = (int)Math.Ceiling(length) + 1;
        double half = Math.PI / count, sigma = widthDeg * Math.PI / 180;
        for (int y = Math.Max(0, (int)cy - R); y < Math.Min(dst.H, (int)cy + R + 1); y++)
            for (int x = Math.Max(0, (int)cx - R); x < Math.Min(dst.W, (int)cx + R + 1); x++)
            {
                double dx = x + 0.5 - cx, dy = y + 0.5 - cy, r = Math.Sqrt(dx * dx + dy * dy);
                if (r > length || r < inner) continue;
                double ang = Math.Atan2(dy, dx) - phaseDeg * Math.PI / 180;
                double sector = 2 * Math.PI / count;
                double off = ((ang % sector) + sector) % sector; if (off > half) off -= sector;
                // Alternate long and short rays for a less mechanical burst.
                int idx = (int)Math.Floor((ang + half) / sector);
                double len = (((idx % 2) + 2) % 2 == 0) ? 1.0 : 0.62;
                double rr = r / (length * len);
                if (rr > 1) continue;
                double w = Math.Exp(-(off * off) / (2 * sigma * sigma * (0.35 + rr))) * Math.Pow(1 - rr, 1.6) * Curve.Smooth(inner, inner + length * 0.08, r) * intensity;
                if (w < 0.003) continue;
                float cr, cg, cb; Heat(1 - rr * 0.9, out cr, out cg, out cb);
                dst.Add(y * dst.W + x, (float)(cr * w), (float)(cg * w), (float)(cb * w), (float)Math.Min(1, w));
            }
    }

    // Thin soft ring.
    public static void Ring(Img dst, double cx, double cy, double radius, double thickness, double intensity, double heat = 0.9)
    {
        if (intensity <= 0 || radius <= 0) return;
        int R = (int)Math.Ceiling(radius + thickness * 3) + 1;
        for (int y = Math.Max(0, (int)cy - R); y < Math.Min(dst.H, (int)cy + R + 1); y++)
            for (int x = Math.Max(0, (int)cx - R); x < Math.Min(dst.W, (int)cx + R + 1); x++)
            {
                double dx = x + 0.5 - cx, dy = y + 0.5 - cy, d = Math.Sqrt(dx * dx + dy * dy) - radius;
                double w = Math.Exp(-(d * d) / (2 * thickness * thickness)) * intensity;
                if (w < 0.003) continue;
                float r, g, b; Heat(heat, out r, out g, out b);
                dst.Add(y * dst.W + x, (float)(r * w), (float)(g * w), (float)(b * w), (float)Math.Min(1, w));
            }
    }

    // Tapered energy streak from tail (x0,y0) to head (x1,y1): white-hot core, gold edges, fading toward the tail.
    public static void Streak(Img dst, double x0, double y0, double x1, double y1, double headWidth, double intensity)
    {
        if (intensity <= 0) return;
        double vx = x1 - x0, vy = y1 - y0, len = Math.Sqrt(vx * vx + vy * vy);
        if (len < 0.5) return;
        vx /= len; vy /= len;
        int pad = (int)Math.Ceiling(headWidth * 2) + 1;
        int xa = Math.Max(0, (int)Math.Min(x0, x1) - pad), xb = Math.Min(dst.W - 1, (int)Math.Max(x0, x1) + pad);
        int ya = Math.Max(0, (int)Math.Min(y0, y1) - pad), yb = Math.Min(dst.H - 1, (int)Math.Max(y0, y1) + pad);
        for (int y = ya; y <= yb; y++)
            for (int x = xa; x <= xb; x++)
            {
                double px = x + 0.5 - x0, py = y + 0.5 - y0, along = px * vx + py * vy, across = -px * vy + py * vx;
                if (along < -headWidth || along > len + headWidth) continue;
                double u = Math.Max(0, Math.Min(1, along / len));
                double width = headWidth * (0.15 + 0.85 * u);
                double capped = along > len ? (along - len) : 0;
                double d2 = across * across + capped * capped;
                double w = Math.Exp(-d2 / (2 * width * width * 0.45)) * Math.Pow(u, 1.4) * intensity;
                if (w < 0.003) continue;
                double core = Math.Exp(-d2 / (2 * width * width * 0.08));
                float r, g, b; Heat(0.35 + 0.65 * core * u, out r, out g, out b);
                dst.Add(y * dst.W + x, (float)(r * w), (float)(g * w), (float)(b * w), (float)Math.Min(1, w));
            }
    }

    // Small round spark.
    public static void Spark(Img dst, double cx, double cy, double radius, double intensity, double heat = 0.95)
    {
        Glow(dst, cx, cy, radius, intensity, heat);
    }
}

// Per-frame quality metrics shared by the three sequences.
public static class SeqMetrics
{
    // Mean absolute premultiplied difference per channel over the canvas.
    public static double Diff(Img a, Img b)
    {
        double d = 0; int n = a.W * a.H;
        for (int i = 0; i < n; i++) d += Math.Abs(a.R[i] - b.R[i]) + Math.Abs(a.G[i] - b.G[i]) + Math.Abs(a.B[i] - b.B[i]) + Math.Abs(a.A[i] - b.A[i]);
        return d / (n * 4.0);
    }

    // Alpha-weighted centroid and lowest opaque row (alpha > 0.5).
    public static void Centroid(Img im, out double cx, out double cy, out int baseRow)
    {
        double s = 0, sx = 0, sy = 0; baseRow = -1;
        for (int y = 0; y < im.H; y++)
            for (int x = 0; x < im.W; x++)
            {
                float a = im.A[y * im.W + x];
                if (a > 0.5f && y > baseRow) baseRow = y;
                s += a; sx += a * x; sy += a * y;
            }
        cx = s > 0 ? sx / s : 0; cy = s > 0 ? sy / s : 0;
    }

    // Count texels with nearly zero alpha but visible colour (dirty edges) and texels whose straight colour is invalid.
    public static int DirtyAlpha(Img im)
    {
        int n = 0;
        for (int i = 0; i < im.W * im.H; i++)
            if (im.A[i] < 1f / 255 && (im.R[i] > 1f / 255 || im.G[i] > 1f / 255 || im.B[i] > 1f / 255)) n++;
        return n;
    }
}
