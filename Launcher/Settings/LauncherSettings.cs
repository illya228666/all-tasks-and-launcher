using Launcher.Apps.Data;

namespace Launcher.Settings;
internal sealed class LauncherSettings
{
    public bool DarkTheme { get; set; }
    public bool ShowCollisions { get; set; }
    public AppListSettings Apps { get; set; } = new();
}
