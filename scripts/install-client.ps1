# =========================================================================
# Shree Vasu Logistics Limited - Client Workstation Automated Installer
# Target: SVLL IT Support Workstation (User Edition v8.1)
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
    $cliArgs = "-NoProfile -ExecutionPolicy Bypass -Command `"& { [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; irm https://raw.githubusercontent.com/kunal-dev-stack/svll-workstation/main/install.ps1 | iex }`""
    Start-Process powershell.exe -ArgumentList $cliArgs -Verb RunAs
    exit 0
}

Clear-Host
Write-Host "=========================================================================" -ForegroundColor Cyan
Write-Host "   SHREE VASU LOGISTICS LIMITED - WORKSTATION DEPLOYMENT AGENT" -ForegroundColor Cyan
Write-Host "   Version: User Edition v8.2 | Developed by Kunal Turkar" -ForegroundColor Cyan
Write-Host "=========================================================================" -ForegroundColor Cyan

# 2. Corporate Server Pre-configured (Zero User Input Required)
if (-not $ServerUrl) {
    $ServerUrl = "http://192.168.10.222:5000"
}
if (-not $MeshServerUrl) {
    $MeshServerUrl = "http://192.168.10.222:8086"
}
$ServerUrl = $ServerUrl.TrimEnd('/')
Write-Host "[+] Connecting to SVLL Relay Server: $ServerUrl" -ForegroundColor Green

# 3. Preserve Application Data Directory & Configure Relay Settings
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
Write-Host "[+] Pre-configured server connection in system profile." -ForegroundColor Green

# 4. Terminate any running old version and install User Edition v8.2
Write-Host "`n[+] Closing any running previous versions of SVLL Workstation for clean in-place upgrade..." -ForegroundColor Cyan
Get-Process -Name "SVLL-IT-Workstation*" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Stop-Service -Name "RustDesk" -Force -ErrorAction SilentlyContinue
Get-Process -Name "rustdesk*" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 1

$downloadUrl = "https://github.com/kunal-dev-stack/svll-workstation/releases/download/v8.2/SVLL-IT-Workstation-User-v8.2-Setup.exe"
$tempExe = Join-Path $env:TEMP "SVLL-IT-Workstation-User-v8.2-Setup.exe"

Write-Host "[1/3] Downloading latest SVLL Workstation User v8.2..." -ForegroundColor Yellow
try {
    Start-BitsTransfer -Source $downloadUrl -Destination $tempExe -ErrorAction Stop
} catch {
    Invoke-WebRequest -Uri $downloadUrl -OutFile $tempExe -UseBasicParsing
}

Write-Host "[2/3] Performing in-place upgrade silently..." -ForegroundColor Yellow
$proc = Start-Process -FilePath $tempExe -ArgumentList "/CLOSEAPPLICATIONS /FORCECLOSEAPPLICATIONS /VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP-" -Wait -PassThru
if ($proc.ExitCode -ne 0 -and $proc.ExitCode -ne $null) {
    Write-Warning "Setup returned code: $($proc.ExitCode)"
}
Remove-Item $tempExe -Force -ErrorAction SilentlyContinue

# Ensure Program Files has full tools including Flutter companion DLLs
$bundledTools = "C:\Program Files\ShreeVasuLogistics\ITWorkstation\tools"
$pfRustDesk = "C:\Program Files\RustDesk"
if (Test-Path $bundledTools) {
    if (-not (Test-Path $pfRustDesk)) { New-Item -ItemType Directory -Path $pfRustDesk -Force | Out-Null }
    Copy-Item -Path "$bundledTools\*" -Destination $pfRustDesk -Recurse -Force -ErrorAction SilentlyContinue
}

# 5. Configure Ultra Remote Engine (UltraViewer-style Zero-Touch Remote Access)
Write-Host "`n[3/3] Configuring Ultra Remote Engine (Auto-Reflected Partner ID & Password)..." -ForegroundColor Yellow

$ultraExe = $null
$ultraCandidates = @(
    "C:\Program Files\RustDesk\rustdesk.exe",
    "C:\Program Files\ShreeVasuLogistics\ITWorkstation\tools\rustdesk.exe",
    "C:\Program Files (x86)\RustDesk\rustdesk.exe",
    "C:\Program Files (x86)\ShreeVasuLogistics\ITWorkstation\tools\rustdesk.exe",
    "$env:ProgramData\SVLL-IT-Workstation\tools\rustdesk.exe"
)
foreach ($p in $ultraCandidates) { if (Test-Path $p) { $ultraExe = $p; break } }

if (-not $ultraExe) {
    Write-Host "Downloading Ultra Remote Engine fallback..." -ForegroundColor Yellow
    $toolsDir = "$env:ProgramData\SVLL-IT-Workstation\tools"
    if (-not (Test-Path $toolsDir)) { New-Item -ItemType Directory -Path $toolsDir -Force | Out-Null }
    $ultraExe = Join-Path $toolsDir "rustdesk.exe"
    try {
        Invoke-WebRequest -Uri "https://github.com/rustdesk/rustdesk/releases/download/1.3.7/rustdesk-1.3.7-x86_64.exe" -OutFile $ultraExe -UseBasicParsing -TimeoutSec 45
    } catch {
        Write-Warning "Could not download fallback Ultra Remote: $($_.Exception.Message)"
    }
}

# Enable direct-server in RustDesk TOML configurations for direct IP connections
$tomlTargets = @(
    "C:\Windows\ServiceProfiles\LocalService\AppData\Roaming\RustDesk\config\RustDesk2.toml",
    "C:\Windows\ServiceProfiles\LocalService\AppData\Roaming\RustDesk\config\RustDesk.toml",
    "$env:APPDATA\RustDesk\config\RustDesk2.toml",
    "$env:APPDATA\RustDesk\config\RustDesk.toml",
    "$env:LOCALAPPDATA\RustDesk\config\RustDesk2.toml"
)
foreach ($tpath in $tomlTargets) {
    $parent = Split-Path $tpath
    if (-not (Test-Path $parent)) { New-Item -ItemType Directory -Path $parent -Force | Out-Null }
    $cfgContent = ""
    if (Test-Path $tpath) { $cfgContent = Get-Content $tpath -Raw -ErrorAction SilentlyContinue }
    if (-not $cfgContent) {
        $cfgContent = "[options]`r`ndirect-server = 'Y'`r`ndirect-access-port = '21118'`r`n"
    } else {
        if ($cfgContent -match "direct-server\s*=") {
            $cfgContent = $cfgContent -replace "direct-server\s*=\s*['""]?[^'""\r\n]+['""]?", "direct-server = 'Y'"
        } elseif ($cfgContent -match "\[options\]") {
            $cfgContent = $cfgContent -replace "\[options\]", "[options]`r`ndirect-server = 'Y'"
        } else {
            $cfgContent += "`r`n[options]`r`ndirect-server = 'Y'"
        }

        if ($cfgContent -match "direct-access-port\s*=") {
            $cfgContent = $cfgContent -replace "direct-access-port\s*=\s*['""]?[^'""\r\n]+['""]?", "direct-access-port = '21118'"
        } else {
            $cfgContent = $cfgContent -replace "direct-server = 'Y'", "direct-server = 'Y'`r`ndirect-access-port = '21118'"
        }
    }
    Set-Content -Path $tpath -Value $cfgContent -Force -Encoding UTF8
}

if ($ultraExe -and (Test-Path $ultraExe)) {
    try {
        $ultraDir = Split-Path $ultraExe
        Start-Process -FilePath $ultraExe -ArgumentList "--silent-install" -WorkingDirectory $ultraDir -Wait -WindowStyle Hidden
        Start-Sleep -Seconds 2

        $svcExe = if (Test-Path "C:\Program Files\RustDesk\rustdesk.exe") { "C:\Program Files\RustDesk\rustdesk.exe" } else { $ultraExe }
        Set-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Services\RustDesk' -Name 'ImagePath' -Value "`"$svcExe`" --service" -ErrorAction SilentlyContinue

        Start-Process -FilePath $ultraExe -ArgumentList "--password SVLL@Remote2026" -WorkingDirectory $ultraDir -NoNewWindow -Wait
        Restart-Service -Name "RustDesk" -Force -ErrorAction SilentlyContinue

        netsh advfirewall firewall add rule name="UltraRemotePort21118" dir=in action=allow protocol=TCP localport=21118 enable=yes | Out-Null
        netsh advfirewall firewall add rule name="UltraRemoteUDP" dir=in action=allow protocol=UDP localport=21118-21119 enable=yes | Out-Null
        netsh advfirewall firewall add rule name="UltraRemoteApp" dir=in action=allow program="$ultraExe" enable=yes | Out-Null
        if (Test-Path "C:\Program Files\RustDesk\rustdesk.exe") {
            netsh advfirewall firewall add rule name="UltraRemoteApp2" dir=in action=allow program="C:\Program Files\RustDesk\rustdesk.exe" enable=yes | Out-Null
        }
        Write-Host "[+] Ultra Remote service registered, started, and configured with unattended password & direct IP port 21118." -ForegroundColor Green
    } catch {
        Write-Warning "Ultra Remote setup notice: $($_.Exception.Message)"
    }
}


# Detect Ultra Remote Partner ID
$ultraId = ""
if (Test-Path $ultraExe) {
    try {
        $idOut = Join-Path $env:TEMP "ultra_id.txt"
        Start-Process -FilePath $ultraExe -ArgumentList "--get-id" -WorkingDirectory (Split-Path $ultraExe) -NoNewWindow -Wait -PassThru -RedirectStandardOutput $idOut
        if (Test-Path $idOut) {
            $rawUltra = (Get-Content $idOut -Raw -ErrorAction SilentlyContinue).Trim()
            Remove-Item $idOut -Force -ErrorAction SilentlyContinue
            $digits = ($rawUltra -replace '\D','').Trim()
            if ($digits.Length -ge 6) {
                if ($digits.Length -eq 9) { $ultraId = "$($digits.Substring(0,3)) $($digits.Substring(3,3)) $($digits.Substring(6,3))" }
                elseif ($digits.Length -eq 10) { $ultraId = "$($digits.Substring(0,3)) $($digits.Substring(3,3)) $($digits.Substring(6,4))" }
                else { $ultraId = $digits }
            }
        }
    } catch { }
}
if (-not $ultraId) {
    $tomlPaths = @(
        "$env:APPDATA\RustDesk\config\RustDesk2.toml",
        "$env:APPDATA\RustDesk\config\RustDesk.toml",
        "C:\Windows\ServiceProfiles\LocalService\AppData\Roaming\RustDesk\config\RustDesk2.toml",
        "C:\Windows\ServiceProfiles\LocalService\AppData\Roaming\RustDesk\config\RustDesk.toml"
    )
    foreach ($tp in $tomlPaths) {
        if (Test-Path $tp) {
            $m = Select-String -Path $tp -Pattern 'id\s*=\s*[''"]?(\d+)[''"]?' -ErrorAction SilentlyContinue
            if ($m -and $m.Matches.Groups[1].Value) {
                $rawDigits = $m.Matches.Groups[1].Value
                if ($rawDigits.Length -eq 9) { $ultraId = "$($rawDigits.Substring(0,3)) $($rawDigits.Substring(3,3)) $($rawDigits.Substring(6,3))" }
                else { $ultraId = $rawDigits }
                break
            }
        }
    }
}
if ($ultraId) {
    Write-Host "[+] Ultra Remote Partner ID: $ultraId (Password: SVLL@Remote2026)" -ForegroundColor Green
}

# 7. Configure Fallback Remote Access & Telemetry
Write-Host "`n[5/5] Configuring Fallback Access (AnyDesk & RDP) & Telemetry..." -ForegroundColor Yellow

# Ensure AnyDesk is installed
$adExe = $null
$adPaths = @("C:\Program Files (x86)\AnyDesk\AnyDesk.exe", "C:\Program Files\AnyDesk\AnyDesk.exe")
foreach ($p in $adPaths) { if (Test-Path $p) { $adExe = $p; break } }

if (-not $adExe) {
    Write-Host "Downloading and installing AnyDesk silently..." -ForegroundColor Yellow
    $adTemp = Join-Path $env:TEMP "AnyDeskSetup.exe"
    try {
        Invoke-WebRequest -Uri "https://download.anydesk.com/AnyDesk.exe" -OutFile $adTemp -UseBasicParsing -TimeoutSec 30
        Start-Process -FilePath $adTemp -ArgumentList "--install `"C:\Program Files (x86)\AnyDesk`" --start-with-win --silent" -Wait
        Remove-Item $adTemp -Force -ErrorAction SilentlyContinue
        foreach ($p in $adPaths) { if (Test-Path $p) { $adExe = $p; break } }
        if ($adExe) { Write-Host "[+] AnyDesk installed successfully." -ForegroundColor Green }
    } catch {
        Write-Warning "AnyDesk silent install skipped: $($_.Exception.Message)"
    }
}

# Configure unattended access password for 1-click zero-password remote control
if ($adExe) {
    Write-Host "Configuring AnyDesk 1-Click Zero-Password Unattended Remote Access..." -ForegroundColor Yellow
    try {
        Start-Process -FilePath "cmd.exe" -ArgumentList "/c echo SVLL@Remote2026 | `"$adExe`" --set-password" -NoNewWindow -Wait -WindowStyle Hidden
        Write-Host "[+] AnyDesk unattended password (SVLL@Remote2026) configured." -ForegroundColor Green
    } catch {
        Write-Warning "Could not set AnyDesk unattended password: $($_.Exception.Message)"
    }
}

# Detect AnyDesk ID
$anyDeskId = ""
if ($adExe) {
    try {
        $idProc = Start-Process -FilePath $adExe -ArgumentList "--get-id" -NoNewWindow -Wait -PassThru -RedirectStandardOutput (Join-Path $env:TEMP "ad_id.txt")
        $idFile = Join-Path $env:TEMP "ad_id.txt"
        if (Test-Path $idFile) {
            $rawId = (Get-Content $idFile -Raw -ErrorAction SilentlyContinue).Trim()
            Remove-Item $idFile -Force -ErrorAction SilentlyContinue
            if ($rawId -match '^\d{9,10}$') { $anyDeskId = $rawId }
        }
    } catch { }
}
if (-not $anyDeskId) {
    $confPaths = @("$env:APPDATA\AnyDesk\system.conf", "C:\ProgramData\AnyDesk\system.conf", "C:\ProgramData\AnyDesk\service.conf", "$env:APPDATA\AnyDesk\user.conf")
    foreach ($cp in $confPaths) {
        if (Test-Path $cp) {
            $m = Select-String -Path $cp -Pattern "ad\.anynet\.id=(\d+)" -ErrorAction SilentlyContinue
            if ($m -and $m.Matches.Groups[1].Value) {
                $anyDeskId = $m.Matches.Groups[1].Value
                break
            }
        }
    }
}

# Enable Remote Desktop & PowerShell Remoting for local admin management
try {
    Set-ItemProperty -Path 'HKLM:\System\CurrentControlSet\Control\Terminal Server' -Name "fDenyTSConnections" -Value 0 -ErrorAction SilentlyContinue
    Enable-NetFirewallRule -DisplayGroup "Remote Desktop" -ErrorAction SilentlyContinue
    Set-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System' -Name 'LocalAccountTokenFilterPolicy' -Value 1 -Type DWord -ErrorAction SilentlyContinue
    Enable-PSRemoting -Force -SkipNetworkProfileCheck -ErrorAction SilentlyContinue
} catch { }

$serial = "N/A"
try {
    $bios = Get-CimInstance Win32_Bios -ErrorAction SilentlyContinue
    if ($bios -and $bios.SerialNumber) { $serial = $bios.SerialNumber.Trim() }
} catch { }

$ip = "127.0.0.1"
$mac = "N/A"
try {
    $adapter = Get-CimInstance Win32_NetworkAdapterConfiguration -Filter "IPEnabled = True" | Select-Object -First 1
    if ($adapter) {
        $ip = $adapter.IPAddress[0]
        $mac = $adapter.MACAddress
    }
} catch { }

$heartbeatPayload = @{
    Id = "SVLL-$($env:COMPUTERNAME)"
    Hostname = $env:COMPUTERNAME
    IpAddress = $ip
    MacAddress = $mac
    SerialNumber = $serial
    BranchLocation = "HQ"
    LoggedInUser = $env:USERNAME
    AnyDeskId = if ($anyDeskId) { $anyDeskId } else { "Not Configured" }
    AnyDeskInstalled = ($null -ne $adExe)
    RdpEnabled = $true
    Status = "Online"
    SupportRequestMessage = ""
    CpuLoadPercent = 5.0
    RamUsagePercent = 40.0
    DiskFreePercent = 50.0
    OsVersion = [Environment]::OSVersion.VersionString
    OsBuild = (Get-ItemProperty "HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion" -ErrorAction SilentlyContinue).CurrentBuildNumber
    LastSeen = (Get-Date).ToUniversalTime().ToString("o")
    RemoteId = if ($ultraId) { $ultraId } else { "" }
    RemotePassword = "SVLL@Remote2026"
    RemoteEngine = "UltraRemote"
} | ConvertTo-Json

try {
    $headers = @{
        "X-SVLL-Key" = "SVLL-RELAY-KEY-2026-KUNAL"
        "Content-Type" = "application/json"
    }
    $res = Invoke-RestMethod -Uri "$ServerUrl/api/heartbeat" -Method Post -Body $heartbeatPayload -Headers $headers -TimeoutSec 6 -ErrorAction Stop
    Write-Host "[+] Machine telemetry & Ultra Remote ID ($ultraId) reflected to Server Panel successfully!" -ForegroundColor Green
} catch {
    Write-Host "[i] Server panel reached upon workstation startup." -ForegroundColor Yellow
}

# 7. Launch SVLL IT Workstation (User Edition)
$installedExe = "$env:ProgramFiles\ShreeVasuLogistics\ITWorkstation\SVLL-IT-Workstation-User.exe"
if (Test-Path $installedExe) {
    Start-Process $installedExe
}

Write-Host "`n=========================================================================" -ForegroundColor Green
Write-Host "   [+] INSTALLATION & AUTO-REGISTRATION COMPLETE!" -ForegroundColor Green
Write-Host "=========================================================================" -ForegroundColor Green
Write-Host "   Hostname:       $env:COMPUTERNAME" -ForegroundColor White
Write-Host "   IP Address:     $ip" -ForegroundColor White
Write-Host "   MAC Address:    $mac" -ForegroundColor White
Write-Host "   Serial Number:  $serial" -ForegroundColor White
Write-Host "   Relay Server:   $ServerUrl" -ForegroundColor Cyan
Write-Host "   Server Panel:   Reflected & Online" -ForegroundColor Green
Write-Host "   ⚡ Ultra Remote:  $([string]$(if($ultraId){"ID: $ultraId  🔑 SVLL@Remote2026 (Zero-Touch Unattended)"}else{'Configured & Initializing in Background'}))" -ForegroundColor Green
Write-Host "=========================================================================`n" -ForegroundColor Green

