using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AegisMeshAgent
{
    public class AgentCore
    {
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private readonly string _serverUrl;
        private readonly string _token;
        private readonly string _deviceId;
        private ClientWebSocket? _ws;
        private bool _isStreamingDesktop = false;
        private SessionBannerForm? _bannerForm;

        public AgentCore(string serverUrl, string token, string? deviceId = null)
        {
            _serverUrl = serverUrl.TrimEnd('/');
            _token = token;
            _deviceId = deviceId ?? GetOrCreateDeviceId();
        }

        private string GetOrCreateDeviceId()
        {
            string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "AegisMesh");
            Directory.CreateDirectory(appData);
            string idFile = Path.Combine(appData, "device.id");

            if (File.Exists(idFile))
            {
                return File.ReadAllText(idFile).Trim();
            }

            string newId = $"win-{Environment.MachineName.ToLower()}-{Guid.NewGuid().ToString("N")[..8]}";
            try { File.WriteAllText(idFile, newId); } catch { }
            return newId;
        }

        public async Task StartAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine($"[AGENT] Initializing AegisMesh Endpoint Agent...");
            Console.WriteLine($"[AGENT] Device ID: {_deviceId}");
            Console.WriteLine($"[AGENT] Host: {Environment.MachineName} ({Environment.OSVersion})");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _ws = new ClientWebSocket();
                    var uri = new Uri($"{_serverUrl}/ws?role=agent&deviceId={_deviceId}&token={_token}");

                    Console.WriteLine($"[AGENT] Connecting to Mesh Gateway at {uri}...");
                    await _ws.ConnectAsync(uri, stoppingToken);
                    Console.WriteLine($"[AGENT] Connected securely to Mesh Gateway!");

                    // 1. Send registration
                    await SendRegistrationAsync(stoppingToken);

                    // 2. Launch background heartbeat and desktop streaming loops
                    var heartbeatTask = RunHeartbeatLoopAsync(stoppingToken);
                    var receiveTask = RunReceiveLoopAsync(stoppingToken);
                    var streamTask = RunDesktopStreamLoopAsync(stoppingToken);

                    await Task.WhenAny(receiveTask, heartbeatTask);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[AGENT] Connection error: {ex.Message}. Reconnecting in 5s...");
                }
                finally
                {
                    _isStreamingDesktop = false;
                    _ws?.Dispose();
                }

                await Task.Delay(5000, stoppingToken);
            }
        }

        private async Task SendRegistrationAsync(CancellationToken ct)
        {
            var telemetry = TelemetryProvider.Collect();
            var payload = new
            {
                hostname = Environment.MachineName,
                os = "windows",
                osVersion = Environment.OSVersion.Version.ToString(),
                arch = Environment.Is64BitOperatingSystem ? "x64" : "x86",
                agentVersion = "1.0.0",
                capabilities = new
                {
                    remoteDesktop = true,
                    remoteTerminal = true,
                    fileTransfer = true,
                    processManager = true,
                    serviceManager = true,
                    diagnostics = true
                },
                latestTelemetry = telemetry
            };

            await SendJsonMessageAsync("DEVICE_REGISTER", payload, ct);
        }

        private async Task RunHeartbeatLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested && _ws?.State == WebSocketState.Open)
            {
                await Task.Delay(10000, ct);
                var telemetry = TelemetryProvider.Collect();
                await SendJsonMessageAsync("DEVICE_HEARTBEAT", new { telemetry }, ct);
            }
        }

        private async Task RunDesktopStreamLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                if (_isStreamingDesktop && _ws?.State == WebSocketState.Open)
                {
                    try
                    {
                        byte[] jpegBytes = ScreenCaptureEngine.CaptureDesktopJpeg(quality: 65);

                        // Send as raw binary frame for optimal performance
                        await _ws.SendAsync(
                            new ArraySegment<byte>(jpegBytes),
                            WebSocketMessageType.Binary,
                            true,
                            ct
                        );
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[STREAM] Frame capture error: {ex.Message}");
                    }

                    // ~25 FPS throttle
                    await Task.Delay(40, ct);
                }
                else
                {
                    await Task.Delay(200, ct);
                }
            }
        }

        private async Task RunReceiveLoopAsync(CancellationToken ct)
        {
            var buffer = new byte[65536];

            while (!ct.IsCancellationRequested && _ws?.State == WebSocketState.Open)
            {
                using var ms = new MemoryStream();
                WebSocketReceiveResult result;
                do
                {
                    result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
                    ms.Write(buffer, 0, result.Count);
                } while (!result.EndOfMessage);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    Console.WriteLine("[AGENT] Server sent close frame");
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    string json = Encoding.UTF8.GetString(ms.ToArray());
                    await HandleIncomingMessageAsync(json, ct);
                }
            }
        }

        private async Task HandleIncomingMessageAsync(string json, CancellationToken ct)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                string type = root.TryGetProperty("type", out var t) ? t.GetString() ?? "" : "";

                switch (type)
                {
                    case "FRAME_DESKTOP":
                        _isStreamingDesktop = true;
                        ShowSessionBanner("Admin");
                        Console.WriteLine("[AGENT] Remote desktop streaming initiated by administrator");
                        break;

                    case "SESSION_DISCONNECT":
                        _isStreamingDesktop = false;
                        HideSessionBanner();
                        Console.WriteLine("[AGENT] Remote session terminated");
                        break;

                    case "INPUT_EVENT":
                        if (root.TryGetProperty("payload", out var inputPayload))
                        {
                            HandleInputEvent(inputPayload);
                        }
                        break;

                    case "COMMAND_EXEC_REQ":
                        if (root.TryGetProperty("payload", out var cmdPayload))
                        {
                            string cmdId = cmdPayload.TryGetProperty("commandId", out var cId) ? cId.GetString() ?? "" : Guid.NewGuid().ToString("N")[..8];
                            string command = cmdPayload.TryGetProperty("command", out var cStr) ? cStr.GetString() ?? "" : "";
                            string shell = cmdPayload.TryGetProperty("shell", out var s) ? s.GetString() ?? "powershell" : "powershell";

                            if (!string.IsNullOrWhiteSpace(command))
                            {
                                Console.WriteLine($"[AGENT] Executing admin command: {command}");
                                var cmdResult = await CommandExecutor.ExecuteAsync(cmdId, command, shell);
                                await SendJsonMessageAsync("COMMAND_EXEC_RESP", cmdResult, ct);
                            }
                        }
                        break;

                    case "FILE_LIST_REQ":
                        if (root.TryGetProperty("payload", out var filePayload))
                        {
                            string path = filePayload.TryGetProperty("path", out var pVal) ? pVal.GetString() ?? @"C:\" : @"C:\";
                            var files = FileManagerEngine.ListDirectory(path);
                            await SendJsonMessageAsync("FILE_LIST_RESP", new { path, files }, ct);
                        }
                        else
                        {
                            var files = FileManagerEngine.ListDirectory(@"C:\");
                            await SendJsonMessageAsync("FILE_LIST_RESP", new { path = @"C:\", files }, ct);
                        }
                        break;

                    case "PROCESS_LIST_REQ":
                        var processes = ProcessServiceEngine.GetProcessList();
                        await SendJsonMessageAsync("PROCESS_LIST_RESP", new { processes }, ct);
                        break;

                    case "PROCESS_KILL_REQ":
                        if (root.TryGetProperty("payload", out var killPayload))
                        {
                            if (killPayload.TryGetProperty("pid", out var pidVal))
                            {
                                int pid = pidVal.GetInt32();
                                bool killed = ProcessServiceEngine.KillProcess(pid);
                                Console.WriteLine($"[AGENT] Kill PID {pid} result: {killed}");
                            }
                        }
                        break;

                    case "SERVICE_LIST_REQ":
                        var services = ProcessServiceEngine.GetServiceList();
                        await SendJsonMessageAsync("SERVICE_LIST_RESP", new { services }, ct);
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AGENT] Error processing server message: {ex.Message}");
            }
        }

        private void HandleInputEvent(JsonElement payload)
        {
            string kind = payload.TryGetProperty("kind", out var k) ? k.GetString() ?? "" : "";

            switch (kind)
            {
                case "mousemove":
                    if (payload.TryGetProperty("x", out var x) && payload.TryGetProperty("y", out var y))
                    {
                        InputInjector.InjectMouseMove(x.GetDouble(), y.GetDouble());
                    }
                    break;

                case "mousedown":
                    if (payload.TryGetProperty("button", out var btn))
                    {
                        InputInjector.InjectMouseDown(btn.GetInt32());
                    }
                    break;

                case "mouseup":
                    if (payload.TryGetProperty("button", out var ubtn))
                    {
                        InputInjector.InjectMouseUp(ubtn.GetInt32());
                    }
                    break;

                case "wheel":
                    if (payload.TryGetProperty("deltaY", out var dY))
                    {
                        InputInjector.InjectMouseWheel(dY.GetInt32());
                    }
                    break;

                case "keydown":
                    if (payload.TryGetProperty("keyCode", out var kd))
                    {
                        InputInjector.InjectKeyDown((ushort)kd.GetInt32());
                    }
                    break;

                case "keyup":
                    if (payload.TryGetProperty("keyCode", out var ku))
                    {
                        InputInjector.InjectKeyUp((ushort)ku.GetInt32());
                    }
                    break;
            }
        }

        private void ShowSessionBanner(string adminUser)
        {
            try
            {
                var thread = new Thread(() =>
                {
                    _bannerForm = new SessionBannerForm(adminUser);
                    _bannerForm.OnEmergencyDisconnectRequested = () =>
                    {
                        _isStreamingDesktop = false;
                        _ = SendJsonMessageAsync("SESSION_DISCONNECT", new { reason = "User requested emergency disconnect" }, CancellationToken.None);
                    };
                    Application.Run(_bannerForm);
                });
                thread.SetApartmentState(ApartmentState.STA);
                thread.IsBackground = true;
                thread.Start();
            }
            catch { }
        }

        private void HideSessionBanner()
        {
            try
            {
                _bannerForm?.Invoke((MethodInvoker)(() => _bannerForm.Close()));
                _bannerForm = null;
            }
            catch { }
        }

        private async Task SendJsonMessageAsync(string type, object payload, CancellationToken ct)
        {
            if (_ws?.State != WebSocketState.Open) return;

            var msg = new
            {
                type,
                deviceId = _deviceId,
                payload,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            string json = JsonSerializer.Serialize(msg, _jsonOptions);
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            await _ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, ct);
        }
    }
}
