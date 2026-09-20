using Launcher.Device.Data;
namespace Launcher.Device.Protocol;
internal interface IDeviceProtocolVersion
{
    DeviceCapabilities Capabilities { get; }
    DeviceProtocolVersion Version { get; }
    string Identity { get; }
    string PollCommand { get; }
    DeviceInput? ReadInput(string reply);
    DeviceWireCommand? Indicator(bool enabled);
    DeviceWireCommand? Lights(DeviceLights lights);
}
