# Erzeugt Assets\app.ico (mehrere Größen) und Assets\app.png aus Code.
# Aufruf: powershell -ExecutionPolicy Bypass -File tools\make-icon.ps1
Add-Type -AssemblyName System.Drawing
$out = Join-Path $PSScriptRoot '..\src\EasyButtonFix\Assets'

function New-IconBitmap([int]$s) {
    $bmp = New-Object System.Drawing.Bitmap $s, $s
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = 'AntiAlias'; $g.PixelOffsetMode = 'HighQuality'
    $g.Clear([System.Drawing.Color]::Transparent)

    # abgerundetes Quadrat mit Verlauf (Blau -> Violett)
    $r = [single]($s * 0.22); $m = [single]($s * 0.04); $w = [single]($s - 2 * $m)
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $path.AddArc($m, $m, $r * 2, $r * 2, 180, 90)
    $path.AddArc($m + $w - $r * 2, $m, $r * 2, $r * 2, 270, 90)
    $path.AddArc($m + $w - $r * 2, $m + $w - $r * 2, $r * 2, $r * 2, 0, 90)
    $path.AddArc($m, $m + $w - $r * 2, $r * 2, $r * 2, 90, 90)
    $path.CloseFigure()
    $rect = New-Object System.Drawing.RectangleF $m, $m, $w, $w
    $grad = New-Object System.Drawing.Drawing2D.LinearGradientBrush $rect, ([System.Drawing.Color]::FromArgb(255, 59, 130, 246)), ([System.Drawing.Color]::FromArgb(255, 139, 92, 246)), 45
    $g.FillPath($grad, $path)

    # Maus
    $mw = [single]($s * 0.36); $mh = [single]($s * 0.54)
    $mx = [single](($s - $mw) / 2 - $s * 0.04); $my = [single]($s * 0.22)
    $white = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::White)
    $mouse = New-Object System.Drawing.Drawing2D.GraphicsPath
    $mouse.AddEllipse($mx, $my, $mw, $mh)
    $g.FillPath($white, $mouse)
    $penW = [single][Math]::Max(1, $s * 0.035)
    $line = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 99, 102, 241)), $penW
    $cx = [single]($mx + $mw / 2)
    $g.DrawLine($line, $cx, $my + $penW, $cx, $my + $mh * 0.38)
    $g.DrawLine($line, $mx + $penW, $my + $mh * 0.38, $mx + $mw - $penW, $my + $mh * 0.38)

    # grüner Haken-Badge unten rechts
    if ($s -ge 20) {
        $bd = [single]($s * 0.40); $bx = [single]($s * 0.56); $by = [single]($s * 0.56)
        $g.FillEllipse((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::White)), $bx - $s * 0.03, $by - $s * 0.03, $bd + $s * 0.06, $bd + $s * 0.06)
        $g.FillEllipse((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 34, 197, 94))), $bx, $by, $bd, $bd)
        $ck = New-Object System.Drawing.Pen ([System.Drawing.Color]::White), ([single][Math]::Max(1.5, $s * 0.055))
        $ck.StartCap = 'Round'; $ck.EndCap = 'Round'; $ck.LineJoin = 'Round'
        $pts = @(
            (New-Object System.Drawing.PointF ($bx + $bd * 0.27), ($by + $bd * 0.52)),
            (New-Object System.Drawing.PointF ($bx + $bd * 0.45), ($by + $bd * 0.70)),
            (New-Object System.Drawing.PointF ($bx + $bd * 0.75), ($by + $bd * 0.33)))
        $g.DrawLines($ck, $pts)
    }
    $g.Dispose()
    return $bmp
}

$sizes = 16, 20, 24, 32, 48, 64, 128, 256
$pngs = foreach ($s in $sizes) {
    $b = New-IconBitmap $s
    $ms = New-Object System.IO.MemoryStream
    $b.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    if ($s -eq 256) { $b.Save((Join-Path $out 'app.png'), [System.Drawing.Imaging.ImageFormat]::Png) }
    $b.Dispose()
    , $ms.ToArray()
}

# ICO-Datei mit PNG-Einträgen zusammensetzen
$fs = [System.IO.File]::Create((Join-Path $out 'app.ico'))
$bw = New-Object System.IO.BinaryWriter $fs
$bw.Write([uint16]0); $bw.Write([uint16]1); $bw.Write([uint16]$sizes.Count)
$offset = 6 + 16 * $sizes.Count
for ($i = 0; $i -lt $sizes.Count; $i++) {
    $s = $sizes[$i]; $d = $pngs[$i]
    $bw.Write([byte]($(if ($s -ge 256) { 0 } else { $s })))
    $bw.Write([byte]($(if ($s -ge 256) { 0 } else { $s })))
    $bw.Write([byte]0); $bw.Write([byte]0)
    $bw.Write([uint16]1); $bw.Write([uint16]32)
    $bw.Write([uint32]$d.Length); $bw.Write([uint32]$offset)
    $offset += $d.Length
}
foreach ($d in $pngs) { $bw.Write($d) }
$bw.Close()
Write-Host "Icon erstellt: $out"
