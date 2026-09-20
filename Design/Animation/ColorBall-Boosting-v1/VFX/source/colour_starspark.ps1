param([string]$Dir, [string]$Out)
# Colour acceptance metrics (composited on navy #20242e) per frame for White and Gold, plus Static arm reach.
$ErrorActionPreference = 'Stop'
Add-Type -Path (Join-Path $Dir 'VfxStarSparkCheck.cs') -ReferencedAssemblies System.Drawing
foreach ($clip in @('White', 'Gold')) {
  for ($f = 1; $f -le 22; $f++) {
    [VfxStarSparkCheck]::Colour((Join-Path $Out ("$clip\frames\ColorBall_StarSpark_{0:000}.png" -f $f)), ("$clip f$f"))
  }
}
foreach ($f in @(1, 2)) { [VfxStarSparkCheck]::Colour((Join-Path $Out ("Static\frames\ColorBall_StarSpark_{0:000}.png" -f $f)), ("Static f$f")) }
foreach ($f in @(1, 2)) { [VfxStarSparkCheck]::Reach((Join-Path $Out ("Static\frames\ColorBall_StarSpark_{0:000}.png" -f $f)), 64.0, 64.0) }
foreach ($clip in @('White', 'Gold')) { foreach ($f in @(3, 10)) { [VfxStarSparkCheck]::Reach((Join-Path $Out ("$clip\frames\ColorBall_StarSpark_{0:000}.png" -f $f)), 64.0, 64.0) } }
