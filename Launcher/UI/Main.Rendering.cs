using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Launcher.Domain;
using Launcher.UI.Controls;

namespace Launcher.UI;

public partial class Main
{
    // Логическая зона питомца, а не размер ячейки PNG. Сохраняет прежние границы
    // движения, центр слежения, компоновку и программную высоту прыжков.
    private const int PetFrameWidth = 192;
    private const int PetFrameHeight = 208;

    // Новая сетка PNG: 8 столбцов x 11 строк, ячейка 149x200, без зазоров.
    // Размер нарезки и отрисовки одинаковый: спрайты показываются в масштабе 1:1.
    private const int PetAtlasColumns = 8;
    private const int PetAtlasRows = 11;
    private const int PetCellWidth = 149;
    private const int PetCellHeight = 200;

    // Общий отступ ячейки сверху логической зоны. Нижняя граница обычных поз
    // оказывается на y=203; внутренний подъём в кадрах прыжка не выравниваем.
    private const int PetAtlasTopOffset = 3;

    // Захват шляпы: верхние 90 пикселей видимой фигуры, только непрозрачные точки.
    // Увеличение высоты расширяет область захвата вниз, в сторону шеи и корпуса.
    private const int PetHeadHitHeight = 90;

    // X середины таза относительно левого края каждой новой ячейки, в пикселях.
    // Все точки совмещаются с x=96 логической зоны; плащ, оружие и руки не учитываются.
    // Первый индекс — строка, второй — кадр с нуля. Здесь только 74 заполненных кадра.
    // Увеличение числа сдвигает рисунок влево, уменьшение — вправо. Вертикальные
    // смещения поз остаются такими, как в PNG; это не центры непрозрачных границ.
    private static readonly int[][] PetBodyAnchorXByRow =
    {
        new[] { 53, 53, 53, 53, 53, 53, 53 },         // 0: idle, все 7 кадров.
        new[] { 78, 82, 79, 77, 80, 80, 80, 75 },     // 1: бег вправо.
        new[] { 72, 67, 63, 69, 65, 64, 68, 69 },     // 2: бег влево.
        new[] { 53, 54, 62, 51 },                     // 3: waving.
        new[] { 53, 58, 60, 57, 57 },                 // 4: прыжок.
        new[] { 47, 49, 46, 65, 63, 52, 53, 55 },     // 5: неудача.
        new[] { 45, 49, 50, 50, 50, 50 },             // 6: waiting.
        new[] { 52, 48, 54, 52, 52, 56 },             // 7: running.
        new[] { 56, 55, 55, 56, 55, 56 },             // 8: review.
        new[] { 54, 68, 66, 65, 65, 66, 70, 70 },     // 9: направления 0-7.
        new[] { 46, 50, 44, 44, 43, 41, 40, 37 }      // 10: направления 8-15.
    };

    // Номера строк атласа считаются с нуля: первая видимая строка имеет индекс 0.
    // Эти константы только связывают имя анимации с нужной строкой спрайт-листа.
    private const int PetIdleRow = 0;
    private const int PetMoveRightRow = 1;
    private const int PetMoveLeftRow = 2;
    private const int PetWaveRow = 3;
    private const int PetJumpRow = 4;
    private const int PetFailedRow = 5;

    // Строки 9 и 10 содержат 16 статичных поз слежения за курсором:
    // направления 0-7 берутся из строки 9, направления 8-15 — из строки 10.
    private const int PetLookFirstRow = 9;

    // Примерно через столько миллисекунд накопленного idle запускается waving.
    // Время других анимаций и режим слежения за курсором сюда не засчитываются.
    private const int PetWaveIntervalMs = 9000;

    // Сколько раз подряд полностью проигрывается строка waving перед возвратом в idle.
    private const int PetAppStateLoopCount = 3;

    // Случайная пауза перед следующим бегом, в миллисекундах. Обе границы включены.
    // Новый интервал назначается после завершения бега и после выхода курсора из окна.
    private const int PetMovementMinDelayMs = 10000;
    private const int PetMovementMaxDelayMs = 20000;

    // Независимая случайная пауза перед попыткой прыжка, также с включёнными границами.
    // Если в этот момент идёт бег, попытка пропускается и назначается новая пауза.
    private const int PetJumpMinDelayMs = 10000;
    private const int PetJumpMaxDelayMs = 17000;

    // Как часто проверяется положение курсора. Меньшее значение делает реакцию быстрее,
    // но чаще будит UI-поток. 50 мс соответствует примерно 20 проверкам в секунду.
    private const int PetCursorPollIntervalMs = 50;

    // Радиус мёртвой зоны вокруг центра Сумрака, в пикселях. Внутри неё сохраняется
    // последнее осмысленное направление головы, чтобы поза не дрожала возле центра.
    private const int PetLookDeadzoneRadius = 12;

    // Минимальный отступ логической зоны Сумрака от краёв его рабочей области.
    private const int PetEdgePadding = 16;

