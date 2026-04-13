using CNC_Drf.Core;

namespace CNC_Drf.ViewModels;

public partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly SerialComm _serial = new();
    private readonly GCodeParser _parser = new();

    // ── Connection ────────────────────────────────────────────────────────
    [ObservableProperty] private string _portName  = "COM3";
    [ObservableProperty] private int    _baudRate  = 115200;
    [ObservableProperty] private bool   _connected = false;
    [ObservableProperty] private string _statusText = "Desligado";

    // ── DRO ───────────────────────────────────────────────────────────────
    [ObservableProperty] private double _posX = 0;
    [ObservableProperty] private double _posY = 0;
    [ObservableProperty] private double _posZ = 0;
    [ObservableProperty] private double _posA = 0;

    // ── G-code ───────────────────────────────────────────────────────────
    [ObservableProperty] private string  _filePath  = string.Empty;
    [ObservableProperty] private int     _totalLines = 0;
    [ObservableProperty] private int     _currentLine = 0;
    [ObservableProperty] private double  _progress   = 0;

    // ── Jog ───────────────────────────────────────────────────────────────
    [ObservableProperty] private double _jogStep  = 1.0;
    [ObservableProperty] private double _jogFeed  = 1000;
    [ObservableProperty] private double _spindleRpm = 0;
    [ObservableProperty] private bool   _spindleOn  = false;

    // ── Terminal log ──────────────────────────────────────────────────────
    public ObservableCollection<string> Log { get; } = [];

    public GCodeParser Parser => _parser;

    public MainViewModel()
    {
        _serial.LineReceived      += OnLineReceived;
        _serial.ConnectionChanged += on => Application.Current.Dispatcher.Invoke(() =>
        {
            Connected  = on;
            StatusText = on ? $"Ligado a {PortName} @ {BaudRate}" : "Desligado";
        });
    }

    // ── Commands ──────────────────────────────────────────────────────────

    [RelayCommand]
    private void ToggleConnect()
    {
        if (Connected)
        {
            _serial.Disconnect();
        }
        else
        {
            try { _serial.Connect(PortName, BaudRate); }
            catch (Exception ex) { AddLog($"[ERRO] {ex.Message}"); }
        }
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
        var axis = parts[0];
        var dir  = parts[1] == "+" ? 1 : -1;
        double dist = dir * JogStep;
        SendCommand($"G91 G1 {axis}{dist:F4} F{JogFeed:F0}");
        SendCommand("G90");
    }

    [RelayCommand]
    private void Home() => SendCommand("$H");

    [RelayCommand]
    private void ZeroAll() => SendCommand("G92 X0 Y0 Z0 A0");

    [RelayCommand]
    private void SpindleToggle()
    {
        SpindleOn = !SpindleOn;
        SendCommand(SpindleOn ? $"M3 S{SpindleRpm:F0}" : "M5");
    }

    [RelayCommand]
    private void CoolantOn()  => SendCommand("M8");
    [RelayCommand]
    private void CoolantOff() => SendCommand("M9");

    [RelayCommand]
    private void FeedHold() => SendCommand("!");
    [RelayCommand]
    private void CycleResume() => SendCommand("~");
    [RelayCommand]
    private void SoftReset() => SendCommand("\x18");

    public void LoadFile(string path)
    {
        FilePath = path;
        var lines = File.ReadAllLines(path);
        _parser.Load(lines);
        TotalLines  = _parser.Lines.Count;
        CurrentLine = 0;
        Progress    = 0;
        AddLog($"[OK] Ficheiro carregado: {Path.GetFileName(path)} ({TotalLines} linhas)");
    }

    public string[] AvailablePorts => SerialPort.GetPortNames();
    public string[] GetAvailablePorts() => SerialPort.GetPortNames();

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
        // Formato GRBL: <Idle|MPos:0.000,0.000,0.000|...>
        if (!line.StartsWith('<')) return;
        var mpos = line.Split('|').FirstOrDefault(s => s.StartsWith("MPos:"));
        if (mpos is null) return;
        var coords = mpos[5..].Split(',');
        if (coords.Length >= 1 && double.TryParse(coords[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var vx)) PosX = vx;
        if (coords.Length >= 2 && double.TryParse(coords[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var vy)) PosY = vy;
        if (coords.Length >= 3 && double.TryParse(coords[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var vz)) PosZ = vz;
    }

    private void AddLog(string msg)
    {
        Log.Add($"[{DateTime.Now:HH:mm:ss}] {msg}");
        if (Log.Count > 2000) Log.RemoveAt(0);
    }

    public void Dispose() => _serial.Dispose();
}
