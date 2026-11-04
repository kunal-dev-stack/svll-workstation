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
    private Button _btnCancelFleetAudit = null!;
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

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Left Diagnostic Actions
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Space
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Right Report Exports

        // Left Actions
        var leftStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

        _btnRunFleetAudit = new Button
        {
            Content = "⚡ Run Full Fleet Audit",
            Height = 28,
            Padding = new Thickness(14, 0, 14, 0),
            FontSize = 11.5,
            FontWeight = FontWeights.Bold,
            Background = SvllBlue,
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand,
            ToolTip = "Concurrently tests Ping, Ports (HTTP/RTSP), Snapshots, and Stream handshakes across all NVRs"
        };
        _btnRunFleetAudit.Click += async (s, e) => await RunFullFleetAuditWithUiAsync();
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

        Grid.SetColumn(leftStack, 0);
        grid.Children.Add(leftStack);

        // Right Actions: Report Generation & Export Engine
        var rightStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

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

        Grid.SetColumn(rightStack, 2);
        grid.Children.Add(rightStack);

        bar.Child = grid;
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
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header row
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Scroll container

        // Matrix Header
        var headGrid = new Grid
        {
            Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
            Margin = new Thickness(0, 0, 0, 6)
        };
        headGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) }); // Status
        headGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) }); // NVR Hub
        headGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });  // Ch #
        headGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(190) }); // Camera Name
        headGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(170) }); // Host & Port
        headGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(85) });  // Latency
        headGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) }); // Probes
        headGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Actions & Troubleshooting
        headGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(190) }); // Tools

        headGrid.Children.Add(CreateMatrixHeadLabel("STATUS", 0));
        headGrid.Children.Add(CreateMatrixHeadLabel("NVR HUB & LOCATION", 1));
        headGrid.Children.Add(CreateMatrixHeadLabel("CH #", 2));
        headGrid.Children.Add(CreateMatrixHeadLabel("CAMERA IDENTIFIER", 3));
        headGrid.Children.Add(CreateMatrixHeadLabel("IP HOST : PORTS", 4));
        headGrid.Children.Add(CreateMatrixHeadLabel("LATENCY", 5));
        headGrid.Children.Add(CreateMatrixHeadLabel("PROBES (RTSP / SNAP)", 6));
        headGrid.Children.Add(CreateMatrixHeadLabel("DIAGNOSIS & ACTIONS", 7));
        headGrid.Children.Add(CreateMatrixHeadLabel("QUICK TOOLS", 8));

        Grid.SetRow(headGrid, 0);
        grid.Children.Add(headGrid);

        // Scroll Viewer for Channel Cards
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

        // Populate initial placeholder rows
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

        var devices = NvrDeviceStorage.Instance.GetDevices();
        if (devices.Count == 0)
        {
            _diagMatrixContainer.Children.Add(new TextBlock
            {
                Text = "No NVR/DVR hubs registered. Add an NVR above to run diagnostics.",
                FontSize = 12,
                Foreground = TextSubtle,
                Margin = new Thickness(12)
            });
            return;
        }

        foreach (var d in devices)
        {
            for (int ch = 1; ch <= d.ChannelCount; ch++)
            {
                var placeholderItem = new CameraDiagnosticItem
                {
                    NvrId = d.Id,
                    NvrName = d.Name,
                    Location = d.Location,
                    SubLocation = d.SubLocation,
                    OwnerName = d.OwnerName,
                    OwnerContact = d.OwnerContact,
                    PlaybackDays = d.PlaybackDays,
                    Host = d.CleanHost,
                    BrandDisplayName = d.GetBrandDisplayName(),
                    ChannelNumber = ch,
                    ChannelName = d.GetChannelName(ch),
                    HttpPort = d.HttpPort,
                    RtspPort = d.RtspPort,
                    Status = CameraHealthStatus.Checking,
                    ErrorDetails = "Diagnostic test pending. Click '⚡ Run Full Fleet Audit' above.",
                    CorrectiveAction = "Probes ready."
                };
                _diagMatrixContainer.Children.Add(BuildDiagnosticRowUi(placeholderItem));
            }
        }
    }

    private async Task RunFullFleetAuditWithUiAsync()
    {
        _btnRunFleetAudit.IsEnabled = false;
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
            var report = await CctvDiagnosticService.AuditAllDevicesAsync(progress, _diagCts.Token);
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

            // Populate rows
            _diagMatrixContainer.Children.Clear();
            foreach (var item in report.Items)
            {
                _diagMatrixContainer.Children.Add(BuildDiagnosticRowUi(item));
            }

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
    private void ExportHtmlReport()
    {
        if (_latestDiagnosticReport == null || _latestDiagnosticReport.Items.Count == 0)
        {
            MessageBox.Show("Please run '⚡ Run Full Fleet Audit' first to generate the latest diagnostic findings before exporting.", "Audit Required", MessageBoxButton.OK, MessageBoxImage.Information);
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

    private void ExportCsvReport()
    {
        if (_latestDiagnosticReport == null || _latestDiagnosticReport.Items.Count == 0)
        {
            MessageBox.Show("Please run '⚡ Run Full Fleet Audit' first before exporting.", "Audit Required", MessageBoxButton.OK, MessageBoxImage.Information);
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

    private void CopyMarkdownReport()
    {
        if (_latestDiagnosticReport == null || _latestDiagnosticReport.Items.Count == 0)
        {
            MessageBox.Show("Please run '⚡ Run Full Fleet Audit' first before copying report.", "Audit Required", MessageBoxButton.OK, MessageBoxImage.Information);
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
            PopulateInitialDiagnosticMatrix();
        }
    }
}

#endif
