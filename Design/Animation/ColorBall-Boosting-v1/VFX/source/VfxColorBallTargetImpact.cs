// Royal Throne Color Ball: ColorBall_TargetImpact.
// Final destruction flash of one marked target when the Color Ball detonates (no shards/debris: handled elsewhere).
// 384x384 canvas, board cell = 192 px, pivot at the canvas centre (192,192), 60 fps, 46 frames (f0 = +0 ... f44 = +44, f45 empty).
// C# 5 only (Windows PowerShell Add-Type). Depends on BoosterFx.cs (Img, Curve, Hash) only; every look helper is local.
//
// Revision 2 (review fixes):
//  - Rim fade (+24..+30) no longer uses partial alpha. The rim stays opaque and thins symmetrically about its centre line
//    (a noise-modulated band), so it breaks into white-hot / pale-gold filaments that shrink to nothing. Heat is floored to
//    pale gold .. warm white, and the halo is a blur of the actual body mask, so every filament keeps its own gold glow.
//  - Hollow is clean: the erosion noise is fully damped deep inside, the old inner-hole glow term is replaced by a 2.2 px
//    halo that hugs the real inner edge (wide halo only outside the body until the rim fade), and detached
//    body islands are removed during the dissolve/ring phase (only the main body survives until the rim fade starts).
//  - Ring body stays at full opacity (K_I = 1) and HeatScale >= 0.98 through +23.
//  - Dissolve opens as 5 small seeded holes on a raised centre plateau that merge into the ragged hollow.
//  - Burst stars: larger (18-30 px), thrown clear of the ring halo (0.64-0.74 cell), twinkle floor 0.85.
//  - Needles: lengths 1.00 / 0.95 / 0.90 (reach 0.73-0.75 cell) so all three clear the puff halo.
using System;
using System.Collections.Generic;
using System.IO;

public static class VfxColorBallTargetImpact
{
    public const string Name = "ColorBall_TargetImpact";
    public const int Size = 384;
    public const double Cell = 192.0;
    public const double Ox = 192.0, Oy = 192.0;
    public const int Frames = 46;

    // ------------------------------------------------------------------ palette (straight RGB)
    static readonly double[] WHITE = { 1.0, 1.0, 1.0 };
    static readonly double[] WARM = { 1.0, 0.973, 0.902 };   // #FFF8E6
    static readonly double[] PALE = { 1.0, 0.878, 0.541 };   // #FFE08A
    static readonly double[] GOLD = { 1.0, 0.761, 0.227 };   // #FFC23A
    static readonly double[] HALO = { 1.0, 0.824, 0.290 };   // #FFD24A

    // House ramp: 1 = white, 0.72 = warm white, 0.42 = pale gold, <= 0.12 = gold. Never darker than #FFC23A (no muddy orange).
    static void Ramp(double t, out double r, out double g, out double b)
    {
        t = t < 0 ? 0 : t > 1 ? 1 : t;
        double[] lo, hi; double u;
        if (t >= 0.72) { lo = WARM; hi = WHITE; u = (t - 0.72) / 0.28; }
        else if (t >= 0.42) { lo = PALE; hi = WARM; u = (t - 0.42) / 0.30; }
        else if (t >= 0.12) { lo = GOLD; hi = PALE; u = (t - 0.12) / 0.30; }
        else { lo = GOLD; hi = GOLD; u = 0; }
        r = lo[0] + (hi[0] - lo[0]) * u; g = lo[1] + (hi[1] - lo[1]) * u; b = lo[2] + (hi[2] - lo[2]) * u;
    }

    static double Clamp01(double v) { return v < 0 ? 0 : v > 1 ? 1 : v; }
    static double Sq(double v) { return v * v; }

