namespace Launcher.Pet.Windows.Desktop;
internal readonly record struct DesktopSurfaceIdentity(DesktopSurfaceType Type, IntPtr WindowHandle, string? ItemKey = null);
