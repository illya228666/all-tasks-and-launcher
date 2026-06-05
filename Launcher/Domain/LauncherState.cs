using System;
using System.Collections.Generic;

namespace Launcher.Domain;

/// <summary>
/// RU: Состояние лаунчера, сохраняемое между запусками.
/// DE: Launcher-Zustand, der zwischen Starts gespeichert wird.
/// </summary>
public sealed class LauncherState
{
    #region [RU] Свойства | [DE] Eigenschaften

    /// <summary>
    /// RU: Включена ли темная тема.
    /// DE: Gibt an, ob das dunkle Theme aktiv ist.
    /// </summary>
    public bool DarkMode { get; set; }

    /// <summary>
    /// RU: Фильтр «только избранные».
    /// DE: Filter "nur Favoriten".
    /// </summary>
    public bool FavoritesOnly { get; set; }

    /// <summary>
    /// RU: Фильтр «только доступные EXE».
    /// DE: Filter "nur verfuegbare EXE".
    /// </summary>
    public bool AvailableOnly { get; set; } = true;

    /// <summary>
    /// RU: Последний поисковый запрос.
    /// DE: Letzte Suchanfrage.
    /// </summary>
    public string LastSearch { get; set; } = string.Empty;

    /// <summary>
    /// RU: Последняя выбранная категория.
    /// DE: Zuletzt gewaehlte Kategorie.
    /// </summary>
    public string LastCategory { get; set; } = LauncherConstants.AllCategories;

    /// <summary>
    /// RU: Последний режим сортировки.
    /// DE: Zuletzt verwendeter Sortiermodus.
    /// </summary>
    public SortMode LastSort { get; set; } = SortMode.ByCategory;

    /// <summary>
    /// RU: Список ключей избранных приложений.
    /// DE: Liste der Favoriten-Schluessel.
    /// </summary>
    public List<string> Favorites { get; set; } = new();

    /// <summary>
    /// RU: Статистика запусков по ключу приложения.
    /// DE: Start-Statistik pro Anwendungsschluessel.
    /// </summary>
    public Dictionary<string, AppUsage> Usage { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    #endregion
}
