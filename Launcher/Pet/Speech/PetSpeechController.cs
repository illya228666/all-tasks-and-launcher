using System.Drawing;
using System.Runtime.InteropServices;
using Launcher.Pet.UI;

namespace Launcher.Pet.Speech;

internal sealed class PetSpeechController : IDisposable
{
    private const int MinDelayMs = 10000;
    private const int MaxDelayMs = 20000;
    private const int LetterIntervalMs = 40;
    private const int ReadTimeMs = 4000;

    // Общий набор: языки выбираются случайно вместе с фразой, без повтора подряд.
    private static readonly string[] Phrases =
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

    private readonly Random _random;
    private readonly Func<bool> _isTrackingCursor;
    private readonly Func<bool> _isCalm;
    private readonly Func<Point?> _getHead;
    private readonly Func<bool> _hasPendingAction;
    private readonly Func<bool> _tryStartPendingAction;
    private readonly Action<string> _hintRequested;
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = LetterIntervalMs };
    private SpeechBubbleWindow? _window;
    private PetTheme _theme;
    private bool _available;
    private bool _wasCalm;
    private bool _hostChanging;
    private long _lastTick;
    private long _waitMs;
    private long _startedAt;
    private long _completedAt;
    private int _visibleLetters;
    private int _lastPhrase = -1;

    internal PetSpeechController(
        Random random,
        Func<bool> isTrackingCursor,
        Func<bool> isCalm,
        Func<Point?> getHead,
        Func<bool> hasPendingAction,
        Func<bool> tryStartPendingAction,
        Action<string> hintRequested)
    {
        _random = random;
        _isTrackingCursor = isTrackingCursor;
        _isCalm = isCalm;
        _getHead = getHead;
        _hasPendingAction = hasPendingAction;
        _tryStartPendingAction = tryStartPendingAction;
        _hintRequested = hintRequested;
        _timer.Tick += Timer_Tick;
        Reset();
    }

    internal bool IsSpeaking { get; private set; }

    internal void Start() => _timer.Start();

    internal void Stop()
    {
        _timer.Stop();
        _available = false;
        Reset();
    }

    internal void ApplyTheme(PetTheme theme)
    {
        _theme = theme;
        UpdatePlacement();
    }

    internal void BeginHostChange(bool preserveSpeech)
    {
        _hostChanging = true;
        if (!preserveSpeech)
            Reset();
    }

    internal void EndHostChange()
    {
        _hostChanging = false;
        UpdatePlacement();
    }

    internal void Reset()
    {
        IsSpeaking = false;
        _window?.Hide();
        _waitMs = _random.Next(MinDelayMs, MaxDelayMs + 1);
        _lastTick = Environment.TickCount64;
        _wasCalm = false;
    }

    internal void UpdatePlacement()
    {
        if (!IsSpeaking || _hostChanging)
            return;

        Point? head = _getHead();
        if (_isTrackingCursor() || head is null)
        {
            Reset();
            return;
        }

        try
        {
            _window!.Display(head.Value, _visibleLetters, _theme.Bubble, _theme.Text, _theme.Border);
        }
        catch (ExternalException exception)
        {
            HandleError(exception);
        }
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        long now = Environment.TickCount64;
        long elapsed = now - _lastTick;
        _lastTick = now;
        bool available = !_isTrackingCursor() && _getHead() is not null;
        if (!available)
        {
            if (_available || IsSpeaking)
                Reset();
            _available = false;
            return;
        }
        if (!_available)
        {
            Reset();
            _available = true;
            elapsed = 0;
        }

        bool calm = _isCalm();
        if (IsSpeaking)
        {
            if (!calm)
            {
                Reset();
                return;
            }

            _visibleLetters = (int)Math.Min(_window!.LetterCount, (now - _startedAt) / LetterIntervalMs);
            if (_visibleLetters == _window.LetterCount && _completedAt == 0)
                _completedAt = now;
            if (_completedAt != 0 && now - _completedAt >= ReadTimeMs)
            {
                Reset();
                _tryStartPendingAction();
                return;
            }
            UpdatePlacement();
            return;
        }

        if (calm && _wasCalm)
            _waitMs -= elapsed;
        _wasCalm = calm;

        // Уже ожидающие действия важнее новой реплики, даже во время махания.
        if (_hasPendingAction())
        {
            _tryStartPendingAction();
            return;
        }
        if (!calm || _waitMs > 0)
            return;

        int index = _random.Next(Phrases.Length - (_lastPhrase >= 0 ? 1 : 0));
        if (_lastPhrase >= 0 && index >= _lastPhrase)
            index++;
        try
        {
            _window ??= new SpeechBubbleWindow();
            _window.SetPhrase(Phrases[index]);
            _lastPhrase = index;
            _startedAt = now;
            _completedAt = 0;
            _visibleLetters = 0;
            IsSpeaking = true;
            UpdatePlacement();
        }
        catch (ExternalException exception)
        {
            HandleError(exception);
        }
    }

    private void HandleError(ExternalException exception)
    {
        _window?.Dispose();
        _window = null;
        Reset(); // Ошибка оверлея не должна блокировать бег и прыжки.
        _hintRequested("Sprechblase konnte nicht angezeigt werden: " + exception.Message);
    }

    public void Dispose()
    {
        _timer.Tick -= Timer_Tick;
        _timer.Dispose();
        _window?.Dispose();
        _window = null;
        IsSpeaking = false;
    }
}
