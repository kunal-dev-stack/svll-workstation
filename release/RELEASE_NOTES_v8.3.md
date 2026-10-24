# SVLL IT Support Workstation - Release Notes v8.3

**Release Date:** October 2026  
**Author:** Kunal Turkar  
**Organization:** Shree Vasu Logistics Limited  
**Target Platform:** Windows 10 / 11 (x64) • .NET 8.0 WPF  

---

## 🚀 What's New in v8.3 — Hub 7 Rebuilt from Ground Up, "Take Remote" Control Deck, Inbuilt SVLL Mesh Agent & Real-Time Socket Listener

SVLL IT Support Workstation v8.3 completely overhauls Hub 7 (**Admin Command Center**) into an Executive NOC management deck with an intuitive **"Take Remote"** card on top, a real-time **Connected Fleet Workstations** grid on the bottom, and introduces an embedded, native **Inbuilt SVLL Mesh Agent & Listener** (port 5055) providing instant 2-way messaging, hardware vitals streaming, and direct remote assistance prep.

---

### ⭐ Highlights of v8.3

1. **⚡ Rebuilt Hub 7 Admin Command Center:**
   - **Executive NOC Control Deck:** Removed legacy clutter and redesigned Hub 7 into a streamlined, high-contrast, professional operations center.
   - **"Take Remote" Primary Control Card:**
     - Target workstation input supporting LAN IP address, Hostname, or Ultra Remote Partner ID.
     - Multi-protocol connection selector: Direct LAN IP (Port 21118 - 60 FPS ultra-low latency), Ultra Remote Partner ID (Cross-WAN / NAT Traversal), Inbuilt Mesh Session (Port 5055), Windows RDP (MSTSC), and AnyDesk.
     - Unattended password manager with auto-reflection and show/hide toggle.
     - Large, vibrant `⚡ TAKE REMOTE CONTROL NOW` action button for immediate 1-click connect.
     - Quick administrative utilities: Ping Host, Message User (live Windows toast), Remote PowerShell Shell, and Remote File Explorer (`\\<target>\C$`).
   - **Interactive Target Pre-Flight Telemetry Deck:** Real-time right-hand card previewing the selected workstation's Hostname, User, IP, Branch, OS Build, BIOS Serial, and live CPU/RAM/Disk progress bars.

2. **🖥️ Connected Fleet Workstations Grid:**
   - Real-time device list with live search by Hostname, IP, User, Branch, or Partner ID.
   - Filter pills for `All Devices`, `🟢 Online Only`, `🚨 Support Beacon Alerts`, and `🛰️ Mesh Active`.
   - Branch filter dropdown covering all logistics hubs (Mumbai HQ, Delhi, Raipur, Ahmedabad, Kolkata, etc.).
   - Live hardware vitals mini-bars on every workstation card (CPU %, RAM %, and Disk C: free %).
   - 1-click `⚡ Take Remote` button on every device row that immediately reflects credentials and launches the remote session.
   - Device row utilities: `[🎯 Select]`, `[💬 Message]`, `[📶 Ping]`, and `[🗑️ Remove]`.

3. **🛰️ Inbuilt SVLL Mesh Agent & Embedded Admin Listener (Port 5055):**
   - **Admin Embedded Listener (`SvllMeshListener.cs`):** Runs an asynchronous TCP server on Port 5055 directly inside the Admin Workstation. Tracks connected sockets, receives client telemetry handshakes, pushes administrative commands, and includes a **Live Sockets Monitor Dialog**.
   - **User Edition Background Agent (`SvllMeshAgentService.cs`):** Lightweight background agent in the User Edition that automatically resolves the Admin/Relay host, connects to port 5055, and streams hardware vitals (CPU %, RAM %, Disk %) every 20 seconds.
   - **Instant Admin-to-User Notification Toasts:** Administrators can type and push instant desktop notification toasts directly to any user PC through the Mesh socket.
   - **Remote Session Handshake (`remote_prep`):** When an admin prepares to connect, the mesh agent verifies that the direct port 21118 listener and RustDesk services are active and firewall rules are unblocked.

4. **🛡️ Automatic Firewall & Silent In-Place Provisioning:**
   - Automated installer scripts (`install.ps1`, `install-client.ps1`, `install-admin.ps1`) automatically configure Windows Firewall rules for TCP port `5055` (Inbuilt Mesh), TCP port `21118` (Direct LAN), and UDP ports `21118-21119`.
   - Automatic silent auto-start registered via Windows `Run` key for uninterrupted background fleet monitoring.

---

### 📦 Release Assets

- **User / Client Edition Setup:** `SVLL-IT-Workstation-User-v8.3-Setup.exe`
- **Admin Edition Setup:** `SVLL-IT-Workstation-Admin-v8.3-Setup.exe`
- **Legacy Updater Alias:** `SVLL-IT-Workstation-v8.3-Setup.exe`
- **Standalone Admin Executable:** `SVLL-IT-Workstation-Admin.exe`
- **Standalone User Executable:** `SVLL-IT-Workstation-User.exe`
- **SHA256 Checksums:** `CHECKSUMS.txt`
