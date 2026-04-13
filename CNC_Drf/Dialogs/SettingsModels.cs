namespace CNC_Drf.Dialogs;

// ── Controller type selection ─────────────────────────────────────────────────

public enum ControllerType
{
    // GRBL family
    Grbl09,          // GRBL 0.9  – legacy Arduino UNO/Mega, jog via G91 G1
    Grbl11,          // GRBL 1.1  – current standard, jog via $J=, real-time overrides
    GrblHAL,         // grblHAL   – multi-hardware, extended status, $I info
    GrblESP32,       // GRBL_ESP32 by bdring – WiFi, WebUI, $I info
    GrblLPC,         // GRBL-LPC  – LPC1768/1769 (Smoothieware-class boards)
    GrblMega5X,      // GRBL Mega-5X – 5-axis support on Arduino Mega
    GrblSTM32,       // GRBL-STM32 – STM32 port
    // Other controllers
    Mach3,           // Mach3 / Mach4 – Windows LPT/USB plugin
}

public class ControllerProfile
{
    public ControllerType Type         { get; set; } = ControllerType.Grbl11;
    public string         Name         { get; }
    public string         Icon         { get; }
    public string         Description  { get; }
    public string         Hardware     { get; }
    public bool           IsGrbl       { get; }
    public bool           SupportsJogCmd       { get; } // $J= command
    public bool           SupportsRealTimeOvr  { get; } // 0xF0-0xFF overrides
    public bool           SupportsUnlockCmd    { get; } // $X unlock alarm
    public bool           PipeSeparatedStatus  { get; } // <State|MPos:...> vs <State,MPos:...>
    public string         StatusInitCmd        { get; } // command after connect

    private ControllerProfile(
        ControllerType type, string name, string icon, string description, string hardware,
        bool isGrbl, bool jogCmd, bool rtOvr, bool unlock, bool pipeSep, string initCmd)
    {
        Type = type; Name = name; Icon = icon; Description = description; Hardware = hardware;
        IsGrbl = isGrbl;
        SupportsJogCmd      = jogCmd;
        SupportsRealTimeOvr = rtOvr;
        SupportsUnlockCmd   = unlock;
        PipeSeparatedStatus = pipeSep;
        StatusInitCmd       = initCmd;
    }

    public static readonly IReadOnlyList<ControllerProfile> All = new List<ControllerProfile>
    {
        new(ControllerType.Grbl09,      "GRBL 0.9",      "⬡",
            "Versão legada. Jog via G91/G90. Status com vírgula como separador.",
            "Arduino UNO / Nano",
            isGrbl:true, jogCmd:false, rtOvr:false, unlock:false, pipeSep:false, initCmd:"?"),

        new(ControllerType.Grbl11,      "GRBL 1.1",      "⬡",
            "Versão atual e mais comum. Jog via $J=. Overrides em tempo real. Desbloqueio $X.",
            "Arduino UNO / Nano / Mega",
            isGrbl:true, jogCmd:true, rtOvr:true, unlock:true, pipeSep:true, initCmd:"?"),

        new(ControllerType.GrblHAL,     "grblHAL",       "⬡",
            "Versão avançada com abstração de hardware. Suporta múltiplos processadores.",
            "STM32, iMXRT, ESP32, RP2040",
            isGrbl:true, jogCmd:true, rtOvr:true, unlock:true, pipeSep:true, initCmd:"?"),

        new(ControllerType.GrblESP32,   "GRBL-ESP32",    "⬡",
            "Port do GRBL para ESP32 com WiFi, Bluetooth e WebUI integrado.",
            "ESP32 (bdring FluidNC / older GRBL_ESP32)",
            isGrbl:true, jogCmd:true, rtOvr:true, unlock:true, pipeSep:true, initCmd:"?"),

        new(ControllerType.GrblLPC,     "GRBL-LPC",      "⬡",
            "Port para processadores LPC. Compatível com placas tipo Smoothieboard.",
            "LPC1768 / LPC1769 (Re-ARM, MKS SBASE)",
            isGrbl:true, jogCmd:true, rtOvr:true, unlock:true, pipeSep:true, initCmd:"?"),

        new(ControllerType.GrblMega5X,  "GRBL Mega-5X",  "⬡",
            "Versão com suporte a 5 eixos para Arduino Mega.",
            "Arduino Mega 2560",
            isGrbl:true, jogCmd:true, rtOvr:true, unlock:true, pipeSep:true, initCmd:"?"),

        new(ControllerType.GrblSTM32,   "GRBL-STM32",    "⬡",
            "Port oficial para microcontroladores STM32. Alta performance.",
            "STM32F1 / F4 / F7",
            isGrbl:true, jogCmd:true, rtOvr:true, unlock:true, pipeSep:true, initCmd:"?"),

        new(ControllerType.Mach3,       "Mach3 / Mach4", "🖥",
            "Software CNC para Windows via LPT, USB ou plugin Ethernet.",
            "PC Windows + hardware LPT / Motion controller",
            isGrbl:false, jogCmd:false, rtOvr:false, unlock:false, pipeSep:false, initCmd:""),
    };

