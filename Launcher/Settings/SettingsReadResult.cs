namespace Launcher.Settings;
internal sealed record SettingsReadResult(LauncherSettings Settings, string? Warning = null);
