#requires -Version 7.0
[CmdletBinding()]
param(
    [string]$RspDirectory = 'Library/Bee/artifacts/1300b0aE.dag',
    [switch]$PrepareOnly
)
$ErrorActionPreference = 'Stop'
$project = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
$git = (Get-Command git -ErrorAction Stop).Source
$dotnet = 'C:/Program Files/Unity/Hub/Editor/6000.5.10f1/Editor/Data/DotNetSdk/dotnet.exe'
$compiler = 'C:/Program Files/Unity/Hub/Editor/6000.5.10f1/Editor/Data/DotNetSdk/sdk/8.0.318/Roslyn/bincore/csc.dll'
$utf8 = [Text.UTF8Encoding]::new($false)

function Invoke-IndexGit {
    param([string[]]$GitArgs, [AllowNull()][string]$InputText = $null)
    $start = [Diagnostics.ProcessStartInfo]::new()
    $start.FileName = $git
    $start.WorkingDirectory = $project
    $start.UseShellExecute = $false
    $start.CreateNoWindow = $true
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    $start.RedirectStandardInput = $true
    $start.StandardOutputEncoding = $utf8
    $start.StandardErrorEncoding = $utf8
    $start.StandardInputEncoding = $utf8
    foreach ($arg in @('-c', "safe.directory=$project") + $GitArgs) { $start.ArgumentList.Add($arg) }
    $process = [Diagnostics.Process]::new()
    $process.StartInfo = $start
    [void]$process.Start()
    $stdout = $process.StandardOutput.ReadToEndAsync()
    $stderr = $process.StandardError.ReadToEndAsync()
    if ($null -ne $InputText) { $process.StandardInput.Write($InputText) }
    $process.StandardInput.Close()
    $process.WaitForExit()
    $output = $stdout.GetAwaiter().GetResult()
    $errorText = $stderr.GetAwaiter().GetResult()
    if ($process.ExitCode -ne 0) { throw "git $($GitArgs[0]) failed: $errorText" }
    $process.Dispose()
    return $output
}

function TextHash([string]$Value) {
    return [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($utf8.GetBytes($Value))).ToLowerInvariant()
}

