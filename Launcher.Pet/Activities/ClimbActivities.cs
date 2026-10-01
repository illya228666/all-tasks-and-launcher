using Launcher.Pet.Animation;
using Launcher.Pet.Data;
using Launcher.Pet.Exploration;

namespace Launcher.Pet.Activities;

internal abstract class ClimbActivity : PetActivity
{
    protected RuinLink Link { get; }
    protected ClimbActivity(RuinLink link) => Link = link;
    protected float Time;
    internal override bool FallsOnEarthquake => true;
    internal sealed override void Update(PetActivityContext c)
    {
        Time += c.Step;
        var nav = c.Actor.Navigation;
        var target = nav.Available[Link.To];
        var source = nav.Available[Link.From];
        System.Drawing.PointF sourcePoint = new(Link.X, source.Y);
        System.Drawing.PointF targetPoint = new(Link.EndX ?? Link.X, target.Y);
        var top = source.Y < target.Y ? sourcePoint : targetPoint;
        var bottom = source.Y < target.Y ? targetPoint : sourcePoint;
        c.Body.X = RuinMotion.ClimbX(top, bottom, nav.FootY, c.Metrics) - c.HalfWidth;
        c.Body.Row = PetAnimationCatalog.ClimbRow;
        c.Actor.Present(Mode);
        Climb(c, target);
    }
    protected abstract void Climb(PetActivityContext c, RuinPlatform target);
}

internal sealed class GrabActivity : ClimbActivity
{
    internal GrabActivity(RuinLink link) : base(link) { }
    internal override PetMode Mode => PetMode.Grabbing;
    protected override void Climb(PetActivityContext c, RuinPlatform target)
    {
        c.Body.Frame = 0;
        if (Time >= 0.2f) c.Actor.ContinueWith(new ClimbingActivity(Link), c.Now);
    }
}

internal sealed class ClimbingActivity : ClimbActivity
{
    internal ClimbingActivity(RuinLink link) : base(link) { }
    internal override PetMode Mode => PetMode.Climbing;
    protected override void Climb(PetActivityContext c, RuinPlatform target)
    {
        c.Body.Frame = 1 + (int)(Time / 0.18f) % 2;
        float distance = target.Y - c.Actor.Navigation.FootY;
        c.Actor.Navigation.FootY += Math.Clamp(distance, -c.Metrics.ClimbSpeed * c.Step, c.Metrics.ClimbSpeed * c.Step);
        if (Math.Abs(distance) <= c.Metrics.ClimbSpeed * c.Step) c.Actor.ContinueWith(new PullUpActivity(Link), c.Now);
    }
}

internal sealed class PullUpActivity : ClimbActivity
{
    internal PullUpActivity(RuinLink link) : base(link) { }
    internal override PetMode Mode => PetMode.PullingUp;
    protected override void Climb(PetActivityContext c, RuinPlatform target)
    {
        c.Body.Frame = 3;
        if (Time >= 0.32f) c.Actor.Navigation.Land(c, target);
    }
}
