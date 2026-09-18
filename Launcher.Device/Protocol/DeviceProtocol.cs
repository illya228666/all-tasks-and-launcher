namespace Launcher.Device.Protocol;
internal static class DeviceProtocol
{
    internal const string Hello = "HELLO";

    private static readonly IDeviceProtocolVersion V1 = new DeviceProtocolV1();
    private static readonly IDeviceProtocolVersion V2 = new DeviceProtocolV2();

    internal static readonly string[] Identities =
    {
        V1.Identity,
        V2.Identity
    };

    internal static IDeviceProtocolVersion FromIdentity(string identity) => identity switch
    {
        DeviceProtocolV1.IdentityValue => V1,
        DeviceProtocolV2.IdentityValue => V2,
        _ => throw new IOException("Unsupported launcher IO protocol: " + identity)
    };
}
