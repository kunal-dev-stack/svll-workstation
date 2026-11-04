// =============================================================================
// SVLL IT Workstation — CCTV & NVR Diagnostic & Troubleshooting Service
// Enterprise Surveillance Health Monitoring, Network Diagnostics & Report Generator
// Researched for Enterprise VMS Compliance, AMC Audits & SLA Verification
// =============================================================================
#if BUILD_ADMIN

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SVLL_IT_Workstation.Models;

namespace SVLL_IT_Workstation;

public enum CameraHealthStatus
{
    Online,
    Degraded,
    Offline,
    AuthFailed,
    Checking
}

public class CameraDiagnosticItem
{
    public string NvrId { get; set; } = "";
    public string NvrName { get; set; } = "";
    public string Location { get; set; } = "";
    public string SubLocation { get; set; } = "";
    public string OwnerName { get; set; } = "";
    public string OwnerContact { get; set; } = "";
    public int PlaybackDays { get; set; } = 30;
    public string Host { get; set; } = "";
    public string BrandDisplayName { get; set; } = "";
    public int ChannelNumber { get; set; } = 1;
    public string ChannelName { get; set; } = "";
    public int HttpPort { get; set; } = 80;
    public int RtspPort { get; set; } = 554;
    public CameraHealthStatus Status { get; set; } = CameraHealthStatus.Checking;
    public long LatencyMs { get; set; } = -1;
    public bool PingSuccess { get; set; }
    public bool HttpPortOpen { get; set; }
    public bool RtspPortOpen { get; set; }
    public bool AuthSuccess { get; set; }
    public int SnapshotBytes { get; set; }
    public string RtspUrl { get; set; } = "";
    public string ErrorDetails { get; set; } = "";
    public string CorrectiveAction { get; set; } = "";
    public DateTime LastAudited { get; set; } = DateTime.Now;

    public string StatusBadgeText => Status switch
    {
        CameraHealthStatus.Online => "🟢 ONLINE",
        CameraHealthStatus.Degraded => "🟡 DEGRADED",
        CameraHealthStatus.AuthFailed => "🔒 AUTH ERROR",
        CameraHealthStatus.Offline => "🔴 OFFLINE",
        _ => "⚪ CHECKING"
    };

    public string StatusColorHex => Status switch
    {
        CameraHealthStatus.Online => "#16A34A",
        CameraHealthStatus.Degraded => "#D97706",
        CameraHealthStatus.AuthFailed => "#9333EA",
        CameraHealthStatus.Offline => "#DC2626",
        _ => "#64748B"
    };
}

public class FleetDiagnosticReport
{
    public DateTime GeneratedAt { get; set; } = DateTime.Now;
    public string AuditorName { get; set; } = "SVLL NOC Surveillance Engineer";
    public int TotalNvrs { get; set; }
    public int TotalChannels { get; set; }
    public int OnlineCount { get; set; }
    public int DegradedCount { get; set; }
    public int OfflineCount { get; set; }
    public int AuthFailedCount { get; set; }
    public double AvailabilityPercent => TotalChannels > 0 ? Math.Round((double)OnlineCount / TotalChannels * 100.0, 1) : 0.0;
    public double AverageLatencyMs { get; set; }
    public string HealthGrade => AvailabilityPercent switch
    {
        >= 95.0 => "GRADE A+ (OPTIMAL SURVEILLANCE)",
        >= 85.0 => "GRADE B (SATISFACTORY — MINOR FAULTS)",
        >= 70.0 => "GRADE C (ATTENTION REQUIRED)",
        _ => "GRADE D (CRITICAL OUTAGES)"
    };

    public List<CameraDiagnosticItem> Items { get; set; } = new();
    public List<string> HighPriorityActions { get; set; } = new();
    public List<string> MediumPriorityActions { get; set; } = new();
    public List<string> LowPriorityActions { get; set; } = new();
}