    // ------------------------------------------------------------------ timeline (frame keys, f0 = +0)
    // Visual diameter of the puff / ring in cells (body edge plus half the spike length).
    static readonly double[] K_D = { 0, 0.37, 1, 0.51, 2, 0.63, 3, 0.74, 4, 0.82, 5, 0.875, 6, 0.91, 7, 0.935, 8, 0.95, 13, 0.975, 14, 1.00, 23, 1.07, 31, 1.10 };
    // Spike length as a fraction of body radius.
    static readonly double[] K_Sp = { 0, 0.28, 6, 0.22, 10, 0.18, 13, 0.12, 15, 0.06, 23, 0.045, 31, 0.04 };
    static readonly double[] K_Lobe = { 0, 0.13, 4, 0.12, 8, 0.09, 13, 0.06, 16, 0.035, 31, 0.03 };
    // Dissolve threshold (body-radius units): seeded holes open at +10/+11, merge by +12/+13, ragged hollow at +14.
    static readonly double[] K_T = { 10, 0.10, 11, 0.26, 12, 0.44, 13, 0.545, 14, 0.585, 16, 0.605, 18, 0.62, 23, 0.65, 26, 0.70, 31, 0.74 };
    // Raised centre plateau so the first holes open at the seed points instead of as one central splat.
    static readonly double[] K_Plateau = { 10, 0.36, 11, 0.36, 12, 0.26, 13, 0.20, 14, 0.0 };
    static readonly double[] K_HoleAmp = { 10, 0.34, 12, 0.40, 14, 0.44, 23, 0.40, 31, 0.40 };
    static readonly double[] K_Flick = { 10, 0.10, 14, 0.20, 31, 0.25 };
    // Rim fade: half-width of the surviving band about the rim centre line (body-radius units, ~1 px per 0.01). >= 0.5 = off.
    static readonly double[] K_BandH = { 22, 0.60, 23, 0.19, 24, 0.15, 25, 0.115, 26, 0.085, 27, 0.055, 28, 0.03, 29, 0.008, 30, -0.017, 31, -0.07 };
    const double BandAmp = 0.095;
    static readonly double[] K_EdgeBand = { 0, 0.40, 8, 0.32, 12, 0.22, 14, 0.16, 23, 0.14, 26, 0.09, 29, 0.04 };
    static readonly double[] K_EdgeHeat = { 0, 0.50, 8, 0.42, 14, 0.40, 31, 0.36 };
    static readonly double[] K_HeatScale = { 0, 1.0, 13, 1.0, 23, 0.98, 31, 0.97 };
    // Late heat floor: filaments stay pale gold .. warm white while they thin (never matte gold at the end).
    static readonly double[] K_HeatFloor = { 23, 0.0, 25, 0.30, 27, 0.50, 29, 0.58 };
    static readonly double[] K_GlowI = { 0, 0.70, 8, 0.55, 14, 0.45, 23, 0.42, 27, 0.38, 30, 0.32, 31, 0.0 };
    static readonly double[] K_GlowW = { 0, 9, 8, 15, 14, 14, 23, 15, 27, 12, 31, 9 };
    static readonly double[] K_GlowGain = { 0, 2.0, 23, 2.0, 27, 2.6, 30, 3.4 };
    // White flash haze around the puff at the pop.
    static readonly double[] K_FlashI = { 0, 0.85, 1, 0.65, 3, 0.30, 6, 0.0 };
    static readonly double[] K_FlashR = { 0, 62, 6, 105 };
    // Needle rays: reach and detached tail (cells), intensity.
    static readonly double[] K_NReach = { 0, 0.40, 2, 0.57, 4, 0.68, 6, 0.73, 8, 0.75 };
    static readonly double[] K_NTail = { 0, 0.0, 3, 0.04, 5, 0.18, 8, 0.42 };
    static readonly double[] K_NI = { 0, 1.0, 3, 1.0, 5, 0.75, 7, 0.25, 8, 0.0 };

    // ------------------------------------------------------------------ render
    public static void Render(string outRoot, int from, int to)
    {
        string dir = Path.Combine(outRoot, "frames");
        Directory.CreateDirectory(dir);
        for (int f = Math.Max(0, from); f <= Math.Min(Frames - 1, to); f++)
        {
            var im = new Img(Size, Size);
            if (f < Frames - 1) DrawFrame(im, f);
            Cleanup(im);
            im.Save(Path.Combine(dir, string.Format("{0}_{1:000}.png", Name, f + 1)));
        }
    }

