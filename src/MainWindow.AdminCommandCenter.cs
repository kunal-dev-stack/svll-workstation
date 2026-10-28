// =============================================================================
// SVLL IT Workstation v8.3 — Admin Command Center (Hub 7 Rebuilt)
// Centralized Executive NOC Deck: 1-Click "Take Remote" + Live Connected Fleet
// Inbuilt SVLL Mesh Agent Listener, Direct LAN Port 21118, and Real-time Vitals.
// =============================================================================
#if BUILD_ADMIN

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using SVLL_IT_Workstation.Models;

namespace SVLL_IT_Workstation;

public partial class MainWindow
{
    // ─── State for Hub 7 ─────────────────────────────────────────────────────
    private static DispatcherTimer? _hub7RefreshTimer;
    private StackPanel _hub7DeviceListPanel = null!;
    private TextBox _hub7SearchBox = null!;
    private ComboBox _hub7BranchFilter = null!;
    private string _hub7StatusFilter = "ALL"; // ALL, ONLINE, ALERTS, MESH
    private TextBlock _hub7TotalCounter = null!;
    private TextBlock _hub7OnlineCounter = null!;
    private TextBlock _hub7MeshCounter = null!;
    private TextBlock _hub7AlertCounter = null!;
    private Border _hub7RelayBanner = null!;
    private Border _hub7TelemetryDeck = null!;

    // Take Remote Card Controls
    private TextBox _trTargetBox = null!;
    private PasswordBox _trPasswordBox = null!;
    private TextBox _trPasswordRevealBox = null!;
    private CheckBox _trShowPasswordChk = null!;
    private ComboBox _trModeCombo = null!;
    private RemoteDeviceItem? _selectedFleetDevice = null;

    // ─── Entry Point ─────────────────────────────────────────────────────────
    public UIElement BuildAdminCommandCenterView()
    {
        var rootGrid = new Grid();

        void ShowContent()
        {
            rootGrid.Children.Clear();
            rootGrid.Children.Add(BuildHub7MainContent());
            StartHub7ServicesAndTimer();
        }

        if (!AdminAuthService.Instance.IsAdminMode)
        {
            rootGrid.Children.Add(BuildAdminLockScreen("Admin Command Center", ShowContent));
        }
        else
        {
            ShowContent();
        }

        AdminAuthService.Instance.AdminModeChanged += (isAdmin) => Dispatcher.Invoke(() =>
        {
            if (!isAdmin)
            {
                StopHub7ServicesAndTimer();
                rootGrid.Children.Clear();
                rootGrid.Children.Add(BuildAdminLockScreen("Admin Command Center", ShowContent));
            }
        });

        return rootGrid;
    }