    // Точка проверки карточек находится на столько пикселей правее левого края Сумрака.
    // Увеличение значения сдвигает точку, по которой выбирается успешный/неудачный прыжок.
    private const int PetCardProbeOffset = 2;

    // Сколько горизонтальных пикселей Сумрак проходит за один полный цикл кадров бега.
    // Код делит расстояние на это число и округляет количество полных циклов:
    // большее значение = меньше циклов и более быстрый бег, меньшее = более медленный.
    private const float PetPixelsPerMovementCycle = 120f;

    // Основа длительностей — openai/codex, файл
    // codex-rs/tui/src/pets/model.rs, метод default_animations().
    // Для idle последняя пауза 1920 мс разделена между кадрами 5 и 6:
    // используются все 7 рисунков, а полный цикл по-прежнему занимает 6600 мс.
    //
    // Первый индекс массива — строка атласа (0-8), второй — номер кадра в этой строке.
    // Каждое число задаёт время показа конкретного кадра в миллисекундах. Например,
    // PetFrameDurationsByRow[3][0] = 140 означает: кадр 0 строки waving виден 140 мс.
    // Меняя отдельное число, можно ускорить или задержать именно этот кадр. Количество
    // чисел в строке должно оставаться равным количеству проигрываемых кадров.
    // Увеличенный последний интервал создаёт естественную паузу перед новым циклом.
    // Строки 9-10 здесь отсутствуют: позу слежения выбирает курсор, а не кадровый таймер.
    private static readonly int[][] PetFrameDurationsByRow =
    {
        new[] { 1680, 660, 660, 840, 840, 960, 960 },     // Строка 0: idle.
        new[] { 120, 120, 120, 120, 120, 120, 120, 220 }, // Строка 1: бег вправо.
        new[] { 120, 120, 120, 120, 120, 120, 120, 220 }, // Строка 2: бег влево.
        new[] { 140, 140, 140, 280 },                     // Строка 3: waving.
        new[] { 140, 140, 140, 140, 280 },                // Строка 4: успешный прыжок.
        new[] { 140, 140, 140, 140, 440, 140, 240, 440 }, // Строка 5: неудача.
        new[] { 150, 150, 150, 150, 150, 260 },           // Строка 6: waiting, пока не используется.
        new[] { 120, 120, 120, 120, 120, 220 },           // Строка 7: running, пока не используется.
        new[] { 150, 150, 150, 150, 150, 280 }            // Строка 8: review, пока не используется.
    };

    // Последовательность успешного прыжка. Каждый элемент — (строка, кадр, подъём).
    // Row и Frame считаются с нуля. Lift — нормализованная высота от 0 (земля) до 1
    // (пиковая точка); фактическое смещение равно Lift * _petJumpPeak.
    // Порядок элементов определяет порядок проигрывания, а длительность каждого элемента
    // берётся из PetFrameDurationsByRow[Row][Frame]. Пик успешного прыжка задаётся ниже
    // как PetFrameHeight / 3f: уменьшение делителя поднимает Сумрака выше.
    private static readonly (int Row, int Frame, float Lift)[] PetSuccessfulJumpFrames =
    {
        (PetJumpRow, 0, 0f),
        (PetJumpRow, 1, 0.5f),
        (PetJumpRow, 2, 1f),
        (PetJumpRow, 3, 0.5f),
        (PetJumpRow, 4, 0f)
    };

    // Неудачный прыжок устроен так же, но после трёх кадров прыжка переходит на кадры
    // строки failed. Его пик задаётся как PetFrameHeight / 4f. Значение Lift = 0 у
    // последних кадров оставляет Сумрака на земле, пока доигрывается реакция на неудачу.
    private static readonly (int Row, int Frame, float Lift)[] PetFailedJumpFrames =
    {
        (PetJumpRow, 0, 0f),
        (PetJumpRow, 1, 1f),
        (PetJumpRow, 3, 0.5f),
        (PetFailedRow, 4, 0f),
        (PetFailedRow, 3, 0f),
        (PetFailedRow, 5, 0f),
        (PetFailedRow, 6, 0f),
        (PetFailedRow, 7, 0f)
    };

    private Bitmap? _petAtlas;
    private Bitmap? _petAtlasWithoutHat;
    private Bitmap? _petHatSprite;
    private HatWindow? _petHatWindow;
    private bool _petHatRemoved;
    private Panel _petPanel = null!;
    private System.Windows.Forms.Timer _petTimer = null!;
    private System.Windows.Forms.Timer _petMovementTimer = null!;
    private System.Windows.Forms.Timer _petJumpTimer = null!;
    private System.Windows.Forms.Timer _petCursorTimer = null!;
    private int _petRow = PetIdleRow;
    private int _petFrame;
    private int _petIdleElapsedMs;
    private int _petWaveLoopsRemaining;
    private int _petGroundY;
    private float _petX = float.NaN;
    private float _petMoveStartX;
    private float _petMoveTargetX;
    private int _petMoveElapsedMs;
    private int _petMoveDurationMs;
    private (int Row, int Frame, float Lift)[]? _petJumpSequence;
    private int _petJumpIndex;
    private float _petJumpPeak;
    // Во время речи запоминаем не больше одного прыжка и одного бега.
    private bool _petJumpPending;
    private bool _petMovementPending;
    private bool _petTrackingCursor;
    private int _petLookIndex;

