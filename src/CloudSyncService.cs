// =============================================================================
// SVLL IT Workstation v8.0 - Cloud Sync Service
// Runs in background on both Admin and User builds.
//
//   USER BUILD:  Sends system heartbeat every 60 seconds to relay.
//                Polls for admin replies on submitted tickets every 2 minutes.
//
//   ADMIN BUILD: Polls /api/fleet every 30 seconds to get live device list.
//                Also sends its own heartbeat so it appears in the fleet.
//
// Relay server: http://<office-nas-ip>:5000  (configured in Admin Security Config)
// API auth:     X-SVLL-Key header with shared secret
// =============================================================================

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.NetworkInformation;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using SVLL_IT_Workstation.Models;

namespace SVLL_IT_Workstation;

public class CloudSyncService
{
    private static CloudSyncService? _instance;
    public static CloudSyncService Instance => _instance ??= new CloudSyncService();

    private readonly HttpClient _http;
    private CancellationTokenSource _cts = new();
    private string _relayBaseUrl = "";
    private RelayClientState _state = new();
    private readonly string _stateFilePath;

    // Events the UI can subscribe to
    public event Action<List<RemoteDeviceItem>>? FleetUpdated;
    public event Action<bool>? RelayConnected; // true = online, false = offline

    public bool IsRelayReachable { get; private set; } = false;
    public List<RemoteDeviceItem> CachedFleet { get; private set; } = new();

    private CloudSyncService()
    {
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
        _http.DefaultRequestHeaders.Add("X-SVLL-Key", BuildConstants.RelayApiKey);
        _http.DefaultRequestHeaders.Add("User-Agent", $"SVLL-Workstation/{BuildConstants.AppVersion}-{BuildConstants.BuildEdition}");

        _stateFilePath = Path.Combine(MainWindow.AuditLogDirectory, "relay_client_state.json");
        LoadState();
    }

    // -------------------------------------------------------------------------
    // INITIALIZATION — called from MainWindow constructor after layout is built
    // -------------------------------------------------------------------------
    public void Start(string relayServerUrl)
    {
        if (string.IsNullOrWhiteSpace(relayServerUrl))
        {
            IsRelayReachable = false;
            return;
        }

        _relayBaseUrl = relayServerUrl.TrimEnd('/');
        _cts = new CancellationTokenSource();

        // Start background task
        _ = Task.Run(() => BackgroundLoopAsync(_cts.Token));
    }

    public void Stop()
    {
        _cts.Cancel();
    }

