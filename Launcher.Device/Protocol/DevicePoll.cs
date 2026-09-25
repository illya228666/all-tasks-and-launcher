using Launcher.Device.Data;

namespace Launcher.Device.Protocol;
internal readonly record struct DevicePoll(DeviceInput? Input, byte? Fade = null);
