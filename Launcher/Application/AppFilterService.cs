using Launcher.Domain;

namespace Launcher.Application;

/// <summary>
/// RU: Отвечает за фильтрацию приложений по запросу и флагам UI.
/// DE: Zustaendig fuer die Filterung der Anwendungen nach Suchanfrage und UI-Flags.
/// </summary>
public sealed class AppFilterService
{
    #region [RU] Бизнес-логика | [DE] Fachlogik

    /// <summary>
    /// RU: Применяет фильтры к коллекции приложений.
    /// DE: Wendet Filter auf eine Anwendungssammlung an.
    /// </summary>
    public IEnumerable<AppEntry> Apply(
        IEnumerable<AppEntry> source,
        string search,
        string selectedCategory,
        bool favoritesOnly,
        bool availableOnly,
        HashSet<string> favoriteKeys,
        Func<AppEntry, string> appKeyBuilder)
    {
        IEnumerable<AppEntry> query = source;

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(app =>
                app.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
                || app.Category.Contains(search, StringComparison.OrdinalIgnoreCase)
                || app.ExePath.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.Equals(selectedCategory, LauncherConstants.AllCategories, StringComparison.Ordinal))
        {
            query = query.Where(app => string.Equals(app.Category, selectedCategory, StringComparison.OrdinalIgnoreCase));
        }

        if (favoritesOnly)
        {
            query = query.Where(app => favoriteKeys.Contains(appKeyBuilder(app)));
        }

        if (availableOnly)
        {
            query = query.Where(app => File.Exists(app.ExePath));
        }

        return query;
    }

    #endregion
}
