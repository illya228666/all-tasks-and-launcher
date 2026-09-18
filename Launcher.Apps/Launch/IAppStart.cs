using Launcher.Apps.Data;

namespace Launcher.Apps.Launch;
public interface IAppStart
{
    AppLaunchResult Start(AppInfo app, bool asAdmin);
}
