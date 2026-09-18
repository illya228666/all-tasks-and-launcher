using Launcher.Apps.Data;
using Launcher.Apps.List;

namespace Launcher.Apps.Launch;
public sealed class AppLaunch
{
    private readonly AppList _apps;
    private readonly IAppStart _start;
    public AppLaunch(AppList apps, IAppStart start)
    {
        _apps = apps;
        _start = start;
    }

    public AppLaunchResult Start(AppInfo app, bool asAdmin = false)
    {
        AppLaunchResult result = _start.Start(app, asAdmin);
        if (result.Status == AppLaunchStatus.Started)
            _apps.RegisterLaunch(app);
        return result;
    }
}
