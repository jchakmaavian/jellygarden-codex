// Royal Throne Color Ball sequences: Reveal, Idle, Boosting. 512x512 canvas, 60 fps.
// The ball is drawn at content scale Kc about its sphere centre, which sits at the canvas centre in every frame.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

public static class CB
{
    public static double Kc = 0.60;
    public const double Ox = 256, Oy = 256;
    public static double BallR { get { return BallRig.R * Kc; } }

    // Our stud palette (straight RGB) for fringes, sparks and bands.
    public static readonly float[][] Palette =
    {
        new[] { 0.10f, 0.66f, 0.95f }, // cyan-blue
        new[] { 0.28f, 0.80f, 0.16f }, // green
        new[] { 1.00f, 0.80f, 0.10f }, // yellow
        new[] { 1.00f, 0.50f, 0.10f }, // orange
        new[] { 0.98f, 0.18f, 0.24f }, // red
        new[] { 0.95f, 0.30f, 0.92f }, // magenta
    };

    // ------------------------------------------------------------------ ball drawing
    public static Affine BallPose(double scale, double sx, double sy, double rotDeg, double dx = 0, double dy = 0)
    {
        return Affine.Pose(BallRig.Cx, BallRig.Cy, Kc * scale * sx, Kc * scale * sy, rotDeg, Ox + dx, Oy + dy);
    }

    // Spin styling: lift the navy body toward a luminous lavender, brighten/saturate studs, optional white-out.
    public static Img Energize(BallLayers l, double bodyLift, double studLift, double studGain, double whiteOut)
    {
        int n = BallRig.N * BallRig.N;
        var o = new Img(BallRig.N, BallRig.N);
        float bl = (float)bodyLift, sl = (float)studLift, sg = (float)studGain, wo = (float)whiteOut;
        for (int i = 0; i < n; i++)
        {
            float ba = l.Body.A[i], ta = l.Studs.A[i];
            if (ba <= 0 && ta <= 0) continue;
            float br = 0, bg = 0, bb = 0;
            if (ba > 0)
            {
                br = l.Body.R[i] / ba; bg = l.Body.G[i] / ba; bb = l.Body.B[i] / ba;
                br += (0.80f - br) * bl; bg += (0.80f - bg) * bl; bb += (0.98f - bb) * bl;
            }
            float r = br * ba, g = bg * ba, b = bb * ba, a = ba;
            if (ta > 0)
            {
                float tr = l.Studs.R[i] / ta, tg = l.Studs.G[i] / ta, tb = l.Studs.B[i] / ta;
                // Saturation/brightness gain around the stud's own luminance, then a lift toward white.
                float lum = 0.3f * tr + 0.59f * tg + 0.11f * tb;
                tr = lum + (tr - lum) * (1 + sg * 0.35f); tg = lum + (tg - lum) * (1 + sg * 0.35f); tb = lum + (tb - lum) * (1 + sg * 0.35f);
                tr *= 1 + sg * 0.25f; tg *= 1 + sg * 0.25f; tb *= 1 + sg * 0.25f;
                tr += (1 - tr) * sl; tg += (1 - tg) * sl; tb += (1 - tb) * sl;
                tr = Math.Max(0, Math.Min(1, tr)); tg = Math.Max(0, Math.Min(1, tg)); tb = Math.Max(0, Math.Min(1, tb));
                // Blurred studs read as light: coverage is strengthened so thin streaks stay saturated.
                float cov = (float)Math.Min(1, 1.25 * Math.Pow(ta, 0.8));
                r = tr * cov + r * (1 - cov); g = tg * cov + g * (1 - cov); b = tb * cov + b * (1 - cov); a = cov + a * (1 - cov);
            }
            if (wo > 0 && a > 0)
            {
                float sr = r / a, sgg = g / a, sb = b / a;
                sr += (1.00f - sr) * wo; sgg += (0.98f - sgg) * wo; sb += (0.90f - sb) * wo;
                r = sr * a; g = sgg * a; b = sb * a;
            }
            o.R[i] = r; o.G[i] = g; o.B[i] = b; o.A[i] = a;
        }
        return o;
    }

    public static Img Original()
    {
        return BallRig.Src;
    }

    // ------------------------------------------------------------------ VFX helpers (canvas space)
    // Additive tinted light blob.
    public static void Tinted(Img dst, double cx, double cy, double radius, double intensity, float[] rgb)
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

