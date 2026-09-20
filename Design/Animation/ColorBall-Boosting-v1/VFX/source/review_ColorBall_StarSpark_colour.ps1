param([string]$Dir, [string]$Out)
$ErrorActionPreference = 'Stop'
Add-Type -Path (Join-Path $Dir 'review_ColorBall_StarSpark.cs') -ReferencedAssemblies System.Drawing
foreach ($clip in @('White','Gold')) { foreach ($fr in @(3,5,10,14,17,19,20,21)) { [ReviewStarSparkColour]::Stats((Join-Path $Out ("$clip\frames\ColorBall_StarSpark_{0:000}.png" -f $fr)), "$clip f$fr") } }
foreach ($fr in @(1,2)) { [ReviewStarSparkColour]::Stats((Join-Path $Out ("Static\frames\ColorBall_StarSpark_{0:000}.png" -f $fr)), "Static f$fr") }
