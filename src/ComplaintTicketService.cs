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

    public const string RealSheetName = "Complain Log sheet IT - New Complaint.csv";
    public static readonly string ProjectFolderCsvPath = Path.Combine(@"c:\IT PROJECTS\svll-workstation", RealSheetName);
    public static readonly string LocalAppCsvPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, RealSheetName);
    public static readonly string AppDataCsvPath = Path.Combine(MainWindow.AuditLogDirectory, RealSheetName);

    // Fallback paths
    public static readonly string LegacyProjectCsvPath = @"c:\IT PROJECTS\svll-workstation\complaints_log_sheet.csv";
    public static readonly string LegacyAppDataCsvPath = Path.Combine(MainWindow.AuditLogDirectory, "complaints_log_sheet.csv");

    // Event notifying UI subscribers that tickets have changed
    public static event Action? OnTicketsChanged;

    public static void NotifyTicketsChanged() => OnTicketsChanged?.Invoke();

    public static string GetActiveCsvPath()
    {
        if (File.Exists(ProjectFolderCsvPath)) return ProjectFolderCsvPath;
        if (File.Exists(LocalAppCsvPath)) return LocalAppCsvPath;
        if (File.Exists(AppDataCsvPath)) return AppDataCsvPath;
        if (Directory.Exists(@"c:\IT PROJECTS\svll-workstation")) return ProjectFolderCsvPath;
        if (File.Exists(LegacyProjectCsvPath)) return LegacyProjectCsvPath;
        if (File.Exists(LegacyAppDataCsvPath)) return LegacyAppDataCsvPath;
        return LocalAppCsvPath;
    }

    private readonly string _ticketsFilePath;
    private readonly List<ComplaintTicket> _tickets = new();
    private int _maxTicketNumber = 12278;

    private ComplaintTicketService()
    {
        _ticketsFilePath = Path.Combine(MainWindow.AuditLogDirectory, "complaint_tickets.json");
        LoadTickets();
    }

    public void LoadTickets()
    {
        _tickets.Clear();

        // 1. First, attempt to load from the real Complain Log sheet CSV
        string activeCsv = GetActiveCsvPath();
        if (File.Exists(activeCsv))
        {
            try
            {
                LoadTicketsFromLogSheetCsv(activeCsv);
            }
            catch { }
        }

        // 2. Also load or merge local JSON tickets
        try
        {
            if (File.Exists(_ticketsFilePath))
            {
                string json = File.ReadAllText(_ticketsFilePath);
                var loaded = JsonSerializer.Deserialize<List<ComplaintTicket>>(json);
                if (loaded != null)
                {
                    foreach (var lt in loaded)
                    {
                        var existing = _tickets.FirstOrDefault(t => t.TicketId == lt.TicketId);
                        if (existing == null)
                        {
                            _tickets.Insert(0, lt);
                        }
                        else
                        {
                            // Preserve local admin updates
                            existing.Status = lt.Status;
                            existing.AdminNotes = lt.AdminNotes;
                            existing.AssignedTechnician = lt.AssignedTechnician;
                            existing.UpdatedAt = lt.UpdatedAt;
                        }
                    }
                }
            }
        }
        catch { }

        // Clean out any legacy thermal printer seed tickets
        _tickets.RemoveAll(t => t.Category == "ThermalPrinter" || t.Title.Contains("Thermal Printer") || t.Description.Contains("Thermal Printer"));

        // If completely empty, populate seed tickets
        if (_tickets.Count == 0)
        {
            PopulateSeedTickets();
        }
    }

    private void LoadTicketsFromLogSheetCsv(string csvPath)
    {
        var allLines = File.ReadAllLines(csvPath);
        if (allLines.Length <= 7) return;

        // Process rows in reverse or from row 7 onwards to find max ID and load active recent tickets
        var parsedTickets = new List<ComplaintTicket>();

        for (int i = 7; i < allLines.Length; i++)
        {
            string line = allLines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;

            var cols = ParseCsvLine(line);
            if (cols.Count < 11) continue;

            string ticketNo = cols.Count > 3 ? cols[3].Trim() : "";
            if (!ticketNo.StartsWith("IT/COM/", StringComparison.OrdinalIgnoreCase))
                continue;

            // Track max ticket number
            string numPart = ticketNo.Substring("IT/COM/".Length).Trim();
            if (int.TryParse(numPart, out int numVal) && numVal > _maxTicketNumber)
            {
                _maxTicketNumber = numVal;
            }

            // Load last 400 complaints into active in-memory list for instant UI responsiveness
            if (i >= allLines.Length - 400)
            {
                string dateStr = cols.Count > 4 ? cols[4].Trim() : "";
                string email = cols.Count > 5 ? cols[5].Trim() : "";
                string dept = cols.Count > 6 ? cols[6].Trim() : "Depot";
                string complainer = cols.Count > 7 ? cols[7].Trim() : "User";
                string contact = cols.Count > 8 ? cols[8].Trim() : "";
                string category = cols.Count > 9 ? cols[9].Trim() : "Other";
                string details = cols.Count > 10 ? cols[10].Trim() : "";
                string prio = cols.Count > 11 ? cols[11].Trim() : "NORMAL";
                string allocatedTo = cols.Count > 13 ? cols[13].Trim() : "Kunal Turkar (SVLL IT)";
                string statusCol = cols.Count > 26 ? cols[26].Trim() : "";
                string remarks = cols.Count > 27 ? cols[27].Trim() : "";

                DateTime createdAt = DateTime.Now;
                DateTime.TryParse(dateStr.Replace(" -", "-"), out createdAt);

                string normStatus = "Open";
                if (statusCol.Equals("COMPLETED", StringComparison.OrdinalIgnoreCase) || statusCol.Equals("DONE", StringComparison.OrdinalIgnoreCase))
                    normStatus = "Resolved";
                else if (statusCol.Equals("CLICK FOR COMPLETED", StringComparison.OrdinalIgnoreCase) || statusCol.Equals("IN PROGRESS", StringComparison.OrdinalIgnoreCase))
                    normStatus = "In Progress";

                string normPrio = prio.ToUpperInvariant().Contains("URGENT") ? "Critical" : "Medium";

                string title = details.Length > 60 ? details.Substring(0, 57) + "..." : details;
                if (string.IsNullOrWhiteSpace(title)) title = $"{category} Incident";

                var tkt = new ComplaintTicket
                {
                    TicketId = ticketNo,
                    Title = title,
                    Description = details,
                    Category = category,
                    Priority = normPrio,
                    Status = normStatus,
                    CreatedAt = createdAt,
                    UpdatedAt = createdAt,
                    CreatedBy = string.IsNullOrWhiteSpace(complainer) ? email : complainer,
                    UserContact = $"{contact} | {email}".Trim(' ', '|'),
                    BranchLocation = dept,
                    Hostname = "SVLL-DEPOT-PC",
                    IpAddress = "192.168.1.100",
                    AnyDeskId = "",
                    AttachedDiagnosticsJson = "{\"Source\": \"Complain Log sheet IT - New Complaint.csv\"}",
                    AssignedTechnician = string.IsNullOrWhiteSpace(allocatedTo) ? "Kunal Turkar (SVLL IT)" : allocatedTo,
                    AdminNotes = remarks
                };

                parsedTickets.Add(tkt);
            }
        }

        // Add tickets in descending order (latest first)
        parsedTickets.Reverse();
        _tickets.AddRange(parsedTickets);
    }

    public static List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        bool inQuotes = false;
        var cur = new System.Text.StringBuilder();
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    cur.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(cur.ToString());
                cur.Clear();
            }
            else
            {
                cur.Append(c);
            }
        }
        result.Add(cur.ToString());
        return result;
    }

    private void PopulateSeedTickets()
    {
        _tickets.Add(new ComplaintTicket
        {
            TicketId = "SVLL-TKT-2026-1001",
            Title = "Office Network Laser Printer Queue Stuck and Spooler Crashing",
            Category = "PrinterSpooler",
            Priority = "High",
            Status = "Open",
            CreatedAt = DateTime.Now.AddHours(-3),
            CreatedBy = "dispatch.bom",
            UserContact = "+91 98200 12345 (Ext 102)",
            BranchLocation = "Mumbai Central Hub",
            Hostname = "SVLL-BOM-DISPATCH-01",
            IpAddress = "192.168.10.45",
            AnyDeskId = "948 213 502",
            Description = "Documents sent to department network laser printer are stuck in status 'Deleting - Printing'. Print spooler service fails to clear queue automatically.",
            AttachedDiagnosticsJson = "{\"Printer\": \"HP LaserJet Enterprise M507\", \"Spooler\": \"Error / Locked\", \"Port\": \"192.168.10.220\", \"QueuedJobs\": 5, \"Driver\": \"HP Universal Printing PCL 6\"}",
            AssignedTechnician = "Kunal Turkar (SVLL IT)",
            AdminNotes = "Spooler flushed via Net Stop Spooler and %systemroot%\\System32\\Spool\\Printers cleared. Driver verified."
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

        // Also save synchronized CSV
        SaveComplaintsCsv();
    }

    public void SaveComplaintsCsv()
    {
        try
        {
            string csv = ExportTicketsCsv();

            // 1. Write to Project Folder if folder exists
            if (Directory.Exists(@"c:\IT PROJECTS\svll-workstation"))
            {
                try
                {
                    File.WriteAllText(ProjectFolderCsvPath, csv);
                }
                catch { }
            }

            // 2. Write to AppData Directory
            if (!Directory.Exists(MainWindow.AuditLogDirectory))
            {
                Directory.CreateDirectory(MainWindow.AuditLogDirectory);
            }
            File.WriteAllText(AppDataCsvPath, csv);

            // 3. Write to local application directory
            try
            {
                File.WriteAllText(LocalAppCsvPath, csv);
            }
            catch { }
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
        bool filedViaAi = false,
        string? complainerName = null,
        string? complainerEmail = null)
    {
        var localDev = FleetRegistryService.Instance.LocalDevice;

        string ticketId = $"IT/COM/{++_maxTicketNumber}";
        string complainer = !string.IsNullOrWhiteSpace(complainerName) ? complainerName : Environment.UserName;
        string email = !string.IsNullOrWhiteSpace(complainerEmail) ? complainerEmail :
                       (userContact.Contains("@") ? userContact : $"{Environment.UserName.ToLowerInvariant()}@shreevasulogistics.com");
        string dept = !string.IsNullOrWhiteSpace(branchLocation) ? branchLocation : localDev.BranchLocation;
        if (string.IsNullOrWhiteSpace(dept)) dept = "LOGISTICS";

        var ticket = new ComplaintTicket
        {
            TicketId = ticketId,
            Title = title,
            Description = description,
            Category = category,
            Priority = priority,
            Status = "Open",
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
            CreatedBy = complainer,
            UserContact = userContact,
            BranchLocation = dept,
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

        // 1. Append directly to Complain Log sheet IT - New Complaint.csv
        AppendToRealLogSheet(ticket, email);

        // 2. Save JSON cache & backup CSV
        SaveTickets();

        // 3. Fire real-time notification so Admin Desk immediately updates
        NotifyTicketsChanged();

        // v8.0: Push to Cloud Relay Server
        _ = Task.Run(async () =>
        {
            try
            {
                string? cloudId = await CloudSyncService.Instance.SubmitTicketAsync(ticket);
                if (!string.IsNullOrEmpty(cloudId))
                {
                    ticket.CloudTicketId = cloudId;
                    SaveTickets();
                }
            }
            catch { }
        });

        // 4. Also save to shared folder if available
        try
        {
            var sharedDir = AdminAuthService.Instance.GetConfig().SharedTicketDirectory;
            if (!string.IsNullOrEmpty(sharedDir) && Directory.Exists(sharedDir))
            {
                string tktFile = Path.Combine(sharedDir, $"TKT-{ticket.TicketId.Replace('/', '-')}.json");
                string j = JsonSerializer.Serialize(ticket, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(tktFile, j);
            }
        }
        catch { }

        return ticket;
    }

    private void AppendToRealLogSheet(ComplaintTicket tkt, string email)
    {
        try
        {
            string monthStr = DateTime.Now.ToString("MMM-yyyy");
            string dateStr = DateTime.Now.ToString("d -MMM - yyyy h:mm tt");
            string cleanDesc = (tkt.Description ?? "").Replace("\"", "\"\"").Replace("\r", " ").Replace("\n", " ");
            string prioStr = tkt.Priority.Equals("Critical", StringComparison.OrdinalIgnoreCase) || tkt.Priority.Equals("High", StringComparison.OrdinalIgnoreCase) || tkt.Priority.Equals("URGENT", StringComparison.OrdinalIgnoreCase) ? "URGENT" : "NORMAL";
            string prioType = "TELEPHONE";
            string assigned = tkt.AssignedTechnician;
            string status = "CLICK FOR COMPLETED";

            string line = $",,{monthStr},{tkt.TicketId},{dateStr},{email},{tkt.BranchLocation},{tkt.CreatedBy},\"{tkt.UserContact}\",{tkt.Category},\"{cleanDesc}\",{prioStr},{prioType},{assigned},,,,,VALID,,,,,,,,{status},,,,,,,,,,,,,,,,,,,,\r\n";

            string targetCsv = GetActiveCsvPath();
            if (File.Exists(targetCsv))
            {
                File.AppendAllText(targetCsv, line, System.Text.Encoding.UTF8);
            }

            // If ProjectFolderCsvPath exists and differs, also write there
            if (File.Exists(ProjectFolderCsvPath) && !string.Equals(targetCsv, ProjectFolderCsvPath, StringComparison.OrdinalIgnoreCase))
            {
                File.AppendAllText(ProjectFolderCsvPath, line, System.Text.Encoding.UTF8);
            }
        }
        catch { }
    }

    public bool UpdateTicket(string ticketId, string status, string adminNotes, string assignedTechnician, string? adminReply = null)
    {
        var ticket = _tickets.FirstOrDefault(t => t.TicketId == ticketId);
        if (ticket != null)
        {
            ticket.Status = status;
            ticket.AdminNotes = adminNotes;
            if (!string.IsNullOrEmpty(adminReply))
            {
                ticket.AdminReply = adminReply;
                ticket.AdminReplyAt = DateTime.Now;
            }
            ticket.AssignedTechnician = assignedTechnician;
            ticket.UpdatedAt = DateTime.Now;
            SaveTickets();
            NotifyTicketsChanged();

#if BUILD_ADMIN
            // v8.0: Sync resolution & reply to cloud relay
            _ = Task.Run(async () =>
            {
                try
                {
                    await CloudSyncService.Instance.ResolveTicketAsync(ticket.CloudTicketId ?? ticket.TicketId, status, ticket.AdminReply ?? adminNotes, adminNotes);
                }
                catch { }
            });
#endif

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
            NotifyTicketsChanged();
            return true;
        }
        return false;
    }

    public string ExportTicketsCsv()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Ticket ID,Created At,Status,Priority,Category,Title,Requester,Contact,Branch,Hostname,IP Address,AnyDesk ID,Assigned Technician,System Telemetry,Admin Notes");
        foreach (var t in _tickets)
        {
            string cleanTitle = (t.Title ?? "").Replace("\"", "\"\"").Replace("\r", " ").Replace("\n", " ");
            string cleanDesc = (t.Description ?? "").Replace("\"", "\"\"").Replace("\r", " ").Replace("\n", " ");
            string cleanNotes = (t.AdminNotes ?? "").Replace("\"", "\"\"").Replace("\r", " ").Replace("\n", " ");
            string cleanDiag = (t.AttachedDiagnosticsJson ?? "").Replace("\"", "\"\"").Replace("\r", " ").Replace("\n", " ");

            sb.AppendLine($"\"{t.TicketId}\",\"{t.CreatedAt:yyyy-MM-dd HH:mm:ss}\",\"{t.Status}\",\"{t.Priority}\",\"{t.Category}\",\"{cleanTitle}\",\"{t.CreatedBy}\",\"{t.UserContact}\",\"{t.BranchLocation}\",\"{t.Hostname}\",\"{t.IpAddress}\",\"{t.AnyDeskId}\",\"{t.AssignedTechnician}\",\"{cleanDiag}\",\"{cleanNotes}\"");
        }
        return sb.ToString();
    }
}
