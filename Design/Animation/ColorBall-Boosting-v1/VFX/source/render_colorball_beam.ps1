param([string]$Dir, [string]$Out, [int]$From = 0, [int]$To = 33, [string]$Preview = '', [switch]$ScalePreviews)
# Renders ColorBall_Beam: Strip + Head (when From == 0) and Sequence / SequenceBody / Contact frames From..To (0-based).
# -Preview "<seqFrame1>,<tag>" writes in-context stacking previews; -ScalePreviews writes the split / 9-slice previews
# (both need rendered frames).
$ErrorActionPreference = 'Stop'
Add-Type -Path @((Join-Path $Dir '..\BoosterFx.cs'), (Join-Path $Dir 'VfxColorBallBeam.cs')) -ReferencedAssemblies System.Drawing
$sw = [Diagnostics.Stopwatch]::StartNew()
if ($ScalePreviews) {
  [VfxColorBallBeam]::RenderScalePreviews($Out)
} elseif ($Preview -eq '') {
  [VfxColorBallBeam]::Render($Out, $From, $To)
  [VfxColorBallBeam]::LoopSelfTest()
} else {
  $p = $Preview -split ','
  [VfxColorBallBeam]::RenderPreview($Out, [int]$p[0], 0, $p[1])
}
"render ms: " + $sw.ElapsedMilliseconds
