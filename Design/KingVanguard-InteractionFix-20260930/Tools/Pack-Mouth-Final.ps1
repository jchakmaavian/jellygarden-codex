param([Parameter(Mandatory=$true)][string]$GeneratedImage)
$ErrorActionPreference='Stop'
$taskRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$projectRoot=[IO.Path]::GetFullPath((Join-Path $taskRoot '..\..'))
$referenceRoot=Join-Path $projectRoot 'Design/KingVanguard-Reference-20260929'
Copy-Item -LiteralPath $GeneratedImage -Destination (Join-Path $taskRoot 'FaceRedesign/Mouth-natural-peach.png') -Force
$source=[IO.File]::ReadAllText((Join-Path $referenceRoot 'FaceRedesign/Tools/FaceWorkboard.cs'),[Text.Encoding]::UTF8)+[Environment]::NewLine+[IO.File]::ReadAllText((Join-Path $PSScriptRoot 'Pack-Mouth-Final.cs'),[Text.Encoding]::UTF8)
Add-Type -TypeDefinition $source -ReferencedAssemblies System.Drawing,System.Web.Extensions,System.Core
$reports=@()
foreach($rig in @('King','AreaCompleted')){$reports+=[KingVanguardTechnical.NaturalMouthPacker]::Run($taskRoot,$referenceRoot,$rig)|ConvertFrom-Json}
@{generatedUtc=[DateTime]::UtcNow.ToString('o');runtimeAssetsModified=$false;reports=$reports}|ConvertTo-Json -Depth 10 | Set-Content -Encoding UTF8 -LiteralPath (Join-Path $taskRoot 'FaceRedesign/PackedFinal/mouth-manifest.json')
$reports|Select-Object output,outputSha256,changedRgbPixels,outsideMouthIslandChanged,baselineAlphaByteDifferences,originalSourceAlphaDifferences,savedPngByteDifferences|ConvertTo-Json -Depth 4
