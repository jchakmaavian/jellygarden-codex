param([string]$Dir, [string]$Root, [switch]$Compose)
$ErrorActionPreference = 'Stop'
Add-Type -Path (Join-Path $Dir 'review_ColorBall_Beam.cs') -ReferencedAssemblies System.Drawing
$rev = Join-Path $Root 'review'
New-Item -ItemType Directory -Force $rev | Out-Null
if ($Compose) {
  $c = [ReviewColorBallBeam]::Compose($Root, $rev)
  $c | Out-File -Encoding utf8 (Join-Path $rev 'review_compose_report.txt')
  $c
} else {
  $txt = [ReviewColorBallBeam]::Run($Root)
  $txt | Out-File -Encoding utf8 (Join-Path $rev 'review_check_report.txt')
  $txt
}
