using System.Drawing;
using Launcher.Pet.Behavior;
using Launcher.Pet.Data;

namespace Launcher.Pet.Activities;

internal sealed class EarthquakeActivity : PetActivity
{
    internal static bool TryStart(PetActivityContext context, Point? head)
    {
        var actor = context.Actor;
        if (!actor.Activity.AllowsEarthquake || head is null) return false;
        float lift = actor.Body.JumpLift;
        bool fall = actor.Location == PetLocation.Desktop && actor.Activity.FallsOnEarthquake;
        actor.Change(new EarthquakeActivity(), context.Now);
        actor.Hat.KnockOff(head.Value);
        actor.Activity.Update(context);
        actor.Body.JumpLift = lift;
        if (actor.Location == PetLocation.Desktop)
        {
            if (fall)
            {
                actor.Reset(context.Now);
                actor.Navigation.Link = null;
                actor.ContinueWith(new FlightActivity(), context.Now);
                actor.Body.Mode = PetMode.Falling;
                actor.Body.JumpLift = lift;
            }
            else actor.Navigation.CancelRoute();
        }
        return true;
    }

    internal override PetMode Mode => PetMode.Earthquake;
    internal override bool UsesRoutine => true;
    internal override bool AllowsEarthquake => false;
    internal override bool AllowsHatDrag => false;
    internal override bool AllowsPetDrag => false;
    internal override bool ShakeWindow => true;
    internal override bool SuppressNavigation => true;
    internal override bool UpdateBeforeRoutine(PetActivityContext c)
    {
        if (c.Now - StartedAtMs < PetEarthquake.DurationMs)
        {
            Update(c);
            return true;
        }
        c.Actor.Change(new IdleActivity(), c.Now);
        return false;
    }
    internal override void Update(PetActivityContext c) => c.Actor.Shake = PetEarthquake.Update(c.Body, c.Now - StartedAtMs);
}
