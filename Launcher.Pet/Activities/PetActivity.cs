using System.Drawing;
using Launcher.Pet.Data;

namespace Launcher.Pet.Activities;

// The actor has exactly one activity. Activities own their clocks and transient
// state; the body contains only placement and the last published visual pose.
internal abstract class PetActivity
{
    internal long StartedAtMs { get; set; }
    // Until the first pose of a continuation is published, idle scheduling keeps
    // the outgoing action's pending timers (including a stop between two ticks).
    internal IdleContinuation Continuation { get; set; }
    internal virtual PetMode Mode => PetMode.Idle;
    internal virtual bool UsesRoutine => false;
    internal virtual bool UsesGroundPolicy => UsesRoutine;
    internal virtual bool SuppressNavigation => false;
    internal virtual bool Calm => false;
    internal virtual bool PreserveSpeech => false;
    internal virtual bool PreserveJumpSchedule => false;
    internal virtual bool PreserveWalkSchedule => false;
    internal virtual bool AllowsHatDrag => true;
    internal virtual bool AllowsPetDrag => true;
    internal virtual bool AllowsEarthquake => true;
    internal virtual bool FallsOnEarthquake => false;
    internal virtual bool IsDragging => false;
    internal virtual bool ShowsRoute => true;
    internal virtual bool CanRetrieveHat => true;
    internal virtual bool CanLook(bool hasPickup) => true;
    internal virtual bool ShakeWindow => false;
    internal virtual void Enter(PetActor actor, PetActivity previous, long now) { }
    internal virtual bool UpdateBeforeRoutine(PetActivityContext context) => false;
    internal abstract void Update(PetActivityContext context);
    internal virtual void Move(PetActivityContext context, Point cursor) { }
    internal virtual void Drop(PetActivityContext context) { }
}

internal readonly record struct IdleContinuation(bool Speech, bool Jump, bool Walk);

internal readonly record struct PetActivityContext(PetActor Actor, PetEnvironment Environment, long Now, float Elapsed, bool HeadVisible = true)
{
    internal PetBody Body => Actor.Body;
    internal float Step => Math.Clamp(Elapsed, 0, 0.04f);
    internal float HalfWidth => PetLogicalGeometry.Width * Environment.Scale / 2;
}
