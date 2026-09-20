param([string]$SourceImage,[string]$Output,[switch]$Samples)
$ErrorActionPreference='Stop'
$parts=@('BoosterFx.cs','BallAnalyze.cs','BallRig.cs','ColorBallSeq.cs','ColorBallReveal.cs','ColorBallMotion.cs') | ForEach-Object { Join-Path $PSScriptRoot $_ }
Add-Type -Path $parts -ReferencedAssemblies System.Drawing
$null=[BallRig]::Setup($SourceImage,'')
foreach($seq in @('Reveal','Idle','Boosting')) {
  $n=if($seq -eq 'Reveal'){60}elseif($seq -eq 'Idle'){120}else{108}
  $dir=Join-Path $Output $seq
  if($Samples) {foreach($f in @(0,4,8,12,18,28,40,53)) { [ColorBallMotion]::Render($seq,$dir,$f,$f) }}
  else {[ColorBallMotion]::Render($seq,$dir,0,($n-1))}
  Write-Output "Rendered $seq"
}
