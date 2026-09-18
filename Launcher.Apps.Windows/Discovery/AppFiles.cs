using Launcher.Apps.Data;

namespace Launcher.Apps.Windows.Discovery;
internal static class AppFiles
{
    internal static bool IsReadError(Exception error) => error is IOException or UnauthorizedAccessException or System.Xml.XmlException or ArgumentException or NotSupportedException;
    internal static IEnumerable<string> FindExecutables(string folder, List<string> warnings)
    {
        var pending = new Stack<string>();
        pending.Push(folder);
        while (pending.Count > 0)
        {
            string current = pending.Pop();
            string[] files = Array.Empty<string>();
            try
            {
                files = Directory.GetFiles(current, "*.exe");
            }
            catch (Exception error) when (IsReadError(error))
            {
                warnings.Add($"{current}: {error.Message}");
            }

            foreach (string file in files)
                yield return file;

            string[] children;
            try
            {
                children = Directory.GetDirectories(current);
            }
            catch (Exception error) when (IsReadError(error))
            {
                warnings.Add($"{current}: {error.Message}");
                continue;
            }

            foreach (string child in children)
            {
                try
                {
                    if ((File.GetAttributes(child) & FileAttributes.ReparsePoint) == 0)
                        pending.Push(child);
                }
                catch (Exception error) when (IsReadError(error))
                {
                    warnings.Add($"{child}: {error.Message}");
                }
            }
        }
    }

    internal static AppInfo Create(string baseFolder, string name, string category, string exe, string folder)
    {
        string path = Path.GetFullPath(exe);
        string id = Path.GetRelativePath(baseFolder, path).Replace('\\', '/').ToUpperInvariant();
        return new(id, name.Replace('_', ' '), category, path, folder, File.Exists(path));
    }
}
