using System.Drawing;
using System.Text.Json.Serialization;

namespace Launcher.Pet.Exploration;

/// <summary>One physical-pixel contract shared by layout, movement and interaction.</summary>
public sealed record DesktopWorldMetrics(float CharacterHeight)
{
    public static DesktopWorldMetrics Legacy { get; } = new(100);
    public static DesktopWorldMetrics ForWorkArea(Size area) => new(Math.Max(1, area.Height * .20f));
    [JsonIgnore] public float MotionScale => CharacterHeight / 100;
    [JsonIgnore] public float RenderScale => CharacterHeight / 200;
    [JsonIgnore] public float HalfBody => 24 * MotionScale;
    [JsonIgnore] public float WalkSpeed => 90 * MotionScale;
    [JsonIgnore] public float ClimbSpeed => 75 * MotionScale;
    [JsonIgnore] public float Gravity => 900 * MotionScale;
    [JsonIgnore] public float JumpSpeed => 430 * MotionScale;
    [JsonIgnore] public float ItemDiameter => CharacterHeight * .12f;
    [JsonIgnore] public float ColonyWidth => CharacterHeight * .42f;
}
