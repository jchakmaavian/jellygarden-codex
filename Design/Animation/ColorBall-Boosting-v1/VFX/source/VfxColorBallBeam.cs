// Royal Throne Color Ball booster: board-space targeting beam "ColorBall_Beam".
// C# 5 (Windows PowerShell Add-Type), depends only on BoosterFx.cs (Img, Curve, Hash, Affine, Fx.DrawLayer).
//
// Everything is drawn along +X from SOURCE (left) to TARGET (right). Three clips:
//   Strip    512x96,  1 frame : stretchable body. Source x=12, target x=500, centre y=48. Pivot (12,48) = (0.0234, 0.5).
//   Head     160x64,  1 frame : travelling white-hot spindle. Front tip of the white core at (134,32) = (0.8375, 0.5).
//   Sequence 640x160, 34 frames: travel f1-f10, arrival f11-f12, 14-frame seamless hold loop f13-f26, fade f27-f34.
//            Source (16,80), target (560,80). Pivot (16,80) = (0.025, 0.5). Unity scales X by distance/544.
// Split variants of the same Sequence (same 34-frame timing, for X scales outside ~0.6x-1.6x):
//   SequenceBody 640x160, 34 frames: the Sequence without the contact flare / glow (line + head only), same pivot.
//   Contact      160x160, 34 frames: only the contact flare / glow, centred (80,80) = (0.5,0.5), placed UNSCALED at the target.
//
// Light model: every layer produces straight colours whose red channel is 1 (white, warm white, pale gold, gold),
// so alpha == max(RGB) of the premultiplied result: the baked light reads as additive glow over light and dark boards.
// Layers are combined with a screen blend (C = C1 + C2 - C1*C2 per premultiplied channel, same for alpha), so stacked
// light saturates toward white instead of clipping or darkening.
using System;
using System.IO;

public static class VfxColorBallBeam
{
    public const string Asset = "ColorBall_Beam";

    // ------------------------------------------------------------------ palette (straight RGB, red channel = 1)
    static readonly double[] CWhite = { 1, 1, 1 };
    static readonly double[] CWarm = { 1, 248 / 255.0, 230 / 255.0 };   // #FFF8E6
    static readonly double[] CPale = { 1, 224 / 255.0, 138 / 255.0 };   // #FFE08A
    static readonly double[] CGold = { 1, 194 / 255.0, 58 / 255.0 };    // #FFC23A
    static readonly double[] CHalo = { 1, 210 / 255.0, 74 / 255.0 };    // #FFD24A

    // ------------------------------------------------------------------ geometry constants
    public const int StripW = 512, StripH = 96;
    public const double StripX0 = 12, StripX1 = 500, StripY = 48;
    public const int HeadW = 160, HeadH = 64;
    public const double HeadTipX = 134, HeadY = 32, HeadLen = 77, HeadHalfT = 9, HeadTail = 18;
    public const int SeqW = 640, SeqH = 160, SeqFrames = 34;
    public const double SeqX0 = 16, SeqX1 = 560, SeqY = 80;
    public const int LoopStart = 13, LoopEnd = 26, LoopLen = 14;
    // Fade f27-f34: progress F = (fi - 26) / FadeFrames (f33 = 0.93, f34 >= 1 -> empty). Each column's own progress is
    // (F - FadeLead * u) / (1 - FadeLead), u = 0 at the source, 1 at the target: the source end is gone by f30,
    // the target end starts at f30 and is out by f34.
    const double FadeFrames = 7.5, FadeLead = 0.45;

    // Body width profile (half widths): core 3 -> 9 px, halo 10 -> 26 px from source to target.
    const double Core0 = 1.5, Core1 = 4.5, Halo0 = 5, Halo1 = 13;

    // ------------------------------------------------------------------ small helpers
    struct Px { public double R, G, B, A; }

    static double Clamp01(double v) { return v < 0 ? 0 : v > 1 ? 1 : v; }
    static double Smooth(double e0, double e1, double x) { return Curve.Smooth(e0, e1, x); }

    // Normal "over" of a straight colour mixed between c1 and c2 by t, with alpha a.
    static void OverMix(ref Px p, double[] c1, double[] c2, double t, double a)
    {
        if (a <= 0) return;
        if (a > 1) a = 1;
        double k = 1 - a, u = 1 - t;
        p.R = (c1[0] * u + c2[0] * t) * a + p.R * k;
        p.G = (c1[1] * u + c2[1] * t) * a + p.G * k;
        p.B = (c1[2] * u + c2[2] * t) * a + p.B * k;
        p.A = a + p.A * k;
    }

    static void ScreenPx(Img dst, int i, double r, double g, double b, double a)
    {
        if (a <= 0) return;
        dst.R[i] = (float)(dst.R[i] + r - dst.R[i] * r);
        dst.G[i] = (float)(dst.G[i] + g - dst.G[i] * g);
        dst.B[i] = (float)(dst.B[i] + b - dst.B[i] * b);
        dst.A[i] = (float)(dst.A[i] + a - dst.A[i] * a);
    }

