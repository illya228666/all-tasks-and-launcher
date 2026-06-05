namespace Launcher.Domain;

/// <summary>
/// RU: Константы приложения для единообразия кода.
/// DE: Anwendungskonstanten fuer konsistenten Code.
/// </summary>
public static class LauncherConstants
{
    #region [RU] Константы | [DE] Konstanten

    /// <summary>
    /// RU: Общая категория в фильтре.
    /// DE: Sammelkategorie im Filter.
    /// </summary>
    public const string AllCategories = "Alle Kategorien";

    /// <summary>
    /// RU: Имя папки для state-файла в LocalApplicationData.
    /// DE: Ordnername fuer die State-Datei in LocalApplicationData.
    /// </summary>
    public const string StateFolderName = "zahlen-launcher";

    /// <summary>
    /// RU: Имя JSON-файла состояния.
    /// DE: Name der JSON-Statusdatei.
    /// </summary>
    public const string StateFileName = "launcher-state.json";

    /// <summary>
    /// RU: Имя solution-файла для поиска корня проекта.
    /// DE: Name der Solution-Datei zur Root-Erkennung.
    /// </summary>
    public const string SolutionFileName = "zahlen.sln";

    #endregion
}
