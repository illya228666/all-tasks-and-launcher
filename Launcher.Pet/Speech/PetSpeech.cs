using System.Globalization;

namespace Launcher.Pet.Speech;
internal sealed class PetSpeech
{
    private const int MinDelayMs = 10000;
    private const int MaxDelayMs = 20000;
    private const int LetterIntervalMs = 40;
    private const int ReadTimeMs = 4000;
    private readonly Random _random;
    private long _lastMs, _startedMs, _remainingMs;
    private bool _wasCalm;
    private int _lastPhrase = -1, _letterCount;
    internal string? Phrase { get; private set; }
    internal int VisibleLetters { get; private set; }
    internal bool IsSpeaking => Phrase is not null;

    internal PetSpeech(Random random)
    {
        _random = random;
        Reset(0);
    }

    internal void Reset(long nowMs)
    {
        Phrase = null;
        VisibleLetters = 0;
        _remainingMs = _random.Next(MinDelayMs, MaxDelayMs + 1);
        _lastMs = nowMs;
        _wasCalm = false;
    }

    internal void Update(long nowMs, bool calm, bool available, bool pendingAction)
    {
        long elapsed = Math.Max(0, nowMs - _lastMs);
        _lastMs = nowMs;
        if (!available || !calm)
        {
            if (IsSpeaking)
                Reset(nowMs);
            _wasCalm = false;
            return;
        }

        if (IsSpeaking)
        {
            VisibleLetters = (int)Math.Min(_letterCount, (nowMs - _startedMs) / LetterIntervalMs);
            if (nowMs - _startedMs >= _letterCount * LetterIntervalMs + ReadTimeMs)
                Reset(nowMs);
            return;
        }

        if (_wasCalm)
            _remainingMs -= elapsed;
        _wasCalm = true;
        if (pendingAction || _remainingMs > 0)
            return;
        int count = PetPhrases.Values.Length;
        int index = _random.Next(count - (_lastPhrase >= 0 ? 1 : 0));
        if (_lastPhrase >= 0 && index >= _lastPhrase)
            index++;
        _lastPhrase = index;
        Phrase = PetPhrases.Values[index];
        _letterCount = StringInfo.ParseCombiningCharacters(Phrase).Length;
        _startedMs = nowMs;
        VisibleLetters = 0;
    }
}
