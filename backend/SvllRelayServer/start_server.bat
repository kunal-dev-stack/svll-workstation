@echo off
echo ========================================================
echo   Starting SVLL IT Relay Server v1.0
echo   Port: 5000 (HTTP)
echo ========================================================
cd /d "%~dp0"
SvllRelayServer.exe --urls "http://0.0.0.0:5000"
pause
