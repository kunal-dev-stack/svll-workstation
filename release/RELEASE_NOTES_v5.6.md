# SVLL IT Support Workstation v5.6 - Official Enterprise Release

**Release Date:** October 5, 2026  
**Lead Developer:** Kunal Turkar | SVLL IT Department  
**Company:** Shree Vasu Logistics Limited  
**Target Systems:** Windows 10 (64-bit) & Windows 11 (64-bit)  
**Framework:** .NET 8.0 Windows Desktop (Self-Contained Single-File Binary)  

---

## 🚀 Executive Release Highlights

Version 5.6 is a major enterprise release of the **SVLL IT Support Workstation**, engineered to deliver automated PC/laptop provisioning, offline USB deployment, cloud WHQL driver downloads, 1-click RAM & CPU acceleration, a 37-scenario dual-syntax IT command playbook, an elevated **App Uninstaller & Nuclear Force Purge** with password bypass, and a streamlined 5-Hub enterprise navigation architecture.

---

## 🌟 New Features in v5.6

### 1. 🗑️ App Uninstaller & Nuclear Force Purge (Password-Bypass Removal) (`Hub 5 -> AppUninstaller`)
* **Live Registry Software Scanner**: Automatically audits 64-bit (`HKLM`), 32-bit WOW64 (`HKLM`), and User (`HKCU`) registry uninstall hives with live instant search and filter pills (`ALL`, `64-BIT`, `32-BIT`, `MSI`).
* **Standard Elevated Silent Uninstall**: Runs the software uninstaller in the workstation's elevated Administrator context with silent parameters (`/qn`, `/VERYSILENT`, `/S`), preventing Windows UAC from prompting for an administrator password.
* **🔥 Nuclear Force Purge & Bypass (Zero Password Required)**: For stubborn software protected by an uninstaller password, or broken/corrupt uninstallers:
  1. Forcibly terminates all running processes of the target application (`Process.Kill(entireProcessTree: true)`).
  2. Stops and unregisters associated Windows background services (`sc.exe delete`).
  3. Takes administrative filesystem ownership (`takeown` and `icacls`) to overcome locked permissions.
  4. Obliterates the application installation folders from disk (`Directory.Delete` & `rd /s /q`).
  5. Wipes the uninstall registration keys from the Windows Registry (`HKLM` and `HKCU`).
  6. Purges startup Run entries and desktop/start menu shortcuts.
  7. **COMPLETELY BYPASSES ANY UNINSTALL PASSWORD WITH ZERO PASSWORDS REQUIRED.**

---

### 2. 🔌 Offline USB Software & Driver Depot (`Hub 5 -> UsbDepot`)
* **Removable Drive Auto-Discovery**: Automatically enumerates connected USB thumb drives and displays volume labels, free space, and capacity (e.g. `E:\ [SVLL_IT_USB] (14.8 GB Free)`).
* **Universal Repository Support**: Allows browsing any external SSD, local directory, or network file share (`\\server\it_depot`).
* **1-Click USB Structure Initialization**: Automatically creates the standard enterprise folder structure on your USB drive (`\SVLL_Depot\Software`, `\Drivers`, `\Scripts`, and a sample `deploy.json`).
* **Deep Recursive File Scanner**: Recursively scans all subfolders up to 5 levels deep for deployable packages (`.msi`, `.exe`, `.inf`, `.ps1`, `.bat`, `.cmd`).
* **Intelligent Silent Argument Detection**:
  * `.msi` ➔ Auto-applies `msiexec.exe /i "<path>" /qn /norestart`.
  * `.inf` Drivers ➔ Auto-applies `pnputil.exe /add-driver "<path>" /install`.
  * `.exe` Binaries ➔ Automatically detects installer families (Chrome, AnyDesk, TeamViewer, 7-Zip, VLC, Notepad++, Adobe Acrobat Reader) and applies universal silent switches (`/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP-` or `/S` or `/quiet`). Switches remain fully editable in the table.
  * `.ps1` / `.bat` Scripts ➔ Auto-applies `powershell.exe -ExecutionPolicy Bypass -File` and `cmd.exe /c`.
* **Sequential Batch Execution Engine**:
  * `🚀 1-CLICK INSTALL ALL SELECTED FROM USB` deploys all selected items in sequence.
  * Real-time progress bar, current item status, and badges (`Ready` ➔ `Installing...` ➔ `✅ Installed` or `❌ Error`).
  * Safety controls: `⏹️ Abort Queue` button and optional Windows System Restore Point creation.
  * Automatically saves `deploy_installed_profile.json` record to the USB drive.

---

### 3. 🌐 Automated Online Driver Finder & Downloader (`Hub 5 -> UsbDepot`)
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

---

### 4. 💾 1-Click Driver Export / Backup to USB (`Hub 5 -> UsbDepot`)
* Backs up all installed third-party drivers (`pnputil.exe /export-driver * "<USB>\SVLL_Depot\Drivers\<Model>"`) directly into cleanly organized `.inf` folders on your USB flash drive.
* Offline deployment: When configuring the next laptop of the same model, plug the USB drive in, switch to Tab 1, and the USB Depot will install all drivers offline in under 30 seconds!

