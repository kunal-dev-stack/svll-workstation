using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using SVLL_IT_Workstation.Models;

namespace SVLL_IT_Workstation;

public class NvrEditorDialog : Window
{
    private readonly NvrDevice _device;
    private readonly bool _isNew;

    private TextBox _txtName = null!;
    private TextBox _txtLocation = null!;
    private ComboBox _cmbBrand = null!;
    private TextBox _txtHost = null!;
    private TextBox _txtHttpPort = null!;
    private TextBox _txtRtspPort = null!;
    private TextBox _txtUsername = null!;
    private PasswordBox _txtPassword = null!;
    private TextBox _txtPasswordVisible = null!;
    private CheckBox _chkShowPassword = null!;
    private ComboBox _cmbChannels = null!;
    private ComboBox _cmbStreamQuality = null!;
    private TextBox _txtCustomRtsp = null!;
    private TextBox _txtCustomSnapshot = null!;
    private StackPanel _customFieldsPanel = null!;

    private StackPanel _channelsListPanel = null!;
    private readonly Dictionary<int, TextBox> _channelNameBoxes = new();

    private Border _testResultCard = null!;
    private TextBlock _lblTestResult = null!;
    private Button _btnTestConnection = null!;
    private Button _btnSave = null!;

    public NvrDevice DeviceResult => _device;

    public NvrEditorDialog(NvrDevice? existingDevice = null, Window? owner = null)
    {
        _isNew = (existingDevice == null);
        _device = existingDevice != null
            ? System.Text.Json.JsonSerializer.Deserialize<NvrDevice>(System.Text.Json.JsonSerializer.Serialize(existingDevice))!
            : new NvrDevice { Name = "New Branch NVR", Location = "Branch Logistics Hub" };

        Title = _isNew ? "Add NVR / DVR Device & Camera Stream" : "Edit NVR / DVR Configuration";
        Width = 720;
        Height = 680;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Owner = owner;
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        ResizeMode = ResizeMode.NoResize;

        Content = BuildUi();
        PopulateFields();
    }

    private UIElement BuildUi()
    {
        var outerBorder = new Border
        {
            Background = MainWindow.BgCard,
            BorderBrush = MainWindow.SvllBlue,
            BorderThickness = new Thickness(1.5),
            CornerRadius = new CornerRadius(14),
            Padding = new Thickness(0),
            Effect = new DropShadowEffect
            {
                BlurRadius = 24,
                ShadowDepth = 6,
                Opacity = 0.35,
                Color = Colors.Black
            }
        };

        var mainGrid = new Grid();
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Scrollable Body
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Footer / Buttons

        // 1. Header Bar
        var header = new Border
        {
            Background = MainWindow.SvllBlue,
            CornerRadius = new CornerRadius(12, 12, 0, 0),
            Padding = new Thickness(20, 14, 20, 14)
        };
        header.MouseLeftButtonDown += (s, e) => { if (e.LeftButton == MouseButtonState.Pressed) DragMove(); };

        var headGrid = new Grid();
        headGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        headGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var titleStack = new StackPanel();
        var titleRow = new StackPanel { Orientation = Orientation.Horizontal };
        titleRow.Children.Add(new TextBlock
        {
            Text = _isNew ? "📹 Add NVR / DVR Video Device" : "⚙️ Edit NVR / DVR Configuration",
            FontSize = 16,
            FontWeight = FontWeights.Bold,
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center
        });
        titleStack.Children.Add(titleRow);
        titleStack.Children.Add(new TextBlock
        {
            Text = "Configure surveillance hardware credentials, channel mapping, RTSP & snapshot endpoints.",
            FontSize = 11,
            Foreground = new SolidColorBrush(Color.FromRgb(224, 231, 255)),
            Margin = new Thickness(0, 3, 0, 0)
        });
        Grid.SetColumn(titleStack, 0);
        headGrid.Children.Add(titleStack);

        var btnClose = new Button
        {
            Content = "✕",
            Width = 32,
            Height = 32,
            Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            FontWeight = FontWeights.Bold,
            Cursor = Cursors.Hand,
            VerticalAlignment = VerticalAlignment.Center
        };
        btnClose.Click += (s, e) => Close();
        Grid.SetColumn(btnClose, 1);
        headGrid.Children.Add(btnClose);

        header.Child = headGrid;
        Grid.SetRow(header, 0);
        mainGrid.Children.Add(header);

        // 2. Scrollable Body
        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Padding = new Thickness(24, 18, 24, 12)
        };

