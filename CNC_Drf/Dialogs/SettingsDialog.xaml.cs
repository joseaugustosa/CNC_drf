namespace CNC_Drf.Dialogs;

public partial class SettingsDialog : Window
{
    public SettingsDialog()
    {
        InitializeComponent();
        LoadAxes();
        LoadMachineSize();
        LoadTools();
        LoadInputPorts();
        LoadOutputPorts();
        LoadDiagnostics();
        RefreshDevices();
    }

    // ── Navigation helpers ────────────────────────────────────────────────
    private void BtnNavDevice_Click(object s, RoutedEventArgs e)  => MainTabControl.SelectedIndex = 1;
    private void BtnNavAxes_Click(object s, RoutedEventArgs e)    => MainTabControl.SelectedIndex = 2;
    private void BtnNavInput_Click(object s, RoutedEventArgs e)   => MainTabControl.SelectedIndex = 3;
    private void BtnNavOutput_Click(object s, RoutedEventArgs e)  => MainTabControl.SelectedIndex = 4;
    private void BtnNavSpindle_Click(object s, RoutedEventArgs e) => MainTabControl.SelectedIndex = 5;
    private void BtnNavMachine_Click(object s, RoutedEventArgs e) => MainTabControl.SelectedIndex = 6;
    private void BtnNavTools_Click(object s, RoutedEventArgs e)   => MainTabControl.SelectedIndex = 7;

    // ── Data loading ──────────────────────────────────────────────────────
    private static List<AxisSettings> BuildAxes() => new()
    {
        new() { Name="X", Color="#e53935", Impulses=320, Speed=1500, Accel=20, HomeOrder=3, HomeToMax=true  },
        new() { Name="Y", Color="#fdd835", Impulses=320, Speed=1500, Accel=20, HomeOrder=2, HomeToMax=true  },
        new() { Name="Z", Color="#1e88e5", Impulses=320, Speed=1500, Accel=20, HomeOrder=1, HomeToMin=true, HomeToMax=false, AxMin=-100, AxMax=0 },
        new() { Name="A", Color="#ab47bc", Impulses=320, Speed=1500, Accel=20, HomeOrder=4, HomeToMax=true, Enabled=false },
        new() { Name="B", Color="#26a69a", Impulses=320, Speed=1500, Accel=20, HomeOrder=5, HomeToMax=true, Enabled=false },
        new() { Name="C", Color="#ff7043", Impulses=320, Speed=1500, Accel=20, HomeOrder=6, HomeToMax=true, Enabled=false },
    };

    private void LoadAxes()   => AxesPanel.ItemsSource     = BuildAxes();
    private void LoadMachineSize() => MachineSizePanel.ItemsSource = BuildAxes();

    private void LoadTools()
    {
        ToolsPanel.ItemsSource = new List<ToolEntry>
        {
            new() { Number=1 },
            new() { Number=2 },
            new() { Number=3 },
        };
    }