    // Thin tapered light spike through (cx, cy) at angle (deg, 0 = up), total length L (both directions), width at centre w.
    public static void Spike(Img dst, double cx, double cy, double angleDeg, double length, double width, double intensity)
    {
        if (intensity <= 0 || length <= 0) return;
        double a = angleDeg * Math.PI / 180, ux = Math.Sin(a), uy = -Math.Cos(a);
        int R = (int)Math.Ceiling(length / 2 + width * 3);
        for (int y = Math.Max(0, (int)cy - R); y < Math.Min(dst.H, (int)cy + R + 1); y++)
            for (int x = Math.Max(0, (int)cx - R); x < Math.Min(dst.W, (int)cx + R + 1); x++)
            {
                double px = x + 0.5 - cx, py = y + 0.5 - cy, along = px * ux + py * uy, across = -px * uy + py * ux;
                double t = Math.Abs(along) / (length / 2); if (t >= 1) continue;
                double wd = width * (1 - 0.85 * t);
                double w = Math.Exp(-(across * across) / (2 * wd * wd)) * Math.Pow(1 - t, 2.2) * intensity;
                if (w < 0.003) continue;
                float r, g, b; Fx.Heat(0.75 + 0.25 * (1 - t), out r, out g, out b);
                dst.Add(y * dst.W + x, (float)(r * w), (float)(g * w), (float)(b * w), (float)Math.Min(1, w));
            }
    }

    // White-hot spiky swell: a bright core disc whose rim is broken into irregular spikes, with a warm outer glow.
    public static void SpikyBurst(Img dst, double cx, double cy, double radius, double spikeLen, double intensity, int seed, double spinDeg)
    {
        if (intensity <= 0 || radius <= 0) return;
        int R = (int)Math.Ceiling(radius * 1.9 + spikeLen + 8);
        const int SP = 28;
        var len = new double[SP]; var wid = new double[SP];
        for (int k = 0; k < SP; k++) { len[k] = 0.35 + 0.65 * Hash.U(seed, k); wid[k] = 0.35 + 0.65 * Hash.U(seed, k + 100); }
        double spin = spinDeg * Math.PI / 180;
        for (int y = Math.Max(0, (int)cy - R); y < Math.Min(dst.H, (int)cy + R + 1); y++)
            for (int x = Math.Max(0, (int)cx - R); x < Math.Min(dst.W, (int)cx + R + 1); x++)
            {
                double dx = x + 0.5 - cx, dy = y + 0.5 - cy, r = Math.Sqrt(dx * dx + dy * dy);
                double ang = Math.Atan2(dy, dx) - spin; ang = ((ang % (2 * Math.PI)) + 2 * Math.PI) % (2 * Math.PI);
                double pos = ang / (2 * Math.PI) * SP; int k0 = (int)Math.Floor(pos) % SP; double fk = pos - Math.Floor(pos);
                // Spike envelope: sharp triangular teeth between neighbouring spike centres.
                double tooth = Math.Max(0, 1 - Math.Abs(fk - 0.5) * 2 / wid[k0]);
                double edge = radius + spikeLen * len[k0] * Math.Pow(tooth, 1.6);
                double inside = Curve.Smooth(edge + 3, edge - 3, r);
                double core = Curve.Smooth(radius * 1.05, radius * 0.55, r);
                double glow = Math.Exp(-Math.Pow(Math.Max(0, r - radius * 0.7) / (radius * 0.45 + spikeLen * 0.5), 2) * 2.4) * 0.5;
                double w = Math.Max(inside * 0.95, glow) * intensity;
                if (w < 0.003) continue;
                // White core, warm yellow rim and spikes, gold glow.
                float cr, cg, cb; Fx.Heat(0.30 + 0.70 * Math.Max(core, inside * 0.45), out cr, out cg, out cb);
                dst.Add(y * dst.W + x, (float)(cr * w), (float)(cg * w), (float)(cb * w), (float)Math.Min(1, w));
            }
    }

    // Soft neutral-white additive halo (no gold ramp), gaussian falloff. squashY < 1 flattens it.
    public static void WhiteGlow(Img dst, double cx, double cy, double radius, double intensity, double squashY = 1)
    {
        if (intensity <= 0 || radius <= 0) return;
        int R = (int)Math.Ceiling(radius * 2.0);
        for (int y = Math.Max(0, (int)cy - R); y < Math.Min(dst.H, (int)cy + R + 1); y++)
            for (int x = Math.Max(0, (int)cx - R); x < Math.Min(dst.W, (int)cx + R + 1); x++)
            {
                double dx = x + 0.5 - cx, dy = (y + 0.5 - cy) / squashY, q = (dx * dx + dy * dy) / (radius * radius);
                double w = Math.Exp(-q * 2.2) * intensity; w *= Window(Math.Sqrt(dx * dx + (y + 0.5 - cy) * (y + 0.5 - cy))); if (w < 0.002) continue;
                dst.Add(y * dst.W + x, (float)(1.0 * w), (float)(0.99 * w), (float)(0.96 * w), (float)Math.Min(1, w));
            }
    }

