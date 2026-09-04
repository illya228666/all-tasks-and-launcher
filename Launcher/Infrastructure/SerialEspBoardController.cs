using System.Diagnostics;
using System.IO.Ports;
using Launcher.Application;

namespace Launcher.Infrastructure;

public sealed class SerialEspBoardController : IEspBoardController
{
    private const int BaudRate = 115200; // Скорость должна совпадать с прошивкой.
    private const int ResponseTimeoutMs = 500; // Общий срок ответа, включая посторонние строки.
    private const int WriteTimeoutMs = 500; // Ограничение ожидания записи.
    private const int StartupDelayMs = 250; // Пауза после открытия для запуска платы; при необходимости увеличить.

    private readonly object _sync = new();
    private SerialPort? _port;
    private bool _disposed;

    public Task<bool> CheckAvailabilityAsync(CancellationToken cancellationToken) =>
        RunAsync(() =>
        {
            if (_port != null)
            {
                Exchange("PING", "ESP_LAUNCHER_OK", cancellationToken);
                return true;
            }

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
                    Exchange("PING", "ESP_LAUNCHER_OK", cancellationToken);
                    return true;
                }
                catch (Exception ex) when (IsConnectionError(ex))
                {
                    ClosePort();
                }
            }

            return false;
        }, cancellationToken);

    public Task<bool> SetD1Async(bool enabled, CancellationToken cancellationToken) =>
        RunAsync(() =>
        {
            if (_port == null)
                return false;

            Exchange(enabled ? "LED D1 ON" : "LED D1 OFF", "OK", cancellationToken);
            return true;
        }, cancellationToken);

    private Task<bool> RunAsync(Func<bool> operation, CancellationToken cancellationToken) => Task.Run(() =>
    {
        // Один замок защищает весь обмен и Dispose; блокирующий serial IO выполняется вне UI.
        lock (_sync)
        {
            if (_disposed)
                return false;

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
                return false;
            }
        }
    });

    private void Exchange(string command, string expectedReply, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        SerialPort port = _port!;
        port.DiscardInBuffer(); // Старое OK не должно подтверждать следующую команду.
        port.WriteLine(command);
        var elapsed = Stopwatch.StartNew();

        while (elapsed.ElapsedMilliseconds < ResponseTimeoutMs)
        {
            cancellationToken.ThrowIfCancellationRequested();
            port.ReadTimeout = Math.Max(1, ResponseTimeoutMs - (int)elapsed.ElapsedMilliseconds);
            string reply = port.ReadLine().Trim('\r', '\n');
            cancellationToken.ThrowIfCancellationRequested();
            if (reply == expectedReply)
                return;
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
