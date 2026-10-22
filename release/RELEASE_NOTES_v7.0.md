# SVLL IT Support Workstation - Release Notes v7.0

**Release Date:** October 2026  
**Author:** Kunal Turkar  
**Organization:** Shree Vasu Logistics Limited  
**Target Platform:** Windows 10 / 11 (x64) • .NET 8.0 WPF  

---

## 🚀 Summary of v7.0 Major Release

SVLL IT Support Workstation v7.0 introduces the **7-Hub Enterprise Architecture**, expanding from 34 to **40 administrative tools**. This release fulfills major corporate requirements:
1. **Hub 6 (`Hub_Remote`)**: Zero-code AnyDesk remote access endpoint controller and password-protected Central Fleet Device Console.
2. **Hub 7 (`Hub_Complaints`)**: IT Complaint ticket registration with automated hardware telemetry, an interactive AI diagnostic triage assistant with clarifying questions, and a password-protected Central IT Service Desk admin panel.
3. **Master Password Administrative Security (`AdminAuthService`)**: Salted SHA-256 administrative lock ensuring only authorized IT administrators can view fleet endpoints and manage incident complaints, while regular user installations are restricted to endpoint mode.
4. **All original 34 tools and 5 hubs remain 100% intact and functional.**

---

## 🏛️ New Hub Specifications

### 🔴 Hub 6: Remote Access & Fleet Console (`Hub_Remote`)
- **My Remote Client & AnyDesk Status (`RemoteClient`)**:
  - Auto-detects local AnyDesk ID directly from `C:\ProgramData\AnyDesk\system.conf` / `service.conf`.
  - 1-Click "Launch AnyDesk", "Copy ID", "Windows Quick Assist", and "Toggle Windows RDP".
  - **1-Click Remote Support Beacon**: When a user experiences an issue, they click "Broadcast Remote Support Beacon". The request is instantly transmitted to the Central Fleet Console with their issue description, machine name, and AnyDesk ID—zero codes need to be dictated over the phone!
- **Fleet Device Admin Console (`FleetAdminConsole`) - Password Protected**:
  - Displays real-time status of all corporate workstations across branches (Mumbai, Delhi, Raipur, Ahmedabad, Kolkata).
  - Status indicators (`🟢 Online`, `🚨 Support Requested`, `⚪ Offline`).
  - **1-Click Remote Control**: IT Admin clicks `[🚀 Connect AnyDesk]` on any device card to launch an AnyDesk session directly into that machine!
  - 1-Click RDP (`mstsc /v:ip`) and live Ping test.
  - Export complete fleet directory to CSV.

---

### 📋 Hub 7: IT Complaint & AI Service Desk (`Hub_Complaints`)
- **Register IT Complaint (`SubmitComplaint`)**:
  - Multi-category incident logger (Zebra/TSC thermal printers, network/WMS, slow PC, USB DLP, email, OS crash).
  - Auto-attaches complete machine telemetry (Host, User, IP, AnyDesk ID, RAM, OS, Spooler status).
  - Instant ticket generation (`SVLL-TKT-xxxxxx`).
- **AI Diagnostic & Triage Assistant (`AiComplaintBot`)**:
  - Intelligent interactive conversational bot that interviews the user with domain-specific clarifying questions.
  - Proactively asks diagnostic questions (e.g. solid green vs blinking red LED, printer spooler queues, gateway pings).
  - Offers 1-click self-healing fixes in chat (*Restart Spooler*, *Flush DNS*, *1-Click Turbo Boost*).
  - Automatically synthesizes user answers and machine vitals into a finalized ticket.
- **Admin Service Desk Console (`TicketAdminDesk`) - Password Protected**:
  - Centralized dashboard for IT administrators.
  - Filter by status (Open, In Progress, Resolved) and priority (Critical, High, Medium, Low).
  - Update ticket status, enter internal IT notes, and assign technicians.
  - **1-Click AnyDesk Connect to Requester**: Jumps straight onto the user's screen.
  - Export service tickets to CSV.
- **Track My Submitted Tickets (`MyTickets`)**:
  - Allows endpoint users to track their tickets, see technician progress, and view resolution notes.

---

## 🔐 Administrative Master Password

- **Default Master Password:** `SVLL@Admin2026`
- **Security:** Salted SHA-256 hash stored in `%AppData%\SVLL_IT_Workstation\admin_security.json`.
- **Top Status Bar Integration:** Live `👑 ADMIN ACTIVE` / `🔒 Admin Locked` toggle button.
