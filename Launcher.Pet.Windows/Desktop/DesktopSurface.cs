using System.Drawing;

namespace Launcher.Pet.Windows.Desktop;
internal readonly record struct DesktopSurface(Rectangle Bounds, DesktopSurfaceIdentity Identity)
{
    internal DesktopSurfaceType Type => Identity.Type;
}
