# =========================================================================
# Shree Vasu Logistics Limited - Attach Setup Assets to Release 7.0
# Developed by: Kunal Turkar
# =========================================================================

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptDir
$releaseDir = Join-Path $projectRoot "release"
$publishDir = Join-Path $projectRoot "publish"

Write-Host "Fetching GitHub credentials..." -ForegroundColor Cyan
$pinfo = New-Object System.Diagnostics.ProcessStartInfo
$pinfo.FileName = "git.exe"
$pinfo.Arguments = "credential fill"
$pinfo.UseShellExecute = $false
$pinfo.RedirectStandardInput = $true
$pinfo.RedirectStandardOutput = $true
$pinfo.CreateNoWindow = $true

$proc = [System.Diagnostics.Process]::Start($pinfo)
$proc.StandardInput.WriteLine("protocol=https")
$proc.StandardInput.WriteLine("host=github.com")
$proc.StandardInput.Close()

$token = $null
while (-not $proc.StandardOutput.EndOfStream) {
    $line = $proc.StandardOutput.ReadLine()
    if ($line.StartsWith("password=")) {
        $token = $line.Substring(9)
        break
    }
}
$proc.WaitForExit()

if (-not $token) {
    throw "GitHub token could not be retrieved from git credential manager."
}

$repo = "kunal-dev-stack/svll-workstation"
$tag = "7.0"
$headers = @{
    "Authorization" = "token " + $token
    "User-Agent"    = "SVLL-Release-Script"
    "Accept"        = "application/vnd.github.v3+json"
}

Write-Host "Querying Release '$tag'..." -ForegroundColor Yellow
$release = Invoke-RestMethod -Uri "https://api.github.com/repos/$repo/releases/tags/$tag" -Headers $headers -Method Get
$releaseId = $release.id
Write-Host ("Found Release ID: " + $releaseId) -ForegroundColor Green

# Update body with complete v7.0 release notes
$notesPath = Join-Path $releaseDir "RELEASE_NOTES_v7.0.md"
if (Test-Path $notesPath) {
    Write-Host "Updating release notes on Release 7.0..." -ForegroundColor Cyan
    $bodyText = [System.IO.File]::ReadAllText($notesPath, [System.Text.Encoding]::UTF8)
    $patchPayload = @{
        name = "SVLL IT Workstation v7.0 - Zero-Code Remote Access & AI Service Desk Edition"
        body = $bodyText
    } | ConvertTo-Json

    $release = Invoke-RestMethod -Uri "https://api.github.com/repos/$repo/releases/$releaseId" -Headers $headers -Method Patch -Body $patchPayload -ContentType "application/json; charset=utf-8"
}

# Fetch existing assets on 7.0 release
$existingAssets = Invoke-RestMethod -Uri ("https://api.github.com/repos/" + $repo + "/releases/" + $releaseId + "/assets") -Headers $headers -Method Get

$uploadBase = "https://uploads.github.com/repos/" + $repo + "/releases/" + $releaseId + "/assets"

$assets = @(
    @{
        Path = Join-Path $releaseDir "SVLL-IT-Workstation-v7.0-Setup.exe"
        Name = "SVLL-IT-Workstation-v7.0-Setup.exe"
        Type = "application/vnd.microsoft.portable-executable"
    },
    @{
        Path = Join-Path $publishDir "SVLL-IT-Workstation.exe"
        Name = "SVLL-IT-Workstation.exe"
        Type = "application/vnd.microsoft.portable-executable"
    },
    @{
        Path = Join-Path $releaseDir "CHECKSUMS.txt"
        Name = "CHECKSUMS.txt"
        Type = "text/plain"
    }
)

foreach ($asset in $assets) {
    $assetPath = $asset.Path
    $assetName = $asset.Name
    $assetType = $asset.Type

    if (-not (Test-Path $assetPath)) {
        Write-Warning ("Asset not found: " + $assetPath)
        continue
    }

    $sizeMb = [Math]::Round(((Get-Item $assetPath).Length / 1MB), 2)

    # Delete if already exists
    if ($existingAssets) {
        $match = $existingAssets | Where-Object { $_.name -eq $assetName }
        if ($match) {
            Write-Host ("Deleting existing asset " + $match.name + "...") -ForegroundColor Yellow
            Invoke-RestMethod -Uri ("https://api.github.com/repos/" + $repo + "/releases/assets/" + $match.id) -Headers $headers -Method Delete
        }
    }

    Write-Host ("Uploading " + $assetName + " (" + $sizeMb + " MB)...") -ForegroundColor Cyan
    $uploadUri = $uploadBase + "?name=" + $assetName

    $uploadHeaders = @{
        "Authorization" = "token " + $token
        "User-Agent"    = "SVLL-Release-Script"
        "Content-Type"  = $assetType
    }

    $res = Invoke-RestMethod -Uri $uploadUri -Headers $uploadHeaders -Method Post -InFile $assetPath -TimeoutSec 1200
    Write-Host ("Uploaded: " + $res.name + " -> " + $res.browser_download_url) -ForegroundColor Green
}

Write-Host "============================================================" -ForegroundColor Green
Write-Host "  Setup Assets Attached to Release 7.0 Successfully!" -ForegroundColor Green
Write-Host ("  URL: " + $release.html_url) -ForegroundColor Green
Write-Host "============================================================" -ForegroundColor Green
