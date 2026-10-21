# SVLL IT Support Workstation v6.0 - Enterprise Provisioning & Logistics Doctor Edition

**Release Date:** October 21, 2026  
**Developer:** Kunal Turkar  
**Organization:** Shree Vasu Logistics Limited  
**Target OS:** Windows 10 & Windows 11 (64-bit)

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
- **Enterprise Installer:** `SVLL-IT-Workstation-v6.0-Setup.exe` (64.08 MB)
  - `SHA-256: C1E2CBA519F4E2FDAD2ECE64891216A7ED00287C1732095ABE7B239D564D05D3`
- **Portable Binary:** `SVLL-IT-Workstation.exe` (68.89 MB)
  - `SHA-256: E3D3EA2137F9C32B205A78C08CAF03F9BF9897E7E0DA1895D3786DCC0ECC8079`

---

### ⚡ 1-Click PowerShell Installation
```powershell
irm https://tinyurl.com/svll-setup | iex
```
*(Or use mirror `irm https://tinyurl.com/svll-workstation | iex`)*
