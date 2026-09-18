using Launcher.Apps.Data;

namespace Launcher.Apps.List;
public interface IAppSource
{
    AppSourceResult Read();
}
