// Royal Throne Color Ball: Idle and Boosting sequences (512x512, 60 fps) plus the standalone CoreFlare and FinishRing assets.
// Shares CB helpers and BallRig with the Reveal. Reference numbers are from cb/COLORBALL-REFERENCE.md (D = resting ball diameter).
using System;
using System.IO;

public static class IdleSeq
{
    // Reference: no idle animation at all (frozen sprite for 4.27 s). The brief allows a tiny shimmer, a very slight pulse and a
    // rare sparkle, so ours stays under the reference's own detection guidance: no transform change of any kind, stud brightness
    // pulse <= 3.5 %, one faint specular sweep on the studs (about +10 levels at its narrow peak) and ONE tiny soft glint per loop.
    // Frame 1 is the untouched art (= Reveal last frame = Boosting first frame).
    public const int Frames = 120;

    static float[] studSoft;

    static void Prepare()
    {
        if (studSoft != null) return;
        int N = BallRig.N; var m = BallRig.StudMask; studSoft = new float[N * N];
        for (int y = 2; y < N - 2; y++)
            for (int x = 2; x < N - 2; x++)
            {
                int c = 0; for (int j = -2; j <= 2; j++) for (int i = -2; i <= 2; i++) if (m[(y + j) * N + x + i]) c++;
                studSoft[y * N + x] = c / 25f;
            }
    }

    public static double Pulse(int t) { return 0.5 - 0.5 * Math.Cos(2 * Math.PI * t / Frames); }

    public static void Render(string outDir, int from, int to)
    {
        Directory.CreateDirectory(outDir);
        Prepare();
        int N = BallRig.N;
        var canvas = new Img(512, 512);
        double B = BallRig.R * 2 * CB.Kc;
        for (int t = from; t <= to; t++)
        {
            CB.Clear(canvas);
            var src = BallRig.Src;
            double p = Pulse(t);
            // Sheen: frames 34-62 (0.47 s), a faint soft diagonal band (top-left -> bottom-right) on the glossy studs, peak about +12 levels.
            double sheen = 0, centre = 0;
            if (t > 34 && t < 62)
            {
                double u = (t - 34) / 28.0, e = u * u * (3 - 2 * u);
                sheen = Math.Sin(Math.PI * u); centre = (-1.25 + 2.5 * e) * BallRig.R;
            }
            Img img = src;
            if (p > 0 || sheen > 0)
            {
                img = src.Clone();
                float kS = (float)(0.035 * p), kB = (float)(0.012 * p);
                double bw = 0.11 * BallRig.R;
                for (int i = 0; i < N * N; i++)
                {
                    float a = src.A[i]; if (a <= 0) continue;
                    float s = studSoft[i], gain = 1 + kS * s + kB * (1 - s);
                    float r = src.R[i] * gain, g = src.G[i] * gain, b = src.B[i] * gain;
                    if (sheen > 0)
                    {
                        int x = i % N, y = i / N;
                        double d = ((x + 0.5 - BallRig.Cx) + (y + 0.5 - BallRig.Cy)) / 1.41421356;
                        double w = Math.Exp(-Math.Pow((d - centre) / bw, 2)) * sheen * (0.040 * s + 0.008 * (1 - s)) * a;
                        r += (float)(w * 1.0); g += (float)(w * 0.99); b += (float)(w * 0.94);
                    }
                    img.R[i] = Math.Min(a, r); img.G[i] = Math.Min(a, g); img.B[i] = Math.Min(a, b);
                }
            }
            Fx.DrawLayer(canvas, img, CB.BallPose(1, 1, 1, 0), 1f, 3);
            // One rare glint on the top orange stud's highlight (source-space position, lit from the top-left).
            Glint(canvas, t, 84, 16, 271, 168, B * 0.045, 0);
            CB.Save(canvas, outDir, "ColorBall_Idle", t);
        }
    }

