using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using SVLL_IT_Workstation.Models;

namespace SVLL_IT_Workstation;

public partial class MainWindow
{
    private readonly List<TcpConnectionItem> _allTcpConnections = new();
    private ListView _lvConnections = null!;
    private TextBox _txtSocketFilter = null!;
    private Button _btnRefreshSockets = null!;
    private TextBlock _lblSocketStats = null!;

    public UIElement BuildSocketInspectorView()
    {
        var root = new Grid { Margin = new Thickness(10) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Filter Toolbar
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // ListView
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Bottom Action Bar

        // 1. Header Card
        var headerBorder = new Border
        {
            Background = BgCard,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(16),
            Margin = new Thickness(0, 0, 0, 10)
        };
        var headerGrid = new Grid();
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var headerLeft = new StackPanel();
        var titleRow = new StackPanel { Orientation = Orientation.Horizontal };
        titleRow.Children.Add(new TextBlock
        {
            Text = "📡 Active Ports & Bandwidth Hog Hunter",
            FontSize = 16,
            FontWeight = FontWeights.ExtraBold,
            Foreground = TextDark
        });
        titleRow.Children.Add(new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(238, 242, 255)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(199, 210, 254)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(8, 2, 8, 2),
            Margin = new Thickness(10, 0, 0, 0),
            Child = new TextBlock { Text = "NETWORK SOCKETS", FontSize = 10, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(67, 56, 202)) }
        });
        headerLeft.Children.Add(titleRow);
        headerLeft.Children.Add(new TextBlock
        {
            Text = "Inspect live TCP sockets, map ports to application binaries, and terminate processes saturating warehouse bandwidth.",
            FontSize = 11,
            Foreground = TextSubtle,
            Margin = new Thickness(0, 4, 0, 0)
        });
        Grid.SetColumn(headerLeft, 0);
        headerGrid.Children.Add(headerLeft);

        _btnRefreshSockets = new Button
        {
            Content = "🔄 REFRESH SOCKETS",
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            Padding = new Thickness(14, 7, 14, 7),
            Background = SvllBlue,
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand
        };
        _btnRefreshSockets.Click += async (s, e) => await ScanTcpSocketsAsync();
        Grid.SetColumn(_btnRefreshSockets, 1);
        headerGrid.Children.Add(_btnRefreshSockets);

        headerBorder.Child = headerGrid;
        Grid.SetRow(headerBorder, 0);
        root.Children.Add(headerBorder);

