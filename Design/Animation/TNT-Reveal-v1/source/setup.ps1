param([string]$Cs, [string]$Src, [string]$Debug, [string]$Patch)
$ErrorActionPreference = 'Stop'
Add-Type -Path $Cs -ReferencedAssemblies System.Drawing
$sw = [Diagnostics.Stopwatch]::StartNew()
[TntRig]::Setup($Src, $Debug, $Patch)
"setup ms: " + $sw.ElapsedMilliseconds
