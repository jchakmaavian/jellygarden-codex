// Splits the propeller artwork into the two layers the animation needs and poses them.
// Nothing is redrawn: the rotor spins as the original painted pixels under an affine that is
// the identity at rest, and the body keeps its own pixels with the hidden sphere cap filled in.
using System;

public static class PropRig
{
    public static Img Art, Body, Rotor, Hub;
    public static readonly Img[] Blade = new Img[3];
    public static readonly double[] Axes = { 35.8, 155.8, 275.8 };
    public static double HubX, HubY, SphereX, SphereY, SphereR, K, Roll, Reach;
    public const int Canvas = 512;

    public static string Setup(string artPath)
    {
        Art = Img.Load(artPath);
        PropGeom.Measure(artPath);
        PropTilt.Measure(artPath);
        HubX = PropGeom.HubCx; HubY = PropGeom.HubCy;
        SphereX = PropGeom.SphereCx; SphereY = PropGeom.SphereCy; SphereR = PropGeom.SphereR;
        K = PropTilt.K; Roll = -4.0; Reach = PropGeom.BladeReach;

        int w = Art.W, h = Art.H;
        Body = new Img(w, h); Rotor = new Img(w, h); Hub = new Img(w, h);
        for (int k = 0; k < 3; k++) Blade[k] = new Img(w, h);
        double[] axes = Axes;                               // rotor-space blade azimuths
        double hubRadius = 140;                             // gold collar reach in rotor space

        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int i = y * w + x;
                if (Art.A[i] <= 0) continue;
                float a0 = Art.A[i];
                var cls = PropAnalyze.Classify(Art.R[i] / a0, Art.G[i] / a0, Art.B[i] / a0, a0);
                double rx, ry; ToRotor(x + .5, y + .5, out rx, out ry);
                double rr = Math.Sqrt(rx * rx + ry * ry);
                double rdeg = Math.Atan2(ry, rx) * 180 / Math.PI; if (rdeg < 0) rdeg += 360;
                double sdx = x + .5 - SphereX, sdy = y + .5 - SphereY;
                bool onSphere = sdx * sdx + sdy * sdy <= (SphereR - 2) * (SphereR - 2);
                bool hub = rr <= hubRadius;
                int blade = -1;
                if (!hub && rr <= Reach * 1.08)
                    for (int k = 0; k < axes.Length; k++)
                    {
                        double d = Math.Abs(((rdeg - axes[k] + 540) % 360) - 180);
                        // A blade is widest at the root and tapers, so the sector narrows with radius.
                        double halfWidth = 46 - 14 * Math.Min(1, rr / (Reach * 1.2));
                        if (d > halfWidth) continue;
                        // Inside the sphere only the down blade exists; the gold band that wraps the
                        // body also reads as rotor-coloured and has to stay with the body.
                        if (onSphere && Math.Abs(((axes[k] - 275.8 + 540) % 360) - 180) > 1) continue;
                        blade = k; break;
                    }
                // The rotor is gold, red and the pink dome. Blue always belongs to the sphere, which
                // keeps the body whole where the down blade and the collar cross it.
                if (cls == PropAnalyze.Part.Blue) { hub = false; blade = -1; }
                var dst = hub || blade >= 0 ? Rotor : Body;
                dst.R[i] = Art.R[i]; dst.G[i] = Art.G[i]; dst.B[i] = Art.B[i]; dst.A[i] = Art.A[i];
                var part = hub ? Hub : blade >= 0 ? Blade[blade] : null;
                if (part != null) { part.R[i] = Art.R[i]; part.G[i] = Art.G[i]; part.B[i] = Art.B[i]; part.A[i] = Art.A[i]; }
            }

