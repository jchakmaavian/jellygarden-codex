// Royal Throne level music: ten original looping tracks, composed note by note and synthesised from scratch
// (no samples, no third-party audio). Each is a 44-bar seamless loop: intro, A, B, A', break, B', outro (which leads
// back into the intro).
// Run: dotnet run musicgen.cs -- <outDir> [nameFilter]
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

string outDir = args.Length > 0 ? args[0] : "out";
string filter = args.Length > 1 ? args[1] : "";
Directory.CreateDirectory(outDir);
foreach (var spec in Tracks.All())
{
    if (filter != "" && !spec.Name.Contains(filter)) continue;
    var sw = Stopwatch.StartNew();
    var song = Arranger.Build(spec);
    var (L, R, stats) = song.Render();
    Wav.Write(Path.Combine(outDir, spec.Name + ".wav"), L, R);
    Console.WriteLine($"{spec.Name} ({spec.Title}): {L.Length / (double)Dsp.SR:F1} s, rendered in {sw.Elapsed.TotalSeconds:F1} s");
    Console.WriteLine(stats);
}
foreach (var (name, title, make) in Tracks.Custom())
{
    if (filter != "" && !name.Contains(filter)) continue;
    var sw = Stopwatch.StartNew();
    var (L, R, stats) = make().Render();
    Wav.Write(Path.Combine(outDir, name + ".wav"), L, R);
    Console.WriteLine($"{name} ({title}): {L.Length / (double)Dsp.SR:F1} s, rendered in {sw.Elapsed.TotalSeconds:F1} s");
    Console.WriteLine(stats);
}

static class Dsp
{
    public const int SR = 44100;
    public const double TAU = Math.PI * 2;
    public static double Mtof(double m) => 440.0 * Math.Pow(2, (m - 69) / 12.0);
    public static double Blep(double t, double dt)
    {
        if (t < dt) { t /= dt; return t + t - t * t - 1; }
        if (t > 1 - dt) { t = (t - 1) / dt; return t * t + t + t + 1; }
        return 0;
    }
    // Attack (linear), exponential decay to sustain, exponential release after the note's duration.
    public static double Env(double t, double dur, double a, double d, double s, double r)
    {
        double Pre(double x) => x < a ? x / a : s + (1 - s) * Math.Exp(-(x - a) / Math.Max(d, 1e-4));
        return t < dur ? Pre(t) : Pre(dur) * Math.Exp(-(t - dur) / Math.Max(r, 1e-4));
    }
}

static class Sc
{
    public static readonly int[] Major = { 0, 2, 4, 5, 7, 9, 11 }, Minor = { 0, 2, 3, 5, 7, 8, 10 };
}

// Zero-delay-feedback state-variable filter.
sealed class Svf
{
    double ic1, ic2, g, k, a1, a2, a3, lastF = -1, lastQ = -1;
    void Set(double f, double q)
    {
        if (Math.Abs(f - lastF) < .5 && q == lastQ) return;
        lastF = f; lastQ = q;
        g = Math.Tan(Math.PI * Math.Clamp(f, 10, Dsp.SR * .45) / Dsp.SR); k = 1 / q;
        a1 = 1 / (1 + g * (g + k)); a2 = g * a1; a3 = g * a2;
    }
    void Tick(double v0, out double lp, out double bp, out double hp)
    {
        double v3 = v0 - ic2, v1 = a1 * ic1 + a2 * v3, v2 = ic2 + a2 * ic1 + a3 * v3;
        ic1 = 2 * v1 - ic1; ic2 = 2 * v2 - ic2; lp = v2; bp = v1; hp = v0 - k * v1 - v2;
    }
    public double Lp(double x, double f, double q) { Set(f, q); Tick(x, out var lp, out _, out _); return lp; }
    public double Bp(double x, double f, double q) { Set(f, q); Tick(x, out _, out var bp, out _); return bp; }
    public double Hp(double x, double f, double q) { Set(f, q); Tick(x, out _, out _, out var hp); return hp; }
}

abstract class Inst
{
    public string Name = "";
    public double Gain = 1, Pan = 0, Rev = .12, Dly = 0;
    public bool Ducked = true;
    public abstract float[][] Render(double dur, double midi, double vel, Random rnd);
    protected static float[][] Mono(float[] x) => new[] { x, x };
}

// Subtractive synth: detuned PolyBLEP oscillators, sub oscillator, filter with its own envelope, vibrato.
sealed class Synth : Inst
{
    public enum W { Saw, Square, Tri, Sine }
    public W Wave = W.Saw;
    public int Voices = 1;
    public double Detune = .1, Width = .6, PW = .5, Sub = 0, Noise = 0;
    public double A = .005, D = .2, S = .7, R = .15;
    public double Cut = 2000, Res = .7, FEnv = 0, FA = .001, FD = .15, FS = 0, KeyTrack = .5;
    public double VibDepth = 0, VibRate = 5.5, VibDelay = .2;
    public override float[][] Render(double dur, double midi, double vel, Random rnd)
    {
        int n = (int)((dur + R * 6 + .01) * Dsp.SR);
        var L = new float[n]; var Rr = new float[n];
        double f0 = Dsp.Mtof(midi);
        var ph = new double[Voices]; var det = new double[Voices]; var gl = new double[Voices]; var gr = new double[Voices];
        for (int v = 0; v < Voices; v++)
        {
            ph[v] = rnd.NextDouble();
            double u = Voices == 1 ? .5 : v / (double)(Voices - 1);
            det[v] = Math.Pow(2, Detune * (u - .5) / 12);
            double p = Voices == 1 ? 0 : Width * (u * 2 - 1);
            gl[v] = Math.Sqrt(.5 * (1 - p)); gr[v] = Math.Sqrt(.5 * (1 + p));
        }
        double norm = 1.2 / Math.Sqrt(Voices), subPh = 0, vibPh = rnd.NextDouble();
        double cutBase = Cut * Math.Pow(f0 / 261.6, KeyTrack);
        var fl = new Svf(); var fr = new Svf();
        for (int i = 0; i < n; i++)
        {
            double t = i / (double)Dsp.SR;
            double fm = 1;
            if (VibDepth > 0) fm = Math.Pow(2, VibDepth * Math.Clamp((t - VibDelay) / .25, 0, 1) * Math.Sin(Dsp.TAU * (VibRate * t + vibPh)) / 12);
            double l = 0, r = 0;
            for (int v = 0; v < Voices; v++)
            {
                double dt = f0 * det[v] * fm / Dsp.SR, p = ph[v], s;
                switch (Wave)
                {
                    case W.Saw: s = 2 * p - 1 - Dsp.Blep(p, dt); break;
                    case W.Square: s = (p < PW ? 1 : -1) + Dsp.Blep(p, dt) - Dsp.Blep((p + 1 - PW) % 1, dt); break;
                    case W.Tri: s = 1 - 4 * Math.Abs(p - .5); break;
                    default: s = Math.Sin(Dsp.TAU * p); break;
                }
                p += dt; if (p >= 1) p -= 1; ph[v] = p;
                l += s * gl[v]; r += s * gr[v];
            }
            l *= norm; r *= norm;
            if (Sub > 0) { double sb = Math.Sin(Dsp.TAU * subPh) * Sub * .8; subPh += f0 * .5 * fm / Dsp.SR; if (subPh >= 1) subPh -= 1; l += sb; r += sb; }
            if (Noise > 0) { double nz = (rnd.NextDouble() * 2 - 1) * Noise; l += nz; r += nz; }
            double cut = cutBase;
            if (FEnv != 0) cut *= Math.Pow(2, FEnv * (.55 + .45 * vel) * Dsp.Env(t, dur, FA, FD, FS, R));
            cut = Math.Min(cut, 18000);
            l = fl.Lp(l, cut, Res); r = fr.Lp(r, cut, Res);
            double amp = Dsp.Env(t, dur, A, D, S, R) * vel;
            L[i] = (float)(l * amp); Rr[i] = (float)(r * amp);
        }
        return new[] { L, Rr };
    }
}

// Karplus-Strong plucked string (harp, pizzicato, guitar, upright bass).
sealed class Pluck : Inst
{
    public double Bright = .5, T60 = 1.2, Release = .08;
    public override float[][] Render(double dur, double midi, double vel, Random rnd)
    {
        double f = Dsp.Mtof(midi), delay = Dsp.SR / f;
        int n = (int)((dur + Release * 7 + .02) * Dsp.SR), exc = (int)Math.Ceiling(delay);
        var y = new double[n]; var o = new float[n];
        var e = new double[exc]; double lp = 0, coef = .15 + .85 * Bright * (.6 + .4 * vel);
        for (int i = 0; i < exc; i++) { lp += (rnd.NextDouble() * 2 - 1 - lp) * coef; e[i] = lp; }
        double mean = e.Average();
        double gHold = Math.Pow(.001, 1 / (f * T60)), gRel = Math.Pow(.001, 1 / (f * Release)), d = delay - .5, dc = 0, prev = 0;
        for (int i = 0; i < n; i++)
        {
            double x = i < exc ? e[i] - mean : 0, fb = 0, pos = i - d;
            if (pos >= 1)
            {
                int j = (int)pos; double fr = pos - j;
                double a = y[j] * (1 - fr) + y[Math.Min(j + 1, i - 1)] * fr, b = y[j - 1] * (1 - fr) + y[j] * fr;
                fb = .5 * (a + b);
            }
            y[i] = x + (i < dur * Dsp.SR ? gHold : gRel) * fb;
            double outv = y[i] - prev + .995 * dc; prev = y[i]; dc = outv;
            o[i] = (float)(outv * vel * 1.6);
        }
        return Mono(o);
    }
}

// Modal mallet: marimba, vibraphone, glockenspiel, celesta, kalimba.
sealed class Mallet : Inst
{
    public (double r, double a, double d)[] Modes = { (1, 1, .5) };
    public double Click = .2, Trem = 0, TremRate = 5.5, Damp = .12;
    public bool Ring = false;
    public override float[][] Render(double dur, double midi, double vel, Random rnd)
    {
        double f = Dsp.Mtof(midi), kt = Math.Pow(440 / f, .35), maxD = Modes.Max(m => m.d) * kt;
        double len = Ring ? Math.Min(maxD * 5, 4) : Math.Min(dur + Damp * 6, maxD * 5);
        int n = (int)(len * Dsp.SR) + 1; var o = new float[n];
        var ph0 = Modes.Select(_ => rnd.NextDouble() * .1).ToArray();
        var clickF = new Svf();
        for (int i = 0; i < n; i++)
        {
            double t = i / (double)Dsp.SR, s = 0;
            for (int m = 0; m < Modes.Length; m++)
                if (f * Modes[m].r < 17000) s += Modes[m].a * Math.Exp(-t / (Modes[m].d * kt)) * Math.Sin(Dsp.TAU * (f * Modes[m].r * t + ph0[m]));
            s *= 1 - Math.Exp(-t / .0008);
            if (!Ring && t > dur) s *= Math.Exp(-(t - dur) / Damp);
            if (Trem > 0) s *= 1 - Trem * (.5 + .5 * Math.Sin(Dsp.TAU * TremRate * t));
            if (t < .02) s += clickF.Bp(rnd.NextDouble() * 2 - 1, Math.Min(f * 4, 9000), 1) * Math.Exp(-t / .0015) * Click;
            o[i] = (float)(s * vel * .8);
        }
        int fade = Math.Min(n, 400); for (int i = 0; i < fade; i++) o[n - 1 - i] *= i / (float)fade;
        return Mono(o);
    }
}

