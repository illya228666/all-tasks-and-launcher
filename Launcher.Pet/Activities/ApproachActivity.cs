using Launcher.Pet.Data;
using Launcher.Pet.Exploration;

namespace Launcher.Pet.Activities;

internal sealed class ApproachActivity : PetActivity
{
    private readonly RuinLink _link;
    internal ApproachActivity(RuinLink link) => _link = link;
    internal override PetMode Mode => PetMode.Walking;
    internal override bool UsesGroundPolicy => true;
    internal override bool PreserveJumpSchedule => true;
    internal override void Update(PetActivityContext c)
    {
        var nav = c.Actor.Navigation;
        float targetX = _link.Kind == RuinLinkKind.Jump ? nav.Available[nav.Support].Center : _link.X;
        if (!RuinWalking.Step(c, targetX)) return;
        PetActivity next;
        if (_link.Kind == RuinLinkKind.Climb) next = new GrabActivity(_link);
        else if (_link.Kind == RuinLinkKind.Bridge) next = new BridgeActivity(_link);
        else
        {
            var target = nav.Available[_link.To];
            float duration = RuinMotion.FlightTime(nav.FootY, target.Y, c.Metrics);
            next = new FlightActivity((target.Center - (c.Body.X + c.HalfWidth)) / duration, -RuinMotion.LaunchSpeed(nav.FootY, c.Metrics));
        }
        c.Actor.ContinueWith(next, c.Now);
    }
}
