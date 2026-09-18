namespace Launcher.Device.Data;
[Flags]
public enum DeviceLights
{
    None = 0,
    Red = 1,
    Yellow = 2,
    Green = 4,
    All = Red | Yellow | Green
}
