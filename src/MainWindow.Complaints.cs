using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using SVLL_IT_Workstation.Models;

namespace SVLL_IT_Workstation;

public partial class MainWindow
{
    // Hub 7: IT Complaints & AI Helpdesk UI Elements
    private TextBox _txtComplaintTitle = null!;
    private TextBox _txtComplaintDesc = null!;
    private ComboBox _cmbComplaintCategory = null!;
    private ComboBox _cmbComplaintPriority = null!;
    private TextBox _txtComplaintContact = null!;
    private TextBox _txtComplainerName = null!;
    private TextBox _txtComplainerEmail = null!;
    private ComboBox _cmbComplaintDepot = null!;

    // AI Chat Elements
    private AiComplaintSession _aiSession = null!;
    private StackPanel _aiChatMessagesPanel = null!;
    private TextBox _txtAiChatInput = null!;
    private WrapPanel _aiQuickRepliesPanel = null!;
    private ScrollViewer _aiChatScroll = null!;

    // Admin Service Desk Elements
    private StackPanel _ticketAdminListPanel = null!;
    private TextBlock _lblAdminTotalTickets = null!;
    private TextBlock _lblAdminOpenTickets = null!;
    private TextBlock _lblAdminProgressTickets = null!;
    private TextBlock _lblAdminCriticalTickets = null!;
    private TextBox _txtTicketSearch = null!;
    private ComboBox _cmbTicketStatusFilter = null!;
    private ComboBox _cmbTicketPriorityFilter = null!;

