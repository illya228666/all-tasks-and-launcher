namespace Launcher.Device.Data;
public sealed record DeviceState(bool IsConnected, string? PortName = null, DeviceProtocolVersion? ProtocolVersion = null);
