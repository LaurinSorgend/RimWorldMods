param([string]$Root)
Add-Type -AssemblyName System.Drawing

function New-Canvas([int]$w, [int]$h) {
    $bmp = New-Object System.Drawing.Bitmap $w, $h, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = 'AntiAlias'
    $g.Clear([System.Drawing.Color]::Transparent)
    return @($bmp, $g)
}

function Draw-PawPrint($g, [float]$cx, [float]$cy, [float]$s, $brush) {
    $g.FillEllipse($brush, $cx - 0.42*$s, $cy - 0.18*$s, 0.84*$s, 0.62*$s)
    foreach ($t in @(@(-0.48,-0.42), @(-0.17,-0.68), @(0.17,-0.68), @(0.48,-0.42))) {
        $g.FillEllipse($brush, $cx + $t[0]*$s - 0.15*$s, $cy + $t[1]*$s - 0.17*$s, 0.30*$s, 0.34*$s)
    }
}

function Draw-TreeGlyph($g, [float]$size, $color) {
    $pen = New-Object System.Drawing.Pen $color, ([float]($size * 0.06))
    $brush = New-Object System.Drawing.SolidBrush $color
    $top = @(($size*0.5), ($size*0.24)); $l = @(($size*0.22), ($size*0.80)); $r = @(($size*0.78), ($size*0.80))
    $barY = $size*0.47
    $g.DrawLine($pen, $top[0], ($top[1] + $size*0.1), $top[0], $barY)
    $g.DrawLine($pen, $l[0], $barY, $r[0], $barY)
    $g.DrawLine($pen, $l[0], $barY, $l[0], ($size*0.60))
    $g.DrawLine($pen, $r[0], $barY, $r[0], ($size*0.60))
    Draw-PawPrint $g $top[0] ($top[1] + $size*0.02) ($size*0.30) $brush
    Draw-PawPrint $g $l[0] ($l[1] + $size*0.02) ($size*0.24) $brush
    Draw-PawPrint $g $r[0] ($r[1] + $size*0.02) ($size*0.24) $brush
}

function Save($pair, [string]$path) {
    $pair[1].Dispose()
    $pair[0].Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $pair[0].Dispose()
}

$ui = Join-Path $Root "Common\Textures\UI\AnimalFamilyTree"
$white = [System.Drawing.Color]::FromArgb(255, 235, 235, 235)

$c = New-Canvas 64 64; Draw-TreeGlyph $c[1] 64 $white; Save $c (Join-Path $ui "BarIcon.png")
$c = New-Canvas 128 128; Draw-TreeGlyph $c[1] 128 $white; Save $c (Join-Path $ui "Gizmo.png")

$c = New-Canvas 64 64; $g = $c[1]
$stone = New-Object System.Drawing.Drawing2D.GraphicsPath
$stone.AddArc(14, 6, 36, 36, 180, 180); $stone.AddLine(50, 24, 50, 54); $stone.AddLine(50, 54, 14, 54); $stone.CloseFigure()
$g.FillPath((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 150, 150, 155))), $stone)
$g.DrawPath((New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 40, 40, 45)), 3), $stone)
$cross = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 60, 60, 65)), 4
$g.DrawLine($cross, 32, 16, 32, 42); $g.DrawLine($cross, 23, 25, 41, 25)
$g.FillRectangle((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 70, 110, 50))), 8, 54, 48, 6)
Save $c (Join-Path $ui "Tombstone.png")

$about = Join-Path $Root "About"
$c = New-Canvas 64 64; $c[1].Clear([System.Drawing.Color]::FromArgb(255, 42, 52, 64)); Draw-TreeGlyph $c[1] 64 $white; Save $c (Join-Path $about "ModIcon.png")

$c = New-Canvas 640 360; $g = $c[1]
$g.Clear([System.Drawing.Color]::FromArgb(255, 34, 42, 52))
$g.TranslateTransform(40, 60); Draw-TreeGlyph $g 240 $white; $g.ResetTransform()
$font = New-Object System.Drawing.Font "Segoe UI", 34, ([System.Drawing.FontStyle]::Bold)
$small = New-Object System.Drawing.Font "Segoe UI", 16
$g.TextRenderingHint = 'AntiAlias'
$g.DrawString("Animal", $font, (New-Object System.Drawing.SolidBrush $white), 300, 110)
$g.DrawString("Family Tree", $font, (New-Object System.Drawing.SolidBrush $white), 300, 160)
$g.DrawString("Lineage for your colony's animals", $small, (New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 240, 215, 140))), 304, 225)
Save $c (Join-Path $about "Preview.png")
Write-Output "done"



