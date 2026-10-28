@echo off
title SVLL Workstation Agent Uninstaller
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo ==========================================================
    echo [ERROR] Administrator elevation required.
    echo Please right-click 'Uninstall-Service.bat' and select 'Run as administrator'.
    echo ==========================================================
    pause
    exit /b 1
)

echo Stopping and removing SVLL Workstation Agent Service...
sc.exe stop AegisMeshAgent >nul 2>&1
sc.exe delete AegisMeshAgent >nul 2>&1
echo Service removed successfully.
pause
