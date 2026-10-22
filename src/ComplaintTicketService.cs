using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.ServiceProcess;
using System.Text.Json;
using SVLL_IT_Workstation.Models;

namespace SVLL_IT_Workstation;

public class ComplaintTicketService
{
    private static ComplaintTicketService? _instance;
    public static ComplaintTicketService Instance => _instance ??= new ComplaintTicketService();

    private readonly string _ticketsFilePath;
    private readonly List<ComplaintTicket> _tickets = new();

    private ComplaintTicketService()
    {
        _ticketsFilePath = Path.Combine(MainWindow.AuditLogDirectory, "complaint_tickets.json");
        LoadTickets();
    }

    public void LoadTickets()
    {
        _tickets.Clear();
        try
        {
            if (File.Exists(_ticketsFilePath))
            {
                string json = File.ReadAllText(_ticketsFilePath);
                var loaded = JsonSerializer.Deserialize<List<ComplaintTicket>>(json);
                if (loaded != null && loaded.Count > 0)
                {
                    _tickets.AddRange(loaded);
                }
            }
        }
        catch { }

        // Also check shared directory if configured
        try
        {
            var sharedDir = AdminAuthService.Instance.GetConfig().SharedTicketDirectory;
            if (!string.IsNullOrEmpty(sharedDir) && Directory.Exists(sharedDir))
            {
                foreach (var file in Directory.GetFiles(sharedDir, "TKT-*.json"))
                {
                    try
                    {
                        string j = File.ReadAllText(file);
                        var t = JsonSerializer.Deserialize<ComplaintTicket>(j);
                        if (t != null && !_tickets.Any(x => x.TicketId == t.TicketId))
                        {
                            _tickets.Add(t);
                        }
                    }
                    catch { }
                }
            }
        }
        catch { }

        // Populate realistic IT helpdesk seed tickets if empty
        if (_tickets.Count == 0)
        {
            PopulateSeedTickets();
        }
    }

    private void PopulateSeedTickets()
    {
        _tickets.Add(new ComplaintTicket
        {
            TicketId = "SVLL-TKT-2026-1001",
            Title = "Zebra ZD421 Thermal Printer Skipping Labels on Dispatch Run",
            Category = "ThermalPrinter",
            Priority = "High",
            Status = "Open",
            CreatedAt = DateTime.Now.AddHours(-3),
            CreatedBy = "warehouse.bom",
            UserContact = "+91 98200 12345 (Ext 102)",
            BranchLocation = "Mumbai Central Hub",
            Hostname = "SVLL-BOM-DISPATCH-01",
            IpAddress = "192.168.10.45",
            AnyDeskId = "948 213 502",
            Description = "Printer prints 1 shipping label then ejects 2 blank labels. Sensor calibration was attempted. Amber light blinks intermittently during high-speed batch printing.",
            AttachedDiagnosticsJson = "{\"Printer\": \"Zebra ZD421-203dpi\", \"Spooler\": \"Running\", \"Port\": \"USB001\", \"QueuedJobs\": 3, \"Driver\": \"ZDesigner ZD421\"}",
            AssignedTechnician = "Kunal Turkar (SVLL IT)",
            AdminNotes = "Requires sensor recalibration via direct ZPL command (^XA~JC^XZ) and gap sensor sensitivity adjustment."
        });

        _tickets.Add(new ComplaintTicket
        {
            TicketId = "SVLL-TKT-2026-1002",
            Title = "Logistics WMS Dispatch ERP Disconnecting Every 10 Minutes",
            Category = "SoftwareWms",
            Priority = "Critical",
            Status = "In Progress",
            CreatedAt = DateTime.Now.AddHours(-1),
            CreatedBy = "inbound.del",
            UserContact = "Ext 405 (Delhi Gate 2)",
            BranchLocation = "Delhi Logistics Park",
            Hostname = "SVLL-DEL-INBOUND-02",
            IpAddress = "192.168.20.18",
            AnyDeskId = "612 849 330",
            Description = "WMS Oracle client session crashes with 'ORA-03113: end-of-file on communication channel'. Inbound truck unloads are currently delayed.",
            AttachedDiagnosticsJson = "{\"Gateway\": \"192.168.20.1 (OK)\", \"DnsLatency\": \"14ms\", \"ErpServer\": \"10.0.1.50 (Dropped 8% packets)\", \"MemoryLoad\": \"58%\"}",
            AssignedTechnician = "Kunal Turkar (SVLL IT)",
            AdminNotes = "Suspected core switch port flap on Port 18. Switch log reviewed; MTU mismatch detected."
        });

        _tickets.Add(new ComplaintTicket
        {
            TicketId = "SVLL-TKT-2026-1003",
            Title = "Need Temporary USB Storage Access for Customs Manifest PDF",
            Category = "UsbDlp",
            Priority = "Medium",
            Status = "Resolved",
            CreatedAt = DateTime.Now.AddDays(-1),
            CreatedBy = "dispatch.rai",
            UserContact = "dispatch.rai@shreevasu.com",
            BranchLocation = "Raipur Main Warehouse",
            Hostname = "SVLL-RAI-DESK-03",
            IpAddress = "192.168.30.52",
            AnyDeskId = "773 901 445",
            Description = "Need to transfer signed bill-of-lading PDFs to Sandisk pen drive for vehicle driver handoff. Workstation currently blocks USB write.",
            AttachedDiagnosticsJson = "{\"UsbPolicy\": \"Read-Only Enforced\", \"Defender\": \"Active\", \"DriveLetter\": \"E:\"}",
            AssignedTechnician = "Kunal Turkar (SVLL IT)",
            AdminNotes = "Granted 4-hour temporary DLP write authorization with audited manifest upload. Restored to Read-Only at 18:00."
        });

        SaveTickets();
    }

