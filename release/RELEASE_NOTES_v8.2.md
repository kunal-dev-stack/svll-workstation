# SVLL IT Support Workstation - Release Notes v8.2

**Release Date:** October 2026  
**Author:** Kunal Turkar  
**Organization:** Shree Vasu Logistics Limited  
**Target Platform:** Windows 10 / 11 (x64) • .NET 8.0 WPF  

---

## 🚀 What's New in v8.2 — Zero-Touch Direct Remote Engine, Full Flutter Companion Bundle & Remote Audit Logging

SVLL IT Support Workstation v8.2 permanently resolves client-side library dependencies, configures out-of-the-box Direct IP peer-to-peer remote access (port 21118), bundles the complete Flutter plugin suite, and introduces compliant remote session audit logging.

---

### ⭐ Highlights of v8.2

1. **📦 Complete Flutter Plugin & Asset Suite Bundled:**
   - Bundled all 17 Flutter companion runtime DLLs (`desktop_drop_plugin.dll`, `desktop_multi_window_plugin.dll`, `dylib_virtual_display.dll`, `file_selector_windows_plugin.dll`, `flutter_windows.dll`, `librustdesk.dll`, `usbmmidd_v2` virtual display drivers, etc.) into the application package.
   - Eliminates the error: `"The code execution cannot proceed because desktop_drop_plugin.dll was not found."`
   - Setup installers deploy runtime dependencies simultaneously to `{app}\tools\` and `C:\Program Files\RustDesk\`.

2. **⚡ Out-of-the-Box Direct IP Listener (Port 21118):**
   - Automatically injects `direct-server = 'Y'` and `direct-access-port = '21118'` into `RustDesk2.toml` across `LocalService` and user profiles.
   - Automatically opens inbound Windows Firewall rules for TCP port `21118` and UDP ports `21118-21119`.
   - Eliminates the error: `"Failed to connect to 192.168.10.x:21118: Please try later."`
   - Enables seamless 1-click peer-to-peer remote control across office LAN subnets without requiring external relay servers.

3. **🛡️ Automatic Engine Self-Healing (`SyncBundledTools`):**
   - On application launch and background fleet synchronization, the engine verifies the integrity of `C:\Program Files\RustDesk`. If any DLL is missing or corrupted, it automatically repairs it from the bundled tools repository.

4. **📝 Remote Session Audit Trail:**
   - Added compliant remote connection audit logging to `%ProgramData%\SVLL-IT-Workstation\audit\remote_connections.log`.
   - Records UTC timestamp, operator Windows username, target device identity (IP or Remote ID), connection mode (`Direct LAN` or `Partner ID`), and connection launch outcome without logging sensitive secrets.

5. **🔄 Seamless In-Place Upgrades:**
   - Client and Admin installers terminate stale background service processes before in-place overwriting, preventing locked file errors during background upgrades.

---

### 📦 Release Assets

- **User / Client Edition Setup:** `SVLL-IT-Workstation-User-v8.2-Setup.exe` (25.4 MB)
- **Admin Edition Setup:** `SVLL-IT-Workstation-Admin-v8.2-Setup.exe` (25.4 MB)
- **Legacy Updater Alias:** `SVLL-IT-Workstation-v8.2-Setup.exe` (25.4 MB)
- **Standalone Admin Executable:** `SVLL-IT-Workstation-Admin.exe` (68.9 MB)
- **Standalone User Executable:** `SVLL-IT-Workstation-User.exe` (68.9 MB)
- **SHA256 Checksums:** `CHECKSUMS.txt`
