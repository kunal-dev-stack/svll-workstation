# =========================================================================
# Shree Vasu Logistics Limited - Automated Project Builder & Packager
# Developed by: Kunal Turkar
# Builds Dual Editions: Admin Edition & User Edition (v8.4)
# =========================================================================

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptDir
$srcDir = Join-Path $projectRoot "src"
$publishAdminDir = Join-Path $projectRoot "publish-admin"
$publishUserDir = Join-Path $projectRoot "publish-user"
$installerDir = Join-Path $projectRoot "installer"
$releaseDir = Join-Path $projectRoot "release"

Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "  Building SVLL IT Support Workstation v8.4 (Dual Editions)" -ForegroundColor Cyan
Write-Host "  Project Directory: $projectRoot" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan

# Terminate running instances if any to avoid file locks
Get-Process -Name "SVLL-IT-Workstation*" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue

# 1. Verify .NET SDK
Write-Host "`n[1/5] Checking .NET SDK installation..." -ForegroundColor Yellow
$dotnetVersion = & dotnet --version
Write-Host "Detected .NET SDK version: $dotnetVersion" -ForegroundColor Green

# 2. Compile & Publish Admin Edition
Write-Host "`n[2/5] Compiling and publishing Admin Edition (BUILD_ADMIN)..." -ForegroundColor Yellow
if (Test-Path $publishAdminDir) {
    Remove-Item $publishAdminDir -Recurse -Force -ErrorAction SilentlyContinue
}

$publishAdminArgs = @(
    "publish",
    "$srcDir\SVLL-IT-Workstation.csproj",
    "-c", "AdminRelease",
    "-r", "win-x64",
    "--no-self-contained",
    "-p:PublishSingleFile=true",
    "-p:IncludeNativeLibrariesForSelfExtract=true",
    "-o", $publishAdminDir
)

& dotnet @publishAdminArgs
if ($LASTEXITCODE -ne 0) {
    throw "Admin publish failed with exit code $LASTEXITCODE"
}

$adminExePath = Join-Path $publishAdminDir "SVLL-IT-Workstation-Admin.exe"
if (-not (Test-Path $adminExePath)) {
    throw "Admin binary was not found at $adminExePath"
}
$adminMb = [Math]::Round(((Get-Item $adminExePath).Length / 1MB), 2)
Write-Host "[OK] Admin Edition published: $adminExePath ($adminMb MB)" -ForegroundColor Green

# 3. Compile & Publish User Edition
Write-Host "`n[3/5] Compiling and publishing User Edition (BUILD_USER)..." -ForegroundColor Yellow
if (Test-Path $publishUserDir) {
    Remove-Item $publishUserDir -Recurse -Force -ErrorAction SilentlyContinue
}

$publishUserArgs = @(
    "publish",
    "$srcDir\SVLL-IT-Workstation.csproj",
    "-c", "UserRelease",
    "-r", "win-x64",
    "--no-self-contained",
    "-p:PublishSingleFile=true",
    "-p:IncludeNativeLibrariesForSelfExtract=true",
    "-o", $publishUserDir
)

& dotnet @publishUserArgs
if ($LASTEXITCODE -ne 0) {
    throw "User publish failed with exit code $LASTEXITCODE"
}

$userExePath = Join-Path $publishUserDir "SVLL-IT-Workstation-User.exe"
if (-not (Test-Path $userExePath)) {
    throw "User binary was not found at $userExePath"
}
$userMb = [Math]::Round(((Get-Item $userExePath).Length / 1MB), 2)
Write-Host "[OK] User Edition published: $userExePath ($userMb MB)" -ForegroundColor Green

# Ensure bundled tools are present in both publish directories
$toolsSrc = Join-Path $srcDir "tools"
if (Test-Path $toolsSrc) {
    Copy-Item -Path $toolsSrc -Destination (Join-Path $publishAdminDir "tools") -Recurse -Force
    Copy-Item -Path $toolsSrc -Destination (Join-Path $publishUserDir "tools") -Recurse -Force
    Write-Host "[OK] Bundled tools (Ultra Remote engine) copied to publish directories." -ForegroundColor Green
}


