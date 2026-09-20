param([string]$Dir, [string]$Out)
$ErrorActionPreference = 'Stop'
Add-Type -Path (Join-Path $Dir 'review_ColorBall_TargetImpact.cs') -ReferencedAssemblies System.Drawing
$fr = Join-Path $Out 'frames'
foreach ($f in 9, 15, 17, 20, 24, 27, 29) {
  $w = 0; $h = 0
  $px = [ReviewTargetImpact]::Load((Join-Path $fr ('ColorBall_TargetImpact_{0:000}.png' -f $f)), [ref]$w, [ref]$h)
  # brightest/most opaque ring-body texel stats in annulus 0.35c..0.5c
  $n=0; $sa=0; $maxa=0; $cnt255=0; $sr=0;$sg=0;$sb=0
  for ($y = 0; $y -lt $h; $y+=1) { for ($x = 0; $x -lt $w; $x+=1) { $dx=$x+0.5-192; $dy=$y+0.5-192; $r=[math]::Sqrt($dx*$dx+$dy*$dy); if ($r -ge 70 -and $r -le 90) { $i=($y*$w+$x)*4; $a=$px[$i+3]; if ($a -ge 200) { $n++; $sa+=$a; if($a -eq 255){$cnt255++}; $sr+=$px[$i+2]; $sg+=$px[$i+1]; $sb+=$px[$i] } } } }
  if ($n -gt 0) { 'f{0}: body texels(a>=200) {1}, mean a {2:0.0}, a=255 share {3:0.00}, mean straight RGB ({4:0},{5:0},{6:0})' -f $f,$n,($sa/$n),($cnt255/$n),($sr/$n),($sg/$n),($sb/$n) }
}
