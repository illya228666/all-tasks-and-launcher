using Launcher.Apps.Data;

namespace Launcher.Apps.List;
internal static class AppSearch
{
    internal static IEnumerable<AppInfo> Apply(IEnumerable<AppInfo> apps, AppFilter filter, HashSet<string> favorites)
    {
        string search = filter.Search.Trim();
        return apps.Where(app => (search.Length == 0 || app.Name.Contains(search, StringComparison.OrdinalIgnoreCase) || app.Category.Contains(search, StringComparison.OrdinalIgnoreCase) || app.ExePath.Contains(search, StringComparison.OrdinalIgnoreCase)) && (filter.Category is null || string.Equals(app.Category, filter.Category, StringComparison.OrdinalIgnoreCase)) && (!filter.FavoritesOnly || favorites.Contains(app.Id)) && (!filter.AvailableOnly || app.IsAvailable));
    }
}