    static void Glint(Img canvas, int t, int born, int life, double sx, double sy, double size, double rot)
    {
        double age = t - born; if (age < 0 || age > life) return;
        double u = age / life;
        // Soft attack from zero over the first 30 % of the life, smooth decay after; soft peak (no flat clipped white core).
        double env = u < 0.3 ? Curve.Smooth(0, 0.3, u) : 1 - Curve.Smooth(0.3, 1.0, u);
        double X = CB.Ox + (sx - BallRig.Cx) * CB.Kc, Y = CB.Oy + (sy - BallRig.Cy) * CB.Kc;
        CB.Tinted(canvas, X, Y, size * 0.9, 0.12 * env, new[] { 1f, 0.99f, 0.95f });
        CB.FatStar(canvas, X, Y, size * (0.55 + 0.45 * env), rot + 20 * u, 0.30 * env, 0.92);
    }
}

public static class BoostSeq
{
    // Structure (60 fps, 0-based index f; files are f+1). Reference = Royal Match light ball activation, measured.
    //   f0        untouched art (= Idle frame 1)
    //   f1-f29    INTRO. f1: tight white glow at full strength + sideways stretch (ref 469). f1-f11 spin-up about the vertical
    //             axis, flattening (ref 473-481). f12-13 switch to the pastel spinning disc (ref 483, 14 f after the hit).
    //             Charge ring #0 contracts from the canvas edge into the halo f6-f30 (ref 477-507). Halo peak f17-f23,
    //             X glint f20-f28 (ref 503-519, compressed). First vertical flare f14-f24.
    //   f30-f119  LOOP, 90 frames = 1.5 s (= the reference charge-event period), seamless: 36 deg/frame spin (9 turns),
    //             height breathing, halo swell after each ring merge, one contracting charge ring per loop,
    //             intermittent vertical flare, core light, glint, lens rays (re-randomised every 2 frames), stars, sparks.
    //             Beams to the targets are separate assets and fire while this loop plays.
    //   f120-f173 FINISH, 54 frames (ref 853-906, no wind-up): hard cut to a white orb (holds 4 f), expands with a milky interior
    //             and zoom rays, hollows from the centre, pure saturated ring, thins and fades to lilac; f173 fully transparent.
    //             The finish can be entered from ANY loop frame (it starts with a hard cut).
    public const int Frames = 174, LoopStart = 30, LoopLen = 90, FinishStart = 120, FinishLen = 54;
    const double Speed = 36;

    static double SpeedAt(int f)
    {
        if (f <= 0) return 0;
        if (f >= 12) return Speed;
        double u = f / 12.0; return Speed * u * u * (1.6 - 0.6 * u);
    }

    static double[] angles;
    static double Angle(int f)
    {
        if (angles == null) { angles = new double[Frames]; for (int i = 1; i < Frames; i++) angles[i] = angles[i - 1] + SpeedAt(i); }
        return angles[Math.Max(0, Math.Min(Frames - 1, f))];
    }

