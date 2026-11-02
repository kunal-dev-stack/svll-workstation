# AegisMesh Android Agent

Enterprise-grade remote management, diagnostics, and screen-sharing client for Android endpoints.

---

### Capabilities & Platform Restrictions

1. **Remote Screen Sharing**:
   - Utilizes standard Android `MediaProjection` API.
   - Per Android security policy, the device user is presented with a system consent prompt (*"Start recording or casting with AegisMesh Agent?"*).
   - Once approved, screen frames are compressed to JPEG and streamed over an encrypted TLS WebSocket session to the administrator console.
2. **Foreground Service & Transparency**:
   - Runs as an active Foreground Service with `android:foregroundServiceType="mediaProjection|connectedDevice"`.
   - Displays a persistent status-bar notification informing the device owner that an administrative session is active, complete with a one-click "Disconnect" button.
3. **Hardware & System Diagnostics**:
   - Queries `BatteryManager` (level, charging status, health).
   - Queries `StatFs` (internal storage used and free GB).
   - Queries `ActivityManager.MemoryInfo` (RAM used and available MB).
   - Queries `ConnectivityManager` (WiFi SSID, Cellular, signal quality).
4. **File & Photo Management**:
   - Implements Android 10+ Scoped Storage guidelines via Storage Access Framework (SAF) and system picker APIs.
   - For enterprise-managed devices (Device Owner mode), broader storage management can be pre-configured via corporate MDM policy.
5. **Call Logs & SMS Policy Compliance**:
   - Under standard Google Play policies, third-party apps cannot access SMS or Call Logs unless configured as default SMS/Dialer apps.
   - For internal enterprise deployments (sideloaded APK or Android Enterprise Private App Store), permissions (`READ_CALL_LOG`, `READ_SMS`) are controlled via runtime permission grants and require explicit corporate disclosure and employee consent.

---

### Build Instructions

1. Ensure the Android SDK (API 34) is installed.
2. Build the debug APK:
   ```bash
   ./gradlew assembleDebug
   ```
3. Install on a connected Android device or emulator:
   ```bash
   adb install -r app/build/outputs/apk/debug/app-debug.apk
   ```
4. Launch the application, enter the Mesh Server Gateway URL and Enrollment Token, and tap **Connect to Gateway**.
