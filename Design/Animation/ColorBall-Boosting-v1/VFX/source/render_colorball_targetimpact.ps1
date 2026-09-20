param([string]$Dir, [string]$Out, [int]$From, [int]$To)
$ErrorActionPreference = 'Stop'
Add-Type -Path @((Join-Path $Dir '..\BoosterFx.cs'), (Join-Path $Dir 'VfxColorBallTargetImpact.cs')) -ReferencedAssemblies System.Drawing
$sw = [Diagnostics.Stopwatch]::StartNew()
[VfxColorBallTargetImpact]::Render($Out, $From, $To)
"rendered $From-$To in " + $sw.ElapsedMilliseconds + " ms"
