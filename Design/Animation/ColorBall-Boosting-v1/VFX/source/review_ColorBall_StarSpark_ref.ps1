param([string]$Dir, [string]$Out)
$ErrorActionPreference = 'Stop'
Add-Type -Path @((Join-Path $Dir '..\BoosterFx.cs'), (Join-Path $Dir 'review_ColorBall_StarSpark_ref.cs')) -ReferencedAssemblies System.Drawing
[ReviewStarSparkRef]::Render($Out)
