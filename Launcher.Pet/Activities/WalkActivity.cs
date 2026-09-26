using Launcher.Pet.Animation;
using Launcher.Pet.Data;

namespace Launcher.Pet.Activities;

internal sealed class WalkActivity : PetActivity
{
    internal const float PixelsPerCycle = 120f;
    internal const int MaxDelayMs = 20000;
    internal const int MinDelayMs = 10000;
    private readonly float _start, _target;
    private readonly int _duration;
    private WalkActivity(float start, float target, int duration) => (_start, _target, _duration) = (start, target, duration);
    internal override PetMode Mode => PetMode.Walking;
    internal override bool UsesRoutine => true;
    internal override bool PreserveJumpSchedule => true;
    internal override void Enter(PetActor actor, PetActivity previous, long now) => actor.Routine.WalkStarted();
    internal static WalkActivity? Create(PetBody body, PetEnvironment env, Random random)
    {
        float min = PetLogicalGeometry.EdgePadding;
        float max = Math.Max(min, env.AreaWidth - PetLogicalGeometry.Width - min);
        float distance = env.WindowWidth / 8f;
        bool left = body.X - min >= distance, right = max - body.X >= distance;
        if (!left && !right) return null;
        bool moveRight = right && (!left || random.Next(2) == 0);
        float available = moveRight ? max - body.X : body.X - min;
        distance += (float)random.NextDouble() * (available - distance);
        int cycle = PetAnimationCatalog.FrameDurationsByRow[PetAnimationCatalog.MoveRightRow].Sum();
        return new(body.X, body.X + (moveRight ? distance : -distance), Math.Max(1, (int)Math.Round(distance / WalkActivity.PixelsPerCycle)) * cycle);
    }
    internal override void Update(PetActivityContext c)
    {
        long elapsed = c.Now - StartedAtMs;
        c.Body.X = _start + (_target - _start) * Math.Min(1f, (float)elapsed / _duration);
        PetFrames.Loop(c.Body, _target >= _start ? PetAnimationCatalog.MoveRightRow : PetAnimationCatalog.MoveLeftRow, elapsed);
        if (elapsed >= _duration) c.Actor.Change(new IdleActivity(), c.Now);
    }
}
