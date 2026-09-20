namespace Launcher.Device.Connection;
internal sealed record DeviceConnectionTiming(int StartupDelayMs = 1600, int RetryDelayMs = 2000, int PollIntervalMs = 100, int HelloRetryMs = 250);
