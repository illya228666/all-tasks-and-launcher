using System.Drawing;
namespace Launcher.Pet.Sprites;

// Authored coordinates in the original atlas cell, shared by both hat variants.
public readonly record struct PetFrameGeometry(int BodyAnchorX, int GroundAnchorY, Point HeadAnchor, Rectangle HeadBounds)
{
    public Point? ItemAnchor { get; init; }
    public Point? HatAnchor { get; init; }
}
