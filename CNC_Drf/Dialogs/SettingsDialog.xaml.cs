namespace CNC_Drf.Dialogs;

public partial class SettingsDialog : Window
{
    public GrblSettings    Grbl       { get; } = new();
    public ControllerType  ActiveControllerType { get; private set; } = ControllerType.Grbl11;

    // tab indices
    private const int TabIdxCommon     = 0;
    private const int TabIdxDevice     = 1;
    private const int TabIdxAxes       = 2;
    private const int TabIdxInput      = 3;
    private const int TabIdxOutput     = 4;
    private const int TabIdxSpindle    = 5;
    private const int TabIdxMachine    = 6;
    private const int TabIdxTools      = 7;
    private const int TabIdxGrbl       = 8;
    private const int TabIdxCtrlType   = 9;
    private const int TabIdxGrblAxes   = 10;

    private bool IsGrblActive => ControllerProfile.Get(ActiveControllerType).IsGrbl;

    // Optional: called by MainWindow after reading $$ from the board
    public Action? RequestGrblRead  { get; set; }
    public Action<IEnumerable<string>>? RequestGrblWrite { get; set; }
    public Action<ControllerType>? ControllerTypeChanged { get; set; }

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
        GrblSettingsToUi();
        LoadControllerCards();
    }

    // ── Navigation helpers ────────────────────────────────────────────────
    private void BtnNavDevice_Click(object s, RoutedEventArgs e)         => MainTabControl.SelectedIndex = TabIdxDevice;
    private void BtnNavAxes_Click(object s, RoutedEventArgs e)           => MainTabControl.SelectedIndex = TabIdxAxes;
    private void BtnNavInput_Click(object s, RoutedEventArgs e)          => MainTabControl.SelectedIndex = TabIdxInput;
    private void BtnNavOutput_Click(object s, RoutedEventArgs e)         => MainTabControl.SelectedIndex = TabIdxOutput;
    private void BtnNavSpindle_Click(object s, RoutedEventArgs e)        => MainTabControl.SelectedIndex = TabIdxSpindle;
    private void BtnNavMachine_Click(object s, RoutedEventArgs e)        => MainTabControl.SelectedIndex = TabIdxMachine;
    private void BtnNavTools_Click(object s, RoutedEventArgs e)          => MainTabControl.SelectedIndex = TabIdxTools;
    private void BtnNavGrbl_Click(object s, RoutedEventArgs e)           => MainTabControl.SelectedIndex = TabIdxGrbl;
    private void BtnNavControllerType_Click(object s, RoutedEventArgs e) => MainTabControl.SelectedIndex = TabIdxCtrlType;
    private void BtnNavGrblAxes_Click(object s, RoutedEventArgs e)       => MainTabControl.SelectedIndex = TabIdxGrblAxes;

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

    // ── GRBL Axes tab ────────────────────────────────────────────────────
    private void BtnGrblAxesRead_Click(object s, RoutedEventArgs e)
    {
        if (RequestGrblRead is null)
        {
            SetGrblAxesStatus("Sem ligação. Ligue primeiro à placa.", error: true);
            return;
        }
        SetGrblAxesStatus("A ler parâmetros da placa ($$)…");
        RequestGrblRead();
    }

    private void BtnGrblAxesWrite_Click(object s, RoutedEventArgs e)
    {
        if (RequestGrblWrite is null)
        {
            SetGrblAxesStatus("Sem ligação. Ligue primeiro à placa.", error: true);
            return;
        }
        GrblAxesUiToSettings();
        // Envia apenas os parâmetros deste tab: $3, $100-102, $110-112, $120-122, $130-132
        var cmds = new[]
        {
            $"$3={Grbl.DirPortInvert}",
            $"$100={Grbl.XStepsPerMm.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)}",
            $"$101={Grbl.YStepsPerMm.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)}",
            $"$102={Grbl.ZStepsPerMm.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)}",
            $"$110={Grbl.XMaxRate.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)}",
            $"$111={Grbl.YMaxRate.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)}",
            $"$112={Grbl.ZMaxRate.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)}",
            $"$120={Grbl.XAccel.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)}",
            $"$121={Grbl.YAccel.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)}",
            $"$122={Grbl.ZAccel.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)}",
            $"$130={Grbl.XMaxTravel.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)}",
            $"$131={Grbl.YMaxTravel.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)}",
            $"$132={Grbl.ZMaxTravel.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)}",
        };
        RequestGrblWrite(cmds);
        SetGrblAxesStatus("Parâmetros gravados na placa com sucesso.", error: false, success: true);
    }

    private void BtnGrblAxesDefaults_Click(object s, RoutedEventArgs e)
    {
        var r = MessageBox.Show("Repor valores padrão dos eixos?",
                    "Repor padrão", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (r != MessageBoxResult.Yes) return;
        var fresh = new GrblSettings();
        Grbl.XStepsPerMm  = fresh.XStepsPerMm;  Grbl.YStepsPerMm  = fresh.YStepsPerMm;  Grbl.ZStepsPerMm  = fresh.ZStepsPerMm;
        Grbl.XMaxRate     = fresh.XMaxRate;      Grbl.YMaxRate     = fresh.YMaxRate;      Grbl.ZMaxRate     = fresh.ZMaxRate;
        Grbl.XAccel       = fresh.XAccel;        Grbl.YAccel       = fresh.YAccel;        Grbl.ZAccel       = fresh.ZAccel;
        Grbl.XMaxTravel   = fresh.XMaxTravel;    Grbl.YMaxTravel   = fresh.YMaxTravel;    Grbl.ZMaxTravel   = fresh.ZMaxTravel;
        Grbl.DirPortInvert = fresh.DirPortInvert;
        GrblAxesSettingsToUi();
        SetGrblAxesStatus("Valores padrão repostos.");
    }

    // Chamado quando a placa responde com $N=V (mesmo handler do tab GRBL geral)
    // Só atualiza se for um parâmetro de eixo relevante
    public void ApplyGrblAxesLine(string line)
    {
        Grbl.ApplyLine(line);
        Dispatcher.Invoke(() =>
        {
            GrblAxesSettingsToUi();
            SetGrblAxesStatus("Valores recebidos da placa.", success: true);
        });
    }

    private void GrblAxesSettingsToUi()
    {
        static string F3(double v) => v.ToString("F3", System.Globalization.CultureInfo.InvariantCulture);
        static string F1(double v) => v.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);

        ChkXDirInvert.IsChecked = Grbl.XDirInvert;
        ChkYDirInvert.IsChecked = Grbl.YDirInvert;
        ChkZDirInvert.IsChecked = Grbl.ZDirInvert;

        TbXSteps.Text   = F3(Grbl.XStepsPerMm);
        TbYSteps.Text   = F3(Grbl.YStepsPerMm);
        TbZSteps.Text   = F3(Grbl.ZStepsPerMm);

        TbXMaxRate.Text = F1(Grbl.XMaxRate);
        TbYMaxRate.Text = F1(Grbl.YMaxRate);
        TbZMaxRate.Text = F1(Grbl.ZMaxRate);

        TbXAccel.Text   = F3(Grbl.XAccel);
        TbYAccel.Text   = F3(Grbl.YAccel);
        TbZAccel.Text   = F3(Grbl.ZAccel);

        TbXTravel.Text  = F3(Grbl.XMaxTravel);
        TbYTravel.Text  = F3(Grbl.YMaxTravel);
        TbZTravel.Text  = F3(Grbl.ZMaxTravel);
    }

    private void GrblAxesUiToSettings()
    {
        static double D(TextBox tb, double def) => double.TryParse(tb.Text,
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out double v) ? v : def;

        Grbl.XDirInvert = ChkXDirInvert.IsChecked == true;
        Grbl.YDirInvert = ChkYDirInvert.IsChecked == true;
        Grbl.ZDirInvert = ChkZDirInvert.IsChecked == true;

        Grbl.XStepsPerMm  = D(TbXSteps,   Grbl.XStepsPerMm);
        Grbl.YStepsPerMm  = D(TbYSteps,   Grbl.YStepsPerMm);
        Grbl.ZStepsPerMm  = D(TbZSteps,   Grbl.ZStepsPerMm);
        Grbl.XMaxRate     = D(TbXMaxRate, Grbl.XMaxRate);
        Grbl.YMaxRate     = D(TbYMaxRate, Grbl.YMaxRate);
        Grbl.ZMaxRate     = D(TbZMaxRate, Grbl.ZMaxRate);
        Grbl.XAccel       = D(TbXAccel,   Grbl.XAccel);
        Grbl.YAccel       = D(TbYAccel,   Grbl.YAccel);
        Grbl.ZAccel       = D(TbZAccel,   Grbl.ZAccel);
        Grbl.XMaxTravel   = D(TbXTravel,  Grbl.XMaxTravel);
        Grbl.YMaxTravel   = D(TbYTravel,  Grbl.YMaxTravel);
        Grbl.ZMaxTravel   = D(TbZTravel,  Grbl.ZMaxTravel);
    }

    private void UpdateGrblAxesLock()
    {
        bool grbl = IsGrblActive;
        GrblAxesLockOverlay.Visibility = grbl ? Visibility.Collapsed : Visibility.Visible;
        BtnGrblAxesRead.IsEnabled      = grbl;
        BtnGrblAxesWrite.IsEnabled     = grbl;
        BtnGrblAxesDefaults.IsEnabled  = grbl;
        TbGrblAxesController.Text      = grbl ? $"Controlador: {ControllerProfile.Get(ActiveControllerType).Name}" : "Modo não-GRBL";
        TbGrblAxesController.Foreground = grbl
            ? new SolidColorBrush(Color.FromRgb(0x2e, 0x7d, 0x32))
            : new SolidColorBrush(Color.FromRgb(0xc6, 0x28, 0x28));
    }

    private void SetGrblAxesStatus(string msg, bool error = false, bool success = false)
    {
        TbGrblAxesStatus.Text = msg;
        TbGrblAxesStatus.Foreground = error   ? new SolidColorBrush(Color.FromRgb(0xc6, 0x28, 0x28))
                                    : success  ? new SolidColorBrush(Color.FromRgb(0x2e, 0x7d, 0x32))
                                               : new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55));
    }

    // ── Controller Type cards ────────────────────────────────────────────
    private void LoadControllerCards()
    {
        ControllerCardPanel.Children.Clear();
        OtherControllerCardPanel.Children.Clear();

        foreach (var profile in ControllerProfile.All)
        {
            var card = BuildControllerCard(profile);
            if (profile.IsGrbl)
                ControllerCardPanel.Children.Add(card);
            else
                OtherControllerCardPanel.Children.Add(card);
        }
        RefreshCardSelectionUi();
        GrblAxesSettingsToUi();
        UpdateGrblAxesLock();
    }

    private Border BuildControllerCard(ControllerProfile profile)
    {
        bool isGrbl   = profile.IsGrbl;
        var accentBg  = isGrbl ? Color.FromRgb(0xe8, 0xea, 0xf6) : Color.FromRgb(0xec, 0xef, 0xf1);
        var accentBd  = isGrbl ? Color.FromRgb(0x79, 0x86, 0xcb) : Color.FromRgb(0x90, 0xa4, 0xae);

        var card = new Border
        {
            Width = 195, Height = 130,
            Margin = new Thickness(4),
            CornerRadius = new CornerRadius(4),
            BorderThickness = new Thickness(2),
            BorderBrush = new SolidColorBrush(accentBd),
            Background = new SolidColorBrush(accentBg),
            Cursor = Cursors.Hand,
            Tag = profile.Type,
        };

        var sp = new StackPanel { Margin = new Thickness(8, 6, 8, 6) };

        // Header row: icon + name
        var header = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 4) };
        header.Children.Add(new TextBlock
        {
            Text = profile.Icon, FontSize = 18,
            VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 6, 0)
        });
        header.Children.Add(new TextBlock
        {
            Text = profile.Name, FontSize = 12, FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(0x1a, 0x23, 0x7e)),
            VerticalAlignment = VerticalAlignment.Center
        });
        sp.Children.Add(header);

        // Hardware line
        sp.Children.Add(new TextBlock
        {
            Text = profile.Hardware, FontSize = 9,
            Foreground = new SolidColorBrush(Color.FromRgb(0x44, 0x44, 0x44)),
            Margin = new Thickness(0, 0, 0, 4), TextWrapping = TextWrapping.Wrap
        });

        // Description
        sp.Children.Add(new TextBlock
        {
            Text = profile.Description, FontSize = 9,
            Foreground = new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55)),
            TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 6)
        });

        // Feature badges
        var badges = new WrapPanel();
        if (profile.SupportsJogCmd)       badges.Children.Add(MakeBadge("$J= Jog",     "#2e7d32", "#e8f5e9"));
        if (profile.SupportsRealTimeOvr)  badges.Children.Add(MakeBadge("RT Override",  "#0d47a1", "#e3f2fd"));
        if (profile.SupportsUnlockCmd)    badges.Children.Add(MakeBadge("$X Unlock",    "#e65100", "#fff3e0"));
        if (!profile.IsGrbl)              badges.Children.Add(MakeBadge("Non-GRBL",     "#37474f", "#eceff1"));
        sp.Children.Add(badges);

        card.Child = card.Child = sp;
        card.MouseLeftButtonUp += (_, _) => SelectController((ControllerType)card.Tag);

        return card;
    }

    private static Border MakeBadge(string text, string fg, string bg)
    {
        var border = new Border
        {
            CornerRadius = new CornerRadius(3),
            Background   = new SolidColorBrush((Color)ColorConverter.ConvertFromString(bg)),
            BorderBrush  = new SolidColorBrush((Color)ColorConverter.ConvertFromString(fg)),
            BorderThickness = new Thickness(1),
            Margin  = new Thickness(0, 0, 3, 2),
            Padding = new Thickness(3, 1, 3, 1),
        };
        border.Child = new TextBlock
        {
            Text       = text, FontSize = 8,
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(fg))
        };
        return border;
    }

    public void SelectController(ControllerType type)
    {
        ActiveControllerType = type;
        RefreshCardSelectionUi();
        UpdateGrblAxesLock();
        ControllerTypeChanged?.Invoke(type);
    }

    private void RefreshCardSelectionUi()
    {
        var profile = ControllerProfile.Get(ActiveControllerType);

        TbActiveController.Text = profile.Name;

        var features = new List<string>();
        if (profile.SupportsJogCmd)      features.Add("$J= Jog");
        if (profile.SupportsRealTimeOvr) features.Add("RT Override");
        if (profile.SupportsUnlockCmd)   features.Add("$X Unlock");
        if (profile.PipeSeparatedStatus) features.Add("Pipe Status");
        TbControllerFeatures.Text = features.Count > 0 ? string.Join("  •  ", features) : "";

        // Highlight selected card
        foreach (var panel in new[] { ControllerCardPanel, OtherControllerCardPanel })
        {
            foreach (UIElement el in panel.Children)
            {
                if (el is Border card && card.Tag is ControllerType ct)
                {
                    bool selected = ct == ActiveControllerType;
                    card.BorderThickness = new Thickness(selected ? 3 : 2);
                    card.Effect = selected
                        ? new System.Windows.Media.Effects.DropShadowEffect
                          { Color = Colors.OrangeRed, BlurRadius = 8, ShadowDepth = 0, Opacity = 0.8 }
                        : null;
                }
            }
        }
    }

    private void BtnRefreshDevices_Click(object s, RoutedEventArgs e) => RefreshDevices();
    private void BtnApply_Click(object s, RoutedEventArgs e)
    {
        GrblUiToSettings();
    }
    private void BtnSave_Click(object s, RoutedEventArgs e)
    {
        GrblUiToSettings();
        DialogResult = true;
        Close();
    }

    // ── GRBL tab buttons ─────────────────────────────────────────────────
    private void BtnGrblRead_Click(object s, RoutedEventArgs e)
    {
        if (RequestGrblRead is null)
        {
            TbGrblStatus.Text = "Not connected. Connect to the board first, then click Read.";
            TbGrblStatus.Foreground = new SolidColorBrush(Color.FromRgb(0xc6, 0x28, 0x28));
            return;
        }
        TbGrblStatus.Text = "Reading settings from board ($)…";
        TbGrblStatus.Foreground = new SolidColorBrush(Color.FromRgb(0x15, 0x65, 0xc0));
        RequestGrblRead();
    }

    private void BtnGrblWrite_Click(object s, RoutedEventArgs e)
    {
        if (RequestGrblWrite is null)
        {
            TbGrblStatus.Text = "Not connected. Connect to the board first, then click Write.";
            TbGrblStatus.Foreground = new SolidColorBrush(Color.FromRgb(0xc6, 0x28, 0x28));
            return;
        }
        GrblUiToSettings();
        RequestGrblWrite(Grbl.ToCommands());
        TbGrblStatus.Text = "Settings written to board.";
        TbGrblStatus.Foreground = new SolidColorBrush(Color.FromRgb(0x2e, 0x7d, 0x32));
    }

    private void BtnGrblDefaults_Click(object s, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "Reset all GRBL fields to default values?",
            "Reset GRBL defaults", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes) return;

        // Re-create with default ctor
        var fresh = new GrblSettings();
        Grbl.ApplyLine("$0=10");   // triggers all defaults via re-reading from a fresh instance
        CopyGrblSettings(fresh, Grbl);
        GrblSettingsToUi();
        TbGrblStatus.Text = "Default values restored.";
        TbGrblStatus.Foreground = new SolidColorBrush(Color.FromRgb(0xe6, 0x51, 0x00));
    }

    // ── Called externally when board returns $N=V lines ───────────────────
    public void ApplyGrblLine(string line)
    {
        Grbl.ApplyLine(line);
        Dispatcher.Invoke(() =>
        {
            GrblSettingsToUi();
            GrblAxesSettingsToUi();   // also refresh axes tab
            TbGrblStatus.Text = "Settings received from board.";
            TbGrblStatus.Foreground = new SolidColorBrush(Color.FromRgb(0x2e, 0x7d, 0x32));
            SetGrblAxesStatus("Valores recebidos da placa.", success: true);
        });
    }

    // ── Sync UI ↔ model ──────────────────────────────────────────────────
    private void GrblSettingsToUi()
    {
        TbGrbl0.Text  = Grbl.StepPulse.ToString();
        TbGrbl1.Text  = Grbl.StepIdleDelay.ToString();
        TbGrbl2.Text  = Grbl.StepPortInvert.ToString();
        TbGrbl3.Text  = Grbl.DirPortInvert.ToString();
        ChkGrbl4.IsChecked = Grbl.StepEnableInvert;
        ChkGrbl5.IsChecked = Grbl.LimitPinsInvert;
        ChkGrbl6.IsChecked = Grbl.ProbePinInvert;

        TbGrbl10.Text  = Grbl.StatusReportMask.ToString();
        TbGrbl11.Text  = Grbl.JunctionDeviation.ToString("F3", System.Globalization.CultureInfo.InvariantCulture);
        TbGrbl12.Text  = Grbl.ArcTolerance.ToString("F3", System.Globalization.CultureInfo.InvariantCulture);
        ChkGrbl13.IsChecked = Grbl.ReportInches;

        ChkGrbl20.IsChecked = Grbl.SoftLimits;
        ChkGrbl21.IsChecked = Grbl.HardLimits;

        ChkGrbl22.IsChecked = Grbl.HomingCycle;
        TbGrbl23.Text = Grbl.HomingDirInvert.ToString();
        TbGrbl24.Text = Grbl.HomingFeed.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
        TbGrbl25.Text = Grbl.HomingSeek.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
        TbGrbl26.Text = Grbl.HomingDebounce.ToString();
        TbGrbl27.Text = Grbl.HomingPullOff.ToString("F3", System.Globalization.CultureInfo.InvariantCulture);

        TbGrbl30.Text = Grbl.MaxSpindleRpm.ToString();
        TbGrbl31.Text = Grbl.MinSpindleRpm.ToString();
        ChkGrbl32.IsChecked = Grbl.LaserMode;

        TbGrbl100.Text = Grbl.XStepsPerMm.ToString("F3", System.Globalization.CultureInfo.InvariantCulture);
        TbGrbl101.Text = Grbl.YStepsPerMm.ToString("F3", System.Globalization.CultureInfo.InvariantCulture);
        TbGrbl102.Text = Grbl.ZStepsPerMm.ToString("F3", System.Globalization.CultureInfo.InvariantCulture);
        TbGrbl110.Text = Grbl.XMaxRate.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
        TbGrbl111.Text = Grbl.YMaxRate.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
        TbGrbl112.Text = Grbl.ZMaxRate.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
        TbGrbl120.Text = Grbl.XAccel.ToString("F3", System.Globalization.CultureInfo.InvariantCulture);
        TbGrbl121.Text = Grbl.YAccel.ToString("F3", System.Globalization.CultureInfo.InvariantCulture);
        TbGrbl122.Text = Grbl.ZAccel.ToString("F3", System.Globalization.CultureInfo.InvariantCulture);
        TbGrbl130.Text = Grbl.XMaxTravel.ToString("F3", System.Globalization.CultureInfo.InvariantCulture);
        TbGrbl131.Text = Grbl.YMaxTravel.ToString("F3", System.Globalization.CultureInfo.InvariantCulture);
        TbGrbl132.Text = Grbl.ZMaxTravel.ToString("F3", System.Globalization.CultureInfo.InvariantCulture);
    }

    private void GrblUiToSettings()
    {
        static int    I(TextBox tb, int    def) => int.TryParse(tb.Text, out int v) ? v : def;
        static double D(TextBox tb, double def) => double.TryParse(tb.Text,
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out double v) ? v : def;

        Grbl.StepPulse      = I(TbGrbl0, Grbl.StepPulse);
        Grbl.StepIdleDelay  = I(TbGrbl1, Grbl.StepIdleDelay);
        Grbl.StepPortInvert = I(TbGrbl2, Grbl.StepPortInvert);
        Grbl.DirPortInvert  = I(TbGrbl3, Grbl.DirPortInvert);
        Grbl.StepEnableInvert = ChkGrbl4.IsChecked == true;
        Grbl.LimitPinsInvert  = ChkGrbl5.IsChecked == true;
        Grbl.ProbePinInvert   = ChkGrbl6.IsChecked == true;

        Grbl.StatusReportMask  = I(TbGrbl10, Grbl.StatusReportMask);
        Grbl.JunctionDeviation = D(TbGrbl11, Grbl.JunctionDeviation);
        Grbl.ArcTolerance      = D(TbGrbl12, Grbl.ArcTolerance);
        Grbl.ReportInches      = ChkGrbl13.IsChecked == true;

        Grbl.SoftLimits = ChkGrbl20.IsChecked == true;
        Grbl.HardLimits = ChkGrbl21.IsChecked == true;

        Grbl.HomingCycle     = ChkGrbl22.IsChecked == true;
        Grbl.HomingDirInvert = I(TbGrbl23, Grbl.HomingDirInvert);
        Grbl.HomingFeed      = D(TbGrbl24, Grbl.HomingFeed);
        Grbl.HomingSeek      = D(TbGrbl25, Grbl.HomingSeek);
        Grbl.HomingDebounce  = I(TbGrbl26, Grbl.HomingDebounce);
        Grbl.HomingPullOff   = D(TbGrbl27, Grbl.HomingPullOff);

        Grbl.MaxSpindleRpm = I(TbGrbl30, Grbl.MaxSpindleRpm);
        Grbl.MinSpindleRpm = I(TbGrbl31, Grbl.MinSpindleRpm);
        Grbl.LaserMode     = ChkGrbl32.IsChecked == true;

        Grbl.XStepsPerMm = D(TbGrbl100, Grbl.XStepsPerMm);
        Grbl.YStepsPerMm = D(TbGrbl101, Grbl.YStepsPerMm);
        Grbl.ZStepsPerMm = D(TbGrbl102, Grbl.ZStepsPerMm);
        Grbl.XMaxRate    = D(TbGrbl110, Grbl.XMaxRate);
        Grbl.YMaxRate    = D(TbGrbl111, Grbl.YMaxRate);
        Grbl.ZMaxRate    = D(TbGrbl112, Grbl.ZMaxRate);
        Grbl.XAccel      = D(TbGrbl120, Grbl.XAccel);
        Grbl.YAccel      = D(TbGrbl121, Grbl.YAccel);
        Grbl.ZAccel      = D(TbGrbl122, Grbl.ZAccel);
        Grbl.XMaxTravel  = D(TbGrbl130, Grbl.XMaxTravel);
        Grbl.YMaxTravel  = D(TbGrbl131, Grbl.YMaxTravel);
        Grbl.ZMaxTravel  = D(TbGrbl132, Grbl.ZMaxTravel);
    }

    private static void CopyGrblSettings(GrblSettings src, GrblSettings dst)
    {
        foreach (var cmd in src.ToCommands())
            dst.ApplyLine(cmd);
    }
}
