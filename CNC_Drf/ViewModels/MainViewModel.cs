using CNC_Drf.Core;

namespace CNC_Drf.ViewModels;

public partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly SerialComm _serial = new();
    private readonly GCodeParser _parser = new();

    // ── Connection ────────────────────────────────────────────────────────
    [ObservableProperty] private string _portName   = "COM3";
    [ObservableProperty] private int    _baudRate   = 115200;
    [ObservableProperty] private bool   _connected  = false;
    [ObservableProperty] private string _statusText = "Please connect the device";

    // ── DRO – 6 eixos ────────────────────────────────────────────────────
    [ObservableProperty] private double _posX = 0;
    [ObservableProperty] private double _posY = 0;
    [ObservableProperty] private double _posZ = 0;
    [ObservableProperty] private double _posA = 0;
    [ObservableProperty] private double _posB = 0;
    [ObservableProperty] private double _posC = 0;

    // ── Modos ─────────────────────────────────────────────────────────────
    [ObservableProperty] private bool _workMode  = true;  // false = Machine
    [ObservableProperty] private bool _inchMode  = false; // false = MM
    [ObservableProperty] private int  _stepMultiplier = 1; // 1 / 10 / 100

    // ── G-code ───────────────────────────────────────────────────────────
    [ObservableProperty] private string _filePath    = string.Empty;
    [ObservableProperty] private string _fileName    = "Drop here";
    [ObservableProperty] private int    _totalLines  = 0;
    [ObservableProperty] private int    _currentLine = 0;
    [ObservableProperty] private double _progress    = 0;
    [ObservableProperty] private string _taskTime    = "0 ms.";
    [ObservableProperty] private bool   _isRunning   = false;

    // ── Jog ───────────────────────────────────────────────────────────────
    [ObservableProperty] private double _jogFeedRate = 500;
    [ObservableProperty] private double _spindleRpm  = 0;
    [ObservableProperty] private bool   _spindleOn   = false;
    [ObservableProperty] private double _spindlePercent = 0;

    // ── Log ───────────────────────────────────────────────────────────────
    public ObservableCollection<string> Log { get; } = [];
    public GCodeParser Parser => _parser;
    public string[] AvailablePorts => SerialPort.GetPortNames();
    public string[] GetAvailablePorts() => SerialPort.GetPortNames();

    // ── Step label ───────────────────────────────────────────────────────
    public string StepLabel => StepMultiplier switch { 10 => "X10", 100 => "X100", _ => "X1" };

    public MainViewModel()
    {
        _serial.LineReceived      += OnLineReceived;
        _serial.ConnectionChanged += on => Application.Current.Dispatcher.Invoke(() =>
        {
            Connected  = on;
            StatusText = on ? $"Ligado — {PortName} @ {BaudRate}" : "Please connect the device";
        });
    }

    // ── Commands ──────────────────────────────────────────────────────────

    [RelayCommand]
    private void ToggleConnect()
    {
        if (Connected) _serial.Disconnect();
        else try { _serial.Connect(PortName, BaudRate); }
             catch (Exception ex) { AddLog($"[ERRO] {ex.Message}"); }
    }

    [RelayCommand]
    private void SendCommand(string? cmd)
    {
        if (string.IsNullOrWhiteSpace(cmd)) return;
        _serial.Send(cmd);
        AddLog($"> {cmd}");
    }

    [RelayCommand]
    private void JogAxis(string? param)
    {
        if (param is null) return;
        var parts = param.Split(':');
        if (parts.Length < 2) return;
        double dist  = (parts[1] == "+" ? 1 : -1) * GetJogStep();
        string unit  = InchMode ? "" : "";
        SendCommand($"G91 G1 {parts[0]}{dist:F4} F{JogFeedRate:F0}");
        SendCommand("G90");
    }

    [RelayCommand]
    private void SetStep(string? s)
    {
        StepMultiplier = s switch { "10" => 10, "100" => 100, _ => 1 };
        OnPropertyChanged(nameof(StepLabel));
    }

    [RelayCommand] private void Home()     => SendCommand("$H");
    [RelayCommand] private void AllZero()  => SendCommand("G92 X0 Y0 Z0 A0 B0 C0");
    [RelayCommand] private void GotoZero() => SendCommand("G90 G0 X0 Y0 Z0");
    [RelayCommand] private void GotoX0Y0() => SendCommand("G90 G0 X0 Y0");
    [RelayCommand] private void AllHome()  => SendCommand("$H");
    [RelayCommand] private void XYHome()   => SendCommand("$HXY");
    [RelayCommand] private void ZSafe()    => SendCommand("G53 G0 Z0");
    [RelayCommand] private void ToolZero() => SendCommand("G92 Z0");

    [RelayCommand]
    private void SpindleToggle()
    {
        SpindleOn = !SpindleOn;
        SendCommand(SpindleOn ? $"M3 S{SpindleRpm:F0}" : "M5");
    }

    [RelayCommand] private void CoolantOn()    => SendCommand("M8");
    [RelayCommand] private void CoolantOff()   => SendCommand("M9");
    [RelayCommand] private void FeedHold()     => SendCommand("!");
    [RelayCommand] private void CycleResume()  => SendCommand("~");
    [RelayCommand] private void SoftReset()    => SendCommand("\x18");

    [RelayCommand]
    private void ToggleWorkMode()
    {
        WorkMode = !WorkMode;
        SendCommand(WorkMode ? "G54" : "");
    }

    [RelayCommand]
    private void ToggleInchMode()
    {
        InchMode = !InchMode;
        SendCommand(InchMode ? "G20" : "G21");
    }

    public void LoadFile(string path)
    {
        FilePath = path;
        FileName = Path.GetFileName(path);
        var lines = File.ReadAllLines(path);
        _parser.Load(lines);
        TotalLines  = _parser.Lines.Count;
        CurrentLine = 0;
        Progress    = 0;
        AddLog($"[OK] {FileName} — {TotalLines} linhas");
    }

    private double GetJogStep()
    {
        double baseStep = 1.0;
        return baseStep * StepMultiplier * (InchMode ? 0.0394 : 1.0);
    }

    private void OnLineReceived(string line)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            AddLog(line);
            ParseStatus(line);
        });
    }

    private void ParseStatus(string line)
    {
        if (!line.StartsWith('<')) return;
        var mpos = line.Split('|').FirstOrDefault(s => s.StartsWith("MPos:") || s.StartsWith("WPos:"));
        if (mpos is null) return;
        var coords = mpos[5..].Split(',');
        double Parse(int i) => coords.Length > i && double.TryParse(coords[i],
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : 0;
        PosX = Parse(0); PosY = Parse(1); PosZ = Parse(2);
        PosA = Parse(3); PosB = Parse(4); PosC = Parse(5);
    }

    private void AddLog(string msg)
    {
        Log.Add($"[{DateTime.Now:HH:mm:ss.fff}] {msg}");
        if (Log.Count > 2000) Log.RemoveAt(0);
    }

    public void Dispose() => _serial.Dispose();
}
