param([string]$Dir, [string]$Out)
$ErrorActionPreference = 'Stop'
Add-Type -Path (Join-Path $Dir 'review_ColorBall_TargetImpact.cs') -ReferencedAssemblies System.Drawing
$fr = Join-Path $Out 'frames'
foreach ($f in 13, 23, 25, 27, 28, 29, 31) {
  $w = 0; $h = 0
  $px = [ReviewTargetImpact]::Load((Join-Path $fr ('ColorBall_TargetImpact_{0:000}.png' -f $f)), [ref]$w, [ref]$h)
  $lines = @()
  for ($y = 0; $y -lt $h; $y++) { for ($x = 0; $x -lt $w; $x++) { $a = $px[($y*$w+$x)*4+3]; if ($a -gt 0 -and $a -lt 8) {
    # max alpha in 3px neighbourhood
    $m = 0; for ($oy=-3;$oy -le 3;$oy++){ for($ox=-3;$ox -le 3;$ox++){ $nx=$x+$ox;$ny=$y+$oy; if($nx -ge 0 -and $ny -ge 0 -and $nx -lt $w -and $ny -lt $h){ $v=$px[($ny*$w+$nx)*4+3]; if($v -gt $m){$m=$v} } } }
    if ($m -lt 8) { $r = [math]::Sqrt(($x+0.5-192)*($x+0.5-192)+($y+0.5-192)*($y+0.5-192)); $lines += ('({0},{1}) a={2} r={3:0}' -f $x,$y,$a,$r) } } } }
  "f$f isolated faint px: " + $lines.Count + "  sample: " + (($lines | Select-Object -First 4) -join '; ')
}