    // Smooth 1D value noise in [0,1).
    static double Noise(int seed, double x)
    {
        double fl = Math.Floor(x); int k = (int)fl; double t = x - fl; t = t * t * (3 - 2 * t);
        return Hash.U(seed, k) * (1 - t) + Hash.U(seed, k + 1) * t;
    }

    static void Clear(Img im)
    {
        Array.Clear(im.R, 0, im.R.Length); Array.Clear(im.G, 0, im.G.Length);
        Array.Clear(im.B, 0, im.B.Length); Array.Clear(im.A, 0, im.A.Length);
    }

    // Final hygiene: drop sub-2/255 alpha tails (they would save as alpha byte 1), keep premultiplied colour <= alpha.
    // The border is NOT masked here: shapes are sized to stay inside, and the checker verifies it.
    static void Clean(Img im)
    {
        const float cut = 1.5f / 255f;
        for (int y = 0; y < im.H; y++)
            for (int x = 0; x < im.W; x++)
            {
                int i = y * im.W + x;
                float a = im.A[i];
                if (a < cut) { im.R[i] = im.G[i] = im.B[i] = im.A[i] = 0; continue; }
                if (a > 1) a = im.A[i] = 1;
                if (im.R[i] > a) im.R[i] = a; if (im.G[i] > a) im.G[i] = a; if (im.B[i] > a) im.B[i] = a;
                if (im.R[i] < 0) im.R[i] = 0; if (im.G[i] < 0) im.G[i] = 0; if (im.B[i] < 0) im.B[i] = 0;
            }
    }

    // ------------------------------------------------------------------ beam body
    public sealed class BodySpec
    {
        public double X0, Y0, Len, RefLen;
        public double CoreMul = 1, HaloMul = 1, HaloGain = 1, Opacity = 1;
        public double CapEnd = 7;          // along-axis extent (px) at which the end-cap halo reaches its nominal half width
        public bool Live;                  // travelling pulses, width shimmer and crackle
        public double LiveGain = 1;
        public double Phase;               // continuous frame phase (frame - LoopStart); all live terms have period 14
        public int Seed;                   // crackle seed, changes every 2 frames and repeats every 14
        public double FadeK = -1;          // 0..1 fade progress (source end first); < 0 = no fade
    }

    // Per-column body parameters at along-axis distance 'along' (already clamped to [0, Len]).
    // bandA scales the warm-white band hugging the core; bandPale scales how far that band shifts toward pale gold.
    static void BodyColumn(BodySpec s, double along, out double cw, out double hw, out double coreA, out double haloA, out double filA,
        out double bandA, out double bandPale)
    {
        double u = s.RefLen > 0 ? Clamp01(along / s.RefLen) : 0;
        cw = (Core0 + (Core1 - Core0) * u) * s.CoreMul;
        hw = (Halo0 + (Halo1 - Halo0) * u) * s.HaloMul;
        coreA = 1; haloA = HaloPeak * s.HaloGain; filA = 0; bandA = 1; bandPale = 1;
        if (s.Live)
        {
            double g = s.LiveGain, tw = 2 * Math.PI;
            // Core width shimmer (+-15%): a ripple running toward the target (period 7 frames) plus a 2-frame crackle term.
            double sh = 0.6 * Math.Sin(tw * 2 * s.Phase / LoopLen - along * 0.011) + 0.4 * (2 * Noise(s.Seed * 7 + 11, along / 38.0) - 1);
            sh = Math.Max(-1, Math.Min(1, sh));
            cw *= 1 + 0.15 * g * sh;
            // Energy pulses: wavelength 272 px, moving 2 wavelengths per 14 frames (38.9 px/frame) from source to target.
            double lam = 272, q = along / lam - 2 * s.Phase / LoopLen, fq = q - Math.Floor(q), dq = (fq - 0.5) * lam;
            double shape = dq >= 0 ? Math.Exp(-(dq / 12) * (dq / 12)) : Math.Exp(-(dq / 34) * (dq / 34));
            double env = Smooth(0.03, 0.16, u) * (1 - 0.45 * Smooth(0.82, 1.0, u));
            double p = shape * env * g;
            cw += 1.3 * p; hw *= 1 + 0.16 * p; haloA *= 1 + 0.45 * p;
            // Crackle: halo edge and brightness flicker along the beam, re-randomised every 2 frames.
            hw *= 1 + 0.07 * g * (2 * Noise(s.Seed * 13 + 5, along / 22.0) - 1);
            haloA *= 1 + 0.14 * g * (2 * Noise(s.Seed * 17 + 3, along / 14.0) - 1);
            filA = 0.62 * g * Smooth(0.03, 0.12, u) * (1 - 0.5 * Smooth(0.9, 1.0, u));
        }
        if (s.FadeK >= 0)
        {
            // Source end first (kU runs 0.3 of the fade ahead at the source).
            double kU = Clamp01((s.FadeK - FadeLead * u) / (1 - FadeLead));
            // The gold halo goes first; the white core THINS while it stays opaque and only drops alpha in the last ~30%
            // of its fade. Partial-alpha pale gold composites to putty grey over the lavender board and olive over dark,
            // whereas a thin opaque white line reads as light on both.
            hw *= 1 - 0.6 * kU; haloA *= Math.Pow(1 - kU, 2.2);
            cw *= 1 - 0.8 * kU; coreA *= 1 - Smooth(0.78, 1.0, kU);
            bandA *= 1 - Smooth(0.35, 0.9, kU); bandPale *= 1 - kU;
            filA *= Math.Pow(1 - kU, 3);
        }
    }

