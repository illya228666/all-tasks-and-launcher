using Launcher.Startup;

namespace Launcher;
internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        using LauncherSession session = LauncherStartup.Create();
        System.Windows.Forms.Application.Run(session.Window);
    }
}
