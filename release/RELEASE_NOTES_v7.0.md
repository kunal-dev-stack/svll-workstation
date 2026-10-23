# SVLL IT Support Workstation - Release Notes v7.0 (Enhanced Edition)

**Release Date:** October 2026  
**Author:** Kunal Turkar  
**Organization:** Shree Vasu Logistics Limited  
**Target Platform:** Windows 10 / 11 (x64) • .NET 8.0 WPF  

---

## 🚀 Summary of v7.0 Major Release & Security Enhancements

SVLL IT Support Workstation v7.0 introduces the **8-Hub Enterprise Architecture**, expanding to **44 production-grade administrative & security tools**. This release fulfills all key enterprise requirements:

1. **Hub 8 (`Hub_Security`) - Endpoint Threat Defense & Security Hardening**:
   - **Zero-Trust Process & Ransomware Shield (`ProcessShield`)**: Real-time process integrity monitoring, detects untrusted executions from `%TEMP%` / `%APPDATA%`, calculates SHA-256 process hashes, VirusTotal threat intelligence lookup, and emergency process freeze / terminate.
   - **CIS Enterprise Benchmark Hardening (`SecurityHardening`)**: Audits and enforces 10 CIS baseline security controls (UAC elevation prompts, SMBv1 disablement, RDP NLA requirement, Windows Firewall profiles, LLMNR poisoning mitigation, AutoRun disablement, Remote Registry disablement, Windows Script Host disablement).
   - **USB Endpoint DLP Guardian (`UsbDlpGuardian`)**: Removable storage data loss prevention with Full Access, Read-Only write protection, and Mass Storage disablement via `USBSTOR` policy.
   - **Port & Active Socket Threat Hunter (`PortThreatHunter`)**: Audits listening ports and active foreign sockets against vulnerable vectors (445, 3389, 135, reverse shell ranges), with 1-click TCP connection termination and Windows Firewall IP isolation.
   - **Emergency Forensics Collector (`ForensicsCollector`)**: 1-click incident response evidence collector capturing running processes, active TCP/UDP sockets, hosts file, and DNS cache into timestamped JSON triage folders.
   - **SSL/TLS & Domain Health Inspector (`CertHealthInspector`)**: Validates corporate endpoints (`svll.in`, `api.svll.in`), certificate chains, protocol negotiation (TLS 1.2 / 1.3), and expiration countdowns.

2. **Enterprise Complaint Report Tool & Live CSV Synchronization (`Complain Log sheet IT - New Complaint.csv`)**:
   - Direct integration with the project's official 9,000+ record log sheet `Complain Log sheet IT - New Complaint.csv`.
   - Sequential ticket number assignment (`IT/COM/12279`+).
   - Real-time live reflection on the IT Service Desk Admin Panel via asynchronous event subscriptions (`OnTicketsChanged`) without application restart.
   - Clean, standardized real complaint categories: purged internal "IT" prefixes to display clean incident categories (*PC / Laptop Working Slow*, *Internet / Network Connectivity Problem*, *Outlook / Corporate Email Issue*, *Computer Hardware Repair & Maintenance*, *Security Camera / CCTV Down*, *UPS / Power Backup Problem*, *Boss Not Working*, *Printer Not Working*, *Software Installation*, *USB DLP Access*, *Other*).
   - Fixed header layout box overlap to cleanly wrap live sync status and administrative action buttons regardless of window resolution.
   - 1-Click quick incident presets and real-time depot dropdown selection.

3. **Dashboard AI Quick Support Copilot & Workstation Feature Hub**:
   - Interactive, intelligent AI Copilot widget embedded directly on the main Executive Operations Dashboard.
   - Dual AI Intelligence Architecture: Free Cloud AI API integration for answering any technical or general question, combined with an instant offline local diagnostic knowledge base with 37+ enterprise scenarios.
   - 1-Click feature access and direct command execution from chat: 1-Click Turbo Boost, Print Spooler Fix & Job Purge, DNS Flush & Gateway Ping, Temp Files & Recycle Bin Purge, IT Ticket Registration, AnyDesk Remote, USB Depot, App Uninstaller, Process Shield, and S.M.A.R.T. Drive Health.
   - Interactive chat stream with action buttons embedded directly within AI responses and minimize/expand toggle.

4. **Tool Deduplication & Streamlined Hub Architecture**:
   - Logistics Hardware Doctor and legacy Zebra/thermal printer components completely purged.
   - USB DLP centralized into Hub 8 as `UsbDlpGuardian`.
   - Elevated UI aesthetics with SVLL corporate design system: custom accent colors, status pill badges, interactive preset cards, and live system telemetry sidebars.

5. **Hub 6 (`Hub_Remote`) - Zero-Code Remote Access & Central Fleet Console**:
   - Auto-detects local AnyDesk ID and provides 1-click remote support beacon broadcasting.
   - Admin Fleet Console for instant remote assistance without reading codes over the phone.

6. **Hub 7 (`Hub_Complaints`) - AI-Powered Incident Intake & Triage**:
   - Interactive diagnostic bot providing clarifying triage questions and 1-click self-healing tools.

7. **Administrative Master Password (`AdminAuthService`)**:
   - Password-protected administrative mode (default: `SVLL@Admin2026`).

---

## 📦 Distribution Packages

- **Setup Installer:** `SVLL-IT-Workstation-v7.0-Setup.exe` (2.97 MB) - Framework-dependent Inno Setup installer bundling `Complain Log sheet IT - New Complaint.csv` and transparent icon.
- **Standalone Binary:** `SVLL-IT-Workstation.exe` (1.71 MB) - Single-file executable ready for deployment.