    // Halo alpha profile across the beam, hn = distance / nominal halo half width.
    const double HaloPeak = 0.90;
    static double HaloProfile(double hn) { return Math.Exp(-1.6 * Math.Pow(hn, 2.2)); }

    // Piecewise-linear hash noise in [0,1): zig-zag, for electric crackle arcs.
    static double ZigZag(int seed, double x)
    {
        double fl = Math.Floor(x); int k = (int)fl; double t = x - fl;
        return Hash.U(seed, k) * (1 - t) + Hash.U(seed, k + 1) * t;
    }

    public static void DrawBody(Img dst, BodySpec s)
    {
        if (s.Opacity <= 0 || s.Len < 0) return;
        double hMax = Math.Max(Halo0, Halo1) * s.HaloMul * 1.6 + 3;
        int ya = Math.Max(0, (int)Math.Floor(s.Y0 - hMax * 1.35 - 2)), yb = Math.Min(dst.H - 1, (int)Math.Ceiling(s.Y0 + hMax * 1.35 + 2));
        int xa = Math.Max(0, (int)Math.Floor(s.X0 - Halo0 * s.HaloMul * 1.8 - 3)), xb = Math.Min(dst.W - 1, (int)Math.Ceiling(s.X0 + s.Len + s.CapEnd * 1.8 + 3));
        for (int x = xa; x <= xb; x++)
        {
            double al = x + 0.5 - s.X0, alc = Math.Max(0, Math.Min(s.Len, al));
            double cw, hw, coreA, haloA, filA, bandA, bandPale;
            BodyColumn(s, alc, out cw, out hw, out coreA, out haloA, out filA, out bandA, out bandPale);
            if (hw < 0.2) continue;
            double bw = 1.1 + 0.4 * cw;
            // Crackle: three short zig-zag arcs hugging the core inside the halo, each on a fixed side per seed,
            // gated into segments. Re-randomised with the 2-frame seed; never leave the halo.
            const int NF = 3;
            var fo = new double[NF]; var fa = new double[NF];
            if (filA > 0)
                for (int k = 0; k < NF; k++)
                {
                    double gate = Smooth(0.52, 0.72, Noise(s.Seed * 41 + k * 97, alc / 48.0 + k * 3.1));
                    if (gate <= 0) continue;
                    double side = Hash.U(s.Seed * 59 + 3, k) < 0.5 ? -1 : 1;
                    double v = ZigZag(s.Seed * 53 + k * 11, alc / 8.0 + k * 0.37);
                    fo[k] = side * (cw + 1.0 + (hw * 0.62 - cw) * (0.25 + 0.75 * v));
                    fa[k] = filA * gate;
                }
            double capE = Math.Min(hw, s.CapEnd), capT = Math.Max(0, hw - capE), capK = 1.8 * hw / (capT + 1.8 * capE);
            for (int y = ya; y <= yb; y++)
            {
                double sr = 0, sg = 0, sb = 0, sa = 0;
                for (int j = 0; j < 3; j++)
                    for (int i = 0; i < 3; i++)
                    {
                        double along = al + (i - 1) / 3.0, across = y + (j + 0.5) / 3.0 - s.Y0;
                        double dc, dh;
                        if (along < 0) { dc = Math.Sqrt(across * across + along * along); dh = dc; }
                        else
                        {
                            // Core round cap centred one core radius before the end, so the white core stops at Len inside the halo.
                            double ce = along - (s.Len - Math.Min(cw, s.Len));
                            dc = ce > 0 ? Math.Sqrt(across * across + ce * ce) : Math.Abs(across);
                            dh = Math.Abs(across);
                        }
                        // Target end: the halo rounds off as an ellipse that starts capT px before the core's round cap,
                        // so the soft end stays inside CapEnd * 1.8 px past the end without looking squared off.
                        if (along > s.Len - capT)
                        {
                            double e = (along - (s.Len - capT)) * capK;
                            dh = Math.Sqrt(across * across + e * e);
                        }
                        double hn = dh / hw;
                        if (hn > 1.8) continue;
                        var p = new Px();
                        OverMix(ref p, CHalo, CGold, Smooth(0.25, 1.1, hn), haloA * HaloProfile(hn));
                        if (filA > 0 && along >= 0 && along <= s.Len)
                        {
                            double best = 0;
                            for (int k = 0; k < NF; k++)
                            {
                                if (fa[k] <= 0) continue;
                                double q = (across - fo[k]) / 0.7;
                                best = Math.Max(best, fa[k] * (Math.Exp(-q * q) * 0.85 + Math.Exp(-q * q * 0.12) * 0.15));
                            }
                            OverMix(ref p, CWarm, CPale, 0.35, best);
                        }
                        double eb = Math.Max(0, dc - cw);
                        OverMix(ref p, CWarm, CPale, Smooth(0.0, 1.6, eb / bw) * bandPale, bandA * coreA * 0.92 * Math.Exp(-(eb / bw) * (eb / bw)));
                        OverMix(ref p, CWhite, CWhite, 0, coreA * Smooth(cw + 0.5, cw - 0.5, dc));
                        sr += p.R; sg += p.G; sb += p.B; sa += p.A;
                    }
                if (sa <= 0) continue;
                double kop = s.Opacity / 9.0;
                ScreenPx(dst, y * dst.W + x, sr * kop, sg * kop, sb * kop, sa * kop);
            }
        }
    }

