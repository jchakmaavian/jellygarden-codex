Add-Type -AssemblyName System.Drawing
$comparison = New-Object System.Drawing.Bitmap(760,360)
$graphics = [Drawing.Graphics]::FromImage($comparison)
$graphics.Clear([Drawing.Color]::FromArgb(9,23,49))
$font = New-Object Drawing.Font('Arial',18,[Drawing.FontStyle]::Bold)
$index=0
foreach($version in @('v10','v11')) {
 $source=[Drawing.Image]::FromFile((Join-Path $pwd ('Design/Cards/LevelCompleted-'+$version+'/Verification/TitleButtons/settled-1080x1920.png')))
 $label=if($index -eq 0){'Before'}else{'Aligned lettering'}
 $graphics.DrawString($label,$font,[Drawing.Brushes]::White,20,10+$index*180)
 $target=New-Object Drawing.Rectangle(20,(45+$index*180),720,125)
 $graphics.DrawImage($source,$target,180,555,720,125,[Drawing.GraphicsUnit]::Pixel)
 $source.Dispose()
 $index++
}
$comparison.Save((Join-Path $pwd 'Design/Cards/LevelCompleted-v11/Alignment-Before-After.png'),[Drawing.Imaging.ImageFormat]::Png)
$font.Dispose();$graphics.Dispose();$comparison.Dispose()