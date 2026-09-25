using Launcher.Device.Data;

namespace Launcher.Device.Protocol;
internal sealed class DeviceProtocolV2 : IDeviceProtocolVersion
{
    internal const string IdentityValue = "LAUNCHER_IO 2";
    private const string Poll = "POLL";
    private const string NoInput = "INPUT NONE";
    private const string LeftPressed = "INPUT LEFT_PRESSED";
    private const string RightPressed = "INPUT RIGHT_PRESSED";
    private const string BothPressed = "INPUT BOTH_PRESSED";
    private const string LedsReply = "OK LEDS";

    public DeviceCapabilities Capabilities => DeviceCapabilities.Lights;
    public DeviceProtocolVersion Version => DeviceProtocolVersion.V2;
    public string Identity => IdentityValue;
    public string PollCommand => Poll;

    public DevicePoll ReadPoll(string reply) => reply switch
    {
        NoInput => new(null),
        LeftPressed => new(DeviceInput.LeftButtonPressed),
        RightPressed => new(DeviceInput.RightButtonPressed),
        BothPressed => new(DeviceInput.BothButtonsPressed),
        _ => throw new DeviceProtocolException(DeviceFailure.InvalidReply, "Unexpected protocol v2 reply.")
    };

    public DeviceWireCommand? Indicator(bool enabled) =>
        null;

    public DeviceWireCommand? Lights(DeviceLights lights) => CreateLights(lights);

    private static DeviceWireCommand CreateLights(DeviceLights lights)
    {
        if ((lights & ~DeviceLights.All) != DeviceLights.None)
            throw new ArgumentOutOfRangeException(nameof(lights));
        return new($"LEDS {(int)lights}", LedsReply);
    }
}