        // 2. Filter Toolbar
        var filterBorder = new Border
        {
            Background = BgCard,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(12, 8, 12, 8),
            Margin = new Thickness(0, 0, 0, 10)
        };
        var filterGrid = new Grid();
        filterGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        filterGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(280) });
        filterGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var lblFilter = new TextBlock
        {
            Text = "🔍 Filter Process / Port:",
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Foreground = TextDark,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 10, 0)
        };
        Grid.SetColumn(lblFilter, 0);
        filterGrid.Children.Add(lblFilter);

        _txtSocketFilter = new TextBox
        {
            Height = 28,
            FontSize = 11,
            VerticalContentAlignment = VerticalAlignment.Center,
            Padding = new Thickness(6, 0, 6, 0),
            BorderBrush = BorderMuted
        };
        _txtSocketFilter.TextChanged += (s, e) => ApplySocketFilter();
        Grid.SetColumn(_txtSocketFilter, 1);
        filterGrid.Children.Add(_txtSocketFilter);

        _lblSocketStats = new TextBlock
        {
            Text = "0 connections evaluated",
            FontSize = 11,
            Foreground = TextSubtle,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        Grid.SetColumn(_lblSocketStats, 2);
        filterGrid.Children.Add(_lblSocketStats);

        filterBorder.Child = filterGrid;
        Grid.SetRow(filterBorder, 1);
        root.Children.Add(filterBorder);

        // 3. ListView of Sockets
        _lvConnections = new ListView
        {
            Background = BgCard,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            FontSize = 11
        };

        var gridView = new GridView();
        gridView.Columns.Add(new GridViewColumn { Header = "Process", DisplayMemberBinding = new System.Windows.Data.Binding("ProcessName"), Width = 140 });
        gridView.Columns.Add(new GridViewColumn { Header = "PID", DisplayMemberBinding = new System.Windows.Data.Binding("ProcessId"), Width = 70 });
        gridView.Columns.Add(new GridViewColumn { Header = "Local Address", DisplayMemberBinding = new System.Windows.Data.Binding("LocalAddress"), Width = 130 });
        gridView.Columns.Add(new GridViewColumn { Header = "Local Port", DisplayMemberBinding = new System.Windows.Data.Binding("LocalPort"), Width = 80 });
        gridView.Columns.Add(new GridViewColumn { Header = "Remote Address", DisplayMemberBinding = new System.Windows.Data.Binding("RemoteAddress"), Width = 140 });
        gridView.Columns.Add(new GridViewColumn { Header = "Remote Port", DisplayMemberBinding = new System.Windows.Data.Binding("RemotePort"), Width = 90 });
        gridView.Columns.Add(new GridViewColumn { Header = "State", DisplayMemberBinding = new System.Windows.Data.Binding("State"), Width = 100 });
        gridView.Columns.Add(new GridViewColumn { Header = "Assessment", DisplayMemberBinding = new System.Windows.Data.Binding("BandwidthHint"), Width = 120 });
        _lvConnections.View = gridView;

        Grid.SetRow(_lvConnections, 2);
        root.Children.Add(_lvConnections);

        // 4. Bottom Action Bar
        var bottomBorder = new Border
        {
            Background = BgCard,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(12, 10, 12, 10),
            Margin = new Thickness(0, 10, 0, 0)
        };
        var btmGrid = new Grid();
        btmGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        btmGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var hintText = new TextBlock
        {
            Text = "💡 Tip: Select any suspicious background process saturating bandwidth and click 'Kill Process' to immediately liberate bandwidth.",
            FontSize = 10.5,
            Foreground = TextSubtle,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(hintText, 0);
        btmGrid.Children.Add(hintText);

        var btnKillProcess = new Button
        {
            Content = "🛑 KILL SELECTED PROCESS (TERMINATE HOG)",
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            Padding = new Thickness(14, 6, 14, 6),
            Background = SvllRed,
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand
        };
        btnKillProcess.Click += (s, e) => KillSelectedSocketProcess();
        Grid.SetColumn(btnKillProcess, 1);
        btmGrid.Children.Add(btnKillProcess);

        bottomBorder.Child = btmGrid;
        Grid.SetRow(bottomBorder, 3);
        root.Children.Add(bottomBorder);

        // Initial scan
        _ = ScanTcpSocketsAsync();

        return root;
    }

    private async Task ScanTcpSocketsAsync()
    {
        _btnRefreshSockets.IsEnabled = false;
        _btnRefreshSockets.Content = "⏳ SCANNING SOCKETS...";
        _allTcpConnections.Clear();

        await Task.Run(() =>
        {
            try
            {
                // Execute PowerShell Get-NetTCPConnection
                string script = "Get-NetTCPConnection | Select-Object -Property LocalAddress,LocalPort,RemoteAddress,RemotePort,State,OwningProcess | ConvertTo-Csv -NoTypeInformation";
                string csv = ExecutePowerShellScript(script);

                var lines = csv.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length > 1)
                {
                    // Cache process names
                    var procDict = new Dictionary<int, string>();
                    foreach (var p in Process.GetProcesses())
                    {
                        try { procDict[p.Id] = p.ProcessName; } catch { }
                    }

                    for (int i = 1; i < lines.Length; i++)
                    {
                        var parts = lines[i].Split(',').Select(p => p.Trim('"')).ToArray();
                        if (parts.Length >= 6)
                        {
                            string localAddr = parts[0];
                            int.TryParse(parts[1], out int localPort);
                            string remoteAddr = parts[2];
                            int.TryParse(parts[3], out int remotePort);
                            string state = parts[4];
                            int.TryParse(parts[5], out int pid);

                            string procName = procDict.TryGetValue(pid, out string? name) ? $"{name}.exe" : $"PID {pid}";
                            string hint = state == "Listen" ? "Listening Port" : (remotePort == 443 || remotePort == 80 ? "Web/Cloud Traffic" : "Active Tunnel");

                            _allTcpConnections.Add(new TcpConnectionItem
                            {
                                LocalAddress = localAddr,
                                LocalPort = localPort,
                                RemoteAddress = remoteAddr,
                                RemotePort = remotePort,
                                State = state,
                                ProcessId = pid,
                                ProcessName = procName,
                                BandwidthHint = hint
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"[SOCKET INSPECTOR] Error scanning TCP connections: {ex.Message}");
            }
        });

        ApplySocketFilter();

        _btnRefreshSockets.IsEnabled = true;
        _btnRefreshSockets.Content = "🔄 REFRESH SOCKETS";
    }

    private void ApplySocketFilter()
    {
        string filter = _txtSocketFilter?.Text?.Trim().ToLowerInvariant() ?? "";
        var filtered = string.IsNullOrEmpty(filter)
            ? _allTcpConnections
            : _allTcpConnections.Where(c =>
                c.ProcessName.ToLowerInvariant().Contains(filter) ||
                c.LocalPort.ToString().Contains(filter) ||
                c.RemotePort.ToString().Contains(filter) ||
                c.RemoteAddress.Contains(filter) ||
                c.State.ToLowerInvariant().Contains(filter)).ToList();

        _lvConnections.ItemsSource = filtered;
        _lblSocketStats.Text = $"Showing {filtered.Count} of {_allTcpConnections.Count} TCP connections";
    }

    private void KillSelectedSocketProcess()
    {
        if (_lvConnections.SelectedItem is not TcpConnectionItem selected)
        {
            MessageBox.Show("Please select an active socket connection from the list.", "Notice", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (selected.ProcessId <= 4)
        {
            MessageBox.Show("System kernel processes (PID <= 4) cannot be terminated.", "Protection", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var confirm = MessageBox.Show(
            $"Are you sure you want to terminate process '{selected.ProcessName}' (PID: {selected.ProcessId})?\n\nThis will immediately drop active network connections.",
            "Confirm Process Termination", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes) return;

        try
        {
            var p = Process.GetProcessById(selected.ProcessId);
            p.Kill();
            Log($"[SOCKET INSPECTOR] Terminated bandwidth hog process '{selected.ProcessName}' (PID: {selected.ProcessId}).");
            MessageBox.Show($"✔ Process '{selected.ProcessName}' terminated.", "Terminated", MessageBoxButton.OK, MessageBoxImage.Information);
            _ = ScanTcpSocketsAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not kill process: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
