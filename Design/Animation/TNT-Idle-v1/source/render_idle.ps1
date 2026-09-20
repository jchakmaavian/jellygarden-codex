param([string]$Cs, [string]$Src, [string]$Patch, [string]$Out, [int]$From = 0, [int]$To = 119, [double]$Kc = 0.84)
$ErrorActionPreference = 'Stop'
Add-Type -Path $Cs -ReferencedAssemblies System.Drawing
$null = [TntRig]::Setup($Src, "", $Patch)
[TntRig]::Kc = $Kc
$sw = [Diagnostics.Stopwatch]::StartNew()
[TntRig]::RenderIdle($Out, $From, $To)
"render ms: " + $sw.ElapsedMilliseconds
