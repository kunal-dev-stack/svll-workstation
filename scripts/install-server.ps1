# =========================================================================
# Shree Vasu Logistics Limited - Relay Server Automated Installer
# Target: SVLL IT Relay Server (v1.0)
# Developed by: Kunal Turkar
# Compatible with Windows 10/11 & Windows Server (x64)
# =========================================================================

$ErrorActionPreference = "Stop"
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

# 1. Require Administrator Elevation
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "[!] Elevation required. Requesting Administrator permissions..." -ForegroundColor Yellow
    $cliArgs = "-NoProfile -ExecutionPolicy Bypass -Command `"& { [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; irm https://raw.githubusercontent.com/kunal-dev-stack/svll-workstation/main/install-server.ps1 | iex }`""
    Start-Process powershell.exe -ArgumentList $cliArgs -Verb RunAs
    exit 0
}

Clear-Host
Write-Host "=========================================================================" -ForegroundColor Cyan
Write-Host "   SHREE VASU LOGISTICS LIMITED - RELAY SERVER v1.0 DEPLOYMENT" -ForegroundColor Cyan
Write-Host "   Central Hub for Fleet Telemetry & Remote Control | Kunal Turkar" -ForegroundColor Cyan
Write-Host "=========================================================================" -ForegroundColor Cyan

# 2. Check & Install .NET 8 ASP.NET Core Runtime if needed
Write-Host "`n[1/5] Checking .NET 8 Runtime..." -ForegroundColor Yellow
$hasNet8 = $false
try {
    $runtimes = & dotnet --list-runtimes 2>&1
    if ($runtimes -match "Microsoft\.AspNetCore\.App 8\.") {
        $hasNet8 = $true
    }
} catch { }

if (-not $hasNet8) {
    Write-Host "Downloading and installing ASP.NET Core 8.0 Runtime..." -ForegroundColor Yellow
    $dotnetInstallerUrl = "https://download.visualstudio.microsoft.com/download/pr/977ecab2-cfc3-4d7a-8ff4-9430c1be7b15/e8a3493e820c3a812dfcf4f58c4dc1b8/dotnet-hosting-8.0.8-win.exe"
    $dotnetTemp = Join-Path $env:TEMP "dotnet-hosting-8.0.exe"
    try {
        Invoke-WebRequest -Uri $dotnetInstallerUrl -OutFile $dotnetTemp -UseBasicParsing
        Start-Process -FilePath $dotnetTemp -ArgumentList "/install /quiet /norestart" -Wait
        Remove-Item $dotnetTemp -Force -ErrorAction SilentlyContinue
        Write-Host "[+] .NET 8.0 Hosting Bundle installed successfully." -ForegroundColor Green
    } catch {
        Write-Warning "Could not install .NET 8 automatically. Please ensure .NET 8 runtime is present."
    }
} else {
    Write-Host "[+] ASP.NET Core 8.0 Runtime verified." -ForegroundColor Green
}

# 3. Download SVLL Relay Server Package
$targetDir = "C:\svll-relay"
if (-not (Test-Path $targetDir)) {
    New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
}

$serverZipUrl = "https://github.com/kunal-dev-stack/svll-workstation/releases/download/server-v1.0/SVLL-Relay-Server-v1.0-win-x64.zip"
$tempZip = Join-Path $env:TEMP "SVLL-Relay-Server-v1.0-win-x64.zip"

Write-Host "`n[2/5] Downloading SVLL Relay Server v1.0 package..." -ForegroundColor Yellow
try {
    Start-BitsTransfer -Source $serverZipUrl -Destination $tempZip -ErrorAction Stop
} catch {
    Invoke-WebRequest -Uri $serverZipUrl -OutFile $tempZip -UseBasicParsing
}

# Stop service before extraction if already running
$svc = Get-Service -Name "SvllRelay" -ErrorAction SilentlyContinue
if ($svc) {
    Write-Host "Stopping existing SvllRelay service..." -ForegroundColor Yellow
    Stop-Service -Name "SvllRelay" -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2
}

Write-Host "`n[3/5] Extracting server binaries to $targetDir..." -ForegroundColor Yellow
Expand-Archive -Path $tempZip -DestinationPath $targetDir -Force
Remove-Item $tempZip -Force -ErrorAction SilentlyContinue

# 4. Open Windows Firewall Port 5000
Write-Host "`n[4/5] Configuring Windows Firewall (Port 5000 TCP)..." -ForegroundColor Yellow
try {
    Remove-NetFirewallRule -DisplayName "SVLL Relay Server" -ErrorAction SilentlyContinue
    New-NetFirewallRule -DisplayName "SVLL Relay Server" -Direction Inbound -Protocol TCP -LocalPort 5000 -Action Allow -ErrorAction SilentlyContinue | Out-Null
    Write-Host "[+] Firewall port 5000 inbound rule enabled." -ForegroundColor Green
} catch {
    Write-Warning "Firewall rule could not be set automatically. Please verify port 5000 is open."
}

# 5. Register and Start Windows Service / Background Task
Write-Host "`n[5/5] Registering and starting SvllRelay Service..." -ForegroundColor Yellow
$exePath = Join-Path $targetDir "SvllRelayServer.exe"
$binPath = if (Test-Path $exePath) { "`"$exePath`" --urls http://0.0.0.0:5000" } else { "dotnet `"$targetDir\SvllRelayServer.dll`" --urls http://0.0.0.0:5000" }

if ($svc) {
    & sc.exe config SvllRelay binpath= "$binPath" start= auto | Out-Null
} else {
    & sc.exe create SvllRelay binpath= "$binPath" start= auto | Out-Null
}
& sc.exe description SvllRelay "SVLL IT Workstation - Central Fleet Relay & Telemetry Server" | Out-Null
& sc.exe start SvllRelay | Out-Null

Start-Sleep -Seconds 3

# If service didn't start (e.g. SCM permissions), launch background daemon process
$svcRunning = (Get-Service -Name "SvllRelay" -ErrorAction SilentlyContinue).Status -eq "Running"
if (-not $svcRunning) {
    Write-Host "[i] Starting SvllRelay background process..." -ForegroundColor Yellow
    if (Test-Path $exePath) {
        Start-Process -FilePath $exePath -ArgumentList "--urls http://0.0.0.0:5000" -WindowStyle Hidden
    } else {
        Start-Process -FilePath "dotnet" -ArgumentList "`"$targetDir\SvllRelayServer.dll`" --urls http://0.0.0.0:5000" -WindowStyle Hidden
    }
    Start-Sleep -Seconds 3
}

# Create Desktop Shortcut for easy management
try {
    $wsh = New-Object -ComObject WScript.Shell
    $shortcut = $wsh.CreateShortcut([System.IO.Path]::Combine([Environment]::GetFolderPath("Desktop"), "SVLL Relay Server.lnk"))
    $shortcut.TargetPath = if (Test-Path (Join-Path $targetDir "start_server.bat")) { Join-Path $targetDir "start_server.bat" } else { $exePath }
    $shortcut.WorkingDirectory = $targetDir
    $shortcut.Description = "Start SVLL IT Relay Server (Port 5000)"
    $shortcut.Save()
} catch { }

# 6. Verify Health Check
$healthOk = $false
try {
    $health = Invoke-RestMethod -Uri "http://localhost:5000/api/health" -TimeoutSec 5 -ErrorAction Stop
    if ($health.status -eq "ok") { $healthOk = $true }
} catch { }

# Detect Local IP Addresses
$localIps = @()
try {
    $adapters = Get-CimInstance Win32_NetworkAdapterConfiguration -Filter "IPEnabled = True" -ErrorAction SilentlyContinue
    foreach ($a in $adapters) {
        foreach ($addr in $a.IPAddress) {
            if ($addr -match '^\d+\.\d+\.\d+\.\d+$' -and $addr -ne '127.0.0.1') {
                $localIps += $addr
            }
        }
    }
} catch { }

$primaryIp = if ($localIps.Count -gt 0) { $localIps[0] } else { "127.0.0.1" }

Write-Host "`n=========================================================================" -ForegroundColor Green
Write-Host "   [+] SVLL RELAY SERVER v1.0 DEPLOYMENT SUCCESSFUL!" -ForegroundColor Green
Write-Host "=========================================================================" -ForegroundColor Green
Write-Host "   Service Name:     SvllRelay (Windows Service - Automatic Boot)" -ForegroundColor White
Write-Host "   Listening Port:   5000 (All interfaces 0.0.0.0)" -ForegroundColor White
Write-Host "   Health Status:    $([string]$(if($healthOk){'[OK] Healthy'}else{'[!] Starting up'}))" -ForegroundColor Green
Write-Host "`n   🖥️  SERVER WEB PANEL (Open in your browser):" -ForegroundColor Cyan
Write-Host "       Local:        http://localhost:5000/" -ForegroundColor White
foreach ($ip in $localIps) {
    Write-Host "       Network LAN:  http://$($ip):5000/" -ForegroundColor Yellow
}
Write-Host "`n   ⚡ WORKSTATION INSTALLATION COMMANDS:" -ForegroundColor Cyan
Write-Host "       User Edition (Employee PCs):  irm https://tinyurl.com/svll-tool | iex" -ForegroundColor Green
Write-Host "       Admin Console (IT Admin PC):  irm https://tinyurl.com/svll-admin | iex" -ForegroundColor Green
Write-Host "       Local LAN Installer:          irm http://$($primaryIp):5000/install.ps1 | iex" -ForegroundColor DarkGray
Write-Host "=========================================================================`n" -ForegroundColor Green
