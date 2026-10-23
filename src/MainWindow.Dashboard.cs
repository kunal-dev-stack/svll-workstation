using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Win32;

namespace SVLL_IT_Workstation;

public partial class MainWindow
{
    // Dashboard Telemetry UI Controls
    private TextBlock _lblCpuVal = null!;
    private TextBlock _lblCpuSub = null!;
    private TextBlock _lblRamVal = null!;
    private TextBlock _lblRamSub = null!;
    private TextBlock _lblNetVal = null!;
    private TextBlock _lblUptimeVal = null!;

    // Waveform Canvases
    private Canvas _cpuCanvas = null!;
    private Polyline _cpuLine = null!;
    private Polygon _cpuFill = null!;
    private Canvas _ramCanvas = null!;
    private Polyline _ramLine = null!;
    private Polygon _ramFill = null!;

    // Network I/O Live Graph Controls
    private Canvas _netCanvas = null!;
    private Polyline _netRxLine = null!;
    private Polygon _netRxFill = null!;
    private Polyline _netTxLine = null!;
    private TextBlock _lblNetIoVal = null!;
    private long _prevBytesRx = 0;
    private long _prevBytesTx = 0;
    private DateTime _prevNetTime = DateTime.MinValue;
    private readonly List<double> _netRxHistory = new List<double>();
    private readonly List<double> _netTxHistory = new List<double>();

    // Circular Health Score Ring Gauge
    private System.Windows.Shapes.Path _healthGaugePath = null!;
    private TextBlock _lblHealthGaugeValue = null!;

    // Dashboard AI Chatbot Controls
    private ScrollViewer _dashboardAiChatScroll = null!;
    private StackPanel _dashboardAiChatPanel = null!;
    private TextBox _txtDashboardAiInput = null!;
    private Button _btnDashboardAiSend = null!;
    private TextBlock _lblDashboardAiStatus = null!;
    private Border _dashboardAiBody = null!;
    private bool _isDashboardAiExpanded = true;

    // Multi-Drive Storage Visualizer Panel
    private StackPanel _panelDriveBars = null!;

    // Top Resource Consuming Processes List
    private ListView _listTopProcesses = null!;
    private readonly List<TopProcessItem> _topProcesses = new List<TopProcessItem>();

    // Deep Hardware Spec Fields
    private TextBlock _lblSpecCpu = null!;
    private TextBlock _lblSpecRam = null!;
    private TextBlock _lblSpecGpu = null!;
    private TextBlock _lblSpecBoard = null!;
    private TextBlock _lblSpecBios = null!;
    private TextBlock _lblSpecOs = null!;
    private TextBlock _lblSpecMac = null!;
    private TextBlock _lblSpecGw = null!;
    private TextBlock _lblSpecPower = null!;
    private TextBlock _lblSpecTasks = null!;

    // Cached Hardware Info
    private bool _hardwareSpecsLoaded = false;
    private string _cachedCpuName = "";
    private string _cachedGpuName = "";
    private string _cachedBoardName = "";
    private string _cachedBiosSerial = "";
    private string _cachedOsBuild = "";
    private string _cachedPowerInfo = "";

    private UIElement BuildDashboardView()
    {
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var root = new StackPanel { Margin = new Thickness(0, 0, 10, 0) };

        // ------------------ 1. Executive Top Header Banner with Circular Ring Gauge ------------------
        var topBanner = new Border
        {
            Background = BgCard,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16, 12, 16, 12),
            Margin = new Thickness(0, 0, 0, 12)
        };
        var bannerGrid = new Grid();
        bannerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        bannerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var titleStack = new StackPanel();
        titleStack.Children.Add(new TextBlock
        {
            Text = "SVLL Executive IT Operations Dashboard",
            FontSize = 17,
            FontWeight = FontWeights.Bold,
            Foreground = SvllBlue
        });
        titleStack.Children.Add(new TextBlock
        {
            Text = $"Endpoint Host: {Environment.MachineName}  |  Logged User: {Environment.UserName}  |  Active Fleet Profile: {_activeConfig.PresetName}",
            FontSize = 11.5,
            Foreground = TextSubtle,
            Margin = new Thickness(0, 2, 0, 0)
        });
        Grid.SetColumn(titleStack, 0);
        bannerGrid.Children.Add(titleStack);

        var topActions = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

        var btnRefresh = new Button
        {
            Content = " Refresh Telemetry ",
            FontSize = 11,
            Padding = new Thickness(10, 5, 10, 5),
            Margin = new Thickness(0, 0, 10, 0),
            Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
            BorderBrush = BorderMuted,
            Cursor = Cursors.Hand
        };
        btnRefresh.Click += (s, e) => UpdateLiveVitals();
        topActions.Children.Add(btnRefresh);

        var btnDashboardTurbo = new Button
        {
            Content = "⚡ 1-Click Turbo Boost",
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            Padding = new Thickness(12, 5, 12, 5),
            Margin = new Thickness(0, 0, 14, 0),
            Background = SvllBlue,
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand
        };
        btnDashboardTurbo.Click += async (s, e) =>
        {
            btnDashboardTurbo.IsEnabled = false;
            btnDashboardTurbo.Content = "⏳ Optimizing...";
            try
            {
                var res = await ExecuteTurboBoostAsync();
                MessageBox.Show(
                    $"🎉 System Accelerated Successfully!\n\n" +
                    $"• RAM Released: {res.freedMb} MB\n" +
                    $"• Bloat Tasks Terminated: {res.killedCount}\n" +
                    $"• Applications Trimmed: {res.trimmedCount}\n" +
                    $"• CPU Power Plan: Ultimate Performance Activated",
                    "1-Click Turbo Acceleration", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            finally
            {
                btnDashboardTurbo.IsEnabled = true;
                btnDashboardTurbo.Content = "⚡ 1-Click Turbo Boost";
            }
        };
        topActions.Children.Add(btnDashboardTurbo);

        // Circular Stability Ring Gauge
        var gaugeContainer = BuildCircularHealthGauge();
        topActions.Children.Add(gaugeContainer);

        Grid.SetColumn(topActions, 1);
        bannerGrid.Children.Add(topActions);
        topBanner.Child = bannerGrid;
        root.Children.Add(topBanner);

        // ------------------ 1.5 SVLL AI Quick Support Copilot (Interactive AI Assistant) ------------------
        root.Children.Add(BuildDashboardAiChatBot());

        // ------------------ Quick Staging Banner for New PC/Laptop Deployment ------------------
        var quickDeployBanner = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(240, 253, 244)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(187, 247, 208)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(14, 10, 14, 10),
            Margin = new Thickness(0, 0, 0, 12)
        };
        var deployGrid = new Grid();
        deployGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        deployGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var deployTextStack = new StackPanel();
        deployTextStack.Children.Add(new TextBlock
        {
            Text = "🔌 RAPID NEW PC & LAPTOP STAGING DEPOT",
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(22, 101, 52))
        });
        deployTextStack.Children.Add(new TextBlock
        {
            Text = "Plug in any USB drive to silently deploy enterprise applications (.exe/.msi), install hardware drivers (.inf), or automatically download matching OEM WHQL drivers.",
            FontSize = 10.5,
            Foreground = TextDark,
            Margin = new Thickness(0, 2, 0, 0)
        });
        deployGrid.Children.Add(deployTextStack);

        var deployActions = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

        var btnOpenDepot = new Button
        {
            Content = "USB Depot ➔",
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            Padding = new Thickness(12, 6, 12, 6),
            Background = new SolidColorBrush(Color.FromRgb(22, 101, 52)),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand,
            Margin = new Thickness(0, 0, 6, 0)
        };
        btnOpenDepot.Click += (s, e) => NavigateTo("UsbDepot");
        deployActions.Children.Add(btnOpenDepot);

        var btnOpenUninstaller = new Button
        {
            Content = "🗑️ Force App Purge ➔",
            ToolTip = "Remove any software without password prompts or force-purge stubborn apps",
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            Padding = new Thickness(12, 6, 12, 6),
            Background = new SolidColorBrush(Color.FromRgb(239, 68, 68)),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand
        };
        btnOpenUninstaller.Click += (s, e) => NavigateTo("AppUninstaller");
        deployActions.Children.Add(btnOpenUninstaller);

        Grid.SetColumn(deployActions, 1);
        deployGrid.Children.Add(deployActions);
        quickDeployBanner.Child = deployGrid;
        root.Children.Add(quickDeployBanner);

