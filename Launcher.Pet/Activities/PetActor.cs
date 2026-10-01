using System.Drawing;
using Launcher.Pet.Behavior;
using Launcher.Pet.Data;
using Launcher.Pet.Exploration;
using Launcher.Pet.Hat;
using Launcher.Pet.Speech;

namespace Launcher.Pet.Activities;

internal sealed class PetActor
{
    internal PetBody Body { get; } = new();
    internal PetActivity Activity { get; private set; } = new IdleActivity();
    internal PetRoutine Routine { get; }
    internal PetNavigation Navigation { get; }
    internal Life.PetLifeBrain? Life { get; set; }
    internal HatWorld Hat { get; }
    internal PetSpeech Speech { get; }
    internal Point Shake { get; set; }
    internal PetLocation Location { get; set; }
    internal float DesktopScale { get; set; } = .5f;
    internal float RenderScale => Location == PetLocation.Launcher ? 1 : Activity is DepartureActivity departure ? departure.Scale : DesktopScale;
    internal bool HasLanded => Location == PetLocation.Desktop || Activity is DepartureActivity { HasLanded: true };

    internal PetActor(Random random, HatWorld hat, PetSpeech speech)
    {
        Hat = hat;
        Speech = speech;
        Routine = new(random);
        Navigation = new(random);
    }

    // Routine transitions reset the animation clock and pose. Traversal transitions
    // use ContinueWith: their outgoing frame remains visible until the next tick.
    internal void Change(PetActivity next, long now)
    {
        PetActivity previous = Activity;
        ContinueWith(next, now);
        Present(next.Mode);
        Body.Frame = 0;
        Body.JumpLift = 0;
        Shake = Point.Empty;
        if (!next.PreserveSpeech && !(next is IdleActivity && previous.Continuation.Speech))
            Speech.Reset(now);
        next.Enter(this, previous, now);
    }

    internal void ContinueWith(PetActivity next, long now)
    {
        next.Continuation = Activity.Continuation;
        Activity = next;
        next.StartedAtMs = now;
    }

    internal void Present(PetMode mode)
    {
        Body.Mode = mode;
        Activity.Continuation = new(Activity.PreserveSpeech, Activity.PreserveJumpSchedule, Activity.PreserveWalkSchedule);
    }

    internal void ResetSession(long now)
    {
        // Departure is paused by Stop, and resumes at its accumulated physics
        // time after Start. Reset only its routine/speech timers while paused.
        var departure = Activity as DepartureActivity;
        Reset(now);
        if (departure is not null) ContinueWith(departure, now);
    }

    internal void Reset(long now)
    {
        Change(new IdleActivity(), now);
        // Deliberate extra scheduling: preserves the original shared Random stream.
        Routine.ScheduleJump(now);
        Routine.ScheduleWalk(now);
    }

    internal void Update(PetActivityContext context)
    {
        if (Location == PetLocation.Desktop && context.Environment.Ruins is not null)
        {
            context = context with { HeadVisible = true };
            Navigation.Prepare(context);
            if (!Activity.UsesGroundPolicy || !Navigation.UpdateGround(context))
            {
                if (Activity.UsesRoutine)
                    Routine.Update(context with { Environment = context.Environment with { PetZoneTopY = (int)Navigation.FootY - PetLogicalGeometry.Height } });
                else
                    Activity.Update(context);
            }
            Body.JumpLift = context.Environment.PetZoneTopY + PetLogicalGeometry.Height - Navigation.FootY;
        }
        else if (Activity.UsesRoutine)
        {
            PetPlacement.Fit(Body, context.Environment);
            var bounds = PetPlacement.SpriteBounds(Body, context.Environment, Shake);
            Routine.Update(context with { HeadVisible = PetPlacement.VisibleHead(Body, context.Environment, bounds) is not null });
        }
        else
            Activity.Update(context);
    }
}
