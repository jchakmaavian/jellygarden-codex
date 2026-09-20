param([string]$Rig, [string]$Chk, [string]$Dir, [string]$Prefix = "TNT_Idle")
$ErrorActionPreference = 'Stop'
Add-Type -Path @($Rig, $Chk) -ReferencedAssemblies System.Drawing
[IdleCheck]::Run($Dir, $Prefix, 120)
