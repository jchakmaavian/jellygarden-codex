// Measures the propeller's sphere, hub and blade axes from the artwork.
using System;
using System.Text;

public static class PropGeom
{
    public static double SphereCx, SphereCy, SphereR;
    public static double HubCx, HubCy, HubR, DomeR;
    public static double[] BladeDeg = new double[3];
    public static double BladeReach;

    public static string Measure(string artPath)
    {
        var art = Img.Load(artPath);
        int w = art.W, h = art.H;
        var part = new PropAnalyze.Part[w * h];
        for (int i = 0; i < w * h; i++)
        {
            float a = art.A[i];
            float r = a > 1e-5f ? art.R[i] / a : 0, g = a > 1e-5f ? art.G[i] / a : 0, b = a > 1e-5f ? art.B[i] / a : 0;
            part[i] = PropAnalyze.Classify(r, g, b, a);
        }

        // Hub dome: the magenta blob with the largest area, found by flood fill.
        int bestN = 0; double bx = 0, by = 0; int bMinX = 0, bMaxX = 0, bMinY = 0, bMaxY = 0;
        var seen = new bool[w * h];
        var stack = new System.Collections.Generic.Stack<int>();
        for (int s = 0; s < w * h; s++)
        {
            if (seen[s] || part[s] != PropAnalyze.Part.Magenta) continue;
            stack.Push(s); seen[s] = true;
            int n = 0; double sx = 0, sy = 0; int miX = w, maX = -1, miY = h, maY = -1;
            while (stack.Count > 0)
            {
                int i = stack.Pop(); int x = i % w, y = i / w;
                n++; sx += x; sy += y;
                if (x < miX) miX = x; if (x > maX) maX = x; if (y < miY) miY = y; if (y > maY) maY = y;
                if (x > 0 && !seen[i - 1] && part[i - 1] == PropAnalyze.Part.Magenta) { seen[i - 1] = true; stack.Push(i - 1); }
                if (x < w - 1 && !seen[i + 1] && part[i + 1] == PropAnalyze.Part.Magenta) { seen[i + 1] = true; stack.Push(i + 1); }
                if (y > 0 && !seen[i - w] && part[i - w] == PropAnalyze.Part.Magenta) { seen[i - w] = true; stack.Push(i - w); }
                if (y < h - 1 && !seen[i + w] && part[i + w] == PropAnalyze.Part.Magenta) { seen[i + w] = true; stack.Push(i + w); }
            }
            if (n > bestN) { bestN = n; bx = sx / n; by = sy / n; bMinX = miX; bMaxX = maX; bMinY = miY; bMaxY = maY; }
        }
        HubCx = bx; HubCy = by; DomeR = (bMaxX - bMinX + 1) / 2.0;

        // Sphere: fit a circle to the alpha silhouette well below the hub, where only the body shows.
        double sumX = 0, sumY = 0, sumX2 = 0, sumY2 = 0, sumXY = 0, sumX3 = 0, sumY3 = 0, sumX2Y = 0, sumXY2 = 0; int m = 0;
        for (int y = (int)HubCy + 90; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int i = y * w + x;
                if (art.A[i] <= .5f) continue;
                bool edge = x == 0 || x == w - 1 || y == h - 1 || art.A[i - 1] <= .5f || art.A[i + 1] <= .5f || art.A[i + w] <= .5f;
                if (!edge) continue;
                double X = x, Y = y;
                sumX += X; sumY += Y; sumX2 += X * X; sumY2 += Y * Y; sumXY += X * Y;
                sumX3 += X * X * X; sumY3 += Y * Y * Y; sumX2Y += X * X * Y; sumXY2 += X * Y * Y; m++;
            }
        double C = m * sumX2 - sumX * sumX, D = m * sumXY - sumX * sumY, E = m * sumX3 + m * sumXY2 - (sumX2 + sumY2) * sumX;
        double G = m * sumY2 - sumY * sumY, H2 = m * sumX2Y + m * sumY3 - (sumX2 + sumY2) * sumY;
        double a1 = (E * G - H2 * D) / (C * G - D * D), b1 = (C * H2 - D * E) / (C * G - D * D);
        SphereCx = a1 / 2; SphereCy = b1 / 2;
        double c1 = (sumX2 + sumY2 - a1 * sumX - b1 * sumY) / m;
        SphereR = Math.Sqrt(c1 + SphereCx * SphereCx + SphereCy * SphereCy);

        // Blade axes: the angular histogram of red tip pixels around the hub has three peaks.
        var bins = new double[360];
        double reach = 0;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                if (part[y * w + x] != PropAnalyze.Part.Red) continue;
                double dx = x - HubCx, dy = HubCy - y;
                double d = Math.Sqrt(dx * dx + dy * dy);
                if (d < DomeR * 1.6) continue;              // skip the specular streak inside the dome
                double deg = Math.Atan2(dy, dx) * 180 / Math.PI; if (deg < 0) deg += 360;
                bins[(int)deg] += 1;
                if (d > reach) reach = d;
            }
        BladeReach = reach;
        var smooth = new double[360];
        for (int i = 0; i < 360; i++) { double s2 = 0; for (int k = -12; k <= 12; k++) s2 += bins[((i + k) % 360 + 360) % 360]; smooth[i] = s2; }
        for (int p = 0; p < 3; p++)
        {
            int best = -1; double bestV = -1;
            for (int i = 0; i < 360; i++)
            {
                bool far = true;
                // Blades sit 120 deg apart, so a new peak has to be well clear of the ones already found.
                for (int q = 0; q < p; q++) { double d2 = Math.Abs(((i - BladeDeg[q] + 540) % 360) - 180); if (d2 < 60) far = false; }
                if (far && smooth[i] > bestV) { bestV = smooth[i]; best = i; }
            }
            // Refine with the weighted mean around the peak.
            double wsum = 0, asum = 0;
            for (int k = -25; k <= 25; k++) { int i = ((best + k) % 360 + 360) % 360; wsum += smooth[i]; asum += smooth[i] * (best + k); }
            BladeDeg[p] = ((asum / wsum) % 360 + 360) % 360;
        }
        Array.Sort(BladeDeg);

        var sb = new StringBuilder();
        sb.AppendLine("sphere centre (" + SphereCx.ToString("0.00") + ", " + SphereCy.ToString("0.00") + ") radius " + SphereR.ToString("0.00"));
        sb.AppendLine("hub dome centre (" + HubCx.ToString("0.00") + ", " + HubCy.ToString("0.00") + ") dome radius " + DomeR.ToString("0.00"));
        sb.AppendLine("hub above sphere centre by " + (SphereCy - HubCy).ToString("0.0") + " px; hub offset x " + (HubCx - SphereCx).ToString("0.0"));
        sb.AppendLine("blade axes deg: " + BladeDeg[0].ToString("0.0") + ", " + BladeDeg[1].ToString("0.0") + ", " + BladeDeg[2].ToString("0.0"));
        sb.AppendLine("blade spacing: " + (BladeDeg[1] - BladeDeg[0]).ToString("0.0") + ", " + (BladeDeg[2] - BladeDeg[1]).ToString("0.0") + ", " + (360 - BladeDeg[2] + BladeDeg[0]).ToString("0.0"));
        sb.AppendLine("blade tip reach " + BladeReach.ToString("0.0") + " px from hub");
        return sb.ToString();
    }
}
