param([string]$Out)
# Per-frame total light (sum of premultiplied luminance) and alpha coverage: shows shimmer amplitude during the hold.
Add-Type -AssemblyName System.Drawing
foreach ($clip in @('White','Gold')) {
  $line = "$clip light:"
  for ($f = 1; $f -le 22; $f++) {
    $p = Join-Path $Out ("$clip\frames\ColorBall_StarSpark_{0:000}.png" -f $f)
    $bmp = New-Object System.Drawing.Bitmap $p
    $data = $bmp.LockBits((New-Object System.Drawing.Rectangle 0,0,$bmp.Width,$bmp.Height), 'ReadOnly', 'Format32bppArgb')
    $n = $bmp.Width * $bmp.Height * 4; $px = New-Object byte[] $n
    [Runtime.InteropServices.Marshal]::Copy($data.Scan0, $px, 0, $n); $bmp.UnlockBits($data); $bmp.Dispose()
    $sum = 0.0
    for ($i = 0; $i -lt $n; $i += 4) { $a = $px[$i+3]; if ($a -gt 0) { $sum += (0.3*$px[$i+2] + 0.59*$px[$i+1] + 0.11*$px[$i]) * $a / 65025.0 } }
    $line += " " + [math]::Round($sum, 0)
  }
  $line
}
