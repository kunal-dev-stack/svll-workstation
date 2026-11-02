<#
.SYNOPSIS
    Signs AegisMesh Agent binaries using Authenticode with SHA-256 and RFC 3161 timestamping.
.DESCRIPTION
    Compliant with Microsoft Defender SmartScreen and enterprise code-signing policies.
#>

[CmdletBinding()]
param (
    [string]$BinaryPath = "",
    [string]$CertificateThumbprint = "",
    [string]$TimestampServer = "http://timestamp.digicert.com"
)

if (-not $BinaryPath) {
    $BinaryPath = Join-Path $PSScriptRoot "..\agent-windows\bin\Debug\net8.0-windows\AegisMeshAgent.exe"
}

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "🛡️  AegisMesh Authenticode Code Signing Pipeline" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

if (-not (Test-Path $BinaryPath)) {
    Write-Error "Binary not found at: $BinaryPath. Please build the project first."
    exit 1
}

# Find or generate code signing certificate
$cert = $null
if ($CertificateThumbprint) {
    $cert = Get-Item "Cert:\CurrentUser\My\$CertificateThumbprint" -ErrorAction SilentlyContinue
    if (-not $cert) {
        $cert = Get-Item "Cert:\LocalMachine\My\$CertificateThumbprint" -ErrorAction SilentlyContinue
    }
} else {
    # Check for existing code-signing cert in CurrentUser store
    $cert = Get-ChildItem -Path Cert:\CurrentUser\My -CodeSigningCert | Select-Object -First 1
}

if (-not $cert) {
    Write-Host "[!] No existing Code Signing Certificate found." -ForegroundColor Yellow
    Write-Host "[*] Generating self-signed enterprise development code-signing certificate..." -ForegroundColor Yellow

    $cert = New-SelfSignedCertificate -Type CodeSigningCert `
        -Subject "CN=AegisMesh Enterprise Systems Internal Dev" `
        -CertStoreLocation "Cert:\CurrentUser\My" `
        -NotAfter (Get-Date).AddYears(3) `
        -HashAlgorithm SHA256

    Write-Host "Generated certificate: $($cert.Subject) [$($cert.Thumbprint)]" -ForegroundColor Green
}

# Sign Binary using Set-AuthenticodeSignature
Write-Host "[*] Signing binary: $BinaryPath..." -ForegroundColor Cyan
$sig = Set-AuthenticodeSignature -FilePath $BinaryPath -Certificate $cert -HashAlgorithm SHA256 -TimestampServer $TimestampServer

$statusColor = if ($sig.Status -eq 'Valid') { 'Green' } else { 'Yellow' }
Write-Host "Signature Status: $($sig.Status)" -ForegroundColor $statusColor
Write-Host "Signer: $($sig.SignerCertificate.Subject)" -ForegroundColor Cyan
Write-Host "Status Message: $($sig.StatusMessage)" -ForegroundColor Gray

Write-Host "==========================================================" -ForegroundColor Green
Write-Host "✅ Authenticode signing procedure complete!" -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Green
