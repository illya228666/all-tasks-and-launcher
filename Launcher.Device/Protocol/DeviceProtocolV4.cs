using System.Globalization;
using Launcher.Device.Data;

namespace Launcher.Device.Protocol;

internal sealed class DeviceProtocolV4 : IDeviceProtocolVersion
{
    public DeviceCapabilities Capabilities => DeviceCapabilities.None;
    public DeviceProtocolVersion Version => DeviceProtocolVersion.V4;
    public string Identity => "LAUNCHER_IO 4";
    public string PollCommand => "POLL";

    public DevicePoll ReadPoll(string reply)
    {
        string[] parts = reply.Split(' ');
        if (parts.Length != 3 || parts[0] != "DICE"
            || !uint.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out uint sequence)
            || !byte.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out byte value)
            || value is < 1 or > 6)
            throw new DeviceProtocolException(DeviceFailure.InvalidReply, "Unexpected protocol v4 dice reply.");

        return new(null, Dice: new(sequence, value));
    }

    public DeviceWireCommand? Indicator(bool enabled) => null;
    public DeviceWireCommand? Lights(DeviceLights lights) => null;
}
