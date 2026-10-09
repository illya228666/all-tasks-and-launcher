namespace Launcher.Device.Data;

// Sequence distinguishes successive rolls even when they have the same result.
public sealed record DeviceDiceRoll(uint Sequence, byte Value);
