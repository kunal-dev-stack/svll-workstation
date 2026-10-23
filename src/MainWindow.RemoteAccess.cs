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
using Microsoft.Win32;
using SVLL_IT_Workstation.Models;

namespace SVLL_IT_Workstation;

public partial class MainWindow
{
    // Hub 6: Remote Access & Fleet Console UI Elements
    private TextBlock _lblLocalAnyDeskId = null!;
    private TextBlock _lblLocalAnyDeskStatus = null!;
    private Border _bannerSupportBeacon = null!;
    private TextBlock _lblBeaconText = null!;
    private TextBox _txtSupportRequestNote = null!;
    private StackPanel _fleetDeviceListPanel = null!;
    private TextBlock _lblFleetTotalCount = null!;
    private TextBlock _lblFleetOnlineCount = null!;
    private TextBlock _lblFleetRequestCount = null!;
    private TextBlock _lblFleetOfflineCount = null!;
    private static System.Windows.Threading.DispatcherTimer? _fleetAutoRefreshTimer;
    private TextBox _txtFleetSearch = null!;
    private ComboBox _cmbFleetBranchFilter = null!;

    // -------------------------------------------------------------
    // 1. MY SYSTEM REMOTE ACCESS & ANYDESK (USER & LOCAL ENDPOINT)
    // -------------------------------------------------------------
    public UIElement BuildRemoteClientView()
    {
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var root = new StackPanel { Margin = new Thickness(10) };

        // 1. Header Card
        var headerBorder = new Border
        {
            Background = BgCard,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(16),
            Margin = new Thickness(0, 0, 0, 12)
        };
        var headerGrid = new Grid();
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var headerLeft = new StackPanel();
        var titleRow = new StackPanel { Orientation = Orientation.Horizontal };
        titleRow.Children.Add(new TextBlock
        {
            Text = "🌐 Remote Access & AnyDesk Endpoint Controller",
            FontSize = 16,
            FontWeight = FontWeights.ExtraBold,
            Foreground = TextDark
        });
        titleRow.Children.Add(new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(239, 246, 255)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(191, 219, 254)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(8, 2, 8, 2),
            Margin = new Thickness(10, 0, 0, 0),
            Child = new TextBlock { Text = "HUB 4 • ENDPOINT ACCESS", FontSize = 10, FontWeight = FontWeights.Bold, Foreground = SvllBlue }
        });
        headerLeft.Children.Add(titleRow);
        headerLeft.Children.Add(new TextBlock
        {
            Text = "Provides instant 1-click remote assistance capabilities via AnyDesk and Windows Quick Assist. View your machine's connection ID or request emergency IT support.",
            FontSize = 11,
            Foreground = TextSubtle,
            Margin = new Thickness(0, 4, 0, 0)
        });
        Grid.SetColumn(headerLeft, 0);
        headerGrid.Children.Add(headerLeft);

