using System.Drawing;

namespace Launcher.Pet.Hat;

internal enum HatMode
{
    Attached,
    Dragging,
    Falling,
    RestingOnWindow,
    RestingOnTaskbar
}

internal sealed class HatState
{
    internal HatMode Mode { get; set; } = HatMode.Attached;
    internal PointF Position { get; set; }
    internal float VelocityY { get; set; }
    internal float Angle { get; set; }
    internal float FallTimeSeconds { get; set; }
    internal IntPtr RestingWindowHandle { get; set; }
    internal float RelativeX { get; set; }
}