    #region [RU] Бизнес-логика | [DE] Fachlogik

    /// <summary>
    /// RU: Перерисовывает центральную область со списком приложений.
    /// DE: Rendert den zentralen Bereich mit der Anwendungsliste neu.
    /// </summary>
    private void Render(bool preservePetSpeech = false)
    {
        if (!IsHandleCreated)
        {
            return;
        }

        if (!preservePetSpeech)
            ResetPetSpeech();
        List<AppEntry> visibleApps = BuildVisibleApps();
        Point scrollPosition = flpApps.AutoScrollPosition;
        _petHostChanging = true;

        flpApps.SuspendLayout();
        try
        {
            flpApps.Controls.Clear();
            if (visibleApps.Count == 0)
            {
                flpApps.Controls.Add(CreateEmptyState());
                flpApps.Controls.Add(CreatePetOnlyPanel());
            }
            else
            {
                bool groupedByCategory = GetSelectedSortMode() == SortMode.ByCategory
                    && string.Equals(_cbCategory.SelectedItem as string, LauncherConstants.AllCategories, System.StringComparison.Ordinal);

                if (groupedByCategory)
                {
                    var groups = visibleApps
                        .GroupBy(app => app.Category)
                        .OrderBy(group => group.Key)
                        .Select(group => (group.Key, Apps: group.ToList()))
                        .ToList();

                    for (int index = 0; index < groups.Count; index++)
                    {
                        flpApps.Controls.Add(CreateSection(
                            groups[index].Key,
                            groups[index].Apps,
                            includePetZone: index == groups.Count - 1));
                    }
                }
                else
                {
                    flpApps.Controls.Add(CreateSection($"Ergebnisse ({visibleApps.Count})", visibleApps, includePetZone: true));
                }
            }
        }
        finally
        {
            flpApps.ResumeLayout();
            if (preservePetSpeech)
                flpApps.AutoScrollPosition = new Point(-scrollPosition.X, -scrollPosition.Y);
            _petHostChanging = false;
        }
        UpdateStats(visibleApps);
        UpdatePetSpeechPlacement();
    }

    #endregion

    #region [RU] Вспомогательные методы | [DE] Hilfsmethoden

    private void InitializePet()
    {
        try
        {
            _petAtlas = LoadPetAtlas("spritesheet_sumrak_hat.png");
            _petAtlasWithoutHat = LoadPetAtlas("spritesheet_sumrak_no_hat.png");
            using var hat = new Bitmap(Path.Combine(AppContext.BaseDirectory, "Resources", "hat_small.png"));
            if (hat.Width != 109 || hat.Height != 64)
                throw new InvalidDataException($"Unerwartete Hutgroesse: {hat.Width}x{hat.Height}.");
            _petHatSprite = new Bitmap(hat);
        }
        catch
        {
            DisposePet();
            throw;
        }

        ValidatePetAtlas(_petAtlas);
        _petPanel = new Panel
        {
            Height = PetFrameHeight,
            Margin = new Padding(4, 0, 4, 0),
            BackColor = Color.Transparent
        };
        _petPanel.Paint += PetPanel_Paint;
        _petPanel.MouseDown += PetPanel_MouseDown;
        EnableDoubleBuffer(_petPanel);

        _petTimer = new System.Windows.Forms.Timer(components)
        {
            Interval = PetFrameDurationsByRow[PetIdleRow][0]
        };
        _petTimer.Tick += PetTimer_Tick;

        _petMovementTimer = new System.Windows.Forms.Timer(components);
        _petMovementTimer.Tick += PetMovementTimer_Tick;

        _petJumpTimer = new System.Windows.Forms.Timer(components);
        _petJumpTimer.Tick += PetJumpTimer_Tick;

        _petCursorTimer = new System.Windows.Forms.Timer(components)
        {
            Interval = PetCursorPollIntervalMs
        };
        _petCursorTimer.Tick += PetCursorTimer_Tick;
        InitializePetSpeech();

        System.Diagnostics.Debug.Assert(!IsPetInsideCardSpan(99, 100, 400));
        System.Diagnostics.Debug.Assert(IsPetInsideCardSpan(100, 100, 400));
        System.Diagnostics.Debug.Assert(IsPetInsideCardSpan(250, 100, 400));
        System.Diagnostics.Debug.Assert(!IsPetInsideCardSpan(401, 100, 400));
        System.Diagnostics.Debug.Assert(GetPetLookIndex(0, -1) == 0);
        System.Diagnostics.Debug.Assert(GetPetLookIndex(1, 0) == 4);
        System.Diagnostics.Debug.Assert(GetPetLookIndex(0, 1) == 8);
        System.Diagnostics.Debug.Assert(GetPetLookIndex(-1, 0) == 12);
        System.Diagnostics.Debug.Assert(GetPetLookIndex(1, -1) == 2);
        System.Diagnostics.Debug.Assert(GetPetLookIndex(-1, -2.4142135f) == 15);
        System.Diagnostics.Debug.Assert(GetPetLookIndex(-0.1f, -1) == 0);
    }

