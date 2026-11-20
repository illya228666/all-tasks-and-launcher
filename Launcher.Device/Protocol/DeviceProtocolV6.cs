using System.Globalization;
using Launcher.Device.Data;

namespace Launcher.Device.Protocol;

internal sealed class DeviceProtocolV6 : IDeviceProtocolVersion
{
    internal const int PinCount = 9;
    public DeviceCapabilities Capabilities => DeviceCapabilities.DigitalOutputs;
    public DeviceProtocolVersion Version => DeviceProtocolVersion.V6;
    public string Identity => "LAUNCHER_IO 6";
    public string PollCommand => "POLL";
    public DeviceWireCommand? Indicator(bool enabled) => null;
    public DeviceWireCommand? Lights(DeviceLights lights) => null;

    internal static DeviceWireCommand SetPin(int pin, bool high)
    {
        if (pin is < 0 or >= PinCount) throw new ArgumentOutOfRangeException(nameof(pin));
        string command = "PIN D" + pin.ToString(CultureInfo.InvariantCulture) + (high ? " HIGH" : " LOW");
        return new(command, "OK " + command);
    }

    public DevicePoll ReadPoll(string reply)
    {
        const string prefix = "PINS ";
        if (!reply.StartsWith(prefix, StringComparison.Ordinal)
            || !int.TryParse(reply.AsSpan(prefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out int levels)
            || levels is < 0 or >= (1 << PinCount))
            throw new DeviceProtocolException(DeviceFailure.InvalidReply, "Unexpected v6 pin levels.");

        return new(null, PinLevels: levels);
    }
}
