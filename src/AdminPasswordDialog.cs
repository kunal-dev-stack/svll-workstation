using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SVLL_IT_Workstation;

public class AdminPasswordDialog : Window
{
    private static readonly SolidColorBrush SvllBlue = new SolidColorBrush(Color.FromRgb(26, 75, 178));
    private static readonly SolidColorBrush SvllRed = new SolidColorBrush(Color.FromRgb(225, 45, 45));
    private static readonly SolidColorBrush BorderMuted = new SolidColorBrush(Color.FromRgb(226, 232, 240));
    private static readonly SolidColorBrush BgCard = new SolidColorBrush(Color.FromRgb(255, 255, 255));
    private static readonly SolidColorBrush TextDark = new SolidColorBrush(Color.FromRgb(15, 23, 42));
    private static readonly SolidColorBrush TextSubtle = new SolidColorBrush(Color.FromRgb(100, 116, 139));

    private PasswordBox _txtPassword = null!;
    private TextBlock _lblError = null!;

    public bool IsAuthenticated { get; private set; } = false;

    public AdminPasswordDialog(string promptContext = "Access to this administrative console is restricted to authorized IT personnel.")
    {
        Title = "IT Administrator Authentication";
        Width = 460;
        Height = 340;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = new SolidColorBrush(Color.FromRgb(248, 250, 252));
        ResizeMode = ResizeMode.NoResize;

        var outerBorder = new Border
        {
            Padding = new Thickness(24)
        };

        var sp = new StackPanel();

        // Header with emblem
        var headerRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 16) };
        var iconBadge = new Border
        {
            Width = 44,
            Height = 44,
            CornerRadius = new CornerRadius(10),
            Background = new SolidColorBrush(Color.FromRgb(239, 246, 255)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(191, 219, 254)),
            BorderThickness = new Thickness(1),
            Margin = new Thickness(0, 0, 14, 0),
            Child = new TextBlock
            {
                Text = "👑",
                FontSize = 22,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
        headerRow.Children.Add(iconBadge);

        var titleStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        titleStack.Children.Add(new TextBlock
        {
            Text = "ADMIN MASTER CONSOLE",
            FontSize = 14,
            FontWeight = FontWeights.ExtraBold,
            Foreground = SvllBlue
        });
        titleStack.Children.Add(new TextBlock
        {
            Text = "Shree Vasu Logistics IT Workstation Security",
            FontSize = 10.5,
            Foreground = TextSubtle,
            Margin = new Thickness(0, 2, 0, 0)
        });
        headerRow.Children.Add(titleStack);
        sp.Children.Add(headerRow);

        // Security description
        var descBox = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(254, 242, 242)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(254, 202, 202)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(10, 8, 10, 8),
            Margin = new Thickness(0, 0, 0, 16),
            Child = new TextBlock
            {
                Text = promptContext,
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(153, 27, 27)),
                TextWrapping = TextWrapping.Wrap
            }
        };
        sp.Children.Add(descBox);

        // Password input
        sp.Children.Add(new TextBlock
        {
            Text = "Enter IT Admin Password:",
            FontSize = 11.5,
            FontWeight = FontWeights.Bold,
            Foreground = TextDark,
            Margin = new Thickness(0, 0, 0, 6)
        });

        _txtPassword = new PasswordBox
        {
            Height = 34,
            Padding = new Thickness(8, 6, 8, 6),
            FontSize = 13,
            Background = BgCard,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1.5),
            Margin = new Thickness(0, 0, 0, 6)
        };
        _txtPassword.KeyDown += (s, e) =>
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                AttemptUnlock();
            }
        };
        sp.Children.Add(_txtPassword);

        _lblError = new TextBlock
        {
            Text = "",
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Foreground = SvllRed,
            Margin = new Thickness(0, 0, 0, 14),
            Visibility = Visibility.Collapsed
        };
        sp.Children.Add(_lblError);

        // Button row
        var btnGrid = new Grid();
        btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var defaultHint = new TextBlock
        {
            Text = "Default: SVLL@Admin2026",
            FontSize = 10,
            Foreground = TextSubtle,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(defaultHint, 0);
        btnGrid.Children.Add(defaultHint);

        var btnCancel = new Button
        {
            Content = "Cancel",
            Width = 85,
            Height = 32,
            Margin = new Thickness(0, 0, 10, 0),
            Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
            BorderBrush = BorderMuted,
            Cursor = System.Windows.Input.Cursors.Hand
        };
        btnCancel.Click += (s, e) => { DialogResult = false; Close(); };
        Grid.SetColumn(btnCancel, 1);
        btnGrid.Children.Add(btnCancel);

        var btnUnlock = new Button
        {
            Content = "👑 Unlock Console",
            Width = 135,
            Height = 32,
            Background = SvllBlue,
            Foreground = Brushes.White,
            FontWeight = FontWeights.Bold,
            BorderThickness = new Thickness(0),
            Cursor = System.Windows.Input.Cursors.Hand
        };
        btnUnlock.Click += (s, e) => AttemptUnlock();
        Grid.SetColumn(btnUnlock, 2);
        btnGrid.Children.Add(btnUnlock);

        sp.Children.Add(btnGrid);
        outerBorder.Child = sp;
        Content = outerBorder;

        Loaded += (s, e) => _txtPassword.Focus();
    }

    private void AttemptUnlock()
    {
        string pass = _txtPassword.Password;
        if (AdminAuthService.Instance.UnlockAdmin(pass))
        {
            IsAuthenticated = true;
            DialogResult = true;
            Close();
        }
        else
        {
            _lblError.Text = "❌ Invalid IT Admin Password. Access Denied.";
            _lblError.Visibility = Visibility.Visible;
            _txtPassword.SelectAll();
            _txtPassword.Focus();
        }
    }
}
