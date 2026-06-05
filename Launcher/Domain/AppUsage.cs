namespace Launcher.Domain;

/// <summary>
/// RU: Хранит статистику запусков приложения.
/// DE: Speichert die Start-Statistik einer Anwendung.
/// </summary>
public sealed class AppUsage
{
    #region [RU] Свойства | [DE] Eigenschaften

    /// <summary>
    /// RU: Количество запусков.
    /// DE: Anzahl der Starts.
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// RU: Время последнего запуска (UTC).
    /// DE: Zeitpunkt des letzten Starts (UTC).
    /// </summary>
    public DateTime? LastLaunchUtc { get; set; }

    #endregion
}
