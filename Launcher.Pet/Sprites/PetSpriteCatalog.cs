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

    // Индексы: строка, затем кадр. Настройка одного кадра, например:
    // new(53, Width: 145, Height: 194, OffsetX: 2, OffsetY: 6).
    // Значения по умолчанию сохраняют прежние размеры, смещения и привязки.
    private static readonly PetFrameGeometry[][] FramesByRow =
    {
        new PetFrameGeometry[] { new(53), new(53), new(53), new(53), new(53), new(53), new(53) },
        new PetFrameGeometry[] { new(78), new(82), new(79), new(77), new(80), new(80), new(80), new(75) },
        new PetFrameGeometry[] { new(72), new(67), new(63), new(69), new(65), new(64), new(68), new(69) },
        new PetFrameGeometry[] { new(53), new(54), new(62), new(51) },
        new PetFrameGeometry[] { new(53), new(58), new(60), new(57), new(57) },
        new PetFrameGeometry[] { new(47), new(49), new(46), new(65), new(63), new(52), new(53), new(55) },
        new PetFrameGeometry[] { new(45), new(49), new(50), new(50), new(50), new(50) },
        new PetFrameGeometry[] { new(52), new(48), new(54), new(52), new(52), new(56) },
        new PetFrameGeometry[] { new(56), new(55), new(55), new(56), new(55), new(56) },
        new PetFrameGeometry[] { new(54), new(68), new(66), new(65), new(65), new(66), new(70), new(70) },
        new PetFrameGeometry[] { new(46), new(50), new(44), new(44), new(43), new(41), new(40), new(37) }
    };

    internal static PetFrameGeometry GetFrameGeometry(int row, int frame) => FramesByRow[row][frame];

    public static Rectangle GetSourceRectangle(int row, int frame) =>
        new(frame * AtlasCellWidth, row * AtlasCellHeight, AtlasCellWidth, AtlasCellHeight);
}
