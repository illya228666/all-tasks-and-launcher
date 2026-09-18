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
    private static readonly string[] Replies = { NoInput, LeftPressed, RightPressed, BothPressed };

    public DeviceProtocolVersion Version => DeviceProtocolVersion.V2;
    public string Identity => IdentityValue;
    public string PollCommand => Poll;
    public string[] PollReplies => Replies;

    public DeviceInput? ReadInput(string reply) => reply switch
    {
        NoInput => null,
        LeftPressed => DeviceInput.LeftButtonPressed,
        RightPressed => DeviceInput.RightButtonPressed,
        BothPressed => DeviceInput.BothButtonsPressed,
        _ => throw new IOException("Unexpected protocol v2 reply.")
    };

    public DeviceWireCommand Indicator(bool enabled) =>
        CreateLights(enabled ? DeviceLights.Green : DeviceLights.None);

    public DeviceWireCommand? Lights(DeviceLights lights) => CreateLights(lights);

    private static DeviceWireCommand CreateLights(DeviceLights lights)
    {
        if ((lights & ~DeviceLights.All) != 0)
            throw new ArgumentOutOfRangeException(nameof(lights));
        return new($"LEDS {(int)lights}", LedsReply);
    }
}
