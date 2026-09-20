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
    private long _jumpAtMs, _walkAtMs;
    internal Point Shake { get; private set; }

    internal PetBehavior(PetState state, Random random)
    {
        _state = state;
        _random = random;
    }

    internal void Reset(long nowMs, PetSpeech speech)
    {
        Change(PetMode.Idle, nowMs, speech);
        ScheduleJump(nowMs);
        ScheduleWalk(nowMs);
    }

    private void ScheduleJump(long nowMs) => _jumpAtMs = nowMs + _random.Next(PetJump.MinDelayMs, PetJump.MaxDelayMs + 1);
    private void ScheduleWalk(long nowMs) => _walkAtMs = nowMs + _random.Next(PetWalk.MinDelayMs, PetWalk.MaxDelayMs + 1);
    private void Change(PetMode mode, long nowMs, PetSpeech speech)
    {
        PetMode previous = _state.Mode;
        _state.Mode = mode;
        _state.StartedAtMs = nowMs;
        _state.Frame = 0;
        _state.JumpLift = 0;
        Shake = Point.Empty;
        if (mode != PetMode.Waving && !(mode == PetMode.Idle && previous == PetMode.Waving))
            speech.Reset(nowMs);
        if (mode == PetMode.Jumping)
            _jumpAtMs = long.MaxValue;
        if (mode == PetMode.Walking)
            _walkAtMs = long.MaxValue;
        if (mode == PetMode.Idle)
        {
            _state.Row = PetAnimationCatalog.IdleRow;
            if (previous != PetMode.Waving && previous != PetMode.Walking)
                ScheduleJump(nowMs);
            if (previous != PetMode.Waving && previous != PetMode.Jumping)
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

        Point? pickup = hat.PickupPoint(environment.Surfaces, environment.PickupSurfaceIdentity);
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

        if (pickup is not null && _state.Mode != PetMode.Jumping && headVisible)
        {
            Change(PetMode.RetrievingHat, nowMs, speech);
            PetHatPickup.Walk(_state, environment, pickup.Value, nowMs, elapsedSeconds);
            return;
        }

        if (environment.CanTrackCursor && !(_state.Mode == PetMode.Jumping && pickup is not null))
        {
            if (_state.Mode != PetMode.Looking)
                Change(PetMode.Looking, nowMs, speech);
            PetLook.Update(_state, environment);
            return;
        }

        if (_state.Mode == PetMode.Looking)
            Change(PetMode.Idle, nowMs, speech);
        bool jumpDue = nowMs >= _jumpAtMs, walkDue = nowMs >= _walkAtMs;
        speech.Update(nowMs, _state.Mode is PetMode.Idle or PetMode.Waving, headVisible, jumpDue || walkDue);
        if (_state.Mode == PetMode.Jumping)
        {
            if (PetJump.Update(_state, nowMs))
                Change(PetMode.Idle, nowMs, speech);
        }
        else if (_state.Mode == PetMode.Walking)
        {
            if (PetWalk.Update(_state, nowMs))
                Change(PetMode.Idle, nowMs, speech);
        }
        else if (_state.Mode == PetMode.Waving)
        {
            if (PetWave.Update(_state, nowMs))
            {
                // RU: Завершение махания сохраняет ожидающие действия и речь. DE: Warten und Sprache bleiben erhalten.
                Change(PetMode.Idle, nowMs, speech);
            }
        }
        else
        {
            if (!speech.IsSpeaking && jumpDue)
            {
                PetJump.Prepare(_state, environment);
                Change(PetMode.Jumping, nowMs, speech);
                PetJump.Update(_state, nowMs);
            }
            else if (!speech.IsSpeaking && walkDue)
            {
                if (PetWalk.Prepare(_state, environment, _random))
                {
                    Change(PetMode.Walking, nowMs, speech);
                    PetWalk.Update(_state, nowMs);
                }
                else
                    _walkAtMs = nowMs + _random.Next(PetWalk.MinDelayMs, PetWalk.MaxDelayMs + 1);
            }
            else if (nowMs - _state.StartedAtMs >= PetWave.DelayMs)
            {
                Change(PetMode.Waving, nowMs, speech);
                PetWave.Update(_state, nowMs);
            }
            else
                PetIdle.Update(_state, nowMs);
        }

        PetPlacement.Fit(_state, environment);
    }
}
