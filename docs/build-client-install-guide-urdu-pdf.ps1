# Builds CLIENT-INSTALL-GUIDE-URDU.pdf from HTML (Edge headless, temp folder for paths with spaces)
# Run: powershell -ExecutionPolicy Bypass -File docs\build-client-install-guide-urdu-pdf.ps1

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$root = Split-Path -Parent $scriptDir
$htmlSource = Join-Path $scriptDir "CLIENT-INSTALL-GUIDE-URDU.html"
$pdfTarget = Join-Path $scriptDir "CLIENT-INSTALL-GUIDE-URDU.pdf"
$clientPdf = Join-Path $root "release\client-install\CLIENT-INSTALL-GUIDE-URDU.pdf"
$desktopPdf = Join-Path $env:USERPROFILE "OneDrive\Attachments\OneDrive\Desktop\CLIENT-INSTALL-GUIDE-URDU.pdf"

$edge = @(
    "${env:ProgramFiles(x86)}\Microsoft\Edge\Application\msedge.exe",
    "${env:ProgramFiles}\Microsoft\Edge\Application\msedge.exe"
) | Where-Object { Test-Path $_ } | Select-Object -First 1

if (-not $edge) {
    Write-Host "Edge not found. Open HTML in browser -> Ctrl+P -> Save as PDF:"
    Write-Host $htmlSource
    exit 1
}

$tmp = Join-Path $env:TEMP "pos-install-urdu"
New-Item -ItemType Directory -Force -Path $tmp | Out-Null
Copy-Item $htmlSource (Join-Path $tmp "guide.html") -Force
$pdfTmp = Join-Path $tmp "guide.pdf"
$htmlUri = "file:///$($tmp -replace '\\','/')/guide.html"

Remove-Item $pdfTmp -ErrorAction SilentlyContinue
& $edge --headless --disable-gpu --print-to-pdf="$pdfTmp" $htmlUri
Start-Sleep -Seconds 3

if (-not (Test-Path $pdfTmp)) {
    Write-Host "Auto PDF failed. Open HTML -> Ctrl+P -> Save as PDF:"
    Write-Host $htmlSource
    exit 1
}

Copy-Item $pdfTmp $pdfTarget -Force

$clientDir = Split-Path $clientPdf -Parent
if (Test-Path $clientDir) {
    Copy-Item $pdfTmp $clientPdf -Force
    Write-Host "Copied to client install:"
    Write-Host $clientPdf
}

if (Test-Path (Split-Path $desktopPdf -Parent)) {
    Copy-Item $pdfTmp $desktopPdf -Force
    Write-Host "Copied to Desktop:"
    Write-Host $desktopPdf
}

Write-Host ""
Write-Host "PDF saved:"
Write-Host $pdfTarget
Write-Host ("Size: {0} KB" -f [math]::Round((Get-Item $pdfTarget).Length / 1KB, 1))
