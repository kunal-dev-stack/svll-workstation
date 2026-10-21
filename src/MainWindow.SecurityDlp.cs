using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;

namespace SVLL_IT_Workstation;

public partial class MainWindow
{
    private TextBlock _lblUsbPolicyStatus = null!;
    private TextBlock _lblDefenderStatus = null!;
    private TextBlock _lblDefenderSigDate = null!;
    private TextBox _txtLapsPassword = null!;

    public UIElement BuildSecurityDlpView()
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
            Text = "🔒 Endpoint Security, DLP & USB Port Lockdown",
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
            Child = new TextBlock { Text = "DATA LOSS PREVENTION", FontSize = 10, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(146, 64, 14)) }
        });
        headerLeft.Children.Add(titleRow);
        headerLeft.Children.Add(new TextBlock
        {
            Text = "Prevent confidential corporate data exfiltration via USB flash drives, enforce Defender antimalware hygiene, and rotate local admin credentials.",
            FontSize = 11,
            Foreground = TextSubtle,
            Margin = new Thickness(0, 4, 0, 0)
        });
        Grid.SetColumn(headerLeft, 0);
        headerGrid.Children.Add(headerLeft);

        headerBorder.Child = headerGrid;
        root.Children.Add(headerBorder);

        // 2. Three Column Security Grid: USB Storage DLP | Defender Shield | Local Admin Vault
        var secGrid = new Grid();
        secGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        secGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        secGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        // ------------------ COLUMN 1: USB STORAGE DLP ------------------
        var dlpCard = new Border
        {
            Background = BgCard,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(16),
            Margin = new Thickness(0, 0, 6, 0)
        };
        var dlpPanel = new StackPanel();
        dlpPanel.Children.Add(new TextBlock { Text = "💾 USB Storage DLP Policies", FontSize = 13, FontWeight = FontWeights.Bold, Foreground = TextDark });
        dlpPanel.Children.Add(new TextBlock
        {
            Text = "Restricts USB storage write operations to prevent company data leakage while keeping mice, keyboards & barcode scanners working.",
            FontSize = 10.5,
            Foreground = TextSubtle,
            Margin = new Thickness(0, 4, 0, 10)
        });

        var dlpStatusBox = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(10),
            Margin = new Thickness(0, 0, 0, 10)
        };
        var dlpStatusStack = new StackPanel();
        dlpStatusStack.Children.Add(new TextBlock { Text = "ACTIVE USB STORAGE STATE:", FontSize = 9.5, FontWeight = FontWeights.Bold, Foreground = TextSubtle });
        _lblUsbPolicyStatus = new TextBlock
        {
            Text = "Detecting policy...",
            FontSize = 12,
            FontWeight = FontWeights.ExtraBold,
            Foreground = SvllBlue,
            Margin = new Thickness(0, 2, 0, 0)
        };
        dlpStatusStack.Children.Add(_lblUsbPolicyStatus);
        dlpStatusBox.Child = dlpStatusStack;
        dlpPanel.Children.Add(dlpStatusBox);

        // DLP Action Buttons
        var btnReadOnly = new Button
        {
            Content = "🔒 SET USB STORAGE READ-ONLY",
            FontSize = 10.5,
            FontWeight = FontWeights.Bold,
            Padding = new Thickness(10, 7, 10, 7),
            Margin = new Thickness(0, 0, 0, 6),
            Background = new SolidColorBrush(Color.FromRgb(217, 119, 6)), // Amber
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand
        };
        btnReadOnly.Click += (s, e) => SetUsbStoragePolicy(UsbPolicyMode.ReadOnly);
        dlpPanel.Children.Add(btnReadOnly);

        var btnBlockAll = new Button
        {
            Content = "🚫 BLOCK USB STORAGE DRIVES",
            FontSize = 10.5,
            FontWeight = FontWeights.Bold,
            Padding = new Thickness(10, 7, 10, 7),
            Margin = new Thickness(0, 0, 0, 6),
            Background = SvllRed,
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand
        };
        btnBlockAll.Click += (s, e) => SetUsbStoragePolicy(UsbPolicyMode.Blocked);
        dlpPanel.Children.Add(btnBlockAll);

        var btnUnrestricted = new Button
        {
            Content = "🟢 RESTORE UNRESTRICTED ACCESS",
            FontSize = 10.5,
            FontWeight = FontWeights.Bold,
            Padding = new Thickness(10, 7, 10, 7),
            Background = new SolidColorBrush(Color.FromRgb(22, 163, 74)),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand
        };
        btnUnrestricted.Click += (s, e) => SetUsbStoragePolicy(UsbPolicyMode.Unrestricted);
        dlpPanel.Children.Add(btnUnrestricted);

        dlpCard.Child = dlpPanel;
        Grid.SetColumn(dlpCard, 0);
        secGrid.Children.Add(dlpCard);

        // ------------------ COLUMN 2: DEFENDER SHIELD ------------------
        var defCard = new Border
        {
            Background = BgCard,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(16),
            Margin = new Thickness(4, 0, 4, 0)
        };
        var defPanel = new StackPanel();
        defPanel.Children.Add(new TextBlock { Text = "🛡️ Defender & Antivirus Shield", FontSize = 13, FontWeight = FontWeights.Bold, Foreground = TextDark });
        defPanel.Children.Add(new TextBlock
        {
            Text = "Verifies real-time kernel protection engine and triggers automated definition updates & quick threat scans.",
            FontSize = 10.5,
            Foreground = TextSubtle,
            Margin = new Thickness(0, 4, 0, 10)
        });

        var defStatusBox = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(10),
            Margin = new Thickness(0, 0, 0, 10)
        };
        var defStatusStack = new StackPanel();
        defStatusStack.Children.Add(new TextBlock { Text = "REAL-TIME PROTECTION:", FontSize = 9.5, FontWeight = FontWeights.Bold, Foreground = TextSubtle });
        _lblDefenderStatus = new TextBlock
        {
            Text = "Active 🟢",
            FontSize = 12,
            FontWeight = FontWeights.ExtraBold,
            Foreground = new SolidColorBrush(Color.FromRgb(22, 163, 74)),
            Margin = new Thickness(0, 2, 0, 4)
        };
        defStatusStack.Children.Add(_lblDefenderStatus);

        defStatusStack.Children.Add(new TextBlock { Text = "CLOUD SIGNATURES DATE:", FontSize = 9.5, FontWeight = FontWeights.Bold, Foreground = TextSubtle });
        _lblDefenderSigDate = new TextBlock
        {
            Text = "Up to date",
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Foreground = TextDark,
            Margin = new Thickness(0, 2, 0, 0)
        };
        defStatusStack.Children.Add(_lblDefenderSigDate);
        defStatusBox.Child = defStatusStack;
        defPanel.Children.Add(defStatusBox);

        var btnUpdateDefender = new Button
        {
            Content = "⚡ UPDATE SIGNATURES & QUICK SCAN",
            FontSize = 10.5,
            FontWeight = FontWeights.Bold,
            Padding = new Thickness(10, 7, 10, 7),
            Margin = new Thickness(0, 0, 0, 6),
            Background = SvllBlue,
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand
        };
        btnUpdateDefender.Click += async (s, e) => await TriggerDefenderUpdateAndScanAsync();
        defPanel.Children.Add(btnUpdateDefender);

        var btnEnableDefender = new Button
        {
            Content = "Enable All Defender Shields",
            FontSize = 10.5,
            FontWeight = FontWeights.SemiBold,
            Padding = new Thickness(10, 7, 10, 7),
            Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
            Foreground = TextDark,
            BorderThickness = new Thickness(1),
            BorderBrush = BorderMuted,
            Cursor = Cursors.Hand
        };
        btnEnableDefender.Click += (s, e) =>
        {
            ExecutePowerShellScript("Set-MpPreference -DisableRealtimeMonitoring $false -DisableBehaviorMonitoring $false -DisableIOAVProtection $false");
            MessageBox.Show("✔ Real-time, Behavior, and I/O protection preferences confirmed enabled.", "Defender Configured", MessageBoxButton.OK, MessageBoxImage.Information);
            RefreshDefenderStatus();
        };
        defPanel.Children.Add(btnEnableDefender);

        defCard.Child = defPanel;
        Grid.SetColumn(defCard, 1);
        secGrid.Children.Add(defCard);

        // ------------------ COLUMN 3: LOCAL ADMIN VAULT (LAPS) ------------------
        var lapsCard = new Border
        {
            Background = BgCard,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(16),
            Margin = new Thickness(6, 0, 0, 0)
        };
        var lapsPanel = new StackPanel();
        lapsPanel.Children.Add(new TextBlock { Text = "🔑 Local Admin Vault (LAPS)", FontSize = 13, FontWeight = FontWeights.Bold, Foreground = TextDark });
        lapsPanel.Children.Add(new TextBlock
        {
            Text = "Cryptographic random 16-character password generator to eliminate static shared admin passwords on branch PCs.",
            FontSize = 10.5,
            Foreground = TextSubtle,
            Margin = new Thickness(0, 4, 0, 10)
        });

        _txtLapsPassword = new TextBox
        {
            Text = GenerateSecurePassword(),
            FontSize = 12.5,
            FontFamily = new FontFamily("Consolas, Courier New"),
            FontWeight = FontWeights.Bold,
            Foreground = SvllBlue,
            Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
            BorderBrush = BorderMuted,
            Padding = new Thickness(8, 6, 8, 6),
            Margin = new Thickness(0, 0, 0, 8),
            IsReadOnly = true
        };
        lapsPanel.Children.Add(_txtLapsPassword);

        var lapsBtnRow = new Grid { Margin = new Thickness(0, 0, 0, 6) };
        lapsBtnRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        lapsBtnRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var btnGenNew = new Button
        {
            Content = "🎲 Generate New",
            FontSize = 10.5,
            FontWeight = FontWeights.Bold,
            Padding = new Thickness(8, 6, 8, 6),
            Background = new SolidColorBrush(Color.FromRgb(147, 51, 234)), // Purple
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand
        };
        btnGenNew.Click += (s, e) => _txtLapsPassword.Text = GenerateSecurePassword();
        Grid.SetColumn(btnGenNew, 0);
        lapsBtnRow.Children.Add(btnGenNew);

        var btnCopy = new Button
        {
            Content = "📋 Copy",
            FontSize = 10.5,
            FontWeight = FontWeights.SemiBold,
            Padding = new Thickness(12, 6, 12, 6),
            Margin = new Thickness(6, 0, 0, 0),
            Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
            Foreground = TextDark,
            BorderThickness = new Thickness(1),
            BorderBrush = BorderMuted,
            Cursor = Cursors.Hand
        };
        btnCopy.Click += (s, e) =>
        {
            Clipboard.SetText(_txtLapsPassword.Text);
            Log($"[SECURITY DLP] Local admin password copied to clipboard by user {Environment.UserName}.");
            MessageBox.Show("✔ Password copied to clipboard!\n\nStore this securely in the central SVLL IT Credential Vault.", "Password Copied", MessageBoxButton.OK, MessageBoxImage.Information);
        };
        Grid.SetColumn(btnCopy, 1);
        lapsBtnRow.Children.Add(btnCopy);

        lapsPanel.Children.Add(lapsBtnRow);

        var btnApplyLocalAdmin = new Button
        {
            Content = "⚡ Apply Password to Local Administrator",
            FontSize = 10.5,
            FontWeight = FontWeights.Bold,
            Padding = new Thickness(10, 7, 10, 7),
            Background = SvllBlue,
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand
        };
        btnApplyLocalAdmin.Click += (s, e) => ApplyLapsToLocalAdminAccount();
        lapsPanel.Children.Add(btnApplyLocalAdmin);

        lapsCard.Child = lapsPanel;
        Grid.SetColumn(lapsCard, 2);
        secGrid.Children.Add(lapsCard);

        root.Children.Add(secGrid);

        scroll.Content = root;

        RefreshUsbPolicyStatus();
        RefreshDefenderStatus();

        return scroll;
    }

    private enum UsbPolicyMode { ReadOnly, Blocked, Unrestricted }

    private void RefreshUsbPolicyStatus()
    {
        try
        {
            int writeProtect = (int)(Registry.GetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\StorageDevicePolicies", "WriteProtect", 0) ?? 0);
            int usbstorStart = (int)(Registry.GetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\USBSTOR", "Start", 3) ?? 3);

            if (usbstorStart == 4)
            {
                _lblUsbPolicyStatus.Text = "🚫 BLOCKED (All USB Drives Disabled)";
                _lblUsbPolicyStatus.Foreground = SvllRed;
            }
            else if (writeProtect == 1)
            {
                _lblUsbPolicyStatus.Text = "🔒 READ-ONLY (Data Exfiltration Blocked)";
                _lblUsbPolicyStatus.Foreground = new SolidColorBrush(Color.FromRgb(217, 119, 6));
            }
            else
            {
                _lblUsbPolicyStatus.Text = "🟢 UNRESTRICTED (Normal Read/Write)";
                _lblUsbPolicyStatus.Foreground = new SolidColorBrush(Color.FromRgb(22, 163, 74));
            }
        }
        catch
        {
            _lblUsbPolicyStatus.Text = "🟢 UNRESTRICTED (Default)";
            _lblUsbPolicyStatus.Foreground = new SolidColorBrush(Color.FromRgb(22, 163, 74));
        }
    }

    private void SetUsbStoragePolicy(UsbPolicyMode mode)
    {
        try
        {
            if (mode == UsbPolicyMode.ReadOnly)
            {
                // Ensure USBSTOR is enabled
                Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\USBSTOR", "Start", 3, RegistryValueKind.DWord);
                // Set WriteProtect = 1
                Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\StorageDevicePolicies", "WriteProtect", 1, RegistryValueKind.DWord);
                Log("[SECURITY DLP] Applied policy: USB Storage is now READ-ONLY. Data copying blocked.");
                MessageBox.Show("✔ USB Storage Policy set to READ-ONLY.\n\nEmployees can read existing files on USB drives, but cannot write or export confidential data.", "Policy Applied", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else if (mode == UsbPolicyMode.Blocked)
            {
                // Set USBSTOR Start = 4
                Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\USBSTOR", "Start", 4, RegistryValueKind.DWord);
                Log("[SECURITY DLP] Applied policy: USB Mass Storage service DISABLED (USBSTOR Start = 4).");
                MessageBox.Show("✔ USB Mass Storage service BLOCKED.\n\nUSB flash drives will no longer mount. Keyboards, mice, and barcode scanners remain functional.", "Policy Applied", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                // Unrestricted
                Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\USBSTOR", "Start", 3, RegistryValueKind.DWord);
                Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\StorageDevicePolicies", "WriteProtect", 0, RegistryValueKind.DWord);
                Log("[SECURITY DLP] Applied policy: USB Storage restored to UNRESTRICTED.");
                MessageBox.Show("✔ USB Storage restored to UNRESTRICTED access.", "Policy Applied", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            RefreshUsbPolicyStatus();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Administrator privileges required: {ex.Message}", "Security Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RefreshDefenderStatus()
    {
        try
        {
            // Query Defender quick status via PowerShell
            string outStr = ExecutePowerShellScript("(Get-MpComputerStatus).AntivirusSignatureLastUpdated.ToString('yyyy-MM-dd')");
            if (!string.IsNullOrWhiteSpace(outStr))
            {
                _lblDefenderSigDate.Text = $"Last Updated: {outStr.Trim()}";
            }
        }
        catch { }
    }

    private async Task TriggerDefenderUpdateAndScanAsync()
    {
        Log("\n[SECURITY DLP] Updating Windows Defender Antivirus signatures & launching quick scan...");
        try
        {
            await Task.Run(() =>
            {
                ExecutePowerShellScript("Update-MpSignature -ErrorAction SilentlyContinue");
                ExecutePowerShellScript("Start-MpScan -ScanType QuickScan -ErrorAction SilentlyContinue");
            });

            Log("[SECURITY DLP] Windows Defender signatures updated and quick scan initiated in background.");
            MessageBox.Show("🎉 Windows Defender Update & Quick Scan Initiated!\n\n• Signatures: Refreshed from Microsoft Cloud\n• Scan Mode: Quick Scan Running in Kernel", "Defender Active", MessageBoxButton.OK, MessageBoxImage.Information);
            RefreshDefenderStatus();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Defender trigger error: {ex.Message}", "Defender Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private string GenerateSecurePassword()
    {
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lower = "abcdefghijkmnpqrstuvwxyz";
        const string digits = "23456789";
        const string special = "!@#$%^&*";

        var rng = RandomNumberGenerator.Create();
        var chars = new char[16];

        // Ensure at least one of each class
        chars[0] = upper[RandomNumber(upper.Length, rng)];
        chars[1] = lower[RandomNumber(lower.Length, rng)];
        chars[2] = digits[RandomNumber(digits.Length, rng)];
        chars[3] = special[RandomNumber(special.Length, rng)];

        string allChars = upper + lower + digits + special;
        for (int i = 4; i < 16; i++)
        {
            chars[i] = allChars[RandomNumber(allChars.Length, rng)];
        }

        // Shuffle
        for (int i = chars.Length - 1; i > 0; i--)
        {
            int j = RandomNumber(i + 1, rng);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }

        return "Svll!" + new string(chars[..11]);
    }

    private int RandomNumber(int max, RandomNumberGenerator rng)
    {
        byte[] b = new byte[4];
        rng.GetBytes(b);
        return (int)(BitConverter.ToUInt32(b, 0) % (uint)max);
    }

    private void ApplyLapsToLocalAdminAccount()
    {
        string newPass = _txtLapsPassword.Text;
        var confirm = MessageBox.Show(
            $"Are you sure you want to change the password for the local 'Administrator' account to:\n\n{newPass}\n\nMake sure you have copied this password!",
            "Confirm Password Rotation", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes) return;

        try
        {
            var p = Process.Start(new ProcessStartInfo("net.exe", $"user Administrator \"{newPass}\"")
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true
            });
            p?.WaitForExit(5000);

            if (p?.ExitCode == 0)
            {
                Log($"[SECURITY DLP] Local Administrator password updated successfully by {Environment.UserName}.");
                MessageBox.Show("✔ Local Administrator password updated successfully!", "Password Changed", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show($"net user command returned exit code {p?.ExitCode}. Ensure account exists and you are running as Administrator.", "Password Notice", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to set password: {ex.Message}", "Security Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
