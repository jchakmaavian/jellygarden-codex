param([string]$Root, [string]$Items, [string]$Out)
# Zip the given sub-folders of $Root (comma-separated) with ZIP-spec forward-slash entry names (Compress-Archive in
# Windows PowerShell 5.1 writes backslashes). Entry names are relative to $Root, e.g. frames/ColorBall_Idle_001.webp.
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
if (Test-Path $Out) { Remove-Item -Force $Out }
$rootFull = (Resolve-Path $Root).Path.TrimEnd('\')
$zip = [System.IO.Compression.ZipFile]::Open($Out, [System.IO.Compression.ZipArchiveMode]::Create)
$n = 0
try {
  foreach ($item in ($Items -split ',')) {
    $dir = Join-Path $rootFull $item
    Get-ChildItem -Path $dir -Recurse -File | Sort-Object FullName | ForEach-Object {
      $rel = $_.FullName.Substring($rootFull.Length + 1) -replace '\\', '/'
      [void][System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $_.FullName, $rel, [System.IO.Compression.CompressionLevel]::Optimal)
      $n++
    }
  }
} finally { $zip.Dispose() }
"zipped $n files -> $Out"
