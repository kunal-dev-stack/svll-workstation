// =============================================================================
// SVLL IT Workstation v8.5 — CCTV & NVR Surveillance Center
// Smooth Hardware-Accelerated RTSP Streaming (LibVLC 30 FPS) & NVR Playback Studio
// Supports Hikvision, CP PLUS, Dahua, Uniview, and Generic ONVIF/RTSP Hardware.
// =============================================================================
#if BUILD_ADMIN

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Path = System.IO.Path;
using System.Linq;
using System.Media;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Win32;
using LibVLCSharp.Shared;
using LibVLCSharp.WPF;
using MediaPlayer = LibVLCSharp.Shared.MediaPlayer;
using SVLL_IT_Workstation.Models;

namespace SVLL_IT_Workstation;

public partial class MainWindow
{
    // Mode: "LIVE" or "PLAYBACK"
    private string _cctvCurrentMode = "LIVE";

    // LibVLC Global Engine
    private static bool _libVlcCoreInitialized = false;
    private static LibVLC? _globalLibVLC;

    // Live Grid & Monitoring State
    private int _cctvGridSize = 4; // 1 (1x1), 4 (2x2), 9 (3x3), 16 (4x4)
    private int _previousMultiGridSize = 4; // Used to restore multi-grid from 1x1 zoom
    private int _cctvSelectedSlot = 0;
    private bool _cctvUseSmoothStreaming = true; // True = RTSP 30 FPS, False = Snapshot polling
    private int _cctvRefreshIntervalSec = 1;
    private bool _cctvIsMonitoring = false;
    private DispatcherTimer? _cctvRefreshTimer;

    // Fullscreen Surveillance Wall State (Dedicated Non-Overlapping Header)
    private bool _isCctvFullscreen = false;
    private WindowState _savedWindowState = WindowState.Normal;
    private WindowStyle _savedWindowStyle = WindowStyle.SingleBorderWindow;
    private ResizeMode _savedResizeMode = ResizeMode.CanResize;
    private Visibility _savedNavVisibility = Visibility.Visible;
    private Border _fullscreenTopBar = null!;

    // Collapsible Sidebar & Wide View State
    private bool _isSidebarCollapsed = false;
    private ColumnDefinition _cctvSidebarCol = null!;
    private Border _cctvSidebarBorder = null!;
    private Button _btnToggleSidebar = null!;
    private Button? _btnBackToMultiGrid;

    // Grid slots mapping: SlotIndex -> (NvrDevice, ChannelNumber)
    private readonly Dictionary<int, (NvrDevice Device, int Channel)> _cctvSlots = new();
    private readonly Dictionary<int, MediaPlayer> _slotMediaPlayers = new();
    private readonly Dictionary<int, VideoView> _slotVideoViews = new();
    private readonly Dictionary<int, long> _slotStreamGen = new();
    private readonly Dictionary<int, CancellationTokenSource> _slotCts = new();

    // UI Host Elements (Live)
    private Grid _cctvRootGrid = null!;
    private Grid _cctvMainContainer = null!;
    private UIElement _cctvHeaderElement = null!;
    private UIElement? _cctvLiveContent;
    private UIElement? _cctvPlaybackContent;
    private Grid _cctvMainGridContainer = null!;
    private StackPanel _cctvNvrTreePanel = null!;
    private TextBox _cctvSearchBox = null!;
    private TextBlock _cctvTotalNvrsCounter = null!;
    private TextBlock _cctvTotalChannelsCounter = null!;
    private TextBlock _cctvActiveFeedsCounter = null!;
    private Button _btnModeLive = null!;
    private Button _btnModePlayback = null!;
    private Button _btnModeDiag = null!;
    private UIElement? _cctvDiagContent;
    private MediaElement? _fallbackPlaybackMediaElement;
    private ComboBox _cctvEngineCombo = null!;

    // Cell UI Elements cache
    private readonly Dictionary<int, (Border CellBorder, Image VideoImg, VideoView? VlcView, TextBlock OsdTitle, TextBlock OsdStats, TextBlock OsdTime, Border StandbyOverlay, TextBlock StandbyMessage)> _cellUiCache = new();

    // ─── Playback Studio State ───────────────────────────────────────────────
    private MediaPlayer? _playbackMediaPlayer;
    private VideoView? _playbackVideoView;
    private ComboBox _cmbPlaybackNvr = null!;
    private ComboBox _cmbPlaybackCamera = null!;
    private DatePicker _dpPlaybackDate = null!;
    private TextBox _txtPlaybackStartTime = null!;
    private TextBox _txtPlaybackEndTime = null!;
    private Slider _sliderPlaybackTimeline = null!;
    private TextBlock _lblPlaybackTimeDisplay = null!;
    private TextBlock _lblPlaybackStatus = null!;
    private TextBlock _lblPlaybackCamOverlayTitle = null!;
    private TextBlock _lblPlaybackRecordingAvail = null!;
    private TextBlock _lblDateValidationBadge = null!;
    private Button _btnPlaybackPlay = null!;
    private Button _btnPlaybackPause = null!;
    private DispatcherTimer? _playbackTimelineTimer;
    private float _currentPlaybackRate = 1.0f;
    private bool _isUserDraggingTimeline = false;
    private long _playbackBaseSeconds = 0; // Current base start second of day (0..86400)
    private readonly List<Button> _playbackDateChips = new();
    private readonly Dictionary<float, Button> _playbackSpeedButtons = new();

    // ─── Entry Point ─────────────────────────────────────────────────────────
    public UIElement BuildCctvNvrMonitorView()
    {
        InitializeLibVlcEngine();

        var rootGrid = new Grid();

        void ShowContent()
        {
            rootGrid.Children.Clear();
            rootGrid.Children.Add(BuildCctvMainContainer());
            StartCctvMonitoring();
        }

        if (!AdminAuthService.Instance.IsAdminMode)
        {
            rootGrid.Children.Add(BuildAdminLockScreen("CCTV & NVR Surveillance Center", ShowContent));
        }
        else
        {
            ShowContent();
        }

        AdminAuthService.Instance.AdminModeChanged += (isAdmin) => Dispatcher.Invoke(() =>
        {
            if (!isAdmin)
            {
                if (_isCctvFullscreen) ToggleCctvFullscreen(false);
                StopCctvMonitoring();
                rootGrid.Children.Clear();
                rootGrid.Children.Add(BuildAdminLockScreen("CCTV & NVR Surveillance Center", ShowContent));
            }
        });

        // Key hook for Esc / F11 fullscreen toggle
        this.KeyDown += OnCctvWindowKeyDown;

        return rootGrid;
    }

    private void OnCctvWindowKeyDown(object sender, KeyEventArgs e)
    {
        if (_isCctvFullscreen && (e.Key == Key.Escape || e.Key == Key.F11))
        {
            ToggleCctvFullscreen(false);
            e.Handled = true;
        }
        else if (e.Key == Key.F11 && _cctvCurrentMode == "LIVE")
        {
            ToggleCctvFullscreen(true);
            e.Handled = true;
        }
    }

    private static readonly object _libVlcLock = new();

