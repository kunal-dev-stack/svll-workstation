using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Media;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.Json;
using System.Threading;
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
    // Watchdog State
    private ListView _listWatchdog = null!;
    private DispatcherTimer? _watchdogTimer;
    private bool _isWatchdogRunning = false;
    private TextBlock _lblWatchdogStatus = null!;

    // Speed Test State
    private TextBlock _lblSpeedMbps = null!;
    private TextBlock _lblSpeedPing = null!;
    private ProgressBar _progSpeed = null!;
    private Canvas _speedCanvas = null!;
    private Polyline _speedLine = null!;
    private Polygon _speedFill = null!;
    private readonly List<double> _speedHistory = new List<double>();

    // 24/7 Watchdog Extended State
    private TextBlock _lblWatchdogTotalHosts = null!;
    private TextBlock _lblWatchdogOnlineHosts = null!;
    private TextBlock _lblWatchdogDrops = null!;
    private TextBlock _lblWatchdogChimeState = null!;
    private bool _watchdogAudioChimeEnabled = true;
    private bool _hasActiveAudioAlert = false;

    #region 1. 24/7 Continuous Ping Watchdog & Multi-Audit

    private void InitializeDefaultWatchdogTargets()
    {
        _watchdogTargets = new List<ContinuousPingTarget>
        {
            new ContinuousPingTarget { Host = "8.8.8.8", Description = "Google Primary DNS (Global WAN)" },
            new ContinuousPingTarget { Host = "1.1.1.1", Description = "Cloudflare Global Resolver" },
            new ContinuousPingTarget { Host = "208.67.222.222", Description = "OpenDNS Enterprise Resolver" }
        };

        // Dynamically detect and prepend the active branch default gateway
        string localGw = DetectLocalGatewayIp();
        if (!string.IsNullOrEmpty(localGw))
        {
            _watchdogTargets.Insert(0, new ContinuousPingTarget { Host = localGw, Description = "Active Branch Default Gateway (LAN)" });
        }
        else
        {
            _watchdogTargets.Insert(0, new ContinuousPingTarget { Host = "192.168.1.1", Description = "Default Gateway (LAN Standard)" });
        }
    }

    private string DetectLocalGatewayIp()
    {
        try
        {
            var active = NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up
                            && n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .OrderByDescending(n => n.GetIPProperties().GatewayAddresses.Count > 0)
                .FirstOrDefault();

            if (active != null)
            {
                var gw = active.GetIPProperties().GatewayAddresses
                    .FirstOrDefault(g => g.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork
                                         && !g.Address.ToString().StartsWith("0.")
                                         && !g.Address.ToString().StartsWith("127."));
                if (gw != null) return gw.Address.ToString();
            }
        }
        catch { }
        return "";
    }

    private UIElement BuildPingMonitorView()
    {
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var root = new StackPanel { Margin = new Thickness(0, 0, 10, 0) };

        var card = new Border
        {
            Background = BgCard,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(20),
            Margin = new Thickness(0, 0, 0, 14)
        };

        var sp = new StackPanel();

        // 1. Header with Badge & Status
        var headerDock = new DockPanel { Margin = new Thickness(0, 0, 0, 16) };
        var titleStack = new StackPanel();
        titleStack.Children.Add(new TextBlock
        {
            Text = "24/7 CONTINUOUS PING WATCHDOG & WAN FLEET MONITOR",
            FontSize = 13.5,
            FontWeight = FontWeights.Bold,
            Foreground = SvllBlue
        });
        titleStack.Children.Add(new TextBlock
        {
            Text = "Uninterrupted gateway reachability diagnostics with acoustic packet drop chimes and live target auditing.",
            FontSize = 11,
            Foreground = TextSubtle,
            Margin = new Thickness(0, 2, 0, 0)
        });
        DockPanel.SetDock(titleStack, Dock.Left);
        headerDock.Children.Add(titleStack);

        var statusBorder = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(14, 6, 14, 6),
            HorizontalAlignment = HorizontalAlignment.Right
        };
        _lblWatchdogStatus = new TextBlock
        {
            Text = "STATUS: STANDBY",
            FontSize = 11.5,
            FontWeight = FontWeights.Bold,
            Foreground = TextSubtle
        };
        statusBorder.Child = _lblWatchdogStatus;
        headerDock.Children.Add(statusBorder);
        sp.Children.Add(headerDock);

        // 2. Telemetry Scorecards (4 Cards)
        var summaryGrid = new Grid { Margin = new Thickness(0, 0, 0, 16) };
        for (int i = 0; i < 4; i++) summaryGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var cTotal = CreateWatchdogMetricCard("MONITORED TARGETS", SvllBlue, out _lblWatchdogTotalHosts, $"{_watchdogTargets.Count} Hosts", "Active in probe queue");
        Grid.SetColumn(cTotal, 0);
        summaryGrid.Children.Add(cTotal);

        var cOnline = CreateWatchdogMetricCard("HEALTHY REACHABLE", new SolidColorBrush(Color.FromRgb(22, 101, 52)), out _lblWatchdogOnlineHosts, "-- / --", "Zero packet loss");
        Grid.SetColumn(cOnline, 1);
        summaryGrid.Children.Add(cOnline);

        var cDrops = CreateWatchdogMetricCard("PACKET DROPS / FAILS", SvllRed, out _lblWatchdogDrops, "0 Drops", "Consecutive drop alerts");
        Grid.SetColumn(cDrops, 2);
        summaryGrid.Children.Add(cDrops);

        var cAudio = CreateWatchdogMetricCard("AUDIO CHIME ALERT", new SolidColorBrush(Color.FromRgb(30, 41, 59)), out _lblWatchdogChimeState, "Enabled 🔔", "Acoustic ping warning");
        Grid.SetColumn(cAudio, 3);
        summaryGrid.Children.Add(cAudio);

        sp.Children.Add(summaryGrid);

        // 3. Target Entry Toolbar
        var addBox = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(12, 10, 12, 10),
            Margin = new Thickness(0, 0, 0, 14)
        };
        var addGrid = new Grid();
        addGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(180) }); // Host
        addGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Description
        addGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Add Button

        var txtNewHost = new TextBox { Height = 28, Margin = new Thickness(0, 0, 8, 0), VerticalContentAlignment = VerticalAlignment.Center, Text = "192.168.1.1" };
        var txtNewDesc = new TextBox { Height = 28, Margin = new Thickness(0, 0, 8, 0), VerticalContentAlignment = VerticalAlignment.Center, Text = "Local Network Target" };
        var btnAddTarget = new Button
        {
            Content = " + Add Target Host ",
            Height = 28,
            Padding = new Thickness(12, 0, 12, 0),
            Background = SvllBlue,
            Foreground = Brushes.White,
            FontWeight = FontWeights.Bold,
            Cursor = Cursors.Hand
        };
        btnAddTarget.Click += (s, e) =>
        {
            string host = txtNewHost.Text.Trim();
            string desc = txtNewDesc.Text.Trim();
            if (string.IsNullOrEmpty(host))
            {
                MessageBox.Show("Please enter a valid IP address or domain hostname.", "Invalid Host", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            _watchdogTargets.Add(new ContinuousPingTarget { Host = host, Description = string.IsNullOrEmpty(desc) ? "Custom Target" : desc });
            _listWatchdog.Items.Refresh();
            _lblWatchdogTotalHosts.Text = $"{_watchdogTargets.Count} Hosts";
            Log($"[WATCHDOG] Added monitoring target: {host} ({desc})");
        };

        Grid.SetColumn(txtNewHost, 0);
        Grid.SetColumn(txtNewDesc, 1);
        Grid.SetColumn(btnAddTarget, 2);
        addGrid.Children.Add(txtNewHost);
        addGrid.Children.Add(txtNewDesc);
        addGrid.Children.Add(btnAddTarget);
        addBox.Child = addGrid;
        sp.Children.Add(addBox);

        // 4. Action Controls Toolbar
        var actionToolbar = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 12) };

        var btnStartStop = new Button
        {
            Content = " ▶ Start 24/7 Watchdog ",
            Height = 34,
            Background = SvllBlue,
            Foreground = Brushes.White,
            FontWeight = FontWeights.Bold,
            Padding = new Thickness(16, 0, 16, 0),
            Margin = new Thickness(0, 0, 8, 0),
            Cursor = Cursors.Hand
        };
        btnStartStop.Click += (s, e) =>
        {
            if (_isWatchdogRunning)
            {
                StopWatchdogMonitor();
                btnStartStop.Content = " ▶ Start 24/7 Watchdog ";
                btnStartStop.Background = SvllBlue;
            }
            else
            {
                StartWatchdogMonitor();
                btnStartStop.Content = " ⏹ Stop Watchdog ";
                btnStartStop.Background = SvllRed;
            }
        };
        actionToolbar.Children.Add(btnStartStop);

        var btnUploadTargets = new Button
        {
            Content = "Upload Target List (.CSV / .TXT)",
            Height = 34,
            Padding = new Thickness(12, 0, 12, 0),
            Margin = new Thickness(0, 0, 8, 0),
            Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
            BorderBrush = BorderMuted,
            Cursor = Cursors.Hand
        };
        btnUploadTargets.Click += (s, e) =>
        {
            UploadWatchdogTargetsList();
            _lblWatchdogTotalHosts.Text = $"{_watchdogTargets.Count} Hosts";
        };
        actionToolbar.Children.Add(btnUploadTargets);

        var btnExportTargets = new Button
        {
            Content = "Export Targets List",
            Height = 34,
            Padding = new Thickness(12, 0, 12, 0),
            Margin = new Thickness(0, 0, 8, 0),
            Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
            BorderBrush = BorderMuted,
            Cursor = Cursors.Hand
        };
        btnExportTargets.Click += (s, e) => ExportWatchdogTargetsList();
        actionToolbar.Children.Add(btnExportTargets);

        var btnRemove = new Button
        {
            Content = "Remove Selected Target",
            Height = 34,
            Padding = new Thickness(12, 0, 12, 0),
            Margin = new Thickness(0, 0, 8, 0),
            Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
            BorderBrush = BorderMuted,
            Cursor = Cursors.Hand
        };
        btnRemove.Click += (s, e) =>
        {
            if (_listWatchdog.SelectedItem is ContinuousPingTarget target)
            {
                _watchdogTargets.Remove(target);
                _listWatchdog.Items.Refresh();
                _lblWatchdogTotalHosts.Text = $"{_watchdogTargets.Count} Hosts";
                Log($"[WATCHDOG] Removed target: {target.Host}");
            }
        };
        actionToolbar.Children.Add(btnRemove);

        var btnResetTargets = new Button
        {
            Content = "Reset to Initial Targets",
            Height = 34,
            Padding = new Thickness(12, 0, 12, 0),
            Margin = new Thickness(0, 0, 8, 0),
            Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
            BorderBrush = BorderMuted,
            Cursor = Cursors.Hand
        };
        btnResetTargets.Click += (s, e) =>
        {
            InitializeDefaultWatchdogTargets();
            _listWatchdog.ItemsSource = null;
            _listWatchdog.ItemsSource = _watchdogTargets;
            _listWatchdog.Items.Refresh();
            _lblWatchdogTotalHosts.Text = $"{_watchdogTargets.Count} Hosts";
            _lblWatchdogOnlineHosts.Text = "-- / --";
            _lblWatchdogDrops.Text = "0 Drops";
            Log("[WATCHDOG] Target list reset to pristine initial state (removed all custom session targets).");
        };
        actionToolbar.Children.Add(btnResetTargets);

        var btnToggleChime = new Button
        {
            Content = _watchdogAudioChimeEnabled ? " 🔔 Chime: Active " : " 🔕 Chime: MUTED ",
            Height = 34,
            Padding = new Thickness(14, 0, 14, 0),
            Background = _watchdogAudioChimeEnabled ? new SolidColorBrush(Color.FromRgb(240, 253, 244)) : new SolidColorBrush(Color.FromRgb(254, 242, 242)),
            Foreground = _watchdogAudioChimeEnabled ? new SolidColorBrush(Color.FromRgb(22, 101, 52)) : SvllRed,
            BorderBrush = _watchdogAudioChimeEnabled ? new SolidColorBrush(Color.FromRgb(187, 247, 208)) : new SolidColorBrush(Color.FromRgb(254, 202, 202)),
            FontWeight = FontWeights.Bold,
            Cursor = Cursors.Hand
        };
        btnToggleChime.Click += (s, e) =>
        {
            _watchdogAudioChimeEnabled = !_watchdogAudioChimeEnabled;
            btnToggleChime.Content = _watchdogAudioChimeEnabled ? " 🔔 Chime: Active " : " 🔕 Chime: MUTED ";
            btnToggleChime.Background = _watchdogAudioChimeEnabled ? new SolidColorBrush(Color.FromRgb(240, 253, 244)) : new SolidColorBrush(Color.FromRgb(254, 242, 242));
            btnToggleChime.Foreground = _watchdogAudioChimeEnabled ? new SolidColorBrush(Color.FromRgb(22, 101, 52)) : SvllRed;
            btnToggleChime.BorderBrush = _watchdogAudioChimeEnabled ? new SolidColorBrush(Color.FromRgb(187, 247, 208)) : new SolidColorBrush(Color.FromRgb(254, 202, 202));
            _lblWatchdogChimeState.Text = _watchdogAudioChimeEnabled ? "Enabled 🔔" : "Muted 🔕";
            _lblWatchdogChimeState.Foreground = _watchdogAudioChimeEnabled ? new SolidColorBrush(Color.FromRgb(22, 101, 52)) : SvllRed;
            Log($"[WATCHDOG] Audio chime failure alerts {(_watchdogAudioChimeEnabled ? "enabled" : "muted")}.");
        };
        actionToolbar.Children.Add(btnToggleChime);

        sp.Children.Add(actionToolbar);

        // 5. Targets ListView
        _listWatchdog = new ListView
        {
            ItemsSource = _watchdogTargets,
            Height = 320,
            FontSize = 11.5,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1)
        };
        var gv = new GridView();
        gv.Columns.Add(new GridViewColumn { Header = "Target IP / Host", Width = 180, DisplayMemberBinding = new Binding("Host") });
        gv.Columns.Add(new GridViewColumn { Header = "Description", Width = 260, DisplayMemberBinding = new Binding("Description") });
        gv.Columns.Add(new GridViewColumn { Header = "Current Latency", Width = 140, DisplayMemberBinding = new Binding("CurrentLatency") });
        gv.Columns.Add(new GridViewColumn { Header = "Watchdog Status", Width = 140, DisplayMemberBinding = new Binding("Status") });
        gv.Columns.Add(new GridViewColumn { Header = "Drops", Width = 90, DisplayMemberBinding = new Binding("ConsecutiveDrops") });
        gv.Columns.Add(new GridViewColumn { Header = "Last Sampled", Width = 160, DisplayMemberBinding = new Binding("LastAuditTime") });
        _listWatchdog.View = gv;
        sp.Children.Add(_listWatchdog);

        card.Child = sp;
        root.Children.Add(card);

        scroll.Content = root;
        return scroll;
    }

    private Border CreateWatchdogMetricCard(string title, Brush accentBrush, out TextBlock valBlock, string defVal, string caption)
    {
        var border = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(14, 10, 14, 10),
            Margin = new Thickness(3, 0, 3, 0)
        };

        var sp = new StackPanel();
        sp.Children.Add(new TextBlock { Text = title, FontSize = 9.0, FontWeight = FontWeights.Bold, Foreground = TextSubtle, Margin = new Thickness(0, 0, 0, 4) });

        valBlock = new TextBlock { Text = defVal, FontSize = 18, FontWeight = FontWeights.ExtraBold, Foreground = accentBrush };
        sp.Children.Add(valBlock);

        sp.Children.Add(new TextBlock { Text = caption, FontSize = 8.5, Foreground = TextSubtle, Margin = new Thickness(0, 3, 0, 0) });
        border.Child = sp;
        return border;
    }

    private void StartWatchdogMonitor()
    {
        _isWatchdogRunning = true;
        _lblWatchdogStatus.Text = "Status: ACTIVE 24/7 MONITORING";
        _lblWatchdogStatus.Foreground = new SolidColorBrush(Color.FromRgb(22, 101, 52));

        _watchdogTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        _watchdogTimer.Tick += async (s, e) => await AuditWatchdogTargetsAsync();
        _watchdogTimer.Start();
        Log("[WATCHDOG] 24/7 background continuous ping watchdog initiated.");
    }

    private void StopWatchdogMonitor()
    {
        _isWatchdogRunning = false;
        _watchdogTimer?.Stop();
        _lblWatchdogStatus.Text = "Status: STOPPED";
        _lblWatchdogStatus.Foreground = TextSubtle;
        Log("[WATCHDOG] Continuous ping watchdog paused.");
    }

    private async Task AuditWatchdogTargetsAsync()
    {
        bool hasConfirmedOutage = false;

        foreach (var target in _watchdogTargets)
        {
            if (string.IsNullOrWhiteSpace(target.Host)) continue;

            await Task.Run(async () =>
            {
                try
                {
                    using var p = new Ping();
                    var reply = await p.SendPingAsync(target.Host, 1000);

                    if (reply.Status == IPStatus.Success)
                    {
                        target.CurrentLatency = reply.RoundtripTime;
                        target.Status = "ONLINE";
                        target.ConsecutiveDrops = 0;
                    }
                    else
                    {
                        target.CurrentLatency = -1;
                        target.Status = "TIMEOUT / DROP";
                        target.ConsecutiveDrops++;
                    }
                    target.LastAuditTime = DateTime.Now.ToString("HH:mm:ss");
                }
                catch
                {
                    target.CurrentLatency = -1;
                    target.Status = "HOST UNREACHABLE";
                    target.ConsecutiveDrops++;
                }
            });

            // Require at least 2 consecutive drops before flagging confirmed outage (prevents single-packet drop false alarms)
            if (target.ConsecutiveDrops >= 2)
            {
                hasConfirmedOutage = true;
            }
        }

        _listWatchdog.Items.Refresh();

        // Update Live Telemetry Scorecards
        int onlineCount = _watchdogTargets.Count(t => t.Status == "ONLINE");
        int totalDrops = _watchdogTargets.Sum(t => t.ConsecutiveDrops);

        if (_lblWatchdogOnlineHosts != null)
            _lblWatchdogOnlineHosts.Text = $"{onlineCount} / {_watchdogTargets.Count}";
        if (_lblWatchdogDrops != null)
            _lblWatchdogDrops.Text = $"{totalDrops} Drops";
        if (_lblWatchdogTotalHosts != null)
            _lblWatchdogTotalHosts.Text = $"{_watchdogTargets.Count} Hosts";

        // Acoustic Failure Alert Chime (Only sounds if chime is enabled AND only on new outage transition, NOT repeating every 3 seconds)
        if (_watchdogAudioChimeEnabled && hasConfirmedOutage && !_hasActiveAudioAlert)
        {
            try
            {
                SystemSounds.Exclamation.Play();
            }
            catch { }
            _hasActiveAudioAlert = true;
            Log("[WATCHDOG] Acoustic alert chime triggered for sustained network outage.");
        }
        else if (!hasConfirmedOutage)
        {
            _hasActiveAudioAlert = false;
        }
    }

    private void UploadWatchdogTargetsList()
    {
        var ofd = new OpenFileDialog
        {
            Filter = "Supported Lists (*.csv;*.txt;*.json)|*.csv;*.txt;*.json|All Files (*.*)|*.*"
        };
        if (ofd.ShowDialog() == true)
        {
            try
            {
                string ext = System.IO.Path.GetExtension(ofd.FileName).ToLowerInvariant();
                if (ext == ".json")
                {
                    var items = JsonSerializer.Deserialize<List<ContinuousPingTarget>>(File.ReadAllText(ofd.FileName));
                    if (items != null)
                    {
                        _watchdogTargets.Clear();
                        _watchdogTargets.AddRange(items);
                    }
                }
                else
                {
                    var lines = File.ReadAllLines(ofd.FileName);
                    _watchdogTargets.Clear();
                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        string[] parts = line.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                        string host = parts[0].Trim();
                        string desc = parts.Length > 1 ? parts[1].Trim() : "Imported Target";
                        _watchdogTargets.Add(new ContinuousPingTarget { Host = host, Description = desc });
                    }
                }

                _listWatchdog.ItemsSource = null;
                _listWatchdog.ItemsSource = _watchdogTargets;
                Log($"[WATCHDOG] Uploaded {_watchdogTargets.Count} targets from {ofd.FileName}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to parse target list: {ex.Message}", "Upload Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void ExportWatchdogTargetsList()
    {
        var sfd = new SaveFileDialog
        {
            Filter = "CSV Files (*.csv)|*.csv|JSON Files (*.json)|*.json|Text Files (*.txt)|*.txt",
            FileName = "SVLL_Monitored_Hosts.csv"
        };
        if (sfd.ShowDialog() == true)
        {
            if (sfd.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                File.WriteAllText(sfd.FileName, JsonSerializer.Serialize(_watchdogTargets, new JsonSerializerOptions { WriteIndented = true }));
            }
            else
            {
                var sb = new StringBuilder();
                sb.AppendLine("Host,Description,Latency,Status,Drops");
                foreach (var t in _watchdogTargets)
                {
                    sb.AppendLine($"{t.Host},{t.Description},{t.CurrentLatency},{t.Status},{t.ConsecutiveDrops}");
                }
                File.WriteAllText(sfd.FileName, sb.ToString());
            }
            Log($"[WATCHDOG] Exported targets list to {sfd.FileName}");
        }
    }

    #endregion

    #region 2. Live Internet Speed Test

    // Speed Test Extended State
    private TextBlock _lblSpeedJitter = null!;
    private TextBlock _lblSpeedPeak = null!;
    private TextBlock _lblSpeedQuality = null!;
    private TextBlock _lblSpeedProgressText = null!;
    private ComboBox _cmbPayloadSize = null!;

    private UIElement BuildSpeedTestView()
    {
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var root = new StackPanel { Margin = new Thickness(0, 0, 10, 0) };

        var card = new Border
        {
            Background = BgCard,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(22),
            Margin = new Thickness(0, 0, 0, 14)
        };

        var sp = new StackPanel();

        // Header with Badge
        var headerRow = new DockPanel { Margin = new Thickness(0, 0, 0, 14) };
        var titleStack = new StackPanel();
        titleStack.Children.Add(new TextBlock
        {
            Text = "ENTERPRISE WAN BANDWIDTH & THROUGHPUT BENCHMARK",
            FontSize = 13.5,
            FontWeight = FontWeights.Bold,
            Foreground = SvllBlue
        });
        titleStack.Children.Add(new TextBlock
        {
            Text = "Diagnoses gateway throughput, latency jitter, and connection quality against high-speed CDN backbones.",
            FontSize = 11,
            Foreground = TextSubtle,
            Margin = new Thickness(0, 2, 0, 0)
        });
        DockPanel.SetDock(titleStack, Dock.Left);
        headerRow.Children.Add(titleStack);

        var qualityBadge = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(12, 5, 12, 5),
            HorizontalAlignment = HorizontalAlignment.Right
        };
        _lblSpeedQuality = new TextBlock
        {
            Text = "STATUS: READY",
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            Foreground = TextDark
        };
        qualityBadge.Child = _lblSpeedQuality;
        headerRow.Children.Add(qualityBadge);
        sp.Children.Add(headerRow);

        // Metric Scorecards Grid (4 Cards)
        var cardsGrid = new Grid { Margin = new Thickness(0, 0, 0, 18) };
        for (int i = 0; i < 4; i++) cardsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        // Card 1: Download Speed
        var c1 = CreateSpeedMetricCard("DOWNLOAD SPEED", SvllBlue, out _lblSpeedMbps, "-- Mbps", "Real-Time Ingress Throughput");
        Grid.SetColumn(c1, 0);
        cardsGrid.Children.Add(c1);

        // Card 2: Peak Speed
        var c2 = CreateSpeedMetricCard("PEAK BURST RATE", new SolidColorBrush(Color.FromRgb(30, 41, 59)), out _lblSpeedPeak, "-- Mbps", "Highest Recorded Sample");
        Grid.SetColumn(c2, 1);
        cardsGrid.Children.Add(c2);

        // Card 3: Ping Latency
        var c3 = CreateSpeedMetricCard("ROUNDTRIP PING", new SolidColorBrush(Color.FromRgb(2, 132, 199)), out _lblSpeedPing, "-- ms", "ICMP CDN Gateway Latency");
        Grid.SetColumn(c3, 2);
        cardsGrid.Children.Add(c3);

        // Card 4: Jitter / Deviation
        var c4 = CreateSpeedMetricCard("LATENCY JITTER", new SolidColorBrush(Color.FromRgb(100, 116, 139)), out _lblSpeedJitter, "-- ms", "Packet Timing Variation");
        Grid.SetColumn(c4, 3);
        cardsGrid.Children.Add(c4);

        sp.Children.Add(cardsGrid);

        // Progress row
        var progRow = new DockPanel { Margin = new Thickness(0, 0, 0, 6) };
        var lblProgTitle = new TextBlock { Text = "BENCHMARK PROGRESS", FontSize = 9.5, FontWeight = FontWeights.Bold, Foreground = TextSubtle };
        DockPanel.SetDock(lblProgTitle, Dock.Left);
        progRow.Children.Add(lblProgTitle);

        _lblSpeedProgressText = new TextBlock { Text = "0%", FontSize = 10, FontWeight = FontWeights.SemiBold, Foreground = TextDark, HorizontalAlignment = HorizontalAlignment.Right };
        progRow.Children.Add(_lblSpeedProgressText);
        sp.Children.Add(progRow);

        _progSpeed = new ProgressBar
        {
            Height = 8,
            Minimum = 0,
            Maximum = 100,
            Value = 0,
            Foreground = SvllBlue,
            Background = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
            BorderThickness = new Thickness(0),
            Margin = new Thickness(0, 0, 0, 16)
        };
        sp.Children.Add(_progSpeed);

        // Sparkline Graph Container
        var chartBox = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(8),
            Margin = new Thickness(0, 0, 0, 18)
        };
        var chartStack = new StackPanel();
        var chartHeader = new TextBlock
        {
            Text = "LIVE THROUGHPUT WAVEFORM (Mbps)",
            FontSize = 9.5,
            FontWeight = FontWeights.Bold,
            Foreground = TextSubtle,
            Margin = new Thickness(6, 4, 0, 6)
        };
        chartStack.Children.Add(chartHeader);

        _speedCanvas = new Canvas { Height = 100, ClipToBounds = true };
        _speedFill = new Polygon { Fill = new LinearGradientBrush(Color.FromArgb(90, 26, 75, 178), Color.FromArgb(10, 26, 75, 178), 90) };
        _speedLine = new Polyline { Stroke = SvllBlue, StrokeThickness = 2.5 };
        _speedCanvas.Children.Add(_speedFill);
        _speedCanvas.Children.Add(_speedLine);
        chartStack.Children.Add(_speedCanvas);
        chartBox.Child = chartStack;
        sp.Children.Add(chartBox);

        // Control Toolbar
        var ctrlRow = new StackPanel { Orientation = Orientation.Horizontal };

        var btnStartSpeed = new Button
        {
            Content = " ▶  Initiate Speed & Latency Test ",
            Height = 36,
            Padding = new Thickness(18, 0, 18, 0),
            Background = SvllBlue,
            Foreground = Brushes.White,
            FontWeight = FontWeights.Bold,
            Cursor = Cursors.Hand,
            Margin = new Thickness(0, 0, 12, 0)
        };
        btnStartSpeed.Click += async (s, e) =>
        {
            btnStartSpeed.IsEnabled = false;
            try { await RunInternetSpeedTestAsync(); }
            finally { btnStartSpeed.IsEnabled = true; }
        };
        ctrlRow.Children.Add(btnStartSpeed);

        var lblPayload = new TextBlock
        {
            Text = "Test Sample Payload:",
            FontSize = 11,
            FontWeight = FontWeights.Medium,
            Foreground = TextDark,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(6, 0, 8, 0)
        };
        ctrlRow.Children.Add(lblPayload);

        _cmbPayloadSize = new ComboBox
        {
            Width = 140,
            Height = 32,
            FontSize = 11,
            VerticalContentAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 12, 0)
        };
        _cmbPayloadSize.Items.Add("Standard (10 MB)");
        _cmbPayloadSize.Items.Add("Express (5 MB)");
        _cmbPayloadSize.Items.Add("Deep Audit (25 MB)");
        _cmbPayloadSize.SelectedIndex = 0;
        ctrlRow.Children.Add(_cmbPayloadSize);

        var btnClear = new Button
        {
            Content = "Reset Benchmark",
            Height = 32,
            Padding = new Thickness(12, 0, 12, 0),
            Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
            BorderBrush = BorderMuted,
            Cursor = Cursors.Hand
        };
        btnClear.Click += (s, e) =>
        {
            _lblSpeedMbps.Text = "-- Mbps";
            _lblSpeedPeak.Text = "-- Mbps";
            _lblSpeedPing.Text = "-- ms";
            _lblSpeedJitter.Text = "-- ms";
            _lblSpeedQuality.Text = "STATUS: READY";
            _lblSpeedQuality.Foreground = TextDark;
            _lblSpeedProgressText.Text = "0%";
            _progSpeed.Value = 0;
            _speedHistory.Clear();
            _speedCanvas.Children.Clear();
            _speedCanvas.Children.Add(_speedFill);
            _speedCanvas.Children.Add(_speedLine);
        };
        ctrlRow.Children.Add(btnClear);

        sp.Children.Add(ctrlRow);

        card.Child = sp;
        root.Children.Add(card);

        scroll.Content = root;
        return scroll;
    }

    private Border CreateSpeedMetricCard(string title, Brush accentBrush, out TextBlock valBlock, string defVal, string caption)
    {
        var border = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12, 10, 12, 10),
            Margin = new Thickness(3, 0, 3, 0)
        };

        var sp = new StackPanel();
        sp.Children.Add(new TextBlock { Text = title, FontSize = 9.0, FontWeight = FontWeights.Bold, Foreground = TextSubtle, Margin = new Thickness(0, 0, 0, 4) });

        valBlock = new TextBlock { Text = defVal, FontSize = 22, FontWeight = FontWeights.ExtraBold, Foreground = accentBrush };
        sp.Children.Add(valBlock);

        sp.Children.Add(new TextBlock { Text = caption, FontSize = 8.5, Foreground = TextSubtle, Margin = new Thickness(0, 3, 0, 0) });
        border.Child = sp;
        return border;
    }

    private async Task RunInternetSpeedTestAsync()
    {
        Log("\n[SPEEDTEST] Initiating live network latency and throughput test...");
        _progSpeed.Value = 10;
        _lblSpeedProgressText.Text = "10% (Auditing Ping & Jitter)";
        _lblSpeedQuality.Text = "AUDITING...";
        _lblSpeedQuality.Foreground = SvllBlue;
        _speedHistory.Clear();

        // 1. Measure Multi-Sample Ping & Latency Jitter
        var pings = new List<long>();
        try
        {
            using var p = new Ping();
            for (int i = 0; i < 4; i++)
            {
                var reply = await p.SendPingAsync("1.1.1.1", 1500);
                if (reply.Status == IPStatus.Success) pings.Add(reply.RoundtripTime);
                await Task.Delay(50);
            }
        }
        catch { }

        long avgLatency = pings.Count > 0 ? (long)pings.Average() : -1;
        long jitter = 0;
        if (pings.Count > 1)
        {
            double diffSum = 0;
            for (int i = 1; i < pings.Count; i++) diffSum += Math.Abs(pings[i] - pings[i - 1]);
            jitter = (long)(diffSum / (pings.Count - 1));
        }

        _lblSpeedPing.Text = avgLatency >= 0 ? $"{avgLatency} ms" : "Timeout";
        _lblSpeedJitter.Text = avgLatency >= 0 ? $"{jitter} ms" : "--";
        _progSpeed.Value = 30;
        _lblSpeedProgressText.Text = "30% (Benchmarking CDN Ingress)";

        // 2. Determine Payload Size
        long targetBytes = 10_000_000;
        if (_cmbPayloadSize?.SelectedIndex == 1) targetBytes = 5_000_000;
        else if (_cmbPayloadSize?.SelectedIndex == 2) targetBytes = 25_000_000;

        double peakMbps = 0.0;

        // 3. Measure Download Throughput
        try
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(25);
            string testUrl = $"https://speed.cloudflare.com/__down?bytes={targetBytes}";

            var sw = Stopwatch.StartNew();
            var response = await client.GetAsync(testUrl, HttpCompletionOption.ResponseHeadersRead);
            using var stream = await response.Content.ReadAsStreamAsync();

            byte[] buffer = new byte[65536];
            long totalBytes = 0;
            int read;

            while ((read = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                totalBytes += read;
                double seconds = sw.Elapsed.TotalSeconds;
                if (seconds > 0.2)
                {
                    double currentMbps = Math.Round((totalBytes * 8.0) / (seconds * 1000000.0), 1);
                    if (currentMbps > peakMbps) peakMbps = currentMbps;

                    _lblSpeedMbps.Text = $"{currentMbps:F1} Mbps";
                    _lblSpeedPeak.Text = $"{peakMbps:F1} Mbps";
                    RenderSparkline(_speedCanvas, _speedLine, _speedFill, _speedHistory, Math.Min(120, currentMbps));
                }
                int pct = Math.Min(95, 30 + (int)((totalBytes / (double)targetBytes) * 65));
                _progSpeed.Value = pct;
                _lblSpeedProgressText.Text = $"{pct}% ({Math.Round(totalBytes / 1048576.0, 1)} MB Transferred)";
            }

            sw.Stop();
            double finalMbps = Math.Round((totalBytes * 8.0) / (sw.Elapsed.TotalSeconds * 1000000.0), 1);
            if (peakMbps < finalMbps) peakMbps = finalMbps;

            _lblSpeedMbps.Text = $"{finalMbps:F1} Mbps";
            _lblSpeedPeak.Text = $"{peakMbps:F1} Mbps";
            _progSpeed.Value = 100;
            _lblSpeedProgressText.Text = "100% (Completed)";

            // Quality Evaluation
            if (finalMbps >= 50 && avgLatency < 40)
            {
                _lblSpeedQuality.Text = "QUALITY: EXCELLENT (ENTERPRISE)";
                _lblSpeedQuality.Foreground = new SolidColorBrush(Color.FromRgb(22, 101, 52));
            }
            else if (finalMbps >= 15 && avgLatency < 100)
            {
                _lblSpeedQuality.Text = "QUALITY: GOOD (OPERATIONAL)";
                _lblSpeedQuality.Foreground = SvllBlue;
            }
            else
            {
                _lblSpeedQuality.Text = "QUALITY: CONGESTED / SLOW";
                _lblSpeedQuality.Foreground = SvllRed;
            }

            Log($"[SPEEDTEST] Completed! Average: {finalMbps:F1} Mbps, Peak: {peakMbps:F1} Mbps, Ping: {avgLatency} ms, Jitter: {jitter} ms.");
        }
        catch (Exception ex)
        {
            _lblSpeedMbps.Text = "Failed";
            _lblSpeedQuality.Text = "TEST FAILED";
            _lblSpeedQuality.Foreground = SvllRed;
            _lblSpeedProgressText.Text = "Failed";
            Log($"[SPEEDTEST ERROR] {ex.Message}");
        }
    }

    #endregion
}
