namespace Launcher.Apps.Launch;
public sealed record AppLaunchResult(AppLaunchStatus Status, string? Error = null);
