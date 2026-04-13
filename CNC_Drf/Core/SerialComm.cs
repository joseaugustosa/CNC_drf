namespace CNC_Drf.Core;

public class SerialComm : IDisposable
{
    private SerialPort? _port;
    private CancellationTokenSource? _cts;

    public event Action<string>? LineReceived;
    public event Action<bool>?   ConnectionChanged;

    public bool IsConnected => _port?.IsOpen ?? false;

    public void Connect(string portName, int baud)
    {
        Disconnect();
        _port = new SerialPort(portName, baud, Parity.None, 8, StopBits.One)
        {
            ReadTimeout  = 2000,
            WriteTimeout = 2000,
            NewLine      = "\n"
        };
        _port.Open();
        _cts = new CancellationTokenSource();
        _ = ReadLoopAsync(_cts.Token);
        ConnectionChanged?.Invoke(true);
    }

    public void Disconnect()
    {
        _cts?.Cancel();
        _cts = null;
        try { _port?.Close(); } catch { }
        _port?.Dispose();
        _port = null;
        ConnectionChanged?.Invoke(false);
    }

    public void Send(string line)
    {
        if (!IsConnected) return;
        _port!.WriteLine(line);
    }

    private async Task ReadLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _port?.IsOpen == true)
        {
            try
            {
                var line = await Task.Run(() => _port.ReadLine(), ct);
                LineReceived?.Invoke(line.Trim());
            }
            catch (OperationCanceledException) { break; }
            catch { await Task.Delay(100, ct).ConfigureAwait(false); }
        }
    }

    public void Dispose() => Disconnect();
}
