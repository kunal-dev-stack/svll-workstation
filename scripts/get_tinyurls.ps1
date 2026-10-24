$wc = New-Object System.Net.WebClient
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$userUrl = "https://github.com/kunal-dev-stack/svll-workstation/releases/download/v8.3/SVLL-IT-Workstation-User-v8.3-Setup.exe"
$adminUrl = "https://github.com/kunal-dev-stack/svll-workstation/releases/download/v8.3/SVLL-IT-Workstation-Admin-v8.3-Setup.exe"
$installUserScript = "https://raw.githubusercontent.com/kunal-dev-stack/svll-workstation/main/install.ps1"
$installAdminScript = "https://raw.githubusercontent.com/kunal-dev-stack/svll-workstation/main/install-admin.ps1"

function Get-TinyUrl([string]$target) {
    try {
        return $wc.DownloadString("https://tinyurl.com/api-create.php?url=" + [System.Uri]::EscapeDataString($target))
    } catch {
        return "N/A"
    }
}

Write-Host "USER_EXE_SHORT: " (Get-TinyUrl $userUrl)
Write-Host "ADMIN_EXE_SHORT: " (Get-TinyUrl $adminUrl)
Write-Host "USER_SCRIPT_SHORT: " (Get-TinyUrl $installUserScript)
Write-Host "ADMIN_SCRIPT_SHORT: " (Get-TinyUrl $installAdminScript)
