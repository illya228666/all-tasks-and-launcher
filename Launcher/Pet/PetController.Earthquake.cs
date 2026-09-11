using System.Drawing;
using Launcher.Pet.Animation;

namespace Launcher.Pet;

internal sealed partial class PetController
{
    private long _earthquakeStartedAt;

    // Точка входа для следующего этапа. Пока ни UI, ни таймеры её не вызывают.
    internal bool TryStartEarthquake()
    {
        if (!_started || _disposed || _state.Mode == PetMode.Earthquake
            || _renderer.GetSpeechHead() is not Point head)
            return false;

        _animationTimer.Stop();
        _movementTimer.Stop();
        _jumpTimer.Stop();
        _cursorTimer.Stop();
        _speech.Stop();
        // Землетрясение прерывает также прыжок и незавершённый подбор шляпы.
        _state.JumpSequence = null;
        _state.JumpIndex = 0;
        _state.JumpPending = false;
        _state.MovementPending = false;
        _state.MoveElapsedMs = 0;
        _state.HatPickupIndex = 0;
        _state.WaveLoopsRemaining = 0;
        _state.Mode = PetMode.Earthquake;
        _earthquakeStartedAt = Environment.TickCount64;
        _renderer.BeginEarthquake();
        _hat.SetInteractionBlocked(true);
        _hat.KnockOff(head);
        // Отображение оверлея может пропустить закрытие/перекомпоновку через Win32.
        if (!_started || _disposed || _state.Mode != PetMode.Earthquake)
            return false;
        AdvanceEarthquake();
        if (!_started || _disposed || _state.Mode != PetMode.Earthquake)
            return false;
        _animationTimer.Interval = PetAnimationCatalog.EarthquakeTickMs;
        _animationTimer.Start();
        return true;
    }

    private void AdvanceEarthquake()
    {
        long elapsed = Environment.TickCount64 - _earthquakeStartedAt;
        if (elapsed >= PetAnimationCatalog.EarthquakeDurationMs)
        {
            EndEarthquake();
            return;
        }

        // Абсолютное время: задержки UI не растягивают пятисекундную сцену.
        var frame = PetAnimationCatalog.EarthquakeFrames.First(item => elapsed < item.UntilMs);
        _state.Row = frame.Row;
        _state.Frame = frame.Frame;
        float seconds = elapsed / 1000f;
        float envelope = Math.Min(1f, Math.Min(elapsed / 200f,
            (PetAnimationCatalog.EarthquakeDurationMs - elapsed) / 900f));
        float amplitude = PetAnimationCatalog.EarthquakeShakePixels * envelope;
        var offset = new Point(
            (int)Math.Round(amplitude * (MathF.Sin(seconds * 47f) + 0.35f * MathF.Sin(seconds * 83f))),
            (int)Math.Round(amplitude * 0.55f * MathF.Sin(seconds * 61f)));
        _renderer.ApplyEarthquake(offset);
    }

    private void EndEarthquake()
    {
        if (_state.Mode != PetMode.Earthquake)
            return;

        _animationTimer.Stop();
        _state.Mode = PetMode.Idle;
        _state.Row = PetAnimationCatalog.IdleRow;
        _state.Frame = 0;
        _state.IdleElapsedMs = 0;
        _animationTimer.Interval = PetAnimationCatalog.FrameDurationsByRow[PetAnimationCatalog.IdleRow][0];
        _renderer.EndEarthquake();
        _hat.SetInteractionBlocked(false);
        if (!_started || _disposed)
            return;

        ReturnToIdle();
        _speech.Start();
        _cursorTimer.Start();
        UpdateCursorTracking();
    }
}
