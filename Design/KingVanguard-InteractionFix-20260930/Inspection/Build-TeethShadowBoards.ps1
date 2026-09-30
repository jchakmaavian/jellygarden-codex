$ErrorActionPreference='Stop'
$inspectionRoot=[IO.Path]::GetFullPath($PSScriptRoot)
$projectRoot=[IO.Path]::GetFullPath((Join-Path $inspectionRoot '..\..\..'))
$referenceRoot=Join-Path $projectRoot 'Design/KingVanguard-Reference-20260929'
$toolSource=Join-Path $referenceRoot 'FaceRedesign/Tools/FaceWorkboard.cs'
Add-Type -TypeDefinition ([IO.File]::ReadAllText($toolSource,[Text.Encoding]::UTF8)) -ReferencedAssemblies System.Drawing,System.Web.Extensions,System.Core
$originalLayout=Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $referenceRoot 'FaceRedesign/Portrait/Portrait-layout.json') | ConvertFrom-Json
$originalCellIds=@(19,21,22,24,26,7,23)
[string[]]$regionNames=@($originalCellIds | ForEach-Object { $wanted=$_; ($originalLayout.cells | Where-Object index -eq $wanted).name })
$atlas=Join-Path $referenceRoot 'Original-rfv387/King.atlas'
$sourceOutput=Join-Path $inspectionRoot 'FaceRedesign/Source'
$currentOutput=Join-Path $inspectionRoot 'FaceRedesign/Current'
$sourceLayout=[FaceWorkboard]::Build((Join-Path $referenceRoot 'Original-rfv387/King.png'),$atlas,$regionNames,$sourceOutput,'TeethShadow',$true,2,384)
$currentLayout=[FaceWorkboard]::Build((Join-Path $projectRoot 'Assets/Characters/KingVenguard/V2/Spine/King_Vanguard.png'),$atlas,$regionNames,$currentOutput,'TeethShadowCurrent',$false,2,384)
@{ sourceLayout=$sourceLayout; currentLayout=$currentLayout; sourceCellIds=$originalCellIds; runtimeAssetsModified=$false; note='Source layout/masks are authoritative registration. Current board displays stored atlas art, including shadows suppressed by current runtime configuration.' } | ConvertTo-Json -Depth 4 | Set-Content -Encoding UTF8 -LiteralPath (Join-Path $inspectionRoot 'teeth-shadow-boards.json')
Write-Output $sourceLayout
Write-Output $currentLayout