public class DiscoveredCamera
{
    public string IpAddress { get; set; } = "";
    public int HttpPort { get; set; } = 80;
    public int RtspPort { get; set; } = 554;
    public bool HttpOpen { get; set; }
    public bool RtspOpen { get; set; }
    public string ProbableBrand { get; set; } = "Generic ONVIF/RTSP";
    public long LatencyMs { get; set; } = -1;
}

public static class CctvDiagnosticService
{
    public static async Task<FleetDiagnosticReport> AuditAllDevicesAsync(
        IProgress<(int current, int total, string message)>? progress = null,
        CancellationToken ct = default)
    {
        var devices = NvrDeviceStorage.Instance.GetDevices();
        var report = new FleetDiagnosticReport
        {
            GeneratedAt = DateTime.Now,
            TotalNvrs = devices.Count
        };

        int totalExpectedChannels = devices.Sum(d => d.ChannelCount);
        report.TotalChannels = totalExpectedChannels;

        int processed = 0;
        var items = new List<CameraDiagnosticItem>();

        foreach (var device in devices)
        {
            if (ct.IsCancellationRequested) break;

            // Audit NVR device reachability
            var nvrResult = await NvrStreamHelper.TestConnectivityAsync(device);
            device.IsOnline = nvrResult.OverallSuccess;
            device.LatencyMs = nvrResult.PingLatencyMs;
            device.LastChecked = DateTime.Now;

            int channelsToAudit = Math.Max(1, device.ChannelCount);
            for (int ch = 1; ch <= channelsToAudit; ch++)
            {
                if (ct.IsCancellationRequested) break;
                processed++;
                string chName = device.GetChannelName(ch);
                progress?.Report((processed, totalExpectedChannels, $"Auditing {device.Name} — Ch {ch:D2} ({chName})..."));

                var item = await AuditSingleCameraInternalAsync(device, ch, nvrResult, ct);
                items.Add(item);
            }
        }

        report.Items = items;
        report.OnlineCount = items.Count(i => i.Status == CameraHealthStatus.Online);
        report.DegradedCount = items.Count(i => i.Status == CameraHealthStatus.Degraded);
        report.OfflineCount = items.Count(i => i.Status == CameraHealthStatus.Offline);
        report.AuthFailedCount = items.Count(i => i.Status == CameraHealthStatus.AuthFailed);

        var validLatencies = items.Where(i => i.LatencyMs > 0).Select(i => i.LatencyMs).ToList();
        report.AverageLatencyMs = validLatencies.Count > 0 ? Math.Round(validLatencies.Average(), 1) : 0.0;

        // Generate Prioritized Corrective Action Plan (CAP)
        foreach (var item in items)
        {
            if (item.Status == CameraHealthStatus.Offline)
            {
                report.HighPriorityActions.Add($"[OFFLINE] {item.NvrName} — CH {item.ChannelNumber:D2} ({item.ChannelName}): Host {item.Host} unreachable. {item.CorrectiveAction}");
            }
            else if (item.Status == CameraHealthStatus.AuthFailed)
            {
                report.HighPriorityActions.Add($"[AUTH FAULT] {item.NvrName} — CH {item.ChannelNumber:D2} ({item.ChannelName}): Login credentials rejected. {item.CorrectiveAction}");
            }
            else if (item.Status == CameraHealthStatus.Degraded)
            {
                report.MediumPriorityActions.Add($"[DEGRADED] {item.NvrName} — CH {item.ChannelNumber:D2} ({item.ChannelName}): High latency ({item.LatencyMs}ms). {item.CorrectiveAction}");
            }
        }

        if (report.Items.Count > 0 && report.Items.All(i => i.Status == CameraHealthStatus.Online))
        {
            report.LowPriorityActions.Add("All surveillance feeds operating within normal parameters. Perform routine lens cleaning during next scheduled maintenance window.");
        }

        return report;
    }