    // Colour mix weights for chromatic rim tips: the reference fringe is mostly magenta/pink and cyan, some green/yellow.
    static readonly int[] TipColours = { 5, 0, 5, 1, 0, 2, 5, 0, 4, 1, 2, 0 };

    /// <summary>
    /// White-hot swell around a spinning ball: soft cream-white core light (lets the white-out ball's bands read through),
    /// an irregular, horizontally biased rim of jagged spikes whose tips pick up our stud colours, and a warm cream glow.
    /// radius = rim radius in canvas px.
    /// </summary>
    public static void WhiteHotSwell(Img dst, double cx, double cy, double radius, double intensity, int seed, double spinDeg, double coreLight, int pass)
    {
        if (intensity <= 0 || radius <= 0) return;
        // Tears at random angles (no regular lattice), mostly on the left and right: spin about the vertical axis smears the
        // white sideways. Each tear has its own width, length and fringe colour (magenta/blue below, cyan/green above, warm sides).
        const int NS = 14;
        var ta0 = new double[NS]; var tw0 = new double[NS]; var tl0 = new double[NS]; var col = new int[NS];
        for (int k = 0; k < NS; k++)
        {
            double side = Hash.U(seed, k + 600) < 0.5 ? 0 : Math.PI;
            ta0[k] = side + (Hash.U(seed, k + 400) - 0.5) * 2 * 1.05;          // within about +-60 deg of horizontal
            tw0[k] = 0.05 + 0.11 * Hash.U(seed, k + 500);                       // half-width (rad)
            tl0[k] = 0.04 + 0.26 * Math.Pow(Hash.U(seed, k), 1.3);
            double sa = Math.Sin(ta0[k]), pick = Hash.U(seed, k + 200);
            col[k] = Math.Abs(sa) < 0.35 ? (pick < 0.5 ? 2 : 5) : sa > 0 ? (pick < 0.6 ? 5 : 0) : (pick < 0.6 ? 0 : 1);
        }
        int R = (int)Math.Ceiling(radius * 1.75 + 6);
        double spin = spinDeg * Math.PI / 180;
        for (int y = Math.Max(0, (int)cy - R); y < Math.Min(dst.H, (int)cy + R + 1); y++)
            for (int x = Math.Max(0, (int)cx - R); x < Math.Min(dst.W, (int)cx + R + 1); x++)
            {
                double acc = 0, tipAcc = 0, rimAcc = 0, cr0 = 0, cg0 = 0, cb0 = 0;
                double dy0 = y + 0.5 - cy, dx0 = x + 0.5 - cx, r0 = Math.Sqrt(dx0 * dx0 + dy0 * dy0);
                if (r0 > R) continue;
                for (int sj = 0; sj < 2; sj++)
                    for (int si = 0; si < 2; si++)
                    {
                        double dx = x + (si + 0.5) / 2 - cx, dy = y + (sj + 0.5) / 2 - cy, r = Math.Sqrt(dx * dx + dy * dy);
                        double ang = Math.Atan2(dy, dx) - spin;
                        double cs2 = Math.Cos(ang) * Math.Cos(ang);
                        double tear = 0; int k0 = 0;
                        for (int k = 0; k < NS; k++)
                        {
                            double da = Math.Abs(ang - ta0[k]); da = da % (2 * Math.PI); if (da > Math.PI) da = 2 * Math.PI - da;
                            if (da >= tw0[k]) continue;
                            double ca = Math.Cos(ta0[k]);
                            double h = tl0[k] * 1.3 * Math.Pow(ca * ca, 2.0) * Math.Pow(1 - da / tw0[k], 1.6);
                            if (h > tear) { tear = h; k0 = k; }
                        }
                        double edge = radius * (1 + 0.07 * cs2 + tear);
                        double inside = Curve.Smooth(edge + 3.5, edge - 3.5, r);
                        acc += inside;
                        double beyond = Math.Max(0, (r - radius * 0.97) / Math.Max(1, edge - radius * 0.97));
                        tipAcc += inside * Curve.Smooth(0.15, 0.75, beyond);
                        rimAcc += inside * Curve.Smooth(radius * 0.72, radius * 0.98, r);
                        var pc = Palette[col[k0]]; cr0 += pc[0] + (1 - pc[0]) * 0.35; cg0 += pc[1] + (1 - pc[1]) * 0.35; cb0 += pc[2] + (1 - pc[2]) * 0.35;
                    }
                acc /= 4; tipAcc /= 4; rimAcc /= 4; cr0 /= 4; cg0 /= 4; cb0 /= 4;
                double rr = r0 / radius;
                // Faint horizontal spin banding inside the core.
                double band = 1 - 0.10 * (0.5 + 0.5 * Math.Sin(dy0 / radius * 9.0 + seed * 0.7));
                double core = Math.Exp(-rr * rr * 1.6) * coreLight * band;
                double glow = Math.Exp(-Math.Pow(Math.Max(0, rr - 0.85) / 0.38, 2) * 2.0) * 0.42;
                double rim = acc * 0.97 * (1 - 0.25 * Curve.Smooth(radius * 0.8, radius * 1.08, r0));
                // Pass 0 (behind the ball): solid jagged blob + glow + tips. Pass 1 (over the ball): soft core light only.
                double w = (pass == 0 ? Math.Max(rim, glow) : core) * intensity * Window(r0);
                if (w < 0.003) continue;
                // Cream-white body, warm cream glow, palette-coloured spike tips.
                float hr, hg, hb; Fx.Heat(pass == 1 ? 0.94 : 0.80 + 0.20 * Math.Max(0, 1 - rr * 0.75), out hr, out hg, out hb);
                double t = pass == 1 ? 0 : Math.Min(1, tipAcc * 1.25) * 0.5;
                double lr = hr * (1 - t) + cr0 * t, lg = hg * (1 - t) + cg0 * t, lb = hb * (1 - t) + cb0 * t;
                dst.Add(y * dst.W + x, (float)(lr * w), (float)(lg * w), (float)(lb * w), (float)Math.Min(1, w));
            }
    }

