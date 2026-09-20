using System.Drawing;

namespace Launcher.Pet.Hat;
internal sealed class HatState
{
    internal HatMode Mode;
    internal PointF Position;
    internal float VelocityY, Angle, FallTimeSeconds, SettleTimeSeconds, SettleStartAngle;
    internal HatSupport? Support;
    internal bool ResolveInitialOverlap;
}