    public static ControllerProfile Get(ControllerType t)
        => All.First(p => p.Type == t);
}

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

/// <summary>
/// Mapeia todos os parâmetros GRBL lidos via $$ e enviados via $N=V.
/// </summary>
public partial class GrblSettings : ObservableObject
{
    // ── Step signals ──────────────────────────────────────────────────────
    [ObservableProperty] private int    _stepPulse      = 10;   // $0  μs
    [ObservableProperty] private int    _stepIdleDelay  = 25;   // $1  ms

    // ── Pin inversion masks ───────────────────────────────────────────────
    [ObservableProperty] private int    _stepPortInvert = 0;    // $2  bitmask
    [ObservableProperty] private int    _dirPortInvert  = 0;    // $3  bitmask

    // Helpers per-axis para $3 (bit 0=X, 1=Y, 2=Z)
    public bool XDirInvert
    {
        get => (DirPortInvert & 1) != 0;
        set { DirPortInvert = value ? DirPortInvert | 1 : DirPortInvert & ~1; OnPropertyChanged(); }
    }
    public bool YDirInvert
    {
        get => (DirPortInvert & 2) != 0;
        set { DirPortInvert = value ? DirPortInvert | 2 : DirPortInvert & ~2; OnPropertyChanged(); }
    }
    public bool ZDirInvert
    {
        get => (DirPortInvert & 4) != 0;
        set { DirPortInvert = value ? DirPortInvert | 4 : DirPortInvert & ~4; OnPropertyChanged(); }
    }
    [ObservableProperty] private bool   _stepEnableInvert = false; // $4
    [ObservableProperty] private bool   _limitPinsInvert  = false; // $5
    [ObservableProperty] private bool   _probePinInvert   = false; // $6

    // ── Status / tolerances ───────────────────────────────────────────────
    [ObservableProperty] private int    _statusReportMask  = 1;     // $10
    [ObservableProperty] private double _junctionDeviation = 0.010; // $11 mm
    [ObservableProperty] private double _arcTolerance      = 0.002; // $12 mm
    [ObservableProperty] private bool   _reportInches      = false; // $13

    // ── Limits ────────────────────────────────────────────────────────────
    [ObservableProperty] private bool   _softLimits = false; // $20
    [ObservableProperty] private bool   _hardLimits = false; // $21

    // ── Homing ────────────────────────────────────────────────────────────
    [ObservableProperty] private bool   _homingCycle     = false; // $22
    [ObservableProperty] private int    _homingDirInvert = 0;     // $23 bitmask
    [ObservableProperty] private double _homingFeed      = 25.0;  // $24 mm/min
    [ObservableProperty] private double _homingSeek      = 500.0; // $25 mm/min
    [ObservableProperty] private int    _homingDebounce  = 250;   // $26 ms
    [ObservableProperty] private double _homingPullOff   = 1.0;   // $27 mm

    // ── Spindle ───────────────────────────────────────────────────────────
    [ObservableProperty] private int    _maxSpindleRpm = 1000; // $30
    [ObservableProperty] private int    _minSpindleRpm = 0;    // $31
    [ObservableProperty] private bool   _laserMode     = false; // $32

    // ── Axis X ────────────────────────────────────────────────────────────
    [ObservableProperty] private double _xStepsPerMm  = 250.0;  // $100
    [ObservableProperty] private double _xMaxRate     = 500.0;  // $110 mm/min
    [ObservableProperty] private double _xAccel       = 10.0;   // $120 mm/s²
    [ObservableProperty] private double _xMaxTravel   = 200.0;  // $130 mm

    // ── Axis Y ────────────────────────────────────────────────────────────
    [ObservableProperty] private double _yStepsPerMm  = 250.0;  // $101
    [ObservableProperty] private double _yMaxRate     = 500.0;  // $111 mm/min
    [ObservableProperty] private double _yAccel       = 10.0;   // $121 mm/s²
    [ObservableProperty] private double _yMaxTravel   = 200.0;  // $131 mm

    // ── Axis Z ────────────────────────────────────────────────────────────
    [ObservableProperty] private double _zStepsPerMm  = 250.0;  // $102
    [ObservableProperty] private double _zMaxRate     = 500.0;  // $112 mm/min
    [ObservableProperty] private double _zAccel       = 10.0;   // $122 mm/s²
    [ObservableProperty] private double _zMaxTravel   = 200.0;  // $132 mm