    // Soft white fill over the outer part of a white-hot ball and the swell around it: no dark ring at the ball silhouette,
    // while the centre keeps its moving spin bands.
    public static void SwellFill(Img dst, double cx, double cy, double ballR, double rimR, double intensity)
    {
        if (intensity <= 0 || rimR <= 0) return;
        int R = (int)Math.Ceiling(rimR * 1.1) + 2;
        for (int y = Math.Max(0, (int)cy - R); y < Math.Min(dst.H, (int)cy + R + 1); y++)
            for (int x = Math.Max(0, (int)cx - R); x < Math.Min(dst.W, (int)cx + R + 1); x++)
            {
                double dx = x + 0.5 - cx, dy = y + 0.5 - cy, r = Math.Sqrt(dx * dx + dy * dy);
                double w = intensity * (0.12 + 0.88 * Math.Pow(Curve.Smooth(0, ballR * 0.97, r), 1.6)) * Curve.Smooth(rimR * 1.04, rimR * 0.86, r);
                if (w < 0.003) continue;
                dst.Add(y * dst.W + x, (float)(1.0 * w), (float)(0.995 * w), (float)(0.97 * w), (float)Math.Min(1, w));
            }
    }

    // Four-point star with fuller arms than Fx.Star (reference glints read as chunky sparkles at board scale).
    public static void FatStar(Img dst, double cx, double cy, double size, double deg, double intensity, double heat = 0.85)
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

    // Rainbow flecks from our stud palette scattered around a radius (saturated, small, short-lived).
    public static void Fringe(Img dst, double cx, double cy, double radius, double spread, int count, double size, double intensity, int seed, double spinDeg)
    {
        if (intensity <= 0) return;
        for (int k = 0; k < count; k++)
        {
            double ang = (k + Hash.U(seed, k) * 0.8) / count * 2 * Math.PI + spinDeg * Math.PI / 180;
            double rr = radius + (Hash.U(seed, k + 50) - 0.5) * spread;
            double px = cx + Math.Cos(ang) * rr, py = cy + Math.Sin(ang) * rr * 0.92;
            var col = Palette[(int)(Hash.U(seed, k + 90) * Palette.Length) % Palette.Length];
            // Horizontal streak flecks: stretched along x like the spin blur.
            double sz = size * (0.6 + 0.8 * Hash.U(seed, k + 130));
            for (int j = -2; j <= 2; j++) Tinted(dst, px + j * sz * 0.55, py, sz * 0.55, intensity * (1 - Math.Abs(j) * 0.25), col);
        }
    }

    // Hard VFX boundary: every glow reaches alpha 0 by this radius from the canvas centre.
    public const double LimitR = 246;
    static double Window(double r) { return Curve.Smooth(LimitR, LimitR - 34, r); }

