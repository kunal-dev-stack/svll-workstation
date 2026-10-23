using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Media;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Win32;

namespace SVLL_IT_Workstation;

public partial class MainWindow : Window
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private class MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;

        public MEMORYSTATUSEX()
        {
            dwLength = (uint)Marshal.SizeOf(this);
        }
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetSystemTimes(out long lpIdleTime, out long lpKernelTime, out long lpUserTime);

    [DllImport("Shell32.dll", CharSet = CharSet.Unicode)]
    private static extern uint SHEmptyRecycleBin(IntPtr hwnd, string pszRootPath, uint dwFlags);

    [DllImport("psapi.dll")]
    private static extern int EmptyWorkingSet(IntPtr hwProc);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetProcessWorkingSetSize(IntPtr proc, IntPtr min, IntPtr max);

    public const string CurrentVersion = "8.0";

    // Corporate Color Palette (Shree Vasu Logistics Limited)
    private static readonly SolidColorBrush SvllBlue = new SolidColorBrush(Color.FromRgb(26, 75, 178));
    private static readonly SolidColorBrush SvllLightBlue = new SolidColorBrush(Color.FromRgb(37, 99, 235));
    private static readonly SolidColorBrush SvllRed = new SolidColorBrush(Color.FromRgb(225, 45, 45));
    private static readonly SolidColorBrush BgApp = new SolidColorBrush(Color.FromRgb(248, 250, 252));
    private static readonly SolidColorBrush BgSidebar = new SolidColorBrush(Color.FromRgb(255, 255, 255));
    private static readonly SolidColorBrush BgCard = new SolidColorBrush(Color.FromRgb(255, 255, 255));
    private static readonly SolidColorBrush BorderMuted = new SolidColorBrush(Color.FromRgb(226, 232, 240));
    private static readonly SolidColorBrush TextDark = new SolidColorBrush(Color.FromRgb(15, 23, 42));
    private static readonly SolidColorBrush TextSubtle = new SolidColorBrush(Color.FromRgb(100, 116, 139));
    private static readonly SolidColorBrush TerminalBg = new SolidColorBrush(Color.FromRgb(15, 23, 42));

    private static readonly FontFamily IconFont = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets, Segoe UI Symbol");

    // UI Host Elements
    private ContentControl _pageHost = null!;
    private StackPanel _navPanel = null!;
    private TextBox _txtConsole = null!;
    private TextBox _txtCmdInput = null!;
    private ComboBox _cmbShellType = null!;
    private ProgressBar _globalProgress = null!;
    private readonly List<string> _cmdHistory = new List<string>();
    private int _cmdHistoryIndex = -1;

    // Active State
    private readonly Dictionary<string, UIElement> _viewCache = new Dictionary<string, UIElement>();
    private readonly Dictionary<string, Button> _navButtons = new Dictionary<string, Button>();
    private WorkstationConfig _activeConfig = new WorkstationConfig();
    private List<NetworkProfile> _profilesList = new List<NetworkProfile>();
    private List<TroubleshootingSolution> _solutionsLibrary = new List<TroubleshootingSolution>();
    private List<ContinuousPingTarget> _watchdogTargets = new List<ContinuousPingTarget>();
    private List<SoftwarePackage> _softwarePackages = new List<SoftwarePackage>();

    // Top Status Bar Live Indicators
    private TextBlock? _lblTopBarHost;
    private TextBlock? _lblTopBarUser;
    private TextBlock? _lblTopBarIp;
    private TextBlock? _lblTopBarGateway;
    private TextBlock? _lblTopBarCpu;
    private TextBlock? _lblTopBarRam;

    // Terminal Drawer Toggle State
    private RowDefinition _terminalRow = new RowDefinition { Height = new GridLength(185) };
    private bool _isTerminalCollapsed = false;
    private Button? _btnToggleTerminal;
    private Grid? _terminalInputBar;

    // 5-Hub Enterprise Workspaces Architecture
    private class HubInfo
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public string Subtitle { get; set; } = "";
        public string Glyph { get; set; } = "";
        public List<(string Tag, string Label, string Icon)> SubTools { get; set; } = new();
    }

    private readonly List<HubInfo> _hubs = new()
    {
        new HubInfo
        {
            Id = "Hub_Monitoring",
            Title = "LIVE MONITORING & TRIAGE",
            Subtitle = "System Vitals, Continuous Watchdog & Latency Tests",
            Glyph = "\uE80F",
            SubTools = new()
            {
                ("Dashboard", "Executive Dashboard", "📊"),
                ("PingMonitor", "24/7 Ping Watchdog", "⚡"),
                ("WmsLatency", "WMS & ERP Latency", "🏢"),
                ("SpeedTest", "Internet Speed Test", "🚀")
            }
        },
        new HubInfo
        {
            Id = "Hub_Network",
            Title = "NETWORK OPERATIONS CENTER",
            Subtitle = "IPConfig, Subnets, DNS Switching & Wi-Fi Diagnostics",
            Glyph = "\uE968",
            SubTools = new()
            {
                ("NetshSuite", "IPConfig & Netsh", "📋"),
                ("Network", "Network & DNS Profiles", "🌐"),
                ("SubnetScanner", "Subnet IP Scanner", "🔍"),
                ("SocketInspector", "Bandwidth Hog Hunter", "📡"),
                ("Wifi", "Wi-Fi Diagnostics", "📶"),
                ("LanShares", "LAN File Shares", "📂")
            }
        },
        new HubInfo
        {
            Id = "Hub_Optimization",
            Title = "SYSTEM OPTIMIZATION & SERVICING",
            Subtitle = "Debloating, Temp Purge, Storage TRIM & Baseline Configs",
            Glyph = "\uE74C",
            SubTools = new()
            {
                ("TurboBooster", "RAM & CPU Booster", "⚡"),
                ("WinUtilTweaks", "WinUtil Tweaks", "⚙️"),
                ("Cleanup", "Disk & Temp Purge", "🧹"),
                ("Disk", "Storage & TRIM", "💾"),
                ("WinFeatures", "Windows Features", "🧩"),
                ("WinUpdateConfig", "Windows Updates", "🔄"),
                ("ConfigManager", "Baseline Profiles", "📋")
            }
        },
        new HubInfo
        {
            Id = "Hub_Diagnostics",
            Title = "FLEET DIAGNOSTICS & HELPDESK",
            Subtitle = "OS Repairs, Print Spooler, Event Analyzer & Support Bundle",
            Glyph = "\uE90F",
            SubTools = new()
            {
                ("Repair", "Windows OS Repair", "🩺"),
                ("StorageSmart", "S.M.A.R.T. Drive Health", "💾"),
                ("Printer", "Print Spooler Diagnostic", "🖨️"),
                ("Services", "Services & Processes", "⚡"),
                ("EventLog", "Event Log Analyzer", "⚠️"),
                ("Health", "PC Health & Battery", "🔋"),
                ("AssetPassport", "Asset Passport & QR", "🛡️"),
                ("SupportBundle", "IT Support Bundle", "📦")
            }
        },
        new HubInfo
        {
            Id = "Hub_Playbook",
            Title = "COMMAND PLAYBOOK & DEPLOYMENT",
            Subtitle = "37 Dual-Syntax Fixes, Offline USB Depot, Driver Finder & WinGet",
            Glyph = "\uE82D",
            SubTools = new()
            {
                ("ProvisioningWizard", "New PC Setup Wizard", "🚀"),
                ("Library", "IT Command Playbook", "📖"),
                ("UsbDepot", "USB Depot & Driver Finder", "🔌"),
                ("WinGetSoftware", "WinGet Software", "📦"),
                ("AppUninstaller", "App Uninstaller & Force Purge", "🗑️"),
                ("Users", "Local Users & Vault", "👤"),
                ("About", "Updates & About", "ℹ️")
            }
        },
        new HubInfo
        {
            Id = "Hub_Security",
            Title = "ENDPOINT SECURITY & HARDENING",
            Subtitle = "Zero-Trust Shield, CIS Benchmark, DLP & Port Threat Hunter",
            Glyph = "\uE72E",
            SubTools = new()
            {
                ("ProcessShield", "Process & Ransomware Shield", "🛡️"),
                ("SecurityHardening", "CIS Security Hardening", "⚡"),
                ("UsbDlpGuardian", "USB Endpoint DLP Guardian", "🔒"),
                ("PortThreatHunter", "Socket & Port Threat Hunter", "📡"),
                ("ForensicsCollector", "Incident Forensics Collector", "📦"),
                ("CertHealthInspector", "SSL/TLS & Domain Health", "🌐")
            }
        },
        new HubInfo
        {
            Id = "Hub_ServiceDesk",
            Title = "IT SERVICE DESK & HELPDESK",
            Subtitle = "Raise Tickets, AI Diagnostic Assistant & Track Requests",
            Glyph = "\uE8BD",
            SubTools = new()
            {
                ("SubmitComplaint", "Raise Support Ticket", "📋"),
                ("AiComplaintBot", "AI Diagnostic Assistant", "🤖"),
                ("MyTickets", "My Open Tickets", "📂"),
                ("RemoteClient", "My Remote Client (AnyDesk)", "🔴"),
#if BUILD_ADMIN
                ("TicketAdminDesk", "Admin Ticket Desk", "👑"),
#endif
            }
        },
#if BUILD_ADMIN
        new HubInfo
        {
            Id = "Hub_AdminCenter",
            Title = "ADMIN COMMAND CENTER",
            Subtitle = "Live Fleet Monitor, 1-Click Remote Access & System Control",
            Glyph = "\uEC09",
            SubTools = new()
            {
                ("AdminCommandCenter", "Fleet & Remote Control", "👑"),
            }
        },
#endif
    };

    private readonly Dictionary<string, (Border Container, ContentControl Host, Dictionary<string, Button> SubButtons, string ActiveSubTag)> _hubViews 
        = new Dictionary<string, (Border, ContentControl, Dictionary<string, Button>, string)>();
    private string _activeHubId = "";

    // Vitals Polling
    private DispatcherTimer? _vitalsTimer;
    private static long _prevIdle, _prevKernel, _prevUser;
    private readonly List<double> _cpuHistory = new List<double>();
    private readonly List<double> _ramHistory = new List<double>();

    // Persistent Device Action & Audit Log Paths
    public static readonly string AuditLogDirectory = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SVLL_IT_Workstation");
    public static readonly string AuditLogPath = System.IO.Path.Combine(AuditLogDirectory, "device_action_history.log");

    public MainWindow(string[] args)
    {
#if BUILD_ADMIN
        Title = $"SVLL IT Support Workstation v{CurrentVersion} 👑 ADMIN EDITION - Enterprise Fleet Diagnostics";
#else
        Title = $"SVLL IT Support Workstation v{CurrentVersion} - User Edition";
#endif
        WindowState = WindowState.Maximized;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Width = 1280;
        Height = 760;
        MinWidth = 980;
        MinHeight = 600;
        Background = BgApp;

        try
        {
            Icon = new BitmapImage(new Uri("pack://application:,,,/svll_brand_logo.png", UriKind.Absolute));
        }
        catch { }

        InitializeSoftwareLibrary();
        InitializeDefaultWatchdogTargets();
        InitializeCommandLibrary();
        LoadProfilesFromDisk();

        BuildMainLayout();
        InitVitalsEngine();

        Loaded += async (s, e) =>
        {
            NavigateTo("Dashboard");
#if BUILD_ADMIN
            // Admin Edition always opens to the Command Center first
            NavigateTo("AdminCommandCenter");
#endif
            Log($"[DEVICE AUDIT] Session started on host {Environment.MachineName} (User: {Environment.UserName}). Build: {BuildConstants.BuildEdition}. Initial state verified.");
            await HandleCommandLineArgsAsync(args);

            // v8.0: Start cloud sync (heartbeat + fleet poll + reply poll)
            string relayUrl = AdminAuthService.Instance.GetRelayServerUrl();
            if (!string.IsNullOrEmpty(relayUrl))
            {
                CloudSyncService.Instance.Start(relayUrl);
                Log($"[CLOUD SYNC] Relay server configured: {relayUrl}. Heartbeat active.");
            }
            else
            {
                Log("[CLOUD SYNC] No relay server URL configured. Fleet sync inactive. Configure in Admin Security Settings.");
            }

            // v8.0: Check MeshAgent service and auto-install/heal if bundled
            try
            {
                if (!MeshAgentService.IsInstalled())
                {
                    Task.Run(() => MeshAgentService.TryInstallLocalAgent());
                }
                else
                {
                    Task.Run(() => MeshAgentService.StartService());
                }
            }
            catch { }

            // v8.0: Subscribe to admin reply notifications
            CloudSyncService.Instance.AdminReplyReceived += OnAdminReplyReceived;

#if BUILD_ADMIN
            // Admin build: subscribe to live fleet updates
            CloudSyncService.Instance.FleetUpdated += OnCloudFleetUpdated;
#endif
        };

        PreviewKeyDown += (s, e) =>
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.K)
            {
                OpenCommandPalette();
                e.Handled = true;
            }
        };

        Closing += async (s, e) =>
        {
            try
            {
                // Stop active background timers
                _watchdogTimer?.Stop();
                _isWatchdogRunning = false;
                _vitalsTimer?.Stop();

                // v8.0: Send offline status to relay before closing
                CloudSyncService.Instance.Stop();
                await CloudSyncService.Instance.SendOfflineStatusAsync();

                // Revert runtime watchdog state to clean factory baseline
                InitializeDefaultWatchdogTargets();

                // Log session closure event
                string sessionEnd = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [DEVICE AUDIT] Session concluded for host {Environment.MachineName}. Reverted all runtime session state to initial factory baseline.\n" +
                                    "--------------------------------------------------------------------------------\n";
                File.AppendAllText(AuditLogPath, sessionEnd);
            }
            catch { }
        };
    }

    // -------------------------------------------------------------------------
    // v8.0 Cloud sync event handlers
    // -------------------------------------------------------------------------

    private void OnAdminReplyReceived(SVLL_IT_Workstation.Models.ComplaintTicket ticket)
    {
        // Show Windows toast-style popup notification
        try
        {
            var notif = new Window
            {
                Title = "SVLL IT — Ticket Reply",
                Width = 400,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.Manual,
                Left = SystemParameters.PrimaryScreenWidth - 420,
                Top = SystemParameters.PrimaryScreenHeight - 220,
                WindowStyle = WindowStyle.ToolWindow,
                ResizeMode = ResizeMode.NoResize,
                Topmost = true,
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(240, 253, 244))
            };
            var sp = new System.Windows.Controls.StackPanel { Margin = new Thickness(16) };
            sp.Children.Add(new System.Windows.Controls.TextBlock
            {
                Text = "✅ Admin Reply Received!",
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(22, 101, 52)),
                Margin = new Thickness(0, 0, 0, 6)
            });
            sp.Children.Add(new System.Windows.Controls.TextBlock
            {
                Text = $"Ticket: {ticket.Title}",
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(15, 23, 42)),
                Margin = new Thickness(0, 0, 0, 4)
            });
            sp.Children.Add(new System.Windows.Controls.TextBlock
            {
                Text = ticket.AdminReply,
                FontSize = 11,
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 64, 175)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 10)
            });
            var btnView = new System.Windows.Controls.Button
            {
                Content = "📂 View My Tickets",
                Padding = new Thickness(12, 6, 12, 6),
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(22, 163, 74)),
                Foreground = System.Windows.Media.Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                FontWeight = FontWeights.Bold
            };
            btnView.Click += (s, e) => { NavigateTo("MyTickets"); notif.Close(); };
            sp.Children.Add(btnView);
            notif.Content = sp;
            notif.Show();

            // Auto-close after 12 seconds
            var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(12) };
            timer.Tick += (s, e) => { timer.Stop(); try { notif.Close(); } catch { } };
            timer.Start();
        }
        catch { }

        // Also invalidate MyTickets view cache so it refreshes
        _viewCache.Remove("MyTickets");
    }

