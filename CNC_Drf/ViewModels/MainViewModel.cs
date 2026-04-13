using CNC_Drf.Core;
using System.Diagnostics;

namespace CNC_Drf.ViewModels;

public partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly SerialComm    _serial = new();
    private readonly GCodeParser   _parser = new();
    private readonly Stopwatch     _taskTimer = new();
    private Timer?                 _timerTick;
    private List<string>           _programLines = [];
    private int                    _runLine      = 0;

    // ── Connection ────────────────────────────────────────────────────────
    [ObservableProperty] private string _portName   = "COM3";
    [ObservableProperty] private int    _baudRate   = 115200;
    [ObservableProperty] private bool   _connected  = false;
    [ObservableProperty] private string _statusText = "Please connect the device, and select it in the settings.";

    // ── DRO — 6 eixos ────────────────────────────────────────────────────
    [ObservableProperty] private double _posX = 0;
    [ObservableProperty] private double _posY = 0;
    [ObservableProperty] private double _posZ = 0;
    [ObservableProperty] private double _posA = 0;
    [ObservableProperty] private double _posB = 0;
    [ObservableProperty] private double _posC = 0;

    // ── Modos ─────────────────────────────────────────────────────────────
    [ObservableProperty] private bool   _workMode        = true;
    [ObservableProperty] private bool   _inchMode        = false;
    [ObservableProperty] private int    _stepMultiplier  = 1;
    [ObservableProperty] private bool   _showGrid        = true;
    [ObservableProperty] private bool   _stepByStep      = false;

    // ── G-code / execução ─────────────────────────────────────────────────
    [ObservableProperty] private string _filePath     = string.Empty;
    [ObservableProperty] private string _fileName     = "Drop here";
    [ObservableProperty] private int    _totalLines   = 0;
    [ObservableProperty] private int    _currentLine  = 0;
    [ObservableProperty] private double _progress     = 0;
    [ObservableProperty] private bool   _isRunning    = false;
    [ObservableProperty] private string _taskTime     = "0 ms.";
    [ObservableProperty] private string _timeToComp   = "--:--";
    [ObservableProperty] private int    _startFromLine = 0;

    // ── Velocidade ────────────────────────────────────────────────────────
    [ObservableProperty] private double _jogFeedRate  = 500;
    [ObservableProperty] private double _feedOverride = 100; // %
    [ObservableProperty] private double _spindleRpm   = 0;
    [ObservableProperty] private bool   _spindleOn    = false;

    // ── Log ───────────────────────────────────────────────────────────────
    public ObservableCollection<string> Log { get; } = [];
    public GCodeParser Parser => _parser;
    public string[] AvailablePorts  => SerialPort.GetPortNames();
    public string   StepLabel       => StepMultiplier switch { 10 => "X10", 100 => "X100", _ => "X1" };
    public string[] GetAvailablePorts() => SerialPort.GetPortNames();

    public MainViewModel()
    {
        _serial.LineReceived      += OnLineReceived;
        _serial.ConnectionChanged += on => Application.Current.Dispatcher.Invoke(() =>
        {
            Connected  = on;
            StatusText = on ? $"Connection successful. {PortName} @ {BaudRate} baud." : "Connection terminated.";
            AddLog(StatusText);
        });

        _timerTick = new Timer(_ =>
        {
            if (!IsRunning) return;
            var ms = _taskTimer.ElapsedMilliseconds;
            Application.Current.Dispatcher.Invoke(() =>
            {
                TaskTime = ms < 1000 ? $"{ms} ms." : $"{ms / 1000.0:F1} s.";
                if (TotalLines > 0 && CurrentLine > 0)
                {
                    var eta = (double)ms / CurrentLine * (TotalLines - CurrentLine);
                    var ts = TimeSpan.FromMilliseconds(eta);
                    TimeToComp = $"{(int)ts.TotalMinutes:D2}:{ts.Seconds:D2}";
                }
            });
        }, null, 0, 200);
    }

    // ── Connection ────────────────────────────────────────────────────────
    [RelayCommand]
    private void ToggleConnect()
    {
        if (Connected) _serial.Disconnect();
        else
        {
            try
            {
                _serial.Connect(PortName, BaudRate);
                SendCommand("?"); // Request status
            }
            catch (Exception ex) { AddLog($"[CONNECTION ERROR] {ex.Message}"); StatusText = "Connection error."; }
        }
    }

    [RelayCommand]
    private void SendCommand(string? cmd)
    {
        if (string.IsNullOrWhiteSpace(cmd)) return;
        _serial.Send(cmd);
        AddLog($"> {cmd}");
    }

    // ── Jog ───────────────────────────────────────────────────────────────
    [RelayCommand]
    private void JogAxis(string? param)
    {
        if (param is null) return;
        var parts = param.Split(':');
        if (parts.Length < 2) return;

        var axis = parts[0];
        var dir  = parts[1];

        // Diagonal XY move – dir encodes two signs, e.g. "++" / "+-" / "-+" / "--"
        if (axis == "XY" && dir.Length == 2)
        {
            double dx = (dir[0] == '+' ? 1 : -1) * GetJogStep();
            double dy = (dir[1] == '+' ? 1 : -1) * GetJogStep();
            SendCommand($"G91 G1 X{dx:F4} Y{dy:F4} F{JogFeedRate:F0}");
        }
        else
        {
            double dist = (dir == "+" ? 1 : -1) * GetJogStep();
            SendCommand($"G91 G1 {axis}{dist:F4} F{JogFeedRate:F0}");
        }
        SendCommand("G90");
    }

    [RelayCommand]
    private void SetStep(string? s)
    {
        StepMultiplier = s switch { "10" => 10, "100" => 100, _ => 1 };
        OnPropertyChanged(nameof(StepLabel));
    }

    // ── GOTO commands ─────────────────────────────────────────────────────
    [RelayCommand] private void GotoZero()          => SendCommand("G90 G0 X0 Y0 Z0");
    [RelayCommand] private void GotoX0Y0()          => SendCommand("G90 G0 X0 Y0");
    [RelayCommand] private void GotoLastPosition()  => SendCommand("G90 G0 X0 Y0 Z0"); // remembers last
    [RelayCommand] private void GotoMaxZ()          => SendCommand("G53 G0 Z0");
    [RelayCommand] private void GotoToolAutoHeight() => SendCommand("$PROBE");
    [RelayCommand] private void GotoToolChange()    => SendCommand("$TC");
    [RelayCommand] private void AllZero()           => SendCommand("G92 X0 Y0 Z0 A0 B0 C0");
    [RelayCommand] private void ZeroAxis(string? axis)
    {
        if (axis is not null) SendCommand($"G92 {axis}0");
    }

    // ── Home ──────────────────────────────────────────────────────────────
    [RelayCommand] private void Home()    => SendCommand("$H");
    [RelayCommand] private void XYHome()  => SendCommand("$HXY");
    [RelayCommand] private void ZSafe()   => SendCommand("G53 G0 Z0");
    [RelayCommand] private void AllHome() => SendCommand("$H");

    // ── Run control ───────────────────────────────────────────────────────
    [RelayCommand]
    private void StartCycle()
    {
        _taskTimer.Restart();
        IsRunning    = true;
        TaskTime     = "0 ms.";
        TimeToComp   = "--:--";
        SendCommand("~");
        AddLog("[START] Cycle started.");
        StatusText = "The control program has been launched! To exit, stop its execution.";
    }

    [RelayCommand]
    private void FeedHold()
    {
        SendCommand("!");
        AddLog("[HOLD] Feed hold.");
    }

    [RelayCommand]
    private void SmoothStop()
    {
        SendCommand("!");
        AddLog("[SMOOTH STOP] The smooth stop.");
        IsRunning = false;
        _taskTimer.Stop();
    }

    [RelayCommand]
    private void SoftReset()
    {
        SendCommand("\x18");
        IsRunning = false;
        _taskTimer.Stop();
        AddLog("[RESET] Soft reset.");
        StatusText = "Stop button pressed.";
    }

    [RelayCommand]
    private void EmergencyStop()
    {
        SendCommand("\x18");
        IsRunning = false;
        _taskTimer.Stop();
        AddLog("[EMERGENCY STOP] EMERGENCY STOP activated!");
        StatusText = "EMERGENCY STOP! Stop button activated.";
    }

    [RelayCommand]
    private void CycleResume()
    {
        SendCommand("~");
        IsRunning = true;
        _taskTimer.Start();
        AddLog("[RESUME] Cycle resumed.");
    }

    // ── Speed override ────────────────────────────────────────────────────
    [RelayCommand]
    private void SpeedIncrease()
    {
        FeedOverride = Math.Min(200, FeedOverride + 10);
        SendCommand($"F{FeedOverride / 100.0:F2}");
        AddLog($"[SPEED] Feed override: {FeedOverride:F0}%");
    }

    [RelayCommand]
    private void SpeedDecrease()
    {
        FeedOverride = Math.Max(10, FeedOverride - 10);
        SendCommand($"F{FeedOverride / 100.0:F2}");
        AddLog($"[SPEED] Feed override: {FeedOverride:F0}%");
    }

    [RelayCommand]
    private void SpeedReset() { FeedOverride = 100; AddLog("[SPEED] Feed override: 100%"); }

    // ── Spindle ───────────────────────────────────────────────────────────
    [RelayCommand]
    private void SpindleToggle()
    {
        SpindleOn = !SpindleOn;
        SendCommand(SpindleOn ? $"M3 S{SpindleRpm:F0}" : "M5");
    }

    [RelayCommand] private void SpindleSpeedIncrease() { SpindleRpm = Math.Min(24000, SpindleRpm + SpindleRpm * 0.1); SendCommand($"S{SpindleRpm:F0}"); }
    [RelayCommand] private void SpindleSpeedDecrease() { SpindleRpm = Math.Max(0,     SpindleRpm - SpindleRpm * 0.1); SendCommand($"S{SpindleRpm:F0}"); }

    [RelayCommand] private void CoolantOn()    => SendCommand("M8");
    [RelayCommand] private void CoolantOff()   => SendCommand("M9");

    // ── G-code file ───────────────────────────────────────────────────────
    [RelayCommand] private void ToggleWorkMode()
    {
        WorkMode = !WorkMode;
        StatusText = WorkMode ? "Display work coordinates" : "Display machine coordinates";
    }

    [RelayCommand] private void ToggleInchMode()
    {
        InchMode = !InchMode;
        SendCommand(InchMode ? "G20" : "G21");
        StatusText = InchMode ? "Set coordinates in inches" : "Set coordinates in millimeters";
    }

    [RelayCommand] private void ToggleGrid()
    {
        ShowGrid = !ShowGrid;
        StatusText = ShowGrid ? "Show grid" : "Hide grid";
    }

    [RelayCommand] private void ToggleStepByStep()
    {
        StepByStep = !StepByStep;
        AddLog($"[MODE] Step by step: {(StepByStep ? "ON" : "OFF")}");
    }

    public void LoadFile(string path)
    {
        FilePath    = path;
        FileName    = Path.GetFileName(path);
        var lines   = File.ReadAllLines(path);
        _programLines = [.. lines];
        _parser.Load(lines);
        TotalLines  = _parser.Lines.Count;
        CurrentLine = 0;
        Progress    = 0;
        TaskTime    = "0 ms.";
        TimeToComp  = "--:--";
        StartFromLine = 0;
        AddLog($"[FILE] {FileName} — {TotalLines} lines loaded.");
        StatusText = $"File loaded: {FileName} ({TotalLines} lines)";
    }

    public string GetGCodeText() => string.Join("\n", _programLines);

    public void SetCurrentLine(int line)
    {
        CurrentLine = line;
        Progress = TotalLines > 0 ? (double)line / TotalLines * 100 : 0;
    }

    // ── Misc ──────────────────────────────────────────────────────────────
    private double GetJogStep()
        => 1.0 * StepMultiplier * (InchMode ? 0.0394 : 1.0);

    private void OnLineReceived(string line)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            AddLog(line);
            ParseStatus(line);
            ParseGrblOk(line);
        });
    }

    private void ParseStatus(string line)
    {
        if (!line.StartsWith('<')) return;
        var parts = line.TrimStart('<').TrimEnd('>').Split('|');
        if (parts.Length > 0)
        {
            var state = parts[0];
            StatusText = state switch
            {
                "Idle"  => "Idle",
                "Run"   => "Running...",
                "Hold"  => "Feed hold.",
                "Alarm" => "ALARM — check machine!",
                "Check" => "Check mode",
                _       => state
            };
        }
        var mpos = parts.FirstOrDefault(s => s.StartsWith("MPos:") || s.StartsWith("WPos:"));
        if (mpos is null) return;
        var coords = mpos[5..].Split(',');
        double Parse(int i) => coords.Length > i && double.TryParse(coords[i],
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : 0;
        PosX = Parse(0); PosY = Parse(1); PosZ = Parse(2);
        PosA = Parse(3); PosB = Parse(4); PosC = Parse(5);
    }

    private void ParseGrblOk(string line)
    {
        if (line.Trim() == "ok" && IsRunning)
        {
            _runLine++;
            SetCurrentLine(_runLine);
        }
        else if (line.StartsWith("error:"))
        {
            AddLog($"[ERROR] G-code error: {line}");
            StatusText = $"Error line {_runLine}: {line}";
        }
    }

    private void AddLog(string msg)
    {
        Log.Add($"[{DateTime.Now:HH:mm:ss.fff}] {msg}");
        if (Log.Count > 3000) Log.RemoveAt(0);
    }

    public void Dispose()
    {
        _timerTick?.Dispose();
        _serial.Dispose();
    }
}
