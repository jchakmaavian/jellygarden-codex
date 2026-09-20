param([string]$Dir, [string]$Root)
# Compiles the builder's source read-only in memory, checks loop periodicity and that saved PNGs match the current source. Writes nothing.
$ErrorActionPreference = 'Stop'
Add-Type -Path @((Join-Path $Dir '..\BoosterFx.cs'), (Join-Path $Dir 'VfxColorBallBeam.cs')) -ReferencedAssemblies System.Drawing
[VfxColorBallBeam]::LoopSelfTest()
$q = Join-Path $Root 'Sequence\frames'
foreach ($f in 6, 11, 13, 20, 26, 30) {
  $img = New-Object Img 640, 160
  [VfxColorBallBeam]::SequenceFrame($img, $f)
  $png = [Img]::Load((Join-Path $q ('ColorBall_Beam_{0:000}.png' -f $f)))
  $mx = 0.0
  for ($i = 0; $i -lt $img.A.Length; $i += 1) { $d = [Math]::Abs([Math]::Round([Img]::Clamp01($img.A[$i]) * 255) - [Math]::Round($png.A[$i] * 255)); if ($d -gt $mx) { $mx = $d } }
  "f$f source-vs-png max alpha byte diff: $mx"
}