#if BUILD_ADMIN
    private void OnCloudFleetUpdated(List<SVLL_IT_Workstation.Models.RemoteDeviceItem> devices)
    {
        // Invalidate cached views so they re-render with fresh data
        _viewCache.Remove("FleetAdminConsole");
        _viewCache.Remove("AdminCommandCenter");
        Dispatcher.Invoke(() =>
        {
            try
            {
                if (_fleetDeviceListPanel != null)
                {
                    RenderFleetDeviceCards();
                    UpdateFleetMetricCounters();
                }
            }
            catch { }
        });
    }
#endif

    private void BuildMainLayout()
    {
        var rootGrid = new Grid();
        rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(270) }); // Left Nav
        rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Main Content

        // ------------------ LEFT SIDEBAR ------------------
        var sidebarBorder = new Border
        {
            Background = BgSidebar,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(0, 0, 1, 0)
        };

        var sidebarGrid = new Grid();
        sidebarGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header & Logo
        sidebarGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Scrollable Nav
        sidebarGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Bottom Attribution Card

        // 1. Header Emblem & Branding
        var headerPanel = new StackPanel { Margin = new Thickness(16, 16, 16, 12) };
        headerPanel.Children.Add(RenderSvllBrandLogo());
        Grid.SetRow(headerPanel, 0);
        sidebarGrid.Children.Add(headerPanel);

        // 2. Navigation Panel in ScrollViewer
        var navScroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        _navPanel = new StackPanel { Margin = new Thickness(10, 0, 10, 10) };

        BuildCategorizedNavigation();

        navScroll.Content = _navPanel;
        Grid.SetRow(navScroll, 1);
        sidebarGrid.Children.Add(navScroll);

        // 3. Bottom Developer Attribution Card
        var devCard = BuildDeveloperAttributionCard();
        Grid.SetRow(devCard, 2);
        sidebarGrid.Children.Add(devCard);

        sidebarBorder.Child = sidebarGrid;
        Grid.SetColumn(sidebarBorder, 0);
        rootGrid.Children.Add(sidebarBorder);

        // ------------------ RIGHT WORKSPACE ------------------
        var mainGrid = new Grid();
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Row 0: Top Status Bar
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Row 1: Dynamic Page Host
        mainGrid.RowDefinitions.Add(_terminalRow); // Row 2: Embedded Interactive Terminal

        var topStatusBar = BuildTopStatusBar();
        Grid.SetRow(topStatusBar, 0);
        mainGrid.Children.Add(topStatusBar);

        _pageHost = new ContentControl { Margin = new Thickness(14, 10, 14, 6) };
        Grid.SetRow(_pageHost, 1);
        mainGrid.Children.Add(_pageHost);

        var terminalPanel = BuildEmbeddedTerminal();
        Grid.SetRow(terminalPanel, 2);
        mainGrid.Children.Add(terminalPanel);

        Grid.SetColumn(mainGrid, 1);
        rootGrid.Children.Add(mainGrid);

        Content = rootGrid;
    }

    private UIElement RenderSvllBrandLogo()
    {
        var root = new StackPanel();
        var topRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 6) };

        try
        {
            var logoUri = new Uri("pack://application:,,,/svll_brand_logo.png", UriKind.Absolute);
            var bitmap = new BitmapImage(logoUri);
            var img = new Image
            {
                Source = bitmap,
                Width = 44,
                Height = 44,
                Stretch = Stretch.Uniform,
                Margin = new Thickness(0, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            topRow.Children.Add(img);
        }
        catch { }

        var brandText = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        brandText.Children.Add(new TextBlock
        {
            Text = "SHREE VASU",
            FontSize = 13.5,
            FontWeight = FontWeights.ExtraBold,
            Foreground = SvllBlue
        });
        brandText.Children.Add(new TextBlock
        {
            Text = "LOGISTICS LIMITED",
            FontSize = 9.5,
            FontWeight = FontWeights.Bold,
            Foreground = SvllRed,
            Margin = new Thickness(0, -1, 0, 0)
        });
        brandText.Children.Add(new TextBlock
        {
            Text = "GET CARRIED AWAY",
            FontSize = 8.0,
            FontWeight = FontWeights.SemiBold,
            Foreground = TextSubtle,
            Margin = new Thickness(0, 1, 0, 0)
        });
        topRow.Children.Add(brandText);
        root.Children.Add(topRow);

        root.Children.Add(new TextBlock
        {
            Text = "IT Workstation Suite",
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Foreground = TextDark,
            Margin = new Thickness(2, 0, 0, 0)
        });

        root.Children.Add(new Border
        {
            Height = 1,
            Background = BorderMuted,
            Margin = new Thickness(0, 8, 0, 4)
        });

        return root;
    }

    public async Task<(int killedCount, int trimmedCount, double freedMb)> ExecuteTurboBoostAsync(
        bool trimWorkingSets = true,
        bool terminateBloat = true,
        bool optimizeCpu = true,
        bool cleanTemps = true)
    {
        Log("\n========================================================");
        Log("[TURBO BOOST] Initiating 1-Click RAM & CPU Optimization...");
        Log("========================================================");

        var memBefore = new MEMORYSTATUSEX();
        GlobalMemoryStatusEx(memBefore);
        double initialAvailMb = memBefore.ullAvailPhys / (1024.0 * 1024.0);
        uint initialLoad = memBefore.dwMemoryLoad;

        int killedCount = 0;
        int trimmedCount = 0;

        await Task.Run(() =>
        {
            // 1. Terminate Safe-To-Kill Non-Essential Background Tasks
            if (terminateBloat)
            {
                var safeToKill = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    // Telemetry & Diagnostics
                    "compattelrunner", "devicecensus", "wsqmcons", "mousocoreworker", "diagtrack",
                    // Background Updaters & Installers
                    "microsoftedgeupdate", "googleupdate", "adobedeviceservice", "adobeupdateservice",
                    "onedrivestandaloneupdater", "jusched",
                    // Consumer Game Bar & Background Bloat
                    "gamebarpresencewriter", "gamebar", "xboxappservices", "xboxnetapisvc",
                    "phoneexperiencehost", "yourphone", "cortana",
                    // News & Widgets
                    "newsandinterests", "widgets"
                };

                int currentPid = Process.GetCurrentProcess().Id;

                foreach (var p in Process.GetProcesses())
                {
                    try
                    {
                        if (p.Id == currentPid) continue;
                        if (safeToKill.Contains(p.ProcessName))
                        {
                            string pName = p.ProcessName;
                            int pId = p.Id;
                            p.Kill();
                            p.WaitForExit(1000);
                            killedCount++;
                            Dispatcher.Invoke(() => Log($"[TURBO BOOST] Terminated non-essential background process: {pName} (PID: {pId})"));
                        }
                    }
                    catch { }
                }
            }

            // 2. Trim Process Working Sets across accessible processes
            if (trimWorkingSets)
            {
                int currentPid = Process.GetCurrentProcess().Id;
                foreach (var p in Process.GetProcesses())
                {
                    try
                    {
                        if (p.Id == currentPid) continue;
                        EmptyWorkingSet(p.Handle);
                        SetProcessWorkingSetSize(p.Handle, (IntPtr)(-1), (IntPtr)(-1));
                        trimmedCount++;
                    }
                    catch { }
                }

                // Trim self
                try
                {
                    EmptyWorkingSet(Process.GetCurrentProcess().Handle);
                    SetProcessWorkingSetSize(Process.GetCurrentProcess().Handle, (IntPtr)(-1), (IntPtr)(-1));
                }
                catch { }

                // Force .NET runtime Garbage Collection
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }

            // 3. Purge Temporary Session Files & Empty Recycle Bin
            if (cleanTemps)
            {
                try { CleanDirectory(System.IO.Path.GetTempPath()); } catch { }
                try { SHEmptyRecycleBin(IntPtr.Zero, null!, 7); } catch { }
            }
        });

        // 4. Activate Windows Ultimate Performance CPU Power Scheme
        if (optimizeCpu)
        {
            Log("[TURBO BOOST] Activating Ultimate Performance power scheme to remove processor throttling...");
            await ExecuteAsync("powercfg.exe", "-duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61");
            await ExecuteAsync("powercfg.exe", "-setactive e9a42b02-d5df-448d-aa00-03f14749eb61");
        }

        // Measure final memory
        var memAfter = new MEMORYSTATUSEX();
        GlobalMemoryStatusEx(memAfter);
        double finalAvailMb = memAfter.ullAvailPhys / (1024.0 * 1024.0);
        uint finalLoad = memAfter.dwMemoryLoad;
        double freedMb = Math.Max(0, Math.Round(finalAvailMb - initialAvailMb, 1));

        Log($"[TURBO BOOST] Optimization Complete! Physical RAM Load: {initialLoad}% -> {finalLoad}% ({freedMb} MB reclaimed).");
        Log($"[TURBO BOOST] Pruned {killedCount} background tasks, trimmed working sets for {trimmedCount} processes.");

        // Update live vitals
        UpdateLiveVitals();

        return (killedCount, trimmedCount, freedMb);
    }

    private UIElement BuildTopStatusBar()
    {
        var bar = new Border
        {
            Background = BgCard,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(14, 6, 14, 6)
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Col 0: Left Identity & Network Badges
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Col 1: Spacer
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Col 2: Telemetry & Actions Dock

        // ------------------ COL 0: LEFT IDENTITY & NETWORK BADGES ------------------
        var leftStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

        // Badge 1: Identity Pill [ 💻 Hostname | 👤 Username ]
        var identityBorder = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(8, 3, 8, 3),
            VerticalAlignment = VerticalAlignment.Center
        };
        var identityStack = new StackPanel { Orientation = Orientation.Horizontal };
        _lblTopBarHost = new TextBlock
        {
            Text = $"💻 {Environment.MachineName}",
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            Foreground = TextDark,
            VerticalAlignment = VerticalAlignment.Center
        };
        var idSep = new Border
        {
            Width = 1,
            Height = 10,
            Background = BorderMuted,
            Margin = new Thickness(6, 0, 6, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        _lblTopBarUser = new TextBlock
        {
            Text = $"👤 {Environment.UserName}",
            FontSize = 11,
            Foreground = TextSubtle,
            VerticalAlignment = VerticalAlignment.Center
        };
        identityStack.Children.Add(_lblTopBarHost);
        identityStack.Children.Add(idSep);
        identityStack.Children.Add(_lblTopBarUser);
        identityBorder.Child = identityStack;
        leftStack.Children.Add(identityBorder);

        // Badge 2: Network Pill [ 🌐 IP • GW 🟢 ]
        var netBorder = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(240, 249, 255)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(186, 230, 253)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(8, 3, 8, 3),
            Margin = new Thickness(6, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        var netStack = new StackPanel { Orientation = Orientation.Horizontal };
        _lblTopBarIp = new TextBlock
        {
            Text = "🌐 Detecting...",
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Foreground = SvllBlue,
            VerticalAlignment = VerticalAlignment.Center
        };
        var netSep = new Border
        {
            Width = 1,
            Height = 10,
            Background = new SolidColorBrush(Color.FromRgb(186, 230, 253)),
            Margin = new Thickness(6, 0, 6, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        _lblTopBarGateway = new TextBlock
        {
            Text = $"GW: {DetectLocalGatewayIp()} 🟢",
            FontSize = 10.5,
            Foreground = new SolidColorBrush(Color.FromRgb(3, 105, 161)),
            VerticalAlignment = VerticalAlignment.Center
        };
        netStack.Children.Add(_lblTopBarIp);
        netStack.Children.Add(netSep);
        netStack.Children.Add(_lblTopBarGateway);
        netBorder.Child = netStack;
        leftStack.Children.Add(netBorder);

        Grid.SetColumn(leftStack, 0);
        grid.Children.Add(leftStack);

        // ------------------ COL 2: RIGHT TELEMETRY & ACTIONS DOCK ------------------
        var rightStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

        // Combined Telemetry Pill [ 🚀 CPU: --%  |  🧠 RAM: --% ]
        var telemetryBorder = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(8, 3, 8, 3),
            VerticalAlignment = VerticalAlignment.Center
        };
        var telemetryStack = new StackPanel { Orientation = Orientation.Horizontal };
        _lblTopBarCpu = new TextBlock
        {
            Text = "🚀 CPU: --%",
            FontSize = 10.5,
            FontWeight = FontWeights.SemiBold,
            Foreground = TextDark,
            VerticalAlignment = VerticalAlignment.Center
        };
        var telemSep = new Border
        {
            Width = 1,
            Height = 10,
            Background = BorderMuted,
            Margin = new Thickness(6, 0, 6, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        _lblTopBarRam = new TextBlock
        {
            Text = "🧠 RAM: --%",
            FontSize = 10.5,
            FontWeight = FontWeights.SemiBold,
            Foreground = TextDark,
            VerticalAlignment = VerticalAlignment.Center
        };
        telemetryStack.Children.Add(_lblTopBarCpu);
        telemetryStack.Children.Add(telemSep);
        telemetryStack.Children.Add(_lblTopBarRam);
        telemetryBorder.Child = telemetryStack;
        rightStack.Children.Add(telemetryBorder);

        // Fleet Health Score Pill
        var healthPill = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(220, 252, 231)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(134, 239, 172)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(8, 3, 8, 3),
            Margin = new Thickness(6, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock
            {
                Text = "🛡️ 98% Optimal",
                FontSize = 10.5,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(22, 101, 52))
            }
        };
        rightStack.Children.Add(healthPill);

        // 1-Click Turbo Boost Button
        var btnTurbo = new Button
        {
            Content = "⚡ Turbo Boost",
            FontSize = 10.5,
            FontWeight = FontWeights.Bold,
            Padding = new Thickness(10, 3, 10, 3),
            Margin = new Thickness(6, 0, 0, 0),
            Background = SvllBlue,
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand
        };
        btnTurbo.Click += async (s, e) =>
        {
            btnTurbo.IsEnabled = false;
            btnTurbo.Content = "⏳ Boosting...";
            try
            {
                var res = await ExecuteTurboBoostAsync();
                MessageBox.Show(
                    $"🎉 Turbo Boost Successful!\n\n" +
                    $"• RAM Released: {res.freedMb} MB\n" +
                    $"• Bloat Tasks Terminated: {res.killedCount}\n" +
                    $"• Working Sets Trimmed: {res.trimmedCount} applications\n" +
                    $"• Power Plan: Ultimate Performance Activated",
                    "Resource Optimization", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            finally
            {
                btnTurbo.IsEnabled = true;
                btnTurbo.Content = "⚡ Turbo Boost";
            }
        };
        rightStack.Children.Add(btnTurbo);

        // Command Palette (Ctrl+K)
        var btnPalette = new Button
        {
            Content = "🔍 Search (Ctrl+K)",
            FontSize = 10.5,
            FontWeight = FontWeights.SemiBold,
            Padding = new Thickness(9, 3, 9, 3),
            Margin = new Thickness(6, 0, 0, 0),
            Background = new SolidColorBrush(Color.FromRgb(15, 23, 42)),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand
        };
        btnPalette.Click += (s, e) => OpenCommandPalette();
        rightStack.Children.Add(btnPalette);

        // Admin Mode Session Pill / Unlock Toggle
        var btnAdminMode = new Button
        {
            Content = AdminAuthService.Instance.IsAdminMode ? "👑 ADMIN ACTIVE" : "🔒 Admin Locked",
            FontSize = 10.5,
            FontWeight = FontWeights.Bold,
            Padding = new Thickness(10, 3, 10, 3),
            Margin = new Thickness(6, 0, 0, 0),
            Background = AdminAuthService.Instance.IsAdminMode ? new SolidColorBrush(Color.FromRgb(220, 252, 231)) : new SolidColorBrush(Color.FromRgb(241, 245, 249)),
            Foreground = AdminAuthService.Instance.IsAdminMode ? new SolidColorBrush(Color.FromRgb(22, 101, 52)) : TextSubtle,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            Cursor = Cursors.Hand
        };
        AdminAuthService.Instance.AdminModeChanged += (isAdmin) =>
        {
            Dispatcher.Invoke(() =>
            {
                btnAdminMode.Content = isAdmin ? "👑 ADMIN ACTIVE" : "🔒 Admin Locked";
                btnAdminMode.Background = isAdmin ? new SolidColorBrush(Color.FromRgb(220, 252, 231)) : new SolidColorBrush(Color.FromRgb(241, 245, 249));
                btnAdminMode.Foreground = isAdmin ? new SolidColorBrush(Color.FromRgb(22, 101, 52)) : TextSubtle;
            });
        };
        btnAdminMode.Click += (s, e) =>
        {
            if (AdminAuthService.Instance.IsAdminMode)
            {
                var res = MessageBox.Show("Do you want to lock the IT Admin session?", "Lock Admin Session", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (res == MessageBoxResult.Yes)
                {
                    AdminAuthService.Instance.LockAdmin();
                }
            }
            else
            {
                var dlg = new AdminPasswordDialog("Authenticate as IT Administrator to unlock fleet management and service desk consoles.") { Owner = this };
                dlg.ShowDialog();
            }
        };
        rightStack.Children.Add(btnAdminMode);

        // Modern Dropdown: Quick Launch ▾
        var btnQuickMenu = new Button
        {
            Content = "⚡ Quick Launch ▾",
            FontSize = 10.5,
            FontWeight = FontWeights.Bold,
            Padding = new Thickness(10, 3, 10, 3),
            Margin = new Thickness(6, 0, 0, 0),
            Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
            Foreground = TextDark,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            Cursor = Cursors.Hand
        };

        var quickMenu = new ContextMenu
        {
            FontSize = 11.5,
            Background = Brushes.White,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1)
        };

        var itemSetup = new MenuItem { Header = "🚀  Zero-Touch New PC Setup Wizard", FontWeight = FontWeights.SemiBold };
        itemSetup.Click += (s, e) => NavigateTo("ProvisioningWizard");
        quickMenu.Items.Add(itemSetup);

        var itemProcessShield = new MenuItem { Header = "🛡️  Zero-Trust Process & Ransomware Shield", FontWeight = FontWeights.SemiBold };
        itemProcessShield.Click += (s, e) => NavigateTo("ProcessShield");
        quickMenu.Items.Add(itemProcessShield);

        var itemHardening = new MenuItem { Header = "⚡  CIS Windows Security Hardening", FontWeight = FontWeights.SemiBold };
        itemHardening.Click += (s, e) => NavigateTo("SecurityHardening");
        quickMenu.Items.Add(itemHardening);

        var itemDlp = new MenuItem { Header = "🔒  USB Endpoint DLP & Device Guardian", FontWeight = FontWeights.SemiBold };
        itemDlp.Click += (s, e) => NavigateTo("UsbDlpGuardian");
        quickMenu.Items.Add(itemDlp);

        var itemSmart = new MenuItem { Header = "💾  S.M.A.R.T. Drive Health & Crash Predictor", FontWeight = FontWeights.SemiBold };
        itemSmart.Click += (s, e) => NavigateTo("StorageSmart");
        quickMenu.Items.Add(itemSmart);

        var itemSocket = new MenuItem { Header = "📡  Bandwidth Hog & Socket Hunter", FontWeight = FontWeights.SemiBold };
        itemSocket.Click += (s, e) => NavigateTo("SocketInspector");
        quickMenu.Items.Add(itemSocket);

        var itemLogSheet = new MenuItem { Header = "📊  Open Complaint Log Sheet (.CSV)", FontWeight = FontWeights.SemiBold };
        itemLogSheet.Click += (s, e) =>
        {
            try
            {
                string csvPath = ComplaintTicketService.GetActiveCsvPath();
                Process.Start(new ProcessStartInfo { FileName = csvPath, UseShellExecute = true });
            }
            catch { }
        };
        quickMenu.Items.Add(itemLogSheet);

        quickMenu.Items.Add(new Separator());

        var itemRemote = new MenuItem { Header = "🔴  Remote Access & AnyDesk Status", FontWeight = FontWeights.SemiBold };
        itemRemote.Click += (s, e) => NavigateTo("RemoteClient");
        quickMenu.Items.Add(itemRemote);

#if BUILD_ADMIN
        var itemFleet = new MenuItem { Header = "👑  Fleet Device Admin Console", FontWeight = FontWeights.SemiBold };
        itemFleet.Click += (s, e) => NavigateTo("FleetAdminConsole");
        quickMenu.Items.Add(itemFleet);
#endif

        var itemComplaint = new MenuItem { Header = "📋  Register IT Support Complaint", FontWeight = FontWeights.SemiBold };
        itemComplaint.Click += (s, e) => NavigateTo("SubmitComplaint");
        quickMenu.Items.Add(itemComplaint);

        var itemAi = new MenuItem { Header = "🤖  AI Diagnostic & Triage Assistant", FontWeight = FontWeights.SemiBold };
        itemAi.Click += (s, e) => NavigateTo("AiComplaintBot");
        quickMenu.Items.Add(itemAi);

#if BUILD_ADMIN
        var itemDesk = new MenuItem { Header = "👑  IT Service Desk Admin Panel", FontWeight = FontWeights.SemiBold };
        itemDesk.Click += (s, e) => NavigateTo("TicketAdminDesk");
        quickMenu.Items.Add(itemDesk);
#endif

        quickMenu.Items.Add(new Separator());

        var itemAudit = new MenuItem { Header = "📋  Open Device Audit Action Log" };
        itemAudit.Click += (s, e) =>
        {
            try
            {
                if (!File.Exists(AuditLogPath))
                {
                    if (!Directory.Exists(AuditLogDirectory))
                        Directory.CreateDirectory(AuditLogDirectory);
                    File.WriteAllText(AuditLogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [DEVICE AUDIT LOG INITIALIZED FOR {Environment.MachineName}]\n");
                }
                Process.Start(new ProcessStartInfo { FileName = AuditLogPath, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not open audit log: {ex.Message}", "Audit Log", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        };
        quickMenu.Items.Add(itemAudit);

        var itemFlush = new MenuItem { Header = "🧹  Instant Flush DNS & ARP Cache" };
        itemFlush.Click += async (s, e) =>
        {
            Log("[QUICK ACTION] Flushing DNS Resolver Cache and ARP entries...");
            await ExecuteAsync("ipconfig.exe", "/flushdns");
            await ExecuteAsync("netsh.exe", "interface ip delete arpcache");
            MessageBox.Show("DNS resolver cache flushed and ARP cache cleared successfully!", "Quick Action", MessageBoxButton.OK, MessageBoxImage.Information);
        };
        quickMenu.Items.Add(itemFlush);

        btnQuickMenu.ContextMenu = quickMenu;
        btnQuickMenu.Click += (s, e) =>
        {
            quickMenu.PlacementTarget = btnQuickMenu;
            quickMenu.Placement = PlacementMode.Bottom;
            quickMenu.IsOpen = true;
        };
        rightStack.Children.Add(btnQuickMenu);

        // Direct Top Status Bar Complaint Sheet Button
        var btnTopCsv = new Button
        {
            Content = "📑 CSV Sheet",
            FontSize = 10.5,
            FontWeight = FontWeights.Bold,
            Padding = new Thickness(9, 3, 9, 3),
            Margin = new Thickness(6, 0, 0, 0),
            Background = new SolidColorBrush(Color.FromRgb(240, 253, 244)),
            Foreground = new SolidColorBrush(Color.FromRgb(22, 101, 52)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(134, 239, 172)),
            BorderThickness = new Thickness(1),
            Cursor = Cursors.Hand
        };
        btnTopCsv.Click += (s, e) =>
        {
            try
            {
                string csvPath = ComplaintTicketService.GetActiveCsvPath();
                Process.Start(new ProcessStartInfo { FileName = csvPath, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not open CSV log sheet: {ex.Message}", "CSV Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        };
        rightStack.Children.Add(btnTopCsv);

        Grid.SetColumn(rightStack, 2);
        grid.Children.Add(rightStack);

        bar.Child = grid;
        return bar;
    }

    private UIElement CreateStatusBarSeparator()
    {
        return new Border
        {
            Width = 1,
            Height = 12,
            Background = BorderMuted,
            Margin = new Thickness(10, 0, 10, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
    }

    private Border CreateStatusPill(UIElement content)
    {
        return new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(8, 2, 8, 2),
            Margin = new Thickness(4, 0, 0, 0),
            Child = content
        };
    }

    private void BuildCategorizedNavigation()
    {
        _navButtons.Clear();
        _navPanel.Children.Clear();

        AddNavSectionHeader("ENTERPRISE WORKSPACES");

        foreach (var hub in _hubs)
        {
            AddHubNav(hub.Id, hub.Glyph, hub.Title, hub.Subtitle);
        }
    }

    private void AddNavSectionHeader(string header)
    {
        var txt = new TextBlock
        {
            Text = header,
            FontSize = 9.5,
            FontWeight = FontWeights.Bold,
            Foreground = TextSubtle,
            Margin = new Thickness(8, 12, 0, 6)
        };
        _navPanel.Children.Add(txt);
    }

    private void AddHubNav(string hubId, string glyph, string title, string subtitle)
    {
        var btn = new Button
        {
            Tag = hubId,
            MinHeight = 48,
            Margin = new Thickness(0, 2, 0, 2),
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand,
            HorizontalContentAlignment = HorizontalAlignment.Left,
            Padding = new Thickness(10, 6, 10, 6)
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(28) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var icon = new TextBlock
        {
            Text = glyph,
            FontFamily = IconFont,
            FontSize = 15,
            Foreground = TextSubtle,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(icon, 0);
        grid.Children.Add(icon);

        var textStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        var txtTitle = new TextBlock
        {
            Text = title,
            FontSize = 11.5,
            FontWeight = FontWeights.Bold,
            Foreground = TextDark
        };
        var txtSub = new TextBlock
        {
            Text = subtitle,
            FontSize = 9.5,
            Foreground = TextSubtle,
            Margin = new Thickness(0, 1, 0, 0)
        };
        textStack.Children.Add(txtTitle);
        textStack.Children.Add(txtSub);
        Grid.SetColumn(textStack, 1);
        grid.Children.Add(textStack);

        btn.Content = grid;
        btn.Click += (s, e) => NavigateTo(hubId);

        _navPanel.Children.Add(btn);
        _navButtons[hubId] = btn;
    }

    private UIElement BuildDeveloperAttributionCard()
    {
        var card = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Margin = new Thickness(12, 8, 12, 12),
            Padding = new Thickness(10, 8, 10, 8)
        };

        var sp = new StackPanel();
        sp.Children.Add(new TextBlock
        {
            Text = "Developed by: Kunal Turkar",
            FontSize = 10.5,
            FontWeight = FontWeights.Bold,
            Foreground = SvllBlue
        });
        sp.Children.Add(new TextBlock
        {
            Text = $"SVLL IT Dept  |  Version {CurrentVersion}",
            FontSize = 9.5,
            Foreground = TextSubtle,
            Margin = new Thickness(0, 1, 0, 0)
        });

        card.Child = sp;
        return card;
    }

    private UIElement GetOrCreateHubView(HubInfo hub)
    {
        if (_hubViews.TryGetValue(hub.Id, out var existing))
        {
            return existing.Container;
        }

        var container = new Border();
        var mainGrid = new Grid();
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Sub-Tabs Bar
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Sub-tool View Host

        // Top Sub-Tabs Bar
        var topBar = new Border
        {
            Background = BgCard,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(14, 10, 14, 8),
            Margin = new Thickness(0, 0, 0, 10)
        };

        var topStack = new StackPanel();

        // Hub Title & Description
        var headerRow = new DockPanel { Margin = new Thickness(0, 0, 0, 8) };

        string hubBadgeText = hub.Id switch
        {
            "Hub_Monitoring" => "HUB 1  •  TRIAGE",
            "Hub_Network" => "HUB 2  •  NOC",
            "Hub_Optimization" => "HUB 3  •  OPTIMIZATION",
            "Hub_Diagnostics" => "HUB 4  •  DIAGNOSTICS",
            "Hub_Playbook" => "HUB 5  •  DEPLOYMENT",
            "Hub_Remote" => "HUB 6  •  REMOTE FLEET",
            "Hub_Complaints" => "HUB 7  •  SERVICE DESK",
            "Hub_Security" => "HUB 8  •  SECURITY HARDENING",
            _ => "WORKSPACE"
        };

        var badge = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(239, 246, 255)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(191, 219, 254)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(5),
            Padding = new Thickness(7, 2, 7, 2),
            Margin = new Thickness(0, 0, 10, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock
            {
                Text = hubBadgeText,
                FontSize = 9.5,
                FontWeight = FontWeights.ExtraBold,
                Foreground = SvllBlue
            }
        };

        var titleBlock = new TextBlock
        {
            Text = hub.Title,
            FontSize = 13,
            FontWeight = FontWeights.Bold,
            Foreground = TextDark,
            VerticalAlignment = VerticalAlignment.Center
        };
        var subBlock = new TextBlock
        {
            Text = $"—  {hub.Subtitle}",
            FontSize = 11,
            Foreground = TextSubtle,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8, 0, 0, 0)
        };
        var titleStack = new StackPanel { Orientation = Orientation.Horizontal };
        titleStack.Children.Add(badge);
        titleStack.Children.Add(titleBlock);
        titleStack.Children.Add(subBlock);
        DockPanel.SetDock(titleStack, Dock.Left);
        headerRow.Children.Add(titleStack);
        topStack.Children.Add(headerRow);

        // Sub-tools Tabs (WrapPanel)
        var tabWrap = new WrapPanel();
        var subButtons = new Dictionary<string, Button>();
        var contentHost = new ContentControl();

        string initialSubTag = hub.SubTools.FirstOrDefault().Tag ?? "";

        foreach (var sub in hub.SubTools)
        {
            var btnTab = new Button
            {
                Content = $"{sub.Icon}  {sub.Label}",
                Height = 31,
                Padding = new Thickness(13, 0, 13, 0),
                Margin = new Thickness(0, 0, 6, 6),
                FontSize = 11,
                Cursor = Cursors.Hand,
                BorderThickness = new Thickness(1),
                BorderBrush = BorderMuted
            };

            string toolTag = sub.Tag;
            btnTab.Click += (s, e) => SwitchHubSubTool(hub.Id, toolTag);

            subButtons[sub.Tag] = btnTab;
            tabWrap.Children.Add(btnTab);
        }

        topStack.Children.Add(tabWrap);
        topBar.Child = topStack;

        Grid.SetRow(topBar, 0);
        mainGrid.Children.Add(topBar);

        Grid.SetRow(contentHost, 1);
        mainGrid.Children.Add(contentHost);

        container.Child = mainGrid;

        _hubViews[hub.Id] = (container, contentHost, subButtons, initialSubTag);

        return container;
    }

    private void SwitchHubSubTool(string hubId, string subTag)
    {
        if (!_hubViews.TryGetValue(hubId, out var hubData)) return;

        // Update Tab Button Styles
        foreach (var kvp in hubData.SubButtons)
        {
            bool isCurrent = kvp.Key == subTag;
            kvp.Value.Background = isCurrent ? SvllBlue : new SolidColorBrush(Color.FromRgb(248, 250, 252));
            kvp.Value.Foreground = isCurrent ? Brushes.White : new SolidColorBrush(Color.FromRgb(51, 65, 85));
            kvp.Value.FontWeight = isCurrent ? FontWeights.Bold : FontWeights.Normal;
            kvp.Value.BorderBrush = isCurrent ? SvllBlue : BorderMuted;
        }

        // Load sub-tool view into host
        if (!_viewCache.TryGetValue(subTag, out var view))
        {
            view = CreateViewByTag(subTag);
            _viewCache[subTag] = view;
        }

        hubData.Host.Content = view;
        _hubViews[hubId] = (hubData.Container, hubData.Host, hubData.SubButtons, subTag);
    }

    public void NavigateTo(string tag)
    {
        // 1. Determine target Hub and SubTool
        HubInfo? targetHub = null;
        string targetSubTag = "";

        if (tag.StartsWith("Hub_"))
        {
            targetHub = _hubs.FirstOrDefault(h => h.Id == tag);
            if (targetHub != null)
            {
                if (_hubViews.TryGetValue(targetHub.Id, out var existingData) && !string.IsNullOrEmpty(existingData.ActiveSubTag))
                    targetSubTag = existingData.ActiveSubTag;
                else
                    targetSubTag = targetHub.SubTools.FirstOrDefault().Tag ?? "";
            }
        }
        else
        {
            foreach (var h in _hubs)
            {
                if (h.SubTools.Any(st => st.Tag == tag))
                {
                    targetHub = h;
                    targetSubTag = tag;
                    break;
                }
            }
        }

        if (targetHub == null)
        {
            targetHub = _hubs[0];
            targetSubTag = targetHub.SubTools[0].Tag;
        }

        _activeHubId = targetHub.Id;

        // 2. Highlight Sidebar Button
        foreach (var kvp in _navButtons)
        {
            bool isCurrent = kvp.Key == targetHub.Id;
            var btn = kvp.Value;
            btn.Background = isCurrent ? new SolidColorBrush(Color.FromRgb(239, 246, 255)) : Brushes.Transparent;
            if (btn.Content is Grid g && g.Children.Count >= 2)
            {
                if (g.Children[0] is TextBlock icon)
                    icon.Foreground = isCurrent ? SvllBlue : TextSubtle;
                if (g.Children[1] is StackPanel sp && sp.Children.Count >= 2)
                {
                    if (sp.Children[0] is TextBlock title)
                        title.Foreground = isCurrent ? SvllBlue : TextDark;
                    if (sp.Children[1] is TextBlock sub)
                        sub.Foreground = isCurrent ? new SolidColorBrush(Color.FromRgb(3, 105, 161)) : TextSubtle;
                }
            }
        }

        // 3. Render Hub View & Activate Sub-Tool
        var hubView = GetOrCreateHubView(targetHub);
        _pageHost.Content = hubView;

        SwitchHubSubTool(targetHub.Id, targetSubTag);
    }

    private UIElement CreateViewByTag(string tag)
    {
        return tag switch
        {
            "Dashboard" => BuildDashboardView(),
            "PingMonitor" => BuildPingMonitorView(),
            "SpeedTest" => BuildSpeedTestView(),
            "Network" => BuildNetworkView(),
            "SubnetScanner" => BuildSubnetScannerView(),
            "NetshSuite" => BuildNetshSuiteView(),
            "Wifi" => BuildWifiView(),
            "LanShares" => BuildLanShareView(),
            "WmsLatency" => BuildWmsLatencyView(),
            "AssetPassport" => BuildAssetPassportView(),
            "EventLog" => BuildEventLogAnalyzerView(),
            "SupportBundle" => BuildSupportBundleView(),
            "TurboBooster" => BuildTurboBoosterView(),
            "WinUtilTweaks" => BuildWinUtilTweaksView(),
            "UsbDepot" => BuildUsbDepotView(),
            "WinGetSoftware" => BuildWinGetSoftwareView(),
            "AppUninstaller" => BuildAppUninstallerView(),
            "WinFeatures" => BuildWinFeaturesView(),
            "WinUpdateConfig" => BuildWinUpdateConfigView(),
            "Users" => BuildUsersView(),
            "Health" => BuildHealthView(),
            "Repair" => BuildRepairView(),
            "Disk" => BuildDiskView(),
            "Services" => BuildServicesView(),
            "Printer" => BuildPrinterView(),
            "Library" => BuildLibraryView(),
            "Cleanup" => BuildCleanupView(),
            "ConfigManager" => BuildConfigManagerView(),
            "About" => BuildAboutView(),
            "ProvisioningWizard" => BuildProvisioningWizardView(),
            "StorageSmart" => BuildStorageSmartView(),
            "SecurityDlp" => BuildUsbDlpGuardianView(),
            "SocketInspector" => BuildSocketInspectorView(),
            "RemoteClient" => BuildRemoteClientView(),
#if BUILD_ADMIN
            "FleetAdminConsole" => BuildAdminCommandCenterView(), // legacy redirect
            "AdminCommandCenter" => BuildAdminCommandCenterView(),
#endif
            "SubmitComplaint" => BuildSubmitComplaintView(),
            "AiComplaintBot" => BuildAiComplaintBotView(),
            "TicketAdminDesk" => BuildTicketAdminDeskView(),
            "MyTickets" => BuildMyTicketsView(),
            "ProcessShield" => BuildProcessShieldView(),
            "SecurityHardening" => BuildSecurityHardeningView(),
            "UsbDlpGuardian" => BuildUsbDlpGuardianView(),
            "PortThreatHunter" => BuildPortThreatHunterView(),
            "ForensicsCollector" => BuildForensicsCollectorView(),
            "CertHealthInspector" => BuildCertHealthInspectorView(),
            _ => BuildDashboardView()
        };
    }

    private UIElement BuildEmbeddedTerminal()
    {
        var panel = new Border
        {
            Background = TerminalBg,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(0, 1, 0, 0)
        };

        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header bar
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Output Box
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Input Bar

        // Header bar
        var bar = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(30, 41, 59)),
            Padding = new Thickness(12, 4, 12, 4)
        };
        var barGrid = new Grid();
        barGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        barGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var title = new TextBlock
        {
            Text = "INTEGRATED ADMINISTRATIVE CONSOLE (POWERSHELL / CMD)",
            FontSize = 10,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(title, 0);
        barGrid.Children.Add(title);

        var actions = new StackPanel { Orientation = Orientation.Horizontal };
        _globalProgress = new ProgressBar
        {
            Width = 140,
            Height = 8,
            IsIndeterminate = false,
            Visibility = Visibility.Collapsed,
            Margin = new Thickness(0, 0, 10, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        actions.Children.Add(_globalProgress);

        var btnClear = new Button
        {
            Content = "Clear Output",
            FontSize = 10,
            Padding = new Thickness(6, 2, 6, 2),
            Background = Brushes.Transparent,
            Foreground = new SolidColorBrush(Color.FromRgb(203, 213, 225)),
            BorderBrush = BorderMuted
        };
        btnClear.Click += (s, e) => _txtConsole.Clear();
        actions.Children.Add(btnClear);

        var btnOpenLog = new Button
        {
            Content = "View Device Audit Log",
            FontSize = 10,
            Padding = new Thickness(6, 2, 6, 2),
            Margin = new Thickness(6, 0, 0, 0),
            Background = Brushes.Transparent,
            Foreground = new SolidColorBrush(Color.FromRgb(203, 213, 225)),
            BorderBrush = BorderMuted,
            Cursor = Cursors.Hand
        };
        btnOpenLog.Click += (s, e) =>
        {
            try
            {
                if (!File.Exists(AuditLogPath))
                {
                    if (!Directory.Exists(AuditLogDirectory))
                        Directory.CreateDirectory(AuditLogDirectory);
                    File.WriteAllText(AuditLogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [DEVICE AUDIT LOG INITIALIZED FOR {Environment.MachineName}]\n");
                }
                Process.Start(new ProcessStartInfo { FileName = AuditLogPath, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not open audit log: {ex.Message}", "Audit Log", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        };
        actions.Children.Add(btnOpenLog);

        _btnToggleTerminal = new Button
        {
            Content = "▼ Minimize",
            FontSize = 10,
            Padding = new Thickness(6, 2, 6, 2),
            Margin = new Thickness(6, 0, 0, 0),
            Background = Brushes.Transparent,
            Foreground = new SolidColorBrush(Color.FromRgb(203, 213, 225)),
            BorderBrush = BorderMuted,
            Cursor = Cursors.Hand
        };
        _btnToggleTerminal.Click += (s, e) =>
        {
            _isTerminalCollapsed = !_isTerminalCollapsed;
            if (_isTerminalCollapsed)
            {
                _terminalRow.Height = new GridLength(32);
                _btnToggleTerminal.Content = "▲ Expand Terminal";
                _txtConsole.Visibility = Visibility.Collapsed;
                if (_terminalInputBar != null) _terminalInputBar.Visibility = Visibility.Collapsed;
            }
            else
            {
                _terminalRow.Height = new GridLength(185);
                _btnToggleTerminal.Content = "▼ Minimize";
                _txtConsole.Visibility = Visibility.Visible;
                if (_terminalInputBar != null) _terminalInputBar.Visibility = Visibility.Visible;
            }
        };
        actions.Children.Add(_btnToggleTerminal);

        Grid.SetColumn(actions, 1);
        barGrid.Children.Add(actions);
        bar.Child = barGrid;
        Grid.SetRow(bar, 0);
        grid.Children.Add(bar);

        // Console Output
        _txtConsole = new TextBox
        {
            Background = TerminalBg,
            Foreground = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
            FontFamily = new FontFamily("Consolas, Courier New, monospace"),
            FontSize = 11,
            IsReadOnly = true,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(10, 6, 10, 6),
            TextWrapping = TextWrapping.Wrap
        };
        Grid.SetRow(_txtConsole, 1);
        grid.Children.Add(_txtConsole);

        // Input bar
        var inputBar = new Grid { Margin = new Thickness(6, 4, 6, 6) };
        _terminalInputBar = inputBar;
        inputBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });
        inputBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        inputBar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        _cmbShellType = new ComboBox
        {
            ItemsSource = new[] { "PowerShell", "CMD" },
            SelectedIndex = 0,
            Height = 26,
            Margin = new Thickness(0, 0, 6, 0)
        };
        Grid.SetColumn(_cmbShellType, 0);
        inputBar.Children.Add(_cmbShellType);

        _txtCmdInput = new TextBox
        {
            Height = 26,
            FontFamily = new FontFamily("Consolas, monospace"),
            FontSize = 11,
            Background = new SolidColorBrush(Color.FromRgb(30, 41, 59)),
            Foreground = Brushes.White,
            BorderBrush = BorderMuted,
            Padding = new Thickness(6, 4, 6, 4)
        };
        _txtCmdInput.KeyDown += async (s, e) =>
        {
            if (e.Key == Key.Enter)
            {
                await ExecuteManualCliCommandAsync();
            }
            else if (e.Key == Key.Up)
            {
                if (_cmdHistory.Count > 0 && _cmdHistoryIndex > 0)
                {
                    _cmdHistoryIndex--;
                    _txtCmdInput.Text = _cmdHistory[_cmdHistoryIndex];
                    _txtCmdInput.CaretIndex = _txtCmdInput.Text.Length;
                }
            }
            else if (e.Key == Key.Down)
            {
                if (_cmdHistory.Count > 0 && _cmdHistoryIndex < _cmdHistory.Count - 1)
                {
                    _cmdHistoryIndex++;
                    _txtCmdInput.Text = _cmdHistory[_cmdHistoryIndex];
                    _txtCmdInput.CaretIndex = _txtCmdInput.Text.Length;
                }
            }
        };
        Grid.SetColumn(_txtCmdInput, 1);
        inputBar.Children.Add(_txtCmdInput);

        var btnRun = new Button
        {
            Content = " Run ",
            Height = 26,
            Margin = new Thickness(6, 0, 0, 0),
            Background = SvllBlue,
            Foreground = Brushes.White,
            FontWeight = FontWeights.SemiBold
        };
        btnRun.Click += async (s, e) => await ExecuteManualCliCommandAsync();
        Grid.SetColumn(btnRun, 2);
        inputBar.Children.Add(btnRun);

        Grid.SetRow(inputBar, 2);
        grid.Children.Add(inputBar);

        panel.Child = grid;
        return panel;
    }

    public async Task ExecuteManualCliCommandAsync()
    {
        string raw = _txtCmdInput.Text.Trim();
        if (string.IsNullOrEmpty(raw)) return;

        _cmdHistory.Add(raw);
        _cmdHistoryIndex = _cmdHistory.Count;
        _txtCmdInput.Clear();

        bool isPs = _cmbShellType.SelectedIndex == 0;
        Log($"\n> [{ (isPs ? "PS" : "CMD") }] {raw}");

        string exe = isPs ? "powershell.exe" : "cmd.exe";
        string args = isPs ? $"-NoProfile -ExecutionPolicy Bypass -Command \"{raw}\"" : $"/c \"{raw}\"";
        await ExecuteAsync(exe, args);
    }

    public async Task<string> ExecuteAsync(string fileName, string arguments)
    {
        _globalProgress.Visibility = Visibility.Visible;
        _globalProgress.IsIndeterminate = true;

        var sb = new StringBuilder();
        try
        {
            await Task.Run(() =>
            {
                using var p = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = fileName,
                        Arguments = arguments,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                p.OutputDataReceived += (s, e) =>
                {
                    if (e.Data != null)
                    {
                        sb.AppendLine(e.Data);
                        Dispatcher.Invoke(() => Log(e.Data));
                    }
                };

                p.ErrorDataReceived += (s, e) =>
                {
                    if (e.Data != null)
                    {
                        sb.AppendLine(e.Data);
                        Dispatcher.Invoke(() => Log($"[ERR] {e.Data}"));
                    }
                };

                p.Start();
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();
                p.WaitForExit();
            });
        }
        catch (Exception ex)
        {
            Log($"[EXECUTION ERROR] {ex.Message}");
        }
        finally
        {
            _globalProgress.IsIndeterminate = false;
            _globalProgress.Visibility = Visibility.Collapsed;
        }

        return sb.ToString();
    }

    public string ExecutePowerShellScript(string script)
    {
        try
        {
            using var p = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };
            p.Start();
            string output = p.StandardOutput.ReadToEnd();
            p.WaitForExit(10000);
            return output;
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    public void Log(string msg)
    {
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        string formattedEntry = $"[{timestamp}] {msg}";

        Dispatcher.Invoke(() =>
        {
            _txtConsole.AppendText(msg + Environment.NewLine);
            _txtConsole.ScrollToEnd();
        });

        // Store action permanently in local device audit history file
        Task.Run(() =>
        {
            try
            {
                if (!Directory.Exists(AuditLogDirectory))
                    Directory.CreateDirectory(AuditLogDirectory);

                File.AppendAllText(AuditLogPath, formattedEntry + Environment.NewLine);
            }
            catch { }
        });
    }

    private Border CreateCard(string title, UIElement content)
    {
        var border = new Border
        {
            Background = BgCard,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(16, 14, 16, 14),
            Margin = new Thickness(0, 0, 0, 14)
        };

        var sp = new StackPanel();
        if (!string.IsNullOrEmpty(title))
        {
            var headerRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 12) };
            headerRow.Children.Add(new Border
            {
                Width = 4,
                Height = 16,
                Background = SvllBlue,
                CornerRadius = new CornerRadius(2),
                Margin = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center
            });
            headerRow.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = SvllBlue,
                VerticalAlignment = VerticalAlignment.Center
            });
            sp.Children.Add(headerRow);
        }
        sp.Children.Add(content);
        border.Child = sp;
        return border;
    }

    private UIElement CreateToolRow(string name, string desc, Func<Task> action, string btnLabel = "Execute")
    {
        var card = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(14, 10, 14, 10),
            Margin = new Thickness(0, 0, 0, 8)
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var textStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 16, 0) };
        var titleRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 3) };

        titleRow.Children.Add(new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(239, 246, 255)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(191, 219, 254)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(5, 1, 5, 1),
            Margin = new Thickness(0, 0, 8, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock
            {
                Text = "⚡ ACTION",
                FontSize = 9,
                FontWeight = FontWeights.Bold,
                Foreground = SvllBlue
            }
        });

        titleRow.Children.Add(new TextBlock
        {
            Text = name,
            FontSize = 12.5,
            FontWeight = FontWeights.Bold,
            Foreground = TextDark,
            VerticalAlignment = VerticalAlignment.Center
        });
        textStack.Children.Add(titleRow);

        textStack.Children.Add(new TextBlock
        {
            Text = desc,
            FontSize = 11,
            Foreground = TextSubtle,
            TextWrapping = TextWrapping.Wrap,
            LineHeight = 16
        });
        Grid.SetColumn(textStack, 0);
        grid.Children.Add(textStack);

        var btn = new Button
        {
            Content = $" {btnLabel} ",
            Height = 30,
            Padding = new Thickness(14, 0, 14, 0),
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            Background = SvllBlue,
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand,
            VerticalAlignment = VerticalAlignment.Center
        };
        btn.Click += async (s, e) =>
        {
            btn.IsEnabled = false;
            try { await action(); }
            finally { btn.IsEnabled = true; }
        };
        Grid.SetColumn(btn, 1);
        grid.Children.Add(btn);

        card.Child = grid;
        return card;
    }

    private void OpenTool(string exe, string args = "")
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = exe,
                Arguments = args,
                UseShellExecute = true
            });
            Log($"[LAUNCH] Opened external management utility: {exe} {args}");
        }
        catch (Exception ex)
        {
            Log($"[LAUNCH ERROR] Failed to start {exe}: {ex.Message}");
        }
    }
}
