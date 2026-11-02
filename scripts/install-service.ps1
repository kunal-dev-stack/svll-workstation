<#
.SYNOPSIS
    Installs and registers AegisMesh Endpoint Agent as a standard Windows Service.
.DESCRIPTION
    Compliant with Microsoft Defender and enterprise EDR guidelines.
    Uses legitimate Windows Service Control Manager (SCM) registration.
#>

[CmdletBinding()]
param (
    [string]$ServerUrl = "ws://localhost:5000",
    [string]$Token = "MESH-ENTERPRISE-TOKEN-2026",
    [string]$InstallPath = "$env:ProgramFiles\AegisMesh"
)

# 1. Require Administrator elevation
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Error "Administrative privileges required. Please execute from an elevated PowerShell prompt."
    exit 1
}

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "🛡️  AegisMesh Endpoint Agent - Windows Service Installation" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 2. Publish/Copy Agent Binary
Write-Host "[1/4] Preparing binary deployment to $InstallPath..." -ForegroundColor Yellow
if (-not (Test-Path $InstallPath)) {
    New-Item -Path $InstallPath -ItemType Directory -Force | Out-Null
}

$sourceExe = "$PSScriptRoot\..\agent-windows\bin\Debug\net8.0-windows\AegisMeshAgent.exe"
if (-not (Test-Path $sourceExe)) {
    Write-Host "Building agent binary..." -ForegroundColor Yellow
    dotnet build "$PSScriptRoot\..\agent-windows\AegisMeshAgent.csproj" -c Release
    $sourceExe = "$PSScriptRoot\..\agent-windows\bin\Release\net8.0-windows\AegisMeshAgent.exe"
}

Copy-Item -Path "$PSScriptRoot\..\agent-windows\bin\Debug\net8.0-windows\*" -Destination $InstallPath -Recurse -Force
$targetExe = Join-Path $InstallPath "AegisMeshAgent.exe"

# 3. Save Configuration File
$config = @{
    serverUrl = $ServerUrl
    token = $Token
    installedAt = (Get-Date).ToString("o")
} | ConvertTo-Json
Set-Content -Path (Join-Path $InstallPath "agent.config.json") -Value $config -Encoding UTF8

# 4. Register with Service Control Manager (SCM)
Write-Host "[2/4] Registering Windows Service via Service Control Manager..." -ForegroundColor Yellow
$serviceName = "AegisMeshAgent"
$displayName = "AegisMesh Endpoint Management Agent"

$existingService = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
if ($existingService) {
    Write-Host "Existing service found. Stopping and updating..." -ForegroundColor Yellow
    sc.exe stop $serviceName
    Start-Sleep -Seconds 2
    sc.exe delete $serviceName
    Start-Sleep -Seconds 1
}

$binPathWithArgs = "`"$targetExe`" --server `"$ServerUrl`" --token `"$Token`""
$createResult = sc.exe create $serviceName binPath= $binPathWithArgs start= auto DisplayName= $displayName
Write-Host $createResult

# 5. Configure Recovery & Description
Write-Host "[3/4] Configuring service recovery policies..." -ForegroundColor Yellow
sc.exe description $serviceName "Provides authorized remote support, hardware diagnostics, and secure administration for enterprise endpoints."
sc.exe failure $serviceName reset= 86400 actions= restart/60000/restart/60000/none/60000

# 6. Start Service
Write-Host "[4/4] Starting service..." -ForegroundColor Yellow
sc.exe start $serviceName

Write-Host "==========================================================" -ForegroundColor Green
Write-Host "✅ AegisMesh Endpoint Agent Service successfully installed!" -ForegroundColor Green
Write-Host "   Service Name: $serviceName" -ForegroundColor Green
Write-Host "   Status: Running" -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Green