// FM electric piano with tine and stereo tremolo.
sealed class EPiano : Inst
{
    public double Index = 1.6, Decay = 1.5, Bell = .25, Trem = .18, Release = .12;
    public override float[][] Render(double dur, double midi, double vel, Random rnd)
    {
        double f = Dsp.Mtof(midi), kt = Math.Pow(440 / f, .3);
        int n = (int)((Math.Min(dur, 6) + Release * 7 + .02) * Dsp.SR);
        var L = new float[n]; var R = new float[n]; double tp = rnd.NextDouble();
        for (int i = 0; i < n; i++)
        {
            double t = i / (double)Dsp.SR;
            double idx = Index * (.4 + .6 * vel) * Math.Exp(-t / .3) + .3;
            double s = Math.Sin(Dsp.TAU * f * t + idx * Math.Sin(Dsp.TAU * f * t));
            if (f * 7.1 < 16000) s += Bell * vel * Math.Exp(-t / .022) * Math.Sin(Dsp.TAU * f * 7.1 * t);
            double amp = Math.Exp(-t / (Decay * kt)) * (1 - Math.Exp(-t / .001));
            if (t > dur) amp *= Math.Exp(-(t - dur) / Release);
            double tr = Trem * Math.Sin(Dsp.TAU * (4.6 * t + tp));
            L[i] = (float)(s * amp * vel * (1 + tr) * .7); R[i] = (float)(s * amp * vel * (1 - tr) * .7);
        }
        return new[] { L, R };
    }
}

// Drawbar organ with percussion and a rotary-speaker wobble.
sealed class Organ : Inst
{
    public double[] Bars = { .4, 1, .6, .7, .35, .25 };
    static readonly double[] Ratios = { .5, 1, 1.5, 2, 3, 4 };
    public double Perc = .35, Leslie = .2, A = .004, R = .05;
    public override float[][] Render(double dur, double midi, double vel, Random rnd)
    {
        double f = Dsp.Mtof(midi);
        int n = (int)((dur + R * 7 + .02) * Dsp.SR);
        var L = new float[n]; var Rr = new float[n]; double lp = rnd.NextDouble();
        for (int i = 0; i < n; i++)
        {
            double t = i / (double)Dsp.SR, s = 0;
            for (int k = 0; k < Bars.Length; k++) if (f * Ratios[k] < 16000) s += Bars[k] * Math.Sin(Dsp.TAU * f * Ratios[k] * t);
            s = s / 2.8 + Perc * Math.Exp(-t / .18) * Math.Sin(Dsp.TAU * f * 3 * t);
            if (t < .004) s += (rnd.NextDouble() * 2 - 1) * .08;
            double amp = Dsp.Env(t, dur, A, .1, 1, R) * vel, w = Leslie * Math.Sin(Dsp.TAU * (6.3 * t + lp));
            L[i] = (float)(s * amp * (1 + w)); Rr[i] = (float)(s * amp * (1 - w));
        }
        return new[] { L, Rr };
    }
}

// Breathy flute: pan flute, whistle, piccolo.
sealed class Flute : Inst
{
    public double Breath = .12, Vib = .15, Chiff = .25, A = .04, R = .08;
    public override float[][] Render(double dur, double midi, double vel, Random rnd)
    {
        double f = Dsp.Mtof(midi);
        int n = (int)((dur + R * 7 + .02) * Dsp.SR);
        var o = new float[n]; var bp = new Svf(); double p = 0, vp = rnd.NextDouble();
        for (int i = 0; i < n; i++)
        {
            double t = i / (double)Dsp.SR;
            double fm = Math.Pow(2, Vib * Math.Clamp((t - .18) / .25, 0, 1) * Math.Sin(Dsp.TAU * (5.2 * t + vp)) / 12);
            p += f * fm / Dsp.SR; if (p >= 1) p -= 1;
            double s = Math.Sin(Dsp.TAU * p) + .22 * Math.Sin(2 * Dsp.TAU * p) + .07 * Math.Sin(3 * Dsp.TAU * p);
            double nb = bp.Bp(rnd.NextDouble() * 2 - 1, Math.Min(f * 2, 12000), 1.6) * (Breath + Chiff * Math.Exp(-t / .045)) * 2.2;
            o[i] = (float)((s + nb) * Dsp.Env(t, dur, A, .25, .85, R) * vel * .8);
        }
        return Mono(o);
    }
}

sealed class Drum : Inst
{
    public enum K { Kick, Snare, Clap, HatC, HatO, Shaker, Rim, Tom, Crash, Clave, Snap, Taiko, Tamb }
    public K Kind;
    public double Tune = 1, Decay = 1, Tone = .5;
    static readonly double[] Metal = { 205.3, 304.4, 369.6, 522.7, 540.0, 800.0 };
    public override float[][] Render(double dur, double midi, double vel, Random rnd)
    {
        double len = Kind switch
        {
            K.Kick => .5, K.Snare => .35, K.Clap => .4, K.HatC => .12, K.HatO => .6, K.Shaker => .15, K.Rim => .08, K.Tom => .7,
            K.Crash => 2.6, K.Clave => .1, K.Snap => .15, K.Taiko => 1.5, _ => .35
        } * Decay;
        int n = (int)(len * Dsp.SR); var x = new float[n];
        var f1 = new Svf(); var f2 = new Svf(); double ph = 0; var mph = new double[6];
        for (int i = 0; i < n; i++)
        {
            double t = i / (double)Dsp.SR, nz = rnd.NextDouble() * 2 - 1, s = 0;
            switch (Kind)
            {
                case K.Kick:
                    {
                        double f = 46 * Tune * (1 + 2.4 * Math.Exp(-t / .028)); ph += f / Dsp.SR;
                        s = Math.Sin(Dsp.TAU * ph) * Math.Exp(-t / (.3 * Decay)) + nz * Math.Exp(-t / .002) * .25 * Tone;
                        s = Math.Tanh(1.8 * s) / Math.Tanh(1.8); break;
                    }
                case K.Snare:
                    {
                        double tone = (Math.Sin(Dsp.TAU * 185 * Tune * t) * .65 + Math.Sin(Dsp.TAU * 330 * Tune * t) * .35) * Math.Exp(-t / .05);
                        double nn = f2.Lp(f1.Hp(nz, 1400, .7), 9000, .7) * Math.Exp(-t / (.12 * Decay));
                        s = Math.Tanh((tone * .8 + nn * 1.1) * 1.3); break;
                    }
                case K.Clap:
                    {
                        double e = 0;
                        for (int k = 0; k < 3; k++) { double tk = t - k * .0105; if (tk >= 0) e = Math.Max(e, Math.Exp(-tk / .0035)); }
                        if (t >= .021) e = Math.Max(e, .75 * Math.Exp(-(t - .021) / (.11 * Decay)));
                        s = f1.Bp(nz, 1250 * Tune, 1.3) * e * 2.2; break;
                    }
                case K.HatC: case K.HatO: case K.Crash: case K.Tamb:
                    {
                        double m = 0, mul = Kind == K.Crash ? 1.45 : Kind == K.Tamb ? 2.1 : 1.0;
                        for (int k = 0; k < 6; k++) { mph[k] += Metal[k] * mul * Tune / Dsp.SR; m += mph[k] % 1 < .5 ? 1 : -1; }
                        m = m / 6 * .7 + nz * .45;
                        double hp = f1.Hp(m, Kind == K.Crash ? 3800 : Kind == K.Tamb ? 6500 : 7200, .8);
                        double dec = (Kind == K.HatC ? .032 : Kind == K.HatO ? .22 : Kind == K.Crash ? 1.1 : .09) * Decay;
                        double env = Math.Exp(-t / dec) * Math.Min(1, t / .0008);
                        if (Kind == K.Tamb && t > .02) env = Math.Max(env, .6 * Math.Exp(-(t - .02) / .06));
                        s = f2.Lp(hp, Kind == K.Crash ? 11000 : 15000, .7) * env * 1.4; break;
                    }
                case K.Shaker:
                    s = f1.Hp(nz, 5500, .7) * Math.Min(1, t / .014) * Math.Exp(-Math.Max(0, t - .014) / (.045 * Decay)); break;
                case K.Rim:
                    s = Math.Sin(Dsp.TAU * 1720 * Tune * t) * .6 * Math.Exp(-t / .009) + Math.Sin(Dsp.TAU * 480 * Tune * t) * .5 * Math.Exp(-t / .014)
                        + f1.Hp(nz, 3000, .7) * Math.Exp(-t / .004) * .6; break;
                case K.Tom:
                    {
                        double f = Dsp.Mtof(midi) * Tune * (1 + .6 * Math.Exp(-t / .045)); ph += f / Dsp.SR;
                        s = Math.Tanh(1.4 * (Math.Sin(Dsp.TAU * ph) * Math.Exp(-t / (.26 * Decay)) + f1.Lp(nz, 900, .7) * Math.Exp(-t / .02) * .4)); break;
                    }
                case K.Taiko:
                    {
                        double f = 68 * Tune * (1 + .9 * Math.Exp(-t / .035)); ph += f / Dsp.SR;
                        s = Math.Sin(Dsp.TAU * ph) * Math.Exp(-t / (.5 * Decay)) + f1.Lp(nz, 500, .9) * Math.Exp(-t / .06) * .9 + f2.Bp(nz, 1800, 1) * Math.Exp(-t / .008) * .5;
                        s = Math.Tanh(1.6 * s); break;
                    }
                case K.Clave:
                    s = Math.Sin(Dsp.TAU * 2450 * Tune * t) * Math.Exp(-t / .028) * Math.Min(1, t / .0005); break;
                case K.Snap:
                    s = f1.Bp(nz, 2300 * Tune, 1.8) * (Math.Exp(-t / .018) + (t > .006 ? .5 * Math.Exp(-(t - .006) / .01) : 0)) * 2.5; break;
            }
            x[i] = (float)(s * vel);
        }
        int fade = Math.Min(n, 200); for (int i = 0; i < fade; i++) x[n - 1 - i] *= i / (float)fade;
        return Mono(x);
    }
}

// Noise sweep that builds into a section.
sealed class Riser : Inst
{
    public override float[][] Render(double dur, double midi, double vel, Random rnd)
    {
        int n = (int)(dur * Dsp.SR); var L = new float[n]; var R = new float[n]; var fl = new Svf(); var fr = new Svf();
        for (int i = 0; i < n; i++)
        {
            double u = i / (double)n, cut = 300 * Math.Pow(30, u), g = u * u * vel * (i > n - 300 ? (n - i) / 300.0 : 1);
            L[i] = (float)(fl.Bp(rnd.NextDouble() * 2 - 1, cut, 2.2) * g * 2); R[i] = (float)(fr.Bp(rnd.NextDouble() * 2 - 1, cut, 2.2) * g * 2);
        }
        return new[] { L, R };
    }
}

