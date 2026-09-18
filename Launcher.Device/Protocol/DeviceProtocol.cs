using Launcher.Device.Data;

namespace Launcher.Device.Protocol;
internal static class DeviceProtocol
{
    internal const string Hello = "HELLO";
    internal const string Identity = "LAUNCHER_IO 1";
    internal const string Poll = "POLL";
    internal const string NoInput = "INPUT NONE";
    internal const string ButtonPressed = "INPUT BUTTON_PRESSED";
    internal const string IndicatorReply = "OK INDICATOR";
    internal static string Indicator(bool enabled) => enabled ? "INDICATOR ON" : "INDICATOR OFF";
    internal static DeviceInput? ReadInput(string reply) => reply switch
    {
        NoInput => null,
        ButtonPressed => DeviceInput.PrimaryButtonPressed,
        _ => throw new IOException("Unexpected device reply.")};
}
