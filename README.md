# SVLL IT Support Workstation (v5.5)

[![Platform](https://img.shields.io/badge/Platform-Windows%2010%20%2F%2011%20(x64)-blue.svg)](https://microsoft.com)
[![Framework](https://img.shields.io/badge/Framework-.NET%208.0%20WPF-purple.svg)](https://dotnet.microsoft.com)
[![License](https://img.shields.io/badge/License-Proprietary%20SVLL-darkred.svg)](#)

> **Enterprise IT Diagnostics, Fleet Management & Automated Remediation Suite**  
> Developed for **Shree Vasu Logistics Limited** by **Kunal Turkar**.

---

## 🚀 Overview

**SVLL IT Support Workstation** is a high-performance desktop application engineered specifically for corporate logistics hubs, branch networks, and warehouse computer fleets. It delivers real-time system vitals, network troubleshooting, unattended software deployment, automated Windows debloating, and 24/7 endpoint reliability.

---

## 🌟 Upgraded Architecture & Key Features

### 1. Overview & Monitoring
- **Real Active IPv4 Resolution**: Dynamically identifies the primary operational network interface with default gateway routing, eliminating `127.0.0.1` loopback display. Reports physical MAC address, subnet mask, assigned IPv4, and gateway.
- **Deep System Topology**: Real-time extraction of CPU model & core count, installed physical RAM capacity & headroom, GPU display adapter & VRAM, Motherboard manufacturer & model, BIOS serial number / Service Tag, OS build number, and live system uptime.
- **Real-Time Visual Waveforms**: Live rolling sparkline waveforms for CPU workload and RAM utilization with gradient fills and smooth polyline rendering.
- **24/7 Continuous Ping Watchdog**: Multi-target ping audit and 24/7 continuous watchdog with acoustic failure chimes, consecutive drop tracking, and support for importing/exporting target lists via `.csv`, `.txt`, and `.json`.
- **Live WAN Speed Test**: Measures download bandwidth throughput (Mbps) and latency against global CDN endpoints.

### 2. Network Operations
- **Network & DNS Profiles**: 1-click switching between DHCP automatic addressing and static IP configurations (Subnet, Gateway, and DNS presets: Google `8.8.8.8`, Cloudflare `1.1.1.1`, Quad9 `9.9.9.9`, OpenDNS).
- **Concurrent Subnet IP Scanner (Free vs. Occupied)**: Scans all 254 endpoints on the local Class-C network (192.168.x.1–254) in seconds using concurrent worker threads. Resolves hostnames, flags occupied vs. free IPs with color badges, and enables 1-click export of unassigned free IPs to clipboard or `.txt` for static allocation.
- **IPConfig & Netsh Reset Suite**: 1-click execution for Winsock catalog reset, TCP/IP stack re-initialization, DNS resolver cache purge, DHCP lease release/renewal, and network adapter restart.
- **Wi-Fi Profile Manager & Key Extraction**: Audits configured wireless SSIDs, extracts cleartext WPA2/WPA3 pre-shared keys, and exports network XML configurations.
- **LAN SMB/UNC Share Explorer & Push**: Connects to central server file shares (`\\server\share`), browses installers over the network, maps persistent drives, and executes or copies packages locally to `C:\SVLL_Deployments`.

### 3. Logistics & Warehouse Operations
- **Thermal Label Printer Diagnostics**:
  - Live discovery of Zebra, TSC, Honeywell, and desktop label printers across USB and TCP network ports.
  - One-click raw ZPL/EPL test barcode label dispatch.
  - Spooler purge and RAW Port 9100 TCP socket connectivity tester for network thermal printers.
- **Handheld Barcode Scanner Live Test Bench**:
  - Direct hardware test input bench for USB & Bluetooth barcode scanners (1D/2D, QR, Code128, DataMatrix).
  - Live capture of scan duration (ms), character length, and decoded payload with acoustic chime and clipboard copy.
- **WMS & ERP Client Latency Tester**:
  - Live TCP handshake diagnostics for warehouse backend systems: WMS database (MSSQL port 1433), Corporate ERP (Oracle port 1521), Logistics API Web Gateway (HTTPS port 443), Active Directory LDAP (port 389), and Raw Print Server (port 9100).

### 4. Enterprise Asset & Helpdesk
- **Hardware Asset Passport & Dynamic QR Code**:
  - Live generation of a scannable on-screen QR Code containing asset hostname, serial number, OS build, IP, MAC address, and RAM capacity.
  - One-click hardware Bill-of-Materials (BOM) export to CSV for IT asset management (ITAM).
- **Windows Event Log & Crash Analyzer**:
  - Real-time audit of critical Windows events over the last 48 hours: Kernel-Power / BSOD (Event 41), Application crashes (Event 1000/1002), Disk controller errors (Event 51), and Service Control Manager errors (Event 7031).
  - Direct crash event viewer with copy-to-clipboard diagnostics.
- **1-Click Diagnostic Support Bundle**:
  - Automated ZIP packaging of system hardware specs, full IPConfig dumps, battery reports, and active event log summaries into `SVLL_Support_Bundle_<Hostname>_<Timestamp>.zip` on the Desktop.
  - Quick-launch buttons for Quick Assist, AnyDesk, and Remote Desktop (MSTSC).

### 5. System Administration
- **Windows Debloat & Tweaks**: Applies fleet debloat optimizations—disabling background telemetry, removing consumer UWP AppX bloatware, showing file extensions in Explorer, disabling Bing in Start, and enabling "End Task" on taskbar.
- **WinGet Software Deployer**:
  - **1-Click Priority Deployments**: Dedicated unattended buttons for **Microsoft 365 Enterprise** (`Microsoft.Office`) and **WinZip** (`WinZipComputing.WinZip`).
  - **Batch Silent Deployer**: Automated batch installer for Chrome, Firefox, 7-Zip, AnyDesk, Notepad++, VS Code, VLC, Wireshark, etc.
- **Windows Optional Features (DISM)**: 1-click toggle for Hyper-V, Windows Sandbox, WSL, Telnet Client, TFTP, and legacy SMB 1.0.
- **Windows Update Strategy**: Configurable update policies (Recommended corporate schedule, factory default, or complete service disablement for uninterrupted operations).
- **Local SAM Users & Windows Vault Admin**:
  - Audits configured local user accounts, active/disabled status, and Administrator group memberships.
  - **1-Click Administrative Password Reset**: Instant password overrides without requiring previous credentials.
  - Windows Vault inspection (`cmdkey /list`) for stored domain and network credentials.

### 6. Diagnostics & Health
- **PC Health & Battery Report Generator**: Audits foundational stability factors with an overall calculated health score (0-100%), automated one-click remediation, and a dedicated **1-click laptop battery report generator** (`powercfg /batteryreport`) that automatically opens the generated HTML report in the browser.
- **System Repair & DISM**: Integrated `sfc /scannow`, `dism /Online /Cleanup-Image /RestoreHealth`, `chkdsk`, and WinSxS component store cleanup.
- **Storage Volumes & SSD TRIM**: Drive volume usage visualization and 1-click SSD TRIM optimization (`Optimize-Volume`).
- **Windows Services Controller**: Real-time status monitoring and restart controls for Print Spooler, Windows Update, BITS, and Remote Desktop.
- **Print Spooler Diagnostic**: Restarts the spooler service and purges locked `.SPL` and `.SHD` print files from `%SystemRoot%\System32\Spool\PRINTERS`.

### 7. Knowledge Base & System
- **22-Scenario IT Troubleshooting Library**: Verified operational playbooks across Network, Printers, Windows OS, Active Directory, Security, Storage, and Performance with dedicated **Copy** and **Run** buttons for both CMD and PowerShell.
- **Disk & Temp Cleanup**: 1-click purge for `%TEMP%`, `C:\Windows\Temp`, `C:\Windows\Prefetch`, and Recycle Bin.
- **Configuration Presets Manager**: Export, import, and apply unified JSON configuration baselines across computers.
- **Integrated Terminal**: Built-in interactive PowerShell and CMD console with command history and live standard output streaming.

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
3. Compile the setup package with our custom transparent icon into `release/SVLL-IT-Workstation-v5.5-Setup.exe`.

---

## 📦 One-Click Silent Installation

To deploy the workstation silently to endpoints:
```powershell
powershell -ExecutionPolicy Bypass -File .\install.ps1
```
Or directly from the cloud:
```powershell
irm https://raw.githubusercontent.com/kunal-dev-stack/svll-workstation/main/install.ps1 | iex
```

---

## 👤 Author
- **Developer:** Kunal Turkar
- **Organization:** Shree Vasu Logistics Limited
- **Version:** 5.5 (Enterprise Edition)
