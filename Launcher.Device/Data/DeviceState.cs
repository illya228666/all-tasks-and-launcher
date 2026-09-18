namespace Launcher.Device.Data;

public sealed record DeviceState(
    DeviceConnectionPhase Phase,
    string? PortName = null,
    DeviceProtocolVersion? ProtocolVersion = null,
    string? Message = null)
{
    public bool IsConnected => Phase == DeviceConnectionPhase.Connected;
}
