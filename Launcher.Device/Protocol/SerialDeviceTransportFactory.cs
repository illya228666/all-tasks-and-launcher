using System.IO.Ports;
namespace Launcher.Device.Protocol;
internal sealed class SerialDeviceTransportFactory : IDeviceTransportFactory
{
    public IReadOnlyList<string> GetPortNames() => SerialPort.GetPortNames().OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToArray();
    public IDeviceTransport Open(string portName) => new SerialDeviceTransport(portName);
}
