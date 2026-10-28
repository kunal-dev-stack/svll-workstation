@echo off
setlocal enabledelayedexpansion
title SVLL Workstation Agent Service Installer

:: Check for administrative rights
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo ==========================================================
    echo [ERROR] Administrator elevation required.
    echo Please right-click 'Install-Service.bat' and select 'Run as administrator'.
    echo ==========================================================
    pause
    exit /b 1
)

echo ==========================================================
echo   Installing SVLL Workstation Agent as a Windows Service
echo ==========================================================

set TARGET_DIR=%ProgramFiles%\AegisMesh
if not exist "%TARGET_DIR%" mkdir "%TARGET_DIR%"

echo Copying binaries to %TARGET_DIR%...
xcopy /E /I /Y "%~dp0*" "%TARGET_DIR%\" >nul

set SERVER_URL=%1
if "%SERVER_URL%"=="" set SERVER_URL=ws://192.168.10.11:5000

set TOKEN=%2
if "%TOKEN%"=="" set TOKEN=MESH-ENTERPRISE-TOKEN-2026

echo Target Gateway: %SERVER_URL%
echo Enrollment Token: %TOKEN%
echo Registering 24/7 Auto-Start Task (SYSTEM)...
sc.exe stop AegisMeshAgent >nul 2>&1
sc.exe delete AegisMeshAgent >nul 2>&1

schtasks /create /tn "AegisMeshAgent" /tr "\"%TARGET_DIR%\AegisMeshAgent.exe\" --server %SERVER_URL% --token %TOKEN%" /sc onstart /ru "SYSTEM" /rl HIGHEST /f >nul 2>&1
taskkill /f /im AegisMeshAgent.exe >nul 2>&1
timeout /t 1 /nobreak >nul

echo Starting agent in background...
start "" /B "%TARGET_DIR%\AegisMeshAgent.exe" --server %SERVER_URL% --token %TOKEN%

echo ==========================================================
echo [SUCCESS] SVLL Workstation Agent installed and running!
echo Target Server: %SERVER_URL%
echo ==========================================================
pause
