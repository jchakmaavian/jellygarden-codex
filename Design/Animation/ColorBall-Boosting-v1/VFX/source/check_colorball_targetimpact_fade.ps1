param([string]$Dir, [string]$Out)
$ErrorActionPreference = 'Stop'
Add-Type -Path (Join-Path $Dir 'review_ColorBall_TargetImpact.cs') -ReferencedAssemblies System.Drawing
$src = @'
using System;
using System.Text;
public static class RimColour
{
    // For the ring band (r 70..125 px): texels with alpha 60..235, composite over navy; count max<215 split by
    // distance to the nearest alpha>=250 texel (<=1 px = AA edge of an opaque filament, >1 px = halo / translucent body).
    public static string Run(byte[] px, int w, int h, int f)
    {
        int bodyLike = 0; int nA = 0, dullEdge = 0, dullOff = 0, opaque = 0, opaqueDull = 0;

        for (int y = 1; y < h - 1; y++)
            for (int x = 1; x < w - 1; x++)
            {
                double dx = x + 0.5 - 192, dy = y + 0.5 - 192, r = Math.Sqrt(dx * dx + dy * dy);
                if (r < 70 || r > 125) continue;
                int i = (y * w + x) * 4; int a = px[i + 3]; if (a == 0) continue;
                double al = a / 255.0;
                double cr = px[i + 2] * al + 32 * (1 - al), cg = px[i + 1] * al + 36 * (1 - al), cb = px[i] * al + 46 * (1 - al);
                double mx = Math.Max(cr, Math.Max(cg, cb));
                if (a >= 250) { opaque++; if (Math.Max(px[i + 2], Math.Max(px[i + 1], px[i])) < 235) opaqueDull++; continue; }
                if (a < 60 || a > 235) continue;
                nA++;
                if (mx >= 215) continue;
                bool nearOpaque = false;
                for (int oy = -1; oy <= 1 && !nearOpaque; oy++) for (int ox = -1; ox <= 1; ox++) if (px[((y + oy) * w + x + ox) * 4 + 3] >= 250) { nearOpaque = true; break; }
                if (nearOpaque) dullEdge++; else { dullOff++; if (px[i] > 170) bodyLike++; }
            }
        return string.Format("f{0}: opaque {1} (straight max<235: {2}) | alpha60-235 {3}: over-navy max<215 {4} = AA edge {5} + halo/translucent {6} (of which body-coloured, straight B>170: {7})", f, opaque, opaqueDull, nA, dullEdge + dullOff, dullEdge, dullOff, bodyLike);
    }
}
'@
Add-Type -TypeDefinition $src
$fr = Join-Path $Out 'frames'
foreach ($f in 24,26,28,29,30,31,32) {
  $w = 0; $h = 0
  $px = [ReviewTargetImpact]::Load((Join-Path $fr ('ColorBall_TargetImpact_{0:000}.png' -f $f)), [ref]$w, [ref]$h)
  [RimColour]::Run($px, $w, $h, $f)
}