sealed class Chord
{
    public int Root, Bass; public int[] Iv = Array.Empty<int>();
    public bool Has(int m) { int r = ((m - Root) % 12 + 12) % 12; foreach (var v in Iv) if (v % 12 == r) return true; return false; }
    public static int Pc(string s)
    {
        int p = "C D EF G A B".IndexOf(s[0]);
        if (p < 0) throw new Exception("note " + s);
        if (s.Length > 1 && s[1] == '#') p++; else if (s.Length > 1 && s[1] == 'b') p--;
        return (p + 12) % 12;
    }
    public static Chord Parse(string s)
    {
        var parts = s.Split('/'); var m = parts[0]; int i = m.Length > 1 && (m[1] == '#' || m[1] == 'b') ? 2 : 1;
        int[] iv = m.Substring(i) switch
        {
            "" => new[] { 0, 4, 7 }, "m" => new[] { 0, 3, 7 }, "7" => new[] { 0, 4, 7, 10 }, "m7" => new[] { 0, 3, 7, 10 },
            "maj7" => new[] { 0, 4, 7, 11 }, "sus4" => new[] { 0, 5, 7 }, "sus2" => new[] { 0, 2, 7 }, "add9" => new[] { 0, 4, 7, 14 },
            "6" => new[] { 0, 4, 7, 9 }, "m6" => new[] { 0, 3, 7, 9 }, "dim" => new[] { 0, 3, 6 }, "9" => new[] { 0, 4, 7, 10, 14 },
            _ => throw new Exception("chord " + s)
        };
        return new Chord { Root = Pc(m.Substring(0, i)), Iv = iv, Bass = parts.Length > 1 ? Pc(parts[1]) : Pc(m.Substring(0, i)) };
    }
}

sealed class Song
{
    public double Bpm, Swing, DuckAmt = .3, RevReturn = 1.2, DlyFb = .35, Room = .82, Damp = .35, MasterLowpass = 0;
    public int Bars, DlySteps = 3; public int[] Scale;
    public readonly Random Rnd;
    readonly List<(Inst I, double T, double Dur, double Midi, double Vel)> evs = new();
    readonly Chord[] chordAt;
    public Song(int bars, double bpm, int[] scale, int seed) { Bars = bars; Bpm = bpm; Scale = scale; Rnd = new Random(seed); chordAt = new Chord[bars * 16]; }
    public double Step => 60.0 / Bpm / 4;
    public double T(int step) => step * Step + (step % 2 == 1 ? Swing * Step : 0);

    public void Prog(int bar, int bars, string prog)
    {
        var toks = prog.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (int b = 0; b < bars; b++)
        {
            var parts = toks[b % toks.Length].Split('_').Select(Chord.Parse).ToArray();
            for (int s = 0; s < 16; s++) chordAt[(bar + b) * 16 + s] = parts[s * parts.Length / 16];
        }
    }
    Chord ChordAt(int step) => chordAt[Math.Clamp(step, 0, chordAt.Length - 1)] ?? throw new Exception("no chord at step " + step);

