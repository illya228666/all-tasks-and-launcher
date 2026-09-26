using Launcher.Pet.Data;
using Launcher.Pet.Exploration;

namespace Launcher.Pet.Activities;

internal sealed class BridgeActivity : PetActivity
{
    private readonly RuinLink _link;
    internal BridgeActivity(RuinLink link) => _link = link;
    internal override PetMode Mode => PetMode.Walking;
    internal override bool FallsOnEarthquake => true;
    internal override bool PreserveJumpSchedule => true;
    internal override void Update(PetActivityContext c)
    {
        var target = c.Actor.Navigation.Available[_link.To];
        c.Actor.Navigation.FootY = target.Y;
        if (RuinWalking.Step(c, _link.EndX ?? target.Center)) c.Actor.Navigation.Land(c, target);
    }
}
