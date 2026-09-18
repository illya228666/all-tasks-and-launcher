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
    private const int DefaultRenderBottomY = DefaultRenderOffsetY + AtlasCellHeight;
    private const int DefaultHeadAnchorY = HeadHitHeight / 2;

    // Масштаб — единый по X/Y. Это намеренно: preset меняет размер персонажа,
    // но никогда не растягивает и не сплющивает конкретный кадр.
    //
    // Значения первых шести строк приблизительно восстанавливают относительный
    // визуальный размер исходного sprite sheet после NormalizeAtlases().
    private static readonly IReadOnlyDictionary<PetFramePreset, PetFramePresetGeometry> Presets =
        new Dictionary<PetFramePreset, PetFramePresetGeometry>
        {
            [PetFramePreset.Standing] = Preset(renderScale: 0.98f),
            [PetFramePreset.Running] = Preset(renderScale: 0.945f),
            [PetFramePreset.Crouched] = Preset(renderScale: 0.645f),
            [PetFramePreset.Airborne] = Preset(renderScale: 0.78f),
            [PetFramePreset.Leaning] = Preset(renderScale: 0.92f),
            [PetFramePreset.Staggering] = Preset(renderScale: 0.84f),
            [PetFramePreset.Fallen] = Preset(renderScale: 0.80f),
            [PetFramePreset.Action] = Preset(),
            [PetFramePreset.Looking] = Preset(),
            [PetFramePreset.LookingUp] = Preset(),
            [PetFramePreset.LookingDown] = Preset()
        };

    // Индексы: строка, затем кадр.
    //
    // BodyAnchorX остаётся индивидуальным, потому что это компенсация положения
    // тела внутри конкретного рисунка. Scale/offset наследуются из визуальной
    // позы; override нужен только для реально выбивающегося кадра.
    private static readonly PetFrameGeometry[][] FramesByRow =
    {
        // Row 0: idle / blink -> одна и та же стоящая фигура.
        new[]
        {
            Frame(53, PetFramePreset.Standing), Frame(53, PetFramePreset.Standing),
            Frame(53, PetFramePreset.Standing), Frame(53, PetFramePreset.Standing),
            Frame(53, PetFramePreset.Standing), Frame(53, PetFramePreset.Standing),
            Frame(53, PetFramePreset.Standing)
        },

        // Row 1: run right. Геометрический размер стабилен; различия ширины
        // остаются естественной частью самой позы внутри нормализованного кадра.
        new[]
        {
            Frame(78, PetFramePreset.Running), Frame(82, PetFramePreset.Running),
            Frame(79, PetFramePreset.Running), Frame(77, PetFramePreset.Running),
            Frame(80, PetFramePreset.Running), Frame(80, PetFramePreset.Running),
            Frame(80, PetFramePreset.Running), Frame(75, PetFramePreset.Running)
        },

        // Row 2: run left -> тот же preset, без отдельного растяжения по X.
        new[]
        {
            Frame(72, PetFramePreset.Running), Frame(67, PetFramePreset.Running),
            Frame(63, PetFramePreset.Running), Frame(69, PetFramePreset.Running),
            Frame(65, PetFramePreset.Running), Frame(64, PetFramePreset.Running),
            Frame(68, PetFramePreset.Running), Frame(69, PetFramePreset.Running)
        },

        // Row 3: wave визуально остаётся standing-позой.
        new[]
        {
            Frame(53, PetFramePreset.Standing), Frame(54, PetFramePreset.Standing),
            Frame(62, PetFramePreset.Standing), Frame(51, PetFramePreset.Standing)
        },

        // Row 4: jump -> компактная подготовка/приземление и полноразмерная
        // воздушная фаза. Программный JumpLift от этих scale не зависит.
        new[]
        {
            Frame(53, PetFramePreset.Crouched),
            Frame(58, PetFramePreset.Airborne),
            Frame(60, PetFramePreset.Airborne),
            Frame(57, PetFramePreset.Airborne),
            Frame(57, PetFramePreset.Crouched)
        },

        // Row 5: failed jump / fall классифицируется по визуальной позе,
        // а не одним общим Failed preset'ом.
        new[]
        {
            Frame(47, PetFramePreset.Standing),
            Frame(49, PetFramePreset.Leaning),
            Frame(46, PetFramePreset.Staggering),
            Frame(65, PetFramePreset.Fallen),
            Frame(63, PetFramePreset.Fallen, renderScale: 0.73f),
            Frame(52, PetFramePreset.Staggering),
            Frame(53, PetFramePreset.Standing),
            Frame(55, PetFramePreset.Standing)
        },

        // Строки 6-8 пока не калибруются этой задачей.
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

        // Look-направления оставлены отдельными visual preset'ами для следующей
        // калибровки: вверх можно сделать выше, вниз — компактнее.
        new[]
        {
            Frame(54, PetFramePreset.LookingUp),
            Frame(68, PetFramePreset.Looking), Frame(66, PetFramePreset.Looking),
            Frame(65, PetFramePreset.Looking), Frame(65, PetFramePreset.Looking),
            Frame(66, PetFramePreset.Looking), Frame(70, PetFramePreset.Looking),
            Frame(70, PetFramePreset.Looking)
        },
        new[]
        {
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

    internal static int GetRenderWidth(float scale) =>
        Math.Max(1, (int)Math.Round(AtlasCellWidth * scale));

    internal static int GetRenderHeight(float scale) =>
        Math.Max(1, (int)Math.Round(AtlasCellHeight * scale));

    private static PetFramePresetGeometry Preset(
        float renderScale = 1f,
        int renderOffsetX = 0,
        int? renderOffsetY = null,
        int headAnchorY = DefaultHeadAnchorY)
    {
        ValidateScale(renderScale);
        return new(
            renderScale,
            renderOffsetX,
            renderOffsetY ?? DefaultRenderBottomY - GetRenderHeight(renderScale),
            headAnchorY);
    }

    private static PetFrameGeometry Frame(
        int bodyAnchorX,
        PetFramePreset preset,
        float? renderScale = null,
        int? renderOffsetX = null,
        int? renderOffsetY = null,
        int? headAnchorY = null)
    {
        PetFramePresetGeometry inherited = Presets[preset];

        float scale = renderScale ?? inherited.RenderScale;
        int offsetX = renderOffsetX ?? inherited.RenderOffsetX;
        int offsetY = renderOffsetY
            ?? (renderScale.HasValue
                ? inherited.RenderOffsetY
                    + GetRenderHeight(inherited.RenderScale)
                    - GetRenderHeight(scale)
                : inherited.RenderOffsetY);
        int headY = headAnchorY ?? inherited.HeadAnchorY;

        ValidateFrameGeometry(bodyAnchorX, scale, headY);
        return new(bodyAnchorX, scale, offsetX, offsetY, headY);
    }

    private static void ValidateFrameGeometry(int bodyAnchorX, float renderScale, int headAnchorY)
    {
        if (bodyAnchorX < 0 || bodyAnchorX >= AtlasCellWidth)
            throw new ArgumentOutOfRangeException(nameof(bodyAnchorX));
        if (headAnchorY < 0 || headAnchorY >= AtlasCellHeight)
            throw new ArgumentOutOfRangeException(nameof(headAnchorY));
        ValidateScale(renderScale);
    }

    private static void ValidateScale(float renderScale)
    {
        if (float.IsNaN(renderScale) || float.IsInfinity(renderScale) || renderScale <= 0f)
            throw new ArgumentOutOfRangeException(nameof(renderScale));
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
