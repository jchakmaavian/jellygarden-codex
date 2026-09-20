param([string]$Dir, [string]$Out)
$ErrorActionPreference = 'Stop'
Add-Type -Path (Join-Path $Dir 'review_ColorBall_TargetImpact.cs') -ReferencedAssemblies System.Drawing
$fr = Join-Path $Out 'frames'
"needle reach alpha>=64"
[ReviewTargetImpact]::Reach($fr, 'ColorBall_TargetImpact', 1, 10, 192.0, 192.0, [double[]]@(211.8, 314.3, 110.0), 64)
"needle reach alpha>=128"
[ReviewTargetImpact]::Reach($fr, 'ColorBall_TargetImpact', 1, 10, 192.0, 192.0, [double[]]@(211.8, 314.3, 110.0), 128)
"control angles (no needle) alpha>=64"
[ReviewTargetImpact]::Reach($fr, 'ColorBall_TargetImpact', 1, 10, 192.0, 192.0, [double[]]@(0.0, 160.0, 260.0), 64)
