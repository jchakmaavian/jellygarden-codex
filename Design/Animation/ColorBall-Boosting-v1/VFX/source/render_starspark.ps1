param([string]$Dir, [string]$Out, [int]$From = 0, [int]$To = 21)
$ErrorActionPreference = 'Stop'
Add-Type -Path @((Join-Path $Dir '..\BoosterFx.cs'), (Join-Path $Dir 'VfxStarSpark.cs')) -ReferencedAssemblies System.Drawing
$sw = [Diagnostics.Stopwatch]::StartNew()
[VfxStarSpark]::Render($Out, $From, $To)
"render ms: " + $sw.ElapsedMilliseconds