    public static async Task<CameraDiagnosticItem> AuditSingleCameraAsync(NvrDevice device, int channel, CancellationToken ct = default)
    {
        var nvrResult = await NvrStreamHelper.TestConnectivityAsync(device);
        return await AuditSingleCameraInternalAsync(device, channel, nvrResult, ct);
    }

    private static async Task<CameraDiagnosticItem> AuditSingleCameraInternalAsync(
        NvrDevice device,
        int channel,
        NvrTestResult nvrTest,
        CancellationToken ct)
    {
        var item = new CameraDiagnosticItem
        {
            NvrId = device.Id,
            NvrName = device.Name,
            Location = device.Location,
            SubLocation = device.SubLocation,
            OwnerName = device.OwnerName,
            OwnerContact = device.OwnerContact,
            PlaybackDays = device.PlaybackDays,
            Host = device.CleanHost,
            BrandDisplayName = device.GetBrandDisplayName(),
            ChannelNumber = channel,
            ChannelName = device.GetChannelName(channel),
            HttpPort = device.HttpPort > 0 ? device.HttpPort : 80,
            RtspPort = device.RtspPort > 0 ? device.RtspPort : 554,
            PingSuccess = nvrTest.PingSuccess,
            LatencyMs = nvrTest.PingLatencyMs,
            HttpPortOpen = nvrTest.HttpPortOpen,
            RtspPortOpen = nvrTest.RtspPortOpen,
            RtspUrl = NvrStreamHelper.BuildRtspUrl(device, channel, StreamQuality.SubStream),
            LastAudited = DateTime.Now
        };

        if (!item.PingSuccess && !item.HttpPortOpen && !item.RtspPortOpen)
        {
            item.Status = CameraHealthStatus.Offline;
            item.ErrorDetails = $"No network response from {item.Host}. Device is unreachable.";
            item.CorrectiveAction = "1. Verify NVR power and Ethernet patch lead.\n2. Verify branch switch port link LED.\n3. Check if IP address changed on DHCP.";
            return item;
        }

        // Test snapshot channel retrieval to verify camera feed exists and auth works
        try
        {
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, ct);
            var snapshot = await NvrStreamHelper.FetchSnapshotBytesAsync(device, channel, linkedCts.Token);
            if (snapshot != null && snapshot.Length > 250)
            {
                item.AuthSuccess = true;
                item.SnapshotBytes = snapshot.Length;
            }
        }
        catch { }

        // Determine Final Health Status
        if (item.RtspPortOpen && item.AuthSuccess)
        {
            if (item.LatencyMs > 180)
            {
                item.Status = CameraHealthStatus.Degraded;
                item.ErrorDetails = $"High network latency ({item.LatencyMs}ms). Potential video stutter or frame loss.";
                item.CorrectiveAction = "Inspect branch LAN bandwidth utilization or wireless bridge signal quality.";
            }
            else
            {
                item.Status = CameraHealthStatus.Online;
                item.ErrorDetails = "Stream and snapshot responsive.";
                item.CorrectiveAction = "No corrective action required. Feed healthy.";
            }
        }
        else if (item.RtspPortOpen && !item.AuthSuccess)
        {
            if (nvrTest.PingSuccess && item.HttpPortOpen)
            {
                item.Status = CameraHealthStatus.AuthFailed;
                item.ErrorDetails = "RTSP & HTTP ports are open, but snapshot/stream authentication failed (401/403).";
                item.CorrectiveAction = "Update or re-verify NVR admin credentials in the Surveillance Center settings.";
            }
            else
            {
                item.Status = CameraHealthStatus.Online;
                item.ErrorDetails = "RTSP port active. Snapshot API unavailable or secured.";
                item.CorrectiveAction = "RTSP streaming operational.";
            }
        }
        else if (item.PingSuccess && !item.RtspPortOpen)
        {
            item.Status = CameraHealthStatus.Degraded;
            item.ErrorDetails = $"Host responds to ICMP Ping ({item.LatencyMs}ms), but RTSP port :{item.RtspPort} is closed.";
            item.CorrectiveAction = $"Ensure RTSP service is enabled on port {item.RtspPort} in the NVR network configuration.";
        }
        else
        {
            item.Status = CameraHealthStatus.Offline;
            item.ErrorDetails = "Camera feed not responding to RTSP or HTTP probes.";
            item.CorrectiveAction = "Check camera power supply, PoE injector, or switch port.";
        }

