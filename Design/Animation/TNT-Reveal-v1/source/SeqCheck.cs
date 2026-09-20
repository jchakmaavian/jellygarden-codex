using System;
using System.IO;
using System.Text;

public static class SeqCheck
{
    public static string Run(string dir, int count, string sourcePath, double kc)
    {
        var sb = new StringBuilder();
        sb.AppendLine("frame  diffPrev  alphaSum   bboxX      bboxY     cx      cy     maxJumpPx");
        Img prev = null;
        double[] diffs = new double[count];
        for (int f = 1; f <= count; f++)
        {
            var img = Img.Load(Path.Combine(dir, string.Format("TNT_Reveal_{0:000}.png", f)));
            int n = img.W * img.H;
            double asum = 0, cx = 0, cy = 0; int x0 = 9999, x1 = -1, y0 = 9999, y1 = -1;
            for (int i = 0; i < n; i++)
            {
                float a = img.A[i]; if (a < 0.02f) continue;
                int x = i % img.W, y = i / img.W;
                asum += a; cx += x * a; cy += y * a;
                if (a > 0.5f) { if (x < x0) x0 = x; if (x > x1) x1 = x; if (y < y0) y0 = y; if (y > y1) y1 = y; }
            }
            cx /= Math.Max(1e-6, asum); cy /= Math.Max(1e-6, asum);
            double d = 0, maxPix = 0;
            if (prev != null)
            {
                for (int i = 0; i < n; i++)
                {
                    double e = Math.Abs(img.R[i] - prev.R[i]) + Math.Abs(img.G[i] - prev.G[i]) + Math.Abs(img.B[i] - prev.B[i]) + Math.Abs(img.A[i] - prev.A[i]);
                    d += e; if (e > maxPix) maxPix = e;
                }
                d /= n;
            }
            diffs[f - 1] = d;
            sb.AppendLine(string.Format("{0,5}  {1,8:0.00000}  {2,8:0}  [{3,3}-{4,3}]  [{5,3}-{6,3}]  {7,6:0.0}  {8,6:0.0}  {9,5:0.00}", f, d, asum, x0, x1, y0, y1, cx, cy, maxPix));
            prev = img;
        }
        // spike detection: a frame whose change is > 2.2x the median of its neighbours
        sb.AppendLine();
        for (int f = 3; f < count - 1; f++)
        {
            double a = diffs[f - 2], b = diffs[f - 1], c = diffs[f], dNext = f + 1 < count ? diffs[f + 1] : c;
            double[] nb = { a, dNext };
            double med = Math.Max(1e-5, (a + dNext) / 2);
            if (c > 2.2 * med && c > 0.002) sb.AppendLine("possible pop at frame " + (f + 1) + ": change " + c.ToString("0.0000") + " vs neighbours ~" + med.ToString("0.0000"));
        }
        if (!string.IsNullOrEmpty(sourcePath) && File.Exists(sourcePath))
        {
            var last = Img.Load(Path.Combine(dir, string.Format("TNT_Reveal_{0:000}.png", count)));
            var srcImg = Img.Load(sourcePath);
            var rest = new Img(512, 512);
            double[] offs = { 0.25, 0.75 };
            for (int y = 0; y < 512; y++) for (int x = 0; x < 512; x++)
            {
                float ar = 0, ag = 0, ab = 0, aa = 0;
                foreach (double sy in offs) foreach (double sx in offs)
                { float r, g, b, a; srcImg.Sample(256 + (x + sx - 256) / kc, 458 + (y + sy - 480) / kc, out r, out g, out b, out a); ar += r; ag += g; ab += b; aa += a; }
                int k = y * 512 + x; rest.R[k] = ar / 4; rest.G[k] = ag / 4; rest.B[k] = ab / 4; rest.A[k] = aa / 4;
            }
            rest.Save(Path.Combine(dir, "..", "idle_reference_kc.png"));
            double se = 0; int n = last.W * last.H;
            for (int i = 0; i < n; i++) { double e = Math.Abs(last.R[i] - rest.R[i]) + Math.Abs(last.G[i] - rest.G[i]) + Math.Abs(last.B[i] - rest.B[i]) + Math.Abs(last.A[i] - rest.A[i]); se += e * e; }
            sb.AppendLine("final frame vs idle reference RMSE " + Math.Sqrt(se / n).ToString("0.00000"));
            double scx = 256 + kc * (368 - 256), scy = 480 + kc * (104 - 458), rr2 = Math.Pow(60 * kc + 4, 2);
            double se2 = 0; int n2 = 0;
            for (int i = 0; i < n; i++) { double dx = i % 512 + 0.5 - scx, dy = i / 512 + 0.5 - scy; if (dx * dx + dy * dy < rr2) continue; double e = Math.Abs(last.R[i] - rest.R[i]) + Math.Abs(last.G[i] - rest.G[i]) + Math.Abs(last.B[i] - rest.B[i]) + Math.Abs(last.A[i] - rest.A[i]); se2 += e * e; n2++; }
            sb.AppendLine("final frame vs idle reference RMSE excluding the rebuilt spark " + Math.Sqrt(se2 / n2).ToString("0.00000"));
        }
        return sb.ToString();
    }
}
