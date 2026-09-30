$ErrorActionPreference='Stop'
$referenceRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$code=[IO.File]::ReadAllText((Join-Path $PSScriptRoot 'FaceWorkboard.cs'),[Text.Encoding]::UTF8)
Add-Type -TypeDefinition $code -ReferencedAssemblies System.Drawing,System.Web.Extensions,System.Core
$registration=Get-Content -LiteralPath (Join-Path $referenceRoot 'facial-rig-registration.json') -Raw -Encoding UTF8 | ConvertFrom-Json
# Put the largest identity features first; preserve every one of the 27 shared source regions.
$priority=@('Sakal Surat','Burun',('B'+[char]0x0131+'y'+[char]0x0131+'k'),('Ta'+[char]0x00e7))
$names=@($priority)+@($registration.shared_regions.name | Where-Object { $priority -notcontains $_ })
if($names.Count -ne 27){throw 'Expected exactly 27 shared portrait/area regions.'}
$result=[FaceWorkboard]::Build((Join-Path $referenceRoot 'Original-rfv387/King.png'),(Join-Path $referenceRoot 'Original-rfv387/King.atlas'),[string[]]$names,(Join-Path $referenceRoot 'FaceRedesign/Portrait'),'Portrait',$true,5,256)
Write-Output $result
