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

internal readonly record struct HatCollisionConnector(
    float X,
    float Top,
    float Bottom,
    HatCollisionSegment ResolveSegment);

/// <summary>
/// Нижний профиль столкновения шляпы. Боковые поля находятся ниже центральной
/// выемки: узкая поверхность может войти в центр шляпы глубже, а широкая
/// поверхность зацепит один из вертикальных соединителей и будет вытолкнута
/// на соответствующее боковое поле.
/// </summary>
internal sealed class HatCollisionProfile
{
    private const float SideWidthRatio = 0.1f;
    private const float SurfaceOverlap = 25f;
    private const float CenterRecessDepth = 16f;

    private readonly HatCollisionSegment[] _segments;
    private readonly HatCollisionConnector[] _connectors;

    internal IReadOnlyList<HatCollisionSegment> Segments => _segments;
    internal IReadOnlyList<HatCollisionConnector> Connectors => _connectors;

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

        _connectors = new[]
        {
            new HatCollisionConnector(
                sideWidth,
                centerContactY,
                brimContactY,
                _segments[0]),
            new HatCollisionConnector(
                hatSize.Width - sideWidth,
                centerContactY,
                brimContactY,
                _segments[2])
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
            foreach (HatCollisionSegment segment in _segments)
            {
                if (!HorizontallyOverlaps(surface.Bounds, currentHatBounds.Left, segment))
                    continue;

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

            if (!resolveInitialOverlap)
                continue;

            HatCollisionSegment? connectorCollision =
                FindConnectorCollision(surface.Bounds, previousHatBounds);
            if (connectorCollision is null)
                continue;

            float lift =
                previousHatBounds.Top + connectorCollision.Value.ContactY - surface.Bounds.Top;
            if (lift < 0f || lift >= smallestLift)
                continue;

            smallestLift = lift;
            overlapCollision = new HatCollision(surface, connectorCollision.Value);
        }

        return overlapCollision ?? firstCollision;
    }

    private HatCollisionSegment? FindConnectorCollision(
        Rectangle surfaceBounds,
        RectangleF hatBounds)
    {
        foreach (HatCollisionConnector connector in _connectors)
        {
            float connectorX = hatBounds.Left + connector.X;
            if (surfaceBounds.Left >= connectorX || surfaceBounds.Right <= connectorX)
                continue;

            float connectorTop = hatBounds.Top + connector.Top;
            float connectorBottom = hatBounds.Top + connector.Bottom;
            if (surfaceBounds.Top < connectorTop || surfaceBounds.Top > connectorBottom)
                continue;

            return connector.ResolveSegment;
        }

        return null;
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