    private static Bitmap LoadPetAtlas(string fileName)
    {
        using var source = new Bitmap(Path.Combine(AppContext.BaseDirectory, "Resources", fileName));
        if (source.Width != PetAtlasColumns * PetCellWidth || source.Height != PetAtlasRows * PetCellHeight)
            throw new InvalidDataException($"Unerwartete Sumrak-Atlasgroesse ({fileName}): {source.Width}x{source.Height}.");
        return new Bitmap(source);
    }

    private void DisposePet()
    {
        _petSpeaking = false;
        _petSpeechTimer?.Dispose();
        _petSpeechWindow?.Dispose();
        _petSpeechWindow = null;
        _petHatWindow?.Dispose();
        _petHatSprite?.Dispose();
        _petAtlasWithoutHat?.Dispose();
        _petAtlas?.Dispose();
    }

    private static Rectangle GetPetSourceRectangle(int row, int frame) =>
        new(frame * PetCellWidth, row * PetCellHeight, PetCellWidth, PetCellHeight);

    private static Point GetPetFrameOffset(int row, int frame) =>
        new(PetFrameWidth / 2 - PetBodyAnchorXByRow[row][frame], PetAtlasTopOffset);

    // Проверка нового атласа при Debug-запуске; в Release обход пикселей не выполняется.
    // Проверяем также пустые ячейки, чтобы новый рисунок нельзя было незаметно пропустить.
    [Conditional("DEBUG")]
    private static void ValidatePetAtlas(Bitmap atlas)
    {
        Debug.Assert(PetBodyAnchorXByRow.Length == PetAtlasRows);
        Debug.Assert(PetBodyAnchorXByRow.Sum(row => row.Length) == 74);
        Debug.Assert(PetFrameDurationsByRow.Length == PetLookFirstRow);
        Debug.Assert(PetFrameDurationsByRow[PetIdleRow].Sum() == 6600);
        var atlasBounds = new Rectangle(Point.Empty, atlas.Size);
        var logicalBounds = new Rectangle(0, 0, PetFrameWidth, PetFrameHeight);

        for (int row = 0; row < PetAtlasRows; row++)
        {
            int frameCount = PetBodyAnchorXByRow[row].Length;
            Debug.Assert(frameCount > 0 && frameCount <= PetAtlasColumns);
            Debug.Assert(frameCount == (row < PetLookFirstRow
                ? PetFrameDurationsByRow[row].Length : PetAtlasColumns));

            for (int frame = 0; frame < PetAtlasColumns; frame++)
            {
                Rectangle source = GetPetSourceRectangle(row, frame);
                Debug.Assert(atlasBounds.Contains(source));
                int left = PetCellWidth, top = PetCellHeight, right = -1, bottom = -1;
                for (int y = 0; y < PetCellHeight; y++)
                {
                    for (int x = 0; x < PetCellWidth; x++)
                    {
                        if (atlas.GetPixel(source.X + x, source.Y + y).A == 0)
                            continue;
                        left = System.Math.Min(left, x);
                        top = System.Math.Min(top, y);
                        right = System.Math.Max(right, x);
                        bottom = System.Math.Max(bottom, y);
                    }
                }

                bool populated = right >= left;
                Debug.Assert(populated == (frame < frameCount), $"Sumrak: row={row}, frame={frame}");
                if (!populated || frame >= frameCount)
                    continue;

                int anchor = PetBodyAnchorXByRow[row][frame];
                Debug.Assert(anchor >= left && anchor <= right);
                Point offset = GetPetFrameOffset(row, frame);
                var visibleBounds = Rectangle.FromLTRB(
                    offset.X + left, offset.Y + top, offset.X + right + 1, offset.Y + bottom + 1);
                Debug.Assert(logicalBounds.Contains(visibleBounds), $"Sumrak bounds: row={row}, frame={frame}");
            }
        }
    }