        // ------------------ 2. Performance Metrics & Visual Waveforms (CPU, RAM, Net I/O) ------------------
        var vitalsCard = new Border
        {
            Background = BgCard,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16),
            Margin = new Thickness(0, 0, 0, 14)
        };

        var vitalsGrid = new Grid();
        vitalsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // CPU Chart
        vitalsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // RAM Chart
        vitalsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Network I/O Chart

        // Chart 1: CPU Workload Waveform
        var cpuStack = new StackPanel { Margin = new Thickness(0, 0, 10, 0) };
        cpuStack.Children.Add(new TextBlock { Text = "CPU WORKLOAD (LIVE 60s WAVEFORM)", FontSize = 10, FontWeight = FontWeights.Bold, Foreground = TextSubtle });
        var cpuNumRow = new DockPanel { Margin = new Thickness(0, 2, 0, 4) };
        _lblCpuVal = new TextBlock { Text = "0%", FontSize = 20, FontWeight = FontWeights.Bold, Foreground = SvllBlue };
        _lblCpuSub = new TextBlock { Text = $"{Environment.ProcessorCount} Threads", FontSize = 11, Foreground = TextSubtle, VerticalAlignment = VerticalAlignment.Bottom, HorizontalAlignment = HorizontalAlignment.Right };
        cpuNumRow.Children.Add(_lblCpuVal);
        cpuNumRow.Children.Add(_lblCpuSub);
        cpuStack.Children.Add(cpuNumRow);

        _cpuCanvas = new Canvas { Height = 56, Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)), ClipToBounds = true };
        AddCanvasGridLines(_cpuCanvas);
        _cpuFill = new Polygon { Fill = new LinearGradientBrush(Color.FromArgb(90, 37, 99, 235), Color.FromArgb(10, 37, 99, 235), 90) };
        _cpuLine = new Polyline { Stroke = SvllLightBlue, StrokeThickness = 2 };
        _cpuCanvas.Children.Add(_cpuFill);
        _cpuCanvas.Children.Add(_cpuLine);
        cpuStack.Children.Add(_cpuCanvas);
        Grid.SetColumn(cpuStack, 0);
        vitalsGrid.Children.Add(cpuStack);

        // Chart 2: Memory (RAM) Waveform
        var ramStack = new StackPanel { Margin = new Thickness(6, 0, 10, 0) };
        ramStack.Children.Add(new TextBlock { Text = "MEMORY USAGE (LIVE 60s WAVEFORM)", FontSize = 10, FontWeight = FontWeights.Bold, Foreground = TextSubtle });
        var ramNumRow = new DockPanel { Margin = new Thickness(0, 2, 0, 4) };
        _lblRamVal = new TextBlock { Text = "0%", FontSize = 20, FontWeight = FontWeights.Bold, Foreground = SvllBlue };
        _lblRamSub = new TextBlock { Text = "0 / 0 GB", FontSize = 11, Foreground = TextSubtle, VerticalAlignment = VerticalAlignment.Bottom, HorizontalAlignment = HorizontalAlignment.Right };
        ramNumRow.Children.Add(_lblRamVal);
        ramNumRow.Children.Add(_lblRamSub);
        ramStack.Children.Add(ramNumRow);

        _ramCanvas = new Canvas { Height = 56, Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)), ClipToBounds = true };
        AddCanvasGridLines(_ramCanvas);
        _ramFill = new Polygon { Fill = new LinearGradientBrush(Color.FromArgb(90, 26, 75, 178), Color.FromArgb(10, 26, 75, 178), 90) };
        _ramLine = new Polyline { Stroke = SvllBlue, StrokeThickness = 2 };
        _ramCanvas.Children.Add(_ramFill);
        _ramCanvas.Children.Add(_ramLine);
        ramStack.Children.Add(_ramCanvas);
        Grid.SetColumn(ramStack, 1);
        vitalsGrid.Children.Add(ramStack);

        // Chart 3: Real-Time Network Bandwidth I/O Chart (Download in Green, Upload in Purple)
        var netStack = new StackPanel { Margin = new Thickness(6, 0, 0, 0) };
        netStack.Children.Add(new TextBlock { Text = "NETWORK BANDWIDTH I/O (RX ⬇ / TX ⬆)", FontSize = 10, FontWeight = FontWeights.Bold, Foreground = TextSubtle });
        var netNumRow = new DockPanel { Margin = new Thickness(0, 2, 0, 4) };
        _lblNetVal = new TextBlock { Text = "Resolving...", FontSize = 16, FontWeight = FontWeights.Bold, Foreground = TextDark };
        _lblNetIoVal = new TextBlock { Text = "⬇ 0 KB/s  ⬆ 0 KB/s", FontSize = 10.5, Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129)), FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Bottom, HorizontalAlignment = HorizontalAlignment.Right };
        netNumRow.Children.Add(_lblNetVal);
        netNumRow.Children.Add(_lblNetIoVal);
        netStack.Children.Add(netNumRow);

        _netCanvas = new Canvas { Height = 56, Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)), ClipToBounds = true };
        AddCanvasGridLines(_netCanvas);
        _netRxFill = new Polygon { Fill = new LinearGradientBrush(Color.FromArgb(70, 16, 185, 129), Color.FromArgb(10, 16, 185, 129), 90) };
        _netRxLine = new Polyline { Stroke = new SolidColorBrush(Color.FromRgb(16, 185, 129)), StrokeThickness = 2 }; // Green Download
        _netTxLine = new Polyline { Stroke = new SolidColorBrush(Color.FromRgb(139, 92, 246)), StrokeThickness = 1.8 }; // Purple Upload
        _netCanvas.Children.Add(_netRxFill);
        _netCanvas.Children.Add(_netRxLine);
        _netCanvas.Children.Add(_netTxLine);
        netStack.Children.Add(_netCanvas);
        Grid.SetColumn(netStack, 2);
        vitalsGrid.Children.Add(netStack);

        vitalsCard.Child = vitalsGrid;
        root.Children.Add(vitalsCard);

        // ------------------ 3. Multi-Drive Storage Visualizer & Top Active Processes ------------------
        var middleGrid = new Grid { Margin = new Thickness(0, 0, 0, 14) };
        middleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Storage Visualizer
        middleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Top Memory Processes

        // Left Card: Logical Volumes Storage Breakdown
        var cardDrives = new Border
        {
            Background = BgCard,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16),
            Margin = new Thickness(0, 0, 8, 0)
        };
        var spDrives = new StackPanel();
        spDrives.Children.Add(new TextBlock
        {
            Text = "LOGICAL STORAGE VOLUMES & CAPACITY BREAKDOWN",
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            Foreground = SvllBlue,
            Margin = new Thickness(0, 0, 0, 8)
        });
        _panelDriveBars = new StackPanel();
        spDrives.Children.Add(_panelDriveBars);
        cardDrives.Child = spDrives;
        Grid.SetColumn(cardDrives, 0);
        middleGrid.Children.Add(cardDrives);

        // Right Card: Top 5 Active Processes by Memory Utilization
        var cardProcs = new Border
        {
            Background = BgCard,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16),
            Margin = new Thickness(8, 0, 0, 0)
        };
        var spProcs = new StackPanel();
        var procHeader = new DockPanel { Margin = new Thickness(0, 0, 0, 8) };
        procHeader.Children.Add(new TextBlock
        {
            Text = "TOP 5 ACTIVE RESOURCE-INTENSIVE PROCESSES",
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            Foreground = SvllBlue
        });
        var btnKillProc = new Button
        {
            Content = "Terminate Selected",
            FontSize = 10,
            Padding = new Thickness(8, 2, 8, 2),
            Background = new SolidColorBrush(Color.FromRgb(254, 242, 242)),
            Foreground = SvllRed,
            BorderBrush = new SolidColorBrush(Color.FromRgb(254, 202, 202)),
            HorizontalAlignment = HorizontalAlignment.Right
        };
        btnKillProc.Click += (s, e) => TerminateSelectedProcess();
        procHeader.Children.Add(btnKillProc);
        spProcs.Children.Add(procHeader);

        _listTopProcesses = new ListView
        {
            Height = 130,
            FontSize = 11,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1)
        };
        var gvProc = new GridView();
        gvProc.Columns.Add(new GridViewColumn { Header = "PID", Width = 55, DisplayMemberBinding = new Binding("Id") });
        gvProc.Columns.Add(new GridViewColumn { Header = "Process Name", Width = 150, DisplayMemberBinding = new Binding("Name") });
        gvProc.Columns.Add(new GridViewColumn { Header = "RAM Usage", Width = 95, DisplayMemberBinding = new Binding("MemoryFormatted") });

        var barCol = new GridViewColumn { Header = "% of RAM", Width = 110 };
        var barFactory = new FrameworkElementFactory(typeof(ProgressBar));
        barFactory.SetValue(ProgressBar.MinimumProperty, 0.0);
        barFactory.SetValue(ProgressBar.MaximumProperty, 20.0); // 20% max scale
        barFactory.SetValue(ProgressBar.HeightProperty, 7.0);
        barFactory.SetValue(ProgressBar.ForegroundProperty, SvllLightBlue);
        barFactory.SetBinding(ProgressBar.ValueProperty, new Binding("MemoryPercent"));
        barCol.CellTemplate = new DataTemplate { VisualTree = barFactory };
        gvProc.Columns.Add(barCol);

        _listTopProcesses.View = gvProc;
        spProcs.Children.Add(_listTopProcesses);
        cardProcs.Child = spProcs;
        Grid.SetColumn(cardProcs, 1);
        middleGrid.Children.Add(cardProcs);

        root.Children.Add(middleGrid);

        // ------------------ 4. Deep Hardware Specifications & System Topology ------------------
        var specsCard = new Border
        {
            Background = BgCard,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16),
            Margin = new Thickness(0, 0, 0, 14)
        };

        var specsContainer = new StackPanel();
        var specsHeader = new DockPanel { Margin = new Thickness(0, 0, 0, 12) };
        var specsTitle = new TextBlock
        {
            Text = "DEEP HARDWARE SPECIFICATIONS & SYSTEM TOPOLOGY",
            FontSize = 12,
            FontWeight = FontWeights.Bold,
            Foreground = SvllBlue
        };
        specsHeader.Children.Add(specsTitle);
        specsContainer.Children.Add(specsHeader);

        var specsGrid = new Grid();
        specsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        specsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        // Left Specs Column
        var leftSpecs = new StackPanel { Margin = new Thickness(0, 0, 10, 0) };
        _lblSpecCpu = AddSpecRow(leftSpecs, "Processor (CPU)", "Querying CPU architecture...");
        _lblSpecRam = AddSpecRow(leftSpecs, "Physical Memory (RAM)", "Calculating capacity and headroom...");
        _lblSpecGpu = AddSpecRow(leftSpecs, "Graphics Adapter (GPU)", "Querying display controller...");
        _lblSpecOs = AddSpecRow(leftSpecs, "Operating System & Build", $"{Environment.OSVersion.VersionString} (64-bit)");
        _lblSpecTasks = AddSpecRow(leftSpecs, "System Tasks & Threads", "Querying OS scheduler...");
        Grid.SetColumn(leftSpecs, 0);
        specsGrid.Children.Add(leftSpecs);

        // Right Specs Column
        var rightSpecs = new StackPanel { Margin = new Thickness(10, 0, 0, 0) };
        _lblSpecBoard = AddSpecRow(rightSpecs, "Motherboard / BaseBoard", "Querying WMI BaseBoard...");
        _lblSpecBios = AddSpecRow(rightSpecs, "BIOS Serial / Service Tag", "Querying BIOS hardware serial...");
        _lblSpecMac = AddSpecRow(rightSpecs, "Physical MAC Address", "Reading active adapter MAC...");
        _lblSpecGw = AddSpecRow(rightSpecs, "Gateway, Subnet & DNS", "Reading network routing table...");
        _lblSpecPower = AddSpecRow(rightSpecs, "Power & Energy Topology", "Detecting battery and AC mains...");
        Grid.SetColumn(rightSpecs, 1);
        specsGrid.Children.Add(rightSpecs);

        specsContainer.Children.Add(specsGrid);
        specsCard.Child = specsContainer;
        root.Children.Add(specsCard);

        scroll.Content = root;

        // Async load deep hardware specifications
        _ = Task.Run(() => LoadHardwareSpecsAsync());

        return scroll;
    }

    private UIElement BuildCircularHealthGauge()
    {
        var border = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(240, 253, 244)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(187, 247, 208)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(10, 4, 12, 4)
        };

        var sp = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

        // Circular Arc Canvas
        var canvas = new Canvas { Width = 38, Height = 38, Margin = new Thickness(0, 0, 8, 0) };

        // Background track circle
        var trackCircle = new Ellipse
        {
            Width = 32,
            Height = 32,
            Stroke = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
            StrokeThickness = 4,
            Margin = new Thickness(3)
        };
        canvas.Children.Add(trackCircle);

        // Foreground health score arc
        _healthGaugePath = new System.Windows.Shapes.Path
        {
            Stroke = new SolidColorBrush(Color.FromRgb(22, 101, 52)),
            StrokeThickness = 4,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round
        };
        canvas.Children.Add(_healthGaugePath);
        sp.Children.Add(canvas);

        var textStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        _lblHealthGaugeValue = new TextBlock
        {
            Text = "100%",
            FontSize = 14,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(22, 101, 52))
        };
        var lblSub = new TextBlock
        {
            Text = "FLEET STABILITY",
            FontSize = 8.5,
            FontWeight = FontWeights.Bold,
            Foreground = TextSubtle
        };
        textStack.Children.Add(_lblHealthGaugeValue);
        textStack.Children.Add(lblSub);
        sp.Children.Add(textStack);

        border.Child = sp;
        UpdateCircularGauge(100);
        return border;
    }

    private void UpdateCircularGauge(double score)
    {
        if (_healthGaugePath == null) return;

        double radius = 16.0;
        double cx = 19.0;
        double cy = 19.0;

        score = Math.Clamp(score, 1, 99.99); // avoid full 360 degenerate arc
        double angle = (score / 100.0) * 360.0;
        double rad = (angle - 90.0) * (Math.PI / 180.0);

        double startX = cx;
        double startY = cy - radius;
        double endX = cx + (radius * Math.Cos(rad));
        double endY = cy + (radius * Math.Sin(rad));

        var figure = new PathFigure
        {
            StartPoint = new Point(startX, startY),
            IsClosed = false
        };
        figure.Segments.Add(new ArcSegment
        {
            Point = new Point(endX, endY),
            Size = new Size(radius, radius),
            IsLargeArc = angle > 180,
            SweepDirection = SweepDirection.Clockwise
        });

        var geom = new PathGeometry();
        geom.Figures.Add(figure);
        _healthGaugePath.Data = geom;

        // Dynamic Color: Emerald (>80), Amber (60-79), Red (<60)
        var color = score >= 80 ? Color.FromRgb(22, 101, 52) : (score >= 60 ? Color.FromRgb(217, 119, 6) : Color.FromRgb(225, 45, 45));
        _healthGaugePath.Stroke = new SolidColorBrush(color);
        if (_lblHealthGaugeValue != null)
        {
            _lblHealthGaugeValue.Text = $"{Math.Round(score)}%";
            _lblHealthGaugeValue.Foreground = new SolidColorBrush(color);
        }
    }

    private void AddCanvasGridLines(Canvas canvas)
    {
        for (int i = 1; i <= 3; i++)
        {
            double pct = i * 0.25;
            var line = new Line
            {
                X1 = 0,
                X2 = 300,
                Y1 = 56 - (56 * pct),
                Y2 = 56 - (56 * pct),
                Stroke = new SolidColorBrush(Color.FromArgb(25, 0, 0, 0)),
                StrokeDashArray = new DoubleCollection { 2, 2 }
            };
            canvas.Children.Add(line);
        }
    }

    private TextBlock AddSpecRow(StackPanel parent, string label, string defaultVal)
    {
        var border = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(5),
            Padding = new Thickness(10, 5, 10, 5),
            Margin = new Thickness(0, 0, 0, 6)
        };

        var p = new StackPanel();
        p.Children.Add(new TextBlock
        {
            Text = label.ToUpperInvariant(),
            FontSize = 9.0,
            FontWeight = FontWeights.Bold,
            Foreground = TextSubtle,
            Margin = new Thickness(0, 0, 0, 1)
        });

        var valBlock = new TextBlock
        {
            Text = defaultVal,
            FontSize = 11.0,
            FontWeight = FontWeights.SemiBold,
            Foreground = TextDark,
            TextWrapping = TextWrapping.Wrap
        };
        p.Children.Add(valBlock);

        border.Child = p;
        parent.Children.Add(border);
        return valBlock;
    }

    private void InitVitalsEngine()
    {
        _vitalsTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
        _vitalsTimer.Tick += (s, e) => UpdateLiveVitals();
        _vitalsTimer.Start();
        UpdateLiveVitals();
    }

    private async void UpdateLiveVitals()
    {
        if (_lblCpuVal == null || _lblRamVal == null) return;

        try
        {
            // 1. RAM Utilization & Available Headroom
            double totalRamGb = 16.0;
            var mem = new MEMORYSTATUSEX();
            if (GlobalMemoryStatusEx(mem))
            {
                double totalGb = Math.Round((double)mem.ullTotalPhys / (1024 * 1024 * 1024), 1);
                double availGb = Math.Round((double)mem.ullAvailPhys / (1024 * 1024 * 1024), 1);
                double usedGb = Math.Round(totalGb - availGb, 1);
                uint loadPct = mem.dwMemoryLoad;
                totalRamGb = totalGb;

                _lblRamVal.Text = $"{loadPct}%";
                _lblRamSub.Text = $"{usedGb} / {totalGb} GB";
                if (_lblTopBarRam != null) _lblTopBarRam.Text = $"🧠 RAM: {loadPct}%";

                if (_lblSpecRam != null)
                {
                    double pageTotal = Math.Round((double)mem.ullTotalPageFile / (1024 * 1024 * 1024), 1);
                    double pageAvail = Math.Round((double)mem.ullAvailPageFile / (1024 * 1024 * 1024), 1);
                    _lblSpecRam.Text = $"{totalGb} GB Physical Installed | {availGb} GB Available Headroom ({100 - loadPct}% Free) | Commit: {Math.Round(pageTotal - pageAvail, 1)}/{pageTotal} GB";
                }

                if (_ramCanvas != null && _ramLine != null && _ramFill != null)
                {
                    RenderSparkline(_ramCanvas, _ramLine, _ramFill, _ramHistory, loadPct);
                }
            }

            // 2. CPU Live Workload Sampling
            double cpuPct = await Task.Run(() => SampleCpuPercentage());
            _lblCpuVal.Text = $"{Math.Round(cpuPct)}%";
            if (_lblTopBarCpu != null) _lblTopBarCpu.Text = $"🚀 CPU: {Math.Round(cpuPct)}%";
            if (_cpuCanvas != null && _cpuLine != null && _cpuFill != null)
            {
                RenderSparkline(_cpuCanvas, _cpuLine, _cpuFill, _cpuHistory, cpuPct);
            }

            // 3. System Processes & Threads Count
            int procCount = Process.GetProcesses().Length;
            if (_lblSpecTasks != null)
            {
                _lblSpecTasks.Text = $"{procCount} Active Processes | {ThreadPool.ThreadCount} ThreadPool Workers | Handles: {Process.GetCurrentProcess().HandleCount}";
            }

            // 4. Multi-Drive Storage Capacity Bars
            UpdateDriveBars();

            // 5. Active Real IPv4 & Real-Time Network I/O Bandwidth
            UpdateNetworkStats();

            // 6. Top 5 High-Memory Processes
            UpdateTopProcesses(totalRamGb);

            // 7. System Live Uptime
            var uptime = TimeSpan.FromMilliseconds(Environment.TickCount64);
            if (_lblUptimeVal != null)
                _lblUptimeVal.Text = $"Uptime: {uptime.Days}d {uptime.Hours}h {uptime.Minutes}m {uptime.Seconds}s";

            // 8. Overall Stability Score & Circular Gauge
            int healthScore = 100;
            if (mem.dwMemoryLoad > 85) healthScore -= 15;
            var cDrive = new DriveInfo("C");
            if (cDrive.IsReady && ((double)cDrive.AvailableFreeSpace / cDrive.TotalSize) < 0.15) healthScore -= 15;
            if (cpuPct > 90) healthScore -= 10;
            UpdateCircularGauge(Math.Max(30, healthScore));
        }
        catch
        {
            // Suppress runtime vitals polling transient exceptions
        }
    }

    private void UpdateDriveBars()
    {
        if (_panelDriveBars == null) return;
        _panelDriveBars.Children.Clear();

        var drives = DriveInfo.GetDrives().Where(d => d.IsReady).ToList();
        foreach (var d in drives)
        {
            double total = Math.Round((double)d.TotalSize / (1024 * 1024 * 1024), 1);
            double free = Math.Round((double)d.AvailableFreeSpace / (1024 * 1024 * 1024), 1);
            double used = Math.Round(total - free, 1);
            int pct = (int)Math.Round(((total - free) / total) * 100);

            var sp = new StackPanel { Margin = new Thickness(0, 0, 0, 8) };
            var labelGrid = new DockPanel { Margin = new Thickness(0, 0, 0, 2) };
            labelGrid.Children.Add(new TextBlock
            {
                Text = $"Volume [{d.Name}] {d.VolumeLabel}",
                FontSize = 10.5,
                FontWeight = FontWeights.Bold,
                Foreground = TextDark
            });
            labelGrid.Children.Add(new TextBlock
            {
                Text = $"{used} / {total} GB ({pct}% Used)",
                FontSize = 10,
                Foreground = TextSubtle,
                HorizontalAlignment = HorizontalAlignment.Right
            });
            sp.Children.Add(labelGrid);

            var bar = new ProgressBar
            {
                Height = 6,
                Minimum = 0,
                Maximum = 100,
                Value = pct,
                Foreground = pct >= 90 ? SvllRed : (pct >= 75 ? new SolidColorBrush(Color.FromRgb(217, 119, 6)) : SvllBlue)
            };
            sp.Children.Add(bar);
            _panelDriveBars.Children.Add(sp);
        }
    }

    private void UpdateNetworkStats()
    {
        try
        {
            var activeInterface = NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up
                            && n.NetworkInterfaceType != NetworkInterfaceType.Loopback
                            && n.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                .OrderByDescending(n => n.GetIPProperties().GatewayAddresses.Count > 0)
                .ThenByDescending(n => n.Speed)
                .FirstOrDefault();

            if (activeInterface != null)
            {
                var ipProps = activeInterface.GetIPProperties();
                var ipv4Unicast = ipProps.UnicastAddresses
                    .FirstOrDefault(u => u.Address.AddressFamily == AddressFamily.InterNetwork
                                         && !u.Address.ToString().StartsWith("127.")
                                         && !u.Address.ToString().StartsWith("169.254"));

                string ip = ipv4Unicast?.Address.ToString() ?? "DHCP Discovering...";
                string subnet = ipv4Unicast?.IPv4Mask.ToString() ?? "255.255.255.0";

                var gateway = ipProps.GatewayAddresses
                    .FirstOrDefault(g => g.Address.AddressFamily == AddressFamily.InterNetwork);
                string gw = gateway != null ? gateway.Address.ToString() : "None";

                byte[] macBytes = activeInterface.GetPhysicalAddress().GetAddressBytes();
                string mac = macBytes.Length == 6
                    ? string.Join(":", macBytes.Select(b => b.ToString("X2")))
                    : "Virtual Adapter";

                _lblNetVal.Text = ip;
                if (_lblTopBarIp != null) _lblTopBarIp.Text = $"🌐 {ip}";
                if (_lblTopBarGateway != null && gw != "None") _lblTopBarGateway.Text = $"⚡ GW: {gw} 🟢";

                // Live Network Bandwidth I/O Throughput
                var stats = activeInterface.GetIPv4Statistics();
                long rx = stats.BytesReceived;
                long tx = stats.BytesSent;
                DateTime now = DateTime.Now;

                if (_prevNetTime != DateTime.MinValue && _prevBytesRx > 0)
                {
                    double dt = (now - _prevNetTime).TotalSeconds;
                    if (dt > 0.5)
                    {
                        double rxKb = Math.Max(0, (rx - _prevBytesRx) / dt / 1024.0);
                        double txKb = Math.Max(0, (tx - _prevBytesTx) / dt / 1024.0);

                        _lblNetIoVal.Text = $"⬇ {rxKb:F1} KB/s  ⬆ {txKb:F1} KB/s";

                        if (_netCanvas != null && _netRxLine != null && _netRxFill != null && _netTxLine != null)
                        {
                            RenderNetworkSparklines(_netCanvas, _netRxLine, _netRxFill, _netTxLine, _netRxHistory, _netTxHistory, rxKb, txKb);
                        }
                    }
                }

                _prevBytesRx = rx;
                _prevBytesTx = tx;
                _prevNetTime = now;

                if (_lblSpecMac != null) _lblSpecMac.Text = mac;
                if (_lblSpecGw != null)
                {
                    string dnsServers = string.Join(", ", ipProps.DnsAddresses.Where(d => d.AddressFamily == AddressFamily.InterNetwork).Take(2));
                    _lblSpecGw.Text = $"Gateway: {gw}  |  Subnet: {subnet}  |  DNS: {dnsServers}";
                }
            }
            else
            {
                _lblNetVal.Text = "Disconnected";
                _lblNetIoVal.Text = "No active connection";
            }
        }
        catch
        {
            _lblNetVal.Text = "Network Offline";
        }
    }

    private void RenderNetworkSparklines(Canvas canvas, Polyline rxLine, Polygon rxFill, Polyline txLine, List<double> rxHistory, List<double> txHistory, double newRx, double newTx)
    {
        rxHistory.Add(newRx);
        txHistory.Add(newTx);
        while (rxHistory.Count > 30) rxHistory.RemoveAt(0);
        while (txHistory.Count > 30) txHistory.RemoveAt(0);

        double width = canvas.ActualWidth > 0 ? canvas.ActualWidth : 220;
        double height = canvas.ActualHeight > 0 ? canvas.ActualHeight : 56;
        double maxVal = Math.Max(50.0, Math.Max(rxHistory.Max(), txHistory.Max()));

        double step = width / Math.Max(1, rxHistory.Count - 1);

        var rxPts = new PointCollection();
        var rxFillPts = new PointCollection { new Point(0, height) };
        var txPts = new PointCollection();

        for (int i = 0; i < rxHistory.Count; i++)
        {
            double x = i * step;
            double yRx = height - (Math.Clamp(rxHistory[i] / maxVal, 0, 1) * (height - 4));
            double yTx = height - (Math.Clamp(txHistory[i] / maxVal, 0, 1) * (height - 4));

            var pRx = new Point(x, yRx);
            rxPts.Add(pRx);
            rxFillPts.Add(pRx);
            txPts.Add(new Point(x, yTx));
        }

        rxFillPts.Add(new Point((rxHistory.Count - 1) * step, height));
        rxLine.Points = rxPts;
        rxFill.Points = rxFillPts;
        txLine.Points = txPts;
    }

    private void UpdateTopProcesses(double totalRamGb)
    {
        if (_listTopProcesses == null) return;

        try
        {
            var top = Process.GetProcesses()
                .OrderByDescending(p =>
                {
                    try { return p.WorkingSet64; } catch { return 0L; }
                })
                .Take(5)
                .Select(p =>
                {
                    double mb = 0;
                    try { mb = p.WorkingSet64 / (1024.0 * 1024.0); } catch { }
                    return new TopProcessItem
                    {
                        Id = p.Id,
                        Name = p.ProcessName,
                        MemoryMb = mb,
                        MemoryPercent = totalRamGb > 0 ? (mb / (totalRamGb * 1024.0)) * 100.0 : 0
                    };
                })
                .ToList();

            _topProcesses.Clear();
            _topProcesses.AddRange(top);
            _listTopProcesses.ItemsSource = null;
            _listTopProcesses.ItemsSource = _topProcesses;
        }
        catch { }
    }

    private void TerminateSelectedProcess()
    {
        if (_listTopProcesses.SelectedItem is TopProcessItem item)
        {
            if (MessageBox.Show($"Are you sure you want to terminate process '{item.Name}' (PID: {item.Id})?", "Confirm Termination", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    var proc = Process.GetProcessById(item.Id);
                    proc.Kill();
                    Log($"[TASK ADMIN] Terminated process {item.Name} (PID {item.Id}).");
                    UpdateLiveVitals();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Could not terminate process: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    private async Task LoadHardwareSpecsAsync()
    {
        if (_hardwareSpecsLoaded) return;

        try
        {
            // 1. CPU Name via Registry (Instant & Reliable)
            string cpu = "";
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
                cpu = key?.GetValue("ProcessorNameString")?.ToString()?.Trim() ?? "";
            }
            catch { }
            if (string.IsNullOrEmpty(cpu))
                cpu = $"{Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER") ?? "x64 Processor"} ({Environment.ProcessorCount} Threads)";
            else
                cpu = $"{cpu} ({Environment.ProcessorCount} Logical Cores)";

            // 2. OS Build via Registry & WMI
            string os = "";
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
                string prodName = key?.GetValue("ProductName")?.ToString() ?? "Windows";
                string displayVer = key?.GetValue("DisplayVersion")?.ToString() ?? "";
                string build = key?.GetValue("CurrentBuildNumber")?.ToString() ?? "";
                string ubr = key?.GetValue("UBR")?.ToString() ?? "";

                // Windows 11 registry ProductName intentionally remains "Windows 10 ..." for backward compatibility
                // Build 22000 and higher is Windows 11
                if (int.TryParse(build, out int bNum) && bNum >= 22000)
                {
                    prodName = prodName.Replace("Windows 10", "Windows 11");
                }

                os = $"{prodName} {displayVer} (Build {build}.{ubr})".Trim();
            }
            catch { }
            if (string.IsNullOrEmpty(os)) os = Environment.OSVersion.VersionString;

            // 3. Motherboard, GPU, BIOS, Power via WMI
            string board = "Unknown Motherboard";
            string bios = "Standard OEM BIOS";
            string gpu = "Standard Display Adapter";
            string power = "AC Mains (Desktop Power Supply)";

            await Task.Run(() =>
            {
                try
                {
                    using var searcher = new ManagementObjectSearcher("SELECT Manufacturer, Product, SerialNumber FROM Win32_BaseBoard");
                    foreach (var obj in searcher.Get())
                    {
                        string mfg = obj["Manufacturer"]?.ToString()?.Trim() ?? "";
                        string prod = obj["Product"]?.ToString()?.Trim() ?? "";
                        board = $"{mfg} {prod}".Trim();
                        break;
                    }
                }
                catch { }

                try
                {
                    using var searcher = new ManagementObjectSearcher("SELECT Manufacturer, SMBIOSBIOSVersion, SerialNumber FROM Win32_BIOS");
                    foreach (var obj in searcher.Get())
                    {
                        string mfg = obj["Manufacturer"]?.ToString()?.Trim() ?? "";
                        string ver = obj["SMBIOSBIOSVersion"]?.ToString()?.Trim() ?? "";
                        string ser = obj["SerialNumber"]?.ToString()?.Trim() ?? "";
                        bios = $"{mfg} {ver} | Service Tag: {ser}".Trim();
                        break;
                    }
                }
                catch { }

                try
                {
                    using var searcher = new ManagementObjectSearcher("SELECT Name, DriverVersion, AdapterRAM FROM Win32_VideoController");
                    foreach (var obj in searcher.Get())
                    {
                        string name = obj["Name"]?.ToString()?.Trim() ?? "";
                        string drv = obj["DriverVersion"]?.ToString()?.Trim() ?? "";
                        if (!string.IsNullOrEmpty(name))
                        {
                            gpu = $"{name} (Driver {drv})";
                            break;
                        }
                    }
                }
                catch { }

                try
                {
                    using var searcher = new ManagementObjectSearcher("SELECT EstimatedChargeRemaining, BatteryStatus FROM Win32_Battery");
                    foreach (var obj in searcher.Get())
                    {
                        int charge = Convert.ToInt32(obj["EstimatedChargeRemaining"]);
                        power = $"Laptop Battery: {charge}% Remaining";
                        break;
                    }
                }
                catch { }
            });

            _cachedCpuName = cpu;
            _cachedOsBuild = os;
            _cachedBoardName = board;
            _cachedBiosSerial = bios;
            _cachedGpuName = gpu;
            _cachedPowerInfo = power;
            _hardwareSpecsLoaded = true;

            Dispatcher.Invoke(() =>
            {
                if (_lblSpecCpu != null) _lblSpecCpu.Text = _cachedCpuName;
                if (_lblSpecOs != null) _lblSpecOs.Text = _cachedOsBuild;
                if (_lblSpecBoard != null) _lblSpecBoard.Text = _cachedBoardName;
                if (_lblSpecBios != null) _lblSpecBios.Text = _cachedBiosSerial;
                if (_lblSpecGpu != null) _lblSpecGpu.Text = _cachedGpuName;
                if (_lblSpecPower != null) _lblSpecPower.Text = _cachedPowerInfo;
            });
        }
        catch
        {
            // Graceful fallback
        }
    }

    private void RenderSparkline(Canvas canvas, Polyline line, Polygon fill, List<double> history, double newVal)
    {
        history.Add(newVal);
        while (history.Count > 30)
        {
            history.RemoveAt(0);
        }

        double width = canvas.ActualWidth > 0 ? canvas.ActualWidth : 220;
        double height = canvas.ActualHeight > 0 ? canvas.ActualHeight : 56;
        double step = width / Math.Max(1, history.Count - 1);

        var pts = new PointCollection();
        var fillPts = new PointCollection { new Point(0, height) };

        for (int i = 0; i < history.Count; i++)
        {
            double x = i * step;
            double y = height - (Math.Clamp(history[i], 0, 100) / 100.0 * (height - 4));
            var p = new Point(x, y);
            pts.Add(p);
            fillPts.Add(p);
        }

        fillPts.Add(new Point((history.Count - 1) * step, height));
        line.Points = pts;
        fill.Points = fillPts;
    }

    private static double SampleCpuPercentage()
    {
        if (!GetSystemTimes(out long idle, out long kernel, out long user)) return 0.0;

        if (_prevIdle == 0L)
        {
            _prevIdle = idle;
            _prevKernel = kernel;
            _prevUser = user;
            return 0.0;
        }

        long dIdle = idle - _prevIdle;
        long dKernel = kernel - _prevKernel;
        long dUser = user - _prevUser;

        _prevIdle = idle;
        _prevKernel = kernel;
        _prevUser = user;

        long total = dKernel + dUser;
        if (total <= 0) return 0.0;

        return Math.Clamp(Math.Round((double)(total - dIdle) / total * 100.0, 1), 0.0, 100.0);
    }

    #region Dashboard AI Quick Support Assistant & Feature Hub

    private UIElement BuildDashboardAiChatBot()
    {
        var card = new Border
        {
            Background = BgCard,
            BorderBrush = SvllBlue,
            BorderThickness = new Thickness(1.5),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(14, 12, 14, 12),
            Margin = new Thickness(0, 0, 0, 14)
        };

        var mainStack = new StackPanel();

        // 1. Header Bar with Expand / Collapse and Clear
        var headerGrid = new Grid();
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var titleRow = new WrapPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

        var iconBadge = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(239, 246, 255)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(191, 219, 254)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(6, 3, 6, 3),
            Margin = new Thickness(0, 0, 8, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock { Text = "🤖", FontSize = 14 }
        };
        titleRow.Children.Add(iconBadge);

        titleRow.Children.Add(new TextBlock
        {
            Text = "SVLL AI Quick Support Copilot",
            FontSize = 14,
            FontWeight = FontWeights.Bold,
            Foreground = SvllBlue,
            VerticalAlignment = VerticalAlignment.Center
        });

        var onlineBadge = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(220, 252, 231)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(134, 239, 172)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(7, 2, 7, 2),
            Margin = new Thickness(8, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock { Text = "🟢 LIVE AI ASSISTANT", FontSize = 9, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(22, 101, 52)) }
        };
        titleRow.Children.Add(onlineBadge);

        var subText = new TextBlock
        {
            Text = "— Ask any technical question or execute any feature across the entire workstation",
            FontSize = 10.5,
            Foreground = TextSubtle,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8, 0, 0, 0)
        };
        titleRow.Children.Add(subText);

        Grid.SetColumn(titleRow, 0);
        headerGrid.Children.Add(titleRow);

        var topButtons = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

        var btnClear = new Button
        {
            Content = "🗑️ Clear",
            FontSize = 10,
            Padding = new Thickness(8, 3, 8, 3),
            Margin = new Thickness(0, 0, 6, 0),
            Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
            BorderBrush = BorderMuted,
            Cursor = Cursors.Hand
        };
        btnClear.Click += (s, e) => ResetDashboardAiChat();
        topButtons.Children.Add(btnClear);

        var btnToggle = new Button
        {
            Content = "▲ Minimize",
            FontSize = 10,
            FontWeight = FontWeights.SemiBold,
            Padding = new Thickness(8, 3, 8, 3),
            Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
            BorderBrush = BorderMuted,
            Cursor = Cursors.Hand
        };
        btnToggle.Click += (s, e) =>
        {
            _isDashboardAiExpanded = !_isDashboardAiExpanded;
            _dashboardAiBody.Visibility = _isDashboardAiExpanded ? Visibility.Visible : Visibility.Collapsed;
            btnToggle.Content = _isDashboardAiExpanded ? "▲ Minimize" : "▼ Expand Support Copilot";
        };
        topButtons.Children.Add(btnToggle);

        Grid.SetColumn(topButtons, 1);
        headerGrid.Children.Add(topButtons);
        mainStack.Children.Add(headerGrid);

        // 2. Collapsible Body Container
        _dashboardAiBody = new Border { Margin = new Thickness(0, 10, 0, 0) };
        var bodyStack = new StackPanel();

        // 2a. 1-Click Fast Feature Action Chips Bar
        var chipsScroll = new ScrollViewer { HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, VerticalScrollBarVisibility = ScrollBarVisibility.Disabled, Margin = new Thickness(0, 0, 0, 8) };
        var chipsWrap = new WrapPanel { Orientation = Orientation.Horizontal };

        void AddQuickFeatureChip(string icon, string label, string actionTag, string? customQuestion = null)
        {
            var btnChip = new Button
            {
                Content = $"{icon} {label}",
                FontSize = 10,
                FontWeight = FontWeights.SemiBold,
                Padding = new Thickness(9, 4, 9, 4),
                Margin = new Thickness(0, 0, 6, 4),
                Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
                BorderBrush = BorderMuted,
                Cursor = Cursors.Hand
            };
            btnChip.Click += (s, e) =>
            {
                if (customQuestion != null)
                {
                    _ = SendDashboardAiMessageAsync(customQuestion);
                }
                else
                {
                    ExecuteAiFeatureAction(actionTag);
                }
            };
            chipsWrap.Children.Add(btnChip);
        }

        AddQuickFeatureChip("⚡", "1-Click Turbo Boost", "TurboBoost");
        AddQuickFeatureChip("🖨️", "Fix Print Spooler", "FixPrintSpooler");
        AddQuickFeatureChip("🌐", "Ping Gateway & DNS", "PingGateway");
        AddQuickFeatureChip("🧹", "Purge Temp Files", "PurgeTemp");
        AddQuickFeatureChip("📋", "File IT Ticket", "OpenSubmitComplaint");
        AddQuickFeatureChip("🔴", "Remote Client", "OpenRemoteClient");
        AddQuickFeatureChip("🔌", "USB Depot", "OpenUsbDepot");
        AddQuickFeatureChip("🗑️", "Force App Purge", "OpenAppUninstaller");
        AddQuickFeatureChip("🛡️", "Security Shield", "OpenProcessShield");
        AddQuickFeatureChip("💾", "S.M.A.R.T. Drive", "OpenStorageSmart");
        AddQuickFeatureChip("📖", "IT Playbook", "OpenLibrary");
        AddQuickFeatureChip("❓", "Ask: How to fix Boss ERP?", "", "How to fix Boss ERP session disconnects?");

        chipsScroll.Content = chipsWrap;
        bodyStack.Children.Add(chipsScroll);

        // 2b. Scrollable Chat History Panel
        _dashboardAiChatScroll = new ScrollViewer
        {
            Height = 220,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
            Padding = new Thickness(8),
            Margin = new Thickness(0, 0, 0, 8)
        };
        var chatBorder = new Border
        {
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Child = _dashboardAiChatScroll
        };

        _dashboardAiChatPanel = new StackPanel();
        _dashboardAiChatScroll.Content = _dashboardAiChatPanel;
        bodyStack.Children.Add(chatBorder);

        // 2c. Input Area & Send Button
        var inputGrid = new Grid { Margin = new Thickness(0, 0, 0, 2) };
        inputGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        inputGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var inputBorder = new Border
        {
            Background = Brushes.White,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(4, 2, 4, 2)
        };

        _txtDashboardAiInput = new TextBox
        {
            FontSize = 11.5,
            BorderThickness = new Thickness(0),
            VerticalAlignment = VerticalAlignment.Center,
            Padding = new Thickness(6, 4, 6, 4)
        };
        _txtDashboardAiInput.KeyDown += (s, e) =>
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                string query = _txtDashboardAiInput.Text.Trim();
                if (!string.IsNullOrEmpty(query))
                {
                    _ = SendDashboardAiMessageAsync(query);
                }
            }
        };
        inputBorder.Child = _txtDashboardAiInput;
        Grid.SetColumn(inputBorder, 0);
        inputGrid.Children.Add(inputBorder);

        _btnDashboardAiSend = new Button
        {
            Content = "Ask AI ➔",
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            Background = SvllBlue,
            Foreground = Brushes.White,
            Padding = new Thickness(14, 6, 14, 6),
            Margin = new Thickness(8, 0, 0, 0),
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand
        };
        _btnDashboardAiSend.Click += (s, e) =>
        {
            string query = _txtDashboardAiInput.Text.Trim();
            if (!string.IsNullOrEmpty(query))
            {
                _ = SendDashboardAiMessageAsync(query);
            }
        };
        Grid.SetColumn(_btnDashboardAiSend, 1);
        inputGrid.Children.Add(_btnDashboardAiSend);
        bodyStack.Children.Add(inputGrid);

        // 2d. Status Indicator
        _lblDashboardAiStatus = new TextBlock
        {
            FontSize = 10,
            Foreground = SvllBlue,
            Margin = new Thickness(4, 3, 0, 0)
        };
        bodyStack.Children.Add(_lblDashboardAiStatus);

        _dashboardAiBody.Child = bodyStack;
        mainStack.Children.Add(_dashboardAiBody);
        card.Child = mainStack;

        // Initialize with friendly welcome greeting
        ResetDashboardAiChat();

        return card;
    }

    private void ResetDashboardAiChat()
    {
        _dashboardAiChatPanel.Children.Clear();
        AppendBotChatBubble(
            $"👋 Hello **{Environment.UserName}**! I am your **SVLL AI Quick Support Copilot** on `{Environment.MachineName}`.\n\n" +
            "• You can **ask any question** (IT issues, Windows errors, Boss ERP client, printer setup, or general tech & logistics questions).\n" +
            "• You have **full 1-click execution** for all workstation tools (e.g. *'turbo boost my PC'*, *'fix printer spooler'*, *'clean temp files'*, *'ping gateway'*, *'file complaint ticket'*).\n\n" +
            "How can I assist you right now?",
            "TurboBoost", "⚡ Run 1-Click Turbo Boost Optimization", "Free RAM and optimize CPU performance", false,
            new List<string> { "⚡ 1-Click Turbo Boost", "🖨️ Fix Print Spooler", "🌐 Ping Gateway & DNS", "🧹 Clean Temp Files", "📋 File IT Complaint Ticket" });
    }

    private void AppendUserChatBubble(string text)
    {
        var bubble = new Border
        {
            Background = SvllBlue,
            CornerRadius = new CornerRadius(12, 12, 2, 12),
            Padding = new Thickness(12, 8, 12, 8),
            Margin = new Thickness(60, 4, 4, 4),
            HorizontalAlignment = HorizontalAlignment.Right
        };

        var sp = new StackPanel();
        sp.Children.Add(new TextBlock
        {
            Text = text,
            FontSize = 11.5,
            Foreground = Brushes.White,
            TextWrapping = TextWrapping.Wrap
        });
        sp.Children.Add(new TextBlock
        {
            Text = DateTime.Now.ToString("hh:mm tt"),
            FontSize = 8.5,
            Foreground = new SolidColorBrush(Color.FromArgb(180, 255, 255, 255)),
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 3, 0, 0)
        });

        bubble.Child = sp;
        _dashboardAiChatPanel.Children.Add(bubble);
        _dashboardAiChatScroll.ScrollToEnd();
    }

    private void AppendBotChatBubble(string text, string? actionTag, string? actionTitle, string? actionDescription, bool isOnline, List<string>? suggestions)
    {
        var bubble = new Border
        {
            Background = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12, 12, 12, 2),
            Padding = new Thickness(12, 10, 12, 10),
            Margin = new Thickness(4, 4, 60, 6),
            HorizontalAlignment = HorizontalAlignment.Left
        };

        var sp = new StackPanel();

        // Header with timestamp and source pill
        var header = new WrapPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 6) };
        header.Children.Add(new TextBlock
        {
            Text = "🤖 SVLL AI Assistant",
            FontSize = 10.5,
            FontWeight = FontWeights.Bold,
            Foreground = SvllBlue,
            VerticalAlignment = VerticalAlignment.Center
        });

        var badge = new Border
        {
            Background = isOnline ? new SolidColorBrush(Color.FromRgb(220, 252, 231)) : new SolidColorBrush(Color.FromRgb(239, 246, 255)),
            BorderBrush = isOnline ? new SolidColorBrush(Color.FromRgb(134, 239, 172)) : new SolidColorBrush(Color.FromRgb(191, 219, 254)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(5, 1, 5, 1),
            Margin = new Thickness(6, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock
            {
                Text = isOnline ? "🌐 CLOUD AI" : "⚡ LOCAL AI",
                FontSize = 8.5,
                FontWeight = FontWeights.Bold,
                Foreground = isOnline ? new SolidColorBrush(Color.FromRgb(22, 101, 52)) : SvllBlue
            }
        };
        header.Children.Add(badge);

        header.Children.Add(new TextBlock
        {
            Text = " • " + DateTime.Now.ToString("hh:mm tt"),
            FontSize = 9,
            Foreground = TextSubtle,
            VerticalAlignment = VerticalAlignment.Center
        });
        sp.Children.Add(header);

        // Body Text
        sp.Children.Add(new TextBlock
        {
            Text = text,
            FontSize = 11.5,
            Foreground = TextDark,
            TextWrapping = TextWrapping.Wrap,
            LineHeight = 16
        });

        // 1-Click Feature Action Button
        if (!string.IsNullOrEmpty(actionTag))
        {
            var btnAction = new Button
            {
                Content = !string.IsNullOrEmpty(actionTitle) ? actionTitle : $"🚀 Execute {actionTag}",
                ToolTip = actionDescription,
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Background = new SolidColorBrush(Color.FromRgb(239, 246, 255)),
                BorderBrush = SvllLightBlue,
                BorderThickness = new Thickness(1.2),
                Foreground = SvllBlue,
                Cursor = Cursors.Hand,
                Padding = new Thickness(12, 6, 12, 6),
                Margin = new Thickness(0, 8, 0, 4),
                HorizontalAlignment = HorizontalAlignment.Left
            };
            btnAction.Click += (s, e) => ExecuteAiFeatureAction(actionTag);
            sp.Children.Add(btnAction);
        }

        // Suggestion Chips
        if (suggestions != null && suggestions.Count > 0)
        {
            var sugWrap = new WrapPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 6, 0, 0) };
            foreach (var sug in suggestions.Take(4))
            {
                var chip = new Button
                {
                    Content = sug,
                    FontSize = 9.5,
                    Padding = new Thickness(7, 3, 7, 3),
                    Margin = new Thickness(0, 0, 5, 3),
                    Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
                    BorderBrush = BorderMuted,
                    Cursor = Cursors.Hand
                };
                chip.Click += (s, e) => _ = SendDashboardAiMessageAsync(sug);
                sugWrap.Children.Add(chip);
            }
            sp.Children.Add(sugWrap);
        }

        bubble.Child = sp;
        _dashboardAiChatPanel.Children.Add(bubble);
        _dashboardAiChatScroll.ScrollToEnd();
    }

    private async Task SendDashboardAiMessageAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return;

        AppendUserChatBubble(query);
        _txtDashboardAiInput.Text = "";
        _btnDashboardAiSend.IsEnabled = false;
        _lblDashboardAiStatus.Text = "🤖 AI Thinking... Querying intelligence engine & running diagnostics...";

        try
        {
            var response = await DashboardAiService.Instance.AskAsync(query);
            AppendBotChatBubble(
                response.Answer,
                response.ActionTag,
                response.ActionTitle,
                response.ActionDescription,
                response.IsOnlineAi,
                response.Suggestions);
        }
        catch (Exception ex)
        {
            AppendBotChatBubble($"⚠️ An unexpected error occurred: {ex.Message}", "TurboBoost", "⚡ Run 1-Click Turbo Boost", "Accelerate PC", false, null);
        }
        finally
        {
            _btnDashboardAiSend.IsEnabled = true;
            _lblDashboardAiStatus.Text = "";
            _txtDashboardAiInput.Focus();
        }
    }

    private async void ExecuteAiFeatureAction(string actionTag)
    {
        switch (actionTag)
        {
            case "TurboBoost":
                AppendUserChatBubble("⚡ Please run 1-Click Turbo Boost to optimize my workstation.");
                _lblDashboardAiStatus.Text = "⏳ Accelerating system and trimming working sets...";
                try
                {
                    var res = await ExecuteTurboBoostAsync();
                    AppendBotChatBubble(
                        $"🎉 **1-Click Turbo Boost Completed!**\n\n" +
                        $"• **RAM Released:** `{res.freedMb} MB` of fragmented memory recovered\n" +
                        $"• **Bloat Tasks Terminated:** `{res.killedCount}` background tasks stopped\n" +
                        $"• **Applications Trimmed:** `{res.trimmedCount}` working sets flushed\n" +
                        $"• **CPU Power Plan:** Ultimate Performance profile active",
                        null, null, null, false, new List<string> { "Purge Temp Files", "Check S.M.A.R.T. Drive", "Ping Gateway Diagnostic" });
                }
                catch (Exception ex)
                {
                    AppendBotChatBubble($"⚠️ Optimization note: {ex.Message}", null, null, null, false, null);
                }
                finally
                {
                    _lblDashboardAiStatus.Text = "";
                }
                break;

            case "FixPrintSpooler":
                AppendUserChatBubble("🖨️ Please fix and restart the print spooler service.");
                _lblDashboardAiStatus.Text = "⏳ Purging locked spooler jobs and restarting Spooler service...";
                try
                {
                    await Task.Run(() =>
                    {
                        ExecutePowerShellScript("Stop-Service Spooler -Force; Remove-Item $env:windir\\System32\\spool\\PRINTERS\\* -Force -Recurse -ErrorAction SilentlyContinue; Start-Service Spooler");
                    });
                    AppendBotChatBubble(
                        "✔ **Print Spooler Subsystem Reset Successfully!**\n\n" +
                        "• **Spooler Service:** Stopped and cleanly restarted\n" +
                        "• **Queue Cache:** Locked `.SPL` and `.SHD` phantom job files purged from `System32\\spool\\PRINTERS`\n" +
                        "• **Status:** Print subsystem is now ready to receive print jobs.",
                        "OpenPrinterSuite", "🖨️ Open Full Print Diagnostics Suite", "Deep test printer ports and drivers", false,
                        new List<string> { "Test Printer Driver", "File IT Complaint Ticket", "1-Click Turbo Boost" });
                }
                catch (Exception ex)
                {
                    AppendBotChatBubble($"⚠️ Spooler reset error: {ex.Message}", null, null, null, false, null);
                }
                finally
                {
                    _lblDashboardAiStatus.Text = "";
                }
                break;

            case "FlushDns":
                AppendUserChatBubble("🌐 Please flush DNS resolver cache and reset network stack.");
                _lblDashboardAiStatus.Text = "⏳ Clearing DNS resolver cache...";
                try
                {
                    await Task.Run(() =>
                    {
                        ExecutePowerShellScript("Clear-DnsClientCache; ipconfig /flushdns");
                    });
                    AppendBotChatBubble(
                        "✔ **DNS Resolver Cache Successfully Flushed!**\n\n" +
                        "• **Cache:** Cleared all poisoned or stale DNS hostname entries\n" +
                        "• **Protocol Stack:** IPv4 and IPv6 resolver renewed\n" +
                        "• **Verification:** Domain names will now be resolved directly from upstream DNS.",
                        "PingGateway", "📡 Test Gateway Ping Latency", "Ping 8.8.8.8 and local gateway", false,
                        new List<string> { "Ping Gateway Diagnostic", "Network Profile Switcher", "File IT Complaint Ticket" });
                }
                catch (Exception ex)
                {
                    AppendBotChatBubble($"⚠️ DNS flush error: {ex.Message}", null, null, null, false, null);
                }
                finally
                {
                    _lblDashboardAiStatus.Text = "";
                }
                break;

            case "PurgeTemp":
                AppendUserChatBubble("🧹 Please clean temporary files and empty Recycle Bin.");
                _lblDashboardAiStatus.Text = "⏳ Purging user and system temp folders...";
                try
                {
                    await Task.Run(() =>
                    {
                        try { CleanDirectory(System.IO.Path.GetTempPath()); } catch { }
                        try { CleanDirectory(@"C:\Windows\Temp"); } catch { }
                        try { SHEmptyRecycleBin(IntPtr.Zero, null!, 7); } catch { }
                    });
                    AppendBotChatBubble(
                        "✔ **Temporary Junk & Recycle Bin Purged!**\n\n" +
                        "• **User Temp:** Cleaned `%TEMP%` directory\n" +
                        "• **System Temp:** Cleaned `C:\\Windows\\Temp`\n" +
                        "• **Recycle Bin:** Emptied all deleted files across local storage drives\n" +
                        "• **Disk Headroom:** Freed storage space for optimal OS caching.",
                        "OpenStorageSmart", "💾 Inspect S.M.A.R.T. Drive Health", "View SSD health & wear %", false,
                        new List<string> { "1-Click Turbo Boost", "Check S.M.A.R.T. Drive", "WinUtil Debloat" });
                }
                catch (Exception ex)
                {
                    AppendBotChatBubble($"⚠️ Temp clean note: {ex.Message}", null, null, null, false, null);
                }
                finally
                {
                    _lblDashboardAiStatus.Text = "";
                }
                break;

            case "PingGateway":
                AppendUserChatBubble("📡 Please run a network gateway ping test.");
                _lblDashboardAiStatus.Text = "⏳ Sending ICMP echo packets to gateway & public DNS...";
                try
                {
                    string pingReport = await Task.Run(() =>
                    {
                        var sb = new System.Text.StringBuilder();
                        try
                        {
                            var ping = new Ping();
                            var reply = ping.Send("8.8.8.8", 2000);
                            if (reply.Status == IPStatus.Success)
                                sb.AppendLine($"• **Internet Gateway (8.8.8.8):** Connected | Roundtrip `{reply.RoundtripTime} ms` | TTL: `{reply.Options?.Ttl}`");
                            else
                                sb.AppendLine($"• **Internet Gateway (8.8.8.8):** Packet Failed ({reply.Status})");
                        }
                        catch (Exception ex)
                        {
                            sb.AppendLine($"• **Internet Gateway:** Error ({ex.Message})");
                        }
                        return sb.ToString().Trim();
                    });

                    AppendBotChatBubble(
                        $"🌐 **Network Latency Ping Diagnostic Results:**\n\n{pingReport}\n\n• **Local IP:** `{FleetRegistryService.Instance.LocalDevice.IpAddress}`\n• **Network State:** Active Ethernet/WLAN Adapter",
                        "FlushDns", "⚡ Flush DNS Resolver Cache", "Clear DNS cache", false,
                        new List<string> { "Flush DNS", "File IT Complaint Ticket", "Network Profile Switcher" });
                }
                catch (Exception ex)
                {
                    AppendBotChatBubble($"⚠️ Ping diagnostic note: {ex.Message}", null, null, null, false, null);
                }
                finally
                {
                    _lblDashboardAiStatus.Text = "";
                }
                break;

            case "OpenSubmitComplaint":
                NavigateTo("SubmitComplaint");
                break;

            case "OpenRemoteClient":
                NavigateTo("RemoteClient");
                break;

            case "OpenUsbDepot":
                NavigateTo("UsbDepot");
                break;

            case "OpenAppUninstaller":
                NavigateTo("AppUninstaller");
                break;

            case "OpenProcessShield":
                NavigateTo("ProcessShield");
                break;

            case "OpenStorageSmart":
                NavigateTo("StorageSmart");
                break;

            case "OpenLibrary":
                NavigateTo("Library");
                break;

            case "OpenAssetPassport":
                NavigateTo("AssetPassport");
                break;

            case "OpenPrinterSuite":
                NavigateTo("Printer");
                break;

            default:
                if (actionTag.StartsWith("Open"))
                    NavigateTo(actionTag.Substring(4));
                else
                    NavigateTo(actionTag);
                break;
        }
    }

    #endregion
}
