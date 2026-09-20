using System.Drawing;
namespace Launcher.Pet.Sprites;

public static class PetSpriteLayout
{
    public static Rectangle GetBounds(Point logicalPosition, PetFrameGeometry frame, Point shake, float scale = 1f)
    {
        int width = (int)Math.Round(PetSpriteCatalog.AtlasCellWidth * PetSpriteCatalog.RenderScale * scale);
        int height = (int)Math.Round(PetSpriteCatalog.AtlasCellHeight * PetSpriteCatalog.RenderScale * scale);
        return new(logicalPosition.X + (int)(PetLogicalGeometry.Width * scale / 2) - Scale(frame.BodyAnchorX, width, PetSpriteCatalog.AtlasCellWidth) - shake.X / 2,
            logicalPosition.Y + (int)(PetLogicalGeometry.Height * scale) - Scale(frame.GroundAnchorY, height, PetSpriteCatalog.AtlasCellHeight) + Math.Min(0, shake.Y / 2), width, height);
    }
    public static Point? VisibleHead(Rectangle spriteBounds, PetFrameGeometry frame, Point areaScreenPosition, Rectangle visibleScreenBounds)
    {
        Point head = new(areaScreenPosition.X + spriteBounds.X + Scale(frame.HeadAnchor.X, spriteBounds.Width, PetSpriteCatalog.AtlasCellWidth),
            areaScreenPosition.Y + spriteBounds.Y + Scale(frame.HeadAnchor.Y, spriteBounds.Height, PetSpriteCatalog.AtlasCellHeight));
        return visibleScreenBounds.Contains(head) ? head : null;
    }
    public static Point MapDestinationToAtlasCell(Point point, Rectangle bounds)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0 || !bounds.Contains(point))
            throw new ArgumentOutOfRangeException(nameof(point));
        return new((int)((long)(point.X - bounds.X) * PetSpriteCatalog.AtlasCellWidth / bounds.Width),
            (int)((long)(point.Y - bounds.Y) * PetSpriteCatalog.AtlasCellHeight / bounds.Height));
    }
    private static int Scale(int value, int destination, int source) => (int)Math.Round((double)value * destination / source);
}
