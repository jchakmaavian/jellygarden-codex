param([string]$Dir)
$ErrorActionPreference = 'Stop'
Add-Type -Path @((Join-Path $Dir '..\BoosterFx.cs'), (Join-Path $Dir 'VfxColorBallTargetGlow.cs')) -ReferencedAssemblies System.Drawing
[VfxColorBallTargetGlow]::Probe()
