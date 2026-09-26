using System.Drawing;
using Launcher.Pet.Behavior;
using Launcher.Pet.Data;

namespace Launcher.Pet.Activities;

internal sealed class DepartureActivity : PetActivity
{
    private readonly PetDeparture _motion;
    internal DepartureActivity(Point start) => _motion = new(start);
    internal float Scale => _motion.Scale;
    internal bool HasLanded => _motion.HasLanded;
    internal override bool AllowsEarthquake => false;
    internal override bool AllowsPetDrag => false;
    internal override void Update(PetActivityContext c)
    {
        if (!_motion.Update(c.Body, c.Environment, c.Elapsed)) return;
        c.Actor.Location = PetLocation.Desktop;
        c.Actor.Reset(c.Now);
    }
}
