# =========================================================================
# Shree Vasu Logistics Limited - One-Click Workstation Bootstrapper
# Developed by: Kunal Turkar
# Compatible with Windows PowerShell 5.1 and PowerShell 7+
# =========================================================================
$ErrorActionPreference = "Stop"
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$repo = "kunal-dev-stack/svll-workstation"
Write-Host "=========================================================" -ForegroundColor Cyan
Write-Host "  Shree Vasu Logistics Limited - Workstation Installer" -ForegroundColor Cyan
Write-Host "  Developed by: Kunal Turkar" -ForegroundColor Cyan
Write-Host "=========================================================" -ForegroundColor Cyan

# Check if a local installer package exists in the repo
$localSetup = Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) "release\SVLL-IT-Workstation-v5.5-Setup.exe"
$tempExe = $null

if (Test-Path $localSetup) {
    Write-Host "✔ Found local release package: $localSetup" -ForegroundColor Green
    $tempExe = $localSetup
} else {
    Write-Host "Fetching latest SVLL Workstation installer release from GitHub..." -ForegroundColor Cyan
    try {
        $releaseJson = Invoke-RestMethod -Uri "https://api.github.com/repos/$repo/releases/latest" -Headers @{"User-Agent"="SVLL-Bootstrapper"}
        $asset = $null
        foreach ($a in $releaseJson.assets) {
            if ($a.name -like "*Setup*.exe") {
                $asset = $a
                break
            }
        }

        if (-not $asset) {
            throw "No installer package (*Setup*.exe) found in the latest release."
        }

        $tempExe = Join-Path $env:TEMP $asset.name
        Write-Host "Downloading $($asset.name)..." -ForegroundColor Yellow
        Invoke-WebRequest -Uri $asset.browser_download_url -OutFile $tempExe -UseBasicParsing
    } catch {
        Write-Warning "Could not fetch online release: $($_.Exception.Message)"
        throw $_
    }
}

Write-Host "Installing silently to Program Files..." -ForegroundColor Cyan
Start-Process -FilePath $tempExe -ArgumentList "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP-" -Wait

if ($tempExe -ne $localSetup) {
    Remove-Item $tempExe -Force -ErrorAction SilentlyContinue
}

$installPath = "$env:ProgramFiles\ShreeVasuLogistics\ITWorkstation\SVLL-IT-Workstation.exe"
if (Test-Path $installPath) {
    Write-Host "✔ Installation complete! Launching SVLL Workstation..." -ForegroundColor Green
    Start-Process $installPath
} else {
    Write-Host "Installed successfully. Check your Start Menu or Desktop." -ForegroundColor Green
}