---

### 5. ⚡ 1-Click RAM & CPU Turbo Booster (`Top Status Bar`, `Dashboard`, and `Hub 3`)
* **Working Set Memory Compaction**: Calls Win32 `EmptyWorkingSet` and `SetProcessWorkingSetSize` across all user-space processes to flush stale pages and commit cached memory back to the available pool.
* **Telemetry & Bloat Process Termination**: Safely halts non-critical telemetry, background updaters, and advertising tasks (`CompatTelRunner`, `mscorsvw`, `GameBarFTServer`, `smartscreen`).
* **Ultimate Performance Power Plan**: Activates Windows' zero-throttling "Ultimate Performance" or "High Performance" power profile (`powercfg /setactive`).
* Real-time metrics: Displays exact megabytes of RAM released, trimmed processes count, and terminated bloat tasks.

---

### 6. 📖 IT Fix & Command Playbook (`Hub 5 -> Library`)
* **37 Enterprise Scenarios across 7 Categories**:
  1. *Network & Connectivity Fixes* (DNS Flush, Winsock Reset, ARP Clear, Gateway Ping, Net Adapter Reset, Release/Renew DHCP)
  2. *Windows OS & Integrity Repairs* (SFC Scannow, DISM RestoreHealth, WinSxS Component Cleanup, CheckDisk, Spooler Reset, Explorer Restart)
  3. *Performance & Storage Optimization* (Clear Temp Dirs, Windows Temp Purge, Storage TRIM, Clear Delivery Optimization, Hibernate Disable)
  4. *Security & Local Administration* (List Local Users, Enable Local Admin, Unlock User Account, Query Windows Credential Vault, Firewall Enable)
  5. *Hardware & Fleet Diagnostics* (Battery Health Report, Query Motherboard Serial, Export System Drivers, Re-enumerate PnP Devices, Device Manager)
  6. *Active Directory & Domain Group Policy* (Force Group Policy Update, Query Applied GPOs, Test Domain Trust, Reset Computer Account)
  7. *Warehouse & Logistics Endpoint Fixes* (Query Zebra/TSC Printers, Clear Thermal Print Spooler, Test WMS Latency, Test Barcode Port)
* **Dual Syntax View**: Displays both CMD and PowerShell code side-by-side with 1-click **Copy CMD**, **Copy PowerShell**, and **⚡ Run Directly in Workstation Console** buttons.

---

### 7. 🏛️ 5-Hub Enterprise Navigation Architecture
Consolidates 28 administrative tools into 5 structured workspaces:
1. **Live Monitoring & Triage (`Hub_Monitoring`)**: System Vitals, Continuous Watchdog, Latency Tests, Bandwidth Speed Test.
2. **Network Operations Center (`Hub_Network`)**: IPConfig, Subnet IP Scanner (Free vs Occupied), Netsh Suite, Wi-Fi Keys, LAN Shares.
3. **System Optimization & Servicing (`Hub_Optimization`)**: 1-Click Turbo Booster, WinUtil Debloat, Storage TRIM & Temp Purge, DISM Features, Windows Update Strategy, Config Manager.
4. **Fleet Diagnostics & Helpdesk (`Hub_Diagnostics`)**: Windows OS Repairs, Print Spooler, Services & Processes, Event Log Analyzer, PC Health & Battery Report, Hardware Asset Passport & QR, IT Support Bundle.
5. **Command Playbook & Deployment (`Hub_Playbook`)**: 37 IT Dual-Syntax Fixes, Offline USB Software & Driver Depot, WinGet Software Deployer, App Uninstaller & Force Purge, Local Users & Vault, Updates & About.

---

## 📦 Distribution Packages & SHA-256 Checksums

| Package | Filename | Size | SHA-256 Checksum |
| :--- | :--- | :--- | :--- |
| **Enterprise Installer** | `SVLL-IT-Workstation-v5.6-Setup.exe` | 63.96 MB | `C161ABF682DAF8E5CB0A21CF0E09FA05658925C3A2D2D8CFA6606C1D118982DC` |
| **Portable Executable** | `SVLL-IT-Workstation.exe` | 68.79 MB | `5B020B7B1433855E299064074C9B4533016B96138D5890D163D1458A9316507B` |

---

## 🛠️ Installation & Execution

### Option A: Standard Inno Setup Installation
Run `SVLL-IT-Workstation-v5.6-Setup.exe` with administrative privileges. The wizard will create desktop shortcuts, start menu entries, and an uninstaller.

### Option B: Zero-Install Standalone Execution
Copy `SVLL-IT-Workstation.exe` to any folder or USB drive and run directly:
```powershell
Start-Process -FilePath ".\SVLL-IT-Workstation.exe"
```
No .NET runtime installation is required (self-contained win-x64 binary).