    public static void DrawFrame(Img im, int f)
    {
        // 1. White flash haze.
        double flash = Curve.Eval(K_FlashI, f);
        if (f <= 6) WhiteHaze(im, Ox, Oy, Curve.Eval(K_FlashR, f), flash);

        // 2. Puff -> dissolving hollow -> ragged ring -> thinning filaments (gone at +31).
        if (f <= 30)
        {
            var p = new BlastP();
            p.F = f;
            double sp = Curve.Eval(K_Sp, f);
            p.Rb = Curve.Eval(K_D, f) * Cell * 0.5 / (1 + 0.5 * sp);
            p.SpFrac = sp;
            p.LobeAmp = Curve.Eval(K_Lobe, f);
            p.LobePhase = f * 0.045;
            p.T = f < 10 ? -2 : Curve.Eval(K_T, f);
            p.Plateau = f < 10 ? 0 : Curve.Eval(K_Plateau, f);
            p.HoleAmp = Curve.Eval(K_HoleAmp, f);
            p.Soft = 0.016;
            p.FlickW = f < 10 ? 0 : Curve.Eval(K_Flick, f);
            p.BandH = f < 22 ? 1 : Curve.Eval(K_BandH, f);
            p.EdgeBand = Curve.Eval(K_EdgeBand, f);
            p.EdgeHeat = Curve.Eval(K_EdgeHeat, f);
            p.TipHeat = 0.16;
            p.HeatScale = Curve.Eval(K_HeatScale, f);
            p.HeatFloor = f < 23 ? 0 : Curve.Eval(K_HeatFloor, f);
            p.I = 1.0;
            p.GlowI = Curve.Eval(K_GlowI, f);
            p.GlowW = Curve.Eval(K_GlowW, f);
            p.GlowGain = Curve.Eval(K_GlowGain, f);
            p.EdgeSeed = 5000 + f / 2;      // jagged edge re-randomises every 2 frames
            p.HoleSeed = 6102;              // stable hole field so the dissolve reads as one erosion
            p.FlickSeed = 6300 + f / 2;     // ragged inner edge crackle, every 2 frames
            p.LobeSeed = 6500;
            p.BandSeed = 6700;              // stable: filaments shrink continuously
            p.BandFlickSeed = 6800 + f / 2; // light crackle on the filaments, every 2 frames
            Blast(im, p);
        }

        // 3. Needle rays (+0..+8).
        double nI = Curve.Eval(K_NI, f);
        if (f <= 8 && nI > 0.001)
        {
            double baseAng = 360 * Hash.U(7101, 0);
            double[] lenF = { 1.0, 0.95, 0.90 };
            for (int k = 0; k < 3; k++)
            {
                double ang = baseAng + k * 120 + (Hash.U(7102, k) - 0.5) * 64;
                double reach = Curve.Eval(K_NReach, f) * Cell * lenF[k];
                double tail = Curve.Eval(K_NTail, f) * Cell * lenF[k];
                double core = 4.0 - 0.5 * k;
                Needle(im, Ox, Oy, ang, tail, reach, core, 6.0 - 0.8 * k, nI * (1 - 0.06 * k));
            }
        }

        // 4. Star burst: 7 chunky four-point stars thrown outward (+0..+18, ease-out), twinkling out by +44.
        // Staggered twinkle-out: one star leaves every ~2 frames over +25..+44; the last is gone at +44.
        int[] sEnd = { 35, 41, 38, 44, 33, 39, 42 };
        for (int k = 0; k < 7; k++)
        {
            double born = 1 + 3 * Hash.U(7201, k);
            double end = sEnd[k];
            if (f < born || f >= end) continue;
            double ang = (360 * Hash.U(7203, 0) + (k + 0.3 * (Hash.U(7204, k) - 0.5)) * 360.0 / 7) * Math.PI / 180;
            // Final radius 0.64-0.74 cell: clear of the ring halo (ring edge ~0.53 cell + glow).
            double rFinal = (0.64 + 0.10 * Hash.U(7205, k)) * Cell;
            double u = Math.Min(1, f / 18.0), ease = 1 - Math.Pow(1 - u, 3);
            double rr = rFinal * (0.10 + 0.90 * ease);
            // Half size 9-15 px: 18-30 px tip to tip.
            double half = 9 + 6 * Hash.U(7206, k);
            bool gold = k == 1 || k == 4;
            double pop = Curve.Eval(new double[] { born, 0.25, born + 3, 1.15, born + 6, 1.0 }, f);
            double per = 9 + 5 * Hash.U(7207, k), ph = per * Hash.U(7208, k);
            double twA = 0.05 + 0.025 * Curve.Smooth(20, 30, f);   // size twinkle floor >= 0.85
            double tw = 1 - twA + twA * Math.Cos(2 * Math.PI * (f + ph) / per);
            double flick = 0.84 + 0.16 * Hash.U(7209 + k, f / 2);
            double size = half * pop * tw * TwinkleOut(f, end, 8);
            double inten = flick * (1 - 0.15 * Curve.Smooth(end - 8, end, f));
            double rot = (Hash.U(7210, k) - 0.5) * 50 * (1 - ease) + (Hash.U(7211, k) - 0.5) * 14;
            ChunkyStar(im, Ox + Math.Cos(ang) * rr, Oy + Math.Sin(ang) * rr, size, rot, inten, gold ? 0.10 : 0.95, gold ? 0.45 : 0.30);
        }

        // 5. Tiny white stars inside the ring (+14..+23, lingering while the rim fades).
        int[] tBorn = { 14, 15, 17, 19 };
        int[] tEnd = { 30, 34, 37, 40 };
        for (int k = 0; k < 4; k++)
        {
            if (f < tBorn[k] || f >= tEnd[k]) continue;
            double ang = (360 * Hash.U(7301, 0) + k * 90 + (Hash.U(7302, k) - 0.5) * 50) * Math.PI / 180;
            double rr = (0.10 + 0.20 * Hash.U(7303, k)) * Cell;
            double half = 4.0 + 2.5 * Hash.U(7304, k);
            double pop = Curve.Eval(new double[] { tBorn[k], 0.3, tBorn[k] + 2, 1.2, tBorn[k] + 4, 1.0 }, f);
            double per = 8 + 4 * Hash.U(7305, k), ph = per * Hash.U(7306, k);
            double tw = 0.80 + 0.20 * Math.Cos(2 * Math.PI * (f + ph) / per);
            double flick = 0.85 + 0.15 * Hash.U(7307 + k, f / 2);
            ChunkyStar(im, Ox + Math.Cos(ang) * rr, Oy + Math.Sin(ang) * rr, half * pop * tw * TwinkleOut(f, tEnd[k], 7), 0, flick * (1 - 0.15 * Curve.Smooth(tEnd[k] - 7, tEnd[k], f)), 0.95, 0.28);
        }
    }

