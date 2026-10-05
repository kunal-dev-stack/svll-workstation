using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SVLL_IT_Workstation;

public class ProfileEditorDialog : Window
{
	private readonly SolidColorBrush SvllBlue = new SolidColorBrush(Color.FromRgb(26, 75, 178));

	private readonly SolidColorBrush BorderMuted = new SolidColorBrush(Color.FromRgb(226, 232, 240));

	private TextBox _txtName = null!;
	private TextBox _txtBranch = null!;
	private TextBox _txtIp = null!;
	private TextBox _txtGw = null!;
	private TextBox _txtDns1 = null!;
	private TextBox _txtDns2 = null!;
	private TextBox _txtNotes = null!;
	private ComboBox _cmbSubnet = null!;
	private RadioButton _rbDhcp = null!;
	private RadioButton _rbStatic = null!;
	private StackPanel _staticPanel = null!;

	public NetworkProfile ResultProfile { get; private set; }

	public ProfileEditorDialog(NetworkProfile? existing = null)
	{
		Title = ((existing == null) ? "Create Branch IP Profile" : "Edit Branch IP Profile");
		Width = 500.0;
		Height = 600.0;
		WindowStartupLocation = WindowStartupLocation.CenterOwner;
		Background = new SolidColorBrush(Color.FromRgb(248, 250, 252));
		ResizeMode = ResizeMode.NoResize;
		ResultProfile = ((existing != null) ? new NetworkProfile
		{
			ProfileName = existing.ProfileName,
			BranchTag = existing.BranchTag,
			IsDhcp = existing.IsDhcp,
			IpAddress = existing.IpAddress,
			SubnetMask = existing.SubnetMask,
			Gateway = existing.Gateway,
			PrimaryDns = existing.PrimaryDns,
			SecondaryDns = existing.SecondaryDns,
			Notes = existing.Notes
		} : new NetworkProfile());
		BuildUI();
		LoadProfile(ResultProfile);
	}

