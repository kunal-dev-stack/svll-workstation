@echo off
echo ========================================================
echo   Uninstalling SVLL IT Relay Server Windows Service
echo ========================================================
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo [ERROR] Please right-click and 'Run as administrator'.
    pause
    exit /b 1
)

sc.exe stop SvllRelay
sc.exe delete SvllRelay
echo.
echo [SUCCESS] SvllRelay Windows Service removed.
pause
