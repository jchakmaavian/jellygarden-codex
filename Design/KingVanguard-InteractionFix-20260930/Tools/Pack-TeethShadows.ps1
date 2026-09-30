[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$PaintedBoard)
$ErrorActionPreference='Stop'
$taskRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$projectRoot=[IO.Path]::GetFullPath((Join-Path $taskRoot '..\..'))
$referenceRoot=Join-Path $projectRoot 'Design/KingVanguard-Reference-20260929'
$beforeRoot=Join-Path $taskRoot 'Before/Atlases'
$paintRoot=Join-Path $taskRoot 'FaceRedesign'
$packedRoot=Join-Path $paintRoot 'Packed'
New-Item -ItemType Directory -Force -Path $beforeRoot,$paintRoot,$packedRoot | Out-Null
$records=@()
foreach($rig in @('King','AreaCompleted')){
    $name=$rig+'_Vanguard.png'
    $source=Join-Path $projectRoot ('Assets/Characters/KingVenguard/V2/Spine/'+$name)
    $snapshot=Join-Path $beforeRoot $name
    $hash=(Get-FileHash -Algorithm SHA256 -LiteralPath $source).Hash.ToLowerInvariant()
    if(Test-Path -LiteralPath $snapshot){
        if((Get-FileHash -Algorithm SHA256 -LiteralPath $snapshot).Hash.ToLowerInvariant() -ne $hash){throw ('Existing baseline differs from current atlas: '+$name)}
    }else{Copy-Item -LiteralPath $source -Destination $snapshot}
    $records+=@{name=$name;source=$source;snapshot=$snapshot;sha256=$hash;bytes=(Get-Item -LiteralPath $source).Length}
}
@{createdUtc=[DateTime]::UtcNow.ToString('o');files=$records;runtimeAssetsModified=$false} | ConvertTo-Json -Depth 6 | Set-Content -Encoding UTF8 -LiteralPath (Join-Path $beforeRoot 'atlas-hashes.json')
$board=Join-Path $paintRoot 'TeethShadow-painted-workboard.png'
if([IO.Path]::GetFullPath($PaintedBoard) -ne [IO.Path]::GetFullPath($board)){Copy-Item -LiteralPath $PaintedBoard -Destination $board -Force}
# Compile the helper alongside FaceWorkboard because dynamic Add-Type assemblies
# cannot be referenced reliably by filename on Windows PowerShell.
$combined=[IO.File]::ReadAllText((Join-Path $referenceRoot 'FaceRedesign/Tools/FaceWorkboard.cs'),[Text.Encoding]::UTF8)+[Environment]::NewLine+[IO.File]::ReadAllText((Join-Path $PSScriptRoot 'Pack-TeethShadows.cs'),[Text.Encoding]::UTF8)
$compiled=Add-Type -TypeDefinition $combined -ReferencedAssemblies System.Drawing,System.Web.Extensions,System.Core -PassThru
$layout=Join-Path $taskRoot 'Inspection/FaceRedesign/Source/TeethShadow-layout.json'
$fullLayout=Join-Path $referenceRoot 'FaceRedesign/Portrait/Portrait-layout.json'
$reports=@()
foreach($rig in @('King','AreaCompleted')){
    $base=Join-Path $beforeRoot ($rig+'_Vanguard.png')
    $mask=Join-Path $referenceRoot ('Original-rfv387/'+$rig+'.png')
    $atlas=Join-Path $referenceRoot ('Original-rfv387/'+$rig+'.atlas')
    $output=Join-Path $packedRoot ($rig+'_Vanguard.png')
    $report=[KingVanguardTechnical.TeethShadowPack]::Pack([string]$layout,[string]$fullLayout,[string]$board,[string]$base,[string]$mask,[string]$atlas,[string]$output) | ConvertFrom-Json
    $reports+=$report
}
foreach($record in $records){if((Get-FileHash -Algorithm SHA256 -LiteralPath $record.source).Hash.ToLowerInvariant() -ne $record.sha256){throw ('Runtime atlas changed while packing: '+$record.name)}}
@{generatedUtc=[DateTime]::UtcNow.ToString('o');paintedBoard=$board;paintedBoardSha256=(Get-FileHash -Algorithm SHA256 -LiteralPath $board).Hash.ToLowerInvariant();runtimeAssetsModified=$false;results=$reports} | ConvertTo-Json -Depth 12 | Set-Content -Encoding UTF8 -LiteralPath (Join-Path $paintRoot 'teeth-shadow-pack-manifest.json')
$reports | Select-Object output,outputSha256,changedPixels,outsideSelectedIslandsChanged,sourceAlphaDifferences,savedPngByteDifferences,unselectedMouthChanged | ConvertTo-Json -Depth 4
