using System.Xml.Linq;
using Launcher.Apps.Data;
using Launcher.Apps.List;

namespace Launcher.Apps.Windows.Discovery;
public sealed class ProjectAppSource : IAppSource
{
    private readonly string _baseFolder;
    private readonly string _launcherExe;
    private readonly FolderAppSource _fallback;
    public ProjectAppSource(string baseFolder, string launcherExe)
    {
        _baseFolder = baseFolder;
        _launcherExe = launcherExe;
        _fallback = new(baseFolder, launcherExe);
    }

    public AppSourceResult Read()
    {
        var warnings = new List<string>();
        var apps = new List<AppInfo>();
        string? root = FindRoot();
        string? project = root is null ? null : Path.Combine(root, "Launcher", "Launcher.csproj");
        if (project is not null && File.Exists(project))
        {
            try
            {
                string[] outputs = AppFiles.FindExecutables(_baseFolder, warnings).ToArray();
                foreach (XElement reference in XDocument.Load(project).Descendants().Where(node => node.Name.LocalName == "ProjectReference"))
                {
                    try
                    {
                        string? include = (string?)reference.Attribute("Include");
                        if (string.IsNullOrWhiteSpace(include))
                            continue;
                        string path = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(project)!, include));
                        if (!File.Exists(path))
                        {
                            warnings.Add($"Projekt fehlt: {path}");
                            continue;
                        }

                        XDocument document = XDocument.Load(path);
                        string? outputType = Value(document, "OutputType");
                        if (outputType is not ("Exe" or "WinExe"))
                            continue;
                        string name = Path.GetFileNameWithoutExtension(path);
                        string assembly = Value(document, "AssemblyName") ?? name;
                        if ((assembly + ".exe").Equals(_launcherExe, StringComparison.OrdinalIgnoreCase)
                            || assembly.Equals("createdump", StringComparison.OrdinalIgnoreCase))
                            continue;
                        string expected = Path.Combine(_baseFolder, assembly + ".exe");
                        string exe = File.Exists(expected) ? expected : outputs.Where(file => Path.GetFileName(file).Equals(assembly + ".exe", StringComparison.OrdinalIgnoreCase)).OrderByDescending(File.GetLastWriteTimeUtc).FirstOrDefault() ?? expected;
                        string[] parts = Path.GetRelativePath(root!, path).Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                        string category = parts.Length >= 3 && parts[0].Length > 3 && char.IsDigit(parts[0][0]) && char.IsDigit(parts[0][1]) && parts[0][2] == '_' ? parts[0] + " / " + parts[1] : parts[0];
                        apps.Add(AppFiles.Create(_baseFolder, name, category.Replace('_', ' '), exe, Path.GetDirectoryName(path)!));
                    }
                    catch (Exception error) when (AppFiles.IsReadError(error))
                    {
                        warnings.Add(error.Message);
                    }
                }
            }
            catch (Exception error) when (AppFiles.IsReadError(error))
            {
                warnings.Add(error.Message);
            }
        }

        if (apps.Count > 0)
            return new(apps, root!, warnings);
        AppSourceResult fallback = _fallback.Read();
        return fallback with
        {
            RootFolder = root ?? _baseFolder,
            Warnings = warnings.Concat(fallback.Warnings).ToArray()
        };
    }

    private string? FindRoot()
    {
        for (DirectoryInfo? folder = new(_baseFolder); folder is not null; folder = folder.Parent)
            if (File.Exists(Path.Combine(folder.FullName, "zahlen.sln")))
                return folder.FullName;
        return null;
    }

    private static string? Value(XDocument document, string name) => document.Descendants().FirstOrDefault(node => node.Name.LocalName == name)?.Value;
}
