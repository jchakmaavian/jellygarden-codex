param([string]$Dir, [string]$Out)
$ErrorActionPreference = 'Stop'
Add-Type -Path (Join-Path $Dir 'VfxStarSparkCheck.cs') -ReferencedAssemblies System.Drawing
foreach ($clip in @('White', 'Gold', 'Static')) {
  [VfxStarSparkCheck]::Run((Join-Path $Out "$clip\frames"), $clip, $false, 2, 64.0, 64.0)
}
# Life clips must end fully transparent.
foreach ($clip in @('White', 'Gold')) {
  $last = Join-Path $Out "$clip\frames\ColorBall_StarSpark_022.png"
  Add-Type -AssemblyName System.Drawing
  $bmp = New-Object System.Drawing.Bitmap $last
  $maxA = 0
  for ($y = 0; $y -lt $bmp.Height; $y++) { for ($x = 0; $x -lt $bmp.Width; $x++) { $a = $bmp.GetPixel($x, $y).A; if ($a -gt $maxA) { $maxA = $a } } }
  $bmp.Dispose()
  "$clip last frame (022) max alpha: $maxA " + $(if ($maxA -eq 0) { 'OK fully transparent' } else { 'FAIL' })
}
