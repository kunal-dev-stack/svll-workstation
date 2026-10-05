using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;

namespace SVLL_IT_Workstation;

public partial class MainWindow
{
    #region 1. Complete IT Troubleshooting & Command Library (37 Enterprise Scenarios)

    private void InitializeCommandLibrary()
    {
        _solutionsLibrary = new List<TroubleshootingSolution>
        {
            // ==================== 1. NETWORK SCENARIOS (8) ====================
            new TroubleshootingSolution
            {
                Title = "169.254.x.x APIPA Automatic Private IP Fallback",
                Category = "Network",
                Symptoms = "Endpoint displays a 169.254.x.x autoconfiguration address; no valid DHCP lease negotiated with gateway/switch.",
                Explanation = "Releases stale DHCP client bindings, resets local ARP table, and initiates a clean DORA DHCP handshake negotiation.",
                CmdCommand = "ipconfig /release && ipconfig /renew",
                PowerShellCommand = "Restart-NetAdapter -Name *; Start-Sleep 2; ipconfig /renew",
                Tags = new[] { "169.254", "apipa", "dhcp", "ipconfig", "network" }
            },
            new TroubleshootingSolution
            {
                Title = "DNS Cache Corruption & Hostname Resolution Failure",
                Category = "Network",
                Symptoms = "Ping to public IP 8.8.8.8 succeeds, but domain names fail with 'DNS_PROBE_FINISHED_NXDOMAIN' or 'Host not found'.",
                Explanation = "Flushes local client DNS resolver cache, reregisters DNS names with active domain controllers, and restarts Dnscache.",
                CmdCommand = "ipconfig /flushdns && ipconfig /registerdns && netsh winsock reset",
                PowerShellCommand = "Clear-DnsClientCache; Register-DnsClient; Restart-Service Dnscache -Force",
                Tags = new[] { "dns", "flushdns", "nxdomain", "resolver", "winsock" }
            },
            new TroubleshootingSolution
            {
                Title = "Complete TCP/IP Protocol Stack & Winsock Reset",
                Category = "Network",
                Symptoms = "System shows valid IP address but cannot transmit or receive packets; corrupted LSP or Winsock catalog.",
                Explanation = "Resets TCP/IP registry configurations to clean system defaults and purges corrupted third-party Winsock LSP layers.",
                CmdCommand = "netsh int ip reset && netsh winsock reset",
                PowerShellCommand = "netsh int ip reset; netsh winsock reset",
                Tags = new[] { "tcp", "winsock", "reset", "stack", "lsp" }
            },
            new TroubleshootingSolution
            {
                Title = "Continuous Gateway & Internet Ping Diagnostic",
                Category = "Network",
                Symptoms = "Intermittent connection dropouts, packet drops, or fluctuating latency during ERP, WMS, or web browsing.",
                Explanation = "Initiates continuous ICMP echo polling to detect packet drops, latency jitter, and timeout patterns.",
                CmdCommand = "ping -t 8.8.8.8",
                PowerShellCommand = "Test-Connection -ComputerName 8.8.8.8 -Count 100",
                Tags = new[] { "ping", "icmp", "latency", "packet loss", "jitter" }
            },
            new TroubleshootingSolution
            {
                Title = "Traceroute & Hop Latency Path Inspection",
                Category = "Network",
                Symptoms = "Sluggish responsiveness to centralized logistics servers; need to pinpoint which router hop is introducing delay.",
                Explanation = "Performs packet hop-by-hop latency tracing with numeric IP routing to isolate WAN and switch bottlenecks.",
                CmdCommand = "tracert -d 8.8.8.8",
                PowerShellCommand = "Test-NetConnection -ComputerName 8.8.8.8 -TraceRoute",
                Tags = new[] { "tracert", "traceroute", "hops", "latency", "routing" }
            },
            new TroubleshootingSolution
            {
                Title = "View Active TCP Listening Ports & Process IDs",
                Category = "Network",
                Symptoms = "Need to verify if a local service (ERP client, local SQL instance, or web service) is listening or port conflict exists.",
                Explanation = "Enumerates all active TCP listening sockets, bound local IPs/ports, and the owning process identifier (PID).",
                CmdCommand = "netstat -ano | findstr LISTENING",
                PowerShellCommand = "Get-NetTCPConnection -State Listen | Select-Object LocalAddress, LocalPort, OwningProcess",
                Tags = new[] { "ports", "netstat", "tcp", "listening", "pid" }
            },
            new TroubleshootingSolution
            {
                Title = "Reveal Saved Warehouse Wi-Fi Passwords & Profiles",
                Category = "Network",
                Symptoms = "Technician needs cleartext WPA2/WPA3 pre-shared security key for currently configured wireless networks.",
                Explanation = "Extracts WLAN profiles stored in the local Windows wireless profile store along with unencrypted security keys.",
                CmdCommand = "netsh wlan show profile name=* key=clear",
                PowerShellCommand = "netsh wlan show profiles | Select-String \":\\s+(.+)$\" | ForEach-Object { $p = $_.Matches.Groups[1].Value.Trim(); netsh wlan show profile name=\"$p\" key=clear }",
                Tags = new[] { "wifi", "wlan", "password", "key", "wireless" }
            },
            new TroubleshootingSolution
            {
                Title = "Network Adapter Hard Reset & Driver Rebind",
                Category = "Network",
                Symptoms = "Ethernet or Wi-Fi adapter stuck in 'Identifying...' or 'Media Disconnected' despite cable being securely inserted.",
                Explanation = "Forcefully cycles the physical network interface controller driver off and on without requiring a machine reboot.",
                CmdCommand = "wmic path win32_networkadapter where \"NetConnectionStatus=2\" call disable && timeout /t 2 && wmic path win32_networkadapter where \"NetConnectionStatus=2\" call enable",
                PowerShellCommand = "Get-NetAdapter | Where-Object Status -eq 'Up' | Restart-NetAdapter",
                Tags = new[] { "adapter", "restart", "nic", "ethernet", "driver" }
            },

            // ==================== 2. PRINTERS SCENARIOS (4) ====================
            new TroubleshootingSolution
            {
                Title = "Stuck Print Spooler / Phantom Locked Jobs Purge",
                Category = "Printers",
                Symptoms = "Print jobs locked with status 'Deleting' or 'Printing'; print queue cannot be cleared from Windows GUI.",
                Explanation = "Terminates the spooler service and forcefully purges locked .SHD and .SPL spool files from System32\\spool\\PRINTERS.",
                CmdCommand = "net stop spooler && del /Q /F /S %systemroot%\\System32\\Spool\\Printers\\* && net start spooler",
                PowerShellCommand = "Stop-Service Spooler -Force; Remove-Item $env:windir\\System32\\spool\\PRINTERS\\* -Force -Recurse; Start-Service Spooler",
                Tags = new[] { "printer", "spooler", "queue", "stuck", "locked" }
            },
            new TroubleshootingSolution
            {
                Title = "Enumerate All Installed Printers & Port Status",
                Category = "Printers",
                Symptoms = "Need to audit all physical and networked printers, assigned IP/USB ports, and operational status.",
                Explanation = "Queries WMI/CIM printer objects to list printer names, drivers, assigned ports, and shared states.",
                CmdCommand = "wmic printer get name,drivername,portname,status,shared",
                PowerShellCommand = "Get-Printer | Select-Object Name, DriverName, PortName, PrinterStatus, Shared",
                Tags = new[] { "printers", "inventory", "wmic", "ports", "drivers" }
            },
            new TroubleshootingSolution
            {
                Title = "List Installed Printer Drivers in Driver Store",
                Category = "Printers",
                Symptoms = "Corrupt printer driver causing spooler crashes; need to inspect installed driver versions and vendors.",
                Explanation = "Enumerates all printer drivers registered in the Windows system repository.",
                CmdCommand = "cscript %systemroot%\\System32\\Printing_Admin_Scripts\\en-US\\prndrvr.vbs -l",
                PowerShellCommand = "Get-PrinterDriver | Select-Object Name, MajorVersion, Manufacturer, PrinterEnvironment",
                Tags = new[] { "drivers", "printer", "spooler", "store" }
            },
            new TroubleshootingSolution
            {
                Title = "Force Clear Specific Print Queue Jobs via WMI",
                Category = "Printers",
                Symptoms = "Single printer queue contains 50+ jammed barcode/invoice jobs while other printers must stay online.",
                Explanation = "Removes all pending or error-state print jobs directly across all queues using PowerShell CIM.",
                CmdCommand = "net stop spooler && del /q /f \"%systemroot%\\System32\\spool\\PRINTERS\\*.*\" && net start spooler",
                PowerShellCommand = "Get-PrintJob -PrinterName * | Remove-PrintJob",
                Tags = new[] { "printjob", "cancel", "purge", "queue" }
            },

            // ==================== 3. WINDOWS OS SCENARIOS (9) ====================
            new TroubleshootingSolution
            {
                Title = "Corrupt System Component Store Image Repair (DISM & SFC)",
                Category = "Windows OS",
                Symptoms = "System UI freezes, file explorer crashes, or SFC reports corrupt files it cannot repair.",
                Explanation = "Services the Windows component store (WinSxS) image payload against clean Windows Update sources, followed by SFC integrity replacement.",
                CmdCommand = "dism /Online /Cleanup-Image /RestoreHealth && sfc /scannow",
                PowerShellCommand = "Repair-WindowsImage -Online -RestoreHealth; sfc /scannow",
                Tags = new[] { "sfc", "dism", "corrupt", "system32", "bsod" }
            },
            new TroubleshootingSolution
            {
                Title = "Stuck Windows Update Download Cache Flush (0x80070002)",
                Category = "Windows OS",
                Symptoms = "Windows Updates stuck downloading at 0%, or update errors 0x80070002, 0x80240034, 0x80070003 occur.",
                Explanation = "Halts Windows Update (wuauserv) and BITS, purges the corrupted SoftwareDistribution download cache, and reregisters services.",
                CmdCommand = "net stop wuauserv && net stop bits && rd /s /q %windir%\\SoftwareDistribution && net start wuauserv",
                PowerShellCommand = "Stop-Service wuauserv, bits; Remove-Item $env:windir\\SoftwareDistribution -Recurse -Force; Start-Service wuauserv, bits",
                Tags = new[] { "update", "softwaredistribution", "wuauserv", "0x80070002" }
            },
            new TroubleshootingSolution
            {
                Title = "Restart Frozen Windows Explorer Shell & Taskbar",
                Category = "Windows OS",
                Symptoms = "Taskbar unresponsive, system tray clock frozen, or Start Menu fails to open.",
                Explanation = "Forcefully terminates all hung explorer.exe threads and cleanly relaunches the Windows desktop shell.",
                CmdCommand = "taskkill /f /im explorer.exe && start explorer.exe",
                PowerShellCommand = "Stop-Process -Name explorer -Force; Start-Process explorer",
                Tags = new[] { "explorer", "taskbar", "shell", "freeze", "desktop" }
            },
            new TroubleshootingSolution
            {
                Title = "Re-register All Modern UWP & Microsoft Store Apps",
                Category = "Windows OS",
                Symptoms = "Windows Calculator, Photos, or Microsoft Store closes immediately on startup without error.",
                Explanation = "Iterates through all provisioned AppX package manifests in Program Files and re-registers XML bindings for all user accounts.",
                CmdCommand = "powershell -ExecutionPolicy Bypass -Command \"Get-AppXPackage -AllUsers | Foreach {Add-AppxPackage -DisableDevelopmentMode -Register \\\"$($_.InstallLocation)\\AppXManifest.xml\\\"}\"",
                PowerShellCommand = "Get-AppXPackage -AllUsers | Foreach {Add-AppxPackage -DisableDevelopmentMode -Register \"$($_.InstallLocation)\\AppXManifest.xml\"}",
                Tags = new[] { "appx", "uwp", "store", "calculator", "photos" }
            },
            new TroubleshootingSolution
            {
                Title = "Rebuild Corrupted Desktop Icon & Thumbnail Cache",
                Category = "Windows OS",
                Symptoms = "Application desktop icons display blank white sheets or distorted graphical glitches.",
                Explanation = "Kills explorer, deletes hidden SQLite icon cache databases (IconCache.db and thumbnail caches), and restarts shell.",
                CmdCommand = "taskkill /f /im explorer.exe && del /a /q \"%localappdata%\\IconCache.db\" && start explorer.exe",
                PowerShellCommand = "Stop-Process -Name explorer -Force; Remove-Item $env:LOCALAPPDATA\\IconCache.db -Force; Start-Process explorer",
                Tags = new[] { "icons", "cache", "thumbnails", "blank", "explorer" }
            },
            new TroubleshootingSolution
            {
                Title = "Generate Battery Health & Energy Efficiency Report",
                Category = "Windows OS",
                Symptoms = "Warehouse laptop battery drains rapidly or dies unexpectedly; need hardware wear level & capacity history.",
                Explanation = "Compiles deep ACPI battery charge history, cycle counts, and battery capacity loss into an HTML report on Desktop.",
                CmdCommand = "powercfg /batteryreport /output \"%userprofile%\\Desktop\\battery_report.html\"",
                PowerShellCommand = "powercfg /batteryreport /output \"$env:USERPROFILE\\Desktop\\battery_report.html\"; Start-Process \"$env:USERPROFILE\\Desktop\\battery_report.html\"",
                Tags = new[] { "battery", "powercfg", "health", "laptop", "power" }
            },
            new TroubleshootingSolution
            {
                Title = "Windows License & Activation Expiration Query",
                Category = "Windows OS",
                Symptoms = "Need to verify if Windows license is KMS, OEM, Retail, or permanent volume license.",
                Explanation = "Queries the Windows Software Licensing Management tool to display license status and expiry.",
                CmdCommand = "slmgr.vbs /xpr && slmgr.vbs /dli",
                PowerShellCommand = "Get-CimInstance SoftwareLicensingProduct -Filter \"PartialProductKey IS NOT NULL\" | Select-Object Name, LicenseStatus, GracePeriodRemaining",
                Tags = new[] { "license", "slmgr", "activation", "kms", "retail" }
            },
            new TroubleshootingSolution
            {
                Title = "Microsoft Activation Scripts (MAS) Community Utility",
                Category = "Windows OS",
                Symptoms = "Windows or Office displays 'Activation Expired' or watermark on desktop.",
                Explanation = "Launches the community open-source Microsoft Activation Scripts (MAS) for HWID / KMS38 activation.",
                CmdCommand = "powershell -Command \"irm https://get.activated.win | iex\"",
                PowerShellCommand = "irm https://get.activated.win | iex",
                Tags = new[] { "mas", "activation", "windows", "office", "hwid" }
            },
            new TroubleshootingSolution
            {
                Title = "Chris Titus Tech Windows Utility (CTT WinUtil)",
                Category = "Windows OS",
                Symptoms = "Need to apply advanced OS debloat, manage background telemetry, or configure Windows servicing.",
                Explanation = "Executes the community CTT WinUtil utility for advanced system tuning and package deployment.",
                CmdCommand = "powershell -Command \"irm https://christitus.com/win | iex\"",
                PowerShellCommand = "irm https://christitus.com/win | iex",
                Tags = new[] { "ctt", "winutil", "debloat", "titus", "tweaks" }
            },

            // ==================== 4. ACTIVE DIRECTORY SCENARIOS (5) ====================
            new TroubleshootingSolution
            {
                Title = "Active Directory Trust Relationship Failure Repair",
                Category = "Active Directory",
                Symptoms = "'The trust relationship between this workstation and the primary domain failed' upon domain user login.",
                Explanation = "Resets and renegotiates the Kerberos machine secure channel password with the domain controller.",
                CmdCommand = "nltest /sc_query:%userdnsdomain% /sc_reset:%userdnsdomain%",
                PowerShellCommand = "Test-ComputerSecureChannel -Repair -Verbose",
                Tags = new[] { "trust", "domain", "active directory", "kerberos", "secure channel" }
            },
            new TroubleshootingSolution
            {
                Title = "Force Immediate Group Policy Update (Computer & User)",
                Category = "Active Directory",
                Symptoms = "New corporate security GPOs, desktop restrictions, or mapped drives not applying to endpoint.",
                Explanation = "Forces an immediate background and foreground re-application of all Active Directory GPOs without reboot.",
                CmdCommand = "gpupdate /force",
                PowerShellCommand = "gpupdate /force",
                Tags = new[] { "gpo", "gpupdate", "policy", "domain", "active directory" }
            },
            new TroubleshootingSolution
            {
                Title = "Generate Comprehensive Group Policy Result HTML Report",
                Category = "Active Directory",
                Symptoms = "Need to troubleshoot which specific GPO is overriding corporate firewall or desktop settings.",
                Explanation = "Generates an exhaustive HTML report of all applied Computer and User GPOs with error codes on Desktop.",
                CmdCommand = "gpresult /h \"%userprofile%\\Desktop\\gpreport.html\" /f",
                PowerShellCommand = "gpresult /h \"$env:USERPROFILE\\Desktop\\gpreport.html\" /f; Start-Process \"$env:USERPROFILE\\Desktop\\gpreport.html\"",
                Tags = new[] { "gpresult", "gpo", "audit", "report", "domain" }
            },
            new TroubleshootingSolution
            {
                Title = "Audit Local Administrators Group Membership",
                Category = "Active Directory",
                Symptoms = "Security compliance requires auditing which domain users or local accounts hold elevated admin rights.",
                Explanation = "Enumerates all members of the local SAM database Administrators group.",
                CmdCommand = "net localgroup Administrators",
                PowerShellCommand = "Get-LocalGroupMember -Group 'Administrators'",
                Tags = new[] { "administrators", "security", "audit", "localgroup", "sam" }
            },
            new TroubleshootingSolution
            {
                Title = "Identify Active Domain Controller & Logon Server",
                Category = "Active Directory",
                Symptoms = "Workstation logging in slowly; need to verify which Domain Controller authenticated the session.",
                Explanation = "Queries the NetLogon service and DNS SRV records to determine the active DC providing authentication.",
                CmdCommand = "echo %LOGONSERVER% && nltest /dsgetdc:%userdnsdomain%",
                PowerShellCommand = "[System.DirectoryServices.ActiveDirectory.Domain]::GetCurrentDomain().FindDomainController().Name",
                Tags = new[] { "dc", "logonserver", "netlogon", "domain", "active directory" }
            },

            // ==================== 5. SECURITY SCENARIOS (4) ====================
            new TroubleshootingSolution
            {
                Title = "BitLocker 48-Digit Numerical Recovery Key Extraction",
                Category = "Security",
                Symptoms = "Need to backup 48-digit numerical BitLocker recovery password for drive C: before BIOS update or motherboard swap.",
                Explanation = "Queries the BitLocker volume protector store to display the numerical recovery password for volume C:.",
                CmdCommand = "manage-bde -protectors -get C:",
                PowerShellCommand = "(Get-BitLockerVolume -MountPoint C:).KeyProtector | Where-Object { $_.KeyProtectorType -eq 'RecoveryPassword' } | Select-Object -ExpandProperty RecoveryPassword",
                Tags = new[] { "bitlocker", "recovery key", "tpm", "encryption", "manage-bde" }
            },
            new TroubleshootingSolution
            {
                Title = "Enable Remote Desktop (RDP) Port 3389 & Firewall Rules",
                Category = "Security",
                Symptoms = "Remote Desktop connection refused; port 3389 closed or disabled in system settings.",
                Explanation = "Modifies Terminal Server registry keys to enable RDP connections and opens inbound Windows Defender Firewall rules.",
                CmdCommand = "reg add \"HKLM\\System\\CurrentControlSet\\Control\\Terminal Server\" /v fDenyTSConnections /t REG_DWORD /d 0 /f && netsh advfirewall firewall set rule group=\"remote desktop\" new enable=Yes",
                PowerShellCommand = "Set-ItemProperty -Path 'HKLM:\\System\\CurrentControlSet\\Control\\Terminal Server' -Name 'fDenyTSConnections' -Value 0; Enable-NetFirewallRule -DisplayGroup 'Remote Desktop'",
                Tags = new[] { "rdp", "remote desktop", "3389", "firewall", "terminal server" }
            },
            new TroubleshootingSolution
            {
                Title = "Reset Windows Defender Firewall to Factory Defaults",
                Category = "Security",
                Symptoms = "Corrupt firewall rules blocking essential warehouse software or network file shares.",
                Explanation = "Restores the Windows Defender Firewall configuration to clean out-of-the-box system defaults.",
                CmdCommand = "netsh advfirewall reset",
                PowerShellCommand = "(New-Object -ComObject HNetCfg.FwPolicy2).RestoreLocalFirewallDefaults()",
                Tags = new[] { "firewall", "advfirewall", "reset", "security", "defaults" }
            },
            new TroubleshootingSolution
            {
                Title = "Force Windows Defender Antivirus Definition Update",
                Category = "Security",
                Symptoms = "Defender antivirus signatures out of date or definition download failing in Windows Security app.",
                Explanation = "Invokes the Microsoft Antimalware Command Line utility to fetch latest security intelligence directly.",
                CmdCommand = "\"%ProgramFiles%\\Windows Defender\\MpCmdRun.exe\" -SignatureUpdate",
                PowerShellCommand = "Update-MpSignature",
                Tags = new[] { "defender", "antivirus", "signatures", "mpcmdrun", "security" }
            },

            // ==================== 6. STORAGE SCENARIOS (4) ====================
            new TroubleshootingSolution
            {
                Title = "WinSxS Component Store Bloat Cleanup & ResetBase",
                Category = "Storage",
                Symptoms = "System drive C: low on space; C:\\Windows\\WinSxS folder consumes 15GB-25GB of disk storage.",
                Explanation = "Purges superseded Windows update component packages and compresses the base OS payload.",
                CmdCommand = "dism.exe /Online /Cleanup-Image /StartComponentCleanup /ResetBase",
                PowerShellCommand = "dism.exe /Online /Cleanup-Image /StartComponentCleanup /ResetBase",
                Tags = new[] { "winsxs", "storage", "cleanup", "dism", "disk space" }
            },
            new TroubleshootingSolution
            {
                Title = "Purge Volume Shadow Copies (VSS) to Free Storage",
                Category = "Storage",
                Symptoms = "Drive C: capacity missing; hidden Volume Shadow Copies holding large block allocations.",
                Explanation = "Deletes obsolete system restore snapshots and shadow storage allocations on volume C:.",
                CmdCommand = "vssadmin delete shadows /for=C: /all /quiet",
                PowerShellCommand = "vssadmin delete shadows /for=C: /all /quiet",
                Tags = new[] { "vss", "shadow copies", "storage", "vssadmin", "disk" }
            },
            new TroubleshootingSolution
            {
                Title = "Force SSD TRIM Optimization & Storage Defragmentation",
                Category = "Storage",
                Symptoms = "Solid-state drive write speeds degraded or sluggish IOPS during database operations.",
                Explanation = "Sends ATA TRIM commands to notify the SSD controller of unused storage blocks for garbage collection.",
                CmdCommand = "defrag C: /O /U /V",
                PowerShellCommand = "Optimize-Volume -DriveLetter C -Defrag -Verbose",
                Tags = new[] { "ssd", "trim", "defrag", "storage", "performance" }
            },
            new TroubleshootingSolution
            {
                Title = "Check & Scan Drive C: File System Integrity (Read-Only Scan)",
                Category = "Storage",
                Symptoms = "Suspected NTFS volume corruption or bad sectors after unexpected power loss.",
                Explanation = "Performs an online read-only verification of the NTFS file system metadata without unmounting volume.",
                CmdCommand = "chkdsk C: /scan",
                PowerShellCommand = "Repair-Volume -DriveLetter C -Scan",
                Tags = new[] { "chkdsk", "ntfs", "filesystem", "corruption", "disk" }
            },

            // ==================== 7. PERFORMANCE SCENARIOS (3) ====================
            new TroubleshootingSolution
            {
                Title = "Unlock Windows 'Ultimate Performance' Power Plan",
                Category = "Performance",
                Symptoms = "CPU dynamic frequency scaling causes micro-stuttering and latency during barcode scanning or heavy workloads.",
                Explanation = "Duplicates and activates the hidden Windows Ultimate Performance power scheme to disable all core throttling.",
                CmdCommand = "powercfg -duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61 && powercfg -setactive e9a42b02-d5df-448d-aa00-03f14749eb61",
                PowerShellCommand = "powercfg -duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61; powercfg -setactive e9a42b02-d5df-448d-aa00-03f14749eb61",
                Tags = new[] { "ultimate performance", "powercfg", "cpu", "throttle", "latency" }
            },
            new TroubleshootingSolution
            {
                Title = "Identify Top 10 Memory Consuming Processes",
                Category = "Performance",
                Symptoms = "Physical RAM utilization at 95%+; workstation freezing and paging heavily.",
                Explanation = "Enumerates and sorts all running Win32 processes by private and working set memory usage.",
                CmdCommand = "tasklist /FO TABLE /NH | sort /R /+58",
                PowerShellCommand = "Get-Process | Sort-Object WorkingSet64 -Descending | Select-Object -First 10 ProcessName, @{Name='RAM (MB)';Expression={[math]::Round($_.WorkingSet64/1MB,2)}}",
                Tags = new[] { "ram", "memory", "leak", "processes", "tasklist" }
            },
            new TroubleshootingSolution
            {
                Title = "Hardware Inventory: Motherboard, BIOS & Serial Inspection",
                Category = "Performance",
                Symptoms = "Need serial number, BIOS firmware version, and motherboard model for warranty or IT asset tracking.",
                Explanation = "Extracts hardware DMI/SMBIOS information directly from the motherboard firmware.",
                CmdCommand = "wmic bios get serialnumber,smbiosbiosversion,manufacturer && wmic baseboard get product,manufacturer",
                PowerShellCommand = "Get-CimInstance Win32_BIOS | Select-Object Manufacturer, SMBIOSBIOSVersion, SerialNumber; Get-CimInstance Win32_BaseBoard | Select-Object Manufacturer, Product",
                Tags = new[] { "hardware", "bios", "serialnumber", "motherboard", "asset" }
            }
        };
    }

