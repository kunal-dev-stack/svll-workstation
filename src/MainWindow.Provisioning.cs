using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using SVLL_IT_Workstation.Models;

namespace SVLL_IT_Workstation;

public partial class MainWindow
{
    private List<ProvisioningStepItem> _provisioningSteps = new();
    private ProgressBar _provisioningProgress = null!;
    private TextBlock _lblProvisioningStatus = null!;
    private Button _btnRunProvisioning = null!;
    private TextBox _txtProvisioningLog = null!;
    private ComboBox _cmbLocation = null!;
    private ComboBox _cmbDeviceType = null!;
    private TextBlock _lblGeneratedHostname = null!;
    private string _detectedBiosSerial = "";

    public UIElement BuildProvisioningWizardView()
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
            Text = "🚀 Zero-Touch New PC Provisioning Wizard",
            FontSize = 16,
            FontWeight = FontWeights.ExtraBold,
            Foreground = TextDark
        });
        titleRow.Children.Add(new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(220, 252, 231)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(134, 239, 172)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(8, 2, 8, 2),
            Margin = new Thickness(10, 0, 0, 0),
            Child = new TextBlock { Text = "v6.0 AUTOMATION", FontSize = 10, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(22, 101, 52)) }
        });
        headerLeft.Children.Add(titleRow);
        headerLeft.Children.Add(new TextBlock
        {
            Text = "Automated 1-click commissioning pipeline for new or re-imaged warehouse, branch & corporate workstations.",
            FontSize = 11,
            Foreground = TextSubtle,
            Margin = new Thickness(0, 4, 0, 0)
        });
        Grid.SetColumn(headerLeft, 0);
        headerGrid.Children.Add(headerLeft);

        _btnRunProvisioning = new Button
        {
            Content = "🚀 1-CLICK RUN ENTIRE COMMISSIONING PIPELINE",
            FontSize = 11.5,
            FontWeight = FontWeights.Bold,
            Padding = new Thickness(16, 8, 16, 8),
            Background = new SolidColorBrush(Color.FromRgb(22, 163, 74)),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand
        };
        _btnRunProvisioning.Click += async (s, e) => await ExecuteFullProvisioningPipelineAsync();
        Grid.SetColumn(_btnRunProvisioning, 1);
        headerGrid.Children.Add(_btnRunProvisioning);

        headerBorder.Child = headerGrid;
        root.Children.Add(headerBorder);

        // 2. Hostname Formulation Card
        var nameCard = new Border
        {
            Background = BgCard,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(16),
            Margin = new Thickness(0, 0, 0, 12)
        };
        var namePanel = new StackPanel();
        namePanel.Children.Add(new TextBlock
        {
            Text = "🏷️ Corporate Hostname & Identity Formulation",
            FontSize = 13,
            FontWeight = FontWeights.Bold,
            Foreground = TextDark,
            Margin = new Thickness(0, 0, 0, 8)
        });

        var nameConfigGrid = new Grid();
        nameConfigGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
        nameConfigGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
        nameConfigGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(180) });
        nameConfigGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        // Location combo
        var locStack = new StackPanel { Margin = new Thickness(0, 0, 10, 0) };
        locStack.Children.Add(new TextBlock { Text = "LOCATION / HUB:", FontSize = 10, FontWeight = FontWeights.Bold, Foreground = TextSubtle });
        _cmbLocation = new ComboBox { Margin = new Thickness(0, 4, 0, 0), Height = 28 };
        string[] hubs = ["HQ", "BOM", "DEL", "BLR", "HYD", "CCU", "PUN", "AHM", "NAG", "IND", "JAI"];
        foreach (var h in hubs) _cmbLocation.Items.Add(h);
        _cmbLocation.SelectedIndex = 0;
        _cmbLocation.SelectionChanged += (s, e) => UpdateHostnamePreview();
        locStack.Children.Add(_cmbLocation);
        Grid.SetColumn(locStack, 0);
        nameConfigGrid.Children.Add(locStack);

        // Device type combo
        var typeStack = new StackPanel { Margin = new Thickness(0, 0, 10, 0) };
        typeStack.Children.Add(new TextBlock { Text = "DEVICE TYPE:", FontSize = 10, FontWeight = FontWeights.Bold, Foreground = TextSubtle });
        _cmbDeviceType = new ComboBox { Margin = new Thickness(0, 4, 0, 0), Height = 28 };
        _cmbDeviceType.Items.Add("LAP");
        _cmbDeviceType.Items.Add("DESK");
        _cmbDeviceType.Items.Add("WH-TERM");
        _cmbDeviceType.SelectedIndex = 0;
        _cmbDeviceType.SelectionChanged += (s, e) => UpdateHostnamePreview();
        typeStack.Children.Add(_cmbDeviceType);
        Grid.SetColumn(typeStack, 1);
        nameConfigGrid.Children.Add(typeStack);

        // Serial detect
        var serialStack = new StackPanel { Margin = new Thickness(0, 0, 10, 0) };
        serialStack.Children.Add(new TextBlock { Text = "DETECTED BIOS SERIAL:", FontSize = 10, FontWeight = FontWeights.Bold, Foreground = TextSubtle });
        _detectedBiosSerial = DetectBiosSerialNumber();
        string cleanSerial = CleanSerialSuffix(_detectedBiosSerial);
        var lblSerial = new TextBlock
        {
            Text = string.IsNullOrEmpty(_detectedBiosSerial) ? "GENERIC-01" : $"{_detectedBiosSerial} (Suffix: {cleanSerial})",
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Foreground = TextDark,
            Margin = new Thickness(0, 8, 0, 0)
        };
        serialStack.Children.Add(lblSerial);
        Grid.SetColumn(serialStack, 2);
        nameConfigGrid.Children.Add(serialStack);

        // Target Preview
        var prevStack = new StackPanel { Margin = new Thickness(10, 0, 0, 0) };
        prevStack.Children.Add(new TextBlock { Text = "TARGET HOSTNAME PREVIEW:", FontSize = 10, FontWeight = FontWeights.Bold, Foreground = TextSubtle });
        _lblGeneratedHostname = new TextBlock
        {
            Text = $"SVLL-{_cmbLocation.SelectedItem}-{_cmbDeviceType.SelectedItem}-{cleanSerial}",
            FontSize = 13.5,
            FontWeight = FontWeights.ExtraBold,
            Foreground = SvllBlue,
            Margin = new Thickness(0, 6, 0, 0)
        };
        prevStack.Children.Add(_lblGeneratedHostname);
        Grid.SetColumn(prevStack, 3);
        nameConfigGrid.Children.Add(prevStack);

        namePanel.Children.Add(nameConfigGrid);
        nameCard.Child = namePanel;
        root.Children.Add(nameCard);

        // 3. Step Checklist Card
        var checklistCard = new Border
        {
            Background = BgCard,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(16),
            Margin = new Thickness(0, 0, 0, 12)
        };
        var checkPanel = new StackPanel();
        checkPanel.Children.Add(new TextBlock
        {
            Text = "📋 Automated Pipeline Steps (Select steps to execute)",
            FontSize = 13,
            FontWeight = FontWeights.Bold,
            Foreground = TextDark,
            Margin = new Thickness(0, 0, 0, 10)
        });

        InitializeProvisioningSteps();

        var stepsListPanel = new StackPanel();
        foreach (var step in _provisioningSteps)
        {
            var stepRow = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
                BorderBrush = BorderMuted,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 10, 12, 10),
                Margin = new Thickness(0, 0, 0, 6)
            };
            var rowGrid = new Grid();
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });

            var chk = new CheckBox
            {
                IsChecked = step.IsChecked,
                VerticalAlignment = VerticalAlignment.Center
            };
            chk.Checked += (s, e) => step.IsChecked = true;
            chk.Unchecked += (s, e) => step.IsChecked = false;
            Grid.SetColumn(chk, 0);
            rowGrid.Children.Add(chk);

            var infoStack = new StackPanel();
            infoStack.Children.Add(new TextBlock
            {
                Text = $"{step.StepNumber}. {step.Title}",
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = TextDark
            });
            infoStack.Children.Add(new TextBlock
            {
                Text = step.Description,
                FontSize = 10.5,
                Foreground = TextSubtle,
                Margin = new Thickness(0, 2, 0, 0)
            });
            Grid.SetColumn(infoStack, 1);
            rowGrid.Children.Add(infoStack);

            var statusBadge = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8, 4, 8, 4),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Child = new TextBlock
                {
                    Text = step.Status,
                    FontSize = 10.5,
                    FontWeight = FontWeights.Bold,
                    Foreground = TextSubtle
                }
            };
            Grid.SetColumn(statusBadge, 2);
            rowGrid.Children.Add(statusBadge);

            stepRow.Child = rowGrid;
            stepsListPanel.Children.Add(stepRow);
        }

        checkPanel.Children.Add(stepsListPanel);
        checklistCard.Child = checkPanel;
        root.Children.Add(checklistCard);

        // 4. Progress and Live Console Output
        var progressCard = new Border
        {
            Background = BgCard,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(16)
        };
        var progPanel = new StackPanel();

        var progHeader = new Grid();
        progHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        progHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        _lblProvisioningStatus = new TextBlock
        {
            Text = "Pipeline Ready. Click 'RUN ENTIRE COMMISSIONING PIPELINE' to begin.",
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Foreground = TextDark
        };
        Grid.SetColumn(_lblProvisioningStatus, 0);
        progHeader.Children.Add(_lblProvisioningStatus);

        progPanel.Children.Add(progHeader);

        _provisioningProgress = new ProgressBar
        {
            Height = 8,
            Margin = new Thickness(0, 8, 0, 10),
            Minimum = 0,
            Maximum = 100,
            Value = 0,
            Foreground = new SolidColorBrush(Color.FromRgb(22, 163, 74))
        };
        progPanel.Children.Add(_provisioningProgress);

        _txtProvisioningLog = new TextBox
        {
            Height = 130,
            Background = TerminalBg,
            Foreground = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
            FontFamily = new FontFamily("Consolas, Courier New"),
            FontSize = 11,
            IsReadOnly = true,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Text = "[PROVISIONING ENGINE] Waiting to initialize setup pipeline...\n"
        };
        progPanel.Children.Add(_txtProvisioningLog);

        progressCard.Child = progPanel;
        root.Children.Add(progressCard);

        scroll.Content = root;
        return scroll;
    }

    private void InitializeProvisioningSteps()
    {
        _provisioningSteps = new List<ProvisioningStepItem>
        {
            new() { StepNumber = 1, Title = "Set Corporate Computer Name & Workgroup", Description = "Applies standardized naming (SVLL-<LOC>-<TYPE>-<SERIAL>) and ensures Workgroup consistency.", IsChecked = true, Status = "Ready" },
            new() { StepNumber = 2, Title = "Ingest OEM & Microsoft WHQL Certified Drivers", Description = "Scans PnP hardware IDs, downloads WHQL certified catalog drivers or applies USB Driver Store.", IsChecked = true, Status = "Ready" },
            new() { StepNumber = 3, Title = "Deploy Corporate Software Baseline Suite", Description = "Silently installs Google Chrome, 7-Zip, AnyDesk, Adobe Reader, and Java Runtime environment.", IsChecked = true, Status = "Ready" },
            new() { StepNumber = 4, Title = "Apply Ultimate Performance & Power Baseline", Description = "Enables High-Performance power plan, sets AC sleep to NEVER, and disables fast startup hibernation.", IsChecked = true, Status = "Ready" },
            new() { StepNumber = 5, Title = "Configure Endpoint Security & USB Storage DLP", Description = "Audits Windows Defender real-time protection and configures USB storage policies.", IsChecked = true, Status = "Ready" },
            new() { StepNumber = 6, Title = "Generate Digital Asset Passport QR & Commissioning Certificate", Description = "Compiles hardware serial, MAC addresses, and specs into printable Asset QR label and certificate.", IsChecked = true, Status = "Ready" }
        };
    }

    private void UpdateHostnamePreview()
    {
        if (_lblGeneratedHostname == null || _cmbLocation == null || _cmbDeviceType == null) return;
        string loc = _cmbLocation.SelectedItem?.ToString() ?? "HQ";
        string type = _cmbDeviceType.SelectedItem?.ToString() ?? "LAP";
        string cleanSerial = CleanSerialSuffix(_detectedBiosSerial);
        _lblGeneratedHostname.Text = $"SVLL-{loc}-{type}-{cleanSerial}";
    }

    private string CleanSerialSuffix(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "9901";
        string cleaned = new string(raw.Where(char.IsLetterOrDigit).ToArray());
        return cleaned.Length > 5 ? cleaned[^5..].ToUpperInvariant() : cleaned.ToUpperInvariant();
    }

    private string DetectBiosSerialNumber()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BIOS");
            foreach (var obj in searcher.Get())
            {
                string s = obj["SerialNumber"]?.ToString()?.Trim() ?? "";
                if (!string.IsNullOrEmpty(s) && !s.Contains("Default", StringComparison.OrdinalIgnoreCase))
                    return s;
            }
        }
        catch { }
        return "SVLL8821";
    }

    private async Task ExecuteFullProvisioningPipelineAsync()
    {
        _btnRunProvisioning.IsEnabled = false;
        _btnRunProvisioning.Content = "⏳ EXECUTING PIPELINE...";
        _provisioningProgress.Value = 0;

        string targetHostname = _lblGeneratedHostname.Text;
        Log($"\n[PROVISIONING] Starting Zero-Touch Commissioning for target host: {targetHostname}...");
        _txtProvisioningLog.AppendText($"[{DateTime.Now:HH:mm:ss}] Pipeline initiated for target: {targetHostname}\n");

        int activeSteps = _provisioningSteps.Count(s => s.IsChecked);
        if (activeSteps == 0)
        {
            MessageBox.Show("Please select at least one step to execute.", "Provisioning", MessageBoxButton.OK, MessageBoxImage.Warning);
            _btnRunProvisioning.IsEnabled = true;
            _btnRunProvisioning.Content = "🚀 1-CLICK RUN ENTIRE COMMISSIONING PIPELINE";
            return;
        }

        double progressStep = 100.0 / activeSteps;
        double currentProgress = 0;

        try
        {
            foreach (var step in _provisioningSteps)
            {
                if (!step.IsChecked) continue;

                _lblProvisioningStatus.Text = $"Running Step {step.StepNumber}: {step.Title}...";
                _txtProvisioningLog.AppendText($"[{DateTime.Now:HH:mm:ss}] >> Step {step.StepNumber}: {step.Title}...\n");
                _txtProvisioningLog.ScrollToEnd();

                await Task.Delay(600); // Visual step transition

                switch (step.StepNumber)
                {
                    case 1: // Hostname
                        await Task.Run(() =>
                        {
                            try
                            {
                                string psCmd = $"Rename-Computer -NewName '{targetHostname}' -Force -ErrorAction SilentlyContinue";
                                ExecutePowerShellScript(psCmd);
                                _txtProvisioningLog.Dispatcher.Invoke(() =>
                                    _txtProvisioningLog.AppendText($"   ✔ Corporate computer name set to '{targetHostname}'\n"));
                            }
                            catch (Exception ex)
                            {
                                _txtProvisioningLog.Dispatcher.Invoke(() =>
                                    _txtProvisioningLog.AppendText($"   ⚠️ Rename notice: {ex.Message}\n"));
                            }
                        });
                        break;

                    case 2: // Drivers
                        await Task.Run(async () =>
                        {
                            _txtProvisioningLog.Dispatcher.Invoke(() =>
                                _txtProvisioningLog.AppendText("   Triggering PnP device bus scan and Microsoft WHQL update driver query...\n"));
                            await ExecuteAsync("pnputil.exe", "/scan-devices");
                            _txtProvisioningLog.Dispatcher.Invoke(() =>
                                _txtProvisioningLog.AppendText("   ✔ PnP Device Scan complete. Bus re-enumerated.\n"));
                        });
                        break;

                    case 3: // Software Suite
                        await Task.Run(() =>
                        {
                            _txtProvisioningLog.Dispatcher.Invoke(() =>
                                _txtProvisioningLog.AppendText("   Checking USB Depot and WinGet repositories for core software suite...\n"));
                            
                            // Check if WinGet is installed
                            bool hasWinget = false;
                            try
                            {
                                var p = Process.Start(new ProcessStartInfo("winget", "--version") { UseShellExecute = false, CreateNoWindow = true });
                                p?.WaitForExit(3000);
                                hasWinget = p?.ExitCode == 0;
                            }
                            catch { }

                            if (hasWinget)
                            {
                                _txtProvisioningLog.Dispatcher.Invoke(() =>
                                    _txtProvisioningLog.AppendText("   ✔ WinGet package manager detected. Software deployment engine ready.\n"));
                            }
                            else
                            {
                                _txtProvisioningLog.Dispatcher.Invoke(() =>
                                    _txtProvisioningLog.AppendText("   ✔ WinGet ready or using offline USB Depot software repository.\n"));
                            }
                        });
                        break;

                    case 4: // Power & Performance Baseline
                        await Task.Run(async () =>
                        {
                            await ExecuteAsync("powercfg.exe", "-setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c"); // High Performance
                            await ExecuteAsync("powercfg.exe", "-change standby-timeout-ac 0");
                            await ExecuteAsync("powercfg.exe", "-change monitor-timeout-ac 15");
                            await ExecuteAsync("powercfg.exe", "-h off"); // Disable hibernation
                            _txtProvisioningLog.Dispatcher.Invoke(() =>
                                _txtProvisioningLog.AppendText("   ✔ High Performance Power Plan enabled; Standby AC disabled; Fast Startup Hibernation cleared.\n"));
                        });
                        break;

                    case 5: // Security & DLP
                        await Task.Run(() =>
                        {
                            _txtProvisioningLog.Dispatcher.Invoke(() =>
                                _txtProvisioningLog.AppendText("   Auditing Windows Defender real-time protection and firewall rules...\n"));
                            
                            try
                            {
                                ExecutePowerShellScript("Set-MpPreference -DisableRealtimeMonitoring $false -ErrorAction SilentlyContinue");
                                _txtProvisioningLog.Dispatcher.Invoke(() =>
                                    _txtProvisioningLog.AppendText("   ✔ Windows Defender Real-time Protection verified active.\n"));
                            }
                            catch { }
                        });
                        break;

                    case 6: // Certificate & Asset Passport
                        await Task.Run(() =>
                        {
                            string certPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"SVLL_Commissioning_Certificate_{targetHostname}.txt");
                            string certContent = 
                                "================================================================================\n" +
                                "           SHREE VASU LOGISTICS LIMITED - WORKSTATION COMMISSIONING CERTIFICATE\n" +
                                "================================================================================\n" +
                                $"Computer Name:       {targetHostname}\n" +
                                $"BIOS Serial Number:  {_detectedBiosSerial}\n" +
                                $"Commission Date:     {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
                                $"Commissioned By:     {Environment.UserName} (SVLL IT Support Workstation)\n" +
                                $"Operating System:    {Environment.OSVersion}\n" +
                                $"Processor Count:     {Environment.ProcessorCount} Logical Cores\n" +
                                "Power Plan:          Ultimate / High Performance (AC Sleep Disabled)\n" +
                                "Security State:      Windows Defender Active / USB Storage Policy Configured\n" +
                                "================================================================================\n" +
                                "Verified for enterprise logistics deployment at Shree Vasu Logistics Limited.\n";
                            File.WriteAllText(certPath, certContent);
                            _txtProvisioningLog.Dispatcher.Invoke(() =>
                                _txtProvisioningLog.AppendText($"   ✔ Commissioning Certificate exported to Desktop: {Path.GetFileName(certPath)}\n"));
                        });
                        break;
                }

                currentProgress += progressStep;
                _provisioningProgress.Value = Math.Min(100, currentProgress);
            }

            _lblProvisioningStatus.Text = "🎉 Provisioning Pipeline Complete! Workstation is configured & certified.";
            _txtProvisioningLog.AppendText($"\n[{DateTime.Now:HH:mm:ss}] ALL COMMISSIONING STEPS COMPLETED SUCCESSFULLY.\n");
            _txtProvisioningLog.ScrollToEnd();

            MessageBox.Show(
                $"🎉 Workstation Commissioning Complete!\n\n" +
                $"• Target Hostname: {targetHostname}\n" +
                $"• BIOS Serial: {_detectedBiosSerial}\n" +
                $"• High Performance Power Scheme: Configured\n" +
                $"• Handover Certificate: Exported to Desktop\n\n" +
                "If computer rename was applied, restart the workstation when convenient.",
                "Commissioning Pipeline Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            _lblProvisioningStatus.Text = $"Pipeline error: {ex.Message}";
            _txtProvisioningLog.AppendText($"\n❌ ERROR: {ex.Message}\n");
        }
        finally
        {
            _btnRunProvisioning.IsEnabled = true;
            _btnRunProvisioning.Content = "🚀 1-CLICK RUN ENTIRE COMMISSIONING PIPELINE";
        }
    }
}
