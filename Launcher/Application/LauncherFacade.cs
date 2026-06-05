using Launcher.Domain;
using Launcher.Infrastructure;

namespace Launcher.Application;

/// <summary>
/// RU: Фасад для UI-уровня. Инкапсулирует загрузку, фильтрацию, сортировку и state-операции.
/// DE: Fassade fuer die UI-Ebene. Kapselt Laden, Filtern, Sortieren und State-Operationen.
/// </summary>
public sealed class LauncherFacade
{
    #region [RU] Поля | [DE] Felder

    private readonly IAppDiscoveryService _discoveryService;
    private readonly IStateStorageService _stateStorageService;
    private readonly AppFilterService _filterService;
    private readonly AppSortService _sortService;

    #endregion

    #region [RU] Конструктор | [DE] Konstruktor

    /// <summary>
    /// RU: Создает фасад с необходимыми сервисами.
    /// DE: Erstellt die Fassade mit den benoetigten Diensten.
    /// </summary>
    public LauncherFacade(
        IAppDiscoveryService discoveryService,
        IStateStorageService stateStorageService,
        AppFilterService filterService,
        AppSortService sortService)
    {
        _discoveryService = discoveryService;
        _stateStorageService = stateStorageService;
        _filterService = filterService;
        _sortService = sortService;
    }

    #endregion

    #region [RU] Бизнес-логика | [DE] Fachlogik

    /// <summary>
    /// RU: Загружает состояние лаунчера.
    /// DE: Laedt den Launcher-Zustand.
    /// </summary>
    public LauncherState LoadState(string statePath)
    {
        return _stateStorageService.Load(statePath);
    }

    /// <summary>
    /// RU: Сохраняет состояние лаунчера.
    /// DE: Speichert den Launcher-Zustand.
    /// </summary>
    public void SaveState(string statePath, LauncherState state)
    {
        _stateStorageService.Save(statePath, state);
    }

    /// <summary>
    /// RU: Загружает список приложений из структуры solution или через fallback-сканирование EXE.
    /// DE: Laedt Anwendungen aus der Solution-Struktur oder per EXE-Fallback-Scan.
    /// </summary>
    public List<AppEntry> LoadApps(string baseDirectory, string launcherExeName, out string? solutionRoot)
    {
        solutionRoot = _discoveryService.FindSolutionRoot(baseDirectory);

        List<AppEntry> apps = _discoveryService.DiscoverFromProjectReferences(baseDirectory, solutionRoot);
        if (apps.Count == 0)
        {
            apps = _discoveryService.DiscoverFromExeScan(baseDirectory, launcherExeName);
        }

        return apps
            .GroupBy(app => app.Name, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();
    }

    /// <summary>
    /// RU: Возвращает список категорий для выпадающего фильтра.
    /// DE: Liefert die Kategorien fuer den Dropdown-Filter.
    /// </summary>
    public List<string> BuildCategories(IEnumerable<AppEntry> apps)
    {
        return apps
            .Select(app => app.Category)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(category => category)
            .ToList();
    }

    /// <summary>
    /// RU: Выполняет фильтрацию и сортировку списка.
    /// DE: Fuehrt Filterung und Sortierung der Liste aus.
    /// </summary>
    public List<AppEntry> FilterAndSortApps(
        IEnumerable<AppEntry> allApps,
        string search,
        string selectedCategory,
        bool favoritesOnly,
        bool availableOnly,
        SortMode sortMode,
        HashSet<string> favoriteKeys,
        LauncherState state)
    {
        IEnumerable<AppEntry> filtered = _filterService.Apply(
            allApps,
            search,
            selectedCategory,
            favoritesOnly,
            availableOnly,
            favoriteKeys,
            BuildAppKey);

        IEnumerable<AppEntry> sorted = _sortService.Apply(filtered, sortMode, app => GetUsage(app, state), favoriteKeys, BuildAppKey);
        return sorted.ToList();
    }

    /// <summary>
    /// RU: Возвращает usage для приложения (или пустой usage).
    /// DE: Liefert die Usage fuer eine App (oder eine leere Usage).
    /// </summary>
    public AppUsage GetUsage(AppEntry app, LauncherState state)
    {
        return state.Usage.TryGetValue(BuildAppKey(app), out AppUsage? usage)
            ? usage
            : new AppUsage();
    }

    /// <summary>
    /// RU: Проверяет, является ли приложение избранным.
    /// DE: Prueft, ob eine Anwendung ein Favorit ist.
    /// </summary>
    public bool IsFavorite(AppEntry app, HashSet<string> favoriteKeys)
    {
        return favoriteKeys.Contains(BuildAppKey(app));
    }

    /// <summary>
    /// RU: Переключает статус избранного у приложения.
    /// DE: Schaltet den Favoritenstatus einer Anwendung um.
    /// </summary>
    public void ToggleFavorite(AppEntry app, HashSet<string> favoriteKeys, LauncherState state)
    {
        string key = BuildAppKey(app);
        if (favoriteKeys.Contains(key))
        {
            favoriteKeys.Remove(key);
        }
        else
        {
            favoriteKeys.Add(key);
        }

        state.Favorites = favoriteKeys.OrderBy(value => value).ToList();
    }

    /// <summary>
    /// RU: Регистрирует запуск приложения в статистике.
    /// DE: Registriert den Start einer Anwendung in der Statistik.
    /// </summary>
    public void RegisterLaunch(AppEntry app, LauncherState state)
    {
        string key = BuildAppKey(app);
        if (!state.Usage.TryGetValue(key, out AppUsage? usage))
        {
            usage = new AppUsage();
            state.Usage[key] = usage;
        }

        usage.Count++;
        usage.LastLaunchUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// RU: Строит стабильный ключ приложения для избранного и статистики.
    /// DE: Baut einen stabilen Anwendungsschluessel fuer Favoriten und Statistik.
    /// </summary>
    public string BuildAppKey(AppEntry app)
    {
        return $"{app.Name}|{app.Category}";
    }

    #endregion
}
