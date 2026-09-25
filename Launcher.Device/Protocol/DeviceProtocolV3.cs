using Launcher.Device.Data;

namespace Launcher.Device.Protocol;
internal sealed class DeviceProtocolV3 : IDeviceProtocolVersion
{
    internal const string IdentityValue = "LAUNCHER_IO 3";
    private const string Poll = "POLL";
    private const string FadePrefix = "FADE ";

    public DeviceCapabilities Capabilities => DeviceCapabilities.None;
    public DeviceProtocolVersion Version => DeviceProtocolVersion.V3;
    public string Identity => IdentityValue;
    public string PollCommand => Poll;

    public DevicePoll ReadPoll(string reply)
    {
        if (!reply.StartsWith(FadePrefix, StringComparison.Ordinal)
            || !byte.TryParse(reply[FadePrefix.Length..], out byte fade))
            throw new DeviceProtocolException(DeviceFailure.InvalidReply, "Unexpected protocol v3 reply.");

        return new(null, fade);
    }

    public DeviceWireCommand? Indicator(bool enabled) => null;
    public DeviceWireCommand? Lights(DeviceLights lights) => null;
}
