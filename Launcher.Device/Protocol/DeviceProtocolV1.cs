using Launcher.Device.Data;

namespace Launcher.Device.Protocol;
internal sealed class DeviceProtocolV1 : IDeviceProtocolVersion
{
    internal const string IdentityValue = "LAUNCHER_IO 1";
    private const string Poll = "POLL";
    private const string NoInput = "INPUT NONE";
    private const string ButtonPressed = "INPUT BUTTON_PRESSED";
    private const string IndicatorReply = "OK INDICATOR";
    private static readonly string[] Replies = { NoInput, ButtonPressed };

    public DeviceProtocolVersion Version => DeviceProtocolVersion.V1;
    public string Identity => IdentityValue;
    public string PollCommand => Poll;
    public string[] PollReplies => Replies;

    public DeviceInput? ReadInput(string reply) => reply switch
    {
        NoInput => null,
        ButtonPressed => DeviceInput.PrimaryButtonPressed,
        _ => throw new IOException("Unexpected protocol v1 reply.")
    };

    public DeviceWireCommand Indicator(bool enabled) =>
        new(enabled ? "INDICATOR ON" : "INDICATOR OFF", IndicatorReply);

    public DeviceWireCommand? Lights(DeviceLights lights) => null;
}