    private void LoadInputPorts()
    {
        var groups = new List<(string GroupName, List<(string Name, bool HasHotKey)> Items)>
        {
            ("Main Input Ports",
                [("Emergency stop",false),("Smooth stop",false),("Pause",false),("Start",false),("Probe",false)]),
            ("X axis",
                [("Limit X+",false),("Limit X-",false),("Home X",false)]),
            ("Y axis",
                [("Limit Y+",false),("Limit Y-",false),("Home Y",false)]),
            ("Z axis",
                [("Limit Z+",false),("Limit Z-",false),("Home Z",false)]),
            ("A axis",
                [("Limit A+",false),("Limit A-",false),("Home A",false)]),
            ("B axis",
                [("Limit B+",false),("Limit B-",false),("Home B",false)]),
            ("C axis",
                [("Limit C+",false),("Limit C-",false),("Home C",false)]),
            ("Hand Jog",
                [("Jog Speed +100",true),("Jog Speed -100",true),("Set X1 speed",true),("Set X10 speed",true),("Set X100 speed",true),
                 ("Start Tool Zero",true),("Stop Tool Zero",true),
                 ("Jog X++",true),("Jog X--",true),("Jog Y++",true),("Jog Y--",true),
                 ("Jog Z++",true),("Jog Z--",true),("Jog A++",true),("Jog A--",true),
                 ("Jog B++",true),("Jog B--",true),("Jog C++",true),("Jog C--",true)]),
            ("Spindle",
                [("On/Off Spindle",true),("Turn on Spindle",true),("Turn off Spindle",true),
                 ("Spindle CW",true),("Spindle CCW",true),("Spindle Speed +10%",true),("Spindle Speed -10%",true),
                 ("Turn on Cooling",true),("Turn on Additional Cooling",true),("Turn off Cooling",true)]),
            ("Jog select axis and move",
                [("Jog++",true),("Jog--",true),
                 ("Select X axis",true),("Select Y axis",true),("Select Z axis",true),
                 ("Select A axis",true),("Select B axis",true),("Select C axis",true)]),
            ("Probes",
                [("Probe 1",false),("Probe 2",false),("Probe 3",false),("Probe 4",false),
                 ("Probe 5",false),("Probe 6",false),("Probe 7",false)]),
        };

        foreach (var (groupName, items) in groups)
        {
            var groupItem = new TreeViewItem
            {
                IsExpanded = true,
                Header = BuildGroupHeader(groupName)
            };
            foreach (var (name, hasHotKey) in items)
                groupItem.Items.Add(BuildPortItem(name, hasHotKey));
            InputPortsTree.Items.Add(groupItem);
        }
    }

    private void LoadOutputPorts()
    {
        var groups = new List<(string GroupName, List<string> Items)>
        {
            ("Spindle Output Ports M3, M4, M5",
                ["Turn the spindle","Spindle CW clockwise","Spindle CCW counterclockwise"]),
            ("Cooling Output Ports M7, M8, M9",
                ["Cooling","Additional cooling"]),
            ("Output Control M62, M63, M64, M65",
                Enumerable.Range(0, 13).Select(i => $"Output port {i}").ToList()),
            ("Tool park control",
                ["Turn on before park tool","Turn off before park tool","Turn on after park tool","Turn off after park tool"]),
            ("Tool change control",
                ["Turn on before change tool","Turn off before change tool","Turn on after change tool","Turn off after change tool"]),
            ("Enable axles Output Ports",
                ["Enable X axle","Enable Y axle","Enable Z axle","Enable A axle","Enable B axle","Enable C axle"]),
        };

        foreach (var (groupName, items) in groups)
        {
            var groupItem = new TreeViewItem
            {
                IsExpanded = true,
                Header = BuildGroupHeader(groupName)
            };
            foreach (var name in items)
                groupItem.Items.Add(BuildOutputItem(name));
            OutputPortsTree.Items.Add(groupItem);
        }
    }

    private static StackPanel BuildGroupHeader(string name)
    {
        var sp = new StackPanel { Orientation = Orientation.Horizontal };
        sp.Children.Add(new TextBlock
        {
            Text = name, FontStyle = FontStyles.Italic,
            Foreground = new SolidColorBrush(Color.FromRgb(0x77, 0x77, 0x77)),
            FontSize = 11, VerticalAlignment = VerticalAlignment.Center
        });
        return sp;
    }