    private void PetTimer_Tick(object? sender, System.EventArgs e)
    {
        if (_petTrackingCursor)
        {
            return;
        }

        if (_petJumpSequence is not null)
        {
            AdvancePetJump();
            return;
        }

        int[] durations = PetFrameDurationsByRow[_petRow];
        bool isMoving = _petRow is PetMoveRightRow or PetMoveLeftRow;

        if (isMoving)
        {
            _petMoveElapsedMs += durations[_petFrame];
            float progress = System.Math.Min(1f, (float)_petMoveElapsedMs / _petMoveDurationMs);
            _petX = _petMoveStartX + ((_petMoveTargetX - _petMoveStartX) * progress);
        }
        else if (_petRow == PetIdleRow)
        {
            _petIdleElapsedMs += durations[_petFrame];
        }

        _petFrame++;

        if (_petFrame >= durations.Length)
        {
            _petFrame = 0;

            if (_petRow == PetWaveRow && --_petWaveLoopsRemaining == 0)
            {
                _petRow = PetIdleRow;
            }
        }

        if (isMoving && _petMoveElapsedMs >= _petMoveDurationMs)
        {
            _petX = _petMoveTargetX;
            ClampPetPosition();
            _petRow = PetIdleRow;
            _petFrame = 0;
            _petMoveElapsedMs = 0;
            ScheduleNextPetMovement();
        }

        if (TryStartPendingPetAction())
            return;

        if (_petRow == PetIdleRow && _petIdleElapsedMs >= PetWaveIntervalMs)
        {
            _petRow = PetWaveRow;
            _petFrame = 0;
            _petIdleElapsedMs = 0;
            _petWaveLoopsRemaining = PetAppStateLoopCount;
        }

        durations = PetFrameDurationsByRow[_petRow];
        _petTimer.Interval = durations[_petFrame];
        _petPanel.Invalidate();
    }

    private void PetJumpTimer_Tick(object? sender, System.EventArgs e)
    {
        _petJumpTimer.Stop();

        if (_petTrackingCursor)
        {
            return;
        }

        if (_petSpeaking)
        {
            _petJumpPending = true;
            return;
        }

        if (_petRow is PetMoveRightRow or PetMoveLeftRow)
        {
            ScheduleNextPetJump();
            return;
        }

        if (_petRow != PetIdleRow)
        {
            _petJumpPending = true;
            return;
        }

        StartPetJump();
    }

    private void StartPetJump()
    {
        bool failed = IsPetBelowBottomCardRow();
        _petJumpTimer.Stop();
        _petJumpPending = false;
        _petJumpSequence = failed ? PetFailedJumpFrames : PetSuccessfulJumpFrames;
        _petJumpIndex = 0;
        _petJumpPeak = PetFrameHeight / (failed ? 4f : 3f);
        _petIdleElapsedMs = 0;
        ApplyCurrentPetJumpFrame();
    }

    private void AdvancePetJump()
    {
        _petJumpIndex++;

        if (_petJumpSequence is null || _petJumpIndex >= _petJumpSequence.Length)
        {
            _petJumpSequence = null;
            _petJumpIndex = 0;
            _petRow = PetIdleRow;
            _petFrame = 0;
            _petIdleElapsedMs = 0;
            _petTimer.Interval = PetFrameDurationsByRow[PetIdleRow][0];
            ScheduleNextPetJump();
            TryStartPendingPetAction();
            _petPanel.Invalidate();
            return;
        }

        ApplyCurrentPetJumpFrame();
    }

    private void ApplyCurrentPetJumpFrame()
    {
        (int row, int frame, _) = _petJumpSequence![_petJumpIndex];
        _petRow = row;
        _petFrame = frame;
        _petTimer.Interval = PetFrameDurationsByRow[row][frame];
        _petPanel.Invalidate();
    }

    private bool IsPetBelowBottomCardRow()
    {
        List<AppCardControl> cards = _petPanel.Controls.OfType<AppCardControl>().ToList();

        if (cards.Count == 0)
        {
            return false;
        }

        int bottomRowTop = cards.Max(card => card.Top);
        List<AppCardControl> bottomRow = cards.Where(card => card.Top == bottomRowTop).ToList();
        int left = bottomRow.Min(card => card.Left);
        int right = bottomRow.Max(card => card.Right);

        return IsPetInsideCardSpan(_petX + PetCardProbeOffset, left, right);
    }

    private static bool IsPetInsideCardSpan(float petX, int left, int right) => petX >= left && petX <= right;

    private void ScheduleNextPetJump()
    {
        _petJumpPending = false;
        _petJumpTimer.Stop();

        if (_petTrackingCursor)
        {
            return;
        }

        _petJumpTimer.Interval = _random.Next(PetJumpMinDelayMs, PetJumpMaxDelayMs + 1);
        _petJumpTimer.Start();
    }

    private void PetMovementTimer_Tick(object? sender, System.EventArgs e)
    {
        _petMovementTimer.Stop();

        if (_petTrackingCursor)
        {
            return;
        }

        if (_petSpeaking || _petJumpPending || _petMovementPending)
        {
            _petMovementPending = true;
            TryStartPendingPetAction();
            return;
        }

        if (_petRow != PetIdleRow)
        {
            ScheduleNextPetMovement();
            return;
        }

        StartPetMovement();
    }

