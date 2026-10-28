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
echo Registering service with Windows Service Control Manager...
sc.exe stop AegisMeshAgent >nul 2>&1
sc.exe delete AegisMeshAgent >nul 2>&1
timeout /t 1 /nobreak >nul

sc.exe create AegisMeshAgent binPath= "\"%TARGET_DIR%\AegisMeshAgent.exe\" --server %SERVER_URL% --token %TOKEN%" start= auto DisplayName= "SVLL Workstation Management Agent"
sc.exe description AegisMeshAgent "Provides authorized remote support, hardware diagnostics, and secure administration for enterprise endpoints."
sc.exe failure AegisMeshAgent reset= 86400 actions= restart/60000/restart/60000/none/60000

echo Starting service...
sc.exe start AegisMeshAgent

echo ==========================================================
echo [SUCCESS] SVLL Workstation Agent installed and running!
echo Target Server: %SERVER_URL%
echo ==========================================================
pause
