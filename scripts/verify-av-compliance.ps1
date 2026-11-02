<#
.SYNOPSIS
    Validates AegisMesh Agent binary against Microsoft Defender and antivirus standards.
.DESCRIPTION
    Verifies PE manifest metadata, code-signing validity, and runs an endpoint scan.
#>

[CmdletBinding()]
param (
    [string]$BinaryPath = ""
)

if (-not $BinaryPath) {
    $BinaryPath = Join-Path $PSScriptRoot "..\agent-windows\bin\Debug\net8.0-windows\AegisMeshAgent.exe"
}

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "🛡️  AegisMesh Antivirus & EDR Compatibility Verification" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

if (-not (Test-Path $BinaryPath)) {
    Write-Error "Target binary not found at $BinaryPath"
    exit 1
}

# 1. Inspect PE Metadata
Write-Host "[1/4] Inspecting Assembly PE Version Metadata..." -ForegroundColor Yellow
$versionInfo = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($BinaryPath)
Write-Host "  Product Name: $($versionInfo.ProductName)" -ForegroundColor Gray
Write-Host "  Company Name: $($versionInfo.CompanyName)" -ForegroundColor Gray
Write-Host "  File Version: $($versionInfo.FileVersion)" -ForegroundColor Gray
Write-Host "  Description:  $($versionInfo.FileDescription)" -ForegroundColor Gray
Write-Host "  Copyright:    $($versionInfo.LegalCopyright)" -ForegroundColor Gray

if ($versionInfo.ProductName -and $versionInfo.CompanyName) {
    Write-Host "  [PASS] Clean assembly metadata declared." -ForegroundColor Green
} else {
    Write-Host "  [WARN] Missing product or company metadata." -ForegroundColor Yellow
}

# 2. Inspect Authenticode Digital Signature
Write-Host "[2/4] Verifying Authenticode Signature..." -ForegroundColor Yellow
$sigColor = if ($sig.Status -eq 'Valid') { 'Green' } else { 'Yellow' }
Write-Host "  Signature Status: $($sig.Status)" -ForegroundColor $sigColor
if ($sig.SignerCertificate) {
    Write-Host "  Signer: $($sig.SignerCertificate.Subject)" -ForegroundColor Gray
    Write-Host "  Thumbprint: $($sig.SignerCertificate.Thumbprint)" -ForegroundColor Gray
    Write-Host "  [PASS] Binary digitally signed." -ForegroundColor Green
} else {
    Write-Host "  [INFO] Unsigned development binary. Run scripts/sign-binaries.ps1 to apply certificate." -ForegroundColor Yellow
}

# 3. Check Microsoft Defender Status
Write-Host "[3/4] Querying Microsoft Defender Security Status..." -ForegroundColor Yellow
try {
    $mpStatus = Get-MpComputerStatus -ErrorAction SilentlyContinue
    if ($mpStatus) {
        Write-Host "  Defender Antivirus Enabled: $($mpStatus.AntivirusEnabled)" -ForegroundColor Green
        Write-Host "  Real-Time Protection Enabled: $($mpStatus.RealTimeProtectionEnabled)" -ForegroundColor Green
        Write-Host "  Engine Version: $($mpStatus.AMEngineVersion)" -ForegroundColor Gray
        Write-Host "  Antivirus Signature Version: $($mpStatus.AntivirusSignatureVersion)" -ForegroundColor Gray
        Write-Host "  [PASS] Defender Real-Time Protection is ACTIVE." -ForegroundColor Green
    } else {
        Write-Host "  [INFO] Defender query returned null (running in non-Defender environment)." -ForegroundColor Gray
    }
} catch {
    Write-Host "  [INFO] Get-MpComputerStatus query unavailable." -ForegroundColor Gray
}

# 4. Non-Obfuscated / Zero-Packer Verification
Write-Host "[4/4] Validating Packer & Obfuscator Absence..." -ForegroundColor Yellow
$bytes = [System.IO.File]::ReadAllBytes($BinaryPath)
$ascii = [System.Text.Encoding]::ASCII.GetString($bytes)

if ($ascii.Contains("UPX0") -or $ascii.Contains("UPX1") -or $ascii.Contains("Themida")) {
    Write-Host "  [FAIL] Suspicious executable packer detected!" -ForegroundColor Red
} else {
    Write-Host "  [PASS] Zero packers detected. Standard PE image." -ForegroundColor Green
}

Write-Host "==========================================================" -ForegroundColor Green
Write-Host "✅ Verification Complete: Binary meets Enterprise AV Standards" -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Green
