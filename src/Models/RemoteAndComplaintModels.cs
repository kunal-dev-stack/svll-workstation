using System;
using System.Collections.Generic;

namespace SVLL_IT_Workstation.Models;

public class RemoteDeviceItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Hostname { get; set; } = Environment.MachineName;
    public string IpAddress { get; set; } = "127.0.0.1";
    public string MacAddress { get; set; } = "00-00-00-00-00-00";
    public string BranchLocation { get; set; } = "Mumbai HQ";
    public string LoggedInUser { get; set; } = Environment.UserName;
    public string AnyDeskId { get; set; } = "";
    public bool AnyDeskInstalled { get; set; } = false;
    public bool RdpEnabled { get; set; } = true;
    public bool QuickAssistAvailable { get; set; } = true;
    public string Status { get; set; } = "Online"; // Online, Busy, SupportRequested, Offline
    public string SupportRequestMessage { get; set; } = "";
    public DateTime? SupportRequestedAt { get; set; }
    public double CpuLoadPercent { get; set; } = 0.0;
    public double RamUsagePercent { get; set; } = 0.0;
    public string OsVersion { get; set; } = "Windows 11 Pro";
    public DateTime LastSeen { get; set; } = DateTime.Now;
    public bool IsLocalHost { get; set; } = false;
}

public class ComplaintTicket
{
    public string TicketId { get; set; } = "SVLL-TKT-" + DateTime.Now.ToString("yyMMdd-HHmmss");
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Category { get; set; } = "General"; // Network, Hardware, SoftwareWms, PrinterSpooler, EmailCredentials, UsbDlp, Other
    public string Priority { get; set; } = "Medium"; // Critical, High, Medium, Low
    public string Status { get; set; } = "Open"; // Open, In Progress, Resolved, Closed
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; } = DateTime.Now;
    public string CreatedBy { get; set; } = Environment.UserName;
    public string UserContact { get; set; } = "";
    public string BranchLocation { get; set; } = "Logistics Hub";
    public string Hostname { get; set; } = Environment.MachineName;
    public string IpAddress { get; set; } = "";
    public string AnyDeskId { get; set; } = "";
    public string AttachedDiagnosticsJson { get; set; } = "";
    public string AiTriageTranscript { get; set; } = "";
    public string AdminNotes { get; set; } = "";
    public string AssignedTechnician { get; set; } = "Kunal Turkar (SVLL IT)";
    public bool FiledViaAi { get; set; } = false;
}

public class AiChatMessage
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Sender { get; set; } = "AI"; // "AI", "User", "System"
    public string Text { get; set; } = "";
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public List<string> QuickReplies { get; set; } = new();
    public string? ActionCommand { get; set; }
    public string? ActionLabel { get; set; }
}

public class AdminSecurityConfig
{
    public string PasswordHash { get; set; } = "";
    public string PasswordSalt { get; set; } = "";
    public DateTime LastPasswordChange { get; set; } = DateTime.Now;
    public string SharedFleetDirectory { get; set; } = "";
    public string SharedTicketDirectory { get; set; } = "";
}
