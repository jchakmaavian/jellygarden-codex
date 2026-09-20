param([string]$Dir, [string]$Root)
$ErrorActionPreference = 'Stop'
Add-Type -Path @((Join-Path $Dir 'QaCheck.cs')) -ReferencedAssemblies System.Drawing
$R = Join-Path $Root 'reveal'; $I = Join-Path $Root 'idle'; $B = Join-Path $Root 'boost'
[QaCheck]::Run($R, 'ColorBall_Reveal', 68, -1, -1, '24')
[QaCheck]::Run($I, 'ColorBall_Idle', 120, 0, 119, '')
[QaCheck]::Run($B, 'ColorBall_Boosting', 174, 30, 119, '1,120')
[QaCheck]::Run((Join-Path $Root 'coreflare'), 'ColorBall_CoreFlare', 90, 0, 89, '')
[QaCheck]::Run((Join-Path $Root 'finishring'), 'ColorBall_FinishRing_Large', 54, -1, -1, '')
'-- hand-offs'
[QaCheck]::Identity((Join-Path $R 'ColorBall_Reveal_068.png'), (Join-Path $I 'ColorBall_Idle_001.png'))
[QaCheck]::Identity((Join-Path $I 'ColorBall_Idle_001.png'), (Join-Path $B 'ColorBall_Boosting_001.png'))
[QaCheck]::Transparent((Join-Path $B 'ColorBall_Boosting_174.png'))
[QaCheck]::Transparent((Join-Path (Join-Path $Root 'finishring') 'ColorBall_FinishRing_Large_054.png'))
