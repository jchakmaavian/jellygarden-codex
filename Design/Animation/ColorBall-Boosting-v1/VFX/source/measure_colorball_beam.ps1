param([string]$Out)
# Prints alpha/colour profiles of ColorBall_Beam frames to verify spec widths.
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @"
using System; using System.Drawing; using System.Text;
public static class BeamMeasure {
  // Width (px) of the run of texels in column x whose alpha >= aMin and (optionally) blue >= bMin (white core test).
  public static string Col(string path, int x, int yc) {
    using (var b = new Bitmap(path)) {
      int core = 0, a50 = 0, a10 = 0, any = 0; var sb = new StringBuilder();
      for (int y = 0; y < b.Height; y++) { var c = b.GetPixel(x, y);
        if (c.A >= 250 && c.B >= 235) core++; if (c.A >= 128) a50++; if (c.A >= 26) a10++; if (c.A > 0) any++; }
      for (int y = yc; y < Math.Min(b.Height, yc + 26); y++) { var c = b.GetPixel(x, y); sb.Append(string.Format("{0}:{1}/{2},{3},{4} ", y - yc, c.A, c.R, c.G, c.B)); }
      return string.Format("x={0}: whiteCore={1} a>=50%={2} a>=10%={3} any={4}\n   {5}", x, core, a50, a10, any, sb);
    }
  }
  public static string Row(string path, int y) {
    using (var b = new Bitmap(path)) {
      int first = -1, last = -1, cf = -1, cl = -1;
      for (int x = 0; x < b.Width; x++) { var c = b.GetPixel(x, y); if (c.A > 0) { if (first < 0) first = x; last = x; } if (c.A >= 250 && c.B >= 235) { if (cf < 0) cf = x; cl = x; } }
      return string.Format("row y={0}: alpha>0 x {1}..{2}; white core x {3}..{4}", y, first, last, cf, cl);
    }
  }
}
"@
$s = Join-Path $Out 'Strip\frames\ColorBall_Beam_001.png'
"STRIP"; [BeamMeasure]::Row($s, 48); foreach ($x in 14, 20, 256, 490, 498) { [BeamMeasure]::Col($s, $x, 48) }
$h = Join-Path $Out 'Head\frames\ColorBall_Beam_001.png'
"HEAD"; [BeamMeasure]::Row($h, 32); foreach ($x in 60, 90, 105, 115, 125, 132) { [BeamMeasure]::Col($h, $x, 32) }
$q = Join-Path $Out 'Sequence\frames'
"SEQ f11"; [BeamMeasure]::Row((Join-Path $q 'ColorBall_Beam_011.png'), 80); [BeamMeasure]::Col((Join-Path $q 'ColorBall_Beam_011.png'), 560, 80)
"SEQ f20"; [BeamMeasure]::Row((Join-Path $q 'ColorBall_Beam_020.png'), 80); foreach ($x in 30, 290, 520, 560) { [BeamMeasure]::Col((Join-Path $q 'ColorBall_Beam_020.png'), $x, 80) }
"SEQ f5"; [BeamMeasure]::Row((Join-Path $q 'ColorBall_Beam_005.png'), 80); [BeamMeasure]::Col((Join-Path $q 'ColorBall_Beam_005.png'), 200, 80)
