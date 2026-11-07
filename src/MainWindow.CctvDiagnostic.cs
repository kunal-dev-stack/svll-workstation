// =============================================================================
// SVLL IT Workstation — CCTV & NVR Diagnostic, Troubleshooting & Health Studio
// Enterprise Camera Matrix, SLA Audits, Automated Health Reporting & Subnet Discovery
// =============================================================================
#if BUILD_ADMIN

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Media;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using SVLL_IT_Workstation.Models;

namespace SVLL_IT_Workstation;

public partial class MainWindow
{
    // Diagnostic Studio State
    private TextBlock _lblDiagTotalNvrs = null!;
    private TextBlock _lblDiagTotalChannels = null!;
    private TextBlock _lblDiagOnlinePercent = null!;
    private TextBlock _lblDiagOnlineRatio = null!;
    private TextBlock _lblDiagAvgLatency = null!;
    private TextBlock _lblDiagHealthGrade = null!;
    private Border _diagGradeBadge = null!;
    private ProgressBar _pbDiagAudit = null!;
    private TextBlock _lblDiagProgress = null!;
    private Button _btnRunFleetAudit = null!;
    private Button _btnRunTargetAudit = null!;
    private Button _btnCancelFleetAudit = null!;
    private ComboBox _cmbDiagTargetNvr = null!;
    private Border _diagMatrixHeaderHost = null!;
    private StackPanel _diagMatrixContainer = null!;
    private ComboBox _cmbReportType = null!;
    private Border _subnetDiscoveryDrawer = null!;
    private TextBox _txtSubnetBase = null!;
    private StackPanel _discoveredDevicesContainer = null!;
    private ProgressBar _pbSubnetScan = null!;
    private TextBlock _lblSubnetScanStatus = null!;
    private Button _btnStartSubnetScan = null!;

    private FleetDiagnosticReport? _latestDiagnosticReport;
    private CancellationTokenSource? _diagCts;
    private CancellationTokenSource? _subnetCts;
    private string _diagResultsFilter = "ALL";
    private readonly List<(Border HubCard, bool HasFaults, Action ExpandChannels, Action CollapseChannels)> _diagHubCards = new();

    // ─── Entry Point: Diagnostic & Troubleshooting Studio View ────────────────
    public UIElement BuildCctvDiagnosticView()
    {
        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 0: Executive KPI Banner
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 1: Action & Report Toolbar
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 2: Progress & Status Banner
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 3: Subnet Discovery Tool (Collapsible)
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // 4: Channel Diagnostic Matrix

        // 0. Executive KPI Banner
        var kpiBanner = BuildDiagnosticKpiBanner();
        Grid.SetRow(kpiBanner, 0);
        root.Children.Add(kpiBanner);

        // 1. Action & Report Toolbar
        var toolbar = BuildDiagnosticToolbar();
        Grid.SetRow(toolbar, 1);
        root.Children.Add(toolbar);

        // 2. Progress & Status Banner
        var progressBanner = BuildDiagnosticProgressBanner();
        Grid.SetRow(progressBanner, 2);
        root.Children.Add(progressBanner);

        // 3. Subnet Discovery Tool
        _subnetDiscoveryDrawer = BuildSubnetDiscoveryDrawer();
        Grid.SetRow(_subnetDiscoveryDrawer, 3);
        root.Children.Add(_subnetDiscoveryDrawer);

        // 4. Channel Diagnostic Matrix
        var matrixArea = BuildDiagnosticMatrixArea();
        Grid.SetRow(matrixArea, 4);
        root.Children.Add(matrixArea);

        return root;
    }

