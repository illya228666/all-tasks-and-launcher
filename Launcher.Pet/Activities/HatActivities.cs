using System.Drawing;
using Launcher.Pet.Behavior;
using Launcher.Pet.Data;

namespace Launcher.Pet.Activities;

internal abstract class LauncherHatActivity : PetActivity
{
    internal override bool UsesRoutine => true;
    internal override bool UpdateBeforeRoutine(PetActivityContext c)
    {
        Update(c);
        return true;
    }
    protected Point? Pickup(PetActivityContext c) => c.Environment.Ruins is null
        ? c.Actor.Hat.PickupPoint(c.Environment.Surfaces, c.Environment.PickupSurfaceIdentity) : null;
}

internal sealed class RetrieveHatActivity : LauncherHatActivity
{
    internal override PetMode Mode => PetMode.RetrievingHat;
    internal bool Walk(PetActivityContext c, Point point) => PetHatPickup.Walk(c.Body, c.Environment, point, c.Now - StartedAtMs, c.Elapsed);
    internal override void Update(PetActivityContext c)
    {
        if (Pickup(c) is not Point point) c.Actor.Change(new IdleActivity(), c.Now);
        else if (Walk(c, point)) c.Actor.Change(new PutOnHatActivity(), c.Now);
    }
}

internal sealed class PutOnHatActivity : LauncherHatActivity
{
    internal override PetMode Mode => PetMode.PuttingOnHat;
    internal override void Update(PetActivityContext c)
    {
        Point? pickup = Pickup(c);
        if (pickup is null && !c.Actor.Hat.Attached) c.Actor.Change(new IdleActivity(), c.Now);
        else if (pickup is Point point && Math.Abs(PetHatPickup.TargetX(point, c.Environment) - c.Body.X) > 1f)
            c.Actor.Change(new RetrieveHatActivity(), c.Now);
        else if (PetHatPickup.PutOn(c.Body, c.Actor.Hat, c.Now - StartedAtMs)) c.Actor.Change(new IdleActivity(), c.Now);
    }
}

internal sealed class RuinHatPickupActivity : PetActivity
{
    internal override PetMode Mode => PetMode.PuttingOnHat;
    internal override void Update(PetActivityContext c)
    {
        var nav = c.Actor.Navigation;
        nav.FootY = nav.Available[nav.Support].Y;
        if (nav.HatPlatform(c)?.Id != nav.Support && !c.Actor.Hat.Attached) c.Actor.Reset(c.Now);
        else if (PetHatPickup.PutOn(c.Body, c.Actor.Hat, c.Now - StartedAtMs))
        {
            nav.CancelRoute();
            c.Actor.Reset(c.Now);
            nav.NextTrip = c.Now + 3000;
        }
    }
}
