param([string]$Dir, [string]$Root)
$ErrorActionPreference = 'Stop'
Add-Type -Path (Join-Path $Dir 'review_ColorBall_TargetGlow.cs') -ReferencedAssemblies System.Drawing
$txt = [ReviewColorBallTargetGlowExtra]::Run($Root, 'ColorBall_TargetGlow')
$txt | Out-File -Encoding utf8 (Join-Path $Root 'review\review_check_extra.txt')
$txt
