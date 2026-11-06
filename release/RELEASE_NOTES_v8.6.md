# Shree Vasu Logistics Limited - SVLL IT Workstation Suite v8.6

**Release Tag:** `v8.6`  
**Date:** November 2026  
**Author:** Kunal Turkar (IT Department - Shree Vasu Logistics Limited)

---

## 🚀 What's New & Fixed in v8.6

### 1. CCTV & Enterprise Multi-NVR Surveillance Center (Admin Hub 7 & Top Menu)
- **Pre-Configured 46 Campus NVR Matrix:** Full integration of all 46 enterprise NVR/DVR nodes across Central Depots and Regional Hubs (Raipur Head Office, Raipur Central Warehouse, Transport Nagar, Bilaspur, Durg, Bhilai, Indore, Jabalpur, Kolkata, Nagpur, and all transit corridors).
- **Embedded Hub Metadata:** Includes depot names, sub-locations, total channel capacities (8/16/32/64 channels), IP hosts, RTSP ports, facility manager contact numbers, and retention window guidelines.
- **Credential Storage:** Saved encrypted credentials for each NVR/DVR for zero-friction access across sessions (`%APPDATA%\SVLL_IT_Workstation\cctv_nvrs.json`).
- **NVR Management Studio:** 1-click dialog to add, edit, or remove NVR/DVR profiles with automated RTSP stream construction for Dahua, Hikvision, CP PLUS, Uniview, and Generic ONVIF brands.

### 2. Hardware-Accelerated Smooth RTSP Live Video Wall (30 FPS)
- **Fluid Video Streaming Engine:** Integrated LibVLC hardware acceleration flags (`--avcodec-hw=any`, network caching down to 250ms, direct3d11 GPU rendering) eliminating frame stuttering and lag.
- **Flexible Grid Configurations:** 1x1 Focus View, 2x2 Quad Monitor, 3x3 Multi-View, and 4x4 Enterprise Video Wall (up to 16 concurrent live streams).
- **Background Resource Management:** Automatically detaches and disposes hidden media players when switching views or changing channels, keeping memory and CPU footprint minimal.
- **Fullscreen Mode:** 1-Click wide video wall fullscreen toggle with top-tier exit button to maximize viewing area across multi-monitor NOC displays.

### 3. 24-Hour Historical NVR Timeline Playback Studio
- **Interactive Calendar with Recording Presence:** Select any past date with visual recording status indicators.
- **Interactive 24-Hour Timeline Scrubber:** Full 00:00 - 24:00 time slider with real-time seeking, elapsed time indicators, and quick jump buttons for shift changes (Morning, Shift 1, Lunch, Shift 2, Dispatch, Night Shift, Midnight).
- **Camera Navigation Controls:** Dedicated `◀ Prev Cam` and `Next Cam ▶` switch buttons for cycling through depot cameras without returning to the main menu.
- **Variable Playback Speeds:** Fast-forward and slow-motion playback at 0.5x, 1.0x, 2.0x, 4.0x, 8.0x, and 16.0x speeds.
- **Evidence Snapshots:** 1-Click snapshot capture tool saving high-resolution frames with timestamp overlays to the local evidence vault.

### 4. Dedicated CCTV Diagnostic & Health Studio
- **Ultra-Fast Parallel & Selective Fleet Audit:** Slashing audit time from 15+ minutes down to 3-5 seconds using concurrent workers, fast unreachable short-circuiting, and selective target filtering ( Raipu Campus, Nagpur Campus, Bilaspur Campus, Durg / Bhilai, Indore / MP, or individual NVRs).
- **Multi-Vector Hub Probing:** Automated concurrent diagnostics testing ICMP ping reachability, RTSP port 554, Web port 80, Hikvision SDK port 8000, and Dahua/CP PLUS SDK port 37777.
- **Network Quality Metrics:** Displays real-time round-trip latency, jitter, packet loss percentages, and estimated stream bandwidth.
- **Subnet CIDR Range Scanner:** In-app network discovery tool to scan local `/24` subnets (e.g., `192.168.1.0/24`, `192.168.11.0/24`) to identify active cameras, unlisted DVRs, and open RTSP ports.
- **Executive Audit Reports:** 1-Click export to structured CSV and interactive executive HTML Health Reports with depot status summaries and color-coded risk alerts.
- **Acoustic Downtime Alarm:** Audible sound alerts when an offline or unreachable NVR node is detected during live diagnostic sweeps.

### 5. Packaged LibVLC Native Runtime & Codecs
- **Bundled Engine & 422 Native Plugins:** The complete LibVLC native runtime and 422 codecs are now bundled directly within the Inno Setup installer packages (`{app}\libvlc`), eliminating "playback engine is not available" errors.
- **Responsive Connection Watchdogs:** Replaced black screen lockups with live connection indicators (`🟡 CONNECTING...`, `🟢 SUB (30 FPS)`, `🔴 UNREACHABLE`, and `⚠️ TIMEOUT`) with automated background player management.
- **Background Pre-Warming:** LibVLC engine initializes asynchronously on application launch, ensuring opening the CCTV surveillance module is completely instantaneous without UI freezes.

### 6. Non-Overlapping 2-Tier Header Architecture
- **Responsive 2-Tier Top Bar:** Split the surveillance header into an upper action tool bar (title, badges, fullscreen, Add NVR, Snap All) and a dedicated lower studio bar (view modes, grid layouts, engine switcher, channel counters).
- **Clean Responsive Layout:** Fully resolved overlapping buttons and truncated controls across all screen sizes and DPI scaling levels.

### 7. Seamless In-App Automated Upgrade
- **1-Click Self-Updating Engine:** Queries GitHub API for release `v8.6`, resolves edition-matched setup installer (`SVLL-IT-Workstation-Admin-v8.6-Setup.exe` or `SVLL-IT-Workstation-User-v8.6-Setup.exe`), downloads silently, and completes background replacement.
- **Backward-Compatible Setup Aliases:** Standalone setup aliases provided (`SVLL-IT-Workstation-Admin-Setup.exe` and `SVLL-IT-Workstation-User-Setup.exe`) ensuring older workstation versions upgrade smoothly without manual intervention.

---

## 📦 Distribution Packages

| Edition | Installer Package | Binary Executable | Target Audience |
| :--- | :--- | :--- | :--- |
| **Admin Edition** | `SVLL-IT-Workstation-Admin-v8.6-Setup.exe` | `SVLL-IT-Workstation-Admin.exe` | IT Engineers, Helpdesk & CCTV NOC |
| **User Edition** | `SVLL-IT-Workstation-User-v8.6-Setup.exe` | `SVLL-IT-Workstation-User.exe` | General Staff & Warehouse Depots |

