import { Router, Request, Response } from 'express';
import path from 'path';
import fs from 'fs';

const router = Router();
const PUBLIC_DIR = path.join(__dirname, '..', '..', 'public');
const ZIP_PATH = path.join(PUBLIC_DIR, 'AegisMesh-Agent-Windows.zip');

// Direct ZIP download
router.get('/windows-agent', (req: Request, res: Response): void => {
  if (fs.existsSync(ZIP_PATH)) {
    res.download(ZIP_PATH, 'AegisMesh-Agent-Windows.zip');
  } else {
    res.status(404).json({ error: 'Agent package not found on server' });
  }
});

// One-line PowerShell web installer for target systems
router.get('/install.ps1', (req: Request, res: Response): void => {
  const host = req.headers.host || 'localhost:5000';
  const protocol = req.protocol === 'https' ? 'https' : 'http';
  const wsProtocol = req.protocol === 'https' ? 'wss' : 'ws';
  const token = (req.query.token as string) || 'MESH-ENTERPRISE-TOKEN-2026';

  const script = `# ====================================================================
# SVLL Workstation / AegisMesh Endpoint Agent - One-Click Web Installer
# ====================================================================
$ErrorActionPreference = 'Stop'

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "🛡️  SVLL Workstation Management Agent - Setup" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

$serverHost = "${host}"
$serverUrl = "${protocol}://${host}"
$wsUrl = "${wsProtocol}://${host}"
$token = "${token}"
$installDir = "$env:ProgramFiles\\AegisMesh"

# 1. Require Administrator elevation
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Warning "Administrative elevation recommended. Installing in User Mode..."
    $installDir = "$env:LocalAppData\\AegisMesh"
}

# 2. Prepare installation directory
if (-not (Test-Path $installDir)) {
    New-Item -ItemType Directory -Path $installDir -Force | Out-Null
}

$zipDest = "$env:TEMP\\AegisMesh-Agent.zip"
$downloadUrl = "$serverUrl/api/agent/windows-agent"

Write-Host "[1/4] Downloading latest agent package from $downloadUrl..." -ForegroundColor Yellow
Invoke-WebRequest -Uri $downloadUrl -OutFile $zipDest -UseBasicParsing

Write-Host "[2/4] Extracting package to $installDir..." -ForegroundColor Yellow
Expand-Archive -Path $zipDest -DestinationPath $installDir -Force
Remove-Item -Path $zipDest -Force -ErrorAction SilentlyContinue

# 3. Create or Update Windows Service (if Admin) or Scheduled Auto-Start
$exePath = Join-Path $installDir "AegisMeshAgent.exe"
if (-not (Test-Path $exePath)) {
    Write-Error "AegisMeshAgent.exe not found after extraction."
    exit 1
}

if ($isAdmin) {
    Write-Host "[3/4] Registering Windows Service (AegisMeshAgent)..." -ForegroundColor Yellow
    sc.exe stop AegisMeshAgent >nul 2>&1
    sc.exe delete AegisMeshAgent >nul 2>&1
    Start-Sleep -Seconds 1

    $binPath = '"{0}" --server "{1}" --token "{2}"' -f $exePath, $wsUrl, $token
    sc.exe create AegisMeshAgent binPath= $binPath start= auto DisplayName= "SVLL Workstation Management Agent"
    sc.exe description AegisMeshAgent "SVLL Workstation remote device management and support service."
    sc.exe failure AegisMeshAgent reset= 86400 actions= restart/60000/restart/60000/none/60000

    Write-Host "[4/4] Starting AegisMeshAgent service..." -ForegroundColor Yellow
    sc.exe start AegisMeshAgent
} else {
    Write-Host "[3/4] Launching agent in background..." -ForegroundColor Yellow
    Start-Process -FilePath $exePath -ArgumentList @("--server", $wsUrl, "--token", $token) -WindowStyle Hidden
}

Write-Host "==========================================================" -ForegroundColor Green
Write-Host "✅ Installation Complete! Device will appear on the Admin Console." -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Green
`;

  res.setHeader('Content-Type', 'text/plain; charset=utf-8');
  res.send(script);
});

// One-click Batch file web installer for non-technical users
router.get('/install.bat', (req: Request, res: Response): void => {
  const host = req.headers.host || 'localhost:5000';
  const protocol = req.protocol === 'https' ? 'https' : 'http';
  const wsProtocol = req.protocol === 'https' ? 'wss' : 'ws';
  const token = (req.query.token as string) || 'MESH-ENTERPRISE-TOKEN-2026';

  const batchScript = `@echo off
setlocal enabledelayedexpansion
title SVLL Workstation Agent One-Click Installer

net session >nul 2>&1
if %errorLevel% neq 0 (
    echo ==========================================================
    echo [ERROR] Administrator elevation required.
    echo Please right-click this file and select 'Run as administrator'.
    echo ==========================================================
    pause
    exit /b 1
)

echo ==========================================================
echo   SVLL Workstation Management Agent - Auto Installer
echo ==========================================================
echo Server Host: ${host}
echo Token:       ${token}
echo.

set SERVER_URL=${wsProtocol}://${host}
set HTTP_URL=${protocol}://${host}
set TOKEN=${token}
set TARGET_DIR=%ProgramFiles%\\AegisMesh

if not exist "%TARGET_DIR%" mkdir "%TARGET_DIR%"

echo [1/3] Downloading agent package from %HTTP_URL%...
powershell -Command "[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; Invoke-WebRequest -Uri '%HTTP_URL%/api/agent/windows-agent' -OutFile '%TEMP%\\AegisMesh-Agent.zip' -UseBasicParsing"
if not exist "%TEMP%\\AegisMesh-Agent.zip" (
    echo [ERROR] Failed to download agent package. Check server connection.
    pause
    exit /b 1
)

echo [2/3] Extracting binaries to %TARGET_DIR%...
powershell -Command "Expand-Archive -Path '%TEMP%\\AegisMesh-Agent.zip' -DestinationPath '%TARGET_DIR%' -Force"
del /f /q "%TEMP%\\AegisMesh-Agent.zip" >nul 2>&1

echo [3/3] Registering and starting Windows Service...
sc.exe stop AegisMeshAgent >nul 2>&1
sc.exe delete AegisMeshAgent >nul 2>&1
timeout /t 1 /nobreak >nul

sc.exe create AegisMeshAgent binPath= "\\\"%TARGET_DIR%\\AegisMeshAgent.exe\\\" --server %SERVER_URL% --token %TOKEN%" start= auto DisplayName= "SVLL Workstation Management Agent"
sc.exe description AegisMeshAgent "SVLL Workstation remote device management and support service."
sc.exe failure AegisMeshAgent reset= 86400 actions= restart/60000/restart/60000/none/60000

sc.exe start AegisMeshAgent

echo.
echo ==========================================================
echo [SUCCESS] SVLL Workstation Agent installed and running!
echo Target Server: %SERVER_URL%
echo Check the Admin Console to manage this computer.
echo ==========================================================
pause
`;

  res.setHeader('Content-Type', 'text/plain; charset=utf-8');
  res.setHeader('Content-Disposition', 'attachment; filename=install.bat');
  res.send(batchScript);
});

export default router;
