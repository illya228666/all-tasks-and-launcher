using System.Drawing;

namespace Launcher.Pet.Hat;

internal enum HatMode
{
    Attached,
    Dragging,
    Falling,
    Resting
}

internal readonly record struct HatSupport(
    DesktopSurfaceIdentity Identity,
    float RelativeX,
    HatCollisionSegment Segment);

internal sealed class HatState
{
    internal HatMode Mode { get; set; } = HatMode.Attached;
    internal PointF Position { get; set; }
    internal float VelocityY { get; set; }
    internal float Angle { get; set; }
    internal float FallTimeSeconds { get; set; }
    internal HatSupport? Support { get; set; }
    internal bool ResolveInitialOverlap { get; set; }
}
