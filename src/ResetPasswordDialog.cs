using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SVLL_IT_Workstation;

public class ResetPasswordDialog : Window
{
    private readonly SolidColorBrush SvllBlue = new SolidColorBrush(Color.FromRgb(26, 75, 178));
    private readonly SolidColorBrush BorderMuted = new SolidColorBrush(Color.FromRgb(226, 232, 240));

    private PasswordBox _txtNewPass;
    private PasswordBox _txtConfirmPass;

    public string TargetUsername { get; }
    public string? NewPassword { get; private set; }

    public ResetPasswordDialog(string username)
    {
        TargetUsername = username;
        Title = $"Reset Password - {username}";
        Width = 420;
        Height = 310;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = new SolidColorBrush(Color.FromRgb(248, 250, 252));
        ResizeMode = ResizeMode.NoResize;

        var mainPanel = new StackPanel { Margin = new Thickness(20) };

        var lblHeader = new TextBlock
        {
            Text = $"Reset Local Password for [{username}]",
            FontSize = 15,
            FontWeight = FontWeights.Bold,
            Foreground = SvllBlue,
            Margin = new Thickness(0, 0, 0, 8)
        };
        mainPanel.Children.Add(lblHeader);

        var lblInfo = new TextBlock
        {
            Text = "Administrative override will set this password immediately without prompting for old credentials.",
            FontSize = 11,
            Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139)),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 16)
        };
        mainPanel.Children.Add(lblInfo);

        mainPanel.Children.Add(new TextBlock { Text = "New Password:", FontSize = 12, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 4) });
        _txtNewPass = new PasswordBox
        {
            Height = 30,
            Padding = new Thickness(6, 4, 6, 4),
            BorderBrush = BorderMuted,
            Margin = new Thickness(0, 0, 0, 12)
        };
        mainPanel.Children.Add(_txtNewPass);

        mainPanel.Children.Add(new TextBlock { Text = "Confirm New Password:", FontSize = 12, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 4) });
        _txtConfirmPass = new PasswordBox
        {
            Height = 30,
            Padding = new Thickness(6, 4, 6, 4),
            BorderBrush = BorderMuted,
            Margin = new Thickness(0, 0, 0, 18)
        };
        mainPanel.Children.Add(_txtConfirmPass);

        var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };

        var btnCancel = new Button
        {
            Content = "Cancel",
            Width = 90,
            Height = 32,
            Margin = new Thickness(0, 0, 10, 0),
            Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
            BorderBrush = BorderMuted
        };
        btnCancel.Click += (s, e) => { DialogResult = false; Close(); };

        var btnSave = new Button
        {
            Content = "Reset Password",
            Width = 120,
            Height = 32,
            Background = SvllBlue,
            Foreground = Brushes.White,
            FontWeight = FontWeights.SemiBold
        };
        btnSave.Click += (s, e) =>
        {
            if (string.IsNullOrEmpty(_txtNewPass.Password))
            {
                MessageBox.Show("Password cannot be blank.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (_txtNewPass.Password != _txtConfirmPass.Password)
            {
                MessageBox.Show("Passwords do not match.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            NewPassword = _txtNewPass.Password;
            DialogResult = true;
            Close();
        };

        btnPanel.Children.Add(btnCancel);
        btnPanel.Children.Add(btnSave);
        mainPanel.Children.Add(btnPanel);

        Content = mainPanel;
    }
}