    // Twinkle-out size multiplier: 1, a brief flare, then a shrink to nothing at 'end' (a pure alpha fade reads grey on dark boards).
    static double TwinkleOut(double f, double end, double window)
    {
        double v = (f - (end - window)) / window;
        if (v <= 0) return 1;
        if (v >= 1) return 0;
        return Curve.Eval(new double[] { 0, 1.0, 0.3, 1.18, 1.0, 0.0 }, v);
    }

    // Zero out sub-visible alpha so no faint stray texels survive, then drop any disconnected speck whose peak alpha is < 10/255.
    static void Cleanup(Img im)
    {
        int n = im.W * im.H;
        for (int i = 0; i < n; i++)
        {
            if (im.A[i] < 1.5f / 255f) { im.A[i] = 0; im.R[i] = 0; im.G[i] = 0; im.B[i] = 0; continue; }
            if (im.R[i] > im.A[i]) im.R[i] = im.A[i];
            if (im.G[i] > im.A[i]) im.G[i] = im.A[i];
            if (im.B[i] > im.A[i]) im.B[i] = im.A[i];
        }
        // Hard guarantee: 2 px transparent border.
        for (int y = 0; y < im.H; y++)
            for (int x = 0; x < im.W; x++)
                if (x < 2 || y < 2 || x >= im.W - 2 || y >= im.H - 2) { int i = y * im.W + x; im.A[i] = 0; im.R[i] = 0; im.G[i] = 0; im.B[i] = 0; }
        // Faint disconnected specks (8-connected components of alpha > 0 with peak < 10/255).
        var lab = new int[n]; var stack = new Stack<int>(); var members = new List<int>(); int id = 0;
        for (int s = 0; s < n; s++)
        {
            if (im.A[s] <= 0 || lab[s] != 0) continue;
            id++; members.Clear(); float peak = 0; stack.Push(s); lab[s] = id;
            while (stack.Count > 0)
            {
                int q = stack.Pop(); members.Add(q); if (im.A[q] > peak) peak = im.A[q];
                int qx = q % im.W, qy = q / im.W;
                for (int oy = -1; oy <= 1; oy++)
                    for (int ox = -1; ox <= 1; ox++)
                    {
                        int nx = qx + ox, ny = qy + oy; if (nx < 0 || ny < 0 || nx >= im.W || ny >= im.H) continue;
                        int m = ny * im.W + nx; if (lab[m] != 0 || im.A[m] <= 0) continue; lab[m] = id; stack.Push(m);
                    }
            }
            if (peak < 10f / 255f) foreach (int q in members) { im.A[q] = 0; im.R[q] = 0; im.G[q] = 0; im.B[q] = 0; }
        }
    }

    // ------------------------------------------------------------------ shapes
    sealed class BlastP
    {
        public int F;
        public double Rb, SpFrac, LobeAmp, LobePhase, T, Plateau, HoleAmp, Soft, FlickW, BandH, EdgeBand, EdgeHeat, TipHeat, HeatScale, HeatFloor, I, GlowI, GlowW, GlowGain;
        public int EdgeSeed, HoleSeed, FlickSeed, LobeSeed, BandSeed, BandFlickSeed;
        public double[] SeedX, SeedY, SeedS, SeedA;
    }

    const int SP = 17;
    const int NSeedHoles = 5;

