using CNC_Drf.Core;
using CNC_Drf.Dialogs;
using System.Diagnostics;

namespace CNC_Drf.ViewModels;

public partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly SerialComm    _serial = new();
    private readonly GCodeParser   _parser = new();
    private readonly Stopwatch     _taskTimer = new();
    private Timer?                 _timerTick;
    private List<string>           _programLines = [];

    // ── Streaming state ───────────────────────────────────────────────────
    private int  _streamIdx    = 0;
    private bool _streamPaused = false;

    // ── DRO poll timer ────────────────────────────────────────────────────
    private Timer? _pollTimer;

    // ── GRBL settings ─────────────────────────────────────────────────────
    public GrblSettings GrblSettings { get; } = new();

    // ── Controller type ───────────────────────────────────────────────────
    private ControllerProfile _controller = ControllerProfile.Get(ControllerType.Grbl11);
    public  ControllerProfile Controller  => _controller;
    [ObservableProperty] private string _controllerName = "GRBL 1.1";

    public void SetControllerType(ControllerType type)
    {
        _controller     = ControllerProfile.Get(type);
        ControllerName  = _controller.Name;
        AddLog($"[CONTROLLER] Changed to: {_controller.Name}");
    }

    // Raised when a $N=V line arrives; interested views can subscribe
    public event Action<string>? GrblSettingLineReceived;

    // ── Connection ────────────────────────────────────────────────────────
    [ObservableProperty] private string _portName   = "COM3";
    [ObservableProperty] private int    _baudRate   = 115200;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(JogAxisCommand))]
    [NotifyCanExecuteChangedFor(nameof(SendCommandCommand))]
    [NotifyCanExecuteChangedFor(nameof(StartCycleCommand))]
    [NotifyCanExecuteChangedFor(nameof(HomeCommand))]
    [NotifyCanExecuteChangedFor(nameof(UnlockAlarmCommand))]
    private bool _connected = false;

    [ObservableProperty] private string _statusText = "Por favor ligue o dispositivo e selecione a porta COM.";

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

    // ── Estado da máquina ─────────────────────────────────────────────────
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(JogAxisCommand))]
    private bool _isAlarm = false;

    // ── Velocidade ────────────────────────────────────────────────────────
    [ObservableProperty] private double _jogFeedRate  = 500;
    [ObservableProperty] private double _feedOverride = 100; // %
    [ObservableProperty] private double _spindleRpm   = 0;
    [ObservableProperty] private bool   _spindleOn    = false;

    // ── Log ───────────────────────────────────────────────────────────────
    public ObservableCollection<string> Log { get; } = [];
    public GCodeParser Parser => _parser;
    public string[] AvailablePorts   => SerialPort.GetPortNames();
    public string   StepLabel        => StepMultiplier switch { 10 => "X10", 100 => "X100", _ => "X1" };
    public string[] GetAvailablePorts() => SerialPort.GetPortNames();
    public string   ConnectionInfo   => Connected ? $"{PortName} @ {BaudRate}" : "";
    public string   CurrentLineText  =>
        (_parser.Lines.Count > 0 && CurrentLine >= 0 && CurrentLine < _parser.Lines.Count)
        ? _parser.Lines[CurrentLine].Raw
        : "";

    public MainViewModel()
    {
        _serial.LineReceived      += OnLineReceived;
        _serial.ConnectionChanged += on => Application.Current.Dispatcher.Invoke(() =>
        {
            Connected  = on;
            StatusText = on ? $"Connection successful. {PortName} @ {BaudRate} baud." : "Connection terminated.";
            AddLog(StatusText);
            OnPropertyChanged(nameof(ConnectionInfo));
        });

        // ── Timer de UI (tempo de execução + ETA) — 200 ms ───────────────
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
                    var ts  = TimeSpan.FromMilliseconds(eta);
                    TimeToComp = $"{(int)ts.TotalMinutes:D2}:{ts.Seconds:D2}";
                }
            });
        }, null, 0, 200);

        // ── Timer de polling DRO — envia '?' ao GRBL a cada 200 ms ───────
        // Sempre que ligado: em Idle, em Jog, e durante execução de programa.
        // Sem isto o DRO só actualiza no homing porque o fluxo de comandos
        // não inclui pedidos de estado contínuos.
        // Resposta: <Run|MPos:x,y,z|FS:...>  → ParseStatus()
        _pollTimer = new Timer(_ =>
        {
            if (Connected)
                _serial.Send("?");
        }, null, 600, 200);
    }

    // ── Connection ────────────────────────────────────────────────────────
    [RelayCommand]
    private void ToggleConnect()
    {
        if (Connected)
        {
            _serial.Disconnect();
        }
        else
        {
            try
            {
                _serial.Connect(PortName, BaudRate);
                AddLog($"[CONTROLLER] Modo: {_controller.Name}  —  {PortName} @ {BaudRate}");

                // Pede estado imediatamente e também após 600ms
                // (algumas placas demoram a arrancar após o DTR)
                if (!string.IsNullOrEmpty(_controller.StatusInitCmd))
                {
                    SendCommand(_controller.StatusInitCmd);
                    Task.Delay(600).ContinueWith(_ =>
                        Application.Current.Dispatcher.Invoke(() =>
                            SendCommand(_controller.StatusInitCmd)));
                }
            }
            catch (Exception ex)
            {
                AddLog($"[ERRO DE LIGAÇÃO] {ex.Message}");
                StatusText = $"Erro ao ligar: {ex.Message}";
            }
        }
    }

    [RelayCommand(CanExecute = nameof(CanSendCommand))]
    private void SendCommand(string? cmd)
    {
        if (string.IsNullOrWhiteSpace(cmd)) return;
        _serial.Send(cmd);
        AddLog($"> {cmd}");
    }
    private bool CanSendCommand(string? _) => Connected;

    // ── Jog ───────────────────────────────────────────────────────────────
    [RelayCommand(CanExecute = nameof(CanJogAxis))]
    private void JogAxis(string? param)
    {
        if (param is null) return;
        var parts = param.Split(':');
        if (parts.Length < 2) return;

        var axis = parts[0];
        var dir  = parts[1];
        var unit = InchMode ? "G20" : "G21";

        if (_controller.SupportsJogCmd)
        {
            // GRBL 1.1+ — $J= command (cancellable real-time jog)
            if (axis == "XY" && dir.Length == 2)
            {
                double dx = (dir[0] == '+' ? 1 : -1) * GetJogStep();
                double dy = (dir[1] == '+' ? 1 : -1) * GetJogStep();
                SendRaw(GC($"$J=G91 {unit} X{dx:F4} Y{dy:F4} F{JogFeedRate:F0}"));
            }
            else
            {
                double dist = (dir == "+" ? 1 : -1) * GetJogStep();
                SendRaw(GC($"$J=G91 {unit} {axis}{dist:F4} F{JogFeedRate:F0}"));
            }
        }
        else
        {
            // GRBL 0.9 / legacy — plain G91/G90
            if (axis == "XY" && dir.Length == 2)
            {
                double dx = (dir[0] == '+' ? 1 : -1) * GetJogStep();
                double dy = (dir[1] == '+' ? 1 : -1) * GetJogStep();
                SendCommand(GC($"G91 G1 X{dx:F4} Y{dy:F4} F{JogFeedRate:F0}"));
            }
            else
            {
                double dist = (dir == "+" ? 1 : -1) * GetJogStep();
                SendCommand(GC($"G91 G1 {axis}{dist:F4} F{JogFeedRate:F0}"));
            }
            SendCommand("G90");
        }
    }

    private bool CanJogAxis(string? _) => Connected && !IsAlarm;

    // Envia sem adicionar ao log (para comandos de alta frequência como jog)
    private void SendRaw(string cmd)
    {
        _serial.Send(cmd);
        AddLog($"> {cmd}");
    }

    /// <summary>
    /// Formata uma string G-code com InvariantCulture (ponto decimal).
    /// Uso: GC($"G1 X{dist:F4} F{feed:F0}")
    /// </summary>
    private static string GC(FormattableString s)
        => FormattableString.Invariant(s);

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
    [RelayCommand(CanExecute = nameof(CanHome))]
    private void Home()    => SendCommand("$H");
    private bool CanHome() => Connected;
    [RelayCommand] private void XYHome()  => SendCommand("$HXY");
    [RelayCommand] private void ZSafe()   => SendCommand("G53 G0 Z0");
    [RelayCommand] private void AllHome() => SendCommand("$H");

    // ── Run control ───────────────────────────────────────────────────────
    [RelayCommand(CanExecute = nameof(CanStartCycle))]
    private void StartCycle()
    {
        if (_parser.Lines.Count == 0)
        {
            StatusText = "Nenhum ficheiro G-code carregado.";
            AddLog("[AVISO] Carregue um ficheiro G-code antes de iniciar.");
            return;
        }

        _streamIdx    = Math.Max(0, Math.Min(StartFromLine, _parser.Lines.Count - 1));
        _streamPaused = false;
        IsRunning     = true;
        CurrentLine   = _streamIdx;
        Progress      = 0;
        TaskTime      = "0 ms.";
        TimeToComp    = "--:--";
        _taskTimer.Restart();

        AddLog($"[START] Ciclo iniciado — {_parser.Lines.Count} linhas  (a partir da linha {_streamIdx + 1})");
        StatusText = "Programa CNC em execução…";
        OnPropertyChanged(nameof(CurrentLineText));

        // Envia a primeira linha
        StreamSendNext();
    }
    private bool CanStartCycle() => Connected && !IsRunning && _parser.Lines.Count > 0;

    /// <summary>Envia a próxima linha G-code pendente. Chamado no arranque e em cada 'ok'.</summary>
    private void StreamSendNext()
    {
        if (!IsRunning || _streamPaused || !Connected) return;

        if (_streamIdx >= _parser.Lines.Count)
        {
            // Programa completo
            IsRunning = false;
            _taskTimer.Stop();
            StatusText = $"Programa concluído — {_parser.Lines.Count} linhas.";
            AddLog("[DONE] ✔ Programa G-code concluído.");
            return;
        }

        var line = _parser.Lines[_streamIdx];
        _serial.Send(line.Raw);
        AddLog(GC($"> L{line.Number}: {line.Raw}"));
    }

    [RelayCommand]
    private void FeedHold()
    {
        _serial.Send("!");          // real-time — sem newline
        _streamPaused = true;
        AddLog("[HOLD] ⏸ Feed hold — máquina em pausa.");
        StatusText = "⏸ Pausa — clique Resume para continuar.";
    }

    [RelayCommand]
    private void CycleResume()
    {
        if (!IsRunning) return;
        _serial.Send("~");          // real-time resume
        _streamPaused = false;
        _taskTimer.Start();
        AddLog("[RESUME] ▶ Ciclo retomado.");
        StatusText = "Programa CNC em execução…";
        StreamSendNext();           // reenviar se necessário
    }

    [RelayCommand]
    private void SmoothStop()
    {
        _serial.Send("!");
        _streamPaused = true;
        IsRunning     = false;
        _taskTimer.Stop();
        AddLog("[SMOOTH STOP] 🟡 Paragem suave.");
        StatusText = "Paragem suave — use Reset para limpar.";
    }

    [RelayCommand]
    private void SoftReset()
    {
        _serial.Send("\x18");
        IsRunning     = false;
        _streamPaused = false;
        _streamIdx    = 0;
        _taskTimer.Stop();
        AddLog("[RESET] ↺ Soft reset (Ctrl+X) enviado.");
        StatusText = "Reset enviado.";
    }

    [RelayCommand]
    private void EmergencyStop()
    {
        _serial.Send("\x18");
        IsRunning     = false;
        _streamPaused = false;
        _streamIdx    = 0;
        _taskTimer.Stop();
        AddLog("[EMERGENCY STOP] ⛔ PARAGEM DE EMERGÊNCIA!");
        StatusText = "⛔ PARAGEM DE EMERGÊNCIA!";
    }

    // ── Speed override ────────────────────────────────────────────────────
    [RelayCommand]
    private void SpeedIncrease()
    {
        FeedOverride = Math.Min(200, FeedOverride + 10);
        SendCommand(GC($"F{FeedOverride / 100.0:F2}"));
        AddLog($"[SPEED] Feed override: {FeedOverride:F0}%");
    }

    [RelayCommand]
    private void SpeedDecrease()
    {
        FeedOverride = Math.Max(10, FeedOverride - 10);
        SendCommand(GC($"F{FeedOverride / 100.0:F2}"));
        AddLog($"[SPEED] Feed override: {FeedOverride:F0}%");
    }

    [RelayCommand]
    private void SpeedReset() { FeedOverride = 100; AddLog("[SPEED] Feed override: 100%"); }

    // ── Spindle ───────────────────────────────────────────────────────────
    [RelayCommand]
    private void SpindleToggle()
    {
        SpindleOn = !SpindleOn;
        SendCommand(SpindleOn ? GC($"M3 S{SpindleRpm:F0}") : "M5");
    }

    [RelayCommand] private void SpindleSpeedIncrease() { SpindleRpm = Math.Min(24000, SpindleRpm + SpindleRpm * 0.1); SendCommand(GC($"S{SpindleRpm:F0}")); }
    [RelayCommand] private void SpindleSpeedDecrease() { SpindleRpm = Math.Max(0,     SpindleRpm - SpindleRpm * 0.1); SendCommand(GC($"S{SpindleRpm:F0}")); }

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
        FilePath      = path;
        FileName      = Path.GetFileName(path);
        var lines     = File.ReadAllLines(path);
        _programLines = [.. lines];
        _parser.Load(lines);
        TotalLines    = _parser.Lines.Count;
        CurrentLine   = 0;
        Progress      = 0;
        TaskTime      = "0 ms.";
        TimeToComp    = "--:--";
        StartFromLine = 0;
        _streamIdx    = 0;
        _streamPaused = false;
        IsRunning     = false;
        OnPropertyChanged(nameof(CurrentLineText));
        StartCycleCommand.NotifyCanExecuteChanged();
        AddLog($"[FICHEIRO] {FileName} — {TotalLines} linhas carregadas.");
        StatusText = $"Ficheiro: {FileName}  ({TotalLines} linhas)";
    }

    public string GetGCodeText() => string.Join("\n", _programLines);

    public void SetCurrentLine(int line)
    {
        CurrentLine = line;
        Progress    = TotalLines > 0 ? (double)line / TotalLines * 100 : 0;
        OnPropertyChanged(nameof(CurrentLineText));
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
        var inner = line.TrimStart('<').TrimEnd('>');

        // GRBL 0.9: <Idle,MPos:0,0,0,Buf:15,RX:0>  (comma-separated)
        // GRBL 1.1: <Idle|MPos:0.000,0.000,0.000|FS:0,0>  (pipe-separated)
        string[] parts = _controller.PipeSeparatedStatus
            ? inner.Split('|')
            : inner.Split(',');

        // For GRBL 0.9 the MPos value is embedded in commas too, so we look for MPos: token
        if (parts.Length > 0)
        {
            var state = parts[0].Trim();
            // GRBL 0.9 may have "Idle" as part of first segment after rebuilding
            StatusText = state switch
            {
                "Idle"          => "Idle",
                "Run"           => "Running...",
                "Hold:0"        => "Feed hold.",
                "Hold:1"        => "Feed hold (door).",
                "Hold"          => "Feed hold.",
                "Jog"           => "Jogging...",
                "Alarm"         => "ALARM — verifique a máquina!",
                "Door:0"        => "Door open.",
                "Check"         => "Check mode (dry run).",
                "Home"          => "Homing...",
                "Sleep"         => "Sleep.",
                _               => state
            };

            // Auto-suggest $X unlock on alarm for supported controllers
            if (state.StartsWith("Alarm") && _controller.SupportsUnlockCmd)
                StatusText += "  →  envie $X para desbloquear";
        }

        // Find MPos or WPos field regardless of separator
        string? mpos = null;
        foreach (var p in parts)
        {
            var t = p.Trim();
            if (t.StartsWith("MPos:") || t.StartsWith("WPos:")) { mpos = t; break; }
        }
        if (mpos is null) return;

        var coords = mpos[5..].Split(',');
        double Parse(int i) => coords.Length > i && double.TryParse(coords[i],
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : 0;
        PosX = Parse(0); PosY = Parse(1); PosZ = Parse(2);
        PosA = Parse(3); PosB = Parse(4); PosC = Parse(5);
    }

    [RelayCommand(CanExecute = nameof(CanUnlock))]
    private void UnlockAlarm()
    {
        if (_controller.SupportsUnlockCmd)
        {
            _serial.Send("$X");
            AddLog("[UNLOCK] $X enviado — a aguardar confirmação da placa…");
        }
        else
        {
            AddLog("[UNLOCK] Este controlador não suporta $X. Use Reset (Ctrl+X).");
        }
    }
    private bool CanUnlock() => Connected;

    // ── GRBL board settings ───────────────────────────────────────────────
    [RelayCommand]
    private void ReadGrblSettings()
    {
        AddLog("[GRBL] Requesting board settings ($$)…");
        SendCommand("$$");
    }

    [RelayCommand]
    private void WriteGrblSettings()
    {
        AddLog("[GRBL] Writing settings to board…");
        foreach (var cmd in GrblSettings.ToCommands())
        {
            _serial.Send(cmd);
            AddLog($"> {cmd}");
        }
        AddLog("[GRBL] All settings sent.");
    }

    public void WriteGrblCommands(IEnumerable<string> commands)
    {
        AddLog("[GRBL] Writing settings to board…");
        foreach (var cmd in commands)
        {
            _serial.Send(cmd);
            AddLog($"> {cmd}");
        }
        AddLog("[GRBL] All settings sent.");
    }

    // ── GRBL error/alarm code tables ─────────────────────────────────────
    private static readonly Dictionary<int, string> GrblErrors = new()
    {
        {1,  "Letra de comando G-code em falta"},
        {2,  "Valor numérico inválido no G-code"},
        {3,  "Comando '$' não reconhecido"},
        {4,  "Valor negativo não permitido aqui"},
        {5,  "Configurações EEPROM inválidas — valores padrão usados"},
        {6,  "A linha de comando '$' não pode ser usada enquanto a máquina está em movimento"},
        {7,  "A mensagem '$' não pode ser usada durante o ciclo"},
        {8,  "Paragem imediata de jog ativa"},
        {9,  "Máquina bloqueada (ALARM) — envie $X para desbloquear"},
        {10, "Posição zero da ferramenta — desactivada"},
        {11, "Limite de linha de G-code excedido"},
        {12, "A caractere de continuação de linha ':' não é suportado"},
        {13, "Comando não suportado"},
        {14, "Código de arco inválido (I/J/R em conflito)"},
        {15, "Destino do jog excede os limites do espaço de trabalho (soft limits)"},
        {16, "Valor de $J inválido — verifique o formato"},
        {17, "Laser mode requer saída PWM ($32=1 e pino laser configurado)"},
        {20, "Unsupported or invalid g-code command found in block."},
        {21, "More than one g-code command from same modal group found"},
        {22, "Feed rate has not yet been set or is undefined"},
        {23, "G-code command in block requires an integer value"},
        {24, "Two G-code commands that both require the use of the XYZ axis words were detected"},
        {25, "A G-code word was repeated in the block"},
        {26, "A G-code command implicitly or explicitly requires XYZ axis words in the block, but none were detected"},
        {27, "N line number value is not within the valid range of 1 - 9,999,999"},
        {28, "A G-code command was sent, but is missing some required P or L value words in the block"},
        {29, "Grbl supports six work coordinate systems G54-G59. G59.1, G59.2, and G59.3 are not supported"},
        {30, "The G53 G-code command requires either a G0 seek or G1 feed motion mode to be active"},
        {31, "There are unused axis words in the block and G80 motion mode cancel is active"},
        {32, "A G2 or G3 arc was commanded but there are no XYZ axis words in the selected plane to trace the arc"},
        {33, "The motion command has an invalid target"},
        {34, "A G2 or G3 arc, when converted from radius format to center format, is too small to draw"},
        {35, "Grbl does not support G2/G3 arcs in the non-XY plane"},
        {36, "The P word is not allowed to be negative in G10 L_ commands"},
        {37, "Homing cycle cannot start when a limit switch is engaged"},
        {38, "Offset value would cause a motion that would exceed the machine's maximum travel"},
    };

    private static readonly Dictionary<int, string> GrblAlarms = new()
    {
        {1,  "Hard limit ativo — limite físico atingido"},
        {2,  "Soft limit — o movimento excede a área de trabalho"},
        {3,  "Reset durante movimento — a posição pode estar perdida"},
        {4,  "Probe falhou — sinal ativo antes de começar"},
        {5,  "Probe falhou — sem contacto durante o ciclo"},
        {6,  "Reset de homing falhou — a máquina não conseguiu encontrar a posição"},
        {7,  "Door — porta aberta durante o ciclo"},
        {8,  "Falha no pull-off do homing (switch sempre ativo)"},
        {9,  "Homing falhou — limite não encontrado dentro do espaço de busca"},
        {10, "Homing falhou — segundo switch de limite ativo ao iniciar"},
        {11, "Safety door abert durante jog"},
    };

    private static string TranslateGrblError(string raw)
    {
        // raw = "error:9" or "ALARM:1"
        if (raw.StartsWith("error:", StringComparison.OrdinalIgnoreCase))
        {
            if (int.TryParse(raw[6..], out int code) && GrblErrors.TryGetValue(code, out var msg))
                return $"ERRO {code}: {msg}";
            return $"ERRO: {raw[6..]}";
        }
        if (raw.StartsWith("ALARM:", StringComparison.OrdinalIgnoreCase))
        {
            if (int.TryParse(raw[6..], out int code) && GrblAlarms.TryGetValue(code, out var msg))
                return $"ALARM {code}: {msg}";
            return $"ALARM: {raw[6..]}";
        }
        return raw;
    }

    private void ParseGrblOk(string line)
    {
        // capture $N=V lines that the board returns after $$
        if (line.StartsWith('$') && line.Contains('=') && line.Length > 2)
        {
            GrblSettings.ApplyLine(line);
            GrblSettingLineReceived?.Invoke(line);
            return;
        }

        // ALARM: line (e.g. "ALARM:9")
        if (line.StartsWith("ALARM:", StringComparison.OrdinalIgnoreCase))
        {
            var msg = TranslateGrblError(line);
            AddLog($"⚠ {msg}");
            StatusText = $"⚠ {msg}";
            if (_controller.SupportsUnlockCmd)
            {
                AddLog("  → Clique em UNLOCK ($X) ou faça Reset para continuar.");
                StatusText += "  — clique UNLOCK";
            }
            IsAlarm = true;
            return;
        }

        if (line.Trim() == "ok")
        {
            IsAlarm = false;
            if (IsRunning && !_streamPaused)
            {
                // A linha _streamIdx foi confirmada — avança e envia a próxima
                SetCurrentLine(_streamIdx);
                _streamIdx++;
                StreamSendNext();
            }
            return;
        }

        if (line.StartsWith("error:", StringComparison.OrdinalIgnoreCase))
        {
            var msg = TranslateGrblError(line);
            AddLog($"✖ {msg}");
            StatusText = msg;

            // error:9 = machine locked → suggest unlock
            if (line == "error:9" && _controller.SupportsUnlockCmd)
            {
                StatusText += "  — clique UNLOCK ($X)";
                IsAlarm = true;
            }
            return;
        }

        // Grbl startup banner: "Grbl 1.1h ['$' for help]"  or  "Grbl 0.9j ['$' for help]"
        if (line.StartsWith("Grbl ", StringComparison.OrdinalIgnoreCase))
        {
            AddLog($"[FIRMWARE] {line}");
            StatusText = $"Ligado: {line}";
            IsAlarm = false;

            // Auto-detect version and switch controller if still on default
            var tokens = line.Split(' ');
            if (tokens.Length >= 2)
            {
                var ver = tokens[1]; // e.g. "1.1h" or "0.9j"
                var autoType = ver.StartsWith("0.") ? ControllerType.Grbl09
                             : ver.StartsWith("1.") ? ControllerType.Grbl11
                             : _controller.Type;

                if (autoType != _controller.Type)
                {
                    SetControllerType(autoType);
                    AddLog($"[AUTO] Versão GRBL detectada: {ver} → modo {_controller.Name}");
                }
            }

            // Send $$ to populate GRBL settings after banner
            Task.Delay(200).ContinueWith(_ =>
                Application.Current.Dispatcher.Invoke(() => _serial.Send("$$")));
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
        _pollTimer?.Dispose();
        _serial.Dispose();
    }
}
