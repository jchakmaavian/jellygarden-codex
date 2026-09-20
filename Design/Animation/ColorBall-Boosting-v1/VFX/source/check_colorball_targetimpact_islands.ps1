param([string]$Dir, [string]$Out)
$ErrorActionPreference = 'Stop'
Add-Type -Path (Join-Path $Dir 'review_ColorBall_TargetImpact.cs') -ReferencedAssemblies System.Drawing
$fr = Join-Path $Out 'frames'
foreach ($f in 11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26) {
  $s = [ReviewTargetImpact]::Blobs((Join-Path $fr ('ColorBall_TargetImpact_{0:000}.png' -f $f)), 24, 192.0, 192.0, 0.0)
  $small = ($s -split "`n") | Where-Object { $_ -match 'n=\s*(\d+)' -and [int]$matches[1] -lt 200 -and $_ -match 'r=([\d\.]+)px' -and [double]$matches[1] -lt 115 }
  "f$f : " + (($small | ForEach-Object { $_.Trim() }) -join "`n      ")
}
