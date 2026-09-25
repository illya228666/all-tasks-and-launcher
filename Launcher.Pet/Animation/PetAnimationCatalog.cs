namespace Launcher.Pet.Animation;
public static class PetAnimationCatalog
{
    internal const int IdleRow = 0;
    internal const int ClimbRow = 11;
    internal const int DragRow = 12;
    internal const int MoveRightRow = 1;
    internal const int MoveLeftRow = 2;
    internal const int WaveRow = 3;
    internal const int JumpRow = 4;
    internal const int FailedRow = 5;
    internal const int LookFirstRow = 9;
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
}