    // ------------------------------------------------------------------ spindle head
    // White-hot spindle pointing +X with its front tip at (tipX, cy): rounded-pointed front, widest ~27% back,
    // long taper to lineCore half width, alpha fading out over the tail so it melts into the line underneath.
    public static void DrawHead(Img dst, double tipX, double cy, double len, double halfT, double lineCore, double tailExt, double intensity)
    {
        if (intensity <= 0 || len < 2) return;
        int n = (int)Math.Ceiling(len + tailExt) + 1;
        var h = new double[n];
        double sf = 0.30 * len;
        for (int k = 0; k < n; k++)
        {
            if (k <= sf) h[k] = halfT * Math.Pow(Math.Sin(Math.PI / 2 * k / sf), 1.0);
            else if (k <= len) { double t = (k - sf) / (len - sf); h[k] = lineCore + (halfT - lineCore) * Math.Pow(1 - t, 1.5) * (1 + 0.5 * t); }
            else h[k] = lineCore;
        }
        const double haloR = 11, bandR = 3.4;
        int xa = Math.Max(0, (int)Math.Floor(tipX - n - haloR * 1.6 - 2)), xb = Math.Min(dst.W - 1, (int)Math.Ceiling(tipX + haloR * 1.6 + 2));
        int ya = Math.Max(0, (int)Math.Floor(cy - halfT - haloR * 1.6 - 2)), yb = Math.Min(dst.H - 1, (int)Math.Ceiling(cy + halfT + haloR * 1.6 + 2));
        for (int y = ya; y <= yb; y++)
            for (int x = xa; x <= xb; x++)
            {
                double sr = 0, sg = 0, sb = 0, sa = 0;
                for (int j = 0; j < 3; j++)
                    for (int i = 0; i < 3; i++)
                    {
                        double dx = x + (i + 0.5) / 3.0 - tipX, dy = y + (j + 0.5) / 3.0 - cy;
                        double best = 1e9; int sNear = 0;
                        for (int k = 0; k < n; k++)
                        {
                            double ex = dx + k;
                            double dd = Math.Sqrt(ex * ex + dy * dy) - h[k];
                            if (dd < best) { best = dd; sNear = k; }
                        }
                        if (best > haloR * 1.6) continue;
                        double m = intensity * (1 - Smooth(len * 0.45, len + tailExt, sNear));
                        if (m <= 0) continue;
                        double d = Math.Max(0, best);
                        var p = new Px();
                        OverMix(ref p, CHalo, CGold, Smooth(0, haloR * 1.1, d), m * 0.72 * Math.Exp(-2.3 * (d / haloR) * (d / haloR)));
                        OverMix(ref p, CWarm, CPale, Smooth(0, bandR * 1.5, d), m * 0.95 * Math.Exp(-(d / bandR) * (d / bandR)));
                        // White-hot interior, a touch warmer right at the outline.
                        OverMix(ref p, CWhite, CWarm, Smooth(-2.5, 0, best), m * Smooth(0.6, -0.6, best));
                        sr += p.R; sg += p.G; sb += p.B; sa += p.A;
                    }
                if (sa <= 0) continue;
                ScreenPx(dst, y * dst.W + x, sr / 9, sg / 9, sb / 9, sa / 9);
            }
    }

