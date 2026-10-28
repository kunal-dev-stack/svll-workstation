# =========================================================================
# Shree Vasu Logistics Limited - Admin Workstation Automated Installer
# Target: SVLL IT Support Workstation (Admin Edition v8.1)
# Developed by: Kunal Turkar
# Compatible with Windows 10/11 & Windows Server (x64)
# =========================================================================

param(
    [string]$ServerUrl = "http://192.168.10.11:5000"
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
Write-Host "   Version: Admin Edition v8.3 | Developed by Kunal Turkar" -ForegroundColor Cyan
Write-Host "=========================================================================" -ForegroundColor Cyan

# 2. Corporate Server Pre-configured (Zero Input Required)
if (-not $ServerUrl) {
    $ServerUrl = "http://192.168.10.11:5000"
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
    RelayServerUrl = $ServerUrl
} | ConvertTo-Json -Depth 5

Set-Content -Path $cfgPath -Value $secConfig -Force
Write-Host "[+] Configured Admin Security and Relay settings." -ForegroundColor Green

# 4. Terminate any running old version and install Admin Edition v8.3
Write-Host "`n[+] Closing any running previous versions of SVLL Workstation for clean in-place upgrade..." -ForegroundColor Cyan
Get-Process -Name "SVLL-IT-Workstation*" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 1

$downloadUrl = "https://github.com/kunal-dev-stack/svll-workstation/releases/download/v8.3/SVLL-IT-Workstation-Admin-v8.3-Setup.exe"
$tempExe = Join-Path $env:TEMP "SVLL-IT-Workstation-Admin-v8.3-Setup.exe"

Write-Host "[1/2] Downloading latest SVLL Workstation Admin v8.3..." -ForegroundColor Yellow
try {
    Start-BitsTransfer -Source $downloadUrl -Destination $tempExe -ErrorAction Stop
} catch {
    Invoke-WebRequest -Uri $downloadUrl -OutFile $tempExe -UseBasicParsing
}

# 5. Install Admin Workstation
Write-Host "[2/2] Performing in-place upgrade of Admin Console silently..." -ForegroundColor Yellow
$proc = Start-Process -FilePath $tempExe -ArgumentList "/CLOSEAPPLICATIONS /FORCECLOSEAPPLICATIONS /VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP-" -PassThru; $proc | Wait-Process -Timeout 45 -ErrorAction SilentlyContinue
if ($proc.ExitCode -ne 0 -and $proc.ExitCode -ne $null) {
    Write-Warning "Setup returned code: $($proc.ExitCode)"
}
Remove-Item $tempExe -Force -ErrorAction SilentlyContinue

# Ensure Admin PC Program Files has full tools including Flutter companion DLLs
$adminTools = if (Test-Path "C:\Program Files\ShreeVasuLogistics\ITWorkstationAdmin\tools") { "C:\Program Files\ShreeVasuLogistics\ITWorkstationAdmin\tools" } else { "C:\Program Files\ShreeVasuLogistics\ITWorkstation\tools" }
if (Test-Path $adminTools) {
    if (-not (Test-Path "C:\Program Files\RustDesk")) { New-Item -ItemType Directory -Path "C:\Program Files\RustDesk" -Force | Out-Null }
    Copy-Item -Path "$adminTools\*" -Destination "C:\Program Files\RustDesk" -Recurse -Force -ErrorAction SilentlyContinue
}

# 6. Ensure Ultra Remote Viewer is installed on Admin PC for 1-Click Connect
Write-Host "`nChecking Ultra Remote Viewer on Admin PC..." -ForegroundColor Yellow
$ultraCandidates = @(
    "C:\Program Files\RustDesk\rustdesk.exe",
    "C:\Program Files\ShreeVasuLogistics\ITWorkstationAdmin\tools\rustdesk.exe",
    "C:\Program Files (x86)\RustDesk\rustdesk.exe",
    "C:\Program Files (x86)\ShreeVasuLogistics\ITWorkstationAdmin\tools\rustdesk.exe",
    "$env:ProgramData\SVLL-IT-Workstation\tools\rustdesk.exe"
)
$ultraExe = $null
foreach ($p in $ultraCandidates) { if (Test-Path $p) { $ultraExe = $p; break } }

if (-not $ultraExe) {
    Write-Host "Downloading and configuring Ultra Remote Engine for Admin Console..." -ForegroundColor Yellow
    $toolsDir = "$env:ProgramData\SVLL-IT-Workstation\tools"
    if (-not (Test-Path $toolsDir)) { New-Item -ItemType Directory -Path $toolsDir -Force | Out-Null }
    $ultraExe = Join-Path $toolsDir "rustdesk.exe"
    try {
        Invoke-WebRequest -Uri "https://github.com/rustdesk/rustdesk/releases/download/1.3.7/rustdesk-1.3.7-x86_64.exe" -OutFile $ultraExe -UseBasicParsing -TimeoutSec 45
        $rdProc = Start-Process -FilePath $ultraExe -ArgumentList "--silent-install" -WorkingDirectory (Split-Path $ultraExe) -PassThru -WindowStyle Hidden; $rdProc | Wait-Process -Timeout 15 -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 2
        Write-Host "[+] Ultra Remote Viewer ready for 1-click connections." -ForegroundColor Green
    } catch {
        Write-Warning "Ultra Remote viewer install notice: $($_.Exception.Message)"
    }
} else {
    Write-Host "[+] Ultra Remote Viewer verified on Admin PC." -ForegroundColor Green
}

# Firewall rules for direct connection
netsh advfirewall firewall add rule name="UltraRemotePort21118" dir=in action=allow protocol=TCP localport=21118 enable=yes | Out-Null
netsh advfirewall firewall add rule name="UltraRemoteUDP" dir=in action=allow protocol=UDP localport=21118-21119 enable=yes | Out-Null
netsh advfirewall firewall add rule name="SvllMeshListenerPort5055" dir=in action=allow protocol=TCP localport=5055 enable=yes | Out-Null

# 7. Check AnyDesk as secondary fallback
Write-Host "`nChecking AnyDesk on Admin PC..." -ForegroundColor Yellow
$adExe = $null
$adPaths = @("C:\Program Files (x86)\AnyDesk\AnyDesk.exe", "C:\Program Files\AnyDesk\AnyDesk.exe")
foreach ($p in $adPaths) { if (Test-Path $p) { $adExe = $p; break } }

if (-not $adExe) {
    Write-Host "Downloading AnyDesk fallback for Admin Console..." -ForegroundColor Yellow
    $adTemp = Join-Path $env:TEMP "AnyDeskSetup.exe"
    try {
        Invoke-WebRequest -Uri "https://download.anydesk.com/AnyDesk.exe" -OutFile $adTemp -UseBasicParsing -TimeoutSec 30
        $adProc = Start-Process -FilePath $adTemp -ArgumentList "--install `"C:\Program Files (x86)\AnyDesk`" --start-with-win --silent" -PassThru -WindowStyle Hidden; $adProc | Wait-Process -Timeout 15 -ErrorAction SilentlyContinue
        Remove-Item $adTemp -Force -ErrorAction SilentlyContinue
        Write-Host "[+] AnyDesk ready as secondary fallback." -ForegroundColor Green
    } catch {
        Write-Warning "AnyDesk silent install skipped: $($_.Exception.Message)"
    }
} else {
    Write-Host "[+] AnyDesk verified on Admin PC." -ForegroundColor Green
}


# 7.5. Create Desktop Shortcut for Web Admin Console
$desktopPath = [Environment]::GetFolderPath("Desktop")
$shortcutFile = Join-Path $desktopPath "SVLL Fleet Web Console.url"
@"
[InternetShortcut]
URL=$ServerUrl
IconIndex=0
IconFile=C:\Windows\System32\shell32.dll
"@ | Set-Content -Path $shortcutFile -Force
Write-Host "[+] Created 'SVLL Fleet Web Console' shortcut on Administrator Desktop." -ForegroundColor Green

# 8. Launch Admin Workstation
$adminExe = if (Test-Path "$env:ProgramFiles\ShreeVasuLogistics\ITWorkstationAdmin\SVLL-IT-Workstation-Admin.exe") {
    "$env:ProgramFiles\ShreeVasuLogistics\ITWorkstationAdmin\SVLL-IT-Workstation-Admin.exe"
} else {
    "$env:ProgramFiles\ShreeVasuLogistics\ITWorkstation\SVLL-IT-Workstation-Admin.exe"
}
if (Test-Path $adminExe) {
    Start-Process $adminExe
}

Write-Host "`n=========================================================================" -ForegroundColor Green
Write-Host "   [+] SVLL ADMIN WORKSTATION v8.3 INSTALLED SUCCESSFULLY!" -ForegroundColor Green
Write-Host "=========================================================================" -ForegroundColor Green
Write-Host "   Edition:        Admin Edition (Full IT Control & Fleet Management)" -ForegroundColor White
Write-Host "   Relay Server:   $ServerUrl" -ForegroundColor Cyan
Write-Host "   Application:    $adminExe" -ForegroundColor White
Write-Host "   Shortcut:       Desktop & Start Menu" -ForegroundColor White
Write-Host "`n   👑 ADMIN FEATURES ACTIVE:" -ForegroundColor Green
Write-Host "       • Live Fleet Telemetry & Active Target Selection Deck" -ForegroundColor White
Write-Host "       • 1-Click Zero-Touch Remote Desktop (Ultra Remote) & Direct LAN" -ForegroundColor White
Write-Host "       • Real-Time IT Complaint Triage & Direct Toast Reply" -ForegroundColor White
Write-Host "       • AI IT Assistant & Fleet Management Tools" -ForegroundColor White

Write-Host "=========================================================================`n" -ForegroundColor Green
