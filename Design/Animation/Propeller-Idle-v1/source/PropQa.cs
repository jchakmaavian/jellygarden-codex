// Quality gate for the propeller sequences: hand-offs, loop seams, drift, clipping and alpha.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

public static class PropQa
{
    static Img[] LoadSeq(string dir, string prefix, int count)
    {
        var a = new Img[count];
        for (int i = 0; i < count; i++) a[i] = Img.Load(Path.Combine(dir, prefix + "_" + (i + 1).ToString("000") + ".png"));
        return a;
    }

    static double Median(List<double> v) { var c = new List<double>(v); c.Sort(); return c.Count == 0 ? 0 : c[c.Count / 2]; }

    static string Common(Img[] f, string label, StringBuilder sb)
    {
        int bad = 0, dirty = 0, faint = 0; double edge = 0;
        foreach (var im in f)
        {
            if (im.W != 512 || im.H != 512) bad++;
            dirty += SeqMetrics.DirtyAlpha(im);
            for (int y = 0; y < im.H; y++)
                for (int x = 0; x < im.W; x++)
                {
                    int i = y * im.W + x;
                    float a = im.A[i];
                    if (a > 0 && a < 1.5f / 255f) faint++;
                    if (x < 2 || y < 2 || x >= im.W - 2 || y >= im.H - 2) edge = Math.Max(edge, a);
                }
        }
        sb.AppendLine("  frames                              " + f.Length + ", all 512x512: " + (bad == 0 ? "yes" : "NO (" + bad + " wrong)"));
        sb.AppendLine("  dirty alpha (colour under alpha 0)  " + dirty);
        sb.AppendLine("  stray near-invisible texels         " + faint);
        sb.AppendLine("  edge clipping (max alpha <2px rim)  " + edge.ToString("0.000"));
        return null;
    }

    static void Steps(Img[] f, int from, int to, StringBuilder sb, string label)
    {
        var d = new List<double>();
        for (int i = from; i < to; i++) d.Add(SeqMetrics.Diff(f[i], f[i + 1]));
        double med = Median(d), mx = 0; int at = -1;
        for (int i = 0; i < d.Count; i++) if (d[i] > mx) { mx = d[i]; at = from + i + 1; }
        sb.AppendLine("  " + label + " median step " + med.ToString("0.000") + ", largest " + mx.ToString("0.000") + " at " + at + "->" + (at + 1));
    }

    public static string Run(string revealDir, string idleDir, string actDir, string artPath)
    {
        var sb = new StringBuilder();
        var art = Img.Load(artPath);
        var rev = LoadSeq(revealDir, "Propeller_Reveal", PropSeq.RevealFrames);
        var idle = LoadSeq(idleDir, "Propeller_Idle", PropSeq.IdleFrames);
        var act = LoadSeq(actDir, "Propeller_Activation", PropSeq.ActFrames);

        sb.AppendLine("REVEAL");
        Common(rev, "reveal", sb);
        Steps(rev, 0, rev.Length - 1, sb, "frame-to-frame");
        sb.AppendLine("  settles on the artwork              diff(last, art) = " + SeqMetrics.Diff(rev[rev.Length - 1], art).ToString("0.0000"));
        sb.AppendLine("  hand-off to Idle 001                diff = " + SeqMetrics.Diff(rev[rev.Length - 1], idle[0]).ToString("0.0000"));

        sb.AppendLine("IDLE");
        Common(idle, "idle", sb);
        Steps(idle, 0, idle.Length - 1, sb, "frame-to-frame");
        sb.AppendLine("  frame 001 is the artwork            diff = " + SeqMetrics.Diff(idle[0], art).ToString("0.0000"));
        var inLoop = new List<double>();
        for (int i = 0; i < idle.Length - 1; i++) inLoop.Add(SeqMetrics.Diff(idle[i], idle[i + 1]));
        double seam = SeqMetrics.Diff(idle[idle.Length - 1], idle[0]);
        sb.AppendLine("  loop seam 120 -> 001                " + seam.ToString("0.000") + " vs median step " + Median(inLoop).ToString("0.000"));
        double minX = 1e9, maxX = -1e9, minY = 1e9, maxY = -1e9;
        foreach (var im in idle) { double cx, cy; int br; SeqMetrics.Centroid(im, out cx, out cy, out br); minX = Math.Min(minX, cx); maxX = Math.Max(maxX, cx); minY = Math.Min(minY, cy); maxY = Math.Max(maxY, cy); }
        sb.AppendLine("  position drift                      x " + (maxX - minX).ToString("0.00") + " px, y " + (maxY - minY).ToString("0.00") + " px");

        sb.AppendLine("ACTIVATION");
        Common(act, "activation", sb);
        sb.AppendLine("  frame 001 is the artwork            diff = " + SeqMetrics.Diff(act[0], art).ToString("0.0000"));
        Steps(act, 0, PropSeq.IntroLast - 1, sb, "intro 001-024");
        Steps(act, PropSeq.LoopFirst - 1, PropSeq.LoopLast - 1, sb, "loop  025-084");
        Steps(act, PropSeq.FinishFirst - 1, act.Length - 1, sb, "finish 085-120");
        var loopSteps = new List<double>();
        for (int i = PropSeq.LoopFirst - 1; i < PropSeq.LoopLast - 1; i++) loopSteps.Add(SeqMetrics.Diff(act[i], act[i + 1]));
        sb.AppendLine("  loop seam 084 -> 025                " + SeqMetrics.Diff(act[PropSeq.LoopLast - 1], act[PropSeq.LoopFirst - 1]).ToString("0.000") + " vs median step " + Median(loopSteps).ToString("0.000"));
        double lastAlpha = 0; foreach (float a in act[act.Length - 1].A) lastAlpha = Math.Max(lastAlpha, a);
        sb.AppendLine("  last frame fully transparent        max alpha " + lastAlpha.ToString("0.0000"));
        return sb.ToString();
    }
}
