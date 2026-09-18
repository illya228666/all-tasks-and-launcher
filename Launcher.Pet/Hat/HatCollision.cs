namespace Launcher.Pet.Hat;
public readonly record struct HatCollision(HatSurface Surface, HatCollisionSegment Segment)
{
    public float ContactY => Segment.ContactY;
}