    /// <summary>Aplica um par "$N=V" recebido do controlador.</summary>
    public void ApplyLine(string line)
    {
        // formato: $100=250.000
        if (!line.StartsWith('$')) return;
        var eq = line.IndexOf('=');
        if (eq < 2) return;
        if (!int.TryParse(line[1..eq], out int id)) return;
        var raw = line[(eq + 1)..].Trim();

        switch (id)
        {
            case 0:   if (int.TryParse(raw, out int v0))   StepPulse      = v0;   break;
            case 1:   if (int.TryParse(raw, out int v1))   StepIdleDelay  = v1;   break;
            case 2:   if (int.TryParse(raw, out int v2))   StepPortInvert = v2;   break;
            case 3:   if (int.TryParse(raw, out int v3))   DirPortInvert  = v3;   break;
            case 4:   StepEnableInvert = raw == "1";                               break;
            case 5:   LimitPinsInvert  = raw == "1";                               break;
            case 6:   ProbePinInvert   = raw == "1";                               break;
            case 10:  if (int.TryParse(raw, out int v10))  StatusReportMask  = v10; break;
            case 11:  if (TryParseInv(raw, out double d11)) JunctionDeviation = d11; break;
            case 12:  if (TryParseInv(raw, out double d12)) ArcTolerance      = d12; break;
            case 13:  ReportInches = raw == "1";                                    break;
            case 20:  SoftLimits   = raw == "1";                                    break;
            case 21:  HardLimits   = raw == "1";                                    break;
            case 22:  HomingCycle  = raw == "1";                                    break;
            case 23:  if (int.TryParse(raw, out int v23))  HomingDirInvert = v23;  break;
            case 24:  if (TryParseInv(raw, out double d24)) HomingFeed      = d24;  break;
            case 25:  if (TryParseInv(raw, out double d25)) HomingSeek      = d25;  break;
            case 26:  if (int.TryParse(raw, out int v26))  HomingDebounce  = v26;  break;
            case 27:  if (TryParseInv(raw, out double d27)) HomingPullOff   = d27;  break;
            case 30:  if (int.TryParse(raw, out int v30))  MaxSpindleRpm   = v30;  break;
            case 31:  if (int.TryParse(raw, out int v31))  MinSpindleRpm   = v31;  break;
            case 32:  LaserMode = raw == "1";                                       break;
            case 100: if (TryParseInv(raw, out double d100)) XStepsPerMm = d100;   break;
            case 101: if (TryParseInv(raw, out double d101)) YStepsPerMm = d101;   break;
            case 102: if (TryParseInv(raw, out double d102)) ZStepsPerMm = d102;   break;
            case 110: if (TryParseInv(raw, out double d110)) XMaxRate    = d110;   break;
            case 111: if (TryParseInv(raw, out double d111)) YMaxRate    = d111;   break;
            case 112: if (TryParseInv(raw, out double d112)) ZMaxRate    = d112;   break;
            case 120: if (TryParseInv(raw, out double d120)) XAccel      = d120;   break;
            case 121: if (TryParseInv(raw, out double d121)) YAccel      = d121;   break;
            case 122: if (TryParseInv(raw, out double d122)) ZAccel      = d122;   break;
            case 130: if (TryParseInv(raw, out double d130)) XMaxTravel  = d130;   break;
            case 131: if (TryParseInv(raw, out double d131)) YMaxTravel  = d131;   break;
            case 132: if (TryParseInv(raw, out double d132)) ZMaxTravel  = d132;   break;
        }
    }

    /// <summary>Gera todas as linhas de comando $N=V para enviar ao controlador.</summary>
    public IEnumerable<string> ToCommands()
    {
        string F(double v) => v.ToString("F3", System.Globalization.CultureInfo.InvariantCulture);
        string B(bool v)   => v ? "1" : "0";
        yield return $"$0={StepPulse}";
        yield return $"$1={StepIdleDelay}";
        yield return $"$2={StepPortInvert}";
        yield return $"$3={DirPortInvert}";
        yield return $"$4={B(StepEnableInvert)}";
        yield return $"$5={B(LimitPinsInvert)}";
        yield return $"$6={B(ProbePinInvert)}";
        yield return $"$10={StatusReportMask}";
        yield return $"$11={F(JunctionDeviation)}";
        yield return $"$12={F(ArcTolerance)}";
        yield return $"$13={B(ReportInches)}";
        yield return $"$20={B(SoftLimits)}";
        yield return $"$21={B(HardLimits)}";
        yield return $"$22={B(HomingCycle)}";
        yield return $"$23={HomingDirInvert}";
        yield return $"$24={F(HomingFeed)}";
        yield return $"$25={F(HomingSeek)}";
        yield return $"$26={HomingDebounce}";
        yield return $"$27={F(HomingPullOff)}";
        yield return $"$30={MaxSpindleRpm}";
        yield return $"$31={MinSpindleRpm}";
        yield return $"$32={B(LaserMode)}";
        yield return $"$100={F(XStepsPerMm)}";
        yield return $"$101={F(YStepsPerMm)}";
        yield return $"$102={F(ZStepsPerMm)}";
        yield return $"$110={F(XMaxRate)}";
        yield return $"$111={F(YMaxRate)}";
        yield return $"$112={F(ZMaxRate)}";
        yield return $"$120={F(XAccel)}";
        yield return $"$121={F(YAccel)}";
        yield return $"$122={F(ZAccel)}";
        yield return $"$130={F(XMaxTravel)}";
        yield return $"$131={F(YMaxTravel)}";
        yield return $"$132={F(ZMaxTravel)}";
    }

    private static bool TryParseInv(string s, out double v)
        => double.TryParse(s, System.Globalization.NumberStyles.Float,
                           System.Globalization.CultureInfo.InvariantCulture, out v);
}
