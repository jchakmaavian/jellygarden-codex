param([string]$Dir, [string]$Root)
$ErrorActionPreference = 'Stop'
Add-Type -Path @((Join-Path $Dir 'review_ColorBall_Beam.cs'), (Join-Path $Dir 'review_ColorBall_Beam_extra.cs')) -ReferencedAssemblies System.Drawing
$t = [ReviewColorBallBeamExtra]::Run($Root)
$t | Out-File -Encoding utf8 (Join-Path $Root 'review\review_extra_report.txt')
$t
