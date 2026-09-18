using System.IO.Ports;
using System.Threading.Channels;
using Launcher.Device.Data;
using Launcher.Device.Protocol;

namespace Launcher.Device.Connection;
// RU: Один цикл владеет портом и очередью команд. DE: Ein Ablauf besitzt Port und Befehlswarteschlange.
public sealed class DeviceConnection : IAsyncDisposable
{
    private const int BaudRate = 115200;
    private const int StartupDelayMs = 1600;
    private const int DiscoveryRetryMs = 2000;
    private const int PollIntervalMs = 100;
    private const int HelloRetryDelayMs = 250;

    private enum CommandKind
    {
        Indicator,
        Lights
    }

    private readonly record struct DeviceCommand(CommandKind Kind, bool IndicatorEnabled, DeviceLights Lights);

    private readonly Channel<DeviceCommand> _commands = Channel.CreateUnbounded<DeviceCommand>(new() { SingleReader = true });
    private readonly CancellationTokenSource _lifetime = new();
    private Task _loop = Task.CompletedTask;
    private SerialPort? _port;
    private IDeviceProtocolVersion? _protocol;
    private bool _started;
    private bool _stopped;
    private bool _disposed;
    private long _nextDiscoveryAt;

    public event Action<DeviceState>? StateChanged;
    public event Action<DeviceInput>? InputReceived;
    public event Action<bool, bool>? IndicatorCompleted;
    public event Action<string>? Failed;

    public void Start()
    {
        if (_started || _stopped)
            return;

        _started = true;
        StateChanged?.Invoke(new(DeviceConnectionPhase.Searching, Message: "Suche nach Controller..."));
        _loop = Task.Run(RunAsync);
    }

    public bool SetIndicator(bool enabled) =>
        !_stopped && _commands.Writer.TryWrite(new(CommandKind.Indicator, enabled, DeviceLights.None));

    public bool SetLights(DeviceLights lights) =>
        !_stopped && _commands.Writer.TryWrite(new(CommandKind.Lights, false, lights));

    private async Task RunAsync()
    {
        CancellationToken token = _lifetime.Token;
        try
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (_port is null)
                        await ConnectAsync(token).ConfigureAwait(false);

                    if (_commands.Reader.TryRead(out DeviceCommand command))
                        ExecuteCommand(command, token);

                    if (_port is not null && _protocol is not null)
                    {
                        string reply = SerialExchange.Send(_port, _protocol.PollCommand, token, _protocol.PollReplies);
                        if (_protocol.ReadInput(reply) is DeviceInput input)
                            InputReceived?.Invoke(input);
                    }
                }
                catch (Exception error) when (IsConnectionError(error))
                {
                    Disconnect("Verbindung verloren: " + FriendlyMessage(error));
                }

