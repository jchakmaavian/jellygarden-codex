// ColorBall_TargetGlow: board-space "charged / marked" highlight on a piece hit by the Color Ball beam.
// 256x256 canvas, pivot (0.5, 0.5) = (128, 128). Board cell = 192 px. Piece footprint = rounded square 176 px, corner radius 44.
// Clips (60 fps):
//   On   10 frames: flash bloom + over-bright rim + one chunky white star + tiny gold sparkles, settling so On f10 == Loop f1.
//   Loop 30 frames, seamless: cream rim (breathing +-12%, period 30), gold outer glow, translucent warm fill,
//        3-5 inner crackle streaks re-randomised every 2 frames (15 seeds repeat), 2 rim glints at different phases.
// C# 5 only. Depends on BoosterFx.cs (Img, Curve, Hash). Helpers from ColorBallSeq.cs are copied locally, not referenced.
using System;
using System.IO;
using System.Text;

public static class VfxColorBallTargetGlow
{
    public const string AssetName = "ColorBall_TargetGlow";
    public const int Size = 256;
    public const double Cx = 128, Cy = 128, Half = 88, Corner = 44;
    public const int OnFrames = 10, LoopFrames = 30;

    const double RimC = -2.5;      // rim centreline: px inside the footprint outline
    const double GlowCut = 36;     // outer glow is exactly 0 this far beyond the outline (4 px from the canvas edge)
    const double FillMax = 0.19;   // fill alpha at the centre (x1.06 at breathing peak => 0.201)

    // House palette (straight RGB).
    static readonly float[] White = { 1f, 1f, 1f };
    static readonly float[] WarmWhite = { 1f, 0.973f, 0.902f };   // #FFF8E6
    static readonly float[] PaleGold = { 1f, 0.878f, 0.541f };    // #FFE08A
    static readonly float[] Gold = { 1f, 0.761f, 0.227f };        // #FFC23A
    static readonly float[] BeamHalo = { 1f, 0.824f, 0.290f };    // #FFD24A
    static readonly float[] Lemon = { 1f, 0.945f, 0.700f };       // light lemon between warm white and pale gold
    static readonly float[] Cream = { 1f, 0.953f, 0.816f };       // #FFF3D0 rim tint at the breathing trough
    static readonly float[] RestWhite = { 1f, 0.981f, 0.931f };   // WarmWhite 70% + White 30%: rim tint at rest

    // ------------------------------------------------------------------ geometry
    // Signed distance to the footprint outline (negative inside).
    public static double Sdf(double x, double y)
    {
        double px = Math.Abs(x - Cx), py = Math.Abs(y - Cy);
        double qx = px - (Half - Corner), qy = py - (Half - Corner);
        double ox = Math.Max(qx, 0), oy = Math.Max(qy, 0);
        return Math.Sqrt(ox * ox + oy * oy) + Math.Min(Math.Max(qx, qy), 0) - Corner;
    }

    public static double PerimLen { get { return 8 * (Half - Corner) + 2 * Math.PI * Corner; } }

    // Point on the outline at u (0..1, clockwise on screen from the left end of the top edge) and its inward normal.
    public static void Perim(double u, out double x, out double y, out double nx, out double ny)
    {
        double e = 2 * (Half - Corner), arc = Math.PI * Corner / 2, h = Half - Corner, side = e + arc;
        u = ((u % 1) + 1) % 1;
        double s = u * 4 * side;
        int k = (int)Math.Floor(s / side); if (k > 3) k = 3;
        s -= k * side;
        double lx, ly, lnx, lny;
        if (s < e) { lx = -h + s; ly = -Half; lnx = 0; lny = 1; }
        else
        {
            double th = -Math.PI / 2 + (s - e) / Corner, c = Math.Cos(th), sn = Math.Sin(th);
            lx = h + Corner * c; ly = -h + Corner * sn; lnx = -c; lny = -sn;
        }
        // Rotate side 0 by k * 90 degrees clockwise on screen: (x, y) -> (-y, x).
        for (int r = 0; r < k; r++)
        {
            double t = lx; lx = -ly; ly = t;
            t = lnx; lnx = -lny; lny = t;
        }
        x = Cx + lx; y = Cy + ly; nx = lnx; ny = lny;
    }

    static int Mod(int a, int m) { return ((a % m) + m) % m; }

