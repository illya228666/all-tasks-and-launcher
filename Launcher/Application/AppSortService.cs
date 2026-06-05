using Launcher.Domain;

namespace Launcher.Application;

/// <summary>
/// RU: Отвечает за сортировку приложений.
/// DE: Zustaendig fuer die Sortierung der Anwendungen.
/// </summary>
public sealed class AppSortService
{
    #region [RU] Бизнес-логика | [DE] Fachlogik

    /// <summary>
    /// RU: Сортирует приложения по выбранному режиму.
    /// DE: Sortiert Anwendungen nach dem gewaehlten Modus.
    /// </summary>
    public IEnumerable<AppEntry> Apply(
        IEnumerable<AppEntry> source,
        SortMode sortMode,
        Func<AppEntry, AppUsage> usageResolver,
        HashSet<string> favoriteKeys,
        Func<AppEntry, string> appKeyBuilder)
    {
        return sortMode switch
        {
            SortMode.ByName => source.OrderBy(app => app.Name),
            SortMode.ByRecent => source.OrderByDescending(app => usageResolver(app).LastLaunchUtc ?? DateTime.MinValue).ThenBy(app => app.Name),
            SortMode.ByMostUsed => source.OrderByDescending(app => usageResolver(app).Count).ThenBy(app => app.Name),
            SortMode.ByFavorites => source.OrderByDescending(app => favoriteKeys.Contains(appKeyBuilder(app))).ThenBy(app => app.Name),
            _ => source.OrderBy(app => app.Category).ThenBy(app => app.Name)
        };
    }

    #endregion
}
