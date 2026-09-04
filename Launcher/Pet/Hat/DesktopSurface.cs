using System.Drawing;

namespace Launcher.Pet.Hat;

internal enum DesktopSurfaceType
{
    Window,
    DesktopIcon,
    Taskbar
}

internal readonly record struct DesktopSurfaceIdentity(
    DesktopSurfaceType Type,
    IntPtr WindowHandle,
    string? ItemKey = null);

internal readonly record struct DesktopSurface(
    Rectangle Bounds,
    DesktopSurfaceIdentity Identity)
{
    internal DesktopSurfaceType Type => Identity.Type;
}
