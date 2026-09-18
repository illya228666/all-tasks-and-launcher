using System.Drawing;

namespace Launcher.Pet.Hat;
internal sealed class HatWorld
{
    internal static readonly Size ImageSize = new(109, 64);
    private readonly HatState _state = new();
    private readonly HatPhysics _physics = new();
    private readonly HatCollisionProfile _collision = new(ImageSize);
    internal HatScene Scene => new(_state.Mode, Point.Round(_state.Position), _state.Angle);
    internal bool Attached => _state.Mode == HatMode.Attached;

    internal void BeginDrag(Point cursor)
    {
        _state.Mode = HatMode.Dragging;
        _state.Support = null;
        _state.Angle = 0;
        Drag(cursor);
    }

    internal void Drag(Point cursor) => _state.Position = new(cursor.X - ImageSize.Width / 2, cursor.Y - ImageSize.Height / 2);
    internal void Drop(bool onHead)
    {
        if (onHead)
            Attach();
        else
            Fall();
    }

    internal void Attach()
    {
        _state.Mode = HatMode.Attached;
        _state.Support = null;
        _state.VelocityY = _state.Angle = 0;
    }

    internal void KnockOff(Point head)
    {
        if (Attached)
            _state.Position = new(head.X - ImageSize.Width / 2, head.Y - ImageSize.Height);
        Fall();
        _state.VelocityY = -180f;
    }

    private void Fall()
    {
        _state.Mode = HatMode.Falling;
        _state.Support = null;
        _state.VelocityY = _state.Angle = _state.FallTimeSeconds = 0;
        _state.ResolveInitialOverlap = true;
    }

    internal Point? PickupPoint(IReadOnlyList<HatSurface> surfaces)
    {
        if (_state.Mode != HatMode.Resting || _state.Support is not HatSupport support)
            return null;
        HatSurface? ground = surfaces.FirstOrDefault(surface => surface.Identity == support.Identity && surface.Kind == HatSurfaceKind.PetGround);
        if (ground is null)
            return null;
        float x = ground.Bounds.Left + support.RelativeX;
        return HatCollisionProfile.HorizontallyOverlaps(ground.Bounds, x, support.Segment) ? new Point((int)Math.Round(x + ImageSize.Width / 2f), ground.Bounds.Top) : null;
    }

    internal void Update(float elapsedSeconds, IReadOnlyList<HatSurface> surfaces)
    {
        if (_state.Mode == HatMode.Falling)
        {
            RectangleF previous = new(_state.Position, ImageSize);
            _physics.Advance(_state, elapsedSeconds);
            HatCollision? collision = _collision.FindFirstCollision(surfaces, previous, new RectangleF(_state.Position, ImageSize), _state.ResolveInitialOverlap);
            _state.ResolveInitialOverlap = false;
            if (collision is HatCollision found)
                Land(found);
        }
        else if (_state.Mode == HatMode.Resting && _state.Support is HatSupport support)
        {
            RectangleF bounds = new(_state.Position, ImageSize);
            HatCollision? takeover = _collision.FindFirstCollision(surfaces.Where(surface => surface.Identity != support.Identity), bounds, bounds, true);
            if (takeover is HatCollision found)
            {
                Land(found);
                return;
            }

            HatSurface? surface = surfaces.FirstOrDefault(item => item.Identity == support.Identity);
            if (surface is null || !HatCollisionProfile.HorizontallyOverlaps(surface.Bounds, surface.Bounds.Left + support.RelativeX, support.Segment))
            {
                Fall();
                return;
            }

            _state.Position = new(surface.Bounds.Left + support.RelativeX, surface.Bounds.Top - support.Segment.ContactY);
        }
    }

    private void Land(HatCollision collision)
    {
        _state.Position = new(_state.Position.X, collision.Surface.Bounds.Top - collision.ContactY);
        _state.VelocityY = 0;
        _state.Support = new(collision.Surface.Identity, _state.Position.X - collision.Surface.Bounds.Left, collision.Segment);
        _state.Mode = HatMode.Resting;
    }
}
