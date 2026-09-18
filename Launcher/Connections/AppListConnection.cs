using System.ComponentModel;
using System.Runtime.InteropServices;
using Launcher.Apps.Data;
using Launcher.Apps.Launch;
using Launcher.Apps.List;
using Launcher.Apps.Windows.Launch;
using Launcher.Windows;

namespace Launcher.Connections;
internal sealed class AppListConnection : IDisposable
{
    private static readonly string[] SurpriseLines =
    {
        "Zufallsmodus aktiviert.",
        "Neue Runde. Neues Glueck.",
        "Mission gestartet.",
        "Bam. Naechstes Projekt!"
    };
    private readonly MainWindow _window;
    private readonly AppList _apps;
    private readonly AppLaunch _launch;
    private readonly Action _save;
    private readonly Random _random = new();
    internal AppListConnection(MainWindow window, AppList apps, AppLaunch launch, Action save)
    {
        _window = window;
        _apps = apps;
        _launch = launch;
        _save = save;
        window.Search.FilterChanged += FilterChanged;
        window.Search.RefreshRequested += Refresh;
        window.Search.RandomRequested += LaunchRandom;
        window.Search.RootRequested += OpenRoot;
        window.RefreshRequested += Refresh;
        window.RandomRequested += LaunchRandom;
        window.Apps.Requested += CardRequested;
    }

    internal void Refresh()
    {
        IReadOnlyList<string> warnings = _apps.Refresh();
        _window.Search.Display(_apps.Filter, _apps.Categories());
        Display();
        _window.Status.ShowHint(warnings.Count == 0 ? "Liste aktualisiert." : string.Join(" | ", warnings));
    }

    private void FilterChanged(AppFilter filter)
    {
        _apps.SetFilter(filter);
        Display();
        _save();
    }

    private void Display()
    {
        var visible = _apps.VisibleApps();
        _window.Apps.Display(visible.Select(app => new AppCardData(app, _apps.IsFavorite(app), _apps.GetUsage(app))).ToArray(), _apps.Filter.Sort == AppSortOrder.ByCategory && _apps.Filter.Category is null);
        _window.Status.ShowStats($"Sichtbar: {visible.Count}/{_apps.Count} | Lauffaehig: {visible.Count(app => app.IsAvailable)} | Favoriten: {_apps.FavoriteCount} | Starts gesamt: {_apps.LaunchCount}");
    }

    private void CardRequested(AppInfo app, AppCardAction action)
    {
        try
        {
            switch (action)
            {
                case AppCardAction.Start:
                    Start(app, false, false);
                    break;
                case AppCardAction.StartAsAdmin:
                    Start(app, true, false);
                    break;
                case AppCardAction.OpenFolder:
                    AppProcess.OpenFolder(app.FolderPath);
                    break;
                case AppCardAction.CopyPath:
                    Clipboard.SetText(app.ExePath);
                    _window.Status.ShowHint("Pfad kopiert.");
                    break;
                case AppCardAction.ToggleFavorite:
                    _apps.ToggleFavorite(app);
                    Display();
                    _save();
                    break;
            }
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ExternalException)
        {
            _window.Status.ShowHint(error.Message);
        }
    }

    private void Start(AppInfo app, bool asAdmin, bool random)
    {
        AppLaunchResult result = _launch.Start(app, asAdmin);
        if (result.Status == AppLaunchStatus.Started)
        {
            Display();
            _window.Status.ShowHint(random ? $"{SurpriseLines[_random.Next(SurpriseLines.Length)]} ({app.Name})" : $"{app.Name} gestartet.");
            _save();
        }
        else
            _window.Status.ShowHint(result.Status switch
            {
                AppLaunchStatus.Cancelled => "Admin-Start wurde abgebrochen.",
                AppLaunchStatus.Missing => "Datei nicht gefunden: " + result.Error,
                _ => "Start fehlgeschlagen: " + result.Error
            });
    }

    private void LaunchRandom()
    {
        var candidates = _apps.VisibleApps().Where(app => app.IsAvailable).ToArray();
        if (candidates.Length == 0)
        {
            _window.Status.ShowHint("Kein startbares Projekt gefunden.");
            return;
        }

        Start(candidates[_random.Next(candidates.Length)], false, true);
    }

    private void OpenRoot()
    {
        try
        {
            AppProcess.OpenFolder(_apps.RootFolder);
        }
        catch (Exception error) when (error is Win32Exception or IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            _window.Status.ShowHint(error.Message);
        }
    }

    public void Dispose()
    {
        _window.Search.FilterChanged -= FilterChanged;
        _window.Search.RefreshRequested -= Refresh;
        _window.Search.RandomRequested -= LaunchRandom;
        _window.Search.RootRequested -= OpenRoot;
        _window.RefreshRequested -= Refresh;
        _window.RandomRequested -= LaunchRandom;
        _window.Apps.Requested -= CardRequested;
    }
}
