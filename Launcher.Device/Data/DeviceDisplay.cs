namespace Launcher.Device.Data;

// State of the physical RIGHT digit, including the exact seven-segment mask.
public sealed record DeviceDisplay(int Value, int Brightness, DeviceDisplayMode Mode, int IntervalMs, int SegmentMask);
