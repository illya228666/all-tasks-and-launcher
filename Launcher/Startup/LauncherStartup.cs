using Launcher.Apps.List;
using Launcher.Apps.Launch;
using Launcher.Apps.Windows.Discovery;
using Launcher.Apps.Windows.Launch;
using Launcher.Device.Connection;
using Launcher.Pet;
using Launcher.Pet.Windows;
using Launcher.Pet.Windows.Desktop;
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
        var device = new DeviceConnection();
        var wallpaper = new DesktopWallpaperSession();
        string? wallpaperWarning = wallpaper.Recover();
        PetWindowsSession? pet = null;
        string? petWarning = null;
        try
        {
            pet = new PetWindowsSession(window, window.Apps.PetArea, new PetWorld(new Random()), wallpaper);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException or System.Runtime.InteropServices.ExternalException)
        {
            petWarning = "Begleiter nicht verfuegbar: " + error.Message;
        }

        string warning = string.Join(" | ", new[] { read.Warning, wallpaperWarning, petWarning }.Where(text => !string.IsNullOrWhiteSpace(text)));
        return new LauncherSession(window, device, pet, apps, new AppLaunch(apps, new AppProcess()), settingsFile, read.Settings, warning);
    }
}