        // Right side: AnyDesk Quick Launch
        var btnLaunchAnyDesk = new Button
        {
            Content = "🚀 Launch AnyDesk",
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            Background = SvllRed,
            Foreground = Brushes.White,
            Padding = new Thickness(14, 6, 14, 6),
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand,
            VerticalAlignment = VerticalAlignment.Center
        };
        btnLaunchAnyDesk.Click += (s, e) =>
        {
            bool ok = FleetRegistryService.LaunchAnyDesk();
            if (!ok)
            {
                MessageBox.Show("AnyDesk is not installed or detected in default paths. You can install it silently via Hub 5 (WinGet / USB Depot).", "AnyDesk Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        };
        Grid.SetColumn(btnLaunchAnyDesk, 1);
        headerGrid.Children.Add(btnLaunchAnyDesk);

        headerBorder.Child = headerGrid;
        root.Children.Add(headerBorder);

        // 2. Active Beacon Banner (Visible when Support is Requested)
        _bannerSupportBeacon = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(254, 242, 242)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(252, 165, 165)),
            BorderThickness = new Thickness(1.5),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(16, 12, 16, 12),
            Margin = new Thickness(0, 0, 0, 14),
            Visibility = FleetRegistryService.Instance.LocalDevice.Status == "SupportRequested" ? Visibility.Visible : Visibility.Collapsed
        };
        var beaconGrid = new Grid();
        beaconGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        beaconGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var beaconTextStack = new StackPanel();
        beaconTextStack.Children.Add(new TextBlock
        {
            Text = "🚨 IT REMOTE ASSISTANCE BEACON ACTIVE",
            FontSize = 12.5,
            FontWeight = FontWeights.ExtraBold,
            Foreground = SvllRed
        });
        _lblBeaconText = new TextBlock
        {
            Text = $"Broadcasting remote support alert to IT Admin Console: \"{FleetRegistryService.Instance.LocalDevice.SupportRequestMessage}\"",
            FontSize = 11,
            Foreground = TextDark,
            Margin = new Thickness(0, 2, 0, 0)
        };
        beaconTextStack.Children.Add(_lblBeaconText);
        Grid.SetColumn(beaconTextStack, 0);
        beaconGrid.Children.Add(beaconTextStack);

        var btnCancelBeacon = new Button
        {
            Content = "Cancel Request",
            FontSize = 11,
            Padding = new Thickness(12, 4, 12, 4),
            Background = Brushes.White,
            BorderBrush = BorderMuted,
            Cursor = Cursors.Hand,
            VerticalAlignment = VerticalAlignment.Center
        };
        btnCancelBeacon.Click += (s, e) =>
        {
            FleetRegistryService.Instance.CancelRemoteAssistance();
            _bannerSupportBeacon.Visibility = Visibility.Collapsed;
            MessageBox.Show("Support request beacon cancelled.", "Beacon Cancelled", MessageBoxButton.OK, MessageBoxImage.Information);
        };
        Grid.SetColumn(btnCancelBeacon, 1);
        beaconGrid.Children.Add(btnCancelBeacon);

        _bannerSupportBeacon.Child = beaconGrid;
        root.Children.Add(_bannerSupportBeacon);

        // 3. Main Grid: AnyDesk Connection Hub (Left) + Request Assistance Form (Right)
        var mainGrid = new Grid();
        mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.1, GridUnitType.Star) });
        mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.9, GridUnitType.Star) });

        // --- LEFT CARD: ANYDESK & REMOTE TOOLS ---
        var anyDeskCard = new Border
        {
            Background = BgCard,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(18),
            Margin = new Thickness(0, 0, 6, 0)
        };
        var anyDeskPanel = new StackPanel();

        anyDeskPanel.Children.Add(new TextBlock
        {
            Text = "🔴 Local AnyDesk Remote Identifier",
            FontSize = 13.5,
            FontWeight = FontWeights.Bold,
            Foreground = TextDark
        });
        anyDeskPanel.Children.Add(new TextBlock
        {
            Text = "Your unique 9-to-10 digit AnyDesk address allows IT engineers to securely connect and resolve issues remotely.",
            FontSize = 10.5,
            Foreground = TextSubtle,
            Margin = new Thickness(0, 3, 0, 12)
        });

        // AnyDesk ID Big Display Box
        var idBox = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225)),
            BorderThickness = new Thickness(1.5),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(16, 12, 16, 12),
            Margin = new Thickness(0, 0, 0, 14)
        };
        var idGrid = new Grid();
        idGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        idGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var idStack = new StackPanel();
        idStack.Children.Add(new TextBlock
        {
            Text = "THIS WORKSTATION ANYDESK ID",
            FontSize = 9.5,
            FontWeight = FontWeights.Bold,
            Foreground = TextSubtle
        });

        string detectedId = FleetRegistryService.Instance.LocalDevice.AnyDeskId;
        _lblLocalAnyDeskId = new TextBlock
        {
            Text = string.IsNullOrEmpty(detectedId) ? "Not Configured" : detectedId,
            FontSize = 24,
            FontWeight = FontWeights.Black,
            FontFamily = new FontFamily("Consolas, Courier New"),
            Foreground = SvllRed,
            Margin = new Thickness(0, 2, 0, 0)
        };
        idStack.Children.Add(_lblLocalAnyDeskId);

        _lblLocalAnyDeskStatus = new TextBlock
        {
            Text = FleetRegistryService.Instance.LocalDevice.AnyDeskInstalled ? "🟢 AnyDesk Service Active & Ready" : "⚠️ AnyDesk Not Detected in Default Directory",
            FontSize = 10.5,
            FontWeight = FontWeights.SemiBold,
            Foreground = FleetRegistryService.Instance.LocalDevice.AnyDeskInstalled ? new SolidColorBrush(Color.FromRgb(22, 101, 52)) : new SolidColorBrush(Color.FromRgb(180, 83, 9)),
            Margin = new Thickness(0, 4, 0, 0)
        };
        idStack.Children.Add(_lblLocalAnyDeskStatus);
        Grid.SetColumn(idStack, 0);
        idGrid.Children.Add(idStack);

        var idActions = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        var btnCopyId = new Button
        {
            Content = "📋 Copy ID",
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Padding = new Thickness(10, 6, 10, 6),
            Margin = new Thickness(0, 0, 6, 0),
            Background = Brushes.White,
            BorderBrush = BorderMuted,
            Cursor = Cursors.Hand
        };
        btnCopyId.Click += (s, e) =>
        {
            Clipboard.SetText(_lblLocalAnyDeskId.Text.Replace(" ", ""));
            MessageBox.Show($"AnyDesk ID [{_lblLocalAnyDeskId.Text}] copied to clipboard.", "Copied", MessageBoxButton.OK, MessageBoxImage.Information);
        };
        idActions.Children.Add(btnCopyId);

        var btnRefreshId = new Button
        {
            Content = "🔄",
            ToolTip = "Re-check AnyDesk config",
            FontSize = 12,
            Padding = new Thickness(8, 6, 8, 6),
            Background = Brushes.White,
            BorderBrush = BorderMuted,
            Cursor = Cursors.Hand
        };
        btnRefreshId.Click += (s, e) =>
        {
            string newId = FleetRegistryService.DetectAnyDeskId();
            _lblLocalAnyDeskId.Text = newId;
            FleetRegistryService.Instance.LocalDevice.AnyDeskId = newId;
        };
        idActions.Children.Add(btnRefreshId);

        Grid.SetColumn(idActions, 1);
        idGrid.Children.Add(idActions);

        idBox.Child = idGrid;
        anyDeskPanel.Children.Add(idBox);

        // Windows Built-in Remote Tools: Quick Assist & RDP
        anyDeskPanel.Children.Add(new TextBlock
        {
            Text = "🖥️ Windows Remote Management Options",
            FontSize = 12.5,
            FontWeight = FontWeights.Bold,
            Foreground = TextDark,
            Margin = new Thickness(0, 6, 0, 8)
        });

        var toolsRow = new WrapPanel { Margin = new Thickness(0, 0, 0, 10) };

        var btnQuickAssist = new Button
        {
            Content = "🤝 Launch Quick Assist",
            FontSize = 11,
            Padding = new Thickness(12, 6, 12, 6),
            Margin = new Thickness(0, 0, 8, 8),
            Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
            BorderBrush = BorderMuted,
            Cursor = Cursors.Hand
        };
        btnQuickAssist.Click += (s, e) =>
        {
            if (!FleetRegistryService.LaunchQuickAssist())
            {
                MessageBox.Show("Windows Quick Assist could not be launched on this system.", "Quick Assist Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        };
        toolsRow.Children.Add(btnQuickAssist);

        var btnRdpConfig = new Button
        {
            Content = "🔒 Toggle Remote Desktop (RDP)",
            FontSize = 11,
            Padding = new Thickness(12, 6, 12, 6),
            Margin = new Thickness(0, 0, 8, 8),
            Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
            BorderBrush = BorderMuted,
            Cursor = Cursors.Hand
        };
        btnRdpConfig.Click += (s, e) =>
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"System\CurrentControlSet\Control\Terminal Server", true);
                if (key != null)
                {
                    object? val = key.GetValue("fDenyTSConnections");
                    int current = val is int i ? i : 1;
                    int next = current == 1 ? 0 : 1;
                    key.SetValue("fDenyTSConnections", next, RegistryValueKind.DWord);

                    string statusMsg = next == 0 ? "ENABLED (Connections Allowed)" : "DISABLED (Blocked)";
                    Log($"[REMOTE DESKTOP] Windows RDP fDenyTSConnections set to {next} ({statusMsg}).");
                    MessageBox.Show($"Windows Remote Desktop is now: {statusMsg}", "RDP Configuration", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to toggle RDP: {ex.Message}. Administrative privileges required.", "Permission Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        };
        toolsRow.Children.Add(btnRdpConfig);

        anyDeskPanel.Children.Add(toolsRow);

        // System Vitals Snapshot Card
        var vitalsBox = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 4, 0, 0)
        };
        var vitalsStack = new StackPanel();
        vitalsStack.Children.Add(new TextBlock
        {
            Text = $"💻 Computer: {Environment.MachineName}  •  👤 User: {Environment.UserName}",
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Foreground = TextDark
        });
        vitalsStack.Children.Add(new TextBlock
        {
            Text = $"🌐 IP Address: {FleetRegistryService.Instance.LocalDevice.IpAddress}  •  MAC: {FleetRegistryService.Instance.LocalDevice.MacAddress}",
            FontSize = 10.5,
            Foreground = TextSubtle,
            Margin = new Thickness(0, 3, 0, 0)
        });
        vitalsBox.Child = vitalsStack;
        anyDeskPanel.Children.Add(vitalsBox);

        anyDeskCard.Child = anyDeskPanel;
        Grid.SetColumn(anyDeskCard, 0);
        mainGrid.Children.Add(anyDeskCard);

        // --- RIGHT CARD: REQUEST IMMEDIATE REMOTE ASSISTANCE ---
        var reqCard = new Border
        {
            Background = BgCard,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(18),
            Margin = new Thickness(6, 0, 0, 0)
        };
        var reqPanel = new StackPanel();

        reqPanel.Children.Add(new TextBlock
        {
            Text = "🚨 1-Click Remote IT Assistance Request",
            FontSize = 13.5,
            FontWeight = FontWeights.Bold,
            Foreground = TextDark
        });
        reqPanel.Children.Add(new TextBlock
        {
            Text = "Send an urgent notification directly to the IT Admin Fleet Console without needing to dictate codes or IP addresses over phone.",
            FontSize = 10.5,
            Foreground = TextSubtle,
            Margin = new Thickness(0, 3, 0, 12)
        });

        reqPanel.Children.Add(new TextBlock
        {
            Text = "Describe your issue / reason for remote assistance:",
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Foreground = TextDark,
            Margin = new Thickness(0, 0, 0, 4)
        });

        _txtSupportRequestNote = new TextBox
        {
            Height = 85,
            TextWrapping = TextWrapping.Wrap,
            AcceptsReturn = true,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Padding = new Thickness(8),
            FontSize = 11.5,
            BorderBrush = BorderMuted,
            Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
            Margin = new Thickness(0, 0, 0, 12),
            Text = "Thermal barcode printer skipping labels during dispatch run."
        };
        reqPanel.Children.Add(_txtSupportRequestNote);

        var quickIssueWrap = new WrapPanel { Margin = new Thickness(0, 0, 0, 14) };
        string[] quickNotes = new[]
        {
            "🖨️ Zebra Printer Stuck",
            "🌐 WMS ERP Disconnected",
            "⚡ System Freeze / Slow",
            "🔒 USB Pen Drive Blocked"
        };
        foreach (var qn in quickNotes)
        {
            var btnChip = new Button
            {
                Content = qn,
                FontSize = 10,
                Padding = new Thickness(8, 3, 8, 3),
                Margin = new Thickness(0, 0, 6, 6),
                Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
                BorderBrush = BorderMuted,
                Cursor = Cursors.Hand
            };
            btnChip.Click += (s, e) => _txtSupportRequestNote.Text = qn.Substring(qn.IndexOf(' ') + 1);
            quickIssueWrap.Children.Add(btnChip);
        }
        reqPanel.Children.Add(quickIssueWrap);

        var btnSendRequest = new Button
        {
            Content = "📢 Broadcast Remote Support Beacon",
            Height = 38,
            FontSize = 12,
            FontWeight = FontWeights.Bold,
            Background = SvllBlue,
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand
        };
        btnSendRequest.Click += (s, e) =>
        {
            string note = _txtSupportRequestNote.Text.Trim();
            if (string.IsNullOrEmpty(note)) note = "Urgent IT assistance requested.";
            FleetRegistryService.Instance.RequestRemoteAssistance(note);
            _lblBeaconText.Text = $"Broadcasting remote support alert to IT Admin Console: \"{note}\"";
            _bannerSupportBeacon.Visibility = Visibility.Visible;
            MessageBox.Show("✔ Remote Support Beacon broadcasted!\n\nIT Support can now see your PC and AnyDesk ID on the Central Fleet Console.", "Support Beacon Active", MessageBoxButton.OK, MessageBoxImage.Information);
        };
        reqPanel.Children.Add(btnSendRequest);

        reqCard.Child = reqPanel;
        Grid.SetColumn(reqCard, 1);
        mainGrid.Children.Add(reqCard);

        root.Children.Add(mainGrid);

        scroll.Content = root;
        return scroll;
    }

    // -------------------------------------------------------------
    // 2. FLEET DEVICE ADMIN CONSOLE (PASSWORD PROTECTED)
    // -------------------------------------------------------------
    public UIElement BuildFleetAdminConsoleView()
    {
        var root = new Grid();

        // Lock screen container if not authenticated
        var lockContainer = BuildAdminLockScreen("Fleet Device Admin Console", () =>
        {
            // Callback when unlocked
            RefreshFleetAdminConsoleContent(root);
        });

        if (!AdminAuthService.Instance.IsAdminMode)
        {
            root.Children.Add(lockContainer);
        }
        else
        {
            RefreshFleetAdminConsoleContent(root);
        }

        // Listen for session lock
        AdminAuthService.Instance.AdminModeChanged += (isAdmin) =>
        {
            Dispatcher.Invoke(() =>
            {
                if (!isAdmin)
                {
                    root.Children.Clear();
                    root.Children.Add(BuildAdminLockScreen("Fleet Device Admin Console", () =>
                    {
                        RefreshFleetAdminConsoleContent(root);
                    }));
                }
            });
        };

        return root;
    }

    private void RefreshFleetAdminConsoleContent(Grid root)
    {
        root.Children.Clear();

        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var mainStack = new StackPanel { Margin = new Thickness(10) };

        // 1. Header with Admin Master Badge & Lock Session Button
        var headerBorder = new Border
        {
            Background = BgCard,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(16),
            Margin = new Thickness(0, 0, 0, 12)
        };
        var headerGrid = new Grid();
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var headerLeft = new StackPanel();
        var titleRow = new StackPanel { Orientation = Orientation.Horizontal };
        titleRow.Children.Add(new TextBlock
        {
            Text = "👑 Fleet Device Management & Remote Admin Console",
            FontSize = 16,
            FontWeight = FontWeights.ExtraBold,
            Foreground = TextDark
        });
        titleRow.Children.Add(new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(254, 243, 199)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(252, 211, 77)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(8, 2, 8, 2),
            Margin = new Thickness(10, 0, 0, 0),
            Child = new TextBlock { Text = "ADMIN ONLY", FontSize = 10, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(146, 64, 14)) }
        });
        headerLeft.Children.Add(titleRow);
        headerLeft.Children.Add(new TextBlock
        {
            Text = "Live monitor and 1-click remote desktop access for all connected corporate workstations across SVLL logistics branches.",
            FontSize = 11,
            Foreground = TextSubtle,
            Margin = new Thickness(0, 4, 0, 0)
        });
        Grid.SetColumn(headerLeft, 0);
        headerGrid.Children.Add(headerLeft);

        var headerRight = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        var btnExportCsv = new Button
        {
            Content = "📄 Export Fleet (CSV)",
            FontSize = 11,
            Padding = new Thickness(10, 6, 10, 6),
            Margin = new Thickness(0, 0, 8, 0),
            Background = Brushes.White,
            BorderBrush = BorderMuted,
            Cursor = Cursors.Hand
        };
        btnExportCsv.Click += (s, e) =>
        {
            try
            {
                string csv = FleetRegistryService.Instance.ExportFleetCsv();
                string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "SVLL_Fleet_Directory.csv");
                File.WriteAllText(path, csv);
                MessageBox.Show($"Fleet inventory exported to:\n{path}", "Export Successful", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        };
        headerRight.Children.Add(btnExportCsv);

        var btnMeshPortal = new Button
        {
            Content = "🌐 MeshCentral Portal",
            FontSize = 11,
            Padding = new Thickness(10, 6, 10, 6),
            Margin = new Thickness(0, 0, 8, 0),
            Background = new SolidColorBrush(Color.FromRgb(240, 253, 244)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(187, 247, 208)),
            Foreground = new SolidColorBrush(Color.FromRgb(22, 101, 52)),
            Cursor = Cursors.Hand,
            ToolTip = "Open MeshCentral web console for fleet remote control"
        };
        btnMeshPortal.Click += (s, e) =>
        {
            string url = AdminAuthService.Instance.GetMeshCentralUrl();
            if (string.IsNullOrEmpty(url)) url = "http://localhost:8086";
            MeshAgentService.LaunchMeshDashboard(url);
        };
        headerRight.Children.Add(btnMeshPortal);

        var btnServerSettings = new Button
        {
            Content = "⚙️ Server Settings",
            FontSize = 11,
            Padding = new Thickness(10, 6, 10, 6),
            Margin = new Thickness(0, 0, 8, 0),
            Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
            BorderBrush = BorderMuted,
            Cursor = Cursors.Hand,
            ToolTip = "Configure Relay Server URL and MeshCentral Server URL"
        };
        btnServerSettings.Click += (s, e) => ShowServerSettingsDialog();
        headerRight.Children.Add(btnServerSettings);

        var btnLockSession = new Button
        {
            Content = "🔒 Lock Admin Session",
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
            BorderBrush = BorderMuted,
            Padding = new Thickness(12, 6, 12, 6),
            Cursor = Cursors.Hand
        };
        btnLockSession.Click += (s, e) =>
        {
            AdminAuthService.Instance.LockAdmin();
        };
        headerRight.Children.Add(btnLockSession);

        Grid.SetColumn(headerRight, 1);
        headerGrid.Children.Add(headerRight);

        headerBorder.Child = headerGrid;
        mainStack.Children.Add(headerBorder);

        // 2. Direct Fast Remote Connect Console
        var fastConnectBorder = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225)),
            BorderThickness = new Thickness(1.5),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(16, 12, 16, 12),
            Margin = new Thickness(0, 0, 0, 12)
        };
        var fastGrid = new Grid();
        fastGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var fastLeft = new StackPanel();
        var fastTitle = new StackPanel { Orientation = Orientation.Horizontal };
        fastTitle.Children.Add(new TextBlock { Text = "⚡ Direct Fast Remote Connect", FontSize = 13, FontWeight = FontWeights.ExtraBold, Foreground = TextDark });
        fastTitle.Children.Add(new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(220, 252, 231)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(187, 247, 208)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(6, 1, 6, 1),
            Margin = new Thickness(8, 0, 0, 0),
            Child = new TextBlock { Text = "INSTANT ACCESS", FontSize = 9, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(22, 101, 52)) }
        });
        fastLeft.Children.Add(fastTitle);
        fastLeft.Children.Add(new TextBlock
        {
            Text = "Enter any Target IP (e.g. 192.168.10.x), Computer Name, or AnyDesk ID to connect directly with 1 click:",
            FontSize = 10.5,
            Foreground = TextSubtle,
            Margin = new Thickness(0, 2, 0, 6)
        });

        var fastInputRow = new StackPanel { Orientation = Orientation.Horizontal };
        var txtDirectTarget = new TextBox
        {
            Width = 260,
            Height = 30,
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            FontFamily = new FontFamily("Consolas, Courier New"),
            Padding = new Thickness(8, 4, 8, 4),
            VerticalContentAlignment = VerticalAlignment.Center,
            BorderBrush = BorderMuted
        };
        fastInputRow.Children.Add(txtDirectTarget);

        var btnFastAnyDesk = new Button
        {
            Content = "🚀 AnyDesk",
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            Background = SvllRed,
            Foreground = Brushes.White,
            Padding = new Thickness(12, 5, 12, 5),
            Margin = new Thickness(8, 0, 0, 0),
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand
        };
        btnFastAnyDesk.Click += (s, e) =>
        {
            string target = txtDirectTarget.Text.Trim();
            if (string.IsNullOrEmpty(target)) { MessageBox.Show("Please enter an AnyDesk ID or IP address.", "Target Missing", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            FleetRegistryService.LaunchAnyDesk(target);
        };
        fastInputRow.Children.Add(btnFastAnyDesk);

        var btnFastRdp = new Button
        {
            Content = "💻 RDP Desktop",
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            Background = new SolidColorBrush(Color.FromRgb(30, 64, 175)),
            Foreground = Brushes.White,
            Padding = new Thickness(12, 5, 12, 5),
            Margin = new Thickness(6, 0, 0, 0),
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand
        };
        btnFastRdp.Click += (s, e) =>
        {
            string target = txtDirectTarget.Text.Trim();
            if (string.IsNullOrEmpty(target)) { MessageBox.Show("Please enter an IP address or hostname.", "Target Missing", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            FleetRegistryService.LaunchRdp(target);
        };
        fastInputRow.Children.Add(btnFastRdp);

        var btnFastShare = new Button
        {
            Content = "📁 Direct C$ Share",
            FontSize = 11,
            Background = Brushes.White,
            BorderBrush = BorderMuted,
            Padding = new Thickness(10, 5, 10, 5),
            Margin = new Thickness(6, 0, 0, 0),
            Cursor = Cursors.Hand,
            ToolTip = "Open \\\\IP\\c$ in Windows Explorer"
        };
        btnFastShare.Click += (s, e) =>
        {
            string target = txtDirectTarget.Text.Trim();
            if (string.IsNullOrEmpty(target)) { MessageBox.Show("Please enter an IP address or hostname.", "Target Missing", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            FleetRegistryService.LaunchFileExplorer(target);
        };
        fastInputRow.Children.Add(btnFastShare);

        var btnFastShell = new Button
        {
            Content = "📡 PowerShell Shell",
            FontSize = 11,
            Background = Brushes.White,
            BorderBrush = BorderMuted,
            Padding = new Thickness(10, 5, 10, 5),
            Margin = new Thickness(6, 0, 0, 0),
            Cursor = Cursors.Hand,
            ToolTip = "Open remote PowerShell interactive shell"
        };
        btnFastShell.Click += (s, e) =>
        {
            string target = txtDirectTarget.Text.Trim();
            if (string.IsNullOrEmpty(target)) { MessageBox.Show("Please enter an IP address or hostname.", "Target Missing", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            FleetRegistryService.LaunchRemoteShell(target);
        };
        fastInputRow.Children.Add(btnFastShell);

        var btnFastManage = new Button
        {
            Content = "⚙️ Manage",
            FontSize = 11,
            Background = Brushes.White,
            BorderBrush = BorderMuted,
            Padding = new Thickness(10, 5, 10, 5),
            Margin = new Thickness(6, 0, 0, 0),
            Cursor = Cursors.Hand,
            ToolTip = "Open Computer Management for remote computer"
        };
        btnFastManage.Click += (s, e) =>
        {
            string target = txtDirectTarget.Text.Trim();
            if (string.IsNullOrEmpty(target)) { MessageBox.Show("Please enter an IP address or hostname.", "Target Missing", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            FleetRegistryService.LaunchComputerManagement(target);
        };
        fastInputRow.Children.Add(btnFastManage);

        var btnFastPing = new Button
        {
            Content = "⚡ Ping",
            FontSize = 11,
            Background = Brushes.White,
            BorderBrush = BorderMuted,
            Padding = new Thickness(10, 5, 10, 5),
            Margin = new Thickness(6, 0, 0, 0),
            Cursor = Cursors.Hand
        };
        btnFastPing.Click += async (s, e) =>
        {
            string target = txtDirectTarget.Text.Trim();
            if (string.IsNullOrEmpty(target)) { MessageBox.Show("Please enter an IP address or hostname.", "Target Missing", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            btnFastPing.IsEnabled = false;
            btnFastPing.Content = "⏳";
            long ms = await FleetRegistryService.PingDeviceAsync(target);
            btnFastPing.IsEnabled = true;
            btnFastPing.Content = "⚡ Ping";
            if (ms >= 0)
            {
                MessageBox.Show($"Reply from {target}: latency = {ms} ms 🟢", "Ping Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show($"Request timed out for {target} 🔴", "Ping Timeout", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        };
        fastInputRow.Children.Add(btnFastPing);

        fastLeft.Children.Add(fastInputRow);
        Grid.SetColumn(fastLeft, 0);
        fastGrid.Children.Add(fastLeft);

        fastConnectBorder.Child = fastGrid;
        mainStack.Children.Add(fastConnectBorder);

        // 3. Metrics Bar (Total, Online, Support Requests, Offline)
        var metricsGrid = new Grid { Margin = new Thickness(0, 0, 0, 12) };
        metricsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        metricsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        metricsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        metricsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var devices = FleetRegistryService.Instance.GetAllDevices();
        int totalDevs = devices.Count;
        int onlineDevs = devices.Count(d => d.Status == "Online");
        int requestDevs = devices.Count(d => d.Status == "SupportRequested");
        int offlineDevs = devices.Count(d => d.Status == "Offline");

        _lblFleetTotalCount = new TextBlock { Text = totalDevs.ToString(), FontSize = 20, FontWeight = FontWeights.ExtraBold, Foreground = SvllBlue };
        _lblFleetOnlineCount = new TextBlock { Text = onlineDevs.ToString(), FontSize = 20, FontWeight = FontWeights.ExtraBold, Foreground = new SolidColorBrush(Color.FromRgb(22, 101, 52)) };
        _lblFleetRequestCount = new TextBlock { Text = requestDevs.ToString(), FontSize = 20, FontWeight = FontWeights.ExtraBold, Foreground = SvllRed };
        _lblFleetOfflineCount = new TextBlock { Text = offlineDevs.ToString(), FontSize = 20, FontWeight = FontWeights.ExtraBold, Foreground = TextSubtle };

        metricsGrid.Children.Add(BuildMetricCard("🖥️ TOTAL ENDPOINTS", _lblFleetTotalCount, "Managed Workstations", 0));
        metricsGrid.Children.Add(BuildMetricCard("🟢 ONLINE & READY", _lblFleetOnlineCount, "Reachable via AnyDesk", 1));
        metricsGrid.Children.Add(BuildMetricCard("🚨 SUPPORT REQUESTED", _lblFleetRequestCount, "Immediate Action Needed", 2));
        metricsGrid.Children.Add(BuildMetricCard("⚪ OFFLINE / SLEEP", _lblFleetOfflineCount, "Not seen > 1 hr", 3));

        mainStack.Children.Add(metricsGrid);

        // 4. Filter & Search Controls
        var filterCard = new Border
        {
            Background = BgCard,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(12, 10, 12, 10),
            Margin = new Thickness(0, 0, 0, 12)
        };
        var filterGrid = new Grid();
        filterGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Search
        filterGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(170) }); // Branch
        filterGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Live status pill
        filterGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Refresh

        var searchPanel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        searchPanel.Children.Add(new TextBlock { Text = "🔍", FontSize = 14, Margin = new Thickness(0, 0, 8, 0), VerticalAlignment = VerticalAlignment.Center });
        _txtFleetSearch = new TextBox
        {
            Width = 260,
            Height = 28,
            Padding = new Thickness(6, 2, 6, 2),
            FontSize = 11,
            BorderBrush = BorderMuted
        };
        _txtFleetSearch.TextChanged += (s, e) => RenderFleetDeviceCards();
        searchPanel.Children.Add(_txtFleetSearch);
        Grid.SetColumn(searchPanel, 0);
        filterGrid.Children.Add(searchPanel);

        _cmbFleetBranchFilter = new ComboBox
        {
            Height = 28,
            FontSize = 11,
            Margin = new Thickness(0, 0, 8, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        _cmbFleetBranchFilter.Items.Add("All Branches");
        _cmbFleetBranchFilter.Items.Add("Mumbai HQ");
        _cmbFleetBranchFilter.Items.Add("Delhi Logistics Park");
        _cmbFleetBranchFilter.Items.Add("Raipur Main Warehouse");
        _cmbFleetBranchFilter.Items.Add("Ahmedabad Terminal");
        _cmbFleetBranchFilter.Items.Add("Kolkata Distribution");
        _cmbFleetBranchFilter.SelectedIndex = 0;
        _cmbFleetBranchFilter.SelectionChanged += (s, e) => RenderFleetDeviceCards();
        Grid.SetColumn(_cmbFleetBranchFilter, 1);
        filterGrid.Children.Add(_cmbFleetBranchFilter);

        var autoSyncPill = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(240, 253, 244)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(187, 247, 208)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(8, 4, 8, 4),
            Margin = new Thickness(0, 0, 8, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock { Text = "🟢 Live Auto-Sync: 4s", FontSize = 10, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(22, 101, 52)) }
        };
        Grid.SetColumn(autoSyncPill, 2);
        filterGrid.Children.Add(autoSyncPill);

        var btnRefreshFleet = new Button
        {
            Content = "🔄 Refresh Fleet",
            FontSize = 11,
            Padding = new Thickness(12, 4, 12, 4),
            Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
            BorderBrush = BorderMuted,
            Cursor = Cursors.Hand
        };
        btnRefreshFleet.Click += (s, e) =>
        {
            FleetRegistryService.Instance.LoadFleet();
            RenderFleetDeviceCards();
            UpdateFleetMetricCounters();
        };
        Grid.SetColumn(btnRefreshFleet, 3);
        filterGrid.Children.Add(btnRefreshFleet);

        filterCard.Child = filterGrid;
        mainStack.Children.Add(filterCard);

        // 5. Device Cards Container
        _fleetDeviceListPanel = new StackPanel();
        RenderFleetDeviceCards();
        mainStack.Children.Add(_fleetDeviceListPanel);

        // Start auto-refresh timer to automatically reflect newly installed devices
        StartFleetAutoRefresh();

        scroll.Content = mainStack;
        root.Children.Add(scroll);
    }

    private void StartFleetAutoRefresh()
    {
        if (_fleetAutoRefreshTimer == null)
        {
            _fleetAutoRefreshTimer = new System.Windows.Threading.DispatcherTimer();
            _fleetAutoRefreshTimer.Interval = TimeSpan.FromSeconds(4);
            _fleetAutoRefreshTimer.Tick += async (s, e) =>
            {
                try
                {
                    await CloudSyncService.Instance.PollFleetAsync();
                    RenderFleetDeviceCards();
                    UpdateFleetMetricCounters();
                }
                catch { }
            };
        }
        if (!_fleetAutoRefreshTimer.IsEnabled)
        {
            _fleetAutoRefreshTimer.Start();
        }
    }

    private void UpdateFleetMetricCounters()
    {
        try
        {
            var devices = FleetRegistryService.Instance.GetAllDevices();
            if (_lblFleetTotalCount != null) _lblFleetTotalCount.Text = devices.Count.ToString();
            if (_lblFleetOnlineCount != null) _lblFleetOnlineCount.Text = devices.Count(d => d.Status == "Online").ToString();
            if (_lblFleetRequestCount != null) _lblFleetRequestCount.Text = devices.Count(d => d.Status == "SupportRequested").ToString();
            if (_lblFleetOfflineCount != null) _lblFleetOfflineCount.Text = devices.Count(d => d.Status == "Offline").ToString();
        }
        catch { }
    }

    private void RenderFleetDeviceCards()
    {
        if (_fleetDeviceListPanel == null) return;
        _fleetDeviceListPanel.Children.Clear();

        string query = _txtFleetSearch?.Text.Trim().ToLowerInvariant() ?? "";
        string branch = _cmbFleetBranchFilter?.SelectedItem?.ToString() ?? "All Branches";

        var devices = FleetRegistryService.Instance.GetAllDevices();

        foreach (var dev in devices)
        {
            // Apply branch filter
            if (branch != "All Branches" && !dev.BranchLocation.Contains(branch.Replace("All Branches", ""), StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Apply search query
            if (!string.IsNullOrEmpty(query))
            {
                bool match = dev.Hostname.ToLowerInvariant().Contains(query) ||
                             dev.LoggedInUser.ToLowerInvariant().Contains(query) ||
                             dev.IpAddress.ToLowerInvariant().Contains(query) ||
                             dev.AnyDeskId.ToLowerInvariant().Contains(query) ||
                             dev.BranchLocation.ToLowerInvariant().Contains(query) ||
                             (!string.IsNullOrEmpty(dev.SerialNumber) && dev.SerialNumber.ToLowerInvariant().Contains(query)) ||
                             (!string.IsNullOrEmpty(dev.MacAddress) && dev.MacAddress.ToLowerInvariant().Contains(query));
                if (!match) continue;
            }

            _fleetDeviceListPanel.Children.Add(BuildDeviceCard(dev));
        }
    }

    private UIElement BuildDeviceCard(RemoteDeviceItem dev)
    {
        bool isRequested = dev.Status == "SupportRequested";
        bool isOnline = dev.Status == "Online" || isRequested;

        var card = new Border
        {
            Background = BgCard,
            BorderBrush = isRequested ? SvllRed : BorderMuted,
            BorderThickness = new Thickness(isRequested ? 1.5 : 1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(14, 12, 14, 12),
            Margin = new Thickness(0, 0, 0, 8)
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(280) }); // Host & Status
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Telemetry & User
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // 1-Click Connect Actions

        // Col 0: Hostname, Branch & Status Pill
        var col0 = new StackPanel();
        var hostRow = new StackPanel { Orientation = Orientation.Horizontal };
        hostRow.Children.Add(new TextBlock
        {
            Text = dev.Hostname,
            FontSize = 13,
            FontWeight = FontWeights.ExtraBold,
            Foreground = TextDark
        });
        if (dev.IsLocalHost)
        {
            hostRow.Children.Add(new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(4, 1, 4, 1),
                Margin = new Thickness(6, 0, 0, 0),
                Child = new TextBlock { Text = "THIS PC", FontSize = 9, FontWeight = FontWeights.Bold, Foreground = TextSubtle }
            });
        }
        col0.Children.Add(hostRow);

        col0.Children.Add(new TextBlock
        {
            Text = $"🏢 {dev.BranchLocation}",
            FontSize = 10.5,
            Foreground = TextSubtle,
            Margin = new Thickness(0, 2, 0, 4)
        });

        // Status indicator
        var statusBadge = new Border
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(6, 2, 6, 2)
        };
        if (isRequested)
        {
            statusBadge.Background = new SolidColorBrush(Color.FromRgb(254, 242, 242));
            statusBadge.Child = new TextBlock { Text = "🚨 URGENT: Remote Assistance Requested", FontSize = 9.5, FontWeight = FontWeights.Bold, Foreground = SvllRed };
        }
        else if (isOnline)
        {
            statusBadge.Background = new SolidColorBrush(Color.FromRgb(220, 252, 231));
            statusBadge.Child = new TextBlock { Text = "🟢 Online & Heartbeat Verified", FontSize = 9.5, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(22, 101, 52)) };
        }
        else
        {
            statusBadge.Background = new SolidColorBrush(Color.FromRgb(241, 245, 249));
            statusBadge.Child = new TextBlock { Text = "⚪ Offline", FontSize = 9.5, FontWeight = FontWeights.Bold, Foreground = TextSubtle };
        }
        col0.Children.Add(statusBadge);

        Grid.SetColumn(col0, 0);
        grid.Children.Add(col0);

        // Col 1: Telemetry, AnyDesk ID, Hardware Vitals (v8.0)
        var col1 = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        var idRow = new StackPanel { Orientation = Orientation.Horizontal };
        idRow.Children.Add(new TextBlock { Text = "AnyDesk: ", FontSize = 11, FontWeight = FontWeights.Bold, Foreground = TextSubtle });
        idRow.Children.Add(new TextBlock
        {
            Text = string.IsNullOrEmpty(dev.AnyDeskId) ? "N/A" : dev.AnyDeskId,
            FontSize = 12.5,
            FontWeight = FontWeights.ExtraBold,
            FontFamily = new FontFamily("Consolas, Courier New"),
            Foreground = SvllRed
        });

        if (dev.MeshAgentRunning || !string.IsNullOrEmpty(dev.MeshNodeId))
        {
            var meshPill = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(240, 253, 244)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(187, 247, 208)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6, 1, 6, 1),
                Margin = new Thickness(10, 0, 0, 0),
                Child = new TextBlock { Text = "🌐 MESH AGENT READY", FontSize = 9, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(22, 101, 52)) }
            };
            idRow.Children.Add(meshPill);
        }
        col1.Children.Add(idRow);

        // Telemetry Row 1: User, IP, MAC, Last Seen
        col1.Children.Add(new TextBlock
        {
            Text = $"👤 User: {dev.LoggedInUser}  •  🌐 IP: {dev.IpAddress}  •  🏷️ MAC: {(string.IsNullOrEmpty(dev.MacAddress) ? "N/A" : dev.MacAddress)}  •  Seen: {dev.LastSeen:HH:mm:ss}",
            FontSize = 10.5,
            Foreground = TextDark,
            Margin = new Thickness(0, 2, 0, 0)
        });

        // Telemetry Row 2: Serial Number, OS & Build (v8.0)
        col1.Children.Add(new TextBlock
        {
            Text = $"🔢 S/N: {(string.IsNullOrEmpty(dev.SerialNumber) ? "N/A" : dev.SerialNumber)}  •  🪟 OS: {dev.OsVersion} (Build {(string.IsNullOrEmpty(dev.OsBuild) ? "N/A" : dev.OsBuild)})",
            FontSize = 10,
            Foreground = TextSubtle,
            Margin = new Thickness(0, 2, 0, 0)
        });

        // Telemetry Row 3: CPU, RAM, Disk Free (v8.0)
        if (dev.CpuLoadPercent > 0 || dev.RamUsagePercent > 0 || dev.DiskFreePercent > 0)
        {
            col1.Children.Add(new TextBlock
            {
                Text = $"⚡ CPU: {dev.CpuLoadPercent}%  •  🧠 RAM: {dev.RamUsagePercent}%  •  💾 C: Free {dev.DiskFreePercent}%",
                FontSize = 10,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(30, 64, 175)),
                Margin = new Thickness(0, 2, 0, 0)
            });
        }

        if (isRequested && !string.IsNullOrEmpty(dev.SupportRequestMessage))
        {
            col1.Children.Add(new TextBlock
            {
                Text = $"Reason: \"{dev.SupportRequestMessage}\"",
                FontSize = 10.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = SvllRed,
                Margin = new Thickness(0, 3, 0, 0)
            });
        }

        Grid.SetColumn(col1, 1);
        grid.Children.Add(col1);

        // Col 2: Action Buttons (Zero-Password Mesh, AnyDesk, RDP, Ping)
        var col2 = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

        // v8.0: Zero-Password Remote Control (MeshCentral)
        var btnConnectMesh = new Button
        {
            Content = "🌐 Zero-Password Remote",
            FontSize = 10.5,
            FontWeight = FontWeights.Bold,
            Background = new SolidColorBrush(Color.FromRgb(22, 101, 52)),
            Foreground = Brushes.White,
            Padding = new Thickness(10, 6, 10, 6),
            Margin = new Thickness(0, 0, 6, 0),
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand,
            ToolTip = "Connect over internet without remote password prompt via MeshCentral"
        };
        btnConnectMesh.Click += (s, e) =>
        {
            string meshUrl = AdminAuthService.Instance.GetMeshCentralUrl();
            if (string.IsNullOrEmpty(meshUrl))
            {
                meshUrl = "http://localhost:8086";
            }

            if (!string.IsNullOrEmpty(dev.MeshNodeId))
            {
                MeshAgentService.LaunchRemoteControl(meshUrl, dev.MeshNodeId);
                Log($"[REMOTE ACCESS] Admin launched Zero-Password remote control session to {dev.Hostname} (Node: {dev.MeshNodeId}).");
            }
            else
            {
                bool launched = MeshAgentService.LaunchMeshDashboard(meshUrl);
                if (!launched)
                {
                    MessageBox.Show($"MeshCentral URL not configured or unreachable.\nConnecting via AnyDesk fallback...", "Remote Control", MessageBoxButton.OK, MessageBoxImage.Information);
                    if (!string.IsNullOrEmpty(dev.AnyDeskId) && dev.AnyDeskId != "Not Configured")
                    {
                        FleetRegistryService.LaunchAnyDesk(dev.AnyDeskId);
                    }
                }
            }
        };
        col2.Children.Add(btnConnectMesh);

        var btnConnectAnyDesk = new Button
        {
            Content = "🚀 AnyDesk",
            FontSize = 10.5,
            FontWeight = FontWeights.Bold,
            Background = SvllRed,
            Foreground = Brushes.White,
            Padding = new Thickness(10, 6, 10, 6),
            Margin = new Thickness(0, 0, 6, 0),
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand
        };
        btnConnectAnyDesk.Click += (s, e) =>
        {
            if (string.IsNullOrEmpty(dev.AnyDeskId) || dev.AnyDeskId == "Not Configured")
            {
                MessageBox.Show($"AnyDesk is not configured on {dev.Hostname}.", "Missing AnyDesk ID", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            FleetRegistryService.LaunchAnyDesk(dev.AnyDeskId);
            Log($"[REMOTE ACCESS] Admin launched 1-click AnyDesk connection to {dev.Hostname} (ID: {dev.AnyDeskId}).");
        };
        col2.Children.Add(btnConnectAnyDesk);

        var btnConnectRdp = new Button
        {
            Content = "💻 RDP",
            FontSize = 10.5,
            Padding = new Thickness(8, 6, 8, 6),
            Margin = new Thickness(0, 0, 6, 0),
            Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
            BorderBrush = BorderMuted,
            Cursor = Cursors.Hand
        };
        btnConnectRdp.Click += (s, e) =>
        {
            FleetRegistryService.LaunchRdp(dev.IpAddress);
            Log($"[REMOTE ACCESS] Admin launched 1-click RDP session to {dev.Hostname} ({dev.IpAddress}).");
        };
        col2.Children.Add(btnConnectRdp);

        var btnShare = new Button
        {
            Content = "📁 C$ Files",
            FontSize = 10.5,
            Padding = new Thickness(8, 6, 8, 6),
            Margin = new Thickness(0, 0, 6, 0),
            Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
            BorderBrush = BorderMuted,
            Cursor = Cursors.Hand,
            ToolTip = $"Open \\\\{dev.IpAddress}\\c$ directly in Windows Explorer"
        };
        btnShare.Click += (s, e) =>
        {
            FleetRegistryService.LaunchFileExplorer(dev.IpAddress);
            Log($"[REMOTE ACCESS] Admin opened C$ share for {dev.Hostname} ({dev.IpAddress}).");
        };
        col2.Children.Add(btnShare);

        var btnShell = new Button
        {
            Content = "📡 Shell",
            FontSize = 10.5,
            Padding = new Thickness(8, 6, 8, 6),
            Margin = new Thickness(0, 0, 6, 0),
            Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
            BorderBrush = BorderMuted,
            Cursor = Cursors.Hand,
            ToolTip = $"Open Remote PowerShell session to {dev.Hostname}"
        };
        btnShell.Click += (s, e) =>
        {
            FleetRegistryService.LaunchRemoteShell(dev.IpAddress);
            Log($"[REMOTE ACCESS] Admin launched PowerShell remote session to {dev.Hostname} ({dev.IpAddress}).");
        };
        col2.Children.Add(btnShell);

        var btnPing = new Button
        {
            Content = "⚡ Ping",
            FontSize = 11,
            Padding = new Thickness(8, 6, 8, 6),
            Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
            BorderBrush = BorderMuted,
            Cursor = Cursors.Hand
        };
        btnPing.Click += async (s, e) =>
        {
            btnPing.IsEnabled = false;
            btnPing.Content = "⏳";
            long ms = await FleetRegistryService.PingDeviceAsync(dev.IpAddress);
            btnPing.IsEnabled = true;
            btnPing.Content = "⚡ Ping";
            if (ms >= 0)
            {
                MessageBox.Show($"Reply from {dev.Hostname} ({dev.IpAddress}): latency = {ms} ms 🟢", "Ping Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show($"Request timed out for {dev.Hostname} ({dev.IpAddress}) 🔴", "Ping Timeout", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        };
        col2.Children.Add(btnPing);

        Grid.SetColumn(col2, 2);
        grid.Children.Add(col2);

        card.Child = grid;
        return card;
    }

    private Border BuildMetricCard(string title, TextBlock valueBlock, string subtitle, int col)
    {
        var b = new Border
        {
            Background = BgCard,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(14, 10, 14, 10),
            Margin = new Thickness(col == 0 ? 0 : 4, 0, col == 3 ? 0 : 4, 0)
        };
        var p = new StackPanel();
        p.Children.Add(new TextBlock { Text = title, FontSize = 9.5, FontWeight = FontWeights.Bold, Foreground = TextSubtle });
        p.Children.Add(valueBlock);
        p.Children.Add(new TextBlock { Text = subtitle, FontSize = 9.5, Foreground = TextSubtle, Margin = new Thickness(0, 2, 0, 0) });
        b.Child = p;
        Grid.SetColumn(b, col);
        return b;
    }

    private UIElement BuildAdminLockScreen(string consoleName, Action onUnlocked)
    {
        var outerBorder = new Border
        {
            Background = BgApp,
            Padding = new Thickness(20)
        };

        var centerCard = new Border
        {
            Width = 440,
            Background = BgCard,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(14),
            Padding = new Thickness(24),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                BlurRadius = 20,
                ShadowDepth = 4,
                Opacity = 0.15,
                Color = Colors.Black
            }
        };

        var sp = new StackPanel();
        sp.Children.Add(new TextBlock
        {
            Text = "🔒",
            FontSize = 36,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 8)
        });

        sp.Children.Add(new TextBlock
        {
            Text = consoleName,
            FontSize = 16,
            FontWeight = FontWeights.ExtraBold,
            Foreground = TextDark,
            HorizontalAlignment = HorizontalAlignment.Center
        });

        sp.Children.Add(new TextBlock
        {
            Text = "Access to this centralized administration console is protected by master password. Standard user distribution does not have access to this screen.",
            FontSize = 11,
            Foreground = TextSubtle,
            TextWrapping = TextWrapping.Wrap,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 6, 0, 16)
        });

        var passBox = new PasswordBox
        {
            Height = 34,
            Padding = new Thickness(8, 4, 8, 4),
            FontSize = 13,
            BorderBrush = BorderMuted,
            Margin = new Thickness(0, 0, 0, 6)
        };
        sp.Children.Add(passBox);

        var lblErr = new TextBlock
        {
            Text = "",
            FontSize = 10.5,
            FontWeight = FontWeights.SemiBold,
            Foreground = SvllRed,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 10),
            Visibility = Visibility.Collapsed
        };
        sp.Children.Add(lblErr);

        var btnUnlock = new Button
        {
            Content = "👑 Unlock IT Admin Console",
            Height = 36,
            Background = SvllBlue,
            Foreground = Brushes.White,
            FontWeight = FontWeights.Bold,
            FontSize = 12,
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand
        };

        void DoUnlock()
        {
            if (AdminAuthService.Instance.UnlockAdmin(passBox.Password))
            {
                onUnlocked();
            }
            else
            {
                lblErr.Text = "❌ Invalid IT Admin Password. Access Denied.";
                lblErr.Visibility = Visibility.Visible;
                passBox.SelectAll();
                passBox.Focus();
            }
        }

        btnUnlock.Click += (s, e) => DoUnlock();
        passBox.KeyDown += (s, e) =>
        {
            if (e.Key == Key.Enter) DoUnlock();
        };

        sp.Children.Add(btnUnlock);

        var hintText = new TextBlock
        {
            Text = "Default master password: SVLL@Admin2026",
            FontSize = 9.5,
            Foreground = TextSubtle,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 10, 0, 0)
        };
        sp.Children.Add(hintText);

        centerCard.Child = sp;
        outerBorder.Child = centerCard;
        return outerBorder;
    }

    private void ShowServerSettingsDialog()
    {
        var dlg = new Window
        {
            Title = "⚙️ SVLL Server Settings (Relay & MeshCentral)",
            Width = 520,
            Height = 360,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            ResizeMode = ResizeMode.NoResize,
            Background = BgCard
        };

        var sp = new StackPanel { Margin = new Thickness(20) };

        sp.Children.Add(new TextBlock
        {
            Text = "⚙️ Enterprise Server Configuration",
            FontSize = 15,
            FontWeight = FontWeights.ExtraBold,
            Foreground = TextDark,
            Margin = new Thickness(0, 0, 0, 4)
        });
        sp.Children.Add(new TextBlock
        {
            Text = "Configure the central Relay Server and MeshCentral URLs for live telemetry reflection and remote control.",
            FontSize = 10.5,
            Foreground = TextSubtle,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 14)
        });

        // Relay Server URL
        sp.Children.Add(new TextBlock { Text = "📡 SVLL Relay Server URL (Telemetry & Tickets):", FontSize = 11, FontWeight = FontWeights.Bold, Foreground = TextDark });
        var txtRelay = new TextBox
        {
            Text = AdminAuthService.Instance.GetRelayServerUrl(),
            Height = 28,
            FontSize = 11,
            Padding = new Thickness(6, 4, 6, 4),
            Margin = new Thickness(0, 4, 0, 12),
            BorderBrush = BorderMuted
        };
        sp.Children.Add(txtRelay);

        // MeshCentral URL
        sp.Children.Add(new TextBlock { Text = "🌐 MeshCentral Server URL (Zero-Password Remote):", FontSize = 11, FontWeight = FontWeights.Bold, Foreground = TextDark });
        var txtMesh = new TextBox
        {
            Text = AdminAuthService.Instance.GetMeshCentralUrl(),
            Height = 28,
            FontSize = 11,
            Padding = new Thickness(6, 4, 6, 4),
            Margin = new Thickness(0, 4, 0, 16),
            BorderBrush = BorderMuted
        };
        sp.Children.Add(txtMesh);

        var lblStatus = new TextBlock { FontSize = 10.5, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 12) };
        sp.Children.Add(lblStatus);

        // Buttons
        var btnRow = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };

        var btnTest = new Button
        {
            Content = "⚡ Test Relay",
            FontSize = 11,
            Padding = new Thickness(14, 6, 14, 6),
            Margin = new Thickness(0, 0, 8, 0),
            Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
            BorderBrush = BorderMuted,
            Cursor = Cursors.Hand
        };
        btnTest.Click += async (s, e) =>
        {
            btnTest.IsEnabled = false;
            lblStatus.Text = "⏳ Testing connection...";
            lblStatus.Foreground = TextSubtle;
            try
            {
                using var http = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                string testUrl = txtRelay.Text.Trim().TrimEnd('/') + "/api/health";
                var res = await http.GetAsync(testUrl);
                if (res.IsSuccessStatusCode)
                {
                    lblStatus.Text = "🟢 Relay Server is reachable and healthy!";
                    lblStatus.Foreground = new SolidColorBrush(Color.FromRgb(22, 101, 52));
                }
                else
                {
                    lblStatus.Text = $"🔴 Relay Server returned error (HTTP {(int)res.StatusCode})";
                    lblStatus.Foreground = SvllRed;
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"🔴 Connection failed: {ex.Message}";
                lblStatus.Foreground = SvllRed;
            }
            finally
            {
                btnTest.IsEnabled = true;
            }
        };
        btnRow.Children.Add(btnTest);

        var btnSave = new Button
        {
            Content = "💾 Save & Apply",
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            Padding = new Thickness(16, 6, 16, 6),
            Background = SvllBlue,
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand
        };
        btnSave.Click += (s, e) =>
        {
            string relay = txtRelay.Text.Trim();
            string mesh = txtMesh.Text.Trim();
            AdminAuthService.Instance.UpdateRelaySettings(relay, mesh);
            CloudSyncService.Instance.Start(relay);
            dlg.Close();
            MessageBox.Show("Server settings saved and applied successfully!\nCloud sync re-initialized.", "Settings Saved", MessageBoxButton.OK, MessageBoxImage.Information);
        };
        btnRow.Children.Add(btnSave);

        sp.Children.Add(btnRow);
        dlg.Content = sp;
        dlg.ShowDialog();
    }
}