$repoRoot = (Invoke-IndexGit @('rev-parse', '--show-toplevel')).Trim()
if ([IO.Path]::GetFullPath($repoRoot) -ne [IO.Path]::GetFullPath($project)) {
    throw 'The helper must run from the JellyGarden-Codex repository, not a parent repository.'
}
if (-not [IO.Path]::IsPathRooted($RspDirectory)) { $RspDirectory = Join-Path $project $RspDirectory }
$rspRoot = (Resolve-Path -LiteralPath $RspDirectory).Path
$indexText = Invoke-IndexGit @('ls-files', '--stage', '-z', '--', '*.cs')
$indexSources = [Collections.Generic.Dictionary[string, string]]::new([StringComparer]::Ordinal)
foreach ($record in $indexText.Split([char]0, [StringSplitOptions]::RemoveEmptyEntries)) {
    if ($record -notmatch '^\d+ ([0-9a-f]+) (\d+)\t(.+)$') { throw "Unexpected index record: $record" }
    if ($Matches[2] -ne '0') { throw "Unmerged C# source: $($Matches[3])" }
    $indexSources.Add($Matches[3], $Matches[1])
}
$sourceMap = [Collections.Generic.Dictionary[string, string]]::new([StringComparer]::Ordinal)
$assemblySources = @{}
$assemblyOptions = @{}
$omitted = [Collections.Generic.List[string]]::new()
foreach ($assembly in @('Assembly-CSharp', 'Assembly-CSharp-Editor')) {
    $sources = [Collections.Generic.List[string]]::new()
    $options = [Collections.Generic.List[string]]::new()
    foreach ($line in Get-Content -LiteralPath (Join-Path $rspRoot "$assembly.rsp")) {
        $trimmed = $line.Trim()
        if ($trimmed -match '^"?([^"\r\n]+\.cs)"?$' -and $trimmed -notmatch '^[-/]') {
            $source = $Matches[1].Replace('\', '/')
            if ($indexSources.ContainsKey($source)) {
                if ($sourceMap.ContainsKey($source)) { throw "Source belongs to two assemblies: $source" }
                $sourceMap.Add($source, $assembly)
                $sources.Add($source)
            } else { $omitted.Add($source) }
        } elseif ($trimmed -notmatch '^[-/](out|refout|analyzer|additionalfile|embed):') {
            $options.Add($line)
        }
    }
    $assemblySources[$assembly] = $sources
    $assemblyOptions[$assembly] = $options
}

# Do not silently omit a newly staged source if Unity has not incorporated it into
# the current response files. This check intentionally does not assign assemblies
# heuristically from folder names.
$changedCs = (Invoke-IndexGit @('diff', '--cached', '--name-only', '-z', '--diff-filter=ACMR', '--', '*.cs')).Split([char]0, [StringSplitOptions]::RemoveEmptyEntries)
foreach ($path in $changedCs) {
    if ($path.StartsWith('Assets/') -and -not $sourceMap.ContainsKey($path)) {
        throw "Staged C# file is not in either current default-assembly RSP: $path. Refresh Unity or use the correct RSP directory; do not claim this snapshot compiled."
    }
}

$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$run = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot ('StagedSnapshot-' + $stamp + '-' + [Guid]::NewGuid().ToString('N').Substring(0, 6))))
$allowed = [IO.Path]::GetFullPath($PSScriptRoot).TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar
if (-not $run.StartsWith($allowed, [StringComparison]::OrdinalIgnoreCase)) { throw 'Snapshot must remain inside Compile.' }
$snapshot = Join-Path $run 'Sources'
[void][IO.Directory]::CreateDirectory($snapshot)
$paths = @($sourceMap.Keys | Sort-Object)
$inputPaths = ($paths -join [char]0) + [char]0
# checkout-index only writes the new snapshot path, and reads exact index blobs.
# It does not stage files or touch the working source tree.
[void](Invoke-IndexGit @('checkout-index', ('--prefix=' + $snapshot.Replace('\', '/') + '/'), '-z', '--stdin') $inputPaths)

$report = [ordered]@{
    utc = [DateTime]::UtcNow.ToString('o')
    head = (Invoke-IndexGit @('rev-parse', 'HEAD')).Trim()
    indexSourceFingerprint = TextHash $indexText
    status = 'PreparedOnly'
    sourceSnapshot = $snapshot
    rspDirectory = $rspRoot
    boundary = 'Default runtime and Editor C# sources are exact Git index blobs. Unity SDK/reference assemblies and defines come from the current Editor RSP. No assets, serialization, runtime behavior, other custom assemblies or staged ProjectSettings are validated by this compile.'
    omittedUntrackedSources = @($omitted)
    sources = @($paths | ForEach-Object { [ordered]@{ path = $_; assembly = $sourceMap[$_]; gitBlob = $indexSources[$_] } })
    assemblies = @()
}
$reportPath = Join-Path $run 'staged-compile-report.json'
try {
    if (-not $PrepareOnly) {
        foreach ($assembly in @('Assembly-CSharp', 'Assembly-CSharp-Editor')) {
            $rsp = [Collections.Generic.List[string]]::new()
            foreach ($option in $assemblyOptions[$assembly]) {
                if ($assembly -eq 'Assembly-CSharp-Editor' -and $option -match '^[-/](r|reference):"?[^"\r\n]*[/\\]Assembly-CSharp(?:\.ref)?\.dll"?$') {
                    $rsp.Add('-r:"' + (Join-Path $run 'Assembly-CSharp.ref.dll') + '"')
                } else { $rsp.Add($option) }
            }
            foreach ($source in $assemblySources[$assembly]) { $rsp.Add('"' + (Join-Path $snapshot $source) + '"') }
            $rsp.Add('-out:"' + (Join-Path $run "$assembly.dll") + '"')
            $rsp.Add('-refout:"' + (Join-Path $run "$assembly.ref.dll") + '"')
            $rspPath = Join-Path $run "$assembly.rsp"
            [IO.File]::WriteAllLines($rspPath, $rsp, $utf8)
            Push-Location $project
            try {
                & $dotnet $compiler ('@' + $rspPath) 2>&1 | Tee-Object -FilePath (Join-Path $run "$assembly.log")
                $code = $LASTEXITCODE
            } finally { Pop-Location }
            $report.assemblies += [ordered]@{ name = $assembly; sourceCount = $assemblySources[$assembly].Count; exitCode = $code }
            if ($code -ne 0) { throw "$assembly failed to compile from the Git index." }
        }
        $report.status = 'Passed'
    }
    $after = Invoke-IndexGit @('ls-files', '--stage', '-z', '--', '*.cs')
    if ($after -cne $indexText) { throw 'The source index changed during this check. Restage and rerun before reporting a pass.' }
} catch {
    $report.status = 'Failed'
    $report.error = $_.Exception.Message
    throw
} finally {
    [IO.File]::WriteAllText($reportPath, ($report | ConvertTo-Json -Depth 8), $utf8)
    Write-Output "Staged compile report: $reportPath"
}
