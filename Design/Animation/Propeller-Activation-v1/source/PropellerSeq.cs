// The three Royal Throne propeller sequences. Every frame is the original artwork posed by the
// rig; only light, sparks and stars are procedural. 512 x 512, 60 fps, transparent.
using System;
using System.IO;

public static class PropSeq
{
    public const int Size = 512;
    public const int RevealFrames = 40;      // 0.667 s
    public const int IdleFrames = 120;       // 2.00 s seamless
    public const int ActFrames = 120;        // intro 1-24, loop 25-84, finish 85-120
    public const int IntroLast = 24, LoopFirst = 25, LoopLast = 84, FinishFirst = 85;

    static double HubX { get { return PropRig.HubX; } }
    static double HubY { get { return PropRig.HubY; } }

    static void Save(Img img, string dir, string name, int frame)
    {
        Directory.CreateDirectory(dir);
        img.Save(Path.Combine(dir, name + "_" + frame.ToString("000") + ".png"));
    }

    /// <summary>
    /// True when the booster is exactly seated and nothing else is drawn. Those frames copy the
    /// one cached rest pose, which is what makes the three sequences hand off byte-identically.
    /// </summary>
    static bool Rest(Img img, double scale, double squash, double tilt, double rise, double sweep, double spin, double fx)
    {
        if (Math.Abs(scale - 1) > 1e-9 || Math.Abs(squash) > 1e-9 || Math.Abs(tilt) > 1e-9) return false;
        if (Math.Abs(rise) > 1e-9 || sweep > 1e-9 || fx > 1e-9) return false;
        if (Math.Abs(((spin % 360) + 360) % 360) > 1e-6) return false;
        var r = PropRig.Rest;
        Array.Copy(r.R, img.R, r.R.Length); Array.Copy(r.G, img.G, r.G.Length);
        Array.Copy(r.B, img.B, r.B.Length); Array.Copy(r.A, img.A, r.A.Length);
        return true;
    }

    // ---------------------------------------------------------------- Reveal

    /// <summary>
    /// Spin angle at reveal frame f. The blades come in fast and decelerate to exactly 0 at the
    /// settle, so the last frames are the untouched artwork.
    /// </summary>
    static double RevealSpin(double f, out double sweep)
    {
        // Degrees per frame, easing from a fast entry to a dead stop.
        double[] rate = { 0, 29, 5, 29, 12, 21, 19, 11, 26, 4, 33, 0, 40, 0 };
        sweep = Curve.Eval(rate, f);
        double a = 0;
        for (double s = 0; s < f; s += 1) a += Curve.Eval(rate, s);
        // Land on a whole turn so the final pose is the original art.
        double total = 0; for (double s = 0; s < RevealFrames; s += 1) total += Curve.Eval(rate, s);
        double turns = Math.Round(total / 360.0);
        return a * (turns * 360.0 / Math.Max(1e-6, total));
    }

