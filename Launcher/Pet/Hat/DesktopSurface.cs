using System.Drawing;

namespace Launcher.Pet.Hat;

internal enum DesktopSurfaceType
{
    Window,
    Taskbar
}

internal readonly record struct DesktopSurface(
    Rectangle Bounds,
    IntPtr WindowHandle,
    DesktopSurfaceType Type);
