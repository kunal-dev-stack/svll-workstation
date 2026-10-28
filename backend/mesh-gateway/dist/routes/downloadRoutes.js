"use strict";
var __importDefault = (this && this.__importDefault) || function (mod) {
    return (mod && mod.__esModule) ? mod : { "default": mod };
};
Object.defineProperty(exports, "__esModule", { value: true });
const express_1 = require("express");
const path_1 = __importDefault(require("path"));
const fs_1 = __importDefault(require("fs"));
const router = (0, express_1.Router)();
const PUBLIC_DIR = path_1.default.join(__dirname, '..', '..', 'public');
const ZIP_PATH = path_1.default.join(PUBLIC_DIR, 'AegisMesh-Agent-Windows.zip');
// Direct ZIP download
router.get('/windows-agent', (req, res) => {
    if (fs_1.default.existsSync(ZIP_PATH)) {
        res.download(ZIP_PATH, 'AegisMesh-Agent-Windows.zip');
    }
    else {
        res.status(404).json({ error: 'Agent package not found on server' });
    }
});
// One-line PowerShell web installer for target systems
router.get('/install.ps1', (req, res) => {
    const host = req.headers.host || 'localhost:5000';
    const protocol = req.protocol === 'https' ? 'https' : 'http';
    const wsProtocol = req.protocol === 'https' ? 'wss' : 'ws';
    const token = req.query.token || 'MESH-ENTERPRISE-TOKEN-2026';
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
    Write-Host "[3/4] Registering 24/7 Auto-Start Task (SYSTEM)..." -ForegroundColor Yellow
    sc.exe stop AegisMeshAgent >nul 2>&1
    sc.exe delete AegisMeshAgent >nul 2>&1

    $taskCmd = '\"' + $exePath + '\" --server \"' + $wsUrl + '\" --token \"' + $token + '\"'
    schtasks.exe /create /tn "AegisMeshAgent" /tr $taskCmd /sc onstart /ru "SYSTEM" /rl HIGHEST /f | Out-Null

    Stop-Process -Name "AegisMeshAgent" -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 1
    Write-Host "[4/4] Launching AegisMeshAgent daemon..." -ForegroundColor Yellow
    Start-Process -FilePath $exePath -ArgumentList @("--server", $wsUrl, "--token", $token) -WindowStyle Hidden
} else {
    Write-Host "[3/4] Registering User Startup..." -ForegroundColor Yellow
    $taskCmd = '\"' + $exePath + '\" --server \"' + $wsUrl + '\" --token \"' + $token + '\"'
    schtasks.exe /create /tn "AegisMeshAgent" /tr $taskCmd /sc onlogon /rl HIGHEST /f | Out-Null

    Stop-Process -Name "AegisMeshAgent" -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 1
    Write-Host "[4/4] Launching agent in background..." -ForegroundColor Yellow
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
router.get('/install.bat', (req, res) => {
    const host = req.headers.host || 'localhost:5000';
    const protocol = req.protocol === 'https' ? 'https' : 'http';
    const wsProtocol = req.protocol === 'https' ? 'wss' : 'ws';
    const token = req.query.token || 'MESH-ENTERPRISE-TOKEN-2026';
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

echo [3/3] Registering auto-start and launching background agent...
sc.exe stop AegisMeshAgent >nul 2>&1
sc.exe delete AegisMeshAgent >nul 2>&1

schtasks /create /tn "AegisMeshAgent" /tr "\"%TARGET_DIR%\\AegisMeshAgent.exe\" --server %SERVER_URL% --token %TOKEN%" /sc onstart /ru "SYSTEM" /rl HIGHEST /f >nul 2>&1
taskkill /f /im AegisMeshAgent.exe >nul 2>&1
timeout /t 1 /nobreak >nul
start "" /B "%TARGET_DIR%\\AegisMeshAgent.exe" --server %SERVER_URL% --token %TOKEN%

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
exports.default = router;
