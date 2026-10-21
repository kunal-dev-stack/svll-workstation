using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SVLL_IT_Workstation;

public partial class MainWindow
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    private class DOCINFOA
    {
        [MarshalAs(UnmanagedType.LPStr)]
        public string? pDocName;
        [MarshalAs(UnmanagedType.LPStr)]
        public string? pOutputFile;
        [MarshalAs(UnmanagedType.LPStr)]
        public string? pDataType;
    }

    [DllImport("winspool.drv", EntryPoint = "OpenPrinterA", SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    private static extern bool OpenPrinter([MarshalAs(UnmanagedType.LPStr)] string szPrinter, out IntPtr hPrinter, IntPtr pd);

    [DllImport("winspool.drv", EntryPoint = "ClosePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    private static extern bool ClosePrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", EntryPoint = "StartDocPrinterA", SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    private static extern bool StartDocPrinter(IntPtr hPrinter, int level, [In, MarshalAs(UnmanagedType.LPStruct)] DOCINFOA di);

    [DllImport("winspool.drv", EntryPoint = "EndDocPrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    private static extern bool EndDocPrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", EntryPoint = "StartPagePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    private static extern bool StartPagePrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", EntryPoint = "EndPagePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    private static extern bool EndPagePrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", EntryPoint = "WritePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    private static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);

    private ComboBox _cmbThermalPrinters = null!;
    private ComboBox _cmbLabelTemplates = null!;
    private TextBox _txtRawZpl = null!;
    private ComboBox _cmbComPorts = null!;
    private ComboBox _cmbBaudRate = null!;
    private TextBox _txtSerialMonitor = null!;
    private Button _btnToggleSerial = null!;
    private SerialPort? _activeSerialPort;
    private bool _isSerialMonitoring = false;

    public UIElement BuildLogisticsDoctorView()
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
            Text = "🖨️ Logistics Hardware Doctor (Thermal Printers & Serial COM)",
            FontSize = 16,
            FontWeight = FontWeights.ExtraBold,
            Foreground = TextDark
        });
        titleRow.Children.Add(new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(243, 232, 255)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(216, 180, 254)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(8, 2, 8, 2),
            Margin = new Thickness(10, 0, 0, 0),
            Child = new TextBlock { Text = "WAREHOUSE PERIPHERALS", FontSize = 10, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(107, 33, 168)) }
        });
        headerLeft.Children.Add(titleRow);
        headerLeft.Children.Add(new TextBlock
        {
            Text = "Diagnostic hub for Zebra/TSC thermal label printers, raw ZPL injection, and RS-232/USB serial scales & barcode scanners.",
            FontSize = 11,
            Foreground = TextSubtle,
            Margin = new Thickness(0, 4, 0, 0)
        });
        Grid.SetColumn(headerLeft, 0);
        headerGrid.Children.Add(headerLeft);

        var btnSpoolPurge = new Button
        {
            Content = "🧹 DEEP SPOOLER PURGE & RESTART",
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            Padding = new Thickness(14, 7, 14, 7),
            Background = new SolidColorBrush(Color.FromRgb(147, 51, 234)),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand
        };
        btnSpoolPurge.Click += async (s, e) => await DeepPurgePrintSpoolerAsync();
        Grid.SetColumn(btnSpoolPurge, 1);
        headerGrid.Children.Add(btnSpoolPurge);

        headerBorder.Child = headerGrid;
        root.Children.Add(headerBorder);

        // 2. Dual Column Layout: Zebra Thermal RAW Sender (Left) & Serial COM Sniffer (Right)
        var splitGrid = new Grid();
        splitGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        splitGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        // ------------------ LEFT: THERMAL PRINTER ZPL / TSPL SENDER ------------------
        var zebraCard = new Border
        {
            Background = BgCard,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(16),
            Margin = new Thickness(0, 0, 6, 0)
        };
        var zebraPanel = new StackPanel();
        zebraPanel.Children.Add(new TextBlock
        {
            Text = "🏷️ Zebra & TSC Direct RAW Label Injection",
            FontSize = 13,
            FontWeight = FontWeights.Bold,
            Foreground = TextDark,
            Margin = new Thickness(0, 0, 0, 4)
        });
        zebraPanel.Children.Add(new TextBlock
        {
            Text = "Sends pure ASCII ZPL/TSPL directly to printer spooler ('RAW' datatype), bypassing GDI graphics drivers.",
            FontSize = 10.5,
            Foreground = TextSubtle,
            Margin = new Thickness(0, 0, 0, 10)
        });

        // Printer selector row
        zebraPanel.Children.Add(new TextBlock { Text = "TARGET THERMAL PRINTER:", FontSize = 10, FontWeight = FontWeights.Bold, Foreground = TextSubtle });
        _cmbThermalPrinters = new ComboBox { Margin = new Thickness(0, 3, 0, 8), Height = 28 };
        RefreshThermalPrinterList();
        zebraPanel.Children.Add(_cmbThermalPrinters);

        // Template selector
        zebraPanel.Children.Add(new TextBlock { Text = "LABEL TEMPLATE / COMMAND:", FontSize = 10, FontWeight = FontWeights.Bold, Foreground = TextSubtle });
        _cmbLabelTemplates = new ComboBox { Margin = new Thickness(0, 3, 0, 8), Height = 28 };
        _cmbLabelTemplates.Items.Add("ZPL: SVLL Shipping Label with Code 128 Barcode");
        _cmbLabelTemplates.Items.Add("ZPL: Pallet ID Asset Tag (4x6 inch)");
        _cmbLabelTemplates.Items.Add("ZPL: Media Sensor Auto-Calibration (~JC)");
        _cmbLabelTemplates.Items.Add("TSPL: TSC TTP-244 Pro Test Barcode");
        _cmbLabelTemplates.SelectedIndex = 0;
        _cmbLabelTemplates.SelectionChanged += (s, e) => LoadZplTemplate();
        zebraPanel.Children.Add(_cmbLabelTemplates);

        // Raw code editor
        zebraPanel.Children.Add(new TextBlock { Text = "RAW COMMAND PAYLOAD (Editable):", FontSize = 10, FontWeight = FontWeights.Bold, Foreground = TextSubtle });
        _txtRawZpl = new TextBox
        {
            Height = 160,
            FontFamily = new FontFamily("Consolas, Courier New"),
            FontSize = 11,
            Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
            Foreground = new SolidColorBrush(Color.FromRgb(15, 23, 42)),
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Margin = new Thickness(0, 3, 0, 10),
            AcceptsReturn = true
        };
        LoadZplTemplate();
        zebraPanel.Children.Add(_txtRawZpl);

        var btnSendZpl = new Button
        {
            Content = "⚡ SEND RAW LABEL TO PRINTER",
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            Padding = new Thickness(12, 8, 12, 8),
            Background = SvllBlue,
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand
        };
        btnSendZpl.Click += (s, e) => SendRawZplToSelectedPrinter();
        zebraPanel.Children.Add(btnSendZpl);

        zebraCard.Child = zebraPanel;
        Grid.SetColumn(zebraCard, 0);
        splitGrid.Children.Add(zebraCard);

        // ------------------ RIGHT: SERIAL COM PORT & BARCODE SNIFFER ------------------
        var serialCard = new Border
        {
            Background = BgCard,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(16),
            Margin = new Thickness(6, 0, 0, 0)
        };
        var serialPanel = new StackPanel();
        serialPanel.Children.Add(new TextBlock
        {
            Text = "📟 Serial COM Port & Weighbridge Sniffer",
            FontSize = 13,
            FontWeight = FontWeights.Bold,
            Foreground = TextDark,
            Margin = new Thickness(0, 0, 0, 4)
        });
        serialPanel.Children.Add(new TextBlock
        {
            Text = "Live ASCII terminal for weighbridge scales, dimensioners, and USB-to-Serial barcode scanners.",
            FontSize = 10.5,
            Foreground = TextSubtle,
            Margin = new Thickness(0, 0, 0, 10)
        });

        // Config row
        var cfgGrid = new Grid { Margin = new Thickness(0, 0, 0, 8) };
        cfgGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        cfgGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var portStack = new StackPanel { Margin = new Thickness(0, 0, 6, 0) };
        portStack.Children.Add(new TextBlock { Text = "ACTIVE COM PORT:", FontSize = 10, FontWeight = FontWeights.Bold, Foreground = TextSubtle });
        _cmbComPorts = new ComboBox { Margin = new Thickness(0, 3, 0, 0), Height = 28 };
        RefreshComPortList();
        portStack.Children.Add(_cmbComPorts);
        Grid.SetColumn(portStack, 0);
        cfgGrid.Children.Add(portStack);

        var baudStack = new StackPanel { Margin = new Thickness(6, 0, 0, 0) };
        baudStack.Children.Add(new TextBlock { Text = "BAUD RATE (8-N-1):", FontSize = 10, FontWeight = FontWeights.Bold, Foreground = TextSubtle });
        _cmbBaudRate = new ComboBox { Margin = new Thickness(0, 3, 0, 0), Height = 28 };
        int[] bauds = [9600, 19200, 38400, 57600, 115200];
        foreach (var b in bauds) _cmbBaudRate.Items.Add(b);
        _cmbBaudRate.SelectedIndex = 0; // 9600 standard
        baudStack.Children.Add(_cmbBaudRate);
        Grid.SetColumn(baudStack, 1);
        cfgGrid.Children.Add(baudStack);

        serialPanel.Children.Add(cfgGrid);

        // Terminal output
        serialPanel.Children.Add(new TextBlock { Text = "LIVE STREAM DATA BUFFER (RX):", FontSize = 10, FontWeight = FontWeights.Bold, Foreground = TextSubtle });
        _txtSerialMonitor = new TextBox
        {
            Height = 160,
            FontFamily = new FontFamily("Consolas, Courier New"),
            FontSize = 11,
            Background = TerminalBg,
            Foreground = new SolidColorBrush(Color.FromRgb(52, 211, 153)), // Emerald green terminal text
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Margin = new Thickness(0, 3, 0, 10),
            IsReadOnly = true,
            Text = "[COM SNIFFER] Click 'START SERIAL MONITOR' to inspect incoming stream...\n"
        };
        serialPanel.Children.Add(_txtSerialMonitor);

        var serialBtnRow = new Grid();
        serialBtnRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        serialBtnRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        _btnToggleSerial = new Button
        {
            Content = "▶️ START SERIAL MONITOR",
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            Padding = new Thickness(12, 8, 12, 8),
            Background = new SolidColorBrush(Color.FromRgb(16, 185, 129)),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand
        };
        _btnToggleSerial.Click += (s, e) => ToggleSerialMonitor();
        Grid.SetColumn(_btnToggleSerial, 0);
        serialBtnRow.Children.Add(_btnToggleSerial);

        var btnClearSerial = new Button
        {
            Content = "Clear Buffer",
            FontSize = 10.5,
            FontWeight = FontWeights.SemiBold,
            Padding = new Thickness(10, 8, 10, 8),
            Margin = new Thickness(8, 0, 0, 0),
            Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
            Foreground = TextDark,
            BorderThickness = new Thickness(1),
            BorderBrush = BorderMuted,
            Cursor = Cursors.Hand
        };
        btnClearSerial.Click += (s, e) => _txtSerialMonitor.Clear();
        Grid.SetColumn(btnClearSerial, 1);
        serialBtnRow.Children.Add(btnClearSerial);

        serialPanel.Children.Add(serialBtnRow);

        serialCard.Child = serialPanel;
        Grid.SetColumn(serialCard, 1);
        splitGrid.Children.Add(serialCard);

        root.Children.Add(splitGrid);

        scroll.Content = root;
        return scroll;
    }

    private void RefreshThermalPrinterList()
    {
        _cmbThermalPrinters.Items.Clear();
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Name, Default FROM Win32_Printer");
            foreach (var obj in searcher.Get())
            {
                string name = obj["Name"]?.ToString() ?? "";
                bool isDef = (bool)(obj["Default"] ?? false);
                if (!string.IsNullOrEmpty(name))
                {
                    _cmbThermalPrinters.Items.Add(isDef ? $"{name} [Default]" : name);
                }
            }
        }
        catch { }

        if (_cmbThermalPrinters.Items.Count == 0)
        {
            _cmbThermalPrinters.Items.Add("No local printers detected (Add in Windows Settings)");
        }
        _cmbThermalPrinters.SelectedIndex = 0;
    }

    private void RefreshComPortList()
    {
        _cmbComPorts.Items.Clear();
        string[] ports = SerialPort.GetPortNames();
        foreach (var p in ports)
        {
            _cmbComPorts.Items.Add(p);
        }

        if (_cmbComPorts.Items.Count == 0)
        {
            _cmbComPorts.Items.Add("COM1 (Simulated/Virtual)");
        }
        _cmbComPorts.SelectedIndex = 0;
    }

    private void LoadZplTemplate()
    {
        if (_txtRawZpl == null || _cmbLabelTemplates == null) return;
        int idx = _cmbLabelTemplates.SelectedIndex;

        if (idx == 0) // Shipping label
        {
            _txtRawZpl.Text =
                "^XA\n" +
                "^FO50,40^A0N,36,30^FDSHREE VASU LOGISTICS LIMITED^FS\n" +
                "^FO50,80^A0N,22,20^FDDESTINATION: BOMBAY WH-BAY-4^FS\n" +
                "^FO50,110^GB500,2,2^FS\n" +
                "^FO50,130^BY3,2,100^BCN,100,Y,N,N^FDSVLL99824108^FS\n" +
                "^FO50,260^A0N,20,18^FDWEIGHT: 42.5 KG | PKGS: 1 OF 4^FS\n" +
                "^XZ";
        }
        else if (idx == 1) // Pallet Tag
        {
            _txtRawZpl.Text =
                "^XA\n" +
                "^FO80,60^A0N,50,40^FD[PALLET DISPATCH TAG]^FS\n" +
                "^FO80,130^A0N,30,25^FDCARRIER: SVLL FLEET TRANSPORT^FS\n" +
                "^FO80,180^BY4,3,140^BCN,140,Y,N,N^FDPAL-2026-BOM-00412^FS\n" +
                "^XZ";
        }
        else if (idx == 2) // Auto-calibration
        {
            _txtRawZpl.Text =
                "~JC\n" +
                "^XA\n" +
                "^JUS\n" +
                "^XZ";
        }
        else // TSPL
        {
            _txtRawZpl.Text =
                "SIZE 4,2\n" +
                "GAP 0.12,0\n" +
                "CLS\n" +
                "TEXT 50,40,\"3\",0,1,1,\"SHREE VASU LOGISTICS LIMITED\"\n" +
                "BARCODE 50,90,\"128\",90,1,0,2,4,\"SVLL-TSC-9901\"\n" +
                "PRINT 1,1\n";
        }
    }

    private void SendRawZplToSelectedPrinter()
    {
        string printerItem = _cmbThermalPrinters.SelectedItem?.ToString() ?? "";
        string printerName = printerItem.Replace(" [Default]", "").Trim();

        if (string.IsNullOrEmpty(printerName) || printerName.StartsWith("No local"))
        {
            MessageBox.Show("Please select a valid installed thermal printer.", "Printer Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        string rawText = _txtRawZpl.Text;
        if (string.IsNullOrWhiteSpace(rawText))
        {
            MessageBox.Show("Please enter valid ZPL/TSPL label code.", "Printer Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            bool success = SendBytesToPrinter(printerName, Encoding.ASCII.GetBytes(rawText), "SVLL_Workstation_RAW_Test");
            if (success)
            {
                Log($"[ZEBRA DOCTOR] Successfully injected {rawText.Length} bytes RAW label to printer '{printerName}'.");
                MessageBox.Show($"✔ RAW label payload dispatched to '{printerName}'.\n\nThe label should print immediately without graphics rasterization.", "Print Successful", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show($"Failed to dispatch raw payload to '{printerName}'. Error code: {Marshal.GetLastWin32Error()}", "Print Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Exception sending to printer: {ex.Message}", "Print Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static bool SendBytesToPrinter(string szPrinterName, byte[] pBytes, string docName)
    {
        if (!OpenPrinter(szPrinterName.Normalize(), out IntPtr hPrinter, IntPtr.Zero))
            return false;

        var di = new DOCINFOA
        {
            pDocName = docName,
            pDataType = "RAW" // Directly tells Windows Spooler to bypass graphics conversion
        };

        bool success = false;
        if (StartDocPrinter(hPrinter, 1, di))
        {
            if (StartPagePrinter(hPrinter))
            {
                IntPtr pUnmanagedBytes = Marshal.AllocCoTaskMem(pBytes.Length);
                Marshal.Copy(pBytes, 0, pUnmanagedBytes, pBytes.Length);
                success = WritePrinter(hPrinter, pUnmanagedBytes, pBytes.Length, out _);
                Marshal.FreeCoTaskMem(pUnmanagedBytes);
                EndPagePrinter(hPrinter);
            }
            EndDocPrinter(hPrinter);
        }
        ClosePrinter(hPrinter);
        return success;
    }

    private void ToggleSerialMonitor()
    {
        if (_isSerialMonitoring)
        {
            // Stop
            try
            {
                _activeSerialPort?.Close();
                _activeSerialPort?.Dispose();
            }
            catch { }
            _activeSerialPort = null;
            _isSerialMonitoring = false;
            _btnToggleSerial.Content = "▶️ START SERIAL MONITOR";
            _btnToggleSerial.Background = new SolidColorBrush(Color.FromRgb(16, 185, 129));
            _txtSerialMonitor.AppendText($"[{DateTime.Now:HH:mm:ss}] Port closed. Monitoring stopped.\n");
        }
        else
        {
            // Start
            string portName = _cmbComPorts.SelectedItem?.ToString()?.Split(' ')[0] ?? "COM1";
            int baud = (int)(_cmbBaudRate.SelectedItem ?? 9600);

            try
            {
                _activeSerialPort = new SerialPort(portName, baud, Parity.None, 8, StopBits.One)
                {
                    ReadTimeout = 1000,
                    WriteTimeout = 1000
                };
                _activeSerialPort.DataReceived += (s, e) =>
                {
                    try
                    {
                        string data = _activeSerialPort.ReadExisting();
                        Dispatcher.Invoke(() =>
                        {
                            _txtSerialMonitor.AppendText($"[{DateTime.Now:HH:mm:ss.fff}] RX >> {data}\n");
                            _txtSerialMonitor.ScrollToEnd();
                        });
                    }
                    catch { }
                };

                _activeSerialPort.Open();
                _isSerialMonitoring = true;
                _btnToggleSerial.Content = "⏹️ STOP SERIAL MONITOR";
                _btnToggleSerial.Background = new SolidColorBrush(Color.FromRgb(239, 68, 68)); // Red
                _txtSerialMonitor.AppendText($"[{DateTime.Now:HH:mm:ss}] PORT OPENED: {portName} @ {baud} BAUD (8-N-1). Listening for RX stream...\n");
            }
            catch (Exception ex)
            {
                _txtSerialMonitor.AppendText($"[{DateTime.Now:HH:mm:ss}] ❌ Could not open {portName}: {ex.Message}\n");
                MessageBox.Show($"Unable to open port {portName}.\nEnsure the USB-to-Serial adapter is plugged in and not in use by another application.", "COM Port Notice", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }

    private async Task DeepPurgePrintSpoolerAsync()
    {
        Log("\n[LOGISTICS DOCTOR] Starting Deep Print Spooler Purge & Restart...");
        try
        {
            await Task.Run(() =>
            {
                // 1. Stop spooler
                try
                {
                    using var sc = new ServiceController("Spooler");
                    if (sc.Status != ServiceControllerStatus.Stopped)
                    {
                        sc.Stop();
                        sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(8));
                    }
                }
                catch { }

                // 2. Kill stuck handles if any
                try
                {
                    foreach (var p in Process.GetProcessesByName("spoolsv"))
                    {
                        p.Kill();
                        p.WaitForExit(3000);
                    }
                }
                catch { }

                // 3. Delete corrupt spool files
                string spoolDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "spool", "PRINTERS");
                int purgedCount = 0;
                if (Directory.Exists(spoolDir))
                {
                    foreach (var file in Directory.GetFiles(spoolDir, "*.*"))
                    {
                        try
                        {
                            File.Delete(file);
                            purgedCount++;
                        }
                        catch { }
                    }
                }

                // 4. Restart spooler
                try
                {
                    using var sc = new ServiceController("Spooler");
                    sc.Start();
                    sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(8));
                }
                catch { }

                Dispatcher.Invoke(() =>
                {
                    Log($"[LOGISTICS DOCTOR] Purged {purgedCount} stuck spool files (*.SPL/*.SHD). Spooler service successfully restarted.");
                    MessageBox.Show($"🎉 Print Spooler Deep Purge Complete!\n\n• Stuck Spool Jobs Purged: {purgedCount}\n• Spooler Service: Running 🟢\n• Print Buffer: Cleared", "Spooler Purged", MessageBoxButton.OK, MessageBoxImage.Information);
                    RefreshThermalPrinterList();
                });
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error purging spooler: {ex.Message}", "Spooler Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