    public static void Reveal(string dir, int from, int to)
    {
        for (int f = from; f <= to; f++)
        {
            double t = (f - 1) / (double)(RevealFrames - 1);
            var img = new Img(Size, Size);
            double sweep; double spin = RevealSpin(f - 1, out sweep);

            // Elastic pop: compact entry, one overshoot, a small counter-dip, then rest.
            double scale = Curve.Eval(new double[] { 0, .34, .06, .78, .13, 1.18, .22, .94, .31, 1.05, .42, .99, .55, 1.0, 1, 1.0 }, t);
            double squash = Curve.Eval(new double[] { 0, .20, .07, -.10, .15, .09, .24, -.05, .36, .02, .55, 0, 1, 0 }, t);
            double tilt = Curve.Eval(new double[] { 0, -7, .10, 5.5, .22, -3.2, .34, 1.6, .48, -.6, .70, 0, 1, 0 }, t);
            double rise = Curve.Eval(new double[] { 0, 26, .12, -7, .26, 3, .44, -1, .70, 0, 1, 0 }, t);

            // Kept modest so the compact booster reads from the very first frame.
            double flash = Curve.Eval(new double[] { 0, .52, .07, .62, .16, .30, .30, .10, .46, 0, 1, 0 }, t);
            if (Rest(img, scale, squash, tilt, rise, sweep, spin, flash)) { Save(img, dir, "Propeller_Reveal", f); continue; }

            var place = PropRig.Place(scale, squash, tilt, 0, rise);
            int blur = sweep > 3 ? (int)Math.Min(16, 3 + sweep * .42) : 1;
            PropRig.Compose(img, spin, Math.Min(sweep, 150), place, blur, 2);

            // Centre flash on arrival, fading as the blades take over.
            double hx, hy; place.Apply(HubX, HubY, out hx, out hy);
            if (flash > .01)
            {
                Fx.Glow(img, hx, hy, 52 * (1 + .45 * flash), flash * .62, .97);
                Fx.Star(img, hx, hy, 86 * (.45 + .7 * flash), 0, flash * .62, .98);
                Fx.Star(img, hx, hy, 62 * (.45 + .65 * flash), 45, flash * .38, .95);
            }
            // Four-point stars flicking outward from the hub as it lands.
            for (int s = 0; s < 7; s++)
            {
                double born = .02 + s * .037;
                double life = (t - born) / .26;
                if (life <= 0 || life >= 1) continue;
                double ang = Hash.U(s, 11) * 360;
                double dist = (70 + Hash.U(s, 3) * 120) * (.35 + .65 * life) * scale * PropRig.ContentScale;
                double sx = hx + Math.Cos(ang * Math.PI / 180) * dist;
                double sy = hy - Math.Sin(ang * Math.PI / 180) * dist * .8;
                double fade = Math.Sin(life * Math.PI);
                Fx.Star(img, sx, sy, 26 + Hash.U(s, 7) * 20, ang * .5, fade * .8, .9);
            }
            // A few gold sparks riding the blade tips while the rotor is still fast.
            if (sweep > 8)
                for (int s = 0; s < 5; s++)
                {
                    double ang = spin + Hash.U(s, 5) * 360;
                    double rr = PropRig.Reach * (.72 + Hash.U(s, 2) * .3) * scale * PropRig.ContentScale;
                    double sx = hx + Math.Cos(ang * Math.PI / 180) * rr;
                    double sy = hy - Math.Sin(ang * Math.PI / 180) * rr * PropRig.K;
                    Fx.Spark(img, sx, sy, 9 + Hash.U(s, 9) * 7, .30 * Math.Min(1, sweep / 26.0), .88);
                }
            PropRig.Clean(img);
            Save(img, dir, "Propeller_Reveal", f);
        }
    }

    // ---------------------------------------------------------------- Idle

    public static void Idle(string dir, int from, int to)
    {
        for (int f = from; f <= to; f++)
        {
            double t = (f - 1) / (double)IdleFrames;          // 0 at frame 1, wraps at 120
            var img = new Img(Size, Size);

            // Almost still: a tiny blade twitch and an equally tiny body bob, both seamless and
            // exactly zero at the loop boundary so frame 1 is the untouched artwork.
            double spin = 1.7 * Math.Sin(2 * Math.PI * t) + .55 * Math.Sin(4 * Math.PI * t);
            double bob = .8 * Math.Sin(2 * Math.PI * t);
            double scale = 1 + .004 * Math.Sin(2 * Math.PI * t);
            double pulse0 = .5 - .5 * Math.Cos(2 * Math.PI * t);
            if (Rest(img, scale, 0, 0, bob, 0, spin, pulse0)) { Save(img, dir, "Propeller_Idle", f); continue; }
            var place = PropRig.Place(scale, 0, 0, 0, bob);
            PropRig.Compose(img, spin, 0, place, 1, 2);

            double hx, hy; place.Apply(HubX, HubY, out hx, out hy);
            // Highlight shimmer gliding over the collar, zero at both ends of the loop.
            double sheen = Math.Max(0, Math.Sin(2 * Math.PI * t)) * Math.Max(0, Math.Sin(2 * Math.PI * t));
            if (sheen > .01) Fx.Glow(img, hx - 26 + 52 * t, hy - 12, 40, sheen * .16, .95);
            // Very small glow variation around the dome.
            double pulse = .5 - .5 * Math.Cos(2 * Math.PI * t);
            Fx.Glow(img, hx, hy, 50, pulse * .085, .93);
            // Two rare sparkles, both born and gone well inside the loop.
            double[] births = { .22, .68 };
            for (int s = 0; s < births.Length; s++)
            {
                double life = (t - births[s]) / .14;
                if (life <= 0 || life >= 1) continue;
                double ang = 34 + s * 150;
                double rr = 60 + s * 22;
                double sx = hx + Math.Cos(ang * Math.PI / 180) * rr;
                double sy = hy - Math.Sin(ang * Math.PI / 180) * rr * .7;
                Fx.Star(img, sx, sy, 15 + 6 * Math.Sin(life * Math.PI), ang, Math.Sin(life * Math.PI) * .45, .95);
            }
            PropRig.Clean(img);
            Save(img, dir, "Propeller_Idle", f);
        }
    }

