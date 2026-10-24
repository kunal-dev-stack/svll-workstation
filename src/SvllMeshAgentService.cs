// =============================================================================
// SVLL IT Workstation v8.3 — SVLL Inbuilt Mesh Agent Service
// Lightweight background agent running on User Edition workstations.
// Connects back to Admin Workstation (Hub 7 Inbuilt Listener on port 5055).
// Auto-registers on boot, pushes vitals telemetry, and executes remote IT commands.
// =============================================================================

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;

namespace SVLL_IT_Workstation;

public class SvllMeshAgentService
{
    private static SvllMeshAgentService? _instance;
    public static SvllMeshAgentService Instance => _instance ??= new SvllMeshAgentService();

    private CancellationTokenSource? _cts;
    private bool _isRunning = false;
    private string _targetAdminHost = "";
    private int _targetAdminPort = SvllMeshListener.DefaultPort;

    public bool IsConnected { get; private set; } = false;
    public DateTime? LastSuccessfulSync { get; private set; }

    public event Action<bool>? ConnectionStatusChanged;
    public event Action<string, string>? AdminMessageReceived;

    /// <summary>
    /// Starts the background mesh agent worker.
    /// </summary>
    public void Start(string? adminHost = null, int port = SvllMeshListener.DefaultPort)
    {
        if (_isRunning) return;

        _targetAdminPort = port;
        _targetAdminHost = !string.IsNullOrEmpty(adminHost) ? adminHost : ResolveAdminHost();

        _cts = new CancellationTokenSource();
        _isRunning = true;

        EnsureFirewallRule();
        EnsureAutoStartInstalled();

        Task.Run(() => AgentWorkerLoopAsync(_cts.Token));
    }

    public void Stop()
    {
        _isRunning = false;
        _cts?.Cancel();
        IsConnected = false;
        ConnectionStatusChanged?.Invoke(false);
    }

    private string ResolveAdminHost()
    {
        try
        {
            // 1. Check configured relay URL
            string relayUrl = AdminAuthService.Instance.GetRelayServerUrl();
            if (string.IsNullOrEmpty(relayUrl)) relayUrl = BuildConstants.DefaultRelayUrl;

            if (Uri.TryCreate(relayUrl, UriKind.Absolute, out var uri))
            {
                return uri.Host;
            }
        }
        catch { }

        return "192.168.10.222";
    }

