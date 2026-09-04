using System.Diagnostics;
using System.Drawing;

namespace Launcher.Pet.Animation;

internal readonly record struct PetJumpFrame(int Row, int Frame, float Lift);

internal static class PetAnimationCatalog
{
    // Логическая зона питомца, а не размер ячейки PNG. Сохраняет прежние границы
    // движения, центр слежения, компоновку и программную высоту прыжков.
    internal const int FrameWidth = 192;
    internal const int FrameHeight = 208;

    // Новая сетка PNG: 8 столбцов x 11 строк, ячейка 149x200, без зазоров.
    internal const int AtlasColumns = 8;
    internal const int AtlasRows = 11;
    internal const int CellWidth = 149;
    internal const int CellHeight = 200;
    internal const int AtlasTopOffset = 3;
    internal const int HeadHitHeight = 90;

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

    internal const int WaveIntervalMs = 9000;
    internal const int WaveLoopCount = 3;
    internal const int MovementMinDelayMs = 10000;
    internal const int MovementMaxDelayMs = 20000;
    internal const int JumpMinDelayMs = 10000;
    internal const int JumpMaxDelayMs = 17000;
    internal const int CursorPollIntervalMs = 50;
    internal const int LookDeadzoneRadius = 12;
    internal const int EdgePadding = 16;
    internal const int CardProbeOffset = 2;
    internal const float PixelsPerMovementCycle = 120f;

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

    internal static Rectangle GetSourceRectangle(int row, int frame) =>
        new(frame * CellWidth, row * CellHeight, CellWidth, CellHeight);

    internal static Point GetFrameOffset(int row, int frame) =>
        new(FrameWidth / 2 - BodyAnchorXByRow[row][frame], AtlasTopOffset);

    internal static int GetLookIndex(float deltaX, float deltaY)
    {
        double degrees = Math.Atan2(deltaX, -deltaY) * 180d / Math.PI;
        if (degrees < 0)
            degrees += 360d;
        return (int)Math.Round(degrees / 22.5d, MidpointRounding.AwayFromZero) % 16;
    }

    [Conditional("DEBUG")]
    internal static void Validate(Bitmap atlas)
    {
        Debug.Assert(BodyAnchorXByRow.Length == AtlasRows);
        Debug.Assert(BodyAnchorXByRow.Sum(row => row.Length) == 74);
        Debug.Assert(FrameDurationsByRow.Length == LookFirstRow);
        Debug.Assert(FrameDurationsByRow[IdleRow].Sum() == 6600);
        Debug.Assert(!IsInsideCardSpan(99, 100, 400));
        Debug.Assert(IsInsideCardSpan(100, 100, 400));
        Debug.Assert(IsInsideCardSpan(250, 100, 400));
        Debug.Assert(!IsInsideCardSpan(401, 100, 400));
        Debug.Assert(GetLookIndex(0, -1) == 0);
        Debug.Assert(GetLookIndex(1, 0) == 4);
        Debug.Assert(GetLookIndex(0, 1) == 8);
        Debug.Assert(GetLookIndex(-1, 0) == 12);
        Debug.Assert(GetLookIndex(1, -1) == 2);
        Debug.Assert(GetLookIndex(-1, -2.4142135f) == 15);
        Debug.Assert(GetLookIndex(-0.1f, -1) == 0);

        var atlasBounds = new Rectangle(Point.Empty, atlas.Size);
        var logicalBounds = new Rectangle(0, 0, FrameWidth, FrameHeight);
        for (int row = 0; row < AtlasRows; row++)
        {
            int frameCount = BodyAnchorXByRow[row].Length;
            Debug.Assert(frameCount > 0 && frameCount <= AtlasColumns);
            Debug.Assert(frameCount == (row < LookFirstRow ? FrameDurationsByRow[row].Length : AtlasColumns));

            for (int frame = 0; frame < AtlasColumns; frame++)
            {
                Rectangle source = GetSourceRectangle(row, frame);
                Debug.Assert(atlasBounds.Contains(source));
                int left = CellWidth, top = CellHeight, right = -1, bottom = -1;
                for (int y = 0; y < CellHeight; y++)
                for (int x = 0; x < CellWidth; x++)
                {
                    if (atlas.GetPixel(source.X + x, source.Y + y).A == 0)
                        continue;
                    left = Math.Min(left, x);
                    top = Math.Min(top, y);
                    right = Math.Max(right, x);
                    bottom = Math.Max(bottom, y);
                }

                bool populated = right >= left;
                Debug.Assert(populated == (frame < frameCount), $"Sumrak: row={row}, frame={frame}");
                if (!populated || frame >= frameCount)
                    continue;

                int anchor = BodyAnchorXByRow[row][frame];
                Debug.Assert(anchor >= left && anchor <= right);
                Point offset = GetFrameOffset(row, frame);
                var visibleBounds = Rectangle.FromLTRB(
                    offset.X + left, offset.Y + top, offset.X + right + 1, offset.Y + bottom + 1);
                Debug.Assert(logicalBounds.Contains(visibleBounds), $"Sumrak bounds: row={row}, frame={frame}");
            }
        }
    }

    internal static bool IsInsideCardSpan(float petX, int left, int right) => petX >= left && petX <= right;
}
