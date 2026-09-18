namespace Launcher.Apps.Data;
public sealed record AppFilter(string Search = "", string? Category = null, bool FavoritesOnly = false, bool AvailableOnly = true, AppSortOrder Sort = AppSortOrder.ByCategory);
