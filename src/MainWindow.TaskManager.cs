using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace SVLL_IT_Workstation;

public partial class MainWindow
{
    // =========================================================================
    // INTERACTIVE TASK MANAGER & PROCESS CONTROLLER
    // =========================================================================

    public class DetailedProcessItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Title { get; set; } = "";
        public double MemoryMb { get; set; }
        public double MemoryPercent { get; set; }
        public string MemoryFormatted => $"{MemoryMb:F1} MB";
        public int ThreadCount { get; set; }
        public string Status { get; set; } = "🟢 Running";
        public string Priority { get; set; } = "Normal";
        public string? ExecutablePath { get; set; }
        public bool HasWindow { get; set; }
    }

    private ListView? _listTaskMgrProcesses;
    private readonly List<DetailedProcessItem> _allProcessesCache = new();
    private TextBox? _txtTaskMgrSearch;
    private string _activeTaskMgrFilter = "ALL";
    private DispatcherTimer? _taskMgrRefreshTimer;
    private bool _isTaskMgrRefreshPaused = false;
    private Button? _btnTaskMgrPause;
    private Button? _btnTaskMgrKill;
    private TextBlock? _lblTaskMgrTotalProc;
    private TextBlock? _lblTaskMgrTotalThreads;
    private TextBlock? _lblTaskMgrTotalRam;
    private TextBlock? _lblTaskMgrTopConsumer;

    public UIElement BuildTaskManagerView()
    {
        var mainGrid = new Grid { Margin = new Thickness(0, 0, 10, 0) };
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header Card
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Filter & Action Bar
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Process Grid
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Status & Summary Bar

        // ------------------ 1. Executive Header & Live KPI Cards ------------------
        var headerCard = new Border
        {
            Background = BgCard,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(16, 12, 16, 12),
            Margin = new Thickness(0, 0, 0, 10)
        };

        var headerStack = new StackPanel();

        var topRow = new DockPanel { Margin = new Thickness(0, 0, 0, 10) };
        var titleStack = new StackPanel();
        var mainTitle = new StackPanel { Orientation = Orientation.Horizontal };
        mainTitle.Children.Add(new TextBlock
        {
            Text = "⚡ INTERACTIVE TASK MANAGER & PROCESS CONTROLLER",
            FontSize = 14,
            FontWeight = FontWeights.Bold,
            Foreground = SvllBlue,
            VerticalAlignment = VerticalAlignment.Center
        });

        var livePill = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(220, 252, 231)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(134, 239, 172)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(6, 2, 6, 2),
            Margin = new Thickness(10, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock
            {
                Text = "🟢 LIVE TELEMETRY",
                FontSize = 9.5,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(22, 101, 52))
            }
        };
        mainTitle.Children.Add(livePill);
        titleStack.Children.Add(mainTitle);

        titleStack.Children.Add(new TextBlock
        {
            Text = "Real-time process scheduler, memory consumption breakdown, thread diagnostics & persistent 1-click task termination",
            FontSize = 10.5,
            Foreground = TextSubtle,
            Margin = new Thickness(0, 2, 0, 0)
        });
        topRow.Children.Add(titleStack);

        var topActions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };

        var btnTurbo = new Button
        {
            Content = "⚡ 1-Click Turbo Boost",
            FontSize = 10.5,
            FontWeight = FontWeights.Bold,
            Padding = new Thickness(12, 5, 12, 5),
            Background = SvllBlue,
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand,
            Margin = new Thickness(0, 0, 6, 0)
        };
        btnTurbo.Click += async (s, e) =>
        {
            await ExecuteTurboBoostAsync();
            RefreshTaskManagerProcesses();
        };
        topActions.Children.Add(btnTurbo);

        DockPanel.SetDock(topActions, Dock.Right);
        topRow.Children.Add(topActions);
        headerStack.Children.Add(topRow);

        // KPI Summary Cards Grid
        var kpiGrid = new Grid();
        kpiGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        kpiGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        kpiGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        kpiGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        Border CreateKpiCard(string icon, string label, out TextBlock valBlock, Color accentColor)
        {
            var b = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(50, accentColor.R, accentColor.G, accentColor.B)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10, 8, 10, 8),
                Margin = new Thickness(3)
            };
            var sp = new StackPanel();
            var hRow = new StackPanel { Orientation = Orientation.Horizontal };
            hRow.Children.Add(new TextBlock { Text = icon, FontSize = 12, Margin = new Thickness(0, 0, 6, 0) });
            hRow.Children.Add(new TextBlock { Text = label, FontSize = 9.5, FontWeight = FontWeights.Bold, Foreground = TextSubtle });
            sp.Children.Add(hRow);

            valBlock = new TextBlock
            {
                Text = "Calculating...",
                FontSize = 13.5,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(accentColor),
                Margin = new Thickness(0, 3, 0, 0)
            };
            sp.Children.Add(valBlock);
            b.Child = sp;
            return b;
        }

        var kpi1 = CreateKpiCard("🔢", "ACTIVE PROCESSES", out _lblTaskMgrTotalProc, Color.FromRgb(37, 99, 235));
        Grid.SetColumn(kpi1, 0);
        kpiGrid.Children.Add(kpi1);

        var kpi2 = CreateKpiCard("🧵", "TOTAL THREADS", out _lblTaskMgrTotalThreads, Color.FromRgb(124, 58, 237));
        Grid.SetColumn(kpi2, 1);
        kpiGrid.Children.Add(kpi2);

        var kpi3 = CreateKpiCard("🧠", "PHYSICAL RAM LOAD", out _lblTaskMgrTotalRam, Color.FromRgb(16, 185, 129));
        Grid.SetColumn(kpi3, 2);
        kpiGrid.Children.Add(kpi3);

        var kpi4 = CreateKpiCard("⚡", "TOP MEMORY TASK", out _lblTaskMgrTopConsumer, Color.FromRgb(225, 29, 72));
        Grid.SetColumn(kpi4, 3);
        kpiGrid.Children.Add(kpi4);

        headerStack.Children.Add(kpiGrid);
        headerCard.Child = headerStack;
        Grid.SetRow(headerCard, 0);
        mainGrid.Children.Add(headerCard);

        // ------------------ 2. Filter & Action Control Strip ------------------
        var controlBorder = new Border
        {
            Background = BgCard,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(10, 8, 10, 8),
            Margin = new Thickness(0, 0, 0, 8)
        };

        var ctrlDock = new DockPanel();

        // Right side: Close Task & Refresh Actions
        var rightActions = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

        _btnTaskMgrKill = new Button
        {
            Content = "❌ Close Task",
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            Padding = new Thickness(14, 6, 14, 6),
            Background = new SolidColorBrush(Color.FromRgb(239, 68, 68)),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand,
            Margin = new Thickness(0, 0, 6, 0),
            IsEnabled = false
        };
        _btnTaskMgrKill.Click += (s, e) => TerminateSelectedTaskMgrProcess();
        rightActions.Children.Add(_btnTaskMgrKill);

        var btnKillTree = new Button
        {
            Content = "⚠️ End Process Tree",
            ToolTip = "Terminate process along with all child tasks it spawned",
            FontSize = 10.5,
            FontWeight = FontWeights.SemiBold,
            Padding = new Thickness(10, 6, 10, 6),
            Background = new SolidColorBrush(Color.FromRgb(254, 242, 242)),
            Foreground = new SolidColorBrush(Color.FromRgb(185, 28, 28)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(252, 165, 165)),
            Cursor = Cursors.Hand,
            Margin = new Thickness(0, 0, 8, 0)
        };
        btnKillTree.Click += (s, e) => TerminateSelectedTaskMgrProcess(killTree: true);
        rightActions.Children.Add(btnKillTree);

        _btnTaskMgrPause = new Button
        {
            Content = "⏸️ Pause",
            ToolTip = "Freeze process list updates while inspecting tasks",
            FontSize = 10.5,
            Padding = new Thickness(10, 6, 10, 6),
            Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
            BorderBrush = BorderMuted,
            Cursor = Cursors.Hand,
            Margin = new Thickness(0, 0, 4, 0)
        };
        _btnTaskMgrPause.Click += (s, e) =>
        {
            _isTaskMgrRefreshPaused = !_isTaskMgrRefreshPaused;
            _btnTaskMgrPause.Content = _isTaskMgrRefreshPaused ? "▶️ Resume (2s)" : "⏸️ Pause";
            _btnTaskMgrPause.Background = _isTaskMgrRefreshPaused ? new SolidColorBrush(Color.FromRgb(254, 243, 199)) : new SolidColorBrush(Color.FromRgb(241, 245, 249));
        };
        rightActions.Children.Add(_btnTaskMgrPause);

        var btnManualRefresh = new Button
        {
            Content = "🔄",
            ToolTip = "Refresh process list now",
            FontSize = 11,
            Padding = new Thickness(8, 6, 8, 6),
            Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
            BorderBrush = BorderMuted,
            Cursor = Cursors.Hand
        };
        btnManualRefresh.Click += (s, e) => RefreshTaskManagerProcesses();
        rightActions.Children.Add(btnManualRefresh);

        DockPanel.SetDock(rightActions, Dock.Right);
        ctrlDock.Children.Add(rightActions);

        // Left side: Search & Category Filter Pills
        var leftFilters = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

        leftFilters.Children.Add(new TextBlock
        {
            Text = "🔍",
            FontSize = 11,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 6, 0)
        });

        _txtTaskMgrSearch = new TextBox
        {
            Width = 180,
            Height = 28,
            FontSize = 11,
            Padding = new Thickness(6, 4, 6, 4),
            BorderBrush = BorderMuted,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        _txtTaskMgrSearch.TextChanged += (s, e) => ApplyTaskMgrFilter();
        leftFilters.Children.Add(_txtTaskMgrSearch);

        var filterPills = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(10, 0, 0, 0) };
        string[] cats = { "ALL", "HIGH MEM (>100MB)", "APPS WITH WINDOWS", "BACKGROUND SERVICES" };
        foreach (var c in cats)
        {
            var pBtn = new Button
            {
                Content = c,
                FontSize = 9.5,
                FontWeight = FontWeights.SemiBold,
                Padding = new Thickness(8, 4, 8, 4),
                Margin = new Thickness(2, 0, 2, 0),
                Background = c == "ALL" ? new SolidColorBrush(Color.FromRgb(219, 234, 254)) : new SolidColorBrush(Color.FromRgb(248, 250, 252)),
                Foreground = c == "ALL" ? SvllBlue : TextDark,
                BorderBrush = BorderMuted,
                Cursor = Cursors.Hand
            };
            pBtn.Click += (s, e) =>
            {
                _activeTaskMgrFilter = c;
                foreach (UIElement child in filterPills.Children)
                {
                    if (child is Button b)
                    {
                        bool isSel = (string)b.Content == c;
                        b.Background = isSel ? new SolidColorBrush(Color.FromRgb(219, 234, 254)) : new SolidColorBrush(Color.FromRgb(248, 250, 252));
                        b.Foreground = isSel ? SvllBlue : TextDark;
                    }
                }
                ApplyTaskMgrFilter();
            };
            filterPills.Children.Add(pBtn);
        }
        leftFilters.Children.Add(filterPills);

        ctrlDock.Children.Add(leftFilters);
        controlBorder.Child = ctrlDock;
        Grid.SetRow(controlBorder, 1);
        mainGrid.Children.Add(controlBorder);

        // ------------------ 3. Detailed Process Table ------------------
        _listTaskMgrProcesses = new ListView
        {
            FontSize = 11,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            Background = BgCard
        };

        var gv = new GridView();

        // PID
        gv.Columns.Add(new GridViewColumn
        {
            Header = "PID",
            Width = 65,
            DisplayMemberBinding = new Binding("Id")
        });

        // Process Name
        gv.Columns.Add(new GridViewColumn
        {
            Header = "Process Name",
            Width = 180,
            DisplayMemberBinding = new Binding("Name")
        });

        // Window Title
        gv.Columns.Add(new GridViewColumn
        {
            Header = "Window Title / Description",
            Width = 240,
            DisplayMemberBinding = new Binding("Title")
        });

        // Memory Usage
        gv.Columns.Add(new GridViewColumn
        {
            Header = "RAM (MB)",
            Width = 90,
            DisplayMemberBinding = new Binding("MemoryFormatted")
        });

        // RAM Bar
        var barCol = new GridViewColumn { Header = "% of RAM", Width = 110 };
        var barFactory = new FrameworkElementFactory(typeof(ProgressBar));
        barFactory.SetValue(ProgressBar.MinimumProperty, 0.0);
        barFactory.SetValue(ProgressBar.MaximumProperty, 20.0);
        barFactory.SetValue(ProgressBar.HeightProperty, 7.0);
        barFactory.SetValue(ProgressBar.ForegroundProperty, SvllLightBlue);
        barFactory.SetBinding(ProgressBar.ValueProperty, new Binding("MemoryPercent"));
        barCol.CellTemplate = new DataTemplate { VisualTree = barFactory };
        gv.Columns.Add(barCol);

        // Threads
        gv.Columns.Add(new GridViewColumn
        {
            Header = "Threads",
            Width = 65,
            DisplayMemberBinding = new Binding("ThreadCount")
        });

        // Status
        gv.Columns.Add(new GridViewColumn
        {
            Header = "Status",
            Width = 95,
            DisplayMemberBinding = new Binding("Status")
        });

        // Priority
        gv.Columns.Add(new GridViewColumn
        {
            Header = "Priority",
            Width = 75,
            DisplayMemberBinding = new Binding("Priority")
        });

        _listTaskMgrProcesses.View = gv;

        // Context Menu for right click
        var rowMenu = new ContextMenu();
        var menuKill = new MenuItem { Header = "❌  Close Task (End Process)", FontWeight = FontWeights.Bold };
        menuKill.Click += (s, e) => TerminateSelectedTaskMgrProcess();
        rowMenu.Items.Add(menuKill);

        var menuTree = new MenuItem { Header = "⚠️  End Process Tree" };
        menuTree.Click += (s, e) => TerminateSelectedTaskMgrProcess(killTree: true);
        rowMenu.Items.Add(menuTree);

        rowMenu.Items.Add(new Separator());

        var menuCopy = new MenuItem { Header = "📋  Copy Process Name & PID" };
        menuCopy.Click += (s, e) =>
        {
            if (_listTaskMgrProcesses.SelectedItem is DetailedProcessItem item)
            {
                Clipboard.SetText($"{item.Name} (PID: {item.Id})");
            }
        };
        rowMenu.Items.Add(menuCopy);

        var menuOpenPath = new MenuItem { Header = "📁  Open File Location" };
        menuOpenPath.Click += (s, e) =>
        {
            if (_listTaskMgrProcesses.SelectedItem is DetailedProcessItem item && !string.IsNullOrEmpty(item.ExecutablePath) && File.Exists(item.ExecutablePath))
            {
                Process.Start("explorer.exe", $"/select,\"{item.ExecutablePath}\"");
            }
            else
            {
                MessageBox.Show("Executable path could not be accessed (may require system privilege).", "Path Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        };
        rowMenu.Items.Add(menuOpenPath);

        _listTaskMgrProcesses.ContextMenu = rowMenu;

        _listTaskMgrProcesses.SelectionChanged += (s, e) =>
        {
            if (_btnTaskMgrKill != null)
                _btnTaskMgrKill.IsEnabled = _listTaskMgrProcesses.SelectedItem != null;
        };

        _listTaskMgrProcesses.MouseDoubleClick += (s, e) =>
        {
            if (_listTaskMgrProcesses.SelectedItem != null)
            {
                TerminateSelectedTaskMgrProcess();
            }
        };

        Grid.SetRow(_listTaskMgrProcesses, 2);
        mainGrid.Children.Add(_listTaskMgrProcesses);

        // ------------------ 4. Status Bar ------------------
        var bottomStatus = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(10, 6, 10, 6),
            Margin = new Thickness(0, 8, 0, 0)
        };
        var bottomDock = new DockPanel();
        bottomDock.Children.Add(new TextBlock
        {
            Text = "💡 Tip: Select any process and click 'Close Task' to terminate. Right-click for options. Double-click to close immediately.",
            FontSize = 10.5,
            Foreground = TextSubtle
        });
        bottomStatus.Child = bottomDock;
        Grid.SetRow(bottomStatus, 3);
        mainGrid.Children.Add(bottomStatus);

        // Initialize timer for 2.5-second live refresh
        if (_taskMgrRefreshTimer == null)
        {
            _taskMgrRefreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2.5) };
            _taskMgrRefreshTimer.Tick += (s, e) =>
            {
                if (!_isTaskMgrRefreshPaused)
                {
                    RefreshTaskManagerProcesses();
                }
            };
            _taskMgrRefreshTimer.Start();
        }

        // Initial Load
        Dispatcher.BeginInvoke(new Action(RefreshTaskManagerProcesses));

        return mainGrid;
    }

    private void RefreshTaskManagerProcesses()
    {
        if (_listTaskMgrProcesses == null) return;

        try
        {
            // IMPORTANT: Remember selected PID so auto-refresh NEVER deselects!
            int? selectedPid = (_listTaskMgrProcesses.SelectedItem as DetailedProcessItem)?.Id;

            // Compute total RAM
            double totalRamGb = 8.0;
            try
            {
                var mem = new MEMORYSTATUSEX();
                GlobalMemoryStatusEx(mem);
                totalRamGb = mem.ullTotalPhys / (1024.0 * 1024.0 * 1024.0);
            }
            catch { }

            var rawProcs = Process.GetProcesses();
            var items = new List<DetailedProcessItem>(rawProcs.Length);
            int totalThreads = 0;
            double topMemMb = 0;
            string topConsumer = "None";

            foreach (var p in rawProcs)
            {
                try
                {
                    double mb = 0;
                    try { mb = p.WorkingSet64 / (1024.0 * 1024.0); } catch { }

                    string title = "";
                    try { title = p.MainWindowTitle; } catch { }

                    int threads = 0;
                    try { threads = p.Threads.Count; } catch { }
                    totalThreads += threads;

                    string priority = "Normal";
                    try { priority = p.PriorityClass.ToString(); } catch { }

                    bool responding = true;
                    try { responding = p.Responding; } catch { }

                    string? exePath = null;
                    try { exePath = p.MainModule?.FileName; } catch { }

                    if (mb > topMemMb)
                    {
                        topMemMb = mb;
                        topConsumer = $"{p.ProcessName} ({mb:F0} MB)";
                    }

                    items.Add(new DetailedProcessItem
                    {
                        Id = p.Id,
                        Name = p.ProcessName,
                        Title = string.IsNullOrWhiteSpace(title) ? (exePath != null ? Path.GetFileName(exePath) : "") : title,
                        MemoryMb = mb,
                        MemoryPercent = totalRamGb > 0 ? (mb / (totalRamGb * 1024.0)) * 100.0 : 0,
                        ThreadCount = threads,
                        Status = responding ? "🟢 Running" : "⚠️ Not Responding",
                        Priority = priority,
                        ExecutablePath = exePath,
                        HasWindow = !string.IsNullOrWhiteSpace(title)
                    });
                }
                catch { }
            }

            // Update KPI cards
            if (_lblTaskMgrTotalProc != null) _lblTaskMgrTotalProc.Text = $"{items.Count} Running";
            if (_lblTaskMgrTotalThreads != null) _lblTaskMgrTotalThreads.Text = $"{totalThreads:N0} Threads";
            if (_lblTaskMgrTopConsumer != null) _lblTaskMgrTopConsumer.Text = topConsumer;

            try
            {
                var mem = new MEMORYSTATUSEX();
                GlobalMemoryStatusEx(mem);
                double usedGb = (mem.ullTotalPhys - mem.ullAvailPhys) / (1024.0 * 1024.0 * 1024.0);
                if (_lblTaskMgrTotalRam != null) _lblTaskMgrTotalRam.Text = $"{usedGb:F1} / {totalRamGb:F1} GB ({mem.dwMemoryLoad}%)";
            }
            catch { }

            _allProcessesCache.Clear();
            _allProcessesCache.AddRange(items.OrderByDescending(x => x.MemoryMb));

            // Apply filter and restore selection!
            ApplyTaskMgrFilter(selectedPid);
        }
        catch { }
    }

    private void ApplyTaskMgrFilter(int? restoreSelectedPid = null)
    {
        if (_listTaskMgrProcesses == null) return;

        int? selectedPid = restoreSelectedPid ?? (_listTaskMgrProcesses.SelectedItem as DetailedProcessItem)?.Id;

        string query = _txtTaskMgrSearch?.Text.Trim().ToLowerInvariant() ?? "";

        var filtered = _allProcessesCache.Where(p =>
        {
            if (_activeTaskMgrFilter == "HIGH MEM (>100MB)" && p.MemoryMb < 100) return false;
            if (_activeTaskMgrFilter == "APPS WITH WINDOWS" && !p.HasWindow) return false;
            if (_activeTaskMgrFilter == "BACKGROUND SERVICES" && p.HasWindow) return false;

            if (!string.IsNullOrEmpty(query))
            {
                if (!p.Name.ToLowerInvariant().Contains(query) &&
                    !p.Id.ToString().Contains(query) &&
                    !p.Title.ToLowerInvariant().Contains(query))
                {
                    return false;
                }
            }
            return true;
        }).ToList();

        _listTaskMgrProcesses.ItemsSource = filtered;

        // Restore selected item so auto-refresh NEVER deselects!
        if (selectedPid.HasValue)
        {
            var match = filtered.FirstOrDefault(x => x.Id == selectedPid.Value);
            if (match != null)
            {
                _listTaskMgrProcesses.SelectedItem = match;
            }
        }

        if (_btnTaskMgrKill != null)
        {
            _btnTaskMgrKill.IsEnabled = _listTaskMgrProcesses.SelectedItem != null;
        }
    }

    private void TerminateSelectedTaskMgrProcess(bool killTree = false)
    {
        if (_listTaskMgrProcesses?.SelectedItem is DetailedProcessItem item)
        {
            string actionText = killTree ? "terminate process tree for" : "close process";
            var result = MessageBox.Show(
                $"Are you sure you want to {actionText} '{item.Name}' (PID: {item.Id})?\n\n" +
                $"Memory in use: {item.MemoryFormatted}",
                "Confirm Close Task", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    if (killTree)
                    {
                        var psi = new ProcessStartInfo
                        {
                            FileName = "taskkill.exe",
                            Arguments = $"/F /T /PID {item.Id}",
                            CreateNoWindow = true,
                            UseShellExecute = false
                        };
                        using var proc = Process.Start(psi);
                        proc?.WaitForExit(2000);
                        Log($"[TASK MANAGER] Terminated process tree for {item.Name} (PID: {item.Id}).");
                    }
                    else
                    {
                        var proc = Process.GetProcessById(item.Id);
                        proc.Kill();
                        proc.WaitForExit(1500);
                        Log($"[TASK MANAGER] Closed process {item.Name} (PID: {item.Id}).");
                    }

                    // Immediate refresh
                    RefreshTaskManagerProcesses();
                    UpdateLiveVitals();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Could not close process: {ex.Message}", "Error Closing Task", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