    // ------------------------------------------------------------------ contact flare
    // Soft white blob (radius blobR) with a gold rim glow and a chunky concave four-point star (tip radius rayLen,
    // along-beam arms shortened by rayAlong), rotated by deg. The white disc is full out to coreIn * blobR and
    // gone at coreOut * blobR; glowGain scales the gold rim glow fully and the warm body partly (the fade drops them first).
    public static void DrawFlare(Img dst, double cx, double cy, double blobR, double rayLen, double rayAlong, double intensity, double rayI, double deg,
        double coreIn, double coreOut, double glowGain)
    {
        if (intensity <= 0 || blobR <= 0) return;
        double rad = deg * Math.PI / 180, cs = Math.Cos(rad), sn = Math.Sin(rad);
        double warmGain = 0.4 + 0.6 * glowGain;
        // Dim flares lean gold: partial-alpha warm white composites grey over a dark board, partial-alpha gold reads as glow.
        double bodyHot = Clamp01((intensity - 0.5) / 0.45), rayHot = Clamp01((intensity * rayI - 0.3) / 0.6);
        int R = (int)Math.Ceiling(Math.Max(blobR * 1.45, rayLen * 1.15)) + 2;
        for (int y = Math.Max(0, (int)cy - R); y <= Math.Min(dst.H - 1, (int)cy + R); y++)
            for (int x = Math.Max(0, (int)cx - R); x <= Math.Min(dst.W - 1, (int)cx + R); x++)
            {
                double sr = 0, sg = 0, sb = 0, sa = 0;
                for (int j = 0; j < 3; j++)
                    for (int i = 0; i < 3; i++)
                    {
                        double dx = x + (i + 0.5) / 3.0 - cx, dy = y + (j + 0.5) / 3.0 - cy;
                        double r = Math.Sqrt(dx * dx + dy * dy), rn = r / blobR;
                        var p = new Px();
                        // Gold rim glow: flat under the blob, rolling off outside it; beam-halo gold at the rim, deeper gold
                        // outward (pale gold at partial alpha reads khaki on dark and putty on lavender).
                        OverMix(ref p, CHalo, CGold, Smooth(0.6, 1.3, rn), intensity * glowGain * 0.78 * Smooth(1.38, 0.72, rn));
                        // Warm-white body of the blob, turning beam-halo gold at its edge.
                        OverMix(ref p, CWarm, CHalo, Smooth(0.2 + 0.3 * bodyHot, 1.0, rn), intensity * warmGain * 0.95 * Smooth(1.05, 0.4, rn));
                        if (rayI > 0 && rayLen > 0)
                        {
                            double u = (dx * cs + dy * sn) / (rayLen * rayAlong), v = (-dx * sn + dy * cs) / rayLen;
                            // Chunky concave four-point star (house FatStar look): white-hot centre, gold tips. Drawn over the
                            // warm body so the spikes read right up to the white disc.
                            double dm = Math.Pow(Math.Abs(u), 0.7) + Math.Pow(Math.Abs(v), 0.7);
                            double rr = r / rayLen;
                            OverMix(ref p, CWarm, CGold, Smooth(0.15 + 0.4 * rayHot, 1.15, rr), intensity * rayI * Smooth(1.06, 0.80, dm));
                        }
                        OverMix(ref p, CWhite, CWhite, 0, intensity * Smooth(coreOut, coreIn, rn));
                        sr += p.R; sg += p.G; sb += p.B; sa += p.A;
                    }
                if (sa <= 0) continue;
                ScreenPx(dst, y * dst.W + x, sr / 9, sg / 9, sb / 9, sa / 9);
            }
    }

    // ------------------------------------------------------------------ clips
    public static Img StripImage()
    {
        var im = new Img(StripW, StripH);
        var s = new BodySpec { X0 = StripX0, Y0 = StripY, Len = StripX1 - StripX0, RefLen = StripX1 - StripX0, CapEnd = 5.3 };
        DrawBody(im, s);
        Clean(im);
        return im;
    }

    public static Img HeadImage()
    {
        var im = new Img(HeadW, HeadH);
        DrawHead(im, HeadTipX, HeadY, HeadLen, HeadHalfT, 3.5, HeadTail, 1.0);
        Clean(im);
        return im;
    }

    static double EaseOutQuad(double t) { t = Clamp01(t); return 1 - (1 - t) * (1 - t); }

    // Sequence frame, fi is 1-based (1..34).
    public static void SequenceFrame(Img c, int fi) { SequenceFrame(c, fi, false); }

    // noFade = true renders frames past the loop without the fade (used by LoopSelfTest).
    // The full frame is the screen blend of the body layer (line + head) and the contact layer (flare / contact glow);
    // screen is commutative and associative, so this is the same image as drawing everything into one canvas.
    public static void SequenceFrame(Img c, int fi, bool noFade)
    {
        var contact = new Img(SeqW, SeqH);
        SequenceLayers(c, contact, fi, noFade, SeqX1, SeqY);
        ScreenInto(c, contact);
        Clean(c);
    }

    static void ScreenInto(Img dst, Img src)
    {
        for (int i = 0; i < dst.A.Length; i++)
            if (src.A[i] > 0) ScreenPx(dst, i, src.R[i], src.G[i], src.B[i], src.A[i]);
    }

    // Arrival flare white-core extents (fractions of blobR) and the gentler hold contact glow core.
    const double ArrCoreIn = 0.42, ArrCoreOut = 0.92, HoldCoreIn = 0.34, HoldCoreOut = 0.82, HoldGlow = 0.5;

