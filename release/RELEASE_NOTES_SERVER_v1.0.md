# SVLL IT Relay Server — Release Notes v1.0

**Release Date:** October 2026  
**Author:** Kunal Turkar  
**Organization:** Shree Vasu Logistics Limited  
**Target Platform:** Windows Server / Windows 10/11 (x64) • .NET 8.0 Web API  

---

## 🌟 Overview

SVLL IT Relay Server v1.0 is the high-performance, lightweight central backbone for **SVLL IT Support Workstation v8.0**. It runs directly on your office server PC, desktop, or NAS with zero cloud costs.

---

## ⚡ Core Capabilities

1. **Live Workstation Heartbeats & Telemetry**
   - Receives system metrics from all SVLL workstations every 60 seconds (`POST /api/heartbeat`).
   - Tracks Machine Hostname, Serial Number, MAC Address, IP Address, OS Version & Build, Logged-in User, CPU Load %, RAM Usage %, and C: Drive Free Space %.
   - Automatically marks machines as `Offline` after 5 minutes of inactivity.

2. **Fleet Reflection API**
   - Admin Workstations poll `GET /api/fleet` every 30 seconds for instant real-time fleet overview.
   - Zero shared network folder or SMB required.

3. **Complaint Ticket Intake & Cloud Relay**
   - Receives complaint tickets filed from User workstations (`POST /api/ticket`).
   - Delivers tickets to Admin Service Desk in real-time (`GET /api/tickets`).

4. **Instant Admin Resolution & Reply Notification**
   - Allows administrators to resolve tickets and reply (`PATCH /api/ticket/{id}`).
   - User workstations poll `GET /api/ticket/{id}/reply` and display a Windows toast notification upon reply.

5. **Persistence**
   - Embedded SQLite database (`svll_relay.db`). Zero external database configuration required.
   - Fully portable and easily backed up by copying `svll_relay.db`.

6. **Enterprise Security**
   - Secured with shared secret header: `X-SVLL-Key: SVLL-RELAY-KEY-2026-KUNAL`.
   - `/api/health` endpoint is open for ping & liveness probes.

---

## 📦 Package Contents (`SVLL-Relay-Server-v1.0-win-x64.zip`)

- `SvllRelayServer.exe` — Single-file .NET 8 Web API executable
- `install_service.bat` — 1-click script to install & start as an auto-starting Windows Service with firewall rule
- `start_server.bat` — 1-click script to run in a console window for testing
- `uninstall_service.bat` — 1-click script to cleanly remove the Windows Service
- `DEPLOY.md` — Full deployment & administration guide
