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

    // Preset'ы отвечают только за визуальный размер/offset кадра.
    // Пока все значения сохраняют прежний вид 149x200; дальше достаточно менять
    // одну строку здесь, чтобы скорректировать целую группу кадров.
    private static readonly IReadOnlyDictionary<PetFramePreset, PetFramePresetGeometry> Presets =
        new Dictionary<PetFramePreset, PetFramePresetGeometry>
        {
            [PetFramePreset.Standing] = Preset(),
            [PetFramePreset.Running] = Preset(),
            [PetFramePreset.Waving] = Preset(),
            [PetFramePreset.Jumping] = Preset(),
            [PetFramePreset.Failed] = Preset(),
            [PetFramePreset.Action] = Preset(),
            [PetFramePreset.Looking] = Preset(),
            [PetFramePreset.LookingUp] = Preset(),
            [PetFramePreset.LookingDown] = Preset()
        };

    // Индексы: строка, затем кадр.
    // BodyAnchorX остаётся индивидуальным для каждого изображения.
    // Остальная геометрия наследуется из preset'а и при необходимости
    // переопределяется прямо в Frame(...), например:
    // Frame(53, PetFramePreset.Standing, renderWidth: 145, renderOffsetX: 2).
    private static readonly PetFrameGeometry[][] FramesByRow =
    {
        new[]
        {
            Frame(53, PetFramePreset.Standing), Frame(53, PetFramePreset.Standing),
            Frame(53, PetFramePreset.Standing), Frame(53, PetFramePreset.Standing),
            Frame(53, PetFramePreset.Standing), Frame(53, PetFramePreset.Standing),
            Frame(53, PetFramePreset.Standing)
        },
        new[]
        {
            Frame(78, PetFramePreset.Running), Frame(82, PetFramePreset.Running),
            Frame(79, PetFramePreset.Running), Frame(77, PetFramePreset.Running),
            Frame(80, PetFramePreset.Running), Frame(80, PetFramePreset.Running),
            Frame(80, PetFramePreset.Running), Frame(75, PetFramePreset.Running)
        },
        new[]
        {
            Frame(72, PetFramePreset.Running), Frame(67, PetFramePreset.Running),
            Frame(63, PetFramePreset.Running), Frame(69, PetFramePreset.Running),
            Frame(65, PetFramePreset.Running), Frame(64, PetFramePreset.Running),
            Frame(68, PetFramePreset.Running), Frame(69, PetFramePreset.Running)
        },
        new[]
        {
            Frame(53, PetFramePreset.Waving), Frame(54, PetFramePreset.Waving),
            Frame(62, PetFramePreset.Waving), Frame(51, PetFramePreset.Waving)
        },
        new[]
        {
            Frame(53, PetFramePreset.Jumping), Frame(58, PetFramePreset.Jumping),
            Frame(60, PetFramePreset.Jumping), Frame(57, PetFramePreset.Jumping),
            Frame(57, PetFramePreset.Jumping)
        },
        new[]
        {
            Frame(47, PetFramePreset.Failed), Frame(49, PetFramePreset.Failed),
            Frame(46, PetFramePreset.Failed), Frame(65, PetFramePreset.Failed),
            Frame(63, PetFramePreset.Failed), Frame(52, PetFramePreset.Failed),
            Frame(53, PetFramePreset.Failed), Frame(55, PetFramePreset.Failed)
        },
        new[]
        {
            Frame(45, PetFramePreset.Action), Frame(49, PetFramePreset.Action),
            Frame(50, PetFramePreset.Action), Frame(50, PetFramePreset.Action),
            Frame(50, PetFramePreset.Action), Frame(50, PetFramePreset.Action)
        },
        new[]
        {
            Frame(52, PetFramePreset.Action), Frame(48, PetFramePreset.Action),
            Frame(54, PetFramePreset.Action), Frame(52, PetFramePreset.Action),
            Frame(52, PetFramePreset.Action), Frame(56, PetFramePreset.Action)
        },
        new[]
        {
            Frame(56, PetFramePreset.Action), Frame(55, PetFramePreset.Action),
            Frame(55, PetFramePreset.Action), Frame(56, PetFramePreset.Action),
            Frame(55, PetFramePreset.Action), Frame(56, PetFramePreset.Action)
        },
        new[]
        {
            // LookIndex 0 = прямо вверх; остальные кадры строки — поворот от верха к низу.
            Frame(54, PetFramePreset.LookingUp),
            Frame(68, PetFramePreset.Looking), Frame(66, PetFramePreset.Looking),
            Frame(65, PetFramePreset.Looking), Frame(65, PetFramePreset.Looking),
            Frame(66, PetFramePreset.Looking), Frame(70, PetFramePreset.Looking),
            Frame(70, PetFramePreset.Looking)
        },
        new[]
        {
            // LookIndex 8 = прямо вниз; остальные продолжают круг до направления вверх.
            Frame(46, PetFramePreset.LookingDown),
            Frame(50, PetFramePreset.Looking), Frame(44, PetFramePreset.Looking),
            Frame(44, PetFramePreset.Looking), Frame(43, PetFramePreset.Looking),
            Frame(41, PetFramePreset.Looking), Frame(40, PetFramePreset.Looking),
            Frame(37, PetFramePreset.Looking)
        }
    };

    // UI может зарезервировать больше места для крупного визуального кадра,
    // но логический ground/прыжки по-прежнему используют PetLogicalGeometry.
    public static int RequiredRenderAreaHeight { get; } = ComputeRequiredRenderAreaHeight();

    internal static PetFrameGeometry GetFrameGeometry(int row, int frame) => FramesByRow[row][frame];

    public static Rectangle GetSourceRectangle(int row, int frame) =>
        new(frame * AtlasCellWidth, row * AtlasCellHeight, AtlasCellWidth, AtlasCellHeight);

    private static PetFramePresetGeometry Preset(
        int renderWidth = AtlasCellWidth,
        int renderHeight = AtlasCellHeight,
        int renderOffsetX = 0,
        int renderOffsetY = DefaultRenderOffsetY,
        int headAnchorY = DefaultHeadAnchorY) =>
        new(renderWidth, renderHeight, renderOffsetX, renderOffsetY, headAnchorY);

    private static PetFrameGeometry Frame(
        int bodyAnchorX,
        PetFramePreset preset,
        int? renderWidth = null,
        int? renderHeight = null,
        int? renderOffsetX = null,
        int? renderOffsetY = null,
        int? headAnchorY = null)
    {
        PetFramePresetGeometry inherited = Presets[preset];

        int width = renderWidth ?? inherited.RenderWidth;
        int height = renderHeight ?? inherited.RenderHeight;
        int offsetX = renderOffsetX ?? inherited.RenderOffsetX;
        int offsetY = renderOffsetY ?? inherited.RenderOffsetY;
        int headY = headAnchorY ?? inherited.HeadAnchorY;

        ValidateFrameGeometry(bodyAnchorX, width, height, headY);
        return new(bodyAnchorX, width, height, offsetX, offsetY, headY);
    }

    private static void ValidateFrameGeometry(int bodyAnchorX, int renderWidth, int renderHeight, int headAnchorY)
    {
        if (bodyAnchorX < 0 || bodyAnchorX >= AtlasCellWidth)
            throw new ArgumentOutOfRangeException(nameof(bodyAnchorX));
        if (headAnchorY < 0 || headAnchorY >= AtlasCellHeight)
            throw new ArgumentOutOfRangeException(nameof(headAnchorY));
        if (renderWidth <= 0)
            throw new ArgumentOutOfRangeException(nameof(renderWidth));
        if (renderHeight <= 0)
            throw new ArgumentOutOfRangeException(nameof(renderHeight));
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
