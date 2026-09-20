using Launcher.Device.Data;
namespace Launcher.Device.Protocol;
internal sealed class DeviceProtocolException : IOException
{
    internal DeviceFailure Failure { get; }
    internal DeviceProtocolException(DeviceFailure failure, string message) : base(message) => Failure = failure;
}
