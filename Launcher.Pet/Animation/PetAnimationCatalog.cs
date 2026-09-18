using System.Drawing;

namespace Launcher.Pet.Animation;
public static class PetAnimationCatalog
{
    // Логическая зона питомца, а не размер ячейки PNG. Сохраняет прежние границы
    // движения, центр слежения, компоновку и программную высоту прыжков.
    public const int FrameWidth = 192;
    public const int FrameHeight = 208;
    // Новая сетка PNG: 8 столбцов x 11 строк, ячейка 149x200, без зазоров.
    public const int AtlasColumns = 8;
    public const int AtlasRows = 11;
    public const int CellWidth = 149;
    public const int CellHeight = 200;
    internal const int AtlasTopOffset = 3;
    public const int HeadHitHeight = 90;
    // X середины таза относительно левого края каждой ячейки. Первый индекс —
    // строка, второй — кадр. Увеличение числа сдвигает рисунок влево.
    internal static readonly int[][] BodyAnchorXByRow =
    {
        new[] { 53, 53, 53, 53, 53, 53, 53 },
        new[] { 78, 82, 79, 77, 80, 80, 80, 75 },
        new[] { 72, 67, 63, 69, 65, 64, 68, 69 },
        new[] { 53, 54, 62, 51 },
        new[] { 53, 58, 60, 57, 57 },
        new[] { 47, 49, 46, 65, 63, 52, 53, 55 },
        new[] { 45, 49, 50, 50, 50, 50 },
        new[] { 52, 48, 54, 52, 52, 56 },
        new[] { 56, 55, 55, 56, 55, 56 },
        new[] { 54, 68, 66, 65, 65, 66, 70, 70 },
        new[] { 46, 50, 44, 44, 43, 41, 40, 37 }
    };
    internal const int IdleRow = 0;
    internal const int MoveRightRow = 1;
    internal const int MoveLeftRow = 2;
    internal const int WaveRow = 3;
    internal const int JumpRow = 4;
    internal const int FailedRow = 5;
    internal const int LookFirstRow = 9;
    internal const int EdgePadding = 16;
    // Землетрясение: время от начала в мс, существующие кадры потери равновесия и подъёма.
    internal static readonly (int UntilMs, int Row, int Frame)[] EarthquakeFrames =
    {
        (180, FailedRow, 0),
        (360, FailedRow, 1),
        (560, FailedRow, 2),
        (800, FailedRow, 3),
        (3400, FailedRow, 4),
        (3900, FailedRow, 3),
        (4300, FailedRow, 5),
        (4650, FailedRow, 6),
        (Launcher.Pet.Behavior.PetEarthquake.DurationMs, FailedRow, 7)
    };
    // Прототип: быстрый подход, приседание, выпрямление и жест рукой у шляпы.
    internal static readonly PetHatPickupFrame[] HatPickupFrames =
    {
        new(JumpRow, 0, 180),
        new(FailedRow, 5, 160),
        new(FailedRow, 6, 160),
        new(WaveRow, 0, 140, PutOn: true),
        new(WaveRow, 1, 160),
        new(IdleRow, 0, 180)
    };
    // Длительности кадров в миллисекундах. Строки 9-10 отсутствуют: look-позу
    // выбирает курсор. Количество значений должно совпадать с числом кадров строки.
    internal static readonly int[][] FrameDurationsByRow =
    {
        new[] { 1680, 660, 660, 840, 840, 960, 960 },
        new[] { 120, 120, 120, 120, 120, 120, 120, 220 },
        new[] { 120, 120, 120, 120, 120, 120, 120, 220 },
        new[] { 140, 140, 140, 280 },
        new[] { 140, 140, 140, 140, 280 },
        new[] { 140, 140, 140, 140, 440, 140, 240, 440 },
        new[] { 150, 150, 150, 150, 150, 260 },
        new[] { 120, 120, 120, 120, 120, 220 },
        new[] { 150, 150, 150, 150, 150, 280 }
    };
    // Lift задаёт нормализованный подъём; фактическая высота вычисляется controller.
    internal static readonly PetJumpFrame[] SuccessfulJumpFrames =
    {
        new(JumpRow, 0, 0f),
        new(JumpRow, 1, 0.5f),
        new(JumpRow, 2, 1f),
        new(JumpRow, 3, 0.5f),
        new(JumpRow, 4, 0f)
    };
    internal static readonly PetJumpFrame[] FailedJumpFrames =
    {
        new(JumpRow, 0, 0f),
        new(JumpRow, 1, 1f),
        new(JumpRow, 3, 0.5f),
        new(FailedRow, 4, 0f),
        new(FailedRow, 3, 0f),
        new(FailedRow, 5, 0f),
        new(FailedRow, 6, 0f),
        new(FailedRow, 7, 0f)
    };
    public static Rectangle GetSourceRectangle(int row, int frame) => new(frame * CellWidth, row * CellHeight, CellWidth, CellHeight);
    internal static Point GetFrameOffset(int row, int frame) => new(FrameWidth / 2 - BodyAnchorXByRow[row][frame], AtlasTopOffset);
}
