# =========================================================================
# Shree Vasu Logistics Limited - GitHub Release Publisher
# Developed by: Kunal Turkar
# Publishes Workstation v8.1 (Client & Admin) and Server v1.1 Releases to GitHub
# =========================================================================

param(
    [string]$Target = "all" # "all", "client", "server"
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptDir
$releaseDir = Join-Path $projectRoot "release"
$publishAdminDir = Join-Path $projectRoot "publish-admin"
$publishUserDir = Join-Path $projectRoot "publish-user"
$backendDir = Join-Path $projectRoot "backend\SvllRelayServer"

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
$headers = @{
    "Authorization" = "token " + $token
    "User-Agent"    = "SVLL-Release-Script"
    "Accept"        = "application/vnd.github.v3+json"
}

function Publish-GitHubRelease {
    param(
        [string]$Tag,
        [string]$ReleaseName,
        [string]$NotesPath,
        [array]$Assets
    )

    $body = if (Test-Path $NotesPath) { [System.IO.File]::ReadAllText($NotesPath, [System.Text.Encoding]::UTF8) } else { "$ReleaseName" }

    Write-Host "`n============================================================" -ForegroundColor Cyan
    Write-Host "  Publishing GitHub Release: $Tag ($ReleaseName)" -ForegroundColor Cyan
    Write-Host "============================================================" -ForegroundColor Cyan

    # 1. Check if release already exists
    Write-Host "Checking if release '$Tag' exists..." -ForegroundColor Yellow
    $release = $null
    try {
        $existing = Invoke-RestMethod -Uri "https://api.github.com/repos/$repo/releases/tags/$Tag" -Headers $headers -Method Get
        if ($existing -and $existing.id) {
            $release = $existing
            Write-Host ("Found existing release ID: " + $release.id) -ForegroundColor Green
        }
    } catch { }

    # 2. Create release if not found
    if (-not $release) {
        Write-Host "Creating GitHub Release '$Tag'..." -ForegroundColor Yellow
        $payload = @{
            tag_name         = $Tag
            target_commitish = "main"
            name             = $ReleaseName
            body             = $body
            draft            = $false
            prerelease       = $false
        } | ConvertTo-Json

        $release = Invoke-RestMethod -Uri "https://api.github.com/repos/$repo/releases" -Headers $headers -Method Post -Body $payload -ContentType "application/json; charset=utf-8"
        Write-Host ("Created Release ID: " + $release.id) -ForegroundColor Green
    } else {
        Write-Host "Updating release notes on '$Tag'..." -ForegroundColor Cyan
        $patchPayload = @{
            name = $ReleaseName
            body = $body
        } | ConvertTo-Json
        $release = Invoke-RestMethod -Uri "https://api.github.com/repos/$repo/releases/$($release.id)" -Headers $headers -Method Patch -Body $patchPayload -ContentType "application/json; charset=utf-8"
    }

    $releaseId = $release.id
    $uploadBase = "https://uploads.github.com/repos/" + $repo + "/releases/" + $releaseId + "/assets"

    # Fetch existing assets on the release
    $existingAssets = Invoke-RestMethod -Uri ("https://api.github.com/repos/" + $repo + "/releases/" + $releaseId + "/assets") -Headers $headers -Method Get

    foreach ($asset in $Assets) {
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

    Write-Host "Release '$Tag' ready at: $($release.html_url)" -ForegroundColor Green
}

# ── 1. Publish Workstation v8.2 Release (Client & Admin) ───────────────────────────
if ($Target -eq "all" -or $Target -eq "client" -or $Target -eq "workstation") {
    $clientAssets = @(
        @{
            Path = Join-Path $releaseDir "SVLL-IT-Workstation-User-v8.2-Setup.exe"
            Name = "SVLL-IT-Workstation-User-v8.2-Setup.exe"
            Type = "application/vnd.microsoft.portable-executable"
        },
        @{
            Path = Join-Path $releaseDir "SVLL-IT-Workstation-Admin-v8.2-Setup.exe"
            Name = "SVLL-IT-Workstation-Admin-v8.2-Setup.exe"
            Type = "application/vnd.microsoft.portable-executable"
        },
        @{
            Path = Join-Path $releaseDir "SVLL-IT-Workstation-v8.2-Setup.exe"
            Name = "SVLL-IT-Workstation-v8.2-Setup.exe"
            Type = "application/vnd.microsoft.portable-executable"
        },
        @{
            Path = Join-Path $publishAdminDir "SVLL-IT-Workstation-Admin.exe"
            Name = "SVLL-IT-Workstation-Admin.exe"
            Type = "application/vnd.microsoft.portable-executable"
        },
        @{
            Path = Join-Path $publishUserDir "SVLL-IT-Workstation-User.exe"
            Name = "SVLL-IT-Workstation-User.exe"
            Type = "application/vnd.microsoft.portable-executable"
        },
        @{
            Path = Join-Path $releaseDir "CHECKSUMS.txt"
            Name = "CHECKSUMS.txt"
            Type = "text/plain"
        }
    )

    Publish-GitHubRelease `
        -Tag "v8.2" `
        -ReleaseName "SVLL IT Support Workstation v8.2 (Dual Editions: Client & Admin)" `
        -NotesPath (Join-Path $releaseDir "RELEASE_NOTES_v8.2.md") `
        -Assets $clientAssets
}

# ── 2. Publish Server v1.1 Release ───────────────────────────────────────────
if ($Target -eq "all" -or $Target -eq "server") {
    $serverAssets = @(
        @{
            Path = Join-Path $releaseDir "SVLL-Relay-Server-v1.1-win-x64.zip"
            Name = "SVLL-Relay-Server-v1.1-win-x64.zip"
            Type = "application/zip"
        },
        @{
            Path = Join-Path $backendDir "DEPLOY.md"
            Name = "DEPLOY.md"
            Type = "text/markdown"
        }
    )

    Publish-GitHubRelease `
        -Tag "server-v1.1" `
        -ReleaseName "SVLL IT Relay Server v1.1" `
        -NotesPath (Join-Path $releaseDir "RELEASE_NOTES_SERVER_v1.1.md") `
        -Assets $serverAssets
}

Write-Host "`n============================================================" -ForegroundColor Green
Write-Host "  ALL RELEASES PUBLISHED TO GITHUB SUCCESSFULLY!" -ForegroundColor Green
Write-Host "============================================================" -ForegroundColor Green
