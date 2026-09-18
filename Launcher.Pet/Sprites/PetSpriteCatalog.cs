using System.Drawing;

namespace Launcher.Pet.Sprites;

public static class PetSpriteCatalog
{
    public const int AtlasColumns = 8;
    public const int AtlasRows = 11;
    public const int AtlasCellWidth = 149;
    public const int AtlasCellHeight = 200;
    // Высота кликабельной области головы в пикселях нормализованного атласа.
    public const int HeadHitHeight = 90;

    private const int DefaultRenderOffsetY = 3;
    private const int DefaultHeadAnchorY = HeadHitHeight / 2;

    // Индексы: строка, затем кадр.
    // Здесь настраивается визуальная геометрия конкретного кадра, например:
    // Frame(53, renderWidth: 145, renderHeight: 194, renderOffsetX: 2, renderOffsetY: 6).
    // Размер ячейки атласа при этом остаётся фиксированным 149x200.
    private static readonly PetFrameGeometry[][] FramesByRow =
    {
        new[] { Frame(53), Frame(53), Frame(53), Frame(53), Frame(53), Frame(53), Frame(53) },
        new[] { Frame(78), Frame(82), Frame(79), Frame(77), Frame(80), Frame(80), Frame(80), Frame(75) },
        new[] { Frame(72), Frame(67), Frame(63), Frame(69), Frame(65), Frame(64), Frame(68), Frame(69) },
        new[] { Frame(53), Frame(54), Frame(62), Frame(51) },
        new[] { Frame(53), Frame(58), Frame(60), Frame(57), Frame(57) },
        new[] { Frame(47), Frame(49), Frame(46), Frame(65), Frame(63), Frame(52), Frame(53), Frame(55) },
        new[] { Frame(45), Frame(49), Frame(50), Frame(50), Frame(50), Frame(50) },
        new[] { Frame(52), Frame(48), Frame(54), Frame(52), Frame(52), Frame(56) },
        new[] { Frame(56), Frame(55), Frame(55), Frame(56), Frame(55), Frame(56) },
        new[] { Frame(54), Frame(68), Frame(66), Frame(65), Frame(65), Frame(66), Frame(70), Frame(70) },
        new[] { Frame(46), Frame(50), Frame(44), Frame(44), Frame(43), Frame(41), Frame(40), Frame(37) }
    };

    // UI может зарезервировать больше места для крупного визуального кадра,
    // но логический ground/прыжки по-прежнему используют PetLogicalGeometry.
    public static int RequiredRenderAreaHeight { get; } = ComputeRequiredRenderAreaHeight();

    internal static PetFrameGeometry GetFrameGeometry(int row, int frame) => FramesByRow[row][frame];

    public static Rectangle GetSourceRectangle(int row, int frame) =>
        new(frame * AtlasCellWidth, row * AtlasCellHeight, AtlasCellWidth, AtlasCellHeight);

    private static PetFrameGeometry Frame(
        int bodyAnchorX,
        int renderWidth = AtlasCellWidth,
        int renderHeight = AtlasCellHeight,
        int renderOffsetX = 0,
        int renderOffsetY = DefaultRenderOffsetY,
        int headAnchorY = DefaultHeadAnchorY)
    {
        if (bodyAnchorX < 0 || bodyAnchorX >= AtlasCellWidth)
            throw new ArgumentOutOfRangeException(nameof(bodyAnchorX));
        if (headAnchorY < 0 || headAnchorY >= AtlasCellHeight)
            throw new ArgumentOutOfRangeException(nameof(headAnchorY));
        if (renderWidth <= 0)
            throw new ArgumentOutOfRangeException(nameof(renderWidth));
        if (renderHeight <= 0)
            throw new ArgumentOutOfRangeException(nameof(renderHeight));

        return new(bodyAnchorX, renderWidth, renderHeight, renderOffsetX, renderOffsetY, headAnchorY);
    }

    private static int ComputeRequiredRenderAreaHeight()
    {
        int height = PetLogicalGeometry.Height;
        foreach (PetFrameGeometry[] row in FramesByRow)
            foreach (PetFrameGeometry frame in row)
                height = Math.Max(height, frame.RenderOffsetY + frame.RenderHeight);
        return height;
    }
}
