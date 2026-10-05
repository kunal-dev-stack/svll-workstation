# =========================================================================
# Shree Vasu Logistics Limited - One-Click Workstation Bootstrapper
# Developed by: Kunal Turkar
# Compatible with Windows PowerShell 5.1 and PowerShell 7+
# =========================================================================
$ErrorActionPreference = "Stop"
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$repo = "kunal-dev-stack/svll-workstation"
$version = "v5.6"
$fallbackUrl = "https://github.com/$repo/releases/download/$version/SVLL-IT-Workstation-$version-Setup.exe"

Write-Host "=========================================================" -ForegroundColor Cyan
Write-Host "  Shree Vasu Logistics Limited - Workstation Installer" -ForegroundColor Cyan
Write-Host "  Version: $version | Developed by: Kunal Turkar" -ForegroundColor Cyan
Write-Host "=========================================================" -ForegroundColor Cyan

# Check if a local installer package exists when run from a cloned repo
$localSetup = $null
if ($MyInvocation.MyCommand -and $MyInvocation.MyCommand.Path) {
    $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
    $candidate = Join-Path $scriptDir "release\SVLL-IT-Workstation-$version-Setup.exe"
    if (Test-Path $candidate) {
        $localSetup = $candidate
    }
}

$tempExe = $null

if ($localSetup) {
    Write-Host "[+] Found local release package: $localSetup" -ForegroundColor Green
    $tempExe = $localSetup
} else {
    Write-Host "Fetching SVLL Workstation $version setup from GitHub..." -ForegroundColor Cyan
    $downloadUrl = $fallbackUrl
    $fileName = "SVLL-IT-Workstation-$version-Setup.exe"

    try {
        $releaseJson = Invoke-RestMethod -Uri "https://api.github.com/repos/$repo/releases/latest" -Headers @{"User-Agent"="SVLL-Bootstrapper"}
        foreach ($a in $releaseJson.assets) {
            if ($a.name -like "*Setup*.exe") {
                $downloadUrl = $a.browser_download_url
                $fileName = $a.name
                break
            }
        }
    } catch {
        Write-Host "Notice: Using direct release endpoint ($downloadUrl)..." -ForegroundColor Yellow
    }

    $tempExe = Join-Path $env:TEMP $fileName
    Write-Host "Downloading $fileName to temporary staging..." -ForegroundColor Yellow
    
    # Download with progress support and fallback
    try {
        Start-BitsTransfer -Source $downloadUrl -Destination $tempExe -ErrorAction Stop
    } catch {
        Invoke-WebRequest -Uri $downloadUrl -OutFile $tempExe -UseBasicParsing
    }
}

Write-Host "Installing SVLL IT Support Workstation silently..." -ForegroundColor Cyan

$processArgs = @{
    FilePath     = $tempExe
    ArgumentList = "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP-"
    Wait         = $true
}

# Elevate if not currently running with administrator rights
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "Requesting Administrator elevation for Program Files installation..." -ForegroundColor Yellow
    $processArgs["Verb"] = "RunAs"
}

$proc = Start-Process @processArgs -PassThru
if ($proc.ExitCode -ne 0 -and $proc.ExitCode -ne $null) {
    Write-Warning "Setup process finished with exit code: $($proc.ExitCode)"
}

# Cleanup temporary download
if ($localSetup -eq $null -and (Test-Path $tempExe)) {
    Remove-Item $tempExe -Force -ErrorAction SilentlyContinue
}

$installPath = "$env:ProgramFiles\ShreeVasuLogistics\ITWorkstation\SVLL-IT-Workstation.exe"
if (Test-Path $installPath) {
    Write-Host "=========================================================" -ForegroundColor Green
    Write-Host "[+] Installation Complete! Launching SVLL Workstation..." -ForegroundColor Green
    Write-Host "=========================================================" -ForegroundColor Green
    Start-Process $installPath
} else {
    Write-Host "[+] Setup executed. Please check your Desktop or Start Menu for SVLL IT Workstation." -ForegroundColor Green
}
