namespace CNC_Drf.Dialogs;

public class AxisRow
{
    public string Name     { get; set; } = "";
    public string Color    { get; set; } = "Black";
    public string Pulses   { get; set; } = "800";
    public string Max      { get; set; } = "200";
    public string Min      { get; set; } = "0";
    public string Backlash { get; set; } = "0";
    public string Accel    { get; set; } = "200";
    public bool   StepInv  { get; set; } = false;
    public bool   DirInv   { get; set; } = false;
}

public class PortRow
{
    public string Name   { get; set; } = "";
    public string Port   { get; set; } = "0";
    public bool   Invert { get; set; } = false;
    public string Desc   { get; set; } = "";
}

public class DeviceRow
{
    public string Name         { get; set; } = "";
    public string Manufacturer { get; set; } = "";
    public string VID          { get; set; } = "";
    public string PID          { get; set; } = "";
    public string Version      { get; set; } = "";
    public string Serial       { get; set; } = "";
    public string Outputs      { get; set; } = "";
    public string Inputs       { get; set; } = "";
}

public partial class SettingsDialog : Window
{
    public SettingsDialog()
    {
        InitializeComponent();
        LoadAxes();
        LoadInputPorts();
        LoadOutputPorts();
        RefreshDevices();
    }

    private void LoadAxes()
    {
        AxesPanel.ItemsSource = new List<AxisRow>
        {
            new() { Name="X", Color="#e53935", Pulses="800", Max="200",  Min="0",    Accel="200" },
            new() { Name="Y", Color="#fdd835", Pulses="800", Max="200",  Min="0",    Accel="200" },
            new() { Name="Z", Color="#1e88e5", Pulses="800", Max="0",    Min="-100", Accel="200" },
            new() { Name="A", Color="#ab47bc", Pulses="800", Max="360",  Min="0",    Accel="200" },
            new() { Name="B", Color="#26a69a", Pulses="800", Max="360",  Min="0",    Accel="200" },
            new() { Name="C", Color="#ff7043", Pulses="800", Max="360",  Min="0",    Accel="200" },
        };
    }

    private void LoadInputPorts()
    {
        InputPortsGrid.ItemsSource = new List<PortRow>
        {
            new() { Name="Limit X-",        Port="1", Invert=false, Desc="Hard limit X minimum" },
            new() { Name="Limit Y-",        Port="2", Invert=false, Desc="Hard limit Y minimum" },
            new() { Name="Limit Z-",        Port="3", Invert=false, Desc="Hard limit Z minimum" },
            new() { Name="Limit A-",        Port="0", Invert=false, Desc="Hard limit A minimum" },
            new() { Name="Limit B-",        Port="0", Invert=false, Desc="Hard limit B minimum" },
            new() { Name="Limit C-",        Port="0", Invert=false, Desc="Hard limit C minimum" },
            new() { Name="Probe 1",         Port="6", Invert=false, Desc="Tool length probe" },
            new() { Name="Probe 2",         Port="0", Invert=false, Desc="" },
            new() { Name="Emergency stop",  Port="8", Invert=false, Desc="E-stop input" },
            new() { Name="Door / Hold",     Port="0", Invert=false, Desc="Safety door" },
        };
    }

    private void LoadOutputPorts()
    {
        OutputPortsGrid.ItemsSource = new List<PortRow>
        {
            new() { Name="Spindle CW (M3)",        Port="0", Invert=false, Desc="Spindle clockwise" },
            new() { Name="Spindle CCW (M4)",       Port="1", Invert=false, Desc="Spindle counter-clockwise" },
            new() { Name="Mist coolant (M7)",      Port="2", Invert=false, Desc="Mist coolant" },
            new() { Name="Flood coolant (M8)",     Port="3", Invert=false, Desc="Flood coolant" },
            new() { Name="Output 0 (M62 P0)",      Port="4", Invert=false, Desc="General output 0" },
            new() { Name="Output 1 (M62 P1)",      Port="5", Invert=false, Desc="General output 1" },
            new() { Name="Output 2 (M62 P2)",      Port="6", Invert=false, Desc="General output 2" },
            new() { Name="Enable X axle",          Port="7", Invert=false, Desc="Motor enable X" },
            new() { Name="Enable Y axle",          Port="8", Invert=false, Desc="Motor enable Y" },
            new() { Name="Enable Z axle",          Port="9", Invert=false, Desc="Motor enable Z" },
        };
    }

    private void RefreshDevices()
    {
        var ports = SerialPort.GetPortNames();
        DeviceGrid.ItemsSource = ports.Select(p => new DeviceRow
        {
            Name = p, Manufacturer = "USB Serial", VID = "1386", PID = "20982",
            Version = "2", Serial = "", Outputs = "0", Inputs = "6"
        }).ToList();
    }

    private void BtnRefreshDevices_Click(object sender, RoutedEventArgs e)
        => RefreshDevices();

    private void BtnApply_Click(object sender, RoutedEventArgs e)
    {
        // Aplicar settings sem fechar
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}
