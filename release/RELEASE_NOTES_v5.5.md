# SVLL IT Support Workstation v5.5 - Official Enterprise Release

**Release Date:** October 5, 2026  
**Lead Developer:** Kunal Turkar | SVLL IT Department  
**Company:** Shree Vasu Logistics Limited  
**Target Systems:** Windows 10 (64-bit) & Windows 11 (64-bit)  
**Framework:** .NET 8.0 Windows Desktop (Self-Contained Single-File Binary)  

---

## 🚀 Executive Release Highlights

Version 5.5 is a landmark major release of the **SVLL IT Support Workstation**, introducing automated PC/laptop provisioning, offline USB deployment, cloud WHQL driver downloads, 1-click RAM & CPU acceleration, a 37-scenario dual-syntax IT command playbook, and a streamlined 5-Hub enterprise navigation architecture.

---

## 🌟 New Features in v5.5

### 1. 🔌 Offline USB Software & Driver Depot (`Hub 5 -> UsbDepot`)
* **Removable Drive Auto-Discovery**: Automatically enumerates connected USB thumb drives and displays volume labels, free space, and capacity (e.g. `E:\ [SVLL_IT_USB] (14.8 GB Free)`).
* **Universal Repository Support**: Allows browsing any external SSD, local directory, or network file share (`\\server\it_depot`).
* **1-Click USB Structure Initialization**: Automatically creates the standard enterprise folder structure on your USB drive:
  * `\SVLL_Depot\Software` (Place `.msi` and `.exe` installers here)
  * `\SVLL_Depot\Drivers` (Place `.inf` driver folders here)
  * `\SVLL_Depot\Scripts` (Place `.bat` and `.ps1` automation scripts here)
  * `\SVLL_Depot\deploy.json` (Customizable deployment manifest)
  * `\SVLL_Depot\README_DEPOT.txt`
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

### 2. 🌐 Automated Online Driver Finder & Downloader (`Hub 5 -> UsbDepot`)
* **OEM Hardware Identification**: Reads Motherboard, Computer Model, and BIOS Serial / Service Tag via WMI.
* **1-Click OEM Support Portals**: Opens the browser directly to the manufacturer's exact support page for that **Dell Service Tag**, **Lenovo Serial**, **HP Serial**, or **ASUS/Acer model**.
* **1-Click OEM Update Tool Launchers**: One-click launches or downloads the official OEM driver utility:
  * **Dell**: Launches `Dell Command | Update` (`dcu-cli.exe`) or opens official installer.
  * **Lenovo**: Launches `Lenovo System Update` / `Lenovo Commercial Vantage`.
  * **HP**: Launches `HP Support Assistant` / `HP Image Assistant`.
  * **Intel**: Launches / downloads `Intel Driver & Support Assistant (DSA)`.
* **Missing & Problem Hardware Diagnostic (Yellow Bangs)**:
  * Scans `Win32_PnPEntity` for devices with `ConfigManagerErrorCode > 0` (Code 28 missing drivers, Code 10, Code 43, Code 31).
  * Displays device name, class, manufacturer, and exact Hardware ID (`PCI\VEN_8086&DEV_...`).
  * `⚡ PnP Auto-Bind (pnputil /scan-devices)` triggers immediate kernel hardware bus re-enumeration.
* **Microsoft WHQL Windows Update Driver Catalog Cloud Downloader**:
  * Leverages the native Windows Update COM API (`Microsoft.Update.Session`) to query Microsoft's cloud catalog for certified hardware drivers matching the machine's exact Hardware IDs (`Search("IsInstalled=0 and Type='Driver'")`).
  * Displays certified driver title, hardware class, provider, and release date.
  * `⚡ 1-Click Download & Install Best Drivers (WHQL)` downloads the certified payload and installs it into the Windows Driver Store with live progress reporting.

---

### 3. 💾 1-Click Driver Export / Backup to USB (`Hub 5 -> UsbDepot`)
* Backs up all installed third-party drivers (`pnputil.exe /export-driver * "<USB>\SVLL_Depot\Drivers\<Model>"`) directly into cleanly organized `.inf` folders on your USB flash drive.
* Offline deployment: When configuring the next laptop of the same model, plug the USB drive in, switch to Tab 1, and the USB Depot will install all drivers offline in under 30 seconds!