    // Neutral white bloom (optionally flattened) windowed to the canvas limit.
    public static void Bloom(Img dst, double cx, double cy, double radius, double intensity, double squashY)
    {
        if (intensity <= 0 || radius <= 0) return;
        int R = (int)Math.Ceiling(LimitR) + 1;
        for (int y = Math.Max(0, (int)cy - R); y < Math.Min(dst.H, (int)cy + R + 1); y++)
            for (int x = Math.Max(0, (int)cx - R); x < Math.Min(dst.W, (int)cx + R + 1); x++)
            {
                double dx = x + 0.5 - cx, dyr = y + 0.5 - cy, dy = dyr / squashY, q = (dx * dx + dy * dy) / (radius * radius);
                double w = Math.Exp(-q * 2.0) * intensity * Window(Math.Sqrt(dx * dx + dyr * dyr)); if (w < 0.002) continue;
                // White centre with a faint cool edge so the warm stud smear does not read orange.
                double c = Math.Exp(-q * 1.2);
                dst.Add(y * dst.W + x, (float)((0.90 + 0.10 * c) * w), (float)((0.95 + 0.05 * c) * w), (float)(1.0 * w), (float)Math.Min(1, w));
            }
    }

    // Tight white glow: full-strength white out to rFull, smooth fall to zero at rZero (reference hit glow / steady halo).
    public static void TightGlow(Img dst, double cx, double cy, double rFull, double rZero, double intensity, double squashY)
    {
        if (intensity <= 0 || rZero <= 0) return;
        int R = (int)Math.Ceiling(Math.Min(LimitR, rZero)) + 1;
        for (int y = Math.Max(0, (int)cy - R); y < Math.Min(dst.H, (int)cy + R + 1); y++)
            for (int x = Math.Max(0, (int)cx - R); x < Math.Min(dst.W, (int)cx + R + 1); x++)
            {
                double dx = x + 0.5 - cx, dyr = y + 0.5 - cy, dy = dyr / squashY, r = Math.Sqrt(dx * dx + dy * dy);
                double u = Curve.Smooth(rFull, rZero, r);
                double w = (1 - u) * (1 - u * 0.35) * intensity * Window(Math.Sqrt(dx * dx + dyr * dyr));
                if (w < 0.002) continue;
                dst.Add(y * dst.W + x, (float)(0.97 * w), (float)(0.985 * w), (float)(1.0 * w), (float)Math.Min(1, w));
            }
    }

    // Thin X glint: two crossing tapered white lines with a small hot centre (not a filled star).
    public static void XGlint(Img dst, double cx, double cy, double span, double rotDeg, double intensity)
    {
        if (intensity <= 0 || span <= 0) return;
        Spike(dst, cx, cy, 45 + rotDeg, span, 4.2, intensity);
        Spike(dst, cx, cy, -45 + rotDeg, span, 4.2, intensity);
        Spike(dst, cx, cy, 45 + rotDeg, span * 0.6, 9.0, intensity * 0.35);
        Spike(dst, cx, cy, -45 + rotDeg, span * 0.6, 9.0, intensity * 0.35);
        Tinted(dst, cx, cy, span * 0.07, intensity * 0.9, new[] { 1f, 1f, 1f });
    }

    // How far the buttons stand off the sphere for a given motion blur (flat while smeared, full domes at rest).
    public static double Protrude(double blurDeg) { return 1 - Curve.Smooth(2.5, 12, blurDeg); }

    // Soft coloured ring (straight RGB), optional weighting toward the underside.
    public static void ColorRing(Img dst, double cx, double cy, double radius, double thickness, double intensity, float[] rgb, double squashY, double underside)
    {
        if (intensity <= 0 || radius <= 0) return;
        int R = (int)Math.Ceiling(radius + thickness * 3) + 1;
        for (int y = Math.Max(0, (int)cy - R); y < Math.Min(dst.H, (int)cy + R + 1); y++)
            for (int x = Math.Max(0, (int)cx - R); x < Math.Min(dst.W, (int)cx + R + 1); x++)
            {
                double dx = x + 0.5 - cx, dy = (y + 0.5 - cy) / squashY, rr = Math.Sqrt(dx * dx + dy * dy), d = rr - radius;
                double side = rr > 1e-6 ? dy / rr : 0; // +1 at the bottom
                double w = Math.Exp(-(d * d) / (2 * thickness * thickness)) * intensity * (1 - underside + underside * (0.5 + 0.5 * side));
                w *= Window(Math.Sqrt(dx * dx + (y + 0.5 - cy) * (y + 0.5 - cy)));
                if (w < 0.003) continue;
                dst.Add(y * dst.W + x, rgb[0] * (float)w, rgb[1] * (float)w, rgb[2] * (float)w, (float)Math.Min(1, w));
            }
    }

