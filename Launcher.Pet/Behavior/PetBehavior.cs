using System.Drawing;
using Launcher.Pet.Animation;
using Launcher.Pet.Data;
using Launcher.Pet.Hat;
using Launcher.Pet.Speech;

namespace Launcher.Pet.Behavior;
// RU: Только здесь выбирается следующее действие. DE: Nur hier wird die nächste Aktion gewählt.
internal sealed class PetBehavior
{
    private readonly PetState _state;
    private readonly Random _random;
    private long _walkAtMs;
    internal Point Shake { get; private set; }

    internal PetBehavior(PetState state, Random random)
    {
        _state = state;
        _random = random;
    }

    internal void Reset(long nowMs, PetSpeech speech)
    {
        Change(PetMode.Idle, nowMs, speech);
    }

    private void ScheduleWalk(long nowMs) => _walkAtMs = nowMs + _random.Next(PetWalk.MinDelayMs, PetWalk.MaxDelayMs + 1);
    private void Change(PetMode mode, long nowMs, PetSpeech speech)
    {
        PetMode previous = _state.Mode;
        _state.Mode = mode;
        _state.StartedAtMs = nowMs;
        _state.Frame = 0;
        _state.JumpLift = 0;
        Shake = Point.Empty;
        speech.Reset(nowMs);
        if (mode == PetMode.Walking)
            _walkAtMs = long.MaxValue;
        if (mode == PetMode.Idle)
        {
            _state.Row = PetAnimationCatalog.IdleRow;
            if (previous != PetMode.Walking)
                ScheduleWalk(nowMs);
        }
    }

    internal bool Earthquake(long nowMs, Point? head, HatWorld hat, PetSpeech speech)
    {
        if (_state.Mode == PetMode.Earthquake || head is null)
            return false;
        Change(PetMode.Earthquake, nowMs, speech);
        hat.KnockOff(head.Value);
        Shake = PetEarthquake.Update(_state, nowMs);
        return true;
    }

    internal void Update(long nowMs, float elapsedSeconds, PetEnvironment environment, HatWorld hat, PetSpeech speech, bool headVisible)
    {
        if (_state.Mode == PetMode.Earthquake)
        {
            if (nowMs - _state.StartedAtMs < PetEarthquake.DurationMs)
            {
                Shake = PetEarthquake.Update(_state, nowMs);
                return;
            }

            Change(PetMode.Idle, nowMs, speech);
        }

        Point? pickup = environment.Ruins is null ? hat.PickupPoint(environment.Surfaces, environment.PickupSurfaceIdentity) : null;
        if (_state.Mode is PetMode.RetrievingHat or PetMode.PuttingOnHat)
        {
            if (pickup is null && !hat.Attached)
            {
                Change(PetMode.Idle, nowMs, speech);
                return;
            }

            if (_state.Mode == PetMode.RetrievingHat)
            {
                if (pickup is null)
                {
                    Change(PetMode.Idle, nowMs, speech);
                    return;
                }

                if (PetHatPickup.Walk(_state, environment, pickup.Value, nowMs, elapsedSeconds))
                    Change(PetMode.PuttingOnHat, nowMs, speech);
            }
            else
            {
                if (pickup is Point point && Math.Abs(PetHatPickup.TargetX(point, environment) - _state.X) > 1f)
                    Change(PetMode.RetrievingHat, nowMs, speech);
                else if (PetHatPickup.PutOn(_state, hat, nowMs))
                    Change(PetMode.Idle, nowMs, speech);
            }

            return;
        }

        if (pickup is not null && headVisible)
        {
            Change(PetMode.RetrievingHat, nowMs, speech);
            PetHatPickup.Walk(_state, environment, pickup.Value, nowMs, elapsedSeconds);
            return;
        }

        // Пока новых кадров нет, обычные look / jump / wave не запускаются.
        if (_state.Mode is PetMode.Looking or PetMode.Jumping or PetMode.Waving)
            Change(PetMode.Idle, nowMs, speech);

        bool walkDue = environment.Ruins is null && nowMs >= _walkAtMs;
        speech.Update(nowMs, _state.Mode == PetMode.Idle, headVisible, walkDue);

        if (_state.Mode == PetMode.Walking)
        {
            if (PetWalk.Update(_state, nowMs))
                Change(PetMode.Idle, nowMs, speech);
        }
        else if (walkDue && !speech.IsSpeaking)
        {
            if (PetWalk.Prepare(_state, environment, _random))
            {
                Change(PetMode.Walking, nowMs, speech);
                PetWalk.Update(_state, nowMs);
            }
            else
                ScheduleWalk(nowMs);
        }
        else
            PetIdle.Update(_state, nowMs);

        PetPlacement.Fit(_state, environment);
    }
}