    void Add(Inst i, double t, double dur, double midi, double vel)
    {
        t += (Rnd.NextDouble() * 2 - 1) * (i is Drum ? .0012 : .0025);
        evs.Add((i, Math.Max(0, t), dur, midi, vel * (1 + (Rnd.NextDouble() * 2 - 1) * .05)));
    }
    // One character per 16th; bars are separated by spaces and the pattern repeats; '-' ties, '.' rests.
    static void Walk(int bar, int bars, string pat, Action<int, char, int> f)
    {
        var bs = pat.Split(new[] { ' ', '|' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var b in bs) if (b.Length != 16) throw new Exception($"pattern bar '{b}' is {b.Length} steps");
        var seq = new List<char>(); for (int b = 0; b < bars; b++) seq.AddRange(bs[b % bs.Length]);
        for (int k = 0; k < seq.Count; k++)
        {
            char c = seq[k]; if (c == '.' || c == '-') continue;
            int len = 1; while (k + len < seq.Count && seq[k + len] == '-') len++;
            f(bar * 16 + k, c, len);
        }
    }
    static int Fit(int pc, int low) => low + ((pc - low) % 12 + 12) % 12;

    public void Drums(Inst d, int bar, int bars, string pat, double vel = 1, int midi = 60) => Walk(bar, bars, pat, (s, c, len) =>
    {
        if (c == 'r') { Add(d, T(s), Step * .5, midi, .55 * vel); Add(d, T(s) + Step * .5, Step * .5, midi, .7 * vel); return; }
        Add(d, T(s), Step * len, midi, (c == 'X' ? 1 : c == 'o' ? .42 : .78) * vel);
    });
    public void Bass(Inst b, int bar, int bars, string pat, int low = 36, double vel = 1, double legato = .85) => Walk(bar, bars, pat, (s, c, len) =>
    {
        var ch = ChordAt(s); int root = Fit(ch.Bass, low);
        int m = c switch
        {
            '8' => root + 12, '5' => Fit(ch.Root + 7, low), '3' => Fit(ch.Root + ch.Iv[1], low),
            '7' => Fit(ch.Root + (ch.Iv.Length > 3 ? ch.Iv[3] : 10), low), 'c' => Fit(ChordAt(s + len).Bass, low) - 1, _ => root
        };
        Add(b, T(s), Step * len * legato, m, (c == 'R' || c == '8' ? 1 : .9) * vel);
    });
    public static int[] Voice(Chord ch, int center, int count)
    {
        var tones = new List<int>(); for (int m = center - 14; m <= center + 14; m++) if (ch.Has(m)) tones.Add(m);
        int[] best = tones.Take(count).ToArray(); double score = 1e9;
        for (int i = 0; i + count <= tones.Count; i++) { var v = tones.Skip(i).Take(count).ToArray(); double sc = Math.Abs(v.Average() - center); if (sc < score) { score = sc; best = v; } }
        return best;
    }
    public void Chords(Inst c, int bar, int bars, string pat, int center = 64, int count = 3, double vel = 1, double legato = .9, double strum = 0) => Walk(bar, bars, pat, (s, ch, len) =>
    {
        var notes = Voice(ChordAt(s), center, count); double v = ch == 'X' ? 1 : ch == 'o' ? .55 : .82;
        for (int k = 0; k < notes.Length; k++) Add(c, T(s) + k * strum, Step * len * legato, notes[k], v * vel);
    });
    public void Arp(Inst a, int bar, int bars, string pat, int low = 64, double vel = 1, double legato = .9) => Walk(bar, bars, pat, (s, c, len) =>
    {
        if (!char.IsDigit(c)) return;
        var ch = ChordAt(s); var tones = new List<int>(); for (int m = low; tones.Count < 10; m++) if (ch.Has(m)) tones.Add(m);
        Add(a, T(s), Step * len * legato, tones[c - '0'], vel * (s % 4 == 0 ? 1 : .8));
    });
    // Scale degrees 1-7, with # or b before and ' (octave up) or , (octave down) after; one token per grid step.
    public void Melody(Inst m, int bar, string notes, int baseMidi, int grid = 2, double vel = 1, int shift = 0, double legato = .92, int transpose = 0, int maxTokens = int.MaxValue)
    {
        var toks = notes.Split(new[] { ' ', '|', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        if (toks.Length % (16 / grid) != 0) throw new Exception($"melody has {toks.Length} steps: {notes}");
        toks = toks.Take(maxTokens).ToArray();
        for (int k = 0; k < toks.Length; k++)
        {
            string tk = toks[k]; if (tk == "." || tk == "-") continue;
            int len = 1; while (k + len < toks.Length && toks[k + len] == "-") len++;
            int s = bar * 16 + k * grid;
            Add(m, T(s), Step * grid * len * legato, Degree(tk, baseMidi, shift) + transpose, vel * ((k * grid) % 8 == 0 ? 1 : .88));
        }
    }
    int Degree(string tk, int baseMidi, int shift)
    {
        int i = 0, acc = 0; while (tk[i] == '#' || tk[i] == 'b') { acc += tk[i] == '#' ? 1 : -1; i++; }
        int d = tk[i] - '1'; if (d < 0 || d > 6) throw new Exception("degree " + tk);
        int oct = 0; for (i++; i < tk.Length; i++) { if (tk[i] == '\'') oct++; else if (tk[i] == ',') oct--; }
        int a = d + 7 * oct + shift, o = (int)Math.Floor(a / 7.0);
        return baseMidi + Scale[a - 7 * o] + 12 * o + acc;
    }
    public void Hit(Inst i, int bar, double step, double durSteps, double midi = 60, double vel = 1) => evs.Add((i, (bar * 16 + step) * Step, durSteps * Step, midi, vel));

    public (float[] L, float[] R, string stats) Render()
    {
        const int SR = Dsp.SR;
        int N = (int)Math.Round(Bars * 16 * Step * SR), tail = 5 * SR, M = N + tail;
        float[] dL = new float[M], dR = new float[M], kL = new float[M], kR = new float[M], rv = new float[M], dl = new float[M];
        var energy = new Dictionary<string, double>(); var kicks = new List<double>();
        foreach (var e in evs)
        {
            if (e.I is Drum dk && dk.Kind == Drum.K.Kick) kicks.Add(e.T);
            var x = e.I.Render(e.Dur, e.Midi, e.Vel, Rnd);
            int s0 = (int)(e.T * SR);
            double gl = Math.Cos((e.I.Pan + 1) * Math.PI / 4) * Math.Sqrt(2) * e.I.Gain, gr = Math.Sin((e.I.Pan + 1) * Math.PI / 4) * Math.Sqrt(2) * e.I.Gain;
            var tL = e.I.Ducked ? dL : kL; var tR = e.I.Ducked ? dR : kR; double en = 0; var weight = new Svf();
            for (int i = 0; i < x[0].Length && s0 + i < M; i++)
            {
                int j = s0 + i; double l = x[0][i] * gl, r = x[1][i] * gr, m = (l + r) * .5;
                tL[j] += (float)l; tR[j] += (float)r; rv[j] += (float)(m * e.I.Rev); dl[j] += (float)(m * e.I.Dly);
                double w = weight.Hp(m, 200, .7); en += w * w;
            }
            energy[e.I.Name] = energy.GetValueOrDefault(e.I.Name) + en;
        }
        // Sidechain: everything marked Ducked dips under each kick (and under the next loop's first kicks, for the wrap).
        var duck = new float[M]; Array.Fill(duck, 1f); double loopSec = N / (double)SR;
        foreach (var k0 in kicks) foreach (var k in new[] { k0, k0 + loopSec })
            {
                int s0 = (int)(k * SR);
                for (int i = 0; i < (int)(.3 * SR); i++)
                {
                    int j = s0 + i; if (j < 0 || j >= M) continue; double t = i / (double)SR;
                    float g = (float)(1 - DuckAmt * (t < .006 ? t / .006 : Math.Exp(-(t - .006) / .085))); if (g < duck[j]) duck[j] = g;
                }
            }
        for (int i = 0; i < M; i++) { dL[i] *= duck[i]; dR[i] *= duck[i]; }
        var (rL, rR) = Reverb(rv); var (eL, eR) = PingPong(dl);
        float[] L = new float[M], R = new float[M];
        double eRev = 0, eDly = 0; var wRev = new Svf(); var wDly = new Svf();
        for (int i = 0; i < M; i++)
        {
            float wl = (float)(rL[i] * RevReturn), wr = (float)(rR[i] * RevReturn);
            L[i] = dL[i] + kL[i] + wl + eL[i]; R[i] = dR[i] + kR[i] + wr + eR[i];
            double a = wRev.Hp(.5 * (wl + wr), 200, .7), b = wDly.Hp(.5 * (eL[i] + eR[i]), 200, .7); eRev += a * a; eDly += b * b;
        }
        energy["(reverb)"] = eRev; energy["(delay)"] = eDly;
        // The loop is seamless: everything that rings past the end is laid over the start.
        for (int i = 0; i < tail; i++) { L[i] += L[N + i]; R[i] += R[N + i]; }
        Array.Resize(ref L, N); Array.Resize(ref R, N);
        Master(L, R);
        double total = energy.Values.Sum();
        var sb = new StringBuilder("  weighted mix (dB of total):");
        foreach (var kv in energy.OrderByDescending(k => k.Value)) sb.Append($" {kv.Key} {10 * Math.Log10(kv.Value / total + 1e-12):F0}");
        return (L, R, sb.ToString());
    }

    (float[], float[]) Reverb(float[] input)
    {
        int[] combT = { 1116, 1188, 1277, 1356, 1422, 1491, 1557, 1617 }, apT = { 556, 441, 341, 225 };
        var outs = new float[2][];
        for (int ch = 0; ch < 2; ch++)
        {
            int sp = ch * 23; var o = new float[input.Length];
            var cb = combT.Select(t => new float[t + sp]).ToArray(); var ci = new int[8]; var cf = new double[8];
            var ab = apT.Select(t => new float[t + sp]).ToArray(); var ai = new int[4];
            for (int i = 0; i < input.Length; i++)
            {
                double x = input[i] * .015, acc = 0;
                for (int c = 0; c < 8; c++)
                {
                    var b = cb[c]; double y = b[ci[c]]; cf[c] = y * (1 - Damp) + cf[c] * Damp; b[ci[c]] = (float)(x + cf[c] * Room);
                    if (++ci[c] >= b.Length) ci[c] = 0; acc += y;
                }
                for (int a = 0; a < 4; a++)
                {
                    var b = ab[a]; double bo = b[ai[a]], y = -acc + bo; b[ai[a]] = (float)(acc + bo * .5);
                    if (++ai[a] >= b.Length) ai[a] = 0; acc = y;
                }
                o[i] = (float)acc;
            }
            outs[ch] = o;
        }
        return (outs[0], outs[1]);
    }
    (float[], float[]) PingPong(float[] input)
    {
        int d = (int)(DlySteps * Step * Dsp.SR); var bl = new float[d]; var br = new float[d]; int idx = 0; double lpL = 0, lpR = 0;
        var L = new float[input.Length]; var R = new float[input.Length];
        for (int i = 0; i < input.Length; i++)
        {
            double ol = bl[idx], or = br[idx]; lpL += (ol - lpL) * .35; lpR += (or - lpR) * .35;
            bl[idx] = (float)(input[i] + lpR * DlyFb); br[idx] = (float)(lpL * DlyFb);
            L[i] = (float)ol; R[i] = (float)or; if (++idx >= d) idx = 0;
        }
        return (L, R);
    }
    // Every stage with memory runs over the loop twice and keeps the second pass, so the loop point stays seamless.
    void Master(float[] L, float[] R)
    {
        int n = L.Length;
        foreach (var x in new[] { L, R })
        {
            double a = Math.Exp(-Dsp.TAU * 30 / Dsp.SR), y = 0, xp = 0;
            for (int pass = 0; pass < 2; pass++) for (int i = 0; i < n; i++) { double v = x[i], o = a * (y + v - xp); xp = v; y = o; if (pass == 1) x[i] = (float)o; }
            // Optional warm top end (an orchestral recording's darker air).
            if (MasterLowpass > 0)
            {
                var lp = new Svf();
                for (int pass = 0; pass < 2; pass++) for (int i = 0; i < n; i++) { double o = lp.Lp(x[i], MasterLowpass, .6); if (pass == 1) x[i] = (float)o; }
            }
        }
        float pk = 1e-9f; for (int i = 0; i < n; i++) pk = Math.Max(pk, Math.Max(Math.Abs(L[i]), Math.Abs(R[i])));
        for (int i = 0; i < n; i++) { L[i] /= pk; R[i] /= pk; }
        double att = Math.Exp(-1 / (.006 * Dsp.SR)), rel = Math.Exp(-1 / (.16 * Dsp.SR)), env = 0, thr = Math.Pow(10, -10 / 20.0), ratio = 2.2;
        for (int pass = 0; pass < 2; pass++)
            for (int i = 0; i < n; i++)
            {
                double lv = Math.Max(Math.Abs(L[i]), Math.Abs(R[i])); env = lv > env ? att * env + (1 - att) * lv : rel * env + (1 - rel) * lv;
                if (pass == 0) continue;
                double g = env > thr ? Math.Pow(env / thr, 1 / ratio - 1) : 1; L[i] = (float)(L[i] * g); R[i] = (float)(R[i] * g);
            }
        double sum = 0; for (int i = 0; i < n; i++) sum += L[i] * L[i] + R[i] * R[i];
        double gain = Math.Pow(10, -15 / 20.0) / Math.Sqrt(sum / (2 * n));
        static float Soft(double x) { double a = Math.Abs(x), k = .8; return (float)(.97 * (a <= k ? x : Math.Sign(x) * (k + (1 - k) * Math.Tanh((a - k) / (1 - k))))); }
        for (int i = 0; i < n; i++) { L[i] = Soft(L[i] * gain); R[i] = Soft(R[i] * gain); }
    }
}

static class Wav
{
    public static void Write(string path, float[] L, float[] R)
    {
        using var bw = new BinaryWriter(File.Create(path)); int n = L.Length;
        bw.Write("RIFF"u8); bw.Write(36 + n * 4); bw.Write("WAVEfmt "u8); bw.Write(16); bw.Write((short)1); bw.Write((short)2);
        bw.Write(Dsp.SR); bw.Write(Dsp.SR * 4); bw.Write((short)4); bw.Write((short)16); bw.Write("data"u8); bw.Write(n * 4);
        for (int i = 0; i < n; i++) { bw.Write((short)Math.Clamp(Math.Round(L[i] * 32767.0), -32768, 32767)); bw.Write((short)Math.Clamp(Math.Round(R[i] * 32767.0), -32768, 32767)); }
    }
}

static class Pre
{
    public static Drum Kick(double tune = 1, double decay = 1, double gain = .9) => new() { Name = "kick", Kind = Drum.K.Kick, Tune = tune, Decay = decay, Gain = gain, Rev = 0, Ducked = false };
    public static Drum Snare(double gain = .62, double tune = 1, string name = "snare") => new() { Name = name, Kind = Drum.K.Snare, Tune = tune, Gain = gain, Rev = .14, Ducked = false };
    public static Drum Clap(double gain = .68) => new() { Name = "clap", Kind = Drum.K.Clap, Gain = gain, Rev = .18, Ducked = false };
    public static Drum HatC(double gain = .32, double pan = .2) => new() { Name = "hat", Kind = Drum.K.HatC, Gain = gain, Pan = pan, Rev = .03, Ducked = false };
    public static Drum HatO(double gain = .18, double pan = -.15) => new() { Name = "open hat", Kind = Drum.K.HatO, Gain = gain, Pan = pan, Rev = .06, Ducked = false };
    public static Drum Shaker(double gain = .2, double pan = -.3) => new() { Name = "shaker", Kind = Drum.K.Shaker, Gain = gain, Pan = pan, Rev = .05, Ducked = false };
    public static Drum Tamb(double gain = .17, double pan = .35) => new() { Name = "tambourine", Kind = Drum.K.Tamb, Gain = gain, Pan = pan, Rev = .08, Ducked = false };
    public static Drum Rim(double gain = .22, double pan = .15) => new() { Name = "rim", Kind = Drum.K.Rim, Gain = gain, Pan = pan, Rev = .12, Ducked = false };
    public static Drum Clave(double gain = .14, double pan = .3) => new() { Name = "clave", Kind = Drum.K.Clave, Gain = gain, Pan = pan, Rev = .15, Ducked = false };
    public static Drum Snap(double gain = .3) => new() { Name = "snap", Kind = Drum.K.Snap, Gain = gain, Rev = .2, Ducked = false };
    public static Drum Tom(double gain = .38) => new() { Name = "tom", Kind = Drum.K.Tom, Gain = gain, Rev = .15, Ducked = false };
    public static Drum Taiko(double gain = .55) => new() { Name = "taiko", Kind = Drum.K.Taiko, Gain = gain, Rev = .25, Ducked = false };
    public static Drum Crash(double gain = .14) => new() { Name = "crash", Kind = Drum.K.Crash, Gain = gain, Rev = .1, Ducked = false };
    public static Riser Riser(double gain = .1) => new() { Name = "riser", Gain = gain, Rev = .3, Ducked = false };

    public static Synth PluckBass(double gain = .45) => new() { Name = "bass", Wave = Synth.W.Saw, Sub = .7, A = .003, D = .22, S = .55, R = .05, Cut = 260, Res = 1.1, FEnv = 2.2, FD = .1, KeyTrack = .3, Gain = gain, Rev = 0 };
    public static Synth SubBass(double gain = .5) => new() { Name = "bass", Wave = Synth.W.Tri, Sub = .8, A = .005, D = .3, S = .8, R = .06, Cut = 700, KeyTrack = 0, Gain = gain, Rev = 0 };
    public static Synth SquareBass(double gain = .3) => new() { Name = "bass", Wave = Synth.W.Square, Sub = .5, A = .002, D = .15, S = .7, R = .03, Cut = 1400, Gain = gain, Rev = 0 };
    public static Synth Tuba(double gain = .5) => new() { Name = "tuba", Wave = Synth.W.Saw, Sub = .4, A = .025, D = .2, S = .85, R = .07, Cut = 380, FEnv = 1.2, FA = .04, FD = .25, FS = .3, KeyTrack = .3, Gain = gain, Rev = .05 };
    public static Synth ReeseBass(double gain = .4) => new() { Name = "bass", Wave = Synth.W.Saw, Voices = 2, Detune = .25, Width = .3, Sub = .6, A = .005, D = .3, S = .9, R = .06, Cut = 420, Res = .9, KeyTrack = .3, Gain = gain, Rev = 0 };
    public static Synth SuperStab(double gain = .16) => new() { Name = "stab", Voices = 5, Detune = .32, Width = .8, Cut = 900, Res = .8, FEnv = 2.4, FD = .16, FS = .15, A = .002, D = .25, S = .35, R = .12, Gain = gain, Rev = .25 };
    public static Synth SuperPad(double gain = .12) => new() { Name = "pad", Voices = 7, Detune = .3, Width = .9, Cut = 1300, Res = .6, A = .35, D = .6, S = .85, R = .7, Gain = gain, Rev = .4 };
    public static Synth WarmPad(double gain = .18) => new() { Name = "pad", Wave = Synth.W.Tri, Voices = 4, Detune = .2, Width = .8, Cut = 2000, A = .5, D = .5, S = .9, R = .8, Gain = gain, Rev = .45 };
    public static Synth SawPluck(double gain = .3) => new() { Name = "pluck lead", Voices = 2, Detune = .1, Width = .5, Cut = 700, Res = 1.0, FEnv = 3.2, FD = .17, FS = .08, A = .002, D = .35, S = .15, R = .15, Gain = gain, Rev = .2, Dly = .18 };
    public static Synth SquareLead(double gain = .18) => new() { Name = "square lead", Wave = Synth.W.Square, Voices = 2, Detune = .07, Cut = 3200, A = .012, D = .2, S = .85, R = .1, VibDepth = .14, Gain = gain, Rev = .22, Dly = .15, Ducked = false };
    public static Synth ChipLead(double gain = .15) => new() { Name = "chip lead", Wave = Synth.W.Square, PW = .25, Cut = 9000, A = .003, D = .1, S = .8, R = .04, VibDepth = .2, VibDelay = .12, Gain = gain, Rev = .12, Dly = .12, Ducked = false };
    public static Synth SawLead(double gain = .17) => new() { Name = "saw lead", Voices = 3, Detune = .14, Width = .5, Cut = 3200, Res = .8, FEnv = .8, FD = .2, A = .01, D = .25, S = .8, R = .14, VibDepth = .16, Gain = gain, Rev = .25, Dly = .18, Ducked = false };
    public static Synth Brass(double gain = .2, string name = "brass") => new() { Name = name, Voices = 3, Detune = .12, Width = .6, Cut = 650, FEnv = 2.3, FA = .05, FD = .35, FS = .5, A = .025, D = .2, S = .85, R = .12, VibDepth = .08, Gain = gain, Rev = .25, Ducked = false };
    public static Synth BrassStab(double gain = .18) => new() { Name = "brass stab", Voices = 3, Detune = .12, Width = .6, Cut = 650, FEnv = 2.3, FA = .02, FD = .15, FS = .4, A = .01, D = .15, S = .4, R = .08, Gain = gain, Rev = .22 };
    public static Synth Strings(double gain = .14) => new() { Name = "strings", Voices = 3, Detune = .16, Width = .7, Cut = 2600, A = .006, D = .12, S = .1, R = .08, Gain = gain, Rev = .3 };
    public static Synth Accordion(double gain = .16, string name = "accordion") => new() { Name = name, Wave = Synth.W.Square, Voices = 3, Detune = .18, Width = .4, PW = .42, Cut = 2600, A = .02, D = .1, S = .95, R = .06, Gain = gain, Rev = .18, Ducked = false };
    public static Synth ChipArp(double gain = .1) => new() { Name = "chip arp", Wave = Synth.W.Square, Cut = 6000, A = .001, D = .08, S = .4, R = .02, Gain = gain, Rev = .1, Pan = .3 };
    public static Synth ChipStab(double gain = .09) => new() { Name = "chip chords", Wave = Synth.W.Square, Cut = 4000, A = .001, D = .12, S = .5, R = .03, Gain = gain, Rev = .12 };
    public static Synth TriLead(double gain = .22) => new() { Name = "tri lead", Wave = Synth.W.Tri, Cut = 8000, A = .01, D = .2, S = .9, R = .08, VibDepth = .15, Gain = gain, Rev = .2, Ducked = false };
    public static Synth Clav(double gain = .12) => new() { Name = "clav", Wave = Synth.W.Square, PW = .3, Cut = 2200, Res = 1.2, FEnv = 1.5, FD = .06, A = .001, D = .12, S = .2, R = .03, Gain = gain, Rev = .08, Pan = .25 };
    public static Pluck Harp(double gain = .3) => new() { Name = "harp", Bright = .55, T60 = 1.6, Release = .25, Gain = gain, Rev = .35, Pan = -.25 };
    public static Pluck Pizz(double gain = .32, string name = "pizzicato") => new() { Name = name, Bright = .7, T60 = .3, Release = .06, Gain = gain, Rev = .25 };
    public static Pluck Guitar(double gain = .24) => new() { Name = "guitar", Bright = .45, T60 = 1.0, Release = .08, Gain = gain, Rev = .15, Pan = -.2 };
    public static Pluck UprightBass(double gain = .62) => new() { Name = "bass", Bright = .04, T60 = .8, Release = .06, Gain = gain, Rev = .02 };
    public static Mallet Marimba(double gain = .3, string name = "marimba") => new() { Name = name, Modes = new[] { (1.0, 1.0, .42), (3.93, .28, .07), (9.2, .07, .02) }, Click = .3, Gain = gain, Rev = .2 };
    public static Mallet Vibes(double gain = .26, string name = "vibes") => new() { Name = name, Modes = new[] { (1.0, 1.0, 1.6), (4.0, .22, .35), (10.0, .05, .08) }, Trem = .3, TremRate = 5, Damp = .3, Gain = gain, Rev = .3, Ducked = false };
    public static Mallet Glock(double gain = .18, string name = "glockenspiel") => new() { Name = name, Modes = new[] { (1.0, 1.0, 1.1), (2.76, .32, .35), (5.4, .14, .12), (8.93, .06, .05) }, Click = .15, Ring = true, Gain = gain, Rev = .3, Ducked = false };
    public static Mallet Celesta(double gain = .2) => new() { Name = "celesta", Modes = new[] { (1.0, 1.0, .9), (2.0, .3, .3), (3.0, .1, .12), (4.1, .05, .05) }, Ring = true, Gain = gain, Rev = .35, Pan = .3 };
    public static Mallet Kalimba(double gain = .26) => new() { Name = "kalimba", Modes = new[] { (1.0, 1.0, .6), (5.95, .18, .06), (10.1, .05, .02) }, Gain = gain, Rev = .25, Pan = -.25 };
    public static EPiano Rhodes(double gain = .24) => new() { Name = "e-piano", Gain = gain, Rev = .25 };
    public static Organ SkankOrgan(double gain = .14) => new() { Name = "organ", Gain = gain, Rev = .15 };
    public static Flute PanFlute(double gain = .24, string name = "pan flute") => new() { Name = name, Breath = .22, Vib = .1, Chiff = .4, Gain = gain, Rev = .35, Dly = .15, Ducked = false };
    public static Flute Whistle(double gain = .2, string name = "whistle") => new() { Name = name, Breath = .08, Vib = .18, Chiff = .2, Gain = gain, Rev = .3, Dly = .12, Ducked = false };
}

sealed class Spec
{
    public string Name = "", Title = "", Key = "C"; public int[] Scale = Sc.Major;
    public double Bpm = 120, Swing = 0, Duck = .3, Rev = 3.0; public int Seed = 1;
    public string ProgA = "", ProgB = "";
    public Inst? Kick, Snare, Clap, Hat, OHat, Perc, Perc2, Bass, Stab, Pad, Arp, LeadA, LeadB, Harm, Crash, Riser, Fill;
    public string KickPat = "x...x...x...x...", SnarePat = "....x.......x...", ClapPat = "....x.......x...", HatPat = "x.x.x.x.x.x.x.x.", OHatPat = "..x...x...x...x.";
    public string PercPat = "................", Perc2Pat = "................", BassPat = "R.......R.......", StabPat = "x..x..x...x..x..", PadPat = "x---------------", ArpPat = "0.1.2.3.2.1.2.1.";
    public string? KickPatB, BassPatB, StabPatB, ArpPatB, PercPatB;
    public string BreakBassPat = "R---------------";
    public string MelA = "", MelB = ""; public int MelBaseA = 72, MelBaseB = 72, HarmShift = -2;
    public int StabCenter = 64, StabCount = 3, PadCenter = 60, PadCount = 4, ArpLow = 64, BassLow = 36, PercMidi = 60; public double StabStrum = 0;
    public Action<Song>? Extra;
}

static class Arranger
{
    // Intro 0-3, A 4-11, B 12-19, A' 20-27, break 28-31, B' 32-39, outro 40-43.
    public static Song Build(Spec p)
    {
        var s = new Song(44, p.Bpm, p.Scale, p.Seed) { Swing = p.Swing, DuckAmt = p.Duck, RevReturn = p.Rev };
        int[] gStart = { 4, 12, 20, 32, 40 }, gLen = { 8, 8, 8, 8, 4 };
        s.Prog(0, 12, p.ProgA); s.Prog(12, 8, p.ProgB); s.Prog(20, 12, p.ProgA); s.Prog(32, 8, p.ProgB); s.Prog(40, 4, p.ProgA);
        static bool IsB(int b) => b == 12 || b == 32;
        for (int g = 0; g < gStart.Length; g++)
        {
            int b = gStart[g], n = gLen[g]; bool isB = IsB(b), outro = b == 40;
            if (p.Kick != null)
            {
                string kp = isB ? p.KickPatB ?? p.KickPat : p.KickPat;
                if (outro) { s.Drums(p.Kick, b, 3, kp); s.Drums(p.Kick, b + 3, 1, kp.Split(' ')[0][..12] + "...."); }
                else s.Drums(p.Kick, b, n, kp);
            }
            if (p.Snare != null) s.Drums(p.Snare, b, n, p.SnarePat);
            if (p.Clap != null) s.Drums(p.Clap, b, n, p.ClapPat);
            if (p.Hat != null) s.Drums(p.Hat, b, n, p.HatPat);
            if (p.OHat != null && !(b == 12)) s.Drums(p.OHat, b, n, p.OHatPat);
            if (p.Perc != null && (isB || b == 20)) s.Drums(p.Perc, b, n, isB ? p.PercPatB ?? p.PercPat : p.PercPat, 1, p.PercMidi);
            if (p.Perc2 != null) s.Drums(p.Perc2, b, n, p.Perc2Pat);
            if (p.Bass != null) s.Bass(p.Bass, b, n, isB ? p.BassPatB ?? p.BassPat : p.BassPat, p.BassLow);
            if (p.Stab != null && (!isB || b == 32)) s.Chords(p.Stab, b, n, isB ? p.StabPatB ?? p.StabPat : p.StabPat, p.StabCenter, p.StabCount, 1, .9, p.StabStrum);
            if (p.Pad != null && isB) s.Chords(p.Pad, b, n, p.PadPat, p.PadCenter, p.PadCount, .9, .98);
            if (p.Arp != null && (isB || b == 20 || outro)) s.Arp(p.Arp, b, n, isB ? p.ArpPatB ?? p.ArpPat : p.ArpPat, p.ArpLow);
            if (p.Crash != null && !outro) s.Hit(p.Crash, b, 0, 16, 60, .9);
        }
        // Intro: pads and arpeggio, the beat joining for its second half.
        if (p.Pad != null) s.Chords(p.Pad, 0, 4, p.PadPat, p.PadCenter, p.PadCount, .8, .98);
        if (p.Arp != null) s.Arp(p.Arp, 0, 4, p.ArpPat, p.ArpLow, .8);
        if (p.Hat != null) s.Drums(p.Hat, 0, 4, p.HatPat, .7);
        if (p.Perc2 != null) s.Drums(p.Perc2, 0, 4, p.Perc2Pat, .8);
        if (p.Kick != null) s.Drums(p.Kick, 2, 2, p.KickPat, .85);
        if (p.Bass != null) s.Bass(p.Bass, 2, 2, p.BassPat, p.BassLow, .85);
        // Break: no kick; pads, a soft arpeggio and the hook's first phrase, then a build.
        if (p.Pad != null) s.Chords(p.Pad, 28, 4, p.PadPat, p.PadCenter, p.PadCount, 1, .98);
        if (p.Arp != null) s.Arp(p.Arp, 28, 4, p.ArpPat, p.ArpLow, .7);
        if (p.Bass != null) s.Bass(p.Bass, 28, 4, p.BreakBassPat, p.BassLow, .7, .97);
        if (p.Hat != null) s.Drums(p.Hat, 30, 2, p.HatPat, .6);
        if (p.LeadA != null) s.Melody(p.LeadA, 28, p.MelA, p.MelBaseA, vel: .75, maxTokens: 32);
        // Melodies.
        if (p.LeadA != null) { s.Melody(p.LeadA, 4, p.MelA, p.MelBaseA); s.Melody(p.LeadA, 20, p.MelA, p.MelBaseA); s.Melody(p.LeadA, 32, p.MelB, p.MelBaseB, vel: .45, transpose: -12); }
        if (p.Harm != null) s.Melody(p.Harm, 20, p.MelA, p.MelBaseA, vel: .6, shift: p.HarmShift);
        if (p.LeadB != null) { s.Melody(p.LeadB, 12, p.MelB, p.MelBaseB); s.Melody(p.LeadB, 32, p.MelB, p.MelBaseB); }
        // Transitions.
        if (p.Riser != null) { s.Hit(p.Riser, 2, 0, 32, 60, .8); s.Hit(p.Riser, 30, 0, 32, 60, 1); s.Hit(p.Riser, 19, 0, 16, 60, .6); }
        if (p.Fill != null)
        {
            s.Drums(p.Fill, 3, 1, "........o.o.xxxx", .8); s.Drums(p.Fill, 11, 1, "............xoxx");
            s.Drums(p.Fill, 19, 1, "........x.x.xxXX"); s.Drums(p.Fill, 27, 1, "............xxXX");
            s.Drums(p.Fill, 31, 1, "o.o.o.x.xxxxXXXX"); s.Drums(p.Fill, 39, 1, "............xoxX");
        }
        p.Extra?.Invoke(s);
        return s;
    }
}

static class Tracks
{
    public static IEnumerable<(string, string, Func<Song>)> Custom()
    {
        yield return ("level-kings-quest", "The King's Quest — orchestral-folk adventure in the castle theme's style, D minor, 96 BPM", KingsQuest);
    }

    // Modelled on what drives the castle theme (analysed 7 Oct 2026): a low drum on every beat, strummed and tambourine 8ths
    // that never stop, minor harmony with the raised leading tone, busy melodic lines with scale runs, and a warm, dark mix.
    // The melody and arrangement are original.
    // Intro 0-1, A 2-9 (flute hook), A' 10-17 (harmony and pizzicato), B 18-25 (horns), bridge 26-29, A'' 30-37 (full),
    // turnaround 38-39 back into the intro.
    static Song KingsQuest()
    {
        var s = new Song(40, 96, Sc.Minor, 1207) { DuckAmt = .06, RevReturn = 2.6, Room = .86, Damp = .45, MasterLowpass = 5200, DlyFb = .22 };
        const string progA = "Dm Bb C A Dm Bb Gm A";
        s.Prog(0, 2, "Dm A"); s.Prog(2, 8, progA); s.Prog(10, 8, progA); s.Prog(18, 8, "Gm C F Bb Gm A Dm A");
        s.Prog(26, 4, "Dm Bb Gm A"); s.Prog(30, 8, progA); s.Prog(38, 2, "Dm A");

        var frame = new Drum { Name = "frame drum", Kind = Drum.K.Taiko, Tune = 1.08, Decay = .45, Gain = .42, Rev = .22, Ducked = false };
        var kick = Pre.Kick(1.2, .8, .28);
        var timp = new Drum { Name = "timpani", Kind = Drum.K.Tom, Decay = 2.4, Gain = .45, Rev = .4, Ducked = false };
        var snare = Pre.Snare(.5, .95, "field snare"); var roll = Pre.Snare(.3, .95, "snare roll");
        var tamb = Pre.Tamb(.22); var crash = Pre.Crash(.11);
        var lute = new Pluck { Name = "lute", Bright = .4, T60 = .7, Release = .07, Gain = .36, Rev = .22, Pan = -.25 };
        var cellos = new Synth { Name = "string chug", Voices = 3, Detune = .14, Width = .5, Cut = 900, Res = .8, FEnv = 1.2, FD = .08, A = .008, D = .12, S = .35, R = .08, KeyTrack = .3, Gain = .4, Rev = .28 };
        var bass = Pre.UprightBass(.26);
        var strings = new Synth { Name = "string section", Voices = 6, Detune = .22, Width = .9, Cut = 1800, Res = .6, A = .3, D = .4, S = .9, R = .5, VibDepth = .08, VibDelay = .3, Gain = .2, Rev = .5 };
        var flute = new Flute { Name = "flute", Breath = .06, Vib = .12, Chiff = .25, A = .03, R = .08, Gain = .24, Rev = .38, Dly = .1, Ducked = false };
        var flute2 = new Flute { Name = "flute harmony", Breath = .1, Vib = .12, Chiff = .2, Gain = .2, Rev = .38, Pan = .3, Ducked = false };
        var horns = new Synth { Name = "horns", Voices = 3, Detune = .1, Width = .5, Cut = 520, FEnv = 1.6, FA = .08, FD = .4, FS = .55, A = .05, D = .3, S = .85, R = .18, VibDepth = .06, KeyTrack = .4, Gain = .34, Rev = .42, Ducked = false };
        var hornPad = new Synth { Name = "horn chords", Voices = 3, Detune = .1, Width = .6, Cut = 480, FEnv = 1.2, FA = .15, FD = .5, FS = .6, A = .12, D = .3, S = .85, R = .3, KeyTrack = .4, Gain = .2, Rev = .45 };
        var harp = Pre.Harp(.4); var pizz = Pre.Pizz(.35, "pizzicato");

        const string melA =
            "5, - - 5, 1 - - - 3 - - 3 5 - - - | 4 - 3 - 1 - 6, - 1 - - 3 4 - - - | 3 - - 3 2 - - - 7, - 1 - 2 - 3 - | #7, - - - 5, - - - #7, - 2 - 1 - #7, - |" +
            "5, - - 5, 1 - - - 3 - - 3 5 - - - | 4 - 3 - 4 - 6 - 3 - - - 4 - - - | 6 - - 6 5 - - - 4 - 3 - 2 - 6, - | #7, - 1 - 2 - #7, - 5, - - - - - - -";
        const string melB =
            "6 - - - 5 - 4 - 6 - - - 1' - - - | 7 - - - 6 - 5 - 7 - - - 2' - - - | 1' - - - 7 - 6 - 5 - - - 3 - - - | 3 - - - - - - - 6 - 5 - 4 - 3 - |" +
            "6 - - - 5 - 4 - 6 - - - 1' - - - | #7 - - - 5 - - - 2' - - - 1' - #7 - | 1' - - - 5 - - - 3 - 4 - 5 - 6 - | #7 - - - - - - - 5 - - - - - - -";
        const string melBridge =
            "1' - - - - - - - 5 - - - 3 - - - | 4 - - - - - - - 3 - - - 1 - - - | 6, - - - 1 - - - 4 - - - 6 - - - | #7, - - - 2 - - - 5, - - - - - - -";
        const string runUp = ". . . . . . . . 1 2 3 4 5 6 #7 1'", runDown = ". . . . . . . . 1' 7 6 5 4 3 2 1";

        // The groove never stops, as in the castle theme: frame drum, kick and bass on the beat; lute and violas chugging
        // the 8ths in the middle register; tambourine on every 8th.
        void Groove(int bar, int bars, bool full)
        {
            s.Drums(frame, bar, bars, "X...x...X...x... X...x...X...x... X...x...X...x... X...x...X...x.xx");
            s.Drums(kick, bar, bars, "x...x...x...x...", full ? 1 : .8);
            s.Drums(tamb, bar, bars, "x.x.x.x.x.x.x.x.", full ? 1 : .8);
            if (full) s.Drums(snare, bar, bars, "....x.......x...");
            s.Chords(lute, bar, bars, "o.x.o.x.o.x.o.x.", 64, 4, 1, .9, .012);
            s.Bass(cellos, bar, bars, "R.R.5.R.R.R.8.5.", 55);
            s.Bass(bass, bar, bars, "R.......5.......", 38);
        }
        Groove(0, 2, false); Groove(2, 8, false); Groove(10, 8, true); Groove(18, 8, true); Groove(30, 8, true); Groove(38, 2, false);
        // Bridge: half-time drums under harp arpeggios, building back with a timpani roll.
        s.Drums(frame, 26, 4, "X.......x.......");
        s.Drums(tamb, 26, 4, "x.x.x.x.x.x.x.x.", .6);
        s.Bass(cellos, 26, 4, "R.R.5.R.R.R.8.5.", 55, .8);
        s.Bass(bass, 26, 4, "R-------5-------", 38);
        s.Arp(harp, 26, 4, "0123432101234321", 62, .8);
        s.Drums(timp, 29, 1, "x.x.x.x.xxxxXXXX", .9, 38);
        s.Drums(roll, 29, 1, "........o.o.xxxx");

        // Melodies.
        s.Melody(flute, 2, melA, 74, grid: 1);
        s.Melody(flute, 10, melA, 74, grid: 1); s.Melody(flute2, 10, melA, 74, grid: 1, shift: -2); s.Melody(pizz, 10, melA, 74, grid: 1, vel: .7, transpose: -12);
        s.Melody(horns, 18, melB, 62, grid: 1); s.Melody(strings, 18, melB, 74, grid: 1, vel: .5);
        s.Melody(flute, 22, melB.Substring(melB.IndexOf("6 - - - 5", 10)), 74, grid: 1, vel: .55);
        s.Melody(flute, 26, melBridge, 74, grid: 1, vel: .8);
        s.Melody(flute, 30, melA, 74, grid: 1); s.Melody(flute2, 30, melA, 74, grid: 1, shift: -2); s.Melody(pizz, 30, melA, 74, grid: 1, vel: .7, transpose: -12);
        s.Chords(hornPad, 30, 8, "x-------x-------", 60, 3, .9, .98);
        // Sustained strings under A' and B.
        s.Chords(strings, 2, 8, "x---------------", 62, 4, .5, .98);
        s.Chords(strings, 10, 8, "x---------------", 62, 4, .7, .98);
        s.Chords(strings, 26, 4, "x---------------", 62, 4, .9, .98);
        s.Chords(strings, 30, 8, "x---------------", 62, 4, .8, .98);
        // Harp runs answer each phrase.
        foreach (var b in new[] { 1, 5, 9, 13, 17, 33, 37, 39 }) s.Melody(harp, b, runUp, 62, grid: 1, vel: .85);
        foreach (var b in new[] { 21, 25 }) s.Melody(harp, b, runDown, 74, grid: 1, vel: .85);
        // Accents into each section.
        foreach (var b in new[] { 2, 10, 18, 30 }) { s.Hit(crash, b, 0, 16, 60, b == 2 ? .6 : .9); s.Hit(timp, b, 0, 8, 38, 1); }
        s.Drums(roll, 17, 1, "........o.o.xxXX"); s.Drums(roll, 37, 1, "............xxXX"); s.Drums(roll, 39, 1, "............oxxx", .8);
        return s;
    }

    public static IEnumerable<Spec> All()
    {
        yield return new Spec
        {
            Name = "level-01-crown-jewels", Title = "Crown Jewels — bright pop-house, D major, 122 BPM", Key = "D", Bpm = 122, Seed = 101, Duck = .38,
            ProgA = "D A Bm G", ProgB = "Bm G D A",
            Kick = Pre.Kick(), Clap = Pre.Clap(), Hat = Pre.HatC(), OHat = Pre.HatO(), Perc = Pre.Tamb(), Perc2 = Pre.Shaker(0.13),
            HatPat = "x.x.x.x.x.x.x.xo", PercPat = "....x.......x...", Perc2Pat = "oxoxoxoxoxoxoxox",
            Bass = Pre.PluckBass(), BassPat = "..R...R...R.5.R.", BassPatB = "..R...R...R...8.",
            Stab = Pre.SuperStab(), StabCenter = 66, Pad = Pre.SuperPad(), PadCenter = 62,
            Arp = new Func<Mallet>(() => { var m = Pre.Marimba(.15); m.Pan = -.3; return m; })(), ArpPat = "0.2.1.2.3.2.1.2.", ArpLow = 74,
            LeadA = Pre.SawPluck(), LeadB = Pre.SquareLead(), Harm = new Func<Mallet>(() => { var m = Pre.Marimba(.22, "marimba harmony"); m.Pan = .3; return m; })(),
            Crash = Pre.Crash(), Riser = Pre.Riser(), Fill = Pre.Snare(0.36, 1, "fill"),
            MelBaseA = 74, MelBaseB = 62,
            MelA = "5 . 3 5 . 6 5 3 | 2 . 1 2 . 3 2 - | 3 . 2 3 . 5 3 2 | 1 - 2 - 3 - 5, - | 5 . 3 5 . 6 5 3 | 2 . 1 2 . 3 2 5 | 6 . 5 6 . 1' 7 6 | 5 - - - 3 - 2 -",
            MelB = "6 - - 5 6 - 1' - | 1' - - 7 6 - 5 - | 3 - - 2 3 - 5 - | 2 - - - - - . . | 6 - - 5 6 - 1' - | 2' - - 1' 7 - 6 - | 5 - 6 - 1' - 3' - | 2' - - - 1' - 7 -",
        };
        yield return new Spec
        {
            Name = "level-02-garden-gallop", Title = "Garden Gallop — folk-pop stomp, G major, 128 BPM", Key = "G", Bpm = 128, Seed = 202, Duck = .22,
            ProgA = "G C Em D", ProgB = "Em C G D",
            Kick = Pre.Kick(1, .9), Snare = Pre.Snare(0.48), Clap = Pre.Clap(0.54), Perc = Pre.Tamb(0.20), Perc2 = Pre.Shaker(0.15),
            PercPat = "x.x.x.x.x.x.x.x.", Perc2Pat = "oxoxoxoxoxoxoxox",
            Bass = Pre.UprightBass(), BassPat = "R...5...R...5...", BassPatB = "R.....R.5.......", BassLow = 38,
            Stab = Pre.Guitar(), StabPat = "x..xx.x.x..xx.x.", StabCenter = 60, StabCount = 4, StabStrum = .011,
            Pad = Pre.Accordion(.07, "accordion pad"), PadPat = "x-------x-------", PadCenter = 64, PadCount = 3,
            Arp = new Func<Pluck>(() => { var a = Pre.Pizz(.26); a.Pan = -.25; return a; })(), ArpPat = "0.1.2.1.0.1.2.1.", ArpPatB = "0.2.1.3.0.2.1.3.", ArpLow = 67,
            LeadA = Pre.Whistle(), LeadB = new Func<Pluck>(() => { var a = Pre.Pizz(.36, "pizzicato lead"); a.Pan = .1; return a; })(), Harm = Pre.Glock(.14),
            Crash = Pre.Crash(), Riser = Pre.Riser(.08), Fill = Pre.Snare(0.34, 1, "fill"),
            MelBaseA = 79, MelBaseB = 67,
            MelA = "5 . 3 . 1 2 3 5 | 6 . 4 . 6 5 4 3 | 3 . 5 . 6 5 3 2 | 2 - 7, - 5, - . . | 5 . 3 . 1 2 3 5 | 6 . 4 . 6 5 4 6 | 5 . 6 . 5 3 2 3 | 2 - 3 - 1 - - -",
            MelB = "3 . 2 3 5 . 3 . | 4 . 3 4 6 . 4 . | 5 . 4 5 1' . 5 . | 2' - 1' - 7 - 6 - | 3 . 2 3 5 . 3 . | 4 . 3 4 6 . 1' . | 1' 7 6 5 3 . 5 . | 2 - - - 7, - - -",
        };
        yield return new Spec
        {
            Name = "level-03-palace-funk", Title = "Palace Funk — swung funk groove, E minor, 108 BPM", Key = "E", Scale = Sc.Minor, Bpm = 108, Swing = .1, Seed = 303, Duck = .2,
            ProgA = "Em7 A7 Em7 A7", ProgB = "Cmaj7 Bm7 Am7 D",
            Kick = Pre.Kick(1, 1.1), Snare = Pre.Snare(0.60), Hat = Pre.HatC(0.32), OHat = Pre.HatO(0.14), Perc = Pre.Tamb(0.15),
            KickPat = "x.x...x...x..x..", SnarePat = "....x..o.o..x..o", HatPat = "xoxoxoxoxoxoxoxo", OHatPat = "..............x.", PercPat = "....x.......x...",
            Bass = new Func<Synth>(() => { var b = Pre.PluckBass(.48); b.FEnv = 3; b.S = .4; return b; })(), BassPat = "R..R8..RR..8.R5.", BassPatB = "R.....RR..8.R.5.",
            Stab = Pre.Rhodes(.19), StabPat = "..x..x.x...x..x.", StabCenter = 62, StabCount = 4,
            Pad = Pre.WarmPad(.1), PadCenter = 60,
            Arp = Pre.Clav(), ArpPat = "0.10.1.20.1.0.2.", ArpLow = 64,
            LeadA = Pre.SawLead(.27), LeadB = Pre.Brass(.22), Harm = new Func<Synth>(() => { var b = Pre.Brass(.13, "brass harmony"); b.Pan = -.3; return b; })(),
            Crash = Pre.Crash(.1), Riser = Pre.Riser(.07), Fill = Pre.Snare(0.39, 1, "fill"),
            MelBaseA = 76, MelBaseB = 64,
            MelA = "1 . 3 . 4 #4 5 . | . . #6 5 3 . 1 . | 7, . 1 3 . 5 7 5 | #6 - 5 - 3 - . . | 1 . 3 . 4 #4 5 . | . . #6 5 3 . 1 3 | 5 . 7 . 1' . 7 5 | #6 - - - 5 - 3 -",
            MelB = "5 - - 3 5 - 7 - | 7 - 5 - 4 - 2 - | 3 - - 1 3 - 5 - | 4 - 2 - 4 - 7 - | 5 - - 3 5 - 1' - | 2' - 1' - 7 - 5 - | 1' - 7 - 6 - 5 - | 4 - - - 2 - - -",
            Extra = s => { var hit = Pre.BrassStab(.2); foreach (var b in new[] { 7, 11, 23, 27 }) s.Chords(hit, b, 1, "..........X..X..", 67, 3); },
        };
        yield return new Spec
        {
            Name = "level-04-knights-dash", Title = "Knight's Dash — driving electro-orchestral, A minor, 134 BPM", Key = "A", Scale = Sc.Minor, Bpm = 134, Seed = 404, Duck = .32,
            ProgA = "Am F C G", ProgB = "Dm F G E",
            Kick = Pre.Kick(), Snare = Pre.Snare(0.63), Hat = Pre.HatC(0.32), OHat = Pre.HatO(0.14), Perc = Pre.Tom(.32),
            KickPatB = "x...x...x...x.x.", PercPat = "..........x...x.", PercMidi = 45,
            Bass = Pre.PluckBass(.36), BassPat = "R.8.R.8.R.8.R.8.", BassPatB = "R.R.8.R.R.R.8.R.",
            Stab = Pre.BrassStab(.2), StabPat = "x.....x.....x...", StabCenter = 64,
            Pad = Pre.SuperPad(.13), PadCenter = 60,
            Arp = new Func<Synth>(() => { var a = Pre.Strings(.15); a.Pan = -.2; return a; })(), ArpPat = "0121012101210121", ArpPatB = "0213021302130213", ArpLow = 57,
            LeadA = Pre.SawLead(.26), LeadB = Pre.Brass(.24), Harm = new Func<Synth>(() => { var b = Pre.Brass(.13, "brass harmony"); b.Pan = .35; return b; })(),
            Crash = Pre.Crash(), Riser = Pre.Riser(), Fill = Pre.Snare(0.39, 1, "fill"),
            MelBaseA = 69, MelBaseB = 57,
            MelA = "1 . 1 2 3 . 5 . | 6 . 5 . 3 . 1 . | 5 . 5 6 5 . 3 . | 2 - - - 7, - 5, - | 1 . 1 2 3 . 5 . | 6 . 5 . 6 . 1' . | 1' . 7 . 5 . 3 . | 2 - 3 - 2 - 7, -",
            MelB = "4 - - - 6 - 4 - | 3 - - - 1 - 3 - | 2 - - - 7, - 2 - | #7, - - - 2 - 5 - | 4 - 5 - 6 - 1' - | 1' - - 7 6 - 5 - | 7 - - 6 5 - 4 - | 5 - - - #7, - - -",
            Extra = s => { var t = Pre.Taiko(.45); s.Drums(t, 12, 8, "X.......X......."); s.Drums(t, 32, 8, "X.......X.....x."); },
        };
        yield return new Spec
        {
            Name = "level-05-sugar-tower", Title = "Sugar Tower — bouncy chiptune pop, F major, 118 BPM", Key = "F", Bpm = 118, Seed = 505, Duck = .22,
            ProgA = "F Dm Bb C", ProgB = "Bb C Am Dm",
            Kick = Pre.Kick(1.05, .9), Snare = Pre.Snare(0.45), Clap = Pre.Clap(0.48), Hat = Pre.HatC(0.32),
            KickPat = "x..x..x...x.....", ClapPat = "....x.......x..x",
            Bass = Pre.SquareBass(), BassPat = "R.R.R.8.R.R.5.8.", BassPatB = "R...R.8.R...5.8.",
            Stab = Pre.ChipStab(), StabPat = "..x...x...x...x.", StabCenter = 65,
            Pad = Pre.WarmPad(.1), PadCenter = 62,
            Arp = Pre.ChipArp(.08), ArpPat = "0123012301230123", ArpLow = 77,
            LeadA = Pre.ChipLead(), LeadB = Pre.Glock(.24, "glockenspiel lead"), Harm = new Func<Synth>(() => { var h = Pre.TriLead(.14); h.Pan = -.3; return h; })(),
            Crash = Pre.Crash(.12), Riser = Pre.Riser(.08), Fill = Pre.Snare(0.34, 1.1, "fill"),
            MelBaseA = 77, MelBaseB = 77,
            MelA = "1 . 3 . 5 . 3 5 | 6 . 5 . 3 . 2 . | 4 . 6 . 1' . 6 4 | 5 - - - - - 7, . | 1 . 3 . 5 . 3 5 | 6 . 1' . 6 . 5 . | 4 . 3 . 4 . 6 . | 5 - 4 - 3 - 2 -",
            MelB = "4 4 6 6 1' 1' 6 . | 5 5 7 7 2' 2' 7 . | 3 3 5 5 7 7 5 . | 6 - - - 1' - - - | 4 4 6 6 1' 1' 6 . | 5 5 7 7 2' 2' 3' . | 3' - 2' - 1' - 7 - | 6 - - - 5 - - -",
        };
        yield return new Spec
        {
            Name = "level-06-moonlit-ballroom", Title = "Moonlit Ballroom — laid-back chillhop, Bb major, 88 BPM", Key = "Bb", Bpm = 88, Swing = .22, Seed = 606, Duck = .15, Rev = 1.5,
            ProgA = "Bbmaj7 Gm7 Ebmaj7 F7", ProgB = "Ebmaj7 Dm7 Cm7 F7",
            Kick = Pre.Kick(.95, 1.2, .8), Snare = new Func<Drum>(() => { var d = Pre.Snare(.45, .9); d.Rev = .25; return d; })(), Hat = Pre.HatC(0.18), Perc = Pre.Rim(.16), Perc2 = Pre.Shaker(0.10),
            KickPat = "x......x..x.....", HatPat = "x.xxx.x.x.xxx.x.", PercPat = "...x......x.....", Perc2Pat = "x.x.x.x.x.x.x.x.",
            Bass = Pre.SubBass(), BassPat = "R-----.R--.5----", BassPatB = "R-----.R--.3----",
            Stab = Pre.Rhodes(.18), StabPat = "x-----.x--...x--", StabCenter = 63, StabCount = 4,
            Pad = Pre.WarmPad(.08), PadCenter = 60,
            Arp = Pre.Celesta(.13), ArpPat = "....0.2...1.3...", ArpLow = 74,
            LeadA = Pre.Vibes(.3), LeadB = Pre.PanFlute(.2, "flute"), Harm = new Func<Mallet>(() => { var v = Pre.Vibes(.14, "vibes harmony"); v.Pan = -.3; return v; })(),
            Crash = null, Riser = Pre.Riser(.05), Fill = Pre.Snare(0.23, .9, "fill"),
            MelBaseA = 70, MelBaseB = 70,
            MelA = "3 . . 5 . 7 6 . | 5 - - - . . 3 4 | 5 . . 3 . 2 1 . | 2 - - - - - . . | 3 . . 5 . 7 6 . | 5 - - - . 6 7 1' | 2' . 1' 7 . 6 5 . | 6 - 5 - 4 - 2 -",
            MelB = "5 - - - 3 4 5 . | 3 - - - 5 . 7 . | 6 - - - 4 . 2 . | 7, - - - 2 - 4 - | 5 - - - 3 4 5 6 | 7 - - - 6 5 3 . | 4 - 6 - 1' - 2' - | 1' - - - 7 - - -",
        };
        yield return new Spec
        {
            Name = "level-07-market-day", Title = "Market Day — ska bounce with accordion, C major, 140 BPM", Key = "C", Bpm = 140, Seed = 707, Duck = .18,
            ProgA = "C Am F G", ProgB = "Dm G C Am",
            Kick = Pre.Kick(1, .9), Snare = Pre.Snare(0.54), Hat = Pre.HatC(0.22), Perc = Pre.Clave(), Perc2 = Pre.Shaker(0.12),
            KickPat = "x.......x.....x.", PercPat = "x..x..x...x.x...", Perc2Pat = "oxoxoxoxoxoxoxox",
            Bass = Pre.PluckBass(.45), BassPat = "R..R5...R..R5.3.", BassPatB = "R...3...5...3...",
            Stab = Pre.SkankOrgan(), StabPat = "..x...x...x...x.", StabCenter = 67,
            Pad = Pre.Accordion(.07, "accordion pad"), PadCenter = 64, PadCount = 3,
            Arp = new Func<Mallet>(() => { var m = Pre.Marimba(.15); m.Pan = -.3; return m; })(), ArpPat = "0.1.2.1.0.1.2.1.", ArpLow = 72,
            LeadA = Pre.Accordion(.2), LeadB = Pre.Brass(.22), Harm = new Func<Synth>(() => { var a = Pre.Accordion(.12, "accordion harmony"); a.Pan = .3; return a; })(),
            Crash = Pre.Crash(), Riser = Pre.Riser(.08), Fill = Pre.Snare(0.36, 1, "fill"),
            MelBaseA = 72, MelBaseB = 60,
            MelA = "5 . 5 3 . 1 3 5 | 6 . 5 3 . 1 . . | 4 . 4 6 . 1' 6 4 | 5 - - - 2 - 7, - | 5 . 5 3 . 1 3 5 | 6 . 1' 6 . 5 . 3 | 4 . 6 . 1' . 3' . | 2' - 1' - 7 - 5 -",
            MelB = "2 . 4 . 6 . 4 . | 5 . 7 . 2' . 7 . | 1' . 7 . 5 . 3 . | 6 - - - . . 5 6 | 2 . 4 . 6 . 1' . | 2' - 1' - 7 - 5 - | 3' . 2' . 1' . 5 . | 6 - - - 7 - - -",
            Extra = s => s.Melody(Pre.Glock(.1, "glockenspiel double"), 32, "2 . 4 . 6 . 4 . | 5 . 7 . 2' . 7 . | 1' . 7 . 5 . 3 . | 6 - - - . . 5 6 | 2 . 4 . 6 . 1' . | 2' - 1' - 7 - 5 - | 3' . 2' . 1' . 5 . | 6 - - - 7 - - -", 84, vel: .6),
        };
        yield return new Spec
        {
            Name = "level-08-sky-kites", Title = "Sky Kites — breezy tropical house, E major, 112 BPM", Key = "E", Bpm = 112, Seed = 808, Duck = .35,
            ProgA = "E B C#m A", ProgB = "C#m A E B",
            Kick = Pre.Kick(1.05, .8, .8), Clap = Pre.Snap(.26), Hat = Pre.Shaker(0.20, .2), OHat = Pre.HatO(0.14), Perc = Pre.Rim(.16),
            HatPat = "xoxoxoxoxoxoxoxo", PercPat = "..x..x....x..x..",
            Bass = Pre.SubBass(.6), BassPat = "R..R..R...R..5..", BassPatB = "R.....R.R.....5.",
            Stab = Pre.Marimba(.18, "marimba chords"), StabPat = "..x..x....x..x..", StabCenter = 68,
            Pad = Pre.WarmPad(.1), PadCenter = 64,
            Arp = Pre.Kalimba(.14), ArpPat = "0.1.2.0.1.2.0.1.", ArpLow = 76,
            LeadA = Pre.PanFlute(.26), LeadB = Pre.Marimba(.32, "marimba lead"), Harm = new Func<Flute>(() => { var f = Pre.PanFlute(.13, "flute harmony"); f.Pan = .3; return f; })(),
            Crash = Pre.Crash(.1), Riser = Pre.Riser(.08), Fill = Pre.Rim(.2),
            MelBaseA = 76, MelBaseB = 76,
            MelA = "5 . . 3 . 5 6 5 | 2 - - - . . 1 2 | 3 . . 1 . 3 5 3 | 1 - - - . . . . | 5 . . 3 . 5 6 5 | 2 - - - . . 5 6 | 1' . . 6 . 5 3 . | 3 - 2 - 1 - - -",
            MelB = "6 6 . 6 5 . 3 . | 1' 1' . 1' 6 . 3 . | 5 5 . 5 3 . 1 . | 2 - - - 7, - - - | 6 6 . 6 5 . 3 . | 1' 1' . 1' 2' . 3' . | 3' - 2' - 1' - 6 - | 5 - - - - - . .",
        };
        yield return new Spec
        {
            Name = "level-09-dragon-drums", Title = "Dragon Drums — epic taiko and synths, D minor, 126 BPM", Key = "D", Scale = Sc.Minor, Bpm = 126, Seed = 909, Duck = .3,
            ProgA = "Dm Bb F C", ProgB = "Gm Bb C A",
            Kick = Pre.Kick(1, 1, .85), Snare = Pre.Snare(0.63), Clap = Pre.Clap(0.37), Hat = Pre.HatC(0.3), Perc = Pre.Taiko(.5),
            PercPat = "x..x..x.x...x.x.",
            Bass = Pre.ReeseBass(), BassPat = "R.RR.RR.RR.RR.R.",
            Stab = Pre.BrassStab(.2), StabPat = "x.....x.....x...", StabCenter = 62,
            Pad = Pre.SuperPad(.15), PadCenter = 62,
            Arp = Pre.Harp(.22), ArpPat = "0123432101234321", ArpLow = 62,
            LeadA = Pre.SawLead(.2), LeadB = Pre.Brass(.24), Harm = new Func<Synth>(() => { var b = Pre.Brass(.12, "brass harmony"); b.Pan = .3; return b; })(),
            Crash = Pre.Crash(), Riser = Pre.Riser(), Fill = Pre.Snare(0.39, 1, "fill"),
            MelBaseA = 74, MelBaseB = 62,
            MelA = "1 - - 5, 1 - 3 - | 3 - 1 - 6, - 1 - | 5 - - 3 5 - 7 - | 4 - - - 2 - 7, - | 1 - - 5, 1 - 3 - | 3 - 1 - 6, - 1 - | 5 - - 7 1' - 3' - | 2' - 1' - 7 - 5 -",
            MelB = "4 - - - 6 - 1' - | 3' - - - 1' - 6 - | 2' - - - 1' - 7 - | #7 - - - 5 - 2 - | 4 - 5 - 6 - 1' - | 3' - 2' - 1' - 6 - | 7 - 1' - 2' - 3' - | 5 - - - #7 - - -",
            Extra = s => { var t = Pre.Taiko(.4); s.Drums(t, 4, 8, "X..x..x.x...x.x."); s.Drums(t, 28, 4, "X.......x.......", .8); },
        };
        yield return new Spec
        {
            Name = "level-10-royal-parade", Title = "Royal Parade — marching-band pop, Eb major, 120 BPM", Key = "Eb", Bpm = 120, Seed = 1010, Duck = .15,
            ProgA = "Eb Bb Cm Ab", ProgB = "Ab Eb Fm Bb",
            Kick = Pre.Kick(.9, 1.3, .8), Snare = Pre.Snare(0.36), Clap = Pre.Clap(0.41), Perc = Pre.Snare(.26, 1.15, "drumline"), Perc2 = Pre.Tamb(0.14),
            KickPat = "x.......x.......", SnarePat = "..o.X.o...o.X.oo", PercPat = "x.oox.oox.oox.oo", Perc2Pat = "..x...x...x...x.",
            Bass = Pre.Tuba(), BassPat = "R...5...R...5...", BassPatB = "R...3...5...3...",
            Stab = Pre.BrassStab(.16), StabPat = "..x...x...x...x.", StabCenter = 63,
            Pad = Pre.Brass(.09, "brass pad"), PadCenter = 60,
            Arp = new Func<Mallet>(() => { var g = Pre.Glock(.08); g.Pan = .3; return g; })(), ArpPat = "0...1...2...1...", ArpLow = 75,
            LeadA = Pre.Brass(.24, "trumpets"), LeadB = Pre.Whistle(.2, "piccolo"), Harm = new Func<Synth>(() => { var b = Pre.Brass(.14, "brass harmony"); b.Pan = -.3; return b; })(),
            Crash = Pre.Crash(.16), Riser = Pre.Riser(.07), Fill = Pre.Snare(0.39, 1.1, "fill"),
            MelBaseA = 63, MelBaseB = 75,
            MelA = "1 . 1 3 5 - 3 . | 5 . 4 . 2 . 7, . | 3 . 3 . 5 . 1' . | 6 - - - 5 - 4 - | 1 . 1 3 5 - 3 . | 5 . 6 . 5 . 2' . | 1' . 7 . 6 . 5 . | 4 - 6 - 1' - - -",
            MelB = "6 6 6 . 4 . 6 . | 5 5 5 . 3 . 5 . | 4 4 4 . 2 . 4 . | 2 - - - 5, - - - | 6 6 6 . 1' . 6 . | 5 5 5 . 1' . 3' . | 2' - 1' - 6 - 4 - | 2 - 5, - 7, - 2 -",
            Extra = s => { var g = Pre.Glock(.1, "glockenspiel double"); foreach (var b in new[] { 4, 20 }) s.Melody(g, b, "1 . 1 3 5 - 3 . | 5 . 4 . 2 . 7, . | 3 . 3 . 5 . 1' . | 6 - - - 5 - 4 - | 1 . 1 3 5 - 3 . | 5 . 6 . 5 . 2' . | 1' . 7 . 6 . 5 . | 4 - 6 - 1' - - -", 75, vel: .55); },
        };
    }
}
