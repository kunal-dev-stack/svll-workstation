# =========================================================================
# Shree Vasu Logistics Limited - One-Click Workstation Bootstrapper
# Developed by: Kunal Turkar
# =========================================================================
$ErrorActionPreference = "Stop"
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$repo = "kunal-dev-stack/svll-workstation"
Write-Host "Fetching latest SVLL Workstation installer release..." -ForegroundColor Cyan

# Query GitHub Releases API for the latest setup installer URL
$releaseJson = Invoke-RestMethod -Uri "https://api.github.com/repos/$repo/releases/latest" -Headers @{"User-Agent"="SVLL-Bootstrapper"}
$asset = $releaseJson.assets | Where-Object { $_.name -like "*Setup*.exe" } | Select-Object -First 1

if (-not $asset) {
    throw "No installer package (*Setup*.exe) found in the latest release."
}

$tempExe = Join-Path $env:TEMP $asset.name
Write-Host "Downloading $($asset.name)..." -ForegroundColor Yellow
Invoke-WebRequest -Uri $asset.browser_download_url -OutFile $tempExe -UseBasicParsing

Write-Host "Installing silently to Program Files..." -ForegroundColor Cyan
Start-Process -FilePath $tempExe -ArgumentList "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP-" -Wait
Remove-Item $tempExe -Force -ErrorAction SilentlyContinue

$installPath = "$env:ProgramFiles\ShreeVasuLogistics\ITWorkstation\SVLL-IT-Workstation.exe"
if (Test-Path $installPath) {
    Write-Host "✔ Installation complete! Launching SVLL Workstation..." -ForegroundColor Green
    Start-Process $installPath
} else {
    Write-Host "Installed successfully. Check your Start Menu or Desktop." -ForegroundColor Green
}
