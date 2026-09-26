using Launcher.Pet.Data;
using Launcher.Pet.Exploration;

namespace Launcher.Pet.Activities;

internal sealed class SeekRuinHatActivity : PetActivity
{
    internal override PetMode Mode => PetMode.Walking;
    internal override bool UsesGroundPolicy => true;
    internal override bool PreserveJumpSchedule => true;
    internal override void Update(PetActivityContext c)
    {
        float x = c.Actor.Hat.RestingPoint(c.Environment.Surfaces)!.Value.X - c.Environment.AreaScreenPosition.X;
        if (!RuinWalking.Step(c, x)) return;
        c.Actor.ContinueWith(new RuinHatPickupActivity(), c.Now);
        c.Actor.Present(PetMode.PuttingOnHat);
        c.Actor.Speech.Reset(c.Now);
    }
}