    public void SaveTickets()
    {
        try
        {
            if (!Directory.Exists(MainWindow.AuditLogDirectory))
            {
                Directory.CreateDirectory(MainWindow.AuditLogDirectory);
            }
            string json = JsonSerializer.Serialize(_tickets, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_ticketsFilePath, json);
        }
        catch { }
    }

    public List<ComplaintTicket> GetAllTickets() => _tickets.OrderByDescending(t => t.CreatedAt).ToList();

    public List<ComplaintTicket> GetTicketsForHost(string hostname) =>
        _tickets.Where(t => t.Hostname.Equals(hostname, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(t => t.CreatedAt).ToList();

    public string CaptureSystemDiagnosticsJson()
    {
        try
        {
            string host = Environment.MachineName;
            string user = Environment.UserName;
            string os = Environment.OSVersion.ToString();
            string ip = FleetRegistryService.Instance.LocalDevice.IpAddress;
            string anydesk = FleetRegistryService.Instance.LocalDevice.AnyDeskId;

            string spoolerStatus = "Unknown";
            try
            {
                using var sc = new ServiceController("Spooler");
                spoolerStatus = sc.Status.ToString();
            }
            catch { }

            var driveInfoList = new List<string>();
            foreach (var d in DriveInfo.GetDrives().Where(d => d.IsReady))
            {
                long freeGb = d.AvailableFreeSpace / (1024 * 1024 * 1024);
                long totalGb = d.TotalSize / (1024 * 1024 * 1024);
                driveInfoList.Add($"{d.Name} ({freeGb}GB free / {totalGb}GB)");
            }

            var diagObj = new
            {
                Host = host,
                User = user,
                OS = os,
                IP = ip,
                AnyDesk = anydesk,
                Spooler = spoolerStatus,
                Drives = string.Join(", ", driveInfoList),
                CapturedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            return JsonSerializer.Serialize(diagObj);
        }
        catch (Exception ex)
        {
            return $"{{\"Error\": \"{ex.Message}\"}}";
        }
    }

    public ComplaintTicket CreateTicket(
        string title,
        string description,
        string category,
        string priority,
        string userContact,
        string branchLocation,
        string? aiTranscript = null,
        bool filedViaAi = false)
    {
        var localDev = FleetRegistryService.Instance.LocalDevice;

        var ticket = new ComplaintTicket
        {
            TicketId = $"SVLL-TKT-{DateTime.Now:yyMMdd-HHmmss}",
            Title = title,
            Description = description,
            Category = category,
            Priority = priority,
            Status = "Open",
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
            CreatedBy = Environment.UserName,
            UserContact = userContact,
            BranchLocation = string.IsNullOrWhiteSpace(branchLocation) ? localDev.BranchLocation : branchLocation,
            Hostname = Environment.MachineName,
            IpAddress = localDev.IpAddress,
            AnyDeskId = localDev.AnyDeskId,
            AttachedDiagnosticsJson = CaptureSystemDiagnosticsJson(),
            AiTriageTranscript = aiTranscript ?? "",
            FiledViaAi = filedViaAi,
            AssignedTechnician = "Kunal Turkar (SVLL IT)",
            AdminNotes = ""
        };

        _tickets.Insert(0, ticket);
        SaveTickets();

        // Also save to shared folder if available
        try
        {
            var sharedDir = AdminAuthService.Instance.GetConfig().SharedTicketDirectory;
            if (!string.IsNullOrEmpty(sharedDir) && Directory.Exists(sharedDir))
            {
                string tktFile = Path.Combine(sharedDir, $"TKT-{ticket.TicketId}.json");
                string j = JsonSerializer.Serialize(ticket, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(tktFile, j);
            }
        }
        catch { }

        return ticket;
    }

    public bool UpdateTicket(string ticketId, string status, string adminNotes, string assignedTechnician)
    {
        var ticket = _tickets.FirstOrDefault(t => t.TicketId == ticketId);
        if (ticket != null)
        {
            ticket.Status = status;
            ticket.AdminNotes = adminNotes;
            ticket.AssignedTechnician = assignedTechnician;
            ticket.UpdatedAt = DateTime.Now;
            SaveTickets();
            return true;
        }
        return false;
    }

    public bool DeleteTicket(string ticketId)
    {
        var ticket = _tickets.FirstOrDefault(t => t.TicketId == ticketId);
        if (ticket != null)
        {
            _tickets.Remove(ticket);
            SaveTickets();
            return true;
        }
        return false;
    }

    public string ExportTicketsCsv()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Ticket ID,Date,Status,Priority,Category,Title,Requester,Contact,Host,IP,AnyDesk ID,Assigned To,Admin Notes");
        foreach (var t in _tickets)
        {
            sb.AppendLine($"\"{t.TicketId}\",\"{t.CreatedAt:yyyy-MM-dd HH:mm}\",\"{t.Status}\",\"{t.Priority}\",\"{t.Category}\",\"{t.Title.Replace("\"", "\"\"")}\",\"{t.CreatedBy}\",\"{t.UserContact}\",\"{t.Hostname}\",\"{t.IpAddress}\",\"{t.AnyDeskId}\",\"{t.AssignedTechnician}\",\"{t.AdminNotes.Replace("\"", "\"\"")}\"");
        }
        return sb.ToString();
    }
}