	private void BuildUI()
	{
		Grid grid = new Grid
		{
			Margin = new Thickness(16.0)
		};
		grid.RowDefinitions.Add(new RowDefinition
		{
			Height = GridLength.Auto
		});
		grid.RowDefinitions.Add(new RowDefinition
		{
			Height = new GridLength(1.0, GridUnitType.Star)
		});
		grid.RowDefinitions.Add(new RowDefinition
		{
			Height = GridLength.Auto
		});
		StackPanel stackPanel = new StackPanel
		{
			Margin = new Thickness(0.0, 0.0, 0.0, 12.0)
		};
		stackPanel.Children.Add(new TextBlock
		{
			Text = Title,
			FontSize = 16.0,
			FontWeight = FontWeights.Bold,
			Foreground = SvllBlue
		});
		Grid.SetRow(stackPanel, 0);
		grid.Children.Add(stackPanel);
		ScrollViewer scrollViewer = new ScrollViewer
		{
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto
		};
		StackPanel stackPanel2 = new StackPanel();
		_txtName = AddField(stackPanel2, "Profile Name (e.g. Raipur Logistics Hub):");
		_txtBranch = AddField(stackPanel2, "Branch / Department Tag (e.g. Warehouse 1, Accounts):");
		Border border = new Border
		{
			Background = Brushes.White,
			BorderBrush = BorderMuted,
			BorderThickness = new Thickness(1.0),
			CornerRadius = new CornerRadius(6.0),
			Padding = new Thickness(10.0),
			Margin = new Thickness(0.0, 6.0, 0.0, 10.0)
		};
		StackPanel stackPanel3 = new StackPanel();
		_rbDhcp = new RadioButton
		{
			Content = "DHCP Mode (Automatic Lease)",
			GroupName = "M",
			IsChecked = true,
			Margin = new Thickness(0.0, 0.0, 0.0, 4.0)
		};
		_rbStatic = new RadioButton
		{
			Content = "Custom Static IP Parameters",
			GroupName = "M"
		};
		_rbDhcp.Checked += (object _, RoutedEventArgs _) =>
		{
			_staticPanel.IsEnabled = false;
		};
		_rbStatic.Checked += (object _, RoutedEventArgs _) =>
		{
			_staticPanel.IsEnabled = true;
		};
		stackPanel3.Children.Add(_rbDhcp);
		stackPanel3.Children.Add(_rbStatic);
		border.Child = stackPanel3;
		stackPanel2.Children.Add(border);
		_staticPanel = new StackPanel
		{
			IsEnabled = false
		};
		_txtIp = AddField(_staticPanel, "Static IP Address:");
		TextBlock element = new TextBlock
		{
			Text = "Subnet Mask:",
			FontWeight = FontWeights.Medium,
			FontSize = 11.5,
			Margin = new Thickness(0.0, 2.0, 0.0, 2.0)
		};
		_cmbSubnet = new ComboBox
		{
			Height = 28.0,
			IsEditable = true,
			Margin = new Thickness(0.0, 0.0, 0.0, 6.0)
		};
		_cmbSubnet.Items.Add("255.255.255.0");
		_cmbSubnet.Items.Add("255.255.0.0");
		_cmbSubnet.Items.Add("255.255.255.128");
		_cmbSubnet.SelectedIndex = 0;
		_staticPanel.Children.Add(element);
		_staticPanel.Children.Add(_cmbSubnet);
		_txtGw = AddField(_staticPanel, "Default Gateway:");
		_txtDns1 = AddField(_staticPanel, "Preferred DNS Server:");
		_txtDns2 = AddField(_staticPanel, "Alternate DNS Server:");
		stackPanel2.Children.Add(_staticPanel);
		_txtNotes = AddField(stackPanel2, "Notes / Description:");
		scrollViewer.Content = stackPanel2;
		Grid.SetRow(scrollViewer, 1);
		grid.Children.Add(scrollViewer);
		StackPanel stackPanel4 = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			HorizontalAlignment = HorizontalAlignment.Right,
			Margin = new Thickness(0.0, 10.0, 0.0, 0.0)
		};
		Button button = new Button
		{
			Content = "Save Profile",
			Width = 110.0,
			Height = 32.0,
			Background = SvllBlue,
			Foreground = Brushes.White,
			FontWeight = FontWeights.SemiBold,
			Margin = new Thickness(0.0, 0.0, 6.0, 0.0),
			Cursor = Cursors.Hand
		};
		button.Click += (object _, RoutedEventArgs _) =>
		{
			if (string.IsNullOrWhiteSpace(_txtName.Text))
			{
				MessageBox.Show("Please enter a profile name.");
			}
			else
			{
				ResultProfile.ProfileName = _txtName.Text.Trim();
				ResultProfile.BranchTag = _txtBranch.Text.Trim();
				ResultProfile.IsDhcp = _rbDhcp.IsChecked == true;
				ResultProfile.IpAddress = _txtIp.Text.Trim();
				ResultProfile.SubnetMask = _cmbSubnet.Text.Trim();
				ResultProfile.Gateway = _txtGw.Text.Trim();
				ResultProfile.PrimaryDns = _txtDns1.Text.Trim();
				ResultProfile.SecondaryDns = _txtDns2.Text.Trim();
				ResultProfile.Notes = _txtNotes.Text.Trim();
				DialogResult = true;
				Close();
			}
		};
		Button button2 = new Button
		{
			Content = "Cancel",
			Width = 80.0,
			Height = 32.0,
			Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
			Cursor = Cursors.Hand
		};
		button2.Click += (object _, RoutedEventArgs _) =>
		{
			DialogResult = false;
			Close();
		};
		stackPanel4.Children.Add(button);
		stackPanel4.Children.Add(button2);
		Grid.SetRow(stackPanel4, 2);
		grid.Children.Add(stackPanel4);
		Content = grid;
	}

	private TextBox AddField(Panel p, string label)
	{
		TextBlock element = new TextBlock
		{
			Text = label,
			FontWeight = FontWeights.Medium,
			FontSize = 11.5,
			Margin = new Thickness(0.0, 2.0, 0.0, 2.0)
		};
		TextBox textBox = new TextBox
		{
			Height = 28.0,
			VerticalContentAlignment = VerticalAlignment.Center,
			Padding = new Thickness(4.0, 0.0, 4.0, 0.0),
			Margin = new Thickness(0.0, 0.0, 0.0, 6.0),
			BorderBrush = BorderMuted
		};
		p.Children.Add(element);
		p.Children.Add(textBox);
		return textBox;
	}

	private void LoadProfile(NetworkProfile p)
	{
		_txtName.Text = p.ProfileName;
		_txtBranch.Text = p.BranchTag;
		_rbDhcp.IsChecked = p.IsDhcp;
		_rbStatic.IsChecked = !p.IsDhcp;
		_staticPanel.IsEnabled = !p.IsDhcp;
		_txtIp.Text = p.IpAddress;
		_cmbSubnet.Text = p.SubnetMask;
		_txtGw.Text = p.Gateway;
		_txtDns1.Text = p.PrimaryDns;
		_txtDns2.Text = p.SecondaryDns;
		_txtNotes.Text = p.Notes;
	}
}