    // Intro keys (width/height in resting diameters), ending on the loop's phase-0 values at f29.
    static readonly double[] K_W = { 0, 1.0, 1, 1.10, 5, 1.12, 10, 1.19, 12, 1.36, 13, 1.43, 17, 1.45, 23, 1.47, 27, 1.44, 29, 1.40 };
    static readonly double[] K_H = { 0, 1.0, 1, 1.0, 5, 0.98, 10, 0.89, 12, 0.85, 13, 0.82, 17, 0.79, 23, 0.74, 27, 0.82, 29, 0.90 };
    static readonly double[] K_BodyLift = { 0, 0.0, 9, 0.06, 11, 0.20, 12, 0.36, 13, 0.50, 29, 0.50 };
    static readonly double[] K_StudLift = { 0, 0.0, 9, 0.02, 12, 0.10, 13, 0.16, 29, 0.14 };
    static readonly double[] K_StudGain = { 0, 0.0, 4, 0.3, 10, 0.7, 13, 1.0, 29, 1.0 };
    static readonly double[] K_WhiteOut = { 0, 0.0, 1, 0.05, 10, 0.06, 13, 0.12, 20, 0.16, 26, 0.10, 29, 0.07 };
    static readonly double[] K_Blur = { 0, 0, 12, 20, 13, 42, 29, 42 };
    static readonly double[] K_BloomR = { 0, 0.62, 1, 0.62, 12, 0.68, 16, 0.82, 19, 0.92, 23, 0.92, 26, 0.86, 29, 0.86 };
    static readonly double[] K_Bloom = { 0, 0.0, 1, 1.0, 12, 1.0, 17, 1.30, 23, 1.30, 26, 1.10, 29, 1.10 };
    static readonly double[] K_Core = { 0, 0.0, 6, 0.10, 13, 0.55, 20, 0.80, 29, 0.70 };
    static readonly double[] K_XGlint = { 19, 0.0, 21, 1.0, 26, 0.9, 29, 0.0 };
    // Hit glow (tight white halo): intensity, full-white radius and zero radius in resting diameters.
    static readonly double[] K_Hit = { 0, 0.0, 1, 1.0, 12, 1.0, 17, 1.0, 23, 1.0, 29, 0.97 };
    static readonly double[] K_HitFull = { 0, 0.58, 1, 0.58, 12, 0.60, 17, 0.70, 23, 0.76, 26, 0.74, 29, 0.74 };
    static readonly double[] K_HitZero = { 0, 0.92, 1, 0.92, 12, 0.94, 17, 1.0, 23, 1.02, 29, 1.0 };

    // Loop phase of an intro/loop frame (intro frames f >= 24 already follow the loop's closing phases 84-89).
    public static int Phase(int f) { return (((f - LoopStart) % LoopLen) + LoopLen) % LoopLen; }

    // Halo swell after a ring merge: rises over phases 84-89, peaks at phase 0, decays by phase 45.
    static double Swell(int f)
    {
        if (f < 24) return 0;
        int p = Phase(f);
        if (p >= 84) return Curve.Smooth(83, 90, p);
        return p < 45 ? Math.Pow(1 - p / 45.0, 2) : 0;
    }

    public static void Render(string outDir, int from, int to)
    {
        Directory.CreateDirectory(outDir);
        var canvas = new Img(512, 512);
        double B = BallRig.R * 2 * CB.Kc, cx = CB.Ox, cy = CB.Oy;
        for (int f = from; f <= to; f++)
        {
            CB.Clear(canvas);
            if (f == 0) { Fx.DrawLayer(canvas, BallRig.Src, CB.BallPose(1, 1, 1, 0), 1f, 3); CB.Save(canvas, outDir, "ColorBall_Boosting", f); continue; }
            if (f < FinishStart) RenderSpin(canvas, f, B, cx, cy);
            else RenderFinish(canvas, f - FinishStart, B, cx, cy);
            CB.Save(canvas, outDir, "ColorBall_Boosting", f);
        }
    }

