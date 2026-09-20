// Royal Throne Color Ball: Reveal sequence (512x512, 60 fps). Shares CB helpers and BallRig.
using System;
using System.IO;

public static class RevealSeq
{
    // Reference (Royal Match light ball, measured, t0 = video f43; see cb/COLORBALL-REFERENCE.md section 2):
    //   R1 43-44 pastel banded mini-ball 0.65x0.54 D (not white) | R2 45-60 white burst grows (ease-out) to 1.41 D, chromatic fringe
    //   R3 61-66 white plateau 1.47 D, first stars at 61, whiteness fading from 63 | R4 67-70 white drops: glassy pastel ball with a
    //   bright rim hugging it (r 0.71 D) | R5 71-98 oblate blurred disc widening to 1.38x0.93 D at 79, ring detaches at r 0.64 D
    //   and expands to 0.82 D, gone by 80; 8 staggered diagonal stars (61-73) drift outward, gone by about 86 | 99 hard cut to the art.
    // Ours (f = video frame - 43): f0-1 R1, f2-17 R2, f18-23 R3, f24-27 R4, f28-55 R5 (same frames as the reference), then instead of
    // the hard cut the spin decelerates from f44 and lands exactly at rest (angle 0, no squash, no blur) on f61. From f52-f55 (still blurred,
    // about 26 -> 8 degrees from rest) the rig hands over to the original art turned on the fitted sphere, so the last degrees of the
    // spin ARE the original pixels; f61-f67 hold the exact art (= Idle frame 1). 68 frames = 1.133 s = 56 reference frames + 12 settle frames.
    public const int Frames = 68;

    static readonly double[] K_Scale = { 0, 0.55, 3, 0.55, 6, 0.615, 8, 0.70, 10, 0.79, 12, 0.85, 14, 0.91, 16, 0.96, 18, 0.98, 20, 1.0, 67, 1.0 };
    // Width/height multipliers. From f24 (scale 1) these are the disc W/H in resting diameters.
    static readonly double[] K_SqX = { 0, 1.18, 3, 1.16, 8, 1.08, 16, 1.02, 19, 1.04, 21, 1.10, 23, 1.18, 24, 1.26, 26, 1.24, 28, 1.21, 30, 1.26, 32, 1.30, 34, 1.32, 36, 1.33, 40, 1.31, 46, 1.26, 50, 1.20, 53, 1.10, 56, 1.0, 67, 1.0 };
    static readonly double[] K_SqY = { 0, 0.84, 3, 0.85, 8, 0.93, 16, 0.99, 19, 1.0, 21, 1.06, 23, 1.13, 24, 1.20, 26, 1.14, 28, 1.05, 30, 1.0, 32, 0.97, 34, 0.94, 36, 0.93, 40, 0.91, 46, 0.92, 50, 0.93, 53, 0.97, 56, 1.0, 67, 1.0 };
    static readonly double[] K_Blur = { 0, 40, 23, 34, 24, 44, 27, 40, 28, 32, 36, 36, 46, 44, 50, 42, 53, 30, 56, 18, 58, 10, 60, 4, 62, 1, 63, 0.3, 64, 0, 67, 0 };
    static readonly double[] K_WhiteOut = { 0, 0.12, 1, 0.16, 2, 0.44, 4, 0.48, 8, 0.48, 19, 0.48, 21, 0.44, 23, 0.38, 24, 0.30, 26, 0.22, 28, 0.14, 32, 0.06, 40, 0.0, 67, 0 };
    static readonly double[] K_BodyLift = { 0, 0.36, 23, 0.30, 24, 0.30, 28, 0.24, 36, 0.14, 50, 0.06, 54, 0.02, 56, 0.0, 67, 0 };
    static readonly double[] K_StudLift = { 0, 0.24, 23, 0.20, 24, 0.20, 28, 0.14, 40, 0.08, 50, 0.03, 56, 0.0, 67, 0 };
    static readonly double[] K_StudGain = { 0, 1.0, 50, 1.0, 54, 0.4, 56, 0.0, 67, 0 };
    // Swell rim radius in resting diameters (the jagged rim and spikes add about 10 % to the visible half-width).
    static readonly double[] K_Swell = { 0, 0.28, 2, 0.305, 4, 0.35, 6, 0.41, 8, 0.465, 10, 0.527, 12, 0.568, 14, 0.605, 16, 0.64, 18, 0.655, 20, 0.668, 23, 0.668 };
    static readonly double[] K_SwellI = { 0, 0.0, 1, 0.20, 2, 0.75, 4, 1.0, 19, 1.0, 21, 0.86, 22, 0.72, 23, 0.58 };
    static readonly double[] K_Core = { 0, 0.02, 2, 0.10, 6, 0.10, 19, 0.12, 21, 0.10, 23, 0.06 };
    // Soft white halo (haze) around the swell, then behind the disc.
    static readonly double[] K_Halo = { 0, 0.10, 4, 0.25, 10, 0.35, 23, 0.40, 24, 0.85, 28, 0.70, 40, 0.48, 52, 0.28, 58, 0.10, 62, 0.02, 64, 0.0, 67, 0 };
    static readonly double[] K_HaloR = { 0, 0.40, 23, 0.78, 24, 0.80, 44, 0.90, 65, 0.92 };
    // Rim hugging the silhouette (f24-27), detaching at f28 and expanding to 0.82 D, gone at f38.
    static readonly double[] K_Ring = { 23, 0.0, 24, 0.80, 27, 0.72, 28, 0.66, 30, 0.58, 32, 0.46, 34, 0.34, 36, 0.14, 37, 0.04, 38, 0.0 };
    static readonly double[] K_RingR = { 24, 0.69, 26, 0.66, 28, 0.645, 30, 0.685, 32, 0.725, 34, 0.77, 36, 0.82, 38, 0.83 };
    // Reference star angles (degrees, screen space, clockwise from +x) and staggered births f18-f29.
    static readonly double[] StarAng = { 25, 216, 123, 330, 49, 238, 145, 311 };
    static readonly double[] StarBorn = { 18, 19, 21, 22, 24, 25, 27, 29 };