    // Unclean layers of one Sequence frame: body (line + spindle head) and contact (flare / glow centred at ccx, ccy).
    public static void SequenceLayers(Img body, Img contact, int fi, bool noFade, double ccx, double ccy)
    {
        Clear(body); Clear(contact);
        double D = SeqX1 - SeqX0;
        int ph = ((fi - LoopStart) % LoopLen + LoopLen) % LoopLen;
        int seed = 40 + ph / 2;
        var s = new BodySpec { X0 = SeqX0, Y0 = SeqY, RefLen = D, Live = true, Phase = fi - LoopStart, Seed = seed, CapEnd = 7 };
        if (fi <= 10)
        {
            // Travel: easeOutQuad tip over 11 frames so it touches the target exactly as the flare fires on f11
            // (f/10 would park the head for two frames at the end), thinner line behind a white-hot spindle head.
            // No pre-glow ahead of the nose on f10: the f11 flare is the impact.
            double tip = D * EaseOutQuad(fi / 11.0);
            s.Len = Math.Max(0, tip - 14);
            s.CoreMul = 0.62; s.HaloMul = 0.74; s.HaloGain = 0.95; s.LiveGain = 0.55;
            DrawBody(body, s);
            double u = tip / D, lineCore = (Core0 + (Core1 - Core0) * u) * 0.62;
            DrawHead(body, SeqX0 + tip, SeqY, HeadLen, HeadHalfT, Math.Max(1.2, lineCore), HeadTail, 1.0);
        }
        else if (fi <= 12)
        {
            // Arrival: contact flare (~0.37 cell tip to tip), line widens to final width.
            s.Len = D;
            s.CoreMul = fi == 11 ? 0.88 : 1.04; s.HaloMul = fi == 11 ? 0.92 : 1.05; s.HaloGain = fi == 11 ? 1.1 : 1.05;
            s.LiveGain = fi == 11 ? 0.75 : 0.95;
            DrawBody(body, s);
            if (fi == 11)
            {
                DrawHead(body, SeqX1, SeqY, HeadLen * 0.6, HeadHalfT * 0.9, 3.5, HeadTail * 0.6, 0.5);
                DrawFlare(contact, ccx, ccy, 28, 36, 0.75, 1.0, 1.0, 0, ArrCoreIn, ArrCoreOut, 1.0);
            }
            else DrawFlare(contact, ccx, ccy, 24, 32, 0.75, 0.90, 0.80, 0, (ArrCoreIn + HoldCoreIn) / 2, (ArrCoreOut + HoldCoreOut) / 2, (1.0 + HoldGlow) / 2);
        }
        else
        {
            // Hold loop (f13-f26) and fade (f27-f34): identical live terms, fade applied on top.
            s.Len = D;
            double k = fi > LoopEnd && !noFade ? (fi - LoopEnd) / FadeFrames : -1;
            s.FadeK = k;
            DrawBody(body, s);
            ContactGlow(contact, ccx, ccy, ph, seed, k);
        }
    }

    // Gently lit contact glow at the target during the loop; ph = loop phase 0..13.
    static void ContactGlow(Img c, double cx, double cy, int ph, int seed, double fadeK)
    {
        // wc = 0 half-way between loop phase 13 (f26) and phase 0 (f13). Periodic terms (period 14 or a divisor) use mixed phase
        // offsets: none is at full rate at the loop cut, and the motion is not time-symmetric (no ping-pong / duplicate frames).
        double wc = 2 * Math.PI * (ph + 0.5) / LoopLen;
        double blobR = 21 - 1.3 * Math.Cos(2 * wc - 0.5);
        // Size, ray length and sway are periodic; only the brightness flicker is hashed (2-frame seed, repeats every 14).
        double I = 0.80 + 0.07 * Math.Cos(2 * wc + 0.6) + 0.035 * (2 * Hash.U(seed, 3) - 1);
        double rayLen = 31 + 3 * Math.Cos(wc + 1.2) - 1.2 * Math.Cos(3 * wc + 0.4);
        double rayI = 0.62 + 0.07 * (2 * Hash.U(seed, 5) - 1);
        double deg = 3 * Math.Sin(wc + 0.3);
        double glowGain = HoldGlow;
        if (fadeK >= 0)
        {
            // Same schedule as the body's target end. Shrinks first while staying bright; the gold glow drops early and the
            // intensity only in the last ~15%, so the contact point goes out as a small white spark, not a grey disc.
            double kU = Clamp01((fadeK - FadeLead) / (1 - FadeLead));
            blobR *= 1 - 0.7 * kU; rayLen *= 1 - 0.55 * kU; rayI *= 1 - 0.3 * kU;
            glowGain *= Math.Pow(1 - kU, 2.0); I *= 1 - Smooth(0.86, 1.0, kU);
        }
        DrawFlare(c, cx, cy, blobR, rayLen, 0.75, I, rayI, deg, HoldCoreIn, HoldCoreOut, glowGain);
    }

    // ------------------------------------------------------------------ entry points
    static string FramePath(string dir, int fi) { return Path.Combine(dir, string.Format("{0}_{1:000}.png", Asset, fi)); }

    public const int ContactW = 160, ContactH = 160;
    const int ContactX0 = 480, ContactY0 = 0;   // crop of the Sequence canvas: target (560,80) -> contact centre (80,80)

