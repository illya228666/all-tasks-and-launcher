using Launcher.Domain;

namespace Launcher.Infrastructure;

/// <summary>
/// RU: Контракт для получения списка приложений из структуры решения/выходных EXE.
/// DE: Vertrag zum Ermitteln von Anwendungen aus Solution-Struktur/EXE-Ausgaben.
/// </summary>
public interface IAppDiscoveryService
{
    /// <summary>
    /// RU: Находит корень solution относительно базовой папки.
    /// DE: Findet den Solution-Root relativ zum Basisordner.
    /// </summary>
    string? FindSolutionRoot(string baseDirectory);

    /// <summary>
    /// RU: Загружает приложения из ProjectReference в Launcher.csproj.
    /// DE: Laedt Anwendungen aus ProjectReference in Launcher.csproj.
    /// </summary>
    List<AppEntry> DiscoverFromProjectReferences(string baseDirectory, string? solutionRoot);

    /// <summary>
    /// RU: Загружает приложения сканированием EXE.
    /// DE: Laedt Anwendungen durch EXE-Scan.
    /// </summary>
    List<AppEntry> DiscoverFromExeScan(string baseDirectory, string launcherExeName);
}
