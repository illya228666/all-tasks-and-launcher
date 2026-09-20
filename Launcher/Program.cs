using Launcher.Startup;

namespace Launcher;
internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        if (args.Contains("--ruins-preview", StringComparer.OrdinalIgnoreCase))
        {
            using var preview = new Launcher.Pet.Windows.Debug.RuinPreviewWindow();
            System.Windows.Forms.Application.Run(preview);
            return;
        }
        using LauncherSession session = LauncherStartup.Create();
        System.Windows.Forms.Application.Run(session.Window);
    }
}
