using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Launcher.Domain;

namespace Launcher.Infrastructure;

/// <summary>
/// RU: Получает список приложений из ProjectReference и fallback-сканирования EXE.
/// DE: Ermittelt Anwendungen aus ProjectReference und EXE-Fallback-Scan.
/// </summary>
public sealed class ProjectReferenceDiscoveryService : IAppDiscoveryService
{
    #region [RU] Бизнес-логика | [DE] Fachlogik

    /// <inheritdoc />
    public string? FindSolutionRoot(string baseDirectory)
    {
        DirectoryInfo? current = new(baseDirectory);
        while (current != null)
        {
            if (File.Exists(Path.Combine(current.FullName, LauncherConstants.SolutionFileName)))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return null;
    }

    /// <inheritdoc />
    public List<AppEntry> DiscoverFromProjectReferences(string baseDirectory, string? solutionRoot)
    {
        var result = new List<AppEntry>();
        if (string.IsNullOrWhiteSpace(solutionRoot))
        {
            return result;
        }

        string launcherProjectPath = Path.Combine(solutionRoot, "Launcher", "Launcher.csproj");
        if (!File.Exists(launcherProjectPath))
        {
            return result;
        }

        XDocument csprojDocument = XDocument.Load(launcherProjectPath);
        IEnumerable<string> projectReferences = csprojDocument.Descendants()
            .Where(node => node.Name.LocalName == "ProjectReference")
            .Select(node => (string?)node.Attribute("Include"))
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Cast<string>();

        foreach (string includePath in projectReferences)
        {
            string projectPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(launcherProjectPath)!, includePath));
            if (!File.Exists(projectPath) || !IsExecutableProject(projectPath))
            {
                continue;
            }

            string projectName = Path.GetFileNameWithoutExtension(projectPath);
            if (projectName.Equals("Launcher", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            result.Add(new AppEntry
            {
                Name = Humanize(projectName),
                Category = ResolveCategory(solutionRoot, projectPath),
                ExePath = ResolveExePath(baseDirectory, projectName),
                FolderPath = Path.GetDirectoryName(projectPath) ?? solutionRoot
            });
        }

        return result;
    }

    /// <inheritdoc />
    public List<AppEntry> DiscoverFromExeScan(string baseDirectory, string launcherExeName)
    {
        return Directory.EnumerateFiles(baseDirectory, "*.exe", SearchOption.AllDirectories)
            .Where(path => !Path.GetFileName(path).Equals(launcherExeName, StringComparison.OrdinalIgnoreCase))
            .Where(path => !Path.GetFileName(path).Equals("createdump.exe", StringComparison.OrdinalIgnoreCase))
            .Select(path => new AppEntry
            {
                Name = Humanize(Path.GetFileNameWithoutExtension(path)),
                Category = "Ausgabeordner",
                ExePath = path,
                FolderPath = Path.GetDirectoryName(path) ?? baseDirectory
            })
            .OrderBy(entry => entry.Name)
            .ToList();
    }

    #endregion

    #region [RU] Вспомогательные методы | [DE] Hilfsmethoden

    private static bool IsExecutableProject(string csprojPath)
    {
        XDocument csprojDocument = XDocument.Load(csprojPath);
        string? outputType = csprojDocument.Descendants().FirstOrDefault(node => node.Name.LocalName == "OutputType")?.Value;

        return string.Equals(outputType, "Exe", StringComparison.OrdinalIgnoreCase)
            || string.Equals(outputType, "WinExe", StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveExePath(string baseDirectory, string projectName)
    {
        string directPath = Path.Combine(baseDirectory, $"{projectName}.exe");
        if (File.Exists(directPath))
        {
            return directPath;
        }

        string? foundPath = Directory.EnumerateFiles(baseDirectory, $"{projectName}.exe", SearchOption.AllDirectories)
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();

        return foundPath ?? directPath;
    }

    private static string ResolveCategory(string solutionRoot, string projectPath)
    {
        string relativePath = Path.GetRelativePath(solutionRoot, projectPath);
        string[] parts = relativePath.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length >= 3 && IsLessonFolder(parts[0]))
        {
            return $"{Humanize(parts[0])} / {Humanize(parts[1])}";
        }

        return parts.Length >= 1 ? Humanize(parts[0]) : "Sonstige";
    }

    private static bool IsLessonFolder(string folderName)
    {
        return folderName.Length > 3
            && char.IsDigit(folderName[0])
            && char.IsDigit(folderName[1])
            && folderName[2] == '_';
    }

    private static string Humanize(string text)
    {
        return text.Replace('_', ' ');
    }

    #endregion
}