        var form = new StackPanel();

        // Row 1: Name and Location
        var gridRow1 = new Grid { Margin = new Thickness(0, 0, 0, 12) };
        gridRow1.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        gridRow1.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });
        gridRow1.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var colName = new StackPanel();
        colName.Children.Add(CreateFieldLabel("NVR / DVR Device Name *"));
        _txtName = CreateInputBox("e.g. Raipur Central Warehouse NVR");
        colName.Children.Add(_txtName);
        Grid.SetColumn(colName, 0);
        gridRow1.Children.Add(colName);

        var colLoc = new StackPanel();
        colLoc.Children.Add(CreateFieldLabel("Branch / Logistics Hub Location"));
        _txtLocation = CreateInputBox("e.g. Raipur Hub - Bay 1");
        colLoc.Children.Add(_txtLocation);
        Grid.SetColumn(colLoc, 2);
        gridRow1.Children.Add(colLoc);
        form.Children.Add(gridRow1);

        // Row 2: Brand / Preset & Stream Quality
        var gridRow2 = new Grid { Margin = new Thickness(0, 0, 0, 12) };
        gridRow2.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        gridRow2.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });
        gridRow2.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var colBrand = new StackPanel();
        colBrand.Children.Add(CreateFieldLabel("Brand / Protocol Architecture *"));
        _cmbBrand = new ComboBox { Height = 34, FontSize = 12, VerticalContentAlignment = VerticalAlignment.Center };
        _cmbBrand.Items.Add("Hikvision / HiLook (ISAPI & RTSP)");
        _cmbBrand.Items.Add("CP PLUS (Indigo / Orange Series)");
        _cmbBrand.Items.Add("Dahua Technology (CGI & RTSP)");
        _cmbBrand.Items.Add("Uniview - UNV (LAPI & RTSP)");
        _cmbBrand.Items.Add("Generic ONVIF / RTSP Stream");
        _cmbBrand.Items.Add("Custom URL Templates");
        _cmbBrand.SelectedIndex = 1;
        _cmbBrand.SelectionChanged += OnBrandSelectionChanged;
        colBrand.Children.Add(_cmbBrand);
        Grid.SetColumn(colBrand, 0);
        gridRow2.Children.Add(colBrand);

        var colQuality = new StackPanel();
        colQuality.Children.Add(CreateFieldLabel("Stream Profile (Multi-View vs HD)"));
        _cmbStreamQuality = new ComboBox { Height = 34, FontSize = 12, VerticalContentAlignment = VerticalAlignment.Center };
        _cmbStreamQuality.Items.Add("Sub-Stream (Smooth 1-5 FPS Multi-Grid • Low Bandwidth)");
        _cmbStreamQuality.Items.Add("Main-Stream (Full HD/4K Detail • High Bandwidth)");
        _cmbStreamQuality.SelectedIndex = 0;
        colQuality.Children.Add(_cmbStreamQuality);
        Grid.SetColumn(colQuality, 2);
        gridRow2.Children.Add(colQuality);
        form.Children.Add(gridRow2);

        // Row 3: Host / IP, HTTP Port, RTSP Port
        var gridRow3 = new Grid { Margin = new Thickness(0, 0, 0, 12) };
        gridRow3.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
        gridRow3.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
        gridRow3.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        gridRow3.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
        gridRow3.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var colHost = new StackPanel();
        colHost.Children.Add(CreateFieldLabel("Host / IP Address / Domain *"));
        _txtHost = CreateInputBox("192.168.1.100");
        colHost.Children.Add(_txtHost);
        Grid.SetColumn(colHost, 0);
        gridRow3.Children.Add(colHost);

        var colHttp = new StackPanel();
        colHttp.Children.Add(CreateFieldLabel("HTTP Port"));
        _txtHttpPort = CreateInputBox("80");
        colHttp.Children.Add(_txtHttpPort);
        Grid.SetColumn(colHttp, 2);
        gridRow3.Children.Add(colHttp);

        var colRtsp = new StackPanel();
        colRtsp.Children.Add(CreateFieldLabel("RTSP Port"));
        _txtRtspPort = CreateInputBox("554");
        colRtsp.Children.Add(_txtRtspPort);
        Grid.SetColumn(colRtsp, 4);
        gridRow3.Children.Add(colRtsp);
        form.Children.Add(gridRow3);

        // Row 4: Credentials (Username & Password)
        var gridRow4 = new Grid { Margin = new Thickness(0, 0, 0, 12) };
        gridRow4.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        gridRow4.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });
        gridRow4.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var colUser = new StackPanel();
        colUser.Children.Add(CreateFieldLabel("Username *"));
        _txtUsername = CreateInputBox("admin");
        colUser.Children.Add(_txtUsername);
        Grid.SetColumn(colUser, 0);
        gridRow4.Children.Add(colUser);

        var colPass = new StackPanel();
        colPass.Children.Add(CreateFieldLabel("Password (Encrypted in Vault)"));
        var passGrid = new Grid();
        passGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        passGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        _txtPassword = new PasswordBox
        {
            Height = 34,
            FontSize = 13,
            VerticalContentAlignment = VerticalAlignment.Center,
            Padding = new Thickness(8, 0, 8, 0),
            BorderBrush = MainWindow.BorderMuted,
            BorderThickness = new Thickness(1)
        };
        _txtPasswordVisible = new TextBox
        {
            Height = 34,
            FontSize = 13,
            VerticalContentAlignment = VerticalAlignment.Center,
            Padding = new Thickness(8, 0, 8, 0),
            BorderBrush = MainWindow.BorderMuted,
            BorderThickness = new Thickness(1),
            Visibility = Visibility.Collapsed
        };
        _txtPassword.PasswordChanged += (s, e) =>
        {
            if (_txtPasswordVisible.Visibility != Visibility.Visible)
                _txtPasswordVisible.Text = _txtPassword.Password;
        };
        _txtPasswordVisible.TextChanged += (s, e) =>
        {
            if (_txtPasswordVisible.Visibility == Visibility.Visible)
                _txtPassword.Password = _txtPasswordVisible.Text;
        };

        _chkShowPassword = new CheckBox
        {
            Content = "Show",
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8, 0, 0, 0),
            FontSize = 11
        };
        _chkShowPassword.Checked += (s, e) =>
        {
            _txtPasswordVisible.Text = _txtPassword.Password;
            _txtPasswordVisible.Visibility = Visibility.Visible;
            _txtPassword.Visibility = Visibility.Collapsed;
        };
        _chkShowPassword.Unchecked += (s, e) =>
        {
            _txtPassword.Password = _txtPasswordVisible.Text;
            _txtPassword.Visibility = Visibility.Visible;
            _txtPasswordVisible.Visibility = Visibility.Collapsed;
        };

        Grid.SetColumn(_txtPassword, 0);
        passGrid.Children.Add(_txtPassword);
        Grid.SetColumn(_txtPasswordVisible, 0);
        passGrid.Children.Add(_txtPasswordVisible);
        Grid.SetColumn(_chkShowPassword, 1);
        passGrid.Children.Add(_chkShowPassword);

        colPass.Children.Add(passGrid);
        Grid.SetColumn(colPass, 2);
        gridRow4.Children.Add(colPass);
        form.Children.Add(gridRow4);

        // Row 5: Channel Count
        var gridRow5 = new Grid { Margin = new Thickness(0, 0, 0, 12) };
        gridRow5.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        gridRow5.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });
        gridRow5.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var colChCount = new StackPanel();
        colChCount.Children.Add(CreateFieldLabel("Total Supported Camera Channels"));
        _cmbChannels = new ComboBox { Height = 34, FontSize = 12, VerticalContentAlignment = VerticalAlignment.Center };
        _cmbChannels.Items.Add("4 Channels (Compact Hub)");
        _cmbChannels.Items.Add("8 Channels (Standard Branch)");
        _cmbChannels.Items.Add("16 Channels (Central Hub / Warehouse)");
        _cmbChannels.Items.Add("32 Channels (Large Logistics Hub)");
        _cmbChannels.Items.Add("64 Channels (Enterprise Hub)");
        _cmbChannels.SelectedIndex = 1;
        _cmbChannels.SelectionChanged += (s, e) => RebuildChannelsList();
        colChCount.Children.Add(_cmbChannels);
        Grid.SetColumn(colChCount, 0);
        gridRow5.Children.Add(colChCount);

        // Test Connection Button Card
        var colTest = new StackPanel { VerticalAlignment = VerticalAlignment.Bottom };
        _btnTestConnection = new Button
        {
            Content = "⚡ Test NVR Connection & Ports",
            Height = 34,
            FontWeight = FontWeights.Bold,
            Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
            BorderBrush = MainWindow.SvllBlue,
            BorderThickness = new Thickness(1),
            Foreground = MainWindow.SvllBlue,
            Cursor = Cursors.Hand
        };
        _btnTestConnection.Click += async (s, e) => await RunConnectionTestAsync();
        colTest.Children.Add(_btnTestConnection);
        Grid.SetColumn(colTest, 2);
        gridRow5.Children.Add(colTest);
        form.Children.Add(gridRow5);

        // Custom Template Fields (Collapsed unless Custom selected)
        _customFieldsPanel = new StackPanel { Visibility = Visibility.Collapsed, Margin = new Thickness(0, 0, 0, 12) };
        _customFieldsPanel.Children.Add(CreateFieldLabel("Custom RTSP Pattern (Tokens: {user},{pass},{host},{port},{channel},{subtype})"));
        _txtCustomRtsp = CreateInputBox("rtsp://{user}:{pass}@{host}:{port}/live/ch{channel}");
        _customFieldsPanel.Children.Add(_txtCustomRtsp);
        _customFieldsPanel.Children.Add(CreateFieldLabel("Custom Snapshot Pattern (Tokens: {host},{httpPort},{channel},{user},{pass})"));
        _txtCustomSnapshot = CreateInputBox("http://{host}:{httpPort}/snapshot?channel={channel}");
        _customFieldsPanel.Children.Add(_txtCustomSnapshot);
        form.Children.Add(_customFieldsPanel);

        // Diagnostic Test Result Card
        _testResultCard = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
            BorderBrush = MainWindow.BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12, 10, 12, 10),
            Margin = new Thickness(0, 0, 0, 12),
            Visibility = Visibility.Collapsed
        };
        _lblTestResult = new TextBlock
        {
            Text = "",
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap,
            FontFamily = new FontFamily("Consolas, Segoe UI")
        };
        _testResultCard.Child = _lblTestResult;
        form.Children.Add(_testResultCard);

        // Channel Names Configuration Expander
        var expander = new Expander
        {
            Header = "📋 Custom Camera / Channel Labels (e.g. Weighbridge, Main Gate, Docking Bay)",
            FontWeight = FontWeights.SemiBold,
            FontSize = 12,
            IsExpanded = true,
            Margin = new Thickness(0, 6, 0, 10)
        };
        var channelsScroll = new ScrollViewer
        {
            MaxHeight = 180,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Margin = new Thickness(0, 8, 0, 0)
        };
        _channelsListPanel = new StackPanel();
        channelsScroll.Content = _channelsListPanel;
        expander.Content = channelsScroll;
        form.Children.Add(expander);

        scroll.Content = form;
        Grid.SetRow(scroll, 1);
        mainGrid.Children.Add(scroll);

        // 3. Footer Bar
        var footer = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
            BorderBrush = MainWindow.BorderMuted,
            BorderThickness = new Thickness(0, 1, 0, 0),
            CornerRadius = new CornerRadius(0, 0, 12, 12),
            Padding = new Thickness(20, 12, 20, 12)
        };

        var footGrid = new Grid();
        footGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        footGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var footInfo = new TextBlock
        {
            Text = "🔒 Credentials saved locally with DPAPI encryption in %APPDATA%\\SVLL_IT_Workstation",
            FontSize = 11,
            Foreground = MainWindow.TextSubtle,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(footInfo, 0);
        footGrid.Children.Add(footInfo);

        var btnStack = new StackPanel { Orientation = Orientation.Horizontal };
        var btnCancel = new Button
        {
            Content = "Cancel",
            Width = 90,
            Height = 34,
            Background = Brushes.White,
            BorderBrush = MainWindow.BorderMuted,
            BorderThickness = new Thickness(1),
            Margin = new Thickness(0, 0, 10, 0),
            Cursor = Cursors.Hand
        };
        btnCancel.Click += (s, e) => Close();
        btnStack.Children.Add(btnCancel);

        _btnSave = new Button
        {
            Content = _isNew ? "💾 Save NVR & Cameras" : "💾 Update Configuration",
            Width = 170,
            Height = 34,
            FontWeight = FontWeights.Bold,
            Background = MainWindow.SvllBlue,
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand
        };
        _btnSave.Click += (s, e) => SaveAndClose();
        btnStack.Children.Add(_btnSave);

        Grid.SetColumn(btnStack, 1);
        footGrid.Children.Add(btnStack);

        footer.Child = footGrid;
        Grid.SetRow(footer, 2);
        mainGrid.Children.Add(footer);

        outerBorder.Child = mainGrid;
        return outerBorder;
    }

    private TextBlock CreateFieldLabel(string text)
    {
        return new TextBlock
        {
            Text = text,
            FontSize = 11.5,
            FontWeight = FontWeights.SemiBold,
            Foreground = MainWindow.TextDark,
            Margin = new Thickness(0, 0, 0, 4)
        };
    }

    private TextBox CreateInputBox(string placeholder)
    {
        return new TextBox
        {
            Height = 34,
            FontSize = 12.5,
            VerticalContentAlignment = VerticalAlignment.Center,
            Padding = new Thickness(8, 0, 8, 0),
            BorderBrush = MainWindow.BorderMuted,
            BorderThickness = new Thickness(1)
        };
    }

    private void OnBrandSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        int idx = _cmbBrand.SelectedIndex;
        if (idx == 0) // Hikvision
        {
            if (_txtHttpPort.Text == "8000" || _txtHttpPort.Text == "80") _txtHttpPort.Text = "80";
        }
        else if (idx == 1 || idx == 2) // CP PLUS / Dahua
        {
            if (_txtHttpPort.Text == "80") _txtHttpPort.Text = "80";
        }

        if (_customFieldsPanel != null)
        {
            _customFieldsPanel.Visibility = (idx == 5) ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void PopulateFields()
    {
        _txtName.Text = _device.Name;
        _txtLocation.Text = _device.Location;
        _cmbBrand.SelectedIndex = (int)_device.Brand;
        _txtHost.Text = _device.Host;
        _txtHttpPort.Text = _device.HttpPort.ToString();
        _txtRtspPort.Text = _device.RtspPort.ToString();
        _txtUsername.Text = _device.Username;
        _txtPassword.Password = _device.Password;
        _txtPasswordVisible.Text = _device.Password;

        _cmbStreamQuality.SelectedIndex = (_device.Quality == StreamQuality.SubStream) ? 0 : 1;
        _txtCustomRtsp.Text = _device.CustomRtspTemplate;
        _txtCustomSnapshot.Text = _device.CustomSnapshotTemplate;

        int chCount = _device.ChannelCount;
        int chIdx = chCount switch
        {
            4 => 0,
            8 => 1,
            16 => 2,
            32 => 3,
            64 => 4,
            _ => 1
        };
        _cmbChannels.SelectedIndex = chIdx;

        RebuildChannelsList();
    }

    private void RebuildChannelsList()
    {
        _channelsListPanel.Children.Clear();
        _channelNameBoxes.Clear();

        int count = GetSelectedChannelCount();
        for (int i = 1; i <= count; i++)
        {
            var row = new Grid { Margin = new Thickness(0, 0, 0, 4) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var lbl = new TextBlock
            {
                Text = $"Channel {i:D2}:",
                FontSize = 11.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = MainWindow.TextSubtle,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(lbl, 0);
            row.Children.Add(lbl);

            string currentName = _device.GetChannelName(i);
            var txt = new TextBox
            {
                Text = currentName,
                Height = 28,
                FontSize = 11.5,
                VerticalContentAlignment = VerticalAlignment.Center,
                Padding = new Thickness(6, 0, 6, 0),
                BorderBrush = MainWindow.BorderMuted,
                BorderThickness = new Thickness(1)
            };
            _channelNameBoxes[i] = txt;
            Grid.SetColumn(txt, 1);
            row.Children.Add(txt);

            _channelsListPanel.Children.Add(row);
        }
    }

    private int GetSelectedChannelCount()
    {
        return _cmbChannels.SelectedIndex switch
        {
            0 => 4,
            1 => 8,
            2 => 16,
            3 => 32,
            4 => 64,
            _ => 8
        };
    }

    private async Task RunConnectionTestAsync()
    {
        _btnTestConnection.IsEnabled = false;
        _btnTestConnection.Content = "⏳ Testing Connectivity...";
        _testResultCard.Visibility = Visibility.Visible;
        _lblTestResult.Text = "Pinging host, testing HTTP & RTSP ports, verifying snapshot credentials...";
        _testResultCard.Background = new SolidColorBrush(Color.FromRgb(248, 250, 252));

        var tempDevice = BuildDeviceFromForm();
        var result = await NvrStreamHelper.TestConnectivityAsync(tempDevice);

        _lblTestResult.Text = result.Message;
        if (result.OverallSuccess)
        {
            _testResultCard.Background = new SolidColorBrush(Color.FromRgb(240, 253, 244));
            _lblTestResult.Foreground = new SolidColorBrush(Color.FromRgb(22, 101, 52));
        }
        else
        {
            _testResultCard.Background = new SolidColorBrush(Color.FromRgb(254, 242, 242));
            _lblTestResult.Foreground = new SolidColorBrush(Color.FromRgb(153, 27, 27));
        }

        _btnTestConnection.IsEnabled = true;
        _btnTestConnection.Content = "⚡ Test NVR Connection & Ports";
    }

    private NvrDevice BuildDeviceFromForm()
    {
        var d = new NvrDevice
        {
            Id = _device.Id,
            Name = string.IsNullOrWhiteSpace(_txtName.Text) ? "Branch NVR" : _txtName.Text.Trim(),
            Location = _txtLocation.Text.Trim(),
            Brand = (NvrBrand)_cmbBrand.SelectedIndex,
            Host = _txtHost.Text.Trim(),
            HttpPort = int.TryParse(_txtHttpPort.Text.Trim(), out int hp) ? hp : 80,
            RtspPort = int.TryParse(_txtRtspPort.Text.Trim(), out int rp) ? rp : 554,
            Username = _txtUsername.Text.Trim(),
            Password = _txtPassword.Password,
            ChannelCount = GetSelectedChannelCount(),
            Quality = (_cmbStreamQuality.SelectedIndex == 1) ? StreamQuality.MainStream : StreamQuality.SubStream,
            CustomRtspTemplate = _txtCustomRtsp.Text.Trim(),
            CustomSnapshotTemplate = _txtCustomSnapshot.Text.Trim()
        };

        d.Channels.Clear();
        foreach (var kvp in _channelNameBoxes)
        {
            d.Channels.Add(new NvrChannelConfig
            {
                ChannelNumber = kvp.Key,
                CustomName = kvp.Value.Text.Trim(),
                IsEnabled = true
            });
        }

        return d;
    }

    private void SaveAndClose()
    {
        if (string.IsNullOrWhiteSpace(_txtName.Text))
        {
            MessageBox.Show("Please enter a name for this NVR / DVR.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            _txtName.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(_txtHost.Text))
        {
            MessageBox.Show("Please enter the Host IP address or domain name for the NVR.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            _txtHost.Focus();
            return;
        }

        var updated = BuildDeviceFromForm();
        _device.Name = updated.Name;
        _device.Location = updated.Location;
        _device.Brand = updated.Brand;
        _device.Host = updated.Host;
        _device.HttpPort = updated.HttpPort;
        _device.RtspPort = updated.RtspPort;
        _device.Username = updated.Username;
        _device.Password = updated.Password;
        _device.ChannelCount = updated.ChannelCount;
        _device.Quality = updated.Quality;
        _device.CustomRtspTemplate = updated.CustomRtspTemplate;
        _device.CustomSnapshotTemplate = updated.CustomSnapshotTemplate;
        _device.Channels = updated.Channels;

        NvrDeviceStorage.Instance.SaveDevice(_device);
        DialogResult = true;
        Close();
    }
}