    static readonly float[] Lilac = { 0.847f, 0.784f, 1.0f }, CyanTint = { 0.50f, 0.91f, 1.0f }, PinkTint = { 1.0f, 0.42f, 0.84f };

    /// <summary>
    /// Detonation shape: a disc (rInner = 0) or thick ring from rInner to rOuter with a jagged outer edge, milky interior,
    /// optional sunburst wedges, cool-white bloom outside, a cyan (top-left) / pink (bottom-right) rim band, and white->lilac colour.
    /// </summary>
    public static void DiscRing(Img dst, double cx, double cy, double rOuter, double rInner, double soft, double interiorAlpha,
        double sunburst, double spikeAmp, double bloomW, double tintBand, double lilac, double intensity, int seed)
    {
        if (intensity <= 0 || rOuter <= 0) return;
        const int TEETH = 44;
        var tl = new double[TEETH]; var tw = new double[TEETH];
        for (int k = 0; k < TEETH; k++) { tl[k] = Math.Pow(Hash.U(seed, k), 1.6); tw[k] = 1.6 + 2.2 * Hash.U(seed, k + 60); }
        int R = (int)Math.Ceiling(LimitR) + 1;
        float wr = (float)(1 - (1 - Lilac[0]) * lilac), wg = (float)(1 - (1 - Lilac[1]) * lilac), wb = 1f;
        for (int y = Math.Max(0, (int)cy - R); y < Math.Min(dst.H, (int)cy + R + 1); y++)
            for (int x = Math.Max(0, (int)cx - R); x < Math.Min(dst.W, (int)cx + R + 1); x++)
            {
                double dx = x + 0.5 - cx, dy = y + 0.5 - cy, r = Math.Sqrt(dx * dx + dy * dy);
                if (r > LimitR) continue;
                double ang = Math.Atan2(dy, dx), pos = (ang + Math.PI) / (2 * Math.PI) * TEETH;
                int k0 = (int)Math.Floor(pos) % TEETH; double fk = pos - Math.Floor(pos);
                double tooth = Math.Max(0, 1 - Math.Abs(fk - 0.5) * tw[k0]);
                double edge = rOuter + spikeAmp * tl[k0] * tooth;
                double outer = Curve.Smooth(edge + soft, edge - soft, r);
                double inner = rInner > 0 ? Curve.Smooth(rInner - soft * 1.6, rInner + soft * 1.6, r) : 1;
                double body = outer * inner + outer * (1 - inner) * interiorAlpha;
                if (sunburst > 0)
                {
                    double wedge = 0.5 + 0.5 * Math.Cos(20 * ang + seed * 0.37);
                    body *= 1 - sunburst * 0.20 * (1 - wedge * wedge) * Curve.Smooth(rOuter * 0.22, rOuter * 0.55, r) * Curve.Smooth(rOuter * 1.0, rOuter * 0.82, r);
                }
                double bloom = r > edge && bloomW > 0 ? Math.Exp(-Math.Pow((r - edge) / bloomW, 2) * 2.2) * 0.6 : 0;
                double w = Math.Max(body, bloom) * intensity * Window(r);
                if (w < 0.003) continue;
                float cr = wr, cg = wg, cb = wb;
                if (bloom > body) { cr = 0.84f; cg = 0.92f; cb = 1.0f; }
                if (tintBand > 0)
                {
                    double band = Curve.Smooth(rOuter - soft * 2 - 20, rOuter - soft * 0.5, r) * Curve.Smooth(edge + bloomW + 6, edge + 2, r);
                    double diag = Math.Max(-1, Math.Min(1, (dx + dy) / Math.Max(1, r) / 1.4142));
                    float[] tc = diag < 0 ? CyanTint : PinkTint;
                    float m = (float)(band * tintBand * Math.Abs(diag));
                    cr += (tc[0] - cr) * m; cg += (tc[1] - cg) * m; cb += (tc[2] - cb) * m;
                }
                dst.Add(y * dst.W + x, cr * (float)w, cg * (float)w, cb * (float)w, (float)Math.Min(1, w));
            }
    }

    /// <summary>
    /// Boosting core flare (also exported as ColorBall_CoreFlare): white core, thin vertical flare spike across the centre,
    /// shorter horizontal spike, slowly turning centre glint and 2-4 thin lens rays re-randomised every 2 frames.
    /// phase = integer frame phase; everything repeats when phase advances by 60.
    /// </summary>
    public static void CoreFlare(Img dst, double cx, double cy, double B, int phase, double intensity, double spikeI, double raysI, double glintI)
    {
        CoreFlare(dst, cx, cy, B, phase, 60, intensity, spikeI, raysI, glintI, 1.70, 1.05);
    }

