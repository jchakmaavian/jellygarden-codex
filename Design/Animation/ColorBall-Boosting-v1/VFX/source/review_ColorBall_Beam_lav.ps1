param([string]$Dir, [string]$Root)
$ErrorActionPreference = 'Stop'
Add-Type -Path @((Join-Path $Dir 'review_ColorBall_Beam.cs'), (Join-Path $Dir 'review_ColorBall_Beam_lav.cs')) -ReferencedAssemblies System.Drawing
$t = [ReviewColorBallBeamLav]::Run($Root)
$t | Out-File -Encoding utf8 (Join-Path $Root 'review\review_lavender_grey_report.txt')
$t
