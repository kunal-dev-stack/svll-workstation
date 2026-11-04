using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using SVLL_IT_Workstation.Models;

namespace SVLL_IT_Workstation;

public class NvrTestResult
{
    public bool PingSuccess { get; set; }
    public long PingLatencyMs { get; set; } = -1;
    public bool HttpPortOpen { get; set; }
    public bool RtspPortOpen { get; set; }
    public bool AuthSuccess { get; set; }
    public int SnapshotByteCount { get; set; }
    public string Message { get; set; } = "";
    public bool OverallSuccess => (PingSuccess || HttpPortOpen) && (AuthSuccess || RtspPortOpen);
}

public static class NvrStreamHelper
{
    private static readonly HttpClient _sharedClient;

    static NvrStreamHelper()
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        };
        _sharedClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(6)
        };
    }

    public static string BuildRtspUrl(NvrDevice device, int channel, StreamQuality? overrideQuality = null)
    {
        var quality = overrideQuality ?? device.Quality;
        string user = Uri.EscapeDataString(device.Username ?? "admin");
        string pass = Uri.EscapeDataString(device.Password ?? "");
        string host = device.CleanHost;
        int rtspPort = device.RtspPort > 0 ? device.RtspPort : 554;
        string creds = string.IsNullOrEmpty(pass) ? user : $"{user}:{pass}";

        if (!string.IsNullOrWhiteSpace(device.CustomRtspTemplate))
        {
            return device.CustomRtspTemplate
                .Replace("{user}", user)
                .Replace("{pass}", pass)
                .Replace("{host}", host)
                .Replace("{port}", rtspPort.ToString())
                .Replace("{rtspPort}", rtspPort.ToString())
                .Replace("{httpPort}", device.HttpPort.ToString())
                .Replace("{channel}", channel.ToString())
                .Replace("{subtype}", quality == StreamQuality.SubStream ? "1" : "0");
        }

        switch (device.Brand)
        {
            case NvrBrand.Hikvision:
                // Hikvision: channel 1 main = 101, sub = 102; channel 2 main = 201, sub = 202
                int subNum = quality == StreamQuality.SubStream ? 2 : 1;
                return $"rtsp://{creds}@{host}:{rtspPort}/Streaming/Channels/{channel}0{subNum}";

            case NvrBrand.CpPlus:
            case NvrBrand.Dahua:
                // Dahua / CP PLUS: subtype=0 (main), subtype=1 (extra/sub)
                int subtype = quality == StreamQuality.SubStream ? 1 : 0;
                return $"rtsp://{creds}@{host}:{rtspPort}/cam/realmonitor?channel={channel}&subtype={subtype}";

            case NvrBrand.Uniview:
                // Uniview: s0 (main), s1 (sub)
                string unvStream = quality == StreamQuality.SubStream ? "s1" : "s0";
                return $"rtsp://{creds}@{host}:{rtspPort}/unicast/c{channel}/{unvStream}/live";

            case NvrBrand.GenericRtsp:
            default:
                int genericSub = quality == StreamQuality.SubStream ? 2 : 1;
                return $"rtsp://{creds}@{host}:{rtspPort}/h264Preview_{channel:D2}_{(quality == StreamQuality.SubStream ? "sub" : "main")}";
        }
    }

    public static string BuildRtspPlaybackUrl(NvrDevice device, int channel, DateTime startTime, DateTime endTime, float scale = 1.0f)
    {
        string user = Uri.EscapeDataString(device.Username ?? "admin");
        string pass = Uri.EscapeDataString(device.Password ?? "");
        string host = device.CleanHost;
        int rtspPort = device.RtspPort > 0 ? device.RtspPort : 554;
        string creds = string.IsNullOrEmpty(pass) ? user : $"{user}:{pass}";

        string scaleStr = Math.Abs(scale - 1.0f) > 0.01f ? $"&scale={scale:0.0}" : "";
        string dahuaRateStr = Math.Abs(scale - 1.0f) > 0.01f ? $"&rate={scale:0.0}" : "";

        switch (device.Brand)
        {
            case NvrBrand.Hikvision:
                // Format: YYYYMMDDTHHmmssZ
                string hikStart = startTime.ToUniversalTime().ToString("yyyyMMddTHHmmssZ");
                string hikEnd = endTime.ToUniversalTime().ToString("yyyyMMddTHHmmssZ");
                return $"rtsp://{creds}@{host}:{rtspPort}/Streaming/tracks/{channel}01?starttime={hikStart}&endtime={hikEnd}{scaleStr}";

            case NvrBrand.CpPlus:
            case NvrBrand.Dahua:
                // Format: YYYY_MM_DD_HH_mm_ss
                string dahuaStart = startTime.ToString("yyyy_MM_dd_HH_mm_ss");
                string dahuaEnd = endTime.ToString("yyyy_MM_dd_HH_mm_ss");
                return $"rtsp://{creds}@{host}:{rtspPort}/cam/playback?channel={channel}&starttime={dahuaStart}&endtime={dahuaEnd}{dahuaRateStr}";

            case NvrBrand.Uniview:
                string unvStart = startTime.ToUniversalTime().ToString("yyyyMMddTHHmmssZ");
                return $"rtsp://{creds}@{host}:{rtspPort}/unicast/c{channel}/s0/playback?starttime={unvStart}{scaleStr}";

            case NvrBrand.GenericRtsp:
            default:
                string genStart = startTime.ToString("yyyyMMddHHmmss");
                return $"rtsp://{creds}@{host}:{rtspPort}/playback?channel={channel}&starttime={genStart}{scaleStr}";
        }
    }

    public static string BuildWebPlaybackUrl(NvrDevice device, int channel, DateTime date)
    {
        string host = device.Host?.Trim() ?? "127.0.0.1";
        int port = device.HttpPort > 0 ? device.HttpPort : 80;
        return $"http://{host}:{port}/#playback?channel={channel}&date={date:yyyy-MM-dd}";
    }

    public static string BuildSnapshotUrl(NvrDevice device, int channel)
    {
        string host = device.CleanHost;
        int httpPort = device.HttpPort > 0 ? device.HttpPort : 80;

        if (!string.IsNullOrWhiteSpace(device.CustomSnapshotTemplate))
        {
            return device.CustomSnapshotTemplate
                .Replace("{host}", host)
                .Replace("{httpPort}", httpPort.ToString())
                .Replace("{port}", httpPort.ToString())
                .Replace("{channel}", channel.ToString())
                .Replace("{user}", Uri.EscapeDataString(device.Username ?? ""))
                .Replace("{pass}", Uri.EscapeDataString(device.Password ?? ""));
        }

        switch (device.Brand)
        {
            case NvrBrand.Hikvision:
                return $"http://{host}:{httpPort}/ISAPI/Streaming/channels/{channel}01/picture";

            case NvrBrand.CpPlus:
            case NvrBrand.Dahua:
                return $"http://{host}:{httpPort}/cgi-bin/snapshot.cgi?channel={channel}";

            case NvrBrand.Uniview:
                return $"http://{host}:{httpPort}/images/snapshot.jpg?channel={channel}";

            case NvrBrand.GenericRtsp:
            default:
                return $"http://{host}:{httpPort}/snapshot.jpg?channel={channel}";
        }
    }

    public static string BuildWebPortalUrl(NvrDevice device)
    {
        if (!string.IsNullOrWhiteSpace(device.WebLink) && (device.WebLink.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || device.WebLink.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
        {
            return device.WebLink;
        }
        string host = device.CleanHost;
        int port = device.HttpPort > 0 ? device.HttpPort : 80;
        return $"http://{host}:{port}/";
    }

    public static async Task<byte[]?> FetchSnapshotBytesAsync(NvrDevice device, int channel, CancellationToken ct = default)
    {
        string primaryUrl = BuildSnapshotUrl(device, channel);
        byte[]? result = await FetchWithAuthAsync(primaryUrl, device.Username, device.Password, ct);
        if (result != null && result.Length > 200) return result;

        // Fallback URLs based on brand
        if (device.Brand == NvrBrand.Hikvision)
        {
            // Alternative Hikvision snapshot endpoints
            string alt1 = $"http://{device.CleanHost}:{device.HttpPort}/Streaming/Channels/{channel}01/picture";
            result = await FetchWithAuthAsync(alt1, device.Username, device.Password, ct);
            if (result != null && result.Length > 200) return result;

            string alt2 = $"http://{device.CleanHost}:{device.HttpPort}/ISAPI/Streaming/channels/{channel}/picture";
            result = await FetchWithAuthAsync(alt2, device.Username, device.Password, ct);
            if (result != null && result.Length > 200) return result;
        }
        else if (device.Brand == NvrBrand.CpPlus || device.Brand == NvrBrand.Dahua)
        {
            string alt1 = $"http://{device.CleanHost}:{device.HttpPort}/cgi-bin/snapshot.cgi?chn={channel}";
            result = await FetchWithAuthAsync(alt1, device.Username, device.Password, ct);
            if (result != null && result.Length > 200) return result;
        }

        return result;
    }

    private static async Task<byte[]?> FetchWithAuthAsync(string url, string username, string password, CancellationToken ct)
    {
        try
        {
            var handler = new HttpClientHandler
            {
                Credentials = new NetworkCredential(username, password),
                PreAuthenticate = true,
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };

            using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(5) };

            // Add standard Basic Auth header directly as proactive auth for devices that don't challenge
            if (!string.IsNullOrEmpty(username))
            {
                var credBytes = System.Text.Encoding.ASCII.GetBytes($"{username}:{password}");
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(credBytes));
            }

            var response = await client.GetAsync(url, ct);
            if (response.IsSuccessStatusCode)
            {
                var bytes = await response.Content.ReadAsByteArrayAsync(ct);
                if (bytes != null && bytes.Length > 100)
                {
                    return bytes;
                }
            }
        }
        catch { }

        return null;
    }

    public static async Task<NvrTestResult> TestConnectivityAsync(NvrDevice device)
    {
        var result = new NvrTestResult();
        string host = device.CleanHost;

        if (string.IsNullOrEmpty(host))
        {
            result.Message = "Error: Host / IP Address is empty.";
            return result;
        }

        // 1. ICMP Ping Test
        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync(host, 1500);
            if (reply.Status == IPStatus.Success)
            {
                result.PingSuccess = true;
                result.PingLatencyMs = reply.RoundtripTime;
            }
        }
        catch { }

        // 2. TCP Port Tests (HTTP Port & RTSP Port)
        int httpPort = device.HttpPort > 0 ? device.HttpPort : 80;
        int rtspPort = device.RtspPort > 0 ? device.RtspPort : 554;

        result.HttpPortOpen = await CheckTcpPortAsync(host, httpPort, 2000);
        result.RtspPortOpen = await CheckTcpPortAsync(host, rtspPort, 2000);

        // 3. HTTP Auth & Snapshot Test
        try
        {
            using var cts = new CancellationTokenSource(4000);
            var snapshot = await FetchSnapshotBytesAsync(device, 1, cts.Token);
            if (snapshot != null && snapshot.Length > 200)
            {
                result.AuthSuccess = true;
                result.SnapshotByteCount = snapshot.Length;
            }
        }
        catch { }

        // Formulate informative status message
        if (result.AuthSuccess)
        {
            result.Message = $"SUCCESS: Connected to {device.Brand} NVR! Ping: {result.PingLatencyMs}ms, HTTP :{httpPort} (Open), RTSP :{rtspPort} ({(result.RtspPortOpen ? "Open" : "Closed")}), Snapshot: {result.SnapshotByteCount / 1024} KB received.";
        }
        else if (result.RtspPortOpen && result.HttpPortOpen)
        {
            result.Message = $"PORTS OPEN: Both HTTP :{httpPort} and RTSP :{rtspPort} open. (Snapshot auth pending or credentials required for HTTP).";
        }
        else if (result.RtspPortOpen)
        {
            result.Message = $"RTSP READY: Port :{rtspPort} is OPEN and streaming. (HTTP port :{httpPort} not responding).";
        }
        else if (result.PingSuccess)
        {
            result.Message = $"HOST REACHABLE: Ping {result.PingLatencyMs}ms OK, but ports HTTP :{httpPort} & RTSP :{rtspPort} are not responding. Check firewall/NVR IP.";
        }
        else
        {
            result.Message = $"UNREACHABLE: No ping or port response from {host}. Verify device is powered on and reachable on the branch LAN/VPN.";
        }

        return result;
    }

    private static async Task<bool> CheckTcpPortAsync(string host, int port, int timeoutMs)
    {
        try
        {
            using var client = new TcpClient();
            var connectTask = client.ConnectAsync(host, port);
            var delayTask = Task.Delay(timeoutMs);
            var completed = await Task.WhenAny(connectTask, delayTask);
            return completed == connectTask && client.Connected;
        }
        catch
        {
            return false;
        }
    }

    public static bool LaunchInVlc(string rtspUrl, out string statusMessage)
    {
        string[] candidates = new[]
        {
            @"C:\Program Files\VideoLAN\VLC\vlc.exe",
            @"C:\Program Files (x86)\VideoLAN\VLC\vlc.exe",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"VideoLAN\VLC\vlc.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"VideoLAN\VLC\vlc.exe")
        };

        string? vlcPath = null;
        foreach (var c in candidates)
        {
            if (File.Exists(c))
            {
                vlcPath = c;
                break;
            }
        }

        if (vlcPath != null)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = vlcPath,
                    Arguments = $"\"{rtspUrl}\" --network-caching=300 --no-video-title-show",
                    UseShellExecute = true
                });
                statusMessage = "Launched stream in VLC Media Player.";
                return true;
            }
            catch (Exception ex)
            {
                statusMessage = $"Failed to start VLC: {ex.Message}";
                return false;
            }
        }

        // Fallback: Copy RTSP URL to clipboard and notify
        try
        {
            System.Windows.Clipboard.SetText(rtspUrl);
            statusMessage = "VLC is not installed. Stream RTSP URL copied to clipboard! (You can paste it into VLC, OBS, or SmartPSS).";
            return false;
        }
        catch
        {
            statusMessage = "RTSP Stream: " + rtspUrl;
            return false;
        }
    }
}

