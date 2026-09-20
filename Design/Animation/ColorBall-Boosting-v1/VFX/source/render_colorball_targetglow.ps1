param([string]$Dir, [string]$Out, [int]$From = 0, [int]$To = 29, [string]$Clip = 'all')
$ErrorActionPreference = 'Stop'
Add-Type -Path @((Join-Path $Dir '..\BoosterFx.cs'), (Join-Path $Dir 'VfxColorBallTargetGlow.cs')) -ReferencedAssemblies System.Drawing
$sw = [Diagnostics.Stopwatch]::StartNew()
if ($Clip -eq 'all') { [VfxColorBallTargetGlow]::Render($Out, $From, $To) } else { [VfxColorBallTargetGlow]::RenderClip($Out, $Clip, $From, $To) }
"render ms: " + $sw.ElapsedMilliseconds