    private bool StartPetMovement()
    {
        _petMovementPending = false;
        _petMovementTimer.Stop();
        ClampPetPosition();

        float minX = PetEdgePadding;
        float maxX = System.Math.Max(minX, _petPanel.ClientSize.Width - PetFrameWidth - PetEdgePadding);
        float minDistance = ClientSize.Width / 8f;
        float availableLeft = _petX - minX;
        float availableRight = maxX - _petX;
        bool canMoveLeft = availableLeft >= minDistance;
        bool canMoveRight = availableRight >= minDistance;

        if (!canMoveLeft && !canMoveRight)
        {
            ScheduleNextPetMovement();
            return false;
        }

        bool moveRight = canMoveRight && (!canMoveLeft || _random.Next(2) == 0);
        float available = moveRight ? availableRight : availableLeft;
        float distance = minDistance + ((float)_random.NextDouble() * (available - minDistance));

        _petMoveStartX = _petX;
        _petMoveTargetX = _petX + (moveRight ? distance : -distance);
        int movementCycleMs = PetFrameDurationsByRow[PetMoveRightRow].Sum();
        int movementCycles = System.Math.Max(1, (int)System.Math.Round(distance / PetPixelsPerMovementCycle));
        _petMoveDurationMs = movementCycles * movementCycleMs;
        _petMoveElapsedMs = 0;
        _petIdleElapsedMs = 0;
        _petRow = moveRight ? PetMoveRightRow : PetMoveLeftRow;
        _petFrame = 0;

        System.Diagnostics.Debug.Assert(distance >= minDistance && distance <= available);
        _petTimer.Interval = PetFrameDurationsByRow[_petRow][0];
        _petPanel.Invalidate();
        return true;
    }

    private void ScheduleNextPetMovement()
    {
        _petMovementTimer.Stop();

        if (_petTrackingCursor)
        {
            return;
        }

        _petMovementTimer.Interval = _random.Next(PetMovementMinDelayMs, PetMovementMaxDelayMs + 1);
        _petMovementTimer.Start();
    }

    private void PetCursorTimer_Tick(object? sender, System.EventArgs e) => UpdatePetCursorTracking();

    private void UpdatePetCursorTracking()
    {
        if (!IsHandleCreated || !_petPanel.IsHandleCreated)
        {
            return;
        }

        ClampPetPosition();
        Point cursor = Cursor.Position;
        Point petCenter = _petPanel.PointToScreen(new Point(
            (int)System.Math.Round(_petX) + (PetFrameWidth / 2),
            _petGroundY + (PetFrameHeight / 2)));
        int deltaX = cursor.X - petCenter.X;
        int deltaY = cursor.Y - petCenter.Y;
        long distanceSquared = ((long)deltaX * deltaX) + ((long)deltaY * deltaY);
        int previousLookIndex = _petLookIndex;

        if (distanceSquared > PetLookDeadzoneRadius * PetLookDeadzoneRadius)
        {
            _petLookIndex = GetPetLookIndex(deltaX, deltaY);
        }

        bool cursorInside = WindowState != FormWindowState.Minimized
            && RectangleToScreen(ClientRectangle).Contains(cursor);

        if (cursorInside && !_petTrackingCursor)
        {
            StartPetCursorTracking();
        }
        else if (!cursorInside && _petTrackingCursor)
        {
            StopPetCursorTracking();
        }
        else if (_petTrackingCursor && _petLookIndex != previousLookIndex)
        {
            ApplyPetLookFrame();
        }
    }

    private void StartPetCursorTracking()
    {
        _petTrackingCursor = true;
        ResetPetSpeech();
        _petTimer.Stop();
        _petMovementTimer.Stop();
        _petJumpTimer.Stop();
        _petJumpSequence = null;
        _petJumpIndex = 0;
        _petJumpPending = false;
        _petMovementPending = false;
        _petMoveElapsedMs = 0;
        _petIdleElapsedMs = 0;
        _petWaveLoopsRemaining = 0;
        ApplyPetLookFrame();
    }

    private void StopPetCursorTracking()
    {
        _petTrackingCursor = false;
        ResetPetSpeech();
        _petRow = PetIdleRow;
        _petFrame = 0;
        _petIdleElapsedMs = 0;
        _petTimer.Interval = PetFrameDurationsByRow[PetIdleRow][0];
        _petTimer.Start();
        ScheduleNextPetMovement();
        ScheduleNextPetJump();
        _petPanel.Invalidate();
    }

    private void ApplyPetLookFrame()
    {
        _petRow = PetLookFirstRow + (_petLookIndex / 8);
        _petFrame = _petLookIndex % 8;
        _petPanel.Invalidate();
    }

    private static int GetPetLookIndex(float deltaX, float deltaY)
    {
        double degrees = System.Math.Atan2(deltaX, -deltaY) * 180d / System.Math.PI;

        if (degrees < 0)
        {
            degrees += 360d;
        }

        return (int)System.Math.Round(degrees / 22.5d, System.MidpointRounding.AwayFromZero) % 16;
    }

