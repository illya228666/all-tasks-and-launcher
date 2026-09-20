using Launcher.Device.Data;
namespace Launcher.Windows;

internal static class DevicePresentation
{
    internal static string Status(DeviceState state) => state.Phase switch
    {
        DeviceConnectionPhase.Waiting => "Controller: wartet",
        DeviceConnectionPhase.Searching => "Controller: Suche...",
        DeviceConnectionPhase.Connecting => $"Controller: pruefe {state.PortName}",
        DeviceConnectionPhase.Connected => $"Controller: {state.PortName} | IO v{(int?)state.ProtocolVersion}",
        DeviceConnectionPhase.Stopped => "Controller: beendet",
        DeviceConnectionPhase.Faulted => "Controller: Fehler",
        _ => "Controller: " + Failure(state.Failure)
    };
    internal static string Failure(DeviceFailure failure) => failure switch
    {
        DeviceFailure.None => "nicht verbunden",
        DeviceFailure.NoPorts => "kein COM-Port",
        DeviceFailure.AccessDenied => "Port belegt / Zugriff verweigert",
        DeviceFailure.Timeout => "keine Antwort",
        DeviceFailure.UnsupportedProtocol => "unbekannte Protokollversion",
        DeviceFailure.InvalidReply => "ungueltige Antwort",
        DeviceFailure.Transport => "Verbindung unterbrochen",
        _ => "unerwarteter Fehler"
    };
    internal static string CommandResult(DeviceCommandResult result) => result switch
    {
        DeviceCommandResult.Completed => "Controller: Ausgabe bestaetigt.",
        DeviceCommandResult.Unsupported => "Controller: Ausgabe nicht unterstuetzt.",
        DeviceCommandResult.Disconnected => "Controller: nicht verbunden.",
        DeviceCommandResult.Cancelled => "Controller: Ausgabe abgebrochen.",
        DeviceCommandResult.Superseded => "Controller: Ausgabe durch neueren Zustand ersetzt.",
        _ => "Controller: Ausgabe fehlgeschlagen."
    };
}
