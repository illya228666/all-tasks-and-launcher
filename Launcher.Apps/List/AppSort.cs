using Launcher.Apps.Data;

namespace Launcher.Apps.List;
internal static class AppSort
{
    internal static IEnumerable<AppInfo> Apply(IEnumerable<AppInfo> apps, AppSortOrder order, Func<AppInfo, AppUsage> usage, HashSet<string> favorites) => order switch
    {
        AppSortOrder.ByName => apps.OrderBy(app => app.Name),
        AppSortOrder.ByRecent => apps.OrderByDescending(app => usage(app).LastLaunchUtc).ThenBy(app => app.Name),
        AppSortOrder.ByMostUsed => apps.OrderByDescending(app => usage(app).Count).ThenBy(app => app.Name),
        AppSortOrder.ByFavorites => apps.OrderByDescending(app => favorites.Contains(app.Id)).ThenBy(app => app.Name),
        _ => apps.OrderBy(app => app.Category).ThenBy(app => app.Name)};
}
