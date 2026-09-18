using Launcher.Apps.Data;

namespace Launcher.Apps.List;
public sealed class AppList
{
    private readonly IAppSource _source;
    private readonly HashSet<string> _favorites;
    private readonly Dictionary<string, AppUsage> _usage;
    private IReadOnlyList<AppInfo> _apps = Array.Empty<AppInfo>();
    public AppFilter Filter { get; private set; }
    public string RootFolder { get; private set; } = "";
    public int Count => _apps.Count;
    public int FavoriteCount => _favorites.Count;
    public long LaunchCount => _usage.Values.Sum(value => (long)value.Count);

    public AppList(IAppSource source, AppListSettings settings)
    {
        _source = source;
        Filter = settings.Filter;
        _favorites = new(settings.Favorites, StringComparer.OrdinalIgnoreCase);
        _usage = new(settings.Usage, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<string> Refresh()
    {
        AppSourceResult result = _source.Read();
        _apps = Array.AsReadOnly(result.Apps.DistinctBy(app => app.Id, StringComparer.OrdinalIgnoreCase).ToArray());
        RootFolder = result.RootFolder;
        if (Filter.Category is not null && !_apps.Any(app => string.Equals(app.Category, Filter.Category, StringComparison.OrdinalIgnoreCase)))
            Filter = Filter with
            {
                Category = null
            };
        return result.Warnings;
    }

    public void SetFilter(AppFilter filter) => Filter = filter;
    public IReadOnlyList<AppInfo> VisibleApps() => Array.AsReadOnly(AppSort.Apply(AppSearch.Apply(_apps, Filter, _favorites), Filter.Sort, GetUsage, _favorites).ToArray());
    public string[] Categories() => _apps.Select(app => app.Category).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(value => value).ToArray();
    public bool IsFavorite(AppInfo app) => _favorites.Contains(app.Id);
    public AppUsage GetUsage(AppInfo app) => _usage.GetValueOrDefault(app.Id) ?? new();
    public void ToggleFavorite(AppInfo app)
    {
        if (!_favorites.Add(app.Id))
            _favorites.Remove(app.Id);
    }

    internal void RegisterLaunch(AppInfo app)
    {
        AppUsage previous = GetUsage(app);
        _usage[app.Id] = new(previous.Count == int.MaxValue ? int.MaxValue : previous.Count + 1, DateTime.UtcNow);
    }

    public AppListSettings SaveSettings() => new()
    {
        Filter = Filter,
        Favorites = _favorites.OrderBy(value => value).ToList(),
        Usage = new(_usage, StringComparer.OrdinalIgnoreCase)
    };
}
