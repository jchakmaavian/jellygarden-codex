[CmdletBinding()]
param([switch]$Finalize)

# Run without -Finalize to inspect the plan. This script never stages, commits, pushes,
# imports into Unity, or changes a source asset. Run -Finalize only after final verification.
$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..\..'))
$referencePath = 'Design/KingVanguard-Reference-20260929'
$deliveryPath = "$referencePath/Delivery"
$v2Path = 'Assets/Characters/KingVenguard/V2'
$hostFiles = @(
    'Assets/JellyGarden/Scripts/GUI/AnimationManager.cs',
    'Assets/JellyGarden/Scripts/GUI/RoyalAreaView.cs',
    'Assets/JellyGarden/Scripts/GUI/RoyalGameplayPresentation.cs',
    'Assets/JellyGarden/Scripts/GUI/RoyalHomeScreen.cs',
    'Assets/JellyGarden/Scripts/GUI/RoyalMenuSkin.cs',
    'Assets/JellyGarden/Scripts/GUI/RoyalCards.cs',
    'Assets/JellyGarden/Scripts/GUI/RoyalLeaderboardPage.cs',
    'Assets/JellyGarden/Scripts/LevelManager.cs'
)
$parentMetas = @('Assets/Characters.meta', 'Assets/Characters/KingVenguard.meta', "$v2Path.meta")
$baselineCaveat = 'This is a recovery overlay of eight complete current host files and their metas, including preexisting work. It does not isolate only this task edits or recreate the current working baseline from HEAD. RoyalCardKit.cs is a preexisting untracked dependency; RoyalGameplayHud.cs has preexisting changes. Restore onto the matching project baseline after comparing hashes; do not blindly overwrite newer work.'