    private async Task AgentWorkerLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _isRunning)
        {
            TcpClient? client = null;
            try
            {
                string host = ResolveAdminHost();
                client = new TcpClient();
                client.ReceiveTimeout = 30000;
                client.SendTimeout = 10000;

                await client.ConnectAsync(host, _targetAdminPort, ct);

                IsConnected = true;
                ConnectionStatusChanged?.Invoke(true);

                using var stream = client.GetStream();
                using var reader = new StreamReader(stream, Encoding.UTF8);
                using var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

                // 1. Send Handshake
                string handshake = BuildTelemetryPacket("handshake");
                await writer.WriteLineAsync(handshake);

                // Start periodic telemetry heartbeat loop
                var heartbeatCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                var heartbeatTask = Task.Run(async () =>
                {
                    while (!heartbeatCts.Token.IsCancellationRequested)
                    {
                        try
                        {
                            await Task.Delay(20000, heartbeatCts.Token);
                            string hb = BuildTelemetryPacket("heartbeat");
                            await writer.WriteLineAsync(hb);
                            LastSuccessfulSync = DateTime.Now;
                        }
                        catch { break; }
                    }
                }, heartbeatCts.Token);

                // Listen for incoming Admin commands
                while (!ct.IsCancellationRequested && client.Connected)
                {
                    string? line = await reader.ReadLineAsync(ct);
                    if (line == null) break;

                    line = line.Trim();
                    if (string.IsNullOrEmpty(line)) continue;

                    ProcessAdminCommand(line, writer);
                }

                heartbeatCts.Cancel();
            }
            catch (OperationCanceledException) { break; }
            catch (Exception)
            {
                IsConnected = false;
                ConnectionStatusChanged?.Invoke(false);
            }
            finally
            {
                try { client?.Close(); } catch { }
                IsConnected = false;
                ConnectionStatusChanged?.Invoke(false);
            }

            // Retry backoff
            await Task.Delay(8000, ct);
        }
    }

    private string BuildTelemetryPacket(string type)
    {
        var local = FleetRegistryService.Instance.LocalDevice;
        string remoteId = UltraRemoteService.GetRemoteId();
        double cpu = GetCpuLoad();
        double ram = GetRamUsage();
        double disk = GetCDriveFreePercent();

        var packet = new
        {
            type = type,
            hostname = Environment.MachineName,
            user = Environment.UserName,
            ip = local.IpAddress,
            os = local.OsVersion,
            serial = GetBiosSerial(),
            cpu = cpu,
            ram = ram,
            disk = disk,
            remoteId = remoteId,
            remotePassword = BuildConstants.DefaultRemotePassword,
            version = BuildConstants.AppVersion
        };

        return JsonSerializer.Serialize(packet);
    }

    private void ProcessAdminCommand(string json, StreamWriter writer)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            string action = root.TryGetProperty("action", out var aElem) ? aElem.GetString() ?? "" : "";

            if (action == "toast")
            {
                string title = "SVLL IT Support Notification";
                string body = "An IT administrator has connected to your workstation.";

                if (root.TryGetProperty("payload", out var pElem))
                {
                    if (pElem.TryGetProperty("title", out var tElem)) title = tElem.GetString() ?? title;
                    if (pElem.TryGetProperty("body", out var bElem)) body = bElem.GetString() ?? body;
                }

                ShowWindowsNotification(title, body);
                AdminMessageReceived?.Invoke(title, body);
            }
            else if (action == "remote_prep")
            {
                // Ensure RustDesk / UltraRemote is provisioned, running, and listening on direct port 21118
                _ = Task.Run(async () =>
                {
                    await UltraRemoteService.EnsureProvisionedAsync();
                    UltraRemoteService.EnsureDirectServerConfigured();
                });
            }
            else if (action == "ping")
            {
                _ = writer.WriteLineAsync("{\"type\":\"msg_reply\",\"hostname\":\"" + Environment.MachineName + "\",\"text\":\"pong\"}");
            }
        }
        catch { }
    }

    private static void ShowWindowsNotification(string title, string body)
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            try
            {
                MessageBox.Show(body, title, MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch { }
        });
    }

    private static void EnsureFirewallRule()
    {
        try
        {
            using var proc = Process.Start(new ProcessStartInfo
            {
                FileName = "netsh.exe",
                Arguments = "advfirewall firewall add rule name=\"SvllMeshAgentOut5055\" dir=out action=allow protocol=TCP remoteport=5055 enable=yes",
                UseShellExecute = false,
                CreateNoWindow = true
            });
            proc?.WaitForExit(2000);
        }
        catch { }
    }

    /// <summary>
    /// Configures silent auto-start for the workstation agent via CurrentUser Run key.
    /// </summary>
    public static void EnsureAutoStartInstalled()
    {
        try
        {
            string exe = Process.GetCurrentProcess().MainModule?.FileName ?? "";
            if (string.IsNullOrEmpty(exe) || !File.Exists(exe)) return;

            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
            if (key != null)
            {
                string? existing = key.GetValue("SVLL-IT-Workstation-Agent") as string;
                if (existing != exe)
                {
                    key.SetValue("SVLL-IT-Workstation-Agent", $"\"{exe}\" --minimized");
                }
            }
        }
        catch { }
    }

    private static string GetBiosSerial()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BIOS");
            foreach (ManagementObject obj in searcher.Get())
            {
                string? s = obj["SerialNumber"]?.ToString()?.Trim();
                if (!string.IsNullOrEmpty(s)) return s;
            }
        }
        catch { }
        return "N/A";
    }

    private static double GetCpuLoad()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT LoadPercentage FROM Win32_Processor");
            foreach (ManagementObject obj in searcher.Get())
            {
                if (ushort.TryParse(obj["LoadPercentage"]?.ToString(), out ushort p)) return p;
            }
        }
        catch { }
        return 0;
    }

    private static double GetRamUsage()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT TotalVisibleMemorySize, FreePhysicalMemory FROM Win32_OperatingSystem");
            foreach (ManagementObject obj in searcher.Get())
            {
                double total = Convert.ToDouble(obj["TotalVisibleMemorySize"]);
                double free = Convert.ToDouble(obj["FreePhysicalMemory"]);
                if (total > 0) return Math.Round(((total - free) / total) * 100, 1);
            }
        }
        catch { }
        return 0;
    }

    private static double GetCDriveFreePercent()
    {
        try
        {
            var drive = new DriveInfo("C");
            if (drive.IsReady && drive.TotalSize > 0)
            {
                return Math.Round(((double)drive.TotalFreeSpace / drive.TotalSize) * 100, 1);
            }
        }
        catch { }
        return 0;
    }
}