    // ---------------------------------------------------------------- Activation

    /// <summary>Degrees the rotor sweeps on activation frame f (1-based).</summary>
    static double ActSweep(double f)
    {
        if (f <= IntroLast) return Curve.Eval(new double[] { 1, 0, 3, 1.5, 6, 9, 10, 26, 15, 46, 20, 62, 24, 70 }, f);
        return 70;                                            // held through loop and finish
    }

    static double ActSpin(double f)
    {
        double a = 0;
        for (double s = 1; s < f; s += 1) a += ActSweep(s);
        return a;
    }

    public static void Activation(string dir, int from, int to)
    {
        // The loop must close on itself: choose a whole number of turns across its 60 frames.
        double loopSpan = ActSweep(LoopFirst) * (LoopLast - LoopFirst + 1);
        double loopTurns = Math.Round(loopSpan / 360.0);
        double loopStep = loopTurns * 360.0 / (LoopLast - LoopFirst + 1);

        for (int f = from; f <= to; f++)
        {
            var img = new Img(Size, Size);
            double sweep, spin, scale, squash, tilt, rise, opacity = 1;

            if (f <= IntroLast)
            {
                double u = (f - 1) / (double)(IntroLast - 1);
                sweep = ActSweep(f);
                spin = ActSpin(f);
                // Anticipation: a short compress, then the lift out of the cell.
                scale = Curve.Eval(new double[] { 0, 1, .12, .965, .28, 1.02, .55, 1.04, 1, 1.06 }, u);
                squash = Curve.Eval(new double[] { 0, 0, .12, .085, .30, -.055, .60, -.02, 1, -.01 }, u);
                tilt = Curve.Eval(new double[] { 0, 0, .35, -1.6, .70, -3.4, 1, -4.2 }, u);
                rise = Curve.Eval(new double[] { 0, 0, .14, 5, .40, -14, .72, -38, 1, -54 }, u);
            }
            else if (f <= LoopLast)
            {
                int k = f - LoopFirst;
                double u = k / (double)(LoopLast - LoopFirst + 1);
                sweep = 70;
                spin = ActSpin(IntroLast + 1) + loopStep * k;
                // Flight pose: held high, with a gentle seamless bob and lean.
                scale = 1.06 + .012 * Math.Sin(2 * Math.PI * u);
                squash = -.012 + .014 * Math.Sin(2 * Math.PI * u + .7);
                tilt = -4.2 + 1.5 * Math.Sin(2 * Math.PI * u);
                rise = -54 + 5 * Math.Sin(2 * Math.PI * u);
            }
            else
            {
                int k = f - FinishFirst;
                double u = k / (double)(ActFrames - FinishFirst);
                sweep = 70 * (1 - .25 * u);
                spin = ActSpin(IntroLast + 1) + loopStep * (LoopLast - LoopFirst + 1) + 70 * k;
                // Clean exit: a last compress, then shrink away to nothing.
                scale = Curve.Eval(new double[] { 0, 1.06, .18, 1.14, .42, .82, .72, .34, 1, .02 }, u);
                squash = Curve.Eval(new double[] { 0, -.012, .18, .07, .5, -.05, 1, 0 }, u);
                tilt = -4.2 + 6 * u;
                rise = -54 - 10 * u;
                opacity = (float)Curve.Eval(new double[] { 0, 1, .45, 1, .75, .45, 1, 0 }, u);
            }

            if (f == 1 && Rest(img, scale, squash, tilt, rise, sweep, spin, 0)) { Save(img, dir, "Propeller_Activation", f); continue; }

            var place = PropRig.Place(scale, squash, tilt, 0, rise);
            int blur = sweep > 3 ? (int)Math.Min(18, 3 + sweep * .44) : 1;
            if (opacity > .002f) PropRig.Compose(img, spin, Math.Min(sweep, 160), place, blur, 2, (float)opacity);

            double hx, hy; place.Apply(HubX, HubY, out hx, out hy);
            if (f <= IntroLast)
            {
                double u = (f - 1) / (double)(IntroLast - 1);
                // Hub charge flash, white into warm gold.
                double flash = Curve.Eval(new double[] { 0, 0, .18, .25, .34, 1.05, .46, .55, .70, .22, 1, .16 }, u);
                Fx.Glow(img, hx, hy, 58 * (1 + .45 * flash), flash * .9, .97);
                if (flash > .3) Fx.Star(img, hx, hy, 118 * flash, 0, (flash - .3) * 1.1, .98);
                for (int s = 0; s < 6; s++)
                {
                    double life = (u - (.20 + s * .055)) / .34;
                    if (life <= 0 || life >= 1) continue;
                    double ang = Hash.U(s, 23) * 360;
                    double dist = (52 + Hash.U(s, 4) * 88) * (.4 + .6 * life);
                    Fx.Star(img, hx + Math.Cos(ang * Math.PI / 180) * dist, hy - Math.Sin(ang * Math.PI / 180) * dist * .8,
                        20 + Hash.U(s, 6) * 16, ang * .7, Math.Sin(life * Math.PI) * .7, .93);
                }
            }
            else if (f <= LoopLast)
            {
                double u = (f - LoopFirst) / (double)(LoopLast - LoopFirst + 1);
                // Steady charged glow plus a compact sparkle trail riding the blade tips.
                Fx.Glow(img, hx, hy, 56, .22 + .05 * Math.Sin(4 * Math.PI * u), .96);
                for (int s = 0; s < 6; s++)
                {
                    double phase = (u + s / 6.0) % 1.0;
                    double ang = spin * .5 + s * 60;
                    double rr = PropRig.Reach * (.55 + .4 * phase) * scale * PropRig.ContentScale;
                    double sx = hx + Math.Cos(ang * Math.PI / 180) * rr;
                    double sy = hy - Math.Sin(ang * Math.PI / 180) * rr * PropRig.K + 26 * phase;
                    double fade = Math.Sin(phase * Math.PI);
                    Fx.Spark(img, sx, sy, 7 + 5 * fade, fade * .34, .9);
                    if (s % 3 == 0) Fx.Star(img, sx, sy, 16 * fade, ang, fade * .3, .95);
                }
            }
            else
            {
                double u = (f - FinishFirst) / (double)(ActFrames - FinishFirst);
                // White-hot pinch, then a short gold star burst as it leaves.
                double burst = Curve.Eval(new double[] { 0, .2, .16, 1.1, .38, .5, .62, .16, 1, 0 }, u);
                // Sized to stay clear of the canvas edge at full spread.
                Fx.Glow(img, hx, hy, 40 + 86 * u, burst * .8, .97);
                Fx.Star(img, hx, hy, 78 + 118 * u, 0, burst * .75, .98);
                Fx.Star(img, hx, hy, 56 + 96 * u, 45, burst * .45, .95);
                for (int s = 0; s < 8; s++)
                {
                    double ang = Hash.U(s, 31) * 360;
                    double dist = (34 + Hash.U(s, 13) * 56) + 118 * u;
                    double fade = Math.Max(0, 1 - u * 1.5);
                    Fx.Spark(img, hx + Math.Cos(ang * Math.PI / 180) * dist, hy - Math.Sin(ang * Math.PI / 180) * dist * .85,
                        6 + Hash.U(s, 17) * 6, fade * .4, .92);
                }
            }
            PropRig.Clean(img);
            Save(img, dir, "Propeller_Activation", f);
        }
    }
}