        return item;
    }

    public static async Task<List<DiscoveredCamera>> ScanSubnetAsync(
        string subnetBase,
        int startIp,
        int endIp,
        IProgress<int>? progress = null,
        CancellationToken ct = default)
    {
        var discovered = new List<DiscoveredCamera>();
        int total = endIp - startIp + 1;
        int current = 0;

        var tasks = new List<Task>();
        var throttler = new SemaphoreSlim(25); // Concurrency cap

        for (int i = startIp; i <= endIp; i++)
        {
            if (ct.IsCancellationRequested) break;
            string targetIp = $"{subnetBase}.{i}";

            tasks.Add(Task.Run(async () =>
            {
                await throttler.WaitAsync();
                try
                {
                    if (ct.IsCancellationRequested) return;

                    using var ping = new Ping();
                    var reply = await ping.SendPingAsync(targetIp, 800);
                    if (reply.Status == IPStatus.Success)
                    {
                        bool rtspOpen = await CheckTcpPortAsync(targetIp, 554, 1000);
                        bool httpOpen = await CheckTcpPortAsync(targetIp, 80, 1000);
                        bool altPortOpen = !rtspOpen && await CheckTcpPortAsync(targetIp, 8000, 1000);

                        if (rtspOpen || httpOpen || altPortOpen)
                        {
                            string brand = "Generic ONVIF/RTSP";
                            if (altPortOpen) brand = "Hikvision / HiLook (Port 8000)";
                            else if (await CheckTcpPortAsync(targetIp, 37777, 600)) brand = "Dahua / CP PLUS (Port 37777)";

                            lock (discovered)
                            {
                                discovered.Add(new DiscoveredCamera
                                {
                                    IpAddress = targetIp,
                                    HttpPort = httpOpen ? 80 : (altPortOpen ? 8000 : 80),
                                    RtspPort = 554,
                                    HttpOpen = httpOpen,
                                    RtspOpen = rtspOpen,
                                    ProbableBrand = brand,
                                    LatencyMs = reply.RoundtripTime
                                });
                            }
                        }
                    }
                }
                catch { }
                finally
                {
                    throttler.Release();
                    int c = Interlocked.Increment(ref current);
                    progress?.Report((int)((double)c / total * 100));
                }
            }));
        }

        await Task.WhenAll(tasks);
        return discovered.OrderBy(d => d.IpAddress).ToList();
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

    // ─── REPORT GENERATION ENGINE (HTML / CSV / MARKDOWN) ───────────────────

    public static string GenerateHtmlReport(FleetDiagnosticReport report)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("  <meta charset=\"UTF-8\">");
        sb.AppendLine("  <title>SVLL Surveillance Infrastructure Health Check & Diagnostic Audit</title>");
        sb.AppendLine("  <style>");
        sb.AppendLine("    body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; background-color: #F8FAFC; color: #0F172A; margin: 0; padding: 24px; line-height: 1.5; }");
        sb.AppendLine("    .container { max-width: 1200px; margin: 0 auto; background: #FFFFFF; border: 1px solid #E2E8F0; border-radius: 12px; box-shadow: 0 4px 6px -1px rgba(0,0,0,0.05); overflow: hidden; }");
        sb.AppendLine("    .header { background: linear-gradient(135deg, #1A4BB2 0%, #0F172A 100%); color: #FFFFFF; padding: 28px 32px; border-bottom: 3px solid #3B82F6; }");
        sb.AppendLine("    .header h1 { margin: 0 0 6px 0; font-size: 24px; font-weight: 800; letter-spacing: -0.5px; }");
        sb.AppendLine("    .header p { margin: 0; font-size: 13px; color: #93C5FD; }");
        sb.AppendLine("    .kpi-row { display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 16px; padding: 24px 32px; background: #F1F5F9; border-bottom: 1px solid #E2E8F0; }");
        sb.AppendLine("    .kpi-card { background: #FFFFFF; padding: 18px 20px; border-radius: 8px; border: 1px solid #E2E8F0; }");
        sb.AppendLine("    .kpi-title { font-size: 11px; font-weight: 700; color: #64748B; text-transform: uppercase; letter-spacing: 0.5px; }");
        sb.AppendLine("    .kpi-value { font-size: 28px; font-weight: 800; color: #1A4BB2; margin: 4px 0 0 0; }");
        sb.AppendLine("    .kpi-sub { font-size: 11.5px; color: #475569; margin-top: 4px; }");
        sb.AppendLine("    .section { padding: 28px 32px; border-bottom: 1px solid #E2E8F0; }");
        sb.AppendLine("    .section-title { font-size: 16px; font-weight: 700; color: #1E293B; margin: 0 0 16px 0; display: flex; align-items: center; gap: 8px; }");
        sb.AppendLine("    table { width: 100%; border-collapse: collapse; font-size: 12.5px; text-align: left; }");
        sb.AppendLine("    th { background: #F8FAFC; color: #475569; font-weight: 700; padding: 12px 14px; border-bottom: 2px solid #CBD5E1; }");
        sb.AppendLine("    td { padding: 12px 14px; border-bottom: 1px solid #E2E8F0; vertical-align: middle; }");
        sb.AppendLine("    tr:nth-child(even) { background-color: #F8FAFC; }");
        sb.AppendLine("    .badge { display: inline-block; padding: 3px 8px; border-radius: 9999px; font-size: 10.5px; font-weight: 700; text-transform: uppercase; }");
        sb.AppendLine("    .badge-online { background: #DCFCE7; color: #166534; }");
        sb.AppendLine("    .badge-degraded { background: #FEF3C7; color: #92400E; }");
        sb.AppendLine("    .badge-offline { background: #FEE2E2; color: #991B1B; }");
        sb.AppendLine("    .badge-auth { background: #F3E8FF; color: #6B21A8; }");
        sb.AppendLine("    .cap-list { list-style: none; padding: 0; margin: 0; }");
        sb.AppendLine("    .cap-item { padding: 10px 14px; margin-bottom: 8px; border-radius: 6px; font-size: 12.5px; }");
        sb.AppendLine("    .cap-high { background: #FEF2F2; border-left: 4px solid #EF4444; color: #991B1B; }");
        sb.AppendLine("    .cap-med { background: #FFFBEB; border-left: 4px solid #F59E0B; color: #92400E; }");
        sb.AppendLine("    .cap-low { background: #F0FDF4; border-left: 4px solid #10B981; color: #166534; }");
        sb.AppendLine("    .footer { padding: 20px 32px; background: #F8FAFC; color: #64748B; font-size: 11px; display: flex; justify-content: space-between; align-items: center; }");
        sb.AppendLine("    @media print { body { background: #FFF; padding: 0; } .container { border: none; box-shadow: none; } .no-print { display: none; } }");
        sb.AppendLine("  </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("  <div class=\"container\">");
        sb.AppendLine("    <div class=\"header\">");
        sb.AppendLine("      <div style=\"float: right; text-align: right;\">");
        sb.AppendLine($"        <span style=\"display: inline-block; background: rgba(255,255,255,0.2); padding: 4px 12px; border-radius: 6px; font-weight: 800; font-size: 12px;\">{report.HealthGrade}</span><br>");
        sb.AppendLine($"        <small style=\"color: #CBD5E1;\">Audit Date: {report.GeneratedAt:yyyy-MM-dd HH:mm:ss}</small>");
        sb.AppendLine("      </div>");
        sb.AppendLine("      <h1>SHREE VASU LOGISTICS LIMITED</h1>");
        sb.AppendLine("      <p>CCTV Surveillance Infrastructure Health Check, Network Diagnostics & Corrective Action Audit</p>");
        sb.AppendLine("    </div>");

        // KPI Row
        sb.AppendLine("    <div class=\"kpi-row\">");
        sb.AppendLine("      <div class=\"kpi-card\">");
        sb.AppendLine("        <div class=\"kpi-title\">Fleet Availability</div>");
        sb.AppendLine($"        <div class=\"kpi-value\" style=\"color: {(report.AvailabilityPercent >= 90 ? "#16A34A" : "#DC2626")};\">{report.AvailabilityPercent}%</div>");
        sb.AppendLine($"        <div class=\"kpi-sub\">{report.OnlineCount} Online / {report.TotalChannels} Monitored</div>");
        sb.AppendLine("      </div>");
        sb.AppendLine("      <div class=\"kpi-card\">");
        sb.AppendLine("        <div class=\"kpi-title\">Surveillance Hubs</div>");
        sb.AppendLine($"        <div class=\"kpi-value\">{report.TotalNvrs}</div>");
        sb.AppendLine("        <div class=\"kpi-sub\">Active NVRs / DVRs Registered</div>");
        sb.AppendLine("      </div>");
        sb.AppendLine("      <div class=\"kpi-card\">");
        sb.AppendLine("        <div class=\"kpi-title\">Avg Network Latency</div>");
        sb.AppendLine($"        <div class=\"kpi-value\" style=\"color: {(report.AverageLatencyMs < 60 ? "#1A4BB2" : "#D97706")};\">{report.AverageLatencyMs} ms</div>");
        sb.AppendLine("        <div class=\"kpi-sub\">LAN/WAN ICMP Roundtrip</div>");
        sb.AppendLine("      </div>");
        sb.AppendLine("      <div class=\"kpi-card\">");
        sb.AppendLine("        <div class=\"kpi-title\">Action Items</div>");
        sb.AppendLine($"        <div class=\"kpi-value\" style=\"color: {(report.OfflineCount + report.AuthFailedCount > 0 ? "#DC2626" : "#16A34A")};\">{report.OfflineCount + report.AuthFailedCount}</div>");
        sb.AppendLine("        <div class=\"kpi-sub\">Critical Faults Requiring Intervention</div>");
        sb.AppendLine("      </div>");
        sb.AppendLine("    </div>");

        // Section: Corrective Action Plan (CAP)
        if (report.HighPriorityActions.Count > 0 || report.MediumPriorityActions.Count > 0 || report.LowPriorityActions.Count > 0)
        {
            sb.AppendLine("    <div class=\"section\">");
            sb.AppendLine("      <h2 class=\"section-title\">🛠️ Corrective Action Plan (CAP) & Resolution Directives</h2>");
            sb.AppendLine("      <ul class=\"cap-list\">");
            foreach (var h in report.HighPriorityActions)
            {
                sb.AppendLine($"        <li class=\"cap-item cap-high\"><strong>HIGH PRIORITY:</strong> {System.Net.WebUtility.HtmlEncode(h)}</li>");
            }
            foreach (var m in report.MediumPriorityActions)
            {
                sb.AppendLine($"        <li class=\"cap-item cap-med\"><strong>MEDIUM PRIORITY:</strong> {System.Net.WebUtility.HtmlEncode(m)}</li>");
            }
            foreach (var l in report.LowPriorityActions)
            {
                sb.AppendLine($"        <li class=\"cap-item cap-low\"><strong>ROUTINE:</strong> {System.Net.WebUtility.HtmlEncode(l)}</li>");
            }
            sb.AppendLine("      </ul>");
            sb.AppendLine("    </div>");
        }

        // Section: Detailed Channel Matrix
        sb.AppendLine("    <div class=\"section\">");
        sb.AppendLine("      <h2 class=\"section-title\">📹 Channel-by-Channel Technical Diagnostic Matrix</h2>");
        sb.AppendLine("      <table>");
        sb.AppendLine("        <thead>");
        sb.AppendLine("          <tr>");
        sb.AppendLine("            <th>Status</th>");
        sb.AppendLine("            <th>NVR Hub & Location</th>");
        sb.AppendLine("            <th>Ch #</th>");
        sb.AppendLine("            <th>Camera Identifier</th>");
        sb.AppendLine("            <th>IP Address : Port</th>");
        sb.AppendLine("            <th>Hardware Profile</th>");
        sb.AppendLine("            <th>Latency</th>");
        sb.AppendLine("            <th>RTSP / Snapshot Probes</th>");
        sb.AppendLine("            <th>Diagnostics / Actions</th>");
        sb.AppendLine("          </tr>");
        sb.AppendLine("        </thead>");
        sb.AppendLine("        <tbody>");

        foreach (var item in report.Items)
        {
            string badgeClass = item.Status switch
            {
                CameraHealthStatus.Online => "badge-online",
                CameraHealthStatus.Degraded => "badge-degraded",
                CameraHealthStatus.AuthFailed => "badge-auth",
                _ => "badge-offline"
            };

            sb.AppendLine("          <tr>");
            sb.AppendLine($"            <td><span class=\"badge {badgeClass}\">{item.StatusBadgeText}</span></td>");
            sb.AppendLine($"            <td><strong>{System.Net.WebUtility.HtmlEncode(item.NvrName)}</strong><br><small style=\"color:#64748B;\">{System.Net.WebUtility.HtmlEncode(item.Location)}</small></td>");
            sb.AppendLine($"            <td><strong>Ch {item.ChannelNumber:D2}</strong></td>");
            sb.AppendLine($"            <td>{System.Net.WebUtility.HtmlEncode(item.ChannelName)}</td>");
            sb.AppendLine($"            <td><code>{item.Host}</code><br><small>HTTP:{item.HttpPort} | RTSP:{item.RtspPort}</small></td>");
            sb.AppendLine($"            <td><small>{System.Net.WebUtility.HtmlEncode(item.BrandDisplayName)}</small></td>");
            sb.AppendLine($"            <td>{(item.LatencyMs >= 0 ? $"{item.LatencyMs} ms" : "Timeout")}</td>");
            sb.AppendLine($"            <td><small>RTSP: {(item.RtspPortOpen ? "Open" : "Closed")}<br>Snapshot: {(item.AuthSuccess ? $"{item.SnapshotBytes / 1024} KB" : "Failed")}</small></td>");
            sb.AppendLine($"            <td><small style=\"color: #334155;\">{System.Net.WebUtility.HtmlEncode(item.ErrorDetails)}<br><span style=\"color:#1E40AF;\">{System.Net.WebUtility.HtmlEncode(item.CorrectiveAction)}</span></small></td>");
            sb.AppendLine("          </tr>");
        }

        sb.AppendLine("        </tbody>");
        sb.AppendLine("      </table>");
        sb.AppendLine("    </div>");

        // Footer
        sb.AppendLine("    <div class=\"footer\">");
        sb.AppendLine("      <div>SHREE VASU LOGISTICS LIMITED • NOC ENTERPRISE SURVEILLANCE & INFRASTRUCTURE SYSTEM</div>");
        sb.AppendLine($"      <div>Generated: {report.GeneratedAt:yyyy-MM-dd HH:mm:ss} | Confidential IT Document</div>");
        sb.AppendLine("    </div>");
        sb.AppendLine("  </div>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    public static string GenerateCsvReport(FleetDiagnosticReport report)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Timestamp,NVR Name,Location,Host,Channel,Camera Name,Status,Ping Latency (ms),HTTP Port,RTSP Port,Auth Status,Snapshot Size (Bytes),Diagnostics,Corrective Action");

        foreach (var i in report.Items)
        {
            string line = string.Join(",", new[]
            {
                EscapeCsv(report.GeneratedAt.ToString("yyyy-MM-dd HH:mm:ss")),
                EscapeCsv(i.NvrName),
                EscapeCsv(i.Location),
                EscapeCsv(i.Host),
                EscapeCsv(i.ChannelNumber.ToString()),
                EscapeCsv(i.ChannelName),
                EscapeCsv(i.Status.ToString()),
                EscapeCsv(i.LatencyMs.ToString()),
                EscapeCsv(i.HttpPort.ToString()),
                EscapeCsv(i.RtspPort.ToString()),
                EscapeCsv(i.AuthSuccess ? "Authenticated" : "Failed"),
                EscapeCsv(i.SnapshotBytes.ToString()),
                EscapeCsv(i.ErrorDetails),
                EscapeCsv(i.CorrectiveAction)
            });
            sb.AppendLine(line);
        }

        return sb.ToString();
    }

    public static string GenerateMarkdownReport(FleetDiagnosticReport report)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# SVLL CCTV Fleet Diagnostic & Surveillance Audit Report");
        sb.AppendLine($"**Audit Timestamp:** {report.GeneratedAt:yyyy-MM-dd HH:mm:ss}  ");
        sb.AppendLine($"**Overall System Health:** {report.HealthGrade}  ");
        sb.AppendLine($"**Availability:** {report.AvailabilityPercent}% ({report.OnlineCount}/{report.TotalChannels} Online)  ");
        sb.AppendLine($"**Average LAN Latency:** {report.AverageLatencyMs} ms  ");
        sb.AppendLine();

        sb.AppendLine("### Key Summary Statistics");
        sb.AppendLine($"- **Total Monitored NVRs/DVRs:** {report.TotalNvrs}");
        sb.AppendLine($"- **Total Monitored Channels:** {report.TotalChannels}");
        sb.AppendLine($"- **🟢 Online & Verified Feeds:** {report.OnlineCount}");
        sb.AppendLine($"- **🟡 Degraded Feeds (High Latency/Packet Drop):** {report.DegradedCount}");
        sb.AppendLine($"- **🔴 Offline Unreachable Feeds:** {report.OfflineCount}");
        sb.AppendLine($"- **🔒 Credential / Auth Faults:** {report.AuthFailedCount}");
        sb.AppendLine();

        if (report.HighPriorityActions.Count > 0 || report.MediumPriorityActions.Count > 0)
        {
            sb.AppendLine("### Prioritized Corrective Action Items");
            foreach (var a in report.HighPriorityActions) sb.AppendLine($"- 🔴 **High Priority:** {a}");
            foreach (var a in report.MediumPriorityActions) sb.AppendLine($"- 🟡 **Medium Priority:** {a}");
            sb.AppendLine();
        }

        sb.AppendLine("### Channel Diagnostic Matrix");
        sb.AppendLine("| Status | NVR Name | Ch # | Camera Name | IP Host | Latency | RTSP Port | Snapshot | Diagnostics & Actions |");
        sb.AppendLine("| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |");
        foreach (var i in report.Items)
        {
            sb.AppendLine($"| {i.StatusBadgeText} | {i.NvrName} | {i.ChannelNumber:D2} | {i.ChannelName} | `{i.Host}` | {(i.LatencyMs >= 0 ? $"{i.LatencyMs}ms" : "N/A")} | {(i.RtspPortOpen ? "Open" : "Closed")} | {(i.AuthSuccess ? $"{i.SnapshotBytes / 1024}KB" : "No")} | {i.ErrorDetails.Replace("\n", " ")} |");
        }

        return sb.ToString();
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        return value;
    }
}

#endif

