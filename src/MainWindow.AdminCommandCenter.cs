// =============================================================================
// SVLL IT Workstation v8.1 — Admin Command Center
// Unified Fleet Monitor + 1-Click Remote Control Deck
// Zero-touch remote management with auto-reflected credentials & direct connection
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
    private StackPanel  _accDeviceCards        = null!;
    private TextBlock   _accTotalLbl           = null!;
    private TextBlock   _accOnlineLbl          = null!;
    private TextBlock   _accOfflineLbl         = null!;
    private TextBlock   _accAlertLbl           = null!;
    private TextBox     _accSearchBox          = null!;
    private CheckBox?   _accUserOnlyChk;
    private Border      _accRelayBanner        = null!;
    private Border      _accSelectedTargetDeck = null!;
    private RemoteDeviceItem? _selectedAccDevice = null;

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
        root.Children.Add(BuildAccSelectedTargetDeck());
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
        titleRow.Children.Add(AccBadge("ADMIN CONTROL", Color.FromRgb(146,64,14), Color.FromRgb(254,243,199)));
        titleRow.Children.Add(AccBadge("ULTRA ENGINE", Color.FromRgb(5,150,105), Color.FromRgb(209,250,229)));
        left.Children.Add(titleRow);
        left.Children.Add(new TextBlock
        {
            Text = "Live fleet monitoring, auto-reflected credentials, and 1-click direct remote control.",
            FontSize = 11, Foreground = TextSubtle
        });
        Grid.SetColumn(left, 0);
        g.Children.Add(left);

        var right = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
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
            }, "Export all fleet workstation records to CSV"));
        right.Children.Add(AccHeaderBtn("⚙️ Relay Config", null, null, () => ShowServerSettingsDialog(), "Configure Relay Server URL"));
        right.Children.Add(AccHeaderBtn("🔄 Refresh Fleet", null, null, async () =>
        {
            await CloudSyncService.Instance.PollFleetAsync();
            AccRenderDeviceCards();
            AccUpdateMetrics();
            RefreshAccRelayBanner();
            AccRefreshTargetDeck();
        }, "Force immediate poll from relay"));
        right.Children.Add(AccHeaderBtn("🔒 Lock", null, null, () => AdminAuthService.Instance.LockAdmin(), "Lock admin mode"));
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
            _accRelayBanner.Background      = new SolidColorBrush(Color.FromRgb(240,253,244));
            _accRelayBanner.BorderBrush     = new SolidColorBrush(Color.FromRgb(187,247,208));
            _accRelayBanner.BorderThickness = new Thickness(1.5);
            _accRelayBanner.Padding         = new Thickness(14, 8, 14, 8);
            info.Children.Add(new TextBlock { Text = "🟢  Relay Server Connected — " + u, FontSize = 12, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(22,101,52)) });
            info.Children.Add(new TextBlock { Text = "Fleet syncing every 30 s. User PCs reflect automatically when they send their first heartbeat.", FontSize = 10.5, Foreground = TextSubtle });
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
                       "      (4) Click ⚙️ Relay Config to verify the URL matches the actual server address.",
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
            AccRefreshTargetDeck();
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
            ("🖥️", "TOTAL PCs",     _accTotalLbl,   "Registered fleet systems", Color.FromRgb(239,246,255), Color.FromRgb(191,219,254)),
            ("🟢", "ONLINE NOW",    _accOnlineLbl,  "Active heartbeats & remote ready", Color.FromRgb(240,253,244), Color.FromRgb(187,247,208)),
            ("🚨", "NEED HELP",     _accAlertLbl,   "User clicked Request Support", Color.FromRgb(254,242,242), Color.FromRgb(252,165,165)),
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

    // ─── Active Selected Target Deck ─────────────────────────────────────────
    private UIElement BuildAccSelectedTargetDeck()
    {
        _accSelectedTargetDeck = new Border
        {
            CornerRadius = new CornerRadius(14),
            Margin = new Thickness(0, 0, 0, 12)
        };
        AccRefreshTargetDeck();
        return _accSelectedTargetDeck;
    }

    private void AccRefreshTargetDeck()
    {
        if (_accSelectedTargetDeck == null) return;

        if (_selectedAccDevice == null)
        {
            // Empty state: guide admin on how to select
            _accSelectedTargetDeck.Background = new SolidColorBrush(Color.FromRgb(15, 23, 42)); // Slate-900
            _accSelectedTargetDeck.BorderBrush = new SolidColorBrush(Color.FromRgb(51, 65, 85)); // Slate-700
            _accSelectedTargetDeck.BorderThickness = new Thickness(1.5);
            _accSelectedTargetDeck.Padding = new Thickness(18, 14, 18, 14);
            _accSelectedTargetDeck.Effect = null;

            var guideGrid = new Grid();
            guideGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            guideGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var iconBox = new Border
            {
                Width = 44, Height = 44, CornerRadius = new CornerRadius(10),
                Background = new SolidColorBrush(Color.FromArgb(40, 16, 185, 129)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(16, 185, 129)),
                BorderThickness = new Thickness(1),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 14, 0),
                Child = new TextBlock
                {
                    Text = "🎯", FontSize = 20, HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };
            Grid.SetColumn(iconBox, 0);
            guideGrid.Children.Add(iconBox);

            var guideInfo = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            var titleRow = new StackPanel { Orientation = Orientation.Horizontal };
            titleRow.Children.Add(new TextBlock
            {
                Text = "Active Target Remote Deck",
                FontSize = 14, FontWeight = FontWeights.ExtraBold,
                Foreground = Brushes.White
            });
            titleRow.Children.Add(AccBadge("WAITING FOR SELECTION", Color.FromRgb(245, 158, 11), Color.FromArgb(40, 245, 158, 11)));
            guideInfo.Children.Add(titleRow);

            guideInfo.Children.Add(new TextBlock
            {
                Text = "☑️ Check the box on any user workstation below to automatically reflect its Ultra Remote ID, unattended password, and 1-click connect directly.",
                FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
                Margin = new Thickness(0, 4, 0, 0)
            });
            Grid.SetColumn(guideInfo, 1);
            guideGrid.Children.Add(guideInfo);

            _accSelectedTargetDeck.Child = guideGrid;
            return;
        }

        // Active state with selected workstation
        var dev = _selectedAccDevice;
        bool isAlert = dev.Status == "SupportRequested";
        bool isOnline = dev.Status == "Online" || isAlert;
        string passToUse = string.IsNullOrEmpty(dev.RemotePassword) ? BuildConstants.DefaultRemotePassword : dev.RemotePassword;
        bool hasUltra = !string.IsNullOrEmpty(dev.RemoteId) && dev.RemoteId != "N/A" && dev.RemoteId != "Not Configured";
        bool hasAD = !string.IsNullOrEmpty(dev.AnyDeskId) && dev.AnyDeskId != "Not Configured" && dev.AnyDeskId != "N/A";

        _accSelectedTargetDeck.Background = new SolidColorBrush(Color.FromRgb(15, 23, 42)); // Slate-900
        _accSelectedTargetDeck.BorderBrush = new SolidColorBrush(Color.FromRgb(16, 185, 129)); // Emerald-500
        _accSelectedTargetDeck.BorderThickness = new Thickness(2);
        _accSelectedTargetDeck.Padding = new Thickness(20, 16, 20, 16);
        _accSelectedTargetDeck.Effect = new DropShadowEffect
        {
            BlurRadius = 18, ShadowDepth = 2, Opacity = 0.35, Color = Color.FromRgb(16, 185, 129)
        };

        var deckPanel = new StackPanel();

        // 1. Target Header Row
        var topRow = new Grid { Margin = new Thickness(0, 0, 0, 12) };
        topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var hostInfo = new StackPanel();
        var hRow = new StackPanel { Orientation = Orientation.Horizontal };
        hRow.Children.Add(new TextBlock
        {
            Text = isAlert ? "🚨 " : (isOnline ? "🟢 " : "⚪ "),
            FontSize = 16, VerticalAlignment = VerticalAlignment.Center
        });
        hRow.Children.Add(new TextBlock
        {
            Text = dev.Hostname,
            FontSize = 18, FontWeight = FontWeights.ExtraBold,
            Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center
        });
        hRow.Children.Add(AccBadge(isOnline ? "ONLINE" : "OFFLINE", isOnline ? Color.FromRgb(16, 185, 129) : Color.FromRgb(148, 163, 184), Color.FromArgb(40, 16, 185, 129)));
        if (!string.IsNullOrEmpty(dev.BranchLocation))
            hRow.Children.Add(AccBadge($"🏢 {dev.BranchLocation}", Color.FromRgb(56, 189, 248), Color.FromArgb(40, 56, 189, 248)));
        if (dev.IsLocalHost)
            hRow.Children.Add(AccBadge("THIS ADMIN PC", Color.FromRgb(168, 85, 247), Color.FromArgb(40, 168, 85, 247)));

        hostInfo.Children.Add(hRow);

        var subRow = new TextBlock
        {
            Text = $"👤 Active User: {dev.LoggedInUser}    🌐 IP Address: {dev.IpAddress}    🪟 OS: {dev.OsVersion}    🕐 Last Seen: {dev.LastSeen:HH:mm:ss}",
            FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
            Margin = new Thickness(0, 4, 0, 0)
        };
        hostInfo.Children.Add(subRow);
        Grid.SetColumn(hostInfo, 0);
        topRow.Children.Add(hostInfo);

        // Top Right: Clear selection
        var btnClear = new Button
        {
            Content = "✖ Deselect",
            FontSize = 11, FontWeight = FontWeights.SemiBold,
            Background = new SolidColorBrush(Color.FromRgb(30, 41, 59)),
            Foreground = new SolidColorBrush(Color.FromRgb(203, 213, 225)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(71, 85, 105)),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(10, 4, 10, 4),
            Cursor = Cursors.Hand,
            VerticalAlignment = VerticalAlignment.Top
        };
        btnClear.Click += (s, e) =>
        {
            _selectedAccDevice = null;
            AccRenderDeviceCards();
            AccRefreshTargetDeck();
        };
        Grid.SetColumn(btnClear, 1);
        topRow.Children.Add(btnClear);
        deckPanel.Children.Add(topRow);

        // 2. Reflected Credentials Deck (3 columns)
        var credsGrid = new Grid { Margin = new Thickness(0, 0, 0, 14) };
        credsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.3, GridUnitType.Star) });
        credsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
        credsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });

        // Cred Box 1: Ultra Remote ID
        var idBox = BuildCredBox(
            "⚡ REFLECTED ULTRA REMOTE ID",
            hasUltra ? dev.RemoteId : (isOnline ? "Auto-reflecting..." : "Offline"),
            hasUltra ? Color.FromRgb(52, 211, 153) : Color.FromRgb(245, 158, 11),
            hasUltra ? "📋 Copy ID" : null,
            () =>
            {
                if (hasUltra)
                {
                    Clipboard.SetText(dev.RemoteId.Replace(" ", ""));
                    MessageBox.Show($"Copied Ultra Remote ID: {dev.RemoteId}", "Copied", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        );
        Grid.SetColumn(idBox, 0);
        credsGrid.Children.Add(idBox);

        // Cred Box 2: Reflected Password
        var passBox = BuildCredBox(
            "🔑 UNATTENDED PASSWORD",
            passToUse,
            Color.FromRgb(248, 250, 252),
            "📋 Copy Pass",
            () =>
            {
                Clipboard.SetText(passToUse);
                MessageBox.Show($"Copied Password: {passToUse}", "Copied", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        );
        idBox.Margin = new Thickness(0, 0, 8, 0);
        passBox.Margin = new Thickness(0, 0, 8, 0);
        Grid.SetColumn(passBox, 1);
        credsGrid.Children.Add(passBox);

        // Cred Box 3: Direct LAN Listener
        var lanBox = BuildCredBox(
            "🌐 DIRECT LAN LISTENER",
            $"{dev.IpAddress}:21118",
            Color.FromRgb(56, 189, 248),
            "📋 Copy IP",
            () =>
            {
                Clipboard.SetText(dev.IpAddress);
                MessageBox.Show($"Copied IP: {dev.IpAddress}", "Copied", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        );
        Grid.SetColumn(lanBox, 2);
        credsGrid.Children.Add(lanBox);

        deckPanel.Children.Add(credsGrid);

        // 3. Primary & Direct Action Buttons Row
        var actionRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };

        // Big Primary Button: 1-Click Connect
        var btnBigConnect = new Button
        {
            Content = hasUltra ? "⚡ 1-Click Connect Directly (Ultra Remote)" : "⚡ Connect via IP (Ultra Remote)",
            Height = 42, FontSize = 12.5, FontWeight = FontWeights.Bold,
            Background = new SolidColorBrush(Color.FromRgb(16, 185, 129)),
            Foreground = Brushes.White, BorderThickness = new Thickness(0),
            Padding = new Thickness(20, 8, 20, 8), Margin = new Thickness(0, 0, 10, 0),
            Cursor = Cursors.Hand
        };
        btnBigConnect.Click += (s, e) =>
        {
            UltraRemoteService.LaunchRemoteConnection(dev.RemoteId, passToUse, dev.IpAddress);
        };
        actionRow.Children.Add(btnBigConnect);

        // Big Secondary Button: Direct LAN Connect (Bypasses external servers)
        var btnDirectLan = new Button
        {
            Content = "🌐 Direct LAN Connect (Peer-to-Peer)",
            Height = 42, FontSize = 12, FontWeight = FontWeights.SemiBold,
            Background = new SolidColorBrush(Color.FromRgb(2, 132, 199)),
            Foreground = Brushes.White, BorderThickness = new Thickness(0),
            Padding = new Thickness(16, 8, 16, 8), Margin = new Thickness(0, 0, 10, 0),
            Cursor = Cursors.Hand,
            ToolTip = "Connect directly over office LAN / WAN without relying on any external relay server"
        };
        btnDirectLan.Click += (s, e) =>
        {
            UltraRemoteService.LaunchDirectIpConnection(dev.IpAddress, passToUse);
        };
        actionRow.Children.Add(btnDirectLan);

        if (hasAD)
        {
            var btnAdFallback = new Button
            {
                Content = $"🚀 AnyDesk ({dev.AnyDeskId})",
                Height = 42, FontSize = 11.5, FontWeight = FontWeights.SemiBold,
                Background = new SolidColorBrush(Color.FromRgb(220, 38, 38)),
                Foreground = Brushes.White, BorderThickness = new Thickness(0),
                Padding = new Thickness(14, 8, 14, 8), Margin = new Thickness(0, 0, 10, 0),
                Cursor = Cursors.Hand,
                ToolTip = "Launch AnyDesk Unattended Remote Control"
            };
            btnAdFallback.Click += (s, e) => FleetRegistryService.LaunchAnyDesk(dev.AnyDeskId, unattended: true);
            actionRow.Children.Add(btnAdFallback);
        }

        deckPanel.Children.Add(actionRow);

        // 4. Quick Tools Bar
        var toolsRow = new StackPanel { Orientation = Orientation.Horizontal };
        toolsRow.Children.Add(DkDeckToolBtn("💻 RDP Desktop", () => FleetRegistryService.LaunchRdp(dev.IpAddress, dev.AnyDeskId)));
        toolsRow.Children.Add(DkDeckToolBtn("📁 Browse C$ Files", () => FleetRegistryService.LaunchFileExplorer(dev.IpAddress)));
        toolsRow.Children.Add(DkDeckToolBtn("📡 Remote PS Shell", () => FleetRegistryService.LaunchRemoteShell(dev.IpAddress)));
        toolsRow.Children.Add(DkDeckToolBtn("⚙️ Computer Mgmt", () => FleetRegistryService.LaunchComputerManagement(dev.IpAddress)));

        var pingBtn = DkDeckToolBtn("⚡ Ping Check", null!);
        pingBtn.Click += async (s, e) =>
        {
            pingBtn.Content = "⏳ Checking…"; pingBtn.IsEnabled = false;
            long ms = await FleetRegistryService.PingDeviceAsync(dev.IpAddress);
            pingBtn.IsEnabled = true; pingBtn.Content = "⚡ Ping Check";
            if (ms >= 0)
                MessageBox.Show($"{dev.Hostname} ({dev.IpAddress}) is responding: {ms} ms 🟢", "Ping Reply", MessageBoxButton.OK, MessageBoxImage.Information);
            else
                MessageBox.Show($"No ICMP reply from {dev.Hostname} ({dev.IpAddress}) 🔴\n(Workstations across separate subnets or branches may filter ICMP, but Ultra Remote port 21118 will still connect!)", "Ping Timeout", MessageBoxButton.OK, MessageBoxImage.Warning);
        };
        toolsRow.Children.Add(pingBtn);

        deckPanel.Children.Add(toolsRow);

        _accSelectedTargetDeck.Child = deckPanel;
    }

    private Border BuildCredBox(string label, string val, Color valColor, string? btnLabel, Action? onClick)
    {
        var box = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(30, 41, 59)), // Slate-800
            BorderBrush = new SolidColorBrush(Color.FromRgb(51, 65, 85)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(14, 10, 14, 10)
        };

        var sp = new StackPanel();
        sp.Children.Add(new TextBlock
        {
            Text = label, FontSize = 9.5, FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184))
        });

        var valRow = new Grid { Margin = new Thickness(0, 4, 0, 0) };
        valRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        valRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var valTxt = new TextBlock
        {
            Text = val, FontSize = 16, FontWeight = FontWeights.ExtraBold,
            FontFamily = new FontFamily("Consolas, Courier New"),
            Foreground = new SolidColorBrush(valColor),
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(valTxt, 0);
        valRow.Children.Add(valTxt);

        if (!string.IsNullOrEmpty(btnLabel) && onClick != null)
        {
            var btn = new Button
            {
                Content = btnLabel, FontSize = 10, FontWeight = FontWeights.SemiBold,
                Background = new SolidColorBrush(Color.FromRgb(51, 65, 85)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(8, 3, 8, 3),
                Cursor = Cursors.Hand,
                VerticalAlignment = VerticalAlignment.Center
            };
            btn.Click += (s, e) => onClick();
            Grid.SetColumn(btn, 1);
            valRow.Children.Add(btn);
        }

        sp.Children.Add(valRow);
        box.Child = sp;
        return box;
    }

    private Button DkDeckToolBtn(string label, Action? onClick)
    {
        var btn = new Button
        {
            Content = label, FontSize = 10.5, FontWeight = FontWeights.SemiBold,
            Background = new SolidColorBrush(Color.FromRgb(30, 41, 59)),
            Foreground = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(51, 65, 85)),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(10, 5, 10, 5),
            Margin = new Thickness(0, 0, 6, 0),
            Cursor = Cursors.Hand
        };
        if (onClick != null) btn.Click += (s, e) => onClick();
        return btn;
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
            Text = "⚡  QUICK CONNECT  —  Type any Partner ID / IP Address / Computer Name / AnyDesk ID and click an action",
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

        row.Children.Add(DkBtn("⚡ Ultra Connect", Color.FromRgb(16,185,129), () => Guard(t => UltraRemoteService.LaunchRemoteConnection(t))));
        row.Children.Add(DkBtn("🚀 AnyDesk",       Color.FromRgb(211,47,47),   () => Guard(t => FleetRegistryService.LaunchAnyDesk(t))));
        row.Children.Add(DkBtn("💻 RDP",           Color.FromRgb(30,64,175),   () => Guard(t => FleetRegistryService.LaunchRdp(t))));
        row.Children.Add(DkBtn("📁 C$ Files",      Color.FromRgb(71,85,105),   () => Guard(t => FleetRegistryService.LaunchFileExplorer(t))));
        row.Children.Add(DkBtn("📡 PS Shell",      Color.FromRgb(26,86,65),    () => Guard(t => FleetRegistryService.LaunchRemoteShell(t))));
        row.Children.Add(DkBtn("⚙️ Manage",        Color.FromRgb(51,65,85),    () => Guard(t => FleetRegistryService.LaunchComputerManagement(t))));

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
            FontSize = 11.5, Height = 28, Width = 320, Padding = new Thickness(6,2,6,2),
            BorderBrush = BorderMuted, VerticalContentAlignment = VerticalAlignment.Center
        };
        _accSearchBox.TextChanged += (s, e) => AccRenderDeviceCards();
        searchRow.Children.Add(_accSearchBox);

        _accUserOnlyChk = new CheckBox
        {
            Content = "👤 User Editions Only",
            FontSize = 11, FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(12, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Cursor = Cursors.Hand
        };
        _accUserOnlyChk.Click += (s, e) => AccRenderDeviceCards();
        searchRow.Children.Add(_accUserOnlyChk);

        searchRow.Children.Add(new TextBlock
        {
            Text = "  Filter by hostname, IP, user, Partner ID, AnyDesk ID…",
            FontSize = 10, Foreground = TextSubtle, VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8, 0, 0, 0)
        });
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

        string q        = _accSearchBox?.Text.Trim().ToLowerInvariant() ?? "";
        bool userOnly   = _accUserOnlyChk?.IsChecked == true;
        var devices     = FleetRegistryService.Instance.GetAllDevices();

        var filtered = devices
            .Where(d => (!userOnly || !d.IsLocalHost) &&
                (string.IsNullOrEmpty(q) ||
                d.Hostname.ToLower().Contains(q) ||
                d.IpAddress.ToLower().Contains(q) ||
                d.LoggedInUser.ToLower().Contains(q) ||
                d.RemoteId.ToLower().Contains(q) ||
                d.AnyDeskId.ToLower().Contains(q) ||
                (!string.IsNullOrEmpty(d.SerialNumber) && d.SerialNumber.ToLower().Contains(q)) ||
                (!string.IsNullOrEmpty(d.MacAddress)   && d.MacAddress.ToLower().Contains(q))   ||
                d.BranchLocation.ToLower().Contains(q)))
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
        bool isAlert    = dev.Status == "SupportRequested";
        bool isOnline   = dev.Status == "Online" || isAlert;
        bool hasAD      = !string.IsNullOrEmpty(dev.AnyDeskId) && dev.AnyDeskId != "Not Configured" && dev.AnyDeskId != "N/A";
        bool hasUltra   = !string.IsNullOrEmpty(dev.RemoteId) && dev.RemoteId != "N/A" && dev.RemoteId != "Not Configured";
        bool isSelected = (_selectedAccDevice != null && _selectedAccDevice.Hostname.Equals(dev.Hostname, StringComparison.OrdinalIgnoreCase));
        string passToUse = string.IsNullOrEmpty(dev.RemotePassword) ? BuildConstants.DefaultRemotePassword : dev.RemotePassword;

        var card = new Border
        {
            Background = isAlert
                ? new SolidColorBrush(Color.FromRgb(255, 250, 250))
                : (isSelected ? new SolidColorBrush(Color.FromRgb(240, 253, 250)) : BgCard),
            BorderBrush = isAlert
                ? SvllRed
                : (isSelected
                    ? new SolidColorBrush(Color.FromRgb(16, 185, 129))
                    : (isOnline ? new SolidColorBrush(Color.FromRgb(187, 247, 208)) : BorderMuted)),
            BorderThickness = new Thickness(isSelected ? 2.5 : (isAlert ? 2 : 1)),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(14, 12, 14, 12),
            Margin  = new Thickness(0, 0, 0, 8),
            Cursor = Cursors.Hand
        };

        if (isSelected)
        {
            card.Effect = new DropShadowEffect { BlurRadius = 14, ShadowDepth = 2, Opacity = 0.25, Color = Color.FromRgb(16, 185, 129) };
        }
        else if (isOnline)
        {
            card.Effect = new DropShadowEffect { BlurRadius = 10, ShadowDepth = 2, Opacity = 0.07, Color = Colors.Black };
        }

        card.MouseDown += (s, e) =>
        {
            // If user clicked an interactive control directly, let it handle
            if (e.OriginalSource is Button || e.OriginalSource is CheckBox || e.OriginalSource is TextBox) return;
            _selectedAccDevice = isSelected ? null : dev;
            AccRenderDeviceCards();
            AccRefreshTargetDeck();
        };

        var g = new Grid();
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(34) }); // Selection Checkbox
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(240) }); // Host Identity
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Telemetry & Credentials
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Actions

        // ── Col 0: Selection Checkbox ──────────────────────────────────────
        var chkSelect = new CheckBox
        {
            IsChecked = isSelected,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Left,
            Cursor = Cursors.Hand,
            ToolTip = "Select this workstation to auto-reflect its ID & Password in the Control Deck above"
        };
        chkSelect.Click += (s, e) =>
        {
            _selectedAccDevice = (chkSelect.IsChecked == true) ? dev : null;
            AccRenderDeviceCards();
            AccRefreshTargetDeck();
        };
        Grid.SetColumn(chkSelect, 0);
        g.Children.Add(chkSelect);

        // ── Col 1: Identity column ─────────────────────────────────────────
        var idCol = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        var nameRow = new StackPanel { Orientation = Orientation.Horizontal };
        nameRow.Children.Add(new TextBlock { Text = isAlert ? "🚨 " : (isOnline ? "🟢 " : "⚪ "), FontSize = 13, VerticalAlignment = VerticalAlignment.Center });
        nameRow.Children.Add(new TextBlock { Text = dev.Hostname, FontSize = 14, FontWeight = FontWeights.ExtraBold, Foreground = TextDark, VerticalAlignment = VerticalAlignment.Center });
        if (dev.IsLocalHost) nameRow.Children.Add(AccBadge("THIS PC", Color.FromRgb(30,64,175), Color.FromRgb(239,246,255)));
        if (hasUltra)        nameRow.Children.Add(AccBadge("⚡ ULTRA", Color.FromRgb(5,150,105), Color.FromRgb(209,250,229)));
        idCol.Children.Add(nameRow);
        idCol.Children.Add(new TextBlock { Text = $"👤 {dev.LoggedInUser}   🌐 {dev.IpAddress}", FontSize = 10.5, Foreground = TextSubtle, Margin = new Thickness(0,3,0,2) });
        if (!string.IsNullOrEmpty(dev.BranchLocation))
            idCol.Children.Add(new TextBlock { Text = $"🏢 {dev.BranchLocation}", FontSize = 10, Foreground = TextSubtle });
        if (isAlert && !string.IsNullOrEmpty(dev.SupportRequestMessage))
            idCol.Children.Add(new TextBlock { Text = $"💬 \"{dev.SupportRequestMessage}\"", FontSize = 10.5, FontWeight = FontWeights.SemiBold, Foreground = SvllRed, Margin = new Thickness(0,4,0,0), TextWrapping = TextWrapping.Wrap });
        Grid.SetColumn(idCol, 1);
        g.Children.Add(idCol);

        // ── Col 2: Telemetry & Credentials column ─────────────────────────
        var teleCol = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(14,0,14,0) };

        // ⚡ Ultra Remote Display (UltraViewer style ID & Password)
        if (hasUltra)
        {
            var ultraRow = new StackPanel { Orientation = Orientation.Horizontal, Cursor = Cursors.Hand, ToolTip = "Click to copy Ultra Remote credentials" };
            ultraRow.MouseDown += (s, e) =>
            {
                try
                {
                    Clipboard.SetText(dev.RemoteId.Replace(" ", ""));
                    MessageBox.Show($"Ultra Remote ID copied: {dev.RemoteId}\nPassword: {passToUse}", "Ultra Remote Credentials", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch { }
            };
            ultraRow.Children.Add(new TextBlock { Text = "⚡ Ultra ID: ", FontSize = 11, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(5,150,105)) });
            ultraRow.Children.Add(new TextBlock { Text = dev.RemoteId, FontSize = 12.5, FontWeight = FontWeights.ExtraBold, FontFamily = new FontFamily("Consolas"), Foreground = TextDark });
            ultraRow.Children.Add(new TextBlock { Text = $"   🔑 {passToUse}", FontSize = 11, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(30, 41, 59)) });
            ultraRow.Children.Add(new TextBlock { Text = " 📋", FontSize = 10, VerticalAlignment = VerticalAlignment.Center });
            teleCol.Children.Add(ultraRow);
        }
        else if (isOnline)
        {
            var pendingRow = new StackPanel { Orientation = Orientation.Horizontal };
            pendingRow.Children.Add(new TextBlock { Text = "⚡ Ultra Remote: ", FontSize = 10.5, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(217,119,6)) });
            pendingRow.Children.Add(new TextBlock { Text = "Auto-reflecting via heartbeat...", FontSize = 10, FontStyle = FontStyles.Italic, Foreground = TextSubtle });
            teleCol.Children.Add(pendingRow);
        }

        if (hasAD)
        {
            var adRow = new StackPanel { Orientation = Orientation.Horizontal, Cursor = Cursors.Hand, ToolTip = "Click to copy AnyDesk ID" };
            adRow.MouseDown += (s, e) =>
            {
                try
                {
                    Clipboard.SetText(dev.AnyDeskId.Replace(" ", ""));
                    MessageBox.Show($"AnyDesk ID copied to clipboard: {dev.AnyDeskId}", "Copied", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch { }
            };
            adRow.Children.Add(new TextBlock { Text = "AnyDesk: ", FontSize = 10, Foreground = TextSubtle });
            adRow.Children.Add(new TextBlock { Text = dev.AnyDeskId, FontSize = 10.5, FontWeight = FontWeights.Bold, FontFamily = new FontFamily("Consolas"), Foreground = SvllRed });
            adRow.Children.Add(new TextBlock { Text = " 📋", FontSize = 9, VerticalAlignment = VerticalAlignment.Center });
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
        Grid.SetColumn(teleCol, 2);
        g.Children.Add(teleCol);

        // ── Col 3: Action buttons column ──────────────────────────────────
        var actCol = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        var wrap = new WrapPanel { Orientation = Orientation.Horizontal, Width = 230 };
        string ip = dev.IpAddress;

        // ⚡ Primary 1-Click Remote button: Ultra Remote (Zero-touch, zero acceptance)
        var btnPrimaryRemote = new Button
        {
            Content = hasUltra ? "⚡ 1-Click Connect" : (hasAD ? "🚀 AnyDesk" : "⚡ Direct Connect"),
            FontSize = 10, FontWeight = FontWeights.Bold,
            Background = hasUltra ? new SolidColorBrush(Color.FromRgb(16,185,129)) : (hasAD ? new SolidColorBrush(Color.FromRgb(211,47,47)) : new SolidColorBrush(Color.FromRgb(100,116,139))),
            Foreground = Brushes.White, BorderThickness = new Thickness(0),
            Padding = new Thickness(9,5,9,5), Margin = new Thickness(0,0,4,4), Cursor = Cursors.Hand,
            ToolTip = hasUltra
                ? $"Instant 1-Click Zero-Touch Remote Desktop (Ultra Engine)\nTarget ID: {dev.RemoteId}\nPassword: {passToUse}\n(No user acceptance or physical access needed on {dev.Hostname})"
                : (hasAD
                    ? $"Instant AnyDesk Remote Control\nTarget AnyDesk ID: {dev.AnyDeskId}"
                    : $"Direct LAN Remote Desktop to {dev.Hostname} ({dev.IpAddress})")
        };

        btnPrimaryRemote.Click += (s, e) =>
        {
            UltraRemoteService.LaunchRemoteConnection(dev.RemoteId, passToUse, dev.IpAddress);
        };

        var cm = new ContextMenu();
        if (hasUltra)
        {
            var miUltra = new MenuItem { Header = $"⚡ 1-Click Ultra Connect ({dev.RemoteId})" };
            miUltra.Click += (s, e) => UltraRemoteService.LaunchRemoteConnection(dev.RemoteId, passToUse, dev.IpAddress);
            cm.Items.Add(miUltra);

            var miLan = new MenuItem { Header = $"🌐 Direct LAN Connect ({dev.IpAddress})" };
            miLan.Click += (s, e) => UltraRemoteService.LaunchDirectIpConnection(dev.IpAddress, passToUse);
            cm.Items.Add(miLan);

            var miCopyId = new MenuItem { Header = $"📋 Copy Partner ID ({dev.RemoteId})" };
            miCopyId.Click += (s, e) => { try { Clipboard.SetText(dev.RemoteId.Replace(" ", "")); MessageBox.Show($"Copied ID: {dev.RemoteId}", "Copied", MessageBoxButton.OK, MessageBoxImage.Information); } catch { } };
            cm.Items.Add(miCopyId);

            var miCopyPass = new MenuItem { Header = $"🔑 Copy Password ({passToUse})" };
            miCopyPass.Click += (s, e) => { try { Clipboard.SetText(passToUse); MessageBox.Show($"Copied Password: {passToUse}", "Copied", MessageBoxButton.OK, MessageBoxImage.Information); } catch { } };
            cm.Items.Add(miCopyPass);
        }

        if (hasAD)
        {
            if (hasUltra) cm.Items.Add(new Separator());
            var miAd = new MenuItem { Header = $"🚀 Connect via AnyDesk Fallback ({dev.AnyDeskId})" };
            miAd.Click += (s, e) => FleetRegistryService.LaunchAnyDesk(dev.AnyDeskId, unattended: true);
            cm.Items.Add(miAd);
        }

        if (cm.Items.Count > 0)
        {
            btnPrimaryRemote.ContextMenu = cm;
        }
        wrap.Children.Add(btnPrimaryRemote);

        if (hasUltra && hasAD)
        {
            wrap.Children.Add(AccCardBtn("🚀 AnyDesk", Color.FromRgb(211,47,47), () => FleetRegistryService.LaunchAnyDesk(dev.AnyDeskId, unattended: true)));
        }

        wrap.Children.Add(AccCardBtn("💻 RDP",    Color.FromRgb(30,64,175),  () => FleetRegistryService.LaunchRdp(ip, dev.AnyDeskId)));
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
            else         MessageBox.Show($"{dev.Hostname}: No reply 🔴\n\nNote: If this workstation is in another office, ICMP/Ping is blocked by standard office routers.", "Timeout", MessageBoxButton.OK, MessageBoxImage.Warning);
        };
        wrap.Children.Add(btnPing);

        actCol.Children.Add(wrap);
        Grid.SetColumn(actCol, 3);
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
                AccRefreshTargetDeck();
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
