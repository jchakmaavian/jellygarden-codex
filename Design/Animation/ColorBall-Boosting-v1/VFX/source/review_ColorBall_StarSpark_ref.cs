// Reviewer-only: house FatStar copied verbatim (from ColorBallSeq.cs CB.FatStar) at the StarSpark canvas scale, for side-by-side comparison.
using System;
using System.IO;
public static class ReviewStarSparkRef
{
    static void FatStar(Img dst, double cx, double cy, double size, double deg, double intensity, double heat)
    {
        double rad = deg * Math.PI / 180, cs = Math.Cos(rad), sn = Math.Sin(rad);
        int R = (int)Math.Ceiling(size * 1.3) + 2;
        for (int y = Math.Max(0, (int)cy - R); y < Math.Min(dst.H, (int)cy + R + 1); y++)
            for (int x = Math.Max(0, (int)cx - R); x < Math.Min(dst.W, (int)cx + R + 1); x++)
            {
                double acc = 0;
                for (int j = 0; j < 3; j++)
                    for (int i = 0; i < 3; i++)
                    {
                        double dx = x + (i + 0.5) / 3 - cx, dy = y + (j + 0.5) / 3 - cy;
                        double u = (dx * cs + dy * sn) / size, v = (-dx * sn + dy * cs) / size;
                        double diamond = Math.Pow(Math.Abs(u), 0.58) + Math.Pow(Math.Abs(v), 0.58);
                        double a = Curve.Smooth(1.08, 0.86, diamond) + Math.Exp(-(u * u + v * v) * 10) * 0.5;
                        acc += Math.Min(1, a);
                    }
                double w = acc / 9 * intensity;
                if (w < 0.003) continue;
                double dxc = x + 0.5 - cx, dyc = y + 0.5 - cy, rr = Math.Sqrt(dxc * dxc + dyc * dyc) / size;
                float r, g, b; Fx.Heat(Math.Max(heat, 1 - rr * 1.1), out r, out g, out b);
                dst.Add(y * dst.W + x, (float)(r * w), (float)(g * w), (float)(b * w), (float)Math.Min(1, w));
            }
    }
    public static void Render(string outDir)
    {
        Directory.CreateDirectory(outDir);
        var a = new Img(128, 128); FatStar(a, 64, 64, 46, 0, 0.95, 0.95); a.Save(Path.Combine(outDir, "ref_fatstar_white.png"));
        var b = new Img(128, 128); FatStar(b, 64, 64, 46, 0, 1.0, 0.45); b.Save(Path.Combine(outDir, "ref_fatstar_gold.png"));
    }
}
