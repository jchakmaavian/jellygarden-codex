using System;
using System.IO;
using System.Text;

// Quality gate for the idle loop: base row, drift, badge flicker, spark path/brightness, loop seam.
public static class IdleCheck
{
    public static string Run(string dir, string prefix, int count)
    {
        var sb = new StringBuilder();
        var frames = new Img[count];
        for (int f = 0; f < count; f++) frames[f] = Img.Load(Path.Combine(dir, string.Format("{0}_{1:000}.png", prefix, f + 1)));
        int N = frames[0].W;

        int[] baseRow = new int[count]; int[] topRow = new int[count];
        double[] cx = new double[count], cy = new double[count], badge = new double[count], sparkI = new double[count], spx = new double[count], spy = new double[count];
        double[] diffNext = new double[count];
        for (int f = 0; f < count; f++)
        {
            var im = frames[f];
            int bRow = -1, tRow = N; double asum = 0, sx = 0, sy = 0;
            for (int i = 0; i < N * N; i++)
            {
                float a = im.A[i]; if (a < 0.5f) continue;
                int x = i % N, y = i / N;
                if (y > bRow) bRow = y; if (y < tRow) tRow = y;
                asum += a; sx += x * a; sy += y * a;
            }
            baseRow[f] = bRow; topRow[f] = tRow; cx[f] = sx / asum; cy[f] = sy / asum;
            // crown badge brightness (box inside the badge, output coords)
            double lum = 0; int n = 0;
            for (int y = 345; y < 415; y++) for (int x = 200; x < 312; x++) { int i = y * N + x; lum += 0.3 * im.R[i] + 0.6 * im.G[i] + 0.1 * im.B[i]; n++; }
            badge[f] = lum / n;
            // spark: bright near-white pixels in the fuse-tip region
            double si = 0, wx = 0, wy = 0;
            for (int y = 110; y < 260; y++) for (int x = 290; x < 430; x++)
            {
                int i = y * N + x; float a = im.A[i]; if (a < 0.05f) continue;
                double l = (0.3 * im.R[i] + 0.6 * im.G[i] + 0.1 * im.B[i]) / a;
                double bl = Math.Max(0, im.B[i] / a - 0.35); // whiteness: spark core is the only near-white area
                if (l < 0.8 || bl <= 0) continue;
                double w = bl * a; si += w; wx += x * w; wy += y * w;
            }
            sparkI[f] = si; spx[f] = si > 0 ? wx / si : 0; spy[f] = si > 0 ? wy / si : 0;
        }
        for (int f = 0; f < count; f++)
        {
            var a = frames[f]; var b = frames[(f + 1) % count]; double d = 0;
            for (int i = 0; i < N * N; i++) d += Math.Abs(a.R[i] - b.R[i]) + Math.Abs(a.G[i] - b.G[i]) + Math.Abs(a.B[i] - b.B[i]) + Math.Abs(a.A[i] - b.A[i]);
            diffNext[f] = d / (N * N);
        }

        int bMin = int.MaxValue, bMax = int.MinValue; foreach (int r in baseRow) { bMin = Math.Min(bMin, r); bMax = Math.Max(bMax, r); }
        int tMin = int.MaxValue, tMax = int.MinValue; foreach (int r in topRow) { tMin = Math.Min(tMin, r); tMax = Math.Max(tMax, r); }
        double cxMin = 1e9, cxMax = -1e9, cyMin = 1e9, cyMax = -1e9;
        for (int f = 0; f < count; f++) { cxMin = Math.Min(cxMin, cx[f]); cxMax = Math.Max(cxMax, cx[f]); cyMin = Math.Min(cyMin, cy[f]); cyMax = Math.Max(cyMax, cy[f]); }
        sb.AppendLine("base row (lowest row alpha>0.5): min " + bMin + " max " + bMax + (bMin == bMax ? "  -> identical in every frame" : "  -> VARIES"));
        sb.AppendLine("top row range: " + tMin + ".." + tMax + " (breath + head bob + fuse)");
        sb.AppendLine("alpha centroid range: x " + cxMin.ToString("0.00") + ".." + cxMax.ToString("0.00") + ", y " + cyMin.ToString("0.00") + ".." + cyMax.ToString("0.00"));
        sb.AppendLine("centroid start vs end of loop: dx " + (cx[count - 1] - cx[0]).ToString("0.000") + ", dy " + (cy[count - 1] - cy[0]).ToString("0.000"));

        double bdMax = 0, bMinL = 1e9, bMaxL = -1e9;
        for (int f = 0; f < count; f++) { bdMax = Math.Max(bdMax, Math.Abs(badge[(f + 1) % count] - badge[f])); bMinL = Math.Min(bMinL, badge[f]); bMaxL = Math.Max(bMaxL, badge[f]); }
        sb.AppendLine("crown badge mean brightness: range " + bMinL.ToString("0.0000") + ".." + bMaxL.ToString("0.0000") + ", max frame-to-frame change " + bdMax.ToString("0.0000") + " (incl. 120->1)");

        double spStep = 0; int spStepAt = 0, spMinF = 0, spMaxF = 0;
        for (int f = 0; f < count; f++)
        {
            int g = (f + 1) % count;
            if (sparkI[f] > 0 && sparkI[g] > 0) { double st = Math.Sqrt(Math.Pow(spx[g] - spx[f], 2) + Math.Pow(spy[g] - spy[f], 2)); if (st > spStep) { spStep = st; spStepAt = f + 1; } }
            if (sparkI[f] < sparkI[spMinF]) spMinF = f; if (sparkI[f] > sparkI[spMaxF]) spMaxF = f;
        }
        sb.AppendLine("spark core path: largest step between consecutive frames " + spStep.ToString("0.00") + " px (frame " + spStepAt + " -> " + (spStepAt % count + 1) + ")");
        sb.AppendLine("spark core intensity: min " + sparkI[spMinF].ToString("0") + " (frame " + (spMinF + 1) + "), max " + sparkI[spMaxF].ToString("0") + " (frame " + (spMaxF + 1) + ")");
        double popMax = 0; int popAt = 0;
        for (int f = 0; f < count; f++)
        {
            int p = (f - 1 + count) % count, g = (f + 1) % count;
            double expected = (sparkI[p] + sparkI[g]) / 2, dev = Math.Abs(sparkI[f] - expected) / Math.Max(1, Math.Max(sparkI[p], sparkI[g]));
            if (dev > popMax) { popMax = dev; popAt = f + 1; }
        }
        sb.AppendLine("spark intensity worst single-frame deviation from neighbours: " + (popMax * 100).ToString("0.0") + "% (frame " + popAt + ")");

        var sorted = (double[])diffNext.Clone(); Array.Sort(sorted);
        double median = sorted[count / 2], p95 = sorted[(int)(count * 0.95)], maxD = sorted[count - 1];
        sb.AppendLine("frame-to-frame change: median " + median.ToString("0.00000") + ", p95 " + p95.ToString("0.00000") + ", max " + maxD.ToString("0.00000"));
        sb.AppendLine("loop seam 120->1 change: " + diffNext[count - 1].ToString("0.00000") + "   (neighbours 119->120: " + diffNext[count - 2].ToString("0.00000") + ", 1->2: " + diffNext[0].ToString("0.00000") + ")");
        int worst = 0; for (int f = 0; f < count; f++) if (diffNext[f] > diffNext[worst]) worst = f;
        sb.AppendLine("largest change is between frame " + (worst + 1) + " and " + ((worst + 1) % count + 1));
        return sb.ToString();
    }
}
