using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using SVLL_IT_Workstation.Models;

namespace SVLL_IT_Workstation;

public partial class MainWindow
{
    private List<PaletteCommandItem> _paletteCommands = new();

    public void OpenCommandPalette()
    {
        InitializePaletteCommands();

        var dlg = new Window
        {
            Title = "SVLL Command Palette (Ctrl+K)",
            WindowStyle = WindowStyle.None,
            AllowsTransparency = true,
            Background = Brushes.Transparent,
            Width = 620,
            Height = 440,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this,
            ShowInTaskbar = false
        };

        var outerBorder = new Border
        {
            Background = BgCard,
            BorderBrush = SvllBlue,
            BorderThickness = new Thickness(1.5),
            CornerRadius = new CornerRadius(14),
            Padding = new Thickness(0),
            Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                BlurRadius = 24,
                ShadowDepth = 6,
                Opacity = 0.35,
                Color = Colors.Black
            }
        };

        var mainLayout = new Grid();
        mainLayout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Search Box
        mainLayout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Results
        mainLayout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Footer / Instructions

        // 1. Search Box Header
        var searchBorder = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(0, 0, 0, 1),
            CornerRadius = new CornerRadius(14, 14, 0, 0),
            Padding = new Thickness(16, 12, 16, 12)
        };
        var searchGrid = new Grid();
        searchGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        searchGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        searchGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var icon = new TextBlock
        {
            Text = "🔍",
            FontSize = 16,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 10, 0)
        };
        Grid.SetColumn(icon, 0);
        searchGrid.Children.Add(icon);

        var txtSearch = new TextBox
        {
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            Foreground = TextDark,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(txtSearch, 1);
        searchGrid.Children.Add(txtSearch);

        var kbdEsc = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(6, 2, 6, 2),
            Child = new TextBlock { Text = "ESC", FontSize = 10, FontWeight = FontWeights.Bold, Foreground = TextSubtle }
        };
        Grid.SetColumn(kbdEsc, 2);
        searchGrid.Children.Add(kbdEsc);

        searchBorder.Child = searchGrid;
        Grid.SetRow(searchBorder, 0);
        mainLayout.Children.Add(searchBorder);

        // 2. Results List
        var lbCommands = new ListBox
        {
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Margin = new Thickness(6)
        };
        ScrollViewer.SetHorizontalScrollBarVisibility(lbCommands, ScrollBarVisibility.Disabled);
        ScrollViewer.SetVerticalScrollBarVisibility(lbCommands, ScrollBarVisibility.Auto);

        void RefreshList(string query)
        {
            lbCommands.Items.Clear();
            string q = query.Trim().ToLowerInvariant();
            var matches = string.IsNullOrEmpty(q)
                ? _paletteCommands
                : _paletteCommands.Where(c =>
                    c.Title.ToLowerInvariant().Contains(q) ||
                    c.Subtitle.ToLowerInvariant().Contains(q) ||
                    c.Category.ToLowerInvariant().Contains(q)).ToList();

            foreach (var cmd in matches)
            {
                var itemGrid = new Grid { Margin = new Thickness(8, 4, 8, 4) };
                itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(34) });
                itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var cmdIcon = new TextBlock
                {
                    Text = cmd.Icon,
                    FontSize = 16,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(cmdIcon, 0);
                itemGrid.Children.Add(cmdIcon);

                var textStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                textStack.Children.Add(new TextBlock
                {
                    Text = cmd.Title,
                    FontSize = 12,
                    FontWeight = FontWeights.Bold,
                    Foreground = TextDark
                });
                textStack.Children.Add(new TextBlock
                {
                    Text = cmd.Subtitle,
                    FontSize = 10,
                    Foreground = TextSubtle,
                    Margin = new Thickness(0, 1, 0, 0)
                });
                Grid.SetColumn(textStack, 1);
                itemGrid.Children.Add(textStack);

                var catBadge = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(6, 2, 6, 2),
                    VerticalAlignment = VerticalAlignment.Center,
                    Child = new TextBlock { Text = cmd.Category, FontSize = 9.5, FontWeight = FontWeights.SemiBold, Foreground = TextSubtle }
                };
                Grid.SetColumn(catBadge, 2);
                itemGrid.Children.Add(catBadge);

                var lbi = new ListBoxItem
                {
                    Content = itemGrid,
                    Tag = cmd,
                    Padding = new Thickness(4),
                    Cursor = Cursors.Hand
                };
                lbCommands.Items.Add(lbi);
            }

            if (lbCommands.Items.Count > 0)
                lbCommands.SelectedIndex = 0;
        }

        txtSearch.TextChanged += (s, e) => RefreshList(txtSearch.Text);
        RefreshList("");

        void ExecuteSelected()
        {
            if (lbCommands.SelectedItem is ListBoxItem lbi && lbi.Tag is PaletteCommandItem selected)
            {
                dlg.Close();
                if (selected.CustomAction != null)
                {
                    selected.CustomAction.Invoke();
                }
                else if (!string.IsNullOrEmpty(selected.TargetTag))
                {
                    NavigateTo(selected.TargetTag);
                }
            }
        }

        lbCommands.MouseDoubleClick += (s, e) => ExecuteSelected();

        dlg.KeyDown += (s, e) =>
        {
            if (e.Key == Key.Escape)
            {
                dlg.Close();
                e.Handled = true;
            }
            else if (e.Key == Key.Enter)
            {
                ExecuteSelected();
                e.Handled = true;
            }
            else if (e.Key == Key.Down)
            {
                if (lbCommands.SelectedIndex < lbCommands.Items.Count - 1)
                    lbCommands.SelectedIndex++;
                lbCommands.ScrollIntoView(lbCommands.SelectedItem);
                e.Handled = true;
            }
            else if (e.Key == Key.Up)
            {
                if (lbCommands.SelectedIndex > 0)
                    lbCommands.SelectedIndex--;
                lbCommands.ScrollIntoView(lbCommands.SelectedItem);
                e.Handled = true;
            }
        };

        Grid.SetRow(lbCommands, 1);
        mainLayout.Children.Add(lbCommands);

        // 3. Footer
        var footerBorder = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(0, 1, 0, 0),
            CornerRadius = new CornerRadius(0, 0, 14, 14),
            Padding = new Thickness(16, 8, 16, 8)
        };
        var footerGrid = new Grid();
        footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var footerHint = new TextBlock
        {
            Text = "↑↓ to navigate • ↵ Enter to execute • ESC to dismiss",
            FontSize = 10,
            Foreground = TextSubtle,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(footerHint, 0);
        footerGrid.Children.Add(footerHint);

        var svllTag = new TextBlock
        {
            Text = "Shree Vasu Logistics Limited",
            FontSize = 10,
            FontWeight = FontWeights.Bold,
            Foreground = SvllBlue,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(svllTag, 1);
        footerGrid.Children.Add(svllTag);

        footerBorder.Child = footerGrid;
        Grid.SetRow(footerBorder, 2);
        mainLayout.Children.Add(footerBorder);

        outerBorder.Child = mainLayout;
        dlg.Content = outerBorder;

        dlg.Loaded += (s, e) => txtSearch.Focus();
        dlg.ShowDialog();
    }

    private void InitializePaletteCommands()
    {
        if (_paletteCommands.Count > 0) return;

        _paletteCommands = new List<PaletteCommandItem>
        {
            // Core v6.0 Advanced Tools
            new() { Title = "1-Click RAM & CPU Turbo Booster", Subtitle = "Flush working sets, kill telemetry & activate Ultimate Performance", Category = "OPTIMIZATION", Icon = "⚡", TargetTag = "TurboBooster" },
            new() { Title = "Zero-Touch New PC Provisioning Wizard", Subtitle = "1-click automated commissioning pipeline for new PCs", Category = "PROVISIONING", Icon = "🚀", TargetTag = "ProvisioningWizard" },
            new() { Title = "Logistics Hardware Doctor (Thermal & Serial)", Subtitle = "Zebra/TSC direct RAW ZPL injection & RS-232 serial sniffer", Category = "LOGISTICS", Icon = "🖨️", TargetTag = "LogisticsDoctor" },
            new() { Title = "Endpoint Security, DLP & USB Port Lockdown", Subtitle = "Read-Only USB storage policies, Defender hygiene & LAPS vault", Category = "SECURITY", Icon = "🔒", TargetTag = "SecurityDlp" },
            new() { Title = "S.M.A.R.T. Drive Health & Crash Predictor", Subtitle = "Inspect NVMe/SATA controller registers, SSD wear % & bad sectors", Category = "STORAGE", Icon = "💾", TargetTag = "StorageSmart" },
            new() { Title = "Active Ports & Bandwidth Hog Hunter", Subtitle = "Map active TCP connections to process binaries and terminate hogs", Category = "NETWORK", Icon = "📡", TargetTag = "SocketInspector" },

            // Hub 1: Monitoring
            new() { Title = "Executive Telemetry Dashboard", Subtitle = "Live CPU, RAM, disk, IP and top memory processes", Category = "MONITORING", Icon = "📊", TargetTag = "Dashboard" },
            new() { Title = "24/7 Continuous Ping Watchdog", Subtitle = "Multi-target automated gateway, DNS and ERP ping monitor", Category = "MONITORING", Icon = "⚡", TargetTag = "PingMonitor" },
            new() { Title = "WMS & ERP Latency Diagnostics", Subtitle = "Test latency to warehouse database and logistics ERP servers", Category = "MONITORING", Icon = "🏢", TargetTag = "WmsLatency" },
            new() { Title = "Internet Speed & Bandwidth Test", Subtitle = "Measure upload/download throughput and jitter", Category = "MONITORING", Icon = "🚀", TargetTag = "SpeedTest" },

            // Hub 2: Network
            new() { Title = "IPConfig & Netsh Network Suite", Subtitle = "Network adapter diagnostics, release/renew IP and ARP reset", Category = "NETWORK", Icon = "📋", TargetTag = "NetshSuite" },
            new() { Title = "Network & DNS Profiles Switcher", Subtitle = "Switch between DHCP, Static Logistics IPs and DNS presets", Category = "NETWORK", Icon = "🌐", TargetTag = "Network" },
            new() { Title = "Subnet IP Scanner (Free vs Occupied)", Subtitle = "Scan /24 subnets to find available unallocated IP addresses", Category = "NETWORK", Icon = "🔍", TargetTag = "SubnetScanner" },
            new() { Title = "Wi-Fi Diagnostics & Saved Keys", Subtitle = "Enumerate Wi-Fi profiles, signal strength and view saved WPA2 keys", Category = "NETWORK", Icon = "📶", TargetTag = "Wifi" },
            new() { Title = "LAN File Shares & SMB Auditor", Subtitle = "Enumerate active SMB network shares and mapped network drives", Category = "NETWORK", Icon = "📂", TargetTag = "LanShares" },

            // Hub 3: Optimization
            new() { Title = "WinUtil Enterprise Debloat & Tweaks", Subtitle = "Remove Cortana, GameBar, consumer bloat and telemetry", Category = "OPTIMIZATION", Icon = "⚙️", TargetTag = "WinUtilTweaks" },
            new() { Title = "Disk Cleanup & Temp File Purge", Subtitle = "Purge Windows Temp, Prefetch, delivery caches and Recycle Bin", Category = "OPTIMIZATION", Icon = "🧹", TargetTag = "Cleanup" },
            new() { Title = "Storage Drive Optimization & TRIM", Subtitle = "Trigger SSD TRIM and defragment traditional storage drives", Category = "OPTIMIZATION", Icon = "💾", TargetTag = "Disk" },
            new() { Title = "Windows Optional Features Manager", Subtitle = "Enable/disable Hyper-V, WSL, Sandbox and legacy components", Category = "OPTIMIZATION", Icon = "🧩", TargetTag = "WinFeatures" },
            new() { Title = "Windows Update Strategy & Pause", Subtitle = "Pause updates, configure metered connection, or trigger update checks", Category = "OPTIMIZATION", Icon = "🔄", TargetTag = "WinUpdateConfig" },
            new() { Title = "Baseline Profile Configuration Manager", Subtitle = "Save and import standardized workstation baseline settings", Category = "OPTIMIZATION", Icon = "📋", TargetTag = "ConfigManager" },

            // Hub 4: Diagnostics
            new() { Title = "Windows OS Repair (SFC & DISM)", Subtitle = "Repair corrupted system files, component store and BCD", Category = "DIAGNOSTICS", Icon = "🩺", TargetTag = "Repair" },
            new() { Title = "Print Spooler Diagnostic & Clear", Subtitle = "Restart printer spooler service and clear stuck print jobs", Category = "DIAGNOSTICS", Icon = "🖨️", TargetTag = "Printer" },
            new() { Title = "Services & Process Controller", Subtitle = "Inspect running services, startup programs and processes", Category = "DIAGNOSTICS", Icon = "⚡", TargetTag = "Services" },
            new() { Title = "Windows Event Log Analyzer", Subtitle = "Inspect System and Application critical errors and crash logs", Category = "DIAGNOSTICS", Icon = "⚠️", TargetTag = "EventLog" },
            new() { Title = "PC Health & Battery Diagnostics", Subtitle = "Generate battery degradation report and hardware health metrics", Category = "DIAGNOSTICS", Icon = "🔋", TargetTag = "Health" },
            new() { Title = "Hardware Asset Passport & QR Code", Subtitle = "View motherboard, BIOS, MAC addresses and export QR passport", Category = "DIAGNOSTICS", Icon = "🛡️", TargetTag = "AssetPassport" },
            new() { Title = "IT Support Incident Bundle (ZIP)", Subtitle = "Compile complete system diagnostic logs into a zip for IT helpdesk", Category = "DIAGNOSTICS", Icon = "📦", TargetTag = "SupportBundle" },

            // Hub 5: Playbook & Deployment
            new() { Title = "37 IT Dual-Syntax Command Playbook", Subtitle = "Instant PowerShell & CMD recipes for 37 common enterprise fixes", Category = "PLAYBOOK", Icon = "📖", TargetTag = "Library" },
            new() { Title = "Offline USB Software & Driver Depot", Subtitle = "Auto-scan USB drives for software packages and WHQL drivers", Category = "DEPLOYMENT", Icon = "🔌", TargetTag = "UsbDepot" },
            new() { Title = "App Uninstaller & Nuclear Force Purge", Subtitle = "Bypass uninstall passwords, take ownership and wipe stubborn apps", Category = "DEPLOYMENT", Icon = "🗑️", TargetTag = "AppUninstaller" },
            new() { Title = "WinGet Enterprise Software Deployer", Subtitle = "Search and unattended install hundreds of verified apps from WinGet", Category = "DEPLOYMENT", Icon = "📦", TargetTag = "WinGetSoftware" },
            new() { Title = "Local Users & Windows Vault Manager", Subtitle = "Audit local user accounts, reset passwords and query credential vault", Category = "SECURITY", Icon = "👤", TargetTag = "Users" },
            new() { Title = "Updates & About SVLL Workstation", Subtitle = "View build info, check for latest releases on GitHub", Category = "SYSTEM", Icon = "ℹ️", TargetTag = "About" },

            // Hub 6: Remote Access & Fleet Console
            new() { Title = "My Remote Access Client & AnyDesk Status", Subtitle = "Inspect local AnyDesk ID, toggle RDP and launch Quick Assist", Category = "REMOTE", Icon = "🔴", TargetTag = "RemoteClient" },
            new() { Title = "Fleet Device Management & Admin Console", Subtitle = "Live monitor and 1-click AnyDesk connection to all fleet workstations", Category = "REMOTE", Icon = "👑", TargetTag = "FleetAdminConsole" },

            // Hub 7: IT Complaints & AI Service Desk
            new() { Title = "Register IT Support Complaint Ticket", Subtitle = "Log workstation, printer, or network incidents with auto-diagnostics", Category = "HELPDESK", Icon = "📋", TargetTag = "SubmitComplaint" },
            new() { Title = "AI Intelligent IT Diagnostic & Intake Assistant", Subtitle = "Interactive conversational triage bot with clarifying questions & quick fixes", Category = "HELPDESK", Icon = "🤖", TargetTag = "AiComplaintBot" },
            new() { Title = "Central IT Service Desk Admin Panel", Subtitle = "Admin dashboard to manage, update, and resolve incident tickets", Category = "HELPDESK", Icon = "👑", TargetTag = "TicketAdminDesk" },
            new() { Title = "Track My Submitted Complaints", Subtitle = "View real-time resolution progress and technician notes for this PC", Category = "HELPDESK", Icon = "📂", TargetTag = "MyTickets" },

            // Fast Instant Actions
            new() {
                Title = "Quick Action: Flush DNS Resolver Cache",
                Subtitle = "Executes ipconfig /flushdns to clear poisoned or stale DNS lookups",
                Category = "QUICK ACTION",
                Icon = "⚡",
                CustomAction = () =>
                {
                    ExecutePowerShellScript("Clear-DnsClientCache");
                    Log("[COMMAND PALETTE] DNS resolver cache flushed successfully.");
                    MessageBox.Show("✔ DNS Resolver Cache successfully flushed (Clear-DnsClientCache).", "DNS Flushed", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            },
            new() {
                Title = "Quick Action: Purge Temp Directory & Recycle Bin",
                Subtitle = "Instantly clears user temp directory and empties Windows Recycle Bin",
                Category = "QUICK ACTION",
                Icon = "🧹",
                CustomAction = async () =>
                {
                    await Task.Run(() =>
                    {
                        try { CleanDirectory(System.IO.Path.GetTempPath()); } catch { }
                        try { SHEmptyRecycleBin(IntPtr.Zero, null!, 7); } catch { }
                    });
                    Log("[COMMAND PALETTE] Temp files and Recycle Bin purged.");
                    MessageBox.Show("✔ Temp directory and Recycle Bin purged.", "Junk Cleared", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        };
    }
}
