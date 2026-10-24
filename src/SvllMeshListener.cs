// =============================================================================
// SVLL IT Workstation v8.3 — Inbuilt Mesh Listener Service
// Embedded TCP & Telemetry Listener running inside Admin Edition (Hub 7).
// Listens on Port 5055 for live connections from SVLL Mesh Agents on User PCs.
// Provides instant 2-way messaging, vitals telemetry, ping, and remote commands.
// =============================================================================

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SVLL_IT_Workstation.Models;

namespace SVLL_IT_Workstation;

public class ConnectedMeshClient
{
    public string Hostname { get; set; } = "";
    public string IpAddress { get; set; } = "";
    public string LoggedInUser { get; set; } = "";
    public string OsVersion { get; set; } = "";
    public string SerialNumber { get; set; } = "";
    public double CpuLoad { get; set; } = 0.0;
    public double RamUsage { get; set; } = 0.0;
    public double DiskFree { get; set; } = 0.0;
    public string RemoteId { get; set; } = "";
    public string RemotePassword { get; set; } = BuildConstants.DefaultRemotePassword;
    public DateTime ConnectedAt { get; set; } = DateTime.Now;
    public DateTime LastHeartbeat { get; set; } = DateTime.Now;

    [System.Text.Json.Serialization.JsonIgnore]
    public TcpClient? Socket { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public StreamWriter? Writer { get; set; }
}

public class SvllMeshListener
{
    private static SvllMeshListener? _instance;
    public static SvllMeshListener Instance => _instance ??= new SvllMeshListener();

    public const int DefaultPort = 5055;
    private TcpListener? _listener;
    private CancellationTokenSource? _cts;
    private readonly ConcurrentDictionary<string, ConnectedMeshClient> _clients = new(StringComparer.OrdinalIgnoreCase);

    public bool IsRunning { get; private set; } = false;
    public int ActivePort { get; private set; } = DefaultPort;

    public event Action<ConnectedMeshClient>? ClientConnected;
    public event Action<string>? ClientDisconnected;
    public event Action<ConnectedMeshClient>? ClientTelemetryUpdated;
    public event Action<string, string>? MessageReceived;

    public int ConnectedCount => _clients.Count;
    public ConnectedMeshClient[] GetAllClients() => _clients.Values.ToArray();

