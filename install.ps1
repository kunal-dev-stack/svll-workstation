# Require Administrator
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "[ERROR] Administrator rights required. Run PowerShell as Administrator." -ForegroundColor Red
    return
}

Write-Host "========================================================" -ForegroundColor Cyan
Write-Host "  Installing SVLL IT Support Workstation v5.0..." -ForegroundColor Cyan
Write-Host "========================================================" -ForegroundColor Cyan

$installerUrl = "https://github.com/kunal-dev-stack/svll-workstation/releases/download/v5.0/SVLL-IT-Workstation-v5.0-Setup.exe"
$localTempPath = Join-Path $env:TEMP "SVLL-IT-Setup.exe"

# Terminate running instance if upgrading
Stop-Process -Name "SVLL-IT-Workstation" -Force -ErrorAction SilentlyContinue

Write-Host "[1/2] Downloading installer from GitHub Releases..." -ForegroundColor Yellow
Invoke-WebRequest -Uri $installerUrl -OutFile $localTempPath -UseBasicParsing

Write-Host "[2/2] Installing silently to Program Files..." -ForegroundColor Yellow
$proc = Start-Process -FilePath $localTempPath -ArgumentList "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP-" -PassThru -Wait

Remove-Item -Path $localTempPath -Force -ErrorAction SilentlyContinue

if ($proc.ExitCode -eq 0) {
    Write-Host "========================================================" -ForegroundColor Green
    Write-Host "✔ SUCCESS: Workstation installed on Desktop & Start Menu!" -ForegroundColor Green
    Write-Host "========================================================" -ForegroundColor Green
} else {
    Write-Host "[ERROR] Installation failed with exit code: $($proc.ExitCode)" -ForegroundColor Red
}