    // period must be divisible by 6 (breathing = period/3, glint turns 90 degrees per period, seeds change every 2 frames).
    public static void CoreFlare(Img dst, double cx, double cy, double B, int phase, int period, double intensity, double spikeI, double raysI, double glintI, double vLen, double hLen)
    {
        int p = ((phase % period) + period) % period, half = p / 2;
        double flick = 0.90 + 0.10 * Hash.U(611, half);
        double breathe = 0.5 + 0.5 * Math.Cos(2 * Math.PI * p * 3.0 / period);
        // Core light.
        if (intensity > 0) WhiteGlow(dst, cx, cy, B * (0.24 + 0.02 * breathe), 0.95 * intensity, 0.82);
        // Vertical flare: crisp core line plus a soft sheath; shorter horizontal partner.
        if (spikeI > 0)
        {
            Spike(dst, cx, cy, 0, B * vLen, 2.3, 0.95 * spikeI * flick);
            Spike(dst, cx, cy, 0, B * vLen * 0.72, 7.0, 0.28 * spikeI);
            Spike(dst, cx, cy, 90, B * hLen, 1.9, 0.62 * spikeI * flick);
        }
        // Centre glint: four-point star turning 90 degrees per period (symmetric, so it loops).
        if (glintI > 0) FatStar(dst, cx, cy, B * (0.15 + 0.015 * breathe) * (0.6 + 0.4 * glintI), 45 + 90.0 * p / period, 0.95 * glintI, 1.0);
        // Lens rays through the centre.
        if (raysI > 0)
        {
            int s = 700 + half, n = 2 + (int)(Hash.U(s, 0) * 3);
            for (int k = 0; k < n; k++)
            {
                double a = 10 + Hash.U(s, 10 + k) * 160; // avoid duplicating the vertical/horizontal spikes
                if (Math.Abs(a - 90) < 12) a += 24;
                Spike(dst, cx, cy, a, B * (0.62 + 0.30 * Hash.U(s, 20 + k)), 1.4, raysI * (0.45 + 0.25 * Hash.U(s, 30 + k)));
            }
        }
    }

