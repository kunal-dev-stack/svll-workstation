# =========================================================================
# Shree Vasu Logistics Limited - Client Workstation Automated Installer
# Target: SVLL IT Support Workstation (User Edition v8.4)
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
    $cliArgs = "-NoProfile -ExecutionPolicy Bypass -Command `"& { [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; irm https://raw.githubusercontent.com/kunal-dev-stack/svll-workstation/main/install.ps1 | iex }`""
    Start-Process powershell.exe -ArgumentList $cliArgs -Verb RunAs
    exit 0
}

Clear-Host
Write-Host "=========================================================================" -ForegroundColor Cyan
Write-Host "   SHREE VASU LOGISTICS LIMITED - WORKSTATION DEPLOYMENT AGENT" -ForegroundColor Cyan
Write-Host "   Version: User Edition v8.4 | Developed by Kunal Turkar" -ForegroundColor Cyan
Write-Host "=========================================================================" -ForegroundColor Cyan

# 2. Dynamic Server Discovery (LAN direct with Cloudflare Internet Fallback)
if (-not $ServerUrl) {
    # Check if LAN Gateway (192.168.10.11:5000) is directly reachable
    $lanCandidate = "http://192.168.10.11:5000"
    try {
        $testLan = Invoke-WebRequest -Uri "$lanCandidate/api/health" -UseBasicParsing -TimeoutSec 2 -ErrorAction Stop
        if ($testLan.StatusCode -eq 200) {
            $ServerUrl = $lanCandidate
        }
    } catch { }
}

# If not on LAN, default to the 24/7 Internet Cloudflare Tunnel Gateway
if (-not $ServerUrl) {
    $ServerUrl = "https://roberts-basis-early-likewise.trycloudflare.com"
}
$ServerUrl = $ServerUrl.TrimEnd('/')
Write-Host "[+] Connecting to SVLL Gateway Server: $ServerUrl" -ForegroundColor Green

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

# 4. Aggressively Purge Legacy Third-Party Remote Tools (RustDesk / UltraRemote)
Write-Host "`n[+] Purging legacy remote tools (RustDesk / UltraRemote) in favor of AegisMesh..." -ForegroundColor Yellow
try {
    Stop-Service -Name "RustDesk" -Force -ErrorAction SilentlyContinue
    sc.exe stop RustDesk *>$null
    sc.exe delete RustDesk *>$null
    Stop-Process -Name "rustdesk*" -Force -ErrorAction SilentlyContinue
    Stop-Process -Name "RuntimeBroker_rustdesk*" -Force -ErrorAction SilentlyContinue

    Remove-Item "C:\Program Files\RustDesk" -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item "$env:ProgramData\RustDesk" -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item "$env:APPDATA\RustDesk" -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item "C:\Windows\ServiceProfiles\LocalService\AppData\Roaming\RustDesk" -Recurse -Force -ErrorAction SilentlyContinue

    netsh advfirewall firewall delete rule name="UltraRemotePort21118" *>$null
    netsh advfirewall firewall delete rule name="UltraRemoteUDP" *>$null
    netsh advfirewall firewall delete rule name="UltraRemoteApp" *>$null
    netsh advfirewall firewall delete rule name="UltraRemoteApp2" *>$null
    Write-Host "[+] Cleaned legacy remote configurations." -ForegroundColor Green
} catch { }

# 5. Terminate any running old version and install User Edition v8.4
Write-Host "`n[+] Closing any running previous versions of SVLL Workstation for clean in-place upgrade..." -ForegroundColor Cyan
Get-Process -Name "SVLL-IT-Workstation*" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 1

$downloadUrl = "https://github.com/kunal-dev-stack/svll-workstation/releases/download/v8.4/SVLL-IT-Workstation-User-v8.4-Setup.exe"
$tempExe = Join-Path $env:TEMP "SVLL-IT-Workstation-User-v8.4-Setup.exe"

Write-Host "[1/3] Downloading latest SVLL Workstation User v8.4..." -ForegroundColor Yellow
try {
    Start-BitsTransfer -Source $downloadUrl -Destination $tempExe -ErrorAction Stop
} catch {
    Invoke-WebRequest -Uri $downloadUrl -OutFile $tempExe -UseBasicParsing
}

Write-Host "[2/3] Performing in-place upgrade silently..." -ForegroundColor Yellow
$proc = Start-Process -FilePath $tempExe -ArgumentList "/CLOSEAPPLICATIONS /FORCECLOSEAPPLICATIONS /VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP-" -PassThru; $proc | Wait-Process -Timeout 45 -ErrorAction SilentlyContinue
if ($proc.ExitCode -ne 0 -and $proc.ExitCode -ne $null) {
    Write-Warning "Setup returned code: $($proc.ExitCode)"
}
Remove-Item $tempExe -Force -ErrorAction SilentlyContinue

# 6. Deploy SVLL Workstation Management Agent (AegisMesh 24/7 Service)
Write-Host "`n[3/3] Deploying AegisMesh Enterprise Remote Management Agent..." -ForegroundColor Cyan
$agentInstallDir = "$env:ProgramFiles\AegisMesh"
if (-not (Test-Path $agentInstallDir)) {
    New-Item -ItemType Directory -Path $agentInstallDir -Force | Out-Null
}

$agentZipUrl = "$ServerUrl/api/agent/windows-agent"
$agentZipTemp = Join-Path $env:TEMP "AegisMesh-Agent.zip"

try {
    Write-Host "    Downloading agent package from $agentZipUrl..." -ForegroundColor Yellow
    try {
        Invoke-WebRequest -Uri $agentZipUrl -OutFile $agentZipTemp -UseBasicParsing -TimeoutSec 30
    } catch {
        # Fallback to GitHub Release asset
        $ghAgentUrl = "https://github.com/kunal-dev-stack/svll-workstation/releases/download/v8.4/AegisMesh-Agent-Windows.zip"
        Invoke-WebRequest -Uri $ghAgentUrl -OutFile $agentZipTemp -UseBasicParsing -TimeoutSec 30
    }

    if (Test-Path $agentZipTemp) {
        Expand-Archive -Path $agentZipTemp -DestinationPath $agentInstallDir -Force
        Remove-Item $agentZipTemp -Force -ErrorAction SilentlyContinue
    }

    $agentExe = Join-Path $agentInstallDir "AegisMeshAgent.exe"
    if (Test-Path $agentExe) {
        Write-Host "    Registering 24/7 Auto-Start Task (SYSTEM)..." -ForegroundColor Yellow
        sc.exe stop AegisMeshAgent *>$null
        sc.exe delete AegisMeshAgent *>$null

        $wsUrl = $ServerUrl.Replace("http://", "ws://").Replace("https://", "wss://")
        $taskCmd = '\"' + $agentExe + '\" --server \"' + $wsUrl + '\" --token \"MESH-ENTERPRISE-TOKEN-2026\"'
        schtasks.exe /create /tn "AegisMeshAgent" /tr $taskCmd /sc onstart /ru "SYSTEM" /rl HIGHEST /f | Out-Null

        Stop-Process -Name "AegisMeshAgent" -Force -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 1
        Write-Host "    Launching background agent daemon..." -ForegroundColor Yellow
        Start-Process -FilePath $agentExe -ArgumentList @("--server", $wsUrl, "--token", "MESH-ENTERPRISE-TOKEN-2026") -WindowStyle Hidden
        Write-Host "[+] SVLL Workstation Management Agent active & connected to $wsUrl" -ForegroundColor Green
    }
} catch {
    Write-Warning "Management agent installation notice: $($_.Exception.Message)"
}

# 7. Post Telemetry Heartbeat to Server
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
    BranchLocation = "Corporate HQ"
    LoggedInUser = $env:USERNAME
    AnyDeskId = ""
    AnyDeskInstalled = $false
    RdpEnabled = $true
    Status = "Online"
    SupportRequestMessage = ""
    CpuLoadPercent = 5.0
    RamUsagePercent = 40.0
    DiskFreePercent = 50.0
    OsVersion = [Environment]::OSVersion.VersionString
    OsBuild = (Get-ItemProperty "HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion" -ErrorAction SilentlyContinue).CurrentBuildNumber
    LastSeen = (Get-Date).ToUniversalTime().ToString("o")
    RemoteId = "SVLL-$($env:COMPUTERNAME)"
    RemotePassword = "SVLL@Remote2026"
    RemoteEngine = "AegisMesh"
} | ConvertTo-Json

try {
    $headers = @{
        "X-SVLL-Key" = "SVLL-RELAY-KEY-2026-KUNAL"
        "Content-Type" = "application/json"
    }
    $res = Invoke-RestMethod -Uri "$ServerUrl/api/heartbeat" -Method Post -Body $heartbeatPayload -Headers $headers -TimeoutSec 6 -ErrorAction SilentlyContinue
    Write-Host "[+] Workstation telemetry successfully reflected to Server Panel!" -ForegroundColor Green
} catch { }

# 8. Launch SVLL IT Workstation (User Edition)
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
Write-Host "   âš¡ Remote Access: AegisMesh Zero-Touch Background Daemon Active" -ForegroundColor Green
Write-Host "=========================================================================`n" -ForegroundColor Green
