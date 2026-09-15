using System.Diagnostics;
using System.IO.Ports;
using System.Text;
using Launcher.Application;

namespace Launcher.Infrastructure;

public sealed class SerialEspBoardController : IDeviceController
{
    private const int BaudRate = 115200; // Скорость должна совпадать с прошивкой.
    private const int ResponseTimeoutMs = 500; // Общий срок ответа, включая посторонние строки.
    private const int WriteTimeoutMs = 500; // Ограничение ожидания записи.
    private const int StartupDelayMs = 1200; // Время на загрузку ESP после открытия USB-UART.
    private const int DiscoveryRetryMs = 2000;
    private const int MaxReplyLength = 128;

    private readonly object _sync = new();
    private SerialPort? _port;
    private bool _disposed;
    private long _nextDiscoveryAt;

    public Task<DevicePollResult> PollAsync(CancellationToken cancellationToken) =>
        RunAsync(() =>
        {
            if (_port == null && !TryConnect(cancellationToken))
                return new DevicePollResult(false);

            string reply = Exchange("POLL", cancellationToken, "INPUT NONE", "INPUT BUTTON_PRESSED");
            return new DevicePollResult(true,
                reply == "INPUT BUTTON_PRESSED" ? DeviceInputEvent.PrimaryButtonPressed : null);
        }, new DevicePollResult(false), cancellationToken);

    private bool TryConnect(CancellationToken cancellationToken)
    {
        if (Environment.TickCount64 < _nextDiscoveryAt)
            return false;

            foreach (string portName in SerialPort.GetPortNames().OrderBy(name => name, StringComparer.OrdinalIgnoreCase))
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    _port = new SerialPort(portName, BaudRate, Parity.None, 8, StopBits.One)
                    {
                        Handshake = Handshake.None,
                        NewLine = "\n",
                        ReadTimeout = ResponseTimeoutMs,
                        WriteTimeout = WriteTimeoutMs,
                        DtrEnable = false,
                        RtsEnable = false
                    };
                    _port.Open();
                    if (cancellationToken.WaitHandle.WaitOne(StartupDelayMs))
                        cancellationToken.ThrowIfCancellationRequested();
                    // Версия протокола проверяется до команд. HELLO сбрасывает старые нажатия.
                    Exchange("Zdarowa", cancellationToken, "Zaebal 1");
                    return true;
                }
                catch (Exception ex) when (IsConnectionError(ex))
                {
                    ClosePort();
                }
            }

        _nextDiscoveryAt = Environment.TickCount64 + DiscoveryRetryMs;
        return false;
    }

    public Task<bool> SetIndicatorAsync(bool enabled, CancellationToken cancellationToken) =>
        RunAsync(() =>
        {
            if (_port == null)
                return false;

            Exchange(enabled ? "INDICATOR ON" : "INDICATOR OFF", cancellationToken, "OK INDICATOR");
            return true;
        }, false, cancellationToken);

    private Task<T> RunAsync<T>(Func<T> operation, T unavailable, CancellationToken cancellationToken) => Task.Run(() =>
    {
        // Один замок защищает весь обмен и Dispose; блокирующий serial IO выполняется вне UI.
        lock (_sync)
        {
            if (_disposed)
                return unavailable;

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                return operation();
            }
            catch (OperationCanceledException)
            {
                ClosePort();
                throw;
            }
            catch (Exception ex) when (IsConnectionError(ex))
            {
                ClosePort();
                return unavailable;
            }
        }
    });

    private string Exchange(string command, CancellationToken cancellationToken, params string[] expectedReplies)
    {
        cancellationToken.ThrowIfCancellationRequested();
        SerialPort port = _port!;
        // Протокол только request/reply: события хранятся на плате до POLL,
        // поэтому очистка старого ответа не теряет несчитанное нажатие.
        port.DiscardInBuffer();
        port.WriteLine(command);
        var elapsed = Stopwatch.StartNew();
        var line = new StringBuilder();

        while (elapsed.ElapsedMilliseconds < ResponseTimeoutMs)
        {
            cancellationToken.ThrowIfCancellationRequested();
            port.ReadTimeout = Math.Max(1, ResponseTimeoutMs - (int)elapsed.ElapsedMilliseconds);
            char character = (char)port.ReadChar();
            cancellationToken.ThrowIfCancellationRequested();
            if (character == '\r')
                continue;
            if (character != '\n')
            {
                if (line.Length >= MaxReplyLength)
                    throw new IOException("Device reply is too long.");
                line.Append(character);
                continue;
            }
            string reply = line.ToString();
            line.Clear();
            if (expectedReplies.Contains(reply, StringComparer.Ordinal))
                return reply;
            // Boot/startup output пропускается, но не продлевает общий timeout.
        }

        throw new TimeoutException("ESP reply was not received.");
    }

    private static bool IsConnectionError(Exception ex) =>
        ex is IOException or UnauthorizedAccessException or TimeoutException or InvalidOperationException or ArgumentException;

    private void ClosePort()
    {
        SerialPort? port = _port;
        _port = null;
        _nextDiscoveryAt = Environment.TickCount64 + DiscoveryRetryMs;
        try
        {
            port?.Dispose();
        }
        catch (Exception ex) when (IsConnectionError(ex))
        {
            // Отключённый USB-UART может сообщить ошибку даже при закрытии.
            Debug.WriteLine(ex);
        }
    }

    public void Dispose()
    {
        lock (_sync)
        {
            _disposed = true;
            ClosePort();
        }
    }
}