    private static TreeViewItem BuildPortItem(string name, bool hasHotKey)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(185) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(65) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(75) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var chk = new CheckBox { VerticalAlignment = VerticalAlignment.Center };
        var nameTb = new TextBlock { Text = name, FontSize = 11, FontStyle = FontStyles.Italic,
            Foreground = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33)) };
        var namePanel = new StackPanel { Orientation = Orientation.Horizontal };
        namePanel.Children.Add(chk);
        namePanel.Children.Add(nameTb);
        Grid.SetColumn(namePanel, 0);

        var portTb = new TextBox { Text = "0", Width = 45, Height = 20, FontSize = 11,
            Background = Brushes.White, BorderBrush = new SolidColorBrush(Color.FromRgb(0xaa, 0xaa, 0xaa)) };
        Grid.SetColumn(portTb, 1);

        var invertChk = new CheckBox { VerticalAlignment = VerticalAlignment.Center };
        var invertPanel = new StackPanel { Orientation = Orientation.Horizontal };
        invertPanel.Children.Add(invertChk);
        invertPanel.Children.Add(new TextBlock { Text = " No", FontSize = 11 });
        Grid.SetColumn(invertPanel, 2);

        var statusBorder = new Border
        {
            Width = 14, Height = 12, Background = new SolidColorBrush(Color.FromRgb(0xe5, 0x39, 0x35)),
            CornerRadius = new CornerRadius(1), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0,0,4,0)
        };
        var statusPanel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        statusPanel.Children.Add(statusBorder);
        statusPanel.Children.Add(new TextBlock { Text = "Inactive", FontSize = 11,
            Foreground = new SolidColorBrush(Color.FromRgb(0xe5, 0x39, 0x35)) });
        Grid.SetColumn(statusPanel, 3);

        grid.Children.Add(namePanel);
        grid.Children.Add(portTb);
        grid.Children.Add(invertPanel);
        grid.Children.Add(statusPanel);

        if (hasHotKey)
        {
            var hkPanel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            hkPanel.Children.Add(new CheckBox { VerticalAlignment = VerticalAlignment.Center });
            hkPanel.Children.Add(new TextBlock { Text = " None", FontSize = 11 });
            Grid.SetColumn(hkPanel, 4);
            grid.Children.Add(hkPanel);
        }

        return new TreeViewItem { Header = grid, Padding = new Thickness(0), BorderThickness = new Thickness(0) };
    }

    private static TreeViewItem BuildOutputItem(string name)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(185) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(65) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var chk = new CheckBox { VerticalAlignment = VerticalAlignment.Center };
        var nameTb = new TextBlock { Text = name, FontSize = 11, FontStyle = FontStyles.Italic,
            Foreground = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33)) };
        var namePanel = new StackPanel { Orientation = Orientation.Horizontal };
        namePanel.Children.Add(chk);
        namePanel.Children.Add(nameTb);
        Grid.SetColumn(namePanel, 0);

        var portTb = new TextBox { Text = "0", Width = 45, Height = 20, FontSize = 11,
            Background = Brushes.White, BorderBrush = new SolidColorBrush(Color.FromRgb(0xaa, 0xaa, 0xaa)) };
        Grid.SetColumn(portTb, 1);

        var invertChk = new CheckBox { VerticalAlignment = VerticalAlignment.Center };
        var invertPanel = new StackPanel { Orientation = Orientation.Horizontal };
        invertPanel.Children.Add(invertChk);
        invertPanel.Children.Add(new TextBlock { Text = " No", FontSize = 11 });
        Grid.SetColumn(invertPanel, 2);

        var statusBorder = new Border
        {
            Width = 14, Height = 12, Background = new SolidColorBrush(Color.FromRgb(0xe5, 0x39, 0x35)),
            CornerRadius = new CornerRadius(1), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0,0,4,0)
        };
        var statusPanel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        statusPanel.Children.Add(statusBorder);
        statusPanel.Children.Add(new TextBlock { Text = "Inactive", FontSize = 11,
            Foreground = new SolidColorBrush(Color.FromRgb(0xe5, 0x39, 0x35)) });
        Grid.SetColumn(statusPanel, 3);

        grid.Children.Add(namePanel);
        grid.Children.Add(portTb);
        grid.Children.Add(invertPanel);
        grid.Children.Add(statusPanel);

        return new TreeViewItem { Header = grid, Padding = new Thickness(0), BorderThickness = new Thickness(0) };
    }

    private void LoadDiagnostics()
    {
        DiagnosticsGrid.ItemsSource = Enumerable.Range(0, 32).Select(i => new
        {
            Label = $"In{i}",
            Color = new SolidColorBrush(Color.FromRgb(0xe5, 0x39, 0x35))
        }).ToList();
    }

    private void RefreshDevices()
    {
        var ports = SerialPort.GetPortNames();
        DeviceGrid.ItemsSource = ports.Select(p => new DeviceEntry
        {
            Name = p, Manufacturer = "USB Serial", VID = "1386",
            PID = "20982", Version = "2", Outputs = "0", Inputs = "6"
        }).ToList();
    }

    private void BtnRefreshDevices_Click(object s, RoutedEventArgs e) => RefreshDevices();
    private void BtnApply_Click(object s, RoutedEventArgs e) { /* apply without close */ }
    private void BtnSave_Click(object s, RoutedEventArgs e) { DialogResult = true; Close(); }
}
