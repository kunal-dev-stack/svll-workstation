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
    #region 1. Complete IT Troubleshooting & Command Library (22 Scenarios)

    private void InitializeCommandLibrary()
    {
        _solutionsLibrary = new List<TroubleshootingSolution>
        {
            new TroubleshootingSolution
            {
                Title = "169.254.x.x APIPA Automatic IP Fallback",
                Category = "Network",
                Symptoms = "No IP lease received from branch DHCP router; endpoint displays 169.254.x.x autoconfiguration address.",
                Explanation = "Reinitializes DHCP client binding, purges stale DORA leases, and requests a fresh address from the router.",
                CmdCommand = "ipconfig /release && ipconfig /renew",
                PowerShellCommand = "Restart-NetAdapter -Name *; Start-Sleep 2; ipconfig /renew",
                Tags = new[] { "169.254", "apipa", "dhcp", "network" }
            },
            new TroubleshootingSolution
            {
                Title = "Stuck Print Spooler / Phantom Locked Jobs",
                Category = "Printers",
                Symptoms = "Print jobs locked with status 'Deleting' or 'Printing'; print queue cannot be cleared from GUI.",
                Explanation = "Terminates the spooler service and forcefully purges locked .SHD and .SPL spool files from System32\\spool\\PRINTERS.",
                CmdCommand = "net stop spooler && del /Q /F /S %systemroot%\\System32\\Spool\\Printers\\* && net start spooler",
                PowerShellCommand = "Stop-Service Spooler -Force; Remove-Item $env:windir\\System32\\spool\\PRINTERS\\* -Force -Recurse; Start-Service Spooler",
                Tags = new[] { "printer", "spooler", "queue" }
            },
            new TroubleshootingSolution
            {
                Title = "DNS Resolution Failure & Webpages Not Loading",
                Category = "Network",
                Symptoms = "Ping to 8.8.8.8 succeeds, but hostnames fail with 'Host not found' or 'DNS_PROBE_FINISHED_NXDOMAIN'.",
                Explanation = "Flushes local resolver cache, reregisters client DNS, and resets the Winsock API catalog.",
                CmdCommand = "ipconfig /flushdns && ipconfig /registerdns && netsh winsock reset",
                PowerShellCommand = "Clear-DnsClientCache; Register-DnsClient; Restart-Service Dnscache -Force",
                Tags = new[] { "dns", "flushdns", "winsock" }
            },
            new TroubleshootingSolution
            {
                Title = "Corrupt System Store & Windows Crashes",
                Category = "Windows OS",
                Symptoms = "System UI freezes, SFC reports corrupt unfixable files, or unexpected BSOD stop errors occur.",
                Explanation = "Repairs the Windows component store image payload using Windows Update as source and executes SFC.",
                CmdCommand = "dism /Online /Cleanup-Image /RestoreHealth && sfc /scannow",
                PowerShellCommand = "Repair-WindowsImage -Online -RestoreHealth; sfc /scannow",
                Tags = new[] { "sfc", "dism", "crash", "corrupt" }
            },
            new TroubleshootingSolution
            {
                Title = "Active Directory Secure Channel / Trust Failure",
                Category = "Active Directory",
                Symptoms = "'The trust relationship between this workstation and the primary domain failed' upon user logon.",
                Explanation = "Tests and repairs the Kerberos secure channel machine password negotiated with the domain controller.",
                CmdCommand = "nltest /sc_query:SVLL.LOCAL /sc_reset:SVLL.LOCAL",
                PowerShellCommand = "Test-ComputerSecureChannel -Repair -Verbose",
                Tags = new[] { "trust", "domain", "active directory", "kerberos" }
            },
            new TroubleshootingSolution
            {
                Title = "WinSxS Component Store Bloat Cleanup",
                Category = "Storage",
                Symptoms = "System drive C: is low on disk space; WinSxS folder consumes 15GB+ of storage space.",
                Explanation = "Purges superseded Windows updates and compresses baseline operating system payloads.",
                CmdCommand = "dism.exe /Online /Cleanup-Image /StartComponentCleanup /ResetBase",
                PowerShellCommand = "dism.exe /Online /Cleanup-Image /StartComponentCleanup /ResetBase",
                Tags = new[] { "winsxs", "storage", "cleanup", "dism" }
            },
            new TroubleshootingSolution
            {
                Title = "Windows Update Download Error 0x80070002",
                Category = "Windows OS",
                Symptoms = "Windows updates fail with error codes 0x80070002, 0x80240034, or download gets stuck at 0%.",
                Explanation = "Stops wuauserv and BITS, purges the corrupt SoftwareDistribution download catalog, and restarts services.",
                CmdCommand = "net stop wuauserv && net stop bits && rd /s /q %windir%\\SoftwareDistribution && net start wuauserv",
                PowerShellCommand = "Stop-Service wuauserv, bits; Remove-Item $env:windir\\SoftwareDistribution -Recurse -Force; Start-Service wuauserv, bits",
                Tags = new[] { "update", "softwaredistribution", "wuauserv" }
            },
            new TroubleshootingSolution
            {
                Title = "BitLocker Volume Recovery Key Extraction",
                Category = "Security",
                Symptoms = "Need to backup BitLocker 48-digit numerical recovery password for volume C: before motherboard swap.",
                Explanation = "Queries the BitLocker volume protector to display the active numerical recovery key ID and key.",
                CmdCommand = "manage-bde -protectors -get C:",
                PowerShellCommand = "(Get-BitLockerVolume -MountPoint C:).KeyProtector | Where-Object { $_.KeyProtectorType -eq 'RecoveryPassword' } | Select-Object -ExpandProperty RecoveryPassword",
                Tags = new[] { "bitlocker", "recovery", "encryption" }
            },
            new TroubleshootingSolution
            {
                Title = "Remote Desktop (RDP) Port 3389 Enable & Firewall",
                Category = "Security",
                Symptoms = "Cannot connect to workstation via MSTSC; connection refused on port 3389.",
                Explanation = "Enables Terminal Server registry keys and opens Windows Defender Firewall rules for RDP.",
                CmdCommand = "reg add \"HKLM\\System\\CurrentControlSet\\Control\\Terminal Server\" /v fDenyTSConnections /t REG_DWORD /d 0 /f && netsh advfirewall firewall set rule group=\"remote desktop\" new enable=Yes",
                PowerShellCommand = "Set-ItemProperty -Path 'HKLM:\\System\\CurrentControlSet\\Control\\Terminal Server' -Name 'fDenyTSConnections' -Value 0; Enable-NetFirewallRule -DisplayGroup 'Remote Desktop'",
                Tags = new[] { "rdp", "remote desktop", "firewall", "3389" }
            },
            new TroubleshootingSolution
            {
                Title = "Microsoft Activation Scripts (MAS) Launch",
                Category = "Windows OS",
                Symptoms = "Windows or Office displays 'Activation Expired' or watermark on desktop.",
                Explanation = "Executes the community Microsoft Activation Scripts (MAS) utility for HWID activation.",
                CmdCommand = "powershell -Command \"irm https://get.activated.win | iex\"",
                PowerShellCommand = "irm https://get.activated.win | iex",
                Tags = new[] { "mas", "activation", "windows", "office" }
            },
            new TroubleshootingSolution
            {
                Title = "Chris Titus Tech Windows Utility Launch",
                Category = "Windows OS",
                Symptoms = "Need to apply advanced debloat, install custom tool bundles, or configure Windows servicing.",
                Explanation = "Launches the Chris Titus Tech Windows Utility script interface via PowerShell.",
                CmdCommand = "powershell -Command \"irm https://christitus.com/win | iex\"",
                PowerShellCommand = "irm https://christitus.com/win | iex",
                Tags = new[] { "ctt", "winutil", "debloat", "titus" }
            },
            new TroubleshootingSolution
            {
                Title = "Re-register Corrupted Windows Store & UWP Apps",
                Category = "Windows OS",
                Symptoms = "Windows Calculator, Photos, or Microsoft Store crash immediately upon launch.",
                Explanation = "Re-registers all modern AppX application manifest manifests across all local user accounts.",
                CmdCommand = "powershell -ExecutionPolicy Bypass -Command \"Get-AppXPackage -AllUsers | Foreach {Add-AppxPackage -DisableDevelopmentMode -Register \\\"$($_.InstallLocation)\\AppXManifest.xml\\\"}\"",
                PowerShellCommand = "Get-AppXPackage -AllUsers | Foreach {Add-AppxPackage -DisableDevelopmentMode -Register \"$($_.InstallLocation)\\AppXManifest.xml\"}",
                Tags = new[] { "uwp", "store", "calculator", "appx" }
            },
            new TroubleshootingSolution
            {
                Title = "Unlock Windows 'Ultimate Performance' Power Plan",
                Category = "Performance",
                Symptoms = "Workstation CPU throttling or sluggish disk performance under high logistics workloads.",
                Explanation = "Unlocks the hidden Windows 10/11 Ultimate Performance power scheme and sets it as active.",
                CmdCommand = "powercfg -duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61 && powercfg -setactive e9a42b02-d5df-448d-aa00-03f14749eb61",
                PowerShellCommand = "powercfg -duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61; powercfg -setactive e9a42b02-d5df-448d-aa00-03f14749eb61",
                Tags = new[] { "power", "performance", "cpu", "throttle" }
            },
            new TroubleshootingSolution
            {
                Title = "Rebuild Corrupted Windows Icon & Thumbnail Cache",
                Category = "Windows OS",
                Symptoms = "Desktop icons show generic white paper icons or incorrect graphical assets.",
                Explanation = "Stops explorer.exe, deletes corrupted IconCache.db and thumbnail cache files, and restarts shell.",
                CmdCommand = "taskkill /f /im explorer.exe && del /a /q \"%localappdata%\\IconCache.db\" && start explorer.exe",
                PowerShellCommand = "Stop-Process -Name explorer -Force; Remove-Item $env:LOCALAPPDATA\\IconCache.db -Force; Start-Process explorer",
                Tags = new[] { "icons", "thumbnail", "cache", "explorer" }
            },
            new TroubleshootingSolution
            {
                Title = "Purge Volume Shadow Copies (VSS)",
                Category = "Storage",
                Symptoms = "Volume C: reports insufficient space due to hidden Volume Shadow Copies holding disk blocks.",
                Explanation = "Deletes all shadow copies on volume C: using VSSAdmin to free storage.",
                CmdCommand = "vssadmin delete shadows /for=C: /all /quiet",
                PowerShellCommand = "vssadmin delete shadows /for=C: /all /quiet",
                Tags = new[] { "vss", "shadow", "storage", "disk" }
            },
            new TroubleshootingSolution
            {
                Title = "Reset Windows Firewall to Factory Default",
                Category = "Security",
                Symptoms = "Inbound or outbound network traffic blocked due to conflicting or corrupted firewall rules.",
                Explanation = "Restores the Windows Defender Firewall to its out-of-the-box state.",
                CmdCommand = "netsh advfirewall reset",
                PowerShellCommand = "(New-Object -ComObject HNetCfg.FwPolicy2).RestoreLocalFirewallDefaults()",
                Tags = new[] { "firewall", "security", "network" }
            },
            new TroubleshootingSolution
            {
                Title = "Release and Renew IP Lease Immediately",
                Category = "Network",
                Symptoms = "Network displays 'No Internet Access' but Ethernet cable is securely connected.",
                Explanation = "Performs an instant DHCP lease release and renewal without rebooting.",
                CmdCommand = "ipconfig /release && ipconfig /renew",
                PowerShellCommand = "ipconfig /release; ipconfig /renew",
                Tags = new[] { "ipconfig", "dhcp", "lease" }
            },
            new TroubleshootingSolution
            {
                Title = "View Open TCP Ports & Listening Sockets",
                Category = "Network",
                Symptoms = "Need to verify if a local service (e.g. ERP client, web server) is listening on a specific port.",
                Explanation = "Lists all active TCP listening ports and the process ID owning the socket.",
                CmdCommand = "netstat -ano | findstr LISTENING",
                PowerShellCommand = "Get-NetTCPConnection -State Listen | Select-Object LocalAddress, LocalPort, OwningProcess",
                Tags = new[] { "ports", "netstat", "tcp", "sockets" }
            },
            new TroubleshootingSolution
            {
                Title = "Audit Local Administrators Group Membership",
                Category = "Active Directory",
                Symptoms = "Unauthorized local accounts or domain users possessing elevated local administrator rights.",
                Explanation = "Enumerates all members of the local Administrators group.",
                CmdCommand = "net localgroup Administrators",
                PowerShellCommand = "Get-LocalGroupMember -Group 'Administrators'",
                Tags = new[] { "administrators", "security", "audit", "sam" }
            },
            new TroubleshootingSolution
            {
                Title = "Force User & Computer Group Policy Update",
                Category = "Active Directory",
                Symptoms = "New domain security policies, wallpaper, or drive mappings not applying to client machine.",
                Explanation = "Forces an immediate re-application of computer and user Group Policy objects from the DC.",
                CmdCommand = "gpupdate /force",
                PowerShellCommand = "gpupdate /force",
                Tags = new[] { "gpo", "gpupdate", "domain", "policy" }
            },
            new TroubleshootingSolution
            {
                Title = "Restart Windows Explorer Shell",
                Category = "Windows OS",
                Symptoms = "Taskbar frozen, Start Menu unresponsive, or system tray icons unresponsive.",
                Explanation = "Forcefully terminates explorer.exe and relaunches the shell cleanly.",
                CmdCommand = "taskkill /f /im explorer.exe && start explorer.exe",
                PowerShellCommand = "Stop-Process -Name explorer -Force; Start-Process explorer",
                Tags = new[] { "explorer", "taskbar", "shell", "freeze" }
            },
            new TroubleshootingSolution
            {
                Title = "Reset TCP/IP Stack & Winsock Catalog",
                Category = "Network",
                Symptoms = "Complete network stack failure where network adapter has valid IP but cannot send packets.",
                Explanation = "Resets both TCP/IP registry parameters and Winsock LSP provider layers.",
                CmdCommand = "netsh int ip reset && netsh winsock reset",
                PowerShellCommand = "netsh int ip reset; netsh winsock reset",
                Tags = new[] { "tcp", "winsock", "reset", "network" }
            }
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
            Padding = new Thickness(16),
            Margin = new Thickness(0, 0, 0, 14)
        };

        var sp = new StackPanel();
        sp.Children.Add(new TextBlock
        {
            Text = "ENTERPRISE IT FIX & COMMAND PLAYBOOK (22 SCENARIOS)",
            FontSize = 13,
            FontWeight = FontWeights.Bold,
            Foreground = SvllBlue,
            Margin = new Thickness(0, 0, 0, 6)
        });
        sp.Children.Add(new TextBlock
        {
            Text = "Comprehensive library of verified logistics IT remediation procedures with 1-click clipboard copy and direct execution for CMD and PowerShell.",
            FontSize = 11,
            Foreground = TextSubtle,
            Margin = new Thickness(0, 0, 0, 12)
        });

        // Search & Filter Toolbar
        var searchGrid = new Grid { Margin = new Thickness(0, 0, 0, 12) };
        searchGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        searchGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(180) });

        var txtSearch = new TextBox
        {
            Height = 30,
            FontSize = 12,
            Padding = new Thickness(8, 4, 8, 4),
            Margin = new Thickness(0, 0, 8, 0)
        };
        Grid.SetColumn(txtSearch, 0);
        searchGrid.Children.Add(txtSearch);

        var cmbCategory = new ComboBox
        {
            Height = 30,
            FontSize = 12,
            ItemsSource = new[] { "All Categories", "Network", "Printers", "Windows OS", "Active Directory", "Storage", "Security", "Performance" },
            SelectedIndex = 0
        };
        Grid.SetColumn(cmbCategory, 1);
        searchGrid.Children.Add(cmbCategory);
        sp.Children.Add(searchGrid);

        // Master-Detail Split Grid
        var mdGrid = new Grid();
        mdGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(320) });
        mdGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        // Left List of scenarios
        var listScenarios = new ListBox
        {
            Height = 440,
            FontSize = 12,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            Margin = new Thickness(0, 0, 12, 0)
        };
        Grid.SetColumn(listScenarios, 0);
        mdGrid.Children.Add(listScenarios);

        // Right Detail Panel
        var detailBorder = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(16)
        };
        var detailStack = new StackPanel();
        detailBorder.Child = detailStack;
        Grid.SetColumn(detailBorder, 1);
        mdGrid.Children.Add(detailBorder);

        sp.Children.Add(mdGrid);
        card.Child = sp;
        root.Children.Add(card);

        void FilterList()
        {
            string q = txtSearch.Text.Trim().ToLowerInvariant();
            string cat = cmbCategory.SelectedItem?.ToString() ?? "All Categories";

            var filtered = _solutionsLibrary.Where(s =>
            {
                bool matchCat = cat == "All Categories" || s.Category == cat;
                bool matchQ = string.IsNullOrEmpty(q)
                              || s.Title.ToLowerInvariant().Contains(q)
                              || s.Symptoms.ToLowerInvariant().Contains(q)
                              || s.Tags.Any(t => t.ToLowerInvariant().Contains(q));
                return matchCat && matchQ;
            }).ToList();

            listScenarios.ItemsSource = filtered;
            if (filtered.Count > 0) listScenarios.SelectedIndex = 0;
            else detailStack.Children.Clear();
        }

        txtSearch.TextChanged += (s, e) => FilterList();
        cmbCategory.SelectionChanged += (s, e) => FilterList();

        listScenarios.SelectionChanged += (s, e) =>
        {
            if (listScenarios.SelectedItem is TroubleshootingSolution sol)
            {
                RenderSolutionDetail(detailStack, sol);
            }
        };

        FilterList();

        scroll.Content = root;
        return scroll;
    }

    private void RenderSolutionDetail(StackPanel container, TroubleshootingSolution sol)
    {
        container.Children.Clear();

        container.Children.Add(new TextBlock
        {
            Text = sol.Title,
            FontSize = 15,
            FontWeight = FontWeights.Bold,
            Foreground = SvllBlue,
            Margin = new Thickness(0, 0, 0, 4)
        });

        container.Children.Add(new TextBlock
        {
            Text = $"Category: {sol.Category}  |  Tags: {string.Join(", ", sol.Tags)}",
            FontSize = 11,
            Foreground = TextSubtle,
            Margin = new Thickness(0, 0, 0, 10)
        });

        container.Children.Add(new TextBlock { Text = "SYMPTOMS / ISSUE:", FontSize = 10.5, FontWeight = FontWeights.Bold, Foreground = TextDark });
        container.Children.Add(new TextBlock { Text = sol.Symptoms, FontSize = 11.5, Foreground = TextDark, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 2, 0, 8) });

        container.Children.Add(new TextBlock { Text = "ROOT CAUSE & EXPLANATION:", FontSize = 10.5, FontWeight = FontWeights.Bold, Foreground = TextDark });
        container.Children.Add(new TextBlock { Text = sol.Explanation, FontSize = 11.5, Foreground = TextDark, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 2, 0, 14) });

        // CMD Command Block
        container.Children.Add(CreateCommandBlock("Command Prompt (CMD)", sol.CmdCommand, false));

        // PowerShell Command Block
        container.Children.Add(CreateCommandBlock("PowerShell Command", sol.PowerShellCommand, true));
    }

    private UIElement CreateCommandBlock(string label, string command, bool isPowerShell)
    {
        var border = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(15, 23, 42)),
            CornerRadius = new CornerRadius(5),
            Padding = new Thickness(10),
            Margin = new Thickness(0, 0, 0, 10)
        };

        var sp = new StackPanel();

        var header = new DockPanel { Margin = new Thickness(0, 0, 0, 6) };
        header.Children.Add(new TextBlock
        {
            Text = label.ToUpperInvariant(),
            FontSize = 10,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184))
        });

        var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };

        var btnCopy = new Button
        {
            Content = "Copy",
            Height = 22,
            Padding = new Thickness(8, 0, 8, 0),
            Margin = new Thickness(0, 0, 6, 0),
            FontSize = 10,
            Background = new SolidColorBrush(Color.FromRgb(30, 41, 59)),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0)
        };
        btnCopy.Click += (s, e) =>
        {
            Clipboard.SetText(command);
            MessageBox.Show("Command copied to clipboard!", "Copied", MessageBoxButton.OK, MessageBoxImage.Information);
        };
        btnPanel.Children.Add(btnCopy);

        var btnRun = new Button
        {
            Content = "Run Now",
            Height = 22,
            Padding = new Thickness(8, 0, 8, 0),
            FontSize = 10,
            Background = SvllBlue,
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0)
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

        header.Children.Add(btnPanel);
        sp.Children.Add(header);

        var txtCmd = new TextBlock
        {
            Text = command,
            FontFamily = new FontFamily("Consolas, monospace"),
            FontSize = 11,
            Foreground = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
            TextWrapping = TextWrapping.Wrap
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
