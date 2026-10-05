using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.ServiceProcess;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;

namespace SVLL_IT_Workstation;

public partial class MainWindow
{
    // ==========================================
    // APP UNINSTALLER & FORCE PURGE STATE
    // ==========================================
    private ListView _listInstalledApps = null!;
    private readonly List<InstalledAppItem> _allInstalledApps = new List<InstalledAppItem>();
    private TextBox _txtAppSearch = null!;
    private TextBlock _lblAppCount = null!;
    private ProgressBar _progUninstall = null!;
    private TextBlock _lblUninstallStatus = null!;
    private Button _btnSilentUninstall = null!;
    private Button _btnForcePurge = null!;
    private string _activeAppFilter = "ALL";

    private UIElement BuildAppUninstallerView()
    {
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var root = new StackPanel { Margin = new Thickness(0, 0, 10, 0) };

        // ------------------ 1. Header Card ------------------
        var headerCard = new Border
        {
            Background = BgCard,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16, 14, 16, 14),
            Margin = new Thickness(0, 0, 0, 12)
        };

        var headerGrid = new Grid();
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var titleStack = new StackPanel();
        var mainTitle = new TextBlock
        {
            Text = "🗑️ APP UNINSTALLER & FORCE PURGE (PASSWORD-BYPASS REMOVAL)",
            FontSize = 13.5,
            FontWeight = FontWeights.Bold,
            Foreground = SvllBlue
        };
        var subTitle = new TextBlock
        {
            Text = "Elevated Administrative Removal: Silently Uninstall Any Program Without Windows UAC Password Prompts, or Force-Purge Password-Protected & Stubborn Software",
            FontSize = 11,
            Foreground = TextSubtle,
            Margin = new Thickness(0, 2, 0, 0)
        };
        titleStack.Children.Add(mainTitle);
        titleStack.Children.Add(subTitle);
        headerGrid.Children.Add(titleStack);

        var topActions = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        var btnRefresh = new Button
        {
            Content = "🔄 Refresh Apps",
            FontSize = 11,
            Padding = new Thickness(10, 5, 10, 5),
            Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
            BorderBrush = BorderMuted,
            Cursor = Cursors.Hand
        };
        btnRefresh.Click += async (s, e) => await ScanInstalledApplicationsAsync();
        topActions.Children.Add(btnRefresh);

        Grid.SetColumn(topActions, 1);
        headerGrid.Children.Add(topActions);
        headerCard.Child = headerGrid;
        root.Children.Add(headerCard);

