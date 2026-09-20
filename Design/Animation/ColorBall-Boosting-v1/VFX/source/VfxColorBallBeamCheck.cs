// Automated technical check for ColorBall_Beam frame clips (reads the saved PNGs, straight ARGB bytes).
// C# 5, System.Drawing only.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

public static class VfxColorBallBeamCheck
{
    sealed class Frame { public int W, H; public byte[] Px; } // BGRA straight

    static Frame Load(string path)
    {
        using (var bmp = new Bitmap(path))
        {
            int w = bmp.Width, h = bmp.Height;
            var f = new Frame { W = w, H = h, Px = new byte[w * h * 4] };
            var data = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            for (int y = 0; y < h; y++) Marshal.Copy(IntPtr.Add(data.Scan0, y * data.Stride), f.Px, y * w * 4, w * 4);
            bmp.UnlockBits(data);
            return f;
        }
    }

    // Mean absolute difference of premultiplied RGBA (0..255 scale) over all texels and channels.
    static double Diff(Frame a, Frame b)
    {
        double s = 0; int n = a.W * a.H;
        for (int i = 0; i < n; i++)
        {
            int k = i * 4;
            double aa = a.Px[k + 3] / 255.0, ba = b.Px[k + 3] / 255.0;
            for (int c = 0; c < 3; c++) s += Math.Abs(a.Px[k + c] * aa - b.Px[k + c] * ba);
            s += Math.Abs(a.Px[k + 3] - b.Px[k + 3]);
        }
        return s / (n * 4.0);
    }

