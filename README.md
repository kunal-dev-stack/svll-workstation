# SVLL IT Support Workstation (v5.6)

[![Platform](https://img.shields.io/badge/Platform-Windows%2010%20%2F%2011%20(x64)-blue.svg)](https://microsoft.com)
[![Framework](https://img.shields.io/badge/Framework-.NET%208.0%20WPF-purple.svg)](https://dotnet.microsoft.com)
[![License](https://img.shields.io/badge/License-Proprietary%20SVLL-darkred.svg)](#)
[![Release](https://img.shields.io/badge/Release-v5.6-success.svg)](#)

> **Enterprise IT Diagnostics, Fleet Management & Automated Remediation Suite**  
> Developed for **Shree Vasu Logistics Limited** by **Kunal Turkar**.

---

## 🚀 Overview

**SVLL IT Support Workstation** is a high-performance desktop workstation engineered specifically for corporate logistics hubs, branch networks, and warehouse computer fleets. It delivers real-time system vitals, offline USB deployment, automated Microsoft WHQL driver catalog installation, 1-click RAM & CPU acceleration, network troubleshooting, unattended software deployment, automated Windows debloating, and 24/7 endpoint reliability.

---

## ⚡ 1-Click System Deployment (PowerShell)

To deploy or update SVLL IT Support Workstation on any clean PC or laptop in seconds, open **Windows PowerShell** (Run as Administrator) and run:

```powershell
irm https://tinyurl.com/svll-setup | iex
```

*Alternative mirror:*
```powershell
irm https://tinyurl.com/svll-workstation | iex
```

> **How it works**: This one-liner downloads the official v5.6 setup package from GitHub releases directly into memory, validates dependencies, executes silent unattended deployment into `C:\Program Files\ShreeVasuLogistics\ITWorkstation`, creates Start Menu and Desktop shortcuts, and launches the workstation immediately.

---

## 🏛️ 5-Hub Enterprise Architecture

The workstation consolidates 28 advanced administrative tools into 5 structured workspaces:

1. **Live Monitoring & Triage (`Hub_Monitoring`)**: System Vitals, Continuous Watchdog, Latency Tests, Bandwidth Speed Test.
2. **Network Operations Center (`Hub_Network`)**: IPConfig, Subnet IP Scanner (Free vs Occupied), Netsh Suite, Wi-Fi Keys, LAN Shares.
3. **System Optimization & Servicing (`Hub_Optimization`)**: 1-Click Turbo Booster, WinUtil Debloat, Storage TRIM & Temp Purge, DISM Features, Windows Update Strategy, Config Manager.
4. **Fleet Diagnostics & Helpdesk (`Hub_Diagnostics`)**: Windows OS Repairs, Print Spooler, Services & Processes, Event Log Analyzer, PC Health & Battery Report, Hardware Asset Passport & QR, IT Support Bundle.
5. **Command Playbook & Deployment (`Hub_Playbook`)**: 37 IT Dual-Syntax Fixes, Offline USB Software & Driver Depot, App Uninstaller & Nuclear Force Purge, WinGet Software Deployer, Local Users & Vault, Updates & About.

---

## 🌟 Key Features in v5.6

### 1. 🔌 Offline USB Software & Driver Depot
* **USB Flash Drive Auto-Discovery**: Automatically enumerates connected removable USB drives and displays volume labels, free space, and capacity (e.g. `E:\ [SVLL_IT_USB] (14.8 GB Free)`).
* **Universal Repository Support**: Allows browsing any external SSD, local directory, or network file share (`\\server\it_depot`).
* **1-Click USB Structure Initialization**: Automatically creates the standard enterprise folder structure on your USB drive (`\SVLL_Depot\Software`, `\Drivers`, `\Scripts`, and a sample `deploy.json`).
* **Deep Recursive File Scanner**: Recursively scans all subfolders up to 5 levels deep for deployable packages (`.msi`, `.exe`, `.inf`, `.ps1`, `.bat`, `.cmd`).
* **Intelligent Silent Argument Detection**:
  * `.msi` ➔ Auto-applies `msiexec.exe /i "<path>" /qn /norestart`.
  * `.inf` Drivers ➔ Auto-applies `pnputil.exe /add-driver "<path>" /install`.
  * `.exe` Binaries ➔ Automatically detects installer families (Chrome, AnyDesk, TeamViewer, 7-Zip, VLC, Notepad++, Adobe Acrobat Reader) and applies universal silent switches (`/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP-` or `/S` or `/quiet`). Switches remain fully editable.
  * `.ps1` / `.bat` Scripts ➔ Auto-applies `powershell.exe -ExecutionPolicy Bypass -File` and `cmd.exe /c`.
* **Sequential Batch Execution Engine**:
  * `🚀 1-CLICK INSTALL ALL SELECTED FROM USB` deploys all selected items in sequence.
  * Real-time progress bar, current item status, and badges (`Ready` ➔ `Installing...` ➔ `✅ Installed` or `❌ Error`).
  * Safety controls: `⏹️ Abort Queue` button and optional Windows System Restore Point creation.
  * Automatically saves `deploy_installed_profile.json` record to the USB drive.

### 2. 🌐 Automated System Driver Finder & Downloader
* **OEM Hardware Identification**: Reads Motherboard, Computer Model, and BIOS Serial / Service Tag via WMI.
* **1-Click OEM Support Portals**: Opens the browser directly to the manufacturer's exact support page for that **Dell Service Tag**, **Lenovo Serial**, **HP Serial**, or **ASUS/Acer model**.
* **1-Click OEM Update Tool Launchers**: One-click launches or downloads the official OEM driver utility (Dell Command | Update, Lenovo Vantage, HP Support Assistant, Intel DSA).
* **Missing & Problem Hardware Diagnostic (Yellow Bangs)**:
  * Scans `Win32_PnPEntity` for devices with `ConfigManagerErrorCode > 0` (Code 28 missing drivers, Code 10, Code 43, Code 31).
  * Displays device name, class, manufacturer, and exact Hardware ID (`PCI\VEN_8086&DEV_...`).
  * `⚡ PnP Auto-Bind (pnputil /scan-devices)` triggers immediate kernel hardware bus re-enumeration.
* **Microsoft WHQL Windows Update Driver Catalog Cloud Downloader**:
  * Leverages the native Windows Update COM API (`Microsoft.Update.Session`) to query Microsoft's cloud catalog for certified hardware drivers matching the machine's exact Hardware IDs (`Search("IsInstalled=0 and Type='Driver'")`).
  * Displays certified driver title, hardware class, provider, and release date.
  * `⚡ 1-Click Download & Install Best Drivers (WHQL)` downloads the certified payload and installs it into the Windows Driver Store with live progress reporting.

### 3. 💾 1-Click Driver Export / Backup to USB
* Backs up all installed third-party drivers (`pnputil.exe /export-driver * "<USB>\SVLL_Depot\Drivers\<Model>"`) directly into cleanly organized `.inf` folders on your USB flash drive.
* Offline deployment: When configuring the next laptop of the same model, plug the USB drive in, switch to Tab 1, and the USB Depot will install all drivers offline in under 30 seconds!

### 4. ⚡ 1-Click RAM & CPU Turbo Booster
* **Working Set Memory Compaction**: Calls Win32 `EmptyWorkingSet` and `SetProcessWorkingSetSize` across all user-space processes to flush stale pages and commit cached memory back to the available pool.
* **Telemetry & Bloat Process Termination**: Safely halts non-critical telemetry, background updaters, and advertising tasks (`CompatTelRunner`, `mscorsvw`, `GameBarFTServer`, `smartscreen`).
* **Ultimate Performance Power Plan**: Activates Windows' zero-throttling "Ultimate Performance" or "High Performance" power profile (`powercfg /setactive`).
* Real-time metrics: Displays exact megabytes of RAM released, trimmed processes count, and terminated bloat tasks.

### 5. 📖 IT Fix & Command Playbook
* **37 Enterprise Scenarios across 7 Categories**:
  1. *Network & Connectivity Fixes* (DNS Flush, Winsock Reset, ARP Clear, Gateway Ping, Net Adapter Reset, Release/Renew DHCP)
  2. *Windows OS & Integrity Repairs* (SFC Scannow, DISM RestoreHealth, WinSxS Component Cleanup, CheckDisk, Spooler Reset, Explorer Restart)
  3. *Performance & Storage Optimization* (Clear Temp Dirs, Windows Temp Purge, Storage TRIM, Clear Delivery Optimization, Hibernate Disable)
  4. *Security & Local Administration* (List Local Users, Enable Local Admin, Unlock User Account, Query Windows Credential Vault, Firewall Enable)
  5. *Hardware & Fleet Diagnostics* (Battery Health Report, Query Motherboard Serial, Export System Drivers, Re-enumerate PnP Devices, Device Manager)
  6. *Active Directory & Domain Group Policy* (Force Group Policy Update, Query Applied GPOs, Test Domain Trust, Reset Computer Account)
  7. *Warehouse & Logistics Endpoint Fixes* (Query Zebra/TSC Printers, Clear Thermal Print Spooler, Test WMS Latency, Test Barcode Port)
* **Dual Syntax View**: Displays both CMD and PowerShell code side-by-side with 1-click **Copy CMD**, **Copy PowerShell**, and **⚡ Run Directly in Workstation Console** buttons.

### 6. 🗑️ App Uninstaller & Nuclear Force Purge (Password Bypass)
* **Silent Elevated Uninstall**: Detects installed applications across 64-bit & 32-bit registry hives (`UninstallString` & `QuietUninstallString`) and executes silent uninstallation under NT AUTHORITY\SYSTEM / Administrator without prompting for Windows passwords or UAC.
* **Nuclear Force Purge**: For stubborn software, enterprise agents, or third-party tools protected by uninstallation passwords:
  * **Process Tree Annihilation**: Force-kills all locked binaries and active worker threads (`taskkill /F /IM`).
  * **Service Destruction**: Stops and deletes associated background Windows services (`sc.exe delete`).
  * **Filesystem Takeown**: Reclaims NTFS ownership (`takeown /F /R /D Y` and `icacls /grant administrators:F`), unlocks handles, and deletes the entire installation directory.
  * **Registry Scrubbing**: Purges all traces from `HKLM\Software\Microsoft\Windows\CurrentVersion\Uninstall` and vendor registry trees.

### 7. 🛡️ Enterprise Stability & Logging
* **Top Status Bar**: Live persistent header displaying Hostname, Logged User, Active IP, Default Gateway (`⚡ GW: IP 🟢`), live CPU %, live RAM %, and Quick Device Audit Log viewer.
* **Persistent Device Action & Audit Log**: Automatically writes every administrative action to `%APPDATA%\SVLL_IT_Workstation\device_action_history.log`.
* **Collapsible Terminal Console**: Header toggle button (`▼ Minimize` / `▲ Expand Terminal`) allows collapsing the terminal to a compact 32px status bar for full-screen workspace visibility.
* **Full-Screen Responsive Scaling**: Automatically launches maximized and scales responsively to monitor resolutions from 1366x768 up to 4K.
* **Windows 10 & 11 Compatible**: Tested and certified for Windows 10 (Build 19041+) and Windows 11.

---

## 🛠️ Build & Packaging Instructions

### Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (Windows x64)
- [Inno Setup 6](https://jrsoftware.org/isdl.php) (Auto-detected in Program Files or AppData)

### Build Executable & Setup Installer
Run the automated build script:
```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\make_project.ps1
```
This script will:
1. Compile the self-contained, single-file Windows x64 binary into `publish/SVLL-IT-Workstation.exe`.
2. Locate the Inno Setup compiler (`ISCC.exe`).
3. Compile the setup package with our custom transparent icon into `release/SVLL-IT-Workstation-v5.6-Setup.exe`.

---

## 📦 Distribution Packages & SHA-256 Checksums

| Package | Filename | Size | SHA-256 Checksum |
| :--- | :--- | :--- | :--- |
| **Enterprise Installer** | `release\SVLL-IT-Workstation-v5.6-Setup.exe` | 63.96 MB | `C161ABF682DAF8E5CB0A21CF0E09FA05658925C3A2D2D8CFA6606C1D118982DC` |
| **Portable Executable** | `publish\SVLL-IT-Workstation.exe` | 68.79 MB | `5B020B7B1433855E299064074C9B4533016B96138D5890D163D1458A9316507B` |

---

## 👤 Author & Organization
- **Developer:** Kunal Turkar
- **Organization:** Shree Vasu Logistics Limited
- **Version:** 5.6 (Enterprise Edition)
