using System.Drawing;

namespace Launcher.Pet.Hat;

internal readonly record struct HatCollision(
    DesktopSurface Surface,
    float ContactY);

internal readonly record struct HatCollisionSegment(
    float Left,
    float Right,
    float ContactY);

/// <summary>
/// Нижний профиль столкновения шляпы. Боковые поля находятся ниже центральной
/// выемки: узкая поверхность может войти в центр шляпы глубже, а широкая
/// поверхность первой зацепит одно из полей.
/// </summary>
internal sealed class HatCollisionProfile
{
    private const float SideWidthRatio = 0.24f;
    private const float SurfaceOverlap = 8f;
    private const float CenterRecessDepth = 16f;

    private readonly HatCollisionSegment[] _segments;

    internal IReadOnlyList<HatCollisionSegment> Segments => _segments;

    internal HatCollisionProfile(Size hatSize)
    {
        float sideWidth = hatSize.Width * SideWidthRatio;
        float brimContactY = hatSize.Height - SurfaceOverlap;
        float centerContactY = brimContactY - CenterRecessDepth;

        _segments = new[]
        {
            new HatCollisionSegment(0f, sideWidth, brimContactY),
            new HatCollisionSegment(sideWidth, hatSize.Width - sideWidth, centerContactY),
            new HatCollisionSegment(hatSize.Width - sideWidth, hatSize.Width, brimContactY)
        };
    }

    internal HatCollision? FindFirstCollision(
        IEnumerable<DesktopSurface> surfaces,
        RectangleF previousHatBounds,
        RectangleF currentHatBounds)
    {
        HatCollision? firstCollision = null;
        float firstLandingTop = float.PositiveInfinity;

        foreach (DesktopSurface surface in surfaces)
        {
            foreach (HatCollisionSegment segment in _segments)
            {
                if (!HorizontallyOverlaps(surface.Bounds, currentHatBounds.Left, segment))
                    continue;

                float previousContactY = previousHatBounds.Top + segment.ContactY;
                float currentContactY = currentHatBounds.Top + segment.ContactY;
                if (previousContactY >= surface.Bounds.Top || currentContactY < surface.Bounds.Top)
                    continue;

                float landingTop = surface.Bounds.Top - segment.ContactY;
                if (landingTop >= firstLandingTop)
                    continue;

                firstLandingTop = landingTop;
                firstCollision = new HatCollision(surface, segment.ContactY);
            }
        }

        return firstCollision;
    }

    internal float? GetSupportContactY(Rectangle surfaceBounds, float hatLeft)
    {
        float? contactY = null;
        foreach (HatCollisionSegment segment in _segments)
        {
            if (!HorizontallyOverlaps(surfaceBounds, hatLeft, segment))
                continue;

            // Если поверхность одновременно попадает под поле и под центральную
            // выемку, более низкое поле касается её раньше.
            contactY = contactY.HasValue
                ? Math.Max(contactY.Value, segment.ContactY)
                : segment.ContactY;
        }

        return contactY;
    }

    private static bool HorizontallyOverlaps(
        Rectangle surfaceBounds,
        float hatLeft,
        HatCollisionSegment segment)
    {
        float segmentLeft = hatLeft + segment.Left;
        float segmentRight = hatLeft + segment.Right;
        return segmentRight > surfaceBounds.Left && segmentLeft < surfaceBounds.Right;
    }
}