    static void RenderSpin(Img canvas, int f, double B, double cx, double cy)
    {
        bool loop = f >= LoopStart;
        int p = Phase(f);
        double c1 = 2 * Math.PI * p / LoopLen;
        // Loop terms are exactly zero at phase 0 (sines), so intro -> loop is continuous.
        double breathe = loop ? 0.07 * Math.Sin(c1) + 0.025 * Math.Sin(2 * c1) : 0;
        double W = loop ? 1.40 + 0.02 * Math.Sin(3 * c1) : Curve.Eval(K_W, f);
        double H = loop ? 0.90 * (1 + breathe) : Curve.Eval(K_H, f);
        double swell = Swell(f);
        double bloomR = (loop ? 0.86 : Curve.Eval(K_BloomR, f)) + 0.08 * swell;
        double bloomI = (loop ? 1.10 : Curve.Eval(K_Bloom, f)) + 0.15 * swell;
        double body = loop ? 0.50 : Curve.Eval(K_BodyLift, f), studL = loop ? 0.14 : Curve.Eval(K_StudLift, f);
        double gain = loop ? 1.0 : Curve.Eval(K_StudGain, f);
        double white = (loop ? 0.07 : Curve.Eval(K_WhiteOut, f)) + 0.03 * swell;
        double blur = loop ? 42 : Math.Max(Curve.Eval(K_Blur, f), SpeedAt(f) * 1.1);
        double core = loop ? 0.70 + 0.06 * Math.Sin(3 * c1) : Curve.Eval(K_Core, f);

        // Charge ring behind everything: contracts from the canvas edge into the halo (spawn phase 66, 24 f).
        ChargeRing(canvas, f, B, cx, cy, B * (loop ? 0.70 + 0.04 * swell : Curve.Eval(K_HitFull, f)));
        // Halo behind the disc: a tight full-strength white glow (reference: pure white just outside the ball from the hit frame,
        // white to about 0.9 D while spinning) plus a softer outer bloom.
        double hitI = loop ? 0.92 + 0.08 * swell : Curve.Eval(K_Hit, f);
        double fullR = loop ? 0.70 + 0.04 * swell : Curve.Eval(K_HitFull, f), zeroR = loop ? 1.0 : Curve.Eval(K_HitZero, f);
        CB.Bloom(canvas, cx, cy, B * bloomR, bloomI * 0.55, 0.80);
        CB.TightGlow(canvas, cx, cy, B * fullR, B * zeroR, hitI, loop ? 0.86 : 0.86 + 0.14 * (1 - Curve.Smooth(1, 13, f)));
        // The ball / disc. The first frames turn the ORIGINAL art on the sphere (exact art at angle 0, so no double image at the
        // hand-off from Idle); the rig takes over over f3-f6 while the spin is still slow. Buttons lie flat while blurred.
        var pose = CB.BallPose(1, W, H, 0);
        // One-step switch (the reference switches instantly from the sphere to the blurred disc): the ORIGINAL art turned on the
        // sphere for 002-005 (up to about 11 degrees), then the motion-blurred rig from 006; nothing is ever blended.
        double rigW = f >= 5 ? 1 : 0;
        Img ballImg;
        if (rigW <= 0) ballImg = RevealSeq.Reproject(Angle(f), blur, 24);
        else
        {
            var layers = BallRig.Render(Angle(f), blur, blur > 25 ? 32 : blur > 1 ? 20 : 1, 1, 1, CB.Protrude(blur));
            ballImg = CB.Energize(layers, body, studL, gain, white);
            if (rigW < 1) ballImg = RevealSeq.Lerp(RevealSeq.Reproject(Angle(f), blur, 24), ballImg, (float)rigW);
        }
        Fx.DrawLayer(canvas, ballImg, pose, 1f, 3);
        // Light wash over the disc so it sits inside the halo.
        CB.Bloom(canvas, cx, cy, B * 0.66, bloomI * 0.15 * Curve.Smooth(4, 13, f), 0.62);
        // Vertical flare: intermittent, within the disc height (ref: thin line over the disc, never past it).
        double xgNow = f < LoopStart ? Curve.Eval(K_XGlint, f) : 0;
        double rays = (loop ? 0.42 : 0.42 * Curve.Smooth(12, 16, f)) * (1 - 0.85 * xgNow);
        double glint = loop ? 0.75 : 0.75 * Curve.Smooth(10, 16, f);
        CB.CoreFlare(canvas, cx, cy, B, p, LoopLen, core, FlareEnvelope(f) * (1 - 0.85 * xgNow), rays, glint * (1 - 0.7 * xgNow), 0.98, 0.80);
        // X glint over the centre at the halo peak (intro only).
        double xg = f < LoopStart ? Curve.Eval(K_XGlint, f) : 0;
        if (xg > 0) CB.XGlint(canvas, cx, cy, B * 0.86 * (0.8 + 0.2 * xg), (f - 19) * 1.5, xg);
        Stars(canvas, f, B);
        Sparks(canvas, f, B);
    }

