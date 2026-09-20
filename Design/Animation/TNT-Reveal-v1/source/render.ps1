param([string]$Cs, [string]$Src, [string]$Patch, [string]$Out, [int]$From = 0, [int]$To = 0, [switch]$Rest, [double]$Kc = 0.84, [string]$Flames = "")
$ErrorActionPreference = 'Stop'
Add-Type -Path $Cs -ReferencedAssemblies System.Drawing
$null = [TntRig]::Setup($Src, "", $Patch)
[TntRig]::Kc = $Kc
if ($Flames) { [TntRig]::LoadFlames($Flames) }
$sw = [Diagnostics.Stopwatch]::StartNew()
if ($Rest) { [TntRig]::Render($Out, 0, 0, $true) } else { [TntRig]::Render($Out, $From, $To, $false) }
"render ms: " + $sw.ElapsedMilliseconds