    private void StartHub7ServicesAndTimer()
    {
        // 1. Ensure Inbuilt Mesh Listener is running on Port 5055
        try
        {
            if (!SvllMeshListener.Instance.IsRunning)
            {
                SvllMeshListener.Instance.Start(SvllMeshListener.DefaultPort);
            }
        }
        catch { }

        // 2. Start UI auto-refresh timer (every 15 seconds)
        _hub7RefreshTimer?.Stop();
        _hub7RefreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(15) };
        _hub7RefreshTimer.Tick += async (s, e) =>
        {
            await CloudSyncService.Instance.PollFleetAsync();
            Hub7RenderDeviceCards();
            Hub7UpdateCounters();
            Hub7RefreshRelayBanner();
            Hub7RefreshTelemetryDeck();
        };
        _hub7RefreshTimer.Start();
    }

    private void StopHub7ServicesAndTimer()
    {
        _hub7RefreshTimer?.Stop();
        _hub7RefreshTimer = null;
    }

    // ─── Main Content Layout ─────────────────────────────────────────────────
    private UIElement BuildHub7MainContent()
    {
        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };

        var root = new StackPanel { Margin = new Thickness(10) };

        // 1. Executive NOC Header
        root.Children.Add(BuildHub7Header());

        // 2. Status & Inbuilt Mesh Listener Banner
        root.Children.Add(BuildHub7RelayAndMeshBanner());

        // 3. Four Fleet Quick Metrics Cards
        root.Children.Add(BuildHub7MetricsRow());

        // 4. CARD 1: "⚡ TAKE REMOTE CONTROL" (Primary Centerpiece)
        root.Children.Add(BuildHub7TakeRemoteCard());

        // 5. CARD 2: "🖥️ CONNECTED FLEET WORKSTATIONS" (Full Fleet Grid)
        root.Children.Add(BuildHub7ConnectedDevicesCard());

        scroll.Content = root;
        return scroll;
    }

    // ─── 1. Header ───────────────────────────────────────────────────────────
    private UIElement BuildHub7Header()
    {
        var card = new Border
        {
            Background = BgCard,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(16, 12, 16, 12),
            Margin = new Thickness(0, 0, 0, 10)
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var left = new StackPanel();
        var titleRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 4) };
        titleRow.Children.Add(new TextBlock
        {
            Text = "👑 Admin Command Center",
            FontSize = 18,
            FontWeight = FontWeights.ExtraBold,
            Foreground = TextDark,
            VerticalAlignment = VerticalAlignment.Center
        });
        titleRow.Children.Add(Hub7Badge("HUB 7", Color.FromRgb(30, 64, 175), Color.FromRgb(219, 234, 254)));
        titleRow.Children.Add(Hub7Badge("MESH ACTIVE", Color.FromRgb(5, 150, 105), Color.FromRgb(209, 250, 229)));
        titleRow.Children.Add(Hub7Badge("AEGISMESH 60FPS", Color.FromRgb(124, 58, 237), Color.FromRgb(237, 233, 254)));
        titleRow.Children.Add(Hub7Badge("CLOUDFLARE WAN", Color.FromRgb(234, 88, 12), Color.FromRgb(255, 237, 213)));
        left.Children.Add(titleRow);

        left.Children.Add(new TextBlock
        {
            Text = "Enterprise Zero-Touch Remote Desktop • Inbuilt Mesh Listener • Live Fleet Telemetry",
            FontSize = 11,
            Foreground = TextSubtle
        });
        Grid.SetColumn(left, 0);
        grid.Children.Add(left);

        var right = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

        // Mesh Listener Dialog Button
        var btnMeshInfo = Hub7HeaderButton("🛰️ Mesh Listener: :5055", new SolidColorBrush(Color.FromRgb(245, 243, 255)), new SolidColorBrush(Color.FromRgb(124, 58, 237)), () =>
        {
            ShowInbuiltMeshListenerDialog();
        }, "View Inbuilt Mesh Listener details & live connected sockets");
        right.Children.Add(btnMeshInfo);

        // Sync Fleet Button
        var btnSync = Hub7HeaderButton("🔄 Sync Fleet", Brushes.White, TextDark, async () =>
        {
            btnMeshInfo.IsEnabled = false;
            await CloudSyncService.Instance.PollFleetAsync();
            Hub7RenderDeviceCards();
            Hub7UpdateCounters();
            Hub7RefreshRelayBanner();
            Hub7RefreshTelemetryDeck();
            btnMeshInfo.IsEnabled = true;
        }, "Poll latest heartbeat data from relay and LAN agents");
        right.Children.Add(btnSync);

        // Export CSV Button
        var btnExport = Hub7HeaderButton("📄 Export CSV", Brushes.White, TextDark, () =>
        {
            try
            {
                string csv = FleetRegistryService.Instance.ExportFleetCsv();
                string path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "SVLL_Fleet_Export.csv");
                System.IO.File.WriteAllText(path, csv);
                MessageBox.Show($"Fleet registry exported successfully to:\n{path}", "Export Successful", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }, "Export fleet device records to CSV on your Desktop");
        right.Children.Add(btnExport);

        // Server Settings Button
        var btnCfg = Hub7HeaderButton("⚙️ Settings", Brushes.White, TextDark, () =>
        {
            ShowServerSettingsDialog();
        }, "Configure Cloud Relay Server URL and network settings");
        right.Children.Add(btnCfg);

        // Lock Admin Mode
        var btnLock = Hub7HeaderButton("🔒 Lock", new SolidColorBrush(Color.FromRgb(254, 242, 242)), SvllRed, () =>
        {
            AdminAuthService.Instance.LockAdmin();
        }, "Lock Admin Mode");
        right.Children.Add(btnLock);

        Grid.SetColumn(right, 1);
        grid.Children.Add(right);

        card.Child = grid;
        return card;
    }

    // ─── 2. Relay & Inbuilt Mesh Listener Banner ──────────────────────────────
    private UIElement BuildHub7RelayAndMeshBanner()
    {
        _hub7RelayBanner = new Border
        {
            CornerRadius = new CornerRadius(10),
            Margin = new Thickness(0, 0, 0, 10)
        };
        Hub7RefreshRelayBanner();
        return _hub7RelayBanner;
    }

    private void Hub7RefreshRelayBanner()
    {
        if (_hub7RelayBanner == null) return;

        bool relayOk = CloudSyncService.Instance.IsRelayReachable;
        string relayUrl = AdminAuthService.Instance.GetRelayServerUrl();
        bool meshOk = SvllMeshListener.Instance.IsRunning;
        int meshCount = SvllMeshListener.Instance.ConnectedCount;

        _hub7RelayBanner.Background = relayOk
            ? new SolidColorBrush(Color.FromRgb(240, 253, 244))
            : new SolidColorBrush(Color.FromRgb(254, 242, 242));
        _hub7RelayBanner.BorderBrush = relayOk
            ? new SolidColorBrush(Color.FromRgb(187, 247, 208))
            : new SolidColorBrush(Color.FromRgb(254, 202, 202));
        _hub7RelayBanner.BorderThickness = new Thickness(1.5);
        _hub7RelayBanner.Padding = new Thickness(14, 8, 14, 8);

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var info = new StackPanel();

        var titleRow = new StackPanel { Orientation = Orientation.Horizontal };
        titleRow.Children.Add(new TextBlock
        {
            Text = relayOk ? "🟢  Corporate Relay Connected" : "🔴  Relay Server Unreachable",
            FontSize = 12,
            FontWeight = FontWeights.Bold,
            Foreground = relayOk ? new SolidColorBrush(Color.FromRgb(22, 101, 52)) : SvllRed
        });
        titleRow.Children.Add(new TextBlock
        {
            Text = $"  —  {relayUrl}",
            FontSize = 11,
            Foreground = TextSubtle,
            VerticalAlignment = VerticalAlignment.Center
        });
        info.Children.Add(titleRow);

        var subRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 0) };
        subRow.Children.Add(new TextBlock
        {
            Text = $"🛰️ Inbuilt Mesh Listener: {(meshOk ? $"ONLINE (Port {SvllMeshListener.Instance.ActivePort}) • {meshCount} Agent(s) Active" : "OFFLINE")}",
            FontSize = 10.5,
            FontWeight = FontWeights.SemiBold,
            Foreground = meshOk ? new SolidColorBrush(Color.FromRgb(109, 40, 217)) : TextSubtle
        });
        subRow.Children.Add(new TextBlock
        {
            Text = "  •  Fleet auto-syncs every 15s  •  AegisMesh Cloudflare Gateway active",
            FontSize = 10,
            Foreground = TextSubtle
        });
        info.Children.Add(subRow);

        Grid.SetColumn(info, 0);
        grid.Children.Add(info);

        var right = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        var btnRetry = new Button
        {
            Content = "⟳ Reconnect",
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Background = Brushes.White,
            Foreground = TextDark,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(10, 4, 10, 4),
            Cursor = Cursors.Hand
        };
        btnRetry.Click += async (s, e) =>
        {
            btnRetry.Content = "⏳";
            btnRetry.IsEnabled = false;
            await Task.Run(async () =>
            {
                CloudSyncService.Instance.Stop();
                CloudSyncService.Instance.Start(AdminAuthService.Instance.GetRelayServerUrl());
                await Task.Delay(2500);
            });
            btnRetry.Content = "⟳ Reconnect";
            btnRetry.IsEnabled = true;
            Hub7RefreshRelayBanner();
            Hub7RenderDeviceCards();
            Hub7UpdateCounters();
            Hub7RefreshTelemetryDeck();
        };
        right.Children.Add(btnRetry);

        Grid.SetColumn(right, 1);
        grid.Children.Add(right);

        _hub7RelayBanner.Child = grid;
    }

    // ─── 3. Four Fleet Quick Metrics Cards ────────────────────────────────────
    private UIElement BuildHub7MetricsRow()
    {
        var grid = new Grid { Margin = new Thickness(0, 0, 0, 10) };
        for (int i = 0; i < 4; i++)
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var devs = FleetRegistryService.Instance.GetAllDevices();
        int meshCount = SvllMeshListener.Instance.ConnectedCount;

        _hub7TotalCounter = MakeMetricValue(devs.Count.ToString(), SvllBlue);
        _hub7OnlineCounter = MakeMetricValue(devs.Count(d => d.Status == "Online").ToString(), new SolidColorBrush(Color.FromRgb(22, 101, 52)));
        _hub7MeshCounter = MakeMetricValue(meshCount.ToString(), new SolidColorBrush(Color.FromRgb(109, 40, 217)));
        _hub7AlertCounter = MakeMetricValue(devs.Count(d => d.Status == "SupportRequested").ToString(), SvllRed);

        (string icon, string label, TextBlock val, string sub, Color bg, Color border)[] cards =
        {
            ("🖥️", "TOTAL FLEET", _hub7TotalCounter, "Registered workstations", Color.FromRgb(239, 246, 255), Color.FromRgb(191, 219, 254)),
            ("🟢", "ONLINE NOW", _hub7OnlineCounter, "Ready for 1-click remote", Color.FromRgb(240, 253, 244), Color.FromRgb(187, 247, 208)),
            ("🛰️", "MESH CONNECTED", _hub7MeshCounter, "Live sockets on port 5055", Color.FromRgb(245, 243, 255), Color.FromRgb(221, 214, 254)),
            ("🚨", "NEED ASSISTANCE", _hub7AlertCounter, "Active user support beacons", Color.FromRgb(254, 242, 242), Color.FromRgb(254, 202, 202))
        };

        for (int i = 0; i < cards.Length; i++)
        {
            var (icon, label, val, sub, bg, border) = cards[i];
            var card = new Border
            {
                Background = new SolidColorBrush(bg),
                BorderBrush = new SolidColorBrush(border),
                BorderThickness = new Thickness(1.5),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(14, 10, 14, 10),
                Margin = new Thickness(i == 0 ? 0 : 4, 0, i == 3 ? 0 : 4, 0)
            };
            var sp = new StackPanel();
            sp.Children.Add(new TextBlock { Text = $"{icon}  {label}", FontSize = 9.5, FontWeight = FontWeights.ExtraBold, Foreground = TextSubtle });
            sp.Children.Add(val);
            sp.Children.Add(new TextBlock { Text = sub, FontSize = 9.5, Foreground = TextSubtle, Margin = new Thickness(0, 2, 0, 0) });
            card.Child = sp;
            Grid.SetColumn(card, i);
            grid.Children.Add(card);
        }

        return grid;
    }

    private TextBlock MakeMetricValue(string text, Brush brush) =>
        new TextBlock { Text = text, FontSize = 24, FontWeight = FontWeights.ExtraBold, Foreground = brush, Margin = new Thickness(0, 2, 0, 0) };

    private void Hub7UpdateCounters()
    {
        try
        {
            var devs = FleetRegistryService.Instance.GetAllDevices();
            int meshCount = SvllMeshListener.Instance.ConnectedCount;

            if (_hub7TotalCounter != null) _hub7TotalCounter.Text = devs.Count.ToString();
            if (_hub7OnlineCounter != null) _hub7OnlineCounter.Text = devs.Count(d => d.Status == "Online").ToString();
            if (_hub7MeshCounter != null) _hub7MeshCounter.Text = meshCount.ToString();
            if (_hub7AlertCounter != null) _hub7AlertCounter.Text = devs.Count(d => d.Status == "SupportRequested").ToString();
        }
        catch { }
    }

    // ─── 4. CARD 1: "⚡ TAKE REMOTE CONTROL" (Primary Centerpiece) ───────────
    private UIElement BuildHub7TakeRemoteCard()
    {
        var card = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(15, 23, 42)), // Slate-900 NOC Theme
            BorderBrush = new SolidColorBrush(Color.FromRgb(16, 185, 129)), // Emerald-500 Glowing Accent
            BorderThickness = new Thickness(2),
            CornerRadius = new CornerRadius(14),
            Padding = new Thickness(18, 14, 18, 14),
            Margin = new Thickness(0, 0, 0, 12),
            Effect = new DropShadowEffect
            {
                BlurRadius = 16,
                ShadowDepth = 2,
                Opacity = 0.28,
                Color = Color.FromRgb(16, 185, 129)
            }
        };

        var mainStack = new StackPanel();

        // Card Header Row
        var headGrid = new Grid { Margin = new Thickness(0, 0, 0, 12) };
        headGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        headGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var headLeft = new StackPanel { Orientation = Orientation.Horizontal };
        headLeft.Children.Add(new TextBlock
        {
            Text = "⚡ TAKE REMOTE CONTROL",
            FontSize = 15,
            FontWeight = FontWeights.ExtraBold,
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center
        });
        headLeft.Children.Add(Hub7Badge("1-CLICK DIRECT", Color.FromRgb(16, 185, 129), Color.FromArgb(40, 16, 185, 129)));
        headLeft.Children.Add(Hub7Badge("60 FPS LAN", Color.FromRgb(56, 189, 248), Color.FromArgb(40, 56, 189, 248)));
        Grid.SetColumn(headLeft, 0);
        headGrid.Children.Add(headLeft);

        var clearBtn = new Button
        {
            Content = "✖ Reset Deck",
            FontSize = 10.5,
            FontWeight = FontWeights.SemiBold,
            Background = new SolidColorBrush(Color.FromRgb(30, 41, 59)),
            Foreground = new SolidColorBrush(Color.FromRgb(203, 213, 225)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(71, 85, 105)),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(8, 3, 8, 3),
            Cursor = Cursors.Hand
        };
        clearBtn.Click += (s, e) =>
        {
            _selectedFleetDevice = null;
            if (_trTargetBox != null) _trTargetBox.Text = "";
            if (_trPasswordBox != null) _trPasswordBox.Password = BuildConstants.DefaultRemotePassword;
            if (_trPasswordRevealBox != null) _trPasswordRevealBox.Text = BuildConstants.DefaultRemotePassword;
            Hub7RefreshTelemetryDeck();
            Hub7RenderDeviceCards();
        };
        Grid.SetColumn(clearBtn, 1);
        headGrid.Children.Add(clearBtn);

        mainStack.Children.Add(headGrid);

        // Two Column Working Deck:
        // Left Column (60%): Target Input, Protocol, Password, Action Button & Quick Utilities
        // Right Column (40%): Live Target Telemetry Pre-Flight Card
        var deckGrid = new Grid();
        deckGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.3, GridUnitType.Star) });
        deckGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });

        // Left Column: Interactive Connect Controls
        var leftControls = new StackPanel { Margin = new Thickness(0, 0, 14, 0) };

        // 1. Target Input Field
        leftControls.Children.Add(new TextBlock
        {
            Text = "TARGET WORKSTATION (IP, HOSTNAME OR PARTNER ID)",
            FontSize = 9.5,
            FontWeight = FontWeights.ExtraBold,
            Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
            Margin = new Thickness(0, 0, 0, 4)
        });

        _trTargetBox = new TextBox
        {
            FontSize = 14,
            FontWeight = FontWeights.Bold,
            Height = 36,
            Background = new SolidColorBrush(Color.FromRgb(30, 41, 59)),
            Foreground = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(71, 85, 105)),
            BorderThickness = new Thickness(1.5),
            Padding = new Thickness(8, 6, 8, 6),
            VerticalContentAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 10)
        };
        _trTargetBox.TextChanged += (s, e) =>
        {
            string typed = _trTargetBox.Text.Trim();
            if (!string.IsNullOrEmpty(typed))
            {
                var match = FleetRegistryService.Instance.GetAllDevices().FirstOrDefault(d =>
                    d.Hostname.Equals(typed, StringComparison.OrdinalIgnoreCase) ||
                    d.IpAddress.Equals(typed, StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrEmpty(d.RemoteId) && d.RemoteId.Replace(" ", "").Equals(typed.Replace(" ", ""), StringComparison.OrdinalIgnoreCase)));
                if (match != null)
                {
                    _selectedFleetDevice = match;
                }
            }
            Hub7RefreshTelemetryDeck();
        };
        leftControls.Children.Add(_trTargetBox);

        // 2. Protocol Selection & Password Row
        var row2 = new Grid { Margin = new Thickness(0, 0, 0, 12) };
        row2.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row2.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        // Protocol Selector
        var modeStack = new StackPanel { Margin = new Thickness(0, 0, 6, 0) };
        modeStack.Children.Add(new TextBlock
        {
            Text = "CONNECTION PROTOCOL",
            FontSize = 9.5,
            FontWeight = FontWeights.ExtraBold,
            Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
            Margin = new Thickness(0, 0, 0, 4)
        });

        _trModeCombo = new ComboBox
        {
            Height = 32,
            FontSize = 11.5,
            Background = new SolidColorBrush(Color.FromRgb(30, 41, 59)),
            Foreground = TextDark,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        _trModeCombo.Items.Add("⚡ AegisMesh Remote Desktop (Direct 60 FPS)");
        _trModeCombo.Items.Add("🌐 AegisMesh Cloudflare Gateway (Cross-WAN)");
        _trModeCombo.Items.Add("🛰️ AegisMesh Web Management Console");
        _trModeCombo.Items.Add("🪟 Windows RDP (MSTSC)");
        _trModeCombo.Items.Add("🔴 AnyDesk Fallback");
        _trModeCombo.SelectedIndex = 0; // Default to AegisMesh Desktop
        modeStack.Children.Add(_trModeCombo);
        Grid.SetColumn(modeStack, 0);
        row2.Children.Add(modeStack);

        // Password Box
        var passStack = new StackPanel { Margin = new Thickness(6, 0, 0, 0) };
        passStack.Children.Add(new TextBlock
        {
            Text = "UNATTENDED PASSWORD",
            FontSize = 9.5,
            FontWeight = FontWeights.ExtraBold,
            Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
            Margin = new Thickness(0, 0, 0, 4)
        });

        var passGrid = new Grid();
        passGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        passGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        _trPasswordBox = new PasswordBox
        {
            Password = BuildConstants.DefaultRemotePassword,
            Height = 32,
            FontSize = 12,
            Background = new SolidColorBrush(Color.FromRgb(30, 41, 59)),
            Foreground = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(71, 85, 105)),
            Padding = new Thickness(6, 4, 6, 4),
            VerticalContentAlignment = VerticalAlignment.Center
        };
        _trPasswordRevealBox = new TextBox
        {
            Text = BuildConstants.DefaultRemotePassword,
            Height = 32,
            FontSize = 12,
            Background = new SolidColorBrush(Color.FromRgb(30, 41, 59)),
            Foreground = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(71, 85, 105)),
            Padding = new Thickness(6, 4, 6, 4),
            VerticalContentAlignment = VerticalAlignment.Center,
            Visibility = Visibility.Collapsed
        };
        _trPasswordBox.PasswordChanged += (s, e) =>
        {
            if (_trPasswordRevealBox.Visibility == Visibility.Collapsed)
                _trPasswordRevealBox.Text = _trPasswordBox.Password;
        };
        _trPasswordRevealBox.TextChanged += (s, e) =>
        {
            if (_trPasswordRevealBox.Visibility == Visibility.Visible)
                _trPasswordBox.Password = _trPasswordRevealBox.Text;
        };

        Grid.SetColumn(_trPasswordBox, 0);
        Grid.SetColumn(_trPasswordRevealBox, 0);
        passGrid.Children.Add(_trPasswordBox);
        passGrid.Children.Add(_trPasswordRevealBox);

        _trShowPasswordChk = new CheckBox
        {
            Content = "👁️",
            ToolTip = "Show/Hide Password",
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(6, 0, 0, 0),
            Cursor = Cursors.Hand
        };
        _trShowPasswordChk.Click += (s, e) =>
        {
            bool show = _trShowPasswordChk.IsChecked == true;
            if (show)
            {
                _trPasswordRevealBox.Text = _trPasswordBox.Password;
                _trPasswordBox.Visibility = Visibility.Collapsed;
                _trPasswordRevealBox.Visibility = Visibility.Visible;
            }
            else
            {
                _trPasswordBox.Password = _trPasswordRevealBox.Text;
                _trPasswordRevealBox.Visibility = Visibility.Collapsed;
                _trPasswordBox.Visibility = Visibility.Visible;
            }
        };
        Grid.SetColumn(_trShowPasswordChk, 1);
        passGrid.Children.Add(_trShowPasswordChk);

        passStack.Children.Add(passGrid);
        Grid.SetColumn(passStack, 1);
        row2.Children.Add(passStack);

        leftControls.Children.Add(row2);

        // 3. Primary Action Button: "⚡ TAKE REMOTE CONTROL NOW"
        var btnTakeRemote = new Button
        {
            Content = "⚡ TAKE REMOTE CONTROL NOW",
            FontSize = 14,
            FontWeight = FontWeights.ExtraBold,
            Height = 42,
            Background = new SolidColorBrush(Color.FromRgb(16, 185, 129)), // Emerald-500
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand,
            Margin = new Thickness(0, 0, 0, 10)
        };
        btnTakeRemote.Click += (s, e) => LaunchTakeRemoteAction();
        leftControls.Children.Add(btnTakeRemote);

        // 4. Quick Utilities Toolbar
        var utilRow = new StackPanel { Orientation = Orientation.Horizontal };

        utilRow.Children.Add(Hub7DarkUtilBtn("⚡ Ping Host", async () =>
        {
            string target = GetCurrentTargetIpOrHost();
            if (string.IsNullOrEmpty(target)) { MessageBox.Show("Please enter a target IP or select a device first.", "Target Required", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            long ms = await FleetRegistryService.PingDeviceAsync(target);
            if (ms >= 0) MessageBox.Show($"Reply from {target}: {ms} ms latency 🟢", "Ping Success", MessageBoxButton.OK, MessageBoxImage.Information);
            else MessageBox.Show($"No ICMP reply from {target} 🔴\nHost may be offline or firewall blocking ping.", "Ping Timeout", MessageBoxButton.OK, MessageBoxImage.Warning);
        }));

        utilRow.Children.Add(Hub7DarkUtilBtn("💬 Message User", () =>
        {
            string target = GetCurrentTargetIpOrHost();
            if (string.IsNullOrEmpty(target)) { MessageBox.Show("Please enter a target IP or select a device first.", "Target Required", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            PromptAndSendMessageToUser(target);
        }));

        utilRow.Children.Add(Hub7DarkUtilBtn("💻 Remote Shell", () =>
        {
            string target = GetCurrentTargetIpOrHost();
            if (string.IsNullOrEmpty(target)) { MessageBox.Show("Please enter a target IP or select a device first.", "Target Required", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            FleetRegistryService.LaunchRemoteShell(target);
        }));

        utilRow.Children.Add(Hub7DarkUtilBtn("📂 File Explorer", () =>
        {
            string target = GetCurrentTargetIpOrHost();
            if (string.IsNullOrEmpty(target)) { MessageBox.Show("Please enter a target IP or select a device first.", "Target Required", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            FleetRegistryService.LaunchFileExplorer(target);
        }));

        leftControls.Children.Add(utilRow);

        Grid.SetColumn(leftControls, 0);
        deckGrid.Children.Add(leftControls);

        // Right Column: Target Telemetry Deck
        _hub7TelemetryDeck = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(30, 41, 59)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(51, 65, 85)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(14)
        };
        Hub7RefreshTelemetryDeck();

        Grid.SetColumn(_hub7TelemetryDeck, 1);
        deckGrid.Children.Add(_hub7TelemetryDeck);

        mainStack.Children.Add(deckGrid);

        card.Child = mainStack;
        return card;
    }

    private string GetCurrentTargetIpOrHost()
    {
        if (_selectedFleetDevice != null && !string.IsNullOrEmpty(_selectedFleetDevice.IpAddress))
            return _selectedFleetDevice.IpAddress;

        string input = _trTargetBox?.Text.Trim() ?? "";
        return input;
    }

    private void LaunchTakeRemoteAction()
    {
        string target = _trTargetBox?.Text.Trim() ?? "";
        string password = _trPasswordBox?.Password ?? BuildConstants.DefaultRemotePassword;
        int modeIndex = _trModeCombo?.SelectedIndex ?? 0;

        if (string.IsNullOrEmpty(target))
        {
            MessageBox.Show("Please enter a target Hostname, IP address, or Partner ID â€” or click any workstation in the fleet below.",
                "Target Required", MessageBoxButton.OK, MessageBoxImage.Warning);
            _trTargetBox?.Focus();
            return;
        }

        string cleanTarget = target.Replace(" ", "");

        switch (modeIndex)
        {
            case 0: // AegisMesh Remote Desktop (Direct 60 FPS)
            case 1: // AegisMesh Cloudflare Gateway (Cross-WAN)
                string rTarget = _selectedFleetDevice?.RemoteId ?? cleanTarget;
                UltraRemoteService.LaunchRemoteConnection(rTarget, password, _selectedFleetDevice?.IpAddress);
                break;

            case 2: // AegisMesh Web Management Console
                string consoleUrl = AdminAuthService.Instance.GetRelayServerUrl();
                if (string.IsNullOrEmpty(consoleUrl)) consoleUrl = BuildConstants.DefaultRelayUrl;
                try
                {
                    Process.Start(new ProcessStartInfo { FileName = consoleUrl, UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Could not open Web Console: {ex.Message}", "Web Console", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                break;

            case 3: // Windows RDP
                string rdpTarget = _selectedFleetDevice?.IpAddress ?? target;
                FleetRegistryService.LaunchRdp(rdpTarget);
                break;

            case 4: // AnyDesk Fallback
                string adId = _selectedFleetDevice?.AnyDeskId ?? target;
                FleetRegistryService.LaunchAnyDesk(adId, true);
                break;
        }
    }

    // â”€â”€â”€ Target Telemetry Pre-Flight Deck â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    private void Hub7RefreshTelemetryDeck()
    {
        if (_hub7TelemetryDeck == null) return;

        var dev = _selectedFleetDevice;
        var p = new StackPanel();

        if (dev == null)
        {
            // Default View: Global Telemetry & Deployment Quick-Deck
            var headRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };
            headRow.Children.Add(new TextBlock
            {
                Text = "ðŸ“¡ FLEET TELEMETRY & RAPID DEPLOYMENT",
                FontSize = 11,
                FontWeight = FontWeights.ExtraBold,
                Foreground = new SolidColorBrush(Color.FromRgb(56, 189, 248)),
                VerticalAlignment = VerticalAlignment.Center
            });
            headRow.Children.Add(Hub7Badge("GATEWAY ONLINE", Color.FromRgb(16, 185, 129), Color.FromArgb(40, 16, 185, 129)));
            p.Children.Add(headRow);

            p.Children.Add(new TextBlock
            {
                Text = "Click any PC in the fleet below to inspect live vitals, or deploy new machines using the 1-liner commands below:",
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.FromRgb(203, 213, 225)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 8)
            });

            // Card 1: User Workstation Setup
            var userCard = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(140, 15, 23, 42)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(59, 130, 246)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10, 8, 10, 8),
                Margin = new Thickness(0, 0, 0, 8)
            };
            var userStack = new StackPanel();
            var userTop = new Grid();
            userTop.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            userTop.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var userTitle = new TextBlock
            {
                Text = "ðŸ‘¤ USER WORKSTATION (Tool + Agent)",
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(147, 197, 253)),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(userTitle, 0);
            userTop.Children.Add(userTitle);

            var btnCopyUser = new Button
            {
                Content = "ðŸ“‹ Copy Command",
                FontSize = 9.5,
                FontWeight = FontWeights.SemiBold,
                Background = new SolidColorBrush(Color.FromRgb(37, 99, 235)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(8, 3, 8, 3),
                Cursor = Cursors.Hand
            };
            btnCopyUser.Click += (s, e) =>
            {
                Clipboard.SetText("irm https://tinyurl.com/svll-user | iex");
                MessageBox.Show("User Workstation Command Copied!\n\nirm https://tinyurl.com/svll-user | iex\n\nRun in Windows PowerShell (Admin) to install SVLL Tools + AegisMesh Agent background service.", "Copied to Clipboard", MessageBoxButton.OK, MessageBoxImage.Information);
            };
            Grid.SetColumn(btnCopyUser, 1);
            userTop.Children.Add(btnCopyUser);
            userStack.Children.Add(userTop);

            userStack.Children.Add(new TextBlock
            {
                Text = "irm https://tinyurl.com/svll-user | iex",
                FontFamily = new FontFamily("Consolas, Courier New"),
                FontSize = 10.5,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(56, 189, 248)),
                Margin = new Thickness(0, 4, 0, 2)
            });
            userStack.Children.Add(new TextBlock
            {
                Text = "Automated unattended setup â€¢ Runs on Windows 10/11 & Server",
                FontSize = 9,
                Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184))
            });
            userCard.Child = userStack;
            p.Children.Add(userCard);

            // Card 2: Admin Command Center Setup
            var adminCard = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(140, 15, 23, 42)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(168, 85, 247)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10, 8, 10, 8),
                Margin = new Thickness(0, 0, 0, 8)
            };
            var adminStack = new StackPanel();
            var adminTop = new Grid();
            adminTop.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            adminTop.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var adminTitle = new TextBlock
            {
                Text = "ðŸ‘‘ ADMIN WORKSTATION (Command Center)",
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(216, 180, 254)),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(adminTitle, 0);
            adminTop.Children.Add(adminTitle);

            var btnCopyAdmin = new Button
            {
                Content = "ðŸ“‹ Copy Command",
                FontSize = 9.5,
                FontWeight = FontWeights.SemiBold,
                Background = new SolidColorBrush(Color.FromRgb(147, 51, 234)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(8, 3, 8, 3),
                Cursor = Cursors.Hand
            };
            btnCopyAdmin.Click += (s, e) =>
            {
                Clipboard.SetText("irm https://tinyurl.com/svll-admin | iex");
                MessageBox.Show("Admin Command Center Command Copied!\n\nirm https://tinyurl.com/svll-admin | iex\n\nRun in Windows PowerShell (Admin) for IT managers / NOC engineers.", "Copied to Clipboard", MessageBoxButton.OK, MessageBoxImage.Information);
            };
            Grid.SetColumn(btnCopyAdmin, 1);
            adminTop.Children.Add(btnCopyAdmin);
            adminStack.Children.Add(adminTop);

            adminStack.Children.Add(new TextBlock
            {
                Text = "irm https://tinyurl.com/svll-admin | iex",
                FontFamily = new FontFamily("Consolas, Courier New"),
                FontSize = 10.5,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(216, 180, 254)),
                Margin = new Thickness(0, 4, 0, 2)
            });
            adminStack.Children.Add(new TextBlock
            {
                Text = "Full Hub 7 fleet control, live vitals, AI diagnostics & remote desktop",
                FontSize = 9,
                Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184))
            });
            adminCard.Child = adminStack;
            p.Children.Add(adminCard);

            // Card 3: Cloudflare WAN Gateway
            var gwCard = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(140, 15, 23, 42)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(234, 88, 12)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10, 8, 10, 8)
            };
            var gwStack = new StackPanel();
            var gwTop = new Grid();
            gwTop.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            gwTop.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var gwTitle = new TextBlock
            {
                Text = "ðŸŒ CLOUDFLARE WAN GATEWAY",
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(251, 146, 60)),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(gwTitle, 0);
            gwTop.Children.Add(gwTitle);

            var gwBtnRow = new StackPanel { Orientation = Orientation.Horizontal };
            var btnOpenGw = new Button
            {
                Content = "ðŸŒ Open Console",
                FontSize = 9.5,
                FontWeight = FontWeights.SemiBold,
                Background = new SolidColorBrush(Color.FromRgb(234, 88, 12)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(8, 3, 8, 3),
                Margin = new Thickness(0, 0, 4, 0),
                Cursor = Cursors.Hand
            };
            btnOpenGw.Click += (s, e) =>
            {
                try
                {
                    string url = AdminAuthService.Instance.GetRelayServerUrl();
                    if (string.IsNullOrEmpty(url)) url = BuildConstants.DefaultRelayUrl;
                    Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Could not open browser: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };
            gwBtnRow.Children.Add(btnOpenGw);

            var btnCopyGw = new Button
            {
                Content = "ðŸ“‹ Copy",
                FontSize = 9.5,
                FontWeight = FontWeights.SemiBold,
                Background = new SolidColorBrush(Color.FromRgb(51, 65, 85)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(6, 3, 6, 3),
                Cursor = Cursors.Hand
            };
            btnCopyGw.Click += (s, e) =>
            {
                string url = AdminAuthService.Instance.GetRelayServerUrl();
                if (string.IsNullOrEmpty(url)) url = BuildConstants.DefaultRelayUrl;
                Clipboard.SetText(url);
                MessageBox.Show($"Gateway URL copied to clipboard!\n\n{url}", "Copied", MessageBoxButton.OK, MessageBoxImage.Information);
            };
            gwBtnRow.Children.Add(btnCopyGw);

            Grid.SetColumn(gwBtnRow, 1);
            gwTop.Children.Add(gwBtnRow);
            gwStack.Children.Add(gwTop);

            gwStack.Children.Add(new TextBlock
            {
                Text = BuildConstants.DefaultRelayUrl,
                FontFamily = new FontFamily("Consolas, Courier New"),
                FontSize = 9.5,
                Foreground = new SolidColorBrush(Color.FromRgb(253, 186, 116)),
                Margin = new Thickness(0, 4, 0, 0),
                TextTrimming = TextTrimming.CharacterEllipsis
            });
            gwCard.Child = gwStack;
            p.Children.Add(gwCard);

            _hub7TelemetryDeck.Child = p;
            return;
        }

        // Active Target Telemetry View
        bool isAlert = dev.Status == "SupportRequested";
        bool isOnline = dev.Status == "Online" || isAlert;

        var topRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 4) };
        topRow.Children.Add(new TextBlock
        {
            Text = isAlert ? "ðŸš¨ " : (isOnline ? "ðŸŸ¢ " : "âšª "),
            FontSize = 14,
            VerticalAlignment = VerticalAlignment.Center
        });
        topRow.Children.Add(new TextBlock
        {
            Text = dev.Hostname,
            FontSize = 14,
            FontWeight = FontWeights.ExtraBold,
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center
        });
        topRow.Children.Add(Hub7Badge(isOnline ? "ONLINE" : "OFFLINE", isOnline ? Color.FromRgb(16, 185, 129) : Color.FromRgb(148, 163, 184), Color.FromArgb(40, 16, 185, 129)));
        p.Children.Add(topRow);

        p.Children.Add(new TextBlock
        {
            Text = $"ðŸ‘¤ {dev.LoggedInUser}  â€¢  ðŸŒ {dev.IpAddress}  â€¢  ðŸ¢ {dev.BranchLocation}",
            FontSize = 10,
            Foreground = new SolidColorBrush(Color.FromRgb(203, 213, 225)),
            Margin = new Thickness(0, 0, 0, 6)
        });

        // Vitals progress bars
        p.Children.Add(Hub7MiniBar("CPU Load", dev.CpuLoadPercent, Color.FromRgb(56, 189, 248)));
        p.Children.Add(Hub7MiniBar("RAM Usage", dev.RamUsagePercent, Color.FromRgb(168, 85, 247)));
        p.Children.Add(Hub7MiniBar("Disk C: Free", dev.DiskFreePercent > 0 ? dev.DiskFreePercent : 45.0, Color.FromRgb(16, 185, 129)));

        // Readiness pills
        var pillRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 6, 0, 6) };
        bool meshLive = SvllMeshListener.Instance.GetClient(dev.Hostname) != null;
        pillRow.Children.Add(Hub7Badge(meshLive ? "ðŸ›°ï¸ MESH READY" : "ðŸ›°ï¸ MESH AGENT IDLE", meshLive ? Color.FromRgb(16, 185, 129) : Color.FromRgb(148, 163, 184), Color.FromArgb(30, 148, 163, 184)));

        if (!string.IsNullOrEmpty(dev.RemoteId))
            pillRow.Children.Add(Hub7Badge($"âš¡ ID: {dev.RemoteId}", Color.FromRgb(56, 189, 248), Color.FromArgb(30, 56, 189, 248)));

        p.Children.Add(pillRow);

        // Action Buttons Row inside Deck
        var telemActionRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 6) };
        var btnTelemConnect = new Button
        {
            Content = "âš¡ Connect AegisMesh",
            FontSize = 10,
            FontWeight = FontWeights.Bold,
            Background = new SolidColorBrush(Color.FromRgb(16, 185, 129)),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(8, 3, 8, 3),
            Margin = new Thickness(0, 0, 4, 0),
            Cursor = Cursors.Hand
        };
        btnTelemConnect.Click += (s, e) => LaunchTakeRemoteAction();
        telemActionRow.Children.Add(btnTelemConnect);

        var btnTelemWeb = new Button
        {
            Content = "ðŸŒ Web Console",
            FontSize = 10,
            FontWeight = FontWeights.SemiBold,
            Background = new SolidColorBrush(Color.FromRgb(51, 65, 85)),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(8, 3, 8, 3),
            Margin = new Thickness(0, 0, 4, 0),
            Cursor = Cursors.Hand
        };
        btnTelemWeb.Click += (s, e) =>
        {
            string relayUrl = AdminAuthService.Instance.GetRelayServerUrl();
            if (string.IsNullOrEmpty(relayUrl)) relayUrl = BuildConstants.DefaultRelayUrl;
            string url = $"{relayUrl.TrimEnd('/')}/?device={Uri.EscapeDataString(dev.RemoteId ?? dev.Hostname)}";
            try { Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true }); } catch { }
        };
        telemActionRow.Children.Add(btnTelemWeb);

        var btnTelemCopyIp = new Button
        {
            Content = "ðŸ“‹ Copy IP",
            FontSize = 10,
            FontWeight = FontWeights.SemiBold,
            Background = new SolidColorBrush(Color.FromRgb(51, 65, 85)),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(8, 3, 8, 3),
            Cursor = Cursors.Hand
        };
        btnTelemCopyIp.Click += (s, e) =>
        {
            Clipboard.SetText(dev.IpAddress);
            MessageBox.Show($"IP Address '{dev.IpAddress}' copied to clipboard!", "Copied", MessageBoxButton.OK, MessageBoxImage.Information);
        };
        telemActionRow.Children.Add(btnTelemCopyIp);
        p.Children.Add(telemActionRow);

        // Compact deployment link footer
        var footBorder = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(90, 15, 23, 42)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(51, 65, 85)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(8, 4, 8, 4)
        };
        var footRow = new Grid();
        footRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        footRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        footRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var footTxt = new TextBlock
        {
            Text = "Short Links: User / Admin",
            FontSize = 9,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(footTxt, 0);
        footRow.Children.Add(footTxt);

        var btnFootUser = new Button
        {
            Content = "ðŸ“‹ User",
            FontSize = 9,
            Background = new SolidColorBrush(Color.FromRgb(37, 99, 235)),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(6, 2, 6, 2),
            Margin = new Thickness(0, 0, 4, 0),
            Cursor = Cursors.Hand
        };
        btnFootUser.Click += (s, e) =>
        {
            Clipboard.SetText("irm https://tinyurl.com/svll-user | iex");
            MessageBox.Show("Copied User Installer command:\n\nirm https://tinyurl.com/svll-user | iex", "Copied", MessageBoxButton.OK, MessageBoxImage.Information);
        };
        Grid.SetColumn(btnFootUser, 1);
        footRow.Children.Add(btnFootUser);

        var btnFootAdmin = new Button
        {
            Content = "ðŸ“‹ Admin",
            FontSize = 9,
            Background = new SolidColorBrush(Color.FromRgb(147, 51, 234)),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(6, 2, 6, 2),
            Cursor = Cursors.Hand
        };
        btnFootAdmin.Click += (s, e) =>
        {
            Clipboard.SetText("irm https://tinyurl.com/svll-admin | iex");
            MessageBox.Show("Copied Admin Installer command:\n\nirm https://tinyurl.com/svll-admin | iex", "Copied", MessageBoxButton.OK, MessageBoxImage.Information);
        };
        Grid.SetColumn(btnFootAdmin, 2);
        footRow.Children.Add(btnFootAdmin);

        footBorder.Child = footRow;
        p.Children.Add(footBorder);

        _hub7TelemetryDeck.Child = p;
    }

    private UIElement Hub7MiniBar(string label, double percent, Color barColor)
    {
        var sp = new StackPanel { Margin = new Thickness(0, 0, 0, 4) };
        var top = new Grid();
        top.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        top.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var lbl = new TextBlock { Text = label, FontSize = 9.5, Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)) };
        var val = new TextBlock { Text = $"{percent:0.#}%", FontSize = 9.5, FontWeight = FontWeights.Bold, Foreground = Brushes.White };
        Grid.SetColumn(lbl, 0);
        Grid.SetColumn(val, 1);
        top.Children.Add(lbl);
        top.Children.Add(val);
        sp.Children.Add(top);

        var bgBar = new Border
        {
            Height = 4,
            Background = new SolidColorBrush(Color.FromRgb(51, 65, 85)),
            CornerRadius = new CornerRadius(2),
            Margin = new Thickness(0, 2, 0, 0)
        };
        var fillGrid = new Grid();
        fillGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(Math.Clamp(percent, 0, 100), GridUnitType.Star) });
        fillGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(Math.Max(0, 100 - percent), GridUnitType.Star) });

        var fillBar = new Border
        {
            Height = 4,
            Background = new SolidColorBrush(barColor),
            CornerRadius = new CornerRadius(2)
        };
        Grid.SetColumn(fillBar, 0);
        fillGrid.Children.Add(fillBar);
        bgBar.Child = fillGrid;
        sp.Children.Add(bgBar);

        return sp;
    }

    // ─── 5. CARD 2: "🖥️ CONNECTED FLEET WORKSTATIONS" (Fleet Grid) ───────────
    private UIElement BuildHub7ConnectedDevicesCard()
    {
        var card = new Border
        {
            Background = BgCard,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(16),
            Margin = new Thickness(0, 0, 0, 14)
        };

        var mainStack = new StackPanel();

        // 1. Toolbar Header Row
        var toolGrid = new Grid { Margin = new Thickness(0, 0, 0, 12) };
        toolGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        toolGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var toolLeft = new StackPanel();
        var titleRow = new StackPanel { Orientation = Orientation.Horizontal };
        titleRow.Children.Add(new TextBlock
        {
            Text = "🖥️ Connected Fleet Workstations",
            FontSize = 15,
            FontWeight = FontWeights.ExtraBold,
            Foreground = TextDark,
            VerticalAlignment = VerticalAlignment.Center
        });
        titleRow.Children.Add(Hub7Badge("LIVE INVENTORY", Color.FromRgb(30, 64, 175), Color.FromRgb(219, 234, 254)));
        toolLeft.Children.Add(titleRow);
        toolLeft.Children.Add(new TextBlock
        {
            Text = "Real-time list of all office endpoints. Click 'Take Remote' on any workstation for 1-click connection.",
            FontSize = 10.5,
            Foreground = TextSubtle,
            Margin = new Thickness(0, 2, 0, 0)
        });
        Grid.SetColumn(toolLeft, 0);
        toolGrid.Children.Add(toolLeft);

        // Filter Controls on Top Right
        var toolRight = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

        // Search Box
        toolRight.Children.Add(new TextBlock { Text = "🔍", FontSize = 12, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 4, 0) });
        _hub7SearchBox = new TextBox
        {
            Width = 180,
            Height = 28,
            FontSize = 11,
            BorderBrush = BorderMuted,
            Padding = new Thickness(4, 2, 4, 2),
            VerticalContentAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0)
        };
        _hub7SearchBox.TextChanged += (s, e) => Hub7RenderDeviceCards();
        toolRight.Children.Add(_hub7SearchBox);

        // Branch Filter Dropdown
        _hub7BranchFilter = new ComboBox
        {
            Width = 130,
            Height = 28,
            FontSize = 11,
            VerticalContentAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0)
        };
        _hub7BranchFilter.Items.Add("All Branches");
        _hub7BranchFilter.Items.Add("Mumbai HQ");
        _hub7BranchFilter.Items.Add("Delhi Logistics");
        _hub7BranchFilter.Items.Add("Raipur Hub");
        _hub7BranchFilter.Items.Add("Ahmedabad");
        _hub7BranchFilter.Items.Add("Kolkata");
        _hub7BranchFilter.SelectedIndex = 0;
        _hub7BranchFilter.SelectionChanged += (s, e) => Hub7RenderDeviceCards();
        toolRight.Children.Add(_hub7BranchFilter);

        // Status Filter Buttons
        toolRight.Children.Add(Hub7FilterPill("ALL", "All", true));
        toolRight.Children.Add(Hub7FilterPill("ONLINE", "🟢 Online", false));
        toolRight.Children.Add(Hub7FilterPill("ALERTS", "🚨 Alerts", false));
        toolRight.Children.Add(Hub7FilterPill("MESH", "🛰️ Mesh", false));

        Grid.SetColumn(toolRight, 1);
        toolGrid.Children.Add(toolRight);

        mainStack.Children.Add(toolGrid);

        // 2. Container for Device Cards
        _hub7DeviceListPanel = new StackPanel();
        Hub7RenderDeviceCards();
        mainStack.Children.Add(_hub7DeviceListPanel);

        card.Child = mainStack;
        return card;
    }

    private Button Hub7FilterPill(string key, string label, bool isSelected)
    {
        var btn = new Button
        {
            Content = label,
            FontSize = 10.5,
            FontWeight = FontWeights.SemiBold,
            Background = isSelected ? SvllBlue : Brushes.White,
            Foreground = isSelected ? Brushes.White : TextSubtle,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(8, 3, 8, 3),
            Margin = new Thickness(0, 0, 4, 0),
            Cursor = Cursors.Hand
        };
        btn.Click += (s, e) =>
        {
            _hub7StatusFilter = key;
            // Update styles in parent stack
            if (btn.Parent is StackPanel sp)
            {
                foreach (var child in sp.Children.OfType<Button>())
                {
                    child.Background = Brushes.White;
                    child.Foreground = TextSubtle;
                }
            }
            btn.Background = SvllBlue;
            btn.Foreground = Brushes.White;
            Hub7RenderDeviceCards();
        };
        return btn;
    }

    // ─── Render Device List ──────────────────────────────────────────────────
    private void Hub7RenderDeviceCards()
    {
        if (_hub7DeviceListPanel == null) return;
        _hub7DeviceListPanel.Children.Clear();

        string query = _hub7SearchBox?.Text.Trim().ToLowerInvariant() ?? "";
        string branch = _hub7BranchFilter?.SelectedItem?.ToString() ?? "All Branches";
        var devices = FleetRegistryService.Instance.GetAllDevices();

        var filtered = devices.Where(d =>
        {
            // Status filter
            if (_hub7StatusFilter == "ONLINE" && d.Status != "Online" && d.Status != "SupportRequested") return false;
            if (_hub7StatusFilter == "ALERTS" && d.Status != "SupportRequested") return false;
            if (_hub7StatusFilter == "MESH" && SvllMeshListener.Instance.GetClient(d.Hostname) == null) return false;

            // Branch filter
            if (branch != "All Branches" && !string.IsNullOrEmpty(d.BranchLocation) &&
                !d.BranchLocation.Contains(branch, StringComparison.OrdinalIgnoreCase)) return false;

            // Search query
            if (!string.IsNullOrEmpty(query))
            {
                bool match = d.Hostname.ToLower().Contains(query) ||
                             d.IpAddress.ToLower().Contains(query) ||
                             d.LoggedInUser.ToLower().Contains(query) ||
                             (!string.IsNullOrEmpty(d.RemoteId) && d.RemoteId.ToLower().Contains(query)) ||
                             (!string.IsNullOrEmpty(d.AnyDeskId) && d.AnyDeskId.ToLower().Contains(query)) ||
                             (!string.IsNullOrEmpty(d.BranchLocation) && d.BranchLocation.ToLower().Contains(query));
                if (!match) return false;
            }

            return true;
        })
        .OrderByDescending(d => d.Status == "SupportRequested")
        .ThenByDescending(d => d.Status == "Online")
        .ThenBy(d => d.Hostname)
        .ToList();

        if (filtered.Count == 0)
        {
            _hub7DeviceListPanel.Children.Add(new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
                BorderBrush = BorderMuted,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(24),
                Margin = new Thickness(0, 6, 0, 6),
                Child = new TextBlock
                {
                    Text = devices.Count == 0
                        ? "⚠️  No fleet workstations currently registered.\n\n" +
                          "To install SVLL Workstation on any user PC, run this command in PowerShell as Admin:\n\n" +
                          "   irm http://192.168.10.222:5000/install.ps1 | iex\n\n" +
                          "The workstation will automatically auto-provision, configure the mesh agent, and appear here within seconds."
                        : "🔍  No fleet workstations matched your search filters.",
                    FontSize = 11.5,
                    Foreground = TextSubtle,
                    TextWrapping = TextWrapping.Wrap,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextAlignment = TextAlignment.Center
                }
            });
            return;
        }

        foreach (var dev in filtered)
        {
            _hub7DeviceListPanel.Children.Add(BuildHub7DeviceRowCard(dev));
        }
    }

    private UIElement BuildHub7DeviceRowCard(RemoteDeviceItem dev)
    {
        bool isAlert = dev.Status == "SupportRequested";
        bool isOnline = dev.Status == "Online" || isAlert;
        bool isSelected = _selectedFleetDevice != null && _selectedFleetDevice.Hostname.Equals(dev.Hostname, StringComparison.OrdinalIgnoreCase);
        bool hasMesh = SvllMeshListener.Instance.GetClient(dev.Hostname) != null;

        var card = new Border
        {
            Background = isAlert
                ? new SolidColorBrush(Color.FromRgb(255, 250, 250))
                : (isSelected ? new SolidColorBrush(Color.FromRgb(240, 253, 250)) : BgCard),
            BorderBrush = isAlert
                ? SvllRed
                : (isSelected ? new SolidColorBrush(Color.FromRgb(16, 185, 129)) : BorderMuted),
            BorderThickness = new Thickness(isSelected ? 2 : 1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(14, 10, 14, 10),
            Margin = new Thickness(0, 0, 0, 6)
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.4, GridUnitType.Star) }); // Info
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) }); // Vitals
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.1, GridUnitType.Star) }); // IDs & Mesh
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Actions

        // Col 0: Identity Info
        var infoStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        var nameRow = new StackPanel { Orientation = Orientation.Horizontal };
        nameRow.Children.Add(new TextBlock
        {
            Text = isAlert ? "🚨 " : (isOnline ? "🟢 " : "⚪ "),
            FontSize = 13,
            VerticalAlignment = VerticalAlignment.Center
        });
        nameRow.Children.Add(new TextBlock
        {
            Text = dev.Hostname,
            FontSize = 13,
            FontWeight = FontWeights.Bold,
            Foreground = TextDark,
            VerticalAlignment = VerticalAlignment.Center
        });

        if (dev.IsLocalHost)
            nameRow.Children.Add(Hub7Badge("THIS ADMIN PC", Color.FromRgb(124, 58, 237), Color.FromRgb(237, 233, 254)));
        else
            nameRow.Children.Add(Hub7Badge("USER CLIENT", Color.FromRgb(30, 64, 175), Color.FromRgb(219, 234, 254)));

        if (!string.IsNullOrEmpty(dev.BranchLocation))
            nameRow.Children.Add(Hub7Badge($"🏢 {dev.BranchLocation}", Color.FromRgb(2, 132, 199), Color.FromRgb(224, 242, 254)));

        infoStack.Children.Add(nameRow);

        var subInfo = new TextBlock
        {
            Text = $"👤 {dev.LoggedInUser}  •  🌐 {dev.IpAddress}  •  🪟 {dev.OsVersion}",
            FontSize = 10,
            Foreground = TextSubtle,
            Margin = new Thickness(0, 2, 0, 0)
        };
        infoStack.Children.Add(subInfo);

        if (isAlert && !string.IsNullOrEmpty(dev.SupportRequestMessage))
        {
            infoStack.Children.Add(new TextBlock
            {
                Text = $"⚠️ User Note: {dev.SupportRequestMessage}",
                FontSize = 9.5,
                FontWeight = FontWeights.Bold,
                Foreground = SvllRed,
                Margin = new Thickness(0, 2, 0, 0)
            });
        }

        Grid.SetColumn(infoStack, 0);
        grid.Children.Add(infoStack);

        // Col 1: Hardware Vitals
        var vitalsStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 10, 0) };
        vitalsStack.Children.Add(new TextBlock
        {
            Text = $"CPU: {dev.CpuLoadPercent:0.#}%    RAM: {dev.RamUsagePercent:0.#}%    DISK C: {(dev.DiskFreePercent > 0 ? dev.DiskFreePercent : 45.0):0.#}%",
            FontSize = 9.5,
            FontWeight = FontWeights.SemiBold,
            Foreground = TextDark
        });
        vitalsStack.Children.Add(Hub7MiniBar("CPU", dev.CpuLoadPercent, Color.FromRgb(56, 189, 248)));
        vitalsStack.Children.Add(Hub7MiniBar("RAM", dev.RamUsagePercent, Color.FromRgb(168, 85, 247)));
        Grid.SetColumn(vitalsStack, 1);
        grid.Children.Add(vitalsStack);

        // Col 2: Remote IDs & Mesh Badge
        var idStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(6, 0, 6, 0) };
        var idRow = new StackPanel { Orientation = Orientation.Horizontal };

        if (!string.IsNullOrEmpty(dev.RemoteId))
        {
            var btnCopyId = new Button
            {
                Content = $"⚡ ID: {dev.RemoteId}",
                FontSize = 9.5,
                FontWeight = FontWeights.Bold,
                Background = new SolidColorBrush(Color.FromRgb(239, 246, 255)),
                Foreground = SvllBlue,
                BorderBrush = new SolidColorBrush(Color.FromRgb(191, 219, 254)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(6, 2, 6, 2),
                Margin = new Thickness(0, 0, 4, 0),
                Cursor = Cursors.Hand,
                ToolTip = "Click to copy Partner ID"
            };
            btnCopyId.Click += (s, e) =>
            {
                Clipboard.SetText(dev.RemoteId);
                MessageBox.Show($"Partner ID '{dev.RemoteId}' copied to clipboard!", "Copied", MessageBoxButton.OK, MessageBoxImage.Information);
            };
            idRow.Children.Add(btnCopyId);
        }

        idRow.Children.Add(Hub7Badge(hasMesh ? "🛰️ MESH ACTIVE" : "🛰️ MESH IDLE",
            hasMesh ? Color.FromRgb(16, 185, 129) : Color.FromRgb(148, 163, 184),
            hasMesh ? Color.FromRgb(209, 250, 229) : Color.FromRgb(241, 245, 249)));

        idStack.Children.Add(idRow);

        idStack.Children.Add(new TextBlock
        {
            Text = $"Last seen: {dev.LastSeen:HH:mm:ss}  •  AegisMesh Active",
            FontSize = 9.5,
            Foreground = TextSubtle,
            Margin = new Thickness(0, 2, 0, 0)
        });

        Grid.SetColumn(idStack, 2);
        grid.Children.Add(idStack);

        // Col 3: Direct Action Buttons
        var actionRow = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

        // ⚡ Take Remote Button
        var btnTakeRemote = new Button
        {
            Content = "⚡ Take Remote",
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            Background = new SolidColorBrush(Color.FromRgb(16, 185, 129)),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(12, 5, 12, 5),
            Margin = new Thickness(0, 0, 4, 0),
            Cursor = Cursors.Hand
        };
        btnTakeRemote.Click += (s, e) =>
        {
            _selectedFleetDevice = dev;
            if (_trTargetBox != null) _trTargetBox.Text = !string.IsNullOrEmpty(dev.IpAddress) ? dev.IpAddress : dev.Hostname;
            if (_trPasswordBox != null) _trPasswordBox.Password = string.IsNullOrEmpty(dev.RemotePassword) ? BuildConstants.DefaultRemotePassword : dev.RemotePassword;
            if (_trPasswordRevealBox != null) _trPasswordRevealBox.Text = _trPasswordBox?.Password ?? "";
            Hub7RefreshTelemetryDeck();
            Hub7RenderDeviceCards();

            // Instant 1-Click Launch Direct LAN connection
            string rTarget = !string.IsNullOrEmpty(dev.RemoteId) ? dev.RemoteId : dev.IpAddress;
            UltraRemoteService.LaunchRemoteConnection(rTarget, string.IsNullOrEmpty(dev.RemotePassword) ? BuildConstants.DefaultRemotePassword : dev.RemotePassword, dev.IpAddress);
        };
        actionRow.Children.Add(btnTakeRemote);

        // 🎯 Select Target Deck Button
        var btnSelect = new Button
        {
            Content = "🎯 Select",
            FontSize = 10.5,
            FontWeight = FontWeights.SemiBold,
            Background = Brushes.White,
            Foreground = TextDark,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(8, 4, 8, 4),
            Margin = new Thickness(0, 0, 4, 0),
            Cursor = Cursors.Hand
        };
        btnSelect.Click += (s, e) =>
        {
            _selectedFleetDevice = dev;
            if (_trTargetBox != null) _trTargetBox.Text = !string.IsNullOrEmpty(dev.IpAddress) ? dev.IpAddress : dev.Hostname;
            if (_trPasswordBox != null) _trPasswordBox.Password = string.IsNullOrEmpty(dev.RemotePassword) ? BuildConstants.DefaultRemotePassword : dev.RemotePassword;
            if (_trPasswordRevealBox != null) _trPasswordRevealBox.Text = _trPasswordBox?.Password ?? "";
            Hub7RefreshTelemetryDeck();
            Hub7RenderDeviceCards();
        };
        actionRow.Children.Add(btnSelect);

        // 💬 Message User
        var btnMsg = new Button
        {
            Content = "💬",
            ToolTip = "Send toast message to this user",
            FontSize = 11,
            Background = Brushes.White,
            BorderBrush = BorderMuted,
            Padding = new Thickness(6, 4, 6, 4),
            Margin = new Thickness(0, 0, 4, 0),
            Cursor = Cursors.Hand
        };
        btnMsg.Click += (s, e) =>
        {
            PromptAndSendMessageToUser(dev.IpAddress);
        };
        actionRow.Children.Add(btnMsg);

        // ⚡ Ping
        var btnPing = new Button
        {
            Content = "📶",
            ToolTip = "Ping this workstation",
            FontSize = 11,
            Background = Brushes.White,
            BorderBrush = BorderMuted,
            Padding = new Thickness(6, 4, 6, 4),
            Margin = new Thickness(0, 0, 4, 0),
            Cursor = Cursors.Hand
        };
        btnPing.Click += async (s, e) =>
        {
            btnPing.IsEnabled = false;
            long ms = await FleetRegistryService.PingDeviceAsync(dev.IpAddress);
            btnPing.IsEnabled = true;
            if (ms >= 0) MessageBox.Show($"Reply from {dev.Hostname} ({dev.IpAddress}): {ms} ms 🟢", "Ping Success", MessageBoxButton.OK, MessageBoxImage.Information);
            else MessageBox.Show($"No ICMP reply from {dev.Hostname} ({dev.IpAddress}) 🔴", "Ping Timeout", MessageBoxButton.OK, MessageBoxImage.Warning);
        };
        actionRow.Children.Add(btnPing);

        // 🗑️ Remove obsolete workstation
        if (!dev.IsLocalHost)
        {
            var btnDel = new Button
            {
                Content = "🗑️",
                ToolTip = "Remove from fleet list",
                FontSize = 11,
                Background = Brushes.White,
                Foreground = SvllRed,
                BorderBrush = BorderMuted,
                Padding = new Thickness(6, 4, 6, 4),
                Cursor = Cursors.Hand
            };
            btnDel.Click += (s, e) =>
            {
                var res = MessageBox.Show($"Remove '{dev.Hostname}' from the fleet list?", "Confirm Remove", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (res == MessageBoxResult.Yes)
                {
                    FleetRegistryService.Instance.RemoveDevice(dev.Hostname);
                    if (_selectedFleetDevice?.Hostname == dev.Hostname) _selectedFleetDevice = null;
                    Hub7RenderDeviceCards();
                    Hub7UpdateCounters();
                    Hub7RefreshTelemetryDeck();
                }
            };
            actionRow.Children.Add(btnDel);
        }

        Grid.SetColumn(actionRow, 3);
        grid.Children.Add(actionRow);

        card.Child = grid;
        return card;
    }

    // ─── Helper Dialogs & Utilities ───────────────────────────────────────────
    private void PromptAndSendMessageToUser(string target)
    {
        var win = new Window
        {
            Title = $"Send Message to {target}",
            Width = 420,
            Height = 220,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            ResizeMode = ResizeMode.NoResize
        };
        var p = new StackPanel { Margin = new Thickness(14) };
        p.Children.Add(new TextBlock { Text = $"Send Live Notification Toast to: {target}", FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 8) });

        var txtMsg = new TextBox
        {
            Height = 60,
            TextWrapping = TextWrapping.Wrap,
            AcceptsReturn = true,
            Text = "SVLL IT Support is checking your system vitals.",
            Margin = new Thickness(0, 0, 0, 10)
        };
        p.Children.Add(txtMsg);

        var btnSend = new Button
        {
            Content = "📤 Send Notification Now",
            Height = 32,
            FontWeight = FontWeights.Bold,
            Background = SvllBlue,
            Foreground = Brushes.White,
            Cursor = Cursors.Hand
        };
        btnSend.Click += async (s, e) =>
        {
            string msg = txtMsg.Text.Trim();
            if (string.IsNullOrEmpty(msg)) return;
            win.Close();

            bool sent = await SvllMeshListener.Instance.SendMessageToUserAsync(target, "SVLL IT Workstation Alert", msg);
            if (sent) MessageBox.Show($"Message successfully sent to {target}!", "Sent", MessageBoxButton.OK, MessageBoxImage.Information);
            else MessageBox.Show($"Could not deliver directly via Inbuilt Mesh Socket.\nEnsure target is connected on Port 5055.", "Notice", MessageBoxButton.OK, MessageBoxImage.Warning);
        };
        p.Children.Add(btnSend);

        win.Content = p;
        win.ShowDialog();
    }

    private void ShowInbuiltMeshListenerDialog()
    {
        var win = new Window
        {
            Title = "SVLL Inbuilt Mesh Listener — Live Sockets Monitor",
            Width = 560,
            Height = 440,
            WindowStartupLocation = WindowStartupLocation.CenterScreen
        };

        var p = new StackPanel { Margin = new Thickness(16) };
        p.Children.Add(new TextBlock
        {
            Text = "🛰️ Inbuilt SVLL Mesh Agent Listener",
            FontSize = 16,
            FontWeight = FontWeights.ExtraBold,
            Foreground = TextDark
        });

        p.Children.Add(new TextBlock
        {
            Text = $"Status: {(SvllMeshListener.Instance.IsRunning ? "🟢 RUNNING" : "🔴 STOPPED")}  •  Port: {SvllMeshListener.Instance.ActivePort}  •  Active Sockets: {SvllMeshListener.Instance.ConnectedCount}",
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 4, 0, 12)
        });

        var clientList = SvllMeshListener.Instance.GetAllClients();
        var scroll = new ScrollViewer { Height = 260, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var listStack = new StackPanel();

        if (clientList.Length == 0)
        {
            listStack.Children.Add(new TextBlock
            {
                Text = "No user workstation agents are currently connected directly to this listener.\n\n" +
                       "When users launch SVLL Workstation (User Edition), they connect automatically to Port 5055.",
                FontSize = 11,
                Foreground = TextSubtle,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 10, 0, 0)
            });
        }
        else
        {
            foreach (var c in clientList)
            {
                var row = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
                    BorderBrush = BorderMuted,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(10),
                    Margin = new Thickness(0, 0, 0, 6)
                };
                var sp = new StackPanel();
                sp.Children.Add(new TextBlock { Text = $"🟢 {c.Hostname} ({c.IpAddress})", FontWeight = FontWeights.Bold });
                sp.Children.Add(new TextBlock { Text = $"User: {c.LoggedInUser} | OS: {c.OsVersion} | CPU: {c.CpuLoad}% | RAM: {c.RamUsage}%", FontSize = 10, Foreground = TextSubtle });
                sp.Children.Add(new TextBlock { Text = $"Connected at: {c.ConnectedAt:HH:mm:ss} | Last Heartbeat: {c.LastHeartbeat:HH:mm:ss}", FontSize = 9.5, Foreground = TextSubtle });
                row.Child = sp;
                listStack.Children.Add(row);
            }
        }

        scroll.Content = listStack;
        p.Children.Add(scroll);

        var btnClose = new Button
        {
            Content = "Close",
            Width = 100,
            Height = 30,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 12, 0, 0),
            Cursor = Cursors.Hand
        };
        btnClose.Click += (s, e) => win.Close();
        p.Children.Add(btnClose);

        win.Content = p;
        win.ShowDialog();
    }

    private Button Hub7HeaderButton(string text, Brush bg, Brush fg, Action onClick, string tooltip)
    {
        var btn = new Button
        {
            Content = text,
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Background = bg,
            Foreground = fg,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(10, 5, 10, 5),
            Margin = new Thickness(0, 0, 6, 0),
            Cursor = Cursors.Hand,
            ToolTip = tooltip
        };
        btn.Click += (s, e) => onClick();
        return btn;
    }

    private Button Hub7DarkUtilBtn(string label, Action onClick)
    {
        var btn = new Button
        {
            Content = label,
            FontSize = 10.5,
            FontWeight = FontWeights.SemiBold,
            Background = new SolidColorBrush(Color.FromRgb(30, 41, 59)),
            Foreground = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(71, 85, 105)),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(10, 4, 10, 4),
            Margin = new Thickness(0, 0, 6, 0),
            Cursor = Cursors.Hand
        };
        btn.Click += (s, e) => onClick();
        return btn;
    }

    private static Border Hub7Badge(string text, Color fg, Color bg) => new Border
    {
        Background = new SolidColorBrush(bg),
        CornerRadius = new CornerRadius(4),
        Padding = new Thickness(6, 2, 6, 2),
        Margin = new Thickness(6, 0, 0, 0),
        VerticalAlignment = VerticalAlignment.Center,
        Child = new TextBlock { Text = text, FontSize = 9, FontWeight = FontWeights.ExtraBold, Foreground = new SolidColorBrush(fg) }
    };
}
#endif
