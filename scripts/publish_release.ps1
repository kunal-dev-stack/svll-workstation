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
$tag = if ($args.Count -gt 0) { $args[0] } else { "v7.1" }
$releaseName = "SVLL IT Workstation v7.1 - AI Quick Support Copilot & Complaint Desk Update"
$notesPath = Join-Path $releaseDir "RELEASE_NOTES_v7.1.md"
$body = if (Test-Path $notesPath) { [System.IO.File]::ReadAllText($notesPath, [System.Text.Encoding]::UTF8) } else { "SVLL IT Support Workstation v7.1 Release" }

$headers = @{
    "Authorization" = "token " + $token
    "User-Agent"    = "SVLL-Release-Script"
    "Accept"        = "application/vnd.github.v3+json"
}

# 1. Check if release already exists
Write-Host "Checking if release '$tag' exists..." -ForegroundColor Yellow
$release = $null
try {
    $existing = Invoke-RestMethod -Uri "https://api.github.com/repos/$repo/releases/tags/$tag" -Headers $headers -Method Get
    if ($existing -and $existing.id) {
        $release = $existing
        Write-Host ("Found existing release ID: " + $release.id) -ForegroundColor Green
    }
} catch {
    # Not found
}

# 2. Create release if not found
if (-not $release) {
    Write-Host "Creating GitHub Release '$tag'..." -ForegroundColor Yellow
    $payload = @{
        tag_name         = $tag
        target_commitish = "main"
        name             = $releaseName
        body             = $body
        draft            = $false
        prerelease       = $false
    } | ConvertTo-Json

    $release = Invoke-RestMethod -Uri "https://api.github.com/repos/$repo/releases" -Headers $headers -Method Post -Body $payload -ContentType "application/json; charset=utf-8"
    Write-Host ("Created Release ID: " + $release.id) -ForegroundColor Green
} else {
    Write-Host "Updating release notes on '$tag'..." -ForegroundColor Cyan
    $patchPayload = @{
        name = $releaseName
        body = $body
    } | ConvertTo-Json
    $release = Invoke-RestMethod -Uri "https://api.github.com/repos/$repo/releases/$($release.id)" -Headers $headers -Method Patch -Body $patchPayload -ContentType "application/json; charset=utf-8"
}

$releaseId = $release.id
$uploadBase = "https://uploads.github.com/repos/" + $repo + "/releases/" + $releaseId + "/assets"

# 3. Assets to upload
$assets = @(
    @{
        Path = Join-Path $releaseDir "SVLL-IT-Workstation-v7.1-Setup.exe"
        Name = "SVLL-IT-Workstation-v7.1-Setup.exe"
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

# Fetch existing assets on the release
$existingAssets = Invoke-RestMethod -Uri ("https://api.github.com/repos/" + $repo + "/releases/" + $releaseId + "/assets") -Headers $headers -Method Get

foreach ($asset in $assets) {
    $assetPath = $asset.Path
    $assetName = $asset.Name
    $assetType = $asset.Type

    if (-not (Test-Path $assetPath)) {
        Write-Warning ("Asset not found: " + $assetPath)
        continue
    }

    $sizeMb = [Math]::Round(((Get-Item $assetPath).Length / 1MB), 2)

    # If asset already exists, delete it first to replace with clean build
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
Write-Host "  GitHub Release Published Successfully!" -ForegroundColor Green
Write-Host ("  URL: " + $release.html_url) -ForegroundColor Green
Write-Host "============================================================" -ForegroundColor Green
