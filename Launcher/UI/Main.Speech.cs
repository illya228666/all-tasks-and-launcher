using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Launcher.UI.Controls;

namespace Launcher.UI;

public partial class Main
{
    // Случайная пауза в миллисекундах: считаем только idle и waving вне реплики.
    private const int PetSpeechMinDelayMs = 10000;
    private const int PetSpeechMaxDelayMs = 20000;
    // Один таймер обслуживает ожидание, печать и выдержку. Скорость — буква за 40 мс.
    private const int PetSpeechLetterIntervalMs = 40;
    // Сколько держать готовую фразу после появления последней буквы.
    private const int PetSpeechReadTimeMs = 4000;

    // Общий набор: языки выбираются случайно вместе с фразой, без повтора подряд.
    // Реплики можно менять здесь; отдельные слова должны помещаться в 260 пикселей.
    private static readonly string[] PetSpeechPhrases =
    {
        "Я тут. Шляпа — по обстоятельствам.",
        "Не завис. Задумался.",
        "Ещё одна задачка — и можно чай.",
        "Большую задачу съедим по кусочкам.",
        "Пока ты думаешь, я красиво стою.",
        "Маленький шаг тоже считается.",
        "Ich bin da. Der Hut ist optional.",
        "Nicht eingefroren. Nur am Denken.",
        "Noch eine Aufgabe, dann gibt’s Tee.",
        "Große Aufgaben? Stück für Stück!",
        "Ich warte nicht. Ich sammle Ideen.",
        "Auch kleine Schritte zählen."
    };

    private System.Windows.Forms.Timer? _petSpeechTimer;
    private SpeechBubbleWindow? _petSpeechWindow;
    private bool _petSpeaking;
    private bool _petSpeechAvailable;
    private bool _petSpeechWasCalm;
    private bool _petHostChanging;
    private long _petSpeechLastTick;
    private long _petSpeechWaitMs;
    private long _petSpeechStartedAt;
    private long _petSpeechCompletedAt;
    private int _petSpeechVisibleLetters;
    private int _petSpeechLastPhrase = -1;

    private void InitializePetSpeech()
    {
        _petSpeechTimer = new System.Windows.Forms.Timer(components) { Interval = PetSpeechLetterIntervalMs };
        _petSpeechTimer.Tick += PetSpeechTimer_Tick;
        ResetPetSpeech();
        LocationChanged += (_, _) => UpdatePetSpeechPlacement();
        VisibleChanged += (_, _) => UpdatePetSpeechPlacement();
        flpApps.Scroll += (_, _) => UpdatePetSpeechPlacement();
    }

    private void ResetPetSpeech()
    {
        _petSpeaking = false;
        _petSpeechWindow?.Hide();
        _petSpeechWaitMs = _random.Next(PetSpeechMinDelayMs, PetSpeechMaxDelayMs + 1);
        _petSpeechLastTick = Environment.TickCount64;
        _petSpeechWasCalm = false;
    }

    private void PetSpeechTimer_Tick(object? sender, EventArgs e)
    {
        long now = Environment.TickCount64;
        long elapsed = now - _petSpeechLastTick;
        _petSpeechLastTick = now;
        bool available = !_petTrackingCursor && TryGetPetSpeechHead(out _);
        if (!available)
        {
            if (_petSpeechAvailable || _petSpeaking)
                ResetPetSpeech();
            _petSpeechAvailable = false;
            return;
        }
        if (!_petSpeechAvailable)
        {
            ResetPetSpeech();
            _petSpeechAvailable = true;
            elapsed = 0;
        }

        bool calm = _petJumpSequence is null && _petRow is PetIdleRow or PetWaveRow;
        if (_petSpeaking)
        {
            if (!calm)
            {
                ResetPetSpeech();
                return;
            }

            _petSpeechVisibleLetters = (int)Math.Min(_petSpeechWindow!.LetterCount,
                (now - _petSpeechStartedAt) / PetSpeechLetterIntervalMs);
            if (_petSpeechVisibleLetters == _petSpeechWindow.LetterCount && _petSpeechCompletedAt == 0)
                _petSpeechCompletedAt = now;
            if (_petSpeechCompletedAt != 0 && now - _petSpeechCompletedAt >= PetSpeechReadTimeMs)
            {
                ResetPetSpeech();
                TryStartPendingPetAction();
                return;
            }
            UpdatePetSpeechPlacement();
            return;
        }

        if (calm && _petSpeechWasCalm)
            _petSpeechWaitMs -= elapsed;
        _petSpeechWasCalm = calm;

        // Уже ожидающие действия важнее новой реплики, даже во время махания.
        if (_petJumpPending || _petMovementPending)
        {
            TryStartPendingPetAction();
            return;
        }
        if (!calm || _petSpeechWaitMs > 0)
            return;

        int index = _random.Next(PetSpeechPhrases.Length - (_petSpeechLastPhrase >= 0 ? 1 : 0));
        if (_petSpeechLastPhrase >= 0 && index >= _petSpeechLastPhrase)
            index++;
        try
        {
            _petSpeechWindow ??= new SpeechBubbleWindow();
            _petSpeechWindow.SetPhrase(PetSpeechPhrases[index]);
            _petSpeechLastPhrase = index;
            _petSpeechStartedAt = now;
            _petSpeechCompletedAt = 0;
            _petSpeechVisibleLetters = 0;
            _petSpeaking = true;
            UpdatePetSpeechPlacement();
        }
        catch (ExternalException exception)
        {
            HandlePetSpeechError(exception);
        }
    }

    private bool TryGetPetSpeechHead(out Point head)
    {
        head = Point.Empty;
        if (!Visible || WindowState == FormWindowState.Minimized || _petPanel is null
            || _petPanel.IsDisposed || !_petPanel.IsHandleCreated || _petPanel.Parent is null)
            return false;

        Rectangle destination = GetPetDestinationRectangle();
        // Ось корпуса и середина верхней области фигуры: для idle и waving.
        // Используем общую геометрию кадра, чтобы учитывать положение панели.
        head = _petPanel.PointToScreen(new Point(
            destination.X + PetBodyAnchorXByRow[_petRow][_petFrame],
            destination.Y + PetHeadHitHeight / 2));
        for (Control? control = _petPanel; control is not null; control = control.Parent)
            if (!control.Visible || !control.ClientRectangle.Contains(control.PointToClient(head)))
                return false;
        return true;
    }

    private void UpdatePetSpeechPlacement()
    {
        if (!_petSpeaking || _petHostChanging)
            return;
        if (_petTrackingCursor || !TryGetPetSpeechHead(out Point head))
        {
            ResetPetSpeech();
            return;
        }
        try
        {
            _petSpeechWindow!.Display(head, _petSpeechVisibleLetters, Surface, TextPrimary, BorderColor);
        }
        catch (ExternalException exception)
        {
            HandlePetSpeechError(exception);
        }
    }

    private void HandlePetSpeechError(ExternalException exception)
    {
        _petSpeechWindow?.Dispose();
        _petSpeechWindow = null;
        ResetPetSpeech(); // Ошибка оверлея не должна блокировать бег и прыжки.
        ShowHint("Sprechblase konnte nicht angezeigt werden: " + exception.Message);
    }

    private bool TryStartPendingPetAction()
    {
        if (_petSpeaking || _petTrackingCursor || _petJumpSequence is not null || _petRow != PetIdleRow)
            return false;
        if (_petJumpPending)
        {
            StartPetJump();
            return true;
        }
        return _petMovementPending && StartPetMovement();
    }
}
