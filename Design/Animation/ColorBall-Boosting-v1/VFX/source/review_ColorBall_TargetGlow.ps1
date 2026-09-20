param([string]$Dir, [string]$Root, [switch]$Pieces)
$ErrorActionPreference = 'Stop'
Add-Type -Path (Join-Path $Dir 'review_ColorBall_TargetGlow.cs') -ReferencedAssemblies System.Drawing
$rev = Join-Path $Root 'review'
if ($Pieces) {
  [ReviewColorBallTargetGlow]::WritePiece((Join-Path $rev 'review_piece_red.png'), 230, 50, 40)
  [ReviewColorBallTargetGlow]::WritePiece((Join-Path $rev 'review_piece_yellow.png'), 255, 205, 30)
  [ReviewColorBallTargetGlow]::WritePiece((Join-Path $rev 'review_piece_blue.png'), 40, 110, 235)
  [ReviewColorBallTargetGlow]::WritePiece((Join-Path $rev 'review_piece_green.png'), 60, 200, 70)
  'pieces written'
}
$txt = [ReviewColorBallTargetGlow]::Check($Root, 'ColorBall_TargetGlow')
$txt | Out-File -Encoding utf8 (Join-Path $rev 'review_check_report.txt')
$txt
