using System.Drawing;

namespace Launcher.Pet.Hat;

internal enum DesktopSurfaceType
{
    Window,
    DesktopIcon,
    Taskbar
}

internal readonly record struct DesktopSurface(
    Rectangle Bounds,
    IntPtr WindowHandle,
    DesktopSurfaceType Type);
