# build/build-installer.ps1
# Script to build the Inno Setup installer for Adara Box MVP

param(
    [switch]$SkipPublish = $false
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host " Adara Box MVP - Installer Build Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# 1. Verify published files
$publishExe = "artifacts/publish/AdaraBox.App.exe"
if (-not $SkipPublish -or -not (Test-Path $publishExe)) {
    Write-Host "`n[1/2] Publishing application binaries first..." -ForegroundColor Yellow
    & "$PSScriptRoot\publish.ps1"
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Application publish step failed!"
        exit $LASTEXITCODE
    }
} else {
    Write-Host "`n[1/2] Using existing publish artifacts at 'artifacts/publish'..." -ForegroundColor Green
}

# 2. Locate ISCC.exe (Inno Setup Compiler)
Write-Host "`n[2/2] Locating Inno Setup 6 Compiler (ISCC.exe)..." -ForegroundColor Yellow

$isccCandidates = @(
    "ISCC.exe",
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "$env:ProgramFiles\Inno Setup 6\ISCC.exe",
    "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
)

$isccPath = $null
foreach ($candidate in $isccCandidates) {
    if (Get-Command $candidate -ErrorAction SilentlyContinue) {
        $isccPath = $candidate
        break
    }
    if (Test-Path $candidate) {
        $isccPath = $candidate
        break
    }
}

if (-not $isccPath) {
    Write-Host "`n[HUMAN CHECK REQUIRED]" -ForegroundColor Magenta
    Write-Host "Inno Setup 6 is not installed on this machine." -ForegroundColor Yellow
    Write-Host "To compile the setup executable manually:"
    Write-Host "  1. Install Inno Setup 6 via: 'winget install JRSoftware.InnoSetup' or download from https://jrsoftware.org/isinfo.php"
    Write-Host "  2. Run: 'powershell -ExecutionPolicy Bypass -File build/build-installer.ps1'"
    Write-Host "  3. Expected Output: 'artifacts/installer/AdaraBox-Setup-1.0.0.exe'"
    exit 0
}

Write-Host "Found Inno Setup Compiler at: $isccPath" -ForegroundColor Green

# Ensure output directory exists
$installerDir = "artifacts/installer"
if (-not (Test-Path $installerDir)) {
    New-Item -ItemType Directory -Force -Path $installerDir | Out-Null
}

$issFile = "installer/AdaraBox.iss"
Write-Host "Compiling '$issFile'..." -ForegroundColor Yellow

& $isccPath $issFile
if ($LASTEXITCODE -ne 0) {
    Write-Error "Inno Setup compilation failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}

$outputExe = Join-Path $installerDir "AdaraBox-Setup-1.0.0.exe"
if (Test-Path $outputExe) {
    $sizeMb = [Math]::Round((Get-Item $outputExe).Length / 1MB, 2)
    Write-Host "`nInstaller created successfully!" -ForegroundColor Green
    Write-Host "File: $outputExe" -ForegroundColor Green
    Write-Host "Size: $sizeMb MB" -ForegroundColor Green
} else {
    Write-Warning "Setup output file was not found at expected path: $outputExe"
}
