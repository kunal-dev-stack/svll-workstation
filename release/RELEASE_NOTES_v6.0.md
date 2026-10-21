# SVLL IT Support Workstation v6.0 - Enterprise Provisioning & Logistics Doctor Edition

**Release Date:** October 21, 2026  
**Developer:** Kunal Turkar  
**Organization:** Shree Vasu Logistics Limited  
**Target OS:** Windows 10 & Windows 11 (64-bit)

---

### 🎨 Windows 11 Fluent UI Modernization & Overlap Resolution

1. **Top Status Bar Responsive 3-Column Architecture:**
   - **Zero Overlap:** Completely resolved top navbar horizontal collision on all screen resolutions down to 980×600.
   - **Compact Identity & Network Badges (Left Column):** Grouped Hostname & Username pill (`[ 💻 Hostname | 👤 Username ]`) and Live Network pill (`[ 🌐 IP • GW 🟢 ]`).
   - **Integrated Telemetry & Action Dock (Right Column):** Grouped CPU/RAM vitals, 🛡️ 98% Optimal fleet health pill, ⚡ Turbo Boost button, and 🔍 Search (Ctrl+K).
   - **⚡ Quick Launch ▾ Dropdown Menu:** Consolidated all primary workflow triggers into a sleek context dropdown:
     - 🚀 Zero-Touch New PC Setup Wizard
     - 🖨️ Zebra & TSC Hardware Doctor
     - 🔒 Endpoint Security & USB DLP Lock
     - 💾 S.M.A.R.T. Drive Health & Crash Predictor
     - 📡 Bandwidth Hog & Socket Hunter
     - 📋 Open Device Audit Action Log
     - 🧹 Instant Flush DNS & ARP Cache

2. **Enterprise Segmented Hub Navigation & Cards:**
   - Color-coded Hub category badges (`HUB 1 • TRIAGE`, `HUB 2 • NOC`, `HUB 3 • OPTIMIZATION`, `HUB 4 • DIAGNOSTICS`, `HUB 5 • DEPLOYMENT`).
   - Modern segmented pill tabs with subtle border outlines and active SVLL Blue state.
   - Professional Windows 11 card containers with 4px vertical brand indicator bars, 10px rounded corners, and clear `[ ⚡ ACTION ]` badges on all tools.

---

### 🚀 What's New in v6.0

#### 1. 🚀 Zero-Touch New PC Setup Wizard (`Hub 5 -> ProvisioningWizard`)
- Standardized corporate naming formulation (`SVLL-<LOC>-<TYPE>-<SERIAL>`).
- 1-Click commissioning pipeline: Hostname -> WHQL drivers -> Standard Apps baseline -> Ultimate Performance power scheme -> Security baseline -> Digital Asset Certificate on Desktop.

#### 2. 🖨️ Logistics Hardware Doctor (`Hub 4 -> LogisticsDoctor`)
- Direct RAW Spooler ZPL/TSPL label injection (`winspool.drv` RAW datatype).
- Serial RS-232 / USB COM Port sniffer with live ASCII terminal for weighbridge scales and barcode scanners.
- Deep print spooler repair and locked spool files purge.

#### 3. 💾 S.M.A.R.T. Drive Health & Crash Predictor (`Hub 4 -> StorageSmart`)
- NVMe/SATA controller register inspection via WMI.
- Real-time remaining life %, power-on hours, bad sector count, and controller temperatures.
- 1-Click reliability report export.

#### 4. 🔒 Endpoint Security, DLP & USB Port Lockdown (`Hub 3 -> SecurityDlp`)
- 1-Click USB Storage DLP: Set Read-Only to block data theft or Block USB Storage completely.
- Local Admin Vault (LAPS): 16-character cryptographic password generator and rotator.
- Defender Cloud Shield: Live real-time protection verification and quick scan trigger.

#### 5. 📡 Active Ports & Bandwidth Hog Hunter (`Hub 2 -> SocketInspector`)
- Live mapping of active TCP connections (`Get-NetTCPConnection`) to process executables and remote destination IPs.
- 1-Click process termination to liberate branch office bandwidth.

#### 6. 🔍 Global Command Palette (`Ctrl + K`)
- Instant spotlight search dialog across all 34 tools and quick actions.
- Keyboard navigation (`↑↓` and `Enter` to execute, `ESC` to dismiss).

---

### 📦 Checksums & Verification
- **Enterprise Installer:** `SVLL-IT-Workstation-v6.0-Setup.exe` (64.04 MB)
  - `SHA-256: 1056F770D707206DCBEE8C09F2FBB659AE9AA6C58EE7BCD08DA71FF77E8F73F3`
- **Portable Binary:** `SVLL-IT-Workstation.exe` (68.89 MB)
  - `SHA-256: 78AE62B4A88FB585E7C89FC9DE4CF427814214A52F2ED7AEA45B567F20BCFD8D`

---

### ⚡ 1-Click PowerShell Installation
```powershell
irm https://tinyurl.com/svll-setup | iex
```
*(Or use mirror `irm https://tinyurl.com/svll-workstation | iex`)*
