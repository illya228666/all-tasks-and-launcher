using System.Globalization;
using Launcher.Device.Data;

namespace Launcher.Device.Protocol;

internal sealed class DeviceProtocolV5 : IDeviceProtocolVersion
{
    public DeviceCapabilities Capabilities => DeviceCapabilities.Display;
    public DeviceProtocolVersion Version => DeviceProtocolVersion.V5;
    public string Identity => "LAUNCHER_IO 5";
    public string PollCommand => "POLL";
    public DeviceWireCommand? Indicator(bool enabled) => null;
    public DeviceWireCommand? Lights(DeviceLights lights) => null;

    public DevicePoll ReadPoll(string reply)
    {
        string[] parts = reply.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 5 || parts[0] != "DISPLAY"
            || !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out int value)
            || value is < 0 or > 99
            || !int.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out int brightness)
            || brightness is < 1 or > 100
            || !Enum.TryParse<DeviceDisplayMode>(parts[3], true, out var mode)
            || !Enum.IsDefined(mode)
            || !int.TryParse(parts[4], NumberStyles.None, CultureInfo.InvariantCulture, out int interval)
            || interval is < 100 or > 2000)
            throw new DeviceProtocolException(DeviceFailure.InvalidReply, "Unexpected v5 display state.");

        return new(null, Display: new(value, brightness, mode, interval));
    }
}
