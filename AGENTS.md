# Shree Vasu Logistics - SVLL IT Workstation & AegisMesh Suite (v8.5)

> **Antigravity Context & Instructions**: This file is automatically loaded by Google Antigravity across all devices when opening this workspace.

---

## 1. Project Overview & Architecture

**SVLL IT Workstation** is a unified corporate IT management, fleet telemetry, remote diagnostics, and zero-touch remote control platform developed for **Shree Vasu Logistics Limited**.

### Subsystems:
1. **WPF Desktop Application (`src/`)**:
   - .NET 8.0 WPF Windows application built with dual preprocessor directives:
     - `BUILD_USER`: Standard employee workstation app, automatic background agent enrollment, self-service IT complaints.
     - `BUILD_ADMIN`: Complete IT NOC Command Center (Hub 7), live fleet scorecards, vitals telemetry (CPU/RAM/Disk), remote desktop control, network diagnostics, and playbook.
2. **Server Gateway (`backend/mesh-gateway/`)**:
   - Node.js + Express + WebSocket backend running on unified port **5000**.
   - Handles real-time agent WebSockets (`/ws`), fleet heartbeat telemetry (`/api/fleet`, `/api/heartbeat`), ticket management (`/api/tickets`), and binary downloads (`/api/agent/*`).
3. **Web Management Console (`admin-web/`)**:
   - React + Vite + Lucide icons dashboard served directly from the server gateway.
   - Remote terminal, desktop view, file manager, process explorer, and token enrollment.
4. **Workstation Agent Daemon (`agent-windows/`)**:
   - C# .NET 8.0 background agent deployed as scheduled task `AegisMeshAgent` running under `SYSTEM` account.
   - Connects back to the server gateway via WebSocket, providing continuous vitals and remote execution.
5. **Installers & Setup Tooling (`installer/`, `scripts/`)**:
   - Inno Setup 7 scripts generating standalone silent installers:
     - `SVLL-IT-Workstation-User-v8.5-Setup.exe` (~20.2 MB)
     - `SVLL-IT-Workstation-Admin-v8.5-Setup.exe` (~20.2 MB)

---

## 2. Infrastructure & Endpoints

| Resource | Value / Endpoint |
| :--- | :--- |
| **Windows Server 2022 Host** | `SVLL-DC01` |
| **Local LAN IP** | `http://192.168.10.11:5000` *(Alt: `http://192.168.100.25:5000`)* |
| **Cloudflare 24/7 Internet Tunnel** | `https://roberts-basis-early-likewise.trycloudflare.com` |
| **GitHub Repository** | `https://github.com/kunal-dev-stack/svll-workstation.git` (`main`) |
| **GitHub Releases** | `https://github.com/kunal-dev-stack/svll-workstation/releases/tag/v8.5` |

---

## 3. Shortened 1-Click Deployment Links

- **User Workstation (Tool + Agent)**:
  - PowerShell 1-liner: `irm https://tinyurl.com/svll-user | iex`
  - Direct EXE download: `https://tinyurl.com/2aktq8dh`
- **Admin Command Center (NOC / IT)**:
  - PowerShell 1-liner: `irm https://tinyurl.com/svll-admin | iex`
  - Direct EXE download: `https://tinyurl.com/25edwv9j`

---

## 4. Key Rules & Coding Conventions

1. **UTF-8 Without BOM**: All PowerShell scripts (`install.ps1`, `install-admin.ps1`, etc.) MUST be saved in UTF-8 without BOM (`New-Object System.Text.UTF8Encoding($false)`). Windows PowerShell 5.1 fails with syntax errors (`?#`) if a BOM exists.
2. **Zero Third-Party Remote Dependencies**: All legacy RustDesk, UltraRemote, and direct port 21118 references have been purged in v8.5. Remote control uses the native AegisMesh 60FPS engine and Web Console over Cloudflare/WebSockets.
3. **Dual Endpoint Fallback**: Installers must always attempt download from GitHub Releases first; if GitHub is unreachable, fall back to the live Server Gateway (`$ServerUrl/api/agent/*`).
4. **Defensive WebSocket Parsing**: `AgentCore.cs` must always normalize the server URI to strip any trailing `/ws` before formatting `ws://.../ws`.

---

## 5. Development & Build Commands

- **Build Admin Edition**:
  ```powershell
  dotnet build src/SVLL-IT-Workstation.csproj -c Release-Admin
  ```
- **Build User Edition**:
  ```powershell
  dotnet build src/SVLL-IT-Workstation.csproj -c Release-User
  ```
- **Build Installers & Dual Packages**:
  ```powershell
  powershell -ExecutionPolicy Bypass -File .\scripts\make_project.ps1
  ```
- **Run / Test Server Gateway**:
  ```bash
  cd backend/mesh-gateway
  npm install
  npm run build
  npm start
  ```
- **Build Web Console**:
  ```bash
  cd admin-web
  npm install
  npm run build
  ```
