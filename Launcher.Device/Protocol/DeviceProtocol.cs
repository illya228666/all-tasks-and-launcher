using Launcher.Device.Data;
namespace Launcher.Device.Protocol;
internal static class DeviceProtocol
{
    internal const string Hello = "HELLO";
    private static readonly IReadOnlyDictionary<string, IDeviceProtocolVersion> Versions =
        new IDeviceProtocolVersion[] { new DeviceProtocolV1(), new DeviceProtocolV2() }
            .ToDictionary(version => version.Identity, StringComparer.Ordinal);
    internal static IDeviceProtocolVersion FromIdentity(string identity) =>
        Versions.TryGetValue(identity, out var version) ? version :
            throw new DeviceProtocolException(DeviceFailure.UnsupportedProtocol, identity);
}
