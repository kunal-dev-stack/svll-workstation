@echo off
echo ========================================================
echo   Installing SVLL IT Relay Server as Windows Service
echo ========================================================
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo [ERROR] Please right-click and 'Run as administrator'.
    pause
    exit /b 1
)

cd /d "%~dp0"
set BINPATH=%~dp0SvllRelayServer.exe --urls http://0.0.0.0:5000

netsh advfirewall firewall add rule name="SVLL Relay Server" dir=in action=allow protocol=TCP localport=5000 >nul 2>&1
sc.exe stop SvllRelay >nul 2>&1
sc.exe delete SvllRelay >nul 2>&1
sc.exe create SvllRelay binpath= "%BINPATH%" start= auto
sc.exe description SvllRelay "SVLL IT Workstation v8.0 - Fleet Relay Server"
sc.exe start SvllRelay

echo.
echo [SUCCESS] SvllRelay Windows Service installed and started!
echo Check health: http://localhost:5000/api/health
pause