    static void AddMix(Img im, int i, float[] a, float[] b, double t, double w)
    {
        if (w <= 0) return;
        if (w > 1) w = 1;
        float ft = (float)t, fw = (float)w;
        float r = (a[0] + (b[0] - a[0]) * ft) * fw, g = (a[1] + (b[1] - a[1]) * ft) * fw, bl = (a[2] + (b[2] - a[2]) * ft) * fw;
        im.Add(i, r, g, bl, fw);
    }

    // ------------------------------------------------------------------ body: fill, outer glow, rim, flash
    // Light budget at the rim centreline (charged, br = 0): fill 0.065 + halo 0.24 + core 0.63 = ~0.93 alpha,
    // so the breathing (+-12% on rim, halo and glow; halo width +-10%; cream -> white tint) never hits the Img.Add clamp:
    // trough ~0.83, peak ~1.0 (just touching). rimMul > 1 (On clip) is the over-bright beat: saturated white core, wider core and 1.5x halo.
    static void DrawBody(Img im, double rimMul, double br, double glowMul, double fillMul, double flashI)
    {
        double over = Math.Max(0, rimMul - 1);
        double rimI = rimMul * (1 + 0.12 * br);
        double glowI = glowMul * (1 + 0.12 * br);
        double fillI = fillMul * (1 + 0.06 * br);
        double hw = 3.0 * (1 + 0.30 * over);                              // rim core half-width: 6 px core at rest
        double sigma = 5.5 * (1 + 0.10 * br) * (1 + 0.5 * Math.Min(1, over));   // halo width breathes; 1.5x on the On beat
        double gs = 1 + 0.06 * br;                                        // glow reach breathes a little
        // Core tint: warm cream at the trough (stays warm over the lavender board instead of greying), warm white at rest,
        // white-hot only toward breathing peaks and on the On over-bright beat.
        float[] coreA, coreB; double whiten;
        if (br >= 0) { coreA = WarmWhite; coreB = White; whiten = 0.3 + 0.7 * br; }
        else { coreA = Cream; coreB = RestWhite; whiten = 1 + br; }
        double hot = Curve.Smooth(1.0, 1.4, rimMul);
        if (hot > 0) { var ca = new float[3]; for (int c = 0; c < 3; c++) ca[c] = coreA[c] + (coreB[c] - coreA[c]) * (float)whiten; coreA = ca; coreB = White; whiten = hot; }
        for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                double d = Sdf(x + 0.5, y + 0.5);
                if (d >= GlowCut) continue;
                int i = y * Size + x;
                // Interior gradient on a squircle norm (p = 4 matches the rounded square closely) so the fill has no
                // medial-axis creases: k = 1 at the centre, 0 at the outline.
                double ux = (x + 0.5 - Cx) / Half, uy = (y + 0.5 - Cy) / Half;
                double qn = Math.Sqrt(Math.Sqrt(ux * ux * ux * ux + uy * uy * uy * uy));
                double k = 1 - Curve.Smooth(0.0, 0.98, qn);
                double inside = Curve.Smooth(1.0, -2.0, d);
                // Translucent warm lemon fill, strongest at the centre, lower toward the rim.
                if (inside > 0)
                {
                    double fw = FillMax * (0.34 + 0.66 * k) * fillI * inside;
                    AddMix(im, i, PaleGold, Lemon, 0.35 + 0.65 * k, fw);
                }
                // Warm gold outer glow from the outline outward.
                double gd = Math.Max(0, d);
                double g1 = gd / (8 * gs), g2 = gd / (10.5 * gs);
                double gw = glowI * (0.30 * Math.Exp(-g1 * g1) + 0.24 * Math.Exp(-g2))
                            * Curve.Smooth(GlowCut, 18, gd) * Curve.Smooth(-1, 2.5, d);
                AddMix(im, i, BeamHalo, Gold, Curve.Smooth(0, 22, gd), gw);
                // Rim: soft pale-gold halo plus a cream/white 6 px core with soft 4 px edges.
                double xr = d - RimC, ax = Math.Abs(xr);
                double halo = Math.Exp(-(xr / sigma) * (xr / sigma)) * 0.24 * rimI;
                AddMix(im, i, PaleGold, WarmWhite, 0.3, halo);
                double core = (1 - Curve.Smooth(hw - 2.0, hw + 2.0, ax)) * 0.63 * rimI;
                AddMix(im, i, coreA, coreB, whiten, core);
                // On-clip flash bloom over the whole piece (white centre, gold edge, short bloom outside).
                if (flashI > 0)
                {
                    double fl;
                    if (d < 0) fl = flashI * (0.74 + 0.26 * Math.Sqrt(k));
                    else fl = flashI * 0.74 * Math.Exp(-(d / 11) * (d / 11)) * Curve.Smooth(GlowCut, 20, d);
                    if (d < 0) AddMix(im, i, WarmWhite, White, Math.Sqrt(k), fl);
                    else AddMix(im, i, WarmWhite, BeamHalo, Curve.Smooth(0, 12, d), fl);
                }
            }
    }

    // ------------------------------------------------------------------ crackle streaks
    // Soft polyline: 2x2-supersampled white core that tapers to both ends, pale-gold halo. t0..t1 remaps the taper; skew < 1 moves the thickest point toward the start.
    static void Polyline(Img im, double[] xs, double[] ys, int n, double coreSigma, double haloSigma, double coreI, double haloI, double t0, double t1, double skew)
    {
        if (coreI <= 0 || n < 2) return;
        var cum = new double[n];
        for (int j = 1; j < n; j++) cum[j] = cum[j - 1] + Math.Sqrt((xs[j] - xs[j - 1]) * (xs[j] - xs[j - 1]) + (ys[j] - ys[j - 1]) * (ys[j] - ys[j - 1]));
        double total = cum[n - 1]; if (total < 0.5) return;
        double pad = haloSigma * 3 + 2;
        double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
        for (int j = 0; j < n; j++) { minX = Math.Min(minX, xs[j]); maxX = Math.Max(maxX, xs[j]); minY = Math.Min(minY, ys[j]); maxY = Math.Max(maxY, ys[j]); }
        int xa = Math.Max(0, (int)Math.Floor(minX - pad)), xb = Math.Min(Size - 1, (int)Math.Ceiling(maxX + pad));
        int ya = Math.Max(0, (int)Math.Floor(minY - pad)), yb = Math.Min(Size - 1, (int)Math.Ceiling(maxY + pad));
        for (int y = ya; y <= yb; y++)
            for (int x = xa; x <= xb; x++)
            {
                double coreAcc = 0;
                for (int sj = 0; sj < 2; sj++)
                    for (int si = 0; si < 2; si++)
                    {
                        double dd, tt; Nearest(xs, ys, cum, n, x + (si + 0.5) / 2, y + (sj + 0.5) / 2, out dd, out tt);
                        double tp = Taper(t0 + (t1 - t0) * Math.Pow(tt, skew));
                        double sg = coreSigma * (0.45 + 0.55 * tp);
                        coreAcc += Math.Exp(-dd / (2 * sg * sg)) * tp;
                    }
                double d2, t; Nearest(xs, ys, cum, n, x + 0.5, y + 0.5, out d2, out t);
                double tph = Taper(t0 + (t1 - t0) * Math.Pow(t, skew));
                double cw = coreAcc / 4 * coreI;
                double hwt = Math.Exp(-d2 / (2 * haloSigma * haloSigma)) * tph * haloI;
                int i = y * Size + x;
                if (hwt > 0.002) AddMix(im, i, PaleGold, WarmWhite, 0.25, hwt);
                if (cw > 0.002) AddMix(im, i, WarmWhite, White, 0.8, cw);
            }
    }

    static double Taper(double t)
    {
        if (t <= 0 || t >= 1) return 0;
        return Math.Pow(Math.Sin(Math.PI * t), 0.6);
    }

    // Squared distance to the polyline and normalised arc position of the nearest point.
    static void Nearest(double[] xs, double[] ys, double[] cum, int n, double px, double py, out double d2, out double t)
    {
        d2 = double.MaxValue; t = 0;
        double total = cum[n - 1];
        for (int j = 0; j < n - 1; j++)
        {
            double vx = xs[j + 1] - xs[j], vy = ys[j + 1] - ys[j], l2 = vx * vx + vy * vy;
            double u = l2 > 1e-9 ? ((px - xs[j]) * vx + (py - ys[j]) * vy) / l2 : 0;
            u = u < 0 ? 0 : u > 1 ? 1 : u;
            double qx = xs[j] + vx * u - px, qy = ys[j] + vy * u - py, dd = qx * qx + qy * qy;
            if (dd < d2) { d2 = dd; t = (cum[j] + (cum[j + 1] - cum[j]) * u) / total; }
        }
    }

    const int CrackleRot = 10;
    // 3-5 jagged flame/crackle streaks running just inside the rim. Same image for 2 frames; 15 images per 30-frame loop.
    static void DrawCrackles(Img im, int phase, double intensity)
    {
        if (intensity <= 0) return;
        int ph = Mod(phase, LoopFrames);
        // The 15-image cycle is rotated so the loop wrap (f30 -> f1) falls on one of the milder image changes
        // (the largest change, image 14 -> 0, now sits mid-loop). Same images, same cyclic order.
        int pair = (ph / 2 + CrackleRot) % (LoopFrames / 2);
        int s = 7100 + pair * 131;
        double flick = (ph % 2 == 0) ? 1.0 : 0.84;   // second frame of each pair a touch dimmer (periodic)
        int count = 3 + (int)(Hash.U(s, 1) * 3);
        if (count > 5) count = 5;
        double rot = Hash.U(s, 2);
        for (int k = 0; k < count; k++)
        {
            // Flame wisp: a long jagged stroke hugging the rim (random walk in inset with occasional sharp kinks),
            // thickest a third of the way along, plus 1-3 short feather barbs swept forward.
            int b = 1000 + k * 200;
            double u0 = rot + (k + 0.25 + 0.5 * Hash.U(s, b)) / count;
            double len = 46 + 30 * Hash.U(s, b + 1);
            int n = 10 + (int)(Hash.U(s, b + 2) * 4);
            double inset = 10 + 8 * Hash.U(s, b + 3);
            double drift = (Hash.U(s, b + 4) - 0.3) * 14 / (n - 1);
            bool reverse = Hash.U(s, b + 5) < 0.5;
            var xs = new double[n]; var ys = new double[n]; var us = new double[n];
            for (int j = 0; j < n; j++)
            {
                if (j > 0)
                {
                    double step = drift + (Hash.U(s, b + 10 + j) - 0.5) * 2.4;
                    if (Hash.U(s, b + 30 + j) < 0.32) step += (Hash.U(s, b + 50 + j) < 0.5 ? -1 : 1) * (2.5 + 2.5 * Hash.U(s, b + 70 + j));
                    inset += step;
                }
                inset = Math.Max(9, Math.Min(30, inset));
                double t = j / (double)(n - 1);
                double px, py, nx, ny; us[j] = u0 + (reverse ? 1 - t : t) * len / PerimLen;
                Perim(us[j], out px, out py, out nx, out ny);
                xs[j] = px + nx * inset; ys[j] = py + ny * inset;
            }
            double ki = intensity * flick * (0.80 + 0.20 * Hash.U(s, b + 6));
            Polyline(im, xs, ys, n, 0.95, 3.2, ki, ki * 0.32, 0, 1, 0.7);
            int barbs = 1 + (int)(Hash.U(s, b + 7) * 3);
            for (int m = 0; m < barbs; m++)
            {
                int c = b + 100 + m * 10;
                int j0 = 2 + (int)(Hash.U(s, c) * (n - 5));
                double tx = xs[j0 + 1] - xs[j0 - 1], ty = ys[j0 + 1] - ys[j0 - 1], tl = Math.Sqrt(tx * tx + ty * ty);
                if (tl < 1e-6) continue;
                tx /= tl; ty /= tl;
                double px, py, nx, ny; Perim(us[j0], out px, out py, out nx, out ny);
                double side = Hash.U(s, c + 1) < 0.75 ? 1 : -1, ang = (25 + 25 * Hash.U(s, c + 2)) * Math.PI / 180;
                double dx = tx * Math.Cos(ang) + nx * Math.Sin(ang) * side, dy = ty * Math.Cos(ang) + ny * Math.Sin(ang) * side;
                double bl = 7 + 9 * Hash.U(s, c + 3), jit = (Hash.U(s, c + 4) - 0.5) * 3.5;
                // Keep barbs inside: one whose tip would reach the rim core is swept inward instead.
                if (Sdf(xs[j0] + dx * bl, ys[j0] + dy * bl) > -7)
                {
                    side = -side;
                    dx = tx * Math.Cos(ang) + nx * Math.Sin(ang) * side; dy = ty * Math.Cos(ang) + ny * Math.Sin(ang) * side;
                }
                var fx = new double[3]; var fy = new double[3];
                fx[0] = xs[j0]; fy[0] = ys[j0];
                fx[1] = xs[j0] + dx * bl * 0.5 - dy * jit; fy[1] = ys[j0] + dy * bl * 0.5 + dx * jit;
                fx[2] = xs[j0] + dx * bl; fy[2] = ys[j0] + dy * bl;
                Polyline(im, fx, fy, 3, 0.75, 2.4, ki * 0.85, ki * 0.22, 0.35, 1, 1.0);
            }
        }
    }

    // ------------------------------------------------------------------ glints and stars
    static readonly double[] GlintU = { 0.47, 0.905 };
    static readonly int[] GlintStart = { 3, 17 };
    const int GlintLen = 10;

    static void DrawGlints(Img im, int phase)
    {
        int ph = Mod(phase, LoopFrames);
        for (int g = 0; g < GlintU.Length; g++)
        {
            int q = Mod(ph - GlintStart[g], LoopFrames);
            if (q >= GlintLen) continue;
            double env = Math.Pow(Math.Sin(Math.PI * (q + 1) / (GlintLen + 1)), 1.3);
            double px, py, nx, ny; Perim(GlintU[g], out px, out py, out nx, out ny);
            // Centred 2 px outside the outline so the arms reach into the gold glow instead of vanishing in the white rim.
            px -= nx * 2; py -= ny * 2;
            Tinted(im, px, py, 10 * env, 0.50 * env, PaleGold);
            FatStar(im, px, py, 15 * env, -6 + 1.2 * q, 1.0, 0.9);
        }
    }

    // Copied from CB.Tinted (ColorBallSeq.cs): additive tinted light blob.
    static void Tinted(Img dst, double cx, double cy, double radius, double intensity, float[] rgb)
    {
        if (intensity <= 0 || radius <= 0) return;
        int R = (int)Math.Ceiling(radius * 2.2);
        for (int y = Math.Max(0, (int)cy - R); y < Math.Min(dst.H, (int)cy + R + 1); y++)
            for (int x = Math.Max(0, (int)cx - R); x < Math.Min(dst.W, (int)cx + R + 1); x++)
            {
                double dx = x + 0.5 - cx, dy = y + 0.5 - cy, q = (dx * dx + dy * dy) / (radius * radius);
                double w = Math.Exp(-q * 2.2) * intensity; if (w < 0.003) continue;
                dst.Add(y * dst.W + x, (float)(rgb[0] * w), (float)(rgb[1] * w), (float)(rgb[2] * w), (float)Math.Min(1, w));
            }
    }

    // Copied from CB.WhiteGlow (ColorBallSeq.cs): neutral-white additive halo.
    static void WhiteGlow(Img dst, double cx, double cy, double radius, double intensity)
    {
        if (intensity <= 0 || radius <= 0) return;
        int R = (int)Math.Ceiling(radius * 2.0);
        for (int y = Math.Max(0, (int)cy - R); y < Math.Min(dst.H, (int)cy + R + 1); y++)
            for (int x = Math.Max(0, (int)cx - R); x < Math.Min(dst.W, (int)cx + R + 1); x++)
            {
                double dx = x + 0.5 - cx, dy = y + 0.5 - cy, q = (dx * dx + dy * dy) / (radius * radius);
                double w = Math.Exp(-q * 2.2) * intensity; if (w < 0.002) continue;
                dst.Add(y * dst.W + x, (float)(1.0 * w), (float)(0.99 * w), (float)(0.96 * w), (float)Math.Min(1, w));
            }
    }

    // Copied from CB.FatStar (ColorBallSeq.cs): chunky concave four-point star, white-hot centre, 3x3 supersampled.
    static void FatStar(Img dst, double cx, double cy, double size, double deg, double intensity, double heat)
    {
        if (intensity <= 0 || size <= 0.3) return;
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

    // ------------------------------------------------------------------ frames
    static void DrawCharged(Img im, int phase, double rimMul, double glowMul, double fillMul, double crackMul, double flash, bool glints)
    {
        double br = Math.Sin(2 * Math.PI * Mod(phase, LoopFrames) / LoopFrames);
        DrawBody(im, rimMul, br, glowMul, fillMul, flash);
        DrawCrackles(im, phase, crackMul);
        if (glints) DrawGlints(im, phase);
    }

    public static Img LoopFrame(int f)
    {
        var im = new Img(Size, Size);
        DrawCharged(im, f, 1, 1, 1, 1, 0, true);
        Finish(im);
        return im;
    }

    // On keys (frame index 0..9); every key ends on exactly the Loop f1 value at index 9.
    static readonly double[] K_OnRim = { 0, 0.55, 1, 1.55, 2, 1.45, 4, 1.22, 6, 1.07, 9, 1.0 };
    static readonly double[] K_OnGlow = { 0, 1.55, 1, 1.50, 3, 1.22, 6, 1.05, 9, 1.0 };
    // Flash peak 0.70 so fill + flash stays ~0.88 alpha at the centre on f1: a faint piece silhouette survives the white-out.
    static readonly double[] K_OnFlash = { 0, 0.70, 1, 0.46, 2, 0.26, 3, 0.13, 5, 0.05, 7, 0.012, 9, 0.0 };
    static readonly double[] K_OnCrackle = { 0, 0.35, 2, 1.0, 9, 1.0 };
    static readonly double[] K_Star = { 0, 0.0, 1, 0.92, 2, 1.0, 4, 0.70, 6, 0.28, 7, 0.0 };
    const double StarX = 193, StarY = 63, StarSize = 35;

    static readonly double[] SparkU = { 0.60, 0.36, 0.83, 0.03 };
    static readonly int[] SparkBorn = { 1, 1, 2, 2 };
    const int SparkLife = 5;

    public static Img OnFrame(int f)
    {
        var im = new Img(Size, Size);
        int phase = f - (OnFrames - 1);   // On f10 (index 9) is Loop phase 0
        double flash = Curve.Eval(K_OnFlash, f);
        DrawCharged(im, phase, Curve.Eval(K_OnRim, f), Curve.Eval(K_OnGlow, f), 1, Curve.Eval(K_OnCrackle, f), flash, f == OnFrames - 1);
        if (f < OnFrames - 1)
        {
            // Chunky white star popping at the top-right corner.
            double st = Curve.Eval(K_Star, f);
            if (st > 0.001)
            {
                WhiteGlow(im, StarX, StarY, 22 * st, 0.55 * st);
                FatStar(im, StarX, StarY, StarSize * st, -6 + 4 * f, 1.0, 0.95);
            }
            // Tiny gold sparkles just outside the rim, drifting outward.
            for (int k = 0; k < SparkU.Length; k++)
            {
                int age = f - SparkBorn[k];
                if (age < 0 || age >= SparkLife) continue;
                double u = age / (double)SparkLife;
                // Shrink only to ~65% and dim late, so the last visible frame is still a small recognisable star (no dust specks).
                double env = 1 - 0.55 * u * u, fade = 1 - 0.6 * u * u;
                double px, py, nx, ny; Perim(SparkU[k], out px, out py, out nx, out ny);
                double off = 5 + 1.4 * age;
                double size = (7 + 3 * Hash.U(8801, k)) * env;
                FatStar(im, px - nx * off, py - ny * off, size, 10 * Hash.U(8802, k) - 5, 0.95 * fade, 0.42);
            }
        }
        Finish(im);
        return im;
    }

    // Remove sub-threshold alpha dust so nothing faint is left scattered.
    static void Finish(Img im)
    {
        float cut = 2f / 255f;
        for (int i = 0; i < im.A.Length; i++)
            if (im.A[i] < cut) { im.A[i] = 0; im.R[i] = 0; im.G[i] = 0; im.B[i] = 0; }
    }

    // Premultiplied sanity for a frame buffer: texels with a colour channel above alpha.
    static int PremulViolations(Img im)
    {
        int n = 0;
        for (int i = 0; i < im.A.Length; i++)
            if (im.R[i] > im.A[i] + 1e-5f || im.G[i] > im.A[i] + 1e-5f || im.B[i] > im.A[i] + 1e-5f) n++;
        return n;
    }

    // Tuning probe (not used by Render): body-only rim/glow profile and composited band luminance at breathing trough/rest/peak
    // and at the On over-bright beat (rimMul 2). Band metrics mirror the review: linear luminance of the sRGB-composited texel.
    public static string Probe()
    {
        var sb = new StringBuilder();
        double[][] bgs = { new double[] { 0x20 / 255.0, 0x24 / 255.0, 0x2e / 255.0 }, new double[] { 0x8a / 255.0, 0x93 / 255.0, 0xc8 / 255.0 } };
        double[] brs = { -1, 0, 1, 0 };
        double[] rms = { 1, 1, 1, 2 };
        string[] names = { "trough", "rest", "peak", "onBeat" };
        var band = new double[4, 2, 3];
        for (int c = 0; c < 4; c++)
        {
            var im = new Img(Size, Size);
            DrawBody(im, rms[c], brs[c], rms[c] > 1 ? 1.5 : 1, 1, 0);
            var row = new StringBuilder();
            for (int x = 200; x <= 248; x += (x < 210 ? 2 : x < 224 ? 1 : 2))
            {
                int i = 128 * Size + x; float a = im.A[i];
                row.Append(string.Format("{0}:{1}", x - 216, (int)Math.Round(a * 255)));
                if (a > 0.01f) row.Append(string.Format("({0:0.00},{1:0.00},{2:0.00}) ", im.R[i] / a, im.G[i] / a, im.B[i] / a)); else row.Append(" ");
            }
            sb.AppendLine(names[c] + " row y=128 [d:alpha(straight rgb)]: " + row);
            for (int b = 0; b < 2; b++)
            {
                double sr = 0, sc = 0, sg = 0; int nr = 0, nc = 0, ng = 0;
                for (int y = 0; y < Size; y++)
                    for (int x = 0; x < Size; x++)
                    {
                        double d = Sdf(x + 0.5, y + 0.5);
                        if (d < -9 || d > 20) continue;
                        int i = y * Size + x; double a = Math.Round(im.A[i] * 255) / 255.0;
                        double r = Math.Min(1, im.R[i]) + bgs[b][0] * (1 - a), g = Math.Min(1, im.G[i]) + bgs[b][1] * (1 - a), bl = Math.Min(1, im.B[i]) + bgs[b][2] * (1 - a);
                        double L = 0.2126 * Lin(r) + 0.7152 * Lin(g) + 0.0722 * Lin(bl);
                        if (d <= 3) { sr += L; nr++; }
                        if (d >= -5 && d <= 0) { sc += L; nc++; }
                        if (d > 3) { sg += L; ng++; }
                    }
                band[c, b, 0] = sr / nr; band[c, b, 1] = sc / nc; band[c, b, 2] = sg / ng;
            }
        }
        string[] bn = { "dark", "lavender" };
        string[] kn = { "rimBand[-9,3]", "core[-5,0]", "glow(3,20]" };
        for (int b = 0; b < 2; b++)
            for (int k = 0; k < 3; k++)
                sb.AppendLine(string.Format("{0} {1}: trough {2:0.0000} ({3:+0.0;-0.0}%) rest {4:0.0000} peak {5:0.0000} ({6:+0.0;-0.0}%) onBeat {7:0.0000} ({8:+0.0;-0.0}%)",
                    bn[b], kn[k], band[0, b, k], (band[0, b, k] / band[1, b, k] - 1) * 100, band[1, b, k], band[2, b, k], (band[2, b, k] / band[1, b, k] - 1) * 100,
                    band[3, b, k], (band[3, b, k] / band[1, b, k] - 1) * 100));
        return sb.ToString();
    }

    static double Lin(double c) { return c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4); }

    public static string Render(string outRoot, int from, int to)
    {
        return RenderClip(outRoot, "On", from, to) + RenderClip(outRoot, "Loop", from, to);
    }

    public static string RenderClip(string outRoot, string clip, int from, int to)
    {
        bool on = clip == "On";
        int n = on ? OnFrames : LoopFrames;
        string dir = Path.Combine(Path.Combine(outRoot, clip), "frames");
        Directory.CreateDirectory(dir);
        var sb = new StringBuilder();
        for (int f = Math.Max(0, from); f <= Math.Min(n - 1, to); f++)
        {
            var im = on ? OnFrame(f) : LoopFrame(f);
            im.Save(Path.Combine(dir, string.Format("{0}_{1:000}.png", AssetName, f + 1)));
            sb.AppendLine(string.Format("{0} f{1:000} premulViolations={2}", clip, f + 1, PremulViolations(im)));
        }
        return sb.ToString();
    }
}
