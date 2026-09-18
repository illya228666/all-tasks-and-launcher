using Launcher.Apps.Data;
using Launcher.Apps.List;

namespace Launcher.Apps.Windows.Discovery;
public sealed class FolderAppSource : IAppSource
{
    private readonly string _folder;
    private readonly string _launcherExe;
    public FolderAppSource(string folder, string launcherExe)
    {
        _folder = folder;
        _launcherExe = launcherExe;
    }

    public AppSourceResult Read()
    {
        var warnings = new List<string>();
        AppInfo[] apps = AppFiles.FindExecutables(_folder, warnings).Where(path => !Path.GetFileName(path).Equals(_launcherExe, StringComparison.OrdinalIgnoreCase) && !Path.GetFileName(path).Equals("createdump.exe", StringComparison.OrdinalIgnoreCase)).Select(path => AppFiles.Create(_folder, Path.GetFileNameWithoutExtension(path), "Ausgabeordner", path, Path.GetDirectoryName(path)!)).OrderBy(app => app.Name).ToArray();
        return new(apps, _folder, warnings);
    }
}
