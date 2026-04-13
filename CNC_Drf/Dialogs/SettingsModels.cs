namespace CNC_Drf.Dialogs;

public partial class PortEntry : ObservableObject
{
    [ObservableProperty] private bool   _enabled = false;
    [ObservableProperty] private string _name    = "";
    [ObservableProperty] private int    _port    = 0;
    [ObservableProperty] private bool   _invert  = false;
    [ObservableProperty] private bool   _active  = false;
    [ObservableProperty] private string _hotKey  = "None";
    [ObservableProperty] private bool   _hasHotKey = false;
    public string Group { get; set; } = "";
    public bool   IsHeader => false;
}

public class PortGroup
{
    public string            Name    { get; set; } = "";
    public List<PortEntry>   Entries { get; set; } = [];
}

public partial class AxisSettings : ObservableObject
{
    public string Name  { get; set; } = "X";
    public string Color { get; set; } = "#e53935";
    [ObservableProperty] private bool   _enabled    = true;
    [ObservableProperty] private int    _impulses   = 320;
    [ObservableProperty] private int    _speed      = 1500;
    [ObservableProperty] private int    _accel      = 20;
    [ObservableProperty] private bool   _dirInvert  = false;
    [ObservableProperty] private bool   _stepInvert = false;
    [ObservableProperty] private double _backlash   = 0;
    [ObservableProperty] private bool   _slave      = false;
    [ObservableProperty] private string _remap      = "";
    [ObservableProperty] private double _axMin      = 0;
    [ObservableProperty] private double _axMax      = 200;
    [ObservableProperty] private bool   _softMin    = false;
    [ObservableProperty] private bool   _softMax    = true;
    [ObservableProperty] private double _safeSlow   = 10;
    [ObservableProperty] private bool   _homeToMin  = false;
    [ObservableProperty] private bool   _homeToMax  = true;
    [ObservableProperty] private int    _homeOrder  = 1;
    [ObservableProperty] private int    _homeSpeed  = 300;
    [ObservableProperty] private int    _homeReturn = 100;
}

public partial class ToolEntry : ObservableObject
{
    public int    Number   { get; set; } = 1;
    [ObservableProperty] private double _height = 0;
    [ObservableProperty] private double _width  = 0;
    [ObservableProperty] private bool   _useX   = false;
    [ObservableProperty] private bool   _useY   = false;
    [ObservableProperty] private bool   _useZ   = false;
    [ObservableProperty] private bool   _useA   = false;
    [ObservableProperty] private bool   _useB   = false;
    [ObservableProperty] private bool   _useC   = false;
    [ObservableProperty] private double _posX   = 0;
    [ObservableProperty] private double _posY   = 0;
    [ObservableProperty] private double _posZ   = 0;
    [ObservableProperty] private double _posA   = 0;
    [ObservableProperty] private double _posB   = 0;
    [ObservableProperty] private double _posC   = 0;
}

public class DeviceEntry
{
    public string Name         { get; set; } = "";
    public string Manufacturer { get; set; } = "";
    public string VID          { get; set; } = "";
    public string PID          { get; set; } = "";
    public string Version      { get; set; } = "";
    public string Serial       { get; set; } = "";
    public string Outputs      { get; set; } = "0";
    public string Inputs       { get; set; } = "0";
}