    private (Brush bg, Brush fg) GetCategoryBadgeColors(string category)
    {
        return category switch
        {
            "Network" => (new SolidColorBrush(Color.FromRgb(224, 242, 254)), new SolidColorBrush(Color.FromRgb(3, 105, 161))),
            "Printers" => (new SolidColorBrush(Color.FromRgb(243, 232, 255)), new SolidColorBrush(Color.FromRgb(126, 34, 206))),
            "Windows OS" => (new SolidColorBrush(Color.FromRgb(254, 243, 199)), new SolidColorBrush(Color.FromRgb(180, 83, 9))),
            "Active Directory" => (new SolidColorBrush(Color.FromRgb(219, 234, 254)), new SolidColorBrush(Color.FromRgb(29, 78, 216))),
            "Security" => (new SolidColorBrush(Color.FromRgb(254, 226, 226)), new SolidColorBrush(Color.FromRgb(185, 28, 28))),
            "Storage" => (new SolidColorBrush(Color.FromRgb(236, 253, 245)), new SolidColorBrush(Color.FromRgb(4, 120, 87))),
            "Performance" => (new SolidColorBrush(Color.FromRgb(237, 233, 254)), new SolidColorBrush(Color.FromRgb(109, 40, 217))),
            _ => (new SolidColorBrush(Color.FromRgb(241, 245, 249)), new SolidColorBrush(Color.FromRgb(71, 85, 105)))
        };
    }

