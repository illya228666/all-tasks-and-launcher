using Launcher.Device.Data;

namespace Launcher.Device.Protocol;

internal readonly record struct DeviceWireCommand(string Command, string ExpectedReply);

internal interface IDeviceProtocolVersion
{
    DeviceProtocolVersion Version { get; }
    string Identity { get; }
    string PollCommand { get; }
    string[] PollReplies { get; }
    DeviceInput? ReadInput(string reply);
    DeviceWireCommand Indicator(bool enabled);
    DeviceWireCommand? Lights(DeviceLights lights);
}
