using Launcher.Pet.Animation;
using Launcher.Pet.Data;

namespace Launcher.Pet.Activities;

internal sealed class IdleActivity : PetActivity
{
    internal override bool UsesRoutine => true;
    internal override bool Calm => true;
    internal override void Enter(PetActor actor, PetActivity previous, long now)
    {
        actor.Body.Row = PetAnimationCatalog.IdleRow;
        if (!previous.Continuation.Jump) actor.Routine.ScheduleJump(now);
        if (!previous.Continuation.Walk) actor.Routine.ScheduleWalk(now);
    }
    internal override void Update(PetActivityContext c) => PetFrames.Loop(c.Body, PetAnimationCatalog.IdleRow, c.Now - StartedAtMs);
}
