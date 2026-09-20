param([string]$Dir, [string]$Root, [switch]$MockPiece)
$ErrorActionPreference = 'Stop'
Add-Type -Path (Join-Path $Dir 'VfxColorBallTargetGlowCheck.cs') -ReferencedAssemblies System.Drawing
$prefix = 'ColorBall_TargetGlow'
if ($MockPiece) {
  $rev = Join-Path $Root 'review'
  New-Item -ItemType Directory -Force $rev | Out-Null
  [VfxColorBallTargetGlowCheck]::WriteMockPiece((Join-Path $rev 'mock_piece.png'))
  'mock piece written'
}
$on = Join-Path $Root 'On\frames'
$loop = Join-Path $Root 'Loop\frames'
[VfxColorBallTargetGlowCheck]::CheckClip($on, $prefix, $false)
[VfxColorBallTargetGlowCheck]::CheckClip($loop, $prefix, $true)
$onLast = Join-Path $on ($prefix + '_010.png')
$onPrev = Join-Path $on ($prefix + '_009.png')
$loop1 = Join-Path $loop ($prefix + '_001.png')
$loop2 = Join-Path $loop ($prefix + '_002.png')
'On f010 vs Loop f001: ' + [VfxColorBallTargetGlowCheck]::Compare($onLast, $loop1) + '  [must be 0]'
'handoff step On f009 -> Loop f001 = {0:0.00000} (seed change, compare with Loop avg seed-change step)' -f [VfxColorBallTargetGlowCheck]::Step($onPrev, $loop1)
'handoff step Loop f001 -> Loop f002 = {0:0.00000}' -f [VfxColorBallTargetGlowCheck]::Step($loop1, $loop2)