---

### 4. ⚡ 1-Click RAM & CPU Turbo Booster (`Top Status Bar`, `Dashboard`, and `Hub 3`)
* **Working Set Memory Compaction**: Calls Win32 `EmptyWorkingSet` and `SetProcessWorkingSetSize` across all user-space processes to flush stale pages and commit cached memory back to the available pool.
* **Telemetry & Bloat Process Termination**: Safely halts non-critical telemetry, background updaters, and advertising tasks (`CompatTelRunner`, `mscorsvw`, `GameBarFTServer`, `smartscreen`).
* **Ultimate Performance Power Plan**: Activates Windows' zero-throttling "Ultimate Performance" or "High Performance" power profile (`powercfg /setactive`).
* Real-time metrics: Displays exact megabytes of RAM released, trimmed processes count, and terminated bloat tasks.

---

### 5. 📖 IT Fix & Command Playbook (`Hub 5 -> Library`)
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

### 6. 🏛️ 5-Hub Enterprise Navigation Architecture
Consolidates 27 administrative tools into 5 structured workspaces:
1. **Live Monitoring & Triage (`Hub_Monitoring`)**: System Vitals, Continuous Watchdog, Latency Tests, Bandwidth Speed Test.
2. **Network Operations Center (`Hub_Network`)**: IPConfig, Subnet IP Scanner (Free vs Occupied), Netsh Suite, Wi-Fi Keys, LAN Shares.
3. **System Optimization & Servicing (`Hub_Optimization`)**: 1-Click Turbo Booster, WinUtil Debloat, Storage TRIM & Temp Purge, DISM Features, Windows Update Strategy, Config Manager.
4. **Fleet Diagnostics & Helpdesk (`Hub_Diagnostics`)**: Windows OS Repairs, Print Spooler, Services & Processes, Event Log Analyzer, PC Health & Battery Report, Hardware Asset Passport & QR, IT Support Bundle.
5. **Command Playbook & Deployment (`Hub_Playbook`)**: 37 IT Dual-Syntax Fixes, Offline USB Software & Driver Depot, WinGet Software Deployer, Local Users & Vault, Updates & About.

---

### 7. 🛡️ Enterprise Stability & Logging
* **Top Status Bar**: Live persistent header displaying Hostname, Logged User, Active IP, Default Gateway (`⚡ GW: IP 🟢`), live CPU %, live RAM %, and Quick Device Audit Log viewer.
* **Persistent Device Action & Audit Log**: Automatically writes every administrative action to `%APPDATA%\SVLL_IT_Workstation\device_action_history.log`.
* **Collapsible Terminal Console**: Header toggle button (`▼ Minimize` / `▲ Expand Terminal`) allows collapsing the terminal to a compact 32px status bar for full-screen workspace visibility.
* **Full-Screen Responsive Scaling**: Automatically launches maximized and scales responsively to monitor resolutions from 1366x768 up to 4K.
* **Windows 10 & 11 Compatible**: Tested and certified for Windows 10 (Build 19041+) and Windows 11.

---

## 📦 Distribution Packages & SHA-256 Checksums

| Package | Filename | Size | SHA-256 Checksum |
| :--- | :--- | :--- | :--- |
| **Enterprise Installer** | `SVLL-IT-Workstation-v5.5-Setup.exe` | 63.94 MB | `EF03B797C33C4D29DE87C7492E61A06E21824246E11767CD193BFF148731685B` |
| **Portable Executable** | `SVLL-IT-Workstation.exe` | 68.77 MB | `462562F19C453E814E2CE5271A2177BA86561F92E8631ED155CA21FAA870D8D8` |

---

## 🛠️ Installation & Execution

### Option A: Standard Inno Setup Installation
Run `SVLL-IT-Workstation-v5.5-Setup.exe` with administrative privileges. The wizard will create desktop shortcuts, start menu entries, and an uninstaller.

### Option B: Zero-Install Standalone Execution
Copy `SVLL-IT-Workstation.exe` to any folder or USB drive and run directly:
```powershell
Start-Process -FilePath ".\SVLL-IT-Workstation.exe"
```
No .NET runtime installation is required (self-contained win-x64 binary).
