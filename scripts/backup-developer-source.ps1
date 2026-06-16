# Developer source backup — full project for next client / future work
# Client ko yeh NAHI dena.

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$releaseRoot = Join-Path $root "release"
$backupRoot = Join-Path $releaseRoot "developer-backup"
$stamp = Get-Date -Format "yyyy-MM-dd"
$zipName = "ShopPOS-FULL-SOURCE-BACKUP-$stamp.zip"
$zipPath = Join-Path $backupRoot $zipName

New-Item -ItemType Directory -Force -Path $backupRoot | Out-Null

$excludeDirNames = @('bin', 'obj', 'publish', '.vs', '.git', 'node_modules', 'developer-backup', 'client-install')
$excludeFilePatterns = @('*.user', '*.suo', '*.cache', 'cloud-pos.db', 'cloud-pos.db-*')

Write-Host "Creating developer backup..."
Write-Host "Source: $root"

$temp = Join-Path $env:TEMP "shoppos-backup-$stamp"
if (Test-Path $temp) { Remove-Item $temp -Recurse -Force }
New-Item -ItemType Directory -Force -Path $temp | Out-Null

function ShouldExcludeDir([string]$name) { $excludeDirNames -contains $name }

function Copy-Filtered([string]$src, [string]$dest) {
    New-Item -ItemType Directory -Force -Path $dest | Out-Null
    Get-ChildItem -LiteralPath $src -Force | ForEach-Object {
        if ($_.PSIsContainer) {
            if (ShouldExcludeDir $_.Name) { return }
            Copy-Filtered $_.FullName (Join-Path $dest $_.Name)
        } else {
            $skip = $false
            foreach ($pat in $excludeFilePatterns) {
                if ($_.Name -like $pat) { $skip = $true; break }
            }
            if (-not $skip) { Copy-Item $_.FullName (Join-Path $dest $_.Name) -Force }
        }
    }
}

Copy-Filtered $root $temp

$readme = @"
ShopPOS — DEVELOPER FULL SOURCE BACKUP
Date: $stamp

Yeh backup SIRF developer ke liye hai — next project / naye client ke liye.
Client ko source code NAHI dena — sirf release\client-install folder do.

Restore:
1. Zip extract karo
2. dotnet restore ShopPOS.sln
3. dotnet build ShopPOS.sln

Includes: source, docs, scripts, database schema, delivery checklist PDF
Excludes: bin, obj, publish output, .vs, cloud-pos.db
"@
Set-Content -Path (Join-Path $temp "BACKUP-README-DEVELOPER.txt") -Value $readme -Encoding UTF8

if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
Compress-Archive -Path "$temp\*" -DestinationPath $zipPath -CompressionLevel Optimal
Remove-Item $temp -Recurse -Force

$desktop = Join-Path $env:USERPROFILE "OneDrive\Attachments\OneDrive\Desktop"
if (Test-Path $desktop) {
    Copy-Item $zipPath (Join-Path $desktop $zipName) -Force
    Write-Host "Desktop copy: $(Join-Path $desktop $zipName)"
}

Write-Host ""
Write-Host "Developer backup ready:"
Write-Host $zipPath
Write-Host "Size: $([math]::Round((Get-Item $zipPath).Length / 1MB, 2)) MB"
