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

    public const string CurrentVersion = "5.5";

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
        Title = $"SVLL IT Support Workstation v{CurrentVersion} - Enterprise Fleet Diagnostics";
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
            Log($"[DEVICE AUDIT] Session started on host {Environment.MachineName} (User: {Environment.UserName}). Initial state verified.");
            await HandleCommandLineArgsAsync(args);
        };

        Closing += (s, e) =>
        {
            try
            {
                // Stop active background timers
                _watchdogTimer?.Stop();
                _isWatchdogRunning = false;
                _vitalsTimer?.Stop();

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
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Dynamic Page Host
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(180) }); // Embedded Interactive Terminal

        _pageHost = new ContentControl { Margin = new Thickness(18, 16, 18, 8) };
        Grid.SetRow(_pageHost, 0);
        mainGrid.Children.Add(_pageHost);

        var terminalPanel = BuildEmbeddedTerminal();
        Grid.SetRow(terminalPanel, 1);
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

    private void BuildCategorizedNavigation()
    {
        _navButtons.Clear();
        _navPanel.Children.Clear();

        // Tier 1: OVERVIEW & MONITORING
        AddNavSectionHeader("OVERVIEW & MONITORING");
        AddNav("\uE80F", "Executive Dashboard", "Dashboard");
        AddNav("\uE839", "24/7 Ping Watchdog", "PingMonitor");
        AddNav("\uEC4A", "Live Internet Speed Test", "SpeedTest");

        // Tier 2: NETWORK OPERATIONS
        AddNavSectionHeader("NETWORK OPERATIONS");
        AddNav("\uE968", "Network & DNS Profiles", "Network");
        AddNav("\uE753", "Subnet IP Scanner", "SubnetScanner");
        AddNav("\uE774", "IPConfig & Netsh Suite", "NetshSuite");
        AddNav("\uE701", "Wi-Fi Keys & Diagnostics", "Wifi");
        AddNav("\uE8B7", "LAN File Share & Push", "LanShares");

        // Tier 3: LOGISTICS & WAREHOUSE
        AddNavSectionHeader("LOGISTICS & WAREHOUSE");
        AddNav("\uE749", "Thermal Label Printers", "ThermalPrinters");
        AddNav("\uEC5A", "Barcode Live Test Bench", "BarcodeScanner");
        AddNav("\uE839", "WMS & ERP Latency Tester", "WmsLatency");

        // Tier 4: ENTERPRISE ASSET & HELPDESK
        AddNavSectionHeader("ENTERPRISE ASSET & HELPDESK");
        AddNav("\uE8A5", "Asset Passport & QR Code", "AssetPassport");
        AddNav("\uE7BA", "Event Log & Crash Analyzer", "EventLog");
        AddNav("\uE8CB", "Diagnostic Support Bundle", "SupportBundle");

        // Tier 5: SYSTEM ADMINISTRATION
        AddNavSectionHeader("SYSTEM ADMINISTRATION");
        AddNav("\uE74C", "WinUtil Tweaks & Debloat", "WinUtilTweaks");
        AddNav("\uE896", "WinGet Software Deployer", "WinGetSoftware");
        AddNav("\uE7B5", "Windows Features (DISM)", "WinFeatures");
        AddNav("\uE777", "Windows Update Strategy", "WinUpdateConfig");
        AddNav("\uE77B", "Local Users & Vault Admin", "Users");

        // Tier 6: DIAGNOSTICS & HEALTH
        AddNavSectionHeader("DIAGNOSTICS & HEALTH");
        AddNav("\uE95E", "PC Health & Battery Report", "Health");
        AddNav("\uE90F", "Windows OS Repair (SFC)", "Repair");
        AddNav("\uEDA2", "Storage Volumes & TRIM", "Disk");
        AddNav("\uE718", "Services & Process Control", "Services");
        AddNav("\uE749", "Print Spooler Diagnostic", "Printer");

        // Tier 7: KNOWLEDGE & SYSTEM
        AddNavSectionHeader("KNOWLEDGE & SYSTEM");
        AddNav("\uE82D", "IT Fix & Command Library", "Library");
        AddNav("\uE74D", "Disk Cleanup & Temp Files", "Cleanup");
        AddNav("\uE8CB", "Config & Presets Manager", "ConfigManager");
        AddNav("\uE946", "Release Updates & About", "About");
    }

    private void AddNavSectionHeader(string header)
    {
        var txt = new TextBlock
        {
            Text = header,
            FontSize = 9.5,
            FontWeight = FontWeights.Bold,
            Foreground = TextSubtle,
            Margin = new Thickness(8, 12, 0, 4)
        };
        _navPanel.Children.Add(txt);
    }

    private void AddNav(string glyph, string label, string tag)
    {
        var btn = new Button
        {
            Tag = tag,
            Height = 33,
            Margin = new Thickness(0, 1.5, 0, 1.5),
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand,
            HorizontalContentAlignment = HorizontalAlignment.Left,
            Padding = new Thickness(8, 0, 8, 0)
        };

        var panel = new StackPanel { Orientation = Orientation.Horizontal };
        var icon = new TextBlock
        {
            Text = glyph,
            FontFamily = IconFont,
            FontSize = 13,
            Foreground = TextSubtle,
            Width = 22,
            VerticalAlignment = VerticalAlignment.Center
        };
        var text = new TextBlock
        {
            Text = label,
            FontSize = 11.5,
            FontWeight = FontWeights.Medium,
            Foreground = TextDark,
            VerticalAlignment = VerticalAlignment.Center
        };

        panel.Children.Add(icon);
        panel.Children.Add(text);
        btn.Content = panel;

        btn.Click += (s, e) => NavigateTo(tag);
        _navPanel.Children.Add(btn);
        _navButtons[tag] = btn;
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

    public void NavigateTo(string tag)
    {
        foreach (var kvp in _navButtons)
        {
            bool isCurrent = kvp.Key == tag;
            var btn = kvp.Value;
            btn.Background = isCurrent ? new SolidColorBrush(Color.FromRgb(239, 246, 255)) : Brushes.Transparent;
            if (btn.Content is StackPanel sp && sp.Children.Count >= 2)
            {
                if (sp.Children[0] is TextBlock icon)
                    icon.Foreground = isCurrent ? SvllBlue : TextSubtle;
                if (sp.Children[1] is TextBlock txt)
                {
                    txt.Foreground = isCurrent ? SvllBlue : TextDark;
                    txt.FontWeight = isCurrent ? FontWeights.SemiBold : FontWeights.Medium;
                }
            }
        }

        if (!_viewCache.TryGetValue(tag, out var view))
        {
            view = CreateViewByTag(tag);
            _viewCache[tag] = view;
        }

        _pageHost.Content = view;
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
            "ThermalPrinters" => BuildThermalPrintersView(),
            "BarcodeScanner" => BuildBarcodeScannerView(),
            "WmsLatency" => BuildWmsLatencyView(),
            "AssetPassport" => BuildAssetPassportView(),
            "EventLog" => BuildEventLogAnalyzerView(),
            "SupportBundle" => BuildSupportBundleView(),
            "WinUtilTweaks" => BuildWinUtilTweaksView(),
            "WinGetSoftware" => BuildWinGetSoftwareView(),
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
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16),
            Margin = new Thickness(0, 0, 0, 14)
        };

        var sp = new StackPanel();
        if (!string.IsNullOrEmpty(title))
        {
            sp.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 13.5,
                FontWeight = FontWeights.Bold,
                Foreground = SvllBlue,
                Margin = new Thickness(0, 0, 0, 12)
            });
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
            CornerRadius = new CornerRadius(7),
            Padding = new Thickness(14, 11, 14, 11),
            Margin = new Thickness(0, 0, 0, 8)
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var textStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 16, 0) };
        var titleRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 3) };

        titleRow.Children.Add(new TextBlock
        {
            Text = "⚡",
            FontSize = 11,
            Foreground = SvllBlue,
            Margin = new Thickness(0, 0, 6, 0),
            VerticalAlignment = VerticalAlignment.Center
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
            Height = 32,
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