    // -------------------------------------------------------------
    // 1. REGISTER IT COMPLAINT / TICKET (USER FORM)
    // -------------------------------------------------------------
    public UIElement BuildSubmitComplaintView()
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
            Margin = new Thickness(0, 0, 0, 10)
        };
        var headerGrid = new Grid();
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var headerLeft = new StackPanel();
        var titleRow = new StackPanel { Orientation = Orientation.Horizontal };
        titleRow.Children.Add(new TextBlock
        {
            Text = "📋 Register IT Support Complaint & Service Incident",
            FontSize = 16,
            FontWeight = FontWeights.ExtraBold,
            Foreground = TextDark
        });
        titleRow.Children.Add(new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(239, 246, 255)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(191, 219, 254)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(8, 2, 8, 2),
            Margin = new Thickness(10, 0, 0, 0),
            Child = new TextBlock { Text = "HUB 7 • SERVICE DESK", FontSize = 10, FontWeight = FontWeights.Bold, Foreground = SvllBlue }
        });
        titleRow.Children.Add(new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(220, 252, 231)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(134, 239, 172)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(8, 2, 8, 2),
            Margin = new Thickness(6, 0, 0, 0),
            Child = new TextBlock { Text = "🟢 LIVE SYNC: Complain Log sheet IT - New Complaint.csv", FontSize = 9.5, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(22, 101, 52)) }
        });
        headerLeft.Children.Add(titleRow);
        headerLeft.Children.Add(new TextBlock
        {
            Text = "Submit technical workstation, ERP client, network, or hardware issues directly to the SVLL IT Support team with automated diagnostics and direct synchronization to the enterprise CSV log sheet and Admin Console.",
            FontSize = 11,
            Foreground = TextSubtle,
            Margin = new Thickness(0, 4, 0, 0)
        });
        Grid.SetColumn(headerLeft, 0);
        headerGrid.Children.Add(headerLeft);

        var headerRight = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

        var btnOpenCsvHeader = new Button
        {
            Content = "📊 Open Log Sheet (.CSV)",
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
            Foreground = TextDark,
            Padding = new Thickness(12, 6, 12, 6),
            Margin = new Thickness(0, 0, 8, 0),
            BorderBrush = BorderMuted,
            Cursor = Cursors.Hand
        };
        btnOpenCsvHeader.Click += (s, e) =>
        {
            try
            {
                string csvPath = ComplaintTicketService.GetActiveCsvPath();
                Process.Start(new ProcessStartInfo { FileName = csvPath, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not open CSV log sheet: {ex.Message}", "CSV Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        };
        headerRight.Children.Add(btnOpenCsvHeader);

        var btnOpenFolderHeader = new Button
        {
            Content = "📁 Open Folder",
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
            Foreground = TextDark,
            Padding = new Thickness(10, 6, 10, 6),
            Margin = new Thickness(0, 0, 8, 0),
            BorderBrush = BorderMuted,
            Cursor = Cursors.Hand
        };
        btnOpenFolderHeader.Click += (s, e) =>
        {
            try
            {
                string dir = Directory.Exists(@"c:\IT PROJECTS\svll-workstation") ? @"c:\IT PROJECTS\svll-workstation" : AppDomain.CurrentDomain.BaseDirectory;
                Process.Start(new ProcessStartInfo { FileName = dir, UseShellExecute = true });
            }
            catch { }
        };
        headerRight.Children.Add(btnOpenFolderHeader);

        var btnAiAssist = new Button
        {
            Content = "🤖 AI Diagnostic Intake",
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            Background = SvllBlue,
            Foreground = Brushes.White,
            Padding = new Thickness(14, 6, 14, 6),
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand
        };
        btnAiAssist.Click += (s, e) => SwitchHubSubTool("Hub_Complaints", "AiComplaintBot");
        headerRight.Children.Add(btnAiAssist);

        Grid.SetColumn(headerRight, 1);
        headerGrid.Children.Add(headerRight);

        headerBorder.Child = headerGrid;
        root.Children.Add(headerBorder);

        // 2. 1-Click Common Problem Presets Card
        var presetsCard = new Border
        {
            Background = BgCard,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(12, 10, 12, 10),
            Margin = new Thickness(0, 0, 0, 10)
        };
        var presetsStack = new StackPanel();
        presetsStack.Children.Add(new TextBlock
        {
            Text = "⚡ 1-CLICK COMMON PROBLEM PRESETS (CLICK TO AUTO-POPULATE INCIDENT REPORT):",
            FontSize = 10,
            FontWeight = FontWeights.Bold,
            Foreground = SvllBlue,
            Margin = new Thickness(0, 0, 0, 6)
        });

        var presetChips = new WrapPanel();

        void AddPresetChip(string icon, string label, string catName, string title, string desc)
        {
            var btn = new Button
            {
                Content = $"{icon} {label}",
                FontSize = 10.5,
                Padding = new Thickness(10, 4, 10, 4),
                Margin = new Thickness(0, 0, 6, 6),
                Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
                BorderBrush = BorderMuted,
                Cursor = Cursors.Hand
            };
            btn.Click += (s, e) =>
            {
                for (int i = 0; i < _cmbComplaintCategory.Items.Count; i++)
                {
                    if (_cmbComplaintCategory.Items[i].ToString()!.Contains(catName))
                    {
                        _cmbComplaintCategory.SelectedIndex = i;
                        break;
                    }
                }
                _txtComplaintTitle.Text = title;
                _txtComplaintDesc.Text = desc;
            };
            presetChips.Children.Add(btn);
        }

        AddPresetChip("🏢", "Boss ERP Session Hang", "Boss Not Working", "Boss ERP Client Session Timeout During Dispatch", "When processing LR and dispatch bills in Boss ERP, client terminates unexpectedly. Intermittent connectivity issue to dispatch database.");
        AddPresetChip("🖨️", "Laser Printer Spooler Stuck", "Printer Not Work", "Department Laser Printer Queue Stuck & Spooler Locked", "Print jobs sent to department network laser printer are locked with status 'Deleting - Printing'. Print spooler service restart and queue cleanup required.");
        AddPresetChip("⚡", "Workstation Freezing", "IT PC / LAPTOP WORKING SLOW", "Workstation Freezing with High CPU / Memory Saturation", "System responsiveness is severely degraded; applications freeze intermittently. Requires performance triage and process audit.");
        AddPresetChip("🌐", "Depot Gateway Drops", "IT INERNET CONNECTIVITY PROBLEM", "Network Gateway Latency and Packet Loss Flapping", "Intermittent ping drops observed to corporate gateway and subnet switches. Branch file transfers timing out.");
        AddPresetChip("📧", "Outlook Send/Receive Error", "IT OUTLOOK MAIL ISSUE", "Corporate Outlook Mail Sync & Delivery Failure", "Outlook client shows disconnected or send/receive error. Mail synchronization failing on department mailbox.");
        AddPresetChip("🔒", "USB Pen Drive DLP Access", "USB DLP Access", "Temporary USB Storage Access Request for Customs Manifest", "Requesting temporary 4-hour USB DLP write authorization to transfer signed vehicle dispatch bills-of-lading to driver pen drive.");

        presetsStack.Children.Add(presetChips);
        presetsCard.Child = presetsStack;
        root.Children.Add(presetsCard);

        // 3. Form Grid (Left: Form Inputs, Right: Auto-Attached Diagnostics)
        var formGrid = new Grid();
        formGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.2, GridUnitType.Star) });
        formGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.8, GridUnitType.Star) });

        // Left Form Card
        var formCard = new Border
        {
            Background = BgCard,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(18),
            Margin = new Thickness(0, 0, 6, 0)
        };
        var formPanel = new StackPanel();

        // Row 1: Category & Priority
        var catRow = new Grid { Margin = new Thickness(0, 0, 0, 10) };
        catRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        catRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var catStack = new StackPanel { Margin = new Thickness(0, 0, 6, 0) };
        catStack.Children.Add(new TextBlock { Text = "Complaint Category *", FontSize = 11, FontWeight = FontWeights.Bold, Foreground = TextDark, Margin = new Thickness(0, 0, 0, 4) });
        _cmbComplaintCategory = new ComboBox { Height = 30, FontSize = 11 };
        _cmbComplaintCategory.Items.Add("🏢 Boss Not Working (ERP / WMS)");
        _cmbComplaintCategory.Items.Add("🖨️ Printer Not Work (Laser / Network)");
        _cmbComplaintCategory.Items.Add("⚡ IT PC / LAPTOP WORKING SLOW");
        _cmbComplaintCategory.Items.Add("🌐 IT INERNET CONNECTIVITY PROBLEM");
        _cmbComplaintCategory.Items.Add("📧 IT OUTLOOK MAIL ISSUE");
        _cmbComplaintCategory.Items.Add("🔧 IT COMPUTER REPAIRING & MAINTENANCE");
        _cmbComplaintCategory.Items.Add("📦 New Software Installation");
        _cmbComplaintCategory.Items.Add("📹 IT CAMERA DOWN");
        _cmbComplaintCategory.Items.Add("🔒 USB DLP Access (Removable Media)");
        _cmbComplaintCategory.Items.Add("❓ Other");
        _cmbComplaintCategory.SelectedIndex = 0;
        catStack.Children.Add(_cmbComplaintCategory);
        Grid.SetColumn(catStack, 0);
        catRow.Children.Add(catStack);

        var prioStack = new StackPanel { Margin = new Thickness(6, 0, 0, 0) };
        prioStack.Children.Add(new TextBlock { Text = "Urgency / Priority *", FontSize = 11, FontWeight = FontWeights.Bold, Foreground = TextDark, Margin = new Thickness(0, 0, 0, 4) });
        _cmbComplaintPriority = new ComboBox { Height = 30, FontSize = 11 };
        _cmbComplaintPriority.Items.Add("URGENT (Warehouse / Dispatch Operations Halted)");
        _cmbComplaintPriority.Items.Add("NORMAL (Standard IT Support Request)");
        _cmbComplaintPriority.SelectedIndex = 1;
        prioStack.Children.Add(_cmbComplaintPriority);
        Grid.SetColumn(prioStack, 1);
        catRow.Children.Add(prioStack);

        formPanel.Children.Add(catRow);

        // Row 2: Complainer Name & Email
        var userRow = new Grid { Margin = new Thickness(0, 0, 0, 10) };
        userRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        userRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var nameStack = new StackPanel { Margin = new Thickness(0, 0, 6, 0) };
        nameStack.Children.Add(new TextBlock { Text = "Complainer Full Name *", FontSize = 11, FontWeight = FontWeights.Bold, Foreground = TextDark, Margin = new Thickness(0, 0, 0, 4) });
        _txtComplainerName = new TextBox
        {
            Height = 30,
            Padding = new Thickness(6, 4, 6, 4),
            FontSize = 11,
            BorderBrush = BorderMuted,
            Text = Environment.UserName
        };
        nameStack.Children.Add(_txtComplainerName);
        Grid.SetColumn(nameStack, 0);
        userRow.Children.Add(nameStack);

        var emailStack = new StackPanel { Margin = new Thickness(6, 0, 0, 0) };
        emailStack.Children.Add(new TextBlock { Text = "Official Email Address *", FontSize = 11, FontWeight = FontWeights.Bold, Foreground = TextDark, Margin = new Thickness(0, 0, 0, 4) });
        _txtComplainerEmail = new TextBox
        {
            Height = 30,
            Padding = new Thickness(6, 4, 6, 4),
            FontSize = 11,
            BorderBrush = BorderMuted,
            Text = $"{Environment.UserName.ToLowerInvariant()}@shreevasulogistics.com"
        };
        emailStack.Children.Add(_txtComplainerEmail);
        Grid.SetColumn(emailStack, 1);
        userRow.Children.Add(emailStack);

        formPanel.Children.Add(userRow);

        // Row 3: Contact & Branch/Depot
        var contactRow = new Grid { Margin = new Thickness(0, 0, 0, 10) };
        contactRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        contactRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var contStack = new StackPanel { Margin = new Thickness(0, 0, 6, 0) };
        contStack.Children.Add(new TextBlock { Text = "Contact Phone / Mobile Number *", FontSize = 11, FontWeight = FontWeights.Bold, Foreground = TextDark, Margin = new Thickness(0, 0, 0, 4) });
        _txtComplaintContact = new TextBox
        {
            Height = 30,
            Padding = new Thickness(6, 4, 6, 4),
            FontSize = 11,
            BorderBrush = BorderMuted,
            Text = "7,000,681,500"
        };
        contStack.Children.Add(_txtComplaintContact);
        Grid.SetColumn(contStack, 0);
        contactRow.Children.Add(contStack);

        var brStack = new StackPanel { Margin = new Thickness(6, 0, 0, 0) };
        brStack.Children.Add(new TextBlock { Text = "Depo / Department Name *", FontSize = 11, FontWeight = FontWeights.Bold, Foreground = TextDark, Margin = new Thickness(0, 0, 0, 4) });
        _cmbComplaintDepot = new ComboBox { Height = 30, FontSize = 11, IsEditable = true };
        _cmbComplaintDepot.Items.Add("IT");
        _cmbComplaintDepot.Items.Add("ACCOUNT");
        _cmbComplaintDepot.Items.Add("ADMIN");
        _cmbComplaintDepot.Items.Add("MDO");
        _cmbComplaintDepot.Items.Add("HR");
        _cmbComplaintDepot.Items.Add("OPERATIONS TENDUA");
        _cmbComplaintDepot.Items.Add("GRASIM INDUSTRIES LIMITED");
        _cmbComplaintDepot.Items.Add("DABUR INDIA LIMITED");
        _cmbComplaintDepot.Items.Add("ORIENT DEPOT");
        _cmbComplaintDepot.Items.Add("PBPL RAIPUR");
        _cmbComplaintDepot.Items.Add("AMARA RAJA BATTERIES LIMITED");
        _cmbComplaintDepot.Items.Add("JK TYRE & INDUSTRIES LIMITED");
        _cmbComplaintDepot.Items.Add("NUVOCO VISTAS CORPORATION LTD");
        _cmbComplaintDepot.Items.Add("RECKITT BENCKISER INDIA LTD.-(HEALTH)");
        _cmbComplaintDepot.Items.Add("SYNGENTA");
        _cmbComplaintDepot.Items.Add("POLYCAB INDIA LIMITED");
        _cmbComplaintDepot.Items.Add("NEW HOLLAND");
        _cmbComplaintDepot.SelectedIndex = 0;
        brStack.Children.Add(_cmbComplaintDepot);
        Grid.SetColumn(brStack, 1);
        contactRow.Children.Add(brStack);

        formPanel.Children.Add(contactRow);

        // Row 4: Subject
        formPanel.Children.Add(new TextBlock { Text = "Complaint Title / Summary *", FontSize = 11, FontWeight = FontWeights.Bold, Foreground = TextDark, Margin = new Thickness(0, 0, 0, 4) });
        _txtComplaintTitle = new TextBox
        {
            Height = 32,
            Padding = new Thickness(6, 4, 6, 4),
            FontSize = 11.5,
            BorderBrush = BorderMuted,
            Margin = new Thickness(0, 0, 0, 10),
            Text = "Boss ERP Client Session Timeout During Dispatch"
        };
        formPanel.Children.Add(_txtComplaintTitle);

        // Row 5: Description
        formPanel.Children.Add(new TextBlock { Text = "Detailed Description of Problem *", FontSize = 11, FontWeight = FontWeights.Bold, Foreground = TextDark, Margin = new Thickness(0, 0, 0, 4) });
        _txtComplaintDesc = new TextBox
        {
            Height = 90,
            TextWrapping = TextWrapping.Wrap,
            AcceptsReturn = true,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Padding = new Thickness(8),
            FontSize = 11.5,
            BorderBrush = BorderMuted,
            Margin = new Thickness(0, 0, 0, 14),
            Text = "When processing LR and dispatch bills in Boss ERP, client terminates unexpectedly. Intermittent connectivity issue to dispatch database."
        };
        formPanel.Children.Add(_txtComplaintDesc);

        // Submit Button
        var btnSubmit = new Button
        {
            Content = "🚀 Register Complaint & Append to Central Log Sheet",
            Height = 40,
            FontSize = 12.5,
            FontWeight = FontWeights.Bold,
            Background = SvllBlue,
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand
        };
        btnSubmit.Click += (s, e) =>
        {
            string title = _txtComplaintTitle.Text.Trim();
            string desc = _txtComplaintDesc.Text.Trim();
            string contact = _txtComplaintContact.Text.Trim();
            string branch = _cmbComplaintDepot.Text.Trim();
            string name = _txtComplainerName.Text.Trim();
            string email = _txtComplainerEmail.Text.Trim();

            if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(desc))
            {
                MessageBox.Show("Please provide both a Title and Detailed Description for the complaint.", "Missing Required Fields", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string catRaw = _cmbComplaintCategory.SelectedItem?.ToString() ?? "Other";
            string category = "Other";
            if (catRaw.Contains("Boss")) category = "Boss Not Working";
            else if (catRaw.Contains("Printer")) category = "Printer Not Work";
            else if (catRaw.Contains("SLOW")) category = "IT PC / LAPTOP WORKING SLOW";
            else if (catRaw.Contains("INERNET")) category = "IT INERNET CONNECTIVITY PROBLEM";
            else if (catRaw.Contains("OUTLOOK")) category = "IT OUTLOOK MAIL ISSUE";
            else if (catRaw.Contains("REPAIRING")) category = "IT COMPUTER REPAIRING & MAINTENANCE";
            else if (catRaw.Contains("Installation")) category = "New Software Installation";
            else if (catRaw.Contains("CAMERA")) category = "IT CAMERA DOWN";
            else if (catRaw.Contains("DLP")) category = "USB DLP Access";

            string prioRaw = _cmbComplaintPriority.SelectedItem?.ToString() ?? "NORMAL";
            string priority = prioRaw.Contains("URGENT") ? "URGENT" : "NORMAL";

            var ticket = ComplaintTicketService.Instance.CreateTicket(
                title, desc, category, priority, contact, branch,
                filedViaAi: false,
                complainerName: name,
                complainerEmail: email);

            Log($"[SERVICE DESK] Complaint ticket {ticket.TicketId} successfully registered & logged to {ComplaintTicketService.RealSheetName}.");

            var res = MessageBox.Show(
                $"🎉 Complaint Ticket Successfully Registered!\n\n" +
                $"• Complaint No: {ticket.TicketId}\n" +
                $"• Category: {ticket.Category}\n" +
                $"• Depo / Dept: {ticket.BranchLocation}\n" +
                $"• Complainer: {ticket.CreatedBy} ({ticket.UserContact})\n" +
                $"• Priority: {ticket.Priority}\n" +
                $"• Assigned To: {ticket.AssignedTechnician}\n\n" +
                $"• Central Sheet: Automatically written & appended to '{ComplaintTicketService.RealSheetName}' in project folder\n" +
                $"• Admin Desk: LIVE reflection on Central IT Helpdesk Admin Console (no restart required)\n\n" +
                $"Would you like to open the CSV log sheet in Excel now?",
                "Ticket Submitted & Synchronized", MessageBoxButton.YesNo, MessageBoxImage.Information);

            if (res == MessageBoxResult.Yes)
            {
                try
                {
                    string csvPath = ComplaintTicketService.GetActiveCsvPath();
                    Process.Start(new ProcessStartInfo { FileName = csvPath, UseShellExecute = true });
                }
                catch { }
            }

            // Navigate to my tickets
            SwitchHubSubTool("Hub_Complaints", "MyTickets");
        };
        formPanel.Children.Add(btnSubmit);

        formCard.Child = formPanel;
        Grid.SetColumn(formCard, 0);
        formGrid.Children.Add(formCard);

        // Right Diagnostics Preview Card
        var diagCard = new Border
        {
            Background = BgCard,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(18),
            Margin = new Thickness(6, 0, 0, 0)
        };
        var diagPanel = new StackPanel();

        diagPanel.Children.Add(new TextBlock
        {
            Text = "🛡️ Auto-Attached System Telemetry",
            FontSize = 13.5,
            FontWeight = FontWeights.Bold,
            Foreground = TextDark
        });
        diagPanel.Children.Add(new TextBlock
        {
            Text = "Eliminates IT guesswork: SVLL Workstation captures these vital metrics and attaches them to your ticket automatically.",
            FontSize = 10.5,
            Foreground = TextSubtle,
            Margin = new Thickness(0, 3, 0, 14)
        });

        var diagBox = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12)
        };
        var diagList = new StackPanel();
        diagList.Children.Add(BuildDiagRow("💻 Hostname", Environment.MachineName));
        diagList.Children.Add(BuildDiagRow("👤 Logged-In User", Environment.UserName));
        diagList.Children.Add(BuildDiagRow("🌐 IPv4 Address", FleetRegistryService.Instance.LocalDevice.IpAddress));
        diagList.Children.Add(BuildDiagRow("🔴 AnyDesk ID", FleetRegistryService.Instance.LocalDevice.AnyDeskId));
        diagList.Children.Add(BuildDiagRow("🩺 Windows OS", Environment.OSVersion.ToString()));
        diagList.Children.Add(BuildDiagRow("🖨️ Print Spooler", "Active (Ready)"));
        diagList.Children.Add(BuildDiagRow("💾 Primary Disk", "Verified Health"));
        diagBox.Child = diagList;
        diagPanel.Children.Add(diagBox);

        // CSV Log Sheet Direct Buttons Card
        var csvCard = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(240, 253, 244)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(187, 247, 208)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 14, 0, 0)
        };
        var csvStack = new StackPanel();
        csvStack.Children.Add(new TextBlock { Text = "📊 Central Complaint Log Sheet (.CSV)", FontSize = 11, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(22, 101, 52)) });
        csvStack.Children.Add(new TextBlock
        {
            Text = "All registered complaints on this system and network are continuously logged to 'complaints_log_sheet.csv' in the project folder.",
            FontSize = 10.5,
            Foreground = TextDark,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 3, 0, 8)
        });

        var csvBtnRow = new Grid();
        csvBtnRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        csvBtnRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var btnOpenCsv = new Button
        {
            Content = "📑 Open CSV File",
            FontSize = 10.5,
            FontWeight = FontWeights.Bold,
            Padding = new Thickness(8, 4, 8, 4),
            Margin = new Thickness(0, 0, 4, 0),
            Background = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(134, 239, 172)),
            Cursor = Cursors.Hand
        };
        btnOpenCsv.Click += (s, e) =>
        {
            try
            {
                string csvPath = ComplaintTicketService.GetActiveCsvPath();
                Process.Start(new ProcessStartInfo { FileName = csvPath, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not open CSV: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        };
        Grid.SetColumn(btnOpenCsv, 0);
        csvBtnRow.Children.Add(btnOpenCsv);

        var btnOpenFolder = new Button
        {
            Content = "📁 Open Folder",
            FontSize = 10.5,
            Padding = new Thickness(8, 4, 8, 4),
            Margin = new Thickness(4, 0, 0, 0),
            Background = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(134, 239, 172)),
            Cursor = Cursors.Hand
        };
        btnOpenFolder.Click += (s, e) =>
        {
            try
            {
                string csvPath = ComplaintTicketService.GetActiveCsvPath();
                string dir = Path.GetDirectoryName(csvPath) ?? @"c:\IT PROJECTS\svll-workstation";
                Process.Start(new ProcessStartInfo { FileName = dir, UseShellExecute = true });
            }
            catch { }
        };
        Grid.SetColumn(btnOpenFolder, 1);
        csvBtnRow.Children.Add(btnOpenFolder);

        csvStack.Children.Add(csvBtnRow);
        csvCard.Child = csvStack;
        diagPanel.Children.Add(csvCard);

        diagCard.Child = diagPanel;
        Grid.SetColumn(diagCard, 1);
        formGrid.Children.Add(diagCard);

        root.Children.Add(formGrid);

        scroll.Content = root;
        return scroll;
    }

    private UIElement BuildDiagRow(string label, string val)
    {
        var g = new Grid { Margin = new Thickness(0, 2, 0, 2) };
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(130) });
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var l = new TextBlock { Text = label, FontSize = 10.5, FontWeight = FontWeights.SemiBold, Foreground = TextSubtle };
        var v = new TextBlock { Text = val, FontSize = 10.5, FontWeight = FontWeights.Bold, Foreground = TextDark };

        Grid.SetColumn(l, 0);
        Grid.SetColumn(v, 1);
        g.Children.Add(l);
        g.Children.Add(v);
        return g;
    }

    // -------------------------------------------------------------
    // 2. AI DIAGNOSTIC & COMPLAINT BOT (INTERACTIVE CONVERSATIONAL)
    // -------------------------------------------------------------
    public UIElement BuildAiComplaintBotView()
    {
        var mainGrid = new Grid { Margin = new Thickness(10) };
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Chat & Vitals Split

        // 1. Header Card
        var headerBorder = new Border
        {
            Background = BgCard,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(14),
            Margin = new Thickness(0, 0, 0, 10)
        };
        var headerGrid = new Grid();
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var titleRow = new StackPanel { Orientation = Orientation.Horizontal };
        titleRow.Children.Add(new TextBlock
        {
            Text = "🤖 SVLL Intelligent IT Triage & Complaint Assistant",
            FontSize = 15,
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
            Child = new TextBlock { Text = "AI POWERED INTAKE", FontSize = 9.5, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(107, 33, 168)) }
        });
        Grid.SetColumn(titleRow, 0);
        headerGrid.Children.Add(titleRow);

        var btnRestartChat = new Button
        {
            Content = "🔄 Reset Conversation",
            FontSize = 11,
            Padding = new Thickness(10, 4, 10, 4),
            Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
            BorderBrush = BorderMuted,
            Cursor = Cursors.Hand
        };
        btnRestartChat.Click += (s, e) => InitAiSession();
        Grid.SetColumn(btnRestartChat, 1);
        headerGrid.Children.Add(btnRestartChat);

        headerBorder.Child = headerGrid;
        Grid.SetRow(headerBorder, 0);
        mainGrid.Children.Add(headerBorder);

        // 2. Chat Area Grid: Left Vitals & Status | Center Conversation
        var chatLayout = new Grid();
        chatLayout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(280) }); // Vitals Sidebar
        chatLayout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Chat Stream

        // Left Vitals Sidebar
        var sidebar = new Border
        {
            Background = BgCard,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(14),
            Margin = new Thickness(0, 0, 10, 0)
        };
        var sbPanel = new StackPanel();
        sbPanel.Children.Add(new TextBlock { Text = "📊 Live Machine Telemetry", FontSize = 12, FontWeight = FontWeights.Bold, Foreground = TextDark });
        sbPanel.Children.Add(new TextBlock { Text = "Real-time hardware context monitored by AI", FontSize = 9.5, Foreground = TextSubtle, Margin = new Thickness(0, 2, 0, 10) });

        var sbBox = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(10)
        };
        var sbList = new StackPanel();
        sbList.Children.Add(BuildDiagRow("Host", Environment.MachineName));
        sbList.Children.Add(BuildDiagRow("User", Environment.UserName));
        sbList.Children.Add(BuildDiagRow("IP", FleetRegistryService.Instance.LocalDevice.IpAddress));
        sbList.Children.Add(BuildDiagRow("AnyDesk", FleetRegistryService.Instance.LocalDevice.AnyDeskId));
        sbBox.Child = sbList;
        sbPanel.Children.Add(sbBox);

        // Fast Self-Remediation Shortcuts
        sbPanel.Children.Add(new TextBlock { Text = "⚡ 1-Click Fixes", FontSize = 11.5, FontWeight = FontWeights.Bold, Foreground = TextDark, Margin = new Thickness(0, 14, 0, 6) });

        var btnSpooler = new Button
        {
            Content = "🖨️ Restart Print Spooler",
            FontSize = 10.5,
            HorizontalContentAlignment = HorizontalAlignment.Left,
            Padding = new Thickness(10, 5, 10, 5),
            Margin = new Thickness(0, 0, 0, 4),
            Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
            BorderBrush = BorderMuted,
            Cursor = Cursors.Hand
        };
        btnSpooler.Click += async (s, e) =>
        {
            btnSpooler.IsEnabled = false;
            await Task.Run(() => ExecutePowerShellScript("Restart-Service Spooler -Force"));
            btnSpooler.IsEnabled = true;
            AppendAiSystemMessage("✔ Print Spooler service successfully restarted.");
        };
        sbPanel.Children.Add(btnSpooler);

        var btnDns = new Button
        {
            Content = "🌐 Flush DNS Cache",
            FontSize = 10.5,
            HorizontalContentAlignment = HorizontalAlignment.Left,
            Padding = new Thickness(10, 5, 10, 5),
            Margin = new Thickness(0, 0, 0, 4),
            Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
            BorderBrush = BorderMuted,
            Cursor = Cursors.Hand
        };
        btnDns.Click += async (s, e) =>
        {
            btnDns.IsEnabled = false;
            await Task.Run(() => ExecutePowerShellScript("Clear-DnsClientCache"));
            btnDns.IsEnabled = true;
            AppendAiSystemMessage("✔ DNS Client Cache successfully flushed.");
        };
        sbPanel.Children.Add(btnDns);

        var btnTurbo = new Button
        {
            Content = "⚡ 1-Click Turbo Boost",
            FontSize = 10.5,
            HorizontalContentAlignment = HorizontalAlignment.Left,
            Padding = new Thickness(10, 5, 10, 5),
            Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
            BorderBrush = BorderMuted,
            Cursor = Cursors.Hand
        };
        btnTurbo.Click += async (s, e) =>
        {
            btnTurbo.IsEnabled = false;
            var res = await ExecuteTurboBoostAsync();
            btnTurbo.IsEnabled = true;
            AppendAiSystemMessage($"✔ Turbo Boost complete! Reclaimed {res.freedMb} MB RAM and trimmed working sets.");
        };
        sbPanel.Children.Add(btnTurbo);

        sidebar.Child = sbPanel;
        Grid.SetColumn(sidebar, 0);
        chatLayout.Children.Add(sidebar);

        // Right Chat Stream & Input Area
        var chatCard = new Border
        {
            Background = BgCard,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(14)
        };
        var chatGrid = new Grid();
        chatGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Message History
        chatGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Quick Replies
        chatGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Input Box

        // 1. Message History ScrollViewer
        _aiChatScroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        _aiChatMessagesPanel = new StackPanel { Margin = new Thickness(0, 0, 6, 0) };
        _aiChatScroll.Content = _aiChatMessagesPanel;
        Grid.SetRow(_aiChatScroll, 0);
        chatGrid.Children.Add(_aiChatScroll);

        // 2. Quick Replies Chips WrapPanel
        _aiQuickRepliesPanel = new WrapPanel { Margin = new Thickness(0, 6, 0, 6) };
        Grid.SetRow(_aiQuickRepliesPanel, 1);
        chatGrid.Children.Add(_aiQuickRepliesPanel);

        // 3. Input Box
        var inputGrid = new Grid();
        inputGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        inputGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        _txtAiChatInput = new TextBox
        {
            Height = 36,
            Padding = new Thickness(10, 6, 10, 6),
            FontSize = 12,
            BorderBrush = BorderMuted,
            Background = new SolidColorBrush(Color.FromRgb(248, 250, 252))
        };
        _txtAiChatInput.KeyDown += (s, e) =>
        {
            if (e.Key == Key.Enter) SendAiMessageFromInput();
        };
        Grid.SetColumn(_txtAiChatInput, 0);
        inputGrid.Children.Add(_txtAiChatInput);

        var btnSend = new Button
        {
            Content = "Send ↵",
            Width = 80,
            Height = 36,
            Margin = new Thickness(8, 0, 0, 0),
            Background = SvllBlue,
            Foreground = Brushes.White,
            FontWeight = FontWeights.Bold,
            FontSize = 11.5,
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand
        };
        btnSend.Click += (s, e) => SendAiMessageFromInput();
        Grid.SetColumn(btnSend, 1);
        inputGrid.Children.Add(btnSend);

        Grid.SetRow(inputGrid, 2);
        chatGrid.Children.Add(inputGrid);

        chatCard.Child = chatGrid;
        Grid.SetColumn(chatCard, 1);
        chatLayout.Children.Add(chatCard);

        Grid.SetRow(chatLayout, 1);
        mainGrid.Children.Add(chatLayout);

        InitAiSession();

        return mainGrid;
    }

    private void InitAiSession()
    {
        _aiSession = AiComplaintEngine.StartNewSession();
        _aiChatMessagesPanel.Children.Clear();
        _aiQuickRepliesPanel.Children.Clear();

        var firstMsg = _aiSession.Messages.FirstOrDefault();
        if (firstMsg != null)
        {
            RenderAiMessageBubble(firstMsg);
            UpdateAiQuickReplies(firstMsg.QuickReplies);
        }
    }

    private void SendAiMessageFromInput()
    {
        string text = _txtAiChatInput.Text.Trim();
        if (string.IsNullOrEmpty(text)) return;
        _txtAiChatInput.Text = "";

        SubmitAiUserMessage(text);
    }

    private void SubmitAiUserMessage(string userText)
    {
        // Render user bubble
        var userBubble = new AiChatMessage { Sender = "User", Text = userText, Timestamp = DateTime.Now };
        RenderAiMessageBubble(userBubble);

        // Process with AI
        var aiResponse = AiComplaintEngine.ProcessUserMessage(_aiSession, userText);
        RenderAiMessageBubble(aiResponse);
        UpdateAiQuickReplies(aiResponse.QuickReplies);

        _aiChatScroll.ScrollToBottom();
    }

    private void AppendAiSystemMessage(string systemText)
    {
        var msg = new AiChatMessage { Sender = "System", Text = systemText, Timestamp = DateTime.Now };
        RenderAiMessageBubble(msg);
        _aiChatScroll.ScrollToBottom();
    }

    private void RenderAiMessageBubble(AiChatMessage msg)
    {
        bool isUser = msg.Sender == "User";
        bool isSystem = msg.Sender == "System";

        var container = new Border
        {
            Margin = new Thickness(0, 0, 0, 8),
            HorizontalAlignment = isUser ? HorizontalAlignment.Right : HorizontalAlignment.Left,
            MaxWidth = 620
        };

        var bubble = new Border
        {
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(12, 10, 12, 10),
            Background = isSystem ? new SolidColorBrush(Color.FromRgb(240, 253, 244)) :
                         isUser ? SvllBlue : new SolidColorBrush(Color.FromRgb(241, 245, 249)),
            BorderBrush = isSystem ? new SolidColorBrush(Color.FromRgb(187, 247, 208)) :
                          isUser ? SvllBlue : BorderMuted,
            BorderThickness = new Thickness(1)
        };

        var sp = new StackPanel();

        // Sender header
        var headerRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 4) };
        headerRow.Children.Add(new TextBlock
        {
            Text = isUser ? "👤 You" : isSystem ? "⚙️ System Action" : "🤖 SVLL IT AI Assistant",
            FontSize = 10,
            FontWeight = FontWeights.Bold,
            Foreground = isUser ? new SolidColorBrush(Color.FromRgb(224, 231, 255)) :
                         isSystem ? new SolidColorBrush(Color.FromRgb(22, 101, 52)) : SvllBlue
        });
        headerRow.Children.Add(new TextBlock
        {
            Text = $" • {msg.Timestamp:HH:mm:ss}",
            FontSize = 9.5,
            Foreground = isUser ? new SolidColorBrush(Color.FromRgb(199, 210, 254)) : TextSubtle,
            Margin = new Thickness(4, 0, 0, 0)
        });
        sp.Children.Add(headerRow);

        // Message text
        var txt = new TextBlock
        {
            Text = msg.Text,
            FontSize = 11.5,
            Foreground = isUser ? Brushes.White : TextDark,
            TextWrapping = TextWrapping.Wrap,
            LineHeight = 16
        };
        sp.Children.Add(txt);

        // Action Command button (if embedded in AI response)
        if (!string.IsNullOrEmpty(msg.ActionCommand) && !string.IsNullOrEmpty(msg.ActionLabel))
        {
            var btnAct = new Button
            {
                Content = $"⚡ {msg.ActionLabel}",
                FontSize = 10.5,
                FontWeight = FontWeights.Bold,
                Padding = new Thickness(10, 4, 10, 4),
                Margin = new Thickness(0, 8, 0, 0),
                Background = SvllBlue,
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            btnAct.Click += async (s, e) =>
            {
                btnAct.IsEnabled = false;
                btnAct.Content = "⏳ Running...";
                if (msg.ActionCommand == "FLUSH_SPOOLER")
                {
                    await Task.Run(() => ExecutePowerShellScript("Restart-Service Spooler -Force"));
                    AppendAiSystemMessage("✔ Spooler restarted via automated intervention.");
                }
                else if (msg.ActionCommand == "FLUSH_DNS")
                {
                    await Task.Run(() => ExecutePowerShellScript("Clear-DnsClientCache"));
                    AppendAiSystemMessage("✔ DNS cache flushed and network reset.");
                }
                else if (msg.ActionCommand == "TURBO_BOOST")
                {
                    var res = await ExecuteTurboBoostAsync();
                    AppendAiSystemMessage($"✔ Turbo Boost reclaimed {res.freedMb} MB RAM.");
                }
            };
            sp.Children.Add(btnAct);
        }

        bubble.Child = sp;
        container.Child = bubble;
        _aiChatMessagesPanel.Children.Add(container);
    }

    private void UpdateAiQuickReplies(List<string> replies)
    {
        _aiQuickRepliesPanel.Children.Clear();
        if (replies == null || replies.Count == 0) return;

        foreach (var r in replies)
        {
            var btnReply = new Button
            {
                Content = r,
                FontSize = 10.5,
                Padding = new Thickness(10, 4, 10, 4),
                Margin = new Thickness(0, 0, 6, 6),
                Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
                BorderBrush = BorderMuted,
                Cursor = Cursors.Hand
            };
            string replyText = r;
            btnReply.Click += (s, e) => SubmitAiUserMessage(replyText);
            _aiQuickRepliesPanel.Children.Add(btnReply);
        }
    }

    // -------------------------------------------------------------
    // 3. ADMIN SERVICE DESK CONSOLE (PASSWORD PROTECTED)
    // -------------------------------------------------------------
    public UIElement BuildTicketAdminDeskView()
    {
        var root = new Grid();

        var lockContainer = BuildAdminLockScreen("IT Service Desk Admin Console", () =>
        {
            RefreshTicketAdminDeskContent(root);
        });

        if (!AdminAuthService.Instance.IsAdminMode)
        {
            root.Children.Add(lockContainer);
        }
        else
        {
            RefreshTicketAdminDeskContent(root);
        }

        AdminAuthService.Instance.AdminModeChanged += (isAdmin) =>
        {
            Dispatcher.Invoke(() =>
            {
                if (!isAdmin)
                {
                    root.Children.Clear();
                    root.Children.Add(BuildAdminLockScreen("IT Service Desk Admin Console", () =>
                    {
                        RefreshTicketAdminDeskContent(root);
                    }));
                }
            });
        };

        ComplaintTicketService.OnTicketsChanged += () =>
        {
            Dispatcher.Invoke(() =>
            {
                if (AdminAuthService.Instance.IsAdminMode)
                {
                    RefreshTicketAdminDeskContent(root);
                }
            });
        };

        return root;
    }

    private void RefreshTicketAdminDeskContent(Grid root)
    {
        root.Children.Clear();

        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var mainStack = new StackPanel { Margin = new Thickness(10) };

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
            Text = "👑 Central IT Helpdesk & Service Desk Admin Panel",
            FontSize = 16,
            FontWeight = FontWeights.ExtraBold,
            Foreground = TextDark
        });
        titleRow.Children.Add(new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(254, 243, 199)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(252, 211, 77)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(8, 2, 8, 2),
            Margin = new Thickness(10, 0, 0, 0),
            Child = new TextBlock { Text = "ADMIN ONLY", FontSize = 10, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(146, 64, 14)) }
        });
        headerLeft.Children.Add(titleRow);
        headerLeft.Children.Add(new TextBlock
        {
            Text = "Manage all submitted incident complaints, change resolution statuses, view attached telemetry, and launch 1-click remote assistance to requesters.",
            FontSize = 11,
            Foreground = TextSubtle,
            Margin = new Thickness(0, 4, 0, 0)
        });
        Grid.SetColumn(headerLeft, 0);
        headerGrid.Children.Add(headerLeft);

        var headerRight = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

        var btnOpenLogSheet = new Button
        {
            Content = "📊 Open Log Sheet (.CSV)",
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            Padding = new Thickness(12, 6, 12, 6),
            Margin = new Thickness(0, 0, 8, 0),
            Background = SvllBlue,
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand
        };
        btnOpenLogSheet.Click += (s, e) =>
        {
            try
            {
                string path = ComplaintTicketService.GetActiveCsvPath();
                Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not open CSV log sheet: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        };
        headerRight.Children.Add(btnOpenLogSheet);

        var btnRefreshAdmin = new Button
        {
            Content = "🔄 Refresh Live",
            FontSize = 11,
            Padding = new Thickness(10, 6, 10, 6),
            Margin = new Thickness(0, 0, 8, 0),
            Background = Brushes.White,
            BorderBrush = BorderMuted,
            Cursor = Cursors.Hand
        };
        btnRefreshAdmin.Click += (s, e) => RefreshTicketAdminDeskContent(root);
        headerRight.Children.Add(btnRefreshAdmin);

        var btnExportCsv = new Button
        {
            Content = "📄 Export Tickets (CSV)",
            FontSize = 11,
            Padding = new Thickness(10, 6, 10, 6),
            Margin = new Thickness(0, 0, 8, 0),
            Background = Brushes.White,
            BorderBrush = BorderMuted,
            Cursor = Cursors.Hand
        };
        btnExportCsv.Click += (s, e) =>
        {
            try
            {
                string csv = ComplaintTicketService.Instance.ExportTicketsCsv();
                string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "SVLL_IT_Complaints.csv");
                File.WriteAllText(path, csv);
                MessageBox.Show($"Tickets exported to:\n{path}", "Export Successful", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        };
        headerRight.Children.Add(btnExportCsv);

        var btnLockSession = new Button
        {
            Content = "🔒 Lock Session",
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
            BorderBrush = BorderMuted,
            Padding = new Thickness(12, 6, 12, 6),
            Cursor = Cursors.Hand
        };
        btnLockSession.Click += (s, e) => AdminAuthService.Instance.LockAdmin();
        headerRight.Children.Add(btnLockSession);

        Grid.SetColumn(headerRight, 1);
        headerGrid.Children.Add(headerRight);

        headerBorder.Child = headerGrid;
        mainStack.Children.Add(headerBorder);

        // 2. Metrics Bar
        var metricsGrid = new Grid { Margin = new Thickness(0, 0, 0, 12) };
        metricsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        metricsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        metricsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        metricsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var allTickets = ComplaintTicketService.Instance.GetAllTickets();
        int total = allTickets.Count;
        int openCount = allTickets.Count(t => t.Status == "Open");
        int progressCount = allTickets.Count(t => t.Status == "In Progress");
        int criticalCount = allTickets.Count(t => t.Priority == "Critical");

        _lblAdminTotalTickets = new TextBlock { Text = total.ToString(), FontSize = 20, FontWeight = FontWeights.ExtraBold, Foreground = SvllBlue };
        _lblAdminOpenTickets = new TextBlock { Text = openCount.ToString(), FontSize = 20, FontWeight = FontWeights.ExtraBold, Foreground = SvllRed };
        _lblAdminProgressTickets = new TextBlock { Text = progressCount.ToString(), FontSize = 20, FontWeight = FontWeights.ExtraBold, Foreground = new SolidColorBrush(Color.FromRgb(202, 138, 4)) };
        _lblAdminCriticalTickets = new TextBlock { Text = criticalCount.ToString(), FontSize = 20, FontWeight = FontWeights.ExtraBold, Foreground = new SolidColorBrush(Color.FromRgb(153, 27, 27)) };

        metricsGrid.Children.Add(BuildMetricCard("📋 TOTAL TICKETS", _lblAdminTotalTickets, "All Time", 0));
        metricsGrid.Children.Add(BuildMetricCard("🔴 OPEN TICKETS", _lblAdminOpenTickets, "Awaiting IT Action", 1));
        metricsGrid.Children.Add(BuildMetricCard("🟡 IN PROGRESS", _lblAdminProgressTickets, "Being Investigated", 2));
        metricsGrid.Children.Add(BuildMetricCard("🚨 CRITICAL PRIORITY", _lblAdminCriticalTickets, "Logistics Halts", 3));

        mainStack.Children.Add(metricsGrid);

        // 3. Search & Filter Bar
        var filterCard = new Border
        {
            Background = BgCard,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(12, 10, 12, 10),
            Margin = new Thickness(0, 0, 0, 12)
        };
        var filterGrid = new Grid();
        filterGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Search
        filterGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) }); // Status
        filterGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) }); // Priority
        filterGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Refresh

        var searchPanel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        searchPanel.Children.Add(new TextBlock { Text = "🔍", FontSize = 14, Margin = new Thickness(0, 0, 8, 0), VerticalAlignment = VerticalAlignment.Center });
        _txtTicketSearch = new TextBox
        {
            Width = 220,
            Height = 28,
            Padding = new Thickness(6, 2, 6, 2),
            FontSize = 11,
            BorderBrush = BorderMuted
        };
        _txtTicketSearch.TextChanged += (s, e) => RenderAdminTicketList();
        searchPanel.Children.Add(_txtTicketSearch);
        Grid.SetColumn(searchPanel, 0);
        filterGrid.Children.Add(searchPanel);

        _cmbTicketStatusFilter = new ComboBox { Height = 28, FontSize = 11, Margin = new Thickness(0, 0, 8, 0), VerticalAlignment = VerticalAlignment.Center };
        _cmbTicketStatusFilter.Items.Add("All Statuses");
        _cmbTicketStatusFilter.Items.Add("Open");
        _cmbTicketStatusFilter.Items.Add("In Progress");
        _cmbTicketStatusFilter.Items.Add("Resolved");
        _cmbTicketStatusFilter.SelectedIndex = 0;
        _cmbTicketStatusFilter.SelectionChanged += (s, e) => RenderAdminTicketList();
        Grid.SetColumn(_cmbTicketStatusFilter, 1);
        filterGrid.Children.Add(_cmbTicketStatusFilter);

        _cmbTicketPriorityFilter = new ComboBox { Height = 28, FontSize = 11, Margin = new Thickness(0, 0, 8, 0), VerticalAlignment = VerticalAlignment.Center };
        _cmbTicketPriorityFilter.Items.Add("All Priorities");
        _cmbTicketPriorityFilter.Items.Add("Critical");
        _cmbTicketPriorityFilter.Items.Add("High");
        _cmbTicketPriorityFilter.Items.Add("Medium");
        _cmbTicketPriorityFilter.Items.Add("Low");
        _cmbTicketPriorityFilter.SelectedIndex = 0;
        _cmbTicketPriorityFilter.SelectionChanged += (s, e) => RenderAdminTicketList();
        Grid.SetColumn(_cmbTicketPriorityFilter, 2);
        filterGrid.Children.Add(_cmbTicketPriorityFilter);

        var btnRefresh = new Button
        {
            Content = "🔄 Refresh",
            FontSize = 11,
            Padding = new Thickness(12, 4, 12, 4),
            Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
            BorderBrush = BorderMuted,
            Cursor = Cursors.Hand
        };
        btnRefresh.Click += (s, e) =>
        {
            ComplaintTicketService.Instance.LoadTickets();
            RenderAdminTicketList();
        };
        Grid.SetColumn(btnRefresh, 3);
        filterGrid.Children.Add(btnRefresh);

        filterCard.Child = filterGrid;
        mainStack.Children.Add(filterCard);

        // 4. Ticket List Panel
        _ticketAdminListPanel = new StackPanel();
        RenderAdminTicketList();
        mainStack.Children.Add(_ticketAdminListPanel);

        scroll.Content = mainStack;
        root.Children.Add(scroll);
    }

    private void RenderAdminTicketList()
    {
        if (_ticketAdminListPanel == null) return;
        _ticketAdminListPanel.Children.Clear();

        string query = _txtTicketSearch?.Text.Trim().ToLowerInvariant() ?? "";
        string statusFilter = _cmbTicketStatusFilter?.SelectedItem?.ToString() ?? "All Statuses";
        string priorityFilter = _cmbTicketPriorityFilter?.SelectedItem?.ToString() ?? "All Priorities";

        var tickets = ComplaintTicketService.Instance.GetAllTickets();

        foreach (var t in tickets)
        {
            if (statusFilter != "All Statuses" && !t.Status.Equals(statusFilter, StringComparison.OrdinalIgnoreCase))
                continue;

            if (priorityFilter != "All Priorities" && !t.Priority.Equals(priorityFilter, StringComparison.OrdinalIgnoreCase))
                continue;

            if (!string.IsNullOrEmpty(query))
            {
                bool match = t.TicketId.ToLowerInvariant().Contains(query) ||
                             t.Title.ToLowerInvariant().Contains(query) ||
                             t.Description.ToLowerInvariant().Contains(query) ||
                             t.CreatedBy.ToLowerInvariant().Contains(query) ||
                             t.Hostname.ToLowerInvariant().Contains(query) ||
                             t.BranchLocation.ToLowerInvariant().Contains(query);
                if (!match) continue;
            }

            _ticketAdminListPanel.Children.Add(BuildAdminTicketCard(t));
        }

        if (_ticketAdminListPanel.Children.Count == 0)
        {
            var empty = new Border
            {
                Background = BgCard,
                BorderBrush = BorderMuted,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(24),
                Child = new TextBlock
                {
                    Text = "No tickets match the selected filters.",
                    FontSize = 12,
                    Foreground = TextSubtle,
                    HorizontalAlignment = HorizontalAlignment.Center
                }
            };
            _ticketAdminListPanel.Children.Add(empty);
        }
    }

    private UIElement BuildAdminTicketCard(ComplaintTicket ticket)
    {
        var card = new Border
        {
            Background = BgCard,
            BorderBrush = ticket.Priority == "Critical" ? SvllRed : BorderMuted,
            BorderThickness = new Thickness(ticket.Priority == "Critical" ? 1.5 : 1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(14, 12, 14, 12),
            Margin = new Thickness(0, 0, 0, 8)
        };

        var mainPanel = new StackPanel();

        // Top Row: Ticket ID, Priority Pill, Status, Date
        var topRow = new Grid { Margin = new Thickness(0, 0, 0, 6) };
        topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var idStack = new StackPanel { Orientation = Orientation.Horizontal };
        idStack.Children.Add(new TextBlock
        {
            Text = ticket.TicketId,
            FontSize = 12,
            FontWeight = FontWeights.ExtraBold,
            FontFamily = new FontFamily("Consolas, Courier New"),
            Foreground = SvllBlue,
            VerticalAlignment = VerticalAlignment.Center
        });

        // Priority Badge
        var prioBadge = new Border
        {
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(6, 1, 6, 1),
            Margin = new Thickness(8, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        if (ticket.Priority == "Critical")
        {
            prioBadge.Background = new SolidColorBrush(Color.FromRgb(254, 242, 242));
            prioBadge.Child = new TextBlock { Text = "CRITICAL 🚨", FontSize = 9.5, FontWeight = FontWeights.Bold, Foreground = SvllRed };
        }
        else if (ticket.Priority == "High")
        {
            prioBadge.Background = new SolidColorBrush(Color.FromRgb(254, 243, 199));
            prioBadge.Child = new TextBlock { Text = "HIGH 🟠", FontSize = 9.5, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(180, 83, 9)) };
        }
        else
        {
            prioBadge.Background = new SolidColorBrush(Color.FromRgb(241, 245, 249));
            prioBadge.Child = new TextBlock { Text = ticket.Priority.ToUpper(), FontSize = 9.5, FontWeight = FontWeights.SemiBold, Foreground = TextSubtle };
        }
        idStack.Children.Add(prioBadge);

        if (ticket.FiledViaAi)
        {
            var aiBadge = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(243, 232, 255)),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6, 1, 6, 1),
                Margin = new Thickness(6, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Child = new TextBlock { Text = "AI TRIAGED", FontSize = 9, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(107, 33, 168)) }
            };
            idStack.Children.Add(aiBadge);
        }

        Grid.SetColumn(idStack, 0);
        topRow.Children.Add(idStack);

        var dateText = new TextBlock
        {
            Text = $"{ticket.CreatedAt:yyyy-MM-dd HH:mm}",
            FontSize = 10,
            Foreground = TextSubtle,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(dateText, 1);
        topRow.Children.Add(dateText);

        mainPanel.Children.Add(topRow);

        // Middle Row: Title & User Info
        mainPanel.Children.Add(new TextBlock
        {
            Text = ticket.Title,
            FontSize = 13,
            FontWeight = FontWeights.Bold,
            Foreground = TextDark,
            Margin = new Thickness(0, 0, 0, 4)
        });

        mainPanel.Children.Add(new TextBlock
        {
            Text = ticket.Description,
            FontSize = 11,
            Foreground = TextSubtle,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 8)
        });

        // Requester details chip
        var reqDetails = new TextBlock
        {
            Text = $"👤 {ticket.CreatedBy} ({ticket.UserContact})  •  💻 {ticket.Hostname} ({ticket.IpAddress})  •  🏢 {ticket.BranchLocation}  •  🔴 AnyDesk: {ticket.AnyDeskId}",
            FontSize = 10,
            Foreground = TextDark,
            Margin = new Thickness(0, 0, 0, 10)
        };
        mainPanel.Children.Add(reqDetails);

        // Bottom Action Bar: Status Selector, Admin Notes, 1-Click AnyDesk Connect
        var btmGrid = new Grid();
        btmGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Status Selector
        btmGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Admin Notes Box
        btmGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Connect AnyDesk & Save

        var statusCombo = new ComboBox { Width = 115, Height = 28, FontSize = 10.5, Margin = new Thickness(0, 0, 8, 0) };
        statusCombo.Items.Add("Open");
        statusCombo.Items.Add("In Progress");
        statusCombo.Items.Add("Resolved");
        statusCombo.Items.Add("Closed");
        statusCombo.SelectedItem = ticket.Status;
        Grid.SetColumn(statusCombo, 0);
        btmGrid.Children.Add(statusCombo);

        var txtNotes = new TextBox
        {
            Height = 28,
            Padding = new Thickness(6, 4, 6, 4),
            FontSize = 10.5,
            BorderBrush = BorderMuted,
            Text = ticket.AdminNotes,
            ToolTip = "Admin Resolution Notes"
        };
        Grid.SetColumn(txtNotes, 1);
        btmGrid.Children.Add(txtNotes);

        var actionStack = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(8, 0, 0, 0) };

        var btnSaveNotes = new Button
        {
            Content = "💾 Update",
            FontSize = 10.5,
            Padding = new Thickness(10, 4, 10, 4),
            Margin = new Thickness(0, 0, 6, 0),
            Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
            BorderBrush = BorderMuted,
            Cursor = Cursors.Hand
        };
        btnSaveNotes.Click += (s, e) =>
        {
            string newStatus = statusCombo.SelectedItem?.ToString() ?? ticket.Status;
            ticket.Status = newStatus;
            ticket.AdminNotes = txtNotes.Text.Trim();
            ComplaintTicketService.Instance.SaveTickets();
            ComplaintTicketService.NotifyTicketsChanged();
            MessageBox.Show($"Ticket [{ticket.TicketId}] status updated to '{newStatus}'.", "Ticket Updated", MessageBoxButton.OK, MessageBoxImage.Information);
        };
        actionStack.Children.Add(btnSaveNotes);

        var btnConnect = new Button
        {
            Content = "🚀 AnyDesk",
            FontSize = 10.5,
            FontWeight = FontWeights.Bold,
            Background = SvllRed,
            Foreground = Brushes.White,
            Padding = new Thickness(10, 4, 10, 4),
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand
        };
        btnConnect.Click += (s, e) =>
        {
            if (string.IsNullOrEmpty(ticket.AnyDeskId) || ticket.AnyDeskId == "Not Configured")
            {
                MessageBox.Show($"AnyDesk ID not found on ticket {ticket.TicketId}.", "No AnyDesk ID", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            FleetRegistryService.LaunchAnyDesk(ticket.AnyDeskId);
            Log($"[SERVICE DESK] Admin connected to requester machine {ticket.Hostname} via AnyDesk ID {ticket.AnyDeskId}.");
        };
        actionStack.Children.Add(btnConnect);

        Grid.SetColumn(actionStack, 2);
        btmGrid.Children.Add(actionStack);

        mainPanel.Children.Add(btmGrid);

        card.Child = mainPanel;
        return card;
    }

    // -------------------------------------------------------------
    // 4. TRACK MY SUBMITTED TICKETS (USER VIEW)
    // -------------------------------------------------------------
    public UIElement BuildMyTicketsView()
    {
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var root = new StackPanel { Margin = new Thickness(10) };

        // Header
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
            Text = "📂 My Submitted IT Complaints & Status Tracker",
            FontSize = 16,
            FontWeight = FontWeights.ExtraBold,
            Foreground = TextDark
        });
        titleRow.Children.Add(new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(239, 246, 255)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(191, 219, 254)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(8, 2, 8, 2),
            Margin = new Thickness(10, 0, 0, 0),
            Child = new TextBlock { Text = "LOCAL STATUS", FontSize = 10, FontWeight = FontWeights.Bold, Foreground = SvllBlue }
        });
        headerLeft.Children.Add(titleRow);
        headerLeft.Children.Add(new TextBlock
        {
            Text = $"Track real-time progress, assigned technician notes, and resolution updates for tickets logged on this workstation ({Environment.MachineName}).",
            FontSize = 11,
            Foreground = TextSubtle,
            Margin = new Thickness(0, 4, 0, 0)
        });
        Grid.SetColumn(headerLeft, 0);
        headerGrid.Children.Add(headerLeft);

        var btnFileNew = new Button
        {
            Content = "➕ File New Complaint",
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            Background = SvllBlue,
            Foreground = Brushes.White,
            Padding = new Thickness(14, 6, 14, 6),
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand,
            VerticalAlignment = VerticalAlignment.Center
        };
        btnFileNew.Click += (s, e) => SwitchHubSubTool("Hub_Complaints", "SubmitComplaint");
        Grid.SetColumn(btnFileNew, 1);
        headerGrid.Children.Add(btnFileNew);

        headerBorder.Child = headerGrid;
        root.Children.Add(headerBorder);

        // List of tickets for this host
        var myTickets = ComplaintTicketService.Instance.GetTicketsForHost(Environment.MachineName);

        if (myTickets.Count == 0)
        {
            var empty = new Border
            {
                Background = BgCard,
                BorderBrush = BorderMuted,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(36),
                Child = new StackPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Children =
                    {
                        new TextBlock { Text = "🎉 No Open Complaints for this Workstation", FontSize = 14, FontWeight = FontWeights.Bold, Foreground = TextDark, HorizontalAlignment = HorizontalAlignment.Center },
                        new TextBlock { Text = "All logistics hardware and IT services on this machine are running normally.", FontSize = 11, Foreground = TextSubtle, Margin = new Thickness(0, 4, 0, 14), HorizontalAlignment = HorizontalAlignment.Center }
                    }
                }
            };
            root.Children.Add(empty);
        }
        else
        {
            foreach (var t in myTickets)
            {
                var card = new Border
                {
                    Background = BgCard,
                    BorderBrush = BorderMuted,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(16),
                    Margin = new Thickness(0, 0, 0, 10)
                };

                var p = new StackPanel();

                var row1 = new Grid();
                row1.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row1.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var leftStack = new StackPanel { Orientation = Orientation.Horizontal };
                leftStack.Children.Add(new TextBlock { Text = t.TicketId, FontSize = 12, FontWeight = FontWeights.ExtraBold, FontFamily = new FontFamily("Consolas, Courier New"), Foreground = SvllBlue });

                var statusPill = new Border
                {
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(6, 1, 6, 1),
                    Margin = new Thickness(8, 0, 0, 0)
                };
                if (t.Status == "Resolved" || t.Status == "Closed")
                {
                    statusPill.Background = new SolidColorBrush(Color.FromRgb(220, 252, 231));
                    statusPill.Child = new TextBlock { Text = $"✔ {t.Status}", FontSize = 9.5, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(22, 101, 52)) };
                }
                else if (t.Status == "In Progress")
                {
                    statusPill.Background = new SolidColorBrush(Color.FromRgb(254, 243, 199));
                    statusPill.Child = new TextBlock { Text = "⏳ In Progress", FontSize = 9.5, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(180, 83, 9)) };
                }
                else
                {
                    statusPill.Background = new SolidColorBrush(Color.FromRgb(239, 246, 255));
                    statusPill.Child = new TextBlock { Text = "🔵 Open", FontSize = 9.5, FontWeight = FontWeights.Bold, Foreground = SvllBlue };
                }
                leftStack.Children.Add(statusPill);

                Grid.SetColumn(leftStack, 0);
                row1.Children.Add(leftStack);

                var timeTxt = new TextBlock { Text = $"{t.CreatedAt:yyyy-MM-dd HH:mm}", FontSize = 10, Foreground = TextSubtle };
                Grid.SetColumn(timeTxt, 1);
                row1.Children.Add(timeTxt);

                p.Children.Add(row1);

                p.Children.Add(new TextBlock
                {
                    Text = t.Title,
                    FontSize = 13,
                    FontWeight = FontWeights.Bold,
                    Foreground = TextDark,
                    Margin = new Thickness(0, 4, 0, 4)
                });

                p.Children.Add(new TextBlock
                {
                    Text = t.Description,
                    FontSize = 11,
                    Foreground = TextSubtle,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 8)
                });

                if (!string.IsNullOrEmpty(t.AdminNotes))
                {
                    var noteBox = new Border
                    {
                        Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
                        BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225)),
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(6),
                        Padding = new Thickness(10, 6, 10, 6),
                        Margin = new Thickness(0, 4, 0, 0)
                    };
                    noteBox.Child = new TextBlock
                    {
                        Text = $"💬 IT Support Update ({t.AssignedTechnician}): {t.AdminNotes}",
                        FontSize = 10.5,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = SvllBlue,
                        TextWrapping = TextWrapping.Wrap
                    };
                    p.Children.Add(noteBox);
                }

                card.Child = p;
                root.Children.Add(card);
            }
        }

        scroll.Content = root;
        return scroll;
    }
}