        FillSphere();
        return "rotor/body split done";
    }

    /// <summary>Screen pixel to rotor-plane coordinates about the hub (identity-safe, unit px).</summary>
    public static void ToRotor(double x, double y, out double rx, out double ry)
    {
        double dx = x - HubX, dy = HubY - y;                 // y up
        double c = Math.Cos(-Roll * Math.PI / 180), s = Math.Sin(-Roll * Math.PI / 180);
        double ux = dx * c - dy * s, uy = dx * s + dy * c;   // undo roll
        rx = ux; ry = uy / K;                                // undo foreshortening
    }

    /// <summary>
    /// Spins the rotor layer by <paramref name="deg"/> in the rotor's own tilted plane: undo the
    /// roll, undo the foreshortening, turn, put both back. At 0 it is the identity, so the rest
    /// pose is the untouched artwork.
    /// </summary>
    public static Affine Spin(double deg)
    {
        var unroll = Affine.Pose(HubX, HubY, 1, 1, -Roll, HubX, HubY);
        var flat = Affine.Pose(HubX, HubY, 1, 1 / K, 0, HubX, HubY);
        var turn = Affine.Pose(HubX, HubY, 1, 1, deg, HubX, HubY);
        var squash = Affine.Pose(HubX, HubY, 1, K, 0, HubX, HubY);
        var reroll = Affine.Pose(HubX, HubY, 1, 1, Roll, HubX, HubY);
        return unroll.Then(flat).Then(turn).Then(squash).Then(reroll);
    }

    /// <summary>
    /// Draws the booster: blades on the rotor's far side go behind the body, the near side in
    /// front, and the collar always sits on top. <paramref name="blur"/> samples spread the blades
    /// over the angle swept in one frame, so fast spin smears the real painted blades.
    /// </summary>
    public static void Compose(Img dst, double spinDeg, double sweepDeg, Affine place, int blur, int ss, float opacity = 1)
    {
        blur = Math.Max(1, blur);
        float share = opacity / blur;
        for (int pass = 0; pass < 2; pass++)
        {
            if (pass == 1) Fx.DrawLayer(dst, Body, place, opacity, ss);
            for (int k = 0; k < 3; k++)
                for (int s = 0; s < blur; s++)
                {
                    double deg = spinDeg + sweepDeg * (blur == 1 ? 0 : (s / (double)(blur - 1) - .5));
                    double psi = (Axes[k] + deg) * Math.PI / 180;
                    // sin > 0 points away from the camera on this tilted rotor: that half is behind.
                    bool behind = Math.Sin(psi) > 0;
                    if (behind != (pass == 0)) continue;
                    Fx.DrawLayer(dst, Blade[k], Spin(deg).Then(place), share, ss);
                }
        }
        for (int s = 0; s < blur; s++)
        {
            double deg = spinDeg + sweepDeg * (blur == 1 ? 0 : (s / (double)(blur - 1) - .5));
            Fx.DrawLayer(dst, Hub, Spin(deg).Then(place), share, ss);
        }
    }

    /// <summary>
    /// Places the whole booster: uniform scale with optional squash/stretch and tilt about the
    /// sphere's bottom contact point, then a translation. Identity when left at defaults.
    /// </summary>
    public static Affine Place(double scale, double squash, double tiltDeg, double tx, double ty)
    {
        double px = SphereX, py = SphereY + SphereR;        // the cell contact point
        double s = scale * ContentScale;
        double sx = s * (1 + squash), sy = s / (1 + squash);
        return Affine.Pose(px, py, sx, sy, tiltDeg, px + tx, py + ty);
    }

    /// <summary>
    /// Every frame draws the art at this fraction of the canvas. The headroom is what keeps the
    /// reveal overshoot, the lifted flight pose and the star bursts clear of the canvas edge.
    /// Unity draws the frames 1 / ContentScale larger to match the board sprite.
    /// </summary>
    public const double ContentScale = .72;

    static Img rest;
    /// <summary>
    /// The seated pose, rendered once. Every resting frame in every sequence copies it, so the
    /// Reveal's last frame, Idle 001 and Activation 001 are byte-identical.
    /// </summary>
    public static Img Rest
    {
        get
        {
            if (rest == null)
            {
                rest = new Img(Art.W, Art.H);
                Compose(rest, 0, 0, Place(1, 0, 0, 0, 0), 1, 3);
                Clean(rest);
            }
            return rest;
        }
    }

    /// <summary>
    /// Drops texels the eye cannot see. Soft glows leave a huge skirt of alpha 1/255 that only
    /// bloats the lossless frames and can bleed through mip maps.
    /// </summary>
    public static void Clean(Img img)
    {
        for (int i = 0; i < img.A.Length; i++)
            if (img.A[i] < 2f / 255f) { img.A[i] = 0; img.R[i] = 0; img.G[i] = 0; img.B[i] = 0; }
    }

    /// <summary>
    /// The rotor hides the top of the sphere. Rebuild that cap from the body's own pixels by
    /// reflecting each empty point across the sphere centre, so the fill keeps the painted
    /// shading and never invents a new design.
    /// </summary>
    static void FillSphere()
    {
        int w = Body.W, h = Body.H;
        // Only the sphere's own blue is a valid source, so the fill can never smear the red blade
        // tip or the gold collar across the body.
        var blue = new Img(w, h);
        for (int i = 0; i < w * h; i++)
        {
            float a = Body.A[i]; if (a <= .6f) continue;
            if (PropAnalyze.Classify(Body.R[i] / a, Body.G[i] / a, Body.B[i] / a, a) != PropAnalyze.Part.Blue) continue;
            blue.R[i] = Body.R[i]; blue.G[i] = Body.G[i]; blue.B[i] = Body.B[i]; blue.A[i] = a;
        }
        // The body is a lit sphere, so its blue is a function of how the surface faces the key
        // light. Fitting that ramp from the painted pixels rebuilds the hidden cap smoothly,
        // using only colours the artwork already contains.
        const int Bins = 48;
        var sumR = new double[Bins]; var sumG = new double[Bins]; var sumB = new double[Bins]; var hits = new double[Bins];
        double lx = -.46, ly = -.66, lz = .60;               // key light, up and to the left
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int i = y * w + x;
                if (blue.A[i] <= .85f) continue;
                int bin = LightBin(x + .5, y + .5, lx, ly, lz, Bins);
                sumR[bin] += blue.R[i]; sumG[bin] += blue.G[i]; sumB[bin] += blue.B[i]; hits[bin]++;
            }
        // Fill empty bins from their neighbours so the ramp is continuous end to end.
        for (int pass = 0; pass < Bins; pass++)
            for (int i = 0; i < Bins; i++)
            {
                if (hits[i] > 0) continue;
                int lo = i - 1, hi = i + 1;
                if (lo >= 0 && hits[lo] > 0) { sumR[i] = sumR[lo] / hits[lo]; sumG[i] = sumG[lo] / hits[lo]; sumB[i] = sumB[lo] / hits[lo]; hits[i] = 1; }
                else if (hi < Bins && hits[hi] > 0) { sumR[i] = sumR[hi] / hits[hi]; sumG[i] = sumG[hi] / hits[hi]; sumB[i] = sumB[hi] / hits[hi]; hits[i] = 1; }
            }
        var filled = Body.Clone();
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int i = y * w + x;
                if (Body.A[i] > .6f) continue;
                double dx = x + .5 - SphereX, dy = y + .5 - SphereY;
                if (dx * dx + dy * dy > (SphereR - 1.5) * (SphereR - 1.5)) continue;
                int bin = LightBin(x + .5, y + .5, lx, ly, lz, Bins);
                if (hits[bin] <= 0) continue;
                filled.R[i] = (float)(sumR[bin] / hits[bin]); filled.G[i] = (float)(sumG[bin] / hits[bin]);
                filled.B[i] = (float)(sumB[bin] / hits[bin]); filled.A[i] = 1;
            }
        Body = filled;
    }

    /// <summary>Bins a point on the sphere by how directly it faces the key light.</summary>
    static int LightBin(double x, double y, double lx, double ly, double lz, int bins)
    {
        double nx = (x - SphereX) / SphereR, ny = (y - SphereY) / SphereR;
        double r2 = nx * nx + ny * ny;
        if (r2 > 1) { double s = 1 / Math.Sqrt(r2); nx *= s; ny *= s; r2 = 1; }
        double nz = Math.Sqrt(Math.Max(0, 1 - r2));
        double d = nx * lx + ny * ly + nz * lz;             // -1 .. 1
        int bin = (int)((d + 1) * .5 * (bins - 1) + .5);
        return bin < 0 ? 0 : bin > bins - 1 ? bins - 1 : bin;
    }
}
