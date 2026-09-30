# Re-register the four approved paints against the original APK alpha masks.
# Output is intentionally confined to SurfacePaint/Registered-final for review.
# Run with Windows PowerShell (System.Drawing / GDI+):
# powershell -NoProfile -ExecutionPolicy Bypass -File .\PackAll.ps1
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$taskReferenceRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$taskPackerPath = Join-Path $PSScriptRoot 'PackPaintedAtlas.cs'
$taskOutputRoot = [IO.Path]::GetFullPath((Join-Path $taskReferenceRoot 'SurfacePaint/Registered-final'))
$taskAllowedRoot = [IO.Path]::GetFullPath((Join-Path $taskReferenceRoot 'SurfacePaint')) + [IO.Path]::DirectorySeparatorChar
if (-not $taskOutputRoot.StartsWith($taskAllowedRoot, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'The registered-atlas destination must stay inside the reference SurfacePaint directory.'
}
[IO.Directory]::CreateDirectory($taskOutputRoot) | Out-Null

# Explicit UTF-8 preserves the Turkish crown and moustache atlas names. A hash
# suffix prevents a repeated invocation from reusing stale compiled packer code.
$taskUtf8 = New-Object Text.UTF8Encoding($false, $true)
$taskPackerCode = [IO.File]::ReadAllText($taskPackerPath, $taskUtf8)
$taskCrownName = 'Ta' + [char]0x00E7
$taskMoustacheName = 'B' + [char]0x0131 + 'y' + [char]0x0131 + 'k'
if (-not $taskPackerCode.Contains($taskCrownName) -or -not $taskPackerCode.Contains($taskMoustacheName)) {
    throw 'The packer source lost its UTF-8 crown or moustache region name.'
}
$taskCodeHash = (Get-FileHash -LiteralPath $taskPackerPath -Algorithm SHA256).Hash.ToLowerInvariant()
$taskTypeName = 'PackPaintedAtlas_' + $taskCodeHash.Substring(0, 16)
$taskPackerType = $taskTypeName -as [type]
if ($null -eq $taskPackerType) {
    $taskCompiledCode = $taskPackerCode.Replace('public static class PackPaintedAtlas', 'public static class ' + $taskTypeName)
    Add-Type -TypeDefinition $taskCompiledCode -ReferencedAssemblies System.Drawing -Language CSharp
    $taskPackerType = $taskTypeName -as [type]
}

$taskJobs = @(
    [pscustomobject]@{ Name='King_Vanguard'; Source='Original-rfv387/King.png'; Paint='SurfacePaint/King-Vanguard-paint.png'; Atlas='Original-rfv387/King.atlas' },
    [pscustomobject]@{ Name='AreaCompleted_Vanguard'; Source='Original-rfv387/AreaCompleted.png'; Paint='SurfacePaint/AreaCompleted-Vanguard-paint.png'; Atlas='Original-rfv387/AreaCompleted.atlas' },
    [pscustomobject]@{ Name='Nightmare_Vanguard'; Source='Original-rfv387/nightmare_popup.png'; Paint='SurfacePaint/Nightmare-Vanguard-paint.png'; Atlas='Original-rfv387/nightmare_popup.atlas' },
    [pscustomobject]@{ Name='KingDrill_Vanguard'; Source='EmbeddedKingBundles/Recovered/kingdrill_v3/Textures/sactx-0-1024x2048-ETC2-KingDrillAtlas-89d7723c__4934485327719105542.png'; Paint='SurfacePaint/KingDrill-Vanguard-paint.png'; Atlas='SurfacePaint/KingDrill-technical.atlas' }
)
$taskResults = @()
foreach ($taskJob in $taskJobs) {
    $taskSource = Join-Path $taskReferenceRoot $taskJob.Source
    $taskPaint = Join-Path $taskReferenceRoot $taskJob.Paint
    $taskAtlas = Join-Path $taskReferenceRoot $taskJob.Atlas
    foreach ($taskInput in @($taskSource, $taskPaint, $taskAtlas)) {
        if (-not [IO.File]::Exists($taskInput)) { throw ('Required input is missing: ' + $taskInput) }
    }
    $taskAtlasText = [IO.File]::ReadAllText($taskAtlas, $taskUtf8)
    if ($taskJob.Name -in @('King_Vanguard', 'AreaCompleted_Vanguard') -and -not $taskAtlasText.Contains($taskCrownName)) {
        throw ('The atlas crown name is not valid UTF-8: ' + $taskAtlas)
    }
    $taskOutput = Join-Path $taskOutputRoot ($taskJob.Name + '.png')
    $taskPackReport = $taskPackerType::Pack([string]$taskSource, [string]$taskPaint, [string]$taskAtlas, [string]$taskOutput)
    [IO.File]::WriteAllText((Join-Path $taskOutputRoot ($taskJob.Name + '-pack-report.txt')), $taskPackReport, $taskUtf8)
    $taskAlpha = $taskPackerType::VerifyAlpha([string]$taskSource, [string]$taskOutput) | ConvertFrom-Json
    if ($taskAlpha.unexpected_alpha_differences -ne 0 -or $taskAlpha.added_alpha_coverage_pixels -ne 0) {
        throw ('Source alpha registration failed for ' + $taskJob.Name + ': ' + ($taskAlpha | ConvertTo-Json -Compress))
    }
    $taskResult = [pscustomobject]@{
        name = $taskJob.Name
        source = $taskJob.Source
        source_sha256 = (Get-FileHash -LiteralPath $taskSource -Algorithm SHA256).Hash.ToLowerInvariant()
        paint = $taskJob.Paint
        paint_sha256 = (Get-FileHash -LiteralPath $taskPaint -Algorithm SHA256).Hash.ToLowerInvariant()
        atlas = $taskJob.Atlas
        atlas_sha256 = (Get-FileHash -LiteralPath $taskAtlas -Algorithm SHA256).Hash.ToLowerInvariant()
        output = 'SurfacePaint/Registered-final/' + $taskJob.Name + '.png'
        output_sha256 = (Get-FileHash -LiteralPath $taskOutput -Algorithm SHA256).Hash.ToLowerInvariant()
        alpha = $taskAlpha
        pack_report = $taskPackReport
    }
    $taskResults += $taskResult
    Write-Output ($taskJob.Name + ': alpha registration passed; ' + $taskAlpha.original_alpha_below_4_cleaned_to_zero + ' original sub-4-alpha texels cleaned. ' + $taskOutput)
}
$taskManifest = [pscustomobject]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    packer_sha256 = $taskCodeHash
    alpha_reference = 'Original source decoded and registered to atlas-declared dimensions by the unchanged GDI+ resampling path.'
    matte_policy = 'Missing generated paint retains the original straightened source pixel and alpha.'
    facial_policy = 'The original face, beard and moustache remain registered to their source deformation meshes; only crown and costume islands receive generated surface paint.'
    preserved_facial_regions = @('Sakal Surat', $taskMoustacheName)
    assets = $taskResults
}
[IO.File]::WriteAllText((Join-Path $taskOutputRoot 'registration-manifest.json'), ($taskManifest | ConvertTo-Json -Depth 8), $taskUtf8)
Write-Output ('Wrote ' + $taskResults.Count + ' inspection atlases and registration-manifest.json. No Assets files were changed.')
