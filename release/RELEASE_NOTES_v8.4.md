# SVLL IT Support Workstation & AegisMesh Fleet Gateway — v8.4

## 🚀 What's New in Version 8.4

### 1. Pure AegisMesh Remote Desktop & Fleet Management (Zero-Touch)
- **RustDesk & UltraRemote completely removed**: Eliminated all third-party dependencies, licenses, TOML configs, and port 21118 requirements.
- **Pure AegisMesh Background Daemon**: Zero-lag 60 FPS remote desktop, remote PowerShell terminal, file manager, process explorer, and diagnostics.
- **Zero-Evasion Standard OS Architecture**: AegisMeshAgent runs as an elevated SYSTEM Scheduled Task with 24/7 self-healing and zero service start timeouts (Error 1053 completely resolved).

### 2. Dual Deployment: LAN Direct + 24/7 Global Internet Access
- Automatically detects if the client is on the corporate office LAN (`http://192.168.10.11:5000`).
- If outside the office (remote branch, home, or on the road), automatically routes over the secure Cloudflare Tunnel (`https://roberts-basis-early-likewise.trycloudflare.com`).
- Zero manual VPN or port-forwarding configuration required.

### 3. Fixed In-App Upgrades & Strict Edition Separation
- Fixed update engine: User workstations strictly download and install the **User Edition** (`SVLL-IT-Workstation-User-v8.4-Setup.exe`).
- Admin consoles strictly download and install the **Admin Edition** (`SVLL-IT-Workstation-Admin-v8.4-Setup.exe`).
- All binaries branded and verified as **v8.4**.

### 4. Enterprise Relay Server & Fleet Reflection
- Built-in REST endpoints (`/api/heartbeat`, `/api/fleet`, `/api/ticket`, `/api/tickets`) directly inside the Express/TypeScript gateway.
- Immediate reflection of connected devices in both the React Admin Web Console and the WPF Admin Workstation.

---

## 📦 Quick Install Commands

### 👤 User Workstations (Any PC on LAN or Internet)
Open PowerShell (Administrator) and run:
```powershell
irm https://raw.githubusercontent.com/kunal-dev-stack/svll-workstation/main/install.ps1 | iex
```

### 👑 Admin Workstations (Office PC / IT Admin Desk)
Open PowerShell (Administrator) and run:
```powershell
irm https://raw.githubusercontent.com/kunal-dev-stack/svll-workstation/main/install-admin.ps1 | iex
```

### 🌐 Direct Web Console Access
Navigate to the web portal on your browser:
- **Global Internet Access**: `https://roberts-basis-early-likewise.trycloudflare.com`
- **Internal Office LAN**: `http://192.168.10.11:5000`
- **Default Credentials**: `admin` / `AdminPassword123!`
