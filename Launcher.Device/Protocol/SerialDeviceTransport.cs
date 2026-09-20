using System.IO.Ports;
namespace Launcher.Device.Protocol;
internal sealed class SerialDeviceTransport : IDeviceTransport
{
    private readonly SerialPort _port;
    internal SerialDeviceTransport(string portName)
    {
        _port = new(portName, 115200, Parity.None, 8, StopBits.One)
        {
            Handshake = Handshake.None, NewLine = "\n", DtrEnable = false, RtsEnable = false,
            ReadTimeout = SerialExchange.ResponseTimeoutMs, WriteTimeout = SerialExchange.WriteTimeoutMs
        };
        try { _port.Open(); }
        catch { _port.Dispose(); throw; }
    }
    public string Exchange(string command, CancellationToken cancellation) => SerialExchange.Send(_port, command, cancellation);
    public void Dispose() => _port.Dispose();
}
