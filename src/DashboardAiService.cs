using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SVLL_IT_Workstation;

public class DashboardAiResponse
{
    public string Answer { get; set; } = "";
    public string? ActionTag { get; set; }
    public string? ActionTitle { get; set; }
    public string? ActionDescription { get; set; }
    public bool IsOnlineAi { get; set; } = false;
    public List<string> Suggestions { get; set; } = new();
}

public class DashboardAiService
{
    private static DashboardAiService? _instance;
    public static DashboardAiService Instance => _instance ??= new DashboardAiService();

    private readonly HttpClient _httpClient;

    private DashboardAiService()
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(5)
        };
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "SVLL-Workstation-Copilot/7.1");
    }

    public async Task<DashboardAiResponse> AskAsync(string query)
    {
        string raw = query.Trim();
        string lower = raw.ToLowerInvariant();

        // 1. Detect explicit Workstation Feature Actions
        var featureResponse = TryMatchWorkstationFeature(raw, lower);

        // If it's a direct operational action (e.g. "boost pc", "fix printer", "flush dns"), return immediately
        if (featureResponse != null && featureResponse.ActionTag != null && IsDirectCommand(lower))
        {
            return featureResponse;
        }

        // 2. Query Free Cloud AI API (Pollinations.ai / Free Public Text Engine)
        string? onlineAnswer = null;
        try
        {
            onlineAnswer = await QueryFreeCloudAiAsync(raw);
        }
        catch
        {
            onlineAnswer = null;
        }

        if (!string.IsNullOrWhiteSpace(onlineAnswer) && onlineAnswer.Length > 10)
        {
            // If online AI succeeded and we matched a related feature, attach the action button!
            return new DashboardAiResponse
            {
                Answer = onlineAnswer.Trim(),
                ActionTag = featureResponse?.ActionTag,
                ActionTitle = featureResponse?.ActionTitle,
                ActionDescription = featureResponse?.ActionDescription,
                IsOnlineAi = true,
                Suggestions = featureResponse?.Suggestions ?? GetDefaultSuggestions()
            };
        }

        // 3. Fallback to Local Knowledge & Expert Diagnostic Brain
        if (featureResponse != null)
        {
            featureResponse.IsOnlineAi = false;
            return featureResponse;
        }

        return GenerateSmartLocalAnswer(raw, lower);
    }

    private bool IsDirectCommand(string lower)
    {
        return lower.StartsWith("turbo") ||
               lower.StartsWith("boost") ||
               lower.StartsWith("clean") ||
               lower.StartsWith("flush") ||
               lower.StartsWith("ping") ||
               lower.StartsWith("fix printer") ||
               lower.StartsWith("open") ||
               lower.StartsWith("run") ||
               lower.StartsWith("launch");
    }

    private async Task<string?> QueryFreeCloudAiAsync(string userPrompt)
    {
        string host = Environment.MachineName;
        string user = Environment.UserName;
        string os = Environment.OSVersion.VersionString;

        string systemPrompt =
            $"You are the SVLL Workstation AI Diagnostic Copilot for Shree Vasu Logistics Limited. " +
            $"Workstation: '{host}', User: '{user}', OS: '{os}'. " +
            "Provide helpful, practical, step-by-step instructions. " +
            "When explaining how to fix any issue or error, ALWAYS structure your answer with clear numbered steps: " +
            "Step 1: [Action], Step 2: [Action], Step 3: [Action]. " +
            "At the end of your explanation, explicitly suggest using the built-in 1-click SVLL tool if one exists (e.g., 1-Click Turbo Boost, Print Spooler Reset, Flush DNS, Temp Purge, S.M.A.R.T. Drive Health, USB Depot, Force Uninstaller, or Service Desk).";

        // Attempt JSON POST
        try
        {
            var payload = new
            {
                messages = new object[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                model = "openai",
                seed = 42
            };

            string json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("https://text.pollinations.ai/", content);
            if (response.IsSuccessStatusCode)
            {
                string text = await response.Content.ReadAsStringAsync();
                if (!string.IsNullOrWhiteSpace(text) && !text.Contains("<!DOCTYPE html>"))
                    return text.Trim();
            }
        }
        catch { }

        // Attempt GET fallback
        try
        {
            string url = "https://text.pollinations.ai/" + Uri.EscapeDataString(userPrompt);
            var response = await _httpClient.GetStringAsync(url);
            if (!string.IsNullOrWhiteSpace(response) && !response.Contains("<!DOCTYPE html>"))
                return response.Trim();
        }
        catch { }

        return null;
    }

    private DashboardAiResponse? TryMatchWorkstationFeature(string raw, string lower)
    {
        // Turbo Booster / PC Slow / Freezing
        if (lower.Contains("turbo") || lower.Contains("boost") || lower.Contains("slow") || lower.Contains("freeze") || lower.Contains("freezing") || lower.Contains("lag") || lower.Contains("speed up") || lower.Contains("ram full") || lower.Contains("cpu high"))
        {
            return new DashboardAiResponse
            {
                Answer = "⚡ **System Performance Diagnostics & Step-by-Step Fix:**\n\n" +
                         "• **Step 1:** Flush memory working sets and release idle cached memory\n" +
                         "• **Step 2:** Terminate non-critical background telemetry and bloat processes\n" +
                         "• **Step 3:** Switch power plan to Windows Ultimate Performance profile\n\n" +
                         "💡 **Built-in Tool Available:** Click the button below to execute all 3 optimization steps automatically!",
                ActionTag = "TurboBoost",
                ActionTitle = "⚡ Execute 1-Click Turbo Boost Now",
                ActionDescription = "Instantly releases idle RAM, trims background applications, and enables maximum CPU performance.",
                Suggestions = new List<string> { "Purge Temp Files", "Check S.M.A.R.T. Drive", "Inspect Top Processes" }
            };
        }

        // Printer & Spooler
        if (lower.Contains("print") || lower.Contains("spool") || lower.Contains("paper queue") || lower.Contains("deleting - printing") || lower.Contains("laser printer"))
        {
            return new DashboardAiResponse
            {
                Answer = "🖨️ **Print Subsystem Lockup Diagnosis & Step-by-Step Fix:**\n\n" +
                         "• **Step 1:** Stop the Windows Print Spooler service (`Stop-Service Spooler`)\n" +
                         "• **Step 2:** Purge all locked phantom `.SPL` and `.SHD` queue files in `C:\\Windows\\System32\\spool\\PRINTERS`\n" +
                         "• **Step 3:** Restart the Print Spooler service (`Start-Service Spooler`)\n" +
                         "• **Step 4:** Re-send your logistics invoice or test page\n\n" +
                         "💡 **Built-in Tool Available:** Click the button below to automatically purge the queue and reset the spooler service in 1 click!",
                ActionTag = "FixPrintSpooler",
                ActionTitle = "🖨️ Fix & Restart Print Spooler Service",
                ActionDescription = "Stops spooler, purges locked job files in System32\\spool\\PRINTERS, and restarts the service.",
                Suggestions = new List<string> { "Test Printer Driver", "File IT Complaint Ticket", "Open Printer Suite" }
            };
        }

        // DNS / Network / Ping / Gateway
        if (lower.Contains("dns") || lower.Contains("ping") || lower.Contains("gateway") || lower.Contains("internet") || lower.Contains("disconnect") || lower.Contains("packet loss") || lower.Contains("offline") || lower.Contains("ipconfig") || lower.Contains("wifi") || lower.Contains("network"))
        {
            return new DashboardAiResponse
            {
                Answer = "🌐 **Network & ERP Connectivity Diagnosis & Step-by-Step Fix:**\n\n" +
                         "• **Step 1:** Clear the local DNS resolver cache (`ipconfig /flushdns`)\n" +
                         "• **Step 2:** Renew the DHCP IP address lease from the warehouse router\n" +
                         "• **Step 3:** Test ICMP echo ping latency to the local gateway (8.8.8.8 / ERP)\n" +
                         "• **Step 4:** Verify Ethernet cable seating or reconnect Wi-Fi\n\n" +
                         "💡 **Built-in Tool Available:** Click the button below to flush DNS and reset the resolver cache immediately!",
                ActionTag = "FlushDns",
                ActionTitle = "⚡ Flush DNS & Reset Resolver Cache",
                ActionDescription = "Executes Clear-DnsClientCache and ipconfig /flushdns to clear stale host lookups.",
                Suggestions = new List<string> { "Ping Gateway Diagnostic", "View My IP & MAC", "Network Profile Switcher" }
            };
        }

        // Temp Files / Disk Cleanup / Junk / Full Drive
        if (lower.Contains("temp") || lower.Contains("clean") || lower.Contains("junk") || lower.Contains("recycle bin") || lower.Contains("disk full") || lower.Contains("c drive") || lower.Contains("cache"))
        {
            return new DashboardAiResponse
            {
                Answer = "🧹 **Storage & SSD Junk Cleanup Step-by-Step Fix:**\n\n" +
                         "• **Step 1:** Delete user temporary cache files in `%TEMP%`\n" +
                         "• **Step 2:** Clear Windows system temp caches in `C:\\Windows\\Temp`\n" +
                         "• **Step 3:** Empty the Windows Recycle Bin across all drives\n" +
                         "• **Step 4:** Reclaim disk headroom for pagefile and OS paging\n\n" +
                         "💡 **Built-in Tool Available:** Click the button below to safely purge all temp caches and empty the Recycle Bin!",
                ActionTag = "PurgeTemp",
                ActionTitle = "🧹 Purge Temp Files & Empty Recycle Bin",
                ActionDescription = "Safely deletes user temp files, system temp cache, and empties the Windows Recycle Bin.",
                Suggestions = new List<string> { "Check S.M.A.R.T. Drive", "Open WinUtil Debloat", "1-Click Turbo Boost" }
            };
        }

        // Complaint / IT Ticket / Service Desk
        if (lower.Contains("ticket") || lower.Contains("complaint") || lower.Contains("report") || lower.Contains("helpdesk") || lower.Contains("support") || lower.Contains("raise issue"))
        {
            return new DashboardAiResponse
            {
                Answer = "📋 **SVLL IT Service Desk:** You can submit an official IT support complaint ticket synchronized directly with the enterprise CSV log sheet (`Complain Log sheet IT - New Complaint.csv`) and IT Admin Console. Automated hardware telemetry is captured and attached to your ticket.",
                ActionTag = "OpenSubmitComplaint",
                ActionTitle = "📋 Register IT Support Complaint Ticket",
                ActionDescription = "Opens the official IT Service Desk intake form with live auto-sync to the central log sheet.",
                Suggestions = new List<string> { "Track My Tickets", "Admin Service Desk", "AI Diagnostic Intake" }
            };
        }

        // Remote Client / AnyDesk
        if (lower.Contains("anydesk") || lower.Contains("remote") || lower.Contains("technician") || lower.Contains("rdp") || lower.Contains("quick assist"))
        {
            return new DashboardAiResponse
            {
                Answer = "🔴 **Remote Assistance:** SVLL Workstation integrates with AnyDesk, Remote Desktop (RDP), and Windows Quick Assist. You can inspect your local AnyDesk address, toggle remote control privileges, and allow IT technicians to assist you.",
                ActionTag = "OpenRemoteClient",
                ActionTitle = "🔴 Open Remote Client & AnyDesk",
                ActionDescription = "Displays your local AnyDesk ID, service status, and remote access toggles.",
                Suggestions = new List<string> { "Fleet Admin Console", "File IT Complaint Ticket", "Check IP Address" }
            };
        }

        // USB Depot / Software Install
        if (lower.Contains("usb depot") || lower.Contains("usb drive") || lower.Contains("pen drive") || lower.Contains("install software") || lower.Contains("offline software") || lower.Contains("driver install"))
        {
            return new DashboardAiResponse
            {
                Answer = "🔌 **Rapid USB Software & Driver Depot:** Plug in any USB flash drive to automatically detect, organize, and silently install enterprise software packages (.exe / .msi) or hardware drivers (.inf) without internet access.",
                ActionTag = "OpenUsbDepot",
                ActionTitle = "🔌 Open USB Software & Driver Depot",
                ActionDescription = "Scan connected USB media for unattended software packages and WHQL drivers.",
                Suggestions = new List<string> { "WinGet Software Deployer", "Force App Purge", "USB DLP Guardian" }
            };
        }

        // App Uninstaller / Force Purge
        if (lower.Contains("uninstall") || lower.Contains("remove app") || lower.Contains("purge software") || lower.Contains("stubborn app") || lower.Contains("delete program"))
        {
            return new DashboardAiResponse
            {
                Answer = "🗑️ **Nuclear Force App Uninstaller:** Remove stubborn software, bypass uninstallation passwords, take ownership of protected folders, and cleanly wipe leftover registry keys and application directories.",
                ActionTag = "OpenAppUninstaller",
                ActionTitle = "🗑️ Open Force App Uninstaller",
                ActionDescription = "Forcefully purge stubborn software without password prompts or uninstaller hangs.",
                Suggestions = new List<string> { "WinUtil Enterprise Debloat", "Purge Temp Files", "Inspect Top Processes" }
            };
        }

        // Security / Process Shield / Malware / Antivirus
        if (lower.Contains("security") || lower.Contains("shield") || lower.Contains("virus") || lower.Contains("malware") || lower.Contains("ransomware") || lower.Contains("threat"))
        {
            return new DashboardAiResponse
            {
                Answer = "🛡️ **Endpoint Threat Defense:** The Zero-Trust Process Shield conducts heuristic memory scans, terminates suspicious unsigned binaries executing out of `%TEMP%` or `%APPDATA%`, and verifies system security hardening against CIS benchmarks.",
                ActionTag = "OpenProcessShield",
                ActionTitle = "🛡️ Launch Zero-Trust Process Shield",
                ActionDescription = "Scan running processes, analyze memory footprints, and verify endpoint security.",
                Suggestions = new List<string> { "CIS Security Hardening", "USB DLP Guardian", "Port Threat Hunter" }
            };
        }

        // S.M.A.R.T. / SSD / Hard Drive Health
        if (lower.Contains("smart") || lower.Contains("drive health") || lower.Contains("ssd") || lower.Contains("hard drive") || lower.Contains("nvme") || lower.Contains("bad sector"))
        {
            return new DashboardAiResponse
            {
                Answer = "💾 **S.M.A.R.T. Drive Diagnostic & Crash Predictor:** Inspects physical NVMe/SATA controller registers, SSD wear life percentages, bad sector reallocations, and temperature thresholds to anticipate hardware failures before data loss occurs.",
                ActionTag = "OpenStorageSmart",
                ActionTitle = "💾 Check S.M.A.R.T. Drive Health",
                ActionDescription = "Reads physical disk controller telemetry and SSD health endurance levels.",
                Suggestions = new List<string> { "Purge Temp Files", "Drive Optimization (TRIM)", "Windows OS Repair" }
            };
        }

        // Boss ERP / WMS / Dispatch
        if (lower.Contains("boss") || lower.Contains("erp") || lower.Contains("wms") || lower.Contains("lr bill") || lower.Contains("dispatch"))
        {
            return new DashboardAiResponse
            {
                Answer = "🏢 **Boss ERP Client Guidance:** Boss ERP session timeouts and disconnects occur when database connection pooling drops or network latency to the central dispatch server exceeds 150ms. Restart Boss client, verify Gateway ping, or file an IT ticket if the database server is unresponsive.",
                ActionTag = "OpenSubmitComplaint",
                ActionTitle = "📋 File Boss ERP Support Ticket",
                ActionDescription = "Registers an urgent ERP incident directly with the SVLL IT Helpdesk team.",
                Suggestions = new List<string> { "Ping Gateway Diagnostic", "Flush DNS Resolver", "1-Click Turbo Boost" }
            };
        }

        // Hardware Specs / IP / MAC / Hostname
        if (lower.Contains("spec") || lower.Contains("specs") || lower.Contains("hardware") || lower.Contains("what is my ip") || lower.Contains("mac address") || lower.Contains("cpu info") || lower.Contains("hostname"))
        {
            string host = Environment.MachineName;
            string user = Environment.UserName;
            int cpuCores = Environment.ProcessorCount;
            string os = Environment.OSVersion.VersionString;

            return new DashboardAiResponse
            {
                Answer = $"💻 **Workstation Hardware Profile:**\n• **Hostname:** `{host}`\n• **Logged User:** `{user}`\n• **CPU Threads:** {cpuCores} Logical Cores\n• **OS Version:** {os}\n• **Architecture:** 64-bit Windows",
                ActionTag = "OpenAssetPassport",
                ActionTitle = "🛡️ View Full Hardware Asset Passport",
                ActionDescription = "Inspect motherboard, BIOS serial tag, physical MAC addresses, and export QR passport.",
                Suggestions = new List<string> { "Check S.M.A.R.T. Drive", "PC Health & Battery", "1-Click Turbo Boost" }
            };
        }

        return null;
    }

    private DashboardAiResponse GenerateSmartLocalAnswer(string raw, string lower)
    {
        // Outlook / Mail
        if (lower.Contains("outlook") || lower.Contains("mail") || lower.Contains("email"))
        {
            return new DashboardAiResponse
            {
                Answer = "📧 **Outlook Email Troubleshooting:** If Outlook displays 'Disconnected' or send/receive failures, verify internet connectivity first. Corrupted local OST files or expired corporate credentials can be resolved by repairing the Outlook profile via Control Panel > Mail or restarting the Microsoft Exchange connectivity service.",
                ActionTag = "OpenSubmitComplaint",
                ActionTitle = "📋 File Outlook Mail Ticket",
                ActionDescription = "Report corporate mail synchronization issues to IT Support.",
                Suggestions = new List<string> { "Flush DNS", "Ping Gateway Diagnostic", "Register IT Ticket" }
            };
        }

        // Camera / CCTV
        if (lower.Contains("camera") || lower.Contains("cctv"))
        {
            return new DashboardAiResponse
            {
                Answer = "📹 **Security CCTV Camera Triage:** If a depot camera is showing down or incorrect timestamping, verify PoE (Power over Ethernet) switch port lights and check whether the camera IP responds to ICMP ping. Report camera hardware faults directly to IT for rapid physical or remote reconfiguration.",
                ActionTag = "OpenSubmitComplaint",
                ActionTitle = "📋 Report Camera Incident",
                ActionDescription = "Logs a Camera Down complaint with live CSV sync.",
                Suggestions = new List<string> { "Ping Gateway Diagnostic", "Register IT Ticket", "Track My Tickets" }
            };
        }

        // General Help / Features
        if (lower.Contains("help") || lower.Contains("what can you do") || lower.Contains("feature") || lower.Contains("command"))
        {
            return new DashboardAiResponse
            {
                Answer = "🤖 **SVLL AI Quick Support Copilot Features:**\nI can answer **any question** (IT, Windows, Boss ERP, hardware, networking, or general questions) and give you **1-click execution** for all workstation tools:\n• ⚡ **Turbo Boost** (Release RAM & speed up PC)\n• 🖨️ **Print Spooler** (Purge stuck jobs & restart service)\n• 🌐 **Network Diagnostics** (Flush DNS & ping gateway)\n• 🧹 **Temp Purge** (Clean junk & empty Recycle Bin)\n• 📋 **IT Complaints** (Auto-sync to company CSV log sheet)\n• 🔌 **USB Depot** & 🗑️ **App Uninstaller**.",
                ActionTag = "OpenLibrary",
                ActionTitle = "📖 Open 37-Scenario Command Playbook",
                ActionDescription = "Explore complete PowerShell and CMD recipes for enterprise fixes.",
                Suggestions = new List<string> { "Execute Turbo Boost", "Fix Print Spooler", "Flush DNS", "Purge Temp Files" }
            };
        }

        // General Technical fallback answer
        return new DashboardAiResponse
        {
            Answer = $"💡 **AI Quick Support Guidance for: '{raw}'**\n\nFor technical issues on this workstation (`{Environment.MachineName}`), you can trigger instant automated repairs using the buttons below, or file an official IT ticket directly synchronized with the SVLL Helpdesk.\n\nNeed assistance with a specific problem? Ask about *printers, network drops, slow PC, Boss ERP, temp files, or hardware specs*.",
            ActionTag = "TurboBoost",
            ActionTitle = "⚡ 1-Click Turbo Boost Optimization",
            ActionDescription = "Optimize system responsiveness and release fragmented memory.",
            Suggestions = GetDefaultSuggestions()
        };
    }

    private List<string> GetDefaultSuggestions()
    {
        return new List<string>
        {
            "⚡ 1-Click Turbo Boost",
            "🖨️ Fix Print Spooler",
            "🌐 Ping Gateway & DNS",
            "🧹 Clean Temp Files",
            "📋 File IT Complaint Ticket",
            "🔴 AnyDesk Remote Client"
        };
    }
}