    // Frame of the one-step cut from the spinning rig to the reprojected original art (about 18 degrees from rest, blur 14;
    // turning the flat art on the sphere stays accurate only for small angles).
    const int CutFrame = 57;

    // The original art as a texture on the fitted sphere, turned by angleDeg about the vertical axis (front surface moves right
    // for positive angles, as in BallRig.Render) with sampled motion blur. At angle 0 and no blur this is exactly the art.
    // Only used for the last small angles of the settle, where the hidden back of the sphere never comes into view.
    public static Img Reproject(double angleDeg, double blurDeg, int samples)
    {
        int N = BallRig.N; var src = BallRig.Src; var o = new Img(N, N);
        double R = BallRig.R, cx = BallRig.Cx, cy = BallRig.Cy;
        int n = blurDeg > 0.2 ? samples : 1;
        var cs = new double[n]; var sn = new double[n];
        for (int k = 0; k < n; k++)
        {
            double th = (angleDeg + (n == 1 ? 0 : blurDeg * ((k + 0.5) / n - 0.5))) * Math.PI / 180;
            cs[k] = Math.Cos(th); sn[k] = Math.Sin(th);
        }
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                double px = x + 0.5 - cx, py = y + 0.5 - cy, d2 = px * px + py * py;
                if (d2 > R * R * 1.25) { int j = y * N + x; if (src.A[j] <= 0) continue; }
                double z = Math.Sqrt(Math.Max(0, R * R - d2));
                // Inside the sphere, keep samples within the body on this latitude so the limb never opens a see-through gap.
                double rho = d2 < R * R ? 0.965 * Math.Sqrt(Math.Max(0, R * R - py * py)) : double.MaxValue;
                float sr = 0, sg = 0, sb = 0, sa = 0;
                for (int k = 0; k < n; k++)
                {
                    // Inverse rotation: where on the resting sphere did this visible point come from?
                    double X = px * cs[k] - z * sn[k]; // same sense as BallRig.Render (texture at lon L shows at L + angle)
                    if (X > rho) X = rho; else if (X < -rho) X = -rho;
                    float r, g, b, a; src.Sample(cx + X, cy + py, out r, out g, out b, out a);
                    sr += r; sg += g; sb += b; sa += a;
                }
                int i = y * N + x; float inv = 1f / n;
                o.R[i] = sr * inv; o.G[i] = sg * inv; o.B[i] = sb * inv; o.A[i] = sa * inv;
            }
        return o;
    }

    // Colour blends linearly, but coverage leads (t^0.4) so shapes present in only one layer (protruding buttons) never read as
    // half-transparent ghosts during a hand-over.
    // Hand-over blend: colour is weighted by each layer's coverage (a shape present in one layer keeps its own colour) and
    // coverage bulges to the union mid-blend, so nothing turns see-through; exact a at t = 0 and exact b at t = 1.
    public static Img LerpMax(Img a, Img b, float t)
    {
        var o = new Img(a.W, a.H); float bulge = 4 * t * (1 - t);
        for (int i = 0; i < a.A.Length; i++)
        {
            float aa = a.A[i], ab = b.A[i]; if (aa <= 0 && ab <= 0) continue;
            float wa = aa * (1 - t), wb = ab * t, ws = wa + wb;
            float alpha = aa + (ab - aa) * t; alpha += (Math.Max(aa, ab) - alpha) * bulge;
            if (ws <= 1e-6f) continue;
            float ra = aa > 0 ? a.R[i] / aa : 0, ga = aa > 0 ? a.G[i] / aa : 0, ba = aa > 0 ? a.B[i] / aa : 0;
            float rb = ab > 0 ? b.R[i] / ab : 0, gb = ab > 0 ? b.G[i] / ab : 0, bbb = ab > 0 ? b.B[i] / ab : 0;
            o.R[i] = (ra * wa + rb * wb) / ws * alpha; o.G[i] = (ga * wa + gb * wb) / ws * alpha; o.B[i] = (ba * wa + bbb * wb) / ws * alpha; o.A[i] = alpha;
        }
        return o;
    }

    public static Img LerpOpaque(Img a, Img b, float t)
    {
        var o = Lerp(a, b, t); float ta = (float)Math.Pow(t, 0.4);
        for (int i = 0; i < o.A.Length; i++)
        {
            float aL = o.A[i]; if (aL <= 1e-5f) continue;
            float aT = a.A[i] + (b.A[i] - a.A[i]) * ta; if (aT < aL) aT = aL;
            float k = aT / aL; o.R[i] = Math.Min(aT, o.R[i] * k); o.G[i] = Math.Min(aT, o.G[i] * k); o.B[i] = Math.Min(aT, o.B[i] * k); o.A[i] = aT;
        }
        return o;
    }

    public static Img Lerp(Img a, Img b, float t)
    {
        var o = new Img(a.W, a.H);
        for (int i = 0; i < a.A.Length; i++)
        {
            o.R[i] = a.R[i] + (b.R[i] - a.R[i]) * t; o.G[i] = a.G[i] + (b.G[i] - a.G[i]) * t;
            o.B[i] = a.B[i] + (b.B[i] - a.B[i]) * t; o.A[i] = a.A[i] + (b.A[i] - a.A[i]) * t;
        }
        return o;
    }

    // Spin angle: constant fast spin, eased from frame 50 to exactly 0 (mod 360) at frame 64 so the studs land in place.
    const double Fast = 31.0, EaseFrom = 50, End = 64;
    static double Angle(int f)
    {
        if (f >= End) return 0;
        double remain = Fast * (End - EaseFrom) / 3.0;
        if (f <= EaseFrom) return (f - EaseFrom) * Fast - remain;
        double u = (f - EaseFrom) / (End - EaseFrom);
        return -remain * Math.Pow(1 - u, 3);
    }

    public static void Render(string outDir, int from, int to)
    {
        Directory.CreateDirectory(outDir);
        var canvas = new Img(512, 512);
        double cell = BallRig.R * 2 * CB.Kc; // resting ball diameter D in canvas px
        for (int f = from; f <= to; f++)
        {
            CB.Clear(canvas);
            double scale = Curve.Eval(K_Scale, f), sx = Curve.Eval(K_SqX, f), sy = Curve.Eval(K_SqY, f);
            double blur = Curve.Eval(K_Blur, f);
            bool swell = f <= 23;
            // Soft white halo behind the ball.
            CB.WhiteGlow(canvas, CB.Ox, CB.Oy, cell * Curve.Eval(K_HaloR, f), Curve.Eval(K_Halo, f) * 0.75, 0.94);
            double rim = swell ? Curve.Eval(K_Swell, f) * cell : 0;
            // Spikes re-randomise every 2 frames and creep round, like the reference jagged flicker.
            if (swell) CB.WhiteHotSwell(canvas, CB.Ox, CB.Oy, rim, Curve.Eval(K_SwellI, f), 17 + f / 2, 0, Curve.Eval(K_Core, f), 0);
            // Settle: the rig hands over to the ORIGINAL art rotated on the fitted sphere (exact art at angle 0) while still
            // blurred and moving; a premultiplied lerp (not two stacked translucent layers) keeps the silhouette fully opaque.
            // One-step cut (as the reference cuts from its blurred disc to the art): rig until CutFrame-1, then the ORIGINAL art
            // turned on the sphere with the same angle and motion blur, so no two button layouts are ever blended.
            double hand = f >= CutFrame ? 1 : 0;
            Img ballImg;
            if (hand >= 1) ballImg = Angle(f) == 0 && blur <= 0 ? BallRig.Src : Reproject(Angle(f), blur, 24);
            else
            {
                var layers = BallRig.Render(Angle(f), blur, blur > 25 ? 32 : blur > 0.5 ? 20 : 1, 1, 1, CB.Protrude(blur));
                ballImg = CB.Energize(layers, Curve.Eval(K_BodyLift, f), Curve.Eval(K_StudLift, f), Curve.Eval(K_StudGain, f), Curve.Eval(K_WhiteOut, f));
                if (hand > 0) ballImg = LerpMax(ballImg, Reproject(Angle(f), blur, 24), (float)hand);
            }
            Fx.DrawLayer(canvas, ballImg, CB.BallPose(scale, sx, sy, 0), 1f, 3);
            if (swell)
            {
                CB.WhiteHotSwell(canvas, CB.Ox, CB.Oy, rim, Curve.Eval(K_SwellI, f), 17 + f / 2, 0, Curve.Eval(K_Core, f), 1);
                // A few horizontal colour flecks thrown off the rim.
                CB.Fringe(canvas, CB.Ox, CB.Oy, rim * 1.02, rim * 0.18, 12, cell * 0.022 + rim * 0.03, Curve.Eval(K_SwellI, f) * 0.55, 31 + f / 2, f * 7);
                // Soft white over the ball edge and the swell so the white-hot ball reads as one light, not a ball in a ring.
                CB.SwellFill(canvas, CB.Ox, CB.Oy, 0.5 * cell * scale * 0.5 * (sx + sy), rim, Curve.Eval(K_SwellI, f) * 0.75);
            }
            // Bright rim hugging the ball, then a thin ring detaching and expanding.
            double ring = Curve.Eval(K_Ring, f);
            if (ring > 0) Fx.Ring(canvas, CB.Ox, CB.Oy, cell * Curve.Eval(K_RingR, f), f < 28 ? 7.0 : 7.5 + 0.9 * (f - 28), ring, 1.0);
            // Eight four-point stars on the reference diagonals, staggered f18-f29, drifting outward, 16-20 frames each.
            for (int k = 0; k < 8; k++)
            {
                double life = 16 + 4 * Hash.U(91, k), age = f - StarBorn[k];
                if (age < 0 || age > life) continue;
                double u = age / life, ang = StarAng[k] * Math.PI / 180;
                double rr = cell * (0.64 + 0.10 * Hash.U(93, k)) + age * cell * 0.011;
                // 0.3 at birth -> peak at 40 % of life -> small, then gone.
                double env = u < 0.4 ? 0.3 + 0.7 * Curve.Smooth(0, 0.4, u) : 1 - 0.75 * Curve.Smooth(0.4, 1.0, u);
                double size = cell * (0.095 + 0.055 * Hash.U(94, k)) * env * Curve.Smooth(1.0, 0.94, u);
                double heat = 0.95;
                CB.FatStar(canvas, CB.Ox + Math.Cos(ang) * rr, CB.Oy + Math.Sin(ang) * rr, size, 8 * Hash.U(95, k) - 4, 1.0, heat);
            }
            CB.Save(canvas, outDir, "ColorBall_Reveal", f);
        }
    }
}
