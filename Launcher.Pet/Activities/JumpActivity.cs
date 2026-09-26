using Launcher.Pet.Animation;
using Launcher.Pet.Data;

namespace Launcher.Pet.Activities;

internal sealed class JumpActivity : PetActivity
{
    internal const int ObstacleProbeOffsetPixels = 2;
    internal const int MaxDelayMs = 17000;
    internal const int MinDelayMs = 10000;
    private readonly bool _failed;
    internal JumpActivity(PetBody body, PetEnvironment env)
    {
        int bottom = env.ObstaclesLocal.Count == 0 ? 0 : env.ObstaclesLocal.Max(rect => rect.Top);
        var row = env.ObstaclesLocal.Where(rect => rect.Top == bottom).ToArray();
        float probe = body.X + JumpActivity.ObstacleProbeOffsetPixels;
        _failed = row.Length > 0 && probe >= row.Min(rect => rect.Left) && probe <= row.Max(rect => rect.Right);
    }
    internal override PetMode Mode => PetMode.Jumping;
    internal override bool UsesRoutine => true;
    internal override bool PreserveWalkSchedule => true;
    internal override bool CanRetrieveHat => false;
    internal override bool CanLook(bool hasPickup) => !hasPickup;
    internal override void Enter(PetActor actor, PetActivity previous, long now) => actor.Routine.JumpStarted();
    internal override void Update(PetActivityContext c)
    {
        long elapsed = c.Now - StartedAtMs;
        foreach (var frame in _failed ? PetAnimationCatalog.FailedJumpFrames : PetAnimationCatalog.SuccessfulJumpFrames)
        {
            int duration = PetAnimationCatalog.FrameDurationsByRow[frame.Row][frame.Frame];
            if (elapsed < duration)
            {
                c.Body.Row = frame.Row;
                c.Body.Frame = frame.Frame;
                c.Body.JumpLift = frame.Lift * PetLogicalGeometry.Height / (_failed ? 4f : 3f);
                return;
            }
            elapsed -= duration;
        }
        c.Actor.Change(new IdleActivity(), c.Now);
    }
}