    public static double FlareEnvelope(int f)
    {
        double e = 0.22 * Curve.Smooth(11, 15, f);
        if (f < LoopStart) { if (f >= 14 && f <= 24) e = Math.Max(e, Math.Sin(Math.PI * (f - 14) / 10.0)); return e; }
        int p = Phase(f);
        if (p >= 8 && p <= 22) e = Math.Max(e, Math.Sin(Math.PI * (p - 8) / 14.0));
        if (p >= 50 && p <= 60) e = Math.Max(e, 0.8 * Math.Sin(Math.PI * (p - 50) / 10.0));
        return e;
    }

    static void ChargeRing(Img canvas, int f, double B, double cx, double cy, double haloFullPx)
    {
        const double spawn = 66, life = 24;
        double age;
        if (f < LoopStart) age = f - (LoopStart - LoopLen + spawn); // pre-loop birth at f6
        else age = ((Phase(f) - spawn) % LoopLen + LoopLen) % LoopLen;
        if (age < 0 || age >= life) return;
        double u = age / life;
        double r = B * (0.98 - 0.46 * u);
        double inten = Curve.Smooth(0, 4, age) * (0.25 + 0.60 * Curve.Smooth(0.1, 0.75, u)) * (1 - Curve.Smooth(0.82, 1.0, u));
        // Melt into the halo instead of meeting its edge: fade out and soften while approaching the full-white radius.
        inten *= Curve.Smooth(haloFullPx - 5, haloFullPx + 45, r);
        CB.ColorRing(canvas, cx, cy, r, 7 + 6 * Curve.Smooth(haloFullPx + 60, haloFullPx, r), inten, new[] { 1f, 1f, 1f }, 1.0, 0);
    }

    // Loop-periodic birth schedule: born at loop phase b; inside the loop the age wraps modulo the loop length. Before the loop
    // only births after f4 exist (so anything alive at the loop start was also drawn before it).
    static bool AgeAt(int f, double b, double life, out double age)
    {
        age = -1;
        if (f < LoopStart)
        {
            double birthF = b - LoopLen + LoopStart;
            if (birthF < 4) return false;
            age = f - birthF; return age >= 0 && age < life;
        }
        age = ((f - LoopStart - b) % LoopLen + LoopLen) % LoopLen; return age < life;
    }

    static void Stars(Img canvas, int f, double B)
    {
        for (int k = 0; k < 6; k++)
        {
            double b = k * 15 + 4 * Hash.U(501, k), life = 13 + 5 * Hash.U(502, k), age;
            if (!AgeAt(f, b, life, out age)) continue;
            double u = age / life, a = Hash.U(503, k) * 2 * Math.PI;
            double rr = B * (0.64 + 0.22 * Hash.U(504, k)) + B * 0.05 * u;
            double env = Math.Sin(Math.PI * Math.Pow(u, 0.6));
            CB.FatStar(canvas, CB.Ox + Math.Cos(a) * rr, CB.Oy + Math.Sin(a) * rr * 0.72, B * (0.05 + 0.03 * Hash.U(505, k)) * env, 15 * Hash.U(506, k) + 25 * u, 0.95, k % 3 == 2 ? 0.5 : 0.95);
        }
    }

    static void Sparks(Img canvas, int f, double B)
    {
        for (int k = 0; k < 12; k++)
        {
            double b = k * 7.5 + 3 * Hash.U(601, k), life = 11 + 5 * Hash.U(602, k), age;
            if (!AgeAt(f, b, life, out age)) continue;
            double u = age / life, a = Hash.U(603, k) * 2 * Math.PI;
            double rim = B * 0.5 * Math.Sqrt(Math.Pow(Math.Cos(a) * 1.40, 2) + Math.Pow(Math.Sin(a) * 0.90, 2));
            double e = 1 - Math.Pow(1 - u, 2);
            double r1 = Math.Min(rim + B * (0.08 + 0.24 * e), CB.LimitR - 16), r0 = Math.Max(rim, r1 - B * 0.12 * (1 - u * 0.6));
            CB.SparkStreak(canvas, CB.Ox, CB.Oy, a, r0, r1, 2.4 * (1 - 0.5 * u), 0.8 * (1 - Math.Pow(u, 1.5)));
        }
    }

