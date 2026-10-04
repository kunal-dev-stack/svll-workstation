using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
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
    // Network Adapter State
    private ComboBox _cmbAdapters = null!;
    private ComboBox _cmbProfiles = null!;
    private TextBlock _txtProfileMeta = null!;
    private RadioButton _rbModeDhcp = null!;
    private RadioButton _rbModeStatic = null!;
    private StackPanel _panelStaticConfig = null!;
    private TextBox _txtStaticIp = null!;
    private ComboBox _cmbStaticSubnet = null!;
    private TextBox _txtStaticGw = null!;
    private TextBox _txtStaticDns1 = null!;
    private TextBox _txtStaticDns2 = null!;

    // Subnet Scanner State
    private TextBox _txtSubnetPrefix = null!;
    private ProgressBar _progSubnetScan = null!;
    private TextBlock _lblSubnetStats = null!;
    private ListView _listSubnetHosts = null!;
    private readonly List<SubnetHostEntry> _allSubnetEntries = new List<SubnetHostEntry>();
    private string _currentSubnetFilter = "ALL";
    private CancellationTokenSource? _scanCts;

    // LAN Share State
    private TextBox _txtSharePath = null!;
    private ListView _listShareFiles = null!;
    private readonly List<NetworkShareItem> _shareItems = new List<NetworkShareItem>();

    #region 1. Network & DNS Profiles View

    private UIElement BuildNetworkView()
    {
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var root = new StackPanel { Margin = new Thickness(0, 0, 10, 0) };

        // Card 1: Network Adapter Selection & Active State
        var cardAdapter = new Border
        {
            Background = BgCard,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16),
            Margin = new Thickness(0, 0, 0, 14)
        };

        var spAdapter = new StackPanel();
        spAdapter.Children.Add(new TextBlock
        {
            Text = "SELECT TARGET NETWORK INTERFACE",
            FontSize = 12,
            FontWeight = FontWeights.Bold,
            Foreground = SvllBlue,
            Margin = new Thickness(0, 0, 0, 8)
        });

        var gridAdapter = new Grid();
        gridAdapter.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        gridAdapter.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        _cmbAdapters = new ComboBox { Height = 32, FontSize = 12, Margin = new Thickness(0, 0, 10, 0) };
        PopulateAdapters();
        Grid.SetColumn(_cmbAdapters, 0);
        gridAdapter.Children.Add(_cmbAdapters);

        var btnRefreshAdapters = new Button
        {
            Content = "Refresh Adapters",
            Height = 32,
            Padding = new Thickness(12, 0, 12, 0),
            Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
            BorderBrush = BorderMuted,
            Cursor = Cursors.Hand
        };
        btnRefreshAdapters.Click += (s, e) => PopulateAdapters();
        Grid.SetColumn(btnRefreshAdapters, 1);
        gridAdapter.Children.Add(btnRefreshAdapters);

        spAdapter.Children.Add(gridAdapter);
        cardAdapter.Child = spAdapter;
        root.Children.Add(cardAdapter);

        // Card 2: Preset Profiles Manager
        var cardProfiles = new Border
        {
            Background = BgCard,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16),
            Margin = new Thickness(0, 0, 0, 14)
        };

        var spProfiles = new StackPanel();
        spProfiles.Children.Add(new TextBlock
        {
            Text = "BRANCH NETWORK IP & DNS PROFILES",
            FontSize = 12,
            FontWeight = FontWeights.Bold,
            Foreground = SvllBlue,
            Margin = new Thickness(0, 0, 0, 8)
        });

        var gridProfBar = new Grid();
        gridProfBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        gridProfBar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        _cmbProfiles = new ComboBox { Height = 32, FontSize = 12, Margin = new Thickness(0, 0, 10, 0) };
        RefreshProfilesDropdown();
        _cmbProfiles.SelectionChanged += (s, e) => OnProfileSelected();
        Grid.SetColumn(_cmbProfiles, 0);
        gridProfBar.Children.Add(_cmbProfiles);

        var profButtons = new StackPanel { Orientation = Orientation.Horizontal };
        var btnNewProf = new Button { Content = "New Profile", Height = 32, Padding = new Thickness(10, 0, 10, 0), Margin = new Thickness(0, 0, 6, 0), Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)), BorderBrush = BorderMuted };
        btnNewProf.Click += (s, e) => OpenNewProfileDialog();
        profButtons.Children.Add(btnNewProf);

        var btnEditProf = new Button { Content = "Edit", Height = 32, Padding = new Thickness(10, 0, 10, 0), Margin = new Thickness(0, 0, 6, 0), Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)), BorderBrush = BorderMuted };
        btnEditProf.Click += (s, e) => EditSelectedProfile();
        profButtons.Children.Add(btnEditProf);

        var btnDelProf = new Button { Content = "Delete", Height = 32, Padding = new Thickness(10, 0, 10, 0), Margin = new Thickness(0, 0, 6, 0), Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)), BorderBrush = BorderMuted };
        btnDelProf.Click += (s, e) => DeleteSelectedProfile();
        profButtons.Children.Add(btnDelProf);

        var btnExportProf = new Button { Content = "Export JSON", Height = 32, Padding = new Thickness(10, 0, 10, 0), Margin = new Thickness(0, 0, 6, 0), Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)), BorderBrush = BorderMuted };
        btnExportProf.Click += (s, e) => ExportProfilesJson();
        profButtons.Children.Add(btnExportProf);

        var btnImportProf = new Button { Content = "Import JSON", Height = 32, Padding = new Thickness(10, 0, 10, 0), Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)), BorderBrush = BorderMuted };
        btnImportProf.Click += (s, e) => ImportProfilesJson();
        profButtons.Children.Add(btnImportProf);

        Grid.SetColumn(profButtons, 1);
        gridProfBar.Children.Add(profButtons);
        spProfiles.Children.Add(gridProfBar);

        _txtProfileMeta = new TextBlock
        {
            Text = "Select a profile to load configuration parameters.",
            FontSize = 11,
            Foreground = TextSubtle,
            Margin = new Thickness(0, 8, 0, 0)
        };
        spProfiles.Children.Add(_txtProfileMeta);

        cardProfiles.Child = spProfiles;
        root.Children.Add(cardProfiles);

        // Card 3: Live Adapter Configuration Editor
        var cardConfig = new Border
        {
            Background = BgCard,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16),
            Margin = new Thickness(0, 0, 0, 14)
        };

        var spConfig = new StackPanel();
        spConfig.Children.Add(new TextBlock
        {
            Text = "IP ADDRESS & DNS ASSIGNMENT",
            FontSize = 12,
            FontWeight = FontWeights.Bold,
            Foreground = SvllBlue,
            Margin = new Thickness(0, 0, 0, 12)
        });

        var rbRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 12) };
        _rbModeDhcp = new RadioButton { Content = "Obtain an IP address automatically (DHCP)", IsChecked = true, Margin = new Thickness(0, 0, 24, 0), FontWeight = FontWeights.SemiBold };
        _rbModeStatic = new RadioButton { Content = "Use the following Static IP address", FontWeight = FontWeights.SemiBold };
        _rbModeDhcp.Checked += (s, e) => _panelStaticConfig.Visibility = Visibility.Collapsed;
        _rbModeStatic.Checked += (s, e) => _panelStaticConfig.Visibility = Visibility.Visible;
        rbRow.Children.Add(_rbModeDhcp);
        rbRow.Children.Add(_rbModeStatic);
        spConfig.Children.Add(rbRow);

        _panelStaticConfig = new StackPanel { Visibility = Visibility.Collapsed, Margin = new Thickness(0, 0, 0, 12) };
        var ipGrid = new Grid();
        ipGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        ipGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        ipGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var ipCol1 = new StackPanel { Margin = new Thickness(0, 0, 8, 0) };
        ipCol1.Children.Add(new TextBlock { Text = "IP Address:", FontSize = 11, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 4) });
        _txtStaticIp = new TextBox { Height = 28, Padding = new Thickness(6, 2, 6, 2), Text = "192.168.1.100" };
        ipCol1.Children.Add(_txtStaticIp);
        Grid.SetColumn(ipCol1, 0);
        ipGrid.Children.Add(ipCol1);

        var ipCol2 = new StackPanel { Margin = new Thickness(8, 0, 8, 0) };
        ipCol2.Children.Add(new TextBlock { Text = "Subnet Mask:", FontSize = 11, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 4) });
        _cmbStaticSubnet = new ComboBox { Height = 28, ItemsSource = new[] { "255.255.255.0 (/24)", "255.255.0.0 (/16)", "255.0.0.0 (/8)", "255.255.255.128 (/25)", "255.255.255.192 (/26)", "255.255.255.240 (/28)" }, SelectedIndex = 0 };
        ipCol2.Children.Add(_cmbStaticSubnet);
        Grid.SetColumn(ipCol2, 1);
        ipGrid.Children.Add(ipCol2);

        var ipCol3 = new StackPanel { Margin = new Thickness(8, 0, 0, 0) };
        ipCol3.Children.Add(new TextBlock { Text = "Default Gateway:", FontSize = 11, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 4) });
        _txtStaticGw = new TextBox { Height = 28, Padding = new Thickness(6, 2, 6, 2), Text = "192.168.1.1" };
        ipCol3.Children.Add(_txtStaticGw);
        Grid.SetColumn(ipCol3, 2);
        ipGrid.Children.Add(ipCol3);

        _panelStaticConfig.Children.Add(ipGrid);
        spConfig.Children.Add(_panelStaticConfig);

        // DNS Servers
        var dnsGrid = new Grid { Margin = new Thickness(0, 0, 0, 16) };
        dnsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        dnsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        dnsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var dnsCol1 = new StackPanel { Margin = new Thickness(0, 0, 8, 0) };
        dnsCol1.Children.Add(new TextBlock { Text = "Preferred DNS Server:", FontSize = 11, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 4) });
        _txtStaticDns1 = new TextBox { Height = 28, Padding = new Thickness(6, 2, 6, 2), Text = "8.8.8.8" };
        dnsCol1.Children.Add(_txtStaticDns1);
        Grid.SetColumn(dnsCol1, 0);
        dnsGrid.Children.Add(dnsCol1);

        var dnsCol2 = new StackPanel { Margin = new Thickness(8, 0, 8, 0) };
        dnsCol2.Children.Add(new TextBlock { Text = "Alternate DNS Server:", FontSize = 11, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 4) });
        _txtStaticDns2 = new TextBox { Height = 28, Padding = new Thickness(6, 2, 6, 2), Text = "8.8.4.4" };
        dnsCol2.Children.Add(_txtStaticDns2);
        Grid.SetColumn(dnsCol2, 1);
        dnsGrid.Children.Add(dnsCol2);

        var dnsCol3 = new StackPanel { Margin = new Thickness(8, 0, 0, 0) };
        dnsCol3.Children.Add(new TextBlock { Text = "Quick DNS Preset:", FontSize = 11, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 4) });
        var cmbDnsPreset = new ComboBox { Height = 28, ItemsSource = new[] { "Google DNS (8.8.8.8)", "Cloudflare DNS (1.1.1.1)", "Quad9 Secure (9.9.9.9)", "OpenDNS Home (208.67.222.222)" }, SelectedIndex = 0 };
        cmbDnsPreset.SelectionChanged += (s, e) =>
        {
            switch (cmbDnsPreset.SelectedIndex)
            {
                case 0: _txtStaticDns1.Text = "8.8.8.8"; _txtStaticDns2.Text = "8.8.4.4"; break;
                case 1: _txtStaticDns1.Text = "1.1.1.1"; _txtStaticDns2.Text = "1.0.0.1"; break;
                case 2: _txtStaticDns1.Text = "9.9.9.9"; _txtStaticDns2.Text = "149.112.112.112"; break;
                case 3: _txtStaticDns1.Text = "208.67.222.222"; _txtStaticDns2.Text = "208.67.220.220"; break;
            }
        };
        dnsCol3.Children.Add(cmbDnsPreset);
        Grid.SetColumn(dnsCol3, 2);
        dnsGrid.Children.Add(dnsCol3);

        spConfig.Children.Add(dnsGrid);

        var btnApplyNet = new Button
        {
            Content = "Apply Configuration to Active Adapter",
            Height = 36,
            Background = SvllBlue,
            Foreground = Brushes.White,
            FontWeight = FontWeights.Bold,
            Cursor = Cursors.Hand
        };
        btnApplyNet.Click += async (s, e) => await ApplyNetworkConfigClick();
        spConfig.Children.Add(btnApplyNet);

        cardConfig.Child = spConfig;
        root.Children.Add(cardConfig);

        scroll.Content = root;
        return scroll;
    }

    private void PopulateAdapters()
    {
        _cmbAdapters.Items.Clear();
        var adapters = NetworkInterface.GetAllNetworkInterfaces()
            .Where(n => n.NetworkInterfaceType != NetworkInterfaceType.Loopback && n.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
            .OrderByDescending(n => n.OperationalStatus == OperationalStatus.Up)
            .ToList();

        foreach (var a in adapters)
        {
            _cmbAdapters.Items.Add($"{a.Name} ({a.NetworkInterfaceType} - {a.OperationalStatus})");
        }
        if (_cmbAdapters.Items.Count > 0) _cmbAdapters.SelectedIndex = 0;
    }

    private void RefreshProfilesDropdown()
    {
        _cmbProfiles.Items.Clear();
        foreach (var p in _profilesList)
        {
            _cmbProfiles.Items.Add(p.ToString());
        }
        if (_cmbProfiles.Items.Count > 0) _cmbProfiles.SelectedIndex = 0;
    }

    private void OnProfileSelected()
    {
        int idx = _cmbProfiles.SelectedIndex;
        if (idx >= 0 && idx < _profilesList.Count)
        {
            var p = _profilesList[idx];
            _txtProfileMeta.Text = $"Selected: {p.ProfileName} | Branch: {p.BranchTag} | Mode: {(p.IsDhcp ? "DHCP" : "Static")}\nNotes: {p.Notes}";
            if (p.IsDhcp)
            {
                _rbModeDhcp.IsChecked = true;
            }
            else
            {
                _rbModeStatic.IsChecked = true;
                _txtStaticIp.Text = p.IpAddress;
                _txtStaticGw.Text = p.Gateway;
                _txtStaticDns1.Text = p.PrimaryDns;
                _txtStaticDns2.Text = p.SecondaryDns;
            }
        }
    }

    private void OpenNewProfileDialog()
    {
        var dlg = new ProfileEditorDialog { Owner = this };
        if (dlg.ShowDialog() == true)
        {
            _profilesList.Add(dlg.ResultProfile);
            SaveProfilesToDisk();
            RefreshProfilesDropdown();
            _cmbProfiles.SelectedIndex = _profilesList.Count - 1;
            Log($"[PROFILE] Created new network profile: '{dlg.ResultProfile.ProfileName}'");
        }
    }

    private void EditSelectedProfile()
    {
        int idx = _cmbProfiles.SelectedIndex;
        if (idx < 0 || idx >= _profilesList.Count) return;

        var existing = _profilesList[idx];
        var dlg = new ProfileEditorDialog(existing) { Owner = this };
        if (dlg.ShowDialog() == true)
        {
            _profilesList[idx] = dlg.ResultProfile;
            SaveProfilesToDisk();
            RefreshProfilesDropdown();
            _cmbProfiles.SelectedIndex = idx;
            Log($"[PROFILE] Updated network profile: '{dlg.ResultProfile.ProfileName}'");
        }
    }

    private void DeleteSelectedProfile()
    {
        int idx = _cmbProfiles.SelectedIndex;
        if (idx < 0 || idx >= _profilesList.Count) return;

        var target = _profilesList[idx];
        if (MessageBox.Show($"Are you sure you want to delete profile '{target.ProfileName}'?", "Confirm Deletion", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            _profilesList.RemoveAt(idx);
            SaveProfilesToDisk();
            RefreshProfilesDropdown();
            Log($"[PROFILE] Deleted network profile: '{target.ProfileName}'");
        }
    }

    private void ExportProfilesJson()
    {
        var sfd = new SaveFileDialog
        {
            Filter = "JSON Files (*.json)|*.json",
            FileName = "SVLL_Network_Profiles.json"
        };
        if (sfd.ShowDialog() == true)
        {
            File.WriteAllText(sfd.FileName, JsonSerializer.Serialize(_profilesList, new JsonSerializerOptions { WriteIndented = true }));
            Log($"[PROFILE] Exported {_profilesList.Count} profiles to {sfd.FileName}");
        }
    }

    private void ImportProfilesJson()
    {
        var ofd = new OpenFileDialog { Filter = "JSON Files (*.json)|*.json" };
        if (ofd.ShowDialog() == true)
        {
            try
            {
                var list = JsonSerializer.Deserialize<List<NetworkProfile>>(File.ReadAllText(ofd.FileName));
                if (list != null)
                {
                    foreach (var item in list)
                    {
                        _profilesList.RemoveAll(x => x.ProfileName.Equals(item.ProfileName, StringComparison.OrdinalIgnoreCase));
                        _profilesList.Add(item);
                    }
                    SaveProfilesToDisk();
                    RefreshProfilesDropdown();
                    Log($"[PROFILE] Imported {list.Count} profiles from {ofd.FileName}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to parse profiles: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void LoadProfilesFromDisk()
    {
        try
        {
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SVLL_IT_Workstation", "profiles.json");
            if (File.Exists(path))
            {
                _profilesList = JsonSerializer.Deserialize<List<NetworkProfile>>(File.ReadAllText(path)) ?? new List<NetworkProfile>();
            }
        }
        catch { }

        if (_profilesList.Count == 0)
        {
            _profilesList.Add(new NetworkProfile { ProfileName = "Corporate Automatic (DHCP)", BranchTag = "Global", IsDhcp = true, PrimaryDns = "8.8.8.8", SecondaryDns = "8.8.4.4", Notes = "Standard fleet DHCP configuration" });
            _profilesList.Add(new NetworkProfile { ProfileName = "Raipur Logistics Hub", BranchTag = "Raipur", IsDhcp = false, IpAddress = "192.168.10.150", SubnetMask = "255.255.255.0", Gateway = "192.168.10.1", PrimaryDns = "192.168.10.1", SecondaryDns = "8.8.8.8", Notes = "Raipur Central Logistics static IP" });
            _profilesList.Add(new NetworkProfile { ProfileName = "Warehouse VLAN 20", BranchTag = "WH-Central", IsDhcp = false, IpAddress = "10.20.1.55", SubnetMask = "255.255.255.0", Gateway = "10.20.1.1", PrimaryDns = "10.20.1.1", SecondaryDns = "1.1.1.1", Notes = "VLAN 20 for inventory scanning terminals" });
        }
    }

    private void SaveProfilesToDisk()
    {
        try
        {
            string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SVLL_IT_Workstation");
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "profiles.json"), JsonSerializer.Serialize(_profilesList, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }

    private async Task ApplyNetworkConfigClick()
    {
        if (_cmbAdapters.SelectedIndex < 0)
        {
            MessageBox.Show("Please select a target network adapter.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        string rawAdapter = _cmbAdapters.SelectedItem.ToString() ?? "";
        string adapterName = rawAdapter.Split('(')[0].Trim();

        Log($"\n[NETSH] Applying configuration to adapter: [{adapterName}]...");

        if (_rbModeDhcp.IsChecked == true)
        {
            await ExecuteAsync("netsh.exe", $"interface ip set address name=\"{adapterName}\" source=dhcp");
            await ExecuteAsync("netsh.exe", $"interface ip set dns name=\"{adapterName}\" source=dhcp");
            Log("[NETSH] Successfully reset adapter to DHCP automatic IP and DNS.");
        }
        else
        {
            string ip = _txtStaticIp.Text.Trim();
            string sub = _cmbStaticSubnet.Text.Split(' ')[0].Trim();
            string gw = _txtStaticGw.Text.Trim();
            string dns1 = _txtStaticDns1.Text.Trim();
            string dns2 = _txtStaticDns2.Text.Trim();

            await ExecuteAsync("netsh.exe", $"interface ip set address name=\"{adapterName}\" static {ip} {sub} {gw}");
            if (!string.IsNullOrEmpty(dns1))
                await ExecuteAsync("netsh.exe", $"interface ip set dns name=\"{adapterName}\" static {dns1} primary");
            if (!string.IsNullOrEmpty(dns2))
                await ExecuteAsync("netsh.exe", $"interface ip add dns name=\"{adapterName}\" {dns2} index=2");

            Log($"[NETSH] Successfully assigned Static IP {ip}/{sub} (Gateway: {gw}, DNS: {dns1}, {dns2}).");
        }

        UpdateLiveVitals();
    }

    #endregion

    #region 2. Subnet IP Scanner (Free vs. Occupied)

    private UIElement BuildSubnetScannerView()
    {
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var root = new StackPanel { Margin = new Thickness(0, 0, 10, 0) };

        // Configuration Card
        var cardConfig = new Border
        {
            Background = BgCard,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16),
            Margin = new Thickness(0, 0, 0, 14)
        };

        var spConfig = new StackPanel();
        spConfig.Children.Add(new TextBlock
        {
            Text = "CLASS-C SUBNET IP SCANNER (CONCURRENT SCANNER)",
            FontSize = 13,
            FontWeight = FontWeights.Bold,
            Foreground = SvllBlue,
            Margin = new Thickness(0, 0, 0, 6)
        });
        spConfig.Children.Add(new TextBlock
        {
            Text = "Discovers all 254 endpoints on the local subnet, flags occupied vs. free IP addresses, resolves network hostnames, and exports unassigned IPs for static assignment.",
            FontSize = 11,
            Foreground = TextSubtle,
            Margin = new Thickness(0, 0, 0, 12)
        });

        var scanBar = new Grid();
        scanBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(220) });
        scanBar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        scanBar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        scanBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var subInputStack = new StackPanel { Margin = new Thickness(0, 0, 10, 0) };
        subInputStack.Children.Add(new TextBlock { Text = "Subnet Prefix (e.g. 192.168.1):", FontSize = 10.5, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 4) });

        string detectedPrefix = DetectLocalSubnetPrefix();
        _txtSubnetPrefix = new TextBox { Text = detectedPrefix, Height = 30, FontSize = 12, Padding = new Thickness(6, 4, 6, 4) };
        subInputStack.Children.Add(_txtSubnetPrefix);
        Grid.SetColumn(subInputStack, 0);
        scanBar.Children.Add(subInputStack);

        var btnStartScan = new Button
        {
            Content = " Start Subnet Scan (1-254) ",
            Height = 30,
            Background = SvllBlue,
            Foreground = Brushes.White,
            FontWeight = FontWeights.Bold,
            Padding = new Thickness(14, 0, 14, 0),
            Margin = new Thickness(0, 18, 8, 0),
            Cursor = Cursors.Hand
        };
        btnStartScan.Click += async (s, e) => await StartSubnetScanAsync();
        Grid.SetColumn(btnStartScan, 1);
        scanBar.Children.Add(btnStartScan);

        var btnStopScan = new Button
        {
            Content = "Stop",
            Height = 30,
            Padding = new Thickness(12, 0, 12, 0),
            Margin = new Thickness(0, 18, 10, 0),
            Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
            BorderBrush = BorderMuted
        };
        btnStopScan.Click += (s, e) => _scanCts?.Cancel();
        Grid.SetColumn(btnStopScan, 2);
        scanBar.Children.Add(btnStopScan);

        _progSubnetScan = new ProgressBar
        {
            Height = 12,
            Minimum = 0,
            Maximum = 254,
            Value = 0,
            Margin = new Thickness(10, 27, 0, 0),
            Visibility = Visibility.Collapsed
        };
        Grid.SetColumn(_progSubnetScan, 3);
        scanBar.Children.Add(_progSubnetScan);

        spConfig.Children.Add(scanBar);
        cardConfig.Child = spConfig;
        root.Children.Add(cardConfig);

        // Results Card
        var cardResults = new Border
        {
            Background = BgCard,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16),
            Margin = new Thickness(0, 0, 0, 14)
        };

        var spResults = new StackPanel();

        // Filter & Export toolbar
        var toolBar = new DockPanel { Margin = new Thickness(0, 0, 0, 10) };

        var filterGroup = new StackPanel { Orientation = Orientation.Horizontal };
        var btnFilterAll = CreateFilterButton("All (254)", () => FilterSubnetList("ALL"));
        var btnFilterOccupied = CreateFilterButton("Occupied Only", () => FilterSubnetList("OCCUPIED"));
        var btnFilterFree = CreateFilterButton("Free / Unassigned Only", () => FilterSubnetList("FREE"));
        filterGroup.Children.Add(btnFilterAll);
        filterGroup.Children.Add(btnFilterOccupied);
        filterGroup.Children.Add(btnFilterFree);
        DockPanel.SetDock(filterGroup, Dock.Left);
        toolBar.Children.Add(filterGroup);

        var exportGroup = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var btnCopyFree = new Button
        {
            Content = "Copy Free IPs",
            Height = 28,
            Padding = new Thickness(10, 0, 10, 0),
            Margin = new Thickness(0, 0, 6, 0),
            Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
            BorderBrush = BorderMuted
        };
        btnCopyFree.Click += (s, e) => CopyFreeIpsToClipboard();
        exportGroup.Children.Add(btnCopyFree);

        var btnExportTxt = new Button
        {
            Content = "Export Free IPs (.TXT)",
            Height = 28,
            Padding = new Thickness(10, 0, 10, 0),
            Margin = new Thickness(0, 0, 6, 0),
            Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
            BorderBrush = BorderMuted
        };
        btnExportTxt.Click += (s, e) => ExportFreeIpsTxt();
        exportGroup.Children.Add(btnExportTxt);

        var btnExportCsv = new Button
        {
            Content = "Export Full Audit (.CSV)",
            Height = 28,
            Padding = new Thickness(10, 0, 10, 0),
            Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
            BorderBrush = BorderMuted
        };
        btnExportCsv.Click += (s, e) => ExportSubnetCsv();
        exportGroup.Children.Add(btnExportCsv);

        toolBar.Children.Add(exportGroup);
        spResults.Children.Add(toolBar);

        _lblSubnetStats = new TextBlock
        {
            Text = "Status: Ready to scan Class-C subnet.",
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Foreground = TextSubtle,
            Margin = new Thickness(0, 0, 0, 8)
        };
        spResults.Children.Add(_lblSubnetStats);

        // List View of scanned hosts
        _listSubnetHosts = new ListView
        {
            Height = 360,
            FontSize = 11.5,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1)
        };

        var gridView = new GridView();
        gridView.Columns.Add(new GridViewColumn { Header = "IP Address", Width = 150, DisplayMemberBinding = new Binding("IpAddress") });
        gridView.Columns.Add(new GridViewColumn { Header = "Host Status", Width = 150, DisplayMemberBinding = new Binding("DisplayStatus") });
        gridView.Columns.Add(new GridViewColumn { Header = "Resolved Hostname", Width = 280, DisplayMemberBinding = new Binding("Hostname") });
        gridView.Columns.Add(new GridViewColumn { Header = "Latency", Width = 100, DisplayMemberBinding = new Binding("LatencyMs") });

        _listSubnetHosts.View = gridView;
        spResults.Children.Add(_listSubnetHosts);

        cardResults.Child = spResults;
        root.Children.Add(cardResults);

        scroll.Content = root;
        return scroll;
    }

    private Button CreateFilterButton(string label, Action action)
    {
        var b = new Button
        {
            Content = label,
            Height = 28,
            Padding = new Thickness(10, 0, 10, 0),
            Margin = new Thickness(0, 0, 6, 0),
            Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
            BorderBrush = BorderMuted,
            Cursor = Cursors.Hand
        };
        b.Click += (s, e) => action();
        return b;
    }

    private string DetectLocalSubnetPrefix()
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
                var ip = active.GetIPProperties().UnicastAddresses
                    .FirstOrDefault(u => u.Address.AddressFamily == AddressFamily.InterNetwork
                                         && !u.Address.ToString().StartsWith("127.")
                                         && !u.Address.ToString().StartsWith("169.254"));
                if (ip != null)
                {
                    string[] parts = ip.Address.ToString().Split('.');
                    if (parts.Length == 4)
                        return $"{parts[0]}.{parts[1]}.{parts[2]}";
                }
            }
        }
        catch { }
        return "192.168.1";
    }

    private async Task StartSubnetScanAsync()
    {
        string prefix = _txtSubnetPrefix.Text.Trim();
        if (string.IsNullOrEmpty(prefix))
        {
            MessageBox.Show("Please enter a valid subnet prefix (e.g. 192.168.1).", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _scanCts?.Cancel();
        _scanCts = new CancellationTokenSource();
        var token = _scanCts.Token;

        _allSubnetEntries.Clear();
        _listSubnetHosts.ItemsSource = null;
        _progSubnetScan.Visibility = Visibility.Visible;
        _progSubnetScan.Value = 0;
        _lblSubnetStats.Text = $"Scanning {prefix}.1 - {prefix}.254...";

        var results = new List<SubnetHostEntry>();
        var sem = new SemaphoreSlim(32); // 32 concurrent ping threads for blazingly fast scan
        var tasks = new List<Task>();

        int completed = 0;

        for (int i = 1; i <= 254; i++)
        {
            string hostIp = $"{prefix}.{i}";
            tasks.Add(Task.Run(async () =>
            {
                await sem.WaitAsync(token);
                try
                {
                    if (token.IsCancellationRequested) return;

                    using var p = new Ping();
                    var reply = await p.SendPingAsync(hostIp, 350);

                    var entry = new SubnetHostEntry { IpAddress = hostIp };

                    if (reply.Status == IPStatus.Success)
                    {
                        entry.Status = "Occupied";
                        entry.LatencyMs = reply.RoundtripTime;
                        try
                        {
                            var hostEntry = await Dns.GetHostEntryAsync(hostIp);
                            entry.Hostname = hostEntry.HostName;
                        }
                        catch
                        {
                            entry.Hostname = "Online Host";
                        }
                    }
                    else
                    {
                        entry.Status = "Free";
                        entry.LatencyMs = -1;
                        entry.Hostname = "-";
                    }

                    lock (results) { results.Add(entry); }
                }
                catch { }
                finally
                {
                    sem.Release();
                    int c = Interlocked.Increment(ref completed);
                    Dispatcher.Invoke(() => _progSubnetScan.Value = c);
                }
            }, token));
        }

        try
        {
            await Task.WhenAll(tasks);
        }
        catch (OperationCanceledException)
        {
            Log("[SCANNER] Subnet scan stopped by operator.");
        }

        _allSubnetEntries.AddRange(results.OrderBy(x =>
        {
            var parts = x.IpAddress.Split('.');
            return int.TryParse(parts.Last(), out int n) ? n : 0;
        }));

        FilterSubnetList(_currentSubnetFilter);
        _progSubnetScan.Visibility = Visibility.Collapsed;

        int occupiedCount = _allSubnetEntries.Count(x => x.IsOccupied);
        int freeCount = _allSubnetEntries.Count(x => !x.IsOccupied);
        _lblSubnetStats.Text = $"Scan Complete! Found: {occupiedCount} Occupied Endpoints, {freeCount} Free/Unassigned IP Addresses.";
        Log($"[SCANNER] Subnet scan of {prefix}.0/24 finished: {occupiedCount} occupied, {freeCount} free.");
    }

    private void FilterSubnetList(string filter)
    {
        _currentSubnetFilter = filter;
        IEnumerable<SubnetHostEntry> filtered = _allSubnetEntries;

        if (filter == "OCCUPIED")
            filtered = _allSubnetEntries.Where(x => x.IsOccupied);
        else if (filter == "FREE")
            filtered = _allSubnetEntries.Where(x => !x.IsOccupied);

        _listSubnetHosts.ItemsSource = filtered.ToList();
    }

    private void CopyFreeIpsToClipboard()
    {
        var freeIps = _allSubnetEntries.Where(x => !x.IsOccupied).Select(x => x.IpAddress).ToList();
        if (freeIps.Count == 0)
        {
            MessageBox.Show("No free IPs currently available to copy.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        Clipboard.SetText(string.Join(Environment.NewLine, freeIps));
        MessageBox.Show($"Copied {freeIps.Count} unassigned IP addresses to clipboard!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ExportFreeIpsTxt()
    {
        var freeIps = _allSubnetEntries.Where(x => !x.IsOccupied).Select(x => x.IpAddress).ToList();
        if (freeIps.Count == 0)
        {
            MessageBox.Show("No free IPs to export.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var sfd = new SaveFileDialog
        {
            Filter = "Text Files (*.txt)|*.txt",
            FileName = $"Free_IPs_{_txtSubnetPrefix.Text.Replace('.', '_')}.txt"
        };
        if (sfd.ShowDialog() == true)
        {
            File.WriteAllLines(sfd.FileName, freeIps);
            Log($"[SCANNER] Exported {freeIps.Count} free IPs to {sfd.FileName}");
        }
    }

    private void ExportSubnetCsv()
    {
        if (_allSubnetEntries.Count == 0)
        {
            MessageBox.Show("No scan results to export.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var sfd = new SaveFileDialog
        {
            Filter = "CSV Files (*.csv)|*.csv",
            FileName = $"Subnet_Audit_{_txtSubnetPrefix.Text.Replace('.', '_')}.csv"
        };
        if (sfd.ShowDialog() == true)
        {
            var sb = new StringBuilder();
            sb.AppendLine("IP Address,Status,Hostname,Latency(ms)");
            foreach (var e in _allSubnetEntries)
            {
                sb.AppendLine($"{e.IpAddress},{e.Status},{e.Hostname},{e.LatencyMs}");
            }
            File.WriteAllText(sfd.FileName, sb.ToString());
            Log($"[SCANNER] Exported full subnet audit to {sfd.FileName}");
        }
    }

    #endregion

    #region 3. IPConfig & Netsh Reset Suite

    private UIElement BuildNetshSuiteView()
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
            Text = "IPCONFIG, WINSOCK & NETWORK ADAPTER RESET SUITE",
            FontSize = 13,
            FontWeight = FontWeights.Bold,
            Foreground = SvllBlue,
            Margin = new Thickness(0, 0, 0, 12)
        });

        sp.Children.Add(CreateToolRow("Flush DNS Resolver Cache", "Clears and purges the Windows local DNS resolver cache to resolve domain mapping failures.",
            async () => await ExecuteAsync("ipconfig.exe", "/flushdns")));

        sp.Children.Add(CreateToolRow("Release Current DHCP Lease", "Releases all active IPv4 address leases negotiated with the branch DHCP router.",
            async () => await ExecuteAsync("ipconfig.exe", "/release")));

        sp.Children.Add(CreateToolRow("Renew DHCP Lease (DORA Cycle)", "Initiates a full DHCP discover-offer-request-acknowledgement cycle to obtain an IP lease.",
            async () => await ExecuteAsync("ipconfig.exe", "/renew")));

        sp.Children.Add(CreateToolRow("Reregister DNS Names", "Refreshes all DHCP leases and reregisters DNS names with the corporate domain controller.",
            async () => await ExecuteAsync("ipconfig.exe", "/registerdns")));

        sp.Children.Add(CreateToolRow("Reset Winsock Catalog", "Restores the Windows Socket (Winsock) API catalog to a clean factory state (requires reboot).",
            async () => await ExecuteAsync("netsh.exe", "winsock reset")));

        sp.Children.Add(CreateToolRow("Reset TCP/IP Protocol Stack", "Rewrites TCP/IP registry keys and resets the core IP protocol stack to factory defaults.",
            async () => await ExecuteAsync("netsh.exe", "int ip reset")));

        sp.Children.Add(CreateToolRow("Flush ARP Address Cache", "Deletes all entries in the local ARP table to eliminate phantom MAC-to-IP binding collisions.",
            async () => await ExecuteAsync("netsh.exe", "interface ip delete arpcache")));

        sp.Children.Add(CreateToolRow("Restart Network Adapters", "Cycles all active physical network adapters by disabling and re-enabling them via PowerShell.",
            async () => await ExecuteAsync("powershell.exe", "-NoProfile -Command \"Get-NetAdapter | Restart-NetAdapter\"")));

        sp.Children.Add(CreateToolRow("Reset WinHTTP Proxy Settings", "Clears system-level proxy configurations that might be intercepting corporate web traffic.",
            async () => await ExecuteAsync("netsh.exe", "winhttp reset proxy")));

        sp.Children.Add(CreateToolRow("Detailed Connection Diagnostic", "Runs PowerShell Test-NetConnection against Google Public DNS to inspect ICMP and routing.",
            async () => await ExecuteAsync("powershell.exe", "-NoProfile -Command \"Test-NetConnection -ComputerName 8.8.8.8 -InformationLevel Detailed\"")));

        card.Child = sp;
        root.Children.Add(card);

        scroll.Content = root;
        return scroll;
    }

    #endregion

    #region 4. Wi-Fi Keys & Diagnostics

    private UIElement BuildWifiView()
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
            Text = "WI-FI PROFILE AUDIT & CLEARTEXT PASSWORDS",
            FontSize = 13,
            FontWeight = FontWeights.Bold,
            Foreground = SvllBlue,
            Margin = new Thickness(0, 0, 0, 6)
        });
        sp.Children.Add(new TextBlock
        {
            Text = "Audits all wireless SSIDs configured on this machine, recovers cleartext WPA2/WPA3 pre-shared keys, and exports network XML configurations.",
            FontSize = 11,
            Foreground = TextSubtle,
            Margin = new Thickness(0, 0, 0, 12)
        });

        var btnBar = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 12) };
        var btnAudit = new Button
        {
            Content = "Audit Saved Wi-Fi Profiles",
            Height = 30,
            Padding = new Thickness(12, 0, 12, 0),
            Margin = new Thickness(0, 0, 8, 0),
            Background = SvllBlue,
            Foreground = Brushes.White,
            FontWeight = FontWeights.SemiBold
        };
        var btnExportAll = new Button
        {
            Content = "Export All Wi-Fi Profiles to XML",
            Height = 30,
            Padding = new Thickness(12, 0, 12, 0),
            Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
            BorderBrush = BorderMuted
        };
        btnBar.Children.Add(btnAudit);
        btnBar.Children.Add(btnExportAll);
        sp.Children.Add(btnBar);

        var listWifi = new ListView
        {
            Height = 320,
            FontSize = 11.5,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1)
        };
        var gv = new GridView();
        gv.Columns.Add(new GridViewColumn { Header = "Wireless SSID", Width = 220, DisplayMemberBinding = new Binding("Ssid") });
        gv.Columns.Add(new GridViewColumn { Header = "Authentication", Width = 160, DisplayMemberBinding = new Binding("Authentication") });
        gv.Columns.Add(new GridViewColumn { Header = "Cleartext Security Key", Width = 260, DisplayMemberBinding = new Binding("Password") });
        listWifi.View = gv;
        sp.Children.Add(listWifi);

        btnAudit.Click += async (s, e) =>
        {
            btnAudit.IsEnabled = false;
            try
            {
                var profiles = await Task.Run(() => AuditWifiProfiles());
                listWifi.ItemsSource = profiles;
                Log($"[WIFI] Audited {profiles.Count} saved wireless network profiles.");
            }
            finally
            {
                btnAudit.IsEnabled = true;
            }
        };

        btnExportAll.Click += async (s, e) =>
        {
            string exportDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "SVLL_Wifi_Profiles");
            Directory.CreateDirectory(exportDir);
            await ExecuteAsync("netsh.exe", $"wlan export profile folder=\"{exportDir}\" key=clear");
            Log($"[WIFI] Exported cleartext Wi-Fi XML profiles to {exportDir}");
            Process.Start("explorer.exe", exportDir);
        };

        card.Child = sp;
        root.Children.Add(card);

        scroll.Content = root;
        return scroll;
    }

    private List<WifiProfileEntry> AuditWifiProfiles()
    {
        var list = new List<WifiProfileEntry>();
        try
        {
            var p = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "netsh.exe",
                    Arguments = "wlan show profiles",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            p.Start();
            string outStr = p.StandardOutput.ReadToEnd();
            p.WaitForExit();

            var matches = Regex.Matches(outStr, @":\s*(.+)");
            var profileNames = new List<string>();
            foreach (Match m in matches)
            {
                string name = m.Groups[1].Value.Trim();
                if (!string.IsNullOrEmpty(name) && !outStr.Substring(0, m.Index).EndsWith("User profiles\r\n    -------------\r\n    All User Profile     "))
                {
                    profileNames.Add(name);
                }
            }

            // More accurate extraction
            var lines = outStr.Split('\n');
            profileNames.Clear();
            foreach (var line in lines)
            {
                if (line.Contains("All User Profile"))
                {
                    int colon = line.IndexOf(':');
                    if (colon != -1) profileNames.Add(line.Substring(colon + 1).Trim());
                }
            }

            foreach (var name in profileNames)
            {
                var entry = new WifiProfileEntry { Ssid = name, Authentication = "WPA2-Personal", Password = "(None / Enterprise)" };
                var p2 = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "netsh.exe",
                        Arguments = $"wlan show profile name=\"{name}\" key=clear",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };
                p2.Start();
                string detail = p2.StandardOutput.ReadToEnd();
                p2.WaitForExit();

                var keyMatch = Regex.Match(detail, @"Key Content\s*:\s*(.+)");
                if (keyMatch.Success) entry.Password = keyMatch.Groups[1].Value.Trim();

                var authMatch = Regex.Match(detail, @"Authentication\s*:\s*(.+)");
                if (authMatch.Success) entry.Authentication = authMatch.Groups[1].Value.Trim();

                list.Add(entry);
            }
        }
        catch (Exception ex)
        {
            Dispatcher.Invoke(() => Log($"[WIFI ERROR] {ex.Message}"));
        }
        return list;
    }

    #endregion

    #region 5. LAN Network Share File Access & Push

    private UIElement BuildLanShareView()
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
            Text = "LAN SMB/UNC NETWORK SHARE EXPLORER & DEPLOYMENT PUSH",
            FontSize = 13,
            FontWeight = FontWeights.Bold,
            Foreground = SvllBlue,
            Margin = new Thickness(0, 0, 0, 6)
        });
        sp.Children.Add(new TextBlock
        {
            Text = "Connect to central server file shares (e.g. \\\\server\\share), browse corporate installers, execute setups directly over the network, or copy packages locally.",
            FontSize = 11,
            Foreground = TextSubtle,
            Margin = new Thickness(0, 0, 0, 12)
        });

        // Path bar
        var pathGrid = new Grid { Margin = new Thickness(0, 0, 0, 10) };
        pathGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pathGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        pathGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        _txtSharePath = new TextBox
        {
            Height = 32,
            FontSize = 12,
            Padding = new Thickness(8, 4, 8, 4),
            Text = @"\\192.168.1.100\Deployments",
            Margin = new Thickness(0, 0, 8, 0)
        };
        Grid.SetColumn(_txtSharePath, 0);
        pathGrid.Children.Add(_txtSharePath);

        var btnBrowse = new Button
        {
            Content = " Browse Share ",
            Height = 32,
            Background = SvllBlue,
            Foreground = Brushes.White,
            FontWeight = FontWeights.Bold,
            Padding = new Thickness(12, 0, 12, 0),
            Margin = new Thickness(0, 0, 6, 0),
            Cursor = Cursors.Hand
        };
        btnBrowse.Click += async (s, e) => await BrowseSharePathAsync();
        Grid.SetColumn(btnBrowse, 1);
        pathGrid.Children.Add(btnBrowse);

        var btnOpenExplorer = new Button
        {
            Content = "Open in Windows Explorer",
            Height = 32,
            Padding = new Thickness(10, 0, 10, 0),
            Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
            BorderBrush = BorderMuted
        };
        btnOpenExplorer.Click += (s, e) =>
        {
            string p = _txtSharePath.Text.Trim();
            if (!string.IsNullOrEmpty(p)) Process.Start("explorer.exe", p);
        };
        Grid.SetColumn(btnOpenExplorer, 2);
        pathGrid.Children.Add(btnOpenExplorer);

        sp.Children.Add(pathGrid);

        // Bookmarks Dropdown
        var bookmarkRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 12) };
        bookmarkRow.Children.Add(new TextBlock { Text = "Frequent Share Presets: ", FontSize = 11, VerticalAlignment = VerticalAlignment.Center });
        var cmbPresets = new ComboBox
        {
            ItemsSource = new[] { @"\\192.168.1.100\Deployments", @"\\dc01\software", @"\\svll-nas01\it-repository", @"\\fileserver\drivers" },
            SelectedIndex = 0,
            Height = 26,
            Width = 260,
            Margin = new Thickness(6, 0, 10, 0)
        };
        cmbPresets.SelectionChanged += (s, e) =>
        {
            if (cmbPresets.SelectedItem != null) _txtSharePath.Text = cmbPresets.SelectedItem.ToString();
        };
        bookmarkRow.Children.Add(cmbPresets);
        sp.Children.Add(bookmarkRow);

        // Share File List
        _listShareFiles = new ListView
        {
            Height = 300,
            FontSize = 11.5,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            Margin = new Thickness(0, 0, 0, 12)
        };
        var gvShare = new GridView();
        gvShare.Columns.Add(new GridViewColumn { Header = "Name", Width = 260, DisplayMemberBinding = new Binding("Name") });
        gvShare.Columns.Add(new GridViewColumn { Header = "Size", Width = 110, DisplayMemberBinding = new Binding("SizeFormatted") });
        gvShare.Columns.Add(new GridViewColumn { Header = "Last Modified", Width = 160, DisplayMemberBinding = new Binding("LastModified") });
        gvShare.Columns.Add(new GridViewColumn { Header = "Full UNC Path", Width = 320, DisplayMemberBinding = new Binding("FullPath") });
        _listShareFiles.View = gvShare;
        _listShareFiles.MouseDoubleClick += (s, e) => ExecuteSelectedShareItem();
        sp.Children.Add(_listShareFiles);

        // File Action Buttons
        var actionRow = new StackPanel { Orientation = Orientation.Horizontal };
        var btnRunNet = new Button
        {
            Content = "Run Selected Over Network",
            Height = 32,
            Padding = new Thickness(14, 0, 14, 0),
            Margin = new Thickness(0, 0, 8, 0),
            Background = SvllBlue,
            Foreground = Brushes.White,
            FontWeight = FontWeights.SemiBold
        };
        btnRunNet.Click += (s, e) => ExecuteSelectedShareItem();
        actionRow.Children.Add(btnRunNet);

        var btnCopyLocal = new Button
        {
            Content = "Copy to C:\\SVLL_Deployments & Run",
            Height = 32,
            Padding = new Thickness(14, 0, 14, 0),
            Margin = new Thickness(0, 0, 8, 0),
            Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
            BorderBrush = BorderMuted
        };
        btnCopyLocal.Click += async (s, e) => await CopyLocalAndRunSelectedAsync();
        actionRow.Children.Add(btnCopyLocal);

        var btnMapDrive = new Button
        {
            Content = "Map as Z: Drive (net use)",
            Height = 32,
            Padding = new Thickness(12, 0, 12, 0),
            Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
            BorderBrush = BorderMuted
        };
        btnMapDrive.Click += async (s, e) =>
        {
            string p = _txtSharePath.Text.Trim();
            await ExecuteAsync("net.exe", $"use Z: \"{p}\" /persistent:yes");
            Log($"[SHARE] Mapped network share {p} to Z: drive.");
        };
        actionRow.Children.Add(btnMapDrive);

        sp.Children.Add(actionRow);
        card.Child = sp;
        root.Children.Add(card);

        scroll.Content = root;
        return scroll;
    }

    private async Task BrowseSharePathAsync()
    {
        string path = _txtSharePath.Text.Trim();
        if (string.IsNullOrEmpty(path)) return;

        Log($"\n[SHARE] Browsing network path: {path}...");

        _shareItems.Clear();
        _listShareFiles.ItemsSource = null;

        await Task.Run(() =>
        {
            try
            {
                var dir = new DirectoryInfo(path);
                if (dir.Exists)
                {
                    foreach (var d in dir.GetDirectories())
                    {
                        _shareItems.Add(new NetworkShareItem
                        {
                            Name = $"📁 {d.Name}",
                            FullPath = d.FullName,
                            IsDirectory = true,
                            FileSizeBytes = 0,
                            LastModified = d.LastWriteTime
                        });
                    }
                    foreach (var f in dir.GetFiles())
                    {
                        _shareItems.Add(new NetworkShareItem
                        {
                            Name = f.Name,
                            FullPath = f.FullName,
                            IsDirectory = false,
                            FileSizeBytes = f.Length,
                            LastModified = f.LastWriteTime
                        });
                    }
                }
                else
                {
                    Dispatcher.Invoke(() => Log($"[SHARE] Directory does not exist or access denied: {path}"));
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => Log($"[SHARE ERROR] {ex.Message}"));
            }
        });

        _listShareFiles.ItemsSource = _shareItems.OrderByDescending(x => x.IsDirectory).ThenBy(x => x.Name).ToList();
        Log($"[SHARE] Found {_shareItems.Count} items on network share.");
    }

    private void ExecuteSelectedShareItem()
    {
        if (_listShareFiles.SelectedItem is NetworkShareItem item)
        {
            if (item.IsDirectory)
            {
                _txtSharePath.Text = item.FullPath;
                _ = BrowseSharePathAsync();
            }
            else
            {
                try
                {
                    Process.Start(new ProcessStartInfo { FileName = item.FullPath, UseShellExecute = true });
                    Log($"[SHARE] Executed directly over network: {item.FullPath}");
                }
                catch (Exception ex)
                {
                    Log($"[SHARE LAUNCH ERROR] {ex.Message}");
                }
            }
        }
    }

    private async Task CopyLocalAndRunSelectedAsync()
    {
        if (_listShareFiles.SelectedItem is NetworkShareItem item && !item.IsDirectory)
        {
            string destDir = @"C:\SVLL_Deployments";
            Directory.CreateDirectory(destDir);
            string destFile = Path.Combine(destDir, Path.GetFileName(item.FullPath));

            Log($"[SHARE] Copying {item.Name} to {destFile}...");
            await Task.Run(() => File.Copy(item.FullPath, destFile, true));
            Log($"[SHARE] Successfully copied locally. Launching setup...");

            try
            {
                Process.Start(new ProcessStartInfo { FileName = destFile, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Log($"[SHARE RUN ERROR] {ex.Message}");
            }
        }
    }

    #endregion
}
