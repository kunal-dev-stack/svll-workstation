# =========================================================================
# Shree Vasu Logistics Limited - Automated Project Builder & Packager
# Developed by: Kunal Turkar
# Compatible with PowerShell 5.1 and PowerShell 7+
# =========================================================================

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptDir
$srcDir = Join-Path $projectRoot "src"
$publishDir = Join-Path $projectRoot "publish"
$installerDir = Join-Path $projectRoot "installer"
$releaseDir = Join-Path $projectRoot "release"

Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "  Building SVLL IT Support Workstation v5.5" -ForegroundColor Cyan
Write-Host "  Project Directory: $projectRoot" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan

# Terminate running instances if any to avoid file locks
Get-Process -Name "SVLL-IT-Workstation" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue

# 1. Verify .NET SDK
Write-Host "`n[1/4] Checking .NET SDK installation..." -ForegroundColor Yellow
$dotnetVersion = & dotnet --version
Write-Host "Detected .NET SDK version: $dotnetVersion" -ForegroundColor Green

# 2. Compile & Publish Self-Contained Win-x64 Executable
Write-Host "`n[2/4] Compiling and publishing self-contained binary..." -ForegroundColor Yellow
if (Test-Path $publishDir) {
    Remove-Item $publishDir -Recurse -Force -ErrorAction SilentlyContinue
}

$publishArgs = @(
    "publish",
    "$srcDir\SVLL-IT-Workstation.csproj",
    "-c", "Release",
    "-r", "win-x64",
    "--self-contained",
    "-p:PublishSingleFile=true",
    "-p:IncludeNativeLibrariesForSelfExtract=true",
    "-p:EnableCompressionInSingleFile=true",
    "-o", $publishDir
)

& dotnet @publishArgs
if ($LASTEXITCODE -ne 0) {
    throw "Dotnet publish failed with exit code $LASTEXITCODE"
}

$exePath = Join-Path $publishDir "SVLL-IT-Workstation.exe"
if (-not (Test-Path $exePath)) {
    throw "Compiled binary was not found at $exePath"
}
$exeSize = (Get-Item $exePath).Length / 1MB
$exeMb = [Math]::Round($exeSize, 2)
Write-Host "[OK] Published single-file executable successfully: $exeMb MB" -ForegroundColor Green

# 3. Locate Inno Setup Compiler (ISCC.exe) across 32-bit, 64-bit and local AppData
Write-Host "`n[3/4] Searching for Inno Setup Compiler (ISCC.exe)..." -ForegroundColor Yellow

$isccCandidates = @(
    "ISCC.exe",
    "$env:ProgramFiles\Inno Setup 6\ISCC.exe",
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe",
    "$env:ProgramData\chocolatey\bin\ISCC.exe",
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "C:\Program Files\Inno Setup 6\ISCC.exe"
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
    Write-Warning "ISCC.exe could not be found. Skipping installer package build."
    Write-Host "Self-contained binary is ready in: $publishDir" -ForegroundColor Green
    exit 0
}

Write-Host "Found Inno Setup Compiler at: $isccPath" -ForegroundColor Green

# 4. Compile Inno Setup Installer Package
Write-Host "`n[4/4] Building setup package with transparent icon..." -ForegroundColor Yellow
if (-not (Test-Path $releaseDir)) {
    New-Item -ItemType Directory -Path $releaseDir -Force | Out-Null
}

$issFile = Join-Path $installerDir "SVLL-IT-Workstation.iss"
& $isccPath $issFile
if ($LASTEXITCODE -ne 0) {
    throw "Inno Setup compilation failed with exit code $LASTEXITCODE"
}

$setupPath = Join-Path $releaseDir "SVLL-IT-Workstation-v5.5-Setup.exe"
if (Test-Path $setupPath) {
    $setupSize = (Get-Item $setupPath).Length / 1MB
    $setupMb = [Math]::Round($setupSize, 2)
    Write-Host "`n[OK] SETUP INSTALLER CREATED SUCCESSFULLY!" -ForegroundColor Green
    Write-Host "Package: $setupPath - $setupMb MB" -ForegroundColor Green
}
