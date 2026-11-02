# Shree Vasu Logistics Limited - SVLL IT Workstation Suite v8.5

**Release Tag:** `v8.5`  
**Date:** November 2026  
**Author:** Kunal Turkar (IT Department - Shree Vasu Logistics Limited)

---

## 🚀 What's New & Fixed in v8.5

### 1. 24/7 Ping Watchdog & Acoustic Alert Monitoring
- **Persistent Target List:** Added `💾 Save Target List` button so customized watchdog ping targets persist across application sessions in `%APPDATA%\SVLL_IT_Workstation\watchdog_targets.json`.
- **Audible Chime Alert Engine:** Fixed acoustic chimes using dual multi-frequency sound alerts (`1318 Hz` / `880 Hz`) with `SystemSounds.Exclamation` fallback, providing audible alerts on gateway/ERP downtime and state transitions. Added dedicated `🔊 Test Chime` button.

### 2. Enterprise Baseline Configuration Profiles
- **Mojibake Purge:** Fully eliminated encoding corruption in UI typography and profile rendering.
- **⚡ Best Performance Profile:** Activates Windows Ultimate Performance power plan, flushes standby RAM cache, applies TCP window auto-tuning (`autotuninglevel=normal`), and clears telemetry bloat.
- **🏢 Normal Use Profile:** Applies standard Balanced power scheme, clears user temp files, and flushes DNS cache for daily office operations.
- **🛡️ High-Security Profile:** Enforces CIS endpoint compliance, firewall isolation, and DLP protection.

### 3. Multi-Point Deep PC Health Checker & Auto-Fix
- **Expanded to 11 Enterprise Diagnostic Checkpoints:**
  1. System Storage (C: headroom and health)
  2. Physical Memory Load (RAM thresholding)
  3. Pending Windows Reboot Flags (CBS & WindowsUpdate registry keys)
  4. Print Spooler Service & Stuck Queue Job Counter
  5. Windows Firewall Status across Domain, Private, and Public profiles
  6. Windows Defender Antivirus Real-time Protection & Signature Hygiene
  7. Windows Update Services (`wuauserv` & `BITS`)
  8. Local Gateway & External DNS Latency Reachability
  9. Windows Time (NTP W32Time) Synchronization
  10. System & User Temp/Junk Space Accumulation
  11. Battery & Power Status
- **1-Click Auto-Remediation:** Automatically resets stuck print queues, enables firewall across all profiles, restarts stalled update/time services, forces NTP synchronization, flushes DNS, and purges junk files.

### 4. Reliable Hub 4 IT Support Bundle & Mega Report
- **Complete Diagnostic Dump:** Added storage partition table, network configuration, process hierarchy, active Windows services, event log crash audit, and installed software inventory.
- **Interactive HTML Diagnostic Mega Report:** Formatted with SVLL executive branding, drive capacity bars, and categorized triage tabs.
- **Reliable Output Handling:** Automatic fallback to Desktop or `C:\SVLL_Support` if write access is restricted.

### 5. Hub 5 New PC Provisioning Wizard Hostname Standards
- **Standardized Naming Convention:**
  - Laptops: `LT-SVLL-<LOC>-<SERIAL>`
  - Desktops: `DT-SVLL-<LOC>-<SERIAL>`
  - Warehouse Terminals: `WT-SVLL-<LOC>-<SERIAL>`
- **Hardware Chassis Auto-Detection:** Automatically detects battery/chassis enclosure to select Laptop (`LT-SVLL`) or Desktop (`DT-SVLL`).
- **NetBIOS Name Safety:** Conforms strictly to 15-character Windows NetBIOS limits.

### 6. Emergency Incident Forensics & Live Triage Collector
- **Direct Drive Storage:** Saves forensic evidence directly to physical drives (`C:\SVLL_Forensics`, Desktop, or connected USB drives), avoiding hidden user directories.
- **10 Rich Forensic Artifacts Captured:**
  - `01_system_and_hardware_profile.txt`: OS build, hardware architecture, AnyDesk/Relay ID.
  - `02_active_logon_sessions.txt`: Interactive console and RDP user logon sessions (`quser.exe`).
  - `03_local_administrators_and_users.txt`: Local administrator group membership and user accounts.
  - `04_processes_and_command_lines.csv`: Complete process memory map with working set and rogue Temp execution flags.
  - `05_listening_and_active_sockets.txt`: Active TCP/UDP listening ports and foreign connections mapped to PID.
  - `06_startup_and_autorun_persistence.txt`: Registry autorun keys (HKLM/HKCU Run & RunOnce).
  - `07_security_event_log_triage.txt`: Failed logon events (4625), success logons (4624), and new service installations (7045).
  - `08_usb_storage_device_history.txt`: USBSTOR registry device enumeration and connected serial tags.
  - `09_network_config_and_dns_cache.txt`: Full network adapter configuration and hosts file mappings.
  - `10_installed_patches_and_hotfixes.txt`: Microsoft Windows QFE hotfix installation history.
  - `00_FORENSIC_TRIAGE_REPORT.html`: Interactive dark-themed triage dashboard with risk indicators.
  - `SVLL_Forensics_<HOST>_<TIMESTAMP>.zip`: Full compressed zip package on the destination drive.

### 7. SVLL AI Quick Support Copilot (Expanded Technical Brain)
- **General Technical Knowledge:** Fully handles queries beyond workstation tools—covering Microsoft Excel (XLOOKUP, VLOOKUP, INDEX-MATCH, SUMIFS, Pivot Tables, freeze fixes), Windows PowerShell/CMD scripts, Blue Screen of Death (BSOD) stop codes, barcode/thermal printers (Zebra/TVS/TSC), and Boss ERP logistics billing.
- **Intelligent Query Routing:** Separates operational commands from questions, providing thorough technical explanations without forcing canned tool buttons.
- **Resilient AI Pipeline:** Increased timeout to 12s with fallback to comprehensive local expert diagnostic knowledge.

### 8. In-App Check Updates Tool (User & Admin Panels)
- **Top Status Bar Button:** Prominent `🔄 Updates` button visible across all views in both User and Admin editions.
- **1-Click Automated Upgrade:** Queries GitHub releases, compares semantic versions, downloads edition-matched setup installer (`SVLL-IT-Workstation-Admin-v8.5-Setup.exe` or `SVLL-IT-Workstation-User-v8.5-Setup.exe`), and performs a clean background upgrade.
- **Multiple Access Points:** Accessible from Top Status Bar, Sidebar Attribution Card, Quick Menu (`⚡ Quick ▾`), Command Palette (`Ctrl+K`), and About view.

---

## 📦 Distribution Packages

| Edition | Installer Package | Binary Executable | Target Audience |
| :--- | :--- | :--- | :--- |
| **Admin Edition** | `SVLL-IT-Workstation-Admin-v8.5-Setup.exe` | `SVLL-IT-Workstation-Admin.exe` | IT Engineers, Helpdesk & NOC |
| **User Edition** | `SVLL-IT-Workstation-User-v8.5-Setup.exe` | `SVLL-IT-Workstation-User.exe` | General Staff & Warehouse Depots |
