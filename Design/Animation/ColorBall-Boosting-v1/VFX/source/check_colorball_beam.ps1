param([string]$Dir, [string]$Out)
# Technical check of the rendered ColorBall_Beam clips. $Out = out\vfx\ColorBall_Beam
$ErrorActionPreference = 'Stop'
Add-Type -Path (Join-Path $Dir 'VfxColorBallBeamCheck.cs') -ReferencedAssemblies System.Drawing
[VfxColorBallBeamCheck]::Run('Strip', (Join-Path $Out 'Strip\frames'), 'ColorBall_Beam', 0, 0, $false)
[VfxColorBallBeamCheck]::Run('Head', (Join-Path $Out 'Head\frames'), 'ColorBall_Beam', 0, 0, $false)
[VfxColorBallBeamCheck]::Run('Sequence', (Join-Path $Out 'Sequence\frames'), 'ColorBall_Beam', 13, 26, $true)
[VfxColorBallBeamCheck]::Run('SequenceBody', (Join-Path $Out 'SequenceBody\frames'), 'ColorBall_Beam', 13, 26, $true)
[VfxColorBallBeamCheck]::Run('Contact', (Join-Path $Out 'Contact\frames'), 'ColorBall_Beam', 13, 26, $true)
