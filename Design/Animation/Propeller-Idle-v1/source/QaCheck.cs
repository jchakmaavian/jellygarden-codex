// Automated QA for the Color Ball sequences: file-level alpha hygiene, edge clipping, identity hand-offs, loop seams,
// frame-to-frame spikes (glow popping / flicker) and silhouette drift.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

public static class QaCheck
{
    public sealed class Raw { public int W, H; public byte[] Px; } // BGRA straight alpha

    public static Raw Load(string path)
    {
        using (var bmp = new Bitmap(path))
        {
            var r = new Raw { W = bmp.Width, H = bmp.Height, Px = new byte[bmp.Width * bmp.Height * 4] };
            var data = bmp.LockBits(new Rectangle(0, 0, r.W, r.H), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            for (int y = 0; y < r.H; y++) Marshal.Copy(IntPtr.Add(data.Scan0, y * data.Stride), r.Px, y * r.W * 4, r.W * 4);
            bmp.UnlockBits(data);
            return r;
        }
    }

    // Mean absolute premultiplied difference (0..255 scale) over RGBA.
    public static double Diff(Raw a, Raw b)
    {
        double d = 0; int n = a.W * a.H;
        for (int i = 0; i < n; i++)
        {
            int k = i * 4; double aa = a.Px[k + 3] / 255.0, ba = b.Px[k + 3] / 255.0;
            for (int c = 0; c < 3; c++) d += Math.Abs(a.Px[k + c] * aa - b.Px[k + c] * ba);
            d += Math.Abs(a.Px[k + 3] - b.Px[k + 3]);
        }
        return d / (n * 4.0);
    }

    public static int MaxAbs(Raw a, Raw b)
    {
        int m = 0; for (int i = 0; i < a.Px.Length; i++) m = Math.Max(m, Math.Abs(a.Px[i] - b.Px[i])); return m;
    }

    public static string Run(string dir, string prefix, int count, int loopA, int loopB, string cutsCsv)
    {
        var sb = new StringBuilder();
        var files = Enumerable.Range(1, count).Select(i => Path.Combine(dir, string.Format("{0}_{1:000}.png", prefix, i))).ToArray();
        var missing = files.Where(f => !File.Exists(f)).ToList();
        sb.AppendLine(string.Format("== {0}: expected {1} frames, missing {2}", prefix, count, missing.Count));
        if (missing.Count > 0) return sb.ToString();
        var cuts = new HashSet<int>((cutsCsv ?? "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(int.Parse));
        int badSize = 0, dirty = 0, faint = 0, maxBorder = 0, borderFrame = -1;
        var steps = new double[count]; var cxs = new double[count]; var cys = new double[count]; var cov = new double[count];
        Raw prev = null, first = null;
        for (int f = 0; f < count; f++)
        {
            var r = Load(files[f]);
            if (first != null && (r.W != first.W || r.H != first.H)) badSize++;
            double s = 0, sx = 0, sy = 0;
            for (int y = 0; y < r.H; y++)
                for (int x = 0; x < r.W; x++)
                {
                    int k = (y * r.W + x) * 4; byte a = r.Px[k + 3];
                    if (a == 0 && (r.Px[k] | r.Px[k + 1] | r.Px[k + 2]) != 0) dirty++;
                    if (a > 0 && a <= 1) faint++;
                    if ((x < 2 || y < 2 || x >= r.W - 2 || y >= r.H - 2) && a > maxBorder) { maxBorder = a; borderFrame = f + 1; }
                    s += a; sx += a * x; sy += a * y;
                }
            cov[f] = s / 255.0; cxs[f] = s > 0 ? sx / s : 0; cys[f] = s > 0 ? sy / s : 0;
            if (prev != null) steps[f] = Diff(prev, r);
            if (f == 0) first = r;
            prev = r;
        }
        sb.AppendLine(string.Format("canvas " + first.W + "x" + first.H + "; size mismatches {0}; dirty-alpha texels (A=0, RGB!=0) {1}; faint A=1 texels {2}; max border alpha {3} (frame {4})", badSize, dirty, faint, maxBorder, borderFrame));
        var normal = new List<double>();
        for (int f = 1; f < count; f++) if (!cuts.Contains(f)) normal.Add(steps[f]);
        normal.Sort();
        double median = normal.Count > 0 ? normal[normal.Count / 2] : 0;
        sb.AppendLine(string.Format("step diff median {0:0.000}, p90 {1:0.000}, max {2:0.000}", median, normal[(int)(normal.Count * 0.9)], normal[normal.Count - 1]));
        // Spikes: a step much larger than both neighbours (pop) outside declared cuts.
        var spikes = new List<string>();
        for (int f = 1; f < count; f++)
        {
            if (cuts.Contains(f)) continue;
            double nb = Math.Max(f > 1 ? steps[f - 1] : 0, f < count - 1 ? steps[f + 1] : 0);
            if (steps[f] > Math.Max(0.6, 2.2 * nb) && steps[f] > 1.5 * median) spikes.Add(string.Format("{0}->{1} step {2:0.00} (neighbours {3:0.00})", f, f + 1, steps[f], nb));
        }
        sb.AppendLine("isolated step spikes (possible pops): " + (spikes.Count == 0 ? "none" : string.Join("; ", spikes)));
        foreach (int c in cuts) if (c > 0 && c < count) sb.AppendLine(string.Format("declared cut {0}->{1}: step {2:0.00}", c, c + 1, steps[c]));
        if (loopA >= 0)
        {
            var la = Load(files[loopA]); var lb = Load(files[loopB]);
            double seam = Diff(lb, la);
            double loopMed = Enumerable.Range(loopA + 1, loopB - loopA).Select(f => steps[f]).OrderBy(v => v).ElementAt((loopB - loopA) / 2);
            sb.AppendLine(string.Format("loop {0}..{1} (files {2}..{3}): seam {4}->{5} diff {6:0.000} vs loop median step {7:0.000} (ratio {8:0.00})",
                loopA, loopB, loopA + 1, loopB + 1, loopB + 1, loopA + 1, seam, loopMed, loopMed > 0 ? seam / loopMed : 0));
        }
        double minCx = cxs.Min(), maxCx = cxs.Max(), minCy = cys.Min(), maxCy = cys.Max();
        sb.AppendLine(string.Format("alpha centroid x {0:0.00}..{1:0.00}, y {2:0.00}..{3:0.00}; coverage first {4:0} last {5:0} min {6:0} max {7:0}",
            minCx, maxCx, minCy, maxCy, cov[0], cov[count - 1], cov.Min(), cov.Max()));
        return sb.ToString();
    }

    public static string Identity(string a, string b)
    {
        var ra = Load(a); var rb = Load(b);
        return string.Format("{0} vs {1}: max byte diff {2}, mean diff {3:0.0000}", Path.GetFileName(a), Path.GetFileName(b), MaxAbs(ra, rb), Diff(ra, rb));
    }

    public static string Transparent(string a)
    {
        var r = Load(a); int m = 0; for (int i = 3; i < r.Px.Length; i += 4) m = Math.Max(m, r.Px[i]);
        return string.Format("{0}: max alpha {1}", Path.GetFileName(a), m);
    }
}

public static class QaCompare
{
    // Exact comparison split into: alpha mismatches, colour mismatches where alpha > 0, and dirty texels (A = 0, RGB != 0) in b.
    public static string Exact(string a, string b)
    {
        var ra = QaCheck.Load(a); var rb = QaCheck.Load(b);
        int alphaDiff = 0, colDiff = 0, dirtyB = 0;
        for (int i = 0; i < ra.Px.Length; i += 4)
        {
            if (ra.Px[i + 3] != rb.Px[i + 3]) alphaDiff++;
            else if (ra.Px[i + 3] > 0 && (ra.Px[i] != rb.Px[i] || ra.Px[i + 1] != rb.Px[i + 1] || ra.Px[i + 2] != rb.Px[i + 2])) colDiff++;
            if (rb.Px[i + 3] == 0 && (rb.Px[i] | rb.Px[i + 1] | rb.Px[i + 2]) != 0) dirtyB++;
        }
        return string.Format("{0} vs {1}: alpha mismatches {2}, colour mismatches (A>0) {3}, dirty texels in second {4}", System.IO.Path.GetFileName(a), System.IO.Path.GetFileName(b), alphaDiff, colDiff, dirtyB);
    }
}