                await Task.Delay(PollIntervalMs, token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
        }
        catch (Exception error)
        {
            Failed?.Invoke(error.Message);
        }
        finally
        {
            ClosePort();
        }
    }

    private void ExecuteCommand(DeviceCommand command, CancellationToken token)
    {
        bool available = _port is not null && _protocol is not null;
        if (!available)
        {
            if (command.Kind == CommandKind.Indicator)
                IndicatorCompleted?.Invoke(command.IndicatorEnabled, false);
            return;
        }

        DeviceWireCommand? wire = command.Kind switch
        {
            CommandKind.Indicator => _protocol!.Indicator(command.IndicatorEnabled),
            CommandKind.Lights => _protocol!.Lights(command.Lights),
            _ => null
        };

        if (wire is null)
        {
            if (command.Kind == CommandKind.Indicator)
                IndicatorCompleted?.Invoke(command.IndicatorEnabled, false);
            return;
        }

        try
        {
            SerialExchange.Send(_port!, wire.Value.Command, token, wire.Value.ExpectedReply);
        }
        catch (Exception error) when (IsConnectionError(error))
        {
            available = false;
            Disconnect("Befehl fehlgeschlagen: " + FriendlyMessage(error));
        }

        if (command.Kind == CommandKind.Indicator)
            IndicatorCompleted?.Invoke(command.IndicatorEnabled, available);
    }

    private async Task ConnectAsync(CancellationToken token)
    {
        if (Environment.TickCount64 < _nextDiscoveryAt)
            return;

        string[] ports = SerialPort.GetPortNames()
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (ports.Length == 0)
        {
            StateChanged?.Invoke(new(DeviceConnectionPhase.Retry, Message: "Kein COM-Port gefunden. Neuer Versuch..."));
            _nextDiscoveryAt = Environment.TickCount64 + DiscoveryRetryMs;
            return;
        }

        string? lastFailure = null;
        foreach (string name in ports)
        {
            token.ThrowIfCancellationRequested();
            StateChanged?.Invoke(new(DeviceConnectionPhase.Connecting, name, Message: "Pruefe " + name + "..."));

            try
            {
                _port = new SerialPort(name, BaudRate, Parity.None, 8, StopBits.One)
                {
                    Handshake = Handshake.None,
                    NewLine = "\n",
                    DtrEnable = false,
                    RtsEnable = false,
                    ReadTimeout = SerialExchange.ResponseTimeoutMs,
                    WriteTimeout = SerialExchange.WriteTimeoutMs
                };

                _port.Open();
                await Task.Delay(StartupDelayMs, token).ConfigureAwait(false);

                string identity;
                try
                {
                    identity = SerialExchange.Send(_port, DeviceProtocol.Hello, token, DeviceProtocol.Identities);
                }
                catch (TimeoutException)
                {
                    await Task.Delay(HelloRetryDelayMs, token).ConfigureAwait(false);
                    identity = SerialExchange.Send(_port, DeviceProtocol.Hello, token, DeviceProtocol.Identities);
                }

                _protocol = DeviceProtocol.FromIdentity(identity);
                StateChanged?.Invoke(new(
                    DeviceConnectionPhase.Connected,
                    name,
                    _protocol.Version,
                    $"Verbunden: {name} | LAUNCHER_IO {(int)_protocol.Version}"));
                return;
            }
            catch (Exception error) when (IsConnectionError(error))
            {
                lastFailure = name + ": " + FriendlyMessage(error);
                ClosePort();
            }
        }

        StateChanged?.Invoke(new(
            DeviceConnectionPhase.Retry,
            Message: lastFailure is null
                ? "Kein kompatibler Controller gefunden. Neuer Versuch..."
                : lastFailure + " | Neuer Versuch..."));

        _nextDiscoveryAt = Environment.TickCount64 + DiscoveryRetryMs;
    }

    private static bool IsConnectionError(Exception error) =>
        error is IOException or UnauthorizedAccessException or TimeoutException or InvalidOperationException or ArgumentException;

    private static string FriendlyMessage(Exception error) => error switch
    {
        UnauthorizedAccessException => "Port ist belegt oder Zugriff wurde verweigert. Arduino Serial Monitor schliessen.",
        TimeoutException => "keine LAUNCHER_IO-Antwort",
        IOException => string.IsNullOrWhiteSpace(error.Message) ? "I/O-Fehler" : error.Message,
        _ => string.IsNullOrWhiteSpace(error.Message) ? error.GetType().Name : error.Message
    };

    private void Disconnect(string message)
    {
        string? portName = _port?.PortName;
        ClosePort();
        StateChanged?.Invoke(new(DeviceConnectionPhase.Retry, portName, Message: message + " | Neuer Versuch..."));
    }

    private void ClosePort()
    {
        SerialPort? port = _port;
        _port = null;
        _protocol = null;
        _nextDiscoveryAt = Environment.TickCount64 + DiscoveryRetryMs;

        try
        {
            port?.Dispose();
        }
        catch (Exception error) when (IsConnectionError(error))
        {
            System.Diagnostics.Debug.WriteLine(error);
        }
    }

    public async Task StopAsync()
    {
        if (!_stopped)
        {
            _stopped = true;
            _commands.Writer.TryComplete();
            _lifetime.Cancel();
        }

        await _loop.ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;
        try
        {
            await StopAsync().ConfigureAwait(false);
        }
        finally
        {
            _lifetime.Dispose();
        }
    }
}