    // ---------------------------------------------------------------- finish (reference model in D units, see FinishState)
    static void RenderFinish(Img canvas, int g, double B, double cx, double cy)
    {
        var s = FinishState.At(g);
        // Compress radii so the ring stays inside the 512 canvas: orb edge 0.84 D -> about 0.66 B, ring peak 2.07 D -> about 0.88 B.
        double th = 0.71 - 0.29 * Curve.Smooth(6, 24, g);
        double peak = Map(s.Peak) * B, bandHalf = s.BandHalf * th * B, ramp = s.InnerRamp * th * B, fall = s.OuterFalloff * th * B;
        CB.OrbRing(canvas, cx, cy, CB.LimitR, peak, bandHalf, ramp, fall, s.Haze, s.HazeDepth, s.HoleFrac * peak, s.ZoomRays, s.Spikes * th * B,
            s.Tint, s.Lilac, s.Intensity, 300 + g / 4);
    }

    static double Map(double rD)
    {
        return rD <= 0.93 ? rD * (0.66 / 0.93) : 0.66 + (rD - 0.93) * 0.195;
    }
}

// Reference finish (853-906) as keyed values in D units per finish frame g (0 = the hard cut).
public struct FinishState
{
    public double Peak, BandHalf, InnerRamp, OuterFalloff, Haze, HazeDepth, HoleFrac, ZoomRays, Spikes, Tint, Lilac, Intensity, Streaks;

    static readonly double[] K_Peak = { 0, 0.80, 2, 0.76, 4, 0.84, 6, 0.86, 8, 0.93, 10, 1.02, 12, 1.11, 14, 1.19, 16, 1.27, 18, 1.33, 20, 1.40, 22, 1.46, 24, 1.51, 26, 1.57, 28, 1.63, 30, 1.68, 32, 1.72, 34, 1.76, 36, 1.81, 38, 1.85, 40, 1.89, 42, 1.91, 44, 1.94, 46, 1.96, 48, 1.98, 50, 2.01, 52, 2.05, 53, 2.07 };
    static readonly double[] K_BandHalf = { 0, 0.06, 6, 0.06, 8, 0.08, 20, 0.12, 24, 0.165, 40, 0.165, 44, 0.10, 46, 0.07, 48, 0.02, 53, 0.01 };
    static readonly double[] K_InnerRamp = { 0, 0.10, 8, 0.20, 24, 0.28, 40, 0.25, 48, 0.10, 53, 0.08 };
    static readonly double[] K_OuterFalloff = { 0, 0.10, 8, 0.16, 30, 0.18, 50, 0.20, 53, 0.20 };
    static readonly double[] K_Haze = { 0, 1.0, 4, 1.0, 8, 0.88, 16, 0.86, 19, 0.84, 20, 0.80, 24, 0.62, 28, 0.45, 32, 0.25, 36, 0.10, 40, 0.0 };
    static readonly double[] K_HazeDepth = { 4, 0.0, 9, 1.0 };
    static readonly double[] K_Hole = { 19, 0.0, 20, 0.11, 22, 0.25, 24, 0.31, 26, 0.38, 28, 0.47, 30, 0.55, 34, 0.65, 40, 0.75 };
    static readonly double[] K_Zoom = { 10, 0.0, 13, 0.6, 24, 0.6, 32, 0.0 };
    static readonly double[] K_Spikes = { 30, 0.0, 32, 0.45, 40, 0.40, 46, 0.15, 48, 0.0 };
    static readonly double[] K_Tint = { 0, 0.0, 2, 0.35, 4, 0.50, 8, 0.30, 12, 0.0 };
    static readonly double[] K_Lilac = { 44, 0.0, 46, 0.30, 48, 0.65, 50, 0.85, 53, 1.0 };
    static readonly double[] K_Intensity = { 0, 1.0, 46, 1.0, 47, 0.85, 48, 0.55, 50, 0.30, 52, 0.10, 53, 0.0 };
    static readonly double[] K_Streaks = { 30, 0.0, 33, 0.75, 40, 0.65, 43, 0.0 };

