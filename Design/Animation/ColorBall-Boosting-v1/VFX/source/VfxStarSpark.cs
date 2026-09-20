// ColorBall_StarSpark: the reusable four-point glint used around the Royal Throne Color Ball.
// C# 5 (Windows PowerShell Add-Type), compiled together with ..\BoosterFx.cs (Img, Curve, Hash).
//
// Canvas 128x128, pivot at canvas coordinate (64, 64) = normalised (0.5, 0.5). Star tip radius at size 1.0 = 52 px.
// Clips (60 fps):
//   White  22 frames  pop 0.15 -> 1.15 (f3) -> 1.0 (f5), hold, shrink from f14, f22 fully transparent. Rotation 0 -> 25 deg.
//   Gold   22 frames  same timing, gold arms.
//   Static  2 frames  f1 white, f2 gold, size 1.0, rotation 0, neutral shimmer (for particle systems).
//
// Look (house CB.FatStar, rebuilt with an analytic anti-aliased edge): concave four-point star |u|^p + |v|^p <= 1,
// white-hot centre dot over a warm-white (White) or pale-gold -> gold (Gold) body, a thin needle of light along each arm
// axis that carries the visible tip out to the full tip radius, and a soft low halo along the outer arm flanks.
// Every glow term is windowed to reach exactly zero inside the tip radius, so nothing reaches past 52 * scale px (+ AA).
//
// End of life (f14 -> f22): the star stays opaque while it shrinks. The halo and bloom leave first (gone by f18/f19), the tips
// fade inward and the arms thin (f15 -> f18), then the whole star pulls in on top of the scale (f19 -> f21) while the colours
// move to white. f21 is a small bright point, f22 empty. No frame is a see-through star.
using System;
using System.IO;

public static class VfxStarSpark
{
    public const string AssetName = "ColorBall_StarSpark";
    public const int Size = 128;
    public const double Cx = 64, Cy = 64;
    public const double TipR = 52;
    public const int LifeFrames = 22;
    const int SS = 5;           // supersamples per axis for the star body (25 per pixel)
    const double P0 = 0.54;     // concavity exponent: notch at 0.39 of tip radius (FatStar uses 0.58, Fx.Star 0.45)

    // House palette, straight RGB.
    static readonly double[] CWhite = { 1.0, 1.0, 1.0 };
    static readonly double[] CWarm = { 1.0, 248 / 255.0, 230 / 255.0 };   // #FFF8E6
    static readonly double[] CPale = { 1.0, 224 / 255.0, 138 / 255.0 };   // #FFE08A
    static readonly double[] CGold = { 1.0, 194 / 255.0, 58 / 255.0 };    // #FFC23A
    static readonly double[] CHalo = { 1.0, 210 / 255.0, 74 / 255.0 };    // #FFD24A

    // Life-clip keys, 0-based frame index (frame file number - 1).
    // Pop: f1 0.15, f3 1.15, f5 1.00 (monotone cubic: no wobble past f5), hold to f14.
    static readonly double[] K_Scale = { 0, 0.15, 2, 1.15, 4, 1.0, 13, 1.0 };
    // Extra light at the pop, gone by f6.
    static readonly double[] K_Flash = { 0, 0.6, 1, 1.0, 2, 0.8, 5, 0.0, 21, 0.0 };
    const int ShrinkFrom = 13, LastFrame = 21;
    const double RotTotal = 25.0;

