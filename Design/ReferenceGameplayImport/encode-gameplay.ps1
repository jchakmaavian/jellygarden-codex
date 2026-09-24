#requires -Version 7.0
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$CaptureRoot
)
$ErrorActionPreference = 'Stop'
$ffmpeg = 'C:/Program Files/Softdeluxe/Free Download Manager/ffmpeg.exe'
$utf8 = [Text.UTF8Encoding]::new($false)
$sourceRoot = (Resolve-Path -LiteralPath $CaptureRoot).Path
$reportFile = Join-Path $sourceRoot 'capture-report.tsv'
if (-not (Test-Path -LiteralPath $reportFile -PathType Leaf)) {
    throw 'No completed capture-report.tsv exists. Wait for RoyalHandoffCapture to finish.'
}
if (-not (Test-Path -LiteralPath $ffmpeg -PathType Leaf)) { throw "Encoder not found: $ffmpeg" }
$rows = @(Import-Csv -LiteralPath $reportFile -Delimiter "`t")
$ok = @($rows | Where-Object { $_.status -eq 'ok' })
if ($ok.Count -eq 0) { throw 'The capture report contains no successful clips.' }
if (@($rows.clip | Group-Object | Where-Object Count -gt 1).Count) { throw 'Duplicate clip rows in capture report.' }

$deliveryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot 'Delivery'))
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$leaf = Split-Path -Leaf $sourceRoot
$output = [IO.Path]::GetFullPath((Join-Path $deliveryRoot ($leaf + '-Encoded-' + $stamp)))
if (-not $output.StartsWith($deliveryRoot.TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Delivery output must remain inside ReferenceGameplayImport/Delivery.'
}
if (Test-Path -LiteralPath $output) { throw "Output already exists; no previous delivery will be overwritten: $output" }
[void][IO.Directory]::CreateDirectory($output)
[void][IO.Directory]::CreateDirectory((Join-Path $output 'Clips'))
[void][IO.Directory]::CreateDirectory((Join-Path $output 'EncodingLogs'))

function Run-Encoder([string[]]$EncoderArguments, [string]$LogPath) {
    $start = [Diagnostics.ProcessStartInfo]::new()
    $start.FileName = $ffmpeg
    $start.UseShellExecute = $false
    $start.CreateNoWindow = $true
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    $start.StandardOutputEncoding = $utf8
    $start.StandardErrorEncoding = $utf8
    foreach ($argument in @('-hide_banner', '-nostdin', '-y') + $EncoderArguments) { $start.ArgumentList.Add($argument) }
    $process = [Diagnostics.Process]::new()
    $process.StartInfo = $start
    [void]$process.Start()
    $stdout = $process.StandardOutput.ReadToEndAsync()
    $stderr = $process.StandardError.ReadToEndAsync()
    $process.WaitForExit()
    $text = $stdout.GetAwaiter().GetResult() + $stderr.GetAwaiter().GetResult()
    [IO.File]::WriteAllText($LogPath, $text, $utf8)
    $result = $process.ExitCode
    $process.Dispose()
    return $result
}

function Video-Options([string]$Codec, [double]$Fps) {
    if ($Codec -eq 'h264_mf') {
        return @('-c:v', 'h264_mf', '-b:v', '4M', '-maxrate', '6M', '-bufsize', '8M', '-g', [string][int][Math]::Round($Fps * 2))
    }
    return @('-c:v', 'mpeg4', '-q:v', '4', '-g', [string][int][Math]::Round($Fps * 2))
}

$manifest = [ordered]@{
    utc = [DateTime]::UtcNow.ToString('o')
    sourceCapture = $sourceRoot
    captureReport = $reportFile
    output = $output
    playbackSpeed = 1.0
    processing = 'Original consecutive frames at capture.json FPS; original captured game audio encoded to AAC. No crop, zoom, overlays, exposure/colour change, audio gain, speed change, invented frames or footage. Odd pixel dimensions receive at most one black padding pixel for YUV420 compatibility.'
    status = 'Encoding'
    excludedCaptureRows = @($rows | Where-Object { $_.status -ne 'ok' })
    clips = @()
    montage = $null
    visualReview = 'Encoding does not constitute visual review; inspect the actual output before delivery.'
}
$manifestPath = Join-Path $output 'encoding-report.json'
$culture = [Globalization.CultureInfo]::InvariantCulture
$selectedNames = @('Match-Match3', 'Match-Match4', 'Match-Cascade', 'Rocket', 'Rocket-Vertical', 'TNT', 'Propeller', 'ColorBall', 'Combo-ColorBall-Rocket')
$preferredCodec = 'h264_mf'
try {
    foreach ($row in $ok) {
        $name = [string]$row.clip
        if ($name -notmatch '^[A-Za-z0-9_-]+$') { throw "Unsafe or unexpected clip folder name: $name" }
        $folder = Join-Path $sourceRoot $name
        $capture = Get-Content -LiteralPath (Join-Path $folder 'capture.json') -Raw | ConvertFrom-Json
        $frames = [int]$capture.frames
        $fps = [double]$capture.fps
        if ($capture.clip -cne $name -or $frames -ne [int]$row.frames -or $frames -lt 1 -or $fps -le 0 -or $fps -gt 240) {
            throw "Inconsistent capture metadata for $name."
        }
        $images = @(Get-ChildItem -LiteralPath $folder -File -Filter '*.jpg' | Where-Object { $_.Name -match '^\d{5}\.jpg$' } | Sort-Object Name)
        if ($images.Count -ne $frames) { throw "$name has $($images.Count) frames, but its finished report declares $frames." }
        for ($i = 0; $i -lt $frames; $i++) {
            if ($images[$i].Name -cne ($i.ToString('D5') + '.jpg')) { throw "Missing/nonconsecutive frame $i in $name." }
        }
        $audioPath = Join-Path $folder 'audio.wav'
        $audioFile = Get-Item -LiteralPath $audioPath
        $expectedBytes = 44L + [long]$capture.samples * 2L
        if ($audioFile.Length -ne $expectedBytes -or [int]$capture.channels -lt 1 -or [int]$capture.sampleRate -lt 8000) {
            throw "Incomplete captured audio or metadata in $name."
        }
        $duration = $frames / $fps
        $audioDuration = [double]$capture.samples / ([double]$capture.channels * [double]$capture.sampleRate)
        if ([Math]::Abs($audioDuration - $duration) -gt [Math]::Max(.05, 2 / $fps)) {
            throw "$name audio/frame duration differs by more than two frames; preserve the source and inspect capture timing."
        }
        $destination = Join-Path $output ('Clips/' + $name + '.mp4')
        $inputArgs = @('-framerate', $fps.ToString('0.########', $culture), '-start_number', '0', '-i', (Join-Path $folder '%05d.jpg'), '-i', $audioPath, '-map', '0:v:0', '-map', '1:a:0')
        $outputArgs = @('-frames:v', [string]$frames, '-t', $duration.ToString('0.########', $culture), '-vf', 'pad=ceil(iw/2)*2:ceil(ih/2)*2', '-pix_fmt', 'yuv420p', '-vsync', '0', '-c:a', 'aac', '-b:a', '160k', '-ar', [string]$capture.sampleRate, '-ac', [string]$capture.channels, '-movflags', '+faststart', $destination)
        Write-Output "Encoding $name at $fps FPS, $([Math]::Round($duration, 2)) seconds (1.0x)."
        $codec = $preferredCodec
        $exitCode = Run-Encoder ($inputArgs + (Video-Options $codec $fps) + $outputArgs) (Join-Path $output ('EncodingLogs/' + $name + '-' + $codec + '.txt'))
        if ($exitCode -ne 0 -and $codec -eq 'h264_mf') {
            Write-Output 'Media Foundation did not encode this clip; using the MPEG-4 fallback.'
            $codec = 'mpeg4'
            $preferredCodec = 'mpeg4'
            $exitCode = Run-Encoder ($inputArgs + (Video-Options $codec $fps) + $outputArgs) (Join-Path $output ('EncodingLogs/' + $name + '-' + $codec + '.txt'))
        }
        if ($exitCode -ne 0) { throw "Could not encode $name; see its encoding log. Source capture is untouched." }
        $manifest.clips += [ordered]@{ name = $name; source = $folder; sourceFrames = $frames; fps = $fps; durationSeconds = $duration; capturedAudioSeconds = $audioDuration; sampleRate = [int]$capture.sampleRate; channels = [int]$capture.channels; codec = $codec; mp4 = $destination; bytes = (Get-Item -LiteralPath $destination).Length; sourceNote = $capture.note }
        [IO.File]::WriteAllText($manifestPath, ($manifest | ConvertTo-Json -Depth 8), $utf8)
    }

    $selected = @()
    $missing = @()
    foreach ($name in $selectedNames) {
        $clip = @($manifest.clips | Where-Object { $_.name -ceq $name })
        if ($clip.Count -eq 1) { $selected += $clip[0] } else { $missing += $name }
    }
    if ($selected.Count -eq 0) { throw 'No selected montage clip was recovered successfully.' }
    $suffix = if ($missing.Count) { '-Partial' } else { '' }
    $montage = Join-Path $output ('ReferenceGameplay-Integrated-1x' + $suffix + '.mp4')
    $playlist = Join-Path $output 'montage.ffconcat'
    $playlistLines = @('ffconcat version 1.0')
    foreach ($clip in $selected) { $playlistLines += "file 'file:$($clip.mp4.Replace('\', '/'))'" }
    [IO.File]::WriteAllLines($playlist, $playlistLines, $utf8)

    $codecCount = @($selected.codec | Select-Object -Unique).Count
    $fpsCount = @($selected.fps | Select-Object -Unique).Count
    $audioFormats = @($selected | ForEach-Object { "$($_.sampleRate)/$($_.channels)" } | Select-Object -Unique).Count
    if ($codecCount -eq 1 -and $fpsCount -eq 1 -and $audioFormats -eq 1) {
        $montageCode = Run-Encoder @('-f', 'concat', '-safe', '0', '-i', $playlist, '-map', '0:v:0', '-map', '0:a:0', '-c', 'copy', '-movflags', '+faststart', $montage) (Join-Path $output 'EncodingLogs/montage-copy.txt')
        $montageMethod = 'Sequential stream copy of complete selected clips; original playback rate and audio.'
    } else {
        # Mixed fallback codecs cannot be concatenated by stream copy. Decode each
        # complete clip and concatenate their timestamps without changing speed.
        $montageArgs = @()
        $links = ''
        for ($i = 0; $i -lt $selected.Count; $i++) {
            $montageArgs += @('-i', $selected[$i].mp4)
            $links += "[$($i):v:0][$($i):a:0]"
        }
        $filter = $links + 'concat=n=' + $selected.Count + ':v=1:a=1[v][a]'
        $montageArgs += @('-filter_complex', $filter, '-map', '[v]', '-map', '[a]')
        $montageArgs += Video-Options 'mpeg4' $selected[0].fps
        $montageArgs += @('-vsync', '2', '-pix_fmt', 'yuv420p', '-c:a', 'aac', '-b:a', '160k', '-movflags', '+faststart', $montage)
        $montageCode = Run-Encoder $montageArgs (Join-Path $output 'EncodingLogs/montage-fallback.txt')
        $montageMethod = 'Sequential full-clip concat filter for mixed codecs; unchanged source timestamps/playback speed, MPEG-4/AAC re-encode.'
    }
    if ($montageCode -ne 0) { throw 'Montage encoding failed; individual clips remain available. See montage log.' }
    $manifest.montage = [ordered]@{ mp4 = $montage; clips = @($selected.name); missingSelectedClips = $missing; durationSeconds = ($selected | Measure-Object durationSeconds -Sum).Sum; method = $montageMethod; bytes = (Get-Item -LiteralPath $montage).Length }
    $manifest.status = if ($missing.Count -or $manifest.excludedCaptureRows.Count) { 'EncodedWithMissingCaptureClips' } else { 'Encoded' }
    Write-Output "Montage: $montage"
    if ($missing.Count) { Write-Warning ('Montage is incomplete; missing selected capture clips: ' + ($missing -join ', ')) }
} catch {
    $manifest.status = 'Failed'
    $manifest.error = $_.Exception.Message
    throw
} finally {
    [IO.File]::WriteAllText($manifestPath, ($manifest | ConvertTo-Json -Depth 8), $utf8)
    Write-Output "Encoding report: $manifestPath"
}



