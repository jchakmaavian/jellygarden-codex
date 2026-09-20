// Measures the rotor's foreshortening: the hub ring is a circle in the rotor plane, so the
// ratio of its drawn height to its width gives the tilt the blades must sweep through.
using System;
using System.Text;

public static class PropTilt
{
    public static double K;

    public static string Measure(string artPath)
    {
        var art = Img.Load(artPath);
        int w = art.W, h = art.H;
        double cx = PropGeom.HubCx, cy = PropGeom.HubCy;
        var sb = new StringBuilder();

        // Hub ring: gold pixels in the annulus just outside the dome.
        double maxX = 0, maxY = 0;
        double ringOuter = 0;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int i = y * w + x;
                float a = art.A[i]; if (a < .5f) continue;
                float r = art.R[i] / a, g = art.G[i] / a, b = art.B[i] / a;
                if (PropAnalyze.Classify(r, g, b, a) != PropAnalyze.Part.Gold) continue;
                double dx = x - cx, dy = y - cy;
                double d = Math.Sqrt(dx * dx + dy * dy);
                if (d > 120) continue;                       // stay on the hub ring, off the blades
                if (Math.Abs(dy) < 12 && Math.Abs(dx) > maxX) maxX = Math.Abs(dx);
                if (Math.Abs(dx) < 12 && Math.Abs(dy) > maxY) maxY = Math.Abs(dy);
                if (d > ringOuter) ringOuter = d;
            }
        sb.AppendLine("hub ring semi-axes: x " + maxX.ToString("0.0") + "  y " + maxY.ToString("0.0") + "  -> k = " + (maxY / maxX).ToString("0.000"));

        // Cross-check with the blade tips: their horizontal reach against their vertical reach.
        double tipMaxX = 0, tipUp = 0;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int i = y * w + x;
                float a = art.A[i]; if (a < .5f) continue;
                float r = art.R[i] / a, g = art.G[i] / a, b = art.B[i] / a;
                if (PropAnalyze.Classify(r, g, b, a) != PropAnalyze.Part.Red) continue;
                double dx = x - cx, dy = cy - y;
                if (Math.Sqrt(dx * dx + dy * dy) < PropGeom.DomeR * 1.6) continue;
                if (Math.Abs(dx) > tipMaxX) tipMaxX = Math.Abs(dx);
                if (dy > tipUp) tipUp = dy;
            }
        sb.AppendLine("blade tips: max |dx| " + tipMaxX.ToString("0.0") + "  max up " + tipUp.ToString("0.0"));
        // The blades are 120 deg apart in the rotor plane. Their uneven screen angles are the
        // projection, so k and the rotor's roll follow from fitting that constraint.
        double bestErr = double.MaxValue, bestK = .8, bestPsi = 30, bestRoll = 0;
        for (double k = .25; k <= 1.001; k += .005)
            for (double psi = 0; psi < 120; psi += .25)
                for (double roll = -12; roll <= 12; roll += .5)
                {
                    double err = 0;
                    for (int i = 0; i < 3; i++)
                    {
                        double p = (psi + 120 * i) * Math.PI / 180;
                        double t = Math.Atan2(k * Math.Sin(p), Math.Cos(p)) * 180 / Math.PI + roll;
                        double best = double.MaxValue;
                        for (int j = 0; j < 3; j++)
                        {
                            double d = Math.Abs(((t - PropGeom.BladeDeg[j] + 540) % 360) - 180);
                            if (d < best) best = d;
                        }
                        err += best * best;
                    }
                    if (err < bestErr) { bestErr = err; bestK = k; bestPsi = psi; bestRoll = roll; }
                }
        K = bestK;
        sb.AppendLine("fitted rotor: k = " + bestK.ToString("0.000") + "  psi0 = " + bestPsi.ToString("0.0") + "  roll = " + bestRoll.ToString("0.0") + "  rms err " + Math.Sqrt(bestErr / 3).ToString("0.00") + " deg");
        sb.AppendLine("rotor plane tilt " + (Math.Acos(Math.Min(1, K)) * 180 / Math.PI).ToString("0.0") + " deg from face-on");
        for (int i = 0; i < 3; i++)
        {
            double p = (bestPsi + 120 * i) * Math.PI / 180;
            double t = Math.Atan2(K * Math.Sin(p), Math.Cos(p)) * 180 / Math.PI + bestRoll;
            sb.AppendLine("  blade " + i + ": rotor psi " + (bestPsi + 120 * i).ToString("0.0") + " -> screen " + ((t % 360 + 360) % 360).ToString("0.0"));
        }
        return sb.ToString();
    }
}
