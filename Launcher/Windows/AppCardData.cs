using Launcher.Apps.Data;

namespace Launcher.Windows;
internal sealed record AppCardData(AppInfo App, bool Favorite, AppUsage Usage);
