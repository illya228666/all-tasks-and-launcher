namespace Launcher.Device.Data;
public sealed record DeviceState(DeviceConnectionPhase Phase, string? PortName = null,
    DeviceProtocolVersion? ProtocolVersion = null, DeviceCapabilities Capabilities = DeviceCapabilities.None,
    DeviceFailure Failure = DeviceFailure.None, string? Detail = null)
{
    public bool IsConnected => Phase == DeviceConnectionPhase.Connected;
}