    // White-hot, lobed, spiky puff with a warm gold edge and a halo that follows the actual body mask.
    // T > -1 erodes it from the centre through a noise field; BandH < 0.5 thins the rim about its centre line.
    static void Blast(Img dst, BlastP p)
    {
        var off = new double[SP]; var len = new double[SP]; var hw = new double[SP];
        for (int k = 0; k < SP; k++)
        {
            off[k] = (Hash.U(p.EdgeSeed, k) - 0.5) * 0.55;
            double l = Hash.U(p.EdgeSeed, k + 40);
            double l2 = Hash.U(p.EdgeSeed, k + 80);
            len[k] = l < 0.20 ? 0.15 + l : l > 0.84 ? 1.0 + 0.35 * l2 : 0.40 + 0.50 * l2;
            hw[k] = 0.22 + 0.30 * Hash.U(p.EdgeSeed, k + 120);
        }
        // Seeded dissolve holes (body-radius units).
        var sx = new double[NSeedHoles]; var sy = new double[NSeedHoles]; var ss = new double[NSeedHoles]; var sa = new double[NSeedHoles];
        double seedBase = 6.2832 * Hash.U(6600, 0);
        for (int i = 0; i < NSeedHoles; i++)
        {
            double rho = 0.08 + 0.30 * Hash.U(6601, i);
            double a = seedBase + 6.2832 * (i / (double)NSeedHoles + 0.14 * (Hash.U(6602, i) - 0.5));
            sx[i] = Math.Cos(a) * rho; sy[i] = Math.Sin(a) * rho;
            ss[i] = 0.08 + 0.04 * Hash.U(6603, i);
            sa[i] = 0.26 + 0.12 * Hash.U(6604, i);
        }
        p.SeedX = sx; p.SeedY = sy; p.SeedS = ss; p.SeedA = sa;

        double ph1 = 6.283 * Hash.U(p.LobeSeed, 1) + p.LobePhase, ph2 = 6.283 * Hash.U(p.LobeSeed, 2) - p.LobePhase * 1.3, ph3 = 6.283 * Hash.U(p.LobeSeed, 3) + p.LobePhase * 0.7;
        double bodyR = p.Rb * (1 + p.LobeAmp * 1.2) * (1 + p.SpFrac * 1.4) + 3;
        double sigma = Math.Max(1.0, p.GlowW * 0.85);
        int kr = (int)Math.Ceiling(sigma * 3);
        int W = dst.W, H = dst.H;
        int bx0 = Math.Max(0, (int)(Ox - bodyR)), bx1 = Math.Min(W - 1, (int)(Ox + bodyR) + 1);
        int by0 = Math.Max(0, (int)(Oy - bodyR)), by1 = Math.Min(H - 1, (int)(Oy + bodyR) + 1);
        bool holes = p.T > -1;
        bool band = p.BandH < 0.5;
        double bandC = (1 + Math.Max(0, p.T)) * 0.5;
        double softB = 0.6 / p.Rb;

        var cov = new double[W * H];
        var heatB = new double[W * H];
        var tipB = new double[W * H];
        var rrB = new double[W * H];

        // Pass 1: body coverage (3x3 supersampled) and heat at the pixel centre.
        for (int y = by0; y <= by1; y++)
            for (int x = bx0; x <= bx1; x++)
            {
                double dxc = x + 0.5 - Ox, dyc = y + 0.5 - Oy, rc = Math.Sqrt(dxc * dxc + dyc * dyc);
                if (rc > bodyR) continue;
                double c = 0;
                for (int sj = 0; sj < 3; sj++)
                    for (int si = 0; si < 3; si++)
                    {
                        double dx = x + (si + 0.5) / 3 - Ox, dy = y + (sj + 0.5) / 3 - Oy, r = Math.Sqrt(dx * dx + dy * dy);
                        double ang = Math.Atan2(dy, dx);
                        double lobeR = p.Rb * (1 + p.LobeAmp * Lobe(ang, ph1, ph2, ph3));
                        double edge = lobeR * (1 + p.SpFrac * Teeth(ang, off, len, hw));
                        double inside = Curve.Smooth(edge + 0.5, edge - 0.5, r);
                        if (inside <= 0) continue;
                        double solid = 1;
                        double rrs = r / lobeR;
                        if (holes) solid = Curve.Smooth(p.T - p.Soft, p.T + p.Soft, HoleMetric(dx, dy, rrs, p));
                        if (band && solid > 0) solid *= Curve.Smooth(-softB, softB, BandMetric(dx, dy, rrs, bandC, p));
                        c += inside * solid;
                    }
                c /= 9;
                if (c <= 0) continue;
                int idx = y * W + x;
                cov[idx] = c;

                double angc = Math.Atan2(dyc, dxc);
                double lobeRc = p.Rb * (1 + p.LobeAmp * Lobe(angc, ph1, ph2, ph3));
                double rr = rc / lobeRc;
                rrB[idx] = rr;
                double dOuter = 1 - rr;
                double dInner = holes ? HoleMetric(dxc, dyc, rr, p) - p.T : 9;
                double dBand = band ? BandMetric(dxc, dyc, rr, bandC, p) : 9;
                double dEdge = Math.Min(dOuter, Math.Min(dInner, dBand));
                double heat, tipness = 0;
                if (dEdge >= 0) heat = p.EdgeHeat + (1 - p.EdgeHeat) * Curve.Smooth(0, p.EdgeBand, dEdge);
                else
                {
                    tipness = Clamp01(-dEdge / Math.Max(0.02, p.SpFrac));
                    heat = p.EdgeHeat + (p.TipHeat - p.EdgeHeat) * tipness;
                }
                heat = Math.Max(heat * p.HeatScale, p.HeatFloor);
                heatB[idx] = heat; tipB[idx] = tipness;
            }

        // Pass 2: nothing floats in the hollow. Dissolve + ring phase: keep only the main body (every detached island goes).
        // Early rim fade (to +25): the rim may start to break, so only drop detached pieces under 60 px.
        if (holes && !band) RemoveIslands(cov, W, H, bx0, bx1, by0, by1, 0.0, true);
        else if (band && p.F <= 25) RemoveIslands(cov, W, H, bx0, bx1, by0, by1, 60.0, false);

        // Pass 3: halo = gaussian blur of the body mask (follows spikes, the ragged inner edge and every late filament).
        // Wide blur outside the body outline; a narrow blur inside holes and the hollow so they stay clean until the rim fade.
        int gx0 = Math.Max(0, bx0 - kr), gx1 = Math.Min(W - 1, bx1 + kr), gy0 = Math.Max(0, by0 - kr), gy1 = Math.Min(H - 1, by1 + kr);
        var blur = BlurMask(cov, W, H, bx0, bx1, by0, by1, gx0, gx1, gy0, gy1, sigma);
        var blurN = holes ? BlurMask(cov, W, H, bx0, bx1, by0, by1, gx0, gx1, gy0, gy1, InnerGlowSigma) : null;
        double lateW = band ? Curve.Smooth(24.5, 28.5, p.F) : 0;

        // Pass 4: composite body + halo as additive-looking light.
        for (int y = gy0; y <= gy1; y++)
            for (int x = gx0; x <= gx1; x++)
            {
                int idx = y * W + x;
                double c = cov[idx];
                double glow = Math.Min(1, blur[idx] * p.GlowGain);
                if (holes)
                {
                    double dxc = x + 0.5 - Ox, dyc = y + 0.5 - Oy;
                    double rr = Math.Sqrt(dxc * dxc + dyc * dyc) / (p.Rb * (1 + p.LobeAmp * Lobe(Math.Atan2(dyc, dxc), ph1, ph2, ph3)));
                    double wWide = Math.Max(Curve.Smooth(0.94, 1.03, rr), lateW);
                    if (wWide < 1) glow = wWide * glow + (1 - wWide) * InnerGlowI * Math.Min(1, blurN[idx] * 2.0);
                }
                double wBody = c * p.I * (1 - 0.18 * tipB[idx]);
                double wGlow = glow * p.GlowI * p.I * (1 - c);
                wGlow *= Curve.Smooth(0.015, 0.05, wGlow);   // short knee: no wide alpha 1-7 tail around the halo
                double w = wBody + wGlow;
                if (w < 0.002) continue;
                double br = 0, bg = 0, bb = 0, gr, gg, gb;
                if (c > 0) Ramp(heatB[idx], out br, out bg, out bb);
                Ramp(0.10 + 0.30 * glow, out gr, out gg, out gb);
                dst.Add(idx, (float)(br * wBody + gr * wGlow), (float)(bg * wBody + gg * wGlow), (float)(bb * wBody + gb * wGlow), (float)Math.Min(1, w));
            }
    }

