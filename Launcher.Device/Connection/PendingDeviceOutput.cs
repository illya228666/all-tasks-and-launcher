using Launcher.Device.Data;
namespace Launcher.Device.Connection;
internal sealed record PendingDeviceOutput(DeviceCapabilities Capability, bool Indicator, DeviceLights Lights,
    TaskCompletionSource<DeviceCommandResult> Completion);