    static Img Crop(Img src, int x0, int y0, int w, int h)
    {
        var o = new Img(w, h);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int i = (y0 + y) * src.W + x0 + x, j = y * w + x;
                o.R[j] = src.R[i]; o.G[j] = src.G[i]; o.B[j] = src.B[i]; o.A[j] = src.A[i];
            }
        return o;
    }

    // outRoot = out/vfx/ColorBall_Beam. from/to are 0-based Sequence frame indices (0..33); Strip and Head render when from == 0.
    // Per frame writes Sequence (full), SequenceBody (line + head only) and Contact (160x160 flare / glow only, unscaled use).
    public static void Render(string outRoot, int from, int to)
    {
        if (from <= 0)
        {
            string sd = Path.Combine(outRoot, "Strip", "frames"); Directory.CreateDirectory(sd);
            StripImage().Save(FramePath(sd, 1));
            string hd = Path.Combine(outRoot, "Head", "frames"); Directory.CreateDirectory(hd);
            HeadImage().Save(FramePath(hd, 1));
        }
        string qd = Path.Combine(outRoot, "Sequence", "frames"); Directory.CreateDirectory(qd);
        string bd = Path.Combine(outRoot, "SequenceBody", "frames"); Directory.CreateDirectory(bd);
        string cd = Path.Combine(outRoot, "Contact", "frames"); Directory.CreateDirectory(cd);
        var body = new Img(SeqW, SeqH); var contact = new Img(SeqW, SeqH);
        for (int f = Math.Max(0, from); f <= Math.Min(SeqFrames - 1, to); f++)
        {
            SequenceLayers(body, contact, f + 1, false, SeqX1, SeqY);
            var full = body.Clone(); ScreenInto(full, contact); Clean(full);
            full.Save(FramePath(qd, f + 1));
            Clean(body); body.Save(FramePath(bd, f + 1));
            var ct = Crop(contact, ContactX0, ContactY0, ContactW, ContactH); Clean(ct); ct.Save(FramePath(cd, f + 1));
        }
    }

    // Periodicity proof: the frame one full loop after f13 (f27 rendered with the fade disabled) must equal f13.
    public static string LoopSelfTest()
    {
        var a = new Img(SeqW, SeqH); var b = new Img(SeqW, SeqH);
        SequenceFrame(a, LoopStart, false); SequenceFrame(b, LoopEnd + 1, true);
        double mx = 0;
        for (int i = 0; i < a.A.Length; i++)
        {
            mx = Math.Max(mx, Math.Abs(a.A[i] - b.A[i])); mx = Math.Max(mx, Math.Abs(a.R[i] - b.R[i]));
            mx = Math.Max(mx, Math.Abs(a.G[i] - b.G[i])); mx = Math.Max(mx, Math.Abs(a.B[i] - b.B[i]));
        }
        return string.Format("loop self-test: max abs difference f13 vs f27-without-fade = {0:0.000} /255", mx * 255);
    }

    // ------------------------------------------------------------------ in-context stacking preview (review only)
    static Img Background(int w, int h, int rgb)
    {
        var im = new Img(w, h);
        float r = ((rgb >> 16) & 255) / 255f, g = ((rgb >> 8) & 255) / 255f, b = (rgb & 255) / 255f;
        for (int i = 0; i < w * h; i++) { im.R[i] = r; im.G[i] = g; im.B[i] = b; im.A[i] = 1; }
        return im;
    }

    // Several beams from one ball position, posed the way Unity would (normal alpha blending), to judge overlap and stretch.
    public static void RenderPreview(string outRoot, int seqFrame1, int headFrame, string tag)
    {
        var strip = Img.Load(FramePath(Path.Combine(outRoot, "Strip", "frames"), 1));
        var head = Img.Load(FramePath(Path.Combine(outRoot, "Head", "frames"), 1));
        var seq = Img.Load(FramePath(Path.Combine(outRoot, "Sequence", "frames"), seqFrame1));
        foreach (var bg in new[] { 0x8a93c8, 0x20242e })
        {
            var im = Background(1280, 800, bg);
            double sx = 320, sy = 400;
            // Strip beams at several distances (scale 0.45x .. 1.75x) and angles, two nearly collinear.
            double[][] targets = { new[] { 1180.0, 180.0 }, new[] { 1160.0, 230.0 }, new[] { 540.0, 700.0 }, new[] { 90.0, 120.0 }, new[] { 420.0, 330.0 } };
            foreach (var t in targets)
            {
                double dx = t[0] - sx, dy = t[1] - sy, dist = Math.Sqrt(dx * dx + dy * dy), deg = Math.Atan2(dy, dx) * 180 / Math.PI;
                Fx.DrawLayer(im, strip, Affine.Pose(StripX0, StripY, dist / (StripX1 - StripX0), 1, deg, sx, sy), 1, 2);
            }
            // A travelling beam: strip up to the head, head at the tip.
            {
                double tx = 1100, ty = 720, dx = tx - sx, dy = ty - sy, dist = Math.Sqrt(dx * dx + dy * dy), deg = Math.Atan2(dy, dx) * 180 / Math.PI;
                double tip = dist * 0.62, ux = dx / dist, uy = dy / dist;
                Fx.DrawLayer(im, strip, Affine.Pose(StripX0, StripY, tip / (StripX1 - StripX0), 0.7, deg, sx, sy), 1, 2);
                Fx.DrawLayer(im, head, Affine.Pose(HeadTipX, HeadY, 1, 1, deg, sx + ux * tip, sy + uy * tip), 1, 2);
            }
            // A Sequence frame posed to a target (X scaled by distance / 544).
            {
                double tx = 560, ty = 60, dx = tx - sx, dy = ty - sy, dist = Math.Sqrt(dx * dx + dy * dy), deg = Math.Atan2(dy, dx) * 180 / Math.PI;
                Fx.DrawLayer(im, seq, Affine.Pose(SeqX0, SeqY, dist / (SeqX1 - SeqX0), 1, deg, sx, sy), 1, 2);
            }
            {
                double tx = 1250, ty = 420, dx = tx - sx, dy = ty - sy, dist = Math.Sqrt(dx * dx + dy * dy), deg = Math.Atan2(dy, dx) * 180 / Math.PI;
                Fx.DrawLayer(im, seq, Affine.Pose(SeqX0, SeqY, dist / (SeqX1 - SeqX0), 1, deg, sx, sy), 1, 2);
            }
            im.Save(Path.Combine(outRoot, string.Format("preview_stack_{0}_{1}.png", bg == 0x8a93c8 ? "lavender" : "dark", tag)));
        }
    }

    // ------------------------------------------------------------------ scale-fix previews (review only)
    // Horizontal 9-slice of src: the left and right borders keep their pixels, the centre stretches to fill outW.
    public static Img Slice9X(Img src, int left, int right, int outW)
    {
        var o = new Img(outW, src.H);
        for (int x = 0; x < outW; x++)
        {
            double ox = x + 0.5, sx;
            if (ox < left) sx = ox;
            else if (ox > outW - right) sx = ox - (outW - src.W);
            else sx = left + (ox - left) * (src.W - left - right) / (double)(outW - left - right);
            for (int y = 0; y < src.H; y++)
            {
                float r, g, b, a; src.Sample(sx, y + 0.5, out r, out g, out b, out a);
                int k = y * outW + x; o.R[k] = r; o.G[k] = g; o.B[k] = b; o.A[k] = a;
            }
        }
        return o;
    }

    // preview_split_<board>.png: for f11, f20, f31 four rows each -- Sequence X-scaled 0.35x | SequenceBody 0.35x + unscaled Contact
    // | SequenceBody 1.8x + unscaled Contact | Sequence 1.8x.  preview_strip_sliced_<board>.png: Strip at 0.4x / 1x / 3x as
    // 9-slice (borders 20/20, pivot pixel 12 at the source) and, for comparison, plainly X-scaled at 0.4x and 3x.
    public static void RenderScalePreviews(string outRoot)
    {
        int[] frames = { 11, 20, 31 };
        foreach (var bg in new[] { 0x8a93c8, 0x20242e })
        {
            string tag = bg == 0x8a93c8 ? "lavender" : "dark";
            const int rowH = 130; double sx0 = 40;
            var im = Background(1120, rowH * 4 * frames.Length, bg);
            for (int fi = 0; fi < frames.Length; fi++)
            {
                var seq = Img.Load(FramePath(Path.Combine(outRoot, "Sequence", "frames"), frames[fi]));
                var body = Img.Load(FramePath(Path.Combine(outRoot, "SequenceBody", "frames"), frames[fi]));
                var ct = Img.Load(FramePath(Path.Combine(outRoot, "Contact", "frames"), frames[fi]));
                double[] dists = { 192, 192, 980, 980 };
                for (int r = 0; r < 4; r++)
                {
                    double cy = rowH * (4 * fi + r) + rowH / 2.0, d = dists[r];
                    if (r == 0 || r == 3) Fx.DrawLayer(im, seq, Affine.Pose(SeqX0, SeqY, d / (SeqX1 - SeqX0), 1, 0, sx0, cy), 1, 2);
                    else
                    {
                        Fx.DrawLayer(im, body, Affine.Pose(SeqX0, SeqY, d / (SeqX1 - SeqX0), 1, 0, sx0, cy), 1, 2);
                        Fx.DrawLayer(im, ct, Affine.Pose(ContactW / 2.0, ContactH / 2.0, 1, 1, 0, sx0 + d, cy), 1, 2);
                    }
                }
            }
            im.Save(Path.Combine(outRoot, string.Format("preview_split_{0}.png", tag)));

            var strip = Img.Load(FramePath(Path.Combine(outRoot, "Strip", "frames"), 1));
            var sim = Background(1560, 100 * 5, bg);
            double[] lens = { 195, 488, 1464 };
            for (int k = 0; k < 3; k++)
            {
                var sl = Slice9X(strip, 20, 20, (int)lens[k] + 24);
                Fx.DrawLayer(sim, sl, Affine.Pose(StripX0, StripY, 1, 1, 0, 40, 50 + 100 * k), 1, 2);
            }
            Fx.DrawLayer(sim, strip, Affine.Pose(StripX0, StripY, 195 / (StripX1 - StripX0), 1, 0, 40, 350), 1, 2);
            Fx.DrawLayer(sim, strip, Affine.Pose(StripX0, StripY, 1464 / (StripX1 - StripX0), 1, 0, 40, 450), 1, 2);
            sim.Save(Path.Combine(outRoot, string.Format("preview_strip_sliced_{0}.png", tag)));
        }
    }
}
