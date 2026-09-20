param([string]$Dir, [string]$Out, [string]$Rerender = '')
$ErrorActionPreference = 'Stop'
Add-Type -Path (Join-Path $Dir 'review_ColorBall_StarSpark.cs') -ReferencedAssemblies System.Drawing
[ReviewStarSpark]::Run((Join-Path $Out 'White\frames'), 'White', 22, $true, 64.0, 64.0)
[ReviewStarSpark]::Run((Join-Path $Out 'Gold\frames'), 'Gold', 22, $true, 64.0, 64.0)
[ReviewStarSpark]::Run((Join-Path $Out 'Static\frames'), 'Static', 2, $false, 64.0, 64.0)
$s1 = Join-Path $Out 'Static\frames\ColorBall_StarSpark_001.png'
$s2 = Join-Path $Out 'Static\frames\ColorBall_StarSpark_002.png'
[ReviewStarSpark]::Profile($s1, 0, 64.0, 64.0)
[ReviewStarSpark]::Profile($s2, 0, 64.0, 64.0)
[ReviewStarSpark]::Profile($s1, 45, 64.0, 64.0)
[ReviewStarSpark]::ArmWidth($s1, 64.0, 64.0, 32)
[ReviewStarSpark]::ArmWidth($s1, 64.0, 64.0, 128)
if ($Rerender -ne '') {
  foreach ($clip in @('White', 'Gold', 'Static')) {
    [ReviewStarSpark]::Compare((Join-Path $Out "$clip\frames"), (Join-Path $Rerender "$clip\frames"))
  }
}
