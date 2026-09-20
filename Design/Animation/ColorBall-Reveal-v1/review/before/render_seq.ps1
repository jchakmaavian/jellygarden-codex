param([string]$Dir, [string]$Src, [string]$Seq, [string]$Out, [string]$Frames)
$ErrorActionPreference = 'Stop'
Add-Type -Path @((Join-Path $Dir 'BoosterFx.cs'), (Join-Path $Dir 'BallAnalyze.cs'), (Join-Path $Dir 'BallRig.cs'), (Join-Path $Dir 'ColorBallSeq.cs'), (Join-Path $Dir 'ColorBallReveal.cs'), (Join-Path $Dir 'ColorBallIdleBoost.cs')) -ReferencedAssemblies System.Drawing
$null = [BallRig]::Setup($Src, "")
$sw = [Diagnostics.Stopwatch]::StartNew()
foreach ($f in ($Frames -split ',')) {
  if ($f -match '^(\d+)-(\d+)$') { $a = [int]$matches[1]; $b = [int]$matches[2] } else { $a = [int]$f; $b = [int]$f }
  switch ($Seq) {
    'reveal' { [RevealSeq]::Render($Out, $a, $b) }
    'idle' { [IdleSeq]::Render($Out, $a, $b) }
    'boost' { [BoostSeq]::Render($Out, $a, $b) }
    'coreflare' { [CoreFlareAsset]::Render($Out, $a, $b) }
    'finishring' { [FinishRingAsset]::Render($Out, $a, $b) }
  }
}
"render ms: " + $sw.ElapsedMilliseconds
