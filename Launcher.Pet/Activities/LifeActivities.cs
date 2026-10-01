using Launcher.Pet.Animation;
using Launcher.Pet.Data;
using Launcher.Pet.Life;

namespace Launcher.Pet.Activities;

internal abstract class LifeActivity : PetActivity
{
    internal override bool UsesGroundPolicy => true;
    internal override bool CanLook(bool hasPickup) => false;
    internal override bool Calm => true;
    internal override void Update(PetActivityContext c)
    {
        var brain = c.Actor.Life!;
        var world = brain.World;
        var intent = world.State.Intent;
        if (intent is null) { c.Actor.Reset(c.Now); return; }
        var site = world.Site(intent.PickedUp ? intent.Destination : intent.Site)!;
        float x = LifeWorld.Position(site, c.Environment.Ruins!).X;
        if (c.Body.Appearance.UsesClipFiles)
        {
            var platform = c.Actor.Navigation.Available[site.Platform];
            string clipName = intent.Kind == LifeTaskKind.Observe ? "observe" : !intent.PickedUp ? "pickup"
                : intent.Kind switch { LifeTaskKind.Plant => "plant", LifeTaskKind.Recycle => "recycle", _ => "place" };
            var clip = c.Body.Appearance.Clip(clipName)!;
            var pose = c.Body.Appearance.GetFrameGeometry(clip.Row, clip.Phases["commit"], c.Actor.Hat.Attached);
            float handOffset = pose.ItemAnchor is { } hand ? (hand.X - pose.BodyAnchorX) * c.Body.Appearance.RenderScale * c.Environment.Scale : 0;
            x = Math.Clamp(x - handOffset, platform.Left + c.Metrics.HalfBody, platform.Right - c.Metrics.HalfBody);
        }
        if (!RuinWalking.Step(c, x)) { intent.ActionSeconds = 0; return; }
        intent.ActionSeconds += c.Step;
        Pose(c, intent);
        if (intent.ActionSeconds < 2) return;
        bool completed = true;
        if (intent.Kind == LifeTaskKind.Observe)
        {
            world.Observe(intent.Event);
            brain.Finish();
        }
        else if (!intent.PickedUp)
        {
            if (!world.PickUp(intent.Item, LifeHolder.Pet)) { brain.Cancel(); completed = false; }
            else { intent.PickedUp = true; intent.ActionSeconds = 0; }
        }
        else
        {
            completed = world.Place(intent.Item, intent.Destination, intent.Kind == LifeTaskKind.Plant, intent.Kind == LifeTaskKind.Recycle);
            if (completed) brain.Finish(); else brain.Cancel();
        }
        c.Actor.Reset(c.Now);
        if (completed && c.Body.Appearance.UsesClipFiles)
        {
            string clip = intent.Kind == LifeTaskKind.Observe ? "observe" : intent.PickedUp && world.Item(intent.Item)?.Holder == LifeHolder.Pet
                ? "pickup" : intent.Kind switch { LifeTaskKind.Plant => "plant", LifeTaskKind.Recycle => "recycle", _ => "place" };
            c.Actor.ContinueWith(new LifeGestureFinishActivity(clip), c.Now);
        }
    }
    protected virtual void Pose(PetActivityContext c, LifeIntent intent)
    {
        c.Actor.Present(PetMode.PuttingOnHat);
        c.Body.Row = intent.ActionSeconds < 1 ? PetAnimationCatalog.FailedRow : PetAnimationCatalog.WaveRow;
        c.Body.Frame = c.Body.Row == PetAnimationCatalog.FailedRow ? 5 : 0;
    }
}
internal sealed class ObserveLifeActivity : LifeActivity
{
    protected override void Pose(PetActivityContext c, LifeIntent intent)
    {
        c.Actor.Present(PetMode.Looking);
        c.Body.Row = PetAnimationCatalog.LookFirstRow;
        c.Body.Frame = 0;
    }
}
internal sealed class TransferLifeActivity : LifeActivity { }
internal sealed class PlantLifeActivity : LifeActivity
{
    protected override void Pose(PetActivityContext c, LifeIntent intent)
    {
        base.Pose(c, intent);
        if (intent.PickedUp) { c.Body.Row = PetAnimationCatalog.FailedRow; c.Body.Frame = 5; }
    }
}
internal sealed class RecycleLifeActivity : LifeActivity
{
    protected override void Pose(PetActivityContext c, LifeIntent intent)
    {
        base.Pose(c, intent);
        c.Body.Row = PetAnimationCatalog.FailedRow;
        c.Body.Frame = (int)(intent.ActionSeconds * 3) % 2 == 0 ? 5 : 6;
    }
}

// The resource operation has already committed. These poses finish the gesture
// while the next intention waits; interruption leaves the actual item in the world.
internal sealed class LifeGestureFinishActivity : PetActivity
{
    internal string Clip { get; }
    internal LifeGestureFinishActivity(string clip) => Clip = clip;
    internal int Frame(long elapsed) => Clip switch
    {
        "observe" => new[] { 11, 8, 4, 0 }[Math.Clamp((int)(elapsed / 100), 0, 3)],
        "pickup" => 7 + Math.Clamp((int)(elapsed / 80), 0, 4),
        "place" => 6 + Math.Clamp((int)(elapsed / 67), 0, 5),
        _ => 11 + Math.Clamp((int)(elapsed / 80), 0, 4)
    };
    internal override void Update(PetActivityContext c)
    {
        c.Actor.Present(PetMode.PuttingOnHat);
        if (c.Now - StartedAtMs >= 400) c.Actor.ContinueWith(new IdleActivity(), c.Now);
    }
}
