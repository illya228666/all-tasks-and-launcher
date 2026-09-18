using System.Drawing;

namespace Launcher.Pet.Sprites;

// Однонаправленное преобразование логической позиции в визуальную геометрию.
// Не изменяет PetState и не читает исходные изображения.
public static class PetSpriteLayout
{
    internal static Rectangle GetBounds(Point logicalPosition, PetFrameGeometry frame, Point shake)
    {
        int bodyAnchorX = Scale(frame.BodyAnchorX, frame.RenderWidth, PetSpriteCatalog.AtlasCellWidth);
        return new(
            logicalPosition.X + PetLogicalGeometry.Width / 2 - bodyAnchorX + frame.RenderOffsetX - shake.X / 2,
            logicalPosition.Y + frame.RenderOffsetY + Math.Min(0, shake.Y / 2),
            frame.RenderWidth,
            frame.RenderHeight);
    }

    internal static Point? VisibleHead(Rectangle spriteBounds, PetFrameGeometry frame, Point areaScreenPosition, Rectangle visibleScreenBounds)
    {
        var head = new Point(
            areaScreenPosition.X + spriteBounds.X + Scale(frame.BodyAnchorX, spriteBounds.Width, PetSpriteCatalog.AtlasCellWidth),
            areaScreenPosition.Y + spriteBounds.Y + Scale(frame.HeadAnchorY, spriteBounds.Height, PetSpriteCatalog.AtlasCellHeight));
        return visibleScreenBounds.Contains(head) ? head : null;
    }

    // Переводит точку из destination rectangle обратно в локальные координаты
    // фиксированной ячейки атласа. Нужен hit-test при разных RenderWidth/Height.
    public static Point MapDestinationToAtlasCell(Point destinationPoint, Rectangle spriteBounds) =>
        new(
            MapCoordinate(destinationPoint.X - spriteBounds.X, spriteBounds.Width, PetSpriteCatalog.AtlasCellWidth),
            MapCoordinate(destinationPoint.Y - spriteBounds.Y, spriteBounds.Height, PetSpriteCatalog.AtlasCellHeight));

    private static int Scale(int value, int destinationSize, int sourceSize) =>
        (int)Math.Round((double)value * destinationSize / sourceSize);

    private static int MapCoordinate(int value, int destinationSize, int sourceSize)
    {
        if (destinationSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(destinationSize));

        return Math.Clamp((int)((long)value * sourceSize / destinationSize), 0, sourceSize - 1);
    }
}
