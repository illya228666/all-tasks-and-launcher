namespace Launcher.Domain;

/// <summary>
/// RU: Описывает одну запускаемую учебную программу в лаунчере.
/// DE: Beschreibt ein startbares Lernprogramm im Launcher.
/// </summary>
public sealed class AppEntry
{
    #region [RU] Свойства | [DE] Eigenschaften

    /// <summary>
    /// RU: Человекочитаемое имя программы.
    /// DE: Lesbarer Name der Anwendung.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// RU: Категория для группировки в интерфейсе.
    /// DE: Kategorie zur Gruppierung in der Benutzeroberflaeche.
    /// </summary>
    public string Category { get; init; } = string.Empty;

    /// <summary>
    /// RU: Полный путь к EXE-файлу.
    /// DE: Vollstaendiger Pfad zur EXE-Datei.
    /// </summary>
    public string ExePath { get; init; } = string.Empty;

    /// <summary>
    /// RU: Папка проекта/приложения.
    /// DE: Projekt-/Anwendungsordner.
    /// </summary>
    public string FolderPath { get; init; } = string.Empty;

    #endregion
}
