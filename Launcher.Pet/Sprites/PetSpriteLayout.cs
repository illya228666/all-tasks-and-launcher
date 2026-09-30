using System.Drawing;
namespace Launcher.Pet.Sprites;

public static class PetSpriteLayout
{
    public static Rectangle GetBounds(Point logicalPosition, PetFrameGeometry frame, Point shake, float scale = 1f, PetAppearance? appearance = null)
    {
        appearance ??= PetAppearance.Original;
        int width = (int)Math.Round(appearance.CellSize.Width * appearance.RenderScale * scale);
        int height = (int)Math.Round(appearance.CellSize.Height * appearance.RenderScale * scale);
        return new(logicalPosition.X + (int)(PetLogicalGeometry.Width * scale / 2) - Scale(frame.BodyAnchorX, width, appearance.CellSize.Width) - shake.X / 2,
            logicalPosition.Y + (int)(PetLogicalGeometry.Height * scale) - Scale(frame.GroundAnchorY, height, appearance.CellSize.Height) + Math.Min(0, shake.Y / 2), width, height);
    }
    public static Point? VisibleHead(Rectangle spriteBounds, PetFrameGeometry frame, Point areaScreenPosition, Rectangle visibleScreenBounds, PetAppearance? appearance = null)
    {
        appearance ??= PetAppearance.Original;
        Point head = new(areaScreenPosition.X + spriteBounds.X + Scale(frame.HeadAnchor.X, spriteBounds.Width, appearance.CellSize.Width),
            areaScreenPosition.Y + spriteBounds.Y + Scale(frame.HeadAnchor.Y, spriteBounds.Height, appearance.CellSize.Height));
        return visibleScreenBounds.Contains(head) ? head : null;
    }
    public static Point MapDestinationToAtlasCell(Point point, Rectangle bounds, PetAppearance? appearance = null)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0 || !bounds.Contains(point))
            throw new ArgumentOutOfRangeException(nameof(point));
        appearance ??= PetAppearance.Original;
        return new((int)((long)(point.X - bounds.X) * appearance.CellSize.Width / bounds.Width),
            (int)((long)(point.Y - bounds.Y) * appearance.CellSize.Height / bounds.Height));
    }
    private static int Scale(int value, int destination, int source) => (int)Math.Round((double)value * destination / source);
}
