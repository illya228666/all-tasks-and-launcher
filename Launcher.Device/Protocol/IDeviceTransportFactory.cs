namespace Launcher.Device.Protocol;
internal interface IDeviceTransportFactory
{
    IReadOnlyList<string> GetPortNames();
    IDeviceTransport Open(string portName);
}
