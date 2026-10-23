# SVLL IT Support Workstation - Release Notes v8.0

**Release Date:** October 2026  
**Author:** Kunal Turkar  
**Organization:** Shree Vasu Logistics Limited  
**Target Platform:** Windows 10 / 11 (x64) • .NET 8.0 WPF  

---

## 🚀 What's New in v8.0 — Enterprise Fleet Cloud & Dual-Edition Architecture

SVLL IT Support Workstation v8.0 is a milestone architectural release introducing **Dual Editions (Admin vs User)**, **Live System Info Telemetry Reflection**, **Zero-Password Remote Control (MeshCentral)**, **Cloud-Synced Complaint Resolution & Instant User Reply**, and an **Upgraded Step-by-Step AI Troubleshooting Copilot**:

---

### 1. 👑 Admin Edition vs 👤 User Edition (Split Binary Architecture)
- **Compile-Time Build Separation (`BUILD_ADMIN` vs `BUILD_USER`)**:
  - **Admin Edition (`SVLL-IT-Workstation-Admin.exe`)**: For Kunal Turkar & IT Administrators only. Automatically enters Admin Mode without password prompts. Features the **Live Fleet Management Console**, **Admin Service Desk & Reply Desk**, **Zero-Password Remote Control Launcher**, and full administrative configuration.
  - **User Edition (`SVLL-IT-Workstation-User.exe`)**: Installed across all SVLL branch workstations. Strictly locks out admin tools at the binary compilation level. Users have access to all 40+ diagnostics, optimizations, local AnyDesk client, and the Complaint Intake / AI Assistant with the new **"My Submitted Complaints"** tracking tab.
- **Dedicated Setup Packages**:
  - `SVLL-IT-Workstation-Admin-v8.0-Setup.exe` (2.99 MB)
  - `SVLL-IT-Workstation-User-v8.0-Setup.exe` (2.99 MB)

---

### 2. 📡 Live System Telemetry Reflection (User → Cloud Relay → Admin)
- **Automatic 60s Heartbeat**: User workstations continuously push rich system telemetry to the office Relay Server (`backend/SvllRelayServer`):
  - Machine Hostname & Logged-in User
  - Hardware Serial Number (WMI `Win32_BIOS.SerialNumber` with Motherboard UUID fallback)
  - Network MAC Address & IPv4 Address
  - Operating System Version & Exact Windows Build Number (`CurrentBuild.UBR`)
  - Live CPU Load %, RAM Usage %, and C: Drive Free Storage %
  - AnyDesk Address & Status
  - MeshAgent Service & Node Status
  - Real-time Status (`Online`, `SupportRequested`, or `Offline` after inactivity)
- **Admin Fleet Console**: Automatically refreshes live devices without requiring LAN file-sharing or shared network folders.

---

### 3. 🌐 Zero-Password Remote Control (MeshCentral Integration)
- **ScreenConnect / PCWiser-Style Access**: Admin can take full interactive remote desktop control over the internet without remote user confirmation or password prompts.
- **MeshAgent Integration**: Silent background agent running as a Windows Service, reverse-tunneling to your self-hosted MeshCentral server.
- **1-Click Launch**: Admin Fleet Console features a direct **"🌐 Zero-Password Remote"** button on every machine card that launches the remote session directly in browser/viewer.
- **AnyDesk & RDP Fallbacks**: Instant 1-click fallback buttons for AnyDesk and RDP remain available on every endpoint card.

---

### 4. 📬 Cloud Complaint Sync, Admin Resolution & User Reply
- **Live Ticket Relay**: When a user registers a complaint (manually or via AI triage), it is instantly posted to the Cloud Relay server and stored in SQLite, in addition to the local CSV log sheet.
- **Admin Resolve & Reply**: Admin Service Desk includes a **"💾 Update & Reply"** button. The administrator's diagnosis and reply message are pushed to the cloud relay.
- **User Toast Notification**: User workstations poll for replies in the background; when the admin replies, a Windows notification pops up on the user's screen displaying the IT reply and resolution.
- **Track My Tickets Tab**: User Edition features a dedicated **"📂 Track My Tickets"** tab with highlighted green badges displaying official admin replies and resolution timestamps.

---

### 5. 🤖 Step-by-Step AI Diagnostic Copilot & Tool Suggestions
- **Numbered Fix Steps**: Both Cloud AI and offline knowledge engines structure answers with clear, practical steps (`Step 1`, `Step 2`, `Step 3`, etc.) explaining exact root causes and manual fixes.
- **Direct Built-in Tool Recommendations**: At the conclusion of every diagnostic explanation, the AI explicitly recommends the corresponding SVLL Workstation built-in tool (e.g., 1-Click Turbo Boost, Print Spooler Reset, DNS Flush, S.M.A.R.T. Drive Doctor, USB Depot, Force Uninstaller) and provides a 1-click execution button.

---

## 🏗️ Backend Relay Server (`backend/SvllRelayServer`)
- Lightweight ASP.NET Core 8 Minimal API with SQLite database persistence (`svll_relay.db`).
- Secured via shared corporate key (`X-SVLL-Key: SVLL-RELAY-KEY-2026-KUNAL`).
- Zero internet cloud costs: Runs directly as a Windows Service or Docker container on your office NAS / Server PC.

---

## 📦 Distribution Packages (v8.0)

| Package | Edition | Size | Intended Machine |
|---|---|---|---|
| `SVLL-IT-Workstation-Admin-v8.0-Setup.exe` | Admin Edition | 2.99 MB | Admin PC (Kunal Turkar) |
| `SVLL-IT-Workstation-User-v8.0-Setup.exe` | User Edition | 2.99 MB | All Branch Workstations |
| `SVLL-IT-Workstation-Admin.exe` | Admin Standalone | 1.80 MB | Portable Admin Binary |
| `SVLL-IT-Workstation-User.exe` | User Standalone | 1.79 MB | Portable User Binary |
| `backend/SvllRelayServer` | Relay Server | ~2 MB | Office NAS / Server PC |
