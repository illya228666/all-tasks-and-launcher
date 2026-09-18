using System.IO.Ports;
using System.Threading.Channels;
using Launcher.Device.Data;
using Launcher.Device.Protocol;

namespace Launcher.Device.Connection;
// RU: Один цикл владеет портом и очередью команд. DE: Ein Ablauf besitzt Port und Befehlswarteschlange.
public sealed class DeviceConnection : IAsyncDisposable
{
    private const int BaudRate = 115200;
    private const int StartupDelayMs = 1200;
    private const int DiscoveryRetryMs = 2000;
    private const int PollIntervalMs = 100;
    private readonly Channel<bool> _commands = Channel.CreateUnbounded<bool>(new() { SingleReader = true });
    private readonly CancellationTokenSource _lifetime = new();
    private Task _loop = Task.CompletedTask;
    private SerialPort? _port;
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
        _loop = Task.Run(RunAsync);
    }

    public bool SetIndicator(bool enabled) => !_stopped && _commands.Writer.TryWrite(enabled);
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
                    if (_commands.Reader.TryRead(out bool enabled))
                    {
                        bool available = _port is not null;
                        if (available)
                        {
                            try
                            {
                                SerialExchange.Send(_port!, DeviceProtocol.Indicator(enabled), token, DeviceProtocol.IndicatorReply);
                            }
                            catch (Exception error) when (IsConnectionError(error))
                            {
                                available = false;
                                ClosePort();
                            }
                        }

                        IndicatorCompleted?.Invoke(enabled, available);
                    }

                    if (_port is not null)
                    {
                        string reply = SerialExchange.Send(_port, DeviceProtocol.Poll, token, DeviceProtocol.NoInput, DeviceProtocol.ButtonPressed);
                        if (DeviceProtocol.ReadInput(reply) is DeviceInput input)
                            InputReceived?.Invoke(input);
                    }
                }
                catch (Exception error) when (IsConnectionError(error))
                {
                    ClosePort();
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

    private async Task ConnectAsync(CancellationToken token)
    {
        if (Environment.TickCount64 < _nextDiscoveryAt)
            return;
        foreach (string name in SerialPort.GetPortNames().OrderBy(value => value, StringComparer.OrdinalIgnoreCase))
        {
            token.ThrowIfCancellationRequested();
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
                SerialExchange.Send(_port, DeviceProtocol.Hello, token, DeviceProtocol.Identity);
                StateChanged?.Invoke(new(true, name));
                return;
            }
            catch (Exception error) when (IsConnectionError(error))
            {
                ClosePort();
            }
        }

        _nextDiscoveryAt = Environment.TickCount64 + DiscoveryRetryMs;
    }

    private static bool IsConnectionError(Exception error) => error is IOException or UnauthorizedAccessException or TimeoutException or InvalidOperationException or ArgumentException;
    private void ClosePort()
    {
        SerialPort? port = _port;
        _port = null;
        _nextDiscoveryAt = Environment.TickCount64 + DiscoveryRetryMs;
        try
        {
            port?.Dispose();
        }
        catch (Exception error) when (IsConnectionError(error))
        {
            System.Diagnostics.Debug.WriteLine(error);
        }

        if (port is not null)
            StateChanged?.Invoke(new(false));
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
