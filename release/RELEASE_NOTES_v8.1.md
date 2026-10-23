# SVLL IT Support Workstation - Release Notes v8.1

**Release Date:** October 2026  
**Author:** Kunal Turkar  
**Organization:** Shree Vasu Logistics Limited  
**Target Platform:** Windows 10 / 11 (x64) • .NET 8.0 WPF  

---

## 🚀 What's New in v8.1 — Advanced Task Manager, USB Depot Engine, Vibrant Dashboard & UI Refinements

SVLL IT Support Workstation v8.1 delivers critical quality-of-life enhancements, bug fixes, modern dashboard aesthetics, a brand-new detailed Task Manager, and an overhauled USB Offline Software & Driver Depot.

---

### 1. ⚡ New Feature: Detailed Interactive Task Manager
- **Dedicated Task Manager View (`MainWindow.TaskManager.cs`)**:
  - Live process enumeration with real-time process ID (PID), memory consumption (MB), CPU usage, thread count, handle count, process description, and executable paths.
  - Quick filter tabs: **ALL**, **USER APPS**, **SYSTEM RUNTIMES**, and **BACKGROUND SERVICES**.
  - Instant real-time search box to quickly filter processes by name, PID, or description.
  - KPI summary metric cards: **Total Running Processes**, **Physical RAM In-Use**, **Peak Memory Consumer**, and **Task Manager Engine Status**.
- **Process Termination & Close Task Actions**:
  - **❌ Close Task (Graceful)**: Attempts clean shutdown (`CloseMainWindow()`) before terminating.
  - **⚠️ End Process Tree (Forceful)**: Instantly terminates the target process and all spawned child processes (`taskkill.exe /PID ... /T /F`).
  - Right-click context menu and keyboard shortcut (`Delete`) for instant task termination.
  - Confirmation prompt prevents accidental closure of critical Windows system processes.
- **Fixed Process Selection & Expanded Top 6 Dashboard List**:
  - Expanded the Dashboard's top process tracker from 5 to **Top 6 Active Resource-Intensive Processes**.
  - **Fixed Auto-Deselect Bug**: In previous releases, background telemetry refresh cycles cleared the user's selected process every 1.5–2 seconds. In v8.1, selection state is cached and seamlessly restored on every polling tick.
  - Added direct **"❌ Close Task"** button and right-click context menu directly on the dashboard's top process card.

---

### 2. 🔌 USB Depot Installer Engine Fixes & Modernized UI
- **Fixed Installer Failures on Unknown Executables**:
  - **Working Directory Fix**: Resolved a critical issue where installers failed because `WorkingDirectory` was not set. Setup files looking for companion archives (`data1.cab`, `.bin`, `.msi`, configuration files) now execute within their source folder.
  - **Multi-Heuristic Installer Detection**:
    - **Inno Setup**: `/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP-`
    - **NSIS Setup** (`/S`): Correctly detected for VLC, Notepad++, 7-Zip, WinRAR, FileZilla, PuTTY, Wireshark, Audacity, and Git.
    - **WiX / Burn Bundles** (`/install /quiet /norestart`): Correctly detected for Visual C++ Redistributables (`vc_redist*`), .NET Runtime (`dotnet*`), Python, and Node.js.
    - **Windows Installer (MSI)**: `msiexec.exe /i "..." /qn /norestart ALLUSERS=1`
    - **Web Browsers**: Chrome, Edge, Firefox, Brave silent deployment.
    - **Remote Utilities**: AnyDesk silent install, TeamViewer `/S`, Zoom `/silent`.
    - **Automation Scripts**: `.bat`, `.cmd`, `.ps1` with bypass policies.
    - **Binary Signature Inspection**: Scans binary headers and footers for `NullsoftInst`, `Inno Setup`, and `WixBundle` markers.
