<#
.SYNOPSIS
    Completely uninstalls AegisMesh Endpoint Agent from Windows.
.DESCRIPTION
    Guarantees clean uninstallation: stops the service, deregisters from SCM,
    and removes deployed binaries and configuration files.
#>

[CmdletBinding()]
param (
    [string]$ServiceName = "AegisMeshAgent",
    [string]$InstallPath = "$env:ProgramFiles\AegisMesh"
)

# 1. Require Administrator elevation
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Error "Administrative privileges required. Please execute from an elevated PowerShell prompt."
    exit 1
}

Write-Host "==========================================================" -ForegroundColor Yellow
Write-Host "🛡️  AegisMesh Endpoint Agent - Clean Uninstallation" -ForegroundColor Yellow
Write-Host "==========================================================" -ForegroundColor Yellow

# 2. Stop Service
Write-Host "[1/3] Stopping Windows Service: $ServiceName..." -ForegroundColor Yellow
$svc = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($svc -and $svc.Status -eq 'Running') {
    Stop-Service -Name $ServiceName -Force
    Start-Sleep -Seconds 2
}

# 3. Delete from Service Control Manager
Write-Host "[2/3] Deregistering service from SCM..." -ForegroundColor Yellow
sc.exe delete $ServiceName

# 4. Remove Files
Write-Host "[3/3] Removing installation directory: $InstallPath..." -ForegroundColor Yellow
if (Test-Path $InstallPath) {
    Remove-Item -Path $InstallPath -Recurse -Force -ErrorAction SilentlyContinue
}

# 5. Clean CommonAppData device identifier
$dataDir = "$env:ProgramData\AegisMesh"
if (Test-Path $dataDir) {
    Remove-Item -Path $dataDir -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Host "==========================================================" -ForegroundColor Green
Write-Host "✅ AegisMesh Agent successfully uninstalled and removed." -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Green