# 4. Locate Inno Setup Compiler (ISCC.exe)
Write-Host "`n[4/5] Searching for Inno Setup Compiler (ISCC.exe)..." -ForegroundColor Yellow

$isccCandidates = @(
    "ISCC.exe",
    "C:\Program Files\Inno Setup 7\ISCC.exe",
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
    Write-Host "Self-contained binaries are ready in:" -ForegroundColor Green
    Write-Host "  Admin: $publishAdminDir" -ForegroundColor Green
    Write-Host "  User:  $publishUserDir" -ForegroundColor Green
    exit 0
}

Write-Host "Found Inno Setup Compiler at: $isccPath" -ForegroundColor Green

# 5. Compile Inno Setup Installers
Write-Host "`n[5/5] Building installer packages for Admin and User editions..." -ForegroundColor Yellow
if (-not (Test-Path $releaseDir)) {
    New-Item -ItemType Directory -Path $releaseDir -Force | Out-Null
}

# Compile Admin Setup
$adminIss = Join-Path $installerDir "SVLL-IT-Workstation-Admin.iss"
Write-Host "Compiling Admin setup ($adminIss)..." -ForegroundColor Cyan
& $isccPath $adminIss
if ($LASTEXITCODE -ne 0) {
    throw "Admin Inno Setup compilation failed with exit code $LASTEXITCODE"
}

# Compile User Setup
$userIss = Join-Path $installerDir "SVLL-IT-Workstation-User.iss"
Write-Host "Compiling User setup ($userIss)..." -ForegroundColor Cyan
& $isccPath $userIss
if ($LASTEXITCODE -ne 0) {
    throw "User Inno Setup compilation failed with exit code $LASTEXITCODE"
}

Write-Host "`n============================================================" -ForegroundColor Green
Write-Host "  SVLL v8.4 DUAL BUILDS COMPLETED SUCCESSFULLY!" -ForegroundColor Green
Write-Host "============================================================" -ForegroundColor Green
$adminPkg = Join-Path $releaseDir "SVLL-IT-Workstation-Admin-v8.4-Setup.exe"
$userPkg = Join-Path $releaseDir "SVLL-IT-Workstation-User-v8.4-Setup.exe"
if (Test-Path $adminPkg) {
    $aMb = [Math]::Round(((Get-Item $adminPkg).Length / 1MB), 2)
    Write-Host "  ðŸ‘‘ Admin Package: $adminPkg ($aMb MB)" -ForegroundColor Green
}
if (Test-Path $userPkg) {
    $uMb = [Math]::Round(((Get-Item $userPkg).Length / 1MB), 2)
    Write-Host "  ðŸ‘¤ User Package:  $userPkg ($uMb MB)" -ForegroundColor Green

    # Create generic Setup alias for backward-compatible in-app updaters
    $legacySetup = Join-Path $releaseDir "SVLL-IT-Workstation-v8.4-Setup.exe"
    Copy-Item $userPkg $legacySetup -Force
    Write-Host "  ðŸ”„ Legacy Updater Alias: $legacySetup" -ForegroundColor Cyan
}

# 6. Generate CHECKSUMS.txt
$checksumsFile = Join-Path $releaseDir "CHECKSUMS.txt"
$hashLines = @()
Get-ChildItem -Path $releaseDir -Filter "*8.4*" | ForEach-Object {
    $hash = (Get-FileHash $_.FullName -Algorithm SHA256).Hash.ToLower()
    $hashLines += "$hash  $($_.Name)"
}
$hashLines | Out-File -FilePath $checksumsFile -Encoding utf8
Write-Host "Generated SHA256 checksums in $checksumsFile" -ForegroundColor Green
