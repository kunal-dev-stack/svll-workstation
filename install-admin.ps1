# =========================================================================
# Shree Vasu Logistics Limited - Admin Workstation Automated Installer
# Target: SVLL IT Support Workstation (Admin Edition v8.4)
# Developed by: Kunal Turkar
# Compatible with Windows 10/11 & Windows Server (x64)
# =========================================================================

param(
    [string]$ServerUrl = ""
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
Write-Host "   Version: Admin Edition v8.4 | Developed by Kunal Turkar" -ForegroundColor Cyan
Write-Host "=========================================================================" -ForegroundColor Cyan

# 2. Dynamic Server Discovery (LAN direct with Cloudflare Internet Fallback)
if (-not $ServerUrl) {
    $lanCandidate = "http://192.168.10.11:5000"
    try {
        $testLan = Invoke-WebRequest -Uri "$lanCandidate/api/health" -UseBasicParsing -TimeoutSec 2 -ErrorAction Stop
        if ($testLan.StatusCode -eq 200) {
            $ServerUrl = $lanCandidate
        }
    } catch { }
}

if (-not $ServerUrl) {
    $ServerUrl = "https://roberts-basis-early-likewise.trycloudflare.com"
}
$ServerUrl = $ServerUrl.TrimEnd('/')
Write-Host "[+] Connecting Admin Console to Gateway: $ServerUrl" -ForegroundColor Green

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
    RelayServerUrl = $ServerUrl
} | ConvertTo-Json -Depth 5

Set-Content -Path $cfgPath -Value $secConfig -Force
Write-Host "[+] Configured Admin Security and Relay settings." -ForegroundColor Green

# 4. Terminate any running old version and install Admin Edition v8.4
Write-Host "`n[+] Closing any running previous versions of SVLL Workstation for clean in-place upgrade..." -ForegroundColor Cyan
Get-Process -Name "SVLL-IT-Workstation*" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 1

$downloadCandidates = @(
    "https://github.com/kunal-dev-stack/svll-workstation/releases/download/v8.4/SVLL-IT-Workstation-Admin-v8.4-Setup.exe",
    "$ServerUrl/api/agent/admin-setup"
)
$tempExe = Join-Path $env:TEMP "SVLL-IT-Workstation-Admin-v8.4-Setup.exe"

Write-Host "[1/2] Downloading latest SVLL Workstation Admin v8.4..." -ForegroundColor Yellow
$downloadSuccess = $false
foreach ($u in $downloadCandidates) {
    try {
        Write-Host "    Connecting to: $u..." -ForegroundColor Yellow
        Invoke-WebRequest -Uri $u -OutFile $tempExe -UseBasicParsing -TimeoutSec 120 -ErrorAction Stop
        if ((Test-Path $tempExe) -and (Get-Item $tempExe).Length -gt 10000000) {
            $downloadSuccess = $true
            Write-Host "    Download completed successfully ($((Get-Item $tempExe).Length) bytes)." -ForegroundColor Green
            break
        }
    } catch {
        Write-Warning "    Download attempt failed: $_"
    }
}
if (-not $downloadSuccess) {
    Write-Error "Failed to download Admin Setup from all available endpoints."
    exit 1
}

# 5. Install Admin Workstation Silently
Write-Host "[2/2] Performing in-place upgrade of Admin Console silently..." -ForegroundColor Yellow
$proc = Start-Process -FilePath $tempExe -ArgumentList "/CLOSEAPPLICATIONS /FORCECLOSEAPPLICATIONS /VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP-" -PassThru; $proc | Wait-Process -Timeout 45 -ErrorAction SilentlyContinue
if ($proc.ExitCode -ne 0 -and $proc.ExitCode -ne $null) {
    Write-Warning "Setup returned code: $($proc.ExitCode)"
}
Remove-Item $tempExe -Force -ErrorAction SilentlyContinue

# 6. Create Desktop Shortcut for Web Admin Console
$desktopPath = [Environment]::GetFolderPath("Desktop")
$shortcutFile = Join-Path $desktopPath "SVLL Fleet Web Console.url"
@"
[InternetShortcut]
URL=$ServerUrl
IconIndex=0
IconFile=C:\Windows\System32\shell32.dll
"@ | Set-Content -Path $shortcutFile -Force
Write-Host "[+] Created 'SVLL Fleet Web Console' shortcut on Administrator Desktop." -ForegroundColor Green

# 7. Launch Admin Workstation
$adminExe = if (Test-Path "$env:ProgramFiles\ShreeVasuLogistics\ITWorkstationAdmin\SVLL-IT-Workstation-Admin.exe") {
    "$env:ProgramFiles\ShreeVasuLogistics\ITWorkstationAdmin\SVLL-IT-Workstation-Admin.exe"
} else {
    "$env:ProgramFiles\ShreeVasuLogistics\ITWorkstation\SVLL-IT-Workstation-Admin.exe"
}
if (Test-Path $adminExe) {
    Start-Process $adminExe
}

Write-Host "`n=========================================================================" -ForegroundColor Green
Write-Host "   [+] SVLL ADMIN WORKSTATION v8.4 INSTALLED SUCCESSFULLY!" -ForegroundColor Green
Write-Host "=========================================================================" -ForegroundColor Green
Write-Host "   Edition:        Admin Edition (Full IT Control & Fleet Management)" -ForegroundColor White
Write-Host "   Relay Server:   $ServerUrl" -ForegroundColor Cyan
Write-Host "   Application:    $adminExe" -ForegroundColor White
Write-Host "   Shortcut:       Desktop & Start Menu" -ForegroundColor White
Write-Host "`n   👑 ADMIN FEATURES ACTIVE:" -ForegroundColor Green
Write-Host "       • Live Fleet Telemetry & Active Target Selection Deck" -ForegroundColor White
Write-Host "       • 1-Click Zero-Touch Remote Desktop (AegisMesh) & Direct LAN" -ForegroundColor White
Write-Host "       • Real-Time IT Complaint Triage & Direct Toast Reply" -ForegroundColor White
Write-Host "       • AI IT Assistant & Fleet Management Tools" -ForegroundColor White
Write-Host "=========================================================================`n" -ForegroundColor Green
