using System.Drawing;

namespace Launcher.Pet.Sprites;

// Однонаправленное преобразование логической позиции в визуальную геометрию.
// Не изменяет PetState и не читает исходные изображения.
internal static class PetSpriteLayout
{
    internal static Rectangle GetBounds(Point logicalPosition, PetFrameGeometry frame, Point shake)
    {
        int bodyAnchorX = Scale(frame.BodyAnchorX, frame.Width, PetSpriteCatalog.AtlasCellWidth);
        return new(
            logicalPosition.X + PetLogicalGeometry.Width / 2 - bodyAnchorX + frame.OffsetX - shake.X / 2,
            logicalPosition.Y + frame.OffsetY + Math.Min(0, shake.Y / 2),
            frame.Width, frame.Height);
    }

    internal static Point? VisibleHead(Rectangle spriteBounds, PetFrameGeometry frame, Point areaScreenPosition, Rectangle visibleScreenBounds)
    {
        var head = new Point(
            areaScreenPosition.X + spriteBounds.X + Scale(frame.BodyAnchorX, spriteBounds.Width, PetSpriteCatalog.AtlasCellWidth),
            areaScreenPosition.Y + spriteBounds.Y + Scale(frame.HeadAnchorY, spriteBounds.Height, PetSpriteCatalog.AtlasCellHeight));
        return visibleScreenBounds.Contains(head) ? head : null;
    }

    private static int Scale(int value, int destinationSize, int sourceSize) =>
        (int)Math.Round((double)value * destinationSize / sourceSize);
}
