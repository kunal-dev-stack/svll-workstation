using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Text.Json;
using System.Threading;
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
    // OFFLINE USB DEPOT & DRIVER FINDER STATE
    // ==========================================

    // Sub-tab view switching
    private Border _panelUsbDeployer = null!;
    private Border _panelDriverFinder = null!;
    private Border _panelDriverBackup = null!;
    private Button _btnTabUsbDeployer = null!;
    private Button _btnTabDriverFinder = null!;
    private Button _btnTabDriverBackup = null!;

    // 1. USB Deployer Controls
    private ComboBox _cmbUsbDrives = null!;
    private TextBox _txtCustomFolderPath = null!;
    private ListView _listUsbItems = null!;
    private readonly List<UsbDepotItem> _allUsbItems = new List<UsbDepotItem>();
    private TextBox _txtUsbSearch = null!;
    private ProgressBar _progUsbBatch = null!;
    private TextBlock _lblUsbBatchStatus = null!;
    private Button _btnBatchInstall = null!;
    private Button _btnAbortBatch = null!;
    private CheckBox _chkCreateRestorePoint = null!;
    private CheckBox _chkSaveDeployJson = null!;
    private bool _isBatchInstalling = false;
    private CancellationTokenSource? _batchCancelCts;
    private string _activeUsbFilter = "ALL";

    // 2. Automated Driver Finder Controls
    private TextBlock _lblOemSystemInfo = null!;
    private TextBlock _lblMissingDevicesHeader = null!;
    private ListView _listMissingDevices = null!;
    private readonly List<MissingHardwareDeviceItem> _missingDevices = new List<MissingHardwareDeviceItem>();
    private ListView _listWhqlDrivers = null!;
    private readonly List<WhqlDriverPackageItem> _whqlDrivers = new List<WhqlDriverPackageItem>();
    private ProgressBar _progDriverScan = null!;
    private TextBlock _lblDriverScanStatus = null!;
    private Button _btnWhqlInstall = null!;
    private string _detectedOemMake = "";
    private string _detectedOemModel = "";
    private string _detectedOemSerial = "";

    // 3. Driver Export / Backup Controls
    private TextBox _txtExportTargetDir = null!;
    private ProgressBar _progDriverExport = null!;
    private TextBlock _lblDriverExportStatus = null!;
    private Button _btnExportDrivers = null!;

    private UIElement BuildUsbDepotView()
    {
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var root = new StackPanel { Margin = new Thickness(0, 0, 10, 0) };

        // ------------------ Top Executive Header Card ------------------
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
            Text = "🔌 OFFLINE USB SOFTWARE & DRIVER DEPOT",
            FontSize = 14,
            FontWeight = FontWeights.Bold,
            Foreground = SvllBlue
        };
        var subTitle = new TextBlock
        {
            Text = "Rapid PC & Laptop Staging: Batch USB Silent Installer, Microsoft WHQL Driver Finder & 1-Click Driver Export",
            FontSize = 11,
            Foreground = TextSubtle,
            Margin = new Thickness(0, 2, 0, 0)
        };
        titleStack.Children.Add(mainTitle);
        titleStack.Children.Add(subTitle);
        headerGrid.Children.Add(titleStack);

        // Sub-Tab Switcher Pills
        var pillStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

        _btnTabUsbDeployer = CreateSubTabPill("🔌 USB Offline Deployer", true);
        _btnTabDriverFinder = CreateSubTabPill("🌐 Auto Driver Finder (WHQL)", false);
        _btnTabDriverBackup = CreateSubTabPill("💾 Driver Backup to USB", false);

        _btnTabUsbDeployer.Click += (s, e) => SwitchDepotTab(0);
        _btnTabDriverFinder.Click += (s, e) => SwitchDepotTab(1);
        _btnTabDriverBackup.Click += (s, e) => SwitchDepotTab(2);

        pillStack.Children.Add(_btnTabUsbDeployer);
        pillStack.Children.Add(_btnTabDriverFinder);
        pillStack.Children.Add(_btnTabDriverBackup);

        Grid.SetColumn(pillStack, 1);
        headerGrid.Children.Add(pillStack);
        headerCard.Child = headerGrid;
        root.Children.Add(headerCard);

        // ------------------ Section 1: Offline USB Deployer Panel ------------------
        _panelUsbDeployer = BuildUsbDeployerSection();
        root.Children.Add(_panelUsbDeployer);

        // ------------------ Section 2: Automated Driver Finder Panel ------------------
        _panelDriverFinder = BuildDriverFinderSection();
        _panelDriverFinder.Visibility = Visibility.Collapsed;
        root.Children.Add(_panelDriverFinder);

        // ------------------ Section 3: Driver Backup Panel ------------------
        _panelDriverBackup = BuildDriverBackupSection();
        _panelDriverBackup.Visibility = Visibility.Collapsed;
        root.Children.Add(_panelDriverBackup);

        scroll.Content = root;

        // Auto initialize drives & query OEM info asynchronously
        Dispatcher.BeginInvoke(new Action(() =>
        {
            RefreshUsbDrivesList();
            _ = Task.Run(DetectOemSystemSpecsAsync);
        }));

        return scroll;
    }

    private Button CreateSubTabPill(string label, bool isActive)
    {
        var btn = new Button
        {
            Content = label,
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Padding = new Thickness(12, 6, 12, 6),
            Margin = new Thickness(4, 0, 0, 0),
            BorderThickness = new Thickness(1),
            Cursor = Cursors.Hand
        };
        UpdatePillVisual(btn, isActive);
        return btn;
    }

    private void UpdatePillVisual(Button btn, bool isActive)
    {
        if (isActive)
        {
            btn.Background = SvllBlue;
            btn.Foreground = Brushes.White;
            btn.BorderBrush = SvllBlue;
        }
        else
        {
            btn.Background = new SolidColorBrush(Color.FromRgb(241, 245, 249));
            btn.Foreground = TextDark;
            btn.BorderBrush = BorderMuted;
        }
    }

    private void SwitchDepotTab(int tabIndex)
    {
        UpdatePillVisual(_btnTabUsbDeployer, tabIndex == 0);
        UpdatePillVisual(_btnTabDriverFinder, tabIndex == 1);
        UpdatePillVisual(_btnTabDriverBackup, tabIndex == 2);

        _panelUsbDeployer.Visibility = tabIndex == 0 ? Visibility.Visible : Visibility.Collapsed;
        _panelDriverFinder.Visibility = tabIndex == 1 ? Visibility.Visible : Visibility.Collapsed;
        _panelDriverBackup.Visibility = tabIndex == 2 ? Visibility.Visible : Visibility.Collapsed;

        if (tabIndex == 1 && _missingDevices.Count == 0 && _whqlDrivers.Count == 0)
        {
            _ = ScanMissingDevicesAndWhqlDriversAsync(false);
        }
    }

    #region Tab 1: Offline USB Software & Driver Deployer

    private Border BuildUsbDeployerSection()
    {
        var card = new Border
        {
            Background = BgCard,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16),
            Margin = new Thickness(0, 0, 0, 14)
        };

        var root = new StackPanel();

        // 1. USB Selection & Drive Status Dock
        var driveDock = new DockPanel { Margin = new Thickness(0, 0, 0, 12) };

        var lblDrive = new TextBlock
        {
            Text = "SELECT USB DRIVE OR DEPOT REPOSITORY:",
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            Foreground = SvllBlue,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 10, 0)
        };
        driveDock.Children.Add(lblDrive);

        var btnRescanDrives = new Button
        {
            Content = "🔄 Rescan Drives",
            FontSize = 11,
            Padding = new Thickness(10, 4, 10, 4),
            Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
            BorderBrush = BorderMuted,
            Margin = new Thickness(6, 0, 0, 0)
        };
        DockPanel.SetDock(btnRescanDrives, Dock.Right);
        btnRescanDrives.Click += (s, e) => RefreshUsbDrivesList();
        driveDock.Children.Add(btnRescanDrives);

        var btnBrowseFolder = new Button
        {
            Content = "📂 Browse Folder...",
            FontSize = 11,
            Padding = new Thickness(10, 4, 10, 4),
            Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
            BorderBrush = BorderMuted,
            Margin = new Thickness(6, 0, 0, 0)
        };
        DockPanel.SetDock(btnBrowseFolder, Dock.Right);
        btnBrowseFolder.Click += (s, e) => BrowseDepotFolder();
        driveDock.Children.Add(btnBrowseFolder);

        var btnSetupStructure = new Button
        {
            Content = "📁 Init USB Structure",
            ToolTip = "Creates recommended \\SVLL_Depot\\Software, \\Drivers, and sample deploy.json on the selected drive",
            FontSize = 11,
            Padding = new Thickness(10, 4, 10, 4),
            Background = new SolidColorBrush(Color.FromRgb(239, 246, 255)),
            Foreground = SvllBlue,
            BorderBrush = new SolidColorBrush(Color.FromRgb(191, 219, 254)),
            Margin = new Thickness(6, 0, 0, 0)
        };
        DockPanel.SetDock(btnSetupStructure, Dock.Right);
        btnSetupStructure.Click += (s, e) => InitUsbDepotFolderStructure();
        driveDock.Children.Add(btnSetupStructure);

        _cmbUsbDrives = new ComboBox
        {
            FontSize = 11,
            Height = 28,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        _cmbUsbDrives.SelectionChanged += (s, e) => OnUsbDriveSelectionChanged();
        driveDock.Children.Add(_cmbUsbDrives);

        root.Children.Add(driveDock);

        // Path indicator if custom folder
        _txtCustomFolderPath = new TextBox
        {
            FontSize = 11,
            Padding = new Thickness(6, 4, 6, 4),
            Margin = new Thickness(0, 0, 0, 10),
            BorderBrush = BorderMuted,
            Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
            IsReadOnly = true,
            Visibility = Visibility.Collapsed
        };
        root.Children.Add(_txtCustomFolderPath);

        // 2. Filter & Action Strip
        var filterStrip = new DockPanel { Margin = new Thickness(0, 0, 0, 10) };

        var btnScanNow = new Button
        {
            Content = "🔍 Scan USB for Software & Drivers",
            FontWeight = FontWeights.Bold,
            FontSize = 11,
            Padding = new Thickness(12, 5, 12, 5),
            Background = SvllBlue,
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            Margin = new Thickness(0, 0, 8, 0)
        };
        btnScanNow.Click += async (s, e) => await ScanUsbDepotDirectoryAsync();
        filterStrip.Children.Add(btnScanNow);

        var btnSelectAll = new Button
        {
            Content = "☑️ Select All",
            FontSize = 10.5,
            Padding = new Thickness(8, 4, 8, 4),
            Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
            BorderBrush = BorderMuted,
            Margin = new Thickness(0, 0, 4, 0)
        };
        btnSelectAll.Click += (s, e) => SetAllUsbItemsSelection(true);
        filterStrip.Children.Add(btnSelectAll);

        var btnDeselectAll = new Button
        {
            Content = "⬜ Deselect All",
            FontSize = 10.5,
            Padding = new Thickness(8, 4, 8, 4),
            Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
            BorderBrush = BorderMuted,
            Margin = new Thickness(0, 0, 10, 0)
        };
        btnDeselectAll.Click += (s, e) => SetAllUsbItemsSelection(false);
        filterStrip.Children.Add(btnDeselectAll);

        // Search text box
        _txtUsbSearch = new TextBox
        {
            FontSize = 11,
            Height = 26,
            Padding = new Thickness(6, 2, 6, 2),
            BorderBrush = BorderMuted,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        _txtUsbSearch.TextChanged += (s, e) => ApplyUsbFilter();
        DockPanel.SetDock(_txtUsbSearch, Dock.Right);
        filterStrip.Children.Add(_txtUsbSearch);

        var lblSearch = new TextBlock
        {
            Text = "Filter:",
            FontSize = 11,
            Foreground = TextSubtle,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(10, 0, 4, 0)
        };
        DockPanel.SetDock(lblSearch, Dock.Right);
        filterStrip.Children.Add(lblSearch);

        // Category Filter Buttons
        var filterPillStack = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(6, 0, 6, 0) };
        string[] categories = { "ALL", "SOFTWARE", "DRIVERS", "SCRIPTS" };
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
                _activeUsbFilter = cat;
                foreach (UIElement child in filterPillStack.Children)
                {
                    if (child is Button b)
                    {
                        bool isSel = (string)b.Content == cat;
                        b.Background = isSel ? new SolidColorBrush(Color.FromRgb(219, 234, 254)) : new SolidColorBrush(Color.FromRgb(248, 250, 252));
                        b.Foreground = isSel ? SvllBlue : TextDark;
                    }
                }
                ApplyUsbFilter();
            };
            filterPillStack.Children.Add(pBtn);
        }
        filterStrip.Children.Add(filterPillStack);

        root.Children.Add(filterStrip);

        // 3. Scanned Packages ListView
        _listUsbItems = new ListView
        {
            Height = 280,
            FontSize = 11,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            Margin = new Thickness(0, 0, 0, 10)
        };

        var gv = new GridView();

        // Column 1: Checkbox
        var chkCol = new GridViewColumn { Header = "Install", Width = 55 };
        var chkFactory = new FrameworkElementFactory(typeof(CheckBox));
        chkFactory.SetBinding(ToggleButton.IsCheckedProperty, new Binding("IsSelected") { Mode = BindingMode.TwoWay });
        chkFactory.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        chkCol.CellTemplate = new DataTemplate { VisualTree = chkFactory };
        gv.Columns.Add(chkCol);

        // Column 2: Type / Category Badge
        var badgeCol = new GridViewColumn { Header = "Type", Width = 95 };
        var badgeFactory = new FrameworkElementFactory(typeof(Border));
        badgeFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(4));
        badgeFactory.SetValue(Border.PaddingProperty, new Thickness(6, 2, 6, 2));
        badgeFactory.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Left);
        
        var badgeText = new FrameworkElementFactory(typeof(TextBlock));
        badgeText.SetBinding(TextBlock.TextProperty, new Binding("CategoryBadge"));
        badgeText.SetValue(TextBlock.FontWeightProperty, FontWeights.Bold);
        badgeText.SetValue(TextBlock.FontSizeProperty, 10.0);
        badgeText.SetValue(TextBlock.ForegroundProperty, Brushes.White);
        badgeFactory.AppendChild(badgeText);

        badgeFactory.SetBinding(Border.BackgroundProperty, new Binding("CategoryColor")
        {
            Converter = new StringToBrushConverter()
        });
        badgeCol.CellTemplate = new DataTemplate { VisualTree = badgeFactory };
        gv.Columns.Add(badgeCol);

        // Column 3: Name & Relative Path
        gv.Columns.Add(new GridViewColumn { Header = "Package / Driver Name", Width = 230, DisplayMemberBinding = new Binding("Name") });
        gv.Columns.Add(new GridViewColumn { Header = "Relative Folder", Width = 160, DisplayMemberBinding = new Binding("RelativePath") });
        gv.Columns.Add(new GridViewColumn { Header = "Size", Width = 80, DisplayMemberBinding = new Binding("FileSizeFormatted") });

        // Column 4: Command / Silent Arguments (Editable)
        var cmdCol = new GridViewColumn { Header = "Silent Execution Command & Switches", Width = 310 };
        var cmdFactory = new FrameworkElementFactory(typeof(TextBox));
        cmdFactory.SetBinding(TextBox.TextProperty, new Binding("SilentCommand") { Mode = BindingMode.TwoWay });
        cmdFactory.SetValue(TextBox.FontSizeProperty, 10.5);
        cmdFactory.SetValue(TextBox.PaddingProperty, new Thickness(4, 1, 4, 1));
        cmdFactory.SetValue(TextBox.BorderBrushProperty, BorderMuted);
        cmdFactory.SetValue(TextBox.BackgroundProperty, new SolidColorBrush(Color.FromRgb(248, 250, 252)));
        cmdCol.CellTemplate = new DataTemplate { VisualTree = cmdFactory };
        gv.Columns.Add(cmdCol);

        // Column 5: Status
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

        _listUsbItems.View = gv;
        root.Children.Add(_listUsbItems);

        // 4. Batch Installation Progress & Action Execution Bar
        _progUsbBatch = new ProgressBar
        {
            Height = 6,
            Margin = new Thickness(0, 0, 0, 6),
            Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129)),
            Background = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
            Visibility = Visibility.Collapsed
        };
        root.Children.Add(_progUsbBatch);

        _lblUsbBatchStatus = new TextBlock
        {
            Text = "Select packages above and click 'Install Selected from USB' to begin sequential silent deployment.",
            FontSize = 11,
            Foreground = TextSubtle,
            Margin = new Thickness(0, 0, 0, 10)
        };
        root.Children.Add(_lblUsbBatchStatus);

        var actionDock = new DockPanel();

        _btnBatchInstall = new Button
        {
            Content = "🚀 1-CLICK INSTALL ALL SELECTED FROM USB",
            FontSize = 12,
            FontWeight = FontWeights.Bold,
            Padding = new Thickness(18, 9, 18, 9),
            Background = new SolidColorBrush(Color.FromRgb(16, 185, 129)), // Emerald Green
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand
        };
        _btnBatchInstall.Click += async (s, e) => await ExecuteBatchUsbInstallAsync();
        actionDock.Children.Add(_btnBatchInstall);

        _btnAbortBatch = new Button
        {
            Content = "⏹️ Abort Queue",
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Padding = new Thickness(12, 9, 12, 9),
            Background = new SolidColorBrush(Color.FromRgb(254, 242, 242)),
            Foreground = SvllRed,
            BorderBrush = new SolidColorBrush(Color.FromRgb(254, 202, 202)),
            Margin = new Thickness(8, 0, 0, 0),
            IsEnabled = false
        };
        _btnAbortBatch.Click += (s, e) => CancelBatchInstallation();
        actionDock.Children.Add(_btnAbortBatch);

        var optStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        DockPanel.SetDock(optStack, Dock.Right);

        _chkCreateRestorePoint = new CheckBox
        {
            Content = "Create System Restore Point before install",
            IsChecked = false,
            FontSize = 10.5,
            Margin = new Thickness(0, 0, 12, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        optStack.Children.Add(_chkCreateRestorePoint);

        _chkSaveDeployJson = new CheckBox
        {
            Content = "Save deploy.json manifest to USB",
            IsChecked = true,
            FontSize = 10.5,
            VerticalAlignment = VerticalAlignment.Center
        };
        optStack.Children.Add(_chkSaveDeployJson);

        actionDock.Children.Add(optStack);
        root.Children.Add(actionDock);

        card.Child = root;
        return card;
    }

    #endregion

    #region Tab 2: Automated System Driver Finder (WHQL & PnP Yellow Bangs)

    private Border BuildDriverFinderSection()
    {
        var card = new Border
        {
            Background = BgCard,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16),
            Margin = new Thickness(0, 0, 0, 14)
        };

        var root = new StackPanel();

        // 1. OEM System Identification & Direct Assistant Card
        var oemCard = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(240, 249, 255)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(186, 230, 253)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(14, 10, 14, 10),
            Margin = new Thickness(0, 0, 0, 14)
        };

        var oemGrid = new Grid();
        oemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        oemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var oemTextStack = new StackPanel();
        var lblOemTitle = new TextBlock
        {
            Text = "DETECTED SYSTEM HARDWARE & OEM SPECIFICATIONS",
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            Foreground = SvllBlue
        };
        _lblOemSystemInfo = new TextBlock
        {
            Text = "Identifying Motherboard, Model, and OEM Service Tag...",
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Foreground = TextDark,
            Margin = new Thickness(0, 3, 0, 0)
        };
        oemTextStack.Children.Add(lblOemTitle);
        oemTextStack.Children.Add(_lblOemSystemInfo);
        oemGrid.Children.Add(oemTextStack);

        var oemBtnStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

        var btnOpenOemWeb = new Button
        {
            Content = "🌐 Open OEM Driver Portal",
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Padding = new Thickness(10, 5, 10, 5),
            Background = SvllBlue,
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            Margin = new Thickness(4, 0, 0, 0)
        };
        btnOpenOemWeb.Click += (s, e) => OpenOemDriverPortal();
        oemBtnStack.Children.Add(btnOpenOemWeb);

        var btnLaunchOemApp = new Button
        {
            Content = "🚀 Launch OEM Update Tool",
            ToolTip = "Runs Dell Command Update, Lenovo Vantage, HP Support Assistant, or Intel Driver Assistant if installed",
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Padding = new Thickness(10, 5, 10, 5),
            Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
            BorderBrush = BorderMuted,
            Margin = new Thickness(6, 0, 0, 0)
        };
        btnLaunchOemApp.Click += (s, e) => LaunchOrDownloadOemDriverTool();
        oemBtnStack.Children.Add(btnLaunchOemApp);

        Grid.SetColumn(oemBtnStack, 1);
        oemGrid.Children.Add(oemBtnStack);
        oemCard.Child = oemGrid;
        root.Children.Add(oemCard);

        // 2. Hardware Diagnostic: Missing & Problem Devices (Yellow Bangs)
        var missingHeaderDock = new DockPanel { Margin = new Thickness(0, 0, 0, 8) };

        _lblMissingDevicesHeader = new TextBlock
        {
            Text = "1. MISSING OR UNCONFIGURED HARDWARE DEVICES (YELLOW EXCLAMATION MARKS):",
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            Foreground = SvllBlue,
            VerticalAlignment = VerticalAlignment.Center
        };
        missingHeaderDock.Children.Add(_lblMissingDevicesHeader);

        var btnScanMissing = new Button
        {
            Content = "🔄 Rescan Hardware Devices",
            FontSize = 10.5,
            Padding = new Thickness(8, 3, 8, 3),
            Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
            BorderBrush = BorderMuted,
            Margin = new Thickness(6, 0, 0, 0)
        };
        DockPanel.SetDock(btnScanMissing, Dock.Right);
        btnScanMissing.Click += async (s, e) => await ScanMissingDevicesAndWhqlDriversAsync(false);
        missingHeaderDock.Children.Add(btnScanMissing);

        var btnPnpRebind = new Button
        {
            Content = "⚡ PnP Auto-Bind (pnputil /scan-devices)",
            ToolTip = "Triggers immediate kernel PnP bus re-enumeration to bind newly staged drivers",
            FontSize = 10.5,
            Padding = new Thickness(8, 3, 8, 3),
            Background = new SolidColorBrush(Color.FromRgb(239, 246, 255)),
            Foreground = SvllBlue,
            BorderBrush = new SolidColorBrush(Color.FromRgb(191, 219, 254)),
            Margin = new Thickness(6, 0, 0, 0)
        };
        DockPanel.SetDock(btnPnpRebind, Dock.Right);
        btnPnpRebind.Click += async (s, e) => await TriggerPnpRescanAsync();
        missingHeaderDock.Children.Add(btnPnpRebind);

        root.Children.Add(missingHeaderDock);

        _listMissingDevices = new ListView
        {
            Height = 130,
            FontSize = 11,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            Margin = new Thickness(0, 0, 0, 14)
        };

        var gvMissing = new GridView();
        gvMissing.Columns.Add(new GridViewColumn { Header = "Device Description", Width = 230, DisplayMemberBinding = new Binding("DeviceName") });
        gvMissing.Columns.Add(new GridViewColumn { Header = "Class", Width = 90, DisplayMemberBinding = new Binding("ClassName") });
        gvMissing.Columns.Add(new GridViewColumn { Header = "Manufacturer", Width = 130, DisplayMemberBinding = new Binding("Manufacturer") });
        gvMissing.Columns.Add(new GridViewColumn { Header = "Hardware ID", Width = 280, DisplayMemberBinding = new Binding("HardwareId") });
        gvMissing.Columns.Add(new GridViewColumn { Header = "Problem State", Width = 180, DisplayMemberBinding = new Binding("ProblemDescription") });
        _listMissingDevices.View = gvMissing;
        root.Children.Add(_listMissingDevices);

        // 3. Microsoft WHQL Windows Update Driver Catalog Section
        var whqlHeaderDock = new DockPanel { Margin = new Thickness(0, 0, 0, 8) };

        var lblWhqlHeader = new TextBlock
        {
            Text = "2. AUTOMATED BEST DRIVERS FROM MICROSOFT WHQL CATALOG:",
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            Foreground = SvllBlue,
            VerticalAlignment = VerticalAlignment.Center
        };
        whqlHeaderDock.Children.Add(lblWhqlHeader);

        var btnSearchWhql = new Button
        {
            Content = "🔍 Search Windows Update WHQL Drivers",
            FontWeight = FontWeights.SemiBold,
            FontSize = 11,
            Padding = new Thickness(12, 4, 12, 4),
            Background = SvllBlue,
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            Margin = new Thickness(6, 0, 0, 0)
        };
        DockPanel.SetDock(btnSearchWhql, Dock.Right);
        btnSearchWhql.Click += async (s, e) => await ScanMissingDevicesAndWhqlDriversAsync(true);
        whqlHeaderDock.Children.Add(btnSearchWhql);

        root.Children.Add(whqlHeaderDock);

        _listWhqlDrivers = new ListView
        {
            Height = 170,
            FontSize = 11,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            Margin = new Thickness(0, 0, 0, 10)
        };

        var gvWhql = new GridView();
        var chkWhqlCol = new GridViewColumn { Header = "Install", Width = 55 };
        var chkWhqlFactory = new FrameworkElementFactory(typeof(CheckBox));
        chkWhqlFactory.SetBinding(ToggleButton.IsCheckedProperty, new Binding("IsSelected") { Mode = BindingMode.TwoWay });
        chkWhqlFactory.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        chkWhqlCol.CellTemplate = new DataTemplate { VisualTree = chkWhqlFactory };
        gvWhql.Columns.Add(chkWhqlCol);

        gvWhql.Columns.Add(new GridViewColumn { Header = "Certified Driver Package Title", Width = 340, DisplayMemberBinding = new Binding("Title") });
        gvWhql.Columns.Add(new GridViewColumn { Header = "Hardware Class", Width = 110, DisplayMemberBinding = new Binding("DriverClass") });
        gvWhql.Columns.Add(new GridViewColumn { Header = "Provider", Width = 150, DisplayMemberBinding = new Binding("DriverProvider") });
        gvWhql.Columns.Add(new GridViewColumn { Header = "Version / Date", Width = 130, DisplayMemberBinding = new Binding("DriverDate") });

        var statusWhqlCol = new GridViewColumn { Header = "Status", Width = 160 };
        var statusWhqlFactory = new FrameworkElementFactory(typeof(TextBlock));
        statusWhqlFactory.SetBinding(TextBlock.TextProperty, new Binding("Status"));
        statusWhqlFactory.SetValue(TextBlock.FontWeightProperty, FontWeights.SemiBold);
        statusWhqlFactory.SetBinding(TextBlock.ForegroundProperty, new Binding("StatusColor")
        {
            Converter = new StringToBrushConverter()
        });
        statusWhqlCol.CellTemplate = new DataTemplate { VisualTree = statusWhqlFactory };
        gvWhql.Columns.Add(statusWhqlCol);

        _listWhqlDrivers.View = gvWhql;
        root.Children.Add(_listWhqlDrivers);

        // 4. Progress and 1-Click Install Button
        _progDriverScan = new ProgressBar
        {
            Height = 6,
            Margin = new Thickness(0, 0, 0, 6),
            Foreground = SvllBlue,
            Background = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
            Visibility = Visibility.Collapsed
        };
        root.Children.Add(_progDriverScan);

        _lblDriverScanStatus = new TextBlock
        {
            Text = "Click 'Search Windows Update WHQL Drivers' to automatically match certified drivers against laptop hardware IDs.",
            FontSize = 11,
            Foreground = TextSubtle,
            Margin = new Thickness(0, 0, 0, 10)
        };
        root.Children.Add(_lblDriverScanStatus);

        var whqlActionDock = new DockPanel();

        _btnWhqlInstall = new Button
        {
            Content = "⚡ 1-CLICK DOWNLOAD & INSTALL BEST DRIVERS (MICROSOFT WHQL)",
            FontSize = 12,
            FontWeight = FontWeights.Bold,
            Padding = new Thickness(18, 9, 18, 9),
            Background = SvllBlue,
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand
        };
        _btnWhqlInstall.Click += async (s, e) => await ExecuteWhqlDriverInstallAsync();
        whqlActionDock.Children.Add(_btnWhqlInstall);

        root.Children.Add(whqlActionDock);

        card.Child = root;
        return card;
    }

    #endregion

    #region Tab 3: Driver Backup / Export to USB

    private Border BuildDriverBackupSection()
    {
        var card = new Border
        {
            Background = BgCard,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16),
            Margin = new Thickness(0, 0, 0, 14)
        };

        var root = new StackPanel();

        var descText = new TextBlock
        {
            Text = "EXPORT INSTALLED OEM DRIVERS TO USB REPOSITORY",
            FontSize = 12,
            FontWeight = FontWeights.Bold,
            Foreground = SvllBlue,
            Margin = new Thickness(0, 0, 0, 4)
        };
        var subDesc = new TextBlock
        {
            Text = "Backs up all installed third-party drivers (Audio, Wi-Fi, Chipset, GPU, LAN) directly into cleanly structured .INF folders on your USB drive. These can be deployed offline to other PCs in 30 seconds via Tab 1.",
            FontSize = 11,
            Foreground = TextSubtle,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 14)
        };
        root.Children.Add(descText);
        root.Children.Add(subDesc);

        // Destination selector
        var destDock = new DockPanel { Margin = new Thickness(0, 0, 0, 12) };
        var lblDest = new TextBlock
        {
            Text = "Target Export Directory:",
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            Foreground = TextDark,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0)
        };
        destDock.Children.Add(lblDest);

        var btnBrowseDest = new Button
        {
            Content = "📂 Choose Folder...",
            FontSize = 11,
            Padding = new Thickness(10, 4, 10, 4),
            Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
            BorderBrush = BorderMuted,
            Margin = new Thickness(6, 0, 0, 0)
        };
        DockPanel.SetDock(btnBrowseDest, Dock.Right);
        btnBrowseDest.Click += (s, e) =>
        {
            var dlg = new OpenFolderDialog();
            dlg.Title = "Select Destination Folder for Driver Backup";
            if (dlg.ShowDialog() == true)
            {
                _txtExportTargetDir.Text = dlg.FolderName;
            }
        };
        destDock.Children.Add(btnBrowseDest);

        _txtExportTargetDir = new TextBox
        {
            FontSize = 11,
            Padding = new Thickness(6, 4, 6, 4),
            Text = @"D:\SVLL_Depot\Drivers\Backup",
            BorderBrush = BorderMuted
        };
        destDock.Children.Add(_txtExportTargetDir);
        root.Children.Add(destDock);

        _progDriverExport = new ProgressBar
        {
            Height = 6,
            Margin = new Thickness(0, 0, 0, 6),
            Foreground = SvllBlue,
            Background = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
            Visibility = Visibility.Collapsed
        };
        root.Children.Add(_progDriverExport);

        _lblDriverExportStatus = new TextBlock
        {
            Text = "Ready to export drivers using Windows DISM / PnP driver store engine.",
            FontSize = 11,
            Foreground = TextSubtle,
            Margin = new Thickness(0, 0, 0, 12)
        };
        root.Children.Add(_lblDriverExportStatus);

        _btnExportDrivers = new Button
        {
            Content = "💾 1-CLICK EXPORT ALL INSTALLED DRIVERS",
            FontSize = 12,
            FontWeight = FontWeights.Bold,
            Padding = new Thickness(18, 9, 18, 9),
            Background = SvllBlue,
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            HorizontalAlignment = HorizontalAlignment.Left,
            Cursor = Cursors.Hand
        };
        _btnExportDrivers.Click += async (s, e) => await ExecuteDriverBackupAsync();
        root.Children.Add(_btnExportDrivers);

        card.Child = root;
        return card;
    }

    #endregion

    #region USB Drive Detection & Scanning Engine

    private void RefreshUsbDrivesList()
    {
        _cmbUsbDrives.Items.Clear();

        try
        {
            var drives = DriveInfo.GetDrives().Where(d => d.IsReady).ToList();
            int selectedIndex = -1;

            for (int i = 0; i < drives.Count; i++)
            {
                var d = drives[i];
                string driveTypeStr = d.DriveType == DriveType.Removable ? "🔌 USB Removable" : "💾 " + d.DriveType.ToString();
                string label = string.IsNullOrEmpty(d.VolumeLabel) ? "No Label" : d.VolumeLabel;
                double freeGb = Math.Round((double)d.AvailableFreeSpace / (1024 * 1024 * 1024), 1);
                double totalGb = Math.Round((double)d.TotalSize / (1024 * 1024 * 1024), 1);

                string itemText = $"{d.Name} [{label}] ({driveTypeStr}, {freeGb} GB free of {totalGb} GB)";
                _cmbUsbDrives.Items.Add(new ComboBoxItem { Content = itemText, Tag = d.RootDirectory.FullName });

                if (selectedIndex == -1 && d.DriveType == DriveType.Removable)
                {
                    selectedIndex = i;
                }
            }

            if (selectedIndex != -1)
            {
                _cmbUsbDrives.SelectedIndex = selectedIndex;
            }
            else if (_cmbUsbDrives.Items.Count > 0)
            {
                _cmbUsbDrives.SelectedIndex = 0;
            }
            else
            {
                _cmbUsbDrives.Items.Add(new ComboBoxItem { Content = "No ready drives detected. Click Browse Folder...", Tag = "" });
                _cmbUsbDrives.SelectedIndex = 0;
            }
        }
        catch (Exception ex)
        {
            Log($"[USB DRIVE DETECT ERROR] {ex.Message}");
        }
    }

    private void OnUsbDriveSelectionChanged()
    {
        if (_cmbUsbDrives.SelectedItem is ComboBoxItem item && item.Tag is string rootPath && !string.IsNullOrEmpty(rootPath))
        {
            _txtCustomFolderPath.Text = rootPath;
            _txtCustomFolderPath.Visibility = Visibility.Visible;

            // Default driver export directory to this drive
            string modelFolder = string.IsNullOrEmpty(_detectedOemModel) ? "PC_Drivers" : SanitizeFileName(_detectedOemModel);
            _txtExportTargetDir.Text = Path.Combine(rootPath, "SVLL_Depot", "Drivers", modelFolder);

            // Auto scan this drive
            _ = ScanUsbDepotDirectoryAsync();
        }
    }

    private void BrowseDepotFolder()
    {
        var dlg = new OpenFolderDialog();
        dlg.Title = "Select USB Drive or Software/Driver Depot Folder";
        if (dlg.ShowDialog() == true)
        {
            string chosenPath = dlg.FolderName;
            _txtCustomFolderPath.Text = chosenPath;
            _txtCustomFolderPath.Visibility = Visibility.Visible;

            // Add or select in ComboBox
            var cbItem = new ComboBoxItem { Content = $"📂 Custom: {chosenPath}", Tag = chosenPath };
            _cmbUsbDrives.Items.Insert(0, cbItem);
            _cmbUsbDrives.SelectedIndex = 0;

            _ = ScanUsbDepotDirectoryAsync();
        }
    }

    private void InitUsbDepotFolderStructure()
    {
        string rootPath = _txtCustomFolderPath.Text.Trim();
        if (string.IsNullOrEmpty(rootPath) || !Directory.Exists(rootPath))
        {
            MessageBox.Show("Please select a valid USB drive first.", "Initialize USB Depot", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            string depotRoot = Path.Combine(rootPath, "SVLL_Depot");
            string swDir = Path.Combine(depotRoot, "Software");
            string drvDir = Path.Combine(depotRoot, "Drivers");
            string scrDir = Path.Combine(depotRoot, "Scripts");

            Directory.CreateDirectory(swDir);
            Directory.CreateDirectory(drvDir);
            Directory.CreateDirectory(scrDir);

            // Create readme
            string readmePath = Path.Combine(depotRoot, "README_DEPOT.txt");
            if (!File.Exists(readmePath))
            {
                File.WriteAllText(readmePath,
                    "SVLL IT WORKSTATION - OFFLINE USB DEPOT REPOSITORY\n" +
                    "===================================================\n" +
                    "1. Place software (.msi, .exe) in \\Software\n" +
                    "2. Place extracted hardware drivers (.inf) in \\Drivers\n" +
                    "3. Place automation scripts (.bat, .ps1) in \\Scripts\n" +
                    "When plugged into any new PC, SVLL Workstation will auto-scan and silently install all items.\n");
            }

            // Create sample manifest
            string manifestPath = Path.Combine(depotRoot, "deploy.json");
            if (!File.Exists(manifestPath))
            {
                var sampleManifest = new
                {
                    RepositoryName = "SVLL Corporate Deployment Depot",
                    CreatedBy = "SVLL IT Dept",
                    CreatedDate = DateTime.Now.ToString("yyyy-MM-dd"),
                    AutoInstallOnPlugin = false,
                    CreateRestorePoint = true
                };
                string json = JsonSerializer.Serialize(sampleManifest, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(manifestPath, json);
            }

            MessageBox.Show(
                $"USB Depot Directory Structure initialized successfully!\n\n" +
                $"• {swDir}\n" +
                $"• {drvDir}\n" +
                $"• {scrDir}\n" +
                $"• {manifestPath}\n\n" +
                "You can now copy your setup installers and driver folders into these directories.",
                "USB Depot Initialized", MessageBoxButton.OK, MessageBoxImage.Information);

            Log($"[USB DEPOT] Initialized repository structure at {depotRoot}");
            _ = ScanUsbDepotDirectoryAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to initialize USB depot: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task ScanUsbDepotDirectoryAsync()
    {
        string rootPath = _txtCustomFolderPath.Text.Trim();
        if (string.IsNullOrEmpty(rootPath) || !Directory.Exists(rootPath)) return;

        _progUsbBatch.Visibility = Visibility.Visible;
        _progUsbBatch.IsIndeterminate = true;
        _lblUsbBatchStatus.Text = $"Scanning '{rootPath}' for software packages, drivers, and scripts...";

        _allUsbItems.Clear();

        await Task.Run(() =>
        {
            try
            {
                var dirInfo = new DirectoryInfo(rootPath);
                var searchExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    ".msi", ".exe", ".inf", ".ps1", ".bat", ".cmd"
                };

                // Directories to skip
                var skipDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "$RECYCLE.BIN", "System Volume Information", ".git", ".vs", "Windows", "Program Files", "Program Files (x86)"
                };

                ScanDirectoryRecursive(dirInfo, rootPath, searchExtensions, skipDirs, _allUsbItems, 0, 5);
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => Log($"[USB SCAN ERROR] {ex.Message}"));
            }
        });

        _progUsbBatch.IsIndeterminate = false;
        _progUsbBatch.Visibility = Visibility.Collapsed;

        ApplyUsbFilter();
        _lblUsbBatchStatus.Text = $"Scan complete. Found {_allUsbItems.Count} package(s) on depot.";
        Log($"[USB DEPOT] Scanned {rootPath}: found {_allUsbItems.Count} deployable packages.");
    }

    private void ScanDirectoryRecursive(DirectoryInfo dir, string rootPath, HashSet<string> validExts, HashSet<string> skipDirs, List<UsbDepotItem> items, int depth, int maxDepth)
    {
        if (depth > maxDepth) return;

        FileInfo[] files;
        try
        {
            files = dir.GetFiles();
        }
        catch
        {
            return;
        }

        foreach (var f in files)
        {
            string ext = f.Extension.ToLowerInvariant();
            if (validExts.Contains(ext))
            {
                // Skip setup files that are uninstaller helpers
                string fName = f.Name.ToLowerInvariant();
                if (fName.Contains("unins000") || fName.Contains("uninstall")) continue;

                var item = CreateDepotItemFromFile(f, rootPath);
                items.Add(item);
            }
        }

        DirectoryInfo[] subDirs;
        try
        {
            subDirs = dir.GetDirectories();
        }
        catch
        {
            return;
        }

        foreach (var sub in subDirs)
        {
            if (skipDirs.Contains(sub.Name) || sub.Name.StartsWith(".")) continue;
            ScanDirectoryRecursive(sub, rootPath, validExts, skipDirs, items, depth + 1, maxDepth);
        }
    }

    private UsbDepotItem CreateDepotItemFromFile(FileInfo f, string rootPath)
    {
        string ext = f.Extension.ToLowerInvariant();
        string relPath = "";
        try
        {
            relPath = Path.GetRelativePath(rootPath, f.DirectoryName ?? rootPath);
            if (relPath == ".") relPath = "\\ (Root)";
            else relPath = "\\" + relPath;
        }
        catch
        {
            relPath = f.DirectoryName ?? "";
        }

        var item = new UsbDepotItem
        {
            Name = f.Name,
            FullPath = f.FullName,
            RelativePath = relPath,
            Extension = ext,
            FileSizeBytes = f.Length,
            FileSizeFormatted = FormatBytes(f.Length),
            IsSelected = true,
            Status = "Ready",
            StatusColor = "#64748B"
        };

        // Classify Category & Detect Silent Install Arguments
        switch (ext)
        {
            case ".msi":
                item.Category = UsbItemCategory.Software;
                item.CategoryBadge = "[MSI SETUP]";
                item.CategoryColor = "#2563EB"; // Blue
                item.SilentCommand = $"msiexec.exe /i \"{f.FullName}\" /qn /norestart";
                break;

            case ".inf":
                item.Category = UsbItemCategory.Driver;
                item.CategoryBadge = "[DRIVER INF]";
                item.CategoryColor = "#10B981"; // Emerald
                item.SilentCommand = $"pnputil.exe /add-driver \"{f.FullName}\" /install";
                break;

            case ".ps1":
                item.Category = UsbItemCategory.Script;
                item.CategoryBadge = "[POWERSHELL]";
                item.CategoryColor = "#8B5CF6"; // Purple
                item.SilentCommand = $"powershell.exe -NoProfile -ExecutionPolicy Bypass -File \"{f.FullName}\"";
                break;

            case ".bat":
            case ".cmd":
                item.Category = UsbItemCategory.Script;
                item.CategoryBadge = "[BATCH CMD]";
                item.CategoryColor = "#D97706"; // Amber
                item.SilentCommand = $"cmd.exe /c \"{f.FullName}\"";
                break;

            case ".exe":
            default:
                item.Category = UsbItemCategory.Software;
                item.CategoryBadge = "[EXE SETUP]";
                item.CategoryColor = "#0284C7"; // Sky Blue
                item.SilentCommand = DetectSilentExeSwitches(f);
                break;
        }

        return item;
    }

    private string DetectSilentExeSwitches(FileInfo f)
    {
        string name = f.Name.ToLowerInvariant();

        if (name.Contains("chrome") || name.Contains("firefox") || name.Contains("edge"))
        {
            return $"\"{f.FullName}\" /silent /install";
        }
        if (name.Contains("anydesk"))
        {
            return $"\"{f.FullName}\" --install \"C:\\Program Files (x86)\\AnyDesk\" --start-with-win --silent";
        }
        if (name.Contains("teamviewer"))
        {
            return $"\"{f.FullName}\" /S";
        }
        if (name.Contains("7z") || name.Contains("7-zip") || name.Contains("vlc") || name.Contains("notepad++"))
        {
            return $"\"{f.FullName}\" /S";
        }
        if (name.Contains("adobe") || name.Contains("acrobat") || name.Contains("reader"))
        {
            return $"\"{f.FullName}\" /sAll /rs /msi EULA_ACCEPT=YES";
        }

        // Standard Universal Silent Switches for Inno Setup / NSIS / InstallShield
        return $"\"{f.FullName}\" /VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP-";
    }

    private void ApplyUsbFilter()
    {
        string query = _txtUsbSearch.Text.Trim().ToLowerInvariant();

        var filtered = _allUsbItems.Where(it =>
        {
            // Category filter
            if (_activeUsbFilter == "SOFTWARE" && it.Category != UsbItemCategory.Software) return false;
            if (_activeUsbFilter == "DRIVERS" && it.Category != UsbItemCategory.Driver) return false;
            if (_activeUsbFilter == "SCRIPTS" && it.Category != UsbItemCategory.Script) return false;

            // Search query
            if (!string.IsNullOrEmpty(query))
            {
                if (!it.Name.ToLowerInvariant().Contains(query) &&
                    !it.RelativePath.ToLowerInvariant().Contains(query) &&
                    !it.CategoryBadge.ToLowerInvariant().Contains(query))
                {
                    return false;
                }
            }

            return true;
        }).ToList();

        _listUsbItems.ItemsSource = filtered;
    }

    private void SetAllUsbItemsSelection(bool select)
    {
        foreach (var it in _allUsbItems)
        {
            it.IsSelected = select;
        }
        _listUsbItems.Items.Refresh();
    }

    private static string FormatBytes(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        int i = 0;
        double d = bytes;
        while (d >= 1024 && i < suffixes.Length - 1)
        {
            d /= 1024;
            i++;
        }
        return $"{d:0.##} {suffixes[i]}";
    }

    #endregion

    #region Tab 1 Execution Engine: Sequential Silent Batch Installer

    private async Task ExecuteBatchUsbInstallAsync()
    {
        var selected = _allUsbItems.Where(it => it.IsSelected).ToList();
        if (selected.Count == 0)
        {
            MessageBox.Show("Please select at least one package or driver to install.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var confirm = MessageBox.Show(
            $"Are you ready to batch-install {selected.Count} package(s) from USB?\n\n" +
            "The workstation will execute all installations silently in sequence.",
            "Confirm Batch Installation", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes) return;

        _isBatchInstalling = true;
        _batchCancelCts = new CancellationTokenSource();
        _btnBatchInstall.IsEnabled = false;
        _btnAbortBatch.IsEnabled = true;
        _progUsbBatch.Visibility = Visibility.Visible;
        _progUsbBatch.Minimum = 0;
        _progUsbBatch.Maximum = selected.Count;
        _progUsbBatch.Value = 0;

        Log($"\n=======================================================");
        Log($"[USB BATCH DEPLOYMENT STARTED] Installing {selected.Count} packages...");
        Log($"=======================================================");

        // 1. Optional System Restore Point
        if (_chkCreateRestorePoint.IsChecked == true)
        {
            _lblUsbBatchStatus.Text = "Creating System Restore Point before installation...";
            Log("[RESTORE POINT] Initiating system checkpoint...");
            await ExecuteAsync("powershell.exe", "-NoProfile -ExecutionPolicy Bypass -Command \"Checkpoint-Computer -Description 'SVLL USB Depot Batch Deployment' -RestorePointType 'APPLICATION_INSTALL' -ErrorAction SilentlyContinue\"");
        }

        int successCount = 0;
        int failCount = 0;

        for (int i = 0; i < selected.Count; i++)
        {
            if (_batchCancelCts.IsCancellationRequested)
            {
                Log("[USB BATCH DEPLOYMENT] Queue aborted by operator.");
                break;
            }

            var item = selected[i];
            item.Status = "Installing...";
            item.StatusColor = "#2563EB"; // Blue
            _listUsbItems.Items.Refresh();

            _lblUsbBatchStatus.Text = $"[{i + 1}/{selected.Count}] Installing {item.Name}...";
            _progUsbBatch.Value = i;

            Log($"\n--> [{i + 1}/{selected.Count}] DEPLOYING: {item.Name} ({item.CategoryBadge})");
            Log($"    Command: {item.SilentCommand}");

            int exitCode = -1;
            try
            {
                exitCode = await RunDepotProcessAsync(item.SilentCommand, _batchCancelCts.Token);
                if (exitCode == 0 || exitCode == 3010) // 3010 = reboot required (success)
                {
                    item.Status = exitCode == 3010 ? "✅ Done (Reboot Req)" : "✅ Installed";
                    item.StatusColor = "#16A34A"; // Green
                    successCount++;
                    Log($"    Status: COMPLETED (Exit Code: {exitCode})");
                }
                else
                {
                    item.Status = $"❌ Error ({exitCode})";
                    item.StatusColor = "#DC2626"; // Red
                    failCount++;
                    Log($"    Status: FAILED with Exit Code: {exitCode}");
                }
            }
            catch (Exception ex)
            {
                item.Status = "❌ Failed";
                item.StatusColor = "#DC2626";
                failCount++;
                Log($"    Status: EXCEPTION: {ex.Message}");
            }

            _listUsbItems.Items.Refresh();
        }

        _progUsbBatch.Value = selected.Count;

        // Save deployment profile to USB if requested
        if (_chkSaveDeployJson.IsChecked == true)
        {
            SaveDepotManifestProfile(selected);
        }

        _isBatchInstalling = false;
        _btnBatchInstall.IsEnabled = true;
        _btnAbortBatch.IsEnabled = false;

        string summary = $"Batch Deployment Completed.\n\nSucceeded: {successCount}\nFailed: {failCount}\nTotal Processed: {selected.Count}";
        _lblUsbBatchStatus.Text = summary.Replace("\n\n", " | ").Replace("\n", " ");
        Log($"\n=======================================================");
        Log($"[USB BATCH DEPLOYMENT FINISHED] {summary}");
        Log($"=======================================================\n");

        MessageBox.Show(summary, "Batch Deployment Summary", MessageBoxButton.OK,
            failCount == 0 ? MessageBoxImage.Information : MessageBoxImage.Warning);
    }

    private void CancelBatchInstallation()
    {
        if (_isBatchInstalling && _batchCancelCts != null)
        {
            _batchCancelCts.Cancel();
            _lblUsbBatchStatus.Text = "Cancelling installation queue...";
        }
    }

    private async Task<int> RunDepotProcessAsync(string commandLine, CancellationToken ct)
    {
        return await Task.Run(() =>
        {
            // Parse executable and arguments
            string exe = "";
            string args = "";

            commandLine = commandLine.Trim();
            if (commandLine.StartsWith("\""))
            {
                int nextQuote = commandLine.IndexOf('"', 1);
                if (nextQuote != -1)
                {
                    exe = commandLine.Substring(1, nextQuote - 1);
                    args = commandLine.Substring(nextQuote + 1).Trim();
                }
                else
                {
                    exe = commandLine;
                }
            }
            else
            {
                int firstSpace = commandLine.IndexOf(' ');
                if (firstSpace != -1)
                {
                    exe = commandLine.Substring(0, firstSpace);
                    args = commandLine.Substring(firstSpace + 1).Trim();
                }
                else
                {
                    exe = commandLine;
                }
            }

            using var p = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = exe,
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            p.OutputDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    Dispatcher.Invoke(() => Log($"    [OUT] {e.Data}"));
                }
            };
            p.ErrorDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    Dispatcher.Invoke(() => Log($"    [ERR] {e.Data}"));
                }
            };

            p.Start();
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();

            while (!p.WaitForExit(500))
            {
                if (ct.IsCancellationRequested)
                {
                    try { p.Kill(); } catch { }
                    return -999;
                }
            }

            return p.ExitCode;
        });
    }

    private void SaveDepotManifestProfile(List<UsbDepotItem> items)
    {
        string rootPath = _txtCustomFolderPath.Text.Trim();
        if (string.IsNullOrEmpty(rootPath) || !Directory.Exists(rootPath)) return;

        try
        {
            string profilePath = Path.Combine(rootPath, "SVLL_Depot", "deploy_installed_profile.json");
            var profileData = new
            {
                HostName = Environment.MachineName,
                InstalledDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                Items = items.Select(it => new
                {
                    it.Name,
                    it.CategoryBadge,
                    it.RelativePath,
                    it.SilentCommand,
                    it.Status
                })
            };

            string json = JsonSerializer.Serialize(profileData, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(profilePath, json);
            Log($"[USB PROFILE] Saved deployment record to {profilePath}");
        }
        catch (Exception ex)
        {
            Log($"[USB PROFILE ERROR] {ex.Message}");
        }
    }

    #endregion

    #region Tab 2: Automated Driver Finder Implementation (WMI + WHQL + OEM)

    private async Task DetectOemSystemSpecsAsync()
    {
        string make = "Unknown";
        string model = "Unknown";
        string serial = "Unknown";

        await Task.Run(() =>
        {
            try
            {
                using var csSearcher = new ManagementObjectSearcher("SELECT Manufacturer, Model FROM Win32_ComputerSystem");
                foreach (var obj in csSearcher.Get())
                {
                    make = obj["Manufacturer"]?.ToString()?.Trim() ?? "Unknown";
                    model = obj["Model"]?.ToString()?.Trim() ?? "Unknown";
                    break;
                }

                using var biosSearcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BIOS");
                foreach (var obj in biosSearcher.Get())
                {
                    serial = obj["SerialNumber"]?.ToString()?.Trim() ?? "Unknown";
                    break;
                }
            }
            catch (Exception ex)
            {
                Log($"[OEM DETECT ERROR] {ex.Message}");
            }
        });

        _detectedOemMake = make;
        _detectedOemModel = model;
        _detectedOemSerial = serial;

        Dispatcher.Invoke(() =>
        {
            _lblOemSystemInfo.Text = $"Manufacturer: {make}  |  Model: {model}  |  Serial / Service Tag: {serial}";
        });
    }

    private void OpenOemDriverPortal()
    {
        string make = _detectedOemMake.ToLowerInvariant();
        string url = "https://www.google.com/search?q=" + Uri.EscapeDataString($"{_detectedOemMake} {_detectedOemModel} drivers support");

        if (make.Contains("dell"))
        {
            url = string.IsNullOrEmpty(_detectedOemSerial) || _detectedOemSerial == "Unknown"
                ? "https://www.dell.com/support/home/en-us?app=drivers"
                : $"https://www.dell.com/support/home/en-us/product-support/servicetag/{_detectedOemSerial}/drivers";
        }
        else if (make.Contains("lenovo"))
        {
            url = string.IsNullOrEmpty(_detectedOemSerial) || _detectedOemSerial == "Unknown"
                ? "https://pcsupport.lenovo.com/us/en/"
                : $"https://pcsupport.lenovo.com/us/en/products/search?query={_detectedOemSerial}";
        }
        else if (make.Contains("hp") || make.Contains("hewlett"))
        {
            url = "https://support.hp.com/us-en/drivers/laptops";
        }
        else if (make.Contains("asus"))
        {
            url = "https://www.asus.com/support/Download-Center/";
        }
        else if (make.Contains("acer"))
        {
            url = "https://www.acer.com/us-en/support/drivers-and-manuals";
        }

        try
        {
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            Log($"[OEM PORTAL] Opened browser to: {url}");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not open browser: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LaunchOrDownloadOemDriverTool()
    {
        string make = _detectedOemMake.ToLowerInvariant();

        // 1. Dell Command Update
        if (make.Contains("dell"))
        {
            string[] dellPaths =
            {
                @"C:\Program Files\Dell\CommandUpdate\dcu-cli.exe",
                @"C:\Program Files (x86)\Dell\CommandUpdate\dcu-cli.exe",
                @"C:\Program Files\Dell\CommandUpdate\DellCommandUpdate.exe"
            };
            string? found = dellPaths.FirstOrDefault(File.Exists);
            if (found != null)
            {
                Process.Start(new ProcessStartInfo { FileName = found, UseShellExecute = true });
                Log($"[OEM TOOL] Launched Dell Command | Update: {found}");
                return;
            }

            var res = MessageBox.Show(
                "Dell Command | Update is not installed on this system.\n\nWould you like to open the official Dell download page?",
                "Dell Command | Update", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res == MessageBoxResult.Yes)
            {
                Process.Start(new ProcessStartInfo { FileName = "https://www.dell.com/support/kbdoc/en-us/000177325/dell-command-update", UseShellExecute = true });
            }
            return;
        }

        // 2. Lenovo System Update / Commercial Vantage
        if (make.Contains("lenovo"))
        {
            string lenovoPath = @"C:\Program Files (x86)\Lenovo\System Update\tvsu.exe";
            if (File.Exists(lenovoPath))
            {
                Process.Start(new ProcessStartInfo { FileName = lenovoPath, UseShellExecute = true });
                Log($"[OEM TOOL] Launched Lenovo System Update: {lenovoPath}");
                return;
            }

            var res = MessageBox.Show(
                "Lenovo System Update is not installed on this system.\n\nWould you like to open the official Lenovo download page?",
                "Lenovo System Update", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res == MessageBoxResult.Yes)
            {
                Process.Start(new ProcessStartInfo { FileName = "https://support.lenovo.com/us/en/downloads/ds012808", UseShellExecute = true });
            }
            return;
        }

        // 3. HP Support Assistant
        if (make.Contains("hp") || make.Contains("hewlett"))
        {
            string hpPath = @"C:\Program Files (x86)\Hewlett-Packard\HP Support Framework\HPSA\HPSF.exe";
            if (File.Exists(hpPath))
            {
                Process.Start(new ProcessStartInfo { FileName = hpPath, UseShellExecute = true });
                Log($"[OEM TOOL] Launched HP Support Assistant: {hpPath}");
                return;
            }

            var res = MessageBox.Show(
                "HP Support Assistant is not installed on this system.\n\nWould you like to open the official HP download page?",
                "HP Support Assistant", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res == MessageBoxResult.Yes)
            {
                Process.Start(new ProcessStartInfo { FileName = "https://www.hp.com/go/hpsupportassistant", UseShellExecute = true });
            }
            return;
        }

        // Generic / Intel Driver Assistant
        var intelPrompt = MessageBox.Show(
            "Would you like to open the Intel Driver & Support Assistant (DSA) which automatically detects and updates Intel Wi-Fi, Bluetooth, GPU, and Chipset drivers?",
            "Intel Driver & Support Assistant", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (intelPrompt == MessageBoxResult.Yes)
        {
            Process.Start(new ProcessStartInfo { FileName = "https://www.intel.com/content/www/us/en/support/detect.html", UseShellExecute = true });
        }
    }

    private async Task TriggerPnpRescanAsync()
    {
        Log("[PNP RESCAN] Triggering device bus re-enumeration (pnputil /scan-devices)...");
        await ExecuteAsync("pnputil.exe", "/scan-devices");
        Log("[PNP RESCAN] Kernel re-enumeration complete.");
        await ScanMissingDevicesAndWhqlDriversAsync(false);
    }

    private async Task ScanMissingDevicesAndWhqlDriversAsync(bool searchWhql)
    {
        _progDriverScan.Visibility = Visibility.Visible;
        _progDriverScan.IsIndeterminate = true;
        _lblDriverScanStatus.Text = "Scanning system hardware for missing drivers (Yellow Bangs)...";

        _missingDevices.Clear();

        // 1. Scan Missing & Problem Devices (WMI Win32_PnPEntity)
        await Task.Run(() =>
        {
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    "SELECT Name, DeviceID, ClassGuid, Manufacturer, ConfigManagerErrorCode, Status FROM Win32_PnPEntity WHERE ConfigManagerErrorCode > 0");

                foreach (var obj in searcher.Get())
                {
                    int errCode = 0;
                    if (obj["ConfigManagerErrorCode"] != null)
                        int.TryParse(obj["ConfigManagerErrorCode"].ToString(), out errCode);

                    string name = obj["Name"]?.ToString() ?? "Unknown Device";
                    string devId = obj["DeviceID"]?.ToString() ?? "";
                    string mfg = obj["Manufacturer"]?.ToString() ?? "Unknown";

                    string desc = GetPnpErrorDescription(errCode);

                    _missingDevices.Add(new MissingHardwareDeviceItem
                    {
                        DeviceName = name,
                        DeviceId = devId,
                        HardwareId = devId,
                        ClassName = GetClassFromGuid(obj["ClassGuid"]?.ToString() ?? ""),
                        Manufacturer = mfg,
                        StatusCode = errCode,
                        ProblemDescription = $"Code {errCode}: {desc}",
                        Status = "Problem Device"
                    });
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => Log($"[HARDWARE SCAN ERROR] {ex.Message}"));
            }
        });

        _listMissingDevices.ItemsSource = null;
        _listMissingDevices.ItemsSource = _missingDevices;

        if (_missingDevices.Count > 0)
        {
            _lblMissingDevicesHeader.Text = $"1. MISSING OR UNCONFIGURED HARDWARE DEVICES ({_missingDevices.Count} YELLOW EXCLAMATION MARKS DETECTED):";
            _lblMissingDevicesHeader.Foreground = SvllRed;
            Log($"[HARDWARE SCAN] Found {_missingDevices.Count} problem/missing device(s).");
        }
        else
        {
            _lblMissingDevicesHeader.Text = "1. MISSING OR UNCONFIGURED HARDWARE DEVICES (✅ 0 PROBLEM DEVICES DETECTED - ALL HARDWARE DRIVERS OK):";
            _lblMissingDevicesHeader.Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129));
            Log("[HARDWARE SCAN] All system hardware devices are operating normally.");
        }

        // 2. Query Windows Update WHQL Driver Catalog if requested
        if (searchWhql)
        {
            _lblDriverScanStatus.Text = "Querying Microsoft Windows Update WHQL Driver Catalog for matching drivers...";
            _whqlDrivers.Clear();

            await Task.Run(() =>
            {
                try
                {
                    // PowerShell worker script for COM API search
                    string psCmd =
                        "$ErrorActionPreference = 'SilentlyContinue'; " +
                        "$Session = New-Object -ComObject Microsoft.Update.Session; " +
                        "$Searcher = $Session.CreateUpdateSearcher(); " +
                        "$Searcher.ServerSelection = 2; " +
                        "$Result = $Searcher.Search(\"IsInstalled=0 and Type='Driver'\"); " +
                        "if ($Result.Updates.Count -gt 0) { " +
                        "  foreach ($u in $Result.Updates) { " +
                        "    Write-Host \"[WHQL_DRIVER]|$($u.Title)|$($u.DriverClass)|$($u.DriverModel)|$($u.DriverProvider)|$($u.LastDeploymentChangeTime)\"; " +
                        "  } " +
                        "} else { " +
                        "  Write-Host '[WHQL_NONE]'; " +
                        "}";

                    using var p = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "powershell.exe",
                            Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{psCmd}\"",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            CreateNoWindow = true
                        }
                    };

                    p.Start();
                    string output = p.StandardOutput.ReadToEnd();
                    p.WaitForExit();

                    using var reader = new StringReader(output);
                    string? line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        line = line.Trim();
                        if (line.StartsWith("[WHQL_DRIVER]|"))
                        {
                            string[] parts = line.Split('|');
                            if (parts.Length >= 6)
                            {
                                _whqlDrivers.Add(new WhqlDriverPackageItem
                                {
                                    IsSelected = true,
                                    Title = parts[1],
                                    DriverClass = parts[2],
                                    DriverModel = parts[3],
                                    DriverProvider = parts[4],
                                    DriverDate = parts[5],
                                    Status = "Available from Microsoft WHQL",
                                    StatusColor = "#16A34A"
                                });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() => Log($"[WHQL DRIVER SEARCH ERROR] {ex.Message}"));
                }
            });

            _listWhqlDrivers.ItemsSource = null;
            _listWhqlDrivers.ItemsSource = _whqlDrivers;

            _lblDriverScanStatus.Text = _whqlDrivers.Count > 0
                ? $"Found {_whqlDrivers.Count} certified WHQL driver package(s) ready for download."
                : "No pending uninstalled drivers found on Microsoft WHQL Catalog (System is up to date).";

            Log($"[WHQL DRIVER SEARCH] Search completed. Found {_whqlDrivers.Count} matching driver package(s).");
        }
        else
        {
            _lblDriverScanStatus.Text = "Hardware device scan complete. Click 'Search Windows Update WHQL Drivers' to check cloud catalog.";
        }

        _progDriverScan.IsIndeterminate = false;
        _progDriverScan.Visibility = Visibility.Collapsed;
    }

    private static string GetPnpErrorDescription(int code)
    {
        return code switch
        {
            1 => "Device is not configured correctly",
            10 => "Device cannot start (Hardware or driver issue)",
            14 => "Device requires computer reboot",
            18 => "Reinstall drivers for this device",
            28 => "Drivers for this device are NOT INSTALLED (Yellow Bang)",
            31 => "Windows cannot load driver (Driver missing/corrupt)",
            39 => "Driver corrupted or missing from system store",
            43 => "Windows has stopped this device (Reported problem)",
            _ => "Hardware driver configuration error"
        };
    }

    private static string GetClassFromGuid(string guid)
    {
        guid = guid.Trim('{', '}').ToLowerInvariant();
        return guid switch
        {
            "4d36e972-e325-11ce-bfc1-08002be10318" => "Network Adapter",
            "4d36e968-e325-11ce-bfc1-08002be10318" => "Display / GPU",
            "4d36e96c-e325-11ce-bfc1-08002be10318" => "Audio / Sound",
            "4d36e97d-e325-11ce-bfc1-08002be10318" => "System / Chipset",
            "36fc9e60-c465-11cf-8056-444553540000" => "USB Controller",
            "e0cbf06c-cdb3-4647-bb8a-263b43f0f974" => "Bluetooth",
            "4d36e967-e325-11ce-bfc1-08002be10318" => "Storage / Disk",
            _ => "System Device"
        };
    }

    private async Task ExecuteWhqlDriverInstallAsync()
    {
        var selected = _whqlDrivers.Where(d => d.IsSelected).ToList();
        if (selected.Count == 0)
        {
            MessageBox.Show("Please search for WHQL drivers and select at least one driver package to install.", "No Drivers Selected", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var confirm = MessageBox.Show(
            $"Download and install {selected.Count} WHQL-certified driver package(s) via Microsoft Windows Update Agent?",
            "Confirm Driver Installation", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes) return;

        _btnWhqlInstall.IsEnabled = false;
        _progDriverScan.Visibility = Visibility.Visible;
        _progDriverScan.IsIndeterminate = true;
        _lblDriverScanStatus.Text = "Downloading and installing certified drivers from Microsoft WHQL Catalog...";

        Log($"\n[WHQL DRIVER DEPLOYMENT] Initiating download & installation for {selected.Count} driver packages...");

        await Task.Run(async () =>
        {
            string psScript =
                "$ErrorActionPreference = 'SilentlyContinue'; " +
                "$Session = New-Object -ComObject Microsoft.Update.Session; " +
                "$Searcher = $Session.CreateUpdateSearcher(); " +
                "$Searcher.ServerSelection = 2; " +
                "$Result = $Searcher.Search(\"IsInstalled=0 and Type='Driver'\"); " +
                "if ($Result.Updates.Count -gt 0) { " +
                "  $UpdatesToDownload = New-Object -ComObject Microsoft.Update.UpdateColl; " +
                "  foreach ($u in $Result.Updates) { $UpdatesToDownload.Add($u) | Out-Null }; " +
                "  $Downloader = $Session.CreateUpdateDownloader(); " +
                "  $Downloader.Updates = $UpdatesToDownload; " +
                "  Write-Host 'Downloading WHQL driver payload...'; " +
                "  $DownResult = $Downloader.Download(); " +
                "  $UpdatesToInstall = New-Object -ComObject Microsoft.Update.UpdateColl; " +
                "  foreach ($u in $Result.Updates) { if ($u.IsDownloaded) { $UpdatesToInstall.Add($u) | Out-Null } }; " +
                "  $Installer = $Session.CreateUpdateInstaller(); " +
                "  $Installer.Updates = $UpdatesToInstall; " +
                "  Write-Host 'Installing drivers into Windows Driver Store...'; " +
                "  $InstResult = $Installer.Install(); " +
                "  Write-Host \"Installation Result Code: $($InstResult.ResultCode)\"; " +
                "} else { " +
                "  Write-Host 'No pending WHQL drivers to install.'; " +
                "}";

            await ExecuteAsync("powershell.exe", $"-NoProfile -ExecutionPolicy Bypass -Command \"{psScript}\"");
        });

        // Trigger PnP bus rescan to bind installed drivers
        await ExecuteAsync("pnputil.exe", "/scan-devices");

        _progDriverScan.IsIndeterminate = false;
        _progDriverScan.Visibility = Visibility.Collapsed;
        _btnWhqlInstall.IsEnabled = true;

        MessageBox.Show("WHQL Driver installation completed. Re-checking hardware devices...", "Driver Installation Finished", MessageBoxButton.OK, MessageBoxImage.Information);
        await ScanMissingDevicesAndWhqlDriversAsync(true);
    }

    #endregion

    #region Tab 3: Driver Backup / Export Implementation

    private async Task ExecuteDriverBackupAsync()
    {
        string targetDir = _txtExportTargetDir.Text.Trim();
        if (string.IsNullOrEmpty(targetDir))
        {
            MessageBox.Show("Please choose a valid destination folder for driver backup.", "Export Drivers", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var confirm = MessageBox.Show(
            $"Export all installed third-party drivers into:\n{targetDir}?\n\n" +
            "This will create organized .INF packages that can be used on other machines.",
            "Confirm Driver Export", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes) return;

        try
        {
            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to create destination folder: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        _btnExportDrivers.IsEnabled = false;
        _progDriverExport.Visibility = Visibility.Visible;
        _progDriverExport.IsIndeterminate = true;
        _lblDriverExportStatus.Text = $"Exporting drivers to '{targetDir}' using pnputil /export-driver...";

        Log($"\n[DRIVER BACKUP] Exporting third-party drivers to: {targetDir}");

        await Task.Run(async () =>
        {
            await ExecuteAsync("pnputil.exe", $"/export-driver * \"{targetDir}\"");
        });

        _progDriverExport.IsIndeterminate = false;
        _progDriverExport.Visibility = Visibility.Collapsed;
        _btnExportDrivers.IsEnabled = true;

        int exportedInfCount = 0;
        try
        {
            exportedInfCount = Directory.GetFiles(targetDir, "*.inf", SearchOption.AllDirectories).Length;
        }
        catch { }

        string doneMsg = $"Driver export completed successfully!\n\nExported {exportedInfCount} .INF driver package(s) into:\n{targetDir}";
        _lblDriverExportStatus.Text = $"Export complete: {exportedInfCount} driver package(s) saved.";
        Log($"[DRIVER BACKUP] Finished. {exportedInfCount} driver packages exported to {targetDir}");

        MessageBox.Show(doneMsg, "Driver Backup Completed", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    #endregion

    private static string SanitizeFileName(string name)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(c, '_');
        }
        return name.Replace(' ', '_');
    }
}

public class StringToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        if (value is string hex && !string.IsNullOrEmpty(hex))
        {
            try
            {
                return (SolidColorBrush)new BrushConverter().ConvertFrom(hex)!;
            }
            catch
            {
                return Brushes.SlateGray;
            }
        }
        return Brushes.SlateGray;
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
