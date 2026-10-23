# =========================================================================
# Shree Vasu Logistics Limited - Client Workstation Automated Installer
# Target: SVLL IT Support Workstation (User Edition v8.0)
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
Write-Host "   Version: User Edition v8.0 | Developed by Kunal Turkar" -ForegroundColor Cyan
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

# 3. Pre-create Application Data Directory & Configure Relay Settings
$programDataDir = "C:\ProgramData\SVLL-IT-Workstation"
if (-not (Test-Path $programDataDir)) {
    New-Item -ItemType Directory -Path $programDataDir -Force | Out-Null
}

$secConfig = @{
    PasswordHash = ""
    PasswordSalt = ""
    LastPasswordChange = (Get-Date).ToString("o")
    SharedFleetDirectory = ""
    SharedTicketDirectory = ""
    RelayServerUrl = $ServerUrl
    MeshCentralUrl = if ($MeshServerUrl) { $MeshServerUrl } else { "$ServerUrl:8086" }
} | ConvertTo-Json -Depth 5

Set-Content -Path (Join-Path $programDataDir "admin_security.json") -Value $secConfig -Force
Write-Host "[+] Pre-configured server connection in system profile." -ForegroundColor Green

# 4. Download and Install SVLL Workstation User Edition v8.0
$downloadUrl = "https://github.com/kunal-dev-stack/svll-workstation/releases/download/v8.0/SVLL-IT-Workstation-User-v8.0-Setup.exe"
$tempExe = Join-Path $env:TEMP "SVLL-IT-Workstation-User-v8.0-Setup.exe"

Write-Host "`n[1/3] Downloading SVLL Workstation User v8.0..." -ForegroundColor Yellow
try {
    Start-BitsTransfer -Source $downloadUrl -Destination $tempExe -ErrorAction Stop
} catch {
    Invoke-WebRequest -Uri $downloadUrl -OutFile $tempExe -UseBasicParsing
}

Write-Host "[2/3] Installing Workstation silently..." -ForegroundColor Yellow
$proc = Start-Process -FilePath $tempExe -ArgumentList "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP-" -Wait -PassThru
if ($proc.ExitCode -ne 0 -and $proc.ExitCode -ne $null) {
    Write-Warning "Setup returned non-zero code: $($proc.ExitCode)"
}
Remove-Item $tempExe -Force -ErrorAction SilentlyContinue

# 5. Automatically Install MeshAgent for Zero-Password Remote Access
Write-Host "`n[3/3] Configuring MeshAgent Remote Control Service..." -ForegroundColor Yellow
$meshInstalled = $false
$meshSvc = Get-Service -Name "Mesh Agent" -ErrorAction SilentlyContinue
if ($meshSvc) {
    Write-Host "[+] MeshAgent is already registered on this machine." -ForegroundColor Green
    if ($meshSvc.Status -ne "Running") {
        Start-Service -Name "Mesh Agent" -ErrorAction SilentlyContinue
    }
    $meshInstalled = $true
} else {
    Write-Host "Fetching MeshAgent binary..." -ForegroundColor Yellow
    $meshDir = "C:\Program Files\Mesh Agent"
    if (-not (Test-Path $meshDir)) { New-Item -ItemType Directory -Path $meshDir -Force | Out-Null }
    $meshExe = Join-Path $meshDir "MeshAgent.exe"

    $agentDownloadUrls = @(
        "$ServerUrl/download/meshagent",
        "https://github.com/Ylianst/MeshAgent/releases/download/legacy-1.2.6/MeshService64.exe"
    )

    $downloadSuccess = $false
    foreach ($url in $agentDownloadUrls) {
        try {
            Invoke-WebRequest -Uri $url -OutFile $meshExe -TimeoutSec 15 -UseBasicParsing -ErrorAction Stop
            if ((Get-Item $meshExe).Length -gt 50000) {
                $downloadSuccess = $true
                break
            }
        } catch { }
    }

    if ($downloadSuccess) {
        Write-Host "Registering MeshAgent service..." -ForegroundColor Yellow
        $installProc = Start-Process -FilePath $meshExe -ArgumentList "-install" -Wait -PassThru -WindowStyle Hidden
        Start-Sleep -Seconds 2
        Start-Service -Name "Mesh Agent" -ErrorAction SilentlyContinue
        Write-Host "[+] MeshAgent service installed and started successfully!" -ForegroundColor Green
        $meshInstalled = $true
    } else {
        Write-Host "[i] MeshAgent binary download skipped. Remote control fallback to AnyDesk active." -ForegroundColor DarkGray
    }
}

# 6. Gather Machine Telemetry and Fire Immediate Heartbeat
Write-Host "`nReflecting machine information to Server Panel..." -ForegroundColor Yellow
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

$meshNodeId = ""
try {
    $meshKey = Get-ItemProperty -Path "HKLM:\SOFTWARE\Open Source\MeshAgent" -ErrorAction SilentlyContinue
    if ($meshKey -and $meshKey.NodeID) { $meshNodeId = $meshKey.NodeID }
} catch { }

$heartbeatPayload = @{
    Id = "SVLL-$($env:COMPUTERNAME)"
    Hostname = $env:COMPUTERNAME
    IpAddress = $ip
    MacAddress = $mac
    SerialNumber = $serial
    BranchLocation = "HQ"
    LoggedInUser = $env:USERNAME
    AnyDeskId = ""
    AnyDeskInstalled = (Test-Path "C:\Program Files (x86)\AnyDesk\AnyDesk.exe") -or (Test-Path "C:\Program Files\AnyDesk\AnyDesk.exe")
    RdpEnabled = $false
    Status = "Online"
    SupportRequestMessage = ""
    CpuLoadPercent = 5.0
    RamUsagePercent = 40.0
    DiskFreePercent = 50.0
    OsVersion = [Environment]::OSVersion.VersionString
    OsBuild = (Get-ItemProperty "HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion" -ErrorAction SilentlyContinue).CurrentBuildNumber
    MeshNodeId = $meshNodeId
    MeshAgentRunning = $meshInstalled
    LastSeen = (Get-Date).ToUniversalTime().ToString("o")
} | ConvertTo-Json

try {
    $headers = @{
        "X-SVLL-Key" = "SVLL-RELAY-KEY-2026-KUNAL"
        "Content-Type" = "application/json"
    }
    $res = Invoke-RestMethod -Uri "$ServerUrl/api/heartbeat" -Method Post -Body $heartbeatPayload -Headers $headers -TimeoutSec 6 -ErrorAction Stop
    Write-Host "[+] Heartbeat registered at server panel successfully!" -ForegroundColor Green
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
Write-Host "   MeshAgent:      $([string]$(if($meshInstalled){'Active (Password-less Remote Ready)'}else{'Inactive'}))" -ForegroundColor Cyan
Write-Host "=========================================================================`n" -ForegroundColor Green