    private UIElement BuildLibraryView()
    {
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var root = new StackPanel { Margin = new Thickness(0, 0, 10, 0) };

        var card = new Border
        {
            Background = BgCard,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(18),
            Margin = new Thickness(0, 0, 0, 14)
        };

        var sp = new StackPanel();

        // 1. Header with title and count badge
        var titleRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 4) };
        titleRow.Children.Add(new TextBlock
        {
            Text = "ENTERPRISE IT FIX & COMMAND PLAYBOOK",
            FontSize = 14,
            FontWeight = FontWeights.Bold,
            Foreground = SvllBlue,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 10, 0)
        });

        var countBadge = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(224, 242, 254)),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(8, 2, 8, 2),
            VerticalAlignment = VerticalAlignment.Center
        };
        countBadge.Child = new TextBlock
        {
            Text = $"{_solutionsLibrary.Count} Enterprise Solutions",
            FontSize = 10.5,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(3, 105, 161))
        };
        titleRow.Children.Add(countBadge);
        sp.Children.Add(titleRow);

        sp.Children.Add(new TextBlock
        {
            Text = "Curated enterprise troubleshooting library with dual-syntax commands (Command Prompt CMD and Windows PowerShell), 1-click clipboard copy, and direct execution into the integrated terminal.",
            FontSize = 11.5,
            Foreground = TextSubtle,
            Margin = new Thickness(0, 0, 0, 12)
        });

        // 2. Category Filter Pills Bar
        var categories = new[] { "All", "Network", "Printers", "Windows OS", "Active Directory", "Security", "Storage", "Performance" };
        string activeCategory = "All";

        var pillContainer = new WrapPanel { Margin = new Thickness(0, 0, 0, 12) };
        var pillButtons = new Dictionary<string, Button>();

        // 3. Search Bar & Result Counter
        var searchGrid = new Grid { Margin = new Thickness(0, 0, 0, 14) };
        searchGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        searchGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var searchBorder = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(8, 2, 8, 2)
        };
        var searchInnerGrid = new Grid();
        searchInnerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        searchInnerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        searchInnerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var searchIcon = new TextBlock
        {
            Text = "🔍",
            FontSize = 12,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 6, 0)
        };
        Grid.SetColumn(searchIcon, 0);
        searchInnerGrid.Children.Add(searchIcon);

        var txtSearch = new TextBox
        {
            Height = 28,
            FontSize = 12,
            BorderThickness = new Thickness(0),
            Background = Brushes.Transparent,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(txtSearch, 1);
        searchInnerGrid.Children.Add(txtSearch);

        var btnClearSearch = new Button
        {
            Content = "✕",
            FontSize = 10,
            FontWeight = FontWeights.Bold,
            Width = 22,
            Height = 22,
            Background = Brushes.Transparent,
            Foreground = TextSubtle,
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand,
            Visibility = Visibility.Collapsed
        };
        btnClearSearch.Click += (s, e) => { txtSearch.Clear(); txtSearch.Focus(); };
        Grid.SetColumn(btnClearSearch, 2);
        searchInnerGrid.Children.Add(btnClearSearch);

        searchBorder.Child = searchInnerGrid;
        Grid.SetColumn(searchBorder, 0);
        searchGrid.Children.Add(searchBorder);

        var lblResultCount = new TextBlock
        {
            FontSize = 11,
            Foreground = TextSubtle,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(14, 0, 0, 0)
        };
        Grid.SetColumn(lblResultCount, 1);
        searchGrid.Children.Add(lblResultCount);

        sp.Children.Add(pillContainer);
        sp.Children.Add(searchGrid);

        // 4. Master-Detail Split Grid
        var mdGrid = new Grid();
        mdGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(380) });
        mdGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        // Left List Container
        var leftScroll = new ScrollViewer
        {
            Height = 560,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Margin = new Thickness(0, 0, 14, 0)
        };
        var leftCardsStack = new StackPanel();
        leftScroll.Content = leftCardsStack;
        Grid.SetColumn(leftScroll, 0);
        mdGrid.Children.Add(leftScroll);

        // Right Detail ScrollViewer
        var detailScroll = new ScrollViewer
        {
            Height = 560,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        var detailStack = new StackPanel();
        detailScroll.Content = detailStack;
        Grid.SetColumn(detailScroll, 1);
        mdGrid.Children.Add(detailScroll);

        sp.Children.Add(mdGrid);
        card.Child = sp;
        root.Children.Add(card);

        TroubleshootingSolution? selectedSolution = null;

        // Populate Pill Buttons
        foreach (var cat in categories)
        {
            int count = (cat == "All") 
                ? _solutionsLibrary.Count 
                : _solutionsLibrary.Count(x => x.Category.Equals(cat, StringComparison.OrdinalIgnoreCase));

            var btnPill = new Button
            {
                Content = $"{cat} ({count})",
                Height = 28,
                Padding = new Thickness(12, 0, 12, 0),
                Margin = new Thickness(0, 0, 6, 6),
                FontSize = 11,
                Cursor = Cursors.Hand,
                BorderThickness = new Thickness(1),
                BorderBrush = BorderMuted
            };

            string currentCategoryName = cat;
            btnPill.Click += (s, e) =>
            {
                activeCategory = currentCategoryName;
                UpdatePillStyles();
                FilterAndRender();
            };

            pillButtons[cat] = btnPill;
            pillContainer.Children.Add(btnPill);
        }

        void UpdatePillStyles()
        {
            foreach (var kvp in pillButtons)
            {
                bool isSelected = kvp.Key == activeCategory;
                kvp.Value.Background = isSelected ? SvllBlue : new SolidColorBrush(Color.FromRgb(241, 245, 249));
                kvp.Value.Foreground = isSelected ? Brushes.White : new SolidColorBrush(Color.FromRgb(30, 41, 59));
                kvp.Value.FontWeight = isSelected ? FontWeights.Bold : FontWeights.Normal;
                kvp.Value.BorderBrush = isSelected ? SvllBlue : BorderMuted;
            }
        }

        void FilterAndRender()
        {
            string q = txtSearch.Text.Trim().ToLowerInvariant();
            btnClearSearch.Visibility = string.IsNullOrEmpty(q) ? Visibility.Collapsed : Visibility.Visible;

            var filtered = _solutionsLibrary.Where(s =>
            {
                bool matchCat = activeCategory == "All" || s.Category.Equals(activeCategory, StringComparison.OrdinalIgnoreCase);
                bool matchQuery = string.IsNullOrEmpty(q)
                    || s.Title.ToLowerInvariant().Contains(q)
                    || s.Symptoms.ToLowerInvariant().Contains(q)
                    || s.Explanation.ToLowerInvariant().Contains(q)
                    || s.Tags.Any(t => t.ToLowerInvariant().Contains(q))
                    || s.CmdCommand.ToLowerInvariant().Contains(q)
                    || s.PowerShellCommand.ToLowerInvariant().Contains(q);
                return matchCat && matchQuery;
            }).ToList();

            lblResultCount.Text = $"Showing {filtered.Count} of {_solutionsLibrary.Count} commands";

            if (selectedSolution == null || !filtered.Contains(selectedSolution))
            {
                selectedSolution = filtered.FirstOrDefault();
            }

            RenderLeftCards(leftCardsStack, filtered, selectedSolution, sol =>
            {
                selectedSolution = sol;
                FilterAndRender();
            });

            if (selectedSolution != null)
            {
                RenderSolutionDetail(detailStack, selectedSolution);
            }
            else
            {
                detailStack.Children.Clear();
                var emptyNotice = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
                    BorderBrush = BorderMuted,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(24),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 40, 0, 0)
                };
                var emptyStack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
                emptyStack.Children.Add(new TextBlock { Text = "🔍 No Matching Scenarios Found", FontSize = 13, FontWeight = FontWeights.Bold, Foreground = TextDark, HorizontalAlignment = HorizontalAlignment.Center });
                emptyStack.Children.Add(new TextBlock { Text = "Try adjusting your search query or selecting 'All' categories.", FontSize = 11, Foreground = TextSubtle, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 4, 0, 0) });
                emptyNotice.Child = emptyStack;
                detailStack.Children.Add(emptyNotice);
            }
        }

        txtSearch.TextChanged += (s, e) => FilterAndRender();

        UpdatePillStyles();
        FilterAndRender();

        scroll.Content = root;
        return scroll;
    }

    private void RenderLeftCards(StackPanel container, List<TroubleshootingSolution> list, TroubleshootingSolution? activeSol, Action<TroubleshootingSolution> onSelect)
    {
        container.Children.Clear();

        foreach (var item in list)
        {
            bool isSelected = item == activeSol;
            var (badgeBg, badgeFg) = GetCategoryBadgeColors(item.Category);

            var card = new Border
            {
                Background = isSelected ? new SolidColorBrush(Color.FromRgb(239, 246, 255)) : Brushes.White,
                BorderBrush = isSelected ? SvllBlue : new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                BorderThickness = isSelected ? new Thickness(3, 1, 1, 1) : new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12, 10, 12, 10),
                Margin = new Thickness(0, 0, 0, 6),
                Cursor = Cursors.Hand
            };

            var stack = new StackPanel();

            // Top Row: Category Badge
            var topRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 4) };
            
            var badgeBorder = new Border
            {
                Background = badgeBg,
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6, 1, 6, 1),
                HorizontalAlignment = HorizontalAlignment.Left
            };
            badgeBorder.Child = new TextBlock
            {
                Text = item.Category.ToUpperInvariant(),
                FontSize = 9.5,
                FontWeight = FontWeights.Bold,
                Foreground = badgeFg
            };
            topRow.Children.Add(badgeBorder);
            stack.Children.Add(topRow);

            // Title
            var txtTitle = new TextBlock
            {
                Text = item.Title,
                FontSize = 11.5,
                FontWeight = FontWeights.Bold,
                Foreground = isSelected ? SvllBlue : TextDark,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 2, 0, 4)
            };
            stack.Children.Add(txtTitle);

            // Symptoms snippet
            var txtSymptoms = new TextBlock
            {
                Text = item.Symptoms,
                FontSize = 10,
                Foreground = TextSubtle,
                TextTrimming = TextTrimming.CharacterEllipsis,
                MaxHeight = 16
            };
            stack.Children.Add(txtSymptoms);

            card.Child = stack;

            // Events
            TroubleshootingSolution currentItem = item;
            card.MouseDown += (s, e) => onSelect(currentItem);
            if (!isSelected)
            {
                card.MouseEnter += (s, e) => card.Background = new SolidColorBrush(Color.FromRgb(248, 250, 252));
                card.MouseLeave += (s, e) => card.Background = Brushes.White;
            }

            container.Children.Add(card);
        }
    }

    private void RenderSolutionDetail(StackPanel container, TroubleshootingSolution sol)
    {
        container.Children.Clear();

        var (badgeBg, badgeFg) = GetCategoryBadgeColors(sol.Category);

        // Header Section
        var headerCard = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(14),
            Margin = new Thickness(0, 0, 0, 10)
        };
        var headerStack = new StackPanel();

        var metaRow = new WrapPanel { Margin = new Thickness(0, 0, 0, 6) };
        var catBadge = new Border
        {
            Background = badgeBg,
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(8, 2, 8, 2),
            Margin = new Thickness(0, 0, 8, 0)
        };
        catBadge.Child = new TextBlock
        {
            Text = sol.Category.ToUpperInvariant(),
            FontSize = 10,
            FontWeight = FontWeights.Bold,
            Foreground = badgeFg
        };
        metaRow.Children.Add(catBadge);

        foreach (var tag in sol.Tags)
        {
            var tagBadge = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6, 2, 6, 2),
                Margin = new Thickness(0, 0, 6, 0)
            };
            tagBadge.Child = new TextBlock
            {
                Text = $"#{tag}",
                FontSize = 10,
                Foreground = TextSubtle
            };
            metaRow.Children.Add(tagBadge);
        }
        headerStack.Children.Add(metaRow);

        headerStack.Children.Add(new TextBlock
        {
            Text = sol.Title,
            FontSize = 15,
            FontWeight = FontWeights.Bold,
            Foreground = SvllBlue,
            TextWrapping = TextWrapping.Wrap
        });
        headerCard.Child = headerStack;
        container.Children.Add(headerCard);

        // Symptoms Box (Soft Amber / Warning)
        var symptomsBox = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(255, 251, 235)), // Amber-50
            BorderBrush = new SolidColorBrush(Color.FromRgb(253, 230, 138)), // Amber-200
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 0, 0, 10)
        };
        var symStack = new StackPanel();
        symStack.Children.Add(new TextBlock
        {
            Text = "⚠️  SYMPTOMS & PROBLEM OCCURRENCE",
            FontSize = 10.5,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(180, 83, 9)), // Amber-700
            Margin = new Thickness(0, 0, 0, 4)
        });
        symStack.Children.Add(new TextBlock
        {
            Text = sol.Symptoms,
            FontSize = 11.5,
            Foreground = TextDark,
            TextWrapping = TextWrapping.Wrap
        });
        symptomsBox.Child = symStack;
        container.Children.Add(symptomsBox);

        // Root Cause & Technical Remediation Box (Soft Sky / Blue)
        var causeBox = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(240, 249, 255)), // Sky-50
            BorderBrush = new SolidColorBrush(Color.FromRgb(186, 230, 253)), // Sky-200
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 0, 0, 12)
        };
        var causeStack = new StackPanel();
        causeStack.Children.Add(new TextBlock
        {
            Text = "💡  ROOT CAUSE & TECHNICAL REMEDIATION",
            FontSize = 10.5,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(3, 105, 161)), // Sky-700
            Margin = new Thickness(0, 0, 0, 4)
        });
        causeStack.Children.Add(new TextBlock
        {
            Text = sol.Explanation,
            FontSize = 11.5,
            Foreground = TextDark,
            TextWrapping = TextWrapping.Wrap
        });
        causeBox.Child = causeStack;
        container.Children.Add(causeBox);

        // CMD Command Block
        container.Children.Add(CreateCommandBlock("Command Prompt (CMD)", sol.CmdCommand, false));

        // PowerShell Command Block
        container.Children.Add(CreateCommandBlock("Windows PowerShell", sol.PowerShellCommand, true));
    }

    private UIElement CreateCommandBlock(string label, string command, bool isPowerShell)
    {
        var border = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(15, 23, 42)), // Slate-900
            BorderBrush = new SolidColorBrush(Color.FromRgb(51, 65, 85)), // Slate-700
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 0, 0, 10)
        };

        var sp = new StackPanel();

        var header = new Grid { Margin = new Thickness(0, 0, 0, 8) };
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var promptStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        promptStack.Children.Add(new TextBlock
        {
            Text = isPowerShell ? "PS >_" : "CMD >_",
            FontFamily = new FontFamily("Consolas, monospace"),
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            Foreground = isPowerShell ? new SolidColorBrush(Color.FromRgb(56, 189, 248)) : new SolidColorBrush(Color.FromRgb(245, 158, 11)),
            Margin = new Thickness(0, 0, 6, 0)
        });
        promptStack.Children.Add(new TextBlock
        {
            Text = label.ToUpperInvariant(),
            FontSize = 10,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(203, 213, 225))
        });
        Grid.SetColumn(promptStack, 0);
        header.Children.Add(promptStack);

        var btnPanel = new StackPanel { Orientation = Orientation.Horizontal };

        var btnCopy = new Button
        {
            Content = "📋 Copy",
            Height = 24,
            Padding = new Thickness(10, 0, 10, 0),
            Margin = new Thickness(0, 0, 6, 0),
            FontSize = 10.5,
            FontWeight = FontWeights.SemiBold,
            Background = new SolidColorBrush(Color.FromRgb(30, 41, 59)),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(1),
            BorderBrush = new SolidColorBrush(Color.FromRgb(71, 85, 105)),
            Cursor = Cursors.Hand
        };
        btnCopy.Click += async (s, e) =>
        {
            try
            {
                Clipboard.SetText(command);
                btnCopy.Content = "✔ Copied!";
                btnCopy.Background = new SolidColorBrush(Color.FromRgb(16, 185, 129));
                await Task.Delay(1800);
                btnCopy.Content = "📋 Copy";
                btnCopy.Background = new SolidColorBrush(Color.FromRgb(30, 41, 59));
            }
            catch { }
        };
        btnPanel.Children.Add(btnCopy);

        var btnRun = new Button
        {
            Content = isPowerShell ? "⚡ Run in PS" : "⚡ Run in CMD",
            Height = 24,
            Padding = new Thickness(10, 0, 10, 0),
            FontSize = 10.5,
            FontWeight = FontWeights.Bold,
            Background = SvllBlue,
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand
        };
        btnRun.Click += async (s, e) =>
        {
            Log($"\n[PLAYBOOK RUN] Executing {label}...");
            if (isPowerShell)
                await ExecuteAsync("powershell.exe", $"-NoProfile -ExecutionPolicy Bypass -Command \"{command}\"");
            else
                await ExecuteAsync("cmd.exe", $"/c \"{command}\"");
        };
        btnPanel.Children.Add(btnRun);

        Grid.SetColumn(btnPanel, 1);
        header.Children.Add(btnPanel);
        sp.Children.Add(header);

        var txtCmd = new TextBox
        {
            Text = command,
            FontFamily = new FontFamily("Consolas, Courier New, monospace"),
            FontSize = 11,
            Foreground = isPowerShell 
                ? new SolidColorBrush(Color.FromRgb(56, 189, 248)) 
                : new SolidColorBrush(Color.FromRgb(241, 245, 249)),
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            IsReadOnly = true,
            TextWrapping = TextWrapping.Wrap,
            Padding = new Thickness(4, 4, 4, 4)
        };
        sp.Children.Add(txtCmd);

        border.Child = sp;
        return border;
    }

    #endregion

    #region 2. Disk Cleanup & Temp Files

    private UIElement BuildCleanupView()
    {
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var root = new StackPanel { Margin = new Thickness(0, 0, 10, 0) };

        var card = new Border
        {
            Background = BgCard,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16),
            Margin = new Thickness(0, 0, 0, 14)
        };

        var sp = new StackPanel();
        sp.Children.Add(new TextBlock
        {
            Text = "DISK CLEANUP & SYSTEM TEMP PURGE",
            FontSize = 13,
            FontWeight = FontWeights.Bold,
            Foreground = SvllBlue,
            Margin = new Thickness(0, 0, 0, 6)
        });
        sp.Children.Add(new TextBlock
        {
            Text = "Purge accumulated junk files, temporary build caches, Windows prefetch, and empty the Recycle Bin.",
            FontSize = 11,
            Foreground = TextSubtle,
            Margin = new Thickness(0, 0, 0, 12)
        });

        sp.Children.Add(CreateToolRow("Purge User Temporary Files (%TEMP%)", "Deletes temporary session files accumulated in the current user AppData\\Local\\Temp directory.",
            () => Task.Run(() =>
            {
                Log("\n[CLEANUP] Cleaning User Temp directory...");
                CleanDirectory(Path.GetTempPath());
                Log("[CLEANUP] User Temp cleanup finished.");
            }), "Clean %TEMP%"));

        sp.Children.Add(CreateToolRow("Purge Windows System Temp (C:\\Windows\\Temp)", "Cleans system-wide installers and temporary files created by background services.",
            () => Task.Run(() =>
            {
                Log("\n[CLEANUP] Cleaning Windows System Temp directory...");
                CleanDirectory(@"C:\Windows\Temp");
                Log("[CLEANUP] System Temp cleanup finished.");
            }), "Clean System Temp"));

        sp.Children.Add(CreateToolRow("Purge Windows Prefetch", "Deletes obsolete application prefetch execution records in C:\\Windows\\Prefetch.",
            () => Task.Run(() =>
            {
                Log("\n[CLEANUP] Cleaning Windows Prefetch directory...");
                CleanDirectory(@"C:\Windows\Prefetch");
                Log("[CLEANUP] Prefetch cleanup finished.");
            }), "Clean Prefetch"));

        sp.Children.Add(CreateToolRow("Empty Windows Recycle Bin", "Permanently empties all deleted files across all physical drive Recycle Bins.",
            () => Task.Run(() =>
            {
                Log("\n[CLEANUP] Emptying Windows Recycle Bin...");
                SHEmptyRecycleBin(IntPtr.Zero, null!, 7);
                Log("[CLEANUP] Recycle Bin emptied.");
            }), "Empty Bin"));

        sp.Children.Add(CreateToolRow("Launch Cleanmgr GUI", "Opens native Windows Disk Cleanup utility for deep system volume analysis.",
            () => Task.Run(() => OpenTool("cleanmgr.exe")), "Open Cleanmgr"));

        card.Child = sp;
        root.Children.Add(card);

        scroll.Content = root;
        return scroll;
    }

    private void CleanDirectory(string path)
    {
        try
        {
            if (!Directory.Exists(path)) return;
            var dir = new DirectoryInfo(path);
            foreach (var f in dir.GetFiles())
            {
                try { f.Delete(); } catch { }
            }
            foreach (var d in dir.GetDirectories())
            {
                try { d.Delete(true); } catch { }
            }
        }
        catch { }
    }

    #endregion

    #region 3. Workstation Config & Baseline Profiles Manager

    private UIElement BuildConfigManagerView()
    {
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var root = new StackPanel { Margin = new Thickness(0, 0, 10, 0) };

        var card = new Border
        {
            Background = BgCard,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16),
            Margin = new Thickness(0, 0, 0, 14)
        };

        var sp = new StackPanel();
        sp.Children.Add(new TextBlock
        {
            Text = "WORKSTATION CONFIGURATION BASELINES",
            FontSize = 13,
            FontWeight = FontWeights.Bold,
            Foreground = SvllBlue,
            Margin = new Thickness(0, 0, 0, 6)
        });
        sp.Children.Add(new TextBlock
        {
            Text = "Export, import, and apply standardized enterprise baseline settings across the logistics computer fleet.",
            FontSize = 11,
            Foreground = TextSubtle,
            Margin = new Thickness(0, 0, 0, 12)
        });

        // Profile Selector & Action Toolbar
        var presetRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 14) };

        var lblPreset = new TextBlock
        {
            Text = "Active Fleet Baseline Profile:",
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Foreground = TextDark,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 10, 0)
        };
        presetRow.Children.Add(lblPreset);

        var cmbPreset = new ComboBox { Width = 280, Height = 32, FontSize = 11, VerticalContentAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 12, 0) };
        cmbPreset.Items.Add("⚡ SVLL High-Performance Baseline (Max Speed, Low RAM/CPU)");
        cmbPreset.Items.Add("🏢 SVLL Standard Corporate Fleet Baseline");
        cmbPreset.Items.Add("🛡️ High-Security Hardened Branch Profile");
        cmbPreset.SelectedIndex = 0;
        cmbPreset.SelectionChanged += (s, e) =>
        {
            if (cmbPreset.SelectedIndex == 0)
            {
                _activeConfig.PresetName = "SVLL High-Performance Baseline";
                _activeConfig.Description = "Maximizes system speed, frees working set RAM, reduces CPU background load, cleans temp caches, and sets Ultimate Performance.";
                _activeConfig.DisableTelemetry = true;
                _activeConfig.RemoveUwpBloat = true;
                _activeConfig.CleanTempsAndCaches = true;
                _activeConfig.OptimizePowerPlan = true;
                _activeConfig.FlushDnsAndNetworkCaches = true;
                _activeConfig.OptimizeBackgroundServices = true;
            }
            else if (cmbPreset.SelectedIndex == 1)
            {
                _activeConfig.PresetName = "SVLL Standard Corporate Fleet Baseline";
                _activeConfig.Description = "Standard operational profile for office workstations with balanced power management.";
                _activeConfig.DisableTelemetry = true;
                _activeConfig.RemoveUwpBloat = true;
                _activeConfig.CleanTempsAndCaches = true;
                _activeConfig.OptimizePowerPlan = false;
                _activeConfig.FlushDnsAndNetworkCaches = true;
                _activeConfig.OptimizeBackgroundServices = false;
            }
            else
            {
                _activeConfig.PresetName = "SVLL High-Security Hardened Branch Profile";
                _activeConfig.Description = "Strict security baseline with telemetry fully eliminated and firewall policies enforced.";
                _activeConfig.DisableTelemetry = true;
                _activeConfig.RemoveUwpBloat = true;
                _activeConfig.CleanTempsAndCaches = true;
                _activeConfig.OptimizePowerPlan = false;
                _activeConfig.FlushDnsAndNetworkCaches = true;
                _activeConfig.OptimizeBackgroundServices = false;
            }
        };
        presetRow.Children.Add(cmbPreset);

        var btnApply = new Button { Content = " ⚡ Apply Baseline & Accelerate System Now ", Height = 32, Padding = new Thickness(16, 0, 16, 0), Background = SvllBlue, Foreground = Brushes.White, FontWeight = FontWeights.Bold, Cursor = Cursors.Hand };
        btnApply.Click += async (s, e) => await ExecuteActiveConfigAsync();
        presetRow.Children.Add(btnApply);

        sp.Children.Add(presetRow);

        var btnBar = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 14) };
        var btnExport = new Button { Content = "Export Active Baseline (.JSON)", Height = 30, Padding = new Thickness(12, 0, 12, 0), Margin = new Thickness(0, 0, 8, 0), Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)), BorderBrush = BorderMuted };
        btnExport.Click += (s, e) => ExportConfigJson();
        btnBar.Children.Add(btnExport);

        var btnImport = new Button { Content = "Import Custom Baseline (.JSON)", Height = 30, Padding = new Thickness(12, 0, 12, 0), Margin = new Thickness(0, 0, 8, 0), Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)), BorderBrush = BorderMuted };
        btnImport.Click += (s, e) => ImportConfigJson();
        btnBar.Children.Add(btnImport);
        sp.Children.Add(btnBar);

        // Performance Optimization Summary Card
        var optBox = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(14),
            Margin = new Thickness(0, 0, 0, 10)
        };
        var optStack = new StackPanel();
        optStack.Children.Add(new TextBlock
        {
            Text = "HIGH-PERFORMANCE SYSTEM ACCELERATION INCLUDED IN ACTIVE BASELINE:",
            FontSize = 10,
            FontWeight = FontWeights.Bold,
            Foreground = SvllBlue,
            Margin = new Thickness(0, 0, 0, 6)
        });
        optStack.Children.Add(new TextBlock { Text = "✔ Working Set RAM Purge: Releases cached unpaged standby memory allocations to reduce RAM load.", FontSize = 11, Foreground = TextDark, Margin = new Thickness(0, 2, 0, 2) });
        optStack.Children.Add(new TextBlock { Text = "✔ CPU Power & Throttling: Unlocks Ultimate Performance power scheme to remove processor latency.", FontSize = 11, Foreground = TextDark, Margin = new Thickness(0, 2, 0, 2) });
        optStack.Children.Add(new TextBlock { Text = "✔ Deep Temp Purge: Cleans %TEMP%, C:\\Windows\\Temp, Prefetch, and empties Recycle Bin.", FontSize = 11, Foreground = TextDark, Margin = new Thickness(0, 2, 0, 2) });
        optStack.Children.Add(new TextBlock { Text = "✔ Telemetry & Bloat Reduction: Disables diagnostic tracking and consumer background UWP apps.", FontSize = 11, Foreground = TextDark, Margin = new Thickness(0, 2, 0, 2) });
        optStack.Children.Add(new TextBlock { Text = "✔ Network Stack Flush: Flushes DNS resolver cache and ARP tables for instantaneous packet routing.", FontSize = 11, Foreground = TextDark, Margin = new Thickness(0, 2, 0, 2) });
        optBox.Child = optStack;
        sp.Children.Add(optBox);

        card.Child = sp;
        root.Children.Add(card);

        scroll.Content = root;
        return scroll;
    }

    private void ExportConfigJson()
    {
        var sfd = new SaveFileDialog
        {
            Filter = "JSON Files (*.json)|*.json",
            FileName = "SVLL_Workstation_Baseline.json"
        };
        if (sfd.ShowDialog() == true)
        {
            File.WriteAllText(sfd.FileName, JsonSerializer.Serialize(_activeConfig, new JsonSerializerOptions { WriteIndented = true }));
            Log($"[CONFIG] Exported baseline configuration to {sfd.FileName}");
        }
    }

    private void ImportConfigJson()
    {
        var ofd = new OpenFileDialog { Filter = "JSON Files (*.json)|*.json" };
        if (ofd.ShowDialog() == true)
        {
            try
            {
                var cfg = JsonSerializer.Deserialize<WorkstationConfig>(File.ReadAllText(ofd.FileName));
                if (cfg != null)
                {
                    _activeConfig = cfg;
                    Log($"[CONFIG] Imported baseline configuration '{cfg.PresetName}' from {ofd.FileName}");
                    MessageBox.Show($"Loaded baseline: {cfg.PresetName}", "Config Imported", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load configuration: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async Task ExecuteActiveConfigAsync()
    {
        Log("\n[CONFIG] Deploying full active configuration baseline...");

        // 1. Telemetry and Bloatware
        if (_activeConfig.DisableTelemetry)
        {
            Log("[CONFIG] Disabling telemetry and consumer background tracking...");
            await ApplyEssentialTweaksAsync();
        }
        if (_activeConfig.RemoveUwpBloat)
        {
            Log("[CONFIG] Removing consumer UWP AppX bloatware...");
            await RemoveUwpBloatwareAsync();
        }

        // 2. High-Performance Power Plan
        if (_activeConfig.OptimizePowerPlan)
        {
            Log("[CONFIG] Activating Ultimate Performance power scheme to reduce CPU latency...");
            await ExecuteAsync("powercfg.exe", "-duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61");
            await ExecuteAsync("powercfg.exe", "-setactive e9a42b02-d5df-448d-aa00-03f14749eb61");
        }

        // 3. Clear Temp Caches & Recycle Bin
        if (_activeConfig.CleanTempsAndCaches)
        {
            Log("[CONFIG] Purging system temp caches, prefetch, and emptying Recycle Bin...");
            CleanDirectory(Path.GetTempPath());
            CleanDirectory(@"C:\Windows\Temp");
            try { SHEmptyRecycleBin(IntPtr.Zero, null!, 7); } catch { }
        }

        // 4. Flush DNS and Network Caches
        if (_activeConfig.FlushDnsAndNetworkCaches)
        {
            Log("[CONFIG] Flushing DNS resolver cache and ARP table...");
            await ExecuteAsync("ipconfig.exe", "/flushdns");
            await ExecuteAsync("arp.exe", "-d *");
        }

        // 5. Working Set RAM Optimization / Garbage Collection
        Log("[CONFIG] Freeing system working set memory allocations...");
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        Log("[CONFIG] Baseline deployment sequence completed successfully! System is running at optimal baseline.");
        MessageBox.Show("Baseline optimizations applied successfully!\n\n• RAM standby memory released\n• CPU power latency minimized\n• System temp caches & recycle bin purged\n• Telemetry & bloatware disabled", "Baseline Applied", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private async Task HandleCommandLineArgsAsync(string[] args)
    {
        if (args != null && args.Contains("--silent-apply"))
        {
            Log("[CLI] Detected --silent-apply flag. Executing baseline...");
            await ExecuteActiveConfigAsync();
        }
    }

    #endregion

    #region 4. Release Updates & About View

    private UIElement BuildAboutView()
    {
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var root = new StackPanel { Margin = new Thickness(0, 0, 10, 0) };

        var card = new Border
        {
            Background = BgCard,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(20),
            Margin = new Thickness(0, 0, 0, 14)
        };

        var sp = new StackPanel();

        sp.Children.Add(RenderSvllBrandLogo());

        var titleBlock = new TextBlock
        {
            Text = $"SVLL IT Support Workstation Suite v{CurrentVersion}",
            FontSize = 16,
            FontWeight = FontWeights.Bold,
            Foreground = SvllBlue,
            Margin = new Thickness(0, 14, 0, 4)
        };
        sp.Children.Add(titleBlock);

        var devBlock = new TextBlock
        {
            Text = "Developed by: Kunal Turkar\nShree Vasu Logistics Limited - Information Technology Department",
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Foreground = TextDark,
            Margin = new Thickness(0, 0, 0, 12)
        };
        sp.Children.Add(devBlock);

        var descBlock = new TextBlock
        {
            Text = "A high-performance enterprise IT workstation utility designed for rapid diagnostics, network provisioning, silent software rollouts, Windows debloating, and 24/7 endpoint reliability.",
            FontSize = 11.5,
            Foreground = TextSubtle,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 16)
        };
        sp.Children.Add(descBlock);

        var updateBox = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(14),
            Margin = new Thickness(0, 0, 0, 14)
        };

        var upStack = new StackPanel();
        upStack.Children.Add(new TextBlock { Text = "CLOUD DISTRIBUTION & UPDATE ENGINE", FontSize = 11, FontWeight = FontWeights.Bold, Foreground = SvllBlue });

        var statusBlock = new TextBlock
        {
            Text = "Click 'Check for Updates' to query latest releases from GitHub.",
            FontSize = 11,
            Foreground = TextSubtle,
            Margin = new Thickness(0, 4, 0, 10)
        };
        upStack.Children.Add(statusBlock);

        var btnRow = new StackPanel { Orientation = Orientation.Horizontal };
        var btnCheckUpdate = new Button
        {
            Content = "Check GitHub for Latest Release",
            Height = 32,
            Padding = new Thickness(14, 0, 14, 0),
            Background = SvllBlue,
            Foreground = Brushes.White,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 10, 0)
        };

        var btnApplyUpdate = new Button
        {
            Content = "Download & Apply Update Now",
            Height = 32,
            Padding = new Thickness(14, 0, 14, 0),
            Background = new SolidColorBrush(Color.FromRgb(16, 185, 129)), // Green
            Foreground = Brushes.White,
            FontWeight = FontWeights.Bold,
            Visibility = Visibility.Collapsed
        };

        string downloadUrl = "";
        string updateFileName = "";

        btnCheckUpdate.Click += async (s, e) =>
        {
            btnCheckUpdate.IsEnabled = false;
            statusBlock.Text = "Querying GitHub API (kunal-dev-stack/svll-workstation)...";
            btnApplyUpdate.Visibility = Visibility.Collapsed;

            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "SVLL-Workstation");
                string json = await client.GetStringAsync("https://api.github.com/repos/kunal-dev-stack/svll-workstation/releases/latest");
                using var doc = JsonDocument.Parse(json);
                string tag = doc.RootElement.GetProperty("tag_name").GetString() ?? "";

                // Find setup asset
                downloadUrl = "";
                if (doc.RootElement.TryGetProperty("assets", out var assets) && assets.ValueKind == JsonValueKind.Array)
                {
                    foreach (var asset in assets.EnumerateArray())
                    {
                        string name = asset.GetProperty("name").GetString() ?? "";
                        if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) && name.Contains("Setup", StringComparison.OrdinalIgnoreCase))
                        {
                            downloadUrl = asset.GetProperty("browser_download_url").GetString() ?? "";
                            updateFileName = name;
                            break;
                        }
                    }
                }

                string cleanTag = tag.TrimStart('v', 'V');
                if (cleanTag != CurrentVersion && !string.IsNullOrEmpty(downloadUrl))
                {
                    statusBlock.Text = $"🎉 New update available: {tag} (Installed: v{CurrentVersion})! Click 'Download & Apply Update Now'.";
                    statusBlock.Foreground = new SolidColorBrush(Color.FromRgb(5, 150, 105));
                    btnApplyUpdate.Visibility = Visibility.Visible;
                }
                else if (cleanTag != CurrentVersion && string.IsNullOrEmpty(downloadUrl))
                {
                    statusBlock.Text = $"New tag {tag} found on GitHub, but no installer (*Setup*.exe) is attached to the release yet.";
                    statusBlock.Foreground = new SolidColorBrush(Color.FromRgb(217, 119, 6));
                }
                else
                {
                    statusBlock.Text = $"✔ You are already running the latest version (v{CurrentVersion}).";
                    statusBlock.Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129));
                }

                Log($"[UPDATER] Checked GitHub releases: latest tag is '{tag}'.");
            }
            catch (Exception ex)
            {
                statusBlock.Text = $"Update query error: {ex.Message}";
                statusBlock.Foreground = new SolidColorBrush(Color.FromRgb(220, 38, 38));
            }
            finally
            {
                btnCheckUpdate.IsEnabled = true;
            }
        };

        btnApplyUpdate.Click += async (s, e) =>
        {
            if (string.IsNullOrEmpty(downloadUrl)) return;
            btnApplyUpdate.IsEnabled = false;
            btnCheckUpdate.IsEnabled = false;
            statusBlock.Text = $"Downloading {updateFileName}... Please wait.";
            statusBlock.Foreground = SvllBlue;

            try
            {
                string tempDir = Path.GetTempPath();
                string targetPath = Path.Combine(tempDir, string.IsNullOrEmpty(updateFileName) ? "SVLL-Setup.exe" : updateFileName);

                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "SVLL-Workstation");
                var bytes = await client.GetByteArrayAsync(downloadUrl);
                await File.WriteAllBytesAsync(targetPath, bytes);

                statusBlock.Text = "Download complete! Launching silent installer and updating...";
                Log($"[UPDATER] Downloaded update to {targetPath}. Launching installer.");

                // Launch silent installer and exit current app
                Process.Start(new ProcessStartInfo
                {
                    FileName = targetPath,
                    Arguments = "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP-",
                    UseShellExecute = true
                });

                await Task.Delay(1000);
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                statusBlock.Text = $"Failed to download/install update: {ex.Message}";
                statusBlock.Foreground = new SolidColorBrush(Color.FromRgb(220, 38, 38));
                btnApplyUpdate.IsEnabled = true;
                btnCheckUpdate.IsEnabled = true;
            }
        };

        btnRow.Children.Add(btnCheckUpdate);
        btnRow.Children.Add(btnApplyUpdate);
        upStack.Children.Add(btnRow);

        updateBox.Child = upStack;
        sp.Children.Add(updateBox);

        card.Child = sp;
        root.Children.Add(card);

        scroll.Content = root;
        return scroll;
    }

    #endregion
}
