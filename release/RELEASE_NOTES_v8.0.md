# SVLL IT Support Workstation - Release Notes v8.0

**Release Date:** October 2026  
**Author:** Kunal Turkar  
**Organization:** Shree Vasu Logistics Limited  
**Target Platform:** Windows 10 / 11 (x64) • .NET 8.0 WPF  

---

## 🚀 What's New in v8.0 — Enterprise Fleet Cloud & Redesigned Hub Architecture

SVLL IT Support Workstation v8.0 is a milestone architectural release that introduces **Dual Compiled Editions (Admin vs User)**, a **Streamlined 6-Hub Interface with Dedicated Admin Command Center**, **Automated AnyDesk & Zero-Password Remote Access**, and **Live Telemetry Reflection to your Central Office Server**:

---

### 1. 👑 Admin Edition vs 👤 User Edition (Split Binary Architecture)
- **Compile-Time Build Separation (`BUILD_ADMIN` vs `BUILD_USER`)**:
  - **Admin Edition (`SVLL-IT-Workstation-Admin.exe`)**: For Kunal Turkar & IT Administrators. Automatically enters Admin Mode without password prompts. Opens directly to the **👑 Admin Command Center**, featuring live fleet telemetry cards, 1-click remote connections, and centralized service desk management.
  - **User Edition (`SVLL-IT-Workstation-User.exe`)**: Installed across all SVLL branch workstations. Strictly locks out admin tools at the binary compilation level. Users have access to diagnostics, optimizations, local AnyDesk client, and the Complaint Intake / AI Assistant with the **"My Open Tickets"** tracking tab.
- **Dedicated Setup Packages**:
  - `SVLL-IT-Workstation-Admin-v8.0-Setup.exe` (3.0 MB)
  - `SVLL-IT-Workstation-User-v8.0-Setup.exe` (2.99 MB)

---

### 2. 🎨 Redesigned & Streamlined Hub Structure
Old hubs 6, 7, and 8 have been completely reorganized and modernized for maximum clarity and zero visual clutter:
- **Hub 1: Network & Connectivity Diagnostics** (IPConfig, DNS, Subnet Scanner, Bandwidth Hunter, Wi-Fi, LAN Shares)
- **Hub 2: System Optimization & Servicing** (Turbo Boost, WinUtil Tweaks, Temp Purge, Storage TRIM, Updates)
- **Hub 3: Fleet Diagnostics & Helpdesk** (OS Repair, S.M.A.R.T. Drive Health, Print Spooler, Event Analyzer, Asset Passport)
- **Hub 4: Command Playbook & Deployment** (Provisioning Wizard, 37 Dual-Syntax Fixes, USB Depot, WinGet, App Uninstaller)
- **Hub 5: Endpoint Security & Hardening** (Process & Ransomware Shield, CIS Benchmark, USB DLP, Port Threat Hunter, Forensics)
- **Hub 6: IT Service Desk & Helpdesk** (Raise Support Ticket, AI Diagnostic Assistant, Track My Tickets, My Local Remote Client)
- **Hub 7 (Admin Only): 👑 Admin Command Center** (Live Fleet Monitoring, Real-time Vitals, 1-Click Remote Access, Quick Connect Bar)

---

### 3. 🖥️ 1-Click Remote Access & Auto-Registration
- **Automatic Silent AnyDesk Deployment**: When User Edition is installed, it verifies AnyDesk is present. If missing, it silently downloads and installs AnyDesk with auto-start enabled.
- **CLI AnyDesk ID Auto-Discovery**: Automatically extracts the endpoint's real AnyDesk ID using `--get-id` and configuration parsing, transmitting it immediately to the server.
- **Zero-Password Remote (MeshCentral Integration)**: Silent background agent reverse-tunneling to your self-hosted MeshCentral server for password-free remote desktop control.
- **1-Click Control Panel in Admin Command Center**:
  - 🚀 **AnyDesk**: Instantly connects directly to the machine's AnyDesk ID.
  - 🌐 **Zero-Pass Remote**: Launches web-based interactive desktop session without user confirmation.
  - 💻 **RDP**: Initiates Microsoft Remote Desktop to endpoint IP (RDP auto-enabled in Windows Firewall).
  - 📁 **C$ Files**: Opens Windows Explorer directly to the remote machine's `\\<IP>\c$` admin share.
  - 📡 **PS Shell**: Initiates an interactive remote PowerShell terminal (`Enter-PSSession`).
  - ⚙️ **Manage**: Launches `compmgmt.msc` targeted directly at the remote workstation.
  - ⚡ **Ping**: Real-time roundtrip latency check.

---

### 4. 🏢 Office Server Deployment & Personal PC Management Setup

To manage your office fleet from your personal PC:

#### A. Office Server PC Setup (e.g. `192.168.10.222`)
On your office PC or dedicated server machine, open PowerShell as Administrator and run:
```powershell
irm https://tinyurl.com/svll-server | iex
```
*(Or from GitHub raw: `irm https://raw.githubusercontent.com/kunal-dev-stack/svll-workstation/main/install-server.ps1 | iex`)*
- Automatically registers the `SvllRelay` Windows Service (.NET 8 with Windows Service Control Manager integration).
- Opens TCP Port 5000 in Windows Firewall.
- Starts the SQLite database (`svll_relay.db`) and web dashboard at `http://localhost:5000/`.

#### B. Personal / Admin PC Setup (Kunal's PC)
On your laptop or admin desktop, open PowerShell as Administrator and run:
```powershell
irm https://tinyurl.com/svll-admin | iex
```
*(Or from GitHub raw: `irm https://raw.githubusercontent.com/kunal-dev-stack/svll-workstation/main/install-admin.ps1 | iex`)*
- Installs the **Admin Edition** and launches into the **Admin Command Center**.
- Click **⚙️ Server Config** in the top bar to set your Office Server IP (e.g. `http://192.168.10.222:5000`).
- The banner instantly turns green (**🟢 Relay Server Connected**), and live fleet heartbeat cards load automatically.

#### C. User PC Deployment (All Employees / Branches)
On any user workstation, open PowerShell as Administrator and run:
```powershell
irm https://tinyurl.com/svll-tool | iex
```
*(Or from GitHub raw: `irm https://raw.githubusercontent.com/kunal-dev-stack/svll-workstation/main/install.ps1 | iex`)*
- Installs the **User Edition** silently.
- Auto-detects/installs AnyDesk, configures RDP, and fires an instant heartbeat with machine specs to the Office Server.
- The machine appears immediately on your Admin Command Center dashboard!

---

## 📦 Distribution Packages (v8.0)

| Package | Edition | Size | Intended Machine |
|---|---|---|---|
| `SVLL-IT-Workstation-Admin-v8.0-Setup.exe` | Admin Edition | 3.00 MB | Admin PC (Kunal Turkar) |
| `SVLL-IT-Workstation-User-v8.0-Setup.exe` | User Edition | 2.99 MB | All Branch Workstations |
| `SVLL-IT-Workstation-v8.0-Setup.exe` | General Setup | 2.99 MB | Backward-compatible updater alias |
| `SVLL-Relay-Server-v1.0-win-x64.zip` | Server Package | 13.8 MB | Office Server / NAS PC |
| `SVLL-IT-Workstation-Admin.exe` | Admin Standalone | 1.83 MB | Portable Admin Binary |
| `SVLL-IT-Workstation-User.exe` | User Standalone | 1.80 MB | Portable User Binary |
