namespace Launcher.Device.Protocol;
internal interface IDeviceTransport : IDisposable
{
    string Exchange(string command, CancellationToken cancellation);
}
