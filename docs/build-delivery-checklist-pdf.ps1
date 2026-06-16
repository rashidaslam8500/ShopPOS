# Builds CLIENT-DELIVERY-CHECKLIST.pdf from HTML (uses temp folder for Edge headless)
# Run: powershell -ExecutionPolicy Bypass -File docs\build-delivery-checklist-pdf.ps1

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$htmlSource = Join-Path $scriptDir "CLIENT-DELIVERY-CHECKLIST.html"
$pdfTarget = Join-Path $scriptDir "CLIENT-DELIVERY-CHECKLIST.pdf"
$desktopPdf = Join-Path $env:USERPROFILE "OneDrive\Attachments\OneDrive\Desktop\CLIENT-DELIVERY-CHECKLIST.pdf"

$edge = @(
    "${env:ProgramFiles(x86)}\Microsoft\Edge\Application\msedge.exe",
    "${env:ProgramFiles}\Microsoft\Edge\Application\msedge.exe"
) | Where-Object { Test-Path $_ } | Select-Object -First 1

if (-not $edge) {
    Write-Host "Open in browser -> Ctrl+P -> Save as PDF:"
    Write-Host $htmlSource
    exit 1
}

$tmp = Join-Path $env:TEMP "pos-checklist"
New-Item -ItemType Directory -Force -Path $tmp | Out-Null
Copy-Item $htmlSource (Join-Path $tmp "checklist.html") -Force
$pdfTmp = Join-Path $tmp "checklist.pdf"
$htmlUri = "file:///$($tmp -replace '\\','/')/checklist.html"

Remove-Item $pdfTmp -ErrorAction SilentlyContinue
& $edge --headless --disable-gpu --print-to-pdf="$pdfTmp" $htmlUri
Start-Sleep -Seconds 2

if (-not (Test-Path $pdfTmp)) {
    Write-Host "Auto PDF failed. Open HTML -> Ctrl+P -> Save as PDF:"
    Write-Host $htmlSource
    exit 1
}

Copy-Item $pdfTmp $pdfTarget -Force
if (Test-Path (Split-Path $desktopPdf -Parent)) {
    Copy-Item $pdfTmp $desktopPdf -Force
    Write-Host "Also copied to Desktop:"
    Write-Host $desktopPdf
}

Write-Host "PDF saved:"
Write-Host $pdfTarget
