namespace Launcher.Apps.Data;
public sealed record AppUsage(int Count = 0, DateTime? LastLaunchUtc = null);
