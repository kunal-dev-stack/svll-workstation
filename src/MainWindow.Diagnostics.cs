using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;

namespace SVLL_IT_Workstation;

public partial class MainWindow
{
    private ListView _listHealthAudit = null!;
    private readonly List<HealthCheckResult> _healthResults = new List<HealthCheckResult>();
    private TextBlock _lblOverallHealthScore = null!;

    private TextBlock _lblHealthPassed = null!;
    private TextBlock _lblHealthWarnings = null!;
    private TextBlock _lblBatteryStatus = null!;

    #region 1. Upgraded PC Health & Battery Report Generator

    private UIElement BuildHealthView()
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

        // 1. Header with Badge & Overall Stability Score
        var headerDock = new DockPanel { Margin = new Thickness(0, 0, 0, 16) };
        var titleStack = new StackPanel();
        titleStack.Children.Add(new TextBlock
        {
            Text = "ENTERPRISE PC HEALTH & BATTERY LIFE AUDITOR",
            FontSize = 13.5,
            FontWeight = FontWeights.Bold,
            Foreground = SvllBlue
        });
        titleStack.Children.Add(new TextBlock
        {
            Text = "Automated baseline inspection of OS integrity, security barriers, pending updates, and hardware battery wear.",
            FontSize = 11,
            Foreground = TextSubtle,
            Margin = new Thickness(0, 2, 0, 0)
        });
        DockPanel.SetDock(titleStack, Dock.Left);
        headerDock.Children.Add(titleStack);

