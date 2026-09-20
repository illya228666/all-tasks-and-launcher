using System.Drawing;

namespace Launcher.Pet.Windows.Desktop;
internal readonly record struct DesktopSurface(Rectangle Bounds, DesktopSurfaceIdentity Identity, Rectangle VisualBounds = default, Rectangle LabelBounds = default)
{
    internal DesktopSurfaceType Type => Identity.Type;
}
