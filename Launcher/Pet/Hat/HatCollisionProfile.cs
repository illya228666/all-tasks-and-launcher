using System.Drawing;

namespace Launcher.Pet.Hat;

internal readonly record struct HatCollision(
    DesktopSurface Surface,
    HatCollisionSegment Segment)
{
    internal float ContactY => Segment.ContactY;
}

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
        RectangleF currentHatBounds,
        bool resolveInitialOverlap)
    {
        HatCollision? firstCollision = null;
        float firstLandingTop = float.PositiveInfinity;
        HatCollision? overlapCollision = null;
        float smallestLift = float.PositiveInfinity;

        foreach (DesktopSurface surface in surfaces)
        {
            HatCollisionSegment? supportSegment = null;
            foreach (HatCollisionSegment segment in _segments)
            {
                if (!HorizontallyOverlaps(surface.Bounds, currentHatBounds.Left, segment))
                    continue;

                if (supportSegment is null || segment.ContactY > supportSegment.Value.ContactY)
                    supportSegment = segment;

                float previousContactY = previousHatBounds.Top + segment.ContactY;
                float currentContactY = currentHatBounds.Top + segment.ContactY;
                if (previousContactY > surface.Bounds.Top || currentContactY < surface.Bounds.Top)
                    continue;

                float landingTop = surface.Bounds.Top - segment.ContactY;
                if (landingTop >= firstLandingTop)
                    continue;

                firstLandingTop = landingTop;
                firstCollision = new HatCollision(surface, segment);
            }

            // Начальное проникновение разрешается одинаково для всех источников.
            // Нижний из перекрывающихся сегментов освобождает весь профиль,
            // а между поверхностями выбирается наименьшее поднятие.
            if (!resolveInitialOverlap || supportSegment is null
                || !previousHatBounds.IntersectsWith(surface.Bounds))
                continue;

            float lift = previousHatBounds.Top + supportSegment.Value.ContactY - surface.Bounds.Top;
            if (lift < 0f || lift >= smallestLift)
                continue;
            smallestLift = lift;
            overlapCollision = new HatCollision(surface, supportSegment.Value);
        }

        return overlapCollision ?? firstCollision;
    }

    internal static bool HorizontallyOverlaps(
        Rectangle surfaceBounds,
        float hatLeft,
        HatCollisionSegment segment)
    {
        float segmentLeft = hatLeft + segment.Left;
        float segmentRight = hatLeft + segment.Right;
        return segmentRight > surfaceBounds.Left && segmentLeft < surfaceBounds.Right;
    }
}
