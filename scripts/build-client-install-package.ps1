# Client install package - NO source code, only EXE + guides
# Developer keeps source; client gets release\client-install only

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$releaseRoot = Join-Path $root "release"
$clientRoot = Join-Path $releaseRoot "client-install"
$posOut = Join-Path $clientRoot "POS"
$apiOut = Join-Path $clientRoot "CLOUD-API"
$wpf = Join-Path $root "src\ShopPOS.WPF\ShopPOS.WPF.csproj"
$api = Join-Path $root "src\ShopPOS.Api\ShopPOS.Api.csproj"
$clientSettings = Join-Path $releaseRoot "appsettings.client.json"

Write-Host "Building CLIENT install package (no source)..."

if (Test-Path $clientRoot) { Remove-Item $clientRoot -Recurse -Force }
New-Item -ItemType Directory -Force -Path $posOut | Out-Null
New-Item -ItemType Directory -Force -Path $apiOut | Out-Null

Push-Location $root
try {
    dotnet publish $wpf -c Release -r win-x64 --self-contained false -o $posOut
    if ($LASTEXITCODE -ne 0) { throw "POS publish failed" }

    dotnet publish $api -c Release -r win-x64 --self-contained false -o $apiOut
    if ($LASTEXITCODE -ne 0) { throw "API publish failed" }
}
finally {
    Pop-Location
}

Copy-Item $clientSettings (Join-Path $posOut "appsettings.json") -Force

$assetsDir = Join-Path $posOut "Assets"
New-Item -ItemType Directory -Force -Path $assetsDir | Out-Null
$devLogo = "C:\Users\HD\OneDrive\Attachments\OneDrive\Desktop\logo\logo.png"
if (Test-Path $devLogo) {
    Copy-Item $devLogo (Join-Path $assetsDir "shop-logo.png") -Force
} else {
    Write-Host "Note: shop logo not found - add Assets\shop-logo.png manually"
}

Copy-Item (Join-Path $releaseRoot "START-POS.bat") (Join-Path $posOut "START-POS.bat") -Force
Copy-Item (Join-Path $releaseRoot "START-CLOUD-API.bat") (Join-Path $apiOut "START-CLOUD-API.bat") -Force
Copy-Item (Join-Path $releaseRoot "CLIENT-INSTALL-GUIDE.txt") (Join-Path $clientRoot "CLIENT-INSTALL-GUIDE.txt") -Force

$urduPdf = Join-Path $root "docs\CLIENT-INSTALL-GUIDE-URDU.pdf"
if (Test-Path $urduPdf) {
    Copy-Item $urduPdf (Join-Path $clientRoot "CLIENT-INSTALL-GUIDE-URDU.pdf") -Force
}

Get-ChildItem $posOut -Filter "*.pdb" -Recurse -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue
Get-ChildItem $apiOut -Filter "*.pdb" -Recurse -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue

$noSource = @(
    "CLIENT INSTALL PACKAGE"
    "====================="
    "Source code is NOT included."
    ""
    "Folders:"
    "  POS\          - Double-click START-POS.bat"
    "  CLOUD-API\    - Optional cloud dashboard (START-CLOUD-API.bat)"
    ""
    "Read CLIENT-INSTALL-GUIDE.txt first."
    ""
    "Requirements on shop PC:"
    "  - Windows 10/11"
    "  - .NET 10 Desktop Runtime"
    "  - SQL Server LocalDB or SQL Express"
    ""
    "Developer keeps full source separately."
) -join [Environment]::NewLine
Set-Content -Path (Join-Path $clientRoot "README-CLIENT.txt") -Value $noSource -Encoding UTF8

$zipPath = Join-Path $releaseRoot "KitchenMart-POS-CLIENT-INSTALL.zip"
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
Compress-Archive -Path "$clientRoot\*" -DestinationPath $zipPath -CompressionLevel Optimal

$desktop = Join-Path $env:USERPROFILE "OneDrive\Attachments\OneDrive\Desktop"
if (Test-Path $desktop) {
    Copy-Item $zipPath (Join-Path $desktop "KitchenMart-POS-CLIENT-INSTALL.zip") -Force
    Write-Host "USB zip on Desktop: KitchenMart-POS-CLIENT-INSTALL.zip"
}

Write-Host ""
Write-Host "Client install folder:"
Write-Host $clientRoot
Write-Host "Client zip:"
Write-Host $zipPath
Write-Host ("Size: {0} MB" -f [math]::Round((Get-Item $zipPath).Length / 1MB, 2))
Write-Host ""
Write-Host "Give client ONLY: client-install folder OR KitchenMart-POS-CLIENT-INSTALL.zip"
Write-Host "Do NOT give: src, scripts, sln, developer backup zip"
