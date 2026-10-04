using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using QRCoder;

namespace SVLL_IT_Workstation;

public partial class MainWindow
{
    // Asset Passport State
    private System.Windows.Controls.Image _imgAssetQrCode = null!;
    private TextBlock _lblAssetPassportText = null!;

    // Event Log Analyzer State
    private ListView _listEventLogs = null!;
    private readonly List<EventLogEntryItem> _allEventLogs = new List<EventLogEntryItem>();
    private ProgressBar _progEventScan = null!;

    #region 1. Asset Passport & Scannable QR Code

    private UIElement BuildAssetPassportView()
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
            Text = "ENTERPRISE HARDWARE ASSET PASSPORT & SCANNABLE QR CODE",
            FontSize = 13,
            FontWeight = FontWeights.Bold,
            Foreground = SvllBlue,
            Margin = new Thickness(0, 0, 0, 6)
        });
        sp.Children.Add(new TextBlock
        {
            Text = "Generate a scannable on-screen QR Code containing complete machine asset parameters (Service Tag, MAC Address, Hostname, CPU, RAM) for instant smartphone inventory capture.",
            FontSize = 11,
            Foreground = TextSubtle,
            Margin = new Thickness(0, 0, 0, 14)
        });

        var gridAsset = new Grid();
        gridAsset.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(220) }); // QR Code Display
        gridAsset.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Data & Actions

        // QR Code Card
        var qrBorder = new Border
        {
            Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(248, 250, 252)),
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(14),
            Margin = new Thickness(0, 0, 16, 0),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        var qrStack = new StackPanel();
        _imgAssetQrCode = new System.Windows.Controls.Image { Width = 180, Height = 180, Margin = new Thickness(0, 0, 0, 8) };
        qrStack.Children.Add(_imgAssetQrCode);
        qrStack.Children.Add(new TextBlock
        {
            Text = "SCAN WITH IT MOBILE APP",
            FontSize = 9.5,
            FontWeight = FontWeights.Bold,
            Foreground = TextSubtle,
            HorizontalAlignment = HorizontalAlignment.Center
        });
        qrBorder.Child = qrStack;
        Grid.SetColumn(qrBorder, 0);
        gridAsset.Children.Add(qrBorder);

        // Passport Data & Actions
        var infoStack = new StackPanel();
        _lblAssetPassportText = new TextBlock
        {
            Text = "Compiling hardware asset passport...",
            FontSize = 11.5,
            FontFamily = new System.Windows.Media.FontFamily("Consolas, monospace"),
            Foreground = TextDark,
            Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(248, 250, 252)),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 0, 0, 12)
        };
        infoStack.Children.Add(_lblAssetPassportText);

        var btnBar = new StackPanel { Orientation = Orientation.Horizontal };
        var btnExportCsv = new Button
        {
            Content = "Export Asset BOM (.CSV)",
            Height = 32,
            Padding = new Thickness(14, 0, 14, 0),
            Margin = new Thickness(0, 0, 8, 0),
            Background = SvllBlue,
            Foreground = System.Windows.Media.Brushes.White,
            FontWeight = FontWeights.SemiBold
        };
        btnExportCsv.Click += (s, e) => ExportAssetBomCsv();
        btnBar.Children.Add(btnExportCsv);

        var btnRegenerate = new Button
        {
            Content = "Regenerate Passport",
            Height = 32,
            Padding = new Thickness(12, 0, 12, 0),
            Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(241, 245, 249)),
            BorderBrush = BorderMuted
        };
        btnRegenerate.Click += (s, e) => GenerateAssetQrCode();
        btnBar.Children.Add(btnRegenerate);

        infoStack.Children.Add(btnBar);
        Grid.SetColumn(infoStack, 1);
        gridAsset.Children.Add(infoStack);

        sp.Children.Add(gridAsset);
        card.Child = sp;
        root.Children.Add(card);

        scroll.Content = root;
        GenerateAssetQrCode();

        return scroll;
    }

    private void GenerateAssetQrCode()
    {
        try
        {
            string host = Environment.MachineName;
            string serial = _cachedBiosSerial.Replace("Service Tag: ", "").Trim();
            string mac = _lblSpecMac?.Text ?? "Unknown";
            string ip = _lblNetVal?.Text ?? "Unknown";
            string cpu = _cachedCpuName;
            string os = _cachedOsBuild;

            var passportData = new StringBuilder();
            passportData.AppendLine($"=== SVLL HARDWARE ASSET PASSPORT ===");
            passportData.AppendLine($"Hostname:        {host}");
            passportData.AppendLine($"Service Tag/S/N: {serial}");
            passportData.AppendLine($"Physical MAC:    {mac}");
            passportData.AppendLine($"Active IP:       {ip}");
            passportData.AppendLine($"Processor:       {cpu}");
            passportData.AppendLine($"OS Build:        {os}");
            passportData.AppendLine($"Audit Date:      {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

            _lblAssetPassportText.Text = passportData.ToString();

            // Encode compressed JSON for QR code
            string qrPayload = $"SVLL_ASSET|H:{host}|S:{serial}|M:{mac}|IP:{ip}|DATE:{DateTime.Now:yyyyMMdd}";

            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(qrPayload, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);
            byte[] qrBytes = qrCode.GetGraphic(20);

            using var ms = new MemoryStream(qrBytes);
            var bi = new BitmapImage();
            bi.BeginInit();
            bi.StreamSource = ms;
            bi.CacheOption = BitmapCacheOption.OnLoad;
            bi.EndInit();
            _imgAssetQrCode.Source = bi;

            Log("[ASSET] Generated scannable QR Code hardware passport.");
        }
        catch (Exception ex)
        {
            Log($"[ASSET QR ERROR] {ex.Message}");
        }
    }

    private void ExportAssetBomCsv()
    {
        var sfd = new SaveFileDialog
        {
            Filter = "CSV Files (*.csv)|*.csv",
            FileName = $"SVLL_Asset_BOM_{Environment.MachineName}.csv"
        };
        if (sfd.ShowDialog() == true)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Field,Value");
            sb.AppendLine($"Hostname,{Environment.MachineName}");
            sb.AppendLine($"Logged User,{Environment.UserName}");
            sb.AppendLine($"BIOS Serial / Service Tag,{_cachedBiosSerial}");
            sb.AppendLine($"Motherboard Model,{_cachedBoardName}");
            sb.AppendLine($"Processor (CPU),{_cachedCpuName}");
            sb.AppendLine($"Display Adapter (GPU),{_cachedGpuName}");
            sb.AppendLine($"Operating System,{_cachedOsBuild}");
            sb.AppendLine($"Physical MAC,{_lblSpecMac?.Text}");
            sb.AppendLine($"Active IPv4,{_lblNetVal?.Text}");
            sb.AppendLine($"Audit Timestamp,{DateTime.Now}");

            File.WriteAllText(sfd.FileName, sb.ToString());
            Log($"[ASSET] Exported hardware Bill of Materials to {sfd.FileName}");
            MessageBox.Show($"Hardware Asset BOM exported successfully!", "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    #endregion

    #region 2. Windows Event Log & Critical Crash Analyzer

    private UIElement BuildEventLogAnalyzerView()
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
            Text = "WINDOWS EVENT LOG & CRITICAL CRASH ANALYZER",
            FontSize = 13,
            FontWeight = FontWeights.Bold,
            Foreground = SvllBlue,
            Margin = new Thickness(0, 0, 0, 6)
        });
        sp.Children.Add(new TextBlock
        {
            Text = "Extracts and categorizes BSOD Stop BugChecks (Event 41), Application Crashes (Event 1000/1002), and Service Failures (Event 7000/7036) from the last 48 hours.",
            FontSize = 11,
            Foreground = TextSubtle,
            Margin = new Thickness(0, 0, 0, 12)
        });

        // Toolbar
        var toolBar = new DockPanel { Margin = new Thickness(0, 0, 0, 10) };
        var filterGroup = new StackPanel { Orientation = Orientation.Horizontal };

        var btnScanLogs = new Button
        {
            Content = " Scan Event Logs (48h) ",
            Height = 30,
            Background = SvllBlue,
            Foreground = System.Windows.Media.Brushes.White,
            FontWeight = FontWeights.Bold,
            Padding = new Thickness(12, 0, 12, 0),
            Margin = new Thickness(0, 0, 8, 0),
            Cursor = Cursors.Hand
        };
        btnScanLogs.Click += async (s, e) => await ScanEventLogsAsync();
        filterGroup.Children.Add(btnScanLogs);

        filterGroup.Children.Add(CreateFilterButton("All Events", () => FilterEventLogs("ALL")));
        filterGroup.Children.Add(CreateFilterButton("BSOD BugChecks (41)", () => FilterEventLogs("BSOD")));
        filterGroup.Children.Add(CreateFilterButton("App Crashes (1000)", () => FilterEventLogs("APP")));
        filterGroup.Children.Add(CreateFilterButton("Disk Paging (51)", () => FilterEventLogs("DISK")));

        DockPanel.SetDock(filterGroup, Dock.Left);
        toolBar.Children.Add(filterGroup);

        _progEventScan = new ProgressBar
        {
            Width = 140,
            Height = 10,
            IsIndeterminate = false,
            Visibility = Visibility.Collapsed,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        toolBar.Children.Add(_progEventScan);

        sp.Children.Add(toolBar);

        // Event Log Table
        _listEventLogs = new ListView
        {
            Height = 320,
            FontSize = 11.5,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            Margin = new Thickness(0, 0, 0, 12)
        };
        var gv = new GridView();
        gv.Columns.Add(new GridViewColumn { Header = "Time Generated", Width = 140, DisplayMemberBinding = new Binding("TimeFormatted") });
        gv.Columns.Add(new GridViewColumn { Header = "Level", Width = 80, DisplayMemberBinding = new Binding("Level") });
        gv.Columns.Add(new GridViewColumn { Header = "ID", Width = 60, DisplayMemberBinding = new Binding("EventId") });
        gv.Columns.Add(new GridViewColumn { Header = "Source", Width = 150, DisplayMemberBinding = new Binding("Source") });
        gv.Columns.Add(new GridViewColumn { Header = "Error Description / Reason", Width = 380, DisplayMemberBinding = new Binding("Message") });
        _listEventLogs.View = gv;
        sp.Children.Add(_listEventLogs);

        var bottomActions = new StackPanel { Orientation = Orientation.Horizontal };
        var btnOpenEventVwr = new Button
        {
            Content = "Open Windows Event Viewer (eventvwr.msc)",
            Height = 28,
            Padding = new Thickness(12, 0, 12, 0),
            Margin = new Thickness(0, 0, 8, 0),
            Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(241, 245, 249)),
            BorderBrush = BorderMuted
        };
        btnOpenEventVwr.Click += (s, e) => OpenTool("eventvwr.msc");
        bottomActions.Children.Add(btnOpenEventVwr);

        var btnClearLogs = new Button
        {
            Content = "Purge Temporary Event Logs",
            Height = 28,
            Padding = new Thickness(12, 0, 12, 0),
            Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(254, 242, 242)),
            Foreground = SvllRed,
            BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(254, 202, 202))
        };
        btnClearLogs.Click += async (s, e) =>
        {
            await ExecuteAsync("wevtutil.exe", "cl Application");
            await ExecuteAsync("wevtutil.exe", "cl System");
            await ScanEventLogsAsync();
        };
        bottomActions.Children.Add(btnClearLogs);

        sp.Children.Add(bottomActions);
        card.Child = sp;
        root.Children.Add(card);

        scroll.Content = root;
        _ = ScanEventLogsAsync();

        return scroll;
    }

    private async Task ScanEventLogsAsync()
    {
        Log("\n[EVENT LOG] Scanning Windows System and Application logs for errors (last 48 hours)...");
        _progEventScan.Visibility = Visibility.Visible;
        _progEventScan.IsIndeterminate = true;
        _allEventLogs.Clear();

        await Task.Run(() =>
        {
            try
            {
                // Query System Log
                string queryStr = "*[System[(Level=1 or Level=2 or Level=3) and TimeCreated[timediff(@SystemTime) <= 172800000]]]";
                var query = new EventLogQuery("System", PathType.LogName, queryStr);
                using var reader = new EventLogReader(query);

                for (var e = reader.ReadEvent(); e != null && _allEventLogs.Count < 100; e = reader.ReadEvent())
                {
                    _allEventLogs.Add(new EventLogEntryItem
                    {
                        TimeGenerated = e.TimeCreated ?? DateTime.Now,
                        Level = e.Level == 1 ? "Critical" : (e.Level == 2 ? "Error" : "Warning"),
                        EventId = e.Id,
                        Source = e.ProviderName,
                        Message = e.FormatDescription() ?? $"Event {e.Id} logged by {e.ProviderName}",
                        Category = "System"
                    });
                }

                // Query Application Log
                var appQuery = new EventLogQuery("Application", PathType.LogName, "*[System[(Level=1 or Level=2) and TimeCreated[timediff(@SystemTime) <= 172800000]]]");
                using var appReader = new EventLogReader(appQuery);
                for (var e = appReader.ReadEvent(); e != null && _allEventLogs.Count < 200; e = appReader.ReadEvent())
                {
                    _allEventLogs.Add(new EventLogEntryItem
                    {
                        TimeGenerated = e.TimeCreated ?? DateTime.Now,
                        Level = e.Level == 1 ? "Critical" : "Error",
                        EventId = e.Id,
                        Source = e.ProviderName,
                        Message = e.FormatDescription() ?? $"Application error in {e.ProviderName}",
                        Category = "Application"
                    });
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => Log($"[EVENT LOG ERROR] {ex.Message}"));
            }
        });

        _progEventScan.Visibility = Visibility.Collapsed;
        _progEventScan.IsIndeterminate = false;
        FilterEventLogs("ALL");
        Log($"[EVENT LOG] Extracted {_allEventLogs.Count} critical and error events.");
    }

    private void FilterEventLogs(string filter)
    {
        IEnumerable<EventLogEntryItem> list = _allEventLogs;
        if (filter == "BSOD") list = _allEventLogs.Where(x => x.EventId == 41 || x.Source.Contains("Kernel-Power"));
        else if (filter == "APP") list = _allEventLogs.Where(x => x.EventId == 1000 || x.EventId == 1002);
        else if (filter == "DISK") list = _allEventLogs.Where(x => x.EventId == 51 || x.Source.Contains("disk"));

        _listEventLogs.ItemsSource = list.OrderByDescending(x => x.TimeGenerated).ToList();
    }

    #endregion

    #region 3. IT Diagnostic Support Bundle (.ZIP Generator)

    private UIElement BuildSupportBundleView()
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
        sp.Children.Add(new TextBlock
        {
            Text = "1-CLICK IT DIAGNOSTIC SUPPORT BUNDLE GENERATOR (.ZIP)",
            FontSize = 13,
            FontWeight = FontWeights.Bold,
            Foreground = SvllBlue,
            Margin = new Thickness(0, 0, 0, 6)
        });
        sp.Children.Add(new TextBlock
        {
            Text = "Packages system specs, network routing tables, recent crash logs, and health diagnostic reports into a single compressed ZIP archive on your Desktop to attach to an IT support ticket.",
            FontSize = 11,
            Foreground = TextSubtle,
            Margin = new Thickness(0, 0, 0, 16)
        });

        // Primary Action Buttons
        var actionBtns = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 16) };

        var btnCreateBundle = new Button
        {
            Content = "📦 Package & Export Full Support Bundle (.ZIP)",
            Height = 42,
            Background = SvllBlue,
            Foreground = System.Windows.Media.Brushes.White,
            FontWeight = FontWeights.Bold,
            FontSize = 12,
            Padding = new Thickness(16, 0, 16, 0),
            Margin = new Thickness(0, 0, 10, 0),
            Cursor = Cursors.Hand
        };
        btnCreateBundle.Click += async (s, e) => await CreateSupportBundleAsync();
        actionBtns.Children.Add(btnCreateBundle);

        var btnMegaReport = new Button
        {
            Content = "📄 Generate Comprehensive IT Mega Report (HTML)",
            Height = 42,
            Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 41, 59)),
            Foreground = System.Windows.Media.Brushes.White,
            FontWeight = FontWeights.Bold,
            FontSize = 12,
            Padding = new Thickness(16, 0, 16, 0),
            Cursor = Cursors.Hand
        };
        btnMegaReport.Click += async (s, e) => await GenerateMegaReportAsync();
        actionBtns.Children.Add(btnMegaReport);

        sp.Children.Add(actionBtns);

        // Remote Assistance Launchers
        var remoteBox = new Border
        {
            Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(248, 250, 252)),
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(14)
        };
        var rStack = new StackPanel();
        rStack.Children.Add(new TextBlock { Text = "INSTANT REMOTE SCREEN SHARING LAUNCHERS", FontSize = 11, FontWeight = FontWeights.Bold, Foreground = SvllBlue, Margin = new Thickness(0, 0, 0, 8) });

        var rBtns = new StackPanel { Orientation = Orientation.Horizontal };
        var btnQuickAssist = new Button { Content = "Windows Quick Assist", Height = 30, Padding = new Thickness(12, 0, 12, 0), Margin = new Thickness(0, 0, 8, 0), Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(241, 245, 249)), BorderBrush = BorderMuted };
        btnQuickAssist.Click += (s, e) => OpenTool("quickassist.exe");
        rBtns.Children.Add(btnQuickAssist);

        var btnAnyDesk = new Button { Content = "AnyDesk Remote", Height = 30, Padding = new Thickness(12, 0, 12, 0), Margin = new Thickness(0, 0, 8, 0), Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(241, 245, 249)), BorderBrush = BorderMuted };
        btnAnyDesk.Click += (s, e) => OpenTool("AnyDesk.exe");
        rBtns.Children.Add(btnAnyDesk);

        var btnMstsc = new Button { Content = "Remote Desktop (MSTSC)", Height = 30, Padding = new Thickness(12, 0, 12, 0), Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(241, 245, 249)), BorderBrush = BorderMuted };
        btnMstsc.Click += (s, e) => OpenTool("mstsc.exe");
        rBtns.Children.Add(btnMstsc);

        rStack.Children.Add(rBtns);
        remoteBox.Child = rStack;
        sp.Children.Add(remoteBox);

        card.Child = sp;
        root.Children.Add(card);

        scroll.Content = root;
        return scroll;
    }

    private async Task CreateSupportBundleAsync()
    {
        Log("\n[SUPPORT] Packaging full diagnostic bundle with all system information...");
        string tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"SVLL_Bundle_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        string zipPath = System.IO.Path.Combine(desktop, $"SVLL_Support_Bundle_{Environment.MachineName}_{DateTime.Now:yyyyMMdd_HHmm}.zip");

        string ramSpec = _lblSpecRam?.Text ?? $"{Math.Round(GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / 1073741824.0, 1)} GB Installed";
        string osSpec = _cachedOsBuild;
        string cpuSpec = _cachedCpuName;
        string boardSpec = _cachedBoardName;
        string biosSpec = _cachedBiosSerial;
        string macSpec = _lblSpecMac?.Text ?? "N/A";
        string gwSpec = _lblSpecGw?.Text ?? "N/A";
        var healthSnapshot = _healthResults.ToList();
        var crashSnapshot = _allEventLogs.ToList();

        await Task.Run(async () =>
        {
            // 1. Comprehensive System Specs Summary
            var specs = new StringBuilder();
            specs.AppendLine("================================================================================");
            specs.AppendLine("          SHREE VASU LOGISTICS LIMITED - IT SUPPORT DIAGNOSTIC DUMP             ");
            specs.AppendLine("================================================================================");
            specs.AppendLine($"Timestamp:         {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            specs.AppendLine($"Machine Hostname:  {Environment.MachineName}");
            specs.AppendLine($"Logged-In User:    {Environment.UserDomainName}\\{Environment.UserName}");
            specs.AppendLine($"OS Architecture:   {Environment.OSVersion.VersionString} (64-bit OS: {Environment.Is64BitOperatingSystem})");
            specs.AppendLine($"OS Build / Display:{osSpec}");
            specs.AppendLine($"Processor:         {cpuSpec}");
            specs.AppendLine($"System Cores:      {Environment.ProcessorCount} Logical Cores");
            specs.AppendLine($"Installed Memory:  {ramSpec}");
            specs.AppendLine($"Motherboard Model: {boardSpec}");
            specs.AppendLine($"BIOS Serial/Tag:   {biosSpec}");
            specs.AppendLine($"Primary MAC:       {macSpec}");
            specs.AppendLine($"Gateway / DNS:     {gwSpec}");
            specs.AppendLine($"System Uptime:     {TimeSpan.FromMilliseconds(Environment.TickCount64):d'd 'h'h 'm'm'}");
            specs.AppendLine("================================================================================");
            File.WriteAllText(System.IO.Path.Combine(tempDir, "01_system_specs.txt"), specs.ToString());

            // 2. Full Network Configuration (ipconfig /all)
            try
            {
                var p = Process.Start(new ProcessStartInfo
                {
                    FileName = "ipconfig.exe",
                    Arguments = "/all",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                });
                string ipOut = p?.StandardOutput.ReadToEnd() ?? "";
                p?.WaitForExit();
                File.WriteAllText(System.IO.Path.Combine(tempDir, "02_ipconfig_all.txt"), ipOut);
            }
            catch { }

            // 3. Routing Table & ARP Cache
            try
            {
                var pRoute = Process.Start(new ProcessStartInfo { FileName = "route.exe", Arguments = "print", UseShellExecute = false, RedirectStandardOutput = true, CreateNoWindow = true });
                string routeOut = pRoute?.StandardOutput.ReadToEnd() ?? "";
                pRoute?.WaitForExit();
                File.WriteAllText(System.IO.Path.Combine(tempDir, "03_route_print.txt"), routeOut);
            }
            catch { }

            // 4. Running Processes Inventory
            try
            {
                var procSb = new StringBuilder();
                procSb.AppendLine("PID\t\tProcess Name\t\tWorking Set (MB)\tThreads");
                procSb.AppendLine("------------------------------------------------------------------------");
                foreach (var pr in Process.GetProcesses().OrderByDescending(x => { try { return x.WorkingSet64; } catch { return 0L; } }))
                {
                    try
                    {
                        procSb.AppendLine($"{pr.Id}\t\t{pr.ProcessName,-24}\t{Math.Round(pr.WorkingSet64 / 1048576.0, 1),8} MB\t\t{pr.Threads.Count}");
                    }
                    catch { }
                }
                File.WriteAllText(System.IO.Path.Combine(tempDir, "04_running_processes.txt"), procSb.ToString());
            }
            catch { }

            // 5. Windows Services Configuration
            try
            {
                var pSvc = Process.Start(new ProcessStartInfo { FileName = "sc.exe", Arguments = "query state= all", UseShellExecute = false, RedirectStandardOutput = true, CreateNoWindow = true });
                string svcOut = pSvc?.StandardOutput.ReadToEnd() ?? "";
                pSvc?.WaitForExit();
                File.WriteAllText(System.IO.Path.Combine(tempDir, "05_windows_services.txt"), svcOut);
            }
            catch { }

            // 6. Recent Crash & Stability Log Entries
            try
            {
                var logSb = new StringBuilder();
                logSb.AppendLine("=== RECENT CRITICAL & ERROR LOG EVENTS (LAST 48 HOURS) ===");
                foreach (var ev in crashSnapshot)
                {
                    logSb.AppendLine($"[{ev.TimeFormatted}] Category: {ev.Category} | Event ID: {ev.EventId} | Level: {ev.Level}");
                    logSb.AppendLine($"Source:   {ev.Source}");
                    logSb.AppendLine($"Details:  {ev.Message}");
                    logSb.AppendLine(new string('-', 70));
                }
                File.WriteAllText(System.IO.Path.Combine(tempDir, "06_event_log_crashes.txt"), logSb.ToString());
            }
            catch { }

            // 7. Full Interactive Mega HTML Diagnostic Report
            string megaHtml = BuildMegaReportContent(specs.ToString(), healthSnapshot, crashSnapshot);
            File.WriteAllText(System.IO.Path.Combine(tempDir, "00_SVLL_IT_MEGA_REPORT.html"), megaHtml);

            // 8. Create Final ZIP Archive
            if (File.Exists(zipPath)) File.Delete(zipPath);
            ZipFile.CreateFromDirectory(tempDir, zipPath, CompressionLevel.Optimal, false);
            Directory.Delete(tempDir, true);
        });

        Log($"[SUPPORT] Support bundle generated successfully: {zipPath}");
        MessageBox.Show($"Complete IT Support Bundle has been exported to your Desktop:\n\n{System.IO.Path.GetFileName(zipPath)}\n\nContains: System Specs, IPConfig, Route Tables, Process List, Services, Event Crashes, and the Mega HTML Report.", "Support Bundle Ready", MessageBoxButton.OK, MessageBoxImage.Information);
        try
        {
            Process.Start(new ProcessStartInfo { FileName = "explorer.exe", Arguments = $"/select,\"{zipPath}\"", UseShellExecute = true });
        }
        catch { }
    }

    private async Task GenerateMegaReportAsync()
    {
        Log("\n[REPORT] Compiling Comprehensive IT Mega Diagnostic Report...");
        string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        string reportPath = System.IO.Path.Combine(desktop, $"SVLL_IT_Mega_Report_{Environment.MachineName}_{DateTime.Now:yyyyMMdd_HHmm}.html");

        string ramSpec = _lblSpecRam?.Text ?? "RAM Telemetry available";
        string osSpec = _cachedOsBuild;
        string cpuSpec = _cachedCpuName;
        string boardSpec = _cachedBoardName;
        string biosSpec = _cachedBiosSerial;
        string macSpec = _lblSpecMac?.Text ?? "N/A";
        string gwSpec = _lblSpecGw?.Text ?? "N/A";
        var healthSnapshot = _healthResults.ToList();
        var crashSnapshot = _allEventLogs.ToList();

        await Task.Run(() =>
        {
            var specs = new StringBuilder();
            specs.AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss} | Hostname: {Environment.MachineName} | User: {Environment.UserDomainName}\\{Environment.UserName}");
            specs.AppendLine($"OS: {osSpec} | CPU: {cpuSpec} | Memory: {ramSpec}");
            specs.AppendLine($"Motherboard: {boardSpec} | BIOS Serial: {biosSpec} | MAC: {macSpec} | Gateway/DNS: {gwSpec}");

            string html = BuildMegaReportContent(specs.ToString(), healthSnapshot, crashSnapshot);
            File.WriteAllText(reportPath, html);
        });

        Log($"[REPORT] IT Mega Report created at: {reportPath}");
        MessageBox.Show($"IT Mega Diagnostic Report created on your Desktop:\n\n{System.IO.Path.GetFileName(reportPath)}\n\nOpening in your browser...", "Mega Report Ready", MessageBoxButton.OK, MessageBoxImage.Information);
        try
        {
            Process.Start(new ProcessStartInfo { FileName = reportPath, UseShellExecute = true });
        }
        catch { }
    }

    private string BuildMegaReportContent(string specsSummary, List<HealthCheckResult> healthList, List<EventLogEntryItem> crashList)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html><head><meta charset='utf-8'>");
        sb.AppendLine("<title>SVLL Enterprise Fleet IT Diagnostic Report</title>");
        sb.AppendLine("<style>");
        sb.AppendLine("body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; margin: 0; padding: 25px; background: #f8fafc; color: #0f172a; }");
        sb.AppendLine(".header { background: linear-gradient(135deg, #1a4bb2, #0f2d72); color: white; padding: 24px; border-radius: 10px; margin-bottom: 20px; box-shadow: 0 4px 6px -1px rgba(0,0,0,0.1); }");
        sb.AppendLine(".header h1 { margin: 0 0 6px 0; font-size: 22px; }");
        sb.AppendLine(".header p { margin: 0; opacity: 0.85; font-size: 13px; }");
        sb.AppendLine(".card { background: white; border: 1px solid #e2e8f0; border-radius: 8px; padding: 20px; margin-bottom: 20px; box-shadow: 0 1px 3px rgba(0,0,0,0.05); }");
        sb.AppendLine(".card h2 { margin: 0 0 14px 0; font-size: 15px; color: #1a4bb2; border-bottom: 2px solid #f1f5f9; padding-bottom: 8px; }");
        sb.AppendLine("table { width: 100%; border-collapse: collapse; font-size: 12px; }");
        sb.AppendLine("th, td { padding: 9px 12px; text-align: left; border-bottom: 1px solid #e2e8f0; }");
        sb.AppendLine("th { background: #f8fafc; color: #475569; font-weight: 600; }");
        sb.AppendLine(".badge { padding: 3px 8px; border-radius: 4px; font-weight: 600; font-size: 11px; }");
        sb.AppendLine(".badge-ok { background: #dcfce7; color: #166534; }");
        sb.AppendLine(".badge-warn { background: #fee2e2; color: #991b1b; }");
        sb.AppendLine("pre { background: #0f172a; color: #e2e8f0; padding: 14px; border-radius: 6px; font-size: 11.5px; overflow-x: auto; font-family: Consolas, monospace; }");
        sb.AppendLine(".grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(220px, 1fr)); gap: 14px; margin-bottom: 15px; }");
        sb.AppendLine(".metric { background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 6px; padding: 12px; }");
        sb.AppendLine(".metric-title { font-size: 10px; font-weight: 700; color: #64748b; margin-bottom: 4px; }");
        sb.AppendLine(".metric-val { font-size: 16px; font-weight: 700; color: #0f172a; }");
        sb.AppendLine("</style></head><body>");

        sb.AppendLine("<div class='header'>");
        sb.AppendLine("<h1>SHREE VASU LOGISTICS LIMITED</h1>");
        sb.AppendLine($"<p>Enterprise Fleet IT Workstation Diagnostic Mega Report &bull; Host: <b>{Environment.MachineName}</b> &bull; Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>");
        sb.AppendLine("</div>");

        // System Specs Grid
        sb.AppendLine("<div class='card'>");
        sb.AppendLine("<h2>1. HARDWARE TOPOLOGY & HOST SPECIFICATIONS</h2>");
        sb.AppendLine("<div class='grid'>");
        sb.AppendLine($"<div class='metric'><div class='metric-title'>ENDPOINT HOST</div><div class='metric-val'>{Environment.MachineName}</div></div>");
        sb.AppendLine($"<div class='metric'><div class='metric-title'>ACTIVE USER</div><div class='metric-val'>{Environment.UserName}</div></div>");
        sb.AppendLine($"<div class='metric'><div class='metric-title'>OPERATING SYSTEM</div><div class='metric-val'>{_cachedOsBuild}</div></div>");
        sb.AppendLine($"<div class='metric'><div class='metric-title'>CPU PROCESSOR</div><div class='metric-val'>{_cachedCpuName}</div></div>");
        sb.AppendLine($"<div class='metric'><div class='metric-title'>BASEBOARD / MOTHERBOARD</div><div class='metric-val'>{_cachedBoardName}</div></div>");
        sb.AppendLine($"<div class='metric'><div class='metric-title'>SERVICE TAG / SERIAL</div><div class='metric-val'>{_cachedBiosSerial}</div></div>");
        sb.AppendLine("</div>");
        sb.AppendLine("<pre>" + specsSummary + "</pre>");
        sb.AppendLine("</div>");

        // Health Audit Table
        sb.AppendLine("<div class='card'>");
        sb.AppendLine("<h2>2. OPERATING SYSTEM FOUNDATIONAL HEALTH AUDIT</h2>");
        sb.AppendLine("<table><tr><th>Component</th><th>Status</th><th>Diagnostic Details</th><th>Recommended Remediation</th></tr>");
        foreach (var r in healthList)
        {
            string cls = r.IsWarning ? "badge-warn" : "badge-ok";
            sb.AppendLine($"<tr><td><b>{r.Component}</b></td><td><span class='badge {cls}'>{r.Status}</span></td><td>{r.Details}</td><td>{r.Recommendation}</td></tr>");
        }
        sb.AppendLine("</table></div>");

        // Event Logs & Crash Analysis
        sb.AppendLine("<div class='card'>");
        sb.AppendLine($"<h2>3. CRITICAL EVENT LOGS & RECENT CRASHES (FOUND: {crashList.Count})</h2>");
        if (crashList.Count > 0)
        {
            sb.AppendLine("<table><tr><th>Timestamp</th><th>Category</th><th>Event ID</th><th>Level</th><th>Message Details</th></tr>");
            foreach (var ev in crashList)
            {
                sb.AppendLine($"<tr><td>{ev.TimeFormatted}</td><td>{ev.Category}</td><td>{ev.EventId}</td><td><span class='badge badge-warn'>{ev.Level}</span></td><td>{ev.Message}</td></tr>");
            }
            sb.AppendLine("</table>");
        }
        else
        {
            sb.AppendLine("<p style='color:#166534; font-weight:600;'>✔ No critical kernel power drops or unhandled application crashes identified in the last 48 hours.</p>");
        }
        sb.AppendLine("</div>");

        sb.AppendLine("<div style='text-align: center; color: #94a3b8; font-size: 11px; margin-top: 30px;'>SVLL IT Support Workstation &bull; Developed by Kunal Turkar &bull; Confidential Internal Diagnostic</div>");
        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    #endregion
}