    const double InnerGlowSigma = 2.2, InnerGlowI = 0.45;

    // Separable gaussian blur of the coverage buffer (source rows/cols [bx0..bx1]x[by0..by1], output [gx0..gx1]x[gy0..gy1]).
    static double[] BlurMask(double[] cov, int W, int H, int bx0, int bx1, int by0, int by1, int gx0, int gx1, int gy0, int gy1, double sigma)
    {
        int kr = (int)Math.Ceiling(sigma * 3);
        var kern = new double[2 * kr + 1]; double ks = 0;
        for (int i = -kr; i <= kr; i++) { kern[i + kr] = Math.Exp(-i * i / (2 * sigma * sigma)); ks += kern[i + kr]; }
        for (int i = 0; i < kern.Length; i++) kern[i] /= ks;
        var tmp = new double[W * H]; var blur = new double[W * H];
        for (int y = by0; y <= by1; y++)
            for (int x = gx0; x <= gx1; x++)
            {
                double s = 0;
                for (int i = -kr; i <= kr; i++) { int xx = x + i; if (xx < bx0 || xx > bx1) continue; s += cov[y * W + xx] * kern[i + kr]; }
                tmp[y * W + x] = s;
            }
        for (int y = gy0; y <= gy1; y++)
            for (int x = gx0; x <= gx1; x++)
            {
                double s = 0;
                for (int i = -kr; i <= kr; i++) { int yy = y + i; if (yy < by0 || yy > by1) continue; s += tmp[yy * W + x] * kern[i + kr]; }
                blur[y * W + x] = s;
            }
        return blur;
    }