    // Tail keys over u = (frameIndex - 13) / 8:  u 0 = f14, .125 = f15, .25 = f16, .375 = f17, .5 = f18, .625 = f19, .75 = f20, .875 = f21.
    static readonly double[] K_TailRim = { 0, 1.0, 0.125, 0.65, 0.25, 0.35, 0.375, 0.12, 0.5, 0.0 };        // halo gone by f18
    static readonly double[] K_TailBloom = { 0, 1.0, 0.25, 0.60, 0.5, 0.15, 0.625, 0.0 };                   // centre bloom gone by f19
    static readonly double[] K_TailEdge = { 0, 1.60, 0.125, 1.32, 0.25, 1.16, 0.375, 1.04, 0.5, 0.96, 0.625, 0.93, 0.875, 0.93 }; // arms gone past this rn (of the contracted size)
    static readonly double[] K_TailEdgeW = { 0, 0.45, 0.5, 0.42, 0.875, 0.42 };                              // width of the tip fade (rn)
    static readonly double[] K_TailContract = { 0, 1.0, 0.375, 1.0, 0.5, 0.96, 0.625, 0.86, 0.75, 0.68, 0.875, 0.47 };    // star pulls inward on top of the scale
    static readonly double[] K_TailThin = { 0, 0.0, 0.25, 0.25, 0.5, 0.60, 0.75, 0.80 };                      // arms thin (lower concavity exponent)
    static readonly double[] K_TailWhite = { 0, 0.0, 0.25, 0.30, 0.5, 0.75, 0.625, 1.0 };                    // colours move to white

    public static int PremulViolations; // texels whose premultiplied RGB exceeded alpha before save (should stay 0)

    // ------------------------------------------------------------------ timing
    public static double LifeScale(int f)
    {
        if (f <= ShrinkFrom) return Curve.Eval(K_Scale, f);
        double u = Math.Min(1.0, (f - ShrinkFrom) / (double)(LastFrame - ShrinkFrom));
        return 1.0 - 0.8 * Math.Pow(u, 1.1);
    }

    // 0 while alive, rises 1/8 per frame from f14; 1 at f22 (nothing drawn).
    public static double LifeTail(int f)
    {
        if (f <= ShrinkFrom) return 0.0;
        return Math.Min(1.0, (f - ShrinkFrom) / (double)(LastFrame - ShrinkFrom));
    }

    public static double LifeRotation(int f) { return RotTotal * f / (double)LastFrame; }

    // Gentle shimmer in [-1, 1]: slow 8-frame swell plus a small twinkle re-randomised every 2 frames.
    public static double Shimmer(int f, int seed)
    {
        return 0.65 * Math.Sin(2 * Math.PI * f / 8.0) + 0.35 * (2 * Hash.U(seed, f / 2) - 1);
    }

    // ------------------------------------------------------------------ helpers
    static double Win(double x) { if (x >= 1) return 0; double q = 1 - x * x; return q * q; }

    static void Lerp(double[] a, double[] b, double t, double[] o)
    {
        o[0] = a[0] + (b[0] - a[0]) * t; o[1] = a[1] + (b[1] - a[1]) * t; o[2] = a[2] + (b[2] - a[2]) * t;
    }

    // Signed distance in px to the concave star edge (positive inside) for a point in star-local px coords.
    // spine = 1 on an arm's axis, ~0 at its edge. an = distance along the nearest arm / s, perp = px from that arm's axis.
    static double StarDist(double u, double v, double s, double p, out double spine, out double an, out double perp)
    {
        double a = Math.Abs(u), b = Math.Abs(v);
        if (b > a) { double t = a; a = b; b = t; }
        an = a / s; perp = b;
        if (an >= 1) { spine = 0; return -Math.Sqrt((a - s) * (a - s) + b * b); }
        double ap = Math.Pow(an, p);
        double w = s * Math.Pow(1 - ap, 1 / p); // arm half-width at this distance along the arm
        double q = w > 1e-9 ? Math.Min(1, b / w) : 1;
        spine = Math.Exp(-q * q * 3.0);
        // Deep inside the centre square (below the notch corner) every point is inside.
        if (an < 0.9 * Math.Pow(0.5, 1 / p)) return s;
        double k = -(ap / an) * Math.Pow(1 - ap, 1 / p - 1); // dw/da
        double d = (w - b) / Math.Sqrt(1 + k * k);
        return Math.Min(d, s - a);
    }

