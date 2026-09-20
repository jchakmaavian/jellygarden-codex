// Reviewer probe: desaturated (grey/putty) composites on the lavender board, per frame and region.
using System;
using System.IO;
using System.Text;

public static class ReviewColorBallBeamLav
{
    static double Lum(double r, double g, double b) { return 0.2126 * r + 0.7152 * g + 0.0722 * b; }
    public static string Run(string root)
    {
        var sb = new StringBuilder();
        const double br = 138, bgc = 147, bb = 200; double lbg = Lum(br, bgc, bb);
        sb.AppendLine("LAVENDER putty/grey: composite chroma < 22 (board chroma 62) and lum between bg+6 and 215; counts src(x<200)/mid/tgt(x>=520); and share of all visible (lum>bg+6) texels");
        string dir = Path.Combine(root, "Sequence", "frames");
        for (int k = 1; k <= 34; k++)
        {
            var f = ReviewColorBallBeam.Load(Path.Combine(dir, string.Format("ColorBall_Beam_{0:000}.png", k)));
            int s = 0, m = 0, t = 0, vis = 0; double minC = 999;
            for (int y = 0; y < f.H; y++)
                for (int x = 0; x < f.W; x++)
                {
                    int i = y * f.W + x; if (f.A[i] == 0) continue; double a = f.A[i] / 255.0;
                    double r = f.R[i] * a + br * (1 - a), g = f.G[i] * a + bgc * (1 - a), b = f.B[i] * a + bb * (1 - a);
                    double c = Math.Max(r, Math.Max(g, b)) - Math.Min(r, Math.Min(g, b)), l = Lum(r, g, b);
                    if (l > lbg + 6) vis++;
                    if (c < 22 && l > lbg + 6 && l < 215) { if (x < 200) s++; else if (x < 520) m++; else t++; if (c < minC) minC = c; }
                }
            sb.AppendLine(string.Format("  f{0}: putty src={1} mid={2} tgt={3} total={4} of visible {5} ({6:0.0}%) minChroma={7:0}", k, s, m, t, s + m + t, vis, vis > 0 ? 100.0 * (s + m + t) / vis : 0, minC > 900 ? 0 : minC));
        }
        return sb.ToString();
    }
}
