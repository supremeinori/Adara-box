# build/publish.ps1
# Script to build, test, and publish Adara Box as self-contained win-x64

param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$OutputDir = "artifacts/publish"
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host " Adara Box MVP - Publish Script ($Configuration)" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# Locate dotnet CLI with SDK support
$userDotnetDir = "$env:LOCALAPPDATA\Microsoft\dotnet"
$userDotnet = Join-Path $userDotnetDir "dotnet.exe"

if (Test-Path $userDotnet) {
    $dotnet = $userDotnet
    $env:DOTNET_ROOT = $userDotnetDir
    $env:PATH = "$userDotnetDir;$env:PATH"
} elseif (Get-Command dotnet -ErrorAction SilentlyContinue) {
    $dotnet = "dotnet"
} else {
    Write-Error "dotnet CLI could not be found in PATH or standard user directories."
    exit 1
}

Write-Host "`n[1/3] Running automated test suite..." -ForegroundColor Yellow
& $dotnet test AdaraBox.sln -c $Configuration --nologo
if ($LASTEXITCODE -ne 0) {
    Write-Error "Automated test suite failed! Publish aborted."
    exit $LASTEXITCODE
}
Write-Host "All tests passed successfully." -ForegroundColor Green

Write-Host "`n[2/3] Publishing AdaraBox.App (Self-Contained, $Runtime)..." -ForegroundColor Yellow
Stop-Process -Name "AdaraBox.App" -Force -ErrorAction SilentlyContinue
Start-Sleep -Milliseconds 600

if (Test-Path $OutputDir) {
    Remove-Item -Recurse -Force $OutputDir
}

& $dotnet publish src/AdaraBox.App -c $Configuration -r $Runtime `
    --self-contained true `
    -p:PublishSingleFile=false `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o $OutputDir `
    --nologo

if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet publish failed!"
    exit $LASTEXITCODE
}

Write-Host "`n[3/3] Verifying published artifacts..." -ForegroundColor Yellow
$exePath = Join-Path $OutputDir "AdaraBox.App.exe"
if (-not (Test-Path $exePath)) {
    Write-Error "Expected executable '$exePath' was not found in output directory."
    exit 1
}

# Calculate total size of published folder
$files = Get-ChildItem -Recurse -File $OutputDir
$totalBytes = ($files | Measure-Object -Property Length -Sum).Sum
$totalMb = [Math]::Round($totalBytes / 1MB, 2)

Write-Host "Executable: $exePath" -ForegroundColor Green
Write-Host "Total published files: $($files.Count)" -ForegroundColor Green
Write-Host "Total published size: $totalMb MB (Self-Contained, no .NET runtime required)" -ForegroundColor Green
Write-Host "`nPublish completed successfully!" -ForegroundColor Cyan
