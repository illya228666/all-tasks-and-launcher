namespace Launcher.Device.Data;

// Latest hardware state returned by POLL; animations run on the ESP, not on Windows.
public sealed record DeviceDisplay(int Value, int Brightness, DeviceDisplayMode Mode, int IntervalMs);