- **Interactive GUI Fallback & Single-Package Actions**:
  - Added **"🖥️ Run Interactive GUI (Show Setup Wizards)"** mode option so operators can install software with visible windows whenever silent mode is not preferred or supported.
  - **Row Actions**: Double-click or right-click any scanned package to deploy it individually (Interactive or Silent).
  - **Preset Switcher**: Easily switch an item's flags between Inno, NSIS, WiX/Burn, MSI, or GUI with 1 click.
  - **15-Minute Timeout Protection**: Silent background processes that become unresponsive are automatically aborted after 15 minutes to prevent blocking deployment queues.
- **Enhanced Depot UI**:
  - 4 Real-time KPI summary metric cards: **Packages Found**, **Total Depot Size**, **Selected Packages**, and **Target Storage**.
  - **"📂 Open in Explorer"** button to jump straight to the USB depot directory.
  - Quick Category Selection presets: **"📦 Software"** and **"🔧 Drivers"**.

---

### 3. 🎨 Vibrant, Informative & Responsive Dashboard
- **Sleek Minimal Startup State**:
  - Integrated terminal row starts in a **minimized state** (`32px` bar with **"▲ Expand Terminal"** toggle) for a clean, distraction-free view on launch.
  - AI Support Copilot starts in a **minimized state** on the dashboard with a 1-click **"▼ Expand Support Copilot"** toggle.
- **Colorful & Responsive Card Aesthetics**:
  - High-contrast card header accents, gradient banners, and clear visual hierarchy.
  - Storage drive meters equipped with hardware badges (`🔌 USB`, `💾 FIXED`), disk label, free gigabytes, and color-coded progress bars.
  - Modernized Fleet Diagnostics and Network Topology cards.

---

### 4. 🧹 Removed CSV Sheet & Ticketing Modules Everywhere
- **Complete Module Clean-up**:
  - Removed top navigation CSV Sheet button and quick-action menu items.
  - Removed ticketing views, complaint submission forms, and ticketing poll loops from the codebase.
  - Cleaned up project files and references (`MainWindow.Complaints.cs`, `AiComplaintEngine.cs`, `ComplaintTicketService.cs` excluded).
  - The software is now strictly focused on high-performance IT triage, diagnostics, endpoint security, and remote management.

---

### 5. 🏷️ Clean Hub Re-numbering Line-by-Line (Fixed "HUB 8" Bug)
Hub numbering has been re-sequenced and verified across both User and Admin editions:
- **HUB 1 • LIVE MONITORING & TRIAGE** (Dashboard & Resource Vitals)
- **HUB 2 • NETWORK OPERATIONS (NOC)** (Connectivity, Subnet Scanner, DNS, Wi-Fi)
- **HUB 3 • SYSTEM OPTIMIZATION** (Cleaners, TRIM, Updates, WinUtil)
- **HUB 4 • FLEET DIAGNOSTICS & SUPPORT** (S.M.A.R.T., Task Manager, Remote Client, Asset Passport)
- **HUB 5 • PLAYBOOK & DEPLOYMENT** (Provisioning, dual-syntax scripts, USB Depot, WinGet)
- **HUB 6 • ENDPOINT SECURITY & HARDENING** *(Previously mislabeled as HUB 8; renumbered and unified)*
- **HUB 7 • ADMIN COMMAND CENTER** *(Admin Edition Only)*

---

## 📦 Distribution Packages (v8.1)

| Package | Edition | Size | Target Environment |
|---|---|---|---|
| `SVLL-IT-Workstation-Admin-v8.1-Setup.exe` | Admin Edition | ~3.1 MB | Admin PC (Kunal Turkar) |
| `SVLL-IT-Workstation-User-v8.1-Setup.exe` | User Edition | ~3.1 MB | Client & Branch Workstations |
| `SVLL-IT-Workstation-v8.1-Setup.exe` | Generic Alias | ~3.1 MB | Backward-compatible updater alias |
| `SVLL-Relay-Server-v1.1-win-x64.zip` | Server Package | ~1.2 MB | Office Server PC / NAS |
| `SVLL-IT-Workstation-Admin.exe` | Standalone Executable | ~1.8 MB | Portable Admin Utility |
| `SVLL-IT-Workstation-User.exe` | Standalone Executable | ~1.8 MB | Portable Client Utility |