    // Removes 8-connected components of coverage > 0.02 whose summed coverage is under minArea, or (keepLargest) every
    // component except the largest one.
    static void RemoveIslands(double[] cov, int W, int H, int x0, int x1, int y0, int y1, double minArea, bool keepLargest)
    {
        var lab = new int[W * H]; var stack = new Stack<int>(); var areas = new List<double>(); areas.Add(0); int id = 0;
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                int s = y * W + x;
                if (cov[s] <= 0.02 || lab[s] != 0) continue;
                id++; double area = 0; stack.Push(s); lab[s] = id;
                while (stack.Count > 0)
                {
                    int q = stack.Pop(); area += cov[q];
                    int qx = q % W, qy = q / W;
                    for (int oy = -1; oy <= 1; oy++)
                        for (int ox = -1; ox <= 1; ox++)
                        {
                            int nx = qx + ox, ny = qy + oy; if (nx < x0 || ny < y0 || nx > x1 || ny > y1) continue;
                            int m = ny * W + nx; if (lab[m] != 0 || cov[m] <= 0.02) continue; lab[m] = id; stack.Push(m);
                        }
                }
                areas.Add(area);
            }
        int largest = 0; for (int i = 1; i <= id; i++) if (largest == 0 || areas[i] > areas[largest]) largest = i;
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                int s = y * W + x, l = lab[s]; if (l == 0) continue;
                bool drop = keepLargest ? l != largest : areas[l] < minArea;
                if (drop) cov[s] = 0;
            }
        // Faint fringe texels (cov <= 0.02) left next to removed islands.
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                int s = y * W + x; if (cov[s] <= 0 || cov[s] > 0.02) continue;
                bool near = false;
                for (int oy = -1; oy <= 1 && !near; oy++)
                    for (int ox = -1; ox <= 1; ox++)
                    {
                        int nx = x + ox, ny = y + oy; if (nx < 0 || ny < 0 || nx >= W || ny >= H) continue;
                        if (cov[ny * W + nx] > 0.02) { near = true; break; }
                    }
                if (!near) cov[s] = 0;
            }
    }

    // Low-frequency lumps, roughly in [-1, 1].
    static double Lobe(double ang, double p1, double p2, double p3)
    {
        return (0.55 * Math.Sin(3 * ang + p1) + 0.30 * Math.Sin(5 * ang + p2) + 0.25 * Math.Sin(2 * ang + p3)) / 1.1;
    }

    // Irregular concave spikes: 0 between teeth, up to 1 at the longest tip.
    static double Teeth(double ang, double[] off, double[] len, double[] hw)
    {
        double pos = (ang / (2 * Math.PI) + 1.0) % 1.0 * SP;
        int k0 = (int)Math.Floor(pos);
        double best = 0;
        for (int dk = -1; dk <= 1; dk++)
        {
            int kk = k0 + dk, k = ((kk % SP) + SP) % SP;
            double c = kk + 0.5 + off[k];
            double d = Math.Abs(pos - c);
            double tooth = 1 - d / hw[k];
            if (tooth <= 0) continue;
            double v = len[k] * Math.Pow(tooth, 1.8);
            if (v > best) best = v;
        }
        return best;
    }

    // Erosion metric: (plateau-raised) radius plus noise minus seeded hole bumps.
    // The noise is fully damped deep inside the hollow, so nothing can survive in the middle of the ring.
    static double HoleMetric(double dx, double dy, double rr, BlastP p)
    {
        double k = Curve.Smooth(p.T - 0.45, p.T - 0.05, rr);
        double m = p.Plateau > 0 ? Math.Sqrt(rr * rr + p.Plateau * p.Plateau) : rr;
        m += (HoleNoise(dx, dy, p) - 0.5) * p.HoleAmp * k;
        double ux = dx / p.Rb, uy = dy / p.Rb;
        for (int i = 0; i < NSeedHoles; i++)
        {
            double d2 = Sq(ux - p.SeedX[i]) + Sq(uy - p.SeedY[i]);
            m -= p.SeedA[i] * Math.Exp(-d2 / Sq(p.SeedS[i]));
        }
        return m;
    }

    // Rim-fade band: positive inside the surviving filament (distance to the band edge, body-radius units).
    static double BandMetric(double dx, double dy, double rr, double centre, BlastP p)
    {
        double n = 0.62 * VNoise(dx, dy, 26, p.BandSeed) + 0.38 * VNoise(dx, dy, 12, p.BandSeed + 1);
        n = n * 0.8 + 0.2 * VNoise(dx, dy, 10, p.BandFlickSeed);
        n = 0.5 + 0.5 * Math.Tanh((n - 0.5) * 3.6);
        return p.BandH + (n - 0.5) * BandAmp - Math.Abs(rr - centre);
    }

    static double HoleNoise(double dx, double dy, BlastP p)
    {
        double n = 0.60 * VNoise(dx, dy, 24, p.HoleSeed) + 0.34 * VNoise(dx, dy, 11, p.HoleSeed + 1) + 0.06 * VNoise(dx, dy, 6, p.HoleSeed + 2);
        if (p.FlickW > 0) n = n * (1 - p.FlickW) + VNoise(dx, dy, 9, p.FlickSeed) * p.FlickW;
        return 0.5 + 0.5 * Math.Tanh((n - 0.5) * 3.6);
    }

    static double VNoise(double x, double y, double cell, int seed)
    {
        double gx = x / cell + 100.37, gy = y / cell + 100.61;
        int ix = (int)Math.Floor(gx), iy = (int)Math.Floor(gy);
        double fx = gx - ix, fy = gy - iy;
        fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy);
        double a = Lat(ix, iy, seed), b = Lat(ix + 1, iy, seed), c = Lat(ix, iy + 1, seed), d = Lat(ix + 1, iy + 1, seed);
        double top = a + (b - a) * fx, bot = c + (d - c) * fx;
        return top + (bot - top) * fy;
    }

    static double Lat(int ix, int iy, int seed)
    {
        unchecked { return Hash.U(seed, (ix * 73856093) ^ (iy * 19349663)); }
    }

    // Thin needle ray from radius r0 to r1 along deg (0 = +x, clockwise on screen): white core, #FFD24A glow, sharp tip.
    static void Needle(Img dst, double cx, double cy, double deg, double r0, double r1, double coreFwhm, double glowSigma, double intensity)
    {
        if (intensity <= 0.002 || r1 - r0 < 1) return;
        double a = deg * Math.PI / 180, ux = Math.Cos(a), uy = Math.Sin(a);
        double pad = glowSigma * 3 + 3;
        double ax = cx + ux * r0, ay = cy + uy * r0, bx = cx + ux * r1, by = cy + uy * r1;
        int xa = Math.Max(0, (int)(Math.Min(ax, bx) - pad)), xb = Math.Min(dst.W - 1, (int)(Math.Max(ax, bx) + pad));
        int ya = Math.Max(0, (int)(Math.Min(ay, by) - pad)), yb = Math.Min(dst.H - 1, (int)(Math.Max(ay, by) + pad));
        double L = r1 - r0, fadeLen = Math.Min(16, L * 0.25);
        for (int y = ya; y <= yb; y++)
            for (int x = xa; x <= xb; x++)
            {
                double core = 0, glow = 0;
                for (int sj = 0; sj < 3; sj++)
                    for (int si = 0; si < 3; si++)
                    {
                        double px = x + (si + 0.5) / 3 - cx, py = y + (sj + 0.5) / 3 - cy;
                        double along = px * ux + py * uy, across = -px * uy + py * ux;
                        if (along < r0 || along > r1 + 1) continue;
                        double t = Clamp01((along - r0) / L);
                        double env = Curve.Smooth(r0, r0 + fadeLen, along) * Curve.Smooth(r1 + 0.6, r1 - 0.6, along);
                        double sc = (coreFwhm / 2.355) * Math.Pow(1 - t, 0.9) + 0.22;
                        double sg = glowSigma * (1 - 0.6 * t) + 0.8;
                        core += Math.Exp(-across * across / (2 * sc * sc)) * env * (1 - 0.25 * t);
                        glow += Math.Exp(-across * across / (2 * sg * sg)) * env * 1.0 * Math.Pow(1 - t, 1.0);
                    }
                core /= 9; glow /= 9;
                double wc = Math.Min(1, core) * intensity, wg = glow * (1 - Math.Min(1, core)) * intensity;
                double w = wc + wg;
                if (w < 0.002) continue;
                double gr, gg, gb; Ramp(0.20 + 0.25 * glow, out gr, out gg, out gb);
                dst.Add(y * dst.W + x, (float)(wc + gr * wg), (float)(wc + gg * wg), (float)(wc + gb * wg), (float)Math.Min(1, w));
            }
    }

    // Chunky concave four-point star (house FatStar look) with white-hot centre, heat-tinted tips and a soft warm halo.
    static void ChunkyStar(Img dst, double cx, double cy, double size, double deg, double intensity, double heat, double haloI)
    {
        if (intensity <= 0.002 || size <= 0.5) return;
        double rad = deg * Math.PI / 180, cs = Math.Cos(rad), sn = Math.Sin(rad);
        int R = (int)Math.Ceiling(size * 1.7) + 2;
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
                double ws = acc / 9 * intensity;
                double dxc = x + 0.5 - cx, dyc = y + 0.5 - cy, rr = Math.Sqrt(dxc * dxc + dyc * dyc) / size;
                double wh = Math.Exp(-rr * rr * 2.6) * haloI * intensity * (1 - Math.Min(1, ws));
                wh *= Curve.Smooth(0.006, 0.03, wh);   // clip the far halo tail (alpha 1-3) so no faint haze ring is left
                double w = ws + wh;
                if (w < 0.002) continue;
                double r, g, b; Ramp(Math.Max(heat, 1 - rr * 1.1), out r, out g, out b);
                double hr, hg, hb; Ramp(heat > 0.5 ? 0.62 : 0.25, out hr, out hg, out hb);
                dst.Add(y * dst.W + x, (float)(r * ws + hr * wh), (float)(g * ws + hg * wh), (float)(b * ws + hb * wh), (float)Math.Min(1, w));
            }
    }

    // Neutral white haze (gaussian), for the pop flash.
    static void WhiteHaze(Img dst, double cx, double cy, double radius, double intensity)
    {
        if (intensity <= 0.002 || radius <= 0) return;
        int R = (int)Math.Ceiling(radius * 1.9);
        for (int y = Math.Max(0, (int)cy - R); y < Math.Min(dst.H, (int)cy + R + 1); y++)
            for (int x = Math.Max(0, (int)cx - R); x < Math.Min(dst.W, (int)cx + R + 1); x++)
            {
                double dx = x + 0.5 - cx, dy = y + 0.5 - cy, q = (dx * dx + dy * dy) / (radius * radius);
                double w = Math.Exp(-q * 2.2) * intensity; if (w < 0.002) continue;
                dst.Add(y * dst.W + x, (float)(1.0 * w), (float)(0.985 * w), (float)(0.95 * w), (float)Math.Min(1, w));
            }
    }
}