    // framesDir: folder of <prefix>_NNN.png. loopStart/loopEnd: 1-based inclusive, 0 = no loop.
    public static string Run(string clipName, string framesDir, string prefix, int loopStart, int loopEnd, bool lastMustBeClear)
    {
        var files = new List<string>(Directory.GetFiles(framesDir, prefix + "_*.png"));
        files.Sort(StringComparer.Ordinal);
        var sb = new StringBuilder();
        sb.AppendLine(string.Format("== {0}: {1} frame(s) in {2}", clipName, files.Count, framesDir));
        if (files.Count == 0) { sb.AppendLine("  NO FRAMES"); return sb.ToString(); }
        var frames = new List<Frame>();
        int worstBorder = 0, worstBorderFrame = 0, premulBad = 0, dirtyClear = 0, faint = 0, isolated = 0;
        bool namesOk = true;
        for (int fi = 0; fi < files.Count; fi++)
        {
            string expect = string.Format("{0}_{1:000}.png", prefix, fi + 1);
            if (!string.Equals(Path.GetFileName(files[fi]), expect, StringComparison.Ordinal)) namesOk = false;
            var f = Load(files[fi]); frames.Add(f);
            int w = f.W, h = f.H;
            int bmax = 0;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int k = (y * w + x) * 4;
                    int a = f.Px[k + 3];
                    bool border = x < 2 || y < 2 || x >= w - 2 || y >= h - 2;
                    if (border && a > bmax) bmax = a;
                    // Premultiplied colour must not exceed alpha: round(c * a / 255) > a.
                    for (int c = 0; c < 3; c++) if ((int)Math.Round(f.Px[k + c] * a / 255.0) > a) { premulBad++; break; }
                    if (a == 0 && (f.Px[k] | f.Px[k + 1] | f.Px[k + 2]) != 0) dirtyClear++;
                    if (a > 0 && a < 2) faint++;
                    if (a > 0)
                    {
                        // Isolated: no other non-zero alpha within a 7x7 neighbourhood.
                        bool alone = true;
                        for (int dy = -3; dy <= 3 && alone; dy++)
                            for (int dx = -3; dx <= 3; dx++)
                            {
                                if (dx == 0 && dy == 0) continue;
                                int xx = x + dx, yy = y + dy;
                                if (xx < 0 || yy < 0 || xx >= w || yy >= h) continue;
                                if (f.Px[(yy * w + xx) * 4 + 3] > 0) { alone = false; break; }
                            }
                        if (alone) isolated++;
                    }
                }
            if (bmax > worstBorder) { worstBorder = bmax; worstBorderFrame = fi + 1; }
        }
        var f0 = frames[0];
        sb.AppendLine(string.Format("  size {0}x{1}, names 1-based sequential: {2}", f0.W, f0.H, namesOk ? "yes" : "NO"));
        sb.AppendLine(string.Format("  max alpha within 2 px of border: {0}{1}  [{2}]", worstBorder, worstBorder > 0 ? " (frame " + worstBorderFrame + ")" : "", worstBorder == 0 ? "PASS" : "FAIL"));
        sb.AppendLine(string.Format("  texels with premultiplied RGB > alpha: {0}  [{1}]", premulBad, premulBad == 0 ? "PASS" : "FAIL"));
        sb.AppendLine(string.Format("  texels alpha 0 with RGB != 0: {0}; texels with alpha 1/255: {1}; isolated texels: {2}  [{3}]",
            dirtyClear, faint, isolated, dirtyClear + faint + isolated == 0 ? "PASS" : "FAIL"));
        if (lastMustBeClear)
        {
            var last = frames[frames.Count - 1]; int lm = 0;
            for (int i = 0; i < last.W * last.H; i++) lm = Math.Max(lm, last.Px[i * 4 + 3]);
            sb.AppendLine(string.Format("  last frame max alpha: {0}  [{1}]", lm, lm == 0 ? "PASS" : "FAIL"));
        }
        if (frames.Count > 1)
        {
            var steps = new StringBuilder();
            for (int i = 0; i + 1 < frames.Count; i++) steps.Append(string.Format("{0:0.00} ", Diff(frames[i], frames[i + 1])));
            sb.AppendLine("  consecutive steps f1->f2 ...: " + steps.ToString().Trim());
        }
        if (loopStart > 0 && loopEnd > loopStart && loopEnd <= frames.Count)
        {
            double sum = 0, mx = 0, mn = double.MaxValue; int n = 0;
            for (int i = loopStart; i < loopEnd; i++)
            {
                double d = Diff(frames[i - 1], frames[i]); sum += d; n++; mx = Math.Max(mx, d); mn = Math.Min(mn, d);
            }
            double avg = sum / n, wrap = Diff(frames[loopEnd - 1], frames[loopStart - 1]);
            sb.AppendLine(string.Format("  loop f{0}-f{1}: wrap f{1}->f{0} = {2:0.000}, mean step = {3:0.000} (min {4:0.000}, max {5:0.000}), ratio {6:0.00}  [{7}]",
                loopStart, loopEnd, wrap, avg, mn, mx, wrap / avg, wrap <= mx * 1.1 && wrap >= mn * 0.9 ? "PASS" : "CHECK"));
            // Crackle re-seeds every 2 frames, so the wrap (a re-seed boundary when the loop length is even) is also
            // compared with the other re-seed steps (f(s+1)->f(s+2), f(s+3)->f(s+4), ...).
            if ((loopEnd - loopStart + 1) % 2 == 0)
            {
                double rs = 0; int rn = 0;
                for (int i = loopStart + 1; i < loopEnd; i += 2) { rs += Diff(frames[i - 1], frames[i]); rn++; }
                if (rn > 0) sb.AppendLine(string.Format("  re-seed steps mean = {0:0.000}; wrap / re-seed mean = {1:0.00}", rs / rn, wrap / (rs / rn)));
            }
            // Also into and out of the loop.
            if (loopStart > 1) sb.AppendLine(string.Format("  entry f{0}->f{1} = {2:0.000}", loopStart - 1, loopStart, Diff(frames[loopStart - 2], frames[loopStart - 1])));
            if (loopEnd < frames.Count) sb.AppendLine(string.Format("  exit f{0}->f{1} = {2:0.000}", loopEnd, loopEnd + 1, Diff(frames[loopEnd - 1], frames[loopEnd])));
        }
        return sb.ToString();
    }
}
