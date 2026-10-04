using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Media;
using System.Net.Sockets;
using System.Printing;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace SVLL_IT_Workstation;

public partial class MainWindow
{
    // Logistics Printers State
    private ListView _listThermalPrinters = null!;
    private readonly List<ThermalPrinterItem> _thermalPrinters = new List<ThermalPrinterItem>();

    // Barcode Test Bench State
    private TextBox _txtBarcodeScannerInput = null!;
    private ListView _listBarcodeEvents = null!;
    private readonly List<BarcodeScanEvent> _barcodeEvents = new List<BarcodeScanEvent>();

    // WMS / ERP Endpoints State
    private ListView _listWmsEndpoints = null!;
    private readonly List<WmsEndpointItem> _wmsEndpoints = new List<WmsEndpointItem>();

    #region 1. Thermal Label Printers (Zebra / TSC / Honeywell)

    private UIElement BuildThermalPrintersView()
    {
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var root = new StackPanel { Margin = new Thickness(0, 0, 10, 0) };

        var card = new Border
        {
            Background = BgCard,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16),
            Margin = new Thickness(0, 0, 0, 14)
        };

        var sp = new StackPanel();
        sp.Children.Add(new TextBlock
        {
            Text = "LOGISTICS THERMAL LABEL PRINTER DIAGNOSTICS (ZEBRA / TSC)",
            FontSize = 13,
            FontWeight = FontWeights.Bold,
            Foreground = SvllBlue,
            Margin = new Thickness(0, 0, 0, 6)
        });
        sp.Children.Add(new TextBlock
        {
            Text = "Inspect shipping label printers, test raw ZPL/EPL port communication (RAW Port 9100), clear stuck spooler print queues, and send test print commands.",
            FontSize = 11,
            Foreground = TextSubtle,
            Margin = new Thickness(0, 0, 0, 12)
        });

