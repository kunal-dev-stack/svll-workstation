# =========================================================================
# Shree Vasu Logistics Limited - Admin Workstation Automated Installer
# Target: SVLL IT Support Workstation (Admin Edition v8.0)
# Developed by: Kunal Turkar
# Compatible with Windows 10/11 & Windows Server (x64)
# =========================================================================

param(
    [string]$ServerUrl = "http://192.168.10.222:5000",
    [string]$MeshServerUrl = "http://192.168.10.222:8086"
)

$ErrorActionPreference = "Stop"
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

# 1. Require Administrator Elevation
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "[!] Elevation required. Requesting Administrator permissions..." -ForegroundColor Yellow
    $cliArgs = "-NoProfile -ExecutionPolicy Bypass -Command `"& { [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; irm https://raw.githubusercontent.com/kunal-dev-stack/svll-workstation/main/install-admin.ps1 | iex }`""
    Start-Process powershell.exe -ArgumentList $cliArgs -Verb RunAs
    exit 0
}

Clear-Host
Write-Host "=========================================================================" -ForegroundColor Cyan
Write-Host "   SHREE VASU LOGISTICS LIMITED - ADMIN CONSOLE DEPLOYMENT" -ForegroundColor Cyan
Write-Host "   Version: Admin Edition v8.0 | Developed by Kunal Turkar" -ForegroundColor Cyan
Write-Host "=========================================================================" -ForegroundColor Cyan

# 2. Corporate Server Pre-configured (Zero Input Required)
if (-not $ServerUrl) {
    $ServerUrl = "http://192.168.10.222:5000"
}
if (-not $MeshServerUrl) {
    $MeshServerUrl = "http://192.168.10.222:8086"
}
$ServerUrl = $ServerUrl.TrimEnd('/')
Write-Host "[+] Connecting Admin Console to Relay Server: $ServerUrl" -ForegroundColor Green

# 3. Configure Security Settings Profile
$programDataDir = "C:\ProgramData\SVLL-IT-Workstation"
$cfgPath = Join-Path $programDataDir "admin_security.json"
if (-not (Test-Path $programDataDir)) {
    New-Item -ItemType Directory -Path $programDataDir -Force | Out-Null
}

$existingConfig = $null
if (Test-Path $cfgPath) {
    try { $existingConfig = Get-Content $cfgPath -Raw | ConvertFrom-Json } catch { }
}

$secConfig = @{
    PasswordHash = if ($existingConfig -and $existingConfig.PasswordHash) { $existingConfig.PasswordHash } else { "" }
    PasswordSalt = if ($existingConfig -and $existingConfig.PasswordSalt) { $existingConfig.PasswordSalt } else { "" }
    LastPasswordChange = if ($existingConfig -and $existingConfig.LastPasswordChange) { $existingConfig.LastPasswordChange } else { (Get-Date).ToString("o") }
    SharedFleetDirectory = if ($existingConfig -and $existingConfig.SharedFleetDirectory) { $existingConfig.SharedFleetDirectory } else { "" }
    SharedTicketDirectory = if ($existingConfig -and $existingConfig.SharedTicketDirectory) { $existingConfig.SharedTicketDirectory } else { "" }
    RelayServerUrl = $ServerUrl
    MeshCentralUrl = if ($MeshServerUrl) { $MeshServerUrl } elseif ($existingConfig -and $existingConfig.MeshCentralUrl) { $existingConfig.MeshCentralUrl } else { "$ServerUrl:8086" }
} | ConvertTo-Json -Depth 5

Set-Content -Path $cfgPath -Value $secConfig -Force
Write-Host "[+] Configured Admin Security and Relay settings." -ForegroundColor Green

# 4. Download Admin Workstation v8.0 Setup
$downloadUrl = "https://github.com/kunal-dev-stack/svll-workstation/releases/download/v8.0/SVLL-IT-Workstation-Admin-v8.0-Setup.exe"
$tempExe = Join-Path $env:TEMP "SVLL-IT-Workstation-Admin-v8.0-Setup.exe"

Write-Host "`n[1/2] Downloading SVLL Workstation Admin v8.0..." -ForegroundColor Yellow
try {
    Start-BitsTransfer -Source $downloadUrl -Destination $tempExe -ErrorAction Stop
} catch {
    Invoke-WebRequest -Uri $downloadUrl -OutFile $tempExe -UseBasicParsing
}

# 5. Install Admin Workstation
Write-Host "[2/2] Installing Admin Console silently..." -ForegroundColor Yellow
$proc = Start-Process -FilePath $tempExe -ArgumentList "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP-" -Wait -PassThru
if ($proc.ExitCode -ne 0 -and $proc.ExitCode -ne $null) {
    Write-Warning "Setup returned non-zero code: $($proc.ExitCode)"
}
Remove-Item $tempExe -Force -ErrorAction SilentlyContinue

# 6. Launch Admin Workstation
$adminExe = "$env:ProgramFiles\ShreeVasuLogistics\ITWorkstation\SVLL-IT-Workstation-Admin.exe"
if (Test-Path $adminExe) {
    Start-Process $adminExe
}

Write-Host "`n=========================================================================" -ForegroundColor Green
Write-Host "   [+] SVLL ADMIN WORKSTATION v8.0 INSTALLED SUCCESSFULLY!" -ForegroundColor Green
Write-Host "=========================================================================" -ForegroundColor Green
Write-Host "   Edition:        Admin Edition (Full IT Control & Fleet Management)" -ForegroundColor White
Write-Host "   Relay Server:   $ServerUrl" -ForegroundColor Cyan
Write-Host "   Application:    $adminExe" -ForegroundColor White
Write-Host "   Shortcut:       Desktop & Start Menu" -ForegroundColor White
Write-Host "`n   👑 ADMIN FEATURES ACTIVE:" -ForegroundColor Green
Write-Host "       • Live Fleet Telemetry from all Employee PCs" -ForegroundColor White
Write-Host "       • 1-Click Zero-Password Remote Access via MeshCentral & AnyDesk" -ForegroundColor White
Write-Host "       • Real-Time IT Complaint Triage & Direct Toast Reply" -ForegroundColor White
Write-Host "       • AI IT Assistant & System Diagnostics Tools" -ForegroundColor White
Write-Host "=========================================================================`n" -ForegroundColor Green
