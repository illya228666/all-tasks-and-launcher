using System.Diagnostics;
using System.IO.Ports;
using System.Text;

namespace Launcher.Device.Protocol;
internal static class SerialExchange
{
    internal const int ResponseTimeoutMs = 500;
    internal const int WriteTimeoutMs = 500;
    private const int MaxReplyLength = 128;
    internal static string Send(SerialPort port, string command, CancellationToken cancellation)
    {
        cancellation.ThrowIfCancellationRequested();
        port.DiscardInBuffer();
        port.WriteLine(command);
        var elapsed = Stopwatch.StartNew();
        var line = new StringBuilder();
        while (elapsed.ElapsedMilliseconds < ResponseTimeoutMs)
        {
            cancellation.ThrowIfCancellationRequested();
            port.ReadTimeout = Math.Max(1, ResponseTimeoutMs - (int)elapsed.ElapsedMilliseconds);
            char character = (char)port.ReadChar();
            cancellation.ThrowIfCancellationRequested();
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
            if (command != DeviceProtocol.Hello || reply.StartsWith("LAUNCHER_IO ", StringComparison.Ordinal) || reply.StartsWith("ERR ", StringComparison.Ordinal))
                return reply;
        }

        throw new TimeoutException("Device reply was not received.");
    }
}
