# SVLL IT Support Workstation - Release Notes v7.1

**Release Date:** October 2026  
**Author:** Kunal Turkar  
**Organization:** Shree Vasu Logistics Limited  
**Target Platform:** Windows 10 / 11 (x64) • .NET 8.0 WPF  

---

## 🚀 What's New in v7.1

SVLL IT Support Workstation v7.1 introduces the **Dashboard AI Quick Support Copilot**, comprehensive workstation feature execution from chat, and critical UI enhancements for the Enterprise Service Desk:

### 1. 🤖 SVLL AI Quick Support Copilot (Executive Dashboard)
- **Embedded Dashboard Copilot**: Seamlessly integrated directly on the Executive Operations Dashboard with expand/minimize toggle and quick-clear controls.
- **Dual AI Intelligence Architecture**:
  - **Free Cloud AI API Integration**: Directly queries high-performance cloud AI (`Pollinations.ai`) to answer any general, hardware, Windows, networking, or logistics question accurately and politely.
  - **Intelligent Local Knowledge Brain**: Offline, zero-latency local fallback containing 37+ enterprise troubleshooting recipes, Windows repairs, and diagnostic rules that work guaranteed even without an internet connection.
- **1-Click Feature Execution from Chat**:
  - `⚡ 1-Click Turbo Boost`: Recovers fragmented RAM, trims working sets, terminates telemetry, and activates Ultimate Performance power plan.
  - `🖨️ Fix Print Spooler`: Automatically halts Spooler, purges locked `.SPL` & `.SHD` phantom job files from `System32\spool\PRINTERS`, and restarts the print subsystem.
  - `🌐 Ping Gateway & DNS`: Sends continuous ICMP echo packets to test gateway and public DNS latency, reporting jitter and packet loss.
  - `🧹 Purge Temp Files`: Cleans user `%TEMP%`, `C:\Windows\Temp`, and empties the Windows Recycle Bin.
  - `📋 File IT Ticket`: 1-click jump to the official IT Complaint Registration form with automated telemetry attachments.
  - Direct execution chips for AnyDesk Remote Client, USB Depot, App Uninstaller, Process Shield, S.M.A.R.T. Drive Health, and IT Command Playbook.
- **Interactive Chat Stream**: Sleek message bubbles with timestamps, source pills (`🌐 CLOUD AI` / `⚡ LOCAL AI`), suggestion chips, and interactive action buttons embedded directly within AI answers.

### 2. 📋 Service Desk Header Overlap Fix & Category Normalization
- **Responsive Layout**: Replaced rigid horizontal stack panels with a responsive `WrapPanel` and horizontal scroll constraints on `ScrollViewer`. Badges (`HUB 7 • SERVICE DESK`, `🟢 LIVE SYNC: Complain Log sheet IT - New Complaint.csv`) wrap gracefully without colliding with action buttons (`📊 Open Log Sheet`, `📁 Open Folder`, `🤖 AI Diagnostic Intake`) at any window width.
- **Purged "IT" Prefixes from Complaint Categories**: Normalized dropdown options and preset chips from internal uppercase spreadsheet labels to real-world incident categories:
  - `⚡ PC / Laptop Working Slow`
  - `🌐 Internet / Network Connectivity Problem`
  - `📧 Outlook / Corporate Email Issue`
  - `🔧 Computer Hardware Repair & Maintenance`
  - `📹 Security Camera / CCTV Down`
  - `🔋 UPS / Power Backup Problem`
  - `🏢 Boss Not Working (ERP / WMS)`
  - `🖨️ Printer Not Working (Laser / Network)`
  - `📦 Software Installation / Update`
  - `🔒 USB DLP Access (Removable Media)`
  - `❓ Other Technical Problem`

### 3. 🔄 Seamless In-App Upgrades
- Fully compatible with the in-app automatic updater (`MainWindow.Knowledge.cs`).
- Workstations running v7.0 or earlier will automatically detect the **v7.1** release on GitHub, download `SVLL-IT-Workstation-v7.1-Setup.exe`, and perform an in-place upgrade without data loss.

---

## 📦 Distribution Packages

- **Setup Installer:** `SVLL-IT-Workstation-v7.1-Setup.exe` (2.98 MB) - Inno Setup installer with transparent icon and live CSV sync sheet.
- **Standalone Binary:** `SVLL-IT-Workstation.exe` (1.76 MB) - Win-x64 single-file executable.