    // ─── 0. Executive KPI Banner ─────────────────────────────────────────────
    private Border BuildDiagnosticKpiBanner()
    {
        var card = new Border
        {
            Background = BgCard,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(14, 10, 14, 10),
            Margin = new Thickness(0, 0, 0, 8)
        };

        var grid = new UniformGrid { Rows = 1, Columns = 4 };

        // 1. Surveillance Availability KPI
        var kpi1 = CreateKpiCard("FLEET AVAILABILITY", out _lblDiagOnlinePercent, out _lblDiagOnlineRatio, "#16A34A");
        _lblDiagOnlinePercent.Text = "-- %";
        _lblDiagOnlineRatio.Text = "Run audit to inspect";
        grid.Children.Add(kpi1);

        // 2. Hubs & Channels KPI
        var kpi2 = CreateKpiCard("SURVEILLANCE HUBS", out _lblDiagTotalNvrs, out _lblDiagTotalChannels, "#1A4BB2");
        _lblDiagTotalNvrs.Text = $"{NvrDeviceStorage.Instance.GetDevices().Count} NVRs";
        _lblDiagTotalChannels.Text = $"{NvrDeviceStorage.Instance.GetDevices().Sum(d => d.ChannelCount)} Total Channels";
        grid.Children.Add(kpi2);

        // 3. Average Latency KPI
        var kpi3 = CreateKpiCard("AVG NETWORK LATENCY", out _lblDiagAvgLatency, out var lblLatencySub, "#0284C7");
        _lblDiagAvgLatency.Text = "-- ms";
        lblLatencySub.Text = "ICMP Ping & Jitter";
        grid.Children.Add(kpi3);

        // 4. System Health Grade
        var gradeBorder = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12, 8, 12, 8),
            Margin = new Thickness(4)
        };
        var gradeStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        gradeStack.Children.Add(new TextBlock
        {
            Text = "SURVEILLANCE HEALTH GRADE",
            FontSize = 10,
            FontWeight = FontWeights.Bold,
            Foreground = TextSubtle
        });

        _diagGradeBadge = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(220, 252, 231)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(34, 197, 94)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(8, 4, 8, 4),
            Margin = new Thickness(0, 4, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Left
        };
        _lblDiagHealthGrade = new TextBlock
        {
            Text = "GRADE A+ (READY FOR AUDIT)",
            FontSize = 11,
            FontWeight = FontWeights.ExtraBold,
            Foreground = new SolidColorBrush(Color.FromRgb(22, 101, 52))
        };
        _diagGradeBadge.Child = _lblDiagHealthGrade;
        gradeStack.Children.Add(_diagGradeBadge);
        gradeBorder.Child = gradeStack;
        grid.Children.Add(gradeBorder);

        card.Child = grid;
        return card;
    }

    private Border CreateKpiCard(string title, out TextBlock valBlock, out TextBlock subBlock, string hexColor)
    {
        var border = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12, 8, 12, 8),
            Margin = new Thickness(4)
        };

        var stack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        stack.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 10,
            FontWeight = FontWeights.Bold,
            Foreground = TextSubtle
        });

        valBlock = new TextBlock
        {
            Text = "--",
            FontSize = 19,
            FontWeight = FontWeights.ExtraBold,
            Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString(hexColor)!,
            Margin = new Thickness(0, 2, 0, 0)
        };
        stack.Children.Add(valBlock);

        subBlock = new TextBlock
        {
            Text = "--",
            FontSize = 10.5,
            Foreground = TextSubtle,
            Margin = new Thickness(0, 2, 0, 0)
        };
        stack.Children.Add(subBlock);

        border.Child = stack;
        return border;
    }

    // ─── 1. Action & Report Toolbar ──────────────────────────────────────────
    private Border BuildDiagnosticToolbar()
    {
        var bar = new Border
        {
            Background = BgCard,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(12, 8, 12, 8),
            Margin = new Thickness(0, 0, 0, 8)
        };

        var barWrap = new WrapPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

        // Left Actions
        var leftStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 2, 14, 2) };

        leftStack.Children.Add(new TextBlock
        {
            Text = "Target:",
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            Foreground = TextSubtle,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 6, 0)
        });

        _cmbDiagTargetNvr = new ComboBox
        {
            Height = 28,
            Width = 230,
            FontSize = 11,
            Margin = new Thickness(0, 0, 6, 0),
            VerticalContentAlignment = VerticalAlignment.Center,
            ToolTip = "Select a specific NVR or campus to diagnose, or choose All NVRs"
        };
        PopulateDiagTargetCombo();
        leftStack.Children.Add(_cmbDiagTargetNvr);

        _btnRunTargetAudit = new Button
        {
            Content = "🎯 Audit Selected Set",
            Height = 28,
            Padding = new Thickness(12, 0, 12, 0),
            FontSize = 11.5,
            FontWeight = FontWeights.Bold,
            Background = SvllBlue,
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand,
            ToolTip = "Fast audit of only the selected NVR or campus in 1-2 seconds",
            Margin = new Thickness(0, 0, 6, 0)
        };
        _btnRunTargetAudit.Click += async (s, e) => await RunSelectedTargetAuditAsync();
        leftStack.Children.Add(_btnRunTargetAudit);

        _btnRunFleetAudit = new Button
        {
            Content = "⚡ Full Fleet Audit",
            Height = 28,
            Padding = new Thickness(12, 0, 12, 0),
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Background = Brushes.White,
            Foreground = SvllBlue,
            BorderBrush = SvllBlue,
            BorderThickness = new Thickness(1),
            Cursor = Cursors.Hand,
            ToolTip = "Fast parallel audit across all 46 NVRs and channels",
            Margin = new Thickness(0, 0, 6, 0)
        };
        _btnRunFleetAudit.Click += async (s, e) => await RunDiagnosticAuditAsync(null);
        leftStack.Children.Add(_btnRunFleetAudit);

        _btnCancelFleetAudit = new Button
        {
            Content = "🛑 Cancel",
            Height = 28,
            Padding = new Thickness(10, 0, 10, 0),
            FontSize = 11,
            Background = Brushes.White,
            Foreground = SvllRed,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            Margin = new Thickness(6, 0, 0, 0),
            Cursor = Cursors.Hand,
            Visibility = Visibility.Collapsed
        };
        _btnCancelFleetAudit.Click += (s, e) => _diagCts?.Cancel();
        leftStack.Children.Add(_btnCancelFleetAudit);

        var btnSubnetDiscovery = new Button
        {
            Content = "🌐 Subnet Camera Discovery",
            Height = 28,
            Padding = new Thickness(12, 0, 12, 0),
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Background = Brushes.White,
            Foreground = TextDark,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            Margin = new Thickness(8, 0, 0, 0),
            Cursor = Cursors.Hand,
            ToolTip = "Scan local branch subnet to automatically discover unmapped IP cameras and NVRs"
        };
        btnSubnetDiscovery.Click += (s, e) =>
        {
            _subnetDiscoveryDrawer.Visibility = (_subnetDiscoveryDrawer.Visibility == Visibility.Visible)
                ? Visibility.Collapsed
                : Visibility.Visible;
        };
        leftStack.Children.Add(btnSubnetDiscovery);

        barWrap.Children.Add(leftStack);

        // Right Actions: Report Generation & Export Engine
        var rightStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 2, 0, 2) };

        rightStack.Children.Add(new TextBlock
        {
            Text = "Report Template: ",
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Foreground = TextSubtle,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 6, 0)
        });

        _cmbReportType = new ComboBox
        {
            Height = 28,
            Width = 210,
            FontSize = 11,
            Margin = new Thickness(0, 0, 8, 0)
        };
        _cmbReportType.Items.Add("Executive Surveillance Audit (HTML/PDF)");
        _cmbReportType.Items.Add("Technical Health Matrix (CSV)");
        _cmbReportType.Items.Add("Corrective Action Plan (CAP Report)");
        _cmbReportType.SelectedIndex = 0;
        rightStack.Children.Add(_cmbReportType);

        var btnViewHtml = new Button
        {
            Content = "🖨️ View & Print HTML Report",
            Height = 28,
            Padding = new Thickness(12, 0, 12, 0),
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            Background = new SolidColorBrush(Color.FromRgb(22, 101, 52)), // Dark Emerald
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            Margin = new Thickness(0, 0, 6, 0),
            Cursor = Cursors.Hand,
            ToolTip = "Generates and opens styled executive surveillance report in browser for 1-click printing or PDF export"
        };
        btnViewHtml.Click += (s, e) => ExportHtmlReport();
        rightStack.Children.Add(btnViewHtml);

        var btnExportCsv = new Button
        {
            Content = "📥 Export CSV",
            Height = 28,
            Padding = new Thickness(10, 0, 10, 0),
            FontSize = 11,
            Background = Brushes.White,
            Foreground = TextDark,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            Margin = new Thickness(0, 0, 6, 0),
            Cursor = Cursors.Hand,
            ToolTip = "Save channel diagnostic dataset as CSV spreadsheet"
        };
        btnExportCsv.Click += (s, e) => ExportCsvReport();
        rightStack.Children.Add(btnExportCsv);

        var btnCopyMarkdown = new Button
        {
            Content = "📋 Copy Report",
            Height = 28,
            Padding = new Thickness(10, 0, 10, 0),
            FontSize = 11,
            Background = Brushes.White,
            Foreground = TextDark,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            Cursor = Cursors.Hand,
            ToolTip = "Copy formatted Markdown health check summary to clipboard"
        };
        btnCopyMarkdown.Click += (s, e) => CopyMarkdownReport();
        rightStack.Children.Add(btnCopyMarkdown);

        barWrap.Children.Add(rightStack);

        bar.Child = barWrap;
        return bar;
    }

    // ─── 2. Progress & Status Banner ─────────────────────────────────────────
    private Border BuildDiagnosticProgressBanner()
    {
        var bar = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12, 6, 12, 6),
            Margin = new Thickness(0, 0, 0, 8)
        };

        var stack = new StackPanel();

        var textRow = new Grid { Margin = new Thickness(0, 0, 0, 4) };
        textRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        textRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        _lblDiagProgress = new TextBlock
        {
            Text = "Diagnostic Engine Ready. Click '⚡ Run Full Fleet Audit' to inspect all cameras and NVR ports.",
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Foreground = TextDark
        };
        Grid.SetColumn(_lblDiagProgress, 0);
        textRow.Children.Add(_lblDiagProgress);

        var lblHelp = new TextBlock
        {
            Text = "Probes: ICMP Ping • HTTP :80 • RTSP :554 • ISAPI/CGI Snapshots",
            FontSize = 10,
            Foreground = TextSubtle
        };
        Grid.SetColumn(lblHelp, 1);
        textRow.Children.Add(lblHelp);

        stack.Children.Add(textRow);

        _pbDiagAudit = new ProgressBar
        {
            Height = 4,
            Minimum = 0,
            Maximum = 100,
            Value = 0,
            Foreground = SvllBlue,
            Background = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
            BorderThickness = new Thickness(0)
        };
        stack.Children.Add(_pbDiagAudit);

        bar.Child = stack;
        return bar;
    }

    // ─── 3. Subnet Camera Discovery Drawer ───────────────────────────────────
    private Border BuildSubnetDiscoveryDrawer()
    {
        var drawer = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(147, 197, 253)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(14, 10, 14, 10),
            Margin = new Thickness(0, 0, 0, 8),
            Visibility = Visibility.Collapsed
        };

        var rootStack = new StackPanel();

        var headerGrid = new Grid { Margin = new Thickness(0, 0, 0, 8) };
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var titleStack = new StackPanel { Orientation = Orientation.Horizontal };
        titleStack.Children.Add(new TextBlock
        {
            Text = "🌐 Local Subnet Camera & NVR Auto-Discovery",
            FontSize = 12,
            FontWeight = FontWeights.Bold,
            Foreground = SvllBlue,
            VerticalAlignment = VerticalAlignment.Center
        });
        titleStack.Children.Add(CctvBadge("PROBES RTSP 554, HTTP 80/8000, DAHUA 37777", Color.FromRgb(30, 64, 175), Color.FromRgb(219, 234, 254)));
        Grid.SetColumn(titleStack, 0);
        headerGrid.Children.Add(titleStack);

        var btnCloseDrawer = new Button
        {
            Content = "✕ Close",
            Height = 22,
            Padding = new Thickness(8, 0, 8, 0),
            FontSize = 10,
            Background = Brushes.White,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            Cursor = Cursors.Hand
        };
        btnCloseDrawer.Click += (s, e) => drawer.Visibility = Visibility.Collapsed;
        Grid.SetColumn(btnCloseDrawer, 1);
        headerGrid.Children.Add(btnCloseDrawer);
        rootStack.Children.Add(headerGrid);

        // Subnet IP Range Controls
        var controlsRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };
        controlsRow.Children.Add(new TextBlock { Text = "Subnet Base: ", FontSize = 11, VerticalAlignment = VerticalAlignment.Center, Foreground = TextDark });

        // Auto-detect default subnet from first registered NVR or local machine
        string defSubnet = "192.168.1";
        var firstDev = NvrDeviceStorage.Instance.GetDevices().FirstOrDefault();
        if (firstDev != null && !string.IsNullOrEmpty(firstDev.Host) && firstDev.Host.Contains("."))
        {
            var parts = firstDev.Host.Split('.');
            if (parts.Length == 4) defSubnet = $"{parts[0]}.{parts[1]}.{parts[2]}";
        }

        _txtSubnetBase = new TextBox
        {
            Text = defSubnet,
            Width = 110,
            Height = 26,
            FontSize = 11,
            VerticalContentAlignment = VerticalAlignment.Center,
            Margin = new Thickness(4, 0, 8, 0)
        };
        controlsRow.Children.Add(_txtSubnetBase);

        controlsRow.Children.Add(new TextBlock { Text = ".1 to .254", FontSize = 11, VerticalAlignment = VerticalAlignment.Center, Foreground = TextSubtle, Margin = new Thickness(0, 0, 12, 0) });

        _btnStartSubnetScan = new Button
        {
            Content = "🔍 Scan Subnet Now",
            Height = 26,
            Padding = new Thickness(12, 0, 12, 0),
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            Background = SvllBlue,
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand
        };
        _btnStartSubnetScan.Click += async (s, e) => await StartCctvSubnetScanAsync();
        controlsRow.Children.Add(_btnStartSubnetScan);

        _lblSubnetScanStatus = new TextBlock
        {
            Text = "Enter subnet (e.g. 192.168.10) and click 'Scan Subnet Now'",
            FontSize = 10.5,
            Foreground = TextSubtle,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(12, 0, 0, 0)
        };
        controlsRow.Children.Add(_lblSubnetScanStatus);

        rootStack.Children.Add(controlsRow);

        _pbSubnetScan = new ProgressBar
        {
            Height = 3,
            Minimum = 0,
            Maximum = 100,
            Value = 0,
            Foreground = SvllBlue,
            Visibility = Visibility.Collapsed,
            Margin = new Thickness(0, 0, 0, 8)
        };
        rootStack.Children.Add(_pbSubnetScan);

        // Discovered Devices List Host
        _discoveredDevicesContainer = new StackPanel();
        rootStack.Children.Add(_discoveredDevicesContainer);

        drawer.Child = rootStack;
        return drawer;
    }

    private async Task StartCctvSubnetScanAsync()
    {
        string subnet = _txtSubnetBase.Text.Trim();
        if (string.IsNullOrEmpty(subnet)) return;

        _btnStartSubnetScan.IsEnabled = false;
        _pbSubnetScan.Visibility = Visibility.Visible;
        _pbSubnetScan.Value = 0;
        _lblSubnetScanStatus.Text = $"Scanning {subnet}.1 - 254 for open CCTV ports...";
        _discoveredDevicesContainer.Children.Clear();

        _subnetCts?.Cancel();
        _subnetCts = new CancellationTokenSource();

        var progress = new Progress<int>(pct => Dispatcher.Invoke(() => _pbSubnetScan.Value = pct));

        try
        {
            var discovered = await CctvDiagnosticService.ScanSubnetAsync(subnet, 1, 254, progress, _subnetCts.Token);

            _lblSubnetScanStatus.Text = $"Scan complete! Found {discovered.Count} responsive camera/NVR device(s).";
            _pbSubnetScan.Visibility = Visibility.Collapsed;

            if (discovered.Count == 0)
            {
                _discoveredDevicesContainer.Children.Add(new TextBlock
                {
                    Text = "No open RTSP or HTTP CCTV services found on this subnet. Check network routing or VLAN tags.",
                    FontSize = 11,
                    Foreground = TextSubtle,
                    Margin = new Thickness(0, 6, 0, 6)
                });
            }
            else
            {
                foreach (var d in discovered)
                {
                    _discoveredDevicesContainer.Children.Add(BuildDiscoveredDeviceRow(d));
                }
            }
        }
        catch (Exception ex)
        {
            _lblSubnetScanStatus.Text = $"Scan interrupted: {ex.Message}";
        }
        finally
        {
            _btnStartSubnetScan.IsEnabled = true;
        }
    }

    private Border BuildDiscoveredDeviceRow(DiscoveredCamera d)
    {
        var row = new Border
        {
            Background = Brushes.White,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(10, 6, 10, 6),
            Margin = new Thickness(0, 0, 0, 4)
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) }); // IP & Ping
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) }); // Brand
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Ports
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Add Button

        var spIp = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        spIp.Children.Add(new TextBlock { Text = d.IpAddress, FontSize = 11.5, FontWeight = FontWeights.Bold, Foreground = SvllBlue });
        spIp.Children.Add(new TextBlock { Text = $"Ping: {d.LatencyMs}ms", FontSize = 10, Foreground = TextSubtle });
        Grid.SetColumn(spIp, 0);
        grid.Children.Add(spIp);

        var lblBrand = new TextBlock
        {
            Text = $"📷 {d.ProbableBrand}",
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Foreground = TextDark,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(lblBrand, 1);
        grid.Children.Add(lblBrand);

        var spPorts = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        if (d.RtspOpen) spPorts.Children.Add(CctvBadge("RTSP :554 OPEN", Color.FromRgb(22, 101, 52), Color.FromRgb(220, 252, 231)));
        if (d.HttpOpen) spPorts.Children.Add(CctvBadge($"HTTP :{d.HttpPort} OPEN", Color.FromRgb(30, 64, 175), Color.FromRgb(219, 234, 254)));
        Grid.SetColumn(spPorts, 2);
        grid.Children.Add(spPorts);

        var btnAdd = new Button
        {
            Content = "➕ Register as NVR",
            Height = 24,
            Padding = new Thickness(10, 0, 10, 0),
            FontSize = 10.5,
            FontWeight = FontWeights.Bold,
            Background = SvllBlue,
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand
        };
        btnAdd.Click += (s, e) =>
        {
            var brand = NvrBrand.GenericRtsp;
            if (d.ProbableBrand.Contains("Hikvision")) brand = NvrBrand.Hikvision;
            else if (d.ProbableBrand.Contains("Dahua") || d.ProbableBrand.Contains("CP PLUS")) brand = NvrBrand.CpPlus;

            var newDevice = new NvrDevice
            {
                Name = $"Discovered NVR ({d.IpAddress})",
                Host = d.IpAddress,
                Brand = brand,
                HttpPort = d.HttpPort,
                RtspPort = d.RtspPort,
                ChannelCount = 8
            };

            var dlg = new NvrEditorDialog(newDevice, this);
            if (dlg.ShowDialog() == true)
            {
                RefreshCctvTree();
                RefreshDiagnosticViewIfVisible();
            }
        };
        Grid.SetColumn(btnAdd, 3);
        grid.Children.Add(btnAdd);

        row.Child = grid;
        return row;
    }

    // ─── 4. Channel Diagnostic Matrix Area ───────────────────────────────────
    private UIElement BuildDiagnosticMatrixArea()
    {
        var card = new Border
        {
            Background = BgCard,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(12)
        };

        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Dynamic Header / Filter row
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Scroll container

        // Dynamic Header Host (Switches between Fleet Hubs overview and Audit Results Filters)
        _diagMatrixHeaderHost = new Border { Margin = new Thickness(0, 0, 0, 8) };
        Grid.SetRow(_diagMatrixHeaderHost, 0);
        grid.Children.Add(_diagMatrixHeaderHost);

        // Scroll Viewer for Diagnostic Cards
        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };

        _diagMatrixContainer = new StackPanel();
        scroll.Content = _diagMatrixContainer;

        Grid.SetRow(scroll, 1);
        grid.Children.Add(scroll);

        card.Child = grid;

        // Populate initial readiness overview
        PopulateInitialDiagnosticMatrix();

        return card;
    }

    private static TextBlock CreateMatrixHeadLabel(string text, int col)
    {
        var tb = new TextBlock
        {
            Text = text,
            FontSize = 10,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(71, 85, 105)),
            Padding = new Thickness(8, 6, 8, 6)
        };
        Grid.SetColumn(tb, col);
        return tb;
    }

    private void PopulateInitialDiagnosticMatrix()
    {
        if (_diagMatrixContainer == null) return;
        _diagMatrixContainer.Children.Clear();
        _diagHubCards.Clear();

        var devices = NvrDeviceStorage.Instance.GetDevices();
        if (devices.Count == 0)
        {
            if (_diagMatrixHeaderHost != null) _diagMatrixHeaderHost.Child = null;
            _diagMatrixContainer.Children.Add(new TextBlock
            {
                Text = "No NVR/DVR hubs registered. Add an NVR above to run diagnostics.",
                FontSize = 12,
                Foreground = TextSubtle,
                Margin = new Thickness(12)
            });
            return;
        }

        // Header for Fleet Overview
        var headerBar = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(12, 8, 12, 8)
        };
        var headGrid = new Grid();
        headGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        headGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var titleStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        titleStack.Children.Add(new TextBlock
        {
            Text = "🏢 REGISTERED NVR SURVEILLANCE FLEET HUBS",
            FontSize = 11.5,
            FontWeight = FontWeights.Bold,
            Foreground = TextDark
        });
        titleStack.Children.Add(new TextBlock
        {
            Text = $"  ({devices.Count} Hubs • {devices.Sum(d => d.ChannelCount)} Channels)",
            FontSize = 11,
            Foreground = SvllBlue,
            FontWeight = FontWeights.SemiBold
        });
        Grid.SetColumn(titleStack, 0);
        headGrid.Children.Add(titleStack);

        var lblStatusInfo = new TextBlock
        {
            Text = "Ready for SLA Health Audit",
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(22, 101, 52)),
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(lblStatusInfo, 1);
        headGrid.Children.Add(lblStatusInfo);

        headerBar.Child = headGrid;
        if (_diagMatrixHeaderHost != null) _diagMatrixHeaderHost.Child = headerBar;

        // Readiness Hero Banner
        var heroBorder = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(239, 246, 255)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(191, 219, 254)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(14, 10, 14, 10),
            Margin = new Thickness(0, 0, 0, 10)
        };
        var heroGrid = new Grid();
        heroGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        heroGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var heroStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        heroStack.Children.Add(new TextBlock
        {
            Text = "⚡ Diagnostic & Troubleshooting Studio Ready",
            FontSize = 12.5,
            FontWeight = FontWeights.Bold,
            Foreground = SvllBlue
        });
        heroStack.Children.Add(new TextBlock
        {
            Text = "Click '⚡ Run Full Fleet Audit' to audit all 46 hubs, or select a campus/hub above to diagnose in 1-2 seconds.",
            FontSize = 11,
            Foreground = TextSubtle,
            Margin = new Thickness(0, 2, 0, 0)
        });
        Grid.SetColumn(heroStack, 0);
        heroGrid.Children.Add(heroStack);

        var btnHeroRun = new Button
        {
            Content = "⚡ Run Full Fleet Audit",
            Height = 28,
            Padding = new Thickness(14, 0, 14, 0),
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            Background = SvllBlue,
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand,
            VerticalAlignment = VerticalAlignment.Center
        };
        btnHeroRun.Click += async (s, e) => await RunDiagnosticAuditAsync(null);
        Grid.SetColumn(btnHeroRun, 1);
        heroGrid.Children.Add(btnHeroRun);

        heroBorder.Child = heroGrid;
        _diagMatrixContainer.Children.Add(heroBorder);

        // Fast NVR Hub Readiness Cards (46 cards render in < 5ms)
        foreach (var d in devices)
        {
            var hubCard = BuildNvrReadinessCard(d);
            _diagMatrixContainer.Children.Add(hubCard);
        }
    }

    private Border BuildNvrReadinessCard(NvrDevice device)
    {
        var border = new Border
        {
            Background = Brushes.White,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(10, 8, 10, 8),
            Margin = new Thickness(0, 0, 0, 6)
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) }); // NVR Name
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(180) }); // Location
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(180) }); // Host & Ports
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) }); // Channels
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Status
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Actions

        // Col 0: NVR Name
        var spName = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        spName.Children.Add(new TextBlock
        {
            Text = "🏢 " + device.Name,
            FontSize = 11.5,
            FontWeight = FontWeights.Bold,
            Foreground = TextDark,
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        spName.Children.Add(new TextBlock
        {
            Text = device.GetBrandDisplayName(),
            FontSize = 9.5,
            Foreground = TextSubtle
        });
        Grid.SetColumn(spName, 0);
        grid.Children.Add(spName);

        // Col 1: Location
        var spLoc = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        spLoc.Children.Add(new TextBlock
        {
            Text = "📍 " + device.Location,
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Foreground = SvllBlue,
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        if (!string.IsNullOrWhiteSpace(device.SubLocation))
        {
            spLoc.Children.Add(new TextBlock { Text = device.SubLocation, FontSize = 9.5, Foreground = TextSubtle, TextTrimming = TextTrimming.CharacterEllipsis });
        }
        Grid.SetColumn(spLoc, 1);
        grid.Children.Add(spLoc);

        // Col 2: Host & Ports
        var spHost = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        spHost.Children.Add(new TextBlock
        {
            Text = device.CleanHost,
            FontSize = 10.5,
            FontFamily = new FontFamily("Consolas, Segoe UI"),
            FontWeight = FontWeights.SemiBold,
            Foreground = TextDark
        });
        spHost.Children.Add(new TextBlock { Text = $"HTTP :{device.HttpPort} | RTSP :{device.RtspPort}", FontSize = 9.5, Foreground = TextSubtle });
        Grid.SetColumn(spHost, 2);
        grid.Children.Add(spHost);

        // Col 3: Channels
        var spCh = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        spCh.Children.Add(new TextBlock
        {
            Text = $"📹 {device.ChannelCount} Channels",
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            Foreground = TextDark
        });
        spCh.Children.Add(new TextBlock { Text = $"{device.PlaybackDays}d retention", FontSize = 9.5, Foreground = TextSubtle });
        Grid.SetColumn(spCh, 3);
        grid.Children.Add(spCh);

        // Col 4: Status Indicator
        var badge = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(8, 2, 8, 2),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center
        };
        badge.Child = new TextBlock
        {
            Text = "⚪ READY FOR AUDIT",
            FontSize = 9.5,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139))
        };
        Grid.SetColumn(badge, 4);
        grid.Children.Add(badge);

        // Col 5: Actions
        var actStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

        var btnTest = new Button
        {
            Content = "⚡ Test Hub",
            Height = 24,
            Padding = new Thickness(8, 0, 8, 0),
            FontSize = 10,
            FontWeight = FontWeights.Bold,
            Background = Brushes.White,
            Foreground = SvllBlue,
            BorderBrush = SvllBlue,
            BorderThickness = new Thickness(1),
            Cursor = Cursors.Hand,
            Margin = new Thickness(0, 0, 4, 0),
            ToolTip = $"Perform fast diagnostic probe on {device.Name} ({device.ChannelCount} channels)"
        };
        btnTest.Click += async (s, e) =>
        {
            await RunDiagnosticAuditAsync(new List<NvrDevice> { device });
        };
        actStack.Children.Add(btnTest);

        var btnWeb = new Button
        {
            Content = "🌐 Web",
            Height = 24,
            Padding = new Thickness(8, 0, 8, 0),
            FontSize = 10,
            Background = Brushes.White,
            Foreground = TextDark,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            Cursor = Cursors.Hand,
            Margin = new Thickness(0, 0, 4, 0)
        };
        btnWeb.Click += (s, e) =>
        {
            string url = NvrStreamHelper.BuildWebPortalUrl(device);
            try { Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true }); } catch { }
        };
        actStack.Children.Add(btnWeb);

        Grid.SetColumn(actStack, 5);
        grid.Children.Add(actStack);

        border.Child = grid;
        return border;
    }

    private void RenderDiagnosticResults(FleetDiagnosticReport report)
    {
        if (_diagMatrixContainer == null) return;
        _diagMatrixContainer.Children.Clear();
        _diagHubCards.Clear();

        var hubGroups = report.Items.GroupBy(i => i.NvrId).ToList();
        int faultyHubCount = 0;
        int healthyHubCount = 0;

        foreach (var group in hubGroups)
        {
            bool hasFault = group.Any(i => i.Status != CameraHealthStatus.Online);
            if (hasFault) faultyHubCount++;
            else healthyHubCount++;
        }

        // Render Filter Header
        var filterBar = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(10, 6, 10, 6)
        };
        var fGrid = new Grid();
        fGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        fGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var leftTabs = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

        Button? btnAll = null;
        Button? btnFaulty = null;
        Button? btnHealthy = null;

        void ApplyFilter(string mode)
        {
            _diagResultsFilter = mode;
            foreach (var hub in _diagHubCards)
            {
                if (mode == "FAULTY") hub.HubCard.Visibility = hub.HasFaults ? Visibility.Visible : Visibility.Collapsed;
                else if (mode == "HEALTHY") hub.HubCard.Visibility = !hub.HasFaults ? Visibility.Visible : Visibility.Collapsed;
                else hub.HubCard.Visibility = Visibility.Visible;
            }

            if (btnAll != null && btnFaulty != null && btnHealthy != null)
            {
                btnAll.Background = (mode == "ALL") ? SvllBlue : Brushes.White;
                btnAll.Foreground = (mode == "ALL") ? Brushes.White : TextDark;

                btnFaulty.Background = (mode == "FAULTY") ? SvllRed : Brushes.White;
                btnFaulty.Foreground = (mode == "FAULTY") ? Brushes.White : TextDark;

                btnHealthy.Background = (mode == "HEALTHY") ? new SolidColorBrush(Color.FromRgb(22, 101, 52)) : Brushes.White;
                btnHealthy.Foreground = (mode == "HEALTHY") ? Brushes.White : TextDark;
            }
        }

        btnAll = new Button
        {
            Content = $"🌐 All Hubs ({hubGroups.Count})",
            Height = 26,
            FontSize = 10.5,
            FontWeight = FontWeights.Bold,
            Background = SvllBlue,
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand,
            Padding = new Thickness(10, 0, 10, 0),
            Margin = new Thickness(0, 0, 4, 0)
        };
        btnAll.Click += (s, e) => ApplyFilter("ALL");
        leftTabs.Children.Add(btnAll);

        btnFaulty = new Button
        {
            Content = $"🔴 Faulty / Issues ({faultyHubCount})",
            Height = 26,
            FontSize = 10.5,
            FontWeight = FontWeights.Bold,
            Background = Brushes.White,
            Foreground = (faultyHubCount > 0) ? SvllRed : TextSubtle,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            Cursor = Cursors.Hand,
            Padding = new Thickness(10, 0, 10, 0),
            Margin = new Thickness(0, 0, 4, 0)
        };
        btnFaulty.Click += (s, e) => ApplyFilter("FAULTY");
        leftTabs.Children.Add(btnFaulty);

        btnHealthy = new Button
        {
            Content = $"🟢 100% Healthy ({healthyHubCount})",
            Height = 26,
            FontSize = 10.5,
            FontWeight = FontWeights.Bold,
            Background = Brushes.White,
            Foreground = TextDark,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            Cursor = Cursors.Hand,
            Padding = new Thickness(10, 0, 10, 0)
        };
        btnHealthy.Click += (s, e) => ApplyFilter("HEALTHY");
        leftTabs.Children.Add(btnHealthy);

        Grid.SetColumn(leftTabs, 0);
        fGrid.Children.Add(leftTabs);

        var rightTools = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        var btnExpandAll = new Button
        {
            Content = "👁 Expand All Channels",
            Height = 24,
            FontSize = 10,
            Background = Brushes.White,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(8, 0, 8, 0),
            Cursor = Cursors.Hand,
            Margin = new Thickness(0, 0, 4, 0)
        };
        btnExpandAll.Click += (s, e) =>
        {
            foreach (var h in _diagHubCards) h.ExpandChannels();
        };
        rightTools.Children.Add(btnExpandAll);

        var btnCollapseAll = new Button
        {
            Content = "📁 Collapse All",
            Height = 24,
            FontSize = 10,
            Background = Brushes.White,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(8, 0, 8, 0),
            Cursor = Cursors.Hand
        };
        btnCollapseAll.Click += (s, e) =>
        {
            foreach (var h in _diagHubCards) h.CollapseChannels();
        };
        rightTools.Children.Add(btnCollapseAll);

        Grid.SetColumn(rightTools, 1);
        fGrid.Children.Add(rightTools);

        filterBar.Child = fGrid;
        if (_diagMatrixHeaderHost != null) _diagMatrixHeaderHost.Child = filterBar;

        // Render Hub Result Cards
        foreach (var group in hubGroups)
        {
            var hubItems = group.ToList();
            var hubCard = BuildHubResultCard(hubItems);
            _diagMatrixContainer.Children.Add(hubCard.Card);
            _diagHubCards.Add((hubCard.Card, hubCard.HasFaults, hubCard.Expand, hubCard.Collapse));
        }

        ApplyFilter(_diagResultsFilter);
    }

    private (Border Card, bool HasFaults, Action Expand, Action Collapse) BuildHubResultCard(List<CameraDiagnosticItem> hubItems)
    {
        var first = hubItems[0];
        int total = hubItems.Count;
        int online = hubItems.Count(i => i.Status == CameraHealthStatus.Online);
        var faultyItems = hubItems.Where(i => i.Status != CameraHealthStatus.Online).ToList();
        bool hasFaults = faultyItems.Count > 0;

        var cardBorder = new Border
        {
            Background = Brushes.White,
            BorderBrush = hasFaults ? new SolidColorBrush(Color.FromRgb(248, 113, 113)) : BorderMuted,
            BorderThickness = new Thickness(hasFaults ? 1.5 : 1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12, 10, 12, 10),
            Margin = new Thickness(0, 0, 0, 8)
        };

        var rootStack = new StackPanel();

        // Hub Header Row
        var headGrid = new Grid();
        headGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Status Badge
        headGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Hub Info
        headGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Actions

        // Status Badge
        var badge = new Border
        {
            Background = hasFaults
                ? new SolidColorBrush(Color.FromRgb(254, 242, 242))
                : new SolidColorBrush(Color.FromRgb(240, 253, 244)),
            BorderBrush = hasFaults
                ? new SolidColorBrush(Color.FromRgb(239, 68, 68))
                : new SolidColorBrush(Color.FromRgb(34, 197, 94)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(10, 3, 10, 3),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 10, 0)
        };
        badge.Child = new TextBlock
        {
            Text = hasFaults ? $"🔴 {faultyItems.Count} FAULTY ({online}/{total})" : $"🟢 100% ONLINE ({total}/{total})",
            FontSize = 10,
            FontWeight = FontWeights.Bold,
            Foreground = hasFaults ? SvllRed : new SolidColorBrush(Color.FromRgb(22, 101, 52))
        };
        Grid.SetColumn(badge, 0);
        headGrid.Children.Add(badge);

        // Hub Info
        var infoStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        var titleRow = new StackPanel { Orientation = Orientation.Horizontal };
        titleRow.Children.Add(new TextBlock
        {
            Text = "🏢 " + first.NvrName,
            FontSize = 12,
            FontWeight = FontWeights.Bold,
            Foreground = TextDark
        });
        titleRow.Children.Add(new TextBlock
        {
            Text = $"  •  📍 {first.Location}" + (string.IsNullOrWhiteSpace(first.SubLocation) ? "" : $" • {first.SubLocation}"),
            FontSize = 10.5,
            Foreground = SvllBlue,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        });
        infoStack.Children.Add(titleRow);

        infoStack.Children.Add(new TextBlock
        {
            Text = $"{first.Host}:{first.HttpPort} | RTSP :{first.RtspPort} • {first.BrandDisplayName} • Latency: {(first.LatencyMs >= 0 ? $"{first.LatencyMs}ms" : "Timeout")}",
            FontSize = 9.5,
            Foreground = TextSubtle,
            Margin = new Thickness(0, 1, 0, 0)
        });
        Grid.SetColumn(infoStack, 1);
        headGrid.Children.Add(infoStack);

        // Quick Actions
        var actStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

        var btnReAudit = new Button
        {
            Content = "⚡ Re-Audit Hub",
            Height = 24,
            Padding = new Thickness(8, 0, 8, 0),
            FontSize = 10,
            FontWeight = FontWeights.Bold,
            Background = Brushes.White,
            Foreground = SvllBlue,
            BorderBrush = SvllBlue,
            BorderThickness = new Thickness(1),
            Cursor = Cursors.Hand,
            Margin = new Thickness(0, 0, 4, 0)
        };
        btnReAudit.Click += async (s, e) =>
        {
            var dev = NvrDeviceStorage.Instance.GetDevices().FirstOrDefault(d => d.Id == first.NvrId);
            if (dev != null) await RunDiagnosticAuditAsync(new List<NvrDevice> { dev });
        };
        actStack.Children.Add(btnReAudit);

        var btnWeb = new Button
        {
            Content = "🌐 Web",
            Height = 24,
            Padding = new Thickness(8, 0, 8, 0),
            FontSize = 10,
            Background = Brushes.White,
            Foreground = TextDark,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            Cursor = Cursors.Hand
        };
        btnWeb.Click += (s, e) =>
        {
            var dev = NvrDeviceStorage.Instance.GetDevices().FirstOrDefault(d => d.Id == first.NvrId);
            if (dev != null)
            {
                string url = NvrStreamHelper.BuildWebPortalUrl(dev);
                try { Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true }); } catch { }
            }
        };
        actStack.Children.Add(btnWeb);

        Grid.SetColumn(actStack, 2);
        headGrid.Children.Add(actStack);
        rootStack.Children.Add(headGrid);

        // Inline Alert Drawer for Faulty Channels
        if (hasFaults)
        {
            var faultBox = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(254, 242, 242)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(254, 202, 202)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10, 6, 10, 6),
                Margin = new Thickness(0, 8, 0, 4)
            };
            var fStack = new StackPanel();
            fStack.Children.Add(new TextBlock
            {
                Text = $"⚠️ ATTENTION REQUIRED: {faultyItems.Count} FAULTY CAMERA CHANNEL{(faultyItems.Count > 1 ? "S" : "")} DETECTED",
                FontSize = 10.5,
                FontWeight = FontWeights.Bold,
                Foreground = SvllRed,
                Margin = new Thickness(0, 0, 0, 4)
            });

            foreach (var fi in faultyItems)
            {
                var fRow = new Grid { Margin = new Thickness(0, 2, 0, 2) };
                fRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
                fRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                fRow.Children.Add(new TextBlock
                {
                    Text = $"• CH {fi.ChannelNumber:D2}: {fi.ChannelName}",
                    FontSize = 10,
                    FontWeight = FontWeights.Bold,
                    Foreground = TextDark
                });

                var errText = new TextBlock
                {
                    Text = $"{fi.ErrorDetails} ➔ {fi.CorrectiveAction}",
                    FontSize = 9.5,
                    Foreground = SvllRed,
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                Grid.SetColumn(errText, 1);
                fRow.Children.Add(errText);

                fStack.Children.Add(fRow);
            }
            faultBox.Child = fStack;
            rootStack.Children.Add(faultBox);
        }

        // Lazy Expandable Channels Drawer
        var chExpander = new Expander
        {
            Header = $"📹 View All {total} Channels (Detailed SLA Breakdown)",
            FontSize = 10.5,
            Foreground = TextSubtle,
            IsExpanded = false,
            Margin = new Thickness(0, 6, 0, 0)
        };

        var chListContainer = new StackPanel { Margin = new Thickness(0, 4, 0, 0) };

        void PopulateChannels()
        {
            if (chListContainer.Children.Count > 0) return;

            // Mini Matrix Header for Channels
            var chHeadGrid = new Grid
            {
                Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
                Margin = new Thickness(0, 2, 0, 4)
            };
            chHeadGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) }); // Status
            chHeadGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) }); // NVR Hub
            chHeadGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });  // Ch #
            chHeadGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(190) }); // Camera Name
            chHeadGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(170) }); // Host & Port
            chHeadGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(85) });  // Latency
            chHeadGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) }); // Probes
            chHeadGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Actions
            chHeadGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(190) }); // Tools

            chHeadGrid.Children.Add(CreateMatrixHeadLabel("STATUS", 0));
            chHeadGrid.Children.Add(CreateMatrixHeadLabel("NVR HUB", 1));
            chHeadGrid.Children.Add(CreateMatrixHeadLabel("CH #", 2));
            chHeadGrid.Children.Add(CreateMatrixHeadLabel("CAMERA IDENTIFIER", 3));
            chHeadGrid.Children.Add(CreateMatrixHeadLabel("IP HOST : PORTS", 4));
            chHeadGrid.Children.Add(CreateMatrixHeadLabel("LATENCY", 5));
            chHeadGrid.Children.Add(CreateMatrixHeadLabel("PROBES", 6));
            chHeadGrid.Children.Add(CreateMatrixHeadLabel("DIAGNOSIS & ACTIONS", 7));
            chHeadGrid.Children.Add(CreateMatrixHeadLabel("TOOLS", 8));

            chListContainer.Children.Add(chHeadGrid);

            foreach (var item in hubItems)
            {
                chListContainer.Children.Add(BuildDiagnosticRowUi(item));
            }
        }

        chExpander.Expanded += (s, e) => PopulateChannels();
        chExpander.Content = chListContainer;
        rootStack.Children.Add(chExpander);

        cardBorder.Child = rootStack;
        return (cardBorder, hasFaults, () => { chExpander.IsExpanded = true; PopulateChannels(); }, () => { chExpander.IsExpanded = false; });
    }

    private void PopulateDiagTargetCombo()
    {
        if (_cmbDiagTargetNvr == null) return;
        _cmbDiagTargetNvr.Items.Clear();

        _cmbDiagTargetNvr.Items.Add(new ComboBoxItem
        {
            Content = "🌐 Full Fleet (All 46 NVRs)",
            Tag = "ALL",
            IsSelected = true
        });

        var devices = NvrDeviceStorage.Instance.GetDevices();

        // Add campus / location groups
        var locations = devices.Select(d => d.Location).Where(l => !string.IsNullOrEmpty(l)).Distinct().OrderBy(l => l);
        foreach (var loc in locations)
        {
            int count = devices.Count(d => d.Location == loc);
            _cmbDiagTargetNvr.Items.Add(new ComboBoxItem
            {
                Content = $"🏢 {loc} Hubs ({count} NVRs)",
                Tag = $"LOC:{loc}"
            });
        }

        // Add individual NVRs
        foreach (var d in devices)
        {
            _cmbDiagTargetNvr.Items.Add(new ComboBoxItem
            {
                Content = $"📹 {d.Name} ({d.CleanHost})",
                Tag = $"NVR:{d.Id}"
            });
        }
    }

    private async Task RunSelectedTargetAuditAsync()
    {
        var item = _cmbDiagTargetNvr.SelectedItem as ComboBoxItem;
        string tag = item?.Tag as string ?? "ALL";
        List<NvrDevice>? targets = null;
        var allDevices = NvrDeviceStorage.Instance.GetDevices();

        if (tag.StartsWith("NVR:"))
        {
            string id = tag.Substring(4);
            targets = allDevices.Where(d => d.Id == id).ToList();
        }
        else if (tag.StartsWith("LOC:"))
        {
            string loc = tag.Substring(4);
            targets = allDevices.Where(d => d.Location == loc).ToList();
        }

        await RunDiagnosticAuditAsync(targets);
    }

    private async Task RunDiagnosticAuditAsync(List<NvrDevice>? targetDevices = null)
    {
        _btnRunFleetAudit.IsEnabled = false;
        if (_btnRunTargetAudit != null) _btnRunTargetAudit.IsEnabled = false;
        _btnCancelFleetAudit.Visibility = Visibility.Visible;
        _pbDiagAudit.Value = 0;

        _diagCts?.Cancel();
        _diagCts = new CancellationTokenSource();

        var progress = new Progress<(int current, int total, string message)>(p => Dispatcher.Invoke(() =>
        {
            _lblDiagProgress.Text = $"[{p.current}/{p.total}] {p.message}";
            if (p.total > 0) _pbDiagAudit.Value = (double)p.current / p.total * 100.0;
        }));

        try
        {
            var report = await CctvDiagnosticService.AuditDevicesAsync(targetDevices, progress, _diagCts.Token);
            _latestDiagnosticReport = report;

            // Update KPI Banner
            _lblDiagOnlinePercent.Text = $"{report.AvailabilityPercent}%";
            _lblDiagOnlineRatio.Text = $"{report.OnlineCount} Online / {report.TotalChannels} Total";
            _lblDiagTotalNvrs.Text = $"{report.TotalNvrs} NVRs";
            _lblDiagTotalChannels.Text = $"{report.TotalChannels} Channels";
            _lblDiagAvgLatency.Text = $"{report.AverageLatencyMs} ms";
            _lblDiagHealthGrade.Text = report.HealthGrade;

            if (report.AvailabilityPercent >= 90)
            {
                _diagGradeBadge.Background = new SolidColorBrush(Color.FromRgb(220, 252, 231));
                _diagGradeBadge.BorderBrush = new SolidColorBrush(Color.FromRgb(34, 197, 94));
                _lblDiagHealthGrade.Foreground = new SolidColorBrush(Color.FromRgb(22, 101, 52));
            }
            else
            {
                _diagGradeBadge.Background = new SolidColorBrush(Color.FromRgb(254, 242, 242));
                _diagGradeBadge.BorderBrush = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                _lblDiagHealthGrade.Foreground = SvllRed;
            }

            // Render Results Grouped by NVR Hub with Fast Expandable Drawers (Zero Freeze)
            RenderDiagnosticResults(report);

            _lblDiagProgress.Text = $"Audit complete at {report.GeneratedAt:HH:mm:ss}. Status: {report.OnlineCount} Online, {report.DegradedCount} Degraded, {report.OfflineCount} Offline, {report.AuthFailedCount} Auth Faults.";
            SystemSounds.Asterisk.Play();
        }
        catch (OperationCanceledException)
        {
            _lblDiagProgress.Text = "Diagnostic audit cancelled by operator.";
        }
        catch (Exception ex)
        {
            _lblDiagProgress.Text = $"Audit error: {ex.Message}";
        }
        finally
        {
            _btnRunFleetAudit.IsEnabled = true;
            if (_btnRunTargetAudit != null) _btnRunTargetAudit.IsEnabled = true;
            _btnCancelFleetAudit.Visibility = Visibility.Collapsed;
        }
    }

    private Border BuildDiagnosticRowUi(CameraDiagnosticItem item)
    {
        var rowBorder = new Border
        {
            Background = Brushes.White,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(0, 6, 0, 6)
        };

        var rowGrid = new Grid();
        rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) }); // Status
        rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) }); // NVR Hub
        rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });  // Ch #
        rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(190) }); // Camera Name
        rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(170) }); // Host & Port
        rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(85) });  // Latency
        rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) }); // Probes
        rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Diagnostics
        rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(190) }); // Tools

        // Col 0: Status Badge
        var badge = new Border
        {
            Background = (SolidColorBrush)new BrushConverter().ConvertFromString(item.StatusColorHex + "22")!,
            BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFromString(item.StatusColorHex)!,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(8, 2, 8, 2),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(6, 0, 0, 0)
        };
        badge.Child = new TextBlock
        {
            Text = item.StatusBadgeText,
            FontSize = 9.5,
            FontWeight = FontWeights.Bold,
            Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString(item.StatusColorHex)!
        };
        Grid.SetColumn(badge, 0);
        rowGrid.Children.Add(badge);

        // Col 1: NVR Hub & Location
        var spHub = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(6, 0, 6, 0) };
        spHub.Children.Add(new TextBlock { Text = item.NvrName, FontSize = 11, FontWeight = FontWeights.Bold, Foreground = TextDark, TextTrimming = TextTrimming.CharacterEllipsis });
        string subLocText = string.IsNullOrWhiteSpace(item.SubLocation) ? item.Location : $"{item.Location} • {item.SubLocation}";
        spHub.Children.Add(new TextBlock { Text = subLocText, FontSize = 9.5, Foreground = TextSubtle, TextTrimming = TextTrimming.CharacterEllipsis });
        Grid.SetColumn(spHub, 1);
        rowGrid.Children.Add(spHub);

        // Col 2: Channel Number
        var lblCh = new TextBlock
        {
            Text = $"CH {item.ChannelNumber:D2}",
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            Foreground = SvllBlue,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(6, 0, 6, 0)
        };
        Grid.SetColumn(lblCh, 2);
        rowGrid.Children.Add(lblCh);

        // Col 3: Camera Identifier & Owner
        var spCam = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(6, 0, 6, 0) };
        spCam.Children.Add(new TextBlock { Text = item.ChannelName, FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = TextDark, TextTrimming = TextTrimming.CharacterEllipsis });
        if (!string.IsNullOrWhiteSpace(item.OwnerName))
        {
            spCam.Children.Add(new TextBlock { Text = $"👤 {item.OwnerName} ({item.OwnerContact})", FontSize = 9.5, Foreground = SvllBlue, TextTrimming = TextTrimming.CharacterEllipsis });
        }
        Grid.SetColumn(spCam, 3);
        rowGrid.Children.Add(spCam);

        // Col 4: Host & Ports
        var spHost = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(6, 0, 6, 0) };
        spHost.Children.Add(new TextBlock { Text = item.Host, FontSize = 10.5, FontFamily = new FontFamily("Consolas, Segoe UI"), FontWeight = FontWeights.SemiBold, Foreground = TextDark });
        spHost.Children.Add(new TextBlock { Text = $"HTTP :{item.HttpPort} | RTSP :{item.RtspPort}", FontSize = 9.5, Foreground = TextSubtle });
        Grid.SetColumn(spHost, 4);
        rowGrid.Children.Add(spHost);

        // Col 5: Latency
        var lblLatency = new TextBlock
        {
            Text = item.LatencyMs >= 0 ? $"{item.LatencyMs} ms" : "--",
            FontSize = 11,
            FontFamily = new FontFamily("Consolas, Segoe UI"),
            FontWeight = FontWeights.Bold,
            Foreground = (item.LatencyMs < 60 && item.LatencyMs >= 0) ? new SolidColorBrush(Color.FromRgb(22, 101, 52)) : (item.LatencyMs > 150 ? SvllRed : new SolidColorBrush(Color.FromRgb(217, 119, 6))),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(6, 0, 6, 0)
        };
        Grid.SetColumn(lblLatency, 5);
        rowGrid.Children.Add(lblLatency);

        // Col 6: Probes (RTSP & Snapshot)
        var spProbes = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(6, 0, 6, 0) };
        spProbes.Children.Add(new TextBlock
        {
            Text = item.RtspPortOpen ? "🟢 RTSP :554 Open" : "🔴 RTSP Closed",
            FontSize = 9.5,
            FontWeight = FontWeights.SemiBold,
            Foreground = item.RtspPortOpen ? new SolidColorBrush(Color.FromRgb(22, 101, 52)) : SvllRed
        });
        spProbes.Children.Add(new TextBlock
        {
            Text = item.AuthSuccess ? $"🟢 Snap OK ({item.SnapshotBytes / 1024} KB)" : "🔴 Snap Probe Failed",
            FontSize = 9.5,
            Foreground = item.AuthSuccess ? new SolidColorBrush(Color.FromRgb(22, 101, 52)) : SvllRed
        });
        Grid.SetColumn(spProbes, 6);
        rowGrid.Children.Add(spProbes);

        // Col 7: Diagnostics & Actionable Advice
        var spDiag = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(6, 0, 6, 0) };
        spDiag.Children.Add(new TextBlock { Text = item.ErrorDetails, FontSize = 10, Foreground = TextDark, TextTrimming = TextTrimming.CharacterEllipsis });
        spDiag.Children.Add(new TextBlock { Text = item.CorrectiveAction, FontSize = 9.5, Foreground = SvllBlue, TextTrimming = TextTrimming.CharacterEllipsis });
        Grid.SetColumn(spDiag, 7);
        rowGrid.Children.Add(spDiag);

        // Col 8: Action Tools
        var spTools = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(6, 0, 6, 0) };

        var btnSingleTest = CreateMatrixActionButton("⚡ Test", async () =>
        {
            var dev = NvrDeviceStorage.Instance.GetDeviceById(item.NvrId);
            if (dev != null)
            {
                var singleRes = await CctvDiagnosticService.AuditSingleCameraAsync(dev, item.ChannelNumber);
                MessageBox.Show($"Diagnostic Result for {singleRes.ChannelName} ({singleRes.Host}):\n\nStatus: {singleRes.StatusBadgeText}\nPing: {singleRes.LatencyMs}ms\nRTSP Port: {(singleRes.RtspPortOpen ? "Open" : "Closed")}\nSnapshot Probe: {(singleRes.AuthSuccess ? $"OK ({singleRes.SnapshotBytes / 1024} KB)" : "Failed")}\n\nDetails:\n{singleRes.ErrorDetails}\n\nCorrective Action:\n{singleRes.CorrectiveAction}",
                    $"Diagnostic: {singleRes.ChannelName}", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        });
        spTools.Children.Add(btnSingleTest);

        var btnSnap = CreateMatrixActionButton("📸 Snap", async () =>
        {
            var dev = NvrDeviceStorage.Instance.GetDeviceById(item.NvrId);
            if (dev != null)
            {
                try
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(4));
                    var bytes = await NvrStreamHelper.FetchSnapshotBytesAsync(dev, item.ChannelNumber, cts.Token);
                    if (bytes != null && bytes.Length > 200)
                    {
                        ShowSnapshotPreviewDialog(dev, item.ChannelNumber, bytes);
                    }
                    else
                    {
                        MessageBox.Show($"Unable to retrieve snapshot from {dev.Name} (Ch {item.ChannelNumber}).\nVerify NVR IP and credentials.", "Snapshot Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Snapshot error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        });
        spTools.Children.Add(btnSnap);

        var btnVlc = CreateMatrixActionButton("▶ Stream", () =>
        {
            var dev = NvrDeviceStorage.Instance.GetDeviceById(item.NvrId);
            if (dev != null)
            {
                string rtspUrl = NvrStreamHelper.BuildRtspUrl(dev, item.ChannelNumber, StreamQuality.SubStream);
                NvrStreamHelper.LaunchInVlc(rtspUrl, out string msg);
                Log($"[CCTV Diagnostic] Stream launch: {msg}");
            }
        });
        spTools.Children.Add(btnVlc);

        var btnWeb = CreateMatrixActionButton("🌐 Web", () =>
        {
            var dev = NvrDeviceStorage.Instance.GetDeviceById(item.NvrId);
            if (dev != null)
            {
                string url = NvrStreamHelper.BuildWebPortalUrl(dev);
                try { Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true }); } catch { }
            }
        });
        spTools.Children.Add(btnWeb);

        Grid.SetColumn(spTools, 8);
        rowGrid.Children.Add(spTools);

        rowBorder.Child = rowGrid;
        return rowBorder;
    }

    private Button CreateMatrixActionButton(string text, Action action)
    {
        var btn = new Button
        {
            Content = text,
            Height = 22,
            Padding = new Thickness(6, 0, 6, 0),
            FontSize = 9.5,
            FontWeight = FontWeights.SemiBold,
            Background = Brushes.White,
            Foreground = TextDark,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            Margin = new Thickness(0, 0, 4, 0),
            Cursor = Cursors.Hand
        };
        btn.Click += (s, e) => action();
        return btn;
    }

    private void ShowSnapshotPreviewDialog(NvrDevice dev, int ch, byte[] imgBytes)
    {
        var win = new Window
        {
            Title = $"Snapshot Probe: {dev.Name} (Ch {ch:D2}) — {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
            Width = 840,
            Height = 560,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this,
            Background = Brushes.Black
        };

        var bmp = new BitmapImage();
        using (var ms = new MemoryStream(imgBytes))
        {
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.StreamSource = ms;
            bmp.EndInit();
        }
        bmp.Freeze();

        var img = new Image { Source = bmp, Stretch = Stretch.Uniform };
        win.Content = img;
        win.ShowDialog();
    }

    // ─── 5. Report Export Engine (HTML Print / CSV / Markdown) ───────────────
    private async void ExportHtmlReport()
    {
        if (_latestDiagnosticReport == null || _latestDiagnosticReport.Items.Count == 0)
        {
            var res = MessageBox.Show(
                "No fleet diagnostic audit has been performed yet.\n\nWould you like to run a quick Full Fleet Audit now to generate the report?",
                "Run Diagnostic Audit",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (res == MessageBoxResult.Yes)
            {
                await RunDiagnosticAuditAsync(null);
                if (_latestDiagnosticReport != null && _latestDiagnosticReport.Items.Count > 0)
                {
                    ExportHtmlReport();
                }
            }
            return;
        }

        try
        {
            string html = CctvDiagnosticService.GenerateHtmlReport(_latestDiagnosticReport);
            string tempFile = Path.Combine(Path.GetTempPath(), $"SVLL_Surveillance_Health_Audit_{DateTime.Now:yyyyMMdd_HHmmss}.html");
            File.WriteAllText(tempFile, html, Encoding.UTF8);

            Process.Start(new ProcessStartInfo
            {
                FileName = tempFile,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to export HTML report: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void ExportCsvReport()
    {
        if (_latestDiagnosticReport == null || _latestDiagnosticReport.Items.Count == 0)
        {
            var res = MessageBox.Show(
                "No fleet diagnostic audit has been performed yet.\n\nWould you like to run a quick Full Fleet Audit now to export the CSV matrix?",
                "Run Diagnostic Audit",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (res == MessageBoxResult.Yes)
            {
                await RunDiagnosticAuditAsync(null);
                if (_latestDiagnosticReport != null && _latestDiagnosticReport.Items.Count > 0)
                {
                    ExportCsvReport();
                }
            }
            return;
        }

        try
        {
            var sfd = new SaveFileDialog
            {
                Title = "Export CCTV Surveillance Diagnostic Matrix",
                Filter = "CSV Spreadsheet (*.csv)|*.csv",
                FileName = $"SVLL_CCTV_Surveillance_Audit_{DateTime.Now:yyyyMMdd}.csv"
            };

            if (sfd.ShowDialog() == true)
            {
                string csv = CctvDiagnosticService.GenerateCsvReport(_latestDiagnosticReport);
                File.WriteAllText(sfd.FileName, csv, Encoding.UTF8);
                MessageBox.Show($"Surveillance matrix exported successfully!\nSaved to: {sfd.FileName}", "Export Completed", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to export CSV: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void CopyMarkdownReport()
    {
        if (_latestDiagnosticReport == null || _latestDiagnosticReport.Items.Count == 0)
        {
            var res = MessageBox.Show(
                "No fleet diagnostic audit has been performed yet.\n\nWould you like to run a quick Full Fleet Audit now to copy the report?",
                "Run Diagnostic Audit",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (res == MessageBoxResult.Yes)
            {
                await RunDiagnosticAuditAsync(null);
                if (_latestDiagnosticReport != null && _latestDiagnosticReport.Items.Count > 0)
                {
                    CopyMarkdownReport();
                }
            }
            return;
        }

        try
        {
            string md = CctvDiagnosticService.GenerateMarkdownReport(_latestDiagnosticReport);
            Clipboard.SetText(md);
            MessageBox.Show("Surveillance health check summary copied to clipboard in Markdown format!\nReady to paste into ticketing, Jira, or email.", "Report Copied", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to copy report: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RefreshDiagnosticViewIfVisible()
    {
        if (_cctvCurrentMode == "DIAGNOSTIC")
        {
            PopulateDiagTargetCombo();
            if (_latestDiagnosticReport != null)
            {
                RenderDiagnosticResults(_latestDiagnosticReport);
            }
            else
            {
                PopulateInitialDiagnosticMatrix();
            }
        }
    }
}

#endif