function ProjectFile([string]$relative) {
    $path = [IO.Path]::GetFullPath((Join-Path $projectRoot $relative))
    if (-not $path.StartsWith($projectRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Path escapes the project: $relative"
    }
    return $path
}

function GitOutput([string[]]$arguments) {
    $result = @(& git -C $projectRoot @arguments)
    if ($LASTEXITCODE -ne 0) { throw "git failed: $($arguments -join ' ')" }
    return $result
}

function RelativePath([string]$absolute) {
    return $absolute.Substring($projectRoot.Length + 1).Replace('\', '/')
}

function ExcludedFromTaskCommit([string]$relative) {
    # Preserve raw APK reference data (.assets/.resS/.skel/.atlas/.unitybin/.so)
    # and reproducible tool sources/reports, not offline compiler executables or caches.
    return $relative -match '(^|/)__pycache__(/|$)|\.(pyc|pyo|pending|exe|dll|pdb|mdb|rsp|obj|o|ilk|exp|cache|tmp|log)(\.meta)?$'
}

if (-not (Test-Path -LiteralPath (ProjectFile 'AGENTS.md'))) { throw 'Expected independent project AGENTS.md.' }
$head = (GitOutput @('rev-parse', 'HEAD')) -join ''
$branch = (GitOutput @('branch', '--show-current')) -join ''
$spineFiles = @(GitOutput @('ls-files', '--', 'Assets/Spine'))
$spineStatus = @(GitOutput @('status', '--short', '--', 'Assets/Spine'))
if ($spineFiles.Count -eq 0 -or $spineStatus.Count -ne 0) {
    throw 'Spine is no longer tracked and clean; reassess baseline backup before finalizing.'
}

$snapshotPaths = @($hostFiles | ForEach-Object { $_; "$_.meta" })
foreach ($relative in @($snapshotPaths) + @($parentMetas)) {
    if (-not (Test-Path -LiteralPath (ProjectFile $relative) -PathType Leaf)) { throw "Missing required file: $relative" }
}
$directPaths = @($parentMetas) + @(Get-ChildItem -LiteralPath (ProjectFile $v2Path) -File -Recurse | ForEach-Object { RelativePath $_.FullName })
$directPaths += @(Get-ChildItem -LiteralPath (ProjectFile $referencePath) -File -Recurse | ForEach-Object { RelativePath $_.FullName } | Where-Object { -not (ExcludedFromTaskCommit $_) })
$directPaths = @($directPaths | Where-Object { -not (ExcludedFromTaskCommit $_) } | Sort-Object -Unique)
if (@($directPaths | Where-Object { $_ -like 'Assets/Characters/KingVenguard/V1/*' -or $hostFiles -contains $_ }).Count) {
    throw 'Task staging plan unexpectedly contains V1 or a live host source file.'
}
$largeFiles = @($directPaths | Where-Object { (Get-Item -LiteralPath (ProjectFile $_)).Length -ge 99MB })
if ($largeFiles.Count) { throw "Files exceed the conservative direct-Git size limit: $($largeFiles -join ', ')" }

$plan = [ordered]@{
    mode = $(if ($Finalize) { 'finalize' } else { 'plan_only' })
    project = $projectRoot
    baseCommit = $head
    branch = $branch
    hostSnapshotPaths = $snapshotPaths
    directTaskFileCount = $directPaths.Count
    directTaskBytes = ($directPaths | ForEach-Object { (Get-Item -LiteralPath (ProjectFile $_)).Length } | Measure-Object -Sum).Sum
    directTaskRoots = @($v2Path, $referencePath) + $parentMetas
    exclusions = @('__pycache__', '*.pyc', '*.pyo', '*.pending', '*.exe', '*.dll', '*.pdb', '*.mdb', '*.rsp', '*.obj', '*.o', '*.ilk', '*.exp', '*.cache', '*.tmp', '*.log', 'matching generated artifact metas', 'all V1 assets', 'live host-file diffs')
    spine = @{ trackedFileCount = $spineFiles.Count; status = 'clean'; archived = $false }
    selfContainedFromHead = $false
    baselineCaveat = $baselineCaveat
    changesGitIndex = $false
}
if (-not $Finalize) { $plan | ConvertTo-Json -Depth 6; return }

# Preserve complete bytes, not patches: several host files already contained unrelated work.
$deliveryRoot = ProjectFile $deliveryPath
[IO.Directory]::CreateDirectory($deliveryRoot) | Out-Null
$stamp = [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss')
$archiveRelative = "$deliveryPath/host-snapshots-$stamp.zip"
$manifestRelative = "$deliveryPath/host-snapshots-$stamp.manifest.json"
$pathsRelative = "$deliveryPath/task-stage-paths-$stamp.txt"
$archivePath = ProjectFile $archiveRelative
$manifestPath = ProjectFile $manifestRelative
$pathsPath = ProjectFile $pathsRelative
foreach ($destination in @($archivePath, $manifestPath, $pathsPath)) {
    if (Test-Path -LiteralPath $destination) { throw "Refusing to overwrite: $destination" }
}

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$records = @()
$archiveStream = [IO.File]::Open($archivePath, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
$archive = New-Object IO.Compression.ZipArchive($archiveStream, [IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($relative in $snapshotPaths) {
        $path = ProjectFile $relative
        $beforeHash = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()
        [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $path, $relative, [IO.Compression.CompressionLevel]::Optimal) | Out-Null
        $afterHash = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()
        if ($beforeHash -ne $afterHash) { throw "Source changed during snapshot: $relative. Discard this incomplete archive and retry after work stops." }
        $records += [ordered]@{ path = $relative; bytes = (Get-Item -LiteralPath $path).Length; sha256 = $beforeHash; trackedAtBase = @(GitOutput @('ls-files', '--', $relative)).Count -gt 0 }
    }
} finally { $archive.Dispose(); $archiveStream.Dispose() }

$verifyArchive = [IO.Compression.ZipFile]::OpenRead($archivePath)
try {
    foreach ($record in $records) {
        $entry = $verifyArchive.GetEntry($record.path)
        if (-not $entry) { throw "Archive entry missing: $($record.path)" }
        $stream = $entry.Open()
        $sha = [Security.Cryptography.SHA256]::Create()
        try { $hash = ([BitConverter]::ToString($sha.ComputeHash($stream))).Replace('-', '').ToLowerInvariant() }
        finally { $sha.Dispose(); $stream.Dispose() }
        if ($hash -ne $record.sha256) { throw "Archive SHA mismatch: $($record.path)" }
    }
} finally { $verifyArchive.Dispose() }

$manifest = [ordered]@{
    schemaVersion = 1
    createdUtc = [DateTime]::UtcNow.ToString('o')
    baseCommit = $head
    branch = $branch
    archive = $archiveRelative
    archiveSha256 = (Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash.ToLowerInvariant()
    verifiedArchiveEntries = $records.Count
    contentsAreWholeMixedWorkingFiles = $true
    selfContainedFromHead = $false
    baselineCaveat = $baselineCaveat
    spine = $plan.spine
    directTaskRoots = $plan.directTaskRoots
    exclusions = $plan.exclusions
    files = $records
}
$utf8 = New-Object Text.UTF8Encoding($false)
[IO.File]::WriteAllText($manifestPath, ($manifest | ConvertTo-Json -Depth 8) + "`n", $utf8)
$directPaths += @($archiveRelative, $manifestRelative, $pathsRelative)
# A reviewable allowlist only. The script intentionally does not run git add/commit/push.
[IO.File]::WriteAllLines($pathsPath, @($directPaths | Sort-Object -Unique), $utf8)
[pscustomobject]@{ archive = $archiveRelative; manifest = $manifestRelative; stagingAllowlist = $pathsRelative; verifiedEntries = $records.Count; gitIndexChanged = $false } | ConvertTo-Json