    private void ClampPetPosition()
    {
        float minX = PetEdgePadding;
        float maxX = System.Math.Max(minX, _petPanel.ClientSize.Width - PetFrameWidth - PetEdgePadding);
        _petX = float.IsNaN(_petX)
            ? System.Math.Clamp((_petPanel.ClientSize.Width - PetFrameWidth) / 2f, minX, maxX)
            : System.Math.Clamp(_petX, minX, maxX);
    }

    // Общая геометрия для отрисовки и попадания мышью: включая внутренние смещения
    // кадра, положение питомца в панели и дополнительный программный подъём.
    private Rectangle GetPetDestinationRectangle()
    {
        ClampPetPosition();
        float jumpLift = _petJumpSequence?[_petJumpIndex].Lift * _petJumpPeak ?? 0f;
        Point offset = GetPetFrameOffset(_petRow, _petFrame);
        return new Rectangle(
            (int)System.Math.Round(_petX) + offset.X,
            _petGroundY + offset.Y - (int)System.Math.Round(jumpLift),
            PetCellWidth,
            PetCellHeight);
    }

    private bool IsPetHeadAt(Point point)
    {
        Rectangle destination = GetPetDestinationRectangle();
        Bitmap? atlas = _petHatRemoved ? _petAtlasWithoutHat : _petAtlas;
        if (atlas is null || !destination.Contains(point))
            return false;

        Rectangle source = GetPetSourceRectangle(_petRow, _petFrame);
        int localX = point.X - destination.X;
        int localY = point.Y - destination.Y;
        if (atlas.GetPixel(source.X + localX, source.Y + localY).A == 0)
            return false;

        // Верх фигуры ищется в текущем кадре, поэтому приседание и прыжок
        // не оставляют область захвата на прежней высоте. Обход — только при
        // снятии или возврате шляпы, по тому атласу, который сейчас виден.
        for (int y = 0; y <= localY; y++)
            for (int x = 0; x < PetCellWidth; x++)
                if (atlas.GetPixel(source.X + x, source.Y + y).A != 0)
                    return localY - y < PetHeadHitHeight;
        return false;
    }