    public ConnectedMeshClient? GetClient(string hostnameOrIp)
    {
        if (string.IsNullOrEmpty(hostnameOrIp)) return null;

        if (_clients.TryGetValue(hostnameOrIp, out var client))
            return client;

        return _clients.Values.FirstOrDefault(c =>
            c.Hostname.Equals(hostnameOrIp, StringComparison.OrdinalIgnoreCase) ||
            c.IpAddress.Equals(hostnameOrIp, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Starts the Inbuilt Mesh Listener on the specified port.
    /// Opens the Windows Firewall rule automatically if elevated.
    /// </summary>
    public bool Start(int port = DefaultPort)
    {
        if (IsRunning) return true;

        try
        {
            ActivePort = port;
            EnsureFirewallRuleOpen(port);

            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start(100);
            _cts = new CancellationTokenSource();
            IsRunning = true;

            Task.Run(() => ListenLoopAsync(_listener, _cts.Token));
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SvllMeshListener] Start failed: {ex.Message}");
            IsRunning = false;
            return false;
        }
    }

    public void Stop()
    {
        if (!IsRunning) return;

        IsRunning = false;
        _cts?.Cancel();

        try { _listener?.Stop(); } catch { }
        _listener = null;

        foreach (var c in _clients.Values)
        {
            try { c.Writer?.Dispose(); } catch { }
            try { c.Socket?.Close(); } catch { }
        }
        _clients.Clear();
    }

    private async Task ListenLoopAsync(TcpListener listener, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && IsRunning)
        {
            try
            {
                var socket = await listener.AcceptTcpClientAsync(ct);
                _ = Task.Run(() => HandleClientAsync(socket, ct), ct);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                if (!IsRunning) break;
                Debug.WriteLine($"[SvllMeshListener] Accept error: {ex.Message}");
                await Task.Delay(500, ct);
            }
        }
    }

    private async Task HandleClientAsync(TcpClient socket, CancellationToken ct)
    {
        string clientEndpoint = socket.Client.RemoteEndPoint?.ToString() ?? "Unknown";
        string clientIp = (socket.Client.RemoteEndPoint as IPEndPoint)?.Address.ToString() ?? "";
        string registeredHostname = "";
        ConnectedMeshClient? meshClient = null;

        try
        {
            var stream = socket.GetStream();
            var reader = new StreamReader(stream, Encoding.UTF8);
            var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

            while (!ct.IsCancellationRequested && socket.Connected)
            {
                string? line = await reader.ReadLineAsync(ct);
                if (line == null) break;

                line = line.Trim();
                if (string.IsNullOrEmpty(line)) continue;

                try
                {
                    using var doc = JsonDocument.Parse(line);
                    var root = doc.RootElement;
                    string type = root.TryGetProperty("type", out var tElem) ? tElem.GetString() ?? "" : "";

                    if (type == "handshake" || type == "heartbeat")
                    {
                        string host = root.TryGetProperty("hostname", out var hElem) ? hElem.GetString() ?? "" : "";
                        if (string.IsNullOrEmpty(host)) host = clientIp;

                        registeredHostname = host;

                        if (meshClient == null)
                        {
                            meshClient = new ConnectedMeshClient
                            {
                                Hostname = host,
                                IpAddress = !string.IsNullOrEmpty(clientIp) ? clientIp : (root.TryGetProperty("ip", out var ipElem) ? ipElem.GetString() ?? "" : ""),
                                LoggedInUser = root.TryGetProperty("user", out var uElem) ? uElem.GetString() ?? "" : "",
                                OsVersion = root.TryGetProperty("os", out var osElem) ? osElem.GetString() ?? "" : "",
                                SerialNumber = root.TryGetProperty("serial", out var sElem) ? sElem.GetString() ?? "" : "",
                                RemoteId = root.TryGetProperty("remoteId", out var rElem) ? rElem.GetString() ?? "" : "",
                                RemotePassword = root.TryGetProperty("remotePassword", out var pElem) ? pElem.GetString() ?? BuildConstants.DefaultRemotePassword : BuildConstants.DefaultRemotePassword,
                                ConnectedAt = DateTime.Now,
                                LastHeartbeat = DateTime.Now,
                                Socket = socket,
                                Writer = writer
                            };

                            if (root.TryGetProperty("cpu", out var cpuElem)) meshClient.CpuLoad = cpuElem.GetDouble();
                            if (root.TryGetProperty("ram", out var ramElem)) meshClient.RamUsage = ramElem.GetDouble();
                            if (root.TryGetProperty("disk", out var diskElem)) meshClient.DiskFree = diskElem.GetDouble();

                            _clients[host] = meshClient;

                            // Reflect into FleetRegistryService
                            UpdateFleetRegistry(meshClient);

                            ClientConnected?.Invoke(meshClient);

                            // Reply ACK
                            await writer.WriteLineAsync("{\"status\":\"ok\",\"action\":\"registered\",\"serverTime\":\"" + DateTime.UtcNow.ToString("O") + "\"}");
                        }
                        else
                        {
                            // Update heartbeat vitals
                            meshClient.LastHeartbeat = DateTime.Now;
                            if (root.TryGetProperty("cpu", out var cpuElem)) meshClient.CpuLoad = cpuElem.GetDouble();
                            if (root.TryGetProperty("ram", out var ramElem)) meshClient.RamUsage = ramElem.GetDouble();
                            if (root.TryGetProperty("disk", out var diskElem)) meshClient.DiskFree = diskElem.GetDouble();
                            if (root.TryGetProperty("remoteId", out var rElem)) meshClient.RemoteId = rElem.GetString() ?? meshClient.RemoteId;

                            UpdateFleetRegistry(meshClient);
                            ClientTelemetryUpdated?.Invoke(meshClient);

                            // Reply heartbeat ACK
                            await writer.WriteLineAsync("{\"status\":\"ok\",\"action\":\"pong\"}");
                        }
                    }
                    else if (type == "msg_reply")
                    {
                        string sender = root.TryGetProperty("hostname", out var hElem) ? hElem.GetString() ?? registeredHostname : registeredHostname;
                        string text = root.TryGetProperty("text", out var txtElem) ? txtElem.GetString() ?? "" : "";
                        MessageReceived?.Invoke(sender, text);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[SvllMeshListener] JSON parse error: {ex.Message}");
                }
            }
        }
        catch { }
        finally
        {
            if (!string.IsNullOrEmpty(registeredHostname))
            {
                _clients.TryRemove(registeredHostname, out _);
                ClientDisconnected?.Invoke(registeredHostname);
            }
            try { socket.Close(); } catch { }
        }
    }

    private void UpdateFleetRegistry(ConnectedMeshClient c)
    {
        try
        {
            var item = new RemoteDeviceItem
            {
                Hostname = c.Hostname,
                IpAddress = c.IpAddress,
                LoggedInUser = c.LoggedInUser,
                OsVersion = c.OsVersion,
                SerialNumber = c.SerialNumber,
                RemoteId = c.RemoteId,
                RemotePassword = c.RemotePassword,
                RemoteEngine = "UltraRemote",
                CpuLoadPercent = c.CpuLoad,
                RamUsagePercent = c.RamUsage,
                DiskFreePercent = c.DiskFree,
                Status = "Online",
                MeshAgentRunning = true,
                LastSeen = DateTime.Now
            };
            FleetRegistryService.Instance.RegisterOrUpdateDevice(item);
        }
        catch { }
    }

    /// <summary>
    /// Sends a JSON command to a specific connected agent.
    /// </summary>
    public async Task<bool> SendCommandAsync(string hostnameOrIp, string action, object? payload = null)
    {
        var client = GetClient(hostnameOrIp);
        if (client?.Writer == null || client.Socket == null || !client.Socket.Connected)
            return false;

        try
        {
            var packet = new
            {
                action = action,
                payload = payload,
                timestamp = DateTime.UtcNow
            };
            string json = JsonSerializer.Serialize(packet);
            await client.Writer.WriteLineAsync(json);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Sends a live toast notification message to a specific user workstation.
    /// </summary>
    public async Task<bool> SendMessageToUserAsync(string hostnameOrIp, string messageTitle, string messageBody)
    {
        return await SendCommandAsync(hostnameOrIp, "toast", new
        {
            title = messageTitle,
            body = messageBody
        });
    }

    /// <summary>
    /// Directs a user workstation to prepare its Ultra Remote engine (starts service & listener on port 21118).
    /// </summary>
    public async Task<bool> RequestRemoteSessionPrepAsync(string hostnameOrIp)
    {
        return await SendCommandAsync(hostnameOrIp, "remote_prep", new
        {
            mode = "direct_lan_21118",
            password = BuildConstants.DefaultRemotePassword
        });
    }

    /// <summary>
    /// Broadcasts a banner toast message to all currently connected user workstations.
    /// </summary>
    public async Task<int> BroadcastMessageAsync(string title, string body)
    {
        int success = 0;
        foreach (var c in _clients.Values)
        {
            if (await SendMessageToUserAsync(c.Hostname, title, body))
                success++;
        }
        return success;
    }

    private static void EnsureFirewallRuleOpen(int port)
    {
        try
        {
            using var proc = Process.Start(new ProcessStartInfo
            {
                FileName = "netsh.exe",
                Arguments = $"advfirewall firewall add rule name=\"SvllMeshListenerPort{port}\" dir=in action=allow protocol=TCP localport={port} enable=yes",
                UseShellExecute = false,
                CreateNoWindow = true
            });
            proc?.WaitForExit(2000);
        }
        catch { }
    }
}
