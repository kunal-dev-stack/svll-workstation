# SVLL IT Support Workstation (v6.0)

[![Platform](https://img.shields.io/badge/Platform-Windows%2010%20%2F%2011%20(x64)-blue.svg)](https://microsoft.com)
[![Framework](https://img.shields.io/badge/Framework-.NET%208.0%20WPF-purple.svg)](https://dotnet.microsoft.com)
[![License](https://img.shields.io/badge/License-Proprietary%20SVLL-darkred.svg)](#)
[![Release](https://img.shields.io/badge/Release-v6.0-success.svg)](#)

> **Enterprise IT Diagnostics, Fleet Management & Automated Remediation Suite**  
> Developed for **Shree Vasu Logistics Limited** by **Kunal Turkar**.

---

## 🚀 Overview

**SVLL IT Support Workstation** is a high-performance desktop workstation engineered specifically for corporate logistics hubs, branch networks, and warehouse computer fleets. It delivers real-time system vitals, zero-touch new PC provisioning, offline USB deployment, automated Microsoft WHQL driver catalog installation, logistics hardware diagnostics (Zebra/TSC & COM serial sniffer), S.M.A.R.T. storage crash prediction, endpoint USB DLP security, 1-click RAM & CPU acceleration, network troubleshooting, unattended software deployment, automated Windows debloating, and 24/7 endpoint reliability.

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

> **How it works**: This one-liner downloads the official setup package directly into memory, validates dependencies, executes silent unattended deployment into `C:\Program Files\ShreeVasuLogistics\ITWorkstation`, creates Start Menu and Desktop shortcuts, and launches the workstation immediately.

---

## 🏛️ 5-Hub Enterprise Architecture

The workstation consolidates **34 advanced administrative tools** into 5 structured workspaces:

1. **Live Monitoring & Triage (`Hub_Monitoring`)**: System Vitals, Continuous Watchdog, Latency Tests, Bandwidth Speed Test.
2. **Network Operations Center (`Hub_Network`)**: IPConfig & Netsh Suite, Network & DNS Profiles, Subnet IP Scanner (Free vs Occupied), Active Ports & Bandwidth Hog Hunter, Wi-Fi Keys & Diagnostics, LAN File Shares.
3. **System Optimization & Servicing (`Hub_Optimization`)**: 1-Click Turbo Booster, Endpoint Security & USB Storage DLP, WinUtil Debloat, Storage TRIM & Temp Purge, Windows Optional Features, Windows Update Strategy, Baseline Config Manager.
4. **Fleet Diagnostics & Helpdesk (`Hub_Diagnostics`)**: Windows OS Repairs, Logistics Hardware Doctor (Zebra/TSC & Serial COM), S.M.A.R.T. Drive Health & Crash Predictor, Print Spooler, Services & Processes, Event Log Analyzer, PC Health & Battery Report, Hardware Asset Passport & QR, IT Support Bundle.
5. **Command Playbook & Deployment (`Hub_Playbook`)**: Zero-Touch New PC Setup Wizard, 37 IT Dual-Syntax Fixes, Offline USB Software & Driver Depot, App Uninstaller & Nuclear Force Purge, WinGet Software Deployer, Local Users & Vault, Updates & About.

---

## 🌟 Next-Gen Features in v6.0

### 1. 🚀 Zero-Touch New PC Provisioning Pipeline (`Hub 5 -> ProvisioningWizard`)
* **Standardized Corporate Identity**: Automatically generates hostnames following enterprise conventions (`SVLL-<LOC>-<TYPE>-<SERIAL>` e.g., `SVLL-BOM-LAP-8921`).
* **1-Click Commissioning Pipeline**:
  1. Sets corporate computer name & validates workgroup.
  2. Ingests OEM & Microsoft WHQL certified drivers via kernel PnP bus scan.
  3. Deploys standard software suite (Chrome, 7-Zip, AnyDesk, Acrobat Reader, Java).
  4. Configures Ultimate Performance power plan and disables sleep on AC.
  5. Audits Windows Defender real-time protection baseline.
  6. Exports a timestamped Commissioning Certificate and Asset Passport QR to the Desktop.

### 2. 🖨️ Logistics Hardware Doctor (`Hub 4 -> LogisticsDoctor`)
* **Direct RAW Spooler ZPL/TSPL Sender**: Sends pure ASCII printer control code (`^XA...^XZ`) directly to USB, Network, and LPT thermal printers via `winspool.drv` using the `RAW` datatype—completely bypassing buggy Windows graphics drivers and rasterization distortions.
* **Serial RS-232 / USB COM Port Sniffer**: Live monitor for weighbridge digital indicators, dimensional scales, and serial barcode scanners with configurable baud rates (`9600`, `19200`, `115200 8-N-1`).
* **Deep Print Spooler Purge**: Forcefully halts `spoolsv.exe`, terminates stuck print handles, deletes corrupt `.SHD` and `.SPL` lockfiles in `C:\Windows\System32\spool\PRINTERS`, and restarts the print spooler.

### 3. 💾 S.M.A.R.T. Storage Health & Crash Predictor (`Hub 4 -> StorageSmart`)
* **Direct Controller Register Inspection**: Directly queries NVMe and SATA registers via WMI (`MSStorageDriver_FailurePredictStatus`, `Win32_DiskDrive`, `Win32_StorageReliabilityCounter`).
* **Predictive Metrics**: Real-time display of Remaining Life (Wear %), Power-On Hours, Reallocated Bad Sectors, Controller Operating Temperatures, and Terabytes Written (TBW).
* **1-Click Diagnostics Export**: Saves a detailed disk reliability report to the Desktop for warranty claims and predictive replacements.

### 4. 🔒 Endpoint Security, DLP & USB Port Lockdown (`Hub 3 -> SecurityDlp`)
* **USB Storage DLP Policy**:
  * `🔒 Set USB Storage Read-Only`: Sets `StorageDevicePolicies\WriteProtect = 1` to block corporate data exfiltration while still allowing users to read documents.
  * `🚫 Block USB Storage Completely`: Disables `usbstor.sys` (`Start = 4`) to prevent unauthorized USB flash drives while keeping USB mice, keyboards, and barcode scanners functional.
  * `🟢 Restore Unrestricted Access`: Restores default read/write access.
* **Local Admin Vault (LAPS)**: Generates cryptographically secure 16-character passwords and rotates local administrator credentials on demand.
* **Defender Cloud Shield**: Live real-time protection status and 1-click cloud definition updates + background quick scan.

### 5. 📡 Active Ports & Bandwidth Hog Hunter (`Hub 2 -> SocketInspector`)
* **Live TCP Socket Mapping**: Maps every open port and established outbound connection directly to process executable names (`Get-NetTCPConnection`).
* **Bandwidth Hog Hunter**: Identifies unauthorized background downloaders or torrents saturating warehouse internet links.
* **1-Click Process Termination**: Instantly terminates rogue processes to liberate branch office bandwidth.

### 6. 🔍 Global Command Palette (`Ctrl + K`)
* **Instant Spotlight Navigation**: Press `Ctrl + K` anywhere in the app to open the fuzzy search palette.
* **Instant Action Execution**: Search any of the 34 tools, playbook commands, or quick actions (e.g. `flush dns`, `turbo`, `zebra`, `laps`) and execute in one keystroke!

### 7. 🔌 Offline USB Software & Driver Depot (`Hub 5 -> UsbDepot`)
* **Auto-Discovery & Folder Structure**: Automatically discovers connected USB thumb drives and initializes depot folders (`\Software`, `\Drivers`, `\Scripts`).
* **Intelligent Silent Switch Detection**: Automatically applies silent unattended switches for `.msi`, `.exe`, `.inf`, and script files.
* **Sequential Batch Deployment**: Deploys multi-app queues with live progress reporting and safety restore points.

### 8. 🗑️ App Uninstaller & Nuclear Force Purge (`Hub 5 -> AppUninstaller`)
* **Silent Elevated Uninstall**: Removes software silently without prompting for UAC or admin passwords.
* **Nuclear Force Purge**: For stubborn apps protected by uninstall passwords: kills process trees, destroys services, reclaims NTFS ownership (`takeown` / `icacls`), and scrubs registry keys.

### 9. ⚡ 1-Click RAM & CPU Turbo Booster
* Flushes working set memory pages (`EmptyWorkingSet`), terminates non-essential background telemetry processes, and activates Windows Ultimate Performance power mode.

### 10. 📖 37 Dual-Syntax Enterprise Fixes Playbook
* Ready-to-run PowerShell and CMD code snippets for network resets, OS integrity scans, print repairs, user administration, and warehouse endpoint troubleshooting.

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
3. Compile the setup package with custom transparent icons into `release/SVLL-IT-Workstation-v6.0-Setup.exe`.

---

## 📦 Distribution Packages & SHA-256 Checksums

| Package | Filename | Size | SHA-256 Checksum |
| :--- | :--- | :--- | :--- |
| **Enterprise Installer** | `release\SVLL-IT-Workstation-v6.0-Setup.exe` | 64.08 MB | `C1E2CBA519F4E2FDAD2ECE64891216A7ED00287C1732095ABE7B239D564D05D3` |
| **Portable Executable** | `publish\SVLL-IT-Workstation.exe` | 68.89 MB | `E3D3EA2137F9C32B205A78C08CAF03F9BF9897E7E0DA1895D3786DCC0ECC8079` |

---

## 👤 Author & Organization
- **Developer:** Kunal Turkar
- **Organization:** Shree Vasu Logistics Limited
- **Version:** 6.0 (Enterprise Edition)
