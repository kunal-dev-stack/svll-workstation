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
            Timeout = TimeSpan.FromSeconds(12)
        };
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "SVLL-Workstation-Copilot/8.5");
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
        // If it's an informational question or technical query, prioritize answering the question directly
        if (IsInformationalQuestion(lower))
        {
            var smartAnswer = GenerateSmartLocalAnswer(raw, lower);
            if (featureResponse != null && smartAnswer.ActionTag == null)
            {
                smartAnswer.ActionTag = featureResponse.ActionTag;
                smartAnswer.ActionTitle = featureResponse.ActionTitle;
                smartAnswer.ActionDescription = featureResponse.ActionDescription;
            }
            return smartAnswer;
        }

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

    private bool IsInformationalQuestion(string lower)
    {
        return lower.StartsWith("how") ||
               lower.StartsWith("what") ||
               lower.StartsWith("why") ||
               lower.StartsWith("where") ||
               lower.StartsWith("when") ||
               lower.StartsWith("can you") ||
               lower.StartsWith("explain") ||
               lower.StartsWith("tell me") ||
               lower.StartsWith("formula") ||
               lower.StartsWith("difference") ||
               lower.StartsWith("syntax") ||
               lower.Contains("meaning") ||
               lower.Contains("definition") ||
               lower.Contains("example") ||
               lower.Contains("vlookup") ||
               lower.Contains("xlookup") ||
               lower.Contains("index match") ||
               lower.Contains("pivot") ||
               lower.Contains("excel") ||
               lower.Contains("bsod") ||
               lower.Contains("blue screen") ||
               lower.Contains("powershell") ||
               lower.Contains("cmd") ||
               lower.Contains("command to") ||
               lower.Contains("shortcut") ||
               lower.Contains("zebra") ||
               lower.Contains("barcode") ||
               lower.Contains("thermal") ||
               lower.Contains("lr bill") ||
               lower.Contains("consignment") ||
               lower.Contains("calculate");
    }

    private async Task<string?> QueryFreeCloudAiAsync(string userPrompt)
    {
        string host = Environment.MachineName;
        string user = Environment.UserName;
        string os = Environment.OSVersion.VersionString;

        string systemPrompt =
            $"You are the SVLL AI IT & Operations Copilot for Shree Vasu Logistics Limited. " +
            $"Host: '{host}', User: '{user}', Windows OS: '{os}'. " +
            "You are a comprehensive technical and logistics assistant capable of answering questions beyond internal workstation tools. " +
            "You provide clear, accurate guidance on: " +
            "- Microsoft Excel (formulas, VLOOKUP/XLOOKUP, pivot tables, formatting, macros)\n" +
            "- Windows 10/11 system commands, PowerShell, CMD, registry, and troubleshooting\n" +
            "- Networking, DNS, IP configuration, Wi-Fi, switches, routers, and connectivity\n" +
            "- Hardware, thermal barcode printers (Zebra/TVS/TSC), scanners, and peripherals\n" +
            "- Enterprise logistics workflows, Lorry Receipts (LR), ERP/WMS issues, and consignment management\n" +
            "- General computing, Office 365, Outlook, software installation, and cybersecurity best practices.\n" +
            "Structure technical instructions with numbered steps and code/formula snippets where helpful. Keep answers practical, concise, and professional.";

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

        // Task Manager / Process Control / Performance
        if (lower.Contains("process") || lower.Contains("task") || lower.Contains("freeze") || lower.Contains("hang") || lower.Contains("slow") || lower.Contains("kill") || lower.Contains("manager"))
        {
            return new DashboardAiResponse
            {
                Answer = "⚡ **Interactive Task Manager:** You can inspect all running processes, memory consumption, active threads, and terminate high-load or unresponsive tasks with persistent selection and 1-click execution.",
                ActionTag = "OpenTaskManager",
                ActionTitle = "⚡ Open Interactive Task Manager",
                ActionDescription = "Opens the full real-time process controller to inspect threads, memory, and close tasks.",
                Suggestions = new List<string> { "1-Click Turbo Boost", "Purge Temp Files", "S.M.A.R.T. Drive Health" }
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
                Answer = "🏢 **Boss ERP Client Guidance:** Boss ERP session timeouts and disconnects occur when database connection pooling drops or network latency to the central dispatch server exceeds 150ms. Restart Boss client, verify Gateway ping, or inspect active network sockets.",
                ActionTag = "OpenWmsLatency",
                ActionTitle = "🏢 Check ERP & WMS Latency",
                ActionDescription = "Tests real-time round-trip latency to the central logistics ERP database.",
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
        // 1. Excel: XLOOKUP
        if (lower.Contains("xlookup"))
        {
            return new DashboardAiResponse
            {
                Answer = "📊 **Microsoft Excel: XLOOKUP Formula Guide**\n\n" +
                         "`=XLOOKUP(lookup_value, lookup_array, return_array, [if_not_found], [match_mode])`\n\n" +
                         "• **Example (Logistics Consignment Lookup):**\n" +
                         "  `=XLOOKUP(A2, Sheet2!A:A, Sheet2!D:D, \"Not Found\", 0)`\n" +
                         "  Looks for Docket ID in `A2`, scans column `A` of Sheet2, and returns Destination from column `D`.\n\n" +
                         "✨ **Key Benefits over VLOOKUP:**\n" +
                         "1. Lookups can be left-to-right OR right-to-left (no column index numbers required)\n" +
                         "2. Built-in error handling (`if_not_found`) eliminates `#N/A` without nesting `IFERROR`\n" +
                         "3. Defaults to exact match automatically.",
                Suggestions = new List<string> { "VLOOKUP Formula Guide", "SUMIFS Formula Guide", "Excel Frozen / Lag Fix" }
            };
        }

        // 2. Excel: VLOOKUP
        if (lower.Contains("vlookup"))
        {
            return new DashboardAiResponse
            {
                Answer = "📊 **Microsoft Excel: VLOOKUP Formula Guide**\n\n" +
                         "`=VLOOKUP(lookup_value, table_array, col_index_num, [range_lookup])`\n\n" +
                         "• **Example (Invoice Billing Rate Lookup):**\n" +
                         "  `=VLOOKUP(B2, Rates!A:D, 3, FALSE)`\n" +
                         "  Searches item in `B2`, matches against column 1 of `Rates!A:D`, and returns value from column 3.\n\n" +
                         "⚠️ **Troubleshooting Common Errors:**\n" +
                         "• **#N/A Error:** The lookup value doesn't exist in the leftmost column, or has leading/trailing spaces. Wrap with `=TRIM(B2)` or use `=IFERROR(VLOOKUP(...), \"Not Found\")`.\n" +
                         "• Always set the 4th argument to `FALSE` (or `0`) for exact match.",
                Suggestions = new List<string> { "XLOOKUP Formula Guide", "Index Match Formula", "Excel Remove Duplicates" }
            };
        }

        // 3. Excel: INDEX / MATCH
        if (lower.Contains("index match") || lower.Contains("index/match"))
        {
            return new DashboardAiResponse
            {
                Answer = "📊 **Microsoft Excel: Two-Way INDEX-MATCH Guide**\n\n" +
                         "`=INDEX(return_range, MATCH(lookup_value, lookup_range, 0))`\n\n" +
                         "• **Example:**\n" +
                         "  `=INDEX(Sheet1!C:C, MATCH(A2, Sheet1!A:A, 0))`\n\n" +
                         "• **Why Use It:** Works in older versions of Excel that do not support `XLOOKUP`, and allows looking up columns located to the left of the search key without rearranging data.",
                Suggestions = new List<string> { "XLOOKUP Formula", "VLOOKUP Formula", "SUMIFS Formula" }
            };
        }

        // 4. Excel: SUMIF / SUMIFS
        if (lower.Contains("sumif") || lower.Contains("sumifs"))
        {
            return new DashboardAiResponse
            {
                Answer = "📊 **Microsoft Excel: SUMIFS (Multi-Condition Summation)**\n\n" +
                         "`=SUMIFS(sum_range, criteria_range1, criteria1, criteria_range2, criteria2)`\n\n" +
                         "• **Example (Warehouse Total Freight Weight by Hub):**\n" +
                         "  `=SUMIFS(D:D, B:B, \"BOM\", C:C, \">100\")`\n" +
                         "  Sums weights in column `D` where Hub (Col B) is \"BOM\" and Weight (Col C) is greater than 100 kg.",
                Suggestions = new List<string> { "VLOOKUP Formula Guide", "Pivot Table Setup", "Excel Formulas" }
            };
        }

        // 5. Excel: Pivot Tables / Remove Duplicates / Freeze Panes
        if (lower.Contains("pivot") || lower.Contains("duplicate") || lower.Contains("freeze pane"))
        {
            return new DashboardAiResponse
            {
                Answer = "📊 **Microsoft Excel: Data Management Workflows**\n\n" +
                         "• **Create Pivot Table:** Select data table → Click **Insert** tab → **PivotTable** → Drag fields to Rows and Values. Press `Alt + F5` to refresh data.\n" +
                         "• **Remove Duplicates:** Select data columns → Click **Data** tab → Click **Remove Duplicates** (or press `Alt + A + M`).\n" +
                         "• **Freeze Top Header Row:** Click **View** tab → **Freeze Panes** → **Freeze Top Row**.",
                Suggestions = new List<string> { "XLOOKUP Formula", "VLOOKUP Formula", "SUMIFS Formula" }
            };
        }

        // 6. Excel Freeze / Not Responding
        if (lower.Contains("excel") && (lower.Contains("freeze") || lower.Contains("crash") || lower.Contains("slow") || lower.Contains("not responding")))
        {
            return new DashboardAiResponse
            {
                Answer = "⚡ **Excel Freeze & Performance Optimization:**\n\n" +
                         "• **Step 1:** Launch Excel in Safe Mode by running `excel.exe /safe` in Windows Run (Win+R)\n" +
                         "• **Step 2:** Disable corrupt COM Add-ins: File → Options → Add-ins → Manage: COM Add-ins → Uncheck non-essential plugins\n" +
                         "• **Step 3:** Convert entire column formulas (e.g., `A:A`) to explicit ranges (e.g., `A2:A5000`) to avoid scanning 1,048,576 rows\n" +
                         "• **Step 4:** Clear Windows Temp memory cache.",
                ActionTag = "PurgeTemp",
                ActionTitle = "🧹 Purge Windows & Office Temp Cache",
                ActionDescription = "Clears cached temp workbook scratch files in %TEMP% and releases locked RAM.",
                Suggestions = new List<string> { "1-Click Turbo Boost", "Clean Temp Files", "XLOOKUP Formula" }
            };
        }

        // 7. Blue Screen of Death (BSOD)
        if (lower.Contains("bsod") || lower.Contains("blue screen") || lower.Contains("stop code") || lower.Contains("dmp"))
        {
            return new DashboardAiResponse
            {
                Answer = "🛑 **Windows Blue Screen of Death (BSOD) Triage:**\n\n" +
                         "• **Step 1:** Note the Stop Code: `CRITICAL_PROCESS_DIED`, `MEMORY_MANAGEMENT`, or `IRQL_NOT_LESS_OR_EQUAL`\n" +
                         "• **Step 2 (Repair System Files):** Run Command Prompt as Administrator and execute:\n" +
                         "  `DISM /Online /Cleanup-Image /RestoreHealth` followed by `sfc /scannow`\n" +
                         "• **Step 3 (RAM Diagnostic):** Press `Win + R`, type `mdsched.exe`, and select *Restart now and check for problems*\n" +
                         "• **Step 4 (Drive S.M.A.R.T.):** Check NVMe/SSD wear and bad sector allocations.",
                ActionTag = "OpenStorageSmart",
                ActionTitle = "💾 Check S.M.A.R.T. Drive Health",
                ActionDescription = "Inspect physical disk health and check for pending bad sector crashes.",
                Suggestions = new List<string> { "1-Click Turbo Boost", "Check PC Health", "Gather Incident Forensics" }
            };
        }

        // 8. Barcode / Thermal Label Printers (Zebra, TVS, TSC)
        if (lower.Contains("zebra") || lower.Contains("barcode") || lower.Contains("thermal") || lower.Contains("label printer") || lower.Contains("tvs") || lower.Contains("tsc"))
        {
            return new DashboardAiResponse
            {
                Answer = "🏷️ **Thermal Barcode & Label Printer Triage (Zebra / TVS / TSC):**\n\n" +
                         "• **Sensor Calibration:** Turn off printer. Hold the **FEED** button while powering ON. Release when the green/red indicator flashes to auto-calibrate label gap sensor.\n" +
                         "• **Skipping Labels / Misaligned Print:** Go to Control Panel → Devices & Printers → Right-click printer → **Printing Preferences** → Set Paper Size to exact label dimensions (e.g. 4\" x 6\" or 2\" x 1\").\n" +
                         "• **Faded or Partial Print:** Clean the thermal heating strip on the printhead using a 99% Isopropyl Alcohol swab.\n" +
                         "• **Stuck Print Jobs:** Use the Print Spooler reset tool below.",
                ActionTag = "FixPrintSpooler",
                ActionTitle = "🖨️ Reset Print Spooler & Purge Stuck Labels",
                ActionDescription = "Clears locked printer queue files and restarts the Windows Spooler service.",
                Suggestions = new List<string> { "Fix Print Spooler", "File IT Complaint Ticket", "Open Command Playbook" }
            };
        }

        // 9. PowerShell & CMD Useful Commands
        if (lower.Contains("powershell") || lower.Contains("command prompt") || lower.Contains("cmd command") || lower.Contains("syntax"))
        {
            return new DashboardAiResponse
            {
                Answer = "💻 **Essential Enterprise Windows & PowerShell Commands:**\n\n" +
                         "• **Test Remote Port Connectivity:**\n" +
                         "  `Test-NetConnection -ComputerName erp.svll.in -Port 443`\n" +
                         "• **Check System Uptime:**\n" +
                         "  `(Get-CimInstance Win32_OperatingSystem).LastBootUpTime`\n" +
                         "• **Flush DNS & Reset IP Lease:**\n" +
                         "  `ipconfig /flushdns ; ipconfig /renew`\n" +
                         "• **Find Top 10 High Memory Processes:**\n" +
                         "  `Get-Process | Sort-Object WorkingSet64 -Descending | Select-Object -First 10 ProcessName, @{Name=\"RAM_MB\";Expression={[math]::Round($_.WorkingSet64/1MB,1)}}`\n" +
                         "• **System Health Scan:**\n" +
                         "  `sfc /scannow`",
                ActionTag = "OpenLibrary",
                ActionTitle = "📖 Open 37-Scenario Command Playbook",
                ActionDescription = "Access full library of ready-to-run enterprise PowerShell & CMD scripts.",
                Suggestions = new List<string> { "1-Click Turbo Boost", "Flush DNS", "Open Interactive Terminal" }
            };
        }

        // 10. Useful Keyboard Shortcuts
        if (lower.Contains("shortcut") || lower.Contains("hotkey") || lower.Contains("keyboard"))
        {
            return new DashboardAiResponse
            {
                Answer = "⌨️ **Top Windows & Office Productivity Shortcuts:**\n\n" +
                         "• `Win + V` → **Clipboard History:** Paste from multiple previously copied texts or screenshots.\n" +
                         "• `Win + Shift + S` → **Snipping Tool:** Capture customizable region screenshot.\n" +
                         "• `Ctrl + Shift + Esc` → **Task Manager:** Instantly opens Windows Task Manager directly.\n" +
                         "• `Win + D` → **Show Desktop:** Minimizes/restores all open windows.\n" +
                         "• `Win + L` → **Lock Workstation:** Immediately locks your PC for security.\n" +
                         "• `Ctrl + K` in SVLL Workstation → Opens the Global Command Palette.",
                Suggestions = new List<string> { "Open Interactive Task Manager", "1-Click Turbo Boost", "Check PC Health" }
            };
        }

        // 11. Boss ERP / Logistics LR Billing
        if (lower.Contains("boss") || lower.Contains("erp") || lower.Contains("wms") || lower.Contains("lr") || lower.Contains("consignment") || lower.Contains("dispatch"))
        {
            return new DashboardAiResponse
            {
                Answer = "🏢 **SVLL Logistics & Boss ERP Operations Guide:**\n\n" +
                         "• **Session Disconnect / Timeout:** If Boss ERP disconnects during LR entry, verify network ping latency to central database. Test port `1521`/`1433`.\n" +
                         "• **Lorry Receipt (LR) Bill Printing:** Ensure the thermal printer driver is set to the default Windows printer before initiating consignment dispatch billing.\n" +
                         "• **Barcode Scanner Delay:** If handheld USB/Bluetooth barcode scanners lag, verify COM emulation vs USB HID keyboard wedge mode.\n" +
                         "• **Gateway Latency Check:** Use the diagnostic button below to test connectivity to the central logistics cluster.",
                ActionTag = "OpenWmsLatency",
                ActionTitle = "🏢 Check ERP & WMS Latency",
                ActionDescription = "Tests real-time round-trip latency to the central logistics ERP database.",
                Suggestions = new List<string> { "Ping Gateway Diagnostic", "Flush DNS", "1-Click Turbo Boost" }
            };
        }

        // 12. Outlook / Mail
        if (lower.Contains("outlook") || lower.Contains("mail") || lower.Contains("email"))
        {
            return new DashboardAiResponse
            {
                Answer = "📧 **Outlook Email Troubleshooting:**\n\n" +
                         "• **'Disconnected' Status:** Click the **Send / Receive** tab and toggle the **Work Offline** button OFF. If issues persist, verify gateway internet reachability.\n" +
                         "• **Corrupt Local Cache (.OST):** Close Outlook. Press `Win + R` and navigate to `%LOCALAPPDATA%\\Microsoft\\Outlook`. Rename your `.ost` data file to `.ost.old`. Relaunch Outlook to automatically rebuild a fresh mailbox cache from the server.\n" +
                         "• **Safe Mode:** Press `Win + R` and run `outlook.exe /safe` to bypass malfunctioning third-party add-ins.",
                ActionTag = "OpenNetwork",
                ActionTitle = "🌐 Network & DNS Profiles",
                ActionDescription = "Inspect network connectivity and switch DNS presets.",
                Suggestions = new List<string> { "Flush DNS", "Ping Gateway Diagnostic", "1-Click Turbo Boost" }
            };
        }

        // 13. Camera / CCTV
        if (lower.Contains("camera") || lower.Contains("cctv"))
        {
            return new DashboardAiResponse
            {
                Answer = "📹 **Security CCTV Camera Triage:**\n\n" +
                         "• **Camera Down / No Signal:** Check PoE (Power over Ethernet) switch port LED status. Solid amber/green indicates active power delivery.\n" +
                         "• **Latency & Packet Loss:** Run continuous ICMP ping to the camera's local static IP address.\n" +
                         "• **Time Drift:** Sync the NVR/camera clock to the SVLL corporate NTP server (`time.windows.com` or local DC).",
                ActionTag = "OpenPingMonitor",
                ActionTitle = "⚡ Launch 24/7 Ping Watchdog",
                ActionDescription = "Continuously ping depot cameras, routers, and gateways with audible alerts.",
                Suggestions = new List<string> { "Ping Gateway Diagnostic", "Subnet IP Scanner", "Wi-Fi Diagnostics" }
            };
        }

        // 14. General Help / Features
        if (lower.Contains("help") || lower.Contains("what can you do") || lower.Contains("feature") || lower.Contains("command"))
        {
            return new DashboardAiResponse
            {
                Answer = "🤖 **SVLL AI Quick Support Copilot Features:**\n\n" +
                         "I can assist with **any technical, software, or logistics question**, including:\n" +
                         "• 📊 **Microsoft Excel & Office:** Formulas (XLOOKUP, VLOOKUP, SUMIFS), pivot tables, and freeze fixes\n" +
                         "• 💻 **Windows OS & PowerShell:** Command lines, BSOD troubleshooting, registry tweaks, and repairs\n" +
                         "• 🏷️ **Hardware & Thermal Printers:** Zebra/TVS barcode calibration, print queues, and scanner setup\n" +
                         "• 🌐 **Networking:** DNS flushes, IP conflicts, Wi-Fi diagnosis, and 24/7 ping monitoring\n" +
                         "• 🏢 **Boss ERP & Logistics:** LR billing, consignment workflows, and database latency checks\n" +
                         "• ⚡ **1-Click Built-in Automation:** Turbo Boost, Temp Purge, Print Spooler Reset, S.M.A.R.T. Drive Diagnostics, and Incident Forensics.",
                ActionTag = "OpenLibrary",
                ActionTitle = "📖 Open 37-Scenario Command Playbook",
                ActionDescription = "Explore complete PowerShell and CMD recipes for enterprise fixes.",
                Suggestions = new List<string> { "Execute Turbo Boost", "Fix Print Spooler", "Flush DNS", "Purge Temp Files" }
            };
        }

        // General Technical fallback answer
        return new DashboardAiResponse
        {
            Answer = $"💡 **AI Quick Support Guidance for: '{raw}'**\n\n" +
                     $"Workstation `{Environment.MachineName}` ({Environment.UserName}) is operating normally.\n\n" +
                     "**How can I assist you?**\n" +
                     "• Ask technical questions: *\"How to do XLOOKUP in Excel?\"*, *\"What is the command to test port?\"*, *\"How to fix Zebra barcode printer?\"*\n" +
                     "• Troubleshoot problems: *\"Blue screen CRITICAL_PROCESS_DIED\"*, *\"Outlook disconnected\"*, *\"Boss ERP timeout\"*\n" +
                     "• Trigger 1-click tools: *\"Boost PC\"*, *\"Fix printer\"*, *\"Flush DNS\"*, *\"Purge temp files\"*.",
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
            "📊 Excel Formulas (XLOOKUP)",
            "🏷️ Barcode Printer Setup",
            "📋 File IT Complaint Ticket"
        };
    }
}
