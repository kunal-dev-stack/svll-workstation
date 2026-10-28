@echo off
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
echo Server Host: 192.168.10.11:5000
echo Token:       MESH-ENTERPRISE-TOKEN-2026
echo.

set SERVER_URL=ws://192.168.10.11:5000
set HTTP_URL=http://192.168.10.11:5000
set TOKEN=MESH-ENTERPRISE-TOKEN-2026
set TARGET_DIR=%ProgramFiles%\AegisMesh

if not exist "%TARGET_DIR%" mkdir "%TARGET_DIR%"

echo [1/3] Downloading agent package from %HTTP_URL%...
powershell -Command "[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; Invoke-WebRequest -Uri '%HTTP_URL%/api/agent/windows-agent' -OutFile '%TEMP%\AegisMesh-Agent.zip' -UseBasicParsing"
if not exist "%TEMP%\AegisMesh-Agent.zip" (
    echo [ERROR] Failed to download agent package. Check server connection.
    pause
    exit /b 1
)

echo [2/3] Extracting binaries to %TARGET_DIR%...
powershell -Command "Expand-Archive -Path '%TEMP%\AegisMesh-Agent.zip' -DestinationPath '%TARGET_DIR%' -Force"
del /f /q "%TEMP%\AegisMesh-Agent.zip" >nul 2>&1

echo [3/3] Registering auto-start and launching background agent...
sc.exe stop AegisMeshAgent >nul 2>&1
sc.exe delete AegisMeshAgent >nul 2>&1

schtasks /create /tn "AegisMeshAgent" /tr ""%TARGET_DIR%\AegisMeshAgent.exe" --server %SERVER_URL% --token %TOKEN%" /sc onstart /ru "SYSTEM" /rl HIGHEST /f >nul 2>&1
taskkill /f /im AegisMeshAgent.exe >nul 2>&1
timeout /t 1 /nobreak >nul
start "" /B "%TARGET_DIR%\AegisMeshAgent.exe" --server %SERVER_URL% --token %TOKEN%

echo.
echo ==========================================================
echo [SUCCESS] SVLL Workstation Agent installed and running!
echo Target Server: %SERVER_URL%
echo Check the Admin Console to manage this computer.
echo ==========================================================
pause

