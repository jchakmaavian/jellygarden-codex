[CmdletBinding()]
param(
    [string]$PaintedBoard,
    [switch]$PreserveSourceCoverage
)

# Default: read-only plan. With -PaintedBoard, repack only Ağız/cell20 into
# review outputs under this task's FaceRedesign folder. Never writes Assets.
# The source atlas snapshots are immutable and their hashes are checked first.
$ErrorActionPreference='Stop'
$taskRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$projectRoot=[IO.Path]::GetFullPath((Join-Path $taskRoot '..\..'))
$referenceRoot=Join-Path $projectRoot 'Design/KingVanguard-Reference-20260929'
$utf8=New-Object Text.UTF8Encoding($false,$true)
$manifestPath=Join-Path $taskRoot 'Before/atlas-hashes.json'
$manifest=Get-Content -LiteralPath $manifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
$layoutSource=Join-Path $referenceRoot 'FaceRedesign/Portrait/Portrait-layout.json'
$layout=Get-Content -LiteralPath $layoutSource -Raw -Encoding UTF8 | ConvertFrom-Json
$mouthName='A'+[char]0x011f+[char]0x0131+'z'
$mouth=@($layout.cells | Where-Object {$_.name -eq $mouthName})
if($mouth.Count -ne 1 -or $mouth[0].index -ne 20){throw 'Expected exactly portrait cell20 / Ağız.'}
$layout.cells=$mouth
$layout.name='Portrait-mouth-only'
$outputRoot=Join-Path $taskRoot 'FaceRedesign/Packed'

$jobs=@(
    [pscustomobject]@{name='King_Vanguard';source='Original-rfv387/King.png';atlas='Original-rfv387/King.atlas'},
    [pscustomobject]@{name='AreaCompleted_Vanguard';source='Original-rfv387/AreaCompleted.png';atlas='Original-rfv387/AreaCompleted.atlas'}
)
foreach($job in $jobs){
    $entry=@($manifest.files | Where-Object {$_.name -eq $job.name})
    if($entry.Count -ne 1){throw ('Missing immutable atlas snapshot manifest entry: '+$job.name)}
    $snapshot=Join-Path $projectRoot $entry[0].snapshot
    if((Get-FileHash -LiteralPath $snapshot -Algorithm SHA256).Hash.ToLowerInvariant() -ne $entry[0].sha256){throw ('Snapshot hash mismatch: '+$snapshot)}
    # Detect another atlas edit before this narrow patch is made, instead of overwriting it later.
    $installed=Join-Path $projectRoot $entry[0].source
    if((Get-FileHash -LiteralPath $installed -Algorithm SHA256).Hash.ToLowerInvariant() -ne $entry[0].sha256){throw ('Installed atlas changed since baseline; reconcile it before packing: '+$installed)}
}

if([string]::IsNullOrWhiteSpace($PaintedBoard)){
    [pscustomobject]@{
        mode='plan_only'; sourceLayout=$layoutSource; cell=20; region=$mouthName
        snapshotManifest=$manifestPath; outputDirectory=$outputRoot
        rigs=@($jobs.name); sourceFacialRgbFallback=$false
        otherFaceAndOutfitPixels='Exact RGBA preservation outside the source mouth UV island'
        updatesAssets=$false
        invocation='powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Pack-MouthOnly.ps1 -PaintedBoard <corrected full portrait workboard.png>'
    } | ConvertTo-Json -Depth 4
    return
}

$paintPath=[IO.Path]::GetFullPath($PaintedBoard)
if(-not (Test-Path -LiteralPath $paintPath -PathType Leaf)){throw ('Corrected workboard is missing: '+$paintPath)}
$codePath=Join-Path $referenceRoot 'FaceRedesign/Tools/FaceWorkboard.cs'
$codeHash=(Get-FileHash -LiteralPath $codePath -Algorithm SHA256).Hash.ToLowerInvariant()
$typeName='MouthOnlyPacker_'+$codeHash.Substring(0,16)
$packerType=$typeName -as [type]
if($null -eq $packerType){
    $code=[IO.File]::ReadAllText($codePath,$utf8).Replace('public static class FaceWorkboard','public static class '+$typeName)
    Add-Type -TypeDefinition $code -ReferencedAssemblies System.Drawing,System.Web.Extensions,System.Core
    $packerType=$typeName -as [type]
}
[IO.Directory]::CreateDirectory($outputRoot)|Out-Null
$layoutPath=Join-Path $taskRoot 'FaceRedesign/MouthOnly-layout.json'
[IO.File]::WriteAllText($layoutPath,($layout|ConvertTo-Json -Depth 12),$utf8)
$results=@()
foreach($job in $jobs){
    $entry=$manifest.files | Where-Object {$_.name -eq $job.name}
    $snapshot=Join-Path $projectRoot $entry.snapshot
    $output=Join-Path $outputRoot ($job.name+'.png')
    $report=$packerType::Pack($layoutPath,$paintPath,$snapshot,(Join-Path $referenceRoot $job.source),(Join-Path $referenceRoot $job.atlas),$output,$false,$PreserveSourceCoverage.IsPresent) | ConvertFrom-Json
    if($report.regions.Count -ne 1 -or $report.regions[0].name -ne $mouthName){throw 'Packer touched a region outside the mouth-only whitelist.'}
    if($report.sourceRgbFallbackPixels -ne 0 -or $report.unrelatedAtlasPixelsChanged -ne 0 -or $report.savedPngRgbaByteDifferences -ne 0){throw 'Mouth-only pixel preservation verification failed.'}
    if($report.regions[0].changedRgbPixels -eq 0){throw 'Corrected mouth produced no RGB change; check the supplied workboard.'}
    $results+=[pscustomobject]@{
        name=$job.name; baselineSha256=$entry.sha256; outputSha256=$report.outputSha256
        region=$mouthName; changedMouthRgbPixels=$report.regions[0].changedRgbPixels
        otherAtlasPixelsChanged=$report.unrelatedAtlasPixelsChanged
        originalFacialRgbFallback=$report.sourceRgbFallbackPixels
        savedPngByteDifferences=$report.savedPngRgbaByteDifferences
        output=$output; report=[IO.Path]::ChangeExtension($output,'.pack-report.json')
    }
}
$delivery=[pscustomobject]@{
    generatedUtc=[DateTime]::UtcNow.ToString('o'); sourceWorkboard=$paintPath
    sourceWorkboardSha256=(Get-FileHash -LiteralPath $paintPath -Algorithm SHA256).Hash.ToLowerInvariant()
    packerSha256=$codeHash; whitelist=@($mouthName); cell=20
    baselineManifest=$manifestPath; assetsModified=$false; results=$results
}
[IO.File]::WriteAllText((Join-Path $taskRoot 'FaceRedesign/mouth-only-validation.json'),($delivery|ConvertTo-Json -Depth 6),$utf8)
$delivery | ConvertTo-Json -Depth 6
