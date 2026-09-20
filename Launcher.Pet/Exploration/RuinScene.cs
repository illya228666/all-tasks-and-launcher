using System.Drawing;
namespace Launcher.Pet.Exploration;

public sealed record DesktopSeed(Rectangle Bounds, Rectangle LabelBounds, bool IsIcon);
public sealed record RuinPlatform(string Id, float Left, float Right, float Y, float RevealAt)
{
    public float Center => (Left + Right) / 2;
}
public sealed record RuinLink(string From, string To, float X, bool Climb, float? EndX = null);
public sealed record RuinDecoration(int Tile, RectangleF Bounds, float RevealAt, bool Mirrored = false, float Opacity = 1f, float Phase = 0, PointF? RootBottom = null, PointF? RootTop = null);
public sealed record RuinScene(Size Size, IReadOnlyList<RuinPlatform> Platforms,
    IReadOnlyList<RuinLink> Links, IReadOnlyList<RuinDecoration> Decorations, int Revision, int Variation = 0);

public static class RuinMotion
{
    public const float Gravity = 900, JumpSpeed = 430, WalkSpeed = 90, ClimbSpeed = 75;
    public const float HalfBody = 24;
    public static float LaunchSpeed(float fromY) => Math.Min(JumpSpeed, MathF.Sqrt(Math.Max(0, 2 * Gravity * (fromY - 110))));
    public static float FlightTime(float fromY, float toY)
    {
        float launch = LaunchSpeed(fromY);
        float discriminant = launch * launch + 2 * Gravity * (toY - fromY);
        return discriminant < 0 ? 0 : (launch + MathF.Sqrt(discriminant)) / Gravity;
    }
    public static bool CanJump(RuinPlatform from, RuinPlatform to, IReadOnlyList<RuinPlatform> platforms)
    {
        float duration = FlightTime(from.Y, to.Y);
        if (duration <= 0 || Math.Abs(to.Center - from.Center) > 210 * duration) return false;
        float velocityX = (to.Center - from.Center) / duration;
        float launch = LaunchSpeed(from.Y);
        foreach (var platform in platforms)
        {
            if (platform.Id == to.Id) continue;
            float discriminant = launch * launch + 2 * Gravity * (platform.Y - from.Y);
            if (discriminant < 0) continue;
            float contactTime = (launch + MathF.Sqrt(discriminant)) / Gravity;
            if (contactTime <= 0.001f || contactTime >= duration - 0.001f) continue;
            float x = from.Center + velocityX * contactTime;
            if (x >= platform.Left + HalfBody && x <= platform.Right - HalfBody) return false;
        }
        return true;
    }
}
