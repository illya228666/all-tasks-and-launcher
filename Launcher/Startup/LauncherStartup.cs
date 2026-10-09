using Launcher.Apps.List;
using Launcher.Apps.Launch;
using Launcher.Apps.Windows.Discovery;
using Launcher.Apps.Windows.Launch;
using Launcher.Settings;
using Launcher.Windows;

namespace Launcher.Startup;
internal static class LauncherStartup
{
    internal static LauncherSession Create()
    {
        string folder = AppContext.BaseDirectory;
        string settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "zahlen-launcher", "launcher-settings.json");
        var settingsFile = new SettingsFile(settingsPath);
        SettingsReadResult read = settingsFile.Read();
        var source = new ProjectAppSource(folder, Path.GetFileName(System.Windows.Forms.Application.ExecutablePath));
        var apps = new AppList(source, read.Settings.Apps);
        var window = new MainWindow(read.Settings.ShowCollisions);
        string? warning = read.Warning;
        return new LauncherSession(window, null, apps, new AppLaunch(apps, new AppProcess()), settingsFile, read.Settings, warning);
    }
}
