param([string]$Dir, [string]$Src, [string]$Seq, [string]$Out, [string]$Frames)
$ErrorActionPreference = 'Stop'
Add-Type -Path @(
  (Join-Path $Dir 'BoosterFx.cs'), (Join-Path $Dir 'PropAnalyze.cs'), (Join-Path $Dir 'PropGeom.cs'),
  (Join-Path $Dir 'PropTilt.cs'), (Join-Path $Dir 'PropRig.cs'), (Join-Path $Dir 'PropellerSeq.cs')
) -ReferencedAssemblies System.Drawing
$null = [PropRig]::Setup($Src)
$sw = [Diagnostics.Stopwatch]::StartNew()
foreach ($f in ($Frames -split ',')) {
  if ($f -match '^(\d+)-(\d+)$') { $a = [int]$matches[1]; $b = [int]$matches[2] } else { $a = [int]$f; $b = [int]$f }
  switch ($Seq) {
    'reveal' { [PropSeq]::Reveal($Out, $a, $b) }
    'idle' { [PropSeq]::Idle($Out, $a, $b) }
    'activation' { [PropSeq]::Activation($Out, $a, $b) }
  }
}
"render ms: " + $sw.ElapsedMilliseconds