        // Toolbar
        var toolBar = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 12) };

        var btnScanPrinters = new Button
        {
            Content = " Refresh Printers ",
            Height = 32,
            Background = SvllBlue,
            Foreground = Brushes.White,
            FontWeight = FontWeights.Bold,
            Padding = new Thickness(14, 0, 14, 0),
            Margin = new Thickness(0, 0, 8, 0),
            Cursor = Cursors.Hand
        };
        btnScanPrinters.Click += (s, e) => AuditThermalPrinters();
        toolBar.Children.Add(btnScanPrinters);

        var btnSendZpl = new Button
        {
            Content = "Send ZPL Test Label",
            Height = 32,
            Padding = new Thickness(12, 0, 12, 0),
            Margin = new Thickness(0, 0, 8, 0),
            Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
            BorderBrush = BorderMuted
        };
        btnSendZpl.Click += async (s, e) => await SendZplTestLabelAsync();
        toolBar.Children.Add(btnSendZpl);

        var btnPurgeQueue = new Button
        {
            Content = "Purge Stuck Spooler Buffer",
            Height = 32,
            Padding = new Thickness(12, 0, 12, 0),
            Margin = new Thickness(0, 0, 8, 0),
            Background = new SolidColorBrush(Color.FromRgb(254, 242, 242)),
            Foreground = SvllRed,
            BorderBrush = new SolidColorBrush(Color.FromRgb(254, 202, 202))
        };
        btnPurgeQueue.Click += async (s, e) =>
        {
            Log("\n[PRINTER] Purging locked label jobs from print spooler...");
            await ExecuteAsync("cmd.exe", "/c \"net stop spooler && del /Q /F /S %systemroot%\\System32\\Spool\\Printers\\* && net start spooler\"");
            AuditThermalPrinters();
            Log("[PRINTER] Spooler queue purged!");
        };
        toolBar.Children.Add(btnPurgeQueue);

        sp.Children.Add(toolBar);

        // Printer Table
        _listThermalPrinters = new ListView
        {
            Height = 240,
            FontSize = 11.5,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            Margin = new Thickness(0, 0, 0, 14)
        };
        var gv = new GridView();
        gv.Columns.Add(new GridViewColumn { Header = "Printer Name", Width = 260, DisplayMemberBinding = new Binding("Name") });
        gv.Columns.Add(new GridViewColumn { Header = "Port", Width = 110, DisplayMemberBinding = new Binding("PortName") });
        gv.Columns.Add(new GridViewColumn { Header = "Status", Width = 110, DisplayMemberBinding = new Binding("Status") });
        gv.Columns.Add(new GridViewColumn { Header = "Queue Jobs", Width = 90, DisplayMemberBinding = new Binding("JobsCount") });
        gv.Columns.Add(new GridViewColumn { Header = "Driver Model", Width = 280, DisplayMemberBinding = new Binding("DriverName") });
        _listThermalPrinters.View = gv;
        sp.Children.Add(_listThermalPrinters);

        // Raw Port 9100 Direct Network Printer Tester
        var portBox = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(12)
        };
        var pStack = new StackPanel();
        pStack.Children.Add(new TextBlock { Text = "RAW TCP PORT 9100 NETWORK PRINTER TESTER", FontSize = 11, FontWeight = FontWeights.Bold, Foreground = SvllBlue, Margin = new Thickness(0, 0, 0, 6) });
        var portGrid = new Grid();
        portGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });
        portGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        portGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var txtPrinterIp = new TextBox { Text = "192.168.1.200", Height = 28, Padding = new Thickness(6, 4, 6, 4), Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(txtPrinterIp, 0);
        portGrid.Children.Add(txtPrinterIp);

        var btnTestRawPort = new Button
        {
            Content = "Test Socket (Port 9100)",
            Height = 28,
            Padding = new Thickness(12, 0, 12, 0),
            Background = SvllBlue,
            Foreground = Brushes.White,
            FontWeight = FontWeights.SemiBold
        };
        btnTestRawPort.Click += async (s, e) =>
        {
            string ip = txtPrinterIp.Text.Trim();
            Log($"\n[PRINTER] Testing RAW TCP Port 9100 on {ip}...");
            try
            {
                using var tcp = new TcpClient();
                var sw = Stopwatch.StartNew();
                await tcp.ConnectAsync(ip, 9100);
                sw.Stop();
                Log($"[PRINTER] Success! Network label printer at {ip}:9100 responded in {sw.ElapsedMilliseconds}ms.");
                MessageBox.Show($"Zebra/TSC printer at {ip}:9100 is ONLINE ({sw.ElapsedMilliseconds}ms)!", "Socket Verified", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Log($"[PRINTER ERROR] Failed to connect to {ip}:9100 - {ex.Message}");
                MessageBox.Show($"Connection failed: {ex.Message}", "Port 9100 Unreachable", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        };
        Grid.SetColumn(btnTestRawPort, 1);
        portGrid.Children.Add(btnTestRawPort);
        pStack.Children.Add(portGrid);
        portBox.Child = pStack;
        sp.Children.Add(portBox);

        card.Child = sp;
        root.Children.Add(card);

        scroll.Content = root;
        AuditThermalPrinters();

        return scroll;
    }

    private void AuditThermalPrinters()
    {
        _thermalPrinters.Clear();
        try
        {
            var server = new LocalPrintServer();
            var queues = server.GetPrintQueues(new[] { EnumeratedPrintQueueTypes.Local, EnumeratedPrintQueueTypes.Connections });
            var defaultQueue = LocalPrintServer.GetDefaultPrintQueue();
            string defaultName = defaultQueue?.Name ?? string.Empty;

            foreach (var q in queues)
            {
                _thermalPrinters.Add(new ThermalPrinterItem
                {
                    Name = q.Name,
                    PortName = q.QueuePort?.Name ?? "USB",
                    DriverName = q.QueueDriver?.Name ?? "Generic",
                    Status = q.IsOffline ? "Offline" : (q.IsBusy ? "Busy" : "Ready"),
                    JobsCount = q.NumberOfJobs,
                    IsDefault = string.Equals(q.Name, defaultName, StringComparison.OrdinalIgnoreCase)
                });
            }
        }
        catch (Exception ex)
        {
            Log($"[PRINTER ERROR] Failed to query print server: {ex.Message}");
        }

        _listThermalPrinters.ItemsSource = null;
        _listThermalPrinters.ItemsSource = _thermalPrinters;
    }

    private async Task SendZplTestLabelAsync()
    {
        if (_listThermalPrinters.SelectedItem is not ThermalPrinterItem p)
        {
            MessageBox.Show("Please select a target label printer from the list.", "Selection Required", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        string zplData = "^XA^FO50,50^ADN,36,20^FDSHREE VASU LOGISTICS^FS^FO50,100^ADN,20,10^FDSVLL IT WORKSTATION VERIFIED^FS^FO50,140^BCN,60,Y,N,N^FD" + DateTime.Now.ToString("yyyyMMddHHmm") + "^FS^XZ";

        Log($"\n[PRINTER] Generating test label payload for '{p.Name}'...");
        string tempZpl = Path.Combine(Path.GetTempPath(), "svll_test_label.txt");
        File.WriteAllText(tempZpl, zplData);

        await ExecuteAsync("cmd.exe", $"/c \"copy /B \"{tempZpl}\" \"{p.Name}\"\"");
        Log($"[PRINTER] Test print command sent to '{p.Name}'.");
    }

    #endregion

    #region 2. Barcode Scanner Live Test Bench

    private UIElement BuildBarcodeScannerView()
    {
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var root = new StackPanel { Margin = new Thickness(0, 0, 10, 0) };

        var card = new Border
        {
            Background = BgCard,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16),
            Margin = new Thickness(0, 0, 0, 14)
        };

        var sp = new StackPanel();
        sp.Children.Add(new TextBlock
        {
            Text = "HANDHELD BARCODE SCANNER LIVE TEST BENCH",
            FontSize = 13,
            FontWeight = FontWeights.Bold,
            Foreground = SvllBlue,
            Margin = new Thickness(0, 0, 0, 6)
        });
        sp.Children.Add(new TextBlock
        {
            Text = "Live capture test bench for USB HID and Wireless handheld barcode scanners (Code128, QR Code, DataMatrix). Automatically records decode speed, text length, and character validation.",
            FontSize = 11,
            Foreground = TextSubtle,
            Margin = new Thickness(0, 0, 0, 12)
        });

        // Live Scanner Input Field
        var inputCard = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(239, 246, 255)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(191, 219, 254)),
            BorderThickness = new Thickness(1.5),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 0, 0, 14)
        };
        var inStack = new StackPanel();
        inStack.Children.Add(new TextBlock
        {
            Text = "SCANNER ACTIVE LISTENER (PULL SCANNER TRIGGER TO TEST):",
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            Foreground = SvllBlue,
            Margin = new Thickness(0, 0, 0, 4)
        });

        _txtBarcodeScannerInput = new TextBox
        {
            Height = 36,
            FontSize = 14,
            FontWeight = FontWeights.Bold,
            Background = Brushes.White,
            BorderBrush = BorderMuted,
            Padding = new Thickness(8, 6, 8, 6)
        };
        _txtBarcodeScannerInput.KeyDown += (s, e) =>
        {
            if (e.Key == Key.Enter)
            {
                string val = _txtBarcodeScannerInput.Text.Trim();
                if (!string.IsNullOrEmpty(val))
                {
                    _barcodeEvents.Insert(0, new BarcodeScanEvent { BarcodeValue = val });
                    _listBarcodeEvents.Items.Refresh();
                    _txtBarcodeScannerInput.Clear();

                    try { SystemSounds.Asterisk.Play(); } catch { }
                    Log($"[BARCODE] Successfully captured scan: [{val}] ({val.Length} chars)");
                }
            }
        };
        inStack.Children.Add(_txtBarcodeScannerInput);
        inputCard.Child = inStack;
        sp.Children.Add(inputCard);

        // History Toolbar
        var barTool = new DockPanel { Margin = new Thickness(0, 0, 0, 8) };
        barTool.Children.Add(new TextBlock
        {
            Text = "SCANNED BARCODE DECODE HISTORY",
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            Foreground = TextDark,
            VerticalAlignment = VerticalAlignment.Center
        });

        var btnGroup = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var btnCopyAll = new Button
        {
            Content = "Copy All Barcodes",
            Height = 26,
            Padding = new Thickness(10, 0, 10, 0),
            Margin = new Thickness(0, 0, 6, 0),
            Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
            BorderBrush = BorderMuted
        };
        btnCopyAll.Click += (s, e) =>
        {
            if (_barcodeEvents.Count > 0)
            {
                Clipboard.SetText(string.Join(Environment.NewLine, _barcodeEvents.Select(x => x.BarcodeValue)));
                MessageBox.Show($"Copied {_barcodeEvents.Count} barcode values to clipboard!", "Copied", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        };
        btnGroup.Children.Add(btnCopyAll);

        var btnClear = new Button
        {
            Content = "Clear History",
            Height = 26,
            Padding = new Thickness(10, 0, 10, 0),
            Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
            BorderBrush = BorderMuted
        };
        btnClear.Click += (s, e) =>
        {
            _barcodeEvents.Clear();
            _listBarcodeEvents.Items.Refresh();
        };
        btnGroup.Children.Add(btnClear);

        barTool.Children.Add(btnGroup);
        sp.Children.Add(barTool);

        // Barcode History ListView
        _listBarcodeEvents = new ListView
        {
            ItemsSource = _barcodeEvents,
            Height = 260,
            FontSize = 11.5,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1)
        };
        var gv = new GridView();
        gv.Columns.Add(new GridViewColumn { Header = "Timestamp", Width = 130, DisplayMemberBinding = new Binding("TimeFormatted") });
        gv.Columns.Add(new GridViewColumn { Header = "Decoded Value", Width = 340, DisplayMemberBinding = new Binding("BarcodeValue") });
        gv.Columns.Add(new GridViewColumn { Header = "Length", Width = 80, DisplayMemberBinding = new Binding("Length") });
        _listBarcodeEvents.View = gv;
        sp.Children.Add(_listBarcodeEvents);

        card.Child = sp;
        root.Children.Add(card);

        scroll.Content = root;

        // Auto-focus barcode input
        Loaded += (s, e) => _txtBarcodeScannerInput.Focus();

        return scroll;
    }

    #endregion

    #region 3. WMS & ERP Client Latency Tester

    private void InitializeWmsEndpoints()
    {
        if (_wmsEndpoints.Count > 0) return;
        _wmsEndpoints.AddRange(new[]
        {
            new WmsEndpointItem { ServiceName = "SVLL Central Warehouse WMS (MSSQL)", Host = "192.168.1.10", Port = 1433 },
            new WmsEndpointItem { ServiceName = "Corporate ERP Database (Oracle)", Host = "192.168.1.15", Port = 1521 },
            new WmsEndpointItem { ServiceName = "Logistics API Web Gateway", Host = "svll.in", Port = 443 },
            new WmsEndpointItem { ServiceName = "Active Directory Domain Controller (LDAP)", Host = "192.168.1.1", Port = 389 },
            new WmsEndpointItem { ServiceName = "Zebra Label Print Server (Raw Port)", Host = "192.168.1.200", Port = 9100 }
        });
    }

    private UIElement BuildWmsLatencyView()
    {
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var root = new StackPanel { Margin = new Thickness(0, 0, 10, 0) };

        var card = new Border
        {
            Background = BgCard,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16),
            Margin = new Thickness(0, 0, 0, 14)
        };

        var sp = new StackPanel();
        sp.Children.Add(new TextBlock
        {
            Text = "ENTERPRISE WMS & ERP DATABASE LATENCY TESTER",
            FontSize = 13,
            FontWeight = FontWeights.Bold,
            Foreground = SvllBlue,
            Margin = new Thickness(0, 0, 0, 6)
        });
        sp.Children.Add(new TextBlock
        {
            Text = "Diagnoses whether logistics system slowdowns are caused by branch Wi-Fi or backend ERP/database socket bottlenecks.",
            FontSize = 11,
            Foreground = TextSubtle,
            Margin = new Thickness(0, 0, 0, 12)
        });

        // Custom Endpoint Entry Toolbar
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
        addGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) }); // Name
        addGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) }); // Host/IP
        addGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Port
        addGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Add Button

        var txtNewName = new TextBox { Height = 28, Margin = new Thickness(0, 0, 8, 0), VerticalContentAlignment = VerticalAlignment.Center, Text = "Custom Warehouse Server" };
        var txtNewHost = new TextBox { Height = 28, Margin = new Thickness(0, 0, 8, 0), VerticalContentAlignment = VerticalAlignment.Center, Text = "192.168.1.50" };
        var txtNewPort = new TextBox { Height = 28, Margin = new Thickness(0, 0, 8, 0), VerticalContentAlignment = VerticalAlignment.Center, Text = "8080" };

        var btnAddEndpoint = new Button
        {
            Content = " + Add Target IP ",
            Height = 28,
            Padding = new Thickness(12, 0, 12, 0),
            Background = SvllBlue,
            Foreground = Brushes.White,
            FontWeight = FontWeights.Bold,
            Cursor = Cursors.Hand
        };
        btnAddEndpoint.Click += (s, e) =>
        {
            string host = txtNewHost.Text.Trim();
            string name = txtNewName.Text.Trim();
            if (string.IsNullOrEmpty(host))
            {
                MessageBox.Show("Please specify a valid IP address or hostname.", "Host Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!int.TryParse(txtNewPort.Text.Trim(), out int port) || port < 1 || port > 65535)
            {
                MessageBox.Show("Please enter a valid port between 1 and 65535.", "Invalid Port", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrEmpty(name)) name = $"Endpoint {host}:{port}";

            _wmsEndpoints.Add(new WmsEndpointItem { ServiceName = name, Host = host, Port = port, Status = "PENDING" });
            _listWmsEndpoints.Items.Refresh();
            Log($"[WMS] Added custom monitoring endpoint: {name} ({host}:{port})");
        };

        Grid.SetColumn(txtNewName, 0);
        Grid.SetColumn(txtNewHost, 1);
        Grid.SetColumn(txtNewPort, 2);
        Grid.SetColumn(btnAddEndpoint, 3);
        addGrid.Children.Add(txtNewName);
        addGrid.Children.Add(txtNewHost);
        addGrid.Children.Add(txtNewPort);
        addGrid.Children.Add(btnAddEndpoint);
        addBox.Child = addGrid;
        sp.Children.Add(addBox);

        // Action Toolbar
        var actionRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
        var btnTest = new Button
        {
            Content = " ▶ Run Socket Handshake Audit ",
            Height = 32,
            Background = SvllBlue,
            Foreground = Brushes.White,
            FontWeight = FontWeights.Bold,
            Padding = new Thickness(14, 0, 14, 0),
            Margin = new Thickness(0, 0, 8, 0),
            Cursor = Cursors.Hand
        };
        btnTest.Click += async (s, e) => await AuditWmsEndpointsAsync();
        actionRow.Children.Add(btnTest);

        var btnRemove = new Button
        {
            Content = "Remove Selected",
            Height = 32,
            Padding = new Thickness(12, 0, 12, 0),
            Margin = new Thickness(0, 0, 8, 0),
            Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
            BorderBrush = BorderMuted,
            Cursor = Cursors.Hand
        };
        btnRemove.Click += (s, e) =>
        {
            if (_listWmsEndpoints.SelectedItem is WmsEndpointItem item)
            {
                _wmsEndpoints.Remove(item);
                _listWmsEndpoints.Items.Refresh();
                Log($"[WMS] Removed endpoint: {item.ServiceName}");
            }
        };
        actionRow.Children.Add(btnRemove);

        var btnResetDefaults = new Button
        {
            Content = "Restore Defaults",
            Height = 32,
            Padding = new Thickness(12, 0, 12, 0),
            Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
            BorderBrush = BorderMuted,
            Cursor = Cursors.Hand
        };
        btnResetDefaults.Click += (s, e) =>
        {
            _wmsEndpoints.Clear();
            InitializeWmsEndpoints();
            _listWmsEndpoints.Items.Refresh();
            Log("[WMS] Restored default corporate logistics endpoints.");
        };
        actionRow.Children.Add(btnResetDefaults);

        sp.Children.Add(actionRow);

        if (_wmsEndpoints.Count == 0) InitializeWmsEndpoints();

        _listWmsEndpoints = new ListView
        {
            ItemsSource = _wmsEndpoints,
            Height = 300,
            FontSize = 11.5,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1)
        };
        var gv = new GridView();
        gv.Columns.Add(new GridViewColumn { Header = "Service Name", Width = 280, DisplayMemberBinding = new Binding("ServiceName") });
        gv.Columns.Add(new GridViewColumn { Header = "Target Host / IP", Width = 180, DisplayMemberBinding = new Binding("Host") });
        gv.Columns.Add(new GridViewColumn { Header = "Port", Width = 80, DisplayMemberBinding = new Binding("Port") });
        gv.Columns.Add(new GridViewColumn { Header = "Socket Status", Width = 160, DisplayMemberBinding = new Binding("StatusFormatted") });
        _listWmsEndpoints.View = gv;
        sp.Children.Add(_listWmsEndpoints);

        card.Child = sp;
        root.Children.Add(card);

        scroll.Content = root;
        return scroll;
    }

    private async Task AuditWmsEndpointsAsync()
    {
        Log("\n[WMS] Performing TCP socket handshake test on corporate database endpoints...");
        foreach (var item in _wmsEndpoints)
        {
            await Task.Run(async () =>
            {
                try
                {
                    using var tcp = new TcpClient();
                    var sw = Stopwatch.StartNew();
                    var connectTask = tcp.ConnectAsync(item.Host, item.Port);
                    var completedTask = await Task.WhenAny(connectTask, Task.Delay(1500));

                    if (completedTask == connectTask && tcp.Connected)
                    {
                        sw.Stop();
                        item.LatencyMs = sw.ElapsedMilliseconds;
                        item.Status = "ONLINE";
                    }
                    else
                    {
                        item.LatencyMs = -1;
                        item.Status = "TIMEOUT / BLOCKED";
                    }
                }
                catch
                {
                    item.LatencyMs = -1;
                    item.Status = "REFUSED / UNREACHABLE";
                }
            });
        }

        _listWmsEndpoints.Items.Refresh();
        Log("[WMS] Database socket audit completed.");
    }

    #endregion
}
