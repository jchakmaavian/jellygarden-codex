param([string]$Dir, [string]$Out)
$ErrorActionPreference = 'Stop'
Add-Type -Path (Join-Path $Dir 'review_ColorBall_TargetImpact.cs') -ReferencedAssemblies System.Drawing
$fr = Join-Path $Out 'frames'
foreach ($f in 10, 14, 18, 22, 26, 33) {
  "== f$f stars (alpha>=24, r>=0.45c)"
  [ReviewTargetImpact]::Blobs((Join-Path $fr ('ColorBall_TargetImpact_{0:000}.png' -f $f)), 24, 192.0, 192.0, 86.0) | Out-String
}