    /// <summary>
    /// Detonation orb / ring (reference finish model): a saturated white band [peak - bandHalf, peak + bandHalf] with a soft
    /// inner ramp and outer falloff, milky interior haze with an opening hole, optional zoom-blur rays in the haze,
    /// jagged outer spikes, cyan/pink edge tint, white -> lilac fade. All radii in canvas px; limitR = hard window.
    /// </summary>
    public static void OrbRing(Img dst, double cx, double cy, double limitR, double peak, double bandHalf, double innerRamp, double outerFalloff,
        double haze, double hazeDepth, double hole, double zoomRays, double spikes, double tint, double lilac, double intensity, int seed)
    {
        if (intensity <= 0) return;
        const int TEETH = 40, RAYS = 29;
        var tl = new double[TEETH]; var tw = new double[TEETH];
        for (int k = 0; k < TEETH; k++) { tl[k] = Hash.U(seed, k + 90) < 0.3 ? 0 : 0.45 + 0.55 * Hash.U(seed, k); tw[k] = 2.2 + 2.0 * Hash.U(seed, k + 60); }
        // Irregular zoom rays: uneven angular positions, widths and strengths (not a regular spoke wheel).
        var rayA = new double[RAYS]; var rayW = new double[RAYS]; var rayS = new double[RAYS];
        for (int k = 0; k < RAYS; k++) { rayA[k] = (k + 0.8 * Hash.U(777, k)) / RAYS * 2 * Math.PI - Math.PI; rayW[k] = 0.02 + 0.05 * Hash.U(778, k); rayS[k] = 0.4 + 0.6 * Hash.U(779, k); }
        int R = (int)Math.Ceiling(limitR) + 1;
        float lr = (float)(1 - 0.145 * lilac), lg = (float)(1 - 0.227 * lilac), lb = (float)(1 - 0.067 * lilac);
        for (int y = Math.Max(0, (int)cy - R); y < Math.Min(dst.H, (int)cy + R + 1); y++)
            for (int x = Math.Max(0, (int)cx - R); x < Math.Min(dst.W, (int)cx + R + 1); x++)
            {
                double dx = x + 0.5 - cx, dy = y + 0.5 - cy, r = Math.Sqrt(dx * dx + dy * dy);
                if (r > limitR) continue;
                double ang = Math.Atan2(dy, dx);
                double edgeOut = peak + bandHalf, spikeV = 0;
                if (spikes > 0 && r > edgeOut)
                {
                    // Outward tapered spikes beyond the ring edge (the edge itself stays whole).
                    double pos = (ang + Math.PI) / (2 * Math.PI) * TEETH; int k0 = (int)Math.Floor(pos) % TEETH; double fk = pos - Math.Floor(pos);
                    double reach = spikes * tl[k0], across = Math.Max(0, 1 - Math.Abs(fk - 0.5) * tw[k0] * (1 + 1.5 * (r - edgeOut) / Math.Max(1, reach)));
                    spikeV = reach > 0.5 ? across * Math.Pow(Curve.Smooth(edgeOut + reach, edgeOut, r), 0.7) : 0;
                }
                double rIn = peak - bandHalf, band;
                if (r > edgeOut) band = Math.Exp(-Math.Pow((r - edgeOut) / Math.Max(1, outerFalloff), 2) * 2.3);
                else if (r >= rIn) band = 1;
                else band = Curve.Smooth(rIn - Math.Max(1, innerRamp), rIn, r);
                double hz = 0;
                if (haze > 0 && r < edgeOut)
                {
                    // Soft glow, not a flat plate: brightest just inside the ring, falling toward the centre; the hole edge is feathered.
                    // Milky interior: slightly brighter toward the rim (hazeDepth ramps in once over the orb frames, never back).
                    double radial = 1 - hazeDepth * 0.30 * (1 - Math.Pow(Curve.Smooth(0, Math.Max(1, rIn), r), 1.4));
                    hz = haze * radial * (hole > 0 ? Curve.Smooth(hole - 4, hole + 22, r) : 1);
                    if (zoomRays > 0)
                    {
                        double ray = 0;
                        for (int k = 0; k < RAYS; k++)
                        {
                            double da = Math.Abs(ang - rayA[k]); if (da > Math.PI) da = 2 * Math.PI - da;
                            ray = Math.Max(ray, rayS[k] * Math.Exp(-(da * da) / (2 * rayW[k] * rayW[k])));
                        }
                        hz *= 1 - zoomRays * 0.22 * (1 - ray) * Curve.Smooth(peak * 0.2, peak * 0.55, r) * Curve.Smooth(rIn, rIn - innerRamp, r);
                    }
                }
                double v = Math.Max(Math.Max(band, hz), spikeV) * intensity * Curve.Smooth(limitR, limitR - 34, r);
                if (v < 0.003) continue;
                float cr = lr, cg = lg, cb = lb;
                if (tint > 0)
                {
                    double near = Curve.Smooth(peak - bandHalf * 2, peak, r) * Curve.Smooth(edgeOut + outerFalloff * 1.5, edgeOut, r);
                    double diag = (dx + dy) / Math.Max(1, r) / 1.41421356;
                    float[] tc = diag < 0 ? CyanTint : PinkTint;
                    float m = (float)(near * tint * Math.Abs(diag));
                    cr += (tc[0] - cr) * m; cg += (tc[1] - cg) * m; cb += (tc[2] - cb) * m;
                }
                dst.Add(y * dst.W + x, cr * (float)v, cg * (float)v, cb * (float)v, (float)Math.Min(1, v));
            }
    }

    // Small outward-flying spark: gold-edged tapered streak with a white head.
    public static void SparkStreak(Img dst, double cx, double cy, double angRad, double r0, double r1, double headW, double intensity)
    {
        if (intensity <= 0) return;
        double ux = Math.Cos(angRad), uy = Math.Sin(angRad);
        Fx.Streak(dst, cx + ux * r0, cy + uy * r0, cx + ux * r1, cy + uy * r1, headW, intensity);
        Fx.Glow(dst, cx + ux * r1, cy + uy * r1, headW * 1.3, intensity * 0.8, 0.85);
    }

    public static void Clear(Img im) { Array.Clear(im.R, 0, im.R.Length); Array.Clear(im.G, 0, im.G.Length); Array.Clear(im.B, 0, im.B.Length); Array.Clear(im.A, 0, im.A.Length); }

    public static string Save(Img im, string dir, string prefix, int frame)
    {
        // Clean alpha: drop sub-2/255 glow tails so no invisible texels are stored.
        for (int i = 0; i < im.A.Length; i++) if (im.A[i] < 1.5f / 255f) { im.R[i] = 0; im.G[i] = 0; im.B[i] = 0; im.A[i] = 0; }
        string p = Path.Combine(dir, string.Format("{0}_{1:000}.png", prefix, frame + 1));
        im.Save(p); return p;
    }
}
