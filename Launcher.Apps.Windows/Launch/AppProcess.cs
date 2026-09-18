using System.ComponentModel;
using System.Diagnostics;
using Launcher.Apps.Data;
using Launcher.Apps.Launch;

namespace Launcher.Apps.Windows.Launch;
public sealed class AppProcess : IAppStart
{
    public AppLaunchResult Start(AppInfo app, bool asAdmin)
    {
        if (!File.Exists(app.ExePath))
            return new(AppLaunchStatus.Missing, app.ExePath);
        try
        {
            using Process? process = Process.Start(new ProcessStartInfo { FileName = app.ExePath, WorkingDirectory = Path.GetDirectoryName(app.ExePath)!, UseShellExecute = true, Verb = asAdmin ? "runas" : "" });
            return process is null ? new(AppLaunchStatus.Failed, "Prozess wurde nicht gestartet.") : new(AppLaunchStatus.Started);
        }
        catch (Win32Exception error) when (error.NativeErrorCode == 1223)
        {
            return new(AppLaunchStatus.Cancelled);
        }
        catch (Exception error) when (error is Win32Exception or IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            return new(AppLaunchStatus.Failed, error.Message);
        }
    }

    public static void OpenFolder(string folder)
    {
        if (!Directory.Exists(folder))
            throw new DirectoryNotFoundException(folder);
        using Process? process = Process.Start(new ProcessStartInfo(folder) { UseShellExecute = true });
    }
}
