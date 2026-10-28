@echo off
setlocal enabledelayedexpansion
title SVLL Workstation Agent One-Click Installer v8.4

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
echo   SVLL Workstation Management Agent - Auto Installer v8.4
echo ==========================================================
echo Connecting to SVLL Gateway and provisioning workstation...
echo.

powershell -NoProfile -ExecutionPolicy Bypass -Command "& { [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; irm https://raw.githubusercontent.com/kunal-dev-stack/svll-workstation/main/install.ps1 | iex }"

pause