    public static FinishState At(int g)
    {
        return new FinishState
        {
            Peak = Curve.Eval(K_Peak, g), BandHalf = Curve.Eval(K_BandHalf, g), InnerRamp = Curve.Eval(K_InnerRamp, g),
            OuterFalloff = Curve.Eval(K_OuterFalloff, g), Haze = Curve.Eval(K_Haze, g), HazeDepth = Curve.Eval(K_HazeDepth, g), HoleFrac = Curve.Eval(K_Hole, g),
            ZoomRays = Curve.Eval(K_Zoom, g), Spikes = Curve.Eval(K_Spikes, g), Tint = Curve.Eval(K_Tint, g),
            Lilac = Curve.Eval(K_Lilac, g), Intensity = Curve.Eval(K_Intensity, g), Streaks = Curve.Eval(K_Streaks, g),
        };
    }
}

public static class CoreFlareAsset
{
    // ColorBall_CoreFlare: the Boosting core flare on its own (additive overlay), 512x512, seamless 90-frame loop in step with the
    // Boosting loop (CoreFlare frame n+1 pairs with Boosting frame 31+n), same centre and scale (B = 235 px).
    public const int Frames = 90;

    public static void Render(string outDir, int from, int to)
    {
        Directory.CreateDirectory(outDir);
        var canvas = new Img(512, 512);
        double B = BallRig.R * 2 * CB.Kc;
        for (int f = from; f <= to; f++)
        {
            CB.Clear(canvas);
            double c1 = 2 * Math.PI * f / Frames;
            CB.CoreFlare(canvas, CB.Ox, CB.Oy, B, f, Frames, 0.70 + 0.06 * Math.Sin(3 * c1), BoostSeq.FlareEnvelope(BoostSeq.LoopStart + f), 0.42, 0.75, 0.98, 0.80);
            CB.Save(canvas, outDir, "ColorBall_CoreFlare", f);
        }
    }
}

public static class FinishRingAsset
{
    // ColorBall_FinishRing_Large: the detonation orb -> ring at TRUE reference scale (ring peak to 2.07 D), for engines that want the
    // full-size shockwave instead of the compressed ring baked into the 512 Boosting frames. 1024x1024, centre (512,512),
    // same pixel scale as the ball sprites (D = 235 px), 54 frames, last frame transparent. Play it in place of Boosting 121-174.
    public const int Frames = 54;

    public static void Render(string outDir, int from, int to)
    {
        Directory.CreateDirectory(outDir);
        var canvas = new Img(1024, 1024);
        double D = BallRig.R * 2 * CB.Kc;
        for (int g = from; g <= to; g++)
        {
            CB.Clear(canvas);
            var s = FinishState.At(g);
            double peak = s.Peak * D;
            CB.OrbRing(canvas, 512, 512, 500, peak, s.BandHalf * D, s.InnerRamp * D, s.OuterFalloff * D, s.Haze, s.HazeDepth, s.HoleFrac * peak,
                s.ZoomRays, s.Spikes * D, s.Tint, s.Lilac, s.Intensity, 300 + g / 4);
            if (s.Streaks > 0)
            {
                CB.Spike(canvas, 512 - peak * 0.97, 512, 90, D * 0.60, 3.0, s.Streaks);
                CB.Spike(canvas, 512 + peak * 0.97, 512, 90, D * 0.60, 3.0, s.Streaks);
            }
            CB.Save(canvas, outDir, "ColorBall_FinishRing_Large", g);
        }
    }
}