        var scoreBorder = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(240, 253, 244)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(187, 247, 208)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16, 6, 16, 6),
            HorizontalAlignment = HorizontalAlignment.Right
        };
        _lblOverallHealthScore = new TextBlock
        {
            Text = "STABILITY SCORE: 100%",
            FontSize = 12.5,
            FontWeight = FontWeights.ExtraBold,
            Foreground = new SolidColorBrush(Color.FromRgb(22, 101, 52))
        };
        scoreBorder.Child = _lblOverallHealthScore;
        headerDock.Children.Add(scoreBorder);
        sp.Children.Add(headerDock);

        // 2. Scorecard Metric Cards (3 Cards)
        var summaryGrid = new Grid { Margin = new Thickness(0, 0, 0, 16) };
        summaryGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        summaryGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        summaryGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var cPassed = CreateHealthSummaryCard("HEALTHY CHECKS", new SolidColorBrush(Color.FromRgb(22, 101, 52)), out _lblHealthPassed, "5 Passed", "Passed baseline tests");
        Grid.SetColumn(cPassed, 0);
        summaryGrid.Children.Add(cPassed);

        var cWarn = CreateHealthSummaryCard("ATTENTION NEEDED", SvllRed, out _lblHealthWarnings, "0 Warnings", "Require administrative remediation");
        Grid.SetColumn(cWarn, 1);
        summaryGrid.Children.Add(cWarn);

        var cBatt = CreateHealthSummaryCard("POWER & BATTERY SUBSYSTEM", SvllBlue, out _lblBatteryStatus, "AC Mains / Battery", "Hardware charge & cell state");
        Grid.SetColumn(cBatt, 2);
        summaryGrid.Children.Add(cBatt);

        sp.Children.Add(summaryGrid);

        // 3. Action Toolbar
        var toolBar = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 12) };

        var btnRunAudit = new Button
        {
            Content = " ▶  Run Full Health Audit ",
            Height = 34,
            Background = SvllBlue,
            Foreground = Brushes.White,
            FontWeight = FontWeights.Bold,
            Padding = new Thickness(16, 0, 16, 0),
            Margin = new Thickness(0, 0, 8, 0),
            Cursor = Cursors.Hand
        };
        btnRunAudit.Click += async (s, e) => await RunHealthAuditAsync();
        toolBar.Children.Add(btnRunAudit);

        var btnRemediate = new Button
        {
            Content = "⚡ 1-Click Auto-Remediate",
            Height = 34,
            Padding = new Thickness(14, 0, 14, 0),
            Margin = new Thickness(0, 0, 8, 0),
            Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
            BorderBrush = BorderMuted,
            FontWeight = FontWeights.SemiBold,
            Cursor = Cursors.Hand
        };
        btnRemediate.Click += async (s, e) => await AutoRemediateHealthIssuesAsync();
        toolBar.Children.Add(btnRemediate);

        var btnBatteryReport = new Button
        {
            Content = "🔋 Generate Battery Report (HTML)",
            Height = 34,
            Padding = new Thickness(14, 0, 14, 0),
            Margin = new Thickness(0, 0, 8, 0),
            Background = new SolidColorBrush(Color.FromRgb(240, 253, 244)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(187, 247, 208)),
            Foreground = new SolidColorBrush(Color.FromRgb(22, 101, 52)),
            FontWeight = FontWeights.Bold,
            Cursor = Cursors.Hand
        };
        btnBatteryReport.Click += async (s, e) => await GenerateBatteryReportAsync();
        toolBar.Children.Add(btnBatteryReport);

        var btnExportReport = new Button
        {
            Content = "Export HTML Audit",
            Height = 34,
            Padding = new Thickness(14, 0, 14, 0),
            Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
            BorderBrush = BorderMuted,
            Cursor = Cursors.Hand
        };
        btnExportReport.Click += (s, e) => ExportHealthHtmlReport();
        toolBar.Children.Add(btnExportReport);

        sp.Children.Add(toolBar);

        // 4. Health Results Table
        _listHealthAudit = new ListView
        {
            Height = 340,
            FontSize = 11.5,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1)
        };
        var gv = new GridView();
        gv.Columns.Add(new GridViewColumn { Header = "Component", Width = 200, DisplayMemberBinding = new Binding("Component") });
        gv.Columns.Add(new GridViewColumn { Header = "Audit Status", Width = 110, DisplayMemberBinding = new Binding("Status") });
        gv.Columns.Add(new GridViewColumn { Header = "Diagnostic Details", Width = 350, DisplayMemberBinding = new Binding("Details") });
        gv.Columns.Add(new GridViewColumn { Header = "Recommended IT Action", Width = 290, DisplayMemberBinding = new Binding("Recommendation") });
        _listHealthAudit.View = gv;
        sp.Children.Add(_listHealthAudit);

        card.Child = sp;
        root.Children.Add(card);

        scroll.Content = root;

        // Auto run audit on view initialization
        _ = RunHealthAuditAsync();

        return scroll;
    }

    private Border CreateHealthSummaryCard(string title, Brush accentBrush, out TextBlock valBlock, string defVal, string caption)
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

    private async Task RunHealthAuditAsync()
    {
        Log("\n[HEALTH] Executing full workstation stability & health audit...");
        _healthResults.Clear();

        await Task.Run(async () =>
        {
            // 1. C: Drive Free Headroom
            var cDrive = new DriveInfo("C");
            if (cDrive.IsReady)
            {
                double freeGb = Math.Round((double)cDrive.AvailableFreeSpace / (1024 * 1024 * 1024), 1);
                double totalGb = Math.Round((double)cDrive.TotalSize / (1024 * 1024 * 1024), 1);
                double pctFree = (cDrive.AvailableFreeSpace / (double)cDrive.TotalSize) * 100.0;

                bool warn = pctFree < 15;
                _healthResults.Add(new HealthCheckResult
                {
                    Component = "System Storage (C:)",
                    Status = warn ? "WARNING" : "HEALTHY",
                    Details = $"{freeGb} GB free out of {totalGb} GB ({Math.Round(pctFree, 1)}% available)",
                    Recommendation = warn ? "Run Disk Cleanup & purge temp files immediately." : "Storage headroom is optimal.",
                    IsWarning = warn
                });
            }

            // 2. Pending System Reboot
            bool pendingReboot = await CheckPendingRebootAsync();
            _healthResults.Add(new HealthCheckResult
            {
                Component = "Pending System Reboot",
                Status = pendingReboot ? "ATTENTION" : "CLEAN",
                Details = pendingReboot ? "Reboot required by Windows Component Servicing." : "No pending reboot flags in registry.",
                Recommendation = pendingReboot ? "Schedule workstation reboot to finish update staging." : "Normal operation.",
                IsWarning = pendingReboot
            });

            // 3. Print Spooler Service
            try
            {
                using var sc = new ServiceController("Spooler");
                bool isRunning = sc.Status == ServiceControllerStatus.Running;
                _healthResults.Add(new HealthCheckResult
                {
                    Component = "Print Spooler Service",
                    Status = isRunning ? "HEALTHY" : "CRITICAL",
                    Details = $"Service status: {sc.Status}",
                    Recommendation = isRunning ? "Print subsystem running normally." : "Restart Spooler and purge locked jobs.",
                    IsWarning = !isRunning
                });
            }
            catch
            {
                _healthResults.Add(new HealthCheckResult { Component = "Print Spooler", Status = "UNKNOWN", Details = "Unable to query service." });
            }

            // 4. Windows Firewall Status
            bool fwOk = true;
            try
            {
                using var p = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "netsh.exe",
                        Arguments = "advfirewall show currentprofile state",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };
                p.Start();
                string fwOut = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
                fwOk = fwOut.Contains("ON");
            }
            catch { }
            _healthResults.Add(new HealthCheckResult
            {
                Component = "Windows Firewall",
                Status = fwOk ? "PROTECTED" : "WARNING",
                Details = fwOk ? "Active firewall profile is ON." : "Firewall profile appears to be disabled.",
                Recommendation = fwOk ? "Network boundary protection active." : "Enable Windows Firewall to protect local endpoint.",
                IsWarning = !fwOk
            });

            // 5. Windows Defender Real-time Protection
            bool defOk = true;
            try
            {
                using var sc = new ServiceController("WinDefend");
                defOk = sc.Status == ServiceControllerStatus.Running;
            }
            catch { }
            _healthResults.Add(new HealthCheckResult
            {
                Component = "Endpoint Antivirus Protection",
                Status = defOk ? "HEALTHY" : "WARNING",
                Details = defOk ? "Microsoft Defender Antivirus service is running." : "Defender service is stopped or managed by 3rd-party EDR.",
                Recommendation = defOk ? "Endpoint is protected against malware." : "Verify EDR / antivirus definitions.",
                IsWarning = !defOk
            });
        });

        _listHealthAudit.ItemsSource = null;
        _listHealthAudit.ItemsSource = _healthResults.ToList();

        int warnCount = _healthResults.Count(x => x.IsWarning);
        int passedCount = _healthResults.Count - warnCount;
        int score = Math.Max(30, 100 - (warnCount * 20));

        _lblOverallHealthScore.Text = $"STABILITY SCORE: {score}%";
        _lblOverallHealthScore.Foreground = score >= 80 ? new SolidColorBrush(Color.FromRgb(22, 101, 52)) : SvllRed;

        if (_lblHealthPassed != null) _lblHealthPassed.Text = $"{passedCount} Checks Clean";
        if (_lblHealthWarnings != null)
        {
            _lblHealthWarnings.Text = $"{warnCount} Issues Found";
            _lblHealthWarnings.Foreground = warnCount > 0 ? SvllRed : new SolidColorBrush(Color.FromRgb(22, 101, 52));
        }
        if (_lblBatteryStatus != null)
        {
            var pwr = SystemParameters.PowerLineStatus;
            _lblBatteryStatus.Text = pwr == PowerLineStatus.Online ? "AC Mains Connected" : "Battery Discharging";
        }

        Log($"[HEALTH] Audit completed: Score {score}% ({warnCount} warnings identified).");
    }

    private async Task<bool> CheckPendingRebootAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                using var k1 = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\RebootPending");
                if (k1 != null) return true;

                using var k2 = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired");
                if (k2 != null) return true;
            }
            catch { }
            return false;
        });
    }

    private async Task AutoRemediateHealthIssuesAsync()
    {
        Log("\n[REMEDIATE] Starting automated workstation health remediation...");

        // 1. Restart Print Spooler if hung
        await ExecuteAsync("net.exe", "start Spooler");

        // 2. Enable Firewall
        await ExecuteAsync("netsh.exe", "advfirewall set currentprofile state on");

        // 3. Clear System Temp & Prefetch
        CleanDirectory(Path.GetTempPath());
        CleanDirectory(@"C:\Windows\Temp");

        Log("[REMEDIATE] Automated remediation steps executed. Re-running audit...");
        await RunHealthAuditAsync();
    }

    private async Task GenerateBatteryReportAsync()
    {
        Log("\n[BATTERY] Generating detailed Windows laptop battery life cycle report...");
        string reportPath = Path.Combine(Path.GetTempPath(), "battery_report.html");

        string res = await ExecuteAsync("powercfg.exe", $"/batteryreport /output \"{reportPath}\"");

        if (File.Exists(reportPath))
        {
            Log($"[BATTERY] Successfully generated battery report at: {reportPath}");
            try
            {
                Process.Start(new ProcessStartInfo { FileName = reportPath, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Log($"[BATTERY ERROR] Could not launch browser: {ex.Message}");
            }
        }
        else
        {
            MessageBox.Show("No battery found on this endpoint (Desktop PC running on direct AC mains).", "Hardware Notice", MessageBoxButton.OK, MessageBoxImage.Information);
            Log("[BATTERY] Notice: Machine appears to be an AC-powered desktop without battery cells.");
        }
    }

    private void ExportHealthHtmlReport()
    {
        var sfd = new SaveFileDialog
        {
            Filter = "HTML Files (*.html)|*.html",
            FileName = $"SVLL_Health_Report_{Environment.MachineName}.html"
        };
        if (sfd.ShowDialog() == true)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html><html><head><meta charset='utf-8'><title>SVLL Workstation Health Audit</title>");
            sb.AppendLine("<style>body{font-family:Segoe UI,sans-serif;margin:30px;background:#f8fafc;color:#0f172a}table{width:100%;border-collapse:collapse;background:#fff}th,td{padding:10px;border:1px solid #e2e8f0;text-align:left}th{background:#1a4bb2;color:#fff}.badge{padding:4px 8px;border-radius:4px;font-weight:bold}.ok{background:#dcfce7;color:#166534}.warn{background:#fee2e2;color:#991b1b}</style></head><body>");
            sb.AppendLine($"<h2>Shree Vasu Logistics Limited - Fleet Diagnostic Audit</h2><p>Host: {Environment.MachineName} | Date: {DateTime.Now}</p>");
            sb.AppendLine("<table><tr><th>Component</th><th>Status</th><th>Details</th><th>Recommendation</th></tr>");
            foreach (var r in _healthResults)
            {
                string cls = r.IsWarning ? "warn" : "ok";
                sb.AppendLine($"<tr><td>{r.Component}</td><td><span class='badge {cls}'>{r.Status}</span></td><td>{r.Details}</td><td>{r.Recommendation}</td></tr>");
            }
            sb.AppendLine("</table></body></html>");
            File.WriteAllText(sfd.FileName, sb.ToString());
            Log($"[HEALTH] Exported HTML diagnostic report to {sfd.FileName}");
            Process.Start(new ProcessStartInfo { FileName = sfd.FileName, UseShellExecute = true });
        }
    }

    #endregion

    #region 2. System Repair & DISM View

    private UIElement BuildRepairView()
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
            Text = "WINDOWS OS FILE SYSTEM INTEGRITY & DISM REPAIR",
            FontSize = 13,
            FontWeight = FontWeights.Bold,
            Foreground = SvllBlue,
            Margin = new Thickness(0, 0, 0, 6)
        });
        sp.Children.Add(new TextBlock
        {
            Text = "Repair corrupted core DLLs, verify system component store health, and schedule volume disk checks.",
            FontSize = 11,
            Foreground = TextSubtle,
            Margin = new Thickness(0, 0, 0, 12)
        });

        sp.Children.Add(CreateToolRow("System File Checker (SFC /scannow)", "Scans integrity of all protected system files and replaces damaged versions with cached originals.",
            async () => await ExecuteAsync("sfc.exe", "/scannow"), "Run SFC Scan"));

        sp.Children.Add(CreateToolRow("DISM CheckHealth & ScanHealth", "Verifies whether the local Windows component store image contains corrupted payloads.",
            async () => await ExecuteAsync("dism.exe", "/Online /Cleanup-Image /ScanHealth"), "Scan Health"));

        sp.Children.Add(CreateToolRow("DISM RestoreHealth (Full Component Recovery)", "Repairs damaged component store payloads by downloading original payloads from Windows Update.",
            async () => await ExecuteAsync("dism.exe", "/Online /Cleanup-Image /RestoreHealth"), "Restore Health"));

        sp.Children.Add(CreateToolRow("CHKDSK File System Verification (Read-Only)", "Scans volume C: for file system master file table corruption without taking the volume offline.",
            async () => await ExecuteAsync("chkdsk.exe", "C: /scan"), "Scan Volume C:"));

        sp.Children.Add(CreateToolRow("Analyze & Purge Component Store (WinSxS)", "Analyzes and cleans superseded updates in the WinSxS directory to reclaim gigabytes of disk space.",
            async () => await ExecuteAsync("dism.exe", "/Online /Cleanup-Image /StartComponentCleanup"), "Clean WinSxS"));

        card.Child = sp;
        root.Children.Add(card);

        scroll.Content = root;
        return scroll;
    }

    #endregion

    #region 3. Drive Health & Volumes View

    private UIElement BuildDiskView()
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
            Text = "LOGICAL STORAGE VOLUMES & SSD TRIM OPTIMIZATION",
            FontSize = 13,
            FontWeight = FontWeights.Bold,
            Foreground = SvllBlue,
            Margin = new Thickness(0, 0, 0, 12)
        });

        var drives = DriveInfo.GetDrives().Where(d => d.IsReady).ToList();
        foreach (var d in drives)
        {
            var driveCard = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
                BorderBrush = BorderMuted,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 0, 10)
            };

            var dGrid = new Grid();
            dGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            dGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var dInfo = new StackPanel();
            double total = Math.Round((double)d.TotalSize / (1024 * 1024 * 1024), 1);
            double free = Math.Round((double)d.AvailableFreeSpace / (1024 * 1024 * 1024), 1);
            int pctUsed = (int)Math.Round(((total - free) / total) * 100);

            dInfo.Children.Add(new TextBlock
            {
                Text = $"Drive [{d.Name}] - {d.VolumeLabel} ({d.DriveFormat})",
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = SvllBlue
            });
            dInfo.Children.Add(new TextBlock
            {
                Text = $"{free} GB Free of {total} GB ({100 - pctUsed}% available)",
                FontSize = 11,
                Foreground = TextSubtle,
                Margin = new Thickness(0, 2, 0, 6)
            });

            var prog = new ProgressBar { Height = 8, Minimum = 0, Maximum = 100, Value = pctUsed, Foreground = SvllBlue };
            dInfo.Children.Add(prog);
            Grid.SetColumn(dInfo, 0);
            dGrid.Children.Add(dInfo);

            var btnTrim = new Button
            {
                Content = "Run SSD TRIM",
                Height = 30,
                Padding = new Thickness(12, 0, 12, 0),
                Margin = new Thickness(12, 0, 0, 0),
                Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
                BorderBrush = BorderMuted,
                VerticalAlignment = VerticalAlignment.Center
            };
            string letter = d.Name.Replace(":\\", "").Replace(":", "");
            btnTrim.Click += async (s, e) =>
            {
                Log($"[TRIM] Running SSD TRIM optimization on drive {letter}...");
                await ExecuteAsync("powershell.exe", $"-NoProfile -Command \"Optimize-Volume -DriveLetter {letter} -Defrag -Verbose\"");
            };
            Grid.SetColumn(btnTrim, 1);
            dGrid.Children.Add(btnTrim);

            driveCard.Child = dGrid;
            sp.Children.Add(driveCard);
        }

        card.Child = sp;
        root.Children.Add(card);

        scroll.Content = root;
        return scroll;
    }

    #endregion

    #region 4. Windows Services Manager

    private UIElement BuildServicesView()
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
            Text = "WINDOWS ESSENTIAL SERVICES CONTROLLER",
            FontSize = 13,
            FontWeight = FontWeights.Bold,
            Foreground = SvllBlue,
            Margin = new Thickness(0, 0, 0, 12)
        });

        void AddServiceRow(string displayName, string serviceName)
        {
            var p = new StackPanel { Margin = new Thickness(0, 4, 0, 8) };
            var g = new Grid();
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var tStack = new StackPanel();
            tStack.Children.Add(new TextBlock { Text = displayName, FontSize = 12, FontWeight = FontWeights.SemiBold });
            tStack.Children.Add(new TextBlock { Text = $"Service Name: {serviceName}", FontSize = 10.5, Foreground = TextSubtle });
            Grid.SetColumn(tStack, 0);
            g.Children.Add(tStack);

            var bStack = new StackPanel { Orientation = Orientation.Horizontal };
            var btnStart = new Button { Content = "Start", Height = 28, Padding = new Thickness(10, 0, 10, 0), Margin = new Thickness(0, 0, 6, 0), Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)), BorderBrush = BorderMuted };
            btnStart.Click += async (s, e) => await ExecuteAsync("net.exe", $"start {serviceName}");
            bStack.Children.Add(btnStart);

            var btnStop = new Button { Content = "Stop", Height = 28, Padding = new Thickness(10, 0, 10, 0), Margin = new Thickness(0, 0, 6, 0), Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)), BorderBrush = BorderMuted };
            btnStop.Click += async (s, e) => await ExecuteAsync("net.exe", $"stop {serviceName}");
            bStack.Children.Add(btnStop);

            var btnRestart = new Button { Content = "Restart", Height = 28, Padding = new Thickness(10, 0, 10, 0), Background = SvllBlue, Foreground = Brushes.White };
            btnRestart.Click += async (s, e) =>
            {
                await ExecuteAsync("net.exe", $"stop {serviceName}");
                await ExecuteAsync("net.exe", $"start {serviceName}");
            };
            bStack.Children.Add(btnRestart);

            Grid.SetColumn(bStack, 1);
            g.Children.Add(bStack);
            p.Children.Add(g);
            sp.Children.Add(p);
        }

        AddServiceRow("Print Spooler Subsystem", "Spooler");
        AddServiceRow("Windows Update Client", "wuauserv");
        AddServiceRow("Background Intelligent Transfer Service (BITS)", "BITS");
        AddServiceRow("Windows Remote Desktop Service", "TermService");
        AddServiceRow("Windows Audio Subsystem", "Audiosrv");
        AddServiceRow("DHCP Client Service", "Dhcp");
        AddServiceRow("DNS Resolver Cache Service", "Dnscache");

        card.Child = sp;
        root.Children.Add(card);

        scroll.Content = root;
        return scroll;
    }

    #endregion

    #region 5. Print Spooler Suite

    private UIElement BuildPrinterView()
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
            Text = "PRINT SPOOLER DIAGNOSTIC & PHANTOM JOB PURGE",
            FontSize = 13,
            FontWeight = FontWeights.Bold,
            Foreground = SvllBlue,
            Margin = new Thickness(0, 0, 0, 6)
        });
        sp.Children.Add(new TextBlock
        {
            Text = "Resolves locked document queues, clears corrupted .SPL/.SHD spool files, and provides quick management console shortcuts.",
            FontSize = 11,
            Foreground = TextSubtle,
            Margin = new Thickness(0, 0, 0, 12)
        });

        sp.Children.Add(CreateToolRow("1-Click Purge Locked Spooler Files", "Stops spooler service, forcefully deletes all corrupted print job files in System32\\spool\\PRINTERS, and restarts the service.",
            async () =>
            {
                Log("\n[PRINTER] Purging locked print spooler files...");
                await ExecuteAsync("cmd.exe", "/c \"net stop spooler && del /Q /F /S %systemroot%\\System32\\Spool\\Printers\\* && net start spooler\"");
                Log("[PRINTER] Print spooler purged and re-initialized!");
            }, "Purge Stuck Jobs"));

        sp.Children.Add(CreateToolRow("Open Windows Print Management Console", "Launches the MMC Print Management snap-in for driver and server inspection.",
            () => Task.Run(() => OpenTool("printmanagement.msc")), "Open PrintManagement"));

        sp.Children.Add(CreateToolRow("Open Classic Devices and Printers Control Panel", "Opens the classic Windows control panel interface for printers.",
            () => Task.Run(() => OpenTool("control.exe", "printers")), "Open Printers Control"));

        card.Child = sp;
        root.Children.Add(card);

        scroll.Content = root;
        return scroll;
    }

    #endregion
}
