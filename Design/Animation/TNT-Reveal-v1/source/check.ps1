param([string]$Rig, [string]$Chk, [string]$Dir, [string]$Src, [double]$Kc = 0.84)
$ErrorActionPreference = 'Stop'
Add-Type -Path @($Rig, $Chk) -ReferencedAssemblies System.Drawing
[SeqCheck]::Run($Dir, 120, $Src, $Kc)
