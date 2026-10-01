using System.Drawing;
namespace Launcher.Pet.Exploration;

public sealed record DesktopSeed(Rectangle Bounds, Rectangle LabelBounds, bool IsIcon);
public sealed record RuinPlatform(string Id, float Left, float Right, float Y, float RevealAt)
{
    public float Center => (Left + Right) / 2;
}
public enum RuinLinkKind { Jump, Climb, Bridge }
public sealed record RuinLink(string From, string To, float X, RuinLinkKind Kind, float? EndX = null);
public sealed record RuinBridge(string Id, string From, string To, float Left, float Right, float Y, float RevealAt);
public sealed record RuinDecoration(int Tile, RectangleF Bounds, float RevealAt, bool Mirrored = false, float Opacity = 1f, float Phase = 0, PointF? RootBottom = null, PointF? RootTop = null)
{
    public string? AssetKey { get; init; }
    public string? PlatformId { get; init; }
}
public sealed record RuinScene(Size Size, IReadOnlyList<RuinPlatform> Platforms,
    IReadOnlyList<RuinLink> Links, IReadOnlyList<RuinBridge> Bridges, IReadOnlyList<RuinDecoration> Decorations, int Revision, int Variation = 0)
{
    public DesktopWorldMetrics? Metrics { get; init; }
    [System.Text.Json.Serialization.JsonIgnore]
    public DesktopWorldMetrics WorldMetrics => Metrics ?? DesktopWorldMetrics.Legacy;
}

public static class RuinMotion
{
    public const float Gravity = 900, JumpSpeed = 430, WalkSpeed = 90, ClimbSpeed = 75;
    public const float HalfBody = 24;
    public static float ClimbX(PointF top, PointF bottom, float y, DesktopWorldMetrics? metrics = null)
    {
        float t = Math.Clamp((y - top.Y) / Math.Max(1, bottom.Y - top.Y), 0, 1);
        float scale = (metrics ?? DesktopWorldMetrics.Legacy).MotionScale;
        float bend = Math.Clamp(18 * scale + Math.Abs(bottom.X - top.X) * 0.15f, 18 * scale, 58 * scale);
        float direction = (((int)(top.X + bottom.X + top.Y) / 90) & 1) == 0 ? 1 : -1;
        return top.X + (bottom.X - top.X) * (t * t * (3 - 2 * t)) + direction * bend * MathF.Sin(MathF.PI * t);
    }
    public static float LaunchSpeed(float fromY, DesktopWorldMetrics? metrics = null)
    {
        var m = metrics ?? DesktopWorldMetrics.Legacy;
        return Math.Min(m.JumpSpeed, MathF.Sqrt(Math.Max(0, 2 * m.Gravity * (fromY - 110 * m.MotionScale))));
    }
    public static float FlightTime(float fromY, float toY, DesktopWorldMetrics? metrics = null)
    {
        var m = metrics ?? DesktopWorldMetrics.Legacy;
        float launch = LaunchSpeed(fromY, m);
        float discriminant = launch * launch + 2 * m.Gravity * (toY - fromY);
        return discriminant < 0 ? 0 : (launch + MathF.Sqrt(discriminant)) / m.Gravity;
    }
    public static bool CanJump(RuinPlatform from, RuinPlatform to, IReadOnlyList<RuinPlatform> platforms, DesktopWorldMetrics? metrics = null)
    {
        var m = metrics ?? DesktopWorldMetrics.Legacy;
        float duration = FlightTime(from.Y, to.Y, m);
        if (duration <= 0 || Math.Abs(to.Center - from.Center) > 210 * m.MotionScale * duration) return false;
        float velocityX = (to.Center - from.Center) / duration;
        float launch = LaunchSpeed(from.Y, m);
        foreach (var platform in platforms)
        {
            if (platform.Id == to.Id) continue;
            float discriminant = launch * launch + 2 * m.Gravity * (platform.Y - from.Y);
            if (discriminant < 0) continue;
            float contactTime = (launch + MathF.Sqrt(discriminant)) / m.Gravity;
            if (contactTime <= 0.001f || contactTime >= duration - 0.001f) continue;
            float x = from.Center + velocityX * contactTime;
            if (x >= platform.Left + m.HalfBody && x <= platform.Right - m.HalfBody) return false;
        }
        return true;
    }
}
