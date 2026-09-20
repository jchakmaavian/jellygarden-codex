param([string]$Dir, [string]$Out, [string]$Mode = 'main')
$ErrorActionPreference = 'Stop'
Add-Type -Path (Join-Path $Dir 'review_ColorBall_TargetImpact.cs') -ReferencedAssemblies System.Drawing
$fr = Join-Path $Out 'frames'
$p = 'ColorBall_TargetImpact'
if ($Mode -eq 'main') {
  [ReviewTargetImpact]::Run($fr, $p, 46, 192.0, 192.0, 192.0)
}
if ($Mode -eq 'needles') {
  foreach ($f in 1, 2, 3, 5, 7) {
    "== frame $f"
    [ReviewTargetImpact]::Needles((Join-Path $fr ('{0}_{1:000}.png' -f $p, $f)), 192.0, 192.0, 70.0, 60.0)
  }
}
if ($Mode -eq 'blobs') {
  foreach ($f in 6, 8, 10, 12, 15, 19, 20, 22, 24, 28, 30, 31, 32, 34, 40, 44) {
    "== frame $f (outside r>=0, alpha>=100)"
    [ReviewTargetImpact]::Blobs((Join-Path $fr ('{0}_{1:000}.png' -f $p, $f)), 100, 192.0, 192.0, 0.0)
  }
}
if ($Mode -eq 'annulus') {
  "puff edge band 60..110 (frames 1..12)"
  [ReviewTargetImpact]::Annulus($fr, $p, 1, 12, 192.0, 192.0, 60.0, 110.0)
  "ring inner band 40..80 (frames 14..28)"
  [ReviewTargetImpact]::Annulus($fr, $p, 14, 28, 192.0, 192.0, 30.0, 80.0)
}
if ($Mode -eq 'compare') {
  [ReviewTargetImpact]::Compare($fr, (Join-Path $Out 'review\rerender\frames'), $p, 46)
}