    // ------------------------------------------------------------------ the star
    /// <summary>
    /// Draw the glint centred at (cx, cy). size = tip radius in px, deg = clockwise rotation, gold = gold arm variant,
    /// shimmer in [-1, 1], flash in [0, 1] = pop light, tail in [0, 1] = end-of-life phase (0 = alive, 1 = gone).
    /// </summary>
    public static void DrawStar(Img dst, double cx, double cy, double size, double deg, bool gold, double shimmer, double flash, double tail)
    {
        if (tail >= 1 || size <= 0.5) return;
        double rad = deg * Math.PI / 180, cs = Math.Cos(rad), sn = Math.Sin(rad);
        // Tail controls (all identity at tail 0).
        double s = size * Curve.Eval(K_TailContract, tail);
        double rimK = Curve.Eval(K_TailRim, tail), bloomK = Curve.Eval(K_TailBloom, tail);
        double edge = Curve.Eval(K_TailEdge, tail), edgeW = Curve.Eval(K_TailEdgeW, tail);
        double p = P0 - 0.10 * Curve.Eval(K_TailThin, tail);
        double whiten = Curve.Eval(K_TailWhite, tail) * (gold ? 0.35 : 1.0);

        double tipFade = (gold ? 0.22 : 0.26) * (1 - 0.60 * shimmer);
        double dotI = Math.Min(1, 0.72 + 0.10 * shimmer + 0.28 * flash);
        dotI = Curve.Eval(new double[] { 0, dotI, 0.375, dotI, 0.625, 1.0, 0.875, 1.0 }, tail);
        // Centre dot radius in px; late in the tail it keeps a floor so the last frame ends on a small bright point.
        double dotPx = Math.Max(0.13 * s, 1.7 * Curve.Smooth(0.55, 0.875, tail));
        double bloomI = (0.30 + 0.10 * shimmer + 0.25 * flash) * bloomK;
        // Soft halo along the outer arm flanks: low, near-neutral light (not an outline).
        double rimI = ((gold ? 0.30 : 0.20) * (1 + 0.25 * shimmer) + 0.06 * flash) * rimK;
        double rimW = Math.Max(0.9, 0.05 * s); // px e-folding distance outside the silhouette
        // Pop flash spilling out of the notches (behind the body), strongest f2-f3.
        double notchI = 0.20 * Math.Pow(Math.Max(0, flash), 1.5);
        // Needle of light along each arm axis: carries the tip out to the full radius.
        double needleI = (gold ? 0.92 : 0.96) * (1 + 0.04 * shimmer);
        double sigma = Math.Max(0.42, Math.Min(0.6, 0.012 * s));
        double inv2s2 = 1.0 / (2 * sigma * sigma);

        int R = (int)Math.Ceiling(s) + 2;
        var col = new double[3]; var sc = new double[3]; var ec = new double[3]; var gc = new double[3];
        int x0 = Math.Max(0, (int)Math.Floor(cx) - R), x1 = Math.Min(dst.W - 1, (int)Math.Floor(cx) + R);
        int y0 = Math.Max(0, (int)Math.Floor(cy) - R), y1 = Math.Min(dst.H - 1, (int)Math.Floor(cy) + R);
        double inv = 1.0 / (SS * SS);
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                double ddx = x + 0.5 - cx, ddy = y + 0.5 - cy, rC = Math.Sqrt(ddx * ddx + ddy * ddy) / s;
                if (rC > 1.0 + 2.0 / s) continue;

                double pr = 0, pg = 0, pb = 0, pa = 0;
                    for (int j = 0; j < SS; j++)
                        for (int i = 0; i < SS; i++)
                        {
                            double dx = x + (i + 0.5) / SS - cx, dy = y + (j + 0.5) / SS - cy;
                            double u = dx * cs + dy * sn, v = -dx * sn + dy * cs;
                            double spine, an, perp;
                            double d = StarDist(u, v, s, p, out spine, out an, out perp);
                            double rn = Math.Sqrt(dx * dx + dy * dy) / s;
                            // Tail retraction: arms fade from the tips inward.
                            double tw = 1 - Curve.Smooth(edge - edgeW, edge, rn);
                            if (tw <= 0) continue;
                            double tipW = Math.Pow(Curve.Smooth(0.30, 1.0, rn), 1.2);

                            // ---- colours
                            if (!gold)
                            {
                                // Spine: white-hot centre -> warm white body -> faint pale-gold tint only at the tips.
                                Lerp(CWhite, CWarm, Curve.Smooth(0.10, 0.34, rn), sc); Lerp(sc, CPale, 0.22 * Curve.Smooth(0.70, 1.0, rn), sc);
                                // Edges: warm white, pale gold only past rn 0.7 (partial alpha must stay neutral).
                                Lerp(CWhite, CWarm, Curve.Smooth(0.10, 0.34, rn), ec); Lerp(ec, CPale, 0.12 * Curve.Smooth(0.70, 1.0, rn), ec);
                                Lerp(ec, sc, Math.Pow(spine, 0.7), col);
                            }
                            else
                            {
                                // Spine: warm white -> pale gold -> gold toward the tips; edges gold sooner.
                                Lerp(CWarm, CPale, Curve.Smooth(0.10, 0.50, rn), sc); Lerp(sc, CGold, 0.60 * Curve.Smooth(0.45, 1.0, rn), sc);
                                Lerp(CPale, CGold, Curve.Smooth(0.10, 0.60, rn), ec);
                                Lerp(ec, sc, Math.Pow(spine, 0.7) * 0.75, col);
                                Lerp(col, CWhite, 1 - Curve.Smooth(0.06, 0.24, rn), col);
                            }
                            if (whiten > 0) { Lerp(col, CWhite, whiten, col); Lerp(sc, gold ? CWarm : CWhite, whiten, sc); }

                            // ---- behind: soft flank halo + notch flash (outside the silhouette only)
                            double bA = 0, bR = 0, bG = 0, bB = 0;
                            if (d < 0)
                            {
                                double dOut = -d;
                                double gE = rimI * Math.Exp(-dOut / rimW) * Curve.Smooth(0.35, 0.62, rn) * (1 - Curve.Smooth(0.80, 0.97, rn));
                                if (gE > 0)
                                {
                                    if (!gold) Lerp(CWhite, CWarm, Curve.Smooth(0.5, 0.9, rn), gc); else Lerp(CPale, CHalo, Curve.Smooth(0.4, 0.8, rn), gc);
                                    bR = gc[0] * gE; bG = gc[1] * gE; bB = gc[2] * gE; bA = gE;
                                }
                                double nb = notchI * (1 - Curve.Smooth(0.42, 0.70, rn));
                                if (nb > 0)
                                {
                                    double[] fc = gold ? CWarm : CWhite; double k0 = 1 - nb;
                                    bR = fc[0] * nb + bR * k0; bG = fc[1] * nb + bG * k0; bB = fc[2] * nb + bB * k0; bA = nb + bA * k0;
                                }
                            }

                            // ---- body (box-filtered edge one sub-sample wide)
                            double cov = d * SS + 0.5;
                            if (cov < 0) cov = 0; else if (cov > 1) cov = 1;
                            double op = (1 - tipFade * tipW) * (0.88 + 0.12 * spine);
                            double a = cov * op;

                            // ---- needle along the arm axis, tapering to exactly 0 at the tip radius
                            double nA = 0;
                            if (an < 1 && an > 0.35)
                            {
                                double taper = Math.Pow(Math.Min(1.0, (1 - an) / 0.40), 0.36);
                                nA = needleI * taper * Math.Exp(-perp * perp * inv2s2) * (1 - 0.35 * tipFade * tipW);
                                if (nA > 1) nA = 1;
                            }
                            // needle over body, both over the behind layer; all scaled by the tail retraction
                            double fr = sc[0] * nA + col[0] * a * (1 - nA), fg = sc[1] * nA + col[1] * a * (1 - nA), fb = sc[2] * nA + col[2] * a * (1 - nA);
                            double fa = nA + a * (1 - nA);
                            double kb = 1 - fa;
                            pr += (fr + bR * kb) * tw; pg += (fg + bG * kb) * tw; pb += (fb + bB * kb) * tw; pa += (fa + bA * kb) * tw;
                        }
                double Ar = pr * inv, Ag = pg * inv, Ab = pb * inv, Aa = pa * inv;

                // ---- luminous centre: tight white dot plus a soft bloom filling the notches (over everything)
                double rPx = rC * s, dot = dotI * Math.Exp(-(rPx / dotPx) * (rPx / dotPx));
                double bloom = bloomI * Math.Exp(-(rC / 0.30) * (rC / 0.30)) * Win(rC / 0.72);
                double la = bloom + dot * (1 - bloom);
                if (la > 1) la = 1;
                double lr, lg, lb;
                if (!gold) { lr = 1.0; lg = 1.0; lb = 1.0; }
                else { lr = 1.0; lg = 0.99 - 0.12 * rC * (1 - whiten); lb = 0.93 - 0.45 * rC * (1 - whiten); }
                if (la > 0)
                {
                    // Colour of the light: dot pure white (White) / warm white (Gold); bloom follows the body tint.
                    double wd = dot * (1 - bloom) / la;
                    if (!gold) { lg = 1.0 - 0.03 * (1 - wd) * (1 - whiten); lb = 1.0 - 0.10 * (1 - wd) * (1 - whiten); }
                    else { lg = lg * (1 - wd) + CWarm[1] * wd; lb = lb * (1 - wd) + CWarm[2] * wd; }
                }
                Ar = lr * la + Ar * (1 - la); Ag = lg * la + Ag * (1 - la); Ab = lb * la + Ab * (1 - la); Aa = la + Aa * (1 - la);

                if (Aa <= 0) continue;
                dst.Over(y * dst.W + x, (float)Ar, (float)Ag, (float)Ab, (float)Aa);
            }
    }

    // Remove sub-2/255 alpha dust and verify premultiplied validity.
    static void Clean(Img im)
    {
        for (int i = 0; i < im.W * im.H; i++)
        {
            if (im.A[i] < 2f / 255f) { im.R[i] = im.G[i] = im.B[i] = im.A[i] = 0; continue; }
            float m = Math.Max(im.R[i], Math.Max(im.G[i], im.B[i]));
            if (m > im.A[i] + 1e-4f) { PremulViolations++; im.R[i] = Math.Min(im.R[i], im.A[i]); im.G[i] = Math.Min(im.G[i], im.A[i]); im.B[i] = Math.Min(im.B[i], im.A[i]); }
        }
    }

    static void Clear(Img im)
    {
        Array.Clear(im.R, 0, im.R.Length); Array.Clear(im.G, 0, im.G.Length); Array.Clear(im.B, 0, im.B.Length); Array.Clear(im.A, 0, im.A.Length);
    }

    static void SaveFrame(Img im, string dir, int frameIndex)
    {
        Clean(im);
        im.Save(Path.Combine(dir, string.Format("{0}_{1:000}.png", AssetName, frameIndex + 1)));
    }

    // ------------------------------------------------------------------ clips
    public static void RenderLife(string clipDir, bool gold, int from, int to)
    {
        string frames = Path.Combine(clipDir, "frames");
        Directory.CreateDirectory(frames);
        var canvas = new Img(Size, Size);
        int seed = gold ? 7127 : 4051;
        for (int f = Math.Max(0, from); f <= Math.Min(LifeFrames - 1, to); f++)
        {
            Clear(canvas);
            double tail = LifeTail(f);
            if (tail < 1)
                DrawStar(canvas, Cx, Cy, TipR * LifeScale(f), LifeRotation(f), gold, Shimmer(f, seed), Curve.Eval(K_Flash, f), tail);
            SaveFrame(canvas, frames, f);
        }
    }

    public static void RenderStatic(string clipDir)
    {
        string frames = Path.Combine(clipDir, "frames");
        Directory.CreateDirectory(frames);
        var canvas = new Img(Size, Size);
        for (int f = 0; f < 2; f++)
        {
            Clear(canvas);
            DrawStar(canvas, Cx, Cy, TipR, 0, f == 1, 0, 0, 0);
            SaveFrame(canvas, frames, f);
        }
    }

    /// <summary>Render all three clips into outRoot\White, outRoot\Gold, outRoot\Static (life frames from..to, 0-based).</summary>
    public static string Render(string outRoot, int from, int to)
    {
        PremulViolations = 0;
        RenderLife(Path.Combine(outRoot, "White"), false, from, to);
        RenderLife(Path.Combine(outRoot, "Gold"), true, from, to);
        RenderStatic(Path.Combine(outRoot, "Static"));
        return "premultiplied violations clamped: " + PremulViolations;
    }
}
