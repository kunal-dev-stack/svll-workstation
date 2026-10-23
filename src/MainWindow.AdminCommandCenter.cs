// =============================================================================
// SVLL IT Workstation v8.0 — Admin Command Center
// Unified Fleet Monitor + 1-Click Remote Control
// =============================================================================
#if BUILD_ADMIN

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using SVLL_IT_Workstation.Models;

namespace SVLL_IT_Workstation;

public partial class MainWindow
{
    // ─── shared state for this panel ─────────────────────────────────────────
    private static System.Windows.Threading.DispatcherTimer? _accAutoRefreshTimer;
    private StackPanel  _accDeviceCards    = null!;
    private TextBlock   _accTotalLbl       = null!;
    private TextBlock   _accOnlineLbl      = null!;
    private TextBlock   _accOfflineLbl     = null!;
    private TextBlock   _accAlertLbl       = null!;
    private TextBox     _accSearchBox      = null!;
    private Border      _accRelayBanner    = null!;

    // ─── Entry Point ─────────────────────────────────────────────────────────
    public UIElement BuildAdminCommandCenterView()
    {
        var rootGrid = new Grid();

        void ShowContent()
        {
            rootGrid.Children.Clear();
            rootGrid.Children.Add(BuildAccContent());
            StartAccAutoRefresh();
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
                StopAccAutoRefresh();
                rootGrid.Children.Clear();
                rootGrid.Children.Add(BuildAdminLockScreen("Admin Command Center", ShowContent));
            }
        });

        return rootGrid;
    }

    // ─── Full content ─────────────────────────────────────────────────────────
    private UIElement BuildAccContent()
    {
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var root   = new StackPanel { Margin = new Thickness(8) };

        root.Children.Add(BuildAccHeader());
        root.Children.Add(BuildAccRelayBanner());
        root.Children.Add(BuildAccMetricsRow());
        root.Children.Add(BuildAccDirectConnectBar());
        root.Children.Add(BuildAccSearchBar());

        _accDeviceCards = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };
        AccRenderDeviceCards();
        root.Children.Add(_accDeviceCards);

        scroll.Content = root;
        return scroll;
    }

    // ─── Header ───────────────────────────────────────────────────────────────
    private UIElement BuildAccHeader()
    {
        var g = new Grid();
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var left = new StackPanel();
        var titleRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 2) };
        titleRow.Children.Add(new TextBlock
        {
            Text = "👑 Admin Command Center",
            FontSize = 18, FontWeight = FontWeights.ExtraBold, Foreground = TextDark,
            VerticalAlignment = VerticalAlignment.Center
        });
        titleRow.Children.Add(AccBadge("ADMIN ONLY", Color.FromRgb(146,64,14), Color.FromRgb(254,243,199)));
        left.Children.Add(titleRow);
        left.Children.Add(new TextBlock
        {
            Text = "Live fleet monitoring and 1-click remote control for all connected user PCs.",
            FontSize = 11, Foreground = TextSubtle
        });
        Grid.SetColumn(left, 0);
        g.Children.Add(left);

        var right = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        right.Children.Add(AccHeaderBtn("🌐 MeshCentral", Color.FromRgb(22,101,52), Brushes.White,
            () => MeshAgentService.LaunchMeshDashboard(AdminAuthService.Instance.GetMeshCentralUrl()),
            "Open MeshCentral web console"));
        right.Children.Add(AccHeaderBtn("📄 Export CSV", null, null,
            () =>
            {
                try
                {
                    string csv  = FleetRegistryService.Instance.ExportFleetCsv();
                    string path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "SVLL_Fleet.csv");
                    System.IO.File.WriteAllText(path, csv);
                    MessageBox.Show($"Exported to:\n{path}", "Done", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
            }, null));
        right.Children.Add(AccHeaderBtn("⚙️ Server Config", null, null, () => ShowServerSettingsDialog(), "Edit relay and MeshCentral URLs"));
        right.Children.Add(AccHeaderBtn("🔄 Refresh Now", null, null, async () =>
        {
            await CloudSyncService.Instance.PollFleetAsync();
            AccRenderDeviceCards();
            AccUpdateMetrics();
            RefreshAccRelayBanner();
        }, "Force poll from relay"));
        right.Children.Add(AccHeaderBtn("🔒 Lock", null, null, () => AdminAuthService.Instance.LockAdmin(), null));
        Grid.SetColumn(right, 1);
        g.Children.Add(right);

        return new Border
        {
            Background = BgCard, BorderBrush = BorderMuted, BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12), Padding = new Thickness(16, 12, 16, 12),
            Margin = new Thickness(0, 0, 0, 10), Child = g
        };
    }

    // ─── Relay status banner ──────────────────────────────────────────────────
    private UIElement BuildAccRelayBanner()
    {
        _accRelayBanner = new Border { CornerRadius = new CornerRadius(10), Margin = new Thickness(0, 0, 0, 10) };
        RefreshAccRelayBanner();
        return _accRelayBanner;
    }

    private void RefreshAccRelayBanner()
    {
        if (_accRelayBanner == null) return;
        bool ok  = CloudSyncService.Instance.IsRelayReachable;
        string u = AdminAuthService.Instance.GetRelayServerUrl();

        var g = new Grid();
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var info = new StackPanel();
        if (ok)
        {
            _accRelayBanner.Background     = new SolidColorBrush(Color.FromRgb(240,253,244));
            _accRelayBanner.BorderBrush    = new SolidColorBrush(Color.FromRgb(187,247,208));
            _accRelayBanner.BorderThickness = new Thickness(1.5);
            _accRelayBanner.Padding        = new Thickness(14, 8, 14, 8);
            info.Children.Add(new TextBlock { Text = "🟢  Relay Server Connected — " + u, FontSize = 12, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(22,101,52)) });
            info.Children.Add(new TextBlock { Text = "Fleet syncing every 30 s. User PCs appear automatically when they send their first heartbeat.", FontSize = 10.5, Foreground = TextSubtle });
        }
        else
        {
            _accRelayBanner.Background      = new SolidColorBrush(Color.FromRgb(254,242,242));
            _accRelayBanner.BorderBrush     = SvllRed;
            _accRelayBanner.BorderThickness = new Thickness(1.5);
            _accRelayBanner.Padding         = new Thickness(14, 10, 14, 10);
            info.Children.Add(new TextBlock { Text = "🔴  Cannot reach Relay Server  —  " + u, FontSize = 12, FontWeight = FontWeights.Bold, Foreground = SvllRed });
            info.Children.Add(new TextBlock
            {
                Text = "Fix:  (1) On the server PC (192.168.10.222) open PowerShell as Admin and run the server installer.\n" +
                       "      (2) Check that TCP port 5000 is allowed through Windows Firewall on the server PC.\n" +
                       "      (3) Both PCs must be on the same LAN or the server port must be forwarded through your router.\n" +
                       "      (4) Click ⚙️ Server Config to verify the URL matches the actual server address.",
                FontSize = 10, Foreground = new SolidColorBrush(Color.FromRgb(185,28,28)),
                TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 4, 0, 0)
            });
        }
        Grid.SetColumn(info, 0);
        g.Children.Add(info);

        var right = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        var btnRetry = new Button
        {
            Content = "⟳ Retry", FontSize = 11, FontWeight = FontWeights.Bold,
            Background = ok ? new SolidColorBrush(Color.FromRgb(240,253,244)) : SvllRed,
            Foreground  = ok ? TextSubtle : Brushes.White,
            BorderBrush = BorderMuted, BorderThickness = new Thickness(ok ? 1 : 0),
            Padding = new Thickness(12,5,12,5), Margin = new Thickness(0,0,6,0), Cursor = Cursors.Hand
        };
        btnRetry.Click += async (s, e) =>
        {
            btnRetry.Content = "⏳"; btnRetry.IsEnabled = false;
            await Task.Run(async () =>
            {
                CloudSyncService.Instance.Stop();
                CloudSyncService.Instance.Start(AdminAuthService.Instance.GetRelayServerUrl());
                await Task.Delay(3500);
            });
            btnRetry.Content = "⟳ Retry"; btnRetry.IsEnabled = true;
            RefreshAccRelayBanner();
            AccRenderDeviceCards();
            AccUpdateMetrics();
        };
        right.Children.Add(btnRetry);
        var btnCfg = new Button
        {
            Content = "⚙️ Configure", FontSize = 11, Background = Brushes.White,
            BorderBrush = BorderMuted, Padding = new Thickness(10,5,10,5), Cursor = Cursors.Hand
        };
        btnCfg.Click += (s, e) => ShowServerSettingsDialog();
        right.Children.Add(btnCfg);
        Grid.SetColumn(right, 1);
        g.Children.Add(right);

        _accRelayBanner.Child = g;
    }

    // ─── Metric cards ─────────────────────────────────────────────────────────
    private UIElement BuildAccMetricsRow()
    {
        var grid = new Grid { Margin = new Thickness(0, 0, 0, 10) };
        for (int i = 0; i < 4; i++)
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var devs   = FleetRegistryService.Instance.GetAllDevices();
        _accTotalLbl   = MakeMetricVal(devs.Count.ToString(), SvllBlue);
        _accOnlineLbl  = MakeMetricVal(devs.Count(d => d.Status == "Online").ToString(), new SolidColorBrush(Color.FromRgb(22,101,52)));
        _accAlertLbl   = MakeMetricVal(devs.Count(d => d.Status == "SupportRequested").ToString(), SvllRed);
        _accOfflineLbl = MakeMetricVal(devs.Count(d => d.Status == "Offline").ToString(), TextSubtle);

        (string icon, string label, TextBlock val, string sub, Color bg, Color border)[] cards =
        {
            ("🖥️", "TOTAL PCs",     _accTotalLbl,   "In fleet", Color.FromRgb(239,246,255), Color.FromRgb(191,219,254)),
            ("🟢", "ONLINE NOW",    _accOnlineLbl,  "Sending heartbeats", Color.FromRgb(240,253,244), Color.FromRgb(187,247,208)),
            ("🚨", "NEED HELP",     _accAlertLbl,   "Support requested", Color.FromRgb(254,242,242), Color.FromRgb(252,165,165)),
            ("⚪", "OFFLINE",       _accOfflineLbl, "No recent heartbeat", Color.FromRgb(248,250,252), Color.FromRgb(226,232,240))
        };

        for (int i = 0; i < cards.Length; i++)
        {
            var (icon, label, val, sub, bg, border) = cards[i];
            var card = new Border
            {
                Background = new SolidColorBrush(bg), BorderBrush = new SolidColorBrush(border),
                BorderThickness = new Thickness(1.5), CornerRadius = new CornerRadius(12),
                Padding = new Thickness(16,12,16,12),
                Margin  = new Thickness(i==0?0:5, 0, i==3?0:5, 0)
            };
            var sp = new StackPanel();
            sp.Children.Add(new TextBlock { Text = $"{icon}  {label}", FontSize = 9.5, FontWeight = FontWeights.ExtraBold, Foreground = TextSubtle });
            sp.Children.Add(val);
            sp.Children.Add(new TextBlock { Text = sub, FontSize = 9.5, Foreground = TextSubtle, Margin = new Thickness(0,2,0,0) });
            card.Child = sp;
            Grid.SetColumn(card, i);
            grid.Children.Add(card);
        }
        return grid;
    }

    private TextBlock MakeMetricVal(string text, Brush brush) =>
        new TextBlock { Text = text, FontSize = 26, FontWeight = FontWeights.ExtraBold, Foreground = brush, Margin = new Thickness(0,4,0,0) };

    private void AccUpdateMetrics()
    {
        try
        {
            var devs = FleetRegistryService.Instance.GetAllDevices();
            if (_accTotalLbl   != null) _accTotalLbl.Text   = devs.Count.ToString();
            if (_accOnlineLbl  != null) _accOnlineLbl.Text  = devs.Count(d => d.Status == "Online").ToString();
            if (_accAlertLbl   != null) _accAlertLbl.Text   = devs.Count(d => d.Status == "SupportRequested").ToString();
            if (_accOfflineLbl != null) _accOfflineLbl.Text = devs.Count(d => d.Status == "Offline").ToString();
        }
        catch { }
    }

    // ─── Direct connect bar ───────────────────────────────────────────────────
    private UIElement BuildAccDirectConnectBar()
    {
        var card = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(30,41,59)),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(16,12,16,12),
            Margin = new Thickness(0,0,0,10)
        };
        var sp = new StackPanel();
        sp.Children.Add(new TextBlock
        {
            Text = "⚡  QUICK CONNECT  —  Type any IP Address / Computer Name / AnyDesk ID and click an action",
            FontSize = 11.5, FontWeight = FontWeights.Bold, Foreground = Brushes.White, Margin = new Thickness(0,0,0,8)
        });

        var row = new StackPanel { Orientation = Orientation.Horizontal };

        // Input field
        var txtTarget = new TextBox
        {
            Width = 220, Height = 32, FontSize = 13,
            FontFamily = new FontFamily("Consolas, Courier New"),
            FontWeight = FontWeights.Bold,
            Padding = new Thickness(10,4,10,4),
            VerticalContentAlignment = VerticalAlignment.Center,
            Background = new SolidColorBrush(Color.FromRgb(51,65,85)),
            Foreground = Brushes.White, CaretBrush = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(100,116,139)),
            BorderThickness = new Thickness(1),
            Margin = new Thickness(0,0,8,0)
        };
        row.Children.Add(txtTarget);

        string Tgt() => txtTarget.Text.Trim();
        void Guard(Action<string> a) { var t = Tgt(); if (string.IsNullOrEmpty(t)) MessageBox.Show("Enter a target first.", "Required", MessageBoxButton.OK, MessageBoxImage.Information); else a(t); }

        row.Children.Add(DkBtn("🚀 AnyDesk",    Color.FromRgb(211,47,47),   () => Guard(t => FleetRegistryService.LaunchAnyDesk(t))));
        row.Children.Add(DkBtn("💻 RDP",        Color.FromRgb(30,64,175),   () => Guard(t => FleetRegistryService.LaunchRdp(t))));
        row.Children.Add(DkBtn("📁 C$ Files",   Color.FromRgb(71,85,105),   () => Guard(t => FleetRegistryService.LaunchFileExplorer(t))));
        row.Children.Add(DkBtn("📡 PS Shell",   Color.FromRgb(26,86,65),    () => Guard(t => FleetRegistryService.LaunchRemoteShell(t))));
        row.Children.Add(DkBtn("⚙️ Manage",    Color.FromRgb(51,65,85),    () => Guard(t => FleetRegistryService.LaunchComputerManagement(t))));

        var btnPing = DkBtn("⚡ Ping", Color.FromRgb(71,85,105), null!);
        btnPing.Click += async (s, e) =>
        {
            var t = Tgt(); if (string.IsNullOrEmpty(t)) return;
            btnPing.Content = "⏳"; btnPing.IsEnabled = false;
            long ms = await FleetRegistryService.PingDeviceAsync(t);
            btnPing.IsEnabled = true; btnPing.Content = "⚡ Ping";
            if (ms >= 0) MessageBox.Show($"Reply from {t}: {ms} ms 🟢", "Ping OK", MessageBoxButton.OK, MessageBoxImage.Information);
            else MessageBox.Show($"No reply from {t} 🔴", "Timeout", MessageBoxButton.OK, MessageBoxImage.Warning);
        };
        row.Children.Add(btnPing);

        sp.Children.Add(row);
        card.Child = sp;
        return card;
    }

    private Button DkBtn(string label, Color bg, Action? onClick)
    {
        var btn = new Button
        {
            Content = label, FontSize = 11, FontWeight = FontWeights.SemiBold,
            Background = new SolidColorBrush(bg), Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(12,6,12,6), Margin = new Thickness(0,0,6,0),
            Cursor = Cursors.Hand, Height = 32
        };
        if (onClick != null) btn.Click += (s, e) => onClick();
        return btn;
    }

    // ─── Search bar ───────────────────────────────────────────────────────────
    private UIElement BuildAccSearchBar()
    {
        var card = new Border
        {
            Background = BgCard, BorderBrush = BorderMuted, BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10), Padding = new Thickness(12,8,12,8),
            Margin = new Thickness(0,0,0,8)
        };
        var g = new Grid();
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var searchRow = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        searchRow.Children.Add(new TextBlock { Text = "🔍", FontSize = 14, Margin = new Thickness(0,0,8,0), VerticalAlignment = VerticalAlignment.Center });
        _accSearchBox = new TextBox
        {
            FontSize = 11.5, Height = 28, Width = 340, Padding = new Thickness(6,2,6,2),
            BorderBrush = BorderMuted, VerticalContentAlignment = VerticalAlignment.Center
        };
        _accSearchBox.TextChanged += (s, e) => AccRenderDeviceCards();
        searchRow.Children.Add(_accSearchBox);
        searchRow.Children.Add(new TextBlock { Text = "  Filter by hostname, IP, user, AnyDesk ID, MAC, Serial, branch…", FontSize = 10, Foreground = TextSubtle, VerticalAlignment = VerticalAlignment.Center });
        Grid.SetColumn(searchRow, 0);
        g.Children.Add(searchRow);

        var pill = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(240,253,244)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(187,247,208)),
            BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(20),
            Padding = new Thickness(10,3,10,3), VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock { Text = "🟢 Live · auto-sync 30s", FontSize = 9.5, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(22,101,52)) }
        };
        Grid.SetColumn(pill, 1);
        g.Children.Add(pill);
        card.Child = g;
        return card;
    }

    // ─── Device cards ─────────────────────────────────────────────────────────
    private void AccRenderDeviceCards()
    {
        if (_accDeviceCards == null) return;
        _accDeviceCards.Children.Clear();

        string q    = _accSearchBox?.Text.Trim().ToLowerInvariant() ?? "";
        var devices = FleetRegistryService.Instance.GetAllDevices();

        var filtered = devices
            .Where(d => string.IsNullOrEmpty(q) ||
                d.Hostname.ToLower().Contains(q) ||
                d.IpAddress.ToLower().Contains(q) ||
                d.LoggedInUser.ToLower().Contains(q) ||
                d.AnyDeskId.ToLower().Contains(q) ||
                (!string.IsNullOrEmpty(d.SerialNumber) && d.SerialNumber.ToLower().Contains(q)) ||
                (!string.IsNullOrEmpty(d.MacAddress)   && d.MacAddress.ToLower().Contains(q))   ||
                d.BranchLocation.ToLower().Contains(q))
            .OrderByDescending(d => d.Status == "SupportRequested")
            .ThenByDescending(d => d.Status == "Online")
            .ThenBy(d => d.Hostname)
            .ToList();

        if (filtered.Count == 0)
        {
            _accDeviceCards.Children.Add(new Border
            {
                Background = BgCard, BorderBrush = BorderMuted, BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10), Padding = new Thickness(30),
                Child = new TextBlock
                {
                    Text = devices.Count == 0
                        ? "⚠️  No user PCs have registered yet.\n\n" +
                          "Install the SVLL User Edition on any user PC — it auto-registers on first launch:\n\n" +
                          "   irm https://raw.githubusercontent.com/kunal-dev-stack/svll-workstation/main/install.ps1 | iex\n\n" +
                          "The PC will appear here within seconds of installation."
                        : "🔍  No PCs match your search.",
                    FontSize = 12, Foreground = TextSubtle, TextWrapping = TextWrapping.Wrap,
                    HorizontalAlignment = HorizontalAlignment.Center, TextAlignment = TextAlignment.Center
                }
            });
            return;
        }

        foreach (var dev in filtered)
            _accDeviceCards.Children.Add(BuildAccDeviceCard(dev));
    }

    private UIElement BuildAccDeviceCard(RemoteDeviceItem dev)
    {
        bool isAlert  = dev.Status == "SupportRequested";
        bool isOnline = dev.Status == "Online" || isAlert;
        bool hasMesh  = dev.MeshAgentRunning || !string.IsNullOrEmpty(dev.MeshNodeId);
        bool hasAD    = !string.IsNullOrEmpty(dev.AnyDeskId) && dev.AnyDeskId != "Not Configured" && dev.AnyDeskId != "N/A";

        var card = new Border
        {
            Background = isAlert ? new SolidColorBrush(Color.FromRgb(255,250,250)) : BgCard,
            BorderBrush = isAlert ? SvllRed : (isOnline ? new SolidColorBrush(Color.FromRgb(187,247,208)) : BorderMuted),
            BorderThickness = new Thickness(isAlert ? 2 : 1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(16,12,16,12),
            Margin  = new Thickness(0,0,0,8)
        };
        if (isOnline)
            card.Effect = new DropShadowEffect { BlurRadius = 10, ShadowDepth = 2, Opacity = 0.07, Color = Colors.Black };

        var g = new Grid();
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(250) });
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        // ── Identity column ────────────────────────────────────────────────
        var idCol = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        var nameRow = new StackPanel { Orientation = Orientation.Horizontal };
        nameRow.Children.Add(new TextBlock { Text = isAlert ? "🚨 " : (isOnline ? "🟢 " : "⚪ "), FontSize = 13, VerticalAlignment = VerticalAlignment.Center });
        nameRow.Children.Add(new TextBlock { Text = dev.Hostname, FontSize = 14, FontWeight = FontWeights.ExtraBold, Foreground = TextDark, VerticalAlignment = VerticalAlignment.Center });
        if (dev.IsLocalHost) nameRow.Children.Add(AccBadge("THIS PC", Color.FromRgb(30,64,175), Color.FromRgb(239,246,255)));
        if (hasMesh)         nameRow.Children.Add(AccBadge("MESH", Color.FromRgb(22,101,52), Color.FromRgb(240,253,244)));
        idCol.Children.Add(nameRow);
        idCol.Children.Add(new TextBlock { Text = $"👤 {dev.LoggedInUser}   🌐 {dev.IpAddress}", FontSize = 10.5, Foreground = TextSubtle, Margin = new Thickness(0,3,0,2) });
        if (!string.IsNullOrEmpty(dev.BranchLocation))
            idCol.Children.Add(new TextBlock { Text = $"🏢 {dev.BranchLocation}", FontSize = 10, Foreground = TextSubtle });
        if (isAlert && !string.IsNullOrEmpty(dev.SupportRequestMessage))
            idCol.Children.Add(new TextBlock { Text = $"💬 \"{dev.SupportRequestMessage}\"", FontSize = 10.5, FontWeight = FontWeights.SemiBold, Foreground = SvllRed, Margin = new Thickness(0,4,0,0), TextWrapping = TextWrapping.Wrap });
        Grid.SetColumn(idCol, 0);
        g.Children.Add(idCol);

        // ── Telemetry column ───────────────────────────────────────────────
        var teleCol = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(16,0,16,0) };
        if (hasAD)
        {
            var adRow = new StackPanel { Orientation = Orientation.Horizontal };
            adRow.Children.Add(new TextBlock { Text = "AnyDesk: ", FontSize = 10.5, Foreground = TextSubtle });
            adRow.Children.Add(new TextBlock { Text = dev.AnyDeskId, FontSize = 11.5, FontWeight = FontWeights.ExtraBold, FontFamily = new FontFamily("Consolas"), Foreground = SvllRed });
            teleCol.Children.Add(adRow);
        }
        teleCol.Children.Add(new TextBlock { Text = $"🏷️ MAC: {(string.IsNullOrEmpty(dev.MacAddress)?"N/A":dev.MacAddress)}   🔢 S/N: {(string.IsNullOrEmpty(dev.SerialNumber)?"N/A":dev.SerialNumber)}", FontSize = 10, Foreground = TextSubtle, Margin = new Thickness(0,2,0,0) });
        teleCol.Children.Add(new TextBlock { Text = $"🪟 {dev.OsVersion} (Build {(string.IsNullOrEmpty(dev.OsBuild)?"N/A":dev.OsBuild)})   🕐 {dev.LastSeen:HH:mm:ss}", FontSize = 10, Foreground = TextSubtle, Margin = new Thickness(0,2,0,0) });
        if (dev.CpuLoadPercent > 0 || dev.RamUsagePercent > 0 || dev.DiskFreePercent > 0)
        {
            var vitals = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0,4,0,0) };
            vitals.Children.Add(AccVitalPill($"CPU {dev.CpuLoadPercent}%",      dev.CpuLoadPercent  > 80 ? Color.FromRgb(220,38,38) : Color.FromRgb(30,64,175)));
            vitals.Children.Add(AccVitalPill($"RAM {dev.RamUsagePercent}%",     dev.RamUsagePercent > 85 ? Color.FromRgb(220,38,38) : Color.FromRgb(107,114,128)));
            vitals.Children.Add(AccVitalPill($"Free {dev.DiskFreePercent}%",    dev.DiskFreePercent < 15 ? Color.FromRgb(220,38,38) : Color.FromRgb(22,101,52)));
            teleCol.Children.Add(vitals);
        }
        Grid.SetColumn(teleCol, 1);
        g.Children.Add(teleCol);

        // ── Action buttons column ──────────────────────────────────────────
        var actCol = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        var wrap = new WrapPanel { Orientation = Orientation.Horizontal, Width = 215 };
        string ip = dev.IpAddress;

        var btnMesh = new Button
        {
            Content = "🌐 Zero-Pass Remote", FontSize = 10, FontWeight = FontWeights.Bold,
            Background = hasMesh ? new SolidColorBrush(Color.FromRgb(22,101,52)) : new SolidColorBrush(Color.FromRgb(100,116,139)),
            Foreground = Brushes.White, BorderThickness = new Thickness(0),
            Padding = new Thickness(8,5,8,5), Margin = new Thickness(0,0,4,4), Cursor = Cursors.Hand,
            ToolTip = hasMesh ? "1-click remote — no password on user PC" : "Mesh Agent not detected on this PC"
        };
        btnMesh.Click += (s, e) =>
        {
            string u = AdminAuthService.Instance.GetMeshCentralUrl();
            if (!string.IsNullOrEmpty(dev.MeshNodeId)) MeshAgentService.LaunchRemoteControl(u, dev.MeshNodeId);
            else MeshAgentService.LaunchMeshDashboard(u);
        };
        wrap.Children.Add(btnMesh);

        wrap.Children.Add(AccCardBtn("🚀 AnyDesk", hasAD ? Color.FromRgb(211,47,47) : Color.FromRgb(100,116,139), () =>
        {
            if (!hasAD) { MessageBox.Show($"No AnyDesk ID for {dev.Hostname}.", "N/A", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            FleetRegistryService.LaunchAnyDesk(dev.AnyDeskId);
        }));
        wrap.Children.Add(AccCardBtn("💻 RDP",    Color.FromRgb(30,64,175),  () => FleetRegistryService.LaunchRdp(ip)));
        wrap.Children.Add(AccCardBtn("📁 Files",  Color.FromRgb(71,85,105),  () => FleetRegistryService.LaunchFileExplorer(ip)));
        wrap.Children.Add(AccCardBtn("📡 Shell",  Color.FromRgb(26,86,65),   () => FleetRegistryService.LaunchRemoteShell(ip)));
        wrap.Children.Add(AccCardBtn("⚙️ Manage", Color.FromRgb(51,65,85),   () => FleetRegistryService.LaunchComputerManagement(ip)));

        var btnPing = AccCardBtn("⚡ Ping", Color.FromRgb(71,85,105), null!);
        btnPing.Click += async (s, e) =>
        {
            btnPing.IsEnabled = false; btnPing.Content = "⏳";
            long ms = await FleetRegistryService.PingDeviceAsync(ip);
            btnPing.IsEnabled = true; btnPing.Content = "⚡ Ping";
            if (ms >= 0) MessageBox.Show($"{dev.Hostname}: {ms} ms 🟢", "Ping", MessageBoxButton.OK, MessageBoxImage.Information);
            else         MessageBox.Show($"{dev.Hostname}: No reply 🔴", "Timeout", MessageBoxButton.OK, MessageBoxImage.Warning);
        };
        wrap.Children.Add(btnPing);

        actCol.Children.Add(wrap);
        Grid.SetColumn(actCol, 2);
        g.Children.Add(actCol);

        card.Child = g;
        return card;
    }

    private Button AccCardBtn(string label, Color bg, Action? onClick)
    {
        var btn = new Button
        {
            Content = label, FontSize = 10,
            Background = new SolidColorBrush(bg), Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(8,5,8,5), Margin = new Thickness(0,0,4,4), Cursor = Cursors.Hand
        };
        if (onClick != null) btn.Click += (s, e) => onClick();
        return btn;
    }

    private Border AccVitalPill(string text, Color fg) => new Border
    {
        Background = new SolidColorBrush(Color.FromArgb(25,fg.R,fg.G,fg.B)),
        BorderBrush = new SolidColorBrush(Color.FromArgb(80,fg.R,fg.G,fg.B)),
        BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(4),
        Padding = new Thickness(6,2,6,2), Margin = new Thickness(0,0,4,0),
        Child = new TextBlock { Text = text, FontSize = 9.5, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(fg) }
    };

    // ─── Auto-refresh ─────────────────────────────────────────────────────────
    private void StartAccAutoRefresh()
    {
        if (_accAutoRefreshTimer?.IsEnabled == true) return;
        _accAutoRefreshTimer ??= new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        _accAutoRefreshTimer.Tick += async (s, e) =>
        {
            try
            {
                await CloudSyncService.Instance.PollFleetAsync();
                AccRenderDeviceCards();
                AccUpdateMetrics();
                RefreshAccRelayBanner();
            }
            catch { }
        };
        _accAutoRefreshTimer.Start();
    }

    private void StopAccAutoRefresh() => _accAutoRefreshTimer?.Stop();

    // ─── Helpers ──────────────────────────────────────────────────────────────
    private static Border AccBadge(string text, Color fg, Color bg) => new Border
    {
        Background = new SolidColorBrush(bg), CornerRadius = new CornerRadius(4),
        Padding = new Thickness(6,1,6,1), Margin = new Thickness(6,0,0,0),
        VerticalAlignment = VerticalAlignment.Center,
        Child = new TextBlock { Text = text, FontSize = 9, FontWeight = FontWeights.ExtraBold, Foreground = new SolidColorBrush(fg) }
    };

    private Button AccHeaderBtn(string label, Color? bg, Brush? fg, Action onClick, string? tip)
    {
        var btn = new Button
        {
            Content = label, FontSize = 11,
            Background = bg.HasValue ? new SolidColorBrush(bg.Value) : new SolidColorBrush(Color.FromRgb(241,245,249)),
            Foreground = fg ?? TextDark,
            BorderBrush = BorderMuted, BorderThickness = new Thickness(bg.HasValue ? 0 : 1),
            Padding = new Thickness(12,5,12,5), Margin = new Thickness(0,0,6,0),
            Cursor = Cursors.Hand, VerticalAlignment = VerticalAlignment.Center
        };
        if (!string.IsNullOrEmpty(tip)) btn.ToolTip = tip;
        btn.Click += async (s, e) => await Task.Run(() => Dispatcher.Invoke(onClick));
        return btn;
    }
}
#endif
