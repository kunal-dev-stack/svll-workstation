@echo off
title SVLL Workstation Agent (Interactive Mode)
set SERVER_URL=%1
if "%SERVER_URL%"=="" set SERVER_URL=ws://192.168.10.11:5000

set TOKEN=%2
if "%TOKEN%"=="" set TOKEN=MESH-ENTERPRISE-TOKEN-2026

echo ==========================================================
echo   SVLL Workstation Endpoint Agent (Interactive Mode)
echo ==========================================================
echo Connecting to Central Gateway (%SERVER_URL%)...
"%~dp0AegisMeshAgent.exe" --server %SERVER_URL% --token %TOKEN%
pause