    // -------------------------------------------------------------------------
    // BACKGROUND LOOP
    // -------------------------------------------------------------------------
    private async Task BackgroundLoopAsync(CancellationToken ct)
    {
        // Initial connectivity probe
        await CheckRelayReachabilityAsync();
        if (IsRelayReachable)
        {
            await SendHeartbeatAsync();
#if BUILD_ADMIN
            await PollFleetAsync();
#endif
        }

        int heartbeatTick = 0;
        int fleetPollTick = 0;
        int replyPollTick = 0;

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(5000, ct); // tick every 5 seconds
                heartbeatTick += 5;
                fleetPollTick += 5;
                replyPollTick += 5;

                // Re-check relay every 60s if currently offline
                if (!IsRelayReachable)
                {
                    await CheckRelayReachabilityAsync();
                    if (!IsRelayReachable) continue;
                }

                // Heartbeat: every 60 seconds (all builds)
                if (heartbeatTick >= 60)
                {
                    heartbeatTick = 0;
                    await SendHeartbeatAsync();
                }

#if BUILD_ADMIN
                // Fleet poll: every 30 seconds (Admin build only)
                if (fleetPollTick >= 30)
                {
                    fleetPollTick = 0;
                    await PollFleetAsync();
                }
#endif

            }
            catch (OperationCanceledException) { break; }
            catch { /* never crash the background loop */ }
        }
    }

    // -------------------------------------------------------------------------
    // HEARTBEAT — push this machine's full system info to relay
    // -------------------------------------------------------------------------
    public async Task SendHeartbeatAsync()
    {
        if (!IsRelayReachable) return;
        try
        {
            var device = CollectLocalDeviceInfo();
            var response = await _http.PostAsJsonAsync($"{_relayBaseUrl}/api/heartbeat", device);
            if (response.IsSuccessStatusCode)
            {
                _state.LastHeartbeatSent = DateTime.Now;
                SaveState();
            }
        }
        catch { }
    }

    // -------------------------------------------------------------------------
    // LOCAL DEVICE INFO COLLECTION (v8.0 — full telemetry)
    // -------------------------------------------------------------------------
    public RemoteDeviceItem CollectLocalDeviceInfo()
    {
        var local = FleetRegistryService.Instance.LocalDevice;

        // Get serial number via WMI
        string serial = GetBiosSerial();

        // Get OS build number
        string osBuild = GetOsBuildNumber();

        // Get disk free percent for C:
        double diskFree = GetCDriveFreePercent();

        // Get MeshAgent status and node ID
        (bool meshRunning, string meshNodeId) = MeshAgentService.GetStatus();

        // Get CPU and RAM
        double cpu = GetCpuLoad();
        double ram = GetRamUsage();

        return new RemoteDeviceItem
        {
            Id = $"SVLL-{Environment.MachineName}",
            Hostname = Environment.MachineName,
            IpAddress = local.IpAddress,
            MacAddress = local.MacAddress,
            BranchLocation = local.BranchLocation,
            LoggedInUser = Environment.UserName,
            AnyDeskId = local.AnyDeskId,
            AnyDeskInstalled = local.AnyDeskInstalled,
            RdpEnabled = local.RdpEnabled,
            Status = local.Status,
            SupportRequestMessage = local.SupportRequestMessage,
            SupportRequestedAt = local.SupportRequestedAt,
            OsVersion = local.OsVersion,
            OsBuild = osBuild,
            SerialNumber = serial,
            CpuLoadPercent = cpu,
            RamUsagePercent = ram,
            DiskFreePercent = diskFree,
            MeshAgentRunning = meshRunning,
            MeshNodeId = meshNodeId,
            LastSeen = DateTime.Now,
            IsLocalHost = true
        };
    }

    private static string? _cachedSerial;

    private static string GetBiosSerial()
    {
        if (_cachedSerial != null) return _cachedSerial;

        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BIOS");
            foreach (ManagementObject obj in searcher.Get())
            {
                string? serial = obj["SerialNumber"]?.ToString()?.Trim();
                if (!string.IsNullOrEmpty(serial) &&
                    serial != "To be filled by O.E.M." &&
                    serial != "Default string" &&
                    serial.Length > 3)
                {
                    _cachedSerial = serial;
                    return _cachedSerial;
                }
            }
        }
        catch { }

        // Fallback: Motherboard UUID
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT UUID FROM Win32_ComputerSystemProduct");
            foreach (ManagementObject obj in searcher.Get())
            {
                string? uuid = obj["UUID"]?.ToString()?.Trim();
                if (!string.IsNullOrEmpty(uuid) && uuid != "FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF")
                {
                    _cachedSerial = "UUID:" + uuid;
                    return _cachedSerial;
                }
            }
        }
        catch { }

        _cachedSerial = "N/A";
        return _cachedSerial;
    }

    private static string GetOsBuildNumber()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            string? build = key?.GetValue("CurrentBuildNumber")?.ToString();
            string? ubr = key?.GetValue("UBR")?.ToString(); // Update Build Revision
            if (!string.IsNullOrEmpty(build))
                return ubr != null ? $"{build}.{ubr}" : build;
        }
        catch { }
        return Environment.OSVersion.Version.Build.ToString();
    }

    private static double GetCDriveFreePercent()
    {
        try
        {
            var drive = new DriveInfo("C");
            if (drive.IsReady)
            {
                return Math.Round((double)drive.AvailableFreeSpace / drive.TotalSize * 100, 1);
            }
        }
        catch { }
        return 0.0;
    }

    private static double GetCpuLoad()
    {
        try
        {
            return Math.Round(MainWindow.LastCpuPercentage, 1);
        }
        catch { }
        return 0.0;
    }

    private static double GetRamUsage()
    {
        try
        {
            var mem = new MainWindow.MEMORYSTATUSEX();
            if (MainWindow.GetMemoryStatus(mem))
            {
                return (double)mem.dwMemoryLoad;
            }
        }
        catch { }
        return 0.0;
    }

    // -------------------------------------------------------------------------
    // FLEET POLL — Admin build pulls full live fleet from relay
    // -------------------------------------------------------------------------
    public async Task PollFleetAsync()
    {
        if (!IsRelayReachable) return;
        try
        {
            var response = await _http.GetAsync($"{_relayBaseUrl}/api/fleet");
            if (response.IsSuccessStatusCode)
            {
                string json = await response.Content.ReadAsStringAsync();
                var devices = JsonSerializer.Deserialize<List<RemoteDeviceItem>>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (devices != null)
                {
                    CachedFleet = devices;
                    FleetUpdated?.Invoke(devices);
                }
            }
        }
        catch { }
    }



    // -------------------------------------------------------------------------
    // RELAY REACHABILITY CHECK
    // -------------------------------------------------------------------------
    private async Task CheckRelayReachabilityAsync()
    {
        if (string.IsNullOrEmpty(_relayBaseUrl))
        {
            IsRelayReachable = false;
            return;
        }
        try
        {
            var response = await _http.GetAsync($"{_relayBaseUrl}/api/health");
            bool wasReachable = IsRelayReachable;
            IsRelayReachable = response.IsSuccessStatusCode;
            if (wasReachable != IsRelayReachable)
                RelayConnected?.Invoke(IsRelayReachable);
        }
        catch
        {
            bool wasReachable = IsRelayReachable;
            IsRelayReachable = false;
            if (wasReachable)
                RelayConnected?.Invoke(false);
        }
    }

    // -------------------------------------------------------------------------
    // STATE PERSISTENCE
    // -------------------------------------------------------------------------
    private void LoadState()
    {
        try
        {
            if (File.Exists(_stateFilePath))
            {
                string json = File.ReadAllText(_stateFilePath);
                var loaded = JsonSerializer.Deserialize<RelayClientState>(json);
                if (loaded != null) _state = loaded;
            }
        }
        catch { }
    }

    private void SaveState()
    {
        try
        {
            if (!Directory.Exists(MainWindow.AuditLogDirectory))
                Directory.CreateDirectory(MainWindow.AuditLogDirectory);
            string json = JsonSerializer.Serialize(_state, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_stateFilePath, json);
        }
        catch { }
    }

    /// <summary>Send offline status before application closes</summary>
    public async Task SendOfflineStatusAsync()
    {
        if (!IsRelayReachable) return;
        try
        {
            var device = CollectLocalDeviceInfo();
            device.Status = "Offline";
            await _http.PostAsJsonAsync($"{_relayBaseUrl}/api/heartbeat", device);
        }
        catch { }
    }
}
