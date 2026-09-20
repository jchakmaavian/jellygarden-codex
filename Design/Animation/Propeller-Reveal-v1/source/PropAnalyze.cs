// Measures the Royal Throne propeller artwork so the rig can pose real painted layers
// instead of redrawing the booster. Reports geometry and writes mask visualisations.
using System;
using System.Collections.Generic;
using System.Text;

public static class PropAnalyze
{
    public enum Part { None, Blue, Gold, Red, Magenta }

    public static Part Classify(float r, float g, float b, float a)
    {
        if (a < .35f) return Part.None;
        float mx = Math.Max(r, Math.Max(g, b)), mn = Math.Min(r, Math.Min(g, b));
        float v = mx, s = mx <= 1e-5f ? 0 : (mx - mn) / mx;
        double h = 0;
        if (mx - mn > 1e-5f)
        {
            if (mx == r) h = 60 * (((g - b) / (mx - mn)) % 6);
            else if (mx == g) h = 60 * ((b - r) / (mx - mn) + 2);
            else h = 60 * ((r - g) / (mx - mn) + 4);
            if (h < 0) h += 360;
        }
        if (s < .18f) return v > .55f ? Part.Gold : Part.Blue;      // near-white highlights sit on gold
        if (h >= 185 && h <= 265) return Part.Blue;
        if (h >= 20 && h < 75) return Part.Gold;
        // The crimson blade tips and the pink hub dome are both magenta-ish; the dome is the
        // more purple of the two, the tips sit closer to pure red.
        if (h >= 305 && h < 342) return Part.Magenta;
        if (h >= 342 || h < 20) return Part.Red;
        if (h >= 265 && h < 305) return Part.Magenta;
        if (h >= 75 && h < 185) return Part.Gold;
        return Part.Blue;
    }

    public static string Report(string artPath, string outDir)
    {
        var art = Img.Load(artPath);
        int w = art.W, h = art.H;
        var part = new Part[w * h];
        int[] count = new int[5];
        int minX = w, minY = h, maxX = -1, maxY = -1;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int i = y * w + x;
                float a = art.A[i];
                float r = a > 1e-5f ? art.R[i] / a : 0, g = a > 1e-5f ? art.G[i] / a : 0, b = a > 1e-5f ? art.B[i] / a : 0;
                var p = Classify(r, g, b, a);
                part[i] = p; count[(int)p]++;
                if (a > .5f) { if (x < minX) minX = x; if (x > maxX) maxX = x; if (y < minY) minY = y; if (y > maxY) maxY = y; }
            }

        // The blue body is the sphere: fit a circle to its outline by least squares on the alpha edge.
        double sx = 0, sy = 0; int n = 0;
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) if (part[y * w + x] == Part.Blue) { sx += x; sy += y; n++; }
        double bcx = n > 0 ? sx / n : 0, bcy = n > 0 ? sy / n : 0;

        // Sphere radius: the widest blue row (the equator) gives the diameter directly.
        int bestRow = -1, bestSpan = 0, bestL = 0, bestR = 0;
        for (int y = 0; y < h; y++)
        {
            int l = -1, r2 = -1;
            for (int x = 0; x < w; x++) if (part[y * w + x] == Part.Blue) { if (l < 0) l = x; r2 = x; }
            if (l >= 0 && r2 - l > bestSpan) { bestSpan = r2 - l; bestRow = y; bestL = l; bestR = r2; }
        }
        double sphereR = bestSpan / 2.0, sphereCx = (bestL + bestR) / 2.0;
        // Bottom of the sphere silhouette on the centre column.
        int bottom = 0; for (int y = h - 1; y >= 0; y--) if (art.A[y * w + (int)Math.Round(sphereCx)] > .5f) { bottom = y; break; }
        double sphereCy = bottom - sphereR;

        // Magenta dome = hub centre.
        double hx = 0, hy = 0; int hn = 0;
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) if (part[y * w + x] == Part.Magenta) { hx += x; hy += y; hn++; }
        double hubCx = hn > 0 ? hx / hn : 0, hubCy = hn > 0 ? hy / hn : 0;
        int hubMinX = w, hubMaxX = -1, hubMinY = h, hubMaxY = -1;
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) if (part[y * w + x] == Part.Magenta)
        { if (x < hubMinX) hubMinX = x; if (x > hubMaxX) hubMaxX = x; if (y < hubMinY) hubMinY = y; if (y > hubMaxY) hubMaxY = y; }

        if (outDir != null && outDir.Length > 0)
        {
            var vis = new Img(w, h);
            for (int i = 0; i < w * h; i++)
            {
                float r = 0, g = 0, b = 0, a = art.A[i] > .05f ? 1f : 0f;
                switch (part[i])
                {
                    case Part.Blue: b = 1; break;
                    case Part.Gold: r = 1; g = .8f; break;
                    case Part.Red: r = 1; break;
                    case Part.Magenta: r = 1; b = 1; break;
                    default: a = 0; break;
                }
                vis.R[i] = r * a; vis.G[i] = g * a; vis.B[i] = b * a; vis.A[i] = a;
            }
            vis.Save(System.IO.Path.Combine(outDir, "parts.png"));
        }

        var sb = new StringBuilder();
        sb.AppendLine("canvas " + w + "x" + h);
        sb.AppendLine("alpha bbox (" + minX + "," + minY + ")-(" + maxX + "," + maxY + ")  size " + (maxX - minX + 1) + "x" + (maxY - minY + 1));
        sb.AppendLine("pixels: blue=" + count[1] + " gold=" + count[2] + " red=" + count[3] + " magenta=" + count[4]);
        sb.AppendLine("blue centroid (" + bcx.ToString("0.0") + "," + bcy.ToString("0.0") + ")");
        sb.AppendLine("sphere: centre (" + sphereCx.ToString("0.00") + "," + sphereCy.ToString("0.00") + ") radius " + sphereR.ToString("0.00") + " (widest blue row y=" + bestRow + ", bottom y=" + bottom + ")");
        sb.AppendLine("hub dome: centroid (" + hubCx.ToString("0.00") + "," + hubCy.ToString("0.00") + ") bbox (" + hubMinX + "," + hubMinY + ")-(" + hubMaxX + "," + hubMaxY + ") size " + (hubMaxX - hubMinX + 1) + "x" + (hubMaxY - hubMinY + 1));
        return sb.ToString();
    }
}
