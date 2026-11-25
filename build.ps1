# Baut EasyButtonFix (Release) und erzeugt in .\publish:
#   - EasyButtonFix-Setup-<Version>.exe   (Installer)
#   - EasyButtonFix-<Version>-portable.zip (ohne Installation)
# Aufruf:  powershell -ExecutionPolicy Bypass -File .\build.ps1
$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

Write-Host "==> App bauen (Release)" -ForegroundColor Cyan
dotnet build src\EasyButtonFix -c Release -nologo -v q
if ($LASTEXITCODE -ne 0) { throw "Build fehlgeschlagen" }

$bin = "src\EasyButtonFix\bin\Release\net48"
$version = (Get-Item "$bin\EasyButtonFix.exe").VersionInfo.ProductVersion
New-Item -ItemType Directory -Force publish | Out-Null

Write-Host "==> Portable ZIP" -ForegroundColor Cyan
$zip = "publish\EasyButtonFix-$version-portable.zip"
if (Test-Path $zip) { Remove-Item $zip }
# erst kopieren: so klappt es auch, wenn die App gerade läuft und ihre DLLs sperrt
$stage = "obj\portable"
if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
New-Item -ItemType Directory -Force $stage | Out-Null
Copy-Item "$bin\*.exe", "$bin\*.dll", "$bin\*.config" $stage
Compress-Archive -Path "$stage\*" -DestinationPath $zip

Write-Host "==> Installer" -ForegroundColor Cyan
$iscc = foreach ($v in 7, 6) {
    "$env:LOCALAPPDATA\Programs\Inno Setup $v\ISCC.exe"
    "$env:ProgramFiles\Inno Setup $v\ISCC.exe"
    "${env:ProgramFiles(x86)}\Inno Setup $v\ISCC.exe"
}
$iscc = $iscc | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $iscc) { throw "Inno Setup nicht gefunden. Download: https://jrsoftware.org/isdl.php" }
& $iscc /Q installer\EasyButtonFix.iss
if ($LASTEXITCODE -ne 0) { throw "Installer-Build fehlgeschlagen" }

Write-Host "`nFertig:" -ForegroundColor Green
Get-ChildItem publish | ForEach-Object { "  {0}  ({1:N1} MB)" -f $_.Name, ($_.Length / 1MB) }
