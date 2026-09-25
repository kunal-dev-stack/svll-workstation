# 1. Require Administrator Elevation
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "[ERROR] Administrator privileges required. Please run PowerShell as Administrator." -ForegroundColor Red
    return
}

Write-Host "========================================================" -ForegroundColor Cyan
Write-Host "  Installing SVLL IT Support Workstation v5.0..." -ForegroundColor Cyan
Write-Host "========================================================" -ForegroundColor Cyan

# 2. Force TLS 1.2 / TLS 1.3 (Prevents "connection forcibly closed" error)
[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12 -bor [System.Net.SecurityProtocolType]::Tls11 -bor [System.Net.SecurityProtocolType]::Tls

$installerUrl = "https://github.com/kunal-dev-stack/svll-workstation/releases/download/v5.0/SVLL-IT-Workstation-v5.0-Setup.exe"
$localTempPath = Join-Path $env:TEMP "SVLL-IT-Setup.exe"

# Terminate existing running instance if updating
Stop-Process -Name "SVLL-IT-Workstation" -Force -ErrorAction SilentlyContinue

# Clean up any leftover 0-byte temporary file
Remove-Item -Path $localTempPath -Force -ErrorAction SilentlyContinue

# 3. Stream download using .NET WebClient with custom User-Agent
Write-Host "[1/2] Downloading installer (~70 MB)..." -ForegroundColor Yellow
try {
    $webClient = New-Object System.Net.WebClient
    $webClient.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)")
    $webClient.DownloadFile($installerUrl, $localTempPath)
} catch {
    Write-Host "[ERROR] Download failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Please verify that the v5.0 release and .exe file exist on GitHub." -ForegroundColor Yellow
    return
}

# 4. Verify file downloaded and is not empty
if (-not (Test-Path $localTempPath) -or (Get-Item $localTempPath).Length -lt 1048576) {
    Write-Host "[ERROR] Downloaded file is missing or corrupted (under 1MB)." -ForegroundColor Red
    return
}

# 5. Run silent Inno Setup installation
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