    private void PetPanel_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left || _petHatRemoved || _petHatSprite is null || !IsPetHeadAt(e.Location))
            return;

        HatWindow? hatWindow = null;
        try
        {
            hatWindow = new HatWindow(_petHatSprite);
            hatWindow.Dropped += PetHat_Dropped;
            hatWindow.BeginDrag(Cursor.Position);
            _petHatWindow = hatWindow;
            _petHatRemoved = true;
            _petPanel.Invalidate();
        }
        catch (System.Runtime.InteropServices.ExternalException ex)
        {
            hatWindow?.Dispose();
            ShowHint($"Der Hut konnte nicht abgenommen werden: {ex.Message}");
        }
    }

    private void PetHat_Dropped(Point screenPoint)
    {
        if (!_petHatRemoved || _petHatWindow is null || WindowState == FormWindowState.Minimized
            || !_petPanel.IsHandleCreated)
            return;

        // Голова должна находиться в видимой части панели, а не за границей
        // прокрутки или под карточкой. Координаты шляпы всегда экранные.
        for (Control? control = _petPanel; control is not null; control = control.Parent)
            if (!control.Visible || !control.RectangleToScreen(control.ClientRectangle).Contains(screenPoint))
                return;

        Point panelPoint = _petPanel.PointToClient(screenPoint);
        if (_petPanel.GetChildAtPoint(panelPoint, GetChildAtPointSkip.Invisible) is not null
            || !IsPetHeadAt(panelPoint))
            return;

        _petHatRemoved = false;
        _petHatWindow.Dispose();
        _petHatWindow = null;
        _petPanel.Invalidate();
    }

    private void PetPanel_Paint(object? sender, PaintEventArgs e)
    {
        e.Graphics.Clear(SurfaceAlt);

        using (var pen = new Pen(BorderColor, 1f))
        {
            var zone = new Rectangle(0, _petGroundY, _petPanel.ClientSize.Width - 1, PetFrameHeight - 1);
            e.Graphics.DrawRectangle(pen, zone);
        }

        Bitmap? atlas = _petHatRemoved ? _petAtlasWithoutHat : _petAtlas;
        if (atlas is null)
        {
            return;
        }

        Rectangle destination = GetPetDestinationRectangle();
        Rectangle source = GetPetSourceRectangle(_petRow, _petFrame);

        e.Graphics.DrawImage(atlas, destination, source, GraphicsUnit.Pixel);
    }

    private Control CreateSection(string title, List<AppEntry> apps, bool includePetZone)
    {
        int width = System.Math.Max(860, flpApps.ClientSize.Width - 34);

        var section = new Panel
        {
            Width = width,
            Margin = new Padding(4, 6, 4, 10),
            BackColor = SurfaceAlt
        };

        section.Paint += (_, e) =>
        {
            using var pen = new Pen(BorderColor, 1f);
            var rect = section.ClientRectangle;
            rect.Width -= 1;
            rect.Height -= 1;
            e.Graphics.DrawRectangle(pen, rect);
        };

        var headerLabel = new Label
        {
            Text = title,
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            ForeColor = TextPrimary,
            Location = new Point(10, 10),
            Size = new Size(width - 20, 22)
        };

        var cardsPanel = new FlowLayoutPanel
        {
            Location = new Point(8, 38),
            Width = width - 16,
            WrapContents = true,
            BackColor = Color.Transparent
        };

        foreach (AppEntry app in apps)
        {
            cardsPanel.Controls.Add(CreateCard(app));
        }

        int cardWidth = 292;
        int cardHeight = 178;
        int columns = System.Math.Max(1, cardsPanel.Width / cardWidth);
        int rows = (apps.Count + columns - 1) / columns;
        int cardsHeight = System.Math.Max(cardHeight, rows * cardHeight);
        cardsPanel.Height = cardsHeight + (includePetZone ? PetFrameHeight : 0);

        if (includePetZone)
        {
            cardsPanel.BackColor = SurfaceAlt;
            SetPetHost(cardsPanel, cardsHeight);
        }

        section.Height = cardsPanel.Top + cardsPanel.Height + 10;
        section.Controls.Add(headerLabel);
        section.Controls.Add(cardsPanel);

        return section;
    }

    private Panel CreatePetOnlyPanel()
    {
        var panel = new Panel
        {
            Width = System.Math.Max(PetFrameWidth, flpApps.ClientSize.Width - 34),
            Height = PetFrameHeight,
            Margin = new Padding(4, 0, 4, 0),
            BackColor = SurfaceAlt
        };

        SetPetHost(panel, 0);
        return panel;
    }

    private void SetPetHost(Panel panel, int groundY)
    {
        _petPanel.Paint -= PetPanel_Paint;
        _petPanel.MouseDown -= PetPanel_MouseDown;
        _petPanel = panel;
        _petGroundY = groundY;
        _petPanel.Paint += PetPanel_Paint;
        _petPanel.MouseDown += PetPanel_MouseDown;
        EnableDoubleBuffer(_petPanel);
        ClampPetPosition();
    }

    private Control CreateEmptyState()
    {
        var panel = new Panel
        {
            Width = System.Math.Max(760, flpApps.ClientSize.Width - 48),
            Height = 118,
            BackColor = Surface,
            Margin = new Padding(10)
        };

        panel.Paint += (_, e) =>
        {
            using var pen = new Pen(BorderColor, 1f);
            var rect = panel.ClientRectangle;
            rect.Width -= 1;
            rect.Height -= 1;
            e.Graphics.DrawRectangle(pen, rect);
        };

        var label = new Label
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10),
            ForeColor = TextMuted,
            TextAlign = ContentAlignment.MiddleLeft,
            Text = "Keine Apps gefunden. Baue die Loesung oder pruefe deine Filter."
        };

        panel.Controls.Add(label);
        return panel;
    }

    private Control CreateCard(AppEntry app)
    {
        bool executableExists = File.Exists(app.ExePath);
        bool favorite = _launcherFacade.IsFavorite(app, _favoriteKeys);
        AppUsage usage = _launcherFacade.GetUsage(app, _state);

        string lastStart = usage.LastLaunchUtc.HasValue
            ? usage.LastLaunchUtc.Value.ToLocalTime().ToString("dd.MM.yyyy HH:mm")
            : "-";

        var card = new AppCardControl();
        card.BindData(
            app.Name,
            app.Category,
            executableExists ? Path.GetFileName(app.ExePath) : "Datei fehlt",
            $"Starts: {usage.Count} | Letzter Start: {lastStart}",
            executableExists,
            favorite,
            Surface,
            SurfaceAlt,
            TextPrimary,
            TextMuted,
            Accent,
            BorderColor);

        card.StartClicked += (_, __) => StartApp(app);
        card.FolderClicked += (_, __) => OpenFolder(app.FolderPath);
        card.PathClicked += (_, __) => CopyPath(app.ExePath);
        card.FavoriteClicked += (_, __) =>
        {
            _launcherFacade.ToggleFavorite(app, _favoriteKeys, _state);
            PersistState();
            Render();
        };
        card.CardDoubleClicked += (_, __) =>
        {
            if (File.Exists(app.ExePath))
            {
                StartApp(app);
            }
        };

        var menu = new ContextMenuStrip();
        menu.Items.Add("Als Admin starten", null, (_, __) => StartApp(app, asAdmin: true));
        menu.Items.Add("EXE-Pfad kopieren", null, (_, __) => CopyPath(app.ExePath));
        card.ContextMenuStrip = menu;

        _tips.SetToolTip(card, "Doppelklick = Start | Rechtsklick = Mehr Optionen");
        return card;
    }

    #endregion
}
