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
}
