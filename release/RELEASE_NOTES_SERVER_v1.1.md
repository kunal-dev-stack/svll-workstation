# SVLL IT Relay Server — Release Notes v1.1

**Release Date:** October 2026  
**Author:** Kunal Turkar  
**Organization:** Shree Vasu Logistics Limited  
**Target Platform:** Windows Server / Windows 10/11 (x64) • .NET 8.0 Web API  

---

## 🌟 Overview

SVLL IT Relay Server v1.1 is the centralized fleet telemetry server that powers **SVLL IT Support Workstation v8.1**. It manages real-time endpoint reflection, remote connection coordination, and health metrics with zero external cloud dependencies.

---

## ⚡ Key Updates in v1.1

1. **Cleaned Telemetry & Service Model**
   - Streamlined API surfaces to focus entirely on real-time workstation heartbeats (`POST /api/heartbeat`), fleet reflection (`GET /api/fleet`), and remote assistance coordination.
   - Deprecated ticketing residue to match Workstation v8.1's streamlined enterprise architecture.

2. **Dashboard UI Refinement**
   - Updated web management dashboard header to Version 1.1 (`DashboardHtml.cs`).
   - Enhanced real-time responsiveness and status indicators for fleet workstations.

3. **Performance & Windows Service Stability**
   - Enhanced SQLite persistence engine (`svll_relay.db`) with optimized connection lifetimes.
   - Automatic offline detection: marks workstations as offline after 5 minutes of missed heartbeats.
   - Zero-configuration self-hosted deployment on port 5000 with Windows Firewall rules.

---

## 📦 Package Contents (`SVLL-Relay-Server-v1.1-win-x64.zip`)

- `SvllRelayServer.exe` — Single-file .NET 8 Web API executable
- `install_service.bat` — 1-click script to install & start as an auto-starting Windows Service with firewall rule
- `start_server.bat` — 1-click script to run in a console window for testing
- `uninstall_service.bat` — 1-click script to cleanly remove the Windows Service
- `DEPLOY.md` — Full deployment & administration guide
