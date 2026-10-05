using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
    private ListView _listUsers = null!;
    private readonly List<LocalUserAccount> _cachedUsers = new List<LocalUserAccount>();
    private TextBox _txtVaultCredentials = null!;

    #region 1. WinUtil Debloat & Tweaks View

    private UIElement BuildWinUtilTweaksView()
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
            Text = "WINDOWS OS DEBLOAT & SYSTEM OPTIMIZATIONS",
            FontSize = 13,
            FontWeight = FontWeights.Bold,
            Foreground = SvllBlue,
            Margin = new Thickness(0, 0, 0, 6)
        });
        sp.Children.Add(new TextBlock
        {
            Text = "Applies enterprise debloat policies, disables intrusive background telemetry services, removes consumer UWP provisioned bloat, and enables power-user desktop tweaks.",
            FontSize = 11,
            Foreground = TextSubtle,
            Margin = new Thickness(0, 0, 0, 12)
        });

        sp.Children.Add(CreateToolRow("Apply Essential Fleet Tweaks", "Disables telemetry, Bing web search in Start, enables file extensions in Explorer, and enables 'End Task' right-click on the taskbar.",
            async () => await ApplyEssentialTweaksAsync(), "Apply Tweaks"));

        sp.Children.Add(CreateToolRow("Remove Consumer UWP Bloatware", "Purges non-essential provisioned AppX packages (Xbox, Solitaire, Feedback Hub, Zune, Bing News, Cortana) across all user profiles.",
            async () => await RemoveUwpBloatwareAsync(), "Purge Bloatware"));

        sp.Children.Add(CreateToolRow("Launch Chris Titus Tech Windows Utility (CTT)", "Launches the official community-standard WinUtil suite via PowerShell execution.",
            async () => await ExecuteAsync("powershell.exe", "-NoProfile -Command \"irm https://christitus.com/win | iex\""), "Launch CTT"));

        sp.Children.Add(CreateToolRow("Launch Microsoft Activation Scripts (MAS)", "Launches the open-source Windows & Office HWID KMS activation script.",
            async () => await ExecuteAsync("powershell.exe", "-NoProfile -Command \"irm https://get.activated.win | iex\""), "Launch MAS"));

        card.Child = sp;
        root.Children.Add(card);

        scroll.Content = root;
        return scroll;
    }

    private async Task ApplyEssentialTweaksAsync()
    {
        Log("\n[TWEAKS] Applying SVLL essential fleet tweaks...");

        // 1. Disable Telemetry
        await ExecuteAsync("reg.exe", "add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\DataCollection\" /v AllowTelemetry /t REG_DWORD /d 0 /f");
        await ExecuteAsync("sc.exe", "stop DiagTrack");
        await ExecuteAsync("sc.exe", "config DiagTrack start= disabled");

        // 2. Disable Bing Search in Start Menu
        await ExecuteAsync("reg.exe", "add \"HKCU\\Software\\Policies\\Microsoft\\Windows\\Explorer\" /v DisableSearchBoxSuggestions /t REG_DWORD /d 1 /f");
        await ExecuteAsync("reg.exe", "add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Search\" /v BingSearchEnabled /t REG_DWORD /d 0 /f");

        // 3. Show File Extensions in Windows Explorer
        await ExecuteAsync("reg.exe", "add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /v HideFileExt /t REG_DWORD /d 0 /f");

        // 4. Enable "End Task" on Taskbar Right-Click (Win 11)
        await ExecuteAsync("reg.exe", "add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\\TaskbarDeveloperSettings\" /v TaskbarEndTask /t REG_DWORD /d 1 /f");

        // 5. Disable Fast Startup / Hibernation for stability
        await ExecuteAsync("powercfg.exe", "-h off");

        Log("[TWEAKS] All essential fleet debloat tweaks applied successfully!");
    }

    private async Task RemoveUwpBloatwareAsync()
    {
        Log("\n[DEBLOAT] Purging pre-installed consumer UWP packages...");
        string psCmd = @"$bloat = @('*Xbox*', '*Solitaire*', '*FeedbackHub*', '*BingNews*', '*BingWeather*', '*GetHelp*', '*YourPhone*');
foreach ($b in $bloat) {
    Get-AppxPackage -Name $b | Remove-AppxPackage -ErrorAction SilentlyContinue;
    Get-AppxProvisionedPackage -Online | Where-Object DisplayName -like $b | Remove-AppxProvisionedPackage -Online -ErrorAction SilentlyContinue;
}";
        await ExecuteAsync("powershell.exe", $"-NoProfile -Command \"{psCmd}\"");
        Log("[DEBLOAT] Consumer UWP bloatware removed successfully!");
    }

    #endregion

    #region 2. WinGet Software Deployer (with M365 & WinZip)

    private void InitializeSoftwareLibrary()
    {
        _softwarePackages = new List<SoftwarePackage>
        {
            // Browsers
            new SoftwarePackage { Name = "Google Chrome", WingetId = "Google.Chrome", Category = "Browser", IsSelected = true },
            new SoftwarePackage { Name = "Mozilla Firefox", WingetId = "Mozilla.Firefox", Category = "Browser", IsSelected = false },
            new SoftwarePackage { Name = "Microsoft Edge", WingetId = "Microsoft.Edge", Category = "Browser", IsSelected = false },
            new SoftwarePackage { Name = "Brave Browser", WingetId = "Brave.Brave", Category = "Browser", IsSelected = false },

            // Remote Access & Support
            new SoftwarePackage { Name = "AnyDesk Remote Support", WingetId = "AnyDeskSoftwareGmbH.AnyDesk", Category = "Remote Support", IsSelected = true },
            new SoftwarePackage { Name = "TeamViewer Client", WingetId = "TeamViewer.TeamViewer", Category = "Remote Support", IsSelected = false },
            new SoftwarePackage { Name = "RustDesk Open Source Remote", WingetId = "RustDesk.RustDesk", Category = "Remote Support", IsSelected = false },
            new SoftwarePackage { Name = "Microsoft Remote Desktop", WingetId = "Microsoft.WindowsTerminal", Category = "Remote Support", IsSelected = false },

            // Office & Productivity
            new SoftwarePackage { Name = "Adobe Acrobat Reader", WingetId = "Adobe.Acrobat.Reader.64-bit", Category = "Productivity", IsSelected = true },
            new SoftwarePackage { Name = "7-Zip File Archiver", WingetId = "7zip.7zip", Category = "Productivity", IsSelected = true },
            new SoftwarePackage { Name = "Microsoft Teams", WingetId = "Microsoft.Teams", Category = "Collaboration", IsSelected = false },
            new SoftwarePackage { Name = "Zoom Meetings", WingetId = "Zoom.Zoom", Category = "Collaboration", IsSelected = false },
            new SoftwarePackage { Name = "Notepad++ Text Editor", WingetId = "Notepad++.Notepad++", Category = "Productivity", IsSelected = true },

            // Network & Diagnostics
            new SoftwarePackage { Name = "PuTTY SSH / Telnet Client", WingetId = "PuTTY.PuTTY", Category = "Network / Admin", IsSelected = false },
            new SoftwarePackage { Name = "WinSCP SFTP / FTP Client", WingetId = "WinSCP.WinSCP", Category = "Network / Admin", IsSelected = false },
            new SoftwarePackage { Name = "Wireshark Packet Analyzer", WingetId = "WiresharkFoundation.Wireshark", Category = "Network / Admin", IsSelected = false },
            new SoftwarePackage { Name = "Nmap Network Scanner", WingetId = "Insecure.Nmap", Category = "Network / Admin", IsSelected = false },
            new SoftwarePackage { Name = "Advanced IP Scanner", WingetId = "Famatech.AdvancedIPScanner", Category = "Network / Admin", IsSelected = false },

            // Utilities & Dev
            new SoftwarePackage { Name = "Microsoft PowerToys", WingetId = "Microsoft.PowerToys", Category = "Utility", IsSelected = false },
            new SoftwarePackage { Name = "WinDirStat Disk Usage", WingetId = "WinDirStat.WinDirStat", Category = "Utility", IsSelected = false },
            new SoftwarePackage { Name = "VLC Media Player", WingetId = "VideoLAN.VLC", Category = "Utility", IsSelected = false },
            new SoftwarePackage { Name = "Visual Studio Code", WingetId = "Microsoft.VisualStudioCode", Category = "Development", IsSelected = false },
            new SoftwarePackage { Name = "Git for Windows", WingetId = "Git.Git", Category = "Development", IsSelected = false },
            new SoftwarePackage { Name = "Postman API Client", WingetId = "Postman.Postman", Category = "Development", IsSelected = false }
        };
    }

    private UIElement BuildWinGetSoftwareView()
    {
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var root = new StackPanel { Margin = new Thickness(0, 0, 10, 0) };

        // Card 1: 1-Click Priority Deployments (Microsoft 365 Enterprise & WinZip)
        var cardPriority = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(239, 246, 255)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(191, 219, 254)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16),
            Margin = new Thickness(0, 0, 0, 14)
        };

        var spPriority = new StackPanel();
        spPriority.Children.Add(new TextBlock
        {
            Text = "PRIORITY 1-CLICK SILENT ENTERPRISE PACKAGES",
            FontSize = 12.5,
            FontWeight = FontWeights.Bold,
            Foreground = SvllBlue,
            Margin = new Thickness(0, 0, 0, 6)
        });
        spPriority.Children.Add(new TextBlock
        {
            Text = "Deploy corporate productivity staples with unattended silent installation arguments via Windows Package Manager (WinGet).",
            FontSize = 11,
            Foreground = TextSubtle,
            Margin = new Thickness(0, 0, 0, 12)
        });

        var gridBtns = new Grid();
        gridBtns.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        gridBtns.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        // Microsoft 365 Button Card
        var btnM365 = new Button
        {
            Height = 52,
            Margin = new Thickness(0, 0, 8, 0),
            Background = SvllBlue,
            Foreground = Brushes.White,
            Cursor = Cursors.Hand,
            HorizontalContentAlignment = HorizontalAlignment.Left,
            Padding = new Thickness(14, 0, 14, 0)
        };
        var m365Panel = new StackPanel();
        m365Panel.Children.Add(new TextBlock { Text = "📦 Deploy Microsoft 365 Enterprise", FontSize = 12.5, FontWeight = FontWeights.Bold });
        m365Panel.Children.Add(new TextBlock { Text = "ID: Microsoft.Office  |  Unattended Silent Rollout", FontSize = 10, Foreground = new SolidColorBrush(Color.FromRgb(219, 234, 254)) });
        btnM365.Content = m365Panel;
        btnM365.Click += async (s, e) =>
        {
            if (!await EnsureWinGetReadyAsync()) return;
            Log("\n[WINGET] Deploying Microsoft 365 Enterprise suite (Microsoft.Office)...");
            await ExecuteAsync("winget.exe", "install --id Microsoft.Office --exact --silent --accept-package-agreements --accept-source-agreements");
            Log("[WINGET] Microsoft 365 installation finished.");
        };
        Grid.SetColumn(btnM365, 0);
        gridBtns.Children.Add(btnM365);

        // WinZip Button Card
        var btnWinZip = new Button
        {
            Height = 52,
            Margin = new Thickness(8, 0, 0, 0),
            Background = new SolidColorBrush(Color.FromRgb(30, 41, 59)),
            Foreground = Brushes.White,
            Cursor = Cursors.Hand,
            HorizontalContentAlignment = HorizontalAlignment.Left,
            Padding = new Thickness(14, 0, 14, 0)
        };
        var winzipPanel = new StackPanel();
        winzipPanel.Children.Add(new TextBlock { Text = "📦 Deploy WinZip Archiver", FontSize = 12.5, FontWeight = FontWeights.Bold });
        winzipPanel.Children.Add(new TextBlock { Text = "ID: WinZipComputing.WinZip  |  Unattended Silent Rollout", FontSize = 10, Foreground = new SolidColorBrush(Color.FromRgb(203, 213, 225)) });
        btnWinZip.Content = winzipPanel;
        btnWinZip.Click += async (s, e) =>
        {
            if (!await EnsureWinGetReadyAsync()) return;
            Log("\n[WINGET] Deploying WinZip Archiver (WinZipComputing.WinZip)...");
            await ExecuteAsync("winget.exe", "install --id WinZipComputing.WinZip --exact --silent --accept-package-agreements --accept-source-agreements");
            Log("[WINGET] WinZip installation finished.");
        };
        Grid.SetColumn(btnWinZip, 1);
        gridBtns.Children.Add(btnWinZip);

        spPriority.Children.Add(gridBtns);
        cardPriority.Child = spPriority;
        root.Children.Add(cardPriority);

        // Card 2: Batch Standard Software Deployment
        var cardStandard = new Border
        {
            Background = BgCard,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16),
            Margin = new Thickness(0, 0, 0, 14)
        };

        var spStd = new StackPanel();
        spStd.Children.Add(new TextBlock
        {
            Text = "BATCH SOFTWARE REPOSITORY (WINGET)",
            FontSize = 13,
            FontWeight = FontWeights.Bold,
            Foreground = SvllBlue,
            Margin = new Thickness(0, 0, 0, 6)
        });
        spStd.Children.Add(new TextBlock
        {
            Text = "Select standard workstation software packages and execute automated unattended batch installations.",
            FontSize = 11,
            Foreground = TextSubtle,
            Margin = new Thickness(0, 0, 0, 12)
        });

        var listSoft = new ListView
        {
            ItemsSource = _softwarePackages,
            Height = 280,
            FontSize = 11.5,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            Margin = new Thickness(0, 0, 0, 12)
        };
        var gv = new GridView();

        var chkCol = new GridViewColumn { Header = "Select", Width = 60 };
        var chkFactory = new FrameworkElementFactory(typeof(CheckBox));
        chkFactory.SetBinding(ToggleButton.IsCheckedProperty, new Binding("IsSelected"));
        chkFactory.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Center);
        chkCol.CellTemplate = new DataTemplate { VisualTree = chkFactory };
        gv.Columns.Add(chkCol);

        gv.Columns.Add(new GridViewColumn { Header = "Application Name", Width = 220, DisplayMemberBinding = new Binding("Name") });
        gv.Columns.Add(new GridViewColumn { Header = "WinGet Package ID", Width = 260, DisplayMemberBinding = new Binding("WingetId") });
        gv.Columns.Add(new GridViewColumn { Header = "Category", Width = 140, DisplayMemberBinding = new Binding("Category") });
        listSoft.View = gv;
        spStd.Children.Add(listSoft);

        var btnBar = new StackPanel { Orientation = Orientation.Horizontal };
        var btnSelectAll = new Button { Content = "Select All", Height = 30, Padding = new Thickness(10, 0, 10, 0), Margin = new Thickness(0, 0, 6, 0), Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)), BorderBrush = BorderMuted };
        btnSelectAll.Click += (s, e) => { foreach (var p in _softwarePackages) p.IsSelected = true; listSoft.Items.Refresh(); };
        btnBar.Children.Add(btnSelectAll);

        var btnDeselectAll = new Button { Content = "Deselect All", Height = 30, Padding = new Thickness(10, 0, 10, 0), Margin = new Thickness(0, 0, 10, 0), Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)), BorderBrush = BorderMuted };
        btnDeselectAll.Click += (s, e) => { foreach (var p in _softwarePackages) p.IsSelected = false; listSoft.Items.Refresh(); };
        btnBar.Children.Add(btnDeselectAll);

        var btnInstallBatch = new Button
        {
            Content = "Install Selected Packages via WinGet",
            Height = 32,
            Padding = new Thickness(16, 0, 16, 0),
            Background = SvllBlue,
            Foreground = Brushes.White,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 12, 0),
            Cursor = Cursors.Hand
        };
        btnBar.Children.Add(btnInstallBatch);

        // Custom WinGet Package Quick Installer
        var txtCustomPackage = new TextBox { Width = 180, Height = 28, VerticalContentAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 6, 0), Text = "Package.ID" };
        var btnCustomInstall = new Button
        {
            Content = "+ Install Custom ID",
            Height = 28,
            Padding = new Thickness(10, 0, 10, 0),
            Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
            BorderBrush = BorderMuted,
            Cursor = Cursors.Hand
        };
        btnCustomInstall.Click += async (s, e) =>
        {
            string pkg = txtCustomPackage.Text.Trim();
            if (string.IsNullOrEmpty(pkg) || pkg == "Package.ID")
            {
                MessageBox.Show("Please enter a valid WinGet package ID (e.g., Git.Git, Zoom.Zoom).", "Invalid Package ID", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!await EnsureWinGetReadyAsync()) return;
            Log($"\n[WINGET] Installing custom package: {pkg}...");
            await ExecuteAsync("winget.exe", $"install --id {pkg} --exact --silent --accept-package-agreements --accept-source-agreements");
            Log($"[WINGET] Installation of {pkg} finished.");
        };
        btnBar.Children.Add(txtCustomPackage);
        btnBar.Children.Add(btnCustomInstall);
        btnInstallBatch.Click += async (s, e) =>
        {
            var selected = _softwarePackages.Where(p => p.IsSelected).ToList();
            if (selected.Count == 0)
            {
                MessageBox.Show("Please select at least one software package.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!await EnsureWinGetReadyAsync()) return;

            Log($"\n[WINGET] Initiating batch deployment for {selected.Count} applications...");
            foreach (var item in selected)
            {
                Log($"[WINGET] Installing {item.Name} ({item.WingetId})...");
                await ExecuteAsync("winget.exe", $"install --id {item.WingetId} -e --silent --accept-package-agreements --accept-source-agreements");
            }
            Log("[WINGET] Batch deployment sequence completed!");
        };

        spStd.Children.Add(btnBar);
        cardStandard.Child = spStd;
        root.Children.Add(cardStandard);

        scroll.Content = root;
        return scroll;
    }

    #endregion

    #region 3. Windows Optional Features (DISM)

    private UIElement BuildWinFeaturesView()
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
            Text = "WINDOWS OPTIONAL FEATURES (DISM)",
            FontSize = 13,
            FontWeight = FontWeights.Bold,
            Foreground = SvllBlue,
            Margin = new Thickness(0, 0, 0, 6)
        });
        sp.Children.Add(new TextBlock
        {
            Text = "Enable or disable native Windows platform subsystems and administrative services using Deployment Image Servicing and Management (DISM).",
            FontSize = 11,
            Foreground = TextSubtle,
            Margin = new Thickness(0, 0, 0, 12)
        });

        void AddFeatureRow(string title, string featureName)
        {
            var p = new StackPanel { Margin = new Thickness(0, 4, 0, 8) };
            var g = new Grid();
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var tStack = new StackPanel();
            tStack.Children.Add(new TextBlock { Text = title, FontSize = 12, FontWeight = FontWeights.SemiBold });
            tStack.Children.Add(new TextBlock { Text = $"DISM Feature: {featureName}", FontSize = 10.5, Foreground = TextSubtle });
            Grid.SetColumn(tStack, 0);
            g.Children.Add(tStack);

            var bStack = new StackPanel { Orientation = Orientation.Horizontal };
            var btnEnable = new Button { Content = "Enable", Height = 28, Padding = new Thickness(10, 0, 10, 0), Margin = new Thickness(0, 0, 6, 0), Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)), BorderBrush = BorderMuted };
            btnEnable.Click += async (s, e) => await ExecuteAsync("dism.exe", $"/Online /Enable-Feature /FeatureName:{featureName} /All /NoRestart");
            bStack.Children.Add(btnEnable);

            var btnDisable = new Button { Content = "Disable", Height = 28, Padding = new Thickness(10, 0, 10, 0), Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)), BorderBrush = BorderMuted };
            btnDisable.Click += async (s, e) => await ExecuteAsync("dism.exe", $"/Online /Disable-Feature /FeatureName:{featureName} /NoRestart");
            bStack.Children.Add(btnDisable);

            Grid.SetColumn(bStack, 1);
            g.Children.Add(bStack);
            p.Children.Add(g);
            sp.Children.Add(p);
        }

        AddFeatureRow("Hyper-V Virtualization Platform", "Microsoft-Hyper-V-All");
        AddFeatureRow("Windows Sandbox Isolation Environment", "Containers-DisposableClientVM");
        AddFeatureRow("Windows Subsystem for Linux (WSL)", "Microsoft-Windows-Subsystem-Linux");
        AddFeatureRow("Virtual Machine Platform", "VirtualMachinePlatform");
        AddFeatureRow("Telnet Diagnostic Client", "TelnetClient");
        AddFeatureRow("TFTP Network Boot Client", "TFTP");
        AddFeatureRow("SMB 1.0/CIFS File Sharing (Legacy)", "SMB1Protocol");

        card.Child = sp;
        root.Children.Add(card);

        scroll.Content = root;
        return scroll;
    }

    #endregion

    #region 4. Windows Update Strategy

    private UIElement BuildWinUpdateConfigView()
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
            Text = "ENTERPRISE WINDOWS UPDATE POLICIES",
            FontSize = 13,
            FontWeight = FontWeights.Bold,
            Foreground = SvllBlue,
            Margin = new Thickness(0, 0, 0, 6)
        });
        sp.Children.Add(new TextBlock
        {
            Text = "Configure fleet update strategies to prevent unexpected reboots during warehouse operations or enforce critical security patches.",
            FontSize = 11,
            Foreground = TextSubtle,
            Margin = new Thickness(0, 0, 0, 12)
        });

        sp.Children.Add(CreateToolRow("Apply Recommended Corporate Update Profile", "Applies scheduled updates, delays feature upgrades for 30 days to ensure stability, and sets active hours to 7 AM - 9 PM.",
            async () =>
            {
                await ExecuteAsync("reg.exe", "add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU\" /v AUOptions /t REG_DWORD /d 3 /f");
                await ExecuteAsync("reg.exe", "add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU\" /v ScheduledInstallDay /t REG_DWORD /d 0 /f");
                Log("[UPDATE] Applied Recommended corporate update profile.");
            }, "Apply Recommended"));

        sp.Children.Add(CreateToolRow("Apply Factory Default Update Policy", "Removes custom group policies and restores standard consumer Windows Update automation.",
            async () =>
            {
                await ExecuteAsync("reg.exe", "delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\" /f");
                await ExecuteAsync("sc.exe", "config wuauserv start= auto");
                await ExecuteAsync("sc.exe", "start wuauserv");
                Log("[UPDATE] Restored default Windows Update policy.");
            }, "Restore Default"));

        sp.Children.Add(CreateToolRow("Block / Disable Windows Update Service", "Disables the Windows Update service (wuauserv) and Delivery Optimization to stop background bandwidth consumption.",
            async () =>
            {
                await ExecuteAsync("sc.exe", "stop wuauserv");
                await ExecuteAsync("sc.exe", "config wuauserv start= disabled");
                await ExecuteAsync("sc.exe", "stop dosvc");
                await ExecuteAsync("sc.exe", "config dosvc start= disabled");
                Log("[UPDATE] Windows Update service stopped and disabled.");
            }, "Block Updates"));

        card.Child = sp;
        root.Children.Add(card);

        scroll.Content = root;
        return scroll;
    }

    #endregion

    #region 5. Local User Accounts & Windows Vault Admin

    private UIElement BuildUsersView()
    {
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var root = new StackPanel { Margin = new Thickness(0, 0, 10, 0) };

        // Card 1: Local SAM User Accounts & Password Admin
        var cardUsers = new Border
        {
            Background = BgCard,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16),
            Margin = new Thickness(0, 0, 0, 14)
        };

        var spUsers = new StackPanel();
        spUsers.Children.Add(new TextBlock
        {
            Text = "LOCAL SAM USER ACCOUNTS & PASSWORD ADMIN",
            FontSize = 13,
            FontWeight = FontWeights.Bold,
            Foreground = SvllBlue,
            Margin = new Thickness(0, 0, 0, 6)
        });
        spUsers.Children.Add(new TextBlock
        {
            Text = "Audit configured local accounts, verify administrative memberships, and execute 1-click administrative password overrides.",
            FontSize = 11,
            Foreground = TextSubtle,
            Margin = new Thickness(0, 0, 0, 12)
        });

        // Toolbar for user management
        var userBar = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
        var btnAuditUsers = new Button
        {
            Content = " Audit Local Accounts ",
            Height = 30,
            Background = SvllBlue,
            Foreground = Brushes.White,
            FontWeight = FontWeights.Bold,
            Padding = new Thickness(12, 0, 12, 0),
            Margin = new Thickness(0, 0, 8, 0),
            Cursor = Cursors.Hand
        };
        btnAuditUsers.Click += async (s, e) => await AuditLocalUsersAsync();
        userBar.Children.Add(btnAuditUsers);

        var btnResetPass = new Button
        {
            Content = " 1-Click Reset Password ",
            Height = 30,
            Background = new SolidColorBrush(Color.FromRgb(225, 45, 45)),
            Foreground = Brushes.White,
            FontWeight = FontWeights.Bold,
            Padding = new Thickness(12, 0, 12, 0),
            Margin = new Thickness(0, 0, 8, 0),
            Cursor = Cursors.Hand
        };
        btnResetPass.Click += async (s, e) => await ResetSelectedUserPasswordAsync();
        userBar.Children.Add(btnResetPass);

        var btnToggleActive = new Button
        {
            Content = "Enable / Disable Account",
            Height = 30,
            Padding = new Thickness(10, 0, 10, 0),
            Margin = new Thickness(0, 0, 8, 0),
            Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
            BorderBrush = BorderMuted
        };
        btnToggleActive.Click += async (s, e) => await ToggleSelectedUserActiveAsync();
        userBar.Children.Add(btnToggleActive);

        var btnAddUser = new Button
        {
            Content = "+ Add Local User",
            Height = 30,
            Padding = new Thickness(10, 0, 10, 0),
            Margin = new Thickness(0, 0, 8, 0),
            Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
            BorderBrush = BorderMuted
        };
        btnAddUser.Click += async (s, e) => await PromptAddLocalUserAsync();
        userBar.Children.Add(btnAddUser);

        var btnCurrentUserSec = new Button
        {
            Content = "🔐 Current User Security & Hash Info",
            Height = 30,
            Padding = new Thickness(10, 0, 10, 0),
            Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
            BorderBrush = BorderMuted,
            Cursor = Cursors.Hand
        };
        btnCurrentUserSec.Click += async (s, e) => await AuditCurrentSessionUserSecurityAsync();
        userBar.Children.Add(btnCurrentUserSec);

        spUsers.Children.Add(userBar);

        _listUsers = new ListView
        {
            Height = 220,
            FontSize = 11.5,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1)
        };
        var gvUsers = new GridView();
        gvUsers.Columns.Add(new GridViewColumn { Header = "Username", Width = 160, DisplayMemberBinding = new Binding("Username") });
        gvUsers.Columns.Add(new GridViewColumn { Header = "Status", Width = 100, DisplayMemberBinding = new Binding("StatusText") });
        gvUsers.Columns.Add(new GridViewColumn { Header = "Role / Group", Width = 140, DisplayMemberBinding = new Binding("RoleText") });
        gvUsers.Columns.Add(new GridViewColumn { Header = "Full Name", Width = 180, DisplayMemberBinding = new Binding("FullName") });
        gvUsers.Columns.Add(new GridViewColumn { Header = "Description", Width = 260, DisplayMemberBinding = new Binding("Description") });
        _listUsers.View = gvUsers;
        spUsers.Children.Add(_listUsers);

        cardUsers.Child = spUsers;
        root.Children.Add(cardUsers);

        // Card 2: Windows Vault & Network Credentials
        var cardVault = new Border
        {
            Background = BgCard,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16),
            Margin = new Thickness(0, 0, 0, 14)
        };

        var spVault = new StackPanel();
        spVault.Children.Add(new TextBlock
        {
            Text = "WINDOWS VAULT & NETWORK SAVED CREDENTIALS (CMDKEY)",
            FontSize = 13,
            FontWeight = FontWeights.Bold,
            Foreground = SvllBlue,
            Margin = new Thickness(0, 0, 0, 6)
        });
        spVault.Children.Add(new TextBlock
        {
            Text = "Audits credentials stored in the Windows Credential Manager / Vault used for network drives, Remote Desktop sessions, and enterprise domains.",
            FontSize = 11,
            Foreground = TextSubtle,
            Margin = new Thickness(0, 0, 0, 10)
        });

        var vaultBtnRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
        var btnAuditVault = new Button
        {
            Content = "Read Vault Credentials (cmdkey /list)",
            Height = 30,
            Padding = new Thickness(12, 0, 12, 0),
            Margin = new Thickness(0, 0, 8, 0),
            Background = SvllBlue,
            Foreground = Brushes.White,
            FontWeight = FontWeights.SemiBold
        };
        btnAuditVault.Click += async (s, e) => await ReadVaultCredentialsAsync();
        vaultBtnRow.Children.Add(btnAuditVault);

        var btnOpenCredMgr = new Button
        {
            Content = "Open Windows Credential Manager GUI",
            Height = 30,
            Padding = new Thickness(12, 0, 12, 0),
            Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
            BorderBrush = BorderMuted
        };
        btnOpenCredMgr.Click += (s, e) => OpenTool("control.exe", "/name Microsoft.CredentialManager");
        vaultBtnRow.Children.Add(btnOpenCredMgr);

        spVault.Children.Add(vaultBtnRow);

        _txtVaultCredentials = new TextBox
        {
            Height = 160,
            FontFamily = new FontFamily("Consolas, monospace"),
            FontSize = 11,
            IsReadOnly = true,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
            BorderBrush = BorderMuted,
            Padding = new Thickness(8),
            Text = "Click 'Read Vault Credentials' to list active stored network credentials."
        };
        spVault.Children.Add(_txtVaultCredentials);

        cardVault.Child = spVault;
        root.Children.Add(cardVault);

        scroll.Content = root;

        // Auto audit users on first view load
        _ = AuditLocalUsersAsync();

        return scroll;
    }

    private async Task AuditLocalUsersAsync()
    {
        Log("\n[USERS] Auditing local SAM accounts...");
        _cachedUsers.Clear();

        await Task.Run(() =>
        {
            try
            {
                var p = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "net.exe",
                        Arguments = "user",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };
                p.Start();
                string output = p.StandardOutput.ReadToEnd();
                p.WaitForExit();

                var lines = output.Split('\n');
                bool capture = false;
                var names = new List<string>();

                foreach (var line in lines)
                {
                    if (line.Contains("-------------------------------------------------------------------------------"))
                    {
                        capture = true;
                        continue;
                    }
                    if (line.Contains("The command completed successfully."))
                    {
                        capture = false;
                        break;
                    }
                    if (capture)
                    {
                        var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var part in parts)
                        {
                            if (!string.IsNullOrWhiteSpace(part)) names.Add(part.Trim());
                        }
                    }
                }

                // Query details for each user
                foreach (var name in names)
                {
                    var user = new LocalUserAccount { Username = name };
                    var p2 = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "net.exe",
                            Arguments = $"user \"{name}\"",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            CreateNoWindow = true
                        }
                    };
                    p2.Start();
                    string details = p2.StandardOutput.ReadToEnd();
                    p2.WaitForExit();

                    if (details.Contains("Account active               Yes")) user.IsActive = true;
                    else if (details.Contains("Account active               No")) user.IsActive = false;

                    if (details.Contains("*Administrators")) user.IsAdmin = true;

                    var mFull = Regex.Match(details, @"Full Name\s+(.+)");
                    if (mFull.Success) user.FullName = mFull.Groups[1].Value.Trim();

                    var mDesc = Regex.Match(details, @"Comment\s+(.+)");
                    if (mDesc.Success) user.Description = mDesc.Groups[1].Value.Trim();

                    _cachedUsers.Add(user);
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => Log($"[USERS ERROR] {ex.Message}"));
            }
        });

        _listUsers.ItemsSource = null;
        _listUsers.ItemsSource = _cachedUsers.ToList();
        Log($"[USERS] Found {_cachedUsers.Count} configured local accounts.");
    }

    private async Task ResetSelectedUserPasswordAsync()
    {
        if (_listUsers.SelectedItem is not LocalUserAccount user)
        {
            MessageBox.Show("Please select a target user from the accounts table.", "Selection Required", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var dlg = new ResetPasswordDialog(user.Username) { Owner = this };
        if (dlg.ShowDialog() == true && !string.IsNullOrEmpty(dlg.NewPassword))
        {
            Log($"[USERS] Resetting administrative password for user: [{user.Username}]...");
            await ExecuteAsync("net.exe", $"user \"{user.Username}\" \"{dlg.NewPassword}\"");
            MessageBox.Show($"Password for '{user.Username}' was reset successfully!", "Password Updated", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private async Task ToggleSelectedUserActiveAsync()
    {
        if (_listUsers.SelectedItem is not LocalUserAccount user)
        {
            MessageBox.Show("Please select a target user.", "Selection Required", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        string newStatus = user.IsActive ? "no" : "yes";
        Log($"[USERS] Setting account active status for '{user.Username}' to {newStatus}...");
        await ExecuteAsync("net.exe", $"user \"{user.Username}\" /active:{newStatus}");
        await AuditLocalUsersAsync();
    }

    private async Task PromptAddLocalUserAsync()
    {
        string username = "SVLL_Support";
        string password = "ChangeMe@2026!";

        if (MessageBox.Show($"Create local administrative user [{username}] with temporary password [{password}]?", "Confirm User Creation", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            Log($"[USERS] Creating local account: {username}...");
            await ExecuteAsync("net.exe", $"user {username} {password} /add");
            await ExecuteAsync("net.exe", $"localgroup Administrators {username} /add");
            await AuditLocalUsersAsync();
            MessageBox.Show($"User {username} created and added to Administrators group!", "Account Created", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private async Task ReadVaultCredentialsAsync()
    {
        Log("\n[VAULT] Querying stored Windows credentials via cmdkey...");
        string output = await ExecuteAsync("cmdkey.exe", "/list");
        _txtVaultCredentials.Text = string.IsNullOrWhiteSpace(output) ? "No stored network credentials found." : output;
    }

    private async Task AuditCurrentSessionUserSecurityAsync()
    {
        string currentUser = Environment.UserName;
        Log($"\n[SECURITY AUDIT] Analyzing authentication & security architecture for active session user: [{currentUser}]...");

        string details = await ExecuteAsync("net.exe", $"user \"{currentUser}\"");

        var sb = new StringBuilder();
        sb.AppendLine("=== WINDOWS LOCAL USER AUTHENTICATION & CREDENTIAL ARCHITECTURE ===");
        sb.AppendLine($"• Logged-in User Account: {currentUser}");
        sb.AppendLine($"• User Domain / Machine:  {Environment.UserDomainName}");
        sb.AppendLine();
        sb.AppendLine("SECURITY NOTICE (WHY WINDOWS PREVENTS VIEWING PLAINTEXT PASSWORDS):");
        sb.AppendLine("Under Microsoft Windows NT architecture (and modern OS security standards), user passwords");
        sb.AppendLine("are NEVER stored in plaintext on disk or in the SAM database. Instead, Windows computes a");
        sb.AppendLine("one-way irreversible cryptographic hash (NTLM / Kerberos). When you log in, Windows hashes");
        sb.AppendLine("what you typed and compares the hashes—it cannot recover or display the original plaintext password.");
        sb.AppendLine();
        sb.AppendLine("ADMINISTRATIVE ACTIONS AVAILABLE:");
        sb.AppendLine("1. If a password is forgotten, administrators perform a 1-Click Password Reset ('net user <username> <new_pass>').");
        sb.AppendLine("2. For Wi-Fi passwords, the Workstation extracts pre-shared keys via 'Wi-Fi Keys & Diagnostics'.");
        sb.AppendLine("3. For saved network drive/RDP passwords, check the 'Windows Vault & Saved Credentials' section below.");
        sb.AppendLine();
        sb.AppendLine("--- ACTIVE USER SAM ACCOUNT ATTRIBUTES ---");
        sb.AppendLine(details);

        _txtVaultCredentials.Text = sb.ToString();
        MessageBox.Show($"Security Audit for [{currentUser}]:\n\nIn accordance with Windows NT security architecture, passwords are protected by one-way irreversible cryptographic hashes and cannot be viewed in plaintext.\n\nTo change or reset the user's password, use the '1-Click Reset Password' button above.", "Windows Authentication Security", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private async Task<bool> EnsureWinGetReadyAsync()
    {
        bool available = await Task.Run(() =>
        {
            try
            {
                var p = Process.Start(new ProcessStartInfo
                {
                    FileName = "where.exe",
                    Arguments = "winget",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
                p?.WaitForExit(2000);
                return p?.ExitCode == 0;
            }
            catch { return false; }
        });

        if (!available)
        {
            var res = MessageBox.Show(
                "Windows Package Manager (WinGet) was not found in your system PATH.\n\n" +
                "On Windows 10, WinGet is provided by the Microsoft 'App Installer' package.\n\n" +
                "Would you like to open the Microsoft Store page to install/update App Installer?",
                "WinGet Not Found (Windows 10 / 11)",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);

            if (res == MessageBoxResult.Yes)
            {
                try
                {
                    Process.Start(new ProcessStartInfo { FileName = "ms-windows-store://pdp/?productid=9NBLGGH4NNS1", UseShellExecute = true });
                }
                catch
                {
                    Process.Start(new ProcessStartInfo { FileName = "https://apps.microsoft.com/detail/9nblggh4nns1", UseShellExecute = true });
                }
            }
            return false;
        }
        return true;
    }

    #endregion
}