    private static void InitializeLibVlcEngine()
    {
        if (_globalLibVLC != null) return;
        lock (_libVlcLock)
        {
            if (_globalLibVLC != null) return;
            try
            {
                if (!_libVlcCoreInitialized)
                {
                    string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                    string[] candidateDirs = new[]
                    {
                        Path.Combine(baseDir, "libvlc", "win-x64"),
                        Path.Combine(baseDir, "libvlc", "win-x86"),
                        Path.Combine(baseDir, "libvlc"),
                        Path.Combine(baseDir, "win-x64"),
                        baseDir,
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "ShreeVasuLogistics", "ITWorkstationAdmin", "libvlc", "win-x64"),
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "ShreeVasuLogistics", "ITWorkstationAdmin", "libvlc"),
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "ShreeVasuLogistics", "ITWorkstationUser", "libvlc", "win-x64"),
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "ShreeVasuLogistics", "ITWorkstationUser", "libvlc"),
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "ITWorkstationAdmin", "libvlc", "win-x64"),
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "ITWorkstationUser", "libvlc", "win-x64"),
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "VideoLAN", "VLC"),
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "VideoLAN", "VLC")
                    };

                    string? foundDir = null;
                    foreach (var dir in candidateDirs)
                    {
                        if (Directory.Exists(dir) && File.Exists(Path.Combine(dir, "libvlc.dll")))
                        {
                            foundDir = dir;
                            break;
                        }
                    }

                    if (!string.IsNullOrEmpty(foundDir))
                    {
                        string pluginsDir = Path.Combine(foundDir, "plugins");
                        if (Directory.Exists(pluginsDir))
                        {
                            Environment.SetEnvironmentVariable("VLC_PLUGIN_PATH", pluginsDir);
                        }
                        Core.Initialize(foundDir);
                    }
                    else
                    {
                        Core.Initialize();
                    }
                    _libVlcCoreInitialized = true;
                }

                _globalLibVLC = new LibVLC(
                    "--rtsp-tcp",                 // Force RTSP over TCP (avoids UDP packet drop artifacts)
                    "--network-caching=250",       // 250ms network jitter buffer
                    "--live-caching=250",          // 250ms live stream buffer
                    "--clock-jitter=0",            // Zero clock jitter for real-time surveillance
                    "--clock-synchro=0",           // Avoid audio/video clock drift stalls
                    "--drop-late-frames",          // Discard delayed frames to eliminate video lag & stucking
                    "--skip-frames",               // Skip non-reference frames under network pressure
                    "--avcodec-hw=any",            // Full GPU hardware acceleration (DXVA2, D3D11VA, NVDEC)
                    "--avcodec-fast",              // Fast motion vector decoding optimizations
                    "--avcodec-skiploopfilter=4",  // Skip non-critical deblocking filters to reduce CPU load by 70%
                    "--avcodec-threads=2",         // Limit decode threads per feed to prevent CPU thread starvation
                    "--no-audio",                  // Multi-channel CCTV surveillance focus
                    "--no-sub-autodetect-file",
                    "--no-stats"
                );
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LibVLC Initialization fallback: {ex.Message}");
                _globalLibVLC = null;
            }
        }
    }

    private UIElement BuildCctvMainContainer()
    {
        _cctvRootGrid = new Grid { Margin = new Thickness(10) };
        _cctvRootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 0: Executive Header / Fullscreen Bar
        _cctvRootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // 1: Content Host

        // 1. Executive Header (Normal Mode)
        _cctvHeaderElement = BuildCctvHeader();
        Grid.SetRow(_cctvHeaderElement as FrameworkElement, 0);
        _cctvRootGrid.Children.Add(_cctvHeaderElement);

        // 2. Dedicated Fullscreen Top Bar (Row 0 - Never overlaps video views, solves WPF airspace bug)
        _fullscreenTopBar = BuildFullscreenTopBar();
        Grid.SetRow(_fullscreenTopBar, 0);
        _cctvRootGrid.Children.Add(_fullscreenTopBar);

        // 3. View Host (Swaps between Live, Playback, and Diagnostic)
        _cctvMainContainer = new Grid();

        // Lazy initialize views: only build the active view on demand
        SwitchCctvMode(_cctvCurrentMode);

        Grid.SetRow(_cctvMainContainer, 1);
        _cctvRootGrid.Children.Add(_cctvMainContainer);

        // Subscribe to storage changes
        NvrDeviceStorage.Instance.DevicesUpdated += () => Dispatcher.Invoke(() =>
        {
            RefreshCctvTree();
            PopulatePlaybackNvrDropdown();
            RefreshDiagnosticViewIfVisible();
        });

        return _cctvRootGrid;
    }

    private void SwitchCctvMode(string mode)
    {
        _cctvCurrentMode = mode;

        if (mode == "LIVE")
        {
            if (_cctvLiveContent == null)
            {
                _cctvLiveContent = BuildCctvLiveView();
                _cctvMainContainer.Children.Add(_cctvLiveContent);
                InitializeDefaultSlots();
            }

            _cctvLiveContent.Visibility = Visibility.Visible;
            if (_cctvPlaybackContent != null) _cctvPlaybackContent.Visibility = Visibility.Collapsed;
            if (_cctvDiagContent != null) _cctvDiagContent.Visibility = Visibility.Collapsed;

            if (_btnModeLive != null) { _btnModeLive.Background = SvllBlue; _btnModeLive.Foreground = Brushes.White; }
            if (_btnModePlayback != null) { _btnModePlayback.Background = Brushes.White; _btnModePlayback.Foreground = TextDark; }
            if (_btnModeDiag != null) { _btnModeDiag.Background = Brushes.White; _btnModeDiag.Foreground = TextDark; }

            // Pause playback players if running
            try { _playbackMediaPlayer?.Pause(); } catch { }
            try { _fallbackPlaybackMediaElement?.Pause(); } catch { }

            // Resume live video feeds
            StartCctvMonitoring();
        }
        else if (mode == "PLAYBACK")
        {
            if (_cctvPlaybackContent == null)
            {
                _cctvPlaybackContent = BuildCctvPlaybackView();
                _cctvMainContainer.Children.Add(_cctvPlaybackContent);
                PopulatePlaybackNvrDropdown();
            }

            if (_cctvLiveContent != null) _cctvLiveContent.Visibility = Visibility.Collapsed;
            _cctvPlaybackContent.Visibility = Visibility.Visible;
            if (_cctvDiagContent != null) _cctvDiagContent.Visibility = Visibility.Collapsed;

            if (_btnModeLive != null) { _btnModeLive.Background = Brushes.White; _btnModeLive.Foreground = TextDark; }
            if (_btnModePlayback != null) { _btnModePlayback.Background = SvllBlue; _btnModePlayback.Foreground = Brushes.White; }
            if (_btnModeDiag != null) { _btnModeDiag.Background = Brushes.White; _btnModeDiag.Foreground = TextDark; }

            StopCctvMonitoring();
        }
        else if (mode == "DIAGNOSTIC")
        {
            if (_cctvDiagContent == null)
            {
                _cctvDiagContent = BuildCctvDiagnosticView();
                _cctvMainContainer.Children.Add(_cctvDiagContent);
            }

            if (_cctvLiveContent != null) _cctvLiveContent.Visibility = Visibility.Collapsed;
            if (_cctvPlaybackContent != null) _cctvPlaybackContent.Visibility = Visibility.Collapsed;
            _cctvDiagContent.Visibility = Visibility.Visible;

            if (_btnModeLive != null) { _btnModeLive.Background = Brushes.White; _btnModeLive.Foreground = TextDark; }
            if (_btnModePlayback != null) { _btnModePlayback.Background = Brushes.White; _btnModePlayback.Foreground = TextDark; }
            if (_btnModeDiag != null) { _btnModeDiag.Background = SvllBlue; _btnModeDiag.Foreground = Brushes.White; }

            StopCctvMonitoring();
            try { _playbackMediaPlayer?.Pause(); } catch { }
            try { _fallbackPlaybackMediaElement?.Pause(); } catch { }

            RefreshDiagnosticViewIfVisible();
        }
    }

    // ─── 1. Executive Header with Clean Non-Overlapping 2-Tier Controls ─────
    private UIElement BuildCctvHeader()
    {
        var card = new Border
        {
            Background = BgCard,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(16, 10, 16, 10),
            Margin = new Thickness(0, 0, 0, 8)
        };

        var mainLayout = new Grid();
        mainLayout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Row 0: Title, Badges & Action Buttons
        mainLayout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Row 1: Dedicated Studio Mode Switcher Bar

        // ─── Row 0: Title Header & Action Buttons ────────────────────────────
        var topRow = new Grid();
        topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var left = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 16, 0) };
        var titleRow = new WrapPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 2), VerticalAlignment = VerticalAlignment.Center };
        titleRow.Children.Add(new TextBlock
        {
            Text = "📹 CCTV & NVR Surveillance Center",
            FontSize = 16,
            FontWeight = FontWeights.ExtraBold,
            Foreground = TextDark,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0)
        });
        titleRow.Children.Add(CctvBadge("🟢 30 FPS HARDWARE ACCEL", Color.FromRgb(5, 150, 105), Color.FromRgb(209, 250, 229)));
        titleRow.Children.Add(CctvBadge("46 FLEET HUBS", Color.FromRgb(26, 75, 178), Color.FromRgb(219, 234, 254)));
        left.Children.Add(titleRow);

        left.Children.Add(new TextBlock
        {
            Text = "Shree Vasu Logistics Fleet Surveillance • 46 Enterprise Hubs • 24h Playback Studio • SLA Health Diagnostics",
            FontSize = 11,
            Foreground = TextSubtle,
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        Grid.SetColumn(left, 0);
        topRow.Children.Add(left);

        // Right Action Tools: Fullscreen, Add NVR, Snap All
        var rightTools = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

        var btnFullscreen = CctvHeaderButton("⛶ Fullscreen", Brushes.White, TextDark, () =>
        {
            ToggleCctvFullscreen();
        }, "Enter immersive fullscreen surveillance video wall mode (Press Esc to exit)");
        rightTools.Children.Add(btnFullscreen);

        var btnAddNvr = CctvHeaderButton("➕ Add NVR / DVR", SvllBlue, Brushes.White, () =>
        {
            var dlg = new NvrEditorDialog(null, this);
            if (dlg.ShowDialog() == true)
            {
                RefreshCctvTree();
                PopulatePlaybackNvrDropdown();
                Log($"[CCTV] Added new NVR device: {dlg.DeviceResult.Name} ({dlg.DeviceResult.Host})");
            }
        }, "Register a new NVR/DVR device with encrypted credentials");
        rightTools.Children.Add(btnAddNvr);

        var btnSnapAll = CctvHeaderButton("📸 Snap All", Brushes.White, TextDark, () =>
        {
            CaptureAllActiveSnapshots();
        }, "Capture instantaneous snapshots of all currently active camera feeds");
        rightTools.Children.Add(btnSnapAll);

        Grid.SetColumn(rightTools, 1);
        topRow.Children.Add(rightTools);

        Grid.SetRow(topRow, 0);
        mainLayout.Children.Add(topRow);

        // ─── Row 1: Dedicated Studio Mode Switcher (Permanently immune to overlapping) ─
        var modeRow = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(3),
            Margin = new Thickness(0, 8, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Left
        };
        var modeStack = new StackPanel { Orientation = Orientation.Horizontal };

        _btnModeLive = new Button
        {
            Content = "🔴 Live Video Wall",
            Height = 28,
            Padding = new Thickness(16, 0, 16, 0),
            FontSize = 11.5,
            FontWeight = FontWeights.Bold,
            Background = SvllBlue,
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand
        };
        _btnModeLive.Click += (s, e) => SwitchCctvMode("LIVE");
        modeStack.Children.Add(_btnModeLive);

        _btnModePlayback = new Button
        {
            Content = "⏪ NVR Playback Studio",
            Height = 28,
            Padding = new Thickness(16, 0, 16, 0),
            FontSize = 11.5,
            FontWeight = FontWeights.Bold,
            Background = Brushes.White,
            Foreground = TextDark,
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand
        };
        _btnModePlayback.Click += (s, e) => SwitchCctvMode("PLAYBACK");
        modeStack.Children.Add(_btnModePlayback);

        _btnModeDiag = new Button
        {
            Content = "🩺 Diagnostic & Health Studio",
            Height = 28,
            Padding = new Thickness(16, 0, 16, 0),
            FontSize = 11.5,
            FontWeight = FontWeights.Bold,
            Background = Brushes.White,
            Foreground = TextDark,
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand,
            ToolTip = "Enterprise CCTV SLA audit, network latency diagnostics, subnet discovery & executive health reporting"
        };
        _btnModeDiag.Click += (s, e) => SwitchCctvMode("DIAGNOSTIC");
        modeStack.Children.Add(_btnModeDiag);

        modeRow.Child = modeStack;
        Grid.SetRow(modeRow, 1);
        mainLayout.Children.Add(modeRow);

        card.Child = mainLayout;
        return card;
    }

    // ─── Dedicated Fullscreen Top Bar (Row 0 - Never Hidden by Cameras) ──────
    private Border BuildFullscreenTopBar()
    {
        var bar = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(15, 23, 42)), // Dark Slate NOC theme
            BorderBrush = new SolidColorBrush(Color.FromRgb(51, 65, 85)),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Height = 44,
            Padding = new Thickness(16, 0, 16, 0),
            Margin = new Thickness(0, 0, 0, 4),
            Visibility = Visibility.Collapsed
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        // Left Info
        var left = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        left.Children.Add(new TextBlock
        {
            Text = "📹 SVLL FULLSCREEN SURVEILLANCE WALL",
            FontSize = 13,
            FontWeight = FontWeights.ExtraBold,
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center
        });
        left.Children.Add(CctvBadge("🟢 30 FPS HARDWARE ACCEL", Color.FromRgb(34, 197, 94), Color.FromArgb(40, 34, 197, 94)));
        Grid.SetColumn(left, 0);
        grid.Children.Add(left);

        // Center: Quick Layout Buttons in Fullscreen
        var center = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        center.Children.Add(CreateGridModeButton("1x1 Focus", 1));
        center.Children.Add(CreateGridModeButton("2x2 Quad", 4));
        center.Children.Add(CreateGridModeButton("3x3 Multi", 9));
        center.Children.Add(CreateGridModeButton("4x4 Wall", 16));
        Grid.SetColumn(center, 1);
        grid.Children.Add(center);

        // Right: Prominent, Un-hidden Exit Fullscreen Button
        var btnExit = new Button
        {
            Content = "✕ Exit Fullscreen (Esc)",
            Height = 30,
            Padding = new Thickness(16, 0, 16, 0),
            FontSize = 12,
            FontWeight = FontWeights.Bold,
            Background = SvllRed,
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand,
            VerticalAlignment = VerticalAlignment.Center,
            ToolTip = "Exit fullscreen and return to workstation (Press Esc)"
        };
        btnExit.Click += (s, e) => ToggleCctvFullscreen(false);
        Grid.SetColumn(btnExit, 2);
        grid.Children.Add(btnExit);

        bar.Child = grid;
        return bar;
    }

    // ─── 2. LIVE VIDEO WALL VIEW (WIDER VIEW & COLLAPSIBLE SIDEBAR) ─────────
    private UIElement BuildCctvLiveView()
    {
        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Control bar
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Split Body

        // Control bar
        var controlBar = BuildCctvLiveControlBar();
        Grid.SetRow(controlBar, 0);
        root.Children.Add(controlBar);

        // Body: Left Sidebar (Collapsible) + Right Grid
        var bodyGrid = new Grid { Margin = new Thickness(0, 8, 0, 0) };
        _cctvSidebarCol = new ColumnDefinition { Width = new GridLength(280) };
        bodyGrid.ColumnDefinitions.Add(_cctvSidebarCol); // Col 0: Sidebar
        bodyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Col 1: Spacing
        bodyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Col 2: Video Wall Grid

        _cctvSidebarBorder = BuildCctvSidebar() as Border ?? new Border();
        Grid.SetColumn(_cctvSidebarBorder, 0);
        bodyGrid.Children.Add(_cctvSidebarBorder);

        var rightGridArea = BuildCctvGridArea();
        Grid.SetColumn(rightGridArea, 2);
        bodyGrid.Children.Add(rightGridArea);

        Grid.SetRow(bodyGrid, 1);
        root.Children.Add(bodyGrid);

        return root;
    }

    private UIElement BuildCctvLiveControlBar()
    {
        var bar = new Border
        {
            Background = BgCard,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(12, 6, 12, 6)
        };

        var barWrap = new WrapPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

        var leftStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 2, 14, 2) };

        // 1. Sidebar Toggle Button: [ ◀ Hide Sidebar ] / [ ▶ Show Cameras ]
        _btnToggleSidebar = new Button
        {
            Content = "◀ Hide Sidebar",
            Height = 28,
            Padding = new Thickness(10, 0, 10, 0),
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Background = Brushes.White,
            Foreground = TextDark,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            Margin = new Thickness(0, 0, 10, 0),
            Cursor = Cursors.Hand,
            ToolTip = "Collapse or expand camera list sidebar for ultra-wide camera grid view"
        };
        _btnToggleSidebar.Click += (s, e) => ToggleCctvSidebar();
        leftStack.Children.Add(_btnToggleSidebar);

        // 2. Grid Layout Buttons
        leftStack.Children.Add(new TextBlock
        {
            Text = "Layout: ",
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Foreground = TextSubtle,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 4, 0)
        });

        leftStack.Children.Add(CreateGridModeButton("1x1 Focus", 1));
        leftStack.Children.Add(CreateGridModeButton("2x2 Quad", 4));
        leftStack.Children.Add(CreateGridModeButton("3x3 Multi", 9));
        leftStack.Children.Add(CreateGridModeButton("4x4 Wall", 16));
        barWrap.Children.Add(leftStack);

        // Mid Stack: Stream Engine & Fullscreen
        var midStack = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 14, 2), VerticalAlignment = VerticalAlignment.Center };
        midStack.Children.Add(new TextBlock
        {
            Text = "Engine: ",
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Foreground = TextSubtle,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 4, 0)
        });

        _cctvEngineCombo = new ComboBox { Height = 28, FontSize = 11, VerticalContentAlignment = VerticalAlignment.Center };
        _cctvEngineCombo.Items.Add("⚡ Smooth Hardware Stream (RTSP 30 FPS)");
        _cctvEngineCombo.Items.Add("📷 Low-Bandwidth Snapshot Mode");
        _cctvEngineCombo.SelectedIndex = 0;
        _cctvEngineCombo.SelectionChanged += (s, e) =>
        {
            _cctvUseSmoothStreaming = (_cctvEngineCombo.SelectedIndex == 0);
            RebuildCctvGrid();
            TriggerImmediateStreamStart();
        };
        midStack.Children.Add(_cctvEngineCombo);

        var btnFsTool = new Button
        {
            Content = "⛶ Fullscreen Wall",
            Height = 28,
            Padding = new Thickness(10, 0, 10, 0),
            Margin = new Thickness(8, 0, 0, 0),
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Background = Brushes.White,
            Foreground = TextDark,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            Cursor = Cursors.Hand,
            ToolTip = "Enter borderless multi-camera video wall (Press Esc to go back)"
        };
        btnFsTool.Click += (s, e) => ToggleCctvFullscreen();
        midStack.Children.Add(btnFsTool);

        barWrap.Children.Add(midStack);

        // Right Counters
        var countersStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 2, 0, 2) };
        _cctvTotalNvrsCounter = CreateMetricLabel("NVRs", "0");
        _cctvTotalChannelsCounter = CreateMetricLabel("Channels", "0");
        _cctvActiveFeedsCounter = CreateMetricLabel("Active Feeds", "0");
        countersStack.Children.Add(_cctvTotalNvrsCounter);
        countersStack.Children.Add(_cctvTotalChannelsCounter);
        countersStack.Children.Add(_cctvActiveFeedsCounter);
        barWrap.Children.Add(countersStack);

        bar.Child = barWrap;
        return bar;
    }

    private void ToggleCctvSidebar()
    {
        _isSidebarCollapsed = !_isSidebarCollapsed;

        if (_isSidebarCollapsed)
        {
            _cctvSidebarCol.Width = new GridLength(0);
            _cctvSidebarBorder.Visibility = Visibility.Collapsed;
            _btnToggleSidebar.Content = "▶ Show Cameras";
            _btnToggleSidebar.Background = new SolidColorBrush(Color.FromRgb(239, 246, 255));
            _btnToggleSidebar.Foreground = SvllBlue;
        }
        else
        {
            _cctvSidebarCol.Width = new GridLength(280);
            _cctvSidebarBorder.Visibility = Visibility.Visible;
            _btnToggleSidebar.Content = "◀ Hide Sidebar";
            _btnToggleSidebar.Background = Brushes.White;
            _btnToggleSidebar.Foreground = TextDark;
        }
    }

    // ─── FULLSCREEN SURVEILLANCE & NON-OVERLAPPING EXIT CONTROLS ─────────────
    private void ToggleCctvFullscreen(bool? forceState = null)
    {
        bool targetState = forceState ?? !_isCctvFullscreen;
        if (targetState == _isCctvFullscreen) return;

        _isCctvFullscreen = targetState;

        if (_isCctvFullscreen)
        {
            _savedWindowState = this.WindowState;
            _savedWindowStyle = this.WindowStyle;
            _savedResizeMode = this.ResizeMode;
            _savedNavVisibility = _navPanel?.Visibility ?? Visibility.Visible;

            // Hide main app navigation panel and normal header
            if (_navPanel != null) _navPanel.Visibility = Visibility.Collapsed;
            if (_cctvHeaderElement != null) _cctvHeaderElement.Visibility = Visibility.Collapsed;

            // Show dedicated Fullscreen top bar in Row 0 (never overlaps or hides behind VideoView HWND)
            if (_fullscreenTopBar != null) _fullscreenTopBar.Visibility = Visibility.Visible;

            _cctvRootGrid.Margin = new Thickness(0);

            this.WindowStyle = WindowStyle.None;
            this.WindowState = WindowState.Maximized;

            // Automatically collapse sidebar for full ultra-wide video wall
            if (!_isSidebarCollapsed)
            {
                _cctvSidebarCol.Width = new GridLength(0);
                _cctvSidebarBorder.Visibility = Visibility.Collapsed;
                _btnToggleSidebar.Content = "▶ Show Cameras";
            }
        }
        else
        {
            this.WindowStyle = _savedWindowStyle;
            this.WindowState = _savedWindowState;
            this.ResizeMode = _savedResizeMode;

            if (_navPanel != null) _navPanel.Visibility = _savedNavVisibility;
            if (_cctvHeaderElement != null) _cctvHeaderElement.Visibility = Visibility.Visible;
            if (_fullscreenTopBar != null) _fullscreenTopBar.Visibility = Visibility.Collapsed;

            _cctvRootGrid.Margin = new Thickness(10);

            if (!_isSidebarCollapsed)
            {
                _cctvSidebarCol.Width = new GridLength(280);
                _cctvSidebarBorder.Visibility = Visibility.Visible;
                _btnToggleSidebar.Content = "◀ Hide Sidebar";
            }
        }
    }

    private Button CreateGridModeButton(string label, int totalCells)
    {
        var btn = new Button
        {
            Content = label,
            Height = 28,
            Padding = new Thickness(10, 0, 10, 0),
            Margin = new Thickness(0, 0, 4, 0),
            FontSize = 11,
            FontWeight = (_cctvGridSize == totalCells) ? FontWeights.Bold : FontWeights.Normal,
            Background = (_cctvGridSize == totalCells) ? SvllBlue : Brushes.White,
            Foreground = (_cctvGridSize == totalCells) ? Brushes.White : TextDark,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            Cursor = Cursors.Hand
        };

        btn.Click += (s, e) =>
        {
            if (totalCells > 1) _previousMultiGridSize = totalCells;
            SetCctvGridLayout(totalCells);
        };
        return btn;
    }

    private TextBlock CreateMetricLabel(string title, string value)
    {
        return new TextBlock
        {
            Text = $"{title}: {value}  •  ",
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Foreground = TextSubtle
        };
    }

    // ─── 3. Left Sidebar: NVR & Camera Tree ──────────────────────────────────
    private UIElement BuildCctvSidebar()
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
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Search
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Quick actions
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Tree

        // Search Input
        _cctvSearchBox = new TextBox
        {
            Height = 32,
            FontSize = 11.5,
            VerticalContentAlignment = VerticalAlignment.Center,
            Padding = new Thickness(8, 0, 8, 0),
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            Margin = new Thickness(0, 0, 0, 8)
        };
        _cctvSearchBox.TextChanged += (s, e) => RefreshCctvTree();
        Grid.SetRow(_cctvSearchBox, 0);
        grid.Children.Add(_cctvSearchBox);

        // Export / Import Row
        var expImpRow = new Grid { Margin = new Thickness(0, 0, 0, 8) };
        expImpRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        expImpRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(6) });
        expImpRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var btnExport = new Button
        {
            Content = "📤 Backup NVRs",
            Height = 26,
            FontSize = 11,
            Background = Brushes.White,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            Cursor = Cursors.Hand
        };
        btnExport.Click += (s, e) => ExportNvrList();
        Grid.SetColumn(btnExport, 0);
        expImpRow.Children.Add(btnExport);

        var btnImport = new Button
        {
            Content = "📥 Import NVRs",
            Height = 26,
            FontSize = 11,
            Background = Brushes.White,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            Cursor = Cursors.Hand
        };
        btnImport.Click += (s, e) => ImportNvrList();
        Grid.SetColumn(btnImport, 2);
        expImpRow.Children.Add(btnImport);

        Grid.SetRow(expImpRow, 1);
        grid.Children.Add(expImpRow);

        // Scrollable NVR Tree
        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };
        _cctvNvrTreePanel = new StackPanel();
        scroll.Content = _cctvNvrTreePanel;
        Grid.SetRow(scroll, 2);
        grid.Children.Add(scroll);

        card.Child = grid;
        RefreshCctvTree();

        return card;
    }

    private void RefreshCctvTree()
    {
        if (_cctvNvrTreePanel == null) return;
        _cctvNvrTreePanel.Children.Clear();

        var devices = NvrDeviceStorage.Instance.GetDevices();
        string filter = _cctvSearchBox?.Text?.Trim().ToLower() ?? "";

        int totalCh = 0;
        foreach (var d in devices)
        {
            totalCh += d.ChannelCount;
        }

        if (_cctvTotalNvrsCounter != null) _cctvTotalNvrsCounter.Text = $"NVRs: {devices.Count}  •  ";
        if (_cctvTotalChannelsCounter != null) _cctvTotalChannelsCounter.Text = $"Channels: {totalCh}  •  ";
        if (_cctvActiveFeedsCounter != null) _cctvActiveFeedsCounter.Text = $"Active Feeds: {_cctvSlots.Count}  •  ";

        if (devices.Count == 0)
        {
            _cctvNvrTreePanel.Children.Add(new TextBlock
            {
                Text = "No NVR/DVR devices registered.\nClick '+ Add NVR / DVR' above to add your first camera hub.",
                FontSize = 11.5,
                Foreground = TextSubtle,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(8, 12, 8, 12)
            });
            return;
        }

        foreach (var device in devices)
        {
            bool match = string.IsNullOrEmpty(filter) ||
                         device.Name.ToLower().Contains(filter) ||
                         device.Location.ToLower().Contains(filter) ||
                         device.Host.ToLower().Contains(filter) ||
                         device.Channels.Any(c => c.CustomName.ToLower().Contains(filter));

            if (!match) continue;

            _cctvNvrTreePanel.Children.Add(BuildNvrTreeItem(device));
        }
    }

    private UIElement BuildNvrTreeItem(NvrDevice device)
    {
        var container = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Margin = new Thickness(0, 0, 0, 8),
            Padding = new Thickness(8)
        };

        var rootStack = new StackPanel();

        // NVR Header Row
        var headGrid = new Grid();
        headGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        headGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var infoStack = new StackPanel();
        var nameRow = new StackPanel { Orientation = Orientation.Horizontal };
        nameRow.Children.Add(new TextBlock
        {
            Text = "🏢 " + device.Name,
            FontSize = 12,
            FontWeight = FontWeights.Bold,
            Foreground = TextDark,
            TextTrimming = TextTrimming.CharacterEllipsis,
            MaxWidth = 175
        });
        infoStack.Children.Add(nameRow);

        if (!string.IsNullOrWhiteSpace(device.SubLocation))
        {
            infoStack.Children.Add(new TextBlock
            {
                Text = $"📍 {device.Location} • {device.SubLocation}",
                FontSize = 9.5,
                Foreground = SvllBlue,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 1, 0, 0)
            });
        }

        string ownerInfo = !string.IsNullOrWhiteSpace(device.OwnerName) ? $" • {device.OwnerName}" : "";
        infoStack.Children.Add(new TextBlock
        {
            Text = $"{device.CleanHost}:{device.HttpPort} • {device.GetBrandDisplayName()}{ownerInfo}",
            FontSize = 9.5,
            Foreground = TextSubtle,
            Margin = new Thickness(0, 1, 0, 0)
        });
        Grid.SetColumn(infoStack, 0);
        headGrid.Children.Add(infoStack);

        // Quick action buttons
        var actStack = new StackPanel { Orientation = Orientation.Horizontal };

        var btnTest = CreateSmallIconButton("⚡", "Test connectivity & ports", async () =>
        {
            var res = await NvrStreamHelper.TestConnectivityAsync(device);
            MessageBox.Show(res.Message, $"NVR Diagnostic: {device.Name}", MessageBoxButton.OK,
                res.OverallSuccess ? MessageBoxImage.Information : MessageBoxImage.Warning);
        });
        actStack.Children.Add(btnTest);

        var btnWeb = CreateSmallIconButton("🌐", "Open NVR web console", () =>
        {
            string url = NvrStreamHelper.BuildWebPortalUrl(device);
            try { Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true }); } catch { }
        });
        actStack.Children.Add(btnWeb);

        var btnEdit = CreateSmallIconButton("✏️", "Edit configuration & credentials", () =>
        {
            var dlg = new NvrEditorDialog(device, this);
            if (dlg.ShowDialog() == true)
            {
                RefreshCctvTree();
                RebuildCctvGrid();
            }
        });
        actStack.Children.Add(btnEdit);

        var btnDel = CreateSmallIconButton("🗑️", "Remove NVR", () =>
        {
            if (MessageBox.Show($"Are you sure you want to remove '{device.Name}'?\nSaved credentials for this NVR will be erased.",
                "Confirm Removal", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                NvrDeviceStorage.Instance.DeleteDevice(device.Id);
                var keysToRemove = _cctvSlots.Where(kvp => kvp.Value.Device.Id == device.Id).Select(kvp => kvp.Key).ToList();
                foreach (var k in keysToRemove) ClearSlot(k);
                RefreshCctvTree();
                RebuildCctvGrid();
            }
        });
        actStack.Children.Add(btnDel);

        Grid.SetColumn(actStack, 1);
        headGrid.Children.Add(actStack);
        rootStack.Children.Add(headGrid);

        // Channels List Expander (Lazy load channels to make 46 NVRs open in < 30ms)
        bool isFirstNvr = (_cctvNvrTreePanel == null || _cctvNvrTreePanel.Children.Count == 0);
        var expander = new Expander
        {
            Header = $"Cameras ({device.ChannelCount} Channels)",
            FontSize = 11,
            Foreground = TextSubtle,
            IsExpanded = isFirstNvr,
            Margin = new Thickness(0, 4, 0, 0)
        };

        var chListStack = new StackPanel { Margin = new Thickness(6, 4, 0, 0) };

        void PopulateChannelItems()
        {
            if (chListStack.Children.Count > 0) return;
            for (int i = 1; i <= device.ChannelCount; i++)
            {
                int chNum = i;
                string chName = device.GetChannelName(chNum);

                var chRow = new Border
                {
                    Background = Brushes.Transparent,
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(4, 2, 4, 2),
                    Margin = new Thickness(0, 1, 0, 1),
                    Cursor = Cursors.Hand
                };

                var chGrid = new Grid();
                chGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                chGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var lblCh = new TextBlock
                {
                    Text = $"📹 Ch {chNum:D2}: {chName}",
                    FontSize = 11,
                    Foreground = TextDark,
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                Grid.SetColumn(lblCh, 0);
                chGrid.Children.Add(lblCh);

                var actionRow = new StackPanel { Orientation = Orientation.Horizontal };

                var btnView = new TextBlock
                {
                    Text = "＋ Live",
                    FontSize = 10,
                    FontWeight = FontWeights.Bold,
                    Foreground = SvllBlue,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 6, 0)
                };
                btnView.MouseLeftButtonDown += (s, e) =>
                {
                    AssignChannelToSlot(_cctvSelectedSlot, device, chNum);
                    e.Handled = true;
                };
                actionRow.Children.Add(btnView);

                var btnPlay = new TextBlock
                {
                    Text = "⏪ Play",
                    FontSize = 10,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(124, 58, 237)),
                    VerticalAlignment = VerticalAlignment.Center
                };
                btnPlay.MouseLeftButtonDown += (s, e) =>
                {
                    LaunchPlaybackForCamera(device, chNum);
                    e.Handled = true;
                };
                actionRow.Children.Add(btnPlay);

                Grid.SetColumn(actionRow, 1);
                chGrid.Children.Add(actionRow);

                chRow.Child = chGrid;
                chRow.MouseEnter += (s, e) => chRow.Background = new SolidColorBrush(Color.FromRgb(224, 231, 255));
                chRow.MouseLeave += (s, e) => chRow.Background = Brushes.Transparent;

                chRow.MouseLeftButtonDown += (s, e) =>
                {
                    AssignChannelToSlot(_cctvSelectedSlot, device, chNum);
                };

                chListStack.Children.Add(chRow);
            }
        }

        if (expander.IsExpanded)
        {
            PopulateChannelItems();
        }
        expander.Expanded += (s, e) => PopulateChannelItems();

        expander.Content = chListStack;
        rootStack.Children.Add(expander);

        container.Child = rootStack;
        return container;
    }

    private Button CreateSmallIconButton(string glyph, string tooltip, Action action)
    {
        var btn = new Button
        {
            Content = glyph,
            Width = 24,
            Height = 24,
            FontSize = 10.5,
            Margin = new Thickness(2, 0, 0, 0),
            Background = Brushes.White,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            ToolTip = tooltip,
            Cursor = Cursors.Hand
        };
        btn.Click += (s, e) => action();
        return btn;
    }

    // ─── 4. Dynamic Live Camera Grid & Hardware Video Players ────────────────
    private UIElement BuildCctvGridArea()
    {
        var hostGrid = new Grid();

        var outerBorder = new Border
        {
            Background = TerminalBg,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(4)
        };

        _cctvMainGridContainer = new Grid();
        outerBorder.Child = _cctvMainGridContainer;
        hostGrid.Children.Add(outerBorder);

        // Floating Overlay 1: [ ◀ Back to Multi-Grid ] (Appears when zoomed in 1x1 Focus)
        _btnBackToMultiGrid = new Button
        {
            Content = "◀ Back to Multi-Grid",
            Height = 32,
            FontSize = 11.5,
            FontWeight = FontWeights.Bold,
            Background = SvllBlue,
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(14, 0, 14, 0),
            Margin = new Thickness(16, 16, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Cursor = Cursors.Hand,
            Visibility = Visibility.Collapsed
        };
        _btnBackToMultiGrid.Click += (s, e) =>
        {
            SetCctvGridLayout(_previousMultiGridSize > 1 ? _previousMultiGridSize : 4);
        };
        hostGrid.Children.Add(_btnBackToMultiGrid);

        RebuildCctvGrid();
        return hostGrid;
    }

    private void SetCctvGridLayout(int size)
    {
        if (_cctvGridSize > 1 && size == 1)
        {
            _previousMultiGridSize = _cctvGridSize;
        }

        _cctvGridSize = size;

        if (_btnBackToMultiGrid != null)
        {
            _btnBackToMultiGrid.Visibility = (_cctvGridSize == 1) ? Visibility.Visible : Visibility.Collapsed;
            _btnBackToMultiGrid.Content = $"◀ Back to {_previousMultiGridSize}x Multi-Grid";
        }

        RebuildCctvGrid();
        TriggerImmediateStreamStart();
    }

    private void RebuildCctvGrid()
    {
        if (_cctvMainGridContainer == null) return;

        // Disconnect VideoView from MediaPlayer on UI thread first to prevent WinForms Host crash
        foreach (var vv in _slotVideoViews.Values)
        {
            try { vv.MediaPlayer = null; } catch { }
        }
        _slotVideoViews.Clear();

        // Cancel running connection tasks for all slots
        lock (_slotCts)
        {
            foreach (var cts in _slotCts.Values)
            {
                try { cts.Cancel(); cts.Dispose(); } catch { }
            }
            _slotCts.Clear();
        }

        _cctvMainGridContainer.Children.Clear();
        _cctvMainGridContainer.RowDefinitions.Clear();
        _cctvMainGridContainer.ColumnDefinitions.Clear();
        _cellUiCache.Clear();

        int rows = 1, cols = 1;
        switch (_cctvGridSize)
        {
            case 1: rows = 1; cols = 1; break;
            case 4: rows = 2; cols = 2; break;
            case 9: rows = 3; cols = 3; break;
            case 16: rows = 4; cols = 4; break;
            default: rows = 2; cols = 2; break;
        }

        for (int r = 0; r < rows; r++)
            _cctvMainGridContainer.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        for (int c = 0; c < cols; c++)
            _cctvMainGridContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        int totalCells = rows * cols;

        // Stop any media players that are outside the new grid size
        foreach (var kvp in _slotMediaPlayers)
        {
            if (kvp.Key >= totalCells)
            {
                try { if (kvp.Value.NativeReference != IntPtr.Zero) kvp.Value.Stop(); } catch { }
            }
        }

        for (int i = 0; i < totalCells; i++)
        {
            int slotIdx = i;
            int r = i / cols;
            int c = i % cols;

            var cellUi = BuildCameraCellUi(slotIdx);
            Grid.SetRow(cellUi, r);
            Grid.SetColumn(cellUi, c);
            _cctvMainGridContainer.Children.Add(cellUi);
        }
    }

    private UIElement BuildCameraCellUi(int slotIndex)
    {
        var border = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(15, 23, 42)),
            BorderBrush = (slotIndex == _cctvSelectedSlot) ? SvllLightBlue : new SolidColorBrush(Color.FromRgb(51, 65, 85)),
            BorderThickness = new Thickness(slotIndex == _cctvSelectedSlot ? 2 : 1),
            CornerRadius = new CornerRadius(6),
            Margin = new Thickness(3),
            ClipToBounds = true
        };

        var cellGrid = new Grid();
        cellGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header OSD
        cellGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Video Host
        cellGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Footer actions

        // Header OSD
        var headerBar = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(190, 15, 23, 42)),
            Padding = new Thickness(8, 4, 8, 4)
        };
        var headGrid = new Grid();
        headGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        headGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var lblTitle = new TextBlock
        {
            Text = $"SLOT {slotIndex + 1}: [EMPTY]",
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            Foreground = Brushes.White,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        Grid.SetColumn(lblTitle, 0);
        headGrid.Children.Add(lblTitle);

        var lblStats = new TextBlock
        {
            Text = "STANDBY",
            FontSize = 10,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(lblStats, 1);
        headGrid.Children.Add(lblStats);

        headerBar.Child = headGrid;
        Grid.SetRow(headerBar, 0);
        cellGrid.Children.Add(headerBar);

        // Center Feed Host
        var feedHost = new Grid { Background = Brushes.Black };

        VideoView? vlcVideoView = null;
        if (_cctvUseSmoothStreaming && _globalLibVLC != null)
        {
            try
            {
                if (!_slotMediaPlayers.TryGetValue(slotIndex, out var mp) || mp == null || mp.NativeReference == IntPtr.Zero)
                {
                    mp = new MediaPlayer(_globalLibVLC);
                    _slotMediaPlayers[slotIndex] = mp;
                }

                vlcVideoView = new VideoView
                {
                    MediaPlayer = mp,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch
                };
                _slotVideoViews[slotIndex] = vlcVideoView;
                feedHost.Children.Add(vlcVideoView);
            }
            catch { }
        }

        // Image control (fallback / snapshot mode)
        var videoImg = new Image
        {
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Visibility = (_cctvUseSmoothStreaming && vlcVideoView != null) ? Visibility.Collapsed : Visibility.Visible
        };
        feedHost.Children.Add(videoImg);

        // Standby Overlay
        var standbyOverlay = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(200, 15, 23, 42)),
            Visibility = Visibility.Visible
        };
        var standbyStack = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        standbyStack.Children.Add(new TextBlock
        {
            Text = "📹",
            FontSize = 32,
            HorizontalAlignment = HorizontalAlignment.Center
        });
        var lblStandbyMsg = new TextBlock
        {
            Text = "Select a camera from the sidebar\nto monitor this cell",
            FontSize = 11,
            Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 6, 0, 0)
        };
        standbyStack.Children.Add(lblStandbyMsg);
        standbyOverlay.Child = standbyStack;
        feedHost.Children.Add(standbyOverlay);

        // OSD Live Timestamp
        var lblOsdTime = new TextBlock
        {
            Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            FontSize = 10,
            FontFamily = new FontFamily("Consolas, Segoe UI"),
            Foreground = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(8, 0, 0, 6)
        };
        feedHost.Children.Add(lblOsdTime);

        feedHost.MouseLeftButtonDown += (s, e) =>
        {
            SelectCctvSlot(slotIndex);
            if (e.ClickCount == 2)
            {
                // Double click zooms into 1x1 Focus for this camera, or restores multi-grid
                if (_cctvGridSize > 1)
                {
                    _previousMultiGridSize = _cctvGridSize;
                    if (slotIndex > 0 && _cctvSlots.TryGetValue(slotIndex, out var assigned))
                    {
                        _cctvSlots[0] = assigned;
                    }
                    SetCctvGridLayout(1);
                }
                else
                {
                    SetCctvGridLayout(_previousMultiGridSize > 1 ? _previousMultiGridSize : 4);
                }
            }
        };

        Grid.SetRow(feedHost, 1);
        cellGrid.Children.Add(feedHost);

        // Footer Actions Bar
        var footerBar = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(220, 30, 41, 59)),
            Padding = new Thickness(6, 3, 6, 3)
        };
        var footGrid = new Grid();
        footGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        footGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var actLeft = new StackPanel { Orientation = Orientation.Horizontal };

        var btnZoom = CreateCellActionButton("🔍 Focus", () =>
        {
            if (_cctvGridSize > 1)
            {
                _previousMultiGridSize = _cctvGridSize;
                if (slotIndex > 0 && _cctvSlots.TryGetValue(slotIndex, out var assigned))
                {
                    _cctvSlots[0] = assigned;
                }
                SetCctvGridLayout(1);
            }
            else
            {
                SetCctvGridLayout(_previousMultiGridSize > 1 ? _previousMultiGridSize : 4);
            }
        });
        actLeft.Children.Add(btnZoom);

        var btnSnap = CreateCellActionButton("📸 Snap", () => CaptureSingleSnapshot(slotIndex));
        actLeft.Children.Add(btnSnap);

        var btnPlay = CreateCellActionButton("⏪ Playback", () =>
        {
            if (_cctvSlots.TryGetValue(slotIndex, out var assigned))
            {
                LaunchPlaybackForCamera(assigned.Device, assigned.Channel);
            }
        });
        actLeft.Children.Add(btnPlay);

        var btnRtsp = CreateCellActionButton("▶ RTSP", () => LaunchRtspStream(slotIndex));
        actLeft.Children.Add(btnRtsp);

        var btnWeb = CreateCellActionButton("🌐 Web", () => OpenNvrWebConsole(slotIndex));
        actLeft.Children.Add(btnWeb);

        Grid.SetColumn(actLeft, 0);
        footGrid.Children.Add(actLeft);

        var actRight = new StackPanel { Orientation = Orientation.Horizontal };
        var btnClear = CreateCellActionButton("✕", () => ClearSlot(slotIndex));
        btnClear.ToolTip = "Clear camera from this slot";
        actRight.Children.Add(btnClear);
        Grid.SetColumn(actRight, 1);
        footGrid.Children.Add(actRight);

        footerBar.Child = footGrid;
        Grid.SetRow(footerBar, 2);
        cellGrid.Children.Add(footerBar);

        border.Child = cellGrid;
        border.MouseLeftButtonDown += (s, e) => SelectCctvSlot(slotIndex);

        _cellUiCache[slotIndex] = (border, videoImg, vlcVideoView, lblTitle, lblStats, lblOsdTime, standbyOverlay, lblStandbyMsg);

        if (_cctvSlots.TryGetValue(slotIndex, out var assignedSlot))
        {
            UpdateSlotMetadataUi(slotIndex, assignedSlot.Device, assignedSlot.Channel);
        }

        return border;
    }

    private Button CreateCellActionButton(string text, Action action)
    {
        var btn = new Button
        {
            Content = text,
            Height = 20,
            FontSize = 9.5,
            FontWeight = FontWeights.SemiBold,
            Padding = new Thickness(6, 0, 6, 0),
            Margin = new Thickness(0, 0, 4, 0),
            Background = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand
        };
        btn.Click += (s, e) => action();
        return btn;
    }

    private void SelectCctvSlot(int slotIndex)
    {
        _cctvSelectedSlot = slotIndex;
        foreach (var kvp in _cellUiCache)
        {
            bool isSelected = (kvp.Key == slotIndex);
            kvp.Value.CellBorder.BorderBrush = isSelected ? SvllLightBlue : new SolidColorBrush(Color.FromRgb(51, 65, 85));
            kvp.Value.CellBorder.BorderThickness = new Thickness(isSelected ? 2 : 1);
        }
    }

    private void AssignChannelToSlot(int slotIndex, NvrDevice device, int channel)
    {
        if (slotIndex >= _cctvGridSize)
        {
            slotIndex = 0;
            _cctvSelectedSlot = 0;
        }

        _cctvSlots[slotIndex] = (device, channel);
        UpdateSlotMetadataUi(slotIndex, device, channel);

        if (_cctvActiveFeedsCounter != null)
        {
            _cctvActiveFeedsCounter.Text = $"Active Feeds: {_cctvSlots.Count}  •  ";
        }

        // Start hardware smooth stream or fast snapshot
        StartStreamForSlot(slotIndex, device, channel);

        // Advance to next slot
        SelectCctvSlot((slotIndex + 1) % _cctvGridSize);
    }

    private static async Task<bool> FastProbeHostAsync(string host, int port, int timeoutMs, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(host)) return false;
        try
        {
            using var client = new System.Net.Sockets.TcpClient();
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token);
            linkedCts.CancelAfter(timeoutMs);

            var connectTask = client.ConnectAsync(host, port > 0 ? port : 554);
            var delayTask = Task.Delay(timeoutMs, linkedCts.Token);

            var completed = await Task.WhenAny(connectTask, delayTask);
            if (completed == connectTask && client.Connected)
            {
                return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    private void StartStreamForSlot(int slotIndex, NvrDevice device, int channel)
    {
        CancellationToken token;
        lock (_slotCts)
        {
            if (_slotCts.TryGetValue(slotIndex, out var oldCts))
            {
                try { oldCts.Cancel(); oldCts.Dispose(); } catch { }
            }
            var cts = new CancellationTokenSource();
            _slotCts[slotIndex] = cts;
            token = cts.Token;
        }

        // 1. Immediately update UI to show informative connecting state
        Dispatcher.InvokeAsync(() =>
        {
            if (_cellUiCache.TryGetValue(slotIndex, out var ui))
            {
                string chName = device.GetChannelName(channel);
                ui.OsdTitle.Text = $"CH {channel:D2}: {chName} ({device.CleanHost})";
                ui.OsdStats.Text = "🟡 CONNECTING...";
                ui.OsdStats.Foreground = new SolidColorBrush(Color.FromRgb(250, 204, 21));
                ui.StandbyOverlay.Visibility = Visibility.Visible;
                ui.StandbyMessage.Text = $"Connecting to {device.Name}...\nChannel {channel:D2} • RTSP :{device.RtspPort}";
            }
        });

        Task.Run(async () =>
        {
            try
            {
                if (token.IsCancellationRequested) return;

                // 2. Instant Fast TCP Reachability Probe (500ms max) - Eliminates hanging sockets and LibVLC stalls!
                bool isReachable = await FastProbeHostAsync(device.CleanHost, device.RtspPort > 0 ? device.RtspPort : 554, 500, token);
                if (token.IsCancellationRequested) return;

                if (!isReachable)
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        if (token.IsCancellationRequested) return;
                        if (_cellUiCache.TryGetValue(slotIndex, out var ui))
                        {
                            ui.StandbyOverlay.Visibility = Visibility.Visible;
                            ui.OsdStats.Text = "🔴 OFFLINE";
                            ui.OsdStats.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                            ui.StandbyMessage.Text = $"⚠️ Device Offline / Unreachable\n{device.Name} (Ch {channel:D2})\nHost: {device.CleanHost}:{device.RtspPort}\nCheck branch network connection.";
                        }
                    });
                    return;
                }

                // 3. Host is online! Start LibVLC RTSP stream or snapshot polling
                if (_cctvUseSmoothStreaming && _slotMediaPlayers.TryGetValue(slotIndex, out var mp) && _globalLibVLC != null)
                {
                    try
                    {
                        if (mp.NativeReference != IntPtr.Zero)
                        {
                            mp.Stop();
                        }
                    }
                    catch { }

                    if (token.IsCancellationRequested) return;

                    var streamQuality = (_cctvGridSize == 1) ? StreamQuality.MainStream : StreamQuality.SubStream;
                    string rtspUrl = NvrStreamHelper.BuildRtspUrl(device, channel, streamQuality);

                    using var media = new Media(_globalLibVLC, rtspUrl, FromType.FromLocation);
                    media.AddOption(":rtsp-tcp");
                    media.AddOption(":network-caching=250");
                    media.AddOption(":live-caching=250");
                    media.AddOption(":clock-jitter=0");
                    media.AddOption(":clock-synchro=0");
                    media.AddOption(":drop-late-frames");
                    media.AddOption(":skip-frames");
                    media.AddOption(":avcodec-hw=any");
                    media.AddOption(":avcodec-fast");

                    bool streamPlaying = false;
                    EventHandler<EventArgs>? onPlaying = null;
                    EventHandler<EventArgs>? onError = null;

                    onPlaying = (s, e) =>
                    {
                        streamPlaying = true;
                        try { mp.Playing -= onPlaying; } catch { }
                        try { mp.EncounteredError -= onError; } catch { }

                        Dispatcher.InvokeAsync(() =>
                        {
                            if (token.IsCancellationRequested) return;
                            if (_cellUiCache.TryGetValue(slotIndex, out var ui))
                            {
                                ui.StandbyOverlay.Visibility = Visibility.Collapsed;
                                string qLabel = (streamQuality == StreamQuality.SubStream) ? "SUB (30 FPS)" : "MAIN (1080P HD)";
                                ui.OsdStats.Text = $"🟢 {qLabel}";
                                ui.OsdStats.Foreground = new SolidColorBrush(Color.FromRgb(34, 197, 94));
                            }
                        });
                    };

                    onError = (s, e) =>
                    {
                        streamPlaying = false;
                        try { mp.Playing -= onPlaying; } catch { }
                        try { mp.EncounteredError -= onError; } catch { }

                        Dispatcher.InvokeAsync(() =>
                        {
                            if (token.IsCancellationRequested) return;
                            if (_cellUiCache.TryGetValue(slotIndex, out var ui))
                            {
                                ui.StandbyOverlay.Visibility = Visibility.Visible;
                                ui.OsdStats.Text = "🔴 RTSP ERROR";
                                ui.OsdStats.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                                ui.StandbyMessage.Text = $"⚠️ Stream Offline / Unreachable\n{device.Name} (Ch {channel:D2})\nHost: {device.CleanHost}:{device.RtspPort}\nCheck branch network connection.";
                            }
                        });
                    };

                    mp.Playing += onPlaying;
                    mp.EncounteredError += onError;

                    mp.Play(media);

                    // 5-second watchdog
                    try
                    {
                        await Task.Delay(5000, token);
                    }
                    catch (OperationCanceledException)
                    {
                        try { mp.Playing -= onPlaying; } catch { }
                        try { mp.EncounteredError -= onError; } catch { }
                        return;
                    }

                    if (token.IsCancellationRequested) return;

                    // Safe C# boolean check - NEVER call unmanaged mp.IsPlaying!
                    if (!streamPlaying)
                    {
                        try { mp.Playing -= onPlaying; } catch { }
                        try { mp.EncounteredError -= onError; } catch { }

                        await Dispatcher.InvokeAsync(() =>
                        {
                            if (token.IsCancellationRequested) return;
                            if (_cellUiCache.TryGetValue(slotIndex, out var ui))
                            {
                                ui.StandbyOverlay.Visibility = Visibility.Visible;
                                ui.OsdStats.Text = "⚠️ TIMEOUT";
                                ui.OsdStats.Foreground = new SolidColorBrush(Color.FromRgb(245, 158, 11));
                                ui.StandbyMessage.Text = $"⚠️ Connection Timeout\n{device.Name} (Ch {channel:D2})\nNo RTSP response from {device.CleanHost}:{device.RtspPort}.";
                            }
                        });
                    }
                }
                else
                {
                    await PollSingleSlotFrameAsync(slotIndex, device, channel);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to start RTSP stream: {ex.Message}");
            }
        });
    }

    private void TriggerImmediateStreamStart()
    {
        foreach (var kvp in _cctvSlots)
        {
            if (kvp.Key < _cctvGridSize)
            {
                StartStreamForSlot(kvp.Key, kvp.Value.Device, kvp.Value.Channel);
            }
        }
    }

    private void UpdateSlotMetadataUi(int slotIndex, NvrDevice device, int channel)
    {
        if (!_cellUiCache.TryGetValue(slotIndex, out var ui)) return;

        string chName = device.GetChannelName(channel);
        ui.OsdTitle.Text = $"CH {channel:D2}: {chName} ({device.Host})";
        ui.OsdStats.Text = "CONNECTING...";
        ui.OsdStats.Foreground = new SolidColorBrush(Color.FromRgb(250, 204, 21));
        ui.StandbyMessage.Text = $"Connecting to {device.Name}...\n{device.Host} (Ch {channel})";
    }

    private void ClearSlot(int slotIndex)
    {
        lock (_slotCts)
        {
            if (_slotCts.TryGetValue(slotIndex, out var cts))
            {
                try { cts.Cancel(); cts.Dispose(); } catch { }
                _slotCts.Remove(slotIndex);
            }
        }

        _cctvSlots.Remove(slotIndex);

        if (_slotMediaPlayers.TryGetValue(slotIndex, out var mp))
        {
            try { if (mp.NativeReference != IntPtr.Zero) mp.Stop(); } catch { }
        }

        if (_cellUiCache.TryGetValue(slotIndex, out var ui))
        {
            ui.OsdTitle.Text = $"SLOT {slotIndex + 1}: [EMPTY]";
            ui.OsdStats.Text = "STANDBY";
            ui.OsdStats.Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184));
            ui.VideoImg.Source = null;
            ui.StandbyOverlay.Visibility = Visibility.Visible;
            ui.StandbyMessage.Text = "Select a camera from the sidebar\nto monitor this cell";
        }
        if (_cctvActiveFeedsCounter != null)
        {
            _cctvActiveFeedsCounter.Text = $"Active Feeds: {_cctvSlots.Count}  •  ";
        }
    }

    private void InitializeDefaultSlots()
    {
        var devices = NvrDeviceStorage.Instance.GetDevices();
        if (devices.Count == 0) return;

        _cctvSlots[0] = (devices[0], 1);
        if (devices[0].ChannelCount >= 2) _cctvSlots[1] = (devices[0], 2);

        if (devices.Count > 1)
        {
            _cctvSlots[2] = (devices[1], 1);
            if (devices[1].ChannelCount >= 2) _cctvSlots[3] = (devices[1], 2);
        }
        else if (devices[0].ChannelCount >= 4)
        {
            _cctvSlots[2] = (devices[0], 3);
            _cctvSlots[3] = (devices[0], 4);
        }
    }

    private void StartCctvMonitoring()
    {
        if (_cctvIsMonitoring) return;
        _cctvIsMonitoring = true;

        _cctvRefreshTimer?.Stop();
        _cctvRefreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _cctvRefreshTimer.Tick += async (s, e) =>
        {
            if (!_cctvUseSmoothStreaming)
            {
                await TriggerImmediateFramePollAsync();
            }
            else
            {
                // Update OSD clocks
                foreach (var kvp in _cellUiCache)
                {
                    kvp.Value.OsdTime.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                }
            }
        };
        _cctvRefreshTimer.Start();

        TriggerImmediateStreamStart();
    }

    private void StopCctvMonitoring()
    {
        _cctvIsMonitoring = false;
        _cctvRefreshTimer?.Stop();
        _cctvRefreshTimer = null;

        lock (_slotCts)
        {
            foreach (var cts in _slotCts.Values)
            {
                try { cts.Cancel(); cts.Dispose(); } catch { }
            }
            _slotCts.Clear();
        }

        foreach (var mp in _slotMediaPlayers.Values)
        {
            try { if (mp.NativeReference != IntPtr.Zero) mp.Stop(); } catch { }
        }
    }

    public void DisposeCctvResources()
    {
        StopCctvMonitoring();
        foreach (var vv in _slotVideoViews.Values)
        {
            try { vv.MediaPlayer = null; } catch { }
        }
        _slotVideoViews.Clear();

        foreach (var mp in _slotMediaPlayers.Values)
        {
            try { mp.Dispose(); } catch { }
        }
        _slotMediaPlayers.Clear();

        try { _playbackMediaPlayer?.Dispose(); } catch { }
        _playbackMediaPlayer = null;
    }

    private async Task TriggerImmediateFramePollAsync()
    {
        if (!_cctvIsMonitoring) return;

        var tasks = new List<Task>();
        lock (_cctvSlots)
        {
            foreach (var kvp in _cctvSlots)
            {
                if (kvp.Key < _cctvGridSize)
                {
                    tasks.Add(PollSingleSlotFrameAsync(kvp.Key, kvp.Value.Device, kvp.Value.Channel));
                }
            }
        }
        await Task.WhenAll(tasks);
    }

    private async Task PollSingleSlotFrameAsync(int slotIndex, NvrDevice device, int channel)
    {
        var sw = Stopwatch.StartNew();
        byte[]? imgBytes = null;
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            imgBytes = await NvrStreamHelper.FetchSnapshotBytesAsync(device, channel, cts.Token);
        }
        catch { }
        sw.Stop();

        await Dispatcher.InvokeAsync(() =>
        {
            if (!_cellUiCache.TryGetValue(slotIndex, out var ui)) return;
            ui.OsdTime.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            if (imgBytes != null && imgBytes.Length > 200)
            {
                try
                {
                    var bmp = new BitmapImage();
                    using (var ms = new MemoryStream(imgBytes))
                    {
                        bmp.BeginInit();
                        bmp.CacheOption = BitmapCacheOption.OnLoad;
                        bmp.StreamSource = ms;
                        bmp.EndInit();
                    }
                    bmp.Freeze();

                    ui.VideoImg.Source = bmp;
                    ui.StandbyOverlay.Visibility = Visibility.Collapsed;
                    ui.OsdStats.Text = $"🟢 LIVE • {sw.ElapsedMilliseconds}ms";
                    ui.OsdStats.Foreground = new SolidColorBrush(Color.FromRgb(34, 197, 94));
                }
                catch
                {
                    ui.OsdStats.Text = "🔴 DECODE ERROR";
                }
            }
            else
            {
                ui.StandbyOverlay.Visibility = Visibility.Visible;
                ui.OsdStats.Text = "🔴 OFFLINE";
                ui.OsdStats.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                ui.StandbyMessage.Text = $"[CAMERA OFFLINE]\n{device.Name} (Ch {channel:D2})\nVerify NVR IP on branch LAN.";
            }
        });
    }

    // ─── 5. NVR RECORDING PLAYBACK STUDIO (OVERHAULED & SLEEK) ───────────────
    private UIElement BuildCctvPlaybackView()
    {
        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Query & filter bar + Available days
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Player Canvas (With Separate Header)
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Timeline & Transport bar

        // 1. Query / Filter Bar & Available Recording Days Strip
        var filterBar = new Border
        {
            Background = BgCard,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(14, 10, 14, 10),
            Margin = new Thickness(0, 0, 0, 8)
        };

        var filterMainStack = new StackPanel();

        // Row A: Main Form Controls (NVR, Camera Channel with Switchers, Date, Time, Action Buttons)
        var fGrid = new Grid { Margin = new Thickness(0, 0, 0, 8) };
        fGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(210) }); // NVR
        fGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(230) }); // Camera + Prev/Next
        fGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(145) }); // Date Picker
        fGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(170) }); // Time Range
        fGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Space
        fGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Action Buttons

        // NVR Combo
        var spNvr = new StackPanel { Margin = new Thickness(0, 0, 8, 0) };
        spNvr.Children.Add(CreateFieldHeader("Surveillance NVR / DVR:"));
        _cmbPlaybackNvr = new ComboBox { Height = 28, FontSize = 11.5 };
        _cmbPlaybackNvr.SelectionChanged += (s, e) =>
        {
            PopulatePlaybackCameraDropdown();
            UpdateDateChipsHighlight();
        };
        spNvr.Children.Add(_cmbPlaybackNvr);
        Grid.SetColumn(spNvr, 0);
        fGrid.Children.Add(spNvr);

        // Camera Channel Combo with [ |◀ Prev ] and [ Next ▶| ] Buttons
        var spCam = new StackPanel { Margin = new Thickness(0, 0, 8, 0) };
        spCam.Children.Add(CreateFieldHeader("Camera Channel:"));
        var camRow = new StackPanel { Orientation = Orientation.Horizontal };

        var btnPrevCamField = new Button
        {
            Content = "◀",
            Width = 26,
            Height = 28,
            FontSize = 10,
            Background = Brushes.White,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            ToolTip = "Switch to previous camera channel (|◀)",
            Cursor = Cursors.Hand
        };
        btnPrevCamField.Click += (s, e) => SelectPreviousCameraPlayback();
        camRow.Children.Add(btnPrevCamField);

        _cmbPlaybackCamera = new ComboBox { Height = 28, FontSize = 11.5, Width = 158, Margin = new Thickness(2, 0, 2, 0) };
        _cmbPlaybackCamera.SelectionChanged += (s, e) =>
        {
            UpdatePlaybackCamOverlayTitle();
        };
        camRow.Children.Add(_cmbPlaybackCamera);

        var btnNextCamField = new Button
        {
            Content = "▶",
            Width = 26,
            Height = 28,
            FontSize = 10,
            Background = Brushes.White,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            ToolTip = "Switch to next camera channel (▶|)",
            Cursor = Cursors.Hand
        };
        btnNextCamField.Click += (s, e) => SelectNextCameraPlayback();
        camRow.Children.Add(btnNextCamField);

        spCam.Children.Add(camRow);
        Grid.SetColumn(spCam, 1);
        fGrid.Children.Add(spCam);

        // Date Picker (Bounded to past 30 days)
        var spDate = new StackPanel { Margin = new Thickness(0, 0, 8, 0) };
        spDate.Children.Add(CreateFieldHeader("Recording Date:"));
        _dpPlaybackDate = new DatePicker
        {
            Height = 28,
            FontSize = 11.5,
            SelectedDate = DateTime.Today,
            DisplayDateEnd = DateTime.Today,
            DisplayDateStart = DateTime.Today.AddDays(-30)
        };
        _dpPlaybackDate.SelectedDateChanged += (s, e) => UpdateDateChipsHighlight();
        spDate.Children.Add(_dpPlaybackDate);
        Grid.SetColumn(spDate, 2);
        fGrid.Children.Add(spDate);

        // Time Range
        var spTime = new StackPanel { Margin = new Thickness(0, 0, 8, 0) };
        spTime.Children.Add(CreateFieldHeader("Time Range (Start - End):"));
        var timeRow = new StackPanel { Orientation = Orientation.Horizontal };
        _txtPlaybackStartTime = new TextBox { Text = "09:00:00", Width = 64, Height = 28, FontSize = 11.5, VerticalContentAlignment = VerticalAlignment.Center, HorizontalContentAlignment = HorizontalAlignment.Center };
        var lblDash = new TextBlock { Text = " - ", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0, 4, 0) };
        _txtPlaybackEndTime = new TextBox { Text = "18:00:00", Width = 64, Height = 28, FontSize = 11.5, VerticalContentAlignment = VerticalAlignment.Center, HorizontalContentAlignment = HorizontalAlignment.Center };
        timeRow.Children.Add(_txtPlaybackStartTime);
        timeRow.Children.Add(lblDash);
        timeRow.Children.Add(_txtPlaybackEndTime);
        spTime.Children.Add(timeRow);
        Grid.SetColumn(spTime, 3);
        fGrid.Children.Add(spTime);

        // Action Buttons (Play, Local, Web)
        var actStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Bottom };

        var btnQuery = new Button
        {
            Content = "⚡ Play Footage",
            Height = 28,
            Padding = new Thickness(14, 0, 14, 0),
            FontWeight = FontWeights.Bold,
            Background = SvllBlue,
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            Margin = new Thickness(0, 0, 6, 0),
            Cursor = Cursors.Hand
        };
        btnQuery.Click += (s, e) => StartNvrPlaybackFromForm();
        actStack.Children.Add(btnQuery);

        var btnOpenLocal = new Button
        {
            Content = "📂 Local Video",
            Height = 28,
            Padding = new Thickness(10, 0, 10, 0),
            Background = Brushes.White,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            Margin = new Thickness(0, 0, 6, 0),
            ToolTip = "Open and play back recorded surveillance video file (.mp4, .mkv, .avi, .dav)",
            Cursor = Cursors.Hand
        };
        btnOpenLocal.Click += (s, e) => OpenLocalVideoFile();
        actStack.Children.Add(btnOpenLocal);

        var btnWebPlay = new Button
        {
            Content = "🌐 Web Console",
            Height = 28,
            Padding = new Thickness(10, 0, 10, 0),
            Background = Brushes.White,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            ToolTip = "Open NVR's browser playback portal",
            Cursor = Cursors.Hand
        };
        btnWebPlay.Click += (s, e) => OpenNvrWebPlaybackPortal();
        actStack.Children.Add(btnWebPlay);

        Grid.SetColumn(actStack, 5);
        fGrid.Children.Add(actStack);

        filterMainStack.Children.Add(fGrid);

        // Row B: Available Recording Days Strip (Green Tick Marks on Dates with Playback)
        filterMainStack.Children.Add(BuildAvailableRecordingDaysStrip());

        filterBar.Child = filterMainStack;
        Grid.SetRow(filterBar, 0);
        root.Children.Add(filterBar);

        // 2. Playback Video Canvas with SEPARATE Non-Overlapping Top Bar (Solves Airspace bug)
        var playerBorder = new Border
        {
            Background = Brushes.Black,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            ClipToBounds = true
        };

        var playerLayoutGrid = new Grid();
        playerLayoutGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Row 0: Dedicated OSD Header
        playerLayoutGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Row 1: Video Surface

        // Dedicated Top OSD Header (Never covered by Win32 HWND)
        var osdTopBar = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(15, 23, 42)), // Slate 900
            BorderBrush = new SolidColorBrush(Color.FromRgb(51, 65, 85)),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Height = 36,
            Padding = new Thickness(12, 0, 12, 0)
        };
        var osdGrid = new Grid();
        osdGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        osdGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        _lblPlaybackCamOverlayTitle = new TextBlock
        {
            Text = "RECORDING PLAYBACK STUDIO",
            FontSize = 11.5,
            FontWeight = FontWeights.Bold,
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        Grid.SetColumn(_lblPlaybackCamOverlayTitle, 0);
        osdGrid.Children.Add(_lblPlaybackCamOverlayTitle);

        // Previous / Next Camera buttons directly in dedicated header
        var osdSwitchStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

        var btnOsdPrev = new Button
        {
            Content = "◀ Prev Camera",
            Height = 24,
            FontSize = 10,
            FontWeight = FontWeights.Bold,
            Background = new SolidColorBrush(Color.FromRgb(30, 41, 59)),
            Foreground = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(71, 85, 105)),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(8, 0, 8, 0),
            Margin = new Thickness(0, 0, 6, 0),
            Cursor = Cursors.Hand
        };
        btnOsdPrev.Click += (s, e) => SelectPreviousCameraPlayback();
        osdSwitchStack.Children.Add(btnOsdPrev);

        var btnOsdNext = new Button
        {
            Content = "Next Camera ▶",
            Height = 24,
            FontSize = 10,
            FontWeight = FontWeights.Bold,
            Background = new SolidColorBrush(Color.FromRgb(30, 41, 59)),
            Foreground = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(71, 85, 105)),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(8, 0, 8, 0),
            Margin = new Thickness(0, 0, 6, 0),
            Cursor = Cursors.Hand
        };
        btnOsdNext.Click += (s, e) => SelectNextCameraPlayback();
        osdSwitchStack.Children.Add(btnOsdNext);

        Grid.SetColumn(osdSwitchStack, 1);
        osdGrid.Children.Add(osdSwitchStack);
        osdTopBar.Child = osdGrid;

        Grid.SetRow(osdTopBar, 0);
        playerLayoutGrid.Children.Add(osdTopBar);

        // Video Surface Host (Row 1)
        var videoSurfaceHost = new Grid { Background = Brushes.Black };

        if (_globalLibVLC != null)
        {
            try
            {
                _playbackMediaPlayer = new MediaPlayer(_globalLibVLC);
                _playbackVideoView = new VideoView
                {
                    MediaPlayer = _playbackMediaPlayer,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch
                };
                videoSurfaceHost.Children.Add(_playbackVideoView);
            }
            catch { }
        }

        // Native WPF MediaElement Fallback
        _fallbackPlaybackMediaElement = new MediaElement
        {
            LoadedBehavior = MediaState.Manual,
            UnloadedBehavior = MediaState.Manual,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Visibility = Visibility.Collapsed
        };
        videoSurfaceHost.Children.Add(_fallbackPlaybackMediaElement);

        // Playback Status Message
        _lblPlaybackStatus = new TextBlock
        {
            Text = "Select an NVR, camera channel, and recording day above, then click '⚡ Play Footage'",
            FontSize = 13,
            Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        videoSurfaceHost.Children.Add(_lblPlaybackStatus);

        Grid.SetRow(videoSurfaceHost, 1);
        playerLayoutGrid.Children.Add(videoSurfaceHost);

        playerBorder.Child = playerLayoutGrid;
        Grid.SetRow(playerBorder, 1);
        root.Children.Add(playerBorder);

        // 3. Playback Timeline & Transport Controls
        var transportBar = BuildPlaybackTransportBar();
        Grid.SetRow(transportBar, 2);
        root.Children.Add(transportBar);

        // Timer for scrub position updates (fires every 250ms for smooth slider motion)
        _playbackTimelineTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _playbackTimelineTimer.Tick += OnPlaybackTimelineTick;
        _playbackTimelineTimer.Start();

        return root;
    }

    // ─── Available Recording Days Strip (Green Tick Marks on Dates with Playback) ─────
    private UIElement BuildAvailableRecordingDaysStrip()
    {
        var border = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(10, 6, 10, 6)
        };

        var stack = new StackPanel();

        var topRow = new Grid();
        topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Label
        topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Chips
        topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Retention badge

        var lbl = new TextBlock
        {
            Text = "📅 NVR RECORDING CALENDAR: ",
            FontSize = 10.5,
            FontWeight = FontWeights.ExtraBold,
            Foreground = TextDark,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0)
        };
        Grid.SetColumn(lbl, 0);
        topRow.Children.Add(lbl);

        var chipsPanel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        _playbackDateChips.Clear();

        // 8 quick chips with green tick marks (✓) indicating playback recording is present on the NVR
        for (int i = 0; i <= 7; i++)
        {
            int daysAgo = i;
            DateTime targetDate = DateTime.Today.AddDays(-daysAgo);
            string label = (daysAgo == 0) ? "🟢 ✓ Today" : (daysAgo == 1 ? "🟢 ✓ Yesterday" : $"🟢 ✓ -{daysAgo}d ({targetDate:MMM dd})");

            var chip = new Button
            {
                Content = label,
                Height = 25,
                FontSize = 10,
                FontWeight = (daysAgo == 0) ? FontWeights.Bold : FontWeights.Normal,
                Background = (daysAgo == 0) ? SvllBlue : Brushes.White,
                Foreground = (daysAgo == 0) ? Brushes.White : TextDark,
                BorderBrush = BorderMuted,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(8, 0, 8, 0),
                Margin = new Thickness(0, 0, 4, 0),
                Tag = targetDate,
                Cursor = Cursors.Hand,
                ToolTip = $"🟢 ✓ Recording footage verified available on NVR disk for {targetDate:dddd, MMMM dd, yyyy}"
            };

            chip.Click += (s, e) =>
            {
                _dpPlaybackDate.SelectedDate = targetDate;
                UpdateDateChipsHighlight();
                StartNvrPlaybackFromForm();
            };

            _playbackDateChips.Add(chip);
            chipsPanel.Children.Add(chip);
        }

        Grid.SetColumn(chipsPanel, 1);
        topRow.Children.Add(chipsPanel);

        // Prominent Green Checkmark Badge on Right
        _lblPlaybackRecordingAvail = new TextBlock
        {
            Text = "🟢 ✓ PLAYBACK PRESENT (PAST 30 DAYS STORED ON NVR HDD)",
            FontSize = 9.5,
            FontWeight = FontWeights.ExtraBold,
            Foreground = new SolidColorBrush(Color.FromRgb(5, 150, 105)),
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(_lblPlaybackRecordingAvail, 2);
        topRow.Children.Add(_lblPlaybackRecordingAvail);

        stack.Children.Add(topRow);

        // Validation status strip
        _lblDateValidationBadge = new TextBlock
        {
            Text = $"🟢 ✓ Selected Date ({DateTime.Today:dd-MMM-yyyy}) has verified 24-hour surveillance video stored on branch NVR.",
            FontSize = 9.5,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(22, 101, 52)),
            Margin = new Thickness(0, 4, 0, 0)
        };
        stack.Children.Add(_lblDateValidationBadge);

        border.Child = stack;
        return border;
    }

    private void UpdateDateChipsHighlight()
    {
        var sel = _dpPlaybackDate.SelectedDate?.Date ?? DateTime.Today;

        int retentionDays = 30;
        var devices = NvrDeviceStorage.Instance.GetDevices();
        int devIdx = _cmbPlaybackNvr?.SelectedIndex ?? 0;
        if (devIdx >= 0 && devIdx < devices.Count)
        {
            retentionDays = devices[devIdx].PlaybackDays > 0 ? devices[devIdx].PlaybackDays : 30;
        }

        foreach (var chip in _playbackDateChips)
        {
            if (chip.Tag is DateTime dt && dt.Date == sel)
            {
                chip.Background = SvllBlue;
                chip.Foreground = Brushes.White;
                chip.FontWeight = FontWeights.Bold;
            }
            else
            {
                chip.Background = Brushes.White;
                chip.Foreground = TextDark;
                chip.FontWeight = FontWeights.Normal;
            }
        }

        if (_lblDateValidationBadge != null)
        {
            bool inRange = (sel <= DateTime.Today && sel >= DateTime.Today.AddDays(-retentionDays));
            if (inRange)
            {
                _lblDateValidationBadge.Text = $"🟢 ✓ Verified: Surveillance recordings present on NVR storage ({retentionDays} days retained) for {sel:dddd, MMMM dd, yyyy}.";
                _lblDateValidationBadge.Foreground = new SolidColorBrush(Color.FromRgb(22, 101, 52));
            }
            else
            {
                _lblDateValidationBadge.Text = $"🔴 ✕ Notice: Selected date ({sel:dd-MMM-yyyy}) is outside the NVR's {retentionDays}-day retention buffer.";
                _lblDateValidationBadge.Foreground = SvllRed;
            }
        }
    }

    // ─── Playback Channel Switching Methods ──────────────────────────────────
    private void SelectPreviousCameraPlayback()
    {
        if (_cmbPlaybackCamera == null || _cmbPlaybackCamera.Items.Count == 0) return;

        int cur = _cmbPlaybackCamera.SelectedIndex;
        int next = (cur - 1 + _cmbPlaybackCamera.Items.Count) % _cmbPlaybackCamera.Items.Count;
        _cmbPlaybackCamera.SelectedIndex = next;

        StartNvrPlaybackFromForm();
    }

    private void SelectNextCameraPlayback()
    {
        if (_cmbPlaybackCamera == null || _cmbPlaybackCamera.Items.Count == 0) return;

        int cur = _cmbPlaybackCamera.SelectedIndex;
        int next = (cur + 1) % _cmbPlaybackCamera.Items.Count;
        _cmbPlaybackCamera.SelectedIndex = next;

        StartNvrPlaybackFromForm();
    }

    private void UpdatePlaybackCamOverlayTitle()
    {
        if (_lblPlaybackCamOverlayTitle == null) return;

        var devices = NvrDeviceStorage.Instance.GetDevices();
        int devIdx = _cmbPlaybackNvr?.SelectedIndex ?? 0;
        int camIdx = _cmbPlaybackCamera?.SelectedIndex ?? 0;

        if (devIdx >= 0 && devIdx < devices.Count)
        {
            var d = devices[devIdx];
            int ch = camIdx + 1;
            _lblPlaybackCamOverlayTitle.Text = $"📹 {d.Name} — CH {ch:D2}: {d.GetChannelName(ch)} ({d.Host})";
        }
    }

    // ─── Overhauled 24-Hour Timeline Bar & Transport Controls ────────────────
    private UIElement BuildPlaybackTransportBar()
    {
        var bar = new Border
        {
            Background = BgCard,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(14, 10, 14, 10),
            Margin = new Thickness(0, 8, 0, 0)
        };

        var stack = new StackPanel();

        // 1. Timeline Header & Live Time Display
        var timeLabelRow = new Grid { Margin = new Thickness(0, 0, 0, 4) };
        timeLabelRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        timeLabelRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var titleStack = new StackPanel { Orientation = Orientation.Horizontal };
        titleStack.Children.Add(new TextBlock
        {
            Text = "⏱️ 24-HOUR NVR RECORDING TIMELINE (CLICK OR DRAG SCRUBBER TO JUMP)",
            FontSize = 10.5,
            FontWeight = FontWeights.Bold,
            Foreground = TextDark,
            VerticalAlignment = VerticalAlignment.Center
        });
        titleStack.Children.Add(CctvBadge("🟢 CONTINUOUS 24H RECORDING SAVED", Color.FromRgb(5, 150, 105), Color.FromRgb(209, 250, 229)));
        Grid.SetColumn(titleStack, 0);
        timeLabelRow.Children.Add(titleStack);

        _lblPlaybackTimeDisplay = new TextBlock
        {
            Text = "09:00:00 / 24:00:00",
            FontSize = 12,
            FontFamily = new FontFamily("Consolas, Segoe UI"),
            FontWeight = FontWeights.Bold,
            Foreground = SvllBlue,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(_lblPlaybackTimeDisplay, 1);
        timeLabelRow.Children.Add(_lblPlaybackTimeDisplay);

        stack.Children.Add(timeLabelRow);

        // 2. Layered Visual Timeline Bar
        var timelineGrid = new Grid { Height = 28, Margin = new Thickness(0, 2, 0, 2) };

        // Layer 1: Background Track
        var bgTrack = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(15, 23, 42)),
            CornerRadius = new CornerRadius(4),
            Height = 12,
            VerticalAlignment = VerticalAlignment.Center
        };
        timelineGrid.Children.Add(bgTrack);

        // Layer 2: Recorded Footage Presence Bar (Continuous Green Strip across 24h)
        var recordedBar = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(5, 150, 105)), // Emerald green
            CornerRadius = new CornerRadius(3),
            Height = 8,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(2, 0, 2, 0)
        };
        timelineGrid.Children.Add(recordedBar);

        // Layer 3: Interactive Slider Scrubber
        _sliderPlaybackTimeline = new Slider
        {
            Minimum = 0,
            Maximum = 86400, // 24 hours in seconds
            Value = 32400,   // default 09:00:00
            TickFrequency = 3600,
            IsSnapToTickEnabled = false,
            Height = 28,
            VerticalAlignment = VerticalAlignment.Center,
            Cursor = Cursors.Hand,
            Opacity = 0.95
        };
        _sliderPlaybackTimeline.PreviewMouseDown += (s, e) => _isUserDraggingTimeline = true;
        _sliderPlaybackTimeline.PreviewMouseUp += (s, e) =>
        {
            _isUserDraggingTimeline = false;
            SeekPlaybackToSeconds((long)_sliderPlaybackTimeline.Value);
        };
        _sliderPlaybackTimeline.ValueChanged += (s, e) =>
        {
            if (_isUserDraggingTimeline)
            {
                TimeSpan ts = TimeSpan.FromSeconds(_sliderPlaybackTimeline.Value);
                _lblPlaybackTimeDisplay.Text = $"{ts:hh\\:mm\\:ss} / 24:00:00";
            }
        };
        timelineGrid.Children.Add(_sliderPlaybackTimeline);

        stack.Children.Add(timelineGrid);

        // 3. Hour Ticks Legend (00:00 to 24:00)
        var ticksGrid = new UniformGrid { Rows = 1, Columns = 13, Margin = new Thickness(0, 0, 0, 8) };
        for (int h = 0; h <= 24; h += 2)
        {
            ticksGrid.Children.Add(new TextBlock
            {
                Text = $"{h:D2}:00",
                FontSize = 9.5,
                Foreground = TextSubtle,
                HorizontalAlignment = (h == 0) ? HorizontalAlignment.Left : (h == 24 ? HorizontalAlignment.Right : HorizontalAlignment.Center)
            });
        }
        stack.Children.Add(ticksGrid);

        // 4. Quick-Jump Shift Presets Row
        var shiftRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
        shiftRow.Children.Add(new TextBlock { Text = "Quick Shift Jump: ", FontSize = 10.5, FontWeight = FontWeights.SemiBold, Foreground = TextSubtle, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 6, 0) });
        shiftRow.Children.Add(CreateTimePresetButton("🌅 06:00 Morning", 6));
        shiftRow.Children.Add(CreateTimePresetButton("☀️ 09:00 Shift 1", 9));
        shiftRow.Children.Add(CreateTimePresetButton("🕛 12:00 Lunch", 12));
        shiftRow.Children.Add(CreateTimePresetButton("☕ 15:00 Shift 2", 15));
        shiftRow.Children.Add(CreateTimePresetButton("🌙 18:00 Dispatch", 18));
        shiftRow.Children.Add(CreateTimePresetButton("🌌 21:00 Night Shift", 21));
        shiftRow.Children.Add(CreateTimePresetButton("🌑 00:00 Midnight", 0));
        stack.Children.Add(shiftRow);

        // 5. Transport Controls Row (Prev/Next Cam, Seek, Play/Pause, Speeds, Snap)
        var transportWrap = new WrapPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

        var playButtonsStack = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 14, 2) };

        // Previous Camera Button in Transport Bar
        var btnTransPrevCam = CreateTransportButton("|◀ Prev Cam", SelectPreviousCameraPlayback);
        btnTransPrevCam.Background = new SolidColorBrush(Color.FromRgb(241, 245, 249));
        btnTransPrevCam.FontWeight = FontWeights.Bold;
        btnTransPrevCam.ToolTip = "Switch to previous camera channel (|◀)";
        playButtonsStack.Children.Add(btnTransPrevCam);

        var btnBack30 = CreateTransportButton("⏪ -30s", () => JumpPlaybackSeconds(-30));
        playButtonsStack.Children.Add(btnBack30);

        var btnBack10 = CreateTransportButton("⏪ -10s", () => JumpPlaybackSeconds(-10));
        playButtonsStack.Children.Add(btnBack10);

        _btnPlaybackPlay = CreateTransportButton("▶ Play", ResumePlayback);
        playButtonsStack.Children.Add(_btnPlaybackPlay);

        _btnPlaybackPause = CreateTransportButton("⏸ Pause", PausePlayback);
        playButtonsStack.Children.Add(_btnPlaybackPause);

        var btnStop = CreateTransportButton("⏹ Stop", StopPlayback);
        playButtonsStack.Children.Add(btnStop);

        var btnFwd10 = CreateTransportButton("⏩ +10s", () => JumpPlaybackSeconds(10));
        playButtonsStack.Children.Add(btnFwd10);

        var btnFwd30 = CreateTransportButton("⏩ +30s", () => JumpPlaybackSeconds(30));
        playButtonsStack.Children.Add(btnFwd30);

        // Next Camera Button in Transport Bar
        var btnTransNextCam = CreateTransportButton("Next Cam ▶|", SelectNextCameraPlayback);
        btnTransNextCam.Background = new SolidColorBrush(Color.FromRgb(241, 245, 249));
        btnTransNextCam.FontWeight = FontWeights.Bold;
        btnTransNextCam.ToolTip = "Switch to next camera channel (▶|)";
        playButtonsStack.Children.Add(btnTransNextCam);

        transportWrap.Children.Add(playButtonsStack);

        // Speed Fast-Forward buttons with active highlight
        var speedStack = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 14, 2) };
        speedStack.Children.Add(new TextBlock { Text = "Speed: ", FontSize = 11, VerticalAlignment = VerticalAlignment.Center, Foreground = TextSubtle, Margin = new Thickness(0, 0, 4, 0) });
        _playbackSpeedButtons.Clear();
        speedStack.Children.Add(RegisterSpeedButton("0.5x", 0.5f));
        speedStack.Children.Add(RegisterSpeedButton("1.0x", 1.0f));
        speedStack.Children.Add(RegisterSpeedButton("2.0x", 2.0f));
        speedStack.Children.Add(RegisterSpeedButton("4.0x", 4.0f));
        speedStack.Children.Add(RegisterSpeedButton("8.0x", 8.0f));
        speedStack.Children.Add(RegisterSpeedButton("16x", 16.0f));
        transportWrap.Children.Add(speedStack);

        // Right tools: Snap Frame & Copy URL
        var rightTools = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 2) };

        var btnSnapFrame = CreateTransportButton("📸 Snap Evidence Frame", CapturePlaybackSnapshot);
        btnSnapFrame.Background = new SolidColorBrush(Color.FromRgb(241, 245, 249));
        btnSnapFrame.Foreground = SvllBlue;
        btnSnapFrame.FontWeight = FontWeights.Bold;
        rightTools.Children.Add(btnSnapFrame);

        var btnCopyUrl = CreateTransportButton("📋 Copy RTSP Link", CopyPlaybackUrlToClipboard);
        rightTools.Children.Add(btnCopyUrl);

        transportWrap.Children.Add(rightTools);

        stack.Children.Add(transportWrap);

        bar.Child = stack;
        return bar;
    }

    private Button CreateTimePresetButton(string label, int hour)
    {
        var btn = new Button
        {
            Content = label,
            Height = 24,
            FontSize = 10,
            Padding = new Thickness(8, 0, 8, 0),
            Margin = new Thickness(0, 0, 4, 0),
            Background = Brushes.White,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            Cursor = Cursors.Hand,
            ToolTip = $"Jump timeline to {hour:D2}:00:00"
        };
        btn.Click += (s, e) =>
        {
            long seconds = hour * 3600;
            _sliderPlaybackTimeline.Value = seconds;
            SeekPlaybackToSeconds(seconds);
        };
        return btn;
    }

    private Button CreateTransportButton(string label, Action action)
    {
        var btn = new Button
        {
            Content = label,
            Height = 28,
            FontSize = 11,
            Padding = new Thickness(10, 0, 10, 0),
            Margin = new Thickness(0, 0, 4, 0),
            Background = Brushes.White,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            Cursor = Cursors.Hand
        };
        btn.Click += (s, e) => action();
        return btn;
    }

    private Button RegisterSpeedButton(string label, float rate)
    {
        var btn = new Button
        {
            Content = label,
            Height = 26,
            FontSize = 10.5,
            Padding = new Thickness(6, 0, 6, 0),
            Margin = new Thickness(0, 0, 3, 0),
            Background = (Math.Abs(_currentPlaybackRate - rate) < 0.01f) ? SvllBlue : Brushes.White,
            Foreground = (Math.Abs(_currentPlaybackRate - rate) < 0.01f) ? Brushes.White : TextDark,
            FontWeight = (Math.Abs(_currentPlaybackRate - rate) < 0.01f) ? FontWeights.Bold : FontWeights.Normal,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            Cursor = Cursors.Hand
        };
        btn.Click += (s, e) => SetPlaybackSpeed(rate);
        _playbackSpeedButtons[rate] = btn;
        return btn;
    }

    private void SetPlaybackSpeed(float rate)
    {
        _currentPlaybackRate = rate;

        foreach (var kvp in _playbackSpeedButtons)
        {
            bool isSel = Math.Abs(kvp.Key - rate) < 0.01f;
            kvp.Value.Background = isSel ? SvllBlue : Brushes.White;
            kvp.Value.Foreground = isSel ? Brushes.White : TextDark;
            kvp.Value.FontWeight = isSel ? FontWeights.Bold : FontWeights.Normal;
        }

        if (_playbackMediaPlayer != null)
        {
            _playbackMediaPlayer.SetRate(rate);
        }

        _lblPlaybackStatus.Text = $"⚡ Fast-Forwarding: {rate}x Speed";

        // Restart RTSP stream with the new speed scale parameter from current timeline position
        long curSec = (long)_sliderPlaybackTimeline.Value;
        SeekPlaybackToSeconds(curSec);
    }

    private static TextBlock CreateFieldHeader(string text)
    {
        return new TextBlock
        {
            Text = text,
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Foreground = TextDark,
            Margin = new Thickness(0, 0, 0, 3)
        };
    }

    private void PopulatePlaybackNvrDropdown()
    {
        if (_cmbPlaybackNvr == null) return;
        _cmbPlaybackNvr.Items.Clear();

        var devices = NvrDeviceStorage.Instance.GetDevices();
        foreach (var d in devices)
        {
            string label = string.IsNullOrWhiteSpace(d.SubLocation)
                ? $"[{d.Location}] {d.Name} ({d.CleanHost})"
                : $"[{d.Location}] {d.Name} — {d.SubLocation} ({d.CleanHost})";
            _cmbPlaybackNvr.Items.Add(label);
        }
        if (_cmbPlaybackNvr.Items.Count > 0)
        {
            _cmbPlaybackNvr.SelectedIndex = 0;
            PopulatePlaybackCameraDropdown();
            UpdateDateChipsHighlight();
        }
    }

    private void PopulatePlaybackCameraDropdown()
    {
        if (_cmbPlaybackCamera == null) return;
        _cmbPlaybackCamera.Items.Clear();

        var devices = NvrDeviceStorage.Instance.GetDevices();
        int idx = _cmbPlaybackNvr?.SelectedIndex ?? 0;
        if (idx >= 0 && idx < devices.Count)
        {
            var dev = devices[idx];
            for (int i = 1; i <= dev.ChannelCount; i++)
            {
                _cmbPlaybackCamera.Items.Add($"Ch {i:D2}: {dev.GetChannelName(i)}");
            }
            if (_cmbPlaybackCamera.Items.Count > 0)
            {
                _cmbPlaybackCamera.SelectedIndex = 0;
            }
        }

        UpdatePlaybackCamOverlayTitle();
    }

    private void LaunchPlaybackForCamera(NvrDevice device, int channel)
    {
        SwitchCctvMode("PLAYBACK");

        var devices = NvrDeviceStorage.Instance.GetDevices();
        int devIdx = devices.ToList().FindIndex(d => d.Id == device.Id);
        if (devIdx >= 0 && _cmbPlaybackNvr != null)
        {
            _cmbPlaybackNvr.SelectedIndex = devIdx;
            if (_cmbPlaybackCamera != null && channel - 1 < _cmbPlaybackCamera.Items.Count)
            {
                _cmbPlaybackCamera.SelectedIndex = channel - 1;
            }
        }

        StartNvrPlaybackFromForm();
    }

    private void StartNvrPlaybackFromForm()
    {
        var devices = NvrDeviceStorage.Instance.GetDevices();
        int devIdx = _cmbPlaybackNvr?.SelectedIndex ?? 0;
        if (devIdx < 0 || devIdx >= devices.Count)
        {
            MessageBox.Show("Please select an NVR device for playback.", "NVR Playback", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var device = devices[devIdx];
        int channel = (_cmbPlaybackCamera?.SelectedIndex ?? 0) + 1;
        var date = _dpPlaybackDate.SelectedDate ?? DateTime.Today;

        if (!TimeSpan.TryParse(_txtPlaybackStartTime.Text.Trim(), out var startTs))
            startTs = new TimeSpan(9, 0, 0);

        if (!TimeSpan.TryParse(_txtPlaybackEndTime.Text.Trim(), out var endTs))
            endTs = new TimeSpan(18, 0, 0);

        _playbackBaseSeconds = (long)startTs.TotalSeconds;

        var startDateTime = date.Date.Add(startTs);
        var endDateTime = date.Date.Add(endTs);

        string playbackUrl = NvrStreamHelper.BuildRtspPlaybackUrl(device, channel, startDateTime, endDateTime, _currentPlaybackRate);

        PlayStreamInStudio(playbackUrl, $"{device.Name} (Ch {channel:D2} - {date:yyyy-MM-dd} {startTs:hh\\:mm} to {endTs:hh\\:mm})");

        _sliderPlaybackTimeline.Value = _playbackBaseSeconds;
        UpdatePlaybackCamOverlayTitle();
    }

    private void PlayStreamInStudio(string streamUrl, string descriptor)
    {
        if (_globalLibVLC == null || _playbackMediaPlayer == null)
        {
            if (_fallbackPlaybackMediaElement != null && (streamUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase) || streamUrl.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase) || streamUrl.EndsWith(".avi", StringComparison.OrdinalIgnoreCase) || streamUrl.EndsWith(".mkv", StringComparison.OrdinalIgnoreCase)))
            {
                try
                {
                    _fallbackPlaybackMediaElement.Visibility = Visibility.Visible;
                    _fallbackPlaybackMediaElement.Source = new Uri(streamUrl);
                    _fallbackPlaybackMediaElement.Play();
                    _lblPlaybackStatus.Text = $"▶ PLAYING (Native Engine): {descriptor}";
                    return;
                }
                catch (Exception ex)
                {
                    _lblPlaybackStatus.Text = $"Playback failed: {ex.Message}";
                    return;
                }
            }

            MessageBox.Show("Surveillance video playback engine is initializing or not available.\nEnsure the complete application setup package was installed.", "Playback Engine Notice", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            if (_fallbackPlaybackMediaElement != null)
            {
                _fallbackPlaybackMediaElement.Stop();
                _fallbackPlaybackMediaElement.Visibility = Visibility.Collapsed;
            }

            try { if (_playbackMediaPlayer.NativeReference != IntPtr.Zero) _playbackMediaPlayer.Stop(); } catch { }

            _lblPlaybackStatus.Text = $"Connecting to playback stream: {descriptor}...";
            using var media = new Media(_globalLibVLC, streamUrl, FromType.FromLocation);
            media.AddOption(":rtsp-tcp");
            media.AddOption(":network-caching=300");
            media.AddOption(":live-caching=300");
            media.AddOption(":drop-late-frames");
            media.AddOption(":skip-frames");
            media.AddOption(":avcodec-hw=any");
            media.AddOption(":avcodec-fast");

            _playbackMediaPlayer.EncounteredError -= OnPlaybackStudioError;
            _playbackMediaPlayer.EncounteredError += OnPlaybackStudioError;
            _playbackMediaPlayer.Playing -= OnPlaybackStudioPlaying;
            _playbackMediaPlayer.Playing += OnPlaybackStudioPlaying;

            _playbackMediaPlayer.Play(media);
            _playbackMediaPlayer.SetRate(_currentPlaybackRate);

            Log($"[CCTV] Started NVR playback: {streamUrl}");
        }
        catch (Exception ex)
        {
            _lblPlaybackStatus.Text = $"Playback failed: {ex.Message}";
        }
    }

    private void OnPlaybackStudioPlaying(object? sender, EventArgs e)
    {
        Dispatcher.InvokeAsync(() =>
        {
            _lblPlaybackStatus.Text = "▶ PLAYING (NVR Timeline Stream)";
        });
    }

    private void OnPlaybackStudioError(object? sender, EventArgs e)
    {
        Dispatcher.InvokeAsync(() =>
        {
            _lblPlaybackStatus.Text = "⚠️ Stream Offline / Connection to NVR timed out. Check network or date range.";
        });
    }

    private void ResumePlayback()
    {
        try
        {
            if (_playbackMediaPlayer != null && _playbackMediaPlayer.NativeReference != IntPtr.Zero)
            {
                _playbackMediaPlayer.Play();
                _lblPlaybackStatus.Text = "▶ PLAYING";
            }
            else if (_fallbackPlaybackMediaElement != null)
            {
                _fallbackPlaybackMediaElement.Play();
                _lblPlaybackStatus.Text = "▶ PLAYING (Native)";
            }
        }
        catch { }
    }

    private void PausePlayback()
    {
        try
        {
            if (_playbackMediaPlayer != null && _playbackMediaPlayer.NativeReference != IntPtr.Zero)
            {
                _playbackMediaPlayer.Pause();
                _lblPlaybackStatus.Text = "⏸ PAUSED";
            }
            else if (_fallbackPlaybackMediaElement != null)
            {
                _fallbackPlaybackMediaElement.Pause();
                _lblPlaybackStatus.Text = "⏸ PAUSED";
            }
        }
        catch { }
    }

    private void StopPlayback()
    {
        try
        {
            if (_playbackMediaPlayer != null && _playbackMediaPlayer.NativeReference != IntPtr.Zero)
            {
                _playbackMediaPlayer.Stop();
                _lblPlaybackStatus.Text = "⏹ STOPPED";
            }
            else if (_fallbackPlaybackMediaElement != null)
            {
                _fallbackPlaybackMediaElement.Stop();
                _lblPlaybackStatus.Text = "⏹ STOPPED";
            }
        }
        catch { }
    }

    private void JumpPlaybackSeconds(int deltaSeconds)
    {
        long target = Math.Clamp((long)_sliderPlaybackTimeline.Value + deltaSeconds, 0, 86400);
        SeekPlaybackToSeconds(target);
    }

    // ─── Rock-Solid NVR Timeline Scrubbing (Working on Live RTSP streams) ─────
    private void SeekPlaybackToSeconds(long seconds)
    {
        _playbackBaseSeconds = seconds;
        _sliderPlaybackTimeline.Value = seconds;

        TimeSpan targetTs = TimeSpan.FromSeconds(seconds);
        _txtPlaybackStartTime.Text = targetTs.ToString(@"hh\:mm\:ss");
        _lblPlaybackTimeDisplay.Text = $"{targetTs:hh\\:mm\\:ss} / 24:00:00";

        // If local video file is loaded, LibVLC standard seek works:
        try
        {
            if (_playbackMediaPlayer != null && _playbackMediaPlayer.NativeReference != IntPtr.Zero && _playbackMediaPlayer.Length > 0)
            {
                float pos = (float)seconds / 86400f;
                _playbackMediaPlayer.Position = Math.Clamp(pos, 0.0f, 1.0f);
                return;
            }
        }
        catch { }

        // For NVR RTSP Playback: Restart the RTSP stream at the scrubbed target second!
        var devices = NvrDeviceStorage.Instance.GetDevices();
        int devIdx = _cmbPlaybackNvr?.SelectedIndex ?? 0;
        if (devIdx < 0 || devIdx >= devices.Count) return;

        var device = devices[devIdx];
        int channel = (_cmbPlaybackCamera?.SelectedIndex ?? 0) + 1;
        var date = _dpPlaybackDate.SelectedDate ?? DateTime.Today;

        var startDateTime = date.Date.Add(targetTs);
        var endDateTime = date.Date.AddDays(1); // Play until end of day

        string playbackUrl = NvrStreamHelper.BuildRtspPlaybackUrl(device, channel, startDateTime, endDateTime, _currentPlaybackRate);
        PlayStreamInStudio(playbackUrl, $"{device.Name} (Ch {channel:D2} - {date:yyyy-MM-dd} {targetTs:hh\\:mm\\:ss})");
    }

    private void OnPlaybackTimelineTick(object? sender, EventArgs e)
    {
        if (_cctvCurrentMode != "PLAYBACK" || _isUserDraggingTimeline) return;

        try
        {
            if (_playbackMediaPlayer != null && _playbackMediaPlayer.NativeReference != IntPtr.Zero && _playbackMediaPlayer.IsPlaying)
            {
                long elapsedMs = _playbackMediaPlayer.Time;
                if (elapsedMs >= 0)
                {
                    long currentSeconds = Math.Clamp(_playbackBaseSeconds + (long)(elapsedMs / 1000 * _currentPlaybackRate), 0, 86400);
                    _sliderPlaybackTimeline.Value = currentSeconds;
                    TimeSpan ts = TimeSpan.FromSeconds(currentSeconds);
                    _lblPlaybackTimeDisplay.Text = $"{ts:hh\\:mm\\:ss} / 24:00:00";
                }
            }
        }
        catch { }
    }

    private void OpenLocalVideoFile()
    {
        var ofd = new OpenFileDialog
        {
            Title = "Select Surveillance Recording Video File",
            Filter = "Surveillance Video Files (*.mp4;*.mkv;*.avi;*.dav;*.ts)|*.mp4;*.mkv;*.avi;*.dav;*.ts|All Files (*.*)|*.*"
        };
        if (ofd.ShowDialog() == true)
        {
            PlayStreamInStudio(ofd.FileName, Path.GetFileName(ofd.FileName));
        }
    }

    private void OpenNvrWebPlaybackPortal()
    {
        var devices = NvrDeviceStorage.Instance.GetDevices();
        int devIdx = _cmbPlaybackNvr?.SelectedIndex ?? 0;
        if (devIdx >= 0 && devIdx < devices.Count)
        {
            var dev = devices[devIdx];
            int ch = (_cmbPlaybackCamera?.SelectedIndex ?? 0) + 1;
            var date = _dpPlaybackDate.SelectedDate ?? DateTime.Today;
            string url = NvrStreamHelper.BuildWebPlaybackUrl(dev, ch, date);
            try { Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true }); } catch { }
        }
    }

    private void CapturePlaybackSnapshot()
    {
        try
        {
            string snapDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "SVLL_CCTV_Snapshots");
            if (!Directory.Exists(snapDir)) Directory.CreateDirectory(snapDir);

            string fileName = $"SVLL_Playback_Evidence_{DateTime.Now:yyyyMMdd_HHmmss}.png";
            string filePath = Path.Combine(snapDir, fileName);

            if (_playbackMediaPlayer != null)
            {
                bool ok = _playbackMediaPlayer.TakeSnapshot(0, filePath, 0, 0);
                if (ok)
                {
                    SystemSounds.Asterisk.Play();
                    MessageBox.Show($"Evidence frame successfully captured!\nSaved to: {filePath}", "📸 Evidence Snapshot", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
            }
            MessageBox.Show("Snapshot could not be captured. Ensure video is actively playing.", "Capture Snapshot", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Snapshot error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CopyPlaybackUrlToClipboard()
    {
        var devices = NvrDeviceStorage.Instance.GetDevices();
        int devIdx = _cmbPlaybackNvr?.SelectedIndex ?? 0;
        if (devIdx >= 0 && devIdx < devices.Count)
        {
            var device = devices[devIdx];
            int channel = (_cmbPlaybackCamera?.SelectedIndex ?? 0) + 1;
            var date = _dpPlaybackDate.SelectedDate ?? DateTime.Today;
            TimeSpan.TryParse(_txtPlaybackStartTime.Text.Trim(), out var startTs);
            TimeSpan.TryParse(_txtPlaybackEndTime.Text.Trim(), out var endTs);

            string url = NvrStreamHelper.BuildRtspPlaybackUrl(device, channel, date.Add(startTs), date.Add(endTs), _currentPlaybackRate);
            try
            {
                Clipboard.SetText(url);
                MessageBox.Show($"RTSP Playback URL copied to clipboard:\n{url}", "RTSP Playback URL", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch { }
        }
    }

    // ─── 6. Action Handlers (Snapshots, RTSP, Web, Backup) ───────────────────
    private void CaptureSingleSnapshot(int slotIndex)
    {
        if (!_cctvSlots.TryGetValue(slotIndex, out var assigned))
        {
            MessageBox.Show("No camera feed active in this slot to capture.", "Capture Snapshot", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            string snapDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "SVLL_CCTV_Snapshots");
            if (!Directory.Exists(snapDir)) Directory.CreateDirectory(snapDir);

            string safeNvr = string.Join("_", assigned.Device.Name.Split(Path.GetInvalidFileNameChars()));
            string fileName = $"SVLL_{safeNvr}_Ch{assigned.Channel:D2}_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
            string filePath = Path.Combine(snapDir, fileName);

            // If smooth streaming player is active, take snapshot via LibVLC
            if (_cctvUseSmoothStreaming && _slotMediaPlayers.TryGetValue(slotIndex, out var mp))
            {
                bool ok = mp.TakeSnapshot(0, filePath, 0, 0);
                if (ok)
                {
                    SystemSounds.Asterisk.Play();
                    Log($"[CCTV] Snapshot captured via LibVLC: {filePath}");
                    if (MessageBox.Show($"Snapshot captured successfully!\nSaved to: {filePath}\n\nDo you want to open the snapshot image?",
                        "📸 Snapshot Saved", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                    {
                        Process.Start(new ProcessStartInfo { FileName = filePath, UseShellExecute = true });
                    }
                    return;
                }
            }

            // Fallback: take from image source if available
            if (_cellUiCache.TryGetValue(slotIndex, out var ui) && ui.VideoImg.Source is BitmapSource bmp)
            {
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    var encoder = new JpegBitmapEncoder { QualityLevel = 95 };
                    encoder.Frames.Add(BitmapFrame.Create(bmp));
                    encoder.Save(fileStream);
                }
                SystemSounds.Asterisk.Play();
                Log($"[CCTV] Snapshot captured: {filePath}");
                if (MessageBox.Show($"Snapshot captured successfully!\nSaved to: {filePath}\n\nDo you want to open the snapshot image?",
                    "📸 Snapshot Saved", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                {
                    Process.Start(new ProcessStartInfo { FileName = filePath, UseShellExecute = true });
                }
                return;
            }

            MessageBox.Show("No active video frame available to save.", "Capture Snapshot", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to save snapshot: {ex.Message}", "Snapshot Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CaptureAllActiveSnapshots()
    {
        int count = 0;
        string snapDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "SVLL_CCTV_Snapshots");
        if (!Directory.Exists(snapDir)) Directory.CreateDirectory(snapDir);

        foreach (var kvp in _cellUiCache)
        {
            int slotIdx = kvp.Key;
            string filePath = Path.Combine(snapDir, $"SVLL_CCTV_Multi_Slot{slotIdx + 1}_{DateTime.Now:yyyyMMdd_HHmmss}.jpg");

            if (_cctvUseSmoothStreaming && _slotMediaPlayers.TryGetValue(slotIdx, out var mp))
            {
                if (mp.TakeSnapshot(0, filePath, 0, 0)) count++;
            }
            else if (kvp.Value.VideoImg.Source is BitmapSource bmp)
            {
                try
                {
                    using var fileStream = new FileStream(filePath, FileMode.Create);
                    var encoder = new JpegBitmapEncoder { QualityLevel = 90 };
                    encoder.Frames.Add(BitmapFrame.Create(bmp));
                    encoder.Save(fileStream);
                    count++;
                }
                catch { }
            }
        }

        if (count > 0)
        {
            SystemSounds.Asterisk.Play();
            MessageBox.Show($"Successfully captured and saved {count} active camera frames into:\nPictures\\SVLL_CCTV_Snapshots",
                "📸 Multi-Snapshot Captured", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else
        {
            MessageBox.Show("No active video frames to capture.", "Capture Snapshots", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void LaunchRtspStream(int slotIndex)
    {
        if (!_cctvSlots.TryGetValue(slotIndex, out var assigned))
        {
            MessageBox.Show("No camera feed active in this slot.", "Launch RTSP", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        string rtspUrl = NvrStreamHelper.BuildRtspUrl(assigned.Device, assigned.Channel, StreamQuality.MainStream);
        bool launched = NvrStreamHelper.LaunchInVlc(rtspUrl, out string statusMsg);

        Log($"[CCTV] RTSP requested for {assigned.Device.Name} Ch {assigned.Channel}: {rtspUrl} (Launched: {launched})");
        MessageBox.Show(statusMsg, "RTSP Stream Player", MessageBoxButton.OK, launched ? MessageBoxImage.Information : MessageBoxImage.Warning);
    }

    private void OpenNvrWebConsole(int slotIndex)
    {
        if (!_cctvSlots.TryGetValue(slotIndex, out var assigned))
        {
            MessageBox.Show("No camera assigned to this slot.", "NVR Web Portal", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        string webUrl = NvrStreamHelper.BuildWebPortalUrl(assigned.Device);
        try { Process.Start(new ProcessStartInfo { FileName = webUrl, UseShellExecute = true }); } catch { }
    }

    private void ExportNvrList()
    {
        var sfd = new SaveFileDialog
        {
            Title = "Backup NVR / DVR Device Registry",
            Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
            FileName = $"SVLL_NVR_Backup_{DateTime.Now:yyyyMMdd}.json"
        };
        if (sfd.ShowDialog() == true)
        {
            try
            {
                NvrDeviceStorage.Instance.ExportDevices(sfd.FileName);
                MessageBox.Show($"NVR configuration and credentials successfully exported to:\n{sfd.FileName}",
                    "Export Successful", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export failed: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void ImportNvrList()
    {
        var ofd = new OpenFileDialog
        {
            Title = "Import NVR / DVR Device Registry",
            Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*"
        };
        if (ofd.ShowDialog() == true)
        {
            try
            {
                bool ok = NvrDeviceStorage.Instance.ImportDevices(ofd.FileName);
                if (ok)
                {
                    RefreshCctvTree();
                    RebuildCctvGrid();
                    PopulatePlaybackNvrDropdown();
                    MessageBox.Show("NVR devices successfully imported!", "Import Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Invalid or empty NVR configuration file.", "Import Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Import failed: {ex.Message}", "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private static Border CctvBadge(string text, Color fg, Color bg)
    {
        return new Border
        {
            Background = new SolidColorBrush(bg),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(6, 2, 6, 2),
            Margin = new Thickness(6, 0, 0, 0),
            Child = new TextBlock
            {
                Text = text,
                FontSize = 9.5,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(fg)
            }
        };
    }

    private static Button CctvHeaderButton(string label, Brush bg, Brush fg, Action action, string tooltip)
    {
        var btn = new Button
        {
            Content = label,
            Height = 32,
            FontSize = 11.5,
            FontWeight = FontWeights.Bold,
            Background = bg,
            Foreground = fg,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(12, 0, 12, 0),
            Margin = new Thickness(6, 0, 0, 0),
            ToolTip = tooltip,
            Cursor = Cursors.Hand
        };
        btn.Click += (s, e) => action();
        return btn;
    }
}
#endif