        // ------------------ 2. Explanatory Notice Banner ------------------
        var noticeCard = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(254, 252, 232)), // Amber light
            BorderBrush = new SolidColorBrush(Color.FromRgb(254, 240, 138)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(12, 8, 12, 8),
            Margin = new Thickness(0, 0, 0, 12)
        };
        var noticeText = new TextBlock
        {
            Text = "💡 HOW PASSWORD-FREE UNINSTALLATION WORKS:\n" +
                   "• Standard Silent Uninstall: Executes the software uninstaller in this workstation's elevated Administrator context, preventing Windows UAC from asking for an administrator password.\n" +
                   "• Force Purge & Bypass: If an application has an internal uninstall password, or is corrupt/locked, Force Purge terminates its processes, deletes its services, takes filesystem ownership, and obliterates all files and registry keys with zero passwords required.",
            FontSize = 10.5,
            Foreground = new SolidColorBrush(Color.FromRgb(133, 77, 14)),
            LineHeight = 16
        };
        noticeCard.Child = noticeText;
        root.Children.Add(noticeCard);

        // ------------------ 3. Search & Category Filter Strip ------------------
        var mainCard = new Border
        {
            Background = BgCard,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16),
            Margin = new Thickness(0, 0, 0, 14)
        };

        var mainContent = new StackPanel();

        var filterStrip = new DockPanel { Margin = new Thickness(0, 0, 0, 10) };

        var btnSelectAll = new Button
        {
            Content = "☑️ Select All",
            FontSize = 10.5,
            Padding = new Thickness(8, 4, 8, 4),
            Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
            BorderBrush = BorderMuted,
            Margin = new Thickness(0, 0, 4, 0)
        };
        btnSelectAll.Click += (s, e) => SetAllAppItemsSelection(true);
        filterStrip.Children.Add(btnSelectAll);

        var btnDeselectAll = new Button
        {
            Content = "⬜ Deselect All",
            FontSize = 10.5,
            Padding = new Thickness(8, 4, 8, 4),
            Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
            BorderBrush = BorderMuted,
            Margin = new Thickness(0, 0, 12, 0)
        };
        btnDeselectAll.Click += (s, e) => SetAllAppItemsSelection(false);
        filterStrip.Children.Add(btnDeselectAll);

        _lblAppCount = new TextBlock
        {
            Text = "Scanning registry for installed software...",
            FontSize = 11,
            Foreground = TextSubtle,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 10, 0)
        };
        filterStrip.Children.Add(_lblAppCount);

        // Search text box
        _txtAppSearch = new TextBox
        {
            FontSize = 11,
            Height = 26,
            Padding = new Thickness(6, 2, 6, 2),
            BorderBrush = BorderMuted,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        _txtAppSearch.TextChanged += (s, e) => ApplyAppFilter();
        DockPanel.SetDock(_txtAppSearch, Dock.Right);
        filterStrip.Children.Add(_txtAppSearch);

        var lblSearch = new TextBlock
        {
            Text = "Search App:",
            FontSize = 11,
            Foreground = TextSubtle,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(10, 0, 4, 0)
        };
        DockPanel.SetDock(lblSearch, Dock.Right);
        filterStrip.Children.Add(lblSearch);

        // Category Filter Pills
        var filterPillStack = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(6, 0, 6, 0) };
        string[] categories = { "ALL", "64-BIT", "32-BIT", "MSI" };
        foreach (var cat in categories)
        {
            var pBtn = new Button
            {
                Content = cat,
                FontSize = 10,
                Padding = new Thickness(8, 3, 8, 3),
                Margin = new Thickness(2, 0, 2, 0),
                Background = cat == "ALL" ? new SolidColorBrush(Color.FromRgb(219, 234, 254)) : new SolidColorBrush(Color.FromRgb(248, 250, 252)),
                Foreground = cat == "ALL" ? SvllBlue : TextDark,
                BorderBrush = BorderMuted
            };
            pBtn.Click += (s, e) =>
            {
                _activeAppFilter = cat;
                foreach (UIElement child in filterPillStack.Children)
                {
                    if (child is Button b)
                    {
                        bool isSel = (string)b.Content == cat;
                        b.Background = isSel ? new SolidColorBrush(Color.FromRgb(219, 234, 254)) : new SolidColorBrush(Color.FromRgb(248, 250, 252));
                        b.Foreground = isSel ? SvllBlue : TextDark;
                    }
                }
                ApplyAppFilter();
            };
            filterPillStack.Children.Add(pBtn);
        }
        filterStrip.Children.Add(filterPillStack);

        mainContent.Children.Add(filterStrip);

        // ------------------ 4. Installed Apps Table / ListView ------------------
        _listInstalledApps = new ListView
        {
            Height = 330,
            FontSize = 11,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            Margin = new Thickness(0, 0, 0, 12)
        };

        var gv = new GridView();

        // Checkbox column
        var chkCol = new GridViewColumn { Header = "Select", Width = 50 };
        var chkFactory = new FrameworkElementFactory(typeof(CheckBox));
        chkFactory.SetBinding(ToggleButton.IsCheckedProperty, new Binding("IsSelected") { Mode = BindingMode.TwoWay });
        chkFactory.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        chkCol.CellTemplate = new DataTemplate { VisualTree = chkFactory };
        gv.Columns.Add(chkCol);

        // Architecture / MSI Badge
        var badgeCol = new GridViewColumn { Header = "Arch", Width = 65 };
        var badgeFactory = new FrameworkElementFactory(typeof(Border));
        badgeFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(3));
        badgeFactory.SetValue(Border.PaddingProperty, new Thickness(4, 1, 4, 1));
        badgeFactory.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Left);

        var badgeText = new FrameworkElementFactory(typeof(TextBlock));
        badgeText.SetBinding(TextBlock.TextProperty, new Binding("ArchitectureBadge"));
        badgeText.SetValue(TextBlock.FontWeightProperty, FontWeights.Bold);
        badgeText.SetValue(TextBlock.FontSizeProperty, 9.5);
        badgeText.SetValue(TextBlock.ForegroundProperty, Brushes.White);
        badgeFactory.AppendChild(badgeText);

        badgeFactory.SetBinding(Border.BackgroundProperty, new Binding("ArchitectureColor")
        {
            Converter = new StringToBrushConverter()
        });
        badgeCol.CellTemplate = new DataTemplate { VisualTree = badgeFactory };
        gv.Columns.Add(badgeCol);

        // Program Name
        gv.Columns.Add(new GridViewColumn { Header = "Program Name", Width = 260, DisplayMemberBinding = new Binding("DisplayName") });
        gv.Columns.Add(new GridViewColumn { Header = "Publisher", Width = 150, DisplayMemberBinding = new Binding("Publisher") });
        gv.Columns.Add(new GridViewColumn { Header = "Version", Width = 100, DisplayMemberBinding = new Binding("DisplayVersion") });
        gv.Columns.Add(new GridViewColumn { Header = "Size", Width = 75, DisplayMemberBinding = new Binding("EstimatedSizeFormatted") });
        gv.Columns.Add(new GridViewColumn { Header = "Install Location", Width = 240, DisplayMemberBinding = new Binding("InstallLocation") });

        // Status
        var statusCol = new GridViewColumn { Header = "Status", Width = 110 };
        var statusFactory = new FrameworkElementFactory(typeof(TextBlock));
        statusFactory.SetBinding(TextBlock.TextProperty, new Binding("Status"));
        statusFactory.SetValue(TextBlock.FontWeightProperty, FontWeights.SemiBold);
        statusFactory.SetBinding(TextBlock.ForegroundProperty, new Binding("StatusColor")
        {
            Converter = new StringToBrushConverter()
        });
        statusCol.CellTemplate = new DataTemplate { VisualTree = statusFactory };
        gv.Columns.Add(statusCol);

        _listInstalledApps.View = gv;
        mainContent.Children.Add(_listInstalledApps);

        // ------------------ 5. Progress and Action Execution Bar ------------------
        _progUninstall = new ProgressBar
        {
            Height = 6,
            Margin = new Thickness(0, 0, 0, 6),
            Foreground = SvllRed,
            Background = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
            Visibility = Visibility.Collapsed
        };
        mainContent.Children.Add(_progUninstall);

        _lblUninstallStatus = new TextBlock
        {
            Text = "Select an application and choose an uninstallation method below.",
            FontSize = 11,
            Foreground = TextSubtle,
            Margin = new Thickness(0, 0, 0, 12)
        };
        mainContent.Children.Add(_lblUninstallStatus);

        var actionDock = new DockPanel();

        // 1. Silent Elevated Uninstall (Standard)
        _btnSilentUninstall = new Button
        {
            Content = "🗑️ SILENT UNINSTALL (NO ADMIN PASSWORD PROMPT)",
            ToolTip = "Runs the uninstaller elevated with silent parameters. Windows UAC will never prompt for a password.",
            FontSize = 11.5,
            FontWeight = FontWeights.Bold,
            Padding = new Thickness(14, 8, 14, 8),
            Background = SvllBlue,
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand,
            Margin = new Thickness(0, 0, 10, 0)
        };
        _btnSilentUninstall.Click += async (s, e) => await ExecuteBatchUninstallAsync(forcePurge: false);
        actionDock.Children.Add(_btnSilentUninstall);

        // 2. Nuclear Force Purge & Bypass (Password-Protected & Stubborn Apps)
        _btnForcePurge = new Button
        {
            Content = "🔥 FORCE PURGE & BYPASS (REMOVE WITHOUT ANY PASSWORD)",
            ToolTip = "For stubborn apps with uninstall passwords or broken uninstallers: terminates processes, deletes services, wipes files, and removes registry keys with zero passwords required.",
            FontSize = 11.5,
            FontWeight = FontWeights.Bold,
            Padding = new Thickness(14, 8, 14, 8),
            Background = SvllRed,
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand
        };
        _btnForcePurge.Click += async (s, e) => await ExecuteBatchUninstallAsync(forcePurge: true);
        actionDock.Children.Add(_btnForcePurge);

        mainContent.Children.Add(actionDock);
        mainCard.Child = mainContent;
        root.Children.Add(mainCard);

        scroll.Content = root;

        // Auto scan installed applications on load
        Dispatcher.BeginInvoke(new Action(async () =>
        {
            await ScanInstalledApplicationsAsync();
        }));

        return scroll;
    }

    private void SetAllAppItemsSelection(bool select)
    {
        foreach (var app in _allInstalledApps)
        {
            app.IsSelected = select;
        }
        _listInstalledApps.Items.Refresh();
    }

    private void ApplyAppFilter()
    {
        string query = _txtAppSearch.Text.Trim().ToLowerInvariant();

        var filtered = _allInstalledApps.Where(app =>
        {
            if (_activeAppFilter == "64-BIT" && !app.ArchitectureBadge.Contains("64")) return false;
            if (_activeAppFilter == "32-BIT" && !app.ArchitectureBadge.Contains("32")) return false;
            if (_activeAppFilter == "MSI" && !app.IsMsi) return false;

            if (!string.IsNullOrEmpty(query))
            {
                if (!app.DisplayName.ToLowerInvariant().Contains(query) &&
                    !app.Publisher.ToLowerInvariant().Contains(query) &&
                    !app.InstallLocation.ToLowerInvariant().Contains(query))
                {
                    return false;
                }
            }

            return true;
        }).ToList();

        _listInstalledApps.ItemsSource = filtered;
        _lblAppCount.Text = $"Showing {filtered.Count} of {_allInstalledApps.Count} installed applications.";
    }

    #region Registry Scanning Engine

    private async Task ScanInstalledApplicationsAsync()
    {
        _progUninstall.Visibility = Visibility.Visible;
        _progUninstall.IsIndeterminate = true;
        _lblUninstallStatus.Text = "Scanning Windows 64-bit and 32-bit registry hives for installed programs...";

        _allInstalledApps.Clear();

        await Task.Run(() =>
        {
            try
            {
                // 1. 64-bit HKLM
                ScanRegistryUninstallHive(RegistryHive.LocalMachine, RegistryView.Registry64, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", "HKLM (64-bit)", "[64-Bit]", "#2563EB");

                // 2. 32-bit HKLM (WOW64)
                ScanRegistryUninstallHive(RegistryHive.LocalMachine, RegistryView.Registry32, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", "HKLM (32-bit)", "[32-Bit]", "#D97706");

                // 3. User HKCU
                ScanRegistryUninstallHive(RegistryHive.CurrentUser, RegistryView.Default, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", "HKCU (User)", "[User]", "#8B5CF6");
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => Log($"[APP SCAN ERROR] {ex.Message}"));
            }
        });

        _progUninstall.IsIndeterminate = false;
        _progUninstall.Visibility = Visibility.Collapsed;

        // Sort alphabetically
        _allInstalledApps.Sort((a, b) => string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase));

        ApplyAppFilter();
        _lblUninstallStatus.Text = $"Ready. {_allInstalledApps.Count} applications detected across all registry hives.";
        Log($"[APP UNINSTALLER] Found {_allInstalledApps.Count} installed applications.");
    }

    private void ScanRegistryUninstallHive(RegistryHive hive, RegistryView view, string subKeyPath, string hiveLabel, string archBadge, string archColor)
    {
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(hive, view);
            using var uninstallKey = baseKey.OpenSubKey(subKeyPath);
            if (uninstallKey == null) return;

            foreach (var keyName in uninstallKey.GetSubKeyNames())
            {
                try
                {
                    using var appKey = uninstallKey.OpenSubKey(keyName);
                    if (appKey == null) continue;

                    string displayName = appKey.GetValue("DisplayName")?.ToString()?.Trim() ?? "";
                    if (string.IsNullOrEmpty(displayName)) continue;

                    // Skip Windows hotfixes, updates, and system components
                    int isSystem = 0;
                    if (appKey.GetValue("SystemComponent") != null)
                        int.TryParse(appKey.GetValue("SystemComponent")!.ToString(), out isSystem);
                    if (isSystem == 1) continue;

                    string releaseType = appKey.GetValue("ReleaseType")?.ToString() ?? "";
                    if (releaseType.Equals("Security Update", StringComparison.OrdinalIgnoreCase) ||
                        releaseType.Equals("Update", StringComparison.OrdinalIgnoreCase) ||
                        releaseType.Equals("Hotfix", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    string version = appKey.GetValue("DisplayVersion")?.ToString()?.Trim() ?? "";
                    string publisher = appKey.GetValue("Publisher")?.ToString()?.Trim() ?? "Unknown";
                    string installLocation = appKey.GetValue("InstallLocation")?.ToString()?.Trim() ?? "";
                    string uninstallString = appKey.GetValue("UninstallString")?.ToString()?.Trim() ?? "";
                    string quietUninstall = appKey.GetValue("QuietUninstallString")?.ToString()?.Trim() ?? "";
                    string installDate = appKey.GetValue("InstallDate")?.ToString()?.Trim() ?? "";

                    int sizeKb = 0;
                    if (appKey.GetValue("EstimatedSize") != null)
                        int.TryParse(appKey.GetValue("EstimatedSize")!.ToString(), out sizeKb);

                    bool isMsi = false;
                    int winInstaller = 0;
                    if (appKey.GetValue("WindowsInstaller") != null)
                        int.TryParse(appKey.GetValue("WindowsInstaller")!.ToString(), out winInstaller);

                    if (winInstaller == 1 || keyName.StartsWith("{") && keyName.EndsWith("}") ||
                        uninstallString.ToLowerInvariant().Contains("msiexec"))
                    {
                        isMsi = true;
                    }

                    // If installLocation is missing, attempt to extract directory from uninstallString
                    if (string.IsNullOrEmpty(installLocation) && !string.IsNullOrEmpty(uninstallString) && !isMsi)
                    {
                        installLocation = ExtractDirectoryFromCommand(uninstallString);
                    }

                    var item = new InstalledAppItem
                    {
                        DisplayName = displayName,
                        DisplayVersion = version,
                        Publisher = publisher,
                        InstallLocation = installLocation,
                        UninstallString = uninstallString,
                        QuietUninstallString = quietUninstall,
                        InstallDate = installDate,
                        IsMsi = isMsi,
                        RegistryKeyName = keyName,
                        RegistryKeyPath = $"{subKeyPath}\\{keyName}",
                        RegistryHive = hiveLabel,
                        ArchitectureBadge = isMsi ? "[MSI]" : archBadge,
                        ArchitectureColor = isMsi ? "#10B981" : archColor,
                        EstimatedSizeFormatted = sizeKb > 0 ? FormatBytes((long)sizeKb * 1024) : "—",
                        Status = "Installed",
                        StatusColor = "#64748B"
                    };

                    _allInstalledApps.Add(item);
                }
                catch
                {
                    // Ignore single subkey read errors
                }
            }
        }
        catch
        {
            // Ignore hive errors
        }
    }

    private static string ExtractDirectoryFromCommand(string cmd)
    {
        try
        {
            cmd = cmd.Trim();
            if (cmd.StartsWith("\""))
            {
                int endQuote = cmd.IndexOf('"', 1);
                if (endQuote > 1)
                {
                    string path = cmd.Substring(1, endQuote - 1);
                    return Path.GetDirectoryName(path) ?? "";
                }
            }
            else
            {
                int firstSpace = cmd.IndexOf(' ');
                string path = firstSpace > 0 ? cmd.Substring(0, firstSpace) : cmd;
                return Path.GetDirectoryName(path) ?? "";
            }
        }
        catch { }
        return "";
    }

    #endregion

    #region Uninstallation Engines

    private async Task ExecuteBatchUninstallAsync(bool forcePurge)
    {
        // Get selected items or item clicked
        var selected = _allInstalledApps.Where(a => a.IsSelected).ToList();
        if (selected.Count == 0 && _listInstalledApps.SelectedItem is InstalledAppItem selItem)
        {
            selected.Add(selItem);
        }

        if (selected.Count == 0)
        {
            MessageBox.Show("Please select or click on at least one application to uninstall.", "No Application Selected", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (forcePurge)
        {
            var warn = MessageBox.Show(
                $"⚠️ NUCLEAR FORCE PURGE & BYPASS WARNING ⚠️\n\n" +
                $"You are about to forcibly eradicate {selected.Count} application(s):\n" +
                string.Join("\n", selected.Take(5).Select(s => $"• {s.DisplayName}")) +
                (selected.Count > 5 ? $"\n...and {selected.Count - 5} more." : "") + "\n\n" +
                "THIS ACTION WILL:\n" +
                "1. Terminate all related running processes.\n" +
                "2. Stop and delete associated Windows background services.\n" +
                "3. Take filesystem ownership and recursively delete all installation files.\n" +
                "4. Obliterate the application's registry entries.\n" +
                "5. BYPASS ANY UNINSTALLATION PASSWORD OR BLOCKED UNINSTALLERS.\n\n" +
                "Do you want to proceed with Force Purge?",
                "Confirm Force Purge & Password Bypass", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (warn != MessageBoxResult.Yes) return;
        }
        else
        {
            var confirm = MessageBox.Show(
                $"Proceed with Elevated Silent Uninstall for {selected.Count} program(s)?\n\n" +
                "This runs under elevated administrator privilege with silent switches so Windows will not ask for an administrator password.",
                "Confirm Silent Uninstall", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;
        }

        _btnSilentUninstall.IsEnabled = false;
        _btnForcePurge.IsEnabled = false;
        _progUninstall.Visibility = Visibility.Visible;
        _progUninstall.Minimum = 0;
        _progUninstall.Maximum = selected.Count;
        _progUninstall.Value = 0;

        Log($"\n=======================================================");
        Log($"[APP REMOVAL STARTED] Method: {(forcePurge ? "FORCE PURGE (PASSWORD BYPASS)" : "ELEVATED SILENT UNINSTALL")} | Targets: {selected.Count}");
        Log($"=======================================================");

        int successCount = 0;
        int failCount = 0;

        for (int i = 0; i < selected.Count; i++)
        {
            var app = selected[i];
            app.Status = forcePurge ? "Purging..." : "Uninstalling...";
            app.StatusColor = "#2563EB"; // Blue
            _listInstalledApps.Items.Refresh();

            _lblUninstallStatus.Text = $"[{i + 1}/{selected.Count}] Processing {app.DisplayName}...";
            _progUninstall.Value = i;

            bool ok = false;
            if (forcePurge)
            {
                ok = await ForcePurgeApplicationAsync(app);
            }
            else
            {
                ok = await StandardSilentUninstallAsync(app);
            }

            if (ok)
            {
                app.Status = forcePurge ? "✅ Purged & Eradicated" : "✅ Uninstalled";
                app.StatusColor = "#16A34A"; // Green
                successCount++;
            }
            else
            {
                app.Status = "❌ Removal Failed";
                app.StatusColor = "#DC2626"; // Red
                failCount++;
            }

            _listInstalledApps.Items.Refresh();
        }

        _progUninstall.Value = selected.Count;
        _progUninstall.Visibility = Visibility.Collapsed;
        _btnSilentUninstall.IsEnabled = true;
        _btnForcePurge.IsEnabled = true;

        string summary = $"Completed: {successCount} removed, {failCount} failed out of {selected.Count} total.";
        _lblUninstallStatus.Text = summary;
        Log($"\n[APP REMOVAL FINISHED] {summary}\n");

        MessageBox.Show(summary, "Application Removal Summary", MessageBoxButton.OK,
            failCount == 0 ? MessageBoxImage.Information : MessageBoxImage.Warning);

        // Refresh list
        await ScanInstalledApplicationsAsync();
    }

    private async Task<bool> StandardSilentUninstallAsync(InstalledAppItem app)
    {
        Log($"\n--> Standard Silent Uninstall: {app.DisplayName}");

        // 1. If MSI package
        if (app.IsMsi)
        {
            string productGuid = ExtractGuid(app.RegistryKeyName);
            if (string.IsNullOrEmpty(productGuid)) productGuid = ExtractGuid(app.UninstallString);

            if (!string.IsNullOrEmpty(productGuid))
            {
                string msiArgs = $"/x {productGuid} /qn /norestart";
                Log($"    Executing MSI uninstaller: msiexec.exe {msiArgs}");
                string output = await ExecuteAsync("msiexec.exe", msiArgs);
                return true;
            }
        }

        // 2. If QuietUninstallString is specified by vendor
        if (!string.IsNullOrEmpty(app.QuietUninstallString))
        {
            Log($"    Executing vendor QuietUninstallString: {app.QuietUninstallString}");
            await ExecuteCommandLineAsync(app.QuietUninstallString);
            return true;
        }

        // 3. If UninstallString exists, append standard silent switches
        if (!string.IsNullOrEmpty(app.UninstallString))
        {
            string cmd = app.UninstallString.Trim();
            string silentCmd = BuildSilentUninstallCommand(cmd);
            Log($"    Executing silent command: {silentCmd}");
            await ExecuteCommandLineAsync(silentCmd);
            return true;
        }

        Log("    [ERROR] No valid uninstall command found. Try 'Force Purge & Bypass'.");
        return false;
    }

    private string BuildSilentUninstallCommand(string rawCmd)
    {
        string lower = rawCmd.ToLowerInvariant();

        // MSI
        if (lower.Contains("msiexec"))
        {
            return rawCmd.Replace("/I", "/X").Replace("/i", "/x") + " /qn /norestart";
        }

        // Inno Setup
        if (lower.Contains("unins000") || lower.Contains("unins001"))
        {
            return $"{rawCmd} /VERYSILENT /SUPPRESSMSGBOXES /NORESTART";
        }

        // NSIS
        if (lower.Contains("uninstall.exe"))
        {
            return $"{rawCmd} /S";
        }

        // Default universal fallback
        return $"{rawCmd} /quiet /norestart";
    }

    private async Task ExecuteCommandLineAsync(string fullCmd)
    {
        await Task.Run(() =>
        {
            try
            {
                using var p = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/c \"{fullCmd}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                p.OutputDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data)) Dispatcher.Invoke(() => Log($"    [OUT] {e.Data}"));
                };
                p.ErrorDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data)) Dispatcher.Invoke(() => Log($"    [ERR] {e.Data}"));
                };

                p.Start();
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();
                p.WaitForExit(90000); // 90s timeout
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => Log($"    [EXECUTION ERROR] {ex.Message}"));
            }
        });
    }

    #endregion

    #region Nuclear Force Purge Engine (Password Bypass & Clean Wipe)

    private async Task<bool> ForcePurgeApplicationAsync(InstalledAppItem app)
    {
        Log($"\n--> 🔥 FORCE PURGING: {app.DisplayName}");
        Log($"    Target Registry Key: {app.RegistryKeyPath}");
        Log($"    Install Location: {app.InstallLocation}");

        bool success = true;

        await Task.Run(async () =>
        {
            // 1. Terminate all running processes associated with this app
            try
            {
                KillAssociatedProcesses(app);
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => Log($"    [WARN] Process kill exception: {ex.Message}"));
            }

            // 2. Stop and delete associated Windows services
            try
            {
                StopAndDeleteAssociatedServices(app);
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => Log($"    [WARN] Service cleanup exception: {ex.Message}"));
            }

            // 3. Forcibly delete installation folder(s)
            if (!string.IsNullOrEmpty(app.InstallLocation) && Directory.Exists(app.InstallLocation))
            {
                try
                {
                    Dispatcher.Invoke(() => Log($"    Taking filesystem ownership: {app.InstallLocation}"));
                    await ExecuteAsync("takeown.exe", $"/F \"{app.InstallLocation}\" /R /D Y");
                    await ExecuteAsync("icacls.exe", $"\"{app.InstallLocation}\" /grant administrators:F /T /C /Q");

                    Dispatcher.Invoke(() => Log($"    Deleting folder: {app.InstallLocation}"));
                    Directory.Delete(app.InstallLocation, recursive: true);
                    Dispatcher.Invoke(() => Log("    ✔ Folder wiped successfully."));
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() => Log($"    [WARN] Folder delete fallback (cmd rmdir): {ex.Message}"));
                    await ExecuteAsync("cmd.exe", $"/c rd /s /q \"{app.InstallLocation}\"");
                }
            }

            // 4. Delete Registry Uninstall Key
            try
            {
                Dispatcher.Invoke(() => Log($"    Deleting uninstall registry key: {app.RegistryKeyName}"));
                DeleteRegistryUninstallKey(app);
                Dispatcher.Invoke(() => Log("    ✔ Registry uninstall registration wiped."));
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => Log($"    [ERROR] Registry wipe failed: {ex.Message}"));
                success = false;
            }

            // 5. Clean Startup Entries
            try
            {
                CleanStartupEntries(app);
            }
            catch { }
        });

        return success;
    }

    private void KillAssociatedProcesses(InstalledAppItem app)
    {
        string targetDir = app.InstallLocation.ToLowerInvariant();
        string appName = SanitizeAppName(app.DisplayName).ToLowerInvariant();

        foreach (var p in Process.GetProcesses())
        {
            try
            {
                bool shouldKill = false;
                string pName = p.ProcessName.ToLowerInvariant();

                if (!string.IsNullOrEmpty(targetDir))
                {
                    try
                    {
                        string pPath = p.MainModule?.FileName?.ToLowerInvariant() ?? "";
                        if (pPath.StartsWith(targetDir))
                        {
                            shouldKill = true;
                        }
                    }
                    catch { }
                }

                if (!shouldKill && appName.Length > 3 && pName.Contains(appName))
                {
                    shouldKill = true;
                }

                if (shouldKill)
                {
                    Dispatcher.Invoke(() => Log($"    Terminating process: {p.ProcessName} (PID {p.Id})"));
                    p.Kill(entireProcessTree: true);
                }
            }
            catch { }
        }
    }

    private void StopAndDeleteAssociatedServices(InstalledAppItem app)
    {
        if (string.IsNullOrEmpty(app.InstallLocation)) return;
        string targetDir = app.InstallLocation.ToLowerInvariant();

        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Name, PathName, State FROM Win32_Service");
            foreach (var svc in searcher.Get())
            {
                string sPath = svc["PathName"]?.ToString()?.ToLowerInvariant() ?? "";
                string sName = svc["Name"]?.ToString() ?? "";

                if (sPath.Contains(targetDir) && !string.IsNullOrEmpty(sName))
                {
                    Dispatcher.Invoke(() => Log($"    Stopping & Deleting Service: {sName}"));
                    try
                    {
                        using var sc = new ServiceController(sName);
                        if (sc.Status == ServiceControllerStatus.Running)
                        {
                            sc.Stop();
                            sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(5));
                        }
                    }
                    catch { }

                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "sc.exe",
                        Arguments = $"delete \"{sName}\"",
                        CreateNoWindow = true,
                        UseShellExecute = false
                    })?.WaitForExit();
                }
            }
        }
        catch { }
    }

    private void DeleteRegistryUninstallKey(InstalledAppItem app)
    {
        string subKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
        string wowKey = @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall";

        // 1. Try HKLM 64-bit
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            using var parent = baseKey.OpenSubKey(subKey, writable: true);
            parent?.DeleteSubKeyTree(app.RegistryKeyName, throwOnMissingSubKey: false);
        }
        catch { }

        // 2. Try HKLM 32-bit (WOW64)
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
            using var parent = baseKey.OpenSubKey(subKey, writable: true);
            parent?.DeleteSubKeyTree(app.RegistryKeyName, throwOnMissingSubKey: false);
        }
        catch { }

        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            using var parent = baseKey.OpenSubKey(wowKey, writable: true);
            parent?.DeleteSubKeyTree(app.RegistryKeyName, throwOnMissingSubKey: false);
        }
        catch { }

        // 3. Try HKCU
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default);
            using var parent = baseKey.OpenSubKey(subKey, writable: true);
            parent?.DeleteSubKeyTree(app.RegistryKeyName, throwOnMissingSubKey: false);
        }
        catch { }
    }

    private void CleanStartupEntries(InstalledAppItem app)
    {
        string[] hives = { @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Run" };
        string appName = SanitizeAppName(app.DisplayName).ToLowerInvariant();

        foreach (var runPath in hives)
        {
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
                using var runKey = baseKey.OpenSubKey(runPath, writable: true);
                if (runKey != null)
                {
                    foreach (var val in runKey.GetValueNames())
                    {
                        if (val.ToLowerInvariant().Contains(appName))
                        {
                            runKey.DeleteValue(val, throwOnMissingValue: false);
                            Dispatcher.Invoke(() => Log($"    Removed startup entry: {val}"));
                        }
                    }
                }
            }
            catch { }
        }
    }

    private static string ExtractGuid(string input)
    {
        if (string.IsNullOrEmpty(input)) return "";
        var match = Regex.Match(input, @"\{[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\}");
        return match.Success ? match.Value : "";
    }

    private static string SanitizeAppName(string name)
    {
        int space = name.IndexOf(' ');
        if (space > 2) return name.Substring(0, space);
        return name;
    }

    #endregion
}
